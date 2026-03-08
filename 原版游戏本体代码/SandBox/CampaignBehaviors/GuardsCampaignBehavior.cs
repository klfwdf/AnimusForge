using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D5 RID: 213
	public class GuardsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060009DC RID: 2524 RVA: 0x0004AD54 File Offset: 0x00048F54
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
		}

		// Token: 0x060009DD RID: 2525 RVA: 0x0004AD84 File Offset: 0x00048F84
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x0004AD86 File Offset: 0x00048F86
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x060009DF RID: 2527 RVA: 0x0004AD8F File Offset: 0x00048F8F
		private float GetProsperityMultiplier(SettlementComponent settlement)
		{
			return ((float)settlement.GetProsperityLevel() + 1f) / 3f;
		}

		// Token: 0x060009E0 RID: 2528 RVA: 0x0004ADA4 File Offset: 0x00048FA4
		private void AddGarrisonAndPrisonCharacters(Settlement settlement)
		{
			this.InitializeGarrisonCharacters(settlement);
			Location locationWithId = settlement.LocationComplex.GetLocationWithId("center");
			CultureObject culture = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? settlement.MapFaction.Culture : settlement.Culture);
			locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreatePrisonGuard), culture, LocationCharacter.CharacterRelations.Neutral, 1);
		}

		// Token: 0x060009E1 RID: 2529 RVA: 0x0004AE00 File Offset: 0x00049000
		private void InitializeGarrisonCharacters(Settlement settlement)
		{
			this._garrisonTroops.Clear();
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				MobileParty garrisonParty = settlement.Town.GarrisonParty;
				if (garrisonParty != null)
				{
					foreach (TroopRosterElement troopRosterElement in garrisonParty.MemberRoster.GetTroopRoster())
					{
						if (troopRosterElement.Character.Occupation == Occupation.Soldier)
						{
							this._garrisonTroops.Add(new ValueTuple<CharacterObject, int>(troopRosterElement.Character, troopRosterElement.Number - troopRosterElement.WoundedNumber));
						}
					}
				}
			}
		}

		// Token: 0x060009E2 RID: 2530 RVA: 0x0004AEAC File Offset: 0x000490AC
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
			if (settlement.IsFortification)
			{
				this.AddGarrisonAndPrisonCharacters(settlement);
				if ((settlement.IsTown || settlement.IsCastle) && CampaignMission.Current != null)
				{
					Location location = CampaignMission.Current.Location;
					this.AddGuardsFromGarrison(settlement, unusedUsablePointCount, location);
				}
			}
		}

		// Token: 0x060009E3 RID: 2531 RVA: 0x0004AF00 File Offset: 0x00049100
		private void AddGuardsFromGarrison(Settlement settlement, Dictionary<string, int> unusedUsablePointCount, Location location)
		{
			int num;
			unusedUsablePointCount.TryGetValue("sp_guard", out num);
			int num2;
			unusedUsablePointCount.TryGetValue("sp_guard_with_spear", out num2);
			int num3;
			unusedUsablePointCount.TryGetValue("sp_guard_patrol", out num3);
			int num4;
			unusedUsablePointCount.TryGetValue("sp_guard_unarmed", out num4);
			int num5;
			unusedUsablePointCount.TryGetValue("sp_guard_castle", out num5);
			float prosperityMultiplier = this.GetProsperityMultiplier(settlement.SettlementComponent);
			float num6 = (settlement.IsCastle ? 1.6f : 0.4f);
			num = (int)((float)num * prosperityMultiplier);
			num2 = (int)((float)num2 * prosperityMultiplier);
			num3 = (int)((float)num3 * prosperityMultiplier);
			num4 = (int)((float)num4 * prosperityMultiplier * num6);
			if (num5 > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateCastleGuard), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num5);
			}
			if (num > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateStandGuard), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num);
			}
			if (num2 > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateStandGuardWithSpear), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num2);
			}
			if (num3 > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreatePatrollingGuard), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num3);
			}
			if (num4 > 0 && location != settlement.LocationComplex.GetLocationWithId("lordshall"))
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateUnarmedGuard), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num4);
			}
			if (location.StringId == "prison")
			{
				int num7;
				if (unusedUsablePointCount.TryGetValue("area_marker_1", out num7) && num7 > 0)
				{
					location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateStandGuard), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
				}
				if (unusedUsablePointCount.TryGetValue("area_marker_2", out num7) && num7 > 0)
				{
					location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateStandGuard), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
				}
				if (unusedUsablePointCount.TryGetValue("area_marker_3", out num7) && num7 > 0)
				{
					location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateStandGuard), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
				}
			}
		}

		// Token: 0x060009E4 RID: 2532 RVA: 0x0004B0E0 File Offset: 0x000492E0
		private static ItemObject GetSuitableSpear(CultureObject culture)
		{
			string objectName = ((culture.StringId == "battania") ? "northern_spear_2_t3" : "western_spear_3_t3");
			return MBObjectManager.Instance.GetObject<ItemObject>(objectName);
		}

		// Token: 0x060009E5 RID: 2533 RVA: 0x0004B118 File Offset: 0x00049318
		private AgentData TakeGuardAgentDataFromGarrisonTroopList(CultureObject culture, bool overrideWeaponWithSpear = false, bool unarmed = false)
		{
			CharacterObject guardRosterElement;
			if (this._garrisonTroops.Count > 0)
			{
				List<ValueTuple<ValueTuple<CharacterObject, int>, float>> list = new List<ValueTuple<ValueTuple<CharacterObject, int>, float>>();
				foreach (ValueTuple<CharacterObject, int> valueTuple in this._garrisonTroops)
				{
					list.Add(new ValueTuple<ValueTuple<CharacterObject, int>, float>(new ValueTuple<CharacterObject, int>(valueTuple.Item1, valueTuple.Item2), (float)valueTuple.Item1.Level));
				}
				int index;
				ValueTuple<CharacterObject, int> valueTuple2 = MBRandom.ChooseWeighted<ValueTuple<CharacterObject, int>>(list, out index);
				guardRosterElement = valueTuple2.Item1;
				if (valueTuple2.Item2 <= 1)
				{
					this._garrisonTroops.RemoveAt(index);
				}
				else
				{
					this._garrisonTroops[index] = new ValueTuple<CharacterObject, int>(valueTuple2.Item1, valueTuple2.Item2 - 1);
				}
			}
			else
			{
				guardRosterElement = culture.Guard;
			}
			return GuardsCampaignBehavior.PrepareGuardAgentDataFromGarrison(guardRosterElement, overrideWeaponWithSpear, unarmed);
		}

		// Token: 0x060009E6 RID: 2534 RVA: 0x0004B200 File Offset: 0x00049400
		private static AgentData PrepareGuardAgentDataFromGarrison(CharacterObject guardRosterElement, bool overrideWeaponWithSpear = false, bool unarmed = false)
		{
			Banner banner = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? PlayerEncounter.LocationEncounter.Settlement.OwnerClan.Banner : null);
			Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(guardRosterElement, false, Equipment.EquipmentType.Battle, -1);
			Dictionary<ItemObject.ItemTypeEnum, int> dictionary = new Dictionary<ItemObject.ItemTypeEnum, int>
			{
				{
					ItemObject.ItemTypeEnum.Polearm,
					0
				},
				{
					ItemObject.ItemTypeEnum.Crossbow,
					0
				},
				{
					ItemObject.ItemTypeEnum.Bolts,
					0
				},
				{
					ItemObject.ItemTypeEnum.Bow,
					0
				},
				{
					ItemObject.ItemTypeEnum.Arrows,
					0
				},
				{
					ItemObject.ItemTypeEnum.Sling,
					0
				},
				{
					ItemObject.ItemTypeEnum.SlingStones,
					0
				},
				{
					ItemObject.ItemTypeEnum.Thrown,
					0
				},
				{
					ItemObject.ItemTypeEnum.Shield,
					0
				}
			};
			int num = 0;
			for (int i = 0; i <= 4; i++)
			{
				if (randomEquipmentElements[i].Item != null)
				{
					if (dictionary.ContainsKey(randomEquipmentElements[i].Item.ItemType))
					{
						Dictionary<ItemObject.ItemTypeEnum, int> dictionary2 = dictionary;
						ItemObject.ItemTypeEnum itemType = randomEquipmentElements[i].Item.ItemType;
						int num2 = dictionary2[itemType];
						dictionary2[itemType] = num2 + 1;
					}
					else
					{
						num++;
					}
				}
			}
			if (overrideWeaponWithSpear && dictionary[ItemObject.ItemTypeEnum.Polearm] > 0)
			{
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary3 = dictionary;
				int num2 = dictionary3[ItemObject.ItemTypeEnum.Polearm];
				dictionary3[ItemObject.ItemTypeEnum.Polearm] = num2 - 1;
			}
			if (num > 0)
			{
				num--;
			}
			else if (dictionary[ItemObject.ItemTypeEnum.Polearm] > 0)
			{
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary4 = dictionary;
				int num2 = dictionary4[ItemObject.ItemTypeEnum.Polearm];
				dictionary4[ItemObject.ItemTypeEnum.Polearm] = num2 - 1;
			}
			else if (dictionary[ItemObject.ItemTypeEnum.Bow] > 0)
			{
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary5 = dictionary;
				int num2 = dictionary5[ItemObject.ItemTypeEnum.Arrows];
				dictionary5[ItemObject.ItemTypeEnum.Arrows] = num2 - 1;
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary6 = dictionary;
				num2 = dictionary6[ItemObject.ItemTypeEnum.Bow];
				dictionary6[ItemObject.ItemTypeEnum.Bow] = num2 - 1;
			}
			else if (dictionary[ItemObject.ItemTypeEnum.Crossbow] > 0)
			{
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary7 = dictionary;
				int num2 = dictionary7[ItemObject.ItemTypeEnum.Crossbow];
				dictionary7[ItemObject.ItemTypeEnum.Crossbow] = num2 - 1;
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary8 = dictionary;
				num2 = dictionary8[ItemObject.ItemTypeEnum.Bolts];
				dictionary8[ItemObject.ItemTypeEnum.Bolts] = num2 - 1;
			}
			else if (dictionary[ItemObject.ItemTypeEnum.Sling] > 0)
			{
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary9 = dictionary;
				int num2 = dictionary9[ItemObject.ItemTypeEnum.Sling];
				dictionary9[ItemObject.ItemTypeEnum.Sling] = num2 - 1;
				Dictionary<ItemObject.ItemTypeEnum, int> dictionary10 = dictionary;
				num2 = dictionary10[ItemObject.ItemTypeEnum.SlingStones];
				dictionary10[ItemObject.ItemTypeEnum.SlingStones] = num2 - 1;
			}
			for (int j = 4; j >= 0; j--)
			{
				if (randomEquipmentElements[j].Item != null)
				{
					bool flag = false;
					int num3;
					if (dictionary.TryGetValue(randomEquipmentElements[j].Item.ItemType, out num3))
					{
						if (num3 > 0)
						{
							flag = true;
							Dictionary<ItemObject.ItemTypeEnum, int> dictionary11 = dictionary;
							ItemObject.ItemTypeEnum itemType = randomEquipmentElements[j].Item.ItemType;
							int num2 = dictionary11[itemType];
							dictionary11[itemType] = num2 - 1;
						}
					}
					else if (num > 0)
					{
						flag = true;
						num--;
					}
					if (flag)
					{
						randomEquipmentElements.AddEquipmentToSlotWithoutAgent((EquipmentIndex)j, default(EquipmentElement));
					}
				}
			}
			if (overrideWeaponWithSpear)
			{
				if (!GuardsCampaignBehavior.IfEquipmentHasSpearSwapSlots(randomEquipmentElements))
				{
					ItemObject suitableSpear = GuardsCampaignBehavior.GetSuitableSpear(guardRosterElement.Culture);
					randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, new EquipmentElement(suitableSpear, null, null, false));
					GuardsCampaignBehavior.IfEquipmentHasSpearSwapSlots(randomEquipmentElements);
				}
			}
			else if (unarmed)
			{
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, default(EquipmentElement));
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, default(EquipmentElement));
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, default(EquipmentElement));
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, default(EquipmentElement));
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ExtraWeaponSlot, default(EquipmentElement));
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, default(EquipmentElement));
			}
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(guardRosterElement.Race, "_settlement");
			return new AgentData(new SimpleAgentOrigin(guardRosterElement, -1, banner, default(UniqueTroopDescriptor))).Equipment(randomEquipmentElements).Monster(monsterWithSuffix).NoHorses(true);
		}

		// Token: 0x060009E7 RID: 2535 RVA: 0x0004B58C File Offset: 0x0004978C
		private static bool IfEquipmentHasSpearSwapSlots(Equipment equipment)
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				ItemObject item = equipment[equipmentIndex].Item;
				if (item != null && item.WeaponComponent.PrimaryWeapon.IsPolearm)
				{
					Equipment.SwapWeapons(equipment, equipmentIndex, EquipmentIndex.WeaponItemBeginSlot);
					return true;
				}
			}
			return false;
		}

		// Token: 0x060009E8 RID: 2536 RVA: 0x0004B5D8 File Offset: 0x000497D8
		private void RemoveShields(Equipment equipment)
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				ItemObject item = equipment[equipmentIndex].Item;
				if (item != null && item.WeaponComponent.PrimaryWeapon.IsShield)
				{
					equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, default(EquipmentElement));
				}
			}
		}

		// Token: 0x060009E9 RID: 2537 RVA: 0x0004B628 File Offset: 0x00049828
		private LocationCharacter CreateCastleGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			AgentData agentData = this.TakeGuardAgentDataFromGarrisonTroopList(culture, true, false);
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors), "sp_guard_castle", true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), false, false, null, false, false, true, null, false);
		}

		// Token: 0x060009EA RID: 2538 RVA: 0x0004B680 File Offset: 0x00049880
		private LocationCharacter CreateStandGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			AgentData agentData = this.TakeGuardAgentDataFromGarrisonTroopList(culture, false, false);
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors), "sp_guard", true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), false, false, null, false, false, true, null, false);
		}

		// Token: 0x060009EB RID: 2539 RVA: 0x0004B6D8 File Offset: 0x000498D8
		private LocationCharacter CreateStandGuardWithSpear(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			AgentData agentData = this.TakeGuardAgentDataFromGarrisonTroopList(culture, true, false);
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors), "sp_guard_with_spear", true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), false, false, null, false, false, true, null, false);
		}

		// Token: 0x060009EC RID: 2540 RVA: 0x0004B730 File Offset: 0x00049930
		private LocationCharacter CreateUnarmedGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			AgentData agentData = this.TakeGuardAgentDataFromGarrisonTroopList(culture, false, true);
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "sp_guard_unarmed", true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_unarmed_guard"), false, false, null, false, false, true, null, false);
		}

		// Token: 0x060009ED RID: 2541 RVA: 0x0004B788 File Offset: 0x00049988
		private LocationCharacter CreatePatrollingGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			AgentData agentData = this.TakeGuardAgentDataFromGarrisonTroopList(culture, false, false);
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddPatrollingGuardBehaviors), "sp_guard_patrol", true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), false, false, null, false, false, true, null, false);
		}

		// Token: 0x060009EE RID: 2542 RVA: 0x0004B7E0 File Offset: 0x000499E0
		private LocationCharacter CreatePrisonGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject prisonGuard = culture.PrisonGuard;
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(prisonGuard.Race, "_settlement");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(prisonGuard, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix);
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors), "sp_prison_guard", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), false, true, null, false, false, true, null, false);
		}

		// Token: 0x060009EF RID: 2543 RVA: 0x0004B864 File Offset: 0x00049A64
		protected void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("disguise_start_conversation_alt", "start", "close_window", "{=uTycGRdI}You need to move along. I'm on duty right now and I can't spare any coin. May Heaven provide.", new ConversationSentence.OnConditionDelegate(this.conversation_disguised_start_on_condition_alt), null, 100, null);
			campaignGameStarter.AddDialogLine("disguise_start_conversation", "start", "close_window", "{=P98iCLjl}Get out of my face, you vile beggar.[if:convo_angry]", new ConversationSentence.OnConditionDelegate(this.conversation_disguised_start_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("prison_guard_start_criminal", "start", "prison_guard_talk_criminal", "{=0UUCTaEj}We hear a lot of complaints about you lately. You better start behaving or you'll get yourself a good flogging.[if:convo_mocking_revenge]", new ConversationSentence.OnConditionDelegate(this.conversation_prison_guard_criminal_start_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("prison_guard_ask_criminal", "prison_guard_talk_criminal", "prison_guard_talk", "{=XqTa0iQZ}What do you want, you degenerate?[if:convo_stern]", null, null, 100, null);
			campaignGameStarter.AddDialogLine("prison_guard_start", "start", "prison_guard_talk", "{=6SppoTum}Yes? What do you want?", new ConversationSentence.OnConditionDelegate(this.conversation_prison_guard_start_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("prison_guard_ask_prisoners", "prison_guard_talk", "prison_guard_ask_prisoners", "{=av0bRae8}Who is imprisoned here?", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_guard_ask_prisoner_talk", "prison_guard_talk", "close_window", "{=QxIXbHai}I want to speak with a prisoner (Cheat).", new ConversationSentence.OnConditionDelegate(this.conversation_prison_guard_visit_prison_cheat_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_prison_guard_visit_prison_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_guard_ask_prisoner_talk_2", "prison_guard_talk", "prison_guard_visit_prison", "{=EGI6ztlH}I want to speak with a prisoner.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_guard_talk_end", "prison_guard_talk", "close_window", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("prison_guard_talk_about_prisoners", "prison_guard_ask_prisoners", "prison_guard_talk", "{=2eydhtcz}Currently, {PRISONER_NAMES} {?IS_PLURAL}are{?}is{\\?} imprisoned here.", new ConversationSentence.OnConditionDelegate(this.conversation_prison_guard_talk_about_prisoners_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("prison_guard_visit_prison_ask_for_permission", "prison_guard_visit_prison", "prison_guard_visit_prison_ask_for_permission_answer", "{=XN0XZAkI}I can't let you in. My {?SETTLEMENT_OWNER.GENDER}Lady{?}Lord{\\?} {SETTLEMENT_OWNER.NAME} would be furious.", new ConversationSentence.OnConditionDelegate(this.conversation_prison_guard_reject_visit_prison_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("prison_guard_visit_prison", "prison_guard_visit_prison", "close_window", "{=XWpEpaQ4}Of course, {?PLAYER.GENDER}madam{?}sir{\\?}. Go in.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_prison_guard_visit_prison_on_consequence), 100, null);
			campaignGameStarter.AddPlayerLine("prison_guard_visit_prison_ask_answer", "prison_guard_visit_prison_ask_for_permission_answer", "prison_guard_visit_prison_ask_for_permission_guard_answer", "{=k3b5KqSc}Come on now. I thought you were the boss here.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("prison_guard_visit_prison_ask_answer_3", "prison_guard_visit_prison_ask_for_permission_guard_answer", "prison_guard_visit_prison_ask_for_permission_answer_options", "{=JaAltoKP}Um... What are you saying?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("prison_guard_visit_permission_try_bribe", "prison_guard_visit_prison_ask_for_permission_answer_options", "prison_guard_bribe_answer_satisfied", "{=dY3Vazug}I found a purse with {AMOUNT}{GOLD_ICON} a few paces away. I reckon it belongs to you.", new ConversationSentence.OnConditionDelegate(this.prison_guard_visit_permission_try_bribe_on_condition), null, 100, new ConversationSentence.OnClickableConditionDelegate(this.can_player_bribe_to_prison_guard_clickable), null);
			campaignGameStarter.AddPlayerLine("prison_guard_visit_prison_ask_answer_3_2", "prison_guard_visit_prison_ask_for_permission_answer_options", "close_window", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("prison_guard_visit_prison_nobody_inside", "prison_guard_visit_prison", "prison_guard_talk", "{=rVHbbrCQ}We're not holding anyone in here right now. There's no reason for you to go in.[ib:closed]", new ConversationSentence.OnConditionDelegate(this.conversation_prison_guard_visit_prison_nobody_inside_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("prison_guard_visit_empty_prison", "prison_guard_visit_prison_nobody_1", "close_window", "{=b3KFoJJ8}All right then. I'll have a look at the prison.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_prison_guard_visit_prison_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_guard_visit_empty_prison_2", "prison_guard_visit_prison_nobody_2", "close_window", "{=b3KFoJJ8}All right then. I'll have a look at the prison.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_guard_not_visit_empty_prison", "prison_guard_visit_prison_nobody_1", "close_window", "{=L5vAhxhO}I have more important business to do.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_guard_not_visit_empty_prison_2", "prison_guard_visit_prison_nobody_2", "close_window", "{=L5vAhxhO}I have more important business to do.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_guard_visit_permission_leave", "prison_guard_visit_prison_2", "close_window", "{=qPRl07mD}All right then. I'll try that.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("prison_guard_visit_permission_bribe", "prison_guard_bribe_answer_satisfied", "close_window", "{=fCrVeHP3}Ah! I was looking for this all day. How good of you to bring it back {?PLAYER.GENDER}madam{?}sir{\\?}. Well, now that I know what an honest {?PLAYER.GENDER}lady{?}man{\\?} you are, there can be no harm in letting you inside for a look. Go in.... Just so you know, though -- I'll be hanging onto the keys, in case you were thinking about undoing anyone's chains.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_prison_guard_visit_permission_bribe_on_consequence), 100, null);
			campaignGameStarter.AddPlayerLine("prison_guard_visit_permission_try_break", "prison_guard_visit_prison_4", "prison_guard_visit_break", "{=htfLEQlf}Give me the keys to the cells -- now!", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("prison_guard_visit_break", "prison_guard_visit_break", "close_window", "{=Kto7RWKE}Help! Help! Prison break!", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_prison_guard_visit_break_on_consequence), 100, null);
			campaignGameStarter.AddDialogLine("castle_guard_start_criminal", "start", "castle_guard_talk_criminal", "{=0UUCTaEj}We hear a lot of complaints about you lately. You better start behaving or you'll get yourself a good flogging.[if:convo_mocking_revenge]", new ConversationSentence.OnConditionDelegate(this.conversation_castle_guard_criminal_start_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("castle_guard_ask_criminal", "castle_guard_talk_criminal", "castle_guard_talk", "{=XqTa0iQZ}What do you want, you degenerate?[if:convo_stern]", null, null, 100, null);
			campaignGameStarter.AddDialogLine("castle_guard_start", "start", "castle_guard_talk", "{=6SppoTum}Yes? What do you want?", new ConversationSentence.OnConditionDelegate(this.conversation_castle_guard_start_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("guard_start", "start", "close_window", "{=!}{GUARD_COMMENT}", new ConversationSentence.OnConditionDelegate(this.conversation_guard_start_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("player_ask_for_permission_to_enter_lords_hall", "castle_guard_talk", "player_ask_permission_to_lords_hall", "{=b2h3r1kL}I want to visit the lord's hall.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("player_ask_for_permission_to_enter_lords_hall_2", "castle_guard_talk", "close_window", "{=never_mind}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("castle_guard_no_permission_nobody_inside", "player_ask_permission_to_lords_hall", "permisson_for_lords_hall", "{=RJtCakaG}There is nobody inside to receive you right now.", new ConversationSentence.OnConditionDelegate(this.conversation_castle_guard_nobody_inside_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("castle_guard_player_can_enter", "player_ask_permission_to_lords_hall", "close_window", "{=bbroVUrD}Of course, my {?PLAYER.GENDER}lady{?}lord{\\?}.", new ConversationSentence.OnConditionDelegate(this.conversation_castle_guard_player_can_enter_lordshall_condition), delegate()
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += this.OpenLordsHallMission;
			}, 100, null);
			campaignGameStarter.AddDialogLine("castle_guard_no_permission", "player_ask_permission_to_lords_hall", "permisson_for_lords_hall", "{=rcoESVVz}Sorry, but we don't know you. We can't just let anyone in. (Not enough renown)", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("player_bribe_to_enter_lords_hall", "permisson_for_lords_hall", "player_bribe_to_castle_guard", "{=7wkHMnNM}Maybe {AMOUNT}{GOLD_ICON} will help you to remember me.", new ConversationSentence.OnConditionDelegate(this.conversation_player_bribe_to_enter_lords_hall_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_player_bribe_to_enter_lords_hall_on_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(this.conversation_player_bribe_to_enter_lords_hall_on_clickable_condition), null);
			campaignGameStarter.AddPlayerLine("player_not_bribe_to_enter_lords_hall", "permisson_for_lords_hall", "close_window", "{=xatWDriV}Never mind then.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("castle_guard_let_player_in", "player_bribe_to_castle_guard", "close_window", "{=g5ofoKa8}Yeah... Now I remember you.", null, delegate()
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += this.OpenLordsHallMission;
			}, 100, null);
		}

		// Token: 0x060009F0 RID: 2544 RVA: 0x0004BE38 File Offset: 0x0004A038
		private bool conversation_prison_guard_criminal_start_on_condition()
		{
			return !Campaign.Current.IsMainHeroDisguised && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.PrisonGuard && Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction && (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction));
		}

		// Token: 0x060009F1 RID: 2545 RVA: 0x0004BEDC File Offset: 0x0004A0DC
		private bool conversation_prison_guard_start_on_condition()
		{
			return !Campaign.Current.IsMainHeroDisguised && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.PrisonGuard && (Settlement.CurrentSettlement.MapFaction == Hero.MainHero.MapFaction || (Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction)));
		}

		// Token: 0x060009F2 RID: 2546 RVA: 0x0004BF9C File Offset: 0x0004A19C
		private bool conversation_prison_guard_talk_about_prisoners_on_condition()
		{
			List<CharacterObject> prisonerHeroes = Settlement.CurrentSettlement.SettlementComponent.GetPrisonerHeroes();
			if (prisonerHeroes.Count == 0)
			{
				MBTextManager.SetTextVariable("PRISONER_NAMES", GameTexts.FindText("str_nobody", null), false);
				MBTextManager.SetTextVariable("IS_PLURAL", "0", false);
			}
			else
			{
				for (int i = 0; i < prisonerHeroes.Count; i++)
				{
					if (i == 0)
					{
						MBTextManager.SetTextVariable("LEFT", prisonerHeroes[i].Name, false);
					}
					else
					{
						MBTextManager.SetTextVariable("RIGHT", prisonerHeroes[i].Name, false);
						MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString(), false);
					}
				}
				MBTextManager.SetTextVariable("IS_PLURAL", (prisonerHeroes.Count > 1) ? 1 : 0);
				MBTextManager.SetTextVariable("PRISONER_NAMES", GameTexts.FindText("str_LEFT_ONLY", null).ToString(), false);
			}
			return true;
		}

		// Token: 0x060009F3 RID: 2547 RVA: 0x0004C07E File Offset: 0x0004A27E
		private bool conversation_prison_guard_visit_prison_cheat_on_condition()
		{
			return Game.Current.IsDevelopmentMode;
		}

		// Token: 0x060009F4 RID: 2548 RVA: 0x0004C08C File Offset: 0x0004A28C
		private bool can_player_bribe_to_prison_guard_clickable(out TextObject explanation)
		{
			int bribeToEnterDungeon = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
			if (Hero.MainHero.Gold < bribeToEnterDungeon)
			{
				explanation = new TextObject("{=TP7rZTKs}You don't have {DENAR_AMOUNT}{GOLD_ICON} denars.", null);
				explanation.SetTextVariable("DENAR_AMOUNT", bribeToEnterDungeon);
				explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				return false;
			}
			explanation = new TextObject("{=hCavIm4G}You will pay {AMOUNT}{GOLD_ICON} denars.", null);
			explanation.SetTextVariable("AMOUNT", bribeToEnterDungeon);
			explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			return true;
		}

		// Token: 0x060009F5 RID: 2549 RVA: 0x0004C120 File Offset: 0x0004A320
		private bool conversation_prison_guard_reject_visit_prison_on_condition()
		{
			bool flag = Settlement.CurrentSettlement.BribePaid >= Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
			StringHelpers.SetCharacterProperties("SETTLEMENT_OWNER", Settlement.CurrentSettlement.OwnerClan.Leader.CharacterObject, null, false);
			SettlementAccessModel.AccessDetails accessDetails;
			Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterDungeon(Settlement.CurrentSettlement, out accessDetails);
			return !flag && accessDetails.AccessLevel != SettlementAccessModel.AccessLevel.FullAccess;
		}

		// Token: 0x060009F6 RID: 2550 RVA: 0x0004C1A4 File Offset: 0x0004A3A4
		private void conversation_prison_guard_visit_prison_on_consequence()
		{
			if (Settlement.CurrentSettlement.IsFortification)
			{
				Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("prison");
				Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
			}
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
			{
				Mission.Current.EndMission();
			};
		}

		// Token: 0x060009F7 RID: 2551 RVA: 0x0004C228 File Offset: 0x0004A428
		private bool conversation_guard_start_on_condition()
		{
			if (Campaign.Current.ConversationManager.OneToOneConversationAgent == null || this.CheckIfConversationAgentIsEscortingTheMainAgent())
			{
				return false;
			}
			if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Soldier && PlayerEncounter.Current != null && PlayerEncounter.InsideSettlement)
			{
				TextObject textObject = new TextObject("{=6JL4GyKC}Can't talk right now. Got to keep my eye on things around here.", null);
				if (Settlement.CurrentSettlement.OwnerClan == Hero.MainHero.Clan || Settlement.CurrentSettlement.MapFaction.Leader == Hero.MainHero)
				{
					textObject = new TextObject("{=xizHRti3}Nothing to report, your lordship.", null);
					if (Hero.MainHero.IsFemale)
					{
						textObject = new TextObject("{=sIfL5Vnx}Nothing to report, your ladyship.", null);
					}
				}
				else if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security <= 20f)
				{
					textObject = new TextObject("{=3sfjBnaJ}It's quiet. Too quiet. Things never stay quiet around here for long.", null);
				}
				else if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security <= 40f)
				{
					textObject = new TextObject("{=jjkOBPkY}Can't let down your guard around here. Too many bastards up to no good.", null);
				}
				else if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security >= 70f)
				{
					textObject = new TextObject("{=AHg5k9q2}Welcome to {SETTLEMENT_NAME}. I think you'll find these are good, law-abiding folk, for the most part.", null);
					textObject.SetTextVariable("SETTLEMENT_NAME", Settlement.CurrentSettlement.Name);
				}
				MBTextManager.SetTextVariable("GUARD_COMMENT", textObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x060009F8 RID: 2552 RVA: 0x0004C38B File Offset: 0x0004A58B
		private bool CheckIfConversationAgentIsEscortingTheMainAgent()
		{
			return Agent.Main != null && Agent.Main.IsActive() && Settlement.CurrentSettlement != null && ConversationMission.OneToOneConversationAgent != null && EscortAgentBehavior.CheckIfAgentIsEscortedBy(ConversationMission.OneToOneConversationAgent, Agent.Main);
		}

		// Token: 0x060009F9 RID: 2553 RVA: 0x0004C3BF File Offset: 0x0004A5BF
		private bool conversation_prison_guard_visit_prison_nobody_inside_condition()
		{
			return Settlement.CurrentSettlement.SettlementComponent.GetPrisonerHeroes().Count == 0;
		}

		// Token: 0x060009FA RID: 2554 RVA: 0x0004C3D8 File Offset: 0x0004A5D8
		private bool prison_guard_visit_permission_try_bribe_on_condition()
		{
			int bribeToEnterDungeon = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
			MBTextManager.SetTextVariable("AMOUNT", bribeToEnterDungeon);
			MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);
			return Hero.MainHero.Gold >= bribeToEnterDungeon && !Campaign.Current.IsMainHeroDisguised;
		}

		// Token: 0x060009FB RID: 2555 RVA: 0x0004C438 File Offset: 0x0004A638
		private void conversation_prison_guard_visit_permission_bribe_on_consequence()
		{
			int bribeToEnterDungeon = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
			BribeGuardsAction.Apply(Settlement.CurrentSettlement, bribeToEnterDungeon);
			this.conversation_prison_guard_visit_prison_on_consequence();
		}

		// Token: 0x060009FC RID: 2556 RVA: 0x0004C470 File Offset: 0x0004A670
		private void conversation_prison_guard_visit_break_on_consequence()
		{
		}

		// Token: 0x060009FD RID: 2557 RVA: 0x0004C474 File Offset: 0x0004A674
		private bool IsCastleGuard()
		{
			Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
			AgentNavigator agentNavigator = ((oneToOneConversationAgent != null) ? oneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator : null);
			bool flag = false;
			if (agentNavigator != null)
			{
				flag = agentNavigator.TargetUsableMachine != null && oneToOneConversationAgent.IsUsingGameObject && agentNavigator.TargetUsableMachine.GameEntity.HasTag("sp_guard_castle");
				if (!flag && (agentNavigator.SpecialTargetTag == "sp_guard_castle" || agentNavigator.SpecialTargetTag == "sp_guard"))
				{
					Location lordsHallLocation = LocationComplex.Current.GetLocationWithId("lordshall");
					MissionAgentHandler missionBehavior = Mission.Current.GetMissionBehavior<MissionAgentHandler>();
					if (missionBehavior != null && missionBehavior.TownPassageProps != null)
					{
						UsableMachine usableMachine = missionBehavior.TownPassageProps.Find((UsableMachine x) => ((Passage)x).ToLocation == lordsHallLocation);
						if (usableMachine != null && usableMachine.GameEntity.GlobalPosition.DistanceSquared(oneToOneConversationAgent.Position) < 100f)
						{
							flag = true;
						}
					}
				}
			}
			return flag;
		}

		// Token: 0x060009FE RID: 2558 RVA: 0x0004C574 File Offset: 0x0004A774
		private bool conversation_castle_guard_start_on_condition()
		{
			return !Campaign.Current.IsMainHeroDisguised && this.IsCastleGuard() && (Settlement.CurrentSettlement.MapFaction == Hero.MainHero.MapFaction || (Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction)));
		}

		// Token: 0x060009FF RID: 2559 RVA: 0x0004C630 File Offset: 0x0004A830
		private bool conversation_castle_guard_criminal_start_on_condition()
		{
			return !Campaign.Current.IsMainHeroDisguised && this.IsCastleGuard() && Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction && (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction));
		}

		// Token: 0x06000A00 RID: 2560 RVA: 0x0004C6CC File Offset: 0x0004A8CC
		private bool conversation_castle_guard_nobody_inside_condition()
		{
			return !LocationComplex.Current.GetLocationWithId("lordshall").GetCharacterList().Any((LocationCharacter c) => c.Character.IsHero && c.Character.HeroObject.IsLord) && Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement) > 0 && Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan;
		}

		// Token: 0x06000A01 RID: 2561 RVA: 0x0004C748 File Offset: 0x0004A948
		private bool conversation_castle_guard_player_can_enter_lordshall_condition()
		{
			bool flag;
			TextObject textObject;
			return Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(Settlement.CurrentSettlement, "lordshall", out flag, out textObject);
		}

		// Token: 0x06000A02 RID: 2562 RVA: 0x0004C778 File Offset: 0x0004A978
		private bool conversation_player_bribe_to_enter_lords_hall_on_condition()
		{
			int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
			MBTextManager.SetTextVariable("AMOUNT", bribeToEnterLordsHall);
			MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);
			return bribeToEnterLordsHall > 0 && !Campaign.Current.IsMainHeroDisguised && !this.conversation_castle_guard_nobody_inside_condition();
		}

		// Token: 0x06000A03 RID: 2563 RVA: 0x0004C7D8 File Offset: 0x0004A9D8
		private void conversation_player_bribe_to_enter_lords_hall_on_consequence()
		{
			int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
			BribeGuardsAction.Apply(Settlement.CurrentSettlement, bribeToEnterLordsHall);
		}

		// Token: 0x06000A04 RID: 2564 RVA: 0x0004C80C File Offset: 0x0004AA0C
		private bool conversation_player_bribe_to_enter_lords_hall_on_clickable_condition(out TextObject explanation)
		{
			int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
			if (Hero.MainHero.Gold < bribeToEnterLordsHall)
			{
				explanation = new TextObject("{=TP7rZTKs}You don't have {DENAR_AMOUNT}{GOLD_ICON} denars.", null);
				explanation.SetTextVariable("DENAR_AMOUNT", bribeToEnterLordsHall);
				explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				return false;
			}
			explanation = new TextObject("{=hCavIm4G}You will pay {AMOUNT}{GOLD_ICON} denars.", null);
			explanation.SetTextVariable("AMOUNT", bribeToEnterLordsHall);
			explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			return true;
		}

		// Token: 0x06000A05 RID: 2565 RVA: 0x0004C8A4 File Offset: 0x0004AAA4
		private void OpenLordsHallMission()
		{
			Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("lordshall");
			Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
			Mission.Current.EndMission();
		}

		// Token: 0x06000A06 RID: 2566 RVA: 0x0004C8F8 File Offset: 0x0004AAF8
		private bool conversation_disguised_start_on_condition()
		{
			return Campaign.Current.IsMainHeroDisguised && (this.IsCastleGuard() || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.PrisonGuard || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Guard || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Soldier);
		}

		// Token: 0x06000A07 RID: 2567 RVA: 0x0004C954 File Offset: 0x0004AB54
		private bool conversation_disguised_start_on_condition_alt()
		{
			return Campaign.Current.IsMainHeroDisguised && MBRandom.RandomInt(2) == 0 && (this.IsCastleGuard() || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.PrisonGuard || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Guard || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Soldier);
		}

		// Token: 0x04000482 RID: 1154
		public const float UnarmedTownGuardSpawnRate = 0.4f;

		// Token: 0x04000483 RID: 1155
		private readonly List<ValueTuple<CharacterObject, int>> _garrisonTroops = new List<ValueTuple<CharacterObject, int>>();
	}
}
