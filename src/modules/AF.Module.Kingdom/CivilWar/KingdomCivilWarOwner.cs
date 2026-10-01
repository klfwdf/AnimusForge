using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// Main-thread owner for the v3 civil-war state machine. A kingdom holds up to MaxFactions concurrent factions,
// each with its own demand, grievance, ultimatum and war. Event handlers only add points; all rolls, membership
// changes and game actions happen in the weekly maintenance slice or in explicit player answers.
internal sealed class KingdomCivilWarOwner
{
	private const int MaxHistory = 12;
	private const int MaxClanGrievance = 100;
	// Row caps sized to the kingdom screen at 1080p: ~660px under the summary bar, 30px per clan row,
	// ~430px of faction header per column, two side boxes sharing the right column.
	private const int PanelMemberLimit = 6;
	private const int PanelSideLimit = 7;
	private const string FactionPrefix = "civilwar-faction-";
	private readonly KingdomCivilWarStorage _storage = new KingdomCivilWarStorage();
	// Settlement -> faction id that marked it; owners change during the war, so clearing cannot use the current owner.
	private readonly Dictionary<string, string> _oppositionMarkFaction = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _openWarKingdoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	// Reused on the campaign thread: no per-clan key-array allocation or per-point exponentiation.
	private readonly List<string> _decaySourceKeys = new List<string>(16);
	private readonly Dictionary<string, double> _decayFactors = new Dictionary<string, double>(StringComparer.Ordinal);
	private static readonly double UnknownSourceDailyRetention = Math.Pow(0.85d, 1d / 7d);

	internal KingdomCivilWarStorage Storage { get { return _storage; } }

