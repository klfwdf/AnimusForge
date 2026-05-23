using System;
using System.Collections.Generic;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class ButtonWidget : ImageWidget
{
	private enum ButtonClickState
	{
		None,
		HandlingClick,
		HandlingAlternateClick
	}

	protected const float _maxDoubleClickDeltaTimeInSeconds = 0.5f;

	protected float _lastClickTime;

	private ButtonClickState _clickState;

	private ButtonType _buttonType;

	public List<Action<Widget>> ClickEventHandlers = new List<Action<Widget>>();

	private Widget _toggleIndicator;

	private bool _isSelected;

	private bool _dominantSelectedState = true;

	[Editor(false)]
	public ButtonType ButtonType
	{
		get
		{
			return _buttonType;
		}
		set
		{
			if (_buttonType != value)
			{
				_buttonType = value;
				Refresh();
			}
		}
	}

	public bool IsToggle => ButtonType == ButtonType.Toggle;

	public bool IsRadio => ButtonType == ButtonType.Radio;

	[Editor(false)]
	public Widget ToggleIndicator
	{
		get
		{
			return _toggleIndicator;
		}
		set
		{
			if (_toggleIndicator != value)
			{
				_toggleIndicator = value;
				Refresh();
			}
		}
	}

	[Editor(false)]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				Refresh();
				RefreshState();
				OnPropertyChanged(value, "IsSelected");
			}
		}
	}

	[Editor(false)]
	public bool DominantSelectedState
	{
		get
		{
			return _dominantSelectedState;
		}
		set
		{
			if (_dominantSelectedState != value)
			{
				_dominantSelectedState = value;
				OnPropertyChanged(value, "DominantSelectedState");
			}
		}
	}

	protected override bool OnPreviewMousePressed()
	{
		base.OnPreviewMousePressed();
		return true;
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		if (!base.OverrideDefaultStateSwitchingEnabled)
		{
			if (base.IsDisabled)
			{
				SetState("Disabled");
			}
			else if (IsSelected && DominantSelectedState)
			{
				SetState("Selected");
			}
			else if (base.IsPressed)
			{
				SetState("Pressed");
			}
			else if (base.IsHovered)
			{
				SetState("Hovered");
			}
			else if (IsSelected && !DominantSelectedState)
			{
				SetState("Selected");
			}
			else
			{
				SetState("Default");
			}
		}
		if (!base.UpdateChildrenStates)
		{
			return;
		}
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			if (!(child is ImageWidget) || !((ImageWidget)child).OverrideDefaultStateSwitchingEnabled)
			{
				child.SetState(base.CurrentState);
			}
		}
	}

	private void Refresh()
	{
		if (IsToggle)
		{
			ShowHideToggle();
		}
	}

	private void ShowHideToggle()
	{
		if (ToggleIndicator != null)
		{
			if (_isSelected)
			{
				ToggleIndicator.Show();
			}
			else
			{
				ToggleIndicator.Hide();
			}
		}
	}

	public ButtonWidget(UIContext context)
		: base(context)
	{
		base.FrictionEnabled = true;
	}

	protected internal override void OnMousePressed()
	{
		if (_clickState != ButtonClickState.None)
		{
			return;
		}
		_clickState = ButtonClickState.HandlingClick;
		base.IsPressed = true;
		if (base.DoNotPassEventsToChildren)
		{
			return;
		}
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			if (child != null)
			{
				child.IsPressed = true;
			}
		}
	}

	protected internal override void OnMouseReleased(bool isFromInput)
	{
		if (_clickState != ButtonClickState.HandlingClick)
		{
			return;
		}
		_clickState = ButtonClickState.None;
		base.IsPressed = false;
		if (!base.DoNotPassEventsToChildren)
		{
			for (int i = 0; i < base.ChildCount; i++)
			{
				Widget child = GetChild(i);
				if (child != null)
				{
					child.IsPressed = false;
				}
			}
		}
		if (IsPointInsideMeasuredAreaAndCheckIfVisible() && isFromInput)
		{
			HandleClick();
		}
	}

	private bool IsPointInsideMeasuredAreaAndCheckIfVisible()
	{
		if (IsPointInsideMeasuredArea(base.EventManager.MousePosition) && IsRecursivelyVisible())
		{
			return true;
		}
		return false;
	}

	protected internal override void OnMouseAlternatePressed()
	{
		if (_clickState != ButtonClickState.None)
		{
			return;
		}
		_clickState = ButtonClickState.HandlingAlternateClick;
		base.IsPressed = true;
		if (base.DoNotPassEventsToChildren)
		{
			return;
		}
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			if (child != null)
			{
				child.IsPressed = true;
			}
		}
	}

	protected internal override void OnMouseAlternateReleased(bool isFromInput)
	{
		if (_clickState != ButtonClickState.HandlingAlternateClick)
		{
			return;
		}
		_clickState = ButtonClickState.None;
		base.IsPressed = false;
		if (!base.DoNotPassEventsToChildren)
		{
			for (int i = 0; i < base.ChildCount; i++)
			{
				Widget child = GetChild(i);
				if (child != null)
				{
					child.IsPressed = false;
				}
			}
		}
		if (IsPointInsideMeasuredAreaAndCheckIfVisible() && isFromInput)
		{
			HandleAlternateClick();
		}
	}

	protected virtual void HandleClick()
	{
		foreach (Action<Widget> clickEventHandler in ClickEventHandlers)
		{
			clickEventHandler(this);
		}
		bool isSelected = IsSelected;
		if (IsToggle)
		{
			IsSelected = !IsSelected;
		}
		else if (IsRadio)
		{
			IsSelected = true;
			if (IsSelected && !isSelected && base.ParentWidget is Container)
			{
				(base.ParentWidget as Container).OnChildSelected(this);
			}
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

	protected virtual void HandleAlternateClick()
	{
		EventFired("AlternateClick");
	}
}
