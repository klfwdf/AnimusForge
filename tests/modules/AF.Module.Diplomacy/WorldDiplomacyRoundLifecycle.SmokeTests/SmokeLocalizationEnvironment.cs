using System.Xml;
using TaleWorlds.Localization;

namespace AnimusForge;

// Test package path only. Production JSON readers and real TaleWorlds TextObject
// are compiled unchanged; there is no fake localization reader or UI leaf.
internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string name) => Path.Combine(
        Environment.GetEnvironmentVariable("AF_SMOKE_PACKAGE_ROOT") ?? Path.Combine(AppContext.BaseDirectory, "ModuleData"), name);
}

internal static class SmokeLocalizationEnvironment
{
    internal static bool EnglishPackage => Environment.GetEnvironmentVariable("AF_SMOKE_PACKAGE_LANGUAGE") == "en";
    internal static bool EnglishUi => Environment.GetEnvironmentVariable("AF_SMOKE_UI_LANGUAGE") == "English";
    internal static string PackageText(string key) => DiplomacyMiscPromptTexts.Get(key, "");
    internal static void Initialize()
    {
        string module = Environment.GetEnvironmentVariable("AF_SMOKE_UI_MODULE");
        if (string.IsNullOrEmpty(module)) return; // Non-UI legacy runners keep the game's English fallback.
        LocalizedTextManager.LoadLocalizationXmls(new[] { module });
        string language = Environment.GetEnvironmentVariable("AF_SMOKE_UI_LANGUAGE") ?? "English";
        if (!MBTextManager.ChangeLanguage(language)) throw new InvalidOperationException("Smoke UI language was not loaded: " + language);
        string actual = DiplomacyUiLocalization.Text("AF_DIP_STATUS_OralDiplomacy_Executed", "Official declaration published and diplomatic action executed.");
        if (language == "English" ? actual != "Official declaration published and diplomatic action executed." : actual != "正式宣言已发布，外交行动已生效。") throw new InvalidOperationException("Actual TaleWorlds UI resource failed to load.");
    }
}
