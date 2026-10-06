using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ContainerPageControlWidget : Widget
{
	private Action<Widget> _nextPageClickedHandler;

	private Action<Widget> _previousPageClickedHandler;

	private Action<Widget, Widget> _onContainerChildAddedHandler;

	private Action<Widget> _onContainerChildRemovedHandler;

	protected int _currentPageIndex;

	private bool _isInitialized;

	private int _itemPerPage;

	private bool _loopNavigation;

	private Container _container;

	private ButtonWidget _nextPageButton;

	private ButtonWidget _previousPageButton;

	private TextWidget _pageText;

	public int PageCount { get; private set; }

	public NavigationScopeTargeter PageButtonsContext { get; set; }

	[Editor(false)]
	public int ItemPerPage
	{
		get
		{
			return _itemPerPage;
		}
		set
		{
			if (_itemPerPage != value)
			{
				_itemPerPage = value;
				OnPropertyChanged(value, "ItemPerPage");
			}
		}
	}

	[Editor(false)]
	public bool LoopNavigation
	{
		get
		{
			return _loopNavigation;
		}
		set
		{
			if (_loopNavigation != value)
			{
				_loopNavigation = value;
				OnPropertyChanged(value, "LoopNavigation");
			}
		}
	}

	[Editor(false)]
	public Container Container
	{
		get
		{
			return _container;
		}
		set
		{
			if (_container != value)
			{
				_container?.ItemAddEventHandlers.Remove(_onContainerChildAddedHandler);
				_container?.ItemAfterRemoveEventHandlers.Remove(_onContainerChildRemovedHandler);
				_container = value;
				_container?.ItemAddEventHandlers.Add(_onContainerChildAddedHandler);
				_container?.ItemAfterRemoveEventHandlers.Add(_onContainerChildRemovedHandler);
				OnPropertyChanged(value, "Container");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget NextPageButton
	{
		get
		{
			return _nextPageButton;
		}
		set
		{
			if (_nextPageButton != value)
			{
				_nextPageButton?.ClickEventHandlers.Remove(_nextPageClickedHandler);
				_nextPageButton = value;
				_nextPageButton?.ClickEventHandlers.Add(_nextPageClickedHandler);
				OnPropertyChanged(value, "NextPageButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget PreviousPageButton
	{
		get
		{
			return _previousPageButton;
		}
		set
		{
			if (_previousPageButton != value)
			{
				_previousPageButton?.ClickEventHandlers.Remove(_previousPageClickedHandler);
				_previousPageButton = value;
				_previousPageButton?.ClickEventHandlers.Add(_previousPageClickedHandler);
				OnPropertyChanged(value, "PreviousPageButton");
			}
		}
	}

	[Editor(false)]
	public TextWidget PageText
	{
		get
		{
			return _pageText;
		}
		set
		{
			if (_pageText != value)
			{
				_pageText = value;
				OnPropertyChanged(value, "PageText");
			}
		}
	}

	public event Action OnPageCountChanged;

	public ContainerPageControlWidget(UIContext context)
		: base(context)
	{
		_nextPageClickedHandler = NextPageClicked;
		_previousPageClickedHandler = PreviousPageClicked;
		_onContainerChildRemovedHandler = OnContainerChildRemoved;
		_onContainerChildAddedHandler = OnContainerChildAdded;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isInitialized)
		{
			int pageCount = PageCount;
			PageCount = TaleWorlds.Library.MathF.Ceiling((float)Container.ChildCount / (float)ItemPerPage);
			if (pageCount != PageCount)
			{
				this.OnPageCountChanged?.Invoke();
			}
			_currentPageIndex = ((PageCount > 1) ? ((int)TaleWorlds.Library.MathF.Clamp(_currentPageIndex, 0f, PageCount - 1)) : 0);
			UpdateControlElements();
			UpdateContainerItems();
			_isInitialized = true;
			OnInitialized();
		}
	}

	private void NextPageClicked(Widget widget)
	{
		int num = _currentPageIndex + 1;
		if (num >= PageCount)
		{
			num = ((!LoopNavigation) ? (PageCount - 1) : 0);
		}
		if (num != _currentPageIndex)
		{
			_currentPageIndex = num;
			UpdateContainerItems();
		}
	}

	private void PreviousPageClicked(Widget widget)
	{
		int num = _currentPageIndex - 1;
		if (num < 0)
		{
			num = (LoopNavigation ? (PageCount - 1) : 0);
		}
		if (num != _currentPageIndex)
		{
			_currentPageIndex = num;
			UpdateContainerItems();
		}
	}

	private void OnContainerChildAdded(Widget parentWidget, Widget addedWidget)
	{
		_isInitialized = false;
	}

	private void OnContainerChildRemoved(Widget widget)
	{
		_isInitialized = false;
	}

	private void UpdateContainerItems()
	{
		int childCount = Container.ChildCount;
		int num = _currentPageIndex * ItemPerPage;
		int num2 = (_currentPageIndex + 1) * ItemPerPage;
		for (int i = 0; i < childCount; i++)
		{
			Container.GetChild(i).IsVisible = i >= num && i < num2;
		}
		UpdatePageText();
		OnContainerItemsUpdated();
	}

	private void UpdateControlElements()
	{
		if (NextPageButton != null)
		{
			NextPageButton.IsVisible = PageCount > 1;
		}
		if (PreviousPageButton != null)
		{
			PreviousPageButton.IsVisible = PageCount > 1;
		}
		if (PageText != null)
		{
			PageText.IsVisible = PageCount > 1;
		}
		if (PageButtonsContext != null)
		{
			PageButtonsContext.IsScopeEnabled = PageCount > 1;
		}
	}

	private void UpdatePageText()
	{
		if (PageText != null)
		{
			PageText.Text = _currentPageIndex + 1 + "/" + PageCount;
		}
	}

	protected virtual void OnInitialized()
	{
	}

	protected virtual void OnContainerItemsUpdated()
	{
	}

	protected void GoToPage(int index)
	{
		if (index >= 0 && index < PageCount && index != _currentPageIndex)
		{
			_currentPageIndex = index;
			UpdateContainerItems();
		}
	}
}
