using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class FaceGeneratorMissionView : MissionView
{
	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (base.Input.IsGameKeyPressed(37) && !GameNetwork.IsSessionActive)
		{
			LoadingWindow.EnableGlobalLoadingWindow();
			ScreenManager.PushScreen(ViewCreator.CreateMBFaceGeneratorScreen(Game.Current.PlayerTroop));
		}
	}
}
