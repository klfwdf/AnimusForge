using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Opens castle GCCZ as a non-campaign custom siege battle. This preserves the
/// post-victory settlement encounter while giving the scene the same deployment,
/// formation and agent capacity used by the troop-inspection battle pipeline.
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
				Side = BattleSideEnum.Attacker
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

			CustomBattleCombatant emptyDefenders = new CustomBattleCombatant(
				new TextObject("AnimusForge Castle Aftermath Empty Defenders"),
				settlement.Culture ?? playerCulture,
				playerBanner)
			{
				Side = BattleSideEnum.Defender
			};

			Mission mission = BannerlordMissions.OpenSiegeMissionWithDeployment(
				sceneName,
				playerCharacter,
				playerCombatant,
				emptyDefenders,
				true,
				wallRatios,
				false,
				new List<MissionSiegeWeapon>(),
				new List<MissionSiegeWeapon>(),
				true,
				wallLevel,
				"",
				false,
				false,
				6f);
			if (mission == null)
			{
				throw new InvalidOperationException("BannerlordMissions.OpenSiegeMissionWithDeployment returned null.");
			}

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
				+ " wallSections=" + wallRatios.Length + " breached=" + breachedSections);
			Logger.Log("CastleAftermath", "Opened castle aftermath with troop-inspection siege host. Host="
				+ SiegeCastleWarSceneProfile.RequiredMissionHostName
				+ ", Scene=" + sceneName + ", Levels=" + sceneLevels
				+ ", Allies=" + selectedAllies.TotalManCount
				+ ", Prisoners=" + CastleAftermathRuntimeBridge.SelectedPrisonerCount
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
}
