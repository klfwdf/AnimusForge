using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004A1 RID: 1185
	public static class ChangeShipOwnerAction
	{
		// Token: 0x0600497D RID: 18813 RVA: 0x00171FE4 File Offset: 0x001701E4
		private static void ApplyInternal(PartyBase newOwner, Ship ship, ChangeShipOwnerAction.ShipOwnerChangeDetail changeDetail)
		{
			PartyBase owner = ship.Owner;
			if (changeDetail == ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTrade)
			{
				float shipTradeValue = Campaign.Current.Models.ShipCostModel.GetShipTradeValue(ship, owner, newOwner);
				if (owner.IsSettlement)
				{
					if (newOwner.MobileParty.IsCaravan || newOwner.MobileParty.IsVillager)
					{
						GiveGoldAction.ApplyForPartyToCharacter(newOwner, null, (int)shipTradeValue, false);
					}
					else
					{
						Clan actualClan = newOwner.MobileParty.ActualClan;
						if (((actualClan != null) ? actualClan.Leader : null) != null)
						{
							GiveGoldAction.ApplyBetweenCharacters(newOwner.MobileParty.ActualClan.Leader, null, (int)shipTradeValue, false);
						}
						else if (newOwner.MobileParty.LeaderHero != null)
						{
							GiveGoldAction.ApplyBetweenCharacters(newOwner.MobileParty.LeaderHero, null, (int)shipTradeValue, false);
						}
						else
						{
							Debug.FailedAssert("Unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\ChangeShipOwnerAction.cs", "ApplyInternal", 46);
							GiveGoldAction.ApplyForPartyToCharacter(newOwner, null, (int)shipTradeValue, false);
						}
					}
					if (newOwner.Ships.Any<Ship>() && !newOwner.MobileParty.Anchor.IsValid)
					{
						newOwner.MobileParty.Anchor.SetSettlement(ship.Owner.Settlement);
					}
				}
				else if (owner.MobileParty.IsCaravan || owner.MobileParty.IsVillager)
				{
					GiveGoldAction.ApplyForCharacterToParty(null, owner, (int)shipTradeValue, false);
				}
				else
				{
					Clan actualClan2 = owner.MobileParty.ActualClan;
					if (((actualClan2 != null) ? actualClan2.Leader : null) != null)
					{
						GiveGoldAction.ApplyBetweenCharacters(null, owner.MobileParty.ActualClan.Leader, (int)shipTradeValue, false);
					}
					else if (owner.LeaderHero != null)
					{
						GiveGoldAction.ApplyBetweenCharacters(null, owner.LeaderHero, (int)shipTradeValue, false);
					}
					else
					{
						Debug.FailedAssert("Unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\ChangeShipOwnerAction.cs", "ApplyInternal", 71);
						GiveGoldAction.ApplyForCharacterToParty(null, owner, (int)shipTradeValue, false);
					}
				}
			}
			ship.Owner = newOwner;
			if (owner != null)
			{
				MobileParty mobileParty = owner.MobileParty;
				if (mobileParty != null)
				{
					mobileParty.SetNavalVisualAsDirty();
				}
			}
			if (newOwner != null)
			{
				MobileParty mobileParty2 = newOwner.MobileParty;
				if (mobileParty2 != null)
				{
					mobileParty2.SetNavalVisualAsDirty();
				}
			}
			CampaignEventDispatcher.Instance.OnShipOwnerChanged(ship, owner, changeDetail);
		}

		// Token: 0x0600497E RID: 18814 RVA: 0x001721D2 File Offset: 0x001703D2
		public static void ApplyByTransferring(PartyBase newOwner, Ship ship)
		{
			ChangeShipOwnerAction.ApplyInternal(newOwner, ship, ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTransferring);
		}

		// Token: 0x0600497F RID: 18815 RVA: 0x001721DC File Offset: 0x001703DC
		public static void ApplyByTrade(PartyBase newOwner, Ship ship)
		{
			ChangeShipOwnerAction.ApplyInternal(newOwner, ship, ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTrade);
		}

		// Token: 0x06004980 RID: 18816 RVA: 0x001721E6 File Offset: 0x001703E6
		public static void ApplyByLooting(PartyBase newOwner, Ship ship)
		{
			ChangeShipOwnerAction.ApplyInternal(newOwner, ship, ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByLooting);
		}

		// Token: 0x06004981 RID: 18817 RVA: 0x001721F0 File Offset: 0x001703F0
		public static void ApplyByProduction(PartyBase newOwner, Ship ship)
		{
			ChangeShipOwnerAction.ApplyInternal(newOwner, ship, ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByProduction);
		}

		// Token: 0x06004982 RID: 18818 RVA: 0x001721FA File Offset: 0x001703FA
		public static void ApplyByMobilePartyCreation(PartyBase newOwner, Ship ship)
		{
			ChangeShipOwnerAction.ApplyInternal(newOwner, ship, ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByMobilePartyCreation);
		}

		// Token: 0x02000884 RID: 2180
		public enum ShipOwnerChangeDetail
		{
			// Token: 0x0400240C RID: 9228
			ApplyByTrade,
			// Token: 0x0400240D RID: 9229
			ApplyByTransferring,
			// Token: 0x0400240E RID: 9230
			ApplyByLooting,
			// Token: 0x0400240F RID: 9231
			ApplyByMobilePartyCreation,
			// Token: 0x04002410 RID: 9232
			ApplyByProduction
		}
	}
}
