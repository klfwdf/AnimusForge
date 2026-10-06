using NavalDLC.Missions.ShipInput;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class WoundedBeastView : MissionView
{
	private bool _initialized;

	public override void OnMissionTick(float dt)
	{
		if (!_initialized)
		{
			Initialize();
			_initialized = true;
		}
	}

	private void Initialize()
	{
		MissionShipControlView missionBehavior = base.Mission.GetMissionBehavior<MissionShipControlView>();
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			missionBehavior.SetSailInput(SailInput.Full);
		}
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			missionBehavior.SetActiveCameraMode(MissionShipControlView.CameraModes.Back);
		}
	}
}
