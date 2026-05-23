using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class CommonVillagersCampaignBehavior : CampaignBehaviorBase
{
	public const float VillagerSpawnPercentageMale = 0.25f;

	public const float VillagerSpawnPercentageFemale = 0.2f;

	public const float VillagerSpawnPercentageLimited = 0.2f;

	public const float VillageOtherPeopleSpawnPercentage = 0.05f;

	private readonly Dictionary<int, string> _rumorsGiven = new Dictionary<int, string>();

	private CampaignTime _lastEnteredTime;

	private float TradeRumorDistance => Campaign.Current.EstimatedAverageCaravanPartySpeed * (float)CampaignTime.HoursInDay;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
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

	private float GetSpawnRate(Settlement settlement)
	{
		return TimeOfDayPercentage() * GetProsperityMultiplier(settlement.SettlementComponent);
	}

	private float GetConfigValue()
	{
		return BannerlordConfig.CivilianAgentCount;
	}

	private float GetProsperityMultiplier(SettlementComponent settlement)
	{
		return ((float)settlement.GetProsperityLevel() + 1f) / 3f;
	}

	private float TimeOfDayPercentage()
	{
		return 1f - TaleWorlds.Library.MathF.Abs(CampaignTime.Now.CurrentHourInDay - (float)TaleWorlds.Library.MathF.Ceiling((float)CampaignTime.HoursInDay * 0.625f)) / (float)TaleWorlds.Library.MathF.Ceiling((float)CampaignTime.HoursInDay * 0.625f);
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		Location locationWithId = settlement.LocationComplex.GetLocationWithId("village_center");
		if (CampaignMission.Current.Location == locationWithId)
		{
			AddVillageCenterCharacters(settlement, unusedUsablePointCount, !CampaignTime.Now.IsDayTime);
		}
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

	private void AddVillageCenterCharacters(Settlement settlement, Dictionary<string, int> unusedUsablePointCount, bool isNight)
	{
		Location locationWithId = settlement.LocationComplex.GetLocationWithId("village_center");
		CultureObject culture = settlement.Culture;
		unusedUsablePointCount.TryGetValue("npc_common", out var value);
		unusedUsablePointCount.TryGetValue("npc_common_limited", out var value2);
		float num = (float)(value + value2) * 0.65f;
		float num2 = MBMath.ClampFloat(GetConfigValue() / num, 0f, 1f);
		float num3 = GetSpawnRate(settlement) * num2 * GetWeatherEffectMultiplier(settlement);
		if (locationWithId == null || CampaignMission.Current.Location != locationWithId)
		{
			return;
		}
		if (value > 0)
		{
			int num4 = (int)((float)value * 0.25f * num3);
			if (num4 > 0)
			{
				locationWithId.AddLocationCharacters(CreateVillageMan, culture, LocationCharacter.CharacterRelations.Neutral, num4);
			}
			int num5 = (int)((float)value * 0.2f * num3);
			if (num5 > 0)
			{
				locationWithId.AddLocationCharacters(CreateVillageWoman, culture, LocationCharacter.CharacterRelations.Neutral, num5);
			}
			if (!isNight)
			{
				int num6 = (int)((float)value * 0.05f * num3);
				if (num6 > 0)
				{
					locationWithId.AddLocationCharacters(CreateMaleChild, culture, LocationCharacter.CharacterRelations.Neutral, num6);
					locationWithId.AddLocationCharacters(CreateFemaleChild, culture, LocationCharacter.CharacterRelations.Neutral, num6);
					locationWithId.AddLocationCharacters(CreateMaleTeenager, culture, LocationCharacter.CharacterRelations.Neutral, num6);
					locationWithId.AddLocationCharacters(CreateFemaleTeenager, culture, LocationCharacter.CharacterRelations.Neutral, num6);
				}
			}
		}
		if (value2 > 0)
		{
			int num7 = (int)((float)value2 * 0.2f * num3);
			if (num7 > 0)
			{
				locationWithId.AddLocationCharacters(CreateVillageManCarryingStuff, culture, LocationCharacter.CharacterRelations.Neutral, num7 / 2);
				locationWithId.AddLocationCharacters(CreateVillageWomanCarryingStuff, culture, LocationCharacter.CharacterRelations.Neutral, num7 / 2);
			}
		}
		int value3 = 0;
		if (unusedUsablePointCount.TryGetValue("spawnpoint_cleaner", out value3))
		{
			locationWithId.AddLocationCharacters(CommonTownsfolkCampaignBehavior.CreateBroomsWoman, culture, LocationCharacter.CharacterRelations.Neutral, value3);
		}
		if (unusedUsablePointCount.TryGetValue("npc_beggar", out value3))
		{
			locationWithId.AddLocationCharacters(CommonTownsfolkCampaignBehavior.CreateFemaleBeggar, culture, LocationCharacter.CharacterRelations.Neutral, value3 / 2);
			locationWithId.AddLocationCharacters(CommonTownsfolkCampaignBehavior.CreateMaleBeggar, culture, LocationCharacter.CharacterRelations.Neutral, value3 / 2);
		}
	}

	public void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement.IsVillage || settlement.IsFortification)
		{
			SettlementHelper.TakeEnemyVillagersOutsideSettlements(settlement);
		}
	}

	private LocationCharacter CreateVillageMan(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villager = culture.Villager;
		Tuple<string, Monster> randomTownsManActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsManActionSetAndMonster(villager.Race);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villager, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(villager)).Monster(randomTownsManActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common", fixedLocation: false, relation, randomTownsManActionSetAndMonster.Item1, useCivilianEquipment: true);
	}

	private LocationCharacter CreateMaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villagerMaleChild = culture.VillagerMaleChild;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(villagerMaleChild.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villagerMaleChild, out var minimumAge, out var maximumAge, "Child");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(villagerMaleChild)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, villagerMaleChild.IsFemale, "_child"), useCivilianEquipment: true);
	}

	private LocationCharacter CreateFemaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villagerFemaleChild = culture.VillagerFemaleChild;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(villagerFemaleChild.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villagerFemaleChild, out var minimumAge, out var maximumAge, "Child");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(villagerFemaleChild)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, villagerFemaleChild.IsFemale, "_child"), useCivilianEquipment: true);
	}

	private LocationCharacter CreateMaleTeenager(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villagerMaleTeenager = culture.VillagerMaleTeenager;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(villagerMaleTeenager.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villagerMaleTeenager, out var minimumAge, out var maximumAge, "Teenager");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(villagerMaleTeenager)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, villagerMaleTeenager.IsFemale, "_villager"), useCivilianEquipment: true);
	}

	private LocationCharacter CreateFemaleTeenager(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villagerFemaleTeenager = culture.VillagerFemaleTeenager;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(villagerFemaleTeenager.Race, "_child");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villagerFemaleTeenager, out var minimumAge, out var maximumAge, "Teenager");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(villagerFemaleTeenager)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, villagerFemaleTeenager.IsFemale, "_villager"), useCivilianEquipment: true);
	}

	private LocationCharacter CreateVillageManCarryingStuff(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villager = culture.Villager;
		string randomStuff = SettlementHelper.GetRandomStuff(isFemale: false);
		Monster monster;
		string actionSetSuffixAndMonsterForItem = CommonTownsfolkCampaignBehavior.GetActionSetSuffixAndMonsterForItem(randomStuff, villager.Race, isFemale: false, out monster);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villager, out var minimumAge, out var maximumAge, "TownsfolkCarryingStuff");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(villager)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(randomStuff);
		LocationCharacter locationCharacter = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, villager.IsFemale, actionSetSuffixAndMonsterForItem), useCivilianEquipment: true, isFixedCharacter: false, itemObject);
		if (itemObject == null)
		{
			locationCharacter.PrefabNamesForBones.Add(agentData.AgentMonster.MainHandItemBoneIndex, randomStuff);
		}
		return locationCharacter;
	}

	private LocationCharacter CreateVillageWoman(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villageWoman = culture.VillageWoman;
		Tuple<string, Monster> randomTownsWomanActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsWomanActionSetAndMonster(villageWoman.Race);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villageWoman, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(villageWoman)).Monster(randomTownsWomanActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common", fixedLocation: false, relation, randomTownsWomanActionSetAndMonster.Item1, useCivilianEquipment: true);
	}

	private LocationCharacter CreateVillageWomanCarryingStuff(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject villageWoman = culture.VillageWoman;
		string randomStuff = SettlementHelper.GetRandomStuff(isFemale: true);
		Monster monster;
		string actionSetSuffixAndMonsterForItem = CommonTownsfolkCampaignBehavior.GetActionSetSuffixAndMonsterForItem(randomStuff, villageWoman.Race, isFemale: false, out monster);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(villageWoman, out var minimumAge, out var maximumAge, "TownsfolkCarryingStuff");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(villageWoman)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(randomStuff);
		LocationCharacter locationCharacter = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, villageWoman.IsFemale, actionSetSuffixAndMonsterForItem), useCivilianEquipment: true, isFixedCharacter: false, itemObject);
		if (itemObject == null)
		{
			locationCharacter.PrefabNamesForBones.Add(agentData.AgentMonster.MainHandItemBoneIndex, randomStuff);
		}
		return locationCharacter;
	}

	protected void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		AddTownspersonAndVillagerDialogs(campaignGameStarter);
	}

	private void AddTownspersonAndVillagerDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("town_beggar_to_disguised_character", "start", "close_window", "{=iVJlUlOg}Look, friend, we'll both eat better tonight if you move to a different spot. Too many of us, and the masters and ladies give a wide berth.", conversation_beggar_to_disguise_start_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_talk_to_disguised_character", "start", "close_window", "{=qGIFpahv}Can't spare any coins for you. Sorry. May Heaven provide.", conversation_townsperson_to_disguise_start_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_start", "start", "town_or_village_talk", "{=!}{CONVERSATION_SCRAP}", conversation_town_or_village_start_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_talk_beggar", "town_or_village_talk", "close_window", "{=kFWmcRpV}The Heavens repay kindness with kindness, my {?PLAYER.GENDER}lady{?}lord{\\?}.", conversation_beggar_start_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_talk", "town_or_village_talk", "town_or_village_player_children_post_rhyme", "{=jZF4l0jY}Oh, sorry, {?PLAYER.GENDER}madam{?}sir{\\?}. May I be of service?", conversation_children_rhymes_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_talk_children", "town_or_village_talk", "town_or_village_player", "{=KPfs7L7B}Ah, my apologies, {?PLAYER.GENDER}madam{?}sir{\\?}. May I help you with something?", null, null);
		campaignGameStarter.AddDialogLine("town_or_village_start_2", "start", "close_window", "{=IrsaIJ4u}Ay, these are hard days indeed...", conversation_beggar_delivered_line_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_start_postrumor_liege", "start", "town_or_village_player", "{=eKLH9fOb}May I be of service, {?PLAYER.GENDER}your ladyship{?}your lordship{\\?}?", conversation_liege_delivered_line_on_street_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_start_postrumor_children", "start", "town_or_village_children_player_no_rhyme", "{=Osaupw6M}Beg pardon, {?PLAYER.GENDER}madam{?}sir{\\?}, I need to get back to my parents. Is there anything you need?", conversation_children_already_delivered_line_on_street_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_start_postrumor_tavern", "start", "town_or_village_player", "{=X1A4r7wY}Your good health, {?PLAYER.GENDER}madam{?}sir{\\?}. May I help you?", conversation_already_delivered_line_in_tavern_on_condition, null);
		campaignGameStarter.AddDialogLine("town_or_village_start_postrumor", "start", "town_or_village_player", "{=P99OLPWU}Excuse me, {?PLAYER.GENDER}madam{?}sir{\\?}, but I must shortly go about my business. Is there anything you need?", conversation_already_delivered_line_on_street_on_condition, null);
		campaignGameStarter.AddPlayerLine("player_run_along_children_1", "town_or_village_player_children_post_rhyme", "close_window", "{=7kn4Jmdl}Ah, yes! We had such rhymes when I was young.", null, null);
		campaignGameStarter.AddPlayerLine("player_run_along_children_2", "town_or_village_player_children_post_rhyme", "close_window", "{=jGxotDqF}Best not sing that to every stranger you meet, child.", null, null);
		campaignGameStarter.AddPlayerLine("player_run_along_children_3", "town_or_village_player_children_post_rhyme", "close_window", "{=bhkC3kcQ}Speak respectfully about your elders, you filthy ragamuffin!", null, null);
		campaignGameStarter.AddPlayerLine("player_run_along_children", "town_or_village_children_player_no_rhyme", "close_window", "{=PV56VAFg}Run along now, child.", null, null);
		campaignGameStarter.AddPlayerLine("player_ask_hero_location", "town_or_village_player", "player_ask_hero_location", "{=urieibC4}I'm looking for someone.", conversation_town_or_village_player_ask_location_of_hero_on_condition, conversation_town_or_village_player_ask_location_of_hero_on_consequence);
		campaignGameStarter.AddDialogLine("player_ask_hero_location_di1", "player_ask_hero_location", "player_ask_hero_location_2", "{=cqlV1YLO}Whom are you looking for?", null, null);
		campaignGameStarter.AddRepeatablePlayerLine("player_ask_hero_location_2", "player_ask_hero_location_2", "player_ask_hero_location_3", "{=obY78MnQ}{HERO.LINK}", "{=4hDu8rDF}I am thinking of a different person.", "player_ask_hero_location", conversation_town_or_village_player_ask_location_of_hero_2_on_condition, conversation_town_or_village_player_ask_location_of_hero_2_on_consequence);
		campaignGameStarter.AddPlayerLine("player_ask_hero_location_2_2", "player_ask_hero_location_2", "town_or_village_pretalk", "{=D33fIGQe}Never mind.", null, null);
		campaignGameStarter.AddDialogLine("town_or_village_pretalk", "town_or_village_pretalk", "town_or_village_player", "{=ds294zxi}Anything else?", null, null);
		campaignGameStarter.AddDialogLine("player_ask_hero_location_3", "player_ask_hero_location_3", "player_ask_hero_location_4", "{=qN2LYVIO}Yes, I know {HERO.LINK}.", conversation_town_or_village_player_ask_location_of_hero_3_on_condition, null);
		campaignGameStarter.AddDialogLine("player_ask_hero_location_4_di", "player_ask_hero_location_3", "town_or_village_pretalk", "{=woMdU4Xl}I don't know where {?HERO.GENDER}she{?}he{\\?} is.", null, null);
		campaignGameStarter.AddPlayerLine("player_ask_hero_location_4", "player_ask_hero_location_4", "player_ask_hero_location_5", "{=a1FeLSbH}Can you take me to {?HERO.GENDER}her{?}him{\\?}?", null, null);
		campaignGameStarter.AddPlayerLine("player_ask_hero_location_4_2", "player_ask_hero_location_4", "town_or_village_pretalk", "{=D33fIGQe}Never mind.", null, null);
		campaignGameStarter.AddDialogLine("player_ask_hero_location_5", "player_ask_hero_location_5", "close_window", "{=mhgUwwZb}Sure. Follow me.", null, conversation_town_or_village_player_ask_location_of_hero_5_on_consequence);
		campaignGameStarter.AddDialogLine("town_or_village_escort_complete", "start", "town_or_village_pretalk", "{=9PBA2OJz}Here {?HERO.GENDER}she{?}he{\\?} is.", conversation_town_or_village_escort_complete_on_condition, conversation_town_or_village_escort_complete_on_consequence);
		campaignGameStarter.AddDialogLine("town_or_village_start_3", "start", "town_or_village_escorting", "{=ym6bSrNo}{STILL_ESCORTING_ANSWER}", conversation_town_or_village_talk_escorting_commoner_on_condition, null);
		campaignGameStarter.AddPlayerLine("town_or_village_escorting_changed_my_mind", "town_or_village_escorting", "town_or_village_pretalk", "{=fkkYatnM}Actually, I've changed my mind. I'll talk to {?HERO.GENDER}her{?}him{\\?} later...", null, conversation_town_or_village_talk_stop_escorting_on_consequence);
		campaignGameStarter.AddPlayerLine("town_or_village_escorting_keep_going", "town_or_village_escorting", "close_window", "{=QTZjjOXb}Let us keep going.", null, null);
		campaignGameStarter.AddPlayerLine("town_or_village_player", "town_or_village_player", "close_window", "{=OlOhuO7X}No thank you. Good day to you.", null, null);
	}

	private bool CheckIfConversationAgentIsEscortingTheMainAgent()
	{
		if (Agent.Main != null && Agent.Main.IsActive() && Settlement.CurrentSettlement != null && ConversationMission.OneToOneConversationAgent != null)
		{
			return EscortAgentBehavior.CheckIfAgentIsEscortedBy(ConversationMission.OneToOneConversationAgent, Agent.Main);
		}
		return false;
	}

	private bool CheckIfTheMainAgentIsBeingEscorted()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (EscortAgentBehavior.CheckIfAgentIsEscortedBy(agent, Agent.Main))
			{
				return true;
			}
		}
		return false;
	}

	private bool conversation_town_or_village_player_ask_location_of_hero_on_condition()
	{
		if (!CheckIfTheMainAgentIsBeingEscorted())
		{
			return heroes_to_look_for().Count != 0;
		}
		return false;
	}

	private void conversation_town_or_village_player_ask_location_of_hero_on_consequence()
	{
		ConversationSentence.SetObjectsToRepeatOver(heroes_to_look_for());
	}

	private List<Hero> heroes_to_look_for()
	{
		List<Hero> list = new List<Hero>();
		Vec3 position = ConversationMission.OneToOneConversationAgent.Position;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent.IsHuman && agent.IsHero && agent.State == AgentState.Active)
			{
				Hero heroObject = ((CharacterObject)agent.Character).HeroObject;
				if (!heroObject.IsLord && position.Distance(agent.Position) > 6f)
				{
					list.Add(heroObject);
				}
			}
		}
		return list;
	}

	private bool conversation_town_or_village_player_ask_location_of_hero_2_on_condition()
	{
		if (!CheckIfTheMainAgentIsBeingEscorted() && ConversationSentence.CurrentProcessedRepeatObject is Hero hero)
		{
			StringHelpers.SetRepeatableCharacterProperties("HERO", hero.CharacterObject);
			return true;
		}
		return false;
	}

	private void conversation_town_or_village_player_ask_location_of_hero_2_on_consequence()
	{
		ConversationHelper.AskedLord = ((Hero)ConversationSentence.SelectedRepeatObject).CharacterObject;
	}

	private bool conversation_town_or_village_player_ask_location_of_hero_3_on_condition()
	{
		Hero heroObject = ConversationHelper.AskedLord.HeroObject;
		Location locationOfCharacter = LocationComplex.Current.GetLocationOfCharacter(heroObject);
		StringHelpers.SetCharacterProperties("HERO", heroObject.CharacterObject);
		return locationOfCharacter == CampaignMission.Current.Location;
	}

	private void conversation_town_or_village_player_ask_location_of_hero_5_on_consequence()
	{
		Hero heroObject = ConversationHelper.AskedLord.HeroObject;
		Agent conversationAgent = ConversationMission.OneToOneConversationAgent;
		Agent targetAgent = null;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent.Character == heroObject.CharacterObject)
			{
				targetAgent = agent;
				break;
			}
		}
		EscortAgentBehavior.AddEscortAgentBehavior(conversationAgent, targetAgent, delegate
		{
			if (!Campaign.Current.ConversationManager.IsConversationFlowActive)
			{
				MissionConversationLogic.Current.StartConversation(conversationAgent, setActionsInstantly: false);
			}
			return false;
		});
	}

	public bool conversation_town_or_village_escort_complete_on_condition()
	{
		if (CheckIfConversationAgentIsEscortingTheMainAgent())
		{
			EscortAgentBehavior behavior = ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>().GetBehavior<EscortAgentBehavior>();
			if (behavior.IsEscortFinished())
			{
				MBTextManager.SetTextVariable("HERO_GENDER", behavior.TargetAgent.Character.IsFemale ? 1 : 0);
				return true;
			}
		}
		return false;
	}

	public void conversation_town_or_village_escort_complete_on_consequence()
	{
		Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
		if (oneToOneConversationAgent?.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
		{
			EscortAgentBehavior.RemoveEscortBehaviorOfAgent(oneToOneConversationAgent);
		}
	}

	private bool conversation_town_or_village_talk_escorting_commoner_on_condition()
	{
		if (CheckIfConversationAgentIsEscortingTheMainAgent())
		{
			EscortAgentBehavior behavior = ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>().GetBehavior<EscortAgentBehavior>();
			float randomFloat = MBRandom.RandomFloat;
			TextObject textObject = ((randomFloat < 0.33f) ? new TextObject("{=Eb7KG1bi}{HERO.LINK} should be just around the corner...[ib:normal]") : ((!(randomFloat < 0.66f)) ? new TextObject("{=fasAZDvM}We're on our way to {HERO.LINK}...[ib:demure]") : new TextObject("{=uhwoWUyR}Still haven't taken you to {HERO.LINK}...[ib:demure]")));
			textObject.SetCharacterProperties("HERO", (CharacterObject)behavior.TargetAgent.Character);
			MBTextManager.SetTextVariable("STILL_ESCORTING_ANSWER", textObject);
			return true;
		}
		return false;
	}

	private void conversation_town_or_village_talk_stop_escorting_on_consequence()
	{
		if (CheckIfConversationAgentIsEscortingTheMainAgent())
		{
			EscortAgentBehavior.RemoveEscortBehaviorOfAgent(ConversationMission.OneToOneConversationAgent);
		}
	}

	private bool conversation_liege_delivered_line_on_street_on_condition()
	{
		if (conversation_already_delivered_line_on_street_on_condition())
		{
			if (Settlement.CurrentSettlement.MapFaction.Leader != Hero.MainHero)
			{
				return Settlement.CurrentSettlement.OwnerClan == Hero.MainHero.Clan;
			}
			return true;
		}
		return false;
	}

	private bool conversation_children_already_delivered_line_on_street_on_condition()
	{
		_ = Campaign.Current.ConversationManager.OneToOneConversationAgent;
		if (conversation_already_delivered_line_on_street_on_condition() && conversation_children_rhymes_on_condition())
		{
			return true;
		}
		return false;
	}

	private bool conversation_already_delivered_line_on_street_on_condition()
	{
		if (!CheckIfConversationAgentIsEscortingTheMainAgent())
		{
			if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Villager || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Townsfolk)
			{
				return PlayerEncounter.InsideSettlement;
			}
			return false;
		}
		return false;
	}

	private bool conversation_already_delivered_line_in_tavern_on_condition()
	{
		if ((CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Villager || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Townsfolk) && PlayerEncounter.InsideSettlement && CampaignMission.Current != null && CampaignMission.Current.Location.StringId == "tavern" && !CheckIfConversationAgentIsEscortingTheMainAgent())
		{
			return true;
		}
		return false;
	}

	private bool conversation_children_rhymes_on_condition()
	{
		return Campaign.Current.ConversationManager.OneToOneConversationAgent.Age < (float)Campaign.Current.Models.AgeModel.BecomeTeenagerAge;
	}

	private bool conversation_townsperson_to_disguise_start_on_condition()
	{
		if (CheckIfConversationAgentIsEscortingTheMainAgent() || Campaign.Current.ConversationManager.OneToOneConversationAgent == null)
		{
			return false;
		}
		if ((CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Villager || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Townsfolk) && PlayerEncounter.Current != null && PlayerEncounter.InsideSettlement)
		{
			return Campaign.Current.IsMainHeroDisguised;
		}
		return false;
	}

	private bool conversation_beggar_to_disguise_start_on_condition()
	{
		if (conversation_beggar_start_on_condition())
		{
			return Campaign.Current.IsMainHeroDisguised;
		}
		return false;
	}

	private bool conversation_beggar_start_on_condition()
	{
		if (CheckIfConversationAgentIsEscortingTheMainAgent() || Campaign.Current.ConversationManager.OneToOneConversationAgent == null)
		{
			return false;
		}
		if (Settlement.CurrentSettlement == null)
		{
			return false;
		}
		if (Campaign.Current.ConversationManager.OneToOneConversationCharacter == Settlement.CurrentSettlement.Culture.Beggar || Campaign.Current.ConversationManager.OneToOneConversationCharacter == Settlement.CurrentSettlement.Culture.FemaleBeggar)
		{
			return true;
		}
		return false;
	}

	private bool conversation_beggar_delivered_line_on_condition()
	{
		if (CheckIfConversationAgentIsEscortingTheMainAgent() || Campaign.Current.ConversationManager.OneToOneConversationAgent == null)
		{
			return false;
		}
		if (Settlement.CurrentSettlement == null)
		{
			return false;
		}
		if (Campaign.Current.ConversationManager.OneToOneConversationCharacter == Settlement.CurrentSettlement.Culture.Beggar || Campaign.Current.ConversationManager.OneToOneConversationCharacter == Settlement.CurrentSettlement.Culture.FemaleBeggar)
		{
			return conversation_already_delivered_line_on_street_on_condition();
		}
		return false;
	}

	private bool conversation_beggar_info_on_condition()
	{
		MBTextManager.SetTextVariable("BEGGAR_INFO", "{=zJReEB8Z}Sitting in the marketplace all day one learns many things if one keeps one's ears open... But alas, I have heard nothing recently that might interest your worshipful self.");
		return true;
	}

	private bool conversation_town_or_village_start_on_condition()
	{
		if (CheckIfConversationAgentIsEscortingTheMainAgent() || Campaign.Current.ConversationManager.OneToOneConversationAgent == null)
		{
			return false;
		}
		if ((CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Villager || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Townsfolk) && PlayerEncounter.Current != null && PlayerEncounter.InsideSettlement)
		{
			if (_lastEnteredTime != CampaignTime.Now)
			{
				_lastEnteredTime = CampaignTime.Now;
				_rumorsGiven.Clear();
			}
			int key = TaleWorlds.Library.MathF.Abs(Campaign.Current.ConversationManager.OneToOneConversationAgent.GetHashCode());
			if (_rumorsGiven.ContainsKey(key))
			{
				return false;
			}
			List<TextObject> list = new List<TextObject>();
			if (conversation_children_rhymes_on_condition())
			{
				list.Add(new TextObject("{=aLAyJPrI}Garios Garios brave and strong... Always right and never wrong...{newline}The men all trust him with their lives.. But not their daughters or their wives."));
				list.Add(new TextObject("{=5ZbsASDx}Emp'ror Arenicos feeling fine. Went to bed with a cup of wine.{newline}In the morning he was dead. Now his wife rules in his stead!"));
				list.Add(new TextObject("{=FxKgQECI}Lucon Lucon I've been told, sir. Was born old and just got older.{newline}Lives in a palace made of gold. Where it's always dark and it's always cold."));
				list.Add(new TextObject("{=YDMlCaaz}Great Khan Monchug lifts his hand. We all hark to his command!{newline}Great Khan Monchug rides away. We go back to nap and play."));
				list.Add(new TextObject("{=wrEKw6ip}Cuckoo bird, cuckoo bird, tell me no lies. 'I snuck into another bird's tree'.{newline}Cuckoo bird, cuckoo bird, say something wise. 'There's a king in Battania who's just like me.'"));
				list.Add(new TextObject("{=2WI066bI}Nimr Nimr he was killed. Died in the cage of the Banu Qild.{newline}All the ladies cry their grief. All the husbands sigh in relief."));
				list.Add(new TextObject("{=Jc8OiSZg}Olek Olek fought a bear. Took it down, fair and square.{newline}Not with his sword and not with his spear. But with his breath of garlic and beer."));
				list.Add(new TextObject("{=TCIaDC9X}The foes of Vlandia came a-plundering. King Derthert summoned his men to the keep!{newline}They rode to his muster with their hooves a-thundering. But Derthert had already gone to sleep."));
			}
			else if (conversation_beggar_start_on_condition())
			{
				list = GetBeggarStories();
			}
			else if (GetPossibleIssueRumors().Count > 0)
			{
				list = GetPossibleIssueRumors();
			}
			else
			{
				GetPossibleRumors(list);
			}
			for (int i = 0; i < list.Count; i++)
			{
				TextObject textObject = list[MBRandom.RandomInt(list.Count)];
				string value = RumorIdentifier(textObject);
				if (!_rumorsGiven.ContainsValue(value))
				{
					MBTextManager.SetTextVariable("CONVERSATION_SCRAP", textObject);
					_rumorsGiven.Add(key, value);
					return true;
				}
			}
		}
		return false;
	}

	private string RumorIdentifier(TextObject conversationScrap)
	{
		string text = conversationScrap.CopyTextObject().ToString();
		return text.Substring(0, (text.Length < 12) ? text.Length : 12);
	}

	private List<TextObject> GetPossibleIssueRumors()
	{
		List<TextObject> list = new List<TextObject>();
		foreach (Hero notable in Settlement.CurrentSettlement.Notables)
		{
			IssueBase issue = notable.Issue;
			if (issue != null)
			{
				TextObject issueAsRumorInSettlement = issue.IssueAsRumorInSettlement;
				if (!issueAsRumorInSettlement.IsEmpty() && !_rumorsGiven.ContainsValue(RumorIdentifier(issueAsRumorInSettlement)))
				{
					list.Add(issueAsRumorInSettlement);
				}
			}
		}
		return list;
	}

	private List<TextObject> GetBeggarStories()
	{
		List<TextObject> list = new List<TextObject>();
		list.Add(new TextObject("{=EiiHoraV}Hard times... Hard times indeed."));
		CharacterObject characterObject = null;
		CharacterObject characterObject2 = null;
		foreach (Village boundVillage in Settlement.CurrentSettlement.BoundVillages)
		{
			foreach (Hero notable in boundVillage.Settlement.Notables)
			{
				if (notable.Occupation == Occupation.RuralNotable && notable.GetTraitLevel(DefaultTraits.Mercy) < 0 && notable.GetTraitLevel(DefaultTraits.Generosity) <= 0)
				{
					characterObject = notable.CharacterObject;
				}
			}
		}
		foreach (Hero notable2 in Settlement.CurrentSettlement.Notables)
		{
			if (notable2.Occupation == Occupation.Merchant && notable2.GetTraitLevel(DefaultTraits.Mercy) < 0 && notable2.GetTraitLevel(DefaultTraits.Generosity) <= 0)
			{
				characterObject2 = notable2.CharacterObject;
			}
		}
		if (CharacterObject.OneToOneConversationCharacter.IsFemale)
		{
			list.Add(new TextObject("{=ZVbQemrz}My husband was wounded in the wars, and now can't pull a plow. So he lost his tenancy, and now I must beg in the marketplace."));
			list.Add(new TextObject("{=olKsdJmv}I married bad. That was my misfortune. Drinks his wages and beats me when I say a word, so now I must beg for bread for our children. "));
			list.Add(new TextObject("{=3kHmtbVZ}What my man did was wrong, there's no denying. But they hanged him, and now what am I to do? Do his children bear his guilt?"));
			list.Add(new TextObject("{=agL4RTbB}My man and I, we never wed proper. So he made a bit of money in the wars and wanted to marry a rich man's daughter. 'Cast her out,' she told him, and that's what he did."));
			list.Add(new TextObject("{=k18dhQA1}The plague took my parents and my uncle took their land. But I'd rather beg than be a servant in a rich man's home!"));
		}
		else
		{
			list.Add(new TextObject("{=oaFFW2bo}The demons come at night and taunt me until dawn. What am I to do?"));
			list.Add(new TextObject("{=b3MBZKuQ}We came here looking for work, as there was none in the village. But the masters want skilled hands only. With a few coins, I could go home."));
			list.Add(new TextObject("{=mrtdsccq}My own people chased me from the village. It weren't true, what they said about me. Coveted my land, I'll warrant."));
			list.Add(new TextObject("{=GKl8U04i}Lamed by an arrow in the leg, and now I can't work a field and I'm no use to anyone, they say."));
			if (characterObject2 != null)
			{
				TextObject textObject = new TextObject("{=yj0VZ8IZ}I lost a mule loaded with wares when it slid into the river, and {HARSH_MERCHANT.NAME} said he'd take it from my wages for the next year, didn't care if I starved. If I didn't like it then I could go beg on the street, he said to me. So look at me. I stood up to him and look at me now.");
				textObject.SetCharacterProperties("HARSH_MERCHANT", characterObject2);
				list.Add(textObject);
			}
			if (characterObject != null)
			{
				TextObject textObject2 = new TextObject("{=SGoz7xvk}I tilled the land of {CRUEL_LANDOWNER.NAME}, and paid my rent, and earned him a pretty penny. But the times changed, the prices changed, he took my tenancy from me and said he'd raise sheep there instead. Where was I to go? What I am to do?");
				textObject2.SetCharacterProperties("CRUEL_LANDOWNER", characterObject);
				list.Add(textObject2);
			}
		}
		return list;
	}

	private void GetPossibleRumors(List<TextObject> conversationScraps)
	{
		List<string> list = new List<string>();
		list.Add("{=8XM8CHIm}brother-in-law");
		list.Add("{=VDfyzs5v}nephew");
		list.Add("{=NPuHiggC}cousin");
		list.Add("{=Mf3vLIQp}uncle");
		int num = CampaignTime.Now.GetDayOfYear / CampaignTime.DaysInSeason;
		float num2 = Settlement.CurrentSettlement.Town?.Loyalty ?? 50f;
		bool flag = false;
		Location location = CampaignMission.Current.Location;
		Location locationWithId = LocationComplex.Current.GetLocationWithId("tavern");
		if (Settlement.CurrentSettlement != null && locationWithId == location)
		{
			flag = true;
		}
		if (flag)
		{
			if (num == 0)
			{
				conversationScraps.Add(new TextObject("{=sm6ckPnp}It's springtime, my friend. The season of the winds. The season of madness. Now then, I believe I'll have another drink."));
			}
			if (num == 1 && !CharacterObject.OneToOneConversationCharacter.IsFemale)
			{
				conversationScraps.Add(new TextObject("{=se2EXQu8}When the heat's this bad, a fellow can build up quite a thirst."));
			}
			if (num == 3)
			{
				conversationScraps.Add(new TextObject("{=kDItfaaN}I'll just have one more to keep me warm on the way home."));
			}
			if (Settlement.CurrentSettlement.Culture.StringId == "empire")
			{
				conversationScraps.Add(new TextObject("{=5EXL1MiE}Sometimes I feel like running off to join those hermits over on Mount Erithrys and putting my worries behind me. Even better, maybe some of these great lords would go there. They can retire and live out their days in the sun, not worrying about being beheaded or betrayed, and they wouldn't need to tax us any. Be better for everyone."));
			}
			conversationScraps.Add(new TextObject("{=bFjtk0Op}Well.. Sometimes when you learn a skill, you pick up a certain way of doing it that can help you in some circumstances and hurt you in others. If you wanted to retrain yourself, your companions, or members of your clan to do things a different way, you might try the tournament master at the arena. They'll help you - for a price, of course."));
		}
		conversationScraps.Add(new TextObject("{=U79CXBbj}Heaven watch over us. Heaven give us strength."));
		SettlementComponent.ProsperityLevel prosperityLevel = Settlement.CurrentSettlement.SettlementComponent.GetProsperityLevel();
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Villager)
		{
			if (prosperityLevel > SettlementComponent.ProsperityLevel.Mid)
			{
				conversationScraps.Add(new TextObject("{=KeIGtpxb}Just glad to have full bellies this year in my household. Can't say that for every year."));
			}
			if (num == 0 && prosperityLevel < SettlementComponent.ProsperityLevel.Mid)
			{
				conversationScraps.Add(new TextObject("{=dlOCK6Ro}After that winter, the cow's too thin to pull the plough. Don't know where I'll get the money to rent one.[rf:convo_grave]"));
			}
			if (num == 1 && prosperityLevel < SettlementComponent.ProsperityLevel.Mid)
			{
				conversationScraps.Add(new TextObject("{=usMQ64pf}Hope there's enough hands for the harvest."));
			}
			if (num == 2 && prosperityLevel < SettlementComponent.ProsperityLevel.Mid)
			{
				conversationScraps.Add(new TextObject("{=46Q5MQHK}It was a thin harvest. It will be a lean winter and a cruel spring. You mark my words.[rf:convo_grave]"));
			}
			if (prosperityLevel < SettlementComponent.ProsperityLevel.Mid)
			{
				conversationScraps.Add(new TextObject("{=MuzS4Et1}Heaven help us. If I can't pay the rent, I'll end up on the side of the road with the landless, hoping for a day's work.[rf:confused_annoyed]"));
			}
			if (num == 0)
			{
				conversationScraps.Add(new TextObject("{=8bnlS0IV}Ploughing, sowing... Got many weeks of that ahead of us still."));
			}
			if (num == 1)
			{
				conversationScraps.Add(new TextObject("{=CSqHz4EG}It's almost harvesting season."));
			}
			if (num == 2)
			{
				conversationScraps.Add(new TextObject("{=FNLtDFbn}Time to butcher and salt our meat for the winter."));
			}
			if (num == 3)
			{
				conversationScraps.Add(new TextObject("{=TDgoyFBi}Not much work to be done... Sewing and mending and what."));
			}
		}
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Townsfolk)
		{
			if (prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
			{
				conversationScraps.Add(new TextObject("{=Emw9ut4A}Hard times... Can't find good help these days. Half of 'em are too hungry to work. The other half are thieves. Smart ones all left weeks ago.[rf:confused_annoyed]"));
			}
			if (prosperityLevel > SettlementComponent.ProsperityLevel.Mid && num2 < 50f)
			{
				conversationScraps.Add(new TextObject("{=mimt3Qph}Every day... More people in the markets... More people at the well... Where do they all come from?"));
			}
			if (prosperityLevel > SettlementComponent.ProsperityLevel.Mid && num2 < 60f)
			{
				conversationScraps.Add(new TextObject("{=RUZKiW0u}Business is good. Can't deny that. But prices are high and getting higher."));
			}
			if (prosperityLevel < SettlementComponent.ProsperityLevel.Low)
			{
				conversationScraps.Add(new TextObject("{=LKGivHYp}The pittance they call a wage these days... I don't want to leave my kin, but I'm thinking I might have to try my luck in some other town."));
			}
			if (Settlement.CurrentSettlement.Culture.StringId == "aserai")
			{
				if (num == 1 && prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
				{
					conversationScraps.Add(new TextObject("{=3zZ8ZIca}Damn the summer. Can't sleep at night for the heat. And now they say the well's running low..."));
				}
			}
			else
			{
				if (num == 3 && prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
				{
					conversationScraps.Add(new TextObject("{=Tol5zRgG}This winter... Kids are all coughing and wheezing. My old ma's been bed-bound for a month. Can't afford coals for the stove at night. Spring thaw can't come fast enough for me."));
				}
				if (num == 2 && prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
				{
					conversationScraps.Add(new TextObject("{=ZVItXrnG}It's a lean autumn, this one. Heaven help us lay in enough wood before the winter."));
				}
			}
		}
		Settlement settlement = ((!Settlement.CurrentSettlement.IsTown && !Settlement.CurrentSettlement.IsCastle) ? Settlement.CurrentSettlement.Village.Bound : Settlement.CurrentSettlement);
		Town town = settlement.Town;
		if ((town != null && town.Loyalty < 20f) || settlement.Town.InRebelliousState)
		{
			conversationScraps.Add(new TextObject("{=ftyp2ul1}Those troublemakers - they're not from around here. I can't believe it's anyone from around here. You hear people talking foreign more and more these days - I'm sure it's them."));
			conversationScraps.Add(new TextObject("{=J1TykdEn}I hear there have been secret meetings in the woods. Solemn oaths, signed in blood. A great wind is going to blow soon - you mark my words."));
		}
		Town town2 = Settlement.CurrentSettlement.Town;
		if (town2 == null && Settlement.CurrentSettlement.IsVillage)
		{
			town2 = ((Settlement.CurrentSettlement.Village.TradeBound != null) ? Settlement.CurrentSettlement.Village.TradeBound.Town : Settlement.CurrentSettlement.Village.Bound.Town);
		}
		IOrderedEnumerable<ItemRosterElement> orderedEnumerable = Settlement.CurrentSettlement.ItemRoster.OrderBy((ItemRosterElement x) => town2.GetItemCategoryPriceIndex(x.EquipmentElement.Item.ItemCategory));
		List<Town> list2 = (from settlement2 in Campaign.Current.Settlements
			where settlement2.IsTown && settlement2 != Settlement.CurrentSettlement && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement2, Settlement.CurrentSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All) < TradeRumorDistance
			select settlement2.Town).ToList();
		int num3 = 0;
		foreach (ItemRosterElement item2 in orderedEnumerable)
		{
			ItemObject targetItem = item2.EquipmentElement.Item;
			if (!targetItem.IsTradeGood)
			{
				continue;
			}
			int price = town2.MarketData.GetPrice(targetItem);
			if (list2.Count <= 0)
			{
				continue;
			}
			Town town3 = TaleWorlds.Core.Extensions.MaxBy(list2, (Town t) => t.GetItemPrice(targetItem));
			int num4 = town3.GetItemPrice(new EquipmentElement(targetItem), null, isSelling: true) - price;
			if (num4 > 1)
			{
				num3++;
				if (num3 > 4)
				{
					break;
				}
				TextObject textObject = num3 switch
				{
					1 => new TextObject("{=8NDuezga}Now, I was talking to my {RANDOM_RELATIVE}, and he tells me he bought some {._}{PLURAL(ITEM_NAME)} around here for cheap and took it to {TOWN_NAME}. Said he made {PRICE_DIFF}{GOLD_ICON} of profit on each one."), 
					2 => new TextObject("{=ubnVca4L}So, yeah... My {RANDOM_RELATIVE} says he took some {._}{PLURAL(ITEM_NAME)} to {TOWN_NAME} and made {PRICE_DIFF}{GOLD_ICON} in profit. Course he might be talking out of his arse."), 
					3 => new TextObject("{=4Slck7UB}So, I heard from my {RANDOM_RELATIVE} that he took some {._}{PLURAL(ITEM_NAME)} over to {TOWN_NAME}, and made a profit of {PRICE_DIFF}{GOLD_ICON}."), 
					4 => new TextObject("{=rTrGXB1o}Yeah.. Word is that {._}{ITEM_NAME} sells well in {TOWN_NAME}. A profit of {PRICE_DIFF}{GOLD_ICON} a load, they say."), 
					_ => null, 
				};
				textObject.SetTextVariable("RANDOM_RELATIVE", new TextObject(list[MBRandom.RandomInt(0, list.Count)]));
				textObject.SetTextVariable("ITEM_NAME", targetItem.Name);
				textObject.SetTextVariable("TOWN_NAME", town3.Name);
				textObject.SetTextVariable("PRICE_DIFF", num4);
				conversationScraps.Add(textObject);
			}
		}
		Hero leader = Settlement.CurrentSettlement.OwnerClan.Leader;
		Hero leader2 = Settlement.CurrentSettlement.MapFaction.Leader;
		if (leader.GetTraitLevel(DefaultTraits.Honor) > 0 && prosperityLevel >= SettlementComponent.ProsperityLevel.Low)
		{
			TextObject textObject2 = new TextObject("{=ztiax0Sn}Say what you will about {?OWNER.GENDER}lady{?}lord{\\?} {OWNER.LINK}... {?OWNER.GENDER}She{?}He{\\?}'ll give the lowest wretch in the realm a fair hearing in {?OWNER.GENDER}her{?}his{\\?} court. Can't deny that.");
			leader.SetPropertiesToTextObject(textObject2, "OWNER");
			conversationScraps.Add(textObject2);
		}
		if (leader.GetTraitLevel(DefaultTraits.Mercy) < 0 && leader.GetTraitLevel(DefaultTraits.Generosity) < 0 && prosperityLevel <= SettlementComponent.ProsperityLevel.Mid)
		{
			TextObject textObject3 = new TextObject("{=mfVJBFUD}The tax on this, the tax on that... Now just what do we think our beloved {?OWNER.GENDER}lady{?}lord{\\?} {OWNER.LINK} does with it all?");
			leader.SetPropertiesToTextObject(textObject3, "OWNER");
			conversationScraps.Add(textObject3);
		}
		if (leader.GetTraitLevel(DefaultTraits.Mercy) > 0 && Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security < 40f)
		{
			TextObject textObject4 = new TextObject("{=ZO5RaXMW}Spare us from {?OWNER.GENDER}ladies{?}lords{\\?} like {OWNER.LINK}, whose hearts are too tender to punish the thieves and rogues who terrorize honest folk.");
			leader.SetPropertiesToTextObject(textObject4, "OWNER");
			conversationScraps.Add(textObject4);
		}
		if (leader.GetTraitLevel(DefaultTraits.Honor) < 0 && Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security < 40f)
		{
			TextObject textObject5 = new TextObject("{=bUweVtkk}Why doesn't {OWNER.LINK} do anything about the thieves plying their trade out on the street? Could it be that {?OWNER.GENDER}she{?}he{\\?} is getting a cut of their take?");
			leader.SetPropertiesToTextObject(textObject5, "OWNER");
			conversationScraps.Add(textObject5);
		}
		if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security < 40f && leader.GetTraitLevel(DefaultTraits.Honor) > 0)
		{
			TextObject textObject6 = new TextObject("{=OwnoJUkn}They say that {OWNER.LINK} is an honest {?OWNER.GENDER}woman{?}man{\\?}. But even {?OWNER.GENDER}she{?}he{\\?} can't stop all the thieving and corruption around here.");
			leader.SetPropertiesToTextObject(textObject6, "OWNER");
			conversationScraps.Add(textObject6);
		}
		if (Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security < 40f && prosperityLevel >= SettlementComponent.ProsperityLevel.High)
		{
			TextObject item = new TextObject("{=wqzyHirl}Look all around you. People making money hand over fist, draped in silks, smelling of wine and perfume. But is it the honest folk? I think not.");
			conversationScraps.Add(item);
		}
		if (leader.GetTraitLevel(DefaultTraits.Mercy) + leader.GetTraitLevel(DefaultTraits.Generosity) < 0 && prosperityLevel >= SettlementComponent.ProsperityLevel.Mid)
		{
			TextObject textObject7 = new TextObject("{=7uFFaSHv}Now some people say our {?OWNER.GENDER}lady{?}lord{\\?} {OWNER.LINK} is as mean as a scalded cat. But I say most people are villains, and it's good they feel a little fear. Anyway, there's meat on the table these days, and who can argue with that?");
			leader.SetPropertiesToTextObject(textObject7, "OWNER");
			conversationScraps.Add(textObject7);
		}
		if (leader2.GetTraitLevel(DefaultTraits.Honor) < 0 && leader2.GetTraitLevel(DefaultTraits.Calculating) < 0 && prosperityLevel <= SettlementComponent.ProsperityLevel.Mid)
		{
			TextObject textObject8 = new TextObject("{=nMFY2Pb0}We are doomed I say, doomed. A ruler who cannot control {?RULER.GENDER}her{?}his{\\?} passions brings ruin on the realm.");
			leader2.SetPropertiesToTextObject(textObject8, "RULER");
			conversationScraps.Add(textObject8);
		}
		if (num2 < 40f)
		{
			conversationScraps.Add(new TextObject("{=RyAH9Zvk}Now the man in the market said, 'All this trouble and woe means the Heavens aren't pleased with the one on the throne.' 'That's treason talk!' I told him. Of course I did."));
		}
		foreach (LogEntry gameActionLog in Campaign.Current.LogEntryHistory.GameActionLogs)
		{
			if (!(gameActionLog.GameTime.ElapsedDaysUntilNow < 60f))
			{
				continue;
			}
			if (gameActionLog is VillageStateChangedLogEntry)
			{
				VillageStateChangedLogEntry villageStateChangedLogEntry = (VillageStateChangedLogEntry)gameActionLog;
				Village village = villageStateChangedLogEntry.Village;
				if (villageStateChangedLogEntry.NewState == Village.VillageStates.BeingRaided && village.Settlement.MapFaction == Settlement.CurrentSettlement.MapFaction && village.Settlement != Settlement.CurrentSettlement)
				{
					TextObject textObject9 = new TextObject("{=JQuCuaVv}I've heard that {ENEMY_NAME} have been raiding across the border... People around here better wake up.");
					textObject9.SetTextVariable("ENEMY_NAME", FactionHelper.GetTermUsedByOtherFaction(villageStateChangedLogEntry.RaiderPartyMapFaction, Settlement.CurrentSettlement.MapFaction, pejorative: false));
					conversationScraps.Add(textObject9);
				}
			}
			if (gameActionLog.GetAsRumor(Settlement.CurrentSettlement, out var comment) > 0)
			{
				conversationScraps.Add(comment);
			}
		}
		int num5 = 0;
		foreach (MobileParty allBanditParty in MobileParty.AllBanditParties)
		{
			if (DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(navCapabilities: (!allBanditParty.IsCurrentlyAtSea) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval, fromMobileParty: allBanditParty, toSettlement: Settlement.CurrentSettlement) < Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 5f)
			{
				num5++;
			}
		}
		if (num5 > 1 && Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town.Security < 50f)
		{
			TextObject textObject10 = new TextObject("{=NPhnQgQS}There's bandits lurking just beyond the walls these days. What about the taxes we pay, I ask you? Why aren't {?OWNER.GENDER}Lady{?}Lord{\\?} {OWNER.LINK}'s men doing their jobs?");
			leader.SetPropertiesToTextObject(textObject10, "OWNER");
			conversationScraps.Add(textObject10);
		}
		if (num5 > 1 && Settlement.CurrentSettlement.IsVillage && Settlement.CurrentSettlement.Village.TradeBound != null && Settlement.CurrentSettlement.Village.TradeBound.Town.Security < 50f)
		{
			TextObject textObject11 = new TextObject("{=64p9ULVu}There's bandits lurking just beyond the outermost fields, I hear. What about the taxes we pay, I ask you? Why aren't {?OWNER.GENDER}Lady{?}Lord{\\?} {OWNER.LINK}'s men doing their jobs?");
			leader.SetPropertiesToTextObject(textObject11, "OWNER");
			conversationScraps.Add(textObject11);
		}
		List<Hero> list3 = Settlement.CurrentSettlement.HeroesWithoutParty.ToList();
		list3.Add(Settlement.CurrentSettlement.OwnerClan.Leader);
		foreach (Hero character in list3)
		{
			int traitLevel = character.GetTraitLevel(DefaultTraits.Mercy);
			int traitLevel2 = character.GetTraitLevel(DefaultTraits.Valor);
			int traitLevel3 = character.GetTraitLevel(DefaultTraits.Generosity);
			int traitLevel4 = character.GetTraitLevel(DefaultTraits.Calculating);
			int traitLevel5 = character.GetTraitLevel(DefaultTraits.Honor);
			if (TaleWorlds.Library.MathF.Abs(traitLevel) + TaleWorlds.Library.MathF.Abs(traitLevel2) + TaleWorlds.Library.MathF.Abs(traitLevel3) + TaleWorlds.Library.MathF.Abs(traitLevel5) + TaleWorlds.Library.MathF.Abs(traitLevel4) >= 2)
			{
				TextObject textObject12 = new TextObject("{=Rvt11UBE}{BASIC_EVAL} {REPUTATION}");
				int num6 = traitLevel + traitLevel5 + traitLevel3;
				TextObject textObject13 = ((!character.IsLord) ? new TextObject("{=CkaSSJz1}{NOTABLE.LINK}...") : ((num6 > 0) ? new TextObject("{=9Box0Yj7}The {?NOTABLE.GENDER}lady{?}lord{\\?} {NOTABLE.LINK}... We're blessed.") : ((num6 != 0 && traitLevel < 0) ? new TextObject("{=aEL2S1PZ}Heavens protect us from the {?NOTABLE.GENDER}lady{?}lord{\\?} {NOTABLE.LINK}...") : new TextObject("{=DgxVa0OP}The {?NOTABLE.GENDER}lady{?}lord{\\?} {NOTABLE.LINK}..."))));
				character.SetPropertiesToTextObject(textObject13, "NOTABLE");
				character.SetPropertiesToTextObject(textObject12, "NOTABLE");
				textObject12.SetTextVariable("BASIC_EVAL", textObject13);
				TextObject textObject14 = Campaign.Current.ConversationManager.FindMatchingTextOrNull("informal_reputation", character.CharacterObject);
				character.SetPropertiesToTextObject(textObject14, "NOTABLE");
				textObject12.SetTextVariable("REPUTATION", textObject14.ToString());
				conversationScraps.Add(textObject12);
			}
			if (character.IsGangLeader && character.GetTraitLevel(DefaultTraits.Mercy) < 0)
			{
				TextObject textObject15 = new TextObject("{=Xqx0uZva}I told that silly {RANDOM_RELATIVE} of mine. I told him. I said, 'You take money from {NOTABLE.LINK}, {?NOTABLE.GENDER}she{?}he{\\?}'ll want back double. And if you value the bones in your hands, you'll pay.' I told him, I did.");
				textObject15.SetTextVariable("RANDOM_RELATIVE", new TextObject(list[MBRandom.RandomInt(0, list.Count)]));
				character.SetPropertiesToTextObject(textObject15, "NOTABLE");
				conversationScraps.Add(textObject15);
			}
			if (character.IsGangLeader && character.GetTraitLevel(DefaultTraits.Generosity) - character.GetTraitLevel(DefaultTraits.Mercy) >= 0)
			{
				TextObject textObject16 = new TextObject("{=PJ7BhyOy}I said to that good-for-nothing... I said to him, 'You leave my daughter alone, or {NOTABLE.LINK} will hear about it.' Hah! I don't see him tomcatting around outside my home any more, that's for sure.");
				character.SetPropertiesToTextObject(textObject16, "NOTABLE");
				conversationScraps.Add(textObject16);
			}
			if (character.IsGangLeader && character.GetTraitLevel(DefaultTraits.Honor) + character.GetTraitLevel(DefaultTraits.Generosity) + character.GetTraitLevel(DefaultTraits.Mercy) < -1)
			{
				TextObject textObject17 = new TextObject("{=ZhCRL9mY}All those bastards walking around drunk and bothering folks.. But they work for {NOTABLE.LINK} and you can't say a thing if you value your head.");
				character.SetPropertiesToTextObject(textObject17, "NOTABLE");
				conversationScraps.Add(textObject17);
			}
			if (character.IsGangLeader && character.GetTraitLevel(DefaultTraits.Mercy) >= 0)
			{
				TextObject textObject18 = new TextObject("{=GeJuBar0}Now, some will tell you that {NOTABLE.LINK} is no better than a common thief who deserves the gallows, but my cousin says {NOTABLE.FIRSTNAME} did him a good turn. So I don't know what to believe...");
				character.SetPropertiesToTextObject(textObject18, "NOTABLE");
				conversationScraps.Add(textObject18);
			}
			if (character.IsMerchant && Settlement.CurrentSettlement.IsTown)
			{
				List<Workshop> list4 = (from x in Settlement.CurrentSettlement.Town.Workshops
					where x.Owner == character
					orderby x.ProfitMade descending
					select x).ToList();
				if (list4.Count > 0)
				{
					Workshop workshop = list4[0];
					if (traitLevel3 > 0 && traitLevel < 0)
					{
						TextObject textObject19 = new TextObject("{=xP8cKZFE}They say the merchant {NOTABLE.LINK} is hiring at {?NOTABLE.GENDER}her{?}his{\\?} {.%}{SHOP_TYPE}{.%}. {?NOTABLE.GENDER}She{?}He{\\?}'s a harsh master but a fair one, they say.");
						textObject19.SetTextVariable("SHOP_TYPE", workshop.WorkshopType.Name);
						character.SetPropertiesToTextObject(textObject19, "NOTABLE");
						conversationScraps.Add(textObject19);
					}
					if (traitLevel3 + traitLevel > 0)
					{
						TextObject textObject20 = new TextObject("{=9O8a1Yz8}I heard in the market that {NOTABLE.LINK} is hiring at {?NOTABLE.GENDER}her{?}his{\\?} {.%}{SHOP_TYPE}{.%}. Treats {?NOTABLE.GENDER}her{?}his{\\?} workers well, they say.");
						textObject20.SetTextVariable("SHOP_TYPE", workshop.WorkshopType.Name);
						character.SetPropertiesToTextObject(textObject20, "NOTABLE");
						conversationScraps.Add(textObject20);
					}
					if (traitLevel5 < 0 && traitLevel + traitLevel3 < 0 && prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
					{
						TextObject textObject21 = new TextObject("{=8N3cs42a}So... The word is that {NOTABLE.LINK} is looking to cut costs at {?NOTABLE.GENDER}her{?}his{\\?} {.%}{SHOP_TYPE}{.%}. They say {?NOTABLE.GENDER}she{?}he{\\?}'s been docking the men's wages right and left.");
						textObject21.SetTextVariable("SHOP_TYPE", workshop.WorkshopType.Name);
						character.SetPropertiesToTextObject(textObject21, "NOTABLE");
						conversationScraps.Add(textObject21);
					}
					if (traitLevel5 < 0)
					{
						TextObject textObject22 = new TextObject("{=e9bLGCQu}They say {NOTABLE.LINK} is hiring at {?NOTABLE.GENDER}her{?}his{\\?} {.%}{SHOP_TYPE}{.%}. {?NOTABLE.GENDER}She{?}He{\\?}'s a slippery one.");
						textObject22.SetTextVariable("SHOP_TYPE", workshop.WorkshopType.Name);
						character.SetPropertiesToTextObject(textObject22, "NOTABLE");
						conversationScraps.Add(textObject22);
					}
					if (traitLevel + traitLevel3 < 0 && prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
					{
						TextObject textObject23 = new TextObject("{=LM1yg5tI}They say {NOTABLE.LINK} turned one of {?NOTABLE.GENDER}her{?}his{\\?} artisans at the {.%}{SHOP_TYPE}{.%} out on the street for not showing up to work at first light. Hard times, these.");
						textObject23.SetTextVariable("SHOP_TYPE", workshop.WorkshopType.Name);
						character.SetPropertiesToTextObject(textObject23, "NOTABLE");
						conversationScraps.Add(textObject23);
					}
					if (traitLevel + traitLevel3 < 0)
					{
						TextObject textObject24 = new TextObject("{=dmaCQda0}Yeah. About that {NOTABLE.LINK}. A hard {?NOTABLE.GENDER}one{?}master{\\?} to work for, they say. One mistake and you're out on the street.");
						textObject24.SetTextVariable("SHOP_TYPE", workshop.WorkshopType.Name);
						character.SetPropertiesToTextObject(textObject24, "NOTABLE");
						conversationScraps.Add(textObject24);
					}
					if (traitLevel4 > 0 && prosperityLevel >= SettlementComponent.ProsperityLevel.High)
					{
						TextObject textObject25 = new TextObject("{=TMhUM4tz}Heard that {NOTABLE.LINK} is making money hand over fist from {?NOTABLE.GENDER}her{?}his{\\?} {.%}{SHOP_TYPE}{.%}. A cunning one, {?NOTABLE.GENDER}she{?}he{\\?} is.");
						textObject25.SetTextVariable("SHOP_TYPE", workshop.WorkshopType.Name);
						character.SetPropertiesToTextObject(textObject25, "NOTABLE");
						conversationScraps.Add(textObject25);
					}
				}
			}
			int relation = character.GetRelation(leader);
			if (character.IsHeadman || character.IsArtisan)
			{
				if (prosperityLevel <= SettlementComponent.ProsperityLevel.Low && relation >= 10)
				{
					TextObject textObject26 = new TextObject("{=ko2Nb5im}I know times are hard, but {NOTABLE.LINK} says {?OWNER.GENDER}lady{?}lord{\\?} {OWNER.LINK} is doing what {?OWNER.GENDER}she{?}he{\\?} can, and I trust {NOTABLE.FIRSTNAME}.");
					character.SetPropertiesToTextObject(textObject26, "NOTABLE");
					leader.SetPropertiesToTextObject(textObject26, "OWNER");
					conversationScraps.Add(textObject26);
				}
				if (prosperityLevel >= SettlementComponent.ProsperityLevel.High && relation >= 10)
				{
					TextObject textObject27 = new TextObject("{=CdRtnwwX}Things are good, and {NOTABLE.LINK} says we should credit this to the wisdom of {?OWNER.GENDER}lady{?}lord{\\?} {OWNER.LINK}.");
					character.SetPropertiesToTextObject(textObject27, "NOTABLE");
					leader.SetPropertiesToTextObject(textObject27, "OWNER");
					conversationScraps.Add(textObject27);
				}
				if (character.GetTraitLevel(DefaultTraits.Honor) > 0)
				{
					TextObject textObject28 = new TextObject("{=Ku49Ipkf}I'll tell anyone who asks: {NOTABLE.LINK} is a righteous {?NOTABLE.GENDER}woman{?}man{\\?}, and speaks for the poor folk. The lords of this land must listen to {?NOTABLE.GENDER}her{?}him{\\?}, or misfortune will fall upon us.");
					character.SetPropertiesToTextObject(textObject28, "NOTABLE");
					conversationScraps.Add(textObject28);
				}
				if (character.GetTraitLevel(DefaultTraits.Mercy) > 0 && prosperityLevel <= SettlementComponent.ProsperityLevel.Mid)
				{
					TextObject textObject29 = new TextObject("{=P7aRBg2h}Thank the heavens we have {?NOTABLE.GENDER}women{?}men{\\?} like {NOTABLE.LINK} to help the poor in these hard times.");
					character.SetPropertiesToTextObject(textObject29, "NOTABLE");
					conversationScraps.Add(textObject29);
				}
				if (character.GetTraitLevel(DefaultTraits.Mercy) <= 0 && Settlement.CurrentSettlement.IsTown && !flag)
				{
					TextObject textObject30 = new TextObject("{=Fu47uNh4}My sister's husband is in the tavern with all the layabouts, throwing away his wages on drinking and dice while his family weeps. I hope {NOTABLE.LINK} gathers some righteous folk and sets fire to it! I'd carry a torch myself, I would.");
					character.SetPropertiesToTextObject(textObject30, "NOTABLE");
					conversationScraps.Add(textObject30);
				}
				if (leader.GetTraitLevel(DefaultTraits.Honor) >= 0 && relation > 0 && Settlement.CurrentSettlement.IsTown && !flag)
				{
					TextObject textObject31 = new TextObject("{=NOb4GcOT}{NOTABLE.LINK} says we must trust in the wisdom of the authorities, so long as they follow the laws of the Heavens.");
					character.SetPropertiesToTextObject(textObject31, "NOTABLE");
					conversationScraps.Add(textObject31);
				}
				if (relation < -10 && Settlement.CurrentSettlement.IsTown && !flag)
				{
					TextObject textObject32 = new TextObject("{=LaBQQ7ue}{NOTABLE.LINK} says that a fish rots from the head. Well, look around this place. I think we know what he's getting at.");
					character.SetPropertiesToTextObject(textObject32, "NOTABLE");
					conversationScraps.Add(textObject32);
				}
			}
			if (character.IsPreacher)
			{
				if (character.GetTraitLevel(DefaultTraits.Honor) > 0)
				{
					TextObject textObject33 = new TextObject("{=9XijWk1o}I'll tell anyone who asks: {NOTABLE.LINK} is a righteous {?NOTABLE.GENDER}woman{?}man{\\?}, and the Heavens speak through {?NOTABLE.GENDER}her{?}his{\\?} mouth. The lords of this land must listen to {?NOTABLE.GENDER}her{?}him{\\?}, or misfortune will fall upon us.");
					conversationScraps.Add(new TextObject(textObject33.ToString()));
				}
				if (character.GetTraitLevel(DefaultTraits.Mercy) > 0 && prosperityLevel <= SettlementComponent.ProsperityLevel.Mid)
				{
					TextObject textObject34 = new TextObject("{=P7aRBg2h}Thank the heavens we have {?NOTABLE.GENDER}women{?}men{\\?} like {NOTABLE.LINK} to help the poor in these hard times.");
					character.SetPropertiesToTextObject(textObject34, "NOTABLE");
					conversationScraps.Add(new TextObject(textObject34.ToString()));
				}
				if (character.GetTraitLevel(DefaultTraits.Mercy) <= 0 && Settlement.CurrentSettlement.IsTown && !flag)
				{
					TextObject textObject35 = new TextObject("{=Fu47uNh4}My sister's husband is in the tavern with all the layabouts, throwing away his wages on drinking and dice while his family weeps. I hope {NOTABLE.LINK} gathers some righteous folk and sets fire to it! I'd carry a torch myself, I would.");
					character.SetPropertiesToTextObject(textObject35, "NOTABLE");
					conversationScraps.Add(textObject35);
				}
				if (leader.GetTraitLevel(DefaultTraits.Honor) >= 0 && relation > 0 && Settlement.CurrentSettlement.IsTown && !flag)
				{
					TextObject textObject36 = new TextObject("{=NOb4GcOT}{NOTABLE.LINK} says we must trust in the wisdom of the authorities, so long as they follow the laws of the Heavens.");
					character.SetPropertiesToTextObject(textObject36, "NOTABLE");
					conversationScraps.Add(textObject36);
				}
				if (relation < -10 && Settlement.CurrentSettlement.IsTown && !flag)
				{
					TextObject textObject37 = new TextObject("{=LaBQQ7ue}{NOTABLE.LINK} says that a fish rots from the head. Well, look around this place. I think we know what he's getting at.");
					character.SetPropertiesToTextObject(textObject37, "NOTABLE");
					conversationScraps.Add(textObject37);
				}
			}
			if (character.IsRuralNotable)
			{
				if (character.GetTraitLevel(DefaultTraits.Honor) < 0 && character.GetTraitLevel(DefaultTraits.Generosity) <= 0 && character.GetTraitLevel(DefaultTraits.Mercy) <= 0)
				{
					TextObject textObject38 = new TextObject("{=avk9kNn1}Funny how the boundary stones on {NOTABLE.FIRSTNAME}'s land always seem a little bit closer every time you look at them.");
					character.SetPropertiesToTextObject(textObject38, "NOTABLE");
					conversationScraps.Add(textObject38);
				}
				if (character.GetTraitLevel(DefaultTraits.Generosity) < 0 && character.GetTraitLevel(DefaultTraits.Mercy) <= 0)
				{
					TextObject textObject39 = new TextObject("{=Ckou6bfg}Old skinflint {NOTABLE.FIRSTNAME} is my great-uncle's son, and not a blessed penny will {?NOTABLE.GENDER}she{?}he{\\?} give me in hard times.");
					character.SetPropertiesToTextObject(textObject39, "NOTABLE");
					conversationScraps.Add(textObject39);
				}
				if (character.GetTraitLevel(DefaultTraits.Calculating) > 0 && character.GetTraitLevel(DefaultTraits.Generosity) <= 0)
				{
					TextObject textObject40 = new TextObject("{=Xo5lc6sz}If you've got more mouths in your house than your land will feed, {NOTABLE.FIRSTNAME} will let you work a bit of {?NOTABLE.GENDER}her{?}his{\\?} property - but he'll take a third of your harvest, even if you're kin.");
					character.SetPropertiesToTextObject(textObject40, "NOTABLE");
					conversationScraps.Add(textObject40);
				}
				if (character.GetTraitLevel(DefaultTraits.Generosity) > 0)
				{
					TextObject textObject41 = new TextObject("{=DeoalRXY}One of {NOTABLE.FIRSTNAME}'s hunting hounds took down my cousin's sheep, but {NOTABLE.FIRSTNAME} paid twice the beast's value in compensation -- very fair, I say, very fair indeed.");
					character.SetPropertiesToTextObject(textObject41, "NOTABLE");
					conversationScraps.Add(textObject41);
				}
				if (character.GetTraitLevel(DefaultTraits.Calculating) - character.GetTraitLevel(DefaultTraits.Mercy) < 0)
				{
					TextObject textObject42 = new TextObject("{=o3MDpesZ}The way {NOTABLE.FIRSTNAME} helps out {?NOTABLE.GENDER}her{?}his{\\?} good-for-nothing relatives, {?NOTABLE.GENDER}she{?}he{\\?} won't have any seed grain for the next sewing -- and I, for one, will laugh.");
					character.SetPropertiesToTextObject(textObject42, "NOTABLE");
					conversationScraps.Add(textObject42);
				}
				if (character.GetTraitLevel(DefaultTraits.Honor) > 0 && character.GetTraitLevel(DefaultTraits.Generosity) < 0)
				{
					TextObject textObject43 = new TextObject("{=QyGQanO7}There's no doubt that {NOTABLE.FIRSTNAME} is a stiff-necked old goat, but you can't say {?NOTABLE.GENDER}she{?}he{\\?}'s not honest as the day is long.");
					character.SetPropertiesToTextObject(textObject43, "NOTABLE");
					conversationScraps.Add(textObject43);
				}
				if (character.GetTraitLevel(DefaultTraits.Generosity) + character.GetTraitLevel(DefaultTraits.Mercy) + character.GetTraitLevel(DefaultTraits.Calculating) > 0)
				{
					TextObject textObject44 = new TextObject("{=NazK9rwF}A decent {?NOTABLE.GENDER}woman{?}man{\\?}, {NOTABLE.FIRSTNAME} is, as rich folk go. Always keeps a bit of grain to help out any of those who ate their seed over the winter. Knows the worth of a good name, {?NOTABLE.GENDER}she{?}he{\\?} does.");
					character.SetPropertiesToTextObject(textObject44, "NOTABLE");
					conversationScraps.Add(textObject44);
				}
			}
		}
	}

	private bool conversation_villager_talk_start_on_condition()
	{
		if ((CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Villager || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Townsfolk) && PlayerEncounter.Current != null)
		{
			return PlayerEncounter.InsideSettlement;
		}
		return false;
	}

	private bool conversation_townsfolk_ask_asses_prices_on_condition()
	{
		return Settlement.CurrentSettlement.IsTown;
	}

	private void conversation_townsfolk_ask_asses_prices_on_consequence()
	{
	}
}
