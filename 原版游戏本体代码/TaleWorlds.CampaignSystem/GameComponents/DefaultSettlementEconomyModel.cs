using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000148 RID: 328
	public class DefaultSettlementEconomyModel : SettlementEconomyModel
	{
		// Token: 0x17000697 RID: 1687
		// (get) Token: 0x060019A2 RID: 6562 RVA: 0x000808B4 File Offset: 0x0007EAB4
		private DefaultSettlementEconomyModel.CategoryValues CategoryValuesCache
		{
			get
			{
				if (this._categoryValues == null)
				{
					this._categoryValues = new DefaultSettlementEconomyModel.CategoryValues();
				}
				return this._categoryValues;
			}
		}

		// Token: 0x060019A3 RID: 6563 RVA: 0x000808D0 File Offset: 0x0007EAD0
		public override ValueTuple<float, float> GetSupplyDemandForCategory(Town town, ItemCategory category, float dailySupply, float dailyDemand, float oldSupply, float oldDemand)
		{
			float num = oldSupply * 0.85f + dailySupply * 0.15f;
			float item = oldDemand * 0.85f + dailyDemand * 0.15f;
			num = MathF.Max(0.1f, num);
			return new ValueTuple<float, float>(num, item);
		}

		// Token: 0x060019A4 RID: 6564 RVA: 0x00080914 File Offset: 0x0007EB14
		public override float GetDailyDemandForCategory(Town town, ItemCategory category, int extraProsperity)
		{
			float num = MathF.Max(0f, town.Prosperity + (float)extraProsperity);
			float num2 = MathF.Max(0f, town.Prosperity - 3000f);
			float num3 = category.BaseDemand * num;
			float num4 = category.LuxuryDemand * num2;
			float result = num3 + num4;
			if (category.BaseDemand < 1E-08f)
			{
				result = num * 0.01f;
			}
			return result;
		}

		// Token: 0x060019A5 RID: 6565 RVA: 0x00080978 File Offset: 0x0007EB78
		public override int GetTownGoldChange(Town town)
		{
			float num = 10000f + town.Prosperity * 12f - (float)town.Gold;
			return MathF.Round(0.25f * num);
		}

		// Token: 0x060019A6 RID: 6566 RVA: 0x000809AC File Offset: 0x0007EBAC
		public override float CalculateDailySettlementBudgetForItemCategory(Town town, float demand, ItemCategory category)
		{
			return demand * MathF.Pow(town.GetItemCategoryPriceIndex(category), 0.3f);
		}

		// Token: 0x060019A7 RID: 6567 RVA: 0x000809C1 File Offset: 0x0007EBC1
		public override float GetDemandChangeFromValue(float purchaseValue)
		{
			return purchaseValue * 0.15f;
		}

		// Token: 0x060019A8 RID: 6568 RVA: 0x000809CA File Offset: 0x0007EBCA
		public override float GetEstimatedDemandForCategory(Town town, ItemData itemData, ItemCategory category)
		{
			return Campaign.Current.Models.SettlementEconomyModel.GetDailyDemandForCategory(town, category, 1000);
		}

		// Token: 0x0400086D RID: 2157
		private DefaultSettlementEconomyModel.CategoryValues _categoryValues;

		// Token: 0x0400086E RID: 2158
		private const int ProsperityLuxuryTreshold = 3000;

		// Token: 0x0400086F RID: 2159
		private const float dailyChangeFactor = 0.15f;

		// Token: 0x04000870 RID: 2160
		private const float oneMinusDailyChangeFactor = 0.85f;

		// Token: 0x02000596 RID: 1430
		private class CategoryValues
		{
			// Token: 0x06004D87 RID: 19847 RVA: 0x0017B680 File Offset: 0x00179880
			public CategoryValues()
			{
				this.PriceDict = new Dictionary<ItemCategory, int>();
				foreach (ItemObject itemObject in Items.All)
				{
					this.PriceDict[itemObject.GetItemCategory()] = itemObject.Value;
				}
			}

			// Token: 0x06004D88 RID: 19848 RVA: 0x0017B6F4 File Offset: 0x001798F4
			public int GetValueOfCategory(ItemCategory category)
			{
				int result = 1;
				this.PriceDict.TryGetValue(category, out result);
				return result;
			}

			// Token: 0x04001791 RID: 6033
			public Dictionary<ItemCategory, int> PriceDict;
		}
	}
}
