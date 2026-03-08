using System;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001EC RID: 492
	public abstract class WorkshopModel : MBGameModel<WorkshopModel>
	{
		// Token: 0x170007A9 RID: 1961
		// (get) Token: 0x06001ED4 RID: 7892
		public abstract int DaysForPlayerSaveWorkshopFromBankruptcy { get; }

		// Token: 0x170007AA RID: 1962
		// (get) Token: 0x06001ED5 RID: 7893
		public abstract int CapitalLowLimit { get; }

		// Token: 0x170007AB RID: 1963
		// (get) Token: 0x06001ED6 RID: 7894
		public abstract int InitialCapital { get; }

		// Token: 0x06001ED7 RID: 7895
		public abstract int GetMaxWorkshopCountForClanTier(int tier);

		// Token: 0x06001ED8 RID: 7896
		public abstract int GetCostForPlayer(Workshop workshop);

		// Token: 0x170007AC RID: 1964
		// (get) Token: 0x06001ED9 RID: 7897
		public abstract int DailyExpense { get; }

		// Token: 0x06001EDA RID: 7898
		public abstract int GetCostForNotable(Workshop workshop);

		// Token: 0x170007AD RID: 1965
		// (get) Token: 0x06001EDB RID: 7899
		public abstract int WarehouseCapacity { get; }

		// Token: 0x170007AE RID: 1966
		// (get) Token: 0x06001EDC RID: 7900
		public abstract int DefaultWorkshopCountInSettlement { get; }

		// Token: 0x170007AF RID: 1967
		// (get) Token: 0x06001EDD RID: 7901
		public abstract int MaximumWorkshopsPlayerCanHave { get; }

		// Token: 0x06001EDE RID: 7902
		public abstract Hero GetNotableOwnerForWorkshop(Workshop workshop);

		// Token: 0x06001EDF RID: 7903
		public abstract ExplainedNumber GetEffectiveConversionSpeedOfProduction(Workshop workshop, float speed, bool includeDescriptions);

		// Token: 0x06001EE0 RID: 7904
		public abstract int GetConvertProductionCost(WorkshopType workshopType);

		// Token: 0x06001EE1 RID: 7905
		public abstract bool CanPlayerSellWorkshop(Workshop workshop, out TextObject explanation);

		// Token: 0x06001EE2 RID: 7906
		public abstract float GetTradeXpPerWarehouseProduction(EquipmentElement production);
	}
}
