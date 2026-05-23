using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class WarmupSpawningBehavior : SpawningBehaviorBase
{
	public WarmupSpawningBehavior()
	{
		IsSpawningEnabled = true;
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
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsSynchronized)
			{
				continue;
			}
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component == null || component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(base.Mission.CurrentTime))
			{
				continue;
			}
			IAgentVisual agentVisualForPeer = component.GetAgentVisualForPeer(0);
			BasicCultureObject basicCultureObject3 = ((component.Culture == basicCultureObject) ? basicCultureObject : basicCultureObject2);
			int num = component.SelectedTroopIndex;
			IEnumerable<MultiplayerClassDivisions.MPHeroClass> mPHeroClasses = MultiplayerClassDivisions.GetMPHeroClasses(basicCultureObject3);
			MultiplayerClassDivisions.MPHeroClass mPHeroClass = ((num < 0) ? null : mPHeroClasses.ElementAt(num));
			if (mPHeroClass == null && num < 0)
			{
				mPHeroClass = mPHeroClasses.First();
				num = 0;
			}
			BasicCharacterObject heroCharacter = mPHeroClass.HeroCharacter;
			Equipment equipment = heroCharacter.Equipment.Clone();
			IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = MPPerkObject.GetOnSpawnPerkHandler(component)?.GetAlternativeEquipments(isPlayer: true);
			if (enumerable != null)
			{
				foreach (var item in enumerable)
				{
					equipment[item.Item1] = item.Item2;
				}
			}
			MatrixFrame matrixFrame;
			if (agentVisualForPeer == null)
			{
				matrixFrame = SpawnComponent.GetSpawnFrame(component.Team, heroCharacter.Equipment.Horse.Item != null);
			}
			else
			{
				matrixFrame = agentVisualForPeer.GetFrame();
				matrixFrame.rotation.MakeUnit();
			}
			MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors = multiplayerBattleColors.GetPeerColors(component);
			AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Equipment(equipment).Team(component.Team)
				.TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
				.InitialPosition(in matrixFrame.origin)
				.InitialDirection(matrixFrame.rotation.f.AsVec2.Normalized())
				.IsFemale(component.Peer.IsFemale)
				.BodyProperties(GetBodyProperties(component, basicCultureObject3))
				.VisualsIndex(0)
				.ClothingColor1(peerColors.ClothingColor1Uint)
				.ClothingColor2(peerColors.ClothingColor2Uint);
			if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
			{
				base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(component, agentBuildData, num);
				if (agentBuildData.AgentVisualsIndex == 0)
				{
					component.HasSpawnedAgentVisuals = true;
					component.EquipmentUpdatingExpired = false;
				}
			}
			GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
		}
	}

	public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
	{
		return true;
	}

	public override int GetMaximumReSpawnPeriodForPeer(MissionPeer peer)
	{
		return 3;
	}

	protected override bool IsRoundInProgress()
	{
		return Mission.Current.CurrentState == Mission.State.Continuing;
	}

	public override void Clear()
	{
		base.Clear();
		RequestStopSpawnSession();
	}
}
