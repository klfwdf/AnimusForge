using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IWorkshopWarehouseCampaignBehavior
{
	bool IsGettingInputsFromWarehouse(Workshop workshop);

	void SetIsGettingInputsFromWarehouse(Workshop workshop, bool isActive);

	float GetStockProductionInWarehouseRatio(Workshop workshop);

	void SetStockProductionInWarehouseRatio(Workshop workshop, float percentage);

	float GetWarehouseItemRosterWeight(Settlement settlement);

	bool IsRawMaterialsSufficientInTownMarket(Workshop workshop);

	int GetInputCount(Workshop workshop);

	int GetOutputCount(Workshop workshop);

	ExplainedNumber GetInputDailyChange(Workshop workshop);

	ExplainedNumber GetOutputDailyChange(Workshop workshop);
}
