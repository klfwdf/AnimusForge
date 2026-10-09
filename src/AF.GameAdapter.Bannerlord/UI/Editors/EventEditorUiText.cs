using TaleWorlds.Core;

namespace AnimusForge;

internal static class EventEditorUiText
{
    internal static string T(string id, string englishFallback, params (string Name, string Value)[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                text.SetTextVariable(variable.Name, variable.Value ?? string.Empty);
            }
        }
        return text.ToString();
    }
}
