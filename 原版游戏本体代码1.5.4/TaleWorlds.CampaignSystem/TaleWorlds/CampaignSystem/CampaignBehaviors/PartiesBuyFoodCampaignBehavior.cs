using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartiesBuyFoodCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void TryBuyingFood(MobileParty mobileParty, Settlement settlement)
	{
		if (Campaign.Current.GameStarted && mobileParty.LeaderHero != null && (settlement.IsTown || settlement.IsVillage) && Campaign.Current.Models.MobilePartyFoodConsumptionModel.DoesPartyConsumeFood(mobileParty) && (mobileParty.Army == null || mobileParty.AttachedTo == null || mobileParty.Army.LeaderParty == mobileParty) && (settlement.IsVillage || (mobileParty.MapFaction != null && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction))) && settlement.ItemRoster.TotalFood > 0)
		{
			float num = 0f;
			PartyFoodBuyingModel partyFoodBuyingModel = Campaign.Current.Models.PartyFoodBuyingModel;
			num = (settlement.IsVillage ? partyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage : partyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown);
			if (mobileParty.Army == null || (mobileParty.AttachedTo == null && mobileParty.Army.LeaderParty != mobileParty))
			{
				BuyFoodInternal(mobileParty, settlement, CalculateFoodCountToBuy(mobileParty, num));
			}
			else
			{
				BuyFoodForArmy(mobileParty, settlement, num);
			}
		}
	}

	private int CalculateFoodCountToBuy(MobileParty mobileParty, float minimumDaysToLast)
	{
		if (mobileParty.FoodChange.ApproximatelyEqualsTo(0f))
		{
			return 0;
		}
		float num = (float)mobileParty.TotalFoodAtInventory / (0f - mobileParty.FoodChange);
		float num2 = minimumDaysToLast - num;
		if (num2 > 0f)
		{
			return (int)((0f - mobileParty.FoodChange) * num2);
		}
		return 0;
	}

	private void BuyFoodInternal(MobileParty mobileParty, Settlement settlement, int numberOfFoodItemsNeededToBuy)
	{
		if (mobileParty.IsMainParty)
		{
			return;
		}
		for (int i = 0; i < numberOfFoodItemsNeededToBuy; i++)
		{
			Campaign.Current.Models.PartyFoodBuyingModel.FindItemToBuy(mobileParty, settlement, out var itemRosterElement, out var itemElementsPrice);
			if (itemRosterElement.EquipmentElement.Item != null)
			{
				if (itemElementsPrice <= (float)mobileParty.PartyTradeGold)
				{
					SellItemsAction.Apply(settlement.Party, mobileParty.Party, itemRosterElement, 1);
				}
				if (itemRosterElement.EquipmentElement.Item.HasHorseComponent && itemRosterElement.EquipmentElement.Item.HorseComponent.IsLiveStock)
				{
					i += itemRosterElement.EquipmentElement.Item.HorseComponent.MeatCount - 1;
				}
				continue;
			}
			break;
		}
	}

	private void BuyFoodForArmy(MobileParty mobileParty, Settlement settlement, float minimumDaysToLast)
	{
		float num = mobileParty.Army.LeaderParty.FoodChange;
		foreach (MobileParty attachedParty in mobileParty.Army.LeaderParty.AttachedParties)
		{
			num += attachedParty.FoodChange;
		}
		List<(int, int)> list = new List<(int, int)>(mobileParty.Army.Parties.Count);
		float num2 = mobileParty.Army.LeaderParty.FoodChange / num;
		int num3 = CalculateFoodCountToBuy(mobileParty.Army.LeaderParty, minimumDaysToLast);
		list.Add(((int)((float)settlement.ItemRoster.TotalFood * num2), num3));
		int num4 = num3;
		foreach (MobileParty attachedParty2 in mobileParty.Army.LeaderParty.AttachedParties)
		{
			num2 = attachedParty2.FoodChange / num;
			num3 = CalculateFoodCountToBuy(attachedParty2, minimumDaysToLast);
			list.Add(((int)((float)settlement.ItemRoster.TotalFood * num2), num3));
			num4 += num3;
		}
		bool flag = settlement.ItemRoster.TotalFood < num4;
		int num5 = 0;
		foreach (var item in list)
		{
			int num6;
			if (flag)
			{
				(num6, _) = item;
			}
			else
			{
				num6 = item.Item2;
			}
			int numberOfFoodItemsNeededToBuy = num6;
			MobileParty mobileParty2 = ((num5 == 0) ? mobileParty.Army.LeaderParty : mobileParty.Army.LeaderParty.AttachedParties[num5 - 1]);
			if (!mobileParty2.IsMainParty)
			{
				BuyFoodInternal(mobileParty2, settlement, numberOfFoodItemsNeededToBuy);
			}
			num5++;
		}
	}

	public void HourlyTickParty(MobileParty mobileParty)
	{
		Settlement currentSettlementOfMobilePartyForAICalculation = MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(mobileParty);
		if (currentSettlementOfMobilePartyForAICalculation != null)
		{
			TryBuyingFood(mobileParty, currentSettlementOfMobilePartyForAICalculation);
		}
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty != null)
		{
			TryBuyingFood(mobileParty, settlement);
		}
	}
}
