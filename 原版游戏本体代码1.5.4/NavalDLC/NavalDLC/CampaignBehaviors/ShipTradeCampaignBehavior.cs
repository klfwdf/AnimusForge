using System;
using System.Linq;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class ShipTradeCampaignBehavior : CampaignBehaviorBase
{
	private const float ShipSellingChance = 0.1f;

	private const float ShipTransferringChance = 0.75f;

	private const float ClanGoldRatioToBuyShip = 0.2f;

	public static bool DebugNavalLordParties;

	public static bool DebugLordParties;

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
		CampaignEvents.OnShipRepairedEvent.AddNonSerializedListener(this, OnShipRepaired);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.9.103828")))
		{
			return;
		}
		foreach (MobileParty allLordParty in MobileParty.AllLordParties)
		{
			if (allLordParty != MobileParty.MainParty && allLordParty.MapEvent == null && allLordParty.SiegeEvent == null && allLordParty.ActualClan != null && allLordParty.LeaderHero != null && allLordParty.Ships.Count > 0 && allLordParty.IsActive && !allLordParty.IsCurrentlyUsedByAQuest && allLordParty.LeaderHero.IsActive)
			{
				int num = 0;
				Ship shipToSell;
				while (TryGetShipToSell(allLordParty, out shipToSell))
				{
					num += (int)Campaign.Current.Models.ShipCostModel.GetShipTradeValue(shipToSell, allLordParty.Party, null);
					DestroyShipAction.Apply(shipToSell);
				}
				if (num > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(null, allLordParty.ActualClan.Leader, num);
				}
			}
		}
		foreach (MobileParty allBanditParty in MobileParty.AllBanditParties)
		{
			if (allBanditParty.MapEvent == null && allBanditParty.ActualClan != null && allBanditParty.Ships.Count > 0 && allBanditParty.IsActive && !allBanditParty.IsCurrentlyUsedByAQuest && allBanditParty.Ships.Count > Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(allBanditParty))
			{
				for (int num2 = allBanditParty.Ships.Count - 1; num2 > Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(allBanditParty) - 1; num2--)
				{
					DestroyShipAction.Apply(allBanditParty.Ships[num2]);
				}
			}
		}
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int index)
	{
		foreach (Clan item in Clan.All)
		{
			DailyTickClan(item);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void DailyTickClan(Clan clan)
	{
		if (!clan.IsBanditFaction && !clan.IsEliminated && clan != Clan.PlayerClan)
		{
			ConsiderPurchasingShip(clan);
			ConsiderSwappingClanLeaderShips(clan);
			ConsiderSwappingShipsBetweenClanParties(clan);
			if (GetTotalNumberOfWarShipsInClan(clan) > NavalDLCManager.Instance.GameModels.ClanShipOwnershipModel.GetIdealShipNumberForClan(clan))
			{
				ConsiderSellingShips(clan);
			}
		}
	}

	private void ConsiderPurchasingShip(Clan clan)
	{
		if (!(MBRandom.RandomFloat < GetClanShipPurchaseChance(clan)))
		{
			return;
		}
		MobileParty partyToGiveShipTo = GetPartyToGiveShipTo(clan);
		if (partyToGiveShipTo != null)
		{
			Town townToBuyShipFrom = GetTownToBuyShipFrom(clan);
			if (townToBuyShipFrom != null)
			{
				TryPurchasingShipFromTown(partyToGiveShipTo, townToBuyShipFrom);
			}
		}
	}

	private float GetClanShipPurchaseChance(Clan clan)
	{
		return 0.5f;
	}

	private void TryPurchasingShipFromTown(MobileParty mobileParty, Town town)
	{
		Ship ship = null;
		MBList<Ship> mBList = mobileParty.Ships.ToMBList();
		float num = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList);
		foreach (Ship availableShip in town.AvailableShips)
		{
			if (Campaign.Current.Models.ShipDistributionModel.CanPartyTakeShip(mobileParty.Party, availableShip) && Campaign.Current.Models.ShipCostModel.GetShipTradeValue(availableShip, town.Settlement.Party, mobileParty.Party) < (float)mobileParty.ActualClan.Gold * 0.2f)
			{
				mBList.Add(availableShip);
				float scoreForPartyShipComposition = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList);
				mBList.Remove(availableShip);
				if (scoreForPartyShipComposition > num)
				{
					num = scoreForPartyShipComposition;
					ship = availableShip;
				}
			}
		}
		Ship ship2 = null;
		foreach (Ship availableShip2 in town.AvailableShips)
		{
			if (!Campaign.Current.Models.ShipDistributionModel.CanPartyTakeShip(mobileParty.Party, availableShip2))
			{
				continue;
			}
			for (int i = 0; i < mBList.Count; i++)
			{
				if (Campaign.Current.Models.ShipCostModel.GetShipTradeValue(availableShip2, town.Settlement.Party, mobileParty.Party) < (float)mobileParty.ActualClan.Gold * 0.2f)
				{
					Ship ship3 = mBList[i];
					mBList[i] = availableShip2;
					float scoreForPartyShipComposition2 = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList);
					if (scoreForPartyShipComposition2 > num)
					{
						num = scoreForPartyShipComposition2;
						ship = availableShip2;
						ship2 = ship3;
					}
					mBList[i] = ship3;
				}
			}
		}
		if (ship != null)
		{
			if (ship2 != null)
			{
				ChangeShipOwnerAction.ApplyByTrade(town.Settlement.Party, ship2);
			}
			ChangeShipOwnerAction.ApplyByTrade(mobileParty.Party, ship);
		}
	}

	private MobileParty GetPartyToGiveShipTo(Clan clan)
	{
		MobileParty result = null;
		float num = float.MaxValue;
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (CanPartyTradeShip(warPartyComponent.MobileParty))
			{
				float scoreForPartyShipComposition = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(warPartyComponent.MobileParty, warPartyComponent.MobileParty.Ships);
				if (scoreForPartyShipComposition < num)
				{
					num = scoreForPartyShipComposition;
					result = warPartyComponent.MobileParty;
				}
			}
		}
		return result;
	}

	private Town GetTownToBuyShipFrom(Clan clan)
	{
		Town town = null;
		if (clan.MapFaction.Fiefs.Count > 0)
		{
			town = clan.MapFaction.Fiefs.GetRandomElementWithPredicate((Town x) => CanClanBuyShipFromTown(clan, x));
		}
		if (town == null && MBRandom.RandomFloat < 0.2f)
		{
			town = Town.AllTowns.GetRandomElementWithPredicate((Town x) => CanClanBuyShipFromTown(clan, x) && !x.MapFaction.IsAtWarWith(clan));
		}
		return town;
	}

	private bool CanClanBuyShipFromTown(Clan clan, Town town)
	{
		if (!town.IsUnderSiege)
		{
			return town.AvailableShips.Count > 0;
		}
		return false;
	}

	private void ConsiderSwappingClanLeaderShips(Clan clan)
	{
		if (!(MBRandom.RandomFloat < 0.75f) || clan.WarPartyComponents.Count <= 2 || !CanPartyTradeShip(clan.Leader.PartyBelongedTo))
		{
			return;
		}
		MobileParty mobileParty = clan.WarPartyComponents.GetRandomElementWithPredicate((WarPartyComponent x) => x.MobileParty != clan.Leader.PartyBelongedTo).MobileParty;
		if (mobileParty == null || !CanPartyTradeShip(mobileParty))
		{
			return;
		}
		MBList<Ship> mBList = clan.Leader.PartyBelongedTo.Ships.ToMBList();
		float num = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(clan.Leader.PartyBelongedTo, mBList);
		Tuple<Ship, Ship> tuple = new Tuple<Ship, Ship>(null, null);
		for (int num2 = mBList.Count - 1; num2 >= 0; num2--)
		{
			Ship ship = mBList[num2];
			if (ship.IsTradeable && Campaign.Current.Models.ShipDistributionModel.CanPartyTakeShip(mobileParty.Party, ship))
			{
				MBList<Ship> mBList2 = mobileParty.Ships.ToMBList();
				if (mBList2.Any())
				{
					mBList.RemoveAt(num2);
					for (int num3 = 0; num3 < mBList2.Count; num3++)
					{
						Ship ship2 = mBList2[num3];
						if (ship2.IsTradeable && Campaign.Current.Models.ShipDistributionModel.CanPartyTakeShip(clan.Leader.PartyBelongedTo.Party, ship2))
						{
							mBList.Add(ship2);
							float scoreForPartyShipComposition = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(clan.Leader.PartyBelongedTo, mBList);
							if (scoreForPartyShipComposition > num)
							{
								num = scoreForPartyShipComposition;
								tuple = new Tuple<Ship, Ship>(ship, ship2);
							}
							mBList.Remove(ship2);
						}
					}
					mBList.Add(ship);
				}
			}
		}
		if (tuple.Item1 != null)
		{
			ChangeShipOwnerAction.ApplyByTransferring(tuple.Item2.Owner, tuple.Item1);
			ChangeShipOwnerAction.ApplyByTransferring(clan.Leader.PartyBelongedTo.Party, tuple.Item2);
		}
	}

	private void ConsiderSwappingShipsBetweenClanParties(Clan clan)
	{
		if (!(MBRandom.RandomFloat < 0.75f) || clan.WarPartyComponents.Count <= 2)
		{
			return;
		}
		MobileParty party1 = clan.WarPartyComponents.GetRandomElementWithPredicate((WarPartyComponent x) => CanPartyTradeShip(x.MobileParty))?.MobileParty;
		MobileParty mobileParty = clan.WarPartyComponents.GetRandomElementWithPredicate((WarPartyComponent x) => x.MobileParty != party1 && CanPartyTradeShip(x.MobileParty))?.MobileParty;
		if (party1 == null || mobileParty == null || party1.IsDisbanding || mobileParty.IsDisbanding)
		{
			return;
		}
		MBList<Ship> mBList = party1.Ships.ToMBList();
		MBList<Ship> mBList2 = mobileParty.Ships.ToMBList();
		float scoreForPartyShipComposition = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(party1, mBList);
		float scoreForPartyShipComposition2 = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList2);
		float num = scoreForPartyShipComposition + scoreForPartyShipComposition2;
		Tuple<Ship, Ship> tuple = new Tuple<Ship, Ship>(null, null);
		for (int num2 = mBList.Count - 1; num2 >= 0; num2--)
		{
			Ship ship = mBList[num2];
			if (ship.IsTradeable && Campaign.Current.Models.ShipDistributionModel.CanPartyTakeShip(mobileParty.Party, ship))
			{
				mBList.RemoveAt(num2);
				float scoreForPartyShipComposition3 = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(party1, mBList);
				mBList2.Add(ship);
				float scoreForPartyShipComposition4 = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList2);
				mBList2.Remove(ship);
				if (scoreForPartyShipComposition3 + scoreForPartyShipComposition4 > num && party1.Ships.Count > 1 && (clan.Leader.PartyBelongedTo != party1 || scoreForPartyShipComposition3 > scoreForPartyShipComposition) && (clan.Leader.PartyBelongedTo != mobileParty || scoreForPartyShipComposition4 > scoreForPartyShipComposition2))
				{
					num = scoreForPartyShipComposition3 + scoreForPartyShipComposition4;
					tuple = new Tuple<Ship, Ship>(ship, null);
				}
				for (int num3 = mBList2.Count - 1; num3 >= 0; num3--)
				{
					Ship ship2 = mBList2[num3];
					if (ship2.IsTradeable && Campaign.Current.Models.ShipDistributionModel.CanPartyTakeShip(party1.Party, ship2))
					{
						mBList.Add(ship2);
						mBList2.Add(ship);
						mBList2.RemoveAt(num3);
						scoreForPartyShipComposition3 = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(party1, mBList);
						scoreForPartyShipComposition4 = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList2);
						if (scoreForPartyShipComposition3 + scoreForPartyShipComposition4 > num && (clan.Leader.PartyBelongedTo != party1 || scoreForPartyShipComposition3 > scoreForPartyShipComposition) && (clan.Leader.PartyBelongedTo != mobileParty || scoreForPartyShipComposition4 > scoreForPartyShipComposition2))
						{
							num = scoreForPartyShipComposition3 + scoreForPartyShipComposition4;
							tuple = new Tuple<Ship, Ship>(ship, ship2);
						}
						mBList2.Remove(ship);
						mBList2.Add(ship2);
					}
				}
				mBList.Add(ship);
			}
		}
		if (tuple.Item1 != null)
		{
			if (tuple.Item2 != null)
			{
				ChangeShipOwnerAction.ApplyByTransferring(party1.Party, tuple.Item2);
			}
			ChangeShipOwnerAction.ApplyByTransferring(mobileParty.Party, tuple.Item1);
		}
	}

	private void ConsiderSellingShips(Clan clan)
	{
		if (!(MBRandom.RandomFloat < 0.1f) || !clan.WarPartyComponents.Any())
		{
			return;
		}
		MobileParty mobileParty = clan.WarPartyComponents.GetRandomElement().MobileParty;
		if (!mobileParty.IsDisbanding && CanPartyTradeShip(mobileParty) && TryGetShipToSell(mobileParty, out var shipToSell))
		{
			Town townToSellShip = GetTownToSellShip(clan);
			if (townToSellShip != null)
			{
				ChangeShipOwnerAction.ApplyByTrade(townToSellShip.Settlement.Party, shipToSell);
			}
		}
	}

	private bool TryGetShipToSell(MobileParty mobileParty, out Ship shipToSell)
	{
		shipToSell = null;
		MBList<Ship> mBList = mobileParty.Ships.ToMBList();
		float num = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList);
		for (int num2 = mBList.Count - 1; num2 >= 0; num2--)
		{
			Ship ship = mBList[num2];
			if (ship.IsTradeable)
			{
				mBList.RemoveAt(num2);
				float scoreForPartyShipComposition = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(mobileParty, mBList);
				if (scoreForPartyShipComposition > num)
				{
					num = scoreForPartyShipComposition;
					shipToSell = ship;
				}
				mBList.Add(ship);
			}
		}
		return shipToSell != null;
	}

	private Town GetTownToSellShip(Clan clan)
	{
		return clan.MapFaction.Fiefs.GetRandomElementWithPredicate((Town x) => x.IsTown && x.GetShipyard() != null && x.GetShipyard().CurrentLevel > 0);
	}

	private int GetTotalNumberOfWarShipsInClan(Clan clan)
	{
		int num = 0;
		for (int i = 0; i < clan.WarPartyComponents.Count; i++)
		{
			num += clan.WarPartyComponents[i].MobileParty.Ships.Count;
		}
		return num;
	}

	private bool CanPartyTradeShip(MobileParty party)
	{
		if (party != null && party.MapEvent == null && party.SiegeEvent == null && !party.IsCurrentlyAtSea && party.LeaderHero != null)
		{
			return party.IsActive;
		}
		return false;
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail details)
	{
		if (details == ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTrade)
		{
			Hero hero = null;
			if (oldOwner.IsSettlement)
			{
				hero = oldOwner.Settlement.Town.Governor;
			}
			else if (ship.Owner.IsSettlement)
			{
				hero = ship.Owner.Settlement.Town.Governor;
			}
			if (hero != null && (hero != Hero.MainHero || ship.Owner.LeaderHero != Hero.MainHero))
			{
				ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
				PerkHelper.AddPerkBonusForTown(NavalPerks.Boatswain.MerchantPrince, hero.CurrentSettlement.Town, isPrimaryBonus: false, ref bonuses);
				GiveGoldAction.ApplyBetweenCharacters(null, hero, bonuses.RoundedResultNumber);
			}
		}
	}

	private void OnShipRepaired(Ship ship, Settlement repairPort)
	{
		if (repairPort != null && repairPort.IsTown)
		{
			Hero governor = repairPort.Town.Governor;
			if (governor != null && (governor != Hero.MainHero || ship.Owner.LeaderHero != Hero.MainHero))
			{
				ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
				PerkHelper.AddPerkBonusForTown(NavalPerks.Boatswain.MasterShipwright, repairPort.Town, isPrimaryBonus: false, ref bonuses);
				GiveGoldAction.ApplyBetweenCharacters(null, governor, bonuses.RoundedResultNumber);
			}
		}
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty == null || !mobileParty.IsCaravan || !mobileParty.HasNavalNavigationCapability || !settlement.IsTown || settlement.Town.Governor == null)
		{
			return;
		}
		if (settlement.Town.Governor.GetPerkValue(NavalPerks.Boatswain.Salvage))
		{
			int num = TaleWorlds.Library.MathF.Round(NavalPerks.Boatswain.Salvage.SecondaryBonus);
			settlement.Town.TradeTaxAccumulated += num;
		}
		if (!settlement.Town.Governor.GetPerkValue(NavalPerks.Boatswain.ShipwrightsHand))
		{
			return;
		}
		CharacterObject basicTroop = settlement.MapFaction.BasicTroop;
		int characterWage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(basicTroop);
		MobileParty garrisonParty = settlement.Town.GarrisonParty;
		int num2 = garrisonParty?.TotalWage ?? 0;
		int num3 = settlement.GarrisonWagePaymentLimit - num2 - 5;
		if (num3 >= characterWage)
		{
			if (garrisonParty == null)
			{
				settlement.AddGarrisonParty();
				garrisonParty = settlement.Town.GarrisonParty;
			}
			int count = Math.Min(num3 / characterWage, TaleWorlds.Library.MathF.Round(NavalPerks.Boatswain.ShipwrightsHand.SecondaryBonus));
			garrisonParty.MemberRoster.AddToCounts(basicTroop, count);
		}
	}

	private void Tick(float dt)
	{
		if (!DebugNavalLordParties && !DebugLordParties)
		{
			return;
		}
		foreach (MobileParty allLordParty in MobileParty.AllLordParties)
		{
			if ((!DebugLordParties && (!allLordParty.IsCurrentlyAtSea || !DebugNavalLordParties)) || (allLordParty.Army != null && allLordParty.Army.LeaderParty != allLordParty) || allLordParty.CurrentSettlement != null || allLordParty.IsInRaftState || allLordParty == MobileParty.MainParty)
			{
				continue;
			}
			Vec3 vec = allLordParty.Position.AsVec3() + Vec3.Up * 3.75f;
			vec.x -= 1f;
			if (allLordParty.Army != null)
			{
				_ = $"Army Ship Count: {allLordParty.Ships.Count + allLordParty.AttachedParties.Sum((MobileParty x) => x.Ships.Count)}";
			}
			else
			{
				_ = $"Ship Count: {allLordParty.Ships.Count}";
			}
		}
		int num = 0;
		foreach (Kingdom item in Kingdom.All)
		{
			item.WarPartyComponents.Count((WarPartyComponent x) => x.MobileParty.IsCurrentlyAtSea && !x.MobileParty.IsInRaftState);
			item.WarPartyComponents.Count((WarPartyComponent x) => !x.MobileParty.IsCurrentlyAtSea);
			num++;
		}
	}
}
