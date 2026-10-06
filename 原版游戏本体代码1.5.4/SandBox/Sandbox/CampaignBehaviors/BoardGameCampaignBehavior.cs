using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.BoardGames;
using SandBox.BoardGames.MissionLogics;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class BoardGameCampaignBehavior : CampaignBehaviorBase
{
	private const int NumberOfBoardGamesCanPlayerPlayAgainstHeroPerDay = 3;

	private Dictionary<Hero, List<CampaignTime>> _heroAndBoardGameTimeDictionary = new Dictionary<Hero, List<CampaignTime>>();

	private Dictionary<Settlement, CampaignTime> _wonBoardGamesInOneWeekInSettlement = new Dictionary<Settlement, CampaignTime>();

	private BoardGameHelper.AIDifficulty _difficulty;

	private int _betAmount;

	private bool _influenceGained;

	private bool _renownGained;

	private bool _opposingHeroExtraXPGained;

	private bool _relationGained;

	private bool _gainedNothing;

	private CultureObject _initializedBoardGameCultureInMission;

	public IEnumerable<Settlement> WonBoardGamesInOneWeekInSettlement
	{
		get
		{
			foreach (Settlement key in _wonBoardGamesInOneWeekInSettlement.Keys)
			{
				yield return key;
			}
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnd);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.OnPlayerBoardGameOverEvent.AddNonSerializedListener(this, OnPlayerBoardGameOver);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	private void OnMissionEnd(IMission obj)
	{
		_initializedBoardGameCultureInMission = null;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_heroAndBoardGameTimeDictionary", ref _heroAndBoardGameTimeDictionary);
		dataStore.SyncData("_wonBoardGamesInOneWeekInSettlement", ref _wonBoardGamesInOneWeekInSettlement);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	private void WeeklyTick()
	{
		DeleteOldBoardGamesOfChampion();
		foreach (Hero item in _heroAndBoardGameTimeDictionary.Keys.ToList())
		{
			DeleteOldBoardGamesOfHero(item);
		}
	}

	private void OnPlayerBoardGameOver(Hero opposingHero, BoardGameHelper.BoardGameState state)
	{
		if (opposingHero != null)
		{
			GameEndWithHero(opposingHero);
			if (state == BoardGameHelper.BoardGameState.Win)
			{
				_opposingHeroExtraXPGained = _difficulty != BoardGameHelper.AIDifficulty.Hard && MBRandom.RandomFloat <= 0.5f;
				SkillLevelingManager.OnBoardGameWonAgainstLord(opposingHero, _difficulty, _opposingHeroExtraXPGained);
				float num = 0.1f;
				num += ((opposingHero.IsFemale != Hero.MainHero.IsFemale) ? 0.1f : 0f);
				num += (float)Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 100f;
				num += ((opposingHero.GetTraitLevel(DefaultTraits.Calculating) == 1) ? 0.2f : 0f);
				bool num2 = MBRandom.RandomFloat <= num;
				bool flag = opposingHero.MapFaction == Hero.MainHero.MapFaction && _difficulty == BoardGameHelper.AIDifficulty.Hard && MBRandom.RandomFloat <= 0.4f;
				bool flag2 = _difficulty == BoardGameHelper.AIDifficulty.Hard;
				if (num2)
				{
					ChangeRelationAction.ApplyPlayerRelation(opposingHero, 1);
					_relationGained = true;
				}
				else if (flag)
				{
					GainKingdomInfluenceAction.ApplyForBoardGameWon(opposingHero, 1f);
					_influenceGained = true;
				}
				else if (flag2)
				{
					GainRenownAction.Apply(Hero.MainHero, 1f);
					_renownGained = true;
				}
				else
				{
					_gainedNothing = true;
				}
			}
		}
		else
		{
			switch (state)
			{
			case BoardGameHelper.BoardGameState.Win:
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _betAmount);
				if (_betAmount > 0)
				{
					PlayerWonAgainstTavernChampion();
				}
				break;
			case BoardGameHelper.BoardGameState.Loss:
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, _betAmount);
				break;
			}
		}
		SetBetAmount(0);
	}

	public void InitializeConversationVars()
	{
		if (CampaignMission.Current?.Location?.StringId == "lordshall" || CampaignMission.Current?.Location?.StringId == "tavern")
		{
			CultureObject boardGameCulture = GetBoardGameCulture();
			CultureObject.BoardGameType boardGame = boardGameCulture.BoardGame;
			if (boardGame == CultureObject.BoardGameType.None)
			{
				MBDebug.ShowWarning("Boardgame not yet implemented, or not found.");
			}
			if (boardGame != CultureObject.BoardGameType.None)
			{
				MBTextManager.SetTextVariable("GAME_NAME", GameTexts.FindText("str_boardgame_name", boardGame.ToString()));
				MBTextManager.SetTextVariable("CULTURE_NAME", boardGameCulture.Name);
				MBTextManager.SetTextVariable("DIFFICULTY", GameTexts.FindText("str_boardgame_difficulty", _difficulty.ToString()));
				MBTextManager.SetTextVariable("BET_AMOUNT", _betAmount.ToString());
				MBTextManager.SetTextVariable("IS_BETTING", (_betAmount > 0) ? 1 : 0);
				Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().SetBoardGame(boardGame);
			}
		}
	}

	public void OnMissionStarted(IMission mission)
	{
		Mission mission2 = (Mission)mission;
		if (Mission.Current.Scene != null)
		{
			_ = Mission.Current.Scene.FindEntityWithTag("boardgame") != null;
		}
		if (Mission.Current.Scene != null && Mission.Current.Scene.FindEntityWithTag("boardgame_holder") != null && CampaignMission.Current.Location != null && (CampaignMission.Current.Location.StringId == "lordshall" || CampaignMission.Current.Location.StringId == "tavern"))
		{
			mission2.AddMissionBehavior(new MissionBoardGameLogic());
			InitializeBoardGamePrefabInMission();
		}
	}

	private CultureObject GetBoardGameCulture()
	{
		if (_initializedBoardGameCultureInMission != null)
		{
			return _initializedBoardGameCultureInMission;
		}
		if (CampaignMission.Current.Location.StringId == "lordshall")
		{
			return Settlement.CurrentSettlement.OwnerClan.Culture;
		}
		return Settlement.CurrentSettlement.Culture;
	}

	private void InitializeBoardGamePrefabInMission()
	{
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			CultureObject boardGameCulture = GetBoardGameCulture();
			CultureObject.BoardGameType boardGame = boardGameCulture.BoardGame;
			GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("boardgame_holder");
			MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
			Mission.Current.Scene.RemoveEntity(gameEntity, 92);
			GameEntity gameEntity2 = GameEntity.Instantiate(Mission.Current.Scene, "BoardGame" + boardGame.ToString() + "_FullSetup", callScriptCallbacks: true);
			gameEntity2.SetGlobalFrame(globalFrame.TransformToParent(gameEntity2.GetFrame()));
			GameEntity firstChildEntityWithTag = gameEntity2.GetFirstChildEntityWithTag("dice_board");
			if (firstChildEntityWithTag != null && firstChildEntityWithTag.HasScriptOfType<VertexAnimator>())
			{
				firstChildEntityWithTag.GetFirstScriptOfType<VertexAnimator>().StopAndGoToEnd();
			}
			_initializedBoardGameCultureInMission = boardGameCulture;
		}
	}

	public void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (_heroAndBoardGameTimeDictionary.ContainsKey(victim))
		{
			_heroAndBoardGameTimeDictionary.Remove(victim);
		}
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (settlement.IsTown && CampaignMission.Current != null)
		{
			Location location = CampaignMission.Current.Location;
			if (location != null && location.StringId == "tavern" && unusedUsablePointCount.TryGetValue("spawnpoint_tavernkeeper", out var value) && value > 0)
			{
				location.AddLocationCharacters(CreateGameHost, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
		}
	}

	private static LocationCharacter CreateGameHost(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject tavernGamehost = culture.TavernGamehost;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(tavernGamehost.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(tavernGamehost, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(tavernGamehost)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors, "gambler_npc", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_villager"), useCivilianEquipment: true);
	}

	protected void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("talk_common_to_taverngamehost", "start", "close_window", "{GAME_MASTER_INTRO}", () => conversation_talk_common_to_taverngamehost_on_condition() && !taverngamehost_player_sitting_now_on_condition(), null);
		campaignGameStarter.AddDialogLine("talk_common_to_taverngamehost_2", "start", "taverngamehost_talk", "{=LGrzKlET}Let me know how much of a challenge you can stand and we'll get started. I'm ready to offer you a {DIFFICULTY} challenge and {?IS_BETTING}a bet of {BET_AMOUNT}{GOLD_ICON}.{?}friendly game.{\\?}", () => conversation_talk_common_to_taverngamehost_on_condition() && taverngamehost_player_sitting_now_on_condition(), null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_game", "taverngamehost_talk", "taverngamehost_think_play", "{=BdpW8gUM}That looks good, let's play!", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_change_difficulty", "taverngamehost_talk", "taverngamehost_change_difficulty", "{=MbwG7Gy8}Can I change the difficulty?", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_change_bet", "taverngamehost_talk", "taverngamehost_change_bet", "{=PbDK3PIi}Can I change the amount we're betting?", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_game_history", "taverngamehost_talk", "taverngamehost_learn_history", "{=YM7etEzu}What exactly is {GAME_NAME}?", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_reject", "taverngamehost_talk", "close_window", "{=N7BFbQmT}I'm not interested.", null, null);
		campaignGameStarter.AddDialogLine("taverngamehost_start_playing_ask_accept", "taverngamehost_think_play", "taverngamehost_start_play", "{=GrHJYz7O}Very well. Now, what side do you want?", taverngame_host_play_game_on_condition, null);
		campaignGameStarter.AddDialogLine("taverngamehost_start_playing_ask_decline", "taverngamehost_think_play", "taverngamehost_talk", "{=bTnmpqU4}I'm afraid I don't have time for another game.", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_first", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=7tuyySmq}I'll start.", conversation_taverngamehost_talk_is_seega_on_condition, conversation_taverngamehost_set_player_one_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_last", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=J9fJlz2Y}You can start.", conversation_taverngamehost_talk_is_seega_on_condition, conversation_taverngamehost_set_player_two_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_first_2", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=HdT5YyAb}I'll be white.", conversation_taverngamehost_talk_is_puluc_on_condition, conversation_taverngamehost_set_player_one_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_last_2", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=i8HysulS}I'll be black.", conversation_taverngamehost_talk_is_puluc_on_condition, conversation_taverngamehost_set_player_two_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_first_3", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=HdT5YyAb}I'll be white.", conversation_taverngamehost_talk_is_konane_on_condition, conversation_taverngamehost_set_player_one_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_last_3", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=i8HysulS}I'll be black.", conversation_taverngamehost_talk_is_konane_on_condition, conversation_taverngamehost_set_player_two_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_first_4", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=HdT5YyAb}I'll be white.", conversation_taverngamehost_talk_is_mutorere_on_condition, conversation_taverngamehost_set_player_one_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_last_4", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=i8HysulS}I'll be black.", conversation_taverngamehost_talk_is_mutorere_on_condition, conversation_taverngamehost_set_player_two_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_first_5", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=EnOOqaqf}I'll be sheep.", conversation_taverngamehost_talk_is_baghchal_on_condition, conversation_taverngamehost_set_player_one_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_last_5", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=QjtOAyKE}I'll be wolves.", conversation_taverngamehost_talk_is_baghchal_on_condition, conversation_taverngamehost_set_player_two_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_first_6", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=qsavxffL}I'll be attackers.", conversation_taverngamehost_talk_is_tablut_on_condition, conversation_taverngamehost_set_player_one_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_last_6", "taverngamehost_start_play", "taverngamehost_confirm_play", "{=WD7vOalb}I'll be defenders.", conversation_taverngamehost_talk_is_tablut_on_condition, conversation_taverngamehost_set_player_two_starts_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_back", "taverngamehost_start_play", "start", "{=dUSfRYYH}Just a minute..", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_start_playing_now", "taverngamehost_confirm_play", "close_window", "{=aB1EZssb}Great, let's begin!", null, conversation_taverngamehost_play_game_on_consequence);
		campaignGameStarter.AddDialogLine("taverngamehost_ask_difficulty", "taverngamehost_change_difficulty", "taverngamehost_changing_difficulty", "{=9VR0VeNT}Yes, how easy should I make things for you?", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_change_difficulty_easy", "taverngamehost_changing_difficulty", "start", "{=j9Weia10}Easy", null, conversation_taverngamehost_difficulty_easy_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_change_difficulty_normal", "taverngamehost_changing_difficulty", "start", "{=8UBfIenN}Normal", null, conversation_taverngamehost_difficulty_normal_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_change_difficulty_hard", "taverngamehost_changing_difficulty", "start", "{=OnaJowBF}Hard. Don't hold back or you'll regret it.", null, conversation_taverngamehost_difficulty_hard_on_consequence);
		campaignGameStarter.AddDialogLine("taverngamehost_ask_betting", "taverngamehost_change_bet", "taverngamehost_changing_bet", "{=T5jd4m69}That will only make this more fun. How much were you thinking?", conversation_taverngamehost_talk_place_bet_on_condition, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_100_denars", "taverngamehost_changing_bet", "start", "{=T29epQk3}100{GOLD_ICON}", conversation_taverngamehost_can_bet_100_denars_on_condition, conversation_taverngamehost_bet_100_denars_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_200_denars", "taverngamehost_changing_bet", "start", "{=mHm5SLhb}200{GOLD_ICON}", conversation_taverngamehost_can_bet_200_denars_on_condition, conversation_taverngamehost_bet_200_denars_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_300_denars", "taverngamehost_changing_bet", "start", "{=LnbzQIz6}300{GOLD_ICON}", conversation_taverngamehost_can_bet_300_denars_on_condition, conversation_taverngamehost_bet_300_denars_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_400_denars", "taverngamehost_changing_bet", "start", "{=ck36TZFP}400{GOLD_ICON}", conversation_taverngamehost_can_bet_400_denars_on_condition, conversation_taverngamehost_bet_400_denars_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_500_denars", "taverngamehost_changing_bet", "start", "{=YHTTPKMb}500{GOLD_ICON}", conversation_taverngamehost_can_bet_500_denars_on_condition, conversation_taverngamehost_bet_500_denars_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_0_denars", "taverngamehost_changing_bet", "start", "{=lVx35dWp}On second thought, let's keep this match friendly.", null, conversation_taverngamehost_bet_0_denars_on_consequence);
		campaignGameStarter.AddDialogLine("taverngamehost_deny_betting", "taverngamehost_change_bet", "taverngamehost_changing_difficulty_for_bet", "{=4xtBNkjN}Unfortunately, I only allow betting when I'm playing at my best. You'll have to up the difficulty.", conversation_taverngamehost_talk_not_place_bet_on_condition, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_changing_difficulty_for_bet_yes", "taverngamehost_changing_difficulty_for_bet", "taverngamehost_change_bet_2", "{=i4xzuOJE}Sure, I'll play at the hardest level.", null, conversation_taverngamehost_difficulty_hard_on_consequence);
		campaignGameStarter.AddPlayerLine("taverngamehost_changing_difficulty_for_bet_no", "taverngamehost_changing_difficulty_for_bet", "start", "{=2ynnnR4c}I'd prefer to keep the difficulty where it's at.", null, null);
		campaignGameStarter.AddDialogLine("taverngamehost_ask_betting_2", "taverngamehost_change_bet_2", "taverngamehost_changing_bet", "{=GfHssUYV}Now, feel free to place a bet.", conversation_taverngamehost_talk_place_bet_on_condition, null);
		campaignGameStarter.AddDialogLine("taverngamehost_tell_history_seega", "taverngamehost_learn_history", "taverngamehost_after_history", "{=9PUvbZzD}{GAME_NAME} is a traditional game within the {CULTURE_NAME}. It is a game of calm strategy. You start by placing your pieces on the board, crafting a trap for your enemy to fall into. Then you battle across the board, capturing and eliminating your opponent.", conversation_taverngamehost_talk_is_seega_on_condition, null);
		campaignGameStarter.AddDialogLine("taverngamehost_tell_history_puluc", "taverngamehost_learn_history", "taverngamehost_after_history", "{=sVcJTu7K}{GAME_NAME} is fast and harsh, as warfare should be. Capture as much as possible to keep your opponent weakened and demoralized. But behind this endless offense, there should always be a strong defense to punish any attempt from your opponent to regain control.", conversation_taverngamehost_talk_is_puluc_on_condition, null);
		campaignGameStarter.AddDialogLine("taverngamehost_tell_history_mutorere", "taverngamehost_learn_history", "taverngamehost_after_history", "{=SV0IEWD2}{GAME_NAME} is a game of anticipation. With no possibility of capturing, all your effort should be on reading your opponent and planning further ahead than him.", conversation_taverngamehost_talk_is_mutorere_on_condition, null);
		campaignGameStarter.AddDialogLine("taverngamehost_tell_history_konane", "taverngamehost_learn_history", "taverngamehost_after_history", "{=tVb0nWxm}War is all about sacrifice. In {GAME_NAME} you must make sure that your opponent sacrifices more than you do. Every move can expose you or your opponent and must be carefully considered.", conversation_taverngamehost_talk_is_konane_on_condition, null);
		campaignGameStarter.AddDialogLine("taverngamehost_tell_history_baghchal", "taverngamehost_learn_history", "taverngamehost_after_history", "{=mo4rbYvm}A couple of powerful wolves against a flock of helpless sheep. {GAME_NAME} is a game of uneven odds and seemingly all-powerful adversaries. But through strategy and sacrifice, even the sheep can dominate the wolves.", conversation_taverngamehost_talk_is_baghchal_on_condition, null);
		campaignGameStarter.AddDialogLine("taverngamehost_tell_history_tablut", "taverngamehost_learn_history", "taverngamehost_after_history", "{=nMzfnOFG}{GAME_NAME} is a game of incredibly uneven odds. A weakened and trapped king must try to escape from a horde of attackers who assault from every direction. Ironic how we, the once all-powerful {CULTURE_NAME}, have now fallen in the same position.", conversation_taverngamehost_talk_is_tablut_on_condition, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_history_back", "taverngamehost_after_history", "start", "{=QP7L2YLG}Sounds fun.", null, null);
		campaignGameStarter.AddPlayerLine("taverngamehost_player_history_leave", "taverngamehost_after_history", "close_window", "{=Ng6Rrlr6}I'd rather do something else", null, null);
		campaignGameStarter.AddPlayerLine("lord_player_play_game", "hero_main_options", "lord_answer_to_play_boardgame", "{=3hv4P5OO}Would you care to pass the time with a game of {GAME_NAME}?", conversation_lord_talk_game_on_condition, null, 2);
		campaignGameStarter.AddPlayerLine("lord_player_cancel_boardgame", "hero_main_options", "lord_answer_to_cancel_play_boardgame", "{=ySk7bD8P}Actually, I have other things to do. Maybe later.", conversation_lord_talk_cancel_game_on_condition, null, 2);
		campaignGameStarter.AddDialogLine("lord_agrees_cancel_play", "lord_answer_to_cancel_play_boardgame", "close_window", "{=dzXaXKaC}Very well.", null, conversation_lord_talk_cancel_game_on_consequence);
		campaignGameStarter.AddPlayerLine("lord_player_ask_to_play_boardgame_again", "hero_main_options", "lord_answer_to_play_again_boardgame", "{=U342eACh}Would you like to play another round of {GAME_NAME}?", conversation_lord_talk_game_again_on_condition, null, 2);
		campaignGameStarter.AddDialogLine("lord_answer_to_play_boardgame_again_accept", "lord_answer_to_play_again_boardgame", "close_window", "{=aD1BoB3c}Yes. Let's have another round.", conversation_lord_play_game_on_condition, conversation_lord_play_game_again_on_consequence);
		campaignGameStarter.AddDialogLine("lord_answer_to_play_boardgame_again_decline", "lord_answer_to_play_again_boardgame", "hero_main_options", "{=fqKVojaV}No, not now.", null, conversation_lord_dont_play_game_again_on_consequence);
		campaignGameStarter.AddDialogLine("lord_after_player_win_boardgame", "start", "close_window", "{=!}{PLAYER_GAME_WON_LORD_STRING}", lord_after_player_win_boardgame_condition, null);
		campaignGameStarter.AddDialogLine("lord_after_lord_win_boardgame", "start", "hero_main_options", "{=dC6YhgPP}Ah. A good match, that.", lord_after_lord_win_boardgame_condition, null);
		campaignGameStarter.AddDialogLine("lord_agrees_play", "lord_answer_to_play_boardgame", "lord_setup_game", "{=!}{GAME_AGREEMENT_STRING}", conversation_lord_play_game_on_condition, conversation_lord_detect_difficulty_consequence);
		campaignGameStarter.AddPlayerLine("lord_player_start_game", "lord_setup_game", "close_window", "{=bAy9PdrF}Let's begin, then.", null, conversation_lord_play_game_on_consequence);
		campaignGameStarter.AddPlayerLine("lord_player_leave", "lord_setup_game", "close_window", "{=OQgBim7l}Actually, I have other things to do.", null, null);
		campaignGameStarter.AddDialogLine("lord_refuses_play", "lord_answer_to_play_boardgame", "close_window", "{=!}{LORD_REJECT_GAME_STRING}", conversation_lord_reject_game_condition, null);
	}

	private bool conversation_lord_reject_game_condition()
	{
		TextObject text = ((Hero.OneToOneConversationHero.GetRelationWithPlayer() > -20f) ? new TextObject("{=aRDcoLX0}Now is not a good time, {PLAYER.NAME}. ") : new TextObject("{=GLRrAj61}I do not wish to play games with the likes of you."));
		MBTextManager.SetTextVariable("LORD_REJECT_GAME_STRING", text);
		return true;
	}

	private bool conversation_talk_common_to_taverngamehost_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.TavernGameHost)
		{
			return false;
		}
		InitializeConversationVars();
		MBTextManager.SetTextVariable("GAME_MASTER_INTRO", "{=HDhLMbt7}Greetings, traveler. Do you play {GAME_NAME}? I am reckoned a master of this game, the traditional pastime of the {CULTURE_NAME}. If you are interested in playing, take a seat and we'll start.");
		if (Settlement.CurrentSettlement.OwnerClan == Hero.MainHero.Clan || Settlement.CurrentSettlement.MapFaction.Leader == Hero.MainHero)
		{
			MBTextManager.SetTextVariable("GAME_MASTER_INTRO", "{=yN4imaGo}Your {?PLAYER.GENDER}ladyship{?}lordship{\\?}... This is quite the honor. Do you play {GAME_NAME}? It's the traditional pastime of the {CULTURE_NAME}, and I am reckoned a master. If you wish to play a game, please, take a seat and we'll start.");
		}
		return true;
	}

	private void conversation_taverngamehost_bet_0_denars_on_consequence()
	{
		SetBetAmount(0);
	}

	private static bool conversation_taverngamehost_can_bet_100_denars_on_condition()
	{
		CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
		CharacterObject characterObject = (CharacterObject)Agent.Main.Character;
		bool num = !oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Gold >= 100;
		bool flag = characterObject.HeroObject.Gold >= 100;
		return num && flag;
	}

	private void conversation_taverngamehost_bet_100_denars_on_consequence()
	{
		SetBetAmount(100);
	}

	private static bool conversation_taverngamehost_can_bet_200_denars_on_condition()
	{
		CharacterObject characterObject = (CharacterObject)ConversationMission.OneToOneConversationAgent.Character;
		CharacterObject characterObject2 = (CharacterObject)Agent.Main.Character;
		bool num = !characterObject.IsHero || characterObject.HeroObject.Gold >= 200;
		bool flag = characterObject2.HeroObject.Gold >= 200;
		return num && flag;
	}

	private void conversation_taverngamehost_bet_200_denars_on_consequence()
	{
		SetBetAmount(200);
	}

	private static bool conversation_taverngamehost_can_bet_300_denars_on_condition()
	{
		CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
		CharacterObject characterObject = (CharacterObject)Agent.Main.Character;
		bool num = !oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Gold >= 300;
		bool flag = characterObject.HeroObject.Gold >= 300;
		return num && flag;
	}

	private void conversation_taverngamehost_bet_300_denars_on_consequence()
	{
		SetBetAmount(300);
	}

	private static bool conversation_taverngamehost_can_bet_400_denars_on_condition()
	{
		CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
		CharacterObject characterObject = (CharacterObject)Agent.Main.Character;
		bool num = !oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Gold >= 400;
		bool flag = characterObject.HeroObject.Gold >= 400;
		return num && flag;
	}

	private void conversation_taverngamehost_bet_400_denars_on_consequence()
	{
		SetBetAmount(400);
	}

	private static bool conversation_taverngamehost_can_bet_500_denars_on_condition()
	{
		CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
		CharacterObject characterObject = (CharacterObject)Agent.Main.Character;
		bool num = !oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Gold >= 500;
		bool flag = characterObject.HeroObject.Gold >= 500;
		return num && flag;
	}

	private bool taverngame_host_play_game_on_condition()
	{
		if (_betAmount == 0)
		{
			return true;
		}
		DeleteOldBoardGamesOfChampion();
		return !_wonBoardGamesInOneWeekInSettlement.ContainsKey(Settlement.CurrentSettlement);
	}

	private void conversation_taverngamehost_bet_500_denars_on_consequence()
	{
		SetBetAmount(500);
	}

	private void conversation_taverngamehost_difficulty_easy_on_consequence()
	{
		SetDifficulty(BoardGameHelper.AIDifficulty.Easy);
		SetBetAmount(0);
	}

	private void conversation_taverngamehost_difficulty_normal_on_consequence()
	{
		SetDifficulty(BoardGameHelper.AIDifficulty.Normal);
		SetBetAmount(0);
	}

	private void conversation_taverngamehost_difficulty_hard_on_consequence()
	{
		SetDifficulty(BoardGameHelper.AIDifficulty.Hard);
	}

	private static void conversation_lord_play_game_again_on_consequence()
	{
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().DetectOpposingAgent();
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().StartBoardGame();
		};
	}

	private static void conversation_lord_dont_play_game_again_on_consequence()
	{
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().SetGameOver(GameOverEnum.PlayerCanceledTheGame);
	}

	private void conversation_lord_detect_difficulty_consequence()
	{
		int skillValue = ConversationMission.OneToOneConversationCharacter.GetSkillValue(DefaultSkills.Steward);
		if (skillValue >= 0 && skillValue < 50)
		{
			SetDifficulty(BoardGameHelper.AIDifficulty.Easy);
		}
		else if (skillValue >= 50 && skillValue < 100)
		{
			SetDifficulty(BoardGameHelper.AIDifficulty.Normal);
		}
		else if (skillValue >= 100)
		{
			SetDifficulty(BoardGameHelper.AIDifficulty.Hard);
		}
	}

	private static void conversation_taverngamehost_set_player_one_starts_on_consequence()
	{
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().SetStartingPlayer(playerOneStarts: true);
	}

	private static void conversation_taverngamehost_set_player_two_starts_on_consequence()
	{
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().SetStartingPlayer(playerOneStarts: false);
	}

	private static void conversation_taverngamehost_play_game_on_consequence()
	{
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().DetectOpposingAgent();
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().StartBoardGame();
		};
	}

	private bool conversation_taverngamehost_talk_place_bet_on_condition()
	{
		CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
		bool num = !oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Gold >= 100;
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (num)
		{
			return _difficulty == BoardGameHelper.AIDifficulty.Hard;
		}
		return false;
	}

	private bool conversation_taverngamehost_talk_not_place_bet_on_condition()
	{
		CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
		bool num = !oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Gold >= 100;
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (num)
		{
			return _difficulty != BoardGameHelper.AIDifficulty.Hard;
		}
		return false;
	}

	private static bool conversation_taverngamehost_talk_is_seega_on_condition()
	{
		MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBehavior != null)
		{
			return missionBehavior.CurrentBoardGame == CultureObject.BoardGameType.Seega;
		}
		return false;
	}

	private static bool conversation_taverngamehost_talk_is_puluc_on_condition()
	{
		MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBehavior != null)
		{
			return missionBehavior.CurrentBoardGame == CultureObject.BoardGameType.Puluc;
		}
		return false;
	}

	private static bool conversation_taverngamehost_talk_is_mutorere_on_condition()
	{
		MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBehavior != null)
		{
			return missionBehavior.CurrentBoardGame == CultureObject.BoardGameType.MuTorere;
		}
		return false;
	}

	private static bool conversation_taverngamehost_talk_is_konane_on_condition()
	{
		MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBehavior != null)
		{
			return missionBehavior.CurrentBoardGame == CultureObject.BoardGameType.Konane;
		}
		return false;
	}

	private static bool conversation_taverngamehost_talk_is_baghchal_on_condition()
	{
		MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBehavior != null)
		{
			return missionBehavior.CurrentBoardGame == CultureObject.BoardGameType.BaghChal;
		}
		return false;
	}

	private static bool conversation_taverngamehost_talk_is_tablut_on_condition()
	{
		MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBehavior != null)
		{
			return missionBehavior.CurrentBoardGame == CultureObject.BoardGameType.Tablut;
		}
		return false;
	}

	public static bool taverngamehost_player_sitting_now_on_condition()
	{
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("gambler_player");
		if (gameEntity != null)
		{
			Chair chair = gameEntity.CollectScriptComponentsIncludingChildrenRecursive<Chair>().FirstOrDefault();
			if (chair != null && Agent.Main != null)
			{
				return chair.IsAgentFullySitting(Agent.Main);
			}
			return false;
		}
		return false;
	}

	private bool conversation_lord_talk_game_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Lord && CampaignMission.Current?.Location?.StringId == "lordshall" && MissionBoardGameLogic.IsBoardGameAvailable())
		{
			InitializeConversationVars();
			return true;
		}
		return false;
	}

	private static bool conversation_lord_talk_game_again_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Lord && MissionBoardGameLogic.IsThereActiveBoardGameWithHero(Hero.OneToOneConversationHero))
		{
			return Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().IsGameInProgress;
		}
		return false;
	}

	private static bool conversation_lord_talk_cancel_game_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Lord && MissionBoardGameLogic.IsThereActiveBoardGameWithHero(Hero.OneToOneConversationHero))
		{
			if (!Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().IsOpposingAgentMovingToPlayingChair)
			{
				return !Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().IsGameInProgress;
			}
			return true;
		}
		return false;
	}

	private static void conversation_lord_talk_cancel_game_on_consequence()
	{
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().SetGameOver(GameOverEnum.PlayerCanceledTheGame);
		};
	}

	private static bool lord_after_lord_win_boardgame_condition()
	{
		MissionBoardGameLogic missionBoardGameLogic = Mission.Current?.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBoardGameLogic != null && missionBoardGameLogic.BoardGameFinalState != BoardGameHelper.BoardGameState.None)
		{
			return missionBoardGameLogic.BoardGameFinalState != BoardGameHelper.BoardGameState.Win;
		}
		return false;
	}

	private bool lord_after_player_win_boardgame_condition()
	{
		MissionBoardGameLogic missionBoardGameLogic = Mission.Current?.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBoardGameLogic != null && missionBoardGameLogic.BoardGameFinalState == BoardGameHelper.BoardGameState.Win)
		{
			if (_relationGained)
			{
				MBTextManager.SetTextVariable("PLAYER_GAME_WON_LORD_STRING", "{=QTfliM5b}I enjoyed our game. Let's play again later.");
			}
			else if (_influenceGained)
			{
				MBTextManager.SetTextVariable("PLAYER_GAME_WON_LORD_STRING", "{=31oG5njl}You are a sharp thinker. Our kingdom would do well to hear your thoughts on matters of importance.");
			}
			else if (_opposingHeroExtraXPGained)
			{
				MBTextManager.SetTextVariable("PLAYER_GAME_WON_LORD_STRING", "{=nxpyHb77}Well, I am still a novice in this game, but I learned a lot from playing with you.");
			}
			else if (_renownGained)
			{
				MBTextManager.SetTextVariable("PLAYER_GAME_WON_LORD_STRING", "{=k1b5crrx}You are an accomplished player. I will take note of that.");
			}
			else if (_gainedNothing)
			{
				MBTextManager.SetTextVariable("PLAYER_GAME_WON_LORD_STRING", "{=HzabMi4t}That was a fun game. Thank you.");
			}
			return true;
		}
		return false;
	}

	private bool conversation_lord_play_game_on_condition()
	{
		if (CanPlayerPlayBoardGameAgainstHero(Hero.OneToOneConversationHero))
		{
			string tagId = "DrinkingInTavernTag";
			if (MissionConversationLogic.Current.ConversationManager.IsTagApplicable(tagId, Hero.OneToOneConversationHero.CharacterObject))
			{
				MBTextManager.SetTextVariable("GAME_AGREEMENT_STRING", new TextObject("{=LztDzy8W}Why not? I'm not going anywhere right now, and I could use another drink."));
			}
			else if (Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
			{
				MBTextManager.SetTextVariable("GAME_AGREEMENT_STRING", new TextObject("{=2luygc8o}Mm. I suppose. Takes my mind off all these problems I have to deal with."));
			}
			else if (Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaEarnest)
			{
				MBTextManager.SetTextVariable("GAME_AGREEMENT_STRING", new TextObject("{=349mwgWC}Certainly. A good game always keeps the mind active and fresh."));
			}
			else if (Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaIronic)
			{
				MBTextManager.SetTextVariable("GAME_AGREEMENT_STRING", new TextObject("{=rGaaVBBT}Ah. Very well. I don't mind testing your mettle."));
			}
			else if (Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaSoftspoken)
			{
				MBTextManager.SetTextVariable("GAME_AGREEMENT_STRING", new TextObject("{=idPV1Csj}Yes... Why not? I have nothing too urgent right now."));
			}
			return true;
		}
		return false;
	}

	private static void conversation_lord_play_game_on_consequence()
	{
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().DetectOpposingAgent();
	}

	public void PlayerWonAgainstTavernChampion()
	{
		if (!_wonBoardGamesInOneWeekInSettlement.ContainsKey(Settlement.CurrentSettlement))
		{
			_wonBoardGamesInOneWeekInSettlement.Add(Settlement.CurrentSettlement, CampaignTime.Now);
		}
	}

	private void GameEndWithHero(Hero hero)
	{
		if (_heroAndBoardGameTimeDictionary.ContainsKey(hero))
		{
			_heroAndBoardGameTimeDictionary[hero].Add(CampaignTime.Now);
			return;
		}
		_heroAndBoardGameTimeDictionary.Add(hero, new List<CampaignTime>());
		_heroAndBoardGameTimeDictionary[hero].Add(CampaignTime.Now);
	}

	private bool CanPlayerPlayBoardGameAgainstHero(Hero hero)
	{
		if (hero.GetRelationWithPlayer() >= 0f)
		{
			DeleteOldBoardGamesOfHero(hero);
			if (_heroAndBoardGameTimeDictionary.ContainsKey(hero))
			{
				List<CampaignTime> list = _heroAndBoardGameTimeDictionary[hero];
				return 3 > list.Count;
			}
			return true;
		}
		return false;
	}

	private void DeleteOldBoardGamesOfChampion()
	{
		foreach (Settlement item in Settlement.All)
		{
			if (_wonBoardGamesInOneWeekInSettlement.ContainsKey(item) && _wonBoardGamesInOneWeekInSettlement[item].ElapsedWeeksUntilNow >= 1f)
			{
				_wonBoardGamesInOneWeekInSettlement.Remove(item);
			}
		}
	}

	private void DeleteOldBoardGamesOfHero(Hero hero)
	{
		if (!_heroAndBoardGameTimeDictionary.ContainsKey(hero))
		{
			return;
		}
		List<CampaignTime> list = _heroAndBoardGameTimeDictionary[hero];
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num].ElapsedDaysUntilNow > 1f)
			{
				list.RemoveAt(num);
			}
		}
		if (list.IsEmpty())
		{
			_heroAndBoardGameTimeDictionary.Remove(hero);
		}
	}

	public void SetBetAmount(int bet)
	{
		_betAmount = bet;
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().SetBetAmount(bet);
		MBTextManager.SetTextVariable("BET_AMOUNT", bet.ToString());
		MBTextManager.SetTextVariable("IS_BETTING", (bet > 0) ? 1 : 0);
	}

	private void SetDifficulty(BoardGameHelper.AIDifficulty difficulty)
	{
		_difficulty = difficulty;
		Mission.Current.GetMissionBehavior<MissionBoardGameLogic>().SetCurrentDifficulty(difficulty);
		MBTextManager.SetTextVariable("DIFFICULTY", GameTexts.FindText("str_boardgame_difficulty", difficulty.ToString()));
	}
}
