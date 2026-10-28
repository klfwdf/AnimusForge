using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace NavalDLC.CampaignBehaviors;

public class ShipUpgradeCampaignBehavior : CampaignBehaviorBase
{
	private const float CaravanShipUpgradeChance = 0.4f;

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickPartyEvent);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
		CampaignEvents.CanHaveUnlockedUpgradePieceEvent.AddNonSerializedListener(this, CanHaveUnlockedUpgradePiece);
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty == null || !mobileParty.IsCaravan || !settlement.HasPort || !settlement.IsTown || !CanPartyUpgradeShips(mobileParty) || settlement.Town.GetShipyard().CurrentLevel <= 0)
		{
			return;
		}
		List<ShipUpgradePiece> availableShipUpgradePieces = settlement.Town.GetAvailableShipUpgradePieces();
		foreach (Ship ship in mobileParty.Ships)
		{
			if (!(MBRandom.RandomFloat < 0.4f))
			{
				continue;
			}
			KeyValuePair<string, ShipSlot> randomSlot = ship.ShipHull.AvailableSlots.GetRandomElementInefficiently();
			ShipUpgradePiece randomElementWithPredicate = availableShipUpgradePieces.GetRandomElementWithPredicate((ShipUpgradePiece x) => x.DoesPieceMatchSlot(randomSlot.Value));
			if (randomElementWithPredicate != null)
			{
				int shipUpgradePieceCost = Campaign.Current.Models.ShipCostModel.GetShipUpgradePieceCost(ship, randomElementWithPredicate, ship.Owner);
				if ((float)mobileParty.PartyTradeGold * 0.2f > (float)shipUpgradePieceCost)
				{
					UpgradeShip(ship, randomSlot.Key, randomElementWithPredicate);
					GiveGoldAction.ApplyForPartyToSettlement(mobileParty.Party, settlement, shipUpgradePieceCost);
				}
			}
		}
	}

	private float GetChanceToUpgradeShipForLord(Hero hero)
	{
		float num = (float)(hero.Clan.Tier + 1 - Campaign.Current.Models.ClanTierModel.MinClanTier) / (float)(1 + Campaign.Current.Models.ClanTierModel.MaxClanTier - Campaign.Current.Models.ClanTierModel.MinClanTier);
		float num2 = (hero.IsKingdomLeader ? 0.6f : (hero.IsClanLeader ? 0.4f : 0.2f));
		return num * num2;
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int index)
	{
		if (index % 2 != 0)
		{
			return;
		}
		foreach (MobileParty item in MobileParty.All)
		{
			DailyTickPartyEvent(item);
		}
	}

	private void CanHaveUnlockedUpgradePiece(Ship ship, ChangeShipOwnerAction.ShipOwnerChangeDetail detail, ref bool result)
	{
		if (detail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByStashing && detail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByUnstashing && detail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTemporarilyRemovingShipsFromPlayer && detail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByGivingBackShipsToPlayer)
		{
			if (ship.Owner == PartyBase.MainParty)
			{
				result = true;
			}
		}
		else
		{
			result = true;
		}
	}

	private void DailyTickPartyEvent(MobileParty party)
	{
		if (party.LeaderHero == null || party.IsCurrentlyAtSea || !CanPartyUpgradeShips(party))
		{
			return;
		}
		float chanceToUpgradeShipForLord = GetChanceToUpgradeShipForLord(party.LeaderHero);
		foreach (Ship ship in party.Ships)
		{
			if (MBRandom.RandomFloat < chanceToUpgradeShipForLord)
			{
				KeyValuePair<string, ShipSlot> randomElementInefficiently = ship.ShipHull.AvailableSlots.GetRandomElementInefficiently();
				ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(randomElementInefficiently.Key);
				int upgradePieceLevelToLook = ((pieceAtSlot == null) ? 1 : (pieceAtSlot.RequiredPortLevel + 1));
				ShipUpgradePiece randomElementWithPredicate = randomElementInefficiently.Value.MatchingPieces.GetRandomElementWithPredicate((ShipUpgradePiece x) => !x.NotMerchandise && x.RequiredPortLevel == upgradePieceLevelToLook);
				if (randomElementWithPredicate != null)
				{
					UpgradeShip(ship, randomElementInefficiently.Key, randomElementWithPredicate);
				}
			}
		}
	}

	private void UpgradeShip(Ship ship, string slotId, ShipUpgradePiece upgradePiece)
	{
		ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(slotId);
		_ = ship.ShipHull.AvailableSlots[slotId];
		if (pieceAtSlot == null || pieceAtSlot.RequiredPortLevel != 3)
		{
			ship.EquipUpgradePiece(slotId, upgradePiece);
		}
		ship.Owner?.MobileParty?.SetNavalVisualAsDirty();
	}

	private bool CanPartyUpgradeShips(MobileParty party)
	{
		if (party.ActualClan != Clan.PlayerClan && party.Ships.Count > 0 && !party.IsCurrentlyUsedByAQuest && party.IsActive && party.MapEvent == null && party.SiegeEvent == null && !party.IsInRaftState)
		{
			return !party.IsDisbanding;
		}
		return false;
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
