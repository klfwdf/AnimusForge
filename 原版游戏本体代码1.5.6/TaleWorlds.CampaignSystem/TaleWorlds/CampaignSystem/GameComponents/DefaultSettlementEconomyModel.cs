using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementEconomyModel : SettlementEconomyModel
{
	private class CategoryValues
	{
		public Dictionary<ItemCategory, int> PriceDict;

		public CategoryValues()
		{
			PriceDict = new Dictionary<ItemCategory, int>();
			foreach (ItemObject item in Items.All)
			{
				PriceDict[item.GetItemCategory()] = item.Value;
			}
		}

		public int GetValueOfCategory(ItemCategory category)
		{
			int value = 1;
			PriceDict.TryGetValue(category, out value);
			return value;
		}
	}

	private CategoryValues _categoryValues;

	private const int ProsperityLuxuryTreshold = 3000;

	private const float dailyChangeFactor = 0.15f;

	private const float oneMinusDailyChangeFactor = 0.85f;

	private CategoryValues CategoryValuesCache
	{
		get
		{
			if (_categoryValues == null)
			{
				_categoryValues = new CategoryValues();
			}
			return _categoryValues;
		}
	}

	public override (float, float) GetSupplyDemandForCategory(Town town, ItemCategory category, float dailySupply, float dailyDemand, float oldSupply, float oldDemand)
	{
		float b = oldSupply * 0.85f + dailySupply * 0.15f;
		float item = oldDemand * 0.85f + dailyDemand * 0.15f;
		b = MathF.Max(0.1f, b);
		return (b, item);
	}

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

	public override int GetTownGoldChange(Town town)
	{
		float num = 10000f + town.Prosperity * 12f - (float)town.Gold;
		return MathF.Round(0.25f * num);
	}

	public override float CalculateDailySettlementBudgetForItemCategory(Town town, float demand, ItemCategory category)
	{
		return demand * MathF.Pow(town.GetItemCategoryPriceIndex(category), 0.3f);
	}

	public override float GetDemandChangeFromValue(float purchaseValue)
	{
		return purchaseValue * 0.15f;
	}

	public override float GetEstimatedDemandForCategory(Town town, ItemData itemData, ItemCategory category)
	{
		return Campaign.Current.Models.SettlementEconomyModel.GetDailyDemandForCategory(town, category, 1000);
	}
}
