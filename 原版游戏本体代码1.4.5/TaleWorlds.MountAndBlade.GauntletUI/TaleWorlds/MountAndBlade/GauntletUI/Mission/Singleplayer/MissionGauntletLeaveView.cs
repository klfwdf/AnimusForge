using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionLeaveView))]
public class MissionGauntletLeaveView : MissionView
{
	private GauntletLayer _gauntletLayer;

	private MissionLeaveVM _dataSource;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new MissionLeaveVM(base.Mission.GetMissionEndTimerValue, base.Mission.GetMissionEndTimeInSeconds);
		_gauntletLayer = new GauntletLayer("MissionLeave", 47);
		_gauntletLayer.LoadMovie("LeaveUI", _dataSource);
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
		_dataSource.Tick(dt);
	}

	private void OnEscapeMenuToggled(bool isOpened)
	{
		ScreenManager.SetSuspendLayer(_gauntletLayer, !isOpened);
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
