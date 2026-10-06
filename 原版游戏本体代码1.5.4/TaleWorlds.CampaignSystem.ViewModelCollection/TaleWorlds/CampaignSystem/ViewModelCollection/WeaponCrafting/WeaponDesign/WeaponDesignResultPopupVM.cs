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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class WeaponDesignResultPopupVM : ViewModel
{
	private readonly Action<CraftingSecondaryUsageItemVM> _onUsageSelected;

	private readonly Func<CraftingSecondaryUsageItemVM, MBBindingList<WeaponDesignResultPropertyItemVM>> _onGetPropertyList;

	private readonly Action _onFinalize;

	private readonly Crafting _crafting;

	private readonly CraftingOrder _completedOrder;

	private readonly ItemObject _craftedItem;

	private readonly ICraftingCampaignBehavior _craftingBehavior;

	private MBBindingList<ItemFlagVM> _weaponFlagIconsList;

	private bool _isInOrderMode;

	private string _orderResultText;

	private string _orderOwnerRemarkText;

	private bool _isOrderSuccessful;

	private bool _canConfirm;

	private string _craftedWeaponWorthText;

	private int _craftedWeaponInitialWorth;

	private int _craftedWeaponPriceDifference;

	private int _craftedWeaponFinalWorth;

	private string _weaponCraftedText;

	private string _doneLbl;

	private MBBindingList<WeaponDesignResultPropertyItemVM> _designResultPropertyList;

	private string _itemName;

	private ItemCollectionElementViewModel _itemVisualModel;

	private HintViewModel _confirmDisabledReasonHint;

	private SelectorVM<CraftingSecondaryUsageItemVM> _secondaryUsageSelector;

	private InputKeyItemVM _doneInputKey;

	[DataSourceProperty]
	public MBBindingList<ItemFlagVM> WeaponFlagIconsList
	{
		get
		{
			return _weaponFlagIconsList;
		}
		set
		{
			if (value != _weaponFlagIconsList)
			{
				_weaponFlagIconsList = value;
				OnPropertyChangedWithValue(value, "WeaponFlagIconsList");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInOrderMode
	{
		get
		{
			return _isInOrderMode;
		}
		set
		{
			if (value != _isInOrderMode)
			{
				_isInOrderMode = value;
				OnPropertyChangedWithValue(value, "IsInOrderMode");
			}
		}
	}

	[DataSourceProperty]
	public int CraftedWeaponFinalWorth
	{
		get
		{
			return _craftedWeaponFinalWorth;
		}
		set
		{
			if (value != _craftedWeaponFinalWorth)
			{
				_craftedWeaponFinalWorth = value;
				OnPropertyChangedWithValue(value, "CraftedWeaponFinalWorth");
			}
		}
	}

	[DataSourceProperty]
	public int CraftedWeaponPriceDifference
	{
		get
		{
			return _craftedWeaponPriceDifference;
		}
		set
		{
			if (value != _craftedWeaponPriceDifference)
			{
				_craftedWeaponPriceDifference = value;
				OnPropertyChangedWithValue(value, "CraftedWeaponPriceDifference");
			}
		}
	}

	[DataSourceProperty]
	public int CraftedWeaponInitialWorth
	{
		get
		{
			return _craftedWeaponInitialWorth;
		}
		set
		{
			if (value != _craftedWeaponInitialWorth)
			{
				_craftedWeaponInitialWorth = value;
				OnPropertyChangedWithValue(value, "CraftedWeaponInitialWorth");
			}
		}
	}

	[DataSourceProperty]
	public string CraftedWeaponWorthText
	{
		get
		{
			return _craftedWeaponWorthText;
		}
		set
		{
			if (value != _craftedWeaponWorthText)
			{
				_craftedWeaponWorthText = value;
				OnPropertyChangedWithValue(value, "CraftedWeaponWorthText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOrderSuccessful
	{
		get
		{
			return _isOrderSuccessful;
		}
		set
		{
			if (value != _isOrderSuccessful)
			{
				_isOrderSuccessful = value;
				OnPropertyChangedWithValue(value, "IsOrderSuccessful");
			}
		}
	}

	[DataSourceProperty]
	public bool CanConfirm
	{
		get
		{
			return _canConfirm;
		}
		set
		{
			if (value != _canConfirm)
			{
				_canConfirm = value;
				OnPropertyChangedWithValue(value, "CanConfirm");
			}
		}
	}

	[DataSourceProperty]
	public string OrderResultText
	{
		get
		{
			return _orderResultText;
		}
		set
		{
			if (value != _orderResultText)
			{
				_orderResultText = value;
				OnPropertyChangedWithValue(value, "OrderResultText");
			}
		}
	}

	[DataSourceProperty]
	public string OrderOwnerRemarkText
	{
		get
		{
			return _orderOwnerRemarkText;
		}
		set
		{
			if (value != _orderOwnerRemarkText)
			{
				_orderOwnerRemarkText = value;
				OnPropertyChangedWithValue(value, "OrderOwnerRemarkText");
			}
		}
	}

	[DataSourceProperty]
	public string WeaponCraftedText
	{
		get
		{
			return _weaponCraftedText;
		}
		set
		{
			if (value != _weaponCraftedText)
			{
				_weaponCraftedText = value;
				OnPropertyChangedWithValue(value, "WeaponCraftedText");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<WeaponDesignResultPropertyItemVM> DesignResultPropertyList
	{
		get
		{
			return _designResultPropertyList;
		}
		set
		{
			if (value != _designResultPropertyList)
			{
				_designResultPropertyList = value;
				OnPropertyChangedWithValue(value, "DesignResultPropertyList");
			}
		}
	}

	[DataSourceProperty]
	public string ItemName
	{
		get
		{
			return _itemName;
		}
		set
		{
			if (value != _itemName)
			{
				_itemName = value;
				UpdateConfirmAvailability();
				OnPropertyChangedWithValue(value, "ItemName");
			}
		}
	}

	[DataSourceProperty]
	public ItemCollectionElementViewModel ItemVisualModel
	{
		get
		{
			return _itemVisualModel;
		}
		set
		{
			if (value != _itemVisualModel)
			{
				_itemVisualModel = value;
				OnPropertyChangedWithValue(value, "ItemVisualModel");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ConfirmDisabledReasonHint
	{
		get
		{
			return _confirmDisabledReasonHint;
		}
		set
		{
			if (value != _confirmDisabledReasonHint)
			{
				_confirmDisabledReasonHint = value;
				OnPropertyChangedWithValue(value, "ConfirmDisabledReasonHint");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<CraftingSecondaryUsageItemVM> SecondaryUsageSelector
	{
		get
		{
			return _secondaryUsageSelector;
		}
		set
		{
			if (value != _secondaryUsageSelector)
			{
				_secondaryUsageSelector = value;
				OnPropertyChangedWithValue(value, "SecondaryUsageSelector");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	public WeaponDesignResultPopupVM(ItemObject craftedItem, TextObject itemName, Action onFinalize, Crafting crafting, CraftingOrder completedOrder, ItemCollectionElementViewModel itemVisualModel, MBBindingList<ItemFlagVM> weaponFlagIconsList, Func<CraftingSecondaryUsageItemVM, MBBindingList<WeaponDesignResultPropertyItemVM>> onGetPropertyList, Action<CraftingSecondaryUsageItemVM> onUsageSelected)
	{
		_craftedItem = craftedItem;
		_onFinalize = onFinalize;
		_crafting = crafting;
		_completedOrder = completedOrder;
		_craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		_onUsageSelected = onUsageSelected;
		SecondaryUsageSelector = new SelectorVM<CraftingSecondaryUsageItemVM>(new List<string>(), -1, OnUsageSelected);
		WeaponFlagIconsList = weaponFlagIconsList;
		_onGetPropertyList = onGetPropertyList;
		ItemModifier currentItemModifier = _craftingBehavior.GetCurrentItemModifier();
		if (currentItemModifier != null)
		{
			TextObject textObject = currentItemModifier.Name.CopyTextObject();
			textObject.SetTextVariable("ITEMNAME", itemName.ToString());
			ItemName = textObject.ToString();
		}
		else
		{
			ItemName = itemName.ToString();
		}
		ItemName = ItemName.Trim();
		ItemVisualModel = itemVisualModel;
		Game.Current?.EventManager.TriggerEvent(new CraftingWeaponResultPopupToggledEvent(isOpen: true));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		IsInOrderMode = _completedOrder != null;
		WeaponCraftedText = new TextObject("{=0mqdFC2x}Weapon Crafted!").ToString();
		DoneLbl = GameTexts.FindText("str_done").ToString();
		RefreshUsages();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey.OnFinalize();
	}

	private void RefreshUsages()
	{
		SecondaryUsageSelector.ItemList.Clear();
		MBReadOnlyList<WeaponComponentData> weapons = _crafting.GetCurrentCraftedItemObject().Weapons;
		int num = SecondaryUsageSelector.SelectedIndex;
		int num2 = 0;
		for (int i = 0; i < weapons.Count; i++)
		{
			if (!CampaignUIHelper.IsItemUsageApplicable(weapons[i]))
			{
				continue;
			}
			TextObject name = GameTexts.FindText("str_weapon_usage", weapons[i].WeaponDescriptionId);
			SecondaryUsageSelector.AddItem(new CraftingSecondaryUsageItemVM(name, num2, i, SecondaryUsageSelector));
			if (IsInOrderMode)
			{
				WeaponComponentData orderWeapon = _completedOrder.GetStatWeapon();
				num = _crafting.GetCurrentCraftedItemObject().Weapons.FindIndex((WeaponComponentData x) => x.WeaponDescriptionId == orderWeapon.WeaponDescriptionId);
			}
			else if (_completedOrder?.GetStatWeapon().WeaponDescriptionId == weapons[i].WeaponDescriptionId)
			{
				num = num2;
			}
			num2++;
		}
		SecondaryUsageSelector.SelectedIndex = ((num >= 0) ? num : 0);
	}

	private void OnUsageSelected(SelectorVM<CraftingSecondaryUsageItemVM> selector)
	{
		DesignResultPropertyList = _onGetPropertyList?.Invoke(selector.SelectedItem);
		if (_isInOrderMode)
		{
			_craftingBehavior.GetOrderResult(_completedOrder, _craftedItem, out var isSucceed, out var orderRemark, out var orderResult, out var finalPrice);
			CraftedWeaponInitialWorth = _completedOrder.BaseGoldReward;
			CraftedWeaponFinalWorth = finalPrice;
			IsOrderSuccessful = isSucceed;
			CraftedWeaponWorthText = new TextObject("{=ZIn8W5ZG}Worth").ToString();
			DesignResultPropertyList.Add(new WeaponDesignResultPropertyItemVM(new TextObject("{=QmfZjCo1}Worth: "), CraftedWeaponInitialWorth, CraftedWeaponInitialWorth, CraftedWeaponFinalWorth - CraftedWeaponInitialWorth, showFloatingPoint: false, isExceedingBeneficial: true, showTooltip: false));
			OrderOwnerRemarkText = orderRemark.ToString();
			OrderResultText = orderResult.ToString();
		}
	}

	private void UpdateConfirmAvailability()
	{
		if (IsInOrderMode)
		{
			CanConfirm = true;
			ConfirmDisabledReasonHint = new HintViewModel();
		}
		else
		{
			Tuple<bool, TextObject> tuple = CampaignUIHelper.IsStringApplicableForItemName(ItemName);
			CanConfirm = tuple.Item1;
			ConfirmDisabledReasonHint = new HintViewModel(tuple.Item2);
		}
	}

	public void ExecuteFinalizeCrafting()
	{
		TextObject textObject = new TextObject("{=!}" + ItemName);
		_crafting.SetCraftedWeaponName(textObject);
		_craftingBehavior.SetCraftedWeaponName(_craftedItem, textObject);
		_onFinalize?.Invoke();
		Game.Current?.EventManager.TriggerEvent(new CraftingWeaponResultPopupToggledEvent(isOpen: false));
		if (!_isInOrderMode)
		{
			TextObject textObject2 = GameTexts.FindText("crafting_added_to_inventory");
			textObject2.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			textObject2.SetTextVariable("ITEM_NAME", ItemName);
			MBInformationManager.AddQuickInformation(textObject2);
		}
	}

	public void ExecuteRandomCraftName()
	{
		ItemName = _crafting.GetRandomCraftName().ToString();
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
