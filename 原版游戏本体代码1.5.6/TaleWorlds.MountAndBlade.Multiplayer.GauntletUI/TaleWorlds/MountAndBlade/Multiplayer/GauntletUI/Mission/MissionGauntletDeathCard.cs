using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerDeathCardUIHandler))]
public class MissionGauntletDeathCard : MissionView
{
	private MPDeathCardVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionPeer _myPeer;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_dataSource = new MPDeathCardVM(missionBehavior.GameType);
		_gauntletLayer = new GauntletLayer("MultiplayerDeathCard", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerDeathCard", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().OnMyAgentVisualSpawned += OnMainAgentVisualSpawned;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (GameNetwork.MyPeer != null && _myPeer == null)
		{
			_myPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
		}
		MissionPeer myPeer = _myPeer;
		if (myPeer != null && myPeer.WantsToSpawnAsBot && _dataSource.IsActive)
		{
			_dataSource.Deactivate();
		}
	}

	private void OnMainAgentVisualSpawned()
	{
		_dataSource.Deactivate();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent.IsMine && blow.DamageType != DamageTypes.Invalid)
		{
			_dataSource.OnMainAgentRemoved(affectorAgent, blow);
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		_dataSource.OnFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().OnMyAgentVisualSpawned -= OnMainAgentVisualSpawned;
		_dataSource = null;
		_gauntletLayer = null;
	}
}
