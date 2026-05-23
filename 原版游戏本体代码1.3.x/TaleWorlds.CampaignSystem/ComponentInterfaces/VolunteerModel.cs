using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001AD RID: 429
	public abstract class VolunteerModel : MBGameModel<VolunteerModel>
	{
		// Token: 0x06001CF5 RID: 7413
		public abstract int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101);

		// Token: 0x06001CF6 RID: 7414
		public abstract int MaximumIndexGarrisonCanRecruitFromHero(Settlement settlement, Hero sellerHero);

		// Token: 0x06001CF7 RID: 7415
		public abstract float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement);

		// Token: 0x06001CF8 RID: 7416
		public abstract CharacterObject GetBasicVolunteer(Hero hero);

		// Token: 0x06001CF9 RID: 7417
		public abstract bool CanHaveRecruits(Hero hero);

		// Token: 0x1700072B RID: 1835
		// (get) Token: 0x06001CFA RID: 7418
		public abstract int MaxVolunteerTier { get; }
	}
}
