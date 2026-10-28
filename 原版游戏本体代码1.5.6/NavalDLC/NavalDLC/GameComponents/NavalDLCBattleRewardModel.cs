using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.CampaignBehaviors;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace NavalDLC.GameComponents;

public class NavalDLCBattleRewardModel : BattleRewardModel
{
	public override int CalculateGoldLossAfterDefeat(Hero partyLeaderHero)
	{
		return base.BaseModel.CalculateGoldLossAfterDefeat(partyLeaderHero);
	}

	public override ExplainedNumber CalculateInfluenceGain(PartyBase winnerParty, float influenceValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, float influenceMultiplierForWinnerSide, bool includeDescriptions)
	{
		return base.BaseModel.CalculateInfluenceGain(winnerParty, influenceValueOfBattleForWinnerSide, contributionShareOfWinnerParty, influenceMultiplierForWinnerSide, includeDescriptions);
	}

	public override float CalculateMoraleChangeOnRoundVictory(PartyBase party, MapEventSide partySide, BattleSideEnum roundWinner)
	{
		return base.BaseModel.CalculateMoraleChangeOnRoundVictory(party, partySide, roundWinner);
	}

	public override ExplainedNumber CalculateMoraleGainVictory(PartyBase winnerParty, float renownValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, bool includeDescriptions)
	{
		return base.BaseModel.CalculateMoraleGainVictory(winnerParty, renownValueOfBattleForWinnerSide, contributionShareOfWinnerParty, includeDescriptions);
	}

	public override int CalculatePlunderedGoldAmountFromDefeatedParty(PartyBase defeatedParty)
	{
		return base.BaseModel.CalculatePlunderedGoldAmountFromDefeatedParty(defeatedParty);
	}

	public override ExplainedNumber CalculateRenownGain(PartyBase winnerParty, float renownValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, float renownMultiplierForWinnerSide, bool includeDescriptions)
	{
		return base.BaseModel.CalculateRenownGain(winnerParty, renownValueOfBattleForWinnerSide, contributionShareOfWinnerParty, renownMultiplierForWinnerSide, includeDescriptions);
	}

	public override float GetAITradePenalty()
	{
		return base.BaseModel.GetAITradePenalty();
	}

	public override float GetBannerLootChanceFromDefeatedHero(Hero defeatedHero)
	{
		return base.BaseModel.GetBannerLootChanceFromDefeatedHero(defeatedHero);
	}

	public override ItemObject GetBannerRewardForWinningMapEvent(MapEvent mapEvent)
	{
		return base.BaseModel.GetBannerRewardForWinningMapEvent(mapEvent);
	}

