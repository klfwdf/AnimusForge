using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyTroopTupleButtonWidget : ButtonWidget
{
	private PartyScreenWidget _screenWidget;

	public InventoryTwoWaySliderWidget _transferSlider;

	private bool _isTupleLeftSide;

	private bool _isTransferable;

	private bool _isMainHero;

	private bool _isPrisoner;

	private int _transferAmount;

	private InventoryTupleExtensionControlsWidget _extendedControlsContainer;

	private Widget _main;

	private Widget _upgradesPanel;

	public string CharacterID { get; set; }

	public PartyScreenWidget ScreenWidget
	{
		get
		{
			if (_screenWidget == null)
			{
				AssignScreenWidget();
			}
			return _screenWidget;
		}
	}

	[Editor(false)]
	public bool IsTupleLeftSide
	{
		get
		{
			return _isTupleLeftSide;
		}
		set
		{
			if (_isTupleLeftSide != value)
			{
				_isTupleLeftSide = value;
				OnPropertyChanged(value, "IsTupleLeftSide");
			}
		}
	}

	[Editor(false)]
	public InventoryTwoWaySliderWidget TransferSlider
	{
		get
		{
			return _transferSlider;
		}
		set
		{
			if (_transferSlider != value)
			{
				_transferSlider = value;
				OnPropertyChanged(value, "TransferSlider");
				value.intPropertyChanged += OnValueChanged;
				_transferSlider.AddState("Selected");
				_transferSlider.OverrideDefaultStateSwitchingEnabled = true;
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
			}
		}
	}

	[Editor(false)]
	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (_isMainHero != value)
			{
				base.AcceptDrag = !value;
				_isMainHero = value;
				OnPropertyChanged(value, "IsMainHero");
			}
		}
	}

	[Editor(false)]
	public bool IsPrisoner
	{
		get
		{
			return _isPrisoner;
		}
		set
		{
			if (_isPrisoner != value)
			{
				_isPrisoner = value;
				OnPropertyChanged(value, "IsPrisoner");
			}
		}
	}

	[Editor(false)]
	public int TransferAmount
	{
		get
		{
			return _transferAmount;
		}
		set
		{
			if (_transferAmount != value)
			{
				_transferAmount = value;
				OnPropertyChanged(value, "TransferAmount");
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
	public Widget Main
	{
		get
		{
			return _main;
		}
		set
		{
			if (_main != value)
			{
				_main = value;
				OnPropertyChanged(value, "Main");
			}
		}
	}

	[Editor(false)]
	public Widget UpgradesPanel
	{
		get
		{
			return _upgradesPanel;
		}
		set
		{
			if (_upgradesPanel != value)
			{
				_upgradesPanel = value;
				OnPropertyChanged(value, "UpgradesPanel");
			}
		}
	}

	public PartyTroopTupleButtonWidget(UIContext context)
		: base(context)
	{
		base.OverrideDefaultStateSwitchingEnabled = true;
		AddState("Selected");
	}

	private void SetWidgetsState(string state)
	{
		SetState(state);
		string currentState = _extendedControlsContainer.CurrentState;
		_extendedControlsContainer.SetState(base.IsSelected ? "Selected" : "Default");
		_main.SetState(state);
		if (currentState == "Default" && base.IsSelected)
		{
			EventFired("Opened");
			TransferSlider.IsExtended = true;
			_extendedControlsContainer.IsExtended = true;
		}
		else if (currentState == "Selected" && !base.IsSelected)
		{
			EventFired("Closed");
			TransferSlider.IsExtended = false;
			_extendedControlsContainer.IsExtended = false;
		}
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		_extendedControlsContainer.IsEnabled = base.IsSelected;
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

	private void AssignScreenWidget()
	{
		Widget widget = this;
		while (widget != base.EventManager.Root && _screenWidget == null)
		{
			if (widget is PartyScreenWidget screenWidget)
			{
				_screenWidget = screenWidget;
			}
			else
			{
				widget = widget.ParentWidget;
			}
		}
	}

	private void OnValueChanged(PropertyOwnerObject arg1, string arg2, int arg3)
	{
		if (arg2 == "ValueInt")
		{
			base.AcceptDrag = arg3 > 0;
		}
	}
}
