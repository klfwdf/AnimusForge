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
        string cache = Path.Combine(module, "Cache", "ModelDropdownCache.json");
        Check(string.Equals((string)InvokeStatic(settings, "GetModelDropdownCachePath", Array.Empty<object>(), 0), cache,
            StringComparison.OrdinalIgnoreCase), "cache belongs to user-approved module Cache");
        string terminalFile = Path.Combine(module, "Configs", "TerminalSettings.json");
        Check((string)InvokeStatic(terminal, "GetSettingsPath", Array.Empty<object>(), 0) == terminalFile, "terminal belongs to user-approved module Configs");
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
        Directory.CreateDirectory(Path.GetDirectoryName(cache));
        File.WriteAllText(cache, "{\"MainOptions\":[\"module-model\"],\"MainSelected\":\"module-model\"}");
        settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(instance, null);
        Check(((System.Collections.IEnumerable)settings.GetField("_mainApiModelOptions", All).GetValue(instance)).Cast<string>().Contains("module-model"), "cache reads module snapshot");
        File.WriteAllText(cache, "{\"MainOptions\":[\"external-model\"],\"MainSelected\":\"external-model\"}");
        File.SetLastWriteTimeUtc(cache, DateTime.UtcNow.AddMinutes(1));
        settings.GetMethod("EnsureModelDropdownCacheHydrated", All).Invoke(instance, null);
        Check(((System.Collections.IEnumerable)settings.GetField("_mainApiModelOptions", All).GetValue(instance)).Cast<string>().Contains("module-model"), "retained local hydration guard does not replace a live model selection");
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

        terminal.GetField("_loaded", All).SetValue(null, false);
        Check((bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null), "missing module settings use defaults, not user settings");
        terminal.GetProperty("IsHotkeyEnabled").SetValue(null, false);
        Check(File.Exists(terminalFile) && !(bool)JObject.Parse(File.ReadAllText(terminalFile))["IsHotkeyEnabled"], "terminal save is module-local");
        terminal.GetField("_loaded", All).SetValue(null, false);
        Check(!(bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null), "terminal cold reload");
        File.WriteAllText(terminalFile, "{\"IsHotkeyEnabled\":false,\"IsMapIconEnabled\":false}");
        terminal.GetField("_loaded", All).SetValue(null, false);
        Check((bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null), "both-disabled settings retain escape hotkey");

        foreach (byte[] invalid in new[] { Encoding.UTF8.GetBytes("{broken"), new byte[] { 0xff, 0x80 } })
        {
            File.WriteAllBytes(terminalFile, invalid);
            terminal.GetField("_loaded", All).SetValue(null, false);
            Check((bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null), "bad settings use safe defaults");
            terminal.GetProperty("IsHotkeyEnabled").SetValue(null, false);
            Check(invalid.SequenceEqual(File.ReadAllBytes(terminalFile)) && (bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null), "bad settings preserve bytes and rollback memory");
        }
        File.WriteAllText(terminalFile, "{\"IsHotkeyEnabled\":true,\"IsMapIconEnabled\":true}");
        File.SetAttributes(terminalFile, FileAttributes.ReadOnly);
        try
        {
            terminal.GetField("_loaded", All).SetValue(null, false);
            terminal.GetProperty("IsHotkeyEnabled").SetValue(null, false);
            Check((bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null) && (bool)JObject.Parse(File.ReadAllText(terminalFile))["IsHotkeyEnabled"], "readonly settings fail without false success");
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
                    terminal.GetField("_loaded", All).SetValue(null, false);
                    terminal.GetProperty("IsHotkeyEnabled").SetValue(null, false);
                    Check((bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null), "unwritable terminal directory rolls memory back");
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
}
