using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions;

public static class SellGoodsForTradeAction
{
	private enum SellGoodsForTradeActionDetail
	{
		VillagerTrade,
		LordTrade
	}

	private static void ApplyInternal(Settlement settlement, MobileParty mobileParty, SellGoodsForTradeActionDetail detail)
	{
		Town town = settlement.Town;
		if (town == null)
		{
			return;
		}
		List<(EquipmentElement, int)> list = new List<(EquipmentElement, int)>();
		if (detail != SellGoodsForTradeActionDetail.VillagerTrade)
		{
			return;
		}
		int num = 10000;
		ItemObject itemObject = null;
		for (int i = 0; i < mobileParty.ItemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = mobileParty.ItemRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.EquipmentElement.Item.ItemCategory == DefaultItemCategories.PackAnimal && elementCopyAtIndex.EquipmentElement.Item.Value < num)
			{
				num = elementCopyAtIndex.EquipmentElement.Item.Value;
				itemObject = elementCopyAtIndex.EquipmentElement.Item;
			}
		}
		for (int num2 = mobileParty.ItemRoster.Count - 1; num2 >= 0; num2--)
		{
			ItemRosterElement elementCopyAtIndex2 = mobileParty.ItemRoster.GetElementCopyAtIndex(num2);
			int itemPrice = town.GetItemPrice(elementCopyAtIndex2.EquipmentElement, mobileParty, isSelling: true);
			int num3 = mobileParty.ItemRoster.GetElementNumber(num2);
			if (elementCopyAtIndex2.EquipmentElement.Item == itemObject)
			{
				int num4 = (int)(0.5f * (float)mobileParty.MemberRoster.TotalManCount);
				num3 -= num4;
			}
			if (num3 > 0)
			{
				int num5 = MathF.Min(num3, town.Gold / itemPrice);
				if (num5 > 0)
				{
					mobileParty.PartyTradeGold += num5 * itemPrice;
					EquipmentElement equipmentElement = elementCopyAtIndex2.EquipmentElement;
					town.ChangeGold(-num5 * itemPrice);
					settlement.ItemRoster.AddToCounts(equipmentElement, num5);
					mobileParty.ItemRoster.AddToCounts(equipmentElement, -num5);
					list.Add((equipmentElement, num5));
				}
			}
		}
		if (!list.IsEmpty() && mobileParty.IsCaravan)
		{
			CampaignEventDispatcher.Instance.OnCaravanTransactionCompleted(mobileParty, town, list);
		}
	}

	public static void ApplyByVillagerTrade(Settlement settlement, MobileParty villagerParty)
	{
		ApplyInternal(settlement, villagerParty, SellGoodsForTradeActionDetail.VillagerTrade);
	}
}
