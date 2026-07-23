using System;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Mission-only adapter for the original execution notification and live captured-lord agent.
/// Authorization and wording remain in the independent castle GCCZ core.
/// </summary>
internal static class CastleAftermathLordExecutionRuntimeBridge
{
	private enum RuntimeStage
	{
		Queued,
		WaitingForNotification,
		FinalizingCampaignDeath
	}

	private sealed class PendingExecution
	{
		internal int Token;
		internal Hero Hero;
		internal Agent Agent;
		internal Mission Mission;
		internal RuntimeStage Stage;
		internal bool NotificationSeenActive;
		internal bool AffirmativeActionReceived;
		internal float ElapsedSinceNotificationRequest;
		internal bool MapEventWasActive;
		internal bool FinalizationWarningShown;
	}

	private static PendingExecution _pending;
	private static int _nextToken;

	internal static bool TryQueue(Hero hero, Agent agent, out string reasonCode)
	{
		reasonCode = string.Empty;
		Mission mission = Mission.Current;
		if (_pending != null)
		{
			reasonCode = "execution_already_pending";
			return false;
		}
		if (!TryValidateTarget(mission, hero, agent, out reasonCode))
		{
			return false;
		}
		try
		{
			if (MBInformationManager.GetIsAnySceneNotificationActive() == true)
			{
				reasonCode = "scene_notification_busy";
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Read scene notification state before lord execution failed: " + ex.Message);
		}

		int token = unchecked(++_nextToken);
		_pending = new PendingExecution
		{
			Token = token,
			Hero = hero,
			Agent = agent,
			Mission = mission,
			Stage = RuntimeStage.Queued
		};
		Logger.Log("CastleAftermath", "Queued original castle lord execution notification. Hero="
			+ (hero.StringId ?? "N/A") + ", Agent=" + agent.Index + ", Token=" + token);
		GcczDiagnosticLog.Log("CastleLordExecution", "queued hero=" + (hero.StringId ?? "N/A")
			+ " agent=" + agent.Index + " token=" + token);
		return true;
	}

	internal static void Tick(Mission mission, float dt)
	{
		PendingExecution pending = _pending;
		if (pending == null)
		{
			return;
		}

		bool contextAvailable = ReferenceEquals(mission, pending.Mission)
			&& mission != null
			&& !mission.IsMissionEnding
			&& SiegeAiInterventionBehavior.ShouldRunSiegeInterventionPostprocessForExternal()
			&& CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission);
		if (pending.Stage == RuntimeStage.FinalizingCampaignDeath)
		{
			ShowFinalizationPendingOnce(
				pending,
				contextAvailable ? "finalization_wait" : "context_ending_with_native_death_mark");
			return;
		}

		if (!contextAvailable)
		{
			Rollback(pending, "castle_execution_context_ended", showMessage: false, hideNotification: true);
			return;
		}

		if (pending.Stage == RuntimeStage.Queued)
		{
			OpenOriginalNotification(pending);
			return;
		}

		pending.ElapsedSinceNotificationRequest += Math.Max(0f, dt);
		bool notificationActive = false;
		try
		{
			notificationActive = MBInformationManager.GetIsAnySceneNotificationActive() == true;
			if (notificationActive)
			{
				pending.NotificationSeenActive = true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Read active lord execution notification state failed: " + ex.Message);
		}

		SiegeCastleLordExecutionFlowDecision decision = SiegeCastleLordExecutionFlowProfile.Evaluate(
			notificationActive,
			pending.NotificationSeenActive,
			pending.AffirmativeActionReceived,
			pending.ElapsedSinceNotificationRequest);
		switch (decision)
		{
			case SiegeCastleLordExecutionFlowDecision.Commit:
				Commit(pending);
				break;
			case SiegeCastleLordExecutionFlowDecision.Cancel:
				Rollback(pending, "player_cancelled_original_execution", showMessage: true, hideNotification: false);
				break;
			case SiegeCastleLordExecutionFlowDecision.OpenFailed:
				Rollback(pending, "original_execution_notification_not_opened", showMessage: true, hideNotification: false);
				break;
		}
	}

	internal static bool TryPrepareForMissionExit(Mission mission)
	{
		PendingExecution pending = _pending;
		if (pending == null || (mission != null && !ReferenceEquals(mission, pending.Mission)))
		{
			return true;
		}
		if (pending.Stage == RuntimeStage.FinalizingCampaignDeath || HasExecutionDeathMark(pending.Hero))
		{
			ShowFinalizationPendingOnce(pending, "mission_exit_request");
		}
		return _pending == null;
	}

	internal static string BuildMissionExitBlockedMessage()
	{
		return _pending?.Stage == RuntimeStage.FinalizingCampaignDeath
			|| HasExecutionDeathMark(_pending?.Hero)
				? SiegeCastleActionOutcomeTextProfile.BuildLordExecutionFinalizationPendingMessage(
					_pending?.Hero?.Name?.ToString())
				: SiegeCastleActionOutcomeTextProfile.BuildLordExecutionConfirmationPendingMessage(
					_pending?.Hero?.Name?.ToString());
	}

	internal static void CancelForMission(Mission mission, string source)
	{
		PendingExecution pending = _pending;
		if (pending == null || (mission != null && !ReferenceEquals(mission, pending.Mission)))
		{
			return;
		}
		if (pending.Stage == RuntimeStage.FinalizingCampaignDeath || HasExecutionDeathMark(pending.Hero))
		{
			ReleaseIrreversiblePending(
				pending,
				(source ?? "mission_cancelled_execution") + "_native_death_mark_handoff");
			return;
		}
		Rollback(pending, source ?? "mission_cancelled_execution", showMessage: false, hideNotification: true);
	}

	internal static void Reset(string source)
	{
		PendingExecution pending = _pending;
		if (pending != null)
		{
			if (pending.Stage == RuntimeStage.FinalizingCampaignDeath || HasExecutionDeathMark(pending.Hero))
			{
				ReleaseIrreversiblePending(
					pending,
					(source ?? "reset_execution_runtime") + "_native_death_mark_handoff");
			}
			else
			{
				Rollback(pending, source ?? "reset_execution_runtime", showMessage: false, hideNotification: true);
			}
		}
		_nextToken = 0;
	}

	private static void OpenOriginalNotification(PendingExecution pending)
	{
		if (!ReferenceEquals(_pending, pending))
		{
			return;
		}
		if (!TryValidateTarget(pending.Mission, pending.Hero, pending.Agent, out string reasonCode))
		{
			Rollback(pending, reasonCode, showMessage: true, hideNotification: false);
			return;
		}
		try
		{
			if (MBInformationManager.GetIsAnySceneNotificationActive() == true)
			{
				Rollback(pending, "scene_notification_became_busy", showMessage: true, hideNotification: false);
				return;
			}
			EndCurrentConversation();
			if (!ReferenceEquals(_pending, pending))
			{
				return;
			}

			pending.Stage = RuntimeStage.WaitingForNotification;
			pending.ElapsedSinceNotificationRequest = 0f;
			HeroExecutionSceneNotificationData notification =
				HeroExecutionSceneNotificationData.CreateForPlayerExecutingHero(
					pending.Hero,
					() => MarkAffirmative(pending.Token),
					SceneNotificationData.RelevantContextType.Mission,
					showNegativeOption: true);
			MBInformationManager.ShowSceneNotification(notification);
			pending.NotificationSeenActive = MBInformationManager.GetIsAnySceneNotificationActive() == true;
			Logger.Log("CastleAftermath", "Opened original castle lord execution notification. Hero="
				+ (pending.Hero.StringId ?? "N/A") + ", Token=" + pending.Token
				+ ", ActiveObserved=" + pending.NotificationSeenActive);
			GcczDiagnosticLog.Log("CastleLordExecution", "opened hero=" + (pending.Hero.StringId ?? "N/A")
				+ " token=" + pending.Token + " activeObserved=" + pending.NotificationSeenActive);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Open original castle lord execution notification failed. Hero="
				+ (pending.Hero?.StringId ?? "N/A") + ", Error=" + ex);
			Rollback(pending, "open_original_execution_exception", showMessage: true, hideNotification: false);
		}
	}

