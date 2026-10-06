using TaleWorlds.CampaignSystem.AdvancedStartOptions;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Extensions;

public static class AdvancedStartOptionsExtensions
{
	private const uint Prime = 2654435761u;

	public static TextObject GetSelectedScenarioName(this AdvancedStartOptionsData options)
	{
		if (options.GetOption("Scenario") != null)
		{
			return options.GetDisplayName("Scenario");
		}
		return TextObject.GetEmpty();
	}

	public static string GetStartType(this AdvancedStartOptionsData options)
	{
		if (options.HasValue("StartType"))
		{
			return options.GetValue<string>("StartType");
		}
		return string.Empty;
	}

	public static string GetKingdomId(this AdvancedStartOptionsData options)
	{
		if (options.HasValue("KingdomId"))
		{
			return options.GetValue<string>("KingdomId");
		}
		return string.Empty;
	}

	public static bool IsFastModeEnabled(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<bool>("EnableFastMode");
	}

	public static string GetScenario(this AdvancedStartOptionsData options)
	{
		if (options.HasValue("Scenario"))
		{
			return options.GetValue<string>("Scenario");
		}
		return string.Empty;
	}

	public static string GetLastStandKingdom(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<string>("LastStandKingdomId");
	}

	public static string GetUnitedEmpireUnifierKingdomId(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<string>("UnitedEmpireUnifierKingdomId");
	}

	public static string GetTwoFactionWarFaction1Id(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<string>("TwoFactionWarFaction1Id");
	}

	public static string GetTwoFactionWarFaction2Id(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<string>("TwoFactionWarFaction2Id");
	}

	public static string GetInvasionScenarioFactionId(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<string>("InvasionScenarioFactionId");
	}

	public static bool IsRisenBanditsEnabled(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<bool>("RisenBanditsMultiplier");
	}

	public static bool IsHighRebellionEnabled(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<bool>("HighRebellion");
	}

	public static bool TryGetSeed(this AdvancedStartOptionsData options, out uint seed)
	{
		seed = 0u;
		bool result = false;
		if (options.HasValue("Seed"))
		{
			result = true;
			seed = options.GetValue<uint>("Seed") ^ 0x9E3779B1u;
		}
		return result;
	}

	public static string GetAlternativeCalradiaVariant(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<string>("AlternativeCalradiaVariantId");
	}

	public static bool IsRecruitmentRateModifierEnabled(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<bool>("RecruitmentRate");
	}

	public static bool IsIncreasedGlobalMovementSpeedEnabled(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<bool>("IncreasedGlobalMovementSpeed");
	}

	public static bool IsPersonalShipEnabled(this AdvancedStartOptionsData options)
	{
		return options.TryGetValue<bool>("PersonalShip");
	}

	private static T TryGetValue<T>(this AdvancedStartOptionsData options, string key)
	{
		if (options.HasValue(key))
		{
			return options.GetValue<T>(key);
		}
		return default(T);
	}
}
