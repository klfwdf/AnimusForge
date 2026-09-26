using System;
using System.IO;
using AnimusForge;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (!condition) throw new Exception("FAIL: " + name);
    }

    private static void Reject(Action action, string name)
    {
        bool rejected = false;
        try { action(); }
        catch (ArgumentException) { rejected = true; }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, name);
    }

    private static void Main()
    {
        string previous = Environment.GetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, null);
            string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnimusForge");
            Check(AnimusForgeDataPaths.ResolveRoot() == expected, "default LocalAppData root");

            string isolated = Path.Combine(Path.GetTempPath(), "af-f1-isolated-root");
            Check(AnimusForgeDataPaths.ResolveRoot(isolated) == isolated, "explicit absolute root");
            Check(AnimusForgeDataPaths.GetPlayerExportsDirectory(isolated) == Path.Combine(isolated, "UserData", "PlayerExports"), "PlayerExports ownership");
            Check(AnimusForgeDataPaths.GetOverridePath(isolated, "CustomPrompts/Policy/Effects/_Common.json")
                == Path.Combine(isolated, "UserData", "Overrides", "CustomPrompts", "Policy", "Effects", "_Common.json"), "override path");
            Check(AnimusForgeDataPaths.GetCacheDirectory(isolated) == Path.Combine(isolated, "Cache"), "cache ownership");
            Check(AnimusForgeDataPaths.GetLogsDirectory(isolated) == Path.Combine(isolated, "Logs"), "logs ownership");
            Check(AnimusForgeDataPaths.GetModelsDirectory(isolated) == Path.Combine(isolated, "Models"), "models ownership");
            Check(AnimusForgeDataPaths.GetRecoveryDirectory(isolated) == Path.Combine(isolated, "Recovery"), "recovery ownership");

            Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, isolated);
            Check(AnimusForgeDataPaths.ResolveRoot() == isolated, "environment override");
            Reject(() => AnimusForgeDataPaths.ResolveRoot("relative-path"), "relative root rejected");
            Reject(() => AnimusForgeDataPaths.ResolveRoot(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "user-data")), "repository root rejected");
            Reject(() => AnimusForgeDataPaths.ResolveRoot(Path.Combine(Directory.GetCurrentDirectory(), "AnimusForge", "PlayerExports")), "module root rejected");
            Reject(() => AnimusForgeDataPaths.ResolveRoot(Path.Combine(Path.GetTempPath(), "single_module_stage", "AnimusForge", "data")), "stage root rejected");
            Reject(() => AnimusForgeDataPaths.ResolveRoot(Path.Combine(Directory.GetCurrentDirectory(), "AnimusForge.csproj")), "file root rejected");
            Reject(() => AnimusForgeDataPaths.GetOverridePath(isolated, "../PlayerExports/secrets.json"), "override traversal rejected");
            Reject(() => AnimusForgeDataPaths.GetOverridePath(isolated, "C:/absolute.json"), "absolute override rejected");
            Reject(() => AnimusForgeDataPaths.GetOverridePath(isolated, "prompt.json:stream"), "alternate stream rejected");
        }
        finally
        {
            Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, previous);
        }

        Console.WriteLine("PASS data-path checks=" + _checks);
    }
}
