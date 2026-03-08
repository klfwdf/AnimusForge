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

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000CE RID: 206
	public class CommonTownsfolkCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000927 RID: 2343 RVA: 0x00043391 File Offset: 0x00041591
		private float GetSpawnRate(Settlement settlement)
		{
			return this.TimeOfDayPercentage() * this.GetProsperityMultiplier(settlement.SettlementComponent) * this.GetWeatherEffectMultiplier(settlement);
		}

		// Token: 0x06000928 RID: 2344 RVA: 0x000433AE File Offset: 0x000415AE
		private float GetConfigValue()
		{
			return BannerlordConfig.CivilianAgentCount;
		}

		// Token: 0x06000929 RID: 2345 RVA: 0x000433B5 File Offset: 0x000415B5
		private float GetProsperityMultiplier(SettlementComponent settlement)
		{
			return ((float)settlement.GetProsperityLevel() + 1f) / 3f;
		}

		// Token: 0x0600092A RID: 2346 RVA: 0x000433CC File Offset: 0x000415CC
		private float GetWeatherEffectMultiplier(Settlement settlement)
		{
			MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position.ToVec2());
			if (weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain)
			{
				return 0.15f;
			}
			if (weatherEventInPosition != MapWeatherModel.WeatherEvent.Blizzard)
			{
				return 1f;
			}
			return 0.4f;
		}

		// Token: 0x0600092B RID: 2347 RVA: 0x00043418 File Offset: 0x00041618
		private float TimeOfDayPercentage()
		{
			int num = MathF.Ceiling((float)CampaignTime.HoursInDay * 0.625f);
			return 1f - MathF.Abs(CampaignTime.Now.CurrentHourInDay - (float)num) / (float)num;
		}

		// Token: 0x0600092C RID: 2348 RVA: 0x00043455 File Offset: 0x00041655
		public override void RegisterEvents()
		{
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
		}

		// Token: 0x0600092D RID: 2349 RVA: 0x0004346E File Offset: 0x0004166E
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600092E RID: 2350 RVA: 0x00043470 File Offset: 0x00041670
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
			if (!settlement.IsCastle)
			{
				Location locationWithId = settlement.LocationComplex.GetLocationWithId("center");
				Location locationWithId2 = settlement.LocationComplex.GetLocationWithId("tavern");
				if (CampaignMission.Current.Location == locationWithId)
				{
					this.AddPeopleToTownCenter(settlement, unusedUsablePointCount, CampaignTime.Now.IsDayTime);
				}
				if (CampaignMission.Current.Location == locationWithId2)
				{
					this.AddPeopleToTownTavern(settlement, unusedUsablePointCount);
				}
			}
		}

		// Token: 0x0600092F RID: 2351 RVA: 0x000434EC File Offset: 0x000416EC
		private void AddPeopleToTownTavern(Settlement settlement, Dictionary<string, int> unusedUsablePointCount)
		{
			Location locationWithId = settlement.LocationComplex.GetLocationWithId("tavern");
			int num;
			unusedUsablePointCount.TryGetValue("npc_common", out num);
			MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position.ToVec2());
			bool flag = weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
			if (num > 0)
			{
				int num2 = (int)((float)num * (0.3f + (flag ? 0.2f : 0f)));
				if (num2 > 0)
				{
					locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateTownsManForTavern), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num2);
				}
				int num3 = (int)((float)num * (0.1f + (flag ? 0.2f : 0f)));
				if (num3 > 0)
				{
					locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateTownsWomanForTavern), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num3);
				}
			}
		}

		// Token: 0x06000930 RID: 2352 RVA: 0x000435C4 File Offset: 0x000417C4
		private void AddPeopleToTownCenter(Settlement settlement, Dictionary<string, int> unusedUsablePointCount, bool isDayTime)
		{
			Location locationWithId = settlement.LocationComplex.GetLocationWithId("center");
			CultureObject culture = settlement.Culture;
			int num;
			unusedUsablePointCount.TryGetValue("npc_common", out num);
			int num2;
			unusedUsablePointCount.TryGetValue("npc_common_limited", out num2);
			float num3 = (float)(num + num2) * 0.65000004f;
			if (num3 != 0f)
			{
				float num4 = MBMath.ClampFloat(this.GetConfigValue() / num3, 0f, 1f);
				float num5 = this.GetSpawnRate(settlement) * num4;
				if (num > 0)
				{
					int num6 = (int)((float)num * 0.2f * num5);
					if (num6 > 0)
					{
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateTownsMan), culture, LocationCharacter.CharacterRelations.Neutral, num6);
					}
					int num7 = (int)((float)num * 0.15f * num5);
					if (num7 > 0)
					{
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateTownsWoman), culture, LocationCharacter.CharacterRelations.Neutral, num7);
					}
				}
				MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position.ToVec2());
				bool flag = weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
				if (isDayTime && !flag)
				{
					if (num2 > 0)
					{
						int num8 = (int)((float)num2 * 0.15f * num5);
						if (num8 > 0)
						{
							locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateTownsManCarryingStuff), culture, LocationCharacter.CharacterRelations.Neutral, num8);
						}
						int num9 = (int)((float)num2 * 0.1f * num5);
						if (num9 > 0)
						{
							locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateTownsWomanCarryingStuff), culture, LocationCharacter.CharacterRelations.Neutral, num9);
						}
						int num10 = (int)((float)num2 * 0.05f * num5);
						if (num10 > 0)
						{
							locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateMaleChild), culture, LocationCharacter.CharacterRelations.Neutral, num10);
							locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateFemaleChild), culture, LocationCharacter.CharacterRelations.Neutral, num10);
							locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateMaleTeenager), culture, LocationCharacter.CharacterRelations.Neutral, num10);
							locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateFemaleTeenager), culture, LocationCharacter.CharacterRelations.Neutral, num10);
						}
					}
					int num11 = 0;
					if (unusedUsablePointCount.TryGetValue("spawnpoint_cleaner", out num11))
					{
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateBroomsWoman), culture, LocationCharacter.CharacterRelations.Neutral, num11);
					}
					if (unusedUsablePointCount.TryGetValue("npc_dancer", out num11))
					{
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateDancer), culture, LocationCharacter.CharacterRelations.Neutral, num11);
					}
					if (settlement.IsTown && unusedUsablePointCount.TryGetValue("npc_beggar", out num11))
					{
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateFemaleBeggar), culture, LocationCharacter.CharacterRelations.Neutral, (num11 == 1) ? 0 : (num11 / 2));
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(CommonTownsfolkCampaignBehavior.CreateMaleBeggar), culture, LocationCharacter.CharacterRelations.Neutral, (num11 == 1) ? 1 : (num11 / 2));
					}
				}
			}
		}

		// Token: 0x06000931 RID: 2353 RVA: 0x00043844 File Offset: 0x00041A44
		public static string GetActionSetSuffixAndMonsterForItem(string itemId, int race, bool isFemale, out Monster monster)
		{
			monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement");
			uint num = <PrivateImplementationDetails>.ComputeStringHash(itemId);
			if (num <= 2354022098U)
			{
				if (num <= 524654717U)
				{
					if (num != 330511441U)
					{
						if (num != 423989003U)
						{
							if (num != 524654717U)
							{
								goto IL_20B;
							}
							if (!(itemId == "_to_carry_bed_convolute_g"))
							{
								goto IL_20B;
							}
							return "_villager_carry_on_shoulder";
						}
						else
						{
							if (!(itemId == "_to_carry_bed_convolute_a"))
							{
								goto IL_20B;
							}
							return "_villager_carry_front";
						}
					}
					else
					{
						if (!(itemId == "_to_carry_bd_basket_a"))
						{
							goto IL_20B;
						}
						return "_villager_with_backpack";
					}
				}
				else if (num != 1406916035U)
				{
					if (num != 1726492488U)
					{
						if (num != 2354022098U)
						{
							goto IL_20B;
						}
						if (!(itemId == "_to_carry_kitchen_pot_c"))
						{
							goto IL_20B;
						}
						return "_villager_carry_right_hand";
					}
					else if (!(itemId == "_to_carry_foods_watermelon_a"))
					{
						goto IL_20B;
					}
				}
				else
				{
					if (!(itemId == "_to_carry_arm_kitchen_pot_c"))
					{
						goto IL_20B;
					}
					return "_villager_carry_right_arm";
				}
			}
			else if (num <= 3512086304U)
			{
				if (num != 2481184366U)
				{
					if (num != 3004030871U)
					{
						if (num != 3512086304U)
						{
							goto IL_20B;
						}
						if (!(itemId == "_to_carry_bd_fabric_c"))
						{
							goto IL_20B;
						}
					}
					else
					{
						if (!(itemId == "_to_carry_foods_basket_apple"))
						{
							goto IL_20B;
						}
						return "_villager_carry_over_head_v2";
					}
				}
				else
				{
					if (!(itemId == "_to_carry_kitchen_pitcher_a"))
					{
						goto IL_20B;
					}
					return "_villager_carry_over_head";
				}
			}
			else if (num <= 3737849652U)
			{
				if (num != 3710634116U)
				{
					if (num != 3737849652U)
					{
						goto IL_20B;
					}
					if (!(itemId == "_to_carry_merchandise_hides_b"))
					{
						goto IL_20B;
					}
					return "_villager_with_backpack";
				}
				else
				{
					if (!(itemId == "practice_spear_t1"))
					{
						goto IL_20B;
					}
					return "_villager_with_staff";
				}
			}
			else if (num != 4035495654U)
			{
				if (num != 4038602446U)
				{
					goto IL_20B;
				}
				if (!(itemId == "simple_sparth_axe_t2"))
				{
					goto IL_20B;
				}
				return "_villager_carry_axe";
			}
			else
			{
				if (!(itemId == "_to_carry_foods_pumpkin_a"))
				{
					goto IL_20B;
				}
				return "_villager_carry_front_v2";
			}
			return "_villager_carry_right_side";
			IL_20B:
			return "_villager_carry_right_hand";
		}

		// Token: 0x06000932 RID: 2354 RVA: 0x00043A64 File Offset: 0x00041C64
		public static Tuple<string, Monster> GetRandomTownsManActionSetAndMonster(int race)
		{
			int num = MBRandom.RandomInt(3);
			Monster monsterWithSuffix;
			if (num == 0)
			{
				monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement");
				return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, false, "_villager"), monsterWithSuffix);
			}
			if (num != 1)
			{
				monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement");
				return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, false, "_villager_3"), monsterWithSuffix);
			}
			monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement_slow");
			return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, false, "_villager_2"), monsterWithSuffix);
		}

		// Token: 0x06000933 RID: 2355 RVA: 0x00043AE0 File Offset: 0x00041CE0
		public static Tuple<string, Monster> GetRandomTownsWomanActionSetAndMonster(int race)
		{
			Monster monsterWithSuffix;
			if (MBRandom.RandomInt(4) == 0)
			{
				monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement_fast");
				return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, true, "_villager"), monsterWithSuffix);
			}
			monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(race, "_settlement_slow");
			return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, true, "_villager_2"), monsterWithSuffix);
		}

		// Token: 0x06000934 RID: 2356 RVA: 0x00043B34 File Offset: 0x00041D34
		private static LocationCharacter CreateTownsMan(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townsman = culture.Townsman;
			Tuple<string, Monster> randomTownsManActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsManActionSetAndMonster(townsman.Race);
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out minValue, out maxValue, "");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman, -1, null, default(UniqueTroopDescriptor))).Monster(randomTownsManActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common", false, relation, randomTownsManActionSetAndMonster.Item1, true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000935 RID: 2357 RVA: 0x00043BD0 File Offset: 0x00041DD0
		private static LocationCharacter CreateTownsManForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townsman = culture.Townsman;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsman.Race, "_settlement_slow");
			string actionSetCode;
			if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
			{
				actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townsman.IsFemale, "_villager_in_aserai_tavern");
			}
			else
			{
				actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townsman.IsFemale, "_villager_in_tavern");
			}
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out minValue, out maxValue, "TavernVisitor");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000936 RID: 2358 RVA: 0x00043CBC File Offset: 0x00041EBC
		private static LocationCharacter CreateTownsWomanForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townswoman = culture.Townswoman;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswoman.Race, "_settlement_slow");
			string actionSetCode;
			if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
			{
				actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townswoman.IsFemale, "_warrior_in_aserai_tavern");
			}
			else
			{
				actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townswoman.IsFemale, "_warrior_in_tavern");
			}
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out minValue, out maxValue, "TavernVisitor");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000937 RID: 2359 RVA: 0x00043DA8 File Offset: 0x00041FA8
		private static LocationCharacter CreateTownsManCarryingStuff(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townsman = culture.Townsman;
			string randomStuff = SettlementHelper.GetRandomStuff(false);
			Monster monster;
			string actionSetSuffixAndMonsterForItem = CommonTownsfolkCampaignBehavior.GetActionSetSuffixAndMonsterForItem(randomStuff, townsman.Race, false, out monster);
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out minValue, out maxValue, "TownsfolkCarryingStuff");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(townsman, -1, null, default(UniqueTroopDescriptor))).Monster(monster).Age(MBRandom.RandomInt(minValue, maxValue));
			ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>(randomStuff);
			LocationCharacter locationCharacter = new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common_limited", false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townsman.IsFemale, actionSetSuffixAndMonsterForItem), true, false, @object, false, false, true, null, false);
			if (@object == null)
			{
				locationCharacter.PrefabNamesForBones.Add(agentData.AgentMonster.MainHandItemBoneIndex, randomStuff);
			}
			return locationCharacter;
		}

		// Token: 0x06000938 RID: 2360 RVA: 0x00043E94 File Offset: 0x00042094
		private static LocationCharacter CreateTownsWoman(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townswoman = culture.Townswoman;
			Tuple<string, Monster> randomTownsWomanActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsWomanActionSetAndMonster(townswoman.Race);
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out minValue, out maxValue, "");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default(UniqueTroopDescriptor))).Monster(randomTownsWomanActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common", false, relation, randomTownsWomanActionSetAndMonster.Item1, true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000939 RID: 2361 RVA: 0x00043F30 File Offset: 0x00042130
		private static LocationCharacter CreateMaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townsmanChild = culture.TownsmanChild;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsmanChild.Race, "_child");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsmanChild, out minValue, out maxValue, "Child");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(townsmanChild, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townsmanChild.IsFemale, "_child"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x0600093A RID: 2362 RVA: 0x00043FE4 File Offset: 0x000421E4
		private static LocationCharacter CreateFemaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townswomanChild = culture.TownswomanChild;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswomanChild.Race, "_child");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswomanChild, out minValue, out maxValue, "Child");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(townswomanChild, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townswomanChild.IsFemale, "_child"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x0600093B RID: 2363 RVA: 0x00044098 File Offset: 0x00042298
		private static LocationCharacter CreateMaleTeenager(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townsmanTeenager = culture.TownsmanTeenager;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsmanTeenager.Race, "_child");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsmanTeenager, out minValue, out maxValue, "Teenager");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(townsmanTeenager, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townsmanTeenager.IsFemale, "_villager"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x0600093C RID: 2364 RVA: 0x0004414C File Offset: 0x0004234C
		private static LocationCharacter CreateFemaleTeenager(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townswomanTeenager = culture.TownswomanTeenager;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswomanTeenager.Race, "_child");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswomanTeenager, out minValue, out maxValue, "Teenager");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(townswomanTeenager, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townswomanTeenager.IsFemale, "_villager"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x0600093D RID: 2365 RVA: 0x00044200 File Offset: 0x00042400
		private static LocationCharacter CreateTownsWomanCarryingStuff(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townswoman = culture.Townswoman;
			string randomStuff = SettlementHelper.GetRandomStuff(true);
			Monster monster;
			string actionSetSuffixAndMonsterForItem = CommonTownsfolkCampaignBehavior.GetActionSetSuffixAndMonsterForItem(randomStuff, townswoman.Race, false, out monster);
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out minValue, out maxValue, "TownsfolkCarryingStuff");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default(UniqueTroopDescriptor))).Monster(monster).Age(MBRandom.RandomInt(minValue, maxValue));
			ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>(randomStuff);
			LocationCharacter locationCharacter = new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common_limited", false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townswoman.IsFemale, actionSetSuffixAndMonsterForItem), true, false, @object, false, false, true, null, false);
			if (@object == null)
			{
				locationCharacter.PrefabNamesForBones.Add(agentData.AgentMonster.MainHandItemBoneIndex, randomStuff);
			}
			return locationCharacter;
		}

		// Token: 0x0600093E RID: 2366 RVA: 0x000442EC File Offset: 0x000424EC
		public static LocationCharacter CreateBroomsWoman(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject townswoman = culture.Townswoman;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswoman.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out minValue, out maxValue, "BroomsWoman");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "spawnpoint_cleaner", false, relation, null, true, false, null, false, false, true, null, false);
		}

		// Token: 0x0600093F RID: 2367 RVA: 0x00044384 File Offset: 0x00042584
		private static LocationCharacter CreateDancer(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject femaleDancer = culture.FemaleDancer;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(femaleDancer.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(femaleDancer, out minValue, out maxValue, "Dancer");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(femaleDancer, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_dancer", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_dancer"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000940 RID: 2368 RVA: 0x00044438 File Offset: 0x00042638
		public static LocationCharacter CreateMaleBeggar(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject beggar = culture.Beggar;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(beggar.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(beggar, out minValue, out maxValue, "Beggar");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(beggar, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_beggar", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_beggar"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000941 RID: 2369 RVA: 0x000444EC File Offset: 0x000426EC
		public static LocationCharacter CreateFemaleBeggar(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject femaleBeggar = culture.FemaleBeggar;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(femaleBeggar.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(femaleBeggar, out minValue, out maxValue, "Beggar");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(femaleBeggar, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_beggar", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_beggar"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x04000464 RID: 1124
		public const float TownsmanSpawnPercentageMale = 0.2f;

		// Token: 0x04000465 RID: 1125
		public const float TownsmanSpawnPercentageFemale = 0.15f;

		// Token: 0x04000466 RID: 1126
		public const float TownsmanSpawnPercentageLimitedMale = 0.15f;

		// Token: 0x04000467 RID: 1127
		public const float TownsmanSpawnPercentageLimitedFemale = 0.1f;

		// Token: 0x04000468 RID: 1128
		public const float TownOtherPeopleSpawnPercentage = 0.05f;

		// Token: 0x04000469 RID: 1129
		public const float TownsmanSpawnPercentageTavernMale = 0.3f;

		// Token: 0x0400046A RID: 1130
		public const float TownsmanSpawnPercentageTavernFemale = 0.1f;

		// Token: 0x0400046B RID: 1131
		public const float BeggarSpawnPercentage = 0.33f;
	}
}
