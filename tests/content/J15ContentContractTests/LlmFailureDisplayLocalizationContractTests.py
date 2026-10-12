"""Static call-site/diagnostic integrity evidence. C# execution is NOT-RUN."""
import importlib.util
import json
from reviewed_localization_inverse import restore_conversation_resources
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

def restore_llm_resources(source):
    # Match the production StreamReader's BOM handling; no data/assertion changes.
    values = json.loads(read('content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json'))
    def restore(match):
        assert match[1] in values, match[1]
        return json.dumps(values[match[1]], ensure_ascii=False)
    source = re.sub(r'LlmSourceTexts.Get\("([^"\n]+)"\)', restore, source)
    raw = json.loads(read('content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json'))
    return re.sub(r'(?:AnimusForge\.)?PromptComposerTexts.Raw\("([^"\n]+)"\)', lambda match: json.dumps(raw[match[1]], ensure_ascii=False), source)


def lexical(source):
    # Ignore formatting only outside literals/comments. Raw diagnostic strings stay exact.
    parts, start = [], 0
    for match in audit.LEX.finditer(source):
        parts += [re.sub(r"\s+", "", source[start:match.start()]), json.dumps(audit.decode(match[0]), ensure_ascii=False) if match.lastgroup == "string" else match[0]]
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
    if relative.endswith('ShoutUtils.cs'):
        # Dedicated settlement HeroNPC Prompt projection; internal Raw role
        # classification/priority remains immutable. Check provenance before inverse.
        module = ROOT / 'content/modules/AF.Module.Conversation/ModuleData'
        prompt_cn = json.loads((module / 'ConversationRuntimePrompts.json').read_text('utf-8-sig'))
        prompt_en = json.loads((module / 'ConversationRuntimePrompts.en.json').read_text('utf-8-sig'))
        raw = json.loads((module / 'ConversationRuntimeCompatibility.json').read_text('utf-8-sig'))
        pairs = {
            'SettlementHeroNpcRoleLord': 'AF_CONV_Runtime_0812',
            'SettlementHeroNpcRoleNotable': 'AF_CONV_Runtime_0813',
            'SettlementHeroNpcRoleWanderer': 'AF_CONV_Runtime_0814',
            'SettlementHeroNpcRoleHero': 'AF_CONV_Runtime_0815',
            'SettlementHeroNpcPrefix': 'AF_CONV_Runtime_0819',
            'SettlementHeroNpcTotalPrefix': 'AF_CONV_Runtime_0820',
            'SettlementHeroNpcTotalSuffix': 'AF_CONV_Runtime_0821',
        }
        for prompt_key, raw_key in pairs.items():
            assert prompt_cn[prompt_key] == raw[raw_key], prompt_key + ' original CN source'
            assert prompt_en[prompt_key].strip(), prompt_key + ' EN projection'
        composer = ROOT / 'content/modules/AF.Module.Prompt/ModuleData'
        for language, values in [('zh-CN', prompt_cn), ('en', prompt_en)]:
            texts = json.loads((composer / ('PromptComposerTexts.' + language + '.json')).read_text('utf-8-sig'))
            assert values['SettlementHeroNpcPrefix'] == texts['persona_scene_settlement_heronpc_prefix_raw']
        signature = 'private static string ProjectSettlementHeroTypeForPrompt(string type)'
        actual = method(current, signature)
        expected = 'private static string ProjectSettlementHeroTypeForPrompt(string type)\n\t{\n\t\tif (type == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0812")) return ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleLord");\n\t\tif (type == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0813")) return ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleNotable");\n\t\tif (type == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0814")) return ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleWanderer");\n\t\treturn ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleHero");\n\t}'
        assert lexical(actual) == lexical(expected), 'exact reviewed role projection only'
        assert current.count(actual) == 1
        current = current.replace(actual, '', 1)
        dedicated = method(current, 'public static string BuildCurrentSettlementHeroNpcLineForPrompt(')
        restored = dedicated
        call = 'ProjectSettlementHeroTypeForPrompt(value2)'
        assert restored.count(call) == 1
        restored = restored.replace(call, 'value2', 1)
        for prompt_key, raw_key in pairs.items():
            if not prompt_key.startswith('SettlementHeroNpcRole'):
                expression = 'ConversationRuntimeTexts.Prompt("' + prompt_key + '")'
                assert restored.count(expression) == 1
                restored = restored.replace(expression, 'ConversationRuntimeTexts.Raw("' + raw_key + '")', 1)
        current = current.replace(dedicated, restored, 1)
    current = restore_llm_resources(restore_conversation_resources(current))
    if relative.endswith('CourierDeliveryBehavior.GenerationLifecycle.cs'):
        before_log = 'Logger.Log("CourierDelivery", "[UnifiedPostprocess] failed: " + error);'
        assert current.count(before_log) == 1
        current = current.replace(before_log, 'Logger.Log("CourierDelivery", "[UnifiedPostprocess] 调用失败: " + error);', 1)
    if relative.endswith('ShoutUtils.cs'):
        current = current.replace('string pattern = ', 'const string pattern = ', 1).replace('string seaSuffix = ', 'const string seaSuffix = ', 1)
        current = re.sub(r'case var (conversationMatch\d+) when \1 == ("(?:\\.|[^"\\])*"):', r'case \2:', current)
        distance = 'string.Format(' + json.dumps('；距离：{0:0.0} 公里', ensure_ascii=False) + ', num)'
        assert current.count(distance) == 1
        current = current.replace(distance, '$' + json.dumps('；距离：{num:0.0} 公里', ensure_ascii=False), 1)
        translations = {
            'Unified LLM Gateway request failed: ': '统一 LLM Gateway 请求失败：',
            'The model response is empty; a persona cannot be generated.': '模型回复为空，无法生成人设。',
            'The model response could not be parsed as persona JSON.': '模型回复无法解析为人设 JSON。',
        }
        for english, raw in translations.items():
            literal = json.dumps(english, ensure_ascii=False)
            assert current.count(literal) == 1, english
            current = current.replace(literal, json.dumps(raw, ensure_ascii=False), 1)
    if relative.endswith('WeekZeroOpeningSummaryGenerationController.cs'):
        for english, raw in {
            '[Week0Short][WARN] Short report parsed empty: ': '[Week0Short][WARN] 短周报解析为空 ',
            '[Week0Short][WARN] Short report generation failed: ': '[Week0Short][WARN] 短周报生成失败 ',
            '; attempt ': '，尝试 ',
            'unknown error': '未知错误',
        }.items():
            literal = json.dumps(english, ensure_ascii=False)
            assert current.count(literal) == (2 if english == '; attempt ' else 1), english
            current = current.replace(literal, json.dumps(raw, ensure_ascii=False))
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
    actual = method(retry, signature)
    actual = restore_llm_resources(actual)
    if signature == 'public static string BuildFailureDetail(':
        metadata = 'string detail = stringBuilder.ToString().TrimEnd();\n\t\tRetryErrorProjection.Remember(detail, fullReason, fullModelReply, fullRawResponse);\n\t\treturn detail;'
        assert actual.count(metadata) == 1, 'metadata retains original reason/reply/response fields and exact returned string'
        actual = actual.replace(metadata, 'return stringBuilder.ToString().TrimEnd();', 1)
        reason_capture = 'string fullReason = NormalizeFullText(reason, "未知错误");\n\t\tstringBuilder.Append(fullReason);'
        assert actual.count(reason_capture) == 1
        actual = actual.replace(reason_capture, 'stringBuilder.Append(NormalizeFullText(reason, "未知错误"));', 1)
    if signature == 'public static void ShowFailurePopup(':
        projection_arg = 'true,\n\t\t\t\tProjectKnownErrorForUi(message))'
        assert actual.count(projection_arg) == 1, 'raw message remains the diagnostic; projection is a separate display argument'
        assert 'public static void Show(string title, string detail, bool rememberForAnalysis = true)' in read('src/AF.GameAdapter.Bannerlord/UI/Errors/NonBlockingErrorReport.cs'), 'explicit true retains the original diagnostic-recording default'
        actual = actual.replace(', ' + projection_arg, ')', 1)
        actual = actual.replace('NonBlockingErrorReport.Show(', '// 纯 LLM 失败报告改走左下角消息，避免完整 API 响应覆盖正在操作的菜单。\n\t\t\tNonBlockingErrorReport.Show(', 1)
    if signature == 'public static bool PromptRetryBlocking(':
        raw_display = 'NonBlockingErrorReport.Show(new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(), error, true, RetryFailureDisplay(stageName, error));'
        assert actual.count(raw_display) == 1, 'blocking main-thread refusal records the raw error and displays its projection separately'
        actual = actual.replace(raw_display, 'ShowFailurePopup(new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(), RetryFailureDisplay(stageName, error));', 1)
    if signature == 'private static void ShowRetryPrompt(':
        raw_display = 'error, true, RetryFailureDisplay(stageName, error))'
        assert actual.count(raw_display) == 1, 'retry inquiry records raw error while preserving stage display'
        actual = actual.replace(raw_display, 'RetryFailureDisplay(stageName, error))', 1)
        actual = actual.replace('// Preserve retry decisions while reporting full API diagnostics outside the inquiry.', '// 保留重试决策窗口，但把可能很长的接口响应从窗口主体迁到非阻塞消息。', 1)
    assert lexical(actual) == lexical(method(before, signature)), signature
