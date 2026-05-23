using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x0200010A RID: 266
	public class WeaponDesignResultPopupVM : ViewModel
	{
		// Token: 0x0600179B RID: 6043 RVA: 0x00059FEC File Offset: 0x000581EC
		public WeaponDesignResultPopupVM(ItemObject craftedItem, TextObject itemName, Action onFinalize, Crafting crafting, CraftingOrder completedOrder, ItemCollectionElementViewModel itemVisualModel, MBBindingList<ItemFlagVM> weaponFlagIconsList, Func<CraftingSecondaryUsageItemVM, MBBindingList<WeaponDesignResultPropertyItemVM>> onGetPropertyList, Action<CraftingSecondaryUsageItemVM> onUsageSelected)
		{
			this._craftedItem = craftedItem;
			this._onFinalize = onFinalize;
			this._crafting = crafting;
			this._completedOrder = completedOrder;
			this._craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			this._onUsageSelected = onUsageSelected;
			this.SecondaryUsageSelector = new SelectorVM<CraftingSecondaryUsageItemVM>(new List<string>(), -1, new Action<SelectorVM<CraftingSecondaryUsageItemVM>>(this.OnUsageSelected));
			this.WeaponFlagIconsList = weaponFlagIconsList;
			this._onGetPropertyList = onGetPropertyList;
			ItemModifier currentItemModifier = this._craftingBehavior.GetCurrentItemModifier();
			if (currentItemModifier != null)
			{
				TextObject textObject = currentItemModifier.Name.CopyTextObject();
				textObject.SetTextVariable("ITEMNAME", itemName.ToString());
				this.ItemName = textObject.ToString();
			}
			else
			{
				this.ItemName = itemName.ToString();
			}
			this.ItemName = this.ItemName.Trim();
			this.ItemVisualModel = itemVisualModel;
			Game game = Game.Current;
			if (game != null)
			{
				game.EventManager.TriggerEvent<CraftingWeaponResultPopupToggledEvent>(new CraftingWeaponResultPopupToggledEvent(true));
			}
			this.RefreshValues();
		}

		// Token: 0x0600179C RID: 6044 RVA: 0x0005A0E4 File Offset: 0x000582E4
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.IsInOrderMode = this._completedOrder != null;
			this.WeaponCraftedText = new TextObject("{=0mqdFC2x}Weapon Crafted!", null).ToString();
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			this.RefreshUsages();
		}

		// Token: 0x0600179D RID: 6045 RVA: 0x0005A138 File Offset: 0x00058338
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.DoneInputKey.OnFinalize();
		}

		// Token: 0x0600179E RID: 6046 RVA: 0x0005A14C File Offset: 0x0005834C
		private void RefreshUsages()
		{
			this.SecondaryUsageSelector.ItemList.Clear();
			MBReadOnlyList<WeaponComponentData> weapons = this._crafting.GetCurrentCraftedItemObject(false, null).Weapons;
			int num = this.SecondaryUsageSelector.SelectedIndex;
			int num2 = 0;
			for (int i = 0; i < weapons.Count; i++)
			{
				if (CampaignUIHelper.IsItemUsageApplicable(weapons[i]))
				{
					TextObject name = GameTexts.FindText("str_weapon_usage", weapons[i].WeaponDescriptionId);
					this.SecondaryUsageSelector.AddItem(new CraftingSecondaryUsageItemVM(name, num2, i, this.SecondaryUsageSelector));
					if (this.IsInOrderMode)
					{
						WeaponComponentData orderWeapon = this._completedOrder.GetStatWeapon();
						num = this._crafting.GetCurrentCraftedItemObject(false, null).Weapons.FindIndex((WeaponComponentData x) => x.WeaponDescriptionId == orderWeapon.WeaponDescriptionId);
					}
					else
					{
						CraftingOrder completedOrder = this._completedOrder;
						if (((completedOrder != null) ? completedOrder.GetStatWeapon().WeaponDescriptionId : null) == weapons[i].WeaponDescriptionId)
						{
							num = num2;
						}
					}
					num2++;
				}
			}
			this.SecondaryUsageSelector.SelectedIndex = ((num >= 0) ? num : 0);
		}

		// Token: 0x0600179F RID: 6047 RVA: 0x0005A270 File Offset: 0x00058470
		private void OnUsageSelected(SelectorVM<CraftingSecondaryUsageItemVM> selector)
		{
			Func<CraftingSecondaryUsageItemVM, MBBindingList<WeaponDesignResultPropertyItemVM>> onGetPropertyList = this._onGetPropertyList;
			this.DesignResultPropertyList = ((onGetPropertyList != null) ? onGetPropertyList(selector.SelectedItem) : null);
			if (this._isInOrderMode)
			{
				bool isOrderSuccessful;
				TextObject textObject;
				TextObject textObject2;
				int craftedWeaponFinalWorth;
				this._craftingBehavior.GetOrderResult(this._completedOrder, this._craftedItem, out isOrderSuccessful, out textObject, out textObject2, out craftedWeaponFinalWorth);
				this.CraftedWeaponInitialWorth = this._completedOrder.BaseGoldReward;
				this.CraftedWeaponFinalWorth = craftedWeaponFinalWorth;
				this.IsOrderSuccessful = isOrderSuccessful;
				this.CraftedWeaponWorthText = new TextObject("{=ZIn8W5ZG}Worth", null).ToString();
				this.DesignResultPropertyList.Add(new WeaponDesignResultPropertyItemVM(new TextObject("{=QmfZjCo1}Worth: ", null), (float)this.CraftedWeaponInitialWorth, (float)this.CraftedWeaponInitialWorth, (float)(this.CraftedWeaponFinalWorth - this.CraftedWeaponInitialWorth), false, true, false));
				this.OrderOwnerRemarkText = textObject.ToString();
				this.OrderResultText = textObject2.ToString();
			}
		}

		// Token: 0x060017A0 RID: 6048 RVA: 0x0005A34C File Offset: 0x0005854C
		private void UpdateConfirmAvailability()
		{
			if (this.IsInOrderMode)
			{
				this.CanConfirm = true;
				this.ConfirmDisabledReasonHint = new HintViewModel();
				return;
			}
			Tuple<bool, TextObject> tuple = CampaignUIHelper.IsStringApplicableForItemName(this.ItemName);
			this.CanConfirm = tuple.Item1;
			this.ConfirmDisabledReasonHint = new HintViewModel(tuple.Item2, null);
		}

		// Token: 0x060017A1 RID: 6049 RVA: 0x0005A3A0 File Offset: 0x000585A0
		public void ExecuteFinalizeCrafting()
		{
			TextObject textObject = new TextObject("{=!}" + this.ItemName, null);
			this._crafting.SetCraftedWeaponName(textObject);
			this._craftingBehavior.SetCraftedWeaponName(this._craftedItem, textObject);
			Action onFinalize = this._onFinalize;
			if (onFinalize != null)
			{
				onFinalize();
			}
			Game game = Game.Current;
			if (game != null)
			{
				game.EventManager.TriggerEvent<CraftingWeaponResultPopupToggledEvent>(new CraftingWeaponResultPopupToggledEvent(false));
			}
			if (!this._isInOrderMode)
			{
				TextObject textObject2 = GameTexts.FindText("crafting_added_to_inventory", null);
				textObject2.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, false);
				textObject2.SetTextVariable("ITEM_NAME", this.ItemName);
				MBInformationManager.AddQuickInformation(textObject2, 0, null, null, "");
			}
		}

		// Token: 0x060017A2 RID: 6050 RVA: 0x0005A456 File Offset: 0x00058656
		public void ExecuteRandomCraftName()
		{
			this.ItemName = this._crafting.GetRandomCraftName().ToString();
		}

		// Token: 0x170007E1 RID: 2017
		// (get) Token: 0x060017A3 RID: 6051 RVA: 0x0005A46E File Offset: 0x0005866E
		// (set) Token: 0x060017A4 RID: 6052 RVA: 0x0005A476 File Offset: 0x00058676
		[DataSourceProperty]
		public MBBindingList<ItemFlagVM> WeaponFlagIconsList
		{
			get
			{
				return this._weaponFlagIconsList;
			}
			set
			{
				if (value != this._weaponFlagIconsList)
				{
					this._weaponFlagIconsList = value;
					base.OnPropertyChangedWithValue<MBBindingList<ItemFlagVM>>(value, "WeaponFlagIconsList");
				}
			}
		}

		// Token: 0x170007E2 RID: 2018
		// (get) Token: 0x060017A5 RID: 6053 RVA: 0x0005A494 File Offset: 0x00058694
		// (set) Token: 0x060017A6 RID: 6054 RVA: 0x0005A49C File Offset: 0x0005869C
		[DataSourceProperty]
		public bool IsInOrderMode
		{
			get
			{
				return this._isInOrderMode;
			}
			set
			{
				if (value != this._isInOrderMode)
				{
					this._isInOrderMode = value;
					base.OnPropertyChangedWithValue(value, "IsInOrderMode");
				}
			}
		}

		// Token: 0x170007E3 RID: 2019
		// (get) Token: 0x060017A7 RID: 6055 RVA: 0x0005A4BA File Offset: 0x000586BA
		// (set) Token: 0x060017A8 RID: 6056 RVA: 0x0005A4C2 File Offset: 0x000586C2
		[DataSourceProperty]
		public int CraftedWeaponFinalWorth
		{
			get
			{
				return this._craftedWeaponFinalWorth;
			}
			set
			{
				if (value != this._craftedWeaponFinalWorth)
				{
					this._craftedWeaponFinalWorth = value;
					base.OnPropertyChangedWithValue(value, "CraftedWeaponFinalWorth");
				}
			}
		}

		// Token: 0x170007E4 RID: 2020
		// (get) Token: 0x060017A9 RID: 6057 RVA: 0x0005A4E0 File Offset: 0x000586E0
		// (set) Token: 0x060017AA RID: 6058 RVA: 0x0005A4E8 File Offset: 0x000586E8
		[DataSourceProperty]
		public int CraftedWeaponPriceDifference
		{
			get
			{
				return this._craftedWeaponPriceDifference;
			}
			set
			{
				if (value != this._craftedWeaponPriceDifference)
				{
					this._craftedWeaponPriceDifference = value;
					base.OnPropertyChangedWithValue(value, "CraftedWeaponPriceDifference");
				}
			}
		}

		// Token: 0x170007E5 RID: 2021
		// (get) Token: 0x060017AB RID: 6059 RVA: 0x0005A506 File Offset: 0x00058706
		// (set) Token: 0x060017AC RID: 6060 RVA: 0x0005A50E File Offset: 0x0005870E
		[DataSourceProperty]
		public int CraftedWeaponInitialWorth
		{
			get
			{
				return this._craftedWeaponInitialWorth;
			}
			set
			{
				if (value != this._craftedWeaponInitialWorth)
				{
					this._craftedWeaponInitialWorth = value;
					base.OnPropertyChangedWithValue(value, "CraftedWeaponInitialWorth");
				}
			}
		}

		// Token: 0x170007E6 RID: 2022
		// (get) Token: 0x060017AD RID: 6061 RVA: 0x0005A52C File Offset: 0x0005872C
		// (set) Token: 0x060017AE RID: 6062 RVA: 0x0005A534 File Offset: 0x00058734
		[DataSourceProperty]
		public string CraftedWeaponWorthText
		{
			get
			{
				return this._craftedWeaponWorthText;
			}
			set
			{
				if (value != this._craftedWeaponWorthText)
				{
					this._craftedWeaponWorthText = value;
					base.OnPropertyChangedWithValue<string>(value, "CraftedWeaponWorthText");
				}
			}
		}

		// Token: 0x170007E7 RID: 2023
		// (get) Token: 0x060017AF RID: 6063 RVA: 0x0005A557 File Offset: 0x00058757
		// (set) Token: 0x060017B0 RID: 6064 RVA: 0x0005A55F File Offset: 0x0005875F
		[DataSourceProperty]
		public bool IsOrderSuccessful
		{
			get
			{
				return this._isOrderSuccessful;
			}
			set
			{
				if (value != this._isOrderSuccessful)
				{
					this._isOrderSuccessful = value;
					base.OnPropertyChangedWithValue(value, "IsOrderSuccessful");
				}
			}
		}

		// Token: 0x170007E8 RID: 2024
		// (get) Token: 0x060017B1 RID: 6065 RVA: 0x0005A57D File Offset: 0x0005877D
		// (set) Token: 0x060017B2 RID: 6066 RVA: 0x0005A585 File Offset: 0x00058785
		[DataSourceProperty]
		public bool CanConfirm
		{
			get
			{
				return this._canConfirm;
			}
			set
			{
				if (value != this._canConfirm)
				{
					this._canConfirm = value;
					base.OnPropertyChangedWithValue(value, "CanConfirm");
				}
			}
		}

		// Token: 0x170007E9 RID: 2025
		// (get) Token: 0x060017B3 RID: 6067 RVA: 0x0005A5A3 File Offset: 0x000587A3
		// (set) Token: 0x060017B4 RID: 6068 RVA: 0x0005A5AB File Offset: 0x000587AB
		[DataSourceProperty]
		public string OrderResultText
		{
			get
			{
				return this._orderResultText;
			}
			set
			{
				if (value != this._orderResultText)
				{
					this._orderResultText = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderResultText");
				}
			}
		}

		// Token: 0x170007EA RID: 2026
		// (get) Token: 0x060017B5 RID: 6069 RVA: 0x0005A5CE File Offset: 0x000587CE
		// (set) Token: 0x060017B6 RID: 6070 RVA: 0x0005A5D6 File Offset: 0x000587D6
		[DataSourceProperty]
		public string OrderOwnerRemarkText
		{
			get
			{
				return this._orderOwnerRemarkText;
			}
			set
			{
				if (value != this._orderOwnerRemarkText)
				{
					this._orderOwnerRemarkText = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderOwnerRemarkText");
				}
			}
		}

		// Token: 0x170007EB RID: 2027
		// (get) Token: 0x060017B7 RID: 6071 RVA: 0x0005A5F9 File Offset: 0x000587F9
		// (set) Token: 0x060017B8 RID: 6072 RVA: 0x0005A601 File Offset: 0x00058801
		[DataSourceProperty]
		public string WeaponCraftedText
		{
			get
			{
				return this._weaponCraftedText;
			}
			set
			{
				if (value != this._weaponCraftedText)
				{
					this._weaponCraftedText = value;
					base.OnPropertyChangedWithValue<string>(value, "WeaponCraftedText");
				}
			}
		}

		// Token: 0x170007EC RID: 2028
		// (get) Token: 0x060017B9 RID: 6073 RVA: 0x0005A624 File Offset: 0x00058824
		// (set) Token: 0x060017BA RID: 6074 RVA: 0x0005A62C File Offset: 0x0005882C
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

		// Token: 0x170007ED RID: 2029
		// (get) Token: 0x060017BB RID: 6075 RVA: 0x0005A64F File Offset: 0x0005884F
		// (set) Token: 0x060017BC RID: 6076 RVA: 0x0005A657 File Offset: 0x00058857
		[DataSourceProperty]
		public MBBindingList<WeaponDesignResultPropertyItemVM> DesignResultPropertyList
		{
			get
			{
				return this._designResultPropertyList;
			}
			set
			{
				if (value != this._designResultPropertyList)
				{
					this._designResultPropertyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<WeaponDesignResultPropertyItemVM>>(value, "DesignResultPropertyList");
				}
			}
		}

		// Token: 0x170007EE RID: 2030
		// (get) Token: 0x060017BD RID: 6077 RVA: 0x0005A675 File Offset: 0x00058875
		// (set) Token: 0x060017BE RID: 6078 RVA: 0x0005A67D File Offset: 0x0005887D
		[DataSourceProperty]
		public string ItemName
		{
			get
			{
				return this._itemName;
			}
			set
			{
				if (value != this._itemName)
				{
					this._itemName = value;
					this.UpdateConfirmAvailability();
					base.OnPropertyChangedWithValue<string>(value, "ItemName");
				}
			}
		}

		// Token: 0x170007EF RID: 2031
		// (get) Token: 0x060017BF RID: 6079 RVA: 0x0005A6A6 File Offset: 0x000588A6
		// (set) Token: 0x060017C0 RID: 6080 RVA: 0x0005A6AE File Offset: 0x000588AE
		[DataSourceProperty]
		public ItemCollectionElementViewModel ItemVisualModel
		{
			get
			{
				return this._itemVisualModel;
			}
			set
			{
				if (value != this._itemVisualModel)
				{
					this._itemVisualModel = value;
					base.OnPropertyChangedWithValue<ItemCollectionElementViewModel>(value, "ItemVisualModel");
				}
			}
		}

		// Token: 0x170007F0 RID: 2032
		// (get) Token: 0x060017C1 RID: 6081 RVA: 0x0005A6CC File Offset: 0x000588CC
		// (set) Token: 0x060017C2 RID: 6082 RVA: 0x0005A6D4 File Offset: 0x000588D4
		[DataSourceProperty]
		public HintViewModel ConfirmDisabledReasonHint
		{
			get
			{
				return this._confirmDisabledReasonHint;
			}
			set
			{
				if (value != this._confirmDisabledReasonHint)
				{
					this._confirmDisabledReasonHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ConfirmDisabledReasonHint");
				}
			}
		}

		// Token: 0x170007F1 RID: 2033
		// (get) Token: 0x060017C3 RID: 6083 RVA: 0x0005A6F2 File Offset: 0x000588F2
		// (set) Token: 0x060017C4 RID: 6084 RVA: 0x0005A6FA File Offset: 0x000588FA
		[DataSourceProperty]
		public SelectorVM<CraftingSecondaryUsageItemVM> SecondaryUsageSelector
		{
			get
			{
				return this._secondaryUsageSelector;
			}
			set
			{
				if (value != this._secondaryUsageSelector)
				{
					this._secondaryUsageSelector = value;
					base.OnPropertyChangedWithValue<SelectorVM<CraftingSecondaryUsageItemVM>>(value, "SecondaryUsageSelector");
				}
			}
		}

		// Token: 0x060017C5 RID: 6085 RVA: 0x0005A718 File Offset: 0x00058918
		public void SetDoneInputKey(HotKey hotkey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x170007F2 RID: 2034
		// (get) Token: 0x060017C6 RID: 6086 RVA: 0x0005A727 File Offset: 0x00058927
		// (set) Token: 0x060017C7 RID: 6087 RVA: 0x0005A72F File Offset: 0x0005892F
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

		// Token: 0x04000AD1 RID: 2769
		private readonly Action<CraftingSecondaryUsageItemVM> _onUsageSelected;

		// Token: 0x04000AD2 RID: 2770
		private readonly Func<CraftingSecondaryUsageItemVM, MBBindingList<WeaponDesignResultPropertyItemVM>> _onGetPropertyList;

		// Token: 0x04000AD3 RID: 2771
		private readonly Action _onFinalize;

		// Token: 0x04000AD4 RID: 2772
		private readonly Crafting _crafting;

		// Token: 0x04000AD5 RID: 2773
		private readonly CraftingOrder _completedOrder;

		// Token: 0x04000AD6 RID: 2774
		private readonly ItemObject _craftedItem;

		// Token: 0x04000AD7 RID: 2775
		private readonly ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000AD8 RID: 2776
		private MBBindingList<ItemFlagVM> _weaponFlagIconsList;

		// Token: 0x04000AD9 RID: 2777
		private bool _isInOrderMode;

		// Token: 0x04000ADA RID: 2778
		private string _orderResultText;

		// Token: 0x04000ADB RID: 2779
		private string _orderOwnerRemarkText;

		// Token: 0x04000ADC RID: 2780
		private bool _isOrderSuccessful;

		// Token: 0x04000ADD RID: 2781
		private bool _canConfirm;

		// Token: 0x04000ADE RID: 2782
		private string _craftedWeaponWorthText;

		// Token: 0x04000ADF RID: 2783
		private int _craftedWeaponInitialWorth;

		// Token: 0x04000AE0 RID: 2784
		private int _craftedWeaponPriceDifference;

		// Token: 0x04000AE1 RID: 2785
		private int _craftedWeaponFinalWorth;

		// Token: 0x04000AE2 RID: 2786
		private string _weaponCraftedText;

		// Token: 0x04000AE3 RID: 2787
		private string _doneLbl;

		// Token: 0x04000AE4 RID: 2788
		private MBBindingList<WeaponDesignResultPropertyItemVM> _designResultPropertyList;

		// Token: 0x04000AE5 RID: 2789
		private string _itemName;

		// Token: 0x04000AE6 RID: 2790
		private ItemCollectionElementViewModel _itemVisualModel;

		// Token: 0x04000AE7 RID: 2791
		private HintViewModel _confirmDisabledReasonHint;

		// Token: 0x04000AE8 RID: 2792
		private SelectorVM<CraftingSecondaryUsageItemVM> _secondaryUsageSelector;

		// Token: 0x04000AE9 RID: 2793
		private InputKeyItemVM _doneInputKey;
	}
}
