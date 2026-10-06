using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class VillageGoodProductionCampaignBehavior : CampaignBehaviorBase
{
	public const float DistributingItemsAtWorldConstant = 1.5f;

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
	{
		if (i == 1)
		{
			DistributeInitialItemsToTowns();
			CalculateInitialAccumulatedTaxes();
			foreach (Village item in Village.All)
			{
				float num = MBRandom.RandomFloat * 5f;
				for (int j = 0; (float)j < num; j++)
				{
					TickProductions(item.Settlement);
				}
			}
		}
		if (i % 20 != 0)
		{
			return;
		}
		foreach (Village item2 in Village.All)
		{
			TickProductions(item2.Settlement, initialProductionForTowns: true);
		}
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		TickProductions(settlement);
	}

	private void DistributeInitialItemsToTowns()
	{
		int num = 25;
		foreach (Town allTown in Campaign.Current.AllTowns)
		{
			float num2 = 0f;
			Settlement settlement = allTown.Settlement;
			foreach (Village allVillage in Campaign.Current.AllVillages)
			{
				float num3 = 0f;
				if (allVillage.TradeBound == settlement)
				{
					num3 += 1f;
				}
				else
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, allVillage.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
					float num4 = 0.5f * (600f / MathF.Pow(distance, 1.5f));
					if (num4 > 0.5f)
					{
						num4 = 0.5f;
					}
					float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, allVillage.TradeBound, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
					float num5 = 0.5f * (600f / MathF.Pow(distance2, 1.5f));
					if (num5 > 0.5f)
					{
						num5 = 0.5f;
					}
					num3 = ((allVillage.Settlement.MapFaction == settlement.MapFaction) ? 1f : 0.6f) * 0.5f * ((num4 + num5) / 2f);
				}
				num2 += num3;
			}
			foreach (Village allVillage2 in Campaign.Current.AllVillages)
			{
				float num6 = 0f;
				if (allVillage2.TradeBound == settlement)
				{
					num6 += 1f;
				}
				else
				{
					float distance3 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, allVillage2.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
					float num7 = 0.5f * (600f / MathF.Pow(distance3, 1.5f));
					if (num7 > 0.5f)
					{
						num7 = 0.5f;
					}
					float distance4 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, allVillage2.TradeBound, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
					float num8 = 0.5f * (600f / MathF.Pow(distance4, 1.5f));
					if (num8 > 0.5f)
					{
						num8 = 0.5f;
					}
					num6 = ((allVillage2.Settlement.MapFaction == settlement.MapFaction) ? 1f : 0.6f) * 0.5f * ((num7 + num8) / 2f);
				}
				foreach (var production in allVillage2.VillageType.Productions)
				{
					ItemObject item = production.Item1;
					float item2 = production.Item2;
					num6 *= 0.12244235f;
					int num9 = MBRandom.RoundRandomized(item2 * num6 * ((float)num * (12f / num2)));
					for (int i = 0; i < num9; i++)
					{
						ItemModifier itemModifier = null;
						EquipmentElement rosterElement = new EquipmentElement(item, itemModifier);
						settlement.ItemRoster.AddToCounts(rosterElement, 1);
					}
				}
			}
		}
	}

	private void CalculateInitialAccumulatedTaxes()
	{
		foreach (Village item in Village.All)
		{
			float num = 0f;
			foreach (var production in item.VillageType.Productions)
			{
				float resultNumber = Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(item, production.Item1).ResultNumber;
				num += (float)production.Item1.Value * resultNumber;
			}
			item.TradeTaxAccumulated = (int)(num * (0.6f + 0.3f * MBRandom.RandomFloat) * Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction());
		}
	}

	private void TickProductions(Settlement settlement, bool initialProductionForTowns = false)
	{
		Village village = settlement.Village;
		if (village != null && !village.IsDeserted)
		{
			int num = 0;
			for (int i = 0; i < village.Owner.ItemRoster.Count; i++)
			{
				num += village.Owner.ItemRoster[i].Amount;
			}
			int warehouseCapacity = village.GetWarehouseCapacity();
			if ((float)num < (float)warehouseCapacity * 1.5f)
			{
				TickGoodProduction(village, initialProductionForTowns);
				TickFoodProduction(village, initialProductionForTowns);
			}
		}
	}

	private void TickGoodProduction(Village village, bool initialProductionForTowns)
	{
		foreach (var production in village.VillageType.Productions)
		{
			ItemObject item = production.Item1;
			int num = MBRandom.RoundRandomized(Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(village, production.Item1).ResultNumber);
			if (num > 0)
			{
				if (!initialProductionForTowns)
				{
					village.Owner.ItemRoster.AddToCounts(item, num);
					CampaignEventDispatcher.Instance.OnItemProduced(item, village.Owner.Settlement, num);
				}
				else if (village.TradeBound != null)
				{
					village.TradeBound.ItemRoster.AddToCounts(item, num);
				}
			}
		}
	}

	private void TickFoodProduction(Village village, bool initialProductionForTowns)
	{
		int num = MBRandom.RoundRandomized(Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyFoodProductionAmount(village));
		for (int i = 0; i < num; i++)
		{
			float num2 = 0f;
			foreach (ItemObject consumableRawItem in Campaign.Current.DefaultVillageTypes.ConsumableRawItems)
			{
				float num3 = 1f / (float)consumableRawItem.Value;
				num2 += num3;
			}
			float num4 = MBRandom.RandomFloat * num2;
			foreach (ItemObject consumableRawItem2 in Campaign.Current.DefaultVillageTypes.ConsumableRawItems)
			{
				float num5 = 1f / (float)consumableRawItem2.Value;
				num4 -= num5;
				if (num4 < 1E-05f)
				{
					if (!initialProductionForTowns)
					{
						village.Owner.ItemRoster.AddToCounts(consumableRawItem2, 1);
						CampaignEventDispatcher.Instance.OnItemProduced(consumableRawItem2, village.Owner.Settlement, 1);
						break;
					}
					if (village.TradeBound != null)
					{
						village.TradeBound.ItemRoster.AddToCounts(consumableRawItem2, 1);
						break;
					}
				}
			}
		}
	}
}
