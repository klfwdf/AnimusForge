using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class SiegeSpawningBehavior : SpawningBehaviorBase
{
	public override void Initialize(SpawnComponent spawnComponent)
	{
		base.Initialize(spawnComponent);
		base.OnAllAgentsFromPeerSpawnedFromVisuals += OnAllAgentsFromPeerSpawnedFromVisuals;
		if (GameMode.WarmupComponent == null)
		{
			RequestStartSpawnSession();
		}
	}

	public override void Clear()
	{
		base.Clear();
		base.OnAllAgentsFromPeerSpawnedFromVisuals -= OnAllAgentsFromPeerSpawnedFromVisuals;
	}

	public override void OnTick(float dt)
	{
		if (IsSpawningEnabled && SpawnCheckTimer.Check(base.Mission.CurrentTime))
		{
			SpawnAgents();
		}
		base.OnTick(dt);
	}

	protected override void SpawnAgents()
	{
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject basicCultureObject2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, basicCultureObject2);
		foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
		{
			if (!networkPeer2.IsSynchronized)
			{
				continue;
			}
			MissionPeer component = networkPeer2.GetComponent<MissionPeer>();
			if (component == null || component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(base.Mission.CurrentTime))
			{
				continue;
			}
			_ = component.Team.Side;
			_ = 1;
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
			if (mPHeroClassForPeer == null || mPHeroClassForPeer.TroopCasualCost > GameMode.GetCurrentGoldForPeer(component))
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
			MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors = multiplayerBattleColors.GetPeerColors(component);
			AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Equipment(equipment).Team(component.Team)
				.TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
				.IsFemale(component.Peer.IsFemale)
				.BodyProperties(GetBodyProperties(component, (component.Culture == basicCultureObject) ? basicCultureObject : basicCultureObject2))
				.VisualsIndex(0)
				.ClothingColor1(peerColors.ClothingColor1Uint)
				.ClothingColor2(peerColors.ClothingColor2Uint);
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

	public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
	{
		return true;
	}

	public override int GetMaximumReSpawnPeriodForPeer(MissionPeer peer)
	{
		if (GameMode.WarmupComponent != null && GameMode.WarmupComponent.IsInWarmup)
		{
			return 3;
		}
		if (peer.Team != null)
		{
			if (peer.Team.Side == BattleSideEnum.Attacker)
			{
				return MultiplayerOptions.OptionType.RespawnPeriodTeam1.GetIntValue();
			}
			if (peer.Team.Side == BattleSideEnum.Defender)
			{
				return MultiplayerOptions.OptionType.RespawnPeriodTeam2.GetIntValue();
			}
		}
		return -1;
	}

	protected override bool IsRoundInProgress()
	{
		return Mission.Current.CurrentState == Mission.State.Continuing;
	}

	private new void OnAllAgentsFromPeerSpawnedFromVisuals(MissionPeer peer)
	{
		bool flag = peer.Team == base.Mission.AttackerTeam;
		_ = base.Mission.DefenderTeam;
		MultiplayerClassDivisions.MPHeroClass mPHeroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue())).ElementAt(peer.SelectedTroopIndex);
		GameMode.ChangeCurrentGoldForPeer(peer, GameMode.GetCurrentGoldForPeer(peer) - mPHeroClass.TroopCasualCost);
	}
}
