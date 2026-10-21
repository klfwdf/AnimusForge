using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;

// A controlled MCM provider around the actual four product settings types.
// Metadata, original setters, terminal definitions and native UI localization are real;
// SaveSettings records routing/failure, without writing the game configuration.
public static class TerminalProviderProbe
{
    public static readonly Dictionary<string, object> Owners = new Dictionary<string, object>();
    public static readonly List<string> Saved = new List<string>();
    public static string FailId;
    public static Action<object> OnSave;
    public static Func<string, object> Lookup;
    public static object Get(string id) => Owners.TryGetValue(id, out object value) ? value : Lookup?.Invoke(id);
    public static void Save(object owner)
    {
        string id = (string)owner.GetType().GetProperty("Id").GetValue(owner);
        if (id == FailId) throw new InvalidOperationException("Controlled save failure");
        OnSave?.Invoke(owner);
        Saved.Add(id);
    }
}

internal static class TerminalSettingsChecks
{
    const BindingFlags S = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    static int checks;
    static void Check(bool value, string name) { checks++; if (!value) throw new Exception("FAIL terminal " + name); }
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(Type type, string name, params object[] values)
        => type.GetMethods(S).Single(m => m.Name == name && m.GetParameters().Length == values.Length).Invoke(null, values);
    internal static void Run(Assembly product, string moduleRoot)
    {
        var mcm = Assembly.Load("MCMv5");
        Type provider = mcm.GetType("MCM.Abstractions.BaseSettingsProvider", true);
        var instance = provider.GetField("<Instance>k__BackingField", S);
        object prior = instance.GetValue(null);
        try
        {
            instance.SetValue(null, BuildProvider(provider));
            var types = new[] { "AnimusForge.DuelSettings", "AnimusForge.DialogueUI.DialogueUiSettings",
                "AnimusForge.Illustrator.IllustratorSettings", "AnimusForge.CoupSystem.CoupSettings" };
            foreach (string name in types)
            {
                object owner = Activator.CreateInstance(product.GetType(name, true));
                TerminalProviderProbe.Owners.Add((string)Property(owner, "Id"), owner);
            }
            Type registry = product.GetType("AnimusForge.TerminalSettingsRegistry", true);
            Type localization = Assembly.Load("TaleWorlds.Localization").GetType("TaleWorlds.Localization.MBTextManager", true);
            Type metadata = mcm.GetType("MCM.Abstractions.Attributes.BaseSettingPropertyAttribute", true);
            object duel = TerminalProviderProbe.Owners["AnimusForge_global_settings"];
            foreach (string language in new[] { "English", "简体中文", "English" })
            {
                localization.GetMethod("ChangeLanguage").Invoke(null, new object[] { language });
                var defs = ((IEnumerable)registry.GetProperty("AllDefinitions").GetValue(null)).Cast<object>().ToDictionary(d => (string)Property(d, "Id"));
                int attributed = 0;
                foreach (string name in types)
                {
                    Type type = product.GetType(name, true);
                    object owner = TerminalProviderProbe.Owners.Values.Single(o => o.GetType() == type);
                    string prefix = type.Name == "DuelSettings" ? "" : type.Name == "DialogueUiSettings" ? "DialogueUi." : type.Name == "IllustratorSettings" ? "Illustrator." : "Coup.";
                    foreach (PropertyInfo property in type.GetProperties())
                    {
                        var attribute = property.GetCustomAttributes(metadata, true).SingleOrDefault();
                        if (attribute == null) continue;
                        attributed++;
                        string id = prefix + property.Name;
                        Check(defs.ContainsKey(id), "every attributed property has a real definition " + id);
                        object def = defs[id];
                        object raw = property.GetValue(owner);
                        var getter = (Delegate)Property(def, "Getter");
                        var setter = Property(def, "Setter") as Delegate;
                        string kind = Property(def, "SettingType").ToString();
                        if (kind == "Button") { Check(Property(def, "Invoker") != null, "original action connected " + id); continue; }
                        object shown = getter.DynamicInvoke(duel);
                        if (kind == "Dropdown")
                        {
                            int count = (int)Property(raw, "Count");
                            int index = (int)Property(raw, "SelectedIndex");
                            if (count > 1) {
                                int next = (index + 1) % count;
                                bool completion = setter.Method.DeclaringType.FullName.IndexOf("TerminalSettingsCoverage", StringComparison.Ordinal) >= 0;
                                // Model selectors can normalize manual selection
                                // back to the stored model. Use the native setter
                                // as the control instead of changing its semantics.
                                int expectedIndex = next;
                                if (completion) {
                                    raw.GetType().GetProperty("SelectedIndex").SetValue(raw, next);
                                    property.SetValue(owner, raw);
                                    expectedIndex = (int)Property(property.GetValue(owner), "SelectedIndex");
                                    object reset = property.GetValue(owner);
                                    reset.GetType().GetProperty("SelectedIndex").SetValue(reset, index);
                                    property.SetValue(owner, reset);
                                }
                                setter.DynamicInvoke(duel, next);
                                Check((int)Property(property.GetValue(owner), "SelectedIndex") == expectedIndex,
                                    (completion ? "new completion preserves native setter normalization " : "existing terminal selection remains connected ") + id);
                                setter.DynamicInvoke(duel, index);
                            }
                            Check(Property(def, "DropdownGetter") != null, "dropdown editor bound " + id);
                        }
                        else
                        {
                            Check(Equals(raw, shown), "getter reads its original owner " + id);
                            object next = raw is bool b ? (object)!b : raw is int n ? n == 0 ? 1 : n-1 : raw is float f ? f+0.05f : raw is string str ? str+"_REPLAY" : null;
                            Check(next != null, "supported original property type " + id);
                            setter.DynamicInvoke(duel, next);
                            Check(Equals(property.GetValue(owner), next), "setter writes its original owner " + id);
                            setter.DynamicInvoke(duel, raw);
                        }
                    }
                }
                var terminalOnly = new Dictionary<string, object> {
                    ["EnablePassiveStareReaction"] = false,
                    ["PassiveStareTriggerSeconds"] = 0,
                    ["MinimumPositiveNpcRelationGain"] = 1
                };
                Check(defs.Count == attributed, "no missing or duplicate attributed or terminal-only definitions");
                foreach (var setting in terminalOnly)
                {
                    Check(defs.ContainsKey(setting.Key), "terminal-only definition exists " + setting.Key);
                    object def = defs[setting.Key];
                    PropertyInfo property = duel.GetType().GetProperty(setting.Key);
                    Check(property.GetCustomAttributes(metadata, true).Length == 1, "terminal control has persistence metadata " + setting.Key);
                    object raw = property.GetValue(duel);
                    Check(Equals(raw, ((Delegate)Property(def, "Getter")).DynamicInvoke(duel)), "terminal-only getter retains owner " + setting.Key);
                    object next = raw is bool value ? (object)!value : ((int)raw == 0 ? 1 : 0);
                    var setter = (Delegate)Property(def, "Setter");
                    setter.DynamicInvoke(duel, next);
                    Check(Equals(property.GetValue(duel), next), "terminal-only setter retains owner " + setting.Key);
                    setter.DynamicInvoke(duel, raw);
                }
                var ui = ((IEnumerable)Call(registry, "GetByCategory", "UI")).Cast<object>().Select(d => (string)Property(d, "Id")).ToArray();
                foreach (string id in new[] { "CourierLetterThemeDropdown", "DialogueUi.InterfaceStyleDropdown", "DialogueUi.BodyFontSize", "DialogueUi.ShoutPanelStyleDropdown", "DialogueUi.ShowHistoryDelete", "BubbleFontSize" })
                    Check(ui.Contains(id), "UI page contains " + id);
                var diplomacy = ((IEnumerable)Call(registry, "GetByCategory", (string)product.GetType("AnimusForge.TerminalCategories", true).GetField("Diplomacy", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null))).Cast<object>().Select(d => (string)Property(d, "Id")).ToArray();
                foreach (string id in new[] { "WeeklyReportPopupBodyFontSize", "EnableWeeklyReportMapNotifications" })
                    Check(diplomacy.Contains(id) && !ui.Contains(id), "complete weekly settings stay on Diplomacy " + id);
                Type textObject = Assembly.Load("TaleWorlds.Localization").GetType("TaleWorlds.Localization.TextObject", true);
                var visible = ((IEnumerable)registry.GetProperty("VisibleDefinitions").GetValue(null)).Cast<object>().ToDictionary(def => (string)Property(def, "Id"));
                string evolutionId = "EnableNpcPersonaEventEvolution";
                Check(visible.ContainsKey(evolutionId) && diplomacy.Contains(evolutionId), "persona evolution is visible on terminal diplomacy page");
                Check((string)Property(visible[evolutionId], "DisplayName") ==
                    (language == "简体中文" ? "事件驱动的 NPC 人设演变" : "Event-driven NPC persona evolution"), "persona evolution title follows UI language");
                foreach (string id in new[] { "ScreenNotifications", "DiagnosticsEnabled", "EnemyInterruptRadiusMeters" })
                    Check(defs.ContainsKey(id) && !visible.ContainsKey(id), "hidden UI setting still exists for persistence " + id);
                foreach (string id in new[] { "ConversationPromptCacheEnabled", "ConversationPromptCacheMaxChars", "ConversationPromptCacheTtlMinutes", "TownAmbientAiShowUsageNotifications", "Illustrator.UseExactEndpointUrl", "Illustrator.DirectorApiBaseUrl", "Illustrator.DirectorApiKey", "Illustrator.DirectorModelName", "Illustrator.DirectorModelDropdown", "Illustrator.FetchDirectorModelList" })
                    Check(!defs.ContainsKey(id) && !visible.ContainsKey(id), "retired API setting absent from metadata and frontend " + id);
                foreach (string id in new[] { "Illustrator.ApiBaseUrl", "Illustrator.ApiKey", "Illustrator.ModelName", "Illustrator.FetchModelList" })
                {
                    Check(visible.ContainsKey(id), "image API visible " + id);
                    string ai = (string)product.GetType("AnimusForge.TerminalCategories", true).GetField("Ai", S).GetValue(null);
                    Check((string)Property(visible[id], "TabCategory") == ai, "image API on AI core " + id);
                }
                string directorId = "Illustrator.EditCustomDirectorPrompt";
                Check(visible.ContainsKey(directorId) && (string)Property(visible[directorId], "TabCategory") ==
                    (string)product.GetType("AnimusForge.TerminalCategories", true).GetField("Ai", S).GetValue(null), "director rule editor on image API page");
                var imageDefinitions = defs.Where(pair => pair.Key.StartsWith("Illustrator.", StringComparison.Ordinal)).ToList();
                string imageApiCategory = (string)Property(defs["MainApiMaxTokens"], "TabCategory");
                string imageApiGroup = (string)Property(defs["Illustrator.ApiBaseUrl"], "GroupName");
                Type imageOwner = product.GetType("AnimusForge.Illustrator.IllustratorSettings", true);
                Type mcmVisibility = product.GetType("AnimusForge.AnimusForgeMcmVisibility", true);
                foreach (var pair in imageDefinitions)
                {
                    Check(visible.ContainsKey(pair.Key) && (string)Property(pair.Value, "TabCategory") == imageApiCategory
                        && (string)Property(pair.Value, "GroupName") == imageApiGroup, "all image settings visible in one image API group: " + pair.Key);
                    PropertyInfo property = imageOwner.GetProperty(pair.Key.Substring("Illustrator.".Length));
                    object groupAttribute = property.GetCustomAttributes(true).Single(a => a.GetType().Name == "SettingPropertyGroupAttribute");
                    string rawGroup = (string)Property(groupAttribute, "GroupName");
                    Check(rawGroup.Contains("AF_ILL_GROUP_IMAGE_API") && (bool)Call(mcmVisibility,"IsVisibleProperty","AnimusForge_Illustrator_v1",property.Name,rawGroup),"native MCM image API admission: " + property.Name);
                }
                Check(visible.Where(pair => pair.Key.StartsWith("Coup.")).Select(pair => Property(pair.Value, "GroupName")).Distinct().Count() == 1, "one armed coup item");
                Check(visible.Where(pair => !pair.Key.StartsWith("Illustrator.", StringComparison.Ordinal) && (pair.Key.Contains("WeeklyReport") || pair.Key.Contains("WorldBulletin"))).Select(pair => Property(pair.Value, "GroupName")).Distinct().Count() == 1, "one weekly and bulletin item, image options belong to image API");
                Check(visible.Where(pair => pair.Key.StartsWith("WorldDiplomacy") || pair.Key.StartsWith("EnableWorldDiplomacy") || pair.Key.StartsWith("EditWorldDiplomacy")).Select(pair => Property(pair.Value, "GroupName")).Distinct().Count() == 2, "two AI diplomacy items");
                Check((string)Property(visible["NaturalLanguageReplyActionsEnabled"], "GroupName") == (string)Property(visible["BattleSpeechEnabled"], "GroupName")
                    && (string)Property(visible["AudienceRepliesEnabled"], "GroupName") == (string)Property(visible["BattleSpeechEnabled"], "GroupName"), "natural actions share one root item");
                string courierGroup = "{=AF_MCM_Group_CourierStationery}NPC contact / courier stationery";
                Check((string)Property(defs["CourierLetterThemeDropdown"], "GroupName") == Activator.CreateInstance(textObject, new object[] { courierGroup, null }).ToString(), "slashes inside a localized group fallback do not leak untranslated suffixes");
                string path = System.IO.Path.Combine(moduleRoot, "ModuleData/Languages", language == "English" ? "af_ui_strings.xml" : "CNs/af_ui_strings-zh-CN.xml");
                string expected = XDocument.Load(path).Descendants("string").Single(e => (string)e.Attribute("id") == "AF_UI_Terminal_Ui").Attribute("text").Value;
                Type categories = product.GetType("AnimusForge.TerminalCategories", true);
                string text = (string)Call(categories, "DisplayText", "UI");
                Check(Activator.CreateInstance(textObject, new object[] { text, null }).ToString() == expected, "native UI page label " + language);
                PropertyInfo devStatus = duel.GetType().GetProperty("EnableDevEditHistory");
                object originalStatus = devStatus.GetValue(duel);
                Type itemVm = product.GetType("AnimusForge.AnimusForgeTerminalItemVM", true);
                foreach (bool enabled in new[] { false, true })
                {
                    devStatus.SetValue(duel, enabled);
                    object item = Activator.CreateInstance(itemVm, new object[] { defs["EnableDevEditHistory"], null });
                    string status = language == "English" ? (enabled ? "✔ Enabled" : "✕ Disabled") : (enabled ? "✔ 已开启" : "✕ 已关闭");
                    Check((string)Property(item, "BoolStatusText") == status, "actual terminal boolean state label " + language);
                }
                devStatus.SetValue(duel, originalStatus);
                Console.WriteLine("Terminal actual metadata: " + defs.Count + " definitions, " + ui.Length + " UI settings, language=" + language);
            }
            PropertyInfo developer = duel.GetType().GetProperty("EnableDevEditHistory");
            bool oldDeveloper = (bool)developer.GetValue(duel);
            int clicks = 0;
            var callback = (Action)(() => clicks++);
            Type sceneVm = product.GetType("AnimusForge.DialogueUI.Scene.SceneSessionVM", true);
            object sceneModel = Activator.CreateInstance(sceneVm, new object[] { callback });
            Type nativeVm = product.GetType("AnimusForge.AnimusForgeNativeConversationOverlayVM", true);
            object nativeModel = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(nativeVm);
            nativeVm.GetField("_onDirectActions", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(nativeModel, callback);
            nativeVm.GetProperty("CanOpenDirectActions").SetValue(nativeModel, true);
            Type wrapperVm = product.GetType("AnimusForge.DialogueUI.Native.NativeOverlayVM", true);
            object wrapperModel = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(wrapperVm);
            wrapperVm.GetField("<Original>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(wrapperModel, nativeModel);
            foreach (bool enabled in new[] { false, true, false })
            {
                developer.SetValue(duel, enabled);
                Check((bool)sceneVm.GetProperty("IsDirectActionsVisible").GetValue(sceneModel) == enabled, "scene manual tags developer visibility");
                Check((bool)nativeVm.GetProperty("IsDirectActionsVisible").GetValue(nativeModel) == enabled, "native manual tags developer visibility");
                Check((bool)wrapperVm.GetProperty("IsDirectActionsVisible").GetValue(wrapperModel) == enabled, "displayed native wrapper manual tags developer visibility");
                int previous = clicks;
                nativeVm.GetMethod("OpenDirectActions").Invoke(nativeModel, null);
                Check(clicks == previous + (enabled ? 1 : 0), "native manual tags developer execution guard");
                sceneVm.GetMethod("ExecuteDirectActions").Invoke(sceneModel, null);
                ((Action)sceneVm.GetMethod("TakePending", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(sceneModel, null))?.Invoke();
                Check(clicks == previous + (enabled ? 2 : 0), "scene manual tags developer execution guard");
            }
            developer.SetValue(duel, oldDeveloper);
            TerminalProviderProbe.Saved.Clear();
            TerminalProviderProbe.FailId = "AnimusForge_DialogueUI_v1";
            Check(!(bool)Call(registry, "SaveSettings"), "partial owner save failure is reported");
            Check(TerminalProviderProbe.Saved.Contains("AnimusForge_Illustrator_v1") && TerminalProviderProbe.Saved.Contains("AnimusForge_Coup_v1"), "other dirty owners still save");
            TerminalProviderProbe.Saved.Clear(); TerminalProviderProbe.FailId = null;
            Check((bool)Call(registry, "SaveSettings"), "failed dirty owner can retry");
            Check(TerminalProviderProbe.Saved.Contains("AnimusForge_DialogueUI_v1"), "failed owner is retained for retry");
            Check(!TerminalProviderProbe.Saved.Contains("AnimusForge_Illustrator_v1") && !TerminalProviderProbe.Saved.Contains("AnimusForge_Coup_v1"), "successfully saved owners are not repeatedly saved");
            Console.WriteLine("PASS actual terminal settings: " + checks + " checks; controlled provider persistence routing, live Gauntlet NOT RUN.");
        }
        finally { instance.SetValue(null, prior); TerminalProviderProbe.Owners.Clear(); TerminalProviderProbe.Saved.Clear(); TerminalProviderProbe.FailId=null; TerminalProviderProbe.OnSave=null; }
    }

    static object BuildProvider(Type provider)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TerminalProviderFixture"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Fixture").DefineType("Provider", TypeAttributes.Public, provider);
        foreach (MethodInfo method in provider.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.IsAbstract))
        {
            Type[] parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var implementation = builder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, method.ReturnType, parameters);
            ILGenerator il = implementation.GetILGenerator();
            if (method.Name == "GetSettings") {
                il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, typeof(TerminalProviderProbe).GetMethod("Get")); il.Emit(OpCodes.Castclass, method.ReturnType);
            } else if (method.Name == "SaveSettings") {
                il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, typeof(TerminalProviderProbe).GetMethod("Save"));
            } else if (method.ReturnType != typeof(void)) {
                if (method.ReturnType.IsValueType) { var temp=il.DeclareLocal(method.ReturnType); il.Emit(OpCodes.Ldloca_S,temp); il.Emit(OpCodes.Initobj,method.ReturnType); il.Emit(OpCodes.Ldloc,temp); }
                else il.Emit(OpCodes.Ldnull);
            }
            il.Emit(OpCodes.Ret); builder.DefineMethodOverride(implementation, method);
        }
        return Activator.CreateInstance(builder.CreateType());
    }
}
