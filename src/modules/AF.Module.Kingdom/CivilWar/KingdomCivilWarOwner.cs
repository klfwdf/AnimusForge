using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// Weekly main-thread owner. Hot paths only read the dictionaries written here.
internal sealed class KingdomCivilWarOwner
{
	internal const int TensionStability = 40;
	internal const int GrievanceDetonation = 100;
	internal const int PanelPageSize = 8;
	private const int MaterialCharCap = 400;
	private const int SummaryCharCap = 240;

	private readonly KingdomCivilWarStorage _storage = new KingdomCivilWarStorage();
	private readonly Dictionary<string, int> _oppositionLoyaltyBySettlement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _openWarKingdoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	internal KingdomCivilWarStorage Storage => _storage;

	internal void Replace(KingdomCivilWarStorage loaded)
	{
		_storage.Kingdoms.Clear();
		_oppositionLoyaltyBySettlement.Clear();
		_openWarKingdoms.Clear();
		if (loaded?.Kingdoms == null) return;
		foreach (KeyValuePair<string, KingdomCivilWarKingdomState> pair in loaded.Kingdoms)
		{
			KingdomCivilWarKingdomState state = Sanitize(pair.Value);
			if (state == null) continue;
			_storage.Kingdoms[state.KingdomId] = state;
			if (state.Stage == KingdomCivilWarStage.OpenWar)
			{
				_openWarKingdoms.Add(state.KingdomId);
				IndexOppositionSettlements(state);
			}
		}
	}

	internal bool IsInOpenCivilWar(string kingdomId)
	{
		return !string.IsNullOrWhiteSpace(kingdomId) && _openWarKingdoms.Contains(kingdomId.Trim());
	}

	internal int GetOppositionLoyaltyDelta(Settlement settlement)
	{
		string id = settlement?.StringId;
		if (string.IsNullOrWhiteSpace(id)) return 0;
		return _oppositionLoyaltyBySettlement.TryGetValue(id, out int delta) ? delta : 0;
	}

	internal bool TryGet(string kingdomId, out KingdomCivilWarKingdomState state)
	{
		state = null;
		if (string.IsNullOrWhiteSpace(kingdomId)) return false;
		return _storage.Kingdoms.TryGetValue(kingdomId.Trim(), out state) && state != null;
	}

