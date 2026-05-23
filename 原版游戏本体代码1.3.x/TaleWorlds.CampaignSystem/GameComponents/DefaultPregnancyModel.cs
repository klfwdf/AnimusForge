using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200013F RID: 319
	public class DefaultPregnancyModel : PregnancyModel
	{
		// Token: 0x17000690 RID: 1680
		// (get) Token: 0x06001965 RID: 6501 RVA: 0x0007ECF8 File Offset: 0x0007CEF8
		public override float PregnancyDurationInDays
		{
			get
			{
				return (float)((Campaign.Current.Options.AccelerationMode == GameAccelerationMode.Fast) ? 18 : 36);
			}
		}

		// Token: 0x17000691 RID: 1681
		// (get) Token: 0x06001966 RID: 6502 RVA: 0x0007ED13 File Offset: 0x0007CF13
		public override float MaternalMortalityProbabilityInLabor
		{
			get
			{
				return 0.015f;
			}
		}

		// Token: 0x17000692 RID: 1682
		// (get) Token: 0x06001967 RID: 6503 RVA: 0x0007ED1A File Offset: 0x0007CF1A
		public override float StillbirthProbability
		{
			get
			{
				return 0.01f;
			}
		}

		// Token: 0x17000693 RID: 1683
		// (get) Token: 0x06001968 RID: 6504 RVA: 0x0007ED21 File Offset: 0x0007CF21
		public override float DeliveringFemaleOffspringProbability
		{
			get
			{
				return 0.51f;
			}
		}

		// Token: 0x17000694 RID: 1684
		// (get) Token: 0x06001969 RID: 6505 RVA: 0x0007ED28 File Offset: 0x0007CF28
		public override float DeliveringTwinsProbability
		{
			get
			{
				return 0.03f;
			}
		}

		// Token: 0x0600196A RID: 6506 RVA: 0x0007ED2F File Offset: 0x0007CF2F
		private bool IsHeroAgeSuitableForPregnancy(Hero hero)
		{
			return hero.Age >= 18f && hero.Age <= 45f;
		}

		// Token: 0x0600196B RID: 6507 RVA: 0x0007ED50 File Offset: 0x0007CF50
		public override float GetDailyChanceOfPregnancyForHero(Hero hero)
		{
			int num = hero.Children.Count + 1;
			float num2 = (float)(4 + 4 * hero.Clan.Tier);
			int count = hero.Clan.AliveLords.Count;
			float num3 = ((hero != Hero.MainHero && hero.Spouse != Hero.MainHero) ? Math.Min(1f, (2f * num2 - (float)count) / num2) : 1f);
			float num4 = (1.2f - (hero.Age - 18f) * 0.04f) / (float)(num * num) * 0.12f * num3;
			float baseNumber = ((hero.Spouse != null && this.IsHeroAgeSuitableForPregnancy(hero)) ? num4 : 0f);
			ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber, false, null);
			if (hero.GetPerkValue(DefaultPerks.Charm.Virile) || hero.Spouse.GetPerkValue(DefaultPerks.Charm.Virile))
			{
				explainedNumber.AddFactor(DefaultPerks.Charm.Virile.PrimaryBonus, DefaultPerks.Charm.Virile.Name);
			}
			return explainedNumber.ResultNumber;
		}

		// Token: 0x04000868 RID: 2152
		private const int MinPregnancyAge = 18;

		// Token: 0x04000869 RID: 2153
		private const int MaxPregnancyAge = 45;
	}
}
