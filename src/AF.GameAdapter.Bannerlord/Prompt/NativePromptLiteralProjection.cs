using System;
using TaleWorlds.Localization;

namespace AnimusForge;

// Request boundary only. Do not change the game's UI language or mutate the source TextObject.
internal static class NativePromptLiteralProjection
{
    internal static string Render(TextObject text)
    {
        if (text == null) return "";
        if (PromptComposerTexts.Get("native_text_language") == "en")
        {
            string source = text.Value ?? "";
            if (source.StartsWith("{=", StringComparison.Ordinal))
            {
                int closing = source.IndexOf('}');
                string literal = closing < 0 ? "" : source.Substring(closing + 1);
                // Only native literal fallbacks are safe here. User names and parameterized
                // TextObjects keep their original renderer and variables.
                if (literal.Length > 0 && literal.IndexOf('{') < 0 && literal.IndexOf('}') < 0)
                    return literal;
            }
        }
        return text.ToString();
    }
}
