using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class AutoClosePopupClosingWidget : Widget
{
	public Widget Target { get; set; }

	public bool IncludeChildren { get; set; }

	public bool IncludeTarget { get; set; }

	public AutoClosePopupClosingWidget(UIContext context)
		: base(context)
	{
	}

	public bool ShouldClosePopup()
	{
		if (!IncludeTarget || base.EventManager.LatestMouseUpWidget != Target)
		{
			if (IncludeChildren)
			{
				return Target?.CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget) ?? false;
			}
			return false;
		}
		return true;
	}
}
