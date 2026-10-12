using TaleWorlds.Localization;

namespace AnimusForge;

// Resolve only at existing display boundaries; raw tiers remain available to facts and saves.
internal static class KingdomStabilityUiText
{
	internal static string Tier(int value)
	{
		string template;
		switch (KingdomStabilityPolicy.GetKingdomStabilityTier(value))
		{
		case KingdomStabilityPolicy.KingdomStabilityTier.ExtremelyHigh:
			template = "{=AF_KINGDOM_CW_Stability_ExtremelyHigh}Extremely high"; break;
		case KingdomStabilityPolicy.KingdomStabilityTier.High:
			template = "{=AF_KINGDOM_CW_Stability_High}High"; break;
		case KingdomStabilityPolicy.KingdomStabilityTier.FairlyHigh:
			template = "{=AF_KINGDOM_CW_Stability_FairlyHigh}Fairly high"; break;
		case KingdomStabilityPolicy.KingdomStabilityTier.Average:
			template = "{=AF_KINGDOM_CW_Stability_Average}Average"; break;
		case KingdomStabilityPolicy.KingdomStabilityTier.Poor:
			template = "{=AF_KINGDOM_CW_Stability_Poor}Poor"; break;
		case KingdomStabilityPolicy.KingdomStabilityTier.VeryPoor:
			template = "{=AF_KINGDOM_CW_Stability_VeryPoor}Very poor"; break;
		default:
			template = "{=AF_KINGDOM_CW_Stability_ExtremelyPoor}Extremely poor"; break;
		}
		return new TextObject(template).ToString();
	}
}
