using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001DD RID: 477
	public abstract class PregnancyModel : MBGameModel<PregnancyModel>
	{
		// Token: 0x06001E65 RID: 7781
		public abstract float GetDailyChanceOfPregnancyForHero(Hero hero);

		// Token: 0x17000799 RID: 1945
		// (get) Token: 0x06001E66 RID: 7782
		public abstract float PregnancyDurationInDays { get; }

		// Token: 0x1700079A RID: 1946
		// (get) Token: 0x06001E67 RID: 7783
		public abstract float MaternalMortalityProbabilityInLabor { get; }

		// Token: 0x1700079B RID: 1947
		// (get) Token: 0x06001E68 RID: 7784
		public abstract float StillbirthProbability { get; }

		// Token: 0x1700079C RID: 1948
		// (get) Token: 0x06001E69 RID: 7785
		public abstract float DeliveringFemaleOffspringProbability { get; }

		// Token: 0x1700079D RID: 1949
		// (get) Token: 0x06001E6A RID: 7786
		public abstract float DeliveringTwinsProbability { get; }
	}
}
