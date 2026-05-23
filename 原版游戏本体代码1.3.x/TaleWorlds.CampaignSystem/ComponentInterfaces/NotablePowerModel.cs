using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001DE RID: 478
	public abstract class NotablePowerModel : MBGameModel<NotablePowerModel>
	{
		// Token: 0x1700079E RID: 1950
		// (get) Token: 0x06001E6C RID: 7788
		public abstract int RegularNotableMaxPowerLevel { get; }

		// Token: 0x1700079F RID: 1951
		// (get) Token: 0x06001E6D RID: 7789
		public abstract int NotableDisappearPowerLimit { get; }

		// Token: 0x06001E6E RID: 7790
		public abstract ExplainedNumber CalculateDailyPowerChangeForHero(Hero hero, bool includeDescriptions = false);

		// Token: 0x06001E6F RID: 7791
		public abstract TextObject GetPowerRankName(Hero hero);

		// Token: 0x06001E70 RID: 7792
		public abstract float GetInfluenceBonusToClan(Hero hero);

		// Token: 0x06001E71 RID: 7793
		public abstract int GetInitialPower(Hero hero);

		// Token: 0x06001E72 RID: 7794
		public abstract int GetInitialNotableSupporterCost(Hero hero);
	}
}
