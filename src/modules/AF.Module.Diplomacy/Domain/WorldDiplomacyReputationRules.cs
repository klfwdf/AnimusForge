using System;
using System.Collections.Generic;
using AnimusForge;
namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyReputationRules
{
 internal const int DefaultNationalPrestige = 100;
	public static int GetNationalPrestigeRelationTarget(int prestige)
	{
		if (prestige >= 80) return 0;
		if (prestige >= 60) return -2;
		if (prestige >= 40) return -5;
		if (prestige >= 20) return -10;
		if (prestige >= 1) return -15;
		return -20;
	}
	public static int GetNationalPrestige(IReadOnlyDictionary<string, int> prestigeByKingdom, string kingdomId)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		return prestigeByKingdom != null
			&& prestigeByKingdom.TryGetValue(normalizedId, out int value)
			? Math.Max(0, Math.Min(DefaultNationalPrestige, value))
			: DefaultNationalPrestige;
	}
	public static int ApplyNationalPrestigeDelta(
		Dictionary<string, int> prestigeByKingdom,
		string kingdomId,
		int delta,
		WorldDiplomacyDocument sourceDocument,
		string reason,
		Func<string, string> resolveKingdomName)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		int before = GetNationalPrestige(prestigeByKingdom, normalizedId);
		int updated = (int)Math.Max(0L, Math.Min(DefaultNationalPrestige, (long)before + delta));
		prestigeByKingdom[normalizedId] = updated;
		WorldDiplomacyDocumentFactRules.RecordDiplomaticStandingChange(sourceDocument, "national_prestige", normalizedId, before, updated, reason,
				resolveKingdomName);
		return updated;
	}
}
