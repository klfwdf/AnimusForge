using StoryMode.Missions;
using StoryMode.View.Missions;
using StoryMode.ViewModelCollection.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace StoryMode.GauntletUI.Missions;

[OverrideView(typeof(MissionTrainingFieldObjectiveView))]
public class MissionGauntletTrainingFieldObjectiveView : MissionView
{
	private TrainingFieldObjectivesVM _dataSource;

	private GauntletLayer _layer;

	private float _beginningTime;

	private bool _isTimerActive;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		TrainingFieldMissionController missionBehavior = base.Mission.GetMissionBehavior<TrainingFieldMissionController>();
		_dataSource = new TrainingFieldObjectivesVM();
		_dataSource.UpdateCurrentObjectiveExplanationText(missionBehavior.InitialCurrentObjective);
		_layer = new GauntletLayer("TrainingFieldObjectives", 2);
		_layer.LoadMovie("TrainingFieldObjectives", _dataSource);
		base.MissionScreen.AddLayer(_layer);
		missionBehavior.TimerTick = _dataSource.UpdateTimerText;
		missionBehavior.CurrentObjectiveTick = _dataSource.UpdateCurrentObjectiveExplanationText;
		missionBehavior.AllObjectivesTick = _dataSource.UpdateObjectivesWith;
		missionBehavior.UIStartTimer = BeginTimer;
		missionBehavior.UIEndTimer = EndTimer;
		missionBehavior.CurrentMouseObjectiveTick = _dataSource.UpdateCurrentMouseObjective;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_isTimerActive)
		{
			_dataSource.UpdateTimerText((base.Mission.CurrentTime - _beginningTime).ToString("0.0"));
		}
	}

	private void BeginTimer()
	{
		_isTimerActive = true;
		_beginningTime = base.Mission.CurrentTime;
	}

	private float EndTimer()
	{
		_isTimerActive = false;
		_dataSource.UpdateTimerText("");
		return base.Mission.CurrentTime - _beginningTime;
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_layer);
		_dataSource = null;
		_layer = null;
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_layer != null)
		{
			_layer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_layer != null)
		{
			_layer.UIContext.ContextAlpha = 1f;
		}
	}
}
