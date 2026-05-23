using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class SettlementHelper
{
	private static readonly string[] StuffToCarryForMan = new string[8] { "_to_carry_foods_basket_apple", "_to_carry_merchandise_hides_b", "_to_carry_bed_convolute_g", "_to_carry_bed_convolute_a", "_to_carry_bd_fabric_c", "_to_carry_bd_basket_a", "practice_spear_t1", "simple_sparth_axe_t2" };

	private static readonly string[] StuffToCarryForWoman = new string[4] { "_to_carry_kitchen_pot_c", "_to_carry_arm_kitchen_pot_c", "_to_carry_foods_basket_apple", "_to_carry_bd_basket_a" };

	private static int _stuffToCarryIndex = MBRandom.NondeterministicRandomInt % 1024;

	public static string GetRandomStuff(bool isFemale)
	{
		string result = ((!isFemale) ? StuffToCarryForMan[_stuffToCarryIndex % StuffToCarryForMan.Length] : StuffToCarryForWoman[_stuffToCarryIndex % StuffToCarryForWoman.Length]);
		_stuffToCarryIndex++;
		return result;
	}

	public static Settlement FindNearestSettlementToSettlement(Settlement fromSettlement, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement result = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Settlement item in Settlement.All)
		{
			if (condition == null || condition(item))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, item, navCapabilities, out landRatio);
				if (num2 < num)
				{
					result = item;
					num = num2;
				}
			}
		}
		return result;
	}

	public static Settlement FindNearestSettlementToMobileParty(MobileParty mobileParty, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement result = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Settlement item in Settlement.All)
		{
			if (condition == null || condition(item))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, item, navCapabilities, out landRatio);
				if (num2 < num)
				{
					result = item;
					num = num2;
				}
			}
		}
		return result;
	}

	public static Settlement FindNearestSettlementToPoint(in CampaignVec2 point, Func<Settlement, bool> condition = null)
	{
		Settlement result = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Settlement item in Settlement.All)
		{
			if (condition == null || condition(item))
			{
				float num2 = item.Position.Distance(point);
				if (num2 < num)
				{
					result = item;
					num = num2;
				}
			}
		}
		return result;
	}

	public static Hideout FindNearestHideoutToSettlement(Settlement fromSettlement, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement settlement = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Hideout item in Hideout.All)
		{
			if (condition == null || condition(item.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, item.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					settlement = item.Settlement;
					num = num2;
				}
			}
		}
		return settlement?.Hideout;
	}

	public static Hideout FindNearestHideoutToMobileParty(MobileParty fromMobileParty, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement settlement = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Hideout item in Hideout.All)
		{
			if (condition == null || condition(item.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, item.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					settlement = item.Settlement;
					num = num2;
				}
			}
		}
		return settlement?.Hideout;
	}

	public static Town FindNearestTownToSettlement(Settlement fromSettlement, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement settlement = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Town allTown in Town.AllTowns)
		{
			if (condition == null || condition(allTown.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, allTown.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					settlement = allTown.Settlement;
					num = num2;
				}
			}
		}
		return settlement?.Town;
	}

	public static Town FindNearestTownToMobileParty(MobileParty mobileParty, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement settlement = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Town allTown in Town.AllTowns)
		{
			if (condition == null || condition(allTown.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, allTown.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					settlement = allTown.Settlement;
					num = num2;
				}
			}
		}
		return settlement?.Town;
	}

	public static int FindNextSettlementAroundMobileParty(MobileParty mobileParty, MobileParty.NavigationType navCapabilities, float maxDistance, int lastIndex, Func<Settlement, bool> condition = null)
	{
		for (int i = lastIndex + 1; i < Settlement.All.Count; i++)
		{
			Settlement settlement = Settlement.All[i];
			if ((condition == null || condition(settlement)) && DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, settlement, navCapabilities, out var _) < maxDistance)
			{
				return i;
			}
		}
		return -1;
	}

	public static Settlement FindNearestCastleToSettlement(Settlement fromSettlement, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement result = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Town allCastle in Town.AllCastles)
		{
			if (condition == null || condition(allCastle.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, allCastle.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					result = allCastle.Settlement;
					num = num2;
				}
			}
		}
		return result;
	}

	public static Settlement FindNearestCastleToMobileParty(MobileParty mobileParty, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement result = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Town allCastle in Town.AllCastles)
		{
			if (condition == null || condition(allCastle.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, allCastle.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					result = allCastle.Settlement;
					num = num2;
				}
			}
		}
		return result;
	}

	public static Settlement FindNearestFortificationToSettlement(Settlement fromSettlement, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Town town = FindNearestTownToSettlement(fromSettlement, navCapabilities, condition);
		Settlement settlement = FindNearestCastleToSettlement(fromSettlement, navCapabilities, condition);
		float num = Campaign.MapDiagonal;
		float landRatio;
		if (settlement != null)
		{
			num = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, settlement, navCapabilities, out landRatio);
		}
		float num2 = Campaign.MapDiagonal;
		if (town != null)
		{
			num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, town.Settlement, navCapabilities, out landRatio);
		}
		if (num > num2)
		{
			return town.Settlement;
		}
		return settlement;
	}

	public static Settlement FindNearestFortificationToMobileParty(MobileParty mobileParty, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Town town = ((mobileParty.CurrentSettlement != null) ? FindNearestTownToSettlement(mobileParty.CurrentSettlement, navCapabilities, condition) : FindNearestTownToMobileParty(mobileParty, navCapabilities, condition));
		Settlement settlement = ((mobileParty.CurrentSettlement != null) ? FindNearestCastleToSettlement(mobileParty.CurrentSettlement, navCapabilities, condition) : FindNearestCastleToMobileParty(mobileParty, navCapabilities, condition));
		float num = Campaign.MapDiagonal;
		float landRatio;
		if (settlement != null)
		{
			num = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, settlement, navCapabilities, out landRatio);
		}
		float num2 = Campaign.MapDiagonal;
		if (town != null)
		{
			num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, town.Settlement, navCapabilities, out landRatio);
		}
		if (num > num2)
		{
			return town.Settlement;
		}
		return settlement;
	}

	public static Settlement FindFurthestFortificationToSettlement(MBReadOnlyList<Town> candidates, MobileParty.NavigationType navCapabilities, Settlement fromSettlement, out float furthestDistance)
	{
		Settlement result = null;
		float num = float.MinValue;
		foreach (Town candidate in candidates)
		{
			float landRatio;
			float num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, candidate.Settlement, navCapabilities, out landRatio);
			if (num2 > num)
			{
				result = candidate.Settlement;
				num = num2;
			}
		}
		furthestDistance = num;
		return result;
	}

	public static Village FindNearestVillageToSettlement(Settlement fromSettlement, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement settlement = null;
		float num = Campaign.MapDiagonal * 2f;
		foreach (Village item in Village.All)
		{
			if (condition == null || condition(item.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, item.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					settlement = item.Settlement;
					num = num2;
				}
			}
		}
		return settlement?.Village;
	}

	public static Village FindNearestVillageToMobileParty(MobileParty fromParty, MobileParty.NavigationType navCapabilities, Func<Settlement, bool> condition = null)
	{
		Settlement settlement = null;
		float num = float.MaxValue;
		foreach (Village item in Village.All)
		{
			if (condition == null || condition(item.Settlement))
			{
				float landRatio;
				float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(fromParty, item.Settlement, navCapabilities, out landRatio);
				if (num2 < num)
				{
					settlement = item.Settlement;
					num = num2;
				}
			}
		}
		return settlement?.Village;
	}

	private static Settlement FindRandomInternal(Func<Settlement, bool> condition, IEnumerable<Settlement> settlementsToIterate)
	{
		List<Settlement> list = new List<Settlement>();
		foreach (Settlement item in settlementsToIterate)
		{
			if (condition(item))
			{
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			return list[MBRandom.RandomInt(list.Count)];
		}
		return null;
	}

	public static Settlement FindRandomSettlement(Func<Settlement, bool> condition = null)
	{
		return FindRandomInternal(condition, Settlement.All);
	}

	public static Settlement FindRandomHideout(Func<Settlement, bool> condition = null)
	{
		return FindRandomInternal(condition, Hideout.All.Select((Hideout x) => x.Settlement));
	}

	public static void TakeEnemyVillagersOutsideSettlements(Settlement settlementWhichChangedFaction)
	{
		if (settlementWhichChangedFaction.IsFortification)
		{
			bool flag;
			do
			{
				flag = false;
				MobileParty mobileParty = null;
				foreach (MobileParty party in settlementWhichChangedFaction.Parties)
				{
					if (party.IsVillager && party.HomeSettlement.IsVillage && party.HomeSettlement.Village.Bound == settlementWhichChangedFaction && party.HomeSettlement.MapFaction != settlementWhichChangedFaction.MapFaction)
					{
						mobileParty = party;
						flag = true;
						break;
					}
				}
				if (flag && mobileParty.MapEvent == null)
				{
					LeaveSettlementAction.ApplyForParty(mobileParty);
					mobileParty.SetMoveModeHold();
				}
			}
			while (flag);
			bool flag2;
			do
			{
				flag2 = false;
				MobileParty mobileParty2 = null;
				foreach (MobileParty party2 in settlementWhichChangedFaction.Parties)
				{
					if (party2.IsCaravan && FactionManager.IsAtWarAgainstFaction(party2.MapFaction, settlementWhichChangedFaction.MapFaction))
					{
						mobileParty2 = party2;
						flag2 = true;
						break;
					}
				}
				if (flag2 && mobileParty2.MapEvent == null)
				{
					LeaveSettlementAction.ApplyForParty(mobileParty2);
					mobileParty2.SetMoveModeHold();
				}
			}
			while (flag2);
			foreach (MobileParty item in MobileParty.All)
			{
				if ((item.IsVillager || item.IsCaravan) && item.TargetSettlement == settlementWhichChangedFaction && item.CurrentSettlement != settlementWhichChangedFaction)
				{
					item.SetMoveModeHold();
				}
			}
		}
		if (!settlementWhichChangedFaction.IsVillage)
		{
			return;
		}
		foreach (MobileParty allVillagerParty in MobileParty.AllVillagerParties)
		{
			if (allVillagerParty.HomeSettlement == settlementWhichChangedFaction && allVillagerParty.CurrentSettlement != settlementWhichChangedFaction)
			{
				if (allVillagerParty.CurrentSettlement != null && allVillagerParty.MapEvent == null)
				{
					LeaveSettlementAction.ApplyForParty(allVillagerParty);
					allVillagerParty.SetMoveModeHold();
				}
				else
				{
					allVillagerParty.SetMoveModeHold();
				}
			}
		}
	}

	public static Settlement GetRandomTown(Clan fromFaction = null)
	{
		int num = 0;
		foreach (Settlement settlement in Campaign.Current.Settlements)
		{
			if ((fromFaction == null || settlement.MapFaction == fromFaction) && (settlement.IsTown || settlement.IsVillage))
			{
				num++;
			}
		}
		int num2 = MBRandom.RandomInt(0, num - 1);
		foreach (Settlement settlement2 in Campaign.Current.Settlements)
		{
			if ((fromFaction == null || settlement2.MapFaction == fromFaction) && (settlement2.IsTown || settlement2.IsVillage))
			{
				num2--;
				if (num2 < 0)
				{
					return settlement2;
				}
			}
		}
		return null;
	}

	public static Settlement GetBestSettlementToSpawnAround(Hero hero)
	{
		Settlement result = null;
		float num = -1f;
		IFaction mapFaction = hero.MapFaction;
		foreach (Settlement item in Settlement.All)
		{
			if (item.Party.MapEvent != null)
			{
				continue;
			}
			float num2 = 0f;
			IFaction mapFaction2 = item.MapFaction;
			if (item.IsTown)
			{
				num2 = 1f;
			}
			else if (item.IsCastle)
			{
				num2 = 0.9f;
			}
			else if (item.IsVillage)
			{
				num2 = 0.8f;
			}
			else
			{
				if (!item.IsHideout)
				{
					continue;
				}
				num2 = ((mapFaction2 == mapFaction) ? 0.2f : 0f);
			}
			float num3 = 0.0001f;
			if (mapFaction2 == mapFaction)
			{
				num3 = 1f;
			}
			else if (DiplomacyHelper.IsSameFactionAndNotEliminated(mapFaction2, mapFaction))
			{
				num3 = 0.01f;
			}
			else if (FactionManager.IsNeutralWithFaction(mapFaction2, mapFaction))
			{
				num3 = 0.0005f;
			}
			float num4 = ((item.Town != null && item.Town.GarrisonParty != null && item.OwnerClan == hero.Clan) ? (item.Town.GarrisonParty.Party.CalculateCurrentStrength() / (item.IsTown ? 60f : 30f)) : 1f);
			float num5 = ((item.IsUnderRaid || item.IsUnderSiege) ? 0.1f : 1f);
			float num6 = ((item.OwnerClan == hero.Clan) ? 1f : 0.25f);
			float num7 = item.RandomFloatWithSeed((uint)hero.RandomInt(), 0.5f, 1f);
			float value = Campaign.Current.Models.MapDistanceModel.GetDistance(hero.MapFaction.FactionMidSettlement, item, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) / Campaign.MapDiagonal;
			float num8 = 1f - TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
			float num9 = num8 * num8;
			float num10 = 1f;
			if (hero.LastKnownClosestSettlement != null)
			{
				value = Campaign.Current.Models.MapDistanceModel.GetDistance(hero.LastKnownClosestSettlement, item, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) / Campaign.MapDiagonal;
				num10 = 1f - TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
				num10 *= num10;
			}
			float num11 = num9 * 0.33f + num10 * 0.66f;
			float num12 = num3 * num2 * num5 * num6 * num4 * num7 * num11;
			if (num12 > num)
			{
				num = num12;
				result = item;
			}
		}
		return result;
	}

	public static IEnumerable<Hero> GetAllHeroesOfSettlement(Settlement settlement, bool includePrisoners)
	{
		foreach (MobileParty party in settlement.Parties)
		{
			if (party.LeaderHero != null)
			{
				yield return party.LeaderHero;
			}
		}
		foreach (Hero item in settlement.HeroesWithoutParty)
		{
			yield return item;
		}
		if (!includePrisoners)
		{
			yield break;
		}
		foreach (TroopRosterElement item2 in settlement.Party.PrisonRoster.GetTroopRoster())
		{
			if (item2.Character.IsHero)
			{
				yield return item2.Character.HeroObject;
			}
		}
	}

	public static bool IsGarrisonStarving(Settlement settlement)
	{
		bool result = false;
		if (settlement.IsStarving)
		{
			result = settlement.Town.FoodChange < 0f - settlement.Town.Prosperity / (float)Campaign.Current.Models.SettlementFoodModel.NumberOfProsperityToEatOneFood;
		}
		return result;
	}

	public static void SpawnNotablesIfNeeded(Settlement settlement)
	{
		if (!settlement.IsTown && !settlement.IsVillage)
		{
			return;
		}
		List<Occupation> list = new List<Occupation>();
		if (settlement.IsTown)
		{
			list = new List<Occupation>
			{
				Occupation.GangLeader,
				Occupation.Artisan,
				Occupation.Merchant
			};
		}
		else if (settlement.IsVillage)
		{
			list = new List<Occupation>
			{
				Occupation.RuralNotable,
				Occupation.Headman
			};
		}
		float randomFloat = MBRandom.RandomFloat;
		float num = 0f;
		int num2 = 0;
		foreach (Occupation item in list)
		{
			num2 += Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, item);
		}
		num = ((settlement.Notables.Count > 0) ? ((float)(num2 - settlement.Notables.Count) / (float)num2) : 1f);
		num *= TaleWorlds.Library.MathF.Pow(num, 0.36f);
		if (!(randomFloat <= num))
		{
			return;
		}
		MBList<Occupation> mBList = new MBList<Occupation>();
		foreach (Occupation item2 in list)
		{
			int num3 = 0;
			foreach (Hero notable in settlement.Notables)
			{
				if (notable.CharacterObject.Occupation == item2)
				{
					num3++;
				}
			}
			int targetNotableCountForSettlement = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, item2);
			if (num3 < targetNotableCountForSettlement)
			{
				mBList.Add(item2);
			}
		}
		if (mBList.Count > 0)
		{
			EnterSettlementAction.ApplyForCharacterOnly(HeroCreator.CreateNotable(mBList.GetRandomElement(), settlement), settlement);
		}
	}

	public static ExplainedNumber GetGarrisonChangeExplainedNumber(Town town)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions: true);
		ExplainedNumber garrisonChangeExplainedNumber = Campaign.Current.GetCampaignBehavior<IGarrisonRecruitmentBehavior>().GetGarrisonChangeExplainedNumber(town);
		if (garrisonChangeExplainedNumber.BaseNumber > 0f)
		{
			result.AddFromExplainedNumber(garrisonChangeExplainedNumber, new TextObject("{=basevalue}Base"));
		}
		if (town.GarrisonParty != null)
		{
			int totalManCount = Campaign.Current.Models.PartyDesertionModel.GetTroopsToDesert(town.GarrisonParty).TotalManCount;
			if (totalManCount > 0)
			{
				TextObject baseText = new TextObject("{=ojBJ3aTO}Desertion");
				result.SubtractFromExplainedNumber(new ExplainedNumber(totalManCount, includeDescriptions: true), baseText);
			}
		}
		return result;
	}

	public static float GetNeighborScoreForConsideringClan(Settlement settlement, Clan consideringClan)
	{
		float num = 0f;
		if (settlement.MapFaction == consideringClan.MapFaction && settlement.IsFortification)
		{
			HashSet<Settlement> hashSet = new HashSet<Settlement>();
			HashSet<Settlement> hashSet2 = new HashSet<Settlement>();
			foreach (Settlement neighborFortification in settlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (!hashSet.Contains(neighborFortification))
				{
					hashSet.Add(neighborFortification);
					num = ((!settlement.MapFaction.IsAtWarWith(neighborFortification.MapFaction)) ? ((settlement.MapFaction != consideringClan.MapFaction) ? (num + 0.05f) : (num + 0.1f)) : (num - 0.2f));
				}
			}
			foreach (Settlement item in hashSet)
			{
				foreach (Settlement neighborFortification2 in item.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
				{
					if (!hashSet.Contains(neighborFortification2) && !hashSet2.Contains(neighborFortification2))
					{
						hashSet2.Add(neighborFortification2);
						num = ((!settlement.MapFaction.IsAtWarWith(neighborFortification2.MapFaction)) ? ((settlement.MapFaction != consideringClan.MapFaction) ? (num + 0.01f) : (num + 0.02f)) : (num - 0.04f));
					}
				}
			}
		}
		return num;
	}
}
