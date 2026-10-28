using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GameOver;

public class GameOverCategoryIconBrushWidget : BrushWidget
{
	public string CategoryID { get; set; }

	public GameOverCategoryIconBrushWidget(UIContext context)
		: base(context)
	{
		base.EventManager.AddLateUpdateAction(this, OnManualLateUpdate, 4);
	}

	private void OnManualLateUpdate(float obj)
	{
		base.Brush = base.Context.GetBrush("GameOver.Category.Visual." + CategoryID);
	}
}
