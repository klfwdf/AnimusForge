using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed partial class ExpeditionParadeBehavior
{
    private void OnAfterSessionLaunched(CampaignGameStarter starter)
    {
        Prune();
        // Vengeance adds execution options during OnSessionLaunched, so resolve their actual
        // positions only after that phase. Castle menus without an execution option use Leave.
        foreach (string menuId in new[] { "town", "castle" })
        {
            var options = starter.GetPresumedGameMenu(menuId).MenuOptions.ToList();
            if (options.Any(o => o.IdString == ExpeditionParadeMenuPolicy.EntryId)) continue;
            int index = ExpeditionParadeMenuPolicy.InsertionIndex(options.Select(o => o.IdString).ToList(), options.FindIndex(o => o.IsLeave));
            starter.AddGameMenuOption(menuId, ExpeditionParadeMenuPolicy.EntryId,
                "{=AF_UI_Parade_MenuEntry}Hold a parade...", ParadeEntryCondition,
                _ => { if (Mission.Current == null && GetSettlementAvailability().Settlement != null) GameMenu.SwitchToMenu(ExpeditionParadeMenuPolicy.MenuId); },
                isLeave: false, index: index);
        }
        starter.AddGameMenu(ExpeditionParadeMenuPolicy.MenuId,
            "{=AF_UI_Parade_MenuDescription}Choose an expedition or victory parade. Only eligible types can be started; troop muster and route checks follow.",
            _ => { }, GameMenu.MenuOverlayType.SettlementWithParties);
        starter.AddGameMenuOption(ExpeditionParadeMenuPolicy.MenuId, "af_parade_expedition",
            "{=AF_UI_Parade_MenuExpedition}Expedition parade", args => ParadeTypeCondition(args, ExpeditionParadeType.Expedition),
            _ => RequestFromSettlementMenu(ExpeditionParadeType.Expedition));
        starter.AddGameMenuOption(ExpeditionParadeMenuPolicy.MenuId, "af_parade_victory",
            "{=AF_UI_Parade_MenuVictory}Victory parade", args => ParadeTypeCondition(args, ExpeditionParadeType.Victory),
            _ => RequestFromSettlementMenu(ExpeditionParadeType.Victory));
        starter.AddGameMenuOption(ExpeditionParadeMenuPolicy.MenuId, "af_parade_back",
            "{=AF_UI_Parade_MenuBack}Back", args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
            _ => ReturnToSettlementMenu(), isLeave: true);
    }

    private bool ParadeEntryCondition(MenuCallbackArgs args)
    {
        Settlement place = Settlement.CurrentSettlement;
        if (Mission.Current != null || place == null || (!place.IsTown && !place.IsCastle)) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        var available = GetSettlementAvailability();
        args.IsEnabled = available.Settlement != null;
        args.Tooltip = args.IsEnabled ? null : new TextObject(available.ExpeditionReason);
        return true;
    }

    private bool ParadeTypeCondition(MenuCallbackArgs args, ExpeditionParadeType type)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Mission;
        var available = GetSettlementAvailability();
        args.IsEnabled = Mission.Current == null && (type == ExpeditionParadeType.Expedition ? available.ExpeditionAvailable : available.VictoryAvailable);
        args.Tooltip = args.IsEnabled ? null : new TextObject(type == ExpeditionParadeType.Expedition ? available.ExpeditionReason : available.VictoryReason);
        return true;
    }

    private static void ReturnToSettlementMenu() => GameMenu.SwitchToMenu(Settlement.CurrentSettlement?.IsCastle == true ? "castle" : "town");

    private void RequestFromSettlementMenu(ExpeditionParadeType type)
    {
        if (Mission.Current != null) return;
        bool queued = TryQueue(null, type, out string message, fromSettlementMenu: true);
        Notify(message);
        if (!queued) { GameMenu.SwitchToMenu(ExpeditionParadeMenuPolicy.MenuId); return; }
        ReturnToSettlementMenu();
        // Settlement menus pause campaign time. Start directly instead of waiting for Tick.
        ProcessPending(startImmediately: true);
    }
}
