namespace TaleWorlds.GauntletUI.BaseTypes;

public class TabToggleWidget : ButtonWidget
{
	private string _tabName;

	public TabControl TabControlWidget { get; set; }

	[Editor(false)]
	public string TabName
	{
		get
		{
			return _tabName;
		}
		set
		{
			if (_tabName != value)
			{
				_tabName = value;
				OnPropertyChanged(value, "TabName");
			}
		}
	}

	public TabToggleWidget(UIContext context)
		: base(context)
	{
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		if (TabControlWidget != null && !string.IsNullOrEmpty(TabName))
		{
			TabControlWidget.SetActiveTab(TabName);
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		bool isDisabled = false;
		if (TabControlWidget == null || string.IsNullOrEmpty(TabName))
		{
			isDisabled = true;
		}
		else
		{
			Widget widget = TabControlWidget.FindChild(TabName);
			if (widget == null || widget.IsDisabled)
			{
				isDisabled = true;
			}
		}
		base.IsDisabled = isDisabled;
		base.IsSelected = DetermineIfIsSelected();
	}

	private bool DetermineIfIsSelected()
	{
		if (TabControlWidget?.ActiveTab != null && !string.IsNullOrEmpty(TabName) && TabControlWidget.ActiveTab.Id == TabName)
		{
			return base.IsVisible;
		}
		return false;
	}
}
