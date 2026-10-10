"""Static call-site/diagnostic integrity evidence. C# execution is NOT-RUN."""
import importlib.util
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "3963e0ef5"
spec = importlib.util.spec_from_file_location("inventory", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


def read(relative):
    return (ROOT / relative).read_text(encoding="utf-8-sig")


def original(relative):
    return subprocess.check_output(["git", "show", BASELINE + ":" + relative], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")


def lexical(source):
    # Ignore formatting only outside literals/comments. Raw diagnostic strings stay exact.
    parts, start = [], 0
    for match in audit.LEX.finditer(source):
        parts += [re.sub(r"\s+", "", source[start:match.start()]), match[0]]
        start = match.end()
    return "".join(parts) + re.sub(r"\s+", "", source[start:])


def method(source, signature):
    start = source.index(signature)
    code = audit.tokenize(source)[0]
    opening, depth = code.index("{", start), 1
    for end in range(opening + 1, len(code)):
        depth += (code[end] == "{") - (code[end] == "}")
        if depth == 0:
            return source[start:end + 1]
    raise AssertionError(signature)


titles = {
    "AF_UI_Retry_CourierReplyPreprocessFailed": '"信使回信前处理失败"',
    "AF_UI_Retry_CourierLetterPreprocessFailed": '"信使来信前处理失败"',
    "AF_UI_Retry_NativeReplyFailed": '"自由对话正文生成失败"',
    "AF_UI_Retry_UnnamedPersonaFailed": '"无名NPC人设生成失败"',
    "AF_UI_Retry_UnnamedPersonaParseFailed": '"无名NPC人设解析失败"',
    "AF_UI_Retry_WeekZeroShortReportFailed": '"第0周短周报生成失败"',
    "AF_UI_Retry_ExternalAuxiliaryFailed": '(source ?? "ExternalAuxiliary") + " 失败"',
}
paths = (
    "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs",
    "src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeMainReply.cs",
    "src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs",
    "src/AF.GameAdapter.Bannerlord/Weekly/WeekZeroOpeningSummaryGenerationController.cs",
    "src/modules/AF.Module.Llm/Application/ConfiguredChatApplicationAdapter.cs",
)
calls = 0
for relative in paths:
    current = read(relative)
    code, _ = audit.tokenize(current)
    replacements = []
    for match in re.finditer(r"LlmRetryPrompt\.ShowLocalizedFailurePopup\s*\(", code):
        spans, end = audit.call_argument_spans(code, match.end() - 1)
        title = current[slice(*spans[0])].strip()
        key = re.search(r"\{=([^}]+)\}", title)[1]
        raw_diagnostic = current[slice(*spans[1])].strip()
        replacements.append((match.start(), end, "LlmRetryPrompt.ShowFailurePopup(" + titles[key] + ", " + raw_diagnostic + ")"))
    assert replacements, relative
    calls += len(replacements)
    for start, end, replacement in reversed(replacements):
        current = current[:start] + replacement + current[end:]
    assert lexical(current) == lexical(original(relative)), "Non-display algorithm or raw diagnostic changed: " + relative
assert calls == 11, calls

relative = "src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs"
retry, before = read(relative), original(relative)
for signature in (
    "public static bool IsRetryableLlmError(", "public static string BuildFailureDetail(",
    "public static void ShowFailurePopup(", "public static Task<bool> PromptRetryAsync(",
    "public static bool PromptRetryBlocking(", "private static void ShowRetryPrompt(",
    "private static void PostToMainThread(", "private static string NormalizeFullText(",
):
    assert method(retry, signature) == method(before, signature), signature
publish = method(retry, "internal static void ShowLocalizedFailurePopup(")
assert "PostToMainThread(Show)" in publish
assert "new TextObject" not in publish.split("void Show()", 1)[0]
assert "NormalizeFullText(diagnostic," in publish and "true, display)" in publish
resolve = method(retry, "private static string ResolveFailureTemplate(")
assert 'StartsWith("{=", StringComparison.Ordinal)' in resolve
assert "SetTextVariable(variableName, value)" in resolve
detail = method(retry, "private static string FailureDetailForDisplay(")
assert "text.Append(response)" in detail and ": reply)" in detail
assert "StringComparison.Ordinal" in detail  # Only identical response/reply duplication is suppressed.
assert not re.search(r"Substring|\.Take\(|new TextObject\((?:modelReply|rawResponse|reasonCode)\)", detail)

en_path = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml"
en = {n.get("id"): n.get("text") for n in ET.parse(en_path).iter("string")}
cn = {n.get("id"): n.get("text") for n in ET.parse(en_path.parent / "CNs/af_ui_strings-zh-CN.xml").iter("string")}
seen = set()
for relative in (*paths, "src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs"):
    for match, _ in audit.tokenize(read(relative))[1]:
        marker = re.match(r"\{=(AF_UI_Retry_[^}]+)\}(.*)", audit.decode(match[0]), re.S)
        if marker:
            assert en[marker[1]] == marker[2] and cn[marker[1]], (relative, marker[1])
            assert not audit.HAN.search(marker[2]), marker[1]
            seen.add(marker[1])
assert set(titles) <= seen

# Renaming the UI entry must not hide Chinese raw diagnostics. Test positional,
# named and nested calls, including commas/parentheses embedded in string literals.
sample = '''LlmRetryPrompt.ShowLocalizedFailurePopup("标题", BuildFailureDetail("诊断,(保持)", ""),
    reasonTemplate: "原因", reasonCode: "代码");'''
code, tokens = audit.tokenize(sample)
roles = {audit.decode(m[0]): audit.localized_failure_argument(code, frames) for m, frames in tokens}
assert roles == {"标题": "titleTemplate", "诊断,(保持)": "diagnostic", "": "diagnostic", "原因": "reasonTemplate", "代码": "reasonCode"}, roles
assert "ShowLocalizedFailurePopup" in audit.SINKS
scanner = read("tools/localization/inventory_untranslated.py")
assert 'elif failure_argument == "diagnostic":' in scanner
assert 'failure_argument != "diagnostic"' in scanner

hooks = read("tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/LivenessHooks.cs.txt")
historical, current = hooks.split("// @@COURIER_CURRENT_LEAVES@@")
assert "void ShowLocalizedFailurePopup(" in historical
assert "void ShowLocalizedFailurePopup(" in current and "reasonCode = null) => NativeHost.Game();" in current
print("PASS static LLM failure localization: 11 display-only call changes, original full diagnostics/algorithms/dispatcher intact, fallbacks paired, scanner and current Courier fixture boundary maintained; C# execution NOT-RUN")
