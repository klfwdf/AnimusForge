using NavalDLC.View.MissionViews;
using NavalDLC.View.MissionViews.Storyline;
using TaleWorlds.MountAndBlade.View;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(NavalCaptivityBattleMissionView))]
public class MissionGauntletCaptivityMissionView : NavalCaptivityBattleMissionView
{
	private bool _hasHandledOarsmenLevel;

	protected override void OnFirstHighlightClearedInternal()
	{
		MissionGauntletShipControlView missionBehavior = base.Mission.GetMissionBehavior<MissionGauntletShipControlView>();
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			missionBehavior.ResumeFeature(MissionGauntletShipControlView.ShipControlFeatureFlags.ToggleSails);
		}
	}

	protected override void OnPlayerStartedEscapeInternal()
	{
		MissionGauntletShipControlView missionBehavior = base.Mission.GetMissionBehavior<MissionGauntletShipControlView>();
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			missionBehavior.SuspendFeature(MissionGauntletShipControlView.ShipControlFeatureFlags.ToggleSails);
			missionBehavior.SuspendFeature(MissionGauntletShipControlView.ShipControlFeatureFlags.ChangeCamera);
			missionBehavior.SetActiveCameraMode(MissionShipControlView.CameraModes.Shoulder);
		}
	}

	protected override void OnOarsmenLevelChangedInternal(int level)
	{
		if (!_hasHandledOarsmenLevel && level == 2)
		{
			_hasHandledOarsmenLevel = true;
			base.Mission.GetMissionBehavior<MissionGauntletShipControlView>().ResumeFeature(MissionGauntletShipControlView.ShipControlFeatureFlags.ChangeCamera);
		}
	}
}
