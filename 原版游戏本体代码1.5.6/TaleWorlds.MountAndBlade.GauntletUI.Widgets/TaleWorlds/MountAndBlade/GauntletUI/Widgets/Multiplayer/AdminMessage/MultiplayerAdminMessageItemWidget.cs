using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.AdminMessage;

public class MultiplayerAdminMessageItemWidget : Widget
{
	public MultiplayerAdminMessageItemWidget(UIContext context)
		: base(context)
	{
	}

	public void Remove()
	{
		EventFired("Remove");
	}
}
