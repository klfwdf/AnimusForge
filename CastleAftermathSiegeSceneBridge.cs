using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Opens castle GCCZ on the non-campaign custom siege host. Native combat spawning,
/// deployment and victory behaviors are replaced with campaign-safe player-side
/// origins while the siege scene layer, wall damage and full agent capacity remain.
/// </summary>
internal static class CastleAftermathSiegeSceneBridge
{
	private static readonly HashSet<string> IncompatibleNativeMissionBehaviors = new HashSet<string>(StringComparer.Ordinal)
	{
		"MissionAgentSpawnLogic",
		"DefaultBattleMissionAgentSpawnLogic",
		"BattleReinforcementsSpawnController",
		"CustomSiegeMissionSpawnHandler",
		"SiegeDeploymentHandler",
		"SiegeDeploymentMissionController",
		"BattleObserverMissionLogic",
		"AgentVictoryLogic",
		"CustomBattleAgentLogic",
		"BannerBearerLogic",
		"BattlePowerCalculationLogic",
		"AssignPlayerRoleInTeamMissionController",
		"GeneralsAndCaptainsAssignmentLogic",
		"MissionAgentPanicHandler",
		"AgentMoraleInteractionLogic",
		"MissionGauntletOrderOfBattleUIHandler",
		"MissionOrderOfBattleUIHandler",
		"DeploymentMissionView",
		"MissionDeploymentBoundaryMarker",
		"MissionEntitySelectionUIHandler",
		"MusicBattleMissionView",
		"MissionPreloadView",
		"MissionCampaignBattleSpectatorView"
	};

