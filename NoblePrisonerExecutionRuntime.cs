using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class NoblePrisonerExecutionRuntime
{
	private const float NotificationOpenTimeoutSeconds = 2.5f;

	private enum Stage
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
		internal Stage Stage;
		internal bool NotificationSeenActive;
		internal bool Affirmative;
		internal float Elapsed;
		internal bool EscalateMeetingAfterCommit;
	}

	private static PendingExecution _pending;
	private static int _nextToken;

	internal static bool TryQueue(Hero hero, Agent agent, out string reason)
	{
		reason = string.Empty;
		Mission mission = Mission.Current;
		if (_pending != null)
		{
			reason = "execution_already_pending";
			return false;
		}
		if (!TryValidate(mission, hero, agent, out reason))
		{
			return false;
		}
		try
		{
			if (MBInformationManager.GetIsAnySceneNotificationActive() == true)
			{
				reason = "scene_notification_busy";
				return false;
			}
		}
		catch
		{
		}

		_pending = new PendingExecution
		{
			Token = unchecked(++_nextToken),
			Hero = hero,
			Agent = agent,
			Mission = mission,
			Stage = Stage.Queued,
			EscalateMeetingAfterCommit = ShouldEscalateMeetingForExecution(hero)
		};
		NoblePrisonerEscortLog.Log("Queued original execution confirmation. hero=" + (hero.StringId ?? "N/A")
			+ ", agent=" + agent.Index + ", token=" + _pending.Token
			+ ", escalateMeeting=" + _pending.EscalateMeetingAfterCommit);
		return true;
	}

	internal static void Tick(Mission mission, float dt)
	{
		PendingExecution pending = _pending;
		if (pending == null)
		{
			return;
		}
		if (mission == null || mission.IsMissionEnding || !ReferenceEquals(mission, pending.Mission))
		{
			Rollback(pending, "execution_context_ended", showMessage: false, hideNotification: true);
			return;
		}
		if (pending.Stage == Stage.Queued)
		{
			OpenNotification(pending);
			return;
		}

		pending.Elapsed += Math.Max(0f, dt);
		bool active = false;
		try
		{
			active = MBInformationManager.GetIsAnySceneNotificationActive() == true;
			pending.NotificationSeenActive |= active;
		}
		catch
		{
		}
		if (pending.Affirmative && !active)
		{
			Commit(pending);
			return;
		}
		if (pending.NotificationSeenActive && !active)
		{
			Rollback(pending, "player_cancelled_execution", showMessage: true, hideNotification: false);
			return;
		}
		if (!pending.NotificationSeenActive && pending.Elapsed >= NotificationOpenTimeoutSeconds)
		{
			Rollback(pending, "execution_notification_not_opened", showMessage: true, hideNotification: false);
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
		if (_pending != null)
		{
			Rollback(_pending, source ?? "reset", showMessage: false, hideNotification: true);
		}
		_nextToken = 0;
	}

	private static void OpenNotification(PendingExecution pending)
	{
		string reason = "pending_execution_mismatch";
		if (!ReferenceEquals(_pending, pending) || !TryValidate(pending.Mission, pending.Hero, pending.Agent, out reason))
		{
			Rollback(pending, reason, showMessage: true, hideNotification: false);
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
			pending.Stage = Stage.WaitingForNotification;
			pending.Elapsed = 0f;
			HeroExecutionSceneNotificationData notification = HeroExecutionSceneNotificationData.CreateForPlayerExecutingHero(
				pending.Hero,
				() => MarkAffirmative(pending.Token),
				SceneNotificationData.RelevantContextType.Mission,
				showNegativeOption: true);
			MBInformationManager.ShowSceneNotification(notification);
			pending.NotificationSeenActive = MBInformationManager.GetIsAnySceneNotificationActive() == true;
			NoblePrisonerEscortLog.Log("Opened original execution confirmation. hero=" + (pending.Hero.StringId ?? "N/A") + ", token=" + pending.Token);
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Open execution confirmation failed. hero=" + (pending.Hero?.StringId ?? "N/A") + ", error=" + ex);
			Rollback(pending, "open_execution_exception", showMessage: true, hideNotification: false);
		}
	}

	private static void MarkAffirmative(int token)
	{
		if (_pending != null && _pending.Token == token)
		{
			_pending.Affirmative = true;
		}
	}

	private static void Commit(PendingExecution pending)
	{
		string reason = "pending_execution_mismatch";
		if (!ReferenceEquals(_pending, pending) || !TryValidate(pending.Mission, pending.Hero, pending.Agent, out reason))
		{
			Rollback(pending, reason, showMessage: true, hideNotification: false);
			return;
		}
		Hero hero = pending.Hero;
		bool deferredByMapEvent = MobileParty.MainParty?.MapEvent != null;
		try
		{
			EndCurrentConversation();
			if (deferredByMapEvent)
			{
				KillCharacterAction.ApplyByExecutionAfterMapEvent(hero, Hero.MainHero, showNotification: true, isForced: true);
			}
			else
			{
				KillCharacterAction.ApplyByExecution(hero, Hero.MainHero, showNotification: true, isForced: true);
			}
			bool accepted = !hero.IsAlive
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Executed
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent;
			if (!accepted)
			{
				Rollback(pending, "native_execution_not_accepted", showMessage: true, hideNotification: false);
				return;
			}
			CompleteAccepted(pending, deferredByMapEvent);
		}
		catch (Exception ex)
		{
			bool accepted = !hero.IsAlive
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Executed
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent;
			NoblePrisonerEscortLog.Log("Commit execution failed. hero=" + (hero.StringId ?? "N/A") + ", accepted=" + accepted + ", error=" + ex);
			if (accepted)
			{
				CompleteAccepted(pending, deferredByMapEvent);
			}
			else
			{
				Rollback(pending, "native_execution_exception", showMessage: true, hideNotification: false);
			}
		}
	}

	private static void CompleteAccepted(PendingExecution pending, bool deferredByMapEvent)
	{
		if (!ReferenceEquals(_pending, pending))
		{
			return;
		}
		_pending = null;
		TryKillSceneAgent(pending.Mission, pending.Agent);
		NoblePrisonerEscortBehavior.RemoveHeroFromAllProfiles(pending.Hero, "execution_accepted");
		if (pending.EscalateMeetingAfterCommit && MeetingBattleRuntime.IsMeetingActive && !MeetingBattleRuntime.IsCombatEscalated)
		{
			MeetingBattleRuntime.RequestCombatEscalation("witnessed_same_faction_prisoner_execution");
			MeetingBattleRuntime.UnlockDiplomaticSideEffects("witnessed_same_faction_prisoner_execution");
			NoblePrisonerEscortLog.Log("Requested immediate meeting combat after witnessed same-faction execution. hero=" + (pending.Hero?.StringId ?? "N/A"));
		}
		NoblePrisonerEscortLog.Log("Committed escorted noble prisoner execution. hero=" + (pending.Hero?.StringId ?? "N/A")
			+ ", deferred=" + deferredByMapEvent + ", escalateMeeting=" + pending.EscalateMeetingAfterCommit);
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
			NoblePrisonerEscortBehavior.UnregisterEscortedAgent(agent, "executed");
			return !agent.IsActive() || agent.State == AgentState.Killed;
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Kill scene execution agent failed. agent=" + agent.Index + ", error=" + ex.Message);
		}
		try
		{
			mission.KillAgentCheat(agent);
			NoblePrisonerEscortBehavior.UnregisterEscortedAgent(agent, "executed_fallback");
			return !agent.IsActive() || agent.State == AgentState.Killed;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryValidate(Mission mission, Hero hero, Agent agent, out string reason)
	{
		reason = string.Empty;
		if (mission == null || mission.IsMissionEnding || !ReferenceEquals(mission, Mission.Current))
		{
			reason = "mission_unavailable";
			return false;
		}
		if (hero == null || hero == Hero.MainHero || !hero.IsAlive || !hero.IsPrisoner
			|| hero.PartyBelongedToAsPrisoner != PartyBase.MainParty)
		{
			reason = "target_not_main_party_prisoner";
			return false;
		}
		if (agent == null || !agent.IsActive() || agent.State == AgentState.Killed
			|| !NoblePrisonerEscortBehavior.IsEscortedAgent(agent))
		{
			reason = "invalid_escorted_scene_agent";
			return false;
		}
		if ((agent.Character as CharacterObject)?.HeroObject != hero)
		{
			reason = "scene_agent_hero_mismatch";
			return false;
		}
		return true;
	}

	private static bool ShouldEscalateMeetingForExecution(Hero prisoner)
	{
		if (prisoner == null || !MeetingBattleRuntime.IsMeetingActive)
		{
			return false;
		}
		PartyBase encountered = null;
		try
		{
			encountered = PlayerEncounter.EncounteredParty;
		}
		catch
		{
		}
		Hero target = MeetingBattleRuntime.TargetHero;
		IFaction prisonerFaction = prisoner.MapFaction ?? prisoner.Clan;
		IFaction encounteredFaction = encountered?.MapFaction ?? target?.MapFaction ?? target?.Clan;
		if (AreSameFaction(prisonerFaction, encounteredFaction))
		{
			return true;
		}
		Clan prisonerClan = prisoner.Clan;
		Clan encounteredClan = encountered?.Owner?.Clan ?? target?.Clan;
		return prisonerClan != null && encounteredClan != null && ReferenceEquals(prisonerClan, encounteredClan);
	}

	private static bool AreSameFaction(IFaction left, IFaction right)
	{
		return left != null
			&& right != null
			&& (ReferenceEquals(left, right)
				|| string.Equals(left.StringId, right.StringId, StringComparison.OrdinalIgnoreCase));
	}

	private static void Rollback(PendingExecution pending, string reason, bool showMessage, bool hideNotification)
	{
		if (pending == null || !ReferenceEquals(_pending, pending))
		{
			return;
		}
		_pending = null;
		if (hideNotification)
		{
			try
			{
				if (MBInformationManager.GetIsAnySceneNotificationActive() == true)
				{
					MBInformationManager.HideSceneNotification();
				}
			}
			catch
			{
			}
		}
		if (showMessage)
		{
			string message = string.Equals(reason, "player_cancelled_execution", StringComparison.Ordinal)
				? "【贵族俘虏随行】已取消处决。"
				: "【贵族俘虏随行】处决未能执行。";
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(0xFFFF6B6Bu)));
		}
		NoblePrisonerEscortLog.Log("Rolled back execution. hero=" + (pending.Hero?.StringId ?? "N/A") + ", reason=" + (reason ?? "N/A"));
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
		catch
		{
		}
	}
}
