using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000110 RID: 272
	public class DefaultEmissaryModel : EmissaryModel
	{
		// Token: 0x1700064F RID: 1615
		// (get) Token: 0x0600179D RID: 6045 RVA: 0x0006F910 File Offset: 0x0006DB10
		public override int EmissaryRelationBonusForMainClan
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x0600179E RID: 6046 RVA: 0x0006F914 File Offset: 0x0006DB14
		public override bool IsEmissary(Hero hero)
		{
			return (hero.CompanionOf == Clan.PlayerClan || hero.Clan == Clan.PlayerClan) && hero.PartyBelongedTo == null && hero.CurrentSettlement != null && hero.CurrentSettlement.IsFortification && !hero.IsPrisoner && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge;
		}
	}
}
