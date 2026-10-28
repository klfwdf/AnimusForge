using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class ArenaMasterCampaignBehavior : CampaignBehaviorBase
{
	private List<Settlement> _arenaMasterHasMetInSettlements = new List<Settlement>();

	private bool _knowTournaments;

	private bool _enteredPracticeFightFromMenu;

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, AfterMissionStarted);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_arenaMasterHasMetInSettlements", ref _arenaMasterHasMetInSettlements);
		dataStore.SyncData("_knowTournaments", ref _knowTournaments);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
		AddGameMenus(campaignGameStarter);
	}

	private void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenuOption("town_arena", "mno_enter_practice_fight", "{=9pg3qc6N}Practice fight", game_menu_enter_practice_fight_on_condition, game_menu_enter_practice_fight_on_consequence, isLeave: false, 1);
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty == MobileParty.MainParty && settlement.IsTown)
		{
			AddArenaMaster(settlement);
		}
	}

	private void OnGameLoadFinished()
	{
		if (Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && LocationComplex.Current != null && Settlement.CurrentSettlement.IsTown && PlayerEncounter.LocationEncounter != null && !Settlement.CurrentSettlement.IsUnderSiege)
		{
			AddArenaMaster(Settlement.CurrentSettlement);
		}
	}

	private void AddArenaMaster(Settlement settlement)
	{
		settlement.LocationComplex.GetLocationWithId("arena").AddLocationCharacters(CreateTournamentMaster, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
	}

	protected void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("arena_master_tournament_meet", "start", "arena_intro_1", "{=GAsVO8cZ}Good day, friend. I'll bet you came here for the games, or as they say nowadays, the tournament!", conversation_arena_master_tournament_meet_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_master_tournament_meet_2", "start", "arena_intro_1a", "{=rqFKxm24}Greetings, friend. If you came for the games, the big fights, I'm afraid you're out of luck. There won't be games, or a 'tournament' as they say nowadays, any time soon.", conversation_arena_master_no_tournament_meet_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_master_meet_no_intro", "start", "arena_master_talk", "{=ZvzxcRbc}Good day, friend. You look like you know your way around an arena. How can I help you?", conversation_arena_master_player_knows_arenas_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_master_meet_start", "start", "arena_master_talk", "{=dgNCuuUL}Hello, {PLAYER.NAME}. Good to see you again.", conversation_arena_master_meet_start_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_master_meet_start_2", "start", "arena_master_post_practice_fight_talk", "{=nmPaCLHp}{FIGHT_DEBRIEF} Do you want to give it another go?", conversation_arena_master_post_fight_on_condition, null);
		campaignGameStarter.AddPlayerLine("arena_intro_1", "arena_intro_1", "arena_intro_tournament", "{=j9RrkCvM}There's a tournament going on?", null, null);
		campaignGameStarter.AddPlayerLine("arena_intro_2", "arena_intro_1a", "arena_intro_no_tournament", "{=W1wVPNpy}I've heard of these games...", null, null);
		campaignGameStarter.AddDialogLine("arena_intro_3", "arena_intro_tournament", "arena_intro_tournament_2", "{=GAq7KAf0}You bet! Say, you look like a fighter. You should join. Back in the old days it was all condemned criminals and fights to the death, but nowadays they use blunted weapons.", null, null);
		campaignGameStarter.AddDialogLine("arena_intro_3a", "arena_intro_tournament_2", "arena_intro_practice_fights", "{=VH27tpkT}It's quite the opportunity to make your name. You risk no more than your teeth, and didn't the Heavens give us thirty of those, just to have a few spare for grand opportunities like this?", null, null);
		campaignGameStarter.AddDialogLine("arena_intro_4", "arena_intro_no_tournament", "arena_intro_no_tournament_2", "{=EA2JcVcb}As well you might! They're a grand old imperial custom that's now spread all over Calradia. Back in the old days, they'd give a hundred condemned criminals swords and have them slash at each other until the sands were drenched in blood![if:convo_merry]", null, null);
		campaignGameStarter.AddDialogLine("arena_intro_4a", "arena_intro_no_tournament_2", "arena_intro_no_tournament_3", "{=EFKxbLaO}Nowadays things are a little different, of course. The emperors got worried about the people's morals and steered them toward more virtuous kinds of killing, like wars. But the games still go on, just with blunted weapons.", null, null);
		campaignGameStarter.AddDialogLine("arena_intro_5", "arena_intro_no_tournament_3", "arena_intro_no_tournament_4", "{=LqkxF5Op}During the games, all the best fighters from the area form teams and pummel each other. Not quite as much fun for the crowd as watching gladiators spill their guts out, of course, but healthier for the participants.[if:convo_approving]", null, null);
		campaignGameStarter.AddDialogLine("arena_intro_5a", "arena_intro_no_tournament_4", "arena_intro_practice_fights", "{=jy1o5cNT}You're a warrior, are you not? The games are a fine way to make your name. The local merchants put together a nice fat purse for the winner to attract the talent.", null, null);
		campaignGameStarter.AddDialogLine("arena_intro_6", "arena_intro_practice_fights", "arena_intro_perk_reset", "{=iLuezAbk}When there's no tournament, it's still worth coming by. A lot of fighters spend their time here practicing to keep in trim, and we'll award the winners a few coins for their troubles.", null, null);
		campaignGameStarter.AddPlayerLine("arena_tournament_rules", "arena_intro_4", "arena_tournament_rules", "{=aHGbTpLp}Tell me how tournaments work.", null, null);
		campaignGameStarter.AddPlayerLine("arena_practice_fight_rules", "arena_intro_4", "arena_practice_fight_rules", "{=H2aaMAe5}Tell me how the practice fights work.", null, null);
		campaignGameStarter.AddPlayerLine("arena_prizes", "arena_intro_4", "arena_prizes", "{=7pH9MzS1}So you pay us to fight? What's in it for you?", null, null);
		campaignGameStarter.AddPlayerLine("arena_prizes_2", "arena_intro_4", "arena_master_pre_talk", "{=R2HP4EiX}I don't have any more questions.", null, null);
		campaignGameStarter.AddDialogLine("arena_master_reminder", "arena_master_reminder", "arena_intro_4", "{=k7ebznzr}Yes?", null, null);
		campaignGameStarter.AddDialogLine("arena_prizes_answer", "arena_prizes", "arena_prizes_amounts", "{=bUmacxw7}Well, even the practice fights draw those who like to bet on the outcome. But the tournaments! Those pull in crowds from miles around. The merchants love a tournament, and that's why they pony up the silver we need to pay the good souls like you who take and receive the hard knocks.", null, null);
		campaignGameStarter.AddDialogLine("arena_training_practice_fight_intro", "arena_tournament_rules", "arena_intro_3a", "{=o0H8Qs0D}The rules of the tournament are standard across Calradia, even outside the Empire. We match the fighters up by drawing lots. Sometimes you're part of a team, and sometimes you fight by yourself.", null, null);
		campaignGameStarter.AddDialogLine("arena_training_practice_fight_intro_1c", "arena_intro_3a", "arena_intro_4", "{=Jgkz4uo6}The lots also determine what weapons you get. The winners of each match proceed to the next round. When only two are left, they battle each other to be declared champion.", null, null);
		campaignGameStarter.AddDialogLine("arena_training_practice_fight_intro_1a", "arena_practice_fight_rules", "arena_intro_4", "{=cPmV8S4e}We leave the arena open to anyone who wants to practice. There are no rules, no teams. Everyone beats at each other until there is only one fighter left standing. Sounds like fun, eh?[ib:confident2]", null, null);
		campaignGameStarter.AddPlayerLine("arena_training_practice_fight_intro_2", "arena_prizes_amounts", "arena_tournament_reward", "{=WwbDoZXg}How much are the prizes in the tournaments?", null, null);
		campaignGameStarter.AddPlayerLine("arena_training_practice_fight_intro_3", "arena_prizes_amounts", "arena_practice_fight_reward", "{=Z4MreMZz}How much are the prizes in the practice fights?", null, null);
		campaignGameStarter.AddPlayerLine("arena_training_practice_fight_intro_4", "arena_prizes_amounts", "arena_master_pre_talk", "{=4vAbAIqi}Okay. I think I get it.", null, null);
		campaignGameStarter.AddDialogLine("arena_training_practice_fight_intro_reward", "arena_practice_fight_reward", "arena_joining_ask", "{=!}{ARENA_REWARD}", conversation_arena_practice_fight_explain_reward_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_training_practice_fight_intro_reward_2", "arena_tournament_reward", "arena_joining_ask", "{=!}{TOURNAMENT_REWARD}", conversation_arena_tournament_explain_reward_on_condition, null);
		campaignGameStarter.AddPlayerLine("arena_training_practice_fight_intro_5", "arena_joining_ask", "arena_joining_answer", "{=Te4pxfWF}So can I join?", null, null);
		campaignGameStarter.AddDialogLine("arena_training_practice_fight_intro_6", "arena_joining_answer", "arena_master_talk", "{=bBVLVT7L}Certainly! Looks like a few of our lads are warming up now for the tournament. You can go and hop in if you want to. Or come back later if you just want to practice.[ib:warrior]", conversation_town_has_tournament_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_training_practice_fight_intro_7", "arena_joining_answer", "arena_master_talk", "{=KtrZs3yA}Certainly! The arena is open to anyone who doesn't mind hard knocks. Looks like a few of our lads are warming up now. You can go and hop in if you want to. Or come back later when there's a tournament.[ib:warrior]", null, null);
		campaignGameStarter.AddDialogLine("arena_master_pre_talk_explain", "arena_master_explain", "arena_prizes_amounts", "{=ke0IvBXb}Anything else I can explain?", null, null);
		campaignGameStarter.AddDialogLine("arena_master_ask_what_to_do", "arena_master_pre_talk", "arena_master_talk", "{=arena_master_24}So, what would you like to do?", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_sign_up_tournament", "arena_master_talk", "arena_master_enter_tournament", "{=arena_master_25}Sign me up for the tournament.", conversation_town_has_tournament_on_condition, null, 100, conversation_town_arena_fight_join_check_on_condition);
		campaignGameStarter.AddPlayerLine("arena_master_ask_for_practice_fight_fight", "arena_master_talk", "arena_master_enter_practice_fight", "{=arena_master_26}I'd like to participate in a practice fight...", null, null, 100, conversation_town_arena_fight_join_check_on_condition);
		campaignGameStarter.AddPlayerLine("arena_master_ask_tournaments", "arena_master_talk", "arena_master_ask_tournaments", "{=arena_master_27}Are there any tournaments going on in nearby towns?", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_remind_something", "arena_master_talk", "arena_master_reminder", "{=iSNrQKEN}I want to go back to something you'd mentioned earlier...", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_leave", "arena_master_talk", "close_window", "{=arena_master_30}I need to leave now. Good bye.", null, null, 80);
		campaignGameStarter.AddDialogLine("arena_master_tournament_location", "arena_master_ask_tournaments", "arena_master_talk", "{=arena_master_31}{NEARBY_TOURNAMENT_STRING}", conversation_tournament_soon_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_master_ask_tournaments_2", "arena_master_ask_tournaments", "arena_master_talk", "{=arena_master_32}There won't be any tournaments any time soon.", null, null, 1);
		campaignGameStarter.AddDialogLine("arena_master_enter_practice_fight_master_confirm", "arena_master_enter_practice_fight", "arena_master_enter_practice_fight_confirm", "{=arena_master_33}Go to it! Grab a practice weapon on your way down.[if:convo_approving]", conversation_arena_join_practice_fight_confirm_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_master_enter_practice_fight_master_decline", "arena_master_enter_practice_fight", "close_window", "{=FguHzavX}You can't practice in the arena because there is a tournament going on right now.", null, null);
		campaignGameStarter.AddDialogLine("arena_master_enter_tournament", "arena_master_enter_tournament", "arena_master_enter_tournament_confirm", "{=arena_master_34}Very well - we'll enter your name in the lots, and when your turn comes up, be ready to go out there and start swinging![if:convo_merry]", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_enter_practice_fight_confirm", "arena_master_enter_practice_fight_confirm", "close_window", "{=arena_master_35}I'll do that.", null, conversation_arena_join_fight_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_enter_practice_fight_decline", "arena_master_enter_practice_fight_confirm", "arena_master_pre_talk", "{=arena_master_36}On second thought, I'll hold off.", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_enter_tournament_confirm", "arena_master_enter_tournament_confirm", "close_window", "{=arena_master_37}I'll be ready.", null, conversation_arena_join_tournament_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_enter_tournament_decline", "arena_master_enter_tournament_confirm", "arena_master_pre_talk", "{=arena_master_38}Actually, never mind.", null, null);
		campaignGameStarter.AddPlayerLine("arena_join_fight", "arena_master_post_practice_fight_talk", "close_window", "{=GmIluR4H}Sure. Why not?", null, conversation_arena_join_fight_on_consequence);
		campaignGameStarter.AddPlayerLine("2593", "arena_master_post_practice_fight_talk", "arena_master_practice_fight_reject", "{=qsg7pZOs}Thanks. But I will give my bruises some time to heal.", null, null);
		campaignGameStarter.AddDialogLine("2594", "arena_master_practice_fight_reject", "close_window", "{=Q7B68CVK}{?PLAYER.GENDER}Splendid{?}Good man{\\?}! That's clever of you.[ib:normal]", null, null);
	}

	private static LocationCharacter CreateTournamentMaster(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject tournamentMaster = culture.TournamentMaster;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(tournamentMaster.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(tournamentMaster, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(tournamentMaster)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, "spawnpoint_tournamentmaster", fixedLocation: true, relation, null, useCivilianEquipment: true, isFixedCharacter: true);
	}

	private bool conversation_arena_master_practice_fights_meet_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && !_knowTournaments)
		{
			MBTextManager.SetTextVariable("TOWN_NAME", Settlement.CurrentSettlement.Name);
			_knowTournaments = true;
			_arenaMasterHasMetInSettlements.Add(Settlement.CurrentSettlement);
			return true;
		}
		return false;
	}

	private bool conversation_town_has_tournament_on_condition()
	{
		if (Settlement.CurrentSettlement.IsTown)
		{
			return Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town) != null;
		}
		return false;
	}

	public static bool conversation_tournament_soon_on_condition()
	{
		List<Town> source = Town.AllTowns.Where((Town x) => Campaign.Current.TournamentManager.GetTournamentGame(x) != null && x != Settlement.CurrentSettlement.Town).ToList();
		source = source.OrderBy((Town x) => DistanceHelper.FindClosestDistanceFromSettlementToSettlement(Settlement.CurrentSettlement, x.Settlement, MobileParty.NavigationType.All)).ToList();
		TextObject textObject = null;
		if (source.Count > 1)
		{
			textObject = new TextObject("{=pinSMuMe}Well, there's one starting up at {CLOSEST_TOURNAMENT}, then another at {NEXT_CLOSEST_TOURNAMENT}. You should probably be able to get to either of those, if you move quickly.[ib:hip]");
			MBTextManager.SetTextVariable("CLOSEST_TOURNAMENT", source[0].Settlement.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("NEXT_CLOSEST_TOURNAMENT", source[1].Settlement.EncyclopediaLinkWithName);
		}
		else if (source.Count == 1)
		{
			MBTextManager.SetTextVariable("CLOSEST_TOURNAMENT", source[0].Settlement.EncyclopediaLinkWithName);
			textObject = new TextObject("{=2WnruiBw}I know of one starting up at {CLOSEST_TOURNAMENT}. You should be able to get there if you move quickly enough.");
		}
		else
		{
			textObject = new TextObject("{=tGI135jv}Ah - I don't know of any right now. That's a bit unusual though. Must be the wars.[ib:closed]");
		}
		MBTextManager.SetTextVariable("NEARBY_TOURNAMENT_STRING", textObject);
		return true;
	}

	private bool conversation_arena_join_practice_fight_confirm_on_condition()
	{
		return !Settlement.CurrentSettlement.Town.HasTournament;
	}

	private bool conversation_arena_join_practice_fight_decline_on_condition()
	{
		return Settlement.CurrentSettlement.Town.HasTournament;
	}

	private bool conversation_town_arena_fight_join_check_on_condition(out TextObject explanation)
	{
		if (Hero.MainHero.IsWounded && Campaign.Current.IsMainHeroDisguised)
		{
			explanation = new TextObject("{=DqZtRBXR}You are wounded and in disguise.");
			return false;
		}
		if (Hero.MainHero.IsWounded)
		{
			explanation = new TextObject("{=yNMrF2QF}You are wounded");
			return false;
		}
		if (Campaign.Current.IsMainHeroDisguised)
		{
			explanation = new TextObject("{=jcEoUPCB}You are in disguise.");
			return false;
		}
		explanation = null;
		return true;
	}

	private bool conversation_arena_master_tournament_meet_on_condition()
	{
		if (Settlement.CurrentSettlement == null)
		{
			return false;
		}
		TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && !_knowTournaments && tournamentGame != null)
		{
			MBTextManager.SetTextVariable("TOWN_NAME", Settlement.CurrentSettlement.Name);
			_knowTournaments = true;
			_arenaMasterHasMetInSettlements.Add(Settlement.CurrentSettlement);
			return true;
		}
		return false;
	}

	private bool conversation_arena_master_no_tournament_meet_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && !_knowTournaments)
		{
			MBTextManager.SetTextVariable("TOWN_NAME", Settlement.CurrentSettlement.Name);
			_knowTournaments = true;
			_arenaMasterHasMetInSettlements.Add(Settlement.CurrentSettlement);
			return true;
		}
		return false;
	}

	private static bool conversation_arena_practice_fight_explain_reward_on_condition()
	{
		MBTextManager.SetTextVariable("OPPONENT_COUNT_1", "3");
		MBTextManager.SetTextVariable("PRIZE_1", "5");
		MBTextManager.SetTextVariable("OPPONENT_COUNT_2", "6");
		MBTextManager.SetTextVariable("PRIZE_2", "10");
		MBTextManager.SetTextVariable("OPPONENT_COUNT_3", "10");
		MBTextManager.SetTextVariable("PRIZE_3", "25");
		MBTextManager.SetTextVariable("OPPONENT_COUNT_4", "20");
		MBTextManager.SetTextVariable("PRIZE_4", "60");
		MBTextManager.SetTextVariable("PRIZE_5", "250");
		MBTextManager.SetTextVariable("ARENA_REWARD", GameTexts.FindText("str_arena_reward"));
		return true;
	}

	private static bool conversation_arena_tournament_explain_reward_on_condition()
	{
		MBTextManager.SetTextVariable("TOURNAMENT_REWARD", new TextObject("{=1esi62Zb}Well - we like tournaments to be memorable. So the sponsors pitch together and buy a prize that they'll be talking about in the markets for weeks. A jeweled blade, say, or a fine-bred warhorse. Something a champion would be proud to own."));
		return true;
	}

	private bool conversation_arena_master_meet_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && _knowTournaments && Settlement.CurrentSettlement.IsTown && !_arenaMasterHasMetInSettlements.Contains(Settlement.CurrentSettlement))
		{
			MBTextManager.SetTextVariable("TOWN_NAME", Settlement.CurrentSettlement.Name);
			_arenaMasterHasMetInSettlements.Add(Settlement.CurrentSettlement);
			return true;
		}
		return false;
	}

	private bool conversation_arena_master_meet_start_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && _knowTournaments && Settlement.CurrentSettlement.IsTown && _arenaMasterHasMetInSettlements.Contains(Settlement.CurrentSettlement))
		{
			return !Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>().AfterPractice;
		}
		return false;
	}

	private bool conversation_arena_master_player_knows_arenas_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && _knowTournaments && Settlement.CurrentSettlement.IsTown && !_arenaMasterHasMetInSettlements.Contains(Settlement.CurrentSettlement))
		{
			return !Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>().AfterPractice;
		}
		return false;
	}

	public static void conversation_arena_join_tournament_on_consequence()
	{
		Mission.Current.EndMission();
		Campaign.Current.GameMenuManager.SetNextMenu("menu_town_tournament_join");
	}

	public static void conversation_arena_join_fight_on_consequence()
	{
		Campaign.Current.ConversationManager.ConversationEndOneShot += StartPlayerPracticeAfterConversationEnd;
	}

	private static void StartPlayerPracticeAfterConversationEnd()
	{
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: false);
		Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>().StartPlayerPractice();
	}

	private bool conversation_arena_master_post_fight_on_condition()
	{
		ArenaPracticeFightMissionController arenaPracticeFightMissionController = Mission.Current?.GetMissionBehavior<ArenaPracticeFightMissionController>();
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && Settlement.CurrentSettlement.IsTown && arenaPracticeFightMissionController != null && arenaPracticeFightMissionController.AfterPractice)
		{
			arenaPracticeFightMissionController.AfterPractice = false;
			int opponentCountBeatenByPlayer = arenaPracticeFightMissionController.OpponentCountBeatenByPlayer;
			int remainingOpponentCountFromLastPractice = arenaPracticeFightMissionController.RemainingOpponentCountFromLastPractice;
			int num = 0;
			int num2;
			if (remainingOpponentCountFromLastPractice == 0)
			{
				num2 = 6;
				num = 250;
			}
			else if (opponentCountBeatenByPlayer == 0)
			{
				num2 = 0;
			}
			else if (opponentCountBeatenByPlayer < 3)
			{
				num2 = 1;
			}
			else if (opponentCountBeatenByPlayer < 6)
			{
				num2 = 2;
				num = 5;
			}
			else if (opponentCountBeatenByPlayer < 10)
			{
				num2 = 3;
				num = 10;
			}
			else if (opponentCountBeatenByPlayer < 20)
			{
				num2 = 4;
				num = 25;
			}
			else
			{
				num2 = 5;
				num = 60;
			}
			MBTextManager.SetTextVariable("PRIZE", num);
			MBTextManager.SetTextVariable("OPPONENT_COUNT", opponentCountBeatenByPlayer);
			TextObject text = GameTexts.FindText("str_arena_take_down", num2.ToString());
			MBTextManager.SetTextVariable("FIGHT_DEBRIEF", text);
			if (num > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, num, disableNotification: true);
				MBTextManager.SetTextVariable("GOLD_AMOUNT", num);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_quest_gold_reward_msg").ToString(), "event:/ui/notification/coins_positive"));
			}
			Mission.Current.SetMissionMode(MissionMode.Conversation, atStart: false);
			return true;
		}
		return false;
	}

	private void AfterMissionStarted(IMission obj)
	{
		if (_enteredPracticeFightFromMenu)
		{
			Mission.Current.SetMissionMode(MissionMode.Battle, atStart: true);
			Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>().StartPlayerPractice();
			_enteredPracticeFightFromMenu = false;
		}
	}

	private void game_menu_enter_practice_fight_on_consequence(MenuCallbackArgs args)
	{
		if (!_arenaMasterHasMetInSettlements.Contains(Settlement.CurrentSettlement))
		{
			_arenaMasterHasMetInSettlements.Add(Settlement.CurrentSettlement);
		}
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("arena"));
		_enteredPracticeFightFromMenu = true;
	}

	private bool game_menu_enter_practice_fight_on_condition(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		args.optionLeaveType = GameMenuOption.LeaveType.PracticeFight;
		if (!_knowTournaments)
		{
			args.Tooltip = new TextObject("{=Sph9Nliz}You need to learn more about the arena by talking with the arena master.");
			args.IsEnabled = false;
			return true;
		}
		if (Hero.MainHero.IsWounded && Campaign.Current.IsMainHeroDisguised)
		{
			args.Tooltip = new TextObject("{=DqZtRBXR}You are wounded and in disguise.");
			args.IsEnabled = false;
			return true;
		}
		if (Hero.MainHero.IsWounded)
		{
			args.Tooltip = new TextObject("{=yNMrF2QF}You are wounded");
			args.IsEnabled = false;
			return true;
		}
		if (Campaign.Current.IsMainHeroDisguised)
		{
			args.Tooltip = new TextObject("{=jcEoUPCB}You are in disguise.");
			args.IsEnabled = false;
			return true;
		}
		if (currentSettlement.Town.HasTournament)
		{
			args.Tooltip = new TextObject("{=NESB0CVc}There is no practice fight because of the Tournament.");
			args.IsEnabled = false;
			return true;
		}
		return true;
	}
}
