using System.Collections.Generic;
using System.Linq;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class ShipRepairCampaignBehavior : CampaignBehaviorBase
{
	private List<IFaction> _factionsThatDoNotHavePort = new List<IFaction>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, AfterSessionLaunched);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, SettlementOwnerChanged);
		CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnAfterSettlementEnter);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnShipDestroyedEvent.AddNonSerializedListener(this, OnShipDestroyed);
	}

	private void OnShipDestroyed(PartyBase party, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
		Hero perkOwnerHero = null;
		if (detail != DestroyShipAction.ShipDestroyDetail.ApplyByDiscard || !party.IsMobile || !party.MobileParty.HasPerk(NavalPerks.Boatswain.ShipwrightsHand, out perkOwnerHero))
		{
			return;
		}
		float num = ship.HitPoints * 0.5f;
		foreach (Ship ship2 in party.Ships)
		{
			if (num <= 0f)
			{
				break;
			}
			float b = ship2.MaxHitPoints - ship2.HitPoints;
			float num2 = MathF.Min(num, b);
			ship2.HitPoints += num2;
			num -= num2;
		}
	}

	private void AfterSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		foreach (Clan item in Clan.All)
		{
			if (item.IsBanditFaction || _factionsThatDoNotHavePort.Contains(item.MapFaction))
			{
				continue;
			}
			bool flag = false;
			foreach (Settlement settlement in item.MapFaction.Settlements)
			{
				if (settlement.HasPort)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				_factionsThatDoNotHavePort.Add(item.MapFaction);
			}
		}
	}

	private void SettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (!settlement.HasPort)
		{
			return;
		}
		if (_factionsThatDoNotHavePort.Contains(newOwner.MapFaction))
		{
			_factionsThatDoNotHavePort.Remove(newOwner.MapFaction);
		}
		bool flag = false;
		foreach (Settlement settlement2 in oldOwner.MapFaction.Settlements)
		{
			if (settlement2.HasPort)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			_factionsThatDoNotHavePort.Add(oldOwner.MapFaction);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (oldKingdom != null)
		{
			bool flag = false;
			foreach (Settlement settlement in oldKingdom.Settlements)
			{
				if (settlement.HasPort)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				_factionsThatDoNotHavePort.Remove(oldKingdom);
			}
		}
		else if (newKingdom != null)
		{
			_factionsThatDoNotHavePort.Remove(clan);
		}
		if (newKingdom != null)
		{
			bool flag2 = false;
			foreach (Settlement settlement2 in newKingdom.Settlements)
			{
				if (settlement2.HasPort)
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				_factionsThatDoNotHavePort.Remove(newKingdom);
			}
			return;
		}
		bool flag3 = false;
		foreach (Settlement settlement3 in clan.Settlements)
		{
			if (settlement3.HasPort)
			{
				flag3 = true;
				break;
			}
		}
		if (!flag3)
		{
			_factionsThatDoNotHavePort.Add(clan);
		}
	}

	private void DailyTickParty(MobileParty mobileParty)
	{
		if ((mobileParty.IsBandit || _factionsThatDoNotHavePort.Contains(mobileParty.MapFaction)) && !mobileParty.IsMainParty && mobileParty.Ships.Any() && mobileParty.IsCurrentlyAtSea && !mobileParty.IsInRaftState && mobileParty.MapEvent == null && MBRandom.RandomFloat < 0.1f)
		{
			if (mobileParty.IsBandit)
			{
				RepairBanditPartyShips(mobileParty);
			}
			else
			{
				RepairPortlessFactionPartyShips(mobileParty);
			}
		}
	}

	private void OnAfterSettlementEnter(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty == null || mobileParty.IsMainParty || !settlement.HasPort || !settlement.IsFortification)
		{
			return;
		}
		if (mobileParty.IsCaravan)
		{
			RepairCaravanPartyShips(mobileParty);
			return;
		}
		Hero leaderHero = mobileParty.LeaderHero;
		if (leaderHero != null && leaderHero.IsMinorFactionHero)
		{
			RepairMinorFactionLordPartyShips(mobileParty);
		}
		else if (mobileParty.IsLordParty)
		{
			RepairLordPartyShips(mobileParty, settlement);
		}
	}

	private void RepairPortlessFactionPartyShips(MobileParty mobileParty)
	{
		foreach (Ship ship in mobileParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				RepairShipAction.ApplyForFree(ship);
			}
		}
	}

	private void RepairCaravanPartyShips(MobileParty mobileParty)
	{
		foreach (Ship ship in mobileParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints && (float)mobileParty.PartyTradeGold > Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, null))
			{
				RepairShipAction.ApplyForFree(ship);
			}
		}
	}

	private void RepairBanditPartyShips(MobileParty mobileParty)
	{
		foreach (Ship ship in mobileParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				RepairShipAction.ApplyForBanditShip(ship);
			}
		}
	}

	private void RepairMinorFactionLordPartyShips(MobileParty mobileParty)
	{
		foreach (Ship ship in mobileParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				RepairShipAction.ApplyForFree(ship);
			}
		}
	}

	private void RepairLordPartyShips(MobileParty mobileParty, Settlement settlement)
	{
		if (mobileParty.LeaderHero == null)
		{
			return;
		}
		foreach (Ship ship in mobileParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints && (float)mobileParty.PartyTradeGold > Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, mobileParty.Party))
			{
				RepairShipAction.Apply(ship, settlement);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
