using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers;

public static class TownHelpers
{
	public static (int, int) GetTownFoodAndMarketStocks(Town town)
	{
		float num = town?.FoodStocks ?? 0f;
		float num2 = 0f;
		if (town != null && town.IsTown)
		{
			for (int num3 = town.Owner.ItemRoster.Count - 1; num3 >= 0; num3--)
			{
				ItemRosterElement elementCopyAtIndex = town.Owner.ItemRoster.GetElementCopyAtIndex(num3);
				if (elementCopyAtIndex.EquipmentElement.Item != null && elementCopyAtIndex.EquipmentElement.Item.ItemCategory.Properties == ItemCategory.Property.BonusToFoodStores)
				{
					num2 += (float)elementCopyAtIndex.Amount;
				}
			}
		}
		return ((int)num, (int)num2);
	}

	public static bool IsThereAnyoneToMeetInTown(Settlement settlement)
	{
		foreach (MobileParty item in settlement.Parties.Where(RequestAMeetingPartyCondition))
		{
			foreach (TroopRosterElement item2 in item.MemberRoster.GetTroopRoster())
			{
				if (item2.Character.IsHero)
				{
					return true;
				}
			}
		}
		using (IEnumerator<Hero> enumerator3 = settlement.HeroesWithoutParty.Where(RequestAMeetingHeroWithoutPartyCondition).GetEnumerator())
		{
			if (enumerator3.MoveNext())
			{
				_ = enumerator3.Current;
				return true;
			}
		}
		return false;
	}

	public static List<Hero> GetHeroesToMeetInTown(Settlement settlement)
	{
		List<Hero> list = new List<Hero>();
		foreach (MobileParty item in settlement.Parties.Where(RequestAMeetingPartyCondition))
		{
			foreach (TroopRosterElement item2 in item.MemberRoster.GetTroopRoster())
			{
				if (item2.Character.IsHero)
				{
					list.Add(item2.Character.HeroObject);
				}
			}
		}
		foreach (Hero item3 in settlement.HeroesWithoutParty.Where(RequestAMeetingHeroWithoutPartyCondition))
		{
			list.Add(item3);
		}
		return list;
	}

	public static MBList<Hero> GetHeroesInSettlement(Settlement settlement, Predicate<Hero> predicate = null)
	{
		MBList<Hero> mBList = new MBList<Hero>();
		foreach (MobileParty party in settlement.Parties)
		{
			foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
			{
				if (item.Character.IsHero && (predicate == null || predicate(item.Character.HeroObject)))
				{
					mBList.Add(item.Character.HeroObject);
				}
			}
		}
		foreach (Hero item2 in settlement.HeroesWithoutParty)
		{
			if (predicate == null || predicate(item2))
			{
				mBList.Add(item2);
			}
		}
		return mBList;
	}

	public static bool RequestAMeetingPartyCondition(MobileParty party)
	{
		if (party.IsLordParty && !party.IsMainParty)
		{
			if (party.Army != null)
			{
				return party.Army != MobileParty.MainParty.Army;
			}
			return true;
		}
		return false;
	}

	public static bool RequestAMeetingHeroWithoutPartyCondition(Hero hero)
	{
		if (hero.CharacterObject.Occupation == Occupation.Lord && !hero.IsPrisoner)
		{
			return hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge;
		}
		return false;
	}

	public static float CalculatePriceDeviationRatio(Town town, EquipmentElement equipmentElement)
	{
		int itemPrice = town.GetItemPrice(equipmentElement);
		float num = 0f;
		float result = 1f;
		if (Town.AllTowns != null)
		{
			foreach (Town allTown in Town.AllTowns)
			{
				num += (float)allTown.GetItemPrice(equipmentElement);
			}
			if (num != 0f)
			{
				float num2 = num / (float)Town.AllTowns.Count;
				result = ((float)itemPrice - num2) / num2;
			}
		}
		return result;
	}
}