projection = method(retry, "internal static string ProjectKnownErrorForUi(")
assert lexical(projection) == lexical('internal static string ProjectKnownErrorForUi(string value) { return RetryErrorProjection.Project(value); }')
metadata = retry.split('private static class RetryErrorProjection', 1)[1].split('// Inputs are detached strings.', 1)[0]
assert 'ConditionalWeakTable<string, Parts> Details' in metadata and 'Details.Add(detail, new Parts(reason, reply, response))' in metadata
assert 'if (value == null) return null;' in metadata and 'if (!Details.TryGetValue(value, out Parts parts)) return Reason(value);' in metadata
assert 'text.Append(Reason(parts.Reason));' in metadata
assert 'text.Append(parts.Reply.Length == 0 ?' in metadata and ': parts.Reply);' in metadata
assert 'text.Append(parts.Response);' in metadata and '!string.Equals(parts.Response, parts.Reply, StringComparison.Ordinal)' in metadata
reason = metadata.split('private static string Reason(', 1)[1]
assert 'reason == SaveRuntimeGuard.BuildStaleRequestErrorText()' in reason and 'return value;' in reason
for key in ('RetryCheckMcm', 'RetryUnknownError', 'LegacyErrorPrefixMissingApiKey', 'LegacyErrorPrefixMissingModel', 'RetryActionTagsMissing'):
    assert 'reason == LlmSourceTexts.Get("' + key + '")' in reason, key
