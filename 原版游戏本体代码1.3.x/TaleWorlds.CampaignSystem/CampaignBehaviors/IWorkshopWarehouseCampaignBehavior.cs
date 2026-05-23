using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000410 RID: 1040
	public interface IWorkshopWarehouseCampaignBehavior
	{
		// Token: 0x060040C8 RID: 16584
		bool IsGettingInputsFromWarehouse(Workshop workshop);

		// Token: 0x060040C9 RID: 16585
		void SetIsGettingInputsFromWarehouse(Workshop workshop, bool isActive);

		// Token: 0x060040CA RID: 16586
		float GetStockProductionInWarehouseRatio(Workshop workshop);

		// Token: 0x060040CB RID: 16587
		void SetStockProductionInWarehouseRatio(Workshop workshop, float percentage);

		// Token: 0x060040CC RID: 16588
		float GetWarehouseItemRosterWeight(Settlement settlement);

		// Token: 0x060040CD RID: 16589
		bool IsRawMaterialsSufficientInTownMarket(Workshop workshop);

		// Token: 0x060040CE RID: 16590
		int GetInputCount(Workshop workshop);

		// Token: 0x060040CF RID: 16591
		int GetOutputCount(Workshop workshop);

		// Token: 0x060040D0 RID: 16592
		ExplainedNumber GetInputDailyChange(Workshop workshop);

		// Token: 0x060040D1 RID: 16593
		ExplainedNumber GetOutputDailyChange(Workshop workshop);
	}
}
