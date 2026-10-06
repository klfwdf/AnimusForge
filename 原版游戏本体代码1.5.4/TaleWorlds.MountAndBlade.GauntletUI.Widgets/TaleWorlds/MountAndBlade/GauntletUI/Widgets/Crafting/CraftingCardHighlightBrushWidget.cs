using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingCardHighlightBrushWidget : BrushWidget
{
	private bool _playingAnimation;

	private bool _firstFrame = true;

	public CraftingCardHighlightBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		if (_firstFrame && base.IsVisible)
		{
			_firstFrame = false;
		}
		else if (!_playingAnimation && !_firstFrame)
		{
			base.BrushRenderer.RestartAnimation();
			_playingAnimation = true;
		}
	}
}
