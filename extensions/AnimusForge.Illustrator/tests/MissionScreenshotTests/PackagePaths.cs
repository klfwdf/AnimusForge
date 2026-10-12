using System;
using System.IO;

namespace AnimusForge
{
    internal static class AnimusForgeModulePaths
    {
        internal static string GetModuleDataFilePath(string file)
            => Path.Combine(AppContext.BaseDirectory, "ModuleData", Path.GetFileName(file));
    }
}