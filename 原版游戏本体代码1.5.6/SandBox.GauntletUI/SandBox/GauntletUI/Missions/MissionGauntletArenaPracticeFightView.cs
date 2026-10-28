using SandBox.Missions.MissionLogics.Arena;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionArenaPracticeFightView))]
public class MissionGauntletArenaPracticeFightView : MissionView
{
	private MissionArenaPracticeFightVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _movie;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		ArenaPracticeFightMissionController missionBehavior = base.Mission.GetMissionBehavior<ArenaPracticeFightMissionController>();
		_dataSource = new MissionArenaPracticeFightVM(missionBehavior);
		_gauntletLayer = new GauntletLayer("MissionArenaPracticeFight", ViewOrderPriority);
		_movie = _gauntletLayer.LoadMovie("ArenaPracticeFight", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.Tick();
	}

	public override void OnMissionScreenFinalize()
	{
		_dataSource.OnFinalize();
		_gauntletLayer.ReleaseMovie(_movie);
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		base.OnMissionScreenFinalize();
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