	private static void MarkAffirmative(int token)
	{
		PendingExecution pending = _pending;
		if (pending == null || pending.Token != token)
		{
			return;
		}
		pending.AffirmativeActionReceived = true;
		Logger.Log("CastleAftermath", "Player affirmed original castle lord execution. Hero="
			+ (pending.Hero?.StringId ?? "N/A") + ", Token=" + token);
		GcczDiagnosticLog.Log("CastleLordExecution", "affirmed hero="
			+ (pending.Hero?.StringId ?? "N/A") + " token=" + token);
	}

	private static void Commit(PendingExecution pending)
	{
		if (!ReferenceEquals(_pending, pending))
		{
			return;
		}
		if (pending.Stage == RuntimeStage.FinalizingCampaignDeath || HasExecutionDeathMark(pending.Hero))
		{
			ObserveCampaignDeathState(pending, "commit_reentry");
			return;
		}
		if (!TryValidateTarget(pending.Mission, pending.Hero, pending.Agent, out string reasonCode))
		{
			Rollback(pending, reasonCode, showMessage: true, hideNotification: false);
			return;
		}

		Hero hero = pending.Hero;
		Agent agent = pending.Agent;
		pending.MapEventWasActive = MobileParty.MainParty?.MapEvent != null;
		try
		{
			EndCurrentConversation();
			KillCharacterAction.ApplyByExecution(
				hero,
				Hero.MainHero,
				showNotification: true,
				isForced: true);

			SiegeCastleLordExecutionCampaignDecision campaignDecision =
				SiegeCastleLordExecutionFlowProfile.EvaluateCampaignState(
					hero.IsAlive,
					HasExecutionDeathMark(hero));
			if (campaignDecision == SiegeCastleLordExecutionCampaignDecision.Persisted)
			{
				CompleteAcceptedExecution(
					pending,
					hero,
					agent,
					pending.MapEventWasActive,
					"native_execution_persisted");
				return;
			}
			if (campaignDecision == SiegeCastleLordExecutionCampaignDecision.IrreversiblePending)
			{
				pending.Stage = RuntimeStage.FinalizingCampaignDeath;
				ShowFinalizationPendingOnce(pending, "native_execution_left_death_mark");
				return;
			}
			Rollback(pending, "native_execution_not_persisted", showMessage: true, hideNotification: false);
		}
		catch (Exception ex)
		{
			bool decisiveSideEffectApplied = !hero.IsAlive;
			Logger.Log("CastleAftermath", "Commit castle lord execution failed. Hero="
				+ (hero.StringId ?? "N/A") + ", DecisiveSideEffectApplied=" + decisiveSideEffectApplied
				+ ", ExecutionDeathMarkPresent=" + HasExecutionDeathMark(hero)
				+ ", Error=" + ex);
			if (decisiveSideEffectApplied)
			{
				CompleteAcceptedExecution(
					pending,
					hero,
					agent,
					pending.MapEventWasActive,
					"native_execution_recovery");
			}
			else if (HasExecutionDeathMark(hero))
			{
				pending.Stage = RuntimeStage.FinalizingCampaignDeath;
				ShowFinalizationPendingOnce(pending, "native_execution_exception_after_death_mark");
			}
			else
			{
				Rollback(pending, "native_execution_exception", showMessage: true, hideNotification: false);
			}
		}
	}

