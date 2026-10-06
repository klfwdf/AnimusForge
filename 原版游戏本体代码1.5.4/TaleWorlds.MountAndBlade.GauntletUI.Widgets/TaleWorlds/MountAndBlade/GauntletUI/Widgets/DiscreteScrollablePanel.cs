using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class DiscreteScrollablePanel : ScrollablePanel
{
	private int _pageIndex;

	private int _maxPages;

	private int _selectedIndex;

	private bool _isRecursivelyVisible;

	private bool _isLooping;

	private bool _scrollToSelectedOnVisibilityChanged;

	private int _itemsPerPage;

	private float _scrollTime;

	private ListPanel _listWidget;

	private ButtonWidget _previousButtonWidget;

	private ButtonWidget _nextButtonWidget;

	[Editor(false)]
	public bool IsLooping
	{
		get
		{
			return _isLooping;
		}
		set
		{
			if (value != _isLooping)
			{
				_isLooping = value;
				OnPropertyChanged(value, "IsLooping");
				UpdateAll();
			}
		}
	}

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
				UpdateAll();
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
				OnPropertyChanged(value, "ListWidget");
				if (_listWidget != null)
				{
					_listWidget.EventFire += ListWidget_EventFire;
				}
				UpdateAll();
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

	public DiscreteScrollablePanel(UIContext context)
		: base(context)
	{
		ItemsPerPage = 6;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
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
	}

	private void UpdateAll()
	{
		UpdatePageInfo();
		UpdateButtonEnabledStates();
		UpdateViewport();
	}

	private void UpdatePageInfo()
	{
		if (ListWidget == null || base.ClipRect == null)
		{
			_pageIndex = 0;
			_maxPages = 0;
		}
		else
		{
			_maxPages = MathF.Max(0, ListWidget.ChildCount - ItemsPerPage) + 1;
			_pageIndex = (int)MathF.Clamp(_pageIndex, 0f, _maxPages - 1);
		}
	}

	private void UpdateButtonEnabledStates()
	{
		if (_maxPages == 1)
		{
			if (PreviousButtonWidget != null)
			{
				PreviousButtonWidget.IsEnabled = false;
			}
			if (NextButtonWidget != null)
			{
				NextButtonWidget.IsEnabled = false;
			}
		}
		else if (IsLooping)
		{
			if (PreviousButtonWidget != null)
			{
				PreviousButtonWidget.IsEnabled = true;
			}
			if (NextButtonWidget != null)
			{
				NextButtonWidget.IsEnabled = true;
			}
		}
		else
		{
			if (PreviousButtonWidget != null)
			{
				PreviousButtonWidget.IsEnabled = _pageIndex > 0;
			}
			if (NextButtonWidget != null)
			{
				NextButtonWidget.IsEnabled = _pageIndex < _maxPages - 1;
			}
		}
	}

	private void UpdateViewport()
	{
		if (base.ClipRect == null || ListWidget == null || ListWidget.ChildCount == 0)
		{
			return;
		}
		Vec2 zero = Vec2.Zero;
		for (int i = 0; i < ListWidget.ChildCount; i++)
		{
			Widget child = ListWidget.GetChild(i);
			zero += child.Size + new Vec2(child.ScaledMarginLeft + child.ScaledMarginRight, child.ScaledMarginBottom + child.ScaledMarginTop);
		}
		zero /= (float)ListWidget.ChildCount;
		if (ListWidget.GetChild(_pageIndex) != null)
		{
			if (base.HorizontalScrollbar != null)
			{
				_horizontalScrollbarInterpolationController?.StartInterpolation(zero.X * (float)_pageIndex, ScrollTime);
			}
			if (base.VerticalScrollbar != null)
			{
				_verticalScrollbarInterpolationController?.StartInterpolation(zero.Y * (float)_pageIndex, ScrollTime);
			}
		}
	}

	private void ListWidget_EventFire(Widget widget, string eventName, object[] eventArgs)
	{
		switch (eventName)
		{
		case "ItemAdd":
		case "ItemRemove":
		case "AfterItemRemove":
			UpdateAll();
			break;
		}
	}

	private void OnPreviousButtonPressed(Widget widget)
	{
		_pageIndex--;
		if (IsLooping && _pageIndex < 0)
		{
			_pageIndex = ListWidget.ChildCount - 1;
		}
		UpdateAll();
	}

	private void OnNextButtonPressed(Widget widget)
	{
		_pageIndex++;
		if (IsLooping && _pageIndex >= _maxPages)
		{
			_pageIndex = 0;
		}
		UpdateAll();
	}

	private void ScrollToSelectedElement()
	{
		_pageIndex = MathF.Min(_pageIndex, _selectedIndex - 1);
		_pageIndex = MathF.Max(_pageIndex, _selectedIndex - ItemsPerPage + 2);
		UpdateAll();
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
