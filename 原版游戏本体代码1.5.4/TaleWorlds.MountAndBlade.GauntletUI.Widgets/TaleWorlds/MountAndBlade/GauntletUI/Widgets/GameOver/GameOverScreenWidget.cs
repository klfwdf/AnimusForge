using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GameOver;

public class GameOverScreenWidget : Widget
{
	public BrushWidget ConceptVisualWidget { get; set; }

	public BrushWidget BannerBrushWidget { get; set; }

	public BrushWidget BannerFrameBrushWidget1 { get; set; }

	public BrushWidget BannerFrameBrushWidget2 { get; set; }

	public string GameOverReason { get; set; }

	public GameOverScreenWidget(UIContext context)
		: base(context)
	{
		base.EventManager.AddLateUpdateAction(this, OnManualLateUpdate, 4);
	}

	private void OnManualLateUpdate(float obj)
	{
		if (ConceptVisualWidget != null)
		{
			ConceptVisualWidget.Brush = base.Context.GetBrush("GameOver.Mask." + GameOverReason);
		}
		if (BannerBrushWidget != null)
		{
			BannerBrushWidget.Brush = base.Context.GetBrush("GameOver.Banner." + GameOverReason);
		}
		if (BannerFrameBrushWidget1 != null)
		{
			BannerFrameBrushWidget1.Brush = base.Context.GetBrush("GameOver.Banner.Frame." + GameOverReason);
		}
		if (BannerFrameBrushWidget2 != null)
		{
			BannerFrameBrushWidget2.Brush = base.Context.GetBrush("GameOver.Banner.Frame." + GameOverReason);
		}
	}
}
