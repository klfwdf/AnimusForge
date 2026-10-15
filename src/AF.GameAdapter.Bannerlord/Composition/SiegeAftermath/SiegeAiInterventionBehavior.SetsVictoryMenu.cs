using System;
using TaleWorlds.CampaignSystem.Roster;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
    private static void RegisterSetsVictoryMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu(SetsVictoryMenuProfile.MenuId, "{=!}{AF_SETS_VICTORY_TEXT}", SetsVictoryMenuOnInit);
        starter.AddGameMenuOption(SetsVictoryMenuProfile.MenuId, SetsVictoryMenuProfile.EnterOptionId,
            "{=!}{AF_SETS_VICTORY_ENTRY_TEXT}", SetsVictoryEntryCondition, SetsVictoryEntryConsequence, isLeave: false, 0);
        starter.AddGameMenuOption(SetsVictoryMenuProfile.MenuId, SetsVictoryMenuProfile.LeaveOptionId,
            SetsVictoryMenuProfile.LeaveText, SetsVictoryLeaveCondition, SetsVictoryLeaveConsequence, isLeave: true, 1);
    }

    private static void SetsVictoryMenuOnInit(MenuCallbackArgs args)
    {
        Settlement settlement = ResolveCurrentSettlement() ?? _activeSettlement;
        MBTextManager.SetTextVariable("AF_SETS_VICTORY_TEXT", SetsVictoryMenuProfile.BuildSummary(settlement?.Name?.ToString(), _setsOwnedSettlementIncidentContext), false);
        MBTextManager.SetTextVariable("AF_SETS_VICTORY_ENTRY_TEXT", settlement?.IsVillage == true
            ? new TextObject("{=AF_GCCZ_VillageDispositionEntry}Personally enter the village to deal with the civilians").ToString()
            : SetsVictoryMenuProfile.GetEntryText(ResolveSetsSettlementSceneKind(settlement)), false);
        args?.MenuContext?.SetBackgroundMeshName("encounter_win");
    }

    private static bool SetsVictoryEntryCondition(MenuCallbackArgs args)
    {
        Settlement settlement = ResolveCurrentSettlement();
        bool hasContext = _setsSettlementEntryVictoryContext
            && settlement != null && ReferenceEquals(settlement, _activeSettlement)
            && SettlementEntryTroopSelectionBehavior.HasPendingVictoryMenuForExternal(settlement.StringId);
        if (hasContext && !settlement.IsVillage) return SiegeInterventionEntryCondition(args);
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        args.IsEnabled = hasContext && PlayerEncounter.LocationEncounter?.Settlement == settlement
            && ResolveOwnedSettlementIncidentLocation(settlement) != null;
        if (args.IsEnabled)
            args.IsEnabled = settlement.MapFaction == Clan.PlayerClan?.MapFaction
                ? VillageAftermathBehavior.CanEnterFriendlyVillage(settlement)
                : VillageAftermathBehavior.CanEnterHostileDispositionForExternal(settlement);
        if (!args.IsEnabled)
            args.Tooltip = new TextObject("{=AF_GCCZ_VillageDispositionNotReady}The village encounter or rewards are not finished yet. Close the reward screens and wait for the disposition menu; do not start another attack.");
        return true;
    }

    private static void SetsVictoryEntryConsequence(MenuCallbackArgs args)
    {
        if (!SetsVictoryEntryCondition(args) || !args.IsEnabled) return;
        Settlement settlement = ResolveCurrentSettlement();
        if (settlement == null || !SettlementEntryTroopSelectionBehavior.HasPendingVictoryMenuForExternal(settlement.StringId)) return;
        if (settlement.IsVillage)
        {
            if (!_setsOwnedSettlementIncidentContext) OpenForeignVillageTroopSelection(args, settlement, settlement.MapFaction != Clan.PlayerClan?.MapFaction);
            else EnterOwnedVillageAftermath(settlement);
            return; // The village mission consumes its admission only after real activation.
        }
        else
        {
            EnterIntervention(args);
        }
        if (Mission.Current != null)
        {
            SettlementEntryTroopSelectionBehavior.CompletePendingVictoryMenuForExternal(settlement.StringId);
        }
    }

    private static bool EnsureVillageVictoryLocationEncounter(Settlement village)
    {
        if (village?.IsVillage != true || Mission.Current != null
            || MobileParty.MainParty?.CurrentSettlement != village
            || PlayerEncounterCompat.GetCurrentMapEventSafe() != null || village.Party?.MapEvent != null
            || PlayerEncounter.Current?.ForceSupplies == true) return false;
        // Native battles can leave the party inside a village without a location encounter.
        // Restore only that missing location, never overwrite a different encounter.
        if (PlayerEncounter.LocationEncounter == null)
            PlayerEncounter.LocationEncounter = new VillageEncounter(village);
        return PlayerEncounter.LocationEncounter.Settlement == village
            && ResolveOwnedSettlementIncidentLocation(village) != null;
    }

    private static bool SetsVictoryLeaveCondition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
        args.IsEnabled = true;
        return true;
    }

    private static void OpenForeignVillageTroopSelection(MenuCallbackArgs args, Settlement village, bool hostile = true)
    {
        var full = TroopRoster.CreateDummyTroopRoster();
        foreach (var entry in BuildInterventionTroopSelectionFullRoster().GetTroopRoster())
            if (entry.Character != null && (entry.Character.IsSoldier || entry.Character.IsHero)) full.AddToCounts(entry.Character, entry.Number);
        Action<TroopRoster> enter = picked =>
        {
            if (village != ResolveCurrentSettlement() || !(hostile ? VillageAftermathBehavior.CanEnterHostileDispositionForExternal(village) : VillageAftermathBehavior.CanEnterFriendlyVillage(village))) return;
            var selected = TroopRoster.CreateDummyTroopRoster();
            int remaining = VillageDispositionActionCatalog.FollowerLimit;
            foreach (var entry in (picked ?? TroopRoster.CreateDummyTroopRoster()).GetTroopRoster())
            {
                if (entry.Character == null || (!entry.Character.IsSoldier && !entry.Character.IsHero) || remaining <= 0) continue;
                int count = Math.Min(remaining, Math.Min(Math.Max(0, entry.Number), full.GetTroopCount(entry.Character)));
                if (count > 0) { selected.AddToCounts(entry.Character, count); remaining -= count; }
            }
            EnterOwnedVillageAftermath(village, hostile, selected, hostile ? "sets_village_victory_menu" : "friendly_village_menu");
        };
        if (full.TotalManCount == 0) { enter(full); return; }
        if (!TryOpenTroopSelectionRuntimeCompat(args.MenuContext, full,
            BuildDefaultInterventionTroopSelection(full, VillageDispositionActionCatalog.FollowerLimit), enter, VillageDispositionActionCatalog.FollowerLimit))
            InformationManager.DisplayMessage(new InformationMessage(GcczLocalizedResourceLoader.UseChineseInterface ? "村庄随行选兵界面无法打开，本次未进入，请重试。" : "The village escort selection screen could not open. Entry cancelled; please retry."));
    }

    private static void SetsVictoryLeaveConsequence(MenuCallbackArgs args)
    {
        Settlement settlement = ResolveCurrentSettlement() ?? _activeSettlement;
        if (settlement == null || !SettlementEntryTroopSelectionBehavior.HasPendingVictoryMenuForExternal(settlement.StringId)) return;
        if (_setsOwnedSettlementIncidentContext)
        {
            SetsOwnedSettlementIncidentLeaveConsequence(args);
        }
        else
        {
            PlayerEncounter.LeaveSettlement();
            PlayerEncounter.Finish(true);
            ClearSetsOwnedSettlementIncidentContext("sets_victory_leave");
        }
        SettlementEntryTroopSelectionBehavior.CompletePendingVictoryMenuForExternal(settlement.StringId);
    }
}