assert 'reason.StartsWith(prefix, StringComparison.Ordinal) && reason.EndsWith(suffix, StringComparison.Ordinal) && reason.Length >= prefix.Length + suffix.Length' in reason
assert 'reason.Substring(prefix.Length, reason.Length - prefix.Length - suffix.Length)' in reason
assert 'text.SetTextVariable("REASON", "AFRETRYREASONEND")' in reason
assert 'text.ToString().Replace("AFRETRYREASONEND", reason.Substring(' in reason
assert not re.search(r'new TextObject\((?:value|parts\.Reply|parts\.Response)|\.Take\(', metadata), 'unknown/model/provider diagnostics remain literal and complete'
publish = method(retry, "internal static void ShowLocalizedFailurePopup(")
assert "PostToMainThread(Show)" in publish
assert "new TextObject" not in publish.split("void Show()", 1)[0]
assert "NormalizeFullText(diagnostic," in publish and "true, display)" in publish
resolve = method(retry, "private static string ResolveFailureTemplate(")
assert 'StartsWith("{=", StringComparison.Ordinal)' in resolve
assert 'SetTextVariable(variableName, "AFRETRYTEMPLATEVALUEEND")' in resolve
assert 'text.ToString().Replace("AFRETRYTEMPLATEVALUEEND", value)' in resolve
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
