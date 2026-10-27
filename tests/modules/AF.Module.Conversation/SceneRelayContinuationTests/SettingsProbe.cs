using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using Newtonsoft.Json.Linq;

// Real compiled setting, native MCM discovery/formatter and terminal delegates.
// Provider storage and module assets are isolated fixtures, not player settings.
internal static class SceneRelaySettingsProbe
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("FAIL " + message); checks++; Console.WriteLine("PASS " + message); }
    private static object Get(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    private static int Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                string name = new AssemblyName(e.Name).Name;
                if (name == "AnimusForge") return Assembly.LoadFrom(args[0]);
                foreach (string dir in args.Skip(2)) {
                    string path = Path.Combine(dir, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            var af = Assembly.LoadFrom(args[0]);
            var localization = Assembly.Load("TaleWorlds.Localization");
            localization.GetType("TaleWorlds.Localization.LocalizedTextManager", true)
                .GetMethod("LoadLocalizationXmls").Invoke(null, new object[] {new[] {args[1]}});
            typeof(GenericServiceProvider).GetField("GlobalServiceProvider", All).SetValue(null, new Discovery());
            var provider = typeof(BaseSettingsProvider);
            provider.GetField("<Instance>k__BackingField", All).SetValue(null,
                typeof(TerminalSettingsChecks).GetMethod("BuildProvider", All).Invoke(null, new object[] {provider}));
            foreach (string name in new[] {"AnimusForge.DuelSettings", "AnimusForge.DialogueUI.DialogueUiSettings", "AnimusForge.Illustrator.IllustratorSettings", "AnimusForge.CoupSystem.CoupSettings"}) {
                var owner = (BaseSettings)Activator.CreateInstance(af.GetType(name, true));
                TerminalProviderProbe.Owners.Add(owner.Id, owner);
            }
            var duel = (BaseSettings)TerminalProviderProbe.Owners["AnimusForge_global_settings"];
            const string id = "ShoutMaxRelayTurns";
            var property = duel.GetType().GetProperty(id);
            var native = duel.GetAllSettingPropertyDefinitions();
            Check(native.Count(x => x.Id == id) == 1, "native MCM discovers exactly one relay limit on original owner");
            Check((int)property.GetValue(duel) == 8, "default relay limit remains eight");
            var attribute = property.GetCustomAttributes(true).Single(x => x.GetType().Name == "SettingPropertyIntegerAttribute");
            Check(Convert.ToInt32(Get(attribute,"MinValue")) == 1 && Convert.ToInt32(Get(attribute,"MaxValue")) == 8, "native MCM range is one through eight");
            Check(!(bool)Get(attribute,"RequireRestart"), "no restart required");
            var registry = af.GetType("AnimusForge.TerminalSettingsRegistry", true);
            var language = localization.GetType("TaleWorlds.Localization.MBTextManager", true).GetMethod("ChangeLanguage");
            foreach (string name in new[] {"English", "简体中文"}) {
                language.Invoke(null, new object[] {name});
                var definitions = ((IEnumerable)registry.GetProperty("AllDefinitions").GetValue(null)).Cast<object>().ToArray();
                var def = definitions.Single(x => (string)Get(x,"Id") == id);
                var scene = definitions.Single(x => (string)Get(x,"Id") == "AutoExcludeUnframedShoutParticipants");
                Check((string)Get(def,"TabCategory") == (string)Get(scene,"TabCategory"), "terminal scene category " + name);
                Check((float)Get(def,"MinValue") == 1 && (float)Get(def,"MaxValue") == 8 && (float)Get(def,"StepValue") == 1, "terminal range and integer step " + name);
                Check(((IEnumerable)registry.GetProperty("VisibleDefinitions").GetValue(null)).Cast<object>().Any(x => (string)Get(x,"Id") == id), "terminal entry visible " + name);
                Check(((string)Get(def,"DisplayName")).Contains(name == "English" ? "relay turns" : "最大接力轮数"), "actual localized terminal title " + name);
                Check(((string)Get(def,"HintText")).Contains(name == "English" ? "including the first reply" : "包含首次回复"), "actual localized terminal counting hint " + name);
                foreach (int value in new[] {1,3,8,0,99}) {
                    ((Delegate)Get(def,"Setter")).DynamicInvoke(duel,value);
                    int expected = Math.Max(1,Math.Min(8,value));
                    Check((int)property.GetValue(duel) == expected && (int)((Delegate)Get(def,"Getter")).DynamicInvoke(duel) == expected,
                        "terminal writes original clamped owner " + name + " " + value);
                }
            }
            var formatterType = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat", true);
            var logger = typeof(BaseSettings).Assembly.GetType("BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1", true).MakeGenericType(formatterType);
            var formatter = Activator.CreateInstance(formatterType, Activator.CreateInstance(logger));
            string Save() => (string)formatterType.GetMethod("SaveJson").Invoke(formatter,new object[]{duel});
            var saved = JObject.Parse(Save());
            foreach (int value in new[] {1,5,8}) {
                property.SetValue(duel,value);
                string path = Path.Combine(args[1],"relay-"+value+".json"); File.WriteAllText(path,Save());
                var loaded = (BaseSettings)Activator.CreateInstance(duel.GetType());
                formatterType.GetMethod("LoadFromJson").Invoke(formatter,new object[]{loaded,File.ReadAllText(path)});
                Check((int)property.GetValue(loaded) == value, "real MCM formatter disk roundtrip " + value);
            }
            var field = saved.Descendants().OfType<JProperty>().Single(x => x.Name == id); field.Remove();
            var legacy = (BaseSettings)Activator.CreateInstance(duel.GetType());
            formatterType.GetMethod("LoadFromJson").Invoke(formatter,new object[]{legacy,saved.ToString()});
            Check((int)property.GetValue(legacy) == 8, "old settings without new field keep default eight");
            Console.WriteLine("Checks="+checks+"; actual DLL/MCM/terminal/CN+EN; no game/player settings/HTTP");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
