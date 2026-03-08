using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x02000007 RID: 7
	public static class TownHelpers
	{
		// Token: 0x06000021 RID: 33 RVA: 0x0000378C File Offset: 0x0000198C
		public static ValueTuple<int, int> GetTownFoodAndMarketStocks(Town town)
		{
			float num = ((town != null) ? town.FoodStocks : 0f);
			float num2 = 0f;
			if (town != null && town.IsTown)
			{
				for (int i = town.Owner.ItemRoster.Count - 1; i >= 0; i--)
				{
					ItemRosterElement elementCopyAtIndex = town.Owner.ItemRoster.GetElementCopyAtIndex(i);
					if (elementCopyAtIndex.EquipmentElement.Item != null && elementCopyAtIndex.EquipmentElement.Item.ItemCategory.Properties == ItemCategory.Property.BonusToFoodStores)
					{
						num2 += (float)elementCopyAtIndex.Amount;
					}
				}
			}
			return new ValueTuple<int, int>((int)num, (int)num2);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00003830 File Offset: 0x00001A30
		public static bool IsThereAnyoneToMeetInTown(Settlement settlement)
		{
			foreach (MobileParty mobileParty in settlement.Parties.Where(new Func<MobileParty, bool>(TownHelpers.RequestAMeetingPartyCondition)))
			{
				using (List<TroopRosterElement>.Enumerator enumerator2 = mobileParty.MemberRoster.GetTroopRoster().GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.Character.IsHero)
						{
							return true;
						}
					}
				}
			}
			using (IEnumerator<Hero> enumerator3 = settlement.HeroesWithoutParty.Where(new Func<Hero, bool>(TownHelpers.RequestAMeetingHeroWithoutPartyCondition)).GetEnumerator())
			{
				if (enumerator3.MoveNext())
				{
					Hero hero = enumerator3.Current;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00003924 File Offset: 0x00001B24
		public static List<Hero> GetHeroesToMeetInTown(Settlement settlement)
		{
			List<Hero> list = new List<Hero>();
			foreach (MobileParty mobileParty in settlement.Parties.Where(new Func<MobileParty, bool>(TownHelpers.RequestAMeetingPartyCondition)))
			{
				foreach (TroopRosterElement troopRosterElement in mobileParty.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character.IsHero)
					{
						list.Add(troopRosterElement.Character.HeroObject);
					}
				}
			}
			foreach (Hero item in settlement.HeroesWithoutParty.Where(new Func<Hero, bool>(TownHelpers.RequestAMeetingHeroWithoutPartyCondition)))
			{
				list.Add(item);
			}
			return list;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00003A34 File Offset: 0x00001C34
		public static MBList<Hero> GetHeroesInSettlement(Settlement settlement, Predicate<Hero> predicate = null)
		{
			MBList<Hero> mblist = new MBList<Hero>();
			foreach (MobileParty mobileParty in settlement.Parties)
			{
				foreach (TroopRosterElement troopRosterElement in mobileParty.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character.IsHero && (predicate == null || predicate(troopRosterElement.Character.HeroObject)))
					{
						mblist.Add(troopRosterElement.Character.HeroObject);
					}
				}
			}
			foreach (Hero hero in settlement.HeroesWithoutParty)
			{
				if (predicate == null || predicate(hero))
				{
					mblist.Add(hero);
				}
			}
			return mblist;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00003B4C File Offset: 0x00001D4C
		public static bool RequestAMeetingPartyCondition(MobileParty party)
		{
			return party.IsLordParty && !party.IsMainParty && (party.Army == null || party.Army != MobileParty.MainParty.Army);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00003B7F File Offset: 0x00001D7F
		public static bool RequestAMeetingHeroWithoutPartyCondition(Hero hero)
		{
			return hero.CharacterObject.Occupation == Occupation.Lord && !hero.IsPrisoner && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00003BBC File Offset: 0x00001DBC
		public static float CalculatePriceDeviationRatio(Town town, EquipmentElement equipmentElement)
		{
			int itemPrice = town.GetItemPrice(equipmentElement, null, false);
			float num = 0f;
			float result = 1f;
			if (Town.AllTowns != null)
			{
				foreach (Town town2 in Town.AllTowns)
				{
					num += (float)town2.GetItemPrice(equipmentElement, null, false);
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
}
