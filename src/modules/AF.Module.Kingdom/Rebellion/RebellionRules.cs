using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;
internal sealed class RebellionClanFacts
{
	internal bool Exists, KingdomExists, KingdomEliminated, PlayerClan, InKingdom, RulingClan, Eliminated, Bandit, Minor, Rebel, Mercenary, LeaderExists, LeaderAlive, LeaderChild, LeaderPrisoner, SameKingCulture, SameLeaderCulture;
	internal int TownCount, CastleCount, RelationToKing, RelationToLeader, Tier;
	internal float Renown, Strength;
}

internal static class RebellionRules
{
	internal const uint DefaultBackground = 4291609515U;
	internal static string CandidatePreflightNote(RebellionClanFacts f)
	{
		if (!f.Exists)
			return "家族为空。";
		if (!f.KingdomExists || f.KingdomEliminated)
			return "王国不存在或已灭亡。";
		if (f.PlayerClan)
			return "玩家家族不参与该系统的自动叛乱。";
		if (!f.InKingdom)
			return "该家族当前不隶属于此王国。";
		if (f.RulingClan)
			return "执政家族不会作为叛乱候选。";
		if (f.Eliminated)
			return "该家族已灭亡。";
		if (f.Bandit || f.Minor || f.Rebel)
			return "该家族派系类型不适合纳入该叛乱逻辑。";
		if (f.Mercenary)
			return "佣兵家族不会作为叛乱候选。";
		if (!f.LeaderExists || !f.LeaderAlive || f.LeaderChild)
			return "族长状态无效。";
		if (f.LeaderPrisoner)
			return "族长当前被囚，暂不触发带城反出。";
		return "";
	}

	internal static string CandidateNote(RebellionClanFacts f, bool force)
	{
		string preflight = CandidatePreflightNote(f);
		if (preflight.Length > 0)
			return preflight;
		if (f.TownCount + f.CastleCount <= 0)
			return "该家族没有城镇或城堡，不能带城反出。";
		if (!force && f.RelationToKing > -5)
			return "与国王关系未低于 -5，暂不列入自动叛乱候选。";
		return "";
	}

	internal static float CandidateScore(RebellionClanFacts f)
	{
		float score = 0;
		score += Math.Min(900f, Math.Max(0f, f.Renown) * .45f);
		score += Math.Max(0, f.Tier) * 140;
		score += Math.Min(420f, Math.Max(0f, f.Strength) / 4f);
		score += f.TownCount * 140;
		score += f.CastleCount * 70;
		score += Math.Max(0, -f.RelationToKing) * 1.5f;
		if (f.SameKingCulture)
			score += 8f;
		return score;
	}

	internal static string FollowerPreflightNote(bool clan, bool leaderClan, bool sameClan) => !clan || !leaderClan ? "家族为空。" : sameClan ? "主导家族不作为跟随候选。" : "";
	internal static string FollowerLeaderNote(bool leader, bool otherLeader) => leader && otherLeader ? "" : "族长状态无效。";
	internal static float FollowerScore(RebellionClanFacts f)
	{
		float score = 0;
		score += Math.Max(0, f.RelationToLeader) * 3.5f;
		score += Math.Max(0, -f.RelationToKing) * 2.5f;
		if (f.SameLeaderCulture)
			score += 6f;
		if (f.SameKingCulture)
			score -= 4f;
		return score;
	}

	internal static bool FollowerFallback() => false;
	internal static bool FollowerEligible(int king, int leader) => leader - king >= 15;
	internal static bool PassChance(bool forced, float chance, Func<float> random, out float roll)
	{
		roll = 0;
		if (forced)
			return true;
		if (chance <= 0)
			return false;
		roll = random();
		return roll < chance;
	}

	internal static List<T> Sort<T>(IEnumerable<T> candidates, Func<T, bool> eligible, Func<T, float> score, Func<T, string> name) => candidates.OrderByDescending(eligible).ThenByDescending(score).ThenBy(x => name(x) ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	internal static T FirstEligible<T>(IEnumerable<T> candidates, Func<T, bool> eligible)
		where T : class => candidates.FirstOrDefault(eligible);
	internal static bool IsKnownOrLegacy(string id, bool known, bool hasStability) => !string.IsNullOrWhiteSpace(id) && (known || (id.StartsWith("new_kingdom", StringComparison.OrdinalIgnoreCase) && hasStability));
	internal static bool CanDiscontinue(bool exists, bool eliminated, bool player, bool hasSettlements, bool known, bool allowPlayer) => exists && !eliminated && (allowPlayer || !player) && !hasSettlements && known;
	internal static int ColorDistance(uint a, uint b) => Math.Abs((int)((a >> 16) & 255) - (int)((b >> 16) & 255)) + Math.Abs((int)((a >> 8) & 255) - (int)((b >> 8) & 255)) + Math.Abs((int)(a & 255) - (int)(b & 255));
	internal static void SelectColors(List<uint> palette, HashSet<uint> used, uint oldBackground, uint oldIcon, bool validIcon, Func<int, int> random, out uint background, out uint icon)
	{
		if (palette.Count == 0)
		{
			background = oldBackground;
			icon = oldIcon != oldBackground ? oldIcon : 4289374890U;
			return;
		}

		var available = palette.Where(x => !used.Contains(x) && x != oldBackground).ToList();
		if (available.Count == 0)
			available = palette.Where(x => x != oldBackground).ToList();
		if (available.Count == 0)
			available = palette;
		background = available[random(available.Count)];
		uint bg = background;
		var alternatives = palette.Where(x => x != bg).ToList();
		icon = oldIcon;
		if (icon == bg || !validIcon || ColorDistance(bg, icon) < 140)
		{
			icon = alternatives.OrderByDescending(x => ColorDistance(bg, x)).FirstOrDefault();
			if (icon == 0)
				icon = alternatives.FirstOrDefault();
			if (icon == 0 || icon == bg)
				icon = uint.MaxValue;
		}
	}
}

internal sealed class RebelKingdomIdentityOwner
{
	private readonly HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	internal void Mark(string id)
	{
		if (!string.IsNullOrWhiteSpace(id))
			known.Add(id);
	}

	internal bool IsKnown(string id) => !string.IsNullOrWhiteSpace(id) && known.Contains(id);
	internal void Remove(string id) => known.Remove(id);
	internal string[] Snapshot() => known.ToArray();
	internal void Replace(IEnumerable<string> ids)
	{
		known.Clear();
		if (ids != null)
			foreach (string id in ids)
				Mark(id);
	}

	internal void Clear() => known.Clear();
}
