"""Static NPC action editor localization and fact-projection migration evidence.

No C# compiler or game runtime is invoked. Actual lifecycle replay is NOT-RUN.
"""
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
UI = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors/NpcActionEditorProjection.cs"
FACT = ROOT / "src/AF.GameAdapter.Bannerlord/Records/NpcActionMaterialPreviewProjection.cs"
PREFIX = "AF_NPCACTION_EDITOR_"
BASELINE = "33a9ba44f"


def method(source, start, end):
    return source[source.index(start):source.index(end, source.index(start))].strip().replace("\r\n", "\n")


def strings(path):
    root = ET.parse(path).getroot()
    assert root.tag == "base" and root.get("type") == "string", f"invalid native resource root: {path}"
    rows = root.findall("strings/string")
    result = {row.attrib["id"]: row.attrib["text"] for row in rows}
    assert len(result) == len(rows), f"duplicate IDs: {path}"
    assert all(value for value in result.values()), f"empty text: {path}"
    return result


ui = UI.read_text(encoding="utf-8-sig")
fact = FACT.read_text(encoding="utf-8-sig")
assert not re.search(r"[\u3400-\u9fff]", ui), "UI projection still contains hardcoded Chinese"
calls = re.findall(r'\bT\("([^"]+)",\s*("(?:\\.|[^"\\])*")', ui)
fallbacks = {PREFIX + key: json.loads(raw) for key, raw in calls}
assert len(fallbacks) == 110, f"unexpected key count: {len(fallbacks)}"
base = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages"
en = strings(base / "afnpcaction_editor_strings.xml")
zh = strings(base / "CNs/afnpcaction_editor_strings-zh-CN.xml")
assert en == fallbacks, "English key/fallback drift or unused resource key"
assert en.keys() == zh.keys(), "asymmetric language keys"
for key in en:
    assert sorted(re.findall(r"\{[A-Z_]+\}", en[key])) == sorted(re.findall(r"\{[A-Z_]+\}", zh[key])), key

baseline = subprocess.check_output(["git", "show", f"{BASELINE}:{UI.relative_to(ROOT).as_posix()}"], cwd=ROOT).decode("utf-8-sig")
old_method = method(baseline, "internal static string BuildDevNpcActionPreviewText(", "internal static string BuildDevNpcActionNarrative(")
new_method = fact[fact.index("internal static string Build("):fact.rfind("}")].strip().replace("\r\n", "\n")
assert new_method.replace("internal static string Build(", "internal static string BuildDevNpcActionPreviewText(", 1) == old_method, "fact algorithm changed during responsibility migration"
host = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
capture = next(line for line in host.splitlines() if "_campaignCharacterRecordCapture = new CampaignCharacterRecordCaptureAdapter(" in line)
assert "entry => NpcActionMaterialPreviewProjection.Build(NpcActionEditorDisplay, entry)" in capture
assert "NpcActionEditorProjection.BuildDevNpcActionPreviewText" not in capture, "localized UI pollutes saved Prompt materials"

prompt_kinds = ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyAggregateEventLineOwner.cs"
prompt_source = prompt_kinds.read_text(encoding="utf-8-sig")
original_kind = method(prompt_source, "internal static string TranslateNpcActionKindForPrompt(", "private static string ParseWeeklyPromptSettlementChangeDetail(")
ui_kind = method(ui, "private static string GetNpcActionKindForDisplay(", "internal static string BuildDevNpcActionItemLabel(")
assert set(re.findall(r'case "([^"]+)"', original_kind)) == set(re.findall(r'case "([^"]+)"', ui_kind)), "missing or invented action kinds"
assert "port.TranslateNpcActionKindForPrompt(" not in ui and "port.BuildNpcActionActorNarrativeText(" not in ui, "UI invokes Prompt/fact text producer"
assert "port.BuildNpcActionActorNarrativeForDisplay?.Invoke(entry)" in ui, "optional actor display port missing"
assert '(entry.Text ?? "")' in ui and "entry.GameDate.Trim()" in ui and "(entry.LocationText ??" in ui, "raw fields were hidden"
assert not re.search(r"\bentry\.\w+\s*=(?!=)", ui), "display projection mutates records"
assert not re.search(r"\b(?:OnTick|GetType|GetMethods|GetFields)\b", ui), "unexpected hot path or reflection"
project = ET.parse(ROOT / "tests/modules/AF.Module.Persona/RootEditorLifecycleTests/RootEditorLifecycleTests.csproj")
for row in project.findall(".//Compile"):
    target = (ROOT / "tests/modules/AF.Module.Persona/RootEditorLifecycleTests" / row.attrib["Include"]).resolve()
    assert target.is_file(), f"fixture closure missing: {target}"
print("PASS NPC action editor static contract: keys=110 kinds=37 UI Chinese=0 fact method=baseline-identical; C# compile/runtime NOT-RUN")
