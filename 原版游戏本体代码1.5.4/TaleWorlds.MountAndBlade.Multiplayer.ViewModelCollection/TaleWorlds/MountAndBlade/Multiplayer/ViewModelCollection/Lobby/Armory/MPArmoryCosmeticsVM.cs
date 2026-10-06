using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.MountAndBlade.Diamond.Lobby;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticCategory;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;

public class MPArmoryCosmeticsVM : ViewModel
{
	public enum ClothingCategory
	{
		Invalid = -1,
		ClothingCategoriesBegin = 0,
		All = 0,
		HeadArmor = 1,
		Cape = 2,
		BodyArmor = 3,
		HandArmor = 4,
		LegArmor = 5,
		ClothingCategoriesEnd = 6
	}

	[Flags]
	public enum TauntCategoryFlag
	{
		None = 0,
		UsableWithMount = 1,
		UsableWithOneHanded = 2,
		UsableWithTwoHanded = 4,
		UsableWithBow = 8,
		UsableWithCrossbow = 0x10,
		UsableWithShield = 0x20,
		All = 0x3F
	}

	public abstract class CosmeticItemComparer : IComparer<MPArmoryCosmeticItemBaseVM>
	{
		private bool _isAscending;

		protected int _sortMultiplier
		{
			get
			{
				if (!_isAscending)
				{
					return -1;
				}
				return 1;
			}
		}

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(MPArmoryCosmeticItemBaseVM x, MPArmoryCosmeticItemBaseVM y);
	}

	private class CosmeticItemNameComparer : CosmeticItemComparer
	{
		public override int Compare(MPArmoryCosmeticItemBaseVM x, MPArmoryCosmeticItemBaseVM y)
		{
			return x.Name.CompareTo(y.Name) * base._sortMultiplier;
		}
	}

	private class CosmeticItemCostComparer : CosmeticItemComparer
	{
		public override int Compare(MPArmoryCosmeticItemBaseVM x, MPArmoryCosmeticItemBaseVM y)
		{
			int num = x.Cost.CompareTo(y.Cost);
			if (num == 0)
			{
				if (x is MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM && y is MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM2)
				{
					num = mPArmoryCosmeticClothingItemVM.EquipmentElement.Item.ItemType.CompareTo(mPArmoryCosmeticClothingItemVM2.EquipmentElement.Item.ItemType);
				}
				else if (x is MPArmoryCosmeticTauntItemVM mPArmoryCosmeticTauntItemVM && y is MPArmoryCosmeticTauntItemVM mPArmoryCosmeticTauntItemVM2)
				{
					num = mPArmoryCosmeticTauntItemVM.Name.CompareTo(mPArmoryCosmeticTauntItemVM2.Name);
				}
			}
			return num * base._sortMultiplier;
		}
	}

	private class CosmeticItemRarityComparer : CosmeticItemComparer
	{
		public override int Compare(MPArmoryCosmeticItemBaseVM x, MPArmoryCosmeticItemBaseVM y)
		{
			int num = x.Cosmetic.Rarity.CompareTo(y.Cosmetic.Rarity);
			if (num == 0)
			{
				if (x is MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM && y is MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM2)
				{
					num = mPArmoryCosmeticClothingItemVM.EquipmentElement.Item.ItemType.CompareTo(mPArmoryCosmeticClothingItemVM2.EquipmentElement.Item.ItemType);
				}
				else if (x is MPArmoryCosmeticTauntItemVM mPArmoryCosmeticTauntItemVM && y is MPArmoryCosmeticTauntItemVM mPArmoryCosmeticTauntItemVM2)
				{
					num = mPArmoryCosmeticTauntItemVM.Name.CompareTo(mPArmoryCosmeticTauntItemVM2.Name);
				}
			}
			return num * base._sortMultiplier;
		}
	}

	private class CosmeticItemCategoryComparer : CosmeticItemComparer
	{
		public override int Compare(MPArmoryCosmeticItemBaseVM x, MPArmoryCosmeticItemBaseVM y)
		{
			if (x is MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM && y is MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM2)
			{
				return mPArmoryCosmeticClothingItemVM.EquipmentElement.Item.ItemType.CompareTo(mPArmoryCosmeticClothingItemVM2.EquipmentElement.Item.ItemType) * base._sortMultiplier;
			}
			return 0;
		}
	}

	private readonly Func<List<IReadOnlyPerkObject>> _getSelectedPerks;

	private List<MPArmoryCosmeticItemBaseVM> _allCosmetics;

	private List<string> _ownedCosmetics;

	private Dictionary<string, List<string>> _usedCosmetics;

	private Equipment _selectedClassDefaultEquipment;

	private CosmeticItemComparer _currentItemComparer;

	private List<CosmeticItemComparer> _itemComparers;

	private Dictionary<ClothingCategory, MPArmoryClothingCosmeticCategoryVM> _clothingCategoriesLookup;

	private Dictionary<TauntCategoryFlag, MPArmoryTauntCosmeticCategoryVM> _tauntCategoriesLookup;

	private Dictionary<string, MPArmoryCosmeticItemBaseVM> _cosmeticItemsLookup;

	private MultiplayerClassDivisions.MPHeroClass _selectedClass;

	private string _selectedTroopID;

	private bool _isLocalCosmeticsDirty;

	private bool _isNetworkCosmeticsDirty;

	private bool _isSendingCosmeticData;

	private bool _isRetrievingCosmeticData;

	private CosmeticsManager.CosmeticType _currentCosmeticType;

	private ClothingCategory _currentClothingCategory;

	private TauntCategoryFlag _currentTauntCategory;

	private InputKeyItemVM _actionInputKey;

	private InputKeyItemVM _previewInputKey;

	private int _loot;

	private bool _isLoading;

	private bool _hasCosmeticInfoReceived;

	private bool _isManagingTaunts;

	private bool _isTauntAssignmentActive;

	private string _cosmeticInfoErrorText;

	private HintViewModel _allCategoriesHint;

	private HintViewModel _bodyCategoryHint;

	private HintViewModel _headCategoryHint;

	private HintViewModel _shoulderCategoryHint;

	private HintViewModel _handCategoryHint;

	private HintViewModel _legCategoryHint;

	private HintViewModel _resetPreviewHint;

	private MPArmoryCosmeticCategoryBaseVM _activeCategory;

	private MPArmoryCosmeticTauntSlotVM _selectedTauntSlot;

	private MPArmoryCosmeticTauntItemVM _selectedTauntItem;

	private SelectorVM<SelectorItemVM> _sortCategories;

	private SelectorVM<SelectorItemVM> _sortOrders;

	private MBBindingList<MPArmoryCosmeticTauntSlotVM> _tauntSlots;

	private MBBindingList<MPArmoryCosmeticCategoryBaseVM> _availableCategories;

