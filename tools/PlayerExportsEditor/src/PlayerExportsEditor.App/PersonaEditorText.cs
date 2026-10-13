using System.Globalization;
using System.Resources;

namespace PlayerExportsEditor.App;

internal static class PersonaEditorText
{
    private static readonly ResourceManager Resources = new("PlayerExportsEditor.App.PersonaEditorText", typeof(PersonaEditorText).Assembly);
    internal static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    internal static string Format(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, Get(key), args);
}
