using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000099 RID: 153
	public class SPItemVM : ItemVM
	{
		// Token: 0x170004B9 RID: 1209
		// (get) Token: 0x06000E95 RID: 3733 RVA: 0x0003CDD2 File Offset: 0x0003AFD2
		// (set) Token: 0x06000E96 RID: 3734 RVA: 0x0003CDDA File Offset: 0x0003AFDA
		public InventoryLogic.InventorySide InventorySide { get; private set; }

		// Token: 0x06000E97 RID: 3735 RVA: 0x0003CDE3 File Offset: 0x0003AFE3
		public SPItemVM()
		{
			base.StringId = "";
			base.ImageIdentifier = new ItemImageIdentifierVM(null, "");
			this._itemType = EquipmentIndex.None;
		}

		// Token: 0x06000E98 RID: 3736 RVA: 0x0003CE18 File Offset: 0x0003B018
		public SPItemVM(InventoryLogic inventoryLogic, bool isHeroFemale, bool canCharacterUseItem, InventoryScreenHelper.InventoryMode usageType, ItemRosterElement newItem, InventoryLogic.InventorySide inventorySide, int itemCost = 0, EquipmentIndex? itemType = -1)
		{
			if (newItem.EquipmentElement.Item == null)
			{
				return;
			}
			this._usageType = usageType;
			this._tradeGoodConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_trade_goods");
			this._itemConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_item");
			this._inventoryLogic = inventoryLogic;
			this.ItemRosterElement = new ItemRosterElement(newItem.EquipmentElement, newItem.Amount);
			base.ItemCost = itemCost;
			this.ItemCount = newItem.Amount;
			this.TransactionCount = 1;
			this.ItemLevel = newItem.EquipmentElement.Item.Difficulty;
			this.InventorySide = inventorySide;
			if (itemType != null)
			{
				EquipmentIndex? equipmentIndex = itemType;
				EquipmentIndex equipmentIndex2 = EquipmentIndex.None;
				if (!((equipmentIndex.GetValueOrDefault() == equipmentIndex2) & (equipmentIndex != null)))
				{
					this._itemType = itemType.Value;
				}
			}
			base.OnItemTypeUpdated();
			base.ItemDescription = newItem.EquipmentElement.GetModifiedItemName().ToString();
			base.StringId = CampaignUIHelper.GetItemLockStringID(newItem.EquipmentElement);
			ItemObject item = newItem.EquipmentElement.Item;
			Clan playerClan = Clan.PlayerClan;
			base.ImageIdentifier = new ItemImageIdentifierVM(item, (playerClan != null) ? playerClan.Banner.Serialize() : null);
			this.IsCivilianItem = newItem.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.Civilian);
			this.IsStealthItem = newItem.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.Stealth);
			this.IsGenderDifferent = (isHeroFemale && this.ItemRosterElement.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale)) || (!isHeroFemale && this.ItemRosterElement.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale));
			this.CanCharacterUseItem = canCharacterUseItem;
			this.IsArtifact = newItem.EquipmentElement.Item.IsUniqueItem;
			this.UpdateCanBeSlaughtered();
			this.UpdateHintTexts();
			InventoryLogic inventoryLogic2 = this._inventoryLogic;
			this.CanBeDonated = inventoryLogic2 != null && inventoryLogic2.CanDonateItem(this.ItemRosterElement, this.InventorySide);
			this.TradeData = new InventoryTradeVM(this._inventoryLogic, this.ItemRosterElement, inventorySide, new Action<int, bool>(this.OnTradeApplyTransaction));
			InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
			InventoryScreenHelper.InventoryMode inventoryMode = ((activeInventoryState != null) ? activeInventoryState.InventoryMode : InventoryScreenHelper.InventoryMode.Default);
			this.IsTransferable = !this.ItemRosterElement.EquipmentElement.IsQuestItem && this.ItemRosterElement.EquipmentElement.Item.IsTransferable && (inventoryMode != InventoryScreenHelper.InventoryMode.Warehouse || this.ItemRosterElement.EquipmentElement.Item.IsTradeGood);
			this.TradeData.IsTradeable = this.IsTransferable;
			this.IsEquipableItem = (InventoryScreenHelper.GetInventoryItemTypeOfItem(newItem.EquipmentElement.Item) & InventoryScreenHelper.InventoryItemType.Equipable) > InventoryScreenHelper.InventoryItemType.None;
			this.UpdateProfitType();
		}

		// Token: 0x06000E99 RID: 3737 RVA: 0x0003D154 File Offset: 0x0003B354
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this.ItemRosterElement.EquipmentElement.Item != null)
			{
				TextObject modifiedItemName = this.ItemRosterElement.EquipmentElement.GetModifiedItemName();
				base.ItemDescription = ((modifiedItemName != null) ? modifiedItemName.ToString() : null) ?? "";
				return;
			}
			base.ItemDescription = "";
		}

		// Token: 0x06000E9A RID: 3738 RVA: 0x0003D1B8 File Offset: 0x0003B3B8
		public void RefreshWith(SPItemVM itemVM, InventoryLogic.InventorySide inventorySide)
		{
			this.InventorySide = inventorySide;
			if (itemVM == null)
			{
				this.Reset();
				return;
			}
			base.ItemDescription = itemVM.ItemDescription;
			base.ItemCost = itemVM.ItemCost;
			base.TypeName = itemVM.TypeName;
			this._itemType = itemVM.ItemType;
			this.ItemCount = itemVM.ItemCount;
			this.TransactionCount = itemVM.TransactionCount;
			this.ItemLevel = itemVM.ItemLevel;
			base.StringId = itemVM.StringId;
			base.ImageIdentifier = itemVM.ImageIdentifier.Clone();
			this.ItemRosterElement = itemVM.ItemRosterElement;
			this.IsCivilianItem = itemVM.IsCivilianItem;
			this.IsStealthItem = itemVM.IsStealthItem;
			this.IsGenderDifferent = itemVM.IsGenderDifferent;
			this.IsEquipableItem = itemVM.IsEquipableItem;
			this.CanCharacterUseItem = this.CanCharacterUseItem;
			this.IsArtifact = itemVM.IsArtifact;
			this.IsSelected = itemVM.IsSelected;
			this._inventoryLogic = itemVM._inventoryLogic;
			this.IsTransferable = itemVM.IsTransferable;
			this.UpdateCanBeSlaughtered();
			this.UpdateHintTexts();
			InventoryLogic inventoryLogic = this._inventoryLogic;
			this.CanBeDonated = inventoryLogic != null && inventoryLogic.CanDonateItem(this.ItemRosterElement, this.InventorySide);
			this.TradeData = new InventoryTradeVM(this._inventoryLogic, itemVM.ItemRosterElement, inventorySide, new Action<int, bool>(this.OnTradeApplyTransaction));
			this.UpdateProfitType();
			int version = base.Version;
			base.Version = version + 1;
		}

		// Token: 0x06000E9B RID: 3739 RVA: 0x0003D32C File Offset: 0x0003B52C
		private void Reset()
		{
			base.ItemDescription = "";
			base.ItemCost = 0;
			base.TypeName = "";
			this._itemType = EquipmentIndex.None;
			this.ItemCount = 0;
			this.TransactionCount = 0;
			this.ItemLevel = 0;
			base.StringId = "";
			base.ImageIdentifier = new ItemImageIdentifierVM(null, "");
			this.ItemRosterElement = default(ItemRosterElement);
			this.ProfitType = 0;
			this.IsCivilianItem = true;
			this.IsStealthItem = false;
			this.IsGenderDifferent = false;
			this.IsEquipableItem = true;
			this.IsArtifact = false;
			this.IsSelected = false;
			this.TradeData = new InventoryTradeVM(this._inventoryLogic, this.ItemRosterElement, InventoryLogic.InventorySide.None, new Action<int, bool>(this.OnTradeApplyTransaction));
			int version = base.Version;
			base.Version = version + 1;
		}

		// Token: 0x06000E9C RID: 3740 RVA: 0x0003D400 File Offset: 0x0003B600
		private void UpdateProfitType()
		{
			this.ProfitType = 0;
			if (Campaign.Current != null)
			{
				InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
				if (((activeInventoryState != null) ? activeInventoryState.InventoryMode : InventoryScreenHelper.InventoryMode.Default) == InventoryScreenHelper.InventoryMode.Trade)
				{
					if (this.InventorySide == InventoryLogic.InventorySide.PlayerInventory)
					{
						Hero mainHero = Hero.MainHero;
						if (mainHero == null || !mainHero.GetPerkValue(DefaultPerks.Trade.Appraiser))
						{
							Hero mainHero2 = Hero.MainHero;
							if (mainHero2 == null || !mainHero2.GetPerkValue(DefaultPerks.Trade.WholeSeller))
							{
								return;
							}
						}
						IPlayerTradeBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IPlayerTradeBehavior>();
						if (campaignBehavior != null)
						{
							int num = -campaignBehavior.GetProjectedProfit(this.ItemRosterElement, base.ItemCost) + base.ItemCost;
							this.ProfitType = (int)SPItemVM.GetProfitTypeFromDiff((float)num, (float)base.ItemCost);
							return;
						}
					}
					else if (this.InventorySide == InventoryLogic.InventorySide.OtherInventory && Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsFortification || Settlement.CurrentSettlement.IsVillage))
					{
						Hero mainHero3 = Hero.MainHero;
						if (mainHero3 == null || !mainHero3.GetPerkValue(DefaultPerks.Trade.CaravanMaster))
						{
							Hero mainHero4 = Hero.MainHero;
							if (mainHero4 == null || !mainHero4.GetPerkValue(DefaultPerks.Trade.MarketDealer))
							{
								return;
							}
						}
						float averagePriceFactorItemCategory = this._inventoryLogic.GetAveragePriceFactorItemCategory(this.ItemRosterElement.EquipmentElement.Item.ItemCategory);
						Town town = (Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.Village.Bound.Town : Settlement.CurrentSettlement.Town);
						if (averagePriceFactorItemCategory != -99f)
						{
							this.ProfitType = (int)SPItemVM.GetProfitTypeFromDiff(town.MarketData.GetPriceFactor(this.ItemRosterElement.EquipmentElement.Item.ItemCategory), averagePriceFactorItemCategory);
						}
					}
				}
			}
		}

		// Token: 0x06000E9D RID: 3741 RVA: 0x0003D59F File Offset: 0x0003B79F
		public void ExecuteBuySingle()
		{
			this.ExecuteBuy(1);
		}

		// Token: 0x06000E9E RID: 3742 RVA: 0x0003D5A8 File Offset: 0x0003B7A8
		public void ExecuteBuy(int amount)
		{
			this.TransactionCount = amount;
			ItemVM.ProcessBuyItem(this, false);
		}

		// Token: 0x06000E9F RID: 3743 RVA: 0x0003D5BD File Offset: 0x0003B7BD
		public void ExecuteSellSingle()
		{
			this.ExecuteSell(1);
		}

		// Token: 0x06000EA0 RID: 3744 RVA: 0x0003D5C6 File Offset: 0x0003B7C6
		public void ExecuteSell(int amount)
		{
			this.TransactionCount = amount;
			SPItemVM.ProcessSellItem(this, false);
		}

		// Token: 0x06000EA1 RID: 3745 RVA: 0x0003D5DB File Offset: 0x0003B7DB
		private void OnTradeApplyTransaction(int amount, bool isBuying)
		{
			this.TransactionCount = amount;
			if (isBuying)
			{
				ItemVM.ProcessBuyItem(this, true);
				return;
			}
			SPItemVM.ProcessSellItem(this, true);
		}

		// Token: 0x06000EA2 RID: 3746 RVA: 0x0003D600 File Offset: 0x0003B800
		public void ExecuteSellItem()
		{
			SPItemVM.ProcessSellItem(this, false);
		}

		// Token: 0x06000EA3 RID: 3747 RVA: 0x0003D610 File Offset: 0x0003B810
		public void ExecuteConcept()
		{
			if (this._tradeGoodConceptObj != null)
			{
				ItemObject item = this.ItemRosterElement.EquipmentElement.Item;
				if (item != null && item.Type == ItemObject.ItemTypeEnum.Goods)
				{
					Campaign.Current.EncyclopediaManager.GoToLink(this._tradeGoodConceptObj.EncyclopediaLink);
					return;
				}
			}
			if (this._itemConceptObj != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._itemConceptObj.EncyclopediaLink);
			}
		}

		// Token: 0x06000EA4 RID: 3748 RVA: 0x0003D687 File Offset: 0x0003B887
		public void ExecuteResetTrade()
		{
			this.TradeData.ExecuteReset();
		}

		// Token: 0x06000EA5 RID: 3749 RVA: 0x0003D694 File Offset: 0x0003B894
		private void UpdateTotalCost()
		{
			if (this.TransactionCount <= 0 || this._inventoryLogic == null || InventoryLogic.IsEquipmentSide(this.InventorySide))
			{
				return;
			}
			int num;
			this.TotalCost = this._inventoryLogic.GetItemTotalPrice(this.ItemRosterElement, this.TransactionCount, out num, this.InventorySide == InventoryLogic.InventorySide.OtherInventory);
		}

		// Token: 0x06000EA6 RID: 3750 RVA: 0x0003D6E8 File Offset: 0x0003B8E8
		public void UpdateTradeData(bool forceUpdateAmounts)
		{
			InventoryTradeVM tradeData = this.TradeData;
			if (tradeData != null)
			{
				tradeData.UpdateItemData(this.ItemRosterElement, this.InventorySide, forceUpdateAmounts);
			}
			this.UpdateProfitType();
		}

		// Token: 0x06000EA7 RID: 3751 RVA: 0x0003D70E File Offset: 0x0003B90E
		public void ExecuteSlaughterItem()
		{
			if (this.CanBeSlaughtered)
			{
				SPItemVM.ProcessItemSlaughter(this);
			}
		}

		// Token: 0x06000EA8 RID: 3752 RVA: 0x0003D723 File Offset: 0x0003B923
		public void ExecuteDonateItem()
		{
			if (this.CanBeDonated)
			{
				SPItemVM.ProcessItemDonate(this);
			}
		}

		// Token: 0x06000EA9 RID: 3753 RVA: 0x0003D738 File Offset: 0x0003B938
		public void ExecuteSetFocused()
		{
			this.IsFocused = true;
			Action<SPItemVM> onFocus = SPItemVM.OnFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(this);
		}

		// Token: 0x06000EAA RID: 3754 RVA: 0x0003D751 File Offset: 0x0003B951
		public void ExecuteSetUnfocused()
		{
			this.IsFocused = false;
			Action<SPItemVM> onFocus = SPItemVM.OnFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(null);
		}

		// Token: 0x06000EAB RID: 3755 RVA: 0x0003D76C File Offset: 0x0003B96C
		public void UpdateCanBeSlaughtered()
		{
			InventoryLogic inventoryLogic = this._inventoryLogic;
			this.CanBeSlaughtered = inventoryLogic != null && inventoryLogic.CanSlaughterItem(this.ItemRosterElement, this.InventorySide) && !this.ItemRosterElement.EquipmentElement.IsQuestItem;
		}

		// Token: 0x06000EAC RID: 3756 RVA: 0x0003D7B8 File Offset: 0x0003B9B8
		private string GetStackModifierString()
		{
			if (InventoryLogic.IsEquipmentSide(this.InventorySide))
			{
				return string.Empty;
			}
			TextObject allStackText = ((this.InventorySide == InventoryLogic.InventorySide.PlayerInventory) ? GameTexts.FindText("str_entire_stack_shortcut_discard_items", null) : GameTexts.FindText("str_entire_stack_shortcut_take_items", null));
			TextObject fiveStackText = ((this.InventorySide == InventoryLogic.InventorySide.PlayerInventory) ? GameTexts.FindText("str_five_stack_shortcut_discard_items", null) : GameTexts.FindText("str_five_stack_shortcut_take_items", null));
			return CampaignUIHelper.GetStackModifierString(allStackText, fiveStackText, this.ItemCount >= 5);
		}

		// Token: 0x06000EAD RID: 3757 RVA: 0x0003D830 File Offset: 0x0003BA30
		private string GetTextWithStackModifierText(string mainText)
		{
			string stackModifierString = this.GetStackModifierString();
			if (string.IsNullOrEmpty(stackModifierString))
			{
				return mainText;
			}
			return GameTexts.FindText("str_string_newline_string", null).SetTextVariable("STR1", mainText).SetTextVariable("STR2", stackModifierString)
				.ToString();
		}

		// Token: 0x06000EAE RID: 3758 RVA: 0x0003D874 File Offset: 0x0003BA74
		public void UpdateHintTexts()
		{
			base.SlaughterHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_slaughter", null).ToString()));
			base.DonateHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_donate", null).ToString()));
			base.PreviewHint = new HintViewModel(GameTexts.FindText("str_inventory_preview", null), null);
			base.EquipHint = new HintViewModel(GameTexts.FindText("str_inventory_equip", null), null);
			base.UnequipHint = new HintViewModel(GameTexts.FindText("str_inventory_unequip", null), null);
			base.LockHint = new HintViewModel(GameTexts.FindText("str_inventory_lock", null), null);
			if (this._usageType == InventoryScreenHelper.InventoryMode.Loot || this._usageType == InventoryScreenHelper.InventoryMode.Stash)
			{
				base.BuyAndEquipHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_inventory_take_and_equip", null).ToString());
				base.BuyHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_take", null).ToString()));
			}
			else if (this._usageType == InventoryScreenHelper.InventoryMode.Default)
			{
				base.BuyAndEquipHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_inventory_take_and_equip", null).ToString());
				base.BuyHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_take", null).ToString()));
			}
			else
			{
				base.BuyAndEquipHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_inventory_buy_and_equip", null).ToString());
				base.BuyHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_buy", null).ToString()));
			}
			if (!this.IsTransferable)
			{
				base.SellHint = new BasicTooltipViewModel(() => new TextObject("{=8xKky9ja}This item cannot be traded or discarded", null).ToString());
				return;
			}
			if (this._usageType == InventoryScreenHelper.InventoryMode.Loot || this._usageType == InventoryScreenHelper.InventoryMode.Stash)
			{
				base.SellHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_give", null).ToString()));
				return;
			}
			if (this._usageType == InventoryScreenHelper.InventoryMode.Default)
			{
				base.SellHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_discard", null).ToString()));
				return;
			}
			base.SellHint = new BasicTooltipViewModel(() => this.GetTextWithStackModifierText(GameTexts.FindText("str_inventory_sell", null).ToString()));
		}

		// Token: 0x06000EAF RID: 3759 RVA: 0x0003DA83 File Offset: 0x0003BC83
		public static SPItemVM.ProfitTypes GetProfitTypeFromDiff(float averageValue, float currentValue)
		{
			if (averageValue == 0f)
			{
				return SPItemVM.ProfitTypes.Default;
			}
			if (averageValue < currentValue * 0.8f)
			{
				return SPItemVM.ProfitTypes.HighProfit;
			}
			if (averageValue < currentValue * 0.95f)
			{
				return SPItemVM.ProfitTypes.Profit;
			}
			if (averageValue > currentValue * 1.05f)
			{
				return SPItemVM.ProfitTypes.Loss;
			}
			if (averageValue > currentValue * 1.2f)
			{
				return SPItemVM.ProfitTypes.HighLoss;
			}
			return SPItemVM.ProfitTypes.Default;
		}

		// Token: 0x170004BA RID: 1210
		// (get) Token: 0x06000EB0 RID: 3760 RVA: 0x0003DAC1 File Offset: 0x0003BCC1
		// (set) Token: 0x06000EB1 RID: 3761 RVA: 0x0003DAC9 File Offset: 0x0003BCC9
		[DataSourceProperty]
		public bool IsFocused
		{
			get
			{
				return this._isFocused;
			}
			set
			{
				if (value != this._isFocused)
				{
					this._isFocused = value;
					base.OnPropertyChangedWithValue(value, "IsFocused");
				}
			}
		}

		// Token: 0x170004BB RID: 1211
		// (get) Token: 0x06000EB2 RID: 3762 RVA: 0x0003DAE7 File Offset: 0x0003BCE7
		// (set) Token: 0x06000EB3 RID: 3763 RVA: 0x0003DAEF File Offset: 0x0003BCEF
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x170004BC RID: 1212
		// (get) Token: 0x06000EB4 RID: 3764 RVA: 0x0003DB0D File Offset: 0x0003BD0D
		// (set) Token: 0x06000EB5 RID: 3765 RVA: 0x0003DB15 File Offset: 0x0003BD15
		[DataSourceProperty]
		public bool IsArtifact
		{
			get
			{
				return this._isArtifact;
			}
			set
			{
				if (value != this._isArtifact)
				{
					this._isArtifact = value;
					base.OnPropertyChangedWithValue(value, "IsArtifact");
				}
			}
		}

		// Token: 0x170004BD RID: 1213
		// (get) Token: 0x06000EB6 RID: 3766 RVA: 0x0003DB33 File Offset: 0x0003BD33
		// (set) Token: 0x06000EB7 RID: 3767 RVA: 0x0003DB3B File Offset: 0x0003BD3B
		[DataSourceProperty]
		public bool IsTransferable
		{
			get
			{
				return this._isTransferable;
			}
			set
			{
				if (value != this._isTransferable)
				{
					this._isTransferable = value;
					base.OnPropertyChangedWithValue(value, "IsTransferable");
				}
			}
		}

		// Token: 0x170004BE RID: 1214
		// (get) Token: 0x06000EB8 RID: 3768 RVA: 0x0003DB59 File Offset: 0x0003BD59
		// (set) Token: 0x06000EB9 RID: 3769 RVA: 0x0003DB61 File Offset: 0x0003BD61
		[DataSourceProperty]
		public bool IsTransferButtonHighlighted
		{
			get
			{
				return this._isTransferButtonHighlighted;
			}
			set
			{
				if (value != this._isTransferButtonHighlighted)
				{
					this._isTransferButtonHighlighted = value;
					base.OnPropertyChangedWithValue(value, "IsTransferButtonHighlighted");
				}
			}
		}

		// Token: 0x170004BF RID: 1215
		// (get) Token: 0x06000EBA RID: 3770 RVA: 0x0003DB7F File Offset: 0x0003BD7F
		// (set) Token: 0x06000EBB RID: 3771 RVA: 0x0003DB87 File Offset: 0x0003BD87
		[DataSourceProperty]
		public bool IsItemHighlightEnabled
		{
			get
			{
				return this._isItemHighlightEnabled;
			}
			set
			{
				if (value != this._isItemHighlightEnabled)
				{
					this._isItemHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsItemHighlightEnabled");
				}
			}
		}

		// Token: 0x170004C0 RID: 1216
		// (get) Token: 0x06000EBC RID: 3772 RVA: 0x0003DBA5 File Offset: 0x0003BDA5
		// (set) Token: 0x06000EBD RID: 3773 RVA: 0x0003DBAD File Offset: 0x0003BDAD
		[DataSourceProperty]
		public bool IsCivilianItem
		{
			get
			{
				return this._isCivilianItem;
			}
			set
			{
				if (value != this._isCivilianItem)
				{
					this._isCivilianItem = value;
					base.OnPropertyChangedWithValue(value, "IsCivilianItem");
				}
			}
		}

		// Token: 0x170004C1 RID: 1217
		// (get) Token: 0x06000EBE RID: 3774 RVA: 0x0003DBCB File Offset: 0x0003BDCB
		// (set) Token: 0x06000EBF RID: 3775 RVA: 0x0003DBD3 File Offset: 0x0003BDD3
		[DataSourceProperty]
		public bool IsStealthItem
		{
			get
			{
				return this._isStealthItem;
			}
			set
			{
				if (value != this._isStealthItem)
				{
					this._isStealthItem = value;
					base.OnPropertyChangedWithValue(value, "IsStealthItem");
				}
			}
		}

		// Token: 0x170004C2 RID: 1218
		// (get) Token: 0x06000EC0 RID: 3776 RVA: 0x0003DBF1 File Offset: 0x0003BDF1
		// (set) Token: 0x06000EC1 RID: 3777 RVA: 0x0003DBF9 File Offset: 0x0003BDF9
		[DataSourceProperty]
		public bool IsNew
		{
			get
			{
				return this._isNew;
			}
			set
			{
				if (value != this._isNew)
				{
					this._isNew = value;
					base.OnPropertyChangedWithValue(value, "IsNew");
				}
			}
		}

		// Token: 0x170004C3 RID: 1219
		// (get) Token: 0x06000EC2 RID: 3778 RVA: 0x0003DC17 File Offset: 0x0003BE17
		// (set) Token: 0x06000EC3 RID: 3779 RVA: 0x0003DC1F File Offset: 0x0003BE1F
		[DataSourceProperty]
		public bool IsGenderDifferent
		{
			get
			{
				return this._isGenderDifferent;
			}
			set
			{
				if (value != this._isGenderDifferent)
				{
					this._isGenderDifferent = value;
					base.OnPropertyChangedWithValue(value, "IsGenderDifferent");
				}
			}
		}

		// Token: 0x170004C4 RID: 1220
		// (get) Token: 0x06000EC4 RID: 3780 RVA: 0x0003DC3D File Offset: 0x0003BE3D
		// (set) Token: 0x06000EC5 RID: 3781 RVA: 0x0003DC45 File Offset: 0x0003BE45
		[DataSourceProperty]
		public bool CanBeSlaughtered
		{
			get
			{
				return this._canBeSlaughtered;
			}
			set
			{
				if (value != this._canBeSlaughtered)
				{
					this._canBeSlaughtered = value;
					base.OnPropertyChangedWithValue(value, "CanBeSlaughtered");
				}
			}
		}

		// Token: 0x170004C5 RID: 1221
		// (get) Token: 0x06000EC6 RID: 3782 RVA: 0x0003DC63 File Offset: 0x0003BE63
		// (set) Token: 0x06000EC7 RID: 3783 RVA: 0x0003DC6B File Offset: 0x0003BE6B
		[DataSourceProperty]
		public bool CanBeDonated
		{
			get
			{
				return this._canBeDonated;
			}
			set
			{
				if (value != this._canBeDonated)
				{
					this._canBeDonated = value;
					base.OnPropertyChangedWithValue(value, "CanBeDonated");
				}
			}
		}

		// Token: 0x170004C6 RID: 1222
		// (get) Token: 0x06000EC8 RID: 3784 RVA: 0x0003DC89 File Offset: 0x0003BE89
		// (set) Token: 0x06000EC9 RID: 3785 RVA: 0x0003DC91 File Offset: 0x0003BE91
		[DataSourceProperty]
		public bool IsEquipableItem
		{
			get
			{
				return this._isEquipableItem;
			}
			set
			{
				if (value != this._isEquipableItem)
				{
					this._isEquipableItem = value;
					base.OnPropertyChangedWithValue(value, "IsEquipableItem");
				}
			}
		}

		// Token: 0x170004C7 RID: 1223
		// (get) Token: 0x06000ECA RID: 3786 RVA: 0x0003DCAF File Offset: 0x0003BEAF
		// (set) Token: 0x06000ECB RID: 3787 RVA: 0x0003DCB7 File Offset: 0x0003BEB7
		[DataSourceProperty]
		public bool CanCharacterUseItem
		{
			get
			{
				return this._canCharacterUseItem;
			}
			set
			{
				if (value != this._canCharacterUseItem)
				{
					this._canCharacterUseItem = value;
					base.OnPropertyChangedWithValue(value, "CanCharacterUseItem");
				}
			}
		}

		// Token: 0x170004C8 RID: 1224
		// (get) Token: 0x06000ECC RID: 3788 RVA: 0x0003DCD5 File Offset: 0x0003BED5
		// (set) Token: 0x06000ECD RID: 3789 RVA: 0x0003DCDD File Offset: 0x0003BEDD
		[DataSourceProperty]
		public bool IsLocked
		{
			get
			{
				return this._isLocked;
			}
			set
			{
				if (value != this._isLocked)
				{
					this._isLocked = value;
					base.OnPropertyChangedWithValue(value, "IsLocked");
					SPItemVM.ProcessLockItem(this, value);
				}
			}
		}

		// Token: 0x170004C9 RID: 1225
		// (get) Token: 0x06000ECE RID: 3790 RVA: 0x0003DD07 File Offset: 0x0003BF07
		// (set) Token: 0x06000ECF RID: 3791 RVA: 0x0003DD0F File Offset: 0x0003BF0F
		[DataSourceProperty]
		public int ItemCount
		{
			get
			{
				return this._count;
			}
			set
			{
				if (value != this._count)
				{
					this._count = value;
					base.OnPropertyChangedWithValue(value, "ItemCount");
					this.UpdateTotalCost();
					this.UpdateTradeData(false);
				}
			}
		}

		// Token: 0x170004CA RID: 1226
		// (get) Token: 0x06000ED0 RID: 3792 RVA: 0x0003DD3A File Offset: 0x0003BF3A
		// (set) Token: 0x06000ED1 RID: 3793 RVA: 0x0003DD42 File Offset: 0x0003BF42
		[DataSourceProperty]
		public int ItemLevel
		{
			get
			{
				return this._level;
			}
			set
			{
				if (value != this._level)
				{
					this._level = value;
					base.OnPropertyChangedWithValue(value, "ItemLevel");
				}
			}
		}

		// Token: 0x170004CB RID: 1227
		// (get) Token: 0x06000ED2 RID: 3794 RVA: 0x0003DD60 File Offset: 0x0003BF60
		// (set) Token: 0x06000ED3 RID: 3795 RVA: 0x0003DD68 File Offset: 0x0003BF68
		[DataSourceProperty]
		public int ProfitType
		{
			get
			{
				return this._profitType;
			}
			set
			{
				if (value != this._profitType)
				{
					this._profitType = value;
					base.OnPropertyChangedWithValue(value, "ProfitType");
				}
			}
		}

		// Token: 0x170004CC RID: 1228
		// (get) Token: 0x06000ED4 RID: 3796 RVA: 0x0003DD86 File Offset: 0x0003BF86
		// (set) Token: 0x06000ED5 RID: 3797 RVA: 0x0003DD8E File Offset: 0x0003BF8E
		[DataSourceProperty]
		public int TransactionCount
		{
			get
			{
				return this._transactionCount;
			}
			set
			{
				if (value != this._transactionCount)
				{
					this._transactionCount = value;
					base.OnPropertyChangedWithValue(value, "TransactionCount");
					this.UpdateTotalCost();
				}
			}
		}

		// Token: 0x170004CD RID: 1229
		// (get) Token: 0x06000ED6 RID: 3798 RVA: 0x0003DDB2 File Offset: 0x0003BFB2
		// (set) Token: 0x06000ED7 RID: 3799 RVA: 0x0003DDBA File Offset: 0x0003BFBA
		[DataSourceProperty]
		public int TotalCost
		{
			get
			{
				return this._totalCost;
			}
			set
			{
				if (value != this._totalCost)
				{
					this._totalCost = value;
					base.OnPropertyChangedWithValue(value, "TotalCost");
				}
			}
		}

		// Token: 0x170004CE RID: 1230
		// (get) Token: 0x06000ED8 RID: 3800 RVA: 0x0003DDD8 File Offset: 0x0003BFD8
		// (set) Token: 0x06000ED9 RID: 3801 RVA: 0x0003DDE0 File Offset: 0x0003BFE0
		[DataSourceProperty]
		public InventoryTradeVM TradeData
		{
			get
			{
				return this._tradeData;
			}
			set
			{
				if (value != this._tradeData)
				{
					this._tradeData = value;
					base.OnPropertyChangedWithValue<InventoryTradeVM>(value, "TradeData");
				}
			}
		}

		// Token: 0x04000698 RID: 1688
		public static Action<SPItemVM> OnFocus;

		// Token: 0x04000699 RID: 1689
		public static Action<SPItemVM, bool> ProcessSellItem;

		// Token: 0x0400069A RID: 1690
		public static Action<SPItemVM> ProcessItemSlaughter;

		// Token: 0x0400069B RID: 1691
		public static Action<SPItemVM> ProcessItemDonate;

		// Token: 0x0400069C RID: 1692
		public static Action<SPItemVM, bool> ProcessLockItem;

		// Token: 0x0400069D RID: 1693
		private readonly InventoryScreenHelper.InventoryMode _usageType;

		// Token: 0x0400069E RID: 1694
		private Concept _tradeGoodConceptObj;

		// Token: 0x0400069F RID: 1695
		private Concept _itemConceptObj;

		// Token: 0x040006A1 RID: 1697
		private InventoryLogic _inventoryLogic;

		// Token: 0x040006A2 RID: 1698
		private bool _isFocused;

		// Token: 0x040006A3 RID: 1699
		private bool _isSelected;

		// Token: 0x040006A4 RID: 1700
		private int _level;

		// Token: 0x040006A5 RID: 1701
		private bool _isTransferable;

		// Token: 0x040006A6 RID: 1702
		private bool _isCivilianItem;

		// Token: 0x040006A7 RID: 1703
		private bool _isStealthItem;

		// Token: 0x040006A8 RID: 1704
		private bool _isGenderDifferent;

		// Token: 0x040006A9 RID: 1705
		private bool _isEquipableItem;

		// Token: 0x040006AA RID: 1706
		private bool _canCharacterUseItem;

		// Token: 0x040006AB RID: 1707
		private bool _isLocked;

		// Token: 0x040006AC RID: 1708
		private bool _isArtifact;

		// Token: 0x040006AD RID: 1709
		private bool _canBeSlaughtered;

		// Token: 0x040006AE RID: 1710
		private bool _canBeDonated;

		// Token: 0x040006AF RID: 1711
		private int _count;

		// Token: 0x040006B0 RID: 1712
		private int _profitType = -5;

		// Token: 0x040006B1 RID: 1713
		private int _transactionCount;

		// Token: 0x040006B2 RID: 1714
		private int _totalCost;

		// Token: 0x040006B3 RID: 1715
		private bool _isTransferButtonHighlighted;

		// Token: 0x040006B4 RID: 1716
		private bool _isItemHighlightEnabled;

		// Token: 0x040006B5 RID: 1717
		private bool _isNew;

		// Token: 0x040006B6 RID: 1718
		private InventoryTradeVM _tradeData;

		// Token: 0x0200020C RID: 524
		public enum ProfitTypes
		{
			// Token: 0x0400118E RID: 4494
			HighLoss = -2,
			// Token: 0x0400118F RID: 4495
			Loss,
			// Token: 0x04001190 RID: 4496
			Default,
			// Token: 0x04001191 RID: 4497
			Profit,
			// Token: 0x04001192 RID: 4498
			HighProfit
		}
	}
}
