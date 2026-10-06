using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers;

public static class FeatHelper
{
	public static void ApplyCultureFeat(CultureObject culture, FeatObject feat, ref ExplainedNumber result)
	{
		if (culture.HasFeat(feat))
		{
			if (feat.IncrementType == FeatObject.AdditionType.Add)
			{
				result.Add(feat.EffectBonus, GameTexts.FindText("str_culture"));
			}
			else if (feat.IncrementType == FeatObject.AdditionType.AddFactor)
			{
				result.AddFactor(feat.EffectBonus, GameTexts.FindText("str_culture"));
			}
			else
			{
				Debug.FailedAssert("feat.IncrementType is out of range!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "ApplyCultureFeat", 3475);
			}
		}
	}

	public static void ApplyCultureFeat(PartyBase party, FeatObject feat, ref ExplainedNumber result)
	{
		if (PartyBaseHelper.HasFeat(party, feat))
		{
			CultureObject culture = null;
			if (party.LeaderHero != null)
			{
				culture = party.LeaderHero.Culture;
			}
			else if (party.Culture != null)
			{
				culture = party.Culture;
			}
			else if (party.Owner != null)
			{
				culture = party.Owner.Culture;
			}
			else if (party.Settlement != null)
			{
				culture = party.Settlement.Culture;
			}
			ApplyCultureFeat(culture, feat, ref result);
		}
	}
}
