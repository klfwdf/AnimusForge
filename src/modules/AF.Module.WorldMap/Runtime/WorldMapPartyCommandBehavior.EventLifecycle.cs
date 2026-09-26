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
	private void OnHourlyTickParty(MobileParty party)
	{
		try
		{
			Hero hero = party?.LeaderHero;
			PartyCommandQueueState state = null;
			lock (_queueLock)
			{
				if (hero != null && !string.IsNullOrWhiteSpace(hero.StringId))
				{
					_queues.TryGetValue(hero.StringId, out state);
				}
				if (state == null)
				{
					string partyActorKey = BuildPartyActorKey(party, createGuid: false);
					if (!string.IsNullOrWhiteSpace(partyActorKey))
					{
						_queues.TryGetValue(partyActorKey, out state);
					}
				}
			}
			if (state != null)
			{
				if (string.IsNullOrWhiteSpace(state.HeroId))
				{
					hero = null;
				}
				ProcessQueueTick(hero, party, state);
				return;
			}
			if (Volatile.Read(ref _hasGovernorExpeditions) == 0
				|| !TryGetGovernorExpeditionForParty(party, out GovernorExpeditionRecord expeditionRecord))
			{
				return;
			}
			hero = ResolveHeroByIdAny(expeditionRecord.HeroId);
			if (string.Equals(expeditionRecord.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(expeditionRecord.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase))
			{
				ProcessGovernorExpeditionReturnTick(hero, party, expeditionRecord);
				return;
			}
			BeginGovernorExpeditionReturn(hero, party, "active_without_queue");
		}
		catch (Exception ex)
		{
			Log("hourly tick failed: " + ex);
		}
	}

	private void OnCampaignTick(float dt)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnCampaignTick"))
		{
		try
		{
			ProcessPendingGovernorExpeditionReconcile();
			ProcessPendingFollowSiegeRefresh();
			ProcessPendingCreateCompanionPartyRequests();
			ProcessPendingGovernorExpeditionRequests();
			double nowDay = NowDay();
			if (nowDay >= _nextDetachedPartyPruneDay)
			{
				_nextDetachedPartyPruneDay = nowDay + 1.0 / 24.0;
				PruneInvalidPlayerDetachedParties();
			}
			if (Volatile.Read(ref _hasForeignClanGuests) != 0 && nowDay >= _nextForeignClanGuestReconcileDay)
			{
				_nextForeignClanGuestReconcileDay = nowDay + 1.0 / 24.0;
				ReconcileForeignClanGuests();
			}
		}
		catch (Exception ex)
		{
			Log("world map command campaign tick failed: " + ex);
		}
		}
	}

	private void ReconcileForeignClanGuests()
	{
		List<ForeignClanGuestRecord> snapshot;
		lock (_queueLock)
		{
			snapshot = _foreignClanGuests.Values.Where(record => record != null).ToList();
		}
		foreach (ForeignClanGuestRecord record in snapshot)
		{
			Hero guest = ResolveHeroByIdAny(record.HeroId);
			if (guest == null || guest.IsDead || !guest.IsActive || guest.Clan == null
				|| !IsForeignClanGuestHero(guest) || !IsForeignClanGuestPlacementValid(guest))
			{
				RemoveForeignClanGuestRecord(record.HeroId, "guest_no_longer_in_player_party_or_foreign_clan");
				continue;
			}
			string currentClanId = guest.Clan.StringId ?? "";
			if (!string.Equals(currentClanId, record.ClanId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				lock (_queueLock)
				{
					if (_foreignClanGuests.TryGetValue(record.HeroId, out ForeignClanGuestRecord current) && current != null)
					{
						current.ClanId = currentClanId;
					}
				}
				Log("foreign clan guest clan updated hero=" + record.HeroId + " clan=" + currentClanId);
			}
			if (!ArePlayerAndClanAtWar(guest.Clan))
			{
				continue;
			}
			MobileParty guestParty = guest.PartyBelongedTo;
			bool leftPlayerControl = IsHeroActuallyInPlayerMainPartyRoster(guest)
				? TryEjectForeignClanGuestAfterWar(guest)
				: TryReleaseForeignClanGuestDetachmentAfterWar(guest, guestParty);
			if (leftPlayerControl)
			{
				RemoveForeignClanGuestRecord(record.HeroId, "war_started");
			}
		}
	}

	private static bool TryEjectForeignClanGuestAfterWar(Hero guest)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (guest == null || mainParty == null || guest.PartyBelongedTo != mainParty
				|| HasActiveMapEvent(mainParty) || mainParty.DefaultBehavior == AiBehavior.EngageParty
				|| PlayerEncounter.Current != null || Mission.Current != null)
			{
				return false;
			}
			Settlement destination = ResolveForeignClanGuestReturnSettlement(guest);
			if (destination != null)
			{
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(guest, destination);
			}
			else
			{
				MakeHeroFugitiveAction.Apply(guest, showNotification: false);
			}
			if (guest.PartyBelongedTo == mainParty || IsHeroActuallyInPlayerMainPartyRoster(guest))
			{
				return false;
			}
			string destinationText = destination == null ? "原家族势力范围" : GetSettlementName(destination);
			LogFact(guest, GetHeroName(guest) + "所属势力已与玩家开战，因此结束外族客军随队状态并离开玩家主队，返回" + destinationText + "；此前已并入玩家主队的普通兵员和物资不自动撤回。");
			DisplayCommandMessage(GetHeroName(guest) + "所属势力已与玩家开战，该外族客军 Hero 已离开玩家主队。", CommandMessageTone.Failure);
			return true;
		}
		catch (Exception ex)
		{
			Log("eject foreign clan guest failed hero=" + (guest?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private bool TryReleaseForeignClanGuestDetachmentAfterWar(Hero guest, MobileParty party)
	{
		try
		{
			if (guest == null || !IsRegisteredPlayerDetachment(guest, party)
				|| HasActiveMapEvent(party) || party.DefaultBehavior == AiBehavior.EngageParty || party.BesiegerCamp != null
				|| PlayerEncounter.Current != null || Mission.Current != null)
			{
				return false;
			}
			StopQueue(guest, "foreign_guest_war_started", out _);
			RemovePlayerDetachedParty(guest, party, "foreign_guest_war_started");
			ReleasePartyAi(party);
			if (IsRegisteredPlayerDetachment(guest, party))
			{
				return false;
			}
			LogFact(guest, GetHeroName(guest) + "所属势力已与玩家开战，因此其外族客军分队已脱离玩家指挥并回归原家族的原版AI；该分队现有兵员和物资不自动转回玩家主队。");
			DisplayCommandMessage(GetHeroName(guest) + "所属势力已与玩家开战，其外族客军分队已脱离玩家指挥。", CommandMessageTone.Failure);
			return true;
		}
		catch (Exception ex)
		{
			Log("release foreign clan guest detachment failed hero=" + (guest?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static Settlement ResolveForeignClanGuestReturnSettlement(Hero guest)
	{
		try
		{
			Clan clan = guest?.Clan;
			Settlement clanSettlement = clan?.Settlements?.FirstOrDefault(settlement => settlement != null);
			if (clanSettlement != null)
			{
				return clanSettlement;
			}
			Settlement home = guest?.HomeSettlement;
			if (home != null)
			{
				return home;
			}
			return guest?.BornSettlement;
		}
		catch
		{
			return null;
		}
	}

	private void RemoveForeignClanGuestRecord(string heroId, string reason)
	{
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return;
		}
		bool removed;
		lock (_queueLock)
		{
			removed = _foreignClanGuests.Remove(heroId);
			Volatile.Write(ref _hasForeignClanGuests, _foreignClanGuests.Count > 0 ? 1 : 0);
		}
		if (removed)
		{
			Log("removed foreign clan guest hero=" + heroId + " reason=" + (reason ?? ""));
		}
	}

	private void OnFollowSiegeEventChanged(SiegeEvent siegeEvent)
	{
		RequestFollowSiegeRefresh();
	}

	private void OnFollowSiegePartyChanged(MobileParty party)
	{
		RequestFollowSiegeRefresh();
	}

	private void RequestFollowSiegeRefresh()
	{
		Volatile.Write(ref _hasPendingFollowSiegeRefresh, 1);
	}

	private void ProcessPendingFollowSiegeRefresh()
	{
		if (Interlocked.Exchange(ref _hasPendingFollowSiegeRefresh, 0) == 0)
		{
			return;
		}
		List<PartyCommandQueueState> snapshot;
		lock (_queueLock)
		{
			snapshot = _queues.Values
				.Where(x => x != null && (IsCurrentFollowCommand(x) || HasPendingSafeExit(x)))
				.ToList();
		}
		foreach (PartyCommandQueueState state in snapshot)
		{
			try
			{
				if (!IsStateStillQueued(state))
				{
					continue;
				}
				Hero hero = ResolveHeroByIdAny(state.HeroId);
				MobileParty party = HasPendingSafeExit(state) ? ResolvePartyForSafeExit(state, hero) : ResolveActorParty(state, hero);
				ProcessQueueTick(string.IsNullOrWhiteSpace(state.HeroId) ? null : hero, party, state);
			}
			catch (Exception ex)
			{
				Log("follow siege refresh failed actor=" + GetActorLogId(state, null, null) + " error=" + ex.Message);
			}
		}
	}

	private bool IsStateStillQueued(PartyCommandQueueState state)
	{
		string queueKey = GetQueueKey(state);
		if (string.IsNullOrWhiteSpace(queueKey))
		{
			return false;
		}
		lock (_queueLock)
		{
			return _queues.TryGetValue(queueKey, out PartyCommandQueueState current) && ReferenceEquals(current, state);
		}
	}

	private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnMobilePartyDestroyed"))
		{
		PerfProbe.MarkEvent("WorldMapCommand.MobilePartyDestroyed");
		try
		{
			string heroId = destroyedParty?.LeaderHero?.StringId;
			bool trackedGovernorExpedition = TryGetGovernorExpeditionForParty(destroyedParty, out GovernorExpeditionRecord destroyedExpedition);
			if (trackedGovernorExpedition)
			{
				heroId = string.IsNullOrWhiteSpace(heroId) ? destroyedExpedition.HeroId : heroId;
			}
			string partyId = destroyedParty?.StringId;
			string destroyedActorKey = BuildPartyActorKey(destroyedParty, createGuid: false);
			PartyCommandQueueState actorState = null;
			lock (_queueLock)
			{
				if (!string.IsNullOrWhiteSpace(heroId))
				{
					_queues.TryGetValue(heroId, out actorState);
				}
				if (actorState == null && !string.IsNullOrWhiteSpace(destroyedActorKey))
				{
					_queues.TryGetValue(destroyedActorKey, out actorState);
				}
			}
			List<PartyCommandQueueState> activeAttackStates = GetActiveAttackStatesSnapshot();
			if (trackedGovernorExpedition)
			{
				RemoveGovernorExpeditionRecord(destroyedExpedition.HeroId, "party_destroyed", removeQueue: true);
			}
			RemovePlayerDetachedParty(destroyedParty?.LeaderHero, destroyedParty, "party_destroyed");
			if (actorState == null && activeAttackStates.Count == 0)
			{
				return;
			}
			bool handled = actorState != null;
			string actorStateKey = GetQueueKey(actorState);
			if (actorState != null)
			{
				if (actorState != null && IsCurrentAttackCommand(actorState))
				{
					LogTerminalAttackFailure(actorState, "执行者部队已被消灭。", "actor_party_destroyed");
				}
				else
				{
					lock (_queueLock)
					{
						_queues.Remove(actorStateKey);
					}
				}
			}
			foreach (PartyCommandQueueState state in activeAttackStates)
			{
				if (state == null || string.Equals(GetQueueKey(state), actorStateKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null)
				{
					continue;
				}
				if (IsKind(command, CommandKind.AttackHero) && !IsSettlementTarget(command) && !string.IsNullOrWhiteSpace(heroId) && string.Equals(command.TargetId, heroId, StringComparison.OrdinalIgnoreCase))
				{
					bool actorDestroyedTarget = PartyBaseMatchesActor(destroyerParty, state);
					TryCompleteCurrentAttackResult(state, actorDestroyedTarget ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, actorDestroyedTarget ? "目标部队已被击溃。" : "目标部队已经被消灭或解散。", actorDestroyedTarget ? "target_party_destroyed_by_actor" : "target_party_destroyed");
					continue;
				}
				if (IsKind(command, CommandKind.AttackParty) && !string.IsNullOrWhiteSpace(partyId) && string.Equals(command.TargetId, partyId, StringComparison.OrdinalIgnoreCase))
				{
					bool actorDestroyedTarget = PartyBaseMatchesActor(destroyerParty, state) || PartyBaseMatchesFaction(destroyerParty, state.ResultActorFactionId);
					TryCompleteCurrentAttackResult(state, actorDestroyedTarget ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, actorDestroyedTarget ? "目标部队已被击溃。" : "目标部队已经被消灭或解散。", actorDestroyedTarget ? "target_mobile_party_destroyed_by_actor" : "target_mobile_party_destroyed");
				}
			}
			if (handled)
			{
				Log("mobile party destroyed hero=" + (heroId ?? "") + " party=" + (partyId ?? ""));
			}
		}
		catch (Exception ex)
		{
			Log("party destroyed handling failed: " + ex.Message);
		}
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnMapEventEnded"))
		{
		PerfProbe.MarkEvent("WorldMapCommand.MapEventEnded");
		RequestFollowSiegeRefresh();
		try
		{
			if (mapEvent == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot())
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || IsSettlementTarget(command) || (!IsKind(command, CommandKind.AttackHero) && !IsKind(command, CommandKind.AttackParty)))
				{
					continue;
				}
				BattleSideEnum actorSide = GetActorSideInMapEvent(mapEvent, state);
				BattleSideEnum targetSide = IsKind(command, CommandKind.AttackParty) ? GetPartySideInMapEvent(mapEvent, command.TargetId) : GetHeroSideInMapEvent(mapEvent, command.TargetId);
				if (actorSide == BattleSideEnum.None || targetSide == BattleSideEnum.None || actorSide == targetSide)
				{
					continue;
				}
				if (!mapEvent.HasWinner)
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "战斗已经结束，但原版事件没有明确胜负。", "map_event_no_winner");
					continue;
				}
				bool won = mapEvent.WinningSide == actorSide;
				string detail = won ? (GetStoredTargetName(state, command) + "的部队被击败。") : (GetStoredActorName(state) + "的部队被击退。");
				detail += BuildMapEventCasualtySummary(mapEvent, actorSide, targetSide);
				TryCompleteCurrentAttackResult(state, won ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, won ? "map_event_attack_success" : "map_event_attack_failure");
			}
		}
		catch (Exception ex)
		{
			Log("map event result handling failed: " + ex.Message);
		}
		}
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnHeroPrisonerTaken"))
		{
		PerfProbe.MarkEvent("WorldMapCommand.HeroPrisonerTaken");
		try
		{
			string prisonerId = prisoner?.StringId;
			if (string.IsNullOrWhiteSpace(prisonerId))
			{
				return;
			}
			List<PartyCommandQueueState> activeAttackStates = GetActiveAttackStatesSnapshot();
			if (TryGetGovernorExpeditionForHero(prisonerId, out GovernorExpeditionRecord expeditionRecord))
			{
				MobileParty expeditionParty = ResolveGovernorExpeditionParty(expeditionRecord);
				RemoveGovernorExpeditionRecord(prisonerId, "hero_prisoner_taken", removeQueue: true);
				ReleasePartyAi(expeditionParty);
				Log("governor expedition cleared after capture hero=" + prisonerId);
			}
			foreach (PartyCommandQueueState state in activeAttackStates)
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsKind(command, CommandKind.AttackHero) || IsSettlementTarget(command))
				{
					continue;
				}
				if (!string.IsNullOrWhiteSpace(state.HeroId) && string.Equals(prisonerId, state.HeroId, StringComparison.OrdinalIgnoreCase))
				{
					LogTerminalAttackFailure(state, "执行者已经被俘，攻击命令失败。", "actor_prisoner_taken");
					continue;
				}
				if (!string.Equals(prisonerId, command.TargetId, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (PartyBaseMatchesActor(capturer, state) || PartyBaseMatchesFaction(capturer, state.ResultActorFactionId))
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, GetStoredTargetName(state, command) + "已被俘，目标部队被击败。", "target_prisoner_taken");
				}
			}
		}
		catch (Exception ex)
		{
			Log("prisoner result handling failed: " + ex.Message);
		}
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			string victimId = victim?.StringId;
			if (string.IsNullOrWhiteSpace(victimId)
				|| !TryGetGovernorExpeditionForHero(victimId, out GovernorExpeditionRecord expeditionRecord))
			{
				return;
			}
			PartyCommandQueueState victimState = null;
			lock (_queueLock)
			{
				_queues.TryGetValue(victimId, out victimState);
			}
			MobileParty expeditionParty = ResolveGovernorExpeditionParty(expeditionRecord);
			RemoveGovernorExpeditionRecord(victimId, "hero_killed", removeQueue: true);
			ReleasePartyAi(expeditionParty);
			if (IsCurrentAttackCommand(victimState))
			{
				LogTerminalAttackFailure(victimState, "执行者已经死亡，攻击命令失败。", "actor_killed");
			}
			Log("governor expedition cleared after death hero=" + victimId);
		}
		catch (Exception ex)
		{
			Log("governor expedition death handling failed: " + ex.Message);
		}
	}

	private void OnSiegeCompleted(Settlement settlement, MobileParty party, bool siegeSuccess, MapEvent.BattleTypes battleType)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("siege"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				if (!PartyMatchesActor(party, state) && !PartyMatchesFaction(party, state.ResultActorFactionId))
				{
					continue;
				}
				string detail = siegeSuccess ? (GetSettlementName(settlement) + "已经被攻下。") : "攻城方未能攻下目标。";
				TryCompleteCurrentAttackResult(state, siegeSuccess ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, siegeSuccess ? "siege_completed_success" : "siege_completed_failure");
			}
		}
		catch (Exception ex)
		{
			Log("siege result handling failed: " + ex.Message);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("siege"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				bool actorFactionCaptured = string.Equals(SafeFactionId(newOwner?.MapFaction), state.ResultActorFactionId, StringComparison.OrdinalIgnoreCase) || string.Equals(SafeFactionId(settlement.MapFaction), state.ResultActorFactionId, StringComparison.OrdinalIgnoreCase);
				bool actorCaptured = !string.IsNullOrWhiteSpace(state.HeroId) && string.Equals(capturerHero?.StringId, state.HeroId, StringComparison.OrdinalIgnoreCase);
				if (actorFactionCaptured || actorCaptured)
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, GetSettlementName(settlement) + "已经被攻下并易主。", "settlement_owner_changed_success");
				}
			}
		}
		catch (Exception ex)
		{
			Log("settlement owner result handling failed: " + ex.Message);
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		try
		{
			Settlement settlement = raidEvent?.MapEventSettlement;
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("raid"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				if (!MapEventSideHasActor(raidEvent.AttackerSide, state))
				{
					continue;
				}
				bool success = winnerSide == BattleSideEnum.Attacker || IsVillageLooted(settlement);
				if (!success && TryKeepRaidCommandAliveAfterRaidEnded(state, command, settlement, raidEvent, "raid_completed_before_loot"))
				{
					continue;
				}
				string detail = success ? "村庄已被洗劫。" : "守军击退了袭掠，村庄没有被洗劫。";
				TryCompleteCurrentAttackResult(state, success ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, success ? "raid_completed_success" : "raid_completed_failure");
			}
		}
		catch (Exception ex)
		{
			Log("raid result handling failed: " + ex.Message);
		}
	}

	private void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
	{
		try
		{
			if (village?.Settlement == null || newState != Village.VillageStates.Looted)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("raid"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, village.Settlement))
				{
					continue;
				}
				if (!PartyMatchesActor(raiderParty, state) && !PartyMatchesFaction(raiderParty, state.ResultActorFactionId))
				{
					continue;
				}
				TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, "村庄已进入被洗劫状态。", "village_looted_success");
			}
		}
		catch (Exception ex)
		{
			Log("village state result handling failed: " + ex.Message);
		}
	}
}