	internal static bool TryOpenMission(Settlement settlement, Location location, string source)
	{
		if (settlement?.IsCastle != true
			|| location == null
			|| !string.Equals(location.StringId, SiegeCastleWarSceneProfile.CenterLocationId, StringComparison.OrdinalIgnoreCase)
			|| settlement.Town == null
			|| Campaign.Current?.Models?.LocationModel == null)
		{
			return false;
		}

		try
		{
			int wallLevel = settlement.Town.GetWallLevel();
			string sceneName = location.GetSceneName(wallLevel);
			string upgradeLevelTag = Campaign.Current.Models.LocationModel.GetUpgradeLevelTag(wallLevel);
			string sceneLevels = SiegeCastleWarSceneProfile.BuildSceneLevels(upgradeLevelTag);
			float[] wallRatios = SiegeCastleWarSceneProfile.NormalizeWallHitPointRatios(settlement.SettlementWallSectionHitPointsRatioList);
			if (string.IsNullOrWhiteSpace(sceneName) || wallRatios.Length == 0)
			{
				Logger.Log("CastleAftermath", "Castle siege scene context unavailable. Settlement=" + (settlement.StringId ?? "N/A")
					+ ", Scene=" + (sceneName ?? "N/A") + ", WallSections=" + wallRatios.Length);
				return false;
			}

			CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
			BasicCultureObject playerCulture = playerCharacter?.Culture ?? settlement.Culture;
			Banner playerBanner = Clan.PlayerClan?.Banner ?? settlement.OwnerClan?.Banner;
			if (playerCharacter == null || playerCulture == null || playerBanner == null)
			{
				throw new InvalidOperationException("Castle custom siege combatant context is unavailable.");
			}

			CustomBattleCombatant playerCombatant = new CustomBattleCombatant(
				new TextObject("AnimusForge Castle Aftermath"),
				playerCulture,
				playerBanner)
			{
				Side = BattleSideEnum.Defender
			};
			playerCombatant.AddCharacter(playerCharacter, 1);
			playerCombatant.SetGeneral(playerCharacter);

			TroopRoster selectedAllies = SiegeAiInterventionBehavior.GetSelectedCastleInterventionRosterSnapshot();
			foreach (TroopRosterElement element in selectedAllies.GetTroopRoster())
			{
				CharacterObject character = element.Character;
				if (character == null || character.IsPlayerCharacter || element.Number <= 0)
				{
					continue;
				}
				playerCombatant.AddCharacter(character, element.Number);
			}

			CustomBattleCombatant emptyAttackers = new CustomBattleCombatant(
				new TextObject("AnimusForge Castle Aftermath Empty Attackers"),
				settlement.Culture ?? playerCulture,
				playerBanner)
			{
				Side = BattleSideEnum.Attacker
			};

			Mission mission = BannerlordMissions.OpenSiegeMissionWithDeployment(
				sceneName,
				playerCharacter,
				playerCombatant,
				emptyAttackers,
				true,
				wallRatios,
				false,
				new List<MissionSiegeWeapon>(),
				new List<MissionSiegeWeapon>(),
				false,
				wallLevel,
				"",
				false,
				false,
				6f);
			if (mission == null)
			{
				throw new InvalidOperationException("BannerlordMissions.OpenSiegeMissionWithDeployment returned null.");
			}

			int removedNativeMissionBehaviors = RemoveIncompatibleNativeMissionBehaviors(mission);
			mission.AddMissionBehavior(new CastleAftermathPlayerRosterMissionBehavior(selectedAllies));
			CampaignMissionComponent campaignMission = mission.GetMissionBehavior<CampaignMissionComponent>();
			if (campaignMission == null)
			{
				campaignMission = new CampaignMissionComponent();
				mission.AddMissionBehavior(campaignMission);
			}
			campaignMission.Location = location;
			CastleAftermathRuntimeBridge.AttachMissionBehavior(mission, SpawnCastlePrisonerAgent);
			int breachedSections = SiegeCastleWarSceneProfile.CountBreachedWallSections(wallRatios);
			GcczDiagnosticLog.Log("CastleSiegeScene", "open requested host=" + SiegeCastleWarSceneProfile.RequiredMissionHostName
				+ " settlement=" + (settlement.StringId ?? "N/A")
				+ " scene=" + sceneName + " levels=" + sceneLevels
				+ " allies=" + selectedAllies.TotalManCount
				+ " prisoners=" + CastleAftermathRuntimeBridge.SelectedPrisonerCount
				+ " removedNativeMissionBehaviors=" + removedNativeMissionBehaviors
				+ " wallSections=" + wallRatios.Length + " breached=" + breachedSections);
			Logger.Log("CastleAftermath", "Opened castle aftermath with troop-inspection siege host. Host="
				+ SiegeCastleWarSceneProfile.RequiredMissionHostName
				+ ", Scene=" + sceneName + ", Levels=" + sceneLevels
				+ ", Allies=" + selectedAllies.TotalManCount
				+ ", Prisoners=" + CastleAftermathRuntimeBridge.SelectedPrisonerCount
				+ ", RemovedNativeMissionBehaviors=" + removedNativeMissionBehaviors
				+ ", WallSections=" + wallRatios.Length + ", Breached=" + breachedSections);
			return true;
		}
		catch (Exception ex)
		{
			GcczDiagnosticLog.Log("CastleSiegeScene", "open failed settlement=" + (settlement?.StringId ?? "N/A")
				+ " source=" + (source ?? "N/A") + " error=" + ex);
			Logger.Log("CastleAftermath", "Open castle aftermath siege scene failed. Source=" + (source ?? "N/A") + ", Error=" + ex);
			return false;
		}
	}

	private static int RemoveIncompatibleNativeMissionBehaviors(Mission mission)
	{
		if (mission?.MissionBehaviors == null)
		{
			return 0;
		}

		List<MissionBehavior> remove = mission.MissionBehaviors
			.Where(behavior => behavior != null && IncompatibleNativeMissionBehaviors.Contains(behavior.GetType().Name))
			.ToList();
		foreach (MissionBehavior behavior in remove)
		{
			mission.RemoveMissionBehavior(behavior);
		}
		Logger.Log("CastleAftermath", "Removed incompatible native siege behaviors/views. Count="
			+ remove.Count + ", Types=" + string.Join(",", remove.Select(behavior => behavior.GetType().Name)));
		return remove.Count;
	}

