using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class NavalShipDistributionCampaignBehavior : CampaignBehaviorBase
{
	public override void SyncData(IDataStore dataStore)
	{
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, OnPartyDisbanded);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
	{
		if (party.ActualClan != null && !party.IsCurrentlyAtSea && (party.ActualClan != Clan.PlayerClan || party.IsCaravan))
		{
			DistributePartyShipsAndRecoverGold(party);
		}
	}

	private void DistributePartyShipsAndRecoverGold(MobileParty mobileParty)
	{
		DistributeShips(mobileParty);
		RecoverGoldFromRemainingShipsAfterDistribution(mobileParty);
	}

	private void OnPartyDisbanded(MobileParty disbandParty, Settlement relatedSettlement)
	{
		if (disbandParty.ActualClan != null && !disbandParty.ActualClan.IsBanditFaction)
		{
			DistributePartyShipsAndRecoverGold(disbandParty);
		}
	}

	private void RecoverGoldFromRemainingShipsAfterDistribution(MobileParty party)
	{
		if (party.ActualClan != null && !party.ActualClan.IsBanditFaction && party.ActualClan.Leader != null && party.ActualClan.Leader.IsActive && party.Ships.Count > 0)
		{
			int amountToRecoverFromRemainingShipsAfterDistribution = ShipHelper.GetAmountToRecoverFromRemainingShipsAfterDistribution(party.Ships, party);
			if (party.ActualClan == Clan.PlayerClan && party.Owner != null)
			{
				MBTextManager.SetTextVariable("GOLD_AMOUNT", amountToRecoverFromRemainingShipsAfterDistribution);
				MBTextManager.SetTextVariable("LEADER_NAME", party.Owner.Name);
				MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				MBInformationManager.AddQuickInformation(new TextObject("{=YaSnA9j0}{LEADER_NAME}'s party has disbanded. You recovered {GOLD_AMOUNT}{GOLD_ICON} from its ships."));
			}
			GiveGoldAction.ApplyBetweenCharacters(null, party.ActualClan.Leader, amountToRecoverFromRemainingShipsAfterDistribution);
		}
	}

	private void DistributeShips(MobileParty party)
	{
		for (int num = party.Ships.Count - 1; num >= 0; num--)
		{
			Ship shipToSend = party.Ships[num];
			if (party.ActualClan.WarPartyComponents.AnyQ((WarPartyComponent x) => x.MobileParty != party && Campaign.Current.Models.ShipDistributionModel.CanSendShipToParty(shipToSend, x.MobileParty)))
			{
				bool doesPartyNeedShips;
				MobileParty clanPartyToGetAvailableShip = ShipHelper.GetClanPartyToGetAvailableShip(shipToSend, party.ActualClan, out doesPartyNeedShips);
				if (clanPartyToGetAvailableShip != null && clanPartyToGetAvailableShip != party && doesPartyNeedShips)
				{
					ChangeShipOwnerAction.ApplyByTransferring(clanPartyToGetAvailableShip.Party, shipToSend);
				}
			}
		}
	}
}
