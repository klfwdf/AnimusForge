using TaleWorlds.Localization;

namespace AnimusForge.DialogueUI;

internal static class DialogueUiLocalization
{
    internal static string Text(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

    internal static string Text(string id, string fallback, string variable, string value)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable(variable, value ?? "");
        return text.ToString();
    }

    internal static string Text(string id, string fallback, string variable1, string value1, string variable2, string value2)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable(variable1, value1 ?? "");
        text.SetTextVariable(variable2, value2 ?? "");
        return text.ToString();
    }

    internal static string Text(string id, string fallback, string variable1, string value1, string variable2, string value2, string variable3, string value3)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable(variable1, value1 ?? "");
        text.SetTextVariable(variable2, value2 ?? "");
        text.SetTextVariable(variable3, value3 ?? "");
        return text.ToString();
    }
}
