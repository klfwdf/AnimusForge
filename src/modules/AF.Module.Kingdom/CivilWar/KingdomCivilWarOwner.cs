using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// Main-thread owner for the v4 civil-war state machine. Event callbacks update grievance and queue ids;
// bounded campaign work and explicit commands share the saved faction state and authoritative effects.
internal sealed partial class KingdomCivilWarOwner
{
	private const int MaxHistory = 12;
	private const int MaxClanGrievance = 100;
	// Row caps sized to the kingdom screen at 1080p: ~660px under the summary bar, 30px per clan row,
	// ~430px of faction header per column, two side boxes sharing the right column.
	private const string FactionPrefix = "civilwar-faction-";
	private readonly KingdomCivilWarStorage _storage = new KingdomCivilWarStorage();
	// Settlement -> faction id that marked it; owners change during the war, so clearing cannot use the current owner.
	private readonly Dictionary<string, string> _oppositionMarkFaction = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _openWarKingdoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _activeNamingRequests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	// Reused on the campaign thread: no per-clan key-array allocation or per-point exponentiation.
	private readonly List<string> _decaySourceKeys = new List<string>(16);
	private static readonly double UnknownSourceDailyRetention = Math.Pow(0.85d, 1d / 7d);

	internal KingdomCivilWarStorage Storage { get { return _storage; } }

	internal void Replace(KingdomCivilWarStorage loaded)
	{
		_decayWork?.Dispose(); _decayWork = null; _decayQueue.Clear(); _decayDue.Clear();
		_politicalWork?.Dispose(); _politicalWork = null; _politicalQueue.Clear(); _politicalDirty.Clear(); _lastPoliticalDay = -1;
		_storage.Kingdoms.Clear();
		_storage.Version = 4;
		_storage.ClanExitUntilDay = loaded?.ClanExitUntilDay ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.Operations = loaded?.Operations ?? new Dictionary<string, CivilWarOperation>(StringComparer.Ordinal); _storage.ProtectedClanUntilDay = loaded?.ProtectedClanUntilDay ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		Revision++;
		_oppositionMarkFaction.Clear();
		_openWarKingdoms.Clear();
		_activeNamingRequests.Clear();
		HasUnpromptedPlayerUltimatum = false;
		HasPendingFollowPrompt = false;
		_hasPoliticalResponse = false;
		if (loaded == null || loaded.Kingdoms == null) return;
		foreach (KeyValuePair<string, KingdomCivilWarKingdomState> pair in loaded.Kingdoms)
		{
			KingdomCivilWarKingdomState state = Sanitize(pair.Value);
			if (state == null) continue;
			MigratePoliticalDays(state);
			Kingdom live = CivilWarWorld.FindKingdom(state.KingdomId);
			if (live != null && live.IsEliminated && state.Factions.Count == 0) continue;
			_storage.Kingdoms[state.KingdomId] = state;
			foreach (KingdomCivilWarFactionState faction in state.Factions)
			{
				if (faction.PendingResponse != null && !faction.PendingResponse.Accepted) _hasPoliticalResponse = true;
				if (faction.PlayerAnswerPending && !faction.PlayerPrompted) HasUnpromptedPlayerUltimatum = true;
				// Saved before answering the follow prompt (or before it popped): ask again after loading.
				if (faction.PlayerFollowPending && faction.Stage == KingdomCivilWarStage.OpenWar && !string.IsNullOrWhiteSpace(faction.RebelKingdomId)) { faction.PlayerFollowAsked = false; HasPendingFollowPrompt = true; }
				if (faction.Stage != KingdomCivilWarStage.OpenWar) continue;
				_openWarKingdoms.Add(state.KingdomId);
				IndexOppositionSettlements(state, faction);
			}
		}
	}

	// ------------------------------------------------------------ queries

	// Also true during the post-war/post-rebellion cooldown: a kingdom hosts one upheaval at a time, so the
	// host's stability rebellion must not fire right after a faction settles (or a rebellion just happened).
	internal bool HasTrackedKingdom(Kingdom kingdom)
	{
		KingdomCivilWarKingdomState state = Find(kingdom);
		return state != null && (state.Factions.Count > 0 || CivilWarWorld.CurrentDay() < state.CooldownUntilDay);
	}

