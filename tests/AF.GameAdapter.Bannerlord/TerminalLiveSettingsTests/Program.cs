using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;
using MCM.Abstractions.GameFeatures;
using Newtonsoft.Json.Linq;

// Actual product setters, MCM discovery/JSON and terminal VMs. Only the provider
// disk boundary is a workspace fixture; no native rendering or HTTP is started.
sealed class Discovery : IGenericServiceProvider
{
    readonly IFileSystemProvider files;
    public Discovery(IFileSystemProvider files = null) { this.files = files; }
    readonly ISettingsPropertyDiscoverer discoverer = (ISettingsPropertyDiscoverer)Activator.CreateInstance(
        typeof(BaseSettings).Assembly.GetType("MCM.Implementation.AttributeSettingsPropertyDiscoverer", true), true);
    readonly ISettingsPropertyDiscoverer fluent = (ISettingsPropertyDiscoverer)Activator.CreateInstance(
        typeof(BaseSettings).Assembly.GetType("MCM.Implementation.FluentSettingsPropertyDiscoverer", true), true);
    internal readonly MCM.Abstractions.Global.IFluentGlobalSettingsContainer Container =
        (MCM.Abstractions.Global.IFluentGlobalSettingsContainer)Activator.CreateInstance(
            typeof(BaseSettings).Assembly.GetType("MCM.Implementation.Global.FluentGlobalSettingsContainer", true), true);
    public T GetService<T>() where T : class => typeof(T) == typeof(IEnumerable<ISettingsPropertyDiscoverer>) ? new[] { discoverer, fluent } as T :
        typeof(T) == typeof(IEnumerable<MCM.Abstractions.Global.IFluentGlobalSettingsContainer>) ? new[] { Container } as T :
        typeof(T) == typeof(IFileSystemProvider) ? files as T : null;
    public IGenericServiceProviderScope CreateScope() => null;
    public void Dispose() { }
}
class Program
{
    sealed class UnsafeArgument
    {
        public int Calls;
        public override string ToString() { Calls++; throw new NullReferenceException("offline diagnostic argument"); }
    }
    static void DiagnosticMarker() { }
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
                if (name == "AnimusForge") return Assembly.LoadFrom(args[0]);
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
#if AF_TTS_ONLY
            SharedMcmChecks.Run(af, duel, root, Check, true);
            TtsVisibilityChecks.Run(af, Check);
#if AF_TTS_VISIBILITY_UI
            TtsVisibilityChecks.NativeUi(af, Assembly.LoadFrom(Path.Combine(args[4], "Bannerlord.MBOptionScreen.v1.4.0.dll")), Check);
#endif
            Console.WriteLine("PASS focused TTS native MCM/terminal checks=" + checks);
            return 0;
#endif
#if AF_BASELINE
            Set(duel, "EnableDeepTrace", true);
            var unsafeArgument = new UnsafeArgument();
            var diagnosticPrefix = af.GetType("AnimusForge.DynamicPatcher", true).GetMethod("UniversalPrefix");
            for (int i = 0; i < 1000; i++) diagnosticPrefix.Invoke(null, new object[] { typeof(Program).GetMethod("DiagnosticMarker", All), new object[] { unsafeArgument } });
            Set(duel, "EnableDeepTrace", false);
            Check(unsafeArgument.Calls == 0, "baseline diagnostic prefix must not call custom ToString; calls=" + unsafeArgument.Calls);
#endif
#if !AF_BASELINE
            SupplementalChecks.Run(af, duel, Check, root);
            SceneTerminalChecks.Run(af, Check);
            SharedMcmChecks.Run(af, duel, root, Check);
#endif
            var formatType = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat", true);
            var loggerType = typeof(BaseSettings).Assembly.GetType("BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1", true).MakeGenericType(formatType);
            object format = Activator.CreateInstance(formatType, Activator.CreateInstance(loggerType));
#if !AF_BASELINE
            IllustratorRetiredApiChecks.Run(af, format, Check);
#endif
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
                string id = pair.Key, prefix = id.StartsWith("Illustrator.") ? "Illustrator." : id.StartsWith("DialogueUi.") ? "DialogueUi." : id.StartsWith("Coup.") ? "Coup." : "";
                var owner = (BaseSettings)(prefix == "" ? duel : TerminalProviderProbe.Owners.Values.Single(o => o.GetType().Name == (prefix == "Illustrator." ? "IllustratorSettings" : prefix == "DialogueUi." ? "DialogueUiSettings" : "CoupSettings")));
                if (Get(pair.Value, "SettingType").ToString() == "Button")
                {
                    Check(Get(pair.Value, "Invoker") != null, "terminal button invoker " + id);
                    Check(owner.GetType().GetProperty(id.Substring(prefix.Length)).GetValue(owner) is Action, "actual owner action bound " + id);
                    continue;
                }
                Check(JObject.Parse(Save(owner))[id.Substring(prefix.Length)] != null, "persistent terminal control " + id);
            }
            var popupType = af.GetType("AnimusForge.AnimusForgeTerminalPopupVM", true);
            object popup = Activator.CreateInstance(popupType, new object[] { null, null, null });
            Action modified = () => popupType.GetMethod("MarkModified").Invoke(popup, null);
            Action textModified = () => popupType.GetMethod("MarkTextModified").Invoke(popup, null);
            var itemType = af.GetType("AnimusForge.AnimusForgeTerminalItemVM", true);
            string OwnerFile(BaseSettings o) => Path.Combine(root, "Configs", o.Id + ".json");
            TerminalProviderProbe.OnSave = o => File.WriteAllText(OwnerFile((BaseSettings)o), Save((BaseSettings)o));
            object gain = Activator.CreateInstance(itemType, new object[] { defs["MinimumPositiveNpcRelationGain"], modified, textModified });
            TerminalProviderProbe.Saved.Clear();
            itemType.GetMethod("ExecuteDecrease").Invoke(gain, null);
            Check(TerminalProviderProbe.Saved.Contains(duel.Id) && !(bool)Get(popup, "HasUnsavedChanges"), "numeric edit saves automatically");
            var afterNumericRestart = (BaseSettings)Activator.CreateInstance(duel.GetType());
            Load(afterNumericRestart, File.ReadAllText(OwnerFile(duel)));
            Check(Equals(Get(afterNumericRestart, "MinimumPositiveNpcRelationGain"), Get(duel, "MinimumPositiveNpcRelationGain")), "numeric edit to automatic provider write to restart reload");
            object text = Activator.CreateInstance(itemType, new object[] { defs["ModelName"], modified, textModified });
            TerminalProviderProbe.Saved.Clear();
            Set(text, "TextValue", "offline-first"); Set(text, "TextValue", "offline-final");
            Check(TerminalProviderProbe.Saved.Count == 0 && (bool)Get(popup, "HasUnsavedChanges"), "text typing applies without per-key disk writes");
            itemType.GetMethod("ExecuteCommitText").Invoke(text, null);
            Check(TerminalProviderProbe.Saved.Count == 1 && !(bool)Get(popup, "HasUnsavedChanges"), "text commit saves once");
            var afterTextRestart = (BaseSettings)Activator.CreateInstance(duel.GetType());
            Load(afterTextRestart, File.ReadAllText(OwnerFile(duel)));
            Check((string)Get(afterTextRestart, "ModelName") == "offline-final", "text commit to automatic provider write to restart reload");
            itemType.GetMethod("ExecuteCommitText").Invoke(text, null);
            Check(TerminalProviderProbe.Saved.Count == 1, "duplicate enter/focus-loss event adds no disk write");
            var runtime = af.GetType("AnimusForge.Illustrator.Core.IllustratorRuntime", true);
            runtime.GetField("_mainThread", All).SetValue(null, Environment.CurrentManagedThreadId);
            var helper = af.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
            var prepare = helper.GetMethod("PrepareNativeCaptureOnMainThread", All);
            var queue = runtime.GetField("FrameWaiters", All).GetValue(null);
            int QueueCount() => (int)Get(queue, "Count");
            var last = helper.GetField("_previousCaptureStage", All);
            var pumpType = helper.GetNestedType("OffscreenStagePump", All);
            var stale = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(pumpType);
            last.SetValue(null, Activator.CreateInstance(last.FieldType, stale));
            using (var cancelled = new CancellationTokenSource())
            {
                cancelled.Cancel(); int before = QueueCount(); bool rejected = false;
                try { prepare.Invoke(null, new object[] { cancelled.Token }); }
                catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException) { rejected = true; }
                Check(rejected && QueueCount() == before && last.GetValue(null) != null, "pre-cancelled capture neither cleans nor admits work");
            }
            long startFrame = (long)runtime.GetField("_frame", All).GetValue(null);
            var ready = (Task)prepare.Invoke(null, new object[] { CancellationToken.None });
            Check(last.GetValue(null) == null && !ready.IsCompleted && QueueCount() == 1, "cleanup retires stale tracking before admission and waits for frames");
            object waiter = queue.GetType().GetMethod("Dequeue").Invoke(queue, null);
            Check((long)Get(waiter, "Key") == startFrame + 2, "new capture requires two subsequent application frames");
            // Advance the managed frame waiter only. Never call the runtime Tick,
            // which also owns live UI/game work; this is not a GPU-completion test.
            Get(waiter, "Value").GetType().GetMethod("TrySetResult").Invoke(Get(waiter, "Value"), new object[] { true });
            Check(ready.Wait(3000), "frame admission completes after managed scheduler acknowledgement");
            if (args.Length > 4)
            {
                var ui = Assembly.LoadFrom(Path.Combine(args[4], "Bannerlord.MBOptionScreen.v1.4.0.dll"));
#if !AF_BASELINE
                McmPageChecks.NativeUi(af, ui, Check);
#endif
                var hostType = ui.GetType("MCM.UI.GUI.ViewModels.SettingsPropertyVM", true);
                var refresh = af.GetType("AnimusForge.McmDropdownRuntimeRefresh", true).GetMethod("TryRefreshDropdown", All);
                foreach (var owner in new[] { duel, (BaseSettings)TerminalProviderProbe.Owners["AnimusForge_Illustrator_v1"] })
                foreach (var definition in owner.GetAllSettingPropertyDefinitions().Where(d => new[] { "MainModelDropdown", "ModelDropdown", "SizePresetDropdown", "QualityDropdown", "StyleDropdown" }.Contains(d.Id)))
                {
                    object host = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(hostType);
                    hostType.GetField("<SettingPropertyDefinition>k__BackingField", All).SetValue(host, definition);
                    object original = Get(host, "DropdownValue");
                    refresh.Invoke(null, new[] { host });
                    object selector = Get(host, "DropdownValue"), selected = Get(selector, "SelectedItem");
                    Check(selected != null && !string.IsNullOrWhiteSpace((string)Get(selected, "StringItem")), "actual MCM property host renders " + definition.Id);
                    Check(((IList)Get(original, "ItemList")).Count > 0, "old MCM binding remains populated during detach " + definition.Id);
                }
            }
            Console.WriteLine("PASS " + checks + " actual MCM JSON/disk reload and terminal autosave checks; native UI and live game not run");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
