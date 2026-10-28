using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.Radial;

public class MissionRadialButtonWidget : ButtonWidget
{
	public MissionRadialButtonWidget(UIContext context)
		: base(context)
	{
	}

	public void ExecuteFocused()
	{
		if (base.IsDisabled)
		{
			SetState("DisabledSelected");
		}
		EventFired("OnFocused");
	}

	public void ExecuteUnfocused()
	{
		if (base.IsDisabled)
		{
			SetState("Disabled");
		}
		else
		{
			SetState("Default");
		}
	}
}
