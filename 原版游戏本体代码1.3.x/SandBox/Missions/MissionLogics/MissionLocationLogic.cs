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

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200007A RID: 122
	public class MissionLocationLogic : MissionLogic
	{
		// Token: 0x060004F2 RID: 1266 RVA: 0x0001F824 File Offset: 0x0001DA24
		public MissionLocationLogic(Location location, string specialPlayerTag = null)
		{
			this._currentLocation = location;
			this._previousLocation = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.GameMenuManager.PreviousLocation : null);
			if (this._previousLocation != null)
			{
				Location currentLocation = this._currentLocation;
				if (currentLocation != null && !currentLocation.LocationsOfPassages.Contains(this._previousLocation))
				{
					Debug.FailedAssert(string.Concat(new object[]
					{
						"No passage from ",
						this._previousLocation.DoorName,
						" to ",
						this._currentLocation.DoorName
					}), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\MissionLocationLogic.cs", ".ctor", 36);
					this._previousLocation = null;
				}
			}
			this._playerSpecialSpawnTag = specialPlayerTag;
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
			CampaignEvents.BeforePlayerAgentSpawnEvent.AddNonSerializedListener(this, new ReferenceAction<MatrixFrame>(this.OnBeforePlayerAgentSpawn));
			CampaignEvents.PlayerAgentSpawned.AddNonSerializedListener(this, new Action(this.OnPlayerAgentSpawned));
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x0001F928 File Offset: 0x0001DB28
		public override void EarlyStart()
		{
			this._missionAgentHandler = Mission.Current.GetMissionBehavior<MissionAgentHandler>();
		}

		// Token: 0x060004F4 RID: 1268 RVA: 0x0001F93A File Offset: 0x0001DB3A
		private void OnPlayerAgentSpawned()
		{
			this.SpawnCharactersAccompanyingPlayer(this._noHorsesforCharactersAccompanyingPlayer);
		}

		// Token: 0x060004F5 RID: 1269 RVA: 0x0001F948 File Offset: 0x0001DB48
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (affectedAgent.IsHuman && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
			{
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
		}

		// Token: 0x060004F6 RID: 1270 RVA: 0x0001F9B0 File Offset: 0x0001DBB0
		public override void OnRemoveBehavior()
		{
			foreach (Location location in LocationComplex.Current.GetListOfLocations())
			{
				if (location.StringId == "center" || location.StringId == "village_center" || location.StringId == "lordshall" || location.StringId == "prison" || location.StringId == "tavern" || location.StringId == "alley" || location.StringId == "port")
				{
					location.RemoveAllCharacters((LocationCharacter x) => !x.Character.IsHero);
				}
			}
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			base.OnRemoveBehavior();
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x0001FAB4 File Offset: 0x0001DCB4
		private void OnBeforePlayerAgentSpawn(ref MatrixFrame spawnPointFrame)
		{
			bool flag = Campaign.Current.GameMode == CampaignGameMode.Campaign && PlayerEncounter.IsActive && (Settlement.CurrentSettlement.IsTown || Settlement.CurrentSettlement.IsCastle) && !Campaign.Current.IsNight && CampaignMission.Current.Location.StringId == "center" && !PlayerEncounter.LocationEncounter.IsInsideOfASettlement;
			if (!string.IsNullOrEmpty(this._playerSpecialSpawnTag))
			{
				WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
				MissionAgentHandler missionAgentHandler = this._missionAgentHandler;
				UsableMachine usableMachine = ((missionAgentHandler != null) ? missionAgentHandler.GetAllUsablePointsWithTag(this._playerSpecialSpawnTag).FirstOrDefault<UsableMachine>() : null);
				if (usableMachine != null)
				{
					StandingPoint standingPoint = usableMachine.StandingPoints.FirstOrDefault<StandingPoint>();
					weakGameEntity = ((standingPoint != null) ? standingPoint.GameEntity : WeakGameEntity.Invalid);
				}
				if (!weakGameEntity.IsValid)
				{
					GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(this._playerSpecialSpawnTag);
					weakGameEntity = ((gameEntity != null) ? gameEntity.WeakEntity : WeakGameEntity.Invalid);
				}
				if (weakGameEntity.IsValid)
				{
					spawnPointFrame = weakGameEntity.GetGlobalFrame();
					spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				}
			}
			else if (CampaignMission.Current.Location.StringId == "arena")
			{
				GameEntity gameEntity2 = base.Mission.Scene.FindEntityWithTag("sp_player_near_arena_master");
				if (gameEntity2 != null)
				{
					spawnPointFrame = gameEntity2.GetGlobalFrame();
					spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				}
			}
			else if (this._previousLocation != null)
			{
				spawnPointFrame = this.GetSpawnFrameOfPassage(this._previousLocation);
				spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				this._noHorsesforCharactersAccompanyingPlayer = true;
			}
			else if (flag)
			{
				GameEntity gameEntity3 = Mission.Current.Scene.FindEntityWithTag("spawnpoint_player_outside");
				if (gameEntity3 != null)
				{
					spawnPointFrame = gameEntity3.GetGlobalFrame();
					spawnPointFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				}
			}
			if (PlayerEncounter.LocationEncounter is TownEncounter)
			{
				PlayerEncounter.LocationEncounter.IsInsideOfASettlement = true;
			}
			if (PlayerEncounter.LocationEncounter.Settlement.IsTown || PlayerEncounter.LocationEncounter.Settlement.IsCastle)
			{
				this._noHorsesforCharactersAccompanyingPlayer = true;
			}
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x0001FCD4 File Offset: 0x0001DED4
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unUsedPoints)
		{
			IEnumerable<LocationCharacter> characterList = CampaignMission.Current.Location.GetCharacterList();
			if (PlayerEncounter.LocationEncounter.Settlement.IsTown && CampaignMission.Current.Location == LocationComplex.Current.GetLocationWithId("center"))
			{
				foreach (LocationCharacter element in LocationComplex.Current.GetLocationWithId("alley").GetCharacterList())
				{
					characterList.Append(element);
				}
			}
		}

		// Token: 0x060004F9 RID: 1273 RVA: 0x0001FD6C File Offset: 0x0001DF6C
		public override void OnCreated()
		{
			if (this._currentLocation != null)
			{
				CampaignMission.Current.Location = this._currentLocation;
			}
		}

		// Token: 0x060004FA RID: 1274 RVA: 0x0001FD88 File Offset: 0x0001DF88
		public void SpawnCharactersAccompanyingPlayer(bool noHorse)
		{
			int num = 0;
			bool flag = PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.Any((AccompanyingCharacter c) => c.IsFollowingPlayerAtMissionStart);
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
			foreach (AccompanyingCharacter accompanyingCharacter in PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer)
			{
				bool flag2 = accompanyingCharacter.LocationCharacter.Character.IsHero && accompanyingCharacter.LocationCharacter.Character.HeroObject.IsWounded;
				if ((this._currentLocation.GetCharacterList().Contains(accompanyingCharacter.LocationCharacter) || !flag2) && accompanyingCharacter.CanEnterLocation(this._currentLocation))
				{
					this._currentLocation.AddCharacter(accompanyingCharacter.LocationCharacter);
					if (accompanyingCharacter.IsFollowingPlayerAtMissionStart || (!flag && num == 0))
					{
						WorldFrame worldFrame = base.Mission.MainAgent.GetWorldFrame();
						worldFrame.Origin.SetVec2(base.Mission.GetRandomPositionAroundPoint(worldFrame.Origin.GetNavMeshVec3(), 0.5f, 2f, false).AsVec2);
						Agent agent = this._missionAgentHandler.SpawnWanderingAgentWithInitialFrame(accompanyingCharacter.LocationCharacter, worldFrame.ToGroundMatrixFrame(), WeakGameEntity.Invalid, noHorse, false);
						if (gameEntity != null)
						{
							int disableFaceWithId = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>().DisableFaceWithId;
							if (disableFaceWithId != -1)
							{
								agent.SetAgentExcludeStateForFaceGroupId(disableFaceWithId, false);
							}
						}
						int num2 = 0;
						for (;;)
						{
							Agent agent2 = agent;
							Vec2 asVec = base.Mission.MainAgent.Position.AsVec2;
							if (agent2.CanMoveDirectlyToPosition(asVec) || num2 >= 50)
							{
								break;
							}
							worldFrame.Origin.SetVec2(base.Mission.GetRandomPositionAroundPoint(worldFrame.Origin.GetNavMeshVec3(), 0.5f, 4f, false).AsVec2);
							agent.TeleportToPosition(worldFrame.ToGroundMatrixFrame().origin);
							num2++;
						}
						agent.SetTeam(base.Mission.PlayerTeam, true);
						num++;
					}
					else
					{
						this._missionAgentHandler.SpawnWanderingAgent(accompanyingCharacter.LocationCharacter).SetTeam(base.Mission.PlayerTeam, true);
					}
					foreach (Agent agent3 in base.Mission.Agents)
					{
						LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(agent3.Origin);
						AccompanyingCharacter accompanyingCharacter2 = PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter);
						if (agent3.GetComponent<CampaignAgentComponent>().AgentNavigator != null && accompanyingCharacter2 != null)
						{
							DailyBehaviorGroup behaviorGroup = agent3.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
							if (accompanyingCharacter.IsFollowingPlayerAtMissionStart)
							{
								FollowAgentBehavior followAgentBehavior = behaviorGroup.GetBehavior<FollowAgentBehavior>() ?? behaviorGroup.AddBehavior<FollowAgentBehavior>();
								behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
								followAgentBehavior.SetTargetAgent(Agent.Main);
							}
							else
							{
								behaviorGroup.Behaviors.Clear();
							}
						}
					}
				}
			}
		}

		// Token: 0x060004FB RID: 1275 RVA: 0x000200E4 File Offset: 0x0001E2E4
		public MatrixFrame GetSpawnFrameOfPassage(Location location)
		{
			MatrixFrame result = MatrixFrame.Identity;
			UsableMachine usableMachine = this._missionAgentHandler.TownPassageProps.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location) ?? this._missionAgentHandler.DisabledPassages.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location);
			if (usableMachine != null)
			{
				MatrixFrame globalFrame = usableMachine.PilotStandingPoint.GameEntity.GetGlobalFrame();
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				globalFrame.origin.z = base.Mission.Scene.GetGroundHeightAtPosition(globalFrame.origin, BodyFlags.CommonCollisionExcludeFlags);
				globalFrame.rotation.RotateAboutUp(3.1415927f);
				result = globalFrame;
			}
			return result;
		}

		// Token: 0x04000290 RID: 656
		private readonly Location _previousLocation;

		// Token: 0x04000291 RID: 657
		private readonly Location _currentLocation;

		// Token: 0x04000292 RID: 658
		private MissionAgentHandler _missionAgentHandler;

		// Token: 0x04000293 RID: 659
		private readonly string _playerSpecialSpawnTag;

		// Token: 0x04000294 RID: 660
		private bool _noHorsesforCharactersAccompanyingPlayer;
	}
}
