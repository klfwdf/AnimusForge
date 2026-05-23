using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeShipOwnerAction
{
	public enum ShipOwnerChangeDetail
	{
		ApplyByTrade,
		ApplyByTransferring,
		ApplyByLooting,
		ApplyByMobilePartyCreation,
		ApplyByProduction
	}

	private static void ApplyInternal(PartyBase newOwner, Ship ship, ShipOwnerChangeDetail changeDetail)
	{
		PartyBase owner = ship.Owner;
		if (changeDetail == ShipOwnerChangeDetail.ApplyByTrade)
		{
			float shipTradeValue = Campaign.Current.Models.ShipCostModel.GetShipTradeValue(ship, owner, newOwner);
			if (owner.IsSettlement)
			{
				if (newOwner.MobileParty.IsCaravan || newOwner.MobileParty.IsVillager)
				{
					GiveGoldAction.ApplyForPartyToCharacter(newOwner, null, (int)shipTradeValue);
				}
				else if (newOwner.MobileParty.ActualClan?.Leader != null)
				{
					GiveGoldAction.ApplyBetweenCharacters(newOwner.MobileParty.ActualClan.Leader, null, (int)shipTradeValue);
				}
				else if (newOwner.MobileParty.LeaderHero != null)
				{
					GiveGoldAction.ApplyBetweenCharacters(newOwner.MobileParty.LeaderHero, null, (int)shipTradeValue);
				}
				else
				{
					Debug.FailedAssert("Unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\ChangeShipOwnerAction.cs", "ApplyInternal", 46);
					GiveGoldAction.ApplyForPartyToCharacter(newOwner, null, (int)shipTradeValue);
				}
				if (newOwner.Ships.Any() && !newOwner.MobileParty.Anchor.IsValid)
				{
					newOwner.MobileParty.Anchor.SetSettlement(ship.Owner.Settlement);
				}
			}
			else if (owner.MobileParty.IsCaravan || owner.MobileParty.IsVillager)
			{
				GiveGoldAction.ApplyForCharacterToParty(null, owner, (int)shipTradeValue);
			}
			else if (owner.MobileParty.ActualClan?.Leader != null)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, owner.MobileParty.ActualClan.Leader, (int)shipTradeValue);
			}
			else if (owner.LeaderHero != null)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, owner.LeaderHero, (int)shipTradeValue);
			}
			else
			{
				Debug.FailedAssert("Unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\ChangeShipOwnerAction.cs", "ApplyInternal", 71);
				GiveGoldAction.ApplyForCharacterToParty(null, owner, (int)shipTradeValue);
			}
		}
		ship.Owner = newOwner;
		owner?.MobileParty?.SetNavalVisualAsDirty();
		newOwner?.MobileParty?.SetNavalVisualAsDirty();
		CampaignEventDispatcher.Instance.OnShipOwnerChanged(ship, owner, changeDetail);
	}

	public static void ApplyByTransferring(PartyBase newOwner, Ship ship)
	{
		ApplyInternal(newOwner, ship, ShipOwnerChangeDetail.ApplyByTransferring);
	}

	public static void ApplyByTrade(PartyBase newOwner, Ship ship)
	{
		ApplyInternal(newOwner, ship, ShipOwnerChangeDetail.ApplyByTrade);
	}

	public static void ApplyByLooting(PartyBase newOwner, Ship ship)
	{
		ApplyInternal(newOwner, ship, ShipOwnerChangeDetail.ApplyByLooting);
	}

	public static void ApplyByProduction(PartyBase newOwner, Ship ship)
	{
		ApplyInternal(newOwner, ship, ShipOwnerChangeDetail.ApplyByProduction);
	}

	public static void ApplyByMobilePartyCreation(PartyBase newOwner, Ship ship)
	{
		ApplyInternal(newOwner, ship, ShipOwnerChangeDetail.ApplyByMobilePartyCreation);
	}
}
