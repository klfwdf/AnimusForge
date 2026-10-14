using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

// Actual product gate, actual CLR types; only property snapshots are intercepted.
// No native Input/Mission/rendering or real Campaign is initialized.
static class SceneTerminalChecks
{
    const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Dictionary<MethodBase, object> values = new Dictionary<MethodBase, object>();
    static bool Snapshot<T>(MethodBase __originalMethod, ref T __result)
    {
        __result = (T)values[__originalMethod];
        return false;
    }
    internal static void Run(Assembly af, Action<bool, string> check)
    {
        var readerType = af.GetType("AnimusForge.TerminalWeeklyReportBrowserPopupVM", true);
        var emptyReader = FormatterServices.GetUninitializedObject(readerType);
        check((int)readerType.GetProperty("ReaderBodyFontSize").GetValue(emptyReader) == 20, "terminal reader default font enlarged without changing stored option");
        var itemType = af.GetType("AnimusForge.TerminalWeeklyReportEntryItemVM", true);
        var readerItem = FormatterServices.GetUninitializedObject(itemType);
        readerType.GetField("_readerItem", All).SetValue(emptyReader, readerItem);
        foreach (int font in new[] { 13, 16, 26 })
        {
            itemType.GetField("_bodyFontSize", All).SetValue(readerItem, font);
            check((int)readerType.GetProperty("ReaderBodyFontSize").GetValue(emptyReader) == font + 2, "terminal reader retains configured basis and enlarges " + font);
        }
        var language = Assembly.Load("TaleWorlds.Localization").GetType("TaleWorlds.Localization.MBTextManager", true).GetMethod("ChangeLanguage");
        var definitions = af.GetType("AnimusForge.TerminalSettingsRegistry", true).GetProperty("AllDefinitions");
        try
        {
            foreach (string name in new[] { "English", "简体中文" })
            {
                language.Invoke(null, new object[] { name });
                var terminalKey = ((IEnumerable)definitions.GetValue(null)).Cast<object>().Single(x => (string)x.GetType().GetProperty("Id").GetValue(x) == "TerminalKey");
                var hint = (string)terminalKey.GetType().GetProperty("HintText").GetValue(terminalKey);
                check(hint.Contains(name == "English" ? "campaign scene" : "战役场景"), "actual terminal shortcut hint projects " + name);
            }
        }
        finally { language.Invoke(null, new object[] { "English" }); }
        var harmony = new Harmony("af.tests.terminal.scene.context");
        Type TypeOf(string assembly, string type) => Assembly.Load(assembly).GetType(type, true);
        var campaign = TypeOf("TaleWorlds.CampaignSystem", "TaleWorlds.CampaignSystem.Campaign");
        var mission = TypeOf("TaleWorlds.MountAndBlade", "TaleWorlds.MountAndBlade.Mission");
        var game = TypeOf("TaleWorlds.Core", "TaleWorlds.Core.Game");
        var manager = TypeOf("TaleWorlds.Core", "TaleWorlds.Core.GameStateManager");
        var screenManager = TypeOf("TaleWorlds.ScreenSystem", "TaleWorlds.ScreenSystem.ScreenManager");
        var missionScreen = TypeOf("TaleWorlds.MountAndBlade.View", "TaleWorlds.MountAndBlade.View.Screens.MissionScreen");
        var missionState = TypeOf("TaleWorlds.MountAndBlade", "TaleWorlds.MountAndBlade.MissionState");
        var conversation = campaign.GetProperty("ConversationManager").PropertyType;
        object Empty(Type type) => FormatterServices.GetUninitializedObject(type);
        MethodInfo Patch(Type type, string property, object value)
        {
            var info = type.GetProperty(property, All);
            var getter = info.GetGetMethod(true);
            values[getter] = value;
            harmony.Patch(getter, prefix: new HarmonyMethod(typeof(SceneTerminalChecks).GetMethod("Snapshot", All).MakeGenericMethod(info.PropertyType)));
            return getter;
        }
        try
        {
            var campaignCurrent = Patch(campaign, "Current", Empty(campaign));
            var missionCurrent = Patch(mission, "Current", Empty(mission));
            Patch(game, "Current", Empty(game)); Patch(game, "GameStateManager", Empty(manager));
            Patch(campaign, "ConversationManager", Empty(conversation));
            var talking = Patch(conversation, "IsConversationInProgress", false);
            var ending = Patch(mission, "IsMissionEnding", false);
            var modeType = mission.GetProperty("Mode").PropertyType;
            var mode = Patch(mission, "Mode", Enum.Parse(modeType, "Battle"));
            var top = Patch(screenManager, "TopScreen", Empty(missionScreen));
            var state = Patch(manager, "ActiveState", Empty(missionState));
            var disabled = Patch(manager, "ActiveStateDisabledByUser", false);
            var gate = af.GetType("AnimusForge.AnimusForgeTerminalBehavior", true).GetMethod("IsTerminalHotkeyContextActive", All);
            bool Allowed() => (bool)gate.Invoke(null, null);
            check(Allowed(), "campaign mission admits terminal hotkey");
            foreach (string name in new[] { "Conversation", "Barter", "StartUp", "CutScene" })
            {
                values[mode] = Enum.Parse(modeType, name);
                check(!Allowed(), "mission context blocked " + name);
            }
            foreach (string name in new[] { "Battle", "Duel", "Stealth", "Deployment", "Tournament" })
            {
                values[mode] = Enum.Parse(modeType, name);
                check(Allowed(), "active campaign mission context " + name);
            }
            values[ending] = true; check(!Allowed(), "ending mission blocked"); values[ending] = false;
            values[talking] = true; check(!Allowed(), "campaign conversation blocked"); values[talking] = false;
            values[disabled] = true; check(!Allowed(), "existing menu disable request blocked"); values[disabled] = false;
            var savedTop = values[top]; values[top] = null; check(!Allowed(), "missing mission screen blocked"); values[top] = savedTop;
            var savedState = values[state]; values[state] = null; check(!Allowed(), "missing mission state blocked"); values[state] = savedState;
            var savedCampaign = values[campaignCurrent]; values[campaignCurrent] = null; check(!Allowed(), "standalone mission without campaign blocked"); values[campaignCurrent] = savedCampaign;
            values[missionCurrent] = null; values[top] = null;
            check(Allowed(), "original map fallback remains admitted");
            values[campaignCurrent] = null; check(!Allowed(), "missing campaign map blocked");
        }
        finally
        {
            foreach (var method in values.Keys) harmony.Unpatch(method, HarmonyPatchType.Prefix, harmony.Id);
            values.Clear();
        }
    }
}
