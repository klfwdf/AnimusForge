using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class NavigatableListPanel : ListPanel
{
	private bool _areIndicesDirty;

	private int _minIndex;

	private int _maxIndex = int.MaxValue;

	private int _stepSize = 1;

	private bool _useSelfIndexForMinimum;

	private Widget _emptyNavigationWidget;

	public ScrollablePanel ParentPanel { get; set; }

	public int AutoScrollTopOffset { get; set; }

	public int AutoScrollBottomOffset { get; set; }

	public int AutoScrollLeftOffset { get; set; }

	public int AutoScrollRightOffset { get; set; }

	public int MinIndex
	{
		get
		{
			return _minIndex;
		}
		set
		{
			if (value != _minIndex)
			{
				_minIndex = value;
				RefreshChildNavigationIndices();
			}
		}
	}

	public int MaxIndex
	{
		get
		{
			return _maxIndex;
		}
		set
		{
			if (value != _maxIndex)
			{
				_maxIndex = value;
				RefreshChildNavigationIndices();
			}
		}
	}

	public int StepSize
	{
		get
		{
			return _stepSize;
		}
		set
		{
			if (value != _stepSize)
			{
				_stepSize = value;
				RefreshChildNavigationIndices();
			}
		}
	}

	public bool UseSelfIndexForMinimum
	{
		get
		{
			return _useSelfIndexForMinimum;
		}
		set
		{
			if (value != _useSelfIndexForMinimum)
			{
				_useSelfIndexForMinimum = value;
				if (_useSelfIndexForMinimum && base.GamepadNavigationIndex != -1)
				{
					SetNavigationIndicesFromSelf();
				}
			}
		}
	}

	public Widget EmptyNavigationWidget
	{
		get
		{
			return _emptyNavigationWidget;
		}
		set
		{
			if (value != _emptyNavigationWidget)
			{
				if (_emptyNavigationWidget != null)
				{
					_emptyNavigationWidget.GamepadNavigationIndex = -1;
				}
				_emptyNavigationWidget = value;
				UpdateEmptyNavigationWidget();
			}
		}
	}

	public NavigatableListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		if (_areIndicesDirty)
		{
			RefreshChildNavigationIndices();
			_areIndicesDirty = false;
		}
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		if (ParentPanel == null)
		{
			ParentPanel = FindParentPanel();
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.OnGamepadNavigationFocusGained = OnWidgetGainedGamepadFocus;
		child.EventFire += OnChildSiblingIndexChanged;
		child.boolPropertyChanged += OnChildVisibilityChanged;
		_areIndicesDirty = true;
		UpdateEmptyNavigationWidget();
	}

	protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		base.OnAfterChildRemoved(child, previousIndexOfChild);
		child.OnGamepadNavigationFocusGained = null;
		child.EventFire -= OnChildSiblingIndexChanged;
		child.boolPropertyChanged -= OnChildVisibilityChanged;
		child.GamepadNavigationIndex = -1;
		UpdateEmptyNavigationWidget();
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		for (int i = 0; i < base.Children.Count; i++)
		{
			base.Children[i].OnGamepadNavigationFocusGained = null;
			base.Children[i].EventFire -= OnChildSiblingIndexChanged;
			base.Children[i].boolPropertyChanged -= OnChildVisibilityChanged;
			base.Children[i].GamepadNavigationIndex = -1;
		}
	}

	private void OnChildVisibilityChanged(PropertyOwnerObject child, string propertyName, bool value)
	{
		if (propertyName == "IsVisible")
		{
			Widget widget = (Widget)child;
			if (!value)
			{
				widget.GamepadNavigationIndex = -1;
			}
			else
			{
				SetNavigationIndexForChild(widget);
			}
		}
	}

	private void OnWidgetGainedGamepadFocus(Widget widget)
	{
		if (ParentPanel != null)
		{
			ScrollablePanel.AutoScrollParameters scrollParameters = new ScrollablePanel.AutoScrollParameters(AutoScrollTopOffset, AutoScrollBottomOffset, AutoScrollLeftOffset, AutoScrollRightOffset);
			ParentPanel.ScrollToChild(widget, scrollParameters);
		}
	}

	private void OnChildSiblingIndexChanged(Widget widget, string eventName, object[] parameters)
	{
		if (eventName == "SiblingIndexChanged")
		{
			_areIndicesDirty = true;
		}
	}

	private void SetNavigationIndexForChild(Widget widget)
	{
		int num = ((base.StackLayout.LayoutMethod != LayoutMethod.VerticalBottomToTop && base.StackLayout.LayoutMethod != LayoutMethod.HorizontalRightToLeft) ? (MinIndex + widget.GetSiblingIndex() * StepSize) : (MaxIndex - widget.GetSiblingIndex() * StepSize));
		if (num <= MaxIndex)
		{
			widget.GamepadNavigationIndex = num;
		}
	}

	protected override void OnGamepadNavigationIndexUpdated(int newIndex)
	{
		if (newIndex != -1 && UseSelfIndexForMinimum)
		{
			SetNavigationIndicesFromSelf();
		}
	}

	private void SetNavigationIndicesFromSelf()
	{
		MinIndex = base.GamepadNavigationIndex;
		base.GamepadNavigationIndex = -1;
		_areIndicesDirty = true;
	}

	protected void RefreshChildNavigationIndices()
	{
		for (int i = 0; i < base.Children.Count; i++)
		{
			SetNavigationIndexForChild(base.Children[i]);
		}
	}

	private void UpdateEmptyNavigationWidget()
	{
		if (_emptyNavigationWidget != null)
		{
			if (base.Children.Count == 0)
			{
				EmptyNavigationWidget.GamepadNavigationIndex = MinIndex;
			}
			else
			{
				EmptyNavigationWidget.GamepadNavigationIndex = -1;
			}
		}
	}
}
