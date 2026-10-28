using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class DiscardItemsCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnItemsDiscardedByPlayerEvent.AddNonSerializedListener(this, OnItemsDiscardedByPlayer);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
	}

	private void OnHourlyTickParty(MobileParty mobileParty)
	{
		if (mobileParty.IsLordParty && !mobileParty.IsMainParty && mobileParty.LeaderHero != null)
		{
			HandlePartyInventory(mobileParty.Party);
		}
	}

	private void OnItemsDiscardedByPlayer(ItemRoster roster)
	{
		int xpBonusForDiscardingItems = Campaign.Current.Models.ItemDiscardModel.GetXpBonusForDiscardingItems(roster);
		if ((float)xpBonusForDiscardingItems > 0f)
		{
			MobilePartyHelper.PartyAddSharedXp(MobileParty.MainParty, xpBonusForDiscardingItems);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void HandlePartyInventory(PartyBase party)
	{
		if (party.IsMobile && party.MobileParty.IsLordParty && !party.MobileParty.IsMainParty && !party.MobileParty.IsCurrentlyAtSea)
		{
			int num = party.ItemRoster.NumberOfLivestockAnimals + party.ItemRoster.NumberOfPackAnimals + MathF.Max(0, party.ItemRoster.NumberOfMounts - party.NumberOfMenWithHorse);
			if (num > party.MemberRoster.TotalManCount)
			{
				DiscardAnimalsCausingHerdingPenalty(party.MobileParty, num - MathF.Max(0, party.ItemRoster.NumberOfMounts - party.NumberOfMenWithHorse));
			}
			if (party.MobileParty.TotalWeightCarried > (float)party.MobileParty.InventoryCapacity)
			{
				DiscardOverburdeningItemsForParty(party.MobileParty, party.MobileParty.TotalWeightCarried - (float)party.MobileParty.InventoryCapacity);
			}
		}
	}

	private void DiscardAnimalsCausingHerdingPenalty(MobileParty mobileParty, int amount)
	{
		int numberOfAnimalsToDiscard = amount;
		int num = mobileParty.ItemRoster.Count - 1;
		while (num >= 0 && numberOfAnimalsToDiscard > 0)
		{
			if (mobileParty.ItemRoster[num].EquipmentElement.Item.IsAnimal)
			{
				DiscardAnimal(mobileParty, mobileParty.ItemRoster[num], ref numberOfAnimalsToDiscard);
			}
			num--;
		}
		int num2 = mobileParty.ItemRoster.Count - 1;
		while (num2 >= 0 && numberOfAnimalsToDiscard > 0)
		{
			if (mobileParty.ItemRoster[num2].EquipmentElement.Item.IsMountable && mobileParty.ItemRoster[num2].EquipmentElement.Item.HorseComponent.IsPackAnimal)
			{
				DiscardAnimal(mobileParty, mobileParty.ItemRoster[num2], ref numberOfAnimalsToDiscard);
			}
			num2--;
		}
		int num3 = mobileParty.ItemRoster.Count - 1;
		while (num3 >= 0 && numberOfAnimalsToDiscard > 0)
		{
			if (mobileParty.ItemRoster[num3].EquipmentElement.Item.IsMountable)
			{
				DiscardAnimal(mobileParty, mobileParty.ItemRoster[num3], ref numberOfAnimalsToDiscard);
			}
			num3--;
		}
	}

	private void DiscardOverburdeningItemsForParty(MobileParty mobileParty, float totalWeightToDiscard)
	{
		int num = (int)(mobileParty.FoodChange * -20f);
		float weightLeftToDiscard = totalWeightToDiscard;
		int num2 = mobileParty.ItemRoster.Count - 1;
		while (num2 >= 0 && weightLeftToDiscard > 0f)
		{
			if (num > 0 && mobileParty.ItemRoster[num2].EquipmentElement.Item.IsFood)
			{
				if (mobileParty.ItemRoster[num2].Amount > num)
				{
					int discardLimit = mobileParty.ItemRoster[num2].Amount - num;
					num = 0;
					DiscardNecessaryAmountOfItems(mobileParty, mobileParty.ItemRoster[num2], ref weightLeftToDiscard, discardLimit);
				}
				else
				{
					num -= mobileParty.ItemRoster[num2].Amount;
				}
			}
			else
			{
				DiscardNecessaryAmountOfItems(mobileParty, mobileParty.ItemRoster[num2], ref weightLeftToDiscard);
			}
			num2--;
		}
	}

	private void DiscardNecessaryAmountOfItems(MobileParty mobileParty, ItemRosterElement itemRosterElement, ref float weightLeftToDiscard, int discardLimit = int.MaxValue)
	{
		float equipmentElementWeight = itemRosterElement.EquipmentElement.GetEquipmentElementWeight();
		int num = MBMath.ClampInt((itemRosterElement.GetRosterElementWeight() <= weightLeftToDiscard) ? itemRosterElement.Amount : MathF.Ceiling(weightLeftToDiscard / equipmentElementWeight), 0, discardLimit);
		weightLeftToDiscard -= equipmentElementWeight * (float)num;
		mobileParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num);
	}

	private void DiscardAnimal(MobileParty mobileParty, ItemRosterElement itemRosterElement, ref int numberOfAnimalsToDiscard)
	{
		int num = ((itemRosterElement.Amount > numberOfAnimalsToDiscard) ? numberOfAnimalsToDiscard : itemRosterElement.Amount);
		numberOfAnimalsToDiscard -= num;
		mobileParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num);
	}
}
