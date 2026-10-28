using NavalDLC.Missions;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.View.MissionViews;
using NavalDLC.ViewModelCollection.Missions.CaptureShip;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(NavalMissionCaptureShipView))]
public class MissionGauntletNavalCaptureShipView : MissionView
{
	private GauntletLayer _gauntletLayer;

	private NavalMissionCaptureShipVM _dataSource;

	public ShipControllerMachine ControllerMachine { get; private set; }

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new NavalMissionCaptureShipVM(3f);
		_gauntletLayer = new GauntletLayer("NavalMissionCaptureShip", 47);
		_gauntletLayer.LoadMovie("NavalMissionCaptureShip", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		ControllerMachine = Agent.Main?.GetComponent<AgentNavalComponent>()?.SteppedShip?.ShipControllerMachine;
		if (ControllerMachine != null && Agent.Main != null && ControllerMachine.PilotAgent == Agent.Main)
		{
			_dataSource.UpdateCaptureTimer(ControllerMachine.CaptureTimer);
		}
		else
		{
			_dataSource.UpdateCaptureTimer(-1f);
		}
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}
}
