using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.Missions.MissionLogics;

public class MissionLocationLogic : MissionLogic
{
	private readonly Location _previousLocation;

	private readonly Location _currentLocation;

	private MissionAgentHandler _missionAgentHandler;

	private readonly string _playerSpecialSpawnTag;

	private bool _noHorsesforCharactersAccompanyingPlayer;

	public MissionLocationLogic(Location location, string specialPlayerTag = null)
	{
		_currentLocation = location;
		_previousLocation = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.GameMenuManager.PreviousLocation : null);
		if (_previousLocation != null)
		{
			Location currentLocation = _currentLocation;
			if (currentLocation != null && !currentLocation.LocationsOfPassages.Contains(_previousLocation))
			{
				Debug.FailedAssert(string.Concat("No passage from ", _previousLocation.DoorName, " to ", _currentLocation.DoorName), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\MissionLocationLogic.cs", ".ctor", 36);
				_previousLocation = null;
			}
		}
		_playerSpecialSpawnTag = specialPlayerTag;
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
		CampaignEvents.BeforePlayerAgentSpawnEvent.AddNonSerializedListener(this, OnBeforePlayerAgentSpawn);
		CampaignEvents.PlayerAgentSpawned.AddNonSerializedListener(this, OnPlayerAgentSpawned);
	}

	public override void EarlyStart()
	{
		_missionAgentHandler = Mission.Current.GetMissionBehavior<MissionAgentHandler>();
	}

