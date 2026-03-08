using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001CC RID: 460
	public abstract class SettlementTaxModel : MBGameModel<SettlementTaxModel>
	{
		// Token: 0x1700077B RID: 1915
		// (get) Token: 0x06001DFC RID: 7676
		public abstract float SettlementCommissionRateTown { get; }

		// Token: 0x1700077C RID: 1916
		// (get) Token: 0x06001DFD RID: 7677
		public abstract float SettlementCommissionRateVillage { get; }

		// Token: 0x1700077D RID: 1917
		// (get) Token: 0x06001DFE RID: 7678
		public abstract int SettlementCommissionDecreaseSecurityThreshold { get; }

		// Token: 0x1700077E RID: 1918
		// (get) Token: 0x06001DFF RID: 7679
		public abstract int MaximumDecreaseBasedOnSecuritySecurity { get; }

		// Token: 0x06001E00 RID: 7680
		public abstract float GetTownTaxRatio(Town town);

		// Token: 0x06001E01 RID: 7681
		public abstract float GetVillageTaxRatio(Village village);

		// Token: 0x06001E02 RID: 7682
		public abstract float GetTownCommissionChangeBasedOnSecurity(Town town, float commission);

		// Token: 0x06001E03 RID: 7683
		public abstract ExplainedNumber CalculateTownTax(Town town, bool includeDescriptions = false);

		// Token: 0x06001E04 RID: 7684
		public abstract int CalculateVillageTaxFromIncome(Village village, int marketIncome);
	}
}
