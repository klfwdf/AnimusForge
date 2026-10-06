using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.TournamentGames;

public class TournamentCampaignBehavior : CampaignBehaviorBase
{
	private const int TournamentCooldownDurationAsDays = 15;

	private Dictionary<Town, CampaignTime> _lastCreatedTournamentDatesInTowns = new Dictionary<Town, CampaignTime>();

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.TownRebelliosStateChanged.AddNonSerializedListener(this, OnTownRebelliousStateChanged);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		Campaign.Current.TournamentManager.InitializeLeaderboardEntry(Hero.MainHero);
		InitializeTournamentLeaderboard();
		for (int i = 0; i < 3; i++)
		{
			foreach (Town allTown in Town.AllTowns)
			{
				if (allTown.IsTown)
				{
					ConsiderStartOrEndTournament(allTown);
				}
			}
		}
	}

	private void OnDailyTick()
	{
		Hero leaderBoardLeader = Campaign.Current.TournamentManager.GetLeaderBoardLeader();
		if (leaderBoardLeader != null && leaderBoardLeader.IsAlive && leaderBoardLeader.Clan != null)
		{
			leaderBoardLeader.Clan.AddRenown(1f);
		}
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		foreach (Town allTown in Town.AllTowns)
		{
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(allTown);
			if (tournamentGame != null && tournamentGame.Prize != null && (tournamentGame.Prize == DefaultItems.Trash || !tournamentGame.Prize.IsReady))
			{
				tournamentGame.UpdateTournamentPrize(includePlayer: false, removeCurrentPrize: true);
			}
		}
		foreach (KeyValuePair<Town, CampaignTime> item in _lastCreatedTournamentDatesInTowns.ToList())
		{
			if (item.Value.ElapsedDaysUntilNow >= 15f)
			{
				_lastCreatedTournamentDatesInTowns.Remove(item.Key);
			}
		}
	}

	private void OnTownRebelliousStateChanged(Town town, bool rebelliousState)
	{
		if (town.InRebelliousState)
		{
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(town);
			if (tournamentGame != null)
			{
				Campaign.Current.TournamentManager.ResolveTournament(tournamentGame, town);
			}
		}
	}

	private void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		Town town = siegeEvent.BesiegedSettlement.Town;
		if (town != null)
		{
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(town);
			if (tournamentGame != null)
			{
				Campaign.Current.TournamentManager.ResolveTournament(tournamentGame, town);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastCreatedTournamentTimesInTowns", ref _lastCreatedTournamentDatesInTowns);
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		Campaign.Current.TournamentManager.DeleteLeaderboardEntry(victim);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
		AddGameMenus(campaignGameStarter);
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if (settlement.IsTown)
		{
			ConsiderStartOrEndTournament(settlement.Town);
		}
	}

	private void ConsiderStartOrEndTournament(Town town)
	{
		if (_lastCreatedTournamentDatesInTowns.TryGetValue(town, out var value) && !(value.ElapsedDaysUntilNow >= 15f))
		{
			return;
		}
		ITournamentManager tournamentManager = Campaign.Current.TournamentManager;
		TournamentGame tournamentGame = tournamentManager.GetTournamentGame(town);
		if (tournamentGame != null && tournamentGame.CreationTime.ElapsedDaysUntilNow >= (float)tournamentGame.RemoveTournamentAfterDays)
		{
			tournamentManager.ResolveTournament(tournamentGame, town);
		}
		if (tournamentGame == null)
		{
			if (MBRandom.RandomFloat < Campaign.Current.Models.TournamentModel.GetTournamentStartChance(town))
			{
				tournamentManager.AddTournament(Campaign.Current.Models.TournamentModel.CreateTournament(town));
				if (!_lastCreatedTournamentDatesInTowns.ContainsKey(town))
				{
					_lastCreatedTournamentDatesInTowns.Add(town, CampaignTime.Now);
				}
				else
				{
					_lastCreatedTournamentDatesInTowns[town] = CampaignTime.Now;
				}
			}
		}
		else if (tournamentGame.CreationTime.ElapsedDaysUntilNow < (float)tournamentGame.RemoveTournamentAfterDays && MBRandom.RandomFloat < Campaign.Current.Models.TournamentModel.GetTournamentEndChance(tournamentGame))
		{
			tournamentManager.ResolveTournament(tournamentGame, town);
		}
	}

	private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		if (winner.IsHero && winner.HeroObject.Clan != null)
		{
			winner.HeroObject.Clan.AddRenown(Campaign.Current.Models.TournamentModel.GetRenownReward(winner.HeroObject, town));
			GainKingdomInfluenceAction.ApplyForDefault(winner.HeroObject, Campaign.Current.Models.TournamentModel.GetInfluenceReward(winner.HeroObject, town));
		}
	}

	private float GetTournamentSimulationScore(Hero hero)
	{
		return Campaign.Current.Models.TournamentModel.GetTournamentSimulationScore(hero.CharacterObject);
	}

	private void InitializeTournamentLeaderboard()
	{
		Hero[] array = Hero.AllAliveHeroes.Where((Hero x) => x.IsLord && GetTournamentSimulationScore(x) > 1.5f).ToArray();
		int numLeaderboardVictoriesAtGameStart = Campaign.Current.Models.TournamentModel.GetNumLeaderboardVictoriesAtGameStart();
		if (array.Length < 3)
		{
			return;
		}
		List<Hero> list = new List<Hero>();
		for (int num = 0; num < numLeaderboardVictoriesAtGameStart; num++)
		{
			list.Clear();
			for (int num2 = 0; num2 < 16; num2++)
			{
				Hero item = array[MBRandom.RandomInt(array.Length)];
				list.Add(item);
			}
			Hero hero = null;
			float num3 = 0f;
			foreach (Hero item2 in list)
			{
				float num4 = GetTournamentSimulationScore(item2) * (0.8f + 0.2f * MBRandom.RandomFloat);
				if (num4 > num3)
				{
					num3 = num4;
					hero = item2;
				}
			}
			Campaign.Current.TournamentManager.AddLeaderboardEntry(hero);
			hero.Clan.AddRenown(Campaign.Current.Models.TournamentModel.GetRenownReward(hero, null));
		}
	}

	protected void AddDialogs(CampaignGameStarter campaignGameSystemStarter)
	{
	}

	protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddGameMenuOption("town_arena", "join_tournament", "{=LN09ZLXZ}Join the tournament", game_menu_join_tournament_on_condition, delegate
		{
			GameMenu.SwitchToMenu("menu_town_tournament_join");
		}, isLeave: false, 1);
		campaignGameSystemStarter.AddGameMenuOption("town_arena", "mno_tournament_event_watch", "{=6bQIRaIl}Watch the tournament", game_menu_tournament_watch_on_condition, game_menu_tournament_watch_current_game_on_consequence, isLeave: false, 2);
		campaignGameSystemStarter.AddGameMenuOption("town_arena", "mno_see_tournament_leaderboard", "{=vGF5S2hE}Leaderboard", game_menu_town_arena_see_leaderboard_on_condition, null, isLeave: false, 3);
		campaignGameSystemStarter.AddGameMenu("menu_town_tournament_join", "{=5Adr6toM}{MENU_TEXT}", game_menu_tournament_join_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("menu_town_tournament_join", "mno_tournament_event_1", "{=es0Y3Bxc}Join", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			return true;
		}, game_menu_tournament_join_current_game_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("menu_town_tournament_join", "mno_tournament_leave", "{=3sRdGQou}Leave", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate
		{
			GameMenu.SwitchToMenu("town_arena");
		}, isLeave: true);
	}

	[GameMenuEventHandler("town_arena", "mno_see_tournament_leaderboard", GameMenuEventHandler.EventType.OnConsequence)]
	public static void game_menu_ui_town_arena_see_leaderboard_on_consequence(MenuCallbackArgs args)
	{
		args.MenuContext.OpenTournamentLeaderboards();
	}

	private bool game_menu_join_tournament_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.JoinTournament, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static bool game_menu_town_arena_see_leaderboard_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
		if (Settlement.CurrentSettlement != null)
		{
			return Settlement.CurrentSettlement.IsTown;
		}
		return false;
	}

	[GameMenuInitializationHandler("menu_town_tournament_join")]
	private static void game_menu_ui_town_ui_on_init(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		args.MenuContext.SetBackgroundMeshName(currentSettlement.Town.WaitMeshName);
	}

	private void game_menu_tournament_join_on_init(MenuCallbackArgs args)
	{
		TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
		tournamentGame.UpdateTournamentPrize(includePlayer: true);
		GameTexts.SetVariable("MENU_TEXT", tournamentGame.GetMenuText());
	}

	private void game_menu_tournament_join_current_game_on_consequence(MenuCallbackArgs args)
	{
		TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
		GameMenu.SwitchToMenu("town");
		tournamentGame.PrepareForTournamentGame(isPlayerParticipating: true);
		Campaign.Current.TournamentManager.OnPlayerJoinTournament(tournamentGame.GetType(), Settlement.CurrentSettlement);
	}

	private bool game_menu_tournament_watch_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.WatchTournament, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private void game_menu_tournament_watch_current_game_on_consequence(MenuCallbackArgs args)
	{
		TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
		GameMenu.SwitchToMenu("town");
		tournamentGame.PrepareForTournamentGame(isPlayerParticipating: false);
		Campaign.Current.TournamentManager.OnPlayerWatchTournament(tournamentGame.GetType(), Settlement.CurrentSettlement);
	}
}
