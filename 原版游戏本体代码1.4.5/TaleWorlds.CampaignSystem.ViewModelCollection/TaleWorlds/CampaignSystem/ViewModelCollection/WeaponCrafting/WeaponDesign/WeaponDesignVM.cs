using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class WeaponDesignVM : ViewModel
{
	[Flags]
	public enum CraftingPieceTierFilter
	{
		None = 0,
		Tier1 = 1,
		Tier2 = 2,
		Tier3 = 4,
		Tier4 = 8,
		Tier5 = 0x10,
		All = 0x1F
	}

	public class PieceTierComparer : IComparer<CraftingPieceVM>
	{
		public int Compare(CraftingPieceVM x, CraftingPieceVM y)
		{
			if (x.Tier != y.Tier)
			{
				return x.Tier.CompareTo(y.Tier);
			}
			return x.CraftingPiece.CraftingPiece.StringId.CompareTo(y.CraftingPiece.CraftingPiece.StringId);
		}
	}

	public class TemplateComparer : IComparer<CraftingTemplate>
	{
		public int Compare(CraftingTemplate x, CraftingTemplate y)
		{
			return string.Compare(x.StringId, y.StringId, StringComparison.OrdinalIgnoreCase);
		}
	}

	public class WeaponPropertyComparer : IComparer<CraftingListPropertyItem>
	{
		public int Compare(CraftingListPropertyItem x, CraftingListPropertyItem y)
		{
			int type = (int)x.Type;
			return type.CompareTo((int)y.Type);
		}
	}

	private CraftingPieceTierFilter _currentTierFilter = CraftingPieceTierFilter.All;

	public const int MAX_SKILL_LEVEL = 300;

	public ItemObject CraftedItemObject;

	private int _selectedWeaponClassIndex;

	private readonly List<CraftingPiece> _newlyUnlockedPieces;

	private readonly List<CraftingTemplate> _primaryUsages;

	private readonly PieceTierComparer _pieceTierComparer;

	private readonly TemplateComparer _templateComparer;

	private readonly ICraftingCampaignBehavior _craftingBehavior;

	private readonly Action _onRefresh;

	private readonly Action _onWeaponCrafted;

	private readonly Func<CraftingAvailableHeroItemVM> _getCurrentCraftingHero;

	private readonly Action<CraftingOrder> _refreshHeroAvailabilities;

	private Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

	private Crafting _crafting;

	private bool _updatePiece = true;

	private Dictionary<CraftingPiece.PieceTypes, CraftingPieceListVM> _pieceListsDictionary;

	private Dictionary<CraftingPiece, CraftingPieceVM> _pieceVMs;

	private TextObject _difficultyTextobj = new TextObject("{=cbbUzYX3}Difficulty: {DIFFICULTY}");

	private TextObject _orderDifficultyTextObj = new TextObject("{=8szijlHj}Order Difficulty: {DIFFICULTY}");

	private bool _isAutoSelectingPieces;

	private bool _shouldRecordHistory;

	private MBBindingList<TierFilterTypeVM> _tierFilters;

	private string _currentCraftedWeaponTemplateId;

	private string _chooseOrderText;

	private string _chooseWeaponTypeText;

	private string _currentCraftedWeaponTypeText;

	private MBBindingList<CraftingPieceListVM> _pieceLists;

	private int _selectedPieceTypeIndex;

	private bool _showOnlyUnlockedPieces;

	private string _missingPropertyWarningText;

	private bool _isInFinalCraftingStage;

	private string _componentSizeLbl;

	private string _itemName;

	private string _difficultyText;

	private int _bladeSize;

	private int _pommelSize;

	private int _handleSize;

	private int _guardSize;

	private CraftingPieceVM _selectedBladePiece;

	private CraftingPieceVM _selectedGuardPiece;

	private CraftingPieceVM _selectedHandlePiece;

	private CraftingPieceVM _selectedPommelPiece;

	private CraftingPieceListVM _activePieceList;

	private CraftingPieceListVM _bladePieceList;

	private CraftingPieceListVM _guardPieceList;

	private CraftingPieceListVM _handlePieceList;

	private CraftingPieceListVM _pommelPieceList;

	private string _alternativeUsageText;

	private string _defaultUsageText;

	private bool _isScabbardVisible;

	private bool _currentWeaponHasScabbard;

	public SelectorVM<CraftingSecondaryUsageItemVM> _secondaryUsageSelector;

	private ItemCollectionElementViewModel _craftedItemVisual;

	private MBBindingList<CraftingListPropertyItem> _primaryPropertyList;

	private MBBindingList<WeaponDesignResultPropertyItemVM> _designResultPropertyList;

	private int _currentDifficulty;

	private int _currentOrderDifficulty;

	private int _maxDifficulty;

	private string _currentDifficultyText;

	private string _currentOrderDifficultyText;

	private string _currentCraftingSkillValueText;

	private bool _isCurrentHeroAtMaxCraftingSkill;

	private int _currentHeroCraftingSkill;

	private bool _isWeaponCivilian;

	private HintViewModel _scabbardHint;

	private HintViewModel _randomizeHint;

	private HintViewModel _undoHint;

	private HintViewModel _redoHint;

	private HintViewModel _showOnlyUnlockedPiecesHint;

	private BasicTooltipViewModel _difficultyExplanationHint;

	private BasicTooltipViewModel _orderDisabledReasonHint;

	private CraftingOrderItemVM _activeCraftingOrder;

	private CraftingOrderPopupVM _craftingOrderPopup;

	private WeaponClassSelectionPopupVM _weaponClassSelectionPopup;

	private string _freeModeButtonText;

	private bool _isOrderButtonActive;

	private bool _isInOrderMode;

	private WeaponDesignResultPopupVM _craftingResultPopup;

	private MBBindingList<ItemFlagVM> _weaponFlagIconsList;

	private CraftingHistoryVM _craftingHistory;

	private TextObject _currentCraftingSkillText;

	[DataSourceProperty]
	public MBBindingList<TierFilterTypeVM> TierFilters
	{
		get
		{
			return _tierFilters;
		}
		set
		{
			if (value != _tierFilters)
			{
				_tierFilters = value;
				OnPropertyChangedWithValue(value, "TierFilters");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCraftedWeaponTemplateId
	{
		get
		{
			return _currentCraftedWeaponTemplateId;
		}
		set
		{
			if (value != _currentCraftedWeaponTemplateId)
			{
				_currentCraftedWeaponTemplateId = value;
				OnPropertyChangedWithValue(value, "CurrentCraftedWeaponTemplateId");
			}
		}
	}

	[DataSourceProperty]
	public string ChooseOrderText
	{
		get
		{
			return _chooseOrderText;
		}
		set
		{
			if (value != _chooseOrderText)
			{
				_chooseOrderText = value;
				OnPropertyChangedWithValue(value, "ChooseOrderText");
			}
		}
	}

	[DataSourceProperty]
	public string ChooseWeaponTypeText
	{
		get
		{
			return _chooseWeaponTypeText;
		}
		set
		{
			if (value != _chooseWeaponTypeText)
			{
				_chooseWeaponTypeText = value;
				OnPropertyChangedWithValue(value, "ChooseWeaponTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCraftedWeaponTypeText
	{
		get
		{
			return _currentCraftedWeaponTypeText;
		}
		set
		{
			if (value != _currentCraftedWeaponTypeText)
			{
				_currentCraftedWeaponTypeText = value;
				OnPropertyChangedWithValue(value, "CurrentCraftedWeaponTypeText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingPieceListVM> PieceLists
	{
		get
		{
			return _pieceLists;
		}
		set
		{
			if (value != _pieceLists)
			{
				_pieceLists = value;
				OnPropertyChangedWithValue(value, "PieceLists");
			}
		}
	}

	[DataSourceProperty]
	public int SelectedPieceTypeIndex
	{
		get
		{
			return _selectedPieceTypeIndex;
		}
		set
		{
			if (value != _selectedPieceTypeIndex)
			{
				_selectedPieceTypeIndex = value;
				OnPropertyChangedWithValue(value, "SelectedPieceTypeIndex");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowOnlyUnlockedPieces
	{
		get
		{
			return _showOnlyUnlockedPieces;
		}
		set
		{
			if (value != _showOnlyUnlockedPieces)
			{
				_showOnlyUnlockedPieces = value;
				OnPropertyChangedWithValue(value, "ShowOnlyUnlockedPieces");
			}
		}
	}

	[DataSourceProperty]
	public string MissingPropertyWarningText
	{
		get
		{
			return _missingPropertyWarningText;
		}
		set
		{
			if (value != _missingPropertyWarningText)
			{
				_missingPropertyWarningText = value;
				OnPropertyChangedWithValue(value, "MissingPropertyWarningText");
			}
		}
	}

	[DataSourceProperty]
	public WeaponDesignResultPopupVM CraftingResultPopup
	{
		get
		{
			return _craftingResultPopup;
		}
		set
		{
			if (value != _craftingResultPopup)
			{
				_craftingResultPopup = value;
				OnPropertyChangedWithValue(value, "CraftingResultPopup");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOrderButtonActive
	{
		get
		{
			return _isOrderButtonActive;
		}
		set
		{
			if (value != _isOrderButtonActive)
			{
				_isOrderButtonActive = value;
				OnPropertyChangedWithValue(value, "IsOrderButtonActive");
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
				OnPropertyChanged("IsInFreeMode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInFreeMode
	{
		get
		{
			return !_isInOrderMode;
		}
		set
		{
			if (value != IsInFreeMode)
			{
				_isInOrderMode = !value;
				OnPropertyChangedWithValue(value, "IsInFreeMode");
				OnPropertyChanged("IsInOrderMode");
			}
		}
	}

	[DataSourceProperty]
	public string FreeModeButtonText
	{
		get
		{
			return _freeModeButtonText;
		}
		set
		{
			if (value != _freeModeButtonText)
			{
				_freeModeButtonText = value;
				OnPropertyChangedWithValue(value, "FreeModeButtonText");
			}
		}
	}

	[DataSourceProperty]
	public CraftingOrderItemVM ActiveCraftingOrder
	{
		get
		{
			return _activeCraftingOrder;
		}
		set
		{
			if (value != _activeCraftingOrder)
			{
				_activeCraftingOrder = value;
				OnPropertyChangedWithValue(value, "ActiveCraftingOrder");
			}
		}
	}

	[DataSourceProperty]
	public CraftingOrderPopupVM CraftingOrderPopup
	{
		get
		{
			return _craftingOrderPopup;
		}
		set
		{
			if (value != _craftingOrderPopup)
			{
				_craftingOrderPopup = value;
				OnPropertyChangedWithValue(value, "CraftingOrderPopup");
			}
		}
	}

	[DataSourceProperty]
	public WeaponClassSelectionPopupVM WeaponClassSelectionPopup
	{
		get
		{
			return _weaponClassSelectionPopup;
		}
		set
		{
			if (value != _weaponClassSelectionPopup)
			{
				_weaponClassSelectionPopup = value;
				OnPropertyChangedWithValue(value, "WeaponClassSelectionPopup");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingListPropertyItem> PrimaryPropertyList
	{
		get
		{
			return _primaryPropertyList;
		}
		set
		{
			if (value != _primaryPropertyList)
			{
				_primaryPropertyList = value;
				OnPropertyChangedWithValue(value, "PrimaryPropertyList");
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
	public ItemCollectionElementViewModel CraftedItemVisual
	{
		get
		{
			return _craftedItemVisual;
		}
		set
		{
			if (value != _craftedItemVisual)
			{
				_craftedItemVisual = value;
				OnPropertyChangedWithValue(value, "CraftedItemVisual");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInFinalCraftingStage
	{
		get
		{
			return _isInFinalCraftingStage;
		}
		set
		{
			if (value != _isInFinalCraftingStage)
			{
				_isInFinalCraftingStage = value;
				OnPropertyChangedWithValue(value, "IsInFinalCraftingStage");
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
				OnPropertyChangedWithValue(value, "ItemName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsScabbardVisible
	{
		get
		{
			return _isScabbardVisible;
		}
		set
		{
			if (value != _isScabbardVisible)
			{
				_isScabbardVisible = value;
				OnPropertyChangedWithValue(value, "IsScabbardVisible");
				_crafting.ReIndex();
				_onRefresh?.Invoke();
			}
		}
	}

	[DataSourceProperty]
	public bool CurrentWeaponHasScabbard
	{
		get
		{
			return _currentWeaponHasScabbard;
		}
		set
		{
			if (value != _currentWeaponHasScabbard)
			{
				_currentWeaponHasScabbard = value;
				OnPropertyChangedWithValue(value, "CurrentWeaponHasScabbard");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentDifficulty
	{
		get
		{
			return _currentDifficulty;
		}
		set
		{
			if (value != _currentDifficulty)
			{
				_currentDifficulty = value;
				OnPropertyChangedWithValue(value, "CurrentDifficulty");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentOrderDifficulty
	{
		get
		{
			return _currentOrderDifficulty;
		}
		set
		{
			if (value != _currentOrderDifficulty)
			{
				_currentOrderDifficulty = value;
				OnPropertyChangedWithValue(value, "CurrentOrderDifficulty");
			}
		}
	}

	[DataSourceProperty]
	public int MaxDifficulty
	{
		get
		{
			return _maxDifficulty;
		}
		set
		{
			if (value != _maxDifficulty)
			{
				_maxDifficulty = value;
				OnPropertyChangedWithValue(value, "MaxDifficulty");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentHeroAtMaxCraftingSkill
	{
		get
		{
			return _isCurrentHeroAtMaxCraftingSkill;
		}
		set
		{
			if (value != _isCurrentHeroAtMaxCraftingSkill)
			{
				_isCurrentHeroAtMaxCraftingSkill = value;
				OnPropertyChangedWithValue(value, "IsCurrentHeroAtMaxCraftingSkill");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentHeroCraftingSkill
	{
		get
		{
			return _currentHeroCraftingSkill;
		}
		set
		{
			if (value != _currentHeroCraftingSkill)
			{
				_currentHeroCraftingSkill = value;
				OnPropertyChangedWithValue(value, "CurrentHeroCraftingSkill");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentDifficultyText
	{
		get
		{
			return _currentDifficultyText;
		}
		set
		{
			if (value != _currentDifficultyText)
			{
				_currentDifficultyText = value;
				OnPropertyChangedWithValue(value, "CurrentDifficultyText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentOrderDifficultyText
	{
		get
		{
			return _currentOrderDifficultyText;
		}
		set
		{
			if (value != _currentOrderDifficultyText)
			{
				_currentOrderDifficultyText = value;
				OnPropertyChangedWithValue(value, "CurrentOrderDifficultyText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCraftingSkillValueText
	{
		get
		{
			return _currentCraftingSkillValueText;
		}
		set
		{
			if (value != _currentCraftingSkillValueText)
			{
				_currentCraftingSkillValueText = value;
				OnPropertyChangedWithValue(value, "CurrentCraftingSkillValueText");
			}
		}
	}

	[DataSourceProperty]
	public string DifficultyText
	{
		get
		{
			return _difficultyText;
		}
		set
		{
			if (value != _difficultyText)
			{
				_difficultyText = value;
				OnPropertyChangedWithValue(value, "DifficultyText");
			}
		}
	}

	[DataSourceProperty]
	public string DefaultUsageText
	{
		get
		{
			return _defaultUsageText;
		}
		set
		{
			if (value != _defaultUsageText)
			{
				_defaultUsageText = value;
				OnPropertyChangedWithValue(value, "DefaultUsageText");
			}
		}
	}

	[DataSourceProperty]
	public string AlternativeUsageText
	{
		get
		{
			return _alternativeUsageText;
		}
		set
		{
			if (value != _alternativeUsageText)
			{
				_alternativeUsageText = value;
				OnPropertyChangedWithValue(value, "AlternativeUsageText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel OrderDisabledReasonHint
	{
		get
		{
			return _orderDisabledReasonHint;
		}
		set
		{
			if (value != _orderDisabledReasonHint)
			{
				_orderDisabledReasonHint = value;
				OnPropertyChangedWithValue(value, "OrderDisabledReasonHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ShowOnlyUnlockedPiecesHint
	{
		get
		{
			return _showOnlyUnlockedPiecesHint;
		}
		set
		{
			if (value != _showOnlyUnlockedPiecesHint)
			{
				_showOnlyUnlockedPiecesHint = value;
				OnPropertyChangedWithValue(value, "ShowOnlyUnlockedPiecesHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DifficultyExplanationHint
	{
		get
		{
			return _difficultyExplanationHint;
		}
		set
		{
			if (value != _difficultyExplanationHint)
			{
				_difficultyExplanationHint = value;
				OnPropertyChangedWithValue(value, "DifficultyExplanationHint");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceListVM ActivePieceList
	{
		get
		{
			return _activePieceList;
		}
		set
		{
			if (value != _activePieceList)
			{
				_activePieceList = value;
				OnPropertyChangedWithValue(value, "ActivePieceList");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceListVM BladePieceList
	{
		get
		{
			return _bladePieceList;
		}
		set
		{
			if (value != _bladePieceList)
			{
				_bladePieceList = value;
				OnPropertyChangedWithValue(value, "BladePieceList");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceListVM GuardPieceList
	{
		get
		{
			return _guardPieceList;
		}
		set
		{
			if (value != _guardPieceList)
			{
				_guardPieceList = value;
				OnPropertyChangedWithValue(value, "GuardPieceList");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceListVM HandlePieceList
	{
		get
		{
			return _handlePieceList;
		}
		set
		{
			if (value != _handlePieceList)
			{
				_handlePieceList = value;
				OnPropertyChangedWithValue(value, "HandlePieceList");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceListVM PommelPieceList
	{
		get
		{
			return _pommelPieceList;
		}
		set
		{
			if (value != _pommelPieceList)
			{
				_pommelPieceList = value;
				OnPropertyChangedWithValue(value, "PommelPieceList");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceVM SelectedBladePiece
	{
		get
		{
			return _selectedBladePiece;
		}
		set
		{
			if (value != _selectedBladePiece)
			{
				_selectedBladePiece = value;
				OnPropertyChangedWithValue(value, "SelectedBladePiece");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceVM SelectedGuardPiece
	{
		get
		{
			return _selectedGuardPiece;
		}
		set
		{
			if (value != _selectedGuardPiece)
			{
				_selectedGuardPiece = value;
				OnPropertyChangedWithValue(value, "SelectedGuardPiece");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceVM SelectedHandlePiece
	{
		get
		{
			return _selectedHandlePiece;
		}
		set
		{
			if (value != _selectedHandlePiece)
			{
				_selectedHandlePiece = value;
				OnPropertyChangedWithValue(value, "SelectedHandlePiece");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceVM SelectedPommelPiece
	{
		get
		{
			return _selectedPommelPiece;
		}
		set
		{
			if (value != _selectedPommelPiece)
			{
				_selectedPommelPiece = value;
				OnPropertyChangedWithValue(value, "SelectedPommelPiece");
			}
		}
	}

	[DataSourceProperty]
	public int ActivePieceSize
	{
		get
		{
			if (ActivePieceList == null)
			{
				return 0;
			}
			return ActivePieceList.PieceType switch
			{
				CraftingPiece.PieceTypes.Blade => BladeSize, 
				CraftingPiece.PieceTypes.Guard => GuardSize, 
				CraftingPiece.PieceTypes.Handle => HandleSize, 
				CraftingPiece.PieceTypes.Pommel => PommelSize, 
				_ => 0, 
			};
		}
		set
		{
			if (value != ActivePieceSize && ActivePieceList != null)
			{
				switch (ActivePieceList.PieceType)
				{
				case CraftingPiece.PieceTypes.Blade:
					BladeSize = value;
					break;
				case CraftingPiece.PieceTypes.Guard:
					GuardSize = value;
					break;
				case CraftingPiece.PieceTypes.Handle:
					HandleSize = value;
					break;
				case CraftingPiece.PieceTypes.Pommel:
					PommelSize = value;
					break;
				case CraftingPiece.PieceTypes.Invalid:
				case CraftingPiece.PieceTypes.NumberOfPieceTypes:
					break;
				}
			}
		}
	}

	[DataSourceProperty]
	public int BladeSize
	{
		get
		{
			return _bladeSize;
		}
		set
		{
			if (value != _bladeSize)
			{
				_bladeSize = value;
				OnPropertyChangedWithValue(value, "BladeSize");
				if (_crafting != null && _updatePiece && _crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Blade))
				{
					int percentage = 100 + value;
					_crafting.ScaleThePiece(CraftingPiece.PieceTypes.Blade, percentage);
					RefreshItem();
				}
				OnPropertyChanged("ActivePieceSize");
			}
		}
	}

	[DataSourceProperty]
	public int GuardSize
	{
		get
		{
			return _guardSize;
		}
		set
		{
			if (value != _guardSize)
			{
				_guardSize = value;
				OnPropertyChangedWithValue(value, "GuardSize");
				if (_crafting != null && _updatePiece && _crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Guard))
				{
					int percentage = 100 + value;
					_crafting.ScaleThePiece(CraftingPiece.PieceTypes.Guard, percentage);
					RefreshItem();
				}
				OnPropertyChanged("ActivePieceSize");
			}
		}
	}

	[DataSourceProperty]
	public int HandleSize
	{
		get
		{
			return _handleSize;
		}
		set
		{
			if (value != _handleSize)
			{
				_handleSize = value;
				OnPropertyChangedWithValue(value, "HandleSize");
				if (_crafting != null && _updatePiece && _crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Handle))
				{
					int percentage = 100 + value;
					_crafting.ScaleThePiece(CraftingPiece.PieceTypes.Handle, percentage);
					RefreshItem();
				}
				OnPropertyChanged("ActivePieceSize");
			}
		}
	}

	[DataSourceProperty]
	public int PommelSize
	{
		get
		{
			return _pommelSize;
		}
		set
		{
			if (value != _pommelSize)
			{
				_pommelSize = value;
				OnPropertyChangedWithValue(value, "PommelSize");
				if (_crafting != null && _updatePiece && _crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Pommel))
				{
					int percentage = 100 + value;
					_crafting.ScaleThePiece(CraftingPiece.PieceTypes.Pommel, percentage);
					RefreshItem();
				}
				OnPropertyChanged("ActivePieceSize");
			}
		}
	}

	[DataSourceProperty]
	public string ComponentSizeLbl
	{
		get
		{
			return _componentSizeLbl;
		}
		set
		{
			if (value != _componentSizeLbl)
			{
				_componentSizeLbl = value;
				OnPropertyChangedWithValue(value, "ComponentSizeLbl");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWeaponCivilian
	{
		get
		{
			return _isWeaponCivilian;
		}
		set
		{
			if (value != _isWeaponCivilian)
			{
				_isWeaponCivilian = value;
				OnPropertyChangedWithValue(value, "IsWeaponCivilian");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ScabbardHint
	{
		get
		{
			return _scabbardHint;
		}
		set
		{
			if (value != _scabbardHint)
			{
				_scabbardHint = value;
				OnPropertyChangedWithValue(value, "ScabbardHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RandomizeHint
	{
		get
		{
			return _randomizeHint;
		}
		set
		{
			if (value != _randomizeHint)
			{
				_randomizeHint = value;
				OnPropertyChangedWithValue(value, "RandomizeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UndoHint
	{
		get
		{
			return _undoHint;
		}
		set
		{
			if (value != _undoHint)
			{
				_undoHint = value;
				OnPropertyChangedWithValue(value, "UndoHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RedoHint
	{
		get
		{
			return _redoHint;
		}
		set
		{
			if (value != _redoHint)
			{
				_redoHint = value;
				OnPropertyChangedWithValue(value, "RedoHint");
			}
		}
	}

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
	public CraftingHistoryVM CraftingHistory
	{
		get
		{
			return _craftingHistory;
		}
		set
		{
			if (value != _craftingHistory)
			{
				_craftingHistory = value;
				OnPropertyChangedWithValue(value, "CraftingHistory");
			}
		}
	}

	public WeaponDesignVM(Crafting crafting, ICraftingCampaignBehavior craftingBehavior, Action onRefresh, Action onWeaponCrafted, Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero, Action<CraftingOrder> refreshHeroAvailabilities, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags)
	{
		_crafting = crafting;
		_craftingBehavior = craftingBehavior;
		_onRefresh = onRefresh;
		_onWeaponCrafted = onWeaponCrafted;
		_getCurrentCraftingHero = getCurrentCraftingHero;
		_getItemUsageSetFlags = getItemUsageSetFlags;
		_refreshHeroAvailabilities = refreshHeroAvailabilities;
		MaxDifficulty = 300;
		_currentCraftingSkillText = new TextObject("{=LEiZWuZm}{SKILL_NAME}: {SKILL_VALUE}");
		PrimaryPropertyList = new MBBindingList<CraftingListPropertyItem>();
		DesignResultPropertyList = new MBBindingList<WeaponDesignResultPropertyItemVM>();
		_newlyUnlockedPieces = new List<CraftingPiece>();
		_pieceTierComparer = new PieceTierComparer();
		BladePieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Blade, OnSelectPieceType);
		GuardPieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Guard, OnSelectPieceType);
		HandlePieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Handle, OnSelectPieceType);
		PommelPieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Pommel, OnSelectPieceType);
		PieceLists = new MBBindingList<CraftingPieceListVM> { BladePieceList, GuardPieceList, HandlePieceList, PommelPieceList };
		_pieceListsDictionary = new Dictionary<CraftingPiece.PieceTypes, CraftingPieceListVM>
		{
			{
				CraftingPiece.PieceTypes.Blade,
				BladePieceList
			},
			{
				CraftingPiece.PieceTypes.Guard,
				GuardPieceList
			},
			{
				CraftingPiece.PieceTypes.Handle,
				HandlePieceList
			},
			{
				CraftingPiece.PieceTypes.Pommel,
				PommelPieceList
			}
		};
		_pieceVMs = new Dictionary<CraftingPiece, CraftingPieceVM>();
		TierFilters = new MBBindingList<TierFilterTypeVM>
		{
			new TierFilterTypeVM(CraftingPieceTierFilter.All, OnSelectPieceTierFilter, GameTexts.FindText("str_crafting_tier_filter_all").ToString()),
			new TierFilterTypeVM(CraftingPieceTierFilter.Tier1, OnSelectPieceTierFilter, GameTexts.FindText("str_tier_one").ToString()),
			new TierFilterTypeVM(CraftingPieceTierFilter.Tier2, OnSelectPieceTierFilter, GameTexts.FindText("str_tier_two").ToString()),
			new TierFilterTypeVM(CraftingPieceTierFilter.Tier3, OnSelectPieceTierFilter, GameTexts.FindText("str_tier_three").ToString()),
			new TierFilterTypeVM(CraftingPieceTierFilter.Tier4, OnSelectPieceTierFilter, GameTexts.FindText("str_tier_four").ToString()),
			new TierFilterTypeVM(CraftingPieceTierFilter.Tier5, OnSelectPieceTierFilter, GameTexts.FindText("str_tier_five").ToString())
		};
		_templateComparer = new TemplateComparer();
		_primaryUsages = CraftingTemplate.All.ToList();
		_primaryUsages.Sort(_templateComparer);
		SecondaryUsageSelector = new SelectorVM<CraftingSecondaryUsageItemVM>(new List<string>(), 0, null);
		CraftingOrderPopup = new CraftingOrderPopupVM(OnCraftingOrderSelected, _getCurrentCraftingHero, GetOrderStatDatas);
		WeaponClassSelectionPopup = new WeaponClassSelectionPopupVM(_craftingBehavior, _primaryUsages, delegate(int x)
		{
			RefreshWeaponDesignMode(null, x);
		}, GetUnlockedPartsCount);
		WeaponFlagIconsList = new MBBindingList<ItemFlagVM>();
		CraftedItemVisual = new ItemCollectionElementViewModel();
		CampaignEvents.CraftingPartUnlockedEvent.AddNonSerializedListener(this, OnNewPieceUnlocked);
		CraftingHistory = new CraftingHistoryVM(_crafting, _craftingBehavior, () => ActiveCraftingOrder?.CraftingOrder, OnSelectItemFromHistory);
		RefreshWeaponDesignMode(null);
		_selectedWeaponClassIndex = _primaryUsages.IndexOf(_crafting.CurrentCraftingTemplate);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ShowOnlyUnlockedPiecesHint = new HintViewModel(new TextObject("{=dOa7frHR}Show only unlocked pieces"));
		ComponentSizeLbl = new TextObject("{=OkWLI5C8}Size:").ToString();
		AlternativeUsageText = new TextObject("{=13wo3QQB}Secondary").ToString();
		DefaultUsageText = new TextObject("{=ta4R2RR7}Primary").ToString();
		DifficultyText = GameTexts.FindText("str_difficulty").ToString();
		ScabbardHint = new HintViewModel(GameTexts.FindText("str_toggle_scabbard"));
		RandomizeHint = new HintViewModel(GameTexts.FindText("str_randomize"));
		UndoHint = new HintViewModel(GameTexts.FindText("str_undo"));
		RedoHint = new HintViewModel(GameTexts.FindText("str_redo"));
		DifficultyExplanationHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetSmithingDifficultyTooltip());
		OrderDisabledReasonHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetOrdersDisabledReasonTooltip(CraftingOrderPopup.CraftingOrders, _getCurrentCraftingHero().Hero));
		_primaryPropertyList.ApplyActionOnAllItems(delegate(CraftingListPropertyItem x)
		{
			x.RefreshValues();
		});
		_selectedBladePiece?.RefreshValues();
		_selectedGuardPiece?.RefreshValues();
		_selectedHandlePiece?.RefreshValues();
		_selectedPommelPiece?.RefreshValues();
		_secondaryUsageSelector.RefreshValues();
		_craftingOrderPopup.RefreshValues();
		ChooseOrderText = CraftingOrderPopup.OrderCountText;
		ChooseWeaponTypeText = new TextObject("{=Gd6zuUwh}Free Build").ToString();
		CurrentCraftedWeaponTypeText = _crafting.CurrentCraftingTemplate.TemplateName.ToString();
		CurrentCraftedWeaponTemplateId = _crafting.CurrentCraftingTemplate.StringId;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.CraftingPartUnlockedEvent.ClearListeners(this);
		CraftingHistory?.OnFinalize();
		CraftedItemVisual?.OnFinalize();
		CraftingResultPopup?.OnFinalize();
		CraftedItemVisual = null;
	}

	internal void OnCraftingLogicRefreshed(Crafting newCraftingLogic)
	{
		_crafting = newCraftingLogic;
		InitializeDefaultFromLogic();
	}

	private void FilterPieces(CraftingPieceTierFilter filter)
	{
		List<int> list = new List<int>();
		switch (filter)
		{
		case CraftingPieceTierFilter.Tier1:
			list.Add(1);
			break;
		case CraftingPieceTierFilter.Tier2:
			list.Add(2);
			break;
		case CraftingPieceTierFilter.Tier3:
			list.Add(3);
			break;
		case CraftingPieceTierFilter.Tier4:
			list.Add(4);
			break;
		case CraftingPieceTierFilter.Tier5:
			list.Add(5);
			break;
		case CraftingPieceTierFilter.All:
			list.AddRange(new int[5] { 1, 2, 3, 4, 5 });
			break;
		default:
			Debug.FailedAssert("Invalid tier filter", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Crafting\\WeaponDesign\\WeaponDesignVM.cs", "FilterPieces", 218);
			break;
		case CraftingPieceTierFilter.None:
			break;
		}
		foreach (TierFilterTypeVM tierFilter in TierFilters)
		{
			tierFilter.IsSelected = filter.HasAllFlags(tierFilter.FilterType);
		}
		foreach (CraftingPieceListVM pieceList in PieceLists)
		{
			foreach (CraftingPieceVM piece in pieceList.Pieces)
			{
				bool flag = list.Contains(piece.CraftingPiece.CraftingPiece.PieceTier);
				bool flag2 = ShowOnlyUnlockedPieces && !piece.PlayerHasPiece;
				piece.IsFilteredOut = !flag || flag2;
			}
		}
		_currentTierFilter = filter;
	}

	private void OnNewPieceUnlocked(CraftingPiece piece)
	{
		if (piece.IsValid && !piece.IsHiddenOnDesigner)
		{
			SetPieceNewlyUnlocked(piece);
			if (_pieceVMs.TryGetValue(piece, out var value))
			{
				value.PlayerHasPiece = true;
				value.IsNewlyUnlocked = true;
			}
		}
	}

	private int GetUnlockedPartsCount(CraftingTemplate template)
	{
		return template.Pieces.Count((CraftingPiece piece) => _craftingBehavior.IsOpened(piece, template) && !string.IsNullOrEmpty(piece.MeshName));
	}

	private WeaponClassVM GetCurrentWeaponClass()
	{
		if (_selectedWeaponClassIndex >= 0 && _selectedWeaponClassIndex < WeaponClassSelectionPopup.WeaponClasses.Count)
		{
			return WeaponClassSelectionPopup.WeaponClasses[_selectedWeaponClassIndex];
		}
		return null;
	}

	private void OnSelectItemFromHistory(WeaponDesignSelectorVM selector)
	{
		TaleWorlds.Core.WeaponDesign design = selector.Design;
		if (design == null)
		{
			Debug.FailedAssert("History design returned null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Crafting\\WeaponDesign\\WeaponDesignVM.cs", "OnSelectItemFromHistory", 284);
			return;
		}
		(CraftingPiece, int)[] array = new(CraftingPiece, int)[design.UsedPieces.Length];
		for (int i = 0; i < design.UsedPieces.Length; i++)
		{
			array[i] = (design.UsedPieces[i].CraftingPiece, design.UsedPieces[i].ScalePercentage);
		}
		SetDesignManually(design.Template, array, forceChangeTemplate: true);
	}

	public void SetPieceNewlyUnlocked(CraftingPiece piece)
	{
		if (!_newlyUnlockedPieces.Contains(piece))
		{
			_newlyUnlockedPieces.Add(piece);
		}
	}

	private void UnsetPieceNewlyUnlocked(CraftingPieceVM pieceVM)
	{
		CraftingPiece craftingPiece = pieceVM.CraftingPiece.CraftingPiece;
		if (_newlyUnlockedPieces.Contains(craftingPiece))
		{
			_newlyUnlockedPieces.Remove(craftingPiece);
			pieceVM.IsNewlyUnlocked = false;
		}
	}

	private void OnSelectPieceTierFilter(CraftingPieceTierFilter filter)
	{
		if (_currentTierFilter != filter)
		{
			FilterPieces(filter);
		}
	}

	private void OnSelectPieceType(CraftingPiece.PieceTypes pieceType, bool fromClick = false)
	{
		CraftingPieceListVM craftingPieceListVM = PieceLists.ElementAt(SelectedPieceTypeIndex);
		if (craftingPieceListVM != null && fromClick)
		{
			foreach (CraftingPieceVM piece in craftingPieceListVM.Pieces)
			{
				if (piece.IsNewlyUnlocked)
				{
					UnsetPieceNewlyUnlocked(piece);
				}
			}
		}
		foreach (CraftingPieceListVM pieceList in PieceLists)
		{
			pieceList.Refresh();
			if (pieceList.PieceType == pieceType)
			{
				pieceList.IsSelected = true;
				ActivePieceList = pieceList;
			}
			else
			{
				pieceList.IsSelected = false;
			}
		}
		SelectedPieceTypeIndex = (int)pieceType;
		OnPropertyChanged("ActivePieceSize");
	}

	private void SelectDefaultPiecesForCurrentTemplate()
	{
		string text = ActiveCraftingOrder?.CraftingOrder.GetStatWeapon().WeaponDescriptionId;
		WeaponDescription statWeaponUsage = ((text != null) ? MBObjectManager.Instance.GetObject<WeaponDescription>(text) : null);
		WeaponClassVM currentWeaponClass = GetCurrentWeaponClass();
		_shouldRecordHistory = false;
		_isAutoSelectingPieces = true;
		foreach (CraftingPieceListVM pieceList in PieceLists)
		{
			if (!_crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceList.PieceType))
			{
				continue;
			}
			CraftingPieceVM craftingPieceVM = null;
			if (IsInFreeMode && currentWeaponClass != null)
			{
				string selectedPieceID = currentWeaponClass.GetSelectedPieceData(pieceList.PieceType);
				craftingPieceVM = pieceList.Pieces.FirstOrDefault((CraftingPieceVM p) => p.CraftingPiece.CraftingPiece.StringId == selectedPieceID);
			}
			if (craftingPieceVM == null)
			{
				craftingPieceVM = (from p in pieceList.Pieces
					orderby p.PlayerHasPiece descending, !p.IsNewlyUnlocked descending, statWeaponUsage == null || statWeaponUsage.AvailablePieces.Any((CraftingPiece x) => x.StringId == p.CraftingPiece.CraftingPiece.StringId) descending
					select p).FirstOrDefault();
			}
			craftingPieceVM?.ExecuteSelect();
		}
		_shouldRecordHistory = true;
		_isAutoSelectingPieces = false;
	}

	private void InitializeDefaultFromLogic()
	{
		PrimaryPropertyList.Clear();
		BladePieceList.Pieces.Clear();
		GuardPieceList.Pieces.Clear();
		HandlePieceList.Pieces.Clear();
		PommelPieceList.Pieces.Clear();
		SelectedBladePiece = new CraftingPieceVM();
		SelectedGuardPiece = new CraftingPieceVM();
		SelectedHandlePiece = new CraftingPieceVM();
		SelectedPommelPiece = new CraftingPieceVM();
		_pieceVMs.Clear();
		bool flag = Campaign.Current.GameMode == CampaignGameMode.Tutorial;
		foreach (CraftingPieceListVM pieceList in PieceLists)
		{
			if (!_crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceList.PieceType))
			{
				continue;
			}
			int pieceType = (int)pieceList.PieceType;
			for (int i = 0; i < _crafting.UsablePiecesList[pieceType].Count; i++)
			{
				WeaponDesignElement weaponDesignElement = _crafting.UsablePiecesList[pieceType][i];
				if (flag || !weaponDesignElement.CraftingPiece.IsHiddenOnDesigner)
				{
					bool flag2 = _craftingBehavior.IsOpened(weaponDesignElement.CraftingPiece, _crafting.CurrentCraftingTemplate);
					CraftingPieceVM craftingPieceVM = new CraftingPieceVM(OnSetItemPieceManually, _crafting.CurrentCraftingTemplate.StringId, _crafting.UsablePiecesList[pieceType][i], pieceType, i, flag2);
					pieceList.Pieces.Add(craftingPieceVM);
					craftingPieceVM.IsNewlyUnlocked = flag2 && _newlyUnlockedPieces.Contains(weaponDesignElement.CraftingPiece);
					if (_crafting.SelectedPieces[pieceType].CraftingPiece == craftingPieceVM.CraftingPiece.CraftingPiece)
					{
						pieceList.SelectedPiece = craftingPieceVM;
						craftingPieceVM.IsSelected = true;
					}
					_pieceVMs.Add(_crafting.UsablePiecesList[pieceType][i].CraftingPiece, craftingPieceVM);
				}
			}
			pieceList.Pieces.Sort(_pieceTierComparer);
		}
		SelectedBladePiece = PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Blade)?.SelectedPiece;
		SelectedGuardPiece = PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Guard)?.SelectedPiece;
		SelectedHandlePiece = PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Handle)?.SelectedPiece;
		SelectedPommelPiece = PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Pommel)?.SelectedPiece;
		ItemName = _crafting.CraftedWeaponName.ToString();
		PommelSize = 0;
		GuardSize = 0;
		HandleSize = 0;
		BladeSize = 0;
		RefreshPieceFlags();
		RefreshItem();
		RefreshAlternativeUsageList();
	}

	private void RefreshPieceFlags()
	{
		foreach (CraftingPieceListVM pieceList in PieceLists)
		{
			pieceList.IsEnabled = _crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceList.PieceType);
			foreach (CraftingPieceVM piece in pieceList.Pieces)
			{
				piece.RefreshFlagIcons();
				if (pieceList.PieceType == CraftingPiece.PieceTypes.Blade)
				{
					AddClassFlagsToPiece(piece);
				}
			}
		}
		RefreshWeaponFlags();
	}

	private void AddClassFlagsToPiece(CraftingPieceVM piece)
	{
		WeaponComponentData weaponWithUsageIndex = _crafting.GetCurrentCraftedItemObject().GetWeaponWithUsageIndex(SecondaryUsageSelector.SelectedIndex);
		int indexOfUsageDataWithId = _crafting.CurrentCraftingTemplate.GetIndexOfUsageDataWithId(weaponWithUsageIndex.WeaponDescriptionId);
		WeaponDescription weaponDescription = _crafting.CurrentCraftingTemplate.WeaponDescriptions.ElementAtOrDefault(indexOfUsageDataWithId);
		if (weaponDescription != null)
		{
			foreach (var flagPath in CampaignUIHelper.GetWeaponFlagDetails(weaponDescription.WeaponFlags))
			{
				if (!piece.ItemAttributeIcons.Any((CraftingItemFlagVM x) => x.Icon.Contains(flagPath.Item1)))
				{
					piece.ItemAttributeIcons.Add(new CraftingItemFlagVM(flagPath.Item1, flagPath.Item2, isDisplayed: false));
				}
			}
		}
		foreach (var usageFlag in CampaignUIHelper.GetFlagDetailsForWeapon(weaponWithUsageIndex, _getItemUsageSetFlags(weaponWithUsageIndex)))
		{
			if (!piece.ItemAttributeIcons.Any((CraftingItemFlagVM x) => x.Icon.Contains(usageFlag.Item1)))
			{
				piece.ItemAttributeIcons.Add(new CraftingItemFlagVM(usageFlag.Item1, usageFlag.Item2, isDisplayed: false));
			}
		}
	}

	private void UpdateSecondaryUsageIndex(SelectorVM<CraftingSecondaryUsageItemVM> selector)
	{
		if (selector.SelectedIndex != -1)
		{
			RefreshStats();
			RefreshPieceFlags();
		}
	}

	private MBBindingList<WeaponDesignResultPropertyItemVM> GetResultPropertyList(CraftingSecondaryUsageItemVM usageItem)
	{
		MBBindingList<WeaponDesignResultPropertyItemVM> mBBindingList = new MBBindingList<WeaponDesignResultPropertyItemVM>();
		if (usageItem == null)
		{
			return mBBindingList;
		}
		int usageIndex = usageItem.UsageIndex;
		TrySetSecondaryUsageIndex(usageIndex);
		RefreshStats();
		ItemModifier currentItemModifier = _craftingBehavior.GetCurrentItemModifier();
		foreach (CraftingListPropertyItem primaryProperty in PrimaryPropertyList)
		{
			float changeAmount = 0f;
			bool showFloatingPoint = primaryProperty.Type == CraftingTemplate.CraftingStatTypes.Weight;
			if (currentItemModifier != null)
			{
				float num = primaryProperty.PropertyValue;
				if (primaryProperty.Type == CraftingTemplate.CraftingStatTypes.SwingDamage)
				{
					num = currentItemModifier.ModifyDamage((int)primaryProperty.PropertyValue);
				}
				else if (primaryProperty.Type == CraftingTemplate.CraftingStatTypes.SwingSpeed)
				{
					num = currentItemModifier.ModifySpeed((int)primaryProperty.PropertyValue);
				}
				else if (primaryProperty.Type == CraftingTemplate.CraftingStatTypes.ThrustDamage)
				{
					num = currentItemModifier.ModifyDamage((int)primaryProperty.PropertyValue);
				}
				else if (primaryProperty.Type == CraftingTemplate.CraftingStatTypes.ThrustSpeed)
				{
					num = currentItemModifier.ModifySpeed((int)primaryProperty.PropertyValue);
				}
				else if (primaryProperty.Type == CraftingTemplate.CraftingStatTypes.Handling)
				{
					num = currentItemModifier.ModifySpeed((int)primaryProperty.PropertyValue);
				}
				if (num != primaryProperty.PropertyValue)
				{
					changeAmount = num - primaryProperty.PropertyValue;
				}
			}
			if (IsInOrderMode)
			{
				mBBindingList.Add(new WeaponDesignResultPropertyItemVM(primaryProperty.Description, primaryProperty.PropertyValue, primaryProperty.TargetValue, changeAmount, showFloatingPoint, primaryProperty.IsExceedingBeneficial));
			}
			else
			{
				mBBindingList.Add(new WeaponDesignResultPropertyItemVM(primaryProperty.Description, primaryProperty.PropertyValue, changeAmount, showFloatingPoint));
			}
		}
		return mBBindingList;
	}

	public void SelectPrimaryWeaponClass(CraftingTemplate template)
	{
		int selectedWeaponClassIndex = _primaryUsages.IndexOf(template);
		_selectedWeaponClassIndex = selectedWeaponClassIndex;
		if (_crafting.CurrentCraftingTemplate != template)
		{
			CraftingHelper.ChangeCurrentCraftingTemplate(template);
		}
		else
		{
			AddHistoryKey();
		}
	}

	private void RefreshWeaponDesignMode(CraftingOrderItemVM orderToSelect, int classIndex = -1, bool doNotAutoSelectPieces = false)
	{
		bool flag = false;
		CraftingTemplate selectedCraftingTemplate = null;
		SecondaryUsageSelector.SelectedIndex = 0;
		if (orderToSelect != null)
		{
			IsInOrderMode = true;
			ActiveCraftingOrder = orderToSelect;
			selectedCraftingTemplate = orderToSelect.CraftingOrder.PreCraftedWeaponDesignItem.WeaponDesign.Template;
			SelectPrimaryWeaponClass(selectedCraftingTemplate);
			flag = true;
		}
		else
		{
			IsInOrderMode = false;
			ActiveCraftingOrder = null;
			if (classIndex >= 0)
			{
				selectedCraftingTemplate = _primaryUsages[classIndex];
				SelectPrimaryWeaponClass(selectedCraftingTemplate);
				flag = true;
			}
		}
		WeaponClassVM weaponClassVM = WeaponClassSelectionPopup.WeaponClasses.FirstOrDefault((WeaponClassVM x) => x.Template == selectedCraftingTemplate);
		if (weaponClassVM != null)
		{
			weaponClassVM.NewlyUnlockedPieceCount = 0;
		}
		CraftingOrderPopup.RefreshOrders();
		CraftingHistory.RefreshAvailability();
		IsOrderButtonActive = CraftingOrderPopup.HasEnabledOrders;
		_onRefresh?.Invoke();
		_refreshHeroAvailabilities?.Invoke(ActiveCraftingOrder?.CraftingOrder);
		if (!flag)
		{
			InitializeDefaultFromLogic();
		}
		RefreshValues();
		RefreshItem();
		OnSelectPieceType(CraftingPiece.PieceTypes.Blade);
		FilterPieces(_currentTierFilter);
		RefreshCurrentHeroSkillLevel();
		if (!doNotAutoSelectPieces)
		{
			SelectDefaultPiecesForCurrentTemplate();
		}
	}

	private void OnCraftingOrderSelected(CraftingOrderItemVM selectedOrder)
	{
		RefreshWeaponDesignMode(selectedOrder);
	}

	public void ExecuteOpenOrderPopup()
	{
		CraftingOrderPopup.ExecuteOpenPopup();
		CraftingOrderItemVM craftingOrderItemVM = CraftingOrderPopup.CraftingOrders?.FirstOrDefault((CraftingOrderItemVM x) => x.CraftingOrder == ActiveCraftingOrder?.CraftingOrder);
		if (craftingOrderItemVM != null)
		{
			craftingOrderItemVM.IsSelected = true;
		}
	}

	public void ExecuteCloseOrderPopup()
	{
		CraftingOrderPopup.IsVisible = false;
	}

	public void ExecuteOpenOrdersTab()
	{
		if (IsInFreeMode)
		{
			CraftingOrderItemVM craftingOrderItemVM = CraftingOrderPopup.CraftingOrders?.FirstOrDefault((CraftingOrderItemVM x) => x.IsEnabled);
			if (craftingOrderItemVM != null)
			{
				CraftingOrderPopup.SelectOrder(craftingOrderItemVM);
			}
			else
			{
				CraftingOrderPopup.ExecuteOpenPopup();
			}
			Game.Current?.EventManager.TriggerEvent(new CraftingOrderTabOpenedEvent(isOpen: true));
		}
	}

	public void ExecuteOpenWeaponClassSelectionPopup()
	{
		WeaponClassSelectionPopup.UpdateNewlyUnlockedPiecesCount(_newlyUnlockedPieces);
		WeaponClassSelectionPopup.WeaponClasses.ApplyActionOnAllItems(delegate(WeaponClassVM x)
		{
			x.IsSelected = x.SelectionIndex == _selectedWeaponClassIndex;
		});
		WeaponClassSelectionPopup.ExecuteOpenPopup();
	}

	public void ExecuteOpenFreeBuildTab()
	{
		if (IsInOrderMode)
		{
			WeaponClassSelectionPopup.UpdateNewlyUnlockedPiecesCount(_newlyUnlockedPieces);
			WeaponClassSelectionPopup.WeaponClasses.ApplyActionOnAllItems(delegate(WeaponClassVM x)
			{
				x.IsSelected = false;
			});
			WeaponClassSelectionPopup.ExecuteSelectWeaponClass(0);
			Game.Current?.EventManager.TriggerEvent(new CraftingOrderTabOpenedEvent(isOpen: false));
		}
	}

	public void CreateCraftingResultPopup()
	{
		CraftedItemVisual.StringId = CraftedItemObject.StringId;
		IsWeaponCivilian = CraftedItemObject.IsCivilian;
		CraftingResultPopup?.OnFinalize();
		CraftingResultPopup = new WeaponDesignResultPopupVM(CraftedItemObject, _crafting.CraftedWeaponName, ExecuteFinalizeCrafting, _crafting, ActiveCraftingOrder?.CraftingOrder, _craftedItemVisual, WeaponFlagIconsList, GetResultPropertyList, OnSecondaryUsageChangedFromPopup);
	}

	private void OnSecondaryUsageChangedFromPopup(CraftingSecondaryUsageItemVM usage)
	{
		for (int i = 0; i < SecondaryUsageSelector.ItemList.Count; i++)
		{
			if (SecondaryUsageSelector.ItemList[i].UsageIndex == usage.UsageIndex)
			{
				SecondaryUsageSelector.SelectedIndex = i;
				break;
			}
		}
	}

	public void ExecuteToggleShowOnlyUnlockedPieces()
	{
		ShowOnlyUnlockedPieces = !ShowOnlyUnlockedPieces;
		FilterPieces(_currentTierFilter);
	}

	public void ExecuteUndo()
	{
		if (!_crafting.Undo())
		{
			return;
		}
		_onRefresh?.Invoke();
		_updatePiece = false;
		int i;
		for (i = 0; i < 4; i++)
		{
			CraftingPiece.PieceTypes pieceTypes = (CraftingPiece.PieceTypes)i;
			if (_crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceTypes))
			{
				CraftingPieceVM piece = _pieceListsDictionary[pieceTypes].Pieces.First((CraftingPieceVM craftingPieceVM) => craftingPieceVM.CraftingPiece.CraftingPiece == _crafting.SelectedPieces[i].CraftingPiece);
				OnSetItemPiece(piece);
			}
		}
		RefreshItem();
		_updatePiece = true;
	}

	public void ExecuteRedo()
	{
		if (!_crafting.Redo())
		{
			return;
		}
		_onRefresh?.Invoke();
		_updatePiece = false;
		int i;
		for (i = 0; i < 4; i++)
		{
			CraftingPiece.PieceTypes pieceTypes = (CraftingPiece.PieceTypes)i;
			if (_crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceTypes))
			{
				CraftingPieceVM piece = _pieceListsDictionary[pieceTypes].Pieces.First((CraftingPieceVM craftingPieceVM) => craftingPieceVM.CraftingPiece.CraftingPiece == _crafting.SelectedPieces[i].CraftingPiece);
				OnSetItemPiece(piece);
			}
		}
		RefreshItem();
		_updatePiece = true;
	}

	internal void OnCraftingHeroChanged(CraftingAvailableHeroItemVM newHero)
	{
		RefreshCurrentHeroSkillLevel();
		RefreshDifficulty();
		CraftingOrderPopup.RefreshOrders();
		IsOrderButtonActive = CraftingOrderPopup.HasEnabledOrders;
	}

	public void ChangeModeIfHeroIsUnavailable()
	{
		CraftingAvailableHeroItemVM craftingAvailableHeroItemVM = _getCurrentCraftingHero();
		if (IsInOrderMode && craftingAvailableHeroItemVM.IsDisabled)
		{
			RefreshWeaponDesignMode(null);
		}
	}

	public void ExecuteBeginHeroHint()
	{
		if (_activeCraftingOrder?.CraftingOrder.OrderOwner != null)
		{
			InformationManager.ShowTooltip(typeof(Hero), _activeCraftingOrder.CraftingOrder.OrderOwner, false);
		}
	}

	public void ExecuteEndHeroHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteRandomize()
	{
		for (int i = 0; i < 4; i++)
		{
			CraftingPiece.PieceTypes pieceTypes = (CraftingPiece.PieceTypes)i;
			if (_crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceTypes))
			{
				CraftingPieceVM randomElementWithPredicate = _pieceListsDictionary[pieceTypes].Pieces.GetRandomElementWithPredicate((CraftingPieceVM p) => p.PlayerHasPiece);
				if (randomElementWithPredicate != null)
				{
					OnSetItemPiece(randomElementWithPredicate, (int)(90f + MBRandom.RandomFloat * 20f), shouldUpdateWholeWeapon: false, forceUpdatePiece: true);
				}
			}
		}
		_updatePiece = false;
		RefreshItem();
		AddHistoryKey();
		_updatePiece = true;
	}

	public void ExecuteChangeScabbardVisibility()
	{
		if (!_crafting.CurrentCraftingTemplate.UseWeaponAsHolsterMesh)
		{
			IsScabbardVisible = !IsScabbardVisible;
		}
	}

	public void SelectWeapon(ItemObject itemObject)
	{
		_crafting.SwitchToCraftedItem(itemObject);
		_onRefresh?.Invoke();
		_updatePiece = false;
		int i;
		for (i = 0; i < 4; i++)
		{
			CraftingPiece.PieceTypes pieceTypes = (CraftingPiece.PieceTypes)i;
			if (_crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceTypes))
			{
				CraftingPieceVM piece = _pieceListsDictionary[pieceTypes].Pieces.First((CraftingPieceVM craftingPieceVM) => craftingPieceVM.CraftingPiece.CraftingPiece == _crafting.CurrentWeaponDesign.UsedPieces[i].CraftingPiece);
				OnSetItemPiece(piece, _crafting.CurrentWeaponDesign.UsedPieces[i].ScalePercentage);
			}
		}
		RefreshItem();
		AddHistoryKey();
		_updatePiece = true;
	}

	public bool CanCompleteOrder()
	{
		bool result = true;
		if (IsInOrderMode)
		{
			ItemObject currentCraftedItemObject = _crafting.GetCurrentCraftedItemObject();
			result = ActiveCraftingOrder.CraftingOrder.CanHeroCompleteOrder(_getCurrentCraftingHero().Hero, currentCraftedItemObject);
		}
		return result;
	}

	public void ExecuteFinalizeCrafting()
	{
		if (_craftingBehavior != null && Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			if (GameStateManager.Current.ActiveState is CraftingState)
			{
				if (IsInOrderMode)
				{
					_craftingBehavior.CompleteOrder(Settlement.CurrentSettlement.Town, ActiveCraftingOrder.CraftingOrder, CraftedItemObject, _getCurrentCraftingHero().Hero);
					CraftedItemObject = null;
					CraftingOrderPopup.RefreshOrders();
					CraftingOrderItemVM craftingOrderItemVM = CraftingOrderPopup.CraftingOrders.FirstOrDefault((CraftingOrderItemVM x) => x.IsEnabled);
					if (craftingOrderItemVM != null)
					{
						CraftingOrderPopup.SelectOrder(craftingOrderItemVM);
					}
					else
					{
						ExecuteOpenFreeBuildTab();
					}
				}
				else
				{
					int bladeSize = BladeSize;
					int guardSize = GuardSize;
					int handleSize = HandleSize;
					int pommelSize = PommelSize;
					RefreshWeaponDesignMode(null, _selectedWeaponClassIndex);
					BladeSize = bladeSize;
					GuardSize = guardSize;
					HandleSize = handleSize;
					PommelSize = pommelSize;
				}
			}
			IsInFinalCraftingStage = false;
		}
		TextObject textObject = new TextObject("{=uZhHh7pm}Crafted {CURR_TEMPLATE_NAME}");
		textObject.SetTextVariable("CURR_TEMPLATE_NAME", _crafting.CurrentCraftingTemplate.TemplateName);
		_crafting.SetCraftedWeaponName(textObject);
	}

	private bool DoesCurrentItemHaveSecondaryUsage(int usageIndex)
	{
		if (usageIndex >= 0)
		{
			return usageIndex < _crafting.GetCurrentCraftedItemObject().Weapons.Count;
		}
		return false;
	}

	private void TrySetSecondaryUsageIndex(int usageIndex)
	{
		int num = 0;
		if (DoesCurrentItemHaveSecondaryUsage(usageIndex))
		{
			CraftingSecondaryUsageItemVM craftingSecondaryUsageItemVM = SecondaryUsageSelector.ItemList.FirstOrDefault((CraftingSecondaryUsageItemVM x) => x.UsageIndex == usageIndex);
			if (craftingSecondaryUsageItemVM != null)
			{
				num = craftingSecondaryUsageItemVM.SelectorIndex;
			}
		}
		if (num >= 0 && num < SecondaryUsageSelector.ItemList.Count)
		{
			SecondaryUsageSelector.SelectedIndex = num;
			SecondaryUsageSelector.ItemList[num].IsSelected = true;
		}
	}

	private void RefreshAlternativeUsageList()
	{
		int usageIndex = SecondaryUsageSelector.SelectedIndex;
		SecondaryUsageSelector.Refresh(new List<string>(), 0, UpdateSecondaryUsageIndex);
		MBReadOnlyList<WeaponComponentData> weapons = _crafting.GetCurrentCraftedItemObject().Weapons;
		int num = 0;
		for (int i = 0; i < weapons.Count; i++)
		{
			if (CampaignUIHelper.IsItemUsageApplicable(weapons[i]))
			{
				TextObject name = GameTexts.FindText("str_weapon_usage", weapons[i].WeaponDescriptionId);
				SecondaryUsageSelector.AddItem(new CraftingSecondaryUsageItemVM(name, num, i, SecondaryUsageSelector));
				if (ActiveCraftingOrder?.CraftingOrder.GetStatWeapon().WeaponDescriptionId == weapons[i].WeaponDescriptionId)
				{
					usageIndex = num;
				}
				num++;
			}
		}
		TrySetSecondaryUsageIndex(usageIndex);
	}

	private void RefreshStats()
	{
		if (!DoesCurrentItemHaveSecondaryUsage(SecondaryUsageSelector.SelectedIndex))
		{
			TrySetSecondaryUsageIndex(0);
		}
		List<CraftingStatData> list = _crafting.GetStatDatas(SecondaryUsageSelector.SelectedIndex).ToList();
		WeaponComponentData weaponComponentData = (IsInOrderMode ? ActiveCraftingOrder.CraftingOrder.GetStatWeapon() : null);
		IEnumerable<CraftingStatData> enumerable = (IsInOrderMode ? GetOrderStatDatas(ActiveCraftingOrder.CraftingOrder) : null);
		ItemObject currentCraftedItemObject = _crafting.GetCurrentCraftedItemObject();
		WeaponComponentData weaponWithUsageIndex = currentCraftedItemObject.GetWeaponWithUsageIndex(SecondaryUsageSelector.SelectedIndex);
		bool flag = weaponComponentData == null || weaponComponentData.WeaponDescriptionId == weaponWithUsageIndex.WeaponDescriptionId;
		if (enumerable != null)
		{
			foreach (CraftingStatData orderStatData in enumerable)
			{
				if (!list.Any((CraftingStatData x) => x.Type == orderStatData.Type && x.DamageType == orderStatData.DamageType))
				{
					if ((orderStatData.Type == CraftingTemplate.CraftingStatTypes.SwingDamage && orderStatData.DamageType != weaponWithUsageIndex.SwingDamageType) || (orderStatData.Type == CraftingTemplate.CraftingStatTypes.ThrustDamage && orderStatData.DamageType != weaponWithUsageIndex.ThrustDamageType))
					{
						list.Add(new CraftingStatData(orderStatData.DescriptionText, 0f, orderStatData.MaxValue, orderStatData.Type, orderStatData.DamageType));
					}
					else
					{
						list.Add(orderStatData);
					}
				}
			}
		}
		PrimaryPropertyList.Clear();
		foreach (CraftingStatData statData in list)
		{
			if (!statData.IsValid)
			{
				continue;
			}
			float num = 0f;
			if (IsInOrderMode && flag)
			{
				num = ActiveCraftingOrder.WeaponAttributes.FirstOrDefault((WeaponAttributeVM x) => x.AttributeType == statData.Type && x.DamageType == statData.DamageType)?.AttributeValue ?? 0f;
			}
			float maxValue = TaleWorlds.Library.MathF.Max(statData.MaxValue, num);
			CraftingListPropertyItem craftingListPropertyItem = new CraftingListPropertyItem(statData.DescriptionText, maxValue, statData.CurValue, num, statData.Type);
			PrimaryPropertyList.Add(craftingListPropertyItem);
			craftingListPropertyItem.IsValidForUsage = true;
		}
		PrimaryPropertyList.Sort(new WeaponPropertyComparer());
		MissingPropertyWarningText = CampaignUIHelper.GetCraftingOrderMissingPropertyWarningText(ActiveCraftingOrder?.CraftingOrder, currentCraftedItemObject);
	}

	private IEnumerable<CraftingStatData> GetOrderStatDatas(CraftingOrder order)
	{
		WeaponComponentData weapon;
		return order?.GetStatDataForItem(order.PreCraftedWeaponDesignItem, out weapon);
	}

	private void RefreshWeaponFlags()
	{
		WeaponFlagIconsList.Clear();
		foreach (CraftingPieceListVM pieceList in PieceLists)
		{
			if (pieceList.SelectedPiece == null)
			{
				continue;
			}
			foreach (CraftingItemFlagVM iconData in pieceList.SelectedPiece.ItemAttributeIcons)
			{
				if (!WeaponFlagIconsList.Any((ItemFlagVM x) => x.Icon == iconData.Icon))
				{
					WeaponFlagIconsList.Add(iconData);
				}
			}
		}
	}

	private void OnSetItemPieceManually(CraftingPieceVM piece)
	{
		OnSetItemPiece(piece);
		RefreshItem();
		AddHistoryKey();
	}

	private void OnSetItemPiece(CraftingPieceVM piece, int scalePercentage = 0, bool shouldUpdateWholeWeapon = true, bool forceUpdatePiece = false)
	{
		CraftingPiece.PieceTypes pieceType = (CraftingPiece.PieceTypes)piece.PieceType;
		_pieceListsDictionary[pieceType].SelectedPiece.IsSelected = false;
		bool updatePiece = _updatePiece;
		if (!_isAutoSelectingPieces)
		{
			UnsetPieceNewlyUnlocked(piece);
		}
		if (updatePiece)
		{
			_crafting.SwitchToPiece(piece.CraftingPiece);
			if (!forceUpdatePiece)
			{
				_updatePiece = false;
			}
		}
		piece.IsSelected = true;
		_pieceListsDictionary[pieceType].SelectedPiece = piece;
		int num = ((scalePercentage != 0) ? scalePercentage : _crafting.SelectedPieces[(int)pieceType].ScalePercentage) - 100;
		switch (pieceType)
		{
		case CraftingPiece.PieceTypes.Blade:
			BladeSize = num;
			SelectedBladePiece = piece;
			break;
		case CraftingPiece.PieceTypes.Guard:
			GuardSize = num;
			SelectedGuardPiece = piece;
			break;
		case CraftingPiece.PieceTypes.Handle:
			HandleSize = num;
			SelectedHandlePiece = piece;
			break;
		case CraftingPiece.PieceTypes.Pommel:
			PommelSize = num;
			SelectedPommelPiece = piece;
			break;
		}
		if (IsInFreeMode)
		{
			GetCurrentWeaponClass()?.RegisterSelectedPiece(pieceType, piece.CraftingPiece.CraftingPiece.StringId);
		}
		_updatePiece = updatePiece;
		RefreshAlternativeUsageList();
		if (shouldUpdateWholeWeapon)
		{
			_onRefresh?.Invoke();
		}
		PieceLists.ApplyActionOnAllItems(delegate(CraftingPieceListVM x)
		{
			x.Refresh();
		});
	}

	public void RefreshItem()
	{
		RefreshStats();
		RefreshWeaponFlags();
		RefreshDifficulty();
		_onRefresh?.Invoke();
	}

	private void RefreshDifficulty()
	{
		CurrentDifficulty = Campaign.Current.Models.SmithingModel.CalculateWeaponDesignDifficulty(_crafting.CurrentWeaponDesign);
		if (IsInOrderMode)
		{
			CurrentOrderDifficulty = TaleWorlds.Library.MathF.Round(ActiveCraftingOrder.CraftingOrder.OrderDifficulty);
		}
		_currentCraftingSkillText.SetTextVariable("SKILL_VALUE", CurrentHeroCraftingSkill);
		_currentCraftingSkillText.SetTextVariable("SKILL_NAME", DefaultSkills.Crafting.Name);
		CurrentCraftingSkillValueText = _currentCraftingSkillText.ToString();
		CurrentDifficultyText = GetCurrentDifficultyText(CurrentHeroCraftingSkill, CurrentDifficulty);
		CurrentOrderDifficultyText = GetCurrentOrderDifficultyText(CurrentOrderDifficulty);
	}

	private string GetCurrentDifficultyText(int skillValue, int difficultyValue)
	{
		_difficultyTextobj.SetTextVariable("DIFFICULTY", difficultyValue);
		return _difficultyTextobj.ToString();
	}

	private string GetCurrentOrderDifficultyText(int orderDifficulty)
	{
		_orderDifficultyTextObj.SetTextVariable("DIFFICULTY", orderDifficulty.ToString());
		return _orderDifficultyTextObj.ToString();
	}

	private void RefreshCurrentHeroSkillLevel()
	{
		CurrentHeroCraftingSkill = _getCurrentCraftingHero?.Invoke()?.Hero.CharacterObject.GetSkillValue(DefaultSkills.Crafting) ?? 0;
		IsCurrentHeroAtMaxCraftingSkill = CurrentHeroCraftingSkill >= 300;
		_currentCraftingSkillText.SetTextVariable("SKILL_VALUE", CurrentHeroCraftingSkill);
		CurrentCraftingSkillValueText = _currentCraftingSkillText.ToString();
		CurrentDifficultyText = GetCurrentDifficultyText(CurrentHeroCraftingSkill, CurrentDifficulty);
	}

	public bool HaveUnlockedAllSelectedPieces()
	{
		foreach (CraftingPieceListVM pieceList in PieceLists)
		{
			if (pieceList.IsEnabled && pieceList.SelectedPiece?.CraftingPiece != null && !pieceList.SelectedPiece.PlayerHasPiece)
			{
				return false;
			}
		}
		return true;
	}

	private void AddHistoryKey()
	{
		if (_shouldRecordHistory)
		{
			_crafting.UpdateHistory();
		}
	}

	public void SwitchToPiece(WeaponDesignElement usedPiece)
	{
		CraftingPieceVM piece = _pieceListsDictionary[usedPiece.CraftingPiece.PieceType].Pieces.FirstOrDefault((CraftingPieceVM p) => p.CraftingPiece.CraftingPiece == usedPiece.CraftingPiece);
		OnSetItemPiece(piece, usedPiece.ScalePercentage);
	}

	internal void SetDesignManually(CraftingTemplate craftingTemplate, (CraftingPiece, int)[] pieces, bool forceChangeTemplate = false)
	{
		int num = _primaryUsages.IndexOf(craftingTemplate);
		if (!(IsInFreeMode && forceChangeTemplate) && num != _selectedWeaponClassIndex)
		{
			return;
		}
		RefreshWeaponDesignMode(ActiveCraftingOrder, _primaryUsages.IndexOf(craftingTemplate), doNotAutoSelectPieces: true);
		for (int i = 0; i < pieces.Length; i++)
		{
			(CraftingPiece, int) currentPiece = pieces[i];
			if (currentPiece.Item1 != null)
			{
				CraftingPieceVM craftingPieceVM = _pieceListsDictionary[currentPiece.Item1.PieceType].Pieces.FirstOrDefault((CraftingPieceVM piece) => piece.CraftingPiece.CraftingPiece == currentPiece.Item1);
				if (craftingPieceVM != null)
				{
					OnSetItemPiece(craftingPieceVM, currentPiece.Item2);
					_crafting.ScaleThePiece(currentPiece.Item1.PieceType, currentPiece.Item2);
				}
			}
		}
		RefreshDifficulty();
		_onRefresh?.Invoke();
	}
}
