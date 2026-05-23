using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003E1 RID: 993
	public class CraftingCampaignBehavior : CampaignBehaviorBase, ICraftingCampaignBehavior, ICampaignBehavior, INonReadyObjectHandler
	{
		// Token: 0x17000E0A RID: 3594
		// (get) Token: 0x06003CCC RID: 15564 RVA: 0x001066D6 File Offset: 0x001048D6
		public IReadOnlyDictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots> CraftingOrders
		{
			get
			{
				return this._craftingOrders;
			}
		}

		// Token: 0x17000E0B RID: 3595
		// (get) Token: 0x06003CCD RID: 15565 RVA: 0x001066E0 File Offset: 0x001048E0
		public IReadOnlyCollection<WeaponDesign> CraftingHistory
		{
			get
			{
				MBList<WeaponDesign> mblist = new MBList<WeaponDesign>();
				foreach (ItemObject itemObject in this._cratingItemsHistory)
				{
					WeaponDesign weaponDesign = itemObject.WeaponDesign;
					mblist.Add(new WeaponDesign(weaponDesign.Template, weaponDesign.WeaponName, weaponDesign.UsedPieces, weaponDesign.HashedCode));
				}
				return mblist;
			}
		}

		// Token: 0x06003CCE RID: 15566 RVA: 0x0010675C File Offset: 0x0010495C
		private string GetNextCraftedItemId()
		{
			string result = string.Format("crafted_item_{0}", this._craftedItemCount);
			this._craftedItemCount++;
			return result;
		}

		// Token: 0x06003CCF RID: 15567 RVA: 0x00106781 File Offset: 0x00104981
		private string GetNextTownOrderId()
		{
			string result = string.Format("town_order_{0}", this._townOrderCount);
			this._townOrderCount++;
			return result;
		}

		// Token: 0x06003CD0 RID: 15568 RVA: 0x001067A8 File Offset: 0x001049A8
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Hero>("_activeCraftingHero", ref this._activeCraftingHero);
			dataStore.SyncData<Dictionary<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData>>("_craftedItemDictionary", ref this._craftedItemDictionary);
			dataStore.SyncData<Dictionary<Hero, CraftingCampaignBehavior.HeroCraftingRecord>>("_heroCraftingRecordsNew", ref this._heroCraftingRecords);
			dataStore.SyncData<Dictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots>>("_craftingOrders", ref this._craftingOrders);
			dataStore.SyncData<List<ItemObject>>("_cratingItemsHistory", ref this._cratingItemsHistory);
			dataStore.SyncData<Dictionary<CraftingTemplate, List<CraftingPiece>>>("_openedPartsDictionary", ref this._openedPartsDictionary);
			dataStore.SyncData<Dictionary<CraftingTemplate, float>>("_openNewPartXpDictionary", ref this._openNewPartXpDictionary);
			dataStore.SyncData<int>("_townOrderCount", ref this._townOrderCount);
			dataStore.SyncData<int>("_craftedItemCount", ref this._craftedItemCount);
			if (dataStore.IsLoading && MBSaveLoad.IsUpdatingGameVersion)
			{
				if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.0", 0))
				{
					List<CraftingPiece> list = new List<CraftingPiece>();
					dataStore.SyncData<List<CraftingPiece>>("_openedParts", ref list);
					if (list != null)
					{
						this._openedPartsDictionary = new Dictionary<CraftingTemplate, List<CraftingPiece>>();
						foreach (CraftingTemplate craftingTemplate in CraftingTemplate.All)
						{
							this._openedPartsDictionary.Add(craftingTemplate, new List<CraftingPiece>());
							foreach (CraftingPiece item in list)
							{
								if (craftingTemplate.Pieces.Contains(item) && !this._openedPartsDictionary[craftingTemplate].Contains(item))
								{
									this._openedPartsDictionary[craftingTemplate].Add(item);
								}
							}
						}
					}
				}
				if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.2", 0))
				{
					List<ItemObject> list2 = new List<ItemObject>();
					for (int i = 0; i < this._craftedItemDictionary.Count; i++)
					{
						KeyValuePair<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData> keyValuePair = this._craftedItemDictionary.ElementAt(i);
						if (keyValuePair.Value.CraftedData.Template.IsReady)
						{
							bool flag = true;
							foreach (PieceData pieceData in keyValuePair.Value.CraftedData.Template.BuildOrders)
							{
								bool flag2 = false;
								foreach (WeaponDesignElement weaponDesignElement in keyValuePair.Value.CraftedData.UsedPieces)
								{
									if (pieceData.PieceType == weaponDesignElement.CraftingPiece.PieceType && weaponDesignElement.CraftingPiece.IsValid)
									{
										flag2 = true;
									}
								}
								if (!flag2)
								{
									flag = false;
									break;
								}
							}
							if (flag)
							{
								string nextCraftedItemId = this.GetNextCraftedItemId();
								keyValuePair.Key.StringId = nextCraftedItemId;
								WeaponDesignElement[] array = new WeaponDesignElement[keyValuePair.Value.CraftedData.UsedPieces.Length];
								for (int l = 0; l < keyValuePair.Value.CraftedData.UsedPieces.Length; l++)
								{
									array[l] = keyValuePair.Value.CraftedData.UsedPieces[l].GetCopy();
								}
								WeaponDesign craftedData = new WeaponDesign(keyValuePair.Value.CraftedData.Template, keyValuePair.Value.CraftedData.WeaponName, array, nextCraftedItemId);
								this._craftedItemDictionary[keyValuePair.Key] = new CraftingCampaignBehavior.CraftedItemInitializationData(craftedData, keyValuePair.Value.ItemName, keyValuePair.Value.Culture);
							}
							else
							{
								list2.Add(keyValuePair.Key);
							}
						}
						else
						{
							list2.Add(keyValuePair.Key);
						}
					}
					foreach (ItemObject key in list2)
					{
						this._craftedItemDictionary.Remove(key);
					}
					List<WeaponDesign> list3 = new List<WeaponDesign>();
					dataStore.SyncData<List<WeaponDesign>>("_craftingHistory", ref list3);
					foreach (WeaponDesign weaponDesign in list3)
					{
						ItemObject itemObject = null;
						foreach (KeyValuePair<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData> keyValuePair2 in this._craftedItemDictionary)
						{
							WeaponDesign craftedData2 = keyValuePair2.Value.CraftedData;
							if (!this._cratingItemsHistory.Contains(keyValuePair2.Key) && weaponDesign.Template == craftedData2.Template)
							{
								bool flag3 = true;
								int num = 0;
								while (num < weaponDesign.UsedPieces.Length && flag3)
								{
									WeaponDesignElement weaponDesignElement2 = weaponDesign.UsedPieces[num];
									string a;
									if (weaponDesignElement2 == null)
									{
										a = null;
									}
									else
									{
										CraftingPiece craftingPiece = weaponDesignElement2.CraftingPiece;
										a = ((craftingPiece != null) ? craftingPiece.StringId : null);
									}
									WeaponDesignElement weaponDesignElement3 = craftedData2.UsedPieces[num];
									string b;
									if (weaponDesignElement3 == null)
									{
										b = null;
									}
									else
									{
										CraftingPiece craftingPiece2 = weaponDesignElement3.CraftingPiece;
										b = ((craftingPiece2 != null) ? craftingPiece2.StringId : null);
									}
									if (a != b)
									{
										flag3 = false;
									}
									num++;
								}
								if (flag3)
								{
									itemObject = keyValuePair2.Key;
									break;
								}
							}
						}
						if (itemObject != null)
						{
							this._cratingItemsHistory.Add(itemObject);
						}
					}
					for (int m = 0; m < this._craftingOrders.Count; m++)
					{
						KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots> keyValuePair3 = this._craftingOrders.ElementAt(m);
						for (int n = 0; n < keyValuePair3.Value.Slots.Count<CraftingOrder>(); n++)
						{
							string nextTownOrderId = this.GetNextTownOrderId();
							CraftingOrder craftingOrder = keyValuePair3.Value.Slots[n];
							if (craftingOrder != null)
							{
								WeaponDesign weaponDesignTemplate = craftingOrder.WeaponDesignTemplate;
								WeaponDesign weaponDesignTemplate2 = new WeaponDesign(weaponDesignTemplate.Template, weaponDesignTemplate.WeaponName, weaponDesignTemplate.UsedPieces, nextTownOrderId);
								CraftingTemplate templateFromId = CraftingTemplate.GetTemplateFromId(weaponDesignTemplate.Template.StringId);
								CraftingOrder craftingOrder2 = new CraftingOrder(craftingOrder.OrderOwner, (float)craftingOrder.DifficultyLevel, weaponDesignTemplate2, templateFromId, craftingOrder.DifficultyLevel, nextTownOrderId);
								keyValuePair3.Value.Slots[n] = craftingOrder2;
							}
						}
					}
				}
			}
		}

		// Token: 0x06003CD1 RID: 15569 RVA: 0x00106E24 File Offset: 0x00105024
		void INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()
		{
			if (this._craftedItemDictionary.Count > 0)
			{
				this.InitializeCraftedItemData();
			}
			foreach (KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots> keyValuePair in this.CraftingOrders)
			{
				foreach (CraftingOrder craftingOrder in keyValuePair.Value.Slots)
				{
					if (craftingOrder != null && !craftingOrder.IsPreCraftedWeaponDesignValid())
					{
						keyValuePair.Value.RemoveTownOrder(craftingOrder);
					}
					else if (craftingOrder != null)
					{
						craftingOrder.InitializeCraftingOrderOnLoad();
					}
				}
				List<CraftingOrder> list = new List<CraftingOrder>();
				foreach (CraftingOrder craftingOrder2 in keyValuePair.Value.CustomOrders)
				{
					if (!craftingOrder2.IsPreCraftedWeaponDesignValid())
					{
						list.Add(craftingOrder2);
					}
					else
					{
						craftingOrder2.InitializeCraftingOrderOnLoad();
					}
				}
				foreach (CraftingOrder order in list)
				{
					keyValuePair.Value.RemoveCustomOrder(order);
				}
			}
			for (int j = this._cratingItemsHistory.Count - 1; j >= 0; j--)
			{
				ItemObject itemObject = this._cratingItemsHistory[j];
				if (itemObject == DefaultItems.Trash || itemObject == null)
				{
					this._cratingItemsHistory.RemoveAt(j);
				}
			}
		}

		// Token: 0x06003CD2 RID: 15570 RVA: 0x00106FC0 File Offset: 0x001051C0
		private void InitializeCraftedItemData()
		{
			for (int i = 0; i < this._craftedItemDictionary.Count; i++)
			{
				KeyValuePair<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData> keyValuePair = this._craftedItemDictionary.ElementAt(i);
				ItemObject key = keyValuePair.Key;
				WeaponDesignElement[] array = new WeaponDesignElement[keyValuePair.Value.CraftedData.UsedPieces.Length];
				for (int j = 0; j < keyValuePair.Value.CraftedData.UsedPieces.Length; j++)
				{
					array[j] = keyValuePair.Value.CraftedData.UsedPieces[j].GetCopy();
				}
				WeaponDesign craftedData = new WeaponDesign(keyValuePair.Value.CraftedData.Template, keyValuePair.Value.CraftedData.WeaponName, array, key.StringId);
				this._craftedItemDictionary[key] = new CraftingCampaignBehavior.CraftedItemInitializationData(craftedData, keyValuePair.Value.ItemName, keyValuePair.Value.Culture);
			}
			foreach (KeyValuePair<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData> keyValuePair2 in this._craftedItemDictionary)
			{
				ItemObject itemObject = Crafting.InitializePreCraftedWeaponOnLoad(keyValuePair2.Key, keyValuePair2.Value.CraftedData, keyValuePair2.Value.ItemName, keyValuePair2.Value.Culture);
				if (itemObject == DefaultItems.Trash || itemObject == null)
				{
					if (MBObjectManager.Instance.GetObject(keyValuePair2.Key.Id) != null)
					{
						MBObjectManager.Instance.UnregisterObject(keyValuePair2.Key);
					}
				}
				else
				{
					ItemObject.InitAsPlayerCraftedItem(ref itemObject);
					itemObject.IsReady = true;
				}
			}
		}

		// Token: 0x06003CD3 RID: 15571 RVA: 0x00107174 File Offset: 0x00105374
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, new Action<ItemObject, ItemModifier, bool>(this.OnNewItemCrafted));
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.HourlyTick));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
		}

		// Token: 0x06003CD4 RID: 15572 RVA: 0x0010723C File Offset: 0x0010543C
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			this.InitializeLists();
			MBList<Hero> mblist = new MBList<Hero>();
			foreach (Town town in Town.AllTowns)
			{
				Settlement settlement = town.Settlement;
				mblist.AddRange(settlement.HeroesWithoutParty);
				foreach (MobileParty mobileParty in settlement.Parties)
				{
					if (mobileParty.LeaderHero != null && !mobileParty.IsMainParty)
					{
						mblist.Add(mobileParty.LeaderHero);
					}
				}
				if (mblist.Count > 0)
				{
					for (int i = 0; i < 6; i++)
					{
						if (this.CraftingOrders[settlement.Town].GetAvailableSlot() > -1)
						{
							this.CreateTownOrder(mblist.GetRandomElement<Hero>(), i);
						}
					}
				}
				mblist.Clear();
			}
		}

		// Token: 0x06003CD5 RID: 15573 RVA: 0x0010734C File Offset: 0x0010554C
		private void DailyTickSettlement(Settlement settlement)
		{
			if (settlement.IsTown && this.CraftingOrders[settlement.Town].IsThereAvailableSlot())
			{
				List<Hero> list = new List<Hero>(settlement.HeroesWithoutParty);
				foreach (MobileParty mobileParty in settlement.Parties)
				{
					if (mobileParty.LeaderHero != null && !mobileParty.IsMainParty)
					{
						list.Add(mobileParty.LeaderHero);
					}
				}
				foreach (Hero hero in list)
				{
					if (hero != Hero.MainHero && MBRandom.RandomFloat <= 0.05f)
					{
						int availableSlot = this.CraftingOrders[settlement.Town].GetAvailableSlot();
						if (availableSlot <= -1)
						{
							break;
						}
						this.CreateTownOrder(hero, availableSlot);
					}
				}
			}
		}

		// Token: 0x06003CD6 RID: 15574 RVA: 0x0010745C File Offset: 0x0010565C
		private void DailyTick()
		{
			foreach (KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots> keyValuePair in this.CraftingOrders)
			{
				foreach (CraftingOrder craftingOrder in keyValuePair.Value.Slots)
				{
					if (craftingOrder != null && MBRandom.RandomFloat <= 0.05f)
					{
						this.ReplaceCraftingOrder(keyValuePair.Key, craftingOrder);
					}
				}
			}
		}

		// Token: 0x06003CD7 RID: 15575 RVA: 0x001074E4 File Offset: 0x001056E4
		private void HourlyTick()
		{
			foreach (KeyValuePair<Hero, CraftingCampaignBehavior.HeroCraftingRecord> keyValuePair in this._heroCraftingRecords)
			{
				if (keyValuePair.Key.CurrentSettlement != null)
				{
					int maxHeroCraftingStamina = this.GetMaxHeroCraftingStamina(keyValuePair.Key);
					if (keyValuePair.Value.CraftingStamina < maxHeroCraftingStamina)
					{
						keyValuePair.Value.CraftingStamina = MathF.Min(maxHeroCraftingStamina, keyValuePair.Value.CraftingStamina + CraftingCampaignBehavior.GetStaminaHourlyRecoveryRate(keyValuePair.Key));
					}
				}
			}
		}

		// Token: 0x06003CD8 RID: 15576 RVA: 0x00107588 File Offset: 0x00105788
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			this.RemoveOrdersOfHeroWithoutCompletionIfExists(victim);
		}

		// Token: 0x06003CD9 RID: 15577 RVA: 0x00107594 File Offset: 0x00105794
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeLists();
			foreach (KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots> keyValuePair in this._craftingOrders)
			{
				for (int i = 0; i < 6; i++)
				{
					CraftingOrder craftingOrder = keyValuePair.Value.Slots[i];
					if (craftingOrder != null && (craftingOrder.PreCraftedWeaponDesignItem == DefaultItems.Trash || craftingOrder.PreCraftedWeaponDesignItem == null || !craftingOrder.PreCraftedWeaponDesignItem.IsReady))
					{
						this.CancelOrder(keyValuePair.Key, craftingOrder);
					}
				}
			}
		}

		// Token: 0x06003CDA RID: 15578 RVA: 0x00107634 File Offset: 0x00105834
		private static int GetStaminaHourlyRecoveryRate(Hero hero)
		{
			int num = 5 + MathF.Round((float)hero.GetSkillValue(DefaultSkills.Crafting) * 0.025f);
			if (hero.GetPerkValue(DefaultPerks.Athletics.Stamina))
			{
				num += MathF.Round((float)num * DefaultPerks.Athletics.Stamina.PrimaryBonus);
			}
			return num;
		}

		// Token: 0x06003CDB RID: 15579 RVA: 0x00107680 File Offset: 0x00105880
		private void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
		{
			if (!this._craftedItemDictionary.ContainsKey(itemObject))
			{
				CultureObject @object = MBObjectManager.Instance.GetObject<CultureObject>(itemObject.Culture.StringId);
				CraftingCampaignBehavior.CraftedItemInitializationData value = new CraftingCampaignBehavior.CraftedItemInitializationData(itemObject.WeaponDesign, itemObject.Name, @object);
				this._craftedItemDictionary.Add(itemObject, value);
			}
		}

		// Token: 0x06003CDC RID: 15580 RVA: 0x001076D4 File Offset: 0x001058D4
		private void AddResearchPoints(CraftingTemplate craftingTemplate, int researchPoints)
		{
			Dictionary<CraftingTemplate, float> openNewPartXpDictionary = this._openNewPartXpDictionary;
			CraftingTemplate craftingTemplate2 = craftingTemplate;
			openNewPartXpDictionary[craftingTemplate2] += (float)researchPoints;
			int count = craftingTemplate.Pieces.Count;
			int num = craftingTemplate.Pieces.Count((CraftingPiece x) => this.IsOpened(x, craftingTemplate));
			float num2 = Campaign.Current.Models.SmithingModel.ResearchPointsNeedForNewPart(count, num);
			do
			{
				if (this._openNewPartXpDictionary[craftingTemplate] > num2)
				{
					openNewPartXpDictionary = this._openNewPartXpDictionary;
					craftingTemplate2 = craftingTemplate;
					openNewPartXpDictionary[craftingTemplate2] -= num2;
					if (this.OpenNewPart(craftingTemplate))
					{
						num++;
					}
				}
				num2 = Campaign.Current.Models.SmithingModel.ResearchPointsNeedForNewPart(count, craftingTemplate.Pieces.Count((CraftingPiece x) => this.IsOpened(x, craftingTemplate)));
			}
			while (this._openNewPartXpDictionary[craftingTemplate] > num2 && num < count);
		}

		// Token: 0x06003CDD RID: 15581 RVA: 0x001077F8 File Offset: 0x001059F8
		private bool OpenNewPart(CraftingTemplate craftingTemplate)
		{
			int num = int.MaxValue;
			MBList<CraftingPiece> mblist = new MBList<CraftingPiece>();
			foreach (CraftingPiece craftingPiece in craftingTemplate.Pieces)
			{
				int pieceTier = craftingPiece.PieceTier;
				if (num >= pieceTier && !craftingPiece.IsHiddenOnDesigner && !this.IsOpened(craftingPiece, craftingTemplate))
				{
					if (num > craftingPiece.PieceTier)
					{
						mblist.Clear();
						num = pieceTier;
					}
					mblist.Add(craftingPiece);
				}
			}
			if (mblist.Count > 0)
			{
				CraftingPiece randomElement = mblist.GetRandomElement<CraftingPiece>();
				this.OpenPart(randomElement, craftingTemplate, true);
				return true;
			}
			return false;
		}

		// Token: 0x06003CDE RID: 15582 RVA: 0x001078A8 File Offset: 0x00105AA8
		private void OpenPart(CraftingPiece selectedPiece, CraftingTemplate craftingTemplate, bool showNotification = true)
		{
			this._openedPartsDictionary[craftingTemplate].Add(selectedPiece);
			CampaignEventDispatcher.Instance.CraftingPartUnlocked(selectedPiece);
			if (showNotification)
			{
				TextObject textObject = new TextObject("{=p9F90bc0}New Smithing Part Unlocked: {PART_NAME} for {WEAPON_TYPE}.", null);
				textObject.SetTextVariable("PART_NAME", selectedPiece.Name);
				textObject.SetTextVariable("WEAPON_TYPE", craftingTemplate.TemplateName);
				MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
			}
		}

		// Token: 0x06003CDF RID: 15583 RVA: 0x00107911 File Offset: 0x00105B11
		public bool IsOpened(CraftingPiece craftingPiece, CraftingTemplate craftingTemplate)
		{
			return craftingPiece.IsGivenByDefault || this._openedPartsDictionary[craftingTemplate].Contains(craftingPiece);
		}

		// Token: 0x06003CE0 RID: 15584 RVA: 0x0010792F File Offset: 0x00105B2F
		public int GetCraftingDifficulty(WeaponDesign weaponDesign)
		{
			return Campaign.Current.Models.SmithingModel.CalculateWeaponDesignDifficulty(weaponDesign);
		}

		// Token: 0x06003CE1 RID: 15585 RVA: 0x00107948 File Offset: 0x00105B48
		private void InitializeLists()
		{
			if (this._craftingOrders.IsEmpty<KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots>>())
			{
				foreach (Town key in Campaign.Current.AllTowns)
				{
					this._craftingOrders.Add(key, new CraftingCampaignBehavior.CraftingOrderSlots());
				}
			}
			foreach (KeyValuePair<CraftingTemplate, List<CraftingPiece>> keyValuePair in this._openedPartsDictionary.ToList<KeyValuePair<CraftingTemplate, List<CraftingPiece>>>())
			{
				if (!CraftingTemplate.All.Contains(keyValuePair.Key))
				{
					this._openedPartsDictionary.Remove(keyValuePair.Key);
				}
			}
			foreach (KeyValuePair<CraftingTemplate, float> keyValuePair2 in this._openNewPartXpDictionary.ToList<KeyValuePair<CraftingTemplate, float>>())
			{
				if (!CraftingTemplate.All.Contains(keyValuePair2.Key))
				{
					this._openNewPartXpDictionary.Remove(keyValuePair2.Key);
				}
			}
			foreach (CraftingTemplate craftingTemplate in CraftingTemplate.All)
			{
				if (!this._openNewPartXpDictionary.ContainsKey(craftingTemplate))
				{
					this._openNewPartXpDictionary.Add(craftingTemplate, 0f);
				}
				if (!this._openedPartsDictionary.ContainsKey(craftingTemplate))
				{
					this._openedPartsDictionary.Add(craftingTemplate, new List<CraftingPiece>());
				}
				foreach (CraftingPiece item in this._openedPartsDictionary[craftingTemplate].ToList<CraftingPiece>())
				{
					if (!craftingTemplate.Pieces.Contains(item))
					{
						this._openedPartsDictionary[craftingTemplate].Remove(item);
					}
				}
			}
		}

		// Token: 0x06003CE2 RID: 15586 RVA: 0x00107B78 File Offset: 0x00105D78
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06003CE3 RID: 15587 RVA: 0x00107B84 File Offset: 0x00105D84
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("blacksmith_begin", "start", "blacksmith_player", "{=gYByVHQy}Good day, {?PLAYER.GENDER}madam{?}sir{\\?}. How may I help you?", new ConversationSentence.OnConditionDelegate(this.conversation_blacksmith_begin_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("blacksmith_craft_items", "blacksmith_player", "player_blacksmith_after_craft", "{=VXKGD0ta}I want to use your forge.", () => Campaign.Current.IsCraftingEnabled, new ConversationSentence.OnConsequenceDelegate(this.conversation_blacksmith_craft_items_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("blacksmith_leave", "blacksmith_player", "close_window", "{=iW9iKbb8}Nothing.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("blacksmith_player_after_craft_anything_else", "player_blacksmith_after_craft", "blacksmith_player_1", "{=IvY187PJ}No matter. Anything else?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("blacksmith_craft_items_1", "blacksmith_player_1", "player_blacksmith_after_craft", "{=hrn1Cdwo}There is something else I need you to make.", () => Campaign.Current.IsCraftingEnabled, new ConversationSentence.OnConsequenceDelegate(this.conversation_blacksmith_craft_items_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("blacksmith_leave_1", "blacksmith_player_1", "close_window", "{=iW9iKbb8}Nothing.", null, null, 100, null, null);
		}

		// Token: 0x06003CE4 RID: 15588 RVA: 0x00107CB2 File Offset: 0x00105EB2
		private bool conversation_blacksmith_begin_on_condition()
		{
			return CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Blacksmith;
		}

		// Token: 0x06003CE5 RID: 15589 RVA: 0x00107CC2 File Offset: 0x00105EC2
		private void conversation_blacksmith_craft_items_on_consequence()
		{
			CraftingHelper.OpenCrafting(CraftingTemplate.All[0], null);
		}

		// Token: 0x06003CE6 RID: 15590 RVA: 0x00107CD5 File Offset: 0x00105ED5
		public int GetHeroCraftingStamina(Hero hero)
		{
			return this.GetRecordForCompanion(hero).CraftingStamina;
		}

		// Token: 0x06003CE7 RID: 15591 RVA: 0x00107CE4 File Offset: 0x00105EE4
		private CraftingCampaignBehavior.HeroCraftingRecord GetRecordForCompanion(Hero hero)
		{
			CraftingCampaignBehavior.HeroCraftingRecord heroCraftingRecord;
			if (!this._heroCraftingRecords.TryGetValue(hero, out heroCraftingRecord))
			{
				heroCraftingRecord = new CraftingCampaignBehavior.HeroCraftingRecord(this.GetMaxHeroCraftingStamina(hero));
				this._heroCraftingRecords[hero] = heroCraftingRecord;
			}
			return heroCraftingRecord;
		}

		// Token: 0x06003CE8 RID: 15592 RVA: 0x00107D1C File Offset: 0x00105F1C
		public void SetHeroCraftingStamina(Hero hero, int value)
		{
			this.GetRecordForCompanion(hero).CraftingStamina = MathF.Max(0, value);
		}

		// Token: 0x06003CE9 RID: 15593 RVA: 0x00107D34 File Offset: 0x00105F34
		public void SetCraftedWeaponName(ItemObject craftedWeaponItem, TextObject name)
		{
			CraftingCampaignBehavior.CraftedItemInitializationData craftedItemInitializationData;
			if (this._craftedItemDictionary.TryGetValue(craftedWeaponItem, out craftedItemInitializationData))
			{
				this._craftedItemDictionary[craftedWeaponItem] = new CraftingCampaignBehavior.CraftedItemInitializationData(craftedItemInitializationData.CraftedData, name, craftedItemInitializationData.Culture);
			}
		}

		// Token: 0x06003CEA RID: 15594 RVA: 0x00107D6F File Offset: 0x00105F6F
		public int GetMaxHeroCraftingStamina(Hero hero)
		{
			return 100 + MathF.Round((float)hero.GetSkillValue(DefaultSkills.Crafting) * 0.5f);
		}

		// Token: 0x06003CEB RID: 15595 RVA: 0x00107D8C File Offset: 0x00105F8C
		public void DoRefinement(Hero hero, Crafting.RefiningFormula refineFormula)
		{
			ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
			if (refineFormula.Input1Count > 0)
			{
				ItemObject craftingMaterialItem = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem(refineFormula.Input1);
				itemRoster.AddToCounts(craftingMaterialItem, -refineFormula.Input1Count);
			}
			if (refineFormula.Input2Count > 0)
			{
				ItemObject craftingMaterialItem2 = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem(refineFormula.Input2);
				itemRoster.AddToCounts(craftingMaterialItem2, -refineFormula.Input2Count);
			}
			if (refineFormula.OutputCount > 0)
			{
				ItemObject craftingMaterialItem3 = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem(refineFormula.Output);
				itemRoster.AddToCounts(craftingMaterialItem3, refineFormula.OutputCount);
			}
			if (refineFormula.Output2Count > 0)
			{
				ItemObject craftingMaterialItem4 = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem(refineFormula.Output2);
				itemRoster.AddToCounts(craftingMaterialItem4, refineFormula.Output2Count);
			}
			hero.AddSkillXp(DefaultSkills.Crafting, (float)Campaign.Current.Models.SmithingModel.GetSkillXpForRefining(ref refineFormula));
			int energyCostForRefining = Campaign.Current.Models.SmithingModel.GetEnergyCostForRefining(ref refineFormula, hero);
			this.SetHeroCraftingStamina(hero, this.GetHeroCraftingStamina(hero) - energyCostForRefining);
			CampaignEventDispatcher.Instance.OnItemsRefined(hero, refineFormula);
		}

		// Token: 0x06003CEC RID: 15596 RVA: 0x00107EC8 File Offset: 0x001060C8
		public void DoSmelting(Hero currentCraftingHero, EquipmentElement equipmentElement)
		{
			ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
			ItemObject item = equipmentElement.Item;
			int[] smeltingOutputForItem = Campaign.Current.Models.SmithingModel.GetSmeltingOutputForItem(item);
			for (int i = 8; i >= 0; i--)
			{
				if (smeltingOutputForItem[i] != 0)
				{
					itemRoster.AddToCounts(Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem((CraftingMaterials)i), smeltingOutputForItem[i]);
				}
			}
			itemRoster.AddToCounts(equipmentElement, -1);
			currentCraftingHero.AddSkillXp(DefaultSkills.Crafting, (float)Campaign.Current.Models.SmithingModel.GetSkillXpForSmelting(item));
			int energyCostForSmelting = Campaign.Current.Models.SmithingModel.GetEnergyCostForSmelting(item, currentCraftingHero);
			this.SetHeroCraftingStamina(currentCraftingHero, this.GetHeroCraftingStamina(currentCraftingHero) - energyCostForSmelting);
			this.AddResearchPoints(item.WeaponDesign.Template, Campaign.Current.Models.SmithingModel.GetPartResearchGainForSmeltingItem(item, currentCraftingHero));
			CampaignEventDispatcher.Instance.OnEquipmentSmeltedByHero(currentCraftingHero, equipmentElement);
		}

		// Token: 0x06003CED RID: 15597 RVA: 0x00107FBC File Offset: 0x001061BC
		public ItemObject CreateCraftedWeaponInFreeBuildMode(Hero hero, WeaponDesign weaponDesign, ItemModifier weaponModifier = null)
		{
			ItemObject itemObject = this.CreateCraftedWeaponInternal(true, hero, weaponDesign, weaponModifier);
			int skillXpForSmithingInFreeBuildMode = Campaign.Current.Models.SmithingModel.GetSkillXpForSmithingInFreeBuildMode(itemObject);
			hero.AddSkillXp(DefaultSkills.Crafting, (float)skillXpForSmithingInFreeBuildMode);
			this.AddItemToHistory(itemObject);
			return itemObject;
		}

		// Token: 0x06003CEE RID: 15598 RVA: 0x00108000 File Offset: 0x00106200
		public ItemObject CreateCraftedWeaponInCraftingOrderMode(Hero crafterHero, CraftingOrder craftingOrder, WeaponDesign weaponDesign)
		{
			ItemObject itemObject = this.CreateCraftedWeaponInternal(false, crafterHero, weaponDesign, null);
			float xpAmount = craftingOrder.GetOrderExperience(itemObject, this._currentItemModifier) + (float)Campaign.Current.Models.SmithingModel.GetSkillXpForSmithingInCraftingOrderMode(itemObject);
			crafterHero.AddSkillXp(DefaultSkills.Crafting, xpAmount);
			return itemObject;
		}

		// Token: 0x06003CEF RID: 15599 RVA: 0x0010804C File Offset: 0x0010624C
		private ItemObject CreateCraftedWeaponInternal(bool isFreeMode, Hero crafterHero, WeaponDesign weaponDesign, ItemModifier weaponModifier = null)
		{
			string nextCraftedItemId = this.GetNextCraftedItemId();
			if (isFreeMode)
			{
				weaponDesign = new WeaponDesign(weaponDesign.Template, weaponDesign.WeaponName, weaponDesign.UsedPieces, nextCraftedItemId);
			}
			CraftingCampaignBehavior.SpendMaterials(weaponDesign);
			ItemObject currentCraftedItemObject = (GameStateManager.Current.ActiveState as CraftingState).CraftingLogic.GetCurrentCraftedItemObject(true, nextCraftedItemId);
			ItemObject.InitAsPlayerCraftedItem(ref currentCraftedItemObject);
			MBObjectManager.Instance.RegisterObject<ItemObject>(currentCraftedItemObject);
			if (isFreeMode)
			{
				if (weaponModifier == null)
				{
					PartyBase.MainParty.ItemRoster.AddToCounts(currentCraftedItemObject, 1);
				}
				else
				{
					EquipmentElement rosterElement = new EquipmentElement(currentCraftedItemObject, weaponModifier, null, false);
					PartyBase.MainParty.ItemRoster.AddToCounts(rosterElement, 1);
				}
			}
			CampaignEventDispatcher.Instance.OnNewItemCrafted(currentCraftedItemObject, weaponModifier, !isFreeMode);
			int energyCostForSmithing = Campaign.Current.Models.SmithingModel.GetEnergyCostForSmithing(currentCraftedItemObject, crafterHero);
			this.SetHeroCraftingStamina(crafterHero, this.GetHeroCraftingStamina(crafterHero) - energyCostForSmithing);
			this.AddResearchPoints(weaponDesign.Template, Campaign.Current.Models.SmithingModel.GetPartResearchGainForSmithingItem(currentCraftedItemObject, crafterHero, isFreeMode));
			return currentCraftedItemObject;
		}

		// Token: 0x06003CF0 RID: 15600 RVA: 0x00108148 File Offset: 0x00106348
		private static void SpendMaterials(WeaponDesign weaponDesign)
		{
			ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
			int[] smithingCostsForWeaponDesign = Campaign.Current.Models.SmithingModel.GetSmithingCostsForWeaponDesign(weaponDesign);
			for (int i = 8; i >= 0; i--)
			{
				if (smithingCostsForWeaponDesign[i] != 0)
				{
					itemRoster.AddToCounts(Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem((CraftingMaterials)i), smithingCostsForWeaponDesign[i]);
				}
			}
		}

		// Token: 0x06003CF1 RID: 15601 RVA: 0x001081A6 File Offset: 0x001063A6
		private void AddItemToHistory(ItemObject craftedObject)
		{
			while (this._cratingItemsHistory.Count >= 10)
			{
				this._cratingItemsHistory.RemoveAt(0);
			}
			this._cratingItemsHistory.Add(craftedObject);
		}

		// Token: 0x06003CF2 RID: 15602 RVA: 0x001081D1 File Offset: 0x001063D1
		public Hero GetActiveCraftingHero()
		{
			return this._activeCraftingHero;
		}

		// Token: 0x06003CF3 RID: 15603 RVA: 0x001081D9 File Offset: 0x001063D9
		public void SetActiveCraftingHero(Hero hero)
		{
			this._activeCraftingHero = hero;
		}

		// Token: 0x06003CF4 RID: 15604 RVA: 0x001081E4 File Offset: 0x001063E4
		public void CreateTownOrder(Hero orderOwner, int orderSlot)
		{
			if (orderOwner.CurrentSettlement == null || !orderOwner.CurrentSettlement.IsTown)
			{
				Debug.Print(string.Concat(new string[]
				{
					"Order owner: ",
					orderOwner.StringId,
					" Settlement",
					(orderOwner.CurrentSettlement == null) ? "null" : orderOwner.CurrentSettlement.StringId,
					" Order owner party: ",
					(orderOwner.PartyBelongedTo == null) ? "null" : orderOwner.PartyBelongedTo.StringId
				}), 0, Debug.DebugColor.White, 17592186044416UL);
			}
			float townOrderDifficulty = CraftingCampaignBehavior.GetTownOrderDifficulty(orderOwner.CurrentSettlement.Town, orderSlot);
			int pieceTier = (int)townOrderDifficulty / 50;
			CraftingTemplate randomElement = CraftingTemplate.All.GetRandomElement<CraftingTemplate>();
			string nextTownOrderId = this.GetNextTownOrderId();
			WeaponDesign weaponDesignTemplate = new WeaponDesign(randomElement, TextObject.GetEmpty(), this.GetWeaponPieces(randomElement, pieceTier), nextTownOrderId);
			this._craftingOrders[orderOwner.CurrentSettlement.Town].AddTownOrder(new CraftingOrder(orderOwner, townOrderDifficulty, weaponDesignTemplate, randomElement, orderSlot, nextTownOrderId));
		}

		// Token: 0x06003CF5 RID: 15605 RVA: 0x001082E4 File Offset: 0x001064E4
		private static float GetTownOrderDifficulty(Town town, int orderSlot)
		{
			int num = 0;
			switch (orderSlot)
			{
			case 0:
				num = MBRandom.RandomInt(40, 80);
				break;
			case 1:
				num = MBRandom.RandomInt(80, 120);
				break;
			case 2:
				num = MBRandom.RandomInt(120, 160);
				break;
			case 3:
				num = MBRandom.RandomInt(160, 200);
				break;
			case 4:
				num = MBRandom.RandomInt(200, 241);
				break;
			case 5:
				num = Hero.MainHero.GetSkillValue(DefaultSkills.Crafting);
				break;
			}
			return (float)num + town.Prosperity / 500f;
		}

		// Token: 0x06003CF6 RID: 15606 RVA: 0x00108380 File Offset: 0x00106580
		public CraftingOrder CreateCustomOrderForHero(Hero orderOwner, float orderDifficulty = -1f, WeaponDesign weaponDesign = null, CraftingTemplate craftingTemplate = null)
		{
			string nextTownOrderId = this.GetNextTownOrderId();
			if (orderDifficulty < 0f)
			{
				orderDifficulty = CraftingCampaignBehavior.GetRandomOrderDifficulty(orderOwner.CurrentSettlement.Town);
			}
			if (craftingTemplate == null)
			{
				craftingTemplate = CraftingTemplate.All.GetRandomElement<CraftingTemplate>();
			}
			if (weaponDesign == null)
			{
				int pieceTier = (int)orderDifficulty / 40;
				weaponDesign = new WeaponDesign(craftingTemplate, TextObject.GetEmpty(), this.GetWeaponPieces(craftingTemplate, pieceTier), nextTownOrderId);
			}
			CraftingOrder craftingOrder = new CraftingOrder(orderOwner, orderDifficulty, weaponDesign, craftingTemplate, -1, nextTownOrderId);
			this._craftingOrders[orderOwner.CurrentSettlement.Town].AddCustomOrder(craftingOrder);
			return craftingOrder;
		}

		// Token: 0x06003CF7 RID: 15607 RVA: 0x00108410 File Offset: 0x00106610
		private static float GetRandomOrderDifficulty(Town town)
		{
			int num = MBRandom.RandomInt(0, 6);
			int num2 = 0;
			switch (num)
			{
			case 0:
				num2 = MBRandom.RandomInt(40, 80);
				break;
			case 1:
				num2 = MBRandom.RandomInt(80, 120);
				break;
			case 2:
				num2 = MBRandom.RandomInt(120, 160);
				break;
			case 3:
				num2 = MBRandom.RandomInt(160, 200);
				break;
			case 4:
				num2 = MBRandom.RandomInt(200, 241);
				break;
			case 5:
				num2 = Hero.MainHero.GetSkillValue(DefaultSkills.Crafting);
				break;
			}
			return (float)num2 + town.Prosperity / 500f;
		}

		// Token: 0x06003CF8 RID: 15608 RVA: 0x001084B4 File Offset: 0x001066B4
		private WeaponDesignElement[] GetWeaponPieces(CraftingTemplate craftingTemplate, int pieceTier)
		{
			WeaponDesignElement[] array = new WeaponDesignElement[4];
			List<WeaponDesignElement>[] array2 = new List<WeaponDesignElement>[4];
			foreach (CraftingPiece craftingPiece in craftingTemplate.Pieces)
			{
				bool flag = false;
				foreach (PieceData pieceData in craftingTemplate.BuildOrders)
				{
					if (pieceData.PieceType == craftingPiece.PieceType)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					int pieceType = (int)craftingPiece.PieceType;
					if (array2[pieceType] == null)
					{
						array2[pieceType] = new List<WeaponDesignElement>();
					}
					array2[pieceType].Add(WeaponDesignElement.CreateUsablePiece(craftingPiece, 100));
				}
			}
			Func<WeaponDesignElement, bool> <>9__0;
			for (int j = 0; j < array.Length; j++)
			{
				if (array2[j] != null)
				{
					WeaponDesignElement[] array3 = array;
					int num = j;
					List<WeaponDesignElement> source = array2[j];
					Func<WeaponDesignElement, bool> predicate;
					if ((predicate = <>9__0) == null)
					{
						predicate = (<>9__0 = (WeaponDesignElement p) => !p.CraftingPiece.IsHiddenOnDesigner && p.CraftingPiece.PieceTier == pieceTier);
					}
					WeaponDesignElement weaponDesignElement;
					if ((weaponDesignElement = source.FirstOrDefaultQ(predicate)) == null)
					{
						weaponDesignElement = array2[j].FirstOrDefaultQ((WeaponDesignElement p) => !p.CraftingPiece.IsHiddenOnDesigner && p.CraftingPiece.PieceTier == 1);
					}
					WeaponDesignElement weaponDesignElement2;
					if ((weaponDesignElement2 = weaponDesignElement) == null)
					{
						weaponDesignElement2 = array2[j].First((WeaponDesignElement p) => !p.CraftingPiece.IsHiddenOnDesigner);
					}
					array3[num] = weaponDesignElement2;
				}
				else
				{
					array[j] = WeaponDesignElement.GetInvalidPieceForType((CraftingPiece.PieceTypes)j);
				}
			}
			return array;
		}

		// Token: 0x06003CF9 RID: 15609 RVA: 0x00108640 File Offset: 0x00106840
		private void ReplaceCraftingOrder(Town town, CraftingOrder order)
		{
			MBList<Hero> mblist = new MBList<Hero>();
			Settlement settlement = town.Settlement;
			mblist.AddRange(settlement.HeroesWithoutParty);
			foreach (MobileParty mobileParty in settlement.Parties)
			{
				if (mobileParty.LeaderHero != null && !mobileParty.IsMainParty)
				{
					mblist.Add(mobileParty.LeaderHero);
				}
			}
			int difficultyLevel = order.DifficultyLevel;
			this._craftingOrders[town].RemoveTownOrder(order);
			if (mblist.Count > 0)
			{
				this.CreateTownOrder(mblist.GetRandomElement<Hero>(), difficultyLevel);
			}
		}

		// Token: 0x06003CFA RID: 15610 RVA: 0x001086F4 File Offset: 0x001068F4
		public void GetOrderResult(CraftingOrder craftingOrder, ItemObject craftedItem, out bool isSucceed, out TextObject orderRemark, out TextObject orderResult, out int finalReward)
		{
			finalReward = this.CalculateOrderPriceDifference(craftingOrder, craftedItem);
			float num;
			float num2;
			bool flag;
			bool flag2;
			craftingOrder.CheckForBonusesAndPenalties(craftedItem, this._currentItemModifier, out num, out num2, out flag, out flag2);
			isSucceed = num >= num2 && flag && flag2;
			int num3 = finalReward - craftingOrder.BaseGoldReward;
			orderRemark = TextObject.GetEmpty();
			if (isSucceed)
			{
				orderResult = new TextObject("{=Nn49hU2W}The client is satisfied.", null);
				if (num3 == 0)
				{
					orderRemark = new TextObject("{=FWHvvZFq}\"This is exactly what I wanted. Here is your money, you've earned it.\"", null);
					return;
				}
				if ((float)num3 > 0f)
				{
					orderRemark = new TextObject("{=raCa7QXj}\"This is even better than what I have imagined. Here is your money, and I'm putting a little extra for your effort.\"", null);
					return;
				}
			}
			else
			{
				orderResult = new TextObject("{=bC2jevlu}The client is displeased.", null);
				if (finalReward <= 0)
				{
					orderRemark = new TextObject("{=NZynd8vT}\"This weapon is worthless. I'm not giving you a dime!\"", null);
					return;
				}
				if (finalReward < craftingOrder.BaseGoldReward)
				{
					TextObject textObject;
					if (!flag || !flag2)
					{
						textObject = new TextObject("{=WyuIksRB}\"This weapon does not have the damage type I wanted. I'm cutting {AMOUNT}{GOLD_ICON} from the price.\"", null);
					}
					else
					{
						textObject = new TextObject("{=wU76OPxM}\"This is worse than what I've asked for. I'm cutting {AMOUNT}{GOLD_ICON} from the price.\"", null);
					}
					textObject.SetTextVariable("AMOUNT", MathF.Abs(num3));
					orderRemark = textObject;
				}
			}
		}

		// Token: 0x06003CFB RID: 15611 RVA: 0x001087EC File Offset: 0x001069EC
		private int CalculateOrderPriceDifference(CraftingOrder craftingOrder, ItemObject craftedItem)
		{
			float num;
			float num2;
			bool flag;
			bool flag2;
			craftingOrder.CheckForBonusesAndPenalties(craftedItem, this._currentItemModifier, out num, out num2, out flag, out flag2);
			float num3 = (float)craftingOrder.BaseGoldReward;
			if (!num.ApproximatelyEqualsTo(0f, 1E-05f) && !num2.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				if (num < num2 || !flag || !flag2)
				{
					float b = (float)Campaign.Current.Models.TradeItemPriceFactorModel.GetTheoreticalMaxItemMarketValue(craftedItem) / (float)Campaign.Current.Models.TradeItemPriceFactorModel.GetTheoreticalMaxItemMarketValue(craftingOrder.PreCraftedWeaponDesignItem);
					num3 = (float)craftingOrder.BaseGoldReward * 0.5f * MathF.Min(1f, b);
					if (num3 > (float)craftingOrder.BaseGoldReward)
					{
						num3 = (float)craftingOrder.BaseGoldReward * 0.5f;
					}
				}
				else if (num > num2)
				{
					num3 = (float)craftingOrder.BaseGoldReward * (1f + (num - num2) / num2 * 0.1f);
				}
			}
			return (int)num3;
		}

		// Token: 0x06003CFC RID: 15612 RVA: 0x001088DC File Offset: 0x00106ADC
		public void CompleteOrder(Town town, CraftingOrder craftingOrder, ItemObject craftedItem, Hero completerHero)
		{
			int amount = this.CalculateOrderPriceDifference(craftingOrder, craftedItem);
			bool flag;
			TextObject textObject;
			TextObject textObject2;
			int num;
			this.GetOrderResult(craftingOrder, craftedItem, out flag, out textObject, out textObject2, out num);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount, false);
			if (this._craftingOrders[town].CustomOrders.Contains(craftingOrder))
			{
				this._craftingOrders[town].RemoveCustomOrder(craftingOrder);
			}
			else
			{
				if (craftingOrder.IsLordOrder)
				{
					this.ChangeCraftedOrderWithTheNoblesWeaponIfItIsBetter(craftedItem, craftingOrder);
					if (craftingOrder.OrderOwner.PartyBelongedTo != null)
					{
						this.GiveTroopToNobleAtWeaponTier((int)craftedItem.Tier, craftingOrder.OrderOwner);
					}
					if (flag && completerHero.GetPerkValue(DefaultPerks.Crafting.SteelMaker3))
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(completerHero, craftingOrder.OrderOwner, (int)DefaultPerks.Crafting.SteelMaker3.SecondaryBonus, true);
					}
				}
				else
				{
					craftingOrder.OrderOwner.AddPower((float)(craftedItem.Tier + 1));
					if (flag && completerHero.GetPerkValue(DefaultPerks.Crafting.ExperiencedSmith))
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(completerHero, craftingOrder.OrderOwner, (int)DefaultPerks.Crafting.ExperiencedSmith.SecondaryBonus, true);
					}
				}
				this._craftingOrders[town].RemoveTownOrder(craftingOrder);
			}
			CampaignEventDispatcher.Instance.OnCraftingOrderCompleted(town, craftingOrder, craftedItem, completerHero);
		}

		// Token: 0x06003CFD RID: 15613 RVA: 0x001089F9 File Offset: 0x00106BF9
		public ItemModifier GetCurrentItemModifier()
		{
			return this._currentItemModifier;
		}

		// Token: 0x06003CFE RID: 15614 RVA: 0x00108A01 File Offset: 0x00106C01
		public void SetCurrentItemModifier(ItemModifier modifier)
		{
			this._currentItemModifier = modifier;
		}

		// Token: 0x06003CFF RID: 15615 RVA: 0x00108A0C File Offset: 0x00106C0C
		private void RemoveOrdersOfHeroWithoutCompletionIfExists(Hero hero)
		{
			foreach (KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots> keyValuePair in this._craftingOrders)
			{
				for (int i = 0; i < 6; i++)
				{
					if (keyValuePair.Value.Slots[i] != null && keyValuePair.Value.Slots[i].OrderOwner == hero)
					{
						keyValuePair.Value.RemoveTownOrder(keyValuePair.Value.Slots[i]);
					}
				}
			}
		}

		// Token: 0x06003D00 RID: 15616 RVA: 0x00108AA4 File Offset: 0x00106CA4
		public void CancelCustomOrder(Town town, CraftingOrder craftingOrder)
		{
			if (this._craftingOrders[town].CustomOrders.Contains(craftingOrder))
			{
				this._craftingOrders[town].RemoveCustomOrder(craftingOrder);
				return;
			}
			Debug.FailedAssert("Trying to cancel a custom order that doesn't exist.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CraftingCampaignBehavior.cs", "CancelCustomOrder", 1408);
		}

		// Token: 0x06003D01 RID: 15617 RVA: 0x00108AF6 File Offset: 0x00106CF6
		private void CancelOrder(Town town, CraftingOrder craftingOrder)
		{
			this._craftingOrders[town].RemoveTownOrder(craftingOrder);
		}

		// Token: 0x06003D02 RID: 15618 RVA: 0x00108B0C File Offset: 0x00106D0C
		private void ChangeCraftedOrderWithTheNoblesWeaponIfItIsBetter(ItemObject craftedItem, CraftingOrder craftingOrder)
		{
			Equipment battleEquipment = craftingOrder.OrderOwner.BattleEquipment;
			for (int i = 0; i < 12; i++)
			{
				if (!battleEquipment[i].IsEmpty)
				{
					WeaponClass weaponClass = craftedItem.PrimaryWeapon.WeaponClass;
					WeaponComponentData primaryWeapon = battleEquipment[i].Item.PrimaryWeapon;
					WeaponClass? weaponClass2 = ((primaryWeapon != null) ? new WeaponClass?(primaryWeapon.WeaponClass) : null);
					if ((weaponClass == weaponClass2.GetValueOrDefault()) & (weaponClass2 != null))
					{
						ItemObject item = battleEquipment[i].Item;
						int thrustSpeed = item.PrimaryWeapon.ThrustSpeed;
						int thrustSpeed2 = craftedItem.PrimaryWeapon.ThrustSpeed;
						int swingSpeed = item.PrimaryWeapon.SwingSpeed;
						int swingSpeed2 = craftedItem.PrimaryWeapon.SwingSpeed;
						int missileSpeed = item.PrimaryWeapon.MissileSpeed;
						int missileSpeed2 = craftedItem.PrimaryWeapon.MissileSpeed;
						float weaponBalance = item.PrimaryWeapon.WeaponBalance;
						float weaponBalance2 = craftedItem.PrimaryWeapon.WeaponBalance;
						int thrustDamage = item.PrimaryWeapon.ThrustDamage;
						int thrustDamage2 = craftedItem.PrimaryWeapon.ThrustDamage;
						DamageTypes thrustDamageType = item.PrimaryWeapon.ThrustDamageType;
						DamageTypes thrustDamageType2 = craftedItem.PrimaryWeapon.ThrustDamageType;
						int swingDamage = item.PrimaryWeapon.SwingDamage;
						int swingDamage2 = craftedItem.PrimaryWeapon.SwingDamage;
						DamageTypes swingDamageType = item.PrimaryWeapon.SwingDamageType;
						DamageTypes swingDamageType2 = craftedItem.PrimaryWeapon.SwingDamageType;
						int accuracy = item.PrimaryWeapon.Accuracy;
						int accuracy2 = craftedItem.PrimaryWeapon.Accuracy;
						float weight = item.Weight;
						float weight2 = craftedItem.Weight;
						if (thrustSpeed2 > thrustSpeed && swingSpeed2 > swingSpeed && missileSpeed2 > missileSpeed && weaponBalance2 > weaponBalance && thrustDamage2 > thrustDamage && thrustDamageType == thrustDamageType2 && swingDamage2 > swingDamage && swingDamageType2 == swingDamageType && accuracy2 > accuracy && weight2 < weight)
						{
							battleEquipment[i] = new EquipmentElement(craftedItem, null, null, false);
							return;
						}
					}
				}
			}
		}

		// Token: 0x06003D03 RID: 15619 RVA: 0x00108CF4 File Offset: 0x00106EF4
		private void GiveTroopToNobleAtWeaponTier(int tier, Hero noble)
		{
			CharacterObject characterObject = noble.Culture.BasicTroop;
			for (int i = 0; i < tier; i++)
			{
				if (characterObject.UpgradeTargets.Length != 0)
				{
					characterObject = characterObject.UpgradeTargets.GetRandomElement<CharacterObject>();
				}
			}
			noble.PartyBelongedTo.AddElementToMemberRoster(characterObject, 1, false);
		}

		// Token: 0x04001270 RID: 4720
		private const float CraftingOrderReplaceChance = 0.05f;

		// Token: 0x04001271 RID: 4721
		private const float CreateCraftingOrderChance = 0.05f;

		// Token: 0x04001272 RID: 4722
		private const int TownCraftingOrderCount = 6;

		// Token: 0x04001273 RID: 4723
		private const int DefaultCraftingOrderPieceTier = 1;

		// Token: 0x04001274 RID: 4724
		private const int CraftingOrderTroopBonusAmount = 1;

		// Token: 0x04001275 RID: 4725
		private const int MinOrderDifficulty = 40;

		// Token: 0x04001276 RID: 4726
		private const int MaxOrderDifficulty = 240;

		// Token: 0x04001277 RID: 4727
		private const int MaxCraftingHistoryDesigns = 10;

		// Token: 0x04001278 RID: 4728
		private const int BaseHeroCraftingStamina = 100;

		// Token: 0x04001279 RID: 4729
		private Hero _activeCraftingHero;

		// Token: 0x0400127A RID: 4730
		private ItemModifier _currentItemModifier;

		// Token: 0x0400127B RID: 4731
		private Dictionary<CraftingTemplate, List<CraftingPiece>> _openedPartsDictionary = new Dictionary<CraftingTemplate, List<CraftingPiece>>();

		// Token: 0x0400127C RID: 4732
		private Dictionary<CraftingTemplate, float> _openNewPartXpDictionary = new Dictionary<CraftingTemplate, float>();

		// Token: 0x0400127D RID: 4733
		private Dictionary<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData> _craftedItemDictionary = new Dictionary<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData>();

		// Token: 0x0400127E RID: 4734
		private Dictionary<Hero, CraftingCampaignBehavior.HeroCraftingRecord> _heroCraftingRecords = new Dictionary<Hero, CraftingCampaignBehavior.HeroCraftingRecord>();

		// Token: 0x0400127F RID: 4735
		private Dictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots> _craftingOrders = new Dictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots>();

		// Token: 0x04001280 RID: 4736
		private List<ItemObject> _cratingItemsHistory = new List<ItemObject>();

		// Token: 0x04001281 RID: 4737
		private int _townOrderCount;

		// Token: 0x04001282 RID: 4738
		private int _craftedItemCount;

		// Token: 0x020007D0 RID: 2000
		public class CraftingCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x060062B0 RID: 25264 RVA: 0x001BB6B1 File Offset: 0x001B98B1
			public CraftingCampaignBehaviorTypeDefiner()
				: base(150000)
			{
			}

			// Token: 0x060062B1 RID: 25265 RVA: 0x001BB6BE File Offset: 0x001B98BE
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(CraftingCampaignBehavior.CraftedItemInitializationData), 10, null);
				base.AddClassDefinition(typeof(CraftingCampaignBehavior.HeroCraftingRecord), 20, null);
				base.AddClassDefinition(typeof(CraftingCampaignBehavior.CraftingOrderSlots), 30, null);
			}

			// Token: 0x060062B2 RID: 25266 RVA: 0x001BB6FC File Offset: 0x001B98FC
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(Dictionary<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData>));
				base.ConstructContainerDefinition(typeof(Dictionary<Hero, CraftingCampaignBehavior.HeroCraftingRecord>));
				base.ConstructContainerDefinition(typeof(Dictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots>));
				base.ConstructContainerDefinition(typeof(List<CraftingOrder>));
			}
		}

		// Token: 0x020007D1 RID: 2001
		internal class CraftedItemInitializationData
		{
			// Token: 0x060062B3 RID: 25267 RVA: 0x001BB749 File Offset: 0x001B9949
			public CraftedItemInitializationData(WeaponDesign craftedData, TextObject itemName, CultureObject culture)
			{
				this.CraftedData = craftedData;
				this.ItemName = itemName;
				this.Culture = culture;
			}

			// Token: 0x060062B4 RID: 25268 RVA: 0x001BB766 File Offset: 0x001B9966
			internal static void AutoGeneratedStaticCollectObjectsCraftedItemInitializationData(object o, List<object> collectedObjects)
			{
				((CraftingCampaignBehavior.CraftedItemInitializationData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060062B5 RID: 25269 RVA: 0x001BB774 File Offset: 0x001B9974
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.CraftedData);
				collectedObjects.Add(this.ItemName);
				collectedObjects.Add(this.Culture);
			}

			// Token: 0x060062B6 RID: 25270 RVA: 0x001BB79A File Offset: 0x001B999A
			internal static object AutoGeneratedGetMemberValueCraftedData(object o)
			{
				return ((CraftingCampaignBehavior.CraftedItemInitializationData)o).CraftedData;
			}

			// Token: 0x060062B7 RID: 25271 RVA: 0x001BB7A7 File Offset: 0x001B99A7
			internal static object AutoGeneratedGetMemberValueItemName(object o)
			{
				return ((CraftingCampaignBehavior.CraftedItemInitializationData)o).ItemName;
			}

			// Token: 0x060062B8 RID: 25272 RVA: 0x001BB7B4 File Offset: 0x001B99B4
			internal static object AutoGeneratedGetMemberValueCulture(object o)
			{
				return ((CraftingCampaignBehavior.CraftedItemInitializationData)o).Culture;
			}

			// Token: 0x04001F30 RID: 7984
			[SaveableField(10)]
			public readonly WeaponDesign CraftedData;

			// Token: 0x04001F31 RID: 7985
			[SaveableField(20)]
			public readonly TextObject ItemName;

			// Token: 0x04001F32 RID: 7986
			[SaveableField(30)]
			public readonly CultureObject Culture;
		}

		// Token: 0x020007D2 RID: 2002
		internal class HeroCraftingRecord
		{
			// Token: 0x060062B9 RID: 25273 RVA: 0x001BB7C1 File Offset: 0x001B99C1
			public HeroCraftingRecord(int maxStamina)
			{
				this.CraftingStamina = maxStamina;
			}

			// Token: 0x060062BA RID: 25274 RVA: 0x001BB7D0 File Offset: 0x001B99D0
			internal static void AutoGeneratedStaticCollectObjectsHeroCraftingRecord(object o, List<object> collectedObjects)
			{
				((CraftingCampaignBehavior.HeroCraftingRecord)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060062BB RID: 25275 RVA: 0x001BB7DE File Offset: 0x001B99DE
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
			}

			// Token: 0x060062BC RID: 25276 RVA: 0x001BB7E0 File Offset: 0x001B99E0
			internal static object AutoGeneratedGetMemberValueCraftingStamina(object o)
			{
				return ((CraftingCampaignBehavior.HeroCraftingRecord)o).CraftingStamina;
			}

			// Token: 0x04001F33 RID: 7987
			[SaveableField(10)]
			public int CraftingStamina;
		}

		// Token: 0x020007D3 RID: 2003
		public class CraftingOrderSlots
		{
			// Token: 0x1700151C RID: 5404
			// (get) Token: 0x060062BD RID: 25277 RVA: 0x001BB7F2 File Offset: 0x001B99F2
			public MBReadOnlyList<CraftingOrder> CustomOrders
			{
				get
				{
					return this._customOrders;
				}
			}

			// Token: 0x060062BE RID: 25278 RVA: 0x001BB7FC File Offset: 0x001B99FC
			public CraftingOrderSlots()
			{
				this.Slots = new CraftingOrder[6];
				for (int i = 0; i < 6; i++)
				{
					this.Slots[i] = null;
				}
				this._customOrders = new MBList<CraftingOrder>();
			}

			// Token: 0x060062BF RID: 25279 RVA: 0x001BB83B File Offset: 0x001B9A3B
			[LoadInitializationCallback]
			private void OnLoad()
			{
				if (this._customOrders == null)
				{
					this._customOrders = new MBList<CraftingOrder>();
				}
			}

			// Token: 0x060062C0 RID: 25280 RVA: 0x001BB850 File Offset: 0x001B9A50
			public bool IsThereAvailableSlot()
			{
				for (int i = 0; i < 6; i++)
				{
					if (this.Slots[i] == null)
					{
						return true;
					}
				}
				return false;
			}

			// Token: 0x060062C1 RID: 25281 RVA: 0x001BB878 File Offset: 0x001B9A78
			public int GetAvailableSlot()
			{
				for (int i = 0; i < 6; i++)
				{
					if (this.Slots[i] == null)
					{
						return i;
					}
				}
				return -1;
			}

			// Token: 0x060062C2 RID: 25282 RVA: 0x001BB89E File Offset: 0x001B9A9E
			internal void AddTownOrder(CraftingOrder craftingOrder)
			{
				this.Slots[craftingOrder.DifficultyLevel] = craftingOrder;
			}

			// Token: 0x060062C3 RID: 25283 RVA: 0x001BB8AE File Offset: 0x001B9AAE
			internal void RemoveTownOrder(CraftingOrder craftingOrder)
			{
				this.Slots[craftingOrder.DifficultyLevel] = null;
			}

			// Token: 0x060062C4 RID: 25284 RVA: 0x001BB8BE File Offset: 0x001B9ABE
			internal void AddCustomOrder(CraftingOrder order)
			{
				this._customOrders.Add(order);
			}

			// Token: 0x060062C5 RID: 25285 RVA: 0x001BB8CC File Offset: 0x001B9ACC
			internal void RemoveCustomOrder(CraftingOrder order)
			{
				this._customOrders.Remove(order);
			}

			// Token: 0x060062C6 RID: 25286 RVA: 0x001BB8DB File Offset: 0x001B9ADB
			internal static void AutoGeneratedStaticCollectObjectsCraftingOrderSlots(object o, List<object> collectedObjects)
			{
				((CraftingCampaignBehavior.CraftingOrderSlots)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060062C7 RID: 25287 RVA: 0x001BB8E9 File Offset: 0x001B9AE9
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.Slots);
				collectedObjects.Add(this._customOrders);
			}

			// Token: 0x060062C8 RID: 25288 RVA: 0x001BB903 File Offset: 0x001B9B03
			internal static object AutoGeneratedGetMemberValueSlots(object o)
			{
				return ((CraftingCampaignBehavior.CraftingOrderSlots)o).Slots;
			}

			// Token: 0x060062C9 RID: 25289 RVA: 0x001BB910 File Offset: 0x001B9B10
			internal static object AutoGeneratedGetMemberValue_customOrders(object o)
			{
				return ((CraftingCampaignBehavior.CraftingOrderSlots)o)._customOrders;
			}

			// Token: 0x04001F34 RID: 7988
			private const int SlotCount = 6;

			// Token: 0x04001F35 RID: 7989
			[SaveableField(10)]
			public CraftingOrder[] Slots;

			// Token: 0x04001F36 RID: 7990
			[SaveableField(30)]
			private MBList<CraftingOrder> _customOrders;
		}
	}
}
