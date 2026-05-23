using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000422 RID: 1058
	public class PartiesBuyHorseCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042E8 RID: 17128 RVA: 0x00142F6C File Offset: 0x0014116C
		public override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameStarted));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameStarted));
		}

		// Token: 0x060042E9 RID: 17129 RVA: 0x00142FD5 File Offset: 0x001411D5
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060042EA RID: 17130 RVA: 0x00142FD7 File Offset: 0x001411D7
		private void OnGameStarted(CampaignGameStarter obj)
		{
			this.CalculateAverageHorsePrice();
		}

		// Token: 0x060042EB RID: 17131 RVA: 0x00142FDF File Offset: 0x001411DF
		private void OnDailyTick()
		{
			this.CalculateAverageHorsePrice();
		}

		// Token: 0x060042EC RID: 17132 RVA: 0x00142FE8 File Offset: 0x001411E8
		private void CalculateAverageHorsePrice()
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < Items.All.Count; i++)
			{
				ItemObject itemObject = Items.All[i];
				if (itemObject.ItemCategory == DefaultItemCategories.Horse)
				{
					num += itemObject.Value;
					num2++;
				}
			}
			if (num2 == 0)
			{
				this._averageHorsePrice = 0f;
				return;
			}
			this._averageHorsePrice = (float)(num / num2);
		}

		// Token: 0x060042ED RID: 17133 RVA: 0x00143050 File Offset: 0x00141250
		public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (mobileParty != null && mobileParty.MapFaction != null && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction) && mobileParty != MobileParty.MainParty && mobileParty.IsLordParty && mobileParty.LeaderHero != null && !mobileParty.IsDisbanding && settlement.IsTown)
			{
				int num = MathF.Min(100000, mobileParty.PartyTradeGold);
				int numberOfMounts = mobileParty.Party.NumberOfMounts;
				if (numberOfMounts > mobileParty.Party.NumberOfRegularMembers)
				{
					return;
				}
				Town town = settlement.Town;
				if (town.MarketData.GetItemCountOfCategory(DefaultItemCategories.Horse) == 0)
				{
					return;
				}
				float num2 = this._averageHorsePrice * (float)numberOfMounts / (float)num;
				if (num2 < 0.08f)
				{
					float randomFloat = MBRandom.RandomFloat;
					float randomFloat2 = MBRandom.RandomFloat;
					float randomFloat3 = MBRandom.RandomFloat;
					float num3 = (0.08f - num2) * (float)num * randomFloat * randomFloat2 * randomFloat3;
					if (num3 > (float)(mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * this._averageHorsePrice)
					{
						num3 = (float)(mobileParty.Party.NumberOfRegularMembers - numberOfMounts) * this._averageHorsePrice;
					}
					this.BuyHorses(mobileParty, town, num3);
				}
			}
			if (mobileParty != null && mobileParty != MobileParty.MainParty && mobileParty.IsLordParty && mobileParty.LeaderHero != null && !mobileParty.IsDisbanding && settlement.IsTown)
			{
				float num4 = 0f;
				for (int i = mobileParty.ItemRoster.Count - 1; i >= 0; i--)
				{
					ItemRosterElement subject = mobileParty.ItemRoster[i];
					if (subject.EquipmentElement.Item.IsMountable)
					{
						num4 += (float)(subject.Amount * subject.EquipmentElement.Item.Value);
					}
					else if (!subject.EquipmentElement.Item.IsFood)
					{
						SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject, subject.Amount, settlement);
					}
				}
				int num5 = MathF.Min(100000, mobileParty.PartyTradeGold);
				if (num4 > (float)num5 * 0.1f)
				{
					for (int j = 0; j < 10; j++)
					{
						ItemRosterElement subject2 = default(ItemRosterElement);
						int num6 = 0;
						for (int k = 0; k < mobileParty.ItemRoster.Count; k++)
						{
							ItemRosterElement itemRosterElement = mobileParty.ItemRoster[k];
							if (itemRosterElement.EquipmentElement.Item.IsMountable && itemRosterElement.EquipmentElement.Item.Value > num6)
							{
								num6 = itemRosterElement.EquipmentElement.Item.Value;
								subject2 = itemRosterElement;
							}
						}
						if (num6 <= 0)
						{
							break;
						}
						SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject2, 1, settlement);
						num4 -= (float)num6;
						if (num4 < (float)num5 * 0.1f)
						{
							break;
						}
					}
				}
			}
		}

		// Token: 0x060042EE RID: 17134 RVA: 0x00143344 File Offset: 0x00141544
		private void BuyHorses(MobileParty mobileParty, Town town, float budget)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = -1;
				int num2 = 100000;
				ItemRoster itemRoster = town.Owner.ItemRoster;
				for (int j = 0; j < itemRoster.Count; j++)
				{
					if (itemRoster.GetItemAtIndex(j).ItemCategory == DefaultItemCategories.Horse)
					{
						int itemPrice = town.GetItemPrice(itemRoster.GetElementCopyAtIndex(j).EquipmentElement, mobileParty, false);
						if (itemPrice < num2)
						{
							num2 = itemPrice;
							num = j;
						}
					}
				}
				if (num >= 0)
				{
					ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(num);
					int num3 = elementCopyAtIndex.Amount;
					if ((float)(num3 * num2) > budget)
					{
						num3 = MathF.Ceiling(budget / (float)num2);
					}
					int numberOfMounts = mobileParty.Party.NumberOfMounts;
					if (num3 > mobileParty.Party.NumberOfRegularMembers - numberOfMounts)
					{
						num3 = mobileParty.Party.NumberOfRegularMembers - numberOfMounts;
					}
					if (num3 > 0)
					{
						SellItemsAction.Apply(town.Owner, mobileParty.Party, elementCopyAtIndex, num3, town.Owner.Settlement);
					}
				}
			}
		}

		// Token: 0x04001312 RID: 4882
		private float _averageHorsePrice;
	}
}
