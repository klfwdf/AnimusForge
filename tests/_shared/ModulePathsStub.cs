using System;
using System.IO;

namespace AnimusForge
{
    // Minimal module path stub for tests that link PromptLanguage/BilingualTextTable but read no ModuleData.
    internal static class AnimusForgeModulePaths
    {
        internal static string GetModuleDataFilePath(string fileName)
            => Path.Combine(AppContext.BaseDirectory, "ModuleData", Path.GetFileName(fileName ?? ""));
    }
}
