using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Inventory
{
	// Token: 0x020000D7 RID: 215
	public class InventoryLogic
	{
		// Token: 0x170005A2 RID: 1442
		// (get) Token: 0x06001475 RID: 5237 RVA: 0x0005E570 File Offset: 0x0005C770
		// (set) Token: 0x06001476 RID: 5238 RVA: 0x0005E578 File Offset: 0x0005C778
		public bool DisableNetwork { get; set; }

		// Token: 0x170005A3 RID: 1443
		// (get) Token: 0x06001477 RID: 5239 RVA: 0x0005E581 File Offset: 0x0005C781
		// (set) Token: 0x06001478 RID: 5240 RVA: 0x0005E589 File Offset: 0x0005C789
		public Action<int> TotalAmountChange { get; set; }

		// Token: 0x170005A4 RID: 1444
		// (get) Token: 0x06001479 RID: 5241 RVA: 0x0005E592 File Offset: 0x0005C792
		// (set) Token: 0x0600147A RID: 5242 RVA: 0x0005E59A File Offset: 0x0005C79A
		public Action DonationXpChange { get; set; }

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x0600147B RID: 5243 RVA: 0x0005E5A4 File Offset: 0x0005C7A4
		// (remove) Token: 0x0600147C RID: 5244 RVA: 0x0005E5DC File Offset: 0x0005C7DC
		public event InventoryLogic.AfterResetDelegate AfterReset;

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x0600147D RID: 5245 RVA: 0x0005E614 File Offset: 0x0005C814
		// (remove) Token: 0x0600147E RID: 5246 RVA: 0x0005E64C File Offset: 0x0005C84C
		public event InventoryLogic.ProcessResultListDelegate AfterTransfer;

		// Token: 0x170005A5 RID: 1445
		// (get) Token: 0x0600147F RID: 5247 RVA: 0x0005E681 File Offset: 0x0005C881
		// (set) Token: 0x06001480 RID: 5248 RVA: 0x0005E689 File Offset: 0x0005C889
		public TroopRoster RightMemberRoster { get; private set; }

		// Token: 0x170005A6 RID: 1446
		// (get) Token: 0x06001481 RID: 5249 RVA: 0x0005E692 File Offset: 0x0005C892
		// (set) Token: 0x06001482 RID: 5250 RVA: 0x0005E69A File Offset: 0x0005C89A
		public TroopRoster LeftMemberRoster { get; private set; }

		// Token: 0x170005A7 RID: 1447
		// (get) Token: 0x06001483 RID: 5251 RVA: 0x0005E6A3 File Offset: 0x0005C8A3
		// (set) Token: 0x06001484 RID: 5252 RVA: 0x0005E6AB File Offset: 0x0005C8AB
		public CharacterObject InitialEquipmentCharacter { get; private set; }

		// Token: 0x170005A8 RID: 1448
		// (get) Token: 0x06001485 RID: 5253 RVA: 0x0005E6B4 File Offset: 0x0005C8B4
		// (set) Token: 0x06001486 RID: 5254 RVA: 0x0005E6BC File Offset: 0x0005C8BC
		public bool IsTrading { get; private set; }

		// Token: 0x170005A9 RID: 1449
		// (get) Token: 0x06001487 RID: 5255 RVA: 0x0005E6C5 File Offset: 0x0005C8C5
		// (set) Token: 0x06001488 RID: 5256 RVA: 0x0005E6CD File Offset: 0x0005C8CD
		public bool IsSpecialActionsPermitted { get; private set; }

		// Token: 0x170005AA RID: 1450
		// (get) Token: 0x06001489 RID: 5257 RVA: 0x0005E6D6 File Offset: 0x0005C8D6
		// (set) Token: 0x0600148A RID: 5258 RVA: 0x0005E6DE File Offset: 0x0005C8DE
		public CharacterObject OwnerCharacter { get; private set; }

		// Token: 0x170005AB RID: 1451
		// (get) Token: 0x0600148B RID: 5259 RVA: 0x0005E6E7 File Offset: 0x0005C8E7
		// (set) Token: 0x0600148C RID: 5260 RVA: 0x0005E6EF File Offset: 0x0005C8EF
		public MobileParty OwnerParty { get; private set; }

		// Token: 0x170005AC RID: 1452
		// (get) Token: 0x0600148D RID: 5261 RVA: 0x0005E6F8 File Offset: 0x0005C8F8
		// (set) Token: 0x0600148E RID: 5262 RVA: 0x0005E700 File Offset: 0x0005C900
		public PartyBase OtherParty { get; private set; }

		// Token: 0x170005AD RID: 1453
		// (get) Token: 0x0600148F RID: 5263 RVA: 0x0005E709 File Offset: 0x0005C909
		// (set) Token: 0x06001490 RID: 5264 RVA: 0x0005E711 File Offset: 0x0005C911
		public IMarketData MarketData { get; private set; }

		// Token: 0x170005AE RID: 1454
		// (get) Token: 0x06001491 RID: 5265 RVA: 0x0005E71A File Offset: 0x0005C91A
		// (set) Token: 0x06001492 RID: 5266 RVA: 0x0005E722 File Offset: 0x0005C922
		public InventoryLogic.CapacityData OtherSideCapacityData { get; private set; }

		// Token: 0x170005AF RID: 1455
		// (get) Token: 0x06001493 RID: 5267 RVA: 0x0005E72C File Offset: 0x0005C92C
		public int OtherSideCurrentWeight
		{
			get
			{
				float num = 0f;
				PartyBase otherParty = this.OtherParty;
				MobileParty mobileParty = ((otherParty != null) ? otherParty.MobileParty : null);
				if (mobileParty != null)
				{
					ItemRoster itemRoster = this._rosters[0];
					InventoryCapacityModel inventoryCapacityModel = Campaign.Current.Models.InventoryCapacityModel;
					for (int i = 0; i < itemRoster.Count; i++)
					{
						TextObject textObject;
						num += inventoryCapacityModel.GetItemEffectiveWeight(itemRoster[i].EquipmentElement, mobileParty, mobileParty.IsCurrentlyAtSea, out textObject) * (float)itemRoster[i].Amount;
					}
				}
				else if (this._inventoryMode == InventoryScreenHelper.InventoryMode.Warehouse && this._workshopWarehouseBehavior != null)
				{
					num = this._workshopWarehouseBehavior.GetWarehouseItemRosterWeight(MobileParty.MainParty.CurrentSettlement);
				}
				return MathF.Ceiling(num);
			}
		}

		// Token: 0x170005B0 RID: 1456
		// (get) Token: 0x06001494 RID: 5268 RVA: 0x0005E7E8 File Offset: 0x0005C9E8
		// (set) Token: 0x06001495 RID: 5269 RVA: 0x0005E7F0 File Offset: 0x0005C9F0
		public TextObject LeftRosterName { get; private set; }

		// Token: 0x170005B1 RID: 1457
		// (get) Token: 0x06001496 RID: 5270 RVA: 0x0005E7F9 File Offset: 0x0005C9F9
		// (set) Token: 0x06001497 RID: 5271 RVA: 0x0005E801 File Offset: 0x0005CA01
		public bool IsDiscardDonating { get; private set; }

		// Token: 0x170005B2 RID: 1458
		// (get) Token: 0x06001498 RID: 5272 RVA: 0x0005E80A File Offset: 0x0005CA0A
		// (set) Token: 0x06001499 RID: 5273 RVA: 0x0005E812 File Offset: 0x0005CA12
		public bool IsOtherPartyFromPlayerClan { get; private set; }

		// Token: 0x170005B3 RID: 1459
		// (get) Token: 0x0600149A RID: 5274 RVA: 0x0005E81B File Offset: 0x0005CA1B
		// (set) Token: 0x0600149B RID: 5275 RVA: 0x0005E823 File Offset: 0x0005CA23
		public InventoryListener InventoryListener { get; private set; }

		// Token: 0x170005B4 RID: 1460
		// (get) Token: 0x0600149C RID: 5276 RVA: 0x0005E82C File Offset: 0x0005CA2C
		public int TotalAmount
		{
			get
			{
				return this.TransactionDebt;
			}
		}

		// Token: 0x170005B5 RID: 1461
		// (get) Token: 0x0600149D RID: 5277 RVA: 0x0005E834 File Offset: 0x0005CA34
		public PartyBase OppositePartyFromListener
		{
			get
			{
				return this.InventoryListener.GetOppositeParty();
			}
		}

		// Token: 0x170005B6 RID: 1462
		// (get) Token: 0x0600149E RID: 5278 RVA: 0x0005E841 File Offset: 0x0005CA41
		public SettlementComponent CurrentSettlementComponent
		{
			get
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				if (currentSettlement == null)
				{
					return null;
				}
				return currentSettlement.SettlementComponent;
			}
		}

		// Token: 0x170005B7 RID: 1463
		// (get) Token: 0x0600149F RID: 5279 RVA: 0x0005E854 File Offset: 0x0005CA54
		public MobileParty CurrentMobileParty
		{
			get
			{
				if (PlayerEncounter.Current != null)
				{
					return PlayerEncounter.EncounteredParty.MobileParty;
				}
				MapEvent mapEvent = PartyBase.MainParty.MapEvent;
				bool flag;
				if (mapEvent == null)
				{
					flag = null != null;
				}
				else
				{
					PartyBase leaderParty = mapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide);
					flag = ((leaderParty != null) ? leaderParty.MobileParty : null) != null;
				}
				if (flag)
				{
					return PartyBase.MainParty.MapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide).MobileParty;
				}
				return null;
			}
		}

		// Token: 0x170005B8 RID: 1464
		// (get) Token: 0x060014A0 RID: 5280 RVA: 0x0005E8C1 File Offset: 0x0005CAC1
		// (set) Token: 0x060014A1 RID: 5281 RVA: 0x0005E8C9 File Offset: 0x0005CAC9
		public int TransactionDebt
		{
			get
			{
				return this._transactionDebt;
			}
			private set
			{
				if (value != this._transactionDebt)
				{
					this._transactionDebt = value;
					this.TotalAmountChange(this._transactionDebt);
				}
			}
		}

		// Token: 0x170005B9 RID: 1465
		// (get) Token: 0x060014A2 RID: 5282 RVA: 0x0005E8EC File Offset: 0x0005CAEC
		// (set) Token: 0x060014A3 RID: 5283 RVA: 0x0005E8F4 File Offset: 0x0005CAF4
		public float XpGainFromDonations
		{
			get
			{
				return this._xpGainFromDonations;
			}
			private set
			{
				if (value != this._xpGainFromDonations)
				{
					this._xpGainFromDonations = value;
					if (this._xpGainFromDonations < 0f)
					{
						this._xpGainFromDonations = 0f;
					}
					Action donationXpChange = this.DonationXpChange;
					if (donationXpChange == null)
					{
						return;
					}
					donationXpChange();
				}
			}
		}

		// Token: 0x060014A4 RID: 5284 RVA: 0x0005E930 File Offset: 0x0005CB30
		public InventoryLogic(MobileParty ownerParty, CharacterObject ownerCharacter, PartyBase merchantParty)
		{
			this._rosters = new ItemRoster[2];
			this._rostersBackup = new ItemRoster[2];
			this.OwnerParty = ownerParty;
			this.OwnerCharacter = ownerCharacter;
			this.OtherParty = merchantParty;
		}

		// Token: 0x060014A5 RID: 5285 RVA: 0x0005E98D File Offset: 0x0005CB8D
		public InventoryLogic(PartyBase merchantParty)
			: this(MobileParty.MainParty, CharacterObject.PlayerCharacter, merchantParty)
		{
		}

		// Token: 0x060014A6 RID: 5286 RVA: 0x0005E9A0 File Offset: 0x0005CBA0
		public void Initialize(ItemRoster leftItemRoster, MobileParty party, bool isTrading, bool isSpecialActionsPermitted, CharacterObject initialCharacterOfRightRoster, InventoryScreenHelper.InventoryCategoryType merchantItemType, IMarketData marketData, bool useBasePrices, InventoryScreenHelper.InventoryMode inventoryMode, TextObject leftRosterName = null, TroopRoster leftMemberRoster = null, InventoryLogic.CapacityData otherSideCapacityData = null)
		{
			this.Initialize(leftItemRoster, party.ItemRoster, party.MemberRoster, isTrading, isSpecialActionsPermitted, initialCharacterOfRightRoster, merchantItemType, marketData, useBasePrices, inventoryMode, leftRosterName, leftMemberRoster, otherSideCapacityData);
		}

		// Token: 0x060014A7 RID: 5287 RVA: 0x0005E9D4 File Offset: 0x0005CBD4
		public void Initialize(ItemRoster leftItemRoster, ItemRoster rightItemRoster, TroopRoster rightMemberRoster, bool isTrading, bool isSpecialActionsPermitted, CharacterObject initialCharacterOfRightRoster, InventoryScreenHelper.InventoryCategoryType merchantItemType, IMarketData marketData, bool useBasePrices, InventoryScreenHelper.InventoryMode inventoryMode, TextObject leftRosterName = null, TroopRoster leftMemberRoster = null, InventoryLogic.CapacityData otherSideCapacityData = null)
		{
			this.OtherSideCapacityData = otherSideCapacityData;
			this.MarketData = marketData;
			this.TransactionDebt = 0;
			this.MerchantItemType = merchantItemType;
			this.InventoryListener = new FakeInventoryListener();
			this._useBasePrices = useBasePrices;
			this.LeftRosterName = leftRosterName;
			this.IsTrading = isTrading;
			this.IsSpecialActionsPermitted = isSpecialActionsPermitted;
			this._inventoryMode = inventoryMode;
			this.InitializeRosters(leftItemRoster, rightItemRoster, rightMemberRoster, initialCharacterOfRightRoster, leftMemberRoster);
			this._transactionHistory.Clear();
			this.InitializeCategoryAverages();
			this.IsDiscardDonating = (this._inventoryMode == InventoryScreenHelper.InventoryMode.Default && !Game.Current.CheatMode) || this._inventoryMode == InventoryScreenHelper.InventoryMode.Loot;
			this.InitializeXpGainFromDonations();
			PartyBase otherParty = this.OtherParty;
			Clan clan;
			if (otherParty == null)
			{
				clan = null;
			}
			else
			{
				MobileParty mobileParty = otherParty.MobileParty;
				clan = ((mobileParty != null) ? mobileParty.ActualClan : null);
			}
			if (clan == Hero.MainHero.Clan)
			{
				this.IsOtherPartyFromPlayerClan = true;
			}
			if (this._inventoryMode == InventoryScreenHelper.InventoryMode.Warehouse)
			{
				this._workshopWarehouseBehavior = Campaign.Current.GetCampaignBehavior<IWorkshopWarehouseCampaignBehavior>();
			}
		}

		// Token: 0x060014A8 RID: 5288 RVA: 0x0005EAC7 File Offset: 0x0005CCC7
		private void InitializeRosters(ItemRoster leftItemRoster, ItemRoster rightItemRoster, TroopRoster rightMemberRoster, CharacterObject initialCharacterOfRightRoster, TroopRoster leftMemberRoster = null)
		{
			this._rosters[0] = leftItemRoster;
			this._rosters[1] = rightItemRoster;
			this.RightMemberRoster = rightMemberRoster;
			this.LeftMemberRoster = leftMemberRoster;
			this.InitialEquipmentCharacter = initialCharacterOfRightRoster;
			this.SetCurrentStateAsInitial();
		}

		// Token: 0x060014A9 RID: 5289 RVA: 0x0005EAF8 File Offset: 0x0005CCF8
		public int GetItemTotalPrice(ItemRosterElement itemRosterElement, int absStockChange, out int lastPrice, bool isBuying)
		{
			lastPrice = this.GetItemPrice(itemRosterElement.EquipmentElement, isBuying);
			return lastPrice;
		}

		// Token: 0x060014AA RID: 5290 RVA: 0x0005EB0D File Offset: 0x0005CD0D
		public void SetPlayerAcceptTraderOffer()
		{
			this._playerAcceptsTraderOffer = true;
		}

		// Token: 0x060014AB RID: 5291 RVA: 0x0005EB18 File Offset: 0x0005CD18
		public bool DoneLogic()
		{
			if (this.IsPreviewingItem)
			{
				return false;
			}
			SettlementComponent currentSettlementComponent = this.CurrentSettlementComponent;
			MobileParty currentMobileParty = this.CurrentMobileParty;
			PartyBase partyBase = null;
			if (currentMobileParty != null)
			{
				partyBase = currentMobileParty.Party;
			}
			else if (currentSettlementComponent != null)
			{
				partyBase = currentSettlementComponent.Owner;
			}
			if (!this._playerAcceptsTraderOffer)
			{
				InventoryListener inventoryListener = this.InventoryListener;
				int? num = ((inventoryListener != null) ? new int?(inventoryListener.GetGold()) : null) + this.TotalAmount;
				int num2 = 0;
				bool flag = (num.GetValueOrDefault() < num2) & (num != null);
			}
			if (this.InventoryListener != null && this.IsTrading && this.OwnerCharacter.HeroObject.Gold - this.TotalAmount < 0)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_warning_you_dont_have_enough_money", null), 0, null, null, "");
				return false;
			}
			if (this._playerAcceptsTraderOffer)
			{
				this._playerAcceptsTraderOffer = false;
				if (this.InventoryListener != null)
				{
					int gold = this.InventoryListener.GetGold();
					this.TransactionDebt = -gold;
				}
			}
			if (this.OwnerCharacter != null && this.OwnerCharacter.HeroObject != null && this.IsTrading)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, this.OwnerCharacter.HeroObject, MathF.Min(-this.TotalAmount, this.InventoryListener.GetGold()), false);
				if (currentSettlementComponent != null && currentSettlementComponent.IsTown && this.OwnerCharacter.GetPerkValue(DefaultPerks.Trade.TrickleDown))
				{
					int num3 = 0;
					List<ValueTuple<ItemRosterElement, int>> boughtItems = this._transactionHistory.GetBoughtItems();
					int num4 = 0;
					while (boughtItems != null && num4 < boughtItems.Count)
					{
						ItemObject item = boughtItems[num4].Item1.EquipmentElement.Item;
						if (item != null && item.IsTradeGood)
						{
							num3 += boughtItems[num4].Item2;
						}
						num4++;
					}
					if (num3 >= 10000)
					{
						for (int i = 0; i < currentSettlementComponent.Settlement.Notables.Count; i++)
						{
							if (currentSettlementComponent.Settlement.Notables[i].IsMerchant)
							{
								ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentSettlementComponent.Settlement.Notables[i], this.OwnerCharacter.HeroObject, MathF.Floor(DefaultPerks.Trade.TrickleDown.PrimaryBonus), true);
							}
						}
					}
				}
			}
			if (this.IsDiscardDonating)
			{
				CampaignEventDispatcher.Instance.OnItemsDiscardedByPlayer(this._rosters[0]);
			}
			CampaignEventDispatcher.Instance.OnPlayerInventoryExchange(this._transactionHistory.GetBoughtItems(), this._transactionHistory.GetSoldItems(), this.IsTrading);
			if (currentSettlementComponent != null && this.InventoryListener != null && this.IsTrading)
			{
				this.InventoryListener.SetGold(this.InventoryListener.GetGold() + this.TotalAmount);
			}
			else if (((currentMobileParty != null) ? currentMobileParty.Party.LeaderHero : null) != null && this.IsTrading)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, this.CurrentMobileParty.Party.LeaderHero, this.TotalAmount, false);
				if (this.CurrentMobileParty.Party.LeaderHero.CompanionOf != null)
				{
					this.CurrentMobileParty.AddTaxGold((int)((float)this.TotalAmount * 0.1f));
				}
			}
			else if (partyBase != null && partyBase.LeaderHero == null && this.IsTrading)
			{
				GiveGoldAction.ApplyForCharacterToParty(null, partyBase, this.TotalAmount, false);
			}
			this._partyInitialEquipment = new InventoryLogic.PartyEquipment(this.OwnerParty);
			return true;
		}

		// Token: 0x060014AC RID: 5292 RVA: 0x0005EE99 File Offset: 0x0005D099
		public List<ValueTuple<ItemRosterElement, int>> GetBoughtItems()
		{
			return this._transactionHistory.GetBoughtItems();
		}

		// Token: 0x060014AD RID: 5293 RVA: 0x0005EEA6 File Offset: 0x0005D0A6
		public List<ValueTuple<ItemRosterElement, int>> GetSoldItems()
		{
			return this._transactionHistory.GetSoldItems();
		}

		// Token: 0x060014AE RID: 5294 RVA: 0x0005EEB3 File Offset: 0x0005D0B3
		public bool CanInventoryCapacityIncrease(InventoryLogic.InventorySide side)
		{
			return this._inventoryMode != InventoryScreenHelper.InventoryMode.Warehouse || side > InventoryLogic.InventorySide.OtherInventory;
		}

		// Token: 0x060014AF RID: 5295 RVA: 0x0005EEC4 File Offset: 0x0005D0C4
		public bool GetCanItemIncreaseInventoryCapacity(ItemObject item)
		{
			return item.HasHorseComponent;
		}

		// Token: 0x060014B0 RID: 5296 RVA: 0x0005EECC File Offset: 0x0005D0CC
		private void InitializeCategoryAverages()
		{
			if (Campaign.Current != null && Settlement.CurrentSettlement != null)
			{
				Town town = (Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.Village.Bound.Town : Settlement.CurrentSettlement.Town);
				foreach (ItemCategory itemCategory in ItemCategories.All)
				{
					float num = 0f;
					for (int i = 0; i < Town.AllTowns.Count; i++)
					{
						if (Town.AllTowns[i] != town)
						{
							num += Town.AllTowns[i].MarketData.GetPriceFactor(itemCategory);
						}
					}
					float num2 = num / (float)(Town.AllTowns.Count - 1);
					this._itemCategoryAverages.Add(itemCategory, num2);
					Debug.Print(string.Format("Average value of {0} : {1}", itemCategory.GetName(), num2), 0, Debug.DebugColor.White, 17592186044416UL);
				}
			}
		}

		// Token: 0x060014B1 RID: 5297 RVA: 0x0005EFF0 File Offset: 0x0005D1F0
		private void InitializeXpGainFromDonations()
		{
			this.XpGainFromDonations = 0f;
			bool flag = PerkHelper.PlayerHasAnyItemDonationPerk();
			bool flag2 = this._inventoryMode == InventoryScreenHelper.InventoryMode.Loot;
			if (flag && flag2)
			{
				this.XpGainFromDonations = (float)Campaign.Current.Models.ItemDiscardModel.GetXpBonusForDiscardingItems(this._rosters[0]);
			}
		}

		// Token: 0x060014B2 RID: 5298 RVA: 0x0005F040 File Offset: 0x0005D240
		private void HandleDonationOnTransferItem(ItemRosterElement rosterElement, int amount, bool isBuying, bool isSelling)
		{
			ItemObject item = rosterElement.EquipmentElement.Item;
			ItemDiscardModel itemDiscardModel = Campaign.Current.Models.ItemDiscardModel;
			if (this.IsDiscardDonating && (isSelling || isBuying) && item != null)
			{
				this.XpGainFromDonations += (float)(itemDiscardModel.GetXpBonusForDiscardingItem(item, amount) * (isSelling ? 1 : (-1)));
			}
		}

		// Token: 0x060014B3 RID: 5299 RVA: 0x0005F09D File Offset: 0x0005D29D
		public float GetAveragePriceFactorItemCategory(ItemCategory category)
		{
			if (this._itemCategoryAverages.ContainsKey(category))
			{
				return this._itemCategoryAverages[category];
			}
			return -99f;
		}

		// Token: 0x060014B4 RID: 5300 RVA: 0x0005F0BF File Offset: 0x0005D2BF
		public bool IsThereAnyChanges()
		{
			return this.IsThereAnyChangeBetweenRosters(this._rosters[1], this._rostersBackup[1]) || !this._partyInitialEquipment.IsEqual(new InventoryLogic.PartyEquipment(this.OwnerParty));
		}

		// Token: 0x060014B5 RID: 5301 RVA: 0x0005F0F4 File Offset: 0x0005D2F4
		private bool IsThereAnyChangeBetweenRosters(ItemRoster roster1, ItemRoster roster2)
		{
			if (roster1.Count != roster2.Count)
			{
				return true;
			}
			using (IEnumerator<ItemRosterElement> enumerator = roster1.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ItemRosterElement item = enumerator.Current;
					if (!roster2.Any((ItemRosterElement e) => e.IsEqualTo(item)))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060014B6 RID: 5302 RVA: 0x0005F16C File Offset: 0x0005D36C
		public void Reset(bool fromCancel)
		{
			this.ResetLogic(fromCancel);
		}

		// Token: 0x060014B7 RID: 5303 RVA: 0x0005F178 File Offset: 0x0005D378
		private void ResetLogic(bool fromCancel)
		{
			Debug.Print("InventoryLogic::Reset", 0, Debug.DebugColor.White, 17592186044416UL);
			for (int i = 0; i < 2; i++)
			{
				this._rosters[i].Clear();
				this._rosters[i].Add(this._rostersBackup[i]);
			}
			this.TransactionDebt = 0;
			this._transactionHistory.Clear();
			this.InitializeXpGainFromDonations();
			this._partyInitialEquipment.ResetEquipment(this.OwnerParty);
			InventoryLogic.AfterResetDelegate afterReset = this.AfterReset;
			if (afterReset != null)
			{
				afterReset(this, fromCancel);
			}
			List<TransferCommandResult> resultList = new List<TransferCommandResult>();
			if (!fromCancel)
			{
				this.OnAfterTransfer(resultList);
			}
		}

		// Token: 0x060014B8 RID: 5304 RVA: 0x0005F218 File Offset: 0x0005D418
		public bool CanPlayerCompleteTransaction()
		{
			InventoryLogic.CapacityData otherSideCapacityData = this.OtherSideCapacityData;
			int num = ((otherSideCapacityData != null) ? otherSideCapacityData.GetCapacity() : (-1));
			return (num == -1 || this.OtherSideCurrentWeight <= num || this.OtherSideCapacityData.CanForceTransaction()) && (!this.IsPreviewingItem || !this.IsTrading || this.TotalAmount <= 0 || (this.TotalAmount >= 0 && this.OwnerCharacter.HeroObject.Gold - this.TotalAmount >= 0));
		}

		// Token: 0x060014B9 RID: 5305 RVA: 0x0005F298 File Offset: 0x0005D498
		public bool CanSlaughterItem(ItemRosterElement element, InventoryLogic.InventorySide sideOfItem)
		{
			return (!this.IsTrading || this._transactionHistory.IsEmpty) && (this.IsSpecialActionsPermitted && this.IsSlaughterable(element.EquipmentElement.Item) && sideOfItem == InventoryLogic.InventorySide.PlayerInventory && element.Amount > 0) && !this._transactionHistory.GetBoughtItems().Any((ValueTuple<ItemRosterElement, int> i) => i.Item1.EquipmentElement.Item == element.EquipmentElement.Item);
		}

		// Token: 0x060014BA RID: 5306 RVA: 0x0005F31F File Offset: 0x0005D51F
		public bool IsSlaughterable(ItemObject item)
		{
			return item.Type == ItemObject.ItemTypeEnum.Animal || item.Type == ItemObject.ItemTypeEnum.Horse;
		}

		// Token: 0x060014BB RID: 5307 RVA: 0x0005F338 File Offset: 0x0005D538
		public bool CanDonateItem(ItemRosterElement element, InventoryLogic.InventorySide sideOfItem)
		{
			return Game.Current.IsDevelopmentMode && this.IsSpecialActionsPermitted && element.Amount > 0 && this.IsDonatable(element.EquipmentElement.Item) && sideOfItem == InventoryLogic.InventorySide.PlayerInventory;
		}

		// Token: 0x060014BC RID: 5308 RVA: 0x0005F380 File Offset: 0x0005D580
		public bool IsDonatable(ItemObject item)
		{
			return item.Type == ItemObject.ItemTypeEnum.Arrows || item.Type == ItemObject.ItemTypeEnum.BodyArmor || item.Type == ItemObject.ItemTypeEnum.Bolts || item.Type == ItemObject.ItemTypeEnum.SlingStones || item.Type == ItemObject.ItemTypeEnum.Bow || item.Type == ItemObject.ItemTypeEnum.Bullets || item.Type == ItemObject.ItemTypeEnum.Cape || item.Type == ItemObject.ItemTypeEnum.ChestArmor || item.Type == ItemObject.ItemTypeEnum.Crossbow || item.Type == ItemObject.ItemTypeEnum.Sling || item.Type == ItemObject.ItemTypeEnum.HandArmor || item.Type == ItemObject.ItemTypeEnum.HeadArmor || item.Type == ItemObject.ItemTypeEnum.HorseHarness || item.Type == ItemObject.ItemTypeEnum.LegArmor || item.Type == ItemObject.ItemTypeEnum.Musket || item.Type == ItemObject.ItemTypeEnum.OneHandedWeapon || item.Type == ItemObject.ItemTypeEnum.Pistol || item.Type == ItemObject.ItemTypeEnum.Polearm || item.Type == ItemObject.ItemTypeEnum.Shield || item.Type == ItemObject.ItemTypeEnum.Thrown || item.Type == ItemObject.ItemTypeEnum.TwoHandedWeapon;
		}

		// Token: 0x060014BD RID: 5309 RVA: 0x0005F46F File Offset: 0x0005D66F
		public void SetInventoryListener(InventoryListener inventoryListener)
		{
			this.InventoryListener = inventoryListener;
		}

		// Token: 0x060014BE RID: 5310 RVA: 0x0005F478 File Offset: 0x0005D678
		public int GetItemPrice(EquipmentElement equipmentElement, bool isBuying = false)
		{
			bool flag = !isBuying;
			bool flag2 = false;
			int result = 0;
			int num;
			bool flag3;
			if (this._transactionHistory.GetLastTransfer(equipmentElement, out num, out flag3) && flag3 != flag)
			{
				flag2 = true;
				result = num;
			}
			if (this._useBasePrices)
			{
				return equipmentElement.GetBaseValue();
			}
			if (flag2)
			{
				return result;
			}
			return this.MarketData.GetPrice(equipmentElement, this.OwnerParty, flag, this.OtherParty);
		}

		// Token: 0x060014BF RID: 5311 RVA: 0x0005F4D8 File Offset: 0x0005D6D8
		public int GetCostOfItemRosterElement(ItemRosterElement itemRosterElement, InventoryLogic.InventorySide side)
		{
			bool isBuying = side == InventoryLogic.InventorySide.OtherInventory && this.IsTrading;
			return this.GetItemPrice(itemRosterElement.EquipmentElement, isBuying);
		}

		// Token: 0x060014C0 RID: 5312 RVA: 0x0005F500 File Offset: 0x0005D700
		private void OnAfterTransfer(List<TransferCommandResult> resultList)
		{
			InventoryLogic.ProcessResultListDelegate afterTransfer = this.AfterTransfer;
			if (afterTransfer != null)
			{
				afterTransfer(this, resultList);
			}
			foreach (TransferCommandResult transferCommandResult in resultList)
			{
				if (transferCommandResult.EffectedNumber > 0)
				{
					Game.Current.EventManager.TriggerEvent<InventoryTransferItemEvent>(new InventoryTransferItemEvent(transferCommandResult.EffectedItemRosterElement.EquipmentElement.Item, transferCommandResult.ResultSide == InventoryLogic.InventorySide.PlayerInventory));
				}
			}
		}

		// Token: 0x060014C1 RID: 5313 RVA: 0x0005F598 File Offset: 0x0005D798
		public void AddTransferCommand(TransferCommand command)
		{
			this.ProcessTransferCommand(command);
		}

		// Token: 0x060014C2 RID: 5314 RVA: 0x0005F5A4 File Offset: 0x0005D7A4
		public void AddTransferCommands(IEnumerable<TransferCommand> commands)
		{
			foreach (TransferCommand command in commands)
			{
				this.ProcessTransferCommand(command);
			}
		}

		// Token: 0x060014C3 RID: 5315 RVA: 0x0005F5EC File Offset: 0x0005D7EC
		public bool CheckItemRosterHasElement(InventoryLogic.InventorySide side, ItemRosterElement rosterElement, int number)
		{
			int num = this._rosters[(int)side].FindIndexOfElement(rosterElement.EquipmentElement);
			return num != -1 && this._rosters[(int)side].GetElementCopyAtIndex(num).Amount >= number;
		}

		// Token: 0x060014C4 RID: 5316 RVA: 0x0005F630 File Offset: 0x0005D830
		private void ProcessTransferCommand(TransferCommand command)
		{
			List<TransferCommandResult> resultList = this.TransferItem(ref command);
			this.OnAfterTransfer(resultList);
		}

		// Token: 0x060014C5 RID: 5317 RVA: 0x0005F650 File Offset: 0x0005D850
		private List<TransferCommandResult> TransferItem(ref TransferCommand transferCommand)
		{
			List<TransferCommandResult> list = new List<TransferCommandResult>();
			string format = "TransferItem Name: {0} | From: {1} To: {2} | Amount: {3}";
			object[] array = new object[4];
			int num = 0;
			ItemObject item = transferCommand.ElementToTransfer.EquipmentElement.Item;
			array[num] = ((item != null) ? item.Name.ToString() : null) ?? "null";
			array[1] = transferCommand.FromSide;
			array[2] = transferCommand.ToSide;
			array[3] = transferCommand.Amount;
			Debug.Print(string.Format(format, array), 0, Debug.DebugColor.White, 17592186044416UL);
			if (transferCommand.ElementToTransfer.EquipmentElement.Item != null && InventoryLogic.TransferIsMovementValid(ref transferCommand) && this.DoesTransferItemExist(ref transferCommand))
			{
				int num2 = 0;
				bool flag = false;
				if (!InventoryLogic.IsEquipmentSide(transferCommand.FromSide) && transferCommand.FromSide != InventoryLogic.InventorySide.None)
				{
					int index = this._rosters[(int)transferCommand.FromSide].FindIndexOfElement(transferCommand.ElementToTransfer.EquipmentElement);
					ItemRosterElement elementCopyAtIndex = this._rosters[(int)transferCommand.FromSide].GetElementCopyAtIndex(index);
					flag = transferCommand.Amount == elementCopyAtIndex.Amount;
				}
				bool flag2 = this.IsSell(transferCommand.FromSide, transferCommand.ToSide);
				bool flag3 = this.IsBuy(transferCommand.FromSide, transferCommand.ToSide);
				for (int i = 0; i < transferCommand.Amount; i++)
				{
					if (InventoryLogic.IsEquipmentSide(transferCommand.ToSide) && transferCommand.ToSideEquipment[(int)transferCommand.ToEquipmentIndex].Item != null)
					{
						TransferCommand transferCommand2 = TransferCommand.Transfer(1, transferCommand.ToSide, InventoryLogic.InventorySide.PlayerInventory, new ItemRosterElement(transferCommand.ToSideEquipment[(int)transferCommand.ToEquipmentIndex], 1), transferCommand.ToEquipmentIndex, EquipmentIndex.None, transferCommand.Character);
						list.AddRange(this.TransferItem(ref transferCommand2));
					}
					EquipmentElement equipmentElement = transferCommand.ElementToTransfer.EquipmentElement;
					int itemPrice = this.GetItemPrice(equipmentElement, flag3);
					if (flag3 || flag2)
					{
						this._transactionHistory.RecordTransaction(equipmentElement, flag2, itemPrice);
					}
					if (this.IsTrading)
					{
						if (flag3)
						{
							num2 += itemPrice;
						}
						else if (flag2)
						{
							num2 -= itemPrice;
						}
					}
					if (InventoryLogic.IsEquipmentSide(transferCommand.FromSide))
					{
						ItemRosterElement itemRosterElement = new ItemRosterElement(transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex], transferCommand.Amount);
						itemRosterElement.Amount--;
						transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex] = itemRosterElement.EquipmentElement;
					}
					else if (transferCommand.FromSide == InventoryLogic.InventorySide.PlayerInventory || transferCommand.FromSide == InventoryLogic.InventorySide.OtherInventory)
					{
						this._rosters[(int)transferCommand.FromSide].AddToCounts(transferCommand.ElementToTransfer.EquipmentElement, -1);
					}
					if (InventoryLogic.IsEquipmentSide(transferCommand.ToSide))
					{
						ItemRosterElement elementToTransfer = transferCommand.ElementToTransfer;
						elementToTransfer.Amount = 1;
						transferCommand.ToSideEquipment[(int)transferCommand.ToEquipmentIndex] = elementToTransfer.EquipmentElement;
					}
					else if (transferCommand.ToSide == InventoryLogic.InventorySide.PlayerInventory || transferCommand.ToSide == InventoryLogic.InventorySide.OtherInventory)
					{
						this._rosters[(int)transferCommand.ToSide].AddToCounts(transferCommand.ElementToTransfer.EquipmentElement, 1);
					}
				}
				if (InventoryLogic.IsEquipmentSide(transferCommand.FromSide))
				{
					ItemRosterElement effectedItemRosterElement = new ItemRosterElement(transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex], transferCommand.Amount);
					int amount = effectedItemRosterElement.Amount;
					effectedItemRosterElement.Amount = amount - 1;
					list.Add(new TransferCommandResult(transferCommand.FromSide, effectedItemRosterElement, -transferCommand.Amount, effectedItemRosterElement.Amount, transferCommand.FromEquipmentIndex, transferCommand.Character));
				}
				else if (transferCommand.FromSide == InventoryLogic.InventorySide.PlayerInventory || transferCommand.FromSide == InventoryLogic.InventorySide.OtherInventory)
				{
					if (flag)
					{
						list.Add(new TransferCommandResult(transferCommand.FromSide, new ItemRosterElement(transferCommand.ElementToTransfer.EquipmentElement, 0), -transferCommand.Amount, 0, transferCommand.FromEquipmentIndex, transferCommand.Character));
					}
					else
					{
						int index2 = this._rosters[(int)transferCommand.FromSide].FindIndexOfElement(transferCommand.ElementToTransfer.EquipmentElement);
						ItemRosterElement elementCopyAtIndex2 = this._rosters[(int)transferCommand.FromSide].GetElementCopyAtIndex(index2);
						list.Add(new TransferCommandResult(transferCommand.FromSide, elementCopyAtIndex2, -transferCommand.Amount, elementCopyAtIndex2.Amount, transferCommand.FromEquipmentIndex, transferCommand.Character));
					}
				}
				if (InventoryLogic.IsEquipmentSide(transferCommand.ToSide))
				{
					ItemRosterElement elementToTransfer2 = transferCommand.ElementToTransfer;
					elementToTransfer2.Amount = 1;
					list.Add(new TransferCommandResult(transferCommand.ToSide, elementToTransfer2, 1, 1, transferCommand.ToEquipmentIndex, transferCommand.Character));
				}
				else if (transferCommand.ToSide == InventoryLogic.InventorySide.PlayerInventory || transferCommand.ToSide == InventoryLogic.InventorySide.OtherInventory)
				{
					int index3 = this._rosters[(int)transferCommand.ToSide].FindIndexOfElement(transferCommand.ElementToTransfer.EquipmentElement);
					ItemRosterElement elementCopyAtIndex3 = this._rosters[(int)transferCommand.ToSide].GetElementCopyAtIndex(index3);
					list.Add(new TransferCommandResult(transferCommand.ToSide, elementCopyAtIndex3, transferCommand.Amount, elementCopyAtIndex3.Amount, transferCommand.ToEquipmentIndex, transferCommand.Character));
				}
				this.HandleDonationOnTransferItem(transferCommand.ElementToTransfer, transferCommand.Amount, flag3, flag2);
				this.TransactionDebt += num2;
			}
			return list;
		}

		// Token: 0x060014C6 RID: 5318 RVA: 0x0005FB66 File Offset: 0x0005DD66
		public static bool IsEquipmentSide(InventoryLogic.InventorySide side)
		{
			return side == InventoryLogic.InventorySide.CivilianEquipment || side == InventoryLogic.InventorySide.BattleEquipment || side == InventoryLogic.InventorySide.StealthEquipment;
		}

		// Token: 0x060014C7 RID: 5319 RVA: 0x0005FB76 File Offset: 0x0005DD76
		private bool IsSell(InventoryLogic.InventorySide fromSide, InventoryLogic.InventorySide toSide)
		{
			return toSide == InventoryLogic.InventorySide.OtherInventory && (InventoryLogic.IsEquipmentSide(fromSide) || fromSide == InventoryLogic.InventorySide.PlayerInventory);
		}

		// Token: 0x060014C8 RID: 5320 RVA: 0x0005FB8B File Offset: 0x0005DD8B
		private bool IsBuy(InventoryLogic.InventorySide fromSide, InventoryLogic.InventorySide toSide)
		{
			return fromSide == InventoryLogic.InventorySide.OtherInventory && (InventoryLogic.IsEquipmentSide(toSide) || toSide == InventoryLogic.InventorySide.PlayerInventory);
		}

		// Token: 0x060014C9 RID: 5321 RVA: 0x0005FBA0 File Offset: 0x0005DDA0
		public void SlaughterItem(ItemRosterElement itemRosterElement)
		{
			List<TransferCommandResult> list = new List<TransferCommandResult>();
			EquipmentElement equipmentElement = itemRosterElement.EquipmentElement;
			int meatCount = equipmentElement.Item.HorseComponent.MeatCount;
			int hideCount = equipmentElement.Item.HorseComponent.HideCount;
			int index = this._rosters[1].AddToCounts(DefaultItems.Meat, meatCount);
			ItemRosterElement elementCopyAtIndex = this._rosters[1].GetElementCopyAtIndex(index);
			bool flag = itemRosterElement.Amount == 1;
			int index2 = this._rosters[1].AddToCounts(itemRosterElement.EquipmentElement, -1);
			if (flag)
			{
				list.Add(new TransferCommandResult(InventoryLogic.InventorySide.PlayerInventory, new ItemRosterElement(equipmentElement, 0), -1, 0, EquipmentIndex.None, null));
			}
			else
			{
				ItemRosterElement elementCopyAtIndex2 = this._rosters[1].GetElementCopyAtIndex(index2);
				list.Add(new TransferCommandResult(InventoryLogic.InventorySide.PlayerInventory, elementCopyAtIndex2, -1, elementCopyAtIndex2.Amount, EquipmentIndex.None, null));
			}
			list.Add(new TransferCommandResult(InventoryLogic.InventorySide.PlayerInventory, elementCopyAtIndex, meatCount, elementCopyAtIndex.Amount, EquipmentIndex.None, null));
			if (hideCount > 0)
			{
				int index3 = this._rosters[1].AddToCounts(DefaultItems.Hides, hideCount);
				ItemRosterElement elementCopyAtIndex3 = this._rosters[1].GetElementCopyAtIndex(index3);
				list.Add(new TransferCommandResult(InventoryLogic.InventorySide.PlayerInventory, elementCopyAtIndex3, hideCount, elementCopyAtIndex3.Amount, EquipmentIndex.None, null));
			}
			this.SetCurrentStateAsInitial();
			this.OnAfterTransfer(list);
		}

		// Token: 0x060014CA RID: 5322 RVA: 0x0005FCD4 File Offset: 0x0005DED4
		public void DonateItem(ItemRosterElement itemRosterElement)
		{
			List<TransferCommandResult> list = new List<TransferCommandResult>();
			int tier = (int)itemRosterElement.EquipmentElement.Item.Tier;
			int num = 100 * (tier + 1);
			InventoryLogic.InventorySide inventorySide = InventoryLogic.InventorySide.PlayerInventory;
			int index = this._rosters[(int)inventorySide].AddToCounts(itemRosterElement.EquipmentElement, -1);
			ItemRosterElement elementCopyAtIndex = this._rosters[(int)inventorySide].GetElementCopyAtIndex(index);
			list.Add(new TransferCommandResult(InventoryLogic.InventorySide.PlayerInventory, elementCopyAtIndex, -1, elementCopyAtIndex.Amount, EquipmentIndex.None, null));
			if (num > 0)
			{
				TroopRosterElement randomElementWithPredicate = PartyBase.MainParty.MemberRoster.GetTroopRoster().GetRandomElementWithPredicate((TroopRosterElement m) => !m.Character.IsHero && m.Character.UpgradeTargets.Length != 0);
				if (randomElementWithPredicate.Character != null)
				{
					PartyBase.MainParty.MemberRoster.AddXpToTroop(randomElementWithPredicate.Character, num);
					TextObject textObject = new TextObject("{=Kwja0a4s}Added {XPAMOUNT} amount of xp to {TROOPNAME}", null);
					textObject.SetTextVariable("XPAMOUNT", num);
					textObject.SetTextVariable("TROOPNAME", randomElementWithPredicate.Character.Name.ToString());
					Debug.Print(textObject.ToString(), 0, Debug.DebugColor.White, 17592186044416UL);
					MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
				}
			}
			this.SetCurrentStateAsInitial();
			this.OnAfterTransfer(list);
		}

		// Token: 0x060014CB RID: 5323 RVA: 0x0005FE08 File Offset: 0x0005E008
		private static bool TransferIsMovementValid(ref TransferCommand transferCommand)
		{
			if (transferCommand.ElementToTransfer.EquipmentElement.IsQuestItem)
			{
				BannerComponent bannerComponent = transferCommand.ElementToTransfer.EquipmentElement.Item.BannerComponent;
				if (((bannerComponent != null) ? bannerComponent.BannerEffect : null) == null || ((transferCommand.FromSide != InventoryLogic.InventorySide.PlayerInventory || !InventoryLogic.IsEquipmentSide(transferCommand.ToSide)) && (!InventoryLogic.IsEquipmentSide(transferCommand.FromSide) || transferCommand.ToSide != InventoryLogic.InventorySide.PlayerInventory)))
				{
					return false;
				}
			}
			bool result = false;
			if (InventoryLogic.IsEquipmentSide(transferCommand.ToSide))
			{
				InventoryScreenHelper.InventoryItemType inventoryItemTypeOfItem = InventoryScreenHelper.GetInventoryItemTypeOfItem(transferCommand.ElementToTransfer.EquipmentElement.Item);
				switch (transferCommand.ToEquipmentIndex)
				{
				case EquipmentIndex.WeaponItemBeginSlot:
				case EquipmentIndex.Weapon1:
				case EquipmentIndex.Weapon2:
				case EquipmentIndex.Weapon3:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Weapon || inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Shield;
					break;
				case EquipmentIndex.ExtraWeaponSlot:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Banner;
					break;
				case EquipmentIndex.NumAllWeaponSlots:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.HeadArmor;
					break;
				case EquipmentIndex.Body:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.BodyArmor;
					break;
				case EquipmentIndex.Leg:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.LegArmor;
					break;
				case EquipmentIndex.Gloves:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.HandArmor;
					break;
				case EquipmentIndex.Cape:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Cape;
					break;
				case EquipmentIndex.ArmorItemEndSlot:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Horse;
					break;
				case EquipmentIndex.HorseHarness:
					result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.HorseHarness;
					break;
				}
			}
			else
			{
				result = true;
			}
			return result;
		}

		// Token: 0x060014CC RID: 5324 RVA: 0x0005FF4C File Offset: 0x0005E14C
		private bool DoesTransferItemExist(ref TransferCommand transferCommand)
		{
			if (transferCommand.FromSide == InventoryLogic.InventorySide.OtherInventory || transferCommand.FromSide == InventoryLogic.InventorySide.PlayerInventory)
			{
				return this.CheckItemRosterHasElement(transferCommand.FromSide, transferCommand.ElementToTransfer, transferCommand.Amount);
			}
			return transferCommand.FromSide != InventoryLogic.InventorySide.None && transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex].Item != null && transferCommand.ElementToTransfer.EquipmentElement.IsEqualTo(transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex]);
		}

		// Token: 0x060014CD RID: 5325 RVA: 0x0005FFD2 File Offset: 0x0005E1D2
		public void TransferOne(ItemRosterElement itemRosterElement)
		{
		}

		// Token: 0x060014CE RID: 5326 RVA: 0x0005FFD4 File Offset: 0x0005E1D4
		public int GetElementCountOnSide(InventoryLogic.InventorySide side)
		{
			return this._rosters[(int)side].Count;
		}

		// Token: 0x060014CF RID: 5327 RVA: 0x0005FFE3 File Offset: 0x0005E1E3
		public IReadOnlyList<ItemRosterElement> GetElementsInInitialRoster(InventoryLogic.InventorySide side)
		{
			return this._rostersBackup[(int)side];
		}

		// Token: 0x060014D0 RID: 5328 RVA: 0x0005FFED File Offset: 0x0005E1ED
		public IReadOnlyList<ItemRosterElement> GetElementsInRoster(InventoryLogic.InventorySide side)
		{
			return this._rosters[(int)side];
		}

		// Token: 0x060014D1 RID: 5329 RVA: 0x0005FFF8 File Offset: 0x0005E1F8
		private void SetCurrentStateAsInitial()
		{
			for (int i = 0; i < this._rostersBackup.Length; i++)
			{
				this._rostersBackup[i] = new ItemRoster(this._rosters[i]);
			}
			this._partyInitialEquipment = new InventoryLogic.PartyEquipment(this.OwnerParty);
		}

		// Token: 0x060014D2 RID: 5330 RVA: 0x00060040 File Offset: 0x0005E240
		public ItemRosterElement? FindItemFromSide(InventoryLogic.InventorySide side, EquipmentElement item)
		{
			int num = this._rosters[(int)side].FindIndexOfElement(item);
			if (num >= 0)
			{
				return new ItemRosterElement?(this._rosters[(int)side].ElementAt(num));
			}
			return null;
		}

		// Token: 0x040006C9 RID: 1737
		private ItemRoster[] _rosters;

		// Token: 0x040006CA RID: 1738
		private ItemRoster[] _rostersBackup;

		// Token: 0x040006CB RID: 1739
		private IWorkshopWarehouseCampaignBehavior _workshopWarehouseBehavior;

		// Token: 0x040006D1 RID: 1745
		public bool IsPreviewingItem;

		// Token: 0x040006D2 RID: 1746
		private InventoryLogic.PartyEquipment _partyInitialEquipment;

		// Token: 0x040006D8 RID: 1752
		private float _xpGainFromDonations;

		// Token: 0x040006D9 RID: 1753
		private int _transactionDebt;

		// Token: 0x040006DA RID: 1754
		private bool _playerAcceptsTraderOffer;

		// Token: 0x040006DB RID: 1755
		private InventoryLogic.TransactionHistory _transactionHistory = new InventoryLogic.TransactionHistory();

		// Token: 0x040006DC RID: 1756
		private Dictionary<ItemCategory, float> _itemCategoryAverages = new Dictionary<ItemCategory, float>();

		// Token: 0x040006DD RID: 1757
		private bool _useBasePrices;

		// Token: 0x040006DE RID: 1758
		public InventoryScreenHelper.InventoryCategoryType MerchantItemType = InventoryScreenHelper.InventoryCategoryType.None;

		// Token: 0x040006DF RID: 1759
		private InventoryScreenHelper.InventoryMode _inventoryMode;

		// Token: 0x02000552 RID: 1362
		public enum TransferType
		{
			// Token: 0x0400169A RID: 5786
			Neutral,
			// Token: 0x0400169B RID: 5787
			Sell,
			// Token: 0x0400169C RID: 5788
			Buy
		}

		// Token: 0x02000553 RID: 1363
		public enum InventorySide
		{
			// Token: 0x0400169E RID: 5790
			OtherInventory,
			// Token: 0x0400169F RID: 5791
			PlayerInventory,
			// Token: 0x040016A0 RID: 5792
			CivilianEquipment,
			// Token: 0x040016A1 RID: 5793
			BattleEquipment,
			// Token: 0x040016A2 RID: 5794
			StealthEquipment,
			// Token: 0x040016A3 RID: 5795
			None = -1
		}

		// Token: 0x02000554 RID: 1364
		// (Invoke) Token: 0x06004CB2 RID: 19634
		public delegate void AfterResetDelegate(InventoryLogic inventoryLogic, bool fromCancel);

		// Token: 0x02000555 RID: 1365
		// (Invoke) Token: 0x06004CB6 RID: 19638
		public delegate void TotalAmountChangeDelegate(int newTotalAmount);

		// Token: 0x02000556 RID: 1366
		// (Invoke) Token: 0x06004CBA RID: 19642
		public delegate void ProcessResultListDelegate(InventoryLogic inventoryLogic, List<TransferCommandResult> results);

		// Token: 0x02000557 RID: 1367
		private class PartyEquipment
		{
			// Token: 0x17000EF4 RID: 3828
			// (get) Token: 0x06004CBD RID: 19645 RVA: 0x0017A5F4 File Offset: 0x001787F4
			// (set) Token: 0x06004CBE RID: 19646 RVA: 0x0017A5FC File Offset: 0x001787FC
			public Dictionary<CharacterObject, Equipment[]> CharacterEquipments { get; private set; }

			// Token: 0x06004CBF RID: 19647 RVA: 0x0017A605 File Offset: 0x00178805
			public PartyEquipment(MobileParty party)
			{
				this.CharacterEquipments = new Dictionary<CharacterObject, Equipment[]>();
				this.InitializeCopyFrom(party);
			}

			// Token: 0x06004CC0 RID: 19648 RVA: 0x0017A620 File Offset: 0x00178820
			public void InitializeCopyFrom(MobileParty party)
			{
				this.CharacterEquipments = new Dictionary<CharacterObject, Equipment[]>();
				for (int i = 0; i < party.MemberRoster.Count; i++)
				{
					CharacterObject character = party.MemberRoster.GetElementCopyAtIndex(i).Character;
					if (character.IsHero)
					{
						this.CharacterEquipments.Add(character, new Equipment[]
						{
							new Equipment(character.FirstBattleEquipment),
							new Equipment(character.FirstCivilianEquipment),
							new Equipment(character.FirstStealthEquipment)
						});
					}
				}
			}

			// Token: 0x06004CC1 RID: 19649 RVA: 0x0017A6A4 File Offset: 0x001788A4
			internal void ResetEquipment(MobileParty ownerParty)
			{
				foreach (KeyValuePair<CharacterObject, Equipment[]> keyValuePair in this.CharacterEquipments)
				{
					foreach (Equipment equipment in keyValuePair.Value)
					{
						if (equipment.IsBattle)
						{
							keyValuePair.Key.FirstBattleEquipment.FillFrom(equipment, true);
						}
						else if (equipment.IsCivilian)
						{
							keyValuePair.Key.FirstCivilianEquipment.FillFrom(equipment, true);
						}
						else if (equipment.IsStealth)
						{
							keyValuePair.Key.FirstStealthEquipment.FillFrom(equipment, true);
						}
						else
						{
							Debug.FailedAssert("Equipment type cannot be found!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Inventory\\InventoryLogic.cs", "ResetEquipment", 1166);
						}
					}
				}
			}

			// Token: 0x06004CC2 RID: 19650 RVA: 0x0017A78C File Offset: 0x0017898C
			public void SetReference(InventoryLogic.PartyEquipment partyEquipment)
			{
				this.CharacterEquipments.Clear();
				this.CharacterEquipments = partyEquipment.CharacterEquipments;
			}

			// Token: 0x06004CC3 RID: 19651 RVA: 0x0017A7A8 File Offset: 0x001789A8
			public bool IsEqual(InventoryLogic.PartyEquipment partyEquipment)
			{
				if (partyEquipment.CharacterEquipments.Keys.Count != this.CharacterEquipments.Keys.Count)
				{
					return false;
				}
				foreach (CharacterObject characterObject in partyEquipment.CharacterEquipments.Keys)
				{
					if (!this.CharacterEquipments.Keys.Contains(characterObject))
					{
						return false;
					}
					Equipment[] array;
					if (!this.CharacterEquipments.TryGetValue(characterObject, out array))
					{
						return false;
					}
					Equipment[] array2;
					if (!partyEquipment.CharacterEquipments.TryGetValue(characterObject, out array2) || array2.Length != array.Length)
					{
						return false;
					}
					for (int i = 0; i < array.Length; i++)
					{
						if (!array[i].IsEquipmentEqualTo(array2[i]))
						{
							return false;
						}
					}
				}
				return true;
			}
		}

		// Token: 0x02000558 RID: 1368
		private class ItemLog : IReadOnlyCollection<int>, IEnumerable<int>, IEnumerable
		{
			// Token: 0x17000EF5 RID: 3829
			// (get) Token: 0x06004CC4 RID: 19652 RVA: 0x0017A894 File Offset: 0x00178A94
			public bool IsSelling
			{
				get
				{
					return this._isSelling;
				}
			}

			// Token: 0x17000EF6 RID: 3830
			// (get) Token: 0x06004CC5 RID: 19653 RVA: 0x0017A89C File Offset: 0x00178A9C
			public int Count
			{
				get
				{
					return ((IReadOnlyCollection<int>)this._transactions).Count;
				}
			}

			// Token: 0x06004CC6 RID: 19654 RVA: 0x0017A8A9 File Offset: 0x00178AA9
			private void AddTransaction(int price, bool isSelling)
			{
				if (this._transactions.IsEmpty<int>())
				{
					this._isSelling = isSelling;
				}
				this._transactions.Add(price);
			}

			// Token: 0x06004CC7 RID: 19655 RVA: 0x0017A8CC File Offset: 0x00178ACC
			private void RemoveLastTransaction()
			{
				if (!this._transactions.IsEmpty<int>())
				{
					this._transactions.RemoveAt(this._transactions.Count - 1);
					return;
				}
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Inventory\\InventoryLogic.cs", "RemoveLastTransaction", 1246);
			}

			// Token: 0x06004CC8 RID: 19656 RVA: 0x0017A918 File Offset: 0x00178B18
			public void RecordTransaction(int price, bool isSelling)
			{
				if (!this._transactions.IsEmpty<int>() && isSelling != this._isSelling)
				{
					this.RemoveLastTransaction();
					return;
				}
				this.AddTransaction(price, isSelling);
			}

			// Token: 0x06004CC9 RID: 19657 RVA: 0x0017A93F File Offset: 0x00178B3F
			public bool GetLastTransaction(out int price, out bool isSelling)
			{
				if (this._transactions.IsEmpty<int>())
				{
					price = 0;
					isSelling = false;
					return false;
				}
				price = this._transactions[this._transactions.Count - 1];
				isSelling = this._isSelling;
				return true;
			}

			// Token: 0x06004CCA RID: 19658 RVA: 0x0017A979 File Offset: 0x00178B79
			public IEnumerator<int> GetEnumerator()
			{
				return ((IEnumerable<int>)this._transactions).GetEnumerator();
			}

			// Token: 0x06004CCB RID: 19659 RVA: 0x0017A986 File Offset: 0x00178B86
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<int>)this._transactions).GetEnumerator();
			}

			// Token: 0x040016A5 RID: 5797
			private List<int> _transactions = new List<int>();

			// Token: 0x040016A6 RID: 5798
			private bool _isSelling;
		}

		// Token: 0x02000559 RID: 1369
		public class CapacityData
		{
			// Token: 0x06004CCD RID: 19661 RVA: 0x0017A9A6 File Offset: 0x00178BA6
			public CapacityData(Func<int> getCapacity, Func<TextObject> getCapacityExceededWarningText, Func<TextObject> getCapacityExceededHintText, bool forceTransaction = false)
			{
				this._getCapacity = getCapacity;
				this._getCapacityExceededWarningText = getCapacityExceededWarningText;
				this._getCapacityExceededHintText = getCapacityExceededHintText;
				this._forceTransaction = forceTransaction;
			}

			// Token: 0x06004CCE RID: 19662 RVA: 0x0017A9CB File Offset: 0x00178BCB
			public int GetCapacity()
			{
				Func<int> getCapacity = this._getCapacity;
				if (getCapacity == null)
				{
					return -1;
				}
				return getCapacity();
			}

			// Token: 0x06004CCF RID: 19663 RVA: 0x0017A9DE File Offset: 0x00178BDE
			public bool CanForceTransaction()
			{
				return this._forceTransaction;
			}

			// Token: 0x06004CD0 RID: 19664 RVA: 0x0017A9E6 File Offset: 0x00178BE6
			public TextObject GetCapacityExceededWarningText()
			{
				Func<TextObject> getCapacityExceededWarningText = this._getCapacityExceededWarningText;
				if (getCapacityExceededWarningText == null)
				{
					return null;
				}
				return getCapacityExceededWarningText();
			}

			// Token: 0x06004CD1 RID: 19665 RVA: 0x0017A9F9 File Offset: 0x00178BF9
			public TextObject GetCapacityExceededHintText()
			{
				Func<TextObject> getCapacityExceededHintText = this._getCapacityExceededHintText;
				if (getCapacityExceededHintText == null)
				{
					return null;
				}
				return getCapacityExceededHintText();
			}

			// Token: 0x040016A7 RID: 5799
			private readonly Func<int> _getCapacity;

			// Token: 0x040016A8 RID: 5800
			private readonly Func<TextObject> _getCapacityExceededWarningText;

			// Token: 0x040016A9 RID: 5801
			private readonly Func<TextObject> _getCapacityExceededHintText;

			// Token: 0x040016AA RID: 5802
			private readonly bool _forceTransaction;
		}

		// Token: 0x0200055A RID: 1370
		private class TransactionHistory
		{
			// Token: 0x06004CD2 RID: 19666 RVA: 0x0017AA0C File Offset: 0x00178C0C
			internal void RecordTransaction(EquipmentElement elementToTransfer, bool isSelling, int price)
			{
				InventoryLogic.ItemLog itemLog;
				if (!this._transactionLogs.TryGetValue(elementToTransfer, out itemLog))
				{
					itemLog = new InventoryLogic.ItemLog();
					this._transactionLogs[elementToTransfer] = itemLog;
				}
				itemLog.RecordTransaction(price, isSelling);
			}

			// Token: 0x17000EF7 RID: 3831
			// (get) Token: 0x06004CD3 RID: 19667 RVA: 0x0017AA44 File Offset: 0x00178C44
			public bool IsEmpty
			{
				get
				{
					return this._transactionLogs.IsEmpty<KeyValuePair<EquipmentElement, InventoryLogic.ItemLog>>();
				}
			}

			// Token: 0x06004CD4 RID: 19668 RVA: 0x0017AA51 File Offset: 0x00178C51
			public void Clear()
			{
				this._transactionLogs.Clear();
			}

			// Token: 0x06004CD5 RID: 19669 RVA: 0x0017AA60 File Offset: 0x00178C60
			public bool GetLastTransfer(EquipmentElement equipmentElement, out int lastPrice, out bool lastIsSelling)
			{
				InventoryLogic.ItemLog itemLog;
				bool flag = this._transactionLogs.TryGetValue(equipmentElement, out itemLog);
				lastPrice = 0;
				lastIsSelling = false;
				return flag && itemLog.GetLastTransaction(out lastPrice, out lastIsSelling);
			}

			// Token: 0x06004CD6 RID: 19670 RVA: 0x0017AA90 File Offset: 0x00178C90
			internal List<ValueTuple<ItemRosterElement, int>> GetTransferredItems(bool isSelling)
			{
				List<ValueTuple<ItemRosterElement, int>> list = new List<ValueTuple<ItemRosterElement, int>>();
				foreach (KeyValuePair<EquipmentElement, InventoryLogic.ItemLog> keyValuePair in this._transactionLogs)
				{
					if (keyValuePair.Value.Count > 0 && !keyValuePair.Value.IsSelling == isSelling)
					{
						int item = keyValuePair.Value.Sum();
						list.Add(new ValueTuple<ItemRosterElement, int>(new ItemRosterElement(keyValuePair.Key.Item, keyValuePair.Value.Count, keyValuePair.Key.ItemModifier), item));
					}
				}
				return list;
			}

			// Token: 0x06004CD7 RID: 19671 RVA: 0x0017AB50 File Offset: 0x00178D50
			internal List<ValueTuple<ItemRosterElement, int>> GetBoughtItems()
			{
				return this.GetTransferredItems(true);
			}

			// Token: 0x06004CD8 RID: 19672 RVA: 0x0017AB59 File Offset: 0x00178D59
			internal List<ValueTuple<ItemRosterElement, int>> GetSoldItems()
			{
				return this.GetTransferredItems(false);
			}

			// Token: 0x040016AB RID: 5803
			private Dictionary<EquipmentElement, InventoryLogic.ItemLog> _transactionLogs = new Dictionary<EquipmentElement, InventoryLogic.ItemLog>();
		}
	}
}
