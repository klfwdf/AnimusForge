namespace AnimusForge;

// Fixture filesystem boundary. The actual production reader loads the copied, unmodified catalogs.
internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string file) => Path.Combine(AppContext.BaseDirectory,file);
}
