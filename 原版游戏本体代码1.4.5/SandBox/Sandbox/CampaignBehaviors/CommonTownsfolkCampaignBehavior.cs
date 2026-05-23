using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class CommonTownsfolkCampaignBehavior : CampaignBehaviorBase
{
	public const float TownsmanSpawnPercentageMale = 0.2f;

	public const float TownsmanSpawnPercentageFemale = 0.15f;

	public const float TownsmanSpawnPercentageLimitedMale = 0.15f;

	public const float TownsmanSpawnPercentageLimitedFemale = 0.1f;

	public const float TownOtherPeopleSpawnPercentage = 0.05f;

	public const float TownsmanSpawnPercentageTavernMale = 0.3f;

	public const float TownsmanSpawnPercentageTavernFemale = 0.1f;

	public const float BeggarSpawnPercentage = 0.33f;

	private float GetSpawnRate(Settlement settlement)
	{
		return TimeOfDayPercentage() * GetProsperityMultiplier(settlement.SettlementComponent) * GetWeatherEffectMultiplier(settlement);
	}

	private float GetConfigValue()
	{
		return BannerlordConfig.CivilianAgentCount;
	}

	private float GetProsperityMultiplier(SettlementComponent settlement)
	{
		return ((float)settlement.GetProsperityLevel() + 1f) / 3f;
	}

	private float GetWeatherEffectMultiplier(Settlement settlement)
	{
		return Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position.ToVec2()) switch
		{
			MapWeatherModel.WeatherEvent.Blizzard => 0.4f, 
			MapWeatherModel.WeatherEvent.HeavyRain => 0.15f, 
			_ => 1f, 
		};
	}

	private float TimeOfDayPercentage()
	{
		int num = TaleWorlds.Library.MathF.Ceiling((float)CampaignTime.HoursInDay * 0.625f);
		return 1f - TaleWorlds.Library.MathF.Abs(CampaignTime.Now.CurrentHourInDay - (float)num) / (float)num;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (!settlement.IsCastle)
		{
			Location locationWithId = settlement.LocationComplex.GetLocationWithId("center");
			Location locationWithId2 = settlement.LocationComplex.GetLocationWithId("tavern");
			if (CampaignMission.Current.Location == locationWithId)
			{
				AddPeopleToTownCenter(settlement, unusedUsablePointCount, CampaignTime.Now.IsDayTime);
			}
			if (CampaignMission.Current.Location == locationWithId2)
			{
				AddPeopleToTownTavern(settlement, unusedUsablePointCount);
			}
		}
	}

	private void AddPeopleToTownTavern(Settlement settlement, Dictionary<string, int> unusedUsablePointCount)
	{
		Location locationWithId = settlement.LocationComplex.GetLocationWithId("tavern");
		unusedUsablePointCount.TryGetValue("npc_common", out var value);
		MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position.ToVec2());
		bool flag = weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
		if (value > 0)
		{
			int num = (int)((float)value * (0.3f + (flag ? 0.2f : 0f)));
			if (num > 0)
			{
				locationWithId.AddLocationCharacters(CreateTownsManForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num);
			}
			int num2 = (int)((float)value * (0.1f + (flag ? 0.2f : 0f)));
			if (num2 > 0)
			{
				locationWithId.AddLocationCharacters(CreateTownsWomanForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num2);
			}
		}
	}

	private void AddPeopleToTownCenter(Settlement settlement, Dictionary<string, int> unusedUsablePointCount, bool isDayTime)
	{
		Location locationWithId = settlement.LocationComplex.GetLocationWithId("center");
		CultureObject culture = settlement.Culture;
		unusedUsablePointCount.TryGetValue("npc_common", out var value);
		unusedUsablePointCount.TryGetValue("npc_common_limited", out var value2);
		float num = (float)(value + value2) * 0.65000004f;
		if (num == 0f)
		{
			return;
		}
		float num2 = MBMath.ClampFloat(GetConfigValue() / num, 0f, 1f);
		float num3 = GetSpawnRate(settlement) * num2;
		if (value > 0)
		{
			int num4 = (int)((float)value * 0.2f * num3);
			if (num4 > 0)
			{
				locationWithId.AddLocationCharacters(CreateTownsMan, culture, LocationCharacter.CharacterRelations.Neutral, num4);
			}
			int num5 = (int)((float)value * 0.15f * num3);
			if (num5 > 0)
			{
				locationWithId.AddLocationCharacters(CreateTownsWoman, culture, LocationCharacter.CharacterRelations.Neutral, num5);
			}
		}
		MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position.ToVec2());
		bool flag = weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
		if (!isDayTime || flag)
		{
			return;
		}
		if (value2 > 0)
		{
			int num6 = (int)((float)value2 * 0.15f * num3);
			if (num6 > 0)
			{
				locationWithId.AddLocationCharacters(CreateTownsManCarryingStuff, culture, LocationCharacter.CharacterRelations.Neutral, num6);
			}
			int num7 = (int)((float)value2 * 0.1f * num3);
			if (num7 > 0)
			{
				locationWithId.AddLocationCharacters(CreateTownsWomanCarryingStuff, culture, LocationCharacter.CharacterRelations.Neutral, num7);
			}
			int num8 = (int)((float)value2 * 0.05f * num3);
			if (num8 > 0)
			{
				locationWithId.AddLocationCharacters(CreateMaleChild, culture, LocationCharacter.CharacterRelations.Neutral, num8);
				locationWithId.AddLocationCharacters(CreateFemaleChild, culture, LocationCharacter.CharacterRelations.Neutral, num8);
				locationWithId.AddLocationCharacters(CreateMaleTeenager, culture, LocationCharacter.CharacterRelations.Neutral, num8);
				locationWithId.AddLocationCharacters(CreateFemaleTeenager, culture, LocationCharacter.CharacterRelations.Neutral, num8);
			}
		}
		int value3 = 0;
		if (unusedUsablePointCount.TryGetValue("spawnpoint_cleaner", out value3))
		{
			locationWithId.AddLocationCharacters(CreateBroomsWoman, culture, LocationCharacter.CharacterRelations.Neutral, value3);
		}
		if (unusedUsablePointCount.TryGetValue("npc_dancer", out value3))
		{
			locationWithId.AddLocationCharacters(CreateDancer, culture, LocationCharacter.CharacterRelations.Neutral, value3);
		}
		if (settlement.IsTown && unusedUsablePointCount.TryGetValue("npc_beggar", out value3))
		{
			locationWithId.AddLocationCharacters(CreateFemaleBeggar, culture, LocationCharacter.CharacterRelations.Neutral, (value3 != 1) ? (value3 / 2) : 0);
			locationWithId.AddLocationCharacters(CreateMaleBeggar, culture, LocationCharacter.CharacterRelations.Neutral, (value3 == 1) ? 1 : (value3 / 2));
		}
	}

	public static string GetActionSetSuffixAndMonsterForItem(string itemId, int race, bool isFemale, out Monster monster)
	{
		monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement");
		switch (itemId)
		{
		case "_to_carry_kitchen_pot_c":
			return "_villager_carry_right_hand";
		case "_to_carry_arm_kitchen_pot_c":
			return "_villager_carry_right_arm";
		case "_to_carry_kitchen_pitcher_a":
			return "_villager_carry_over_head";
		case "_to_carry_foods_basket_apple":
			return "_villager_carry_over_head_v2";
		case "_to_carry_merchandise_hides_b":
			return "_villager_with_backpack";
		case "_to_carry_foods_pumpkin_a":
			return "_villager_carry_front_v2";
		case "_to_carry_bd_fabric_c":
		case "_to_carry_foods_watermelon_a":
			return "_villager_carry_right_side";
		case "_to_carry_bed_convolute_a":
			return "_villager_carry_front";
		case "_to_carry_bed_convolute_g":
			return "_villager_carry_on_shoulder";
		case "_to_carry_bd_basket_a":
			return "_villager_with_backpack";
		case "practice_spear_t1":
			return "_villager_with_staff";
		case "simple_sparth_axe_t2":
			return "_villager_carry_axe";
		default:
			return "_villager_carry_right_hand";
		}
	}

	public static Tuple<string, Monster> GetRandomTownsManActionSetAndMonster(int race)
	{
		switch (MBRandom.RandomInt(3))
		{
		case 0:
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement");
			return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager"), monsterWithSuffix);
		}
		case 1:
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement_slow");
			return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_2"), monsterWithSuffix);
		}
		default:
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement");
			return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_3"), monsterWithSuffix);
		}
		}
	}

	public static Tuple<string, Monster> GetRandomTownsWomanActionSetAndMonster(int race)
	{
		Monster monsterWithSuffix;
		if (MBRandom.RandomInt(4) == 0)
		{
			monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement_fast");
			return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: true, "_villager"), monsterWithSuffix);
		}
		monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement_slow");
		return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: true, "_villager_2"), monsterWithSuffix);
	}

	private static LocationCharacter CreateTownsMan(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsman = culture.Townsman;
		Tuple<string, Monster> randomTownsManActionSetAndMonster = GetRandomTownsManActionSetAndMonster(townsman.Race);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman)).Monster(randomTownsManActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common", fixedLocation: false, relation, randomTownsManActionSetAndMonster.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateTownsManForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsman = culture.Townsman;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsman.Race, "_settlement_slow");
		string actionSetCode = ((!(culture.StringId.ToLower() == "aserai") && !(culture.StringId.ToLower() == "khuzait")) ? ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townsman.IsFemale, "_villager_in_tavern") : ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townsman.IsFemale, "_villager_in_aserai_tavern"));
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out var minimumAge, out var maximumAge, "TavernVisitor");
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, relation, actionSetCode, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateTownsWomanForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townswoman = culture.Townswoman;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswoman.Race, "_settlement_slow");
		string actionSetCode = ((!(culture.StringId.ToLower() == "aserai") && !(culture.StringId.ToLower() == "khuzait")) ? ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townswoman.IsFemale, "_warrior_in_tavern") : ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townswoman.IsFemale, "_warrior_in_aserai_tavern"));
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out var minimumAge, out var maximumAge, "TavernVisitor");
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", fixedLocation: true, relation, actionSetCode, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateTownsManCarryingStuff(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsman = culture.Townsman;
		string randomStuff = SettlementHelper.GetRandomStuff(isFemale: false);
		Monster monster;
		string actionSetSuffixAndMonsterForItem = GetActionSetSuffixAndMonsterForItem(randomStuff, townsman.Race, isFemale: false, out monster);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out var minimumAge, out var maximumAge, "TownsfolkCarryingStuff");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(townsman)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(randomStuff);
		LocationCharacter locationCharacter = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townsman.IsFemale, actionSetSuffixAndMonsterForItem), useCivilianEquipment: true, isFixedCharacter: false, itemObject);
		if (itemObject == null)
		{
			locationCharacter.PrefabNamesForBones.Add(agentData.AgentMonster.MainHandItemBoneIndex, randomStuff);
		}
		return locationCharacter;
	}

	private static LocationCharacter CreateTownsWoman(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townswoman = culture.Townswoman;
		Tuple<string, Monster> randomTownsWomanActionSetAndMonster = GetRandomTownsWomanActionSetAndMonster(townswoman.Race);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman)).Monster(randomTownsWomanActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common", fixedLocation: false, relation, randomTownsWomanActionSetAndMonster.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateMaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsmanChild = culture.TownsmanChild;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsmanChild.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsmanChild, out var minimumAge, out var maximumAge, "Child");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(townsmanChild)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townsmanChild.IsFemale, "_child"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateFemaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townswomanChild = culture.TownswomanChild;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswomanChild.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswomanChild, out var minimumAge, out var maximumAge, "Child");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(townswomanChild)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townswomanChild.IsFemale, "_child"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateMaleTeenager(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsmanTeenager = culture.TownsmanTeenager;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsmanTeenager.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsmanTeenager, out var minimumAge, out var maximumAge, "Teenager");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(townsmanTeenager)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townsmanTeenager.IsFemale, "_villager"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateFemaleTeenager(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townswomanTeenager = culture.TownswomanTeenager;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswomanTeenager.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswomanTeenager, out var minimumAge, out var maximumAge, "Teenager");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(townswomanTeenager)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townswomanTeenager.IsFemale, "_villager"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateTownsWomanCarryingStuff(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townswoman = culture.Townswoman;
		string randomStuff = SettlementHelper.GetRandomStuff(isFemale: true);
		Monster monster;
		string actionSetSuffixAndMonsterForItem = GetActionSetSuffixAndMonsterForItem(randomStuff, townswoman.Race, isFemale: false, out monster);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out var minimumAge, out var maximumAge, "TownsfolkCarryingStuff");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(townswoman)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(randomStuff);
		LocationCharacter locationCharacter = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townswoman.IsFemale, actionSetSuffixAndMonsterForItem), useCivilianEquipment: true, isFixedCharacter: false, itemObject);
		if (itemObject == null)
		{
			locationCharacter.PrefabNamesForBones.Add(agentData.AgentMonster.MainHandItemBoneIndex, randomStuff);
		}
		return locationCharacter;
	}

	public static LocationCharacter CreateBroomsWoman(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townswoman = culture.Townswoman;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswoman.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out var minimumAge, out var maximumAge, "BroomsWoman");
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "spawnpoint_cleaner", fixedLocation: false, relation, null, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateDancer(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject femaleDancer = culture.FemaleDancer;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(femaleDancer.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(femaleDancer, out var minimumAge, out var maximumAge, "Dancer");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(femaleDancer)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_dancer", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_dancer"), useCivilianEquipment: true);
	}

	public static LocationCharacter CreateMaleBeggar(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject beggar = culture.Beggar;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(beggar.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(beggar, out var minimumAge, out var maximumAge, "Beggar");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(beggar)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_beggar", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_beggar"), useCivilianEquipment: true);
	}

	public static LocationCharacter CreateFemaleBeggar(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject femaleBeggar = culture.FemaleBeggar;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(femaleBeggar.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(femaleBeggar, out var minimumAge, out var maximumAge, "Beggar");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(femaleBeggar)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_beggar", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_beggar"), useCivilianEquipment: true);
	}
}
