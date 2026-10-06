using TaleWorlds.GauntletUI;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyListPanel : NavigatableListPanel
{
	public PartyListPanel(UIContext context)
		: base(context)
	{
		base.ClearSelectedOnRemoval = true;
	}
}
