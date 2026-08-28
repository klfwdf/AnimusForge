using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using SandBox.Conversation.MissionLogics;
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
	private const float NotificationTimeout = 2.5f;
	private const float ConversationExitTimeout = 90f;
	private const float AttackTimeout = 24f;
	private const float ApproachTimeout = 16f;
	private const string FallbackWeaponId = "iron_spatha_sword_t2";

	private enum Stage
	{
		QueuedConfirmation,
		WaitingForNotification,
		WaitingForConversationExit,
		PerformingAttack,
		ApproachingPlayer,
	}

	private sealed class AgentSnapshot
	{
		internal Team Team;
		internal Formation Formation;
		internal AgentControllerType Controller;
		internal AgentFlag Flags;
		internal Agent.MortalityState Mortality;
		internal float SpeedLimit;
	}

	private sealed class PendingExecution
	{
		internal int Token;
		internal Hero ActorHero;
		internal Agent ActorAgent;
		internal Hero PrisonerHero;
		internal Agent PrisonerAgent;
		internal Mission Mission;
		internal Stage Stage;
		internal bool NotificationSeen;
		internal bool Affirmative;
		internal bool NativeCommitInProgress;
		internal bool Committed;
		internal bool ExecutionEffectsCompleted;
		internal bool DispositionCompleted;
		internal bool EscalateMeeting;
		internal float Elapsed;
		internal float NextRefresh;
		internal NobleExecutionHeadDisposition HeadDisposition;
		internal NobleExecutionRelationAttribution RelationAttribution;
		internal AgentSnapshot ActorSnapshot;
		internal AgentSnapshot PrisonerSnapshot;
		internal MissionMode OriginalMissionMode;
		internal bool MissionModeChanged;
		internal Team ActorTeam;
		internal Team PrisonerTeam;
		internal EquipmentIndex TemporaryWeaponSlot = EquipmentIndex.None;
	}

	private static PendingExecution _pending;
	private static int _nextToken;

	internal static bool ControlsAgent(Agent agent)
	{
		return _pending != null
			&& agent != null
			&& (agent == _pending.ActorAgent || agent == _pending.PrisonerAgent);
	}

	internal static bool TryQueue(Hero prisonerHero, Agent prisonerAgent, out string reason)
	{
		reason = string.Empty;
		Mission mission = Mission.Current;
		if (_pending != null)
		{
			reason = "execution_already_pending";
			return false;
		}
		if (!TryValidatePrisoner(mission, prisonerHero, prisonerAgent, out reason))
		{
			return false;
		}
		Agent actorAgent = Agent.Main ?? mission.MainAgent;
		if (actorAgent == null || !actorAgent.IsActive())
		{
			reason = "player_agent_unavailable";
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
		catch { }
		_pending = CreatePending(
			Hero.MainHero,
			actorAgent,
			prisonerHero,
			prisonerAgent,
			NobleExecutionHeadDisposition.GiveToPlayer,
			NobleExecutionRelationAttribution.Player,
			Stage.QueuedConfirmation);
		return true;
	}

	internal static bool TryQueueActorExecution(
		Hero actorHero,
		Agent actorAgent,
		Hero prisonerHero,
		Agent prisonerAgent,
		NobleExecutionHeadDisposition headDisposition,
		NobleExecutionRelationAttribution relationAttribution,
		out string reason)
	{
		reason = string.Empty;
		if (_pending != null)
		{
			reason = "execution_already_pending";
			return false;
		}
		if (!TryValidatePrisoner(Mission.Current, prisonerHero, prisonerAgent, out reason))
		{
			return false;
		}
		if (actorHero == null || actorHero == prisonerHero || !actorHero.IsAlive || actorHero.IsPrisoner
			|| actorAgent == null || !actorAgent.IsActive()
			|| (actorAgent.Character as CharacterObject)?.HeroObject != actorHero)
		{
			reason = "invalid_execution_actor";
			return false;
		}
		_pending = CreatePending(
			actorHero,
			actorAgent,
			prisonerHero,
			prisonerAgent,
			headDisposition,
			relationAttribution,
			Stage.WaitingForConversationExit);
		NoblePrisonerEscortLog.Log("Queued consented scene execution. actor=" + actorHero.StringId
			+ ", prisoner=" + prisonerHero.StringId + ", head=" + headDisposition);
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
			Abort(pending, "execution_context_ended", false, true);
			return;
		}
		pending.Elapsed += Math.Max(0f, dt);
		switch (pending.Stage)
		{
		case Stage.QueuedConfirmation:
			OpenNotification(pending);
			break;
		case Stage.WaitingForNotification:
			TickNotification(pending);
			break;
		case Stage.WaitingForConversationExit:
			TickConversationExit(pending);
			break;
		case Stage.PerformingAttack:
			TickAttack(pending);
			break;
		case Stage.ApproachingPlayer:
			TickApproach(pending);
			break;
		}
	}

	internal static void OnAgentHit(Agent affectedAgent, Agent affectorAgent)
	{
		PendingExecution pending = _pending;
		if (pending != null
			&& pending.Stage == Stage.PerformingAttack
			&& affectedAgent == pending.PrisonerAgent
			&& affectorAgent == pending.ActorAgent)
		{
			CommitOnFirstHit(pending);
		}
	}

	internal static void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState state)
	{
		PendingExecution pending = _pending;
		if (pending == null || affectedAgent == null)
		{
			return;
		}
		if (affectedAgent == pending.PrisonerAgent
			&& !pending.Committed
			&& !pending.NativeCommitInProgress)
		{
			Abort(pending, "prisoner_removed_before_execution_hit", true, false);
		}
		else if (affectedAgent == pending.ActorAgent && state == AgentState.Killed)
		{
			Abort(pending, "execution_actor_killed", true, false);
		}
	}

	internal static void CancelForMission(Mission mission, string source)
	{
		if (_pending != null && (mission == null || ReferenceEquals(mission, _pending.Mission)))
		{
			Abort(_pending, source ?? "mission_cancelled_execution", false, true);
		}
	}

	internal static void Reset(string source)
	{
		if (_pending != null)
		{
			Abort(_pending, source ?? "reset", false, true);
		}
		_nextToken = 0;
	}

	private static PendingExecution CreatePending(
		Hero actorHero,
		Agent actorAgent,
		Hero prisonerHero,
		Agent prisonerAgent,
		NobleExecutionHeadDisposition headDisposition,
		NobleExecutionRelationAttribution relationAttribution,
		Stage stage)
	{
		return new PendingExecution
		{
			Token = unchecked(++_nextToken),
			ActorHero = actorHero,
			ActorAgent = actorAgent,
			PrisonerHero = prisonerHero,
			PrisonerAgent = prisonerAgent,
			Mission = Mission.Current,
			Stage = stage,
			HeadDisposition = headDisposition,
			RelationAttribution = relationAttribution,
			EscalateMeeting = ShouldEscalateMeeting(prisonerHero),
		};
	}

	private static void OpenNotification(PendingExecution pending)
	{
		if (!TryValidatePrisoner(pending.Mission, pending.PrisonerHero, pending.PrisonerAgent, out string reason))
		{
			Abort(pending, reason, true, false);
			return;
		}
		try
		{
			EndCurrentConversation();
			pending.Stage = Stage.WaitingForNotification;
			pending.Elapsed = 0f;
			HeroExecutionSceneNotificationData notification = HeroExecutionSceneNotificationData.CreateForPlayerExecutingHero(
				pending.PrisonerHero,
				() => MarkAffirmative(pending.Token),
				SceneNotificationData.RelevantContextType.Mission,
				showNegativeOption: true);
			MBInformationManager.ShowSceneNotification(notification);
			pending.NotificationSeen = MBInformationManager.GetIsAnySceneNotificationActive() == true;
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Open execution confirmation failed: " + ex);
			Abort(pending, "open_execution_exception", true, false);
		}
	}

	private static void TickNotification(PendingExecution pending)
	{
		bool active = false;
		try
		{
			active = MBInformationManager.GetIsAnySceneNotificationActive() == true;
			pending.NotificationSeen |= active;
		}
		catch { }
		if (pending.Affirmative && !active)
		{
			pending.Stage = Stage.WaitingForConversationExit;
			pending.Elapsed = 0f;
		}
		else if (pending.NotificationSeen && !active)
		{
			Abort(pending, "player_cancelled_execution", true, false);
		}
		else if (!pending.NotificationSeen && pending.Elapsed >= NotificationTimeout)
		{
			Abort(pending, "execution_notification_not_opened", true, false);
		}
	}

	private static void TickConversationExit(PendingExecution pending)
	{
		if (Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
		{
			if (!TryBeginAttack(pending, out string reason))
			{
				Abort(pending, reason, true, false);
			}
		}
		else if (pending.Elapsed >= ConversationExitTimeout)
		{
			Abort(pending, "conversation_exit_timeout", true, false);
		}
	}

	private static bool TryBeginAttack(PendingExecution pending, out string reason)
	{
		reason = string.Empty;
		if (!TryValidatePrisoner(pending.Mission, pending.PrisonerHero, pending.PrisonerAgent, out reason)
			|| pending.ActorHero == null || !pending.ActorHero.IsAlive || pending.ActorHero.IsPrisoner
			|| pending.ActorAgent == null || !pending.ActorAgent.IsActive())
		{
			reason = string.IsNullOrWhiteSpace(reason) ? "execution_actor_unavailable" : reason;
			return false;
		}
		try
		{
			pending.ActorSnapshot = Capture(pending.ActorAgent);
			pending.PrisonerSnapshot = Capture(pending.PrisonerAgent);
			pending.OriginalMissionMode = pending.Mission.Mode;
			pending.MissionModeChanged = pending.Mission.Mode != MissionMode.Battle;
			if (pending.MissionModeChanged)
			{
				pending.Mission.SetMissionMode(MissionMode.Battle, atStart: false);
			}
			pending.ActorTeam = pending.Mission.Teams.Add(BattleSideEnum.Attacker, 0xFF305080u, 0xFF305080u, null, false, false);
			pending.PrisonerTeam = pending.Mission.Teams.Add(BattleSideEnum.Defender, 0xFF7A2020u, 0xFF7A2020u, null, false, false);
			if (pending.ActorTeam == null || pending.PrisonerTeam == null || pending.ActorTeam == pending.PrisonerTeam)
			{
				reason = "execution_team_setup_failed";
				return false;
			}
			ConfigureTemporaryHostility(pending);
			ReleaseConversationControl(pending);
			PreparePrisoner(pending);
			PrepareActor(pending, true);
			pending.Stage = Stage.PerformingAttack;
			pending.Elapsed = 0f;
			pending.NextRefresh = 0f;
			InformationManager.DisplayMessage(new InformationMessage(
				pending.ActorHero.Name + "走向" + pending.PrisonerHero.Name + "，准备行刑。",
				Color.FromUint(0xFFDFC16Bu)));
			return true;
		}
		catch (Exception ex)
		{
			reason = "execution_combat_setup_exception";
			NoblePrisonerEscortLog.Log("Begin consented execution failed: " + ex);
			return false;
		}
	}

	private static void TickAttack(PendingExecution pending)
	{
		if (!pending.ActorAgent.IsActive() || !pending.PrisonerAgent.IsActive()
			|| !pending.ActorHero.IsAlive || !pending.PrisonerHero.IsAlive)
		{
			Abort(pending, "execution_participant_lost", true, false);
			return;
		}
		if (pending.Elapsed >= AttackTimeout)
		{
			Abort(pending, "execution_attack_timeout", true, false);
			return;
		}
		if (pending.Mission.CurrentTime >= pending.NextRefresh)
		{
			pending.NextRefresh = pending.Mission.CurrentTime + 0.2f;
			PreparePrisoner(pending);
			PrepareActor(pending, false);
		}
	}

	private static void CommitOnFirstHit(PendingExecution pending)
	{
		if (!ReferenceEquals(_pending, pending) || pending.Committed)
		{
			return;
		}
		Hero responsible = pending.RelationAttribution == NobleExecutionRelationAttribution.Player
			? Hero.MainHero
			: pending.ActorHero;
		try
		{
			pending.NativeCommitInProgress = true;
			KillCharacterAction.ApplyByExecution(pending.PrisonerHero, responsible, true, true);
			if (!IsExecutionAccepted(pending.PrisonerHero))
			{
				pending.NativeCommitInProgress = false;
				Abort(pending, "native_execution_not_accepted", true, false);
				return;
			}
			pending.Committed = true;
			pending.NativeCommitInProgress = false;
			KillScenePrisoner(pending);
			CompleteExecutionEffects(pending);
			RestoreCombatState(pending, false);
			if (pending.ActorHero != Hero.MainHero && pending.ActorAgent.IsActive())
			{
				pending.Stage = Stage.ApproachingPlayer;
				pending.Elapsed = 0f;
				BeginApproach(pending);
			}
			else
			{
				CompleteDisposition(pending);
				_pending = null;
			}
		}
		catch (Exception ex)
		{
			pending.NativeCommitInProgress = false;
			NoblePrisonerEscortLog.Log("Commit execution on hit failed accepted=" + IsExecutionAccepted(pending.PrisonerHero) + " error=" + ex);
			if (IsExecutionAccepted(pending.PrisonerHero))
			{
				pending.Committed = true;
				KillScenePrisoner(pending);
				CompleteExecutionEffects(pending);
				RestoreCombatState(pending, false);
				CompleteDisposition(pending);
				_pending = null;
			}
			else
			{
				Abort(pending, "native_execution_exception", true, false);
			}
		}
	}

	private static void CompleteExecutionEffects(PendingExecution pending)
	{
		if (pending == null || pending.ExecutionEffectsCompleted)
		{
			return;
		}
		pending.ExecutionEffectsCompleted = true;
		try
		{
			NoblePrisonerEscortBehavior.RemoveHeroFromAllProfiles(pending.PrisonerHero, "execution_accepted");
			NoblePrisonerEscortBehavior.UnregisterEscortedAgent(pending.PrisonerAgent, "executed");
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Execution profile cleanup failed: " + ex.Message);
		}
		string actorName = pending.ActorHero?.Name?.ToString() ?? "某人";
		string prisonerName = pending.PrisonerHero?.Name?.ToString() ?? "某贵族";
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(actorName + "处决" + prisonerName, Color.FromUint(0xFFFF6B6Bu)));
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Execution notification failed: " + ex.Message);
		}
		try
		{
			if (pending.EscalateMeeting && MeetingBattleRuntime.IsMeetingActive && !MeetingBattleRuntime.IsCombatEscalated)
			{
				MeetingBattleRuntime.RequestCombatEscalation("witnessed_same_faction_prisoner_execution");
				MeetingBattleRuntime.UnlockDiplomaticSideEffects("witnessed_same_faction_prisoner_execution");
			}
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Execution meeting escalation failed: " + ex.Message);
		}
	}

	private static void CompleteDisposition(PendingExecution pending)
	{
		if (pending == null || pending.DispositionCompleted)
		{
			return;
		}
		pending.DispositionCompleted = true;
		string actorName = pending.ActorHero?.Name?.ToString() ?? "某人";
		string prisonerName = pending.PrisonerHero?.Name?.ToString() ?? "某贵族";
		bool giveHead = pending.HeadDisposition == NobleExecutionHeadDisposition.GiveToPlayer;
		if (giveHead)
		{
			try
			{
				string requestedName = NoblePrisonerExecutionPolicy.BuildHeadItemName(prisonerName);
				int generated = RewardSystemBehavior.GenerateNamedInventoryItemToRosterForExternal(
					MobileParty.MainParty?.ItemRoster,
					requestedName,
					1,
					out string generatedId,
					out string itemName,
					"noble_prisoner_execution_head",
					"noble_head_" + (pending.PrisonerHero?.StringId ?? "unknown"));
				if (generated > 0)
				{
					InformationManager.DisplayMessage(new InformationMessage(
						actorName + "将“" + (itemName ?? requestedName) + "”交给了玩家。",
						Color.FromUint(0xFF8DDC7Eu)));
					NoblePrisonerEscortLog.Log("Generated execution head item id=" + (generatedId ?? "N/A"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage(
						"【贵族俘虏随行】处决完成，但首级 RP 道具生成失败。",
						Color.FromUint(0xFFFF6B6Bu)));
				}
			}
			catch (Exception ex)
			{
				NoblePrisonerEscortLog.Log("Execution head disposition failed: " + ex.Message);
			}
		}
		try
		{
			MyBehavior.AppendExternalDialogueHistory(
				pending.ActorHero,
				null,
				null,
				"[AFEF NPC行为补充] 你在玩家面前亲手处决了“" + prisonerName + "”"
				+ (giveHead ? "，随后把死者的头颅交给了玩家。" : "，并决定自己保留死者的头颅。")
				+ "此事已经发生，不能否认。");
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Execution dialogue history failed: " + ex.Message);
		}
	}

	private static void BeginApproach(PendingExecution pending)
	{
		Agent player = Agent.Main ?? pending.Mission.MainAgent;
		if (player == null || !player.IsActive())
		{
			CompleteDisposition(pending);
			_pending = null;
			return;
		}
		pending.ActorAgent.Controller = AgentControllerType.AI;
		pending.ActorAgent.SetIsAIPaused(false);
		pending.ActorAgent.DisableScriptedMovement();
		pending.ActorAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
		pending.ActorAgent.SetTargetPosition(player.Position.AsVec2);
		pending.ActorAgent.SetLookAgent(player);
	}

	private static void TickApproach(PendingExecution pending)
	{
		Agent player = Agent.Main ?? pending.Mission.MainAgent;
		if (player == null || !player.IsActive() || !pending.ActorAgent.IsActive())
		{
			CompleteDisposition(pending);
			_pending = null;
			return;
		}
		if (pending.ActorAgent.Position.Distance(player.Position) > 2.4f && pending.Elapsed < ApproachTimeout)
		{
			pending.ActorAgent.SetTargetPosition(player.Position.AsVec2);
			pending.ActorAgent.SetLookAgent(player);
			return;
		}
		pending.ActorAgent.DisableScriptedMovement();
		pending.ActorAgent.ClearTargetFrame();
		pending.ActorAgent.SetLookAgent(null);
		pending.ActorAgent.Controller = pending.ActorSnapshot?.Controller ?? AgentControllerType.AI;
		CompleteDisposition(pending);
		_pending = null;
		TryStartConversation(pending.Mission, pending.ActorAgent);
	}

	private static void PrepareActor(PendingExecution pending, bool forceWeapon)
	{
		Agent actor = pending.ActorAgent;
		if (actor.Team != pending.ActorTeam)
		{
			actor.SetTeam(pending.ActorTeam, true);
		}
		actor.Formation = null;
		actor.Controller = AgentControllerType.AI;
		actor.SetMortalityState(Agent.MortalityState.Invulnerable);
		actor.SetIsAIPaused(false);
		actor.DisableScriptedMovement();
		actor.SetMaximumSpeedLimit(-1f, false);
		actor.SetAgentFlags(actor.GetAgentFlags() | AgentFlag.CanAttack | AgentFlag.CanDefend | AgentFlag.IsHumanoid | AgentFlag.CanGetAlarmed | AgentFlag.CanWieldWeapon);
		actor.SetWatchState(Agent.WatchState.Alarmed);
		if (forceWeapon)
		{
			EnsureMeleeWeapon(pending);
		}
		EquipmentIndex slot = FindMeleeWeaponSlot(actor);
		if (slot != EquipmentIndex.None)
		{
			actor.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.WithAnimation, false);
		}
		actor.SetLookAgent(pending.PrisonerAgent);
		actor.ResetEnemyCaches();
		actor.InvalidateTargetAgent();
		actor.InvalidateAIWeaponSelections();
		BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(actor, false);
		BannerlordApiCompat.TrySetAgentCombatTarget(actor, pending.PrisonerAgent);
		actor.ForceAiBehaviorSelection();
	}

	private static void PreparePrisoner(PendingExecution pending)
	{
		Agent prisoner = pending.PrisonerAgent;
		if (prisoner.Team != pending.PrisonerTeam)
		{
			prisoner.SetTeam(pending.PrisonerTeam, true);
		}
		prisoner.Formation = null;
		prisoner.Controller = AgentControllerType.AI;
		prisoner.SetMortalityState(Agent.MortalityState.Invulnerable);
		prisoner.SetIsAIPaused(true);
		prisoner.DisableScriptedMovement();
		prisoner.SetMaximumSpeedLimit(0f, false);
		prisoner.SetAgentFlags((prisoner.GetAgentFlags() | AgentFlag.IsHumanoid) & ~AgentFlag.CanAttack);
		prisoner.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		prisoner.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
	}

	private static void EnsureMeleeWeapon(PendingExecution pending)
	{
		if (FindMeleeWeaponSlot(pending.ActorAgent) != EquipmentIndex.None)
		{
			return;
		}
		EquipmentIndex slot = FindEmptyWeaponSlot(pending.ActorAgent);
		ItemObject item = Game.Current?.ObjectManager?.GetObject<ItemObject>(FallbackWeaponId);
		if (slot == EquipmentIndex.None || item == null)
		{
			return;
		}
		MissionWeapon weapon = new MissionWeapon(item, null, pending.ActorAgent.Origin?.Banner);
		pending.ActorAgent.EquipWeaponWithNewEntity(slot, ref weapon);
		pending.TemporaryWeaponSlot = slot;
	}

	private static EquipmentIndex FindMeleeWeaponSlot(Agent actor)
	{
		for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
		{
			MissionWeapon weapon = actor.Equipment[slot];
			if (!weapon.IsEmpty && weapon.Item?.Weapons?.Any(usage => usage != null && usage.IsMeleeWeapon && !usage.IsShield && !usage.IsAmmo) == true)
			{
				return slot;
			}
		}
		return EquipmentIndex.None;
	}

	private static EquipmentIndex FindEmptyWeaponSlot(Agent actor)
	{
		for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
		{
			if (actor.Equipment[slot].IsEmpty)
			{
				return slot;
			}
		}
		return EquipmentIndex.None;
	}

	private static void KillScenePrisoner(PendingExecution pending)
	{
		try
		{
			if (!pending.PrisonerAgent.IsActive() || pending.PrisonerAgent.State == AgentState.Killed)
			{
				return;
			}
			pending.PrisonerAgent.SetMortalityState(Agent.MortalityState.Mortal);
			Blow blow = new Blow
			{
				DamageCalculated = true,
				BaseMagnitude = 2000f,
				InflictedDamage = 2000,
				DamagedPercentage = 1f,
				OwnerId = pending.ActorAgent?.Index ?? -1,
			};
			pending.PrisonerAgent.Die(blow, Agent.KillInfo.Invalid);
		}
		catch
		{
			try { pending.Mission.KillAgentCheat(pending.PrisonerAgent); } catch { }
		}
	}

	private static AgentSnapshot Capture(Agent agent)
	{
		return new AgentSnapshot
		{
			Team = agent.Team,
			Formation = agent.Formation,
			Controller = agent.Controller,
			Flags = agent.GetAgentFlags(),
			Mortality = agent.CurrentMortalityState,
			SpeedLimit = agent.GetMaximumSpeedLimit(),
		};
	}

	private static void ConfigureTemporaryHostility(PendingExecution pending)
	{
		foreach (Team team in pending.Mission.Teams)
		{
			if (team == null || team == pending.ActorTeam || team == pending.PrisonerTeam)
			{
				continue;
			}
			team.SetIsEnemyOf(pending.ActorTeam, false);
			pending.ActorTeam.SetIsEnemyOf(team, false);
			team.SetIsEnemyOf(pending.PrisonerTeam, false);
			pending.PrisonerTeam.SetIsEnemyOf(team, false);
		}
		pending.ActorTeam.SetIsEnemyOf(pending.PrisonerTeam, true);
		pending.PrisonerTeam.SetIsEnemyOf(pending.ActorTeam, true);
	}

	private static void RestoreCombatState(PendingExecution pending, bool restorePrisoner)
	{
		try
		{
			pending.ActorTeam?.SetIsEnemyOf(pending.PrisonerTeam, false);
			pending.PrisonerTeam?.SetIsEnemyOf(pending.ActorTeam, false);
		}
		catch { }
		RestoreAgent(pending.ActorAgent, pending.ActorSnapshot, pending.TemporaryWeaponSlot);
		pending.TemporaryWeaponSlot = EquipmentIndex.None;
		if (restorePrisoner)
		{
			RestoreAgent(pending.PrisonerAgent, pending.PrisonerSnapshot, EquipmentIndex.None);
		}
		try
		{
			if (pending.MissionModeChanged && pending.Mission != null && !pending.Mission.IsMissionEnding)
			{
				pending.Mission.SetMissionMode(pending.OriginalMissionMode, false);
			}
		}
		catch { }
		pending.MissionModeChanged = false;
	}

	private static void RestoreAgent(Agent agent, AgentSnapshot snapshot, EquipmentIndex temporaryWeaponSlot)
	{
		if (agent == null || snapshot == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(agent, true);
			BannerlordApiCompat.TrySetAgentCombatTarget(agent, null);
			agent.SetLookAgent(null);
			agent.InvalidateTargetAgent();
			if (temporaryWeaponSlot != EquipmentIndex.None)
			{
				agent.RemoveEquippedWeapon(temporaryWeaponSlot);
			}
			if (snapshot.Team != null && agent.Team != snapshot.Team)
			{
				agent.SetTeam(snapshot.Team, true);
			}
			agent.Formation = snapshot.Formation;
			if (snapshot.Formation != null)
			{
				agent.TryAttachToFormation();
			}
			agent.SetAgentFlags(snapshot.Flags);
			agent.SetMortalityState(snapshot.Mortality);
			agent.SetMaximumSpeedLimit(snapshot.SpeedLimit, false);
			agent.Controller = snapshot.Controller;
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Restore execution agent failed index=" + agent.Index + " error=" + ex.Message);
		}
	}

	private static void ReleaseConversationControl(PendingExecution pending)
	{
		int[] indexes = { pending.ActorAgent.Index, pending.PrisonerAgent.Index };
		ShoutBehavior.ReleaseSceneConversationAgentsForCombatExternal(indexes, "noble_prisoner_execution");
		foreach (int index in indexes)
		{
			ShoutBehavior.TryForceStopSceneFollowForExternal(index, "noble_prisoner_execution");
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(index, "noble_prisoner_execution");
		}
	}

	private static bool TryValidatePrisoner(Mission mission, Hero hero, Agent agent, out string reason)
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

	private static bool IsExecutionAccepted(Hero hero)
	{
		return hero != null && (!hero.IsAlive
			|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Executed
			|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent);
	}

	private static bool ShouldEscalateMeeting(Hero prisoner)
	{
		if (prisoner == null || !MeetingBattleRuntime.IsMeetingActive)
		{
			return false;
		}
		PartyBase encountered = null;
		try { encountered = PlayerEncounter.EncounteredParty; } catch { }
		Hero target = MeetingBattleRuntime.TargetHero;
		IFaction prisonerFaction = prisoner.MapFaction ?? prisoner.Clan;
		IFaction encounteredFaction = encountered?.MapFaction ?? target?.MapFaction ?? target?.Clan;
		if (prisonerFaction != null && encounteredFaction != null
			&& (ReferenceEquals(prisonerFaction, encounteredFaction)
				|| string.Equals(prisonerFaction.StringId, encounteredFaction.StringId, StringComparison.OrdinalIgnoreCase)))
		{
			return true;
		}
		return prisoner.Clan != null && (encountered?.Owner?.Clan ?? target?.Clan) == prisoner.Clan;
	}

	private static void MarkAffirmative(int token)
	{
		if (_pending != null && _pending.Token == token)
		{
			_pending.Affirmative = true;
		}
	}

	private static void Abort(PendingExecution pending, string reason, bool showMessage, bool hideNotification)
	{
		if (pending == null || !ReferenceEquals(_pending, pending))
		{
			return;
		}
		if (hideNotification)
		{
			try
			{
				if (MBInformationManager.GetIsAnySceneNotificationActive() == true)
				{
					MBInformationManager.HideSceneNotification();
				}
			}
			catch { }
		}
		if (pending.ActorSnapshot != null || pending.PrisonerSnapshot != null || pending.MissionModeChanged)
		{
			RestoreCombatState(pending, !pending.Committed);
		}
		if (pending.Committed)
		{
			CompleteDisposition(pending);
		}
		_pending = null;
		if (showMessage)
		{
			string message = reason == "player_cancelled_execution"
				? "【贵族俘虏随行】已取消处决。"
				: "【贵族俘虏随行】处决行动中止，未写入处决结果。";
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(0xFFFF6B6Bu)));
		}
		NoblePrisonerEscortLog.Log("Aborted scene execution actor=" + (pending.ActorHero?.StringId ?? "N/A")
			+ ", prisoner=" + (pending.PrisonerHero?.StringId ?? "N/A") + ", reason=" + (reason ?? "N/A"));
	}

	private static void TryStartConversation(Mission mission, Agent actorAgent)
	{
		try
		{
			if (mission != null && !mission.IsMissionEnding && actorAgent != null && actorAgent.IsActive())
			{
				mission.GetMissionBehavior<MissionConversationLogic>()
					?.StartConversation(actorAgent, false, false);
			}
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Start post-execution conversation failed: " + ex.Message);
		}
	}

	private static void EndCurrentConversation()
	{
		try { AnimusForgeNativeConversationOverlay.CloseActive(); } catch { }
		try { ShoutBehavior.CloseNativeConversationInputForExternal(); } catch { }
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				Campaign.Current.ConversationManager.EndConversation();
			}
		}
		catch { }
	}
}