	// A host stability/coup rebellion happened outside the faction flow: start the same kingdom cooldown.
	internal void NoteKingdomRebellion(Kingdom kingdom)
	{
		if (kingdom == null) return;
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, CivilWarWorld.CurrentWeek());
		state.CooldownUntilWeek = Math.Max(state.CooldownUntilWeek, CivilWarWorld.CurrentWeek() + tuning.CooldownWeeks);
		state.CooldownUntilDay = Math.Max(state.CooldownUntilDay, CivilWarWorld.CurrentDay() + tuning.CooldownWeeks * 7);
		AddHistory(state, CivilWarWorld.CurrentWeek(), "王国刚经历叛乱，" + tuning.CooldownWeeks + " 周内不再形成新派系");
		Revision++;
	}

	// War/peace between a kingdom and one of its own civil-war rebel kingdoms is part of the civil war, not a crown decision.
	internal bool IsCivilWarPair(Kingdom a, Kingdom b)
	{
		if (a == null || b == null || _openWarKingdoms.Count == 0) return false;
		return IsRebelOf(a, b) || IsRebelOf(b, a);
	}

	private bool IsRebelOf(Kingdom crown, Kingdom rebel)
	{
		if (!_openWarKingdoms.Contains(crown.StringId ?? "")) return false;
		KingdomCivilWarKingdomState state = Find(crown);
		return state != null && state.Factions.Any(x => string.Equals(x.RebelKingdomId, rebel.StringId, StringComparison.OrdinalIgnoreCase));
	}

	// O(open-war kingdoms): true while this kingdom is the rebel side of a running civil war.
	private bool IsActiveRebelKingdom(Kingdom kingdom)
	{
		if (kingdom == null || _openWarKingdoms.Count == 0) return false;
		foreach (string crownId in _openWarKingdoms)
		{
			KingdomCivilWarKingdomState state;
			if (_storage.Kingdoms.TryGetValue(crownId, out state) && state.Factions.Any(x => string.Equals(x.RebelKingdomId, kingdom.StringId, StringComparison.OrdinalIgnoreCase))) return true;
		}
		return false;
	}

	internal bool IsInOpenCivilWar(string kingdomId)
	{
		return !string.IsNullOrWhiteSpace(kingdomId) && _openWarKingdoms.Contains(kingdomId.Trim());
	}

	internal int GetOppositionLoyaltyDelta(Settlement settlement)
	{
		string id = settlement == null ? "" : settlement.StringId;
		return !string.IsNullOrWhiteSpace(id) && _oppositionMarkFaction.ContainsKey(id) ? -1 : 0;
	}

	private KingdomCivilWarKingdomState Find(Kingdom kingdom)
	{
		KingdomCivilWarKingdomState state;
		return kingdom != null && _storage.Kingdoms.TryGetValue(kingdom.StringId ?? "", out state) ? state : null;
	}

	private KingdomCivilWarFactionState FindFaction(string factionId, out KingdomCivilWarKingdomState owner)
	{
		owner = null;
		if (string.IsNullOrWhiteSpace(factionId)) return null;
		foreach (KingdomCivilWarKingdomState state in _storage.Kingdoms.Values)
		{
			KingdomCivilWarFactionState faction = state.Factions.FirstOrDefault(x => string.Equals(x.Id, factionId.Trim(), StringComparison.OrdinalIgnoreCase));
			if (faction == null) continue;
			owner = state;
			return faction;
		}
		return null;
	}

	private static KingdomCivilWarFactionState FactionOfClan(KingdomCivilWarKingdomState state, Clan clan)
	{
		KingdomCivilWarClanState record;
		if (state == null || clan == null || !state.Clans.TryGetValue(clan.StringId ?? "", out record) || record.Side != KingdomCivilWarSide.Opposition) return null;
		return state.Factions.FirstOrDefault(x => string.Equals(x.Id, record.FactionId, StringComparison.OrdinalIgnoreCase));
	}

	private static IEnumerable<KingdomCivilWarClanState> Members(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction)
	{
		return state.Clans.Values.Where(x => x != null && x.Side == KingdomCivilWarSide.Opposition && string.Equals(x.FactionId, faction.Id, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsPreWar(KingdomCivilWarFactionState faction)
	{
		return faction != null && (faction.Stage == KingdomCivilWarStage.FactionFormed || faction.Stage == KingdomCivilWarStage.Ultimatum);
	}

	// ------------------------------------------------------------ event entry points (no rolls, no scans)

	internal void AddGrievance(Kingdom kingdom, string sourceId, IEnumerable<Clan> clans, float points, int week, string text)
	{
		if (kingdom == null || !DuelSettings.IsCivilWarFactionsEnabled() || (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed())) return;
		if (IsActiveRebelKingdom(kingdom)) return;
		CivilWarGrievanceSourceDef source = CivilWarCatalog.FindSource(sourceId);
		if (source == null || points <= 0f) return;
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, week);
		int withdrawalThreshold = -1;
		List<string> withdrawnClans = null;
		foreach (Clan clan in (clans ?? Enumerable.Empty<Clan>()).Where(x => x != null && x.Kingdom == kingdom && IsPoliticalClan(x)))
		{
			KingdomCivilWarClanState record = GetOrCreateClan(state, clan, week);
			DecayPoliticalClan(kingdom, state, record, CivilWarWorld.CurrentDay());
			AddPoints(record, source.Id, points);
			// Support is conditional on events, not permanent immunity from political discontent.
			// Reuse the existing discontent threshold; the player still chooses their own allegiance.
			if (record.Side != KingdomCivilWarSide.Crown || clan == kingdom.RulingClan || clan == Clan.PlayerClan) continue;
			if (withdrawalThreshold < 0) withdrawalThreshold = DuelSettings.BuildCivilWarTuning().DiscontentThreshold;
			if (TotalGrievance(record) < withdrawalThreshold) continue;
			record.Side = KingdomCivilWarSide.Middle;
			record.FactionId = "";
			record.SideSinceWeek = week; record.SideSinceDay = CivilWarWorld.CurrentDay();
			if (withdrawnClans == null) withdrawnClans = new List<string>();
			withdrawnClans.Add(CivilWarWorld.ClanName(clan));
		}
		if (!string.IsNullOrWhiteSpace(text)) AddHistory(state, week, text);
		if (withdrawnClans != null)
		{
			string withdrawal = string.Join("、", withdrawnClans) + "因" + source.Name + "等事件积累不满，撤回对王室的支持，转为中立";
			AddHistory(state, week, withdrawal);
			WriteMaterial(kingdom, week, withdrawal);
			Logger.Log("KingdomCivilWar", "crown support withdrawn kingdom=" + kingdom.StringId + " source=" + source.Id + " clans=" + withdrawnClans.Count);
		}
		NotifyPoliticalChange(kingdom, sourceId);

	}

	// continue_war pledge: peace with the pledged target before the deadline breaks it once.
	internal void RecordPeace(Kingdom kingdom, IFaction other, int week)
	{
		KingdomCivilWarKingdomState state = Find(kingdom);
		if (state != null && other is Kingdom && !IsCivilWarPair(kingdom, other as Kingdom)) RelieveWeariness(state);
		if (state == null || other == null || string.IsNullOrWhiteSpace(state.NoPeaceTargetId)) return;
		if (CivilWarWorld.CurrentDay() > (state.NoPeaceUntilDay < 0 ? state.NoPeaceUntilWeek * 7 : state.NoPeaceUntilDay)) { ClearPledge(state); return; }
		if (!string.Equals(state.NoPeaceTargetId, other.StringId, StringComparison.OrdinalIgnoreCase)) return;
		List<Clan> clans = (state.NoPeaceClanIds ?? new List<string>()).Select(CivilWarWorld.FindClan).Where(x => x != null).ToList();
		ClearPledge(state);
		AddGrievance(kingdom, "broken_pledge", clans, 25f, week, "国王违背承诺，与" + (other.Name?.ToString() ?? "敌国") + "议和");
	}

	// ------------------------------------------------------------ war weariness (battle by battle)

	// Current weariness with lazy decay; no daily pass touches it.
	private static float Weariness(KingdomCivilWarKingdomState state)
	{
		return state == null ? 0f : CivilWarWearinessRules.Decayed(state.Weariness, state.WearinessDay, CivilWarWorld.CurrentDay());
	}

	internal float GetWeariness(Kingdom kingdom) => Weariness(Find(kingdom));

	// Called once per finished battle side (MapEventEnded). O(1): one dictionary lookup and arithmetic.
	internal void RecordBattleWeariness(Kingdom kingdom, int casualties, int committed, bool lost)
	{
		if (kingdom == null || kingdom.IsEliminated || IsActiveRebelKingdom(kingdom)) return;
		float points = CivilWarWearinessRules.BattlePoints(casualties, committed, lost);
		if (points <= 0f) return;
		int day = CivilWarWorld.CurrentDay();
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, CivilWarWorld.CurrentWeek());
		bool wasBelow = !CivilWarWearinessRules.MeetsFormation(Weariness(state));
		state.Weariness = CivilWarRules.Clamp(Weariness(state) + points, 0f, CivilWarWearinessRules.Max);
		state.WearinessDay = day;
		// Crossing the formation line is a political event; otherwise wake the kingdom at most once a week.
		if (wasBelow && CivilWarWearinessRules.MeetsFormation(state.Weariness) || day >= state.WearinessNotifyDay + 7)
		{
			state.WearinessNotifyDay = day;
			NotifyPoliticalChange(kingdom, "war_weariness");
		}
	}

	private static void RelieveWeariness(KingdomCivilWarKingdomState state)
	{
		if (state == null) return;
		state.Weariness = CivilWarWearinessRules.AfterPeace(Weariness(state));
		state.WearinessDay = CivilWarWorld.CurrentDay();
	}

	private static void ClearPledge(KingdomCivilWarKingdomState state)
	{
		state.NoPeaceTargetId = "";
		state.NoPeaceUntilWeek = 0; state.NoPeaceUntilDay = 0;
		state.NoPeaceClanIds = new List<string>();
	}

	internal void RecordPolicyImposed(Kingdom kingdom, string policyId, int week, string text)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(policyId)) return;
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, week);
		state.LastImposedPolicyId = policyId.Trim();
		AddGrievance(kingdom, "policy_imposed", CivilWarWorld.Vassals(kingdom), 8f, week, text);
	}

	// Player-king ultimatum prompt: set by the weekly slice, drained by a cheap hourly flag check.
	internal bool HasUnpromptedPlayerUltimatum { get; private set; }

	internal KingdomCivilWarFactionState TakeUnpromptedPlayerUltimatum(out KingdomCivilWarKingdomState owner)
	{
		HasUnpromptedPlayerUltimatum = false;
		owner = null;
		foreach (KingdomCivilWarKingdomState state in _storage.Kingdoms.Values)
		{
			if (!CivilWarWorld.IsPlayerRuled(CivilWarWorld.FindKingdom(state.KingdomId))) continue;
			foreach (KingdomCivilWarFactionState faction in state.Factions)
			{
				if (!faction.PlayerAnswerPending || faction.PlayerPrompted) continue;
				faction.PlayerPrompted = true;
				owner = state;
				// More prompts may remain; the next hourly check takes them one at a time.
				HasUnpromptedPlayerUltimatum = state.Factions.Any(x => x.PlayerAnswerPending && !x.PlayerPrompted) || _storage.Kingdoms.Values.Any(s => s != state && s.Factions.Any(x => x.PlayerAnswerPending && !x.PlayerPrompted));
				return faction;
			}
		}
		return null;
	}

	internal static string DescribeUltimatum(KingdomCivilWarFactionState faction)
	{
		if (faction == null) return "";
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		return FactionName(faction, demand) + "递交最后通牒：" + FormatDemand(demand, faction.TargetName)
			+ "\n\n已被拒绝 " + faction.Refusals + " 次。若不答复，将在第 " + faction.AnswerDeadlineDay + " 天自动判定。拒绝可能导致内战。";
	}

	// One campaign event per day. Only stored kingdoms/clans/sources are visited, never the world clan list.
	internal void AdvanceDay(int dayIndex)
	{
		if (dayIndex < 0 || _lastPoliticalDay == dayIndex) return;
		foreach (var state in _storage.Kingdoms.Values)
		{
			if (!_decayDue.ContainsKey(state.KingdomId)) _decayQueue.Enqueue(state.KingdomId);
			_decayDue[state.KingdomId] = dayIndex;
			NotifyPoliticalChange(CivilWarWorld.FindKingdom(state.KingdomId), "daily");
		}
		_lastPoliticalDay = dayIndex;
	}

	// ------------------------------------------------------------ weekly slice

	internal void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> ignoredRecentEvents)
	{
		if (kingdom == null) return;
		if (kingdom.IsEliminated) { NotifyPoliticalChange(kingdom, "kingdom_destroyed"); return; }
		// Compatibility host: decisions run once through the event worker, never again in the weekly fallback.
		NotifyPoliticalChange(kingdom, "weekly");
	}

	// A destroyed kingdom has no civil war left: drop its state, war flag and loyalty marks.
	private void DropKingdom(string kingdomId)
	{
		KingdomCivilWarKingdomState state;
		if (string.IsNullOrWhiteSpace(kingdomId) || !_storage.Kingdoms.TryGetValue(kingdomId, out state)) return;
		foreach (KingdomCivilWarFactionState faction in state.Factions) ClearWarMarks(faction.Id);
		_storage.Kingdoms.Remove(kingdomId);
		_openWarKingdoms.Remove(kingdomId);
	}

	// At most one new faction per kingdom per week. The leader is the most aggrieved clan that is not yet in a faction,
	// and the demand is one no other faction of the kingdom already holds.
	private void TryFormFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, int stability, CivilWarTuning tuning, Clan candidate = null)
	{
		if (!CivilWarFactionRules.CanFormFaction(state.Factions.Count, week, state.CooldownUntilWeek, tuning)) return;
		Clan leader = candidate ?? CivilWarWorld.Vassals(kingdom).Where(x => FactionOfClan(state, x) == null)
			.OrderByDescending(x => TotalGrievance(GetOrCreateClan(state, x, week))).FirstOrDefault();
		if (leader == null) return;
		if (!PoliticalClan(leader, kingdom) || leader == kingdom.RulingClan || leader == Clan.PlayerClan || FactionOfClan(state, leader) != null) return;
		KingdomCivilWarClanState leaderState = GetOrCreateClan(state, leader, week);
		float grievance = TotalGrievance(leaderState);
		float weariness = Weariness(state);
		if (!CivilWarFactionRules.MeetsFormationCondition(grievance, weariness, tuning)) return;
		CivilWarRoll roll = CivilWarRules.Roll("form_faction", CivilWarCatalog.FormFaction, BuildFeatures(kingdom, state, null, leader, stability), 1f, tuning, RandomFloat);
		string cause = grievance >= tuning.DiscontentThreshold ? "不满达到阈值" : "王国厌战（" + weariness.ToString("0") + "）";
		if (!roll.Passed) { AddHistory(state, week, CivilWarWorld.ClanName(leader) + cause + "，但成派判定未通过（" + roll.Chance.ToString("0.00") + "）"); return; }
		List<string> taken = state.Factions.Select(x => x.DemandId).ToList();
		List<CivilWarDemandDef> eligible = CivilWarCatalog.ValidDemands.Where(x => !CivilWarFactionRules.IsDemandTaken(taken, x.Id) && IsDemandEligible(x, kingdom, leader, state)).ToList();
		CivilWarDemandDef demand = CivilWarDecisions.PickDemand(eligible, leaderState.Grievance, tuning, RandomFloat);
		if (demand == null) return;
		KingdomCivilWarFactionState faction = new KingdomCivilWarFactionState
		{
			Id = FactionPrefix + kingdom.StringId + "-" + (++state.FactionSerial), DemandId = demand.Id, Stage = KingdomCivilWarStage.FactionFormed, StageWeek = week,
			LeaderClanId = leader.StringId ?? "", CreatedWeek = week, UltimatumWeek = week + tuning.UltimatumDelayWeeks, UltimatumDay = CivilWarWorld.CurrentDay() + tuning.UltimatumDelayWeeks * 7, LastDemandDay = CivilWarWorld.CurrentDay(),
			TargetId = ResolveTargetId(demand, kingdom, state), TargetName = ResolveTargetName(demand, kingdom, state), WarGoal = (int)CivilWarCatalog.EffectiveWarGoal(demand),
			Grievance = grievance
		};
		if (demand.Target != CivilWarDemandTarget.None && string.IsNullOrWhiteSpace(faction.TargetId)) return;
		state.Factions.Add(faction);
		leaderState.Side = KingdomCivilWarSide.Opposition;
		leaderState.FactionId = faction.Id;
		leaderState.SideSinceWeek = week; leaderState.SideSinceDay = CivilWarWorld.CurrentDay();
		ApplyFoundingRelationLoss(kingdom, leader);
		string name = FactionName(faction, demand);
		AddHistory(state, week, name + "成立，诉求：" + FormatDemand(demand, faction.TargetName));
		PublishPoliticalResult(kingdom, state, faction, leader, faction.Id + ":founded", "反对派成立：" + name + "，诉求" + FormatDemand(demand, faction.TargetName));
	}

	private void AdvanceUltimatum(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, int stability, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		if (demand == null || leader == null || leader.Kingdom != kingdom || leader.Leader == null || !leader.Leader.IsAlive)
		{
			Dissolve(kingdom, state, faction, week, tuning, FactionName(faction, demand) + "的诉求目标或领袖已失效，派系解散");
			return;
		}
		// Single-war mode: an escalated faction waits for the other war to end, then rebels without a new roll.
		if (faction.WaitingForOtherWar)
		{
			if (!CivilWarFactionRules.CanOpenWar(OtherFactionAtWar(state, faction), tuning)) return;
			faction.WaitingForOtherWar = false;
			OpenWar(kingdom, state, faction, leader, week, tuning, adjustStability);
			return;
		}
		// Single-war mode: while another faction fights, the others pause (no new ultimatum, no ruling).
		if (IsPausedByOtherWar(state, faction, tuning) && !faction.PlayerAnswerPending) return;
		if (CivilWarWorld.CurrentDay() < faction.UltimatumDay) return;
		faction.Stage = KingdomCivilWarStage.Ultimatum;
		faction.DemandLocked = true;
		bool playerKing = CivilWarWorld.IsPlayerRuled(kingdom);
		if (playerKing && !faction.PlayerAnswerPending)
		{
			faction.PlayerAnswerPending = true;
			faction.PlayerPrompted = false;
			faction.PlayerAnswerDeadlineWeek = week + tuning.PlayerAnswerWeeks;
			faction.AnswerDeadlineDay = CivilWarWorld.CurrentDay() + tuning.PlayerAnswerWeeks * 7;
			HasUnpromptedPlayerUltimatum = true;
			AddHistory(state, week, FactionName(faction, demand) + "的最后通牒已送达玩家国王，等待明确答复");
			return;
		}
		if (playerKing && faction.PlayerAnswerPending && CivilWarWorld.CurrentDay() < faction.AnswerDeadlineDay) return;
		if (!playerKing && TryAiGovernance(kingdom, state, faction)) return;
		Dictionary<string, float> features = BuildFeatures(kingdom, state, faction, leader, stability);
		CivilWarUltimatumResult ruling = playerKing
			? CivilWarDecisions.RollRefusal(demand, features, faction.Refusals, tuning, RandomFloat, CivilWarAftermathRules.EscalationFactor(CurrentAftermath(state, week)))
			: CivilWarDecisions.RuleOnUltimatum(demand, features, faction.Refusals, tuning, RandomFloat, CivilWarAftermathRules.EscalationFactor(CurrentAftermath(state, week)), CivilWarFactionRules.AcceptScale(InConcessionWindow(state, faction, tuning)));
		ApplyRuling(kingdom, state, faction, demand, leader, week, tuning, adjustStability, ruling);
	}

	// Returns true only when an accepted demand was actually carried out.
	private bool ApplyRuling(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, CivilWarDemandDef demand, Clan leader, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability, CivilWarUltimatumResult ruling)
	{
		string name = FactionName(faction, demand);
		faction.LastRuling = RulingText(ruling.Ruling, ruling.Escalate);
		faction.LastEscalateChance = ruling.EscalateRoll.Chance;
		faction.PlayerAnswerPending = false;
		Logger.Log("KingdomCivilWar", "ruling faction=" + faction.Id + " " + ruling.Ruling + " " + CivilWarWorld.Limit(ruling.Log, 300));
		if (ruling.Ruling == CivilWarRuling.Accept)
		{
			var result = Execute(new CivilWarActionRequest { OperationId = "ultimatum:" + faction.Id + ":" + CivilWarWorld.CurrentDay(), KingdomId = kingdom.StringId, FactionId = faction.Id, Action = CivilWarAction.Concede }, kingdom.RulingClan);
			return result.Status == CivilWarActionStatus.Applied;
		}
		if (ruling.Ruling == CivilWarRuling.Defer)
		{
			// Stalling has its own limit so a king who always defers cannot hold a faction forever. It is not a refusal:
			// refusals drive escalation and usurp conversion, which a deferral must not.
			faction.Defers++;
			if (!IsPlayerLed(faction) && faction.Defers >= tuning.MaxRefusals) { Dissolve(kingdom, state, faction, week, tuning, name + "被国王一再拖延，派系瓦解"); return false; }
			RefuseAndReschedule(faction, week, tuning);
			AddHistory(state, week, "国王暂缓答复" + name + "，最后通牒延后");
			return false;
		}
		faction.Refusals++;
		AddGrievanceToFaction(state, faction, "demand_refused", 20f, 8f);
		faction.LastEscalationDay = CivilWarWorld.CurrentDay(); faction.EscalationPending = false;
		if (ruling.ConvertToUsurp && !IsPlayerLed(faction))
		{
			CivilWarDemandDef usurp = CivilWarCatalog.FindDemand(CivilWarCatalog.UsurpDemandId);
			// Another faction may already claim the throne; then this one keeps its demand.
			if (usurp != null && !state.Factions.Any(x => x != faction && x.DemandId == usurp.Id))
			{
				faction.DemandId = usurp.Id; faction.WarGoal = (int)CivilWarWarGoal.Usurp; faction.TargetId = ""; faction.TargetName = "王位"; demand = usurp;
				AddHistory(state, week, name + "屡遭拒绝，转而要求国王退位");
			}
		}
		// Escalation is decided by the roll (x0.3 for non-war demands via the catalog EscalationScale),
		// but only once the faction has been refused often enough.
		bool enoughRefusals = CivilWarFactionRules.HasEnoughRefusals(faction.Refusals, tuning);
		if (ruling.Escalate && enoughRefusals && !IsPlayerLed(faction))
		{
			OpenWar(kingdom, state, faction, leader, week, tuning, adjustStability);
			return false;
		}
		if (!IsPlayerLed(faction) && faction.Refusals >= tuning.MaxRefusals)
		{
			Dissolve(kingdom, state, faction, week, tuning, name + "多次被拒仍未起兵，派系瓦解");
			return false;
		}
		RefuseAndReschedule(faction, week, tuning);
		AddHistory(state, week, "国王拒绝" + name + "的诉求，" + (ruling.Escalate && !enoughRefusals
			? "但派系被拒次数未满（" + faction.Refusals + "/" + tuning.MinRefusalsBeforeWar + "），暂不起兵"
			: "但升级判定未通过"));
		return false;
	}

	private static bool OtherFactionAtWar(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction)
	{
		return state.Factions.Any(x => x != faction && x.Stage == KingdomCivilWarStage.OpenWar);
	}

	private static bool IsPausedByOtherWar(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, CivilWarTuning tuning)
	{
		return IsPreWar(faction) && !CivilWarFactionRules.CanOpenWar(OtherFactionAtWar(state, faction), tuning);
	}

	// One faction took up arms: the kingdom enters its cooldown (no new faction) and every other pre-war faction
	// holds its next ultimatum until the cooldown ends. Runs once per war outbreak, over this kingdom's factions only.
	private void CoolDownOtherFactions(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState rising, int week, CivilWarTuning tuning, string risingName)
	{
		int day = CivilWarWorld.CurrentDay();
		state.CooldownUntilWeek = Math.Max(state.CooldownUntilWeek, week + tuning.CooldownWeeks);
		state.CooldownUntilDay = Math.Max(state.CooldownUntilDay, day + tuning.CooldownWeeks * 7);
		int held = 0;
		foreach (KingdomCivilWarFactionState other in state.Factions)
		{
			if (other == rising || !IsPreWar(other)) continue;
			other.UltimatumWeek = Math.Max(other.UltimatumWeek, week + tuning.CooldownWeeks);
			other.UltimatumDay = Math.Max(other.UltimatumDay, day + tuning.CooldownWeeks * 7);
			held++;
		}
		if (held > 0) AddHistory(state, week, risingName + "起兵，其余派系进入冷却（第 " + state.CooldownUntilDay + " 天前不递交最后通牒）");
	}

	// Only pre-war factions get the softer king; a faction already in arms is settled by the war.
	private static bool InConcessionWindow(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, CivilWarTuning tuning)
		=> state != null && IsPreWar(faction) && CivilWarFactionRules.InConcessionWindow(CivilWarWorld.CurrentDay(), state.CooldownUntilDay, tuning);

	// Penalties and the world bulletin fire only when the rebel kingdom really exists (OnRebelKingdomCreated).
	private void OpenWar(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, Clan leader, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability, bool playerAuthorized = false)
	{
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		if (IsPlayerLed(faction) && !playerAuthorized)
		{
			faction.WaitingForOtherWar = false; faction.EscalationPending = false;
			RefuseAndReschedule(faction, week, tuning);
			return;
		}
		if (faction.Stage == KingdomCivilWarStage.OpenWar) return;
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			RefuseAndReschedule(faction, week, tuning);
			AddHistory(state, week, "玩家王国的稳定度叛乱免疫阻止了" + name + "起兵");
			return;
		}
		if (!CivilWarFactionRules.CanOpenWar(OtherFactionAtWar(state, faction), tuning))
		{
			faction.WaitingForOtherWar = true;
			faction.Stage = KingdomCivilWarStage.FactionFormed;
			AddHistory(state, week, name + "决意起兵，但国内已有内战，暂时按兵不动");
			return;
		}
		faction.Stage = KingdomCivilWarStage.OpenWar;
		faction.StageWeek = week;
		_openWarKingdoms.Add(kingdom.StringId);
		CoolDownOtherFactions(state, faction, week, tuning, name);
		List<Clan> followers = Members(state, faction).Select(x => CivilWarWorld.FindClan(x.ClanId))
			.Where(x => x != null && x != leader && x != Clan.PlayerClan && x.Kingdom == kingdom).ToList();
		faction.WarClanIds = followers.Select(x => x.StringId).Concat(new[] { leader.StringId }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		faction.WarRequestWeek = week;
		faction.WarRequestDay = CivilWarWorld.CurrentDay(); faction.Version++;
		faction.RebelKingdomId = "";
		faction.WarStartWeek = 0;
		faction.LastFactionPower = FactionPower(kingdom, faction);
		// A player vassal in this faction is asked once the rebel kingdom exists (see OnRebelKingdomCreated / TakePendingFollowPrompt).
		KingdomCivilWarClanState player;
		faction.PlayerFollowPending = false; faction.PlayerFollowAsked = false; faction.PlayerFollowAccepted = false;
		if (leader != Clan.PlayerClan && Clan.PlayerClan != null && !CivilWarWorld.IsPlayerRuled(kingdom) && state.Clans.TryGetValue(Clan.PlayerClan.StringId ?? "", out player) && player.Side == KingdomCivilWarSide.Opposition && player.FactionId == faction.Id)
			faction.PlayerFollowPending = true;
		_activeNamingRequests.Add(faction.Id);
		Host.QueueRebellion(kingdom, leader, followers, faction.Id, true);
		IndexOppositionSettlements(state, faction);
		AddHistory(state, week, name + "起兵，正在建立临时叛军王国");
	}

	private void AdvanceOpenWar(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		if (faction.ResolutionNeedsReview) return;
		if (!string.IsNullOrWhiteSpace(faction.ResolutionOutcomeId))
		{
			ResolveWar(kingdom, state, faction, week, tuning, adjustStability);
			return;
		}
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		if (leader == null && string.IsNullOrWhiteSpace(faction.RebelKingdomId)) { Dissolve(kingdom, state, faction, week, tuning, name + "的领袖已不存在，起兵作罢"); return; }
		// With a rebel kingdom in the field, a lost leader means the rebellion collapsed: settle it as a crown victory
		// so the rebel kingdom is wound up instead of being left at war with no faction behind it.
		if (leader == null || leader.IsEliminated) faction.RebelKingdomDestroyed = true;
		if (string.IsNullOrWhiteSpace(faction.RebelKingdomId))
		{
			if (leader?.Kingdom != kingdom) { faction.ResolutionNeedsReview = true; faction.ResolutionError = "待建国领袖已离开原王国，保留状态核查。"; return; }
			if (_activeNamingRequests.Add(faction.Id)) Host.QueueRebellion(kingdom, leader, faction.WarClanIds.Select(CivilWarWorld.FindClan).Where(x => x != null && x != leader && x != Clan.PlayerClan && x.Kingdom == kingdom).ToList(), faction.Id, true);
			// WarStartWeek is only set by the creation callback; without it there is no war to resolve.
			if (CivilWarWorld.CurrentDay() <= faction.WarRequestDay + tuning.WarRequestTimeoutWeeks * 7) return;
			NotifyRebellionFailed(faction.Id, "建国请求超时");
			return;
		}
		Kingdom rebel = CivilWarWorld.FindKingdom(faction.RebelKingdomId);
		float crownPower = CivilWarWorld.Strength(kingdom);
		float rebelPower = CivilWarWorld.Strength(rebel);
		faction.LastFactionPower = CivilWarRules.Clamp(rebelPower / Math.Max(1f, crownPower + rebelPower), 0f, 1f);
		faction.LastWarScore = CivilWarRules.Clamp((rebelPower - crownPower) / Math.Max(1f, rebelPower + crownPower), -1f, 1f);
		if (!CivilWarWorld.IsAlive(rebel)) faction.RebelKingdomDestroyed = true;
		else if (!kingdom.IsAtWarWith(rebel)) faction.EndedByPeace = true;
		int elapsed = Math.Max(0, CivilWarWorld.CurrentDay() - faction.WarStartDay) / 7;
		if (faction.RebelKingdomDestroyed || faction.EndedByPeace) { ResolveWar(kingdom, state, faction, week, tuning, adjustStability); return; }
		if (elapsed < tuning.MinWarWeeks || (elapsed < tuning.MaxWarWeeks && CivilWarWorld.CurrentDay() < faction.WarEvaluationDay + 7)) return;
		faction.WarEvaluationDay = CivilWarWorld.CurrentDay();
		Dictionary<string, float> features = WarFeatures(kingdom, state, faction, leader, week, tuning);
		CivilWarRoll endRoll = CivilWarRules.Roll("war_end", CivilWarCatalog.WarEnds, features, elapsed >= tuning.MaxWarWeeks ? 2f : 1f, tuning, RandomFloat);
		if (elapsed >= tuning.MaxWarWeeks || endRoll.Passed)
		{
			if (elapsed < tuning.MaxWarWeeks && !CivilWarWorld.IsPlayerRuled(kingdom) && TryAiGovernance(kingdom, state, faction)) return;
			ResolveWar(kingdom, state, faction, week, tuning, adjustStability);
		}
	}

	private Dictionary<string, float> WarFeatures(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, Clan leader, int week, CivilWarTuning tuning)
	{
		Dictionary<string, float> features = BuildFeatures(kingdom, state, faction, leader, 50);
		features[CivilWarFeature.WarScore] = faction.LastWarScore;
		features[CivilWarFeature.WarScoreAbs] = Math.Abs(faction.LastWarScore);
		features[CivilWarFeature.WarProgress] = CivilWarRules.Clamp((week - faction.WarStartWeek) / (float)Math.Max(1, tuning.MaxWarWeeks), 0f, 1f);
		return features;
	}

	// Called by MyBehavior after a real temporary rebel kingdom is created.
	internal void OnRebelKingdomCreated(string factionId, Kingdom rebelKingdom, int week)
	{
		if (!DuelSettings.IsCivilWarFactionsEnabled() || rebelKingdom == null) return;
		KingdomCivilWarFactionState faction = FindFaction(factionId, out KingdomCivilWarKingdomState state);
		if (faction == null || faction.Stage != KingdomCivilWarStage.OpenWar || !string.IsNullOrWhiteSpace(faction.RebelKingdomId))
		{
			// The faction timed out or dissolved while the host was still naming the rebel kingdom.
			Logger.Log("KingdomCivilWar", "[WARN] rebel kingdom " + rebelKingdom.StringId + " created for inactive faction " + factionId + "; it now fights as an ordinary rebellion");
			return;
		}
		Kingdom kingdom = CivilWarWorld.FindKingdom(state.KingdomId);
		faction.RebelKingdomId = rebelKingdom.StringId ?? "";
		if (_storage.Operations.TryGetValue(faction.RebellionOperationId ?? "", out var completedRequest)) { completedRequest.Status = (int)CivilWarActionStatus.Applied; completedRequest.Message = "叛军王国已经建立，内战爆发。"; }
		_activeNamingRequests.Remove(faction.Id);
		faction.WarStartWeek = Math.Max(week, faction.WarRequestWeek);
		faction.WarStartDay = CivilWarWorld.CurrentDay();
		if (faction.LeaderClanId == Clan.PlayerClan?.StringId) { faction.PlayerFollowPending = false; faction.PlayerFollowAccepted = true; }
		IndexOppositionSettlements(state, faction);
		faction.RebelFortShareAtStart = CivilWarWorld.FortificationCount(rebelKingdom) / (float)Math.Max(1, CivilWarWorld.FortificationCount(kingdom));
		Host.AdjustStability(kingdom, -8, "civil_war_outbreak");
		Host.ApplyPrestige(kingdom, -10, "内战爆发");
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		AddHistory(state, week, name + "建立" + CivilWarWorld.KingdomName(rebelKingdom) + "，内战爆发");
		PublishPoliticalResult(kingdom, state, faction, CivilWarWorld.FindClan(faction.LeaderClanId), faction.Id + ":war:outbreak", "内战爆发：" + name + "建立" + CivilWarWorld.KingdomName(rebelKingdom) + "，与" + CivilWarWorld.KingdomName(kingdom) + "开战");
		Revision++;
		if (faction.PlayerFollowPending) HasPendingFollowPrompt = true;
	}

	// ------------------------------------------------------------ player follows the rebels (vassal in the rising faction)

	// Set when a rebel kingdom is created for the player's faction; drained by the hourly prompt check.
	internal bool HasPendingFollowPrompt { get; private set; }

	internal KingdomCivilWarFactionState TakePendingFollowPrompt()
	{
		HasPendingFollowPrompt = false;
		foreach (KingdomCivilWarKingdomState state in _storage.Kingdoms.Values)
			foreach (KingdomCivilWarFactionState faction in state.Factions)
			{
				if (!faction.PlayerFollowPending || faction.PlayerFollowAsked || faction.Stage != KingdomCivilWarStage.OpenWar || string.IsNullOrWhiteSpace(faction.RebelKingdomId)) continue;
				faction.PlayerFollowAsked = true;
				return faction;
			}
		return null;
	}

	internal static string DescribeFollowPrompt(KingdomCivilWarFactionState faction)
	{
		if (faction == null) return "";
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		string rebel = CivilWarWorld.KingdomName(CivilWarWorld.FindKingdom(faction.RebelKingdomId));
		return "你所在的" + name + "已起兵，建立" + rebel + "与王室开战。\n\n追随：你的家族带着封地转投" + rebel + "，内战结束后随结局一同归国（王室获胜时会受到惩处）。\n留下：你的家族改站王室一边，与昔日同党为敌。";
	}

	internal bool AnswerFollow(string factionId, bool follow, out string message)
	{
		message = "";
		KingdomCivilWarFactionState faction = FindFaction(factionId, out KingdomCivilWarKingdomState state);
		if (faction == null || !faction.PlayerFollowPending || faction.Stage != KingdomCivilWarStage.OpenWar) { message = "这次起兵已经结束。"; return false; }
		faction.PlayerFollowPending = false;
		Kingdom kingdom = CivilWarWorld.FindKingdom(state.KingdomId);
		Kingdom rebel = CivilWarWorld.FindKingdom(faction.RebelKingdomId);
		Clan player = Clan.PlayerClan;
		int week = CivilWarWorld.CurrentWeek();
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		KingdomCivilWarClanState record = GetOrCreateClan(state, player, week);
		if (follow && player != null && player.Kingdom == kingdom && CivilWarWorld.IsAlive(rebel) && Host.MovePlayerToRebels(player, kingdom, rebel))
		{
			faction.PlayerFollowAccepted = true;
			if (!faction.WarClanIds.Contains(player.StringId)) faction.WarClanIds.Add(player.StringId);
			IndexOppositionSettlements(state, faction);
			message = "你的家族追随" + name + "，转投" + CivilWarWorld.KingdomName(rebel) + "。";
			AddHistory(state, week, message);
			return true;
		}
		// Staying (or a failed defection): the player stands with the crown against the rising.
		record.Side = KingdomCivilWarSide.Crown; record.FactionId = ""; record.SideSinceWeek = week; record.SideSinceDay = CivilWarWorld.CurrentDay();
		state.PlayerSide = "crown";
		// Also repair marks made by an older build before the player chose to stay.
		if (player?.Settlements != null)
			foreach (Settlement settlement in player.Settlements)
				if (settlement != null && _oppositionMarkFaction.TryGetValue(settlement.StringId, out string markedBy) && markedBy == faction.Id)
					_oppositionMarkFaction.Remove(settlement.StringId);
		message = follow ? "未能转投叛军，你的家族留在王国一方。" : "你的家族拒绝追随" + name + "，站到了王室一边。";
		AddHistory(state, week, message);
		return true;
	}

	// Host asks before naming/executing a queued civil-war rebellion: false means the faction moved on (timed out,
	// dissolved, already has its rebel kingdom) and the queued request must be dropped.
	internal bool IsRebellionRequestActive(string factionId)
	{
		KingdomCivilWarFactionState faction = FindFaction(factionId, out _);
		return DuelSettings.IsCivilWarFactionsEnabled() && faction != null && faction.Stage == KingdomCivilWarStage.OpenWar && string.IsNullOrWhiteSpace(faction.RebelKingdomId);
	}

	// Host gave up (execution failed, naming skipped, immunity). Nothing was fought: dissolve without a result or penalty.
	internal void NotifyRebellionFailed(string factionId, string reason)
	{
		_activeNamingRequests.Remove(factionId ?? "");
		KingdomCivilWarFactionState faction = FindFaction(factionId, out KingdomCivilWarKingdomState state);
		if (faction == null || faction.Stage != KingdomCivilWarStage.OpenWar || !string.IsNullOrWhiteSpace(faction.RebelKingdomId)) return;
		Kingdom kingdom = CivilWarWorld.FindKingdom(state.KingdomId);
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		Logger.Log("KingdomCivilWar", "rebellion failed faction=" + faction.Id + " reason=" + (reason ?? ""));
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		faction.ResolutionError = "起兵尚未完成：" + CivilWarWorld.Limit(reason, 160);
		if (_storage.Operations.TryGetValue(faction.RebellionOperationId ?? "", out var failedRequest)) { failedRequest.Status = (int)CivilWarActionStatus.PartialFailure; failedRequest.Message = faction.ResolutionError; }
		if (leader?.Kingdom == kingdom)
		{
			faction.Stage = KingdomCivilWarStage.FactionFormed; faction.WarRequestDay = -1; faction.WarClanIds.Clear();
			faction.PlayerFollowPending = false; faction.PlayerFollowAsked = false;
			faction.UltimatumDay = CivilWarWorld.CurrentDay() + 7; faction.WaitingForOtherWar = false;
			ClearWarMarks(faction.Id); if (!state.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) _openWarKingdoms.Remove(state.KingdomId);
		}
		else faction.ResolutionNeedsReview = true;
		AddHistory(state, CivilWarWorld.CurrentWeek(), faction.ResolutionError + "；派系已保留。");
		faction.Version++; SaveSummary(state); Revision++;
	}

	private void ResolveWar(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		CivilWarWarGoal goal = faction.WarGoal == 0 ? CivilWarCatalog.EffectiveWarGoal(demand) : (CivilWarWarGoal)faction.WarGoal;
		string log = "forced";
		CivilWarOutcomeDef outcome;
		bool restoration = !string.IsNullOrWhiteSpace(faction.RestorationClanId);
		// Only an explicit war victory authorizes restoration. Our own return effect later
		// makes peace, so retain that authorization across retries rather than rerolling it.
		if (restoration && !faction.RestorationVictoryConfirmed && faction.RebelKingdomDestroyed)
			outcome = CivilWarCatalog.FindOutcome(CivilWarCatalog.CrownVictoryOutcomeId);
		else if (restoration && !faction.RestorationVictoryConfirmed && faction.EndedByPeace)
			outcome = CivilWarCatalog.FindOutcome(CivilWarCatalog.NegotiatedOutcomeId);
		else if (!string.IsNullOrWhiteSpace(faction.ResolutionOutcomeId)) outcome = CivilWarCatalog.FindOutcome(faction.ResolutionOutcomeId);
		else if (faction.RebelKingdomDestroyed) outcome = CivilWarCatalog.FindOutcome(CivilWarCatalog.CrownVictoryOutcomeId);
		else if (faction.EndedByPeace) outcome = CivilWarCatalog.FindOutcome(goal == CivilWarWarGoal.Secede ? CivilWarCatalog.SecedeOutcomeId : CivilWarCatalog.NegotiatedOutcomeId);
		else outcome = CivilWarDecisions.PickOutcome(goal, WarFeatures(kingdom, state, faction, leader, week, tuning), tuning, RandomFloat, out log);
		if (outcome != null) faction.ResolutionOutcomeId = outcome.Id;
		if (restoration && outcome?.Id == "rebels_usurp" && !faction.EndedByPeace && !faction.RebelKingdomDestroyed)
			faction.RestorationVictoryConfirmed = true;
		CivilWarEffectContext ctx = BuildContext(kingdom, state, faction, demand, leader, week);
		string reason = "无可用结局";
		string effectId = restoration && faction.RestorationVictoryConfirmed && outcome?.Id == "rebels_usurp"
			? CivilWarEffectIds.RestoreDynasty : outcome?.EffectId;
		bool applied = outcome != null && CivilWarEffects.TryApply(effectId, ctx, out reason);
		string name = FactionName(faction, demand);
		string text = applied ? name + "的内战结束（" + outcome.Name + "）：" + string.Join("；", ctx.Notes) : name + "的内战结算失败：" + (reason ?? "无可用结局");
		AddHistory(state, week, text);
		if (applied) PublishPoliticalResult(kingdom, state, faction, leader, faction.Id + ":war:resolved", text);
		Logger.Log("KingdomCivilWar", "resolve faction=" + faction.Id + " outcome=" + (outcome?.Id ?? "none") + " applied=" + applied + " log=" + CivilWarWorld.Limit(log, 300));
		if (!applied)
		{
			faction.ResolutionError = CivilWarWorld.Limit(reason, 240);
			// Guard/reconciliation failures can retry weekly. An unexpected exception may have applied
			// additive effects already: retain the save record and require inspection, never replay blindly.
			faction.ResolutionNeedsReview = outcome == null || !ctx.RetryableFailure;
			return;
		}
		FinishFaction(kingdom, state, faction, week, tuning, adjustStability, outcome.RebelsWon ? -4 : 6);
		CivilWarAftermath mood = outcome.Id == CivilWarCatalog.NegotiatedOutcomeId ? CivilWarAftermath.Settled
			: outcome.RebelsWon ? CivilWarAftermath.Emboldened : CivilWarAftermath.Suppressed;
		ApplyAftermath(kingdom, state, week, tuning, mood);
	}

	// A civil war just ended. Every remaining faction gets a truce before its next ultimatum, a faction that had
	// already decided to rebel must be asked again, and the outcome cows or emboldens them for a while.
	private void ApplyAftermath(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning, CivilWarAftermath mood)
	{
		int truce = CivilWarAftermathRules.TruceWeeks(tuning);
		state.Aftermath = (int)mood;
		state.AftermathUntilWeek = week + CivilWarAftermathRules.MoodWeeks(tuning);
		state.AftermathUntilDay = CivilWarWorld.CurrentDay() + CivilWarAftermathRules.MoodWeeks(tuning) * 7;
		float factor = CivilWarAftermathRules.GrievanceFactor(mood);
		foreach (KingdomCivilWarFactionState other in state.Factions)
		{
			if (!IsPreWar(other)) continue;
			other.UltimatumWeek = Math.Max(other.UltimatumWeek, week + truce);
			other.UltimatumDay = Math.Max(other.UltimatumDay, CivilWarWorld.CurrentDay() + truce * 7);
			other.WaitingForOtherWar = false;
			if (other.PlayerAnswerPending) { other.PlayerAnswerPending = false; other.PlayerPrompted = false; }
			other.Stage = KingdomCivilWarStage.FactionFormed;
			if (factor == 1f) continue;
			foreach (KingdomCivilWarClanState member in Members(state, other))
				foreach (string source in member.Grievance.Keys.ToList())
					member.Grievance[source] = CivilWarRules.Clamp(member.Grievance[source] * factor, 0f, MaxClanGrievance);
		}
		if (state.Factions.Count == 0) return;
		string text = mood == CivilWarAftermath.Suppressed ? "王室平叛震慑了其余派系，各派暂时偃旗息鼓"
			: mood == CivilWarAftermath.Emboldened ? "叛军得胜鼓舞了其余派系，但各派仍需时间整顿"
			: "内战平息，其余派系暂缓施压";
		AddHistory(state, week, text + "（" + truce + " 周内不递交最后通牒）");
		SaveSummary(state);
	}

	private static CivilWarAftermath CurrentAftermath(KingdomCivilWarKingdomState state, int week)
	{
		return state != null && CivilWarWorld.CurrentDay() <= (state.AftermathUntilDay < 0 ? state.AftermathUntilWeek * 7 : state.AftermathUntilDay) ? (CivilWarAftermath)state.Aftermath : CivilWarAftermath.None;
	}

	// Removes one faction. Its members return to the middle; other factions keep running. New factions wait CooldownWeeks.
	private void FinishFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability, int stabilityDelta, bool startCooldown = true)
	{
		if (stabilityDelta != 0 && kingdom != null) adjustStability?.Invoke(kingdom, stabilityDelta);
		faction.LastParticipantIds = Members(state, faction).Select(x => x.ClanId).ToList(); ProtectWarClans(faction);
		state.Factions.Remove(faction);
		if (startCooldown)
		{
			state.CooldownUntilWeek = Math.Max(state.CooldownUntilWeek, week + tuning.CooldownWeeks);
			state.CooldownUntilDay = Math.Max(state.CooldownUntilDay, CivilWarWorld.CurrentDay() + tuning.CooldownWeeks * 7);
		}
		foreach (KingdomCivilWarClanState clan in state.Clans.Values.Where(x => string.Equals(x.FactionId, faction.Id, StringComparison.OrdinalIgnoreCase)))
		{
			clan.Side = KingdomCivilWarSide.Middle;
			clan.FactionId = "";
			clan.SideSinceWeek = week; clan.SideSinceDay = CivilWarWorld.CurrentDay();
		}
		if (Clan.PlayerClan != null && !state.Clans.Values.Any(x => x.ClanId == Clan.PlayerClan.StringId && x.Side == KingdomCivilWarSide.Opposition) && state.PlayerSide == "opposition") state.PlayerSide = "";
		ClearWarMarks(faction.Id);
		if (!state.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) _openWarKingdoms.Remove(state.KingdomId);
		SaveSummary(state);
	}

	private void Dissolve(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, string reason, bool startCooldown = true)
	{
		AddHistory(state, week, reason);
		FinishFaction(kingdom, state, faction, week, tuning, null, 0, startCooldown);
	}

	private static void RefuseAndReschedule(KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning)
	{
		faction.UltimatumWeek = week + tuning.UltimatumDelayWeeks;
		faction.UltimatumDay = CivilWarWorld.CurrentDay() + tuning.UltimatumDelayWeeks * 7;
		faction.PlayerAnswerPending = false;
		faction.Stage = KingdomCivilWarStage.FactionFormed;
	}

	// ------------------------------------------------------------ player actions (inquiry / dialogue tags)

	// Joining the opposition means joining the speaker's faction, or the only pre-war faction if the speaker has none and the choice is unambiguous.
	internal bool TryJoinPlayer(Kingdom kingdom, Hero speaker, KingdomCivilWarSide side, out string message)
	{
		var state = Find(kingdom);
		var faction = FactionOfClan(state, speaker?.Clan);
		if (side == KingdomCivilWarSide.Opposition && !IsPreWar(faction))
		{
			// The speaker is not in a pre-war faction: only fall back when the choice is unambiguous, never guess the strongest one.
			var open = state?.Factions.Where(IsPreWar).Take(2).ToList();
			faction = open != null && open.Count == 1 ? open[0] : null;
			if (faction == null) { message = "无法确定要加入哪个派系，请先明确说明是哪个派系。"; return false; }
		}
		var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom?.StringId ?? "",
			FactionId = faction?.Id ?? "", Action = side == KingdomCivilWarSide.Crown ? CivilWarAction.JoinCrown : CivilWarAction.JoinOpposition }, Clan.PlayerClan);
		message = result.Message;
		return result.Status == CivilWarActionStatus.Applied;
	}

	internal bool TryRecruitClan(Hero recruiter, Clan target, Kingdom kingdom, out string message)
	{
		message = "";
		KingdomCivilWarKingdomState state = kingdom == null ? null : GetOrCreate(kingdom, CivilWarWorld.CurrentWeek());
		// One shared precondition check: the dialogue tag filter (ApplicableDialogueActions) uses the same method, so they cannot drift apart.
		string block = RecruitBlockReason(recruiter, target, kingdom, state);
		if (state == null || block.Length > 0) { message = block.Length > 0 ? block : "当前对象不满足派系招募条件。"; return false; }
		int week = CivilWarWorld.CurrentWeek();
		KingdomCivilWarClanState record = GetOrCreateClan(state, target, week);
		bool crown = CivilWarWorld.IsPlayerRuled(kingdom) && recruiter.Clan == kingdom.RulingClan;
		if (crown || state.Clans[recruiter.Clan.StringId].Side == KingdomCivilWarSide.Crown)
		{
			record.Side = KingdomCivilWarSide.Crown; record.FactionId = "";
			message = CivilWarWorld.ClanName(target) + "加入了王室派。";
		}
		else
		{
			KingdomCivilWarFactionState faction = FactionOfClan(state, recruiter.Clan);
			if (faction == null || !IsPreWar(faction)) { message = "招募者所在派系已无法招募。"; return false; }
			record.Side = KingdomCivilWarSide.Opposition; record.FactionId = faction.Id;
			AddGrievanceToFaction(state, faction, "demand_refused", 8f, 0f);
			message = CivilWarWorld.ClanName(target) + "加入了" + FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId)) + "。";
		}
		record.SideSinceWeek = week; record.SideSinceDay = CivilWarWorld.CurrentDay();
		record.LastRecruitWeek = week;
		SaveSummary(state);
		PublishPoliticalResult(kingdom, state, FactionOfClan(state, target), target, "recruit:" + target.StringId + ":" + CivilWarWorld.CurrentDay(), message);
		NotifyPoliticalChange(kingdom, "membership");
		Revision++;
		return true;
	}

	// Dialogue LEAVE tags: the speaker's own clan (SELF) or the player's clan (PLAYER) steps out of its current side.
	// Goes through Execute/Quote, so the same relation loss, 7-day exit lock and war lock as the panel apply.
	internal bool TryLeaveClan(Kingdom kingdom, Clan clan, out string message)
	{
		message = "";
		if (kingdom == null || clan == null) { message = "当前对象无法退出阵营。"; return false; }
		var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom.StringId, Action = CivilWarAction.Leave }, clan);
		message = result.Message;
		return result.Status == CivilWarActionStatus.Applied;
	}

	// Only the player's own faction can be detonated, through its leader.
	internal bool TryDetonate(Hero speaker, Kingdom kingdom, out string message)
	{
		message = "";
		KingdomCivilWarKingdomState state = Find(kingdom);
		KingdomCivilWarFactionState faction = speaker?.Clan == null ? null : FactionOfClan(state, speaker.Clan);
		if (kingdom == null || faction == null || !IsPreWar(faction) || faction.LeaderClanId != speaker.Clan.StringId || PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom)
			|| Clan.PlayerClan == null || FactionOfClan(state, Clan.PlayerClan) != faction)
		{ message = "当前不满足引爆内战的条件。"; return false; }
		var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom.StringId,
			FactionId = faction.Id, Action = CivilWarAction.Detonate }, Clan.PlayerClan, true);
		message = result.Message;
		return result.Status == CivilWarActionStatus.AwaitingKingdom;
	}

	// Dialogue ANSWER tags have no faction id: they answer the pending ultimatum of the speaker's faction, else the oldest.
	internal bool TryAnswerPlayerUltimatum(Kingdom kingdom, Hero speaker, bool accept, out string message)
	{
		KingdomCivilWarKingdomState state = Find(kingdom);
		KingdomCivilWarFactionState faction = speaker?.Clan == null ? null : FactionOfClan(state, speaker.Clan);
		if (faction == null || !faction.PlayerAnswerPending) faction = state?.Factions.FirstOrDefault(x => x.PlayerAnswerPending);
		return TryAnswerPlayerUltimatum(faction?.Id, accept, out message);
	}

	internal bool TryAnswerPlayerUltimatum(string factionId, bool accept, out string message)
	{
		message = "";
		KingdomCivilWarFactionState faction = FindFaction(factionId, out KingdomCivilWarKingdomState state);
		Kingdom kingdom = CivilWarWorld.FindKingdom(state?.KingdomId);
		if (faction == null || !faction.PlayerAnswerPending || !CivilWarWorld.IsPlayerRuled(kingdom)) { message = "当前没有待答复的最后通牒。"; return false; }
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		if (demand == null || leader == null) { message = "最后通牒已失效。"; return false; }
		if (accept)
		{
			var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom.StringId, FactionId = faction.Id, Action = CivilWarAction.Concede }, Clan.PlayerClan);
			message = result.Message; return result.Status == CivilWarActionStatus.Applied;
		}
		int week = CivilWarWorld.CurrentWeek();
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		int stability = Host.GetStability(kingdom);
		Dictionary<string, float> features = BuildFeatures(kingdom, state, faction, leader, stability);
		CivilWarUltimatumResult ruling = accept
			? new CivilWarUltimatumResult { Ruling = CivilWarRuling.Accept, Log = "player accept" }
			: CivilWarDecisions.RollRefusal(demand, features, faction.Refusals, tuning, RandomFloat, CivilWarAftermathRules.EscalationFactor(CurrentAftermath(state, week)));
		AddHistory(state, week, "玩家国王" + (accept ? "接受" : "拒绝") + FactionName(faction, demand) + "的最后通牒");
		bool carriedOut = ApplyRuling(kingdom, state, faction, demand, leader, week, tuning, HostStability, ruling);
		// A failed acceptance also removes the faction, so "still in the list" cannot tell the two apart.
		if (accept) message = carriedOut ? "已接受最后通牒，派系解散。" : "诉求尚未兑现，派系保留等待处理。";
		else message = faction.Stage == KingdomCivilWarStage.OpenWar ? "你拒绝了最后通牒，派系起兵了。" : faction.WaitingForOtherWar ? "你拒绝了最后通牒，派系决意起兵，正等待时机。" : "你拒绝了最后通牒，派系暂未起兵。";
		faction.Version++; SaveSummary(state); Revision++;
		return true;
	}

	private static readonly Action<Kingdom, int> HostStability = (k, d) => Host.AdjustStability(k, d, "civil_war");

	// ------------------------------------------------------------ panel (on demand, when the tab opens)

	internal CivilWarPanelKingdom BuildPlayerKingdomPanel()
	{
		CivilWarPanelKingdom panel = new CivilWarPanelKingdom();
		Kingdom kingdom = PlayerPoliticalKingdom();
		if (!DuelSettings.IsCivilWarFactionsEnabled()) { panel.EmptyText = "内战派系功能未启用"; return panel; }
		if (kingdom == null || kingdom.IsEliminated) { panel.EmptyText = "你尚未加入任何王国"; return panel; }
		panel.Available = true;
		panel.KingdomId = kingdom.StringId; panel.Revision = Revision;
		panel.Name = CivilWarWorld.KingdomName(kingdom);
		panel.Stability = Host.GetStability(kingdom);
		panel.StabilityTier = StabilityTierText(panel.Stability);
		KingdomCivilWarKingdomState state = Find(kingdom);
		List<KingdomCivilWarFactionState> factions = state?.Factions ?? new List<KingdomCivilWarFactionState>();
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		int day = CivilWarWorld.CurrentDay();
		if (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) panel.StageText = "玩家王国派系已在设置中关闭";
		else if (factions.Count == 0) panel.StageText = state != null && day < state.CooldownUntilDay ? "内战余波平息中（第 " + state.CooldownUntilDay + " 天前不会成派）" : "尚无派系成形";
		else if (factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) panel.StageText = factions.Count(x => x.Stage == KingdomCivilWarStage.OpenWar) + " 派正在内战";
		else if (factions.Any(x => x.Stage == KingdomCivilWarStage.Ultimatum)) panel.StageText = factions.Count(x => x.Stage == KingdomCivilWarStage.Ultimatum) + " 派发出最后通牒";
		else panel.StageText = factions.Count + " 个反对派已成立";
		int week = CivilWarWorld.CurrentWeek();
		// Strength is read once per clan; every share on the panel (bar, columns, rows) uses this one total.
		Dictionary<string, float> strength = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		foreach (Clan clan in CivilWarWorld.LandedClans(kingdom)) strength[clan.StringId ?? ""] = CivilWarWorld.Strength(clan);
		foreach (KingdomCivilWarFactionState f in factions.Where(x => x.Stage == KingdomCivilWarStage.OpenWar))
			foreach (string id in f.WarClanIds ?? new List<string>())
				if (!strength.ContainsKey(id)) strength[id] = CivilWarWorld.Strength(CivilWarWorld.FindClan(id));
		float total = Math.Max(1f, strength.Values.Sum());
		Func<IEnumerable<string>, int> share = ids => (int)Math.Round(100f * ids.Distinct(StringComparer.OrdinalIgnoreCase).Sum(cid => strength.TryGetValue(cid ?? "", out float v) ? v : 0f) / total);
		ResolvePlayerRole(kingdom, state, panel);
		foreach (KingdomCivilWarFactionState faction in factions) panel.Factions.Add(ToPanelFaction(kingdom, state, faction, week, panel, share));
		panel.OppositionCount = panel.Factions.Sum(x => x.MemberCount);
		// Everyone outside a faction: crown side (king first) and undecided clans.
		List<string> crownIds = new List<string>(), middleIds = new List<string>();
		foreach (Clan clan in CivilWarWorld.LandedClans(kingdom))
		{
			KingdomCivilWarClanState record = null;
			state?.Clans.TryGetValue(clan.StringId ?? "", out record);
			if (record != null && record.Side == KingdomCivilWarSide.Opposition && factions.Any(x => x.Id == record.FactionId)) continue;
			bool king = clan == kingdom.RulingClan;
			bool crown = king || record?.Side == KingdomCivilWarSide.Crown;
			(crown ? crownIds : middleIds).Add(clan.StringId ?? "");
			(crown ? panel.Crown : panel.Middle).Add(PanelClan(kingdom, clan, record, king ? "国王" : crown ? "王室阵营" : "未表态", king, share(new[] { clan.StringId })));
		}
		panel.CrownCount = panel.Crown.Count;
		panel.MiddleCount = panel.Middle.Count;
		panel.CrownPowerPercent = share(crownIds);
		panel.MiddlePowerPercent = share(middleIds);
		panel.Crown = panel.Crown.OrderByDescending(x => x.IsLeader).ThenByDescending(x => x.IsPlayer).ThenByDescending(x => x.GrievanceValue).ToList();
		panel.Middle = panel.Middle.OrderByDescending(x => x.IsPlayer).ThenByDescending(x => x.GrievanceValue).ToList();
		// Formation view: the same candidate pool TryFormFaction uses (vassals outside any faction).
		panel.Threshold = tuning.DiscontentThreshold;
		panel.MaxFactions = Math.Max(1, Math.Min(4, tuning.MaxFactions));
		Clan top = null;
		foreach (Clan clan in CivilWarWorld.Vassals(kingdom))
		{
			if (FactionOfClan(state, clan) != null || state != null && state.Clans.TryGetValue(clan.StringId ?? "", out var candidateRecord) && candidateRecord.Side != KingdomCivilWarSide.Middle) continue;
			KingdomCivilWarClanState record = null;
			state?.Clans.TryGetValue(clan.StringId ?? "", out record);
			float grievance = TotalGrievance(record);
			if (top == null || grievance > panel.TopGrievance) { top = clan; panel.TopGrievance = grievance; }
		}
		panel.TopGrievanceClan = top == null ? "" : CivilWarWorld.ClanName(top);
		int cooldownDay = state?.CooldownUntilDay ?? 0;
		panel.Conditions.Add(Condition("正式封臣资格", top != null ? "已满足" : "无合格家族", top != null));
		panel.Conditions.Add(Condition("本国派系名额", factions.Count + " / " + panel.MaxFactions, factions.Count < panel.MaxFactions));
		panel.Conditions.Add(Condition("不满门槛 " + panel.Threshold, panel.TopGrievance >= panel.Threshold ? "已达到" : "未达到", panel.TopGrievance >= panel.Threshold));
		float weariness = Weariness(state);
		panel.Conditions.Add(Condition("或 厌战度 " + CivilWarWearinessRules.FormationThreshold.ToString("0"), weariness.ToString("0") + (CivilWarWearinessRules.MeetsFormation(weariness) ? " 已达到" : " 未达到"), CivilWarWearinessRules.MeetsFormation(weariness)));
		panel.Conditions.Add(Condition("战后冷却", day < cooldownDay ? "至第 " + cooldownDay + " 天" : "无", day >= cooldownDay));
		if (CivilWarWorld.IsPlayerRuled(kingdom)) panel.Conditions.Add(Condition("玩家王国派系", DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed() ? "已开启" : "设置已关闭", DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()));
		List<KingdomCivilWarHistoryEntry> history = state?.History ?? new List<KingdomCivilWarHistoryEntry>();
		for (int i = history.Count - 1; i >= 0 && panel.Chronicle.Count < 3; i--)
			if (!string.IsNullOrWhiteSpace(history[i]?.Text)) panel.Chronicle.Add("第 " + history[i].Week + " 周  ·  " + CivilWarWorld.Limit(history[i].Text, 46));
		PlayerPanelFigures(kingdom, state, panel, day);
		return panel;
	}

	private static CivilWarPanelCondition Condition(string name, string value, bool met) => new CivilWarPanelCondition { Name = name, Value = value, Met = met };

	private static void ResolvePlayerRole(Kingdom kingdom, KingdomCivilWarKingdomState state, CivilWarPanelKingdom panel)
	{
		Clan player = Clan.PlayerClan;
		panel.PlayerClanName = CivilWarWorld.ClanName(player);
		string id = player?.StringId ?? "";
		KingdomCivilWarFactionState own = state?.Factions.FirstOrDefault(f => f.Stage == KingdomCivilWarStage.OpenWar && (f.WarClanIds?.Contains(id) == true || f.LeaderClanId == id)) ?? FactionOfClan(state, player);
		KingdomCivilWarClanState record = null;
		state?.Clans.TryGetValue(id, out record);
		if (player != null && kingdom.RulingClan == player) panel.Role = CivilWarPanelRole.King;
		else if (own != null) { panel.Role = string.Equals(own.LeaderClanId, id, StringComparison.OrdinalIgnoreCase) ? CivilWarPanelRole.Leader : CivilWarPanelRole.Member; panel.PlayerFactionId = own.Id; }
		else panel.Role = record?.Side == KingdomCivilWarSide.Crown ? CivilWarPanelRole.Crown : CivilWarPanelRole.Middle;
		string tag = own == null ? "" : FactionTag(CivilWarCatalog.FindDemand(own.DemandId));
		if (panel.Role == CivilWarPanelRole.King) panel.Identity = "你是国王  ·  王室阵营";
		else if (!PoliticalClan(player, kingdom) && own == null) panel.Identity = panel.PlayerClanName + "  ·  非正式封臣，不能参与派系";
		else panel.Identity = "封臣  ·  " + panel.PlayerClanName + "  ·  " + RoleLabel(panel.Role, tag);
	}

	internal static string RoleLabel(CivilWarPanelRole role, string factionTag)
	{
		switch (role)
		{
			case CivilWarPanelRole.King: return "国王";
			case CivilWarPanelRole.Crown: return "王室阵营";
			case CivilWarPanelRole.Member: return factionTag + "成员";
			case CivilWarPanelRole.Leader: return factionTag + "领袖";
			default: return "未表态";
		}
	}

	private void PlayerPanelFigures(Kingdom kingdom, KingdomCivilWarKingdomState state, CivilWarPanelKingdom panel, int day)
	{
		Clan player = Clan.PlayerClan;
		KingdomCivilWarClanState record = null;
		state?.Clans.TryGetValue(player?.StringId ?? "", out record);
		panel.PlayerGrievance = (int)Math.Round(TotalGrievance(record));
		panel.PlayerRelationToKing = CivilWarWorld.Relation(player?.Leader, kingdom.Leader);
		KingdomCivilWarFactionState own = state?.Factions.FirstOrDefault(x => x.Id == panel.PlayerFactionId);
		panel.PlayerRelationToLeader = own == null ? 0 : CivilWarWorld.Relation(player?.Leader, CivilWarWorld.FindClan(own.LeaderClanId)?.Leader);
		int exitUntil = 0;
		if (player != null) _storage.ClanExitUntilDay.TryGetValue(player.StringId ?? "", out exitUntil);
		bool locked = state?.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar) == true;
		string exit = "与领袖 −" + CivilWarPoliticalRules.ExitLeaderRelationLoss + "、成员 −" + CivilWarPoliticalRules.ExitMemberRelationLoss + "，" + CivilWarPoliticalRules.ExitDays + " 天内不能加入或建立派系。";
		switch (panel.Role)
		{
			case CivilWarPanelRole.King:
				panel.ActionHint = panel.Factions.Count > 0 ? "压制、谈判、强制解散共用 " + CivilWarPoliticalRules.ActionDays + " 天冷却；妥协不受限，但会兑现诉求。" : "反对派成立后开放压制、谈判、妥协与强制解散。";
				return;
			case CivilWarPanelRole.Crown:
				panel.ActionHint = "退出王室阵营：与国王 −" + CivilWarPoliticalRules.ExitLeaderRelationLoss + "、其余王室家族 −" + CivilWarPoliticalRules.ExitMemberRelationLoss + "，" + CivilWarPoliticalRules.ExitDays + " 天内保持中立。";
				break;
			case CivilWarPanelRole.Member:
				panel.ActionHint = "起兵由领袖决定，提议被拒后 " + CivilWarPoliticalRules.ActionDays + " 天内不能再提。退出：" + exit;
				break;
			case CivilWarPanelRole.Leader:
				panel.ActionHint = "不会自动起兵；手动起兵须本家族军力占比达到 " + DuelSettings.BuildCivilWarTuning().PlayerDetonationStrengthPercent + "%（当前 " + PlayerStrengthPercent(kingdom, player).ToString("0.0") + "%）。可主动解散或改建，仍受战后冷却。";
				break;
			default:
				panel.ActionHint = day < exitUntil ? "退出冷却至第 " + exitUntil + " 天，期间不能加入或建立派系。" : "加入后改投须先退出：" + exit;
				break;
		}
		if (locked) panel.ActionHint = "建国请求或内战期间，成员去留锁定。";
	}

	private static CivilWarPanelClan PanelClan(Kingdom kingdom, Clan clan, KingdomCivilWarClanState record, string stance, bool leader, int powerPercent)
	{
		float grievance = TotalGrievance(record);
		int relation = CivilWarWorld.Relation(clan?.Leader, kingdom?.Leader);
		bool king = clan != null && clan == kingdom?.RulingClan;
		bool player = clan != null && clan == Clan.PlayerClan;
		return new CivilWarPanelClan
		{
			Name = CivilWarWorld.ClanName(clan) + (player ? "（你）" : ""),
			Stance = stance,
			Reason = TopSourceName(record),
			Grievance = (int)Math.Round(grievance),
			GrievanceValue = grievance,
			RelationToKing = relation,
			RelationText = king ? "" : relation > 0 ? "+" + relation : relation < 0 ? "−" + (-relation) : "0",
			PowerPercent = powerPercent,
			IsLeader = leader,
			IsPlayer = player
		};
	}

	private CivilWarPanelFaction ToPanelFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarPanelKingdom kingdomPanel, Func<IEnumerable<string>, int> share)
	{
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		bool paused = IsPausedByOtherWar(state, faction, DuelSettings.BuildCivilWarTuning());
		string tag = FactionTag(demand);
		string demandWord = tag.EndsWith("派", StringComparison.Ordinal) ? tag.Substring(0, tag.Length - 1) : tag;
		bool playerFaction = faction.Id == kingdomPanel.PlayerFactionId;
		bool playerLeader = playerFaction && kingdomPanel.Role == CivilWarPanelRole.Leader;
		CivilWarPanelFaction panel = new CivilWarPanelFaction
		{
			Id = faction.Id,
			Tag = tag,
			DemandTag = demandWord + "诉求",
			DemandId = faction.DemandId, TargetId = faction.TargetId,
			Name = FactionName(faction, demand),
			ShortName = CivilWarWorld.ClanName(leader) + tag,
			Color = demand?.Color ?? "#6B3A78FF",
			LeaderLine = "领袖  " + CivilWarWorld.ClanName(leader) + (leader != null && leader == Clan.PlayerClan ? "（你）" : "") + "   ·   与国王关系 " + SignedRelation(leader, kingdom),
			DemandTitle = demand == null ? "诉求已失效" : "要求" + FormatDemand(demand, faction.TargetName),
			Grievance = (int)Math.Round(CivilWarRules.Clamp(faction.Grievance, 0f, 100f)),
			Stage = FactionStageText(faction, week, paused, CurrentAftermath(state, week)),
			Refusals = faction.Refusals,
			RefusalUnit = faction.Defers > 0 ? "次 · 拖延 " + faction.Defers : "次",
			IsPlayerFaction = playerFaction,
			IsPlayerLeader = playerLeader,
			HasPendingResponse = faction.PendingResponse != null,
			PendingDissolve = faction.PendingResponse?.Dissolve == true,
			PendingGold = faction.PendingResponse?.Gold ?? 0,
			PendingInfluence = faction.PendingResponse?.Influence ?? 0
		};
		bool war = faction.Stage == KingdomCivilWarStage.OpenWar;
		List<string> memberIds = (war ? faction.WarClanIds ?? new List<string>() : Members(state, faction).Select(x => x.ClanId)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		panel.Fortifications = war && !string.IsNullOrWhiteSpace(faction.RebelKingdomId)
			? CivilWarWorld.FortificationCount(CivilWarWorld.FindKingdom(faction.RebelKingdomId))
			: memberIds.Sum(id => CivilWarWorld.FortificationCount(CivilWarWorld.FindClan(id)));
		panel.PowerPercent = share(memberIds);
		DescribeDeadline(faction, panel, paused, playerLeader);
		DescribeSteps(faction, panel, paused);
		panel.Note = DescribeNote(faction, demand, kingdomPanel.Role, playerFaction);
		if (IsPlayerLed(faction) && !war && faction.PendingResponse == null) panel.Note = "你领导的派系不会自动起兵；可更改诉求并重评成员，起兵须手动确认且满足实力门槛。";
		foreach (string id in memberIds)
		{
			Clan clan = CivilWarWorld.FindClan(id);
			if (clan == null || clan.IsEliminated) continue;
			KingdomCivilWarClanState record = null;
			state?.Clans.TryGetValue(id, out record);
			bool isLeader = string.Equals(id, faction.LeaderClanId, StringComparison.OrdinalIgnoreCase);
			CivilWarPanelClan row = PanelClan(kingdom, clan, record, isLeader ? "派系领袖" : "支持" + demandWord, isLeader, share(new[] { id }));
			if (row.IsPlayer && !isLeader && record != null && record.SideSinceDay >= 0)
			{
				int ago = CivilWarWorld.CurrentDay() - record.SideSinceDay;
				row.Stance += ago <= 0 ? " · 今日加入" : " · " + ago + " 日前加入";
			}
			panel.Members.Add(row);
		}
		panel.MemberCount = panel.Members.Count;
		panel.Members = panel.Members.OrderByDescending(x => x.IsLeader).ThenByDescending(x => x.IsPlayer).ThenByDescending(x => x.GrievanceValue).ToList();
		return panel;
	}

	// Right-hand block of the dossier header: the one deadline that matters now.
	private static void DescribeDeadline(KingdomCivilWarFactionState f, CivilWarPanelFaction p, bool paused, bool playerLeader)
	{
		int day = CivilWarWorld.CurrentDay();
		p.DeadlineUnit = "天";
		if (f.PendingResponse != null)
		{
			p.DeadlineLabel = f.PendingResponse.Dissolve ? "解散令" : "王室来函";
			p.DeadlineValue = Math.Max(0, f.PendingResponse.DeadlineDay - day).ToString();
			p.DeadlineNote = playerLeader ? "待你答复 · 超时视为" + (f.PendingResponse.Dissolve ? "抗命" : "拒绝") : "等待派系领袖答复";
			p.DeadlineUrgent = true;
		}
		else if (!string.IsNullOrWhiteSpace(f.ResolutionError))
		{
			p.DeadlineLabel = "结算"; p.DeadlineValue = "异常"; p.DeadlineUnit = ""; p.DeadlineNote = f.ResolutionNeedsReview ? "已保留状态，需检查日志" : "次日重试"; p.DeadlineUrgent = true;
		}
		else if (f.Stage == KingdomCivilWarStage.OpenWar)
		{
			bool founding = string.IsNullOrWhiteSpace(f.RebelKingdomId);
			p.DeadlineLabel = founding ? "起兵" : "内战";
			p.DeadlineValue = founding ? "建国中" : "第 " + Math.Max(1, CivilWarWorld.CurrentWeek() - f.WarStartWeek + 1);
			p.DeadlineUnit = founding ? "" : "周";
			p.DeadlineNote = founding ? "等待叛军王国建立" : "叛军王国已与王室开战";
			p.DeadlineUrgent = true;
		}
		else if (f.Stage == KingdomCivilWarStage.Ultimatum)
		{
			p.DeadlineLabel = "最后通牒";
			p.DeadlineValue = f.PlayerAnswerPending ? Math.Max(0, f.AnswerDeadlineDay - day).ToString() : "裁决中";
			p.DeadlineUnit = f.PlayerAnswerPending ? "天" : "";
			p.DeadlineNote = f.PlayerAnswerPending ? "期限内等待国王答复" : "国王正在裁决";
			p.DeadlineUrgent = true;
		}
		else if (f.WaitingForOtherWar || paused)
		{
			p.DeadlineLabel = f.WaitingForOtherWar ? "决意起兵" : "暂停"; p.DeadlineValue = "待命"; p.DeadlineUnit = "";
			p.DeadlineNote = f.WaitingForOtherWar ? "等待国内战事结束后起兵" : "国内战事期间按兵不动";
		}
		else
		{
			int left = f.UltimatumDay - day;
			p.DeadlineLabel = "递交通牒";
			p.DeadlineValue = left > 0 ? left.ToString() : "即将";
			p.DeadlineUnit = left > 0 ? "天" : "";
			p.DeadlineNote = left > 0 ? "之后向国王递交最后通牒" : "即将递交最后通牒";
		}
	}

	// Four-step demand progress shown under the dossier header.
	private static void DescribeSteps(KingdomCivilWarFactionState f, CivilWarPanelFaction p, bool paused)
	{
		int day = CivilWarWorld.CurrentDay();
		bool war = f.Stage == KingdomCivilWarStage.OpenWar;
		bool ultimatum = f.Stage == KingdomCivilWarStage.Ultimatum;
		bool pending = f.PendingResponse != null;
		bool delivered = ultimatum || (war && f.Refusals > 0);
		p.Steps.Add(Step("成立", "第 " + f.CreatedWeek + " 周", 1));
		if (delivered) p.Steps.Add(Step("递交通牒", f.UltimatumDay >= 0 ? "第 " + f.UltimatumDay + " 天" : "已递交", 1));
		else if (war || pending) p.Steps.Add(Step("递交通牒", war ? "未经通牒" : "王室先行回应", 0));
		else
		{
			int left = f.UltimatumDay - day;
			string note = f.WaitingForOtherWar || paused ? "暂停" : left > 0 ? "剩余 " + left + " 天" : "即将递交";
			p.Steps.Add(Step("递交通牒", f.Refusals > 0 ? "被拒 " + f.Refusals + " 次后重提 · " + note : note, 2));
		}
		if (pending) p.Steps.Add(Step(f.PendingResponse.Dissolve ? "解散令" : "王室谈判", "剩余 " + Math.Max(0, f.PendingResponse.DeadlineDay - day) + " 天", 2));
		else if (ultimatum) p.Steps.Add(Step("国王答复", f.PlayerAnswerPending ? "剩余 " + Math.Max(0, f.AnswerDeadlineDay - day) + " 天" : "裁决中", 2));
		else if (war) p.Steps.Add(Step("国王答复", f.Refusals > 0 ? "已拒绝" : "未经答复", 1));
		else p.Steps.Add(Step("国王答复", f.Refusals > 0 ? "前次被拒" : "", 0));
		if (war) p.Steps.Add(Step("起兵", string.IsNullOrWhiteSpace(f.RebelKingdomId) ? "叛军建国中" : "内战第 " + Math.Max(1, CivilWarWorld.CurrentWeek() - f.WarStartWeek + 1) + " 周", 2));
		else if (IsPlayerLed(f)) p.Steps.Add(Step("手动起兵", "等待你确认", 0));
		else p.Steps.Add(Step("可能起兵", f.WaitingForOtherWar ? "已决意，待战事结束" : "拒绝后判定", 0));
	}

	private static CivilWarPanelStep Step(string name, string note, int state) => new CivilWarPanelStep { Name = name, Note = note, State = state };

	// One line under the demand title, written for the viewer's role.
	private static string DescribeNote(KingdomCivilWarFactionState f, CivilWarDemandDef demand, CivilWarPanelRole role, bool playerFaction)
	{
		CivilWarWarGoal goal = f.WarGoal == 0 ? CivilWarCatalog.EffectiveWarGoal(demand) : (CivilWarWarGoal)f.WarGoal;
		string goalName = goal == CivilWarWarGoal.Usurp ? "夺位" : goal == CivilWarWarGoal.Secede ? "独立" : "逼宫";
		string text;
		if (f.Stage == KingdomCivilWarStage.OpenWar) text = (string.IsNullOrWhiteSpace(f.RebelKingdomId) ? "叛军正在建国。" : "叛军王国已与王室开战。") + "战争目标：" + goalName + "。";
		else if (f.PendingResponse != null && playerFaction && role == CivilWarPanelRole.Leader)
			text = f.PendingResponse.Dissolve ? "国王下令解散派系：服从则解散，抗命则起兵。"
				: "国王提出 " + (f.PendingResponse.Gold > 0 ? f.PendingResponse.Gold + " 金币" : f.PendingResponse.Influence + " 影响力") + " 补偿，换取撤回诉求并解散派系；接受后补偿归你的家族。";
		else if (f.PendingResponse != null) text = f.PendingResponse.Dissolve ? "国王已下令解散，等待派系领袖服从或抗命。" : "国王提出补偿换取撤回诉求，等待派系领袖答复。";
		else if (role == CivilWarPanelRole.King) text = (goal == CivilWarWarGoal.Usurp ? "妥协即退位，由派系领袖继位" : "妥协将兑现该诉求") + "；拒绝后，派系按概率决定是否起兵。";
		else if (role == CivilWarPanelRole.Crown) text = "若派系起兵，王室阵营家族随国王应战。";
		else if (playerFaction) text = "国王若拒绝，派系按概率决定是否起兵；建国请求或内战开始后，成员去留锁定。";
		else text = "国王若妥协将兑现诉求；若拒绝，派系按概率决定是否起兵。";
		if (demand != null && demand.EscalationScale < 1f && f.Stage != KingdomCivilWarStage.OpenWar) text += "被拒后起兵概率较低（×" + demand.EscalationScale.ToString("0.0") + "）。";
		return text;
	}

	private static string FactionStageText(KingdomCivilWarFactionState faction, int week, bool paused, CivilWarAftermath aftermath)
	{
		if (faction.PendingResponse != null) return "待派系回应 · 剩 " + Math.Max(0, faction.PendingResponse.DeadlineDay - CivilWarWorld.CurrentDay()) + " 天";
		switch (faction.Stage)
		{
			case KingdomCivilWarStage.OpenWar:
				if (!string.IsNullOrWhiteSpace(faction.ResolutionError)) return faction.ResolutionNeedsReview ? "结算异常 · 已保留状态，需检查日志" : "结算未完成 · 次日重试";
				return string.IsNullOrWhiteSpace(faction.RebelKingdomId) ? "起兵中 · 叛军王国建立中" : "内战 · 第 " + Math.Max(1, week - faction.WarStartWeek + 1) + " 周";
			case KingdomCivilWarStage.Ultimatum:
				return faction.PlayerAnswerPending ? "最后通牒 · 待国王答复（剩 " + Math.Max(0, faction.AnswerDeadlineDay - CivilWarWorld.CurrentDay()) + " 天）" : "最后通牒 · 等待裁决";
			default:
				if (faction.WaitingForOtherWar) return "决意起兵 · 等待国内战事结束";
				if (paused) return "暂停 · 国内战事期间按兵不动";
				int left = faction.UltimatumDay - CivilWarWorld.CurrentDay();
				if (left > 0 && aftermath == CivilWarAftermath.Suppressed) return "受平叛震慑 · " + left + " 天后提出通牒";
				if (left > 0 && aftermath == CivilWarAftermath.Emboldened) return "受叛军得胜鼓舞 · " + left + " 天后提出通牒";
				return left > 0 ? "已成形 · " + left + " 天后提出通牒" : "已成形 · 即将提出通牒";
		}
	}

	private static string RulingText(CivilWarRuling ruling, bool escalate)
	{
		switch (ruling)
		{
			case CivilWarRuling.Accept: return "国王接受";
			case CivilWarRuling.Defer: return "国王拖延";
			default: return escalate ? "国王拒绝，派系起兵" : "国王拒绝";
		}
	}

	private static string FactionTag(CivilWarDemandDef demand)
	{
		string name = demand?.FactionNameFormat ?? "";
		int index = name.IndexOf("为首的", StringComparison.Ordinal);
		return index >= 0 ? name.Substring(index + 3) : "反对派";
	}

	private static string FactionName(KingdomCivilWarFactionState faction, CivilWarDemandDef demand)
	{
		string leader = CivilWarWorld.ClanName(CivilWarWorld.FindClan(faction?.LeaderClanId));
		return (demand?.FactionNameFormat ?? "{leader}为首的反对派").Replace("{leader}", leader);
	}

	private static string SignedRelation(Clan clan, Kingdom kingdom)
	{
		int relation = CivilWarWorld.Relation(clan?.Leader, kingdom?.Leader);
		return relation > 0 ? "+" + relation : relation.ToString();
	}

	private static string StabilityTierText(int value)
	{
		switch (KingdomStabilityPolicy.GetKingdomStabilityTier(value))
		{
			case KingdomStabilityPolicy.KingdomStabilityTier.ExtremelyHigh: return "极高";
			case KingdomStabilityPolicy.KingdomStabilityTier.High: return "高";
			case KingdomStabilityPolicy.KingdomStabilityTier.FairlyHigh: return "较高";
			case KingdomStabilityPolicy.KingdomStabilityTier.Average: return "一般";
			case KingdomStabilityPolicy.KingdomStabilityTier.Poor: return "较差";
			case KingdomStabilityPolicy.KingdomStabilityTier.VeryPoor: return "很差";
			default: return "极差";
		}
	}

	private static string TopSourceName(KingdomCivilWarClanState record)
	{
		if (record?.Grievance == null || record.Grievance.Count == 0) return "";
		KeyValuePair<string, float> top = record.Grievance.OrderByDescending(x => x.Value).First();
		return top.Value < 1f ? "" : CivilWarCatalog.FindSource(top.Key)?.Name ?? "";
	}

	// ------------------------------------------------------------ membership and features

	// How well a clan's grievance mix matches a faction's demand (same affinity table as demand picking).
	private static float DemandAffinity(CivilWarDemandDef demand, KingdomCivilWarClanState record)
	{
		if (demand == null || record?.Grievance == null) return 0f;
		float score = demand.BaseWeight;
		foreach (KeyValuePair<string, float> affinity in demand.Affinity)
			if (record.Grievance.TryGetValue(affinity.Key, out float points)) score += affinity.Value * Math.Max(0f, points) / 20f;
		return score;
	}

	private static float RelationAffinity(Clan clan, KingdomCivilWarFactionState faction)
	{
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		return Math.Max(0f, CivilWarWorld.Relation(clan?.Leader, leader?.Leader) / 100f);
	}

	private Dictionary<string, float> BuildFeatures(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, Clan clan, int stability)
	{
		Clan leader = CivilWarWorld.FindClan(faction == null ? "" : faction.LeaderClanId) ?? clan;
		Hero king = kingdom?.Leader;
		float total = state?.CachedGrievance ?? 0f;
		float clanGrievance = clan == null || state == null ? 0f : TotalGrievance(GetOrCreateClan(state, clan, CivilWarWorld.CurrentWeek())) / 100f;
		float relationKing = CivilWarWorld.Relation(clan?.Leader, king) / 100f;
		float relationLeader = clan == leader ? 1f : CivilWarWorld.Relation(clan?.Leader, leader?.Leader) / 100f;
		return new Dictionary<string, float>(StringComparer.Ordinal)
		{
			[CivilWarFeature.KingMercy] = CivilWarWorld.Trait(king, DefaultTraits.Mercy),
			[CivilWarFeature.KingAuthoritarian] = CivilWarWorld.Trait(king, DefaultTraits.Authoritarian),
			[CivilWarFeature.KingEgalitarian] = CivilWarWorld.Trait(king, DefaultTraits.Egalitarian),
			[CivilWarFeature.KingValor] = CivilWarWorld.Trait(king, DefaultTraits.Valor),
			[CivilWarFeature.KingGenerosity] = CivilWarWorld.Trait(king, DefaultTraits.Generosity),
			[CivilWarFeature.KingHonor] = CivilWarWorld.Trait(king, DefaultTraits.Honor),
			[CivilWarFeature.LeaderValor] = CivilWarWorld.Trait(leader?.Leader, DefaultTraits.Valor),
			[CivilWarFeature.LeaderCalculating] = CivilWarWorld.Trait(leader?.Leader, DefaultTraits.Calculating),
			[CivilWarFeature.LeaderHonor] = CivilWarWorld.Trait(leader?.Leader, DefaultTraits.Honor),
			[CivilWarFeature.LeaderMercy] = CivilWarWorld.Trait(leader?.Leader, DefaultTraits.Mercy),
			[CivilWarFeature.Instability] = CivilWarRules.Clamp((50f - stability) / 50f, 0f, 1f),
			[CivilWarFeature.WarLoad] = CivilWarRules.Clamp(CivilWarWorld.KingdomWarCount(kingdom) / 5f, 0f, 1f),
			[CivilWarFeature.KingdomGrievance] = CivilWarRules.Clamp(total / 100f, 0f, 1f),
			[CivilWarFeature.Weariness] = Weariness(state) / CivilWarWearinessRules.Max,
			[CivilWarFeature.FactionPower] = faction == null ? 0f : IsPreWar(faction) ? FactionPower(kingdom, state, faction) : faction.LastFactionPower,
			[CivilWarFeature.FactionGrievance] = faction == null ? 0f : CivilWarRules.Clamp(faction.Grievance / 100f, 0f, 1f),
			[CivilWarFeature.Refusals] = faction == null ? 0f : CivilWarRules.Clamp(faction.Refusals / 4f, 0f, 1f),
			[CivilWarFeature.ClanGrievance] = clanGrievance,
			[CivilWarFeature.RelationGap] = CivilWarRules.Clamp(relationLeader - relationKing, -1f, 1f),
			[CivilWarFeature.RelationToKing] = CivilWarRules.Clamp(relationKing, -1f, 1f),
			[CivilWarFeature.BloodShy] = CivilWarRules.Clamp(Math.Max(CivilWarWorld.Trait(clan?.Leader, DefaultTraits.Honor), CivilWarWorld.Trait(clan?.Leader, DefaultTraits.Mercy)), 0f, 1f),
			[CivilWarFeature.One] = 1f
		};
	}

	private static bool IsDemandEligible(CivilWarDemandDef demand, Kingdom kingdom, Clan leader, KingdomCivilWarKingdomState state)
	{
		if (demand == null || leader == null || CivilWarWorld.FortificationCount(leader) < demand.MinFortifications || CivilWarWorld.Relation(leader.Leader, kingdom.Leader) > demand.MaxLeaderRelationToKing) return false;
		return demand.Target == CivilWarDemandTarget.None || !string.IsNullOrWhiteSpace(ResolveTargetId(demand, kingdom, state));
	}

	private static string ResolveTargetId(CivilWarDemandDef demand, Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		if (demand == null || kingdom == null) return "";
		if (demand.Target == CivilWarDemandTarget.EnemyKingdom) return StrongestForeignEnemy(kingdom, state)?.StringId ?? "";
		if (demand.Target == CivilWarDemandTarget.ImposedPolicy)
			return kingdom.ActivePolicies.Any(x => x.StringId == state?.LastImposedPolicyId) ? state.LastImposedPolicyId : "";
		return "";
	}

	// Peace/war demands target a real foreign enemy, never one of this kingdom's own civil-war rebel kingdoms.
	private static Kingdom StrongestForeignEnemy(Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		HashSet<string> rebels = new HashSet<string>((state?.Factions ?? new List<KingdomCivilWarFactionState>()).Select(x => x.RebelKingdomId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		try
		{
			return kingdom?.FactionsAtWarWith?.OfType<Kingdom>()
				.Where(x => x != null && !x.IsEliminated && !rebels.Contains(x.StringId ?? ""))
				.OrderByDescending(CivilWarWorld.Strength).FirstOrDefault();
		}
		catch { return null; }
	}

	private static string ResolveTargetName(CivilWarDemandDef demand, Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		if (demand == null) return "";
		if (demand.Target == CivilWarDemandTarget.EnemyKingdom) return CivilWarWorld.KingdomName(StrongestForeignEnemy(kingdom, state));
		if (demand.Target == CivilWarDemandTarget.ImposedPolicy) return "现行政策";
		return "";
	}

	private static string FormatDemand(CivilWarDemandDef demand, string target)
	{
		return (demand == null ? "未命名诉求" : demand.Text).Replace("{target}", string.IsNullOrWhiteSpace(target) ? "目标" : target);
	}

	private CivilWarEffectContext BuildContext(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, CivilWarDemandDef demand, Clan leader, int week)
	{
		return new CivilWarEffectContext { Kingdom = kingdom, State = state, Faction = faction, Demand = demand, LeaderClan = leader, RebelKingdom = CivilWarWorld.FindKingdom(faction?.RebelKingdomId), Week = week, Host = Host };
	}

	// ------------------------------------------------------------ storage helpers

	private KingdomCivilWarKingdomState GetOrCreate(Kingdom kingdom, int week)
	{
		string id = kingdom.StringId ?? "";
		KingdomCivilWarKingdomState state;
		if (!_storage.Kingdoms.TryGetValue(id, out state) || state == null) { state = new KingdomCivilWarKingdomState { KingdomId = id, StageWeek = week, LastGrievanceDecayDay = CivilWarWorld.CurrentDay() }; _storage.Kingdoms[id] = state; }
		if (state.Clans == null) state.Clans = new Dictionary<string, KingdomCivilWarClanState>(StringComparer.OrdinalIgnoreCase);
		if (state.Factions == null) state.Factions = new List<KingdomCivilWarFactionState>();
		if (state.History == null) state.History = new List<KingdomCivilWarHistoryEntry>();
		return state;
	}

	private static KingdomCivilWarClanState GetOrCreateClan(KingdomCivilWarKingdomState state, Clan clan, int week)
	{
		if (state == null || clan == null || string.IsNullOrWhiteSpace(clan.StringId)) return new KingdomCivilWarClanState();
		KingdomCivilWarClanState record;
		if (!state.Clans.TryGetValue(clan.StringId, out record) || record == null) { record = new KingdomCivilWarClanState { ClanId = clan.StringId, SideSinceWeek = week, SideSinceDay = CivilWarWorld.CurrentDay() }; state.Clans[clan.StringId] = record; }
		if (record.Grievance == null) record.Grievance = new Dictionary<string, float>(StringComparer.Ordinal);
		if (record.FactionId == null) record.FactionId = "";
		record.Owner = state;
		return record;
	}

	private static void AddPoints(KingdomCivilWarClanState record, string sourceId, float points)
	{
		if (record?.Grievance == null || string.IsNullOrWhiteSpace(sourceId) || points <= 0f) return;
		float current;
		if (!record.Grievance.TryGetValue(sourceId, out current)) current = 0f;
		record.Grievance[sourceId] = CivilWarRules.Clamp(current + points, 0f, MaxClanGrievance);
		float total = TotalGrievance(record), delta = total - record.CachedGrievance;
		record.CachedGrievance = total;
		if (record.Owner != null) record.Owner.CachedGrievance += delta;
		if (record.Owner != null) record.Owner.LastMaxGrievance = Math.Max(record.Owner.LastMaxGrievance, total);
		var faction = record.CachedFaction;
		if (faction != null)
		{
			faction.GrievanceSum += delta;
			faction.Grievance = faction.GrievanceSum / Math.Max(1, faction.CachedMemberCount);
			if (!faction.CrossedSeventy && faction.Grievance >= 70) { faction.CrossedSeventy = true; faction.EscalationPending = true; }
			if (sourceId == "royal_execution" || sourceId == "broken_pledge" || sourceId == "royal_suppression") faction.EscalationPending = true;
		}
	}

	// Leader gets the full hit, the faction's other members a share. Other factions are untouched.
	private static void AddGrievanceToFaction(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, string sourceId, float leaderPoints, float memberPoints)
	{
		if (state == null || faction == null) return;
		foreach (KingdomCivilWarClanState record in state.Clans.Values)
		{
			if (string.Equals(record.ClanId, faction.LeaderClanId, StringComparison.OrdinalIgnoreCase)) AddPoints(record, sourceId, leaderPoints);
			else if (record.Side == KingdomCivilWarSide.Opposition && record.FactionId == faction.Id) AddPoints(record, sourceId, memberPoints);
		}
	}

	private static float TotalGrievance(KingdomCivilWarClanState state)
	{
		return state?.Grievance == null ? 0f : CivilWarRules.Clamp(state.Grievance.Values.Sum(x => CivilWarRules.Clamp(x, 0f, MaxClanGrievance)), 0f, MaxClanGrievance);
	}

	private static float FactionPower(Kingdom kingdom, KingdomCivilWarFactionState faction)
	{
		if (kingdom == null || faction == null) return 0f;
		float rebel = 0f;
		foreach (string id in faction.WarClanIds ?? new List<string>()) rebel += CivilWarWorld.Strength(CivilWarWorld.FindClan(id));
		return CivilWarRules.Clamp(rebel / Math.Max(1f, CivilWarWorld.Strength(kingdom)), 0f, 1f);
	}

	// Pre-war: member clans' share of the kingdom's strength.
	private static float FactionPower(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction)
	{
		if (kingdom == null || faction == null) return 0f;
		float members = faction.CachedStrength;
		return CivilWarRules.Clamp(members / Math.Max(1f, CivilWarWorld.Strength(kingdom)), 0f, 1f);
	}

	private static void RefreshClans(Kingdom kingdom, KingdomCivilWarKingdomState state, int week)
	{
		foreach (Clan clan in CivilWarWorld.LandedClans(kingdom)) GetOrCreateClan(state, clan, week);
		// Clans that left the kingdom drop out of their pre-war faction.
		foreach (KingdomCivilWarClanState record in state.Clans.Values.Where(x => x.Side == KingdomCivilWarSide.Opposition))
		{
			Clan clan = CivilWarWorld.FindClan(record.ClanId);
			KingdomCivilWarFactionState faction = state.Factions.FirstOrDefault(x => x.Id == record.FactionId);
			if (faction == null || (IsPreWar(faction) && (clan == null || clan.IsEliminated || clan.Kingdom != kingdom))) { record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; }
		}
	}

	// Per-faction grievance (independent) and the kingdom summary used by HasTrackedKingdom/panel.
	private static void SaveSummary(KingdomCivilWarKingdomState state)
	{
		if (state == null) return;
		foreach (bool ignored in RebuildPoliticalCaches(state)) { }
	}

	// Delta rebuild can yield between clans without exposing a half-zeroed aggregate to incoming events.
	private static IEnumerable<bool> RebuildPoliticalCaches(KingdomCivilWarKingdomState state)
	{
		float max = 0;
		foreach (var record in state.Clans.Values.ToArray())
		{
			record.Owner = state;
			UpdatePoliticalGrievanceCache(record);
			float strength = CivilWarWorld.Strength(CivilWarWorld.FindClan(record.ClanId));
			if (record.CachedFaction != null) record.CachedFaction.CachedStrength += strength - record.CachedStrength;
			record.CachedStrength = strength;
			max = Math.Max(max, record.CachedGrievance);
			var next = state.Factions.FirstOrDefault(f => f.Stage == KingdomCivilWarStage.OpenWar ? f.WarClanIds.Contains(record.ClanId) : record.Side == KingdomCivilWarSide.Opposition && record.FactionId == f.Id);
			if (record.CachedFaction != next)
			{
				var previous = record.CachedFaction;
				if (previous != null) { previous.GrievanceSum -= record.CachedGrievance; previous.CachedMemberCount--; previous.CachedStrength -= strength; previous.Grievance = previous.GrievanceSum / Math.Max(1, previous.CachedMemberCount); }
				record.CachedFaction = next;
				if (next != null) { next.GrievanceSum += record.CachedGrievance; next.CachedMemberCount++; next.CachedStrength += strength; next.Grievance = next.GrievanceSum / Math.Max(1, next.CachedMemberCount); }
			}
			yield return true;
		}
		state.LastMaxGrievance = max;
		state.Stage = state.Factions.Count == 0 ? KingdomCivilWarStage.Discontent : (KingdomCivilWarStage)state.Factions.Max(x => (int)x.Stage);
	}

	private static void AddHistory(KingdomCivilWarKingdomState state, int week, string text)
	{
		if (state == null || string.IsNullOrWhiteSpace(text)) return;
		if (state.History == null) state.History = new List<KingdomCivilWarHistoryEntry>();
		state.History.Add(new KingdomCivilWarHistoryEntry { Week = week, Text = CivilWarWorld.Limit(text, 240) });
		while (state.History.Count > MaxHistory) state.History.RemoveAt(0);
	}

	private static void WriteMaterial(Kingdom kingdom, int week, string text) { CivilWarCampaignBehavior.RecordMaterial(kingdom, week, CivilWarWorld.Limit(text, 400)); }
	private static void WriteFact(Kingdom kingdom, Hero hero, string key, string text)
	{
		if (hero == Hero.MainHero) MyBehavior.RecordPlayerActionForExternal(text, key, "civil_war", true, kingdom?.Leader, null, CivilWarWorld.KingdomName(kingdom));
		else if (hero != null) MyBehavior.RecordNpcActionForExternal(hero, text, key, "civil_war", true, true, kingdom?.Leader, null, CivilWarWorld.KingdomName(kingdom), false, null);
		MyBehavior.RecordCivilWarMemoryFact(hero, text);
	}

	private void IndexOppositionSettlements(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction)
	{
		// The player is not a combatant until AnswerFollow adds their clan to WarClanIds.
		foreach (string clanId in (faction.WarClanIds ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Clan clan = CivilWarWorld.FindClan(clanId);
			if (clan?.Settlements == null) continue;
			foreach (Settlement settlement in clan.Settlements.Where(x => x != null && (x.IsTown || x.IsCastle))) _oppositionMarkFaction[settlement.StringId] = faction.Id;
		}
	}

	private void ClearWarMarks(string factionId)
	{
		foreach (KeyValuePair<string, string> mark in _oppositionMarkFaction.ToList())
			if (string.Equals(mark.Value, factionId, StringComparison.OrdinalIgnoreCase)) _oppositionMarkFaction.Remove(mark.Key);
	}

	private static bool IsPoliticalClan(Clan clan) => CivilWarWorld.IsPoliticalClan(clan);
	private static float RandomFloat() { return MBRandom.RandomFloat; }

	// Also migrates v2 saves: the single `Faction` becomes the first entry of `Factions`, and its opposition clans join it.
	private static KingdomCivilWarKingdomState Sanitize(KingdomCivilWarKingdomState state)
	{
		if (state == null || string.IsNullOrWhiteSpace(state.KingdomId)) return null;
		state.KingdomId = state.KingdomId.Trim();
		if (state.Clans == null) state.Clans = new Dictionary<string, KingdomCivilWarClanState>(StringComparer.OrdinalIgnoreCase);
		if (state.History == null) state.History = new List<KingdomCivilWarHistoryEntry>();
		if (state.NoPeaceClanIds == null) state.NoPeaceClanIds = new List<string>();
		if (state.Factions == null) state.Factions = new List<KingdomCivilWarFactionState>();
		foreach (string key in state.Clans.Keys.ToList()) if (state.Clans[key] == null) state.Clans.Remove(key);
		state.Factions.RemoveAll(x => x == null);
		KingdomCivilWarFactionState legacy = state.LegacyFaction;
		state.LegacyFaction = null;
		// v2: only a faction that was actually running (not a leftover from cooldown) is carried over.
		if (legacy != null && (state.Stage == KingdomCivilWarStage.FactionFormed || state.Stage == KingdomCivilWarStage.Ultimatum || state.Stage == KingdomCivilWarStage.OpenWar))
		{
			if (string.IsNullOrWhiteSpace(legacy.Id)) legacy.Id = FactionPrefix + state.KingdomId + "-" + (++state.FactionSerial);
			legacy.Stage = state.Stage;
			legacy.StageWeek = state.StageWeek;
			if (!state.Factions.Any(x => x.Id == legacy.Id)) state.Factions.Add(legacy);
			foreach (KingdomCivilWarClanState clan in state.Clans.Values) if (clan.Side == KingdomCivilWarSide.Opposition && string.IsNullOrWhiteSpace(clan.FactionId)) clan.FactionId = legacy.Id;
		}
		foreach (KingdomCivilWarClanState clan in state.Clans.Values)
		{
			if (clan.Grievance == null) clan.Grievance = new Dictionary<string, float>(StringComparer.Ordinal);
			if (clan.FactionId == null) clan.FactionId = "";
			foreach (string key in clan.Grievance.Keys.ToList()) clan.Grievance[key] = CivilWarRules.Clamp(clan.Grievance[key], 0f, MaxClanGrievance);
		}
		foreach (KingdomCivilWarFactionState faction in state.Factions.ToList())
		{
			if (string.IsNullOrWhiteSpace(faction.Id)) faction.Id = FactionPrefix + state.KingdomId + "-" + (++state.FactionSerial);
			if (CivilWarCatalog.FindDemand(faction.DemandId) == null)
			{
				Logger.Log("KingdomCivilWar", "[WARN] dropped faction with unknown demand " + (faction.DemandId ?? "null"));
				state.Factions.Remove(faction);
				continue;
			}
			if (faction.WarClanIds == null) faction.WarClanIds = new List<string>();
			if (faction.Stage == KingdomCivilWarStage.None || faction.Stage == KingdomCivilWarStage.Discontent || faction.Stage == KingdomCivilWarStage.Cooldown) faction.Stage = KingdomCivilWarStage.FactionFormed;
		}
		foreach (KingdomCivilWarClanState clan in state.Clans.Values)
			if (clan.Side == KingdomCivilWarSide.Opposition && !state.Factions.Any(x => x.Id == clan.FactionId)) { clan.Side = KingdomCivilWarSide.Middle; clan.FactionId = ""; }
		SaveSummary(state);
		return state;
	}

	private static readonly ICivilWarHost Host = new CivilWarHost();
	private sealed class CivilWarHost : ICivilWarHost
	{
		public void AdjustStability(Kingdom kingdom, int delta, string reason) { if (kingdom != null && delta != 0) MyBehavior.TryAdjustKingdomStabilityForExternal(kingdom, delta, reason, out _, out _); }
		public bool MovePlayerToRebels(Clan player, Kingdom home, Kingdom rebel)
		{
			try
			{
				TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction.ApplyByJoinToKingdomByDefection(player, home, rebel, default(CampaignTime), showNotification: true);
				return player.Kingdom == rebel;
			}
			catch (Exception ex)
			{
				Logger.Log("KingdomCivilWar", "[WARN] player follow failed: " + ex.Message);
				return false;
			}
		}
		public int GetStability(Kingdom kingdom) { return MyBehavior.GetKingdomStabilityValueForExternal(kingdom); }
		public bool DiscontinueLandlessKingdom(Kingdom kingdom, string reason) { return MyBehavior.TryDiscontinueLandlessKingdomForExternal(kingdom, reason); }
		public void MakeClansPeaceful(IEnumerable<Clan> clans, string reason) { MyBehavior.MakeCivilWarClansPeacefulForExternal(clans, reason); }
		public void QueueRebellion(Kingdom kingdom, Clan leader, List<Clan> followers, string factionId, bool startNow) { MyBehavior.QueueCivilWarRebellionForExternal(kingdom, leader, followers, factionId, startNow); }
		public void ApplyPrestige(Kingdom kingdom, int delta, string reason) { TeamModuleServices.CivilWar.ApplyPrestigeDelta(kingdom == null ? "" : kingdom.StringId, delta, reason); }
		public void RecordMaterial(Kingdom kingdom, int week, string text) { WriteMaterial(kingdom, week, text); }
		public void RecordFact(Kingdom kingdom, Hero hero, string key, string text) { WriteFact(kingdom, hero, key, text); }
	}
}