	[DataSourceProperty]
	public InputKeyItemVM ActionInputKey
	{
		get
		{
			return _actionInputKey;
		}
		set
		{
			if (value != _actionInputKey)
			{
				_actionInputKey = value;
				OnPropertyChangedWithValue(value, "ActionInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PreviewInputKey
	{
		get
		{
			return _previewInputKey;
		}
		set
		{
			if (value != _previewInputKey)
			{
				_previewInputKey = value;
				OnPropertyChangedWithValue(value, "PreviewInputKey");
			}
		}
	}

	[DataSourceProperty]
	public int Loot
	{
		get
		{
			return _loot;
		}
		set
		{
			if (value != _loot)
			{
				_loot = value;
				OnPropertyChangedWithValue(value, "Loot");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			if (value != _isLoading)
			{
				_isLoading = value;
				OnPropertyChangedWithValue(value, "IsLoading");
			}
		}
	}

	[DataSourceProperty]
	public bool HasCosmeticInfoReceived
	{
		get
		{
			return _hasCosmeticInfoReceived;
		}
		set
		{
			if (value != _hasCosmeticInfoReceived)
			{
				_hasCosmeticInfoReceived = value;
				OnPropertyChangedWithValue(value, "HasCosmeticInfoReceived");
			}
		}
	}

	[DataSourceProperty]
	public bool IsManagingTaunts
	{
		get
		{
			return _isManagingTaunts;
		}
		set
		{
			if (value != _isManagingTaunts)
			{
				_isManagingTaunts = value;
				OnPropertyChangedWithValue(value, "IsManagingTaunts");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTauntAssignmentActive
	{
		get
		{
			return _isTauntAssignmentActive;
		}
		set
		{
			if (value != _isTauntAssignmentActive)
			{
				_isTauntAssignmentActive = value;
				OnPropertyChangedWithValue(value, "IsTauntAssignmentActive");
			}
		}
	}

	[DataSourceProperty]
	public string CosmeticInfoErrorText
	{
		get
		{
			return _cosmeticInfoErrorText;
		}
		set
		{
			if (value != _cosmeticInfoErrorText)
			{
				_cosmeticInfoErrorText = value;
				OnPropertyChangedWithValue(value, "CosmeticInfoErrorText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AllCategoriesHint
	{
		get
		{
			return _allCategoriesHint;
		}
		set
		{
			if (value != _allCategoriesHint)
			{
				_allCategoriesHint = value;
				OnPropertyChangedWithValue(value, "AllCategoriesHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BodyCategoryHint
	{
		get
		{
			return _bodyCategoryHint;
		}
		set
		{
			if (value != _bodyCategoryHint)
			{
				_bodyCategoryHint = value;
				OnPropertyChangedWithValue(value, "BodyCategoryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HeadCategoryHint
	{
		get
		{
			return _headCategoryHint;
		}
		set
		{
			if (value != _headCategoryHint)
			{
				_headCategoryHint = value;
				OnPropertyChangedWithValue(value, "HeadCategoryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ShoulderCategoryHint
	{
		get
		{
			return _shoulderCategoryHint;
		}
		set
		{
			if (value != _shoulderCategoryHint)
			{
				_shoulderCategoryHint = value;
				OnPropertyChangedWithValue(value, "ShoulderCategoryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HandCategoryHint
	{
		get
		{
			return _handCategoryHint;
		}
		set
		{
			if (value != _handCategoryHint)
			{
				_handCategoryHint = value;
				OnPropertyChangedWithValue(value, "HandCategoryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LegCategoryHint
	{
		get
		{
			return _legCategoryHint;
		}
		set
		{
			if (value != _legCategoryHint)
			{
				_legCategoryHint = value;
				OnPropertyChangedWithValue(value, "LegCategoryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetPreviewHint
	{
		get
		{
			return _resetPreviewHint;
		}
		set
		{
			if (value != _resetPreviewHint)
			{
				_resetPreviewHint = value;
				OnPropertyChangedWithValue(value, "ResetPreviewHint");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryCosmeticCategoryBaseVM ActiveCategory
	{
		get
		{
			return _activeCategory;
		}
		set
		{
			if (value != _activeCategory)
			{
				_activeCategory = value;
				OnPropertyChangedWithValue(value, "ActiveCategory");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryCosmeticTauntSlotVM SelectedTauntSlot
	{
		get
		{
			return _selectedTauntSlot;
		}
		set
		{
			if (value != _selectedTauntSlot)
			{
				_selectedTauntSlot = value;
				OnPropertyChangedWithValue(value, "SelectedTauntSlot");
				UpdateTauntAssignmentState();
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryCosmeticTauntItemVM SelectedTauntItem
	{
		get
		{
			return _selectedTauntItem;
		}
		set
		{
			if (value != _selectedTauntItem)
			{
				_selectedTauntItem = value;
				OnPropertyChangedWithValue(value, "SelectedTauntItem");
				UpdateTauntAssignmentState();
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> SortCategories
	{
		get
		{
			return _sortCategories;
		}
		set
		{
			if (value != _sortCategories)
			{
				_sortCategories = value;
				OnPropertyChangedWithValue(value, "SortCategories");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> SortOrders
	{
		get
		{
			return _sortOrders;
		}
		set
		{
			if (value != _sortOrders)
			{
				_sortOrders = value;
				OnPropertyChangedWithValue(value, "SortOrders");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPArmoryCosmeticTauntSlotVM> TauntSlots
	{
		get
		{
			return _tauntSlots;
		}
		set
		{
			if (value != _tauntSlots)
			{
				_tauntSlots = value;
				OnPropertyChangedWithValue(value, "TauntSlots");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPArmoryCosmeticCategoryBaseVM> AvailableCategories
	{
		get
		{
			return _availableCategories;
		}
		set
		{
			if (value != _availableCategories)
			{
				_availableCategories = value;
				OnPropertyChangedWithValue(value, "AvailableCategories");
			}
		}
	}

	public static event Action<MPArmoryCosmeticItemBaseVM> OnCosmeticPreview;

	public static event Action<MPArmoryCosmeticItemBaseVM> OnRemoveCosmeticFromPreview;

	public static event Action<List<EquipmentElement>> OnEquipmentRefreshed;

	public static event Action OnTauntAssignmentRefresh;

	public MPArmoryCosmeticsVM(Func<List<IReadOnlyPerkObject>> getSelectedPerks)
	{
		_getSelectedPerks = getSelectedPerks;
		_usedCosmetics = new Dictionary<string, List<string>>();
		_ownedCosmetics = new List<string>();
		_clothingCategoriesLookup = new Dictionary<ClothingCategory, MPArmoryClothingCosmeticCategoryVM>();
		_tauntCategoriesLookup = new Dictionary<TauntCategoryFlag, MPArmoryTauntCosmeticCategoryVM>();
		_cosmeticItemsLookup = new Dictionary<string, MPArmoryCosmeticItemBaseVM>();
		AvailableCategories = new MBBindingList<MPArmoryCosmeticCategoryBaseVM>();
		SortCategories = new SelectorVM<SelectorItemVM>(0, OnSortCategoryUpdated);
		SortOrders = new SelectorVM<SelectorItemVM>(0, OnSortOrderUpdated);
		TauntSlots = new MBBindingList<MPArmoryCosmeticTauntSlotVM>();
		InitializeCosmeticItemComparers();
		InitializeAllCosmetics();
		InitializeCallbacks();
		IsLoading = true;
		SortCategories.AddItem(new SelectorItemVM(new TextObject("{=J2wEawTl}Category")));
		SortCategories.AddItem(new SelectorItemVM(new TextObject("{=ebUrBmHK}Price")));
		SortCategories.AddItem(new SelectorItemVM(new TextObject("{=bD8nTS86}Rarity")));
		SortCategories.AddItem(new SelectorItemVM(new TextObject("{=PDdh1sBj}Name")));
		SortCategories.SelectedIndex = 0;
		SortOrders.AddItem(new SelectorItemVM(new TextObject("{=mOmFzU78}Ascending")));
		SortOrders.AddItem(new SelectorItemVM(new TextObject("{=FgFUsncP}Descending")));
		SortOrders.SelectedIndex = 0;
		RefreshAvailableCategoriesBy(CosmeticsManager.CosmeticType.Clothing);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SortCategories.RefreshValues();
		SortOrders.RefreshValues();
		CosmeticInfoErrorText = new TextObject("{=ehkVpzpa}Unable to get cosmetic information").ToString();
		AllCategoriesHint = new HintViewModel(new TextObject("{=yfa7tpbK}All"));
		BodyCategoryHint = new HintViewModel(GameTexts.FindText("str_inventory_type_13"));
		HeadCategoryHint = new HintViewModel(GameTexts.FindText("str_inventory_type_12"));
		ShoulderCategoryHint = new HintViewModel(GameTexts.FindText("str_inventory_type_22"));
		HandCategoryHint = new HintViewModel(GameTexts.FindText("str_inventory_type_15"));
		LegCategoryHint = new HintViewModel(GameTexts.FindText("str_inventory_type_14"));
		ResetPreviewHint = new HintViewModel(new TextObject("{=imUnCFgZ}Reset preview"));
		_allCosmetics.ForEach(delegate(MPArmoryCosmeticItemBaseVM c)
		{
			c.RefreshValues();
		});
		AvailableCategories.ApplyActionOnAllItems(delegate(MPArmoryCosmeticCategoryBaseVM c)
		{
			c.RefreshValues();
		});
	}

	private void InitializeCallbacks()
	{
		MPArmoryClothingCosmeticCategoryVM.OnSelected += OnClothingCosmeticCategorySelected;
		MPArmoryTauntCosmeticCategoryVM.OnSelected += OnTauntCosmeticCategorySelected;
		MPArmoryCosmeticItemBaseVM.OnPreviewed += EquipItemOnHeroPreview;
		MPArmoryCosmeticItemBaseVM.OnEquipped += OnCosmeticEquipRequested;
		MPArmoryCosmeticTauntSlotVM.OnFocusChanged += OnTauntSlotFocusChanged;
		MPArmoryCosmeticTauntSlotVM.OnSelected += OnTauntSlotSelected;
		MPArmoryCosmeticTauntSlotVM.OnPreview += OnTauntSlotPreview;
		MPArmoryCosmeticTauntSlotVM.OnTauntEquipped += OnTauntItemEquipped;
	}

	private void FinalizeCallbacks()
	{
		MPArmoryClothingCosmeticCategoryVM.OnSelected -= OnClothingCosmeticCategorySelected;
		MPArmoryTauntCosmeticCategoryVM.OnSelected -= OnTauntCosmeticCategorySelected;
		MPArmoryCosmeticItemBaseVM.OnPreviewed -= EquipItemOnHeroPreview;
		MPArmoryCosmeticItemBaseVM.OnEquipped -= OnCosmeticEquipRequested;
		MPArmoryCosmeticTauntSlotVM.OnFocusChanged -= OnTauntSlotFocusChanged;
		MPArmoryCosmeticTauntSlotVM.OnSelected -= OnTauntSlotSelected;
		MPArmoryCosmeticTauntSlotVM.OnPreview -= OnTauntSlotPreview;
		MPArmoryCosmeticTauntSlotVM.OnTauntEquipped -= OnTauntItemEquipped;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		FinalizeCallbacks();
		AvailableCategories.ApplyActionOnAllItems(delegate(MPArmoryCosmeticCategoryBaseVM c)
		{
			c.OnFinalize();
		});
	}

	public async void OnTick(float dt)
	{
		if (NetworkMain.GameClient == null)
		{
			_isNetworkCosmeticsDirty = false;
			_isLocalCosmeticsDirty = false;
		}
		if (!_isSendingCosmeticData && !_isRetrievingCosmeticData)
		{
			if (_isNetworkCosmeticsDirty)
			{
				RefreshCosmeticInfoFromNetworkAux();
				_isNetworkCosmeticsDirty = false;
			}
			if (_isLocalCosmeticsDirty)
			{
				await UpdateUsedCosmeticsAux();
				_isLocalCosmeticsDirty = false;
			}
		}
	}

	private void InitializeCosmeticItemComparers()
	{
		_itemComparers = new List<CosmeticItemComparer>
		{
			new CosmeticItemCategoryComparer(),
			new CosmeticItemCostComparer(),
			new CosmeticItemRarityComparer(),
			new CosmeticItemNameComparer()
		};
		_currentItemComparer = _itemComparers[0];
	}

	private void InitializeAllCosmetics()
	{
		_tauntCategoriesLookup.Clear();
		_tauntCategoriesLookup.Add(TauntCategoryFlag.All, new MPArmoryTauntCosmeticCategoryVM(TauntCategoryFlag.All));
		foreach (TauntCategoryFlag value in Enum.GetValues(typeof(TauntCategoryFlag)))
		{
			if (value > TauntCategoryFlag.None && value < TauntCategoryFlag.All)
			{
				_tauntCategoriesLookup.Add(value, new MPArmoryTauntCosmeticCategoryVM(value));
			}
		}
		_clothingCategoriesLookup.Clear();
		for (ClothingCategory clothingCategory = ClothingCategory.ClothingCategoriesBegin; clothingCategory < ClothingCategory.ClothingCategoriesEnd; clothingCategory++)
		{
			_clothingCategoriesLookup.Add(clothingCategory, new MPArmoryClothingCosmeticCategoryVM(clothingCategory));
		}
		_allCosmetics = new List<MPArmoryCosmeticItemBaseVM>();
		CosmeticsManager.Initialize(ModuleHelper.GetModuleFullPath("Native") + "ModuleData");
		List<CosmeticElement> list = CosmeticsManager.CosmeticElementsList.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Type == CosmeticsManager.CosmeticType.Clothing && list[i] is ClothingCosmeticElement clothingCosmeticElement)
			{
				MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM = new MPArmoryCosmeticClothingItemVM(clothingCosmeticElement, clothingCosmeticElement.Id);
				mPArmoryCosmeticClothingItemVM.IsUnlocked = clothingCosmeticElement.IsFree;
				mPArmoryCosmeticClothingItemVM.IsSelectable = true;
				_allCosmetics.Add(mPArmoryCosmeticClothingItemVM);
				_cosmeticItemsLookup.Add(clothingCosmeticElement.Id, mPArmoryCosmeticClothingItemVM);
				_clothingCategoriesLookup[mPArmoryCosmeticClothingItemVM.ClothingCategory].AvailableCosmetics.Add(mPArmoryCosmeticClothingItemVM);
				_clothingCategoriesLookup[ClothingCategory.ClothingCategoriesBegin].AvailableCosmetics.Add(mPArmoryCosmeticClothingItemVM);
			}
			else
			{
				if (list[i].Type != CosmeticsManager.CosmeticType.Taunt || !(list[i] is TauntCosmeticElement tauntCosmeticElement))
				{
					continue;
				}
				MPArmoryCosmeticTauntItemVM mPArmoryCosmeticTauntItemVM = new MPArmoryCosmeticTauntItemVM(tauntCosmeticElement.Id, tauntCosmeticElement, tauntCosmeticElement.Id);
				mPArmoryCosmeticTauntItemVM.IsUnlocked = tauntCosmeticElement.IsFree;
				mPArmoryCosmeticTauntItemVM.IsSelectable = true;
				_allCosmetics.Add(mPArmoryCosmeticTauntItemVM);
				_cosmeticItemsLookup.Add(tauntCosmeticElement.Id, mPArmoryCosmeticTauntItemVM);
				foreach (TauntCategoryFlag value2 in Enum.GetValues(typeof(TauntCategoryFlag)))
				{
					if (value2 > TauntCategoryFlag.None && value2 <= TauntCategoryFlag.All && (mPArmoryCosmeticTauntItemVM.TauntCategory & value2) != TauntCategoryFlag.None)
					{
						_tauntCategoriesLookup[value2].AvailableCosmetics.Add(mPArmoryCosmeticTauntItemVM);
					}
				}
			}
		}
		for (int j = 0; j < TauntCosmeticElement.MaxNumberOfTaunts; j++)
		{
			TauntSlots.Add(new MPArmoryCosmeticTauntSlotVM(j));
		}
	}

	private void OnClothingCosmeticCategorySelected(MPArmoryClothingCosmeticCategoryVM selectedCosmetic)
	{
		FilterClothingsByCategory(selectedCosmetic);
	}

	private void OnTauntCosmeticCategorySelected(MPArmoryTauntCosmeticCategoryVM selectedCosmetic)
	{
		FilterTauntsByCategory(selectedCosmetic);
	}

	public void RefreshAvailableCategoriesBy(CosmeticsManager.CosmeticType type)
	{
		_currentCosmeticType = type;
		AvailableCategories.Clear();
		switch (type)
		{
		case CosmeticsManager.CosmeticType.Clothing:
			foreach (KeyValuePair<ClothingCategory, MPArmoryClothingCosmeticCategoryVM> item in _clothingCategoriesLookup)
			{
				AvailableCategories.Add(item.Value);
			}
			break;
		case CosmeticsManager.CosmeticType.Taunt:
			foreach (KeyValuePair<TauntCategoryFlag, MPArmoryTauntCosmeticCategoryVM> item2 in _tauntCategoriesLookup)
			{
				AvailableCategories.Add(item2.Value);
			}
			break;
		}
		if (AvailableCategories.Count > 0)
		{
			if (type == CosmeticsManager.CosmeticType.Clothing && _currentClothingCategory != ClothingCategory.Invalid)
			{
				FilterClothingsByCategory(_clothingCategoriesLookup[_currentClothingCategory]);
			}
			else if (type == CosmeticsManager.CosmeticType.Taunt)
			{
				TauntCategoryFlag key = ((_currentTauntCategory != TauntCategoryFlag.None) ? _currentTauntCategory : TauntCategoryFlag.All);
				FilterTauntsByCategory(_tauntCategoriesLookup[key]);
			}
		}
	}

	public void RefreshPlayerData(PlayerData playerData)
	{
		Loot = playerData.Gold;
	}

	public void RefreshCosmeticInfoFromNetwork()
	{
		_isNetworkCosmeticsDirty = true;
	}

	private void RefreshCosmeticInfoFromNetworkAux()
	{
		_isRetrievingCosmeticData = true;
		if (NetworkMain.GameClient.PlayerData == null)
		{
			_isRetrievingCosmeticData = false;
			return;
		}
		IsLoading = true;
		HasCosmeticInfoReceived = true;
		IsLoading = false;
		string text = NetworkMain.GameClient?.PlayerData?.UserId.ToString();
		IReadOnlyDictionary<string, List<string>> readOnlyDictionary = NetworkMain.GameClient?.UsedCosmetics;
		List<string> list = NetworkMain.GameClient?.OwnedCosmetics?.ToList();
		if (text == null || readOnlyDictionary == null || list == null)
		{
			_isRetrievingCosmeticData = false;
			return;
		}
		_ownedCosmetics = list;
		MBReadOnlyList<TauntIndexData> tauntIndicesForPlayer = MultiplayerLocalDataManager.Instance.TauntSlotData.GetTauntIndicesForPlayer(text);
		RefreshTaunts(text, tauntIndicesForPlayer);
		_usedCosmetics = new Dictionary<string, List<string>>();
		foreach (KeyValuePair<string, List<string>> item in readOnlyDictionary)
		{
			_usedCosmetics.Add(item.Key, new List<string>());
			foreach (string item2 in readOnlyDictionary[item.Key])
			{
				_usedCosmetics[item.Key].Add(item2);
			}
		}
		RefreshSelectedClass(_selectedClass, _getSelectedPerks());
		_isRetrievingCosmeticData = false;
	}

	private async Task<bool> UpdateUsedCosmeticsAux()
	{
		_isSendingCosmeticData = true;
		IReadOnlyDictionary<string, List<string>> usedCosmetics = NetworkMain.GameClient.UsedCosmetics;
		Dictionary<string, List<(string, bool)>> dictionary = new Dictionary<string, List<(string, bool)>>();
		foreach (string key2 in _usedCosmetics.Keys)
		{
			dictionary.Add(key2, new List<(string, bool)>());
		}
		foreach (KeyValuePair<string, List<string>> item2 in usedCosmetics)
		{
			foreach (string item3 in item2.Value)
			{
				if (!_usedCosmetics[item2.Key].Contains(item3))
				{
					dictionary[item2.Key].Add((item3, false));
				}
			}
		}
		foreach (KeyValuePair<string, List<string>> usedCosmetic in _usedCosmetics)
		{
			if (!usedCosmetics.ContainsKey(usedCosmetic.Key))
			{
				foreach (string item4 in usedCosmetic.Value)
				{
					dictionary[usedCosmetic.Key].Add((item4, true));
				}
				continue;
			}
			foreach (string item5 in usedCosmetic.Value)
			{
				if (!usedCosmetics[usedCosmetic.Key].Contains(item5))
				{
					dictionary[usedCosmetic.Key].Add((item5, true));
				}
			}
		}
		foreach (KeyValuePair<string, List<(string, bool)>> item6 in dictionary)
		{
			List<ItemObject.ItemTypeEnum> list = new List<ItemObject.ItemTypeEnum>();
			foreach (var item7 in item6.Value)
			{
				var (key, _) = item7;
				if (item7.Item2 && _cosmeticItemsLookup.TryGetValue(key, out var value) && value is MPArmoryCosmeticClothingItemVM { EquipmentElement: var equipmentElement })
				{
					ItemObject.ItemTypeEnum itemType = equipmentElement.Item.ItemType;
					list.Add(itemType);
				}
			}
		}
		List<TauntIndexData> list2 = new List<TauntIndexData>();
		for (int i = 0; i < TauntSlots.Count; i++)
		{
			MPArmoryCosmeticTauntItemVM assignedTauntItem = TauntSlots[i].AssignedTauntItem;
			if (assignedTauntItem != null)
			{
				TauntIndexData item = new TauntIndexData(assignedTauntItem.TauntID, i);
				list2.Add(item);
			}
		}
		bool result = false;
		string text = NetworkMain.GameClient?.PlayerData?.UserId.ToString();
		if (text != null)
		{
			MultiplayerLocalDataManager.Instance.TauntSlotData.SetTauntIndicesForPlayer(text, list2);
			result = await NetworkMain.GameClient.UpdateUsedCosmeticItems(dictionary);
		}
		_isSendingCosmeticData = false;
		return result;
	}

	public void RefreshSelectedClass(MultiplayerClassDivisions.MPHeroClass selectedClass, List<IReadOnlyPerkObject> selectedPerks)
	{
		_selectedClass = selectedClass;
		if (_selectedClass == null)
		{
			return;
		}
		_selectedClassDefaultEquipment = _selectedClass.HeroCharacter.Equipment.Clone();
		if (selectedPerks != null)
		{
			MPArmoryVM.ApplyPerkEffectsToEquipment(ref _selectedClassDefaultEquipment, selectedPerks);
		}
		_selectedTroopID = _selectedClass.StringId;
		ActiveCategory?.Sort(_currentItemComparer);
		if (_ownedCosmetics != null)
		{
			foreach (string ownedCosmeticID in _ownedCosmetics)
			{
				MPArmoryCosmeticItemBaseVM mPArmoryCosmeticItemBaseVM = ActiveCategory?.AvailableCosmetics.FirstOrDefault((MPArmoryCosmeticItemBaseVM c) => c.CosmeticID == ownedCosmeticID);
				if (mPArmoryCosmeticItemBaseVM != null)
				{
					mPArmoryCosmeticItemBaseVM.IsUnlocked = true;
				}
			}
		}
		RefreshFilters();
	}

	private void EquipItemOnHeroPreview(MPArmoryCosmeticItemBaseVM itemVM)
	{
		if (itemVM != null)
		{
			MPArmoryCosmeticsVM.OnCosmeticPreview?.Invoke(itemVM);
		}
		else
		{
			Debug.FailedAssert("Previewing null item", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Armory\\MPArmoryCosmeticsVM.cs", "EquipItemOnHeroPreview", 531);
		}
	}

	private void OnCosmeticEquipRequested(MPArmoryCosmeticItemBaseVM cosmeticItemVM)
	{
		if (cosmeticItemVM.CosmeticType == CosmeticsManager.CosmeticType.Clothing)
		{
			OnItemEquipRequested((MPArmoryCosmeticClothingItemVM)cosmeticItemVM);
		}
		else if (cosmeticItemVM.CosmeticType == CosmeticsManager.CosmeticType.Taunt)
		{
			OnTauntEquipRequested((MPArmoryCosmeticTauntItemVM)cosmeticItemVM);
		}
	}

	private void OnItemEquipRequested(MPArmoryCosmeticClothingItemVM itemVM)
	{
		if (itemVM.IsUsed && !itemVM.Cosmetic.IsFree && ActiveCategory != null && ActiveCategory.CosmeticType == CosmeticsManager.CosmeticType.Clothing && itemVM.ClothingCosmeticElement.ReplaceItemsId.Count > 0 && _selectedClassDefaultEquipment != null)
		{
			for (int i = 0; i < itemVM.ClothingCosmeticElement.ReplaceItemsId.Count; i++)
			{
				string replacedItemId = itemVM.ClothingCosmeticElement.ReplaceItemsId[i];
				for (EquipmentIndex equipmentIndex = EquipmentIndex.NumAllWeaponSlots; equipmentIndex < EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
				{
					if (_selectedClassDefaultEquipment[equipmentIndex].Item?.StringId == replacedItemId && ActiveCategory.AvailableCosmetics.FirstOrDefault((MPArmoryCosmeticItemBaseVM c) => c.Cosmetic.Id == replacedItemId) is MPArmoryCosmeticClothingItemVM itemVM2)
					{
						OnClothingItemEquipped(itemVM2);
						_isLocalCosmeticsDirty = true;
						return;
					}
				}
			}
		}
		if (itemVM.ClothingCosmeticElement.ReplaceItemless.Any((Tuple<string, string> r) => r.Item1 == _selectedClass.StringId))
		{
			if (itemVM.IsUsed)
			{
				itemVM.IsUsed = false;
				_usedCosmetics?[_selectedTroopID].Remove(itemVM.CosmeticID);
				MPArmoryCosmeticsVM.OnRemoveCosmeticFromPreview?.Invoke(itemVM);
			}
			else
			{
				itemVM.ActionText = itemVM.UnequipText;
				OnClothingItemEquipped(itemVM);
			}
		}
		else
		{
			OnClothingItemEquipped(itemVM);
		}
		_isLocalCosmeticsDirty = true;
	}

	private void OnClothingItemEquipped(MPArmoryCosmeticClothingItemVM itemVM, bool forceRemove = true)
	{
		EquipItemOnHeroPreview(itemVM);
		if (!_usedCosmetics.ContainsKey(_selectedTroopID))
		{
			_usedCosmetics.Add(_selectedTroopID, new List<string>());
		}
		if (itemVM.CosmeticID != string.Empty && !_usedCosmetics[_selectedTroopID].Contains(itemVM.CosmeticID))
		{
			_usedCosmetics[_selectedTroopID].Add(itemVM.CosmeticID);
		}
		foreach (MPArmoryCosmeticItemBaseVM availableCosmetic in ActiveCategory.AvailableCosmetics)
		{
			if (((MPArmoryCosmeticClothingItemVM)availableCosmetic).EquipmentElement.Item.ItemType == itemVM.EquipmentElement.Item.ItemType)
			{
				availableCosmetic.IsUsed = false;
				if (itemVM.Cosmetic.Id != availableCosmetic.Cosmetic.Id && forceRemove)
				{
					_usedCosmetics[_selectedTroopID]?.Remove(availableCosmetic.CosmeticID);
				}
			}
		}
		itemVM.IsUsed = true;
		if (ActiveCategory != null)
		{
			UpdateKeyBindingsForCategory(ActiveCategory);
		}
	}

	public void ClearTauntSelections()
	{
		if (SelectedTauntItem == null && SelectedTauntSlot == null)
		{
			return;
		}
		OnTauntEquipRequested(null);
		OnTauntSlotSelected(null);
		foreach (MPArmoryCosmeticTauntSlotVM tauntSlot in TauntSlots)
		{
			tauntSlot.IsAcceptingTaunts = false;
			tauntSlot.IsFocused = false;
		}
	}

	private void OnTauntEquipRequested(MPArmoryCosmeticTauntItemVM tauntItem)
	{
		if (SelectedTauntItem != null)
		{
			if (SelectedTauntItem == tauntItem)
			{
				ClearTauntSelections();
				return;
			}
			SelectedTauntItem.IsSelected = false;
		}
		SelectedTauntItem = tauntItem;
		if (SelectedTauntItem != null)
		{
			MPArmoryCosmeticTauntSlotVM mPArmoryCosmeticTauntSlotVM = null;
			for (int i = 0; i < TauntSlots.Count; i++)
			{
				if (TauntSlots[i].AssignedTauntItem?.CosmeticID == tauntItem.CosmeticID)
				{
					mPArmoryCosmeticTauntSlotVM = TauntSlots[i];
					break;
				}
			}
			if (mPArmoryCosmeticTauntSlotVM != null)
			{
				SelectedTauntItem = null;
				mPArmoryCosmeticTauntSlotVM.AssignTauntItem(null);
				ClearTauntSelections();
				_isLocalCosmeticsDirty = true;
				return;
			}
			SelectedTauntItem.IsSelected = true;
			SelectedTauntItem.ActionText = SelectedTauntItem.CancelEquipText;
			foreach (MPArmoryCosmeticItemBaseVM availableCosmetic in ActiveCategory.AvailableCosmetics)
			{
				availableCosmetic.IsSelectable = availableCosmetic == SelectedTauntItem;
			}
		}
		else
		{
			foreach (MPArmoryCosmeticItemBaseVM availableCosmetic2 in ActiveCategory.AvailableCosmetics)
			{
				availableCosmetic2.IsSelectable = true;
			}
		}
		foreach (MPArmoryCosmeticTauntSlotVM tauntSlot in TauntSlots)
		{
			tauntSlot.IsAcceptingTaunts = tauntSlot.AssignedTauntItem != tauntItem;
		}
		MPArmoryCosmeticsVM.OnTauntAssignmentRefresh?.Invoke();
	}

	private void OnTauntSlotFocusChanged(MPArmoryCosmeticTauntSlotVM changedSlot, bool isFocused)
	{
		foreach (MPArmoryCosmeticTauntSlotVM tauntSlot in TauntSlots)
		{
			tauntSlot.IsFocused = isFocused && changedSlot == tauntSlot;
			if (tauntSlot.IsAcceptingTaunts)
			{
				tauntSlot.EmptySlotKeyVisual.SetForcedVisibility(false);
				tauntSlot.SelectKeyVisual.SetForcedVisibility(null);
			}
			else if (tauntSlot.AssignedTauntItem != null)
			{
				tauntSlot.EmptySlotKeyVisual.SetForcedVisibility(null);
				tauntSlot.SelectKeyVisual.SetForcedVisibility(false);
			}
			else
			{
				tauntSlot.EmptySlotKeyVisual.SetForcedVisibility(false);
			}
			bool? forcedVisibility = ((!tauntSlot.IsAcceptingTaunts && tauntSlot.AssignedTauntItem != null) ? ((bool?)null) : new bool?(false));
			bool? forcedVisibility2 = ((tauntSlot.AssignedTauntItem != null || tauntSlot.IsAcceptingTaunts) ? ((bool?)null) : new bool?(false));
			tauntSlot.EmptySlotKeyVisual?.SetForcedVisibility(forcedVisibility);
			tauntSlot.SelectKeyVisual?.SetForcedVisibility(forcedVisibility2);
		}
	}

	private void OnTauntSlotPreview(MPArmoryCosmeticTauntSlotVM previewSlot)
	{
		previewSlot?.AssignedTauntItem?.ExecutePreview();
	}

	private void OnTauntSlotSelected(MPArmoryCosmeticTauntSlotVM selectedSlot)
	{
		if (SelectedTauntSlot == null && SelectedTauntItem == null && selectedSlot != null && selectedSlot.IsEmpty)
		{
			return;
		}
		MPArmoryCosmeticTauntSlotVM selectedTauntSlot = SelectedTauntSlot;
		SelectedTauntSlot = selectedSlot;
		if (selectedTauntSlot != null)
		{
			selectedTauntSlot.IsSelected = false;
		}
		if (SelectedTauntSlot != null)
		{
			SelectedTauntSlot.IsSelected = true;
		}
		if (selectedSlot?.AssignedTauntItem != null)
		{
			bool flag = false;
			for (int i = 0; i < ActiveCategory.AvailableCosmetics.Count; i++)
			{
				if (ActiveCategory.AvailableCosmetics[i] == selectedSlot.AssignedTauntItem)
				{
					flag = true;
					break;
				}
			}
			if (!flag && _tauntCategoriesLookup.TryGetValue(TauntCategoryFlag.All, out var value))
			{
				FilterTauntsByCategory(value);
			}
		}
		foreach (MPArmoryCosmeticItemBaseVM availableCosmetic in ActiveCategory.AvailableCosmetics)
		{
			availableCosmetic.IsSelectable = selectedSlot == null || availableCosmetic == selectedSlot?.AssignedTauntItem;
		}
		if (SelectedTauntItem == null)
		{
			MPArmoryCosmeticTauntSlotVM selectedTauntSlot2 = SelectedTauntSlot;
			if (selectedTauntSlot2 != null && !selectedTauntSlot2.IsEmpty)
			{
				foreach (MPArmoryCosmeticTauntSlotVM tauntSlot in TauntSlots)
				{
					tauntSlot.IsAcceptingTaunts = tauntSlot != selectedSlot;
				}
			}
		}
		if (SelectedTauntSlot != null)
		{
			bool flag2 = false;
			if (SelectedTauntItem != null && SelectedTauntSlot.AssignedTauntItem != SelectedTauntItem)
			{
				MPArmoryCosmeticTauntSlotVM mPArmoryCosmeticTauntSlotVM = null;
				for (int j = 0; j < TauntSlots.Count; j++)
				{
					if (TauntSlots[j].AssignedTauntItem == SelectedTauntItem)
					{
						mPArmoryCosmeticTauntSlotVM = TauntSlots[j];
						break;
					}
				}
				if (mPArmoryCosmeticTauntSlotVM != null)
				{
					MPArmoryCosmeticTauntItemVM assignedTauntItem = SelectedTauntSlot.AssignedTauntItem;
					MPArmoryCosmeticTauntItemVM assignedTauntItem2 = mPArmoryCosmeticTauntSlotVM.AssignedTauntItem;
					SelectedTauntSlot.AssignTauntItem(assignedTauntItem2, isSwapping: true);
					mPArmoryCosmeticTauntSlotVM.AssignTauntItem(assignedTauntItem, isSwapping: true);
				}
				else
				{
					SelectedTauntSlot.AssignTauntItem(SelectedTauntItem);
				}
				flag2 = true;
				ClearTauntSelections();
			}
			else if (selectedTauntSlot != null && !selectedTauntSlot.IsEmpty && SelectedTauntSlot != selectedTauntSlot)
			{
				MPArmoryCosmeticTauntItemVM assignedTauntItem3 = selectedTauntSlot.AssignedTauntItem;
				MPArmoryCosmeticTauntItemVM assignedTauntItem4 = SelectedTauntSlot.AssignedTauntItem;
				SelectedTauntSlot.AssignTauntItem(assignedTauntItem3, isSwapping: true);
				selectedTauntSlot.AssignTauntItem(assignedTauntItem4, isSwapping: true);
				flag2 = true;
				ClearTauntSelections();
			}
			if (flag2)
			{
				_isLocalCosmeticsDirty = true;
			}
		}
		MPArmoryCosmeticsVM.OnTauntAssignmentRefresh?.Invoke();
	}

	private void OnTauntItemEquipped(MPArmoryCosmeticTauntSlotVM equippedSlot, MPArmoryCosmeticTauntItemVM previousTauntItem, bool isSwapping)
	{
		for (int i = 0; i < TauntSlots.Count; i++)
		{
			MPArmoryCosmeticTauntItemVM assignedTauntItem = TauntSlots[i].AssignedTauntItem;
			if (assignedTauntItem != null && !assignedTauntItem.IsUnlocked)
			{
				Debug.FailedAssert("Assigned a taunt without ownership: " + assignedTauntItem.TauntID, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Armory\\MPArmoryCosmeticsVM.cs", "OnTauntItemEquipped", 874);
			}
		}
		_isLocalCosmeticsDirty = true;
	}

	public void OnItemObtained(string cosmeticID, int finalLoot)
	{
		_ownedCosmetics.Add(cosmeticID);
		RefreshCosmeticInfoFromNetwork();
		Loot = finalLoot;
	}

	private void OnSortCategoryUpdated(SelectorVM<SelectorItemVM> selector)
	{
		if (SortCategories.SelectedIndex == -1)
		{
			SortCategories.SelectedIndex = 0;
		}
		_currentItemComparer = _itemComparers[selector.SelectedIndex];
		ActiveCategory?.Sort(_currentItemComparer);
	}

	private void OnSortOrderUpdated(SelectorVM<SelectorItemVM> selector)
	{
		if (SortOrders.SelectedIndex == -1)
		{
			SortOrders.SelectedIndex = 0;
		}
		foreach (CosmeticItemComparer itemComparer in _itemComparers)
		{
			itemComparer.SetSortMode(selector.SelectedIndex == 0);
		}
		ActiveCategory?.Sort(_currentItemComparer);
	}

	private void RefreshFilters()
	{
		MPArmoryTauntCosmeticCategoryVM value2;
		if (_currentCosmeticType == CosmeticsManager.CosmeticType.Clothing && _clothingCategoriesLookup.TryGetValue(_currentClothingCategory, out var value))
		{
			FilterClothingsByCategory(value);
		}
		else if (_currentCosmeticType == CosmeticsManager.CosmeticType.Taunt && _tauntCategoriesLookup.TryGetValue(_currentTauntCategory, out value2))
		{
			FilterTauntsByCategory(value2);
		}
	}

	private void FilterClothingsByCategory(MPArmoryClothingCosmeticCategoryVM clothingCategory)
	{
		if (_currentCosmeticType != CosmeticsManager.CosmeticType.Clothing)
		{
			RefreshAvailableCategoriesBy(CosmeticsManager.CosmeticType.Clothing);
			return;
		}
		if (clothingCategory == null)
		{
			Debug.FailedAssert("Trying to filter by null clothing category", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Armory\\MPArmoryCosmeticsVM.cs", "FilterClothingsByCategory", 937);
			return;
		}
		_currentClothingCategory = clothingCategory.ClothingCategory;
		foreach (KeyValuePair<ClothingCategory, MPArmoryClothingCosmeticCategoryVM> item in _clothingCategoriesLookup)
		{
			item.Value.IsSelected = false;
		}
		clothingCategory.SetDefaultEquipments(_selectedClassDefaultEquipment);
		ActiveCategory = clothingCategory;
		if (_selectedClass != null)
		{
			foreach (MPArmoryCosmeticItemBaseVM allCosmetic in _allCosmetics)
			{
				if (allCosmetic.CosmeticType == CosmeticsManager.CosmeticType.Clothing)
				{
					clothingCategory.ReplaceCosmeticWithDefaultItem((MPArmoryCosmeticClothingItemVM)allCosmetic, clothingCategory.ClothingCategory, _selectedClass, _ownedCosmetics);
				}
			}
		}
		ActiveCategory.Sort(_currentItemComparer);
		RefreshEquipment();
		if (ActiveCategory != null)
		{
			ActiveCategory.IsSelected = true;
			UpdateKeyBindingsForCategory(ActiveCategory);
		}
	}

	private void FilterTauntsByCategory(MPArmoryTauntCosmeticCategoryVM tauntCategory)
	{
		if (_currentCosmeticType != CosmeticsManager.CosmeticType.Taunt)
		{
			RefreshAvailableCategoriesBy(CosmeticsManager.CosmeticType.Taunt);
		}
		_currentTauntCategory = tauntCategory.TauntCategory;
		foreach (KeyValuePair<TauntCategoryFlag, MPArmoryTauntCosmeticCategoryVM> item in _tauntCategoriesLookup)
		{
			item.Value.IsSelected = false;
		}
		ActiveCategory = tauntCategory;
		if (ActiveCategory != null)
		{
			ActiveCategory.IsSelected = true;
			UpdateKeyBindingsForCategory(ActiveCategory);
		}
		ActiveCategory.Sort(_currentItemComparer);
	}

	private void RefreshEquipment()
	{
		Dictionary<EquipmentIndex, bool> dictionary = new Dictionary<EquipmentIndex, bool>();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.NumAllWeaponSlots; equipmentIndex < EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
		{
			dictionary.Add(equipmentIndex, value: false);
		}
		List<EquipmentElement> list = new List<EquipmentElement>();
		foreach (MPArmoryCosmeticItemBaseVM item2 in ActiveCategory.AvailableCosmetics.Where((MPArmoryCosmeticItemBaseVM c) => c.Cosmetic.Rarity == CosmeticsManager.CosmeticRarity.Default))
		{
			if (item2 is MPArmoryCosmeticClothingItemVM mPArmoryCosmeticClothingItemVM)
			{
				OnClothingItemEquipped(mPArmoryCosmeticClothingItemVM, forceRemove: false);
				dictionary[mPArmoryCosmeticClothingItemVM.EquipmentElement.Item.GetCosmeticEquipmentIndex()] = true;
				list.Add(mPArmoryCosmeticClothingItemVM.EquipmentElement);
			}
		}
		if (!string.IsNullOrEmpty(_selectedTroopID))
		{
			Dictionary<string, List<string>> usedCosmetics = _usedCosmetics;
			if (usedCosmetics != null && usedCosmetics.ContainsKey(_selectedTroopID))
			{
				Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
				foreach (string key in _usedCosmetics.Keys)
				{
					List<string> list2 = new List<string>();
					foreach (string item3 in _usedCosmetics[key])
					{
						list2.Add(item3);
					}
					dictionary2.Add(key, list2);
				}
				foreach (string cosmeticID in dictionary2[_selectedTroopID])
				{
					MPArmoryCosmeticClothingItemVM cosmeticItem = (MPArmoryCosmeticClothingItemVM)_allCosmetics.First((MPArmoryCosmeticItemBaseVM c) => c.CosmeticID == cosmeticID);
					if (cosmeticItem == null)
					{
						continue;
					}
					EquipmentIndex cosmeticEquipmentIndex = cosmeticItem.EquipmentElement.Item.GetCosmeticEquipmentIndex();
					if (!(cosmeticItem.Cosmetic as ClothingCosmeticElement).ReplaceItemless.IsEmpty() || !_selectedClassDefaultEquipment[cosmeticEquipmentIndex].IsEmpty)
					{
						EquipmentElement item = list.FirstOrDefault((EquipmentElement i) => i.Item.GetCosmeticEquipmentIndex() == cosmeticItem.EquipmentElement.Item.GetCosmeticEquipmentIndex());
						if (!item.IsEmpty)
						{
							list.Remove(item);
							list.Add(cosmeticItem.EquipmentElement);
						}
						OnClothingItemEquipped(cosmeticItem);
						dictionary[cosmeticEquipmentIndex] = true;
					}
				}
			}
		}
		foreach (EquipmentIndex key2 in dictionary.Keys)
		{
			if (!dictionary[key2])
			{
				((MPArmoryClothingCosmeticCategoryVM)ActiveCategory)?.OnEquipmentRefreshed(key2);
			}
		}
		MPArmoryCosmeticsVM.OnEquipmentRefreshed?.Invoke(list);
	}

	private void RefreshTaunts(string playerId, MBReadOnlyList<TauntIndexData> registeredTaunts)
	{
		List<TauntIndexData> list = registeredTaunts?.ToList();
		if (list == null)
		{
			list = new List<TauntIndexData>();
			foreach (MPArmoryCosmeticItemBaseVM item2 in (from c in _tauntCategoriesLookup.SelectMany((KeyValuePair<TauntCategoryFlag, MPArmoryTauntCosmeticCategoryVM> c) => c.Value.AvailableCosmetics)
				where c.Cosmetic.IsFree
				select c).Distinct())
			{
				TauntIndexData item = new TauntIndexData(item2.CosmeticID, list.Count);
				list.Add(item);
			}
		}
		foreach (KeyValuePair<TauntCategoryFlag, MPArmoryTauntCosmeticCategoryVM> item3 in _tauntCategoriesLookup)
		{
			foreach (MPArmoryCosmeticItemBaseVM availableCosmetic in item3.Value.AvailableCosmetics)
			{
				availableCosmetic.IsUnlocked = availableCosmetic.Cosmetic.IsFree || _ownedCosmetics.Contains(availableCosmetic.CosmeticID);
			}
		}
		for (int num = 0; num < TauntSlots.Count; num++)
		{
			TauntSlots[num].AssignTauntItem(null);
		}
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			string tauntId = list[num2].TauntId;
			int tauntIndex = list[num2].TauntIndex;
			if (_cosmeticItemsLookup.TryGetValue(tauntId, out var value) && value is MPArmoryCosmeticTauntItemVM mPArmoryCosmeticTauntItemVM)
			{
				if (!mPArmoryCosmeticTauntItemVM.IsUnlocked)
				{
					Debug.FailedAssert("Trying to add non-owned cosmetic to taunt slot: " + mPArmoryCosmeticTauntItemVM.TauntID, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Armory\\MPArmoryCosmeticsVM.cs", "RefreshTaunts", 1115);
				}
				else if (tauntIndex >= 0 && tauntIndex < TauntSlots.Count)
				{
					TauntSlots[tauntIndex].AssignTauntItem(mPArmoryCosmeticTauntItemVM);
				}
			}
		}
	}

	private void UpdateTauntAssignmentState()
	{
		IsTauntAssignmentActive = SelectedTauntItem != null || SelectedTauntSlot != null;
	}

	private void ExecuteRefreshCosmeticInfo()
	{
		RefreshCosmeticInfoFromNetwork();
	}

	private void ExecuteResetPreview()
	{
		RefreshSelectedClass(_selectedClass, _getSelectedPerks());
	}

	public void RefreshKeyBindings(HotKey actionKey, HotKey previewKey)
	{
		ActionInputKey = InputKeyItemVM.CreateFromHotKey(actionKey, isConsoleOnly: false);
		PreviewInputKey = InputKeyItemVM.CreateFromHotKey(previewKey, isConsoleOnly: false);
		for (int i = 0; i < AvailableCategories.Count; i++)
		{
			UpdateKeyBindingsForCategory(AvailableCategories[i]);
		}
	}

	private void UpdateKeyBindingsForCategory(MPArmoryCosmeticCategoryBaseVM categoryVM)
	{
		if (ActionInputKey != null && PreviewInputKey != null)
		{
			for (int i = 0; i < categoryVM.AvailableCosmetics.Count; i++)
			{
				categoryVM.AvailableCosmetics[i].RefreshKeyBindings(ActionInputKey.HotKey, PreviewInputKey.HotKey);
			}
		}
	}
}
