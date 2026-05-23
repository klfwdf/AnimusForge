using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C3 RID: 1219
	public static class SellItemsAction
	{
		// Token: 0x06004A13 RID: 18963 RVA: 0x00174EC0 File Offset: 0x001730C0
		private static void ApplyInternal(PartyBase sellerParty, PartyBase buyerParty, ItemRosterElement itemRosterElement, int number, Settlement currentSettlement)
		{
			if (currentSettlement == null)
			{
				if (sellerParty.Settlement != null)
				{
					currentSettlement = sellerParty.Settlement;
				}
				else
				{
					if (buyerParty.Settlement == null)
					{
						throw new MBInvalidParameterException("currentSettlement");
					}
					currentSettlement = buyerParty.Settlement;
				}
			}
			Town town = currentSettlement.Town;
			if (town == null)
			{
				if (!currentSettlement.IsVillage)
				{
					throw new MBException();
				}
				town = ((currentSettlement.Village.TradeBound != null) ? currentSettlement.Village.TradeBound.Town : currentSettlement.Village.Bound.Town);
			}
			MobileParty mobileParty = ((buyerParty != null) ? buyerParty.MobileParty : null);
			bool isSelling = false;
			if (mobileParty == null)
			{
				mobileParty = ((sellerParty != null) ? sellerParty.MobileParty : null);
				isSelling = true;
			}
			if (mobileParty == null)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < number; i++)
			{
				int itemPrice = town.GetItemPrice(itemRosterElement.EquipmentElement, mobileParty, isSelling);
				num += itemPrice;
				sellerParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -1);
				if (buyerParty != null)
				{
					buyerParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, 1);
				}
			}
			if (buyerParty != null && buyerParty.IsSettlement)
			{
				if (!mobileParty.IsCaravan)
				{
					GiveGoldAction.ApplyForSettlementToCharacter(buyerParty.Settlement, sellerParty.LeaderHero, num, false);
					return;
				}
				if (Campaign.Current.GameStarted)
				{
					GiveGoldAction.ApplyForSettlementToParty(buyerParty.Settlement, sellerParty, num, false);
					return;
				}
			}
			else if (sellerParty != null && sellerParty.IsSettlement)
			{
				int num2 = MBRandom.RoundRandomized((float)num * (sellerParty.Settlement.IsTown ? Campaign.Current.Models.SettlementTaxModel.GetTownTaxRatio(sellerParty.Settlement.Town) : Campaign.Current.Models.SettlementTaxModel.GetVillageTaxRatio(sellerParty.Settlement.Village)));
				if (mobileParty.IsCaravan)
				{
					if (Campaign.Current.GameStarted)
					{
						GiveGoldAction.ApplyForPartyToSettlement(buyerParty, sellerParty.Settlement, num, false);
					}
				}
				else if (buyerParty != null)
				{
					GiveGoldAction.ApplyForCharacterToSettlement(buyerParty.LeaderHero, sellerParty.Settlement, num, false);
				}
				else
				{
					sellerParty.Settlement.SettlementComponent.ChangeGold(num);
				}
				sellerParty.Settlement.SettlementComponent.ChangeGold(-num2);
				if (sellerParty.Settlement.Town != null)
				{
					float townCommissionChangeBasedOnSecurity = Campaign.Current.Models.SettlementTaxModel.GetTownCommissionChangeBasedOnSecurity(sellerParty.Settlement.Town, (float)num2);
					sellerParty.Settlement.Town.TradeTaxAccumulated += (int)townCommissionChangeBasedOnSecurity;
					return;
				}
			}
			else if (sellerParty.MobileParty.CurrentSettlement != null)
			{
				if (sellerParty.IsMobile && sellerParty.MobileParty.IsCaravan && sellerParty.Owner != null)
				{
					GiveGoldAction.ApplyForPartyToParty(buyerParty, sellerParty, num, false);
					return;
				}
				if (buyerParty != null)
				{
					GiveGoldAction.ApplyBetweenCharacters(buyerParty.LeaderHero, sellerParty.LeaderHero, num, false);
				}
			}
		}

		// Token: 0x06004A14 RID: 18964 RVA: 0x0017516D File Offset: 0x0017336D
		public static void Apply(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement subject, int number, Settlement currentSettlement = null)
		{
			SellItemsAction.ApplyInternal(receiverParty, payerParty, subject, number, currentSettlement);
		}
	}
}
