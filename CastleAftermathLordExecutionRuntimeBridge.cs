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
		WaitingForNotification
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
		if (!ReferenceEquals(mission, pending.Mission)
			|| mission == null
			|| mission.IsMissionEnding
			|| !SiegeAiInterventionBehavior.ShouldRunSiegeInterventionPostprocessForExternal()
			|| !CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission))
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

	internal static void CancelForMission(Mission mission, string source)
	{
		PendingExecution pending = _pending;
		if (pending != null && (mission == null || ReferenceEquals(mission, pending.Mission)))
		{
			Rollback(pending, source ?? "mission_cancelled_execution", showMessage: false, hideNotification: true);
		}
	}

	internal static void Reset(string source)
	{
		PendingExecution pending = _pending;
		if (pending != null)
		{
			Rollback(pending, source ?? "reset_execution_runtime", showMessage: false, hideNotification: true);
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
		if (!TryValidateTarget(pending.Mission, pending.Hero, pending.Agent, out string reasonCode))
		{
			Rollback(pending, reasonCode, showMessage: true, hideNotification: false);
			return;
		}

		Hero hero = pending.Hero;
		Agent agent = pending.Agent;
		bool deferredByMapEvent = MobileParty.MainParty?.MapEvent != null;
		try
		{
			EndCurrentConversation();
			if (deferredByMapEvent)
			{
				KillCharacterAction.ApplyByExecutionAfterMapEvent(
					hero,
					Hero.MainHero,
					showNotification: true,
					isForced: true);
			}
			else
			{
				KillCharacterAction.ApplyByExecution(
					hero,
					Hero.MainHero,
					showNotification: true,
					isForced: true);
			}

			bool nativeExecutionAccepted = !hero.IsAlive
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Executed
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent;
			if (!nativeExecutionAccepted)
			{
				Rollback(pending, "native_execution_not_accepted", showMessage: true, hideNotification: false);
				return;
			}
			CompleteAcceptedExecution(pending, hero, agent, deferredByMapEvent, "native_execution_accepted");
		}
		catch (Exception ex)
		{
			bool decisiveSideEffectApplied = !hero.IsAlive
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Executed
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent;
			Logger.Log("CastleAftermath", "Commit castle lord execution failed. Hero="
				+ (hero.StringId ?? "N/A") + ", DecisiveSideEffectApplied=" + decisiveSideEffectApplied
				+ ", Error=" + ex);
			if (decisiveSideEffectApplied)
			{
				CompleteAcceptedExecution(pending, hero, agent, deferredByMapEvent, "native_execution_recovery");
			}
			else
			{
				Rollback(pending, "native_execution_exception", showMessage: true, hideNotification: false);
			}
		}
	}

	private static void CompleteAcceptedExecution(
		PendingExecution pending,
		Hero hero,
		Agent agent,
		bool deferredByMapEvent,
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
				deferredByMapEvent,
				sceneDeathApplied);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Record accepted castle lord execution outcome failed. Hero="
				+ (hero?.StringId ?? "N/A") + ", Error=" + ex);
		}
		Logger.Log("CastleAftermath", "Committed castle lord execution. Hero=" + (hero?.StringId ?? "N/A")
			+ ", DeferredByMapEvent=" + deferredByMapEvent + ", SceneDeath=" + sceneDeathApplied
			+ ", Source=" + (source ?? "N/A"));
		GcczDiagnosticLog.Log("CastleLordExecution", "committed hero=" + (hero?.StringId ?? "N/A")
			+ " deferred=" + deferredByMapEvent + " sceneDeath=" + sceneDeathApplied
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
			string message = cancelled
				? SiegeCastleActionOutcomeTextProfile.BuildLordExecutionCancelledMessage(pending.Hero?.Name?.ToString())
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
