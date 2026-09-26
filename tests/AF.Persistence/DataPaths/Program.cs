using System;
using System.Collections.Generic;
using System.IO;
using AnimusForge;

internal static class Program
{
    private static int _checks;
    private sealed class BrokenRow { public string Value => throw new InvalidOperationException("synthetic serialization failure"); }

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
        catch (Newtonsoft.Json.JsonReaderException) { rejected = true; }
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
            Check(PlayerExportsStore.GetPlayerExportsRootPath() == Path.Combine(isolated, "UserData", "PlayerExports"), "game export root uses shared user-data contract");
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

        string workspace = Directory.GetCurrentDirectory();
        Check(File.Exists(Path.Combine(workspace, "AnimusForge.csproj")), "test writes only inside workspace");
        string fixture = Path.Combine(workspace, "artifacts", "tests", "af2-data-paths", "atomic-" + Guid.NewGuid().ToString("N") + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(fixture));
        try
        {
            File.WriteAllText(fixture, "{\"old\":true}");
            bool failed = false;
            try { PlayerExportsStore.WriteJson(fixture, new BrokenRow()); }
            catch (Exception) { failed = true; }
            Check(failed, "serialization failure reported");
            Check(File.ReadAllText(fixture) == "{\"old\":true}", "failed write preserves existing export");
            PlayerExportsStore.WriteJson(fixture, new { Value = 2 });
            Check(PlayerExportsStore.ReadJson<Dictionary<string, int>>(fixture)["Value"] == 2, "validated candidate replaces existing export");
            Check(Directory.GetFiles(Path.GetDirectoryName(fixture), "." + Path.GetFileName(fixture) + ".*.tmp").Length == 0, "candidate removed after publication");
        }
        finally
        {
            if (File.Exists(fixture)) File.Delete(fixture);
        }

        string exportFixture = Path.Combine(workspace, "artifacts", "tests", "af2-export-package", Guid.NewGuid().ToString("N"));
        string exportRoot = Path.Combine(exportFixture, "PlayerExports");
        string recovery = Path.Combine(exportFixture, "Recovery");
        string existing = Path.Combine(exportRoot, "demo", "event_data", "old.json");
        Directory.CreateDirectory(Path.GetDirectoryName(existing));
        File.WriteAllText(existing, "{\"old\":true}");
        string oldKnowledge = Path.Combine(exportRoot, "demo", "knowledge", "rules.json");
        Directory.CreateDirectory(Path.GetDirectoryName(oldKnowledge));
        File.WriteAllText(oldKnowledge, "{\"prior\":true}");
        var package = PlayerExportsPackageExport.Begin(exportRoot, "demo", recovery);
        string candidateDir = Path.Combine(package.CandidatePath, "event_data");
        Reject(() => PlayerExportsStore.ClearCandidateJsonFiles(Path.GetDirectoryName(existing)), "active JSON set cannot be cleared by candidate API");
        PlayerExportsStore.ClearCandidateJsonFiles(candidateDir);
        PlayerExportsStore.WriteJson(Path.Combine(candidateDir, "new.json"), new { New = true });
        File.WriteAllText(Path.Combine(package.CandidatePath, "knowledge", "rules.json"), "{\"partial\":true}");
        package.RestoreSubdirectory("knowledge");
        Check(File.ReadAllText(Path.Combine(package.CandidatePath, "knowledge", "rules.json")) == "{\"prior\":true}", "failed optional section restores prior candidate bytes");
        Reject(() => package.RestoreSubdirectory("../demo"), "section restore rejects traversal");
        Check(File.Exists(existing) && !File.Exists(Path.Combine(exportRoot, "demo", "event_data", "new.json")), "group candidate leaves old package active");
        package.Publish();
        Check(!File.Exists(existing) && File.Exists(Path.Combine(exportRoot, "demo", "event_data", "new.json")), "group publication replaces old JSON set");
        Check(File.ReadAllText(oldKnowledge) == "{\"prior\":true}", "failed optional section leaves prior active bytes");
        Check(Directory.GetFiles(recovery, "old.json", SearchOption.AllDirectories).Length == 1, "verified old package remains in private recovery");
        Check(Directory.GetDirectories(exportRoot, ".af-export-retired.*").Length == 0, "verified retired copy removed after backup");
        Check(PlayerExportsStore.FindLatestExportFolder(exportRoot) == Path.Combine(exportRoot, "demo"), "hidden candidates excluded from latest import");