	internal IReadOnlyList<KingdomCivilWarKingdomState> ListForPanel()
	{
		return _storage.Kingdoms.Values
			.Where(x => x != null && x.Stage != KingdomCivilWarStage.None && x.Stage != KingdomCivilWarStage.Reconciled)
			.Where(x => !PlayerKingdomRebellionImmunity.ShouldProtectKingdom(FindKingdom(x.KingdomId)))
			.OrderBy(x => x.KingdomId, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	internal bool HasTrackedKingdom(Kingdom kingdom)
	{
		return kingdom != null && _storage.Kingdoms.ContainsKey(kingdom.StringId ?? "");
	}

	internal void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> recentEvents)
	{
		if (kingdom == null || kingdom.IsEliminated || weekIndex <= 0) return;
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled()) return;
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom)) return;
		string kingdomId = kingdom.StringId ?? "";
		if (string.IsNullOrWhiteSpace(kingdomId)) return;
		if (!_storage.Kingdoms.TryGetValue(kingdomId, out KingdomCivilWarKingdomState state))
		{
			state = new KingdomCivilWarKingdomState { KingdomId = kingdomId, Stage = KingdomCivilWarStage.FactionsFormed, StageWeek = weekIndex };
			EnsureLoyalists(kingdom, state);
			_storage.Kingdoms[kingdomId] = state;
			state.Summary = BuildSummary(kingdom, state);
			return;
		}
		if (state.Stage == KingdomCivilWarStage.Reconciled || state.Stage == KingdomCivilWarStage.OpenWar && state.OpenWarConsequencesApplied && weekIndex >= state.StageWeek + 2)
		{
			ResolveWar(kingdom, state, weekIndex, adjustStability);
			return;
		}
		if (!state.LoyalistsReady) EnsureLoyalists(kingdom, state);
		if (state.Stage == KingdomCivilWarStage.OpenWar) return;
		KingdomCivilWarFactionState active = ActiveFaction(state);
		if (active == null)
		{
			if (stability < TensionStability) TryCreateFaction(kingdom, state, weekIndex, recentEvents);
			state.Summary = BuildSummary(kingdom, state);
			return;
		}
		if (active.CreatedWeek == weekIndex)
		{
			state.Summary = BuildSummary(kingdom, state);
			return;
		}
		AnswerDemand(kingdom, state, active, weekIndex, adjustStability);
		state.Summary = BuildSummary(kingdom, state);
	}

	internal bool TryJoinPlayer(Kingdom kingdom, KingdomCivilWarSide side, out string message)
	{
		message = "";
		Clan player = Clan.PlayerClan;
		if (kingdom == null || player == null || player.Kingdom != kingdom)
		{
			message = "你不属于这个王国，不能加入它的派系。";
			return false;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			message = "玩家统治的王国不进入这套派系。";
			return false;
		}
		if (!TryGet(kingdom.StringId, out KingdomCivilWarKingdomState state) || state.Stage < KingdomCivilWarStage.FactionsFormed)
		{
			message = "这个王国的派系尚未成形。";
			return false;
		}
		if (side == KingdomCivilWarSide.Middle)
		{
			message = "中间派不能作为玩家加入的派系。";
			return false;
		}
		state.PlayerSide = side == KingdomCivilWarSide.Crown ? "crown" : "opposition";
		UpsertClan(state, player.StringId, side, leans: false, bloodShy: false, grievance: 0);
		if (side == KingdomCivilWarSide.Crown && string.IsNullOrWhiteSpace(state.CrownLeaderClanId)) state.CrownLeaderClanId = player.StringId;
		state.Summary = BuildSummary(kingdom, state);
		message = "你的家族已加入" + SideName(side) + "。";
		WriteLeaderMemory(kingdom, state, "玩家家族加入" + SideName(side));
		return true;
	}

	internal bool TryRecruitClan(Hero recruiter, Clan target, Kingdom kingdom, out string message)
	{
		message = "";
		if (recruiter == null || target == null || kingdom == null || target.Kingdom != kingdom || recruiter.Clan?.Kingdom != kingdom)
		{
			message = "说服对象必须与你属于同一个王国。";
			return false;
		}
		if (target == Clan.PlayerClan || target == kingdom.RulingClan)
		{
			message = "不能用这个动作改变玩家家族或执政家族的派系。";
			return false;
		}
		if (!TryGet(kingdom.StringId, out KingdomCivilWarKingdomState state) || state.Stage < KingdomCivilWarStage.FactionsFormed)
		{
			message = "派系尚未成形，无从劝入。";
			return false;
		}
		if (!TrySideOf(state, recruiter.Clan?.StringId, out KingdomCivilWarSide recruiterSide) || recruiterSide == KingdomCivilWarSide.Middle)
		{
			message = "你还没有明确的派系，不能替它拉人。";
			return false;
		}
		Hero leader = target.Leader;
		int towardRecruiter = Relation(leader, recruiter);
		int towardKing = Relation(leader, kingdom.Leader);
		bool accepts = recruiterSide == KingdomCivilWarSide.Opposition
			? towardKing < -5 || towardRecruiter - towardKing >= 15
			: towardKing >= 10 || towardRecruiter >= 20;
		if (!accepts)
		{
			message = GetClanName(target) + "没有被说服。";
			return false;
		}
		UpsertClan(state, target.StringId, recruiterSide, leans: false, bloodShy: false, grievance: recruiterSide == KingdomCivilWarSide.Opposition ? 20 : 0);
		if (recruiterSide == KingdomCivilWarSide.Opposition && string.IsNullOrWhiteSpace(state.OppositionLeaderClanId)) state.OppositionLeaderClanId = target.StringId;
		state.Summary = BuildSummary(kingdom, state);
		message = GetClanName(target) + "加入了" + SideName(recruiterSide) + "。";
		WriteLeaderMemory(kingdom, state, message);
		return true;
	}

	internal bool TryDetonate(Kingdom kingdom, int weekIndex, bool manual, Func<Kingdom, int, int> adjustStability, out string message)
	{
		message = "";
		if (kingdom == null || PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			message = "这个王国不能引爆内战。";
			return false;
		}
		if (!TryGet(kingdom.StringId, out KingdomCivilWarKingdomState state) || state.Stage < KingdomCivilWarStage.FactionsFormed)
		{
			message = "派系尚未成形，不能引爆。";
			return false;
		}
		if (!manual && state.OppositionGrievance < GrievanceDetonation)
		{
			message = "不满尚未满。";
			return false;
		}
		if (state.Stage == KingdomCivilWarStage.OpenWar)
		{
			message = "内战已经爆发。";
			return false;
		}
		KingdomCivilWarStage before = state.Stage;
		state.Stage = KingdomCivilWarStage.OpenWar;
		state.StageWeek = Math.Max(1, weekIndex);
		state.OppositionGrievance = GrievanceDetonation;
		ApplyStageConsequences(kingdom, state, before, state.StageWeek, adjustStability);
		state.Summary = BuildSummary(kingdom, state);
		message = manual ? "你手动引爆了内战。" : "不满达到满值，内战爆发。";
		WriteLeaderMemory(kingdom, state, message);
		return true;
	}

	private static void EnsureLoyalists(Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		Clan crown = kingdom.RulingClan;
		if (crown == null) return;
		state.LoyalistsReady = true;
		state.CrownLeaderClanId = crown.StringId ?? "";
		state.Stage = KingdomCivilWarStage.FactionsFormed;
		KingdomCivilWarFactionState loyalists = state.Factions.FirstOrDefault(x => x.Kind == KingdomCivilWarDemandKind.None);
		if (loyalists == null)
		{
			loyalists = new KingdomCivilWarFactionState { Id = "loyalist", Name = GetClanName(crown) + "为首的保皇派", Kind = KingdomCivilWarDemandKind.None, LeaderClanId = crown.StringId ?? "", CreatedWeek = state.StageWeek };
			state.Factions.Add(loyalists);
		}
		loyalists.ClanIds.Clear();
		loyalists.ClanIds.Add(crown.StringId ?? "");
		UpsertClan(state, crown.StringId, KingdomCivilWarSide.Crown, false, false, 0);
		foreach (Clan clan in LandedClans(kingdom))
		{
			if (clan == crown || clan == Clan.PlayerClan) continue;
			if (Relation(clan.Leader, kingdom.Leader) < 10) continue;
			loyalists.ClanIds.Add(clan.StringId);
			UpsertClan(state, clan.StringId, KingdomCivilWarSide.Crown, false, false, 0);
		}
	}

	private static void TryCreateFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, int weekIndex, IReadOnlyList<string> recentEvents)
	{
		if (state.Factions.Count >= 3) return;
		foreach (string fact in recentEvents ?? Array.Empty<string>())
		{
			string[] parts = (fact ?? "").Split('\t');
			if (parts.Length < 3) continue;
			string key = parts[1];
			if (string.IsNullOrWhiteSpace(key) || state.ConsumedEventKeys.Contains(key)) continue;
			state.ConsumedEventKeys.Add(key);
			KingdomCivilWarDemandKind kind = KindFor(kingdom, parts[0]);
			if (kind == KingdomCivilWarDemandKind.None) return;
			Clan leader = Candidate(kingdom, kind);
			if (leader == null) return;
			bool separatist = kind == KingdomCivilWarDemandKind.Autonomy;
			var faction = new KingdomCivilWarFactionState
			{
				Id = "faction_" + state.Factions.Count,
				Name = NameFor(leader, kind),
				Demand = DemandFor(kind, parts[2]),
				Kind = kind,
				LeaderClanId = leader.StringId ?? "",
				EventKey = key,
				CreatedWeek = weekIndex,
				Separatist = separatist,
				Grievance = 25
			};
			faction.ClanIds.Add(leader.StringId ?? "");
			state.Factions.Add(faction);
			state.ActiveFactionId = faction.Id;
			state.OppositionLeaderClanId = leader.StringId ?? "";
			state.OppositionGrievance = 25;
			state.Reasons.Clear();
			state.Reasons.Add(faction.Name + "因“" + Limit(parts[2], 30) + "”提出：" + faction.Demand);
			UpsertClan(state, leader.StringId, KingdomCivilWarSide.Opposition, false, false, 25);
			WriteMaterial(kingdom, weekIndex, faction.Name + "提出诉求：" + faction.Demand);
			return;
		}
	}

	private static KingdomCivilWarDemandKind KindFor(Kingdom kingdom, string materialKind)
	{
		bool tyrant = Trait(kingdom.Leader, DefaultTraits.Authoritarian) > 0;
		if (tyrant && (materialKind == "player_execution" || materialKind == "clan_destroyed" || materialKind == "siege_aftermath" || materialKind == "kingdom_decision_support")) return KingdomCivilWarDemandKind.Usurpation;
		if (materialKind == "war_declared" || materialKind == "raid_completed" || materialKind == "siege_aftermath")
			return Trait(kingdom.Leader, DefaultTraits.Mercy) > 0 ? KingdomCivilWarDemandKind.MakePeace : KingdomCivilWarDemandKind.ContinueWar;
		if (materialKind == "kingdom_decision_support") return tyrant ? KingdomCivilWarDemandKind.RevokeDecision : KingdomCivilWarDemandKind.KeepDecision;
		if (materialKind == "player_execution" || materialKind == "clan_destroyed") return KingdomCivilWarDemandKind.Redress;
		return KingdomCivilWarDemandKind.None;
	}

	private static Clan Candidate(Kingdom kingdom, KingdomCivilWarDemandKind kind)
	{
		Clan crown = kingdom.RulingClan;
		Clan best = null;
		float bestScore = float.MinValue;
		foreach (Clan clan in LandedClans(kingdom))
		{
			if (clan == crown || clan == Clan.PlayerClan || FortificationCount(clan) <= 0) continue;
			int relation = Relation(clan.Leader, kingdom.Leader);
			if (kind == KingdomCivilWarDemandKind.Autonomy && relation >= -5) continue;
			if (kind == KingdomCivilWarDemandKind.Usurpation && relation >= 0) continue;
			float score = Score(clan, relation);
			if (score > bestScore)
			{
				bestScore = score;
				best = clan;
			}
		}
		if (best == null && kind == KingdomCivilWarDemandKind.Autonomy)
		{
			foreach (Clan clan in LandedClans(kingdom))
			{
				if (clan == crown || clan == Clan.PlayerClan || Relation(clan.Leader, kingdom.Leader) >= -5 || FortificationCount(clan) <= 0) continue;
				return clan;
			}
		}
		return best;
	}

	private void AnswerDemand(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int weekIndex, Action<Kingdom, int> adjustStability)
	{
		bool merciful = Trait(kingdom.Leader, DefaultTraits.Mercy) > 0 || Trait(kingdom.Leader, DefaultTraits.Egalitarian) > 0;
		bool accept = merciful && faction.Grievance < 60 && faction.Kind != KingdomCivilWarDemandKind.Usurpation;
		if (accept)
		{
			faction.Satisfied = true;
			faction.Grievance = 0;
			state.OppositionGrievance = 0;
			state.ActiveFactionId = "";
			adjustStability?.Invoke(kingdom, 4);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(FindClan(faction.LeaderClanId)?.Leader, kingdom.Leader, 10, true);
			WriteMaterial(kingdom, weekIndex, "国王接受了" + faction.Name + "的诉求：" + faction.Demand);
			return;
		}
		faction.Grievance = Math.Min(GrievanceDetonation, faction.Grievance + 25);
		state.OppositionGrievance = faction.Grievance;
		WriteMaterial(kingdom, weekIndex, "国王拒绝了" + faction.Name + "的诉求：" + faction.Demand);
		if (faction.Grievance >= GrievanceDetonation)
		{
			state.Stage = KingdomCivilWarStage.OpenWar;
			state.StageWeek = weekIndex;
			ApplyStageConsequences(kingdom, state, KingdomCivilWarStage.Standoff, weekIndex, (target, delta) => { adjustStability?.Invoke(target, delta); return 0; });
		}
	}

	private void ResolveWar(Kingdom kingdom, KingdomCivilWarKingdomState state, int weekIndex, Action<Kingdom, int> adjustStability)
	{
		KingdomCivilWarFactionState faction = ActiveFaction(state);
		Clan leader = FindClan(faction?.LeaderClanId);
		bool canSplit = faction?.Separatist == true && FortificationCount(leader) > 0;
		bool suppress = !canSplit && (FortificationCount(leader) <= 0 || Trait(kingdom.Leader, DefaultTraits.Authoritarian) > 0);
		if (faction?.Kind == KingdomCivilWarDemandKind.Usurpation && leader != null && !suppress)
		{
			kingdom.RulingClan = leader;
			state.CrownLeaderClanId = leader.StringId ?? "";
			state.Stage = KingdomCivilWarStage.FactionsFormed;
			state.ActiveFactionId = "";
			faction.Satisfied = true;
			ClearWarMarks(kingdom.StringId);
			adjustStability?.Invoke(kingdom, -6);
			WriteMaterial(kingdom, weekIndex, GetClanName(leader) + "迫使国王让出统治权。");
			return;
		}
		if (suppress || leader == null)
		{
			if (faction != null) state.Factions.Remove(faction);
			state.ActiveFactionId = "";
			state.OppositionGrievance = 0;
			state.Stage = KingdomCivilWarStage.FactionsFormed;
			ClearWarMarks(kingdom.StringId);
			adjustStability?.Invoke(kingdom, 5);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leader?.Leader, kingdom.Leader, -20, true);
			WriteMaterial(kingdom, weekIndex, "保皇派镇压了" + (faction?.Name ?? "反对派") + "。");
			return;
		}
		WriteMaterial(kingdom, weekIndex, GetClanName(leader) + "仍控制封地，分离诉求转入现有分裂建国。");
		MyBehavior.QueueCivilWarSplitForExternal(kingdom, leader);
	}

	private static KingdomCivilWarFactionState ActiveFaction(KingdomCivilWarKingdomState state)
	{
		return state.Factions.FirstOrDefault(x => string.Equals(x.Id, state.ActiveFactionId, StringComparison.OrdinalIgnoreCase) && !x.Satisfied);
	}

	private static string NameFor(Clan leader, KingdomCivilWarDemandKind kind)
	{
		if (kind == KingdomCivilWarDemandKind.Usurpation) return GetClanName(leader) + "为首的宣权派";
		if (kind == KingdomCivilWarDemandKind.Autonomy) return GetClanName(leader) + "为首的分离派";
		if (kind == KingdomCivilWarDemandKind.MakePeace) return GetClanName(leader) + "为首的反战派";
		if (kind == KingdomCivilWarDemandKind.ContinueWar) return GetClanName(leader) + "为首的主战派";
		if (kind == KingdomCivilWarDemandKind.Redress) return GetClanName(leader) + "为首的复仇派";
		return GetClanName(leader) + "为首的改革派";
	}

	private static string DemandFor(KingdomCivilWarDemandKind kind, string fact)
	{
		string source = Limit(fact, 24);
		if (kind == KingdomCivilWarDemandKind.Usurpation) return "限制王权，并由本派领袖取代暴君。起因：" + source;
		if (kind == KingdomCivilWarDemandKind.Autonomy) return "给予封地自治。起因：" + source;
		if (kind == KingdomCivilWarDemandKind.MakePeace) return "停止战争并议和。起因：" + source;
		if (kind == KingdomCivilWarDemandKind.ContinueWar) return "继续战争，不许议和。起因：" + source;
		if (kind == KingdomCivilWarDemandKind.Redress) return "赔偿并追究责任。起因：" + source;
		if (kind == KingdomCivilWarDemandKind.RevokeDecision) return "撤销这项决定。起因：" + source;
		return "维持这项决定。起因：" + source;
	}

	private void Crystallize(Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		state.Clans.Clear();
		state.Reasons.Clear();
		Clan crown = kingdom.RulingClan;
		Hero king = kingdom.Leader;
		List<Clan> landed = LandedClans(kingdom).ToList();
		Clan oppositionLeader = null;
		float best = float.MinValue;
		foreach (Clan clan in landed)
		{
			if (clan == crown || clan == Clan.PlayerClan) continue;
			int relation = Relation(clan.Leader, king);
			if (relation >= -5 || FortificationCount(clan) <= 0) continue;
			float score = Score(clan, relation);
			if (score > best)
			{
				best = score;
				oppositionLeader = clan;
			}
		}
		state.CrownLeaderClanId = crown?.StringId ?? "";
		state.OppositionLeaderClanId = oppositionLeader?.StringId ?? "";
		if (crown != null) UpsertClan(state, crown.StringId, KingdomCivilWarSide.Crown, false, false, 0);
		foreach (Clan clan in landed)
		{
			if (clan == crown || clan == Clan.PlayerClan) continue;
			int relation = Relation(clan.Leader, king);
			int leaderGap = oppositionLeader == null ? 0 : Relation(clan.Leader, oppositionLeader.Leader) - relation;
			bool bloodShy = Trait(clan.Leader, DefaultTraits.Honor) > 0 || Trait(clan.Leader, DefaultTraits.Mercy) > 0;
			KingdomCivilWarSide side = KingdomCivilWarSide.Middle;
			if (relation >= 10) side = KingdomCivilWarSide.Crown;
			else if (relation < -5 && FortificationCount(clan) > 0 && !bloodShy) side = KingdomCivilWarSide.Opposition;
			else if (Trait(clan.Leader, DefaultTraits.Calculating) > 0 && FortificationCount(clan) >= 2 && relation < 0 && !bloodShy) side = KingdomCivilWarSide.Opposition;
			UpsertClan(state, clan.StringId, side, side == KingdomCivilWarSide.Middle && leaderGap >= 15, bloodShy, side == KingdomCivilWarSide.Opposition ? Math.Max(0, -relation) : 0);
		}
		if (oppositionLeader != null) state.Reasons.Add(GetClanName(oppositionLeader) + "与国王交恶且握有封地。");
		if (state.Clans.Any(x => x.LeansOpposition)) state.Reasons.Add("部分中间家族与反对派领袖的私交明显高于国王。");
		if (state.Clans.Any(x => x.BloodShy && x.Side == KingdomCivilWarSide.Middle)) state.Reasons.Add("荣誉或仁慈的族长暂不加入流血派系。");
		state.OppositionGrievance = state.Clans.Where(x => x.Side == KingdomCivilWarSide.Opposition).Select(x => x.Grievance).DefaultIfEmpty(0).Max();
	}

	private void RefreshMembership(Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		HashSet<string> live = new HashSet<string>(LandedClans(kingdom).Select(x => x.StringId ?? ""), StringComparer.OrdinalIgnoreCase);
		if (!string.IsNullOrWhiteSpace(state.CrownLeaderClanId)) live.Add(state.CrownLeaderClanId);
		state.Clans.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.ClanId) || !live.Contains(x.ClanId));
		if (!string.IsNullOrWhiteSpace(state.PlayerSide))
		{
			KingdomCivilWarSide side = string.Equals(state.PlayerSide, "opposition", StringComparison.OrdinalIgnoreCase) ? KingdomCivilWarSide.Opposition : KingdomCivilWarSide.Crown;
			UpsertClan(state, Clan.PlayerClan?.StringId, side, false, false, 0);
		}
	}

	private static int GrievanceGain(Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		int opposition = state.Clans.Count(x => x.Side == KingdomCivilWarSide.Opposition);
		if (opposition <= 0) return 0;
		int gain = 8 + Math.Min(12, opposition * 4);
		if (Trait(kingdom.Leader, DefaultTraits.Authoritarian) > 0) gain += 6;
		return gain;
	}

	private static bool CanReconcile(Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		Hero king = kingdom.Leader;
		if (king == null) return false;
		if (Trait(king, DefaultTraits.Mercy) <= 0 && Trait(king, DefaultTraits.Egalitarian) <= 0) return false;
		return state.OppositionGrievance < 60 && state.Clans.Count(x => x.Side == KingdomCivilWarSide.Opposition) <= 1;
	}

	private void DefectFenceSitters(Kingdom kingdom, KingdomCivilWarKingdomState state, int weekIndex)
	{
		if (state.OpenWarConsequencesApplied) return;
		foreach (KingdomCivilWarClanState clanState in state.Clans.Where(x => x.LeansOpposition && x.Side == KingdomCivilWarSide.Middle).ToList())
		{
			Clan clan = FindClan(clanState.ClanId);
			if (clan == null || clan == Clan.PlayerClan) continue;
			clanState.Side = KingdomCivilWarSide.Opposition;
			clanState.LeansOpposition = false;
			clanState.Grievance = Math.Max(clanState.Grievance, 20);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, kingdom.Leader, -10, true);
			WriteFact(kingdom, clan.Leader, "civil_war_defect:" + kingdom.StringId + ":" + clan.StringId + ":" + weekIndex, GetClanName(clan) + "在内战爆发当周倒向反对派。");
		}
	}

	private void ApplyStageConsequences(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarStage before, int weekIndex, Func<Kingdom, int, int> adjustStability)
	{
		bool entered = state.Stage != before;
		if (entered && state.Stage == KingdomCivilWarStage.OpenWar)
		{
			adjustStability?.Invoke(kingdom, -8);
			AdjustPrestige(kingdom, -10);
			_openWarKingdoms.Add(kingdom.StringId);
			IndexOppositionSettlements(state);
			WriteMaterial(kingdom, weekIndex, "内战爆发：" + Limit(state.Summary, MaterialCharCap));
			WriteLeaderMemory(kingdom, state, "内战爆发");
			state.OpenWarConsequencesApplied = true;
		}
		else if (entered && state.Stage == KingdomCivilWarStage.Reconciled)
		{
			adjustStability?.Invoke(kingdom, 4);
			ClearWarMarks(kingdom.StringId);
			WriteMaterial(kingdom, weekIndex, "内战和解：" + GetKingdomName(kingdom) + "的派系暂时散去。");
		}
		else if (state.Stage == KingdomCivilWarStage.Standoff && weekIndex != state.StageWeek)
		{
			adjustStability?.Invoke(kingdom, -2);
		}
	}

	private void IndexOppositionSettlements(KingdomCivilWarKingdomState state)
	{
		foreach (KingdomCivilWarClanState clanState in state.Clans.Where(x => x.Side == KingdomCivilWarSide.Opposition))
		{
			Clan clan = FindClan(clanState.ClanId);
			if (clan?.Settlements == null) continue;
			foreach (Settlement settlement in clan.Settlements)
			{
				if (settlement != null && (settlement.IsTown || settlement.IsCastle))
					_oppositionLoyaltyBySettlement[settlement.StringId] = -1;
			}
		}
	}

	private void ClearWarMarks(string kingdomId)
	{
		_openWarKingdoms.Remove(kingdomId);
		List<string> stale = _oppositionLoyaltyBySettlement.Where(x => FindSettlementOwnerKingdom(x.Key) == kingdomId).Select(x => x.Key).ToList();
		foreach (string id in stale) _oppositionLoyaltyBySettlement.Remove(id);
	}

	private void Remove(string kingdomId)
	{
		_storage.Kingdoms.Remove(kingdomId);
		ClearWarMarks(kingdomId);
	}

	private static void UpsertClan(KingdomCivilWarKingdomState state, string clanId, KingdomCivilWarSide side, bool leans, bool bloodShy, int grievance)
	{
		if (string.IsNullOrWhiteSpace(clanId)) return;
		KingdomCivilWarClanState existing = state.Clans.FirstOrDefault(x => string.Equals(x.ClanId, clanId, StringComparison.OrdinalIgnoreCase));
		if (existing == null)
		{
			state.Clans.Add(new KingdomCivilWarClanState { ClanId = clanId, Side = side, LeansOpposition = leans, BloodShy = bloodShy, Grievance = grievance });
			return;
		}
		existing.Side = side;
		existing.LeansOpposition = leans;
		existing.BloodShy = bloodShy;
		existing.Grievance = Math.Max(existing.Grievance, grievance);
	}

	private static bool TrySideOf(KingdomCivilWarKingdomState state, string clanId, out KingdomCivilWarSide side)
	{
		side = KingdomCivilWarSide.Middle;
		KingdomCivilWarClanState clan = state.Clans.FirstOrDefault(x => string.Equals(x.ClanId, clanId, StringComparison.OrdinalIgnoreCase));
		if (clan == null) return false;
		side = clan.Side;
		return true;
	}

	private static IEnumerable<Clan> LandedClans(Kingdom kingdom)
	{
		return (kingdom?.Clans ?? Enumerable.Empty<Clan>()).Where(x => x != null && !x.IsEliminated && !x.IsBanditFaction && !x.IsMinorFaction && !x.IsUnderMercenaryService && !x.IsClanTypeMercenary && x.Leader != null && x.Leader.IsAlive);
	}

	private static int FortificationCount(Clan clan)
	{
		return clan?.Settlements?.Count(x => x != null && (x.IsTown || x.IsCastle)) ?? 0;
	}

	private static float Score(Clan clan, int relationToKing)
	{
		return Math.Max(0, clan?.Tier ?? 0) * 140f + FortificationCount(clan) * 100f + Math.Max(0, -relationToKing) * 1.5f;
	}

	private static int Relation(Hero left, Hero right)
	{
		if (left == null || right == null || left == right) return 0;
		try { return left.GetRelation(right); }
		catch { return 0; }
	}

	private static int Trait(Hero hero, TraitObject trait)
	{
		if (hero == null || trait == null) return 0;
		try { return hero.GetTraitLevel(trait); }
		catch { return 0; }
	}

	private static void AdjustPrestige(Kingdom kingdom, int delta)
	{
		TeamModuleServices.CivilWar.ApplyPrestigeDelta(kingdom?.StringId, delta, "内战");
	}

	private static void WriteMaterial(Kingdom kingdom, int weekIndex, string text)
	{
		CivilWarCampaignBehavior.RecordMaterial(kingdom, weekIndex, Limit(text, MaterialCharCap));
	}

	private static void WriteLeaderMemory(Kingdom kingdom, KingdomCivilWarKingdomState state, string text)
	{
		WriteFact(kingdom, FindClan(state.OppositionLeaderClanId)?.Leader, "civil_war:" + kingdom.StringId + ":" + state.Stage + ":" + state.StageWeek, text);
		WriteFact(kingdom, kingdom.Leader, "civil_war_crown:" + kingdom.StringId + ":" + state.Stage + ":" + state.StageWeek, text);
	}

	private static void WriteFact(Kingdom kingdom, Hero hero, string key, string text)
	{
		if (hero == null || string.IsNullOrWhiteSpace(text)) return;
		MyBehavior.RecordNpcActionForExternal(hero, text, key, "civil_war", true, true, kingdom?.Leader, null, GetKingdomName(kingdom), false, null);
	}

	internal static CivilWarPanelKingdom ToPanel(KingdomCivilWarKingdomState state)
	{
		Kingdom kingdom = FindKingdom(state?.KingdomId);
		var panel = new CivilWarPanelKingdom
		{
			Name = GetKingdomName(kingdom),
			Stage = StageName(state?.Stage ?? KingdomCivilWarStage.None),
			GrievanceText = (state?.OppositionGrievance ?? 0) + "/100",
			GrievanceBar = Math.Max(0, Math.Min(220, (state?.OppositionGrievance ?? 0) * 220 / 100))
		};
		foreach (KingdomCivilWarFactionState faction in state?.Factions?.Take(3) ?? Enumerable.Empty<KingdomCivilWarFactionState>())
		{
			panel.Factions.Add(new CivilWarPanelFaction
			{
				Name = faction.Name ?? "",
				Demand = string.IsNullOrWhiteSpace(faction.Demand) ? "维持王权" : faction.Demand,
				Members = string.Join("、", faction.ClanIds.Take(4).Select(id => GetClanName(FindClan(id)))),
				Color = ColorFor(faction.Kind),
				GrievanceBar = Math.Max(8, Math.Min(180, faction.Grievance * 180 / 100)),
				Satisfied = faction.Satisfied
			});
		}
		return panel;
	}

	private static string ColorFor(KingdomCivilWarDemandKind kind)
	{
		if (kind == KingdomCivilWarDemandKind.None) return "#1E4C8CFF";
		if (kind == KingdomCivilWarDemandKind.Usurpation) return "#8C1E1EFF";
		if (kind == KingdomCivilWarDemandKind.Autonomy) return "#8C5A12FF";
		if (kind == KingdomCivilWarDemandKind.MakePeace) return "#1E6B4AFF";
		return "#6B3A78FF";
	}

	internal static string FormatPanel(KingdomCivilWarKingdomState state)
	{
		Kingdom kingdom = FindKingdom(state?.KingdomId);
		StringBuilder builder = new StringBuilder();
		builder.Append(GetKingdomName(kingdom));
		builder.Append(" | ").Append(StageName(state.Stage));
		builder.Append(" | 不满 ").Append(state.OppositionGrievance).Append('/').Append(GrievanceDetonation);
		if (!string.IsNullOrWhiteSpace(state.PlayerSide)) builder.Append(" | 玩家在").Append(state.PlayerSide == "opposition" ? "反对派" : "王室派");
		builder.Append('\n').Append(Limit(state.Summary, SummaryCharCap));
		foreach (KingdomCivilWarFactionState faction in state.Factions.Take(3))
		{
			builder.Append('\n').Append(faction.Name);
			if (!string.IsNullOrWhiteSpace(faction.Demand)) builder.Append(" | 诉求：").Append(Limit(faction.Demand, 50));
			if (faction.Satisfied) builder.Append(" | 已满足");
		}
		foreach (KingdomCivilWarSide side in new[] { KingdomCivilWarSide.Crown, KingdomCivilWarSide.Opposition, KingdomCivilWarSide.Middle })
		{
			List<KingdomCivilWarClanState> clans = state.Clans.Where(x => x.Side == side).Take(6).ToList();
			if (clans.Count == 0) continue;
			builder.Append('\n').Append(SideName(side)).Append("：");
			builder.Append(string.Join("、", clans.Select(x => GetClanName(FindClan(x.ClanId)) + (x.LeansOpposition ? "(骑墙)" : ""))));
		}
		if (state.Reasons.Count > 0) builder.Append('\n').Append(string.Join("", state.Reasons.Take(3)));
		return builder.ToString();
	}

	private static string BuildSummary(Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		int crown = state.Clans.Count(x => x.Side == KingdomCivilWarSide.Crown);
		int opposition = state.Clans.Count(x => x.Side == KingdomCivilWarSide.Opposition);
		int middle = state.Clans.Count(x => x.Side == KingdomCivilWarSide.Middle);
		return GetKingdomName(kingdom) + "现为" + StageName(state.Stage) + "。王室 " + crown + " 家，反对派 " + opposition + " 家，中间派 " + middle + " 家。";
	}

	private static KingdomCivilWarKingdomState Sanitize(KingdomCivilWarKingdomState state)
	{
		if (state == null || string.IsNullOrWhiteSpace(state.KingdomId)) return null;
		if (state.Stage == KingdomCivilWarStage.None || state.Stage == KingdomCivilWarStage.Reconciled) return null;
		state.KingdomId = state.KingdomId.Trim();
		state.Clans ??= new List<KingdomCivilWarClanState>();
		state.Reasons ??= new List<string>();
		state.Clans = state.Clans.Where(x => x != null && !string.IsNullOrWhiteSpace(x.ClanId)).Take(32).ToList();
		state.Reasons = state.Reasons.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Limit(x, 40)).Take(3).ToList();
		state.OppositionGrievance = Math.Max(0, Math.Min(GrievanceDetonation, state.OppositionGrievance));
		state.Summary = Limit(state.Summary, SummaryCharCap);
		return state;
	}

	private static string FindSettlementOwnerKingdom(string settlementId)
	{
		Settlement settlement = Settlement.Find(settlementId);
		return settlement?.OwnerClan?.Kingdom?.StringId ?? "";
	}

	private static Kingdom FindKingdom(string kingdomId)
	{
		return Kingdom.All?.FirstOrDefault(x => x != null && string.Equals(x.StringId, kingdomId, StringComparison.OrdinalIgnoreCase));
	}

	private static Clan FindClan(string clanId)
	{
		return Clan.All?.FirstOrDefault(x => x != null && string.Equals(x.StringId, clanId, StringComparison.OrdinalIgnoreCase));
	}

	private static string SideName(KingdomCivilWarSide side)
	{
		if (side == KingdomCivilWarSide.Crown) return "王室派";
		if (side == KingdomCivilWarSide.Opposition) return "反对派";
		return "中间派";
	}

	private static string StageName(KingdomCivilWarStage stage)
	{
		switch (stage)
		{
		case KingdomCivilWarStage.Tension: return "紧张";
		case KingdomCivilWarStage.FactionsFormed: return "派系成形";
		case KingdomCivilWarStage.Standoff: return "对峙";
		case KingdomCivilWarStage.OpenWar: return "内战";
		case KingdomCivilWarStage.Reconciled: return "和解";
		default: return "无";
		}
	}

	private static string GetKingdomName(Kingdom kingdom) => kingdom?.Name?.ToString() ?? "王国";
	private static string GetClanName(Clan clan) => clan?.Name?.ToString() ?? "家族";
	private static string Limit(string text, int max)
	{
		string value = (text ?? "").Trim();
		return value.Length <= max ? value : value.Substring(0, max);
	}
}
