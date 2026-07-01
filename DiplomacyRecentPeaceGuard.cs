using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

internal static class DiplomacyRecentPeaceGuard
{
	private const double ProtectionSeconds = 45.0;

	private static readonly Dictionary<string, DateTime> RecentPeaceUtcByPair = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

	internal static void RegisterPeace(IFaction faction1, IFaction faction2, string reason)
	{
		string key = BuildPairKey(faction1, faction2);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		RecentPeaceUtcByPair[key] = DateTime.UtcNow;
	}

	internal static bool ShouldBlockEncounterHostility(PartyBase attackerParty, PartyBase defenderParty, string source)
	{
		return ShouldBlockHostility(attackerParty?.MapFaction, defenderParty?.MapFaction, source);
	}

	internal static bool ShouldBlockDeclareWar(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail, string source)
	{
		return ShouldBlockHostility(faction1, faction2, source + ":" + detail);
	}

	internal static bool ShouldBlockDeclareWar(IFaction faction1, IFaction faction2, string source)
	{
		return ShouldBlockHostility(faction1, faction2, source);
	}

	private static bool ShouldBlockHostility(IFaction faction1, IFaction faction2, string source)
	{
		string key = BuildPairKey(faction1, faction2);
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		PruneExpired();
		if (!RecentPeaceUtcByPair.TryGetValue(key, out DateTime registeredUtc))
		{
			return false;
		}
		double ageSeconds = (DateTime.UtcNow - registeredUtc).TotalSeconds;
		if (ageSeconds > ProtectionSeconds)
		{
			RecentPeaceUtcByPair.Remove(key);
			return false;
		}
		return true;
	}

	private static string BuildPairKey(IFaction faction1, IFaction faction2)
	{
		string id1 = GetNormalizedFactionId(faction1);
		string id2 = GetNormalizedFactionId(faction2);
		if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2) || string.Equals(id1, id2, StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		return string.Compare(id1, id2, StringComparison.OrdinalIgnoreCase) <= 0
			? id1 + "|" + id2
			: id2 + "|" + id1;
	}

	private static string GetNormalizedFactionId(IFaction faction)
	{
		IFaction normalized = NormalizeFaction(faction);
		return (normalized?.StringId ?? "").Trim();
	}

	private static IFaction NormalizeFaction(IFaction faction)
	{
		if (faction == null)
		{
			return null;
		}
		try
		{
			IFaction mapFaction = faction.MapFaction;
			if (mapFaction != null)
			{
				return mapFaction;
			}
		}
		catch
		{
		}
		return faction;
	}

	private static void PruneExpired()
	{
		if (RecentPeaceUtcByPair.Count == 0)
		{
			return;
		}
		DateTime now = DateTime.UtcNow;
		List<string> expired = null;
		foreach (KeyValuePair<string, DateTime> item in RecentPeaceUtcByPair)
		{
			if ((now - item.Value).TotalSeconds > ProtectionSeconds)
			{
				if (expired == null)
				{
					expired = new List<string>();
				}
				expired.Add(item.Key);
			}
		}
		if (expired == null)
		{
			return;
		}
		foreach (string key in expired)
		{
			RecentPeaceUtcByPair.Remove(key);
		}
	}
}
