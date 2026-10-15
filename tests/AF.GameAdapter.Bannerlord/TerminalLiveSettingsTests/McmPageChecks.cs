using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using AnimusForge;

// Real native fluent discovery, owner PropertyRefs and preset cloning; no renderer.
static class McmPageChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    internal static void NativeUi(Assembly af, Assembly ui, Action<bool, string> check)
    {
        var pages = (FluentGlobalSettings[])af.GetType("AnimusForge.AnimusForgeMcmPages", true)
            .GetMethod("CreatePages", All).Invoke(null, new object[] { TerminalProviderProbe.Owners.Values.Cast<BaseSettings>().ToArray() });
        var patch = new HarmonyLib.Harmony("af.tests.mcm.pages");
        af.GetType("AnimusForge.AnimusForgeMcmVisibility", true).GetMethod("EnsurePatched", All).Invoke(null, new object[] { patch });
        af.GetType("AnimusForge.AnimusForgeMcmPages", true).GetMethod("EnsurePatched", All).Invoke(null, new object[] { patch });
        var vmType = ui.GetType("MCM.UI.GUI.ViewModels.SettingsVM", true);
        var groupType = ui.GetType("MCM.UI.GUI.ViewModels.SettingsPropertyGroupVM", true);
        var comparer = ui.GetType("MCM.UI.Utils.UISettingsUtils", true).GetField("SettingsPropertyGroupVMComparer", All).GetValue(null);
        var compare = comparer.GetType().GetMethod("Compare");
        object Group(SettingsPropertyGroupDefinition definition)
        {
            object group = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(groupType);
            groupType.GetField("<SettingPropertyGroupDefinition>k__BackingField", All).SetValue(group, definition);
            return group;
        }
        try
        {
            foreach (var page in pages) TerminalProviderProbe.Owners.Add(page.Id, page);
            foreach (var page in pages)
            {
                object vm = Activator.CreateInstance(vmType, new object[] { new SettingsDefinition(page.Id), null });
                check((string)vmType.GetProperty("DisplayName").GetValue(vm) == page.DisplayName, "real SettingsVM constructs and renders restored page");
                var props = page.GetAllSettingPropertyDefinitions().ToDictionary(p => p.Id);
                if (props.TryGetValue("MinimumPositiveNpcRelationGain", out var relation))
                {
                    object old = relation.PropertyReference.Value;
                    object stack = vmType.GetProperty("URS").GetValue(vm);
                    var actionType = ui.GetType("MCM.UI.Actions.SetValueTypeAction`1", true).MakeGenericType(typeof(int));
                    object action = Activator.CreateInstance(actionType, new object[] { relation.PropertyReference, 8 });
                    stack.GetType().GetMethod("Do").Invoke(stack, new[] { action });
                    check((int)relation.PropertyReference.Value == 8, "native MCM action edits original owner");
                    stack.GetType().GetMethod("UndoAll").Invoke(stack, null);
                    check(Equals(old, relation.PropertyReference.Value), "native MCM cancel restores original owner");
                    vmType.GetMethod("ResetSettingsValue").Invoke(vm, new object[] { relation.Id });
                    check(Equals(relation.PropertyReference.Value, new DuelSettings().MinimumPositiveNpcRelationGain), "native reset reads original default");
                    stack.GetType().GetMethod("UndoAll").Invoke(stack, null);
                }
                if (props.ContainsKey("Illustrator.ApiBaseUrl"))
                {
                    var image = page.GetSettingPropertyGroups().Single(g => SettingsUtils.GetAllSettingPropertyDefinitions(g).Any(p => p.Id == "Illustrator.ApiBaseUrl"));
                    var ambient = page.GetSettingPropertyGroups().Single(g => SettingsUtils.GetAllSettingPropertyDefinitions(g).Any(p => p.Id == "TownAmbientAiApiUrl"));
                    check((int)compare.Invoke(comparer, new[] { Group(image), Group(ambient) }) < 0, "real MCM UI comparer puts image above ambient");
                }
                vmType.GetMethod("OnFinalize").Invoke(vm, null);
            }
            McmReopenLifecycleChecks.Run(af, ui, pages, check);
        }
        finally { foreach (var page in pages) TerminalProviderProbe.Owners.Remove(page.Id); }
    }
    internal static void Run(Assembly af, Action<bool, string> check, string language)
    {
        var owners = TerminalProviderProbe.Owners.Values.Cast<BaseSettings>().ToArray();
        var pages = (FluentGlobalSettings[])af.GetType("AnimusForge.AnimusForgeMcmPages", true)
            .GetMethod("CreatePages", All).Invoke(null, new object[] { owners });
        check(pages.Length == 2 && pages.All(p => p.FormatType == "none"), "two MCM views have no business formatter");
        check(pages[1].DisplayName == (language == "English" ? "AnimusForge terminal settings" : "AnimusForge终端设置"), "localized independent terminal page");
        var api = pages[0].GetAllSettingPropertyDefinitions().ToDictionary(p => p.Id);
        var terminal = pages[1].GetAllSettingPropertyDefinitions().ToDictionary(p => p.Id);
        var visible = TerminalSettingsRegistry.VisibleDefinitions.ToDictionary(p => p.Id);
        check(api.Count + terminal.Count == visible.Count && !api.Keys.Intersect(terminal.Keys).Any(), "every visible terminal setting appears in exactly one MCM page");
        check(api.ContainsKey("Illustrator.ApiBaseUrl") && api.ContainsKey("TownAmbientAiApiUrl"), "image and ambient APIs in same API page");
        check(!api.ContainsKey("Illustrator.UseExactEndpointUrl") && !terminal.ContainsKey("Illustrator.UseExactEndpointUrl"), "exact URL UI remains hidden");
        // MCM UI sorts ascending; backend discovery deliberately sorts descending.
        var ordered = pages[0].GetSettingPropertyGroups().OrderBy(g => g.Order).ToArray();
        int image = Array.FindIndex(ordered, g => SettingsUtils.GetAllSettingPropertyDefinitions(g).Any(p => p.Id == "Illustrator.ApiBaseUrl"));
        int ambient = Array.FindIndex(ordered, g => SettingsUtils.GetAllSettingPropertyDefinitions(g).Any(p => p.Id == "TownAmbientAiApiUrl"));
        check(image >= 0 && ambient > image, "native MCM group order puts image above ambient");
        var coverage = af.GetType("AnimusForge.TerminalSettingsCoverage", true);
        int Order(string id) => (int)coverage.GetMethod("ViewGroupOrder", All).Invoke(null, new object[] { id });
        check(Order("Illustrator.ApiBaseUrl") < Order("TownAmbientAiApiUrl"), "terminal image group above ambient");
        foreach (var owner in owners)
        {
            string prefix = owner.Id == "AnimusForge_global_settings" ? "" : owner.Id == "AnimusForge_Illustrator_v1" ? "Illustrator." :
                owner.Id == "AnimusForge_DialogueUI_v1" ? "DialogueUi." : "Coup.";
            foreach (var original in owner.GetAllSettingPropertyDefinitions())
            {
                string id = prefix + original.Id;
                if (!visible.ContainsKey(id)) continue;
                var projected = api.TryGetValue(id, out var a) ? a : terminal[id];
                check(projected.PropertyReference.Equals(original.PropertyReference), "original owner PropertyRef: " + id);
                check(projected.MinValue == original.MinValue && projected.MaxValue == original.MaxValue &&
                    projected.SettingType == original.SettingType && projected.RequireRestart == original.RequireRestart,
                    "original metadata: " + id);
                check(!projected.IsToggle, "all enable switches remain visible rows: " + id);
            }
        }
        // Each owner has a control in the restored page, including formerly hidden groups.
        foreach (string id in new[] { "MinimumPositiveNpcRelationGain", "Coup.Enabled", "DialogueUi.BodyFontSize", "Illustrator.EnableSceneOffscreenRendering" })
        {
            check(terminal.ContainsKey(id), "restored owner control exists: " + id);
            var property = terminal[id];
            object old = property.PropertyReference.Value;
            object changed = old is bool boolean ? (object)!boolean : old is int integer ? (object)(integer + 1) : old;
            property.PropertyReference.Value = changed;
            var def = visible[id];
            check(Equals(def.Getter((DuelSettings)owners.Single(o => o.Id == "AnimusForge_global_settings")), changed), "MCM edit visible in terminal: " + id);
            property.PropertyReference.Value = old;
        }
        var relation = terminal["MinimumPositiveNpcRelationGain"];
        object previous = relation.PropertyReference.Value;
        visible[relation.Id].Setter((DuelSettings)owners.Single(o => o.Id == "AnimusForge_global_settings"), 7);
        check((int)relation.PropertyReference.Value == 7, "terminal edit visible in MCM");
        var defaultPage = pages[1].GetBuiltInPresets().Single(p => p.Id == "default").LoadPreset();
        check((int)relation.PropertyReference.Value == 7, "loading default preset does not mutate live owners");
        check(defaultPage.GetAllSettingPropertyDefinitions().Any(p => p.Id == relation.Id), "default preset includes restored properties");
        relation.PropertyReference.Value = previous;
        var priorSave = TerminalProviderProbe.OnSave;
        try
        {
            TerminalProviderProbe.OnSave = null;
            TerminalProviderProbe.Saved.Clear();
            pages[1].OnPropertyChanged(BaseSettings.SaveTriggered);
            check(new HashSet<string>(TerminalProviderProbe.Saved).SetEquals(owners.Select(o => o.Id)), "MCM view saves original four owner IDs only");
        }
        finally { TerminalProviderProbe.OnSave = priorSave; }
    }
}
