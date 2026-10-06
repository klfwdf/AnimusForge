using System;
using System.IO;

namespace AnimusForge.Illustrator.Engine
{
    internal static class IllustratorStoragePaths
    {
        private static readonly string ModuleRoot = ResolveModuleRoot();
        internal static string ImageDirectory => Path.Combine(ModuleRoot, "logs", "image save");
        internal static string TempDirectory => Path.Combine(ModuleRoot, "Cache", "Illustrator", "Temp");
        internal static string ConfigDirectory => Path.Combine(ModuleRoot, "Configs", "Illustrator");
        internal static string ApiTestDirectory => Path.Combine(ModuleRoot, "Cache", "Illustrator", "ApiTest");

        internal static string EnsureDirectory(string path)
        {
            var full = Path.GetFullPath(path);
            if (!full.StartsWith(ModuleRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Illustrator storage must remain inside the active module.");
            Directory.CreateDirectory(full);
            return full;
        }

        private static string ResolveModuleRoot()
        {
            var root = AnimusForgeModulePaths.GetCurrentModuleRoot();
            if (string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root))
                throw new IOException("The active AnimusForge module directory is unavailable.");
            return Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
