using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionAgentAlarmStateView))]
public class MissionGauntletAgentAlarmStateView : MissionAgentAlarmStateView
{
	private GauntletLayer _layer;

	private MissionAgentAlarmStateVM _dataSource;

	public MissionGauntletAgentAlarmStateView()
	{
		_dataSource = new MissionAgentAlarmStateVM();
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource.Initialize(base.Mission, base.MissionScreen.CombatCamera);
		_layer = new GauntletLayer("MissionAlarmState", 10);
		_layer.LoadMovie("AgentAlarmStateMissionView", _dataSource);
		base.MissionScreen.AddLayer(_layer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_layer);
		_dataSource.OnFinalize();
		_dataSource = null;
		_layer = null;
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		base.OnAgentBuild(agent, banner);
		_dataSource?.OnAgentBuild(agent, banner);
	}

	public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
		base.OnAgentTeamChanged(prevTeam, newTeam, agent);
		_dataSource?.OnAgentTeamChanged(prevTeam, newTeam, agent);
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		_dataSource?.OnAgentRemoved(affectedAgent);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource?.Update();
	}

	protected override void OnResumeView()
	{
		base.OnResumeView();
		ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
	}

	protected override void OnSuspendView()
	{
		base.OnSuspendView();
		ScreenManager.SetSuspendLayer(_layer, isSuspended: true);
	}
}
