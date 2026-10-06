using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Missions.Objectives;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Objective;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionObjectiveView))]
public class MissionGauntletObjectiveView : MissionObjectiveView
{
	private GauntletLayer _gauntletLayer;

	private MissionObjectiveVM _dataSource;

	private MissionObjectiveLogic _objectiveLogic;

	private MissionObjective _latestObjective;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_objectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
		if (_objectiveLogic == null)
		{
			Debug.FailedAssert("Mission objective view is enabled but there is no objective logic in mission", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\Mission\\MissionGauntletObjectiveView.cs", "OnMissionScreenInitialize", 34);
			return;
		}
		_dataSource = new MissionObjectiveVM(_objectiveLogic, base.MissionScreen.CombatCamera);
		_gauntletLayer = new GauntletLayer("MissionObjective", 1);
		_gauntletLayer.LoadMovie("MissionObjectives", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		if (_gauntletLayer != null)
		{
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
		}
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
			_dataSource = null;
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_objectiveLogic != null && _gauntletLayer != null && _dataSource != null)
		{
			UpdateContextAlpha(dt);
			MissionObjective currentObjective = _objectiveLogic.CurrentObjective;
			if (_latestObjective != currentObjective)
			{
				_latestObjective = currentObjective;
				_dataSource.UpdateObjective(_latestObjective);
			}
			_dataSource.Tick(dt);
		}
	}

	private void UpdateContextAlpha(float dt)
	{
		float valueTo = (_dataSource.IsEnabled ? 1f : 0f);
		float amount = MathF.Clamp(dt * 6f, 0f, 1f);
		float contextAlpha = _gauntletLayer.UIContext.ContextAlpha;
		contextAlpha = MathF.Lerp(contextAlpha, valueTo, amount);
		_gauntletLayer.UIContext.ContextAlpha = contextAlpha;
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

	protected override void OnResumeView()
	{
		base.OnResumeView();
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
	}

	protected override void OnSuspendView()
	{
		base.OnSuspendView();
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
	}
}
