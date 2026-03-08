using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000144 RID: 324
	public class DefaultRansomValueCalculationModel : RansomValueCalculationModel
	{
		// Token: 0x06001983 RID: 6531 RVA: 0x0007F6A0 File Offset: 0x0007D8A0
		public override int PrisonerRansomValue(CharacterObject prisoner, Hero sellerHero = null)
		{
			float roundedResultNumber = (float)Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(prisoner, null, false).RoundedResultNumber;
			float num = 0f;
			float num2 = 0f;
			float num3 = 1f;
			Hero heroObject = prisoner.HeroObject;
			if (((heroObject != null) ? heroObject.Clan : null) != null)
			{
				num = (float)(prisoner.HeroObject.Clan.Tier + 2) * 200f * (prisoner.HeroObject.IsClanLeader ? (prisoner.HeroObject.IsKingdomLeader ? 6f : 2.5f) : 1f);
				num2 = MathF.Sqrt((float)MathF.Max(0, prisoner.HeroObject.Gold)) * 6f;
				if (prisoner.HeroObject.Clan.Kingdom != null)
				{
					int count = prisoner.HeroObject.Clan.Kingdom.Fiefs.Count;
					num3 = (prisoner.HeroObject.MapFaction.IsKingdomFaction ? ((count < 8) ? (((float)count + 1f) / 9f) : (1f + MathF.Sqrt((float)(count - 8)) * 0.1f)) : 1f);
				}
				else
				{
					num3 = 0.5f;
				}
			}
			float num4 = ((prisoner.HeroObject != null) ? (num + num2) : 0f);
			int num5 = (int)((roundedResultNumber + num4) * ((!prisoner.IsHero) ? 0.25f : 1f) * num3);
			if (sellerHero != null)
			{
				if (!prisoner.IsHero)
				{
					if (sellerHero.GetPerkValue(DefaultPerks.Roguery.Manhunter) && sellerHero.PartyBelongedTo != null && !sellerHero.PartyBelongedTo.IsCurrentlyAtSea)
					{
						num5 = MathF.Round((float)num5 + (float)num5 * DefaultPerks.Roguery.Manhunter.PrimaryBonus);
					}
				}
				else if (sellerHero.IsPartyLeader && sellerHero.GetPerkValue(DefaultPerks.Roguery.RansomBroker))
				{
					float num6 = DefaultPerks.Roguery.RansomBroker.PrimaryBonus;
					if (sellerHero.PartyBelongedTo.IsCurrentlyAtSea)
					{
						num6 *= 0.5f;
					}
					num5 = MathF.Round((float)num5 + (float)num5 * num6);
				}
			}
			if (num5 != 0)
			{
				return num5;
			}
			return 1;
		}
	}
}
