using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
	private static TownColonizationSnapshot _loadedTownColonizationSnapshot;
	private static bool _loadedTownColonizationRecoveryReady;

	private static bool TryRecoverLoadedTownColonizationState()
	{
		TownColonizationSnapshot loaded = _loadedTownColonizationSnapshot;
		if (loaded == null || !_loadedTownColonizationRecoveryReady || Mission.Current != null)
		{
			return false;
		}

		Settlement settlement = ResolveLiveCurrentSettlement();
		string menuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
		bool matchingTown = settlement?.Town != null
			&& !settlement.IsCastle
			&& string.Equals(settlement.StringId, loaded.SettlementId, StringComparison.OrdinalIgnoreCase);
		bool hasNativeContext = matchingTown
			&& SiegeAftermathMenuProfile.IsNativeSettlementTakenMenuId(menuId)
			&& TryCaptureNativeSiegeContextForLoadRecovery(settlement);
		TownColonizationLoadRecoveryDecision decision = TownColonizationLoadRecoveryPolicy.Evaluate(
			loaded,
			settlement?.StringId,
			hasNativeContext);
		_loadedTownColonizationSnapshot = null;
		_loadedTownColonizationRecoveryReady = false;

		if (decision.Kind == TownColonizationLoadRecoveryKind.None
			|| decision.Kind == TownColonizationLoadRecoveryKind.ClearTerminal)
		{
			ActiveTownColonization.Reset();
			Logger.Log("SiegeAiIntervention", "Cleared terminal GCCZ town colonization load state. Settlement=" + (loaded.SettlementId ?? "N/A") + ", State=" + loaded.State + ", OutcomeCommitted=" + loaded.SettlementOutcomeCommitted);
			return false;
		}
		if (decision.Kind == TownColonizationLoadRecoveryKind.RejectUnsafe)
		{
			ActiveTownColonization.Reset();
			Logger.Log("SiegeAiIntervention", "Rejected unsafe GCCZ town colonization load recovery. SavedSettlement=" + (loaded.SettlementId ?? "N/A") + ", LiveSettlement=" + (settlement?.StringId ?? "N/A") + ", Menu=" + (menuId ?? "N/A") + ", NativeContext=" + hasNativeContext);
			GcczDiagnosticLog.Log("CulturalRepopulation", "load recovery rejected savedSettlement=" + (loaded.SettlementId ?? "N/A")
				+ " liveSettlement=" + (settlement?.StringId ?? "N/A")
				+ " menu=" + (menuId ?? "N/A")
				+ " nativeContext=" + hasNativeContext);
			return false;
		}

		if (!PrepareLoadedTownColonizationRuntime(settlement, decision.Snapshot))
		{
			ResetAftermathRuntimeGuards("town_colonization_load_recovery_prepare_failed");
			return false;
		}

		bool recovered = decision.Kind == TownColonizationLoadRecoveryKind.ResumeCultureCommit
			? FinalizeRecoveredTownColonizationCultureCommit(settlement)
			: FinalizePendingAftermath("town_colonization_load_recovery");
		if (!recovered)
		{
			Logger.Log("SiegeAiIntervention", "GCCZ town colonization load recovery failed closed. Settlement=" + (settlement?.StringId ?? "N/A") + ", Recovery=" + decision.Kind);
			ResetAftermathRuntimeGuards("town_colonization_load_recovery_failed");
			return false;
		}

		Logger.Log("SiegeAiIntervention", "Recovered GCCZ town colonization state exactly once. Settlement=" + (settlement.StringId ?? "N/A") + ", Recovery=" + decision.Kind);
		GcczDiagnosticLog.Log("CulturalRepopulation", "load recovery completed settlement=" + (settlement.StringId ?? "N/A") + " recovery=" + decision.Kind);
		return true;
	}

	private static bool PrepareLoadedTownColonizationRuntime(
		Settlement settlement,
		TownColonizationSnapshot snapshot)
	{
		if (settlement?.Town == null || snapshot == null)
		{
			return false;
		}

		ActiveTownColonization.Restore(snapshot);
		if (!ActiveTownColonization.ResolvesAsColonization)
		{
			return false;
		}

		_activeSettlement = settlement;
		_activeSettlementId = settlement.StringId ?? string.Empty;
		_activeSettlementName = settlement.Name?.ToString() ?? string.Empty;
		_activeMode = InterventionMode.Massacre;
		_pendingMode = InterventionMode.None;
		_massacreStarted = true;
		_massacreStopped = false;
		_massacreVictoryReached = snapshot.CommitReason == TownColonizationCommitReason.CapturedTargetsEliminated;
		_pendingAftermath = SiegeAftermathAction.SiegeAftermath.Devastate;
		_pendingAftermathTrigger = "town_colonization_load_recovery";
		_pendingAftermathDetail = "Recovered a validated pending colonization outcome from the matching native aftermath context.";
		_hasPendingAftermath = true;
		return true;
	}

	private static bool FinalizeRecoveredTownColonizationCultureCommit(Settlement settlement)
	{
		if (settlement?.Town == null
			|| !ActiveTownColonization.IsSettlementOutcomeCommitted
			|| !ApplyCulturalRepopulationNow("town_colonization_load_recovery_culture_commit"))
		{
			return false;
		}

		EncounterCompletion.SetSummaryAftermath(SiegeAftermathResolutionKind.Devastate);
		_nativeDevastateAftermathFlowActive = true;
		EncounterCompletion.ResetNativeDevastateSummaryContinue();
		TrySetNativePlayerEncounterAftermathForSummary(SiegeAftermathAction.SiegeAftermath.Devastate);
		MarkAftermathResolvedForCompletion(settlement, SiegeAftermathAction.SiegeAftermath.Devastate);
		PrepareCompletedInterventionSummary(SiegeAftermathAction.SiegeAftermath.Devastate);
		_hasPendingAftermath = false;
		return true;
	}

	private static bool TryCaptureNativeSiegeContextForLoadRecovery(Settlement settlement)
	{
		try
		{
			SiegeAftermathCampaignBehavior behavior = Campaign.Current?.GetCampaignBehavior<SiegeAftermathCampaignBehavior>();
			if (behavior == null || settlement?.Town == null)
			{
				return false;
			}

			Type type = typeof(SiegeAftermathCampaignBehavior);
			MobileParty besiegerParty = ReadPrivateField<MobileParty>(behavior, type, "_besiegerParty");
			Clan previousOwner = ReadPrivateField<Clan>(behavior, type, "_prevSettlementOwnerClan");
			Dictionary<MobileParty, float> contributions = ReadPrivateField<Dictionary<MobileParty, float>>(behavior, type, "_siegeEventPartyContributions");
			if (besiegerParty == null || previousOwner == null || contributions == null || contributions.Count == 0)
			{
				return false;
			}

			_besiegerParty = besiegerParty;
			_previousSettlementOwnerClan = previousOwner;
			_partyContributions = new Dictionary<MobileParty, float>(contributions);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryCaptureNativeSiegeContextForLoadRecovery failed: " + ex.Message);
			return false;
		}
	}
}
