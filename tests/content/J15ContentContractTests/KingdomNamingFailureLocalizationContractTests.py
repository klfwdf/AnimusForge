"""Static evidence for worker/UI and diagnostic boundaries; does not execute C#."""
import ast
import importlib.util
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("inventory", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")
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

runtime_path = "src/AF.GameAdapter.Bannerlord/Kingdom/KingdomRebellionRuntimeController.cs"
runtime = read(runtime_path)
editor = read("src/AF.GameAdapter.Bannerlord/UI/Editors/KingdomRebellionEditorController.cs")
for source in (runtime, editor):
    for worker in source.split("Task.Run(delegate")[1:]:
        detached = worker.split("EnqueueKingdomRebellionNamingMainThreadAction", 1)[0]
        assert "new TextObject" not in detached and "KingdomUiText." not in detached
        assert not re.search(r"\bUi\(", detached)
        assert "runtimeGeneration" not in detached or "SaveRuntimeGuard" not in detached
assert "AutomaticNamingFailurePrefix + ex.Message" in runtime
assert "NamingRetryFailurePrefix + ex.Message" in runtime
assert "ForcedNamingFailurePrefix + ex.Message" in editor
base = subprocess.check_output(["git", "show", "f3c16e889:" + runtime_path], cwd=ROOT).decode("utf-8-sig")
for signature in ("internal static RebelKingdomNamingResult BuildFailedRebelKingdomNamingResult(", "internal static string BuildRebelKingdomNamingSystemPrompt(", "internal RebelKingdomNamingResult GenerateRebelKingdomNamingFromPrompts("):
    assert method(runtime, signature) == method(base, signature), signature
# No serialized/transient host DTO field was added or rewritten for display metadata.
host_path = "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs"
assert read(host_path) == subprocess.check_output(["git", "show", "f3c16e889:" + host_path], cwd=ROOT).decode("utf-8-sig")

en_path = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml"
cn_path = en_path.parent / "CNs/af_ui_strings-zh-CN.xml"
en = {n.get("id"): n.get("text") for n in ET.parse(en_path).iter("string")}
cn = {n.get("id"): n.get("text") for n in ET.parse(cn_path).iter("string")}
projection = method(editor, "internal static string NamingFailureDisplay(")
for key, text in re.findall(r'Ui\("([A-Za-z0-9_]+)", "([^"\n]+)"\)', projection):
    assert en[key] == text and cn[key], key
assert projection.count("text.Substring(") == 3
assert "return text;" in projection
report = method(editor, "internal static void ReportRebelKingdomNamingFailure(")
assert "true, NamingFailureDisplay(failureReason)" in report
feed = read("src/AF.GameAdapter.Bannerlord/UI/Errors/NonBlockingErrorReport.cs")
assert "public static void Show(string title, string detail, bool rememberForAnalysis = true)" in feed
assert "=> Show(title, detail, rememberForAnalysis, null);" in feed
publish = method(feed, "internal static void Show(")
assert "BuildVisibleMessage(normalizedTitle, visibleDetail)" in publish
assert "RedactConfiguredApiKeys(Normalize(displayDetail, normalizedDetail))" in publish
assert "AiErrorAnalysisInquiry.RememberFailure(normalizedTitle, normalizedDetail)" in publish
assert "ShouldSuppressDuplicate(normalizedTitle, normalizedDetail)" in publish
assert 'Logger.Log("NonBlockingErrorReport", "title=" + normalizedTitle + "\\n" + normalizedDetail)' in publish
ast.parse(read("tests/AF.Persistence/PlayerExports/run.py"))
print("PASS naming failures: 3 detached workers, known-prefix display only, full raw diagnostic/log/analysis preservation, stable public Show signature; C# execution NOT-RUN")
