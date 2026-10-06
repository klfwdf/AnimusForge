using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartiesBuyHorseCampaignBehavior : CampaignBehaviorBase
{
	private float _averageHorsePrice;

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameStarted);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnGameStarted);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnGameStarted(CampaignGameStarter obj)
	{
		CalculateAverageHorsePrice();
	}

	private void OnDailyTick()
	{
		CalculateAverageHorsePrice();
	}

	private void CalculateAverageHorsePrice()
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < Items.All.Count; i++)
		{
			ItemObject itemObject = Items.All[i];
			if (itemObject.ItemCategory == DefaultItemCategories.Horse)
			{
				num += itemObject.Value;
				num2++;
			}
		}
		if (num2 == 0)
		{
			_averageHorsePrice = 0f;
		}
		else
		{
			_averageHorsePrice = num / num2;
		}
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty != null && mobileParty.MapFaction != null && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction) && mobileParty != MobileParty.MainParty && mobileParty.IsLordParty && mobileParty.LeaderHero != null && !mobileParty.IsDisbanding && settlement.IsTown)
		{
			int num = MathF.Min(100000, mobileParty.PartyTradeGold);
			int numberOfMounts = mobileParty.Party.NumberOfMounts;
			if (numberOfMounts > mobileParty.Party.NumberOfRegularMembers)
			{
				return;
			}
			Town town = settlement.Town;
			if (town.MarketData.GetItemCountOfCategory(DefaultItemCategories.Horse) == 0)
			{
				return;
			}
			float num2 = _averageHorsePrice * (float)numberOfMounts / (float)num;
			if (num2 < 0.08f)
			{
				float randomFloat = MBRandom.RandomFloat;
				float randomFloat2 = MBRandom.RandomFloat;
				float randomFloat3 = MBRandom.RandomFloat;
				float num3 = (0.08f - num2) * (float)num * randomFloat * randomFloat2 * randomFloat3;
				if (num3 > (float)(mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * _averageHorsePrice)
				{
					num3 = (float)(mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * _averageHorsePrice;
				}
				BuyHorses(mobileParty, town, num3);
			}
		}
		if (mobileParty == null || mobileParty == MobileParty.MainParty || !mobileParty.IsLordParty || mobileParty.LeaderHero == null || mobileParty.IsDisbanding || !settlement.IsTown)
		{
			return;
		}
		float num4 = 0f;
		for (int num5 = mobileParty.ItemRoster.Count - 1; num5 >= 0; num5--)
		{
			ItemRosterElement subject = mobileParty.ItemRoster[num5];
			if (subject.EquipmentElement.Item.IsMountable)
			{
				num4 += (float)(subject.Amount * subject.EquipmentElement.Item.Value);
			}
			else if (!subject.EquipmentElement.Item.IsFood)
			{
				SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject, subject.Amount, settlement);
			}
		}
		int num6 = MathF.Min(100000, mobileParty.PartyTradeGold);
		if (!(num4 > (float)num6 * 0.1f))
		{
			return;
		}
		for (int i = 0; i < 10; i++)
		{
			ItemRosterElement subject2 = default(ItemRosterElement);
			int num7 = 0;
			for (int j = 0; j < mobileParty.ItemRoster.Count; j++)
			{
				ItemRosterElement itemRosterElement = mobileParty.ItemRoster[j];
				if (itemRosterElement.EquipmentElement.Item.IsMountable && itemRosterElement.EquipmentElement.Item.Value > num7)
				{
					num7 = itemRosterElement.EquipmentElement.Item.Value;
					subject2 = itemRosterElement;
				}
			}
			if (num7 > 0)
			{
				SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject2, 1, settlement);
				num4 -= (float)num7;
				if (num4 < (float)num6 * 0.1f)
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	private void BuyHorses(MobileParty mobileParty, Town town, float budget)
	{
		for (int i = 0; i < 2; i++)
		{
			int num = -1;
			int num2 = 100000;
			ItemRoster itemRoster = town.Owner.ItemRoster;
			for (int j = 0; j < itemRoster.Count; j++)
			{
				if (itemRoster.GetItemAtIndex(j).ItemCategory == DefaultItemCategories.Horse)
				{
					int itemPrice = town.GetItemPrice(itemRoster.GetElementCopyAtIndex(j).EquipmentElement, mobileParty);
					if (itemPrice < num2)
					{
						num2 = itemPrice;
						num = j;
					}
				}
			}
			if (num >= 0)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(num);
				int num3 = elementCopyAtIndex.Amount;
				if ((float)(num3 * num2) > budget)
				{
					num3 = MathF.Ceiling(budget / (float)num2);
				}
				int numberOfMounts = mobileParty.Party.NumberOfMounts;
				if (num3 > mobileParty.Party.NumberOfRegularMembers - numberOfMounts)
				{
					num3 = mobileParty.Party.NumberOfRegularMembers - numberOfMounts;
				}
				if (num3 > 0)
				{
					SellItemsAction.Apply(town.Owner, mobileParty.Party, elementCopyAtIndex, num3, town.Owner.Settlement);
				}
			}
		}
	}
}
