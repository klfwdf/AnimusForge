using TaleWorlds.GauntletUI.Layout;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class DragCarrierWidget : Widget
{
	public DragCarrierWidget(UIContext context)
		: base(context)
	{
		base.LayoutImp = new DragCarrierLayout();
		base.DoNotAcceptEvents = true;
		base.DoNotPassEventsToChildren = true;
		base.IsDisabled = true;
	}
}
