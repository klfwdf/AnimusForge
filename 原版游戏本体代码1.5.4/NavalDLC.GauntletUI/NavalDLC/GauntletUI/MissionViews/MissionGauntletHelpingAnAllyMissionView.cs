using NavalDLC.View.MissionViews;
using NavalDLC.View.MissionViews.Storyline;
using TaleWorlds.MountAndBlade.View;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(HelpingAnAllyMissionView))]
public class MissionGauntletHelpingAnAllyMissionView : HelpingAnAllyMissionView
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
