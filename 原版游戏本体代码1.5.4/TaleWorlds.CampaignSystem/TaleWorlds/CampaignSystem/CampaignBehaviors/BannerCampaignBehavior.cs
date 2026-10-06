using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class BannerCampaignBehavior : CampaignBehaviorBase
{
	private const int BannerLevel1CooldownDays = 4;

	private const int BannerLevel2CooldownDays = 8;

	private const int BannerLevel3CooldownDays = 12;

	private const float BannerItemUpdateChance = 0.1f;

	private const float GiveBannerItemChance = 0.25f;

	private Dictionary<Hero, CampaignTime> _heroNextBannerLootTime = new Dictionary<Hero, CampaignTime>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, GiveBannersToHeroes);
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
		CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, OnCollectLootItems);
		CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, OnClanCreated);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_heroNextBannerLootTime", ref _heroNextBannerLootTime);
	}

	private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		GiveBannersToHeroes();
	}

	private void GiveBannersToHeroes()
	{
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (CanBannerBeGivenToHero(allAliveHero))
			{
				ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(allAliveHero);
				if (randomBannerItemForHero != null)
				{
					allAliveHero.BannerItem = new EquipmentElement(randomBannerItemForHero);
				}
			}
		}
	}

	private void DailyTickHero(Hero hero)
	{
		if (hero.Clan == Clan.PlayerClan)
		{
			return;
		}
		EquipmentElement bannerItem = hero.BannerItem;
		BannerItemModel bannerItemModel = Campaign.Current.Models.BannerItemModel;
		if (!bannerItem.IsInvalid() && bannerItemModel.CanBannerBeUpdated(bannerItem.Item) && MBRandom.RandomFloat < 0.1f)
		{
			int bannerLevel = ((BannerComponent)bannerItem.Item.ItemComponent).BannerLevel;
			int bannerItemLevelForHero = bannerItemModel.GetBannerItemLevelForHero(hero);
			if (bannerLevel != bannerItemLevelForHero)
			{
				ItemObject upgradeBannerForHero = GetUpgradeBannerForHero(hero, bannerItemLevelForHero);
				if (upgradeBannerForHero != null)
				{
					hero.BannerItem = new EquipmentElement(upgradeBannerForHero);
				}
			}
		}
		else if (bannerItem.IsInvalid() && CanBannerBeGivenToHero(hero) && MBRandom.RandomFloat < 0.25f && !hero.IsPrisoner)
		{
			ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(hero);
			if (randomBannerItemForHero != null)
			{
				hero.BannerItem = new EquipmentElement(randomBannerItemForHero);
			}
		}
	}

	private ItemObject GetUpgradeBannerForHero(Hero hero, int upgradeBannerLevel)
	{
		ItemObject item = hero.BannerItem.Item;
		foreach (ItemObject possibleRewardBannerItem in Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems())
		{
			BannerComponent bannerComponent = (BannerComponent)possibleRewardBannerItem.ItemComponent;
			if (possibleRewardBannerItem.Culture == item.Culture && bannerComponent.BannerLevel == upgradeBannerLevel && bannerComponent.BannerEffect == ((BannerComponent)item.ItemComponent).BannerEffect)
			{
				return possibleRewardBannerItem;
			}
		}
		return BannerHelper.GetRandomBannerItemForHero(hero);
	}

	private void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
	{
		if (winnerParty != PartyBase.MainParty)
		{
			return;
		}
		MapEvent mapEvent = MobileParty.MainParty.MapEvent;
		ItemObject itemObject = null;
		itemObject = Campaign.Current.Models.BattleRewardModel.GetBannerRewardForWinningMapEvent(mapEvent);
		if (itemObject != null)
		{
			gainedLoots.AddToCounts(itemObject, 1);
		}
		Hero hero = null;
		MBReadOnlyList<MapEventParty> mBReadOnlyList = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);
		if (mBReadOnlyList.Exists((MapEventParty x) => x.Party.IsMobile && x.Party.MobileParty.Army != null))
		{
			foreach (MapEventParty item in mBReadOnlyList)
			{
				if (item.Party.IsMobile && item.Party.MobileParty.Army != null && !item.Party.MobileParty.Army.ArmyOwner.BannerItem.IsInvalid() && CanBannerBeLootedFromHero(item.Party.MobileParty.Army.ArmyOwner))
				{
					hero = item.Party.MobileParty.Army.ArmyOwner;
					break;
				}
			}
		}
		if (hero == null)
		{
			hero = mBReadOnlyList.GetRandomElementWithPredicate((MapEventParty x) => x.Party.LeaderHero != null && !x.Party.LeaderHero.BannerItem.IsInvalid() && CanBannerBeLootedFromHero(x.Party.LeaderHero))?.Party.LeaderHero;
		}
		if (hero != null)
		{
			float bannerLootChanceFromDefeatedHero = Campaign.Current.Models.BattleRewardModel.GetBannerLootChanceFromDefeatedHero(hero);
			if (MBRandom.RandomFloat <= bannerLootChanceFromDefeatedHero)
			{
				LogBannerLootForHero(hero, ((BannerComponent)hero.BannerItem.Item.ItemComponent).BannerLevel);
				gainedLoots.AddToCounts(hero.BannerItem.Item, 1);
				hero.BannerItem = new EquipmentElement(null);
			}
		}
	}

	private void OnHeroComesOfAge(Hero hero)
	{
		if (CanBannerBeGivenToHero(hero))
		{
			ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(hero);
			if (randomBannerItemForHero != null)
			{
				hero.BannerItem = new EquipmentElement(randomBannerItemForHero);
			}
		}
	}

	private void OnHeroCreated(Hero hero, bool isBornNaturally = false)
	{
		if (CanBannerBeGivenToHero(hero))
		{
			ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(hero);
			if (randomBannerItemForHero != null)
			{
				hero.BannerItem = new EquipmentElement(randomBannerItemForHero);
			}
		}
	}

	private void OnClanCreated(Clan clan, bool isCompanion)
	{
		if (!isCompanion)
		{
			return;
		}
		Hero leader = clan.Leader;
		if (leader.BannerItem.IsInvalid())
		{
			ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(leader);
			if (randomBannerItemForHero != null)
			{
				leader.BannerItem = new EquipmentElement(randomBannerItemForHero);
			}
		}
	}

	private bool CanBannerBeLootedFromHero(Hero hero)
	{
		if (_heroNextBannerLootTime.ContainsKey(hero))
		{
			return _heroNextBannerLootTime[hero].IsPast;
		}
		return true;
	}

	private int GetCooldownDays(int bannerLevel)
	{
		if (bannerLevel == 1)
		{
			return 4;
		}
		if (bannerLevel == 1)
		{
			return 8;
		}
		return 12;
	}

	private void LogBannerLootForHero(Hero hero, int bannerLevel)
	{
		CampaignTime value = CampaignTime.DaysFromNow(GetCooldownDays(bannerLevel));
		if (!_heroNextBannerLootTime.ContainsKey(hero))
		{
			_heroNextBannerLootTime.Add(hero, value);
		}
		else
		{
			_heroNextBannerLootTime[hero] = value;
		}
	}

	private bool CanBannerBeGivenToHero(Hero hero)
	{
		int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
		if (hero.Occupation == Occupation.Lord && hero.Age >= (float)heroComesOfAge && hero.BannerItem.IsInvalid())
		{
			return hero.Clan != Clan.PlayerClan;
		}
		return false;
	}
}
