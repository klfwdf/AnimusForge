using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class TradeRumorsCampaignBehavior : CampaignBehaviorBase, ITradeRumorCampaignBehavior, ICampaignBehavior
{
	private List<TradeRumor> _tradeRumors = new List<TradeRumor>();

	private Dictionary<Settlement, CampaignTime> _enteredSettlements = new Dictionary<Settlement, CampaignTime>();

	public IEnumerable<TradeRumor> TradeRumors
	{
		get
		{
			foreach (TradeRumor tradeRumor in _tradeRumors)
			{
				if (!tradeRumor.IsExpired())
				{
					yield return tradeRumor;
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_enteredSettlements", ref _enteredSettlements);
		dataStore.SyncData("_tradeRumors", ref _tradeRumors);
	}

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnTradeRumorIsTakenEvent.AddNonSerializedListener(this, OnTradeRumorIsTaken);
	}

	public void OnTradeRumorIsTaken(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
	{
		AddTradeRumors(newRumors, sourceSettlement);
	}

	public void AddTradeRumors(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
	{
		bool flag = true;
		foreach (TradeRumor newRumor in newRumors)
		{
			foreach (TradeRumor tradeRumor in TradeRumors)
			{
				if (tradeRumor.Settlement == newRumor.Settlement && tradeRumor.ItemCategory == newRumor.ItemCategory)
				{
					flag = false;
				}
			}
			if (flag)
			{
				_tradeRumors.Add(newRumor);
			}
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
	}

	public void DailyTick()
	{
		AddDailyTradeRumors(1);
		DeleteExpiredRumors();
		DeleteExpiredEnteredSettlements();
	}

	private void DeleteExpiredEnteredSettlements()
	{
		List<Settlement> list = new List<Settlement>();
		foreach (KeyValuePair<Settlement, CampaignTime> enteredSettlement in _enteredSettlements)
		{
			if (CampaignTime.Now - enteredSettlement.Value >= CampaignTime.Days(1f))
			{
				list.Add(enteredSettlement.Key);
			}
		}
		foreach (Settlement item in list)
		{
			_enteredSettlements.Remove(item);
		}
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty == null || (!mobileParty.IsMainParty && (!mobileParty.IsCaravan || mobileParty.Party.Owner == null || mobileParty.Party.Owner.Clan != Clan.PlayerClan || !Hero.MainHero.GetPerkValue(DefaultPerks.Trade.TravelingRumors))) || !settlement.IsTown || settlement.Town?.MarketData == null || (_enteredSettlements.ContainsKey(settlement) && (!_enteredSettlements.ContainsKey(settlement) || !(CampaignTime.Now - _enteredSettlements[settlement] >= CampaignTime.Days(1f)))))
		{
			return;
		}
		List<TradeRumor> list = new List<TradeRumor>();
		foreach (TradeRumor item in _tradeRumors.Where((TradeRumor x) => x.Settlement == settlement))
		{
			list.Add(item);
		}
		foreach (TradeRumor item2 in list)
		{
			_tradeRumors.Remove(item2);
		}
		List<TradeRumor> list2 = new List<TradeRumor>();
		foreach (ItemObject allTradeGood in Items.AllTradeGoods)
		{
			list2.Add(new TradeRumor(settlement, allTradeGood, settlement.Town.GetItemPrice(allTradeGood), settlement.Town.GetItemPrice(allTradeGood, null, isSelling: true), 10));
		}
		AddTradeRumors(list2, settlement);
		if (!_enteredSettlements.ContainsKey(settlement))
		{
			_enteredSettlements.Add(settlement, CampaignTime.Now);
		}
		else
		{
			_enteredSettlements[settlement] = CampaignTime.Now;
		}
	}

	public void DeleteExpiredRumors()
	{
		List<TradeRumor> list = new List<TradeRumor>();
		foreach (TradeRumor item in _tradeRumors.Where((TradeRumor x) => x.IsExpired()))
		{
			list.Add(item);
		}
		foreach (TradeRumor item2 in list)
		{
			_tradeRumors.Remove(item2);
		}
	}

	public void AddDailyTradeRumors(int numberOfTradeRumors)
	{
		int num = 0;
		foreach (ItemObject item in Items.All)
		{
			if (item.Type == ItemObject.ItemTypeEnum.Goods || item.Type == ItemObject.ItemTypeEnum.Horse)
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
			foreach (Town allTown in Campaign.Current.AllTowns)
			{
				num2--;
				if (num2 >= 0)
				{
					continue;
				}
				foreach (ItemObject item2 in Items.All)
				{
					if (item2.Type == ItemObject.ItemTypeEnum.Goods || item2.Type == ItemObject.ItemTypeEnum.Horse)
					{
						num3--;
						if (num3 < 0)
						{
							list.Add(new TradeRumor(allTown.Settlement, item2, allTown.GetItemPrice(item2), allTown.GetItemPrice(item2, null, isSelling: true), 10 + MBRandom.RandomInt(10)));
							break;
						}
					}
				}
				break;
			}
			if (!Hero.MainHero.GetPerkValue(DefaultPerks.Trade.Tollgates))
			{
				continue;
			}
			foreach (Workshop ownedWorkshop in Hero.MainHero.OwnedWorkshops)
			{
				foreach (ItemObject allTradeGood in Items.AllTradeGoods)
				{
					list.Add(new TradeRumor(ownedWorkshop.Settlement, allTradeGood, ownedWorkshop.Settlement.Town.GetItemPrice(allTradeGood), ownedWorkshop.Settlement.Town.GetItemPrice(allTradeGood, null, isSelling: true), 10));
				}
			}
		}
		AddTradeRumors(list);
	}
}
