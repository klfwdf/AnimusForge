using SandBox.Missions.MissionLogics;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionQuestBarView))]
public class MissionGauntletQuestBarView : MissionQuestBarView
{
	private const float MinProgressValue = 0f;

	private const float MaxProgressValue = 1f;

	private GauntletLayer _gauntletLayer;

	private MissionQuestBarVM _dataSource;

	private IMissionProgressTracker _missionProgressTracker;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new MissionQuestBarVM();
		_gauntletLayer = new GauntletLayer("MissionQuestBar", 10);
		_gauntletLayer.LoadMovie("MissionQuestBar", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
		{
			if (missionBehavior is IMissionProgressTracker)
			{
				_missionProgressTracker = missionBehavior as IMissionProgressTracker;
				break;
			}
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		_dataSource.OnFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_missionProgressTracker != null)
		{
			_dataSource.UpdateQuestValues(0f, 1f, _missionProgressTracker.CurrentProgress);
		}
	}
}
