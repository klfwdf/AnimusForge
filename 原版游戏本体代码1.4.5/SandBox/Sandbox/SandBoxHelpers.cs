using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Objects;
using TaleWorlds.ObjectSystem;

namespace SandBox;

public static class SandBoxHelpers
{
	public static class MissionHelper
	{
		public static void FollowAgent(Agent agent, Agent target)
		{
			if (agent != null && target != null && agent.IsActive() && target.IsActive())
			{
				AgentBehaviorGroup activeBehaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetActiveBehaviorGroup();
				if (activeBehaviorGroup != null)
				{
					FollowAgentBehavior followAgentBehavior = activeBehaviorGroup.GetBehavior<FollowAgentBehavior>();
					if (followAgentBehavior == null)
					{
						followAgentBehavior = activeBehaviorGroup.AddBehavior<FollowAgentBehavior>();
					}
					activeBehaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
					followAgentBehavior.SetTargetAgent(target);
				}
			}
			else
			{
				Debug.FailedAssert("Cant follow agent", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\SandboxHelpers.cs", "FollowAgent", 45);
			}
		}

		public static void UnfollowAgent(Agent agent)
		{
			if (agent != null && agent.IsActive())
			{
				AgentBehaviorGroup activeBehaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetActiveBehaviorGroup();
				if (activeBehaviorGroup != null && activeBehaviorGroup.GetBehavior<FollowAgentBehavior>() != null)
				{
					activeBehaviorGroup.RemoveBehavior<FollowAgentBehavior>();
				}
			}
			else
			{
				Debug.FailedAssert("Cant unfollow agent", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\SandboxHelpers.cs", "UnfollowAgent", 66);
			}
		}

		public static void FadeOutAgents(IEnumerable<Agent> agents, bool hideInstantly, bool hideMount)
		{
			if (agents == null)
			{
				return;
			}
			Agent[] array = agents.ToArray();
			Agent[] array2 = array;
			foreach (Agent agent in array2)
			{
				if (!agent.IsMount)
				{
					agent.FadeOut(hideInstantly, hideMount);
				}
			}
			array2 = array;
			foreach (Agent agent2 in array2)
			{
				if (agent2.State != AgentState.Routed)
				{
					agent2.FadeOut(hideInstantly, hideMount);
				}
			}
		}

		public static void DisableGenericMissionEventScript(string triggeringObjectTag, GenericMissionEvent missionEvent)
		{
			foreach (ScriptComponentBehavior scriptComponent in Mission.Current.Scene.FindEntityWithTag(triggeringObjectTag).GetScriptComponents())
			{
				if (scriptComponent is GenericMissionEventScript genericMissionEventScript && genericMissionEventScript.EventId.Equals(missionEvent.EventId) && genericMissionEventScript.Parameter.Equals(missionEvent.Parameter))
				{
					genericMissionEventScript.IsDisabled = true;
				}
			}
		}

		public static void SpawnPlayer(bool civilianEquipment = false, bool noHorses = false, bool noWeapon = false, bool wieldInitialWeapons = false, string spawnTag = "")
		{
			GameEntity gameEntity = null;
			gameEntity = (string.IsNullOrEmpty(spawnTag) ? Mission.Current.Scene.FindEntityWithTag("spawnpoint_player") : Mission.Current.Scene.FindEntityWithTag(spawnTag));
			SpawnPlayer(gameEntity, civilianEquipment, noHorses, noWeapon, wieldInitialWeapons);
		}

		public static void SpawnPlayer(GameEntity spawnPosition, bool civilianEquipment = false, bool noHorses = false, bool noWeapon = false, bool wieldInitialWeapons = false)
		{
			if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
			{
				civilianEquipment = false;
			}
			MatrixFrame spawnFrame = MatrixFrame.Identity;
			if (spawnPosition != null)
			{
				spawnFrame = spawnPosition.GetGlobalFrame();
				spawnFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			}
			CampaignEventDispatcher.Instance.OnBeforePlayerAgentSpawn(ref spawnFrame);
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
			AgentBuildData agentBuildData = new AgentBuildData(playerCharacter).Team(Mission.Current.PlayerTeam).InitialPosition(in spawnFrame.origin).InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized())
				.CivilianEquipment(civilianEquipment)
				.NoHorses(noHorses)
				.NoWeapons(noWeapon)
				.ClothingColor1(Mission.Current.PlayerTeam.Color)
				.ClothingColor2(Mission.Current.PlayerTeam.Color2)
				.TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter))
				.MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed()))
				.Controller(AgentControllerType.Player);
			Debug.Print($"Spawn position: {spawnFrame.origin}");
			if (playerCharacter.HeroObject?.ClanBanner != null)
			{
				agentBuildData.Banner(playerCharacter.HeroObject.ClanBanner);
			}
			if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
			{
				agentBuildData.TroopOrigin(new SimpleAgentOrigin(CharacterObject.PlayerCharacter));
			}
			if (Campaign.Current.IsMainHeroDisguised)
			{
				MBEquipmentRoster mBEquipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("npc_disguised_hero_equipment_template");
				agentBuildData.Equipment(mBEquipmentRoster.DefaultEquipment);
			}
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			if (wieldInitialWeapons)
			{
				agent.WieldInitialWeapons();
			}
			CampaignEventDispatcher.Instance.OnPlayerAgentSpawned();
			if (spawnPosition != null)
			{
				string[] tags = spawnPosition.Tags;
				foreach (string tag in tags)
				{
					agent.AgentVisuals.GetEntity().AddTag(tag);
				}
			}
			for (int j = 0; j < 3; j++)
			{
				Agent.Main.AgentVisuals.GetSkeleton().TickAnimations(0.1f, Agent.Main.AgentVisuals.GetGlobalFrame(), tickAnimsForChildren: true);
			}
		}

		public static List<Agent> SpawnHorses()
		{
			List<Agent> list = new List<Agent>();
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_horse"))
			{
				MatrixFrame globalFrame = item.GetGlobalFrame();
				string objectName = item.Tags[1];
				ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(objectName);
				ItemRosterElement rosterElement = new ItemRosterElement(itemObject, 1);
				ItemRosterElement harnessRosterElement = default(ItemRosterElement);
				if (itemObject.HasHorseComponent)
				{
					globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					Agent agent = Mission.Current.SpawnMonster(rosterElement, harnessRosterElement, in globalFrame.origin, globalFrame.rotation.f.AsVec2);
					AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(item, agent);
					SimulateAnimalAnimations(agent);
					list.Add(agent);
				}
			}
			return list;
		}

		public static void SpawnSheeps()
		{
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_sheep"))
			{
				MatrixFrame globalFrame = item.GetGlobalFrame();
				ItemRosterElement rosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("sheep"));
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Agent agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in globalFrame.origin, globalFrame.rotation.f.AsVec2);
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("navigation_mesh_deactivator");
				if (gameEntity != null)
				{
					NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithId, isExcluded: true);
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithIdForAnimals, isExcluded: true);
				}
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(item, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnCows()
		{
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_cow"))
			{
				MatrixFrame globalFrame = item.GetGlobalFrame();
				ItemRosterElement rosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("cow"));
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Agent agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in globalFrame.origin, globalFrame.rotation.f.AsVec2);
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("navigation_mesh_deactivator");
				if (gameEntity != null)
				{
					NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithId, isExcluded: true);
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithIdForAnimals, isExcluded: true);
				}
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(item, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnGeese()
		{
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_goose"))
			{
				MatrixFrame globalFrame = item.GetGlobalFrame();
				ItemRosterElement rosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("goose"));
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Agent agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in globalFrame.origin, globalFrame.rotation.f.AsVec2);
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("navigation_mesh_deactivator");
				if (gameEntity != null)
				{
					NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithId, isExcluded: true);
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithIdForAnimals, isExcluded: true);
				}
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(item, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnChicken()
		{
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_chicken"))
			{
				MatrixFrame globalFrame = item.GetGlobalFrame();
				ItemRosterElement rosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("chicken"));
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Agent agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in globalFrame.origin, globalFrame.rotation.f.AsVec2);
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("navigation_mesh_deactivator");
				if (gameEntity != null)
				{
					NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithId, isExcluded: true);
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithIdForAnimals, isExcluded: true);
				}
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(item, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnHogs()
		{
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_hog"))
			{
				MatrixFrame globalFrame = item.GetGlobalFrame();
				ItemRosterElement rosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("hog"));
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Agent agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in globalFrame.origin, globalFrame.rotation.f.AsVec2);
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("navigation_mesh_deactivator");
				if (gameEntity != null)
				{
					NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithId, isExcluded: true);
					agent.SetAgentExcludeStateForFaceGroupId(firstScriptOfType.DisableFaceWithIdForAnimals, isExcluded: true);
				}
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(item, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		private static void SimulateAnimalAnimations(Agent agent)
		{
			int num = 10 + MBRandom.RandomInt(90);
			for (int i = 0; i < num; i++)
			{
				agent.TickActionChannels(0.1f);
				Vec3 vec = agent.ComputeAnimationDisplacement(0.1f);
				if (vec.LengthSquared > 0f)
				{
					agent.TeleportToPosition(agent.Position + vec);
				}
				agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), tickAnimsForChildren: true);
			}
		}
	}

	public static class MapSceneHelper
	{
		public static bool[] GetRegionMapping(PartyNavigationModel model)
		{
			TerrainType[] obj = (TerrainType[])Enum.GetValues(typeof(TerrainType));
			bool[] array = new bool[obj.Max((TerrainType v) => (int)v) + 1];
			TerrainType[] array2 = obj;
			foreach (TerrainType terrainType in array2)
			{
				array[(int)terrainType] = model.IsTerrainTypeValidForNavigationType(terrainType, MobileParty.NavigationType.Default);
			}
			return array;
		}
	}
}
