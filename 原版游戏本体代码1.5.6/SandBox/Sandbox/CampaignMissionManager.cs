using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox;

public class CampaignMissionManager : CampaignMission.ICampaignMissionManager
{
	IMission CampaignMission.ICampaignMissionManager.OpenSiegeMissionWithDeployment(string scene, float[] wallHitPointsPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int upgradeLevel, bool isSallyOut, bool isReliefForceAttack)
	{
		return SandBoxMissions.OpenSiegeMissionWithDeployment(scene, wallHitPointsPercentages, hasAnySiegeTower, siegeWeaponsOfAttackers, siegeWeaponsOfDefenders, isPlayerAttacker, upgradeLevel, isSallyOut, isReliefForceAttack);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenSiegeMissionNoDeployment(string scene, bool isSallyOut, bool isReliefForceAttack)
	{
		return SandBoxMissions.OpenSiegeMissionNoDeployment(scene, isSallyOut, isReliefForceAttack);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenSiegeLordsHallFightMission(string scene, FlattenedTroopRoster attackerPriorityList)
	{
		return SandBoxMissions.OpenSiegeLordsHallFightMission(scene, attackerPriorityList);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenBattleMission(MissionInitializerRecord rec)
	{
		return SandBoxMissions.OpenBattleMission(rec);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenCaravanBattleMission(MissionInitializerRecord rec, bool isCaravan)
	{
		return SandBoxMissions.OpenCaravanBattleMission(rec, isCaravan);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenBattleMission(string scene, bool usesTownDecalAtlas, string sceneLevels)
	{
		return SandBoxMissions.OpenBattleMission(scene, usesTownDecalAtlas, sceneLevels);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenAlleyFightMission(string scene, int upgradeLevel, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
	{
		return SandBoxMissions.OpenAlleyFightMission(scene, upgradeLevel, location, playerSideTroops, rivalSideTroops);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenCombatMissionWithDialogue(string scene, CharacterObject characterToTalkTo, int upgradeLevel)
	{
		return SandBoxMissions.OpenCombatMissionWithDialogue(scene, characterToTalkTo, upgradeLevel);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenBattleMissionWhileEnteringSettlement(string scene, int upgradeLevel, int numberOfMaxTroopToBeSpawnedForPlayer, int numberOfMaxTroopToBeSpawnedForOpponent)
	{
		return SandBoxMissions.OpenBattleMissionWhileEnteringSettlement(scene, upgradeLevel, numberOfMaxTroopToBeSpawnedForPlayer, numberOfMaxTroopToBeSpawnedForOpponent);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenHideoutBattleMission(string scene, FlattenedTroopRoster playerTroops, bool isTutorial)
	{
		return SandBoxMissions.OpenHideoutBattleMission(scene, playerTroops, isTutorial);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenTownCenterMission(string scene, int townUpgradeLevel, Location location, CharacterObject talkToChar, string playerSpawnTag)
	{
		return SandBoxMissions.OpenTownCenterMission(scene, townUpgradeLevel, location, talkToChar, playerSpawnTag);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenCastleCourtyardMission(string scene, int castleUpgradeLevel, Location location, CharacterObject talkToChar)
	{
		return SandBoxMissions.OpenCastleCourtyardMission(scene, castleUpgradeLevel, location, talkToChar);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenVillageMission(string scene, Location location, CharacterObject talkToChar)
	{
		return SandBoxMissions.OpenVillageMission(scene, location, talkToChar);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenIndoorMission(string scene, int upgradeLevel, Location location, CharacterObject talkToChar)
	{
		return SandBoxMissions.OpenIndoorMission(scene, upgradeLevel, location, talkToChar);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenPrisonBreakMission(string scene, Location location, CharacterObject prisonerCharacter)
	{
		return SandBoxMissions.OpenPrisonBreakMission(scene, location, prisonerCharacter);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenArenaStartMission(string scene, Location location, CharacterObject talkToChar)
	{
		return SandBoxMissions.OpenArenaStartMission(scene, location, talkToChar);
	}

	public IMission OpenArenaDuelMission(string scene, Location location, CharacterObject duelCharacter, bool requireCivilianEquipment, bool spawnBOthSidesWithHorse, Action<CharacterObject> onDuelEndAction, float customAgentHealth)
	{
		return SandBoxMissions.OpenArenaDuelMission(scene, location, duelCharacter, requireCivilianEquipment, spawnBOthSidesWithHorse, onDuelEndAction, customAgentHealth);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenConversationMission(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, string specialScene, string sceneLevels, bool isMultiAgentConversation)
	{
		return SandBoxMissions.OpenConversationMission(playerCharacterData, conversationPartnerData, specialScene, sceneLevels, isMultiAgentConversation);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenMeetingMission(string scene, CharacterObject character)
	{
		return SandBoxMissions.OpenMeetingMission(scene, character);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenRetirementMission(string scene, Location location, CharacterObject talkToChar, string sceneLevels, string unconsciousMenuId)
	{
		return SandBoxMissions.OpenRetirementMission(scene, location, talkToChar, sceneLevels, unconsciousMenuId);
	}

	IMission CampaignMission.ICampaignMissionManager.OpenHideoutAmbushMission(string sceneName, FlattenedTroopRoster playerTroops, Location location)
	{
		return SandBoxMissions.OpenHideoutAmbushMission(sceneName, playerTroops, location);
	}

	public IMission OpenDisguiseMission(string scene, bool willSetUpContact, string sceneLevels, Location fromLocation)
	{
		return SandBoxMissions.OpenDisguiseMission(scene, willSetUpContact, fromLocation, sceneLevels);
	}

	public IMission OpenNavalRaidMission(TroopRoster navalRaidTroops, BattleSideEnum navalSide, List<Ship> allShips)
	{
		return null;
	}

	public IMission OpenNavalBattleMission(MissionInitializerRecord rec)
	{
		return null;
	}

	public IMission OpenNavalSetPieceBattleMission(MissionInitializerRecord rec, MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips)
	{
		return null;
	}
}
