using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PolicyEffectModule.ContractTests;

internal static partial class Program
{
    private static void TestModuleRuntimeStorageContracts()
    {
        string root = Environment.GetEnvironmentVariable("AF_PROMPT_TEST_ROOT");
        string repository = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
        Check(!string.IsNullOrWhiteSpace(root) && Path.IsPathRooted(root)
            && Path.GetFullPath(root).StartsWith(Path.Combine(repository, "artifacts") + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) && !Directory.Exists(root), "new workspace-only runtime fixture required");
        string module = Path.Combine(root, "AnimusForge");
        Directory.CreateDirectory(Path.Combine(module, "ModuleData"));
        File.WriteAllText(Path.Combine(module, "SubModule.xml"), "<Module><Id value=\"AnimusForge\" /></Module>");
        PrepareFixtureModuleAssets(module);
        SetFixtureModuleRoot(module);
        string fakeUser = Path.Combine(root, "ignored-user-data");
        Directory.CreateDirectory(Path.Combine(fakeUser, "UserData", "Settings"));
        File.WriteAllText(Path.Combine(fakeUser, "UserData", "Settings", "TerminalSettings.json"), "{\"IsHotkeyEnabled\":false,\"IsMapIconEnabled\":true}");
        Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", fakeUser);
        Type settings = SutType("AnimusForge.DuelSettings");
        Type terminal = SutType("AnimusForge.AnimusForgeTerminalSettings");
        Type exports = SutType("AnimusForge.PlayerExportsStore");
        Type dataPaths = SutType("AnimusForge.AnimusForgeDataPaths");
        Check(InvokeStatic(dataPaths, "GetLegacyDataPath", new object[] { "UserData/PlayerExports" }, 1) == null,
            "actual module cannot resolve old AppData data");
        Check(InvokeStatic(dataPaths, "GetLegacyDocumentsPath", new object[] { "Mount and Blade II Bannerlord/Configs" }, 1) == null,
            "actual module cannot resolve old Documents data");
        string cache = Path.Combine(module, "Cache", "ModelDropdownCache.json");
        Check(string.Equals((string)InvokeStatic(settings, "GetModelDropdownCachePath", Array.Empty<object>(), 0), cache,
            StringComparison.OrdinalIgnoreCase), "cache belongs to user-approved module Cache");
        string terminalFile = Path.Combine(module, "Configs", "TerminalSettings.json");
        Check(terminal.GetField("_loaded", All) == null, "terminal has no second file-backed settings cache; shared MCM save/reload is covered by SharedMcmChecks");
        Check((string)InvokeStatic(exports, "GetPlayerExportsRootPath", Array.Empty<object>(), 0) == Path.Combine(module, "PlayerExports"), "exports belong to module");
        Directory.CreateDirectory(Path.Combine(module, "PlayerExports", "module-package"));
        Directory.CreateDirectory(Path.Combine(fakeUser, "UserData", "PlayerExports", "USER_ONLY"));
        string userPackage = Path.Combine(fakeUser, "UserData", "PlayerExports", "USER_ONLY");
        File.WriteAllText(Path.Combine(userPackage, "data.json"), "{\"fromUser\":true}");
        string importLink = Path.Combine(module, "PlayerExports", "USER_LINK");
        RunPromptFixtureCommand("cmd.exe", "/c mklink /J \"" + importLink + "\" \"" + userPackage + "\"");
        object folders = InvokeStatic(exports, "GetImportFolders", Array.Empty<object>(), 0);
        Check(((System.Collections.IEnumerable)folders).Cast<object>().Count() == 1, "production import list excludes user-data packages");
        Check(exports.GetMethod("ReadJson", All).MakeGenericMethod(typeof(JObject)).Invoke(null,
            new object[] { Path.Combine(importLink, "data.json") }) == null, "linked import JSON cannot read user-data content");
        Check((string)InvokeStatic(exports, "ResolveImportFolderPath", new object[] { "" }, 1)
            == Path.Combine(module, "PlayerExports", "module-package"), "latest module import ignores reparse package");

        object instance = Activator.CreateInstance(settings);
        TestSceneActionLegacySettings(instance);
        TestModuleMcmRouting(instance, module, fakeUser);
        TestRetainedPromptLayers(settings, module);
        Directory.CreateDirectory(Path.GetDirectoryName(cache));
        File.WriteAllText(cache, "{\"MainOptions\":[\"module-model\"],\"MainSelected\":\"module-model\"}");
        settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(instance, null);
        Check(((System.Collections.IEnumerable)settings.GetField("_mainApiModelOptions", All).GetValue(instance)).Cast<string>().Contains("module-model"), "cache reads module snapshot");
        File.WriteAllText(cache, "{\"MainOptions\":[\"external-model\"],\"MainSelected\":\"external-model\"}");
        File.SetLastWriteTimeUtc(cache, DateTime.UtcNow.AddMinutes(1));
        settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(instance, null);
        Check(((System.Collections.IEnumerable)settings.GetField("_mainApiModelOptions", All).GetValue(instance)).Cast<string>().Contains("external-model"), "same cache owner accepts a newer module snapshot");
        object rehydrated = Activator.CreateInstance(settings);
        settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(rehydrated, null);
        instance=rehydrated;
        Check(((System.Collections.IEnumerable)settings.GetField("_mainApiModelOptions", All).GetValue(instance)).Cast<string>().Contains("external-model"), "fresh owner accepts externally updated valid snapshot");
        settings.GetMethod("PersistModelDropdownCacheSnapshot", All).Invoke(instance, null);
        Check(JObject.Parse(File.ReadAllText(cache))["MainOptions"].Values<string>().Contains("external-model"), "cache saves module snapshot");
        object cold = Activator.CreateInstance(settings);
        settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(cold, null);
        Check(((System.Collections.IEnumerable)settings.GetField("_mainApiModelOptions", All).GetValue(cold)).Cast<string>().Contains("external-model"), "cold cache reload");

        foreach (byte[] invalid in new[] { Encoding.UTF8.GetBytes("{broken"),
            Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("{\"MainSelected\":\"").Concat(new byte[] { 0xff }).Concat(Encoding.UTF8.GetBytes("\"}")).ToArray() })
        {
            File.WriteAllBytes(cache, invalid);
            File.SetLastWriteTimeUtc(cache, DateTime.UtcNow.AddMinutes(2));
            settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(instance, null);
            Check(((System.Collections.IEnumerable)settings.GetField("_mainApiModelOptions", All).GetValue(instance)).Cast<string>().Contains("external-model"), "bad cache retains live options");
            settings.GetMethod("PersistModelDropdownCacheSnapshot", All).Invoke(instance, null);
            Check(invalid.SequenceEqual(File.ReadAllBytes(cache)), "bad cache original is protected on save");
        }
        File.WriteAllText(cache, "{\"MainOptions\":[\"readonly-cache\"]}");
        byte[] readonlyCache = File.ReadAllBytes(cache);
        File.SetAttributes(cache, FileAttributes.ReadOnly);
        try
        {
            settings.GetMethod("PersistModelDropdownCacheSnapshot", All).Invoke(instance, null);
            Check(readonlyCache.SequenceEqual(File.ReadAllBytes(cache)), "readonly cache original is preserved");
        }
        finally { File.SetAttributes(cache, FileAttributes.Normal); }

        InvokeStatic(terminal, "TrySaveSettingsFile", new object[] { terminalFile, Activator.CreateInstance(SutType("AnimusForge.AnimusForgeTerminalSettingsData")) }, 2);
        Check(File.Exists(terminalFile), "legacy entry compatibility primitive remains module-bound");
        foreach (byte[] invalid in new[] { Encoding.UTF8.GetBytes("{broken"), new byte[] { 0xff, 0x80 } })
        {
            File.WriteAllBytes(terminalFile, invalid);
            bool badEntryRejected = false;
            try { InvokeStatic(terminal, "TrySaveSettingsFile", new object[] { terminalFile, Activator.CreateInstance(SutType("AnimusForge.AnimusForgeTerminalSettingsData")) }, 2); }
            catch { badEntryRejected = true; }
            Check(badEntryRejected && invalid.SequenceEqual(File.ReadAllBytes(terminalFile)), "bad legacy entry bytes are protected");
        }
        File.WriteAllText(terminalFile, "{\"IsHotkeyEnabled\":true,\"IsMapIconEnabled\":true}");
        File.SetAttributes(terminalFile, FileAttributes.ReadOnly);
        try
        {
            bool readonlyEntryRejected = false;
            try { InvokeStatic(terminal, "TrySaveSettingsFile", new object[] { terminalFile, Activator.CreateInstance(SutType("AnimusForge.AnimusForgeTerminalSettingsData")) }, 2); }
            catch { readonlyEntryRejected = true; }
            Check(readonlyEntryRejected && (bool)JObject.Parse(File.ReadAllText(terminalFile))["IsHotkeyEnabled"], "readonly legacy entry is protected");
        }
        finally { File.SetAttributes(terminalFile, FileAttributes.Normal); }
        string package = Path.Combine(module, "PlayerExports", "existing");
        Directory.CreateDirectory(package);
        File.WriteAllText(Path.Combine(package, "old.json"), "{\"keep\":true}");
        object transaction = InvokeStatic(exports, "BeginExportPackage", new object[] { Path.Combine(module, "PlayerExports"), "existing" }, 2);
        transaction.GetType().GetMethod("Publish", All).Invoke(transaction, null);
        Check(Directory.GetFiles(Path.Combine(module, "Recovery"), "old.json", SearchOption.AllDirectories).Length == 1, "export backup is module-local");
        Check(!Directory.Exists(Path.Combine(fakeUser, "Cache")) && !Directory.Exists(Path.Combine(fakeUser, "Recovery")), "operations do not materialize user cache or recovery");

        string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
        foreach (string directory in new[] { Path.Combine(module, "Cache"), Path.Combine(module, "Configs") })
        {
            string original = directory.EndsWith("Cache") ? cache : terminalFile;
            byte[] bytes = File.ReadAllBytes(original);
            RunPromptFixtureCommand("icacls.exe", "\"" + directory + "\" /deny *" + sid + ":(OI)(CI)(WD,AD)");
            try
            {
                if (original == cache) settings.GetMethod("PersistModelDropdownCacheSnapshot", All).Invoke(instance, null);
                else
                {
                    bool unwritableEntryRejected = false;
                    try { InvokeStatic(terminal, "TrySaveSettingsFile", new object[] { terminalFile, Activator.CreateInstance(SutType("AnimusForge.AnimusForgeTerminalSettingsData")) }, 2); }
                    catch { unwritableEntryRejected = true; }
                    Check(unwritableEntryRejected, "unwritable legacy entry directory rejects compatibility write");
                }
                Check(bytes.SequenceEqual(File.ReadAllBytes(original)), "unwritable directory preserves original");
            }
            finally { RunPromptFixtureCommand("icacls.exe", "\"" + directory + "\" /remove:d *" + sid); }
        }
        string linkedModule = Path.Combine(root, "linked", "AnimusForge");
        Directory.CreateDirectory(Path.Combine(linkedModule,"ModuleData"));
        File.WriteAllText(Path.Combine(linkedModule, "SubModule.xml"), "<Module><Id value=\"AnimusForge\" /></Module>");
        foreach (string directory in new[] { "Cache", "Configs", "PlayerExports" })
            RunPromptFixtureCommand("cmd.exe", "/c mklink /J \"" + Path.Combine(linkedModule, directory) + "\" \"" + Path.Combine(module, directory) + "\"");
        SetFixtureModuleRoot(linkedModule);
        byte[] beforeLink = File.ReadAllBytes(cache);
        object linkedInstance = Activator.CreateInstance(settings);
        settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(linkedInstance, null);
        settings.GetMethod("PersistModelDropdownCacheSnapshot", All).Invoke(linkedInstance, null);
        Check(beforeLink.SequenceEqual(File.ReadAllBytes(cache)), "reparse cache cannot read or overwrite target");
        bool linkedRejected = false;
        try { InvokeStatic(exports, "GetPlayerExportsRootPath", Array.Empty<object>(), 0); }
        catch { linkedRejected = true; }
        Check(linkedRejected, "reparse exports root is rejected");
        SetFixtureModuleRoot(module);
        Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", "relative-invalid-root");
        Check((string)InvokeStatic(exports, "GetPlayerExportsRootPath", Array.Empty<object>(), 0) == Path.Combine(module, "PlayerExports"), "even invalid old override is ignored");
        SetFixtureModuleRoot(Path.Combine(root, "missing", "AnimusForge"));
        bool rejected = false;
        try { InvokeStatic(exports, "GetPlayerExportsRootPath", Array.Empty<object>(), 0); }
        catch { rejected = true; }
        Check(rejected, "missing module cannot fall back to CWD or AppData");
        SetFixtureModuleRoot(module);
    }

