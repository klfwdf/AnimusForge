using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class ItemConsumptionBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedFollowUp);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedFollowUpEnd);
		CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTickTown);
	}

	private void OnNewGameCreatedFollowUp(CampaignGameStarter starter, int i)
	{
		if (i < 2)
		{
			MakeConsumptionAllTowns();
		}
	}

	private void OnNewGameCreatedFollowUpEnd(CampaignGameStarter starter)
	{
		Dictionary<ItemCategory, float> categoryBudget = new Dictionary<ItemCategory, float>();
		for (int i = 0; i < 10; i++)
		{
			foreach (Town allTown in Town.AllTowns)
			{
				UpdateSupplyAndDemand(allTown);
				UpdateDemandShift(allTown, categoryBudget);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void DailyTickTown(Town town)
	{
		Dictionary<ItemCategory, int> saleLog = new Dictionary<ItemCategory, int>();
		MakeConsumptionInTown(town, saleLog);
	}

	private void MakeConsumptionAllTowns()
	{
		foreach (Town allTown in Town.AllTowns)
		{
			DailyTickTown(allTown);
		}
	}

	private void MakeConsumptionInTown(Town town, Dictionary<ItemCategory, int> saleLog)
	{
		Dictionary<ItemCategory, float> dictionary = new Dictionary<ItemCategory, float>();
		DeleteOverproducedItems(town);
		UpdateSupplyAndDemand(town);
		UpdateDemandShift(town, dictionary);
		MakeConsumption(town, dictionary, saleLog);
		GetFoodFromMarket(town, saleLog);
		UpdateSellLog(town, saleLog);
		UpdateTownGold(town);
	}

	private void UpdateTownGold(Town town)
	{
		int townGoldChange = Campaign.Current.Models.SettlementEconomyModel.GetTownGoldChange(town);
		town.ChangeGold(townGoldChange);
	}

	private void DeleteOverproducedItems(Town town)
	{
		ItemRoster itemRoster = town.Owner.ItemRoster;
		for (int num = itemRoster.Count - 1; num >= 0; num--)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(num);
			ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
			int amount = elementCopyAtIndex.Amount;
			if (amount > 0 && (item.IsCraftedByPlayer || item.IsBannerItem))
			{
				itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -amount);
			}
			else if (elementCopyAtIndex.EquipmentElement.ItemModifier != null && MBRandom.RandomFloat < 0.05f)
			{
				itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -1);
			}
		}
	}

	private static void GetFoodFromMarket(Town town, Dictionary<ItemCategory, int> saleLog)
	{
		float foodChange = town.FoodChange;
		var (num, num2) = TownHelpers.GetTownFoodAndMarketStocks(town);
		if (town.IsTown && town.IsUnderSiege && foodChange < 0f && num <= 0 && num2 > 0)
		{
			GetFoodFromMarketInternal(town, Math.Abs(TaleWorlds.Library.MathF.Floor(foodChange)), saleLog);
		}
	}

	private void UpdateSellLog(Town town, Dictionary<ItemCategory, int> saleLog)
	{
		List<Town.SellLog> list = new List<Town.SellLog>();
		foreach (KeyValuePair<ItemCategory, int> item in saleLog)
		{
			if (item.Value > 0)
			{
				list.Add(new Town.SellLog(item.Key, item.Value));
			}
		}
		town.SetSoldItems(list);
	}

	private static void GetFoodFromMarketInternal(Town town, int amount, Dictionary<ItemCategory, int> saleLog)
	{
		ItemRoster itemRoster = town.Owner.ItemRoster;
		int num = itemRoster.Count - 1;
		while (num >= 0 && amount > 0)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(num);
			ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
			if (item.ItemCategory.Properties == ItemCategory.Property.BonusToFoodStores)
			{
				int num2 = ((elementCopyAtIndex.Amount >= amount) ? amount : elementCopyAtIndex.Amount);
				amount -= num2;
				itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -num2);
				int value = 0;
				saleLog.TryGetValue(item.ItemCategory, out value);
				saleLog[item.ItemCategory] = value + num2;
			}
			num--;
		}
	}

	private static void MakeConsumption(Town town, Dictionary<ItemCategory, float> categoryDemand, Dictionary<ItemCategory, int> saleLog)
	{
		saleLog.Clear();
		TownMarketData marketData = town.MarketData;
		ItemRoster itemRoster = town.Owner.ItemRoster;
		for (int num = itemRoster.Count - 1; num >= 0; num--)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(num);
			ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
			int amount = elementCopyAtIndex.Amount;
			ItemCategory itemCategory = item.GetItemCategory();
			float demand = categoryDemand[itemCategory];
			float num2 = Campaign.Current.Models.SettlementEconomyModel.CalculateDailySettlementBudgetForItemCategory(town, demand, itemCategory);
			if (num2 > 0.01f)
			{
				int price = marketData.GetPrice(item);
				float num3 = num2 / (float)price;
				if (num3 > (float)amount)
				{
					num3 = amount;
				}
				int num4 = MBRandom.RoundRandomized(num3);
				if (num4 > amount)
				{
					num4 = amount;
				}
				itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -num4);
				categoryDemand[itemCategory] = num2 - num3 * (float)price;
				town.ChangeGold(num4 * price);
				int value = 0;
				saleLog.TryGetValue(itemCategory, out value);
				saleLog[itemCategory] = value + num4;
			}
		}
	}

	private void UpdateDemandShift(Town town, Dictionary<ItemCategory, float> categoryBudget)
	{
		TownMarketData marketData = town.MarketData;
		foreach (ItemCategory item in ItemCategories.All)
		{
			categoryBudget[item] = Campaign.Current.Models.SettlementEconomyModel.GetDailyDemandForCategory(town, item);
		}
		foreach (ItemCategory item2 in ItemCategories.All)
		{
			if (item2.CanSubstitute == null)
			{
				continue;
			}
			ItemData categoryData = marketData.GetCategoryData(item2);
			ItemData categoryData2 = marketData.GetCategoryData(item2.CanSubstitute);
			if (categoryData.Supply / categoryData.Demand > categoryData2.Supply / categoryData2.Demand && categoryData2.Demand > categoryData.Demand)
			{
				float num = (categoryData2.Demand - categoryData.Demand) * item2.SubstitutionFactor;
				marketData.SetDemand(item2, categoryData.Demand + num);
				marketData.SetDemand(item2.CanSubstitute, categoryData2.Demand - num);
				if (categoryBudget.TryGetValue(item2, out var value) && categoryBudget.TryGetValue(item2.CanSubstitute, out var value2))
				{
					categoryBudget[item2] = value + num;
					categoryBudget[item2.CanSubstitute] = value2 - num;
				}
			}
		}
	}

	private static void UpdateSupplyAndDemand(Town town)
	{
		TownMarketData marketData = town.MarketData;
		SettlementEconomyModel settlementEconomyModel = Campaign.Current.Models.SettlementEconomyModel;
		foreach (ItemCategory item in ItemCategories.All)
		{
			ItemData categoryData = marketData.GetCategoryData(item);
			float estimatedDemandForCategory = settlementEconomyModel.GetEstimatedDemandForCategory(town, categoryData, item);
			var (supply, demand) = settlementEconomyModel.GetSupplyDemandForCategory(town, item, categoryData.InStoreValue, estimatedDemandForCategory, categoryData.Supply, categoryData.Demand);
			marketData.SetSupplyDemand(item, supply, demand);
		}
	}
}
