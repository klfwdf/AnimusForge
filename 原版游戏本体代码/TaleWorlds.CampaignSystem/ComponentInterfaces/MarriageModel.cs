using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D9 RID: 473
	public abstract class MarriageModel : MBGameModel<MarriageModel>
	{
		// Token: 0x06001E47 RID: 7751
		public abstract bool IsCoupleSuitableForMarriage(Hero firstHero, Hero secondHero);

		// Token: 0x06001E48 RID: 7752
		public abstract int GetEffectiveRelationIncrease(Hero firstHero, Hero secondHero);

		// Token: 0x06001E49 RID: 7753
		public abstract Clan GetClanAfterMarriage(Hero firstHero, Hero secondHero);

		// Token: 0x06001E4A RID: 7754
		public abstract bool IsSuitableForMarriage(Hero hero);

		// Token: 0x06001E4B RID: 7755
		public abstract bool IsClanSuitableForMarriage(Clan clan);

		// Token: 0x06001E4C RID: 7756
		public abstract float NpcCoupleMarriageChance(Hero firstHero, Hero secondHero);

		// Token: 0x06001E4D RID: 7757
		public abstract bool ShouldNpcMarriageBetweenClansBeAllowed(Clan consideringClan, Clan targetClan);

		// Token: 0x06001E4E RID: 7758
		public abstract List<Hero> GetAdultChildrenSuitableForMarriage(Hero hero);

		// Token: 0x1700078E RID: 1934
		// (get) Token: 0x06001E4F RID: 7759
		public abstract int MinimumMarriageAgeMale { get; }

		// Token: 0x1700078F RID: 1935
		// (get) Token: 0x06001E50 RID: 7760
		public abstract int MinimumMarriageAgeFemale { get; }
	}
}
