using NavalDLC.View.MissionViews;
using TaleWorlds.MountAndBlade.View;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(NavalStorylinePirateBattleMissionView))]
internal class MissionGauntletPirateBattleMissionView : NavalStorylinePirateBattleMissionView
{
	protected override void OnShipsInitializedInternal()
	{
		MissionGauntletShipControlView missionBehavior = base.Mission.GetMissionBehavior<MissionGauntletShipControlView>();
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			missionBehavior.SetActiveCameraMode(MissionShipControlView.CameraModes.Back);
		}
	}
}