    private static void TestSceneActionLegacySettings(object settings)
    {
        string repository = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
        string source = File.ReadAllText(Path.Combine(repository, "extensions", "AnimusForge.XihaiAction", "src", "Runtime", "SceneActionsMcmSettings.cs"));
        // Fail before invoking an old bridge: a red replay must never query live Documents.
        Check(!source.Contains("Environment.GetFolderPath") && !source.Contains("File.ReadAllText"),
            "scene action bridge never queries or imports old Documents settings");
        InvokeStatic(SutType("AnimusForge.McmSettingsStorage"), "EnsureReady", Array.Empty<object>(), 0);
        Type bridge = SutType("AnimusForge.XihaiAction.SceneActionsMcmSettings");
        var properties = (System.Collections.Generic.Dictionary<string, System.Reflection.PropertyInfo>)bridge.GetField("Properties", All).GetValue(null);
        var previous = properties.ToArray();
        properties.Clear();
        foreach (var property in settings.GetType().GetProperties(All)) properties[property.Name] = property;
        try
        {
            var version = properties["SceneActionsMcmMigrationVersion"];
            var natural = properties["NaturalLanguageReplyActionsEnabled"];
            version.SetValue(settings, 0);
            natural.SetValue(settings, false);
            bridge.GetMethod("EnsureLegacyMigration", All).Invoke(null, new[] { settings });
            Check(!(bool)natural.GetValue(settings), "scene action bridge retains existing module user switches");
            Check((int)version.GetValue(settings) == 2, "scene action bridge preserves its existing settings version identity");
            properties["ReplyMinimumChars"].SetValue(settings, 30);
            properties["ReplyMaximumChars"].SetValue(settings, 80);
            version.SetValue(settings, 0);
            bridge.GetMethod("EnsureLegacyMigration", All).Invoke(null, new[] { settings });
            Check((int)properties["ReplyMinimumChars"].GetValue(settings) == 60
                && (int)properties["ReplyMaximumChars"].GetValue(settings) == 160,
                "scene action module-local battle speech defaults upgrade remains active");
        }
        finally
        {
            properties.Clear();
            foreach (var pair in previous) properties[pair.Key] = pair.Value;
        }
    }

