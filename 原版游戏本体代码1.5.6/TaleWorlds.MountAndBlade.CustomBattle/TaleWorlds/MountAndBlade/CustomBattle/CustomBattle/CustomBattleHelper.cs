using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

public static class CustomBattleHelper
{
	public const string DefaultBattleGameTypeStringId = "Battle";

	public const string DefaultSiegeGameTypeStringId = "Siege";

	public const string DefaultVillageGameTypeStringId = "Village";

	private const string EmpireInfantryTroop = "imperial_veteran_infantryman";

	private const string EmpireRangedTroop = "imperial_archer";

	private const string EmpireCavalryTroop = "imperial_heavy_horseman";

	private const string EmpireHorseArcherTroop = "bucellarii";

	private const string SturgiaInfantryTroop = "sturgian_spearman";

	private const string SturgiaRangedTroop = "sturgian_archer";

	private const string SturgiaCavalryTroop = "sturgian_hardened_brigand";

	private const string AseraiInfantryTroop = "aserai_infantry";

	private const string AseraiRangedTroop = "aserai_archer";

	private const string AseraiCavalryTroop = "aserai_mameluke_cavalry";

	private const string AseraiHorseArcherTroop = "aserai_faris";

	private const string VlandiaInfantryTroop = "vlandian_swordsman";

	private const string VlandiaRangedTroop = "vlandian_hardened_crossbowman";

	private const string VlandiaCavalryTroop = "vlandian_knight";

	private const string BattaniaInfantryTroop = "battanian_picked_warrior";

	private const string BattaniaRangedTroop = "battanian_hero";

	private const string BattaniaCavalryTroop = "battanian_scout";

	private const string KhuzaitInfantryTroop = "khuzait_spear_infantry";

	private const string KhuzaitRangedTroop = "khuzait_archer";

	private const string KhuzaitCavalryTroop = "khuzait_lancer";

	private const string KhuzaitHorseArcherTroop = "khuzait_horse_archer";

	private const string NordInfantryTroop = "nord_spear_warrior";

	private const string NordRangedTroop = "nord_marksman";

	public static int GetIndexFromGameTypeStringId(string gameTypeStringId)
	{
		switch (gameTypeStringId)
		{
		case "Battle":
			return 0;
		case "Siege":
			return 1;
		case "Village":
			return 2;
		default:
			Debug.FailedAssert("Given gameTypeStringId: \"" + gameTypeStringId + "\" is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.CustomBattle\\CustomBattle\\CustomBattleHelper.cs", "GetIndexFromGameTypeStringId", 78);
			return -1;
		}
	}

	public static void StartGame(CustomBattleData data)
	{
		Game.Current.PlayerTroop = data.PlayerCharacter;
		if (data.GameTypeStringId == "Siege")
		{
			BannerlordMissions.OpenSiegeMissionWithDeployment(data.SceneId, data.PlayerCharacter, data.PlayerParty, data.EnemyParty, data.IsPlayerGeneral, data.WallHitpointPercentages, data.HasAnySiegeTower, data.AttackerMachines, data.DefenderMachines, data.IsPlayerAttacker, data.SceneUpgradeLevel, data.SeasonId, data.IsSallyOut, data.IsReliefAttack, data.TimeOfDay);
		}
		else
		{
			BannerlordMissions.OpenCustomBattleMission(data.SceneId, data.PlayerCharacter, data.PlayerParty, data.EnemyParty, data.IsPlayerGeneral, data.PlayerSideGeneralCharacter, data.SceneLevel, data.SeasonId, data.TimeOfDay);
		}
	}

	public static int[] GetTroopCounts(int armySize, CustomBattleCompositionData compositionData)
	{
		int[] array = new int[4];
		armySize--;
		array[1] = MathF.Round(compositionData.RangedPercentage * (float)armySize);
		array[2] = MathF.Round(compositionData.CavalryPercentage * (float)armySize);
		array[3] = MathF.Round(compositionData.RangedCavalryPercentage * (float)armySize);
		array[0] = armySize - array.Sum();
		return array;
	}

