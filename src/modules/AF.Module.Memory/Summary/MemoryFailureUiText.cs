using System;
using TaleWorlds.Localization;

namespace AnimusForge;

// Detached display descriptions can travel with worker diagnostics without resolving game text.
internal sealed class MemoryFailureUiText
{
    private readonly string _id, _fallback;
    private readonly (string Name, object Value)[] _variables;

    private MemoryFailureUiText(string id, string fallback, (string, object)[] variables)
    { _id = id; _fallback = fallback ?? ""; _variables = variables; }

    internal static MemoryFailureUiText T(string id, string fallback, params (string Name, object Value)[] variables)
        => new MemoryFailureUiText(id, fallback, variables);
    internal static MemoryFailureUiText Raw(string value) => new MemoryFailureUiText(null, value, null);
    internal static MemoryFailureUiText Error(string value) => string.IsNullOrWhiteSpace(value)
        ? T("AF_MEMORY_UI_UnknownError", "Unknown error") : Raw(value);

    internal static MemoryFailureUiText ParseFailure(string parseError, string reply)
    {
        MemoryFailureUiText detail = parseError?.Trim() switch
        {
            "SUMMARY 为空。" => T("AF_MEMORY_UI_SummaryEmpty", "SUMMARY is empty."),
            "TITLE 或 SUMMARY 为空。" => T("AF_MEMORY_UI_TitleOrSummaryEmpty", "TITLE or SUMMARY is empty."),
            "JSON 解析失败。" => T("AF_MEMORY_UI_JsonParseFailed", "JSON parsing failed."),
            "找不到 JSON 对象。" => T("AF_MEMORY_UI_JsonObjectMissing", "No JSON object could be found."),
            null or "" => T("AF_MEMORY_UI_UnknownParseError", "Unknown parsing error."),
            _ => Raw(parseError.Trim())
        };
        const string requiredFieldsPrefix = "未找到包含必需字段的 JSON 对象：";
        if (parseError?.StartsWith(requiredFieldsPrefix, StringComparison.Ordinal) == true)
            detail = T("AF_MEMORY_UI_JsonRequiredFieldsMissing", "No JSON object contained the required fields: {FIELDS}.",
                ("FIELDS", parseError.Substring(requiredFieldsPrefix.Length).TrimEnd('。')));
        return T("AF_MEMORY_UI_SummaryParseFailed", "Summary response format could not be parsed: {ERROR}\n\nFull model reply:\n{REPLY}",
            ("ERROR", detail), ("REPLY", string.IsNullOrWhiteSpace(reply) ? T("AF_MEMORY_UI_NoModelReply", "No model reply was received or could be parsed") : Raw(reply.Trim())));
    }

    // Called only by UI publication/acceptance on the existing main-thread phase.
    internal string Resolve()
    {
        if (_id == null) return _fallback;
        var text = new TextObject("{=" + _id + "}" + _fallback);
        foreach (var variable in _variables)
            text.SetTextVariable(variable.Name, variable.Value is MemoryFailureUiText nested
                ? nested.Resolve() : Convert.ToString(variable.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "");
        return text.ToString();
    }
}

internal sealed class MemoryFailureDisplay
{
    internal readonly MemoryFailureUiText Title, Message;
    internal MemoryFailureDisplay(MemoryFailureUiText title, MemoryFailureUiText message)
    { Title = title; Message = message; }
}
