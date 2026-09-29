using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

// Game-side lookups shared by the civil war owner and effects. Id lookups go through MBObjectManager
// (hash lookup) with a linear fallback only when the manager misses.
internal static class CivilWarWorld
{
	internal static Clan FindClan(string id)
	{
		string key = (id ?? "").Trim();
		if (key.Length == 0) return null;
		try
		{
			return MBObjectManager.Instance?.GetObject<Clan>(key)
				?? Clan.All?.FirstOrDefault(x => x != null && string.Equals(x.StringId, key, StringComparison.OrdinalIgnoreCase));
		}
		catch { return null; }
	}

	internal static Kingdom FindKingdom(string id)
	{
		string key = (id ?? "").Trim();
		if (key.Length == 0) return null;
		try
		{
			return MBObjectManager.Instance?.GetObject<Kingdom>(key)
				?? Kingdom.All?.FirstOrDefault(x => x != null && string.Equals(x.StringId, key, StringComparison.OrdinalIgnoreCase));
		}
		catch { return null; }
	}

	internal static int CurrentWeek()
	{
		try { return Math.Max(1, (int)(CampaignTime.Now.ToDays / 7.0)); }
		catch { return 1; }
	}

	internal static bool IsPrisoner(Hero hero)
	{
		try { return hero != null && hero.IsPrisoner; }
		catch { return false; }
	}

	internal static bool IsAlive(Kingdom kingdom) => kingdom != null && !kingdom.IsEliminated;

	internal static bool IsPlayerRuled(Kingdom kingdom)
	{
		if (kingdom == null) return false;
		Clan player = Clan.PlayerClan;
		return player != null && kingdom.RulingClan == player || Hero.MainHero != null && kingdom.Leader == Hero.MainHero;
	}

	// Clans that can take part in kingdom politics (not mercenaries, bandits or minor factions).
	internal static IEnumerable<Clan> LandedClans(Kingdom kingdom)
	{
		return (kingdom?.Clans ?? Enumerable.Empty<Clan>()).Where(x => x != null && !x.IsEliminated && !x.IsBanditFaction && !x.IsMinorFaction
			&& !x.IsUnderMercenaryService && !x.IsClanTypeMercenary && x.Leader != null && x.Leader.IsAlive);
	}

	// Politically active clans other than the ruling clan and the player clan.
	internal static IEnumerable<Clan> Vassals(Kingdom kingdom)
	{
		Clan ruling = kingdom?.RulingClan;
		return LandedClans(kingdom).Where(x => x != ruling && x != Clan.PlayerClan);
	}

	internal static int FortificationCount(Clan clan)
	{
		return clan?.Settlements?.Count(x => x != null && (x.IsTown || x.IsCastle)) ?? 0;
	}

	internal static int FortificationCount(Kingdom kingdom)
	{
		return kingdom?.Settlements?.Count(x => x != null && (x.IsTown || x.IsCastle)) ?? 0;
	}

	internal static float Strength(Clan clan)
	{
		try { return Math.Max(0f, clan?.CurrentTotalStrength ?? 0f); }
		catch { return 0f; }
	}

	internal static float Strength(Kingdom kingdom)
	{
		try { return Math.Max(0f, kingdom?.CurrentTotalStrength ?? 0f); }
		catch { return 0f; }
	}

	internal static int Relation(Hero left, Hero right)
	{
		if (left == null || right == null || left == right) return 0;
		try { return left.GetRelation(right); }
		catch { return 0; }
	}

	internal static float Trait(Hero hero, TraitObject trait)
	{
		if (hero == null || trait == null) return 0f;
		try { return CivilWarRules.Trait(hero.GetTraitLevel(trait)); }
		catch { return 0f; }
	}

	internal static int KingdomWarCount(Kingdom kingdom)
	{
		try { return kingdom?.FactionsAtWarWith?.Count(x => x is Kingdom other && !other.IsEliminated) ?? 0; }
		catch { return 0; }
	}

	// Strongest living kingdom currently at war with this one, excluding the given rebel kingdom id.
	internal static Kingdom StrongestEnemy(Kingdom kingdom, string excludeId)
	{
		try
		{
			return kingdom?.FactionsAtWarWith?.OfType<Kingdom>()
				.Where(x => x != null && !x.IsEliminated && !string.Equals(x.StringId, excludeId, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(Strength).FirstOrDefault();
		}
		catch { return null; }
	}

	internal static void MakePeaceSafe(IFaction first, IFaction second, string reason)
	{
		try
		{
			if (first == null || second == null || first == second || first.IsEliminated || second.IsEliminated) return;
			if (!first.IsAtWarWith(second)) return;
			MakePeaceAction.Apply(first, second);
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[WARN] make peace failed reason=" + reason + " error=" + ex.Message);
		}
	}

	internal static void ChangeRelation(Hero hero, Hero other, int delta)
	{
		if (hero == null || other == null || hero == other || delta == 0) return;
		try { ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, other, delta, false); }
		catch (Exception ex) { Logger.Log("KingdomCivilWar", "[WARN] relation change failed: " + ex.Message); }
	}

	internal static string ClanName(Clan clan) => clan?.Name?.ToString() ?? "某家族";

	internal static string KingdomName(Kingdom kingdom) => kingdom?.Name?.ToString() ?? "某王国";

	internal static string FindClanOwnerKingdom(string settlementId)
	{
		try
		{
			Settlement settlement = Settlement.Find(settlementId);
			return settlement?.OwnerClan?.Kingdom?.StringId ?? "";
		}
		catch { return ""; }
	}

	internal static string Limit(string text, int max)
	{
		string value = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		return value.Length <= max ? value : value.Substring(0, max);
	}
}
