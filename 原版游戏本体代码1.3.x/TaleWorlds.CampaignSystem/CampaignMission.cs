using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200005A RID: 90
	public static class CampaignMission
	{
		// Token: 0x170001D0 RID: 464
		// (get) Token: 0x060008F8 RID: 2296 RVA: 0x00026F46 File Offset: 0x00025146
		// (set) Token: 0x060008F9 RID: 2297 RVA: 0x00026F4D File Offset: 0x0002514D
		public static ICampaignMission Current { get; set; }

		// Token: 0x060008FA RID: 2298 RVA: 0x00026F55 File Offset: 0x00025155
		public static IMission OpenBattleMission(string scene, bool usesTownDecalAtlas)
		{
			return Campaign.Current.CampaignMissionManager.OpenBattleMission(scene, usesTownDecalAtlas);
		}

		// Token: 0x060008FB RID: 2299 RVA: 0x00026F68 File Offset: 0x00025168
		public static IMission OpenAlleyFightMission(string scene, int upgradeLevel, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
		{
			return Campaign.Current.CampaignMissionManager.OpenAlleyFightMission(scene, upgradeLevel, location, playerSideTroops, rivalSideTroops);
		}

		// Token: 0x060008FC RID: 2300 RVA: 0x00026F7F File Offset: 0x0002517F
		public static IMission OpenCombatMissionWithDialogue(string scene, CharacterObject characterToTalkTo, int upgradeLevel)
		{
			return Campaign.Current.CampaignMissionManager.OpenCombatMissionWithDialogue(scene, characterToTalkTo, upgradeLevel);
		}

		// Token: 0x060008FD RID: 2301 RVA: 0x00026F93 File Offset: 0x00025193
		public static IMission OpenBattleMissionWhileEnteringSettlement(string scene, int upgradeLevel, int numberOfMaxTroopToBeSpawnedForPlayer, int numberOfMaxTroopToBeSpawnedForOpponent)
		{
			return Campaign.Current.CampaignMissionManager.OpenBattleMissionWhileEnteringSettlement(scene, upgradeLevel, numberOfMaxTroopToBeSpawnedForPlayer, numberOfMaxTroopToBeSpawnedForOpponent);
		}

		// Token: 0x060008FE RID: 2302 RVA: 0x00026FA8 File Offset: 0x000251A8
		public static IMission OpenHideoutBattleMission(string scene, FlattenedTroopRoster playerTroops, bool isTutorial)
		{
			return Campaign.Current.CampaignMissionManager.OpenHideoutBattleMission(scene, playerTroops, isTutorial);
		}

		// Token: 0x060008FF RID: 2303 RVA: 0x00026FBC File Offset: 0x000251BC
		public static IMission OpenSiegeMissionWithDeployment(string scene, float[] wallHitPointsPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int upgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false)
		{
			return Campaign.Current.CampaignMissionManager.OpenSiegeMissionWithDeployment(scene, wallHitPointsPercentages, hasAnySiegeTower, siegeWeaponsOfAttackers, siegeWeaponsOfDefenders, isPlayerAttacker, upgradeLevel, isSallyOut, isReliefForceAttack);
		}

		// Token: 0x06000900 RID: 2304 RVA: 0x00026FE6 File Offset: 0x000251E6
		public static IMission OpenSiegeMissionNoDeployment(string scene, bool isSallyOut = false, bool isReliefForceAttack = false)
		{
			return Campaign.Current.CampaignMissionManager.OpenSiegeMissionNoDeployment(scene, isSallyOut, isReliefForceAttack);
		}

		// Token: 0x06000901 RID: 2305 RVA: 0x00026FFA File Offset: 0x000251FA
		public static IMission OpenSiegeLordsHallFightMission(string scene, FlattenedTroopRoster attackerPriorityList)
		{
			return Campaign.Current.CampaignMissionManager.OpenSiegeLordsHallFightMission(scene, attackerPriorityList);
		}

		// Token: 0x06000902 RID: 2306 RVA: 0x0002700D File Offset: 0x0002520D
		public static IMission OpenBattleMission(MissionInitializerRecord rec)
		{
			return Campaign.Current.CampaignMissionManager.OpenBattleMission(rec);
		}

		// Token: 0x06000903 RID: 2307 RVA: 0x0002701F File Offset: 0x0002521F
		public static IMission OpenNavalBattleMission(MissionInitializerRecord rec)
		{
			return Campaign.Current.CampaignMissionManager.OpenNavalBattleMission(rec);
		}

		// Token: 0x06000904 RID: 2308 RVA: 0x00027031 File Offset: 0x00025231
		public static IMission OpenNavalSetPieceBattleMission(MissionInitializerRecord rec, MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips)
		{
			return Campaign.Current.CampaignMissionManager.OpenNavalSetPieceBattleMission(rec, playerShips, playerAllyShips, enemyShips);
		}

		// Token: 0x06000905 RID: 2309 RVA: 0x00027046 File Offset: 0x00025246
		public static IMission OpenCaravanBattleMission(MissionInitializerRecord rec, bool isCaravan)
		{
			return Campaign.Current.CampaignMissionManager.OpenCaravanBattleMission(rec, isCaravan);
		}

		// Token: 0x06000906 RID: 2310 RVA: 0x00027059 File Offset: 0x00025259
		public static IMission OpenTownCenterMission(string scene, Location location, CharacterObject talkToChar, int townUpgradeLevel, string playerSpawnTag)
		{
			return Campaign.Current.CampaignMissionManager.OpenTownCenterMission(scene, townUpgradeLevel, location, talkToChar, playerSpawnTag);
		}

		// Token: 0x06000907 RID: 2311 RVA: 0x00027070 File Offset: 0x00025270
		public static IMission OpenCastleCourtyardMission(string scene, Location location, CharacterObject talkToChar, int castleUpgradeLevel)
		{
			return Campaign.Current.CampaignMissionManager.OpenCastleCourtyardMission(scene, castleUpgradeLevel, location, talkToChar);
		}

		// Token: 0x06000908 RID: 2312 RVA: 0x00027085 File Offset: 0x00025285
		public static IMission OpenVillageMission(string scene, Location location, CharacterObject talkToChar)
		{
			return Campaign.Current.CampaignMissionManager.OpenVillageMission(scene, location, talkToChar);
		}

		// Token: 0x06000909 RID: 2313 RVA: 0x00027099 File Offset: 0x00025299
		public static IMission OpenIndoorMission(string scene, int upgradeLevel, Location location, CharacterObject talkToChar)
		{
			return Campaign.Current.CampaignMissionManager.OpenIndoorMission(scene, upgradeLevel, location, talkToChar);
		}

		// Token: 0x0600090A RID: 2314 RVA: 0x000270AE File Offset: 0x000252AE
		public static IMission OpenPrisonBreakMission(string scene, Location location, CharacterObject prisonerCharacter)
		{
			return Campaign.Current.CampaignMissionManager.OpenPrisonBreakMission(scene, location, prisonerCharacter);
		}

		// Token: 0x0600090B RID: 2315 RVA: 0x000270C2 File Offset: 0x000252C2
		public static IMission OpenArenaStartMission(string scene, Location location, CharacterObject talkToChar)
		{
			return Campaign.Current.CampaignMissionManager.OpenArenaStartMission(scene, location, talkToChar);
		}

		// Token: 0x0600090C RID: 2316 RVA: 0x000270D6 File Offset: 0x000252D6
		public static IMission OpenArenaDuelMission(string scene, Location location, CharacterObject talkToChar, bool requireCivilianEquipment, bool spawnBothSidesWithHorse, Action<CharacterObject> onDuelEnd, float customAgentHealth)
		{
			return Campaign.Current.CampaignMissionManager.OpenArenaDuelMission(scene, location, talkToChar, requireCivilianEquipment, spawnBothSidesWithHorse, onDuelEnd, customAgentHealth);
		}

		// Token: 0x0600090D RID: 2317 RVA: 0x000270F1 File Offset: 0x000252F1
		public static IMission OpenConversationMission(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, string specialScene = "", string sceneLevels = "", bool isMultiAgentConversation = false)
		{
			return Campaign.Current.CampaignMissionManager.OpenConversationMission(playerCharacterData, conversationPartnerData, specialScene, sceneLevels, isMultiAgentConversation);
		}

		// Token: 0x0600090E RID: 2318 RVA: 0x00027108 File Offset: 0x00025308
		public static IMission OpenRetirementMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null, string unconsciousMenuId = "")
		{
			return Campaign.Current.CampaignMissionManager.OpenRetirementMission(scene, location, talkToChar, sceneLevels, unconsciousMenuId);
		}

		// Token: 0x0600090F RID: 2319 RVA: 0x0002711F File Offset: 0x0002531F
		public static IMission OpenHideoutAmbushMission(string sceneName, FlattenedTroopRoster playerTroops, Location location)
		{
			return Campaign.Current.CampaignMissionManager.OpenHideoutAmbushMission(sceneName, playerTroops, location);
		}

		// Token: 0x06000910 RID: 2320 RVA: 0x00027133 File Offset: 0x00025333
		public static IMission OpenDisguiseMission(string scene, bool willSetUpContact, string sceneLevels, Location fromLocation)
		{
			return Campaign.Current.CampaignMissionManager.OpenDisguiseMission(scene, willSetUpContact, sceneLevels, fromLocation);
		}

		// Token: 0x02000512 RID: 1298
		public interface ICampaignMissionManager
		{
			// Token: 0x06004B59 RID: 19289
			IMission OpenSiegeMissionWithDeployment(string scene, float[] wallHitPointsPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int upgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false);

			// Token: 0x06004B5A RID: 19290
			IMission OpenSiegeMissionNoDeployment(string scene, bool isSallyOut = false, bool isReliefForceAttack = false);

			// Token: 0x06004B5B RID: 19291
			IMission OpenSiegeLordsHallFightMission(string scene, FlattenedTroopRoster attackerPriorityList);

			// Token: 0x06004B5C RID: 19292
			IMission OpenBattleMission(MissionInitializerRecord rec);

			// Token: 0x06004B5D RID: 19293
			IMission OpenCaravanBattleMission(MissionInitializerRecord rec, bool isCaravan);

			// Token: 0x06004B5E RID: 19294
			IMission OpenBattleMission(string scene, bool usesTownDecalAtlas);

			// Token: 0x06004B5F RID: 19295
			IMission OpenNavalBattleMission(MissionInitializerRecord rec);

			// Token: 0x06004B60 RID: 19296
			IMission OpenNavalSetPieceBattleMission(MissionInitializerRecord rec, MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips);

			// Token: 0x06004B61 RID: 19297
			IMission OpenHideoutBattleMission(string scene, FlattenedTroopRoster playerTroops, bool isTutorial);

			// Token: 0x06004B62 RID: 19298
			IMission OpenTownCenterMission(string scene, int townUpgradeLevel, Location location, CharacterObject talkToChar, string playerSpawnTag);

			// Token: 0x06004B63 RID: 19299
			IMission OpenCastleCourtyardMission(string scene, int castleUpgradeLevel, Location location, CharacterObject talkToChar);

			// Token: 0x06004B64 RID: 19300
			IMission OpenVillageMission(string scene, Location location, CharacterObject talkToChar);

			// Token: 0x06004B65 RID: 19301
			IMission OpenIndoorMission(string scene, int upgradeLevel, Location location, CharacterObject talkToChar);

			// Token: 0x06004B66 RID: 19302
			IMission OpenPrisonBreakMission(string scene, Location location, CharacterObject prisonerCharacter);

			// Token: 0x06004B67 RID: 19303
			IMission OpenArenaStartMission(string scene, Location location, CharacterObject talkToChar);

			// Token: 0x06004B68 RID: 19304
			IMission OpenArenaDuelMission(string scene, Location location, CharacterObject duelCharacter, bool requireCivilianEquipment, bool spawnBOthSidesWithHorse, Action<CharacterObject> onDuelEndAction, float customAgentHealth);

			// Token: 0x06004B69 RID: 19305
			IMission OpenConversationMission(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, string specialScene = "", string sceneLevels = "", bool isMultiAgentConversation = false);

			// Token: 0x06004B6A RID: 19306
			IMission OpenMeetingMission(string scene, CharacterObject character);

			// Token: 0x06004B6B RID: 19307
			IMission OpenAlleyFightMission(string scene, int upgradeLevel, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops);

			// Token: 0x06004B6C RID: 19308
			IMission OpenCombatMissionWithDialogue(string scene, CharacterObject characterToTalkTo, int upgradeLevel);

			// Token: 0x06004B6D RID: 19309
			IMission OpenBattleMissionWhileEnteringSettlement(string scene, int upgradeLevel, int numberOfMaxTroopToBeSpawnedForPlayer, int numberOfMaxTroopToBeSpawnedForOpponent);

			// Token: 0x06004B6E RID: 19310
			IMission OpenRetirementMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null, string unconsciousMenuId = "");

			// Token: 0x06004B6F RID: 19311
			IMission OpenHideoutAmbushMission(string sceneName, FlattenedTroopRoster playerTroops, Location location);

			// Token: 0x06004B70 RID: 19312
			IMission OpenDisguiseMission(string scene, bool willSetUpContact, string sceneLevels, Location fromLocation);
		}
	}
}
