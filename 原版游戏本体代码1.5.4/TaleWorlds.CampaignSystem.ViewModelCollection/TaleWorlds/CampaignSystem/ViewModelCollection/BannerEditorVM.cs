using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.BannerEditor;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class BannerEditorVM : ViewModel
{
	private readonly string _initialBanner;

	public int ShieldSlotIndex = 3;

	public int CurrentShieldIndex;

	public ItemRosterElement ShieldRosterElement;

	private readonly Action<bool> OnExit;

	private readonly Action _refresh;

	private readonly ItemObject _shield;

	private readonly Banner _banner;

	private BannerIconVM _currentSelectedIcon;

	private BannerColorVM _currentSelectedPrimaryColor;

	private BannerColorVM _currentSelectedSigilColor;

	private readonly Action<int> _goToIndex;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private MBBindingList<InputKeyItemVM> _cameraControlKeys;

	private string _iconCodes;

	private string _colorCodes;

	private BannerViewModel _bannerVM;

	private MBBindingList<BannerIconVM> _iconsList;

	private MBBindingList<BannerColorVM> _primaryColorList;

	private MBBindingList<BannerColorVM> _sigilColorList;

	private string _cancelText;

	private string _doneText;

	private string _sizeText;

	private string _primaryColorText;

	private string _sigilColorText;

	private string _currentShieldName;

	private bool _canChangeBackgroundColor;

	private int _currentIconSize;

	private int _minIconSize;

	private int _maxIconSize;

	private HintViewModel _resetHint;

	private HintViewModel _randomizeHint;

	private HintViewModel _undoHint;

	private HintViewModel _redoHint;

	private MBBindingList<HintViewModel> _categoryNames;

	private bool _characterGamepadControlsEnabled;

	private string _title = "";

	private string _description = "";

	private int _totalStageCount = -1;

	private int _currentStageIndex = -1;

	private int _furthestIndex = -1;

	private bool _initialized;

	public BasicCharacterObject Character { get; }

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
	public MBBindingList<InputKeyItemVM> CameraControlKeys
	{
		get
		{
			return _cameraControlKeys;
		}
		set
		{
			if (value != _cameraControlKeys)
			{
				_cameraControlKeys = value;
				OnPropertyChangedWithValue(value, "CameraControlKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HintViewModel> CategoryNames
	{
		get
		{
			return _categoryNames;
		}
		set
		{
			if (value != _categoryNames)
			{
				_categoryNames = value;
				OnPropertyChangedWithValue(value, "CategoryNames");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BannerIconVM> IconsList
	{
		get
		{
			return _iconsList;
		}
		set
		{
			if (value != _iconsList)
			{
				_iconsList = value;
				OnPropertyChangedWithValue(value, "IconsList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BannerColorVM> PrimaryColorList
	{
		get
		{
			return _primaryColorList;
		}
		set
		{
			if (value != _primaryColorList)
			{
				_primaryColorList = value;
				OnPropertyChangedWithValue(value, "PrimaryColorList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BannerColorVM> SigilColorList
	{
		get
		{
			return _sigilColorList;
		}
		set
		{
			if (value != _sigilColorList)
			{
				_sigilColorList = value;
				OnPropertyChangedWithValue(value, "SigilColorList");
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
	public string CurrentShieldName
	{
		get
		{
			return _currentShieldName;
		}
		set
		{
			if (value != _currentShieldName)
			{
				_currentShieldName = value;
				OnPropertyChangedWithValue(value, "CurrentShieldName");
			}
		}
	}

	[DataSourceProperty]
	public int MinIconSize
	{
		get
		{
			return _minIconSize;
		}
		set
		{
			if (value != _minIconSize)
			{
				_minIconSize = value;
				OnPropertyChangedWithValue(value, "MinIconSize");
			}
		}
	}

	[DataSourceProperty]
	public int MaxIconSize
	{
		get
		{
			return _maxIconSize;
		}
		set
		{
			if (value != _maxIconSize)
			{
				_maxIconSize = value;
				OnPropertyChangedWithValue(value, "MaxIconSize");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentIconSize
	{
		get
		{
			return _currentIconSize;
		}
		set
		{
			if (value != _currentIconSize)
			{
				_currentIconSize = value;
				OnPropertyChangedWithValue(value, "CurrentIconSize");
				if (_initialized)
				{
					OnBannerIconSizeChange(value);
				}
			}
		}
	}

	[DataSourceProperty]
	public string PrimaryColorText
	{
		get
		{
			return _primaryColorText;
		}
		set
		{
			if (value != _primaryColorText)
			{
				_primaryColorText = value;
				OnPropertyChangedWithValue(value, "PrimaryColorText");
			}
		}
	}

	[DataSourceProperty]
	public string SizeText
	{
		get
		{
			return _sizeText;
		}
		set
		{
			if (value != _sizeText)
			{
				_sizeText = value;
				OnPropertyChangedWithValue(value, "SizeText");
			}
		}
	}

	[DataSourceProperty]
	public string SigilColorText
	{
		get
		{
			return _sigilColorText;
		}
		set
		{
			if (value != _sigilColorText)
			{
				_sigilColorText = value;
				OnPropertyChangedWithValue(value, "SigilColorText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

	[DataSourceProperty]
	public BannerViewModel BannerVM
	{
		get
		{
			return _bannerVM;
		}
		set
		{
			if (value != _bannerVM)
			{
				_bannerVM = value;
				OnPropertyChangedWithValue(value, "BannerVM");
			}
		}
	}

	[DataSourceProperty]
	public string IconCodes
	{
		get
		{
			return _iconCodes;
		}
		set
		{
			if (value != _iconCodes)
			{
				_iconCodes = value;
				OnPropertyChangedWithValue(value, "IconCodes");
			}
		}
	}

	[DataSourceProperty]
	public string ColorCodes
	{
		get
		{
			return _colorCodes;
		}
		set
		{
			if (value != _colorCodes)
			{
				_colorCodes = value;
				OnPropertyChangedWithValue(value, "ColorCodes");
			}
		}
	}

	[DataSourceProperty]
	public bool CanChangeBackgroundColor
	{
		get
		{
			return _canChangeBackgroundColor;
		}
		set
		{
			if (value != _canChangeBackgroundColor)
			{
				_canChangeBackgroundColor = value;
				OnPropertyChangedWithValue(value, "CanChangeBackgroundColor");
			}
		}
	}

	[DataSourceProperty]
	public bool CharacterGamepadControlsEnabled
	{
		get
		{
			return _characterGamepadControlsEnabled;
		}
		set
		{
			if (value != _characterGamepadControlsEnabled)
			{
				_characterGamepadControlsEnabled = value;
				OnPropertyChangedWithValue(value, "CharacterGamepadControlsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public int TotalStageCount
	{
		get
		{
			return _totalStageCount;
		}
		set
		{
			if (value != _totalStageCount)
			{
				_totalStageCount = value;
				OnPropertyChangedWithValue(value, "TotalStageCount");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentStageIndex
	{
		get
		{
			return _currentStageIndex;
		}
		set
		{
			if (value != _currentStageIndex)
			{
				_currentStageIndex = value;
				OnPropertyChangedWithValue(value, "CurrentStageIndex");
			}
		}
	}

	[DataSourceProperty]
	public int FurthestIndex
	{
		get
		{
			return _furthestIndex;
		}
		set
		{
			if (value != _furthestIndex)
			{
				_furthestIndex = value;
				OnPropertyChangedWithValue(value, "FurthestIndex");
			}
		}
	}

	public BannerEditorVM(BasicCharacterObject character, Banner banner, Action<bool> onExit, Action refresh, int currentStageIndex, int totalStagesCount, int furthestIndex, Action<int> goToIndex)
	{
		Character = character;
		_initialBanner = banner.Serialize();
		_banner = banner;
		IconsList = new MBBindingList<BannerIconVM>();
		PrimaryColorList = new MBBindingList<BannerColorVM>();
		SigilColorList = new MBBindingList<BannerColorVM>();
		_refresh = refresh;
		OnExit = onExit;
		BannerVM = new BannerViewModel(banner);
		CanChangeBackgroundColor = true;
		_shield = FindShield();
		if (_shield != null)
		{
			ShieldRosterElement = new ItemRosterElement(_shield, 1);
		}
		else
		{
			Debug.FailedAssert("Banner Editor couldn't find a shield to show", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\BannerEditorVM.cs", ".ctor", 55);
		}
		_goToIndex = goToIndex;
		TotalStageCount = totalStagesCount;
		CurrentStageIndex = currentStageIndex;
		FurthestIndex = furthestIndex;
		CameraControlKeys = new MBBindingList<InputKeyItemVM>();
		MinIconSize = 100;
		MaxIconSize = 700;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = GameTexts.FindText("str_banner_editor").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		ResetHint = new HintViewModel(GameTexts.FindText("str_reset_icon"));
		RandomizeHint = new HintViewModel(GameTexts.FindText("str_randomize"));
		UndoHint = new HintViewModel(GameTexts.FindText("str_undo"));
		RedoHint = new HintViewModel(GameTexts.FindText("str_redo"));
		PrimaryColorText = new TextObject("{=xwRWjlar}Background Color:").ToString();
		SigilColorText = new TextObject("{=7tBOCHm6}Sigil Color:").ToString();
		SizeText = new TextObject("{=OkWLI5C8}Size:").ToString();
		CategoryNames = new MBBindingList<HintViewModel>();
		foreach (BannerIconGroup bannerIconGroup in BannerManager.Instance.BannerIconGroups)
		{
			if (bannerIconGroup.IsPattern)
			{
				continue;
			}
			foreach (KeyValuePair<int, BannerIconData> availableIcon in bannerIconGroup.AvailableIcons)
			{
				BannerIconVM bannerIconVM = new BannerIconVM(availableIcon.Key, OnIconSelection);
				IconsList.Add(bannerIconVM);
				bannerIconVM.IsSelected = bannerIconVM.IconID == _banner.GetIconMeshId();
			}
			CategoryNames.Add(new HintViewModel(bannerIconGroup.Name, "banner_group_hint_" + bannerIconGroup.Id));
		}
		bool flag = IsColorsSwitched();
		foreach (KeyValuePair<int, BannerColor> item in BannerManager.Instance.ReadOnlyColorPalette)
		{
			bool flag2 = (flag ? item.Value.PlayerCanChooseForSigil : item.Value.PlayerCanChooseForBackground);
			bool num = (flag ? item.Value.PlayerCanChooseForBackground : item.Value.PlayerCanChooseForSigil);
			if (flag2)
			{
				BannerColorVM bannerColorVM = new BannerColorVM(item.Key, item.Value.Color, OnPrimaryColorSelection);
				if (bannerColorVM.ColorID == _banner.GetPrimaryColorId())
				{
					bannerColorVM.IsSelected = true;
					_currentSelectedPrimaryColor = bannerColorVM;
				}
				PrimaryColorList.Add(bannerColorVM);
			}
			if (num)
			{
				BannerColorVM bannerColorVM2 = new BannerColorVM(item.Key, item.Value.Color, OnSigilColorSelection);
				if (bannerColorVM2.ColorID == _banner.GetIconColorId())
				{
					bannerColorVM2.IsSelected = true;
					_currentSelectedSigilColor = bannerColorVM2;
				}
				SigilColorList.Add(bannerColorVM2);
			}
		}
		CurrentIconSize = (int)_banner.GetIconSize().X;
		_initialized = true;
	}

	public void RefreshSelectedColorsAndSigils()
	{
		int iconMeshId = BannerVM.Banner.GetIconMeshId();
		int primaryColorId = _banner.GetPrimaryColorId();
		int iconColorId = _banner.GetIconColorId();
		for (int i = 0; i < IconsList.Count; i++)
		{
			BannerIconVM bannerIconVM = IconsList[i];
			bannerIconVM.IsSelected = bannerIconVM.IconID == iconMeshId;
		}
		if (!DoesListHaveColor(PrimaryColorList, primaryColorId) || !DoesListHaveColor(SigilColorList, iconColorId))
		{
			ExecuteSwitchColors();
		}
		if (!DoesListHaveColor(PrimaryColorList, primaryColorId) || !DoesListHaveColor(SigilColorList, iconColorId))
		{
			Debug.FailedAssert("Color lists do not contain banner colors", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\BannerEditorVM.cs", "RefreshSelectedColorsAndSigils", 160);
			return;
		}
		for (int j = 0; j < PrimaryColorList.Count; j++)
		{
			BannerColorVM bannerColorVM = PrimaryColorList[j];
			bannerColorVM.IsSelected = bannerColorVM.ColorID == primaryColorId;
		}
		for (int k = 0; k < SigilColorList.Count; k++)
		{
			BannerColorVM bannerColorVM2 = SigilColorList[k];
			bannerColorVM2.IsSelected = bannerColorVM2.ColorID == iconColorId;
		}
	}

	private static bool DoesListHaveColor(MBBindingList<BannerColorVM> list, int color)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].ColorID == color)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsColorsSwitched()
	{
		foreach (KeyValuePair<int, BannerColor> item in BannerManager.Instance.ReadOnlyColorPalette)
		{
			if (item.Value.PlayerCanChooseForBackground && item.Key == _banner.GetPrimaryColorId())
			{
				return false;
			}
		}
		return true;
	}

	public void SetClanRelatedRules(bool canChangeBackgroundColor)
	{
		CanChangeBackgroundColor = canChangeBackgroundColor;
	}

	private void OnIconSelection(BannerIconVM icon)
	{
		if (icon != _currentSelectedIcon)
		{
			if (_currentSelectedIcon != null)
			{
				_currentSelectedIcon.IsSelected = false;
			}
			_currentSelectedIcon = icon;
			icon.IsSelected = true;
			BannerVM.Banner.SetIconMeshId(icon.IconID);
			_refresh();
		}
	}

	public void ExecuteSwitchColors()
	{
		if (_currentSelectedPrimaryColor == null || _currentSelectedSigilColor == null)
		{
			Debug.FailedAssert("Couldn't find current player clan colors in the list of selectable banner editor colors.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\BannerEditorVM.cs", "ExecuteSwitchColors", 230);
			return;
		}
		MBBindingList<BannerColorVM> primaryColorList = PrimaryColorList;
		PrimaryColorList = SigilColorList;
		SigilColorList = primaryColorList;
		PrimaryColorList.ApplyActionOnAllItems(delegate(BannerColorVM x)
		{
			x.SetOnSelectionAction(OnPrimaryColorSelection);
		});
		SigilColorList.ApplyActionOnAllItems(delegate(BannerColorVM x)
		{
			x.SetOnSelectionAction(OnSigilColorSelection);
		});
		BannerColorVM currentSelectedPrimaryColor = _currentSelectedPrimaryColor;
		_currentSelectedPrimaryColor = _currentSelectedSigilColor;
		_currentSelectedSigilColor = currentSelectedPrimaryColor;
		_currentSelectedPrimaryColor.IsSelected = true;
		_currentSelectedSigilColor.IsSelected = true;
		BannerVM.Banner.SetPrimaryColorId(_currentSelectedPrimaryColor.ColorID);
		BannerVM.Banner.SetSecondaryColorId(_currentSelectedPrimaryColor.ColorID);
		BannerVM.Banner.SetIconColorId(_currentSelectedSigilColor.ColorID);
		_refresh();
	}

	private void OnPrimaryColorSelection(BannerColorVM color)
	{
		if (color != _currentSelectedPrimaryColor)
		{
			if (_currentSelectedPrimaryColor != null)
			{
				_currentSelectedPrimaryColor.IsSelected = false;
			}
			_currentSelectedPrimaryColor = color;
			color.IsSelected = true;
			BannerVM.Banner.SetPrimaryColorId(color.ColorID);
			BannerVM.Banner.SetSecondaryColorId(color.ColorID);
			_refresh();
		}
	}

	private void OnSigilColorSelection(BannerColorVM color)
	{
		if (color != _currentSelectedSigilColor)
		{
			if (_currentSelectedSigilColor != null)
			{
				_currentSelectedSigilColor.IsSelected = false;
			}
			_currentSelectedSigilColor = color;
			color.IsSelected = true;
			BannerVM.Banner.SetIconColorId(color.ColorID);
			_refresh();
		}
	}

	private void OnBannerIconSizeChange(int newSize)
	{
		BannerVM.Banner.SetIconSize(newSize);
		_refresh();
	}

	public void ExecuteDone()
	{
		OnExit(obj: false);
	}

	public void ExecuteCancel()
	{
		_banner.Deserialize(_initialBanner);
		OnExit(obj: true);
	}

	private ItemObject FindShield()
	{
		for (int i = 0; i < 4; i++)
		{
			EquipmentElement equipmentFromSlot = Character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item?.PrimaryWeapon != null && equipmentFromSlot.Item.PrimaryWeapon.IsShield && equipmentFromSlot.Item.IsUsingTableau)
			{
				return equipmentFromSlot.Item;
			}
		}
		foreach (ItemObject objectType in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
		{
			if (objectType.PrimaryWeapon != null && objectType.PrimaryWeapon.IsShield && objectType.IsUsingTableau)
			{
				return objectType;
			}
		}
		return null;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
		for (int i = 0; i < CameraControlKeys.Count; i++)
		{
			CameraControlKeys[i].OnFinalize();
		}
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void AddCameraControlInputKey(HotKey hotKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameKey gameKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameAxisKey gameAxisKey, TextObject keyName)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), keyName, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void ExecuteGoToIndex(int index)
	{
		_goToIndex(index);
	}
}
