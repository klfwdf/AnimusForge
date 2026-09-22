using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior : CampaignBehaviorBase
{
	private void ProcessQueueTick(Hero hero, MobileParty party, PartyCommandQueueState state)
	{
		NormalizeState(state);
		if (ProcessPendingSafeExit(hero, party, state))
		{
			return;
		}
		if (!ValidateActor(state, hero, party, out string reason))
		{
			if (IsCurrentAttackCommand(state))
			{
				TryCompleteCurrentAttackResult(state, CommandResultOutcome.Failure, "执行者已经失去可控制部队或被俘，命令失败。", "actor_invalid:" + reason);
				return;
			}
			FinishQueue(hero, party, state, "actor_invalid:" + reason, appendFact: true);
			return;
		}
		if (state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, "queue_done", appendFact: true);
			return;
		}
		PartyCommandEntry command = state.Commands[state.CurrentIndex];
		if (command == null || !IsExecutableCommand(command))
		{
			AdvanceCommand(hero, party, state, "invalid_command");
			return;
		}
		CommandStage stage = ParseStage(state.Stage);
		if (stage == CommandStage.New)
		{
			StartCurrentCommand(hero, party, state);
			return;
		}
		double now = NowDay();
		if (IsFollowCommand(command) && HasFollowDurationElapsed(state, command, now))
		{
			LogFact(state, hero, BuildFollowCompletedFact(state, hero, party, command));
			AdvanceCommand(hero, party, state, IsKind(command, CommandKind.FollowParty) ? "follow_party_done" : "follow_done");
			return;
		}
		if (state.TimeoutDay > 0.0 && now > state.TimeoutDay)
		{
			if (TryKeepCommandAliveAfterTimeout(hero, party, state, command, now))
			{
				Log("timeout deferred hero=" + (hero?.StringId ?? "") + " index=" + state.CurrentIndex + " kind=" + (command?.Kind ?? "") + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			}
			else
			{
				if (IsKind(command, CommandKind.AttackHero) || IsKind(command, CommandKind.AttackParty))
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, BuildAttackTimeoutDetail(command, state), "timeout");
					return;
				}
				LogFact(state, hero, BuildCommandTimeoutFact(state, hero, command));
				AdvanceCommand(hero, party, state, "timeout");
				return;
			}
		}
		switch (WorldMapOrderCoordinator.ClassifyCommand(command.Kind))
		{
			case WorldMapCommandRoute.GoToSettlement:
				TickGoToSettlement(hero, party, state, command);
				return;
			case WorldMapCommandRoute.PatrolSettlement:
				TickPatrolSettlement(hero, party, state, command);
				return;
			case WorldMapCommandRoute.FollowHero:
				TickFollowHero(hero, party, state, command);
				return;
			case WorldMapCommandRoute.FollowParty:
				TickFollowParty(hero, party, state, command);
				return;
			case WorldMapCommandRoute.AttackHero:
				TickAttackHero(hero, party, state, command);
				return;
			case WorldMapCommandRoute.AttackParty:
				TickAttackParty(hero, party, state, command);
				return;
			case WorldMapCommandRoute.MergeToPlayer:
				TickMergeToPlayer(hero, party, state, command);
				return;
			default:
				AdvanceCommand(hero, party, state, "unknown_command_kind");
				return;
		}
	}

	private void StartCurrentCommand(Hero hero, MobileParty party, PartyCommandQueueState state)
	{
		if (state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, "queue_done", appendFact: true);
			return;
		}
		PartyCommandEntry command = state.Commands[state.CurrentIndex];
		if (TryConvertCurrentGoToSettlementCommand(hero, party, state, command, "start"))
		{
			command = state.Commands[state.CurrentIndex];
		}
		ResetResultTracking(state);
		state.ArmySurvivalRenewalKey = "";
		state.ArmySurvivalLastPaidDay = -1.0;
		state.CommandStartDay = NowDay();
		state.ArrivalDay = -1.0;
		state.EngageCommitted = false;
		state.LastIssuedActionKey = "";
		state.LastStatusMessageKey = "";
		state.MergeTransferFailureCount = 0;
		state.MergeRetryAfterDay = -1.0;
		state.TimeoutDay = ComputeTimeoutDay(party, command);
		bool isFollowCommand = IsFollowCommand(command);
		if (!isFollowCommand && !PreemptBlockingWorldActivityForCommand(hero, party, command, state, "start"))
		{
			return;
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			Settlement settlement = ResolveSettlementById(command.TargetId);
			if (settlement == null)
			{
				AdvanceCommand(hero, party, state, "settlement_missing");
				return;
			}
			LockPartyAi(party);
			SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "visit:" + settlement.StringId;
			if (!ShouldSuppressCommandMessages(state))
			{
				DisplayCommandMessage(BuildGoToSettlementStartMessage(state, hero, party, settlement, command), CommandMessageTone.Progress);
			}
			Log("start go actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			Settlement settlement = ResolveSettlementById(command.TargetId);
			if (settlement == null)
			{
				AdvanceCommand(hero, party, state, "settlement_missing");
				return;
			}
			LockPartyAi(party);
			SetPartyAiAction.GetActionForPatrollingAroundSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "patrol:" + settlement.StringId;
			DisplayCommandMessage(GetActorName(state, hero, party) + "开始前往" + GetSettlementName(settlement) + "附近，抵达后巡逻" + Math.Max(1, command.Days) + "天。", CommandMessageTone.Progress);
			Log("start patrol actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			state.Stage = CommandStage.Traveling.ToString();
			TickFollowHero(hero, party, state, command, isStarting: true);
			return;
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			state.Stage = CommandStage.Traveling.ToString();
			TickFollowParty(hero, party, state, command, isStarting: true);
			return;
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			if (IsSettlementTarget(command))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (!IsSupportedAttackSettlement(settlement))
				{
					AdvanceCommand(hero, party, state, "attack_settlement_invalid");
					return;
				}
				state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
				string settlementAttackMode = NormalizeAttackMode(command.Mode);
				if (CanStartSettlementAttackWithVanillaAi(party, settlement, settlementAttackMode))
				{
					SynchronizeArmyObjectiveForCommand(party, command);
					CommitSettlementAttack(hero, party, settlement, state, settlementAttackMode);
					Log("start settlement_attack_vanilla actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " mode=" + settlementAttackMode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
					return;
				}
				LockPartyAi(party);
				SynchronizeArmyObjectiveForCommand(party, command);
				MoveTowardSettlementAttackPoint(party, settlement);
				state.Stage = CommandStage.Tracking.ToString();
				state.LastIssuedActionKey = "track_settlement_attack:" + settlement.StringId;
				DisplayCommandMessage(GetActorName(state, hero, party) + "开始向" + GetSettlementName(settlement) + "机动，准备" + (settlement.IsVillage ? "烧掠" : "围攻") + "，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
				Log("start settlement_attack_track actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
				return;
			}
			MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
			if (targetParty == null)
			{
				Hero targetHero = ResolveHeroById(command.TargetId);
				Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, null);
				if (shelter != null)
				{
					state.Stage = CommandStage.Tracking.ToString();
					state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
					DisplayCommandMessage(GetActorName(state, hero, party) + "开始前往" + GetSettlementName(shelter) + "外侧，等待" + GetHeroName(targetHero) + "离开定居点以执行攻击命令。", CommandMessageTone.Progress);
					MaintainAttackShelterWaiting(hero, party, targetHero, shelter, state, command, "start_target_inside_settlement_without_party");
					Log("start attack_shelter_wait actor=" + GetActorLogId(state, hero, party) + " target=" + command.TargetId + " settlement=" + shelter.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
					return;
				}
				AdvanceCommand(hero, party, state, "attack_target_missing");
				return;
			}
			Settlement targetShelter = ResolveTargetHeroShelterSettlement(ResolveHeroById(command.TargetId), targetParty);
			if (targetShelter != null)
			{
				state.Stage = CommandStage.Tracking.ToString();
				state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
				DisplayCommandMessage(GetActorName(state, hero, party) + "开始前往" + GetSettlementName(targetShelter) + "外侧，等待" + GetHeroName(ResolveHeroById(command.TargetId)) + "离开定居点以执行攻击命令。", CommandMessageTone.Progress);
				MaintainAttackShelterWaiting(hero, party, ResolveHeroById(command.TargetId), targetShelter, state, command, "start_target_inside_settlement");
				Log("start attack_shelter_wait actor=" + GetActorLogId(state, hero, party) + " target=" + command.TargetId + " settlement=" + targetShelter.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
			state.Stage = CommandStage.Tracking.ToString();
			state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
			state.LastIssuedActionKey = "track_attack:" + command.TargetId;
			DisplayCommandMessage(GetActorName(state, hero, party) + "开始追踪" + GetHeroName(ResolveHeroById(command.TargetId)) + "的部队，准备攻击，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
			Log("start attack_track actor=" + GetActorLogId(state, hero, party) + " target=" + command.TargetId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			return;
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
			if (!IsPartyUsable(targetParty) || targetParty == party)
			{
				AdvanceCommand(hero, party, state, "attack_party_target_missing");
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
			state.Stage = CommandStage.Tracking.ToString();
			state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
			state.LastIssuedActionKey = "track_party_attack:" + command.TargetId;
			DisplayCommandMessage(GetActorName(state, hero, party) + "开始追踪" + GetPartyName(targetParty) + "，准备攻击，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
			Log("start party_attack_track actor=" + GetActorLogId(state, hero, party) + " targetParty=" + command.TargetId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			return;
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			if (!CanMergeToPlayer(hero, party, out string mergeReason))
			{
				NotifyCommandStatus(state, "merge_to_player_invalid:" + mergeReason, BuildMergeEligibilityFailureMessage(hero, mergeReason), CommandMessageTone.Failure);
				AdvanceCommand(hero, party, state, "merge_invalid:" + mergeReason, terminalFailure: true);
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			IssueMergeApproachAction(party, MobileParty.MainParty);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "merge_to_player";
			DisplayCommandMessage(IsForeignClanGuestHero(hero)
				? (GetHeroName(hero) + "开始率部前往玩家主队，准备在保留原家族身份的情况下以客军形式整队并入。")
				: (GetHeroName(hero) + "开始返回玩家部队，准备会合并转入兵力。"), CommandMessageTone.Progress);
			Log("start merge actor=" + GetActorLogId(state, hero, party));
			return;
		}
	}

	private void AdvanceCommand(Hero hero, MobileParty party, PartyCommandQueueState state, string reason, bool terminalFailure = false)
	{
		Log("advance actor=" + GetActorLogId(state, hero, party) + " index=" + state.CurrentIndex + " reason=" + reason);
		if (HasFollowSiegeState(state) && !TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: false, "advance:" + reason))
		{
			SetPendingSafeExit(state, PendingSafeExitAdvance, reason);
			RequestFollowSiegeRefresh();
			return;
		}
		ClearPendingSafeExit(state);
		AbortCurrentCommandIfNeeded(party, state);
		ResetResultTracking(state);
		state.CurrentIndex++;
		state.Stage = CommandStage.New.ToString();
		state.ArrivalDay = -1.0;
		state.TimeoutDay = -1.0;
		state.EngageCommitted = false;
		state.LastIssuedActionKey = "";
		state.LastStatusMessageKey = "";
		if (state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, terminalFailure ? reason : "queue_done", appendFact: true);
			return;
		}
		StartCurrentCommand(hero, party, state);
	}

	private void FinishQueue(Hero hero, MobileParty party, PartyCommandQueueState state, string reason, bool appendFact)
	{
		if (HasFollowSiegeState(state) && !TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: false, "finish:" + reason))
		{
			SetPendingSafeExit(state, PendingSafeExitStop, reason);
			RequestFollowSiegeRefresh();
			if (appendFact)
			{
				LogFact(state, hero, GetActorName(state, hero, party) + "的大地图命令队列已经结束；当前原版战斗结算后将安全退出攻城并回归原版行动状态。");
			}
			return;
		}
		if (party != null)
		{
			AbortCurrentCommandIfNeeded(party, state);
			ReleasePartyAi(party);
		}
		string queueKey = GetQueueKey(state);
		if (!string.IsNullOrWhiteSpace(queueKey))
		{
			lock (_queueLock)
			{
				_queues.Remove(queueKey);
			}
		}
		bool governorExpeditionHandled = BeginGovernorExpeditionReturn(hero, party, "queue_finished:" + reason);
		bool returningGovernorExpedition = governorExpeditionHandled
			&& TryGetGovernorExpeditionForHero(hero?.StringId, out GovernorExpeditionRecord finishingRecord)
			&& string.Equals(finishingRecord.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase);
		if (appendFact)
		{
			LogFact(state, hero, GetActorName(state, hero, party) + "的大地图命令队列已经结束（" + GetQueueEndReasonText(reason) + "），"
				+ (returningGovernorExpedition ? "临时总督远征队开始返驻地交还兵员。"
					: governorExpeditionHandled ? "临时总督远征已按当前游戏状态结束或交还原版 AI。" : "回归原版行动状态。"));
		}
		Log("finish actor=" + GetActorLogId(state, hero, party) + " reason=" + reason);
	}
}
