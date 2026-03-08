using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C3 RID: 451
	public abstract class SettlementEconomyModel : MBGameModel<SettlementEconomyModel>
	{
		// Token: 0x06001DA7 RID: 7591
		public abstract float GetEstimatedDemandForCategory(Town town, ItemData itemData, ItemCategory category);

		// Token: 0x06001DA8 RID: 7592
		public abstract float GetDailyDemandForCategory(Town town, ItemCategory category, int extraProsperity = 0);

		// Token: 0x06001DA9 RID: 7593
		public abstract float GetDemandChangeFromValue(float purchaseValue);

		// Token: 0x06001DAA RID: 7594
		public abstract ValueTuple<float, float> GetSupplyDemandForCategory(Town town, ItemCategory category, float dailySupply, float dailyDemand, float oldSupply, float oldDemand);

		// Token: 0x06001DAB RID: 7595
		public abstract int GetTownGoldChange(Town town);

		// Token: 0x06001DAC RID: 7596
		public abstract float CalculateDailySettlementBudgetForItemCategory(Town town, float demand, ItemCategory category);
	}
}
