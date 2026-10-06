using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryTwoWaySliderWidget : TwoWaySliderWidget
{
	private bool _isExtended;

	private Widget _initFiller;

	private bool _isBeingDragged;

	private bool _shouldRemoveZeroCounts;

	private ButtonWidget _increaseStockButtonWidget;

	private ButtonWidget _decreaseStockButtonWidget;

	private bool _isRightSide;

	public bool IsExtended
	{
		get
		{
			return _isExtended;
		}
		set
		{
			if (_isExtended != value)
			{
				CheckFillerState();
				_isExtended = value;
			}
		}
	}

	[Editor(false)]
	public ButtonWidget IncreaseStockButtonWidget
	{
		get
		{
			return _increaseStockButtonWidget;
		}
		set
		{
			if (_increaseStockButtonWidget != value)
			{
				_increaseStockButtonWidget = value;
				OnPropertyChanged(value, "IncreaseStockButtonWidget");
				value.ClickEventHandlers.Add(OnStockChangeClick);
			}
		}
	}

	[Editor(false)]
	public ButtonWidget DecreaseStockButtonWidget
	{
		get
		{
			return _decreaseStockButtonWidget;
		}
		set
		{
			if (_decreaseStockButtonWidget != value)
			{
				_decreaseStockButtonWidget = value;
				OnPropertyChanged(value, "DecreaseStockButtonWidget");
				value.ClickEventHandlers.Add(OnStockChangeClick);
			}
		}
	}

	[Editor(false)]
	public bool IsRightSide
	{
		get
		{
			return _isRightSide;
		}
		set
		{
			if (_isRightSide != value)
			{
				_isRightSide = value;
				OnPropertyChanged(value, "IsRightSide");
			}
		}
	}

	public InventoryTwoWaySliderWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnParallelUpdate(float dt)
	{
		if (_initFiller == null && base.Filler != null)
		{
			_initFiller = base.Filler;
		}
		if (IsExtended)
		{
			base.OnParallelUpdate(dt);
			CheckFillerState();
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_isBeingDragged && !base.IsPressed)
		{
			Widget handle = base.Handle;
			if (handle != null && !handle.IsPressed)
			{
				_shouldRemoveZeroCounts = true;
			}
		}
		_isBeingDragged = base.IsPressed || (base.Handle?.IsPressed ?? false);
		if (_shouldRemoveZeroCounts)
		{
			EventFired("RemoveZeroCounts");
			_shouldRemoveZeroCounts = false;
		}
	}

	private void CheckFillerState()
	{
		if (_initFiller != null)
		{
			if (IsExtended && base.Filler == null)
			{
				base.Filler = _initFiller;
			}
			else if (!IsExtended && base.Filler != null)
			{
				base.Filler = null;
			}
		}
	}

	private void OnStockChangeClick(Widget obj)
	{
		_manuallyIncreased = true;
		_shouldRemoveZeroCounts = true;
	}
}
