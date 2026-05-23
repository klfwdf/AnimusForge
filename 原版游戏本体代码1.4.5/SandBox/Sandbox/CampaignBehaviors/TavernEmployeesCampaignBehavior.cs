using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.CampaignBehaviors;

public class TavernEmployeesCampaignBehavior : CampaignBehaviorBase
{
	private enum TavernInquiryCompanionType
	{
		Scout,
		Engineer,
		Surgeon,
		Quartermaster,
		FirstMate,
		Navigator,
		CaravanLeader,
		Leader,
		Roguery
	}

	public const int TavernCompanionInquiryCost = 2;

	private const int MinimumTavernCompanionInquirySkillLevel = 30;

	private const int BaseTunPrice = 50;

	private const int AskForClanInfoPrice = 500;

	private Settlement _orderedDrinkThisDayInSettlement;

	private bool _orderedDrinkThisVisit;

	private bool _hasMetWithRansomBroker;

	private bool _hasBoughtTunToParty;

	private Hero _inquiryCurrentCompanion;

	private TavernInquiryCompanionType _selectedCompanionType;

	private int _inquiryVariationIndex;

	private readonly Dictionary<TavernInquiryCompanionType, List<Hero>> _previouslyRecommendedCompanions = new Dictionary<TavernInquiryCompanionType, List<Hero>>();

