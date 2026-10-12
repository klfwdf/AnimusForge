using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AnimusForge;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL " + name);
        checks++;
    }
    private static void Reject(Action action, string name)
    {
        try { action(); }
        catch (ArgumentException) { checks++; return; }
        catch (InvalidOperationException) { checks++; return; }
        catch (IOException) { checks++; return; }
        throw new Exception("FAIL expected rejection " + name);
    }
    private static string Module(string parent, string id = "AnimusForge")
    {
        string root = Path.Combine(parent, "AnimusForge");
        Directory.CreateDirectory(Path.Combine(root, "ModuleData"));
        File.WriteAllText(Path.Combine(root, "SubModule.xml"), "<Module><Id value=\"" + id + "\" /></Module>");
        return root;
    }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void TestRetiredMigration(string root, string fixture)
    {
        string legacy = Path.Combine(fixture, "legacy", "exports");
        string destination = Path.Combine(root, "PlayerExports", "Custom");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(destination);
        string old = Path.Combine(legacy, "unknown{VALUE}$1.json");
        File.WriteAllText(old, "{\"unknown\":\"literal {NAME} $1 中文\",\"counter\":9001}");
        string existing = Path.Combine(destination, "conflict.json");
        File.WriteAllText(existing, "{\"moduleUser\":true}");
        string sourceBefore = Hash(old), existingBefore = Hash(existing);
        DateTime sourceTime = File.GetLastWriteTimeUtc(old);
        DateTime timestamp = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Directory.SetLastWriteTimeUtc(destination, timestamp);
        AnimusForgeDataPaths.MigrateLegacyDirectoryOnce(root, destination, legacy);
        Check(!File.Exists(Path.Combine(destination, Path.GetFileName(old))), "retired migration never imports legacy files");
        Check(Hash(old) == sourceBefore && File.GetLastWriteTimeUtc(old) == sourceTime, "legacy bytes and timestamp untouched");
        Check(Hash(existing) == existingBefore, "existing module user wins");
        Check(Directory.GetLastWriteTimeUtc(destination) == timestamp, "existing package ordering timestamp preserved");
        Check(!Directory.Exists(Path.Combine(root, "Recovery", "RuntimeStorage")), "retired migration writes no recovery receipt");
        using (new FileStream(old, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            AnimusForgeDataPaths.MigrateLegacyDirectoryOnce(root, destination, legacy);
            Check(!AnimusForgeDataPaths.CopyLegacyFileIfMissing(root, old, Path.Combine(destination, "copy.json")), "retired copy neither reads locked source nor reports success");
        }
        AnimusForgeDataPaths.MigrateLegacyDirectoryOnce(root, destination, @"\\unavailable\legacy");
        Check(!AnimusForgeDataPaths.CopyLegacyFileIfMissing(root, @"\\unavailable\legacy.json", Path.Combine(destination, "missing.json")), "retired helpers do not probe unavailable sources");
        Reject(() => AnimusForgeDataPaths.CopyLegacyFileIfMissing(root, old, Path.Combine(fixture, "escape.json")), "copy destination guard");
        Check(Directory.GetFiles(destination, ".af-migration-*").Length == 0, "retired migration creates no candidates");
        string blocked = Path.Combine(root, "Configs", "blocked");
        Directory.CreateDirectory(Path.GetDirectoryName(blocked)!);
        File.WriteAllText(blocked, "not a directory");
        Reject(() => AnimusForgeDataPaths.MigrateLegacyDirectoryOnce(root, Path.Combine(blocked, "child"), legacy), "retired migration retains target validation");
        Reject(() => AnimusForgeDataPaths.MigrateLegacyDirectoryOnce(root, Path.Combine(blocked, "child"), legacy), "invalid target remains rejected");
        Check(File.ReadAllText(blocked) == "not a directory", "blocked original preserved");
    }
    private static void Main(string[] args)
    {
        string fixture = Path.GetFullPath(args[0]);
        if (!fixture.StartsWith(Path.GetFullPath(Directory.GetCurrentDirectory()) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fixtures must remain inside the calling output directory.");
        // Red replay exercises only explicit workspace sources, never live AppData/Documents.
        if (args.Contains("--no-legacy-only"))
        {
            TestRetiredMigration(Module(Path.Combine(fixture, "active")), fixture);
            Console.WriteLine("PASS retired legacy checks=" + checks);
            return;
        }
        Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, null);
        Reject(() => AnimusForgeDataPaths.ResolveRoot(), "missing module never uses CWD/AppData/TEMP");
        string root = Module(Path.Combine(fixture, "active"));
        Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, root);
        Check(AnimusForgeDataPaths.GetCurrentRoot() == root, "explicit standalone module is active");
        Check(AnimusForgeDataPaths.GetPlayerExportsDirectory(root) == Path.Combine(root, "PlayerExports"), "exports purpose");
        Check(AnimusForgeDataPaths.GetSettingsDirectory(root) == Path.Combine(root, "Configs"), "configs purpose");
        Check(AnimusForgeDataPaths.GetOverridePath(root, "CustomPrompts/Policy/Effects/x.json") == Path.Combine(root, "CustomPrompts", "Policy", "Effects", "x.json"), "custom purpose");
        Check(AnimusForgeDataPaths.GetCacheDirectory(root) == Path.Combine(root, "Cache"), "cache purpose");
        Check(AnimusForgeDataPaths.GetLogsDirectory(root) == Path.Combine(root, "Logs"), "logs purpose");
        Check(AnimusForgeDataPaths.GetRecoveryDirectory(root) == Path.Combine(root, "Recovery"), "recovery purpose");
        Check(AnimusForgeDataPaths.GetPackagedCustomPromptsDirectory(root) == Path.Combine(root, "ModuleData", "Defaults", "CustomPrompts"), "readonly defaults separate");
        Reject(() => AnimusForgeDataPaths.ResolveRoot("relative"), "relative root");
        Reject(() => AnimusForgeDataPaths.ResolveRoot(Path.GetPathRoot(root)), "volume root");
        Reject(() => AnimusForgeDataPaths.ResolveRoot(Module(Path.Combine(fixture, "wrong"), "OtherMod")), "wrong identity");
        Reject(() => AnimusForgeDataPaths.ResolveRoot(Module(Path.Combine(fixture, "single_module_stage"))), "stage");
        Reject(() => AnimusForgeDataPaths.GetOverridePath(root, "../escape.json"), "traversal");
        Reject(() => AnimusForgeDataPaths.GetOverridePath(root, "x.json:stream"), "ADS");
        string sourceParent = Path.Combine(fixture, "source");
        string sourceModule = Module(sourceParent);
        File.WriteAllText(Path.Combine(sourceParent, "AnimusForge.csproj"), "<Project />");
        Reject(() => AnimusForgeDataPaths.ResolveRoot(sourceModule), "source distribution tree");
        Check(AnimusForgeDataPaths.FindModuleRootFromDirectory(Path.Combine(root, "bin", "Win64_Shipping_Client", "versions", "1.4")) == null,
            "automatic discovery cannot silently select the surrounding checkout");

        Check(AnimusForgeDataPaths.GetLegacyDataPath("UserData/PlayerExports") == null, "old AppData root is unavailable");
        Check(AnimusForgeDataPaths.GetLegacyDocumentsPath("Mount and Blade II Bannerlord/Configs") == null, "old Documents root is unavailable");
        TestRetiredMigration(root, fixture);
        string candidateLink = Path.Combine(fixture, "link-container", "AnimusForge");
        Directory.CreateDirectory(Path.GetDirectoryName(candidateLink)!);
        try
        {
            Directory.CreateSymbolicLink(candidateLink, root);
            Reject(() => AnimusForgeDataPaths.ResolveRoot(Path.Combine(candidateLink, "..", "link")), "reparse root");
        }
        catch (UnauthorizedAccessException) { Console.WriteLine("NOT-RUN reparse creation: OS privilege unavailable"); }
        catch (IOException ex) when ((ex.HResult & 0xffff) == 1314) { Console.WriteLine("NOT-RUN reparse creation: OS privilege 1314 unavailable"); }
        catch (PlatformNotSupportedException) { Console.WriteLine("NOT-RUN reparse creation: OS unsupported"); }
        Console.WriteLine("PASS runtime storage pure IO checks=" + checks);
    }
}
