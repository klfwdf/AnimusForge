using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BUTR.DependencyInjection;
using HarmonyLib;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using Newtonsoft.Json.Linq;

// Native MCM directory composition and actual product save/load, with only the
// filesystem mapped into an isolated workspace Documents fixture.
static class SharedMcmChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static string mainPath;
    static bool UserPathPrefix(ref string __result) { __result = mainPath; return false; }
    sealed class Files : IFileSystemProvider
    {
        public readonly string Root;
        public bool FailWrites;
        public Files(string root) { Root = root; Directory.CreateDirectory(root); }
        public GameDirectory GetModSettingsDirectory() => new GameDirectory(PlatformDirectoryType.User, "Configs\\ModSettings\\");
        public GameDirectory GetDirectory(GameDirectory directory, string name) => new GameDirectory(directory.Type, directory.Path + name + "\\");
        public GameDirectory GetOrCreateDirectory(GameDirectory directory, string name) => GetDirectory(directory, name);
        public string GetSystemPath(GameDirectory directory) => Path.Combine(Root, directory.Path.Replace('\\', Path.DirectorySeparatorChar));
        public string GetSystemPath(GameFile file) => Path.Combine(GetSystemPath(file.Owner), file.Name);
        public GameFile[] GetFiles(GameDirectory directory, string pattern) => Array.Empty<GameFile>();
        public GameFile GetFile(GameDirectory directory, string name) { var f = new GameFile(directory, name); return File.Exists(GetSystemPath(f)) ? f : null; }
        public GameFile GetOrCreateFile(GameDirectory directory, string name) { Directory.CreateDirectory(GetSystemPath(directory)); return new GameFile(directory, name); }
        public bool WriteData(GameFile file, byte[] data) { if (FailWrites) return false; if (data == null) File.Delete(GetSystemPath(file)); else { Directory.CreateDirectory(GetSystemPath(file.Owner)); File.WriteAllBytes(GetSystemPath(file), data); } return true; }
        public byte[] ReadData(GameFile file) => File.Exists(GetSystemPath(file)) ? File.ReadAllBytes(GetSystemPath(file)) : null;
    }
    internal static void Run(Assembly af, BaseSettings duel, string module, Action<bool,string> check)
    {
        var providerField = typeof(GenericServiceProvider).GetField("GlobalServiceProvider", All);
        var previous = providerField.GetValue(null);
        var files = new Files(Path.Combine(module, "SharedDocuments", "Mount and Blade II Bannerlord"));
        var discovery = new Discovery(files);
        providerField.SetValue(null, discovery);
        var originalOwner = TerminalProviderProbe.Owners[duel.Id];
        var originalSave = TerminalProviderProbe.OnSave;
        var harmony = new Harmony("af.tests.shared.mcm.userpath");
        var userPath = Assembly.Load("TaleWorlds.Library").GetType("TaleWorlds.Library.PlatformFilePath", true).GetProperty("FileFullPath").GetGetMethod();
        try
        {
            var storage = af.GetType("AnimusForge.McmSettingsStorage", true);
            storage.GetMethod("EnsureReady", All).Invoke(null, null);
            var formatType = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat", true);
            var loggerType = typeof(BaseSettings).Assembly.GetType("BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1", true).MakeGenericType(formatType);
            var format = Activator.CreateInstance(formatType, Activator.CreateInstance(loggerType));
            var save = formatType.GetMethod("Save"); var load = formatType.GetMethod("Load");
            GameDirectory OwnerDirectory(BaseSettings owner) => files.GetDirectory(files.GetDirectory(files.GetModSettingsDirectory(), "Global"), owner.FolderName);
            string OwnerPath(BaseSettings owner) => files.GetSystemPath(new GameFile(OwnerDirectory(owner),owner.Id+".json"));
            mainPath = OwnerPath(duel);
            harmony.Patch(userPath,prefix:new HarmonyMethod(typeof(SharedMcmChecks).GetMethod("UserPathPrefix",All)));
            var terminal = af.GetType("AnimusForge.AnimusForgeTerminalSettings",true);
            check((string)terminal.GetMethod("GetSettingsPath",All).Invoke(null,null)==mainPath,"terminal and MCM share main JSON path");
            var nativeFiles = typeof(BaseSettings).Assembly.GetType("MCM.Internal.GameFeatures.FileSystemProvider",true);
            var native = Activator.CreateInstance(nativeFiles,true);
            var root = (GameDirectory)nativeFiles.GetMethod("GetModSettingsDirectory").Invoke(native,null);
            var global = (GameDirectory)nativeFiles.GetMethod("GetDirectory").Invoke(native,new object[]{root,"Global"});
            var folder = (GameDirectory)nativeFiles.GetMethod("GetDirectory").Invoke(native,new object[]{global,"AnimusForge"});
            check(folder.Type==PlatformDirectoryType.User && folder.Path.Replace('\\','/').TrimEnd('/').EndsWith("Configs/ModSettings/Global/AnimusForge"),"actual MCM directory helpers remain standard User store");
            foreach(var owner in TerminalProviderProbe.Owners.Values.Cast<BaseSettings>())
            {
                check(owner.FolderName==(owner.Id=="AnimusForge_Coup_v1"?"AnimusForge_Coup":"AnimusForge"),"native owner folder "+owner.Id);
                check((bool)save.Invoke(format,new object[]{owner,OwnerDirectory(owner),owner.Id}),"actual MCM disk save "+owner.Id);
                var again=(BaseSettings)Activator.CreateInstance(owner.GetType());
                load.Invoke(format,new object[]{again,OwnerDirectory(owner),owner.Id});
                check(File.Exists(OwnerPath(owner)),"shared owner file "+owner.Id);
            }
            var json=JObject.Parse(File.ReadAllText(mainPath)); json["MinimumPositiveNpcRelationGain"]=5;
            var evolution = duel.GetType().GetProperty("EnableNpcPersonaEventEvolution");
            check(evolution != null && !(bool)evolution.GetValue(duel), "persona evolution defaults off");
            evolution.SetValue(duel, true);
            check((bool)save.Invoke(format,new object[]{duel,OwnerDirectory(duel),duel.Id}), "persona evolution native MCM save");
            var evolutionReload=(BaseSettings)Activator.CreateInstance(duel.GetType());
            load.Invoke(format,new object[]{evolutionReload,OwnerDirectory(duel),duel.Id});
            check((bool)evolution.GetValue(evolutionReload), "persona evolution native MCM JSON roundtrip");
            evolution.SetValue(duel, false);
            var profileType = af.GetType("AnimusForge.MyBehavior+NpcPersonaProfile", true);
            var persona = Activator.CreateInstance(profileType, true);
            profileType.GetField("SpeechStyle").SetValue(persona, "careful wording");
            profileType.GetField("RecentChangeReason").SetValue(persona, "A lost castle changed his outlook.");
            var render = af.GetType("AnimusForge.NpcPersonaTextRules", true).GetMethod("BuildPersonaContext", All, null, new[]{profileType,typeof(bool)},null);
            check(((string)render.Invoke(null,new[]{persona,(object)true})).Contains("A lost castle changed his outlook."), "actual DLL encyclopedia projection displays change reason");
            check(!((string)render.Invoke(null,new[]{persona,(object)false})).Contains("A lost castle changed his outlook."), "prompt projection uses latest fields without extra UI explanation tokens");
            json.Remove("TerminalHotkeyEnabled");json.Remove("TerminalMapIconEnabled");
            File.WriteAllText(mainPath,json.ToString());
            string legacy=Path.Combine(module,"Configs","TerminalSettings.json");
            File.WriteAllText(legacy,"{\"IsHotkeyEnabled\":false,\"IsMapIconEnabled\":true}");
            string legacyBytes=File.ReadAllText(legacy);
            string moduleMain=Path.Combine(module,"Configs","ModSettings","Global","AnimusForge",duel.Id+".json");
            Directory.CreateDirectory(Path.GetDirectoryName(moduleMain));File.WriteAllText(moduleMain,"{\"MinimumPositiveNpcRelationGain\":99}");
            var restored=(BaseSettings)Activator.CreateInstance(duel.GetType());load.Invoke(format,new object[]{restored,OwnerDirectory(duel),duel.Id});
            object Get(object o,string p)=>o.GetType().GetProperty(p).GetValue(o);
            check((int)Get(restored,"MinimumPositiveNpcRelationGain")==5,"existing shared MCM wins over module business config");
            check(!(bool)Get(restored,"TerminalHotkeyEnabled") && (bool)Get(restored,"TerminalMapIconEnabled"),"missing shared entry fields restore legacy entry only");
            TerminalProviderProbe.Owners[duel.Id]=restored;
            TerminalProviderProbe.OnSave=o=>save.Invoke(format,new object[]{o,OwnerDirectory((BaseSettings)o),((BaseSettings)o).Id});
            terminal.GetProperty("IsHotkeyEnabled").SetValue(null,true);
            var disk=JObject.Parse(File.ReadAllText(mainPath));
            check((bool)disk["TerminalHotkeyEnabled"] && (bool)disk["TerminalMapIconEnabled"],"entry edit persists in same MCM main JSON");
            check(File.ReadAllText(legacy)==legacyBytes,"entry edit never writes legacy terminal file");
            var restarted=(BaseSettings)Activator.CreateInstance(duel.GetType());load.Invoke(format,new object[]{restarted,OwnerDirectory(duel),duel.Id});
            check((bool)Get(restarted,"TerminalHotkeyEnabled"),"restart retains shared entry choice over legacy entry");
            terminal.GetProperty("IsMapIconEnabled").SetValue(null,false);
            terminal.GetProperty("IsHotkeyEnabled").SetValue(null,false);
            check((bool)terminal.GetProperty("IsHotkeyEnabled").GetValue(null),"at least one entry stays available");
            TerminalProviderProbe.OnSave=o=>{throw new IOException("offline save failure");};
            terminal.GetProperty("IsMapIconEnabled").SetValue(null,true);
            check(!(bool)terminal.GetProperty("IsMapIconEnabled").GetValue(null),"entry save failure rolls back memory");
            TerminalProviderProbe.OnSave=o=>save.Invoke(format,new object[]{o,OwnerDirectory((BaseSettings)o),((BaseSettings)o).Id});
            byte[] beforeFailure=File.ReadAllBytes(mainPath);
            files.FailWrites=true;
            terminal.GetProperty("IsMapIconEnabled").SetValue(null,true);
            check(!(bool)terminal.GetProperty("IsMapIconEnabled").GetValue(null) && beforeFailure.SequenceEqual(File.ReadAllBytes(mainPath)),"native MCM false result retains file and rolls back entry");
            files.FailWrites=false;
            var presets=files.GetDirectory(files.GetDirectory(files.GetModSettingsDirectory(),"Presets"),duel.Id);
            check((bool)save.Invoke(format,new object[]{restored,presets,"profile"}) && File.Exists(files.GetSystemPath(new GameFile(presets,"profile.json"))),"preset keeps native per-ID location");
            var foreign=files.GetDirectory(files.GetDirectory(files.GetModSettingsDirectory(),"Global"),"OtherMod");
            check((bool)save.Invoke(format,new object[]{restored,foreign,"foreign"}) && File.Exists(files.GetSystemPath(new GameFile(foreign,"foreign.json"))),"foreign directory is not rerouted");
            check(File.ReadAllText(moduleMain)=="{\"MinimumPositiveNpcRelationGain\":99}","module business config is untouched");
            var previousLookup = TerminalProviderProbe.Lookup;
            try
            {
                TerminalProviderProbe.Lookup = id => discovery.Container.GetSettings(id);
                var pageType = af.GetType("AnimusForge.AnimusForgeMcmPages", true);
                var register = pageType.GetMethod("RegisterPages", All);
                register.Invoke(null, null);
                var ids = new[] { "AnimusForge_Api_UI_v1", "AnimusForge_Terminal_UI_v1" };
                check(ids.All(id => discovery.Container.GetSettings(id) != null), "both views register through actual MCM fluent container");
                check((bool)af.GetType("AnimusForge.AnimusForgeMcmVisibility", true).GetField("RestoredPagesReady", All).GetValue(null), "original entries replaced only after both view registrations succeed");
                register.Invoke(null, null);
                check(discovery.Container.SettingsDefinitions.Count() == 2, "reopening registers exactly two fresh views");
                TerminalProviderProbe.Saved.Clear();
                foreach (string id in ids)
                {
                    var view = discovery.Container.GetSettings(id);
                    check(discovery.Container.SaveSettings(view), "native MCM container saves view " + id);
                    check(!File.Exists(OwnerPath(view)), "view never creates duplicate business JSON " + id);
                }
                check(new HashSet<string>(TerminalProviderProbe.Saved).SetEquals(TerminalProviderProbe.Owners.Keys), "view saves route to four original disk owners");
            }
            finally { TerminalProviderProbe.Lookup = previousLookup; }
        }
        finally
        {
            harmony.Unpatch(userPath,HarmonyPatchType.Prefix,harmony.Id);
            TerminalProviderProbe.Owners[duel.Id]=originalOwner;
            TerminalProviderProbe.OnSave=originalSave;
            providerField.SetValue(null,previous);
        }
    }
}