	private void OnPlayerAgentSpawned()
	{
		SpawnCharactersAccompanyingPlayer(_noHorsesforCharactersAccompanyingPlayer);
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (!affectedAgent.IsHuman || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
		{
			return;
		}
		LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(affectedAgent.Origin);
		if (locationCharacter != null)
		{
			CampaignMission.Current.Location.RemoveLocationCharacter(locationCharacter);
			if (PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter) != null && affectedAgent.State == AgentState.Killed)
			{
				PlayerEncounter.LocationEncounter.RemoveAccompanyingCharacter(locationCharacter);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		foreach (Location listOfLocation in LocationComplex.Current.GetListOfLocations())
		{
			if (listOfLocation.StringId == "center" || listOfLocation.StringId == "village_center" || listOfLocation.StringId == "lordshall" || listOfLocation.StringId == "prison" || listOfLocation.StringId == "tavern" || listOfLocation.StringId == "alley" || listOfLocation.StringId == "port")
			{
				listOfLocation.RemoveAllCharacters((LocationCharacter x) => !x.Character.IsHero);
			}
		}
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		base.OnRemoveBehavior();
	}

	private void OnBeforePlayerAgentSpawn(ref MatrixFrame spawnPointFrame)
	{
		bool flag = Campaign.Current.GameMode == CampaignGameMode.Campaign && PlayerEncounter.IsActive && (Settlement.CurrentSettlement.IsTown || Settlement.CurrentSettlement.IsCastle) && !Campaign.Current.IsNight && CampaignMission.Current.Location.StringId == "center" && !PlayerEncounter.LocationEncounter.IsInsideOfASettlement;
		if (!string.IsNullOrEmpty(_playerSpecialSpawnTag))
		{
			WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
			UsableMachine usableMachine = _missionAgentHandler?.GetAllUsablePointsWithTag(_playerSpecialSpawnTag).FirstOrDefault();
			if (usableMachine != null)
			{
				weakGameEntity = usableMachine.StandingPoints.FirstOrDefault()?.GameEntity ?? WeakGameEntity.Invalid;
			}
			if (!weakGameEntity.IsValid)
			{
				weakGameEntity = Mission.Current.Scene.FindEntityWithTag(_playerSpecialSpawnTag)?.WeakEntity ?? WeakGameEntity.Invalid;
			}
			if (weakGameEntity.IsValid)
			{
				spawnPointFrame = weakGameEntity.GetGlobalFrame();
				spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			}
		}
		else if (CampaignMission.Current.Location.StringId == "arena")
		{
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_player_near_arena_master");
			if (gameEntity != null)
			{
				spawnPointFrame = gameEntity.GetGlobalFrame();
				spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			}
		}
		else if (_previousLocation != null)
		{
			spawnPointFrame = GetSpawnFrameOfPassage(_previousLocation);
			spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			_noHorsesforCharactersAccompanyingPlayer = true;
		}
		else if (flag)
		{
			GameEntity gameEntity2 = Mission.Current.Scene.FindEntityWithTag("spawnpoint_player_outside");
			if (gameEntity2 != null)
			{
				spawnPointFrame = gameEntity2.GetGlobalFrame();
				spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			}
		}
		if (PlayerEncounter.LocationEncounter is TownEncounter)
		{
			PlayerEncounter.LocationEncounter.IsInsideOfASettlement = true;
		}
		if (PlayerEncounter.LocationEncounter.Settlement.IsTown || PlayerEncounter.LocationEncounter.Settlement.IsCastle)
		{
			_noHorsesforCharactersAccompanyingPlayer = true;
		}
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unUsedPoints)
	{
		IEnumerable<LocationCharacter> characterList = CampaignMission.Current.Location.GetCharacterList();
		if (!PlayerEncounter.LocationEncounter.Settlement.IsTown || CampaignMission.Current.Location != LocationComplex.Current.GetLocationWithId("center"))
		{
			return;
		}
		foreach (LocationCharacter character in LocationComplex.Current.GetLocationWithId("alley").GetCharacterList())
		{
			characterList.Append(character);
		}
	}

	public override void OnCreated()
	{
		if (_currentLocation != null)
		{
			CampaignMission.Current.Location = _currentLocation;
		}
	}

	public void SpawnCharactersAccompanyingPlayer(bool noHorse)
	{
		int num = 0;
		bool flag = PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.Any((AccompanyingCharacter c) => c.IsFollowingPlayerAtMissionStart);
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
		foreach (AccompanyingCharacter item in PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer)
		{
			bool flag2 = item.LocationCharacter.Character.IsHero && item.LocationCharacter.Character.HeroObject.IsWounded;
			if ((!_currentLocation.GetCharacterList().Contains(item.LocationCharacter) && flag2) || !item.CanEnterLocation(_currentLocation))
			{
				continue;
			}
			_currentLocation.AddCharacter(item.LocationCharacter);
			if (item.IsFollowingPlayerAtMissionStart || (!flag && num == 0))
			{
				WorldFrame worldFrame = base.Mission.MainAgent.GetWorldFrame();
				worldFrame.Origin.SetVec2(base.Mission.GetRandomPositionAroundPoint(worldFrame.Origin.GetNavMeshVec3(), 0.5f, 2f).AsVec2);
				Agent agent = _missionAgentHandler.SpawnWanderingAgentWithInitialFrame(item.LocationCharacter, worldFrame.ToGroundMatrixFrame(), WeakGameEntity.Invalid, noHorse);
				if (gameEntity != null)
				{
					int disableFaceWithId = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>().DisableFaceWithId;
					if (disableFaceWithId != -1)
					{
						agent.SetAgentExcludeStateForFaceGroupId(disableFaceWithId, isExcluded: false);
					}
				}
				int num2 = 0;
				while (!agent.CanMoveDirectlyToPosition(base.Mission.MainAgent.Position.AsVec2) && num2 < 50)
				{
					worldFrame.Origin.SetVec2(base.Mission.GetRandomPositionAroundPoint(worldFrame.Origin.GetNavMeshVec3(), 0.5f, 4f).AsVec2);
					agent.TeleportToPosition(worldFrame.ToGroundMatrixFrame().origin);
					num2++;
				}
				agent.SetTeam(base.Mission.PlayerTeam, sync: true);
				num++;
			}
			else
			{
				_missionAgentHandler.SpawnWanderingAgent(item.LocationCharacter).SetTeam(base.Mission.PlayerTeam, sync: true);
			}
			foreach (Agent agent2 in base.Mission.Agents)
			{
				LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(agent2.Origin);
				AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter);
				if (agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null && accompanyingCharacter != null)
				{
					DailyBehaviorGroup behaviorGroup = agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
					if (item.IsFollowingPlayerAtMissionStart)
					{
						FollowAgentBehavior obj = behaviorGroup.GetBehavior<FollowAgentBehavior>() ?? behaviorGroup.AddBehavior<FollowAgentBehavior>();
						behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
						obj.SetTargetAgent(Agent.Main);
					}
					else
					{
						behaviorGroup.Behaviors.Clear();
					}
				}
			}
		}
	}

	public MatrixFrame GetSpawnFrameOfPassage(Location location)
	{
		MatrixFrame result = MatrixFrame.Identity;
		UsableMachine usableMachine = _missionAgentHandler.TownPassageProps.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location) ?? _missionAgentHandler.DisabledPassages.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location);
		if (usableMachine != null)
		{
			MatrixFrame globalFrame = usableMachine.PilotStandingPoint.GameEntity.GetGlobalFrame();
			globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			globalFrame.origin.z = base.Mission.Scene.GetGroundHeightAtPosition(globalFrame.origin);
			globalFrame.rotation.RotateAboutUp(System.MathF.PI);
			result = globalFrame;
		}
		return result;
	}
}
