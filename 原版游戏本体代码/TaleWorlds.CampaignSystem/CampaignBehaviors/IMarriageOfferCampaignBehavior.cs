using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000401 RID: 1025
	public interface IMarriageOfferCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x06003FB6 RID: 16310
		void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden);

		// Token: 0x06003FB7 RID: 16311
		void OnMarriageOfferCanceled(Hero suitor, Hero maiden);

		// Token: 0x06003FB8 RID: 16312
		MBBindingList<TextObject> GetMarriageAcceptedConsequences();

		// Token: 0x06003FB9 RID: 16313
		void OnMarriageOfferAcceptedOnPopUp();

		// Token: 0x06003FBA RID: 16314
		void OnMarriageOfferDeclinedOnPopUp();

		// Token: 0x06003FBB RID: 16315
		bool IsHeroEngaged(Hero hero);
	}
}
