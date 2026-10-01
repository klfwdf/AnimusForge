using System;
using System.Globalization;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

/// <summary>
/// Converts live AF and Bannerlord ruler facts into the reusable observation model.
/// </summary>
internal static class GcczTownRuleMemoryRulerAdapter
{
	internal static SettlementRuleMemoryObservation CreateObservation(
		Settlement settlement,
		Hero ruler,
		int currentDay,
		bool useMinimumDurationFallback)
	{
		CultureObject culture = settlement?.Culture;
		return new SettlementRuleMemoryObservation(
			settlement?.StringId,
			settlement?.Name?.ToString(),
			ruler?.StringId,
			ruler?.Name?.ToString(),
			culture?.StringId,
			culture?.Name?.ToString(),
			ResolvePersonality(ruler),
			currentDay,
			useMinimumDurationFallback);
	}

	internal static bool IsSameHero(Hero first, Hero second)
	{
		return first == second
			|| (!string.IsNullOrWhiteSpace(first?.StringId)
				&& string.Equals(first.StringId, second?.StringId, StringComparison.OrdinalIgnoreCase));
	}

	private static string ResolvePersonality(Hero ruler)
	{
		if (ruler == null)
		{
			return string.Empty;
		}

		string personality = string.Empty;
		if (ruler != Hero.MainHero)
		{
			MyBehavior.GetNpcPersonaForExternal(ruler, out personality, out _);
		}
		string traits = string.Join(
			", ",
			new[]
			{
				"Mercy=" + ruler.GetTraitLevel(DefaultTraits.Mercy).ToString(CultureInfo.InvariantCulture),
				"Valor=" + ruler.GetTraitLevel(DefaultTraits.Valor).ToString(CultureInfo.InvariantCulture),
				"Honor=" + ruler.GetTraitLevel(DefaultTraits.Honor).ToString(CultureInfo.InvariantCulture),
				"Generosity=" + ruler.GetTraitLevel(DefaultTraits.Generosity).ToString(CultureInfo.InvariantCulture),
				"Calculating=" + ruler.GetTraitLevel(DefaultTraits.Calculating).ToString(CultureInfo.InvariantCulture),
			});
		return string.IsNullOrWhiteSpace(personality)
			? traits
			: personality.Trim() + "; reputation traits: " + traits;
	}
}
