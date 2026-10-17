using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using AnimusForge;
using AnimusForge.PolicyEffects;
using Newtonsoft.Json.Linq;

namespace PolicyEffectModule.ContractTests;

internal static partial class Program
{
    private static void TestModulePromptStorageContracts(bool refreshOnly = false)
    {
        string root = Environment.GetEnvironmentVariable("AF_PROMPT_TEST_ROOT");
        string repository = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
        Check(!string.IsNullOrWhiteSpace(root) && Path.IsPathRooted(root)
            && Path.GetFullPath(root).StartsWith(Path.Combine(repository, "artifacts") + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) && !Directory.Exists(root), "new workspace-only prompt fixture required");
        Directory.CreateDirectory(root);
        string moduleRoot = Path.Combine(root, "AnimusForge");
        Directory.CreateDirectory(Path.Combine(moduleRoot, "ModuleData"));
        File.WriteAllText(Path.Combine(moduleRoot, "SubModule.xml"), "<Module><Id value=\"AnimusForge\" /></Module>");
        string prompts = Path.Combine(moduleRoot, "CustomPrompts");
        string policy = Path.Combine(prompts, "Policy");
        Directory.CreateDirectory(policy);
        Type settings = SutType("AnimusForge.DuelSettings");
        Type paths = SutType("AnimusForge.AnimusForgeModulePaths");
        // Also isolate production Logger initialization before invoking any disk helpers.
        PrepareFixtureModuleAssets(moduleRoot);
        SetFixtureModuleRoot(moduleRoot);
        string ordinary = Path.Combine(prompts, "PlayerCustomPromptRule.json");
        InvokeStatic(settings, "PersistCustomPromptTextFileUnlocked", new object[] { ordinary, "custom" }, 2);
        InvokeStatic(settings, "PersistCustomPromptTextFileUnlocked", new object[] { ordinary, "" }, 2);
        Check(File.Exists(ordinary) && (string)JObject.Parse(File.ReadAllText(ordinary))["Text"] == "",
            "blank ordinary text must remain a module JSON, not delete an override");

        SetFixtureModuleRoot(moduleRoot);
        string oldDataRoot = Environment.GetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", Path.Combine(root, "ignored-data"));
            Check((string)InvokeStatic(settings, "GetCustomPromptTextStoreDirectory", Array.Empty<object>(), 0) == prompts,
                "ordinary prompt path uses current module");
            Check((string)InvokeStatic(settings, "GetCustomPromptTextStoreDirectoryForPolicyPrompts", Array.Empty<object>(), 0) == policy,
                "policy and companion settings path uses current module");
            Check(!Directory.Exists(Path.Combine(root, "ignored-data")), "prompt operations must not materialize an AppData override");
            string fakeUser = Path.Combine(root, "ignored-data", "UserData", "Overrides", "CustomPrompts");
            Directory.CreateDirectory(fakeUser);
            File.WriteAllText(Path.Combine(fakeUser, "PlayerCustomPromptRule.json"), "{\"Version\":1,\"Text\":\"IGNORED_USER\"}");
            object defaults = InvokeStatic(settings, "BuildDefaultCustomPromptTextStore", Array.Empty<object>(), 0);
            InvokeStatic(settings, "EnsureCustomPromptTextStoreFilesUnlocked", new[] { (object)prompts, defaults }, 2);
            Check((string)JObject.Parse(File.ReadAllText(ordinary))["Text"] == "", "initialization preserves explicit blank");
            string weekly = Path.Combine(prompts, "WeeklyReportWritingRequirements.json");
            Check(File.Exists(weekly) && File.Exists(Path.Combine(prompts, "WorldBulletinWritingRequirements.json")),
                "missing old and newly added ordinary prompts are materialized");
            settings.GetField("_customPromptTextStoreCached", All).SetValue(null, null);
            settings.GetField("_customPromptTextStoreNextRefreshTimestamp", All).SetValue(null, 0L);
            Check((string)InvokeStatic(settings, "LoadPlayerCustomPromptRuleFromDiskOrDefault", Array.Empty<object>(), 0) == "",
                "actual ordinary store ignores user-data file and reads module blank");
            Check((bool)InvokeStatic(settings, "TryPersistPlayerCustomPromptRuleFile", new object[] { "MODULE_SAVED" }, 1),
                "actual ordinary writer succeeds in module");
            Check((bool)InvokeStatic(settings, "TryPersistNpcRulerPolicyPromptFile", new object[] { "" }, 1)
                && !string.IsNullOrWhiteSpace((string)JObject.Parse(File.ReadAllText(Path.Combine(policy, "NpcRulerPolicyPrompt.json")))["Text"]),
                "blank ruler policy writes normalized current default");
            Check((string)InvokeStatic(settings, "LoadPlayerCustomPromptRuleFromDiskOrDefault", Array.Empty<object>(), 0) == "MODULE_SAVED",
                "ordinary save immediately invalidates cached snapshot");
            File.WriteAllText(ordinary, "{\"Version\":1,\"Text\":\"EXTERNAL_EDIT_LONGER\"}");
            Check((string)InvokeStatic(settings, "LoadPlayerCustomPromptRuleFromDiskOrDefault", Array.Empty<object>(), 0) == "MODULE_SAVED",
                "ordinary refresh remains throttled");
            settings.GetField("_customPromptTextStoreNextRefreshTimestamp", All).SetValue(null, 0L);
            Check((string)InvokeStatic(settings, "LoadPlayerCustomPromptRuleFromDiskOrDefault", Array.Empty<object>(), 0) == "EXTERNAL_EDIT_LONGER",
                "ordinary refresh reloads changed file");
            settings.GetField("_customPromptTextStoreCached", All).SetValue(null, null);
            settings.GetField("_customPromptTextStoreNextRefreshTimestamp", All).SetValue(null, 0L);
            Check((string)InvokeStatic(settings, "LoadPlayerCustomPromptRuleFromDiskOrDefault", Array.Empty<object>(), 0) == "EXTERNAL_EDIT_LONGER",
                "ordinary cold reload preserves persisted module content");

            // The caller has already validated these directories. A fingerprint
            // scan must not reopen the module manifest once per prompt file.
            MethodInfo fingerprint = settings.GetMethod("ComputeCustomPromptTextStoreFingerprint", All);
            Func<string, bool, long> scan = (directory, userOverrides) => (long)fingerprint.Invoke(null,
                fingerprint.GetParameters().Length == 1
                    ? new object[] { directory }
                    : new object[] { directory, userOverrides });
            string packaged = Path.Combine(moduleRoot, "ModuleData", "Defaults", "CustomPrompts");
            Directory.CreateDirectory(packaged);
            File.WriteAllText(Path.Combine(packaged, "PlayerCustomPromptRule.json"), "{\"Version\":1,\"Text\":\"PACKAGED 中文 English\"}");
            long userFingerprint = scan(prompts, true);
            long packagedFingerprint = scan(packaged, false);
            Check(userFingerprint != 0L && packagedFingerprint != 0L, "prompt fingerprints initialized");
            using (new FileStream(Path.Combine(moduleRoot, "SubModule.xml"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Check(scan(prompts, true) == userFingerprint, "user fingerprint reuses validated directory without manifest IO");
                Check(scan(packaged, false) == packagedFingerprint, "packaged fingerprint reuses validated directory without manifest IO");
            }
            File.AppendAllText(Path.Combine(packaged, "PlayerCustomPromptRule.json"), " ");
            Check(scan(packaged, false) != packagedFingerprint, "packaged fingerprint still detects external edits");

            foreach (byte[] invalid in new[] { Encoding.UTF8.GetBytes("{broken"), new byte[] { 0xff, 0xfe, 0x80 },
                Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("{\"Version\":1,\"Text\":\"NOT_UTF8\"}")).ToArray(), new byte[300000] })
            {
                File.WriteAllBytes(ordinary, invalid);
                object[] read = { ordinary, null, "fallback", null };
                Check(!(bool)InvokeStatic(settings, "TryReadCustomPromptTextJsonFile", read, 4), "invalid ordinary file rejected");
                Check(!(bool)InvokeStatic(settings, "TryPersistPlayerCustomPromptRuleFile", new object[] { "REPLACEMENT" }, 1)
                    && invalid.SequenceEqual(File.ReadAllBytes(ordinary)), "invalid ordinary original cannot be overwritten");
            }
            File.WriteAllText(ordinary, "{\"Version\":1,\"Text\":\"READONLY\"}");
            File.SetAttributes(ordinary, FileAttributes.ReadOnly);
            try
            {
                Check(!(bool)InvokeStatic(settings, "TryPersistPlayerCustomPromptRuleFile", new object[] { "REPLACEMENT" }, 1)
                    && File.ReadAllText(ordinary).Contains("READONLY"), "read-only ordinary save fails without changing original");
            }
            finally { File.SetAttributes(ordinary, FileAttributes.Normal); }

            byte[] originalPrompt = File.ReadAllBytes(ordinary);
            string importedPrompt = Path.Combine(prompts, "LegacyImported", "PlayerCustomPromptRule.json");
            JObject promptMap = JObject.Parse(File.ReadAllText(Path.Combine(repository, "content", "content-map.json")));
            string promptSource = Path.Combine(repository, (string)promptMap["entries"].OfType<JObject>().Single(row =>
                (string)row["target"] == "ModuleData/Defaults/CustomPrompts/PlayerCustomPromptRule.json")["source"]);
            try
            {
                foreach (string asset in new[] { promptSource, Path.Combine(Path.GetDirectoryName(promptSource), "PlayerCustomPromptRule.en.json") })
                {
                    if (File.Exists(importedPrompt)) File.Delete(importedPrompt);
                    File.Copy(asset, ordinary, true);
                    Check((bool)InvokeStatic(settings, "IsKnownPackagedCustomPromptFile", new object[] { ordinary }, 1),
                        "CN/EN exact default hashes remain recognized");
                    long cleanUser = scan(prompts, true);
                    long unprojected = scan(prompts, false);
                    Directory.CreateDirectory(Path.GetDirectoryName(importedPrompt));
                    File.WriteAllText(importedPrompt, "{\"Version\":1,\"Text\":\"Imported 中文 English\"}");
                    Check(scan(prompts, true) != cleanUser, "user fingerprint detects retained imported override");
                    Check(scan(prompts, false) == unprojected, "package fingerprint does not apply user override projection");
                }
            }
            finally
            {
                File.WriteAllBytes(ordinary, originalPrompt);
                if (File.Exists(importedPrompt)) File.Delete(importedPrompt);
            }

            if (refreshOnly) return;

            Check(PolicyEffectPromptService.EnsureStorageFiles(out string error), "initialize policy prompts: " + error);
            var module = PolicyEffectModuleCatalog.Modules.First(value => value.Descriptor?.PromptVisible == true);
            string effect = Path.Combine(policy, "Effects", module.Id + ".json");
            string common = Path.Combine(policy, "Effects", PolicyEffectPromptService.CommonEffectPromptFileName);
            string auto = Path.Combine(policy, PolicyEffectPromptService.AutoDraftPromptFileName);
            Check(File.Exists(effect) && File.Exists(common) && File.Exists(auto), "policy initialization creates all missing documents");
            var doc = JObject.Parse(File.ReadAllText(effect));
            doc["FutureField"] = "keep";
            doc["UnderstandingPrompt"] = "UNCHANGED_OTHER_FIELD";
            File.WriteAllText(effect, doc.ToString());
            Check(PolicyEffectPromptService.TrySaveModulePrompt(module.Id, PolicyEffectPromptKind.Evaluation, "", out error),
                "blank effect save succeeds: " + error);
            doc = JObject.Parse(File.ReadAllText(effect));
            Check((string)doc["EvaluationPrompt"] == module.Descriptor.EditableEvaluationPrompt
                && (string)doc["UnderstandingPrompt"] == "UNCHANGED_OTHER_FIELD" && (string)doc["FutureField"] == "keep",
                "blank effect writes descriptor default while preserving other and unknown fields");
            doc["FutureField"] = new string('x', 150000);
            File.WriteAllText(effect, doc.ToString());
            byte[] withinLimitOriginal = File.ReadAllBytes(effect);
            Check(!PolicyEffectPromptService.TrySaveModulePrompt(module.Id, PolicyEffectPromptKind.Evaluation, new string('新', 40000), out error)
                && withinLimitOriginal.SequenceEqual(File.ReadAllBytes(effect)), "combined serialized policy size limit preserves original");
            Check(PolicyEffectPromptService.TrySaveAutoDraftPrompt("", out error) && File.Exists(auto)
                && (string)JObject.Parse(File.ReadAllText(auto))["Text"] == PolicyEffectPromptService.DefaultAutoDraftPrompt,
                "blank auto draft writes current default document");
            Check(PolicyEffectPromptService.TrySaveCommonEvaluationPrompt("", out error) && File.Exists(common)
                && (string)JObject.Parse(File.ReadAllText(common))["CommonEvaluationPrompt"] == PolicyEffectPromptService.DefaultCommonEvaluationPrompt,
                "blank common prompt writes current default document");
            Check(PolicyEffectPromptService.TrySaveAutoDraftPrompt("SAVED_POLICY", out error), "policy save succeeds");
            File.WriteAllText(auto, "{\"Version\":1,\"Text\":\"EXTERNAL_POLICY_EDIT\"}");
            Check(PolicyEffectPromptService.GetAutoDraftPrompt() == "SAVED_POLICY", "policy refresh remains throttled");
            PolicyEffectPromptService.ReloadForContractTests();
            Check(PolicyEffectPromptService.GetAutoDraftPrompt() == "EXTERNAL_POLICY_EDIT", "policy cold reload reads module file");
            byte[] corrupt = Encoding.UTF8.GetBytes("{broken");
            File.WriteAllBytes(effect, corrupt);
            PolicyEffectPromptService.ReloadForContractTests();
            Check(PolicyEffectPromptService.GetEvaluationPrompt(module) == module.Descriptor.EditableEvaluationPrompt,
                "corrupt effect falls back only to its descriptor");
            Check(!PolicyEffectPromptService.TrySaveModulePrompt(module.Id, PolicyEffectPromptKind.Evaluation, "no", out error)
                && corrupt.SequenceEqual(File.ReadAllBytes(effect)), "corrupt effect original preserved on save");
            Check(PolicyEffectPromptService.EnsureStorageFiles(out error) && corrupt.SequenceEqual(File.ReadAllBytes(effect)),
                "initialization does not repair or overwrite existing corruption");
            foreach (byte[] invalid in new[] { new byte[] { 0xff, 0xfe, 0x80 }, new byte[300000],
                Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("{\"Version\":1,\"Text\":\"NOT_UTF8\"}")).ToArray() })
            {
                File.WriteAllBytes(auto, invalid);
                Check(!PolicyEffectPromptService.TrySaveAutoDraftPrompt("NO", out error)
                    && invalid.SequenceEqual(File.ReadAllBytes(auto)), "invalid policy encoding/size original preserved");
            }
            Type retrieval = SutType("AnimusForge.PolicyEffects.PolicyEffectModuleRetrievalSettings");
            string retrievalPath = (string)InvokeStatic(retrieval, "GetSettingsPath", new object[] { false }, 1);
            Check(retrievalPath == Path.Combine(policy, "PolicyEffectModuleRetrievalSettings.json"), "retrieval switches share module policy path");
            Type balance = SutType("AnimusForge.SettlementBalanceSettings");
            InvokeStatic(balance, "Initialize", Array.Empty<object>(), 0);
            object balanceStore = balance.GetField("_store", All).GetValue(null);
            Check((string)balanceStore.GetType().GetField("_path", All).GetValue(balanceStore)
                == Path.Combine(policy, "SettlementBalanceSettings.json"), "balance limits share module policy path");
            Check(PolicyEffectModuleRetrievalSettings.TrySave(PolicyEffectModuleRetrievalSettings.CreateEditableStateSnapshot(), out error)
                && File.Exists(retrievalPath), "retrieval configuration saves in module");
            string balancePath = Path.Combine(policy, "SettlementBalanceSettings.json");
            Check(SettlementBalanceSettings.TrySave(SettlementBalanceSnapshot.Default, out error) && File.Exists(balancePath),
                "balance configuration saves in module");
            foreach (byte[] invalid in new[] { Encoding.UTF8.GetBytes("{broken"), new byte[] { 0xff, 0xfe, 0x80 }, new byte[300000] })
            {
                File.WriteAllBytes(retrievalPath, invalid);
                File.WriteAllBytes(balancePath, invalid);
                Check(!PolicyEffectModuleRetrievalSettings.TrySave(PolicyEffectModuleRetrievalSettings.CreateEditableStateSnapshot(), out error)
                    && invalid.SequenceEqual(File.ReadAllBytes(retrievalPath)), "invalid retrieval original cannot be overwritten");
                Check(!SettlementBalanceSettings.TrySave(SettlementBalanceSnapshot.Default, out error)
                    && invalid.SequenceEqual(File.ReadAllBytes(balancePath)), "invalid balance original cannot be overwritten");
            }
            Check(File.ReadAllText(Path.Combine(fakeUser, "PlayerCustomPromptRule.json")).Contains("IGNORED_USER"),
                "inactive user-data content is untouched");

            string linked = Path.Combine(root, "linked-prompts");
            RunPromptFixtureCommand("cmd.exe", "/c mklink /J \"" + linked + "\" \"" + prompts + "\"");
            byte[] beforeLink = File.ReadAllBytes(ordinary);
            object[] linkedRead = { Path.Combine(linked, Path.GetFileName(ordinary)), null, "fallback", null };
            Check(!(bool)InvokeStatic(settings, "TryReadCustomPromptTextJsonFile", linkedRead, 4), "reparse ancestor refuses ordinary reading");
            PolicyEffectPromptService.SetStorageDirectoryOverrideForContractTests(linked);
            Check(!PolicyEffectPromptService.EnsureStorageFiles(out error)
                && !PolicyEffectPromptService.TrySaveAutoDraftPrompt("NO", out error), "reparse policy root refuses initialize and save");
            Check(beforeLink.SequenceEqual(File.ReadAllBytes(ordinary)), "reparse failures preserve target original");
            PolicyEffectPromptService.SetStorageDirectoryOverrideForContractTests(null);

            string readonlyRoot = Path.Combine(root, "readonly", "AnimusForge");
            Directory.CreateDirectory(Path.Combine(readonlyRoot, "ModuleData"));
            File.WriteAllText(Path.Combine(readonlyRoot, "SubModule.xml"), "<Module><Id value=\"AnimusForge\" /></Module>");
            string readonlyPrompts = Path.Combine(readonlyRoot, "CustomPrompts");
            Directory.CreateDirectory(readonlyPrompts);
            string readonlyOriginal = Path.Combine(readonlyPrompts, "PlayerCustomPromptRule.json");
            File.WriteAllText(readonlyOriginal, "{\"Version\":1,\"Text\":\"READONLY_DIRECTORY\"}");
            string sid = WindowsIdentity.GetCurrent().User.Value;
            RunPromptFixtureCommand("icacls.exe", "\"" + readonlyPrompts + "\" /deny *" + sid + ":(OI)(CI)(WD,AD)");
            try
            {
                SetFixtureModuleRoot(readonlyRoot);
                settings.GetField("_customPromptTextStoreCached", All).SetValue(null, null);
                settings.GetField("_customPromptTextStoreNextRefreshTimestamp", All).SetValue(null, 0L);
                Check((string)InvokeStatic(settings, "LoadPlayerCustomPromptRuleFromDiskOrDefault", Array.Empty<object>(), 0) == "READONLY_DIRECTORY",
                    "failed missing-file initialization does not hide readable original");
                Check(!(bool)InvokeStatic(settings, "TryPersistPlayerCustomPromptRuleFile", new object[] { "NO" }, 1)
                    && File.ReadAllText(readonlyOriginal).Contains("READONLY_DIRECTORY"), "read-only directory refuses save and retains original");
                Check(!PolicyEffectPromptService.EnsureStorageFiles(out error) && !string.IsNullOrWhiteSpace(error),
                    "read-only directory policy initialization explicitly fails");
            }
            finally { RunPromptFixtureCommand("icacls.exe", "\"" + readonlyPrompts + "\" /remove:d *" + sid); }
            SetFixtureModuleRoot(Path.Combine(root, "missing-module"));
            Check(!(bool)InvokeStatic(settings, "TryPersistPlayerCustomPromptRuleFile", new object[] { "NO" }, 1)
                && !Directory.Exists(Path.Combine(root, "missing-module")), "unresolved module refuses save without CWD/AppData fallback");
        }
        finally
        {
            PolicyEffectPromptService.SetStorageDirectoryOverrideForContractTests(null);
            PolicyEffectPromptService.ReloadForContractTests();
            SetFixtureModuleRoot(null);
            Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", oldDataRoot);
        }
    }

    private static void PrepareFixtureModuleAssets(string module)
    {
        string repository=FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
        var map=JObject.Parse(File.ReadAllText(Path.Combine(repository,"content","content-map.json")));
        foreach(JObject row in map["entries"])
        {
            string relative=(string)row["target"];
            if(relative==null || !relative.StartsWith("ModuleData/",StringComparison.Ordinal)
                || !relative.EndsWith(".json",StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("ModuleData/Defaults/",StringComparison.Ordinal)) continue;
            string source=Path.Combine(repository,(string)row["source"]);
            string english=Path.Combine(Path.GetDirectoryName(source),Path.GetFileNameWithoutExtension(source)+".en.json");
            if(File.Exists(english))source=english;
            string destination=Path.Combine(module,relative.Replace('/',Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if(!File.Exists(destination))File.Copy(source,destination,false);
        }
    }

    // Restart only the cold cached path owner in the fixture; execute the production resolver unchanged.
    private static void SetFixtureModuleRoot(string root)
    {
        Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", root);
        Type data=SutType("AnimusForge.AnimusForgeDataPaths");
        var lazy=new Lazy<string>(() => (string)data.GetMethod("ResolveRoot", All).Invoke(null,new object[]{root}));
        data.GetField("CurrentRoot", All).SetValue(null,lazy);
    }

    private static void RunPromptFixtureCommand(string executable, string arguments)
    {
        using (var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), executable), arguments)
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
        {
            string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            Check(process.ExitCode == 0, "workspace fixture command failed: " + output);
        }
    }
}
