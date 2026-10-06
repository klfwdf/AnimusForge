using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class TabControl : Widget
{
	private Widget _activeTab;

	private int _selectedIndex;

	[Editor(false)]
	public Widget ActiveTab
	{
		get
		{
			return _activeTab;
		}
		private set
		{
			if (_activeTab != value)
			{
				_activeTab = value;
				this.OnActiveTabChange?.Invoke();
			}
		}
	}

	[DataSourceProperty]
	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			if (_selectedIndex != value)
			{
				_selectedIndex = value;
				SetActiveTab(_selectedIndex);
				OnPropertyChanged(value, "SelectedIndex");
			}
		}
	}

	public event OnActiveTabChangeEvent OnActiveTabChange;

	public TabControl(UIContext context)
		: base(context)
	{
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		base.OnBeforeChildRemoved(child);
		if (child == ActiveTab)
		{
			ActiveTab = null;
		}
	}

	private void SetActiveTab(int index)
	{
		Widget child = GetChild(index);
		SetActiveTab(child);
	}

	public void SetActiveTab(string tabName)
	{
		Widget activeTab = FindChild(tabName);
		SetActiveTab(activeTab);
	}

	private void SetActiveTab(Widget newTab)
	{
		if (ActiveTab != newTab && newTab != null)
		{
			if (ActiveTab != null)
			{
				ActiveTab.IsVisible = false;
			}
			ActiveTab = newTab;
			ActiveTab.IsVisible = true;
			SelectedIndex = GetChildIndex(ActiveTab);
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (ActiveTab != null && ActiveTab.ParentWidget == null)
		{
			ActiveTab = null;
		}
		if (ActiveTab == null || ActiveTab.IsDisabled)
		{
			for (int i = 0; i < base.ChildCount; i++)
			{
				Widget child = GetChild(i);
				if (child.IsEnabled && !string.IsNullOrEmpty(child.Id))
				{
					ActiveTab = child;
					break;
				}
			}
		}
		for (int j = 0; j < base.ChildCount; j++)
		{
			Widget child2 = GetChild(j);
			if (ActiveTab != child2 && (child2.IsEnabled || child2.IsVisible))
			{
				child2.IsVisible = false;
			}
			if (ActiveTab == child2)
			{
				child2.IsVisible = true;
			}
		}
	}
}
