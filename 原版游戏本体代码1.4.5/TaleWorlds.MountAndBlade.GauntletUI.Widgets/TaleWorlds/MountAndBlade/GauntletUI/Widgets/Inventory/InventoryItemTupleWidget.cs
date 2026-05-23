using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryItemTupleWidget : InventoryItemButtonWidget
{
	private bool _isCivilianStateSet;

	private bool _isStealthStateSet;

	private float _extendedUpdateTimer;

	private TextWidget _nameTextWidget;

	private TextWidget _countTextWidget;

	private TextWidget _costTextWidget;

	private int _profitState;

	private BrushListPanel _mainContainer;

	private InventoryTupleExtensionControlsWidget _extendedControlsContainer;

	private InventoryTwoWaySliderWidget _slider;

	private Widget _sliderParent;

	private TextWidget _sliderTextWidget;

	private bool _isTransferable;

	private ButtonWidget _equipButton;

	private int _transactionCount;

	private int _itemCount;

	private bool _isCivilian;

	private bool _isStealth;

	private bool _isGenderDifferent;

	private bool _isEquipable;

	private bool _canCharacterUseItem;

	private bool _isNewlyAdded;

	private Brush _defaultBrush;

	private Brush _cantUseInSetBrush;

	private Brush _characterCantUseBrush;

	private string _itemID;

	public InventoryImageIdentifierWidget ItemImageIdentifier { get; set; }

	[Editor(false)]
	public string ItemID
	{
		get
		{
			return _itemID;
		}
		set
		{
			if (_itemID != value)
			{
				_itemID = value;
				OnPropertyChanged(value, "ItemID");
			}
		}
	}

	[Editor(false)]
	public TextWidget NameTextWidget
	{
		get
		{
			return _nameTextWidget;
		}
		set
		{
			if (_nameTextWidget != value)
			{
				_nameTextWidget = value;
				OnPropertyChanged(value, "NameTextWidget");
				NameTextWidget.AddState("Pressed");
			}
		}
	}

	[Editor(false)]
	public TextWidget CountTextWidget
	{
		get
		{
			return _countTextWidget;
		}
		set
		{
			if (_countTextWidget != value)
			{
				if (_countTextWidget != null)
				{
					_countTextWidget.intPropertyChanged -= CountTextWidgetOnPropertyChanged;
				}
				_countTextWidget = value;
				if (_countTextWidget != null)
				{
					_countTextWidget.intPropertyChanged += CountTextWidgetOnPropertyChanged;
				}
				OnPropertyChanged(value, "CountTextWidget");
				UpdateCountText();
			}
		}
	}

	[Editor(false)]
	public TextWidget CostTextWidget
	{
		get
		{
			return _costTextWidget;
		}
		set
		{
			if (_costTextWidget != value)
			{
				_costTextWidget = value;
				UpdateCostText();
				OnPropertyChanged(value, "CostTextWidget");
			}
		}
	}

	public int ProfitState
	{
		get
		{
			return _profitState;
		}
		set
		{
			if (value != _profitState)
			{
				_profitState = value;
				UpdateCostText();
				OnPropertyChanged(value, "ProfitState");
			}
		}
	}

	[Editor(false)]
	public BrushListPanel MainContainer
	{
		get
		{
			return _mainContainer;
		}
		set
		{
			if (_mainContainer != value)
			{
				_mainContainer = value;
				OnPropertyChanged(value, "MainContainer");
			}
		}
	}

	[Editor(false)]
	public InventoryTupleExtensionControlsWidget ExtendedControlsContainer
	{
		get
		{
			return _extendedControlsContainer;
		}
		set
		{
			if (_extendedControlsContainer != value)
			{
				_extendedControlsContainer = value;
				OnPropertyChanged(value, "ExtendedControlsContainer");
			}
		}
	}

	[Editor(false)]
	public InventoryTwoWaySliderWidget Slider
	{
		get
		{
			return _slider;
		}
		set
		{
			if (_slider != value)
			{
				if (_slider != null)
				{
					_slider.intPropertyChanged -= SliderIntPropertyChanged;
				}
				_slider = value;
				if (_slider != null)
				{
					_slider.intPropertyChanged += SliderIntPropertyChanged;
				}
				OnPropertyChanged(value, "Slider");
				Slider.AddState("Selected");
				Slider.OverrideDefaultStateSwitchingEnabled = true;
			}
		}
	}

	[Editor(false)]
	public Widget SliderParent
	{
		get
		{
			return _sliderParent;
		}
		set
		{
			if (_sliderParent != value)
			{
				_sliderParent = value;
				OnPropertyChanged(value, "SliderParent");
				SliderParent.AddState("Selected");
			}
		}
	}

	[Editor(false)]
	public TextWidget SliderTextWidget
	{
		get
		{
			return _sliderTextWidget;
		}
		set
		{
			if (_sliderTextWidget != value)
			{
				_sliderTextWidget = value;
				OnPropertyChanged(value, "SliderTextWidget");
				SliderTextWidget.AddState("Selected");
			}
		}
	}

	[Editor(false)]
	public bool IsTransferable
	{
		get
		{
			return _isTransferable;
		}
		set
		{
			if (_isTransferable != value)
			{
				_isTransferable = value;
				OnPropertyChanged(value, "IsTransferable");
				UpdateDragAvailability();
			}
		}
	}

	[Editor(false)]
	public ButtonWidget EquipButton
	{
		get
		{
			return _equipButton;
		}
		set
		{
			if (_equipButton != value)
			{
				_equipButton = value;
				OnPropertyChanged(value, "EquipButton");
			}
		}
	}

	[Editor(false)]
	public int TransactionCount
	{
		get
		{
			return _transactionCount;
		}
		set
		{
			if (_transactionCount != value)
			{
				_transactionCount = value;
				OnPropertyChanged(value, "TransactionCount");
			}
		}
	}

	[Editor(false)]
	public int ItemCount
	{
		get
		{
			return _itemCount;
		}
		set
		{
			if (_itemCount != value)
			{
				_itemCount = value;
				OnPropertyChanged(value, "ItemCount");
				UpdateDragAvailability();
			}
		}
	}

	[Editor(false)]
	public bool IsCivilian
	{
		get
		{
			return _isCivilian;
		}
		set
		{
			if (_isCivilian != value || !_isCivilianStateSet)
			{
				_isCivilian = value;
				OnPropertyChanged(value, "IsCivilian");
				_isCivilianStateSet = true;
				UpdateEquipmentTypeState();
			}
		}
	}

	[Editor(false)]
	public bool IsStealth
	{
		get
		{
			return _isStealth;
		}
		set
		{
			if (_isStealth != value || !_isStealthStateSet)
			{
				_isStealth = value;
				OnPropertyChanged(value, "IsStealth");
				_isStealthStateSet = true;
				UpdateEquipmentTypeState();
			}
		}
	}

	[Editor(false)]
	public bool IsGenderDifferent
	{
		get
		{
			return _isGenderDifferent;
		}
		set
		{
			if (_isGenderDifferent != value)
			{
				_isGenderDifferent = value;
				OnPropertyChanged(value, "IsGenderDifferent");
				UpdateEquipmentTypeState();
			}
		}
	}

	[Editor(false)]
	public bool IsEquipable
	{
		get
		{
			return _isEquipable;
		}
		set
		{
			if (_isEquipable != value)
			{
				_isEquipable = value;
				OnPropertyChanged(value, "IsEquipable");
				UpdateDragAvailability();
			}
		}
	}

	[Editor(false)]
	public bool IsNewlyAdded
	{
		get
		{
			return _isNewlyAdded;
		}
		set
		{
			if (_isNewlyAdded != value)
			{
				_isNewlyAdded = value;
				OnPropertyChanged(value, "IsNewlyAdded");
				ItemImageIdentifier.SetRenderRequestedPreviousFrame(value);
			}
		}
	}

	[Editor(false)]
	public bool CanCharacterUseItem
	{
		get
		{
			return _canCharacterUseItem;
		}
		set
		{
			if (_canCharacterUseItem != value)
			{
				_canCharacterUseItem = value;
				OnPropertyChanged(value, "CanCharacterUseItem");
				UpdateEquipmentTypeState();
			}
		}
	}

	[Editor(false)]
	public Brush DefaultBrush
	{
		get
		{
			return _defaultBrush;
		}
		set
		{
			if (_defaultBrush != value)
			{
				_defaultBrush = value;
				OnPropertyChanged(value, "DefaultBrush");
			}
		}
	}

	[Editor(false)]
	public Brush CantUseInSetBrush
	{
		get
		{
			return _cantUseInSetBrush;
		}
		set
		{
			if (_cantUseInSetBrush != value)
			{
				_cantUseInSetBrush = value;
				OnPropertyChanged(value, "CantUseInSetBrush");
			}
		}
	}

	[Editor(false)]
	public Brush CharacterCantUseBrush
	{
		get
		{
			return _characterCantUseBrush;
		}
		set
		{
			if (_characterCantUseBrush != value)
			{
				_characterCantUseBrush = value;
				OnPropertyChanged(value, "CharacterCantUseBrush");
			}
		}
	}

	public InventoryItemTupleWidget(UIContext context)
		: base(context)
	{
		base.OverrideDefaultStateSwitchingEnabled = false;
		AddState("Selected");
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		base.ScreenWidget.intPropertyChanged += InventoryScreenWidgetOnPropertyChanged;
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		base.ScreenWidget.intPropertyChanged -= InventoryScreenWidgetOnPropertyChanged;
	}

	private void SetWidgetsState(string state)
	{
		SetState(state);
		string currentState = ExtendedControlsContainer.CurrentState;
		ExtendedControlsContainer.SetState(base.IsSelected ? "Selected" : "Default");
		MainContainer.SetState(state);
		NameTextWidget.SetState((state == "Pressed") ? state : "Default");
		if (currentState == "Default" && base.IsSelected)
		{
			EventFired("Opened");
			Slider.IsExtended = true;
		}
		else if (currentState == "Selected" && !base.IsSelected)
		{
			EventFired("Closed");
			Slider.IsExtended = false;
		}
	}

	private void OnExtendedHiddenUpdate(float dt)
	{
		if (!base.IsSelected)
		{
			_extendedUpdateTimer += dt;
			if (_extendedUpdateTimer > 2f)
			{
				ExtendedControlsContainer.IsVisible = false;
			}
			else
			{
				base.EventManager.AddLateUpdateAction(this, OnExtendedHiddenUpdate, 1);
			}
		}
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		_ = ExtendedControlsContainer.IsVisible;
		ExtendedControlsContainer.IsExtended = base.IsSelected;
		if (base.IsSelected)
		{
			ExtendedControlsContainer.IsVisible = true;
		}
		else if (ExtendedControlsContainer.IsVisible)
		{
			_extendedUpdateTimer = 0f;
			base.EventManager.AddLateUpdateAction(this, OnExtendedHiddenUpdate, 1);
		}
		if (base.IsDisabled)
		{
			SetWidgetsState("Disabled");
		}
		else if (base.IsPressed)
		{
			SetWidgetsState("Pressed");
		}
		else if (base.IsHovered)
		{
			SetWidgetsState("Hovered");
		}
		else if (base.IsSelected)
		{
			SetWidgetsState("Selected");
		}
		else
		{
			SetWidgetsState("Default");
		}
	}

	private void UpdateEquipmentTypeState()
	{
		if (base.ScreenWidget == null)
		{
			return;
		}
		bool flag = base.ScreenWidget.EquipmentMode == 0 && !IsCivilian && IsEquipable;
		bool flag2 = base.ScreenWidget.EquipmentMode == 2 && !IsStealth && IsEquipable;
		if (IsEquipable && !CanCharacterUseItem)
		{
			if (!MainContainer.Brush.IsCloneRelated(CharacterCantUseBrush))
			{
				MainContainer.Brush = CharacterCantUseBrush;
				EquipButton.IsVisible = true;
				EquipButton.IsEnabled = false;
			}
		}
		else if (flag || flag2)
		{
			if (!MainContainer.Brush.IsCloneRelated(CantUseInSetBrush))
			{
				MainContainer.Brush = CantUseInSetBrush;
				EquipButton.IsVisible = true;
				EquipButton.IsEnabled = false;
			}
		}
		else if (!MainContainer.Brush.IsCloneRelated(DefaultBrush))
		{
			MainContainer.Brush = DefaultBrush;
			EquipButton.IsVisible = IsEquipable;
			EquipButton.IsEnabled = IsEquipable;
		}
	}

	private void SliderIntPropertyChanged(PropertyOwnerObject owner, string propertyName, int value)
	{
		if (propertyName == "ValueInt")
		{
			TransactionCount = _slider.ValueInt;
		}
	}

	private void CountTextWidgetOnPropertyChanged(PropertyOwnerObject owner, string propertyName, int value)
	{
		if (propertyName == "IntText")
		{
			UpdateCountText();
		}
	}

	private void InventoryScreenWidgetOnPropertyChanged(PropertyOwnerObject owner, string propertyName, int value)
	{
		if (propertyName == "EquipmentMode")
		{
			UpdateEquipmentTypeState();
		}
	}

	private void UpdateCountText()
	{
		if (SliderTextWidget != null)
		{
			SliderTextWidget.IsHidden = CountTextWidget.IsHidden;
		}
	}

	private void UpdateCostText()
	{
		if (CostTextWidget != null)
		{
			switch (ProfitState)
			{
			case -2:
				CostTextWidget.SetState("VeryBad");
				break;
			case -1:
				CostTextWidget.SetState("Bad");
				break;
			case 0:
				CostTextWidget.SetState("Default");
				break;
			case 1:
				CostTextWidget.SetState("Good");
				break;
			case 2:
				CostTextWidget.SetState("VeryGood");
				break;
			}
		}
	}

	private void UpdateDragAvailability()
	{
		base.AcceptDrag = ItemCount > 0 && (IsTransferable || IsEquipable);
	}
}
