using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.Missions;

public class NavalMissionManager : CampaignMission.ICampaignMissionManager
{
	private readonly CampaignMission.ICampaignMissionManager _baseMissionManager;

	public NavalMissionManager(CampaignMission.ICampaignMissionManager baseMissionManager)
	{
		_baseMissionManager = baseMissionManager;
	}

	public IMission OpenNavalRaidMission(TroopRoster navalRaidTroops, BattleSideEnum navalSide, List<Ship> allShips)
	{
		return NavalMissions.OpenNavalRaidMission(navalRaidTroops, navalSide, allShips);
	}

	public IMission OpenNavalBattleMission(MissionInitializerRecord rec)
	{
		return NavalMissions.OpenNavalBattleMission(rec);
	}

	public IMission OpenNavalSetPieceBattleMission(MissionInitializerRecord rec, MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips)
	{
		return NavalMissions.OpenNavalSetPieceBattleMission(rec, playerShips, playerAllyShips, enemyShips);
	}

	public IMission OpenAlleyFightMission(string scene, int upgradeLevel, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
	{
		return _baseMissionManager.OpenAlleyFightMission(scene, upgradeLevel, location, playerSideTroops, rivalSideTroops);
	}

	public IMission OpenArenaDuelMission(string scene, Location location, CharacterObject duelCharacter, bool requireCivilianEquipment, bool spawnBOthSidesWithHorse, Action<CharacterObject> onDuelEndAction, float customAgentHealth)
	{
		return _baseMissionManager.OpenArenaDuelMission(scene, location, duelCharacter, requireCivilianEquipment, spawnBOthSidesWithHorse, onDuelEndAction, customAgentHealth);
	}

	public IMission OpenArenaStartMission(string scene, Location location, CharacterObject talkToChar)
	{
		return _baseMissionManager.OpenArenaStartMission(scene, location, talkToChar);
	}

	public IMission OpenBattleMission(MissionInitializerRecord rec)
	{
		return _baseMissionManager.OpenBattleMission(rec);
	}

	public IMission OpenBattleMission(string scene, bool usesTownDecalAtlas, string sceneLevels)
	{
		return _baseMissionManager.OpenBattleMission(scene, usesTownDecalAtlas, sceneLevels);
	}

	public IMission OpenBattleMissionWhileEnteringSettlement(string scene, int upgradeLevel, int numberOfMaxTroopToBeSpawnedForPlayer, int numberOfMaxTroopToBeSpawnedForOpponent)
	{
		return _baseMissionManager.OpenBattleMissionWhileEnteringSettlement(scene, upgradeLevel, numberOfMaxTroopToBeSpawnedForPlayer, numberOfMaxTroopToBeSpawnedForOpponent);
	}

	public IMission OpenCaravanBattleMission(MissionInitializerRecord rec, bool isCaravan)
	{
		return _baseMissionManager.OpenCaravanBattleMission(rec, isCaravan);
	}

	public IMission OpenCastleCourtyardMission(string scene, int castleUpgradeLevel, Location location, CharacterObject talkToChar)
	{
		return _baseMissionManager.OpenCastleCourtyardMission(scene, castleUpgradeLevel, location, talkToChar);
	}

	public IMission OpenCombatMissionWithDialogue(string scene, CharacterObject characterToTalkTo, int upgradeLevel)
	{
		return _baseMissionManager.OpenCombatMissionWithDialogue(scene, characterToTalkTo, upgradeLevel);
	}

	public IMission OpenConversationMission(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, string specialScene = "", string sceneLevels = "", bool isMultiAgentConversation = false)
	{
		return _baseMissionManager.OpenConversationMission(playerCharacterData, conversationPartnerData, specialScene, sceneLevels, isMultiAgentConversation);
	}

	public IMission OpenHideoutBattleMission(string scene, FlattenedTroopRoster playerTroops, bool isTutorial)
	{
		return _baseMissionManager.OpenHideoutBattleMission(scene, playerTroops, isTutorial);
	}

	public IMission OpenIndoorMission(string scene, int upgradeLevel, Location location, CharacterObject talkToChar)
	{
		return _baseMissionManager.OpenIndoorMission(scene, upgradeLevel, location, talkToChar);
	}

	public IMission OpenMeetingMission(string scene, CharacterObject character)
	{
		return _baseMissionManager.OpenMeetingMission(scene, character);
	}

	public IMission OpenPrisonBreakMission(string scene, Location location, CharacterObject prisonerCharacter)
	{
		return _baseMissionManager.OpenPrisonBreakMission(scene, location, prisonerCharacter);
	}

	public IMission OpenRetirementMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null, string unconsciousMenuId = "")
	{
		return _baseMissionManager.OpenRetirementMission(scene, location, talkToChar, sceneLevels);
	}

	public IMission OpenSiegeLordsHallFightMission(string scene, FlattenedTroopRoster attackerPriorityList)
	{
		return _baseMissionManager.OpenSiegeLordsHallFightMission(scene, attackerPriorityList);
	}

	public IMission OpenSiegeMissionNoDeployment(string scene, bool isSallyOut = false, bool isReliefForceAttack = false)
	{
		return _baseMissionManager.OpenSiegeMissionNoDeployment(scene, isSallyOut, isReliefForceAttack);
	}

	public IMission OpenSiegeMissionWithDeployment(string scene, float[] wallHitPointsPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int upgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false)
	{
		return _baseMissionManager.OpenSiegeMissionWithDeployment(scene, wallHitPointsPercentages, hasAnySiegeTower, siegeWeaponsOfAttackers, siegeWeaponsOfDefenders, isPlayerAttacker, upgradeLevel, isSallyOut, isReliefForceAttack);
	}

	public IMission OpenTownCenterMission(string scene, int townUpgradeLevel, Location location, CharacterObject talkToChar, string playerSpawnTag)
	{
		return _baseMissionManager.OpenTownCenterMission(scene, townUpgradeLevel, location, talkToChar, playerSpawnTag);
	}

	public IMission OpenVillageMission(string scene, Location location, CharacterObject talkToChar)
	{
		return _baseMissionManager.OpenVillageMission(scene, location, talkToChar);
	}

	public IMission OpenHideoutAmbushMission(string sceneName, FlattenedTroopRoster playerTroops, Location location)
	{
		return _baseMissionManager.OpenHideoutAmbushMission(sceneName, playerTroops, location);
	}

	public IMission OpenDisguiseMission(string scene, bool willSetUpContact, string sceneLevels, Location fromLocation)
	{
		return _baseMissionManager.OpenDisguiseMission(scene, willSetUpContact, sceneLevels, fromLocation);
	}
}
