using System.Text.RegularExpressions;
using TaleWorlds.Localization;

namespace AnimusForge.DialogueUI;

internal static class DialogueUiLocalization
{
    private static readonly Regex LiteralSlots = new Regex("AFDUILITERALVALUE[123]", RegexOptions.CultureInvariant);

    internal static string Text(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

    internal static string Text(string id, string fallback, string variable, string value)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable(variable, "AFDUILITERALVALUE1");
        return RenderLiteralValues(text, value);
    }

    internal static string Text(string id, string fallback, string variable1, string value1, string variable2, string value2)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable(variable1, "AFDUILITERALVALUE1");
        text.SetTextVariable(variable2, "AFDUILITERALVALUE2");
        return RenderLiteralValues(text, value1, value2);
    }

    internal static string Text(string id, string fallback, string variable1, string value1, string variable2, string value2, string variable3, string value3)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable(variable1, "AFDUILITERALVALUE1");
        text.SetTextVariable(variable2, "AFDUILITERALVALUE2");
        text.SetTextVariable(variable3, "AFDUILITERALVALUE3");
        return RenderLiteralValues(text, value1, value2, value3);
    }

    // Names and already-rendered values are data, not another TextObject template.
    // A single replacement pass also preserves values containing another slot or $1.
    private static string RenderLiteralValues(TextObject text, string value1, string value2 = null, string value3 = null)
        => LiteralSlots.Replace(text.ToString(), match =>
            (match.Value[match.Value.Length - 1] == '1' ? value1 :
             match.Value[match.Value.Length - 1] == '2' ? value2 : value3) ?? "");
}
