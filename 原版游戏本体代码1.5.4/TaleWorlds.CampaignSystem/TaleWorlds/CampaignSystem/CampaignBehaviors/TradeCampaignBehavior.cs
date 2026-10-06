using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class TradeCampaignBehavior : CampaignBehaviorBase
{
	public enum TradeGoodType
	{
		Grain,
		Wood,
		Meat,
		Wool,
		Cheese,
		Iron,
		Salt,
		Spice,
		Raw_Silk,
		Fish,
		Flax,
		Grape,
		Hides,
		Clay,
		Date_Fruit,
		Bread,
		Beer,
		Wine,
		Tools,
		Pottery,
		Cloth,
		Linen,
		Leather,
		Velvet,
		Saddle_Horse,
		Steppe_Horse,
		Hunter,
		Desert_Horse,
		Charger,
		War_Horse,
		Steppe_Charger,
		Desert_War_Horse,
		Unknown,
		NumberOfTradeItems
	}

	private Dictionary<ItemCategory, float> _numberOfTotalItemsAtGameWorld;

	public const float MaximumTaxRatioForVillages = 1f;

	public const float MaximumTaxRatioForTowns = 0.5f;

	public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		InitializeMarkets();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTickTown);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter campaignGameStarter, int i)
	{
		if (i == 2)
		{
			InitializeTrade();
		}
		if (i % 10 != 0)
		{
			return;
		}
		foreach (Town allTown in Campaign.Current.AllTowns)
		{
			UpdateMarketStores(allTown);
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		foreach (Town allTown in Town.AllTowns)
		{
			UpdateMarketStores(allTown);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_numberOfTotalItemsAtGameWorld", ref _numberOfTotalItemsAtGameWorld);
	}

	private void InitializeTrade()
	{
		_numberOfTotalItemsAtGameWorld = new Dictionary<ItemCategory, float>();
		Campaign.Current.Settlements.Where((Settlement settlement) => settlement.IsTown).ToList();
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero.CharacterObject.Occupation == Occupation.Lord && allAliveHero.Clan != Clan.PlayerClan)
			{
				int num = 0;
				num = ((allAliveHero.Clan?.Leader != allAliveHero) ? 10000 : (50000 + 10000 * allAliveHero.Clan.Tier + ((allAliveHero == allAliveHero.MapFaction.Leader) ? 50000 : 0)));
				GiveGoldAction.ApplyBetweenCharacters(null, allAliveHero, num);
			}
		}
	}

	public void DailyTickTown(Town town)
	{
		UpdateMarketStores(town);
	}

	private void UpdateMarketStores(Town town)
	{
		town.MarketData.UpdateStores();
	}

	private void InitializeMarkets()
	{
		foreach (Town allTown in Town.AllTowns)
		{
			foreach (ItemCategory item in ItemCategories.All)
			{
				if (item.IsValid)
				{
					allTown.MarketData.AddDemand(item, 3f);
					allTown.MarketData.AddSupply(item, 2f);
				}
			}
		}
	}
}
