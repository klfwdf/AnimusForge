using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000130 RID: 304
	public class DefaultPartyFoodBuyingModel : PartyFoodBuyingModel
	{
		// Token: 0x17000687 RID: 1671
		// (get) Token: 0x060018DE RID: 6366 RVA: 0x00079F62 File Offset: 0x00078162
		public override float MinimumDaysFoodToLastWhileBuyingFoodFromTown
		{
			get
			{
				return 30f;
			}
		}

		// Token: 0x17000688 RID: 1672
		// (get) Token: 0x060018DF RID: 6367 RVA: 0x00079F69 File Offset: 0x00078169
		public override float MinimumDaysFoodToLastWhileBuyingFoodFromVillage
		{
			get
			{
				return 12f;
			}
		}

		// Token: 0x17000689 RID: 1673
		// (get) Token: 0x060018E0 RID: 6368 RVA: 0x00079F70 File Offset: 0x00078170
		public override float LowCostFoodPriceAverage
		{
			get
			{
				return 30f;
			}
		}

		// Token: 0x060018E1 RID: 6369 RVA: 0x00079F78 File Offset: 0x00078178
		public override void FindItemToBuy(MobileParty mobileParty, Settlement settlement, out ItemRosterElement itemElement, out float itemElementsPrice)
		{
			itemElement = ItemRosterElement.Invalid;
			itemElementsPrice = 0f;
			float num = 0f;
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			int num2 = -1;
			for (int i = 0; i < settlement.ItemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = settlement.ItemRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.Amount > 0)
				{
					bool flag = elementCopyAtIndex.EquipmentElement.Item.HasHorseComponent && elementCopyAtIndex.EquipmentElement.Item.HorseComponent.IsLiveStock;
					if (elementCopyAtIndex.EquipmentElement.Item.IsFood || flag)
					{
						int itemPrice = settlementComponent.GetItemPrice(elementCopyAtIndex.EquipmentElement, mobileParty, false);
						int itemValue = elementCopyAtIndex.EquipmentElement.ItemValue;
						if ((itemPrice < 120 || flag) && mobileParty.PartyTradeGold >= itemPrice)
						{
							object obj = (flag ? ((120f - (float)(itemPrice / elementCopyAtIndex.EquipmentElement.Item.HorseComponent.MeatCount)) * 0.0083f) : ((float)(120 - itemPrice) * 0.0083f));
							float num3 = (flag ? ((100f - (float)(itemValue / elementCopyAtIndex.EquipmentElement.Item.HorseComponent.MeatCount)) * 0.01f) : ((float)(100 - itemValue) * 0.01f));
							object obj2 = obj;
							float num4 = obj2 * obj2 * num3 * num3;
							if (num4 > 0f)
							{
								if (MBRandom.RandomFloat * (num + num4) >= num)
								{
									num2 = i;
									itemElementsPrice = (float)itemPrice;
								}
								num += num4;
							}
						}
					}
				}
			}
			if (num2 != -1)
			{
				itemElement = settlement.ItemRoster.GetElementCopyAtIndex(num2);
			}
		}
	}
}