	public override float GetExpectedLootedItemValueFromCasualty(Hero winnerPartyLeaderHero, CharacterObject casualtyCharacter)
	{
		return base.BaseModel.GetExpectedLootedItemValueFromCasualty(winnerPartyLeaderHero, casualtyCharacter);
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootCasualtyChances(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
	{
		return base.BaseModel.GetLootCasualtyChances(winnerParties, defeatedParty);
	}

	public override EquipmentElement GetLootedItemFromTroop(CharacterObject character, float targetValue)
	{
		return base.BaseModel.GetLootedItemFromTroop(character, targetValue);
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootGoldChances(MBReadOnlyList<MapEventParty> winnerParties)
	{
		return base.BaseModel.GetLootGoldChances(winnerParties);
	}

	public override MBList<KeyValuePair<MapEventParty, float>> GetLootItemChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
	{
		MBList<KeyValuePair<MapEventParty, float>> lootItemChancesForWinnerParties = base.BaseModel.GetLootItemChancesForWinnerParties(winnerParties, defeatedParty);
		if (defeatedParty.IsMobile && (defeatedParty.MobileParty.IsCaravan || defeatedParty.MobileParty.IsVillager))
		{
			for (int i = 0; i < lootItemChancesForWinnerParties.Count; i++)
			{
				PartyBase party = lootItemChancesForWinnerParties[i].Key.Party;
				ExplainedNumber result = new ExplainedNumber(lootItemChancesForWinnerParties[i].Value);
				FeatHelper.ApplyCultureFeat(party, NavalCulturalFeats.NordHostileActionBonusFeat, ref result);
				if (defeatedParty.MobileParty.IsCaravan && defeatedParty.MobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability)
				{
					PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.PiratesProwess, party.MobileParty, isPrimaryBonus: false, ref result);
				}
				lootItemChancesForWinnerParties[i] = new KeyValuePair<MapEventParty, float>(lootItemChancesForWinnerParties[i].Key, result.ResultNumber);
			}
		}
		return lootItemChancesForWinnerParties;
	}

	public override void GetCaptureMemberChancesForWinnerParties(MapEvent endedMapEvent, MBReadOnlyList<MapEventParty> winnerParties, out MBList<KeyValuePair<MapEventParty, float>> woundedMemberChances, out MBList<KeyValuePair<MapEventParty, float>> healthyMemberChances)
	{
		woundedMemberChances = new MBList<KeyValuePair<MapEventParty, float>>();
		healthyMemberChances = new MBList<KeyValuePair<MapEventParty, float>>();
		base.BaseModel.GetCaptureMemberChancesForWinnerParties(endedMapEvent, winnerParties, out woundedMemberChances, out healthyMemberChances);
		float num = 0f;
		for (int i = 0; i < woundedMemberChances.Count; i++)
		{
			KeyValuePair<MapEventParty, float> keyValuePair = woundedMemberChances[i];
			MapEventParty key = keyValuePair.Key;
			ExplainedNumber stat = new ExplainedNumber(keyValuePair.Value);
			if (key.Party.IsMobile)
			{
				PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.RiverRaider, key.Party.MobileParty, isPrimaryBonus: false, ref stat);
			}
			woundedMemberChances[i] = new KeyValuePair<MapEventParty, float>(key, stat.ResultNumber);
			num += woundedMemberChances[i].Value;
		}
		if (num > 0f)
		{
			for (int j = 0; j < woundedMemberChances.Count; j++)
			{
				woundedMemberChances[j] = new KeyValuePair<MapEventParty, float>(woundedMemberChances[j].Key, woundedMemberChances[j].Value / num);
			}
		}
		num = 0f;
		for (int k = 0; k < healthyMemberChances.Count; k++)
		{
			KeyValuePair<MapEventParty, float> keyValuePair2 = healthyMemberChances[k];
			MapEventParty key2 = keyValuePair2.Key;
			ExplainedNumber stat2 = new ExplainedNumber(keyValuePair2.Value);
			if (key2.Party.IsMobile)
			{
				PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.RiverRaider, key2.Party.MobileParty, isPrimaryBonus: false, ref stat2);
			}
			healthyMemberChances[k] = new KeyValuePair<MapEventParty, float>(key2, stat2.ResultNumber);
			num += healthyMemberChances[k].Value;
		}
		if (num > 0f)
		{
			for (int l = 0; l < healthyMemberChances.Count; l++)
			{
				healthyMemberChances[l] = new KeyValuePair<MapEventParty, float>(healthyMemberChances[l].Key, healthyMemberChances[l].Value / num);
			}
		}
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootPrisonerChances(MBReadOnlyList<MapEventParty> winnerParties, TroopRosterElement prisonerElement)
	{
		return base.BaseModel.GetLootPrisonerChances(winnerParties, prisonerElement);
	}

	public override float CalculateShipDamageAfterDefeat(Ship ship)
	{
		return ship.MaxHitPoints * MBRandom.RandomFloatRanged(0.2f, 0.5f);
	}

	public override MBReadOnlyList<KeyValuePair<Ship, MapEventParty>> DistributeDefeatedPartyShipsAmongWinners(MapEvent mapEvent, MBReadOnlyList<Ship> shipsToLoot, MBReadOnlyList<MapEventParty> winnerParties)
	{
		if (mapEvent.IsPlayerMapEvent && NavalStorylineData.IsNavalStoryLineActive())
		{
			return new MBReadOnlyList<KeyValuePair<Ship, MapEventParty>>();
		}
		Dictionary<Ship, MapEventParty> dictionary = new Dictionary<Ship, MapEventParty>();
		MBList<Ship> mBList = new MBList<Ship>();
		foreach (Ship item in shipsToLoot)
		{
			dictionary.Add(item, null);
			if (MBRandom.RandomFloat < 0.5f)
			{
				if (item.CanEquipFigurehead)
				{
					item.ChangeFigurehead(null);
				}
				mBList.Add(item);
			}
		}
		IEnumerable<MapEventParty> source = winnerParties.WhereQ((MapEventParty x) => x.Party.IsMobile && x.Party.MobileParty.PartyComponent.CanHaveNavalNavigationCapability && !x.Party.MobileParty.IsPatrolParty);
		if (source.AnyQ())
		{
			float winnerPartiesTotalScoreForLootingShips = source.SumQ((MapEventParty x) => PartyLootShipScore(x));
			List<MapEventParty> list = source.OrderByQ((MapEventParty x) => (float)x.Party.Ships.Count + (1f - PartyLootShipScore(x) / winnerPartiesTotalScoreForLootingShips)).ToList();
			List<MapEventParty> source2 = new List<MapEventParty>();
			if (mBList.Count < list.Count)
			{
				source2 = list.GetRange(mBList.Count, list.Count - mBList.Count).ToList();
				list.RemoveRange(mBList.Count, list.Count - mBList.Count);
			}
			list = list.OrderByDescending((MapEventParty x) => PartyLootShipScore(x)).ToList();
			if (source2.AnyQ())
			{
				list.AddRange(source2.OrderByDescending((MapEventParty x) => PartyLootShipScore(x)).ToList());
			}
			bool flag = true;
			while (flag && mBList.Count > 0)
			{
				flag = false;
				foreach (MapEventParty item2 in list)
				{
					MBList<Ship> mBList2 = item2.Ships.ToMBList();
					foreach (KeyValuePair<Ship, MapEventParty> item3 in dictionary)
					{
						if (item3.Value == item2)
						{
							mBList2.Add(item3.Key);
						}
					}
					Ship shipToLootForWinnerParty = GetShipToLootForWinnerParty(item2, mBList2, mBList);
					if (shipToLootForWinnerParty != null)
					{
						flag = true;
						dictionary[shipToLootForWinnerParty] = item2;
						mBList.Remove(shipToLootForWinnerParty);
					}
					if (mBList.Count == 0)
					{
						break;
					}
				}
				if (mBList.Count > 0)
				{
					list = list.OrderByDescending((MapEventParty x) => PartyLootShipScore(x)).ToList();
				}
			}
		}
		if (mBList.Count > 0 && winnerParties.AnyQ((MapEventParty x) => x.Party == PartyBase.MainParty))
		{
			mBList.Shuffle();
			int num = dictionary.CountQ((KeyValuePair<Ship, MapEventParty> x) => x.Value?.Party == PartyBase.MainParty);
			if (mBList.Count + num > 25)
			{
				mBList = mBList.Take(25 - num).ToMBList();
			}
			MapEventParty mapEventParty = winnerParties.Find((MapEventParty x) => x.Party == PartyBase.MainParty);
			int num2 = 0;
			foreach (MapEventParty winnerParty in winnerParties)
			{
				int contributionToBattle = winnerParty.ContributionToBattle;
				num2 += contributionToBattle;
			}
			foreach (Ship item4 in mBList)
			{
				if (MBRandom.RandomInt(num2) < mapEventParty.ContributionToBattle)
				{
					dictionary[item4] = mapEventParty;
					continue;
				}
				break;
			}
		}
		return dictionary.ToMBList();
	}

	private float PartyLootShipScore(MapEventParty party)
	{
		ExplainedNumber stat = new ExplainedNumber(party.ContributionToBattle);
		stat.Add(party.Party.MemberRoster.TotalManCount);
		if (party.Party.LeaderHero != null)
		{
			Hero leaderHero = party.Party.LeaderHero;
			if (leaderHero.IsKingdomLeader)
			{
				stat.Add(50000f);
			}
			else if (leaderHero.IsClanLeader)
			{
				stat.Add(20000f);
			}
			if (leaderHero.Clan != null)
			{
				float value = MBMath.Map(leaderHero.Clan.Tier, Campaign.Current.Models.ClanTierModel.MinClanTier, Campaign.Current.Models.ClanTierModel.MaxClanTier, 5000f, 10000f);
				stat.Add(value);
			}
		}
		if (party.Party.MobileParty?.ActualClan != null)
		{
			float value2 = MBMath.Map(party.Party.MobileParty.ActualClan.Tier, Campaign.Current.Models.ClanTierModel.MinClanTier, Campaign.Current.Models.ClanTierModel.MaxClanTier, 5000f, 10000f);
			stat.Add(value2);
		}
		if (party.Party.IsMobile)
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.GildedPurse, party.Party.MobileParty, isPrimaryBonus: true, ref stat);
		}
		return stat.RoundedResultNumber;
	}

	private Ship GetShipToLootForWinnerParty(MapEventParty winnerParty, MBList<Ship> partyShipsToConsider, MBList<Ship> lootableShips)
	{
		float num = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(winnerParty.Party.MobileParty, partyShipsToConsider);
		Ship result = null;
		foreach (Ship lootableShip in lootableShips)
		{
			if (Campaign.Current.Models.ShipDistributionModel.CanPartyTakeShip(winnerParty.Party, lootableShip))
			{
				partyShipsToConsider.Add(lootableShip);
				float scoreForPartyShipComposition = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(winnerParty.Party.MobileParty, partyShipsToConsider);
				partyShipsToConsider.Remove(lootableShip);
				if (scoreForPartyShipComposition > num)
				{
					num = scoreForPartyShipComposition;
					result = lootableShip;
				}
			}
		}
		return result;
	}

	public override float GetMainPartyMemberScatterChance()
	{
		return base.BaseModel.GetMainPartyMemberScatterChance();
	}

	public override int GetPlayerGainedRelationAmount(MapEvent mapEvent, Hero hero)
	{
		return base.BaseModel.GetPlayerGainedRelationAmount(mapEvent, hero);
	}

	public override float GetShipSiegeEngineHitMoraleEffect(Ship ship, SiegeEngineType siegeEngineType)
	{
		return 0f;
	}

	public override float GetSunkenShipMoraleEffect(PartyBase shipOwner, Ship ship)
	{
		float result = -2f;
		switch (ship.ShipHull.Type)
		{
		case ShipHull.ShipType.Light:
			result = -1f;
			break;
		case ShipHull.ShipType.Medium:
			result = -2f;
			break;
		case ShipHull.ShipType.Heavy:
			result = -3f;
			break;
		default:
			Debug.FailedAssert("Ship type not handled", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCBattleRewardModel.cs", "GetSunkenShipMoraleEffect", 434);
			break;
		}
		return result;
	}

	public override MBReadOnlyList<MapEventParty> GetWinnerPartiesThatCanPlunderGoldFromShips(MBReadOnlyList<MapEventParty> winnerParties)
	{
		MBList<MapEventParty> mBList = new MBList<MapEventParty>();
		foreach (MapEventParty winnerParty in winnerParties)
		{
			if (winnerParty.Party != PartyBase.MainParty && winnerParty.ContributionToBattle > 0 && winnerParty.Party.IsMobile && !winnerParty.Party.MobileParty.IsBandit && !winnerParty.Party.MobileParty.IsCaravan)
			{
				mBList.Add(winnerParty);
			}
		}
		return mBList;
	}

	public override Figurehead GetFigureheadLoot(MBReadOnlyList<MapEventParty> defeatedParties, PartyBase defeatedSideLeaderParty)
	{
		Figurehead result = null;
		if (CanUnlockFigurehead())
		{
			IEnumerable<Hero> heroes = defeatedParties.WhereQ((MapEventParty x) => x.Party.LeaderHero != null).SelectQ((MapEventParty x) => x.Party.LeaderHero);
			float figureheadDropChanceForHeroes = GetFigureheadDropChanceForHeroes(heroes);
			if (MBRandom.RandomFloat <= figureheadDropChanceForHeroes)
			{
				List<Figurehead> unlockedFigureheadsByMainHero = Campaign.Current.UnlockedFigureheadsByMainHero;
				List<(Figurehead, float)> list = new List<(Figurehead, float)>();
				foreach (MapEventParty defeatedParty in defeatedParties)
				{
					foreach (Ship ship in defeatedParty.Ships)
					{
						if (ship.Figurehead != null && !unlockedFigureheadsByMainHero.Contains(ship.Figurehead))
						{
							if (defeatedParty.Party == defeatedSideLeaderParty && defeatedSideLeaderParty.MobileParty?.Army?.LeaderParty == defeatedSideLeaderParty.MobileParty)
							{
								list.Add((ship.Figurehead, 0.2f));
							}
							else
							{
								list.Add((ship.Figurehead, 0.1f));
							}
						}
					}
				}
				return MBRandom.ChooseWeighted(list);
			}
		}
		return result;
	}

	private bool CanUnlockFigurehead()
	{
		return Campaign.Current.GetCampaignBehavior<NavalDLCFigureheadCampaignBehavior>().LastFigureheadLootTime.ElapsedDaysUntilNow >= 8f;
	}

	private float GetFigureheadDropChanceForHeroes(IEnumerable<Hero> heroes)
	{
		float num = 0f;
		foreach (Hero hero in heroes)
		{
			IFaction mapFaction = hero.MapFaction;
			if (mapFaction != null && mapFaction.IsKingdomFaction && hero.MapFaction.Leader == hero)
			{
				num = 0.6f;
				break;
			}
			if (hero.Clan?.Leader == hero && num < 0.5f)
			{
				num = 0.5f;
			}
			else if (hero.Clan != null && num < 0.4f)
			{
				num = 0.4f;
			}
		}
		return num;
	}

	public override bool CanTroopBeTakenPrisoner(CharacterObject troop)
	{
		return base.BaseModel.CanTroopBeTakenPrisoner(troop);
	}
}
