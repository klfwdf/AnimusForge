using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AnimusForge;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Common;

static class TtsVisibilityChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static object Get(object o, string p) => o.GetType().GetProperty(p, All).GetValue(o);
    static void Set(object o, string p, object v) => o.GetType().GetProperty(p, All).SetValue(o, v);
    static bool Expected(string id, int mode)
    {
        if (id.StartsWith("TtsLiteGuide")) return mode == 2;
        if (id == "TtsVolcDedicatedVolume" || id == "TtsGptSoVitsOutputLanguage") return true;
        if (id.StartsWith("TtsVolcDedicated")) return mode == 0;
        if (id.StartsWith("TtsOpenAi") || id == "TtsStreamingPlaybackEnabled") return mode == 1;
        if (id == "TestTtsVolcDedicatedVoice") return mode < 2;
        if (id == "RefreshTtsGptSoVitsRuntime") return mode == 2;
        if (id == "TtsGptSoVitsApiUrl" || id == "TtsGptSoVitsManifestPath") return mode == 3;
        if (id.StartsWith("TtsGptSoVits") || id == "TestTtsGptSoVitsVoice") return mode >= 2;
        return true;
    }
    internal static void Run(Assembly af, Action<bool, string> check)
    {
        var settings = (DuelSettings)TerminalProviderProbe.Owners["AnimusForge_global_settings"];
        var all = settings.GetAllSettingPropertyDefinitions().Where(p => p.GroupName.Contains("AF_MCM_Tts_Group")).ToArray();
        var policy = af.GetType("AnimusForge.TtsSettingsVisibility", true).GetMethod("IsVisible", All);
        var provider = TerminalSettingsRegistry.GetById("TtsProviderDropdown");
        int previous = settings.TtsProviderDropdown.SelectedIndex;
        var unrelated = TerminalSettingsRegistry.VisibleDefinitions.Where(d => !all.Any(p => p.Id == d.Id)).Select(d => d.Id).ToArray();
        var before = new Dictionary<string, object> { ["url"] = settings.TtsGptSoVitsApiUrl, ["key"] = settings.TtsOpenAiApiKey, ["voice"] = settings.TtsVolcDedicatedSpeaker };
        try
        {
            check(all.Length >= 30, "full TTS metadata includes all service settings");
            for (int mode = 0; mode < 4; mode++)
            {
                provider.Setter(settings, mode);
                var visible = new HashSet<string>(TerminalSettingsRegistry.VisibleDefinitions.Select(d => d.Id));
                var category = new HashSet<string>(TerminalSettingsRegistry.GetByCategory(provider.TabCategory).Select(d => d.Id));
                foreach (var p in all)
                {
                    bool expected = Expected(p.Id, mode);
                    check((bool)policy.Invoke(null, new object[] { p.Id, mode }) == expected, "policy " + mode + ":" + p.Id);
                    check(visible.Contains(p.Id) == expected && category.Contains(p.Id) == expected, "terminal/search source " + mode + ":" + p.Id);
                    check(TerminalSettingsRegistry.GetById(p.Id) != null, "hidden setting still discoverable " + p.Id);
                }
                check(unrelated.SequenceEqual(TerminalSettingsRegistry.VisibleDefinitions.Where(d => !all.Any(p => p.Id == d.Id)).Select(d => d.Id)), "non-TTS visible items unchanged " + mode);
            }
            // The real terminal view rebuilds only after the provider changes.
            Type popupType = af.GetType("AnimusForge.AnimusForgeTerminalPopupVM", true);
            object popup = Activator.CreateInstance(popupType, new object[] { null, null, null });
            Set(popup, "SearchText", "TtsGptSoVitsApiUrl");
            string[] Items() => ((IEnumerable)Get(popup, "Items")).Cast<object>().Select(x => (string)Get(x, "Id")).ToArray();
            check(Items().Contains("TtsGptSoVitsApiUrl"), "manual URL visible in actual terminal search");
            provider.Setter(settings, 2);
            popupType.GetMethod("MarkModified", All).Invoke(popup, null);
            check(!Items().Contains("TtsGptSoVitsApiUrl"), "terminal provider edit immediately removes manual URL");
            provider.Setter(settings, 3);
            popupType.GetMethod("RefreshCurrentItemsFromSettings", All).Invoke(popup, null);
            check(Items().Contains("TtsGptSoVitsApiUrl"), "terminal refresh restores manual URL");
            check(Equals(before["url"], settings.TtsGptSoVitsApiUrl) && Equals(before["key"], settings.TtsOpenAiApiKey) && Equals(before["voice"], settings.TtsVolcDedicatedSpeaker), "switching preserves URLs keys and voices");
        }
        finally { provider.Setter(settings, previous); }
    }

    internal static void NativeUi(Assembly af, Assembly ui, Action<bool, string> check)
    {
        var refresh = af.GetType("AnimusForge.McmDropdownRuntimeRefresh", true);
        refresh.GetMethod("EnsurePatched", All).Invoke(null, null);
        var tts = af.GetType("AnimusForge.TtsMcmVisibility", true);
        var live = (IList)tts.GetField("LiveRows", All).GetValue(null);
        int baseline = live.Count;
        var settings = (DuelSettings)TerminalProviderProbe.Owners["AnimusForge_global_settings"];
        int original = settings.TtsProviderDropdown.SelectedIndex;
        var pages = (FluentGlobalSettings[])af.GetType("AnimusForge.AnimusForgeMcmPages", true).GetMethod("CreatePages", All)
            .Invoke(null, new object[] { TerminalProviderProbe.Owners.Values.Cast<BaseSettings>().ToArray() });
        var panelType = ui.GetType("MCM.UI.GUI.ViewModels.ModOptionsVM", true);
        var vmType = ui.GetType("MCM.UI.GUI.ViewModels.SettingsVM", true);
        var entryType = ui.GetType("MCM.UI.GUI.ViewModels.SettingsEntryVM", true);
        object panel = FormatterServices.GetUninitializedObject(panelType);
        var field = panelType.GetField("<ModSettingsList>k__BackingField", All);
        field.SetValue(panel, Activator.CreateInstance(field.FieldType));
        refresh.GetMethod("ModOptionsVmCtorPostfix", All).Invoke(null, new[] { panel });
        foreach (var page in pages) TerminalProviderProbe.Owners.Add(page.Id, page);
        object vm = null;
        try
        {
            settings.TtsProviderDropdown.SelectedIndex = 0;
            vm = Activator.CreateInstance(vmType, new object[] { new SettingsDefinition(pages[0].Id), panel });
            object entry = Activator.CreateInstance(entryType, new[] { vm, null });
            object list = Get(panel, "ModSettingsList"); list.GetType().GetMethod("Add").Invoke(list, new[] { entry });
            var rows = new Dictionary<string, object>();
            void Group(object group)
            {
                Set(group, "IsExpanded", true);
                foreach (object row in (IEnumerable)Get(group, "SettingProperties"))
                    rows[((ISettingsPropertyDefinition)Get(row, "SettingPropertyDefinition")).Id] = row;
                foreach (object child in (IEnumerable)Get(group, "SettingPropertyGroups")) Group(child);
            }
            foreach (object group in (IEnumerable)Get(vm, "SettingPropertyGroups")) Group(group);
            var definitions = pages[0].GetAllSettingPropertyDefinitions().ToDictionary(p => p.Id);
            string[] ids = definitions.Values.Where(p => p.GroupName.Contains("AF_TERMINAL_GROUP_TTS_API")).Select(p => p.Id).ToArray();
            int notifications = 0;
            ((INotifyPropertyChanged)rows["TtsGptSoVitsApiUrl"]).PropertyChanged += (_, a) => { if (a.PropertyName == "IsSettingVisible") notifications++; };
            var unchanged = rows.Where(p => !ids.Contains(p.Key)).ToDictionary(p => p.Key, p => (bool)Get(p.Value, "IsSettingVisible"));
            object stack = Get(vm, "URS");
            void Pump() { for (int i = 0; i < 3; i++) refresh.GetMethod("OnApplicationTick", All).Invoke(null, null); }
            void Verify(int mode)
            {
                foreach (string id in ids) check((bool)Get(rows[id], "IsSettingVisible") == Expected(id, mode), "real native visibility " + mode + ":" + id);
                check(unchanged.All(p => p.Value == (bool)Get(rows[p.Key], "IsSettingVisible")), "native other API rows unchanged " + mode);
            }
            Pump(); Verify(0);
            for (int mode = 1; mode < 4; mode++)
            {
                // Actual native selector callback -> SetSelectedIndexAction -> URS -> owner.
                object selector = Get(rows["TtsProviderDropdown"], "DropdownValue");
                Set(selector, "SelectedIndex", mode);
                Pump();
                check(settings.TtsProviderDropdown.SelectedIndex == mode, "native dropdown updates original owner " + mode);
                Verify(mode);
            }
            check(notifications > 0, "provider selection notifies bound IsSettingVisible");
            stack.GetType().GetMethod("UndoAll").Invoke(stack, null); Pump();
            check(settings.TtsProviderDropdown.SelectedIndex == 0, "native cancel restores provider"); Verify(0);
            Set(Get(rows["TtsProviderDropdown"], "DropdownValue"), "SelectedIndex", 2); Pump(); Verify(2);
            vmType.GetMethod("ResetSettingsValue").Invoke(vm, new object[] { "TtsProviderDropdown" }); Pump();
            check(settings.TtsProviderDropdown.SelectedIndex == 0, "native reset restores original provider default"); Verify(0);
            stack.GetType().GetMethod("UndoAll").Invoke(stack, null); Pump();
            Set(panel, "SearchText", (string)Get(rows["TtsOpenAiApiUrl"], "Name"));
            check(!(bool)Get(rows["TtsOpenAiApiUrl"], "IsSettingVisible"), "native search cannot reveal inactive provider parameters");
            Set(panel, "SearchText", "");
            Set(rows["TtsVolcDedicatedApiUrl"], "IsSelected", true);
            object groupVm = Get(rows["TtsVolcDedicatedApiUrl"], "Group");
            Set(groupVm, "IsExpanded", false);
            check(!(bool)Get(rows["TtsVolcDedicatedApiUrl"], "IsSettingVisible"), "native collapse rule retained");
            Set(groupVm, "IsExpanded", true);
            var preset = pages[0].GetBuiltInPresets().Single(p => p.Id == BaseSettings.DefaultPresetId).LoadPreset();
            check(preset.GetAllSettingPropertyDefinitions().Count(p => ids.Contains(p.Id)) == ids.Length, "preset retains hidden service values");
        }
        finally
        {
            panelType.GetMethod("OnFinalize").Invoke(panel, null);
            foreach (var page in pages) TerminalProviderProbe.Owners.Remove(page.Id);
            settings.TtsProviderDropdown.SelectedIndex = original;
        }
        refresh.GetMethod("OnApplicationTick", All).Invoke(null, null);
        check(live.Count == baseline, "close removes TTS rows and pending refresh cannot revive them");
        foreach (var page in pages) TerminalProviderProbe.Owners.Add(page.Id, page);
        try { McmReopenLifecycleChecks.Run(af, ui, pages, check); }
        finally { foreach (var page in pages) TerminalProviderProbe.Owners.Remove(page.Id); }
        check(live.Count == baseline, "eight reopens, late rows and other mod do not leak TTS tracking");
    }
}
