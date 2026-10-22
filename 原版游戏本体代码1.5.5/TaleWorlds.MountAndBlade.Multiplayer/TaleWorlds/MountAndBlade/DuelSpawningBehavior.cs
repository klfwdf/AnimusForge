using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

namespace TaleWorlds.MountAndBlade;

public class DuelSpawningBehavior : SpawningBehaviorBase
{
	public override void Initialize(SpawnComponent spawnComponent)
	{
		base.Initialize(spawnComponent);
		base.OnPeerSpawnedFromVisuals += OnPeerSpawned;
		if (GameMode.WarmupComponent == null)
		{
			RequestStartSpawnSession();
		}
	}

	public override void Clear()
	{
		base.Clear();
		base.OnPeerSpawnedFromVisuals -= OnPeerSpawned;
	}

	public override void OnTick(float dt)
	{
		if (IsSpawningEnabled && SpawnCheckTimer.Check(Mission.Current.CurrentTime))
		{
			SpawnAgents();
		}
		base.OnTick(dt);
	}

	protected override void SpawnAgents()
	{
		foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
		{
			if (!networkPeer2.IsSynchronized)
			{
				continue;
			}
			MissionPeer component = networkPeer2.GetComponent<MissionPeer>();
			if (!(component.Representative is DuelMissionRepresentative) || !networkPeer2.IsSynchronized || component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || component.Culture == null || !component.SpawnTimer.Check(Mission.Current.CurrentTime))
			{
				continue;
			}
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
			if (mPHeroClassForPeer == null)
			{
				if (component.SelectedTroopIndex == 0)
				{
					continue;
				}
				component.SelectedTroopIndex = 0;
				int broadcastFlags = (GameMode.IsGameModeUsingOpposingTeams ? 64 : 0);
				NetworkCommunicator targetPlayer = (GameMode.IsGameModeUsingOpposingTeams ? networkPeer2 : null);
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer2, component.SelectedTroopIndex));
				GameNetwork.EndBroadcastModuleEvent((GameNetwork.EventBroadcastFlags)broadcastFlags, targetPlayer);
				if (!GameMode.IsGameModeUsingOpposingTeams)
				{
					continue;
				}
				foreach (MissionPeer item in VirtualPlayer.Peers<MissionPeer>())
				{
					NetworkCommunicator networkPeer = item.GetNetworkPeer();
					if (item.IsSpectator && !networkPeer.IsServerPeer && networkPeer.IsSynchronized)
					{
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer2, component.SelectedTroopIndex));
						GameNetwork.EndModuleEventAsServer();
					}
				}
				continue;
			}
			BasicCharacterObject heroCharacter = mPHeroClassForPeer.HeroCharacter;
			Equipment equipment = heroCharacter.Equipment.Clone();
			IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = MPPerkObject.GetOnSpawnPerkHandler(component)?.GetAlternativeEquipments(isPlayer: true);
			if (enumerable != null)
			{
				foreach (var item2 in enumerable)
				{
					equipment[item2.Item1] = item2.Item2;
				}
			}
			AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Equipment(equipment).Team(component.Team)
				.TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
				.IsFemale(component.Peer.IsFemale)
				.BodyProperties(GetBodyProperties(component, component.Culture))
				.VisualsIndex(0)
				.ClothingColor1(component.Culture.Color)
				.ClothingColor2(component.Culture.Color2);
			if (GameMode.ShouldSpawnVisualsForServer(networkPeer2))
			{
				base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(component, agentBuildData, component.SelectedTroopIndex);
				if (agentBuildData.AgentVisualsIndex == 0)
				{
					component.HasSpawnedAgentVisuals = true;
					component.EquipmentUpdatingExpired = false;
				}
			}
			GameMode.HandleAgentVisualSpawning(networkPeer2, agentBuildData);
		}
	}

	public override bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer)
	{
		return true;
	}

	protected override bool IsRoundInProgress()
	{
		return Mission.Current.CurrentState == Mission.State.Continuing;
	}

	private void OnPeerSpawned(MissionPeer peer)
	{
		_ = peer.Representative;
	}
}
