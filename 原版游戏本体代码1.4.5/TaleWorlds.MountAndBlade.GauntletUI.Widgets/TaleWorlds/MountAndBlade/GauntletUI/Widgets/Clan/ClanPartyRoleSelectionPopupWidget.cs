using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Clan;

public class ClanPartyRoleSelectionPopupWidget : AutoClosePopupWidget
{
	private List<Widget> _toggleWidgets;

	private Widget _activeToggleWidget;

	[Editor(false)]
	public Widget ActiveToggleWidget
	{
		get
		{
			return _activeToggleWidget;
		}
		set
		{
			if (_activeToggleWidget != value)
			{
				_activeToggleWidget = value;
				OnPropertyChanged(value, "ActiveToggleWidget");
			}
		}
	}

	public ClanPartyRoleSelectionPopupWidget(UIContext context)
		: base(context)
	{
		_toggleWidgets = new List<Widget>();
		base.IsVisible = false;
	}

	protected override void OnLateUpdate(float dt)
	{
		if (base.IsVisible && base.EventManager.LatestMouseUpWidget != _lastCheckedMouseUpWidget && !_toggleWidgets.Contains(base.EventManager.LatestMouseUpWidget))
		{
			base.IsVisible = base.EventManager.LatestMouseUpWidget == this || CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget);
			CheckClosingWidgetsAndUpdateVisibility();
		}
		if (!base.IsVisible)
		{
			ActiveToggleWidget = null;
		}
		_lastCheckedMouseUpWidget = base.EventManager.LatestMouseUpWidget;
	}

	public void AddToggleWidget(Widget widget)
	{
		if (!_toggleWidgets.Contains(widget))
		{
			_toggleWidgets.Add(widget);
		}
	}
}