	internal static Agent SpawnCastlePrisonerAgent(
		Mission mission,
		IAgentOriginBase origin,
		int formationTroopCount,
		int formationTroopIndex,
		FormationClass formationClass)
	{
		CharacterObject character = origin?.Troop as CharacterObject;
		Team team = mission?.PlayerTeam;
		if (character == null || team == null
			|| !TryResolveCastleSpawnBasis(mission, out Vec3 anchor, out Vec3 forward, out Vec3 right))
		{
			return null;
		}

		bool isLord = (int)formationClass == SiegeCastleRosterSelectionProfile.LordPrisonerFormationIndex;
		int columns = isLord
			? SiegeCastleRosterSelectionProfile.LordPrisonerSpawnGridColumns
			: SiegeCastleRosterSelectionProfile.RegularPrisonerSpawnGridColumns;
		float lateralOffset = isLord
			? SiegeCastleRosterSelectionProfile.LordPrisonerSpawnLateralOffset
			: SiegeCastleRosterSelectionProfile.RegularPrisonerSpawnLateralOffset;
		Vec3 position = BuildGridPosition(
			anchor,
			forward,
			right,
			formationTroopIndex,
			columns,
			SiegeCastleRosterSelectionProfile.PrisonerSpawnStartDepth,
			SiegeCastleRosterSelectionProfile.PrisonerSpawnRowSpacing,
			SiegeCastleRosterSelectionProfile.PrisonerSpawnLateralSpacing,
			lateralOffset);
		position = ProjectSpawnPositionToNavigationMesh(mission, position, anchor);
		return SpawnCampaignAgent(
			mission,
			character,
			origin,
			team,
			formationClass,
			formationTroopCount,
			formationTroopIndex,
			position,
			forward.AsVec2,
			isPlayer: false,
			wieldInitialWeapons: false);
	}

	internal static Agent SpawnCampaignAgent(
		Mission mission,
		CharacterObject character,
		IAgentOriginBase origin,
		Team team,
		FormationClass formationClass,
		int formationTroopCount,
		int formationTroopIndex,
		Vec3 position,
		Vec2 direction,
		bool isPlayer,
		bool wieldInitialWeapons)
	{
		if (mission == null || character == null || origin == null || team == null)
		{
			return null;
		}
		try
		{
			AgentBuildData buildData = new AgentBuildData(character)
				.TroopOrigin(origin)
				.Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement"))
				.Team(team)
				.InitialPosition(in position)
				.InitialDirection(direction.Normalized())
				.Controller(isPlayer ? AgentControllerType.Player : AgentControllerType.AI)
				.CivilianEquipment(false)
				.NoHorses(true);
			if (!isPlayer)
			{
				Formation formation = team.GetFormation(formationClass);
				if (formation != null)
				{
					buildData = buildData
						.Formation(formation)
						.FormationTroopSpawnCount(Math.Max(1, formationTroopCount))
						.FormationTroopSpawnIndex(Math.Max(0, formationTroopIndex))
						.SpawnsIntoOwnFormation(true)
						.SpawnsUsingOwnTroopClass(false);
				}
			}

			Agent agent = mission.SpawnAgent(buildData, false);
			if (agent?.Character?.IsHero == true)
			{
				agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.IsUnique);
			}
			if (agent != null && wieldInitialWeapons)
			{
				agent.WieldInitialWeapons();
			}
			return agent;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Explicit castle agent spawn failed. Character="
				+ (character.StringId ?? "N/A")
				+ ", IsPlayer=" + isPlayer
				+ ", Formation=" + formationClass
				+ ", Position=" + position
				+ ", Error=" + ex);
			return null;
		}
	}

	internal static bool TryResolveCastleSpawnBasis(
		Mission mission,
		out Vec3 anchor,
		out Vec3 forward,
		out Vec3 right)
	{
		anchor = Vec3.Zero;
		forward = Vec3.Forward;
		right = Vec3.Side;
		try
		{
			GameEntity entity = mission?.Scene?.FindEntityWithTag(SiegeCastleRosterSelectionProfile.DefenderSpawnAnchorTag);
			if (entity == null)
			{
				Logger.Log("CastleAftermath", "Castle defender spawn anchor is missing. Tag="
					+ SiegeCastleRosterSelectionProfile.DefenderSpawnAnchorTag);
				return false;
			}

			MatrixFrame frame = entity.GetGlobalFrame();
			anchor = frame.origin;
			forward = frame.rotation.f;
			forward.z = 0f;
			if (forward.LengthSquared < 0.01f)
			{
				forward = Vec3.Forward;
			}
			forward.Normalize();
			right = Vec3.CrossProduct(forward, Vec3.Up);
			if (right.LengthSquared < 0.01f)
			{
				right = Vec3.Side;
			}
			right.Normalize();
			anchor = ProjectSpawnPositionToNavigationMesh(mission, anchor, anchor);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Resolve castle defender spawn anchor failed: " + ex);
			return false;
		}
	}

	internal static Vec3 BuildGridPosition(
		Vec3 anchor,
		Vec3 forward,
		Vec3 right,
		int index,
		int columns,
		float startDepth,
		float rowSpacing,
		float lateralSpacing,
		float lateralOffset)
	{
		int safeColumns = Math.Max(1, columns);
		int safeIndex = Math.Max(0, index);
		int row = safeIndex / safeColumns;
		int column = safeIndex % safeColumns;
		float lateral = (column - (safeColumns - 1) * 0.5f) * lateralSpacing + lateralOffset;
		float depth = startDepth + row * rowSpacing;
		return anchor - forward * depth + right * lateral;
	}

	internal static Vec3 ProjectSpawnPositionToNavigationMesh(Mission mission, Vec3 candidate, Vec3 fallback)
	{
		Scene scene = mission?.Scene;
		if (scene == null)
		{
			return fallback;
		}
		try
		{
			candidate.z = scene.GetGroundHeightAtPosition(candidate, BodyFlags.CommonCollisionExcludeFlags);
			WorldPosition world = new WorldPosition(scene, candidate);
			if (world.GetNearestNavMesh() != UIntPtr.Zero)
			{
				return world.GetNavMeshVec3();
			}
		}
		catch
		{
		}
		try
		{
			fallback.z = scene.GetGroundHeightAtPosition(fallback, BodyFlags.CommonCollisionExcludeFlags);
		}
		catch
		{
		}
		return fallback;
	}
}

