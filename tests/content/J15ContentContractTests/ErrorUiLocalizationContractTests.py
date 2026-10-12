"""Static display contract; does not claim execution of the C# error flow."""
import ast
import importlib.util
import json
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
for signature in ("private static void PostToMainThread(", "private static string NormalizeFullText("):
    assert method(retry, signature) == method(baseline, signature), signature

# Reconstruct compatibility reads from their actual embedded source values.
# The retry predicate and original diagnostic format must remain unchanged.
compat = json.loads((ROOT / "content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json").read_text(encoding="utf-8-sig"))
def restore_compat(text):
    return re.sub(r'LlmSourceTexts.Get\("([^"]+)"\)', lambda m: json.dumps(compat[m[1]], ensure_ascii=False), text)
signature = "public static bool IsRetryableLlmError("
assert restore_compat(method(retry, signature)) == method(baseline, signature), signature
signature = "public static string BuildFailureDetail("
restored = restore_compat(method(retry, signature))
old_append = '\t\tstringBuilder.Append(NormalizeFullText(reason, "未知错误"));'
new_append = '\t\tstring fullReason = NormalizeFullText(reason, "未知错误");\n\t\tstringBuilder.Append(fullReason);'
assert new_append in restored
restored = restored.replace(new_append, old_append)
metadata = '\t\tstring detail = stringBuilder.ToString().TrimEnd();\n\t\tRetryErrorProjection.Remember(detail, fullReason, fullModelReply, fullRawResponse);\n\t\treturn detail;'
assert metadata in restored
restored = restored.replace(metadata, '\t\treturn stringBuilder.ToString().TrimEnd();')
assert restored == method(baseline, signature), 'Diagnostic grammar changed beyond the explicit weak display metadata'

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
for call in re.finditer(r'Ui(?:Format)?\("(AF_UI_(?:ErrorAnalysis_|Retry_)[^"]+)",\s*("(?:[^"\\]|\\.)*")', analysis):
    fallback = audit.decode(call[2])
    assert en[call[1]] == fallback, call[1]
    assert "\\n" not in en[call[1]], call[1]
    seen.add(call[1])
for name in ("ShowAnalysisResult", "ShowAnalysisFailure"):
    assert "SafeInvoke(onClosed)" in method(analysis, "private static void " + name + "(")
assert "completion.TrySetResult(true)" in method(retry, "private static void ShowRetryPrompt(")
assert "completion.TrySetResult(false)" in method(retry, "private static void ShowRetryPrompt(")
assert "PostToMainThread(Show)" in method(retry, "private static void ShowRetryPrompt(")

worker = method(analysis, "private static bool TryAnalyzeWithPreprocessApi(")
assert "new TextObject" not in worker
assert not re.search(r"\bUi(?:Format)?\(", worker)
assert "out AnalysisFailure error" in worker
for kind in ("ConfigurationUnavailable", "ConfigurationIncomplete", "Http", "Request", "EmptyReply", "Timeout", "Exception"):
    assert "new AnalysisFailure(AnalysisFailureKind." + kind in worker, kind
resolver = method(analysis, "private static string ResolveAnalysisFailure(")
assert "NormalizeText(error.Detail" in resolver
assert "ResolveAnalysisFailure(error)" in method(analysis, "private static void ShowAnalysisFailure(")
assert "MainThreadActions.Enqueue(delegate" in method(analysis, "private static void BeginAnalysis(")
assert "Interlocked.Exchange(ref _analysisInProgress, 0)" in method(analysis, "private static void BeginAnalysis(")
analysis_baseline = subprocess.check_output([
    "git", "show", "3eadd643f:src/AF.GameAdapter.Bannerlord/UI/Errors/AiErrorAnalysisInquiry.cs"], cwd=ROOT).decode("utf-8-sig")
prompt = r"private const string AnalysisSystemPrompt = .*;"
capture_root = ROOT / "content/foundation/AF.Foundation.Localization/ModuleData"
capture_zh = json.loads((capture_root / "AdapterPromptCapture.zh-CN.json").read_text(encoding="utf-8-sig"))
capture_en = json.loads((capture_root / "AdapterPromptCapture.en.json").read_text(encoding="utf-8-sig"))
analysis_keys = {
    "error_analysis_system", "error_analysis_title", "error_analysis_unknown",
    "error_analysis_truncated", "error_analysis_redacted",
}
assert set(re.findall(r'AdapterPromptCaptureTexts.PcapText\("([^"]+)"\)', analysis)) == analysis_keys
for key in analysis_keys:
    assert capture_zh[key] and capture_en[key], key
    assert not re.search(r"[\u3400-\u9fff]", capture_en[key]), key
# Invert only the reviewed package substitutions. Original truncation, redaction,
# caching and error-envelope algorithms remain checked against the old source.
analysis_restored = re.sub(
    r'AdapterPromptCaptureTexts.PcapText\("(error_analysis_[^"]+)"\)',
    lambda m: json.dumps(capture_zh[m[1]], ensure_ascii=False), analysis)
analysis_restored = analysis_restored.replace(
    "private static string AnalysisSystemPrompt =>", "private const string AnalysisSystemPrompt =")
assert analysis_restored.count('Logger.Log("AiErrorAnalysis", "Main-thread callback failed: " + ex.Message)') == 1
analysis_restored = analysis_restored.replace(
    'Logger.Log("AiErrorAnalysis", "Main-thread callback failed: " + ex.Message)',
    'Logger.Log("AiErrorAnalysis", "主线程回调失败: " + ex.Message)')
assert re.search(prompt, analysis_restored)[0] == re.search(prompt, analysis_baseline)[0]
for signature in ("private static string LimitForAnalysis(", "private static string RedactConfiguredApiKeys(",
                  "private static bool TryGetCachedAnalysis(", "private static void StoreCachedAnalysis(",
                  "public static void OnApplicationTick("):
    assert method(analysis_restored, signature) == method(analysis_baseline, signature), signature
assert 'new PromptMessage("user", "报错标题：" + safeTitle + "\\n<error>\\n" + safeDetail + "\\n</error>")' in method(analysis_restored, "private static bool TryAnalyzeWithPreprocessApi(")
capture_loader = (ROOT / "src/AF.GameAdapter.Bannerlord/Prompt/AdapterPromptCaptureTexts.cs").read_text(encoding="utf-8-sig")
assert 'GetModuleDataFilePath("AdapterPromptCapture.json")' in capture_loader
assert 'internal static string PcapText(string key) => Values.Value[key];' in capture_loader
assert "TextObject" not in capture_loader and "CurrentLanguage" not in capture_loader
entries = json.loads((ROOT / "content/content-map.json").read_text(encoding="utf-8-sig"))["entries"]
assert any(e["source"].endswith("/AdapterPromptCapture.zh-CN.json") and e["target"] == "ModuleData/AdapterPromptCapture.json" for e in entries)

fixture = ROOT / "tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/run_liveness.py"
ast.parse(fixture.read_text(encoding="utf-8-sig"))
assert "private static string RetryStageDisplay(" in fixture.read_text(encoding="utf-8-sig")
hooks = (fixture.parent / "LivenessHooks.cs.txt").read_text(encoding="utf-8-sig")
assert "void SetTextVariable(string name,object value)" in hooks
assert len(seen) >= 15
print("PASS static error UI: fallbacks, real newlines, unchanged protocol/dispatcher, retry/callback boundaries")
