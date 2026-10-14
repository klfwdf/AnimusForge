using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;
using Newtonsoft.Json.Linq;

// Actual product setters, MCM discovery/JSON and terminal VMs. Only the provider
// disk boundary is a workspace fixture; no native rendering or HTTP is started.
sealed class Discovery : IGenericServiceProvider
{
    readonly ISettingsPropertyDiscoverer discoverer = (ISettingsPropertyDiscoverer)Activator.CreateInstance(
        typeof(BaseSettings).Assembly.GetType("MCM.Implementation.AttributeSettingsPropertyDiscoverer", true), true);
    public T GetService<T>() where T : class => typeof(T) == typeof(IEnumerable<ISettingsPropertyDiscoverer>) ? new[] { discoverer } as T : null;
    public IGenericServiceProviderScope CreateScope() => null;
    public void Dispose() { }
}
class Program
{
    static int checks;
    static readonly BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static object Get(object o, string p) => o.GetType().GetProperty(p).GetValue(o);
    static void Set(object o, string p, object v) => o.GetType().GetProperty(p).SetValue(o, v);
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL " + name); checks++; }
    static int Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                string name = new AssemblyName(e.Name).Name;
                foreach (string dir in args.Skip(2)) { string path = Path.Combine(dir, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
                return null;
            };
            var af = Assembly.LoadFrom(args[0]);
            string root = (string)af.GetType("AnimusForge.AnimusForgeModulePaths").GetMethod("GetCurrentModuleRoot").Invoke(null, null);
            Check(root == args[1], "all paths resolve within workspace fixture");
            typeof(GenericServiceProvider).GetField("GlobalServiceProvider", All).SetValue(null, new Discovery());
            var providerType = typeof(BaseSettingsProvider);
            providerType.GetField("<Instance>k__BackingField", All).SetValue(null,
                typeof(TerminalSettingsChecks).GetMethod("BuildProvider", All).Invoke(null, new object[] { providerType }));
            foreach (string type in new[] { "AnimusForge.DuelSettings", "AnimusForge.DialogueUI.DialogueUiSettings", "AnimusForge.Illustrator.IllustratorSettings", "AnimusForge.CoupSystem.CoupSettings" })
            {
                var owner = (BaseSettings)Activator.CreateInstance(af.GetType(type, true));
                TerminalProviderProbe.Owners.Add(owner.Id, owner);
            }
            var duel = (BaseSettings)TerminalProviderProbe.Owners["AnimusForge_global_settings"];
            var formatType = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat", true);
            var loggerType = typeof(BaseSettings).Assembly.GetType("BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1", true).MakeGenericType(formatType);
            object format = Activator.CreateInstance(formatType, Activator.CreateInstance(loggerType));
            string Save(BaseSettings o) => (string)formatType.GetMethod("SaveJson").Invoke(format, new object[] { o });
            void Load(BaseSettings o, string json) => formatType.GetMethod("LoadFromJson").Invoke(format, new object[] { o, json });
            string fixture = Path.Combine(root, "Configs", "roundtrip.json"); Directory.CreateDirectory(Path.GetDirectoryName(fixture));
            foreach (int value in new[] { 5, 0, 20 })
            {
                Set(duel, "MinimumPositiveNpcRelationGain", value);
                Set(duel, "EnablePassiveStareReaction", true); Set(duel, "PassiveStareTriggerSeconds", 37);
                string json = Save(duel); var saved = JObject.Parse(json);
                Check((int?)saved["MinimumPositiveNpcRelationGain"] == value, "actual MCM JSON contains gain " + value);
                File.WriteAllText(fixture, json);
                var reloaded = (BaseSettings)Activator.CreateInstance(duel.GetType()); Load(reloaded, File.ReadAllText(fixture));
                Check((int)Get(reloaded, "MinimumPositiveNpcRelationGain") == value, "disk reload retains gain " + value);
                Check((bool)Get(reloaded, "EnablePassiveStareReaction") && (int)Get(reloaded, "PassiveStareTriggerSeconds") == 37, "both stare settings reload");
            }
            var registry = af.GetType("AnimusForge.TerminalSettingsRegistry", true);
            var defs = ((IEnumerable)registry.GetProperty("AllDefinitions").GetValue(null)).Cast<object>().ToDictionary(d => (string)Get(d, "Id"));
            // Verify every terminal control against its actual persistence owner.
            foreach (var pair in defs)
            {
                if (Get(pair.Value, "SettingType").ToString() == "Button") continue;
                string id = pair.Key, prefix = id.StartsWith("Illustrator.") ? "Illustrator." : id.StartsWith("DialogueUi.") ? "DialogueUi." : id.StartsWith("Coup.") ? "Coup." : "";
                var owner = (BaseSettings)(prefix == "" ? duel : TerminalProviderProbe.Owners.Values.Single(o => o.GetType().Name == (prefix == "Illustrator." ? "IllustratorSettings" : prefix == "DialogueUi." ? "DialogueUiSettings" : "CoupSettings")));
                Check(JObject.Parse(Save(owner))[id.Substring(prefix.Length)] != null, "persistent terminal control " + id);
            }
            var popupType = af.GetType("AnimusForge.AnimusForgeTerminalPopupVM", true);
            object popup = Activator.CreateInstance(popupType, new object[] { null, null, null });
            Action modified = () => popupType.GetMethod("MarkModified").Invoke(popup, null);
            Action textModified = () => popupType.GetMethod("MarkTextModified").Invoke(popup, null);
            var itemType = af.GetType("AnimusForge.AnimusForgeTerminalItemVM", true);
            object gain = Activator.CreateInstance(itemType, new object[] { defs["MinimumPositiveNpcRelationGain"], modified, textModified });
            TerminalProviderProbe.Saved.Clear();
            itemType.GetMethod("ExecuteDecrease").Invoke(gain, null);
            Check(TerminalProviderProbe.Saved.Contains(duel.Id) && !(bool)Get(popup, "HasUnsavedChanges"), "numeric edit saves automatically");
            object text = Activator.CreateInstance(itemType, new object[] { defs["ModelName"], modified, textModified });
            TerminalProviderProbe.Saved.Clear();
            Set(text, "TextValue", "offline-first"); Set(text, "TextValue", "offline-final");
            Check(TerminalProviderProbe.Saved.Count == 0 && (bool)Get(popup, "HasUnsavedChanges"), "text typing applies without per-key disk writes");
            itemType.GetMethod("ExecuteCommitText").Invoke(text, null);
            Check(TerminalProviderProbe.Saved.Count == 1 && !(bool)Get(popup, "HasUnsavedChanges"), "text commit saves once");
            itemType.GetMethod("ExecuteCommitText").Invoke(text, null);
            Check(TerminalProviderProbe.Saved.Count == 1, "duplicate enter/focus-loss event adds no disk write");
            Console.WriteLine("PASS " + checks + " actual MCM JSON/disk reload and terminal autosave checks; native UI and live game not run");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
