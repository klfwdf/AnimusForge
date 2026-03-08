using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C2 RID: 1218
	public static class SellGoodsForTradeAction
	{
		// Token: 0x06004A11 RID: 18961 RVA: 0x00174CF4 File Offset: 0x00172EF4
		private static void ApplyInternal(Settlement settlement, MobileParty mobileParty, SellGoodsForTradeAction.SellGoodsForTradeActionDetail detail)
		{
			Town town = settlement.Town;
			if (town == null)
			{
				return;
			}
			List<ValueTuple<EquipmentElement, int>> list = new List<ValueTuple<EquipmentElement, int>>();
			if (detail == SellGoodsForTradeAction.SellGoodsForTradeActionDetail.VillagerTrade)
			{
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
				for (int j = mobileParty.ItemRoster.Count - 1; j >= 0; j--)
				{
					ItemRosterElement elementCopyAtIndex2 = mobileParty.ItemRoster.GetElementCopyAtIndex(j);
					int itemPrice = town.GetItemPrice(elementCopyAtIndex2.EquipmentElement, mobileParty, true);
					int num2 = mobileParty.ItemRoster.GetElementNumber(j);
					if (elementCopyAtIndex2.EquipmentElement.Item == itemObject)
					{
						int num3 = (int)(0.5f * (float)mobileParty.MemberRoster.TotalManCount);
						num2 -= num3;
					}
					if (num2 > 0)
					{
						int num4 = MathF.Min(num2, town.Gold / itemPrice);
						if (num4 > 0)
						{
							mobileParty.PartyTradeGold += num4 * itemPrice;
							EquipmentElement equipmentElement = elementCopyAtIndex2.EquipmentElement;
							town.ChangeGold(-num4 * itemPrice);
							settlement.ItemRoster.AddToCounts(equipmentElement, num4);
							mobileParty.ItemRoster.AddToCounts(equipmentElement, -num4);
							list.Add(new ValueTuple<EquipmentElement, int>(equipmentElement, num4));
						}
					}
				}
				if (!list.IsEmpty<ValueTuple<EquipmentElement, int>>() && mobileParty.IsCaravan)
				{
					CampaignEventDispatcher.Instance.OnCaravanTransactionCompleted(mobileParty, town, list);
				}
			}
		}

		// Token: 0x06004A12 RID: 18962 RVA: 0x00174EB4 File Offset: 0x001730B4
		public static void ApplyByVillagerTrade(Settlement settlement, MobileParty villagerParty)
		{
			SellGoodsForTradeAction.ApplyInternal(settlement, villagerParty, SellGoodsForTradeAction.SellGoodsForTradeActionDetail.VillagerTrade);
		}

		// Token: 0x02000895 RID: 2197
		private enum SellGoodsForTradeActionDetail
		{
			// Token: 0x0400245A RID: 9306
			VillagerTrade,
			// Token: 0x0400245B RID: 9307
			LordTrade
		}
	}
}