	private static void ObserveCampaignDeathState(PendingExecution pending, string source)
	{
		if (pending == null || !ReferenceEquals(_pending, pending))
		{
			return;
		}

		Hero hero = pending.Hero;
		if (hero == null)
		{
			Rollback(
				pending,
				"native_execution_not_persisted",
				showMessage: true,
				hideNotification: false);
			return;
		}
		SiegeCastleLordExecutionCampaignDecision campaignDecision =
			SiegeCastleLordExecutionFlowProfile.EvaluateCampaignState(
				hero.IsAlive,
				HasExecutionDeathMark(hero));
		if (campaignDecision == SiegeCastleLordExecutionCampaignDecision.Persisted)
		{
			CompleteAcceptedExecution(
				pending,
				hero,
				pending.Agent,
				pending.MapEventWasActive,
				(source ?? "death_mark_observe") + "_persisted");
			return;
		}
		if (campaignDecision == SiegeCastleLordExecutionCampaignDecision.Failed)
		{
			Rollback(
				pending,
				"native_execution_not_persisted",
				showMessage: true,
				hideNotification: false);
			return;
		}

		pending.Stage = RuntimeStage.FinalizingCampaignDeath;
		ShowFinalizationPendingOnce(pending, source);
	}

	private static void ShowFinalizationPendingOnce(PendingExecution pending, string source)
	{
		if (pending == null || pending.FinalizationWarningShown)
		{
			return;
		}
		pending.FinalizationWarningShown = true;
		InformationManager.DisplayMessage(new InformationMessage(
			SiegeCastleActionOutcomeTextProfile.BuildLordExecutionFinalizationPendingMessage(
				pending.Hero?.Name?.ToString()),
			Color.FromUint(SiegeCastleActionOutcomeTextProfile.WarningColor)));
		Logger.Log("CastleAftermath", "Castle lord execution campaign finalization pending. Hero="
			+ (pending.Hero?.StringId ?? "N/A") + ", Source=" + (source ?? "N/A"));
		GcczDiagnosticLog.Log("CastleLordExecution", "finalizationPending hero="
			+ (pending.Hero?.StringId ?? "N/A") + " source=" + (source ?? "N/A"));
	}

