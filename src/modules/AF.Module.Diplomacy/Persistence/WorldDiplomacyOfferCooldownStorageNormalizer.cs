using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.Refactor.Persistence;

internal static class WorldDiplomacyOfferCooldownStorageNormalizer
{
	internal const int CurrentSchemaVersion = 1;
	internal const int MaxStoredCooldowns = 2048;

	public static void Normalize(WorldDiplomacyStorage storage)
	{
		if (storage == null) throw new ArgumentNullException(nameof(storage));
		storage.OfferCooldowns ??= new List<WorldDiplomacyOfferCooldown>();
		Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> normalized =
			new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
		foreach (WorldDiplomacyOfferCooldown cooldown in storage.OfferCooldowns)
		{
			if (!TryCreateKey(cooldown, out WorldDiplomacyOfferCooldownKey key)) continue;
			if (normalized.TryGetValue(key, out WorldDiplomacyOfferCooldown existing)
				&& existing.LastFailedRoundDay > cooldown.LastFailedRoundDay) continue;
			normalized[key] = new WorldDiplomacyOfferCooldown
			{
				ProposerKingdomId = key.ProposerKingdomId,
				TargetKingdomId = key.TargetKingdomId,
				Domain = DomainToken(key.Domain),
				LastFailedRoundDay = cooldown.LastFailedRoundDay,
				SourceRoundId = (cooldown.SourceRoundId ?? "").Trim()
			};
		}

		storage.OfferCooldowns = normalized.Values
			.OrderByDescending(x => x.LastFailedRoundDay)
			.ThenBy(x => x.ProposerKingdomId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.TargetKingdomId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.Domain, StringComparer.OrdinalIgnoreCase)
			.Take(MaxStoredCooldowns)
			.ToList();
		storage.OfferCooldownStateSchemaVersion = CurrentSchemaVersion;
	}

	internal static bool TryCreateKey(
		WorldDiplomacyOfferCooldown cooldown,
		out WorldDiplomacyOfferCooldownKey key)
	{
		key = default;
		if (cooldown == null || cooldown.LastFailedRoundDay < 0
			|| !TryParseDomain(cooldown.Domain, out WorldDiplomacyOfferDomain domain)) return false;
		key = new WorldDiplomacyOfferCooldownKey(
			cooldown.ProposerKingdomId,
			cooldown.TargetKingdomId,
			domain);
		return key.IsValid;
	}

	internal static string DomainToken(WorldDiplomacyOfferDomain domain)
	{
		return domain switch
		{
			WorldDiplomacyOfferDomain.Trade => "trade",
			WorldDiplomacyOfferDomain.Alliance => "alliance",
			_ => ""
		};
	}

	private static bool TryParseDomain(string value, out WorldDiplomacyOfferDomain domain)
	{
		string normalized = (value ?? "").Trim();
		if (string.Equals(normalized, "trade", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "propose_trade", StringComparison.OrdinalIgnoreCase))
		{
			domain = WorldDiplomacyOfferDomain.Trade;
			return true;
		}
		if (string.Equals(normalized, "alliance", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "propose_alliance", StringComparison.OrdinalIgnoreCase))
		{
			domain = WorldDiplomacyOfferDomain.Alliance;
			return true;
		}
		domain = WorldDiplomacyOfferDomain.None;
		return false;
	}
}