internal sealed class CastleAftermathPlayerRosterMissionBehavior : MissionLogic
{
	private readonly TroopRoster _selectedAllies;
	private bool _spawned;

	internal CastleAftermathPlayerRosterMissionBehavior(TroopRoster selectedAllies)
	{
		_selectedAllies = selectedAllies ?? TroopRoster.CreateDummyTroopRoster();
	}

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override void AfterStart()
	{
		base.AfterStart();
		Logger.Log("CastleAftermath", "Castle player roster waiting for first mission tick before spawn. Mode="
			+ (base.Mission?.Mode.ToString() ?? "null"));
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		Mission mission = base.Mission;
		if (_spawned || mission == null || mission.IsMissionEnding
			|| mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
		{
			return;
		}
		if (!TryEnterNonCombatStartupMode(mission))
		{
			return;
		}
		_spawned = true;
		SpawnPlayerAndAllies();
	}

	private static bool TryEnterNonCombatStartupMode(Mission mission)
	{
		try
		{
			if (mission == null)
			{
				return false;
			}
			MissionMode previousMode = mission.Mode;
			if (previousMode == MissionMode.Battle)
			{
				mission.SetMissionMode(MissionMode.StartUp, atStart: false);
				Logger.Log("CastleAftermath", "Castle mission switched to non-combat startup mode before agent spawn. PreviousMode="
					+ previousMode + ", CurrentMode=" + mission.Mode + ", Agents=" + (mission.Agents?.Count ?? 0));
			}
			return mission.Mode == MissionMode.StartUp;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Castle mission startup-mode preparation failed: " + ex);
			return false;
		}
	}

	private void SpawnPlayerAndAllies()
	{
		Mission mission = base.Mission;
		PartyBase party = PartyBase.MainParty;
		CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
		Team playerTeam = mission?.PlayerTeam;
		if (mission == null || party == null || playerCharacter == null || playerTeam == null)
		{
			Logger.Log("CastleAftermath", "Castle player roster spawn aborted: mission context unavailable.");
			return;
		}
		if (!CastleAftermathSiegeSceneBridge.TryResolveCastleSpawnBasis(
			mission,
			out Vec3 anchor,
			out Vec3 forward,
			out Vec3 right))
		{
			Logger.Log("CastleAftermath", "Castle player roster spawn aborted: defender spawn anchor unavailable.");
			return;
		}

		Vec3 playerPosition = anchor + forward * SiegeCastleRosterSelectionProfile.PlayerSpawnForwardOffset;
		playerPosition = CastleAftermathSiegeSceneBridge.ProjectSpawnPositionToNavigationMesh(
			mission,
			playerPosition,
			anchor);
		Agent player = CastleAftermathSiegeSceneBridge.SpawnCampaignAgent(
			mission,
			playerCharacter,
			new PartyAgentOrigin(party, playerCharacter),
			playerTeam,
			FormationClass.Infantry,
			1,
			0,
			playerPosition,
			forward.AsVec2,
			isPlayer: true,
			wieldInitialWeapons: true);
		if (player != null)
		{
			player.SetMortalityState(Agent.MortalityState.Immortal);
		}

		List<CharacterObject> allies = ExpandSelectedAllies();
		Dictionary<FormationClass, int> formationCounts = allies
			.GroupBy(ResolveFormationClass)
			.ToDictionary(group => group.Key, group => group.Count());
		Dictionary<FormationClass, int> formationIndexes = new Dictionary<FormationClass, int>();
		int spawnedAllies = 0;
		for (int allyIndex = 0; allyIndex < allies.Count; allyIndex++)
		{
			CharacterObject character = allies[allyIndex];
			FormationClass formationClass = ResolveFormationClass(character);
			formationIndexes.TryGetValue(formationClass, out int formationIndex);
			Vec3 allyPosition = CastleAftermathSiegeSceneBridge.BuildGridPosition(
				anchor,
				forward,
				right,
				allyIndex,
				SiegeCastleRosterSelectionProfile.AlliedSpawnGridColumns,
				SiegeCastleRosterSelectionProfile.AlliedSpawnStartDepth,
				SiegeCastleRosterSelectionProfile.AlliedSpawnRowSpacing,
				SiegeCastleRosterSelectionProfile.AlliedSpawnLateralSpacing,
				0f);
			allyPosition = CastleAftermathSiegeSceneBridge.ProjectSpawnPositionToNavigationMesh(
				mission,
				allyPosition,
				anchor);
			Agent agent = CastleAftermathSiegeSceneBridge.SpawnCampaignAgent(
				mission,
				character,
				new PartyAgentOrigin(party, character),
				playerTeam,
				formationClass,
				formationCounts[formationClass],
				formationIndex,
				allyPosition,
				forward.AsVec2,
				isPlayer: false,
				wieldInitialWeapons: true);
			formationIndexes[formationClass] = formationIndex + 1;
			if (agent == null)
			{
				continue;
			}
			agent.SetMortalityState(Agent.MortalityState.Immortal);
			agent.SetWatchState(Agent.WatchState.Patrolling);
			SiegeAiInterventionBehavior.EnsureAgentPlayerCommandableForExternal(agent, "castle_aftermath_manual_ally_spawn");
			spawnedAllies++;
		}

		bool commandUiReady = SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(
			mission,
			"castle_aftermath_manual_roster_spawn");
		Logger.Log("CastleAftermath", "Castle player roster spawned with campaign-safe origins. Main="
			+ (player?.Character?.StringId ?? "null")
			+ ", MainActive=" + (player?.IsActive() == true)
			+ ", Allies=" + spawnedAllies + "/" + allies.Count
			+ ", Team=" + playerTeam.Side
			+ ", SpawnAnchor=" + anchor
			+ ", Mode=" + mission.Mode
			+ ", MissionAgents=" + (mission.Agents?.Count ?? 0)
			+ ", CommandUiReady=" + commandUiReady);
	}

	private List<CharacterObject> ExpandSelectedAllies()
	{
		List<CharacterObject> result = new List<CharacterObject>();
		foreach (TroopRosterElement element in _selectedAllies.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			if (character == null || character.IsPlayerCharacter || element.Number <= 0)
			{
				continue;
			}
			for (int i = 0; i < element.Number; i++)
			{
				result.Add(character);
			}
		}
		return result;
	}

	private static FormationClass ResolveFormationClass(CharacterObject character)
	{
		FormationClass formationClass = character?.DefaultFormationClass ?? FormationClass.Infantry;
		switch (formationClass)
		{
			case FormationClass.Infantry:
			case FormationClass.Ranged:
			case FormationClass.Cavalry:
			case FormationClass.HorseArcher:
				return formationClass;
			default:
				return FormationClass.Infantry;
		}
	}
}
