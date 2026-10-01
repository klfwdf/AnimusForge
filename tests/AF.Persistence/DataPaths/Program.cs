using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                // Credential-free synthetic profiles can have no Windows KnownFolder binding.
                // The production contract must reject, never fall back to relative/user data.
                bool unavailableRejected = false;
                try { AnimusForgeDataPaths.ResolveRoot(); }
                catch (InvalidOperationException ex)
                {
                    unavailableRejected = ex.Message == "LocalAppData is unavailable; AnimusForge user data has no safe fallback.";
                }
                Check(unavailableRejected, "unavailable LocalAppData fails closed with the precise cause");
                Console.WriteLine("NOT-RUN default LocalAppData root: isolated profile has no KnownFolder binding; fail-closed behavior verified");
            }
            else
            {
                string expected = Path.Combine(localAppData, "AnimusForge");
                Check(AnimusForgeDataPaths.ResolveRoot() == expected, "default LocalAppData root");
            }

            string isolated = isolatedRoot();
            Check(AnimusForgeDataPaths.ResolveRoot(isolated) == isolated, "explicit absolute root");
            Check(AnimusForgeDataPaths.GetPlayerExportsDirectory(isolated) == Path.Combine(isolated, "UserData", "PlayerExports"), "PlayerExports ownership");
            Check(AnimusForgeDataPaths.GetOverridePath(isolated, "CustomPrompts/Policy/Effects/_Common.json")
                == Path.Combine(isolated, "UserData", "Overrides", "CustomPrompts", "Policy", "Effects", "_Common.json"), "override path");
            Check(AnimusForgeDataPaths.GetCacheDirectory(isolated) == Path.Combine(isolated, "Cache"), "cache ownership");
            Check(AnimusForgeDataPaths.GetLogsDirectory(isolated) == Path.Combine(isolated, "Logs"), "logs ownership");
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

        string settingsPath = Path.Combine(workspace, "artifacts", "tests", "af2-terminal-settings", Guid.NewGuid().ToString("N"), "TerminalSettings.json");
        Check(AnimusForgeTerminalSettings.GetSettingsPath() == Path.Combine(isolatedRoot(), "UserData", "Settings", "TerminalSettings.json"), "TerminalSettings uses typed user path");
        Check(!AnimusForgeTerminalSettings.TryLoadSettingsFile(settingsPath, out _), "missing TerminalSettings uses built-in defaults");
        AnimusForgeTerminalSettings.TrySaveSettingsFile(settingsPath, new AnimusForgeTerminalSettingsData { IsHotkeyEnabled = false, IsMapIconEnabled = true });
        Check(AnimusForgeTerminalSettings.TryLoadSettingsFile(settingsPath, out var terminal) && !terminal.IsHotkeyEnabled && terminal.IsMapIconEnabled, "TerminalSettings user file round trip");
        Check(Directory.GetFiles(Path.GetDirectoryName(settingsPath), ".afp-*").Length == 0, "TerminalSettings candidate published");
        File.WriteAllText(settingsPath, "{broken");
        Reject(() => AnimusForgeTerminalSettings.TrySaveSettingsFile(settingsPath, new AnimusForgeTerminalSettingsData()), "corrupt TerminalSettings cannot be overwritten");
        Check(File.ReadAllText(settingsPath) == "{broken", "corrupt TerminalSettings preserved");

        string module = Path.Combine(workspace, "artifacts", "tests", "af2-model-store", Guid.NewGuid().ToString("N"), "AnimusForge");
        string onnx = Path.Combine(module, "ONNX");
        string nested = Path.Combine(onnx, "onnx");
        string reranker = Path.Combine(onnx, "reranker");
        string userRoot = Path.Combine(Path.GetDirectoryName(module), "user-root");
        string userModels = Path.Combine(userRoot, "Models");
        Directory.CreateDirectory(nested);
        Directory.CreateDirectory(reranker);
        Directory.CreateDirectory(userModels);
        string userEmbedding = Path.Combine(userModels, "embedding");
        Directory.CreateDirectory(userEmbedding);
        File.WriteAllText(Path.Combine(userEmbedding, "model.onnx"), "user-only");
        File.WriteAllText(Path.Combine(userEmbedding, "tokenizer.json"), "{}");
        File.WriteAllText(Path.Combine(userEmbedding, "config.json"), "{}");
        string userReceipt = Path.Combine(userModels, ".af-models-ready.json");
        File.WriteAllText(userReceipt, "{\"schema\":1,\"groups\":{\"embedding\":{}}}");
        string oldCompletion = Path.Combine(userRoot, "Recovery", "models-old", "completed.json");
        Directory.CreateDirectory(Path.GetDirectoryName(oldCompletion));
        File.WriteAllText(oldCompletion, "{\"schema\":1}");
        Reject(() => AnimusForgeModelStore.ResolveEmbedding(module), "M2 user model and receipt cannot replace missing module model");
        string tokenizer = Path.Combine(onnx, "tokenizer.json");
        string config = Path.Combine(onnx, "config.json");
        File.WriteAllText(tokenizer, "{}");
        File.WriteAllText(config, "{}");
        string rootQuantized = Path.Combine(onnx, "model_quantized.onnx");
        string nestedQuantized = Path.Combine(nested, "model_quantized.onnx");
        File.WriteAllText(rootQuantized, "root-quantized");
        File.WriteAllText(nestedQuantized, "nested-quantized");
        AnimusForgeModulePaths.TestModuleRoot = module;
        var selected = AnimusForgeModelStore.ResolveEmbedding();
        Check(selected.ModelPath == nestedQuantized && selected.TokenizerPath == tokenizer && selected.ConfigPath == config,
            "M1/M4 active module nested quantized candidate wins");
        string priorDataRoot = Environment.GetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, userRoot);
            Check(AnimusForgeModelStore.ResolveEmbedding().ModelPath == nestedQuantized,
                "M3 data-root override cannot change model source");
        }
        finally { Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, priorDataRoot); }
        File.Delete(nestedQuantized);
        Check(AnimusForgeModelStore.ResolveEmbedding(module).ModelPath == rootQuantized, "M4 root quantized fallback");
        File.Delete(rootQuantized);
        string nestedModel = Path.Combine(nested, "model.onnx");
        File.WriteAllText(nestedModel, "nested-unquantized");
        Reject(() => AnimusForgeModelStore.ResolveEmbedding(module), "M4 nonquantized external data required");
        File.WriteAllText(nestedModel + "_data", "external-data");
        Check(AnimusForgeModelStore.ResolveEmbedding(module).ModelPath == nestedModel, "M4 nested unquantized and sidecar");
        File.Delete(nestedModel);
        File.Delete(nestedModel + "_data");
        string rootModel = Path.Combine(onnx, "model.onnx");
        File.WriteAllText(rootModel, "root-unquantized");
        File.WriteAllText(rootModel + "_data", "external-data");
        Check(AnimusForgeModelStore.ResolveEmbedding(module).ModelPath == rootModel, "M4 root unquantized candidate");
        File.Delete(tokenizer);
        Reject(() => AnimusForgeModelStore.ResolveEmbedding(module), "M5 missing tokenizer blocks required model");
        File.WriteAllText(tokenizer, "{}");
        File.WriteAllText(config, "{broken");
        Reject(() => AnimusForgeModelStore.ResolveEmbedding(module), "M5 corrupt config blocks required model");
        File.WriteAllText(config, "{}");
        Check(AnimusForgeModelStore.ResolveEmbedding(module).ModelPath == rootModel,
            "M5 repaired module dependency is available after restart");
        bool lockedRejected = false;
        using (new FileStream(rootModel, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            try { AnimusForgeModelStore.ResolveEmbedding(module); }
            catch (IOException) { lockedRejected = true; }
        }
        Check(lockedRejected, "M5 locked model file cannot pass as readable");
        File.WriteAllText(Path.Combine(reranker, "tokenizer.json"), "{}");
        string rerankerModel = Path.Combine(reranker, "model.onnx");
        string rerankerQuantized = Path.Combine(reranker, "model_quantized.onnx");
        File.WriteAllText(rerankerModel, "reranker-default");
        File.WriteAllText(rerankerQuantized, "reranker-quantized");
        Check(AnimusForgeModelStore.ResolveReranker(module).ModelPath == rerankerQuantized, "M4 reranker quantized candidate");
        File.Delete(rerankerQuantized);
        Check(AnimusForgeModelStore.ResolveReranker(module).ModelPath == rerankerModel, "M4 reranker regular candidate");
        File.Delete(rerankerModel);
        Reject(() => AnimusForgeModelStore.ResolveReranker(module), "M4 absent optional reranker is unavailable");
        Check(AnimusForgeModelStore.ResolveEmbedding(module).ModelPath == rootModel,
            "M4 optional reranker absence does not change required embedding");
        File.WriteAllText(userReceipt, "{broken");
        File.Delete(oldCompletion);
        File.SetLastWriteTimeUtc(rootModel, DateTime.UtcNow.AddDays(-7));
        Check(AnimusForgeModelStore.ResolveEmbedding(module).ModelPath == rootModel,
            "M6 corrupt receipt, absent Recovery and changed model mtime do not gate module model");
        Check(File.ReadAllText(userReceipt) == "{broken",
            "M6 corrupt legacy receipt remains untouched");
        AnimusForgeModulePaths.TestModuleRoot = null;

        Console.WriteLine("PASS data-path checks=" + _checks);
    }

    private static string isolatedRoot()
    {
        string root = Environment.GetEnvironmentVariable("AF_DATA_PATHS_TEST_ROOT");
        if (string.IsNullOrWhiteSpace(root)) return Path.Combine(Path.GetTempPath(), "af-f1-isolated-root");
        if (!Path.IsPathRooted(root) || Directory.Exists(root) || File.Exists(root))
            throw new InvalidOperationException("AF_DATA_PATHS_TEST_ROOT must be an unused absolute path.");
        return Path.GetFullPath(root);
    }
}

namespace AnimusForge
{
    internal static class AnimusForgeModulePaths
    {
        internal static string TestModuleRoot;
        public static string GetCurrentModuleRoot() => TestModuleRoot;
    }

    internal static class Logger
    {
        internal static void Log(string category, string message) { }
    }
}
