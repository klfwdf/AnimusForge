using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.CustomBattle.Views;

[OverrideView(typeof(MissionCheatView))]
internal class GauntletCustomBattleMissionCheatView : MissionCheatView
{
	public override void InitializeScreen()
	{
	}

	public override void FinalizeScreen()
	{
	}

	public override bool GetIsCheatsAvailable()
	{
		return false;
	}
}
