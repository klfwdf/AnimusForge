using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000421 RID: 1057
	public class PartiesBuyFoodCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042DF RID: 17119 RVA: 0x00142AED File Offset: 0x00140CED
		public override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickParty));
		}

		// Token: 0x060042E0 RID: 17120 RVA: 0x00142B1D File Offset: 0x00140D1D
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060042E1 RID: 17121 RVA: 0x00142B20 File Offset: 0x00140D20
		private void TryBuyingFood(MobileParty mobileParty, Settlement settlement)
		{
			if (Campaign.Current.GameStarted && mobileParty.LeaderHero != null && (settlement.IsTown || settlement.IsVillage) && Campaign.Current.Models.MobilePartyFoodConsumptionModel.DoesPartyConsumeFood(mobileParty) && (mobileParty.Army == null || mobileParty.AttachedTo == null || mobileParty.Army.LeaderParty == mobileParty) && (settlement.IsVillage || (mobileParty.MapFaction != null && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction))) && settlement.ItemRoster.TotalFood > 0)
			{
				PartyFoodBuyingModel partyFoodBuyingModel = Campaign.Current.Models.PartyFoodBuyingModel;
				float minimumDaysToLast = (settlement.IsVillage ? partyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage : partyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown);
				if (mobileParty.Army == null || (mobileParty.AttachedTo == null && mobileParty.Army.LeaderParty != mobileParty))
				{
					this.BuyFoodInternal(mobileParty, settlement, this.CalculateFoodCountToBuy(mobileParty, minimumDaysToLast));
					return;
				}
				this.BuyFoodForArmy(mobileParty, settlement, minimumDaysToLast);
			}
		}

		// Token: 0x060042E2 RID: 17122 RVA: 0x00142C30 File Offset: 0x00140E30
		private int CalculateFoodCountToBuy(MobileParty mobileParty, float minimumDaysToLast)
		{
			if (mobileParty.FoodChange.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				return 0;
			}
			float num = (float)mobileParty.TotalFoodAtInventory / -mobileParty.FoodChange;
			float num2 = minimumDaysToLast - num;
			if (num2 > 0f)
			{
				return (int)(-mobileParty.FoodChange * num2);
			}
			return 0;
		}

		// Token: 0x060042E3 RID: 17123 RVA: 0x00142C80 File Offset: 0x00140E80
		private void BuyFoodInternal(MobileParty mobileParty, Settlement settlement, int numberOfFoodItemsNeededToBuy)
		{
			if (!mobileParty.IsMainParty)
			{
				for (int i = 0; i < numberOfFoodItemsNeededToBuy; i++)
				{
					ItemRosterElement subject;
					float num;
					Campaign.Current.Models.PartyFoodBuyingModel.FindItemToBuy(mobileParty, settlement, out subject, out num);
					if (subject.EquipmentElement.Item == null)
					{
						break;
					}
					if (num <= (float)mobileParty.PartyTradeGold)
					{
						SellItemsAction.Apply(settlement.Party, mobileParty.Party, subject, 1, null);
					}
					if (subject.EquipmentElement.Item.HasHorseComponent && subject.EquipmentElement.Item.HorseComponent.IsLiveStock)
					{
						i += subject.EquipmentElement.Item.HorseComponent.MeatCount - 1;
					}
				}
			}
		}

		// Token: 0x060042E4 RID: 17124 RVA: 0x00142D44 File Offset: 0x00140F44
		private void BuyFoodForArmy(MobileParty mobileParty, Settlement settlement, float minimumDaysToLast)
		{
			float num = mobileParty.Army.LeaderParty.FoodChange;
			foreach (MobileParty mobileParty2 in mobileParty.Army.LeaderParty.AttachedParties)
			{
				num += mobileParty2.FoodChange;
			}
			List<ValueTuple<int, int>> list = new List<ValueTuple<int, int>>(mobileParty.Army.Parties.Count);
			float num2 = mobileParty.Army.LeaderParty.FoodChange / num;
			int num3 = this.CalculateFoodCountToBuy(mobileParty.Army.LeaderParty, minimumDaysToLast);
			list.Add(new ValueTuple<int, int>((int)((float)settlement.ItemRoster.TotalFood * num2), num3));
			int num4 = num3;
			foreach (MobileParty mobileParty3 in mobileParty.Army.LeaderParty.AttachedParties)
			{
				num2 = mobileParty3.FoodChange / num;
				num3 = this.CalculateFoodCountToBuy(mobileParty3, minimumDaysToLast);
				list.Add(new ValueTuple<int, int>((int)((float)settlement.ItemRoster.TotalFood * num2), num3));
				num4 += num3;
			}
			bool flag = settlement.ItemRoster.TotalFood < num4;
			int num5 = 0;
			foreach (ValueTuple<int, int> valueTuple in list)
			{
				int numberOfFoodItemsNeededToBuy = (flag ? valueTuple.Item1 : valueTuple.Item2);
				MobileParty mobileParty4 = ((num5 == 0) ? mobileParty.Army.LeaderParty : mobileParty.Army.LeaderParty.AttachedParties[num5 - 1]);
				if (!mobileParty4.IsMainParty)
				{
					this.BuyFoodInternal(mobileParty4, settlement, numberOfFoodItemsNeededToBuy);
				}
				num5++;
			}
		}

		// Token: 0x060042E5 RID: 17125 RVA: 0x00142F38 File Offset: 0x00141138
		public void HourlyTickParty(MobileParty mobileParty)
		{
			Settlement currentSettlementOfMobilePartyForAICalculation = MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(mobileParty);
			if (currentSettlementOfMobilePartyForAICalculation != null)
			{
				this.TryBuyingFood(mobileParty, currentSettlementOfMobilePartyForAICalculation);
			}
		}

		// Token: 0x060042E6 RID: 17126 RVA: 0x00142F57 File Offset: 0x00141157
		public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (mobileParty != null)
			{
				this.TryBuyingFood(mobileParty, settlement);
			}
		}
	}
}
