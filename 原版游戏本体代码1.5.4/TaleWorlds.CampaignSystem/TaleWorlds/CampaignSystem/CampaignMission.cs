using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public static class CampaignMission
{
	public interface ICampaignMissionManager
	{
		IMission OpenSiegeMissionWithDeployment(string scene, float[] wallHitPointsPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int upgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false);

		IMission OpenSiegeMissionNoDeployment(string scene, bool isSallyOut = false, bool isReliefForceAttack = false);

		IMission OpenSiegeLordsHallFightMission(string scene, FlattenedTroopRoster attackerPriorityList);

		IMission OpenBattleMission(MissionInitializerRecord rec);

		IMission OpenCaravanBattleMission(MissionInitializerRecord rec, bool isCaravan);

		IMission OpenBattleMission(string scene, bool usesTownDecalAtlas, string sceneLevels);

		IMission OpenNavalRaidMission(TroopRoster navalRaidTroops, BattleSideEnum navalSide, List<Ship> allShips);

		IMission OpenNavalBattleMission(MissionInitializerRecord rec);

		IMission OpenNavalSetPieceBattleMission(MissionInitializerRecord rec, MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips);

		IMission OpenHideoutBattleMission(string scene, FlattenedTroopRoster playerTroops, bool isTutorial);

		IMission OpenTownCenterMission(string scene, int townUpgradeLevel, Location location, CharacterObject talkToChar, string playerSpawnTag);

		IMission OpenCastleCourtyardMission(string scene, int castleUpgradeLevel, Location location, CharacterObject talkToChar);

		IMission OpenVillageMission(string scene, Location location, CharacterObject talkToChar);

		IMission OpenIndoorMission(string scene, int upgradeLevel, Location location, CharacterObject talkToChar);

		IMission OpenPrisonBreakMission(string scene, Location location, CharacterObject prisonerCharacter);

		IMission OpenArenaStartMission(string scene, Location location, CharacterObject talkToChar);

		IMission OpenArenaDuelMission(string scene, Location location, CharacterObject duelCharacter, bool requireCivilianEquipment, bool spawnBOthSidesWithHorse, Action<CharacterObject> onDuelEndAction, float customAgentHealth);

		IMission OpenConversationMission(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, string specialScene = "", string sceneLevels = "", bool isMultiAgentConversation = false);

		IMission OpenMeetingMission(string scene, CharacterObject character);

		IMission OpenAlleyFightMission(string scene, int upgradeLevel, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops);

		IMission OpenCombatMissionWithDialogue(string scene, CharacterObject characterToTalkTo, int upgradeLevel);

		IMission OpenBattleMissionWhileEnteringSettlement(string scene, int upgradeLevel, int numberOfMaxTroopToBeSpawnedForPlayer, int numberOfMaxTroopToBeSpawnedForOpponent);

		IMission OpenRetirementMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null, string unconsciousMenuId = "");

		IMission OpenHideoutAmbushMission(string sceneName, FlattenedTroopRoster playerTroops, Location location);

		IMission OpenDisguiseMission(string scene, bool willSetUpContact, string sceneLevels, Location fromLocation);
	}

	public static ICampaignMission Current { get; set; }

	public static IMission OpenBattleMission(string scene, bool usesTownDecalAtlas, string sceneLevels = "")
	{
		return Campaign.Current.CampaignMissionManager.OpenBattleMission(scene, usesTownDecalAtlas, sceneLevels);
	}

	public static IMission OpenNavalRaidMission(TroopRoster attackerSideTroops, BattleSideEnum navalSide, List<Ship> allShips)
	{
		return Campaign.Current.CampaignMissionManager.OpenNavalRaidMission(attackerSideTroops, navalSide, allShips);
	}

	public static IMission OpenAlleyFightMission(string scene, int upgradeLevel, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
	{
		return Campaign.Current.CampaignMissionManager.OpenAlleyFightMission(scene, upgradeLevel, location, playerSideTroops, rivalSideTroops);
	}

	public static IMission OpenCombatMissionWithDialogue(string scene, CharacterObject characterToTalkTo, int upgradeLevel)
	{
		return Campaign.Current.CampaignMissionManager.OpenCombatMissionWithDialogue(scene, characterToTalkTo, upgradeLevel);
	}

	public static IMission OpenBattleMissionWhileEnteringSettlement(string scene, int upgradeLevel, int numberOfMaxTroopToBeSpawnedForPlayer, int numberOfMaxTroopToBeSpawnedForOpponent)
	{
		return Campaign.Current.CampaignMissionManager.OpenBattleMissionWhileEnteringSettlement(scene, upgradeLevel, numberOfMaxTroopToBeSpawnedForPlayer, numberOfMaxTroopToBeSpawnedForOpponent);
	}

	public static IMission OpenHideoutBattleMission(string scene, FlattenedTroopRoster playerTroops, bool isTutorial)
	{
		return Campaign.Current.CampaignMissionManager.OpenHideoutBattleMission(scene, playerTroops, isTutorial);
	}

	public static IMission OpenSiegeMissionWithDeployment(string scene, float[] wallHitPointsPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int upgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false)
	{
		return Campaign.Current.CampaignMissionManager.OpenSiegeMissionWithDeployment(scene, wallHitPointsPercentages, hasAnySiegeTower, siegeWeaponsOfAttackers, siegeWeaponsOfDefenders, isPlayerAttacker, upgradeLevel, isSallyOut, isReliefForceAttack);
	}

	public static IMission OpenSiegeMissionNoDeployment(string scene, bool isSallyOut = false, bool isReliefForceAttack = false)
	{
		return Campaign.Current.CampaignMissionManager.OpenSiegeMissionNoDeployment(scene, isSallyOut, isReliefForceAttack);
	}

	public static IMission OpenSiegeLordsHallFightMission(string scene, FlattenedTroopRoster attackerPriorityList)
	{
		return Campaign.Current.CampaignMissionManager.OpenSiegeLordsHallFightMission(scene, attackerPriorityList);
	}

	public static IMission OpenBattleMission(MissionInitializerRecord rec)
	{
		return Campaign.Current.CampaignMissionManager.OpenBattleMission(rec);
	}

	public static IMission OpenNavalBattleMission(MissionInitializerRecord rec)
	{
		return Campaign.Current.CampaignMissionManager.OpenNavalBattleMission(rec);
	}

	public static IMission OpenNavalSetPieceBattleMission(MissionInitializerRecord rec, MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips)
	{
		return Campaign.Current.CampaignMissionManager.OpenNavalSetPieceBattleMission(rec, playerShips, playerAllyShips, enemyShips);
	}

	public static IMission OpenCaravanBattleMission(MissionInitializerRecord rec, bool isCaravan)
	{
		return Campaign.Current.CampaignMissionManager.OpenCaravanBattleMission(rec, isCaravan);
	}

	public static IMission OpenTownCenterMission(string scene, Location location, CharacterObject talkToChar, int townUpgradeLevel, string playerSpawnTag)
	{
		return Campaign.Current.CampaignMissionManager.OpenTownCenterMission(scene, townUpgradeLevel, location, talkToChar, playerSpawnTag);
	}

	public static IMission OpenCastleCourtyardMission(string scene, Location location, CharacterObject talkToChar, int castleUpgradeLevel)
	{
		return Campaign.Current.CampaignMissionManager.OpenCastleCourtyardMission(scene, castleUpgradeLevel, location, talkToChar);
	}

	public static IMission OpenVillageMission(string scene, Location location, CharacterObject talkToChar)
	{
		return Campaign.Current.CampaignMissionManager.OpenVillageMission(scene, location, talkToChar);
	}

	public static IMission OpenIndoorMission(string scene, int upgradeLevel, Location location, CharacterObject talkToChar)
	{
		return Campaign.Current.CampaignMissionManager.OpenIndoorMission(scene, upgradeLevel, location, talkToChar);
	}

	public static IMission OpenPrisonBreakMission(string scene, Location location, CharacterObject prisonerCharacter)
	{
		return Campaign.Current.CampaignMissionManager.OpenPrisonBreakMission(scene, location, prisonerCharacter);
	}

	public static IMission OpenArenaStartMission(string scene, Location location, CharacterObject talkToChar)
	{
		return Campaign.Current.CampaignMissionManager.OpenArenaStartMission(scene, location, talkToChar);
	}

	public static IMission OpenArenaDuelMission(string scene, Location location, CharacterObject talkToChar, bool requireCivilianEquipment, bool spawnBothSidesWithHorse, Action<CharacterObject> onDuelEnd, float customAgentHealth)
	{
		return Campaign.Current.CampaignMissionManager.OpenArenaDuelMission(scene, location, talkToChar, requireCivilianEquipment, spawnBothSidesWithHorse, onDuelEnd, customAgentHealth);
	}

	public static IMission OpenConversationMission(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, string specialScene = "", string sceneLevels = "", bool isMultiAgentConversation = false)
	{
		return Campaign.Current.CampaignMissionManager.OpenConversationMission(playerCharacterData, conversationPartnerData, specialScene, sceneLevels, isMultiAgentConversation);
	}

	public static IMission OpenRetirementMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null, string unconsciousMenuId = "")
	{
		return Campaign.Current.CampaignMissionManager.OpenRetirementMission(scene, location, talkToChar, sceneLevels, unconsciousMenuId);
	}

	public static IMission OpenHideoutAmbushMission(string sceneName, FlattenedTroopRoster playerTroops, Location location)
	{
		return Campaign.Current.CampaignMissionManager.OpenHideoutAmbushMission(sceneName, playerTroops, location);
	}

	public static IMission OpenDisguiseMission(string scene, bool willSetUpContact, string sceneLevels, Location fromLocation)
	{
		return Campaign.Current.CampaignMissionManager.OpenDisguiseMission(scene, willSetUpContact, sceneLevels, fromLocation);
	}
}
