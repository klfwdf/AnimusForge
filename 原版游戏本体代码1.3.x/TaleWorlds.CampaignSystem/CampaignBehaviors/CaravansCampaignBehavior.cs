using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D9 RID: 985
	public class CaravansCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E06 RID: 3590
		// (get) Token: 0x06003A9B RID: 15003 RVA: 0x000F2840 File Offset: 0x000F0A40
		private int MaxNumberOfItemsToBuyFromSingleCategory
		{
			get
			{
				return Campaign.Current.Models.CaravanModel.MaxNumberOfItemsToBuyFromSingleCategory;
			}
		}

		// Token: 0x17000E07 RID: 3591
		// (get) Token: 0x06003A9C RID: 15004 RVA: 0x000F2856 File Offset: 0x000F0A56
		public ITradeAgreementsCampaignBehavior TradeAgreementsCampaignBehavior
		{
			get
			{
				if (this._tradeAgreementsBehavior == null)
				{
					this._tradeAgreementsBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
				}
				return this._tradeAgreementsBehavior;
			}
		}

		// Token: 0x06003A9D RID: 15005 RVA: 0x000F2876 File Offset: 0x000F0A76
		private float GetDistanceLimitVeryFarAsDaysForNavigationType(bool isNavalCaravan)
		{
			if (!isNavalCaravan)
			{
				return this._defaultCaravanVeryFarCache;
			}
			return this._navalCaravanVeryFarCache;
		}

		// Token: 0x06003A9E RID: 15006 RVA: 0x000F2888 File Offset: 0x000F0A88
		private float GetDistanceLimitFarAsDaysForNavigationType(bool isNavalCaravan)
		{
			return this.GetDistanceLimitVeryFarAsDaysForNavigationType(isNavalCaravan) * 0.75f;
		}

		// Token: 0x06003A9F RID: 15007 RVA: 0x000F2897 File Offset: 0x000F0A97
		private float GetDistanceLimitMediumAsDaysForNavigationType(bool isNavalCaravan)
		{
			return this.GetDistanceLimitVeryFarAsDaysForNavigationType(isNavalCaravan) * 0.5f;
		}

		// Token: 0x06003AA0 RID: 15008 RVA: 0x000F28A6 File Offset: 0x000F0AA6
		private float GetDistanceLimitCloseAsDaysForNavigationType(bool isNavalCaravan)
		{
			return this.GetDistanceLimitVeryFarAsDaysForNavigationType(isNavalCaravan) * 0.25f;
		}

		// Token: 0x06003AA1 RID: 15009 RVA: 0x000F28B8 File Offset: 0x000F0AB8
		public CaravansCampaignBehavior()
		{
			this._tradeActionLogPool = new CaravansCampaignBehavior.TradeActionLogPool(4096);
		}

		// Token: 0x06003AA2 RID: 15010 RVA: 0x000F2968 File Offset: 0x000F0B68
		public override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.DailyTickHero));
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickParty));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEndEvent));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.OnMobilePartyCreated));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
			CampaignEvents.OnLootDistributedToPartyEvent.AddNonSerializedListener(this, new Action<PartyBase, PartyBase, ItemRoster>(this.OnLootDistributedToParty));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
		}

		// Token: 0x06003AA3 RID: 15011 RVA: 0x000F2AB7 File Offset: 0x000F0CB7
		private void OnKingdomDestroyed(Kingdom destroyedKingdom)
		{
			if (this._prohibitedKingdomsForPlayerCaravans.Contains(destroyedKingdom))
			{
				this._prohibitedKingdomsForPlayerCaravans.Remove(destroyedKingdom);
			}
		}

		// Token: 0x06003AA4 RID: 15012 RVA: 0x000F2AD4 File Offset: 0x000F0CD4
		private void OnGameLoadFinished()
		{
			this.CreatePriceDataCache();
			foreach (MobileParty mobileParty in MobileParty.AllCaravanParties)
			{
				if ((!mobileParty.IsActive || !mobileParty.IsReady) && this._caravanLastHomeTownVisitTime.ContainsKey(mobileParty))
				{
					this._caravanLastHomeTownVisitTime.Remove(mobileParty);
				}
			}
		}

		// Token: 0x06003AA5 RID: 15013 RVA: 0x000F2B50 File Offset: 0x000F0D50
		private void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
			for (int i = 0; i < siegeEvent.BesiegedSettlement.Parties.Count; i++)
			{
				if (siegeEvent.BesiegedSettlement.Parties[i].IsCaravan)
				{
					siegeEvent.BesiegedSettlement.Parties[i].SetMoveModeHold();
				}
			}
		}

		// Token: 0x06003AA6 RID: 15014 RVA: 0x000F2BA6 File Offset: 0x000F0DA6
		private void OnLootDistributedToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster lootedItems)
		{
			if (winnerParty.IsMobile && defeatedParty.IsMobile && defeatedParty.MobileParty.IsCaravan)
			{
				SkillLevelingManager.OnLoot(winnerParty.MobileParty, defeatedParty.MobileParty, lootedItems, true);
			}
		}

		// Token: 0x06003AA7 RID: 15015 RVA: 0x000F2BD8 File Offset: 0x000F0DD8
		private void OnNewGameCreatedPartialFollowUpEndEvent(CampaignGameStarter obj)
		{
			for (int i = 0; i < 2; i++)
			{
				foreach (Hero hero in Hero.AllAliveHeroes)
				{
					if (hero.Clan != Clan.PlayerClan && Campaign.Current.Models.CaravanModel.CanHeroCreateCaravan(hero))
					{
						this.SpawnCaravan(hero, true);
					}
				}
				this.UpdateAverageValues();
				this.DoInitialTradeRuns();
			}
		}

		// Token: 0x06003AA8 RID: 15016 RVA: 0x000F2C68 File Offset: 0x000F0E68
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<MobileParty, CampaignTime>>("_tradeRumorTakenCaravans", ref this._tradeRumorTakenCaravans);
			dataStore.SyncData<Dictionary<MobileParty, CampaignTime>>("_lootedCaravans", ref this._lootedCaravans);
			dataStore.SyncData<Dictionary<MobileParty, CaravansCampaignBehavior.PlayerInteraction>>("_interactedCaravans", ref this._interactedCaravans);
			dataStore.SyncData<Dictionary<MobileParty, List<CaravansCampaignBehavior.TradeActionLog>>>("_tradeActionLogs", ref this._tradeActionLogs);
			dataStore.SyncData<Dictionary<MobileParty, CampaignTime>>("_caravanLastHomeTownVisitTime", ref this._caravanLastHomeTownVisitTime);
			dataStore.SyncData<List<Kingdom>>("_prohibitedKingdomsForPlayerCaravans", ref this._prohibitedKingdomsForPlayerCaravans);
		}

		// Token: 0x06003AA9 RID: 15017 RVA: 0x000F2CE4 File Offset: 0x000F0EE4
		private void DoInitialTradeRuns()
		{
			foreach (MobileParty mobileParty in MobileParty.AllCaravanParties)
			{
				Settlement currentSettlement = mobileParty.CurrentSettlement;
				Town town = ((currentSettlement != null) ? currentSettlement.Town : null);
				MobileParty.NavigationType navigationType = (mobileParty.HasNavalNavigationCapability ? MobileParty.NavigationType.Naval : MobileParty.NavigationType.Default);
				List<ValueTuple<Town, float>> list = new List<ValueTuple<Town, float>>();
				foreach (Town town2 in Town.AllTowns)
				{
					if (town2.Settlement.HasPort || mobileParty.HasLandNavigationCapability)
					{
						bool flag = navigationType == MobileParty.NavigationType.Naval;
						float distance;
						if (mobileParty.CurrentSettlement != null)
						{
							distance = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, town2.Settlement, flag, flag, navigationType);
						}
						else
						{
							float num;
							distance = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, town2.Settlement, flag, navigationType, out num);
						}
						list.Add(new ValueTuple<Town, float>(town2, 1f / MathF.Pow(distance, 1.5f)));
					}
				}
				Town town3 = MBRandom.ChooseWeighted<Town>(list);
				if (town3 != null && town != null)
				{
					this.CreatePriceDataCache();
					if (MBRandom.RandomFloat < 0.5f)
					{
						this.SellGoods(mobileParty, town, 0.7f, false);
						this.BuyGoods(mobileParty, town);
						this.SellGoods(mobileParty, town3, 0.7f, false);
					}
					else
					{
						this.SellGoods(mobileParty, town3, 0.7f, false);
						this.BuyGoods(mobileParty, town3);
						this.SellGoods(mobileParty, town, 0.7f, false);
					}
				}
			}
		}

		// Token: 0x06003AAA RID: 15018 RVA: 0x000F2EC4 File Offset: 0x000F10C4
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.CacheVeryFarDistances();
			this.AddDialogs(campaignGameStarter);
			this.UpdateAverageValues();
		}

		// Token: 0x06003AAB RID: 15019 RVA: 0x000F2EDC File Offset: 0x000F10DC
		private void CacheVeryFarDistances()
		{
			MobileParty.NavigationType navigationType = MobileParty.NavigationType.Naval;
			float num = 20f;
			float num2 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * num;
			float num3 = Campaign.Current.EstimatedAverageCaravanPartyNavalSpeed * (float)CampaignTime.HoursInDay;
			this._navalCaravanVeryFarCache = num2 / num3;
			navigationType = MobileParty.NavigationType.Default;
			num = 5f;
			num2 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * num;
			num3 = Campaign.Current.EstimatedAverageCaravanPartySpeed * (float)CampaignTime.HoursInDay;
			this._defaultCaravanVeryFarCache = num2 / num3;
		}

		// Token: 0x06003AAC RID: 15020 RVA: 0x000F2F4C File Offset: 0x000F114C
		private void OnMapEventEnded(MapEvent mapEvent)
		{
			foreach (PartyBase partyBase in mapEvent.InvolvedParties)
			{
				if (partyBase.IsMobile && partyBase.MobileParty.IsCaravan && mapEvent.IsWinnerSide(partyBase.Side))
				{
					if (partyBase.MobileParty.HasNavalNavigationCapability)
					{
						this.DiscardShips(partyBase.MobileParty);
					}
					MobileParty mobileParty = partyBase.MobileParty;
					int numberOfPackAnimals = mobileParty.ItemRoster.NumberOfPackAnimals;
					int numberOfLivestockAnimals = mobileParty.ItemRoster.NumberOfLivestockAnimals;
					int numberOfMounts = mobileParty.ItemRoster.NumberOfMounts;
					int totalManCount = mobileParty.MemberRoster.TotalManCount;
					if (partyBase.MobileParty.HasLandNavigationCapability && (float)(numberOfPackAnimals + numberOfLivestockAnimals + numberOfMounts) > (float)totalManCount * 1.2f)
					{
						int num2;
						for (int i = numberOfPackAnimals + numberOfLivestockAnimals + numberOfMounts; i > totalManCount; i -= num2)
						{
							int num = 10000;
							ItemRosterElement itemRosterElement = partyBase.MobileParty.ItemRoster[0];
							foreach (ItemRosterElement itemRosterElement2 in partyBase.MobileParty.ItemRoster)
							{
								if (itemRosterElement2.EquipmentElement.Item.IsMountable || itemRosterElement2.EquipmentElement.Item.ItemCategory == DefaultItemCategories.PackAnimal || itemRosterElement2.EquipmentElement.Item.IsAnimal)
								{
									int itemValue = itemRosterElement2.EquipmentElement.ItemValue;
									if (itemValue < num)
									{
										num = itemValue;
										itemRosterElement = itemRosterElement2;
									}
								}
							}
							num2 = MathF.Min(itemRosterElement.Amount, MathF.Max(1, i - totalManCount));
							mobileParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num2);
						}
					}
					int inventoryCapacity = mobileParty.InventoryCapacity;
					float totalWeightCarried = mobileParty.TotalWeightCarried;
					float num3 = 0f;
					if (totalWeightCarried - num3 > (float)inventoryCapacity)
					{
						while (totalWeightCarried - num3 > (float)inventoryCapacity)
						{
							int num4 = 10000;
							ItemRosterElement itemRosterElement3 = partyBase.MobileParty.ItemRoster[0];
							foreach (ItemRosterElement itemRosterElement4 in partyBase.MobileParty.ItemRoster)
							{
								if (!itemRosterElement4.EquipmentElement.Item.IsMountable)
								{
									int itemValue2 = itemRosterElement4.EquipmentElement.ItemValue;
									if (itemValue2 < num4)
									{
										num4 = itemValue2;
										itemRosterElement3 = itemRosterElement4;
									}
								}
							}
							int val = MathF.Ceiling((totalWeightCarried - num3 - (float)inventoryCapacity) / itemRosterElement3.EquipmentElement.Weight);
							int num5 = Math.Max(1, Math.Min(itemRosterElement3.Amount, val));
							float weight = itemRosterElement3.EquipmentElement.Weight;
							mobileParty.ItemRoster.AddToCounts(itemRosterElement3.EquipmentElement, -num5);
							num3 += weight * (float)num5;
						}
					}
				}
			}
		}

		// Token: 0x06003AAD RID: 15021 RVA: 0x000F32A4 File Offset: 0x000F14A4
		public void SpawnCaravan(Hero hero, bool initialSpawn = false)
		{
			bool flag = Campaign.Current.Models.CaravanModel.GetEliteCaravanSpawnChance(hero) > hero.RandomFloat();
			PartyTemplateObject randomElementWithPredicate = (flag ? hero.Culture.EliteCaravanPartyTemplates : hero.Culture.CaravanPartyTemplates).GetRandomElementWithPredicate((PartyTemplateObject x) => x.ShipHulls.Count == 0 != hero.CurrentSettlement.HasPort);
			bool isNaval = randomElementWithPredicate.ShipHulls.Any<ShipTemplateStack>();
			Settlement settlement = hero.HomeSettlement ?? hero.BornSettlement;
			Settlement spawnSettlement;
			if (settlement == null)
			{
				spawnSettlement = Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Settlement.HasPort == isNaval).Settlement;
			}
			else if (settlement.IsTown)
			{
				spawnSettlement = settlement;
			}
			else if (settlement.IsVillage)
			{
				spawnSettlement = settlement.Village.TradeBound ?? Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Settlement.HasPort == isNaval).Settlement;
			}
			else
			{
				spawnSettlement = Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Settlement.HasPort == isNaval).Settlement;
			}
			MobileParty caravanParty = CaravanPartyComponent.CreateCaravanParty(hero, spawnSettlement, randomElementWithPredicate, initialSpawn, null, null, flag);
			if (!initialSpawn)
			{
				hero.AddPower((float)Campaign.Current.Models.CaravanModel.GetPowerChangeAfterCaravanCreation(hero, caravanParty));
			}
		}

		// Token: 0x06003AAE RID: 15022 RVA: 0x000F340C File Offset: 0x000F160C
		private void UpdateAverageValues()
		{
			Dictionary<ItemCategory, ValueTuple<float, int>> dictionary = new Dictionary<ItemCategory, ValueTuple<float, int>>();
			foreach (ItemObject itemObject in Items.All)
			{
				if (itemObject.IsReady)
				{
					ValueTuple<float, int> valueTuple;
					dictionary.TryGetValue(itemObject.ItemCategory, out valueTuple);
					dictionary[itemObject.ItemCategory] = new ValueTuple<float, int>(valueTuple.Item1 + (float)MathF.Min(500, itemObject.Value), valueTuple.Item2 + 1);
				}
			}
			this._packAnimalCategoryIndex = -1;
			for (int i = 0; i < ItemCategories.All.Count; i++)
			{
				ItemCategory itemCategory = ItemCategories.All[i];
				ValueTuple<float, int> valueTuple2;
				bool flag = dictionary.TryGetValue(itemCategory, out valueTuple2);
				this._averageValuesCached[itemCategory] = (flag ? (valueTuple2.Item1 / (float)valueTuple2.Item2) : 1f);
				if (itemCategory == DefaultItemCategories.PackAnimal)
				{
					this._packAnimalCategoryIndex = i;
				}
			}
		}

		// Token: 0x06003AAF RID: 15023 RVA: 0x000F3518 File Offset: 0x000F1718
		private void CreatePriceDataCache()
		{
			foreach (ItemCategory itemCategory in ItemCategories.All)
			{
				float num = 0f;
				float num2 = 1000f;
				float num3 = 0f;
				float num4 = 1000f;
				foreach (Town town in Town.AllTowns)
				{
					float itemCategoryPriceIndex = town.GetItemCategoryPriceIndex(itemCategory);
					num += itemCategoryPriceIndex;
					if (itemCategoryPriceIndex < num2)
					{
						num2 = itemCategoryPriceIndex;
					}
					if (town.Settlement.HasPort)
					{
						num3 += itemCategoryPriceIndex;
						if (itemCategoryPriceIndex < num4)
						{
							num4 = itemCategoryPriceIndex;
						}
					}
				}
				float averageBuySellPriceIndex = num / (float)Town.AllTowns.Count;
				float averageBuySellPriceIndex2 = num3 / (float)Town.AllTowns.Count((Town x) => x.Settlement.HasPort);
				this._priceDictionary[itemCategory] = new CaravansCampaignBehavior.PriceIndexData(averageBuySellPriceIndex, num2);
				this._coastalPriceDictionary[itemCategory] = new CaravansCampaignBehavior.PriceIndexData(averageBuySellPriceIndex2, num4);
			}
		}

		// Token: 0x06003AB0 RID: 15024 RVA: 0x000F3658 File Offset: 0x000F1858
		public void DailyTick()
		{
			this.DeleteExpiredTradeRumorTakenCaravans();
			this.DeleteExpiredLootedCaravans();
			this.CreatePriceDataCache();
		}

		// Token: 0x06003AB1 RID: 15025 RVA: 0x000F366C File Offset: 0x000F186C
		private void DailyTickHero(Hero hero)
		{
			if (hero != Hero.MainHero && MBRandom.RandomFloat < 0.75f && Campaign.Current.Models.CaravanModel.CanHeroCreateCaravan(hero))
			{
				this.SpawnCaravan(hero, false);
			}
		}

		// Token: 0x06003AB2 RID: 15026 RVA: 0x000F36A4 File Offset: 0x000F18A4
		private void DeleteExpiredTradeRumorTakenCaravans()
		{
			List<MobileParty> list = new List<MobileParty>();
			foreach (KeyValuePair<MobileParty, CampaignTime> keyValuePair in this._tradeRumorTakenCaravans)
			{
				if (CampaignTime.Now - keyValuePair.Value >= CampaignTime.Days(1f))
				{
					list.Add(keyValuePair.Key);
				}
			}
			foreach (MobileParty key in list)
			{
				this._tradeRumorTakenCaravans.Remove(key);
			}
		}

		// Token: 0x06003AB3 RID: 15027 RVA: 0x000F376C File Offset: 0x000F196C
		private void DeleteExpiredLootedCaravans()
		{
			List<MobileParty> list = new List<MobileParty>();
			foreach (KeyValuePair<MobileParty, CampaignTime> keyValuePair in this._lootedCaravans)
			{
				if (CampaignTime.Now - keyValuePair.Value >= CampaignTime.Days(10f))
				{
					list.Add(keyValuePair.Key);
				}
			}
			foreach (MobileParty key in list)
			{
				this._lootedCaravans.Remove(key);
			}
		}

		// Token: 0x06003AB4 RID: 15028 RVA: 0x000F3834 File Offset: 0x000F1A34
		private Town GetDestinationForMobileParty(MobileParty party)
		{
			Settlement targetSettlement = party.TargetSettlement;
			if (targetSettlement == null)
			{
				return null;
			}
			return targetSettlement.Town;
		}

		// Token: 0x06003AB5 RID: 15029 RVA: 0x000F3848 File Offset: 0x000F1A48
		public void HourlyTickParty(MobileParty mobileParty)
		{
			if (!Campaign.Current.GameStarted)
			{
				return;
			}
			if (mobileParty.IsCaravan)
			{
				bool flag = false;
				float randomFloat = MBRandom.RandomFloat;
				if (mobileParty.MapEvent == null && !mobileParty.IsInRaftState && mobileParty.IsPartyTradeActive && !mobileParty.Ai.DoNotMakeNewDecisions && mobileParty.DefaultBehavior != AiBehavior.MoveToNearestLandOrPort)
				{
					if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsFortification)
					{
						if ((!mobileParty.CurrentSettlement.IsUnderSiege || (!mobileParty.CurrentSettlement.SiegeEvent.IsBlockadeActive && mobileParty.HasNavalNavigationCapability)) && mobileParty.ShortTermBehavior != AiBehavior.FleeToPoint && !mobileParty.Ai.IsAlerted && (mobileParty.IsCurrentlyUsedByAQuest || randomFloat < 0.33333334f))
						{
							float num = ((mobileParty.MemberRoster.TotalManCount > 0) ? ((float)mobileParty.MemberRoster.TotalWounded / (float)mobileParty.MemberRoster.TotalManCount) : 1f);
							float num2 = 1f;
							if ((double)num > 0.4)
							{
								num2 = 0f;
							}
							else if ((double)num > 0.2)
							{
								num2 = 0.1f;
							}
							else if ((double)num > 0.1)
							{
								num2 = 0.2f;
							}
							else if ((double)num > 0.05)
							{
								num2 = 0.3f;
							}
							else if ((double)num > 0.025)
							{
								num2 = 0.4f;
							}
							float randomFloat2 = MBRandom.RandomFloat;
							if (num2 > randomFloat2)
							{
								flag = true;
							}
						}
					}
					else
					{
						Town destinationForMobileParty = this.GetDestinationForMobileParty(mobileParty);
						flag = destinationForMobileParty == null || (destinationForMobileParty.IsUnderSiege && (!mobileParty.HasNavalNavigationCapability || destinationForMobileParty.Settlement.SiegeEvent.IsBlockadeActive)) || !this.CanTradeWith(mobileParty.MapFaction, destinationForMobileParty.MapFaction);
					}
					if (flag)
					{
						if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsTown)
						{
							Town town = mobileParty.CurrentSettlement.Town;
							this.BuyGoods(mobileParty, town);
						}
						MobileParty.NavigationType navigationType;
						bool isFromPort;
						bool isTargetingPort;
						Town town2 = this.ThinkNextDestination(mobileParty, out navigationType, out isFromPort, out isTargetingPort);
						if (town2 != null)
						{
							SetPartyAiAction.GetActionForVisitingSettlement(mobileParty, town2.Settlement, navigationType, isFromPort, isTargetingPort);
						}
					}
				}
			}
		}

		// Token: 0x06003AB6 RID: 15030 RVA: 0x000F3A78 File Offset: 0x000F1C78
		public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			Town town = settlement.Town;
			if (Campaign.Current.GameStarted && mobileParty != null && town != null && mobileParty.IsCaravan && mobileParty.IsPartyTradeActive && mobileParty.IsActive)
			{
				if (mobileParty.DefaultBehavior == AiBehavior.MoveToNearestLandOrPort)
				{
					mobileParty.SetMoveModeHold();
				}
				if (mobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability)
				{
					this.AdjustConvoyShips(mobileParty, town);
					this.RefillConvoyTroops(mobileParty);
				}
				if (Campaign.Current.GameStarted)
				{
					List<CaravansCampaignBehavior.TradeActionLog> list;
					if (this._tradeActionLogs.TryGetValue(mobileParty, out list))
					{
						for (int i = list.Count - 1; i >= 0; i--)
						{
							CaravansCampaignBehavior.TradeActionLog tradeActionLog = list[i];
							if (tradeActionLog.BoughtTime.ElapsedDaysUntilNow > 7f)
							{
								list.RemoveAt(i);
								this._tradeActionLogPool.ReleaseLog(tradeActionLog);
							}
						}
					}
					this.SellGoods(mobileParty, town, 1.1f, false);
				}
				if (mobileParty.HomeSettlement == settlement)
				{
					this._caravanLastHomeTownVisitTime[mobileParty] = CampaignTime.Now;
				}
			}
			if (mobileParty != null && mobileParty.IsCaravan && mobileParty.HasLandNavigationCapability && settlement.IsTown && settlement.Town.Governor != null && settlement.Town.Governor.GetPerkValue(DefaultPerks.Trade.Tollgates))
			{
				settlement.Town.TradeTaxAccumulated += MathF.Round(DefaultPerks.Trade.Tollgates.SecondaryBonus);
			}
		}

		// Token: 0x06003AB7 RID: 15031 RVA: 0x000F3BD8 File Offset: 0x000F1DD8
		private void DiscardShips(MobileParty convoy)
		{
			MBList<ValueTuple<Ship, float>> mblist = new MBList<ValueTuple<Ship, float>>();
			foreach (Ship ship in convoy.Ships)
			{
				mblist.Add(new ValueTuple<Ship, float>(ship, this.GetShipPriority(convoy, ship, false)));
			}
			mblist = (from x in mblist
				orderby x.Item2 descending
				select x).ToMBList<ValueTuple<Ship, float>>();
			int idealShipNumber = Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(convoy);
			for (int i = 0; i < mblist.Count; i++)
			{
				Ship item = mblist[i].Item1;
				float item2 = mblist[i].Item2;
				if (i >= idealShipNumber || (item2 == -3.4028235E+38f && convoy.Ships.Count > 1))
				{
					DestroyShipAction.ApplyByDiscard(item);
				}
			}
		}

		// Token: 0x06003AB8 RID: 15032 RVA: 0x000F3CD8 File Offset: 0x000F1ED8
		private void RefillConvoyTroops(MobileParty convoy)
		{
			PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(convoy.Owner.Culture, convoy.CaravanPartyComponent.IsElite, false);
			int totalManCount = convoy.MemberRoster.TotalManCount;
			int num = convoy.Party.PartySizeLimit - totalManCount;
			if (num > 0)
			{
				int num2 = randomCaravanTemplate.Stacks.Sum((PartyTemplateStack x) => x.MaxValue);
				float num3 = (float)num / (float)num2;
				foreach (PartyTemplateStack partyTemplateStack in randomCaravanTemplate.Stacks)
				{
					CharacterObject character = partyTemplateStack.Character;
					int num4 = MathF.Floor((float)partyTemplateStack.MaxValue * num3);
					num -= num4;
					convoy.MemberRoster.AddToCounts(character, num4, false, 0, 0, true, -1);
				}
				if (num > 0)
				{
					List<ValueTuple<int, float>> list = new List<ValueTuple<int, float>>();
					for (int i = 0; i < randomCaravanTemplate.Stacks.Count; i++)
					{
						PartyTemplateStack partyTemplateStack2 = randomCaravanTemplate.Stacks[i];
						float item = (float)(partyTemplateStack2.MaxValue + partyTemplateStack2.MinValue) / 2f;
						list.Add(new ValueTuple<int, float>(i, item));
					}
					for (int j = 0; j < num; j++)
					{
						int index = MBRandom.ChooseWeighted<int>(list);
						CharacterObject character2 = randomCaravanTemplate.Stacks[index].Character;
						convoy.MemberRoster.AddToCounts(character2, 1, false, 0, 0, true, -1);
					}
				}
			}
		}

		// Token: 0x06003AB9 RID: 15033 RVA: 0x000F3E68 File Offset: 0x000F2068
		private void AdjustConvoyShips(MobileParty caravan, Town town)
		{
			if (town.AvailableShips.Count > 0 && caravan.PartyTradeGold > 10000 && caravan.Ships.Count < Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(caravan))
			{
				this.BuyShips(caravan, town);
			}
		}

		// Token: 0x06003ABA RID: 15034 RVA: 0x000F3EBC File Offset: 0x000F20BC
		private void BuyShips(MobileParty caravan, Town town)
		{
			MBList<ValueTuple<Ship, float>> mblist = new MBList<ValueTuple<Ship, float>>();
			bool flag = false;
			if (caravan.CaravanPartyComponent.IsElite)
			{
				flag = true;
				for (int i = 0; i < caravan.Ships.Count; i++)
				{
					if (caravan.Ships[i].ShipHull.Type == ShipHull.ShipType.Medium)
					{
						flag = false;
						break;
					}
				}
			}
			for (int j = town.AvailableShips.Count - 1; j >= 0; j--)
			{
				Ship ship = town.AvailableShips[j];
				if (ship.ShipHull.Type == ShipHull.ShipType.Light || (ship.ShipHull.Type == ShipHull.ShipType.Medium && flag))
				{
					mblist.Add(new ValueTuple<Ship, float>(ship, this.GetShipPriority(caravan, ship, false)));
				}
			}
			mblist = (from x in mblist
				orderby x.Item2 descending
				select x).ToMBList<ValueTuple<Ship, float>>();
			int idealShipNumber = Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(caravan);
			int num = 0;
			while (num < mblist.Count && caravan.Ships.Count < idealShipNumber)
			{
				Ship item = mblist[num].Item1;
				if ((float)caravan.PartyTradeGold * 0.5f >= Campaign.Current.Models.ShipCostModel.GetShipTradeValue(item, town.Settlement.Party, caravan.Party) && (item.ShipHull.Type != ShipHull.ShipType.Medium || flag))
				{
					ChangeShipOwnerAction.ApplyByTrade(caravan.Party, item);
					if (item.ShipHull.Type == ShipHull.ShipType.Medium)
					{
						flag = false;
					}
				}
				num++;
			}
		}

		// Token: 0x06003ABB RID: 15035 RVA: 0x000F4056 File Offset: 0x000F2256
		private float GetShipPriority(MobileParty convoy, Ship ship, bool isForSelling)
		{
			return Campaign.Current.Models.PartyShipLimitModel.GetShipPriority(convoy, ship, isForSelling);
		}

		// Token: 0x06003ABC RID: 15036 RVA: 0x000F4070 File Offset: 0x000F2270
		public void OnSettlementLeft(MobileParty mobileParty, Settlement settlement)
		{
			if (mobileParty != null && mobileParty != MobileParty.MainParty && (mobileParty.IsCaravan || mobileParty.IsLordParty))
			{
				int inventoryCapacity = mobileParty.InventoryCapacity;
				float totalWeightCarried = mobileParty.TotalWeightCarried;
				Town town = (settlement.IsTown ? settlement.Town : (settlement.IsVillage ? settlement.Village.Bound.Town : null));
				if (town != null)
				{
					float num = 1.1f;
					while (totalWeightCarried > (float)inventoryCapacity)
					{
						this.SellGoods(mobileParty, town, num, true);
						num -= 0.02f;
						if (num < 0.75f)
						{
							break;
						}
						inventoryCapacity = mobileParty.InventoryCapacity;
						totalWeightCarried = mobileParty.TotalWeightCarried;
					}
				}
			}
		}

		// Token: 0x06003ABD RID: 15037 RVA: 0x000F4110 File Offset: 0x000F2310
		private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			if (mobileParty.IsCaravan)
			{
				if (this._interactedCaravans.ContainsKey(mobileParty))
				{
					this._interactedCaravans.Remove(mobileParty);
				}
				List<CaravansCampaignBehavior.TradeActionLog> list;
				if (this._tradeActionLogs.TryGetValue(mobileParty, out list))
				{
					this._tradeActionLogs.Remove(mobileParty);
					for (int i = 0; i < list.Count; i++)
					{
						CaravansCampaignBehavior.TradeActionLog log = list[i];
						this._tradeActionLogPool.ReleaseLog(log);
					}
				}
				if (this._caravanLastHomeTownVisitTime.ContainsKey(mobileParty))
				{
					this._caravanLastHomeTownVisitTime.Remove(mobileParty);
				}
			}
		}

		// Token: 0x06003ABE RID: 15038 RVA: 0x000F419D File Offset: 0x000F239D
		private void OnMobilePartyCreated(MobileParty mobileParty)
		{
			if (mobileParty.IsCaravan)
			{
				this._caravanLastHomeTownVisitTime.Add(mobileParty, CampaignTime.Now);
			}
		}

		// Token: 0x06003ABF RID: 15039 RVA: 0x000F41B8 File Offset: 0x000F23B8
		private Town ThinkNextDestination(MobileParty caravanParty, out MobileParty.NavigationType bestNavigationType, out bool isFromPort, out bool isTargetingPort)
		{
			this.RefreshTotalValueOfItemsAtCategoryForParty(caravanParty);
			Town town = this.FindNextDestinationForCaravan(caravanParty, true, out bestNavigationType, out isFromPort, out isTargetingPort);
			if (town == null)
			{
				town = this.FindNextDestinationForCaravan(caravanParty, false, out bestNavigationType, out isFromPort, out isTargetingPort);
			}
			return town;
		}

		// Token: 0x06003AC0 RID: 15040 RVA: 0x000F41EC File Offset: 0x000F23EC
		private Town FindNextDestinationForCaravan(MobileParty caravanParty, bool distanceCut, out MobileParty.NavigationType bestNavigationType, out bool isFromPort, out bool isTargetingPort)
		{
			float num = 0f;
			Town result = null;
			bestNavigationType = MobileParty.NavigationType.None;
			isTargetingPort = false;
			float num2 = caravanParty.TotalWeightCarried / (float)caravanParty.InventoryCapacity;
			num2 = MBMath.Map(num2, 0f, 1f, 0f, 0.9f);
			CampaignTime lastHomeVisitTimeOfCaravan;
			this._caravanLastHomeTownVisitTime.TryGetValue(caravanParty, out lastHomeVisitTimeOfCaravan);
			bool hasNavalNavigationCapability = caravanParty.HasNavalNavigationCapability;
			foreach (Town town in Town.AllTowns)
			{
				if (town.Owner.Settlement != caravanParty.CurrentSettlement && (!town.IsUnderSiege || (!town.Settlement.SiegeEvent.IsBlockadeActive && hasNavalNavigationCapability)) && this.CanTradeWith(caravanParty.MapFaction, town.MapFaction) && (town.Settlement.HasPort || !hasNavalNavigationCapability) && (!town.Settlement.Parties.Contains(MobileParty.MainParty) || !MobileParty.MainParty.MapFaction.IsAtWarWith(caravanParty.MapFaction)))
				{
					MobileParty.NavigationType navigationType;
					bool flag;
					float tradeScoreForTown = this.GetTradeScoreForTown(caravanParty, town, lastHomeVisitTimeOfCaravan, num2, distanceCut, out navigationType, out flag);
					if (tradeScoreForTown > num)
					{
						num = tradeScoreForTown;
						result = town;
						isTargetingPort = flag;
						bestNavigationType = navigationType;
					}
				}
			}
			isFromPort = isTargetingPort && caravanParty.CurrentSettlement != null;
			return result;
		}

		// Token: 0x06003AC1 RID: 15041 RVA: 0x000F435C File Offset: 0x000F255C
		private void AdjustVeryFarAddition(bool isNavalCaravan, float distanceAsDays, float minimumAddition, ref float veryFarAddition)
		{
			float distanceLimitVeryFarAsDaysForNavigationType = this.GetDistanceLimitVeryFarAsDaysForNavigationType(isNavalCaravan);
			if (distanceAsDays > distanceLimitVeryFarAsDaysForNavigationType)
			{
				veryFarAddition += (distanceAsDays - distanceLimitVeryFarAsDaysForNavigationType) * minimumAddition * 4f;
			}
			float distanceLimitFarAsDaysForNavigationType = this.GetDistanceLimitFarAsDaysForNavigationType(isNavalCaravan);
			if (distanceAsDays > distanceLimitFarAsDaysForNavigationType)
			{
				veryFarAddition += (distanceAsDays - distanceLimitFarAsDaysForNavigationType) * minimumAddition * 3f;
			}
			float distanceLimitMediumAsDaysForNavigationType = this.GetDistanceLimitMediumAsDaysForNavigationType(isNavalCaravan);
			if (distanceAsDays > distanceLimitMediumAsDaysForNavigationType)
			{
				veryFarAddition += (distanceAsDays - distanceLimitMediumAsDaysForNavigationType) * minimumAddition * 2f;
			}
			float distanceLimitCloseAsDaysForNavigationType = this.GetDistanceLimitCloseAsDaysForNavigationType(isNavalCaravan);
			if (distanceAsDays > distanceLimitCloseAsDaysForNavigationType)
			{
				veryFarAddition += (distanceAsDays - distanceLimitCloseAsDaysForNavigationType) * minimumAddition;
			}
		}

		// Token: 0x06003AC2 RID: 15042 RVA: 0x000F43DC File Offset: 0x000F25DC
		private float GetTradeScoreForTown(MobileParty caravanParty, Town town, CampaignTime lastHomeVisitTimeOfCaravan, float caravanFullness, bool distanceCut, out MobileParty.NavigationType bestNavigationType, out bool isTargetingPort)
		{
			bool hasNavalNavigationCapability = caravanParty.HasNavalNavigationCapability;
			isTargetingPort = hasNavalNavigationCapability;
			float num;
			bool flag;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(caravanParty, town.Settlement, isTargetingPort, out bestNavigationType, out num, out flag);
			if (bestNavigationType == MobileParty.NavigationType.None)
			{
				bestNavigationType = MobileParty.NavigationType.None;
				isTargetingPort = false;
				return -1f;
			}
			float num2 = num / ((hasNavalNavigationCapability ? Campaign.Current.EstimatedAverageCaravanPartyNavalSpeed : Campaign.Current.EstimatedAverageCaravanPartySpeed) * (float)CampaignTime.HoursInDay);
			float num3 = 0f;
			this.AdjustVeryFarAddition(hasNavalNavigationCapability, num2, 0.15f, ref num3);
			float elapsedDaysUntilNow = lastHomeVisitTimeOfCaravan.ElapsedDaysUntilNow;
			bool flag2 = elapsedDaysUntilNow > this.GetDistanceLimitVeryFarAsDaysForNavigationType(hasNavalNavigationCapability);
			if (flag2)
			{
				float distanceAsDays = num / ((hasNavalNavigationCapability ? Campaign.Current.EstimatedAverageCaravanPartyNavalSpeed : Campaign.Current.EstimatedAverageCaravanPartySpeed) * (float)CampaignTime.HoursInDay);
				this.AdjustVeryFarAddition(hasNavalNavigationCapability, distanceAsDays, ((elapsedDaysUntilNow - 1f) * MathF.Sqrt(elapsedDaysUntilNow - 1f) - 1f) * 0.008f, ref num3);
			}
			ExplainedNumber explainedNumber = default(ExplainedNumber);
			town.AddEffectOfBuildings(BuildingEffectEnum.CaravanAccessibility, ref explainedNumber);
			float num4 = Math.Max(1f, explainedNumber.ResultNumber);
			float distanceLimitVeryFarAsDaysForNavigationType = this.GetDistanceLimitVeryFarAsDaysForNavigationType(hasNavalNavigationCapability);
			float num5 = num2 + num3;
			if (distanceCut && (town.Owner.Settlement != caravanParty.HomeSettlement || !flag2) && num5 > distanceLimitVeryFarAsDaysForNavigationType)
			{
				bestNavigationType = MobileParty.NavigationType.None;
				isTargetingPort = false;
				return -1f;
			}
			float num6 = (hasNavalNavigationCapability ? MathF.Max(0.1f, 1f - num5 / (2f * distanceLimitVeryFarAsDaysForNavigationType)) : (1f / num5));
			float num7 = 1f;
			if (caravanParty.HomeSettlement == town.Owner.Settlement)
			{
				num7 = 1f + elapsedDaysUntilNow * 0.1f * (elapsedDaysUntilNow * 0.1f);
				if (num6 < 0.5f)
				{
					num6 = 0.5f;
				}
			}
			TownMarketData marketData = town.MarketData;
			float num8 = 1.1f;
			float num9 = 0f;
			for (int i = 0; i < caravanParty.Party.ItemRoster.Count; i++)
			{
				ItemObject item = caravanParty.ItemRoster.GetElementCopyAtIndex(i).EquipmentElement.Item;
				float limitValue = num8 - MathF.Sqrt((float)MathF.Min(this._totalValueOfItemsAtCategory[item.ItemCategory], 5000) / 5000f) * 0.2f;
				num9 += this.CalculateTownSellScoreForCategory(caravanParty, marketData, i, limitValue);
			}
			num9 *= (hasNavalNavigationCapability ? 0.5f : 0.3f) + caravanFullness;
			float num10 = 0f;
			for (int j = 0; j < ItemCategories.All.Count; j++)
			{
				ItemCategory itemCategory = ItemCategories.All[j];
				if (itemCategory.IsTradeGood || itemCategory.IsAnimal)
				{
					num10 += this.CalculateTownBuyScoreForCategory(marketData, j, caravanParty);
				}
			}
			num10 *= MathF.Max(0.1f, 1f - 2f * (caravanFullness - (hasNavalNavigationCapability ? 0.5f : 0.3f) * MathF.Min(num9, 1000f) / 1000f));
			num10 = MathF.Min(num10, (float)((int)(0.5f * (float)caravanParty.PartyTradeGold)));
			float num11 = ((caravanParty.IsCurrentlyUsedByAQuest && town.Settlement == caravanParty.HomeSettlement && caravanParty.Position.Distance(caravanParty.HomeSettlement.Position) < Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringTown * 5f) ? 0.1f : 1f);
			float num12 = 1f;
			float num13 = ((town.Security >= 75f) ? (1f + MathF.Clamp((town.Security - 75f) * 0.002f, 0f, 0.05f)) : 1f);
			float num14 = ((caravanParty.Owner != null) ? caravanParty.Owner.RandomFloat(1f, 1.03f) : 1f);
			float num15 = 1f;
			if (this.TradeAgreementsCampaignBehavior != null && caravanParty.MapFaction.IsKingdomFaction && town.MapFaction.IsKingdomFaction && this.TradeAgreementsCampaignBehavior.HasTradeAgreement((Kingdom)caravanParty.MapFaction, (Kingdom)town.MapFaction))
			{
				num15 = (hasNavalNavigationCapability ? 1.5f : 2f);
			}
			return (num9 + num10) * num6 * num15 * num7 * num11 * num12 * num13 * num14 * num4;
		}

		// Token: 0x06003AC3 RID: 15043 RVA: 0x000F482C File Offset: 0x000F2A2C
		private float CalculateTownSellScoreForCategory(MobileParty party, TownMarketData marketData, int i, float limitValue)
		{
			ItemRosterElement itemRosterElement = party.Party.ItemRoster[i];
			ItemCategory itemCategory = itemRosterElement.EquipmentElement.Item.ItemCategory;
			CaravansCampaignBehavior.PriceIndexData priceIndexData;
			this.GetCategoryPriceData(itemCategory, party, out priceIndexData);
			float num = marketData.GetPriceFactor(itemCategory) - priceIndexData.AverageBuySellPriceIndex * limitValue;
			if (num > 0f)
			{
				int num2 = ((itemRosterElement.EquipmentElement.Item.ItemCategory != DefaultItemCategories.PackAnimal || !party.HasLandNavigationCapability) ? itemRosterElement.Amount : MathF.Max(0, itemRosterElement.Amount - party.MemberRoster.TotalManCount));
				float num3 = ((itemCategory.Properties == ItemCategory.Property.BonusToFoodStores) ? 1.1f : 1f);
				return num * num3 * (float)MathF.Min(4000, itemRosterElement.EquipmentElement.Item.Value * num2);
			}
			return 0f;
		}

		// Token: 0x06003AC4 RID: 15044 RVA: 0x000F4916 File Offset: 0x000F2B16
		private void SetPlayerInteraction(MobileParty mobileParty, CaravansCampaignBehavior.PlayerInteraction interaction)
		{
			if (this._interactedCaravans.ContainsKey(mobileParty))
			{
				this._interactedCaravans[mobileParty] = interaction;
				return;
			}
			this._interactedCaravans.Add(mobileParty, interaction);
		}

		// Token: 0x06003AC5 RID: 15045 RVA: 0x000F4944 File Offset: 0x000F2B44
		private CaravansCampaignBehavior.PlayerInteraction GetPlayerInteraction(MobileParty mobileParty)
		{
			CaravansCampaignBehavior.PlayerInteraction result;
			if (this._interactedCaravans.TryGetValue(mobileParty, out result))
			{
				return result;
			}
			return CaravansCampaignBehavior.PlayerInteraction.None;
		}

		// Token: 0x06003AC6 RID: 15046 RVA: 0x000F4964 File Offset: 0x000F2B64
		private float CalculateTownBuyScoreForCategory(TownMarketData marketData, int categoryIndex, MobileParty mobileParty)
		{
			ItemCategory itemCategory = ItemCategories.All[categoryIndex];
			CaravansCampaignBehavior.PriceIndexData priceIndexData;
			this.GetCategoryPriceData(itemCategory, mobileParty, out priceIndexData);
			float priceFactor = marketData.GetPriceFactor(itemCategory);
			float num = priceIndexData.AverageBuySellPriceIndex / priceFactor;
			float num2 = num * num - 1.1f;
			if (num2 > 0f)
			{
				return MathF.Min(MathF.Sqrt(this._averageValuesCached[itemCategory]) * 3f * num2, 0.3f * (float)marketData.GetCategoryData(itemCategory).InStoreValue);
			}
			return 0f;
		}

		// Token: 0x06003AC7 RID: 15047 RVA: 0x000F49E0 File Offset: 0x000F2BE0
		private bool GetCategoryPriceData(ItemCategory category, MobileParty mobileParty, out CaravansCampaignBehavior.PriceIndexData priceIndex)
		{
			bool result = true;
			if (!(this.ShouldPartyUseCoastalPrices(mobileParty) ? this._coastalPriceDictionary : this._priceDictionary).TryGetValue(category, out priceIndex))
			{
				result = false;
				priceIndex = new CaravansCampaignBehavior.PriceIndexData(1f, 1f);
			}
			return result;
		}

		// Token: 0x06003AC8 RID: 15048 RVA: 0x000F4A28 File Offset: 0x000F2C28
		private void RefreshTotalValueOfItemsAtCategoryForParty(MobileParty caravanParty)
		{
			this._totalValueOfItemsAtCategory.Clear();
			for (int i = 0; i < caravanParty.ItemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = caravanParty.ItemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				int num = elementCopyAtIndex.Amount * (item.Value + 10);
				int num2;
				if (this._totalValueOfItemsAtCategory.TryGetValue(item.ItemCategory, out num2))
				{
					this._totalValueOfItemsAtCategory[item.ItemCategory] = num2 + num;
				}
				else
				{
					this._totalValueOfItemsAtCategory.Add(item.ItemCategory, num);
				}
			}
		}

		// Token: 0x06003AC9 RID: 15049 RVA: 0x000F4AC4 File Offset: 0x000F2CC4
		private bool ShouldPartyUseCoastalPrices(MobileParty mobileParty)
		{
			return mobileParty.IsCaravan && mobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability;
		}

		// Token: 0x06003ACA RID: 15050 RVA: 0x000F4ADC File Offset: 0x000F2CDC
		private void SellGoodsInternal(MobileParty mobileParty, Town town, bool sellHorses, List<ValueTuple<EquipmentElement, int>> soldItems, float priceIndexSellLimit = 1.1f, bool toLoseWeight = false)
		{
			int itemAverageWeight = Campaign.Current.Models.InventoryCapacityModel.GetItemAverageWeight();
			this.RefreshTotalValueOfItemsAtCategoryForParty(mobileParty);
			for (int i = mobileParty.ItemRoster.Count - 1; i >= 0; i--)
			{
				int num = (int)((float)mobileParty.ItemRoster.NumberOfPackAnimals - (float)mobileParty.Party.NumberOfAllMembers * 0.6f);
				int num2 = (int)((float)mobileParty.ItemRoster.NumberOfLivestockAnimals - (float)mobileParty.Party.NumberOfAllMembers * 0.6f);
				ItemRosterElement elementCopyAtIndex = mobileParty.ItemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				CaravansCampaignBehavior.PriceIndexData priceIndexData;
				if (this.GetCategoryPriceData(item.GetItemCategory(), mobileParty, out priceIndexData) && sellHorses == (item.HasHorseComponent || item.ItemCategory == DefaultItemCategories.PackAnimal) && (!toLoseWeight || !item.HasHorseComponent || !mobileParty.HasLandNavigationCapability))
				{
					bool flag = item.ItemCategory == DefaultItemCategories.PackAnimal;
					if (!flag || num > 0 || !mobileParty.HasLandNavigationCapability)
					{
						float priceFactor = town.MarketData.GetPriceFactor(item.ItemCategory);
						float num3 = priceFactor / priceIndexData.AverageBuySellPriceIndex;
						float num4 = priceIndexSellLimit - (Campaign.Current.GameStarted ? (MathF.Sqrt((float)MathF.Min(this._totalValueOfItemsAtCategory[item.ItemCategory], 5000) / 5000f) * 0.4f) : 0f);
						bool flag2 = num > 0 && flag;
						bool flag3 = num2 > 0 && item.HorseComponent != null && item.HorseComponent.IsLiveStock;
						if (num3 >= num4 || (mobileParty.HasLandNavigationCapability && (flag3 || flag2)))
						{
							float num5 = 0.8f * priceIndexData.AverageBuySellPriceIndex + 0.2f * priceIndexData.MinBuySellPriceIndex;
							if (priceFactor >= num5 * num4 || (mobileParty.HasLandNavigationCapability && (flag3 || flag2)))
							{
								float num6 = priceFactor - num5 * num4;
								float demand = town.MarketData.GetDemand(item.ItemCategory);
								float num7 = Campaign.Current.Models.SettlementEconomyModel.CalculateDailySettlementBudgetForItemCategory(town, demand, item.ItemCategory) + (float)(2 * item.Value);
								int itemPrice = town.GetItemPrice(item, mobileParty, true);
								float num8 = ((item.ItemCategory == DefaultItemCategories.PackAnimal) ? 1.5f : 1f);
								float num9 = (mobileParty.HasNavalNavigationCapability ? 5f : 3f);
								float num10 = num7 * num6 * num3 * num8 * num9;
								if (num10 > 0f || flag3 || flag2)
								{
									int num11 = MBRandom.RoundRandomized(num10 / (float)itemPrice);
									if (mobileParty.HasLandNavigationCapability)
									{
										if (flag2)
										{
											num11 = num;
										}
										else if (flag3)
										{
											num11 = num2;
										}
									}
									int amount = elementCopyAtIndex.Amount;
									if (num11 > amount)
									{
										num11 = amount;
									}
									if (num11 * itemPrice > town.Gold)
									{
										num11 = town.Gold / itemPrice;
									}
									if (toLoseWeight && mobileParty.TotalWeightCarried - (float)(num11 * itemAverageWeight) < (float)mobileParty.InventoryCapacity)
									{
										num11 = (int)((mobileParty.TotalWeightCarried - (float)mobileParty.InventoryCapacity) / (float)itemAverageWeight + 0.99f);
									}
									if (num11 > elementCopyAtIndex.Amount)
									{
										num11 = elementCopyAtIndex.Amount;
									}
									if (num11 * itemPrice > town.Gold)
									{
										num11 = town.Gold / itemPrice;
									}
									if (num11 > 0)
									{
										soldItems.Add(new ValueTuple<EquipmentElement, int>(elementCopyAtIndex.EquipmentElement, num11));
										if (Campaign.Current.GameStarted)
										{
											this.OnSellItems(mobileParty, elementCopyAtIndex, town);
										}
										SellItemsAction.Apply(mobileParty.Party, town.Owner, elementCopyAtIndex, num11, town.Owner.Settlement);
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06003ACB RID: 15051 RVA: 0x000F4E80 File Offset: 0x000F3080
		private void SellGoods(MobileParty mobileParty, Town town, float priceIndexSellLimit = 1.1f, bool toLoseWeight = false)
		{
			this.RefreshTotalValueOfItemsAtCategoryForParty(mobileParty);
			List<ValueTuple<EquipmentElement, int>> list = new List<ValueTuple<EquipmentElement, int>>();
			this.SellGoodsInternal(mobileParty, town, false, list, priceIndexSellLimit, toLoseWeight);
			this.SellGoodsInternal(mobileParty, town, true, list, priceIndexSellLimit, toLoseWeight);
			if (!list.IsEmpty<ValueTuple<EquipmentElement, int>>() && mobileParty.IsCaravan)
			{
				CampaignEventDispatcher.Instance.OnCaravanTransactionCompleted(mobileParty, town, list);
			}
		}

		// Token: 0x06003ACC RID: 15052 RVA: 0x000F4ED4 File Offset: 0x000F30D4
		private void OnSellItems(MobileParty caravanParty, ItemRosterElement rosterElement, Town soldTown)
		{
			int itemPrice = soldTown.GetItemPrice(rosterElement.EquipmentElement.Item, caravanParty, true);
			List<CaravansCampaignBehavior.TradeActionLog> list;
			if (this._tradeActionLogs.TryGetValue(caravanParty, out list))
			{
				foreach (CaravansCampaignBehavior.TradeActionLog tradeActionLog in list)
				{
					if (tradeActionLog.ItemRosterElement.EquipmentElement.Item == rosterElement.EquipmentElement.Item && itemPrice > tradeActionLog.SellPrice)
					{
						tradeActionLog.OnSellAction(soldTown.Settlement, itemPrice);
					}
				}
			}
		}

		// Token: 0x06003ACD RID: 15053 RVA: 0x000F4F84 File Offset: 0x000F3184
		private void BuyGoods(MobileParty caravanParty, Town town)
		{
			CaravansCampaignBehavior.<>c__DisplayClass80_0 CS$<>8__locals1 = new CaravansCampaignBehavior.<>c__DisplayClass80_0();
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.town = town;
			CS$<>8__locals1.caravanParty = caravanParty;
			List<ValueTuple<EquipmentElement, int>> list = new List<ValueTuple<EquipmentElement, int>>();
			float capacityFactor = this.CalculateCapacityFactor(CS$<>8__locals1.caravanParty);
			float budgetFactor = this.CalculateBudgetFactor(CS$<>8__locals1.caravanParty);
			this.RefreshTotalValueOfItemsAtCategoryForParty(CS$<>8__locals1.caravanParty);
			MBList<ItemCategory> mblist = (from x in ItemCategories.All
				orderby CS$<>8__locals1.<>4__this.CalculateBuyValue(x, CS$<>8__locals1.town, CS$<>8__locals1.caravanParty, budgetFactor, capacityFactor) descending
				select x).ToMBList<ItemCategory>();
			int num = (CS$<>8__locals1.caravanParty.HasNavalNavigationCapability ? 10 : 5);
			for (int i = 0; i < num; i++)
			{
				this.BuyCategory(CS$<>8__locals1.caravanParty, CS$<>8__locals1.town, mblist[i], budgetFactor, capacityFactor, list);
			}
			if (CS$<>8__locals1.caravanParty.HasNavalNavigationCapability)
			{
				this.BuyCategory(CS$<>8__locals1.caravanParty, CS$<>8__locals1.town, DefaultItemCategories.Grain, budgetFactor, capacityFactor, list);
				this.BuyCategory(CS$<>8__locals1.caravanParty, CS$<>8__locals1.town, DefaultItemCategories.Fish, budgetFactor, capacityFactor, list);
			}
			else if ((float)(CS$<>8__locals1.caravanParty.ItemRoster.NumberOfPackAnimals + CS$<>8__locals1.caravanParty.ItemRoster.NumberOfLivestockAnimals) < (float)CS$<>8__locals1.caravanParty.Party.NumberOfAllMembers * 2f && CS$<>8__locals1.caravanParty.ItemRoster.NumberOfPackAnimals < CS$<>8__locals1.caravanParty.Party.NumberOfAllMembers && this._packAnimalCategoryIndex >= 0 && CS$<>8__locals1.caravanParty.PartyTradeGold > 1000)
			{
				this.BuyCategory(CS$<>8__locals1.caravanParty, CS$<>8__locals1.town, DefaultItemCategories.PackAnimal, budgetFactor, capacityFactor, list);
			}
			if (!list.IsEmpty<ValueTuple<EquipmentElement, int>>())
			{
				CampaignEventDispatcher.Instance.OnCaravanTransactionCompleted(CS$<>8__locals1.caravanParty, CS$<>8__locals1.town, list);
			}
		}

		// Token: 0x06003ACE RID: 15054 RVA: 0x000F51D5 File Offset: 0x000F33D5
		private float CalculateBudgetFactor(MobileParty caravanParty)
		{
			return 0.1f + MathF.Clamp((float)caravanParty.PartyTradeGold / 5000f, 0f, 1f);
		}

		// Token: 0x06003ACF RID: 15055 RVA: 0x000F51FC File Offset: 0x000F33FC
		private float CalculateCapacityFactor(MobileParty caravanParty)
		{
			float num = caravanParty.TotalWeightCarried / ((float)caravanParty.InventoryCapacity + 1f);
			num *= 0.9f;
			return 1.1f - MathF.Clamp(num, 0f, 1f);
		}

		// Token: 0x06003AD0 RID: 15056 RVA: 0x000F523C File Offset: 0x000F343C
		private void BuyCategory(MobileParty caravanParty, Town town, ItemCategory category, float budgetFactor, float capacityFactor, List<ValueTuple<EquipmentElement, int>> boughtItems)
		{
			float num = this.CalculateBuyValue(category, town, caravanParty, budgetFactor, capacityFactor);
			if (num < 7f)
			{
				return;
			}
			if (caravanParty.TotalWeightCarried / (float)caravanParty.InventoryCapacity > 0.9f && !category.IsAnimal)
			{
				return;
			}
			if (town.MarketData.GetCategoryData(category).InStore == 0)
			{
				return;
			}
			float num2 = MathF.Min(MathF.Min((float)caravanParty.PartyTradeGold * 0.5f, num * 1.5f), (float)Campaign.Current.Models.CaravanModel.GetMaxGoldToSpendOnOneItemCategory(caravanParty, category));
			if (!Campaign.Current.GameStarted)
			{
				num2 *= 0.5f;
			}
			float num3 = num2;
			int num4 = 0;
			Predicate<ItemObject> <>9__0;
			for (;;)
			{
				int num5 = 0;
				int x2 = (int)(MBRandom.RandomFloat * (float)town.Owner.ItemRoster.Count);
				ItemRoster itemRoster = town.Owner.ItemRoster;
				Predicate<ItemObject> predicate;
				if ((predicate = <>9__0) == null)
				{
					predicate = (<>9__0 = (ItemObject x) => x.ItemCategory == category);
				}
				int num6 = itemRoster.FindIndexFirstAfterXthElement(predicate, x2);
				if (num6 < 0)
				{
					break;
				}
				ItemRosterElement rosterElement = town.Owner.ItemRoster.GetElementCopyAtIndex(num6);
				ItemObject item = rosterElement.EquipmentElement.Item;
				int itemPrice = town.GetItemPrice(item, caravanParty, false);
				int num7 = MBRandom.RoundRandomized(num3 / (float)itemPrice);
				if (num7 > rosterElement.Amount)
				{
					num7 = rosterElement.Amount;
				}
				if (num7 > this.MaxNumberOfItemsToBuyFromSingleCategory)
				{
					num7 = this.MaxNumberOfItemsToBuyFromSingleCategory;
				}
				if ((!category.IsAnimal || !caravanParty.HasLandNavigationCapability) && caravanParty.TotalWeightCarried + (float)num7 * item.Weight > (float)caravanParty.InventoryCapacity)
				{
					num7 = (int)(((float)caravanParty.InventoryCapacity * 0.9f - caravanParty.TotalWeightCarried) / item.Weight);
				}
				if (caravanParty.HasLandNavigationCapability && rosterElement.EquipmentElement.Item.HorseComponent != null && (rosterElement.EquipmentElement.Item.HorseComponent.IsLiveStock || rosterElement.EquipmentElement.Item.HorseComponent.IsPackAnimal))
				{
					int numberOfPackAnimals = caravanParty.ItemRoster.NumberOfPackAnimals;
					int numberOfLivestockAnimals = caravanParty.ItemRoster.NumberOfLivestockAnimals;
					if (rosterElement.EquipmentElement.Item.HorseComponent.IsLiveStock && (float)(numberOfLivestockAnimals + num7) > (float)caravanParty.Party.NumberOfAllMembers * 0.6f)
					{
						num7 = (int)((float)caravanParty.Party.NumberOfAllMembers * 0.6f) - numberOfLivestockAnimals;
					}
					else if (rosterElement.EquipmentElement.Item.HorseComponent.IsPackAnimal && numberOfPackAnimals + num7 > caravanParty.Party.NumberOfAllMembers)
					{
						num7 = caravanParty.Party.NumberOfAllMembers - numberOfPackAnimals;
					}
				}
				if (num7 > 0)
				{
					SellItemsAction.Apply(town.Owner, caravanParty.Party, rosterElement, num7, town.Owner.Settlement);
					int num8 = boughtItems.FindIndex((ValueTuple<EquipmentElement, int> x) => x.Item1.IsEqualTo(rosterElement.EquipmentElement));
					if (num8 == -1)
					{
						boughtItems.Add(new ValueTuple<EquipmentElement, int>(rosterElement.EquipmentElement, -num7));
					}
					else
					{
						boughtItems[num8] = new ValueTuple<EquipmentElement, int>(rosterElement.EquipmentElement, -num7 + boughtItems[num8].Item2);
					}
					num4 += num7;
					num3 -= (float)(num7 * itemPrice + 1);
					num5 = num7 * itemPrice;
					Town destinationForMobileParty = this.GetDestinationForMobileParty(caravanParty);
					if (caravanParty.LastVisitedSettlement != null && destinationForMobileParty != null && Campaign.Current.GameStarted)
					{
						List<CaravansCampaignBehavior.TradeActionLog> list;
						if (!this._tradeActionLogs.TryGetValue(caravanParty, out list))
						{
							list = new List<CaravansCampaignBehavior.TradeActionLog>();
							this._tradeActionLogs.Add(caravanParty, list);
						}
						int itemPrice2 = town.GetItemPrice(rosterElement.EquipmentElement, caravanParty, false);
						list.Add(this._tradeActionLogPool.CreateNewLog(town.Settlement, itemPrice2, rosterElement));
					}
				}
				if (num3 <= 0f || num5 <= 0 || num4 >= Campaign.Current.Models.CaravanModel.MaxNumberOfItemsToBuyFromSingleCategory)
				{
					return;
				}
			}
		}

		// Token: 0x06003AD1 RID: 15057 RVA: 0x000F5698 File Offset: 0x000F3898
		private int CaravanTotalValue(MobileParty caravanParty)
		{
			float num = 0f;
			for (int i = 0; i < caravanParty.ItemRoster.Count; i++)
			{
				ItemRosterElement itemRosterElement = caravanParty.ItemRoster[i];
				num += this.GetGlobalItemSellPrice(itemRosterElement.EquipmentElement.Item, caravanParty) * (float)itemRosterElement.Amount;
			}
			return (int)num + caravanParty.PartyTradeGold;
		}

		// Token: 0x06003AD2 RID: 15058 RVA: 0x000F56FC File Offset: 0x000F38FC
		private float CalculateBuyValue(ItemCategory category, Town town, MobileParty caravanParty, float budgetFactor, float capacityFactor)
		{
			if (!category.IsTradeGood && !category.IsAnimal)
			{
				return 0f;
			}
			CaravansCampaignBehavior.PriceIndexData priceIndexData;
			if (!this.GetCategoryPriceData(category, caravanParty, out priceIndexData))
			{
				return 0f;
			}
			if (town.MarketData.GetItemCountOfCategory(category) == 0)
			{
				return 0f;
			}
			float num = 0f;
			if (Campaign.Current.GameStarted && this._totalValueOfItemsAtCategory.ContainsKey(category))
			{
				num = MathF.Sqrt((float)MathF.Min(this._totalValueOfItemsAtCategory[category], 5000) / 5000f) * 0.4f;
			}
			float itemCategoryPriceIndex = town.GetItemCategoryPriceIndex(category);
			float averageBuySellPriceIndex = priceIndexData.AverageBuySellPriceIndex;
			float num2 = averageBuySellPriceIndex * (1f - num) - itemCategoryPriceIndex;
			if (num2 < 0f)
			{
				return 0f;
			}
			float demand = town.MarketData.GetDemand(category);
			float num3 = 0.1f * MathF.Pow(demand, 0.5f);
			float num4 = num2 * this._averageValuesCached[category];
			float num5 = num2 * 200f;
			float num6 = averageBuySellPriceIndex / itemCategoryPriceIndex;
			float num7 = ((category.Properties == ItemCategory.Property.BonusToFoodStores) ? 1.1f : 1f);
			return ((category == DefaultItemCategories.PackAnimal && caravanParty.HasLandNavigationCapability) ? 1.5f : 1f) * num7 * num6 * num3 * (num4 * budgetFactor + num5 * capacityFactor);
		}

		// Token: 0x06003AD3 RID: 15059 RVA: 0x000F5844 File Offset: 0x000F3A44
		private float GetGlobalItemSellPrice(ItemObject item, MobileParty mobileParty)
		{
			CaravansCampaignBehavior.PriceIndexData priceIndexData;
			if (!this.GetCategoryPriceData(item.ItemCategory, mobileParty, out priceIndexData))
			{
				return 1f;
			}
			return priceIndexData.AverageBuySellPriceIndex * (float)item.Value;
		}

		// Token: 0x06003AD4 RID: 15060 RVA: 0x000F5878 File Offset: 0x000F3A78
		private List<Kingdom> GetSuitableKingdomsAsTradePartnerForPlayerCaravans()
		{
			List<Kingdom> list = new List<Kingdom>();
			foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
			{
				if (!kingdom.IsEliminated)
				{
					list.Add(kingdom);
				}
			}
			return list;
		}

		// Token: 0x06003AD5 RID: 15061 RVA: 0x000F58E0 File Offset: 0x000F3AE0
		private List<Kingdom> GetSuitableKingdomsForHomeSettlement()
		{
			List<Kingdom> list = new List<Kingdom>();
			foreach (Kingdom kingdom in this.GetSuitableKingdomsAsTradePartnerForPlayerCaravans())
			{
				if (this.GetSuitableHomeSettlementsForKingdom(kingdom).Count > 0)
				{
					list.Add(kingdom);
				}
			}
			return list;
		}

		// Token: 0x06003AD6 RID: 15062 RVA: 0x000F594C File Offset: 0x000F3B4C
		private List<Settlement> GetSuitableHomeSettlementsForKingdom(Kingdom kingdom)
		{
			List<Settlement> list = new List<Settlement>();
			MobileParty conversationParty = MobileParty.ConversationParty;
			bool hasNavalNavigationCapability = conversationParty.HasNavalNavigationCapability;
			foreach (Settlement settlement in kingdom.Settlements)
			{
				if (settlement.IsTown && settlement != conversationParty.HomeSettlement && (!hasNavalNavigationCapability || settlement.HasPort))
				{
					list.Add(settlement);
				}
			}
			return list;
		}

		// Token: 0x06003AD7 RID: 15063 RVA: 0x000F59D4 File Offset: 0x000F3BD4
		protected void AddDialogs(CampaignGameStarter starter)
		{
			starter.AddPlayerLine("caravan_companion_talk_start", "hero_main_options", "caravan_companion_talk_start", "{=q0RY0dQG}We need to talk business.", new ConversationSentence.OnConditionDelegate(this.companion_is_caravan_leader_on_condition), null, 100, null, null);
			starter.AddDialogLine("caravan_companion_talk_start_reply", "caravan_companion_talk_start", "caravan_companion_talk_start_reply", "{=9RiXgPc1}Certainly. What do you need to know?", null, null, 100, null);
			starter.AddPlayerLine("caravan_companion_change_home_settlement", "caravan_companion_talk_start_reply", "caravan_companion_ask_change_home_settlement", "{=dMQ1u6l2}I would like you to start trading out of a different town.", new ConversationSentence.OnConditionDelegate(this.caravan_companion_change_home_settlement_on_condition), new ConversationSentence.OnConsequenceDelegate(this.caravan_companion_change_home_settlement_on_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(this.caravan_companion_change_home_settlement_clickable_condition), null);
			starter.AddPlayerLine("caravan_companion_prohibit_kingdoms", "caravan_companion_talk_start_reply", "caravan_companion_prohibit_kingdoms_selected", "{=5LhfbFpX}Let's discuss our trade partners.", new ConversationSentence.OnConditionDelegate(this.caravan_companion_prohibit_kingdoms_on_condition), new ConversationSentence.OnConsequenceDelegate(this.caravan_companion_prohibit_kingdoms_on_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(this.caravan_companion_prohibit_kingdoms_clickable_condition), null);
			starter.AddPlayerLine("caravan_companion_trade_rumors", "caravan_companion_talk_start_reply", "caravan_companion_ask_trade_rumors", "{=oMuxr3X6}What news of the markets? Any good deals to be had?", null, null, 100, null, null);
			starter.AddDialogLine("caravan_companion_ask_trade_rumors", "caravan_companion_ask_trade_rumors", "caravan_companion_anything_else", "{=sC4ZLZ8x}{COMMENT}", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_ask_trade_rumors_on_consequence), 100, null);
			starter.AddDialogLine("caravan_companion_talk_player_thank", "caravan_companion_anything_else", "caravan_companion_talk_end", "{=DQBaaC0e}Is there anything else?", null, null, 100, null);
			starter.AddPlayerLine("caravan_companion_talk_not_leave", "caravan_companion_talk_end", "lord_pretalk", "{=i2FwKPmC}Yes, I wanted to talk about something else..", null, null, 100, null, null);
			starter.AddPlayerLine("caravan_companion_talk_leave", "caravan_companion_talk_end", "close_window", "{=1IJouNaM}Carry on, then. Farewell.", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_player_leave_encounter_on_consequence), 100, null, null);
			starter.AddPlayerLine("caravan_companion_nevermind", "caravan_companion_talk_start_reply", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			starter.AddDialogLine("caravan_companion_ask_change_home_settlement", "caravan_companion_ask_change_home_settlement", "caravan_companion_ask_change_home_settlement_2", "{=8oJYCDbO}Certainly. We currently trade out of {HOME_SETTLEMENT}. In which realm shall we make our new home?", new ConversationSentence.OnConditionDelegate(this.caravan_companion_ask_change_home_settlement_2_on_condition), null, 100, null);
			starter.AddRepeatablePlayerLine("caravan_companion_ask_change_home_settlement_2", "caravan_companion_ask_change_home_settlement_2", "caravan_companion_ask_change_home_settlement_3", "{=!}{KINGDOM_NAME}", "{=bKqka5Uj}I am thinking of a different realm.", "caravan_companion_ask_change_home_settlement", new ConversationSentence.OnConditionDelegate(this.caravan_companion_ask_change_home_settlement_3_on_condition), new ConversationSentence.OnConsequenceDelegate(this.caravan_companion_ask_change_home_settlement_3_on_consequence), 100, null);
			starter.AddDialogLine("caravan_companion_ask_change_home_settlement_3", "caravan_companion_ask_change_home_settlement_3", "caravan_companion_ask_change_home_settlement_4", "{=SuQSy6g2}And which town there did you have in mind?", null, null, 100, null);
			starter.AddRepeatablePlayerLine("caravan_companion_ask_change_home_settlement_4", "caravan_companion_ask_change_home_settlement_4", "caravan_companion_change_home_settlement_end", "{=!}{SETTLEMENT.NAME}", "{=aE4JAqn0}I am thinking of a different settlement.", "caravan_companion_ask_change_home_settlement_3", new ConversationSentence.OnConditionDelegate(this.caravan_companion_ask_change_home_settlement_4_on_condition), new ConversationSentence.OnConsequenceDelegate(this.caravan_companion_ask_change_home_settlement_4_on_consequence), 100, null);
			starter.AddPlayerLine("caravan_companion_ask_change_home_settlement_cancel", "caravan_companion_ask_change_home_settlement_2", "lord_pretalk", "{=PznWhAdU}Actually, never mind.", null, null, 100, null, null);
			starter.AddPlayerLine("caravan_companion_ask_change_home_settlement_cancel_2", "caravan_companion_ask_change_home_settlement_4", "lord_pretalk", "{=PznWhAdU}Actually, never mind.", null, null, 100, null, null);
			starter.AddDialogLine("caravan_companion_change_home_settlement_end", "caravan_companion_change_home_settlement_end", "caravan_companion_anything_else", "{=cWwYmaw7}Understood. {NEW_HOME_SETTLEMENT_LINE}", new ConversationSentence.OnConditionDelegate(this.caravan_companion_change_home_settlement_end_on_condition), null, 100, null);
			starter.AddDialogLine("caravan_companion_prohibit_kingdoms_selected", "caravan_companion_prohibit_kingdoms_selected", "caravan_companion_prohibit_kingdoms_selected_2", "{=cK0yctTy}Certainly. {DESPITE_WAR}", null, null, 100, null);
			starter.AddRepeatablePlayerLine("caravan_companion_prohibit_kingdoms_selected_2", "caravan_companion_prohibit_kingdoms_selected_2", "caravan_companion_prohibit_kingdoms_selected", "{=!}{CONTINUE_OR_STOP_TRADE}", "{=bKqka5Uj}I am thinking of a different realm.", "caravan_companion_prohibit_kingdoms_selected", new ConversationSentence.OnConditionDelegate(this.caravan_companion_prohibit_kingdoms_selected_2_on_condition), new ConversationSentence.OnConsequenceDelegate(this.caravan_companion_prohibit_kingdoms_selected_2_on_consequence), 100, null);
			starter.AddPlayerLine("caravan_companion_prohibit_kingdoms_selected_cancel", "caravan_companion_prohibit_kingdoms_selected_2", "lord_pretalk", "{=FM7YZaOa}Alright, that is all.", null, null, 100, null, null);
			starter.AddDialogLine("player_caravan_talk_start", "start", "player_caravan_talk_start", "{=BsVXQEhj}How may I help you?", new ConversationSentence.OnConditionDelegate(this.player_caravan_talk_start_on_condition), null, 100, null);
			starter.AddPlayerLine("player_caravan_trade_rumors", "player_caravan_talk_start", "player_caravan_ask_trade_rumors", "{=shNl2Npf}What news of the markets?", null, null, 100, null, null);
			starter.AddDialogLine("player_caravan_ask_trade_rumors", "player_caravan_ask_trade_rumors", "player_caravan_anything_else", "{=sC4ZLZ8x}{COMMENT}", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_ask_trade_rumors_on_consequence), 100, null);
			starter.AddDialogLine("player_caravan_talk_player_thank", "player_caravan_anything_else", "player_caravan_talk_end", "{=DQBaaC0e}Is there anything else?", null, null, 100, null);
			starter.AddPlayerLine("player_caravan_talk_not_leave", "player_caravan_talk_end", "start", "{=i2FwKPmC}Yes, I wanted to talk about something else..", null, null, 100, null, null);
			starter.AddPlayerLine("player_caravan_talk_leave", "player_caravan_talk_end", "close_window", "{=1IJouNaM}Carry on, then. Farewell.", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_player_leave_encounter_on_consequence), 100, null, null);
			starter.AddPlayerLine("player_caravan_nevermind", "player_caravan_talk_start", "close_window", "{=D33fIGQe}Never mind.", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_player_leave_encounter_on_consequence), 100, null, null);
			starter.AddDialogLine("caravan_hero_leader_talk_start", "start", "caravan_talk", "{=!}{CARAVAN_GREETING}", new ConversationSentence.OnConditionDelegate(this.caravan_start_talk_on_condition), null, 100, null);
			starter.AddDialogLine("caravan_pretalk", "caravan_pretalk", "caravan_talk", "{=3cBfSJOI}Is there anything else?[ib:normal]", null, null, 100, null);
			starter.AddPlayerLine("caravan_buy_products", "caravan_talk", "caravan_player_trade", "{=t0UGXPV4}I'm interested in trading. What kind of products do you have?", new ConversationSentence.OnConditionDelegate(this.caravan_buy_products_on_condition), null, 100, null, null);
			starter.AddPlayerLine("caravan_trade_rumors", "caravan_talk", "caravan_ask_trade_rumors", "{=b5Ucatkb}Tell me about your journeys. What news of the markets?", new ConversationSentence.OnConditionDelegate(this.caravan_ask_trade_rumors_on_condition), null, 100, null, null);
			starter.AddDialogLine("caravan_ask_trade_rumors", "caravan_ask_trade_rumors", "caravan_trade_rumors_player_answer", "{=sC4ZLZ8x}{COMMENT}", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_ask_trade_rumors_on_consequence), 100, null);
			starter.AddPlayerLine("caravan_trade_rumors_player_answer", "caravan_trade_rumors_player_answer", "caravan_talk_player_thank", "{=ha7EmrU9}Thank you for that information.", null, null, 100, null, null);
			starter.AddDialogLine("caravan_talk_player_thank", "caravan_talk_player_thank", "caravan_talk", "{=BQuVWKvq}You're welcome. Is there anything we need to discuss?", null, null, 100, null);
			starter.AddPlayerLine("caravan_loot", "caravan_talk", "caravan_loot_talk", "{=WOBy5UfY}Hand over your goods, or die!", new ConversationSentence.OnConditionDelegate(this.caravan_loot_on_condition), null, 100, new ConversationSentence.OnClickableConditionDelegate(this.caravan_loot_on_clickable_condition), null);
			starter.AddPlayerLine("caravan_talk_leave", "caravan_talk", "close_window", "{=1IJouNaM}Carry on, then. Farewell.", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_talk_leave_on_consequence), 100, null, null);
			starter.AddDialogLine("caravan_player_trade_end", "caravan_player_trade", "caravan_pretalk", "{=tlLDHAIu}Very well. A pleasure doing business with you.[rf:convo_relaxed_happy][ib:demure]", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_player_trade_end_on_condition), null, 100, null);
			starter.AddDialogLine("caravan_player_trade_end_response", "caravan_player_trade_response", "close_window", "{=2g2FhKb5}Farewell.", null, null, 100, null);
			starter.AddDialogLine("caravan_fight", "caravan_loot_talk", "caravan_do_not_bribe", "{=!}{CARAVAN_DEFIANCE}", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_not_bribe_on_condition), null, 100, null);
			starter.AddPlayerLine("player_decided_to_fight", "caravan_do_not_bribe", "close_window", "{=EhxS7NQ4}So be it. Attack!", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_caravan_fight_on_consequence), 100, null, null);
			starter.AddPlayerLine("player_decided_to_not_fight_1", "caravan_do_not_bribe", "close_window", "{=bfPsE9M1}You must have misunderstood me. Go in peace.", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_talk_leave_on_consequence), 100, null, null);
			starter.AddDialogLine("caravan_accepted_to_give_some_goods", "caravan_loot_talk", "caravan_give_some_goods", "{=dMc3SjOK}We can pay you. {TAKE_MONEY_AND_PRODUCT_STRING}[rf:idle_angry][ib:nervous]", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_give_goods_on_condition), null, 100, null);
			starter.AddPlayerLine("player_decided_to_take_some_goods", "caravan_give_some_goods", "caravan_end_talk_bribe", "{=0Pd84h4W}I'll accept that.", null, null, 100, delegate(out TextObject explanation)
			{
				explanation = new TextObject("{=nbgyyif6}This action may start a war.", null);
				return true;
			}, null);
			starter.AddPlayerLine("player_decided_to_take_everything", "caravan_give_some_goods", "player_wants_everything", "{=QZ6IcCIm}I want everything you've got.", null, null, 100, delegate(out TextObject explanation)
			{
				explanation = new TextObject("{=nbgyyif6}This action may start a war.", null);
				return true;
			}, null);
			starter.AddPlayerLine("player_decided_to_not_fight_2", "caravan_give_some_goods", "close_window", "{=bfPsE9M1}You must have misunderstood me. Go in peace.", null, new ConversationSentence.OnConsequenceDelegate(this.caravan_talk_leave_on_consequence), 100, null, null);
			starter.AddDialogLine("caravan_fight_no_surrender", "player_wants_everything", "close_window", "{=3JfCwL31}You will have to fight us first![rf:idle_angry][ib:aggressive]", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_not_surrender_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_caravan_fight_on_consequence), 100, null);
			starter.AddDialogLine("caravan_accepted_to_give_everything", "player_wants_everything", "player_decision_to_take_prisoners", "{=hbtbSag8}We can't fight you. We surrender. Please don't hurt us. Take what you want.[if:idle_angry][ib:nervous]", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_give_goods_on_condition), null, 100, null);
			starter.AddPlayerLine("player_do_not_take_prisoners", "player_decision_to_take_prisoners", "caravan_end_talk_surrender", "{=6kaia5qP}Give me all your wares!", null, null, 100, delegate(out TextObject explanation)
			{
				explanation = new TextObject("{=nbgyyif6}This action may start a war.", null);
				return true;
			}, null);
			starter.AddPlayerLine("player_decided_to_take_prisoner", "player_decision_to_take_prisoners", "caravan_taken_prisoner_warning_check", "{=1gv0AVUN}You are my prisoners now.", null, null, 100, delegate(out TextObject explanation)
			{
				explanation = new TextObject("{=1LlH1Jof}This action will start a war.", null);
				return true;
			}, null);
			starter.AddPlayerLine("player_decided_to_force_fight", "player_decision_to_take_prisoners", "caravan_force_start_encounter", "{=ha53qb7v}Don't bother pleading for your lives. At them, lads!", null, null, 100, delegate(out TextObject explanation)
			{
				explanation = new TextObject("{=1LlH1Jof}This action will start a war.", null);
				return true;
			}, null);
			starter.AddDialogLine("caravan_force_fight_encounter", "caravan_force_start_encounter", "close_window", "{=yoWl6w1I}Heaven will avenge us, you butcher!", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_caravan_fight_forced_on_consequence), 100, null);
			starter.AddDialogLine("caravan_warn_player_to_take_prisoner", "caravan_taken_prisoner_warning_check", "caravan_taken_prisoner_warning_answer", "{=NuYzgBZB}You are going too far. The {KINGDOM} won't stand for the destruction of its caravans.", new ConversationSentence.OnConditionDelegate(this.conversation_warn_player_on_condition), null, 100, null);
			starter.AddDialogLine("caravan_do_not_warn_player", "caravan_taken_prisoner_warning_check", "close_window", "{=BvytaDUJ}Heaven protect us from the likes of you.", null, delegate()
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += this.player_take_prisoner_consequence;
			}, 100, null);
			starter.AddPlayerLine("player_decided_to_take_prisoner_continue", "caravan_taken_prisoner_warning_answer", "close_window", "{=WVkc4UgX}Continue.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_caravan_took_prisoner_on_consequence), 100, null, null);
			starter.AddPlayerLine("player_decided_to_take_prisoner_leave", "caravan_taken_prisoner_warning_answer", "caravan_loot_talk", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			starter.AddDialogLine("caravan_bribery_leave", "caravan_end_talk_bribe", "close_window", "{=uPwKhAps}Can we leave now?", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_looted_leave_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_caravan_looted_leave_on_consequence), 100, null);
			starter.AddDialogLine("caravan_surrender_leave", "caravan_end_talk_surrender", "close_window", "{=uPwKhAps}Can we leave now?", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_looted_leave_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_caravan_surrender_leave_on_consequence), 100, null);
		}

		// Token: 0x06003AD8 RID: 15064 RVA: 0x000F63C1 File Offset: 0x000F45C1
		private bool caravan_companion_change_home_settlement_on_condition()
		{
			return !MobileParty.ConversationParty.IsCurrentlyUsedByAQuest;
		}

		// Token: 0x06003AD9 RID: 15065 RVA: 0x000F63D2 File Offset: 0x000F45D2
		private bool caravan_companion_change_home_settlement_clickable_condition(out TextObject explanation)
		{
			if (this.GetSuitableKingdomsForHomeSettlement().Count == 0)
			{
				explanation = new TextObject("{=MQM5SxdI}There is no suitable kingdom to select a new home settlement from.", null);
				return false;
			}
			explanation = null;
			return true;
		}

		// Token: 0x06003ADA RID: 15066 RVA: 0x000F63F4 File Offset: 0x000F45F4
		private void caravan_companion_change_home_settlement_on_consequence()
		{
			ConversationSentence.SetObjectsToRepeatOver(this.GetSuitableKingdomsForHomeSettlement(), 5);
		}

		// Token: 0x06003ADB RID: 15067 RVA: 0x000F6404 File Offset: 0x000F4604
		private bool caravan_companion_ask_change_home_settlement_2_on_condition()
		{
			MobileParty conversationParty = MobileParty.ConversationParty;
			string variableName = "HOME_SETTLEMENT";
			Settlement homeSettlement = conversationParty.HomeSettlement;
			MBTextManager.SetTextVariable(variableName, (homeSettlement != null) ? homeSettlement.Name : null, false);
			return true;
		}

		// Token: 0x06003ADC RID: 15068 RVA: 0x000F6438 File Offset: 0x000F4638
		private bool caravan_companion_ask_change_home_settlement_3_on_condition()
		{
			Kingdom kingdom = ConversationSentence.CurrentProcessedRepeatObject as Kingdom;
			if (kingdom != null)
			{
				ConversationSentence.SelectedRepeatLine.SetTextVariable("KINGDOM_NAME", kingdom.Name);
				return true;
			}
			return false;
		}

		// Token: 0x06003ADD RID: 15069 RVA: 0x000F646C File Offset: 0x000F466C
		private void caravan_companion_ask_change_home_settlement_3_on_consequence()
		{
			Kingdom kingdom = ConversationSentence.SelectedRepeatObject as Kingdom;
			ConversationSentence.SetObjectsToRepeatOver(this.GetSuitableHomeSettlementsForKingdom(kingdom), 5);
		}

		// Token: 0x06003ADE RID: 15070 RVA: 0x000F6494 File Offset: 0x000F4694
		private bool caravan_companion_ask_change_home_settlement_4_on_condition()
		{
			Settlement settlement = ConversationSentence.CurrentProcessedRepeatObject as Settlement;
			if (settlement != null)
			{
				StringHelpers.SetSettlementProperties("SETTLEMENT", settlement, null, true);
				return true;
			}
			return false;
		}

		// Token: 0x06003ADF RID: 15071 RVA: 0x000F64C0 File Offset: 0x000F46C0
		private void caravan_companion_ask_change_home_settlement_4_on_consequence()
		{
			Settlement settlement = ConversationSentence.SelectedRepeatObject as Settlement;
			StringHelpers.SetSettlementProperties("SETTLEMENT", settlement, null, false);
			MobileParty.ConversationParty.CaravanPartyComponent.ChangeHomeSettlement(settlement);
		}

		// Token: 0x06003AE0 RID: 15072 RVA: 0x000F64F8 File Offset: 0x000F46F8
		private bool caravan_companion_change_home_settlement_end_on_condition()
		{
			MobileParty conversationParty = MobileParty.ConversationParty;
			TextObject textObject = TextObject.GetEmpty();
			if (conversationParty.HomeSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				textObject = new TextObject("{=Qyqurdka}Although it is inside enemy territory, our new home is {SETTLEMENT_NAME}.", null);
			}
			else
			{
				textObject = new TextObject("{=JIPKLzsx}Our new home is {SETTLEMENT_NAME}.", null);
			}
			textObject.SetTextVariable("SETTLEMENT_NAME", conversationParty.HomeSettlement.Name);
			MBTextManager.SetTextVariable("NEW_HOME_SETTLEMENT_LINE", textObject, false);
			return true;
		}

		// Token: 0x06003AE1 RID: 15073 RVA: 0x000F656B File Offset: 0x000F476B
		private bool caravan_companion_prohibit_kingdoms_on_condition()
		{
			return !MobileParty.ConversationParty.IsCurrentlyUsedByAQuest;
		}

		// Token: 0x06003AE2 RID: 15074 RVA: 0x000F657C File Offset: 0x000F477C
		private void caravan_companion_prohibit_kingdoms_on_consequence()
		{
			ConversationSentence.SetObjectsToRepeatOver(this.GetSuitableKingdomsAsTradePartnerForPlayerCaravans(), 5);
		}

		// Token: 0x06003AE3 RID: 15075 RVA: 0x000F658A File Offset: 0x000F478A
		private bool caravan_companion_prohibit_kingdoms_clickable_condition(out TextObject explanation)
		{
			if (this.GetSuitableKingdomsAsTradePartnerForPlayerCaravans().Count == 0)
			{
				explanation = new TextObject("{=vUmhym4n}There is no suitable kingdom to discuss as a trade partner.", null);
				return false;
			}
			explanation = null;
			return true;
		}

		// Token: 0x06003AE4 RID: 15076 RVA: 0x000F65AC File Offset: 0x000F47AC
		private bool caravan_companion_prohibit_kingdoms_selected_2_on_condition()
		{
			Kingdom kingdom = ConversationSentence.CurrentProcessedRepeatObject as Kingdom;
			if (kingdom != null)
			{
				bool flag = this._prohibitedKingdomsForPlayerCaravans.Contains(kingdom);
				TextObject textObject = TextObject.GetEmpty();
				if (flag)
				{
					textObject = new TextObject("{=1QBbbq4h}Let's continue trading with {KINGDOM_NAME}.", null);
				}
				else
				{
					textObject = new TextObject("{=KsFOH8vo}Let's stop trading with {KINGDOM_NAME}.", null);
				}
				textObject.SetTextVariable("KINGDOM_NAME", kingdom.Name);
				ConversationSentence.SelectedRepeatLine.SetTextVariable("CONTINUE_OR_STOP_TRADE", textObject);
				return true;
			}
			return false;
		}

		// Token: 0x06003AE5 RID: 15077 RVA: 0x000F661C File Offset: 0x000F481C
		private void caravan_companion_prohibit_kingdoms_selected_2_on_consequence()
		{
			Kingdom kingdom = ConversationSentence.SelectedRepeatObject as Kingdom;
			bool flag = this._prohibitedKingdomsForPlayerCaravans.Contains(kingdom);
			TextObject textObject = TextObject.GetEmpty();
			if (flag)
			{
				this._prohibitedKingdomsForPlayerCaravans.Remove(kingdom);
			}
			else
			{
				this._prohibitedKingdomsForPlayerCaravans.Add(kingdom);
				if (kingdom.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					textObject = new TextObject("{=y9sgoggj}We are currently at war with {KINGDOM_NAME}, and we shall not start trading with them even if we make peace.", null);
					textObject.SetTextVariable("KINGDOM_NAME", kingdom.Name);
				}
			}
			MBTextManager.SetTextVariable("DESPITE_WAR", textObject, false);
		}

		// Token: 0x06003AE6 RID: 15078 RVA: 0x000F66A4 File Offset: 0x000F48A4
		private void conversation_caravan_fight_forced_on_consequence()
		{
			this.SetPlayerInteraction(MobileParty.ConversationParty, CaravansCampaignBehavior.PlayerInteraction.Hostile);
			BeHostileAction.ApplyEncounterHostileAction(MobileParty.MainParty.Party, MobileParty.ConversationParty.Party);
		}

		// Token: 0x06003AE7 RID: 15079 RVA: 0x000F66CC File Offset: 0x000F48CC
		private bool companion_is_caravan_leader_on_condition()
		{
			return Hero.OneToOneConversationHero != null && MobileParty.ConversationParty != null && MobileParty.ConversationParty.Party.Owner == Hero.MainHero && MobileParty.ConversationParty.IsCaravan && (Hero.OneToOneConversationHero.IsPlayerCompanion || Hero.OneToOneConversationHero.Clan == Clan.PlayerClan);
		}

		// Token: 0x06003AE8 RID: 15080 RVA: 0x000F672C File Offset: 0x000F492C
		private bool player_caravan_talk_start_on_condition()
		{
			return Hero.OneToOneConversationHero == null && MobileParty.ConversationParty != null && MobileParty.ConversationParty.Party.Owner == Hero.MainHero && MobileParty.ConversationParty.IsCaravan && PartyBase.MainParty.Side == BattleSideEnum.Attacker;
		}

		// Token: 0x06003AE9 RID: 15081 RVA: 0x000F6778 File Offset: 0x000F4978
		private void player_take_prisoner_consequence()
		{
			if (MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredMobileParty.MapFaction))
			{
				this.conversation_caravan_took_prisoner_on_consequence();
			}
		}

		// Token: 0x06003AEA RID: 15082 RVA: 0x000F679C File Offset: 0x000F499C
		private bool conversation_warn_player_on_condition()
		{
			IFaction mapFaction = MobileParty.ConversationParty.MapFaction;
			MBTextManager.SetTextVariable("KINGDOM", mapFaction.IsKingdomFaction ? ((Kingdom)mapFaction).EncyclopediaTitle : mapFaction.Name, false);
			return PlayerEncounter.Current != null && !PlayerEncounter.LeaveEncounter && !MobileParty.MainParty.MapFaction.IsAtWarWith(MobileParty.ConversationParty.MapFaction);
		}

		// Token: 0x06003AEB RID: 15083 RVA: 0x000F6808 File Offset: 0x000F4A08
		private bool caravan_start_talk_on_condition()
		{
			if (MobileParty.ConversationParty == null || !MobileParty.ConversationParty.IsCaravan)
			{
				return false;
			}
			CaravansCampaignBehavior.PlayerInteraction playerInteraction = this.GetPlayerInteraction(MobileParty.ConversationParty);
			this.SetPlayerInteraction(MobileParty.ConversationParty, CaravansCampaignBehavior.PlayerInteraction.Friendly);
			if (playerInteraction == CaravansCampaignBehavior.PlayerInteraction.Hostile)
			{
				MBTextManager.SetTextVariable("CARAVAN_GREETING", "{=L7AN6ybY}What do you want with us now?", false);
			}
			else if (playerInteraction != CaravansCampaignBehavior.PlayerInteraction.None)
			{
				MBTextManager.SetTextVariable("CARAVAN_GREETING", "{=Z5kqbeyu}Greetings, once again. Is there anything else?", false);
			}
			else if (CharacterObject.OneToOneConversationCharacter.IsHero && PartyBase.MainParty.Side == BattleSideEnum.Attacker && MobileParty.ConversationParty.Party.Owner != Hero.MainHero)
			{
				StringHelpers.SetCharacterProperties("LEADER", CharacterObject.OneToOneConversationCharacter, null, false);
				MBTextManager.SetTextVariable("CARAVAN_GREETING", "{=afVsbikp}Greetings, traveller. How may we help you?", false);
			}
			else
			{
				MBTextManager.SetTextVariable("HOMETOWN", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName, false);
				StringHelpers.SetCharacterProperties("MERCHANT", MobileParty.ConversationParty.Party.Owner.CharacterObject, null, false);
				StringHelpers.SetCharacterProperties("PROTECTOR", MobileParty.ConversationParty.HomeSettlement.OwnerClan.Leader.CharacterObject, null, false);
				TextObject text = new TextObject("{=FpUybbSk}Greetings. This caravan is owned by {MERCHANT.LINK}. We trade under the protection of {PROTECTOR.LINK}, master of {HOMETOWN}. How may we help you?[if:convo_normal]", null);
				if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					text = new TextObject("{=yGttYe7g}Greetings. This ship is owned by {MERCHANT.LINK}. We sail under the protection of {PROTECTOR.LINK}, master of {HOMETOWN}. How may we help you?[if:convo_normal]", null);
				}
				MBTextManager.SetTextVariable("CARAVAN_GREETING", text, false);
			}
			return true;
		}

		// Token: 0x06003AEC RID: 15084 RVA: 0x000F6964 File Offset: 0x000F4B64
		private bool caravan_loot_on_condition()
		{
			return MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCaravan && MobileParty.ConversationParty.Party.MapFaction != Hero.MainHero.MapFaction && MobileParty.ConversationParty.Party.Owner != Hero.MainHero;
		}

		// Token: 0x06003AED RID: 15085 RVA: 0x000F69BC File Offset: 0x000F4BBC
		private bool caravan_loot_on_clickable_condition(out TextObject explanation)
		{
			if (this._lootedCaravans.ContainsKey(MobileParty.ConversationParty))
			{
				explanation = new TextObject("{=il2khBNl}You just looted this party.", null);
				return false;
			}
			int num;
			ItemRoster itemRoster;
			this.BribeAmount(MobileParty.ConversationParty.Party, out num, out itemRoster);
			bool flag = num > 0;
			bool flag2 = !itemRoster.IsEmpty<ItemRosterElement>();
			if (flag)
			{
				if (flag2)
				{
					TextObject textObject = ((itemRoster.Count == 1) ? GameTexts.FindText("str_LEFT_RIGHT", null) : GameTexts.FindText("str_LEFT_comma_RIGHT", null));
					TextObject textObject2 = GameTexts.FindText("str_looted_party_have_money", null);
					textObject2.SetTextVariable("MONEY", num);
					textObject2.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					textObject2.SetTextVariable("ITEM_LIST", textObject);
					for (int i = 0; i < itemRoster.Count; i++)
					{
						ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
						TextObject textObject3 = GameTexts.FindText("str_offered_item_list", null);
						textObject3.SetTextVariable("COUNT", elementCopyAtIndex.Amount);
						textObject3.SetTextVariable("ITEM", elementCopyAtIndex.EquipmentElement.Item.Name);
						textObject.SetTextVariable("LEFT", textObject3);
						if (itemRoster.Count == 1)
						{
							textObject.SetTextVariable("RIGHT", TextObject.GetEmpty());
						}
						else if (itemRoster.Count - 2 > i)
						{
							TextObject textObject4 = GameTexts.FindText("str_LEFT_comma_RIGHT", null);
							textObject.SetTextVariable("RIGHT", textObject4);
							textObject = textObject4;
						}
						else
						{
							TextObject textObject5 = GameTexts.FindText("str_LEFT_ONLY", null);
							textObject.SetTextVariable("RIGHT", textObject5);
							textObject = textObject5;
						}
					}
					MBTextManager.SetTextVariable("TAKE_MONEY_AND_PRODUCT_STRING", textObject2, false);
				}
				else
				{
					TextObject textObject6 = GameTexts.FindText("str_looted_party_have_money_but_no_item", null);
					textObject6.SetTextVariable("MONEY", num);
					textObject6.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					MBTextManager.SetTextVariable("TAKE_MONEY_AND_PRODUCT_STRING", textObject6, false);
				}
			}
			else if (flag2)
			{
				TextObject textObject7 = ((itemRoster.Count == 1) ? GameTexts.FindText("str_LEFT_RIGHT", null) : GameTexts.FindText("str_LEFT_comma_RIGHT", null));
				TextObject textObject8 = GameTexts.FindText("str_looted_party_have_no_money", null);
				textObject8.SetTextVariable("ITEM_LIST", textObject7);
				for (int j = 0; j < itemRoster.Count; j++)
				{
					ItemRosterElement elementCopyAtIndex2 = itemRoster.GetElementCopyAtIndex(j);
					TextObject textObject9 = GameTexts.FindText("str_offered_item_list", null);
					textObject9.SetTextVariable("COUNT", elementCopyAtIndex2.Amount);
					textObject9.SetTextVariable("ITEM", elementCopyAtIndex2.EquipmentElement.Item.Name);
					textObject7.SetTextVariable("LEFT", textObject9);
					if (itemRoster.Count == 1)
					{
						textObject7.SetTextVariable("RIGHT", TextObject.GetEmpty());
					}
					else if (itemRoster.Count - 2 > j)
					{
						TextObject textObject10 = GameTexts.FindText("str_LEFT_comma_RIGHT", null);
						textObject7.SetTextVariable("RIGHT", textObject10);
						textObject7 = textObject10;
					}
					else
					{
						TextObject textObject11 = GameTexts.FindText("str_LEFT_ONLY", null);
						textObject7.SetTextVariable("RIGHT", textObject11);
						textObject7 = textObject11;
					}
				}
				MBTextManager.SetTextVariable("TAKE_MONEY_AND_PRODUCT_STRING", textObject8, false);
			}
			if (!flag && !flag2)
			{
				explanation = new TextObject("{=pbRwAjUN}They seem to have no valuable goods.", null);
				return false;
			}
			explanation = null;
			return true;
		}

		// Token: 0x06003AEE RID: 15086 RVA: 0x000F6CF8 File Offset: 0x000F4EF8
		private bool caravan_buy_products_on_condition()
		{
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCaravan)
			{
				if (MobileParty.ConversationParty.IsInRaftState)
				{
					return false;
				}
				for (int i = 0; i < MobileParty.ConversationParty.ItemRoster.Count; i++)
				{
					if (MobileParty.ConversationParty.ItemRoster.GetElementNumber(i) > 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06003AEF RID: 15087 RVA: 0x000F6D56 File Offset: 0x000F4F56
		private void caravan_player_leave_encounter_on_consequence()
		{
			PlayerEncounter.LeaveEncounter = true;
		}

		// Token: 0x06003AF0 RID: 15088 RVA: 0x000F6D5E File Offset: 0x000F4F5E
		private bool caravan_ask_trade_rumors_on_condition()
		{
			return !MobileParty.ConversationParty.IsInRaftState;
		}

		// Token: 0x06003AF1 RID: 15089 RVA: 0x000F6D70 File Offset: 0x000F4F70
		private void caravan_ask_trade_rumors_on_consequence()
		{
			Town destinationForMobileParty = this.GetDestinationForMobileParty(MobileParty.ConversationParty);
			if (MobileParty.ConversationParty.LastVisitedSettlement != null && destinationForMobileParty != null && MobileParty.ConversationParty.LastVisitedSettlement != destinationForMobileParty.Settlement)
			{
				List<ValueTuple<CaravansCampaignBehavior.TradeActionLog, float>> list = new List<ValueTuple<CaravansCampaignBehavior.TradeActionLog, float>>();
				List<CaravansCampaignBehavior.TradeActionLog> list2;
				if (this._tradeActionLogs.TryGetValue(MobileParty.ConversationParty, out list2))
				{
					foreach (CaravansCampaignBehavior.TradeActionLog tradeActionLog in list2)
					{
						float profitRate = tradeActionLog.ProfitRate;
						if (profitRate > 1.2f && tradeActionLog.SoldSettlement != null && tradeActionLog.SoldSettlement != tradeActionLog.BoughtSettlement)
						{
							list.Add(new ValueTuple<CaravansCampaignBehavior.TradeActionLog, float>(tradeActionLog, profitRate));
						}
					}
				}
				if (list.Count <= 0)
				{
					MBTextManager.SetTextVariable("COMMENT", GameTexts.FindText("str_caravan_trade_comment_no_profit", null), false);
					return;
				}
				CaravansCampaignBehavior.TradeActionLog tradeActionLog2 = MBRandom.ChooseWeighted<CaravansCampaignBehavior.TradeActionLog>(list);
				MBTextManager.SetTextVariable("ITEM_NAME", tradeActionLog2.ItemRosterElement.EquipmentElement.Item.Name, false);
				MBTextManager.SetTextVariable("SETTLEMENT", tradeActionLog2.BoughtSettlement.EncyclopediaLinkWithName, false);
				MBTextManager.SetTextVariable("DESTINATION", tradeActionLog2.SoldSettlement.EncyclopediaLinkWithName, false);
				MBTextManager.SetTextVariable("BUY_COST", tradeActionLog2.BuyPrice);
				MBTextManager.SetTextVariable("SELL_COST", tradeActionLog2.SellPrice);
				MBTextManager.SetTextVariable("COMMENT", GameTexts.FindText("str_caravan_trade_comment", null), false);
				if (!this._tradeRumorTakenCaravans.ContainsKey(MobileParty.ConversationParty) || (this._tradeRumorTakenCaravans.ContainsKey(MobileParty.ConversationParty) && CampaignTime.Now - this._tradeRumorTakenCaravans[MobileParty.ConversationParty] >= CampaignTime.Days(1f)))
				{
					List<TradeRumor> list3 = new List<TradeRumor>();
					list3.Add(new TradeRumor(destinationForMobileParty.Owner.Settlement, tradeActionLog2.ItemRosterElement.EquipmentElement.Item, destinationForMobileParty.GetItemPrice(tradeActionLog2.ItemRosterElement.EquipmentElement.Item, null, false), destinationForMobileParty.GetItemPrice(tradeActionLog2.ItemRosterElement.EquipmentElement.Item, null, true), 10));
					Town town = MobileParty.ConversationParty.LastVisitedSettlement.Town;
					if (town != null)
					{
						list3.Add(new TradeRumor(town.Owner.Settlement, tradeActionLog2.ItemRosterElement.EquipmentElement.Item, town.GetItemPrice(tradeActionLog2.ItemRosterElement.EquipmentElement.Item, null, false), town.GetItemPrice(tradeActionLog2.ItemRosterElement.EquipmentElement.Item, null, true), 10));
					}
					if (list3.Count > 0)
					{
						CampaignEventDispatcher.Instance.OnTradeRumorIsTaken(list3, null);
						if (this._tradeRumorTakenCaravans.ContainsKey(MobileParty.ConversationParty) && CampaignTime.Now - this._tradeRumorTakenCaravans[MobileParty.ConversationParty] >= CampaignTime.Days(1f))
						{
							this._tradeRumorTakenCaravans[MobileParty.ConversationParty] = CampaignTime.Now;
							return;
						}
						this._tradeRumorTakenCaravans.Add(MobileParty.ConversationParty, CampaignTime.Now);
						return;
					}
				}
			}
			else
			{
				MBTextManager.SetTextVariable("COMMENT", new TextObject("{=TEUVTPIa}Well, we've been resting in town for a while, so our information is probably quite out of date.", null), false);
			}
		}

		// Token: 0x06003AF2 RID: 15090 RVA: 0x000F70D8 File Offset: 0x000F52D8
		private void caravan_talk_leave_on_consequence()
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		// Token: 0x06003AF3 RID: 15091 RVA: 0x000F70E7 File Offset: 0x000F52E7
		private bool conversation_caravan_player_trade_end_on_condition()
		{
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCaravan)
			{
				InventoryScreenHelper.OpenTradeWithCaravanOrAlleyParty(MobileParty.ConversationParty, InventoryScreenHelper.InventoryCategoryType.None);
			}
			return true;
		}

		// Token: 0x06003AF4 RID: 15092 RVA: 0x000F7108 File Offset: 0x000F5308
		private bool conversation_caravan_not_bribe_on_condition()
		{
			if (MobileParty.ConversationParty == null || !MobileParty.ConversationParty.IsCaravan)
			{
				return false;
			}
			if (MobileParty.ConversationParty.IsInRaftState)
			{
				return false;
			}
			if (MobileParty.ConversationParty.IsCurrentlyAtSea)
			{
				MBTextManager.SetTextVariable("CARAVAN_DEFIANCE", "{=jfKTahGa}If you want our wares you'll have to take this ship, you damned pirate![rf: idle_angry][ib: aggressive]", false);
			}
			else
			{
				MBTextManager.SetTextVariable("CARAVAN_DEFIANCE", "{=QNaKmkt9}We're paid to guard this caravan. If you want to rob it, it's going to be over our dead bodies![rf: idle_angry][ib: aggressive]", false);
			}
			return !this.IsBribeFeasible();
		}

		// Token: 0x06003AF5 RID: 15093 RVA: 0x000F716F File Offset: 0x000F536F
		private bool conversation_caravan_not_surrender_on_condition()
		{
			return MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCaravan && !MobileParty.ConversationParty.IsInRaftState && !this.IsSurrenderFeasible();
		}

		// Token: 0x06003AF6 RID: 15094 RVA: 0x000F719D File Offset: 0x000F539D
		private void conversation_caravan_fight_on_consequence()
		{
			this.SetPlayerInteraction(MobileParty.ConversationParty, CaravansCampaignBehavior.PlayerInteraction.Hostile);
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
		}

		// Token: 0x06003AF7 RID: 15095 RVA: 0x000F71BF File Offset: 0x000F53BF
		private bool conversation_caravan_give_goods_on_condition()
		{
			return MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCaravan;
		}

		// Token: 0x06003AF8 RID: 15096 RVA: 0x000F71D4 File Offset: 0x000F53D4
		private bool conversation_caravan_looted_leave_on_condition()
		{
			return MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCaravan;
		}

		// Token: 0x06003AF9 RID: 15097 RVA: 0x000F71EC File Offset: 0x000F53EC
		private void conversation_caravan_looted_leave_on_consequence()
		{
			int amount;
			ItemRoster itemRoster;
			this.BribeAmount(MobileParty.ConversationParty.Party, out amount, out itemRoster);
			GiveGoldAction.ApplyForPartyToCharacter(MobileParty.ConversationParty.Party, Hero.MainHero, amount, false);
			if (!itemRoster.IsEmpty<ItemRosterElement>())
			{
				for (int i = itemRoster.Count - 1; i >= 0; i--)
				{
					PartyBase party = MobileParty.ConversationParty.Party;
					PartyBase party2 = Hero.MainHero.PartyBelongedTo.Party;
					ItemRosterElement itemRosterElement = itemRoster[i];
					GiveItemAction.ApplyForParties(party, party2, itemRosterElement);
				}
			}
			BeHostileAction.ApplyMinorCoercionHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
			this._lootedCaravans.Add(MobileParty.ConversationParty, CampaignTime.Now);
			this.SetPlayerInteraction(MobileParty.ConversationParty, CaravansCampaignBehavior.PlayerInteraction.Hostile);
			SkillLevelingManager.OnLoot(MobileParty.MainParty, MobileParty.ConversationParty, itemRoster, false);
			PlayerEncounter.LeaveEncounter = true;
		}

		// Token: 0x06003AFA RID: 15098 RVA: 0x000F72B4 File Offset: 0x000F54B4
		private void conversation_caravan_surrender_leave_on_consequence()
		{
			ItemRoster itemRoster = new ItemRoster(MobileParty.ConversationParty.ItemRoster);
			bool flag = false;
			for (int i = 0; i < itemRoster.Count; i++)
			{
				if (itemRoster.GetElementNumber(i) > 0)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { 
				{
					PartyBase.MainParty,
					itemRoster
				} });
				MobileParty.ConversationParty.ItemRoster.Clear();
			}
			int num = MathF.Max(MobileParty.ConversationParty.PartyTradeGold, 0);
			if (num > 0)
			{
				GiveGoldAction.ApplyForPartyToCharacter(MobileParty.ConversationParty.Party, Hero.MainHero, num, false);
			}
			BeHostileAction.ApplyMajorCoercionHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
			this._lootedCaravans.Add(MobileParty.ConversationParty, CampaignTime.Now);
			SkillLevelingManager.OnLoot(MobileParty.MainParty, MobileParty.ConversationParty, itemRoster, false);
			PlayerEncounter.LeaveEncounter = true;
		}

		// Token: 0x06003AFB RID: 15099 RVA: 0x000F7388 File Offset: 0x000F5588
		private void conversation_caravan_took_prisoner_on_consequence()
		{
			MobileParty encounteredMobileParty = PlayerEncounter.EncounteredMobileParty;
			ItemRoster itemRoster = new ItemRoster(encounteredMobileParty.ItemRoster);
			bool flag = false;
			for (int i = 0; i < itemRoster.Count; i++)
			{
				if (itemRoster.GetElementNumber(i) > 0)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { 
				{
					PartyBase.MainParty,
					itemRoster
				} });
				encounteredMobileParty.ItemRoster.Clear();
			}
			int num = MathF.Max(encounteredMobileParty.PartyTradeGold, 0);
			if (num > 0)
			{
				GiveGoldAction.ApplyForPartyToCharacter(encounteredMobileParty.Party, Hero.MainHero, num, false);
			}
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, encounteredMobileParty.Party);
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			foreach (TroopRosterElement troopRosterElement in encounteredMobileParty.MemberRoster.GetTroopRoster())
			{
				troopRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number, false, 0, 0, true, -1);
			}
			PartyScreenHelper.OpenScreenAsLoot(TroopRoster.CreateDummyTroopRoster(), troopRoster, encounteredMobileParty.Name, troopRoster.TotalManCount, null);
			SkillLevelingManager.OnLoot(MobileParty.MainParty, encounteredMobileParty, itemRoster, false);
			DestroyPartyAction.Apply(MobileParty.MainParty.Party, encounteredMobileParty);
			PlayerEncounter.LeaveEncounter = true;
		}

		// Token: 0x06003AFC RID: 15100 RVA: 0x000F74CC File Offset: 0x000F56CC
		private bool IsBribeFeasible()
		{
			float resultNumber = Campaign.Current.Models.EncounterModel.GetBribeChance(MobileParty.ConversationParty, MobileParty.MainParty).ResultNumber;
			return MobileParty.ConversationParty.Party.RandomFloatWithSeed(5U, 1f) <= resultNumber;
		}

		// Token: 0x06003AFD RID: 15101 RVA: 0x000F751C File Offset: 0x000F571C
		private bool IsSurrenderFeasible()
		{
			float surrenderChance = Campaign.Current.Models.EncounterModel.GetSurrenderChance(MobileParty.ConversationParty, MobileParty.MainParty);
			return MobileParty.ConversationParty.Party.RandomFloatWithSeed(7U, 1f) <= surrenderChance;
		}

		// Token: 0x06003AFE RID: 15102 RVA: 0x000F7564 File Offset: 0x000F5764
		private void BribeAmount(PartyBase party, out int gold, out ItemRoster items)
		{
			int num = 0;
			ItemRoster itemRoster = new ItemRoster();
			bool flag = false;
			for (int i = 0; i < MobileParty.ConversationParty.ItemRoster.Count; i++)
			{
				num += MobileParty.ConversationParty.ItemRoster.GetElementUnitCost(i) * MobileParty.ConversationParty.ItemRoster.GetElementNumber(i);
				flag = true;
			}
			num += MobileParty.ConversationParty.PartyTradeGold;
			int num2 = MathF.Min((int)((float)num * 0.05f), 2000);
			int num3 = MathF.Min(MobileParty.ConversationParty.PartyTradeGold, num2);
			if (num3 < num2 && flag)
			{
				for (int j = 0; j < MobileParty.ConversationParty.ItemRoster.Count; j++)
				{
					ItemRosterElement elementCopyAtIndex = MobileParty.ConversationParty.ItemRoster.GetElementCopyAtIndex(j);
					if (elementCopyAtIndex.EquipmentElement.ItemValue * elementCopyAtIndex.Amount >= num2 - num3)
					{
						if (elementCopyAtIndex.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Goods)
						{
							if (!itemRoster.IsEmpty<ItemRosterElement>())
							{
								itemRoster.Clear();
							}
							itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement.Item, elementCopyAtIndex.Amount);
							break;
						}
						if (itemRoster.IsEmpty<ItemRosterElement>())
						{
							itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement.Item, elementCopyAtIndex.Amount);
						}
					}
				}
				if (itemRoster.IsEmpty<ItemRosterElement>())
				{
					int num4 = num2 - num3;
					int num5 = 0;
					while (num5 < MobileParty.ConversationParty.ItemRoster.Count && num4 > 0)
					{
						ItemRosterElement randomElement = MobileParty.ConversationParty.ItemRoster.GetRandomElement<ItemRosterElement>();
						num4 -= randomElement.Amount * randomElement.EquipmentElement.ItemValue;
						itemRoster.AddToCounts(randomElement.EquipmentElement.Item, randomElement.Amount);
						num5++;
					}
				}
			}
			gold = num3;
			items = itemRoster;
		}

		// Token: 0x06003AFF RID: 15103 RVA: 0x000F7748 File Offset: 0x000F5948
		private bool CanTradeWith(IFaction caravanFaction, IFaction targetFaction)
		{
			Kingdom item;
			return !caravanFaction.IsAtWarWith(targetFaction) && (caravanFaction != Hero.MainHero.MapFaction || (item = targetFaction as Kingdom) == null || !this._prohibitedKingdomsForPlayerCaravans.Contains(item));
		}

		// Token: 0x04001215 RID: 4629
		private const float InventoryFullnessGoal = 0.9f;

		// Token: 0x04001216 RID: 4630
		private const float AverageCaravanWaitAtSettlement = 3f;

		// Token: 0x04001217 RID: 4631
		private const float ProfitRateRumorThreshold = 1.2f;

		// Token: 0x04001218 RID: 4632
		private const float ReferenceBudgetValue = 5000f;

		// Token: 0x04001219 RID: 4633
		private const float HighSecurityThreshold = 75f;

		// Token: 0x0400121A RID: 4634
		private const float MustDiscardPriorityValue = -3.4028235E+38f;

		// Token: 0x0400121B RID: 4635
		private const float CaravanTradeAgreementBonus = 2f;

		// Token: 0x0400121C RID: 4636
		private const float ConvoyTradeAgreementBonus = 1.5f;

		// Token: 0x0400121D RID: 4637
		private float _navalCaravanVeryFarCache = -1f;

		// Token: 0x0400121E RID: 4638
		private float _defaultCaravanVeryFarCache = -1f;

		// Token: 0x0400121F RID: 4639
		private ITradeAgreementsCampaignBehavior _tradeAgreementsBehavior;

		// Token: 0x04001220 RID: 4640
		private Dictionary<MobileParty, CampaignTime> _tradeRumorTakenCaravans = new Dictionary<MobileParty, CampaignTime>();

		// Token: 0x04001221 RID: 4641
		private Dictionary<MobileParty, CampaignTime> _caravanLastHomeTownVisitTime = new Dictionary<MobileParty, CampaignTime>();

		// Token: 0x04001222 RID: 4642
		private Dictionary<MobileParty, CampaignTime> _lootedCaravans = new Dictionary<MobileParty, CampaignTime>();

		// Token: 0x04001223 RID: 4643
		private Dictionary<MobileParty, CaravansCampaignBehavior.PlayerInteraction> _interactedCaravans = new Dictionary<MobileParty, CaravansCampaignBehavior.PlayerInteraction>();

		// Token: 0x04001224 RID: 4644
		private Dictionary<MobileParty, List<CaravansCampaignBehavior.TradeActionLog>> _tradeActionLogs = new Dictionary<MobileParty, List<CaravansCampaignBehavior.TradeActionLog>>();

		// Token: 0x04001225 RID: 4645
		private CaravansCampaignBehavior.TradeActionLogPool _tradeActionLogPool;

		// Token: 0x04001226 RID: 4646
		private List<Kingdom> _prohibitedKingdomsForPlayerCaravans = new List<Kingdom>();

		// Token: 0x04001227 RID: 4647
		private int _packAnimalCategoryIndex = -1;

		// Token: 0x04001228 RID: 4648
		private readonly Dictionary<ItemCategory, float> _averageValuesCached = new Dictionary<ItemCategory, float>();

		// Token: 0x04001229 RID: 4649
		private readonly Dictionary<ItemCategory, CaravansCampaignBehavior.PriceIndexData> _priceDictionary = new Dictionary<ItemCategory, CaravansCampaignBehavior.PriceIndexData>();

		// Token: 0x0400122A RID: 4650
		private readonly Dictionary<ItemCategory, CaravansCampaignBehavior.PriceIndexData> _coastalPriceDictionary = new Dictionary<ItemCategory, CaravansCampaignBehavior.PriceIndexData>();

		// Token: 0x0400122B RID: 4651
		private readonly Dictionary<ItemCategory, int> _totalValueOfItemsAtCategory = new Dictionary<ItemCategory, int>();

		// Token: 0x020007B7 RID: 1975
		public class CaravansCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06006251 RID: 25169 RVA: 0x001BAE1C File Offset: 0x001B901C
			public CaravansCampaignBehaviorTypeDefiner()
				: base(60000)
			{
			}

			// Token: 0x06006252 RID: 25170 RVA: 0x001BAE29 File Offset: 0x001B9029
			protected override void DefineEnumTypes()
			{
				base.AddEnumDefinition(typeof(CaravansCampaignBehavior.PlayerInteraction), 1, null);
			}

			// Token: 0x06006253 RID: 25171 RVA: 0x001BAE3D File Offset: 0x001B903D
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(Dictionary<MobileParty, CaravansCampaignBehavior.PlayerInteraction>));
				base.ConstructContainerDefinition(typeof(List<CaravansCampaignBehavior.TradeActionLog>));
				base.ConstructContainerDefinition(typeof(Dictionary<MobileParty, List<CaravansCampaignBehavior.TradeActionLog>>));
			}

			// Token: 0x06006254 RID: 25172 RVA: 0x001BAE6F File Offset: 0x001B906F
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(CaravansCampaignBehavior.TradeActionLog), 2, null);
			}
		}

		// Token: 0x020007B8 RID: 1976
		private enum PlayerInteraction
		{
			// Token: 0x04001ECE RID: 7886
			None,
			// Token: 0x04001ECF RID: 7887
			Friendly,
			// Token: 0x04001ED0 RID: 7888
			TradedWith,
			// Token: 0x04001ED1 RID: 7889
			Hostile
		}

		// Token: 0x020007B9 RID: 1977
		private struct PriceIndexData
		{
			// Token: 0x06006255 RID: 25173 RVA: 0x001BAE83 File Offset: 0x001B9083
			public PriceIndexData(float averageBuySellPriceIndex, float minBuySellPriceIndex)
			{
				this.AverageBuySellPriceIndex = averageBuySellPriceIndex;
				this.MinBuySellPriceIndex = minBuySellPriceIndex;
			}

			// Token: 0x04001ED2 RID: 7890
			internal readonly float AverageBuySellPriceIndex;

			// Token: 0x04001ED3 RID: 7891
			internal readonly float MinBuySellPriceIndex;
		}

		// Token: 0x020007BA RID: 1978
		internal class TradeActionLog
		{
			// Token: 0x17001518 RID: 5400
			// (get) Token: 0x06006256 RID: 25174 RVA: 0x001BAE93 File Offset: 0x001B9093
			public float ProfitRate
			{
				get
				{
					return (float)this.SellPrice / (float)this.BuyPrice;
				}
			}

			// Token: 0x06006257 RID: 25175 RVA: 0x001BAEA4 File Offset: 0x001B90A4
			public void OnSellAction(Settlement soldSettlement, int sellPrice)
			{
				this.SellPrice = sellPrice;
				this.SoldSettlement = soldSettlement;
			}

			// Token: 0x06006258 RID: 25176 RVA: 0x001BAEB4 File Offset: 0x001B90B4
			public void Reset()
			{
				this.BoughtSettlement = null;
				this.SoldSettlement = null;
				this.SellPrice = 0;
				this.BuyPrice = 0;
			}

			// Token: 0x06006259 RID: 25177 RVA: 0x001BAED2 File Offset: 0x001B90D2
			internal static void AutoGeneratedStaticCollectObjectsTradeActionLog(object o, List<object> collectedObjects)
			{
				((CaravansCampaignBehavior.TradeActionLog)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600625A RID: 25178 RVA: 0x001BAEE0 File Offset: 0x001B90E0
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.BoughtSettlement);
				ItemRosterElement.AutoGeneratedStaticCollectObjectsItemRosterElement(this.ItemRosterElement, collectedObjects);
				collectedObjects.Add(this.SoldSettlement);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.BoughtTime, collectedObjects);
			}

			// Token: 0x0600625B RID: 25179 RVA: 0x001BAF1C File Offset: 0x001B911C
			internal static object AutoGeneratedGetMemberValueBoughtSettlement(object o)
			{
				return ((CaravansCampaignBehavior.TradeActionLog)o).BoughtSettlement;
			}

			// Token: 0x0600625C RID: 25180 RVA: 0x001BAF29 File Offset: 0x001B9129
			internal static object AutoGeneratedGetMemberValueBuyPrice(object o)
			{
				return ((CaravansCampaignBehavior.TradeActionLog)o).BuyPrice;
			}

			// Token: 0x0600625D RID: 25181 RVA: 0x001BAF3B File Offset: 0x001B913B
			internal static object AutoGeneratedGetMemberValueSellPrice(object o)
			{
				return ((CaravansCampaignBehavior.TradeActionLog)o).SellPrice;
			}

			// Token: 0x0600625E RID: 25182 RVA: 0x001BAF4D File Offset: 0x001B914D
			internal static object AutoGeneratedGetMemberValueItemRosterElement(object o)
			{
				return ((CaravansCampaignBehavior.TradeActionLog)o).ItemRosterElement;
			}

			// Token: 0x0600625F RID: 25183 RVA: 0x001BAF5F File Offset: 0x001B915F
			internal static object AutoGeneratedGetMemberValueSoldSettlement(object o)
			{
				return ((CaravansCampaignBehavior.TradeActionLog)o).SoldSettlement;
			}

			// Token: 0x06006260 RID: 25184 RVA: 0x001BAF6C File Offset: 0x001B916C
			internal static object AutoGeneratedGetMemberValueBoughtTime(object o)
			{
				return ((CaravansCampaignBehavior.TradeActionLog)o).BoughtTime;
			}

			// Token: 0x04001ED4 RID: 7892
			[SaveableField(0)]
			public Settlement BoughtSettlement;

			// Token: 0x04001ED5 RID: 7893
			[SaveableField(1)]
			public int BuyPrice;

			// Token: 0x04001ED6 RID: 7894
			[SaveableField(2)]
			public int SellPrice;

			// Token: 0x04001ED7 RID: 7895
			[SaveableField(3)]
			public ItemRosterElement ItemRosterElement;

			// Token: 0x04001ED8 RID: 7896
			[SaveableField(4)]
			public Settlement SoldSettlement;

			// Token: 0x04001ED9 RID: 7897
			[SaveableField(5)]
			public CampaignTime BoughtTime;
		}

		// Token: 0x020007BB RID: 1979
		internal class TradeActionLogPool
		{
			// Token: 0x17001519 RID: 5401
			// (get) Token: 0x06006262 RID: 25186 RVA: 0x001BAF86 File Offset: 0x001B9186
			public int Size
			{
				get
				{
					Stack<CaravansCampaignBehavior.TradeActionLog> stack = this._stack;
					if (stack == null)
					{
						return 0;
					}
					return stack.Count;
				}
			}

			// Token: 0x1700151A RID: 5402
			// (get) Token: 0x06006263 RID: 25187 RVA: 0x001BAF99 File Offset: 0x001B9199
			private int MaxSize { get; }

			// Token: 0x06006264 RID: 25188 RVA: 0x001BAFA4 File Offset: 0x001B91A4
			public TradeActionLogPool(int size)
			{
				this.MaxSize = size;
				this._stack = new Stack<CaravansCampaignBehavior.TradeActionLog>(size);
				for (int i = 0; i < size; i++)
				{
					this._stack.Push(new CaravansCampaignBehavior.TradeActionLog());
				}
			}

			// Token: 0x06006265 RID: 25189 RVA: 0x001BAFE8 File Offset: 0x001B91E8
			public CaravansCampaignBehavior.TradeActionLog CreateNewLog(Settlement boughtSettlement, int buyPrice, ItemRosterElement itemRosterElement)
			{
				CaravansCampaignBehavior.TradeActionLog tradeActionLog = ((this._stack.Count > 0) ? this._stack.Pop() : new CaravansCampaignBehavior.TradeActionLog());
				tradeActionLog.BoughtSettlement = boughtSettlement;
				tradeActionLog.BuyPrice = buyPrice;
				tradeActionLog.ItemRosterElement = itemRosterElement;
				tradeActionLog.BoughtTime = CampaignTime.Now;
				return tradeActionLog;
			}

			// Token: 0x06006266 RID: 25190 RVA: 0x001BB035 File Offset: 0x001B9235
			public void ReleaseLog(CaravansCampaignBehavior.TradeActionLog log)
			{
				log.Reset();
				if (this._stack.Count < this.MaxSize)
				{
					this._stack.Push(log);
				}
			}

			// Token: 0x06006267 RID: 25191 RVA: 0x001BB05C File Offset: 0x001B925C
			public override string ToString()
			{
				return string.Format("TrackPool: {0}", this.Size);
			}

			// Token: 0x04001EDB RID: 7899
			private Stack<CaravansCampaignBehavior.TradeActionLog> _stack;
		}
	}
}
