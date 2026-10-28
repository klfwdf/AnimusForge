namespace TaleWorlds.GauntletUI.BaseTypes;

public class ImageWidget : BrushWidget
{
	public bool OverrideDefaultStateSwitchingEnabled { get; set; }

	public bool OverrideDefaultStateSwitchingDisabled
	{
		get
		{
			return !OverrideDefaultStateSwitchingEnabled;
		}
		set
		{
			if (value != !OverrideDefaultStateSwitchingEnabled)
			{
				OverrideDefaultStateSwitchingEnabled = !value;
			}
		}
	}

	public ImageWidget(UIContext context)
		: base(context)
	{
		AddState("Pressed");
		AddState("Hovered");
		AddState("Disabled");
	}

	protected override void RefreshState()
	{
		if (!OverrideDefaultStateSwitchingEnabled)
		{
			if (base.IsDisabled)
			{
				SetState("Disabled");
			}
			else if (base.IsPressed)
			{
				SetState("Pressed");
			}
			else if (base.IsHovered)
			{
				SetState("Hovered");
			}
			else
			{
				SetState("Default");
			}
		}
		base.RefreshState();
	}
}
