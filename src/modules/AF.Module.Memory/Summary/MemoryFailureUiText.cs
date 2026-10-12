using System;
using TaleWorlds.Localization;

namespace AnimusForge;

// Detached display descriptions can travel with worker diagnostics without resolving game text.
internal sealed class MemoryFailureUiText
{
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFMEMORYFAILURELITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
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
            var _ when string.Equals(parseError?.Trim(), MemoryTextResources.Raw("MEM_Raw_116"), StringComparison.Ordinal) => T("AF_MEMORY_UI_SummaryEmpty", "SUMMARY is empty."),
            var _ when string.Equals(parseError?.Trim(), MemoryTextResources.Raw("MEM_Raw_117"), StringComparison.Ordinal) => T("AF_MEMORY_UI_TitleOrSummaryEmpty", "TITLE or SUMMARY is empty."),
            var _ when string.Equals(parseError?.Trim(), MemoryTextResources.Raw("MEM_Raw_077"), StringComparison.Ordinal) => T("AF_MEMORY_UI_JsonParseFailed", "JSON parsing failed."),
            var _ when string.Equals(parseError?.Trim(), MemoryTextResources.Raw("MEM_Raw_078"), StringComparison.Ordinal) => T("AF_MEMORY_UI_JsonObjectMissing", "No JSON object could be found."),
            null or "" => T("AF_MEMORY_UI_UnknownParseError", "Unknown parsing error."),
            _ => Raw(parseError.Trim())
        };
        string requiredFieldsPrefix = MemoryTextResources.Raw("MEM_Raw_076");
        if (parseError?.StartsWith(requiredFieldsPrefix, StringComparison.Ordinal) == true)
            detail = T("AF_MEMORY_UI_JsonRequiredFieldsMissing", "No JSON object contained the required fields: {FIELDS}.",
                ("FIELDS", RequiredFieldsForDisplay(parseError.Substring(requiredFieldsPrefix.Length).TrimEnd('。'))));
        return T("AF_MEMORY_UI_SummaryParseFailed", "Summary response format could not be parsed: {ERROR}\n\nFull model reply:\n{REPLY}",
            ("ERROR", detail), ("REPLY", string.IsNullOrWhiteSpace(reply) ? T("AF_MEMORY_UI_NoModelReply", "No model reply was received or could be parsed") : Raw(reply.Trim())));
    }

    private static MemoryFailureUiText RequiredFieldsForDisplay(string fields)
    {
        string none = MemoryTextResources.Raw("MEM_Raw_079");
        MemoryFailureUiText Group(string value) => value == none
            ? T("AF_MEMORY_UI_JsonEmptyFieldGroup", "(none)") : Raw(value);
        string[] groups = fields.Split(new[] { " + " }, StringSplitOptions.None);
        return groups.Length == 2
            ? T("AF_MEMORY_UI_JsonFieldGroups", "{PRIMARY} + {SECONDARY}",
                ("PRIMARY", Group(groups[0])), ("SECONDARY", Group(groups[1])))
            : Group(fields);
    }

    // Called only by UI publication/acceptance on the existing main-thread phase.
    internal string Resolve()
    {
        if (_id == null) return _fallback;
        var text = new TextObject("{=" + _id + "}" + _fallback);
        for (int i = 0; i < _variables.Length; i++)
            text.SetTextVariable(_variables[i].Name, "AFMEMORYFAILURELITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match =>
        {
            object value = _variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value;
            return value is MemoryFailureUiText nested ? nested.Resolve()
                : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        });
    }
}

internal sealed class MemoryFailureDisplay
{
    internal readonly MemoryFailureUiText Title, Message;
    internal MemoryFailureDisplay(MemoryFailureUiText title, MemoryFailureUiText message)
    { Title = title; Message = message; }
}
