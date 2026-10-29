namespace AnimusForge;
// Explicit package-path leaf; the real resource reader is compiled in this replay.
internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string name) => System.IO.Path.Combine(
        System.Environment.GetEnvironmentVariable("AF_MEMORY_PACKAGE_ROOT") ?? throw new System.InvalidOperationException("Select CN or EN resource root"), name);
}
