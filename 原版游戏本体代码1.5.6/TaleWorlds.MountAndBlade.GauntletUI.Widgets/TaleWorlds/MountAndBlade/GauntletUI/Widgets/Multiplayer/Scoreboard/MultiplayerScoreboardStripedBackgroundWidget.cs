using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

public class MultiplayerScoreboardStripedBackgroundWidget : MultiplayerScoreboardStatsListPanel
{
	public MultiplayerScoreboardStripedBackgroundWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		if (base.ChildCount % 2 == 1)
		{
			child.Sprite = base.Context.SpriteData.GetSprite("BlankWhiteSquare_9");
			child.Color = Color.ConvertStringToColor("#000000FF");
			child.AlphaFactor = 0.2f;
		}
	}
}
