using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class AutoClosePopupWidget : Widget
{
	private List<AutoClosePopupClosingWidget> _closingWidgets = new List<AutoClosePopupClosingWidget>();

	protected Widget _lastCheckedMouseUpWidget;

	private Widget _popupParentWidget;

	[Editor(false)]
	public Widget PopupParentWidget
	{
		get
		{
			return _popupParentWidget;
		}
		set
		{
			if (_popupParentWidget != value)
			{
				_popupParentWidget = value;
				OnPropertyChanged(value, "PopupParentWidget");
			}
		}
	}

	public AutoClosePopupWidget(UIContext context)
		: base(context)
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			if (GetChild(i) is AutoClosePopupClosingWidget item)
			{
				_closingWidgets.Add(item);
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.IsVisible && base.EventManager.LatestMouseUpWidget != PopupParentWidget && base.EventManager.LatestMouseUpWidget != _lastCheckedMouseUpWidget)
		{
			base.IsVisible = base.EventManager.LatestMouseUpWidget == this || CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget);
			CheckClosingWidgetsAndUpdateVisibility();
			_lastCheckedMouseUpWidget = (base.IsVisible ? base.EventManager.LatestMouseUpWidget : null);
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		if (child is AutoClosePopupClosingWidget item)
		{
			_closingWidgets.Add(item);
		}
	}

	protected void CheckClosingWidgetsAndUpdateVisibility()
	{
		if (!base.IsVisible)
		{
			return;
		}
		foreach (AutoClosePopupClosingWidget closingWidget in _closingWidgets)
		{
			if (closingWidget.ShouldClosePopup())
			{
				base.IsVisible = false;
				break;
			}
		}
	}
}
