using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Encyclopedia;

public class EncyclopediaSearchBarBrushWidget : BrushWidget
{
	private bool _showChat;

	private ScrollablePanel _searchResultPanel;

	private EditableTextWidget _searchInputWidget;

	private int _minCharAmountToShowResults;

	public bool ShowResults
	{
		get
		{
			return _showChat;
		}
		set
		{
			if (value != _showChat)
			{
				_showChat = value;
				OnPropertyChanged(value, "ShowResults");
			}
		}
	}

	public EditableTextWidget SearchInputWidget
	{
		get
		{
			return _searchInputWidget;
		}
		set
		{
			if (value != _searchInputWidget)
			{
				if (_searchInputWidget != null)
				{
					_searchInputWidget.EventFire -= OnSearchInputClick;
				}
				_searchInputWidget = value;
				OnPropertyChanged(value, "SearchInputWidget");
				if (_searchInputWidget != null)
				{
					_searchInputWidget.EventFire += OnSearchInputClick;
				}
			}
		}
	}

	public ScrollablePanel SearchResultPanel
	{
		get
		{
			return _searchResultPanel;
		}
		set
		{
			if (value != _searchResultPanel)
			{
				_searchResultPanel = value;
				OnPropertyChanged(value, "SearchResultPanel");
			}
		}
	}

	public int MinCharAmountToShowResults
	{
		get
		{
			return _minCharAmountToShowResults;
		}
		set
		{
			if (value != _minCharAmountToShowResults)
			{
				_minCharAmountToShowResults = value;
				OnPropertyChanged(value, "MinCharAmountToShowResults");
			}
		}
	}

	public EncyclopediaSearchBarBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		bool flag = base.EventManager.LatestMouseUpWidget == this || CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget) || base.EventManager.LatestMouseDownWidget == this || CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget);
		bool flag2 = SearchResultPanel.CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget) || SearchResultPanel.CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget);
		ShowResults = (flag || flag2) && SearchInputWidget.Text.Length >= MinCharAmountToShowResults;
		SearchResultPanel.IsVisible = ShowResults;
	}

	protected override void OnMousePressed()
	{
		base.OnMousePressed();
		EventFired("SearchBarClick");
	}

	private void OnSearchInputClick(Widget widget, string eventName, object[] arguments)
	{
		if (eventName == "MouseDown")
		{
			EventFired("SearchBarClick");
		}
	}
}
