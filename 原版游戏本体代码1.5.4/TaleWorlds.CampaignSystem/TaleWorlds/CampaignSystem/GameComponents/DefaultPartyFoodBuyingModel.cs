using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyFoodBuyingModel : PartyFoodBuyingModel
{
	public override float MinimumDaysFoodToLastWhileBuyingFoodFromTown => 30f;

	public override float MinimumDaysFoodToLastWhileBuyingFoodFromVillage => 12f;

	public override float LowCostFoodPriceAverage => 30f;

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
			if (elementCopyAtIndex.Amount <= 0)
			{
				continue;
			}
			bool flag = elementCopyAtIndex.EquipmentElement.Item.HasHorseComponent && elementCopyAtIndex.EquipmentElement.Item.HorseComponent.IsLiveStock;
			if (!(elementCopyAtIndex.EquipmentElement.Item.IsFood || flag))
			{
				continue;
			}
			int itemPrice = settlementComponent.GetItemPrice(elementCopyAtIndex.EquipmentElement, mobileParty);
			int itemValue = elementCopyAtIndex.EquipmentElement.ItemValue;
			if (!(itemPrice < 120 || flag) || mobileParty.PartyTradeGold < itemPrice)
			{
				continue;
			}
			float num3 = (flag ? ((120f - (float)(itemPrice / elementCopyAtIndex.EquipmentElement.Item.HorseComponent.MeatCount)) * 0.0083f) : ((float)(120 - itemPrice) * 0.0083f));
			float num4 = (flag ? ((100f - (float)(itemValue / elementCopyAtIndex.EquipmentElement.Item.HorseComponent.MeatCount)) * 0.01f) : ((float)(100 - itemValue) * 0.01f));
			float num5 = num3 * num3 * num4 * num4;
			if (num5 > 0f)
			{
				if (MBRandom.RandomFloat * (num + num5) >= num)
				{
					num2 = i;
					itemElementsPrice = itemPrice;
				}
				num += num5;
			}
		}
		if (num2 != -1)
		{
			itemElement = settlement.ItemRoster.GetElementCopyAtIndex(num2);
		}
	}
}
