using System;
using System.IO;

namespace AnimusForge;

// Only the module filesystem boundary is replaced. The project links the production
// WeeklyMaterialTexts reader and supplies real package-selected CN/EN prompt files.
// Run each language in a separate process because the production reader caches once.
internal static class WeeklyPromptFixture
{
    internal static bool English { get; private set; }

    internal static void Select(string[] args)
    {
        if (args.Length == 0 || args.Length == 1 && args[0] == "cn") English = false;
        else if (args.Length == 1 && args[0] == "en") English = true;
        else throw new ArgumentException("Use cn or en to select the prompt package fixture.");
    }
}

internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string filename)
        => Path.Combine(AppContext.BaseDirectory, "prompt-fixtures", WeeklyPromptFixture.English ? "en" : "cn", "ModuleData", filename);
}
