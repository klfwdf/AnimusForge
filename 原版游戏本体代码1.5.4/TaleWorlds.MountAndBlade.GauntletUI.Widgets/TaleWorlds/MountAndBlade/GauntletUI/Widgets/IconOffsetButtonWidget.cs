using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class IconOffsetButtonWidget : IconBrushWidget
{
	public int NormalXOffset { get; set; }

	public int NormalYOffset { get; set; }

	public int PressedXOffset { get; set; }

	public int PressedYOffset { get; set; }

	public Widget ButtonIcon { get; set; }

	public IconOffsetButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		BrushLayer brushLayer = base.IconBrush?.GetLayer(base.IconID);
		if (brushLayer?.Sprite != null)
		{
			base.SuggestedWidth = brushLayer.Sprite.Width;
			base.SuggestedHeight = brushLayer.Sprite.Height;
		}
		if (ButtonIcon != null)
		{
			if (base.IsPressed || base.IsSelected)
			{
				ButtonIcon.PositionYOffset = PressedYOffset;
				ButtonIcon.PositionXOffset = PressedXOffset;
			}
			else
			{
				ButtonIcon.PositionYOffset = NormalYOffset;
				ButtonIcon.PositionXOffset = NormalXOffset;
			}
		}
	}

	protected override void RefreshState()
	{
		if (base.IsSelected)
		{
			SetState("Selected");
		}
		else if (base.IsDisabled)
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
}
