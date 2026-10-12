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
	// Main-thread lifecycle index: includes pending creation and registered coup wars. Never serialized.
	private readonly HashSet<KingdomCivilWarFactionState> _openWarFactions = new HashSet<KingdomCivilWarFactionState>();
	internal int WorldOpenWarCount => _openWarFactions.Count;
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
		_openWarFactions.Clear();
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
				_openWarFactions.Add(faction);
				IndexOppositionSettlements(state, faction);
			}
		}
	}

	// ------------------------------------------------------------ queries

	// Also true during the post-war/post-rebellion cooldown: a kingdom hosts one upheaval at a time, so the
	// host's stability rebellion must not fire right after a faction settles (or a rebellion just happened).
	internal bool HasTrackedKingdom(Kingdom kingdom)
	{
		// A rebel kingdom still fighting its civil war never hosts a stability rebellion of its own.
		if (IsActiveRebelKingdom(kingdom)) return true;
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
		AddHistory(state, CivilWarWorld.CurrentWeek(), CivilWarUiText.T("AF_KINGDOM_CW_Core_RebellionCooldown", "The kingdom has just been through a rebellion; no new faction will form for {WEEKS} weeks.", ("WEEKS", tuning.CooldownWeeks.ToString())));
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
			string withdrawal = string.Join("、", withdrawnClans) + KingdomCivilWarTexts.Raw("KW_Raw_113") + source.Name + KingdomCivilWarTexts.Raw("KW_Raw_114");
			// Panel projection of the same fact. The raw `withdrawal` above still feeds the weekly material
			// and the log; only the chronicle entry renders the localized sentence, labelled by source id.
			string withdrawalDisplay = CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_SupportWithdrawn", "{CLANS} grow discontent over {SOURCE} and withdraw their support for the crown, turning neutral", ("CLANS", string.Join(", ", withdrawnClans)), ("SOURCE", CivilWarUiText.SourceName(source.Id, source.Name)));
			AddHistory(state, week, withdrawalDisplay);
			WriteMaterial(kingdom, week, withdrawal);
			Logger.Log("KingdomCivilWar", "crown support withdrawn kingdom=" + kingdom.StringId + " source=" + source.Id + " clans=" + withdrawnClans.Count);
		}
		NotifyPoliticalChange(kingdom, sourceId);

	}

	// continue_war pledge: peace with the pledged target before the deadline breaks it once.
	internal void RecordPeace(Kingdom kingdom, IFaction other, int week)
	{
		KingdomCivilWarKingdomState state = Find(kingdom);
		if (state == null || other == null || string.IsNullOrWhiteSpace(state.NoPeaceTargetId)) return;
		if (CivilWarWorld.CurrentDay() > (state.NoPeaceUntilDay < 0 ? state.NoPeaceUntilWeek * 7 : state.NoPeaceUntilDay)) { ClearPledge(state); return; }
		if (!string.Equals(state.NoPeaceTargetId, other.StringId, StringComparison.OrdinalIgnoreCase)) return;
		List<Clan> clans = (state.NoPeaceClanIds ?? new List<string>()).Select(CivilWarWorld.FindClan).Where(x => x != null).ToList();
		ClearPledge(state);
		AddGrievance(kingdom, "broken_pledge", clans, 25f, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_PledgeBroken", "The king broke his pledge and made peace with {OTHER}", ("OTHER", other.Name?.ToString() ?? CivilWarUiText.T("AF_KINGDOM_CW_Core_EnemyKingdom", "an enemy kingdom"))));
	}

	// ------------------------------------------------------------ war weariness (from the war-stats terminal)

	// Highest weariness among the kingdom's current wars, exactly as the terminal shows it (0..100).
	private static float Weariness(Kingdom kingdom) => kingdom == null ? 0f : MyBehavior.GetMaxWarWearinessForExternal(kingdom);

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
		// Display text for an inquiry: use the localized projections. FactionName/FormatDemand keep
		// serving the stored history, AFEF and weekly material.
		return CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumDelivered", "{FACTION} has delivered an ultimatum: {DEMAND}", ("FACTION", FactionDisplayName(faction, demand)), ("DEMAND", CivilWarUiText.DemandDisplayText(demand, ResolveDemandTargetDisplay(demand, faction, null))))
			+ CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumStatus", "\n\nRefused {COUNT} times. Without an answer it will be decided automatically on day {DAY}. A refusal may lead to civil war.", ("COUNT", faction.Refusals.ToString()), ("DAY", faction.AnswerDeadlineDay.ToString()));
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
		foreach (KingdomCivilWarFactionState faction in state.Factions) { ClearWarMarks(faction.Id); _openWarFactions.Remove(faction); }
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
		float weariness = Weariness(kingdom);
		if (!CivilWarFactionRules.MeetsFormationCondition(grievance, weariness, tuning)) return;
		CivilWarRoll roll = CivilWarRules.Roll("form_faction", CivilWarCatalog.FormFaction, BuildFeatures(kingdom, state, null, leader, stability), 1f, tuning, RandomFloat);
		string cause = grievance >= tuning.DiscontentThreshold
			? CivilWarUiText.T("AF_KINGDOM_CW_Core_CauseThreshold", "discontent has reached the threshold")
			: CivilWarUiText.T("AF_KINGDOM_CW_Core_CauseWeariness", "the kingdom is war-weary ({VALUE})", ("VALUE", weariness.ToString("0")));
		if (!roll.Passed) { AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_FormationRollFailed", "{CLAN} {CAUSE}, but the faction-formation roll failed ({CHANCE})", ("CLAN", CivilWarWorld.ClanName(leader)), ("CAUSE", cause), ("CHANCE", roll.Chance.ToString("0.00")))); return; }
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
		AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_FactionFounded", "{FACTION} is founded; demand: {DEMAND}", ("FACTION", name), ("DEMAND", FormatDemand(demand, faction.TargetName))));
		PublishPoliticalResult(kingdom, state, faction, leader, faction.Id + ":founded", KingdomCivilWarTexts.Raw("KW_Raw_115") + name + KingdomCivilWarTexts.Raw("KW_Raw_116") + FormatDemand(demand, faction.TargetName),
			displayText: CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_OppositionFounded", "An opposition faction is founded: {FACTION}, demanding {DEMAND}", ("FACTION", FactionDisplayName(faction, demand)), ("DEMAND", CivilWarUiText.DemandDisplayText(demand, ResolveDemandTargetDisplay(demand, faction, kingdom)))));
	}

	private void AdvanceUltimatum(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, int stability, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		if (demand == null || leader == null || leader.Kingdom != kingdom || leader.Leader == null || !leader.Leader.IsAlive)
		{
			Dissolve(kingdom, state, faction, week, tuning, CivilWarUiText.T("AF_KINGDOM_CW_Core_FactionDissolvedInvalid", "{FACTION}'s demand target or leader is no longer valid; the faction dissolves", ("FACTION", FactionName(faction, demand))));
			return;
		}
		// Single-war mode: an escalated faction waits for the other war to end, then rebels without a new roll.
		if (faction.WaitingForOtherWar)
		{
			if (!CivilWarFactionRules.CanOpenWar(OtherFactionAtWar(state, faction), tuning)
				|| !CivilWarFactionRules.HasWorldWarSlot(WorldOpenWarCount, tuning)) return;
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
			AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumToPlayerKing", "{FACTION}'s ultimatum has been delivered to the player king; awaiting an explicit answer", ("FACTION", FactionName(faction, demand))));
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
			if (!IsPlayerLed(faction) && faction.Defers >= tuning.MaxRefusals) { Dissolve(kingdom, state, faction, week, tuning, CivilWarUiText.T("AF_KINGDOM_CW_Core_DissolvedKingDelays", "The king kept delaying {FACTION}; the faction falls apart", ("FACTION", name))); return false; }
			RefuseAndReschedule(faction, week, tuning);
			AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_KingDefers", "The king defers his answer to {FACTION}; the ultimatum is postponed", ("FACTION", name)));
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
				faction.DemandId = usurp.Id; faction.WarGoal = (int)CivilWarWarGoal.Usurp; faction.TargetId = ""; faction.TargetName = KingdomCivilWarTexts.Raw("KW_Raw_110"); demand = usurp;
				AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_DemandTurnedToAbdication", "{FACTION} has been refused repeatedly and now demands the king's abdication", ("FACTION", name)));
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
			Dissolve(kingdom, state, faction, week, tuning, CivilWarUiText.T("AF_KINGDOM_CW_Core_DissolvedRepeatedRefusals", "{FACTION} was refused repeatedly and still did not rise; the faction falls apart", ("FACTION", name)));
			return false;
		}
		RefuseAndReschedule(faction, week, tuning);
		AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_KingRefusedDemand", "The king refused {FACTION}'s demand, {REASON}", ("FACTION", name), ("REASON", ruling.Escalate && !enoughRefusals
			? CivilWarUiText.T("AF_KINGDOM_CW_Core_RefusalCountNotMet", "but the faction has not been refused enough times ({COUNT}/{MIN}); it will not rise yet", ("COUNT", faction.Refusals.ToString()), ("MIN", tuning.MinRefusalsBeforeWar.ToString()))
			: CivilWarUiText.T("AF_KINGDOM_CW_Core_EscalationRollFailed", "but the escalation roll failed"))));
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
		if (held > 0) AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_OthersCoolingDown", "{FACTION} has risen; the other factions enter a cooldown (no ultimatums before day {DAY})", ("FACTION", risingName), ("DAY", state.CooldownUntilDay.ToString())));
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
			AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_RebellionImmunityBlocked", "The player kingdom's stability-rebellion immunity stopped {FACTION} from rising", ("FACTION", name)));
			return;
		}
		if (!CivilWarFactionRules.CanOpenWar(OtherFactionAtWar(state, faction), tuning))
		{
			faction.WaitingForOtherWar = true;
			faction.Stage = KingdomCivilWarStage.FactionFormed;
			AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_NoSlotDomesticWar", "{FACTION} is resolved to rise, but a civil war is already running in the realm; it holds back for now", ("FACTION", name)));
			return;
		}
		if (!CivilWarFactionRules.HasWorldWarSlot(WorldOpenWarCount, tuning, playerAuthorized))
		{
			if (!faction.WaitingForOtherWar) AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_NoSlotWorldLimit", "{FACTION} is resolved to rise, but the world rebellion limit has been reached ({COUNT}/{MAX}); it holds back for now", ("FACTION", name), ("COUNT", WorldOpenWarCount.ToString()), ("MAX", tuning.WorldMaxWars.ToString())));
			faction.WaitingForOtherWar = true;
			faction.Stage = KingdomCivilWarStage.FactionFormed;
			return;
		}
		faction.WaitingForOtherWar = false;
		faction.Stage = KingdomCivilWarStage.OpenWar;
		_openWarFactions.Add(faction);
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
		AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_RisingRebelKingdom", "{FACTION} has risen and is founding a temporary rebel kingdom", ("FACTION", name)));
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
		if (leader == null && string.IsNullOrWhiteSpace(faction.RebelKingdomId)) { Dissolve(kingdom, state, faction, week, tuning, CivilWarUiText.T("AF_KINGDOM_CW_Core_LeaderGoneNoRising", "{FACTION}'s leader no longer exists; the uprising is called off", ("FACTION", name))); return; }
		// With a rebel kingdom in the field, a lost leader means the rebellion collapsed: settle it as a crown victory
		// so the rebel kingdom is wound up instead of being left at war with no faction behind it.
		if (leader == null || leader.IsEliminated) faction.RebelKingdomDestroyed = true;
		if (string.IsNullOrWhiteSpace(faction.RebelKingdomId))
		{
			if (leader?.Kingdom != kingdom) { faction.ResolutionNeedsReview = true; faction.ResolutionError = KingdomCivilWarTexts.Raw("KW_Raw_117"); return; }
			if (_activeNamingRequests.Add(faction.Id)) Host.QueueRebellion(kingdom, leader, faction.WarClanIds.Select(CivilWarWorld.FindClan).Where(x => x != null && x != leader && x != Clan.PlayerClan && x.Kingdom == kingdom).ToList(), faction.Id, true);
			// WarStartWeek is only set by the creation callback; without it there is no war to resolve.
			if (CivilWarWorld.CurrentDay() <= faction.WarRequestDay + tuning.WarRequestTimeoutWeeks * 7) return;
			NotifyRebellionFailed(faction.Id, CivilWarUiText.T("AF_KINGDOM_CW_Core_ReasonFoundingTimeout", "the founding request timed out"));
			return;
		}
		Kingdom rebel = CivilWarWorld.FindKingdom(faction.RebelKingdomId);
		float crownPower = CivilWarWorld.Strength(kingdom);
		float rebelPower = CivilWarWorld.Strength(rebel);
		faction.LastFactionPower = CivilWarRules.Clamp(rebelPower / Math.Max(1f, crownPower + rebelPower), 0f, 1f);
		// War score from this war's weariness and casualty exchange (war-stats terminal), not raw troop totals.
		if (CivilWarWorld.IsAlive(rebel))
			faction.LastWarScore = CivilWarWearinessRules.WarScore(MyBehavior.GetWarWearinessForExternal(kingdom, rebel), MyBehavior.GetWarWearinessForExternal(rebel, kingdom),
				MyBehavior.GetWarCasualtiesForExternal(kingdom, rebel), MyBehavior.GetWarCasualtiesForExternal(rebel, kingdom));
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
		if (_storage.Operations.TryGetValue(faction.RebellionOperationId ?? "", out var completedRequest)) { completedRequest.Status = (int)CivilWarActionStatus.Applied; completedRequest.Message = CivilWarUiText.T("AF_KINGDOM_CW_Core_RebelKingdomEstablished", "The rebel kingdom has been established; the civil war breaks out."); }
		_activeNamingRequests.Remove(faction.Id);
		faction.WarStartWeek = Math.Max(week, faction.WarRequestWeek);
		faction.WarStartDay = CivilWarWorld.CurrentDay();
		if (faction.LeaderClanId == Clan.PlayerClan?.StringId) { faction.PlayerFollowPending = false; faction.PlayerFollowAccepted = true; }
		IndexOppositionSettlements(state, faction);
		faction.RebelFortShareAtStart = CivilWarWorld.FortificationCount(rebelKingdom) / (float)Math.Max(1, CivilWarWorld.FortificationCount(kingdom));
		Host.AdjustStability(kingdom, -8, "civil_war_outbreak");
		Host.ApplyPrestige(kingdom, -10, KingdomCivilWarTexts.Raw("KW_Raw_118"));
		CivilWarDemandDef outbreakDemand = CivilWarCatalog.FindDemand(faction.DemandId);
		string name = FactionName(faction, outbreakDemand);
		AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_OutbreakHistory", "{FACTION} establishes {REBEL}; the civil war breaks out", ("FACTION", name), ("REBEL", CivilWarWorld.KingdomName(rebelKingdom))));
		PublishPoliticalResult(kingdom, state, faction, CivilWarWorld.FindClan(faction.LeaderClanId), faction.Id + ":war:outbreak", KingdomCivilWarTexts.Raw("KW_Raw_119") + name + KingdomCivilWarTexts.Raw("KW_Raw_120") + CivilWarWorld.KingdomName(rebelKingdom) + KingdomCivilWarTexts.Raw("KW_Raw_121") + CivilWarWorld.KingdomName(kingdom) + KingdomCivilWarTexts.Raw("KW_Raw_122"),
			displayText: CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_Outbreak", "{FACTION} establishes {REBEL} and goes to war with {KINGDOM}; the civil war breaks out", ("FACTION", FactionDisplayName(faction, outbreakDemand)), ("REBEL", CivilWarWorld.KingdomName(rebelKingdom)), ("KINGDOM", CivilWarWorld.KingdomName(kingdom))));
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
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		// Display text for an inquiry: the faction name uses the localized projection. The rebel kingdom
		// is resolved from the live object so a destroyed kingdom does not leak the Chinese "some
		// kingdom" placeholder that CivilWarWorld.KingdomName answers for a null kingdom.
		string name = FactionDisplayName(faction, demand);
		Kingdom rebelKingdom = CivilWarWorld.FindKingdom(faction.RebelKingdomId);
		string rebel = rebelKingdom == null ? CivilWarUiText.T("AF_KINGDOM_CW_World_UnknownKingdom", "some kingdom") : CivilWarWorld.KingdomName(rebelKingdom);
		return CivilWarUiText.T("AF_KINGDOM_CW_Core_FollowPromptIntro", "The {FACTION} you belong to has risen and founded {REBEL}, going to war with the crown.", ("FACTION", name), ("REBEL", rebel))
			+ CivilWarUiText.T("AF_KINGDOM_CW_Core_FollowPromptFollow", "\n\nFollow: your clan takes its fiefs over to {REBEL}; when the civil war ends you return with the outcome (if the crown wins, you will be punished).", ("REBEL", rebel))
			+ CivilWarUiText.T("AF_KINGDOM_CW_Core_FollowPromptStay", "\nStay: your clan sides with the crown and turns against its former comrades.");
	}

	internal bool AnswerFollow(string factionId, bool follow, out string message)
	{
		message = "";
		KingdomCivilWarFactionState faction = FindFaction(factionId, out KingdomCivilWarKingdomState state);
		if (faction == null || !faction.PlayerFollowPending || faction.Stage != KingdomCivilWarStage.OpenWar) { message = CivilWarUiText.T("AF_KINGDOM_CW_Core_FollowEnded", "This uprising is already over."); return false; }
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
			message = CivilWarUiText.T("AF_KINGDOM_CW_Core_FollowAccepted", "Your clan follows {FACTION} and defects to {REBEL}.", ("FACTION", name), ("REBEL", CivilWarWorld.KingdomName(rebel)));
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
		message = follow ? CivilWarUiText.T("AF_KINGDOM_CW_Core_FollowFailed", "The defection to the rebels failed; your clan stays with the kingdom.") : CivilWarUiText.T("AF_KINGDOM_CW_Core_FollowDeclined", "Your clan refuses to follow {FACTION} and sides with the crown.", ("FACTION", name));
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
		faction.ResolutionError = CivilWarUiText.T("AF_KINGDOM_CW_Core_RebellionIncomplete", "The uprising is not yet complete: {REASON}", ("REASON", CivilWarWorld.Limit(reason, 160)));
		if (_storage.Operations.TryGetValue(faction.RebellionOperationId ?? "", out var failedRequest)) { failedRequest.Status = (int)CivilWarActionStatus.PartialFailure; failedRequest.Message = faction.ResolutionError; }
		if (leader?.Kingdom == kingdom)
		{
			faction.Stage = KingdomCivilWarStage.FactionFormed; faction.WarRequestDay = -1; faction.WarClanIds.Clear();
			_openWarFactions.Remove(faction);
			faction.PlayerFollowPending = false; faction.PlayerFollowAsked = false;
			faction.UltimatumDay = CivilWarWorld.CurrentDay() + 7; faction.WaitingForOtherWar = false;
			ClearWarMarks(faction.Id); if (!state.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) _openWarKingdoms.Remove(state.KingdomId);
		}
		else faction.ResolutionNeedsReview = true;
		// ResolutionError is diagnostic state and may hold a raw host reason, so it is never rendered verbatim here;
		// the panel only reads its emptiness (see the panel note) and the raw text stays in the log and in the state.
		AddHistory(state, CivilWarWorld.CurrentWeek(), CivilWarUiText.T("AF_KINGDOM_CW_Core_RebellionIncompleteRetained", "The uprising did not complete; the faction has been retained."));
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
		string reason = KingdomCivilWarTexts.Raw("KW_Raw_123");
		string effectId = restoration && faction.RestorationVictoryConfirmed && outcome?.Id == "rebels_usurp"
			? CivilWarEffectIds.RestoreDynasty : outcome?.EffectId;
		bool applied = outcome != null && CivilWarEffects.TryApply(effectId, ctx, out reason);
		string name = FactionName(faction, demand);
		string text = applied ? name + KingdomCivilWarTexts.Raw("KW_Raw_124") + outcome.Name + "）：" + string.Join("；", ctx.Notes) : name + KingdomCivilWarTexts.Raw("KW_Raw_125") + (reason ?? KingdomCivilWarTexts.Raw("KW_Raw_123"));
		// Panel projection of the same fact. `outcome.Name` and `ctx.Notes` are raw rule text, so both render
		// through their projections here; the raw `text` above keeps feeding the political-result material,
		// the saved receipt and the AFEF facts.
		string display = applied
			? CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_WarResolved", "{FACTION}'s civil war ends ({OUTCOME}): {NOTES}", ("FACTION", FactionDisplayName(faction, demand)), ("OUTCOME", OutcomeDisplayName(outcome)), ("NOTES", CivilWarUiText.NoteText(ctx.Notes, ctx.NoteProjections)))
			: outcome == null
				? CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_NoOutcome", "{FACTION}'s civil war could not be settled: no outcome was available", ("FACTION", FactionDisplayName(faction, demand)))
				: CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_ResolutionFailed", "{FACTION}'s civil war could not be settled: {REASON}", ("FACTION", FactionDisplayName(faction, demand)), ("REASON", CivilWarUiText.FailureText(ctx.FailureCode, ctx.FailureArg, ctx.FailureArg2) ?? reason ?? ""));
		AddHistory(state, week, display);
		if (applied) PublishPoliticalResult(kingdom, state, faction, leader, faction.Id + ":war:resolved", text, displayText: display);
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
		// A newly independent kingdom starts with the same cooldown before its own factions can form.
		if (outcome.Id == CivilWarCatalog.SecedeOutcomeId && CivilWarWorld.IsAlive(ctx.RebelKingdom)) NoteKingdomRebellion(ctx.RebelKingdom);
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
		string text = mood == CivilWarAftermath.Suppressed ? CivilWarUiText.T("AF_KINGDOM_CW_Core_AftermathSuppressed", "The crown's suppression has cowed the remaining factions; for now they fall quiet")
			: mood == CivilWarAftermath.Emboldened ? CivilWarUiText.T("AF_KINGDOM_CW_Core_AftermathEmboldened", "The rebels' victory has emboldened the remaining factions, but they still need time to regroup")
			: CivilWarUiText.T("AF_KINGDOM_CW_Core_AftermathSettled", "The civil war has calmed; the remaining factions hold back their pressure");
		AddHistory(state, week, CivilWarUiText.T("AF_KINGDOM_CW_Core_AftermathTruce", "{MOOD} (no ultimatums for {WEEKS} weeks)", ("MOOD", text), ("WEEKS", truce.ToString())));
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
		_openWarFactions.Remove(faction);
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

	// `displayText` is an optional panel-only projection of `reason`. The raw `reason` stays the rule string
	// that material, saved facts and the caller's own records keep; only the chronicle entry uses the
	// projection, and a caller without one keeps writing `reason` alone.
	private void Dissolve(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, string reason, bool startCooldown = true, string displayText = null)
	{
		AddHistory(state, week, displayText ?? reason);
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
		=> TryJoinPlayer(kingdom, speaker, side, out message, out _);

	internal bool TryJoinPlayer(Kingdom kingdom, Hero speaker, KingdomCivilWarSide side, out string message, out string displayMessage)
	{
		displayMessage = "";
		var state = Find(kingdom);
		var faction = FactionOfClan(state, speaker?.Clan);
		if (side == KingdomCivilWarSide.Opposition && !IsPreWar(faction))
		{
			// The speaker is not in a pre-war faction: only fall back when the choice is unambiguous, never guess the strongest one.
			var open = state?.Factions.Where(IsPreWar).Take(2).ToList();
			faction = open != null && open.Count == 1 ? open[0] : null;
			if (faction == null) { message = KingdomCivilWarTexts.Raw("KW_Raw_126"); return false; }
		}
		var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom?.StringId ?? "",
			FactionId = faction?.Id ?? "", Action = side == KingdomCivilWarSide.Crown ? CivilWarAction.JoinCrown : CivilWarAction.JoinOpposition }, Clan.PlayerClan);
		message = result.Message;
		displayMessage = CivilWarUiText.ResultNotice(result);
		return result.Status == CivilWarActionStatus.Applied;
	}

	internal bool TryRecruitClan(Hero recruiter, Clan target, Kingdom kingdom, out string message)
		=> TryRecruitClan(recruiter, target, kingdom, out message, out _);

	internal bool TryRecruitClan(Hero recruiter, Clan target, Kingdom kingdom, out string message, out string displayMessage)
	{
		message = "";
		displayMessage = "";
		KingdomCivilWarKingdomState state = kingdom == null ? null : GetOrCreate(kingdom, CivilWarWorld.CurrentWeek());
		// One shared precondition check: the dialogue tag filter (ApplicableDialogueActions) uses the same method, so they cannot drift apart.
		string block = RecruitBlockReason(recruiter, target, kingdom, state);
		if (state == null || block.Length > 0) { message = block.Length > 0 ? block : KingdomCivilWarTexts.Raw("KW_Raw_127"); return false; }
		int week = CivilWarWorld.CurrentWeek();
		KingdomCivilWarClanState record = GetOrCreateClan(state, target, week);
		bool crown = CivilWarWorld.IsPlayerRuled(kingdom) && recruiter.Clan == kingdom.RulingClan;
		// `display` is the panel projection of `message`; `message` itself is the out value the dialogue tag
		// channel receives, so it keeps its raw wording.
		string display;
		if (crown || state.Clans[recruiter.Clan.StringId].Side == KingdomCivilWarSide.Crown)
		{
			record.Side = KingdomCivilWarSide.Crown; record.FactionId = "";
			message = CivilWarWorld.ClanName(target) + KingdomCivilWarTexts.Raw("KW_Raw_128");
			display = CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_RecruitedCrown", "{CLAN} joins the crown faction.", ("CLAN", CivilWarWorld.ClanName(target)));
		}
		else
		{
			KingdomCivilWarFactionState faction = FactionOfClan(state, recruiter.Clan);
			if (faction == null || !IsPreWar(faction)) { message = KingdomCivilWarTexts.Raw("KW_Raw_129"); return false; }
			record.Side = KingdomCivilWarSide.Opposition; record.FactionId = faction.Id;
			AddGrievanceToFaction(state, faction, "demand_refused", 8f, 0f);
			CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
			message = CivilWarWorld.ClanName(target) + KingdomCivilWarTexts.Raw("KW_Raw_130") + FactionName(faction, demand) + "。";
			display = CivilWarUiText.T("AF_KINGDOM_CW_Chronicle_RecruitedFaction", "{CLAN} joins {FACTION}.", ("CLAN", CivilWarWorld.ClanName(target)), ("FACTION", FactionDisplayName(faction, demand)));
		}
		record.SideSinceWeek = week; record.SideSinceDay = CivilWarWorld.CurrentDay();
		record.LastRecruitWeek = week;
		SaveSummary(state);
		PublishPoliticalResult(kingdom, state, FactionOfClan(state, target), target, "recruit:" + target.StringId + ":" + CivilWarWorld.CurrentDay(), message, displayText: display);
		displayMessage = display;
		NotifyPoliticalChange(kingdom, "membership");
		Revision++;
		return true;
	}

	// Dialogue LEAVE tags: the speaker's own clan (SELF) or the player's clan (PLAYER) steps out of its current side.
	// Goes through Execute/Quote, so the same relation loss, 7-day exit lock and war lock as the panel apply.
	internal bool TryLeaveClan(Kingdom kingdom, Clan clan, out string message)
		=> TryLeaveClan(kingdom, clan, out message, out _);

	internal bool TryLeaveClan(Kingdom kingdom, Clan clan, out string message, out string displayMessage)
	{
		message = "";
		displayMessage = "";
		if (kingdom == null || clan == null) { message = KingdomCivilWarTexts.Raw("KW_Raw_131"); return false; }
		var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom.StringId, Action = CivilWarAction.Leave }, clan);
		message = result.Message;
		displayMessage = CivilWarUiText.ResultNotice(result);
		return result.Status == CivilWarActionStatus.Applied;
	}

	// Only the player's own faction can be detonated, through its leader.
	internal bool TryDetonate(Hero speaker, Kingdom kingdom, out string message)
		=> TryDetonate(speaker, kingdom, out message, out _);

	internal bool TryDetonate(Hero speaker, Kingdom kingdom, out string message, out string displayMessage)
	{
		message = "";
		displayMessage = "";
		KingdomCivilWarKingdomState state = Find(kingdom);
		KingdomCivilWarFactionState faction = speaker?.Clan == null ? null : FactionOfClan(state, speaker.Clan);
		if (kingdom == null || faction == null || !IsPreWar(faction) || faction.LeaderClanId != speaker.Clan.StringId || PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom)
			|| Clan.PlayerClan == null || FactionOfClan(state, Clan.PlayerClan) != faction)
		{ message = KingdomCivilWarTexts.Raw("KW_Raw_132"); return false; }
		var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom.StringId,
			FactionId = faction.Id, Action = CivilWarAction.Detonate }, Clan.PlayerClan, true);
		message = result.Message;
		displayMessage = CivilWarUiText.ResultNotice(result);
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
		if (faction == null || !faction.PlayerAnswerPending || !CivilWarWorld.IsPlayerRuled(kingdom)) { message = CivilWarUiText.T("AF_KINGDOM_CW_Core_NoPendingUltimatum", "There is no ultimatum awaiting an answer."); return false; }
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		if (demand == null || leader == null) { message = CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumExpired", "The ultimatum is no longer valid."); return false; }
		if (accept)
		{
			var result = Execute(new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = kingdom.StringId, FactionId = faction.Id, Action = CivilWarAction.Concede }, Clan.PlayerClan);
			// Display path: the Concede result carries a projection key, so the notification renders the
			// localized sentence instead of the raw Chinese receipt text.
			message = CivilWarUiText.ResultNotice(result); return result.Status == CivilWarActionStatus.Applied;
		}
		int week = CivilWarWorld.CurrentWeek();
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		int stability = Host.GetStability(kingdom);
		Dictionary<string, float> features = BuildFeatures(kingdom, state, faction, leader, stability);
		CivilWarUltimatumResult ruling = accept
			? new CivilWarUltimatumResult { Ruling = CivilWarRuling.Accept, Log = "player accept" }
			: CivilWarDecisions.RollRefusal(demand, features, faction.Refusals, tuning, RandomFloat, CivilWarAftermathRules.EscalationFactor(CurrentAftermath(state, week)));
		AddHistory(state, week, accept
			? CivilWarUiText.T("AF_KINGDOM_CW_Core_PlayerKingAccepted", "The player king accepted {FACTION}'s ultimatum", ("FACTION", FactionName(faction, demand)))
			: CivilWarUiText.T("AF_KINGDOM_CW_Core_PlayerKingRefused", "The player king refused {FACTION}'s ultimatum", ("FACTION", FactionName(faction, demand))));
		bool carriedOut = ApplyRuling(kingdom, state, faction, demand, leader, week, tuning, HostStability, ruling);
		// A failed acceptance also removes the faction, so "still in the list" cannot tell the two apart.
		if (accept) message = carriedOut
			? CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumAcceptedDissolved", "The ultimatum was accepted; the faction has disbanded.")
			: CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumAcceptPending", "The demand could not be fulfilled; the faction remains pending action.");
		else message = faction.Stage == KingdomCivilWarStage.OpenWar
			? CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumRefusedRevolt", "You refused the ultimatum; the faction has risen in revolt.")
			: faction.WaitingForOtherWar
				? CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumRefusedWaiting", "You refused the ultimatum; the faction is resolved to revolt and awaits its moment.")
				: CivilWarUiText.T("AF_KINGDOM_CW_Core_UltimatumRefusedNoRevolt", "You refused the ultimatum; the faction has not revolted yet.");
		faction.Version++; SaveSummary(state); Revision++;
		return true;
	}

	private static readonly Action<Kingdom, int> HostStability = (k, d) => Host.AdjustStability(k, d, "civil_war");

	// ------------------------------------------------------------ panel (on demand, when the tab opens)

	internal CivilWarPanelKingdom BuildPlayerKingdomPanel()
	{
		CivilWarPanelKingdom panel = new CivilWarPanelKingdom();
		Kingdom kingdom = PlayerPoliticalKingdom();
		if (!DuelSettings.IsCivilWarFactionsEnabled()) { panel.EmptyText = CivilWarUiText.T("AF_KINGDOM_CW_Panel_EmptyDisabled", "Civil war factions are disabled"); return panel; }
		if (kingdom == null || kingdom.IsEliminated) { panel.EmptyText = CivilWarUiText.T("AF_KINGDOM_CW_Panel_EmptyNoKingdom", "You have not joined any kingdom"); return panel; }
		panel.Available = true;
		panel.KingdomId = kingdom.StringId; panel.Revision = Revision;
		panel.Name = CivilWarWorld.KingdomName(kingdom);
		panel.Stability = Host.GetStability(kingdom);
		panel.StabilityTier = StabilityTierText(panel.Stability);
		KingdomCivilWarKingdomState state = Find(kingdom);
		List<KingdomCivilWarFactionState> factions = state?.Factions ?? new List<KingdomCivilWarFactionState>();
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		int day = CivilWarWorld.CurrentDay();
		if (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) panel.StageText = CivilWarUiText.T("AF_KINGDOM_CW_Panel_StagePlayerKingdomDisabled", "Factions are disabled by settings in the player's kingdom");
		else if (factions.Count == 0)
		{
			bool cooling = state != null && day < state.CooldownUntilDay;
			// The ledger wants a distinct title only for the plain "nothing has formed" case; cooldown
			// keeps its own text, so the flag is false there.
			panel.NoFactionFormed = !cooling;
			panel.StageText = cooling
				? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageCooldown", "Settling after civil war  ·  no faction can form before day {DAY}", ("DAY", state.CooldownUntilDay.ToString()))
				: CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageNoneFormed", "No factions have formed yet");
		}
		else if (factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) panel.StageText = CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageOpenWar", "{COUNT} factions are in civil war", ("COUNT", factions.Count(x => x.Stage == KingdomCivilWarStage.OpenWar).ToString()));
		else if (factions.Any(x => x.Stage == KingdomCivilWarStage.Ultimatum)) panel.StageText = CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageUltimatum", "{COUNT} factions have issued an ultimatum", ("COUNT", factions.Count(x => x.Stage == KingdomCivilWarStage.Ultimatum).ToString()));
		else panel.StageText = CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageFormed", "{COUNT} opposition factions have formed", ("COUNT", factions.Count.ToString()));
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
			(crown ? panel.Crown : panel.Middle).Add(PanelClan(kingdom, clan, record, king
				? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StanceKing", "King")
				: crown ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StanceCrown", "Crown faction") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_StanceUndeclared", "Undeclared"), king, share(new[] { clan.StringId })));
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
		panel.Conditions.Add(Condition(CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionFormalVassal", "Formal vassal eligibility"), top != null ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionMet", "Met") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionNoEligibleClan", "No eligible clan"), top != null));
		panel.Conditions.Add(Condition(CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionFactionSlots", "Faction slots in this kingdom"), factions.Count + " / " + panel.MaxFactions, factions.Count < panel.MaxFactions));
		panel.Conditions.Add(Condition(CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionGrievanceThreshold", "Grievance threshold {THRESHOLD}", ("THRESHOLD", panel.Threshold.ToString())), panel.TopGrievance >= panel.Threshold ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionReached", "Reached") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionNotReached", "Not reached"), panel.TopGrievance >= panel.Threshold));
		float weariness = Weariness(kingdom);
		panel.Conditions.Add(Condition(CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionOrWeariness", "or war weariness {VALUE}", ("VALUE", CivilWarWearinessRules.FormationThreshold.ToString("0"))), weariness.ToString("0") + " " + (CivilWarWearinessRules.MeetsFormation(weariness) ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionReached", "Reached") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionNotReached", "Not reached")), CivilWarWearinessRules.MeetsFormation(weariness)));
		panel.Conditions.Add(Condition(CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionPostWarCooldown", "Post-war cooldown"), day < cooldownDay ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_UntilDay", "until day {DAY}", ("DAY", cooldownDay.ToString())) : CivilWarUiText.T("AF_KINGDOM_CW_Panel_None", "None"), day >= cooldownDay));
		if (CivilWarWorld.IsPlayerRuled(kingdom)) panel.Conditions.Add(Condition(CivilWarUiText.T("AF_KINGDOM_CW_Panel_ConditionPlayerKingdomFactions", "Player-kingdom factions"), DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed() ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_Enabled", "Enabled") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_SettingDisabled", "Disabled in settings"), DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()));
		List<KingdomCivilWarHistoryEntry> history = state?.History ?? new List<KingdomCivilWarHistoryEntry>();
		for (int i = history.Count - 1; i >= 0 && panel.Chronicle.Count < 3; i--)
			if (!string.IsNullOrWhiteSpace(history[i]?.Text)) panel.Chronicle.Add(CivilWarUiText.T("AF_KINGDOM_CW_Panel_ChronicleWeek", "Week {WEEK}  ·  {TEXT}", ("WEEK", history[i].Week.ToString()), ("TEXT", CivilWarWorld.Limit(history[i].Text, 46))));
		PlayerPanelFigures(kingdom, state, panel, day);
		return panel;
	}

	private static CivilWarPanelCondition Condition(string name, string value, bool met) => new CivilWarPanelCondition { Name = name, Value = value, Met = met };

	private static void ResolvePlayerRole(Kingdom kingdom, KingdomCivilWarKingdomState state, CivilWarPanelKingdom panel)
	{
		Clan player = Clan.PlayerClan;
		// CivilWarWorld.ClanName answers a Chinese "some clan" placeholder for a null clan, which must not
		// leak into the English panel; check the object first, as FactionDisplayName does.
		panel.PlayerClanName = player == null ? CivilWarUiText.T("AF_KINGDOM_CW_World_UnknownClan", "an unknown clan") : CivilWarWorld.ClanName(player);
		string id = player?.StringId ?? "";
		KingdomCivilWarFactionState own = state?.Factions.FirstOrDefault(f => f.Stage == KingdomCivilWarStage.OpenWar && (f.WarClanIds?.Contains(id) == true || f.LeaderClanId == id)) ?? FactionOfClan(state, player);
		KingdomCivilWarClanState record = null;
		state?.Clans.TryGetValue(id, out record);
		if (player != null && kingdom.RulingClan == player) panel.Role = CivilWarPanelRole.King;
		else if (own != null) { panel.Role = string.Equals(own.LeaderClanId, id, StringComparison.OrdinalIgnoreCase) ? CivilWarPanelRole.Leader : CivilWarPanelRole.Member; panel.PlayerFactionId = own.Id; }
		else panel.Role = record?.Side == KingdomCivilWarSide.Crown ? CivilWarPanelRole.Crown : CivilWarPanelRole.Middle;
		string tag = own == null ? "" : CivilWarUiText.DemandTag(CivilWarCatalog.FindDemand(own.DemandId));
		if (panel.Role == CivilWarPanelRole.King) panel.Identity = CivilWarUiText.T("AF_KINGDOM_CW_Panel_IdentityKing", "You are the king  ·  Crown faction");
		else if (!PoliticalClan(player, kingdom) && own == null) panel.Identity = CivilWarUiText.T("AF_KINGDOM_CW_Panel_IdentityNotVassal", "{CLAN}  ·  Not a formal vassal; cannot join a faction", ("CLAN", panel.PlayerClanName));
		else panel.Identity = CivilWarUiText.T("AF_KINGDOM_CW_Panel_IdentityVassal", "Vassal  ·  {CLAN}  ·  {ROLE}", ("CLAN", panel.PlayerClanName), ("ROLE", RoleLabel(panel.Role, tag)));
	}

	// Delegates to the shared helper so the reserved AF_KINGDOM_CW_Role_* keys stay the single source of truth.
	internal static string RoleLabel(CivilWarPanelRole role, string factionTag) => CivilWarUiText.Role(role, factionTag);

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
		string exit = CivilWarUiText.T("AF_KINGDOM_CW_Panel_ExitCost", "Leader −{LEADER}, members −{MEMBER}; cannot join or found a faction for {DAYS} days.", ("LEADER", CivilWarPoliticalRules.ExitLeaderRelationLoss.ToString()), ("MEMBER", CivilWarPoliticalRules.ExitMemberRelationLoss.ToString()), ("DAYS", CivilWarPoliticalRules.ExitDays.ToString()));
		switch (panel.Role)
		{
			case CivilWarPanelRole.King:
				panel.ActionHint = panel.Factions.Count > 0
					? CivilWarUiText.T("AF_KINGDOM_CW_Panel_KingHintActive", "Suppression, negotiation and force-dissolution share a {DAYS}-day cooldown; concession is unlimited but honors the demand.", ("DAYS", CivilWarPoliticalRules.ActionDays.ToString()))
					: CivilWarUiText.T("AF_KINGDOM_CW_Panel_KingHintIdle", "Once an opposition faction forms, suppression, negotiation, concession and force-dissolution become available.");
				return;
			case CivilWarPanelRole.Crown:
				panel.ActionHint = CivilWarUiText.T("AF_KINGDOM_CW_Panel_CrownExitHint", "Leaving the crown faction: king −{LEADER}, other crown clans −{MEMBER}; you stay neutral for {DAYS} days.", ("LEADER", CivilWarPoliticalRules.ExitLeaderRelationLoss.ToString()), ("MEMBER", CivilWarPoliticalRules.ExitMemberRelationLoss.ToString()), ("DAYS", CivilWarPoliticalRules.ExitDays.ToString()));
				break;
			case CivilWarPanelRole.Member:
				panel.ActionHint = CivilWarUiText.T("AF_KINGDOM_CW_Panel_MemberHint", "Rebellion is the leader's decision; after a refused proposal you cannot raise it again for {DAYS} days. Leaving: {EXIT}", ("DAYS", CivilWarPoliticalRules.ActionDays.ToString()), ("EXIT", exit));
				break;
			case CivilWarPanelRole.Leader:
				panel.ActionHint = CivilWarUiText.T("AF_KINGDOM_CW_Panel_LeaderHint", "This faction never rebels automatically; a manual rebellion needs this clan to hold {THRESHOLD}% of the kingdom's military power (currently {CURRENT}%). It can be dissolved or refounded, still subject to the post-war cooldown.", ("THRESHOLD", DuelSettings.BuildCivilWarTuning().PlayerDetonationStrengthPercent.ToString()), ("CURRENT", PlayerStrengthPercent(kingdom, player).ToString("0.0")));
				break;
			default:
				panel.ActionHint = day < exitUntil
					? CivilWarUiText.T("AF_KINGDOM_CW_Panel_MiddleCooldown", "Exit cooldown runs until day {DAY}; until then you cannot join or found a faction.", ("DAY", exitUntil.ToString()))
					: CivilWarUiText.T("AF_KINGDOM_CW_Panel_MiddleSwitchHint", "To switch factions after joining, leave first: {EXIT}", ("EXIT", exit));
				break;
		}
		if (locked) panel.ActionHint = CivilWarUiText.T("AF_KINGDOM_CW_Panel_MembersLocked", "During a founding request or open civil war, member membership is locked.");
	}

	private static CivilWarPanelClan PanelClan(Kingdom kingdom, Clan clan, KingdomCivilWarClanState record, string stance, bool leader, int powerPercent)
	{
		float grievance = TotalGrievance(record);
		int relation = CivilWarWorld.Relation(clan?.Leader, kingdom?.Leader);
		bool king = clan != null && clan == kingdom?.RulingClan;
		bool player = clan != null && clan == Clan.PlayerClan;
		return new CivilWarPanelClan
		{
			Name = CivilWarWorld.ClanName(clan) + (player ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_YouSuffix", " (you)") : ""),
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
		// CivilWarWorld.ClanName answers a Chinese "some clan" placeholder for a null clan; resolve the
		// display name once so both the short label and the dossier header stay English.
		string leaderName = leader == null ? CivilWarUiText.T("AF_KINGDOM_CW_World_UnknownClan", "an unknown clan") : CivilWarWorld.ClanName(leader);
		bool paused = IsPausedByOtherWar(state, faction, DuelSettings.BuildCivilWarTuning());
		// Localized faction tag comes from the shared helper (AF_KINGDOM_CW_FactionTag_*). The Chinese
		// suffix strip below still applies to the zh-CN tag ("反战派" -> "反战"); English tags pass through whole.
		string tag = CivilWarUiText.DemandTag(demand);
		string demandWord = tag.EndsWith(KingdomCivilWarTexts.Raw("KW_Raw_133"), StringComparison.Ordinal) ? tag.Substring(0, tag.Length - 1) : tag;
		bool playerFaction = faction.Id == kingdomPanel.PlayerFactionId;
		bool playerLeader = playerFaction && kingdomPanel.Role == CivilWarPanelRole.Leader;
		CivilWarPanelFaction panel = new CivilWarPanelFaction
		{
			Id = faction.Id,
			Tag = tag,
			DemandTag = CivilWarUiText.T("AF_KINGDOM_CW_Panel_DemandTag", "{TAG} demand", ("TAG", demandWord)),
			DemandId = faction.DemandId, TargetId = faction.TargetId,
			Name = FactionDisplayName(faction, demand),
			ShortName = CivilWarUiText.T("AF_KINGDOM_CW_Panel_ShortName", "{CLAN} {TAG}", ("CLAN", leaderName), ("TAG", tag)),
			Color = demand?.Color ?? "#6B3A78FF",
			LeaderLine = CivilWarUiText.T("AF_KINGDOM_CW_Panel_LeaderLine", "Leader  {CLAN}{YOU}   ·   Relation with king {RELATION}", ("CLAN", leaderName), ("YOU", leader != null && leader == Clan.PlayerClan ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_YouSuffix", " (you)") : ""), ("RELATION", SignedRelation(leader, kingdom))),
			DemandTitle = demand == null
				? CivilWarUiText.T("AF_KINGDOM_CW_Panel_DemandTitleExpired", "Demand no longer valid")
				: CivilWarUiText.T("AF_KINGDOM_CW_Panel_DemandTitle", "Demand: {DEMAND}", ("DEMAND", CivilWarUiText.DemandDisplayText(demand, ResolveDemandTargetDisplay(demand, faction, kingdom)))),
			Grievance = (int)Math.Round(CivilWarRules.Clamp(faction.Grievance, 0f, 100f)),
			Stage = FactionStageText(faction, week, paused, CurrentAftermath(state, week)),
			Refusals = faction.Refusals,
			RefusalUnit = faction.Defers > 0
				? CivilWarUiText.T("AF_KINGDOM_CW_Panel_RefusalUnitDeferred", " times  ·  deferred {COUNT}", ("COUNT", faction.Defers.ToString()))
				: CivilWarUiText.T("AF_KINGDOM_CW_Panel_RefusalUnit", " times"),
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
		if (IsPlayerLed(faction) && !war && faction.PendingResponse == null) panel.Note = CivilWarUiText.T("AF_KINGDOM_CW_Panel_PlayerLedNote", "A faction you lead never rebels automatically; you can change the demand and re-evaluate members, but a rebellion needs manual confirmation and must meet the strength threshold.");
		foreach (string id in memberIds)
		{
			Clan clan = CivilWarWorld.FindClan(id);
			if (clan == null || clan.IsEliminated) continue;
			KingdomCivilWarClanState record = null;
			state?.Clans.TryGetValue(id, out record);
			bool isLeader = string.Equals(id, faction.LeaderClanId, StringComparison.OrdinalIgnoreCase);
			CivilWarPanelClan row = PanelClan(kingdom, clan, record, isLeader ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StanceFactionLeader", "Faction leader") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_StanceSupporter", "{TAG} supporter", ("TAG", demandWord)), isLeader, share(new[] { id }));
			if (row.IsPlayer && !isLeader && record != null && record.SideSinceDay >= 0)
			{
				int ago = CivilWarWorld.CurrentDay() - record.SideSinceDay;
				row.Stance += ago <= 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_JoinedToday", " · joined today") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_JoinedDaysAgo", " · joined {DAYS} days ago", ("DAYS", ago.ToString()));
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
		p.DeadlineUnit = CivilWarUiText.T("AF_KINGDOM_CW_Panel_UnitDays", "d");
		if (f.PendingResponse != null)
		{
			p.DeadlineLabel = f.PendingResponse.Dissolve ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_DissolveOrder", "Dissolution order") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_RoyalLetter", "Royal letter");
			p.DeadlineValue = Math.Max(0, f.PendingResponse.DeadlineDay - day).ToString();
			p.DeadlineNote = playerLeader
				? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteAwaitingYourAnswer", "Awaiting your answer  ·  timeout counts as {OUTCOME}", ("OUTCOME", f.PendingResponse.Dissolve ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_OutcomeDefiance", "defiance") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_OutcomeRefusal", "refusal")))
				: CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteAwaitingLeaderAnswer", "Waiting for the faction leader's answer");
			p.DeadlineUrgent = true;
		}
		else if (!string.IsNullOrWhiteSpace(f.ResolutionError))
		{
			p.DeadlineLabel = CivilWarUiText.T("AF_KINGDOM_CW_Panel_Settlement", "Settlement"); p.DeadlineValue = CivilWarUiText.T("AF_KINGDOM_CW_Panel_ValueError", "Error"); p.DeadlineUnit = ""; p.DeadlineNote = f.ResolutionNeedsReview ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteCheckLog", "State kept; check the log") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteRetryNextDay", "Retry next day"); p.DeadlineUrgent = true;
		}
		else if (f.Stage == KingdomCivilWarStage.OpenWar)
		{
			bool founding = string.IsNullOrWhiteSpace(f.RebelKingdomId);
			p.DeadlineLabel = founding ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_Rebellion", "Rebellion") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_CivilWar", "Civil war");
			p.DeadlineValue = founding ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_ValueFounding", "Founding") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_ValueWeekNumber", "{WEEK}", ("WEEK", Math.Max(1, CivilWarWorld.CurrentWeek() - f.WarStartWeek + 1).ToString()));
			p.DeadlineUnit = founding ? "" : CivilWarUiText.T("AF_KINGDOM_CW_Panel_UnitWeeks", "w");
			p.DeadlineNote = founding ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteWaitingRebelFounding", "Waiting for the rebel kingdom to be founded") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteRebelAtWarWithCrown", "The rebel kingdom has gone to war with the crown");
			p.DeadlineUrgent = true;
		}
		else if (f.Stage == KingdomCivilWarStage.Ultimatum)
		{
			p.DeadlineLabel = CivilWarUiText.T("AF_KINGDOM_CW_Panel_Ultimatum", "Ultimatum");
			p.DeadlineIsUltimatum = true;
			p.DeadlineValue = f.PlayerAnswerPending ? Math.Max(0, f.AnswerDeadlineDay - day).ToString() : CivilWarUiText.T("AF_KINGDOM_CW_Panel_RulingPending", "Under review");
			p.DeadlineUnit = f.PlayerAnswerPending ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_UnitDays", "d") : "";
			p.DeadlineNote = f.PlayerAnswerPending ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteAwaitKingReply", "Waiting for the king's answer within the deadline") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteKingRuling", "The king is deciding");
			p.DeadlineUrgent = true;
		}
		else if (f.WaitingForOtherWar || paused)
		{
			p.DeadlineLabel = f.WaitingForOtherWar ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_ResolvedToRebel", "Resolved to rebel") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_Paused", "Paused"); p.DeadlineValue = CivilWarUiText.T("AF_KINGDOM_CW_Panel_StandingBy", "Standing by"); p.DeadlineUnit = "";
			p.DeadlineNote = f.WaitingForOtherWar ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteWaitingRebellionSlot", "Waiting for a rebellion slot to free up") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteHoldingStillHomeWar", "Holding still while the kingdom is at war");
		}
		else
		{
			int left = f.UltimatumDay - day;
			p.DeadlineLabel = CivilWarUiText.T("AF_KINGDOM_CW_Panel_DeliverUltimatum", "Deliver ultimatum");
			p.DeadlineValue = left > 0 ? left.ToString() : CivilWarUiText.T("AF_KINGDOM_CW_Panel_ValueSoon", "Soon");
			p.DeadlineUnit = left > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_UnitDays", "d") : "";
			p.DeadlineNote = left > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteWillDeliverUltimatum", "Will deliver an ultimatum to the king after that") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteDeliveringUltimatum", "About to deliver the ultimatum");
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
		p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_StepFounded", "Founded"), CivilWarUiText.T("AF_KINGDOM_CW_Panel_WeekOf", "Week {WEEK}", ("WEEK", f.CreatedWeek.ToString())), 1));
		if (delivered) p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_DeliverUltimatum", "Deliver ultimatum"), f.UltimatumDay >= 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_DayOf", "Day {DAY}", ("DAY", f.UltimatumDay.ToString())) : CivilWarUiText.T("AF_KINGDOM_CW_Panel_Delivered", "Delivered"), 1));
		else if (war || pending) p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_DeliverUltimatum", "Deliver ultimatum"), war ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoUltimatum", "No ultimatum") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_CrownRespondedFirst", "The crown responded first"), 0));
		else
		{
			int left = f.UltimatumDay - day;
			string note = f.WaitingForOtherWar || paused ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_Paused", "Paused") : left > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_DaysLeft", "{DAYS} days left", ("DAYS", left.ToString())) : CivilWarUiText.T("AF_KINGDOM_CW_Panel_DeliveringSoon", "Delivering soon");
			p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_DeliverUltimatum", "Deliver ultimatum"), f.Refusals > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteRefusedRetry", "Refused {COUNT} times; raised again  ·  {NOTE}", ("COUNT", f.Refusals.ToString()), ("NOTE", note)) : note, 2));
		}
		if (pending) p.Steps.Add(Step(f.PendingResponse.Dissolve ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_DissolveOrder", "Dissolution order") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_RoyalTalks", "Royal talks"), CivilWarUiText.T("AF_KINGDOM_CW_Panel_DaysLeft", "{DAYS} days left", ("DAYS", Math.Max(0, f.PendingResponse.DeadlineDay - day).ToString())), 2));
		else if (ultimatum) p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_StepKingAnswer", "King's answer"), f.PlayerAnswerPending ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_DaysLeft", "{DAYS} days left", ("DAYS", Math.Max(0, f.AnswerDeadlineDay - day).ToString())) : CivilWarUiText.T("AF_KINGDOM_CW_Panel_RulingPending", "Under review"), 2));
		else if (war) p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_StepKingAnswer", "King's answer"), f.Refusals > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_Refused", "Refused") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoAnswer", "No answer"), 1));
		else p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_StepKingAnswer", "King's answer"), f.Refusals > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_PreviouslyRefused", "Previously refused") : "", 0));
		if (war) p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_Rebellion", "Rebellion"), string.IsNullOrWhiteSpace(f.RebelKingdomId) ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_RebelFounding", "The rebel kingdom is being founded") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_CivilWarWeek", "Civil war week {WEEK}", ("WEEK", Math.Max(1, CivilWarWorld.CurrentWeek() - f.WarStartWeek + 1).ToString())), 2));
		else if (IsPlayerLed(f)) p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_StepManualRebellion", "Manual rebellion"), CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteAwaitYourConfirm", "Waiting for your confirmation"), 0));
		else p.Steps.Add(Step(CivilWarUiText.T("AF_KINGDOM_CW_Panel_StepMayRebel", "May rebel"), f.WaitingForOtherWar ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteResolvedWaitWarEnd", "Resolved; waiting for the war to end") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteDecidedOnRefusal", "Decided after refusal"), 0));
	}

	private static CivilWarPanelStep Step(string name, string note, int state) => new CivilWarPanelStep { Name = name, Note = note, State = state };

	// One line under the demand title, written for the viewer's role.
	private static string DescribeNote(KingdomCivilWarFactionState f, CivilWarDemandDef demand, CivilWarPanelRole role, bool playerFaction)
	{
		CivilWarWarGoal goal = f.WarGoal == 0 ? CivilWarCatalog.EffectiveWarGoal(demand) : (CivilWarWarGoal)f.WarGoal;
		string goalName = goal == CivilWarWarGoal.Usurp ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_GoalUsurp", "Usurpation") : goal == CivilWarWarGoal.Secede ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_GoalSecede", "Independence") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_GoalPressure", "Pressure the throne");
		string text;
		if (f.Stage == KingdomCivilWarStage.OpenWar) text = string.IsNullOrWhiteSpace(f.RebelKingdomId)
			? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteFoundingGoal", "The rebels are founding a kingdom. War goal: {GOAL}.", ("GOAL", goalName))
			: CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteOpenWarGoal", "The rebel kingdom has gone to war with the crown. War goal: {GOAL}.", ("GOAL", goalName));
		else if (f.PendingResponse != null && playerFaction && role == CivilWarPanelRole.Leader)
			text = f.PendingResponse.Dissolve ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteKingDissolveOrder", "The king has ordered the faction dissolved: obey and it disbands, defy and it rebels.")
				: CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteCompensationOffer", "The king offers {OFFER} in compensation for withdrawing the demand and dissolving the faction; if you accept, the compensation goes to your clan.", ("OFFER", f.PendingResponse.Gold > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_GoldAmount", "{AMOUNT} gold", ("AMOUNT", f.PendingResponse.Gold.ToString())) : CivilWarUiText.T("AF_KINGDOM_CW_Panel_InfluenceAmount", "{AMOUNT} influence", ("AMOUNT", f.PendingResponse.Influence.ToString()))));
		else if (f.PendingResponse != null) text = f.PendingResponse.Dissolve ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteDissolveAwaitsLeader", "The king has ordered dissolution; the faction leader may obey or defy.") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteCompensationAwaitsLeader", "The king offers compensation for withdrawing the demand; the faction leader has yet to answer.");
		else if (role == CivilWarPanelRole.King) text = (goal == CivilWarWarGoal.Usurp ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteConcedeAbdication", "Conceding means abdication, with the faction leader taking the throne") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteConcedeHonors", "Conceding will honor the demand")) + CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteRefusalRollSuffix", "; if refused, the faction decides by chance whether to rebel.");
		else if (role == CivilWarPanelRole.Crown) text = CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteCrownFightsWithKing", "If the faction rebels, crown-faction clans fight alongside the king.");
		else if (playerFaction) text = CivilWarUiText.T("AF_KINGDOM_CW_Panel_NotePlayerFactionRisk", "If the king refuses, the faction decides by chance whether to rebel; once a founding request or open civil war begins, member membership is locked.");
		else text = CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteDefaultRisk", "If the king concedes the demand is honored; if refused, the faction decides by chance whether to rebel.");
		if (demand != null && demand.EscalationScale < 1f && f.Stage != KingdomCivilWarStage.OpenWar) text += CivilWarUiText.T("AF_KINGDOM_CW_Panel_NoteLowEscalation", " If refused, the chance of rebellion is lower (×{SCALE}).", ("SCALE", demand.EscalationScale.ToString("0.0")));
		return text;
	}

	private static string FactionStageText(KingdomCivilWarFactionState faction, int week, bool paused, CivilWarAftermath aftermath)
	{
		if (faction.PendingResponse != null) return CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageAwaitingResponse", "Awaiting faction response  ·  {DAYS} days left", ("DAYS", Math.Max(0, faction.PendingResponse.DeadlineDay - CivilWarWorld.CurrentDay()).ToString()));
		switch (faction.Stage)
		{
			case KingdomCivilWarStage.OpenWar:
				if (!string.IsNullOrWhiteSpace(faction.ResolutionError)) return faction.ResolutionNeedsReview ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageSettlementError", "Settlement error  ·  state kept; check the log") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageSettlementIncomplete", "Settlement incomplete  ·  retrying next day");
				return string.IsNullOrWhiteSpace(faction.RebelKingdomId) ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageRebelling", "Rebellion under way  ·  the rebel kingdom is being founded") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageCivilWarWeek", "Civil war  ·  week {WEEK}", ("WEEK", Math.Max(1, week - faction.WarStartWeek + 1).ToString()));
			case KingdomCivilWarStage.Ultimatum:
				return faction.PlayerAnswerPending ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageUltimatumAwaitKing", "Ultimatum  ·  awaiting the king's answer ({DAYS} days left)", ("DAYS", Math.Max(0, faction.AnswerDeadlineDay - CivilWarWorld.CurrentDay()).ToString())) : CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageUltimatumAwaitRuling", "Ultimatum  ·  awaiting a ruling");
			default:
				if (faction.WaitingForOtherWar) return CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageResolvedWaitingSlot", "Resolved to rebel  ·  waiting for a rebellion slot");
				if (paused) return CivilWarUiText.T("AF_KINGDOM_CW_Panel_StagePausedHomeWar", "Paused  ·  holding still while the kingdom is at war");
				int left = faction.UltimatumDay - CivilWarWorld.CurrentDay();
				if (left > 0 && aftermath == CivilWarAftermath.Suppressed) return CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageSuppressed", "Deterred by the suppression  ·  ultimatum in {DAYS} days", ("DAYS", left.ToString()));
				if (left > 0 && aftermath == CivilWarAftermath.Emboldened) return CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageEmboldened", "Emboldened by rebel victories  ·  ultimatum in {DAYS} days", ("DAYS", left.ToString()));
				return left > 0 ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageFormedUltimatumIn", "Formed  ·  ultimatum in {DAYS} days", ("DAYS", left.ToString())) : CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageFormedUltimatumSoon", "Formed  ·  ultimatum imminent");
		}
	}

	private static string RulingText(CivilWarRuling ruling, bool escalate)
	{
		switch (ruling)
		{
			case CivilWarRuling.Accept: return CivilWarUiText.T("AF_KINGDOM_CW_Panel_RulingKingAccepts", "The king accepts");
			case CivilWarRuling.Defer: return CivilWarUiText.T("AF_KINGDOM_CW_Panel_RulingKingDefers", "The king defers");
			default: return escalate ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_RulingKingRefusesRebels", "The king refuses; the faction rises") : CivilWarUiText.T("AF_KINGDOM_CW_Panel_RulingKingRefuses", "The king refuses");
		}
	}

	private static string FactionTag(CivilWarDemandDef demand)
	{
		string name = demand?.FactionNameFormat ?? "";
		int index = name.IndexOf(KingdomCivilWarTexts.Raw("KW_Raw_134"), StringComparison.Ordinal);
		return index >= 0 ? name.Substring(index + 3) : KingdomCivilWarTexts.Raw("KW_Raw_135");
	}

	private static string FactionName(KingdomCivilWarFactionState faction, CivilWarDemandDef demand)
	{
		string leader = CivilWarWorld.ClanName(CivilWarWorld.FindClan(faction?.LeaderClanId));
		string format = demand?.FactionNameFormat;
		// The catalog owns FactionNameFormat; only the no-demand fallback is localized here.
		return string.IsNullOrWhiteSpace(format)
			? CivilWarUiText.T("AF_KINGDOM_CW_Panel_FactionNameFallback", "{LEADER}'s opposition faction", ("LEADER", leader))
			: format.Replace("{leader}", leader);
	}

	// Display-only faction name for the panel. Composes the localized faction tag with the leader clan
	// so English never renders the catalog's Chinese "…为首的反对派" rule text. Stored history, AFEF and
	// weekly material keep using FactionName, which stays on the catalog format.
	private static string FactionDisplayName(KingdomCivilWarFactionState faction, CivilWarDemandDef demand)
	{
		// Check the clan object first: CivilWarWorld.ClanName answers a Chinese "some clan" placeholder
		// for a null clan, which must not leak into the English panel.
		Clan leader = CivilWarWorld.FindClan(faction?.LeaderClanId);
		return CivilWarUiText.T("AF_KINGDOM_CW_Panel_FactionName", "{LEADER}'s {TAG}",
			("LEADER", leader == null ? CivilWarUiText.T("AF_KINGDOM_CW_World_UnknownClan", "an unknown clan") : CivilWarWorld.ClanName(leader)),
			("TAG", CivilWarUiText.DemandTag(demand)));
	}

	// Display projection of a resolution outcome. The catalog keeps its own Chinese rule text for matching
	// and stored facts; the panel renders this localized form, and an outcome this build does not know
	// still shows its real catalog name rather than a blank.
	private static string OutcomeDisplayName(CivilWarOutcomeDef outcome)
	{
		switch (outcome?.Id)
		{
			case CivilWarCatalog.CrownVictoryOutcomeId:
				return CivilWarUiText.T("AF_KINGDOM_CW_Outcome_CrownVictory", "Crown victory");
			case CivilWarCatalog.SecedeOutcomeId:
				return CivilWarUiText.T("AF_KINGDOM_CW_Outcome_RebelsSecede", "Rebels secede");
			case CivilWarCatalog.NegotiatedOutcomeId:
				return CivilWarUiText.T("AF_KINGDOM_CW_Outcome_Negotiated", "Negotiated settlement");
			case "rebels_usurp":
				return CivilWarUiText.T("AF_KINGDOM_CW_Outcome_RebelsUsurp", "Rebels take the throne");
			case "rebels_coerce":
				return CivilWarUiText.T("AF_KINGDOM_CW_Outcome_RebelsCoerce", "Rebels force concessions");
			default:
				return outcome?.Name ?? "";
		}
	}

	private static string SignedRelation(Clan clan, Kingdom kingdom)
	{
		int relation = CivilWarWorld.Relation(clan?.Leader, kingdom?.Leader);
		return relation > 0 ? "+" + relation : relation.ToString();
	}

	// Reuses the shared helper's reserved AF_KINGDOM_CW_Stability_* keys. The helper indexes tiers
	// high-to-low (0 = extremely high), so each named case maps explicitly instead of casting.
	private static string StabilityTierText(int value)
	{
		switch (KingdomStabilityPolicy.GetKingdomStabilityTier(value))
		{
			case KingdomStabilityPolicy.KingdomStabilityTier.ExtremelyHigh: return CivilWarUiText.StabilityTier(0);
			case KingdomStabilityPolicy.KingdomStabilityTier.High: return CivilWarUiText.StabilityTier(1);
			case KingdomStabilityPolicy.KingdomStabilityTier.FairlyHigh: return CivilWarUiText.StabilityTier(2);
			case KingdomStabilityPolicy.KingdomStabilityTier.Average: return CivilWarUiText.StabilityTier(3);
			case KingdomStabilityPolicy.KingdomStabilityTier.Poor: return CivilWarUiText.StabilityTier(4);
			case KingdomStabilityPolicy.KingdomStabilityTier.VeryPoor: return CivilWarUiText.StabilityTier(5);
			default: return CivilWarUiText.StabilityTier(6);
		}
	}

	private static string TopSourceName(KingdomCivilWarClanState record)
	{
		if (record?.Grievance == null || record.Grievance.Count == 0) return "";
		KeyValuePair<string, float> top = record.Grievance.OrderByDescending(x => x.Value).First();
		return top.Value < 1f ? "" : CivilWarUiText.SourceName(top.Key, CivilWarCatalog.FindSource(top.Key)?.Name ?? "");
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
			[CivilWarFeature.Weariness] = Weariness(kingdom) / CivilWarWearinessRules.Max,
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
		if (demand.Target == CivilWarDemandTarget.ImposedPolicy) return KingdomCivilWarTexts.Raw("KW_Raw_136");
		return "";
	}

	// Display-only projection of a faction's demand target. The stored faction.TargetName stays the
	// Chinese rule fact that history, AFEF and weekly material keep using; the panel and menus render
	// a live, localized name instead. Unknown targets fall back to the stored fact so a future demand
	// kind still shows something truthful rather than a blank.
	private static string ResolveDemandTargetDisplay(CivilWarDemandDef demand, KingdomCivilWarFactionState faction, Kingdom kingdom)
	{
		string stored = faction?.TargetName ?? "";
		if (demand == null) return stored;
		switch (demand.Target)
		{
			case CivilWarDemandTarget.EnemyKingdom:
				// FindKingdom returns null for a kingdom destroyed since the demand was recorded, and
				// CivilWarWorld.KingdomName would then answer its Chinese "some kingdom" placeholder.
				// Check the object, not the string, so that case keeps the stored fact instead.
				Kingdom enemy = CivilWarWorld.FindKingdom(faction?.TargetId);
				return enemy == null ? stored : CivilWarWorld.KingdomName(enemy);
			case CivilWarDemandTarget.ImposedPolicy:
				string policy = kingdom?.ActivePolicies?.FirstOrDefault(x => x.StringId == faction?.TargetId)?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(policy)) return policy;
				// The policy may already be repealed, or the caller may not have the kingdom at hand;
				// the recorded name is still the truthful target in that case.
				return string.IsNullOrWhiteSpace(stored) ? CivilWarUiText.T("AF_KINGDOM_CW_Panel_TargetCurrentPolicy", "the current policy") : stored;
			default:
				return stored;
		}
	}

	private static string FormatDemand(CivilWarDemandDef demand, string target)
	{
		return (demand == null ? KingdomCivilWarTexts.Raw("KW_Raw_137") : demand.Text).Replace("{target}", string.IsNullOrWhiteSpace(target) ? KingdomCivilWarTexts.Raw("KW_Raw_138") : target);
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
		// All event/action additions pass here exactly once; stored grievance and decay are unchanged.
		points *= 0.5f;
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
