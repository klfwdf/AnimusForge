using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.BannerEditor;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

public class BannerBuilderVM : ViewModel
{
	private const int PatternLayerIndex = 0;

	public int ShieldSlotIndex = 3;

	public int CurrentShieldIndex;

	public ItemRosterElement ShieldRosterElement;

	private readonly BasicCharacterObject _character;

	private readonly Action<bool> _onExit;

	private readonly Action _refresh;

	private readonly Action _copyBannerCode;

	private BannerImageIdentifierVM _bannerImageIdentifier;

	private string _iconCodes;

	private string _colorCodes;

	private string _bannerCodeAsString;

	private BannerViewModel _bannerVM;

	private MBBindingList<BannerBuilderCategoryVM> _categories;

	private MBBindingList<BannerBuilderLayerVM> _layers;

	private BannerBuilderLayerVM _currentSelectedLayer;

	private BannerBuilderItemVM _currentSelectedItem;

	private BannerBuilderColorSelectionVM _colorSelection;

	private string _title;

	private string _cancelText;

	private string _doneText;

	private string _currentShieldName;

	private bool _canChangeBackgroundColor;

	private bool _isBannerPreviewsActive;

	private bool _isEditorPreviewActive;

	private bool _isLayerPreviewActive;

	private int _minIconSize;

	private int _maxIconSize;

	private HintViewModel _resetHint;

	private HintViewModel _randomizeHint;

	private HintViewModel _undoHint;

	private HintViewModel _redoHint;

	private HintViewModel _drawStrokeHint;

	private HintViewModel _centerHint;

	private HintViewModel _resetSizeHint;

	private HintViewModel _mirrorHint;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	public Banner CurrentBanner { get; private set; }