	private static bool HasExecutionDeathMark(Hero hero)
	{
		return hero != null
			&& (hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Executed
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent);
	}

	private static void CompleteAcceptedExecution(
		PendingExecution pending,
		Hero hero,
		Agent agent,
		bool mapEventWasActive,
		string source)
	{
		if (!ReferenceEquals(_pending, pending))
		{
			return;
		}
		_pending = null;
		bool sceneDeathApplied = TryKillSceneAgent(pending.Mission, agent);
		try
		{
			CastleAftermathRuntimeBridge.ResolveExecutedLordPrisoner(
				hero,
				SiegeCastleLordExecutionFlowProfile.RuntimeSource);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Resolve accepted castle lord execution failed. Hero="
				+ (hero?.StringId ?? "N/A") + ", Error=" + ex);
		}
		try
		{
			SiegeAiInterventionBehavior.NotifyCastleLordExecutedForExternal(
				hero,
				sceneDeathApplied);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Record accepted castle lord execution outcome failed. Hero="
				+ (hero?.StringId ?? "N/A") + ", Error=" + ex);
		}
		Logger.Log("CastleAftermath", "Committed castle lord execution. Hero=" + (hero?.StringId ?? "N/A")
			+ ", CampaignDeathPersisted=" + (hero?.IsAlive == false)
			+ ", MapEventWasActive=" + mapEventWasActive
			+ ", SceneDeath=" + sceneDeathApplied
			+ ", Source=" + (source ?? "N/A"));
		GcczDiagnosticLog.Log("CastleLordExecution", "committed hero=" + (hero?.StringId ?? "N/A")
			+ " campaignDeathPersisted=" + (hero?.IsAlive == false)
			+ " mapEventWasActive=" + mapEventWasActive
			+ " sceneDeath=" + sceneDeathApplied
			+ " source=" + (source ?? "N/A"));
	}

	private static bool TryKillSceneAgent(Mission mission, Agent agent)
	{
		if (mission == null || agent == null)
		{
			return false;
		}
		try
		{
			if (!agent.IsActive() || agent.State == AgentState.Killed)
			{
				return true;
			}
			if (agent.Origin is PrisonerAgentOrigin prisonerOrigin)
			{
				prisonerOrigin.MarkCampaignCasualtyHandledExternally(
					SiegeCastleLordExecutionFlowProfile.RuntimeSource);
			}
			agent.SetMortalityState(Agent.MortalityState.Mortal);
			agent.SetIsAIPaused(isPaused: false);
			agent.DisableScriptedMovement();
			Blow blow = new Blow
			{
				DamageCalculated = true,
				BaseMagnitude = 2000f,
				InflictedDamage = 2000,
				DamagedPercentage = 1f,
				OwnerId = Agent.Main?.Index ?? -1
			};
			agent.Die(blow, Agent.KillInfo.Invalid);
			if (!agent.IsActive() || agent.State == AgentState.Killed)
			{
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Direct castle lord scene death failed. Agent="
				+ agent.Index + ", Error=" + ex.Message);
		}

		try
		{
			mission.KillAgentCheat(agent);
			return !agent.IsActive() || agent.State == AgentState.Killed;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Fallback castle lord scene death failed. Agent="
				+ agent.Index + ", Error=" + ex.Message);
			return false;
		}
	}

	private static bool TryValidateTarget(Mission mission, Hero hero, Agent agent, out string reasonCode)
	{
		reasonCode = string.Empty;
		if (mission == null || mission.IsMissionEnding)
		{
			reasonCode = "castle_mission_unavailable";
			return false;
		}
		if (!SiegeAiInterventionBehavior.ShouldRunSiegeInterventionPostprocessForExternal()
			|| !CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission))
		{
			reasonCode = "castle_stage_inactive";
			return false;
		}
		if (hero == null || hero == Hero.MainHero || !hero.IsAlive || !hero.IsLord)
		{
			reasonCode = "invalid_lord_target";
			return false;
		}
		if (!hero.IsPrisoner && hero.PartyBelongedToAsPrisoner == null)
		{
			reasonCode = "lord_not_current_prisoner";
			return false;
		}
		if (agent == null
			|| !agent.IsActive()
			|| agent.State == AgentState.Killed
			|| agent.State == AgentState.Unconscious
			|| !CastleAftermathRuntimeBridge.IsLordPrisonerAgent(agent))
		{
			reasonCode = "invalid_lord_scene_agent";
			return false;
		}
		Hero agentHero = (agent.Character as CharacterObject)?.HeroObject;
		if (agentHero != hero || !CastleAftermathRuntimeBridge.ContainsSelectedLord(hero))
		{
			reasonCode = "lord_not_in_selected_castle_roster";
			return false;
		}
		return true;
	}

	private static void Rollback(
		PendingExecution pending,
		string reasonCode,
		bool showMessage,
		bool hideNotification)
	{
		if (pending == null || !ReferenceEquals(_pending, pending))
		{
			return;
		}
		if (HasExecutionDeathMark(pending.Hero))
		{
			pending.Stage = RuntimeStage.FinalizingCampaignDeath;
			ShowFinalizationPendingOnce(pending, reasonCode);
			Logger.Log("CastleAftermath", "Suppressed castle lord execution rollback after native death mark. Hero="
				+ (pending.Hero?.StringId ?? "N/A") + ", Reason=" + (reasonCode ?? "N/A"));
			GcczDiagnosticLog.Log("CastleLordExecution", "rollbackSuppressed hero="
				+ (pending.Hero?.StringId ?? "N/A") + " reason=" + (reasonCode ?? "N/A"));
			return;
		}
		_pending = null;
		CastleAftermathDispositionSessionBridge.UnmarkApplied(
			SiegeCastleActionKind.ExecuteLord,
			SiegeCastleActionSpeakerRole.CapturedLord,
			pending.Agent,
			pending.Hero);
		if (hideNotification)
		{
			try
			{
				if (MBInformationManager.GetIsAnySceneNotificationActive() == true)
				{
					MBInformationManager.HideSceneNotification();
				}
			}
			catch (Exception ex)
			{
				Logger.Log("CastleAftermath", "Hide pending lord execution notification failed: " + ex.Message);
			}
		}
		if (showMessage)
		{
			bool cancelled = string.Equals(
				reasonCode,
				"player_cancelled_original_execution",
				StringComparison.Ordinal);
			bool persistenceFailed = string.Equals(
					reasonCode,
					"native_execution_not_persisted",
					StringComparison.Ordinal)
				|| string.Equals(
					reasonCode,
					"native_execution_exception",
					StringComparison.Ordinal);
			string message = cancelled
				? SiegeCastleActionOutcomeTextProfile.BuildLordExecutionCancelledMessage(pending.Hero?.Name?.ToString())
				: persistenceFailed
					? SiegeCastleActionOutcomeTextProfile.BuildLordExecutionPersistenceFailedMessage(
						pending.Hero?.Name?.ToString())
					: SiegeCastleActionOutcomeTextProfile.BuildLordExecutionFailedMessage(pending.Hero?.Name?.ToString());
			InformationManager.DisplayMessage(new InformationMessage(
				message,
				Color.FromUint(SiegeCastleActionOutcomeTextProfile.WarningColor)));
		}
		Logger.Log("CastleAftermath", "Rolled back castle lord execution. Hero="
			+ (pending.Hero?.StringId ?? "N/A") + ", Reason=" + (reasonCode ?? "N/A"));
		GcczDiagnosticLog.Log("CastleLordExecution", "rollback hero="
			+ (pending.Hero?.StringId ?? "N/A") + " reason=" + (reasonCode ?? "N/A"));
	}

	private static void ReleaseIrreversiblePending(PendingExecution pending, string source)
	{
		if (pending == null || !ReferenceEquals(_pending, pending))
		{
			return;
		}
		_pending = null;
		Logger.Log("CastleAftermath", "Released castle lord execution runtime with native death mark intact. Hero="
			+ (pending.Hero?.StringId ?? "N/A") + ", Alive=" + (pending.Hero?.IsAlive == true)
			+ ", DeathMark=" + (pending.Hero?.DeathMark.ToString() ?? "N/A")
			+ ", Source=" + (source ?? "N/A"));
		GcczDiagnosticLog.Log("CastleLordExecution", "nativeDeathMarkHandoff hero="
			+ (pending.Hero?.StringId ?? "N/A") + " alive=" + (pending.Hero?.IsAlive == true)
			+ " deathMark=" + (pending.Hero?.DeathMark.ToString() ?? "N/A")
			+ " source=" + (source ?? "N/A"));
	}

	private static void EndCurrentConversation()
	{
		try
		{
			AnimusForgeNativeConversationOverlay.CloseActive();
		}
		catch
		{
		}
		try
		{
			ShoutBehavior.CloseNativeConversationInputForExternal();
		}
		catch
		{
		}
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				Campaign.Current.ConversationManager.EndConversation();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "End conversation before castle lord execution failed: " + ex.Message);
		}
	}
}