	public static float[] GetWallHitpointPercentages(int breachedWallCount)
	{
		float[] array = new float[2];
		switch (breachedWallCount)
		{
		case 1:
		{
			int num = MBRandom.RandomInt(2);
			array[num] = 0f;
			array[1 - num] = 1f;
			break;
		}
		case 0:
			array[0] = 1f;
			array[1] = 1f;
			break;
		default:
			array[0] = 0f;
			array[1] = 0f;
			break;
		}
		return array;
	}

	public static SiegeEngineType GetSiegeWeaponType(SiegeEngineType siegeWeaponType)
	{
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ladder)
		{
			return DefaultSiegeEngineTypes.Ladder;
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ballista)
		{
			return DefaultSiegeEngineTypes.Ballista;
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.FireBallista)
		{
			return DefaultSiegeEngineTypes.FireBallista;
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ram || siegeWeaponType == DefaultSiegeEngineTypes.ImprovedRam)
		{
			return DefaultSiegeEngineTypes.Ram;
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.SiegeTower)
		{
			return DefaultSiegeEngineTypes.SiegeTower;
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Onager || siegeWeaponType == DefaultSiegeEngineTypes.Catapult)
		{
			return DefaultSiegeEngineTypes.Onager;
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.FireOnager || siegeWeaponType == DefaultSiegeEngineTypes.FireCatapult)
		{
			return DefaultSiegeEngineTypes.FireOnager;
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Trebuchet || siegeWeaponType == DefaultSiegeEngineTypes.Bricole)
		{
			return DefaultSiegeEngineTypes.Trebuchet;
		}
		return siegeWeaponType;
	}

	public static CustomBattleData PrepareBattleData(BasicCharacterObject playerCharacter, BasicCharacterObject playerSideGeneralCharacter, CustomBattleCombatant playerParty, CustomBattleCombatant enemyParty, CustomBattlePlayerSide playerSide, CustomBattlePlayerType battlePlayerType, string gameTypeStringId, string scene, string season, float timeOfDay, List<MissionSiegeWeapon> attackerMachines, List<MissionSiegeWeapon> defenderMachines, float[] wallHitPointsPercentages, int sceneUpgradeLevel, bool isSallyOut, string forcedSceneLevel)
	{
		bool isPlayerAttacker = playerSide == CustomBattlePlayerSide.Attacker;
		bool isPlayerGeneral = battlePlayerType == CustomBattlePlayerType.Commander;
		CustomBattleData result = new CustomBattleData
		{
			GameTypeStringId = gameTypeStringId,
			SceneId = scene,
			PlayerCharacter = playerCharacter,
			PlayerParty = playerParty,
			EnemyParty = enemyParty,
			IsPlayerGeneral = isPlayerGeneral,
			PlayerSideGeneralCharacter = playerSideGeneralCharacter,
			SeasonId = season,
			SceneLevel = forcedSceneLevel,
			TimeOfDay = timeOfDay
		};
		if (result.GameTypeStringId == "Siege")
		{
			result.AttackerMachines = attackerMachines;
			result.DefenderMachines = defenderMachines;
			result.WallHitpointPercentages = wallHitPointsPercentages;
			result.HasAnySiegeTower = attackerMachines.Exists((MissionSiegeWeapon mm) => mm.Type == DefaultSiegeEngineTypes.SiegeTower);
			result.IsPlayerAttacker = isPlayerAttacker;
			result.SceneUpgradeLevel = sceneUpgradeLevel;
			result.IsSallyOut = isSallyOut;
			result.IsReliefAttack = false;
		}
		return result;
	}

	public static CustomBattleCombatant[] GetCustomBattleParties(BasicCharacterObject playerCharacter, BasicCharacterObject playerSideGeneralCharacter, BasicCharacterObject enemyCharacter, BasicCultureObject playerFaction, int[] playerNumbers, List<BasicCharacterObject>[] playerTroopSelections, BasicCultureObject enemyFaction, int[] enemyNumbers, List<BasicCharacterObject>[] enemyTroopSelections, bool isPlayerAttacker)
	{
		Banner banner = new Banner(playerFaction.Banner, playerFaction.Color, playerFaction.Color2);
		Banner banner2 = new Banner(enemyFaction.Banner, enemyFaction.Color, enemyFaction.Color2);
		if (playerFaction.StringId == enemyFaction.StringId)
		{
			uint primaryColor = banner2.GetPrimaryColor();
			banner2.ChangePrimaryColor(banner2.GetFirstIconColor());
			banner2.ChangeIconColors(primaryColor);
		}
		CustomBattleCombatant[] array = new CustomBattleCombatant[2]
		{
			new CustomBattleCombatant(new TextObject("{=sSJSTe5p}Player Party"), playerFaction, banner),
			new CustomBattleCombatant(new TextObject("{=0xC75dN6}Enemy Party"), enemyFaction, banner2)
		};
		array[0].Side = (isPlayerAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		array[0].AddCharacter(playerCharacter, 1);
		if (playerSideGeneralCharacter != null)
		{
			array[0].AddCharacter(playerSideGeneralCharacter, 1);
			array[0].SetGeneral(playerSideGeneralCharacter);
		}
		else
		{
			array[0].SetGeneral(playerCharacter);
		}
		array[1].Side = array[0].Side.GetOppositeSide();
		array[1].AddCharacter(enemyCharacter, 1);
		for (int i = 0; i < array.Length; i++)
		{
			PopulateListsWithDefaults(ref array[i], (i == 0) ? playerNumbers : enemyNumbers, (i == 0) ? playerTroopSelections : enemyTroopSelections);
		}
		return array;
	}

	private static void PopulateListsWithDefaults(ref CustomBattleCombatant customBattleParties, int[] numbers, List<BasicCharacterObject>[] troopList)
	{
		BasicCultureObject basicCulture = customBattleParties.BasicCulture;
		if (troopList == null)
		{
			troopList = new List<BasicCharacterObject>[4]
			{
				new List<BasicCharacterObject>(),
				new List<BasicCharacterObject>(),
				new List<BasicCharacterObject>(),
				new List<BasicCharacterObject>()
			};
		}
		if (troopList[0].Count == 0)
		{
			troopList[0] = new List<BasicCharacterObject> { GetDefaultTroopOfFormationForFaction(basicCulture, FormationClass.Infantry) };
		}
		if (troopList[1].Count == 0)
		{
			troopList[1] = new List<BasicCharacterObject> { GetDefaultTroopOfFormationForFaction(basicCulture, FormationClass.Ranged) };
		}
		if (troopList[2].Count == 0)
		{
			troopList[2] = new List<BasicCharacterObject> { GetDefaultTroopOfFormationForFaction(basicCulture, FormationClass.Cavalry) };
		}
		if (troopList[3].Count == 0)
		{
			troopList[3] = new List<BasicCharacterObject> { GetDefaultTroopOfFormationForFaction(basicCulture, FormationClass.HorseArcher) };
		}
		if (troopList[3].Count == 0 || troopList[3].All((BasicCharacterObject troop) => troop == null))
		{
			numbers[2] += numbers[3] / 3;
			numbers[1] += numbers[3] / 3;
			numbers[0] += numbers[3] / 3;
			numbers[0] += numbers[3] - numbers[3] / 3 * 3;
			numbers[3] = 0;
		}
		for (int num = 0; num < 4; num++)
		{
			int count = troopList[num].Count;
			int num2 = numbers[num];
			if (num2 <= 0)
			{
				continue;
			}
			float num3 = (float)num2 / (float)count;
			float num4 = 0f;
			for (int num5 = 0; num5 < count; num5++)
			{
				float num6 = num3 + num4;
				int num7 = MathF.Floor(num6);
				num4 = num6 - (float)num7;
				customBattleParties.AddCharacter(troopList[num][num5], num7);
				numbers[num] -= num7;
				if (num5 == count - 1 && numbers[num] > 0)
				{
					customBattleParties.AddCharacter(troopList[num][num5], numbers[num]);
					numbers[num] = 0;
				}
			}
		}
	}

	public static void AssertMissingTroopsForDebug()
	{
		foreach (BasicCultureObject faction in CustomBattleData.Factions)
		{
			for (int i = 0; i < 4; i++)
			{
				GetDefaultTroopOfFormationForFaction(faction, (FormationClass)i);
			}
		}
	}

	public static BasicCharacterObject GetDefaultTroopOfFormationForFaction(BasicCultureObject culture, FormationClass formation)
	{
		if (culture.StringId.ToLower() == "empire")
		{
			switch (formation)
			{
			case FormationClass.Infantry:
				return GetTroopFromId("imperial_veteran_infantryman");
			case FormationClass.Ranged:
				return GetTroopFromId("imperial_archer");
			case FormationClass.Cavalry:
				return GetTroopFromId("imperial_heavy_horseman");
			case FormationClass.HorseArcher:
				return GetTroopFromId("bucellarii");
			}
		}
		else if (culture.StringId.ToLower() == "sturgia")
		{
			switch (formation)
			{
			case FormationClass.Infantry:
				return GetTroopFromId("sturgian_spearman");
			case FormationClass.Ranged:
				return GetTroopFromId("sturgian_archer");
			case FormationClass.Cavalry:
				return GetTroopFromId("sturgian_hardened_brigand");
			}
		}
		else if (culture.StringId.ToLower() == "aserai")
		{
			switch (formation)
			{
			case FormationClass.Infantry:
				return GetTroopFromId("aserai_infantry");
			case FormationClass.Ranged:
				return GetTroopFromId("aserai_archer");
			case FormationClass.Cavalry:
				return GetTroopFromId("aserai_mameluke_cavalry");
			case FormationClass.HorseArcher:
				return GetTroopFromId("aserai_faris");
			}
		}
		else if (culture.StringId.ToLower() == "vlandia")
		{
			switch (formation)
			{
			case FormationClass.Infantry:
				return GetTroopFromId("vlandian_swordsman");
			case FormationClass.Ranged:
				return GetTroopFromId("vlandian_hardened_crossbowman");
			case FormationClass.Cavalry:
				return GetTroopFromId("vlandian_knight");
			}
		}
		else if (culture.StringId.ToLower() == "battania")
		{
			switch (formation)
			{
			case FormationClass.Infantry:
				return GetTroopFromId("battanian_picked_warrior");
			case FormationClass.Ranged:
				return GetTroopFromId("battanian_hero");
			case FormationClass.Cavalry:
				return GetTroopFromId("battanian_scout");
			}
		}
		else if (culture.StringId.ToLower() == "khuzait")
		{
			switch (formation)
			{
			case FormationClass.Infantry:
				return GetTroopFromId("khuzait_spear_infantry");
			case FormationClass.Ranged:
				return GetTroopFromId("khuzait_archer");
			case FormationClass.Cavalry:
				return GetTroopFromId("khuzait_lancer");
			case FormationClass.HorseArcher:
				return GetTroopFromId("khuzait_horse_archer");
			}
		}
		else if (culture.StringId.ToLower() == "nord")
		{
			switch (formation)
			{
			case FormationClass.Infantry:
				return GetTroopFromId("nord_spear_warrior");
			case FormationClass.Ranged:
				return GetTroopFromId("nord_marksman");
			}
		}
		return null;
	}

	private static BasicCharacterObject GetTroopFromId(string troopId)
	{
		return MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopId);
	}
}
