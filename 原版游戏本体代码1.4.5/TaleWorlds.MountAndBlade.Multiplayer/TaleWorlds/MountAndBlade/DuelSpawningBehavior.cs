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
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsSynchronized)
			{
				continue;
			}
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (!(component.Representative is DuelMissionRepresentative) || !networkPeer.IsSynchronized || component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || component.Culture == null || !component.SpawnTimer.Check(Mission.Current.CurrentTime))
			{
				continue;
			}
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
			if (mPHeroClassForPeer == null)
			{
				if (component.SelectedTroopIndex != 0)
				{
					component.SelectedTroopIndex = 0;
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
				}
				continue;
			}
			BasicCharacterObject heroCharacter = mPHeroClassForPeer.HeroCharacter;
			Equipment equipment = heroCharacter.Equipment.Clone();
			IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = MPPerkObject.GetOnSpawnPerkHandler(component)?.GetAlternativeEquipments(isPlayer: true);
			if (enumerable != null)
			{
				foreach (var item in enumerable)
				{
					equipment[item.Item1] = item.Item2;
				}
			}
			AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Equipment(equipment).Team(component.Team)
				.TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
				.IsFemale(component.Peer.IsFemale)
				.BodyProperties(GetBodyProperties(component, component.Culture))
				.VisualsIndex(0)
				.ClothingColor1(component.Culture.Color)
				.ClothingColor2(component.Culture.Color2);
			if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
			{
				base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(component, agentBuildData, component.SelectedTroopIndex);
				if (agentBuildData.AgentVisualsIndex == 0)
				{
					component.HasSpawnedAgentVisuals = true;
					component.EquipmentUpdatingExpired = false;
				}
			}
			GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
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
