"""Static display contract; does not claim execution of the C# error flow."""
import ast
import importlib.util
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location(
    "inventory_untranslated", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)

def method(source, signature):
    start = source.index(signature)
    code = audit.tokenize(source)[0]
    opening = code.index("{", start)
    depth = 1
    for end in range(opening + 1, len(code)):
        depth += (code[end] == "{") - (code[end] == "}")
        if depth == 0:
            return source[start:end + 1]
    raise AssertionError(signature)

relative = "src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs"
retry = (ROOT / relative).read_text(encoding="utf-8-sig")
baseline = subprocess.check_output(["git", "show", "b98a7d970:" + relative], cwd=ROOT).decode("utf-8-sig")
for signature in ("public static bool IsRetryableLlmError(", "public static string BuildFailureDetail(",
                  "private static void PostToMainThread(", "private static string NormalizeFullText("):
    assert method(retry, signature) == method(baseline, signature), signature

en = {node.get("id"): node.get("text") for node in ET.parse(
    ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml").iter("string")}
seen = set()
for filename in ("LlmRetryPrompt.cs", "NonBlockingErrorReport.cs", "AiErrorAnalysisInquiry.cs"):
    source = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Errors" / filename).read_text(encoding="utf-8-sig")
    for match, _ in audit.tokenize(source)[1]:
        value = audit.decode(match[0])
        marker = re.match(r"\{=(AF_UI_(?:Retry_|ErrorReport_|ErrorAnalysis_)[^}]+)\}(.*)", value, re.S)
        if marker:
            assert en[marker[1]] == marker[2], (filename, marker[1])
            assert "\\n" not in en[marker[1]], marker[1]
            seen.add(marker[1])

analysis = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Errors/AiErrorAnalysisInquiry.cs").read_text(encoding="utf-8-sig")
for name in ("ShowAnalysisResult", "ShowAnalysisFailure"):
    assert "SafeInvoke(onClosed)" in method(analysis, "private static void " + name + "(")
assert "completion.TrySetResult(true)" in method(retry, "private static void ShowRetryPrompt(")
assert "completion.TrySetResult(false)" in method(retry, "private static void ShowRetryPrompt(")
assert "PostToMainThread(Show)" in method(retry, "private static void ShowRetryPrompt(")

fixture = ROOT / "tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/run_liveness.py"
ast.parse(fixture.read_text(encoding="utf-8-sig"))
assert "private static string RetryStageDisplay(" in fixture.read_text(encoding="utf-8-sig")
hooks = (fixture.parent / "LivenessHooks.cs.txt").read_text(encoding="utf-8-sig")
assert "void SetTextVariable(string name,object value)" in hooks
assert len(seen) >= 15
print("PASS static error UI: fallbacks, real newlines, unchanged protocol/dispatcher, retry/callback boundaries")
