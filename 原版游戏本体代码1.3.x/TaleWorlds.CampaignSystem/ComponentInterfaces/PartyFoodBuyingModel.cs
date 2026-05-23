using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A9 RID: 425
	public abstract class PartyFoodBuyingModel : MBGameModel<PartyFoodBuyingModel>
	{
		// Token: 0x06001CDD RID: 7389
		public abstract void FindItemToBuy(MobileParty mobileParty, Settlement settlement, out ItemRosterElement itemRosterElement, out float itemElementsPrice);

		// Token: 0x17000723 RID: 1827
		// (get) Token: 0x06001CDE RID: 7390
		public abstract float MinimumDaysFoodToLastWhileBuyingFoodFromTown { get; }

		// Token: 0x17000724 RID: 1828
		// (get) Token: 0x06001CDF RID: 7391
		public abstract float MinimumDaysFoodToLastWhileBuyingFoodFromVillage { get; }

		// Token: 0x17000725 RID: 1829
		// (get) Token: 0x06001CE0 RID: 7392
		public abstract float LowCostFoodPriceAverage { get; }
	}
}
