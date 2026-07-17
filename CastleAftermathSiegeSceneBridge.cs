using System;
using AnimusForge.SiegeAftermathIntervention;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Opens castle GCCZ through the troop-inspection battle lifecycle while loading
/// only the fortification's vanilla siege scene layer and persisted wall state.
/// No campaign siege battle, siege deployment, combat settlement or victory logic
/// is started by this bridge.
/// </summary>
internal static class CastleAftermathSiegeSceneBridge
{
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
			float[] wallRatios = SiegeCastleWarSceneProfile.NormalizeWallHitPointRatios(
				settlement.SettlementWallSectionHitPointsRatioList);
			if (string.IsNullOrWhiteSpace(sceneName) || wallRatios.Length == 0)
			{
				Logger.Log("CastleAftermath", "Castle siege scene context unavailable. Settlement="
					+ (settlement.StringId ?? "N/A") + ", Scene=" + (sceneName ?? "N/A")
					+ ", WallSections=" + wallRatios.Length);
				return false;
			}

			MissionInitializerRecord initializer = BuildInspectionMissionInitializer(
				sceneName,
				sceneLevels,
				wallLevel);
			if (!TroopInspectionBehavior.TryOpenPreparedExternalInspectionMission(
				initializer,
				out Mission mission,
				out string openError))
			{
				throw new InvalidOperationException("Troop-inspection mission host rejected castle scene: " + openError);
			}

			AttachCastleSceneBehaviors(mission, location, wallRatios);
			int breachedSections = SiegeCastleWarSceneProfile.CountBreachedWallSections(wallRatios);
			int alliedCount = SiegeAiInterventionBehavior.GetSelectedCastleInterventionRosterSnapshot().TotalManCount;
			GcczDiagnosticLog.Log("CastleSiegeScene", "open requested host="
				+ SiegeCastleWarSceneProfile.RequiredMissionHostName
				+ " settlement=" + (settlement.StringId ?? "N/A")
				+ " scene=" + sceneName + " levels=" + sceneLevels
				+ " allies=" + alliedCount
				+ " prisoners=" + CastleAftermathRuntimeBridge.SelectedPrisonerCount
				+ " wallSections=" + wallRatios.Length + " breached=" + breachedSections);
			Logger.Log("CastleAftermath", "Opened castle aftermath with troop-inspection mission lifecycle. Host="
				+ SiegeCastleWarSceneProfile.RequiredMissionHostName
				+ ", Scene=" + sceneName + ", Levels=" + sceneLevels
				+ ", Allies=" + alliedCount
				+ ", Prisoners=" + CastleAftermathRuntimeBridge.SelectedPrisonerCount
				+ ", WallSections=" + wallRatios.Length + ", Breached=" + breachedSections);
			return true;
		}
		catch (Exception ex)
		{
			TroopInspectionBehavior.CancelPreparedExternalInspectionRuntime("castle_scene_open_failed");
			GcczDiagnosticLog.Log("CastleSiegeScene", "open failed settlement="
				+ (settlement?.StringId ?? "N/A") + " source=" + (source ?? "N/A") + " error=" + ex);
			Logger.Log("CastleAftermath", "Open castle aftermath siege scene failed. Source="
				+ (source ?? "N/A") + ", Error=" + ex);
			return false;
		}
	}

	private static MissionInitializerRecord BuildInspectionMissionInitializer(
		string sceneName,
		string sceneLevels,
		int wallLevel)
	{
		MobileParty mainParty = MobileParty.MainParty;
		MissionInitializerRecord initializer = new MissionInitializerRecord(sceneName)
		{
			SceneLevels = sceneLevels,
			SceneUpgradeLevel = wallLevel,
			NeedsRandomTerrain = false,
			PlayingInCampaignMode = true,
			RandomTerrainSeed = MBRandom.RandomInt(10000),
			SceneHasMapPatch = false,
			DecalAtlasGroup = 2
		};
		if (mainParty != null)
		{
			initializer.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(mainParty.Position);
		}
		if (Campaign.Current?.Models?.DifficultyModel != null)
		{
			float friendlyDamage = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
			initializer.DamageToFriendsMultiplier = friendlyDamage;
			initializer.DamageFromPlayerToFriendsMultiplier = friendlyDamage;
		}
		return initializer;
	}

	private static void AttachCastleSceneBehaviors(Mission mission, Location location, float[] wallRatios)
	{
		CampaignMissionComponent campaignMission = mission.GetMissionBehavior<CampaignMissionComponent>();
		if (campaignMission == null)
		{
			campaignMission = new CampaignMissionComponent();
			mission.AddMissionBehavior(campaignMission);
		}
		campaignMission.Location = location;

		if (mission.GetMissionBehavior<SiegeMissionPreparationHandler>() == null)
		{
			mission.AddMissionBehavior(new SiegeMissionPreparationHandler(
				isSallyOut: false,
				isReliefForceAttack: false,
				wallHitPointPercentages: wallRatios,
				hasAnySiegeTower: false));
		}
		CastleAftermathRuntimeBridge.AttachMissionBehavior(mission);
		CastleAftermathDefensiveDeviceRuntimeBridge.AttachMissionBehavior(mission);
	}
}
