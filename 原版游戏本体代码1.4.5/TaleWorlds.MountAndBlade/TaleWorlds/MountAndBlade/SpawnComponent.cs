using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SpawnComponent : MissionLogic
{
	private MissionMultiplayerGameModeBase _missionMultiplayerGameModeBase;

	public SpawnFrameBehaviorBase SpawnFrameBehavior { get; private set; }

	public SpawningBehaviorBase SpawningBehavior { get; private set; }

	public SpawnComponent(SpawnFrameBehaviorBase spawnFrameBehavior, SpawningBehaviorBase spawningBehavior)
	{
		SpawnFrameBehavior = spawnFrameBehavior;
		SpawningBehavior = spawningBehavior;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionMultiplayerGameModeBase = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
	}

	public bool AreAgentsSpawning()
	{
		return SpawningBehavior.AreAgentsSpawning();
	}

	public void SetNewSpawnFrameBehavior(SpawnFrameBehaviorBase spawnFrameBehavior)
	{
		SpawnFrameBehavior = spawnFrameBehavior;
		if (SpawnFrameBehavior != null)
		{
			SpawnFrameBehavior.Initialize();
		}
	}

	public void SetNewSpawningBehavior(SpawningBehaviorBase spawningBehavior)
	{
		SpawningBehavior = spawningBehavior;
		if (SpawningBehavior != null)
		{
			SpawningBehavior.Initialize(this);
		}
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		SpawningBehavior.Clear();
	}

	public static void SetSiegeSpawningBehavior()
	{
		Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new SiegeSpawnFrameBehavior());
		Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new SiegeSpawningBehavior());
	}

	public static void SetFlagDominationSpawningBehavior()
	{
		Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new FlagDominationSpawnFrameBehavior());
		Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new FlagDominationSpawningBehavior());
	}

	public static void SetWarmupSpawningBehavior()
	{
		Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new FFASpawnFrameBehavior());
		Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new WarmupSpawningBehavior());
	}

	public static void SetSpawningBehaviorForCurrentGameType(MultiplayerGameType currentGameType)
	{
		switch (currentGameType)
		{
		case MultiplayerGameType.Siege:
			SetSiegeSpawningBehavior();
			break;
		case MultiplayerGameType.Battle:
		case MultiplayerGameType.Captain:
		case MultiplayerGameType.Skirmish:
			SetFlagDominationSpawningBehavior();
			break;
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		SetNewSpawnFrameBehavior(SpawnFrameBehavior);
		SetNewSpawningBehavior(SpawningBehavior);
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		SpawningBehavior.OnTick(dt);
	}

	protected void StartSpawnSession()
	{
		SpawningBehavior.RequestStartSpawnSession();
	}

	public MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn = false)
	{
		return SpawnFrameBehavior?.GetSpawnFrame(team, hasMount, isInitialSpawn) ?? MatrixFrame.Identity;
	}

	protected void SpawnEquipmentUpdated(MissionPeer lobbyPeer, Equipment equipment)
	{
		if (GameNetwork.IsServer && lobbyPeer != null && SpawningBehavior.CanUpdateSpawnEquipment(lobbyPeer) && lobbyPeer.HasSpawnedAgentVisuals)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new EquipEquipmentToPeer(lobbyPeer.GetNetworkPeer(), equipment));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}

	public void SetEarlyAgentVisualsDespawning(MissionPeer missionPeer, bool canDespawnEarly = true)
	{
		if (missionPeer != null && AllowEarlyAgentVisualsDespawning(missionPeer))
		{
			missionPeer.EquipmentUpdatingExpired = canDespawnEarly;
		}
	}

	public void ToggleUpdatingSpawnEquipment(bool canUpdate)
	{
		SpawningBehavior.ToggleUpdatingSpawnEquipment(canUpdate);
	}

	public bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
	{
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(lobbyPeer);
		if (!_missionMultiplayerGameModeBase.IsClassAvailable(mPHeroClassForPeer))
		{
			return false;
		}
		return SpawningBehavior.AllowEarlyAgentVisualsDespawning(lobbyPeer);
	}

	public int GetMaximumReSpawnPeriodForPeer(MissionPeer lobbyPeer)
	{
		return SpawningBehavior.GetMaximumReSpawnPeriodForPeer(lobbyPeer);
	}

	public override void OnClearScene()
	{
		base.OnClearScene();
		SpawningBehavior.OnClearScene();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		SpawningBehavior.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		SpawnFrameBehavior.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
	}
}
