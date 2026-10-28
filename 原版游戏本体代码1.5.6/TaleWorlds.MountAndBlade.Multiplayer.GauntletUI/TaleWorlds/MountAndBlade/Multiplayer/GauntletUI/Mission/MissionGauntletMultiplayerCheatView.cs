using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionCheatView))]
public class MissionGauntletMultiplayerCheatView : MissionCheatView
{
	public override bool GetIsCheatsAvailable()
	{
		return false;
	}

	public override void InitializeScreen()
	{
	}

	public override void FinalizeScreen()
	{
	}
}
