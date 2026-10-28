using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;

public class MapSiegeQueueIndexTextWidget : TextWidget
{
	public MapSiegeQueueIndexTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		base.IsVisible = base.IntText > 0;
	}
}