    private static void TestModuleMcmRouting(object settings, string module, string fakeUser)
    {
        Type routing = SutType("AnimusForge.McmSettingsStorage");
        Check(routing.GetMethod("RouteJson", All) == null && routing.GetMethod("RoutePresetDirectory", All) == null,
            "module MCM directory overrides are retired; shared persistence replay is in SharedMcmChecks");
        Type files = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MCM.Internal.GameFeatures.FileSystemProvider")).First(t => t != null);
        object provider = Activator.CreateInstance(files, true);
        object root = files.GetMethod("GetModSettingsDirectory").Invoke(provider, null);
        object global = files.GetMethod("GetDirectory").Invoke(provider, new[] { root, (object)"Global" });
        object owner = files.GetMethod("GetDirectory").Invoke(provider, new[] { global, (object)"AnimusForge" });
        Type directory = owner.GetType();
        Check(directory.GetProperty("Type").GetValue(owner).ToString() == "User", "native MCM User directory is retained");
        Check(((string)directory.GetProperty("Path").GetValue(owner)).Replace('\\', '/').TrimEnd('/').EndsWith("Configs/ModSettings/Global/AnimusForge"), "shared AF folder keeps native relative identity");
        object foreign = files.GetMethod("GetDirectory").Invoke(provider, new[] { global, (object)"OtherMod" });
        Check(((string)directory.GetProperty("Path").GetValue(foreign)).TrimEnd('\\', '/').EndsWith("OtherMod"), "native directory helpers leave foreign IDs unchanged");
    }