        string guarded = Path.Combine(exportRoot, "guarded", "value.json");
        Directory.CreateDirectory(Path.GetDirectoryName(guarded));
        File.WriteAllText(guarded, "{\"keep\":1}");
        var malformed = PlayerExportsPackageExport.Begin(exportRoot, "guarded", recovery);
        File.WriteAllText(Path.Combine(malformed.CandidatePath, "value.json"), "{broken");
        Reject(() => malformed.Publish(), "malformed candidate rejected");
        Check(File.ReadAllText(guarded) == "{\"keep\":1}", "malformed group leaves old active");

        var raced = PlayerExportsPackageExport.Begin(exportRoot, "guarded", recovery);
        File.WriteAllText(guarded, "{\"newer\":2}");
        Reject(() => raced.Publish(), "concurrent edit rejected");
        Check(File.ReadAllText(guarded) == "{\"newer\":2}", "concurrent edit not overwritten");
        Check(!Path.GetFileName(PlayerExportsStore.FindLatestExportFolder(exportRoot)).StartsWith(".", StringComparison.Ordinal), "failed hidden candidates excluded from latest import");

        string blockedRecovery = Path.Combine(exportFixture, "blocked-recovery");
        File.WriteAllText(blockedRecovery, "not a directory");
        var noBackup = PlayerExportsPackageExport.Begin(exportRoot, "guarded", blockedRecovery);
        bool backupFailed = false;
        try { noBackup.Publish(); }
        catch (IOException) { backupFailed = true; }
        Check(backupFailed && File.ReadAllText(guarded) == "{\"newer\":2}", "unwritable recovery cannot publish over old package");

        string legacyModule = Path.Combine(workspace, "artifacts", "tests", "af2-data-paths", "legacy-" + Guid.NewGuid().ToString("N"));
        string legacyFile = Path.Combine(legacyModule, "PlayerExports", "demo", "old.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyFile));
        File.WriteAllText(legacyFile, "{\"old\":true}");
        string pendingRoot = Path.Combine(Path.GetTempPath(), "af2-pending-" + Guid.NewGuid().ToString("N"));
        Reject(() => PlayerExportsStore.EnsureLegacyMigrationReady(legacyModule, pendingRoot), "detected legacy data blocks an unready user root");
        string marker = Path.Combine(legacyModule, "UserData", ".player-exports-ready.json");
        Directory.CreateDirectory(Path.GetDirectoryName(marker));
        string moduleKey = PlayerExportsStore.ComputeModuleRootKey(legacyModule);
        if (Path.DirectorySeparatorChar == '\\')
            Check(PlayerExportsStore.ComputeModuleRootKey(@"C:\Games\AnimusForge") == "341e25efcc78c23c0ae0dd148a422cbd8cbcd7746d6307246c90ab2bea29bf79", "C# migration key normalization");
        string manifestHash = new string('a', 64);
        File.WriteAllText(marker, "{\"schema\":1,\"sources\":{\"" + moduleKey + "\":\"" + manifestHash + "\"}}");
        Reject(() => PlayerExportsStore.VerifyMigrationMarker(legacyModule, marker), "ready pointer without completed recovery cannot permit cutover");
        string completed = Path.Combine(legacyModule, "Recovery", "player-exports-" + manifestHash.Substring(0, 24), "completed.json");
        Directory.CreateDirectory(Path.GetDirectoryName(completed));
        File.WriteAllText(completed, "{\"schema\":1,\"manifestSha256\":\"" + manifestHash + "\"}");
        PlayerExportsStore.VerifyMigrationMarker(legacyModule, marker);
        Check(true, "matching verified migration record permits cutover");
        File.WriteAllText(marker, "{\"schema\":1,\"sources\":{\"different\":\"" + manifestHash + "\"}}");
        Reject(() => PlayerExportsStore.VerifyMigrationMarker(legacyModule, marker), "unrelated migration record cannot permit cutover");
        File.WriteAllText(marker, "{broken");
        Reject(() => PlayerExportsStore.VerifyMigrationMarker(legacyModule, marker), "corrupt migration record cannot permit cutover");

        Console.WriteLine("PASS data-path checks=" + _checks);
    }
}
