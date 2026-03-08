using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200040C RID: 1036
	public class ItemConsumptionBehavior : CampaignBehaviorBase
	{
		// Token: 0x060040B1 RID: 16561 RVA: 0x0012D85C File Offset: 0x0012BA5C
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedFollowUp));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedFollowUpEnd));
			CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, new Action<Town>(this.DailyTickTown));
		}

		// Token: 0x060040B2 RID: 16562 RVA: 0x0012D8AE File Offset: 0x0012BAAE
		private void OnNewGameCreatedFollowUp(CampaignGameStarter starter, int i)
		{
			if (i < 2)
			{
				this.MakeConsumptionAllTowns();
			}
		}

		// Token: 0x060040B3 RID: 16563 RVA: 0x0012D8BC File Offset: 0x0012BABC
		private void OnNewGameCreatedFollowUpEnd(CampaignGameStarter starter)
		{
			Dictionary<ItemCategory, float> categoryBudget = new Dictionary<ItemCategory, float>();
			for (int i = 0; i < 10; i++)
			{
				foreach (Town town in Town.AllTowns)
				{
					ItemConsumptionBehavior.UpdateSupplyAndDemand(town);
					this.UpdateDemandShift(town, categoryBudget);
				}
			}
		}

		// Token: 0x060040B4 RID: 16564 RVA: 0x0012D928 File Offset: 0x0012BB28
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060040B5 RID: 16565 RVA: 0x0012D92C File Offset: 0x0012BB2C
		private void DailyTickTown(Town town)
		{
			Dictionary<ItemCategory, int> saleLog = new Dictionary<ItemCategory, int>();
			this.MakeConsumptionInTown(town, saleLog);
		}

		// Token: 0x060040B6 RID: 16566 RVA: 0x0012D948 File Offset: 0x0012BB48
		private void MakeConsumptionAllTowns()
		{
			foreach (Town town in Town.AllTowns)
			{
				this.DailyTickTown(town);
			}
		}

		// Token: 0x060040B7 RID: 16567 RVA: 0x0012D99C File Offset: 0x0012BB9C
		private void MakeConsumptionInTown(Town town, Dictionary<ItemCategory, int> saleLog)
		{
			Dictionary<ItemCategory, float> dictionary = new Dictionary<ItemCategory, float>();
			this.DeleteOverproducedItems(town);
			ItemConsumptionBehavior.UpdateSupplyAndDemand(town);
			this.UpdateDemandShift(town, dictionary);
			ItemConsumptionBehavior.MakeConsumption(town, dictionary, saleLog);
			ItemConsumptionBehavior.GetFoodFromMarket(town, saleLog);
			this.UpdateSellLog(town, saleLog);
			this.UpdateTownGold(town);
		}

		// Token: 0x060040B8 RID: 16568 RVA: 0x0012D9E4 File Offset: 0x0012BBE4
		private void UpdateTownGold(Town town)
		{
			int townGoldChange = Campaign.Current.Models.SettlementEconomyModel.GetTownGoldChange(town);
			town.ChangeGold(townGoldChange);
		}

		// Token: 0x060040B9 RID: 16569 RVA: 0x0012DA10 File Offset: 0x0012BC10
		private void DeleteOverproducedItems(Town town)
		{
			ItemRoster itemRoster = town.Owner.ItemRoster;
			for (int i = itemRoster.Count - 1; i >= 0; i--)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
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

		// Token: 0x060040BA RID: 16570 RVA: 0x0012DAB4 File Offset: 0x0012BCB4
		private static void GetFoodFromMarket(Town town, Dictionary<ItemCategory, int> saleLog)
		{
			float foodChange = town.FoodChange;
			ValueTuple<int, int> townFoodAndMarketStocks = TownHelpers.GetTownFoodAndMarketStocks(town);
			int item = townFoodAndMarketStocks.Item1;
			int item2 = townFoodAndMarketStocks.Item2;
			if (town.IsTown && town.IsUnderSiege && foodChange < 0f && item <= 0 && item2 > 0)
			{
				ItemConsumptionBehavior.GetFoodFromMarketInternal(town, Math.Abs(MathF.Floor(foodChange)), saleLog);
			}
		}

		// Token: 0x060040BB RID: 16571 RVA: 0x0012DB10 File Offset: 0x0012BD10
		private void UpdateSellLog(Town town, Dictionary<ItemCategory, int> saleLog)
		{
			List<Town.SellLog> list = new List<Town.SellLog>();
			foreach (KeyValuePair<ItemCategory, int> keyValuePair in saleLog)
			{
				if (keyValuePair.Value > 0)
				{
					list.Add(new Town.SellLog(keyValuePair.Key, keyValuePair.Value));
				}
			}
			town.SetSoldItems(list);
		}

		// Token: 0x060040BC RID: 16572 RVA: 0x0012DB88 File Offset: 0x0012BD88
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
					int num3 = 0;
					saleLog.TryGetValue(item.ItemCategory, out num3);
					saleLog[item.ItemCategory] = num3 + num2;
				}
				num--;
			}
		}

		// Token: 0x060040BD RID: 16573 RVA: 0x0012DC34 File Offset: 0x0012BE34
		private static void MakeConsumption(Town town, Dictionary<ItemCategory, float> categoryDemand, Dictionary<ItemCategory, int> saleLog)
		{
			saleLog.Clear();
			TownMarketData marketData = town.MarketData;
			ItemRoster itemRoster = town.Owner.ItemRoster;
			for (int i = itemRoster.Count - 1; i >= 0; i--)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				int amount = elementCopyAtIndex.Amount;
				ItemCategory itemCategory = item.GetItemCategory();
				float demand = categoryDemand[itemCategory];
				float num = Campaign.Current.Models.SettlementEconomyModel.CalculateDailySettlementBudgetForItemCategory(town, demand, itemCategory);
				if (num > 0.01f)
				{
					int price = marketData.GetPrice(item, null, false, null);
					float num2 = num / (float)price;
					if (num2 > (float)amount)
					{
						num2 = (float)amount;
					}
					int num3 = MBRandom.RoundRandomized(num2);
					if (num3 > amount)
					{
						num3 = amount;
					}
					itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -num3);
					categoryDemand[itemCategory] = num - num2 * (float)price;
					town.ChangeGold(num3 * price);
					int num4 = 0;
					saleLog.TryGetValue(itemCategory, out num4);
					saleLog[itemCategory] = num4 + num3;
				}
			}
		}

		// Token: 0x060040BE RID: 16574 RVA: 0x0012DD4C File Offset: 0x0012BF4C
		private void UpdateDemandShift(Town town, Dictionary<ItemCategory, float> categoryBudget)
		{
			TownMarketData marketData = town.MarketData;
			foreach (ItemCategory itemCategory in ItemCategories.All)
			{
				categoryBudget[itemCategory] = Campaign.Current.Models.SettlementEconomyModel.GetDailyDemandForCategory(town, itemCategory, 0);
			}
			foreach (ItemCategory itemCategory2 in ItemCategories.All)
			{
				if (itemCategory2.CanSubstitute != null)
				{
					ItemData categoryData = marketData.GetCategoryData(itemCategory2);
					ItemData categoryData2 = marketData.GetCategoryData(itemCategory2.CanSubstitute);
					if (categoryData.Supply / categoryData.Demand > categoryData2.Supply / categoryData2.Demand && categoryData2.Demand > categoryData.Demand)
					{
						float num = (categoryData2.Demand - categoryData.Demand) * itemCategory2.SubstitutionFactor;
						marketData.SetDemand(itemCategory2, categoryData.Demand + num);
						marketData.SetDemand(itemCategory2.CanSubstitute, categoryData2.Demand - num);
						float num2;
						float num3;
						if (categoryBudget.TryGetValue(itemCategory2, out num2) && categoryBudget.TryGetValue(itemCategory2.CanSubstitute, out num3))
						{
							categoryBudget[itemCategory2] = num2 + num;
							categoryBudget[itemCategory2.CanSubstitute] = num3 - num;
						}
					}
				}
			}
		}

		// Token: 0x060040BF RID: 16575 RVA: 0x0012DEC8 File Offset: 0x0012C0C8
		private static void UpdateSupplyAndDemand(Town town)
		{
			TownMarketData marketData = town.MarketData;
			SettlementEconomyModel settlementEconomyModel = Campaign.Current.Models.SettlementEconomyModel;
			foreach (ItemCategory itemCategory in ItemCategories.All)
			{
				ItemData categoryData = marketData.GetCategoryData(itemCategory);
				float estimatedDemandForCategory = settlementEconomyModel.GetEstimatedDemandForCategory(town, categoryData, itemCategory);
				ValueTuple<float, float> supplyDemandForCategory = settlementEconomyModel.GetSupplyDemandForCategory(town, itemCategory, (float)categoryData.InStoreValue, estimatedDemandForCategory, categoryData.Supply, categoryData.Demand);
				float item = supplyDemandForCategory.Item1;
				float item2 = supplyDemandForCategory.Item2;
				marketData.SetSupplyDemand(itemCategory, item, item2);
			}
		}
	}
}
