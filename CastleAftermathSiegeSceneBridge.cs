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
	private static readonly HashSet<string> IncompatibleNativeBattleBehaviors = new HashSet<string>(StringComparer.Ordinal)
	{
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
		"AgentMoraleInteractionLogic"
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

			int removedNativeBehaviors = RemoveIncompatibleNativeBattleBehaviors(mission);
			mission.AddMissionBehavior(new CastleAftermathPlayerRosterMissionBehavior(selectedAllies));
			CampaignMissionComponent campaignMission = mission.GetMissionBehavior<CampaignMissionComponent>();
			if (campaignMission == null)
			{
				campaignMission = new CampaignMissionComponent();
				mission.AddMissionBehavior(campaignMission);
			}
			campaignMission.Location = location;
			CastleAftermathRuntimeBridge.AttachMissionBehavior(mission);
			int breachedSections = SiegeCastleWarSceneProfile.CountBreachedWallSections(wallRatios);
			GcczDiagnosticLog.Log("CastleSiegeScene", "open requested host=" + SiegeCastleWarSceneProfile.RequiredMissionHostName
				+ " settlement=" + (settlement.StringId ?? "N/A")
				+ " scene=" + sceneName + " levels=" + sceneLevels
				+ " allies=" + selectedAllies.TotalManCount
				+ " prisoners=" + CastleAftermathRuntimeBridge.SelectedPrisonerCount
				+ " removedNativeBehaviors=" + removedNativeBehaviors
				+ " wallSections=" + wallRatios.Length + " breached=" + breachedSections);
			Logger.Log("CastleAftermath", "Opened castle aftermath with troop-inspection siege host. Host="
				+ SiegeCastleWarSceneProfile.RequiredMissionHostName
				+ ", Scene=" + sceneName + ", Levels=" + sceneLevels
				+ ", Allies=" + selectedAllies.TotalManCount
				+ ", Prisoners=" + CastleAftermathRuntimeBridge.SelectedPrisonerCount
				+ ", RemovedNativeBehaviors=" + removedNativeBehaviors
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

	private static int RemoveIncompatibleNativeBattleBehaviors(Mission mission)
	{
		if (mission?.MissionBehaviors == null)
		{
			return 0;
		}

		List<MissionBehavior> remove = mission.MissionBehaviors
			.Where(behavior => behavior != null && IncompatibleNativeBattleBehaviors.Contains(behavior.GetType().Name))
			.ToList();
		foreach (MissionBehavior behavior in remove)
		{
			mission.RemoveMissionBehavior(behavior);
		}
		return remove.Count;
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
		if (_spawned)
		{
			return;
		}
		_spawned = true;
		SpawnPlayerAndAllies();
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

		Agent player = BannerlordApiCompat.SpawnPlayerSideTroop(
			mission,
			new PartyAgentOrigin(party, playerCharacter),
			1,
			0,
			FormationClass.Infantry,
			hasFormation: false,
			spawnWithHorse: false,
			wieldInitialWeapons: true,
			useTroopClassForSpawn: false);
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
		foreach (CharacterObject character in allies)
		{
			FormationClass formationClass = ResolveFormationClass(character);
			formationIndexes.TryGetValue(formationClass, out int formationIndex);
			Agent agent = BannerlordApiCompat.SpawnPlayerSideTroop(
				mission,
				new PartyAgentOrigin(party, character),
				formationCounts[formationClass],
				formationIndex,
				formationClass,
				hasFormation: true,
				spawnWithHorse: false,
				wieldInitialWeapons: true,
				useTroopClassForSpawn: false);
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

		mission.SetMissionMode(MissionMode.Battle, atStart: true);
		bool commandUiReady = SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(
			mission,
			"castle_aftermath_manual_roster_spawn");
		Logger.Log("CastleAftermath", "Castle player roster spawned with campaign-safe origins. Main="
			+ (player?.Character?.StringId ?? "null")
			+ ", MainActive=" + (player?.IsActive() == true)
			+ ", Allies=" + spawnedAllies + "/" + allies.Count
			+ ", Team=" + playerTeam.Side
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
