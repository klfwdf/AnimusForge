using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000446 RID: 1094
	public class TradeCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060045C3 RID: 17859 RVA: 0x0015AE66 File Offset: 0x00159066
		public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeMarkets();
		}

		// Token: 0x060045C4 RID: 17860 RVA: 0x0015AE70 File Offset: 0x00159070
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, new Action<Town>(this.DailyTickTown));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedPartialFollowUp));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x060045C5 RID: 17861 RVA: 0x0015AEDC File Offset: 0x001590DC
		private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter campaignGameStarter, int i)
		{
			if (i == 2)
			{
				this.InitializeTrade();
			}
			if (i % 10 == 0)
			{
				foreach (Town town in Campaign.Current.AllTowns)
				{
					this.UpdateMarketStores(town);
				}
			}
		}

		// Token: 0x060045C6 RID: 17862 RVA: 0x0015AF44 File Offset: 0x00159144
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			foreach (Town town in Town.AllTowns)
			{
				this.UpdateMarketStores(town);
			}
		}

		// Token: 0x060045C7 RID: 17863 RVA: 0x0015AF98 File Offset: 0x00159198
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<ItemCategory, float>>("_numberOfTotalItemsAtGameWorld", ref this._numberOfTotalItemsAtGameWorld);
		}

		// Token: 0x060045C8 RID: 17864 RVA: 0x0015AFAC File Offset: 0x001591AC
		private void InitializeTrade()
		{
			this._numberOfTotalItemsAtGameWorld = new Dictionary<ItemCategory, float>();
			(from settlement in Campaign.Current.Settlements
				where settlement.IsTown
				select settlement).ToList<Settlement>();
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (hero.CharacterObject.Occupation == Occupation.Lord && hero.Clan != Clan.PlayerClan)
				{
					Clan clan = hero.Clan;
					int amount;
					if (((clan != null) ? clan.Leader : null) == hero)
					{
						amount = 50000 + 10000 * hero.Clan.Tier + ((hero == hero.MapFaction.Leader) ? 50000 : 0);
					}
					else
					{
						amount = 10000;
					}
					GiveGoldAction.ApplyBetweenCharacters(null, hero, amount, false);
				}
			}
		}

		// Token: 0x060045C9 RID: 17865 RVA: 0x0015B0AC File Offset: 0x001592AC
		public void DailyTickTown(Town town)
		{
			this.UpdateMarketStores(town);
		}

		// Token: 0x060045CA RID: 17866 RVA: 0x0015B0B5 File Offset: 0x001592B5
		private void UpdateMarketStores(Town town)
		{
			town.MarketData.UpdateStores();
		}

		// Token: 0x060045CB RID: 17867 RVA: 0x0015B0C4 File Offset: 0x001592C4
		private void InitializeMarkets()
		{
			foreach (Town town in Town.AllTowns)
			{
				foreach (ItemCategory itemCategory in ItemCategories.All)
				{
					if (itemCategory.IsValid)
					{
						town.MarketData.AddDemand(itemCategory, 3f);
						town.MarketData.AddSupply(itemCategory, 2f);
					}
				}
			}
		}

		// Token: 0x04001380 RID: 4992
		private Dictionary<ItemCategory, float> _numberOfTotalItemsAtGameWorld;

		// Token: 0x04001381 RID: 4993
		public const float MaximumTaxRatioForVillages = 1f;

		// Token: 0x04001382 RID: 4994
		public const float MaximumTaxRatioForTowns = 0.5f;

		// Token: 0x02000853 RID: 2131
		public enum TradeGoodType
		{
			// Token: 0x04002369 RID: 9065
			Grain,
			// Token: 0x0400236A RID: 9066
			Wood,
			// Token: 0x0400236B RID: 9067
			Meat,
			// Token: 0x0400236C RID: 9068
			Wool,
			// Token: 0x0400236D RID: 9069
			Cheese,
			// Token: 0x0400236E RID: 9070
			Iron,
			// Token: 0x0400236F RID: 9071
			Salt,
			// Token: 0x04002370 RID: 9072
			Spice,
			// Token: 0x04002371 RID: 9073
			Raw_Silk,
			// Token: 0x04002372 RID: 9074
			Fish,
			// Token: 0x04002373 RID: 9075
			Flax,
			// Token: 0x04002374 RID: 9076
			Grape,
			// Token: 0x04002375 RID: 9077
			Hides,
			// Token: 0x04002376 RID: 9078
			Clay,
			// Token: 0x04002377 RID: 9079
			Date_Fruit,
			// Token: 0x04002378 RID: 9080
			Bread,
			// Token: 0x04002379 RID: 9081
			Beer,
			// Token: 0x0400237A RID: 9082
			Wine,
			// Token: 0x0400237B RID: 9083
			Tools,
			// Token: 0x0400237C RID: 9084
			Pottery,
			// Token: 0x0400237D RID: 9085
			Cloth,
			// Token: 0x0400237E RID: 9086
			Linen,
			// Token: 0x0400237F RID: 9087
			Leather,
			// Token: 0x04002380 RID: 9088
			Velvet,
			// Token: 0x04002381 RID: 9089
			Saddle_Horse,
			// Token: 0x04002382 RID: 9090
			Steppe_Horse,
			// Token: 0x04002383 RID: 9091
			Hunter,
			// Token: 0x04002384 RID: 9092
			Desert_Horse,
			// Token: 0x04002385 RID: 9093
			Charger,
			// Token: 0x04002386 RID: 9094
			War_Horse,
			// Token: 0x04002387 RID: 9095
			Steppe_Charger,
			// Token: 0x04002388 RID: 9096
			Desert_War_Horse,
			// Token: 0x04002389 RID: 9097
			Unknown,
			// Token: 0x0400238A RID: 9098
			NumberOfTradeItems
		}
	}
}
