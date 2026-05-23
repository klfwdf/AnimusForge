using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ToggleStateButtonWidget : ButtonWidget
{
	private Widget _widgetToClose;

	private bool _allowSwitchOff = true;

	private bool _notifyParentForSelection = true;

	[Editor(false)]
	public Widget WidgetToClose
	{
		get
		{
			return _widgetToClose;
		}
		set
		{
			if (_widgetToClose != value)
			{
				_widgetToClose = value;
				OnPropertyChanged(value, "WidgetToClose");
				if (_widgetToClose != null)
				{
					_widgetToClose.IsVisible = base.IsSelected;
				}
			}
		}
	}

	[Editor(false)]
	public bool AllowSwitchOff
	{
		get
		{
			return _allowSwitchOff;
		}
		set
		{
			if (_allowSwitchOff != value)
			{
				_allowSwitchOff = value;
				OnPropertyChanged(value, "AllowSwitchOff");
			}
		}
	}

	[Editor(false)]
	public bool NotifyParentForSelection
	{
		get
		{
			return _notifyParentForSelection;
		}
		set
		{
			if (_notifyParentForSelection != value)
			{
				_notifyParentForSelection = value;
				OnPropertyChanged(value, "NotifyParentForSelection");
			}
		}
	}

	public ToggleStateButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void HandleClick()
	{
		foreach (Action<Widget> clickEventHandler in ClickEventHandlers)
		{
			clickEventHandler(this);
		}
		bool isSelected = base.IsSelected;
		if (!base.IsSelected)
		{
			base.IsSelected = true;
		}
		else if (AllowSwitchOff)
		{
			base.IsSelected = false;
		}
		if (base.IsSelected && !isSelected && NotifyParentForSelection && base.ParentWidget is Container)
		{
			(base.ParentWidget as Container).OnChildSelected(this);
		}
		if (AllowSwitchOff && !base.IsSelected && NotifyParentForSelection && base.ParentWidget is Container)
		{
			(base.ParentWidget as Container).OnChildSelected(null);
		}
		EventFired("Click");
		if (base.Context.EventManager.Time - _lastClickTime < 0.5f)
		{
			EventFired("DoubleClick");
		}
		else
		{
			_lastClickTime = base.Context.EventManager.Time;
		}
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		if (base.UpdateChildrenStates)
		{
			UpdateChildrenStatesRecursively(this);
		}
		if (_widgetToClose != null)
		{
			_widgetToClose.IsVisible = base.IsSelected;
		}
	}

	private void UpdateChildrenStatesRecursively(Widget parent)
	{
		parent.SetState(base.CurrentState);
		if (parent.ChildCount <= 0)
		{
			return;
		}
		foreach (Widget child in parent.Children)
		{
			UpdateChildrenStatesRecursively(child);
		}
	}
}