	[DataSourceProperty]
	public BannerImageIdentifierVM BannerImageIdentifier
	{
		get
		{
			return _bannerImageIdentifier;
		}
		set
		{
			if (value != _bannerImageIdentifier)
			{
				_bannerImageIdentifier = value;
				OnPropertyChangedWithValue(value, "BannerImageIdentifier");
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
	public MBBindingList<BannerBuilderCategoryVM> Categories
	{
		get
		{
			return _categories;
		}
		set
		{
			if (value != _categories)
			{
				_categories = value;
				OnPropertyChangedWithValue(value, "Categories");
			}
		}
	}

	[DataSourceProperty]
	public BannerBuilderColorSelectionVM ColorSelection
	{
		get
		{
			return _colorSelection;
		}
		set
		{
			if (value != _colorSelection)
			{
				_colorSelection = value;
				OnPropertyChangedWithValue(value, "ColorSelection");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BannerBuilderLayerVM> Layers
	{
		get
		{
			return _layers;
		}
		set
		{
			if (value != _layers)
			{
				_layers = value;
				OnPropertyChangedWithValue(value, "Layers");
			}
		}
	}

	[DataSourceProperty]
	public BannerBuilderLayerVM CurrentSelectedLayer
	{
		get
		{
			return _currentSelectedLayer;
		}
		set
		{
			if (value != _currentSelectedLayer)
			{
				_currentSelectedLayer = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedLayer");
			}
		}
	}

	[DataSourceProperty]
	public BannerBuilderItemVM CurrentSelectedItem
	{
		get
		{
			return _currentSelectedItem;
		}
		set
		{
			if (value != _currentSelectedItem)
			{
				_currentSelectedItem = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedItem");
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
	public HintViewModel DrawStrokeHint
	{
		get
		{
			return _drawStrokeHint;
		}
		set
		{
			if (value != _drawStrokeHint)
			{
				_drawStrokeHint = value;
				OnPropertyChangedWithValue(value, "DrawStrokeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CenterHint
	{
		get
		{
			return _centerHint;
		}
		set
		{
			if (value != _centerHint)
			{
				_centerHint = value;
				OnPropertyChangedWithValue(value, "CenterHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetSizeHint
	{
		get
		{
			return _resetSizeHint;
		}
		set
		{
			if (value != _resetSizeHint)
			{
				_resetSizeHint = value;
				OnPropertyChangedWithValue(value, "ResetSizeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MirrorHint
	{
		get
		{
			return _mirrorHint;
		}
		set
		{
			if (value != _mirrorHint)
			{
				_mirrorHint = value;
				OnPropertyChangedWithValue(value, "MirrorHint");
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
	public string BannerCodeAsString
	{
		get
		{
			return _bannerCodeAsString;
		}
		set
		{
			if (value != _bannerCodeAsString)
			{
				_bannerCodeAsString = value;
				OnPropertyChangedWithValue(value, "BannerCodeAsString");
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
	public bool IsBannerPreviewsActive
	{
		get
		{
			return _isBannerPreviewsActive;
		}
		set
		{
			if (value != _isBannerPreviewsActive)
			{
				_isBannerPreviewsActive = value;
				OnPropertyChangedWithValue(value, "IsBannerPreviewsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEditorPreviewActive
	{
		get
		{
			return _isEditorPreviewActive;
		}
		set
		{
			if (value != _isEditorPreviewActive)
			{
				_isEditorPreviewActive = value;
				OnPropertyChangedWithValue(value, "IsEditorPreviewActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLayerPreviewActive
	{
		get
		{
			return _isLayerPreviewActive;
		}
		set
		{
			if (value != _isLayerPreviewActive)
			{
				_isLayerPreviewActive = value;
				OnPropertyChangedWithValue(value, "IsLayerPreviewActive");
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

	public BannerBuilderVM(BasicCharacterObject character, string initialKey, Action<bool> onExit, Action refresh, Action copyBannerCode)
	{
		_character = character;
		_onExit = onExit;
		_refresh = refresh;
		_copyBannerCode = copyBannerCode;
		Categories = new MBBindingList<BannerBuilderCategoryVM>();
		Layers = new MBBindingList<BannerBuilderLayerVM>();
		ColorSelection = new BannerBuilderColorSelectionVM();
		BannerBuilderLayerVM.SetLayerActions(OnRefreshFromLayer, OnLayerSelection, OnLayerDeletion, OnColorSelection);
		ItemObject itemObject = FindShield(_character, "highland_riders_shield");
		if (itemObject != null)
		{
			ShieldRosterElement = new ItemRosterElement(itemObject, 1);
		}
		CurrentBanner = new Banner(initialKey);
		PopulateCategories();
		PopulateLayers();
		OnLayerSelection(Layers[0]);
		BannerCodeAsString = initialKey;
		BannerImageIdentifier = new BannerImageIdentifierVM(CurrentBanner, nineGrid: true);
		RefreshValues();
		IsEditorPreviewActive = true;
		IsLayerPreviewActive = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = new TextObject("{=!}Banner Builder").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		ResetHint = new HintViewModel(GameTexts.FindText("str_reset_icon"));
		RandomizeHint = new HintViewModel(GameTexts.FindText("str_randomize"));
		UndoHint = new HintViewModel(GameTexts.FindText("str_undo"));
		RedoHint = new HintViewModel(GameTexts.FindText("str_redo"));
		DrawStrokeHint = new HintViewModel(new TextObject("{=!}Draw Stroke"));
		CenterHint = new HintViewModel(new TextObject("{=!}Align Center"));
		ResetSizeHint = new HintViewModel(new TextObject("{=!}Reset Size"));
		MirrorHint = new HintViewModel(new TextObject("{=!}Mirror"));
	}

	private void PopulateCategories()
	{
		Categories.Clear();
		for (int i = 0; i < BannerManager.Instance.BannerIconGroups.Count; i++)
		{
			BannerIconGroup category = BannerManager.Instance.BannerIconGroups[i];
			Categories.Add(new BannerBuilderCategoryVM(category, OnItemSelection));
		}
	}

	private void PopulateLayers()
	{
		Layers.Clear();
		for (int i = 0; i < CurrentBanner.GetBannerDataListCount(); i++)
		{
			Layers.Add(new BannerBuilderLayerVM(CurrentBanner.GetBannerDataAtIndex(i), i));
		}
	}

	private void OnColorSelection(int selectedColorId, Action<BannerBuilderColorItemVM> onSelection)
	{
		ColorSelection.EnableWith(selectedColorId, onSelection);
	}

	private void OnLayerSelection(BannerBuilderLayerVM selectedLayer)
	{
		if (CurrentSelectedLayer != null)
		{
			CurrentSelectedLayer.IsSelected = false;
		}
		if (CurrentSelectedItem != null)
		{
			CurrentSelectedItem.IsSelected = false;
		}
		CurrentSelectedLayer = selectedLayer;
		if (CurrentSelectedLayer != null)
		{
			BannerBuilderItemVM itemFromID = GetItemFromID(CurrentSelectedLayer.IconID);
			if (itemFromID != null)
			{
				CurrentSelectedItem = itemFromID;
				CurrentSelectedItem.IsSelected = true;
			}
			CurrentSelectedLayer.IsSelected = true;
			bool isPatternLayerSelected = CurrentSelectedLayer.LayerIndex == 0;
			Categories.ApplyActionOnAllItems(delegate(BannerBuilderCategoryVM c)
			{
				c.IsEnabled = (c.IsPattern ? isPatternLayerSelected : (!isPatternLayerSelected));
			});
			UpdateSelectedItem();
		}
	}

	private void OnLayerDeletion(BannerBuilderLayerVM layerToDelete)
	{
		if (layerToDelete == null || layerToDelete.LayerIndex != 0)
		{
			CurrentBanner.RemoveIconDataAtIndex(layerToDelete.LayerIndex);
			if (CurrentSelectedLayer == layerToDelete)
			{
				OnLayerSelection(Layers[layerToDelete.LayerIndex - 1]);
			}
			Layers.RemoveAt(layerToDelete.LayerIndex);
			RefreshLayerIndicies();
			Refresh();
		}
	}

	private void OnItemSelection(BannerBuilderItemVM selectedItem)
	{
		if (CurrentSelectedLayer != null)
		{
			CurrentBanner.GetBannerDataAtIndex(CurrentSelectedLayer.LayerIndex).MeshId = selectedItem.MeshID;
			UpdateSelectedItem();
			CurrentSelectedLayer.Refresh();
			Refresh();
		}
	}

	private void UpdateSelectedItem()
	{
		if (CurrentSelectedLayer == null)
		{
			return;
		}
		int meshId = CurrentBanner.GetBannerDataAtIndex(CurrentSelectedLayer.LayerIndex).MeshId;
		for (int i = 0; i < Categories.Count; i++)
		{
			BannerBuilderCategoryVM bannerBuilderCategoryVM = Categories[i];
			for (int j = 0; j < bannerBuilderCategoryVM.ItemsList.Count; j++)
			{
				BannerBuilderItemVM bannerBuilderItemVM = bannerBuilderCategoryVM.ItemsList[j];
				bannerBuilderItemVM.IsSelected = bannerBuilderItemVM.MeshID == meshId;
			}
		}
	}

	public void ExecuteCancel()
	{
		_onExit?.Invoke(obj: true);
	}

	public void ExecuteDone()
	{
		_onExit?.Invoke(obj: false);
	}

	public void ExecuteAddDefaultLayer()
	{
		BannerData defaultBannerData = GetDefaultBannerData();
		CurrentBanner.AddIconData(defaultBannerData);
		BannerBuilderLayerVM bannerBuilderLayerVM = new BannerBuilderLayerVM(defaultBannerData, Layers.Count);
		Layers.Add(bannerBuilderLayerVM);
		OnLayerSelection(bannerBuilderLayerVM);
		Refresh();
	}

	public void ExecuteDuplicateCurrentLayer()
	{
		BannerBuilderLayerVM currentSelectedLayer = CurrentSelectedLayer;
		if (currentSelectedLayer != null && !currentSelectedLayer.IsLayerPattern)
		{
			BannerData bannerData = new BannerData(CurrentSelectedLayer.Data);
			CurrentBanner.AddIconData(bannerData);
			BannerBuilderLayerVM bannerBuilderLayerVM = new BannerBuilderLayerVM(bannerData, Layers.Count);
			Layers.Add(bannerBuilderLayerVM);
			OnLayerSelection(bannerBuilderLayerVM);
			Refresh();
		}
	}

	public void ExecuteCopyBannerCode()
	{
		_copyBannerCode?.Invoke();
	}

	public void ExecuteReorderWithParameters(BannerBuilderLayerVM layer, int index, string targetTag)
	{
		if (!layer.IsLayerPattern && index != 0)
		{
			int index2 = ((layer.LayerIndex >= index) ? index : (index - 1));
			Layers.Remove(layer);
			Layers.Insert(index2, layer);
			CurrentBanner.RemoveIconDataAtIndex(layer.LayerIndex);
			CurrentBanner.AddIconData(layer.Data, index2);
			RefreshLayerIndicies();
			OnRefreshFromLayer();
		}
	}

	public void ExecuteReorderToEndWithParameters(BannerBuilderLayerVM layer, int index, string targetTag)
	{
		if (!layer.IsLayerPattern)
		{
			ExecuteReorderWithParameters(layer, Layers.Count, string.Empty);
		}
	}

	private void OnRefreshFromLayer()
	{
		Refresh();
	}

	public string GetBannerCode()
	{
		return BannerCodeAsString;
	}

	public void SetBannerCode(string v)
	{
		string bannerCodeAsString = BannerCodeAsString;
		try
		{
			CurrentBanner.Deserialize(v);
			PopulateLayers();
			OnLayerSelection(Layers[0]);
			Refresh();
		}
		catch (Exception)
		{
			InformationManager.DisplayMessage(new InformationMessage("Couldn't parse the clipboard text."));
			CurrentBanner.Deserialize(bannerCodeAsString);
			PopulateLayers();
			OnLayerSelection(Layers[0]);
			Refresh();
		}
	}

	private void Refresh()
	{
		_refresh?.Invoke();
		BannerImageIdentifier = new BannerImageIdentifierVM(CurrentBanner, nineGrid: true);
	}

	private BannerBuilderItemVM GetItemFromID(int id)
	{
		for (int i = 0; i < Categories.Count; i++)
		{
			BannerBuilderCategoryVM bannerBuilderCategoryVM = Categories[i];
			for (int j = 0; j < bannerBuilderCategoryVM.ItemsList.Count; j++)
			{
				BannerBuilderItemVM bannerBuilderItemVM = bannerBuilderCategoryVM.ItemsList[j];
				if (bannerBuilderItemVM.MeshID == id)
				{
					return bannerBuilderItemVM;
				}
			}
		}
		return null;
	}

	private void RefreshLayerIndicies()
	{
		for (int i = 0; i < Layers.Count; i++)
		{
			Layers[i].SetLayerIndex(i);
		}
	}

	public void TranslateCurrentLayerWith(Vec2 moveDirection)
	{
		CurrentSelectedLayer.PositionValueX += moveDirection.x;
		CurrentSelectedLayer.PositionValueY += moveDirection.y;
		CurrentSelectedLayer.PositionValueX = TaleWorlds.Library.MathF.Clamp(CurrentSelectedLayer.PositionValueX, 0f, 1528f);
		CurrentSelectedLayer.PositionValueY = TaleWorlds.Library.MathF.Clamp(CurrentSelectedLayer.PositionValueY, 0f, 1528f);
		Refresh();
	}

	public void DeleteCurrentLayer()
	{
		BannerBuilderLayerVM currentSelectedLayer = CurrentSelectedLayer;
		if (currentSelectedLayer != null && !currentSelectedLayer.IsLayerPattern)
		{
			OnLayerDeletion(Layers[CurrentSelectedLayer.LayerIndex]);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		BannerBuilderLayerVM.ResetLayerActions();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	private static ItemObject FindShield(BasicCharacterObject character, string desiredShieldID = "")
	{
		for (int i = 0; i < 4; i++)
		{
			EquipmentElement equipmentFromSlot = character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item?.PrimaryWeapon != null && equipmentFromSlot.Item.PrimaryWeapon.IsShield && equipmentFromSlot.Item.IsUsingTableau)
			{
				return equipmentFromSlot.Item;
			}
		}
		if (!string.IsNullOrEmpty(desiredShieldID))
		{
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(desiredShieldID);
			if (itemObject != null)
			{
				WeaponComponentData primaryWeapon = itemObject.PrimaryWeapon;
				if (primaryWeapon != null && primaryWeapon.IsShield)
				{
					return itemObject;
				}
			}
		}
		MBReadOnlyList<ItemObject> objectTypeList = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>();
		for (int j = 0; j < objectTypeList.Count; j++)
		{
			ItemObject itemObject2 = objectTypeList[j];
			WeaponComponentData primaryWeapon2 = itemObject2.PrimaryWeapon;
			if (primaryWeapon2 != null && primaryWeapon2.IsShield && itemObject2.IsUsingTableau)
			{
				return itemObject2;
			}
		}
		return null;
	}

	private static BannerData GetDefaultBannerData()
	{
		return new BannerData(133, 171, 171, new Vec2(483f, 483f), new Vec2(764f, 764f), drawStroke: false, mirror: false, 0f);
	}
}
