using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
	private static void QueueDirectMassacreAftermathScript(string reason)
	{
		QueueDirectAftermathScript(TownDirectAftermathKind.Massacre, reason);
	}

	private static void QueueDirectPlunderAftermathScript(string reason)
	{
		QueueDirectAftermathScript(TownDirectAftermathKind.Plunder, reason);
	}

	private static void QueueDirectAftermathScript(TownDirectAftermathKind kind, string reason)
	{
		try
		{
			DirectAftermathFlow.Queue(kind);
			EncounterCompletion.BeginSummary(kind == TownDirectAftermathKind.Massacre
				? SiegeAftermathResolutionKind.Devastate
				: SiegeAftermathResolutionKind.Pillage);
			EncounterCompletion.ClearTransition(preserveSummary: true);
			if (kind == TownDirectAftermathKind.Massacre)
			{
				_nativeDevastateAftermathFlowActive = false;
				EncounterCompletion.TryHandleNativeDevastateSummaryContinue();
			}
			Logger.Log(
				"SiegeAiIntervention",
				"Queued direct AF " + DescribeDirectAftermathKind(kind)
				+ " aftermath script. Reason=" + (reason ?? "N/A")
				+ ", LootItems=" + (_pendingLootRoster?.Count ?? 0)
				+ ", MarketGold=" + _lastMarketGoldLoot
				+ ", CivilianGold=" + _lastCivilianGoldLoot);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "QueueDirectAftermathScript failed. Kind=" + kind + ", Error=" + ex.Message);
		}
	}

	private static bool TryRunDirectMassacreAftermathScript(
		string source = SiegeDirectAftermathSourceProfile.CampaignTickDirectMassacreScriptSource)
	{
		return TryRunDirectAftermathScript(TownDirectAftermathKind.Massacre, source);
	}

	private static bool TryRunDirectPlunderAftermathScript(
		string source = SiegeDirectAftermathSourceProfile.CampaignTickDirectPlunderScriptSource)
	{
		return TryRunDirectAftermathScript(TownDirectAftermathKind.Plunder, source);
	}

	private static bool TryRunDirectAftermathScript(TownDirectAftermathKind kind, string source)
	{
		if (!DirectAftermathFlow.IsPendingFor(kind) || Mission.Current != null)
		{
			return false;
		}

		try
		{
			if (_hasPendingAftermath)
			{
				FinalizePendingAftermath(ResolveDirectPendingAftermathSource(kind));
			}
			if (!_afAftermathResolved)
			{
				return true;
			}

			string pumpSource = source ?? ResolveDirectFallbackPumpSource(kind);
			if (!IsSafeToOpenDirectAftermathLootScreen(kind, pumpSource))
			{
				return true;
			}

			if (kind == TownDirectAftermathKind.Massacre)
			{
				_nativeDevastateAftermathFlowActive = false;
				EncounterCompletion.TryHandleNativeDevastateSummaryContinue();
			}
			TryExitCurrentCampaignMenu();

			if (DirectAftermathFlow.IsWaitingForLootClose)
			{
				if (Game.Current?.GameStateManager?.ActiveState is InventoryState)
				{
					return true;
				}
				DirectAftermathFlow.TryMarkLootScreenClosed(kind);
			}

			if (DirectAftermathFlow.IsAwaitingEncounterFinish)
			{
				return TryCompleteDirectAftermathScript(kind, afterLoot: true);
			}

			if (!DirectAftermathFlow.HasOpenedLootScreen && _pendingLootRoster != null && _pendingLootRoster.Count > 0)
			{
				TryOpenDirectAftermathLootScreenNow(kind, pumpSource);
				return true;
			}

			ShowDirectAftermathLootMessage(kind);
			return TryCompleteDirectAftermathScript(kind, afterLoot: false);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryRunDirectAftermathScript failed. Kind=" + kind + ", Error=" + ex.Message);
			return true;
		}
	}

	private static bool TryCompleteDirectAftermathScript(TownDirectAftermathKind kind, bool afterLoot)
	{
		SiegeAftermathAction.SiegeAftermath aftermath = ResolveDirectAftermath(kind);
		string finishSource = afterLoot
			? ResolveDirectAfterLootSource(kind)
			: ResolveDirectNoLootSource(kind);
		QueueEncounterFinishAfterIntervention(aftermath, finishSource, 0, forceDelay: true);
		if (!TryFinishPlayerEncounterAfterInterventionNow(aftermath, finishSource))
		{
			return true;
		}

		DirectAftermathFlow.Complete(kind);
		EncounterCompletion.ClearTransition(preserveSummary: false);
		ClearActiveState(preserveSummarySwitch: false);
		Logger.Log(
			"SiegeAiIntervention",
			"Direct AF " + DescribeDirectAftermathKind(kind)
			+ " aftermath script completed " + (afterLoot ? "after loot screen." : "without loot screen."));
		return true;
	}

	internal static bool TryHandleDirectMassacreAftermathMenuForExternal(string menuId, string source)
	{
		return TryHandleDirectAftermathMenuForExternal(TownDirectAftermathKind.Massacre, menuId, source);
	}

	internal static bool TryHandleDirectPlunderAftermathMenuForExternal(string menuId, string source)
	{
		return TryHandleDirectAftermathMenuForExternal(TownDirectAftermathKind.Plunder, menuId, source);
	}

	private static bool TryHandleDirectAftermathMenuForExternal(
		TownDirectAftermathKind kind,
		string menuId,
		string source)
	{
		try
		{
			if (!DirectAftermathFlow.IsPendingFor(kind)
				|| string.IsNullOrWhiteSpace(menuId)
				|| !IsNativeSiegeAftermathMenuId(menuId))
			{
				return false;
			}

			string kindName = DescribeDirectAftermathKind(kind);
			Logger.Log(
				"SiegeAiIntervention",
				"Intercepted native siege aftermath menu for direct AF " + kindName
				+ " loot script. Menu=" + menuId + ", Source=" + (source ?? "N/A"));
			if (Mission.Current != null)
			{
				LogDirectAftermathLootDeferOnce(
					kind,
					SiegeDirectAftermathSourceProfile.BuildNativeMenuMissionCurrentSource(menuId),
					"Suppressed native siege aftermath menu while Mission.Current is still active; direct AF "
					+ kindName + " loot will be pumped after MapState. Menu=" + menuId + ", Source=" + (source ?? "N/A"));
				return true;
			}

			string openSource = source ?? SiegeDirectAftermathSourceProfile.NativeMenuInterceptSource;
			if (!TryOpenDirectAftermathLootScreenNow(kind, openSource)
				&& IsSafeToOpenDirectAftermathLootScreen(
					kind,
					source ?? SiegeDirectAftermathSourceProfile.NativeMenuInterceptNoLootProbeSource))
			{
				SiegeAftermathAction.SiegeAftermath aftermath = ResolveDirectAftermath(kind);
				string noLootSource = ResolveDirectNativeMenuNoLootSource(kind);
				QueueEncounterFinishAfterIntervention(aftermath, noLootSource, 0, forceDelay: true);
				TryFinishPlayerEncounterAfterInterventionNow(aftermath, noLootSource);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log(
				"SiegeAiIntervention",
				"TryHandleDirectAftermathMenuForExternal failed. Kind=" + kind
				+ ", Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	internal static bool TryPumpDirectMassacreAftermathScriptForExternal(string source)
	{
		return TryPumpDirectAftermathScriptForExternal(
			TownDirectAftermathKind.Massacre,
			source ?? SiegeDirectAftermathSourceProfile.ExternalDirectMassacreScriptSource);
	}

	internal static bool TryPumpDirectPlunderAftermathScriptForExternal(string source)
	{
		return TryPumpDirectAftermathScriptForExternal(
			TownDirectAftermathKind.Plunder,
			source ?? SiegeDirectAftermathSourceProfile.ExternalDirectPlunderScriptSource);
	}

	private static bool TryPumpDirectAftermathScriptForExternal(TownDirectAftermathKind kind, string source)
	{
		try
		{
			return DirectAftermathFlow.IsPendingFor(kind)
				&& TryRunDirectAftermathScript(kind, source);
		}
		catch (Exception ex)
		{
			Logger.Log(
				"SiegeAiIntervention",
				"TryPumpDirectAftermathScriptForExternal failed. Kind=" + kind
				+ ", Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return DirectAftermathFlow.IsPendingFor(kind);
		}
	}

	private static bool TryOpenDirectAftermathLootScreenNow(TownDirectAftermathKind kind, string source)
	{
		try
		{
			if (!DirectAftermathFlow.IsPendingFor(kind)
				|| DirectAftermathFlow.HasOpenedLootScreen
				|| _pendingLootRoster == null
				|| _pendingLootRoster.Count <= 0
				|| !IsSafeToOpenDirectAftermathLootScreen(kind, source)
				|| !DirectAftermathFlow.TryBeginLootScreen(kind))
			{
				return false;
			}

			_pendingLootScreen = true;
			_pendingLootScreenShown = true;
			if (kind == TownDirectAftermathKind.Massacre)
			{
				_nativeDevastateAftermathFlowActive = false;
				EncounterCompletion.TryHandleNativeDevastateSummaryContinue();
			}
			ShowDirectAftermathLootMessage(kind);
			TryExitCurrentCampaignMenu();
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster>
			{
				{ PartyBase.MainParty, _pendingLootRoster },
			});
			Logger.Log(
				"SiegeAiIntervention",
				"Direct AF " + DescribeDirectAftermathKind(kind)
				+ " script opened loot screen immediately. Source=" + (source ?? "N/A")
				+ ", LootItems=" + _pendingLootRoster.Count
				+ ", MarketGold=" + _lastMarketGoldLoot
				+ ", CivilianGold=" + _lastCivilianGoldLoot);
			return true;
		}
		catch (Exception ex)
		{
			DirectAftermathFlow.TryRecoverLootScreenOpenFailure(kind);
			_pendingLootScreenShown = false;
			Logger.Log(
				"SiegeAiIntervention",
				"TryOpenDirectAftermathLootScreenNow failed. Kind=" + kind
				+ ", Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static bool IsSafeToOpenDirectAftermathLootScreen(TownDirectAftermathKind kind, string source)
	{
		try
		{
			string kindName = DescribeDirectAftermathKind(kind);
			if (Mission.Current != null)
			{
				LogDirectAftermathLootDeferOnce(
					kind,
					SiegeDirectAftermathSourceProfile.MissionCurrentLootDeferSource,
					"Direct " + kindName + " loot screen deferred because Mission.Current is still active. Source=" + (source ?? "N/A"));
				return false;
			}

			object activeState = Game.Current?.GameStateManager?.ActiveState;
			if (activeState == null)
			{
				LogDirectAftermathLootDeferOnce(
					kind,
					SiegeDirectAftermathSourceProfile.NullStateLootDeferSource,
					"Direct " + kindName + " loot screen deferred because active game state is null. Source=" + (source ?? "N/A"));
				return false;
			}
			if (activeState is InventoryState)
			{
				return false;
			}
			if (activeState is MapState)
			{
				return true;
			}

			string stateName = activeState.GetType().FullName ?? activeState.GetType().Name;
			LogDirectAftermathLootDeferOnce(
				kind,
				SiegeDirectAftermathSourceProfile.BuildActiveStateLootDeferSource(stateName),
				"Direct " + kindName + " loot screen deferred until MapState. Source="
				+ (source ?? "N/A") + ", ActiveState=" + stateName);
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log(
				"SiegeAiIntervention",
				"IsSafeToOpenDirectAftermathLootScreen failed. Kind=" + kind
				+ ", Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static void LogDirectAftermathLootDeferOnce(
		TownDirectAftermathKind kind,
		string key,
		string message)
	{
		try
		{
			if (DirectAftermathFlow.TrySetDeferKey(kind, key))
			{
				Logger.Log("SiegeAiIntervention", message);
			}
		}
		catch
		{
		}
	}

	private static void ShowDirectAftermathLootMessage(TownDirectAftermathKind kind)
	{
		if (!DirectAftermathFlow.TryClaimMessage(kind))
		{
			return;
		}

		if (kind == TownDirectAftermathKind.Massacre
			&& IsCulturalRepopulationOutcome
			&& !IsCulturalRepopulationCommitted)
		{
			ApplyCulturalRepopulationNow(SiegeCulturalRepopulationProfile.DirectMassacreLootMessageApplySource);
		}

		try
		{
			if (kind == TownDirectAftermathKind.Massacre)
			{
				string action = IsCulturalRepopulationOutcome
					? SiegeLootAccountingProfile.CulturalRepopulationActionName
					: SiegeLootAccountingProfile.MassacreActionName;
				InformationManager.DisplayMessage(new InformationMessage(
					SiegeLootAccountingProfile.BuildDirectDevastateSettlementMessage(action),
					Color.FromUint(SiegeLootAccountingProfile.DirectDevastateSettlementMessageColor)));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(
					SiegeLootAccountingProfile.BuildDirectPlunderSettlementMessage(),
					Color.FromUint(SiegeLootAccountingProfile.DirectPlunderSettlementMessageColor)));
			}

			InformationManager.DisplayMessage(new InformationMessage(
				SiegeLootAccountingProfile.BuildLootCreditedSummaryMessage(
					_lastMarketGoldLoot,
					_lastCivilianGoldLoot,
					_lastLootItemTotal,
					_lastLootStackKinds),
				Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
		}
		catch
		{
		}
	}

	private static void TryExitCurrentCampaignMenu()
	{
		try
		{
			if (Campaign.Current?.CurrentMenuContext != null)
			{
				GameMenu.ExitToLast();
			}
		}
		catch
		{
		}
	}

	private static SiegeAftermathAction.SiegeAftermath ResolveDirectAftermath(TownDirectAftermathKind kind)
	{
		return kind == TownDirectAftermathKind.Massacre
			? SiegeAftermathAction.SiegeAftermath.Devastate
			: SiegeAftermathAction.SiegeAftermath.Pillage;
	}

	private static string ResolveDirectPendingAftermathSource(TownDirectAftermathKind kind)
	{
		return kind == TownDirectAftermathKind.Massacre
			? SiegeDirectAftermathSourceProfile.DirectMassacrePendingAftermathSource
			: SiegeDirectAftermathSourceProfile.DirectPlunderPendingAftermathSource;
	}

	private static string ResolveDirectFallbackPumpSource(TownDirectAftermathKind kind)
	{
		return kind == TownDirectAftermathKind.Massacre
			? SiegeDirectAftermathSourceProfile.DirectMassacreFallbackPumpSource
			: SiegeDirectAftermathSourceProfile.DirectPlunderFallbackPumpSource;
	}

	private static string ResolveDirectAfterLootSource(TownDirectAftermathKind kind)
	{
		return kind == TownDirectAftermathKind.Massacre
			? SiegeDirectAftermathSourceProfile.DirectMassacreAfterLootSource
			: SiegeDirectAftermathSourceProfile.DirectPlunderAfterLootSource;
	}

	private static string ResolveDirectNoLootSource(TownDirectAftermathKind kind)
	{
		return kind == TownDirectAftermathKind.Massacre
			? SiegeDirectAftermathSourceProfile.DirectMassacreNoLootSource
			: SiegeDirectAftermathSourceProfile.DirectPlunderNoLootSource;
	}

	private static string ResolveDirectNativeMenuNoLootSource(TownDirectAftermathKind kind)
	{
		return kind == TownDirectAftermathKind.Massacre
			? SiegeDirectAftermathSourceProfile.DirectMassacreNativeMenuNoLootSource
			: SiegeDirectAftermathSourceProfile.DirectPlunderNativeMenuNoLootSource;
	}

	private static string DescribeDirectAftermathKind(TownDirectAftermathKind kind)
	{
		return kind == TownDirectAftermathKind.Massacre ? "massacre" : "plunder";
	}
}
