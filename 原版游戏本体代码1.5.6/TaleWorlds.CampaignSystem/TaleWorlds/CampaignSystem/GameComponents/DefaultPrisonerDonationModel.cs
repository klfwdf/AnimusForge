using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPrisonerDonationModel : PrisonerDonationModel
{
	public override float CalculateRelationGainAfterHeroPrisonerDonate(PartyBase donatingParty, Hero donatedHero, Settlement donatedSettlement)
	{
		float result = 0f;
		int num = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(donatedHero.CharacterObject, donatingParty.LeaderHero);
		int relation = donatedHero.GetRelation(donatedSettlement.OwnerClan.Leader);
		if (relation <= 0)
		{
			float num2 = 1f - (float)relation / 200f;
			result = (donatedHero.IsKingdomLeader ? (MathF.Min(40f, MathF.Pow(num, 0.5f) * 0.5f) * num2) : ((donatedHero.Clan.Leader != donatedHero) ? (MathF.Min(20f, MathF.Pow(num, 0.5f) * 0.1f) * num2) : (MathF.Min(30f, MathF.Pow(num, 0.5f) * 0.25f) * num2)));
		}
		return result;
	}

	public override float CalculateInfluenceGainAfterPrisonerDonation(PartyBase donatingParty, CharacterObject donatedPrisoner, Settlement donatedSettlement)
	{
		return MathF.Pow(Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(donatedPrisoner, donatingParty.LeaderHero), 0.4f) * 0.2f;
	}

	public override float CalculateInfluenceGainAfterTroopDonation(PartyBase donatingParty, CharacterObject donatedCharacter, Settlement donatedSettlement)
	{
		Hero leaderHero = donatingParty.LeaderHero;
		ExplainedNumber stat = new ExplainedNumber(donatedCharacter.GetPower() / 3f);
		if (leaderHero != null && leaderHero.GetPerkValue(DefaultPerks.Steward.Relocation))
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.Relocation, donatingParty.MobileParty, isPrimaryBonus: true, ref stat);
		}
		return stat.ResultNumber;
	}
}
