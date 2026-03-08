using System;
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

namespace TaleWorlds.CampaignSystem.TournamentGames
{
	// Token: 0x020002D0 RID: 720
	public class TournamentCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060026F8 RID: 9976 RVA: 0x000A2190 File Offset: 0x000A0390
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.TournamentFinished.AddNonSerializedListener(this, new Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>(this.OnTournamentFinished));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.TownRebelliosStateChanged.AddNonSerializedListener(this, new Action<Town, bool>(this.OnTownRebelliousStateChanged));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
		}

		// Token: 0x060026F9 RID: 9977 RVA: 0x000A226C File Offset: 0x000A046C
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			Campaign.Current.TournamentManager.InitializeLeaderboardEntry(Hero.MainHero, 0);
			this.InitializeTournamentLeaderboard();
			for (int i = 0; i < 3; i++)
			{
				foreach (Town town in Town.AllTowns)
				{
					if (town.IsTown)
					{
						this.ConsiderStartOrEndTournament(town);
					}
				}
			}
		}

		// Token: 0x060026FA RID: 9978 RVA: 0x000A22F0 File Offset: 0x000A04F0
		private void OnDailyTick()
		{
			Hero leaderBoardLeader = Campaign.Current.TournamentManager.GetLeaderBoardLeader();
			if (leaderBoardLeader != null && leaderBoardLeader.IsAlive && leaderBoardLeader.Clan != null)
			{
				leaderBoardLeader.Clan.AddRenown(1f, true);
			}
		}

		// Token: 0x060026FB RID: 9979 RVA: 0x000A2334 File Offset: 0x000A0534
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			foreach (Town town in Town.AllTowns)
			{
				TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(town);
				if (tournamentGame != null && tournamentGame.Prize != null && (tournamentGame.Prize == DefaultItems.Trash || !tournamentGame.Prize.IsReady))
				{
					tournamentGame.UpdateTournamentPrize(false, true);
				}
			}
			foreach (KeyValuePair<Town, CampaignTime> keyValuePair in this._lastCreatedTournamentDatesInTowns.ToList<KeyValuePair<Town, CampaignTime>>())
			{
				if (keyValuePair.Value.ElapsedDaysUntilNow >= 15f)
				{
					this._lastCreatedTournamentDatesInTowns.Remove(keyValuePair.Key);
				}
			}
		}

		// Token: 0x060026FC RID: 9980 RVA: 0x000A2428 File Offset: 0x000A0628
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

		// Token: 0x060026FD RID: 9981 RVA: 0x000A2464 File Offset: 0x000A0664
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

		// Token: 0x060026FE RID: 9982 RVA: 0x000A24A5 File Offset: 0x000A06A5
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Town, CampaignTime>>("_lastCreatedTournamentTimesInTowns", ref this._lastCreatedTournamentDatesInTowns);
		}

		// Token: 0x060026FF RID: 9983 RVA: 0x000A24B9 File Offset: 0x000A06B9
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			Campaign.Current.TournamentManager.DeleteLeaderboardEntry(victim);
		}

		// Token: 0x06002700 RID: 9984 RVA: 0x000A24CB File Offset: 0x000A06CB
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
			this.AddGameMenus(campaignGameStarter);
		}

		// Token: 0x06002701 RID: 9985 RVA: 0x000A24DB File Offset: 0x000A06DB
		private void DailyTickSettlement(Settlement settlement)
		{
			if (settlement.IsTown)
			{
				this.ConsiderStartOrEndTournament(settlement.Town);
			}
		}

		// Token: 0x06002702 RID: 9986 RVA: 0x000A24F4 File Offset: 0x000A06F4
		private void ConsiderStartOrEndTournament(Town town)
		{
			CampaignTime campaignTime;
			if (!this._lastCreatedTournamentDatesInTowns.TryGetValue(town, out campaignTime) || campaignTime.ElapsedDaysUntilNow >= 15f)
			{
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
						if (!this._lastCreatedTournamentDatesInTowns.ContainsKey(town))
						{
							this._lastCreatedTournamentDatesInTowns.Add(town, CampaignTime.Now);
							return;
						}
						this._lastCreatedTournamentDatesInTowns[town] = CampaignTime.Now;
						return;
					}
				}
				else if (tournamentGame.CreationTime.ElapsedDaysUntilNow < (float)tournamentGame.RemoveTournamentAfterDays && MBRandom.RandomFloat < Campaign.Current.Models.TournamentModel.GetTournamentEndChance(tournamentGame))
				{
					tournamentManager.ResolveTournament(tournamentGame, town);
				}
			}
		}

		// Token: 0x06002703 RID: 9987 RVA: 0x000A2604 File Offset: 0x000A0804
		private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
		{
			if (winner.IsHero && winner.HeroObject.Clan != null)
			{
				winner.HeroObject.Clan.AddRenown((float)Campaign.Current.Models.TournamentModel.GetRenownReward(winner.HeroObject, town), true);
				GainKingdomInfluenceAction.ApplyForDefault(winner.HeroObject, (float)Campaign.Current.Models.TournamentModel.GetInfluenceReward(winner.HeroObject, town));
			}
		}

		// Token: 0x06002704 RID: 9988 RVA: 0x000A267A File Offset: 0x000A087A
		private float GetTournamentSimulationScore(Hero hero)
		{
			return Campaign.Current.Models.TournamentModel.GetTournamentSimulationScore(hero.CharacterObject);
		}

		// Token: 0x06002705 RID: 9989 RVA: 0x000A2698 File Offset: 0x000A0898
		private void InitializeTournamentLeaderboard()
		{
			Hero[] array = (from x in Hero.AllAliveHeroes
				where x.IsLord && this.GetTournamentSimulationScore(x) > 1.5f
				select x).ToArray<Hero>();
			int numLeaderboardVictoriesAtGameStart = Campaign.Current.Models.TournamentModel.GetNumLeaderboardVictoriesAtGameStart();
			if (array.Length < 3)
			{
				return;
			}
			List<Hero> list = new List<Hero>();
			for (int i = 0; i < numLeaderboardVictoriesAtGameStart; i++)
			{
				list.Clear();
				for (int j = 0; j < 16; j++)
				{
					Hero item = array[MBRandom.RandomInt(array.Length)];
					list.Add(item);
				}
				Hero hero = null;
				float num = 0f;
				foreach (Hero hero2 in list)
				{
					float num2 = this.GetTournamentSimulationScore(hero2) * (0.8f + 0.2f * MBRandom.RandomFloat);
					if (num2 > num)
					{
						num = num2;
						hero = hero2;
					}
				}
				Campaign.Current.TournamentManager.AddLeaderboardEntry(hero);
				hero.Clan.AddRenown((float)Campaign.Current.Models.TournamentModel.GetRenownReward(hero, null), true);
			}
		}

		// Token: 0x06002706 RID: 9990 RVA: 0x000A27C8 File Offset: 0x000A09C8
		protected void AddDialogs(CampaignGameStarter campaignGameSystemStarter)
		{
		}

		// Token: 0x06002707 RID: 9991 RVA: 0x000A27CC File Offset: 0x000A09CC
		protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
		{
			campaignGameSystemStarter.AddGameMenuOption("town_arena", "join_tournament", "{=LN09ZLXZ}Join the tournament", new GameMenuOption.OnConditionDelegate(this.game_menu_join_tournament_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("menu_town_tournament_join");
			}, false, 1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_arena", "mno_tournament_event_watch", "{=6bQIRaIl}Watch the tournament", new GameMenuOption.OnConditionDelegate(this.game_menu_tournament_watch_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_tournament_watch_current_game_on_consequence), false, 2, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_arena", "mno_see_tournament_leaderboard", "{=vGF5S2hE}Leaderboard", new GameMenuOption.OnConditionDelegate(TournamentCampaignBehavior.game_menu_town_arena_see_leaderboard_on_condition), null, false, 3, false, null);
			campaignGameSystemStarter.AddGameMenu("menu_town_tournament_join", "{=5Adr6toM}{MENU_TEXT}", new OnInitDelegate(this.game_menu_tournament_join_on_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_town_tournament_join", "mno_tournament_event_1", "{=es0Y3Bxc}Join", delegate(MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Mission;
				return true;
			}, new GameMenuOption.OnConsequenceDelegate(this.game_menu_tournament_join_current_game_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_town_tournament_join", "mno_tournament_leave", "{=3sRdGQou}Leave", delegate(MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Leave;
				return true;
			}, delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("town_arena");
			}, true, -1, false, null);
		}

		// Token: 0x06002708 RID: 9992 RVA: 0x000A292E File Offset: 0x000A0B2E
		[GameMenuEventHandler("town_arena", "mno_see_tournament_leaderboard", GameMenuEventHandler.EventType.OnConsequence)]
		public static void game_menu_ui_town_arena_see_leaderboard_on_consequence(MenuCallbackArgs args)
		{
			args.MenuContext.OpenTournamentLeaderboards();
		}

		// Token: 0x06002709 RID: 9993 RVA: 0x000A293C File Offset: 0x000A0B3C
		private bool game_menu_join_tournament_on_condition(MenuCallbackArgs args)
		{
			bool shouldBeDisabled;
			TextObject disabledText;
			bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.JoinTournament, out shouldBeDisabled, out disabledText);
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
		}

		// Token: 0x0600270A RID: 9994 RVA: 0x000A2979 File Offset: 0x000A0B79
		private static bool game_menu_town_arena_see_leaderboard_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
			return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown;
		}

		// Token: 0x0600270B RID: 9995 RVA: 0x000A2998 File Offset: 0x000A0B98
		[GameMenuInitializationHandler("menu_town_tournament_join")]
		private static void game_menu_ui_town_ui_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			args.MenuContext.SetBackgroundMeshName(currentSettlement.Town.WaitMeshName);
		}

		// Token: 0x0600270C RID: 9996 RVA: 0x000A29C4 File Offset: 0x000A0BC4
		private void game_menu_tournament_join_on_init(MenuCallbackArgs args)
		{
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
			tournamentGame.UpdateTournamentPrize(true, false);
			GameTexts.SetVariable("MENU_TEXT", tournamentGame.GetMenuText());
		}

		// Token: 0x0600270D RID: 9997 RVA: 0x000A2A04 File Offset: 0x000A0C04
		private void game_menu_tournament_join_current_game_on_consequence(MenuCallbackArgs args)
		{
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
			GameMenu.SwitchToMenu("town");
			tournamentGame.PrepareForTournamentGame(true);
			Campaign.Current.TournamentManager.OnPlayerJoinTournament(tournamentGame.GetType(), Settlement.CurrentSettlement);
		}

		// Token: 0x0600270E RID: 9998 RVA: 0x000A2A58 File Offset: 0x000A0C58
		private bool game_menu_tournament_watch_on_condition(MenuCallbackArgs args)
		{
			bool shouldBeDisabled;
			TextObject disabledText;
			bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.WatchTournament, out shouldBeDisabled, out disabledText);
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
		}

		// Token: 0x0600270F RID: 9999 RVA: 0x000A2A94 File Offset: 0x000A0C94
		private void game_menu_tournament_watch_current_game_on_consequence(MenuCallbackArgs args)
		{
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
			GameMenu.SwitchToMenu("town");
			tournamentGame.PrepareForTournamentGame(false);
			Campaign.Current.TournamentManager.OnPlayerWatchTournament(tournamentGame.GetType(), Settlement.CurrentSettlement);
		}

		// Token: 0x04000B60 RID: 2912
		private const int TournamentCooldownDurationAsDays = 15;

		// Token: 0x04000B61 RID: 2913
		private Dictionary<Town, CampaignTime> _lastCreatedTournamentDatesInTowns = new Dictionary<Town, CampaignTime>();
	}
}
