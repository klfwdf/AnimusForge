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
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.CampaignBehaviors;

public class GuardsCampaignBehavior : CampaignBehaviorBase
{
	public const float UnarmedTownGuardSpawnRate = 0.4f;

	private readonly List<(CharacterObject, int)> _garrisonTroops = new List<(CharacterObject, int)>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	private float GetProsperityMultiplier(SettlementComponent settlement)
	{
		return ((float)settlement.GetProsperityLevel() + 1f) / 3f;
	}

	private void AddGarrisonAndPrisonCharacters(Settlement settlement)
	{
		InitializeGarrisonCharacters(settlement);
		settlement.LocationComplex.GetLocationWithId("center").AddLocationCharacters(culture: (Campaign.Current.GameMode == CampaignGameMode.Campaign) ? settlement.MapFaction.Culture : settlement.Culture, createDelegate: CreatePrisonGuard, relation: LocationCharacter.CharacterRelations.Neutral, count: 1);
	}

	private void InitializeGarrisonCharacters(Settlement settlement)
	{
		_garrisonTroops.Clear();
		if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
		{
			return;
		}
		MobileParty garrisonParty = settlement.Town.GarrisonParty;
		if (garrisonParty == null)
		{
			return;
		}
		foreach (TroopRosterElement item in garrisonParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character.Occupation == Occupation.Soldier)
			{
				_garrisonTroops.Add((item.Character, item.Number - item.WoundedNumber));
			}
		}
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (settlement.IsFortification)
		{
			AddGarrisonAndPrisonCharacters(settlement);
			if ((settlement.IsTown || settlement.IsCastle) && CampaignMission.Current != null)
			{
				Location location = CampaignMission.Current.Location;
				AddGuardsFromGarrison(settlement, unusedUsablePointCount, location);
			}
		}
	}

	private void AddGuardsFromGarrison(Settlement settlement, Dictionary<string, int> unusedUsablePointCount, Location location)
	{
		unusedUsablePointCount.TryGetValue("sp_guard", out var value);
		unusedUsablePointCount.TryGetValue("sp_guard_with_spear", out var value2);
		unusedUsablePointCount.TryGetValue("sp_guard_patrol", out var value3);
		unusedUsablePointCount.TryGetValue("sp_guard_unarmed", out var value4);
		unusedUsablePointCount.TryGetValue("sp_guard_castle", out var value5);
		float prosperityMultiplier = GetProsperityMultiplier(settlement.SettlementComponent);
		float num = (settlement.IsCastle ? 1.6f : 0.4f);
		value = (int)((float)value * prosperityMultiplier);
		value2 = (int)((float)value2 * prosperityMultiplier);
		value3 = (int)((float)value3 * prosperityMultiplier);
		value4 = (int)((float)value4 * prosperityMultiplier * num);
		if (value5 > 0)
		{
			location.AddLocationCharacters(CreateCastleGuard, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, value5);
		}
		if (value > 0)
		{
			location.AddLocationCharacters(CreateStandGuard, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
		}
		if (value2 > 0)
		{
			location.AddLocationCharacters(CreateStandGuardWithSpear, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, value2);
		}
		if (value3 > 0)
		{
			location.AddLocationCharacters(CreatePatrollingGuard, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, value3);
		}
		if (value4 > 0 && location != settlement.LocationComplex.GetLocationWithId("lordshall"))
		{
			location.AddLocationCharacters(CreateUnarmedGuard, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, value4);
		}
		if (location.StringId == "prison")
		{
			if (unusedUsablePointCount.TryGetValue("area_marker_1", out var value6) && value6 > 0)
			{
				location.AddLocationCharacters(CreateStandGuard, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
			if (unusedUsablePointCount.TryGetValue("area_marker_2", out value6) && value6 > 0)
			{
				location.AddLocationCharacters(CreateStandGuard, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
			if (unusedUsablePointCount.TryGetValue("area_marker_3", out value6) && value6 > 0)
			{
				location.AddLocationCharacters(CreateStandGuard, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
		}
	}

	private static ItemObject GetSuitableSpear(CultureObject culture)
	{
		string objectName = ((culture.StringId == "battania") ? "northern_spear_2_t3" : "western_spear_3_t3");
		return MBObjectManager.Instance.GetObject<ItemObject>(objectName);
	}

	private AgentData TakeGuardAgentDataFromGarrisonTroopList(CultureObject culture, bool overrideWeaponWithSpear = false, bool unarmed = false)
	{
		CharacterObject guardRosterElement;
		if (_garrisonTroops.Count > 0)
		{
			List<((CharacterObject, int), float)> list = new List<((CharacterObject, int), float)>();
			foreach (var garrisonTroop in _garrisonTroops)
			{
				list.Add(((garrisonTroop.Item1, garrisonTroop.Item2), garrisonTroop.Item1.Level));
			}
			int chosenIndex;
			(CharacterObject, int) tuple = MBRandom.ChooseWeighted(list, out chosenIndex);
			(guardRosterElement, _) = tuple;
			if (tuple.Item2 <= 1)
			{
				_garrisonTroops.RemoveAt(chosenIndex);
			}
			else
			{
				_garrisonTroops[chosenIndex] = (tuple.Item1, tuple.Item2 - 1);
			}
		}
		else
		{
			guardRosterElement = culture.Guard;
		}
		return PrepareGuardAgentDataFromGarrison(guardRosterElement, overrideWeaponWithSpear, unarmed);
	}

	private static AgentData PrepareGuardAgentDataFromGarrison(CharacterObject guardRosterElement, bool overrideWeaponWithSpear = false, bool unarmed = false)
	{
		Banner banner = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? PlayerEncounter.LocationEncounter.Settlement.OwnerClan.Banner : null);
		Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(guardRosterElement, randomEquipmentModifier: false, Equipment.EquipmentType.Battle);
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
					dictionary[randomEquipmentElements[i].Item.ItemType]++;
				}
				else
				{
					num++;
				}
			}
		}
		if (overrideWeaponWithSpear && dictionary[ItemObject.ItemTypeEnum.Polearm] > 0)
		{
			dictionary[ItemObject.ItemTypeEnum.Polearm]--;
		}
		if (num > 0)
		{
			num--;
		}
		else if (dictionary[ItemObject.ItemTypeEnum.Polearm] > 0)
		{
			dictionary[ItemObject.ItemTypeEnum.Polearm]--;
		}
		else if (dictionary[ItemObject.ItemTypeEnum.Bow] > 0)
		{
			dictionary[ItemObject.ItemTypeEnum.Arrows]--;
			dictionary[ItemObject.ItemTypeEnum.Bow]--;
		}
		else if (dictionary[ItemObject.ItemTypeEnum.Crossbow] > 0)
		{
			dictionary[ItemObject.ItemTypeEnum.Crossbow]--;
			dictionary[ItemObject.ItemTypeEnum.Bolts]--;
		}
		else if (dictionary[ItemObject.ItemTypeEnum.Sling] > 0)
		{
			dictionary[ItemObject.ItemTypeEnum.Sling]--;
			dictionary[ItemObject.ItemTypeEnum.SlingStones]--;
		}
		for (int num2 = 4; num2 >= 0; num2--)
		{
			if (randomEquipmentElements[num2].Item != null)
			{
				bool flag = false;
				if (dictionary.TryGetValue(randomEquipmentElements[num2].Item.ItemType, out var value))
				{
					if (value > 0)
					{
						flag = true;
						dictionary[randomEquipmentElements[num2].Item.ItemType]--;
					}
				}
				else if (num > 0)
				{
					flag = true;
					num--;
				}
				if (flag)
				{
					randomEquipmentElements.AddEquipmentToSlotWithoutAgent((EquipmentIndex)num2, default(EquipmentElement));
				}
			}
		}
		if (overrideWeaponWithSpear)
		{
			if (!IfEquipmentHasSpearSwapSlots(randomEquipmentElements))
			{
				ItemObject suitableSpear = GetSuitableSpear(guardRosterElement.Culture);
				randomEquipmentElements.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, new EquipmentElement(suitableSpear));
				IfEquipmentHasSpearSwapSlots(randomEquipmentElements);
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
		return new AgentData(new SimpleAgentOrigin(guardRosterElement, -1, banner)).Equipment(randomEquipmentElements).Monster(monsterWithSuffix).NoHorses(noHorses: true);
	}

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

	private LocationCharacter CreateCastleGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		AgentData agentData = TakeGuardAgentDataFromGarrisonTroopList(culture, overrideWeaponWithSpear: true);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors, "sp_guard_castle", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), useCivilianEquipment: false);
	}

	private LocationCharacter CreateStandGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		AgentData agentData = TakeGuardAgentDataFromGarrisonTroopList(culture);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors, "sp_guard", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), useCivilianEquipment: false);
	}

	private LocationCharacter CreateStandGuardWithSpear(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		AgentData agentData = TakeGuardAgentDataFromGarrisonTroopList(culture, overrideWeaponWithSpear: true);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors, "sp_guard_with_spear", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), useCivilianEquipment: false);
	}

	private LocationCharacter CreateUnarmedGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		AgentData agentData = TakeGuardAgentDataFromGarrisonTroopList(culture, overrideWeaponWithSpear: false, unarmed: true);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "sp_guard_unarmed", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_unarmed_guard"), useCivilianEquipment: false);
	}

	private LocationCharacter CreatePatrollingGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		AgentData agentData = TakeGuardAgentDataFromGarrisonTroopList(culture);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddPatrollingGuardBehaviors, "sp_guard_patrol", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), useCivilianEquipment: false);
	}

	private LocationCharacter CreatePrisonGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject prisonGuard = culture.PrisonGuard;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(prisonGuard.Race, "_settlement");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(prisonGuard)).Monster(monsterWithSuffix);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors, "sp_prison_guard", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), useCivilianEquipment: false, isFixedCharacter: true);
	}

	protected void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("disguise_start_conversation_alt", "start", "close_window", "{=uTycGRdI}You need to move along. I'm on duty right now and I can't spare any coin. May Heaven provide.", conversation_disguised_start_on_condition_alt, null);
		campaignGameStarter.AddDialogLine("disguise_start_conversation", "start", "close_window", "{=P98iCLjl}Get out of my face, you vile beggar.[if:convo_angry]", conversation_disguised_start_on_condition, null);
		campaignGameStarter.AddDialogLine("prison_guard_start_criminal", "start", "prison_guard_talk_criminal", "{=0UUCTaEj}We hear a lot of complaints about you lately. You better start behaving or you'll get yourself a good flogging.[if:convo_mocking_revenge]", conversation_prison_guard_criminal_start_on_condition, null);
		campaignGameStarter.AddDialogLine("prison_guard_ask_criminal", "prison_guard_talk_criminal", "prison_guard_talk", "{=XqTa0iQZ}What do you want, you degenerate?[if:convo_stern]", null, null);
		campaignGameStarter.AddDialogLine("prison_guard_start", "start", "prison_guard_talk", "{=6SppoTum}Yes? What do you want?", conversation_prison_guard_start_on_condition, null);
		campaignGameStarter.AddPlayerLine("prison_guard_ask_prisoners", "prison_guard_talk", "prison_guard_ask_prisoners", "{=av0bRae8}Who is imprisoned here?", null, null);
		campaignGameStarter.AddPlayerLine("prison_guard_ask_prisoner_talk", "prison_guard_talk", "close_window", "{=QxIXbHai}I want to speak with a prisoner (Cheat).", conversation_prison_guard_visit_prison_cheat_on_condition, conversation_prison_guard_visit_prison_on_consequence);
		campaignGameStarter.AddPlayerLine("prison_guard_ask_prisoner_talk_2", "prison_guard_talk", "prison_guard_visit_prison", "{=EGI6ztlH}I want to speak with a prisoner.", null, null);
		campaignGameStarter.AddPlayerLine("prison_guard_talk_end", "prison_guard_talk", "close_window", "{=D33fIGQe}Never mind.", null, null);
		campaignGameStarter.AddDialogLine("prison_guard_talk_about_prisoners", "prison_guard_ask_prisoners", "prison_guard_talk", "{=2eydhtcz}Currently, {PRISONER_NAMES} {?IS_PLURAL}are{?}is{\\?} imprisoned here.", conversation_prison_guard_talk_about_prisoners_on_condition, null);
		campaignGameStarter.AddDialogLine("prison_guard_visit_prison_ask_for_permission", "prison_guard_visit_prison", "prison_guard_visit_prison_ask_for_permission_answer", "{=XN0XZAkI}I can't let you in. My {?SETTLEMENT_OWNER.GENDER}Lady{?}Lord{\\?} {SETTLEMENT_OWNER.NAME} would be furious.", conversation_prison_guard_reject_visit_prison_on_condition, null);
		campaignGameStarter.AddDialogLine("prison_guard_visit_prison", "prison_guard_visit_prison", "close_window", "{=XWpEpaQ4}Of course, {?PLAYER.GENDER}madam{?}sir{\\?}. Go in.", null, conversation_prison_guard_visit_prison_on_consequence);
		campaignGameStarter.AddPlayerLine("prison_guard_visit_prison_ask_answer", "prison_guard_visit_prison_ask_for_permission_answer", "prison_guard_visit_prison_ask_for_permission_guard_answer", "{=k3b5KqSc}Come on now. I thought you were the boss here.", null, null);
		campaignGameStarter.AddDialogLine("prison_guard_visit_prison_ask_answer_3", "prison_guard_visit_prison_ask_for_permission_guard_answer", "prison_guard_visit_prison_ask_for_permission_answer_options", "{=JaAltoKP}Um... What are you saying?", null, null);
		campaignGameStarter.AddPlayerLine("prison_guard_visit_permission_try_bribe", "prison_guard_visit_prison_ask_for_permission_answer_options", "prison_guard_bribe_answer_satisfied", "{=dY3Vazug}I found a purse with {AMOUNT}{GOLD_ICON} a few paces away. I reckon it belongs to you.", prison_guard_visit_permission_try_bribe_on_condition, null, 100, can_player_bribe_to_prison_guard_clickable);
		campaignGameStarter.AddPlayerLine("prison_guard_visit_prison_ask_answer_3_2", "prison_guard_visit_prison_ask_for_permission_answer_options", "close_window", "{=D33fIGQe}Never mind.", null, null);
		campaignGameStarter.AddDialogLine("prison_guard_visit_prison_nobody_inside", "prison_guard_visit_prison", "prison_guard_talk", "{=rVHbbrCQ}We're not holding anyone in here right now. There's no reason for you to go in.[ib:closed]", conversation_prison_guard_visit_prison_nobody_inside_condition, null);
		campaignGameStarter.AddPlayerLine("prison_guard_visit_empty_prison", "prison_guard_visit_prison_nobody_1", "close_window", "{=b3KFoJJ8}All right then. I'll have a look at the prison.", null, conversation_prison_guard_visit_prison_on_consequence);
		campaignGameStarter.AddPlayerLine("prison_guard_visit_empty_prison_2", "prison_guard_visit_prison_nobody_2", "close_window", "{=b3KFoJJ8}All right then. I'll have a look at the prison.", null, null);
		campaignGameStarter.AddPlayerLine("prison_guard_not_visit_empty_prison", "prison_guard_visit_prison_nobody_1", "close_window", "{=L5vAhxhO}I have more important business to do.", null, null);
		campaignGameStarter.AddPlayerLine("prison_guard_not_visit_empty_prison_2", "prison_guard_visit_prison_nobody_2", "close_window", "{=L5vAhxhO}I have more important business to do.", null, null);
		campaignGameStarter.AddPlayerLine("prison_guard_visit_permission_leave", "prison_guard_visit_prison_2", "close_window", "{=qPRl07mD}All right then. I'll try that.", null, null);
		campaignGameStarter.AddDialogLine("prison_guard_visit_permission_bribe", "prison_guard_bribe_answer_satisfied", "close_window", "{=fCrVeHP3}Ah! I was looking for this all day. How good of you to bring it back {?PLAYER.GENDER}madam{?}sir{\\?}. Well, now that I know what an honest {?PLAYER.GENDER}lady{?}man{\\?} you are, there can be no harm in letting you inside for a look. Go in.... Just so you know, though -- I'll be hanging onto the keys, in case you were thinking about undoing anyone's chains.", null, conversation_prison_guard_visit_permission_bribe_on_consequence);
		campaignGameStarter.AddPlayerLine("prison_guard_visit_permission_try_break", "prison_guard_visit_prison_4", "prison_guard_visit_break", "{=htfLEQlf}Give me the keys to the cells -- now!", null, null);
		campaignGameStarter.AddDialogLine("prison_guard_visit_break", "prison_guard_visit_break", "close_window", "{=Kto7RWKE}Help! Help! Prison break!", null, conversation_prison_guard_visit_break_on_consequence);
		campaignGameStarter.AddDialogLine("castle_guard_start_criminal", "start", "castle_guard_talk_criminal", "{=0UUCTaEj}We hear a lot of complaints about you lately. You better start behaving or you'll get yourself a good flogging.[if:convo_mocking_revenge]", conversation_castle_guard_criminal_start_on_condition, null);
		campaignGameStarter.AddDialogLine("castle_guard_ask_criminal", "castle_guard_talk_criminal", "castle_guard_talk", "{=XqTa0iQZ}What do you want, you degenerate?[if:convo_stern]", null, null);
		campaignGameStarter.AddDialogLine("castle_guard_start", "start", "castle_guard_talk", "{=6SppoTum}Yes? What do you want?", conversation_castle_guard_start_on_condition, null);
		campaignGameStarter.AddDialogLine("guard_start", "start", "close_window", "{=!}{GUARD_COMMENT}", conversation_guard_start_on_condition, null);
		campaignGameStarter.AddPlayerLine("player_ask_for_permission_to_enter_lords_hall", "castle_guard_talk", "player_ask_permission_to_lords_hall", "{=b2h3r1kL}I want to visit the lord's hall.", null, null);
		campaignGameStarter.AddPlayerLine("player_ask_for_permission_to_enter_lords_hall_2", "castle_guard_talk", "close_window", "{=never_mind}Never mind.", null, null);
		campaignGameStarter.AddDialogLine("castle_guard_no_permission_nobody_inside", "player_ask_permission_to_lords_hall", "permisson_for_lords_hall", "{=RJtCakaG}There is nobody inside to receive you right now.", conversation_castle_guard_nobody_inside_condition, null);
		campaignGameStarter.AddDialogLine("castle_guard_player_can_enter", "player_ask_permission_to_lords_hall", "close_window", "{=bbroVUrD}Of course, my {?PLAYER.GENDER}lady{?}lord{\\?}.", conversation_castle_guard_player_can_enter_lordshall_condition, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += OpenLordsHallMission;
		});
		campaignGameStarter.AddDialogLine("castle_guard_no_permission", "player_ask_permission_to_lords_hall", "permisson_for_lords_hall", "{=rcoESVVz}Sorry, but we don't know you. We can't just let anyone in. (Not enough renown)", null, null);
		campaignGameStarter.AddPlayerLine("player_bribe_to_enter_lords_hall", "permisson_for_lords_hall", "player_bribe_to_castle_guard", "{=7wkHMnNM}Maybe {AMOUNT}{GOLD_ICON} will help you to remember me.", conversation_player_bribe_to_enter_lords_hall_on_condition, conversation_player_bribe_to_enter_lords_hall_on_consequence, 100, conversation_player_bribe_to_enter_lords_hall_on_clickable_condition);
		campaignGameStarter.AddPlayerLine("player_not_bribe_to_enter_lords_hall", "permisson_for_lords_hall", "close_window", "{=xatWDriV}Never mind then.", null, null);
		campaignGameStarter.AddDialogLine("castle_guard_let_player_in", "player_bribe_to_castle_guard", "close_window", "{=g5ofoKa8}Yeah... Now I remember you.", null, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += OpenLordsHallMission;
		});
	}

	private bool conversation_prison_guard_criminal_start_on_condition()
	{
		if (!Campaign.Current.IsMainHeroDisguised && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.PrisonGuard && Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction)
		{
			if (!Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction))
			{
				return Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction);
			}
			return true;
		}
		return false;
	}

	private bool conversation_prison_guard_start_on_condition()
	{
		if (!Campaign.Current.IsMainHeroDisguised && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.PrisonGuard)
		{
			if (Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction)
			{
				if (Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction))
				{
					return !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool conversation_prison_guard_talk_about_prisoners_on_condition()
	{
		List<CharacterObject> prisonerHeroes = Settlement.CurrentSettlement.SettlementComponent.GetPrisonerHeroes();
		if (prisonerHeroes.Count == 0)
		{
			MBTextManager.SetTextVariable("PRISONER_NAMES", GameTexts.FindText("str_nobody"));
			MBTextManager.SetTextVariable("IS_PLURAL", "0");
		}
		else
		{
			for (int i = 0; i < prisonerHeroes.Count; i++)
			{
				if (i == 0)
				{
					MBTextManager.SetTextVariable("LEFT", prisonerHeroes[i].Name);
					continue;
				}
				MBTextManager.SetTextVariable("RIGHT", prisonerHeroes[i].Name);
				MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_LEFT_comma_RIGHT").ToString());
			}
			MBTextManager.SetTextVariable("IS_PLURAL", (prisonerHeroes.Count > 1) ? 1 : 0);
			MBTextManager.SetTextVariable("PRISONER_NAMES", GameTexts.FindText("str_LEFT_ONLY").ToString());
		}
		return true;
	}

	private bool conversation_prison_guard_visit_prison_cheat_on_condition()
	{
		return Game.Current.IsDevelopmentMode;
	}

	private bool can_player_bribe_to_prison_guard_clickable(out TextObject explanation)
	{
		int bribeToEnterDungeon = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
		if (Hero.MainHero.Gold < bribeToEnterDungeon)
		{
			explanation = new TextObject("{=TP7rZTKs}You don't have {DENAR_AMOUNT}{GOLD_ICON} denars.");
			explanation.SetTextVariable("DENAR_AMOUNT", bribeToEnterDungeon);
			explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			return false;
		}
		explanation = new TextObject("{=hCavIm4G}You will pay {AMOUNT}{GOLD_ICON} denars.");
		explanation.SetTextVariable("AMOUNT", bribeToEnterDungeon);
		explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		return true;
	}

	private bool conversation_prison_guard_reject_visit_prison_on_condition()
	{
		bool num = Settlement.CurrentSettlement.BribePaid >= Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
		StringHelpers.SetCharacterProperties("SETTLEMENT_OWNER", Settlement.CurrentSettlement.OwnerClan.Leader.CharacterObject);
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterDungeon(Settlement.CurrentSettlement, out var accessDetails);
		if (!num)
		{
			return accessDetails.AccessLevel != SettlementAccessModel.AccessLevel.FullAccess;
		}
		return false;
	}

	private void conversation_prison_guard_visit_prison_on_consequence()
	{
		if (Settlement.CurrentSettlement.IsFortification)
		{
			Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("prison");
			Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
		}
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			Mission.Current.EndMission();
		};
	}

	private bool conversation_guard_start_on_condition()
	{
		if (Campaign.Current.ConversationManager.OneToOneConversationAgent == null || CheckIfConversationAgentIsEscortingTheMainAgent())
		{
			return false;
		}
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Soldier && PlayerEncounter.Current != null && PlayerEncounter.InsideSettlement)
		{
			TextObject textObject = new TextObject("{=6JL4GyKC}Can't talk right now. Got to keep my eye on things around here.");
			if (Settlement.CurrentSettlement.OwnerClan == Hero.MainHero.Clan || Settlement.CurrentSettlement.MapFaction.Leader == Hero.MainHero)
			{
				textObject = new TextObject("{=xizHRti3}Nothing to report, your lordship.");
				if (Hero.MainHero.IsFemale)
				{
					textObject = new TextObject("{=sIfL5Vnx}Nothing to report, your ladyship.");
				}
			}
			else if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security <= 20f)
			{
				textObject = new TextObject("{=3sfjBnaJ}It's quiet. Too quiet. Things never stay quiet around here for long.");
			}
			else if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security <= 40f)
			{
				textObject = new TextObject("{=jjkOBPkY}Can't let down your guard around here. Too many bastards up to no good.");
			}
			else if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security >= 70f)
			{
				textObject = new TextObject("{=AHg5k9q2}Welcome to {SETTLEMENT_NAME}. I think you'll find these are good, law-abiding folk, for the most part.");
				textObject.SetTextVariable("SETTLEMENT_NAME", Settlement.CurrentSettlement.Name);
			}
			MBTextManager.SetTextVariable("GUARD_COMMENT", textObject);
			return true;
		}
		return false;
	}

	private bool CheckIfConversationAgentIsEscortingTheMainAgent()
	{
		if (Agent.Main != null && Agent.Main.IsActive() && Settlement.CurrentSettlement != null && ConversationMission.OneToOneConversationAgent != null)
		{
			return EscortAgentBehavior.CheckIfAgentIsEscortedBy(ConversationMission.OneToOneConversationAgent, Agent.Main);
		}
		return false;
	}

	private bool conversation_prison_guard_visit_prison_nobody_inside_condition()
	{
		return Settlement.CurrentSettlement.SettlementComponent.GetPrisonerHeroes().Count == 0;
	}

	private bool prison_guard_visit_permission_try_bribe_on_condition()
	{
		int bribeToEnterDungeon = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
		MBTextManager.SetTextVariable("AMOUNT", bribeToEnterDungeon);
		MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		return !Campaign.Current.IsMainHeroDisguised;
	}

	private void conversation_prison_guard_visit_permission_bribe_on_consequence()
	{
		int bribeToEnterDungeon = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement);
		BribeGuardsAction.Apply(Settlement.CurrentSettlement, bribeToEnterDungeon);
		conversation_prison_guard_visit_prison_on_consequence();
	}

	private void conversation_prison_guard_visit_break_on_consequence()
	{
	}

	private bool IsCastleGuard()
	{
		Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
		AgentNavigator agentNavigator = oneToOneConversationAgent?.GetComponent<CampaignAgentComponent>().AgentNavigator;
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

	private bool conversation_castle_guard_start_on_condition()
	{
		if (!Campaign.Current.IsMainHeroDisguised && IsCastleGuard())
		{
			if (Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction)
			{
				if (Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction))
				{
					return !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool conversation_castle_guard_criminal_start_on_condition()
	{
		if (!Campaign.Current.IsMainHeroDisguised && IsCastleGuard() && Settlement.CurrentSettlement.MapFaction != Hero.MainHero.MapFaction)
		{
			if (!Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction))
			{
				return Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(Settlement.CurrentSettlement.MapFaction);
			}
			return true;
		}
		return false;
	}

	private bool conversation_castle_guard_nobody_inside_condition()
	{
		if (!LocationComplex.Current.GetLocationWithId("lordshall").GetCharacterList().Any((LocationCharacter c) => c.Character.IsHero && c.Character.HeroObject.IsLord) && Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement) > 0)
		{
			return Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan;
		}
		return false;
	}

	private bool conversation_castle_guard_player_can_enter_lordshall_condition()
	{
		bool disableOption;
		TextObject disabledText;
		return Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(Settlement.CurrentSettlement, "lordshall", out disableOption, out disabledText);
	}

	private bool conversation_player_bribe_to_enter_lords_hall_on_condition()
	{
		int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
		MBTextManager.SetTextVariable("AMOUNT", bribeToEnterLordsHall);
		MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		if (bribeToEnterLordsHall > 0 && !Campaign.Current.IsMainHeroDisguised)
		{
			return !conversation_castle_guard_nobody_inside_condition();
		}
		return false;
	}

	private void conversation_player_bribe_to_enter_lords_hall_on_consequence()
	{
		int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
		BribeGuardsAction.Apply(Settlement.CurrentSettlement, bribeToEnterLordsHall);
	}

	private bool conversation_player_bribe_to_enter_lords_hall_on_clickable_condition(out TextObject explanation)
	{
		int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
		if (Hero.MainHero.Gold < bribeToEnterLordsHall)
		{
			explanation = new TextObject("{=TP7rZTKs}You don't have {DENAR_AMOUNT}{GOLD_ICON} denars.");
			explanation.SetTextVariable("DENAR_AMOUNT", bribeToEnterLordsHall);
			explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			return false;
		}
		explanation = new TextObject("{=hCavIm4G}You will pay {AMOUNT}{GOLD_ICON} denars.");
		explanation.SetTextVariable("AMOUNT", bribeToEnterLordsHall);
		explanation.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		return true;
	}

	private void OpenLordsHallMission()
	{
		Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("lordshall");
		Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
		Mission.Current.EndMission();
	}

	private bool conversation_disguised_start_on_condition()
	{
		if (Campaign.Current.IsMainHeroDisguised)
		{
			if (!IsCastleGuard() && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.PrisonGuard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.Guard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.ArenaMaster)
			{
				return CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Soldier;
			}
			return true;
		}
		return false;
	}

	private bool conversation_disguised_start_on_condition_alt()
	{
		if (Campaign.Current.IsMainHeroDisguised && MBRandom.RandomInt(2) == 0)
		{
			if (!IsCastleGuard() && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.PrisonGuard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.Guard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.ArenaMaster)
			{
				return CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Soldier;
			}
			return true;
		}
		return false;
	}
}
