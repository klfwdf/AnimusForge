using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class GamepadCursorMarkerWidget : BrushWidget
{
	private bool _flipVisual;

	public bool FlipVisual
	{
		get
		{
			return _flipVisual;
		}
		set
		{
			if (value != _flipVisual)
			{
				_flipVisual = value;
				base.Brush.DefaultLayer.HorizontalFlip = value;
			}
		}
	}

	public GamepadCursorMarkerWidget(UIContext context)
		: base(context)
	{
	}
}
