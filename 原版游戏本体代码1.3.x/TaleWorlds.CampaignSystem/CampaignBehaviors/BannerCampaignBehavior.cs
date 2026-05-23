using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D1 RID: 977
	public class BannerCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003A39 RID: 14905 RVA: 0x000F067C File Offset: 0x000EE87C
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.GiveBannersToHeroes));
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.DailyTickHero));
			CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, new Action<PartyBase, ItemRoster>(this.OnCollectLootItems));
			CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeroComesOfAge));
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroCreated));
			CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, new Action<Clan, bool>(this.OnClanCreated));
		}

		// Token: 0x06003A3A RID: 14906 RVA: 0x000F072A File Offset: 0x000EE92A
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Hero, CampaignTime>>("_heroNextBannerLootTime", ref this._heroNextBannerLootTime);
		}

		// Token: 0x06003A3B RID: 14907 RVA: 0x000F073E File Offset: 0x000EE93E
		private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
		{
			this.GiveBannersToHeroes();
		}

		// Token: 0x06003A3C RID: 14908 RVA: 0x000F0748 File Offset: 0x000EE948
		private void GiveBannersToHeroes()
		{
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (this.CanBannerBeGivenToHero(hero))
				{
					ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(hero);
					if (randomBannerItemForHero != null)
					{
						hero.BannerItem = new EquipmentElement(randomBannerItemForHero, null, null, false);
					}
				}
			}
		}

		// Token: 0x06003A3D RID: 14909 RVA: 0x000F07B8 File Offset: 0x000EE9B8
		private void DailyTickHero(Hero hero)
		{
			if (hero.Clan != Clan.PlayerClan)
			{
				EquipmentElement bannerItem = hero.BannerItem;
				BannerItemModel bannerItemModel = Campaign.Current.Models.BannerItemModel;
				if (!bannerItem.IsInvalid() && bannerItemModel.CanBannerBeUpdated(bannerItem.Item) && MBRandom.RandomFloat < 0.1f)
				{
					int bannerLevel = ((BannerComponent)bannerItem.Item.ItemComponent).BannerLevel;
					int bannerItemLevelForHero = bannerItemModel.GetBannerItemLevelForHero(hero);
					if (bannerLevel != bannerItemLevelForHero)
					{
						ItemObject upgradeBannerForHero = this.GetUpgradeBannerForHero(hero, bannerItemLevelForHero);
						if (upgradeBannerForHero != null)
						{
							hero.BannerItem = new EquipmentElement(upgradeBannerForHero, null, null, false);
							return;
						}
					}
				}
				else if (bannerItem.IsInvalid() && this.CanBannerBeGivenToHero(hero) && MBRandom.RandomFloat < 0.25f && !hero.IsPrisoner)
				{
					ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(hero);
					if (randomBannerItemForHero != null)
					{
						hero.BannerItem = new EquipmentElement(randomBannerItemForHero, null, null, false);
					}
				}
			}
		}

		// Token: 0x06003A3E RID: 14910 RVA: 0x000F0890 File Offset: 0x000EEA90
		private ItemObject GetUpgradeBannerForHero(Hero hero, int upgradeBannerLevel)
		{
			ItemObject item = hero.BannerItem.Item;
			foreach (ItemObject itemObject in Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems())
			{
				BannerComponent bannerComponent = (BannerComponent)itemObject.ItemComponent;
				if (itemObject.Culture == item.Culture && bannerComponent.BannerLevel == upgradeBannerLevel && bannerComponent.BannerEffect == ((BannerComponent)item.ItemComponent).BannerEffect)
				{
					return itemObject;
				}
			}
			return BannerHelper.GetRandomBannerItemForHero(hero);
		}

		// Token: 0x06003A3F RID: 14911 RVA: 0x000F0940 File Offset: 0x000EEB40
		private void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
		{
			if (winnerParty == PartyBase.MainParty)
			{
				MapEvent mapEvent = MobileParty.MainParty.MapEvent;
				ItemObject bannerRewardForWinningMapEvent = Campaign.Current.Models.BattleRewardModel.GetBannerRewardForWinningMapEvent(mapEvent);
				if (bannerRewardForWinningMapEvent != null)
				{
					gainedLoots.AddToCounts(bannerRewardForWinningMapEvent, 1);
				}
				Hero hero = null;
				MBReadOnlyList<MapEventParty> mbreadOnlyList = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);
				if (mbreadOnlyList.Exists((MapEventParty x) => x.Party.IsMobile && x.Party.MobileParty.Army != null))
				{
					foreach (MapEventParty mapEventParty in mbreadOnlyList)
					{
						if (mapEventParty.Party.IsMobile && mapEventParty.Party.MobileParty.Army != null && !mapEventParty.Party.MobileParty.Army.ArmyOwner.BannerItem.IsInvalid() && this.CanBannerBeLootedFromHero(mapEventParty.Party.MobileParty.Army.ArmyOwner))
						{
							hero = mapEventParty.Party.MobileParty.Army.ArmyOwner;
							break;
						}
					}
				}
				if (hero == null)
				{
					MapEventParty randomElementWithPredicate = mbreadOnlyList.GetRandomElementWithPredicate((MapEventParty x) => x.Party.LeaderHero != null && !x.Party.LeaderHero.BannerItem.IsInvalid() && this.CanBannerBeLootedFromHero(x.Party.LeaderHero));
					hero = ((randomElementWithPredicate != null) ? randomElementWithPredicate.Party.LeaderHero : null);
				}
				if (hero != null)
				{
					float bannerLootChanceFromDefeatedHero = Campaign.Current.Models.BattleRewardModel.GetBannerLootChanceFromDefeatedHero(hero);
					if (MBRandom.RandomFloat <= bannerLootChanceFromDefeatedHero)
					{
						this.LogBannerLootForHero(hero, ((BannerComponent)hero.BannerItem.Item.ItemComponent).BannerLevel);
						gainedLoots.AddToCounts(hero.BannerItem.Item, 1);
						hero.BannerItem = new EquipmentElement(null, null, null, false);
					}
				}
			}
		}

		// Token: 0x06003A40 RID: 14912 RVA: 0x000F0B18 File Offset: 0x000EED18
		private void OnHeroComesOfAge(Hero hero)
		{
			if (this.CanBannerBeGivenToHero(hero))
			{
				ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(hero);
				if (randomBannerItemForHero != null)
				{
					hero.BannerItem = new EquipmentElement(randomBannerItemForHero, null, null, false);
				}
			}
		}

		// Token: 0x06003A41 RID: 14913 RVA: 0x000F0B48 File Offset: 0x000EED48
		private void OnHeroCreated(Hero hero, bool isBornNaturally = false)
		{
			if (this.CanBannerBeGivenToHero(hero))
			{
				ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(hero);
				if (randomBannerItemForHero != null)
				{
					hero.BannerItem = new EquipmentElement(randomBannerItemForHero, null, null, false);
				}
			}
		}

		// Token: 0x06003A42 RID: 14914 RVA: 0x000F0B78 File Offset: 0x000EED78
		private void OnClanCreated(Clan clan, bool isCompanion)
		{
			if (isCompanion)
			{
				Hero leader = clan.Leader;
				if (leader.BannerItem.IsInvalid())
				{
					ItemObject randomBannerItemForHero = BannerHelper.GetRandomBannerItemForHero(leader);
					if (randomBannerItemForHero != null)
					{
						leader.BannerItem = new EquipmentElement(randomBannerItemForHero, null, null, false);
					}
				}
			}
		}

		// Token: 0x06003A43 RID: 14915 RVA: 0x000F0BB8 File Offset: 0x000EEDB8
		private bool CanBannerBeLootedFromHero(Hero hero)
		{
			return !this._heroNextBannerLootTime.ContainsKey(hero) || this._heroNextBannerLootTime[hero].IsPast;
		}

		// Token: 0x06003A44 RID: 14916 RVA: 0x000F0BE9 File Offset: 0x000EEDE9
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

		// Token: 0x06003A45 RID: 14917 RVA: 0x000F0BFC File Offset: 0x000EEDFC
		private void LogBannerLootForHero(Hero hero, int bannerLevel)
		{
			CampaignTime value = CampaignTime.DaysFromNow((float)this.GetCooldownDays(bannerLevel));
			if (!this._heroNextBannerLootTime.ContainsKey(hero))
			{
				this._heroNextBannerLootTime.Add(hero, value);
				return;
			}
			this._heroNextBannerLootTime[hero] = value;
		}

		// Token: 0x06003A46 RID: 14918 RVA: 0x000F0C40 File Offset: 0x000EEE40
		private bool CanBannerBeGivenToHero(Hero hero)
		{
			int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
			return hero.Occupation == Occupation.Lord && hero.Age >= (float)heroComesOfAge && hero.BannerItem.IsInvalid() && hero.Clan != Clan.PlayerClan;
		}

		// Token: 0x0400120C RID: 4620
		private const int BannerLevel1CooldownDays = 4;

		// Token: 0x0400120D RID: 4621
		private const int BannerLevel2CooldownDays = 8;

		// Token: 0x0400120E RID: 4622
		private const int BannerLevel3CooldownDays = 12;

		// Token: 0x0400120F RID: 4623
		private const float BannerItemUpdateChance = 0.1f;

		// Token: 0x04001210 RID: 4624
		private const float GiveBannerItemChance = 0.25f;

		// Token: 0x04001211 RID: 4625
		private Dictionary<Hero, CampaignTime> _heroNextBannerLootTime = new Dictionary<Hero, CampaignTime>();
	}
}
