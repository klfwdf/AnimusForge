using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000447 RID: 1095
	public class TradeRumorsCampaignBehavior : CampaignBehaviorBase, ITradeRumorCampaignBehavior, ICampaignBehavior
	{
		// Token: 0x17000E2C RID: 3628
		// (get) Token: 0x060045CD RID: 17869 RVA: 0x0015B17C File Offset: 0x0015937C
		public IEnumerable<TradeRumor> TradeRumors
		{
			get
			{
				foreach (TradeRumor tradeRumor in this._tradeRumors)
				{
					if (!tradeRumor.IsExpired())
					{
						yield return tradeRumor;
					}
				}
				List<TradeRumor>.Enumerator enumerator = default(List<TradeRumor>.Enumerator);
				yield break;
				yield break;
			}
		}

		// Token: 0x060045CE RID: 17870 RVA: 0x0015B18C File Offset: 0x0015938C
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Settlement, CampaignTime>>("_enteredSettlements", ref this._enteredSettlements);
			dataStore.SyncData<List<TradeRumor>>("_tradeRumors", ref this._tradeRumors);
		}

		// Token: 0x060045CF RID: 17871 RVA: 0x0015B1B4 File Offset: 0x001593B4
		public override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnTradeRumorIsTakenEvent.AddNonSerializedListener(this, new Action<List<TradeRumor>, Settlement>(this.OnTradeRumorIsTaken));
		}

		// Token: 0x060045D0 RID: 17872 RVA: 0x0015B21D File Offset: 0x0015941D
		public void OnTradeRumorIsTaken(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
		{
			this.AddTradeRumors(newRumors, sourceSettlement);
		}

		// Token: 0x060045D1 RID: 17873 RVA: 0x0015B228 File Offset: 0x00159428
		public void AddTradeRumors(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
		{
			bool flag = true;
			foreach (TradeRumor tradeRumor in newRumors)
			{
				foreach (TradeRumor tradeRumor2 in this.TradeRumors)
				{
					if (tradeRumor2.Settlement == tradeRumor.Settlement && tradeRumor2.ItemCategory == tradeRumor.ItemCategory)
					{
						flag = false;
					}
				}
				if (flag)
				{
					this._tradeRumors.Add(tradeRumor);
				}
			}
		}

		// Token: 0x060045D2 RID: 17874 RVA: 0x0015B2D8 File Offset: 0x001594D8
		private void OnNewGameCreated(CampaignGameStarter starter)
		{
		}

		// Token: 0x060045D3 RID: 17875 RVA: 0x0015B2DA File Offset: 0x001594DA
		public void DailyTick()
		{
			this.AddDailyTradeRumors(1);
			this.DeleteExpiredRumors();
			this.DeleteExpiredEnteredSettlements();
		}

		// Token: 0x060045D4 RID: 17876 RVA: 0x0015B2F0 File Offset: 0x001594F0
		private void DeleteExpiredEnteredSettlements()
		{
			List<Settlement> list = new List<Settlement>();
			foreach (KeyValuePair<Settlement, CampaignTime> keyValuePair in this._enteredSettlements)
			{
				if (CampaignTime.Now - keyValuePair.Value >= CampaignTime.Days(1f))
				{
					list.Add(keyValuePair.Key);
				}
			}
			foreach (Settlement key in list)
			{
				this._enteredSettlements.Remove(key);
			}
		}

		// Token: 0x060045D5 RID: 17877 RVA: 0x0015B3B8 File Offset: 0x001595B8
		public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (mobileParty == null || (!mobileParty.IsMainParty && (!mobileParty.IsCaravan || mobileParty.Party.Owner == null || mobileParty.Party.Owner.Clan != Clan.PlayerClan || !Hero.MainHero.GetPerkValue(DefaultPerks.Trade.TravelingRumors))) || !settlement.IsTown)
			{
				return;
			}
			Town town = settlement.Town;
			if (((town != null) ? town.MarketData : null) == null)
			{
				return;
			}
			if (!this._enteredSettlements.ContainsKey(settlement) || (this._enteredSettlements.ContainsKey(settlement) && CampaignTime.Now - this._enteredSettlements[settlement] >= CampaignTime.Days(1f)))
			{
				List<TradeRumor> list = new List<TradeRumor>();
				IEnumerable<TradeRumor> tradeRumors = this._tradeRumors;
				Func<TradeRumor, bool> <>9__0;
				Func<TradeRumor, bool> predicate;
				if ((predicate = <>9__0) == null)
				{
					predicate = (<>9__0 = (TradeRumor x) => x.Settlement == settlement);
				}
				foreach (TradeRumor item in tradeRumors.Where(predicate))
				{
					list.Add(item);
				}
				foreach (TradeRumor item2 in list)
				{
					this._tradeRumors.Remove(item2);
				}
				List<TradeRumor> list2 = new List<TradeRumor>();
				foreach (ItemObject itemObject in Items.AllTradeGoods)
				{
					list2.Add(new TradeRumor(settlement, itemObject, settlement.Town.GetItemPrice(itemObject, null, false), settlement.Town.GetItemPrice(itemObject, null, true), 10));
				}
				this.AddTradeRumors(list2, settlement);
				if (!this._enteredSettlements.ContainsKey(settlement))
				{
					this._enteredSettlements.Add(settlement, CampaignTime.Now);
					return;
				}
				this._enteredSettlements[settlement] = CampaignTime.Now;
			}
		}

		// Token: 0x060045D6 RID: 17878 RVA: 0x0015B624 File Offset: 0x00159824
		public void DeleteExpiredRumors()
		{
			List<TradeRumor> list = new List<TradeRumor>();
			foreach (TradeRumor item in from x in this._tradeRumors
				where x.IsExpired()
				select x)
			{
				list.Add(item);
			}
			foreach (TradeRumor item2 in list)
			{
				this._tradeRumors.Remove(item2);
			}
		}

		// Token: 0x060045D7 RID: 17879 RVA: 0x0015B6E0 File Offset: 0x001598E0
		public void AddDailyTradeRumors(int numberOfTradeRumors)
		{
			int num = 0;
			foreach (ItemObject itemObject in Items.All)
			{
				if (itemObject.Type == ItemObject.ItemTypeEnum.Goods || itemObject.Type == ItemObject.ItemTypeEnum.Horse)
				{
					num++;
				}
			}
			int count = Campaign.Current.AllTowns.Count;
			List<TradeRumor> list = new List<TradeRumor>();
			for (int i = 0; i < numberOfTradeRumors; i++)
			{
				int num2 = MBRandom.RandomInt(count);
				int num3 = MBRandom.RandomInt(num);
				foreach (Town town in Campaign.Current.AllTowns)
				{
					num2--;
					if (num2 < 0)
					{
						using (List<ItemObject>.Enumerator enumerator = Items.All.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								ItemObject itemObject2 = enumerator.Current;
								if (itemObject2.Type == ItemObject.ItemTypeEnum.Goods || itemObject2.Type == ItemObject.ItemTypeEnum.Horse)
								{
									num3--;
									if (num3 < 0)
									{
										list.Add(new TradeRumor(town.Settlement, itemObject2, town.GetItemPrice(itemObject2, null, false), town.GetItemPrice(itemObject2, null, true), 10 + MBRandom.RandomInt(10)));
										break;
									}
								}
							}
							break;
						}
					}
				}
				if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.Tollgates))
				{
					foreach (Workshop workshop in Hero.MainHero.OwnedWorkshops)
					{
						foreach (ItemObject itemObject3 in Items.AllTradeGoods)
						{
							list.Add(new TradeRumor(workshop.Settlement, itemObject3, workshop.Settlement.Town.GetItemPrice(itemObject3, null, false), workshop.Settlement.Town.GetItemPrice(itemObject3, null, true), 10));
						}
					}
				}
			}
			this.AddTradeRumors(list, null);
		}

		// Token: 0x04001383 RID: 4995
		private List<TradeRumor> _tradeRumors = new List<TradeRumor>();

		// Token: 0x04001384 RID: 4996
		private Dictionary<Settlement, CampaignTime> _enteredSettlements = new Dictionary<Settlement, CampaignTime>();
	}
}
