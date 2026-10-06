using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.ComponentInterfaces;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GameComponents;

public class NavalDLCShipDeploymentModel : ShipDeploymentModel
{
	private const int BaseShipDeploymentLimit = 3;

	private const int MaxShipDeploymentLimit = 8;

	public override int GetShipDeploymentLimit(MobileParty party)
	{
		int num = (ShipDeploymentModel.IgnoreDeploymentLimits ? 8 : 3);
		ExplainedNumber stat = new ExplainedNumber(num);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.PortAuthority, BattleEnvironment.Naval, party, isPrimaryBonus: true, ref stat);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.BlessingsOfTheSea, BattleEnvironment.Naval, party, isPrimaryBonus: true, ref stat);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.MerchantFleet, BattleEnvironment.Naval, party, isPrimaryBonus: true, ref stat);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.Stormrider, BattleEnvironment.Naval, party, isPrimaryBonus: false, ref stat);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.MasterAndCommander, BattleEnvironment.Naval, party, isPrimaryBonus: false, ref stat);
		return (int)stat.ResultNumber;
	}

	public override void GetMapEventPartiesOfPlayerTeams(MBReadOnlyList<MapEventParty> playerSideMapEventParties, bool isPlayerSergeant, out MapEventParty playerMapEventParty, out MBList<MapEventParty> playerTeamMapEventParties, out MBList<MapEventParty> playerAllyTeamMapEventParties)
	{
		MobileParty mainParty = MobileParty.MainParty;
		playerMapEventParty = playerSideMapEventParties.FirstOrDefault((MapEventParty mep) => !mep.IsNpcParty);
		_ = mainParty.Army;
		playerTeamMapEventParties = new MBList<MapEventParty>();
		playerAllyTeamMapEventParties = new MBList<MapEventParty>();
		IBattleCombatant allyCombatant;
		bool flag = MissionCombatantsLogic.SupportsAllyTeamOnPlayerSide(playerSideMapEventParties.Select((MapEventParty mapEventParty) => mapEventParty.Party), playerMapEventParty.Party, isPlayerSergeant, isNavalLandHybridMission: false, out allyCombatant);
		foreach (MapEventParty playerSideMapEventParty in playerSideMapEventParties)
		{
			if (PartyBase.IsPartyUnderPlayerCommand(playerSideMapEventParty.Party) || !flag)
			{
				playerTeamMapEventParties.Add(playerSideMapEventParty);
			}
			else
			{
				playerAllyTeamMapEventParties.Add(playerSideMapEventParty);
			}
		}
	}

	public override void GetShipDeploymentLimitsOfPlayerTeams(MBList<MapEventParty> playerTeamMapEventParties, MBList<MapEventParty> playerAllyTeamMapEventParties, out NavalShipDeploymentLimit playerTeamDeploymentLimit, out NavalShipDeploymentLimit playerAllyTeamDeploymentLimit)
	{
		if (!playerAllyTeamMapEventParties.IsEmpty())
		{
			playerTeamDeploymentLimit = GetTeamShipDeploymentLimit(playerTeamMapEventParties);
			playerAllyTeamDeploymentLimit = GetTeamShipDeploymentLimit(playerAllyTeamMapEventParties);
			int netDeploymentLimit = playerTeamDeploymentLimit.NetDeploymentLimit;
			int netDeploymentLimit2 = playerAllyTeamDeploymentLimit.NetDeploymentLimit;
			int num = netDeploymentLimit + netDeploymentLimit2;
			if (num > 8)
			{
				num = 8;
				float num2 = (float)netDeploymentLimit / (float)(netDeploymentLimit + netDeploymentLimit2);
				int num3 = MathF.Min(MathF.Max(1, MathF.Round(num2 * (float)num)), netDeploymentLimit);
				int num4 = num - num3;
				if (num3 > playerTeamDeploymentLimit.SkeletalCrewLimit)
				{
					int num5 = num3 - playerTeamDeploymentLimit.SkeletalCrewLimit;
					num3 -= num5;
					num4 = MathF.Min(num4 + num5, playerAllyTeamDeploymentLimit.SkeletalCrewLimit);
				}
				if (num4 > playerAllyTeamDeploymentLimit.SkeletalCrewLimit)
				{
					int num6 = num4 - playerAllyTeamDeploymentLimit.SkeletalCrewLimit;
					num4 -= num6;
					num3 = MathF.Min(num3 + num6, playerTeamDeploymentLimit.SkeletalCrewLimit);
				}
				playerTeamDeploymentLimit = new NavalShipDeploymentLimit(playerTeamDeploymentLimit.PartiesLimit, playerTeamDeploymentLimit.SkeletalCrewLimit, num3);
				playerAllyTeamDeploymentLimit = new NavalShipDeploymentLimit(playerAllyTeamDeploymentLimit.PartiesLimit, playerAllyTeamDeploymentLimit.SkeletalCrewLimit, num4);
			}
		}
		else
		{
			playerTeamDeploymentLimit = GetTeamShipDeploymentLimit(playerTeamMapEventParties);
			playerAllyTeamDeploymentLimit = NavalShipDeploymentLimit.Invalid();
		}
	}

	public override NavalShipDeploymentLimit GetTeamShipDeploymentLimit(MBReadOnlyList<MapEventParty> teamMapEventParties)
	{
		int num = 0;
		MBList<Ship> mBList = new MBList<Ship>();
		int num2 = 0;
		foreach (MapEventParty teamMapEventParty in teamMapEventParties)
		{
			MobileParty mobileParty = teamMapEventParty.Party.MobileParty;
			if (mobileParty != null)
			{
				mBList.AddRange(mobileParty.Ships);
				num += mobileParty.Party.NumberOfHealthyMembers;
				num2 += NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetShipDeploymentLimit(mobileParty);
			}
		}
		mBList.Sort((Ship s1, Ship s2) => s1.SkeletalCrewCapacity.CompareTo(s2.SkeletalCrewCapacity));
		int num3 = num;
		int num4 = 0;
		foreach (Ship item in mBList)
		{
			if (num3 >= item.SkeletalCrewCapacity)
			{
				num3 -= item.SkeletalCrewCapacity;
				num4++;
				continue;
			}
			break;
		}
		num4 = MathF.Min(MathF.Max(num4, 1), 8);
		num2 = MathF.Min(num2, 8);
		return new NavalShipDeploymentLimit(num2, num4, MathF.Max(num2, num4));
	}

	public override Ship GetSuitablePlayerShip(MapEventParty playerMapEventParty, MBList<MapEventParty> playerTeamMapEventParties)
	{
		int playerTeamTroopCount = playerTeamMapEventParties.Sum((MapEventParty mep) => mep.Party.NumberOfHealthyMembers);
		Ship ship = null;
		if (!playerMapEventParty.Ships.IsEmpty())
		{
			IEnumerable<Ship> source = playerMapEventParty.Ships.Where((Ship s1) => s1.SkeletalCrewCapacity <= playerTeamTroopCount);
			if (!source.IsEmpty())
			{
				return source.MaxBy((Ship ship2) => ship2.GetCombatFactor());
			}
			return playerMapEventParty.Ships.MinBy((Ship ship2) => ship2.SkeletalCrewCapacity);
		}
		MBList<Ship> mBList = new MBList<Ship>();
		foreach (MapEventParty playerTeamMapEventParty in playerTeamMapEventParties)
		{
			mBList.AddRange(playerTeamMapEventParty.Ships);
		}
		IEnumerable<Ship> source2 = mBList.Where((Ship s1) => s1.SkeletalCrewCapacity <= playerTeamTroopCount);
		if (!source2.IsEmpty())
		{
			return source2.MinBy((Ship ship2) => ship2.GetCombatFactor());
		}
		return mBList.MinBy((Ship ship2) => ship2.SkeletalCrewCapacity);
	}

	public override void FillShipsOfTeamParties(MBReadOnlyList<MapEventParty> teamMapEventParties, NavalShipDeploymentLimit shipDeploymentLimit, MBList<IShipOrigin> teamShips)
	{
		int netDeploymentLimit = shipDeploymentLimit.NetDeploymentLimit;
		IOrderedEnumerable<MapEventParty> orderedEnumerable = teamMapEventParties.OrderByDescending((MapEventParty teamEventParty) => GetNavalPartyPriority(teamEventParty.Party));
		int troopCount = orderedEnumerable.Sum((MapEventParty party) => party.Party.NumberOfHealthyMembers);
		MBList<(Ship ship, MapEventParty party, bool fixedShip)> candidateShips = new MBList<(Ship, MapEventParty, bool)>();
		foreach (IShipOrigin teamShip in teamShips)
		{
			foreach (MapEventParty item2 in orderedEnumerable)
			{
				if (item2.Ships.Contains(teamShip))
				{
					candidateShips.Add(((Ship)teamShip, item2, true));
					break;
				}
			}
		}
		teamShips.Clear();
		int num = 0;
		Dictionary<MapEventParty, MBQueue<(Ship, bool)>> dictionary = new Dictionary<MapEventParty, MBQueue<(Ship, bool)>>();
		MBList<(Ship, bool)> mBList = new MBList<(Ship, bool)>();
		foreach (MapEventParty item3 in orderedEnumerable)
		{
			foreach (Ship ship in item3.Ships)
			{
				mBList.Add((ship, false));
			}
			if (!candidateShips.IsEmpty())
			{
				mBList.RemoveAll(((Ship ship, bool isReplaced) teamShipTuple) => candidateShips.Any(((Ship ship, MapEventParty party, bool fixedShip) candidateShipTuple) => candidateShipTuple.ship == teamShipTuple.ship));
			}
			mBList.Sort(((Ship ship, bool isReplaced) firstShipTuple, (Ship ship, bool isReplaced) secondShipTuple) => secondShipTuple.ship.GetCombatFactor().CompareTo(firstShipTuple.ship.GetCombatFactor()));
			num += mBList.Count;
			dictionary[item3] = new MBQueue<(Ship, bool)>(mBList);
			mBList.Clear();
		}
		bool flag = true;
		while (flag && candidateShips.Count < netDeploymentLimit)
		{
			flag = false;
			foreach (MapEventParty item4 in orderedEnumerable)
			{
				MBQueue<(Ship, bool)> mBQueue = dictionary[item4];
				if (!mBQueue.IsEmpty())
				{
					(Ship, bool) tuple = mBQueue.Dequeue();
					num--;
					candidateShips.Add((tuple.Item1, item4, false));
					flag = true;
				}
			}
		}
		if (num > 0)
		{
			int firstUnfilledIndex;
			bool flag2 = CanShipsBeFilled(troopCount, 0.65f, candidateShips, out firstUnfilledIndex);
			bool flag3 = true;
			while (flag3 && !flag2)
			{
				flag3 = false;
				for (int num2 = firstUnfilledIndex; num2 >= 0; num2--)
				{
					(Ship, MapEventParty, bool) tuple2 = candidateShips[num2];
					if (!tuple2.Item3)
					{
						MapEventParty item = tuple2.Item2;
						MBQueue<(Ship, bool)> mBQueue2 = dictionary[item];
						if (!mBQueue2.IsEmpty())
						{
							(Ship, bool) tuple3 = mBQueue2.Peek();
							if (!tuple3.Item2)
							{
								mBQueue2.Dequeue();
								mBQueue2.Enqueue((tuple2.Item1, true));
								candidateShips[num2] = (tuple3.Item1, item, false);
								flag3 = true;
							}
						}
					}
					flag2 = CanShipsBeFilled(troopCount, 0.65f, candidateShips, out firstUnfilledIndex);
					if (flag2)
					{
						break;
					}
				}
			}
		}
		if (num > 0)
		{
			flag = true;
			while (flag)
			{
				flag = false;
				foreach (MapEventParty item5 in orderedEnumerable)
				{
					MBQueue<(Ship, bool)> mBQueue3 = dictionary[item5];
					if (!mBQueue3.IsEmpty())
					{
						(Ship, bool) tuple4 = mBQueue3.Dequeue();
						num--;
						candidateShips.Add((tuple4.Item1, item5, false));
						flag = true;
					}
				}
			}
		}
		dictionary.Clear();
		if (candidateShips.Count > netDeploymentLimit)
		{
			bool flag4 = false;
			bool flag5 = true;
			while (!flag4 && flag5)
			{
				flag5 = false;
				flag4 = IsSkeletalCrewLimitationSatisfied(candidateShips, troopCount, netDeploymentLimit);
				if (flag4)
				{
					continue;
				}
				for (int num3 = netDeploymentLimit - 1; num3 >= 0; num3--)
				{
					(Ship, MapEventParty, bool) shipTupleToBeSwapped = candidateShips[num3];
					if (!shipTupleToBeSwapped.Item3)
					{
						_ = shipTupleToBeSwapped.Item1.SkeletalCrewCapacity;
						int swapIndex = -1;
						if (FindBestSwapShipBelowSkeletalCrewLimit(candidateShips, shipTupleToBeSwapped, netDeploymentLimit, checkTeamMatch: true, out swapIndex))
						{
							(Ship, MapEventParty, bool) value = candidateShips[num3];
							candidateShips[num3] = candidateShips[swapIndex];
							candidateShips[swapIndex] = value;
							flag5 = true;
							break;
						}
						if (FindBestSwapShipBelowSkeletalCrewLimit(candidateShips, shipTupleToBeSwapped, netDeploymentLimit, checkTeamMatch: false, out swapIndex))
						{
							(Ship, MapEventParty, bool) value2 = candidateShips[num3];
							candidateShips[num3] = candidateShips[swapIndex];
							candidateShips[swapIndex] = value2;
							flag5 = true;
							break;
						}
					}
				}
			}
		}
		if (candidateShips.Count > netDeploymentLimit)
		{
			MBList<(Ship, MapEventParty, bool)> mBList2 = candidateShips.Skip(netDeploymentLimit).ToMBList();
			candidateShips.RemoveRange(netDeploymentLimit, candidateShips.Count - netDeploymentLimit);
			mBList2.Sort(((Ship ship, MapEventParty party, bool fixedShip) s1, (Ship ship, MapEventParty party, bool fixedShip) s2) => s2.ship.TotalCrewCapacity.CompareTo(s1.ship.TotalCrewCapacity));
			candidateShips.AddRange(mBList2);
		}
		foreach (var item6 in candidateShips)
		{
			teamShips.Add(item6.ship);
		}
	}

	public override void GetOrderedCaptainsForPlayerTeamShips(MBReadOnlyList<MapEventParty> playerTeamMapEventParties, MBReadOnlyList<IShipOrigin> playerTeamShips, out List<string> playerTeamCaptainsByPriority)
	{
		List<string> list = HeroHelper.OrderHeroesOnPlayerSideByPriority(includeArmyLeader: true, includePlayerCompanions: true);
		playerTeamCaptainsByPriority = new List<string>(playerTeamShips.Count);
		foreach (IShipOrigin ship in playerTeamShips)
		{
			MapEventParty shipParty = playerTeamMapEventParties.FirstOrDefault((MapEventParty mep) => mep.Ships.Contains(ship));
			string text = list.FirstOrDefault((string heroId) => shipParty.Party.LeaderHero?.StringId.Equals(heroId) ?? false);
			if (text != null)
			{
				playerTeamCaptainsByPriority.Add(text);
				list.Remove(text);
			}
			else
			{
				playerTeamCaptainsByPriority.Add(string.Empty);
			}
		}
		for (int num = 0; num < playerTeamCaptainsByPriority.Count; num++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			if (playerTeamCaptainsByPriority[num].IsEmpty())
			{
				playerTeamCaptainsByPriority[num] = list[0];
				list.RemoveAt(0);
			}
		}
		int num2 = -1;
		int num3 = playerTeamCaptainsByPriority.Count - 1;
		while (num3 >= 0 && playerTeamCaptainsByPriority[num3].IsEmpty())
		{
			num2 = num3;
			num3--;
		}
		if (num2 >= 0)
		{
			playerTeamCaptainsByPriority.RemoveRange(num2, playerTeamCaptainsByPriority.Count - num2);
		}
		int num4 = 0;
		for (int num5 = 0; num5 < playerTeamCaptainsByPriority.Count; num5++)
		{
			if (!playerTeamCaptainsByPriority[num5].IsEmpty())
			{
				continue;
			}
			for (int num6 = playerTeamCaptainsByPriority.Count - 1 - num4; num6 > num5; num6--)
			{
				if (!playerTeamCaptainsByPriority[num6].IsEmpty())
				{
					playerTeamCaptainsByPriority[num5] = playerTeamCaptainsByPriority[num6];
					playerTeamCaptainsByPriority[num6] = string.Empty;
					num4++;
					break;
				}
			}
		}
		playerTeamCaptainsByPriority.RemoveAll((string entry) => entry.IsEmpty());
	}

	public override int GetMaximumDeployableTroopCountForTeam(MBList<IShipOrigin> teamShips, bool isPlayerTeam = false)
	{
		int num = 0;
		if (teamShips != null && teamShips.Count > 0)
		{
			int num2 = MathF.Min(8, teamShips.Count);
			if (isPlayerTeam)
			{
				List<IShipOrigin> list = teamShips.OrderByDescending((IShipOrigin ship) => ship.TotalCrewCapacity).ToList();
				for (int num3 = 0; num3 < num2; num3++)
				{
					num += list[num3].TotalCrewCapacity;
				}
			}
			else
			{
				for (int num4 = 0; num4 < num2; num4++)
				{
					num += teamShips[num4].TotalCrewCapacity;
				}
			}
		}
		return num;
	}

	private static float GetNavalPartyPriority(PartyBase party)
	{
		float num = 0f;
		IFaction mapFaction = party.MapFaction;
		if (mapFaction != null && mapFaction.IsClan)
		{
			Clan clan = (Clan)mapFaction;
			Hero leaderHero = party.LeaderHero;
			Kingdom kingdom = clan.Kingdom;
			if (leaderHero != null)
			{
				if (kingdom != null && leaderHero == kingdom.Leader)
				{
					num += 100000f;
				}
				if (leaderHero == clan.Leader)
				{
					num += 10000f;
				}
			}
			int maxClanTier = Campaign.Current.Models.ClanTierModel.MaxClanTier;
			int minClanTier = Campaign.Current.Models.ClanTierModel.MinClanTier;
			float num2 = MathF.Clamp((float)(clan.Tier - minClanTier) / (float)maxClanTier, 0f, 1f);
			num += num2 * 1000f;
		}
		return num;
	}

	private static bool CanShipsBeFilled(int troopCount, float fillPercentage, MBReadOnlyList<(Ship ship, MapEventParty party, bool fixedShip)> ships, out int firstUnfilledIndex)
	{
		int num = troopCount;
		int num2 = ships.Count - 1;
		while (num2 >= 0)
		{
			int num3 = (int)((float)ships[num2].ship.TotalCrewCapacity * fillPercentage);
			if (num >= num3)
			{
				num -= num3;
				num2--;
				continue;
			}
			firstUnfilledIndex = num2;
			return false;
		}
		firstUnfilledIndex = -1;
		return true;
	}

	private static bool IsSkeletalCrewLimitationSatisfied(MBList<(Ship ship, MapEventParty party, bool fixedShip)> ships, int troopCount, int shipsToProcessCount)
	{
		int num = MathF.Min(shipsToProcessCount, ships.Count);
		int num2 = troopCount;
		for (int i = 0; i < num; i++)
		{
			(Ship, MapEventParty, bool) tuple = ships[i];
			if (num2 < tuple.Item1.SkeletalCrewCapacity)
			{
				break;
			}
			num2 -= tuple.Item1.SkeletalCrewCapacity;
		}
		return num2 >= 0;
	}

	private static bool FindBestSwapShipBelowSkeletalCrewLimit(MBList<(Ship ship, MapEventParty party, bool fixedShip)> ships, (Ship ship, MapEventParty party, bool fixedShip) shipTupleToBeSwapped, int startIndex, bool checkTeamMatch, out int swapIndex)
	{
		swapIndex = -1;
		int num = 0;
		int skeletalCrewCapacity = shipTupleToBeSwapped.ship.SkeletalCrewCapacity;
		for (int i = startIndex; i < ships.Count; i++)
		{
			(Ship, MapEventParty, bool) tuple = ships[i];
			if (!tuple.Item3 && (!checkTeamMatch || tuple.Item2 == shipTupleToBeSwapped.party))
			{
				int skeletalCrewCapacity2 = tuple.Item1.SkeletalCrewCapacity;
				if (skeletalCrewCapacity2 < skeletalCrewCapacity && skeletalCrewCapacity2 > num)
				{
					swapIndex = i;
					num = skeletalCrewCapacity2;
				}
			}
		}
		return swapIndex > -1;
	}
}
