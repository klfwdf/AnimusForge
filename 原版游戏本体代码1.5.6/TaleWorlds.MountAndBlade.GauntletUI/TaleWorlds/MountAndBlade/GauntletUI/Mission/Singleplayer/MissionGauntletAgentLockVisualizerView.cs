using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionAgentLockVisualizerView))]
public class MissionGauntletAgentLockVisualizerView : MissionBattleUIBaseView
{
	private GauntletLayer _layer;

	private MissionAgentLockVisualizerVM _dataSource;

	private MissionMainAgentController _missionMainAgentController;

	private Agent _latestLockedAgent;

	private Agent _latestPotentialLockedAgent;

	protected override void OnCreateView()
	{
		_missionMainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		_missionMainAgentController.OnLockedAgentChanged += OnLockedAgentChanged;
		_missionMainAgentController.OnPotentialLockedAgentChanged += OnPotentialLockedAgentChanged;
		_dataSource = new MissionAgentLockVisualizerVM();
		_layer = new GauntletLayer("MissionAgentLockVisualizer", 10);
		_layer.LoadMovie("AgentLockTargets", _dataSource);
		base.MissionScreen.AddLayer(_layer);
	}

	protected override void OnDestroyView()
	{
		base.MissionScreen.RemoveLayer(_layer);
		_dataSource.OnFinalize();
		_dataSource = null;
		_layer = null;
		_missionMainAgentController = null;
	}

	protected override void OnSuspendView()
	{
		if (_layer != null)
		{
			ScreenManager.SetSuspendLayer(_layer, isSuspended: true);
		}
	}

	protected override void OnResumeView()
	{
		if (_layer != null)
		{
			ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
		}
	}

	private void OnPotentialLockedAgentChanged(Agent newPotentialAgent)
	{
		MissionAgentLockVisualizerVM dataSource = _dataSource;
		if (dataSource != null && dataSource.IsEnabled)
		{
			_dataSource.OnPossibleLockAgentChange(_latestPotentialLockedAgent, newPotentialAgent);
			_latestPotentialLockedAgent = newPotentialAgent;
		}
	}

	private void OnLockedAgentChanged(Agent newAgent)
	{
		MissionAgentLockVisualizerVM dataSource = _dataSource;
		if (dataSource != null && dataSource.IsEnabled)
		{
			_dataSource.OnActiveLockAgentChange(_latestLockedAgent, newAgent);
			_latestLockedAgent = newAgent;
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!base.IsViewCreated || _dataSource == null)
		{
			return;
		}
		_dataSource.IsEnabled = IsMainAgentAvailable();
		if (_dataSource.IsEnabled)
		{
			for (int i = 0; i < _dataSource.AllTrackedAgents.Count; i++)
			{
				MissionAgentLockItemVM missionAgentLockItemVM = _dataSource.AllTrackedAgents[i];
				float screenX = 0f;
				float screenY = 0f;
				float w = 0f;
				MBWindowManager.WorldToScreenInsideUsableArea(base.MissionScreen.CombatCamera, missionAgentLockItemVM.TrackedAgent.GetChestGlobalPosition(), ref screenX, ref screenY, ref w);
				missionAgentLockItemVM.Position = new Vec2(screenX, screenY);
			}
		}
	}

	private bool IsMainAgentAvailable()
	{
		return Agent.Main?.IsActive() ?? false;
	}
}
