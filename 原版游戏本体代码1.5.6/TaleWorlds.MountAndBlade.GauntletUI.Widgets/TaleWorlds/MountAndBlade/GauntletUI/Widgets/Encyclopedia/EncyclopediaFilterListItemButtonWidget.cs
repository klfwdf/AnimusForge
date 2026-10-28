using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Encyclopedia;

public class EncyclopediaFilterListItemButtonWidget : ButtonWidget
{
	public EncyclopediaFilterListItemButtonWidget(UIContext context)
		: base(context)
	{
		base.OverrideDefaultStateSwitchingEnabled = true;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.IsDisabled)
		{
			SetState("Disabled");
		}
		else if (base.IsHovered)
		{
			SetState("Hovered");
		}
		else if (base.IsSelected)
		{
			SetState("Selected");
		}
		else if (base.IsPressed)
		{
			SetState("Pressed");
		}
		else
		{
			SetState("Default");
		}
	}
}
