using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using AnimusForge;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Common;

// Actual product DLL, native MCM discovery/JSON, terminal VM and diplomacy host.
// Only settings storage is isolated to the existing workspace fixture provider.
static class CountrySelectionSettingsChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    const string Id = "WorldDiplomacyActivityDropdown";
    internal static void Run(Assembly af, string root, Action<bool, string> check)
    {
        var localization = Assembly.Load("TaleWorlds.Localization");
        localization.GetType("TaleWorlds.Localization.LocalizedTextManager", true).GetMethod("LoadLocalizationXmls")
            .Invoke(null, new object[] { new[] { root } });
        var manager = localization.GetType("TaleWorlds.Localization.MBTextManager", true);
        var owners = TerminalProviderProbe.Owners.Values.Cast<BaseSettings>().ToArray();
        var owner = (DuelSettings)owners.Single(x => x.Id == "AnimusForge_global_settings");
        var hostType = af.GetType("AnimusForge.WorldDiplomacyBehavior+OrchestrationHost", true);
        object host = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(hostType);
        int Preference() => (int)hostType.GetProperty("AdmissionCountryCountPreference", All).GetValue(host);
        var formatType = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat", true);
        var loggerType = typeof(BaseSettings).Assembly.GetType("BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1", true).MakeGenericType(formatType);
        object format = Activator.CreateInstance(formatType, Activator.CreateInstance(loggerType));
        check(owner.WorldDiplomacyActivityDropdown.SelectedIndex == 1, "standard is the original owner's default");
        foreach (string language in new[] { "English", "简体中文", "English" })
        {
            check((bool)manager.GetMethod("ChangeLanguage").Invoke(null, new object[] { language }), "native UI language loaded");
            var def = TerminalSettingsRegistry.VisibleDefinitions.Single(x => x.Id == Id);
            check(def.TabCategory == (string)af.GetType("AnimusForge.TerminalCategories", true).GetField("Diplomacy", All).GetValue(null),
                "terminal country count is on diplomacy page");
            check(def.DisplayName == (language == "English" ? "Event participant count" : "事件准入国家数"), "localized country-count title");
            var pages = (FluentGlobalSettings[])af.GetType("AnimusForge.AnimusForgeMcmPages", true).GetMethod("CreatePages", All)
                .Invoke(null, new object[] { owners });
            var mcm = pages[1].GetAllSettingPropertyDefinitions().Single(x => x.Id == Id);
            check(!pages[0].GetAllSettingPropertyDefinitions().Any(x => x.Id == Id), "native MCM has one country-count control on terminal settings page");
            var expected = language == "English" ? new[] { "Low", "Standard", "High" } : new[] { "少", "标准", "高" };
            var vmType = af.GetType("AnimusForge.AnimusForgeTerminalItemVM", true);
            foreach (int index in new[] { 0, 2, 1 })
            {
                mcm.PropertyReference.Value = new Dropdown<string>(new[] { "legacy low", "legacy medium", "legacy high" }, index);
                check(owner.WorldDiplomacyActivityDropdown.SelectedIndex == index && Preference() == index,
                    "native MCM edits reach same saved owner and actual diplomacy host");
                check(def.DropdownGetter(owner).SelectedIndex == index, "MCM edits immediately visible to terminal");
                object vm = Activator.CreateInstance(vmType, new object[] { def, null, null });
                check((string)vmType.GetProperty("DisplayValue").GetValue(vm) == expected[index], "actual terminal VM renders bilingual selected value");
                int next = (index + 1) % 3;
                def.Setter(owner, next);
                check(((Dropdown<string>)mcm.PropertyReference.Value).SelectedIndex == next && Preference() == next,
                    "terminal edit immediately reaches MCM and diplomacy host");
                string json = (string)formatType.GetMethod("SaveJson").Invoke(format, new object[] { owner });
                var restored = new DuelSettings();
                formatType.GetMethod("LoadFromJson").Invoke(format, new object[] { restored, json });
                check(restored.WorldDiplomacyActivityDropdown.SelectedIndex == next && restored.WorldDiplomacyActivityDropdown.Count == 3,
                    "actual native MCM JSON roundtrip retains selection and replaces obsolete labels");
            }
            owner.WorldDiplomacyActivityDropdown.SelectedIndex = 1;
            var defaults = pages[1].GetBuiltInPresets().Single(x => x.Id == BaseSettings.DefaultPresetId).LoadPreset();
            check(((Dropdown<string>)defaults.GetAllSettingPropertyDefinitions().Single(x => x.Id == Id).PropertyReference.Value).SelectedIndex == 1,
                "native MCM preset restores standard selection");
            TerminalProviderProbe.Saved.Clear(); pages[1].OnPropertyChanged(BaseSettings.SaveTriggered);
            check(TerminalProviderProbe.Saved.Contains(owner.Id) && TerminalProviderProbe.Saved.All(x => owners.Any(o => o.Id == x)),
                "native page saves existing owners only");
        }
        Console.WriteLine("PASS country selection native MCM / terminal / JSON / bilingual VM / actual diplomacy host; no game or HTTP.");
    }
}
