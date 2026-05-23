using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000094 RID: 148
	public class SPInventoryVM : ViewModel
	{
		// Token: 0x06000CDF RID: 3295 RVA: 0x0003637D File Offset: 0x0003457D
		private InventoryLogic.InventorySide GetEquipmentToInventorySide(SPInventoryVM.EquipmentModes equipmentMode)
		{
			switch (equipmentMode)
			{
			case SPInventoryVM.EquipmentModes.Civilian:
				return InventoryLogic.InventorySide.CivilianEquipment;
			case SPInventoryVM.EquipmentModes.Battle:
				return InventoryLogic.InventorySide.BattleEquipment;
			case SPInventoryVM.EquipmentModes.Stealth:
				return InventoryLogic.InventorySide.StealthEquipment;
			default:
				return InventoryLogic.InventorySide.None;
			}
		}

		// Token: 0x06000CE0 RID: 3296 RVA: 0x0003639C File Offset: 0x0003459C
		public SPInventoryVM(InventoryLogic inventoryLogic, bool isInCivilianModeByDefault, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags)
		{
			this.IsSearchAvailable = true;
			InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
			this._usageType = ((activeInventoryState != null) ? activeInventoryState.InventoryMode : InventoryScreenHelper.InventoryMode.Default);
			this._inventoryLogic = inventoryLogic;
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			this._getItemUsageSetFlags = getItemUsageSetFlags;
			this._filters = new Dictionary<SPInventoryVM.Filters, List<int>>();
			this._filters.Add(SPInventoryVM.Filters.All, this._everyItemType);
			this._filters.Add(SPInventoryVM.Filters.Weapons, this._weaponItemTypes);
			this._filters.Add(SPInventoryVM.Filters.Armors, this._armorItemTypes);
			this._filters.Add(SPInventoryVM.Filters.Mounts, this._mountItemTypes);
			this._filters.Add(SPInventoryVM.Filters.ShieldsAndRanged, this._shieldAndRangedItemTypes);
			this._filters.Add(SPInventoryVM.Filters.Miscellaneous, this._miscellaneousItemTypes);
			this._equipAfterTransferStack = new Stack<SPItemVM>();
			this._comparedItemList = new List<ItemVM>();
			this._donationMaxShareableXp = MobilePartyHelper.GetMaximumXpAmountPartyCanGet(MobileParty.MainParty);
			MBTextManager.SetTextVariable("XP_DONATION_LIMIT", this._donationMaxShareableXp);
			if (this._inventoryLogic != null)
			{
				this._currentCharacter = this._inventoryLogic.InitialEquipmentCharacter;
				this._isTrading = inventoryLogic.IsTrading;
				this._inventoryLogic.AfterReset += this.AfterReset;
				InventoryLogic inventoryLogic2 = this._inventoryLogic;
				inventoryLogic2.TotalAmountChange = (Action<int>)Delegate.Combine(inventoryLogic2.TotalAmountChange, new Action<int>(this.OnTotalAmountChange));
				InventoryLogic inventoryLogic3 = this._inventoryLogic;
				inventoryLogic3.DonationXpChange = (Action)Delegate.Combine(inventoryLogic3.DonationXpChange, new Action(this.OnDonationXpChange));
				this._inventoryLogic.AfterTransfer += this.AfterTransfer;
				this._rightTroopRoster = inventoryLogic.RightMemberRoster;
				this._leftTroopRoster = inventoryLogic.LeftMemberRoster;
				this._currentInventoryCharacterIndex = this._rightTroopRoster.FindIndexOfTroop(this._currentCharacter);
				this.OnDonationXpChange();
				this.CompanionExists = this.DoesCompanionExist();
			}
			this.MainCharacter = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.MainCharacter.FillFrom(this._currentCharacter.HeroObject, -1, false, false);
			this.ItemMenu = new ItemMenuVM(new Action<ItemVM, int>(this.ResetComparedItems), this._inventoryLogic, this._getItemUsageSetFlags, new Func<EquipmentIndex, SPItemVM>(this.GetItemFromIndex));
			this.IsRefreshed = false;
			this.RightItemListVM = new MBBindingList<SPItemVM>();
			this.LeftItemListVM = new MBBindingList<SPItemVM>();
			this.CharacterHelmSlot = new SPItemVM();
			this.CharacterCloakSlot = new SPItemVM();
			this.CharacterTorsoSlot = new SPItemVM();
			this.CharacterGloveSlot = new SPItemVM();
			this.CharacterBootSlot = new SPItemVM();
			this.CharacterMountSlot = new SPItemVM();
			this.CharacterMountArmorSlot = new SPItemVM();
			this.CharacterWeapon1Slot = new SPItemVM();
			this.CharacterWeapon2Slot = new SPItemVM();
			this.CharacterWeapon3Slot = new SPItemVM();
			this.CharacterWeapon4Slot = new SPItemVM();
			this.CharacterBannerSlot = new SPItemVM();
			this.ProductionTooltip = new BasicTooltipViewModel();
			this.CurrentCharacterSkillsTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetInventoryCharacterTooltip(this._currentCharacter.HeroObject));
			this.RefreshCallbacks();
			this._selectedEquipmentIndex = 0;
			if (isInCivilianModeByDefault)
			{
				this.EquipmentMode = 0;
			}
			if (this._inventoryLogic != null)
			{
				this.UpdateRightCharacter();
				this.UpdateLeftCharacter();
				this.InitializeInventory();
			}
			this.RightInventoryOwnerGold = Hero.MainHero.Gold;
			if (this._inventoryLogic.OtherSideCapacityData != null)
			{
				this.OtherSideHasCapacity = this._inventoryLogic.OtherSideCapacityData.GetCapacity() != -1;
			}
			this.IsOtherInventoryGoldRelevant = this._usageType != InventoryScreenHelper.InventoryMode.Loot;
			this.PlayerInventorySortController = new SPInventorySortControllerVM(ref this._rightItemListVM);
			this.OtherInventorySortController = new SPInventorySortControllerVM(ref this._leftItemListVM);
			this.PlayerInventorySortController.SortByDefaultState();
			if (this._usageType == InventoryScreenHelper.InventoryMode.Loot)
			{
				this.OtherInventorySortController.CostState = 1;
				this.OtherInventorySortController.ExecuteSortByCost();
			}
			else
			{
				this.OtherInventorySortController.SortByDefaultState();
			}
			Tuple<int, int> tuple = this._viewDataTracker.InventoryGetSortPreference((int)this._usageType);
			if (tuple != null)
			{
				this.PlayerInventorySortController.SortByOption((SPInventorySortControllerVM.InventoryItemSortOption)tuple.Item1, (SPInventorySortControllerVM.InventoryItemSortState)tuple.Item2);
			}
			this.ItemPreview = new ItemPreviewVM(new Action(this.OnPreviewClosed));
			this._characterList = new SelectorVM<InventoryCharacterSelectorItemVM>(0, new Action<SelectorVM<InventoryCharacterSelectorItemVM>>(this.OnCharacterSelected));
			this.AddApplicableCharactersToListFromRoster(this._rightTroopRoster.GetTroopRoster());
			if (this._inventoryLogic.IsOtherPartyFromPlayerClan && this._leftTroopRoster != null)
			{
				this.AddApplicableCharactersToListFromRoster(this._leftTroopRoster.GetTroopRoster());
			}
			if (this._characterList.SelectedIndex == -1 && this._characterList.ItemList.Count > 0)
			{
				this._characterList.SelectedIndex = 0;
			}
			this.BannerTypeName = ItemObject.ItemTypeEnum.Banner.ToString();
			InventoryTradeVM.RemoveZeroCounts += this.ExecuteRemoveZeroCounts;
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.RefreshValues();
		}

		// Token: 0x06000CE1 RID: 3297 RVA: 0x00036AA8 File Offset: 0x00034CA8
		private void AddApplicableCharactersToListFromRoster(MBList<TroopRosterElement> roster)
		{
			for (int i = 0; i < roster.Count; i++)
			{
				CharacterObject character = roster[i].Character;
				if (character.IsHero && this.CanSelectHero(character.HeroObject))
				{
					this._characterList.AddItem(new InventoryCharacterSelectorItemVM(character.StringId, character.HeroObject, character.HeroObject.Name));
					if (character == this._currentCharacter)
					{
						this._characterList.SelectedIndex = this._characterList.ItemList.Count - 1;
					}
				}
			}
		}

		// Token: 0x06000CE2 RID: 3298 RVA: 0x00036B38 File Offset: 0x00034D38
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.RightInventoryOwnerName = PartyBase.MainParty.Name.ToString();
			this.SeparatorText = new TextObject("{=dB6cFDmz}/", null).ToString();
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			this.CancelLbl = GameTexts.FindText("str_cancel", null).ToString();
			this.ResetLbl = GameTexts.FindText("str_reset", null).ToString();
			this.TypeText = GameTexts.FindText("str_sort_by_type_label", null).ToString();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.QuantityText = GameTexts.FindText("str_quantity_sign", null).ToString();
			this.CostText = GameTexts.FindText("str_value", null).ToString();
			this.SearchPlaceholderText = new TextObject("{=tQOPRBFg}Search...", null).ToString();
			this.FilterAllHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_all", null), null);
			this.FilterWeaponHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_weapons", null), null);
			this.FilterArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_armors", null), null);
			this.FilterShieldAndRangedHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_shields_ranged", null), null);
			this.FilterMountAndHarnessHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_mounts", null), null);
			this.FilterMiscHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_other", null), null);
			this.CivilianOutfitHint = new HintViewModel(GameTexts.FindText("str_inventory_civilian_outfit", null), null);
			this.BattleOutfitHint = new HintViewModel(GameTexts.FindText("str_inventory_battle_outfit", null), null);
			this.StealthOutfitHint = new HintViewModel(GameTexts.FindText("str_inventory_stealth_outfit", null), null);
			this.EquipmentHelmSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_helm_slot", null), null);
			this.EquipmentArmorSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_armor_slot", null), null);
			this.EquipmentBootSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_boot_slot", null), null);
			this.EquipmentCloakSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_cloak_slot", null), null);
			this.EquipmentGloveSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_glove_slot", null), null);
			this.EquipmentHarnessSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_mount_armor_slot", null), null);
			this.EquipmentMountSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_mount_slot", null), null);
			this.EquipmentWeaponSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_weapons", null), null);
			this.EquipmentBannerSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_banner_slot", null), null);
			this.WeightHint = new HintViewModel(GameTexts.FindText("str_inventory_weight_desc", null), null);
			this.ArmArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_arm_armor", null), null);
			this.BodyArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_body_armor", null), null);
			this.HeadArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_head_armor", null), null);
			this.LegArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_leg_armor", null), null);
			this.HorseArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_horse_armor", null), null);
			this.DonationLblHint = new HintViewModel(GameTexts.FindText("str_inventory_donation_label_hint", null), null);
			this.SetPreviousCharacterHint();
			this.SetNextCharacterHint();
			this.PreviewHint = new HintViewModel(GameTexts.FindText("str_inventory_preview", null), null);
			this.EquipHint = new HintViewModel(GameTexts.FindText("str_inventory_equip", null), null);
			this.UnequipHint = new HintViewModel(GameTexts.FindText("str_inventory_unequip", null), null);
			this.ResetHint = new HintViewModel(GameTexts.FindText("str_reset", null), null);
			this.PlayerSideCapacityExceededText = GameTexts.FindText("str_capacity_exceeded", null).ToString();
			this.PlayerSideCapacityExceededHint = new HintViewModel(GameTexts.FindText("str_capacity_exceeded_hint", null), null);
			this.MainPartyLandCapacityExceededText = new TextObject("{=fgyvzyB5}Land Capacity Exceeded", null).ToString();
			this.MainPartySeaCapacityExceededText = new TextObject("{=7dXs9c2b}Sea Capacity Exceeded", null).ToString();
			this.MainPartyLandCapacityExceededHint = new HintViewModel(new TextObject("{=knayk28P}You will slow down on land. Be careful.", null), null);
			this.MainPartySeaCapacityExceededHint = new HintViewModel(new TextObject("{=zoX9akov}You will slow down at sea. Be careful.", null), null);
			if (this._inventoryLogic.OtherSideCapacityData != null)
			{
				TextObject capacityExceededWarningText = this._inventoryLogic.OtherSideCapacityData.GetCapacityExceededWarningText();
				this.OtherSideCapacityExceededText = ((capacityExceededWarningText != null) ? capacityExceededWarningText.ToString() : null);
				this.OtherSideCapacityExceededHint = new HintViewModel(this._inventoryLogic.OtherSideCapacityData.GetCapacityExceededHintText(), null);
			}
			this.SetBuyAllHint();
			this.SetSellAllHint();
			if (this._usageType == InventoryScreenHelper.InventoryMode.Loot || this._usageType == InventoryScreenHelper.InventoryMode.Stash)
			{
				this.SellHint = new HintViewModel(GameTexts.FindText("str_inventory_give", null), null);
			}
			else if (this._usageType == InventoryScreenHelper.InventoryMode.Default)
			{
				this.SellHint = new HintViewModel(GameTexts.FindText("str_inventory_discard", null), null);
			}
			else
			{
				this.SellHint = new HintViewModel(GameTexts.FindText("str_inventory_sell", null), null);
			}
			this.CharacterHelmSlot.RefreshValues();
			this.CharacterCloakSlot.RefreshValues();
			this.CharacterTorsoSlot.RefreshValues();
			this.CharacterGloveSlot.RefreshValues();
			this.CharacterBootSlot.RefreshValues();
			this.CharacterMountSlot.RefreshValues();
			this.CharacterMountArmorSlot.RefreshValues();
			this.CharacterWeapon1Slot.RefreshValues();
			this.CharacterWeapon2Slot.RefreshValues();
			this.CharacterWeapon3Slot.RefreshValues();
			this.CharacterWeapon4Slot.RefreshValues();
			this.CharacterBannerSlot.RefreshValues();
			SPInventorySortControllerVM playerInventorySortController = this.PlayerInventorySortController;
			if (playerInventorySortController != null)
			{
				playerInventorySortController.RefreshValues();
			}
			SPInventorySortControllerVM otherInventorySortController = this.OtherInventorySortController;
			if (otherInventorySortController == null)
			{
				return;
			}
			otherInventorySortController.RefreshValues();
		}

		// Token: 0x06000CE3 RID: 3299 RVA: 0x000370B4 File Offset: 0x000352B4
		public override void OnFinalize()
		{
			ItemVM.ProcessEquipItem = null;
			ItemVM.ProcessUnequipItem = null;
			ItemVM.ProcessPreviewItem = null;
			ItemVM.ProcessBuyItem = null;
			SPItemVM.ProcessSellItem = null;
			ItemVM.ProcessItemSelect = null;
			ItemVM.ProcessItemTooltip = null;
			SPItemVM.ProcessItemSlaughter = null;
			SPItemVM.ProcessItemDonate = null;
			SPItemVM.OnFocus = null;
			InventoryTradeVM.RemoveZeroCounts -= this.ExecuteRemoveZeroCounts;
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.ItemPreview.OnFinalize();
			this.ItemPreview = null;
			this.CancelInputKey.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.ResetInputKey.OnFinalize();
			this.PreviousCharacterInputKey.OnFinalize();
			this.NextCharacterInputKey.OnFinalize();
			this.BuyAllInputKey.OnFinalize();
			this.SellAllInputKey.OnFinalize();
			ItemVM.ProcessEquipItem = null;
			ItemVM.ProcessUnequipItem = null;
			ItemVM.ProcessPreviewItem = null;
			ItemVM.ProcessBuyItem = null;
			SPItemVM.ProcessLockItem = null;
			SPItemVM.ProcessSellItem = null;
			ItemVM.ProcessItemSelect = null;
			ItemVM.ProcessItemTooltip = null;
			SPItemVM.ProcessItemSlaughter = null;
			SPItemVM.ProcessItemDonate = null;
			SPItemVM.OnFocus = null;
			this.MainCharacter.OnFinalize();
			this._inventoryLogic = null;
			base.OnFinalize();
		}

		// Token: 0x06000CE4 RID: 3300 RVA: 0x000371E4 File Offset: 0x000353E4
		public void RefreshCallbacks()
		{
			ItemVM.ProcessEquipItem = new Action<ItemVM>(this.ProcessEquipItem);
			ItemVM.ProcessUnequipItem = new Action<ItemVM>(this.ProcessUnequipItem);
			ItemVM.ProcessPreviewItem = new Action<ItemVM>(this.ProcessPreviewItem);
			ItemVM.ProcessBuyItem = new Action<ItemVM, bool>(this.ProcessBuyItem);
			SPItemVM.ProcessLockItem = new Action<SPItemVM, bool>(this.ProcessLockItem);
			SPItemVM.ProcessSellItem = new Action<SPItemVM, bool>(this.ProcessSellItem);
			ItemVM.ProcessItemSelect = new Action<ItemVM>(this.ProcessItemSelect);
			ItemVM.ProcessItemTooltip = new Action<ItemVM>(this.ProcessItemTooltip);
			SPItemVM.ProcessItemSlaughter = new Action<SPItemVM>(this.ProcessItemSlaughter);
			SPItemVM.ProcessItemDonate = new Action<SPItemVM>(this.ProcessItemDonate);
			SPItemVM.OnFocus = new Action<SPItemVM>(this.OnItemFocus);
		}

		// Token: 0x06000CE5 RID: 3301 RVA: 0x000372AC File Offset: 0x000354AC
		private bool CanSelectHero(Hero hero)
		{
			return hero.IsAlive && hero.CanHeroEquipmentBeChanged() && hero.Clan == Clan.PlayerClan && hero.HeroState != Hero.CharacterStates.Disabled && !hero.IsChild;
		}

		// Token: 0x06000CE6 RID: 3302 RVA: 0x000372DF File Offset: 0x000354DF
		private void OnEquipmentModeChanged()
		{
			this.IsCivilianMode = this.EquipmentMode == 0;
			this.IsBattleMode = this.EquipmentMode == 1;
			this.IsStealthMode = this.EquipmentMode == 2;
		}

		// Token: 0x06000CE7 RID: 3303 RVA: 0x0003730E File Offset: 0x0003550E
		private void SetPreviousCharacterHint()
		{
			this.PreviousCharacterHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("HOTKEY", this.GetPreviousCharacterKeyText());
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_prev_char", null));
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
		}

		// Token: 0x06000CE8 RID: 3304 RVA: 0x00037327 File Offset: 0x00035527
		private void SetNextCharacterHint()
		{
			this.NextCharacterHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("HOTKEY", this.GetNextCharacterKeyText());
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_next_char", null));
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
		}

		// Token: 0x06000CE9 RID: 3305 RVA: 0x00037340 File Offset: 0x00035540
		private void SetBuyAllHint()
		{
			TextObject buyAllHintText;
			if (this._usageType == InventoryScreenHelper.InventoryMode.Trade)
			{
				buyAllHintText = GameTexts.FindText("str_inventory_buy_all", null);
			}
			else
			{
				buyAllHintText = GameTexts.FindText("str_inventory_take_all", null);
			}
			this.BuyAllHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("HOTKEY", this.GetBuyAllKeyText());
				GameTexts.SetVariable("TEXT", buyAllHintText);
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
		}

		// Token: 0x06000CEA RID: 3306 RVA: 0x000373A0 File Offset: 0x000355A0
		private void SetSellAllHint()
		{
			TextObject sellAllHintText;
			if (this._usageType == InventoryScreenHelper.InventoryMode.Loot || this._usageType == InventoryScreenHelper.InventoryMode.Stash)
			{
				sellAllHintText = GameTexts.FindText("str_inventory_give_all", null);
			}
			else if (this._usageType == InventoryScreenHelper.InventoryMode.Default)
			{
				sellAllHintText = GameTexts.FindText("str_inventory_discard_all", null);
			}
			else
			{
				sellAllHintText = GameTexts.FindText("str_inventory_sell_all", null);
			}
			this.SellAllHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("HOTKEY", this.GetSellAllKeyText());
				GameTexts.SetVariable("TEXT", sellAllHintText);
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
		}

		// Token: 0x06000CEB RID: 3307 RVA: 0x00037424 File Offset: 0x00035624
		private void OnCharacterSelected(SelectorVM<InventoryCharacterSelectorItemVM> selector)
		{
			if (this._inventoryLogic == null || selector.SelectedItem == null)
			{
				return;
			}
			for (int i = 0; i < this._rightTroopRoster.Count; i++)
			{
				if (this._rightTroopRoster.GetCharacterAtIndex(i).StringId == selector.SelectedItem.CharacterID)
				{
					this.UpdateCurrentCharacterIfPossible(i, true);
					return;
				}
			}
			if (this._leftTroopRoster != null)
			{
				for (int j = 0; j < this._leftTroopRoster.Count; j++)
				{
					if (this._leftTroopRoster.GetCharacterAtIndex(j).StringId == selector.SelectedItem.CharacterID)
					{
						this.UpdateCurrentCharacterIfPossible(j, false);
						return;
					}
				}
			}
		}

		// Token: 0x1700041B RID: 1051
		// (get) Token: 0x06000CEC RID: 3308 RVA: 0x000374D0 File Offset: 0x000356D0
		private Equipment ActiveEquipment
		{
			get
			{
				switch (this.EquipmentMode)
				{
				case 0:
					return this._currentCharacter.FirstCivilianEquipment;
				case 1:
					return this._currentCharacter.FirstBattleEquipment;
				case 2:
					return this._currentCharacter.FirstStealthEquipment;
				default:
					Debug.FailedAssert("Invalid active equipment type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ActiveEquipment", 517);
					return null;
				}
			}
		}

		// Token: 0x06000CED RID: 3309 RVA: 0x00037536 File Offset: 0x00035736
		public void ExecuteShowRecap()
		{
			InformationManager.ShowTooltip(typeof(InventoryLogic), new object[] { this._inventoryLogic });
		}

		// Token: 0x06000CEE RID: 3310 RVA: 0x00037556 File Offset: 0x00035756
		public void ExecuteCancelRecap()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000CEF RID: 3311 RVA: 0x00037560 File Offset: 0x00035760
		public void ExecuteRemoveZeroCounts()
		{
			List<SPItemVM> list = this.LeftItemListVM.ToList<SPItemVM>();
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (list[i].ItemCount == 0 && i >= 0 && i < this.LeftItemListVM.Count)
				{
					list[i].IsSelected = false;
					this.LeftItemListVM.RemoveAt(i);
				}
			}
			List<SPItemVM> list2 = this.RightItemListVM.ToList<SPItemVM>();
			for (int j = list2.Count - 1; j >= 0; j--)
			{
				if (list2[j].ItemCount == 0 && j >= 0 && j < this.RightItemListVM.Count)
				{
					list2[j].IsSelected = false;
					this.RightItemListVM.RemoveAt(j);
				}
			}
		}

		// Token: 0x06000CF0 RID: 3312 RVA: 0x0003761D File Offset: 0x0003581D
		private void ProcessPreviewItem(ItemVM item)
		{
			this._inventoryLogic.IsPreviewingItem = true;
			this.ItemPreview.Open(item.ItemRosterElement.EquipmentElement);
		}

		// Token: 0x06000CF1 RID: 3313 RVA: 0x00037641 File Offset: 0x00035841
		public void ClosePreview()
		{
			this.ItemPreview.Close();
		}

		// Token: 0x06000CF2 RID: 3314 RVA: 0x0003764E File Offset: 0x0003584E
		private void OnPreviewClosed()
		{
			this._inventoryLogic.IsPreviewingItem = false;
		}

		// Token: 0x06000CF3 RID: 3315 RVA: 0x0003765C File Offset: 0x0003585C
		private void ProcessEquipItem(ItemVM draggedItem)
		{
			SPItemVM spitemVM = draggedItem as SPItemVM;
			if ((!spitemVM.IsCivilianItem && this._equipmentMode == SPInventoryVM.EquipmentModes.Civilian) || (!spitemVM.IsStealthItem && this._equipmentMode == SPInventoryVM.EquipmentModes.Stealth))
			{
				return;
			}
			if (!spitemVM.IsTransferable && !this._currentCharacter.IsPlayerCharacter)
			{
				return;
			}
			this.IsRefreshed = false;
			this.EquipEquipment(spitemVM);
			this.RefreshInformationValues();
			this.ExecuteRemoveZeroCounts();
			this.IsRefreshed = true;
		}

		// Token: 0x06000CF4 RID: 3316 RVA: 0x000376C9 File Offset: 0x000358C9
		private void ProcessUnequipItem(ItemVM draggedItem)
		{
			if (!(draggedItem as SPItemVM).IsTransferable && !this._currentCharacter.IsPlayerCharacter)
			{
				return;
			}
			this.IsRefreshed = false;
			this.UnequipEquipment(draggedItem as SPItemVM);
			this.RefreshInformationValues();
			this.IsRefreshed = true;
		}

		// Token: 0x06000CF5 RID: 3317 RVA: 0x00037708 File Offset: 0x00035908
		private void ProcessBuyItem(ItemVM itemBase, bool cameFromTradeData)
		{
			SPItemVM spitemVM = itemBase as SPItemVM;
			if (spitemVM == null || !spitemVM.IsTransferable)
			{
				return;
			}
			if (this.IsEntireStackModifierActive && !cameFromTradeData)
			{
				ItemRosterElement? itemRosterElement;
				this.TransactionCount = ((this._inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.OtherInventory, (spitemVM != null) ? spitemVM.ItemRosterElement.EquipmentElement : EquipmentElement.Invalid) != null) ? itemRosterElement.GetValueOrDefault().Amount : 0);
			}
			else if (this.IsFiveStackModifierActive && !cameFromTradeData)
			{
				this.TransactionCount = 5;
			}
			else
			{
				this.TransactionCount = ((spitemVM != null) ? spitemVM.TransactionCount : 0);
			}
			if (this.TransactionCount == 0)
			{
				Debug.FailedAssert("Transaction count should not be zero", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ProcessBuyItem", 640);
				return;
			}
			this.IsRefreshed = false;
			MBTextManager.SetTextVariable("ITEM_DESCRIPTION", itemBase.ItemDescription, false);
			MBTextManager.SetTextVariable("ITEM_COST", itemBase.ItemCost);
			this.BuyItem(spitemVM);
			if (!cameFromTradeData)
			{
				this.ExecuteRemoveZeroCounts();
			}
			this.RefreshInformationValues();
			this.IsRefreshed = true;
		}

		// Token: 0x06000CF6 RID: 3318 RVA: 0x0003780C File Offset: 0x00035A0C
		private void ProcessSellItem(SPItemVM item, bool cameFromTradeData)
		{
			if (!item.IsTransferable)
			{
				return;
			}
			if (InventoryLogic.IsEquipmentSide(item.InventorySide))
			{
				this.TransactionCount = 1;
			}
			else if (this.IsEntireStackModifierActive && !cameFromTradeData)
			{
				ItemRosterElement? itemRosterElement = this._inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.PlayerInventory, item.ItemRosterElement.EquipmentElement);
				this.TransactionCount = ((itemRosterElement != null) ? itemRosterElement.GetValueOrDefault().Amount : 0);
			}
			else if (this.IsFiveStackModifierActive && !cameFromTradeData)
			{
				this.TransactionCount = 5;
			}
			else
			{
				this.TransactionCount = item.TransactionCount;
			}
			if (this.TransactionCount == 0)
			{
				Debug.FailedAssert("Transaction count should not be zero", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ProcessSellItem", 690);
				return;
			}
			this.IsRefreshed = false;
			MBTextManager.SetTextVariable("ITEM_DESCRIPTION", item.ItemDescription, false);
			MBTextManager.SetTextVariable("ITEM_COST", item.ItemCost);
			this.SellItem(item);
			if (!cameFromTradeData)
			{
				this.ExecuteRemoveZeroCounts();
			}
			this.RefreshInformationValues();
			this.IsRefreshed = true;
		}

		// Token: 0x06000CF7 RID: 3319 RVA: 0x00037908 File Offset: 0x00035B08
		private void ProcessLockItem(SPItemVM item, bool isLocked)
		{
			if (isLocked && item.InventorySide == InventoryLogic.InventorySide.PlayerInventory && !this._lockedItemIDs.Contains(item.StringId))
			{
				this._lockedItemIDs.Add(item.StringId);
				return;
			}
			if (!isLocked && item.InventorySide == InventoryLogic.InventorySide.PlayerInventory && this._lockedItemIDs.Contains(item.StringId))
			{
				this._lockedItemIDs.Remove(item.StringId);
			}
		}

		// Token: 0x06000CF8 RID: 3320 RVA: 0x00037978 File Offset: 0x00035B78
		private ItemVM ProcessCompareItem(ItemVM item, int alternativeUsageIndex = 0)
		{
			this._selectedEquipmentIndex = 0;
			this._comparedItemList.Clear();
			ItemVM itemVM = null;
			bool flag = false;
			EquipmentIndex equipmentIndex = EquipmentIndex.None;
			SPItemVM spitemVM = null;
			bool flag2 = item.ItemType >= EquipmentIndex.WeaponItemBeginSlot && item.ItemType < EquipmentIndex.ExtraWeaponSlot;
			if (!InventoryLogic.IsEquipmentSide(((SPItemVM)item).InventorySide))
			{
				if (flag2)
				{
					for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.ExtraWeaponSlot; equipmentIndex2++)
					{
						EquipmentIndex itemType = equipmentIndex2;
						SPItemVM itemFromIndex = this.GetItemFromIndex(itemType);
						if (itemFromIndex != null && itemFromIndex.ItemRosterElement.EquipmentElement.Item != null && ItemHelper.CheckComparability(item.ItemRosterElement.EquipmentElement.Item, itemFromIndex.ItemRosterElement.EquipmentElement.Item, alternativeUsageIndex))
						{
							this._comparedItemList.Add(itemFromIndex);
						}
					}
					if (!this._comparedItemList.IsEmpty<ItemVM>())
					{
						this.SortComparedItems(item);
						itemVM = this._comparedItemList[0];
						this._lastComparedItemIndex = 0;
					}
					if (itemVM != null)
					{
						equipmentIndex = itemVM.ItemType;
					}
				}
				else
				{
					equipmentIndex = item.ItemType;
				}
			}
			if (item.ItemType >= EquipmentIndex.WeaponItemBeginSlot && item.ItemType < EquipmentIndex.NumEquipmentSetSlots)
			{
				spitemVM = ((equipmentIndex != EquipmentIndex.None) ? this.GetItemFromIndex(equipmentIndex) : null);
				flag = spitemVM != null && !string.IsNullOrEmpty(spitemVM.StringId) && item.StringId != spitemVM.StringId;
			}
			if (!this._selectedTooltipItemStringID.Equals(item.StringId) || (flag && !this._comparedTooltipItemStringID.Equals(spitemVM.StringId)))
			{
				this._selectedTooltipItemStringID = item.StringId;
				if (flag)
				{
					this._comparedTooltipItemStringID = spitemVM.StringId;
				}
			}
			this._selectedEquipmentIndex = (int)equipmentIndex;
			if (spitemVM == null || spitemVM.ItemRosterElement.IsEmpty)
			{
				return null;
			}
			return spitemVM;
		}

		// Token: 0x06000CF9 RID: 3321 RVA: 0x00037B2C File Offset: 0x00035D2C
		private void ResetComparedItems(ItemVM item, int alternativeUsageIndex)
		{
			ItemVM comparedItem = this.ProcessCompareItem(item, alternativeUsageIndex);
			this.ItemMenu.SetItem(this._selectedItem, this.GetEquipmentToInventorySide((SPInventoryVM.EquipmentModes)this.EquipmentMode), comparedItem, this._currentCharacter, alternativeUsageIndex);
		}

		// Token: 0x06000CFA RID: 3322 RVA: 0x00037B68 File Offset: 0x00035D68
		private void SortComparedItems(ItemVM selectedItem)
		{
			List<ItemVM> list = new List<ItemVM>();
			for (int i = 0; i < this._comparedItemList.Count; i++)
			{
				if (selectedItem.StringId == this._comparedItemList[i].StringId && !list.Contains(this._comparedItemList[i]))
				{
					list.Add(this._comparedItemList[i]);
				}
			}
			for (int j = 0; j < this._comparedItemList.Count; j++)
			{
				if (this._comparedItemList[j].ItemRosterElement.EquipmentElement.Item.Type == selectedItem.ItemRosterElement.EquipmentElement.Item.Type && !list.Contains(this._comparedItemList[j]))
				{
					list.Add(this._comparedItemList[j]);
				}
			}
			for (int k = 0; k < this._comparedItemList.Count; k++)
			{
				WeaponComponent weaponComponent = this._comparedItemList[k].ItemRosterElement.EquipmentElement.Item.WeaponComponent;
				WeaponComponent weaponComponent2 = selectedItem.ItemRosterElement.EquipmentElement.Item.WeaponComponent;
				if (((weaponComponent2.Weapons.Count > 1 && weaponComponent2.Weapons[1].WeaponClass == weaponComponent.Weapons[0].WeaponClass) || (weaponComponent.Weapons.Count > 1 && weaponComponent.Weapons[1].WeaponClass == weaponComponent2.Weapons[0].WeaponClass) || (weaponComponent2.Weapons.Count > 1 && weaponComponent.Weapons.Count > 1 && weaponComponent2.Weapons[1].WeaponClass == weaponComponent.Weapons[1].WeaponClass)) && !list.Contains(this._comparedItemList[k]))
				{
					list.Add(this._comparedItemList[k]);
				}
			}
			if (this._comparedItemList.Count != list.Count)
			{
				foreach (ItemVM item in this._comparedItemList)
				{
					if (!list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
			this._comparedItemList = list;
		}

		// Token: 0x06000CFB RID: 3323 RVA: 0x00037DF4 File Offset: 0x00035FF4
		public void ProcessItemTooltip(ItemVM item)
		{
			if (item == null || string.IsNullOrEmpty(item.StringId))
			{
				return;
			}
			this._selectedItem = item as SPItemVM;
			ItemVM comparedItem = this.ProcessCompareItem(item, 0);
			this.ItemMenu.SetItem(this._selectedItem, this.GetEquipmentToInventorySide((SPInventoryVM.EquipmentModes)this.EquipmentMode), comparedItem, this._currentCharacter, 0);
			this.RefreshTransactionCost(1);
			this._selectedItem.UpdateCanBeSlaughtered();
		}

		// Token: 0x06000CFC RID: 3324 RVA: 0x00037E5E File Offset: 0x0003605E
		public void ResetSelectedItem()
		{
			this._selectedItem = null;
		}

		// Token: 0x06000CFD RID: 3325 RVA: 0x00037E68 File Offset: 0x00036068
		private void ProcessItemSlaughter(SPItemVM item)
		{
			this.IsRefreshed = false;
			if (string.IsNullOrEmpty(item.StringId) || !item.CanBeSlaughtered)
			{
				return;
			}
			this.SlaughterItem(item);
			this.RefreshInformationValues();
			if (item.ItemCount == 0)
			{
				this.ExecuteRemoveZeroCounts();
			}
			this.IsRefreshed = true;
		}

		// Token: 0x06000CFE RID: 3326 RVA: 0x00037EB4 File Offset: 0x000360B4
		private void ProcessItemDonate(SPItemVM item)
		{
			this.IsRefreshed = false;
			if (string.IsNullOrEmpty(item.StringId) || !item.CanBeDonated)
			{
				return;
			}
			this.DonateItem(item);
			this.RefreshInformationValues();
			if (item.ItemCount == 0)
			{
				this.ExecuteRemoveZeroCounts();
			}
			this.IsRefreshed = true;
		}

		// Token: 0x06000CFF RID: 3327 RVA: 0x00037F00 File Offset: 0x00036100
		private void OnItemFocus(SPItemVM item)
		{
			this.CurrentFocusedItem = item;
		}

		// Token: 0x06000D00 RID: 3328 RVA: 0x00037F09 File Offset: 0x00036109
		private void ProcessItemSelect(ItemVM item)
		{
			this.ExecuteRemoveZeroCounts();
			this.ExecuteSelectItem(item);
		}

		// Token: 0x06000D01 RID: 3329 RVA: 0x00037F18 File Offset: 0x00036118
		private void RefreshTransactionCost(int transactionCount = 1)
		{
			if (this._selectedItem != null && this.IsTrading)
			{
				int maxIndividualPrice;
				int itemTotalPrice = this._inventoryLogic.GetItemTotalPrice(this._selectedItem.ItemRosterElement, transactionCount, out maxIndividualPrice, this._selectedItem.InventorySide == InventoryLogic.InventorySide.OtherInventory);
				this.ItemMenu.SetTransactionCost(itemTotalPrice, maxIndividualPrice);
			}
		}

		// Token: 0x06000D02 RID: 3330 RVA: 0x00037F6C File Offset: 0x0003616C
		public void RefreshComparedItem()
		{
			this._lastComparedItemIndex++;
			if (this._lastComparedItemIndex > this._comparedItemList.Count - 1)
			{
				this._lastComparedItemIndex = 0;
			}
			if (!this._comparedItemList.IsEmpty<ItemVM>() && this._selectedItem != null && this._comparedItemList[this._lastComparedItemIndex] != null)
			{
				this.ItemMenu.SetItem(this._selectedItem, this.GetEquipmentToInventorySide((SPInventoryVM.EquipmentModes)this.EquipmentMode), this._comparedItemList[this._lastComparedItemIndex], this._currentCharacter, 0);
			}
		}

		// Token: 0x06000D03 RID: 3331 RVA: 0x00038000 File Offset: 0x00036200
		private void AfterReset(InventoryLogic itemRoster, bool fromCancel)
		{
			this._inventoryLogic = itemRoster;
			if (!fromCancel)
			{
				switch (this.ActiveFilterIndex)
				{
				case 1:
					this._inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Weapon;
					break;
				case 2:
					this._inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Shield;
					break;
				case 3:
					this._inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Armors;
					break;
				case 4:
					this._inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.HorseCategory;
					break;
				case 5:
					this._inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Goods;
					break;
				default:
					this._inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.All;
					break;
				}
				this.InitializeInventory();
				this.PlayerInventorySortController = new SPInventorySortControllerVM(ref this._rightItemListVM);
				this.OtherInventorySortController = new SPInventorySortControllerVM(ref this._leftItemListVM);
				this.PlayerInventorySortController.SortByDefaultState();
				this.OtherInventorySortController.SortByDefaultState();
				Tuple<int, int> tuple = this._viewDataTracker.InventoryGetSortPreference((int)this._usageType);
				if (tuple != null)
				{
					this.PlayerInventorySortController.SortByOption((SPInventorySortControllerVM.InventoryItemSortOption)tuple.Item1, (SPInventorySortControllerVM.InventoryItemSortState)tuple.Item2);
				}
				this.UpdateRightCharacter();
				this.UpdateLeftCharacter();
				this.RightInventoryOwnerName = PartyBase.MainParty.Name.ToString();
				this.RightInventoryOwnerGold = Hero.MainHero.Gold;
			}
		}

		// Token: 0x06000D04 RID: 3332 RVA: 0x0003812C File Offset: 0x0003632C
		private void OnTotalAmountChange(int newTotalAmount)
		{
			MBTextManager.SetTextVariable("PAY_OR_GET", (this._inventoryLogic.TotalAmount < 0) ? 1 : 0);
			int f = MathF.Min(-this._inventoryLogic.TotalAmount, this._inventoryLogic.InventoryListener.GetGold());
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(f));
			this.TradeLbl = ((this._inventoryLogic.TotalAmount == 0) ? "" : GameTexts.FindText("str_inventory_trade_label", null).ToString());
			this.RightInventoryOwnerGold = Hero.MainHero.Gold - this._inventoryLogic.TotalAmount;
			InventoryListener inventoryListener = this._inventoryLogic.InventoryListener;
			this.LeftInventoryOwnerGold = (((inventoryListener != null) ? new int?(inventoryListener.GetGold()) : null) + this._inventoryLogic.TotalAmount) ?? 0;
		}

		// Token: 0x06000D05 RID: 3333 RVA: 0x0003823C File Offset: 0x0003643C
		private void OnDonationXpChange()
		{
			int num = (int)this._inventoryLogic.XpGainFromDonations;
			bool isDonationXpGainExceedsMax = false;
			if (num > this._donationMaxShareableXp)
			{
				num = this._donationMaxShareableXp;
				isDonationXpGainExceedsMax = true;
			}
			this.IsDonationXpGainExceedsMax = isDonationXpGainExceedsMax;
			this.HasGainedExperience = num > 0;
			MBTextManager.SetTextVariable("XP_AMOUNT", num);
			this.ExperienceLbl = ((num == 0) ? "" : GameTexts.FindText("str_inventory_donation_label", null).ToString());
		}

		// Token: 0x06000D06 RID: 3334 RVA: 0x000382A8 File Offset: 0x000364A8
		private void AfterTransfer(InventoryLogic inventoryLogic, List<TransferCommandResult> results)
		{
			this._isCharacterEquipmentDirty = false;
			List<SPItemVM> list = new List<SPItemVM>();
			HashSet<ItemCategory> hashSet = new HashSet<ItemCategory>();
			for (int num = 0; num != results.Count; num++)
			{
				TransferCommandResult transferCommandResult = results[num];
				if (transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory || transferCommandResult.ResultSide == InventoryLogic.InventorySide.PlayerInventory)
				{
					bool flag = false;
					MBBindingList<SPItemVM> mbbindingList = ((transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory) ? this.LeftItemListVM : this.RightItemListVM);
					for (int i = 0; i < mbbindingList.Count; i++)
					{
						SPItemVM spitemVM = mbbindingList[i];
						if (spitemVM != null && spitemVM.ItemRosterElement.EquipmentElement.IsEqualTo(transferCommandResult.EffectedItemRosterElement.EquipmentElement))
						{
							spitemVM.ItemRosterElement.Amount = transferCommandResult.FinalNumber;
							spitemVM.ItemCount = transferCommandResult.FinalNumber;
							spitemVM.ItemCost = this._inventoryLogic.GetItemPrice(spitemVM.ItemRosterElement.EquipmentElement, transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory);
							list.Add(spitemVM);
							if (!hashSet.Contains(spitemVM.ItemRosterElement.EquipmentElement.Item.GetItemCategory()))
							{
								hashSet.Add(spitemVM.ItemRosterElement.EquipmentElement.Item.GetItemCategory());
							}
							if (spitemVM.IsSelected)
							{
								this.ScrollItemId = spitemVM.ItemRosterElement.EquipmentElement.Item.StringId;
								this.ScrollToItem = true;
							}
							flag = true;
							break;
						}
					}
					if (!flag && transferCommandResult.EffectedNumber > 0 && this._inventoryLogic != null)
					{
						SPItemVM newItem = null;
						SPItemVM spitemVM2;
						if (transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory)
						{
							newItem = new SPItemVM(this._inventoryLogic, this.MainCharacter.IsFemale, this.CanCharacterUseItemBasedOnSkills(transferCommandResult.EffectedItemRosterElement), this._usageType, transferCommandResult.EffectedItemRosterElement, InventoryLogic.InventorySide.OtherInventory, this._inventoryLogic.GetCostOfItemRosterElement(transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide), null);
							spitemVM2 = this.RightItemListVM.FirstOrDefault((SPItemVM x) => x.ItemRosterElement.EquipmentElement.IsEqualTo(newItem.ItemRosterElement.EquipmentElement));
						}
						else
						{
							newItem = new SPItemVM(this._inventoryLogic, this.MainCharacter.IsFemale, this.CanCharacterUseItemBasedOnSkills(transferCommandResult.EffectedItemRosterElement), this._usageType, transferCommandResult.EffectedItemRosterElement, InventoryLogic.InventorySide.PlayerInventory, this._inventoryLogic.GetCostOfItemRosterElement(transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide), null);
							spitemVM2 = this.LeftItemListVM.FirstOrDefault((SPItemVM x) => x.ItemRosterElement.EquipmentElement.IsEqualTo(newItem.ItemRosterElement.EquipmentElement));
						}
						this.UpdateFilteredStatusOfItem(newItem);
						newItem.ItemCount = transferCommandResult.FinalNumber;
						newItem.IsLocked = newItem.InventorySide == InventoryLogic.InventorySide.PlayerInventory && this._lockedItemIDs.Contains(newItem.StringId);
						newItem.IsNew = true;
						newItem.IsSelected = spitemVM2 != null && spitemVM2.IsSelected;
						mbbindingList.Add(newItem);
						if (newItem.IsSelected)
						{
							this.ScrollItemId = transferCommandResult.EffectedItemRosterElement.EquipmentElement.Item.StringId;
							this.ScrollToItem = true;
						}
					}
				}
				else if (InventoryLogic.IsEquipmentSide(transferCommandResult.ResultSide))
				{
					SPItemVM spitemVM3 = null;
					if (transferCommandResult.FinalNumber > 0)
					{
						spitemVM3 = new SPItemVM(this._inventoryLogic, this.MainCharacter.IsFemale, this.CanCharacterUseItemBasedOnSkills(transferCommandResult.EffectedItemRosterElement), this._usageType, transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide, this._inventoryLogic.GetCostOfItemRosterElement(transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide), new EquipmentIndex?(transferCommandResult.EffectedEquipmentIndex));
						spitemVM3.IsNew = true;
					}
					this.UpdateEquipment(transferCommandResult.ResultSideEquipment, spitemVM3, transferCommandResult.EffectedEquipmentIndex);
					this._isCharacterEquipmentDirty = true;
				}
			}
			SPItemVM selectedItem = this._selectedItem;
			if (selectedItem != null && selectedItem.ItemCount > 1)
			{
				this.ProcessItemTooltip(this._selectedItem);
				this._selectedItem.UpdateCanBeSlaughtered();
			}
			this.CheckEquipAfterTransferStack();
			if (!this.ActiveEquipment[EquipmentIndex.HorseHarness].IsEmpty && this.ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
			{
				this.UnequipEquipment(this.CharacterMountArmorSlot);
			}
			if (!this.ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty && !this.ActiveEquipment[EquipmentIndex.HorseHarness].IsEmpty && this.ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent.Monster.FamilyType != this.ActiveEquipment[EquipmentIndex.HorseHarness].Item.ArmorComponent.FamilyType)
			{
				this.UnequipEquipment(this.CharacterMountArmorSlot);
			}
			foreach (SPItemVM spitemVM4 in list)
			{
				spitemVM4.UpdateTradeData(true);
				spitemVM4.UpdateCanBeSlaughtered();
			}
			this.UpdateCostOfItemsInCategory(hashSet);
			if (PartyBase.MainParty.IsMobile)
			{
				PartyBase.MainParty.MobileParty.MemberRoster.UpdateVersion();
				PartyBase.MainParty.MobileParty.PrisonRoster.UpdateVersion();
			}
		}

		// Token: 0x06000D07 RID: 3335 RVA: 0x00038824 File Offset: 0x00036A24
		private void UpdateCostOfItemsInCategory(HashSet<ItemCategory> categories)
		{
			foreach (SPItemVM spitemVM in this.LeftItemListVM)
			{
				if (categories.Contains(spitemVM.ItemRosterElement.EquipmentElement.Item.GetItemCategory()))
				{
					spitemVM.ItemCost = this._inventoryLogic.GetCostOfItemRosterElement(spitemVM.ItemRosterElement, InventoryLogic.InventorySide.OtherInventory);
				}
			}
			foreach (SPItemVM spitemVM2 in this.RightItemListVM)
			{
				if (categories.Contains(spitemVM2.ItemRosterElement.EquipmentElement.Item.GetItemCategory()))
				{
					spitemVM2.ItemCost = this._inventoryLogic.GetCostOfItemRosterElement(spitemVM2.ItemRosterElement, InventoryLogic.InventorySide.PlayerInventory);
				}
			}
		}

		// Token: 0x06000D08 RID: 3336 RVA: 0x00038910 File Offset: 0x00036B10
		private void CheckEquipAfterTransferStack()
		{
			while (this._equipAfterTransferStack.Count > 0)
			{
				SPItemVM spitemVM = new SPItemVM();
				spitemVM.RefreshWith(this._equipAfterTransferStack.Pop(), InventoryLogic.InventorySide.PlayerInventory);
				this.EquipEquipment(spitemVM);
			}
		}

		// Token: 0x06000D09 RID: 3337 RVA: 0x0003894C File Offset: 0x00036B4C
		private void RefreshInformationValues()
		{
			TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT", null);
			int inventoryCapacity = MobileParty.MainParty.InventoryCapacity;
			float totalWeightCarried = MobileParty.MainParty.TotalWeightCarried;
			this.MainPartyTotalWeightCarriedText = totalWeightCarried.ToString("#0");
			this.MainPartyInventoryCapacityText = inventoryCapacity.ToString();
			this.PlayerEquipmentCountWarned = totalWeightCarried > (float)inventoryCapacity;
			this.ShowMainPartyLandCapacityTexts = MobileParty.MainParty.IsCurrentlyAtSea;
			this.ShowMainPartySeaCapacityTexts = !this.ShowMainPartyLandCapacityTexts && MobileParty.MainParty.HasNavalNavigationCapability;
			if (this.ShowMainPartyLandCapacityTexts)
			{
				int num = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(MobileParty.MainParty, false, false).ResultNumber;
				int num2 = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(MobileParty.MainParty, false, false, 0, 0, 0, false).ResultNumber;
				this.MainPartyLandWeightText = num.ToString();
				this.MainPartyLandCapacityText = num2.ToString();
				this.IsMainPartyLandCapacityWarned = num > num2;
			}
			else if (this.ShowMainPartySeaCapacityTexts)
			{
				int num3 = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(MobileParty.MainParty, true, false).ResultNumber;
				int num4 = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(MobileParty.MainParty, true, false, 0, 0, 0, false).ResultNumber;
				this.MainPartySeaWeightText = num3.ToString();
				this.MainPartySeaCapacityText = num4.ToString();
				this.IsMainPartySeaCapacityWarned = num3 > num4;
			}
			this.ShowMainPartyLandCapacityWarning = this.ShowMainPartyLandCapacityTexts && this.IsMainPartyLandCapacityWarned && !this.PlayerEquipmentCountWarned;
			this.ShowMainPartySeaCapacityWarning = this.ShowMainPartySeaCapacityTexts && this.IsMainPartySeaCapacityWarned && !this.PlayerEquipmentCountWarned;
			if (this.OtherSideHasCapacity)
			{
				int otherSideCurrentWeight = this._inventoryLogic.OtherSideCurrentWeight;
				int capacity = this._inventoryLogic.OtherSideCapacityData.GetCapacity();
				textObject.SetTextVariable("LEFT", otherSideCurrentWeight);
				textObject.SetTextVariable("RIGHT", capacity);
				this.OtherEquipmentCountText = textObject.ToString();
				this.OtherEquipmentCountWarned = otherSideCurrentWeight > capacity;
				this.OtherEquipmentCapacityExceededWarning = this.OtherEquipmentCountWarned && !this.PlayerEquipmentCountWarned && !this.IsMainPartyLandCapacityWarned && !this.IsMainPartySeaCapacityWarned;
			}
			this.NoSaddleText = new TextObject("{=QSPrSsHv}No Saddle!", null).ToString();
			this.NoSaddleHint = new HintViewModel(new TextObject("{=VzCoqt8D}No sadle equipped. -10% penalty to mounted speed and maneuver.", null), null);
			SPItemVM characterMountSlot = this.CharacterMountSlot;
			bool noSaddleWarned;
			if (characterMountSlot != null && !characterMountSlot.ItemRosterElement.IsEmpty)
			{
				SPItemVM characterMountArmorSlot = this.CharacterMountArmorSlot;
				noSaddleWarned = characterMountArmorSlot != null && characterMountArmorSlot.ItemRosterElement.IsEmpty;
			}
			else
			{
				noSaddleWarned = false;
			}
			this.NoSaddleWarned = noSaddleWarned;
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				this.InventoryCapacityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryCapacityTooltip(MobileParty.MainParty, false, false));
				this.LandCapacityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryCapacityTooltip(MobileParty.MainParty, true, false));
				this.SeaCapacityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryCapacityTooltip(MobileParty.MainParty, false, true));
				this.TotalWeightCarriedHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryWeightTooltip(MobileParty.MainParty, false, false));
				this.LandWeightHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryWeightTooltip(MobileParty.MainParty, true, false));
				this.SeaWeightHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryWeightTooltip(MobileParty.MainParty, false, true));
			}
			if (this._isCharacterEquipmentDirty)
			{
				this.MainCharacter.SetEquipment(this.ActiveEquipment);
				this.UpdateCharacterArmorValues();
				this.RefreshCharacterTotalWeight();
			}
			this._isCharacterEquipmentDirty = false;
			this.UpdateIsDoneDisabled();
		}

		// Token: 0x06000D0A RID: 3338 RVA: 0x00038D44 File Offset: 0x00036F44
		public bool IsItemEquipmentPossible(SPItemVM itemVM)
		{
			if (itemVM == null)
			{
				return false;
			}
			if (!this._currentCharacter.IsPlayerCharacter && (itemVM == null || !itemVM.IsTransferable))
			{
				return false;
			}
			if (this.TargetEquipmentType == EquipmentIndex.None)
			{
				this.TargetEquipmentType = itemVM.GetItemTypeWithItemObject();
				if (this.TargetEquipmentType == EquipmentIndex.None)
				{
					return false;
				}
				if (this.TargetEquipmentType == EquipmentIndex.WeaponItemBeginSlot)
				{
					EquipmentIndex targetEquipmentType = EquipmentIndex.WeaponItemBeginSlot;
					bool flag = false;
					bool flag2 = false;
					SPItemVM[] array = new SPItemVM[] { this.CharacterWeapon1Slot, this.CharacterWeapon2Slot, this.CharacterWeapon3Slot, this.CharacterWeapon4Slot };
					for (int i = 0; i < array.Length; i++)
					{
						if (string.IsNullOrEmpty(array[i].StringId))
						{
							flag = true;
							targetEquipmentType = EquipmentIndex.WeaponItemBeginSlot + i;
							break;
						}
						if (!flag2 && array[i].ItemRosterElement.EquipmentElement.Item.Type == itemVM.ItemRosterElement.EquipmentElement.Item.Type)
						{
							flag2 = true;
							targetEquipmentType = EquipmentIndex.WeaponItemBeginSlot + i;
						}
					}
					if (flag || flag2)
					{
						this.TargetEquipmentType = targetEquipmentType;
					}
					else
					{
						this.TargetEquipmentType = EquipmentIndex.WeaponItemBeginSlot;
					}
				}
			}
			else if (itemVM.ItemType != this.TargetEquipmentType && (this.TargetEquipmentType < EquipmentIndex.WeaponItemBeginSlot || this.TargetEquipmentType > EquipmentIndex.Weapon3 || itemVM.ItemType < EquipmentIndex.WeaponItemBeginSlot || itemVM.ItemType > EquipmentIndex.Weapon3))
			{
				return false;
			}
			if (!this.CanCharacterUseItemBasedOnSkills(itemVM.ItemRosterElement))
			{
				TextObject textObject = new TextObject("{=rgqA29b8}You don't have enough {SKILL_NAME} skill to equip this item", null);
				textObject.SetTextVariable("SKILL_NAME", itemVM.ItemRosterElement.EquipmentElement.Item.RelevantSkill.Name);
				MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
				return false;
			}
			if (!this.CanCharacterUserItemBasedOnUsability(itemVM.ItemRosterElement))
			{
				TextObject textObject2 = new TextObject("{=ITKb4cKv}{ITEM_NAME} is not equippable.", null);
				textObject2.SetTextVariable("ITEM_NAME", itemVM.ItemRosterElement.EquipmentElement.GetModifiedItemName());
				MBInformationManager.AddQuickInformation(textObject2, 0, null, null, "");
				return false;
			}
			if (!Equipment.IsItemFitsToSlot((EquipmentIndex)this.TargetEquipmentIndex, itemVM.ItemRosterElement.EquipmentElement.Item))
			{
				TextObject textObject3 = new TextObject("{=Omjlnsk3}{ITEM_NAME} cannot be equipped on this slot.", null);
				textObject3.SetTextVariable("ITEM_NAME", itemVM.ItemRosterElement.EquipmentElement.GetModifiedItemName());
				MBInformationManager.AddQuickInformation(textObject3, 0, null, null, "");
				return false;
			}
			if (this.TargetEquipmentType == EquipmentIndex.HorseHarness)
			{
				if (string.IsNullOrEmpty(this.CharacterMountSlot.StringId))
				{
					return false;
				}
				if (!this.ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty && this.ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent.Monster.FamilyType != itemVM.ItemRosterElement.EquipmentElement.Item.ArmorComponent.FamilyType)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000D0B RID: 3339 RVA: 0x00039000 File Offset: 0x00037200
		private bool CanCharacterUserItemBasedOnUsability(ItemRosterElement itemRosterElement)
		{
			return !itemRosterElement.EquipmentElement.Item.HasHorseComponent || itemRosterElement.EquipmentElement.Item.HorseComponent.IsRideable;
		}

		// Token: 0x06000D0C RID: 3340 RVA: 0x00039041 File Offset: 0x00037241
		private bool CanCharacterUseItemBasedOnSkills(ItemRosterElement itemRosterElement)
		{
			return CharacterHelper.CanUseItemBasedOnSkill(this._currentCharacter, itemRosterElement.EquipmentElement);
		}

		// Token: 0x06000D0D RID: 3341 RVA: 0x00039058 File Offset: 0x00037258
		private void EquipEquipment(SPItemVM itemVM)
		{
			if (itemVM == null || string.IsNullOrEmpty(itemVM.StringId))
			{
				return;
			}
			SPItemVM spitemVM = new SPItemVM();
			spitemVM.RefreshWith(itemVM, this.GetEquipmentToInventorySide(this._equipmentMode));
			if (!this.IsItemEquipmentPossible(spitemVM))
			{
				return;
			}
			SPItemVM itemFromIndex = this.GetItemFromIndex(this.TargetEquipmentType);
			if (itemFromIndex != null && itemFromIndex.ItemRosterElement.EquipmentElement.IsEqualTo(spitemVM.ItemRosterElement.EquipmentElement))
			{
				return;
			}
			bool flag = itemFromIndex != null && itemFromIndex.ItemType != EquipmentIndex.None && InventoryLogic.IsEquipmentSide(itemVM.InventorySide);
			if (!flag)
			{
				EquipmentIndex equipmentIndex = EquipmentIndex.None;
				if (itemVM.ItemRosterElement.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Shield && !InventoryLogic.IsEquipmentSide(itemVM.InventorySide))
				{
					for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 <= EquipmentIndex.NumAllWeaponSlots; equipmentIndex2++)
					{
						SPItemVM itemFromIndex2 = this.GetItemFromIndex(equipmentIndex2);
						bool flag2;
						if (itemFromIndex2 == null)
						{
							flag2 = false;
						}
						else
						{
							ItemObject item = itemFromIndex2.ItemRosterElement.EquipmentElement.Item;
							ItemObject.ItemTypeEnum? itemTypeEnum = ((item != null) ? new ItemObject.ItemTypeEnum?(item.Type) : null);
							ItemObject.ItemTypeEnum itemTypeEnum2 = ItemObject.ItemTypeEnum.Shield;
							flag2 = (itemTypeEnum.GetValueOrDefault() == itemTypeEnum2) & (itemTypeEnum != null);
						}
						if (flag2)
						{
							equipmentIndex = equipmentIndex2;
							break;
						}
					}
				}
				if (itemVM != null)
				{
					ItemObject item2 = itemVM.ItemRosterElement.EquipmentElement.Item;
					ItemObject.ItemTypeEnum? itemTypeEnum = ((item2 != null) ? new ItemObject.ItemTypeEnum?(item2.Type) : null);
					ItemObject.ItemTypeEnum itemTypeEnum2 = ItemObject.ItemTypeEnum.Shield;
					if (((itemTypeEnum.GetValueOrDefault() == itemTypeEnum2) & (itemTypeEnum != null)) && equipmentIndex != EquipmentIndex.None)
					{
						this.TargetEquipmentType = equipmentIndex;
					}
				}
			}
			List<TransferCommand> list = new List<TransferCommand>();
			TransferCommand item3 = TransferCommand.Transfer(1, itemVM.InventorySide, this.GetEquipmentToInventorySide(this._equipmentMode), spitemVM.ItemRosterElement, spitemVM.ItemType, this.TargetEquipmentType, this._currentCharacter);
			list.Add(item3);
			if (flag)
			{
				TransferCommand item4 = TransferCommand.Transfer(1, InventoryLogic.InventorySide.PlayerInventory, this.GetEquipmentToInventorySide(this._equipmentMode), itemFromIndex.ItemRosterElement, EquipmentIndex.None, spitemVM.ItemType, this._currentCharacter);
				list.Add(item4);
			}
			this._inventoryLogic.AddTransferCommands(list);
		}

		// Token: 0x06000D0E RID: 3342 RVA: 0x0003925C File Offset: 0x0003745C
		private void UnequipEquipment(SPItemVM itemVM)
		{
			if (itemVM == null || string.IsNullOrEmpty(itemVM.StringId))
			{
				return;
			}
			TransferCommand command = TransferCommand.Transfer(1, this.GetEquipmentToInventorySide(this._equipmentMode), InventoryLogic.InventorySide.PlayerInventory, itemVM.ItemRosterElement, itemVM.ItemType, itemVM.ItemType, this._currentCharacter);
			this._inventoryLogic.AddTransferCommand(command);
			itemVM.IsSelected = false;
		}

		// Token: 0x06000D0F RID: 3343 RVA: 0x000392BC File Offset: 0x000374BC
		private void UpdateEquipment(Equipment equipment, SPItemVM itemVM, EquipmentIndex itemType)
		{
			if (this.ActiveEquipment == equipment)
			{
				this.RefreshEquipment(itemVM, itemType);
			}
			equipment[itemType] = ((itemVM == null) ? default(EquipmentElement) : itemVM.ItemRosterElement.EquipmentElement);
		}

		// Token: 0x06000D10 RID: 3344 RVA: 0x000392FC File Offset: 0x000374FC
		private void UnequipEquipmentWithEquipmentIndex(EquipmentIndex slotType)
		{
			switch (slotType)
			{
			case EquipmentIndex.None:
				return;
			case EquipmentIndex.WeaponItemBeginSlot:
				this.UnequipEquipment(this.CharacterWeapon1Slot);
				return;
			case EquipmentIndex.Weapon1:
				this.UnequipEquipment(this.CharacterWeapon2Slot);
				return;
			case EquipmentIndex.Weapon2:
				this.UnequipEquipment(this.CharacterWeapon3Slot);
				return;
			case EquipmentIndex.Weapon3:
				this.UnequipEquipment(this.CharacterWeapon4Slot);
				return;
			case EquipmentIndex.ExtraWeaponSlot:
				this.UnequipEquipment(this.CharacterBannerSlot);
				return;
			case EquipmentIndex.NumAllWeaponSlots:
				this.UnequipEquipment(this.CharacterHelmSlot);
				return;
			case EquipmentIndex.Body:
				this.UnequipEquipment(this.CharacterTorsoSlot);
				return;
			case EquipmentIndex.Leg:
				this.UnequipEquipment(this.CharacterBootSlot);
				return;
			case EquipmentIndex.Gloves:
				this.UnequipEquipment(this.CharacterGloveSlot);
				return;
			case EquipmentIndex.Cape:
				this.UnequipEquipment(this.CharacterCloakSlot);
				return;
			case EquipmentIndex.ArmorItemEndSlot:
				this.UnequipEquipment(this.CharacterMountSlot);
				if (!string.IsNullOrEmpty(this.CharacterMountArmorSlot.StringId))
				{
					this.UnequipEquipment(this.CharacterMountArmorSlot);
				}
				return;
			case EquipmentIndex.HorseHarness:
				this.UnequipEquipment(this.CharacterMountArmorSlot);
				return;
			default:
				return;
			}
		}

		// Token: 0x06000D11 RID: 3345 RVA: 0x00039400 File Offset: 0x00037600
		protected void RefreshEquipment(SPItemVM itemVM, EquipmentIndex itemType)
		{
			InventoryLogic.InventorySide equipmentToInventorySide = this.GetEquipmentToInventorySide(this._equipmentMode);
			switch (itemType)
			{
			case EquipmentIndex.None:
				return;
			case EquipmentIndex.WeaponItemBeginSlot:
				this.CharacterWeapon1Slot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.Weapon1:
				this.CharacterWeapon2Slot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.Weapon2:
				this.CharacterWeapon3Slot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.Weapon3:
				this.CharacterWeapon4Slot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.ExtraWeaponSlot:
				this.CharacterBannerSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.NumAllWeaponSlots:
				this.CharacterHelmSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.Body:
				this.CharacterTorsoSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.Leg:
				this.CharacterBootSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.Gloves:
				this.CharacterGloveSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.Cape:
				this.CharacterCloakSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.ArmorItemEndSlot:
				this.CharacterMountSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			case EquipmentIndex.HorseHarness:
				this.CharacterMountArmorSlot.RefreshWith(itemVM, equipmentToInventorySide);
				return;
			default:
				return;
			}
		}

		// Token: 0x06000D12 RID: 3346 RVA: 0x00039500 File Offset: 0x00037700
		private bool UpdateCurrentCharacterIfPossible(int characterIndex, bool isFromRightSide)
		{
			CharacterObject character = (isFromRightSide ? this._rightTroopRoster : this._leftTroopRoster).GetElementCopyAtIndex(characterIndex).Character;
			if (character.IsHero)
			{
				if (!character.HeroObject.CanHeroEquipmentBeChanged())
				{
					Hero mainHero = Hero.MainHero;
					bool flag;
					if (mainHero == null)
					{
						flag = false;
					}
					else
					{
						Clan clan = mainHero.Clan;
						bool? flag2 = ((clan != null) ? new bool?(clan.AliveLords.Contains(character.HeroObject)) : null);
						bool flag3 = true;
						flag = (flag2.GetValueOrDefault() == flag3) & (flag2 != null);
					}
					if (!flag)
					{
						return false;
					}
				}
				this._currentInventoryCharacterIndex = characterIndex;
				this._currentCharacter = character;
				this.MainCharacter.FillFrom(this._currentCharacter.HeroObject, -1, false, false);
				if (this._currentCharacter.IsHero)
				{
					CharacterViewModel mainCharacter = this.MainCharacter;
					IFaction mapFaction = this._currentCharacter.HeroObject.MapFaction;
					mainCharacter.ArmorColor1 = ((mapFaction != null) ? mapFaction.Color : 0U);
					CharacterViewModel mainCharacter2 = this.MainCharacter;
					IFaction mapFaction2 = this._currentCharacter.HeroObject.MapFaction;
					mainCharacter2.ArmorColor2 = ((mapFaction2 != null) ? mapFaction2.Color2 : 0U);
				}
				this.UpdateRightCharacter();
				this.RefreshInformationValues();
				return true;
			}
			return false;
		}

		// Token: 0x06000D13 RID: 3347 RVA: 0x00039624 File Offset: 0x00037824
		private bool DoesCompanionExist()
		{
			for (int i = 1; i < this._rightTroopRoster.Count; i++)
			{
				CharacterObject character = this._rightTroopRoster.GetElementCopyAtIndex(i).Character;
				if (character.IsHero && !character.HeroObject.CanHeroEquipmentBeChanged() && character.HeroObject != Hero.MainHero)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000D14 RID: 3348 RVA: 0x00039680 File Offset: 0x00037880
		private void UpdateLeftCharacter()
		{
			this.IsTradingWithSettlement = false;
			if (this._inventoryLogic.LeftRosterName != null)
			{
				this.LeftInventoryOwnerName = this._inventoryLogic.LeftRosterName.ToString();
				Settlement settlement = this._currentCharacter.HeroObject.CurrentSettlement;
				InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
				InventoryScreenHelper.InventoryMode inventoryMode = ((activeInventoryState != null) ? activeInventoryState.InventoryMode : InventoryScreenHelper.InventoryMode.Default);
				if (settlement != null && inventoryMode == InventoryScreenHelper.InventoryMode.Warehouse)
				{
					this.IsTradingWithSettlement = true;
					this.ProductionTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetSettlementProductionTooltip(settlement));
					return;
				}
			}
			else
			{
				Settlement settlement = this._currentCharacter.HeroObject.CurrentSettlement;
				if (settlement != null)
				{
					this.LeftInventoryOwnerName = settlement.Name.ToString();
					this.ProductionTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetSettlementProductionTooltip(settlement));
					this.IsTradingWithSettlement = !settlement.IsHideout;
					if (this._inventoryLogic.InventoryListener != null)
					{
						this.LeftInventoryOwnerGold = this._inventoryLogic.InventoryListener.GetGold();
						return;
					}
				}
				else
				{
					PartyBase oppositePartyFromListener = this._inventoryLogic.OppositePartyFromListener;
					MobileParty mobileParty = ((oppositePartyFromListener != null) ? oppositePartyFromListener.MobileParty : null);
					if (mobileParty != null && (mobileParty.IsCaravan || mobileParty.IsVillager))
					{
						this.LeftInventoryOwnerName = mobileParty.Name.ToString();
						InventoryListener inventoryListener = this._inventoryLogic.InventoryListener;
						this.LeftInventoryOwnerGold = ((inventoryListener != null) ? inventoryListener.GetGold() : 0);
						return;
					}
					this.LeftInventoryOwnerName = GameTexts.FindText("str_loot", null).ToString();
				}
			}
		}

		// Token: 0x06000D15 RID: 3349 RVA: 0x00039818 File Offset: 0x00037A18
		private void UpdateRightCharacter()
		{
			this.UpdateCharacterEquipment();
			this.UpdateCharacterArmorValues();
			this.RefreshCharacterTotalWeight();
			this.RefreshCharacterCanUseItem();
			this.CurrentCharacterName = this._currentCharacter.Name.ToString();
			this.RightInventoryOwnerGold = Hero.MainHero.Gold - this._inventoryLogic.TotalAmount;
		}

		// Token: 0x06000D16 RID: 3350 RVA: 0x00039870 File Offset: 0x00037A70
		private SPItemVM InitializeCharacterEquipmentSlot(ItemRosterElement itemRosterElement, EquipmentIndex equipmentIndex)
		{
			InventoryLogic.InventorySide equipmentToInventorySide = this.GetEquipmentToInventorySide(this._equipmentMode);
			SPItemVM spitemVM;
			if (!itemRosterElement.IsEmpty)
			{
				spitemVM = new SPItemVM(this._inventoryLogic, this.MainCharacter.IsFemale, this.CanCharacterUseItemBasedOnSkills(itemRosterElement), this._usageType, itemRosterElement, equipmentToInventorySide, this._inventoryLogic.GetCostOfItemRosterElement(itemRosterElement, equipmentToInventorySide), new EquipmentIndex?(equipmentIndex));
			}
			else
			{
				spitemVM = new SPItemVM();
				spitemVM.RefreshWith(null, equipmentToInventorySide);
			}
			return spitemVM;
		}

		// Token: 0x06000D17 RID: 3351 RVA: 0x000398E0 File Offset: 0x00037AE0
		private void UpdateCharacterEquipment()
		{
			this.CharacterHelmSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.NumAllWeaponSlots), 1), EquipmentIndex.NumAllWeaponSlots);
			this.CharacterCloakSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Cape), 1), EquipmentIndex.Cape);
			this.CharacterTorsoSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Body), 1), EquipmentIndex.Body);
			this.CharacterGloveSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Gloves), 1), EquipmentIndex.Gloves);
			this.CharacterBootSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Leg), 1), EquipmentIndex.Leg);
			this.CharacterMountSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.ArmorItemEndSlot), 1), EquipmentIndex.ArmorItemEndSlot);
			this.CharacterMountArmorSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.HorseHarness), 1), EquipmentIndex.HorseHarness);
			this.CharacterWeapon1Slot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.WeaponItemBeginSlot), 1), EquipmentIndex.WeaponItemBeginSlot);
			this.CharacterWeapon2Slot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1), 1), EquipmentIndex.Weapon1);
			this.CharacterWeapon3Slot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2), 1), EquipmentIndex.Weapon2);
			this.CharacterWeapon4Slot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3), 1), EquipmentIndex.Weapon3);
			this.CharacterBannerSlot = this.InitializeCharacterEquipmentSlot(new ItemRosterElement(this.ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.ExtraWeaponSlot), 1), EquipmentIndex.ExtraWeaponSlot);
			this.MainCharacter.SetEquipment(this.ActiveEquipment);
		}

		// Token: 0x06000D18 RID: 3352 RVA: 0x00039A78 File Offset: 0x00037C78
		private void UpdateCharacterArmorValues()
		{
			Equipment.EquipmentType equipmentType = this.ChangeIntoEquipmentType(this.GetEquipmentToInventorySide(this._equipmentMode));
			this.CurrentCharacterArmArmor = this._currentCharacter.GetArmArmorSum(equipmentType);
			this.CurrentCharacterBodyArmor = this._currentCharacter.GetBodyArmorSum(equipmentType);
			this.CurrentCharacterHeadArmor = this._currentCharacter.GetHeadArmorSum(equipmentType);
			this.CurrentCharacterLegArmor = this._currentCharacter.GetLegArmorSum(equipmentType);
			this.CurrentCharacterHorseArmor = this._currentCharacter.GetHorseArmorSum(equipmentType);
		}

		// Token: 0x06000D19 RID: 3353 RVA: 0x00039AF2 File Offset: 0x00037CF2
		private Equipment.EquipmentType ChangeIntoEquipmentType(InventoryLogic.InventorySide equipmentMode)
		{
			switch (equipmentMode)
			{
			case InventoryLogic.InventorySide.CivilianEquipment:
				return Equipment.EquipmentType.Civilian;
			case InventoryLogic.InventorySide.BattleEquipment:
				return Equipment.EquipmentType.Battle;
			case InventoryLogic.InventorySide.StealthEquipment:
				return Equipment.EquipmentType.Stealth;
			default:
				Debug.FailedAssert("Cannot change InventoryLogic EquipmentMode to EquiptmentType", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ChangeIntoEquipmentType", 1891);
				return Equipment.EquipmentType.Invalid;
			}
		}

		// Token: 0x06000D1A RID: 3354 RVA: 0x00039B2C File Offset: 0x00037D2C
		private void RefreshCharacterTotalWeight()
		{
			CharacterObject currentCharacter = this._currentCharacter;
			float num = ((currentCharacter != null && currentCharacter.GetPerkValue(DefaultPerks.Athletics.FormFittingArmor)) ? (1f + DefaultPerks.Athletics.FormFittingArmor.PrimaryBonus) : 1f);
			this.CurrentCharacterTotalEncumbrance = MathF.Round(this.ActiveEquipment.GetTotalWeightOfWeapons() + this.ActiveEquipment.GetTotalWeightOfArmor(true) * num, 1).ToString("0.0");
		}

		// Token: 0x06000D1B RID: 3355 RVA: 0x00039BA0 File Offset: 0x00037DA0
		private void RefreshCharacterCanUseItem()
		{
			for (int i = 0; i < this.RightItemListVM.Count; i++)
			{
				this.RightItemListVM[i].CanCharacterUseItem = this.CanCharacterUseItemBasedOnSkills(this.RightItemListVM[i].ItemRosterElement);
			}
			for (int j = 0; j < this.LeftItemListVM.Count; j++)
			{
				this.LeftItemListVM[j].CanCharacterUseItem = this.CanCharacterUseItemBasedOnSkills(this.LeftItemListVM[j].ItemRosterElement);
			}
		}

		// Token: 0x06000D1C RID: 3356 RVA: 0x00039C2C File Offset: 0x00037E2C
		private void InitializeInventory()
		{
			this.IsRefreshed = false;
			switch (this._inventoryLogic.MerchantItemType)
			{
			case InventoryScreenHelper.InventoryCategoryType.Armors:
				this.ActiveFilterIndex = 3;
				break;
			case InventoryScreenHelper.InventoryCategoryType.Weapon:
				this.ActiveFilterIndex = 1;
				break;
			case InventoryScreenHelper.InventoryCategoryType.Shield:
				this.ActiveFilterIndex = 2;
				break;
			case InventoryScreenHelper.InventoryCategoryType.HorseCategory:
				this.ActiveFilterIndex = 4;
				break;
			case InventoryScreenHelper.InventoryCategoryType.Goods:
				this.ActiveFilterIndex = 5;
				break;
			default:
				this.ActiveFilterIndex = 0;
				break;
			}
			this.RightItemListVM.Clear();
			this.LeftItemListVM.Clear();
			int num = MathF.Max(this._inventoryLogic.GetElementCountOnSide(InventoryLogic.InventorySide.PlayerInventory), this._inventoryLogic.GetElementCountOnSide(InventoryLogic.InventorySide.OtherInventory));
			ItemRosterElement[] array = (from i in this._inventoryLogic.GetElementsInRoster(InventoryLogic.InventorySide.PlayerInventory)
				orderby i.EquipmentElement.GetModifiedItemName().ToString()
				select i).ToArray<ItemRosterElement>();
			ItemRosterElement[] array2 = (from i in this._inventoryLogic.GetElementsInRoster(InventoryLogic.InventorySide.OtherInventory)
				orderby i.EquipmentElement.GetModifiedItemName().ToString()
				select i).ToArray<ItemRosterElement>();
			this._lockedItemIDs = this._viewDataTracker.GetInventoryLocks().ToList<string>();
			for (int j = 0; j < num; j++)
			{
				if (j < array.Length)
				{
					ItemRosterElement itemRosterElement = array[j];
					SPItemVM spitemVM = new SPItemVM(this._inventoryLogic, this.MainCharacter.IsFemale, this.CanCharacterUseItemBasedOnSkills(itemRosterElement), this._usageType, itemRosterElement, InventoryLogic.InventorySide.PlayerInventory, this._inventoryLogic.GetCostOfItemRosterElement(itemRosterElement, InventoryLogic.InventorySide.PlayerInventory), null);
					this.UpdateFilteredStatusOfItem(spitemVM);
					spitemVM.IsLocked = spitemVM.InventorySide == InventoryLogic.InventorySide.PlayerInventory && this.IsItemLocked(itemRosterElement);
					this.RightItemListVM.Add(spitemVM);
				}
				if (j < array2.Length)
				{
					ItemRosterElement itemRosterElement2 = array2[j];
					SPItemVM spitemVM2 = new SPItemVM(this._inventoryLogic, this.MainCharacter.IsFemale, this.CanCharacterUseItemBasedOnSkills(itemRosterElement2), this._usageType, itemRosterElement2, InventoryLogic.InventorySide.OtherInventory, this._inventoryLogic.GetCostOfItemRosterElement(itemRosterElement2, InventoryLogic.InventorySide.OtherInventory), null);
					this.UpdateFilteredStatusOfItem(spitemVM2);
					spitemVM2.IsLocked = spitemVM2.InventorySide == InventoryLogic.InventorySide.PlayerInventory && this.IsItemLocked(itemRosterElement2);
					this.LeftItemListVM.Add(spitemVM2);
				}
			}
			this.RefreshInformationValues();
			this.IsRefreshed = true;
		}

		// Token: 0x06000D1D RID: 3357 RVA: 0x00039E80 File Offset: 0x00038080
		private bool IsItemLocked(ItemRosterElement item)
		{
			string text = item.EquipmentElement.Item.StringId;
			if (item.EquipmentElement.ItemModifier != null)
			{
				text += item.EquipmentElement.ItemModifier.StringId;
			}
			return this._lockedItemIDs.Contains(text);
		}

		// Token: 0x06000D1E RID: 3358 RVA: 0x00039EDA File Offset: 0x000380DA
		public void CompareNextItem()
		{
			this.CycleBetweenWeaponSlots();
			this.RefreshComparedItem();
		}

		// Token: 0x06000D1F RID: 3359 RVA: 0x00039EE8 File Offset: 0x000380E8
		public void ExecuteSelectItem(ItemVM item)
		{
			if (item != null)
			{
				SPItemVM spitemVM = item as SPItemVM;
				if (spitemVM == null || !spitemVM.IsSelected)
				{
					goto IL_49;
				}
			}
			using (IEnumerator<SPItemVM> enumerator = this.GetAllItems(true).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					SPItemVM spitemVM2 = enumerator.Current;
					spitemVM2.IsSelected = false;
				}
				return;
			}
			IL_49:
			if (this.GetEquippedItems().Contains(item))
			{
				foreach (SPItemVM spitemVM3 in this.GetAllItems(false))
				{
					spitemVM3.IsSelected = false;
				}
				using (IEnumerator<SPItemVM> enumerator = this.GetEquippedItems().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						SPItemVM spitemVM4 = enumerator.Current;
						spitemVM4.IsSelected = spitemVM4 == item;
					}
					return;
				}
			}
			foreach (SPItemVM spitemVM5 in this.GetEquippedItems())
			{
				spitemVM5.IsSelected = false;
			}
			foreach (SPItemVM spitemVM6 in this.GetAllItems(false))
			{
				spitemVM6.IsSelected = spitemVM6.ItemRosterElement.EquipmentElement.IsEqualTo(item.ItemRosterElement.EquipmentElement);
				if (spitemVM6.IsSelected)
				{
					this.ScrollItemId = spitemVM6.ItemRosterElement.EquipmentElement.Item.StringId;
					this.ScrollToItem = true;
				}
			}
		}

		// Token: 0x06000D20 RID: 3360 RVA: 0x0003A098 File Offset: 0x00038298
		public void ExecuteClearSelectedItem()
		{
			this.ExecuteSelectItem(null);
		}

		// Token: 0x06000D21 RID: 3361 RVA: 0x0003A0A1 File Offset: 0x000382A1
		private IEnumerable<SPItemVM> GetAllItems(bool includeEquipped)
		{
			foreach (SPItemVM spitemVM in this.LeftItemListVM)
			{
				yield return spitemVM;
			}
			IEnumerator<SPItemVM> enumerator = null;
			foreach (SPItemVM spitemVM2 in this.RightItemListVM)
			{
				yield return spitemVM2;
			}
			enumerator = null;
			if (includeEquipped)
			{
				foreach (SPItemVM spitemVM3 in this.GetEquippedItems())
				{
					yield return spitemVM3;
				}
				enumerator = null;
			}
			yield break;
			yield break;
		}

		// Token: 0x06000D22 RID: 3362 RVA: 0x0003A0B8 File Offset: 0x000382B8
		private IEnumerable<SPItemVM> GetEquippedItems()
		{
			yield return this.CharacterHelmSlot;
			yield return this.CharacterCloakSlot;
			yield return this.CharacterTorsoSlot;
			yield return this.CharacterGloveSlot;
			yield return this.CharacterBootSlot;
			yield return this.CharacterMountSlot;
			yield return this.CharacterMountArmorSlot;
			yield return this.CharacterWeapon1Slot;
			yield return this.CharacterWeapon2Slot;
			yield return this.CharacterWeapon3Slot;
			yield return this.CharacterWeapon4Slot;
			yield return this.CharacterBannerSlot;
			yield break;
		}

		// Token: 0x06000D23 RID: 3363 RVA: 0x0003A0C8 File Offset: 0x000382C8
		public bool IsAnyEquippedItemSelected()
		{
			using (IEnumerator<SPItemVM> enumerator = this.GetEquippedItems().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IsSelected)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000D24 RID: 3364 RVA: 0x0003A11C File Offset: 0x0003831C
		private void BuyItem(SPItemVM item)
		{
			if (this.TargetEquipmentType != EquipmentIndex.None && item.ItemType != this.TargetEquipmentType && (this.TargetEquipmentType < EquipmentIndex.WeaponItemBeginSlot || this.TargetEquipmentType > EquipmentIndex.ExtraWeaponSlot || item.ItemType < EquipmentIndex.WeaponItemBeginSlot || item.ItemType > EquipmentIndex.ExtraWeaponSlot))
			{
				return;
			}
			if (this.TargetEquipmentType == EquipmentIndex.None)
			{
				this.TargetEquipmentType = item.ItemType;
				if (item.ItemType >= EquipmentIndex.WeaponItemBeginSlot && item.ItemType <= EquipmentIndex.ExtraWeaponSlot)
				{
					this.TargetEquipmentType = this.ActiveEquipment.GetWeaponPickUpSlotIndex(item.ItemRosterElement.EquipmentElement, false);
				}
			}
			int b = item.ItemCount;
			if (item.InventorySide == InventoryLogic.InventorySide.PlayerInventory)
			{
				ItemRosterElement? itemRosterElement = this._inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.OtherInventory, item.ItemRosterElement.EquipmentElement);
				if (itemRosterElement != null)
				{
					b = itemRosterElement.Value.Amount;
				}
			}
			TransferCommand command = TransferCommand.Transfer(MathF.Min(this.TransactionCount, b), InventoryLogic.InventorySide.OtherInventory, InventoryLogic.InventorySide.PlayerInventory, item.ItemRosterElement, item.ItemType, this.TargetEquipmentType, this._currentCharacter);
			this._inventoryLogic.AddTransferCommand(command);
			if (this.EquipAfterBuy)
			{
				this._equipAfterTransferStack.Push(item);
			}
		}

		// Token: 0x06000D25 RID: 3365 RVA: 0x0003A238 File Offset: 0x00038438
		private void SellItem(SPItemVM item)
		{
			InventoryLogic.InventorySide inventorySide = item.InventorySide;
			int b = item.ItemCount;
			if (inventorySide == InventoryLogic.InventorySide.OtherInventory)
			{
				inventorySide = InventoryLogic.InventorySide.PlayerInventory;
				ItemRosterElement? itemRosterElement = this._inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.PlayerInventory, item.ItemRosterElement.EquipmentElement);
				if (itemRosterElement != null)
				{
					b = itemRosterElement.Value.Amount;
				}
			}
			TransferCommand command = TransferCommand.Transfer(MathF.Min(this.TransactionCount, b), inventorySide, InventoryLogic.InventorySide.OtherInventory, item.ItemRosterElement, item.ItemType, this.TargetEquipmentType, this._currentCharacter);
			this._inventoryLogic.AddTransferCommand(command);
		}

		// Token: 0x06000D26 RID: 3366 RVA: 0x0003A2C4 File Offset: 0x000384C4
		private void SlaughterItem(SPItemVM item)
		{
			int num = 1;
			if (this.IsFiveStackModifierActive)
			{
				num = MathF.Min(5, item.ItemCount);
			}
			else if (this.IsEntireStackModifierActive)
			{
				num = item.ItemCount;
			}
			for (int i = 0; i < num; i++)
			{
				this._inventoryLogic.SlaughterItem(item.ItemRosterElement);
			}
		}

		// Token: 0x06000D27 RID: 3367 RVA: 0x0003A318 File Offset: 0x00038518
		private void DonateItem(SPItemVM item)
		{
			if (this.IsFiveStackModifierActive)
			{
				int itemCount = item.ItemCount;
				for (int i = 0; i < MathF.Min(5, itemCount); i++)
				{
					this._inventoryLogic.DonateItem(item.ItemRosterElement);
				}
				return;
			}
			this._inventoryLogic.DonateItem(item.ItemRosterElement);
		}

		// Token: 0x06000D28 RID: 3368 RVA: 0x0003A36C File Offset: 0x0003856C
		private float GetCapacityBudget(MobileParty party, bool isBuy)
		{
			if (isBuy)
			{
				if (party != null)
				{
					return (float)party.InventoryCapacity - party.TotalWeightCarried;
				}
				return 0f;
			}
			else
			{
				if (this._inventoryLogic.OtherSideCapacityData != null)
				{
					return (float)(this._inventoryLogic.OtherSideCapacityData.GetCapacity() - this._inventoryLogic.OtherSideCurrentWeight);
				}
				return -1f;
			}
		}

		// Token: 0x06000D29 RID: 3369 RVA: 0x0003A3C4 File Offset: 0x000385C4
		private void TransferAll(bool isBuy)
		{
			this.IsRefreshed = false;
			List<TransferCommand> list = new List<TransferCommand>(this.LeftItemListVM.Count);
			MBBindingList<SPItemVM> mbbindingList = new MBBindingList<SPItemVM>();
			foreach (SPItemVM spitemVM in (isBuy ? this.LeftItemListVM : this.RightItemListVM))
			{
				if (spitemVM != null && !spitemVM.IsFiltered && spitemVM != null && !spitemVM.IsLocked && spitemVM != null && spitemVM.IsTransferable)
				{
					mbbindingList.Add(spitemVM);
				}
			}
			MobileParty mobileParty;
			if (!isBuy)
			{
				PartyBase otherParty = this._inventoryLogic.OtherParty;
				mobileParty = ((otherParty != null) ? otherParty.MobileParty : null);
			}
			else
			{
				mobileParty = MobileParty.MainParty;
			}
			MobileParty mobileParty2 = mobileParty;
			PartyBase otherParty2 = this._inventoryLogic.OtherParty;
			bool flag = otherParty2 != null && otherParty2.IsSettlement;
			InventoryCapacityModel inventoryCapacityModel = Campaign.Current.Models.InventoryCapacityModel;
			mbbindingList.Sort(new SPInventoryVM.RosterElementComparer(inventoryCapacityModel, mobileParty2, flag));
			InventoryLogic.InventorySide fromSide = (isBuy ? InventoryLogic.InventorySide.OtherInventory : InventoryLogic.InventorySide.PlayerInventory);
			InventoryLogic.InventorySide inventorySide = (isBuy ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory);
			if (flag && !isBuy)
			{
				this.TransferAllForSettlement(mbbindingList, fromSide, inventorySide, list);
			}
			else
			{
				InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
				bool flag2 = ((activeInventoryState != null) ? activeInventoryState.InventoryMode : InventoryScreenHelper.InventoryMode.Default) == InventoryScreenHelper.InventoryMode.Warehouse;
				float num = 0f;
				float num2 = 0f;
				if (mbbindingList.Count > 0)
				{
					if (mobileParty2 != null)
					{
						TextObject textObject;
						num2 = inventoryCapacityModel.GetItemEffectiveWeight(mbbindingList[0].ItemRosterElement.EquipmentElement, mobileParty2, mobileParty2.IsCurrentlyAtSea, out textObject);
					}
					else if (flag2)
					{
						num2 = mbbindingList[0].ItemRosterElement.EquipmentElement.GetEquipmentElementWeight();
					}
				}
				float capacityBudget = this.GetCapacityBudget(mobileParty2, isBuy);
				bool flag3 = capacityBudget < num2;
				bool flag4 = this._inventoryLogic.CanInventoryCapacityIncrease(inventorySide);
				if (!flag3 && flag4)
				{
					List<TransferCommand> list2 = new List<TransferCommand>(0);
					for (int i = 0; i < mbbindingList.Count; i++)
					{
						SPItemVM spitemVM2 = mbbindingList[i];
						if (!this._inventoryLogic.GetCanItemIncreaseInventoryCapacity(spitemVM2.ItemRosterElement.EquipmentElement.Item))
						{
							break;
						}
						TransferCommand item = TransferCommand.Transfer(spitemVM2.ItemRosterElement.Amount, fromSide, inventorySide, spitemVM2.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, this._currentCharacter);
						list2.Add(item);
						mbbindingList.Remove(spitemVM2);
						i--;
					}
					if (list2.Count > 0)
					{
						this._inventoryLogic.AddTransferCommands(list2);
						list2.Clear();
						capacityBudget = this.GetCapacityBudget(mobileParty2, isBuy);
					}
				}
				int num3 = mbbindingList.Count - 1;
				while (0 <= num3)
				{
					SPItemVM spitemVM3 = mbbindingList[num3];
					int num4 = spitemVM3.ItemRosterElement.Amount;
					if (!flag3)
					{
						TextObject textObject2;
						float num5 = (flag2 ? spitemVM3.ItemRosterElement.EquipmentElement.GetEquipmentElementWeight() : inventoryCapacityModel.GetItemEffectiveWeight(spitemVM3.ItemRosterElement.EquipmentElement, mobileParty2, mobileParty2.IsCurrentlyAtSea, out textObject2));
						float num6 = num + num5 * (float)num4;
						if (num4 > 0 && num6 > capacityBudget)
						{
							num4 = MBMath.ClampInt(num4, 0, MathF.Floor((capacityBudget - num) / num5));
						}
						num += (float)num4 * num5;
					}
					if (num4 > 0)
					{
						TransferCommand item2 = TransferCommand.Transfer(num4, fromSide, inventorySide, spitemVM3.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, this._currentCharacter);
						list.Add(item2);
					}
					num3--;
				}
			}
			this._inventoryLogic.AddTransferCommands(list);
			this.RefreshInformationValues();
			this.ExecuteRemoveZeroCounts();
			this.IsRefreshed = true;
		}

		// Token: 0x06000D2A RID: 3370 RVA: 0x0003A730 File Offset: 0x00038930
		private void TransferAllForSettlement(MBBindingList<SPItemVM> list, InventoryLogic.InventorySide fromSide, InventoryLogic.InventorySide toSide, List<TransferCommand> commands)
		{
			float num = (float)this.LeftInventoryOwnerGold;
			float num2 = float.MaxValue;
			float num3 = 0f;
			foreach (SPItemVM spitemVM in list)
			{
				int itemCost = spitemVM.ItemCost;
				if ((float)itemCost < num2)
				{
					num2 = (float)itemCost;
				}
			}
			bool flag = num < num2;
			int num4 = list.Count - 1;
			while (0 <= num4)
			{
				SPItemVM spitemVM2 = list[num4];
				int amount = spitemVM2.ItemRosterElement.Amount;
				if (!flag)
				{
					for (int i = 0; i < amount; i++)
					{
						float num5 = (float)spitemVM2.ItemCost;
						num3 += num5;
						if (num3 >= num)
						{
							num3 -= num5;
							break;
						}
						this._inventoryLogic.AddTransferCommands(new List<TransferCommand> { TransferCommand.Transfer(1, fromSide, toSide, spitemVM2.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, this._currentCharacter) });
					}
				}
				else
				{
					TransferCommand item = TransferCommand.Transfer(amount, fromSide, toSide, spitemVM2.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, this._currentCharacter);
					commands.Add(item);
				}
				num4--;
			}
		}

		// Token: 0x06000D2B RID: 3371 RVA: 0x0003A858 File Offset: 0x00038A58
		public void ExecuteSelectStealthOutfit()
		{
			this.EquipmentMode = 2;
		}

		// Token: 0x06000D2C RID: 3372 RVA: 0x0003A861 File Offset: 0x00038A61
		public void ExecuteSelectBattleOutfit()
		{
			this.EquipmentMode = 1;
		}

		// Token: 0x06000D2D RID: 3373 RVA: 0x0003A86A File Offset: 0x00038A6A
		public void ExecuteSelectCivilianOutfit()
		{
			this.EquipmentMode = 0;
		}

		// Token: 0x06000D2E RID: 3374 RVA: 0x0003A873 File Offset: 0x00038A73
		public void ExecuteBuyAllItems()
		{
			this.TransferAll(true);
		}

		// Token: 0x06000D2F RID: 3375 RVA: 0x0003A87C File Offset: 0x00038A7C
		public void ExecuteSellAllItems()
		{
			this.TransferAll(false);
		}

		// Token: 0x06000D30 RID: 3376 RVA: 0x0003A888 File Offset: 0x00038A88
		public void ExecuteBuyItemTest()
		{
			this.TransactionCount = 1;
			this.EquipAfterBuy = false;
			int totalGold = Hero.MainHero.Gold;
			foreach (SPItemVM spitemVM in this.LeftItemListVM.Where(delegate(SPItemVM i)
			{
				ItemObject item = i.ItemRosterElement.EquipmentElement.Item;
				return item != null && item.IsFood && i.ItemCost <= totalGold;
			}))
			{
				if (spitemVM.ItemCost <= totalGold)
				{
					this.ProcessBuyItem(spitemVM, false);
					totalGold -= spitemVM.ItemCost;
				}
			}
		}

		// Token: 0x06000D31 RID: 3377 RVA: 0x0003A92C File Offset: 0x00038B2C
		public void ExecuteResetTranstactions()
		{
			this._inventoryLogic.Reset(false);
			InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_inventory_reset_message", null).ToString()));
			this.CurrentFocusedItem = null;
		}

		// Token: 0x06000D32 RID: 3378 RVA: 0x0003A95C File Offset: 0x00038B5C
		public void ExecuteResetAndCompleteTranstactions()
		{
			this.ExecuteRemoveZeroCounts();
			InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
			if (((activeInventoryState != null) ? activeInventoryState.InventoryMode : InventoryScreenHelper.InventoryMode.Default) == InventoryScreenHelper.InventoryMode.Loot)
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_loot_behind", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					InventoryScreenHelper.CloseScreen(true);
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			InventoryScreenHelper.CloseScreen(true);
		}

		// Token: 0x06000D33 RID: 3379 RVA: 0x0003A9FC File Offset: 0x00038BFC
		public void ExecuteCompleteTranstactions()
		{
			this.ExecuteRemoveZeroCounts();
			InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
			if (((activeInventoryState != null) ? activeInventoryState.InventoryMode : InventoryScreenHelper.InventoryMode.Default) == InventoryScreenHelper.InventoryMode.Loot && !this._inventoryLogic.IsThereAnyChanges() && this._inventoryLogic.GetElementsInInitialRoster(InventoryLogic.InventorySide.OtherInventory).Any<ItemRosterElement>())
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_loot_behind", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.HandleDone), null, "", 0f, null, null, null), false, false);
				return;
			}
			this.HandleDone();
		}

		// Token: 0x06000D34 RID: 3380 RVA: 0x0003AAA8 File Offset: 0x00038CA8
		private void HandleDone()
		{
			MBInformationManager.HideInformations();
			bool flag = this._inventoryLogic.TotalAmount < 0;
			InventoryListener inventoryListener = this._inventoryLogic.InventoryListener;
			bool flag2 = ((inventoryListener != null) ? inventoryListener.GetGold() : 0) >= MathF.Abs(this._inventoryLogic.TotalAmount);
			int num = (int)this._inventoryLogic.XpGainFromDonations;
			int num2 = ((this._usageType == InventoryScreenHelper.InventoryMode.Default && num == 0 && !Game.Current.CheatMode) ? this._inventoryLogic.GetElementCountOnSide(InventoryLogic.InventorySide.OtherInventory) : 0);
			if (flag && !flag2)
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_trader_doesnt_have_enough_money", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					InventoryScreenHelper.CloseScreen(false);
				}, null, "", 0f, null, null, null), false, false);
			}
			else if (num2 > 0)
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_discarding_items", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					InventoryScreenHelper.CloseScreen(false);
				}, null, "", 0f, null, null, null), false, false);
			}
			else
			{
				InventoryScreenHelper.CloseScreen(false);
			}
			this.SaveItemLockStates();
			this.SaveItemSortStates();
		}

		// Token: 0x06000D35 RID: 3381 RVA: 0x0003AC2E File Offset: 0x00038E2E
		private void SaveItemLockStates()
		{
			this._viewDataTracker.SetInventoryLocks(this._lockedItemIDs);
		}

		// Token: 0x06000D36 RID: 3382 RVA: 0x0003AC44 File Offset: 0x00038E44
		private void SaveItemSortStates()
		{
			this._viewDataTracker.InventorySetSortPreference((int)this._usageType, (int)this.PlayerInventorySortController.CurrentSortOption.Value, (int)this.PlayerInventorySortController.CurrentSortState.Value);
		}

		// Token: 0x06000D37 RID: 3383 RVA: 0x0003AC88 File Offset: 0x00038E88
		public void ExecuteTransferWithParameters(SPItemVM item, int index, string targetTag)
		{
			bool isTransferable = item.IsTransferable;
			bool flag = targetTag == "PlayerInventory" || targetTag.StartsWith("Equipment");
			bool isPlayerCharacter = this._currentCharacter.IsPlayerCharacter;
			if (!isTransferable && (!flag || !isPlayerCharacter))
			{
				return;
			}
			if (targetTag == "OverCharacter")
			{
				this.TargetEquipmentIndex = -1;
				if (item.InventorySide == InventoryLogic.InventorySide.OtherInventory)
				{
					item.TransactionCount = 1;
					this.TransactionCount = 1;
					this.ProcessEquipItem(item);
					return;
				}
				if (item.InventorySide == InventoryLogic.InventorySide.PlayerInventory)
				{
					this.ProcessEquipItem(item);
					return;
				}
			}
			else if (targetTag == "PlayerInventory")
			{
				this.TargetEquipmentIndex = -1;
				if (item.InventorySide == this.GetEquipmentToInventorySide(this._equipmentMode))
				{
					this.ProcessUnequipItem(item);
					return;
				}
				if (item.InventorySide == InventoryLogic.InventorySide.OtherInventory)
				{
					item.TransactionCount = item.ItemCount;
					this.TransactionCount = item.ItemCount;
					this.ProcessBuyItem(item, false);
					return;
				}
			}
			else if (targetTag == "OtherInventory")
			{
				if (item.InventorySide != InventoryLogic.InventorySide.OtherInventory)
				{
					item.TransactionCount = item.ItemCount;
					this.TransactionCount = item.ItemCount;
					this.ProcessSellItem(item, false);
					return;
				}
			}
			else if (targetTag.StartsWith("Equipment"))
			{
				this.TargetEquipmentIndex = int.Parse(targetTag.Substring("Equipment".Length + 1));
				if (item.InventorySide == InventoryLogic.InventorySide.OtherInventory)
				{
					item.TransactionCount = 1;
					this.TransactionCount = 1;
					this.ProcessEquipItem(item);
					return;
				}
				if (item.InventorySide == InventoryLogic.InventorySide.PlayerInventory || item.InventorySide == this.GetEquipmentToInventorySide(this._equipmentMode))
				{
					this.ProcessEquipItem(item);
				}
			}
		}

		// Token: 0x06000D38 RID: 3384 RVA: 0x0003AE13 File Offset: 0x00039013
		private void UpdateIsDoneDisabled()
		{
			this.IsDoneDisabled = !this._inventoryLogic.CanPlayerCompleteTransaction();
		}

		// Token: 0x06000D39 RID: 3385 RVA: 0x0003AE2C File Offset: 0x0003902C
		private void ProcessFilter(SPInventoryVM.Filters filterIndex)
		{
			this.ActiveFilterIndex = (int)filterIndex;
			this.IsRefreshed = false;
			foreach (SPItemVM spitemVM in this.LeftItemListVM)
			{
				if (spitemVM != null)
				{
					this.UpdateFilteredStatusOfItem(spitemVM);
				}
			}
			foreach (SPItemVM spitemVM2 in this.RightItemListVM)
			{
				if (spitemVM2 != null)
				{
					this.UpdateFilteredStatusOfItem(spitemVM2);
				}
			}
			this.IsRefreshed = true;
		}

		// Token: 0x06000D3A RID: 3386 RVA: 0x0003AED0 File Offset: 0x000390D0
		private void UpdateFilteredStatusOfItem(SPItemVM item)
		{
			bool flag = !this._filters[this._activeFilterIndex].Contains(item.TypeId);
			bool flag2 = false;
			if (this.IsSearchAvailable && (item.InventorySide == InventoryLogic.InventorySide.OtherInventory || item.InventorySide == InventoryLogic.InventorySide.PlayerInventory))
			{
				string text = ((item.InventorySide == InventoryLogic.InventorySide.OtherInventory) ? this.LeftSearchText : this.RightSearchText);
				if (text.Length > 1)
				{
					flag2 = !item.ItemDescription.ToLower().Contains(text);
				}
			}
			item.IsFiltered = flag || flag2;
		}

		// Token: 0x06000D3B RID: 3387 RVA: 0x0003AF5B File Offset: 0x0003915B
		private void OnSearchTextChanged(bool isLeft)
		{
			if (this.IsSearchAvailable)
			{
				(isLeft ? this.LeftItemListVM : this.RightItemListVM).ApplyActionOnAllItems(delegate(SPItemVM x)
				{
					this.UpdateFilteredStatusOfItem(x);
				});
			}
		}

		// Token: 0x06000D3C RID: 3388 RVA: 0x0003AF87 File Offset: 0x00039187
		public void ExecuteFilterNone()
		{
			this.ProcessFilter(SPInventoryVM.Filters.All);
			Game.Current.EventManager.TriggerEvent<InventoryFilterChangedEvent>(new InventoryFilterChangedEvent(SPInventoryVM.Filters.All));
		}

		// Token: 0x06000D3D RID: 3389 RVA: 0x0003AFA5 File Offset: 0x000391A5
		public void ExecuteFilterWeapons()
		{
			this.ProcessFilter(SPInventoryVM.Filters.Weapons);
			Game.Current.EventManager.TriggerEvent<InventoryFilterChangedEvent>(new InventoryFilterChangedEvent(SPInventoryVM.Filters.Weapons));
		}

		// Token: 0x06000D3E RID: 3390 RVA: 0x0003AFC3 File Offset: 0x000391C3
		public void ExecuteFilterArmors()
		{
			this.ProcessFilter(SPInventoryVM.Filters.Armors);
			Game.Current.EventManager.TriggerEvent<InventoryFilterChangedEvent>(new InventoryFilterChangedEvent(SPInventoryVM.Filters.Armors));
		}

		// Token: 0x06000D3F RID: 3391 RVA: 0x0003AFE1 File Offset: 0x000391E1
		public void ExecuteFilterShieldsAndRanged()
		{
			this.ProcessFilter(SPInventoryVM.Filters.ShieldsAndRanged);
			Game.Current.EventManager.TriggerEvent<InventoryFilterChangedEvent>(new InventoryFilterChangedEvent(SPInventoryVM.Filters.ShieldsAndRanged));
		}

		// Token: 0x06000D40 RID: 3392 RVA: 0x0003AFFF File Offset: 0x000391FF
		public void ExecuteFilterMounts()
		{
			this.ProcessFilter(SPInventoryVM.Filters.Mounts);
			Game.Current.EventManager.TriggerEvent<InventoryFilterChangedEvent>(new InventoryFilterChangedEvent(SPInventoryVM.Filters.Mounts));
		}

		// Token: 0x06000D41 RID: 3393 RVA: 0x0003B01D File Offset: 0x0003921D
		public void ExecuteFilterMisc()
		{
			this.ProcessFilter(SPInventoryVM.Filters.Miscellaneous);
			Game.Current.EventManager.TriggerEvent<InventoryFilterChangedEvent>(new InventoryFilterChangedEvent(SPInventoryVM.Filters.Miscellaneous));
		}

		// Token: 0x06000D42 RID: 3394 RVA: 0x0003B03C File Offset: 0x0003923C
		public void CycleBetweenWeaponSlots()
		{
			EquipmentIndex selectedEquipmentIndex = (EquipmentIndex)this._selectedEquipmentIndex;
			if (selectedEquipmentIndex >= EquipmentIndex.WeaponItemBeginSlot && selectedEquipmentIndex < EquipmentIndex.NumAllWeaponSlots)
			{
				int selectedEquipmentIndex2 = this._selectedEquipmentIndex;
				do
				{
					if (this._selectedEquipmentIndex < 3)
					{
						this._selectedEquipmentIndex++;
					}
					else
					{
						this._selectedEquipmentIndex = 0;
					}
				}
				while (this._selectedEquipmentIndex != selectedEquipmentIndex2 && this.GetItemFromIndex((EquipmentIndex)this._selectedEquipmentIndex).ItemRosterElement.EquipmentElement.Item == null);
			}
		}

		// Token: 0x06000D43 RID: 3395 RVA: 0x0003B0A8 File Offset: 0x000392A8
		private SPItemVM GetItemFromIndex(EquipmentIndex itemType)
		{
			switch (itemType)
			{
			case EquipmentIndex.WeaponItemBeginSlot:
				return this.CharacterWeapon1Slot;
			case EquipmentIndex.Weapon1:
				return this.CharacterWeapon2Slot;
			case EquipmentIndex.Weapon2:
				return this.CharacterWeapon3Slot;
			case EquipmentIndex.Weapon3:
				return this.CharacterWeapon4Slot;
			case EquipmentIndex.ExtraWeaponSlot:
				return this.CharacterBannerSlot;
			case EquipmentIndex.NumAllWeaponSlots:
				return this.CharacterHelmSlot;
			case EquipmentIndex.Body:
				return this.CharacterTorsoSlot;
			case EquipmentIndex.Leg:
				return this.CharacterBootSlot;
			case EquipmentIndex.Gloves:
				return this.CharacterGloveSlot;
			case EquipmentIndex.Cape:
				return this.CharacterCloakSlot;
			case EquipmentIndex.ArmorItemEndSlot:
				return this.CharacterMountSlot;
			case EquipmentIndex.HorseHarness:
				return this.CharacterMountArmorSlot;
			default:
				return null;
			}
		}

		// Token: 0x06000D44 RID: 3396 RVA: 0x0003B144 File Offset: 0x00039344
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null)
				{
					if (obj.NewNotificationElementID != "TransferButtonOnlyFood" && this._isFoodTransferButtonHighlightApplied)
					{
						this.SetFoodTransferButtonHighlightState(false);
						this._isFoodTransferButtonHighlightApplied = false;
					}
					if (obj.NewNotificationElementID != "InventoryMicsFilter" && this.IsMicsFilterHighlightEnabled)
					{
						this.IsMicsFilterHighlightEnabled = false;
					}
					if (obj.NewNotificationElementID != "EquipmentSetFilters" && this.IsEquipmentSetFiltersHighlighted)
					{
						this.IsEquipmentSetFiltersHighlighted = false;
					}
					if (obj.NewNotificationElementID != "InventoryOtherBannerItems" && this.IsBannerItemsHighlightApplied)
					{
						this.SetBannerItemsHighlightState(false);
						this.IsEquipmentSetFiltersHighlighted = false;
					}
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (!string.IsNullOrEmpty(this._latestTutorialElementID))
				{
					if (!this._isFoodTransferButtonHighlightApplied && this._latestTutorialElementID == "TransferButtonOnlyFood")
					{
						this.SetFoodTransferButtonHighlightState(true);
						this._isFoodTransferButtonHighlightApplied = true;
					}
					if (!this.IsMicsFilterHighlightEnabled && this._latestTutorialElementID == "InventoryMicsFilter")
					{
						this.IsMicsFilterHighlightEnabled = true;
					}
					if (!this.IsEquipmentSetFiltersHighlighted && this._latestTutorialElementID == "EquipmentSetFilters")
					{
						this.IsEquipmentSetFiltersHighlighted = true;
					}
					if (!this.IsBannerItemsHighlightApplied && this._latestTutorialElementID == "InventoryOtherBannerItems")
					{
						this.IsBannerItemsHighlightApplied = true;
						this.ExecuteFilterMisc();
						this.SetBannerItemsHighlightState(true);
						return;
					}
				}
				else
				{
					if (this._isFoodTransferButtonHighlightApplied)
					{
						this.SetFoodTransferButtonHighlightState(false);
						this._isFoodTransferButtonHighlightApplied = false;
					}
					if (this.IsMicsFilterHighlightEnabled)
					{
						this.IsMicsFilterHighlightEnabled = false;
					}
					if (this.IsEquipmentSetFiltersHighlighted)
					{
						this.IsEquipmentSetFiltersHighlighted = false;
					}
					if (this.IsBannerItemsHighlightApplied)
					{
						this.SetBannerItemsHighlightState(false);
						this.IsBannerItemsHighlightApplied = false;
					}
				}
			}
		}

		// Token: 0x06000D45 RID: 3397 RVA: 0x0003B304 File Offset: 0x00039504
		private void SetFoodTransferButtonHighlightState(bool state)
		{
			for (int i = 0; i < this.LeftItemListVM.Count; i++)
			{
				SPItemVM spitemVM = this.LeftItemListVM[i];
				if (spitemVM.ItemRosterElement.EquipmentElement.Item.IsFood)
				{
					spitemVM.IsTransferButtonHighlighted = state;
				}
			}
		}

		// Token: 0x06000D46 RID: 3398 RVA: 0x0003B358 File Offset: 0x00039558
		private void SetBannerItemsHighlightState(bool state)
		{
			for (int i = 0; i < this.LeftItemListVM.Count; i++)
			{
				SPItemVM spitemVM = this.LeftItemListVM[i];
				if (spitemVM.ItemRosterElement.EquipmentElement.Item.IsBannerItem)
				{
					spitemVM.IsItemHighlightEnabled = state;
				}
			}
		}

		// Token: 0x1700041C RID: 1052
		// (get) Token: 0x06000D47 RID: 3399 RVA: 0x0003B3A9 File Offset: 0x000395A9
		// (set) Token: 0x06000D48 RID: 3400 RVA: 0x0003B3B1 File Offset: 0x000395B1
		[DataSourceProperty]
		public HintViewModel ResetHint
		{
			get
			{
				return this._resetHint;
			}
			set
			{
				if (value != this._resetHint)
				{
					this._resetHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ResetHint");
				}
			}
		}

		// Token: 0x1700041D RID: 1053
		// (get) Token: 0x06000D49 RID: 3401 RVA: 0x0003B3CF File Offset: 0x000395CF
		// (set) Token: 0x06000D4A RID: 3402 RVA: 0x0003B3D7 File Offset: 0x000395D7
		[DataSourceProperty]
		public string LeftInventoryLabel
		{
			get
			{
				return this._leftInventoryLabel;
			}
			set
			{
				if (value != this._leftInventoryLabel)
				{
					this._leftInventoryLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "LeftInventoryLabel");
				}
			}
		}

		// Token: 0x1700041E RID: 1054
		// (get) Token: 0x06000D4B RID: 3403 RVA: 0x0003B3FA File Offset: 0x000395FA
		// (set) Token: 0x06000D4C RID: 3404 RVA: 0x0003B402 File Offset: 0x00039602
		[DataSourceProperty]
		public string RightInventoryLabel
		{
			get
			{
				return this._rightInventoryLabel;
			}
			set
			{
				if (value != this._rightInventoryLabel)
				{
					this._rightInventoryLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "RightInventoryLabel");
				}
			}
		}

		// Token: 0x1700041F RID: 1055
		// (get) Token: 0x06000D4D RID: 3405 RVA: 0x0003B425 File Offset: 0x00039625
		// (set) Token: 0x06000D4E RID: 3406 RVA: 0x0003B42D File Offset: 0x0003962D
		[DataSourceProperty]
		public string DoneLbl
		{
			get
			{
				return this._doneLbl;
			}
			set
			{
				if (value != this._doneLbl)
				{
					this._doneLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneLbl");
				}
			}
		}

		// Token: 0x17000420 RID: 1056
		// (get) Token: 0x06000D4F RID: 3407 RVA: 0x0003B450 File Offset: 0x00039650
		// (set) Token: 0x06000D50 RID: 3408 RVA: 0x0003B458 File Offset: 0x00039658
		[DataSourceProperty]
		public bool IsDoneDisabled
		{
			get
			{
				return this._isDoneDisabled;
			}
			set
			{
				if (value != this._isDoneDisabled)
				{
					this._isDoneDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsDoneDisabled");
				}
			}
		}

		// Token: 0x17000421 RID: 1057
		// (get) Token: 0x06000D51 RID: 3409 RVA: 0x0003B476 File Offset: 0x00039676
		// (set) Token: 0x06000D52 RID: 3410 RVA: 0x0003B47E File Offset: 0x0003967E
		[DataSourceProperty]
		public bool OtherSideHasCapacity
		{
			get
			{
				return this._otherSideHasCapacity;
			}
			set
			{
				if (value != this._otherSideHasCapacity)
				{
					this._otherSideHasCapacity = value;
					base.OnPropertyChangedWithValue(value, "OtherSideHasCapacity");
				}
			}
		}

		// Token: 0x17000422 RID: 1058
		// (get) Token: 0x06000D53 RID: 3411 RVA: 0x0003B49C File Offset: 0x0003969C
		// (set) Token: 0x06000D54 RID: 3412 RVA: 0x0003B4A4 File Offset: 0x000396A4
		[DataSourceProperty]
		public bool IsSearchAvailable
		{
			get
			{
				return this._isSearchAvailable;
			}
			set
			{
				if (value != this._isSearchAvailable)
				{
					if (!value)
					{
						this.LeftSearchText = string.Empty;
						this.RightSearchText = string.Empty;
					}
					this._isSearchAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsSearchAvailable");
				}
			}
		}

		// Token: 0x17000423 RID: 1059
		// (get) Token: 0x06000D55 RID: 3413 RVA: 0x0003B4DB File Offset: 0x000396DB
		// (set) Token: 0x06000D56 RID: 3414 RVA: 0x0003B4E3 File Offset: 0x000396E3
		[DataSourceProperty]
		public bool IsOtherInventoryGoldRelevant
		{
			get
			{
				return this._isOtherInventoryGoldRelevant;
			}
			set
			{
				if (value != this._isOtherInventoryGoldRelevant)
				{
					this._isOtherInventoryGoldRelevant = value;
					base.OnPropertyChangedWithValue(value, "IsOtherInventoryGoldRelevant");
				}
			}
		}

		// Token: 0x17000424 RID: 1060
		// (get) Token: 0x06000D57 RID: 3415 RVA: 0x0003B501 File Offset: 0x00039701
		// (set) Token: 0x06000D58 RID: 3416 RVA: 0x0003B509 File Offset: 0x00039709
		[DataSourceProperty]
		public string CancelLbl
		{
			get
			{
				return this._cancelLbl;
			}
			set
			{
				if (value != this._cancelLbl)
				{
					this._cancelLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelLbl");
				}
			}
		}

		// Token: 0x17000425 RID: 1061
		// (get) Token: 0x06000D59 RID: 3417 RVA: 0x0003B52C File Offset: 0x0003972C
		// (set) Token: 0x06000D5A RID: 3418 RVA: 0x0003B534 File Offset: 0x00039734
		[DataSourceProperty]
		public string ResetLbl
		{
			get
			{
				return this._resetLbl;
			}
			set
			{
				if (value != this._resetLbl)
				{
					this._resetLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ResetLbl");
				}
			}
		}

		// Token: 0x17000426 RID: 1062
		// (get) Token: 0x06000D5B RID: 3419 RVA: 0x0003B557 File Offset: 0x00039757
		// (set) Token: 0x06000D5C RID: 3420 RVA: 0x0003B55F File Offset: 0x0003975F
		[DataSourceProperty]
		public string TypeText
		{
			get
			{
				return this._typeText;
			}
			set
			{
				if (value != this._typeText)
				{
					this._typeText = value;
					base.OnPropertyChangedWithValue<string>(value, "TypeText");
				}
			}
		}

		// Token: 0x17000427 RID: 1063
		// (get) Token: 0x06000D5D RID: 3421 RVA: 0x0003B582 File Offset: 0x00039782
		// (set) Token: 0x06000D5E RID: 3422 RVA: 0x0003B58A File Offset: 0x0003978A
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x17000428 RID: 1064
		// (get) Token: 0x06000D5F RID: 3423 RVA: 0x0003B5AD File Offset: 0x000397AD
		// (set) Token: 0x06000D60 RID: 3424 RVA: 0x0003B5B5 File Offset: 0x000397B5
		[DataSourceProperty]
		public string QuantityText
		{
			get
			{
				return this._quantityText;
			}
			set
			{
				if (value != this._quantityText)
				{
					this._quantityText = value;
					base.OnPropertyChangedWithValue<string>(value, "QuantityText");
				}
			}
		}

		// Token: 0x17000429 RID: 1065
		// (get) Token: 0x06000D61 RID: 3425 RVA: 0x0003B5D8 File Offset: 0x000397D8
		// (set) Token: 0x06000D62 RID: 3426 RVA: 0x0003B5E0 File Offset: 0x000397E0
		[DataSourceProperty]
		public string CostText
		{
			get
			{
				return this._costText;
			}
			set
			{
				if (value != this._costText)
				{
					this._costText = value;
					base.OnPropertyChangedWithValue<string>(value, "CostText");
				}
			}
		}

		// Token: 0x1700042A RID: 1066
		// (get) Token: 0x06000D63 RID: 3427 RVA: 0x0003B603 File Offset: 0x00039803
		// (set) Token: 0x06000D64 RID: 3428 RVA: 0x0003B60B File Offset: 0x0003980B
		[DataSourceProperty]
		public string SearchPlaceholderText
		{
			get
			{
				return this._searchPlaceholderText;
			}
			set
			{
				if (value != this._searchPlaceholderText)
				{
					this._searchPlaceholderText = value;
					base.OnPropertyChangedWithValue<string>(value, "SearchPlaceholderText");
				}
			}
		}

		// Token: 0x1700042B RID: 1067
		// (get) Token: 0x06000D65 RID: 3429 RVA: 0x0003B62E File Offset: 0x0003982E
		// (set) Token: 0x06000D66 RID: 3430 RVA: 0x0003B636 File Offset: 0x00039836
		[DataSourceProperty]
		public BasicTooltipViewModel ProductionTooltip
		{
			get
			{
				return this._productionTooltip;
			}
			set
			{
				if (value != this._productionTooltip)
				{
					this._productionTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ProductionTooltip");
				}
			}
		}

		// Token: 0x1700042C RID: 1068
		// (get) Token: 0x06000D67 RID: 3431 RVA: 0x0003B654 File Offset: 0x00039854
		// (set) Token: 0x06000D68 RID: 3432 RVA: 0x0003B65C File Offset: 0x0003985C
		[DataSourceProperty]
		public BasicTooltipViewModel InventoryCapacityHint
		{
			get
			{
				return this._inventoryCapacityHint;
			}
			set
			{
				if (value != this._inventoryCapacityHint)
				{
					this._inventoryCapacityHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "InventoryCapacityHint");
				}
			}
		}

		// Token: 0x1700042D RID: 1069
		// (get) Token: 0x06000D69 RID: 3433 RVA: 0x0003B67A File Offset: 0x0003987A
		// (set) Token: 0x06000D6A RID: 3434 RVA: 0x0003B682 File Offset: 0x00039882
		[DataSourceProperty]
		public BasicTooltipViewModel LandCapacityHint
		{
			get
			{
				return this._landCapacityHint;
			}
			set
			{
				if (value != this._landCapacityHint)
				{
					this._landCapacityHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "LandCapacityHint");
				}
			}
		}

		// Token: 0x1700042E RID: 1070
		// (get) Token: 0x06000D6B RID: 3435 RVA: 0x0003B6A0 File Offset: 0x000398A0
		// (set) Token: 0x06000D6C RID: 3436 RVA: 0x0003B6A8 File Offset: 0x000398A8
		[DataSourceProperty]
		public BasicTooltipViewModel SeaCapacityHint
		{
			get
			{
				return this._seaCapacityHint;
			}
			set
			{
				if (value != this._seaCapacityHint)
				{
					this._seaCapacityHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SeaCapacityHint");
				}
			}
		}

		// Token: 0x1700042F RID: 1071
		// (get) Token: 0x06000D6D RID: 3437 RVA: 0x0003B6C6 File Offset: 0x000398C6
		// (set) Token: 0x06000D6E RID: 3438 RVA: 0x0003B6CE File Offset: 0x000398CE
		[DataSourceProperty]
		public BasicTooltipViewModel TotalWeightCarriedHint
		{
			get
			{
				return this._totalWeightCarriedHint;
			}
			set
			{
				if (value != this._totalWeightCarriedHint)
				{
					this._totalWeightCarriedHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TotalWeightCarriedHint");
				}
			}
		}

		// Token: 0x17000430 RID: 1072
		// (get) Token: 0x06000D6F RID: 3439 RVA: 0x0003B6EC File Offset: 0x000398EC
		// (set) Token: 0x06000D70 RID: 3440 RVA: 0x0003B6F4 File Offset: 0x000398F4
		[DataSourceProperty]
		public BasicTooltipViewModel LandWeightHint
		{
			get
			{
				return this._landWeightHint;
			}
			set
			{
				if (value != this._landWeightHint)
				{
					this._landWeightHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "LandWeightHint");
				}
			}
		}

		// Token: 0x17000431 RID: 1073
		// (get) Token: 0x06000D71 RID: 3441 RVA: 0x0003B712 File Offset: 0x00039912
		// (set) Token: 0x06000D72 RID: 3442 RVA: 0x0003B71A File Offset: 0x0003991A
		[DataSourceProperty]
		public BasicTooltipViewModel SeaWeightHint
		{
			get
			{
				return this._seaWeightHint;
			}
			set
			{
				if (value != this._seaWeightHint)
				{
					this._seaWeightHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SeaWeightHint");
				}
			}
		}

		// Token: 0x17000432 RID: 1074
		// (get) Token: 0x06000D73 RID: 3443 RVA: 0x0003B738 File Offset: 0x00039938
		// (set) Token: 0x06000D74 RID: 3444 RVA: 0x0003B740 File Offset: 0x00039940
		[DataSourceProperty]
		public BasicTooltipViewModel CurrentCharacterSkillsTooltip
		{
			get
			{
				return this._currentCharacterSkillsTooltip;
			}
			set
			{
				if (value != this._currentCharacterSkillsTooltip)
				{
					this._currentCharacterSkillsTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CurrentCharacterSkillsTooltip");
				}
			}
		}

		// Token: 0x17000433 RID: 1075
		// (get) Token: 0x06000D75 RID: 3445 RVA: 0x0003B75E File Offset: 0x0003995E
		// (set) Token: 0x06000D76 RID: 3446 RVA: 0x0003B766 File Offset: 0x00039966
		[DataSourceProperty]
		public HintViewModel NoSaddleHint
		{
			get
			{
				return this._noSaddleHint;
			}
			set
			{
				if (value != this._noSaddleHint)
				{
					this._noSaddleHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "NoSaddleHint");
				}
			}
		}

		// Token: 0x17000434 RID: 1076
		// (get) Token: 0x06000D77 RID: 3447 RVA: 0x0003B784 File Offset: 0x00039984
		// (set) Token: 0x06000D78 RID: 3448 RVA: 0x0003B78C File Offset: 0x0003998C
		[DataSourceProperty]
		public HintViewModel DonationLblHint
		{
			get
			{
				return this._donationLblHint;
			}
			set
			{
				if (value != this._donationLblHint)
				{
					this._donationLblHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DonationLblHint");
				}
			}
		}

		// Token: 0x17000435 RID: 1077
		// (get) Token: 0x06000D79 RID: 3449 RVA: 0x0003B7AA File Offset: 0x000399AA
		// (set) Token: 0x06000D7A RID: 3450 RVA: 0x0003B7B2 File Offset: 0x000399B2
		[DataSourceProperty]
		public HintViewModel ArmArmorHint
		{
			get
			{
				return this._armArmorHint;
			}
			set
			{
				if (value != this._armArmorHint)
				{
					this._armArmorHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ArmArmorHint");
				}
			}
		}

		// Token: 0x17000436 RID: 1078
		// (get) Token: 0x06000D7B RID: 3451 RVA: 0x0003B7D0 File Offset: 0x000399D0
		// (set) Token: 0x06000D7C RID: 3452 RVA: 0x0003B7D8 File Offset: 0x000399D8
		[DataSourceProperty]
		public HintViewModel BodyArmorHint
		{
			get
			{
				return this._bodyArmorHint;
			}
			set
			{
				if (value != this._bodyArmorHint)
				{
					this._bodyArmorHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "BodyArmorHint");
				}
			}
		}

		// Token: 0x17000437 RID: 1079
		// (get) Token: 0x06000D7D RID: 3453 RVA: 0x0003B7F6 File Offset: 0x000399F6
		// (set) Token: 0x06000D7E RID: 3454 RVA: 0x0003B7FE File Offset: 0x000399FE
		[DataSourceProperty]
		public HintViewModel HeadArmorHint
		{
			get
			{
				return this._headArmorHint;
			}
			set
			{
				if (value != this._headArmorHint)
				{
					this._headArmorHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "HeadArmorHint");
				}
			}
		}

		// Token: 0x17000438 RID: 1080
		// (get) Token: 0x06000D7F RID: 3455 RVA: 0x0003B81C File Offset: 0x00039A1C
		// (set) Token: 0x06000D80 RID: 3456 RVA: 0x0003B824 File Offset: 0x00039A24
		[DataSourceProperty]
		public HintViewModel LegArmorHint
		{
			get
			{
				return this._legArmorHint;
			}
			set
			{
				if (value != this._legArmorHint)
				{
					this._legArmorHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "LegArmorHint");
				}
			}
		}

		// Token: 0x17000439 RID: 1081
		// (get) Token: 0x06000D81 RID: 3457 RVA: 0x0003B842 File Offset: 0x00039A42
		// (set) Token: 0x06000D82 RID: 3458 RVA: 0x0003B84A File Offset: 0x00039A4A
		[DataSourceProperty]
		public HintViewModel HorseArmorHint
		{
			get
			{
				return this._horseArmorHint;
			}
			set
			{
				if (value != this._horseArmorHint)
				{
					this._horseArmorHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "HorseArmorHint");
				}
			}
		}

		// Token: 0x1700043A RID: 1082
		// (get) Token: 0x06000D83 RID: 3459 RVA: 0x0003B868 File Offset: 0x00039A68
		// (set) Token: 0x06000D84 RID: 3460 RVA: 0x0003B870 File Offset: 0x00039A70
		[DataSourceProperty]
		public HintViewModel FilterAllHint
		{
			get
			{
				return this._filterAllHint;
			}
			set
			{
				if (value != this._filterAllHint)
				{
					this._filterAllHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FilterAllHint");
				}
			}
		}

		// Token: 0x1700043B RID: 1083
		// (get) Token: 0x06000D85 RID: 3461 RVA: 0x0003B88E File Offset: 0x00039A8E
		// (set) Token: 0x06000D86 RID: 3462 RVA: 0x0003B896 File Offset: 0x00039A96
		[DataSourceProperty]
		public HintViewModel FilterWeaponHint
		{
			get
			{
				return this._filterWeaponHint;
			}
			set
			{
				if (value != this._filterWeaponHint)
				{
					this._filterWeaponHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FilterWeaponHint");
				}
			}
		}

		// Token: 0x1700043C RID: 1084
		// (get) Token: 0x06000D87 RID: 3463 RVA: 0x0003B8B4 File Offset: 0x00039AB4
		// (set) Token: 0x06000D88 RID: 3464 RVA: 0x0003B8BC File Offset: 0x00039ABC
		[DataSourceProperty]
		public HintViewModel FilterArmorHint
		{
			get
			{
				return this._filterArmorHint;
			}
			set
			{
				if (value != this._filterArmorHint)
				{
					this._filterArmorHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FilterArmorHint");
				}
			}
		}

		// Token: 0x1700043D RID: 1085
		// (get) Token: 0x06000D89 RID: 3465 RVA: 0x0003B8DA File Offset: 0x00039ADA
		// (set) Token: 0x06000D8A RID: 3466 RVA: 0x0003B8E2 File Offset: 0x00039AE2
		[DataSourceProperty]
		public HintViewModel FilterShieldAndRangedHint
		{
			get
			{
				return this._filterShieldAndRangedHint;
			}
			set
			{
				if (value != this._filterShieldAndRangedHint)
				{
					this._filterShieldAndRangedHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FilterShieldAndRangedHint");
				}
			}
		}

		// Token: 0x1700043E RID: 1086
		// (get) Token: 0x06000D8B RID: 3467 RVA: 0x0003B900 File Offset: 0x00039B00
		// (set) Token: 0x06000D8C RID: 3468 RVA: 0x0003B908 File Offset: 0x00039B08
		[DataSourceProperty]
		public HintViewModel FilterMountAndHarnessHint
		{
			get
			{
				return this._filterMountAndHarnessHint;
			}
			set
			{
				if (value != this._filterMountAndHarnessHint)
				{
					this._filterMountAndHarnessHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FilterMountAndHarnessHint");
				}
			}
		}

		// Token: 0x1700043F RID: 1087
		// (get) Token: 0x06000D8D RID: 3469 RVA: 0x0003B926 File Offset: 0x00039B26
		// (set) Token: 0x06000D8E RID: 3470 RVA: 0x0003B92E File Offset: 0x00039B2E
		[DataSourceProperty]
		public HintViewModel FilterMiscHint
		{
			get
			{
				return this._filterMiscHint;
			}
			set
			{
				if (value != this._filterMiscHint)
				{
					this._filterMiscHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FilterMiscHint");
				}
			}
		}

		// Token: 0x17000440 RID: 1088
		// (get) Token: 0x06000D8F RID: 3471 RVA: 0x0003B94C File Offset: 0x00039B4C
		// (set) Token: 0x06000D90 RID: 3472 RVA: 0x0003B954 File Offset: 0x00039B54
		[DataSourceProperty]
		public HintViewModel StealthOutfitHint
		{
			get
			{
				return this._stealthOutfitHint;
			}
			set
			{
				if (value != this._stealthOutfitHint)
				{
					this._stealthOutfitHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "StealthOutfitHint");
				}
			}
		}

		// Token: 0x17000441 RID: 1089
		// (get) Token: 0x06000D91 RID: 3473 RVA: 0x0003B972 File Offset: 0x00039B72
		// (set) Token: 0x06000D92 RID: 3474 RVA: 0x0003B97A File Offset: 0x00039B7A
		[DataSourceProperty]
		public HintViewModel CivilianOutfitHint
		{
			get
			{
				return this._civilianOutfitHint;
			}
			set
			{
				if (value != this._civilianOutfitHint)
				{
					this._civilianOutfitHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CivilianOutfitHint");
				}
			}
		}

		// Token: 0x17000442 RID: 1090
		// (get) Token: 0x06000D93 RID: 3475 RVA: 0x0003B998 File Offset: 0x00039B98
		// (set) Token: 0x06000D94 RID: 3476 RVA: 0x0003B9A0 File Offset: 0x00039BA0
		[DataSourceProperty]
		public HintViewModel BattleOutfitHint
		{
			get
			{
				return this._battleOutfitHint;
			}
			set
			{
				if (value != this._battleOutfitHint)
				{
					this._battleOutfitHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "BattleOutfitHint");
				}
			}
		}

		// Token: 0x17000443 RID: 1091
		// (get) Token: 0x06000D95 RID: 3477 RVA: 0x0003B9BE File Offset: 0x00039BBE
		// (set) Token: 0x06000D96 RID: 3478 RVA: 0x0003B9C6 File Offset: 0x00039BC6
		[DataSourceProperty]
		public HintViewModel EquipmentHelmSlotHint
		{
			get
			{
				return this._equipmentHelmSlotHint;
			}
			set
			{
				if (value != this._equipmentHelmSlotHint)
				{
					this._equipmentHelmSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentHelmSlotHint");
				}
			}
		}

		// Token: 0x17000444 RID: 1092
		// (get) Token: 0x06000D97 RID: 3479 RVA: 0x0003B9E4 File Offset: 0x00039BE4
		// (set) Token: 0x06000D98 RID: 3480 RVA: 0x0003B9EC File Offset: 0x00039BEC
		[DataSourceProperty]
		public HintViewModel EquipmentArmorSlotHint
		{
			get
			{
				return this._equipmentArmorSlotHint;
			}
			set
			{
				if (value != this._equipmentArmorSlotHint)
				{
					this._equipmentArmorSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentArmorSlotHint");
				}
			}
		}

		// Token: 0x17000445 RID: 1093
		// (get) Token: 0x06000D99 RID: 3481 RVA: 0x0003BA0A File Offset: 0x00039C0A
		// (set) Token: 0x06000D9A RID: 3482 RVA: 0x0003BA12 File Offset: 0x00039C12
		[DataSourceProperty]
		public HintViewModel EquipmentBootSlotHint
		{
			get
			{
				return this._equipmentBootSlotHint;
			}
			set
			{
				if (value != this._equipmentBootSlotHint)
				{
					this._equipmentBootSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentBootSlotHint");
				}
			}
		}

		// Token: 0x17000446 RID: 1094
		// (get) Token: 0x06000D9B RID: 3483 RVA: 0x0003BA30 File Offset: 0x00039C30
		// (set) Token: 0x06000D9C RID: 3484 RVA: 0x0003BA38 File Offset: 0x00039C38
		[DataSourceProperty]
		public HintViewModel EquipmentCloakSlotHint
		{
			get
			{
				return this._equipmentCloakSlotHint;
			}
			set
			{
				if (value != this._equipmentCloakSlotHint)
				{
					this._equipmentCloakSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentCloakSlotHint");
				}
			}
		}

		// Token: 0x17000447 RID: 1095
		// (get) Token: 0x06000D9D RID: 3485 RVA: 0x0003BA56 File Offset: 0x00039C56
		// (set) Token: 0x06000D9E RID: 3486 RVA: 0x0003BA5E File Offset: 0x00039C5E
		[DataSourceProperty]
		public HintViewModel EquipmentGloveSlotHint
		{
			get
			{
				return this._equipmentGloveSlotHint;
			}
			set
			{
				if (value != this._equipmentGloveSlotHint)
				{
					this._equipmentGloveSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentGloveSlotHint");
				}
			}
		}

		// Token: 0x17000448 RID: 1096
		// (get) Token: 0x06000D9F RID: 3487 RVA: 0x0003BA7C File Offset: 0x00039C7C
		// (set) Token: 0x06000DA0 RID: 3488 RVA: 0x0003BA84 File Offset: 0x00039C84
		[DataSourceProperty]
		public HintViewModel EquipmentHarnessSlotHint
		{
			get
			{
				return this._equipmentHarnessSlotHint;
			}
			set
			{
				if (value != this._equipmentHarnessSlotHint)
				{
					this._equipmentHarnessSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentHarnessSlotHint");
				}
			}
		}

		// Token: 0x17000449 RID: 1097
		// (get) Token: 0x06000DA1 RID: 3489 RVA: 0x0003BAA2 File Offset: 0x00039CA2
		// (set) Token: 0x06000DA2 RID: 3490 RVA: 0x0003BAAA File Offset: 0x00039CAA
		[DataSourceProperty]
		public HintViewModel EquipmentMountSlotHint
		{
			get
			{
				return this._equipmentMountSlotHint;
			}
			set
			{
				if (value != this._equipmentMountSlotHint)
				{
					this._equipmentMountSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentMountSlotHint");
				}
			}
		}

		// Token: 0x1700044A RID: 1098
		// (get) Token: 0x06000DA3 RID: 3491 RVA: 0x0003BAC8 File Offset: 0x00039CC8
		// (set) Token: 0x06000DA4 RID: 3492 RVA: 0x0003BAD0 File Offset: 0x00039CD0
		[DataSourceProperty]
		public HintViewModel EquipmentWeaponSlotHint
		{
			get
			{
				return this._equipmentWeaponSlotHint;
			}
			set
			{
				if (value != this._equipmentWeaponSlotHint)
				{
					this._equipmentWeaponSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentWeaponSlotHint");
				}
			}
		}

		// Token: 0x1700044B RID: 1099
		// (get) Token: 0x06000DA5 RID: 3493 RVA: 0x0003BAEE File Offset: 0x00039CEE
		// (set) Token: 0x06000DA6 RID: 3494 RVA: 0x0003BAF6 File Offset: 0x00039CF6
		[DataSourceProperty]
		public HintViewModel EquipmentBannerSlotHint
		{
			get
			{
				return this._equipmentBannerSlotHint;
			}
			set
			{
				if (value != this._equipmentBannerSlotHint)
				{
					this._equipmentBannerSlotHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipmentBannerSlotHint");
				}
			}
		}

		// Token: 0x1700044C RID: 1100
		// (get) Token: 0x06000DA7 RID: 3495 RVA: 0x0003BB14 File Offset: 0x00039D14
		// (set) Token: 0x06000DA8 RID: 3496 RVA: 0x0003BB1C File Offset: 0x00039D1C
		[DataSourceProperty]
		public BasicTooltipViewModel BuyAllHint
		{
			get
			{
				return this._buyAllHint;
			}
			set
			{
				if (value != this._buyAllHint)
				{
					this._buyAllHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "BuyAllHint");
				}
			}
		}

		// Token: 0x1700044D RID: 1101
		// (get) Token: 0x06000DA9 RID: 3497 RVA: 0x0003BB3A File Offset: 0x00039D3A
		// (set) Token: 0x06000DAA RID: 3498 RVA: 0x0003BB42 File Offset: 0x00039D42
		[DataSourceProperty]
		public BasicTooltipViewModel SellAllHint
		{
			get
			{
				return this._sellAllHint;
			}
			set
			{
				if (value != this._sellAllHint)
				{
					this._sellAllHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SellAllHint");
				}
			}
		}

		// Token: 0x1700044E RID: 1102
		// (get) Token: 0x06000DAB RID: 3499 RVA: 0x0003BB60 File Offset: 0x00039D60
		// (set) Token: 0x06000DAC RID: 3500 RVA: 0x0003BB68 File Offset: 0x00039D68
		[DataSourceProperty]
		public BasicTooltipViewModel PreviousCharacterHint
		{
			get
			{
				return this._previousCharacterHint;
			}
			set
			{
				if (value != this._previousCharacterHint)
				{
					this._previousCharacterHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "PreviousCharacterHint");
				}
			}
		}

		// Token: 0x1700044F RID: 1103
		// (get) Token: 0x06000DAD RID: 3501 RVA: 0x0003BB86 File Offset: 0x00039D86
		// (set) Token: 0x06000DAE RID: 3502 RVA: 0x0003BB8E File Offset: 0x00039D8E
		[DataSourceProperty]
		public BasicTooltipViewModel NextCharacterHint
		{
			get
			{
				return this._nextCharacterHint;
			}
			set
			{
				if (value != this._nextCharacterHint)
				{
					this._nextCharacterHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "NextCharacterHint");
				}
			}
		}

		// Token: 0x17000450 RID: 1104
		// (get) Token: 0x06000DAF RID: 3503 RVA: 0x0003BBAC File Offset: 0x00039DAC
		// (set) Token: 0x06000DB0 RID: 3504 RVA: 0x0003BBB4 File Offset: 0x00039DB4
		[DataSourceProperty]
		public HintViewModel WeightHint
		{
			get
			{
				return this._weightHint;
			}
			set
			{
				if (value != this._weightHint)
				{
					this._weightHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "WeightHint");
				}
			}
		}

		// Token: 0x17000451 RID: 1105
		// (get) Token: 0x06000DB1 RID: 3505 RVA: 0x0003BBD2 File Offset: 0x00039DD2
		// (set) Token: 0x06000DB2 RID: 3506 RVA: 0x0003BBDA File Offset: 0x00039DDA
		[DataSourceProperty]
		public HintViewModel PreviewHint
		{
			get
			{
				return this._previewHint;
			}
			set
			{
				if (value != this._previewHint)
				{
					this._previewHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PreviewHint");
				}
			}
		}

		// Token: 0x17000452 RID: 1106
		// (get) Token: 0x06000DB3 RID: 3507 RVA: 0x0003BBF8 File Offset: 0x00039DF8
		// (set) Token: 0x06000DB4 RID: 3508 RVA: 0x0003BC00 File Offset: 0x00039E00
		[DataSourceProperty]
		public HintViewModel EquipHint
		{
			get
			{
				return this._equipHint;
			}
			set
			{
				if (value != this._equipHint)
				{
					this._equipHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EquipHint");
				}
			}
		}

		// Token: 0x17000453 RID: 1107
		// (get) Token: 0x06000DB5 RID: 3509 RVA: 0x0003BC1E File Offset: 0x00039E1E
		// (set) Token: 0x06000DB6 RID: 3510 RVA: 0x0003BC26 File Offset: 0x00039E26
		[DataSourceProperty]
		public HintViewModel UnequipHint
		{
			get
			{
				return this._unequipHint;
			}
			set
			{
				if (value != this._unequipHint)
				{
					this._unequipHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "UnequipHint");
				}
			}
		}

		// Token: 0x17000454 RID: 1108
		// (get) Token: 0x06000DB7 RID: 3511 RVA: 0x0003BC44 File Offset: 0x00039E44
		// (set) Token: 0x06000DB8 RID: 3512 RVA: 0x0003BC4C File Offset: 0x00039E4C
		[DataSourceProperty]
		public HintViewModel SellHint
		{
			get
			{
				return this._sellHint;
			}
			set
			{
				if (value != this._sellHint)
				{
					this._sellHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "SellHint");
				}
			}
		}

		// Token: 0x17000455 RID: 1109
		// (get) Token: 0x06000DB9 RID: 3513 RVA: 0x0003BC6A File Offset: 0x00039E6A
		// (set) Token: 0x06000DBA RID: 3514 RVA: 0x0003BC72 File Offset: 0x00039E72
		[DataSourceProperty]
		public HintViewModel PlayerSideCapacityExceededHint
		{
			get
			{
				return this._playerSideCapacityExceededHint;
			}
			set
			{
				if (value != this._playerSideCapacityExceededHint)
				{
					this._playerSideCapacityExceededHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PlayerSideCapacityExceededHint");
				}
			}
		}

		// Token: 0x17000456 RID: 1110
		// (get) Token: 0x06000DBB RID: 3515 RVA: 0x0003BC90 File Offset: 0x00039E90
		// (set) Token: 0x06000DBC RID: 3516 RVA: 0x0003BC98 File Offset: 0x00039E98
		[DataSourceProperty]
		public HintViewModel MainPartyLandCapacityExceededHint
		{
			get
			{
				return this._mainPartyLandCapacityExceededHint;
			}
			set
			{
				if (value != this._mainPartyLandCapacityExceededHint)
				{
					this._mainPartyLandCapacityExceededHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "MainPartyLandCapacityExceededHint");
				}
			}
		}

		// Token: 0x17000457 RID: 1111
		// (get) Token: 0x06000DBD RID: 3517 RVA: 0x0003BCB6 File Offset: 0x00039EB6
		// (set) Token: 0x06000DBE RID: 3518 RVA: 0x0003BCBE File Offset: 0x00039EBE
		[DataSourceProperty]
		public HintViewModel MainPartySeaCapacityExceededHint
		{
			get
			{
				return this._mainPartySeaCapacityExceededHint;
			}
			set
			{
				if (value != this._mainPartySeaCapacityExceededHint)
				{
					this._mainPartySeaCapacityExceededHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "MainPartySeaCapacityExceededHint");
				}
			}
		}

		// Token: 0x17000458 RID: 1112
		// (get) Token: 0x06000DBF RID: 3519 RVA: 0x0003BCDC File Offset: 0x00039EDC
		// (set) Token: 0x06000DC0 RID: 3520 RVA: 0x0003BCE4 File Offset: 0x00039EE4
		[DataSourceProperty]
		public HintViewModel OtherSideCapacityExceededHint
		{
			get
			{
				return this._otherSideCapacityExceededHint;
			}
			set
			{
				if (value != this._otherSideCapacityExceededHint)
				{
					this._otherSideCapacityExceededHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "OtherSideCapacityExceededHint");
				}
			}
		}

		// Token: 0x17000459 RID: 1113
		// (get) Token: 0x06000DC1 RID: 3521 RVA: 0x0003BD02 File Offset: 0x00039F02
		// (set) Token: 0x06000DC2 RID: 3522 RVA: 0x0003BD0A File Offset: 0x00039F0A
		[DataSourceProperty]
		public SelectorVM<InventoryCharacterSelectorItemVM> CharacterList
		{
			get
			{
				return this._characterList;
			}
			set
			{
				if (value != this._characterList)
				{
					this._characterList = value;
					base.OnPropertyChangedWithValue<SelectorVM<InventoryCharacterSelectorItemVM>>(value, "CharacterList");
				}
			}
		}

		// Token: 0x1700045A RID: 1114
		// (get) Token: 0x06000DC3 RID: 3523 RVA: 0x0003BD28 File Offset: 0x00039F28
		// (set) Token: 0x06000DC4 RID: 3524 RVA: 0x0003BD30 File Offset: 0x00039F30
		[DataSourceProperty]
		public SPInventorySortControllerVM PlayerInventorySortController
		{
			get
			{
				return this._playerInventorySortController;
			}
			set
			{
				if (value != this._playerInventorySortController)
				{
					this._playerInventorySortController = value;
					base.OnPropertyChangedWithValue<SPInventorySortControllerVM>(value, "PlayerInventorySortController");
				}
			}
		}

		// Token: 0x1700045B RID: 1115
		// (get) Token: 0x06000DC5 RID: 3525 RVA: 0x0003BD4E File Offset: 0x00039F4E
		// (set) Token: 0x06000DC6 RID: 3526 RVA: 0x0003BD56 File Offset: 0x00039F56
		[DataSourceProperty]
		public SPInventorySortControllerVM OtherInventorySortController
		{
			get
			{
				return this._otherInventorySortController;
			}
			set
			{
				if (value != this._otherInventorySortController)
				{
					this._otherInventorySortController = value;
					base.OnPropertyChangedWithValue<SPInventorySortControllerVM>(value, "OtherInventorySortController");
				}
			}
		}

		// Token: 0x1700045C RID: 1116
		// (get) Token: 0x06000DC7 RID: 3527 RVA: 0x0003BD74 File Offset: 0x00039F74
		// (set) Token: 0x06000DC8 RID: 3528 RVA: 0x0003BD7C File Offset: 0x00039F7C
		[DataSourceProperty]
		public ItemPreviewVM ItemPreview
		{
			get
			{
				return this._itemPreview;
			}
			set
			{
				if (value != this._itemPreview)
				{
					this._itemPreview = value;
					base.OnPropertyChangedWithValue<ItemPreviewVM>(value, "ItemPreview");
				}
			}
		}

		// Token: 0x1700045D RID: 1117
		// (get) Token: 0x06000DC9 RID: 3529 RVA: 0x0003BD9A File Offset: 0x00039F9A
		// (set) Token: 0x06000DCA RID: 3530 RVA: 0x0003BDA2 File Offset: 0x00039FA2
		[DataSourceProperty]
		public int ActiveFilterIndex
		{
			get
			{
				return (int)this._activeFilterIndex;
			}
			set
			{
				if (value != (int)this._activeFilterIndex)
				{
					this._activeFilterIndex = (SPInventoryVM.Filters)value;
					base.OnPropertyChangedWithValue(value, "ActiveFilterIndex");
				}
			}
		}

		// Token: 0x1700045E RID: 1118
		// (get) Token: 0x06000DCB RID: 3531 RVA: 0x0003BDC0 File Offset: 0x00039FC0
		// (set) Token: 0x06000DCC RID: 3532 RVA: 0x0003BDC8 File Offset: 0x00039FC8
		[DataSourceProperty]
		public bool CompanionExists
		{
			get
			{
				return this._companionExists;
			}
			set
			{
				if (value != this._companionExists)
				{
					this._companionExists = value;
					base.OnPropertyChangedWithValue(value, "CompanionExists");
				}
			}
		}

		// Token: 0x1700045F RID: 1119
		// (get) Token: 0x06000DCD RID: 3533 RVA: 0x0003BDE6 File Offset: 0x00039FE6
		// (set) Token: 0x06000DCE RID: 3534 RVA: 0x0003BDEE File Offset: 0x00039FEE
		[DataSourceProperty]
		public bool IsTradingWithSettlement
		{
			get
			{
				return this._isTradingWithSettlement;
			}
			set
			{
				if (value != this._isTradingWithSettlement)
				{
					this._isTradingWithSettlement = value;
					base.OnPropertyChangedWithValue(value, "IsTradingWithSettlement");
				}
			}
		}

		// Token: 0x17000460 RID: 1120
		// (get) Token: 0x06000DCF RID: 3535 RVA: 0x0003BE0C File Offset: 0x0003A00C
		// (set) Token: 0x06000DD0 RID: 3536 RVA: 0x0003BE14 File Offset: 0x0003A014
		[DataSourceProperty]
		public int EquipmentMode
		{
			get
			{
				return (int)this._equipmentMode;
			}
			set
			{
				if (value != (int)this._equipmentMode)
				{
					this._equipmentMode = (SPInventoryVM.EquipmentModes)value;
					base.OnPropertyChangedWithValue(value, "EquipmentMode");
					this.UpdateRightCharacter();
					this.OnEquipmentModeChanged();
					this.RefreshInformationValues();
					Game.Current.EventManager.TriggerEvent<InventoryEquipmentTypeChangedEvent>(new InventoryEquipmentTypeChangedEvent(this._equipmentMode == SPInventoryVM.EquipmentModes.Battle));
				}
			}
		}

		// Token: 0x17000461 RID: 1121
		// (get) Token: 0x06000DD1 RID: 3537 RVA: 0x0003BE6C File Offset: 0x0003A06C
		// (set) Token: 0x06000DD2 RID: 3538 RVA: 0x0003BE74 File Offset: 0x0003A074
		[DataSourceProperty]
		public bool IsMicsFilterHighlightEnabled
		{
			get
			{
				return this._isMicsFilterHighlightEnabled;
			}
			set
			{
				if (value != this._isMicsFilterHighlightEnabled)
				{
					this._isMicsFilterHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsMicsFilterHighlightEnabled");
				}
			}
		}

		// Token: 0x17000462 RID: 1122
		// (get) Token: 0x06000DD3 RID: 3539 RVA: 0x0003BE92 File Offset: 0x0003A092
		// (set) Token: 0x06000DD4 RID: 3540 RVA: 0x0003BE9A File Offset: 0x0003A09A
		[DataSourceProperty]
		public bool IsEquipmentSetFiltersHighlighted
		{
			get
			{
				return this._isCivilianFilterHighlightEnabled;
			}
			set
			{
				if (value != this._isCivilianFilterHighlightEnabled)
				{
					this._isCivilianFilterHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEquipmentSetFiltersHighlighted");
				}
			}
		}

		// Token: 0x17000463 RID: 1123
		// (get) Token: 0x06000DD5 RID: 3541 RVA: 0x0003BEB8 File Offset: 0x0003A0B8
		// (set) Token: 0x06000DD6 RID: 3542 RVA: 0x0003BEC0 File Offset: 0x0003A0C0
		[DataSourceProperty]
		public ItemMenuVM ItemMenu
		{
			get
			{
				return this._itemMenu;
			}
			set
			{
				if (value != this._itemMenu)
				{
					this._itemMenu = value;
					base.OnPropertyChangedWithValue<ItemMenuVM>(value, "ItemMenu");
				}
			}
		}

		// Token: 0x17000464 RID: 1124
		// (get) Token: 0x06000DD7 RID: 3543 RVA: 0x0003BEDE File Offset: 0x0003A0DE
		// (set) Token: 0x06000DD8 RID: 3544 RVA: 0x0003BEE6 File Offset: 0x0003A0E6
		[DataSourceProperty]
		public string PlayerSideCapacityExceededText
		{
			get
			{
				return this._playerSideCapacityExceededText;
			}
			set
			{
				if (value != this._playerSideCapacityExceededText)
				{
					this._playerSideCapacityExceededText = value;
					base.OnPropertyChangedWithValue<string>(value, "PlayerSideCapacityExceededText");
				}
			}
		}

		// Token: 0x17000465 RID: 1125
		// (get) Token: 0x06000DD9 RID: 3545 RVA: 0x0003BF09 File Offset: 0x0003A109
		// (set) Token: 0x06000DDA RID: 3546 RVA: 0x0003BF11 File Offset: 0x0003A111
		[DataSourceProperty]
		public string MainPartyLandCapacityExceededText
		{
			get
			{
				return this._mainPartyLandCapacityExceededText;
			}
			set
			{
				if (value != this._mainPartyLandCapacityExceededText)
				{
					this._mainPartyLandCapacityExceededText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyLandCapacityExceededText");
				}
			}
		}

		// Token: 0x17000466 RID: 1126
		// (get) Token: 0x06000DDB RID: 3547 RVA: 0x0003BF34 File Offset: 0x0003A134
		// (set) Token: 0x06000DDC RID: 3548 RVA: 0x0003BF3C File Offset: 0x0003A13C
		[DataSourceProperty]
		public string MainPartySeaCapacityExceededText
		{
			get
			{
				return this._mainPartySeaCapacityExceededText;
			}
			set
			{
				if (value != this._mainPartySeaCapacityExceededText)
				{
					this._mainPartySeaCapacityExceededText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartySeaCapacityExceededText");
				}
			}
		}

		// Token: 0x17000467 RID: 1127
		// (get) Token: 0x06000DDD RID: 3549 RVA: 0x0003BF5F File Offset: 0x0003A15F
		// (set) Token: 0x06000DDE RID: 3550 RVA: 0x0003BF67 File Offset: 0x0003A167
		[DataSourceProperty]
		public string SeparatorText
		{
			get
			{
				return this._separatorText;
			}
			set
			{
				if (value != this._separatorText)
				{
					this._separatorText = value;
					base.OnPropertyChangedWithValue<string>(value, "SeparatorText");
				}
			}
		}

		// Token: 0x17000468 RID: 1128
		// (get) Token: 0x06000DDF RID: 3551 RVA: 0x0003BF8A File Offset: 0x0003A18A
		// (set) Token: 0x06000DE0 RID: 3552 RVA: 0x0003BF92 File Offset: 0x0003A192
		[DataSourceProperty]
		public string OtherSideCapacityExceededText
		{
			get
			{
				return this._otherSideCapacityExceededText;
			}
			set
			{
				if (value != this._otherSideCapacityExceededText)
				{
					this._otherSideCapacityExceededText = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherSideCapacityExceededText");
				}
			}
		}

		// Token: 0x17000469 RID: 1129
		// (get) Token: 0x06000DE1 RID: 3553 RVA: 0x0003BFB5 File Offset: 0x0003A1B5
		// (set) Token: 0x06000DE2 RID: 3554 RVA: 0x0003BFBD File Offset: 0x0003A1BD
		[DataSourceProperty]
		public string LeftSearchText
		{
			get
			{
				return this._leftSearchText;
			}
			set
			{
				if (value != this._leftSearchText)
				{
					this._leftSearchText = value;
					base.OnPropertyChangedWithValue<string>(value, "LeftSearchText");
					this.OnSearchTextChanged(true);
				}
			}
		}

		// Token: 0x1700046A RID: 1130
		// (get) Token: 0x06000DE3 RID: 3555 RVA: 0x0003BFE7 File Offset: 0x0003A1E7
		// (set) Token: 0x06000DE4 RID: 3556 RVA: 0x0003BFEF File Offset: 0x0003A1EF
		[DataSourceProperty]
		public string RightSearchText
		{
			get
			{
				return this._rightSearchText;
			}
			set
			{
				if (value != this._rightSearchText)
				{
					this._rightSearchText = value;
					base.OnPropertyChangedWithValue<string>(value, "RightSearchText");
					this.OnSearchTextChanged(false);
				}
			}
		}

		// Token: 0x1700046B RID: 1131
		// (get) Token: 0x06000DE5 RID: 3557 RVA: 0x0003C019 File Offset: 0x0003A219
		// (set) Token: 0x06000DE6 RID: 3558 RVA: 0x0003C021 File Offset: 0x0003A221
		[DataSourceProperty]
		public bool HasGainedExperience
		{
			get
			{
				return this._hasGainedExperience;
			}
			set
			{
				if (value != this._hasGainedExperience)
				{
					this._hasGainedExperience = value;
					base.OnPropertyChangedWithValue(value, "HasGainedExperience");
				}
			}
		}

		// Token: 0x1700046C RID: 1132
		// (get) Token: 0x06000DE7 RID: 3559 RVA: 0x0003C03F File Offset: 0x0003A23F
		// (set) Token: 0x06000DE8 RID: 3560 RVA: 0x0003C047 File Offset: 0x0003A247
		[DataSourceProperty]
		public bool IsDonationXpGainExceedsMax
		{
			get
			{
				return this._isDonationXpGainExceedsMax;
			}
			set
			{
				if (value != this._isDonationXpGainExceedsMax)
				{
					this._isDonationXpGainExceedsMax = value;
					base.OnPropertyChangedWithValue(value, "IsDonationXpGainExceedsMax");
				}
			}
		}

		// Token: 0x1700046D RID: 1133
		// (get) Token: 0x06000DE9 RID: 3561 RVA: 0x0003C065 File Offset: 0x0003A265
		// (set) Token: 0x06000DEA RID: 3562 RVA: 0x0003C06D File Offset: 0x0003A26D
		[DataSourceProperty]
		public bool NoSaddleWarned
		{
			get
			{
				return this._noSaddleWarned;
			}
			set
			{
				if (value != this._noSaddleWarned)
				{
					this._noSaddleWarned = value;
					base.OnPropertyChangedWithValue(value, "NoSaddleWarned");
				}
			}
		}

		// Token: 0x1700046E RID: 1134
		// (get) Token: 0x06000DEB RID: 3563 RVA: 0x0003C08B File Offset: 0x0003A28B
		// (set) Token: 0x06000DEC RID: 3564 RVA: 0x0003C093 File Offset: 0x0003A293
		[DataSourceProperty]
		public bool ShowMainPartyLandCapacityTexts
		{
			get
			{
				return this._showMainPartyLandCapacityTexts;
			}
			set
			{
				if (value != this._showMainPartyLandCapacityTexts)
				{
					this._showMainPartyLandCapacityTexts = value;
					base.OnPropertyChangedWithValue(value, "ShowMainPartyLandCapacityTexts");
				}
			}
		}

		// Token: 0x1700046F RID: 1135
		// (get) Token: 0x06000DED RID: 3565 RVA: 0x0003C0B1 File Offset: 0x0003A2B1
		// (set) Token: 0x06000DEE RID: 3566 RVA: 0x0003C0B9 File Offset: 0x0003A2B9
		[DataSourceProperty]
		public bool ShowMainPartySeaCapacityTexts
		{
			get
			{
				return this._showMainPartySeaCapacityTexts;
			}
			set
			{
				if (value != this._showMainPartySeaCapacityTexts)
				{
					this._showMainPartySeaCapacityTexts = value;
					base.OnPropertyChangedWithValue(value, "ShowMainPartySeaCapacityTexts");
				}
			}
		}

		// Token: 0x17000470 RID: 1136
		// (get) Token: 0x06000DEF RID: 3567 RVA: 0x0003C0D7 File Offset: 0x0003A2D7
		// (set) Token: 0x06000DF0 RID: 3568 RVA: 0x0003C0DF File Offset: 0x0003A2DF
		[DataSourceProperty]
		public bool PlayerEquipmentCountWarned
		{
			get
			{
				return this._playerEquipmentCountWarned;
			}
			set
			{
				if (value != this._playerEquipmentCountWarned)
				{
					this._playerEquipmentCountWarned = value;
					base.OnPropertyChangedWithValue(value, "PlayerEquipmentCountWarned");
				}
			}
		}

		// Token: 0x17000471 RID: 1137
		// (get) Token: 0x06000DF1 RID: 3569 RVA: 0x0003C0FD File Offset: 0x0003A2FD
		// (set) Token: 0x06000DF2 RID: 3570 RVA: 0x0003C105 File Offset: 0x0003A305
		[DataSourceProperty]
		public bool IsMainPartyLandCapacityWarned
		{
			get
			{
				return this._isMainPartyLandCapacityWarned;
			}
			set
			{
				if (value != this._isMainPartyLandCapacityWarned)
				{
					this._isMainPartyLandCapacityWarned = value;
					base.OnPropertyChangedWithValue(value, "IsMainPartyLandCapacityWarned");
				}
			}
		}

		// Token: 0x17000472 RID: 1138
		// (get) Token: 0x06000DF3 RID: 3571 RVA: 0x0003C123 File Offset: 0x0003A323
		// (set) Token: 0x06000DF4 RID: 3572 RVA: 0x0003C12B File Offset: 0x0003A32B
		[DataSourceProperty]
		public bool IsMainPartySeaCapacityWarned
		{
			get
			{
				return this._isMainPartySeaCapacityWarned;
			}
			set
			{
				if (value != this._isMainPartySeaCapacityWarned)
				{
					this._isMainPartySeaCapacityWarned = value;
					base.OnPropertyChangedWithValue(value, "IsMainPartySeaCapacityWarned");
				}
			}
		}

		// Token: 0x17000473 RID: 1139
		// (get) Token: 0x06000DF5 RID: 3573 RVA: 0x0003C149 File Offset: 0x0003A349
		// (set) Token: 0x06000DF6 RID: 3574 RVA: 0x0003C151 File Offset: 0x0003A351
		[DataSourceProperty]
		public bool ShowMainPartyLandCapacityWarning
		{
			get
			{
				return this._showMainPartyLandCapacityWarning;
			}
			set
			{
				if (value != this._showMainPartyLandCapacityWarning)
				{
					this._showMainPartyLandCapacityWarning = value;
					base.OnPropertyChangedWithValue(value, "ShowMainPartyLandCapacityWarning");
				}
			}
		}

		// Token: 0x17000474 RID: 1140
		// (get) Token: 0x06000DF7 RID: 3575 RVA: 0x0003C16F File Offset: 0x0003A36F
		// (set) Token: 0x06000DF8 RID: 3576 RVA: 0x0003C177 File Offset: 0x0003A377
		[DataSourceProperty]
		public bool ShowMainPartySeaCapacityWarning
		{
			get
			{
				return this._showMainPartySeaCapacityWarning;
			}
			set
			{
				if (value != this._showMainPartySeaCapacityWarning)
				{
					this._showMainPartySeaCapacityWarning = value;
					base.OnPropertyChangedWithValue(value, "ShowMainPartySeaCapacityWarning");
				}
			}
		}

		// Token: 0x17000475 RID: 1141
		// (get) Token: 0x06000DF9 RID: 3577 RVA: 0x0003C195 File Offset: 0x0003A395
		// (set) Token: 0x06000DFA RID: 3578 RVA: 0x0003C19D File Offset: 0x0003A39D
		[DataSourceProperty]
		public bool OtherEquipmentCountWarned
		{
			get
			{
				return this._otherEquipmentCountWarned;
			}
			set
			{
				if (value != this._otherEquipmentCountWarned)
				{
					this._otherEquipmentCountWarned = value;
					base.OnPropertyChangedWithValue(value, "OtherEquipmentCountWarned");
				}
			}
		}

		// Token: 0x17000476 RID: 1142
		// (get) Token: 0x06000DFB RID: 3579 RVA: 0x0003C1BB File Offset: 0x0003A3BB
		// (set) Token: 0x06000DFC RID: 3580 RVA: 0x0003C1C3 File Offset: 0x0003A3C3
		[DataSourceProperty]
		public bool OtherEquipmentCapacityExceededWarning
		{
			get
			{
				return this._otherEquipmentCapacityExceededWarning;
			}
			set
			{
				if (value != this._otherEquipmentCapacityExceededWarning)
				{
					this._otherEquipmentCapacityExceededWarning = value;
					base.OnPropertyChangedWithValue(value, "OtherEquipmentCapacityExceededWarning");
				}
			}
		}

		// Token: 0x17000477 RID: 1143
		// (get) Token: 0x06000DFD RID: 3581 RVA: 0x0003C1E1 File Offset: 0x0003A3E1
		// (set) Token: 0x06000DFE RID: 3582 RVA: 0x0003C1E9 File Offset: 0x0003A3E9
		[DataSourceProperty]
		public string OtherEquipmentCountText
		{
			get
			{
				return this._otherEquipmentCountText;
			}
			set
			{
				if (value != this._otherEquipmentCountText)
				{
					this._otherEquipmentCountText = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherEquipmentCountText");
				}
			}
		}

		// Token: 0x17000478 RID: 1144
		// (get) Token: 0x06000DFF RID: 3583 RVA: 0x0003C20C File Offset: 0x0003A40C
		// (set) Token: 0x06000E00 RID: 3584 RVA: 0x0003C214 File Offset: 0x0003A414
		[DataSourceProperty]
		public string MainPartyTotalWeightCarriedText
		{
			get
			{
				return this._mainPartyTotalWeightCarriedText;
			}
			set
			{
				if (value != this._mainPartyTotalWeightCarriedText)
				{
					this._mainPartyTotalWeightCarriedText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyTotalWeightCarriedText");
				}
			}
		}

		// Token: 0x17000479 RID: 1145
		// (get) Token: 0x06000E01 RID: 3585 RVA: 0x0003C237 File Offset: 0x0003A437
		// (set) Token: 0x06000E02 RID: 3586 RVA: 0x0003C23F File Offset: 0x0003A43F
		[DataSourceProperty]
		public string MainPartyLandWeightText
		{
			get
			{
				return this._mainPartyLandWeightText;
			}
			set
			{
				if (value != this._mainPartyLandWeightText)
				{
					this._mainPartyLandWeightText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyLandWeightText");
				}
			}
		}

		// Token: 0x1700047A RID: 1146
		// (get) Token: 0x06000E03 RID: 3587 RVA: 0x0003C262 File Offset: 0x0003A462
		// (set) Token: 0x06000E04 RID: 3588 RVA: 0x0003C26A File Offset: 0x0003A46A
		[DataSourceProperty]
		public string MainPartySeaWeightText
		{
			get
			{
				return this._mainPartySeaWeightText;
			}
			set
			{
				if (value != this._mainPartySeaWeightText)
				{
					this._mainPartySeaWeightText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartySeaWeightText");
				}
			}
		}

		// Token: 0x1700047B RID: 1147
		// (get) Token: 0x06000E05 RID: 3589 RVA: 0x0003C28D File Offset: 0x0003A48D
		// (set) Token: 0x06000E06 RID: 3590 RVA: 0x0003C295 File Offset: 0x0003A495
		[DataSourceProperty]
		public string MainPartyInventoryCapacityText
		{
			get
			{
				return this._mainPartyInventoryCapacityText;
			}
			set
			{
				if (value != this._mainPartyInventoryCapacityText)
				{
					this._mainPartyInventoryCapacityText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyInventoryCapacityText");
				}
			}
		}

		// Token: 0x1700047C RID: 1148
		// (get) Token: 0x06000E07 RID: 3591 RVA: 0x0003C2B8 File Offset: 0x0003A4B8
		// (set) Token: 0x06000E08 RID: 3592 RVA: 0x0003C2C0 File Offset: 0x0003A4C0
		[DataSourceProperty]
		public string MainPartyLandCapacityText
		{
			get
			{
				return this._mainPartyLandCapacityText;
			}
			set
			{
				if (value != this._mainPartyLandCapacityText)
				{
					this._mainPartyLandCapacityText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyLandCapacityText");
				}
			}
		}

		// Token: 0x1700047D RID: 1149
		// (get) Token: 0x06000E09 RID: 3593 RVA: 0x0003C2E3 File Offset: 0x0003A4E3
		// (set) Token: 0x06000E0A RID: 3594 RVA: 0x0003C2EB File Offset: 0x0003A4EB
		[DataSourceProperty]
		public string MainPartySeaCapacityText
		{
			get
			{
				return this._mainPartySeaCapacityText;
			}
			set
			{
				if (value != this._mainPartySeaCapacityText)
				{
					this._mainPartySeaCapacityText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartySeaCapacityText");
				}
			}
		}

		// Token: 0x1700047E RID: 1150
		// (get) Token: 0x06000E0B RID: 3595 RVA: 0x0003C30E File Offset: 0x0003A50E
		// (set) Token: 0x06000E0C RID: 3596 RVA: 0x0003C316 File Offset: 0x0003A516
		[DataSourceProperty]
		public string NoSaddleText
		{
			get
			{
				return this._noSaddleText;
			}
			set
			{
				if (value != this._noSaddleText)
				{
					this._noSaddleText = value;
					base.OnPropertyChangedWithValue<string>(value, "NoSaddleText");
				}
			}
		}

		// Token: 0x1700047F RID: 1151
		// (get) Token: 0x06000E0D RID: 3597 RVA: 0x0003C339 File Offset: 0x0003A539
		// (set) Token: 0x06000E0E RID: 3598 RVA: 0x0003C341 File Offset: 0x0003A541
		[DataSourceProperty]
		public int TargetEquipmentIndex
		{
			get
			{
				return (int)this._targetEquipmentIndex;
			}
			set
			{
				if (value != (int)this._targetEquipmentIndex)
				{
					this._targetEquipmentIndex = (EquipmentIndex)value;
					base.OnPropertyChangedWithValue(value, "TargetEquipmentIndex");
				}
			}
		}

		// Token: 0x17000480 RID: 1152
		// (get) Token: 0x06000E0F RID: 3599 RVA: 0x0003C35F File Offset: 0x0003A55F
		// (set) Token: 0x06000E10 RID: 3600 RVA: 0x0003C367 File Offset: 0x0003A567
		public EquipmentIndex TargetEquipmentType
		{
			get
			{
				return this._targetEquipmentIndex;
			}
			set
			{
				if (value != this._targetEquipmentIndex)
				{
					this._targetEquipmentIndex = value;
					base.OnPropertyChanged("TargetEquipmentIndex");
				}
			}
		}

		// Token: 0x17000481 RID: 1153
		// (get) Token: 0x06000E11 RID: 3601 RVA: 0x0003C384 File Offset: 0x0003A584
		// (set) Token: 0x06000E12 RID: 3602 RVA: 0x0003C38C File Offset: 0x0003A58C
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
				}
				this.RefreshTransactionCost(value);
			}
		}

		// Token: 0x17000482 RID: 1154
		// (get) Token: 0x06000E13 RID: 3603 RVA: 0x0003C3B1 File Offset: 0x0003A5B1
		// (set) Token: 0x06000E14 RID: 3604 RVA: 0x0003C3B9 File Offset: 0x0003A5B9
		[DataSourceProperty]
		public bool IsTrading
		{
			get
			{
				return this._isTrading;
			}
			set
			{
				if (value != this._isTrading)
				{
					this._isTrading = value;
					base.OnPropertyChangedWithValue(value, "IsTrading");
				}
			}
		}

		// Token: 0x17000483 RID: 1155
		// (get) Token: 0x06000E15 RID: 3605 RVA: 0x0003C3D7 File Offset: 0x0003A5D7
		// (set) Token: 0x06000E16 RID: 3606 RVA: 0x0003C3DF File Offset: 0x0003A5DF
		[DataSourceProperty]
		public bool EquipAfterBuy
		{
			get
			{
				return this._equipAfterBuy;
			}
			set
			{
				if (value != this._equipAfterBuy)
				{
					this._equipAfterBuy = value;
					base.OnPropertyChangedWithValue(value, "EquipAfterBuy");
				}
			}
		}

		// Token: 0x17000484 RID: 1156
		// (get) Token: 0x06000E17 RID: 3607 RVA: 0x0003C3FD File Offset: 0x0003A5FD
		// (set) Token: 0x06000E18 RID: 3608 RVA: 0x0003C405 File Offset: 0x0003A605
		[DataSourceProperty]
		public string TradeLbl
		{
			get
			{
				return this._tradeLbl;
			}
			set
			{
				if (value != this._tradeLbl)
				{
					this._tradeLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "TradeLbl");
				}
			}
		}

		// Token: 0x17000485 RID: 1157
		// (get) Token: 0x06000E19 RID: 3609 RVA: 0x0003C428 File Offset: 0x0003A628
		// (set) Token: 0x06000E1A RID: 3610 RVA: 0x0003C430 File Offset: 0x0003A630
		[DataSourceProperty]
		public string ExperienceLbl
		{
			get
			{
				return this._experienceLbl;
			}
			set
			{
				if (value != this._experienceLbl)
				{
					this._experienceLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ExperienceLbl");
				}
			}
		}

		// Token: 0x17000486 RID: 1158
		// (get) Token: 0x06000E1B RID: 3611 RVA: 0x0003C453 File Offset: 0x0003A653
		// (set) Token: 0x06000E1C RID: 3612 RVA: 0x0003C45B File Offset: 0x0003A65B
		[DataSourceProperty]
		public string CurrentCharacterName
		{
			get
			{
				return this._currentCharacterName;
			}
			set
			{
				if (value != this._currentCharacterName)
				{
					this._currentCharacterName = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCharacterName");
				}
			}
		}

		// Token: 0x17000487 RID: 1159
		// (get) Token: 0x06000E1D RID: 3613 RVA: 0x0003C47E File Offset: 0x0003A67E
		// (set) Token: 0x06000E1E RID: 3614 RVA: 0x0003C486 File Offset: 0x0003A686
		[DataSourceProperty]
		public string RightInventoryOwnerName
		{
			get
			{
				return this._rightInventoryOwnerName;
			}
			set
			{
				if (value != this._rightInventoryOwnerName)
				{
					this._rightInventoryOwnerName = value;
					base.OnPropertyChangedWithValue<string>(value, "RightInventoryOwnerName");
				}
			}
		}

		// Token: 0x17000488 RID: 1160
		// (get) Token: 0x06000E1F RID: 3615 RVA: 0x0003C4A9 File Offset: 0x0003A6A9
		// (set) Token: 0x06000E20 RID: 3616 RVA: 0x0003C4B1 File Offset: 0x0003A6B1
		[DataSourceProperty]
		public string LeftInventoryOwnerName
		{
			get
			{
				return this._leftInventoryOwnerName;
			}
			set
			{
				if (value != this._leftInventoryOwnerName)
				{
					this._leftInventoryOwnerName = value;
					base.OnPropertyChangedWithValue<string>(value, "LeftInventoryOwnerName");
				}
			}
		}

		// Token: 0x17000489 RID: 1161
		// (get) Token: 0x06000E21 RID: 3617 RVA: 0x0003C4D4 File Offset: 0x0003A6D4
		// (set) Token: 0x06000E22 RID: 3618 RVA: 0x0003C4DC File Offset: 0x0003A6DC
		[DataSourceProperty]
		public int RightInventoryOwnerGold
		{
			get
			{
				return this._rightInventoryOwnerGold;
			}
			set
			{
				if (value != this._rightInventoryOwnerGold)
				{
					this._rightInventoryOwnerGold = value;
					base.OnPropertyChangedWithValue(value, "RightInventoryOwnerGold");
				}
			}
		}

		// Token: 0x1700048A RID: 1162
		// (get) Token: 0x06000E23 RID: 3619 RVA: 0x0003C4FA File Offset: 0x0003A6FA
		// (set) Token: 0x06000E24 RID: 3620 RVA: 0x0003C502 File Offset: 0x0003A702
		[DataSourceProperty]
		public int LeftInventoryOwnerGold
		{
			get
			{
				return this._leftInventoryOwnerGold;
			}
			set
			{
				if (value != this._leftInventoryOwnerGold)
				{
					this._leftInventoryOwnerGold = value;
					base.OnPropertyChangedWithValue(value, "LeftInventoryOwnerGold");
				}
			}
		}

		// Token: 0x1700048B RID: 1163
		// (get) Token: 0x06000E25 RID: 3621 RVA: 0x0003C520 File Offset: 0x0003A720
		// (set) Token: 0x06000E26 RID: 3622 RVA: 0x0003C528 File Offset: 0x0003A728
		[DataSourceProperty]
		public int ItemCountToBuy
		{
			get
			{
				return this._itemCountToBuy;
			}
			set
			{
				if (value != this._itemCountToBuy)
				{
					this._itemCountToBuy = value;
					base.OnPropertyChangedWithValue(value, "ItemCountToBuy");
				}
			}
		}

		// Token: 0x1700048C RID: 1164
		// (get) Token: 0x06000E27 RID: 3623 RVA: 0x0003C546 File Offset: 0x0003A746
		// (set) Token: 0x06000E28 RID: 3624 RVA: 0x0003C54E File Offset: 0x0003A74E
		[DataSourceProperty]
		public string CurrentCharacterTotalEncumbrance
		{
			get
			{
				return this._currentCharacterTotalEncumbrance;
			}
			set
			{
				if (value != this._currentCharacterTotalEncumbrance)
				{
					this._currentCharacterTotalEncumbrance = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCharacterTotalEncumbrance");
				}
			}
		}

		// Token: 0x1700048D RID: 1165
		// (get) Token: 0x06000E29 RID: 3625 RVA: 0x0003C571 File Offset: 0x0003A771
		// (set) Token: 0x06000E2A RID: 3626 RVA: 0x0003C579 File Offset: 0x0003A779
		[DataSourceProperty]
		public float CurrentCharacterLegArmor
		{
			get
			{
				return this._currentCharacterLegArmor;
			}
			set
			{
				if (MathF.Abs(value - this._currentCharacterLegArmor) > 0.01f)
				{
					this._currentCharacterLegArmor = value;
					base.OnPropertyChangedWithValue(value, "CurrentCharacterLegArmor");
				}
			}
		}

		// Token: 0x1700048E RID: 1166
		// (get) Token: 0x06000E2B RID: 3627 RVA: 0x0003C5A2 File Offset: 0x0003A7A2
		// (set) Token: 0x06000E2C RID: 3628 RVA: 0x0003C5AA File Offset: 0x0003A7AA
		[DataSourceProperty]
		public float CurrentCharacterHeadArmor
		{
			get
			{
				return this._currentCharacterHeadArmor;
			}
			set
			{
				if (MathF.Abs(value - this._currentCharacterHeadArmor) > 0.01f)
				{
					this._currentCharacterHeadArmor = value;
					base.OnPropertyChangedWithValue(value, "CurrentCharacterHeadArmor");
				}
			}
		}

		// Token: 0x1700048F RID: 1167
		// (get) Token: 0x06000E2D RID: 3629 RVA: 0x0003C5D3 File Offset: 0x0003A7D3
		// (set) Token: 0x06000E2E RID: 3630 RVA: 0x0003C5DB File Offset: 0x0003A7DB
		[DataSourceProperty]
		public float CurrentCharacterBodyArmor
		{
			get
			{
				return this._currentCharacterBodyArmor;
			}
			set
			{
				if (MathF.Abs(value - this._currentCharacterBodyArmor) > 0.01f)
				{
					this._currentCharacterBodyArmor = value;
					base.OnPropertyChangedWithValue(value, "CurrentCharacterBodyArmor");
				}
			}
		}

		// Token: 0x17000490 RID: 1168
		// (get) Token: 0x06000E2F RID: 3631 RVA: 0x0003C604 File Offset: 0x0003A804
		// (set) Token: 0x06000E30 RID: 3632 RVA: 0x0003C60C File Offset: 0x0003A80C
		[DataSourceProperty]
		public float CurrentCharacterArmArmor
		{
			get
			{
				return this._currentCharacterArmArmor;
			}
			set
			{
				if (MathF.Abs(value - this._currentCharacterArmArmor) > 0.01f)
				{
					this._currentCharacterArmArmor = value;
					base.OnPropertyChangedWithValue(value, "CurrentCharacterArmArmor");
				}
			}
		}

		// Token: 0x17000491 RID: 1169
		// (get) Token: 0x06000E31 RID: 3633 RVA: 0x0003C635 File Offset: 0x0003A835
		// (set) Token: 0x06000E32 RID: 3634 RVA: 0x0003C63D File Offset: 0x0003A83D
		[DataSourceProperty]
		public float CurrentCharacterHorseArmor
		{
			get
			{
				return this._currentCharacterHorseArmor;
			}
			set
			{
				if (MathF.Abs(value - this._currentCharacterHorseArmor) > 0.01f)
				{
					this._currentCharacterHorseArmor = value;
					base.OnPropertyChangedWithValue(value, "CurrentCharacterHorseArmor");
				}
			}
		}

		// Token: 0x17000492 RID: 1170
		// (get) Token: 0x06000E33 RID: 3635 RVA: 0x0003C666 File Offset: 0x0003A866
		// (set) Token: 0x06000E34 RID: 3636 RVA: 0x0003C66E File Offset: 0x0003A86E
		[DataSourceProperty]
		public bool IsRefreshed
		{
			get
			{
				return this._isRefreshed;
			}
			set
			{
				if (this._isRefreshed != value)
				{
					this._isRefreshed = value;
					base.OnPropertyChangedWithValue(value, "IsRefreshed");
				}
			}
		}

		// Token: 0x17000493 RID: 1171
		// (get) Token: 0x06000E35 RID: 3637 RVA: 0x0003C68C File Offset: 0x0003A88C
		// (set) Token: 0x06000E36 RID: 3638 RVA: 0x0003C694 File Offset: 0x0003A894
		[DataSourceProperty]
		public bool IsExtendedEquipmentControlsEnabled
		{
			get
			{
				return this._isExtendedEquipmentControlsEnabled;
			}
			set
			{
				if (value != this._isExtendedEquipmentControlsEnabled)
				{
					this._isExtendedEquipmentControlsEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsExtendedEquipmentControlsEnabled");
				}
			}
		}

		// Token: 0x17000494 RID: 1172
		// (get) Token: 0x06000E37 RID: 3639 RVA: 0x0003C6B2 File Offset: 0x0003A8B2
		// (set) Token: 0x06000E38 RID: 3640 RVA: 0x0003C6BA File Offset: 0x0003A8BA
		[DataSourceProperty]
		public bool IsFocusedOnItemList
		{
			get
			{
				return this._isFocusedOnItemList;
			}
			set
			{
				if (value != this._isFocusedOnItemList)
				{
					this._isFocusedOnItemList = value;
					base.OnPropertyChangedWithValue(value, "IsFocusedOnItemList");
				}
			}
		}

		// Token: 0x17000495 RID: 1173
		// (get) Token: 0x06000E39 RID: 3641 RVA: 0x0003C6D8 File Offset: 0x0003A8D8
		// (set) Token: 0x06000E3A RID: 3642 RVA: 0x0003C6E0 File Offset: 0x0003A8E0
		[DataSourceProperty]
		public SPItemVM CurrentFocusedItem
		{
			get
			{
				return this._currentFocusedItem;
			}
			set
			{
				if (value != this._currentFocusedItem)
				{
					this._currentFocusedItem = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CurrentFocusedItem");
				}
			}
		}

		// Token: 0x17000496 RID: 1174
		// (get) Token: 0x06000E3B RID: 3643 RVA: 0x0003C6FE File Offset: 0x0003A8FE
		// (set) Token: 0x06000E3C RID: 3644 RVA: 0x0003C706 File Offset: 0x0003A906
		[DataSourceProperty]
		public SPItemVM CharacterHelmSlot
		{
			get
			{
				return this._characterHelmSlot;
			}
			set
			{
				if (value != this._characterHelmSlot)
				{
					this._characterHelmSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterHelmSlot");
				}
			}
		}

		// Token: 0x17000497 RID: 1175
		// (get) Token: 0x06000E3D RID: 3645 RVA: 0x0003C724 File Offset: 0x0003A924
		// (set) Token: 0x06000E3E RID: 3646 RVA: 0x0003C72C File Offset: 0x0003A92C
		[DataSourceProperty]
		public SPItemVM CharacterCloakSlot
		{
			get
			{
				return this._characterCloakSlot;
			}
			set
			{
				if (value != this._characterCloakSlot)
				{
					this._characterCloakSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterCloakSlot");
				}
			}
		}

		// Token: 0x17000498 RID: 1176
		// (get) Token: 0x06000E3F RID: 3647 RVA: 0x0003C74A File Offset: 0x0003A94A
		// (set) Token: 0x06000E40 RID: 3648 RVA: 0x0003C752 File Offset: 0x0003A952
		[DataSourceProperty]
		public SPItemVM CharacterTorsoSlot
		{
			get
			{
				return this._characterTorsoSlot;
			}
			set
			{
				if (value != this._characterTorsoSlot)
				{
					this._characterTorsoSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterTorsoSlot");
				}
			}
		}

		// Token: 0x17000499 RID: 1177
		// (get) Token: 0x06000E41 RID: 3649 RVA: 0x0003C770 File Offset: 0x0003A970
		// (set) Token: 0x06000E42 RID: 3650 RVA: 0x0003C778 File Offset: 0x0003A978
		[DataSourceProperty]
		public SPItemVM CharacterGloveSlot
		{
			get
			{
				return this._characterGloveSlot;
			}
			set
			{
				if (value != this._characterGloveSlot)
				{
					this._characterGloveSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterGloveSlot");
				}
			}
		}

		// Token: 0x1700049A RID: 1178
		// (get) Token: 0x06000E43 RID: 3651 RVA: 0x0003C796 File Offset: 0x0003A996
		// (set) Token: 0x06000E44 RID: 3652 RVA: 0x0003C79E File Offset: 0x0003A99E
		[DataSourceProperty]
		public SPItemVM CharacterBootSlot
		{
			get
			{
				return this._characterBootSlot;
			}
			set
			{
				if (value != this._characterBootSlot)
				{
					this._characterBootSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterBootSlot");
				}
			}
		}

		// Token: 0x1700049B RID: 1179
		// (get) Token: 0x06000E45 RID: 3653 RVA: 0x0003C7BC File Offset: 0x0003A9BC
		// (set) Token: 0x06000E46 RID: 3654 RVA: 0x0003C7C4 File Offset: 0x0003A9C4
		[DataSourceProperty]
		public SPItemVM CharacterMountSlot
		{
			get
			{
				return this._characterMountSlot;
			}
			set
			{
				if (value != this._characterMountSlot)
				{
					this._characterMountSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterMountSlot");
				}
			}
		}

		// Token: 0x1700049C RID: 1180
		// (get) Token: 0x06000E47 RID: 3655 RVA: 0x0003C7E2 File Offset: 0x0003A9E2
		// (set) Token: 0x06000E48 RID: 3656 RVA: 0x0003C7EA File Offset: 0x0003A9EA
		[DataSourceProperty]
		public SPItemVM CharacterMountArmorSlot
		{
			get
			{
				return this._characterMountArmorSlot;
			}
			set
			{
				if (value != this._characterMountArmorSlot)
				{
					this._characterMountArmorSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterMountArmorSlot");
				}
			}
		}

		// Token: 0x1700049D RID: 1181
		// (get) Token: 0x06000E49 RID: 3657 RVA: 0x0003C808 File Offset: 0x0003AA08
		// (set) Token: 0x06000E4A RID: 3658 RVA: 0x0003C810 File Offset: 0x0003AA10
		[DataSourceProperty]
		public SPItemVM CharacterWeapon1Slot
		{
			get
			{
				return this._characterWeapon1Slot;
			}
			set
			{
				if (value != this._characterWeapon1Slot)
				{
					this._characterWeapon1Slot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterWeapon1Slot");
				}
			}
		}

		// Token: 0x1700049E RID: 1182
		// (get) Token: 0x06000E4B RID: 3659 RVA: 0x0003C82E File Offset: 0x0003AA2E
		// (set) Token: 0x06000E4C RID: 3660 RVA: 0x0003C836 File Offset: 0x0003AA36
		[DataSourceProperty]
		public SPItemVM CharacterWeapon2Slot
		{
			get
			{
				return this._characterWeapon2Slot;
			}
			set
			{
				if (value != this._characterWeapon2Slot)
				{
					this._characterWeapon2Slot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterWeapon2Slot");
				}
			}
		}

		// Token: 0x1700049F RID: 1183
		// (get) Token: 0x06000E4D RID: 3661 RVA: 0x0003C854 File Offset: 0x0003AA54
		// (set) Token: 0x06000E4E RID: 3662 RVA: 0x0003C85C File Offset: 0x0003AA5C
		[DataSourceProperty]
		public SPItemVM CharacterWeapon3Slot
		{
			get
			{
				return this._characterWeapon3Slot;
			}
			set
			{
				if (value != this._characterWeapon3Slot)
				{
					this._characterWeapon3Slot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterWeapon3Slot");
				}
			}
		}

		// Token: 0x170004A0 RID: 1184
		// (get) Token: 0x06000E4F RID: 3663 RVA: 0x0003C87A File Offset: 0x0003AA7A
		// (set) Token: 0x06000E50 RID: 3664 RVA: 0x0003C882 File Offset: 0x0003AA82
		[DataSourceProperty]
		public SPItemVM CharacterWeapon4Slot
		{
			get
			{
				return this._characterWeapon4Slot;
			}
			set
			{
				if (value != this._characterWeapon4Slot)
				{
					this._characterWeapon4Slot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterWeapon4Slot");
				}
			}
		}

		// Token: 0x170004A1 RID: 1185
		// (get) Token: 0x06000E51 RID: 3665 RVA: 0x0003C8A0 File Offset: 0x0003AAA0
		// (set) Token: 0x06000E52 RID: 3666 RVA: 0x0003C8A8 File Offset: 0x0003AAA8
		[DataSourceProperty]
		public SPItemVM CharacterBannerSlot
		{
			get
			{
				return this._characterBannerSlot;
			}
			set
			{
				if (value != this._characterBannerSlot)
				{
					this._characterBannerSlot = value;
					base.OnPropertyChangedWithValue<SPItemVM>(value, "CharacterBannerSlot");
				}
			}
		}

		// Token: 0x170004A2 RID: 1186
		// (get) Token: 0x06000E53 RID: 3667 RVA: 0x0003C8C6 File Offset: 0x0003AAC6
		// (set) Token: 0x06000E54 RID: 3668 RVA: 0x0003C8CE File Offset: 0x0003AACE
		[DataSourceProperty]
		public HeroViewModel MainCharacter
		{
			get
			{
				return this._mainCharacter;
			}
			set
			{
				if (value != this._mainCharacter)
				{
					this._mainCharacter = value;
					base.OnPropertyChangedWithValue<HeroViewModel>(value, "MainCharacter");
				}
			}
		}

		// Token: 0x170004A3 RID: 1187
		// (get) Token: 0x06000E55 RID: 3669 RVA: 0x0003C8EC File Offset: 0x0003AAEC
		// (set) Token: 0x06000E56 RID: 3670 RVA: 0x0003C8F4 File Offset: 0x0003AAF4
		[DataSourceProperty]
		public MBBindingList<SPItemVM> RightItemListVM
		{
			get
			{
				return this._rightItemListVM;
			}
			set
			{
				if (value != this._rightItemListVM)
				{
					this._rightItemListVM = value;
					base.OnPropertyChangedWithValue<MBBindingList<SPItemVM>>(value, "RightItemListVM");
				}
			}
		}

		// Token: 0x170004A4 RID: 1188
		// (get) Token: 0x06000E57 RID: 3671 RVA: 0x0003C912 File Offset: 0x0003AB12
		// (set) Token: 0x06000E58 RID: 3672 RVA: 0x0003C91A File Offset: 0x0003AB1A
		[DataSourceProperty]
		public MBBindingList<SPItemVM> LeftItemListVM
		{
			get
			{
				return this._leftItemListVM;
			}
			set
			{
				if (value != this._leftItemListVM)
				{
					this._leftItemListVM = value;
					base.OnPropertyChangedWithValue<MBBindingList<SPItemVM>>(value, "LeftItemListVM");
				}
			}
		}

		// Token: 0x170004A5 RID: 1189
		// (get) Token: 0x06000E59 RID: 3673 RVA: 0x0003C938 File Offset: 0x0003AB38
		// (set) Token: 0x06000E5A RID: 3674 RVA: 0x0003C940 File Offset: 0x0003AB40
		[DataSourceProperty]
		public bool IsBannerItemsHighlightApplied
		{
			get
			{
				return this._isBannerItemsHighlightApplied;
			}
			set
			{
				if (value != this._isBannerItemsHighlightApplied)
				{
					this._isBannerItemsHighlightApplied = value;
					base.OnPropertyChangedWithValue(value, "IsBannerItemsHighlightApplied");
				}
			}
		}

		// Token: 0x170004A6 RID: 1190
		// (get) Token: 0x06000E5B RID: 3675 RVA: 0x0003C95E File Offset: 0x0003AB5E
		// (set) Token: 0x06000E5C RID: 3676 RVA: 0x0003C966 File Offset: 0x0003AB66
		[DataSourceProperty]
		public string BannerTypeName
		{
			get
			{
				return this._bannerTypeName;
			}
			set
			{
				if (value != this._bannerTypeName)
				{
					this._bannerTypeName = value;
					base.OnPropertyChangedWithValue<string>(value, "BannerTypeName");
				}
			}
		}

		// Token: 0x170004A7 RID: 1191
		// (get) Token: 0x06000E5D RID: 3677 RVA: 0x0003C989 File Offset: 0x0003AB89
		// (set) Token: 0x06000E5E RID: 3678 RVA: 0x0003C991 File Offset: 0x0003AB91
		[DataSourceProperty]
		public bool ScrollToItem
		{
			get
			{
				return this._scrollToItem;
			}
			set
			{
				if (value != this._scrollToItem)
				{
					this._scrollToItem = value;
					base.OnPropertyChangedWithValue(value, "ScrollToItem");
				}
			}
		}

		// Token: 0x170004A8 RID: 1192
		// (get) Token: 0x06000E5F RID: 3679 RVA: 0x0003C9AF File Offset: 0x0003ABAF
		// (set) Token: 0x06000E60 RID: 3680 RVA: 0x0003C9B7 File Offset: 0x0003ABB7
		[DataSourceProperty]
		public string ScrollItemId
		{
			get
			{
				return this._scrollItemId;
			}
			set
			{
				if (value != this._scrollItemId)
				{
					this._scrollItemId = value;
					base.OnPropertyChangedWithValue<string>(value, "ScrollItemId");
				}
			}
		}

		// Token: 0x170004A9 RID: 1193
		// (get) Token: 0x06000E61 RID: 3681 RVA: 0x0003C9DA File Offset: 0x0003ABDA
		// (set) Token: 0x06000E62 RID: 3682 RVA: 0x0003C9E2 File Offset: 0x0003ABE2
		[DataSourceProperty]
		public bool IsCivilianMode
		{
			get
			{
				return this._isCivilianMode;
			}
			set
			{
				if (value != this._isCivilianMode)
				{
					this._isCivilianMode = value;
					base.OnPropertyChangedWithValue(value, "IsCivilianMode");
				}
			}
		}

		// Token: 0x170004AA RID: 1194
		// (get) Token: 0x06000E63 RID: 3683 RVA: 0x0003CA00 File Offset: 0x0003AC00
		// (set) Token: 0x06000E64 RID: 3684 RVA: 0x0003CA08 File Offset: 0x0003AC08
		[DataSourceProperty]
		public bool IsBattleMode
		{
			get
			{
				return this._isBattleMode;
			}
			set
			{
				if (value != this._isBattleMode)
				{
					this._isBattleMode = value;
					base.OnPropertyChangedWithValue(value, "IsBattleMode");
				}
			}
		}

		// Token: 0x170004AB RID: 1195
		// (get) Token: 0x06000E65 RID: 3685 RVA: 0x0003CA26 File Offset: 0x0003AC26
		// (set) Token: 0x06000E66 RID: 3686 RVA: 0x0003CA2E File Offset: 0x0003AC2E
		[DataSourceProperty]
		public bool IsStealthMode
		{
			get
			{
				return this._isStealthMode;
			}
			set
			{
				if (value != this._isStealthMode)
				{
					this._isStealthMode = value;
					base.OnPropertyChangedWithValue(value, "IsStealthMode");
				}
			}
		}

		// Token: 0x06000E67 RID: 3687 RVA: 0x0003CA4C File Offset: 0x0003AC4C
		private TextObject GetPreviousCharacterKeyText()
		{
			if (this.PreviousCharacterInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.PreviousCharacterInputKey.KeyID);
		}

		// Token: 0x06000E68 RID: 3688 RVA: 0x0003CA7A File Offset: 0x0003AC7A
		private TextObject GetNextCharacterKeyText()
		{
			if (this.NextCharacterInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.NextCharacterInputKey.KeyID);
		}

		// Token: 0x06000E69 RID: 3689 RVA: 0x0003CAA8 File Offset: 0x0003ACA8
		private TextObject GetBuyAllKeyText()
		{
			if (this.BuyAllInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.BuyAllInputKey.KeyID);
		}

		// Token: 0x06000E6A RID: 3690 RVA: 0x0003CAD6 File Offset: 0x0003ACD6
		private TextObject GetSellAllKeyText()
		{
			if (this.SellAllInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.SellAllInputKey.KeyID);
		}

		// Token: 0x06000E6B RID: 3691 RVA: 0x0003CB04 File Offset: 0x0003AD04
		public void SetResetInputKey(HotKey hotkey)
		{
			this.ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x06000E6C RID: 3692 RVA: 0x0003CB13 File Offset: 0x0003AD13
		public void SetCancelInputKey(HotKey gameKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(gameKey, true);
		}

		// Token: 0x06000E6D RID: 3693 RVA: 0x0003CB22 File Offset: 0x0003AD22
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000E6E RID: 3694 RVA: 0x0003CB31 File Offset: 0x0003AD31
		public void SetPreviousCharacterInputKey(HotKey hotKey)
		{
			this.PreviousCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.SetPreviousCharacterHint();
		}

		// Token: 0x06000E6F RID: 3695 RVA: 0x0003CB46 File Offset: 0x0003AD46
		public void SetNextCharacterInputKey(HotKey hotKey)
		{
			this.NextCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.SetNextCharacterHint();
		}

		// Token: 0x06000E70 RID: 3696 RVA: 0x0003CB5B File Offset: 0x0003AD5B
		public void SetBuyAllInputKey(HotKey hotKey)
		{
			this.BuyAllInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.SetBuyAllHint();
		}

		// Token: 0x06000E71 RID: 3697 RVA: 0x0003CB70 File Offset: 0x0003AD70
		public void SetSellAllInputKey(HotKey hotKey)
		{
			this.SellAllInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.SetSellAllHint();
		}

		// Token: 0x06000E72 RID: 3698 RVA: 0x0003CB85 File Offset: 0x0003AD85
		public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
		{
			this._getKeyTextFromKeyId = getKeyTextFromKeyId;
		}

		// Token: 0x170004AC RID: 1196
		// (get) Token: 0x06000E73 RID: 3699 RVA: 0x0003CB8E File Offset: 0x0003AD8E
		// (set) Token: 0x06000E74 RID: 3700 RVA: 0x0003CB96 File Offset: 0x0003AD96
		[DataSourceProperty]
		public InputKeyItemVM ResetInputKey
		{
			get
			{
				return this._resetInputKey;
			}
			set
			{
				if (value != this._resetInputKey)
				{
					this._resetInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ResetInputKey");
				}
			}
		}

		// Token: 0x170004AD RID: 1197
		// (get) Token: 0x06000E75 RID: 3701 RVA: 0x0003CBB4 File Offset: 0x0003ADB4
		// (set) Token: 0x06000E76 RID: 3702 RVA: 0x0003CBBC File Offset: 0x0003ADBC
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x170004AE RID: 1198
		// (get) Token: 0x06000E77 RID: 3703 RVA: 0x0003CBDA File Offset: 0x0003ADDA
		// (set) Token: 0x06000E78 RID: 3704 RVA: 0x0003CBE2 File Offset: 0x0003ADE2
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x170004AF RID: 1199
		// (get) Token: 0x06000E79 RID: 3705 RVA: 0x0003CC00 File Offset: 0x0003AE00
		// (set) Token: 0x06000E7A RID: 3706 RVA: 0x0003CC08 File Offset: 0x0003AE08
		[DataSourceProperty]
		public InputKeyItemVM PreviousCharacterInputKey
		{
			get
			{
				return this._previousCharacterInputKey;
			}
			set
			{
				if (value != this._previousCharacterInputKey)
				{
					this._previousCharacterInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PreviousCharacterInputKey");
				}
			}
		}

		// Token: 0x170004B0 RID: 1200
		// (get) Token: 0x06000E7B RID: 3707 RVA: 0x0003CC26 File Offset: 0x0003AE26
		// (set) Token: 0x06000E7C RID: 3708 RVA: 0x0003CC2E File Offset: 0x0003AE2E
		[DataSourceProperty]
		public InputKeyItemVM NextCharacterInputKey
		{
			get
			{
				return this._nextCharacterInputKey;
			}
			set
			{
				if (value != this._nextCharacterInputKey)
				{
					this._nextCharacterInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "NextCharacterInputKey");
				}
			}
		}

		// Token: 0x170004B1 RID: 1201
		// (get) Token: 0x06000E7D RID: 3709 RVA: 0x0003CC4C File Offset: 0x0003AE4C
		// (set) Token: 0x06000E7E RID: 3710 RVA: 0x0003CC54 File Offset: 0x0003AE54
		[DataSourceProperty]
		public InputKeyItemVM BuyAllInputKey
		{
			get
			{
				return this._buyAllInputKey;
			}
			set
			{
				if (value != this._buyAllInputKey)
				{
					this._buyAllInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "BuyAllInputKey");
				}
			}
		}

		// Token: 0x170004B2 RID: 1202
		// (get) Token: 0x06000E7F RID: 3711 RVA: 0x0003CC72 File Offset: 0x0003AE72
		// (set) Token: 0x06000E80 RID: 3712 RVA: 0x0003CC7A File Offset: 0x0003AE7A
		[DataSourceProperty]
		public InputKeyItemVM SellAllInputKey
		{
			get
			{
				return this._sellAllInputKey;
			}
			set
			{
				if (value != this._sellAllInputKey)
				{
					this._sellAllInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "SellAllInputKey");
				}
			}
		}

		// Token: 0x040005DD RID: 1501
		public bool DoNotSync;

		// Token: 0x040005DE RID: 1502
		private readonly Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

		// Token: 0x040005DF RID: 1503
		public bool IsFiveStackModifierActive;

		// Token: 0x040005E0 RID: 1504
		private readonly IViewDataTracker _viewDataTracker;

		// Token: 0x040005E1 RID: 1505
		public bool IsEntireStackModifierActive;

		// Token: 0x040005E2 RID: 1506
		private readonly int _donationMaxShareableXp;

		// Token: 0x040005E3 RID: 1507
		private readonly Stack<SPItemVM> _equipAfterTransferStack;

		// Token: 0x040005E4 RID: 1508
		private readonly TroopRoster _rightTroopRoster;

		// Token: 0x040005E5 RID: 1509
		private InventoryScreenHelper.InventoryMode _usageType = InventoryScreenHelper.InventoryMode.Trade;

		// Token: 0x040005E6 RID: 1510
		private bool _isTrading;

		// Token: 0x040005E7 RID: 1511
		private readonly TroopRoster _leftTroopRoster;

		// Token: 0x040005E8 RID: 1512
		private int _lastComparedItemIndex;

		// Token: 0x040005E9 RID: 1513
		private bool _isCharacterEquipmentDirty;

		// Token: 0x040005EA RID: 1514
		private int _currentInventoryCharacterIndex;

		// Token: 0x040005EB RID: 1515
		private string _selectedTooltipItemStringID = "";

		// Token: 0x040005EC RID: 1516
		private string _comparedTooltipItemStringID = "";

		// Token: 0x040005ED RID: 1517
		private InventoryLogic _inventoryLogic;

		// Token: 0x040005EE RID: 1518
		private CharacterObject _currentCharacter;

		// Token: 0x040005EF RID: 1519
		private SPItemVM _selectedItem;

		// Token: 0x040005F0 RID: 1520
		private List<ItemVM> _comparedItemList;

		// Token: 0x040005F1 RID: 1521
		private Func<string, TextObject> _getKeyTextFromKeyId;

		// Token: 0x040005F2 RID: 1522
		private List<string> _lockedItemIDs;

		// Token: 0x040005F3 RID: 1523
		private readonly List<int> _everyItemType = new List<int>
		{
			1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
			11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
			21, 22, 23, 24, 25, 26
		};

		// Token: 0x040005F4 RID: 1524
		private readonly List<int> _weaponItemTypes = new List<int> { 2, 3, 4 };

		// Token: 0x040005F5 RID: 1525
		private readonly List<int> _armorItemTypes = new List<int> { 14, 15, 16, 17, 23, 24 };

		// Token: 0x040005F6 RID: 1526
		private readonly List<int> _mountItemTypes = new List<int> { 1, 25 };

		// Token: 0x040005F7 RID: 1527
		private readonly List<int> _shieldAndRangedItemTypes = new List<int>
		{
			8, 5, 6, 7, 9, 10, 11, 12, 18, 19,
			20
		};

		// Token: 0x040005F8 RID: 1528
		private readonly List<int> _miscellaneousItemTypes = new List<int> { 13, 21, 22, 26 };

		// Token: 0x040005F9 RID: 1529
		private readonly Dictionary<SPInventoryVM.Filters, List<int>> _filters;

		// Token: 0x040005FA RID: 1530
		private int _selectedEquipmentIndex;

		// Token: 0x040005FB RID: 1531
		private bool _isFoodTransferButtonHighlightApplied;

		// Token: 0x040005FC RID: 1532
		private bool _isBannerItemsHighlightApplied;

		// Token: 0x040005FD RID: 1533
		private string _latestTutorialElementID;

		// Token: 0x040005FE RID: 1534
		private string _leftInventoryLabel;

		// Token: 0x040005FF RID: 1535
		private string _rightInventoryLabel;

		// Token: 0x04000600 RID: 1536
		private bool _otherSideHasCapacity;

		// Token: 0x04000601 RID: 1537
		private bool _isDoneDisabled;

		// Token: 0x04000602 RID: 1538
		private bool _isSearchAvailable;

		// Token: 0x04000603 RID: 1539
		private bool _isOtherInventoryGoldRelevant;

		// Token: 0x04000604 RID: 1540
		private string _doneLbl;

		// Token: 0x04000605 RID: 1541
		private string _cancelLbl;

		// Token: 0x04000606 RID: 1542
		private string _resetLbl;

		// Token: 0x04000607 RID: 1543
		private string _typeText;

		// Token: 0x04000608 RID: 1544
		private string _nameText;

		// Token: 0x04000609 RID: 1545
		private string _quantityText;

		// Token: 0x0400060A RID: 1546
		private string _costText;

		// Token: 0x0400060B RID: 1547
		private string _searchPlaceholderText;

		// Token: 0x0400060C RID: 1548
		private HintViewModel _resetHint;

		// Token: 0x0400060D RID: 1549
		private HintViewModel _filterAllHint;

		// Token: 0x0400060E RID: 1550
		private HintViewModel _filterWeaponHint;

		// Token: 0x0400060F RID: 1551
		private HintViewModel _filterArmorHint;

		// Token: 0x04000610 RID: 1552
		private HintViewModel _filterShieldAndRangedHint;

		// Token: 0x04000611 RID: 1553
		private HintViewModel _filterMountAndHarnessHint;

		// Token: 0x04000612 RID: 1554
		private HintViewModel _filterMiscHint;

		// Token: 0x04000613 RID: 1555
		private HintViewModel _stealthOutfitHint;

		// Token: 0x04000614 RID: 1556
		private HintViewModel _civilianOutfitHint;

		// Token: 0x04000615 RID: 1557
		private HintViewModel _battleOutfitHint;

		// Token: 0x04000616 RID: 1558
		private HintViewModel _equipmentHelmSlotHint;

		// Token: 0x04000617 RID: 1559
		private HintViewModel _equipmentArmorSlotHint;

		// Token: 0x04000618 RID: 1560
		private HintViewModel _equipmentBootSlotHint;

		// Token: 0x04000619 RID: 1561
		private HintViewModel _equipmentCloakSlotHint;

		// Token: 0x0400061A RID: 1562
		private HintViewModel _equipmentGloveSlotHint;

		// Token: 0x0400061B RID: 1563
		private HintViewModel _equipmentHarnessSlotHint;

		// Token: 0x0400061C RID: 1564
		private HintViewModel _equipmentMountSlotHint;

		// Token: 0x0400061D RID: 1565
		private HintViewModel _equipmentWeaponSlotHint;

		// Token: 0x0400061E RID: 1566
		private HintViewModel _equipmentBannerSlotHint;

		// Token: 0x0400061F RID: 1567
		private BasicTooltipViewModel _buyAllHint;

		// Token: 0x04000620 RID: 1568
		private BasicTooltipViewModel _sellAllHint;

		// Token: 0x04000621 RID: 1569
		private BasicTooltipViewModel _previousCharacterHint;

		// Token: 0x04000622 RID: 1570
		private BasicTooltipViewModel _nextCharacterHint;

		// Token: 0x04000623 RID: 1571
		private HintViewModel _weightHint;

		// Token: 0x04000624 RID: 1572
		private HintViewModel _armArmorHint;

		// Token: 0x04000625 RID: 1573
		private HintViewModel _bodyArmorHint;

		// Token: 0x04000626 RID: 1574
		private HintViewModel _headArmorHint;

		// Token: 0x04000627 RID: 1575
		private HintViewModel _legArmorHint;

		// Token: 0x04000628 RID: 1576
		private HintViewModel _horseArmorHint;

		// Token: 0x04000629 RID: 1577
		private HintViewModel _previewHint;

		// Token: 0x0400062A RID: 1578
		private HintViewModel _equipHint;

		// Token: 0x0400062B RID: 1579
		private HintViewModel _unequipHint;

		// Token: 0x0400062C RID: 1580
		private HintViewModel _sellHint;

		// Token: 0x0400062D RID: 1581
		private HintViewModel _playerSideCapacityExceededHint;

		// Token: 0x0400062E RID: 1582
		private HintViewModel _mainPartyLandCapacityExceededHint;

		// Token: 0x0400062F RID: 1583
		private HintViewModel _mainPartySeaCapacityExceededHint;

		// Token: 0x04000630 RID: 1584
		private HintViewModel _noSaddleHint;

		// Token: 0x04000631 RID: 1585
		private HintViewModel _donationLblHint;

		// Token: 0x04000632 RID: 1586
		private HintViewModel _otherSideCapacityExceededHint;

		// Token: 0x04000633 RID: 1587
		private BasicTooltipViewModel _totalWeightCarriedHint;

		// Token: 0x04000634 RID: 1588
		private BasicTooltipViewModel _landWeightHint;

		// Token: 0x04000635 RID: 1589
		private BasicTooltipViewModel _seaWeightHint;

		// Token: 0x04000636 RID: 1590
		private BasicTooltipViewModel _inventoryCapacityHint;

		// Token: 0x04000637 RID: 1591
		private BasicTooltipViewModel _landCapacityHint;

		// Token: 0x04000638 RID: 1592
		private BasicTooltipViewModel _seaCapacityHint;

		// Token: 0x04000639 RID: 1593
		private BasicTooltipViewModel _currentCharacterSkillsTooltip;

		// Token: 0x0400063A RID: 1594
		private BasicTooltipViewModel _productionTooltip;

		// Token: 0x0400063B RID: 1595
		private HeroViewModel _mainCharacter;

		// Token: 0x0400063C RID: 1596
		private bool _isExtendedEquipmentControlsEnabled;

		// Token: 0x0400063D RID: 1597
		private bool _isFocusedOnItemList;

		// Token: 0x0400063E RID: 1598
		private SPItemVM _currentFocusedItem;

		// Token: 0x0400063F RID: 1599
		private bool _equipAfterBuy;

		// Token: 0x04000640 RID: 1600
		private MBBindingList<SPItemVM> _leftItemListVM;

		// Token: 0x04000641 RID: 1601
		private MBBindingList<SPItemVM> _rightItemListVM;

		// Token: 0x04000642 RID: 1602
		private ItemMenuVM _itemMenu;

		// Token: 0x04000643 RID: 1603
		private SPItemVM _characterHelmSlot;

		// Token: 0x04000644 RID: 1604
		private SPItemVM _characterCloakSlot;

		// Token: 0x04000645 RID: 1605
		private SPItemVM _characterTorsoSlot;

		// Token: 0x04000646 RID: 1606
		private SPItemVM _characterGloveSlot;

		// Token: 0x04000647 RID: 1607
		private SPItemVM _characterBootSlot;

		// Token: 0x04000648 RID: 1608
		private SPItemVM _characterMountSlot;

		// Token: 0x04000649 RID: 1609
		private SPItemVM _characterMountArmorSlot;

		// Token: 0x0400064A RID: 1610
		private SPItemVM _characterWeapon1Slot;

		// Token: 0x0400064B RID: 1611
		private SPItemVM _characterWeapon2Slot;

		// Token: 0x0400064C RID: 1612
		private SPItemVM _characterWeapon3Slot;

		// Token: 0x0400064D RID: 1613
		private SPItemVM _characterWeapon4Slot;

		// Token: 0x0400064E RID: 1614
		private SPItemVM _characterBannerSlot;

		// Token: 0x0400064F RID: 1615
		private EquipmentIndex _targetEquipmentIndex = EquipmentIndex.None;

		// Token: 0x04000650 RID: 1616
		private int _transactionCount = -1;

		// Token: 0x04000651 RID: 1617
		private bool _isRefreshed;

		// Token: 0x04000652 RID: 1618
		private string _tradeLbl = "";

		// Token: 0x04000653 RID: 1619
		private string _experienceLbl = "";

		// Token: 0x04000654 RID: 1620
		private bool _hasGainedExperience;

		// Token: 0x04000655 RID: 1621
		private bool _isDonationXpGainExceedsMax;

		// Token: 0x04000656 RID: 1622
		private bool _noSaddleWarned;

		// Token: 0x04000657 RID: 1623
		private bool _isTradingWithSettlement;

		// Token: 0x04000658 RID: 1624
		private bool _showMainPartyLandCapacityTexts;

		// Token: 0x04000659 RID: 1625
		private bool _showMainPartySeaCapacityTexts;

		// Token: 0x0400065A RID: 1626
		private string _otherEquipmentCountText;

		// Token: 0x0400065B RID: 1627
		private string _mainPartyTotalWeightCarriedText;

		// Token: 0x0400065C RID: 1628
		private string _mainPartyLandWeightText;

		// Token: 0x0400065D RID: 1629
		private string _mainPartySeaWeightText;

		// Token: 0x0400065E RID: 1630
		private string _mainPartyInventoryCapacityText;

		// Token: 0x0400065F RID: 1631
		private bool _otherEquipmentCapacityExceededWarning;

		// Token: 0x04000660 RID: 1632
		private bool _otherEquipmentCountWarned;

		// Token: 0x04000661 RID: 1633
		private bool _playerEquipmentCountWarned;

		// Token: 0x04000662 RID: 1634
		private string _mainPartyLandCapacityText;

		// Token: 0x04000663 RID: 1635
		private string _mainPartySeaCapacityText;

		// Token: 0x04000664 RID: 1636
		private string _noSaddleText;

		// Token: 0x04000665 RID: 1637
		private string _leftSearchText = "";

		// Token: 0x04000666 RID: 1638
		private bool _isMainPartyLandCapacityWarned;

		// Token: 0x04000667 RID: 1639
		private bool _isMainPartySeaCapacityWarned;

		// Token: 0x04000668 RID: 1640
		private bool _showMainPartyLandCapacityWarning;

		// Token: 0x04000669 RID: 1641
		private bool _showMainPartySeaCapacityWarning;

		// Token: 0x0400066A RID: 1642
		private string _playerSideCapacityExceededText;

		// Token: 0x0400066B RID: 1643
		private string _mainPartyLandCapacityExceededText;

		// Token: 0x0400066C RID: 1644
		private string _mainPartySeaCapacityExceededText;

		// Token: 0x0400066D RID: 1645
		private string _separatorText;

		// Token: 0x0400066E RID: 1646
		private string _otherSideCapacityExceededText;

		// Token: 0x0400066F RID: 1647
		private string _rightSearchText = "";

		// Token: 0x04000670 RID: 1648
		private string _bannerTypeName;

		// Token: 0x04000671 RID: 1649
		private SPInventoryVM.EquipmentModes _equipmentMode = SPInventoryVM.EquipmentModes.Battle;

		// Token: 0x04000672 RID: 1650
		private bool _companionExists;

		// Token: 0x04000673 RID: 1651
		private SPInventoryVM.Filters _activeFilterIndex;

		// Token: 0x04000674 RID: 1652
		private bool _isMicsFilterHighlightEnabled;

		// Token: 0x04000675 RID: 1653
		private bool _isCivilianFilterHighlightEnabled;

		// Token: 0x04000676 RID: 1654
		private ItemPreviewVM _itemPreview;

		// Token: 0x04000677 RID: 1655
		private SelectorVM<InventoryCharacterSelectorItemVM> _characterList;

		// Token: 0x04000678 RID: 1656
		private SPInventorySortControllerVM _otherInventorySortController;

		// Token: 0x04000679 RID: 1657
		private SPInventorySortControllerVM _playerInventorySortController;

		// Token: 0x0400067A RID: 1658
		private bool _scrollToItem;

		// Token: 0x0400067B RID: 1659
		private string _scrollItemId;

		// Token: 0x0400067C RID: 1660
		private bool _isBattleMode = true;

		// Token: 0x0400067D RID: 1661
		private bool _isStealthMode;

		// Token: 0x0400067E RID: 1662
		private bool _isCivilianMode;

		// Token: 0x0400067F RID: 1663
		private string _leftInventoryOwnerName;

		// Token: 0x04000680 RID: 1664
		private int _leftInventoryOwnerGold;

		// Token: 0x04000681 RID: 1665
		private string _rightInventoryOwnerName;

		// Token: 0x04000682 RID: 1666
		private string _currentCharacterName;

		// Token: 0x04000683 RID: 1667
		private int _rightInventoryOwnerGold;

		// Token: 0x04000684 RID: 1668
		private int _itemCountToBuy;

		// Token: 0x04000685 RID: 1669
		private float _currentCharacterArmArmor;

		// Token: 0x04000686 RID: 1670
		private float _currentCharacterBodyArmor;

		// Token: 0x04000687 RID: 1671
		private float _currentCharacterHeadArmor;

		// Token: 0x04000688 RID: 1672
		private float _currentCharacterLegArmor;

		// Token: 0x04000689 RID: 1673
		private float _currentCharacterHorseArmor;

		// Token: 0x0400068A RID: 1674
		private string _currentCharacterTotalEncumbrance;

		// Token: 0x0400068B RID: 1675
		private InputKeyItemVM _resetInputKey;

		// Token: 0x0400068C RID: 1676
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x0400068D RID: 1677
		private InputKeyItemVM _doneInputKey;

		// Token: 0x0400068E RID: 1678
		private InputKeyItemVM _previousCharacterInputKey;

		// Token: 0x0400068F RID: 1679
		private InputKeyItemVM _nextCharacterInputKey;

		// Token: 0x04000690 RID: 1680
		private InputKeyItemVM _buyAllInputKey;

		// Token: 0x04000691 RID: 1681
		private InputKeyItemVM _sellAllInputKey;

		// Token: 0x02000200 RID: 512
		public enum EquipmentModes
		{
			// Token: 0x04001161 RID: 4449
			Civilian,
			// Token: 0x04001162 RID: 4450
			Battle,
			// Token: 0x04001163 RID: 4451
			Stealth
		}

		// Token: 0x02000201 RID: 513
		public enum Filters
		{
			// Token: 0x04001165 RID: 4453
			All,
			// Token: 0x04001166 RID: 4454
			Weapons,
			// Token: 0x04001167 RID: 4455
			ShieldsAndRanged,
			// Token: 0x04001168 RID: 4456
			Armors,
			// Token: 0x04001169 RID: 4457
			Mounts,
			// Token: 0x0400116A RID: 4458
			Miscellaneous
		}

		// Token: 0x02000202 RID: 514
		private class RosterElementComparer : IComparer<SPItemVM>
		{
			// Token: 0x060023F9 RID: 9209 RVA: 0x0007ED58 File Offset: 0x0007CF58
			public RosterElementComparer(InventoryCapacityModel inventoryCapacityModel, MobileParty currentParty, bool basedOnGoldAmount)
			{
				this._inventoryCapacityModel = inventoryCapacityModel;
				this._currentParty = currentParty;
				this._basedOnGoldAmount = basedOnGoldAmount;
			}

			// Token: 0x060023FA RID: 9210 RVA: 0x0007ED78 File Offset: 0x0007CF78
			public int Compare(SPItemVM x, SPItemVM y)
			{
				EquipmentElement equipmentElement = x.ItemRosterElement.EquipmentElement;
				EquipmentElement equipmentElement2 = y.ItemRosterElement.EquipmentElement;
				if (this._currentParty != null)
				{
					TextObject textObject;
					TextObject textObject2;
					int num = this._inventoryCapacityModel.GetItemEffectiveWeight(equipmentElement, this._currentParty, this._currentParty.IsCurrentlyAtSea, out textObject).CompareTo(this._inventoryCapacityModel.GetItemEffectiveWeight(equipmentElement2, this._currentParty, this._currentParty.IsCurrentlyAtSea, out textObject2));
					if (num != 0)
					{
						return num;
					}
					return x.ItemCost.CompareTo(y.ItemCost);
				}
				else
				{
					if (this._basedOnGoldAmount)
					{
						return x.ItemCost.CompareTo(y.ItemCost);
					}
					return 0;
				}
			}

			// Token: 0x0400116B RID: 4459
			private readonly MobileParty _currentParty;

			// Token: 0x0400116C RID: 4460
			private readonly InventoryCapacityModel _inventoryCapacityModel;

			// Token: 0x0400116D RID: 4461
			private readonly bool _basedOnGoldAmount;
		}
	}
}
