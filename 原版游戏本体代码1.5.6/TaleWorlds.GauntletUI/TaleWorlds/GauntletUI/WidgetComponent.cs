using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI;

public abstract class WidgetComponent
{
	public Widget Target { get; private set; }

	protected WidgetComponent(Widget target)
	{
		Target = target;
	}
}
