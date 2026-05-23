using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class SPInventoryVM : ViewModel
{
	public enum EquipmentModes
	{
		Civilian,
		Battle,
		Stealth
	}

	public enum Filters
	{
		All,
		Weapons,
		ShieldsAndRanged,
		Armors,
		Mounts,
		Miscellaneous
	}

	private class RosterElementComparer : IComparer<SPItemVM>
	{
		private readonly MobileParty _currentParty;

		private readonly InventoryCapacityModel _inventoryCapacityModel;

		private readonly bool _basedOnGoldAmount;

		public RosterElementComparer(InventoryCapacityModel inventoryCapacityModel, MobileParty currentParty, bool basedOnGoldAmount)
		{
			_inventoryCapacityModel = inventoryCapacityModel;
			_currentParty = currentParty;
			_basedOnGoldAmount = basedOnGoldAmount;
		}

		public int Compare(SPItemVM x, SPItemVM y)
		{
			EquipmentElement equipmentElement = x.ItemRosterElement.EquipmentElement;
			EquipmentElement equipmentElement2 = y.ItemRosterElement.EquipmentElement;
			if (_currentParty != null)
			{
				TextObject description;
				TextObject description2;
				int num = _inventoryCapacityModel.GetItemEffectiveWeight(equipmentElement, _currentParty, _currentParty.IsCurrentlyAtSea, out description).CompareTo(_inventoryCapacityModel.GetItemEffectiveWeight(equipmentElement2, _currentParty, _currentParty.IsCurrentlyAtSea, out description2));
				if (num != 0)
				{
					return num;
				}
				return x.ItemCost.CompareTo(y.ItemCost);
			}
			if (_basedOnGoldAmount)
			{
				return x.ItemCost.CompareTo(y.ItemCost);
			}
			return 0;
		}
	}

	public bool DoNotSync;

	private readonly Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

	public bool IsFiveStackModifierActive;

	private readonly IViewDataTracker _viewDataTracker;

	public bool IsEntireStackModifierActive;

	private readonly int _donationMaxShareableXp;

	private readonly Stack<SPItemVM> _equipAfterTransferStack;

	private readonly TroopRoster _rightTroopRoster;

	private InventoryScreenHelper.InventoryMode _usageType = InventoryScreenHelper.InventoryMode.Trade;

	private bool _isTrading;

	private readonly TroopRoster _leftTroopRoster;

	private int _lastComparedItemIndex;

	private bool _isCharacterEquipmentDirty;

	private int _currentInventoryCharacterIndex;

	private string _selectedTooltipItemStringID = "";

	private string _comparedTooltipItemStringID = "";

	private InventoryLogic _inventoryLogic;

	private CharacterObject _currentCharacter;

	private SPItemVM _selectedItem;

	private List<ItemVM> _comparedItemList;

	private Func<string, TextObject> _getKeyTextFromKeyId;

	private List<string> _lockedItemIDs;

	private readonly List<int> _everyItemType = new List<int>
	{
		1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
		11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
		21, 22, 23, 24, 25, 26
	};

	private readonly List<int> _weaponItemTypes = new List<int> { 2, 3, 4 };

	private readonly List<int> _armorItemTypes = new List<int> { 14, 15, 16, 17, 23, 24 };

	private readonly List<int> _mountItemTypes = new List<int> { 1, 25 };

	private readonly List<int> _shieldAndRangedItemTypes = new List<int>
	{
		8, 5, 6, 7, 9, 10, 11, 12, 18, 19,
		20
	};

	private readonly List<int> _miscellaneousItemTypes = new List<int> { 13, 21, 22, 26 };

	private readonly Dictionary<Filters, List<int>> _filters;

	private int _selectedEquipmentIndex;

	private bool _isFoodTransferButtonHighlightApplied;

	private bool _isBannerItemsHighlightApplied;

	private string _latestTutorialElementID;

	private string _leftInventoryLabel;

	private string _rightInventoryLabel;

	private bool _otherSideHasCapacity;

	private bool _isDoneDisabled;

	private bool _isSearchAvailable;

	private bool _isOtherInventoryGoldRelevant;

	private string _doneLbl;

	private string _cancelLbl;

	private string _resetLbl;

	private string _typeText;

	private string _nameText;

	private string _quantityText;

	private string _costText;

	private string _searchPlaceholderText;

	private HintViewModel _resetHint;

	private HintViewModel _filterAllHint;

	private HintViewModel _filterWeaponHint;

	private HintViewModel _filterArmorHint;

	private HintViewModel _filterShieldAndRangedHint;

	private HintViewModel _filterMountAndHarnessHint;

	private HintViewModel _filterMiscHint;

	private HintViewModel _stealthOutfitHint;

	private HintViewModel _civilianOutfitHint;

	private HintViewModel _battleOutfitHint;

	private HintViewModel _equipmentHelmSlotHint;

	private HintViewModel _equipmentArmorSlotHint;

	private HintViewModel _equipmentBootSlotHint;

	private HintViewModel _equipmentCloakSlotHint;

	private HintViewModel _equipmentGloveSlotHint;

	private HintViewModel _equipmentHarnessSlotHint;

	private HintViewModel _equipmentMountSlotHint;

	private HintViewModel _equipmentWeaponSlotHint;

	private HintViewModel _equipmentBannerSlotHint;

	private BasicTooltipViewModel _buyAllHint;

	private BasicTooltipViewModel _sellAllHint;

	private BasicTooltipViewModel _previousCharacterHint;

	private BasicTooltipViewModel _nextCharacterHint;

	private HintViewModel _weightHint;

	private HintViewModel _armArmorHint;

	private HintViewModel _bodyArmorHint;

	private HintViewModel _headArmorHint;

	private HintViewModel _legArmorHint;

	private HintViewModel _horseArmorHint;

	private HintViewModel _previewHint;

	private HintViewModel _equipHint;

	private HintViewModel _unequipHint;

	private HintViewModel _sellHint;

	private HintViewModel _playerSideCapacityExceededHint;

	private HintViewModel _mainPartyLandCapacityExceededHint;

	private HintViewModel _mainPartySeaCapacityExceededHint;

	private HintViewModel _noSaddleHint;

	private HintViewModel _donationLblHint;

	private HintViewModel _otherSideCapacityExceededHint;

	private BasicTooltipViewModel _totalWeightCarriedHint;

	private BasicTooltipViewModel _landWeightHint;

	private BasicTooltipViewModel _seaWeightHint;

	private BasicTooltipViewModel _inventoryCapacityHint;

	private BasicTooltipViewModel _landCapacityHint;

	private BasicTooltipViewModel _seaCapacityHint;

	private BasicTooltipViewModel _currentCharacterSkillsTooltip;

	private BasicTooltipViewModel _productionTooltip;

	private HeroViewModel _mainCharacter;

	private bool _isExtendedEquipmentControlsEnabled;

	private bool _isFocusedOnItemList;

	private SPItemVM _currentFocusedItem;

	private bool _equipAfterBuy;

	private MBBindingList<SPItemVM> _leftItemListVM;

	private MBBindingList<SPItemVM> _rightItemListVM;

	private ItemMenuVM _itemMenu;

	private SPItemVM _characterHelmSlot;

	private SPItemVM _characterCloakSlot;

	private SPItemVM _characterTorsoSlot;

	private SPItemVM _characterGloveSlot;

	private SPItemVM _characterBootSlot;

	private SPItemVM _characterMountSlot;

	private SPItemVM _characterMountArmorSlot;

	private SPItemVM _characterWeapon1Slot;

	private SPItemVM _characterWeapon2Slot;

	private SPItemVM _characterWeapon3Slot;

	private SPItemVM _characterWeapon4Slot;

	private SPItemVM _characterBannerSlot;

	private EquipmentIndex _targetEquipmentIndex = EquipmentIndex.None;

	private int _transactionCount = -1;

	private bool _isRefreshed;

	private string _tradeLbl = "";

	private string _experienceLbl = "";

	private bool _hasGainedExperience;

	private bool _isDonationXpGainExceedsMax;

	private bool _noSaddleWarned;

	private bool _isTradingWithSettlement;

	private bool _showMainPartyLandCapacityTexts;

	private bool _showMainPartySeaCapacityTexts;

	private string _otherEquipmentCountText;

	private string _mainPartyTotalWeightCarriedText;

	private string _mainPartyLandWeightText;

	private string _mainPartySeaWeightText;

	private string _mainPartyInventoryCapacityText;

	private bool _otherEquipmentCapacityExceededWarning;

	private bool _otherEquipmentCountWarned;

	private bool _playerEquipmentCountWarned;

	private string _noSaddleText;

	private string _mainPartyLandCapacityText;

	private string _mainPartySeaCapacityText;

	private bool _isMainPartyLandCapacityWarned;

	private string _leftSearchText = "";

	private bool _isMainPartySeaCapacityWarned;

	private bool _showMainPartyLandCapacityWarning;

	private bool _showMainPartySeaCapacityWarning;

	private string _playerSideCapacityExceededText;

	private string _mainPartyLandCapacityExceededText;

	private string _mainPartySeaCapacityExceededText;

	private string _separatorText;

	private string _otherSideCapacityExceededText;

	private string _rightSearchText = "";

	private string _bannerTypeName;

	private EquipmentModes _equipmentMode = EquipmentModes.Battle;

	private bool _companionExists;

	private Filters _activeFilterIndex;

	private bool _isMicsFilterHighlightEnabled;

	private bool _isCivilianFilterHighlightEnabled;

	private ItemPreviewVM _itemPreview;

	private SelectorVM<InventoryCharacterSelectorItemVM> _characterList;

	private SPInventorySortControllerVM _otherInventorySortController;

	private SPInventorySortControllerVM _playerInventorySortController;

	private bool _scrollToItem;

	private string _scrollItemId;

	private bool _isBattleMode = true;

	private bool _isStealthMode;

	private bool _isCivilianMode;

	private string _leftInventoryOwnerName;

	private int _leftInventoryOwnerGold;

	private string _rightInventoryOwnerName;

	private string _currentCharacterName;

	private int _rightInventoryOwnerGold;

	private int _itemCountToBuy;

	private float _currentCharacterArmArmor;

	private float _currentCharacterBodyArmor;

	private float _currentCharacterHeadArmor;

	private float _currentCharacterLegArmor;

	private float _currentCharacterHorseArmor;

	private string _currentCharacterTotalEncumbrance;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _previousCharacterInputKey;

	private InputKeyItemVM _nextCharacterInputKey;

	private InputKeyItemVM _buyAllInputKey;

	private InputKeyItemVM _sellAllInputKey;

	private Equipment ActiveEquipment
	{
		get
		{
			switch (EquipmentMode)
			{
			case 0:
				return _currentCharacter.FirstCivilianEquipment;
			case 1:
				return _currentCharacter.FirstBattleEquipment;
			case 2:
				return _currentCharacter.FirstStealthEquipment;
			default:
				Debug.FailedAssert("Invalid active equipment type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ActiveEquipment", 517);
				return null;
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetHint
	{
		get
		{
			return _resetHint;
		}
		set
		{
			if (value != _resetHint)
			{
				_resetHint = value;
				OnPropertyChangedWithValue(value, "ResetHint");
			}
		}
	}

	[DataSourceProperty]
	public string LeftInventoryLabel
	{
		get
		{
			return _leftInventoryLabel;
		}
		set
		{
			if (value != _leftInventoryLabel)
			{
				_leftInventoryLabel = value;
				OnPropertyChangedWithValue(value, "LeftInventoryLabel");
			}
		}
	}

	[DataSourceProperty]
	public string RightInventoryLabel
	{
		get
		{
			return _rightInventoryLabel;
		}
		set
		{
			if (value != _rightInventoryLabel)
			{
				_rightInventoryLabel = value;
				OnPropertyChangedWithValue(value, "RightInventoryLabel");
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
	public bool IsDoneDisabled
	{
		get
		{
			return _isDoneDisabled;
		}
		set
		{
			if (value != _isDoneDisabled)
			{
				_isDoneDisabled = value;
				OnPropertyChangedWithValue(value, "IsDoneDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool OtherSideHasCapacity
	{
		get
		{
			return _otherSideHasCapacity;
		}
		set
		{
			if (value != _otherSideHasCapacity)
			{
				_otherSideHasCapacity = value;
				OnPropertyChangedWithValue(value, "OtherSideHasCapacity");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSearchAvailable
	{
		get
		{
			return _isSearchAvailable;
		}
		set
		{
			if (value != _isSearchAvailable)
			{
				if (!value)
				{
					LeftSearchText = string.Empty;
					RightSearchText = string.Empty;
				}
				_isSearchAvailable = value;
				OnPropertyChangedWithValue(value, "IsSearchAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOtherInventoryGoldRelevant
	{
		get
		{
			return _isOtherInventoryGoldRelevant;
		}
		set
		{
			if (value != _isOtherInventoryGoldRelevant)
			{
				_isOtherInventoryGoldRelevant = value;
				OnPropertyChangedWithValue(value, "IsOtherInventoryGoldRelevant");
			}
		}
	}

	[DataSourceProperty]
	public string CancelLbl
	{
		get
		{
			return _cancelLbl;
		}
		set
		{
			if (value != _cancelLbl)
			{
				_cancelLbl = value;
				OnPropertyChangedWithValue(value, "CancelLbl");
			}
		}
	}

	[DataSourceProperty]
	public string ResetLbl
	{
		get
		{
			return _resetLbl;
		}
		set
		{
			if (value != _resetLbl)
			{
				_resetLbl = value;
				OnPropertyChangedWithValue(value, "ResetLbl");
			}
		}
	}

	[DataSourceProperty]
	public string TypeText
	{
		get
		{
			return _typeText;
		}
		set
		{
			if (value != _typeText)
			{
				_typeText = value;
				OnPropertyChangedWithValue(value, "TypeText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string QuantityText
	{
		get
		{
			return _quantityText;
		}
		set
		{
			if (value != _quantityText)
			{
				_quantityText = value;
				OnPropertyChangedWithValue(value, "QuantityText");
			}
		}
	}

	[DataSourceProperty]
	public string CostText
	{
		get
		{
			return _costText;
		}
		set
		{
			if (value != _costText)
			{
				_costText = value;
				OnPropertyChangedWithValue(value, "CostText");
			}
		}
	}

	[DataSourceProperty]
	public string SearchPlaceholderText
	{
		get
		{
			return _searchPlaceholderText;
		}
		set
		{
			if (value != _searchPlaceholderText)
			{
				_searchPlaceholderText = value;
				OnPropertyChangedWithValue(value, "SearchPlaceholderText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel ProductionTooltip
	{
		get
		{
			return _productionTooltip;
		}
		set
		{
			if (value != _productionTooltip)
			{
				_productionTooltip = value;
				OnPropertyChangedWithValue(value, "ProductionTooltip");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel InventoryCapacityHint
	{
		get
		{
			return _inventoryCapacityHint;
		}
		set
		{
			if (value != _inventoryCapacityHint)
			{
				_inventoryCapacityHint = value;
				OnPropertyChangedWithValue(value, "InventoryCapacityHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel LandCapacityHint
	{
		get
		{
			return _landCapacityHint;
		}
		set
		{
			if (value != _landCapacityHint)
			{
				_landCapacityHint = value;
				OnPropertyChangedWithValue(value, "LandCapacityHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SeaCapacityHint
	{
		get
		{
			return _seaCapacityHint;
		}
		set
		{
			if (value != _seaCapacityHint)
			{
				_seaCapacityHint = value;
				OnPropertyChangedWithValue(value, "SeaCapacityHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TotalWeightCarriedHint
	{
		get
		{
			return _totalWeightCarriedHint;
		}
		set
		{
			if (value != _totalWeightCarriedHint)
			{
				_totalWeightCarriedHint = value;
				OnPropertyChangedWithValue(value, "TotalWeightCarriedHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel LandWeightHint
	{
		get
		{
			return _landWeightHint;
		}
		set
		{
			if (value != _landWeightHint)
			{
				_landWeightHint = value;
				OnPropertyChangedWithValue(value, "LandWeightHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SeaWeightHint
	{
		get
		{
			return _seaWeightHint;
		}
		set
		{
			if (value != _seaWeightHint)
			{
				_seaWeightHint = value;
				OnPropertyChangedWithValue(value, "SeaWeightHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CurrentCharacterSkillsTooltip
	{
		get
		{
			return _currentCharacterSkillsTooltip;
		}
		set
		{
			if (value != _currentCharacterSkillsTooltip)
			{
				_currentCharacterSkillsTooltip = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterSkillsTooltip");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel NoSaddleHint
	{
		get
		{
			return _noSaddleHint;
		}
		set
		{
			if (value != _noSaddleHint)
			{
				_noSaddleHint = value;
				OnPropertyChangedWithValue(value, "NoSaddleHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DonationLblHint
	{
		get
		{
			return _donationLblHint;
		}
		set
		{
			if (value != _donationLblHint)
			{
				_donationLblHint = value;
				OnPropertyChangedWithValue(value, "DonationLblHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ArmArmorHint
	{
		get
		{
			return _armArmorHint;
		}
		set
		{
			if (value != _armArmorHint)
			{
				_armArmorHint = value;
				OnPropertyChangedWithValue(value, "ArmArmorHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BodyArmorHint
	{
		get
		{
			return _bodyArmorHint;
		}
		set
		{
			if (value != _bodyArmorHint)
			{
				_bodyArmorHint = value;
				OnPropertyChangedWithValue(value, "BodyArmorHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HeadArmorHint
	{
		get
		{
			return _headArmorHint;
		}
		set
		{
			if (value != _headArmorHint)
			{
				_headArmorHint = value;
				OnPropertyChangedWithValue(value, "HeadArmorHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LegArmorHint
	{
		get
		{
			return _legArmorHint;
		}
		set
		{
			if (value != _legArmorHint)
			{
				_legArmorHint = value;
				OnPropertyChangedWithValue(value, "LegArmorHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HorseArmorHint
	{
		get
		{
			return _horseArmorHint;
		}
		set
		{
			if (value != _horseArmorHint)
			{
				_horseArmorHint = value;
				OnPropertyChangedWithValue(value, "HorseArmorHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FilterAllHint
	{
		get
		{
			return _filterAllHint;
		}
		set
		{
			if (value != _filterAllHint)
			{
				_filterAllHint = value;
				OnPropertyChangedWithValue(value, "FilterAllHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FilterWeaponHint
	{
		get
		{
			return _filterWeaponHint;
		}
		set
		{
			if (value != _filterWeaponHint)
			{
				_filterWeaponHint = value;
				OnPropertyChangedWithValue(value, "FilterWeaponHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FilterArmorHint
	{
		get
		{
			return _filterArmorHint;
		}
		set
		{
			if (value != _filterArmorHint)
			{
				_filterArmorHint = value;
				OnPropertyChangedWithValue(value, "FilterArmorHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FilterShieldAndRangedHint
	{
		get
		{
			return _filterShieldAndRangedHint;
		}
		set
		{
			if (value != _filterShieldAndRangedHint)
			{
				_filterShieldAndRangedHint = value;
				OnPropertyChangedWithValue(value, "FilterShieldAndRangedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FilterMountAndHarnessHint
	{
		get
		{
			return _filterMountAndHarnessHint;
		}
		set
		{
			if (value != _filterMountAndHarnessHint)
			{
				_filterMountAndHarnessHint = value;
				OnPropertyChangedWithValue(value, "FilterMountAndHarnessHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FilterMiscHint
	{
		get
		{
			return _filterMiscHint;
		}
		set
		{
			if (value != _filterMiscHint)
			{
				_filterMiscHint = value;
				OnPropertyChangedWithValue(value, "FilterMiscHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel StealthOutfitHint
	{
		get
		{
			return _stealthOutfitHint;
		}
		set
		{
			if (value != _stealthOutfitHint)
			{
				_stealthOutfitHint = value;
				OnPropertyChangedWithValue(value, "StealthOutfitHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CivilianOutfitHint
	{
		get
		{
			return _civilianOutfitHint;
		}
		set
		{
			if (value != _civilianOutfitHint)
			{
				_civilianOutfitHint = value;
				OnPropertyChangedWithValue(value, "CivilianOutfitHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BattleOutfitHint
	{
		get
		{
			return _battleOutfitHint;
		}
		set
		{
			if (value != _battleOutfitHint)
			{
				_battleOutfitHint = value;
				OnPropertyChangedWithValue(value, "BattleOutfitHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentHelmSlotHint
	{
		get
		{
			return _equipmentHelmSlotHint;
		}
		set
		{
			if (value != _equipmentHelmSlotHint)
			{
				_equipmentHelmSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentHelmSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentArmorSlotHint
	{
		get
		{
			return _equipmentArmorSlotHint;
		}
		set
		{
			if (value != _equipmentArmorSlotHint)
			{
				_equipmentArmorSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentArmorSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentBootSlotHint
	{
		get
		{
			return _equipmentBootSlotHint;
		}
		set
		{
			if (value != _equipmentBootSlotHint)
			{
				_equipmentBootSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentBootSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentCloakSlotHint
	{
		get
		{
			return _equipmentCloakSlotHint;
		}
		set
		{
			if (value != _equipmentCloakSlotHint)
			{
				_equipmentCloakSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentCloakSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentGloveSlotHint
	{
		get
		{
			return _equipmentGloveSlotHint;
		}
		set
		{
			if (value != _equipmentGloveSlotHint)
			{
				_equipmentGloveSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentGloveSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentHarnessSlotHint
	{
		get
		{
			return _equipmentHarnessSlotHint;
		}
		set
		{
			if (value != _equipmentHarnessSlotHint)
			{
				_equipmentHarnessSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentHarnessSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentMountSlotHint
	{
		get
		{
			return _equipmentMountSlotHint;
		}
		set
		{
			if (value != _equipmentMountSlotHint)
			{
				_equipmentMountSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentMountSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentWeaponSlotHint
	{
		get
		{
			return _equipmentWeaponSlotHint;
		}
		set
		{
			if (value != _equipmentWeaponSlotHint)
			{
				_equipmentWeaponSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentWeaponSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipmentBannerSlotHint
	{
		get
		{
			return _equipmentBannerSlotHint;
		}
		set
		{
			if (value != _equipmentBannerSlotHint)
			{
				_equipmentBannerSlotHint = value;
				OnPropertyChangedWithValue(value, "EquipmentBannerSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel BuyAllHint
	{
		get
		{
			return _buyAllHint;
		}
		set
		{
			if (value != _buyAllHint)
			{
				_buyAllHint = value;
				OnPropertyChangedWithValue(value, "BuyAllHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SellAllHint
	{
		get
		{
			return _sellAllHint;
		}
		set
		{
			if (value != _sellAllHint)
			{
				_sellAllHint = value;
				OnPropertyChangedWithValue(value, "SellAllHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel PreviousCharacterHint
	{
		get
		{
			return _previousCharacterHint;
		}
		set
		{
			if (value != _previousCharacterHint)
			{
				_previousCharacterHint = value;
				OnPropertyChangedWithValue(value, "PreviousCharacterHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel NextCharacterHint
	{
		get
		{
			return _nextCharacterHint;
		}
		set
		{
			if (value != _nextCharacterHint)
			{
				_nextCharacterHint = value;
				OnPropertyChangedWithValue(value, "NextCharacterHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel WeightHint
	{
		get
		{
			return _weightHint;
		}
		set
		{
			if (value != _weightHint)
			{
				_weightHint = value;
				OnPropertyChangedWithValue(value, "WeightHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PreviewHint
	{
		get
		{
			return _previewHint;
		}
		set
		{
			if (value != _previewHint)
			{
				_previewHint = value;
				OnPropertyChangedWithValue(value, "PreviewHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipHint
	{
		get
		{
			return _equipHint;
		}
		set
		{
			if (value != _equipHint)
			{
				_equipHint = value;
				OnPropertyChangedWithValue(value, "EquipHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UnequipHint
	{
		get
		{
			return _unequipHint;
		}
		set
		{
			if (value != _unequipHint)
			{
				_unequipHint = value;
				OnPropertyChangedWithValue(value, "UnequipHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SellHint
	{
		get
		{
			return _sellHint;
		}
		set
		{
			if (value != _sellHint)
			{
				_sellHint = value;
				OnPropertyChangedWithValue(value, "SellHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PlayerSideCapacityExceededHint
	{
		get
		{
			return _playerSideCapacityExceededHint;
		}
		set
		{
			if (value != _playerSideCapacityExceededHint)
			{
				_playerSideCapacityExceededHint = value;
				OnPropertyChangedWithValue(value, "PlayerSideCapacityExceededHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MainPartyLandCapacityExceededHint
	{
		get
		{
			return _mainPartyLandCapacityExceededHint;
		}
		set
		{
			if (value != _mainPartyLandCapacityExceededHint)
			{
				_mainPartyLandCapacityExceededHint = value;
				OnPropertyChangedWithValue(value, "MainPartyLandCapacityExceededHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MainPartySeaCapacityExceededHint
	{
		get
		{
			return _mainPartySeaCapacityExceededHint;
		}
		set
		{
			if (value != _mainPartySeaCapacityExceededHint)
			{
				_mainPartySeaCapacityExceededHint = value;
				OnPropertyChangedWithValue(value, "MainPartySeaCapacityExceededHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel OtherSideCapacityExceededHint
	{
		get
		{
			return _otherSideCapacityExceededHint;
		}
		set
		{
			if (value != _otherSideCapacityExceededHint)
			{
				_otherSideCapacityExceededHint = value;
				OnPropertyChangedWithValue(value, "OtherSideCapacityExceededHint");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<InventoryCharacterSelectorItemVM> CharacterList
	{
		get
		{
			return _characterList;
		}
		set
		{
			if (value != _characterList)
			{
				_characterList = value;
				OnPropertyChangedWithValue(value, "CharacterList");
			}
		}
	}

	[DataSourceProperty]
	public SPInventorySortControllerVM PlayerInventorySortController
	{
		get
		{
			return _playerInventorySortController;
		}
		set
		{
			if (value != _playerInventorySortController)
			{
				_playerInventorySortController = value;
				OnPropertyChangedWithValue(value, "PlayerInventorySortController");
			}
		}
	}

	[DataSourceProperty]
	public SPInventorySortControllerVM OtherInventorySortController
	{
		get
		{
			return _otherInventorySortController;
		}
		set
		{
			if (value != _otherInventorySortController)
			{
				_otherInventorySortController = value;
				OnPropertyChangedWithValue(value, "OtherInventorySortController");
			}
		}
	}

	[DataSourceProperty]
	public ItemPreviewVM ItemPreview
	{
		get
		{
			return _itemPreview;
		}
		set
		{
			if (value != _itemPreview)
			{
				_itemPreview = value;
				OnPropertyChangedWithValue(value, "ItemPreview");
			}
		}
	}

	[DataSourceProperty]
	public int ActiveFilterIndex
	{
		get
		{
			return (int)_activeFilterIndex;
		}
		set
		{
			if (value != (int)_activeFilterIndex)
			{
				_activeFilterIndex = (Filters)value;
				OnPropertyChangedWithValue(value, "ActiveFilterIndex");
			}
		}
	}

	[DataSourceProperty]
	public bool CompanionExists
	{
		get
		{
			return _companionExists;
		}
		set
		{
			if (value != _companionExists)
			{
				_companionExists = value;
				OnPropertyChangedWithValue(value, "CompanionExists");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTradingWithSettlement
	{
		get
		{
			return _isTradingWithSettlement;
		}
		set
		{
			if (value != _isTradingWithSettlement)
			{
				_isTradingWithSettlement = value;
				OnPropertyChangedWithValue(value, "IsTradingWithSettlement");
			}
		}
	}

	[DataSourceProperty]
	public int EquipmentMode
	{
		get
		{
			return (int)_equipmentMode;
		}
		set
		{
			if (value != (int)_equipmentMode)
			{
				_equipmentMode = (EquipmentModes)value;
				OnPropertyChangedWithValue(value, "EquipmentMode");
				UpdateRightCharacter();
				OnEquipmentModeChanged();
				RefreshInformationValues();
				Game.Current.EventManager.TriggerEvent(new InventoryEquipmentTypeChangedEvent(_equipmentMode == EquipmentModes.Battle));
			}
		}
	}

	[DataSourceProperty]
	public bool IsMicsFilterHighlightEnabled
	{
		get
		{
			return _isMicsFilterHighlightEnabled;
		}
		set
		{
			if (value != _isMicsFilterHighlightEnabled)
			{
				_isMicsFilterHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsMicsFilterHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEquipmentSetFiltersHighlighted
	{
		get
		{
			return _isCivilianFilterHighlightEnabled;
		}
		set
		{
			if (value != _isCivilianFilterHighlightEnabled)
			{
				_isCivilianFilterHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsEquipmentSetFiltersHighlighted");
			}
		}
	}

	[DataSourceProperty]
	public ItemMenuVM ItemMenu
	{
		get
		{
			return _itemMenu;
		}
		set
		{
			if (value != _itemMenu)
			{
				_itemMenu = value;
				OnPropertyChangedWithValue(value, "ItemMenu");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerSideCapacityExceededText
	{
		get
		{
			return _playerSideCapacityExceededText;
		}
		set
		{
			if (value != _playerSideCapacityExceededText)
			{
				_playerSideCapacityExceededText = value;
				OnPropertyChangedWithValue(value, "PlayerSideCapacityExceededText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyLandCapacityExceededText
	{
		get
		{
			return _mainPartyLandCapacityExceededText;
		}
		set
		{
			if (value != _mainPartyLandCapacityExceededText)
			{
				_mainPartyLandCapacityExceededText = value;
				OnPropertyChangedWithValue(value, "MainPartyLandCapacityExceededText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartySeaCapacityExceededText
	{
		get
		{
			return _mainPartySeaCapacityExceededText;
		}
		set
		{
			if (value != _mainPartySeaCapacityExceededText)
			{
				_mainPartySeaCapacityExceededText = value;
				OnPropertyChangedWithValue(value, "MainPartySeaCapacityExceededText");
			}
		}
	}

	[DataSourceProperty]
	public string SeparatorText
	{
		get
		{
			return _separatorText;
		}
		set
		{
			if (value != _separatorText)
			{
				_separatorText = value;
				OnPropertyChangedWithValue(value, "SeparatorText");
			}
		}
	}

	[DataSourceProperty]
	public string OtherSideCapacityExceededText
	{
		get
		{
			return _otherSideCapacityExceededText;
		}
		set
		{
			if (value != _otherSideCapacityExceededText)
			{
				_otherSideCapacityExceededText = value;
				OnPropertyChangedWithValue(value, "OtherSideCapacityExceededText");
			}
		}
	}

	[DataSourceProperty]
	public string LeftSearchText
	{
		get
		{
			return _leftSearchText;
		}
		set
		{
			if (value != _leftSearchText)
			{
				_leftSearchText = value;
				OnPropertyChangedWithValue(value, "LeftSearchText");
				OnSearchTextChanged(isLeft: true);
			}
		}
	}

	[DataSourceProperty]
	public string RightSearchText
	{
		get
		{
			return _rightSearchText;
		}
		set
		{
			if (value != _rightSearchText)
			{
				_rightSearchText = value;
				OnPropertyChangedWithValue(value, "RightSearchText");
				OnSearchTextChanged(isLeft: false);
			}
		}
	}

	[DataSourceProperty]
	public bool HasGainedExperience
	{
		get
		{
			return _hasGainedExperience;
		}
		set
		{
			if (value != _hasGainedExperience)
			{
				_hasGainedExperience = value;
				OnPropertyChangedWithValue(value, "HasGainedExperience");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDonationXpGainExceedsMax
	{
		get
		{
			return _isDonationXpGainExceedsMax;
		}
		set
		{
			if (value != _isDonationXpGainExceedsMax)
			{
				_isDonationXpGainExceedsMax = value;
				OnPropertyChangedWithValue(value, "IsDonationXpGainExceedsMax");
			}
		}
	}

	[DataSourceProperty]
	public bool NoSaddleWarned
	{
		get
		{
			return _noSaddleWarned;
		}
		set
		{
			if (value != _noSaddleWarned)
			{
				_noSaddleWarned = value;
				OnPropertyChangedWithValue(value, "NoSaddleWarned");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowMainPartyLandCapacityTexts
	{
		get
		{
			return _showMainPartyLandCapacityTexts;
		}
		set
		{
			if (value != _showMainPartyLandCapacityTexts)
			{
				_showMainPartyLandCapacityTexts = value;
				OnPropertyChangedWithValue(value, "ShowMainPartyLandCapacityTexts");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowMainPartySeaCapacityTexts
	{
		get
		{
			return _showMainPartySeaCapacityTexts;
		}
		set
		{
			if (value != _showMainPartySeaCapacityTexts)
			{
				_showMainPartySeaCapacityTexts = value;
				OnPropertyChangedWithValue(value, "ShowMainPartySeaCapacityTexts");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerEquipmentCountWarned
	{
		get
		{
			return _playerEquipmentCountWarned;
		}
		set
		{
			if (value != _playerEquipmentCountWarned)
			{
				_playerEquipmentCountWarned = value;
				OnPropertyChangedWithValue(value, "PlayerEquipmentCountWarned");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainPartyLandCapacityWarned
	{
		get
		{
			return _isMainPartyLandCapacityWarned;
		}
		set
		{
			if (value != _isMainPartyLandCapacityWarned)
			{
				_isMainPartyLandCapacityWarned = value;
				OnPropertyChangedWithValue(value, "IsMainPartyLandCapacityWarned");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainPartySeaCapacityWarned
	{
		get
		{
			return _isMainPartySeaCapacityWarned;
		}
		set
		{
			if (value != _isMainPartySeaCapacityWarned)
			{
				_isMainPartySeaCapacityWarned = value;
				OnPropertyChangedWithValue(value, "IsMainPartySeaCapacityWarned");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowMainPartyLandCapacityWarning
	{
		get
		{
			return _showMainPartyLandCapacityWarning;
		}
		set
		{
			if (value != _showMainPartyLandCapacityWarning)
			{
				_showMainPartyLandCapacityWarning = value;
				OnPropertyChangedWithValue(value, "ShowMainPartyLandCapacityWarning");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowMainPartySeaCapacityWarning
	{
		get
		{
			return _showMainPartySeaCapacityWarning;
		}
		set
		{
			if (value != _showMainPartySeaCapacityWarning)
			{
				_showMainPartySeaCapacityWarning = value;
				OnPropertyChangedWithValue(value, "ShowMainPartySeaCapacityWarning");
			}
		}
	}

	[DataSourceProperty]
	public bool OtherEquipmentCountWarned
	{
		get
		{
			return _otherEquipmentCountWarned;
		}
		set
		{
			if (value != _otherEquipmentCountWarned)
			{
				_otherEquipmentCountWarned = value;
				OnPropertyChangedWithValue(value, "OtherEquipmentCountWarned");
			}
		}
	}

	[DataSourceProperty]
	public bool OtherEquipmentCapacityExceededWarning
	{
		get
		{
			return _otherEquipmentCapacityExceededWarning;
		}
		set
		{
			if (value != _otherEquipmentCapacityExceededWarning)
			{
				_otherEquipmentCapacityExceededWarning = value;
				OnPropertyChangedWithValue(value, "OtherEquipmentCapacityExceededWarning");
			}
		}
	}

	[DataSourceProperty]
	public string OtherEquipmentCountText
	{
		get
		{
			return _otherEquipmentCountText;
		}
		set
		{
			if (value != _otherEquipmentCountText)
			{
				_otherEquipmentCountText = value;
				OnPropertyChangedWithValue(value, "OtherEquipmentCountText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyTotalWeightCarriedText
	{
		get
		{
			return _mainPartyTotalWeightCarriedText;
		}
		set
		{
			if (value != _mainPartyTotalWeightCarriedText)
			{
				_mainPartyTotalWeightCarriedText = value;
				OnPropertyChangedWithValue(value, "MainPartyTotalWeightCarriedText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyLandWeightText
	{
		get
		{
			return _mainPartyLandWeightText;
		}
		set
		{
			if (value != _mainPartyLandWeightText)
			{
				_mainPartyLandWeightText = value;
				OnPropertyChangedWithValue(value, "MainPartyLandWeightText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartySeaWeightText
	{
		get
		{
			return _mainPartySeaWeightText;
		}
		set
		{
			if (value != _mainPartySeaWeightText)
			{
				_mainPartySeaWeightText = value;
				OnPropertyChangedWithValue(value, "MainPartySeaWeightText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyInventoryCapacityText
	{
		get
		{
			return _mainPartyInventoryCapacityText;
		}
		set
		{
			if (value != _mainPartyInventoryCapacityText)
			{
				_mainPartyInventoryCapacityText = value;
				OnPropertyChangedWithValue(value, "MainPartyInventoryCapacityText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyLandCapacityText
	{
		get
		{
			return _mainPartyLandCapacityText;
		}
		set
		{
			if (value != _mainPartyLandCapacityText)
			{
				_mainPartyLandCapacityText = value;
				OnPropertyChangedWithValue(value, "MainPartyLandCapacityText");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartySeaCapacityText
	{
		get
		{
			return _mainPartySeaCapacityText;
		}
		set
		{
			if (value != _mainPartySeaCapacityText)
			{
				_mainPartySeaCapacityText = value;
				OnPropertyChangedWithValue(value, "MainPartySeaCapacityText");
			}
		}
	}

	[DataSourceProperty]
	public string NoSaddleText
	{
		get
		{
			return _noSaddleText;
		}
		set
		{
			if (value != _noSaddleText)
			{
				_noSaddleText = value;
				OnPropertyChangedWithValue(value, "NoSaddleText");
			}
		}
	}

	[DataSourceProperty]
	public int TargetEquipmentIndex
	{
		get
		{
			return (int)_targetEquipmentIndex;
		}
		set
		{
			if (value != (int)_targetEquipmentIndex)
			{
				_targetEquipmentIndex = (EquipmentIndex)value;
				OnPropertyChangedWithValue(value, "TargetEquipmentIndex");
			}
		}
	}

	public EquipmentIndex TargetEquipmentType
	{
		get
		{
			return _targetEquipmentIndex;
		}
		set
		{
			if (value != _targetEquipmentIndex)
			{
				_targetEquipmentIndex = value;
				OnPropertyChanged("TargetEquipmentIndex");
			}
		}
	}

	[DataSourceProperty]
	public int TransactionCount
	{
		get
		{
			return _transactionCount;
		}
		set
		{
			if (value != _transactionCount)
			{
				_transactionCount = value;
				OnPropertyChangedWithValue(value, "TransactionCount");
			}
			RefreshTransactionCost(value);
		}
	}

	[DataSourceProperty]
	public bool IsTrading
	{
		get
		{
			return _isTrading;
		}
		set
		{
			if (value != _isTrading)
			{
				_isTrading = value;
				OnPropertyChangedWithValue(value, "IsTrading");
			}
		}
	}

	[DataSourceProperty]
	public bool EquipAfterBuy
	{
		get
		{
			return _equipAfterBuy;
		}
		set
		{
			if (value != _equipAfterBuy)
			{
				_equipAfterBuy = value;
				OnPropertyChangedWithValue(value, "EquipAfterBuy");
			}
		}
	}

	[DataSourceProperty]
	public string TradeLbl
	{
		get
		{
			return _tradeLbl;
		}
		set
		{
			if (value != _tradeLbl)
			{
				_tradeLbl = value;
				OnPropertyChangedWithValue(value, "TradeLbl");
			}
		}
	}

	[DataSourceProperty]
	public string ExperienceLbl
	{
		get
		{
			return _experienceLbl;
		}
		set
		{
			if (value != _experienceLbl)
			{
				_experienceLbl = value;
				OnPropertyChangedWithValue(value, "ExperienceLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCharacterName
	{
		get
		{
			return _currentCharacterName;
		}
		set
		{
			if (value != _currentCharacterName)
			{
				_currentCharacterName = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterName");
			}
		}
	}

	[DataSourceProperty]
	public string RightInventoryOwnerName
	{
		get
		{
			return _rightInventoryOwnerName;
		}
		set
		{
			if (value != _rightInventoryOwnerName)
			{
				_rightInventoryOwnerName = value;
				OnPropertyChangedWithValue(value, "RightInventoryOwnerName");
			}
		}
	}

	[DataSourceProperty]
	public string LeftInventoryOwnerName
	{
		get
		{
			return _leftInventoryOwnerName;
		}
		set
		{
			if (value != _leftInventoryOwnerName)
			{
				_leftInventoryOwnerName = value;
				OnPropertyChangedWithValue(value, "LeftInventoryOwnerName");
			}
		}
	}

	[DataSourceProperty]
	public int RightInventoryOwnerGold
	{
		get
		{
			return _rightInventoryOwnerGold;
		}
		set
		{
			if (value != _rightInventoryOwnerGold)
			{
				_rightInventoryOwnerGold = value;
				OnPropertyChangedWithValue(value, "RightInventoryOwnerGold");
			}
		}
	}

	[DataSourceProperty]
	public int LeftInventoryOwnerGold
	{
		get
		{
			return _leftInventoryOwnerGold;
		}
		set
		{
			if (value != _leftInventoryOwnerGold)
			{
				_leftInventoryOwnerGold = value;
				OnPropertyChangedWithValue(value, "LeftInventoryOwnerGold");
			}
		}
	}

	[DataSourceProperty]
	public int ItemCountToBuy
	{
		get
		{
			return _itemCountToBuy;
		}
		set
		{
			if (value != _itemCountToBuy)
			{
				_itemCountToBuy = value;
				OnPropertyChangedWithValue(value, "ItemCountToBuy");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCharacterTotalEncumbrance
	{
		get
		{
			return _currentCharacterTotalEncumbrance;
		}
		set
		{
			if (value != _currentCharacterTotalEncumbrance)
			{
				_currentCharacterTotalEncumbrance = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterTotalEncumbrance");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentCharacterLegArmor
	{
		get
		{
			return _currentCharacterLegArmor;
		}
		set
		{
			if (TaleWorlds.Library.MathF.Abs(value - _currentCharacterLegArmor) > 0.01f)
			{
				_currentCharacterLegArmor = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterLegArmor");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentCharacterHeadArmor
	{
		get
		{
			return _currentCharacterHeadArmor;
		}
		set
		{
			if (TaleWorlds.Library.MathF.Abs(value - _currentCharacterHeadArmor) > 0.01f)
			{
				_currentCharacterHeadArmor = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterHeadArmor");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentCharacterBodyArmor
	{
		get
		{
			return _currentCharacterBodyArmor;
		}
		set
		{
			if (TaleWorlds.Library.MathF.Abs(value - _currentCharacterBodyArmor) > 0.01f)
			{
				_currentCharacterBodyArmor = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterBodyArmor");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentCharacterArmArmor
	{
		get
		{
			return _currentCharacterArmArmor;
		}
		set
		{
			if (TaleWorlds.Library.MathF.Abs(value - _currentCharacterArmArmor) > 0.01f)
			{
				_currentCharacterArmArmor = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterArmArmor");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentCharacterHorseArmor
	{
		get
		{
			return _currentCharacterHorseArmor;
		}
		set
		{
			if (TaleWorlds.Library.MathF.Abs(value - _currentCharacterHorseArmor) > 0.01f)
			{
				_currentCharacterHorseArmor = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterHorseArmor");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRefreshed
	{
		get
		{
			return _isRefreshed;
		}
		set
		{
			if (_isRefreshed != value)
			{
				_isRefreshed = value;
				OnPropertyChangedWithValue(value, "IsRefreshed");
			}
		}
	}

	[DataSourceProperty]
	public bool IsExtendedEquipmentControlsEnabled
	{
		get
		{
			return _isExtendedEquipmentControlsEnabled;
		}
		set
		{
			if (value != _isExtendedEquipmentControlsEnabled)
			{
				_isExtendedEquipmentControlsEnabled = value;
				OnPropertyChangedWithValue(value, "IsExtendedEquipmentControlsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFocusedOnItemList
	{
		get
		{
			return _isFocusedOnItemList;
		}
		set
		{
			if (value != _isFocusedOnItemList)
			{
				_isFocusedOnItemList = value;
				OnPropertyChangedWithValue(value, "IsFocusedOnItemList");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CurrentFocusedItem
	{
		get
		{
			return _currentFocusedItem;
		}
		set
		{
			if (value != _currentFocusedItem)
			{
				_currentFocusedItem = value;
				OnPropertyChangedWithValue(value, "CurrentFocusedItem");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterHelmSlot
	{
		get
		{
			return _characterHelmSlot;
		}
		set
		{
			if (value != _characterHelmSlot)
			{
				_characterHelmSlot = value;
				OnPropertyChangedWithValue(value, "CharacterHelmSlot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterCloakSlot
	{
		get
		{
			return _characterCloakSlot;
		}
		set
		{
			if (value != _characterCloakSlot)
			{
				_characterCloakSlot = value;
				OnPropertyChangedWithValue(value, "CharacterCloakSlot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterTorsoSlot
	{
		get
		{
			return _characterTorsoSlot;
		}
		set
		{
			if (value != _characterTorsoSlot)
			{
				_characterTorsoSlot = value;
				OnPropertyChangedWithValue(value, "CharacterTorsoSlot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterGloveSlot
	{
		get
		{
			return _characterGloveSlot;
		}
		set
		{
			if (value != _characterGloveSlot)
			{
				_characterGloveSlot = value;
				OnPropertyChangedWithValue(value, "CharacterGloveSlot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterBootSlot
	{
		get
		{
			return _characterBootSlot;
		}
		set
		{
			if (value != _characterBootSlot)
			{
				_characterBootSlot = value;
				OnPropertyChangedWithValue(value, "CharacterBootSlot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterMountSlot
	{
		get
		{
			return _characterMountSlot;
		}
		set
		{
			if (value != _characterMountSlot)
			{
				_characterMountSlot = value;
				OnPropertyChangedWithValue(value, "CharacterMountSlot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterMountArmorSlot
	{
		get
		{
			return _characterMountArmorSlot;
		}
		set
		{
			if (value != _characterMountArmorSlot)
			{
				_characterMountArmorSlot = value;
				OnPropertyChangedWithValue(value, "CharacterMountArmorSlot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterWeapon1Slot
	{
		get
		{
			return _characterWeapon1Slot;
		}
		set
		{
			if (value != _characterWeapon1Slot)
			{
				_characterWeapon1Slot = value;
				OnPropertyChangedWithValue(value, "CharacterWeapon1Slot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterWeapon2Slot
	{
		get
		{
			return _characterWeapon2Slot;
		}
		set
		{
			if (value != _characterWeapon2Slot)
			{
				_characterWeapon2Slot = value;
				OnPropertyChangedWithValue(value, "CharacterWeapon2Slot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterWeapon3Slot
	{
		get
		{
			return _characterWeapon3Slot;
		}
		set
		{
			if (value != _characterWeapon3Slot)
			{
				_characterWeapon3Slot = value;
				OnPropertyChangedWithValue(value, "CharacterWeapon3Slot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterWeapon4Slot
	{
		get
		{
			return _characterWeapon4Slot;
		}
		set
		{
			if (value != _characterWeapon4Slot)
			{
				_characterWeapon4Slot = value;
				OnPropertyChangedWithValue(value, "CharacterWeapon4Slot");
			}
		}
	}

	[DataSourceProperty]
	public SPItemVM CharacterBannerSlot
	{
		get
		{
			return _characterBannerSlot;
		}
		set
		{
			if (value != _characterBannerSlot)
			{
				_characterBannerSlot = value;
				OnPropertyChangedWithValue(value, "CharacterBannerSlot");
			}
		}
	}

	[DataSourceProperty]
	public HeroViewModel MainCharacter
	{
		get
		{
			return _mainCharacter;
		}
		set
		{
			if (value != _mainCharacter)
			{
				_mainCharacter = value;
				OnPropertyChangedWithValue(value, "MainCharacter");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SPItemVM> RightItemListVM
	{
		get
		{
			return _rightItemListVM;
		}
		set
		{
			if (value != _rightItemListVM)
			{
				_rightItemListVM = value;
				OnPropertyChangedWithValue(value, "RightItemListVM");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SPItemVM> LeftItemListVM
	{
		get
		{
			return _leftItemListVM;
		}
		set
		{
			if (value != _leftItemListVM)
			{
				_leftItemListVM = value;
				OnPropertyChangedWithValue(value, "LeftItemListVM");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBannerItemsHighlightApplied
	{
		get
		{
			return _isBannerItemsHighlightApplied;
		}
		set
		{
			if (value != _isBannerItemsHighlightApplied)
			{
				_isBannerItemsHighlightApplied = value;
				OnPropertyChangedWithValue(value, "IsBannerItemsHighlightApplied");
			}
		}
	}

	[DataSourceProperty]
	public string BannerTypeName
	{
		get
		{
			return _bannerTypeName;
		}
		set
		{
			if (value != _bannerTypeName)
			{
				_bannerTypeName = value;
				OnPropertyChangedWithValue(value, "BannerTypeName");
			}
		}
	}

	[DataSourceProperty]
	public bool ScrollToItem
	{
		get
		{
			return _scrollToItem;
		}
		set
		{
			if (value != _scrollToItem)
			{
				_scrollToItem = value;
				OnPropertyChangedWithValue(value, "ScrollToItem");
			}
		}
	}

	[DataSourceProperty]
	public string ScrollItemId
	{
		get
		{
			return _scrollItemId;
		}
		set
		{
			if (value != _scrollItemId)
			{
				_scrollItemId = value;
				OnPropertyChangedWithValue(value, "ScrollItemId");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCivilianMode
	{
		get
		{
			return _isCivilianMode;
		}
		set
		{
			if (value != _isCivilianMode)
			{
				_isCivilianMode = value;
				OnPropertyChangedWithValue(value, "IsCivilianMode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBattleMode
	{
		get
		{
			return _isBattleMode;
		}
		set
		{
			if (value != _isBattleMode)
			{
				_isBattleMode = value;
				OnPropertyChangedWithValue(value, "IsBattleMode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsStealthMode
	{
		get
		{
			return _isStealthMode;
		}
		set
		{
			if (value != _isStealthMode)
			{
				_isStealthMode = value;
				OnPropertyChangedWithValue(value, "IsStealthMode");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
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

	[DataSourceProperty]
	public InputKeyItemVM PreviousCharacterInputKey
	{
		get
		{
			return _previousCharacterInputKey;
		}
		set
		{
			if (value != _previousCharacterInputKey)
			{
				_previousCharacterInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousCharacterInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextCharacterInputKey
	{
		get
		{
			return _nextCharacterInputKey;
		}
		set
		{
			if (value != _nextCharacterInputKey)
			{
				_nextCharacterInputKey = value;
				OnPropertyChangedWithValue(value, "NextCharacterInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM BuyAllInputKey
	{
		get
		{
			return _buyAllInputKey;
		}
		set
		{
			if (value != _buyAllInputKey)
			{
				_buyAllInputKey = value;
				OnPropertyChangedWithValue(value, "BuyAllInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM SellAllInputKey
	{
		get
		{
			return _sellAllInputKey;
		}
		set
		{
			if (value != _sellAllInputKey)
			{
				_sellAllInputKey = value;
				OnPropertyChangedWithValue(value, "SellAllInputKey");
			}
		}
	}

	private InventoryLogic.InventorySide GetEquipmentToInventorySide(EquipmentModes equipmentMode)
	{
		return equipmentMode switch
		{
			EquipmentModes.Civilian => InventoryLogic.InventorySide.CivilianEquipment, 
			EquipmentModes.Stealth => InventoryLogic.InventorySide.StealthEquipment, 
			EquipmentModes.Battle => InventoryLogic.InventorySide.BattleEquipment, 
			_ => InventoryLogic.InventorySide.None, 
		};
	}

	public SPInventoryVM(InventoryLogic inventoryLogic, bool isInCivilianModeByDefault, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags)
	{
		IsSearchAvailable = true;
		_usageType = InventoryScreenHelper.GetActiveInventoryState()?.InventoryMode ?? InventoryScreenHelper.InventoryMode.Default;
		_inventoryLogic = inventoryLogic;
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		_getItemUsageSetFlags = getItemUsageSetFlags;
		_filters = new Dictionary<Filters, List<int>>();
		_filters.Add(Filters.All, _everyItemType);
		_filters.Add(Filters.Weapons, _weaponItemTypes);
		_filters.Add(Filters.Armors, _armorItemTypes);
		_filters.Add(Filters.Mounts, _mountItemTypes);
		_filters.Add(Filters.ShieldsAndRanged, _shieldAndRangedItemTypes);
		_filters.Add(Filters.Miscellaneous, _miscellaneousItemTypes);
		_equipAfterTransferStack = new Stack<SPItemVM>();
		_comparedItemList = new List<ItemVM>();
		_donationMaxShareableXp = MobilePartyHelper.GetMaximumXpAmountPartyCanGet(MobileParty.MainParty);
		MBTextManager.SetTextVariable("XP_DONATION_LIMIT", _donationMaxShareableXp);
		if (_inventoryLogic != null)
		{
			_currentCharacter = _inventoryLogic.InitialEquipmentCharacter;
			_isTrading = inventoryLogic.IsTrading;
			_inventoryLogic.AfterReset += AfterReset;
			InventoryLogic inventoryLogic2 = _inventoryLogic;
			inventoryLogic2.TotalAmountChange = (Action<int>)Delegate.Combine(inventoryLogic2.TotalAmountChange, new Action<int>(OnTotalAmountChange));
			InventoryLogic inventoryLogic3 = _inventoryLogic;
			inventoryLogic3.DonationXpChange = (Action)Delegate.Combine(inventoryLogic3.DonationXpChange, new Action(OnDonationXpChange));
			_inventoryLogic.AfterTransfer += AfterTransfer;
			_rightTroopRoster = inventoryLogic.RightMemberRoster;
			_leftTroopRoster = inventoryLogic.LeftMemberRoster;
			_currentInventoryCharacterIndex = _rightTroopRoster.FindIndexOfTroop(_currentCharacter);
			OnDonationXpChange();
			CompanionExists = DoesCompanionExist();
		}
		MainCharacter = new HeroViewModel();
		MainCharacter.FillFrom(_currentCharacter.HeroObject);
		ItemMenu = new ItemMenuVM(ResetComparedItems, _inventoryLogic, _getItemUsageSetFlags, GetItemFromIndex);
		IsRefreshed = false;
		RightItemListVM = new MBBindingList<SPItemVM>();
		LeftItemListVM = new MBBindingList<SPItemVM>();
		CharacterHelmSlot = new SPItemVM();
		CharacterCloakSlot = new SPItemVM();
		CharacterTorsoSlot = new SPItemVM();
		CharacterGloveSlot = new SPItemVM();
		CharacterBootSlot = new SPItemVM();
		CharacterMountSlot = new SPItemVM();
		CharacterMountArmorSlot = new SPItemVM();
		CharacterWeapon1Slot = new SPItemVM();
		CharacterWeapon2Slot = new SPItemVM();
		CharacterWeapon3Slot = new SPItemVM();
		CharacterWeapon4Slot = new SPItemVM();
		CharacterBannerSlot = new SPItemVM();
		ProductionTooltip = new BasicTooltipViewModel();
		CurrentCharacterSkillsTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetInventoryCharacterTooltip(_currentCharacter.HeroObject));
		RefreshCallbacks();
		_selectedEquipmentIndex = 0;
		if (isInCivilianModeByDefault)
		{
			EquipmentMode = 0;
		}
		if (_inventoryLogic != null)
		{
			UpdateRightCharacter();
			UpdateLeftCharacter();
			InitializeInventory();
		}
		RightInventoryOwnerGold = Hero.MainHero.Gold;
		if (_inventoryLogic.OtherSideCapacityData != null)
		{
			OtherSideHasCapacity = _inventoryLogic.OtherSideCapacityData.GetCapacity() != -1;
		}
		IsOtherInventoryGoldRelevant = _usageType != InventoryScreenHelper.InventoryMode.Loot;
		PlayerInventorySortController = new SPInventorySortControllerVM(ref _rightItemListVM);
		OtherInventorySortController = new SPInventorySortControllerVM(ref _leftItemListVM);
		PlayerInventorySortController.SortByDefaultState();
		if (_usageType == InventoryScreenHelper.InventoryMode.Loot)
		{
			OtherInventorySortController.CostState = 1;
			OtherInventorySortController.ExecuteSortByCost();
		}
		else
		{
			OtherInventorySortController.SortByDefaultState();
		}
		Tuple<int, int> tuple = _viewDataTracker.InventoryGetSortPreference((int)_usageType);
		if (tuple != null)
		{
			PlayerInventorySortController.SortByOption((SPInventorySortControllerVM.InventoryItemSortOption)tuple.Item1, (SPInventorySortControllerVM.InventoryItemSortState)tuple.Item2);
		}
		ItemPreview = new ItemPreviewVM(OnPreviewClosed);
		_characterList = new SelectorVM<InventoryCharacterSelectorItemVM>(0, OnCharacterSelected);
		AddApplicableCharactersToListFromRoster(_rightTroopRoster.GetTroopRoster());
		if (_inventoryLogic.IsOtherPartyFromPlayerClan && _leftTroopRoster != null)
		{
			AddApplicableCharactersToListFromRoster(_leftTroopRoster.GetTroopRoster());
		}
		if (_characterList.SelectedIndex == -1 && _characterList.ItemList.Count > 0)
		{
			_characterList.SelectedIndex = 0;
		}
		BannerTypeName = ItemObject.ItemTypeEnum.Banner.ToString();
		InventoryTradeVM.RemoveZeroCounts += ExecuteRemoveZeroCounts;
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		RefreshValues();
	}

	private void AddApplicableCharactersToListFromRoster(MBList<TroopRosterElement> roster)
	{
		for (int i = 0; i < roster.Count; i++)
		{
			CharacterObject character = roster[i].Character;
			if (character.IsHero && CanSelectHero(character.HeroObject))
			{
				_characterList.AddItem(new InventoryCharacterSelectorItemVM(character.StringId, character.HeroObject, character.HeroObject.Name));
				if (character == _currentCharacter)
				{
					_characterList.SelectedIndex = _characterList.ItemList.Count - 1;
				}
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RightInventoryOwnerName = PartyBase.MainParty.Name.ToString();
		SeparatorText = new TextObject("{=dB6cFDmz}/").ToString();
		DoneLbl = GameTexts.FindText("str_done").ToString();
		CancelLbl = GameTexts.FindText("str_cancel").ToString();
		ResetLbl = GameTexts.FindText("str_reset").ToString();
		TypeText = GameTexts.FindText("str_sort_by_type_label").ToString();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		QuantityText = GameTexts.FindText("str_quantity_sign").ToString();
		CostText = GameTexts.FindText("str_value").ToString();
		SearchPlaceholderText = new TextObject("{=tQOPRBFg}Search...").ToString();
		FilterAllHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_all"));
		FilterWeaponHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_weapons"));
		FilterArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_armors"));
		FilterShieldAndRangedHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_shields_ranged"));
		FilterMountAndHarnessHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_mounts"));
		FilterMiscHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_other"));
		CivilianOutfitHint = new HintViewModel(GameTexts.FindText("str_inventory_civilian_outfit"));
		BattleOutfitHint = new HintViewModel(GameTexts.FindText("str_inventory_battle_outfit"));
		StealthOutfitHint = new HintViewModel(GameTexts.FindText("str_inventory_stealth_outfit"));
		EquipmentHelmSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_helm_slot"));
		EquipmentArmorSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_armor_slot"));
		EquipmentBootSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_boot_slot"));
		EquipmentCloakSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_cloak_slot"));
		EquipmentGloveSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_glove_slot"));
		EquipmentHarnessSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_mount_armor_slot"));
		EquipmentMountSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_mount_slot"));
		EquipmentWeaponSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_filter_weapons"));
		EquipmentBannerSlotHint = new HintViewModel(GameTexts.FindText("str_inventory_banner_slot"));
		WeightHint = new HintViewModel(GameTexts.FindText("str_inventory_weight_desc"));
		ArmArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_arm_armor"));
		BodyArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_body_armor"));
		HeadArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_head_armor"));
		LegArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_leg_armor"));
		HorseArmorHint = new HintViewModel(GameTexts.FindText("str_inventory_horse_armor"));
		DonationLblHint = new HintViewModel(GameTexts.FindText("str_inventory_donation_label_hint"));
		SetPreviousCharacterHint();
		SetNextCharacterHint();
		PreviewHint = new HintViewModel(GameTexts.FindText("str_inventory_preview"));
		EquipHint = new HintViewModel(GameTexts.FindText("str_inventory_equip"));
		UnequipHint = new HintViewModel(GameTexts.FindText("str_inventory_unequip"));
		ResetHint = new HintViewModel(GameTexts.FindText("str_reset"));
		PlayerSideCapacityExceededText = GameTexts.FindText("str_capacity_exceeded").ToString();
		PlayerSideCapacityExceededHint = new HintViewModel(GameTexts.FindText("str_capacity_exceeded_hint"));
		MainPartyLandCapacityExceededText = new TextObject("{=fgyvzyB5}Land Capacity Exceeded").ToString();
		MainPartySeaCapacityExceededText = new TextObject("{=7dXs9c2b}Sea Capacity Exceeded").ToString();
		MainPartyLandCapacityExceededHint = new HintViewModel(new TextObject("{=knayk28P}You will slow down on land. Be careful."));
		MainPartySeaCapacityExceededHint = new HintViewModel(new TextObject("{=zoX9akov}You will slow down at sea. Be careful."));
		if (_inventoryLogic.OtherSideCapacityData != null)
		{
			OtherSideCapacityExceededText = _inventoryLogic.OtherSideCapacityData.GetCapacityExceededWarningText()?.ToString();
			OtherSideCapacityExceededHint = new HintViewModel(_inventoryLogic.OtherSideCapacityData.GetCapacityExceededHintText());
		}
		SetBuyAllHint();
		SetSellAllHint();
		if (_usageType == InventoryScreenHelper.InventoryMode.Loot || _usageType == InventoryScreenHelper.InventoryMode.Stash)
		{
			SellHint = new HintViewModel(GameTexts.FindText("str_give"));
		}
		else if (_usageType == InventoryScreenHelper.InventoryMode.Default)
		{
			SellHint = new HintViewModel(GameTexts.FindText("str_inventory_discard"));
		}
		else
		{
			SellHint = new HintViewModel(GameTexts.FindText("str_inventory_sell"));
		}
		CharacterHelmSlot.RefreshValues();
		CharacterCloakSlot.RefreshValues();
		CharacterTorsoSlot.RefreshValues();
		CharacterGloveSlot.RefreshValues();
		CharacterBootSlot.RefreshValues();
		CharacterMountSlot.RefreshValues();
		CharacterMountArmorSlot.RefreshValues();
		CharacterWeapon1Slot.RefreshValues();
		CharacterWeapon2Slot.RefreshValues();
		CharacterWeapon3Slot.RefreshValues();
		CharacterWeapon4Slot.RefreshValues();
		CharacterBannerSlot.RefreshValues();
		PlayerInventorySortController?.RefreshValues();
		OtherInventorySortController?.RefreshValues();
	}

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
		InventoryTradeVM.RemoveZeroCounts -= ExecuteRemoveZeroCounts;
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		ItemPreview.OnFinalize();
		ItemPreview = null;
		CancelInputKey.OnFinalize();
		DoneInputKey.OnFinalize();
		ResetInputKey.OnFinalize();
		PreviousCharacterInputKey.OnFinalize();
		NextCharacterInputKey.OnFinalize();
		BuyAllInputKey.OnFinalize();
		SellAllInputKey.OnFinalize();
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
		MainCharacter.OnFinalize();
		_inventoryLogic = null;
		base.OnFinalize();
	}

	public void RefreshCallbacks()
	{
		ItemVM.ProcessEquipItem = ProcessEquipItem;
		ItemVM.ProcessUnequipItem = ProcessUnequipItem;
		ItemVM.ProcessPreviewItem = ProcessPreviewItem;
		ItemVM.ProcessBuyItem = ProcessBuyItem;
		SPItemVM.ProcessLockItem = ProcessLockItem;
		SPItemVM.ProcessSellItem = ProcessSellItem;
		ItemVM.ProcessItemSelect = ProcessItemSelect;
		ItemVM.ProcessItemTooltip = ProcessItemTooltip;
		SPItemVM.ProcessItemSlaughter = ProcessItemSlaughter;
		SPItemVM.ProcessItemDonate = ProcessItemDonate;
		SPItemVM.OnFocus = OnItemFocus;
	}

	private bool CanSelectHero(Hero hero)
	{
		if (hero.IsAlive && hero.CanHeroEquipmentBeChanged() && hero.Clan == Clan.PlayerClan && hero.HeroState != Hero.CharacterStates.Disabled)
		{
			return !hero.IsChild;
		}
		return false;
	}

	private void OnEquipmentModeChanged()
	{
		IsCivilianMode = EquipmentMode == 0;
		IsBattleMode = EquipmentMode == 1;
		IsStealthMode = EquipmentMode == 2;
	}

	private void SetPreviousCharacterHint()
	{
		PreviousCharacterHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("HOTKEY", GetPreviousCharacterKeyText());
			GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_prev_char"));
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
	}

	private void SetNextCharacterHint()
	{
		NextCharacterHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("HOTKEY", GetNextCharacterKeyText());
			GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_next_char"));
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
	}

	private void SetBuyAllHint()
	{
		TextObject buyAllHintText;
		if (_usageType == InventoryScreenHelper.InventoryMode.Trade)
		{
			buyAllHintText = GameTexts.FindText("str_inventory_buy_all");
		}
		else
		{
			buyAllHintText = GameTexts.FindText("str_inventory_take_all");
		}
		BuyAllHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("HOTKEY", GetBuyAllKeyText());
			GameTexts.SetVariable("TEXT", buyAllHintText);
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
	}

	private void SetSellAllHint()
	{
		TextObject sellAllHintText;
		if (_usageType == InventoryScreenHelper.InventoryMode.Loot || _usageType == InventoryScreenHelper.InventoryMode.Stash)
		{
			sellAllHintText = GameTexts.FindText("str_inventory_give_all");
		}
		else if (_usageType == InventoryScreenHelper.InventoryMode.Default)
		{
			sellAllHintText = GameTexts.FindText("str_inventory_discard_all");
		}
		else
		{
			sellAllHintText = GameTexts.FindText("str_inventory_sell_all");
		}
		SellAllHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("HOTKEY", GetSellAllKeyText());
			GameTexts.SetVariable("TEXT", sellAllHintText);
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
	}

	private void OnCharacterSelected(SelectorVM<InventoryCharacterSelectorItemVM> selector)
	{
		if (_inventoryLogic == null || selector.SelectedItem == null)
		{
			return;
		}
		for (int i = 0; i < _rightTroopRoster.Count; i++)
		{
			if (_rightTroopRoster.GetCharacterAtIndex(i).StringId == selector.SelectedItem.CharacterID)
			{
				UpdateCurrentCharacterIfPossible(i, isFromRightSide: true);
				return;
			}
		}
		if (_leftTroopRoster == null)
		{
			return;
		}
		for (int j = 0; j < _leftTroopRoster.Count; j++)
		{
			if (_leftTroopRoster.GetCharacterAtIndex(j).StringId == selector.SelectedItem.CharacterID)
			{
				UpdateCurrentCharacterIfPossible(j, isFromRightSide: false);
				break;
			}
		}
	}

	public void ExecuteShowRecap()
	{
		InformationManager.ShowTooltip(typeof(InventoryLogic), _inventoryLogic);
	}

	public void ExecuteCancelRecap()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteRemoveZeroCounts()
	{
		List<SPItemVM> list = LeftItemListVM.ToList();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num].ItemCount == 0 && num >= 0 && num < LeftItemListVM.Count)
			{
				list[num].IsSelected = false;
				LeftItemListVM.RemoveAt(num);
			}
		}
		List<SPItemVM> list2 = RightItemListVM.ToList();
		for (int num2 = list2.Count - 1; num2 >= 0; num2--)
		{
			if (list2[num2].ItemCount == 0 && num2 >= 0 && num2 < RightItemListVM.Count)
			{
				list2[num2].IsSelected = false;
				RightItemListVM.RemoveAt(num2);
			}
		}
	}

	private void ProcessPreviewItem(ItemVM item)
	{
		_inventoryLogic.IsPreviewingItem = true;
		ItemPreview.Open(item.ItemRosterElement.EquipmentElement);
	}

	public void ClosePreview()
	{
		ItemPreview.Close();
	}

	private void OnPreviewClosed()
	{
		_inventoryLogic.IsPreviewingItem = false;
	}

	private void ProcessEquipItem(ItemVM draggedItem)
	{
		SPItemVM sPItemVM = draggedItem as SPItemVM;
		if (sPItemVM.IsTransferable || _currentCharacter.IsPlayerCharacter)
		{
			IsRefreshed = false;
			EquipEquipment(sPItemVM);
			RefreshInformationValues();
			ExecuteRemoveZeroCounts();
			IsRefreshed = true;
		}
	}

	private void ProcessUnequipItem(ItemVM draggedItem)
	{
		if ((draggedItem as SPItemVM).IsTransferable || _currentCharacter.IsPlayerCharacter)
		{
			IsRefreshed = false;
			UnequipEquipment(draggedItem as SPItemVM);
			RefreshInformationValues();
			IsRefreshed = true;
		}
	}

	private void ProcessBuyItem(ItemVM itemBase, bool cameFromTradeData)
	{
		if (!(itemBase is SPItemVM { IsTransferable: not false } sPItemVM))
		{
			return;
		}
		if (IsEntireStackModifierActive && !cameFromTradeData)
		{
			TransactionCount = _inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.OtherInventory, sPItemVM?.ItemRosterElement.EquipmentElement ?? EquipmentElement.Invalid)?.Amount ?? 0;
		}
		else if (IsFiveStackModifierActive && !cameFromTradeData)
		{
			TransactionCount = 5;
		}
		else
		{
			TransactionCount = sPItemVM?.TransactionCount ?? 0;
		}
		if (TransactionCount == 0)
		{
			Debug.FailedAssert("Transaction count should not be zero", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ProcessBuyItem", 634);
			return;
		}
		IsRefreshed = false;
		MBTextManager.SetTextVariable("ITEM_DESCRIPTION", itemBase.ItemDescription);
		MBTextManager.SetTextVariable("ITEM_COST", itemBase.ItemCost);
		BuyItem(sPItemVM);
		if (!cameFromTradeData)
		{
			ExecuteRemoveZeroCounts();
		}
		RefreshInformationValues();
		IsRefreshed = true;
	}

	private void ProcessSellItem(SPItemVM item, bool cameFromTradeData)
	{
		if (!item.IsTransferable)
		{
			return;
		}
		if (InventoryLogic.IsEquipmentSide(item.InventorySide))
		{
			TransactionCount = 1;
		}
		else if (IsEntireStackModifierActive && !cameFromTradeData)
		{
			TransactionCount = _inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.PlayerInventory, item.ItemRosterElement.EquipmentElement)?.Amount ?? 0;
		}
		else if (IsFiveStackModifierActive && !cameFromTradeData)
		{
			TransactionCount = 5;
		}
		else
		{
			TransactionCount = item.TransactionCount;
		}
		if (TransactionCount == 0)
		{
			Debug.FailedAssert("Transaction count should not be zero", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ProcessSellItem", 684);
			return;
		}
		IsRefreshed = false;
		MBTextManager.SetTextVariable("ITEM_DESCRIPTION", item.ItemDescription);
		MBTextManager.SetTextVariable("ITEM_COST", item.ItemCost);
		SellItem(item);
		if (!cameFromTradeData)
		{
			ExecuteRemoveZeroCounts();
		}
		RefreshInformationValues();
		IsRefreshed = true;
	}

	private void ProcessLockItem(SPItemVM item, bool isLocked)
	{
		if (isLocked && item.InventorySide == InventoryLogic.InventorySide.PlayerInventory && !_lockedItemIDs.Contains(item.StringId))
		{
			_lockedItemIDs.Add(item.StringId);
		}
		else if (!isLocked && item.InventorySide == InventoryLogic.InventorySide.PlayerInventory && _lockedItemIDs.Contains(item.StringId))
		{
			_lockedItemIDs.Remove(item.StringId);
		}
	}

	private ItemVM ProcessCompareItem(ItemVM item, int alternativeUsageIndex = 0)
	{
		_selectedEquipmentIndex = 0;
		_comparedItemList.Clear();
		ItemVM itemVM = null;
		bool flag = false;
		EquipmentIndex equipmentIndex = EquipmentIndex.None;
		SPItemVM sPItemVM = null;
		bool flag2 = item.ItemType >= EquipmentIndex.WeaponItemBeginSlot && item.ItemType < EquipmentIndex.ExtraWeaponSlot;
		if (!InventoryLogic.IsEquipmentSide(((SPItemVM)item).InventorySide))
		{
			if (flag2)
			{
				for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.ExtraWeaponSlot; equipmentIndex2++)
				{
					EquipmentIndex itemType = equipmentIndex2;
					SPItemVM itemFromIndex = GetItemFromIndex(itemType);
					if (itemFromIndex != null && itemFromIndex.ItemRosterElement.EquipmentElement.Item != null && ItemHelper.CheckComparability(item.ItemRosterElement.EquipmentElement.Item, itemFromIndex.ItemRosterElement.EquipmentElement.Item, alternativeUsageIndex))
					{
						_comparedItemList.Add(itemFromIndex);
					}
				}
				if (!_comparedItemList.IsEmpty())
				{
					SortComparedItems(item);
					itemVM = _comparedItemList[0];
					_lastComparedItemIndex = 0;
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
			sPItemVM = ((equipmentIndex != EquipmentIndex.None) ? GetItemFromIndex(equipmentIndex) : null);
			flag = sPItemVM != null && !string.IsNullOrEmpty(sPItemVM.StringId) && item.StringId != sPItemVM.StringId;
		}
		if (!_selectedTooltipItemStringID.Equals(item.StringId) || (flag && !_comparedTooltipItemStringID.Equals(sPItemVM.StringId)))
		{
			_selectedTooltipItemStringID = item.StringId;
			if (flag)
			{
				_comparedTooltipItemStringID = sPItemVM.StringId;
			}
		}
		_selectedEquipmentIndex = (int)equipmentIndex;
		if (sPItemVM == null || sPItemVM.ItemRosterElement.IsEmpty)
		{
			return null;
		}
		return sPItemVM;
	}

	private void ResetComparedItems(ItemVM item, int alternativeUsageIndex)
	{
		ItemVM comparedItem = ProcessCompareItem(item, alternativeUsageIndex);
		ItemMenu.SetItem(_selectedItem, GetEquipmentToInventorySide((EquipmentModes)EquipmentMode), comparedItem, _currentCharacter, alternativeUsageIndex);
	}

	private void SortComparedItems(ItemVM selectedItem)
	{
		List<ItemVM> list = new List<ItemVM>();
		for (int i = 0; i < _comparedItemList.Count; i++)
		{
			if (selectedItem.StringId == _comparedItemList[i].StringId && !list.Contains(_comparedItemList[i]))
			{
				list.Add(_comparedItemList[i]);
			}
		}
		for (int j = 0; j < _comparedItemList.Count; j++)
		{
			if (_comparedItemList[j].ItemRosterElement.EquipmentElement.Item.Type == selectedItem.ItemRosterElement.EquipmentElement.Item.Type && !list.Contains(_comparedItemList[j]))
			{
				list.Add(_comparedItemList[j]);
			}
		}
		for (int k = 0; k < _comparedItemList.Count; k++)
		{
			WeaponComponent weaponComponent = _comparedItemList[k].ItemRosterElement.EquipmentElement.Item.WeaponComponent;
			WeaponComponent weaponComponent2 = selectedItem.ItemRosterElement.EquipmentElement.Item.WeaponComponent;
			if (((weaponComponent2.Weapons.Count > 1 && weaponComponent2.Weapons[1].WeaponClass == weaponComponent.Weapons[0].WeaponClass) || (weaponComponent.Weapons.Count > 1 && weaponComponent.Weapons[1].WeaponClass == weaponComponent2.Weapons[0].WeaponClass) || (weaponComponent2.Weapons.Count > 1 && weaponComponent.Weapons.Count > 1 && weaponComponent2.Weapons[1].WeaponClass == weaponComponent.Weapons[1].WeaponClass)) && !list.Contains(_comparedItemList[k]))
			{
				list.Add(_comparedItemList[k]);
			}
		}
		if (_comparedItemList.Count != list.Count)
		{
			foreach (ItemVM comparedItem in _comparedItemList)
			{
				if (!list.Contains(comparedItem))
				{
					list.Add(comparedItem);
				}
			}
		}
		_comparedItemList = list;
	}

	public void ProcessItemTooltip(ItemVM item)
	{
		if (item != null && !string.IsNullOrEmpty(item.StringId))
		{
			_selectedItem = item as SPItemVM;
			ItemVM comparedItem = ProcessCompareItem(item);
			ItemMenu.SetItem(_selectedItem, GetEquipmentToInventorySide((EquipmentModes)EquipmentMode), comparedItem, _currentCharacter);
			RefreshTransactionCost();
			_selectedItem.UpdateCanBeSlaughtered();
		}
	}

	public void ResetSelectedItem()
	{
		_selectedItem = null;
	}

	private void ProcessItemSlaughter(SPItemVM item)
	{
		IsRefreshed = false;
		if (!string.IsNullOrEmpty(item.StringId) && item.CanBeSlaughtered)
		{
			SlaughterItem(item);
			RefreshInformationValues();
			if (item.ItemCount == 0)
			{
				ExecuteRemoveZeroCounts();
			}
			IsRefreshed = true;
		}
	}

	private void ProcessItemDonate(SPItemVM item)
	{
		IsRefreshed = false;
		if (!string.IsNullOrEmpty(item.StringId) && item.CanBeDonated)
		{
			DonateItem(item);
			RefreshInformationValues();
			if (item.ItemCount == 0)
			{
				ExecuteRemoveZeroCounts();
			}
			IsRefreshed = true;
		}
	}

	private void OnItemFocus(SPItemVM item)
	{
		CurrentFocusedItem = item;
	}

	private void ProcessItemSelect(ItemVM item)
	{
		ExecuteRemoveZeroCounts();
		ExecuteSelectItem(item);
	}

	private void RefreshTransactionCost(int transactionCount = 1)
	{
		if (_selectedItem != null && IsTrading)
		{
			int lastPrice;
			int itemTotalPrice = _inventoryLogic.GetItemTotalPrice(_selectedItem.ItemRosterElement, transactionCount, out lastPrice, _selectedItem.InventorySide == InventoryLogic.InventorySide.OtherInventory);
			ItemMenu.SetTransactionCost(itemTotalPrice, lastPrice);
		}
	}

	public void RefreshComparedItem()
	{
		_lastComparedItemIndex++;
		if (_lastComparedItemIndex > _comparedItemList.Count - 1)
		{
			_lastComparedItemIndex = 0;
		}
		if (!_comparedItemList.IsEmpty() && _selectedItem != null && _comparedItemList[_lastComparedItemIndex] != null)
		{
			ItemMenu.SetItem(_selectedItem, GetEquipmentToInventorySide((EquipmentModes)EquipmentMode), _comparedItemList[_lastComparedItemIndex], _currentCharacter);
		}
	}

	private void AfterReset(InventoryLogic itemRoster, bool fromCancel)
	{
		_inventoryLogic = itemRoster;
		if (!fromCancel)
		{
			switch ((Filters)ActiveFilterIndex)
			{
			case Filters.Armors:
				_inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Armors;
				break;
			case Filters.Weapons:
				_inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Weapon;
				break;
			case Filters.ShieldsAndRanged:
				_inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Shield;
				break;
			case Filters.Mounts:
				_inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.HorseCategory;
				break;
			case Filters.Miscellaneous:
				_inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.Goods;
				break;
			default:
				_inventoryLogic.MerchantItemType = InventoryScreenHelper.InventoryCategoryType.All;
				break;
			}
			InitializeInventory();
			PlayerInventorySortController = new SPInventorySortControllerVM(ref _rightItemListVM);
			OtherInventorySortController = new SPInventorySortControllerVM(ref _leftItemListVM);
			PlayerInventorySortController.SortByDefaultState();
			OtherInventorySortController.SortByDefaultState();
			Tuple<int, int> tuple = _viewDataTracker.InventoryGetSortPreference((int)_usageType);
			if (tuple != null)
			{
				PlayerInventorySortController.SortByOption((SPInventorySortControllerVM.InventoryItemSortOption)tuple.Item1, (SPInventorySortControllerVM.InventoryItemSortState)tuple.Item2);
			}
			UpdateRightCharacter();
			UpdateLeftCharacter();
			RightInventoryOwnerName = PartyBase.MainParty.Name.ToString();
			RightInventoryOwnerGold = Hero.MainHero.Gold;
		}
	}

	private void OnTotalAmountChange(int newTotalAmount)
	{
		MBTextManager.SetTextVariable("PAY_OR_GET", (_inventoryLogic.TotalAmount < 0) ? 1 : 0);
		int f = TaleWorlds.Library.MathF.Min(-_inventoryLogic.TotalAmount, _inventoryLogic.InventoryListener.GetGold());
		MBTextManager.SetTextVariable("TRADE_AMOUNT", TaleWorlds.Library.MathF.Abs(f));
		TradeLbl = ((_inventoryLogic.TotalAmount == 0) ? "" : GameTexts.FindText("str_inventory_trade_label").ToString());
		RightInventoryOwnerGold = Hero.MainHero.Gold - _inventoryLogic.TotalAmount;
		LeftInventoryOwnerGold = (_inventoryLogic.InventoryListener?.GetGold() + _inventoryLogic.TotalAmount) ?? 0;
	}

	private void OnDonationXpChange()
	{
		int num = (int)_inventoryLogic.XpGainFromDonations;
		bool isDonationXpGainExceedsMax = false;
		if (num > _donationMaxShareableXp)
		{
			num = _donationMaxShareableXp;
			isDonationXpGainExceedsMax = true;
		}
		IsDonationXpGainExceedsMax = isDonationXpGainExceedsMax;
		HasGainedExperience = num > 0;
		MBTextManager.SetTextVariable("XP_AMOUNT", num);
		ExperienceLbl = ((num == 0) ? "" : GameTexts.FindText("str_inventory_donation_label").ToString());
	}

	private void AfterTransfer(InventoryLogic inventoryLogic, List<TransferCommandResult> results)
	{
		_isCharacterEquipmentDirty = false;
		List<SPItemVM> list = new List<SPItemVM>();
		HashSet<ItemCategory> hashSet = new HashSet<ItemCategory>();
		for (int i = 0; i != results.Count; i++)
		{
			TransferCommandResult transferCommandResult = results[i];
			if (transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory || transferCommandResult.ResultSide == InventoryLogic.InventorySide.PlayerInventory)
			{
				bool flag = false;
				MBBindingList<SPItemVM> mBBindingList = ((transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory) ? LeftItemListVM : RightItemListVM);
				for (int j = 0; j < mBBindingList.Count; j++)
				{
					SPItemVM sPItemVM = mBBindingList[j];
					if (sPItemVM != null && sPItemVM.ItemRosterElement.EquipmentElement.IsEqualTo(transferCommandResult.EffectedItemRosterElement.EquipmentElement))
					{
						sPItemVM.ItemRosterElement.Amount = transferCommandResult.FinalNumber;
						sPItemVM.ItemCount = transferCommandResult.FinalNumber;
						sPItemVM.ItemCost = _inventoryLogic.GetItemPrice(sPItemVM.ItemRosterElement.EquipmentElement, transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory);
						list.Add(sPItemVM);
						if (!hashSet.Contains(sPItemVM.ItemRosterElement.EquipmentElement.Item.GetItemCategory()))
						{
							hashSet.Add(sPItemVM.ItemRosterElement.EquipmentElement.Item.GetItemCategory());
						}
						if (sPItemVM.IsSelected)
						{
							ScrollItemId = sPItemVM.ItemRosterElement.EquipmentElement.Item.StringId;
							ScrollToItem = true;
						}
						flag = true;
						break;
					}
				}
				if (flag || transferCommandResult.EffectedNumber <= 0 || _inventoryLogic == null)
				{
					continue;
				}
				SPItemVM newItem = null;
				SPItemVM sPItemVM2 = null;
				if (transferCommandResult.ResultSide == InventoryLogic.InventorySide.OtherInventory)
				{
					newItem = new SPItemVM(_inventoryLogic, MainCharacter.IsFemale, CanCharacterUseItem(transferCommandResult.EffectedItemRosterElement), _usageType, transferCommandResult.EffectedItemRosterElement, InventoryLogic.InventorySide.OtherInventory, _inventoryLogic.GetCostOfItemRosterElement(transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide), null);
					sPItemVM2 = RightItemListVM.FirstOrDefault((SPItemVM x) => x.ItemRosterElement.EquipmentElement.IsEqualTo(newItem.ItemRosterElement.EquipmentElement));
				}
				else
				{
					newItem = new SPItemVM(_inventoryLogic, MainCharacter.IsFemale, CanCharacterUseItem(transferCommandResult.EffectedItemRosterElement), _usageType, transferCommandResult.EffectedItemRosterElement, InventoryLogic.InventorySide.PlayerInventory, _inventoryLogic.GetCostOfItemRosterElement(transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide), null);
					sPItemVM2 = LeftItemListVM.FirstOrDefault((SPItemVM x) => x.ItemRosterElement.EquipmentElement.IsEqualTo(newItem.ItemRosterElement.EquipmentElement));
				}
				UpdateFilteredStatusOfItem(newItem);
				newItem.ItemCount = transferCommandResult.FinalNumber;
				newItem.IsLocked = newItem.InventorySide == InventoryLogic.InventorySide.PlayerInventory && _lockedItemIDs.Contains(newItem.StringId);
				newItem.IsNew = true;
				newItem.IsSelected = sPItemVM2?.IsSelected ?? false;
				mBBindingList.Add(newItem);
				if (newItem.IsSelected)
				{
					ScrollItemId = transferCommandResult.EffectedItemRosterElement.EquipmentElement.Item.StringId;
					ScrollToItem = true;
				}
			}
			else if (InventoryLogic.IsEquipmentSide(transferCommandResult.ResultSide))
			{
				SPItemVM sPItemVM3 = null;
				if (transferCommandResult.FinalNumber > 0)
				{
					sPItemVM3 = new SPItemVM(_inventoryLogic, MainCharacter.IsFemale, CanCharacterUseItem(transferCommandResult.EffectedItemRosterElement), _usageType, transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide, _inventoryLogic.GetCostOfItemRosterElement(transferCommandResult.EffectedItemRosterElement, transferCommandResult.ResultSide), transferCommandResult.EffectedEquipmentIndex);
					sPItemVM3.IsNew = true;
				}
				UpdateEquipment(transferCommandResult.ResultSideEquipment, sPItemVM3, transferCommandResult.EffectedEquipmentIndex);
				_isCharacterEquipmentDirty = true;
			}
		}
		SPItemVM selectedItem = _selectedItem;
		if (selectedItem != null && selectedItem.ItemCount > 1)
		{
			ProcessItemTooltip(_selectedItem);
			_selectedItem.UpdateCanBeSlaughtered();
		}
		CheckEquipAfterTransferStack();
		if (!ActiveEquipment[EquipmentIndex.HorseHarness].IsEmpty && ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
		{
			UnequipEquipment(CharacterMountArmorSlot);
		}
		if (!ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty && !ActiveEquipment[EquipmentIndex.HorseHarness].IsEmpty && ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent.Monster.FamilyType != ActiveEquipment[EquipmentIndex.HorseHarness].Item.ArmorComponent.FamilyType)
		{
			UnequipEquipment(CharacterMountArmorSlot);
		}
		foreach (SPItemVM item in list)
		{
			item.UpdateTradeData(forceUpdateAmounts: true);
			item.UpdateCanBeSlaughtered();
		}
		UpdateCostOfItemsInCategory(hashSet);
		if (PartyBase.MainParty.IsMobile)
		{
			PartyBase.MainParty.MobileParty.MemberRoster.UpdateVersion();
			PartyBase.MainParty.MobileParty.PrisonRoster.UpdateVersion();
		}
	}

	private void UpdateCostOfItemsInCategory(HashSet<ItemCategory> categories)
	{
		foreach (SPItemVM item in LeftItemListVM)
		{
			if (categories.Contains(item.ItemRosterElement.EquipmentElement.Item.GetItemCategory()))
			{
				item.ItemCost = _inventoryLogic.GetCostOfItemRosterElement(item.ItemRosterElement, InventoryLogic.InventorySide.OtherInventory);
			}
		}
		foreach (SPItemVM item2 in RightItemListVM)
		{
			if (categories.Contains(item2.ItemRosterElement.EquipmentElement.Item.GetItemCategory()))
			{
				item2.ItemCost = _inventoryLogic.GetCostOfItemRosterElement(item2.ItemRosterElement, InventoryLogic.InventorySide.PlayerInventory);
			}
		}
	}

	private void CheckEquipAfterTransferStack()
	{
		while (_equipAfterTransferStack.Count > 0)
		{
			SPItemVM sPItemVM = new SPItemVM();
			sPItemVM.RefreshWith(_equipAfterTransferStack.Pop(), InventoryLogic.InventorySide.PlayerInventory);
			EquipEquipment(sPItemVM);
		}
	}

	private void RefreshInformationValues()
	{
		TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT");
		int inventoryCapacity = MobileParty.MainParty.InventoryCapacity;
		float totalWeightCarried = MobileParty.MainParty.TotalWeightCarried;
		MainPartyTotalWeightCarriedText = totalWeightCarried.ToString("#0");
		MainPartyInventoryCapacityText = inventoryCapacity.ToString();
		PlayerEquipmentCountWarned = totalWeightCarried > (float)inventoryCapacity;
		ShowMainPartyLandCapacityTexts = MobileParty.MainParty.IsCurrentlyAtSea;
		ShowMainPartySeaCapacityTexts = !ShowMainPartyLandCapacityTexts && MobileParty.MainParty.HasNavalNavigationCapability;
		if (ShowMainPartyLandCapacityTexts)
		{
			int num = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(MobileParty.MainParty, isCurrentlyAtSea: false).ResultNumber;
			int num2 = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(MobileParty.MainParty, isCurrentlyAtSea: false).ResultNumber;
			MainPartyLandWeightText = num.ToString();
			MainPartyLandCapacityText = num2.ToString();
			IsMainPartyLandCapacityWarned = num > num2;
		}
		else if (ShowMainPartySeaCapacityTexts)
		{
			int num3 = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(MobileParty.MainParty, isCurrentlyAtSea: true).ResultNumber;
			int num4 = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(MobileParty.MainParty, isCurrentlyAtSea: true).ResultNumber;
			MainPartySeaWeightText = num3.ToString();
			MainPartySeaCapacityText = num4.ToString();
			IsMainPartySeaCapacityWarned = num3 > num4;
		}
		ShowMainPartyLandCapacityWarning = ShowMainPartyLandCapacityTexts && IsMainPartyLandCapacityWarned && !PlayerEquipmentCountWarned;
		ShowMainPartySeaCapacityWarning = ShowMainPartySeaCapacityTexts && IsMainPartySeaCapacityWarned && !PlayerEquipmentCountWarned;
		if (OtherSideHasCapacity)
		{
			int otherSideCurrentWeight = _inventoryLogic.OtherSideCurrentWeight;
			int capacity = _inventoryLogic.OtherSideCapacityData.GetCapacity();
			textObject.SetTextVariable("LEFT", otherSideCurrentWeight);
			textObject.SetTextVariable("RIGHT", capacity);
			OtherEquipmentCountText = textObject.ToString();
			OtherEquipmentCountWarned = otherSideCurrentWeight > capacity;
			OtherEquipmentCapacityExceededWarning = OtherEquipmentCountWarned && !PlayerEquipmentCountWarned && !IsMainPartyLandCapacityWarned && !IsMainPartySeaCapacityWarned;
		}
		NoSaddleText = new TextObject("{=QSPrSsHv}No Saddle!").ToString();
		NoSaddleHint = new HintViewModel(new TextObject("{=VzCoqt8D}No sadle equipped. -10% penalty to mounted speed and maneuver."));
		SPItemVM characterMountSlot = CharacterMountSlot;
		NoSaddleWarned = characterMountSlot != null && !characterMountSlot.ItemRosterElement.IsEmpty && (CharacterMountArmorSlot?.ItemRosterElement.IsEmpty ?? false);
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			InventoryCapacityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryCapacityTooltip(MobileParty.MainParty));
			LandCapacityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryCapacityTooltip(MobileParty.MainParty, forceLand: true));
			SeaCapacityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryCapacityTooltip(MobileParty.MainParty, forceLand: false, forceSea: true));
			TotalWeightCarriedHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryWeightTooltip(MobileParty.MainParty));
			LandWeightHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryWeightTooltip(MobileParty.MainParty, forceLand: true));
			SeaWeightHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryWeightTooltip(MobileParty.MainParty, forceLand: false, forceSea: true));
		}
		if (_isCharacterEquipmentDirty)
		{
			MainCharacter.SetEquipment(ActiveEquipment);
			UpdateCharacterArmorValues();
			RefreshCharacterTotalWeight();
		}
		_isCharacterEquipmentDirty = false;
		UpdateIsDoneDisabled();
	}

	public bool IsItemEquipmentPossible(SPItemVM itemVM)
	{
		if (itemVM == null)
		{
			return false;
		}
		if (!itemVM.IsStealthItem && _equipmentMode == EquipmentModes.Stealth)
		{
			TextObject textObject = new TextObject("{=*}{ITEM_NAME} cannot be equipped with stealth equipment.");
			textObject.SetTextVariable("ITEM_NAME", itemVM.ItemRosterElement.EquipmentElement.GetModifiedItemName());
			MBInformationManager.AddQuickInformation(textObject);
			return false;
		}
		if (!itemVM.IsCivilianItem && _equipmentMode == EquipmentModes.Civilian)
		{
			TextObject textObject2 = new TextObject("{=*}{ITEM_NAME} cannot be equipped with civilian equipment.");
			textObject2.SetTextVariable("ITEM_NAME", itemVM.ItemRosterElement.EquipmentElement.GetModifiedItemName());
			MBInformationManager.AddQuickInformation(textObject2);
			return false;
		}
		if (!_currentCharacter.IsPlayerCharacter && (itemVM == null || !itemVM.IsTransferable))
		{
			return false;
		}
		if (TargetEquipmentType == EquipmentIndex.None)
		{
			TargetEquipmentType = itemVM.GetItemTypeWithItemObject();
			if (TargetEquipmentType == EquipmentIndex.None)
			{
				return false;
			}
			if (TargetEquipmentType == EquipmentIndex.WeaponItemBeginSlot)
			{
				EquipmentIndex targetEquipmentType = EquipmentIndex.WeaponItemBeginSlot;
				bool flag = false;
				bool flag2 = false;
				SPItemVM[] array = new SPItemVM[4] { CharacterWeapon1Slot, CharacterWeapon2Slot, CharacterWeapon3Slot, CharacterWeapon4Slot };
				for (int i = 0; i < array.Length; i++)
				{
					if (string.IsNullOrEmpty(array[i].StringId))
					{
						flag = true;
						targetEquipmentType = (EquipmentIndex)(0 + i);
						break;
					}
					if (!flag2 && array[i].ItemRosterElement.EquipmentElement.Item.Type == itemVM.ItemRosterElement.EquipmentElement.Item.Type)
					{
						flag2 = true;
						targetEquipmentType = (EquipmentIndex)(0 + i);
					}
				}
				if (flag || flag2)
				{
					TargetEquipmentType = targetEquipmentType;
				}
				else
				{
					TargetEquipmentType = EquipmentIndex.WeaponItemBeginSlot;
				}
			}
		}
		else if (itemVM.ItemType != TargetEquipmentType && (TargetEquipmentType < EquipmentIndex.WeaponItemBeginSlot || TargetEquipmentType > EquipmentIndex.Weapon3 || itemVM.ItemType < EquipmentIndex.WeaponItemBeginSlot || itemVM.ItemType > EquipmentIndex.Weapon3))
		{
			return false;
		}
		if (!CanCharacterUseItem(itemVM.ItemRosterElement, out var reason))
		{
			MBInformationManager.AddQuickInformation(reason);
			return false;
		}
		if (!Equipment.IsItemFitsToSlot((EquipmentIndex)TargetEquipmentIndex, itemVM.ItemRosterElement.EquipmentElement.Item))
		{
			TextObject textObject3 = new TextObject("{=Omjlnsk3}{ITEM_NAME} cannot be equipped on this slot.");
			textObject3.SetTextVariable("ITEM_NAME", itemVM.ItemRosterElement.EquipmentElement.GetModifiedItemName());
			MBInformationManager.AddQuickInformation(textObject3);
			return false;
		}
		if (TargetEquipmentType == EquipmentIndex.HorseHarness)
		{
			if (string.IsNullOrEmpty(CharacterMountSlot.StringId))
			{
				return false;
			}
			if (!ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty && ActiveEquipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent.Monster.FamilyType != itemVM.ItemRosterElement.EquipmentElement.Item.ArmorComponent.FamilyType)
			{
				return false;
			}
		}
		return true;
	}

	private bool CanCharacterUseItem(ItemRosterElement itemRosterElement, out TextObject reason)
	{
		return CharacterHelper.CanUseItem(_currentCharacter, itemRosterElement.EquipmentElement, out reason);
	}

	private bool CanCharacterUseItem(ItemRosterElement itemRosterElement)
	{
		return CharacterHelper.CanUseItem(_currentCharacter, itemRosterElement.EquipmentElement);
	}

	private void EquipEquipment(SPItemVM itemVM)
	{
		if (itemVM == null || string.IsNullOrEmpty(itemVM.StringId))
		{
			return;
		}
		SPItemVM sPItemVM = new SPItemVM();
		sPItemVM.RefreshWith(itemVM, GetEquipmentToInventorySide(_equipmentMode));
		if (!IsItemEquipmentPossible(sPItemVM))
		{
			return;
		}
		SPItemVM itemFromIndex = GetItemFromIndex(TargetEquipmentType);
		if (itemFromIndex != null && itemFromIndex.ItemRosterElement.EquipmentElement.IsEqualTo(sPItemVM.ItemRosterElement.EquipmentElement))
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
					SPItemVM itemFromIndex2 = GetItemFromIndex(equipmentIndex2);
					if (itemFromIndex2 != null && itemFromIndex2.ItemRosterElement.EquipmentElement.Item?.Type == ItemObject.ItemTypeEnum.Shield)
					{
						equipmentIndex = equipmentIndex2;
						break;
					}
				}
			}
			if (itemVM != null && itemVM.ItemRosterElement.EquipmentElement.Item?.Type == ItemObject.ItemTypeEnum.Shield && equipmentIndex != EquipmentIndex.None)
			{
				TargetEquipmentType = equipmentIndex;
			}
		}
		List<TransferCommand> list = new List<TransferCommand>();
		TransferCommand item = TransferCommand.Transfer(1, itemVM.InventorySide, GetEquipmentToInventorySide(_equipmentMode), sPItemVM.ItemRosterElement, sPItemVM.ItemType, TargetEquipmentType, _currentCharacter);
		list.Add(item);
		if (flag)
		{
			TransferCommand item2 = TransferCommand.Transfer(1, InventoryLogic.InventorySide.PlayerInventory, GetEquipmentToInventorySide(_equipmentMode), itemFromIndex.ItemRosterElement, EquipmentIndex.None, sPItemVM.ItemType, _currentCharacter);
			list.Add(item2);
		}
		_inventoryLogic.AddTransferCommands(list);
	}

	private void UnequipEquipment(SPItemVM itemVM)
	{
		if (itemVM != null && !string.IsNullOrEmpty(itemVM.StringId))
		{
			TransferCommand command = TransferCommand.Transfer(1, GetEquipmentToInventorySide(_equipmentMode), InventoryLogic.InventorySide.PlayerInventory, itemVM.ItemRosterElement, itemVM.ItemType, itemVM.ItemType, _currentCharacter);
			_inventoryLogic.AddTransferCommand(command);
			itemVM.IsSelected = false;
		}
	}

	private void UpdateEquipment(Equipment equipment, SPItemVM itemVM, EquipmentIndex itemType)
	{
		if (ActiveEquipment == equipment)
		{
			RefreshEquipment(itemVM, itemType);
		}
		equipment[itemType] = itemVM?.ItemRosterElement.EquipmentElement ?? default(EquipmentElement);
	}

	private void UnequipEquipmentWithEquipmentIndex(EquipmentIndex slotType)
	{
		switch (slotType)
		{
		case EquipmentIndex.WeaponItemBeginSlot:
			UnequipEquipment(CharacterWeapon1Slot);
			break;
		case EquipmentIndex.Weapon1:
			UnequipEquipment(CharacterWeapon2Slot);
			break;
		case EquipmentIndex.Weapon2:
			UnequipEquipment(CharacterWeapon3Slot);
			break;
		case EquipmentIndex.Weapon3:
			UnequipEquipment(CharacterWeapon4Slot);
			break;
		case EquipmentIndex.ExtraWeaponSlot:
			UnequipEquipment(CharacterBannerSlot);
			break;
		case EquipmentIndex.Body:
			UnequipEquipment(CharacterTorsoSlot);
			break;
		case EquipmentIndex.Leg:
			UnequipEquipment(CharacterBootSlot);
			break;
		case EquipmentIndex.Cape:
			UnequipEquipment(CharacterCloakSlot);
			break;
		case EquipmentIndex.Gloves:
			UnequipEquipment(CharacterGloveSlot);
			break;
		case EquipmentIndex.NumAllWeaponSlots:
			UnequipEquipment(CharacterHelmSlot);
			break;
		case EquipmentIndex.HorseHarness:
			UnequipEquipment(CharacterMountArmorSlot);
			break;
		case EquipmentIndex.ArmorItemEndSlot:
			UnequipEquipment(CharacterMountSlot);
			if (!string.IsNullOrEmpty(CharacterMountArmorSlot.StringId))
			{
				UnequipEquipment(CharacterMountArmorSlot);
			}
			break;
		case EquipmentIndex.None:
			break;
		}
	}

	protected void RefreshEquipment(SPItemVM itemVM, EquipmentIndex itemType)
	{
		InventoryLogic.InventorySide equipmentToInventorySide = GetEquipmentToInventorySide(_equipmentMode);
		switch (itemType)
		{
		case EquipmentIndex.WeaponItemBeginSlot:
			CharacterWeapon1Slot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.Weapon1:
			CharacterWeapon2Slot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.Weapon2:
			CharacterWeapon3Slot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.Weapon3:
			CharacterWeapon4Slot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.ExtraWeaponSlot:
			CharacterBannerSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.Body:
			CharacterTorsoSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.Leg:
			CharacterBootSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.Cape:
			CharacterCloakSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.Gloves:
			CharacterGloveSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.NumAllWeaponSlots:
			CharacterHelmSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.HorseHarness:
			CharacterMountArmorSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.ArmorItemEndSlot:
			CharacterMountSlot.RefreshWith(itemVM, equipmentToInventorySide);
			break;
		case EquipmentIndex.None:
			break;
		}
	}

	private bool UpdateCurrentCharacterIfPossible(int characterIndex, bool isFromRightSide)
	{
		CharacterObject character = (isFromRightSide ? _rightTroopRoster : _leftTroopRoster).GetElementCopyAtIndex(characterIndex).Character;
		if (character.IsHero)
		{
			if (!character.HeroObject.CanHeroEquipmentBeChanged())
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero == null || mainHero.Clan?.AliveLords.Contains(character.HeroObject) != true)
				{
					goto IL_0114;
				}
			}
			_currentInventoryCharacterIndex = characterIndex;
			_currentCharacter = character;
			MainCharacter.FillFrom(_currentCharacter.HeroObject);
			if (_currentCharacter.IsHero)
			{
				MainCharacter.ArmorColor1 = _currentCharacter.HeroObject.MapFaction?.Color ?? 0;
				MainCharacter.ArmorColor2 = _currentCharacter.HeroObject.MapFaction?.Color2 ?? 0;
			}
			UpdateRightCharacter();
			RefreshInformationValues();
			return true;
		}
		goto IL_0114;
		IL_0114:
		return false;
	}

	private bool DoesCompanionExist()
	{
		for (int i = 1; i < _rightTroopRoster.Count; i++)
		{
			CharacterObject character = _rightTroopRoster.GetElementCopyAtIndex(i).Character;
			if (character.IsHero && !character.HeroObject.CanHeroEquipmentBeChanged() && character.HeroObject != Hero.MainHero)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateLeftCharacter()
	{
		IsTradingWithSettlement = false;
		if (_inventoryLogic.LeftRosterName != null)
		{
			LeftInventoryOwnerName = _inventoryLogic.LeftRosterName.ToString();
			Settlement settlement = _currentCharacter.HeroObject.CurrentSettlement;
			InventoryScreenHelper.InventoryMode inventoryMode = InventoryScreenHelper.GetActiveInventoryState()?.InventoryMode ?? InventoryScreenHelper.InventoryMode.Default;
			if (settlement != null && inventoryMode == InventoryScreenHelper.InventoryMode.Warehouse)
			{
				IsTradingWithSettlement = true;
				ProductionTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetSettlementProductionTooltip(settlement));
			}
			return;
		}
		Settlement settlement2 = _currentCharacter.HeroObject.CurrentSettlement;
		if (settlement2 != null)
		{
			LeftInventoryOwnerName = settlement2.Name.ToString();
			ProductionTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetSettlementProductionTooltip(settlement2));
			IsTradingWithSettlement = !settlement2.IsHideout;
			if (_inventoryLogic.InventoryListener != null)
			{
				LeftInventoryOwnerGold = _inventoryLogic.InventoryListener.GetGold();
			}
		}
		else
		{
			MobileParty mobileParty = _inventoryLogic.OppositePartyFromListener?.MobileParty;
			if (mobileParty != null && (mobileParty.IsCaravan || mobileParty.IsVillager))
			{
				LeftInventoryOwnerName = mobileParty.Name.ToString();
				LeftInventoryOwnerGold = _inventoryLogic.InventoryListener?.GetGold() ?? 0;
			}
			else
			{
				LeftInventoryOwnerName = GameTexts.FindText("str_loot").ToString();
			}
		}
	}

	private void UpdateRightCharacter()
	{
		UpdateCharacterEquipment();
		UpdateCharacterArmorValues();
		UpdateCharacterArmorColor();
		RefreshCharacterTotalWeight();
		RefreshCharacterCanUseItem();
		CurrentCharacterName = _currentCharacter.Name.ToString();
		RightInventoryOwnerGold = Hero.MainHero.Gold - _inventoryLogic.TotalAmount;
	}

	private SPItemVM InitializeCharacterEquipmentSlot(ItemRosterElement itemRosterElement, EquipmentIndex equipmentIndex)
	{
		InventoryLogic.InventorySide equipmentToInventorySide = GetEquipmentToInventorySide(_equipmentMode);
		SPItemVM sPItemVM = null;
		if (!itemRosterElement.IsEmpty)
		{
			sPItemVM = new SPItemVM(_inventoryLogic, MainCharacter.IsFemale, CanCharacterUseItem(itemRosterElement), _usageType, itemRosterElement, equipmentToInventorySide, _inventoryLogic.GetCostOfItemRosterElement(itemRosterElement, equipmentToInventorySide), equipmentIndex);
		}
		else
		{
			sPItemVM = new SPItemVM();
			sPItemVM.RefreshWith(null, equipmentToInventorySide);
		}
		return sPItemVM;
	}

	private void UpdateCharacterEquipment()
	{
		CharacterHelmSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.NumAllWeaponSlots), 1), EquipmentIndex.NumAllWeaponSlots);
		CharacterCloakSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Cape), 1), EquipmentIndex.Cape);
		CharacterTorsoSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Body), 1), EquipmentIndex.Body);
		CharacterGloveSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Gloves), 1), EquipmentIndex.Gloves);
		CharacterBootSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Leg), 1), EquipmentIndex.Leg);
		CharacterMountSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.ArmorItemEndSlot), 1), EquipmentIndex.ArmorItemEndSlot);
		CharacterMountArmorSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.HorseHarness), 1), EquipmentIndex.HorseHarness);
		CharacterWeapon1Slot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.WeaponItemBeginSlot), 1), EquipmentIndex.WeaponItemBeginSlot);
		CharacterWeapon2Slot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1), 1), EquipmentIndex.Weapon1);
		CharacterWeapon3Slot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2), 1), EquipmentIndex.Weapon2);
		CharacterWeapon4Slot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3), 1), EquipmentIndex.Weapon3);
		CharacterBannerSlot = InitializeCharacterEquipmentSlot(new ItemRosterElement(ActiveEquipment.GetEquipmentFromSlot(EquipmentIndex.ExtraWeaponSlot), 1), EquipmentIndex.ExtraWeaponSlot);
		MainCharacter.SetEquipment(ActiveEquipment);
	}

	private void UpdateCharacterArmorColor()
	{
		if (ActiveEquipment.IsStealth)
		{
			MainCharacter.ArmorColor1 = 4279111698u;
			MainCharacter.ArmorColor2 = 4279111698u;
		}
		else
		{
			MainCharacter.ArmorColor1 = _currentCharacter.HeroObject.MapFaction?.Color ?? 0;
			MainCharacter.ArmorColor2 = _currentCharacter.HeroObject.MapFaction?.Color2 ?? 0;
		}
	}

	private void UpdateCharacterArmorValues()
	{
		Equipment.EquipmentType equipmentType = ChangeIntoEquipmentType(GetEquipmentToInventorySide(_equipmentMode));
		CurrentCharacterArmArmor = _currentCharacter.GetArmArmorSum(equipmentType);
		CurrentCharacterBodyArmor = _currentCharacter.GetBodyArmorSum(equipmentType);
		CurrentCharacterHeadArmor = _currentCharacter.GetHeadArmorSum(equipmentType);
		CurrentCharacterLegArmor = _currentCharacter.GetLegArmorSum(equipmentType);
		CurrentCharacterHorseArmor = _currentCharacter.GetHorseArmorSum(equipmentType);
	}

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
			Debug.FailedAssert("Cannot change InventoryLogic EquipmentMode to EquiptmentType", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\SPInventoryVM.cs", "ChangeIntoEquipmentType", 1906);
			return Equipment.EquipmentType.Invalid;
		}
	}

	private void RefreshCharacterTotalWeight()
	{
		CharacterObject currentCharacter = _currentCharacter;
		float num = ((currentCharacter != null && currentCharacter.GetPerkValue(DefaultPerks.Athletics.FormFittingArmor)) ? (1f + DefaultPerks.Athletics.FormFittingArmor.PrimaryBonus) : 1f);
		CurrentCharacterTotalEncumbrance = TaleWorlds.Library.MathF.Round(ActiveEquipment.GetTotalWeightOfWeapons() + ActiveEquipment.GetTotalWeightOfArmor(forHuman: true) * num, 1).ToString("0.0");
	}

	private void RefreshCharacterCanUseItem()
	{
		for (int i = 0; i < RightItemListVM.Count; i++)
		{
			RightItemListVM[i].CanCharacterUseItem = CanCharacterUseItem(RightItemListVM[i].ItemRosterElement);
		}
		for (int j = 0; j < LeftItemListVM.Count; j++)
		{
			LeftItemListVM[j].CanCharacterUseItem = CanCharacterUseItem(LeftItemListVM[j].ItemRosterElement);
		}
	}

	private void InitializeInventory()
	{
		IsRefreshed = false;
		switch (_inventoryLogic.MerchantItemType)
		{
		case InventoryScreenHelper.InventoryCategoryType.Armors:
			ActiveFilterIndex = 3;
			break;
		case InventoryScreenHelper.InventoryCategoryType.Weapon:
			ActiveFilterIndex = 1;
			break;
		case InventoryScreenHelper.InventoryCategoryType.Shield:
			ActiveFilterIndex = 2;
			break;
		case InventoryScreenHelper.InventoryCategoryType.HorseCategory:
			ActiveFilterIndex = 4;
			break;
		case InventoryScreenHelper.InventoryCategoryType.Goods:
			ActiveFilterIndex = 5;
			break;
		default:
			ActiveFilterIndex = 0;
			break;
		}
		RightItemListVM.Clear();
		LeftItemListVM.Clear();
		int num = TaleWorlds.Library.MathF.Max(_inventoryLogic.GetElementCountOnSide(InventoryLogic.InventorySide.PlayerInventory), _inventoryLogic.GetElementCountOnSide(InventoryLogic.InventorySide.OtherInventory));
		ItemRosterElement[] array = (from i in _inventoryLogic.GetElementsInRoster(InventoryLogic.InventorySide.PlayerInventory)
			orderby i.EquipmentElement.GetModifiedItemName().ToString()
			select i).ToArray();
		ItemRosterElement[] array2 = (from i in _inventoryLogic.GetElementsInRoster(InventoryLogic.InventorySide.OtherInventory)
			orderby i.EquipmentElement.GetModifiedItemName().ToString()
			select i).ToArray();
		_lockedItemIDs = _viewDataTracker.GetInventoryLocks().ToList();
		for (int num2 = 0; num2 < num; num2++)
		{
			if (num2 < array.Length)
			{
				ItemRosterElement itemRosterElement = array[num2];
				SPItemVM sPItemVM = new SPItemVM(_inventoryLogic, MainCharacter.IsFemale, CanCharacterUseItem(itemRosterElement), _usageType, itemRosterElement, InventoryLogic.InventorySide.PlayerInventory, _inventoryLogic.GetCostOfItemRosterElement(itemRosterElement, InventoryLogic.InventorySide.PlayerInventory), null);
				UpdateFilteredStatusOfItem(sPItemVM);
				sPItemVM.IsLocked = sPItemVM.InventorySide == InventoryLogic.InventorySide.PlayerInventory && IsItemLocked(itemRosterElement);
				RightItemListVM.Add(sPItemVM);
			}
			if (num2 < array2.Length)
			{
				ItemRosterElement itemRosterElement2 = array2[num2];
				SPItemVM sPItemVM2 = new SPItemVM(_inventoryLogic, MainCharacter.IsFemale, CanCharacterUseItem(itemRosterElement2), _usageType, itemRosterElement2, InventoryLogic.InventorySide.OtherInventory, _inventoryLogic.GetCostOfItemRosterElement(itemRosterElement2, InventoryLogic.InventorySide.OtherInventory), null);
				UpdateFilteredStatusOfItem(sPItemVM2);
				sPItemVM2.IsLocked = sPItemVM2.InventorySide == InventoryLogic.InventorySide.PlayerInventory && IsItemLocked(itemRosterElement2);
				LeftItemListVM.Add(sPItemVM2);
			}
		}
		RefreshInformationValues();
		IsRefreshed = true;
	}

	private bool IsItemLocked(ItemRosterElement item)
	{
		string text = item.EquipmentElement.Item.StringId;
		if (item.EquipmentElement.ItemModifier != null)
		{
			text += item.EquipmentElement.ItemModifier.StringId;
		}
		return _lockedItemIDs.Contains(text);
	}

	public void CompareNextItem()
	{
		CycleBetweenWeaponSlots();
		RefreshComparedItem();
	}

	public void ExecuteSelectItem(ItemVM item)
	{
		if (item != null)
		{
			SPItemVM obj = item as SPItemVM;
			if (obj == null || !obj.IsSelected)
			{
				if (GetEquippedItems().Contains(item))
				{
					foreach (SPItemVM allItem in GetAllItems(includeEquipped: false))
					{
						allItem.IsSelected = false;
					}
					{
						foreach (SPItemVM equippedItem in GetEquippedItems())
						{
							equippedItem.IsSelected = equippedItem == item;
						}
						return;
					}
				}
				foreach (SPItemVM equippedItem2 in GetEquippedItems())
				{
					equippedItem2.IsSelected = false;
				}
				{
					foreach (SPItemVM allItem2 in GetAllItems(includeEquipped: false))
					{
						allItem2.IsSelected = allItem2.ItemRosterElement.EquipmentElement.IsEqualTo(item.ItemRosterElement.EquipmentElement);
						if (allItem2.IsSelected)
						{
							ScrollItemId = allItem2.ItemRosterElement.EquipmentElement.Item.StringId;
							ScrollToItem = true;
						}
					}
					return;
				}
			}
		}
		foreach (SPItemVM allItem3 in GetAllItems(includeEquipped: true))
		{
			allItem3.IsSelected = false;
		}
	}

	public void ExecuteClearSelectedItem()
	{
		ExecuteSelectItem(null);
	}

	private IEnumerable<SPItemVM> GetAllItems(bool includeEquipped)
	{
		foreach (SPItemVM item in LeftItemListVM)
		{
			yield return item;
		}
		foreach (SPItemVM item2 in RightItemListVM)
		{
			yield return item2;
		}
		if (!includeEquipped)
		{
			yield break;
		}
		foreach (SPItemVM equippedItem in GetEquippedItems())
		{
			yield return equippedItem;
		}
	}

	private IEnumerable<SPItemVM> GetEquippedItems()
	{
		yield return CharacterHelmSlot;
		yield return CharacterCloakSlot;
		yield return CharacterTorsoSlot;
		yield return CharacterGloveSlot;
		yield return CharacterBootSlot;
		yield return CharacterMountSlot;
		yield return CharacterMountArmorSlot;
		yield return CharacterWeapon1Slot;
		yield return CharacterWeapon2Slot;
		yield return CharacterWeapon3Slot;
		yield return CharacterWeapon4Slot;
		yield return CharacterBannerSlot;
	}

	public bool IsAnyEquippedItemSelected()
	{
		foreach (SPItemVM equippedItem in GetEquippedItems())
		{
			if (equippedItem.IsSelected)
			{
				return true;
			}
		}
		return false;
	}

	private void BuyItem(SPItemVM item)
	{
		if (TargetEquipmentType != EquipmentIndex.None && item.ItemType != TargetEquipmentType && (TargetEquipmentType < EquipmentIndex.WeaponItemBeginSlot || TargetEquipmentType > EquipmentIndex.ExtraWeaponSlot || item.ItemType < EquipmentIndex.WeaponItemBeginSlot || item.ItemType > EquipmentIndex.ExtraWeaponSlot))
		{
			return;
		}
		if (TargetEquipmentType == EquipmentIndex.None)
		{
			TargetEquipmentType = item.ItemType;
			if (item.ItemType >= EquipmentIndex.WeaponItemBeginSlot && item.ItemType <= EquipmentIndex.ExtraWeaponSlot)
			{
				TargetEquipmentType = ActiveEquipment.GetWeaponPickUpSlotIndex(item.ItemRosterElement.EquipmentElement, isStuckMissile: false);
			}
		}
		int b = item.ItemCount;
		if (item.InventorySide == InventoryLogic.InventorySide.PlayerInventory)
		{
			ItemRosterElement? itemRosterElement = _inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.OtherInventory, item.ItemRosterElement.EquipmentElement);
			if (itemRosterElement.HasValue)
			{
				b = itemRosterElement.Value.Amount;
			}
		}
		TransferCommand command = TransferCommand.Transfer(TaleWorlds.Library.MathF.Min(TransactionCount, b), InventoryLogic.InventorySide.OtherInventory, InventoryLogic.InventorySide.PlayerInventory, item.ItemRosterElement, item.ItemType, TargetEquipmentType, _currentCharacter);
		_inventoryLogic.AddTransferCommand(command);
		if (EquipAfterBuy)
		{
			_equipAfterTransferStack.Push(item);
		}
	}

	private void SellItem(SPItemVM item)
	{
		InventoryLogic.InventorySide inventorySide = item.InventorySide;
		int b = item.ItemCount;
		if (inventorySide == InventoryLogic.InventorySide.OtherInventory)
		{
			inventorySide = InventoryLogic.InventorySide.PlayerInventory;
			ItemRosterElement? itemRosterElement = _inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.PlayerInventory, item.ItemRosterElement.EquipmentElement);
			if (itemRosterElement.HasValue)
			{
				b = itemRosterElement.Value.Amount;
			}
		}
		TransferCommand command = TransferCommand.Transfer(TaleWorlds.Library.MathF.Min(TransactionCount, b), inventorySide, InventoryLogic.InventorySide.OtherInventory, item.ItemRosterElement, item.ItemType, TargetEquipmentType, _currentCharacter);
		_inventoryLogic.AddTransferCommand(command);
	}

	private void SlaughterItem(SPItemVM item)
	{
		int num = 1;
		if (IsFiveStackModifierActive)
		{
			num = TaleWorlds.Library.MathF.Min(5, item.ItemCount);
		}
		else if (IsEntireStackModifierActive)
		{
			num = item.ItemCount;
		}
		for (int i = 0; i < num; i++)
		{
			_inventoryLogic.SlaughterItem(item.ItemRosterElement);
		}
	}

	private void DonateItem(SPItemVM item)
	{
		if (IsFiveStackModifierActive)
		{
			int itemCount = item.ItemCount;
			for (int i = 0; i < TaleWorlds.Library.MathF.Min(5, itemCount); i++)
			{
				_inventoryLogic.DonateItem(item.ItemRosterElement);
			}
		}
		else
		{
			_inventoryLogic.DonateItem(item.ItemRosterElement);
		}
	}

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
		if (_inventoryLogic.OtherSideCapacityData != null)
		{
			return _inventoryLogic.OtherSideCapacityData.GetCapacity() - _inventoryLogic.OtherSideCurrentWeight;
		}
		return -1f;
	}

	private void TransferAll(bool isBuy)
	{
		IsRefreshed = false;
		List<TransferCommand> list = new List<TransferCommand>(LeftItemListVM.Count);
		MBBindingList<SPItemVM> mBBindingList = new MBBindingList<SPItemVM>();
		foreach (SPItemVM item3 in isBuy ? LeftItemListVM : RightItemListVM)
		{
			if (item3 != null && !item3.IsFiltered && item3 != null && !item3.IsLocked && item3 != null && item3.IsTransferable)
			{
				mBBindingList.Add(item3);
			}
		}
		MobileParty mobileParty = (isBuy ? MobileParty.MainParty : _inventoryLogic.OtherParty?.MobileParty);
		bool flag = _inventoryLogic.OtherParty?.IsSettlement ?? false;
		InventoryCapacityModel inventoryCapacityModel = Campaign.Current.Models.InventoryCapacityModel;
		mBBindingList.Sort(new RosterElementComparer(inventoryCapacityModel, mobileParty, flag));
		InventoryLogic.InventorySide fromSide = ((!isBuy) ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory);
		InventoryLogic.InventorySide inventorySide = (isBuy ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory);
		if (flag && !isBuy)
		{
			TransferAllForSettlement(mBBindingList, fromSide, inventorySide, list);
		}
		else
		{
			bool flag2 = (InventoryScreenHelper.GetActiveInventoryState()?.InventoryMode ?? InventoryScreenHelper.InventoryMode.Default) == InventoryScreenHelper.InventoryMode.Warehouse;
			float num = 0f;
			float num2 = 0f;
			if (mBBindingList.Count > 0)
			{
				if (mobileParty != null)
				{
					num2 = inventoryCapacityModel.GetItemEffectiveWeight(mBBindingList[0].ItemRosterElement.EquipmentElement, mobileParty, mobileParty.IsCurrentlyAtSea, out var _);
				}
				else if (flag2)
				{
					num2 = mBBindingList[0].ItemRosterElement.EquipmentElement.GetEquipmentElementWeight();
				}
			}
			float capacityBudget = GetCapacityBudget(mobileParty, isBuy);
			bool flag3 = capacityBudget < num2;
			bool flag4 = _inventoryLogic.CanInventoryCapacityIncrease(inventorySide);
			if (!flag3 && flag4)
			{
				List<TransferCommand> list2 = new List<TransferCommand>(0);
				int num3;
				for (num3 = 0; num3 < mBBindingList.Count; num3++)
				{
					SPItemVM sPItemVM = mBBindingList[num3];
					if (!_inventoryLogic.GetCanItemIncreaseInventoryCapacity(sPItemVM.ItemRosterElement.EquipmentElement.Item))
					{
						break;
					}
					TransferCommand item = TransferCommand.Transfer(sPItemVM.ItemRosterElement.Amount, fromSide, inventorySide, sPItemVM.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, _currentCharacter);
					list2.Add(item);
					mBBindingList.Remove(sPItemVM);
					num3--;
				}
				if (list2.Count > 0)
				{
					_inventoryLogic.AddTransferCommands(list2);
					list2.Clear();
					capacityBudget = GetCapacityBudget(mobileParty, isBuy);
				}
			}
			int num4 = mBBindingList.Count - 1;
			while (0 <= num4)
			{
				SPItemVM sPItemVM2 = mBBindingList[num4];
				int num5 = sPItemVM2.ItemRosterElement.Amount;
				if (!flag3)
				{
					TextObject description2;
					float num6 = (flag2 ? sPItemVM2.ItemRosterElement.EquipmentElement.GetEquipmentElementWeight() : inventoryCapacityModel.GetItemEffectiveWeight(sPItemVM2.ItemRosterElement.EquipmentElement, mobileParty, mobileParty.IsCurrentlyAtSea, out description2));
					float num7 = num + num6 * (float)num5;
					if (num5 > 0 && num7 > capacityBudget)
					{
						num5 = MBMath.ClampInt(num5, 0, TaleWorlds.Library.MathF.Floor((capacityBudget - num) / num6));
					}
					num += (float)num5 * num6;
				}
				if (num5 > 0)
				{
					TransferCommand item2 = TransferCommand.Transfer(num5, fromSide, inventorySide, sPItemVM2.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, _currentCharacter);
					list.Add(item2);
				}
				num4--;
			}
		}
		_inventoryLogic.AddTransferCommands(list);
		RefreshInformationValues();
		ExecuteRemoveZeroCounts();
		IsRefreshed = true;
	}

	private void TransferAllForSettlement(MBBindingList<SPItemVM> list, InventoryLogic.InventorySide fromSide, InventoryLogic.InventorySide toSide, List<TransferCommand> commands)
	{
		float num = LeftInventoryOwnerGold;
		float num2 = float.MaxValue;
		float num3 = 0f;
		foreach (SPItemVM item2 in list)
		{
			int itemCost = item2.ItemCost;
			if ((float)itemCost < num2)
			{
				num2 = itemCost;
			}
		}
		bool flag = num < num2;
		int num4 = list.Count - 1;
		while (0 <= num4)
		{
			SPItemVM sPItemVM = list[num4];
			int amount = sPItemVM.ItemRosterElement.Amount;
			if (!flag)
			{
				for (int i = 0; i < amount; i++)
				{
					float num5 = sPItemVM.ItemCost;
					num3 += num5;
					if (num3 < num)
					{
						_inventoryLogic.AddTransferCommands(new List<TransferCommand> { TransferCommand.Transfer(1, fromSide, toSide, sPItemVM.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, _currentCharacter) });
						continue;
					}
					num3 -= num5;
					break;
				}
			}
			else
			{
				TransferCommand item = TransferCommand.Transfer(amount, fromSide, toSide, sPItemVM.ItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, _currentCharacter);
				commands.Add(item);
			}
			num4--;
		}
	}

	public void ExecuteSelectStealthOutfit()
	{
		EquipmentMode = 2;
	}

	public void ExecuteSelectBattleOutfit()
	{
		EquipmentMode = 1;
	}

	public void ExecuteSelectCivilianOutfit()
	{
		EquipmentMode = 0;
	}

	public void ExecuteBuyAllItems()
	{
		TransferAll(isBuy: true);
	}

	public void ExecuteSellAllItems()
	{
		TransferAll(isBuy: false);
	}

	public void ExecuteBuyItemTest()
	{
		TransactionCount = 1;
		EquipAfterBuy = false;
		int totalGold = Hero.MainHero.Gold;
		foreach (SPItemVM item2 in LeftItemListVM.Where(delegate(SPItemVM i)
		{
			ItemObject item = i.ItemRosterElement.EquipmentElement.Item;
			return item != null && item.IsFood && i.ItemCost <= totalGold;
		}))
		{
			if (item2.ItemCost <= totalGold)
			{
				ProcessBuyItem(item2, cameFromTradeData: false);
				totalGold -= item2.ItemCost;
			}
		}
	}

	public void ExecuteResetTranstactions()
	{
		_inventoryLogic.Reset(fromCancel: false);
		InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_inventory_reset_message").ToString()));
		CurrentFocusedItem = null;
	}

	public void ExecuteResetAndCompleteTranstactionsWithoutInquiry()
	{
		ExecuteResetAndCompleteTranstactions();
	}

	public void ExecuteResetAndCompleteTranstactions(bool showCancelInquiry = false)
	{
		ExecuteRemoveZeroCounts();
		if ((InventoryScreenHelper.GetActiveInventoryState()?.InventoryMode ?? InventoryScreenHelper.InventoryMode.Default) == InventoryScreenHelper.InventoryMode.Loot)
		{
			if (_inventoryLogic.IsThereAnyChanges())
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_cancelling_changes").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
				{
					InventoryScreenHelper.CloseScreen(fromCancel: true);
				}, null));
			}
			else if (_inventoryLogic.GetElementsInInitialRoster(InventoryLogic.InventorySide.OtherInventory).Any())
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_loot_behind").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
				{
					InventoryScreenHelper.CloseScreen(fromCancel: true);
				}, null));
			}
			else
			{
				InventoryScreenHelper.CloseScreen(fromCancel: true);
			}
		}
		else if (showCancelInquiry && _inventoryLogic.IsThereAnyChanges())
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_cancelling_changes").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				InventoryScreenHelper.CloseScreen(fromCancel: true);
			}, null));
		}
		else
		{
			InventoryScreenHelper.CloseScreen(fromCancel: true);
		}
	}

	public void ExecuteCompleteTranstactions()
	{
		ExecuteRemoveZeroCounts();
		if ((InventoryScreenHelper.GetActiveInventoryState()?.InventoryMode ?? InventoryScreenHelper.InventoryMode.Default) == InventoryScreenHelper.InventoryMode.Loot && !PlayerEquipmentCountWarned && _inventoryLogic.GetElementCountOnSide(InventoryLogic.InventorySide.OtherInventory) > 0)
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_loot_behind").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), HandleDone, null));
		}
		else if (_inventoryLogic.IsThereAnyChanges() && (PlayerEquipmentCountWarned || OtherEquipmentCountWarned))
		{
			GameTexts.SetVariable("newline", "\n");
			string text = string.Empty;
			if (PlayerEquipmentCountWarned)
			{
				text = GameTexts.FindText("str_inventory_over_limit").ToString();
			}
			else if (OtherEquipmentCountWarned)
			{
				text = GameTexts.FindText("str_inventory_other_party_over_limit").ToString();
			}
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=uJro3Bua}Over Limit").ToString(), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), HandleDone, null));
		}
		else
		{
			HandleDone();
		}
	}

	private void HandleDone()
	{
		MBInformationManager.HideInformations();
		bool num = _inventoryLogic.TotalAmount < 0;
		bool flag = (_inventoryLogic.InventoryListener?.GetGold() ?? 0) >= TaleWorlds.Library.MathF.Abs(_inventoryLogic.TotalAmount);
		int num2 = (int)_inventoryLogic.XpGainFromDonations;
		int num3 = ((_usageType == InventoryScreenHelper.InventoryMode.Default && num2 == 0 && !Game.Current.CheatMode) ? _inventoryLogic.GetElementCountOnSide(InventoryLogic.InventorySide.OtherInventory) : 0);
		if (num && !flag)
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_trader_doesnt_have_enough_money").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				InventoryScreenHelper.CloseScreen(fromCancel: false);
			}, null));
		}
		else if (num3 > 0)
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_discarding_items").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				InventoryScreenHelper.CloseScreen(fromCancel: false);
			}, null));
		}
		else
		{
			InventoryScreenHelper.CloseScreen(fromCancel: false);
		}
		SaveItemLockStates();
		SaveItemSortStates();
	}

	private void SaveItemLockStates()
	{
		_viewDataTracker.SetInventoryLocks(_lockedItemIDs);
	}

	private void SaveItemSortStates()
	{
		_viewDataTracker.InventorySetSortPreference((int)_usageType, (int)PlayerInventorySortController.CurrentSortOption.Value, (int)PlayerInventorySortController.CurrentSortState.Value);
	}

	public void ExecuteTransferWithParameters(SPItemVM item, int index, string targetTag)
	{
		bool isTransferable = item.IsTransferable;
		bool flag = targetTag == "PlayerInventory" || targetTag.StartsWith("Equipment");
		bool isPlayerCharacter = _currentCharacter.IsPlayerCharacter;
		if (!isTransferable && (!flag || !isPlayerCharacter))
		{
			return;
		}
		switch (targetTag)
		{
		case "OverCharacter":
			TargetEquipmentIndex = -1;
			if (item.InventorySide == InventoryLogic.InventorySide.OtherInventory)
			{
				item.TransactionCount = 1;
				TransactionCount = 1;
				ProcessEquipItem(item);
			}
			else if (item.InventorySide == InventoryLogic.InventorySide.PlayerInventory)
			{
				ProcessEquipItem(item);
			}
			return;
		case "PlayerInventory":
			TargetEquipmentIndex = -1;
			if (item.InventorySide == GetEquipmentToInventorySide(_equipmentMode))
			{
				ProcessUnequipItem(item);
			}
			else if (item.InventorySide == InventoryLogic.InventorySide.OtherInventory)
			{
				item.TransactionCount = item.ItemCount;
				TransactionCount = item.ItemCount;
				ProcessBuyItem(item, cameFromTradeData: false);
			}
			return;
		case "OtherInventory":
			if (item.InventorySide != InventoryLogic.InventorySide.OtherInventory)
			{
				item.TransactionCount = item.ItemCount;
				TransactionCount = item.ItemCount;
				ProcessSellItem(item, cameFromTradeData: false);
			}
			return;
		}
		if (targetTag.StartsWith("Equipment"))
		{
			TargetEquipmentIndex = int.Parse(targetTag.Substring("Equipment".Length + 1));
			if (item.InventorySide == InventoryLogic.InventorySide.OtherInventory)
			{
				item.TransactionCount = 1;
				TransactionCount = 1;
				ProcessEquipItem(item);
			}
			else if (item.InventorySide == InventoryLogic.InventorySide.PlayerInventory || item.InventorySide == GetEquipmentToInventorySide(_equipmentMode))
			{
				ProcessEquipItem(item);
			}
		}
	}

	private void UpdateIsDoneDisabled()
	{
		IsDoneDisabled = !_inventoryLogic.CanPlayerCompleteTransaction();
	}

	private void ProcessFilter(Filters filterIndex)
	{
		ActiveFilterIndex = (int)filterIndex;
		IsRefreshed = false;
		foreach (SPItemVM item in LeftItemListVM)
		{
			if (item != null)
			{
				UpdateFilteredStatusOfItem(item);
			}
		}
		foreach (SPItemVM item2 in RightItemListVM)
		{
			if (item2 != null)
			{
				UpdateFilteredStatusOfItem(item2);
			}
		}
		IsRefreshed = true;
	}

	private void UpdateFilteredStatusOfItem(SPItemVM item)
	{
		bool flag = !_filters[_activeFilterIndex].Contains(item.TypeId);
		bool flag2 = false;
		if (IsSearchAvailable && (item.InventorySide == InventoryLogic.InventorySide.OtherInventory || item.InventorySide == InventoryLogic.InventorySide.PlayerInventory))
		{
			string text = ((item.InventorySide == InventoryLogic.InventorySide.OtherInventory) ? LeftSearchText : RightSearchText);
			if (text.Length > 1)
			{
				flag2 = !item.ItemDescription.ToLower().Contains(text);
			}
		}
		item.IsFiltered = flag || flag2;
	}

	private void OnSearchTextChanged(bool isLeft)
	{
		if (IsSearchAvailable)
		{
			(isLeft ? LeftItemListVM : RightItemListVM).ApplyActionOnAllItems(delegate(SPItemVM x)
			{
				UpdateFilteredStatusOfItem(x);
			});
		}
	}

	public void ExecuteFilterNone()
	{
		ProcessFilter(Filters.All);
		Game.Current.EventManager.TriggerEvent(new InventoryFilterChangedEvent(Filters.All));
	}

	public void ExecuteFilterWeapons()
	{
		ProcessFilter(Filters.Weapons);
		Game.Current.EventManager.TriggerEvent(new InventoryFilterChangedEvent(Filters.Weapons));
	}

	public void ExecuteFilterArmors()
	{
		ProcessFilter(Filters.Armors);
		Game.Current.EventManager.TriggerEvent(new InventoryFilterChangedEvent(Filters.Armors));
	}

	public void ExecuteFilterShieldsAndRanged()
	{
		ProcessFilter(Filters.ShieldsAndRanged);
		Game.Current.EventManager.TriggerEvent(new InventoryFilterChangedEvent(Filters.ShieldsAndRanged));
	}

	public void ExecuteFilterMounts()
	{
		ProcessFilter(Filters.Mounts);
		Game.Current.EventManager.TriggerEvent(new InventoryFilterChangedEvent(Filters.Mounts));
	}

	public void ExecuteFilterMisc()
	{
		ProcessFilter(Filters.Miscellaneous);
		Game.Current.EventManager.TriggerEvent(new InventoryFilterChangedEvent(Filters.Miscellaneous));
	}

	public void CycleBetweenWeaponSlots()
	{
		EquipmentIndex selectedEquipmentIndex = (EquipmentIndex)_selectedEquipmentIndex;
		if (selectedEquipmentIndex < EquipmentIndex.WeaponItemBeginSlot || selectedEquipmentIndex >= EquipmentIndex.NumAllWeaponSlots)
		{
			return;
		}
		int selectedEquipmentIndex2 = _selectedEquipmentIndex;
		do
		{
			if (_selectedEquipmentIndex < 3)
			{
				_selectedEquipmentIndex++;
			}
			else
			{
				_selectedEquipmentIndex = 0;
			}
		}
		while (_selectedEquipmentIndex != selectedEquipmentIndex2 && GetItemFromIndex((EquipmentIndex)_selectedEquipmentIndex).ItemRosterElement.EquipmentElement.Item == null);
	}

	private SPItemVM GetItemFromIndex(EquipmentIndex itemType)
	{
		return itemType switch
		{
			EquipmentIndex.WeaponItemBeginSlot => CharacterWeapon1Slot, 
			EquipmentIndex.Weapon1 => CharacterWeapon2Slot, 
			EquipmentIndex.Weapon2 => CharacterWeapon3Slot, 
			EquipmentIndex.Weapon3 => CharacterWeapon4Slot, 
			EquipmentIndex.ExtraWeaponSlot => CharacterBannerSlot, 
			EquipmentIndex.Body => CharacterTorsoSlot, 
			EquipmentIndex.Leg => CharacterBootSlot, 
			EquipmentIndex.Cape => CharacterCloakSlot, 
			EquipmentIndex.Gloves => CharacterGloveSlot, 
			EquipmentIndex.NumAllWeaponSlots => CharacterHelmSlot, 
			EquipmentIndex.HorseHarness => CharacterMountArmorSlot, 
			EquipmentIndex.ArmorItemEndSlot => CharacterMountSlot, 
			_ => null, 
		};
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (!(obj.NewNotificationElementID != _latestTutorialElementID))
		{
			return;
		}
		if (_latestTutorialElementID != null)
		{
			if (obj.NewNotificationElementID != "TransferButtonOnlyFood" && _isFoodTransferButtonHighlightApplied)
			{
				SetFoodTransferButtonHighlightState(state: false);
				_isFoodTransferButtonHighlightApplied = false;
			}
			if (obj.NewNotificationElementID != "InventoryMicsFilter" && IsMicsFilterHighlightEnabled)
			{
				IsMicsFilterHighlightEnabled = false;
			}
			if (obj.NewNotificationElementID != "EquipmentSetFilters" && IsEquipmentSetFiltersHighlighted)
			{
				IsEquipmentSetFiltersHighlighted = false;
			}
			if (obj.NewNotificationElementID != "InventoryOtherBannerItems" && IsBannerItemsHighlightApplied)
			{
				SetBannerItemsHighlightState(state: false);
				IsEquipmentSetFiltersHighlighted = false;
			}
		}
		_latestTutorialElementID = obj.NewNotificationElementID;
		if (!string.IsNullOrEmpty(_latestTutorialElementID))
		{
			if (!_isFoodTransferButtonHighlightApplied && _latestTutorialElementID == "TransferButtonOnlyFood")
			{
				SetFoodTransferButtonHighlightState(state: true);
				_isFoodTransferButtonHighlightApplied = true;
			}
			if (!IsMicsFilterHighlightEnabled && _latestTutorialElementID == "InventoryMicsFilter")
			{
				IsMicsFilterHighlightEnabled = true;
			}
			if (!IsEquipmentSetFiltersHighlighted && _latestTutorialElementID == "EquipmentSetFilters")
			{
				IsEquipmentSetFiltersHighlighted = true;
			}
			if (!IsBannerItemsHighlightApplied && _latestTutorialElementID == "InventoryOtherBannerItems")
			{
				IsBannerItemsHighlightApplied = true;
				ExecuteFilterMisc();
				SetBannerItemsHighlightState(state: true);
			}
		}
		else
		{
			if (_isFoodTransferButtonHighlightApplied)
			{
				SetFoodTransferButtonHighlightState(state: false);
				_isFoodTransferButtonHighlightApplied = false;
			}
			if (IsMicsFilterHighlightEnabled)
			{
				IsMicsFilterHighlightEnabled = false;
			}
			if (IsEquipmentSetFiltersHighlighted)
			{
				IsEquipmentSetFiltersHighlighted = false;
			}
			if (IsBannerItemsHighlightApplied)
			{
				SetBannerItemsHighlightState(state: false);
				IsBannerItemsHighlightApplied = false;
			}
		}
	}

	private void SetFoodTransferButtonHighlightState(bool state)
	{
		for (int i = 0; i < LeftItemListVM.Count; i++)
		{
			SPItemVM sPItemVM = LeftItemListVM[i];
			if (sPItemVM.ItemRosterElement.EquipmentElement.Item.IsFood)
			{
				sPItemVM.IsTransferButtonHighlighted = state;
			}
		}
	}

	private void SetBannerItemsHighlightState(bool state)
	{
		for (int i = 0; i < LeftItemListVM.Count; i++)
		{
			SPItemVM sPItemVM = LeftItemListVM[i];
			if (sPItemVM.ItemRosterElement.EquipmentElement.Item.IsBannerItem)
			{
				sPItemVM.IsItemHighlightEnabled = state;
			}
		}
	}

	private TextObject GetPreviousCharacterKeyText()
	{
		if (PreviousCharacterInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(PreviousCharacterInputKey.KeyID);
	}

	private TextObject GetNextCharacterKeyText()
	{
		if (NextCharacterInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(NextCharacterInputKey.KeyID);
	}

	private TextObject GetBuyAllKeyText()
	{
		if (BuyAllInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(BuyAllInputKey.KeyID);
	}

	private TextObject GetSellAllKeyText()
	{
		if (SellAllInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(SellAllInputKey.KeyID);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey gameKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(gameKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetPreviousCharacterInputKey(HotKey hotKey)
	{
		PreviousCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		SetPreviousCharacterHint();
	}

	public void SetNextCharacterInputKey(HotKey hotKey)
	{
		NextCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		SetNextCharacterHint();
	}

	public void SetBuyAllInputKey(HotKey hotKey)
	{
		BuyAllInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		SetBuyAllHint();
	}

	public void SetSellAllInputKey(HotKey hotKey)
	{
		SellAllInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		SetSellAllHint();
	}

	public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
	{
		_getKeyTextFromKeyId = getKeyTextFromKeyId;
	}
}
