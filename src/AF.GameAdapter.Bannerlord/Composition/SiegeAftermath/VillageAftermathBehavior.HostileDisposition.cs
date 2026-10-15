using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private static bool _pendingHostileDisposition;
    private static bool _hostileDispositionActive;
    private static bool _hostileVillageMercyApplied;

    internal static bool CanEnterHostileDispositionForExternal(Settlement village)
    {
        return VillageDispositionActionCatalog.CanEnter(village?.IsVillage == true && village.Village != null,
            village?.Village?.VillageState == Village.VillageStates.Normal,
            village?.IsUnderSiege == true || village?.Party?.MapEvent != null,
            village?.MapFaction == Clan.PlayerClan?.MapFaction,
            SettlementEntryTroopSelectionBehavior.HasPendingVictoryMenuForExternal(village?.StringId));
    }

    private static bool TryActivateHostileDisposition(Settlement village, Mission mission, string source)
    {
        try
        {
            if (mission == null || !CanEnterHostileDispositionForExternal(village)) return false;
            if (!PrepareForeignEconomy(village)) return false;

            _activeMission = mission;
            _activeVillageId = village.StringId;
            _activeAuthorityKind = VillageAftermathAuthorityKind.None;
            _cultureInquiryPending = false;
            AppliedActions.Clear();
            _hostileDispositionActive = true;
            _hostileVillageMercyApplied = false;
            ClearHostileDialogue();
            Departure.Reset(); DestructiveActions.Clear();
        HostileDialogue.Begin(village.StringId, GcczTownPromptResourceProvider.UseEnglishPrompt, GcczTownPromptResourceProvider.GetCatalog());
            SettlementEntryTroopSelectionBehavior.CompletePendingVictoryMenuForExternal(village.StringId);
            InformationManager.DisplayMessage(new InformationMessage(
                VUi("军事战果已结算；TAB结算村庄所得。未处置直接离开默认掠夺，宽恕或成功协助后不追加掠夺。", "Military rewards settled. TAB settles village proceeds. Leaving without a disposition defaults to plunder; mercy or successful aid prevents that default."), Color.FromUint(SuccessColor)));
            GcczDiagnosticLog.Log("VillageEntry", "foreign disposition activated village=" + village.StringId + " source=" + source);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("VillageAftermath", "Activate foreign village disposition failed: " + ex);
            if (EconomyOwner != null && EconomyOwner._dispositionPayoutVillage == village?.StringId) EconomyOwner._dispositionAwaitingExit = true;
            ClearMissionState("foreign_activation_failed");
            return false;
        }
    }

    private static bool TryProcessHostileDisposition(int targetAgentIndex, ref string text,
        out bool actionHandled, bool directReply)
    {
        actionHandled = false;
        bool hasProtocol = text?.IndexOf("[VILLAGE_DISPOSE:", StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasRetired = text?.IndexOf("[VILLAGE_ACTION:", StringComparison.OrdinalIgnoreCase) >= 0;
        var actions = VillageDispositionActionCatalog.Extract(text);
        text = VillageDispositionActionCatalog.RemoveTags(FriendlyVillageActionCatalog.RemoveTags(text));
        VillageDispositionSpeakerRole role = ResolveHostileSpeakerRole(targetAgentIndex, out _);
        bool hadSupport = EconomyOwner?.Assistance.CanSpendForSceneTurn(_activeVillageId, InitialVillageAssistance, ForeignPersuasionTurn) == true;
        bool hasVoluntaryAction = actions.Any(spec => spec.OutcomeLevel > 0);
        foreach (VillageDispositionActionSpec spec in actions)
        {
            if (spec.Kind == VillageDispositionActionKind.VillagerApproval && hasVoluntaryAction) continue;
            if (!VillageDispositionSpeakerPolicy.CanAuthorize(spec, role, directReply))
            {
                GcczDiagnosticLog.Log("VillageAction", "rejected foreign action=" + spec.Kind + " role=" + role + " direct=" + directReply);
                continue;
            }
            if (!spec.HasDefinedEffect)
            {
                TryDisplayFailureMessage(VUi("此项的场景／大地图执行尚未接通，本次未执行。", "This scene or campaign action is not yet connected; nothing was executed."));
                continue;
            }
            if (spec.OutcomeLevel > 0 && !hadSupport) continue;
            if (spec.Kind == VillageDispositionActionKind.Mercy && !_hostileVillageMercyApplied)
            {
                StopVillageMassacre(); _hostileVillageMercyApplied = true; Departure.MarkDisposition();
                actionHandled = true;
                RecordForeignFact(ResolveActiveVillage(), VP("玩家宽恕剩余村民，既有伤亡保持，不另加奖惩。", "The player pardoned remaining villagers; prior casualties remain, with no extra rewards or penalties."), "mercy", false);
                InformationManager.DisplayMessage(new InformationMessage(
                    VUi("已宽恕村民；不追加奖惩，既有后果保持。", "Villagers pardoned; no extra rewards or penalties. Previous consequences remain."), Color.FromUint(SuccessColor)));
                GcczDiagnosticLog.Log("VillageAction", "foreign village mercy confirmed village=" + _activeVillageId);
            }
            else if (spec.Kind != VillageDispositionActionKind.Mercy)
                actionHandled |= ApplyForeignEconomicAction(spec, targetAgentIndex);
        }
        return hasProtocol || hasRetired;
    }
}