	internal void Replace(KingdomCivilWarStorage loaded)
	{
		_storage.Kingdoms.Clear();
		_oppositionMarkFaction.Clear();
		_openWarKingdoms.Clear();
		HasUnpromptedPlayerUltimatum = false;
		HasPendingFollowPrompt = false;
		if (loaded == null || loaded.Kingdoms == null) return;
		foreach (KeyValuePair<string, KingdomCivilWarKingdomState> pair in loaded.Kingdoms)
		{
			KingdomCivilWarKingdomState state = Sanitize(pair.Value);
			if (state == null) continue;
			Kingdom live = CivilWarWorld.FindKingdom(state.KingdomId);
			if (live != null && live.IsEliminated) continue;
			_storage.Kingdoms[state.KingdomId] = state;
			foreach (KingdomCivilWarFactionState faction in state.Factions)
			{
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

	internal bool HasTrackedKingdom(Kingdom kingdom)
	{
		KingdomCivilWarKingdomState state = Find(kingdom);
		return state != null && state.Factions.Count > 0;
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
		DecayGrievanceToDay(kingdom, state, CivilWarWorld.CurrentDay());
		int withdrawalThreshold = -1;
		List<string> withdrawnClans = null;
		foreach (Clan clan in (clans ?? Enumerable.Empty<Clan>()).Where(x => x != null && x.Kingdom == kingdom && IsPoliticalClan(x)))
		{
			KingdomCivilWarClanState record = GetOrCreateClan(state, clan, week);
			AddPoints(record, source.Id, points);
			// Support is conditional on events, not permanent immunity from political discontent.
			// Reuse the existing discontent threshold; the player still chooses their own allegiance.
			if (record.Side != KingdomCivilWarSide.Crown || clan == kingdom.RulingClan || clan == Clan.PlayerClan) continue;
			if (withdrawalThreshold < 0) withdrawalThreshold = DuelSettings.BuildCivilWarTuning().DiscontentThreshold;
			if (TotalGrievance(record) < withdrawalThreshold) continue;
			record.Side = KingdomCivilWarSide.Middle;
			record.FactionId = "";
			record.SideSinceWeek = week;
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
	}

	// continue_war pledge: peace with the pledged target before the deadline breaks it once.
	internal void RecordPeace(Kingdom kingdom, IFaction other, int week)
	{
		KingdomCivilWarKingdomState state = Find(kingdom);
		if (state == null || other == null || string.IsNullOrWhiteSpace(state.NoPeaceTargetId)) return;
		if (week > state.NoPeaceUntilWeek) { ClearPledge(state); return; }
		if (!string.Equals(state.NoPeaceTargetId, other.StringId, StringComparison.OrdinalIgnoreCase)) return;
		List<Clan> clans = (state.NoPeaceClanIds ?? new List<string>()).Select(CivilWarWorld.FindClan).Where(x => x != null).ToList();
		ClearPledge(state);
		AddGrievance(kingdom, "broken_pledge", clans, 25f, week, "国王违背承诺，与" + (other.Name?.ToString() ?? "敌国") + "议和");
	}

	private static void ClearPledge(KingdomCivilWarKingdomState state)
	{
		state.NoPeaceTargetId = "";
		state.NoPeaceUntilWeek = 0;
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
			+ "\n\n已被拒绝 " + faction.Refusals + " 次。若不答复，将在第 " + faction.PlayerAnswerDeadlineWeek + " 周自动判定。拒绝可能导致内战。";
	}

	// One campaign event per day. Only stored kingdoms/clans/sources are visited, never the world clan list.
	internal void AdvanceDay(int dayIndex)
	{
		if (dayIndex < 0) return;
		foreach (KingdomCivilWarKingdomState state in _storage.Kingdoms.Values)
			DecayGrievanceToDay(CivilWarWorld.FindKingdom(state.KingdomId), state, dayIndex);
	}

	private void DecayGrievanceToDay(Kingdom kingdom, KingdomCivilWarKingdomState state, int day)
	{
		if (day < 0 || day == state.LastGrievanceDecayDay) return;
		int previousDay = state.LastGrievanceDecayDay;
		state.LastGrievanceDecayDay = day;
		// Disabled periods do not accrue a catch-up bill on re-enable; legacy saves start from today.
		if (previousDay < 0 || day < previousDay || kingdom == null || kingdom.IsEliminated || !DuelSettings.IsCivilWarFactionsEnabled()
			|| (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) || IsActiveRebelKingdom(kingdom)) return;
		int days = day - previousDay;
		_decayFactors.Clear();
		foreach (KingdomCivilWarClanState clan in state.Clans.Values)
		{
			_decaySourceKeys.Clear();
			_decaySourceKeys.AddRange(clan.Grievance.Keys);
			foreach (string sourceId in _decaySourceKeys)
			{
				if (!_decayFactors.TryGetValue(sourceId, out double factor))
				{
					double daily = CivilWarCatalog.FindSource(sourceId)?.DailyRetention ?? UnknownSourceDailyRetention;
					factor = days == 1 ? daily : Math.Pow(daily, days);
					_decayFactors[sourceId] = factor;
				}
				clan.Grievance[sourceId] = CivilWarRules.Clamp((float)(clan.Grievance[sourceId] * factor), 0f, MaxClanGrievance);
			}
		}
		SaveSummary(state);
	}

	// ------------------------------------------------------------ weekly slice

	internal void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> ignoredRecentEvents)
	{
		if (kingdom == null || weekIndex <= 0) return;
		if (kingdom.IsEliminated) { DropKingdom(kingdom.StringId); return; }
		if (!DuelSettings.IsCivilWarFactionsEnabled()) return;
		if (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) return;
		// A rebel kingdom still fighting its crown does not split again; it is tracked once the war is over.
		if (IsActiveRebelKingdom(kingdom)) return;
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, weekIndex);
		if (state.LastAdvancedWeek >= weekIndex) return;
		DecayGrievanceToDay(kingdom, state, CivilWarWorld.CurrentDay());
		RefreshClans(kingdom, state, weekIndex);
		state.LastAdvancedWeek = weekIndex;
		try
		{
			// Snapshot: factions may finish (and be removed) while iterating.
			foreach (KingdomCivilWarFactionState faction in state.Factions.ToList())
			{
				if (!state.Factions.Contains(faction)) continue;
				if (faction.Stage == KingdomCivilWarStage.OpenWar) AdvanceOpenWar(kingdom, state, faction, weekIndex, tuning, adjustStability);
				else AdvanceUltimatum(kingdom, state, faction, weekIndex, stability, tuning, adjustStability);
			}
			RefreshMembership(kingdom, state, weekIndex, tuning, stability);
			TryFormFaction(kingdom, state, weekIndex, stability, tuning);
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[ERROR] weekly owner failed kingdom=" + kingdom.StringId + " error=" + ex);
		}
		SaveSummary(state);
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
	private void TryFormFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, int stability, CivilWarTuning tuning)
	{
		if (!CivilWarFactionRules.CanFormFaction(state.Factions.Count, week, state.CooldownUntilWeek, tuning)) return;
		Clan leader = CivilWarWorld.Vassals(kingdom).Where(x => FactionOfClan(state, x) == null)
			.OrderByDescending(x => TotalGrievance(GetOrCreateClan(state, x, week))).FirstOrDefault();
		if (leader == null) return;
		KingdomCivilWarClanState leaderState = GetOrCreateClan(state, leader, week);
		float grievance = TotalGrievance(leaderState);
		if (grievance < tuning.DiscontentThreshold) return;
		CivilWarRoll roll = CivilWarRules.Roll("form_faction", CivilWarCatalog.FormFaction, BuildFeatures(kingdom, state, null, leader, stability), 1f, tuning, RandomFloat);
		if (!roll.Passed) { AddHistory(state, week, CivilWarWorld.ClanName(leader) + "不满达到阈值，但成派判定未通过（" + roll.Chance.ToString("0.00") + "）"); return; }
		List<string> taken = state.Factions.Select(x => x.DemandId).ToList();
		List<CivilWarDemandDef> eligible = CivilWarCatalog.ValidDemands.Where(x => !CivilWarFactionRules.IsDemandTaken(taken, x.Id) && IsDemandEligible(x, kingdom, leader, state)).ToList();
		CivilWarDemandDef demand = CivilWarDecisions.PickDemand(eligible, leaderState.Grievance, tuning, RandomFloat);
		if (demand == null) return;
		KingdomCivilWarFactionState faction = new KingdomCivilWarFactionState
		{
			Id = FactionPrefix + kingdom.StringId + "-" + (++state.FactionSerial), DemandId = demand.Id, Stage = KingdomCivilWarStage.FactionFormed, StageWeek = week,
			LeaderClanId = leader.StringId ?? "", CreatedWeek = week, UltimatumWeek = week + tuning.UltimatumDelayWeeks,
			TargetId = ResolveTargetId(demand, kingdom, state), TargetName = ResolveTargetName(demand, kingdom, state), WarGoal = (int)CivilWarCatalog.EffectiveWarGoal(demand),
			Grievance = grievance
		};
		if (demand.Target != CivilWarDemandTarget.None && string.IsNullOrWhiteSpace(faction.TargetId)) return;
		state.Factions.Add(faction);
		leaderState.Side = KingdomCivilWarSide.Opposition;
		leaderState.FactionId = faction.Id;
		leaderState.SideSinceWeek = week;
		string name = FactionName(faction, demand);
		AddHistory(state, week, name + "成立，诉求：" + FormatDemand(demand, faction.TargetName));
		WriteMaterial(kingdom, week, "反对派成立：" + name + "，诉求" + FormatDemand(demand, faction.TargetName));
		WriteFact(kingdom, leader.Leader, "civil_war:faction:" + faction.Id, "成立" + name + "，诉求" + FormatDemand(demand, faction.TargetName));
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
		if (week < faction.UltimatumWeek) return;
		faction.Stage = KingdomCivilWarStage.Ultimatum;
		bool playerKing = CivilWarWorld.IsPlayerRuled(kingdom);
		if (playerKing && !faction.PlayerAnswerPending)
		{
			faction.PlayerAnswerPending = true;
			faction.PlayerPrompted = false;
			faction.PlayerAnswerDeadlineWeek = week + tuning.PlayerAnswerWeeks;
			HasUnpromptedPlayerUltimatum = true;
			AddHistory(state, week, FactionName(faction, demand) + "的最后通牒已送达玩家国王，等待明确答复");
			return;
		}
		if (playerKing && faction.PlayerAnswerPending && week < faction.PlayerAnswerDeadlineWeek) return;
		Dictionary<string, float> features = BuildFeatures(kingdom, state, faction, leader, stability);
		CivilWarUltimatumResult ruling = playerKing
			? CivilWarDecisions.RollRefusal(demand, features, faction.Refusals, tuning, RandomFloat, CivilWarAftermathRules.EscalationFactor(CurrentAftermath(state, week)))
			: CivilWarDecisions.RuleOnUltimatum(demand, features, faction.Refusals, tuning, RandomFloat, CivilWarAftermathRules.EscalationFactor(CurrentAftermath(state, week)));
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
			CivilWarEffectContext ctx = BuildContext(kingdom, state, faction, demand, leader, week);
			if (CivilWarEffects.TryApply(demand.AcceptEffectId, ctx, out string reason))
			{
				AddHistory(state, week, "国王接受" + name + "的诉求：" + string.Join("；", ctx.Notes));
				FinishFaction(kingdom, state, faction, week, tuning, adjustStability, 4);
				return true;
			}
			// The demand can no longer be met (e.g. peace already made): nothing left to fight for.
			Dissolve(kingdom, state, faction, week, tuning, "接受" + name + "的诉求时发现已无法兑现（" + reason + "），派系解散");
			return false;
		}
		if (ruling.Ruling == CivilWarRuling.Defer)
		{
			// Stalling has its own limit so a king who always defers cannot hold a faction forever. It is not a refusal:
			// refusals drive escalation and usurp conversion, which a deferral must not.
			faction.Defers++;
			if (faction.Defers >= tuning.MaxRefusals) { Dissolve(kingdom, state, faction, week, tuning, name + "被国王一再拖延，派系瓦解"); return false; }
			RefuseAndReschedule(faction, week, tuning);
			AddHistory(state, week, "国王暂缓答复" + name + "，最后通牒延后");
			return false;
		}
		faction.Refusals++;
		AddGrievanceToFaction(state, faction, "demand_refused", 20f, 8f);
		if (ruling.ConvertToUsurp)
		{
			CivilWarDemandDef usurp = CivilWarCatalog.FindDemand(CivilWarCatalog.UsurpDemandId);
			// Another faction may already claim the throne; then this one keeps its demand.
			if (usurp != null && !state.Factions.Any(x => x != faction && x.DemandId == usurp.Id))
			{
				faction.DemandId = usurp.Id; faction.WarGoal = (int)CivilWarWarGoal.Usurp; faction.TargetId = ""; faction.TargetName = "王位"; demand = usurp;
				AddHistory(state, week, name + "屡遭拒绝，转而要求国王退位");
			}
		}
		// Escalation is decided only by the roll (x0.3 for non-war demands via the catalog EscalationScale).
		if (ruling.Escalate)
		{
			OpenWar(kingdom, state, faction, leader, week, tuning, adjustStability);
			return false;
		}
		if (faction.Refusals >= tuning.MaxRefusals)
		{
			Dissolve(kingdom, state, faction, week, tuning, name + "多次被拒仍未起兵，派系瓦解");
			return false;
		}
		RefuseAndReschedule(faction, week, tuning);
		AddHistory(state, week, "国王拒绝" + name + "的诉求，但升级判定未通过");
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

	// Penalties and the world bulletin fire only when the rebel kingdom really exists (OnRebelKingdomCreated).
	private void OpenWar(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, Clan leader, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
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
		List<Clan> followers = Members(state, faction).Select(x => CivilWarWorld.FindClan(x.ClanId))
			.Where(x => x != null && x != leader && x != Clan.PlayerClan && x.Kingdom == kingdom).ToList();
		faction.WarClanIds = followers.Select(x => x.StringId).Concat(new[] { leader.StringId }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		faction.WarRequestWeek = week;
		faction.RebelKingdomId = "";
		faction.WarStartWeek = 0;
		faction.LastFactionPower = FactionPower(kingdom, faction);
		// A player vassal in this faction is asked once the rebel kingdom exists (see OnRebelKingdomCreated / TakePendingFollowPrompt).
		KingdomCivilWarClanState player;
		faction.PlayerFollowPending = false; faction.PlayerFollowAsked = false; faction.PlayerFollowAccepted = false;
		if (Clan.PlayerClan != null && !CivilWarWorld.IsPlayerRuled(kingdom) && state.Clans.TryGetValue(Clan.PlayerClan.StringId ?? "", out player) && player.Side == KingdomCivilWarSide.Opposition && player.FactionId == faction.Id)
			faction.PlayerFollowPending = true;
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
			// WarStartWeek is only set by the creation callback; without it there is no war to resolve.
			if (week <= faction.WarRequestWeek + tuning.WarRequestTimeoutWeeks) return;
			Dissolve(kingdom, state, faction, week, tuning, name + "的叛军王国未能建立，内战请求作废");
			return;
		}
		Kingdom rebel = CivilWarWorld.FindKingdom(faction.RebelKingdomId);
		float crownPower = CivilWarWorld.Strength(kingdom);
		float rebelPower = CivilWarWorld.Strength(rebel);
		faction.LastFactionPower = CivilWarRules.Clamp(rebelPower / Math.Max(1f, crownPower + rebelPower), 0f, 1f);
		faction.LastWarScore = CivilWarRules.Clamp((rebelPower - crownPower) / Math.Max(1f, rebelPower + crownPower), -1f, 1f);
		if (!CivilWarWorld.IsAlive(rebel)) faction.RebelKingdomDestroyed = true;
		else if (!kingdom.IsAtWarWith(rebel)) faction.EndedByPeace = true;
		int elapsed = Math.Max(0, week - faction.WarStartWeek);
		if (faction.RebelKingdomDestroyed || faction.EndedByPeace) { ResolveWar(kingdom, state, faction, week, tuning, adjustStability); return; }
		if (elapsed < tuning.MinWarWeeks) return;
		Dictionary<string, float> features = WarFeatures(kingdom, state, faction, leader, week, tuning);
		CivilWarRoll endRoll = CivilWarRules.Roll("war_end", CivilWarCatalog.WarEnds, features, elapsed >= tuning.MaxWarWeeks ? 2f : 1f, tuning, RandomFloat);
		if (elapsed >= tuning.MaxWarWeeks || endRoll.Passed) ResolveWar(kingdom, state, faction, week, tuning, adjustStability);
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
		faction.WarStartWeek = Math.Max(week, faction.WarRequestWeek);
		faction.RebelFortShareAtStart = CivilWarWorld.FortificationCount(rebelKingdom) / (float)Math.Max(1, CivilWarWorld.FortificationCount(kingdom));
		Host.AdjustStability(kingdom, -8, "civil_war_outbreak");
		Host.ApplyPrestige(kingdom, -10, "内战爆发");
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		AddHistory(state, week, name + "建立" + CivilWarWorld.KingdomName(rebelKingdom) + "，内战爆发");
		WriteMaterial(kingdom, week, "内战爆发：" + name + "建立" + CivilWarWorld.KingdomName(rebelKingdom) + "，与" + CivilWarWorld.KingdomName(kingdom) + "开战");
		// Only the outbreak reaches the instant bulletin; faction founding and resolution stay weekly material.
		MyBehavior.Instance?.CaptureWorldBulletinCivilWar(state.KingdomId, (state.KingdomId ?? "") + ":" + Math.Max(0, week) + ":outbreak:" + faction.Id);
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
		record.Side = KingdomCivilWarSide.Crown; record.FactionId = ""; record.SideSinceWeek = week;
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
		KingdomCivilWarFactionState faction = FindFaction(factionId, out KingdomCivilWarKingdomState state);
		if (faction == null || faction.Stage != KingdomCivilWarStage.OpenWar || !string.IsNullOrWhiteSpace(faction.RebelKingdomId)) return;
		Kingdom kingdom = CivilWarWorld.FindKingdom(state.KingdomId);
		string name = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId));
		Logger.Log("KingdomCivilWar", "rebellion failed faction=" + faction.Id + " reason=" + (reason ?? ""));
		Dissolve(kingdom, state, faction, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), name + "起兵失败，派系解散：" + CivilWarWorld.Limit(string.IsNullOrWhiteSpace(reason) ? "叛军王国未能建立" : reason, 80));
	}

	private void ResolveWar(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		CivilWarWarGoal goal = faction.WarGoal == 0 ? CivilWarCatalog.EffectiveWarGoal(demand) : (CivilWarWarGoal)faction.WarGoal;
		string log = "forced";
		CivilWarOutcomeDef outcome;
		if (!string.IsNullOrWhiteSpace(faction.ResolutionOutcomeId)) outcome = CivilWarCatalog.FindOutcome(faction.ResolutionOutcomeId);
		else if (faction.RebelKingdomDestroyed) outcome = CivilWarCatalog.FindOutcome(CivilWarCatalog.CrownVictoryOutcomeId);
		else if (faction.EndedByPeace) outcome = CivilWarCatalog.FindOutcome(goal == CivilWarWarGoal.Secede ? CivilWarCatalog.SecedeOutcomeId : CivilWarCatalog.NegotiatedOutcomeId);
		else outcome = CivilWarDecisions.PickOutcome(goal, WarFeatures(kingdom, state, faction, leader, week, tuning), tuning, RandomFloat, out log);
		if (outcome != null) faction.ResolutionOutcomeId = outcome.Id;
		CivilWarEffectContext ctx = BuildContext(kingdom, state, faction, demand, leader, week);
		string reason = "无可用结局";
		bool applied = outcome != null && CivilWarEffects.TryApply(outcome.EffectId, ctx, out reason);
		string name = FactionName(faction, demand);
		string text = applied ? name + "的内战结束（" + outcome.Name + "）：" + string.Join("；", ctx.Notes) : name + "的内战结算失败：" + (reason ?? "无可用结局");
		AddHistory(state, week, text);
		WriteMaterial(kingdom, week, text);
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
		float factor = CivilWarAftermathRules.GrievanceFactor(mood);
		foreach (KingdomCivilWarFactionState other in state.Factions)
		{
			if (!IsPreWar(other)) continue;
			other.UltimatumWeek = Math.Max(other.UltimatumWeek, week + truce);
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
		return CivilWarAftermathRules.Current((CivilWarAftermath)(state?.Aftermath ?? 0), week, state?.AftermathUntilWeek ?? 0);
	}

	// Removes one faction. Its members return to the middle; other factions keep running. New factions wait CooldownWeeks.
	private void FinishFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability, int stabilityDelta)
	{
		if (stabilityDelta != 0 && kingdom != null) adjustStability?.Invoke(kingdom, stabilityDelta);
		state.Factions.Remove(faction);
		state.CooldownUntilWeek = Math.Max(state.CooldownUntilWeek, week + tuning.CooldownWeeks);
		foreach (KingdomCivilWarClanState clan in state.Clans.Values.Where(x => string.Equals(x.FactionId, faction.Id, StringComparison.OrdinalIgnoreCase)))
		{
			clan.Side = KingdomCivilWarSide.Middle;
			clan.FactionId = "";
			clan.SideSinceWeek = week;
		}
		if (Clan.PlayerClan != null && !state.Clans.Values.Any(x => x.ClanId == Clan.PlayerClan.StringId && x.Side == KingdomCivilWarSide.Opposition) && state.PlayerSide == "opposition") state.PlayerSide = "";
		ClearWarMarks(faction.Id);
		if (!state.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) _openWarKingdoms.Remove(state.KingdomId);
		SaveSummary(state);
	}

	private void Dissolve(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning, string reason)
	{
		AddHistory(state, week, reason);
		FinishFaction(kingdom, state, faction, week, tuning, null, 0);
	}

	private static void RefuseAndReschedule(KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning)
	{
		faction.UltimatumWeek = week + tuning.UltimatumDelayWeeks;
		faction.PlayerAnswerPending = false;
		faction.Stage = KingdomCivilWarStage.FactionFormed;
	}

	// ------------------------------------------------------------ player actions (inquiry / dialogue tags)

	// Joining the opposition means joining the speaker's faction, or the strongest pre-war faction if the speaker has none.
	internal bool TryJoinPlayer(Kingdom kingdom, Hero speaker, KingdomCivilWarSide side, out string message)
	{
		message = "";
		if (kingdom == null || Clan.PlayerClan == null || Clan.PlayerClan.Kingdom != kingdom) { message = "你不属于这个王国。"; return false; }
		if (CivilWarWorld.IsPlayerRuled(kingdom)) { message = "国王不能加入派系。"; return false; }
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom)) { message = "该王国受稳定度叛乱免疫保护。"; return false; }
		KingdomCivilWarKingdomState state = Find(kingdom);
		if (state == null || state.Factions.Count == 0) { message = "派系尚未成形。"; return false; }
		if (side == KingdomCivilWarSide.Middle) { message = "玩家只能加入王室派或反对派。"; return false; }
		int week = CivilWarWorld.CurrentWeek();
		KingdomCivilWarClanState player = GetOrCreateClan(state, Clan.PlayerClan, week);
		if (side == KingdomCivilWarSide.Crown)
		{
			player.Side = KingdomCivilWarSide.Crown; player.FactionId = ""; player.SideSinceWeek = week;
			state.PlayerSide = "crown";
			message = "玩家家族已加入王室派。";
		}
		else
		{
			KingdomCivilWarFactionState speakerFaction = speaker?.Clan == null ? null : FactionOfClan(state, speaker.Clan);
			KingdomCivilWarFactionState faction = (IsPreWar(speakerFaction) ? speakerFaction : null)
				?? state.Factions.Where(IsPreWar).OrderByDescending(x => x.LastFactionPower).FirstOrDefault();
			if (faction == null || !IsPreWar(faction)) { message = "当前没有可加入的派系。"; return false; }
			player.Side = KingdomCivilWarSide.Opposition; player.FactionId = faction.Id; player.SideSinceWeek = week;
			state.PlayerSide = "opposition";
			message = "玩家家族已加入" + FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId)) + "。";
		}
		AddHistory(state, week, message);
		return true;
	}

	internal bool TryRecruitClan(Hero recruiter, Clan target, Kingdom kingdom, out string message)
	{
		message = "";
		KingdomCivilWarKingdomState state = Find(kingdom);
		if (recruiter?.Clan == null || target == null || kingdom == null || target.Kingdom != kingdom || recruiter.Clan.Kingdom != kingdom || target == Clan.PlayerClan || target == kingdom.RulingClan || state == null || state.Factions.Count == 0)
		{ message = "当前对象不满足派系招募条件。"; return false; }
		if (state.Factions.Any(x => x.LeaderClanId == target.StringId)) { message = "派系领袖不会被游说。"; return false; }
		int week = CivilWarWorld.CurrentWeek();
		KingdomCivilWarClanState record = GetOrCreateClan(state, target, week);
		bool crown = CivilWarWorld.IsPlayerRuled(kingdom) && recruiter.Clan == kingdom.RulingClan;
		KingdomCivilWarClanState source;
		if (!crown && (!state.Clans.TryGetValue(recruiter.Clan.StringId ?? "", out source) || source.Side == KingdomCivilWarSide.Middle)) { message = "招募者没有明确派系。"; return false; }
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
		record.SideSinceWeek = week;
		record.LastRecruitWeek = week;
		AddHistory(state, week, message);
		return true;
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
		int week = CivilWarWorld.CurrentWeek();
		OpenWar(kingdom, state, faction, speaker.Clan, week, DuelSettings.BuildCivilWarTuning(), HostStability);
		if (faction.Stage != KingdomCivilWarStage.OpenWar) { message = faction.WaitingForOtherWar ? "国内已有内战，派系暂缓起兵。" : "起兵被阻止。"; return faction.WaitingForOtherWar; }
		message = FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId)) + "决定起兵。";
		return true;
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
		if (accept) message = carriedOut ? "已接受最后通牒，派系解散。" : "诉求已无法兑现，派系就此解散。";
		else message = faction.Stage == KingdomCivilWarStage.OpenWar ? "你拒绝了最后通牒，派系起兵了。" : faction.WaitingForOtherWar ? "你拒绝了最后通牒，派系决意起兵，正等待时机。" : "你拒绝了最后通牒，派系暂未起兵。";
		return true;
	}

	private static readonly Action<Kingdom, int> HostStability = (k, d) => Host.AdjustStability(k, d, "civil_war");

	// ------------------------------------------------------------ panel (on demand, when the tab opens)

	internal CivilWarPanelKingdom BuildPlayerKingdomPanel()
	{
		CivilWarPanelKingdom panel = new CivilWarPanelKingdom();
		Kingdom kingdom = Clan.PlayerClan?.Kingdom;
		if (!DuelSettings.IsCivilWarFactionsEnabled()) { panel.EmptyText = "内战派系功能未启用"; return panel; }
		if (kingdom == null || kingdom.IsEliminated) { panel.EmptyText = "你尚未加入任何王国"; return panel; }
		panel.Available = true;
		panel.Name = CivilWarWorld.KingdomName(kingdom);
		panel.Stability = Host.GetStability(kingdom);
		panel.StabilityTier = StabilityTierText(panel.Stability);
		KingdomCivilWarKingdomState state = Find(kingdom);
		List<KingdomCivilWarFactionState> factions = state?.Factions ?? new List<KingdomCivilWarFactionState>();
		if (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) panel.StageText = "玩家王国派系已在设置中关闭";
		else if (factions.Count == 0) panel.StageText = state != null && CivilWarWorld.CurrentWeek() < state.CooldownUntilWeek ? "内战余波平息中（第 " + state.CooldownUntilWeek + " 周前不会成派）" : "尚无派系成形";
		else if (factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) panel.StageText = factions.Count(x => x.Stage == KingdomCivilWarStage.OpenWar) + " 派正在内战";
		else if (factions.Any(x => x.Stage == KingdomCivilWarStage.Ultimatum)) panel.StageText = factions.Count(x => x.Stage == KingdomCivilWarStage.Ultimatum) + " 派发出最后通牒";
		else panel.StageText = factions.Count + " 个派系成形";
		int week = CivilWarWorld.CurrentWeek();
		foreach (KingdomCivilWarFactionState faction in factions) panel.Factions.Add(ToPanelFaction(kingdom, state, faction, week));
		panel.OppositionCount = panel.Factions.Sum(x => x.MemberCount);
		// Everyone outside a faction: crown side first, then undecided clans (incl. the player) by grievance.
		foreach (Clan clan in CivilWarWorld.LandedClans(kingdom))
		{
			KingdomCivilWarClanState record = null;
			state?.Clans.TryGetValue(clan.StringId ?? "", out record);
			if (record != null && record.Side == KingdomCivilWarSide.Opposition && factions.Any(x => x.Id == record.FactionId)) continue;
			int grievance = (int)Math.Round(TotalGrievance(record));
			bool crown = clan == kingdom.RulingClan || record?.Side == KingdomCivilWarSide.Crown;
			string name = clan == Clan.PlayerClan ? CivilWarWorld.ClanName(clan) + "（你）" : CivilWarWorld.ClanName(clan);
			string info = clan == kingdom.RulingClan ? "国王" : clan == Clan.PlayerClan && !crown ? "可在对话中表态" : RelationText(clan, kingdom);
			(crown ? panel.Crown : panel.Middle).Add(new CivilWarPanelClan { Name = name, Info = info, Grievance = grievance, IsLeader = clan == kingdom.RulingClan });
		}
		panel.CrownCount = panel.Crown.Count;
		panel.MiddleCount = panel.Middle.Count;
		panel.Crown = panel.Crown.OrderByDescending(x => x.IsLeader).ThenBy(x => x.Grievance).Take(PanelSideLimit).ToList();
		panel.Middle = panel.Middle.OrderByDescending(x => x.Grievance).Take(PanelSideLimit).ToList();
		return panel;
	}

	private CivilWarPanelFaction ToPanelFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week)
	{
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		CivilWarPanelFaction panel = new CivilWarPanelFaction
		{
			Id = faction.Id,
			Tag = FactionTag(demand),
			Name = FactionName(faction, demand),
			Color = demand?.Color ?? "#6B3A78FF",
			Leader = "首领 " + CivilWarWorld.ClanName(leader) + "  ·  与国王 " + SignedRelation(leader, kingdom),
			Demand = "「" + FormatDemand(demand, faction.TargetName) + "」",
			Goal = GoalText(demand, faction),
			Grievance = (int)Math.Round(CivilWarRules.Clamp(faction.Grievance, 0f, 100f)),
			Stage = FactionStageText(faction, week, IsPausedByOtherWar(state, faction, DuelSettings.BuildCivilWarTuning()), CurrentAftermath(state, week)),
			Refusal = "拒绝 " + faction.Refusals + " 次" + (faction.Defers > 0 ? " · 拖延 " + faction.Defers + " 次" : "") + (faction.LastEscalateChance > 0f ? " · 上次升级 " + (int)Math.Round(faction.LastEscalateChance * 100f) + "%" : "") + (string.IsNullOrWhiteSpace(faction.LastRuling) ? "" : " · " + faction.LastRuling)
		};
		int forts = faction.Stage == KingdomCivilWarStage.OpenWar && !string.IsNullOrWhiteSpace(faction.RebelKingdomId)
			? CivilWarWorld.FortificationCount(CivilWarWorld.FindKingdom(faction.RebelKingdomId))
			: Members(state, faction).Sum(x => CivilWarWorld.FortificationCount(CivilWarWorld.FindClan(x.ClanId)));
		panel.Power = "兵力 " + (int)Math.Round(faction.LastFactionPower * 100f) + "% · " + forts + " 座城池";
		IEnumerable<string> memberIds = faction.Stage == KingdomCivilWarStage.OpenWar ? faction.WarClanIds ?? new List<string>() : Members(state, faction).Select(x => x.ClanId);
		foreach (string id in memberIds.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Clan clan = CivilWarWorld.FindClan(id);
			if (clan == null || clan.IsEliminated) continue;
			KingdomCivilWarClanState record;
			state.Clans.TryGetValue(id, out record);
			string top = TopSourceName(record);
			panel.Members.Add(new CivilWarPanelClan
			{
				Name = CivilWarWorld.ClanName(clan) + (clan == Clan.PlayerClan ? "（你）" : ""),
				Info = string.IsNullOrWhiteSpace(top) ? "" : top,
				Grievance = (int)Math.Round(TotalGrievance(record)),
				IsLeader = string.Equals(id, faction.LeaderClanId, StringComparison.OrdinalIgnoreCase)
			});
		}
		panel.MemberCount = panel.Members.Count;
		panel.Members = panel.Members.OrderByDescending(x => x.IsLeader).ThenByDescending(x => x.Grievance).Take(PanelMemberLimit).ToList();
		return panel;
	}

	private static string FactionStageText(KingdomCivilWarFactionState faction, int week, bool paused, CivilWarAftermath aftermath)
	{
		switch (faction.Stage)
		{
			case KingdomCivilWarStage.OpenWar:
				if (!string.IsNullOrWhiteSpace(faction.ResolutionError)) return faction.ResolutionNeedsReview ? "结算异常 · 已保留状态，需检查日志" : "结算未完成 · 下周重试";
				return string.IsNullOrWhiteSpace(faction.RebelKingdomId) ? "起兵中 · 叛军王国建立中" : "内战 · 第 " + Math.Max(1, week - faction.WarStartWeek + 1) + " 周";
			case KingdomCivilWarStage.Ultimatum:
				return faction.PlayerAnswerPending ? "最后通牒 · 待国王答复（剩 " + Math.Max(0, faction.PlayerAnswerDeadlineWeek - week) + " 周）" : "最后通牒 · 等待裁决";
			default:
				if (faction.WaitingForOtherWar) return "决意起兵 · 等待国内战事结束";
				if (paused) return "暂停 · 国内战事期间按兵不动";
				int left = faction.UltimatumWeek - week;
				if (left > 0 && aftermath == CivilWarAftermath.Suppressed) return "受平叛震慑 · " + left + " 周后提出通牒";
				if (left > 0 && aftermath == CivilWarAftermath.Emboldened) return "受叛军得胜鼓舞 · " + left + " 周后提出通牒";
				return left > 0 ? "已成形 · " + left + " 周后提出通牒" : "已成形 · 即将提出通牒";
		}
	}

	private static string GoalText(CivilWarDemandDef demand, KingdomCivilWarFactionState faction)
	{
		CivilWarWarGoal goal = faction.WarGoal == 0 ? CivilWarCatalog.EffectiveWarGoal(demand) : (CivilWarWarGoal)faction.WarGoal;
		string goalName = goal == CivilWarWarGoal.Usurp ? "夺位" : goal == CivilWarWarGoal.Secede ? "独立" : "逼宫";
		return demand != null && demand.EscalationScale < 1f ? "被拒后起兵概率较低（×" + demand.EscalationScale.ToString("0.0") + "）· 战争目标：" + goalName : "战争目标：" + goalName;
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

	private static string RelationText(Clan clan, Kingdom kingdom) { return "王 " + SignedRelation(clan, kingdom); }

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

	// Weekly, once per kingdom. Middle clans may join the pre-war faction whose demand fits their own grievance;
	// members may drift back to the middle after the side lock. War members are fixed by WarClanIds.
	private void RefreshMembership(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning, int stability)
	{
		List<KingdomCivilWarFactionState> open = state.Factions.Where(IsPreWar).ToList();
		if (open.Count == 0) return;
		float leaveFactor = CivilWarAftermathRules.LeaveFactor(CurrentAftermath(state, week));
		foreach (Clan clan in CivilWarWorld.Vassals(kingdom))
		{
			KingdomCivilWarClanState record = GetOrCreateClan(state, clan, week);
			if (state.Factions.Any(x => x.LeaderClanId == clan.StringId)) continue;
			if (record.Side == KingdomCivilWarSide.Middle)
			{
				int pick = CivilWarFactionRules.PickFactionForClan(open.Select(f => DemandAffinity(CivilWarCatalog.FindDemand(f.DemandId), record) + RelationAffinity(clan, f)).ToList(), tuning, RandomFloat);
				if (pick < 0) continue;
				KingdomCivilWarFactionState target = open[pick];
				if (!CivilWarRules.Roll("join:" + clan.StringId, CivilWarCatalog.JoinOpposition, BuildFeatures(kingdom, state, target, clan, stability), 1f, tuning, RandomFloat).Passed) continue;
				record.Side = KingdomCivilWarSide.Opposition; record.FactionId = target.Id; record.SideSinceWeek = week;
			}
			else if (record.Side == KingdomCivilWarSide.Opposition && week - record.SideSinceWeek >= tuning.SideLockWeeks)
			{
				KingdomCivilWarFactionState current = FactionOfClan(state, clan);
				if (current == null) { record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; continue; }
				if (!IsPreWar(current)) continue;
				if (CivilWarRules.Roll("leave:" + clan.StringId, CivilWarCatalog.LeaveOpposition, BuildFeatures(kingdom, state, current, clan, stability), leaveFactor, tuning, RandomFloat).Passed)
				{ record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; record.SideSinceWeek = week; }
			}
		}
		foreach (KingdomCivilWarFactionState faction in state.Factions) if (IsPreWar(faction)) faction.LastFactionPower = FactionPower(kingdom, state, faction);
	}

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
		float total = state == null ? 0f : state.Clans.Values.Sum(TotalGrievance);
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
			[CivilWarFeature.FactionPower] = faction?.LastFactionPower ?? 0f,
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
		if (!state.Clans.TryGetValue(clan.StringId, out record) || record == null) { record = new KingdomCivilWarClanState { ClanId = clan.StringId, SideSinceWeek = week }; state.Clans[clan.StringId] = record; }
		if (record.Grievance == null) record.Grievance = new Dictionary<string, float>(StringComparer.Ordinal);
		if (record.FactionId == null) record.FactionId = "";
		return record;
	}

	private static void AddPoints(KingdomCivilWarClanState record, string sourceId, float points)
	{
		if (record?.Grievance == null || string.IsNullOrWhiteSpace(sourceId) || points <= 0f) return;
		float current;
		if (!record.Grievance.TryGetValue(sourceId, out current)) current = 0f;
		record.Grievance[sourceId] = CivilWarRules.Clamp(current + points, 0f, MaxClanGrievance);
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
		float members = Members(state, faction).Sum(x => CivilWarWorld.Strength(CivilWarWorld.FindClan(x.ClanId)));
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
		state.LastMaxGrievance = state.Clans.Values.Select(TotalGrievance).DefaultIfEmpty(0f).Max();
		foreach (KingdomCivilWarFactionState faction in state.Factions)
		{
			IEnumerable<KingdomCivilWarClanState> members = faction.Stage == KingdomCivilWarStage.OpenWar
				? (faction.WarClanIds ?? new List<string>()).Select(id => { state.Clans.TryGetValue(id, out KingdomCivilWarClanState r); return r; }).Where(x => x != null)
				: Members(state, faction);
			faction.Grievance = CivilWarFactionRules.FactionGrievance(members.Select(TotalGrievance));
		}
		if (state.Factions.Count == 0) state.Stage = KingdomCivilWarStage.Discontent;
		else state.Stage = (KingdomCivilWarStage)state.Factions.Max(x => (int)x.Stage);
	}

	private static void AddHistory(KingdomCivilWarKingdomState state, int week, string text)
	{
		if (state == null || string.IsNullOrWhiteSpace(text)) return;
		if (state.History == null) state.History = new List<KingdomCivilWarHistoryEntry>();
		state.History.Add(new KingdomCivilWarHistoryEntry { Week = week, Text = CivilWarWorld.Limit(text, 240) });
		while (state.History.Count > MaxHistory) state.History.RemoveAt(0);
	}

	private static void WriteMaterial(Kingdom kingdom, int week, string text) { CivilWarCampaignBehavior.RecordMaterial(kingdom, week, CivilWarWorld.Limit(text, 400)); }
	private static void WriteFact(Kingdom kingdom, Hero hero, string key, string text) { if (hero != null) MyBehavior.RecordNpcActionForExternal(hero, text, key, "civil_war", true, true, kingdom == null ? null : kingdom.Leader, null, CivilWarWorld.KingdomName(kingdom), false, null); }

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

	private static bool IsPoliticalClan(Clan clan) { return clan != null && !clan.IsEliminated && !clan.IsBanditFaction && !clan.IsMinorFaction && !clan.IsUnderMercenaryService && !clan.IsClanTypeMercenary; }
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
		public void QueueRebellion(Kingdom kingdom, Clan leader, List<Clan> followers, string factionId, bool startNow) { MyBehavior.QueueCivilWarRebellionForExternal(kingdom, leader, followers, factionId, startNow); }
		public void ApplyPrestige(Kingdom kingdom, int delta, string reason) { TeamModuleServices.CivilWar.ApplyPrestigeDelta(kingdom == null ? "" : kingdom.StringId, delta, reason); }
		public void RecordMaterial(Kingdom kingdom, int week, string text) { WriteMaterial(kingdom, week, text); }
		public void RecordFact(Kingdom kingdom, Hero hero, string key, string text) { WriteFact(kingdom, hero, key, text); }
	}
}
