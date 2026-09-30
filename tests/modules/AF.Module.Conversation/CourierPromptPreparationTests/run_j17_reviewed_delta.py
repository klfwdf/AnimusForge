"""Supersede the historical whole-root inverse with a J17-scoped prompt consumer review."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--mutate", choices=["drop-role"])
args = parser.parse_args()
review = json.loads((HERE / "source-review.json").read_text(encoding="utf-8-sig"))
baseline = subprocess.check_output(
    ["git", "show", review["baseline"] + ":" + review["path"]], cwd=ROOT
).decode("utf-8-sig").replace("\r\n", "\n")
assert hashlib.sha256(baseline.encode()).hexdigest() == review["beforeSha256"], "historical baseline drift"
expected = baseline.splitlines(keepends=True)
for hunk in reversed(review["hunks"]):
    start, before = hunk["start"], hunk["before"]
    assert expected[start:start + len(before)] == before, "historical inverse hunk drift"
    expected[start:start + len(before)] = hunk["after"]
assert hashlib.sha256("".join(expected).encode()).hexdigest() == review["afterSha256"], "historical inverse corrupt"

spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
prompt = (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs").read_text(encoding="utf-8-sig")
messages = (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptMessages.cs").read_text(encoding="utf-8-sig")
root = (ROOT / "CourierDeliveryBehavior.cs").read_text(encoding="utf-8-sig")
shout = (ROOT / "ShoutBehavior.cs").read_text(encoding="utf-8-sig")
reply = extract.declaration(prompt, "private CourierReplyGenerationRequest BuildReplyRequestFromPreparedPrompt(")
inbound = extract.declaration(prompt, "private InboundLetterGenerationRequest BuildInboundRequestFromPreparedPrompt(")
reply_messages = extract.declaration(messages, "private static List<object> BuildCourierReplyMessages(")
inbound_messages = extract.declaration(messages, "private static List<object> BuildInboundNpcLetterMessages(")
if args.mutate:
    inbound_messages = inbound_messages.replace("AppendCourierPersistentMemoryRoleMessages(", "SkipCourierPersistentMemoryRoleMessages(", 1)
for consumer, builder in ((reply, "BuildCourierReplyMessages("), (inbound, "BuildInboundNpcLetterMessages(")):
    assert consumer.count(builder) == 1, "Courier final prompt builder call changed"
    assert consumer.count("ShoutBehavior.HasPreprocessRuleHitForExternal(") == 1, "Courier qualification seam changed"
    assert consumer.index("ExcludeCourierSelectedRuleIds(") < consumer.index(builder), "Courier exclusion after message build"
    assert consumer.index("BuildUncompressedMemoryRoleMessagesForExternal(") < consumer.index(builder), "Courier memory role capture missing"
for method in (reply_messages, inbound_messages):
    assert method.count("AppendCourierPersistentMemoryRoleMessages(") == 1, "Courier role conversion missing"
    assert method.count("CreateCourierChatMessage(\"system\",") == 1, "Courier system message missing"
    assert method.count("CreateCourierChatMessage(\"user\",") >= 1, "Courier user message missing"
assert "private static bool HasPreprocessRuleHit(" not in root, "Courier private predicate was reintroduced"
assert "internal static bool HasPreprocessRuleHitForExternal(" in shout, "shared predicate seam missing"
assert "PendingInboundDeliveredMemoryIntents = CapturePendingInboundDeliveredMemoryIntents()" in root, "approved J17 save seam missing"
assert "RestorePendingInboundDeliveredMemoryIntents(npcStorage?.PendingInboundDeliveredMemoryIntents);" in root, "approved J17 load seam missing"

result = subprocess.run(
    [sys.executable, "-B", str(HERE / "run.py"), "--output-name", "j17-reviewed-delta"],
    cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=150)
if result.returncode != 0:
    print(result.stdout + result.stderr, end="")
    raise SystemExit(result.returncode)
assert "PASS 552 Courier prompt checks / 76 scenarios" in result.stdout, "Courier behavior acceptance missing"
print("PASS historical inverse identity preserved; J17 current Courier prompt consumers reviewed; 552/76 behavior replay")