    private static void TestRetainedPromptLayers(Type settings, string module)
    {
        const string file = "PlayerCustomPromptRule.json";
        string repository = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
        JObject map = JObject.Parse(File.ReadAllText(Path.Combine(repository, "content", "content-map.json")));
        JObject row = map["entries"].OfType<JObject>().Single(entry =>
            (string)entry["target"] == "ModuleData/Defaults/CustomPrompts/" + file);
        string source = Path.Combine(repository, (string)row["source"]);
        string english = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source) + ".en.json");
        if (File.Exists(english)) source = english;
        string packaged = Path.Combine(module, "ModuleData", "Defaults", "CustomPrompts");
        string user = Path.Combine(module, "CustomPrompts");
        Directory.CreateDirectory(packaged);
        Directory.CreateDirectory(user);
        File.Copy(source, Path.Combine(packaged, file));
        string userFile = Path.Combine(user, file);
        File.Copy(source, userFile);
        Check((bool)InvokeStatic(settings, "IsKnownPackagedCustomPromptFile", new object[] { userFile }, 1),
            "current clean default remains recognized by exact known hash");
        Check(InvokeStatic(settings, "GetCustomPromptUserReadPath", new object[] { userFile }, 1) == null,
            "known old bundle without module import uses package defaults, not C data");
        object[] readArgs = { user, packaged, file, null, "", null };
        Check((bool)settings.GetMethod("TryReadLayeredCustomPromptTextJsonFile", All).Invoke(null, readArgs)
            && (string)readArgs[5] == ((string)JObject.Parse(File.ReadAllText(source))["Text"]).Replace("\r\n", "\n").Trim(),
            "localized package default layer still supplies actual prompt content");
        string imported = Path.Combine(user, "LegacyImported", file);
        Directory.CreateDirectory(Path.GetDirectoryName(imported));
        File.WriteAllText(imported, "{\"Version\":1,\"Text\":\"already-imported\"}");
        Check((string)InvokeStatic(settings, "GetCustomPromptUserReadPath", new object[] { userFile }, 1) == imported,
            "already imported module prompt remains readable without migration");
        readArgs[5] = null;
        Check((bool)settings.GetMethod("TryReadLayeredCustomPromptTextJsonFile", All).Invoke(null, readArgs)
            && (string)readArgs[5] == "already-imported", "module import retains precedence over packaged defaults");
        File.WriteAllText(userFile, "{\"Version\":1,\"Text\":\"module-user-edit\"}");
        readArgs[5] = null;
        Check((bool)settings.GetMethod("TryReadLayeredCustomPromptTextJsonFile", All).Invoke(null, readArgs)
            && (string)readArgs[5] == "module-user-edit", "edited module prompt retains precedence over imported and packaged content");
    }
}
