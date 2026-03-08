using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C2 RID: 450
	public abstract class SettlementFoodModel : MBGameModel<SettlementFoodModel>
	{
		// Token: 0x1700074F RID: 1871
		// (get) Token: 0x06001DA1 RID: 7585
		public abstract int FoodStocksUpperLimit { get; }

		// Token: 0x17000750 RID: 1872
		// (get) Token: 0x06001DA2 RID: 7586
		public abstract int NumberOfProsperityToEatOneFood { get; }

		// Token: 0x17000751 RID: 1873
		// (get) Token: 0x06001DA3 RID: 7587
		public abstract int NumberOfMenOnGarrisonToEatOneFood { get; }

		// Token: 0x17000752 RID: 1874
		// (get) Token: 0x06001DA4 RID: 7588
		public abstract int CastleFoodStockUpperLimitBonus { get; }

		// Token: 0x06001DA5 RID: 7589
		public abstract ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false);
	}
}
