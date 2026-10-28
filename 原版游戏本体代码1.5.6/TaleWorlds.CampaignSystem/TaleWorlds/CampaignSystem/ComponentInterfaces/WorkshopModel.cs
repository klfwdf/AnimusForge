using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class WorkshopModel : MBGameModel<WorkshopModel>
{
	public abstract int DaysForPlayerSaveWorkshopFromBankruptcy { get; }

	public abstract int CapitalLowLimit { get; }

	public abstract int InitialCapital { get; }

	public abstract int DailyExpense { get; }

	public abstract int WarehouseCapacity { get; }

	public abstract int DefaultWorkshopCountInSettlement { get; }

	public abstract int MaximumWorkshopsPlayerCanHave { get; }

	public abstract int GetMaxWorkshopCountForClanTier(int tier);

	public abstract int GetCostForPlayer(Workshop workshop);

	public abstract int GetCostForNotable(Workshop workshop);

	public abstract Hero GetNotableOwnerForWorkshop(Workshop workshop);

	public abstract ExplainedNumber GetEffectiveConversionSpeedOfProduction(Workshop workshop, float speed, bool includeDescriptions);

	public abstract int GetConvertProductionCost(WorkshopType workshopType);

	public abstract bool CanPlayerSellWorkshop(Workshop workshop, out TextObject explanation);

	public abstract float GetTradeXpPerWarehouseProduction(EquipmentElement production);
}
