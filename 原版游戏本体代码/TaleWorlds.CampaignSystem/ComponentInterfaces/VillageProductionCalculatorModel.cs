using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001AB RID: 427
	public abstract class VillageProductionCalculatorModel : MBGameModel<VillageProductionCalculatorModel>
	{
		// Token: 0x06001CE7 RID: 7399
		public abstract float CalculateProductionSpeedOfItemCategory(ItemCategory item);

		// Token: 0x06001CE8 RID: 7400
		public abstract ExplainedNumber CalculateDailyProductionAmount(Village village, ItemObject item);

		// Token: 0x06001CE9 RID: 7401
		public abstract float CalculateDailyFoodProductionAmount(Village village);
	}
}
