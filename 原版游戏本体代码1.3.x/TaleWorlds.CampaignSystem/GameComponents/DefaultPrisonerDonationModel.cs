using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000141 RID: 321
	public class DefaultPrisonerDonationModel : PrisonerDonationModel
	{
		// Token: 0x06001973 RID: 6515 RVA: 0x0007EFC0 File Offset: 0x0007D1C0
		public override float CalculateRelationGainAfterHeroPrisonerDonate(PartyBase donatingParty, Hero donatedHero, Settlement donatedSettlement)
		{
			float result = 0f;
			int num = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(donatedHero.CharacterObject, donatingParty.LeaderHero);
			int relation = donatedHero.GetRelation(donatedSettlement.OwnerClan.Leader);
			if (relation <= 0)
			{
				float num2 = 1f - (float)relation / 200f;
				if (donatedHero.IsKingdomLeader)
				{
					result = MathF.Min(40f, MathF.Pow((float)num, 0.5f) * 0.5f) * num2;
				}
				else if (donatedHero.Clan.Leader == donatedHero)
				{
					result = MathF.Min(30f, MathF.Pow((float)num, 0.5f) * 0.25f) * num2;
				}
				else
				{
					result = MathF.Min(20f, MathF.Pow((float)num, 0.5f) * 0.1f) * num2;
				}
			}
			return result;
		}

		// Token: 0x06001974 RID: 6516 RVA: 0x0007F094 File Offset: 0x0007D294
		public override float CalculateInfluenceGainAfterPrisonerDonation(PartyBase donatingParty, CharacterObject donatedPrisoner, Settlement donatedSettlement)
		{
			return MathF.Pow((float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(donatedPrisoner, donatingParty.LeaderHero), 0.4f) * 0.2f;
		}

		// Token: 0x06001975 RID: 6517 RVA: 0x0007F0C4 File Offset: 0x0007D2C4
		public override float CalculateInfluenceGainAfterTroopDonation(PartyBase donatingParty, CharacterObject donatedCharacter, Settlement donatedSettlement)
		{
			Hero leaderHero = donatingParty.LeaderHero;
			ExplainedNumber explainedNumber = new ExplainedNumber(donatedCharacter.GetPower() / 3f, false, null);
			if (leaderHero != null && leaderHero.GetPerkValue(DefaultPerks.Steward.Relocation))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.Relocation, donatingParty.MobileParty, true, ref explainedNumber, false);
			}
			return explainedNumber.ResultNumber;
		}
	}
}
