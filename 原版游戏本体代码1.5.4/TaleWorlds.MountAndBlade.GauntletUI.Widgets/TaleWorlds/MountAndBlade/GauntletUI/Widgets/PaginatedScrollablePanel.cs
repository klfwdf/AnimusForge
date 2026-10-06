using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class PaginatedScrollablePanel : ScrollablePanel
{
	public enum ContainerDirections
	{
		Horizontal,
		Vertical
	}

	private int _pageIndex;

	private int _maxPages;

	private int _selectedIndex;

	private bool _isRecursivelyVisible;

	private bool _isInterpolating;

	private bool _scrollToSelectedOnVisibilityChanged;

	private int _itemsPerPage;

	private float _scrollTime;

	private ContainerDirections _containerDirection;

	private ListPanel _listWidget;

	private ButtonWidget _previousButtonWidget;

	private ButtonWidget _nextButtonWidget;

	private NavigationScopeTargeter _navigationScope;

	[Editor(false)]
	public bool ScrollToSelectedOnVisibilityChanged
	{
		get
		{
			return _scrollToSelectedOnVisibilityChanged;
		}
		set
		{
			if (value != _scrollToSelectedOnVisibilityChanged)
			{
				_scrollToSelectedOnVisibilityChanged = value;
				OnPropertyChanged(value, "ScrollToSelectedOnVisibilityChanged");
			}
		}
	}

	[Editor(false)]
	public int ItemsPerPage
	{
		get
		{
			return _itemsPerPage;
		}
		set
		{
			if (value != _itemsPerPage)
			{
				_itemsPerPage = value;
				OnPropertyChanged(value, "ItemsPerPage");
			}
		}
	}

	[Editor(false)]
	public float ScrollTime
	{
		get
		{
			return _scrollTime;
		}
		set
		{
			if (value != _scrollTime)
			{
				_scrollTime = value;
				OnPropertyChanged(value, "ScrollTime");
			}
		}
	}

	[Editor(false)]
	public ContainerDirections ContainerDirection
	{
		get
		{
			return _containerDirection;
		}
		set
		{
			if (value != _containerDirection)
			{
				_containerDirection = value;
				OnPropertyChanged((int)value, "ContainerDirection");
				UpdatePageInfo();
				UpdateChildrenNavigationStates();
				UpdateViewport();
			}
		}
	}

	[Editor(false)]
	public ListPanel ListWidget
	{
		get
		{
			return _listWidget;
		}
		set
		{
			if (value != _listWidget)
			{
				if (_listWidget != null)
				{
					_listWidget.EventFire -= ListWidget_EventFire;
				}
				_listWidget = value;
				if (_listWidget != null)
				{
					_listWidget.EventFire += ListWidget_EventFire;
				}
				UpdatePageInfo();
				UpdateChildrenNavigationStates();
				UpdateViewport();
				OnPropertyChanged(value, "ListWidget");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget PreviousButtonWidget
	{
		get
		{
			return _previousButtonWidget;
		}
		set
		{
			if (value != _previousButtonWidget)
			{
				_previousButtonWidget?.ClickEventHandlers.Remove(OnPreviousButtonPressed);
				_previousButtonWidget = value;
				_previousButtonWidget?.ClickEventHandlers.Add(OnPreviousButtonPressed);
				OnPropertyChanged(value, "PreviousButtonWidget");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget NextButtonWidget
	{
		get
		{
			return _nextButtonWidget;
		}
		set
		{
			if (value != _nextButtonWidget)
			{
				_nextButtonWidget?.ClickEventHandlers.Remove(OnNextButtonPressed);
				_nextButtonWidget = value;
				_nextButtonWidget?.ClickEventHandlers.Add(OnNextButtonPressed);
				OnPropertyChanged(value, "NextButtonWidget");
			}
		}
	}

	[Editor(false)]
	public NavigationScopeTargeter NavigationScope
	{
		get
		{
			return _navigationScope;
		}
		set
		{
			if (value != _navigationScope)
			{
				_navigationScope = value;
				OnPropertyChanged(value, "NavigationScope");
			}
		}
	}

	public PaginatedScrollablePanel(UIContext context)
		: base(context)
	{
		ItemsPerPage = 4;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		int num = -1;
		for (int i = 0; i < ListWidget.ChildCount; i++)
		{
			if (ListWidget.GetChild(i)?.GetFirstInChildrenAndThisRecursive((Widget x) => x is ButtonWidget) is ButtonWidget { IsSelected: not false })
			{
				num = i;
				break;
			}
		}
		if (_selectedIndex != num)
		{
			_selectedIndex = num;
			OnSelectedWidgetUpdated();
		}
		bool flag = IsRecursivelyVisible();
		if (_isRecursivelyVisible != flag)
		{
			_isRecursivelyVisible = flag;
			OnVisibilityUpdated();
		}
		ScrollbarInterpolationController horizontalScrollbarInterpolationController = _horizontalScrollbarInterpolationController;
		bool flag2 = (horizontalScrollbarInterpolationController != null && horizontalScrollbarInterpolationController.IsInterpolating) || (_verticalScrollbarInterpolationController?.IsInterpolating ?? false);
		if (_isInterpolating != flag2)
		{
			_isInterpolating = flag2;
			if (NavigationScope != null)
			{
				NavigationScope.DoNotAutoNavigateAfterSort = _isInterpolating;
			}
			UpdateChildrenNavigationStates();
		}
	}

	private void ListWidget_EventFire(Widget widget, string eventName, object[] eventArgs)
	{
		switch (eventName)
		{
		case "ItemAdd":
		case "ItemRemove":
		case "AfterItemRemove":
			UpdatePageInfo();
			UpdateChildrenNavigationStates();
			break;
		}
	}

	private void OnPreviousButtonPressed(Widget widget)
	{
		UpdatePageInfo();
		_pageIndex = Mathf.Clamp(_pageIndex - 1, 0, _maxPages - 1);
		UpdateButtonEnabledStates();
		UpdateChildrenNavigationStates();
		UpdateViewport();
	}

	private void OnNextButtonPressed(Widget widget)
	{
		UpdatePageInfo();
		_pageIndex = Mathf.Clamp(_pageIndex + 1, 0, _maxPages - 1);
		UpdateButtonEnabledStates();
		UpdateChildrenNavigationStates();
		UpdateViewport();
	}

	private void UpdatePageInfo()
	{
		if (ListWidget == null || base.ClipRect == null)
		{
			_pageIndex = 0;
			_maxPages = 0;
			return;
		}
		int childCount = ListWidget.ChildCount;
		_maxPages = (int)Mathf.Ceil((float)childCount / (float)ItemsPerPage);
		_pageIndex = Mathf.Clamp(_pageIndex, 0, _maxPages - 1);
		UpdateButtonEnabledStates();
	}

	private void UpdateButtonEnabledStates()
	{
		if (_maxPages == 1)
		{
			if (PreviousButtonWidget != null)
			{
				PreviousButtonWidget.IsDisabled = true;
				PreviousButtonWidget.DoNotAcceptNavigation = PreviousButtonWidget.IsDisabled;
			}
			if (NextButtonWidget != null)
			{
				NextButtonWidget.IsDisabled = true;
				NextButtonWidget.DoNotAcceptNavigation = NextButtonWidget.IsDisabled;
			}
		}
		else
		{
			if (PreviousButtonWidget != null)
			{
				PreviousButtonWidget.IsDisabled = _pageIndex == 0;
				PreviousButtonWidget.DoNotAcceptNavigation = PreviousButtonWidget.IsDisabled;
			}
			if (NextButtonWidget != null)
			{
				NextButtonWidget.IsDisabled = _pageIndex == _maxPages - 1;
				NextButtonWidget.DoNotAcceptNavigation = NextButtonWidget.IsDisabled;
			}
		}
	}

	private void UpdateViewport()
	{
		if (base.ClipRect != null && ListWidget != null && ListWidget.ChildCount != 0)
		{
			Vector2 zero = Vector2.Zero;
			for (int i = 0; i < ListWidget.ChildCount; i++)
			{
				Widget child = ListWidget.GetChild(i);
				zero += child.Size + new Vector2(child.ScaledMarginLeft + child.ScaledMarginRight, child.ScaledMarginBottom + child.ScaledMarginTop);
			}
			zero /= (float)ListWidget.ChildCount;
			if (ContainerDirection == ContainerDirections.Horizontal && base.HorizontalScrollbar != null)
			{
				float value = _horizontalScrollbarInterpolationController.GetValue();
				float num = zero.X * (float)ItemsPerPage;
				float duration = Mathf.Abs(value - zero.X * (float)ItemsPerPage * (float)_pageIndex) / num * ScrollTime;
				_horizontalScrollbarInterpolationController.StartInterpolation(zero.X * (float)ItemsPerPage * (float)_pageIndex, duration);
			}
			else if (ContainerDirection == ContainerDirections.Vertical && base.VerticalScrollbar != null)
			{
				float value2 = _horizontalScrollbarInterpolationController.GetValue();
				float num2 = zero.Y * (float)ItemsPerPage;
				float duration2 = Mathf.Abs(value2 - zero.Y * (float)ItemsPerPage * (float)_pageIndex) / num2 * ScrollTime;
				_verticalScrollbarInterpolationController.StartInterpolation(zero.Y * (float)ItemsPerPage * (float)_pageIndex, duration2);
			}
		}
	}

	private void UpdateChildrenNavigationStates()
	{
		if (base.ClipRect == null || ListWidget == null || ListWidget.ChildCount == 0)
		{
			return;
		}
		if (_isInterpolating)
		{
			for (int i = 0; i < ListWidget.ChildCount; i++)
			{
				ListWidget.GetChild(i).DoNotAcceptNavigation = true;
			}
			return;
		}
		if (_pageIndex == _maxPages - 1)
		{
			for (int j = 0; j < ListWidget.ChildCount; j++)
			{
				Widget child = ListWidget.GetChild(j);
				if (j >= ListWidget.ChildCount - ItemsPerPage)
				{
					child.GamepadNavigationIndex = j - ListWidget.ChildCount + ItemsPerPage + 1;
					child.DoNotAcceptNavigation = false;
				}
				else
				{
					child.DoNotAcceptNavigation = true;
				}
			}
			return;
		}
		for (int k = 0; k < ListWidget.ChildCount; k++)
		{
			Widget child2 = ListWidget.GetChild(k);
			if (k >= _pageIndex * ItemsPerPage && k < (_pageIndex + 1) * ItemsPerPage)
			{
				child2.GamepadNavigationIndex = k - _pageIndex * ItemsPerPage + 1;
				child2.DoNotAcceptNavigation = false;
			}
			else
			{
				child2.DoNotAcceptNavigation = true;
			}
		}
	}

	private void ScrollToSelectedElement()
	{
		UpdatePageInfo();
		if (_pageIndex != _maxPages - 1 || _selectedIndex < ListWidget.ChildCount - ItemsPerPage)
		{
			_pageIndex = Mathf.Clamp(_selectedIndex / ItemsPerPage, 0, _maxPages - 1);
		}
		UpdateButtonEnabledStates();
		UpdateChildrenNavigationStates();
		UpdateViewport();
	}

	private void OnSelectedWidgetUpdated()
	{
		ScrollToSelectedElement();
	}

	private void OnVisibilityUpdated()
	{
		if (_isRecursivelyVisible && ScrollToSelectedOnVisibilityChanged)
		{
			ScrollToSelectedElement();
		}
	}
}