	private float MaxTownDistanceAsDays => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) * 2f / (Campaign.Current.EstimatedAverageVillagerPartySpeed * (float)CampaignTime.HoursInDay);

	public override void RegisterEvents()
	{
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_orderedDrinkThisDayInSettlement", ref _orderedDrinkThisDayInSettlement);
		dataStore.SyncData("_orderedDrinkThisVisit", ref _orderedDrinkThisVisit);
		dataStore.SyncData("_hasMetWithRansomBroker", ref _hasMetWithRansomBroker);
		dataStore.SyncData("_hasBoughtTunToParty", ref _hasBoughtTunToParty);
	}

	public void DailyTick()
	{
		_orderedDrinkThisDayInSettlement = null;
	}

	public void WeeklyTick()
	{
		_hasBoughtTunToParty = false;
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
		_inquiryVariationIndex = MBRandom.NondeterministicRandomInt % 6;
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (!settlement.IsTown || CampaignMission.Current == null)
		{
			return;
		}
		Location location = CampaignMission.Current.Location;
		if (location != null && location.StringId == "tavern")
		{
			if (unusedUsablePointCount.TryGetValue("spawnpoint_tavernkeeper", out var value) && value > 0)
			{
				location.AddLocationCharacters(CreateTavernkeeper, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
			if (unusedUsablePointCount.TryGetValue("sp_tavern_wench", out value) && value > 0)
			{
				location.AddLocationCharacters(CreateTavernWench, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
			if (unusedUsablePointCount.TryGetValue("musician", out value) && value > 0)
			{
				location.AddLocationCharacters(CreateMusician, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
			}
			location.AddLocationCharacters(CreateRansomBroker, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
		}
		else if (location != null && location.StringId == "center" && !Campaign.Current.IsNight)
		{
			if (unusedUsablePointCount.TryGetValue("spawnpoint_tavernkeeper", out var value2))
			{
				location.AddLocationCharacters(CreateTavernkeeper, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
			if (unusedUsablePointCount.TryGetValue("sp_tavern_wench", out value2))
			{
				location.AddLocationCharacters(CreateTavernWench, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
			if (unusedUsablePointCount.TryGetValue("musician", out value2))
			{
				location.AddLocationCharacters(CreateMusician, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, value2);
			}
		}
	}

	public void OnMissionStarted(IMission mission)
	{
		_orderedDrinkThisVisit = false;
	}

	private static LocationCharacter CreateTavernWench(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject tavernWench = culture.TavernWench;
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(tavernWench, out var minimumAge, out var maximumAge);
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(tavernWench.Race, "_settlement");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(tavernWench)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_tavern_wench", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_barmaid"), useCivilianEquipment: true)
		{
			PrefabNamesForBones = { 
			{
				agentData.AgentMonster.OffHandItemBoneIndex,
				"kitchen_pitcher_b_tavern"
			} }
		};
	}

	private static LocationCharacter CreateTavernkeeper(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject tavernkeeper = culture.Tavernkeeper;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(tavernkeeper.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(tavernkeeper, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(tavernkeeper)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "spawnpoint_tavernkeeper", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_tavern_keeper"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateMusician(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject musician = culture.Musician;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(musician.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(musician, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(musician)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "musician", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_musician"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateRansomBroker(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject ransomBroker = culture.RansomBroker;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(ransomBroker.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(ransomBroker, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(ransomBroker)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "npc_common", fixedLocation: true, relation, null, useCivilianEquipment: true);
	}

	protected void AddDialogs(CampaignGameStarter cgs)
	{
		cgs.AddDialogLine("talk_common_to_tavernkeeper", "start", "tavernkeeper_talk", "{=QCuxL92I}Good day, {?PLAYER.GENDER}madam{?}sir{\\?}. How can I help you?", () => CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Tavernkeeper, null);
		cgs.AddPlayerLine("tavernkeeper_talk_to_get_quest", "tavernkeeper_talk", "tavernkeeper_ask_quests", "{=A61ppTa6}Do you know of anyone who might have a task for someone like me?", null, null);
		cgs.AddPlayerLine("tavernkeeper_get_clan_info_start", "tavernkeeper_talk", "tavernkeeper_offer_clan_info", "{=shXdvd5p}I'm looking for information about the owner of this town.", player_ask_information_about_the_owner_of_the_town_condition, null);
		cgs.AddDialogLine("tavernkeeper_get_clan_info_answer", "tavernkeeper_offer_clan_info", "player_offer_clan_info", "{=i96KTeph}I can sell you information about {OWNER_CLAN}, who are the owners of our town {SETTLEMENT} for {PRICE}{GOLD_ICON}.", tavernkeeper_offer_clan_info_on_condition, null);
		cgs.AddPlayerLine("tavernkeeper_get_clan_info_player_answer_1", "player_offer_clan_info", "tavernkeeper_pretalk", "{=VaxbQby7}That sounds like a great deal.", null, player_accepts_clan_info_offer_on_consequence, 100, player_accepts_clan_info_offer_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_get_clan_info_player_answer_2", "player_offer_clan_info", "tavernkeeper_pretalk", "{=CH7b5LaX}I have changed my mind.", null, null);
		cgs.AddPlayerLine("tavernkeeper_companion_info_start", "tavernkeeper_talk", "tavernkeeper_companion_info_tavernkeeper_answer", "{=JPiId1fb}I am looking for some people to hire with specific skills. Would you happen to know anyone looking for work in the towns nearby? ", tavernkeeper_talk_companion_on_condition, null);
		cgs.AddDialogLine("tavernkeeper_companion_info_answer", "tavernkeeper_companion_info_tavernkeeper_answer", "tavernkeeper_list_companion_types", "{=ASVkuYHG}I know a few. What kind of a person are you looking for?", null, null);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_select_scout", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=joCSXAQQ}I would travel much faster if I had a good scout by my side.", null, delegate
		{
			FindCompanionWithType(TavernInquiryCompanionType.Scout);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_select_engineer", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=NGcKLV88}A good engineer could help construct siege engines and new buildings in towns.", null, delegate
		{
			FindCompanionWithType(TavernInquiryCompanionType.Engineer);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_select_surgeon", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=Y5ztM8zq}My men would feel safer with a good surgeon to aid them in their time of need.", null, delegate
		{
			FindCompanionWithType(TavernInquiryCompanionType.Surgeon);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_select_quartermaster", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=88Fk9keT}I am sure I can do more with fewer supplies if I had a good quartermaster.", null, delegate
		{
			FindCompanionWithType(TavernInquiryCompanionType.Quartermaster);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_select_caravan_leader", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=kePH44eg}I am planning to sponsor my own caravans, and someone who could run them would be perfect.", null, delegate
		{
			FindCompanionWithType(TavernInquiryCompanionType.CaravanLeader);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_select_leader", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=9z0Yz8za}I need a lieutenant to be my right hand and help me direct my troops in battle.", null, delegate
		{
			FindCompanionWithType(TavernInquiryCompanionType.Leader);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_select_roguery", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=DMUsPekF}I need someone who knows a bit about the darker side of the world, who can handle villains and thieves.", null, delegate
		{
			FindCompanionWithType(TavernInquiryCompanionType.Roguery);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("tavernkeeper_companion_info_player_nevermind", "tavernkeeper_list_companion_types", "tavernkeeper_pretalk", "{=tdvnKIyS}Never mind.", null, null);
		cgs.AddDialogLine("tavernkeeper_companion_info_tavernkeeper_not_found_answer_1", "player_selected_companion_type", "tavernkeeper_talk_companion_end", "{=!}{CANNOT_THINK_OF_ANYONE_LINE}[rb:negative]", () => !FoundCompanion(), IncreaseVariationIndex);
		cgs.AddPlayerLine("tavernkeeper_companion_info_tavernkeeper_end", "tavernkeeper_talk_companion_end", "start", "{=PDf52VCf}Thank you for your time anyway.", null, IncreaseVariationIndex);
		cgs.AddDialogLine("tavernkeeper_companion_info_tavernkeeper_found_answer_companion_is_inside_1", "player_selected_companion_type", "player_companion_response", "{=QdEGu0CW}A {?INQUIRY_COMPANION.GENDER}woman{?}man{\\?} called {INQUIRY_COMPANION.LINK} has passed through here on his way to {COMPANION_SETTLEMENT}. You may catch up to {?INQUIRY_COMPANION.GENDER}her{?}him{\\?} if you hurry.[rb:positive]", () => _inquiryCurrentCompanion.CurrentSettlement != Settlement.CurrentSettlement && _inquiryVariationIndex % 3 == 0, IncreaseVariationIndex);
		cgs.AddDialogLine("tavernkeeper_companion_info_tavernkeeper_found_answer_companion_is_inside_2", "player_selected_companion_type", "player_companion_response", "{=WSAS3XLC}There was someone here not long ago, went by the name of {INQUIRY_COMPANION.LINK}. {?INQUIRY_COMPANION.GENDER}She{?}He{\\?} left for {COMPANION_SETTLEMENT}. Perhaps you can find them there, or on the road.[rb:positive]", () => _inquiryCurrentCompanion.CurrentSettlement != Settlement.CurrentSettlement && _inquiryVariationIndex % 3 == 1, IncreaseVariationIndex);
		cgs.AddDialogLine("tavernkeeper_companion_info_tavernkeeper_found_answer_companion_is_inside_3", "player_selected_companion_type", "player_companion_response", "{=ahydRFKe}There was someone who called {?INQUIRY_COMPANION.GENDER}herself{?}himself{\\?} {INQUIRY_COMPANION.LINK}. Sounds like the kind of person who might interest you, no? {?INQUIRY_COMPANION.GENDER}She{?}He{\\?} was headed for {COMPANION_SETTLEMENT}.[rb:positive]", () => _inquiryCurrentCompanion.CurrentSettlement != Settlement.CurrentSettlement && _inquiryVariationIndex % 3 == 2, IncreaseVariationIndex);
		cgs.AddDialogLine("tavernkeeper_companion_info_tavernkeeper_found_answer_companion_is_outside_1", "player_selected_companion_type", "player_companion_response", "{=gfmU4wiM}A {?INQUIRY_COMPANION.GENDER}woman{?}man{\\?} named {INQUIRY_COMPANION.LINK} is staying at my tavern. You may want to talk with him.", () => _inquiryVariationIndex % 3 == 0, IncreaseVariationIndex);
		cgs.AddDialogLine("tavernkeeper_companion_info_tavernkeeper_found_answer_companion_is_outside_2", "player_selected_companion_type", "player_companion_response", "{=qSy4Ns1N}There's someone who might meet your needs who is in here having a drink right now. Goes by the name of {INQUIRY_COMPANION.LINK}.", () => _inquiryVariationIndex % 3 == 1, IncreaseVariationIndex);
		cgs.AddDialogLine("tavernkeeper_companion_info_tavernkeeper_found_answer_companion_is_outside_3", "player_selected_companion_type", "player_companion_response", "{=rLcRwkqK}You might want to look around here for a {?INQUIRY_COMPANION.GENDER}lady{?}fellow{\\?} named {INQUIRY_COMPANION.LINK}. I believe {?INQUIRY_COMPANION.GENDER}she{?}he{\\?} might possess that kind of skill.", () => _inquiryVariationIndex % 3 == 2, IncreaseVariationIndex);
		cgs.AddPlayerLine("player_companion_response_to_tavernkeeper", "player_companion_response", "tavernkeeper_companion_info_tavernkeeper_answer", "{=SqrRaIU5}I would like to ask about someone with different expertise.", FoundCompanion, null);
		cgs.AddPlayerLine("player_companion_response_to_tavernkeeper_2", "player_companion_response", "player_selected_companion_type", "{=vx4ML2gX}Is there someone else other than {INQUIRY_COMPANION.LINK}?", FoundCompanion, delegate
		{
			FindCompanionWithType(_selectedCompanionType);
		}, 100, companion_type_select_clickable_condition);
		cgs.AddPlayerLine("player_companion_response_to_tavernkeeper_found", "player_companion_response", "start", "{=3FzBTb3w}Thank you for this information. It was {COMPANION_INQUIRY_COST}{GOLD_ICON} well spent.", FoundCompanion, null);
		cgs.AddPlayerLine("player_companion_response_to_tavernkeeper_not_found", "player_companion_response", "start", "{=PDf52VCf}Thank you for your time anyway.", () => !FoundCompanion(), null);
		cgs.AddPlayerLine("tavernkeeper_talk_to_leave", "tavernkeeper_talk", "close_window", "{=fF2BdOy9}I don't need anything now.", null, delegate
		{
			_previouslyRecommendedCompanions.Clear();
		});
		cgs.AddDialogLine("1972", "tavernkeeper_pretalk", "tavernkeeper_talk", "{=ds294zxi}Anything else?", null, null);
		cgs.AddDialogLine("talk_common_to_tavernmaid", "start", "tavernmaid_talk", "{=ddYWbO8b}The usual?", conversation_tavernmaid_offers_usual_on_condition, null);
		cgs.AddDialogLine("talk_common_to_tavernmaid_2", "start", "tavernmaid_talk", "{=x7k87vj3}What can I bring you, {?PLAYER.GENDER}madam{?}sir{\\?}? Would you like to taste our local speciality, {DRINK} with {FOOD}?", conversation_tavernmaid_offers_food_on_condition, null);
		cgs.AddDialogLine("talk_common_to_tavernmaid_3", "start", "tavernmaid_talk", "{=Tn9g83ry}Enjoying your drink, {?PLAYER.GENDER}madam{?}sir{\\?}?", conversation_tavernmaid_gossips_on_condition, null);
		cgs.AddPlayerLine("tavernmaid_order_food", "tavernmaid_talk", "tavernmaid_order_acknowledge", "{=E57VFXqU}I'll have that.", conversation_player_can_order_food_on_condition, null);
		cgs.AddDialogLine("tavernmain_order_acknowledge", "tavernmaid_order_acknowledge", "close_window", "{=3wb2dCfz}It'll be right up then, {?PLAYER.GENDER}ma'am{?}sir{\\?}.", null, conversation_tavernmaid_delivers_food_on_consequence);
		cgs.AddPlayerLine("tavernmaid_ask_tun", "tavernmaid_talk", "tavern_drink_morale_to_party", "{=oAaKaXEy}I really like this meal. I'd like it served to all my men.", conversation_tavernmaid_buy_tun_on_condition, null);
		cgs.AddPlayerLine("tavernmaid_leave", "tavernmaid_talk", "close_window", "{=Piq3oYmG}I'm fine, thank you.", null, null);
		cgs.AddDialogLine("tavernmaid_give_tun_gold", "tavern_drink_morale_to_party", "tun_give_gold", "{=bjNuUqTx}With pleasure, {?PLAYER.GENDER}madam{?}sir{\\?}. That will cost you {COST}{GOLD_ICON}.", calculate_tun_cost_on_condition, null);
		cgs.AddPlayerLine("tavernmaid_give_tun_gold_2", "tun_give_gold", "tavernmaid_enjoy", "{=nAM821Fb}Here you are.", null, can_buy_tun_on_consequence, 100, can_buy_tun_on_clickable_condition);
		cgs.AddPlayerLine("tavernmaid_not_give_tun_gold", "tun_give_gold", "start", "{=2PEKd3Sz}Actually, I changed my mind. Cancel the order...", null, null);
		cgs.AddDialogLine("tavernmaid_best_wishes", "tavernmaid_enjoy", "close_window", "{=ZGMfmNe0}Very generous of you, my {?PLAYER.GENDER}lady{?}lord{\\?}. Good health and fortune to all of you.", null, null);
		cgs.AddDialogLine("talk_bard", "start", "talk_bard_player", "{=!}{LYRIC_SCRAP}", conversation_talk_bard_on_condition, null);
		cgs.AddPlayerLine("talk_bard_player_leave", "talk_bard_player", "close_window", "{=sbczu2VI}Play on, good man.", null, null);
		cgs.AddDialogLine("tavernkeeper_quest_info", "tavernkeeper_ask_quests", "tavernkeeper_has_quest", "{=uUkCLZEo}Let's see... {ISSUE_GIVER_LIST}.", tavenkeeper_has_quest_on_condition, null);
		cgs.AddPlayerLine("tavernkeeper_player_thanks", "tavernkeeper_has_quest", "tavernkeeper_pretalk", "{=eALf5d30}Thanks!", null, null);
		cgs.AddDialogLine("tavernkeeper_turndown", "tavernkeeper_doesnot_have_quests", "tavernkeeper_talk", "{=py6Y46sa}No, I didn't hear any...", null, null);
		AddRansomBrokerDialogs(cgs);
	}

	private bool player_ask_information_about_the_owner_of_the_town_condition()
	{
		return Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan;
	}

	private void player_accepts_clan_info_offer_on_consequence()
	{
		foreach (Hero hero in Settlement.CurrentSettlement.OwnerClan.Heroes)
		{
			hero.IsKnownToPlayer = true;
		}
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 500);
	}

	private bool player_accepts_clan_info_offer_clickable_condition(out TextObject explanation)
	{
		bool flag = true;
		foreach (Hero hero in Settlement.CurrentSettlement.OwnerClan.Heroes)
		{
			if (!hero.IsKnownToPlayer)
			{
				flag = false;
			}
		}
		if (flag)
		{
			explanation = new TextObject("{=LBiZ9Rie}You already possess this information.");
			return false;
		}
		explanation = new TextObject("{=!}{INFORMATION_COST}{GOLD_ICON}.");
		MBTextManager.SetTextVariable("INFORMATION_COST", 500);
		if (Hero.MainHero.Gold < 500)
		{
			explanation = new TextObject("{=xVZVYNan}You don't have enough{GOLD_ICON}.");
			return false;
		}
		return true;
	}

	private bool tavernkeeper_offer_clan_info_on_condition()
	{
		MBTextManager.SetTextVariable("OWNER_CLAN", Settlement.CurrentSettlement.OwnerClan.EncyclopediaLinkWithName);
		MBTextManager.SetTextVariable("SETTLEMENT", Settlement.CurrentSettlement.EncyclopediaLinkWithName);
		MBTextManager.SetTextVariable("PRICE", 500);
		return true;
	}

	private void SetCannotThinkOfAnyoneLine(TavernInquiryCompanionType type)
	{
		TextObject text = ((!_previouslyRecommendedCompanions.ContainsKey(type)) ? ((_inquiryVariationIndex % 2 == 0) ? new TextObject("{=BYpoxUEB}I haven't heard of someone with such skills looking for work for a while now.") : new TextObject("{=SbYlYGFA}Sorry. No one like that has passed through here for a while.")) : new TextObject("{=eeIX6loR}I can't think of anyone else."));
		MBTextManager.SetTextVariable("CANNOT_THINK_OF_ANYONE_LINE", text);
	}

	private void IncreaseVariationIndex()
	{
		_inquiryVariationIndex++;
	}

	private bool FoundCompanion()
	{
		return _inquiryCurrentCompanion != null;
	}

	public void FindCompanionWithType(PartyRole role)
	{
		switch (role)
		{
		case PartyRole.FirstMate:
			FindCompanionWithType(TavernInquiryCompanionType.FirstMate);
			break;
		case PartyRole.Navigator:
			FindCompanionWithType(TavernInquiryCompanionType.Navigator);
			break;
		default:
			Debug.FailedAssert("Wrong function call! This only should be called for First Mate and Navigator.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\CampaignBehaviors\\TavernEmployeesCampaignBehavior.cs", "FindCompanionWithType", 353);
			break;
		}
	}

	private void FindCompanionWithType(TavernInquiryCompanionType companionType)
	{
		int num = 30;
		Hero inquiryCurrentCompanion = null;
		float num2 = MaxTownDistanceAsDays * Campaign.Current.EstimatedAverageVillagerPartySpeed * (float)CampaignTime.HoursInDay;
		foreach (Town allTown in Town.AllTowns)
		{
			if (allTown.Settlement.HeroesWithoutParty.Count <= 0 || !(Campaign.Current.Models.MapDistanceModel.GetDistance(allTown.Settlement, Settlement.CurrentSettlement, isFromPort: false, isTargetingPort: false, (!Settlement.CurrentSettlement.HasPort || !allTown.Settlement.HasPort) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.All) < num2))
			{
				continue;
			}
			foreach (Hero item in allTown.Settlement.HeroesWithoutParty)
			{
				int num3 = 0;
				_previouslyRecommendedCompanions.TryGetValue(companionType, out var value);
				if (item.IsWanderer && (value == null || !value.Contains(item)))
				{
					switch (companionType)
					{
					case TavernInquiryCompanionType.Scout:
						num3 = item.GetSkillValue(Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(PartyRole.Scout));
						break;
					case TavernInquiryCompanionType.Engineer:
						num3 = item.GetSkillValue(Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(PartyRole.Engineer));
						break;
					case TavernInquiryCompanionType.Surgeon:
						num3 = item.GetSkillValue(Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(PartyRole.Surgeon));
						break;
					case TavernInquiryCompanionType.Quartermaster:
						num3 = item.GetSkillValue(Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(PartyRole.Quartermaster));
						break;
					case TavernInquiryCompanionType.FirstMate:
						num3 = item.GetSkillValue(Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(PartyRole.FirstMate));
						break;
					case TavernInquiryCompanionType.Navigator:
						num3 = item.GetSkillValue(Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(PartyRole.Navigator));
						break;
					case TavernInquiryCompanionType.CaravanLeader:
						num3 += item.GetSkillValue(DefaultSkills.Trade);
						break;
					case TavernInquiryCompanionType.Leader:
						num3 = item.GetSkillValue(DefaultSkills.Leadership);
						num3 += item.GetSkillValue(DefaultSkills.Tactics);
						break;
					case TavernInquiryCompanionType.Roguery:
						num3 = item.GetSkillValue(DefaultSkills.Roguery);
						break;
					}
				}
				if (num3 > num)
				{
					num = num3;
					inquiryCurrentCompanion = item;
				}
			}
		}
		_inquiryCurrentCompanion = inquiryCurrentCompanion;
		_selectedCompanionType = companionType;
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 2);
		if (_inquiryCurrentCompanion != null)
		{
			StringHelpers.SetCharacterProperties("INQUIRY_COMPANION", _inquiryCurrentCompanion.CharacterObject);
			MBTextManager.SetTextVariable("COMPANION_SETTLEMENT", _inquiryCurrentCompanion.CurrentSettlement.EncyclopediaLinkWithName);
			_inquiryCurrentCompanion.IsKnownToPlayer = true;
			if (_previouslyRecommendedCompanions.ContainsKey(_selectedCompanionType))
			{
				_previouslyRecommendedCompanions[_selectedCompanionType].Add(_inquiryCurrentCompanion);
			}
			else
			{
				_previouslyRecommendedCompanions.Add(_selectedCompanionType, new List<Hero> { _inquiryCurrentCompanion });
			}
		}
		SetCannotThinkOfAnyoneLine(companionType);
	}

	private bool conversation_talk_bard_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Musician)
		{
			List<string> list = new List<string>();
			Settlement randomElementWithPredicate = Settlement.All.GetRandomElementWithPredicate((Settlement x) => x.IsTown && x.Culture == Settlement.CurrentSettlement.Culture && x != Settlement.CurrentSettlement);
			MBTextManager.SetTextVariable("RANDOM_TOWN", randomElementWithPredicate.Name);
			list.Add("{=3n1KRLpZ}'My love is far {newline} I know not where {newline} Perhaps the winds shall tell me'");
			list.Add("{=NQOQb0C9}'And many thousand bodies lay a-rotting in the sun {newline} But things like that must be you know for kingdoms to be won'");
			list.Add("{=bs8ayCGX}'A warrior brave you might surely be {newline} With your blade and your helm and your bold fiery steed {newline} But I'll give you a warning you'd be wise to heed {newline} Don't toy with the fishwives of {RANDOM_TOWN}'");
			list.Add("{=3n1KRLpZ}'My love is far {newline} I know not where {newline} Perhaps the winds shall tell me'");
			list.Add("{=YequZz6U}'Oh the maidens of {RANDOM_TOWN} are merry and fair {newline} Plotting their mischief with flowers in their hair {newline} Were I still a young man I sure would be there {newline} But now I'll take warmth over trouble'");
			list.Add("{=CM8Tr3lL}'Oh my pocket's been picked {newline} And my shirt's drenched with sick {newline} And my head feels like it's come a fit to bursting'");
			list.Add("{=DFkzQHRQ}'For all the silks of the Padishah {newline} For all the Emperor's gold {newline} For all the spice in the distance East...'");
			list.Add("{=2fbLBXtT}'O'er the whale-road she sped {newline} She were manned by the dead  {newline} And the clouds followed black in her wake'");
			string value = list[MBRandom.RandomInt(0, list.Count)];
			MBTextManager.SetTextVariable("LYRIC_SCRAP", new TextObject(value));
			return true;
		}
		return false;
	}

	private static bool companion_type_select_clickable_condition(out TextObject explanation)
	{
		explanation = new TextObject("{=!}{COMPANION_INQUIRY_COST}{GOLD_ICON}.");
		MBTextManager.SetTextVariable("COMPANION_INQUIRY_COST", 2);
		if (Hero.MainHero.Gold < 2)
		{
			explanation = new TextObject("{=xVZVYNan}You don't have enough{GOLD_ICON}.");
			return false;
		}
		return true;
	}

	private void AddRansomBrokerDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("ransom_broker_start", "start", "ransom_broker_intro", "{=!}{RANSOM_BROKER_INTRO}", conversation_ransom_broker_start_on_condition, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_intro", "ransom_broker_intro", "ransom_broker_2", "{=TGYJUUn0}Go on.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_intro_2_di", "ransom_broker_2", "ransom_broker_3", "{=MFDb5duu}Splendid! I suspect that you may, in your line of work, occasionally acquire a few captives. I could possibly take them off your hands. I'd pay you, of course.", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_intro_2", "ransom_broker_3", "ransom_broker_4", "{=bPqwLopK}Mm. Are you a slaver?", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_intro_3", "ransom_broker_4", "ransom_broker_5", "{=YCC0hPuC}Ah, no. Slavers are rare these days. It used to be that, when the Empire and its neighbors made war upon each other, the defeated were usually taken as slaves. That helped pay for the war, you see! But today, for better or for worse, that is not practical. What with the frontiers broken and raiders crossing this way and that, there are far too many opportunities for captives to escape. But that does not mean that war cannot be profitable! Indeed it still can!", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_intro_4", "ransom_broker_5", "ransom_broker_6", "{=8bfzW0np}Many captives are still taken, and their families will pay to have them back. Men such as I criss-cross the Empire and the outer kingdoms, acquiring prisoners, and contacting their kin for a suitable ransom.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_intro_5", "ransom_broker_6", "ransom_broker_info_talk", "{=rLlIVqmY}So, were you to acquire a few prisoners in one of your victorious affrays, and wish to relieve yourself of their care and feeding, I or one of my colleagues would be happy to pay for them as a sort of speculative investment.", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_info_talk_player_1", "ransom_broker_info_talk", "ransom_broker_families", "{=QHoCsSZX}What if their families can't pay?", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_info_talk_player_2", "ransom_broker_info_talk", "ransom_broker_prices", "{=btA10FML}What can I get for a prisoner?", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_info_talk_player_3", "ransom_broker_info_talk", "ransom_broker_ransom_me", "{=nwJPPIvn}Would you be able to ransom me if I were taken?", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_info_talk_player_4", "ransom_broker_info_talk", "ransom_broker_pretalk", "{=TyultuzK}That's all I need to know. Thank you.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_families", "ransom_broker_families", "ransom_broker_info_talk", "{=zxonBgY2}Oh, I suppose I could sell them to the republics of Geroia, to row their galleys, although even in Geroia they prefer free oarsmen these days... But it rarely comes to that. You'd be surprised what sorts of treasures a peasant can dig out of his cowshed or wheedle out of his cousins!", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_prices", "ransom_broker_prices", "ransom_broker_info_talk", "{=PLbxHyPu}It varies. I fancy that I have a fine eye for assessing a ransom. There are a dozen little things about a man that will tell you whether he goes to bed hungry, or spices his meat with pepper and cloves from the east. The real money of course is in the aristocracy, and if you ever want to do my job you'll want to learn about every landowning family or tribal chief in Calradia, their estates, their offspring both lawful and bastard, and, of course, their credit with the merchants.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_ransom_me", "ransom_broker_ransom_me", "ransom_broker_info_talk", "{=4tY23HWb}Of course. I'm welcome in every court in Calradia. There's not many who can say that! So always be sure to keep a pot of denars buried somewhere, and a loyal servant who can find it in a hurry.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_start_has_met", "start", "ransom_broker_talk", "{=w4yxgY3F}Greetings. If you have any prisoners, I will be happy to buy them from you.", conversation_ransom_broker_start_has_met_on_condition, null);
		campaignGameStarter.AddDialogLine("ransom_broker_pretalk", "ransom_broker_pretalk", "ransom_broker_talk", "{=AQi1arUp}Anyway, if you have any prisoners, I will be happy to buy them from you.", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_talk_player_1", "ransom_broker_talk", "ransom_broker_sell_prisoners", "{=cAVxYAdw}Then you'd better bring your purse. I have got prisoners to sell.", conversation_ransom_broker_open_party_screen_on_condition, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_talk_player_2", "ransom_broker_talk", "ransom_broker_2", "{=Yac7bSU3}Tell me about what you do again.", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_talk_player_4", "ransom_broker_talk", "ransom_broker_no_prisoners", "{=CQMkh88h}I don't have any prisoners to sell, but that's good to know.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_no_prisoners", "ransom_broker_no_prisoners", "close_window", "{=mEsaiLOR}Very well then. If you happen to have any more prisoners, you know where to find me.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_sell_prisoners", "ransom_broker_sell_prisoners", "ransom_broker_sell_prisoners_3", "{=xFmYRCHs}Let me see what you have...", null, conversation_ransom_broker_sell_prisoners_on_consequence);
		campaignGameStarter.AddDialogLine("ransom_broker_sell_prisoners_3", "ransom_broker_sell_prisoners_3", "ransom_broker_pretalk", "{=3BvfOe1y}Very well then. You catch some more and you want me to take them off of your hands, you know where to find me...", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_sell_prisoners_2", "ransom_broker_sell_prisoners_2", "close_window", "{=fQaPv0Xl}I will be staying here for a few days. Let me know if you need my services.", null, null);
	}

	private bool conversation_tavernmaid_offers_usual_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.TavernWench && !_orderedDrinkThisVisit)
		{
			return _orderedDrinkThisDayInSettlement == Settlement.CurrentSettlement;
		}
		return false;
	}

	private bool conversation_tavernmaid_offers_food_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.TavernWench && _orderedDrinkThisDayInSettlement != Settlement.CurrentSettlement)
		{
			if (MobileParty.MainParty.CurrentSettlement.Culture.StringId == "vlandia")
			{
				MBTextManager.SetTextVariable("DRINK", new TextObject("{=07qBenIW}a flagon of ale"));
				MBTextManager.SetTextVariable("FOOD", new TextObject("{=uJceH1Dv}a Sunor sausage"));
				if (MobileParty.MainParty.CurrentSettlement.Position.Y < 150f)
				{
					MBTextManager.SetTextVariable("FOOD", new TextObject("{=07QlFlXK}a plate of herrings"));
				}
			}
			if (MobileParty.MainParty.CurrentSettlement.Culture.StringId == "empire")
			{
				MBTextManager.SetTextVariable("DRINK", new TextObject("{=ybXgaKEv}a glass of local wine"));
				MBTextManager.SetTextVariable("FOOD", new TextObject("{=IBhZGxxm}a plate of olives"));
				if (MobileParty.MainParty.CurrentSettlement.Position.X < 300f)
				{
					MBTextManager.SetTextVariable("FOOD", new TextObject("{=d18Ul2Zl}a plate of sardines"));
				}
			}
			if (MobileParty.MainParty.CurrentSettlement.Culture.StringId == "khuzait")
			{
				MBTextManager.SetTextVariable("DRINK", new TextObject("{=WZbrxhYm}a flask of kefir"));
				MBTextManager.SetTextVariable("FOOD", new TextObject("{=0qc11kmz}a plate of mutton dumplings"));
			}
			if (MobileParty.MainParty.CurrentSettlement.Culture.StringId == "aserai")
			{
				MBTextManager.SetTextVariable("DRINK", new TextObject("{=AULqrp7D}a glass of Calradian wine"));
				MBTextManager.SetTextVariable("FOOD", new TextObject("{=GhPGpR90}a plate of dates"));
			}
			if (MobileParty.MainParty.CurrentSettlement.Culture.StringId == "sturgia")
			{
				MBTextManager.SetTextVariable("DRINK", new TextObject("{=bZbFrIUr}a mug of kvass"));
				MBTextManager.SetTextVariable("FOOD", new TextObject("{=LPBVTiV6}a strip of bacon"));
			}
			if (MobileParty.MainParty.CurrentSettlement.Culture.StringId == "battania")
			{
				MBTextManager.SetTextVariable("DRINK", new TextObject("{=vEHaOSIT}a mug of sour beer"));
				MBTextManager.SetTextVariable("FOOD", new TextObject("{=z4arML8E}a strip of dried venison"));
			}
			if (MobileParty.MainParty.CurrentSettlement.Culture.StringId == "nord")
			{
				MBTextManager.SetTextVariable("DRINK", new TextObject("{=z47FuQ3f}a horn of barley beer"));
				MBTextManager.SetTextVariable("FOOD", new TextObject("{=gcB54MUl}some buttered hardfish"));
			}
			return true;
		}
		return false;
	}

	private void conversation_tavernmaid_delivers_food_on_consequence()
	{
		_orderedDrinkThisDayInSettlement = Settlement.CurrentSettlement;
		_orderedDrinkThisVisit = true;
	}

	private bool conversation_tavernmaid_gossips_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.TavernWench)
		{
			return _orderedDrinkThisDayInSettlement == Settlement.CurrentSettlement;
		}
		return false;
	}

	private bool conversation_player_can_order_food_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.TavernWench)
		{
			return !_orderedDrinkThisVisit;
		}
		return false;
	}

	private static bool tavenkeeper_has_quest_on_condition()
	{
		List<IssueBase> list = IssueManager.GetIssuesInSettlement(Hero.MainHero.CurrentSettlement).ToList();
		if (list.Count > 0)
		{
			list.Shuffle();
			if (list.Count == 1)
			{
				MBTextManager.SetTextVariable("ISSUE_GIVER_LIST", "{=roTCX8S8}{ISSUE_GIVER_1.LINK} mentioned something about needing someone to do some work.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER_1", list[0].IssueOwner.CharacterObject);
			}
			else if (list.Count == 2)
			{
				MBTextManager.SetTextVariable("ISSUE_GIVER_LIST", "{=79XElnsg}{ISSUE_GIVER_1.LINK} mentioned something about needing someone to do some work. And I think {ISSUE_GIVER_2.LINK} was looking for someone.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER_1", list[0].IssueOwner.CharacterObject);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER_2", list[1].IssueOwner.CharacterObject);
			}
			else
			{
				MBTextManager.SetTextVariable("ISSUE_GIVER_LIST", "{=SIxE2LGn}{ISSUE_GIVER_1.LINK} mentioned something about needing someone to do some work. And I think {ISSUE_GIVER_2.LINK} and {ISSUE_GIVER_3.LINK} were looking for someone.");
				StringHelpers.SetCharacterProperties("ISSUE_GIVER_1", list[0].IssueOwner.CharacterObject);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER_2", list[1].IssueOwner.CharacterObject);
				StringHelpers.SetCharacterProperties("ISSUE_GIVER_3", list[2].IssueOwner.CharacterObject);
			}
			return true;
		}
		MBTextManager.SetTextVariable("ISSUE_GIVER_LIST", "{=RlP8aYVJ}Nobody is looking for help right now.");
		return true;
	}

	private bool tavernkeeper_talk_companion_on_condition()
	{
		_inquiryCurrentCompanion = null;
		return true;
	}

	private bool conversation_tavernmaid_buy_tun_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.TavernWench && _orderedDrinkThisDayInSettlement == Settlement.CurrentSettlement && !_hasBoughtTunToParty)
		{
			return PartyBase.MainParty.MemberRoster.Count > 1;
		}
		return false;
	}

	private bool can_buy_tun_on_clickable_condition(out TextObject explanation)
	{
		if (Hero.MainHero.Gold < get_tun_price())
		{
			explanation = new TextObject("{=xVZVYNan}You don't have enough{GOLD_ICON}.");
			return false;
		}
		explanation = null;
		return true;
	}

	private void can_buy_tun_on_consequence()
	{
		int tun_price = get_tun_price();
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, tun_price);
		MobileParty.MainParty.RecentEventsMorale += 2f;
		_hasBoughtTunToParty = true;
	}

	private static bool calculate_tun_cost_on_condition()
	{
		int tun_price = get_tun_price();
		MBTextManager.SetTextVariable("COST", tun_price);
		return true;
	}

	private static int get_tun_price()
	{
		return (int)(50f + (float)MobileParty.MainParty.MemberRoster.TotalHealthyCount * 0.2f);
	}

	private bool conversation_ransom_broker_start_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.RansomBroker && !_hasMetWithRansomBroker)
		{
			_hasMetWithRansomBroker = true;
			MBTextManager.SetTextVariable("RANSOM_BROKER_INTRO", "{=Y7tozytM}Hello, {?PLAYER.GENDER}madam{?}sir{\\?}. You have the bearing of a warrior. Do you have a minute? We may have interests in common.");
			if (Settlement.CurrentSettlement.OwnerClan == Hero.MainHero.Clan || Settlement.CurrentSettlement.MapFaction.Leader == Hero.MainHero)
			{
				MBTextManager.SetTextVariable("RANSOM_BROKER_INTRO", "{=6zxo4AU9}This is quite the honor, your {?PLAYER.GENDER}ladyship{?}lordship{\\?}. Do you have a minute? I may be able to do you a service.");
			}
			return true;
		}
		return false;
	}

	private bool conversation_ransom_broker_open_party_screen_on_condition()
	{
		return MobileParty.MainParty.Party.NumberOfPrisoners > 0;
	}

	private bool conversation_ransom_broker_start_has_met_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.RansomBroker)
		{
			return _hasMetWithRansomBroker;
		}
		return false;
	}

	private void conversation_ransom_broker_sell_prisoners_on_consequence()
	{
		PartyScreenHelper.OpenScreenAsRansom();
	}
}
