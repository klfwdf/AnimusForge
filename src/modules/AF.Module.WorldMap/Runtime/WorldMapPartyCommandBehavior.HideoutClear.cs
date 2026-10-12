using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior
{
	private sealed class NpcHideoutClearBattle
	{
		internal PartyCommandQueueState State;
		internal int CommandIndex;
		internal bool PlayerParticipated;
		internal bool Finalized;
		internal string Casualties = "";
	}

	// Weak identity survives STOP/queue removal through native finalization without
	// retaining old map events. Saved ownership is restored from the existing queue.
	private ConditionalWeakTable<MapEvent, NpcHideoutClearBattle> _npcHideoutClearBattles = new ConditionalWeakTable<MapEvent, NpcHideoutClearBattle>();

	private static bool MustWaitForNpcHideoutBattle(MobileParty party, PartyCommandQueueState state) =>
		NpcHideoutClearPolicy.MustDeferStop(IsKind(GetCurrentCommand(state), CommandKind.ClearHideout), state?.EngageCommitted == true, party?.MapEvent != null);

	private bool TryDeferNpcHideoutStop(MobileParty party, PartyCommandQueueState state, string reason, out string fact)
	{
		fact = "";
		if (!MustWaitForNpcHideoutBattle(party, state)) return false;
		bool alreadyStopping = IsStopPending(state);
		SetPendingSafeExit(state, PendingSafeExitStop, reason);
		if (!alreadyStopping)
			fact = WorldMapSourceTexts.Get("raw_literals.wm_lit_0001") + GetActorName(state, null, party) + WorldMapSourceTexts.Get("raw_literals.wm_lit_0331");
		return true; // Keep the saved ownership until the battle has safely finalized.
	}

	private void RestoreNpcHideoutClearBattleOwnership(CampaignGameStarter starter)
	{
		PartyCommandQueueState[] states;
		lock (_queueLock) states = _queues.Values.Where(s => s.EngageCommitted && IsKind(GetCurrentCommand(s), CommandKind.ClearHideout)).ToArray();
		foreach (PartyCommandQueueState state in states)
		{
			MobileParty party = ResolveActorParty(state, ResolveHeroByIdAny(state.HeroId));
			if (party?.MapEvent != null) TryGetNpcHideoutClearBattle(party.MapEvent, out _);
		}
	}

	private static bool HideoutHasBandits(Settlement settlement) => settlement?.IsHideout == true
		&& settlement.Parties.Any(p => p != null && (p.IsBandit || p.IsBanditBossParty));

	private static NpcHideoutClearTargetState CaptureHideoutClearTarget(Settlement settlement)
	{
		if (settlement?.IsHideout != true) return default(NpcHideoutClearTargetState);
		return new NpcHideoutClearTargetState
		{
			Exists = true,
			Spotted = settlement.Hideout.IsSpotted,
			HasBandits = HideoutHasBandits(settlement),
			QuestBusy = settlement.IsSettlementBusy((object)Instance ?? typeof(WorldMapPartyCommandBehavior))
				|| Campaign.Current.QuestManager.TrackedObjects.ContainsKey(settlement),
			HasBattle = settlement.Party.MapEvent != null,
			AttackTimeReady = settlement.Hideout.NextPossibleAttackTime.IsPast
		};
	}

	private static bool CanOfferHideoutClearTarget(Settlement settlement) => WorldMapNpcHideoutCompletionPatch.IsReady
		&& settlement?.Hideout?.IsInfested == true && NpcHideoutClearPolicy.CanOffer(CaptureHideoutClearTarget(settlement));

	private static string BuildKnownHideoutClearTargetsPrompt(Hero hero, CharacterObject character, int agentIndex)
	{
		string header = WorldMapPromptTexts.Get("AF_WM_HideoutTargetsHeader");
		string noTargets = WorldMapPromptTexts.Get("AF_WM_NoTasks");
		string idSeparator = WorldMapPromptTexts.Get("AF_WM_HideoutTargetIdSeparator");
		if (!WorldMapNpcHideoutCompletionPatch.IsReady || Campaign.Current == null) return header + noTargets;
		hero = hero ?? character?.HeroObject;
		if (hero == null && !CanUseNonHeroPartyFallbackForExternal(character, agentIndex)) return header + noTargets;
		try
		{
			MobileParty observer = ResolveActorParty(hero) ?? MobileParty.MainParty;
			if (observer == null) return header + noTargets;
			// Only prompt preparation enumerates known hideouts; hourly runtime resolves
			// its one target by ID and never scans every hideout or every queue.
			var targets = Hideout.All.Where(h => h != null && h.IsSpotted && CanOfferHideoutClearTarget(h.Settlement))
				.OrderBy(h => observer.Position.DistanceSquared(h.Settlement.GatePosition))
				.Take(12).Select(h => (h.Settlement.Name?.ToString() ?? h.Settlement.StringId ?? WorldMapPromptTexts.Get("AF_WM_TargetSettlement")).Trim()
					+ idSeparator + h.Settlement.StringId).ToArray();
			return header + (targets.Length == 0 ? noTargets : "\n" + string.Join("\n", targets));
		}
		catch (Exception ex)
		{
			Log("hideout candidates unavailable: " + ex.Message);
			return header + noTargets;
		}
	}

	private void TickClearHideout(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (party.MapEvent != null)
		{
			TryGetNpcHideoutClearBattle(party.MapEvent, out _);
			return; // Never change movement, finish a task, or replay a battle during combat.
		}
		if (state.EngageCommitted)
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, WorldMapSourceTexts.Get("raw_literals.wm_lit_0332"), "hideout_result_missing");
			return;
		}
		Settlement target = ResolveSettlementById(command.TargetId);
		NpcHideoutTargetDecision decision = NpcHideoutClearPolicy.EvaluateTarget(CaptureHideoutClearTarget(target));
		if (!WorldMapNpcHideoutCompletionPatch.IsReady || decision == NpcHideoutTargetDecision.Missing || decision == NpcHideoutTargetDecision.Protected)
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Failure, decision == NpcHideoutTargetDecision.Protected
				? WorldMapSourceTexts.Get("raw_literals.wm_lit_0333") : WorldMapSourceTexts.Get("raw_literals.wm_lit_0334"), "hideout_unavailable");
			return;
		}
		if (decision == NpcHideoutTargetDecision.AlreadyCleared)
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, WorldMapSourceTexts.Get("raw_literals.wm_lit_0335"), "hideout_already_empty");
			return;
		}
		if (party.IsBandit || party.MapFaction == null || target.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(party.MapFaction, target.MapFaction))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Failure, WorldMapSourceTexts.Get("raw_literals.wm_lit_0336"), "hideout_actor_ineligible");
			return;
		}
		if (decision == NpcHideoutTargetDecision.Wait)
		{
			state.Stage = CommandStage.Tracking.ToString();
			return; // A busy battle/cooldown is rechecked on the existing hourly tick.
		}
		LeaveTargetSettlementIfInside(party, target);
		if (party.CurrentSettlement != null) LeaveSettlementAction.ApplyForParty(party);
		LockPartyAi(party);
		if (!IsPartyNearPosition(party, target.GatePosition, SettlementAttackCommitDistance))
		{
			if (state.Stage == CommandStage.New.ToString())
				DisplayCommandMessage(WorldMapUiText.HideoutTravel(GetActorName(state, hero, party), GetSettlementName(target)), CommandMessageTone.Progress);
			MoveTowardSettlementAttackPoint(party, target);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "clear_hideout_travel:" + target.StringId;
			return;
		}
		// Revalidate the precise target immediately before the one native mutation.
		if (!CanOfferHideoutClearTarget(target) || party.MapEvent != null || party.Party.NumberOfHealthyMembers <= 0) return;
		BeginResultTracking(state, "hideout_clear", "settlement", target.StringId, GetSettlementName(target), party.MapFaction, target.MapFaction);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		try
		{
			HideoutEventComponent component = HideoutEventComponent.CreateHideoutEvent(party.Party, target.Party, false);
			if (component?.MapEvent == null) throw new InvalidOperationException("Native hideout battle was not created.");
			_npcHideoutClearBattles.GetValue(component.MapEvent, _ => new NpcHideoutClearBattle { State = state, CommandIndex = state.CurrentIndex });
			DisplayCommandMessage(WorldMapUiText.HideoutEngage(GetActorName(state, hero, party), GetSettlementName(target)), CommandMessageTone.Progress);
			Log("hideout clear committed actor=" + GetActorLogId(state, hero, party) + " target=" + target.StringId);
		}
		catch (Exception ex)
		{
			Log("hideout battle creation failed: " + ex);
			// Creation may have committed before another mod's callback threw.
			// Restore ownership rather than detaching/replaying that partial battle.
			if (party.MapEvent != null && TryGetNpcHideoutClearBattle(party.MapEvent, out _)) return;
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Failure, WorldMapSourceTexts.Get("raw_literals.wm_lit_0337"), "hideout_create_failed");
		}
	}

	private bool TryGetNpcHideoutClearBattle(MapEvent mapEvent, out NpcHideoutClearBattle battle)
	{
		battle = null;
		if (mapEvent?.IsHideoutBattle != true) return false;
		if (_npcHideoutClearBattles.TryGetValue(mapEvent, out battle)) return true;
		MobileParty actor = mapEvent.AttackerSide?.LeaderParty?.MobileParty;
		if (actor == null || actor == MobileParty.MainParty) return false;
		string key = actor.LeaderHero?.StringId ?? BuildPartyActorKey(actor, createGuid: false);
		if (string.IsNullOrWhiteSpace(key)) return false;
		PartyCommandQueueState state;
		lock (_queueLock) _queues.TryGetValue(key, out state);
		PartyCommandEntry command = GetCurrentCommand(state);
		if (state == null || !state.EngageCommitted || !IsKind(command, CommandKind.ClearHideout)
			|| !string.Equals(state.ResultKind, "hideout_clear", StringComparison.Ordinal)
			|| !IsTargetSettlement(command, mapEvent.MapEventSettlement) || !PartyMatchesActor(actor, state)) return false;
		battle = _npcHideoutClearBattles.GetValue(mapEvent, _ => new NpcHideoutClearBattle { State = state, CommandIndex = state.CurrentIndex });
		return true;
	}

	private void CaptureNpcHideoutClearBattleEnd(MapEvent mapEvent)
	{
		if (!TryGetNpcHideoutClearBattle(mapEvent, out NpcHideoutClearBattle battle)) return;
		battle.PlayerParticipated = battle.PlayerParticipated || mapEvent.IsPlayerMapEvent || mapEvent.InvolvedParties.Contains(PartyBase.MainParty);
		battle.Casualties = BuildMapEventCasualtySummary(mapEvent, BattleSideEnum.Attacker, BattleSideEnum.Defender);
		// MapEventEnded precedes native party removal. Confirmation/queue advancement
		// waits for FinalizeComponent; reporting success here would be premature.
	}

	internal static bool ShouldSuppressNpcHideoutPlayerCompletion(MapEvent mapEvent)
	{
		WorldMapPartyCommandBehavior owner = Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		if (owner == null || !owner.TryGetNpcHideoutClearBattle(mapEvent, out NpcHideoutClearBattle battle)) return false;
		battle.PlayerParticipated = battle.PlayerParticipated || mapEvent.IsPlayerMapEvent || mapEvent.InvolvedParties.Contains(PartyBase.MainParty);
		return NpcHideoutClearPolicy.SuppressPlayerCompletion(true, battle.PlayerParticipated);
	}

	internal static void FinalizeNpcHideoutClearBattle(MapEvent mapEvent)
	{
		WorldMapPartyCommandBehavior owner = Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		if (owner == null || !owner._npcHideoutClearBattles.TryGetValue(mapEvent, out NpcHideoutClearBattle battle) || battle.Finalized) return;
		battle.Finalized = true;
		PartyCommandQueueState state = battle.State;
		if (!owner.IsStateStillQueued(state) || state.CurrentIndex != battle.CommandIndex || !IsKind(GetCurrentCommand(state), CommandKind.ClearHideout)) return;
		Settlement target = mapEvent.MapEventSettlement;
		NpcHideoutBattleOutcome outcome = NpcHideoutClearPolicy.ResolveBattle(target?.IsHideout == true && mapEvent.HasWinner,
			mapEvent.WinningSide == BattleSideEnum.Attacker, HideoutHasBandits(target));
		if (outcome == NpcHideoutBattleOutcome.Success)
			target.Hideout.SetNextPossibleAttackTime(Campaign.Current.Models.HideoutModel.HideoutHiddenDuration);
		string detail = outcome == NpcHideoutBattleOutcome.Success ? WorldMapSourceTexts.Get("raw_literals.wm_lit_0338")
			: outcome == NpcHideoutBattleOutcome.Failure ? WorldMapSourceTexts.Get("raw_literals.wm_lit_0339") : WorldMapSourceTexts.Get("raw_literals.wm_lit_0340");
		CommandResultOutcome result = outcome == NpcHideoutBattleOutcome.Success ? CommandResultOutcome.Success
			: outcome == NpcHideoutBattleOutcome.Failure ? CommandResultOutcome.Failure : CommandResultOutcome.Incomplete;
		if (IsStopPending(state))
		{
			Hero hero = ResolveHeroByIdAny(state.HeroId);
			if (!state.ResultLogged)
			{
				state.ResultLogged = true;
				LogFact(state, hero, BuildAttackResultFact(hero, state, GetCurrentCommand(state), result, detail + battle.Casualties));
			}
			owner.ProcessPendingSafeExit(hero, ResolveActorParty(state, hero), state);
			return;
		}
		owner.TryCompleteCurrentAttackResult(state, result, detail + battle.Casualties, "hideout_clear_" + outcome.ToString().ToLowerInvariant());
	}
}
