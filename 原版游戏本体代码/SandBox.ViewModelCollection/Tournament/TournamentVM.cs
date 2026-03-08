using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Tournament
{
	// Token: 0x02000011 RID: 17
	public class TournamentVM : ViewModel
	{
		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000121 RID: 289 RVA: 0x00006A0C File Offset: 0x00004C0C
		public Action DisableUI { get; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x06000122 RID: 290 RVA: 0x00006A14 File Offset: 0x00004C14
		public TournamentBehavior Tournament { get; }

		// Token: 0x06000123 RID: 291 RVA: 0x00006A1C File Offset: 0x00004C1C
		public TournamentVM(Action disableUI, TournamentBehavior tournamentBehavior)
		{
			this.DisableUI = disableUI;
			this.CurrentMatch = new TournamentMatchVM();
			this.Round1 = new TournamentRoundVM();
			this.Round2 = new TournamentRoundVM();
			this.Round3 = new TournamentRoundVM();
			this.Round4 = new TournamentRoundVM();
			this._rounds = new List<TournamentRoundVM> { this.Round1, this.Round2, this.Round3, this.Round4 };
			this._tournamentWinner = new TournamentParticipantVM();
			this.Tournament = tournamentBehavior;
			this.WinnerIntro = GameTexts.FindText("str_tournament_winner_intro", null).ToString();
			this.BattleRewards = new MBBindingList<TournamentRewardVM>();
			for (int i = 0; i < this._rounds.Count; i++)
			{
				this._rounds[i].Initialize(this.Tournament.Rounds[i], GameTexts.FindText("str_tournament_round", i.ToString()));
			}
			this.Refresh();
			this.Tournament.TournamentEnd += this.OnTournamentEnd;
			this.PrizeVisual = (this.HasPrizeItem ? new ItemImageIdentifierVM(this.Tournament.TournamentGame.Prize, "") : new ItemImageIdentifierVM(null, ""));
			this.SkipAllRoundsHint = new HintViewModel();
			this.RefreshValues();
		}

		// Token: 0x06000124 RID: 292 RVA: 0x00006B98 File Offset: 0x00004D98
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.LeaveText = GameTexts.FindText("str_tournament_leave", null).ToString();
			this.SkipRoundText = GameTexts.FindText("str_tournament_skip_round", null).ToString();
			this.WatchRoundText = GameTexts.FindText("str_tournament_watch_round", null).ToString();
			this.JoinTournamentText = GameTexts.FindText("str_tournament_join_tournament", null).ToString();
			this.BetText = GameTexts.FindText("str_bet", null).ToString();
			this.AcceptText = GameTexts.FindText("str_accept", null).ToString();
			this.CancelText = GameTexts.FindText("str_cancel", null).ToString();
			this.TournamentWinnerTitle = GameTexts.FindText("str_tournament_winner_title", null).ToString();
			this.BetTitleText = GameTexts.FindText("str_wager", null).ToString();
			GameTexts.SetVariable("MAX_AMOUNT", this.Tournament.GetMaximumBet());
			GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			this.BetDescriptionText = GameTexts.FindText("str_tournament_bet_description", null).ToString();
			this.TournamentPrizeText = GameTexts.FindText("str_tournament_prize", null).ToString();
			this.PrizeItemName = this.Tournament.TournamentGame.Prize.Name.ToString();
			MBTextManager.SetTextVariable("SETTLEMENT_NAME", this.Tournament.Settlement.Name, false);
			this.TournamentTitle = GameTexts.FindText("str_tournament", null).ToString();
			this.CurrentWagerText = GameTexts.FindText("str_tournament_current_wager", null).ToString();
			this.SkipAllRoundsHint.HintText = new TextObject("{=GaOE4bdd}Skip All Rounds", null);
			TournamentRoundVM round = this._round1;
			if (round != null)
			{
				round.RefreshValues();
			}
			TournamentRoundVM round2 = this._round2;
			if (round2 != null)
			{
				round2.RefreshValues();
			}
			TournamentRoundVM round3 = this._round3;
			if (round3 != null)
			{
				round3.RefreshValues();
			}
			TournamentRoundVM round4 = this._round4;
			if (round4 != null)
			{
				round4.RefreshValues();
			}
			TournamentMatchVM currentMatch = this._currentMatch;
			if (currentMatch != null)
			{
				currentMatch.RefreshValues();
			}
			TournamentParticipantVM tournamentWinner = this._tournamentWinner;
			if (tournamentWinner == null)
			{
				return;
			}
			tournamentWinner.RefreshValues();
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00006DA3 File Offset: 0x00004FA3
		public void ExecuteBet()
		{
			this._thisRoundBettedAmount += this.WageredDenars;
			this.Tournament.PlaceABet(this.WageredDenars);
			this.RefreshBetProperties();
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00006DD0 File Offset: 0x00004FD0
		public void ExecuteJoinTournament()
		{
			if (this.PlayerCanJoinMatch())
			{
				this.Tournament.StartMatch();
				this.IsCurrentMatchActive = true;
				this.CurrentMatch.Refresh(true);
				this.CurrentMatch.State = 3;
				this.DisableUI();
				this.IsCurrentMatchActive = true;
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00006E21 File Offset: 0x00005021
		public void ExecuteSkipRound()
		{
			if (this.IsTournamentIncomplete)
			{
				this.Tournament.SkipMatch(false);
			}
			this.Refresh();
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00006E40 File Offset: 0x00005040
		public void ExecuteSkipAllRounds()
		{
			int num = 0;
			int num2 = this.Tournament.Rounds.Sum((TournamentRound r) => r.Matches.Length);
			while (!this.CanPlayerJoin)
			{
				TournamentRound currentRound = this.Tournament.CurrentRound;
				if (((currentRound != null) ? currentRound.CurrentMatch : null) == null || num >= num2)
				{
					break;
				}
				this.ExecuteSkipRound();
				num++;
			}
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00006EB0 File Offset: 0x000050B0
		public void ExecuteWatchRound()
		{
			if (!this.PlayerCanJoinMatch())
			{
				this.Tournament.StartMatch();
				this.IsCurrentMatchActive = true;
				this.CurrentMatch.Refresh(true);
				this.CurrentMatch.State = 3;
				this.DisableUI();
				this.IsCurrentMatchActive = true;
			}
		}

		// Token: 0x0600012A RID: 298 RVA: 0x00006F04 File Offset: 0x00005104
		public void ExecuteLeave()
		{
			if (this.CurrentMatch != null)
			{
				List<TournamentMatch> list = new List<TournamentMatch>();
				for (int i = this.Tournament.CurrentRoundIndex; i < this.Tournament.Rounds.Length; i++)
				{
					list.AddRange(from x in this.Tournament.Rounds[i].Matches
						where x.State != TournamentMatch.MatchState.Finished
						select x);
				}
				if (list.Any((TournamentMatch x) => x.Participants.Any((TournamentParticipant y) => y.Character == CharacterObject.PlayerCharacter)))
				{
					InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_forfeit", null).ToString(), GameTexts.FindText("str_tournament_forfeit_game", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.EndTournamentMission), null, "", 0f, null, null, null), true, false);
					return;
				}
			}
			this.EndTournamentMission();
		}

		// Token: 0x0600012B RID: 299 RVA: 0x00007016 File Offset: 0x00005216
		private void EndTournamentMission()
		{
			this.Tournament.EndTournamentViaLeave();
			Mission.Current.EndMission();
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00007030 File Offset: 0x00005230
		private void RefreshBetProperties()
		{
			TextObject textObject = new TextObject("{=L9GnQvsq}Stake: {BETTED_DENARS}", null);
			textObject.SetTextVariable("BETTED_DENARS", this.Tournament.BettedDenars);
			this.BettedDenarsText = textObject.ToString();
			TextObject textObject2 = new TextObject("{=xzzSaN4b}Expected: {OVERALL_EXPECTED_DENARS}", null);
			textObject2.SetTextVariable("OVERALL_EXPECTED_DENARS", this.Tournament.OverallExpectedDenars);
			this.OverallExpectedDenarsText = textObject2.ToString();
			TextObject textObject3 = new TextObject("{=yF5fpwNE}Total: {TOTAL}", null);
			textObject3.SetTextVariable("TOTAL", this.Tournament.PlayerDenars);
			this.TotalDenarsText = textObject3.ToString();
			base.OnPropertyChanged("IsBetButtonEnabled");
			this.MaximumBetValue = MathF.Min(this.Tournament.GetMaximumBet() - this._thisRoundBettedAmount, Hero.MainHero.Gold);
			GameTexts.SetVariable("NORMALIZED_EXPECTED_GOLD", (int)(this.Tournament.BetOdd * 100f));
			GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			this.BetOddsText = GameTexts.FindText("str_tournament_bet_odd", null).ToString();
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000713D File Offset: 0x0000533D
		private void OnNewRoundStarted(int prevRoundIndex, int currentRoundIndex)
		{
			this._isPlayerParticipating = this.Tournament.IsPlayerParticipating;
			this._thisRoundBettedAmount = 0;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00007158 File Offset: 0x00005358
		public void Refresh()
		{
			this.IsCurrentMatchActive = false;
			this.CurrentMatch = this._rounds[this.Tournament.CurrentRoundIndex].Matches.Find((TournamentMatchVM m) => m.IsValid && m.Match == this.Tournament.CurrentMatch);
			this.ActiveRoundIndex = this.Tournament.CurrentRoundIndex;
			this.CanPlayerJoin = this.PlayerCanJoinMatch();
			base.OnPropertyChanged("IsTournamentIncomplete");
			base.OnPropertyChanged("InitializationOver");
			base.OnPropertyChanged("IsBetButtonEnabled");
			this.HasPrizeItem = this.Tournament.TournamentGame.Prize != null && !this.IsOver;
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00007200 File Offset: 0x00005400
		private void OnTournamentEnd()
		{
			TournamentParticipantVM[] array = this.Round4.Matches.Last((TournamentMatchVM m) => m.IsValid).GetParticipants().ToArray<TournamentParticipantVM>();
			TournamentParticipantVM tournamentParticipantVM = array[0];
			TournamentParticipantVM tournamentParticipantVM2 = array[1];
			this.TournamentWinner = this.Round4.Matches.Last((TournamentMatchVM m) => m.IsValid).GetParticipants().First((TournamentParticipantVM p) => p.Participant == this.Tournament.Winner);
			this.TournamentWinner.Refresh();
			if (this.TournamentWinner.Participant.Character.IsHero)
			{
				Hero heroObject = this.TournamentWinner.Participant.Character.HeroObject;
				this.TournamentWinner.Character.ArmorColor1 = heroObject.MapFaction.Color;
				this.TournamentWinner.Character.ArmorColor2 = heroObject.MapFaction.Color2;
			}
			else
			{
				CultureObject culture = this.TournamentWinner.Participant.Character.Culture;
				this.TournamentWinner.Character.ArmorColor1 = culture.Color;
				this.TournamentWinner.Character.ArmorColor2 = culture.Color2;
			}
			this.IsWinnerHero = this.Tournament.Winner.Character.IsHero;
			if (this.IsWinnerHero)
			{
				this.WinnerBanner = new BannerImageIdentifierVM(this.Tournament.Winner.Character.HeroObject.ClanBanner, true);
			}
			if (this.TournamentWinner.Participant.Character.IsPlayerCharacter)
			{
				TournamentParticipantVM tournamentParticipantVM3 = ((tournamentParticipantVM == this.TournamentWinner) ? tournamentParticipantVM2 : tournamentParticipantVM);
				GameTexts.SetVariable("TOURNAMENT_FINAL_OPPONENT", tournamentParticipantVM3.Name);
				this.WinnerIntro = GameTexts.FindText("str_tournament_result_won", null).ToString();
				if (this.Tournament.TournamentGame.TournamentWinRenown > 0f)
				{
					GameTexts.SetVariable("RENOWN", this.Tournament.TournamentGame.TournamentWinRenown.ToString("F1"));
					this.BattleRewards.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_renown", null).ToString()));
				}
				if (this.Tournament.TournamentGame.TournamentWinInfluence > 0f)
				{
					float tournamentWinInfluence = this.Tournament.TournamentGame.TournamentWinInfluence;
					TextObject textObject = GameTexts.FindText("str_tournament_influence", null);
					textObject.SetTextVariable("INFLUENCE", tournamentWinInfluence.ToString("F1"));
					textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
					this.BattleRewards.Add(new TournamentRewardVM(textObject.ToString()));
				}
				if (this.Tournament.TournamentGame.Prize != null)
				{
					string content = this.Tournament.TournamentGame.Prize.Name.ToString();
					GameTexts.SetVariable("REWARD", content);
					this.BattleRewards.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_reward", null).ToString(), new ItemImageIdentifierVM(this.Tournament.TournamentGame.Prize, "")));
				}
				if (this.Tournament.OverallExpectedDenars > 0)
				{
					int overallExpectedDenars = this.Tournament.OverallExpectedDenars;
					TextObject textObject2 = GameTexts.FindText("str_tournament_bet", null);
					textObject2.SetTextVariable("BET", overallExpectedDenars);
					textObject2.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					this.BattleRewards.Add(new TournamentRewardVM(textObject2.ToString()));
				}
			}
			else if (tournamentParticipantVM.Participant.Character.IsPlayerCharacter || tournamentParticipantVM2.Participant.Character.IsPlayerCharacter)
			{
				TournamentParticipantVM tournamentParticipantVM4 = ((tournamentParticipantVM == this.TournamentWinner) ? tournamentParticipantVM : tournamentParticipantVM2);
				GameTexts.SetVariable("TOURNAMENT_FINAL_OPPONENT", tournamentParticipantVM4.Name);
				this.WinnerIntro = GameTexts.FindText("str_tournament_result_eliminated_at_final", null).ToString();
			}
			else
			{
				int num = 3;
				bool flag = this.Round3.GetParticipants().Any((TournamentParticipantVM p) => p.Participant.Character.IsPlayerCharacter);
				bool flag2 = this.Round2.GetParticipants().Any((TournamentParticipantVM p) => p.Participant.Character.IsPlayerCharacter);
				bool flag3 = this.Round1.GetParticipants().Any((TournamentParticipantVM p) => p.Participant.Character.IsPlayerCharacter);
				if (flag)
				{
					num = 3;
				}
				else if (flag2)
				{
					num = 2;
				}
				else if (flag3)
				{
					num = 1;
				}
				bool flag4 = tournamentParticipantVM == this.TournamentWinner;
				GameTexts.SetVariable("TOURNAMENT_FINAL_PARTICIPANT_A", flag4 ? tournamentParticipantVM.Name : tournamentParticipantVM2.Name);
				GameTexts.SetVariable("TOURNAMENT_FINAL_PARTICIPANT_B", flag4 ? tournamentParticipantVM2.Name : tournamentParticipantVM.Name);
				if (this._isPlayerParticipating)
				{
					GameTexts.SetVariable("TOURNAMENT_ELIMINATED_ROUND", num.ToString());
					this.WinnerIntro = GameTexts.FindText("str_tournament_result_eliminated", null).ToString();
				}
				else
				{
					this.WinnerIntro = GameTexts.FindText("str_tournament_result_spectator", null).ToString();
				}
			}
			this.IsOver = true;
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00007731 File Offset: 0x00005931
		private bool PlayerCanJoinMatch()
		{
			if (this.IsTournamentIncomplete)
			{
				return this.Tournament.CurrentMatch.Participants.Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
			}
			return false;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00007774 File Offset: 0x00005974
		public void OnAgentRemoved(Agent agent)
		{
			if (this.IsCurrentMatchActive && agent.IsHuman)
			{
				TournamentParticipant participant = this.CurrentMatch.Match.GetParticipant(agent.Origin.UniqueSeed);
				if (participant != null)
				{
					this.CurrentMatch.GetParticipants().First((TournamentParticipantVM p) => p.Participant == participant).IsDead = true;
				}
			}
		}

		// Token: 0x06000132 RID: 306 RVA: 0x000077E2 File Offset: 0x000059E2
		public void ExecuteShowPrizeItemTooltip()
		{
			if (this.HasPrizeItem)
			{
				InformationManager.ShowTooltip(typeof(ItemObject), new object[]
				{
					new EquipmentElement(this.Tournament.TournamentGame.Prize, null, null, false)
				});
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00007821 File Offset: 0x00005A21
		public void ExecuteHidePrizeItemTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00007828 File Offset: 0x00005A28
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey == null)
			{
				return;
			}
			cancelInputKey.OnFinalize();
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00007851 File Offset: 0x00005A51
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00007860 File Offset: 0x00005A60
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x06000137 RID: 311 RVA: 0x0000786F File Offset: 0x00005A6F
		// (set) Token: 0x06000138 RID: 312 RVA: 0x00007877 File Offset: 0x00005A77
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x06000139 RID: 313 RVA: 0x00007895 File Offset: 0x00005A95
		// (set) Token: 0x0600013A RID: 314 RVA: 0x0000789D File Offset: 0x00005A9D
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x0600013B RID: 315 RVA: 0x000078BB File Offset: 0x00005ABB
		// (set) Token: 0x0600013C RID: 316 RVA: 0x000078C3 File Offset: 0x00005AC3
		[DataSourceProperty]
		public string TournamentWinnerTitle
		{
			get
			{
				return this._tournamentWinnerTitle;
			}
			set
			{
				if (value != this._tournamentWinnerTitle)
				{
					this._tournamentWinnerTitle = value;
					base.OnPropertyChangedWithValue<string>(value, "TournamentWinnerTitle");
				}
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x0600013D RID: 317 RVA: 0x000078E6 File Offset: 0x00005AE6
		// (set) Token: 0x0600013E RID: 318 RVA: 0x000078EE File Offset: 0x00005AEE
		[DataSourceProperty]
		public TournamentParticipantVM TournamentWinner
		{
			get
			{
				return this._tournamentWinner;
			}
			set
			{
				if (value != this._tournamentWinner)
				{
					this._tournamentWinner = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "TournamentWinner");
				}
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x0600013F RID: 319 RVA: 0x0000790C File Offset: 0x00005B0C
		// (set) Token: 0x06000140 RID: 320 RVA: 0x00007914 File Offset: 0x00005B14
		[DataSourceProperty]
		public int MaximumBetValue
		{
			get
			{
				return this._maximumBetValue;
			}
			set
			{
				if (value != this._maximumBetValue)
				{
					this._maximumBetValue = value;
					base.OnPropertyChangedWithValue(value, "MaximumBetValue");
					this._wageredDenars = -1;
					this.WageredDenars = 0;
				}
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000141 RID: 321 RVA: 0x00007940 File Offset: 0x00005B40
		[DataSourceProperty]
		public bool IsBetButtonEnabled
		{
			get
			{
				return this.PlayerCanJoinMatch() && this.Tournament.GetMaximumBet() > this._thisRoundBettedAmount && Hero.MainHero.Gold > 0;
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x06000142 RID: 322 RVA: 0x0000796C File Offset: 0x00005B6C
		// (set) Token: 0x06000143 RID: 323 RVA: 0x00007974 File Offset: 0x00005B74
		[DataSourceProperty]
		public string BetText
		{
			get
			{
				return this._betText;
			}
			set
			{
				if (value != this._betText)
				{
					this._betText = value;
					base.OnPropertyChangedWithValue<string>(value, "BetText");
				}
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x06000144 RID: 324 RVA: 0x00007997 File Offset: 0x00005B97
		// (set) Token: 0x06000145 RID: 325 RVA: 0x0000799F File Offset: 0x00005B9F
		[DataSourceProperty]
		public string BetTitleText
		{
			get
			{
				return this._betTitleText;
			}
			set
			{
				if (value != this._betTitleText)
				{
					this._betTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "BetTitleText");
				}
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x06000146 RID: 326 RVA: 0x000079C2 File Offset: 0x00005BC2
		// (set) Token: 0x06000147 RID: 327 RVA: 0x000079CA File Offset: 0x00005BCA
		[DataSourceProperty]
		public string CurrentWagerText
		{
			get
			{
				return this._currentWagerText;
			}
			set
			{
				if (value != this._currentWagerText)
				{
					this._currentWagerText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentWagerText");
				}
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000148 RID: 328 RVA: 0x000079ED File Offset: 0x00005BED
		// (set) Token: 0x06000149 RID: 329 RVA: 0x000079F5 File Offset: 0x00005BF5
		[DataSourceProperty]
		public string BetDescriptionText
		{
			get
			{
				return this._betDescriptionText;
			}
			set
			{
				if (value != this._betDescriptionText)
				{
					this._betDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "BetDescriptionText");
				}
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x0600014A RID: 330 RVA: 0x00007A18 File Offset: 0x00005C18
		// (set) Token: 0x0600014B RID: 331 RVA: 0x00007A20 File Offset: 0x00005C20
		[DataSourceProperty]
		public ItemImageIdentifierVM PrizeVisual
		{
			get
			{
				return this._prizeVisual;
			}
			set
			{
				if (value != this._prizeVisual)
				{
					this._prizeVisual = value;
					base.OnPropertyChangedWithValue<ItemImageIdentifierVM>(value, "PrizeVisual");
				}
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x0600014C RID: 332 RVA: 0x00007A3E File Offset: 0x00005C3E
		// (set) Token: 0x0600014D RID: 333 RVA: 0x00007A46 File Offset: 0x00005C46
		[DataSourceProperty]
		public string PrizeItemName
		{
			get
			{
				return this._prizeItemName;
			}
			set
			{
				if (value != this._prizeItemName)
				{
					this._prizeItemName = value;
					base.OnPropertyChangedWithValue<string>(value, "PrizeItemName");
				}
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x0600014E RID: 334 RVA: 0x00007A69 File Offset: 0x00005C69
		// (set) Token: 0x0600014F RID: 335 RVA: 0x00007A71 File Offset: 0x00005C71
		[DataSourceProperty]
		public string TournamentPrizeText
		{
			get
			{
				return this._tournamentPrizeText;
			}
			set
			{
				if (value != this._tournamentPrizeText)
				{
					this._tournamentPrizeText = value;
					base.OnPropertyChangedWithValue<string>(value, "TournamentPrizeText");
				}
			}
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000150 RID: 336 RVA: 0x00007A94 File Offset: 0x00005C94
		// (set) Token: 0x06000151 RID: 337 RVA: 0x00007A9C File Offset: 0x00005C9C
		[DataSourceProperty]
		public int WageredDenars
		{
			get
			{
				return this._wageredDenars;
			}
			set
			{
				if (value != this._wageredDenars)
				{
					this._wageredDenars = value;
					base.OnPropertyChangedWithValue(value, "WageredDenars");
					this.ExpectedBetDenars = ((this._wageredDenars == 0) ? 0 : this.Tournament.GetExpectedDenarsForBet(this._wageredDenars));
				}
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000152 RID: 338 RVA: 0x00007ADC File Offset: 0x00005CDC
		// (set) Token: 0x06000153 RID: 339 RVA: 0x00007AE4 File Offset: 0x00005CE4
		[DataSourceProperty]
		public int ExpectedBetDenars
		{
			get
			{
				return this._expectedBetDenars;
			}
			set
			{
				if (value != this._expectedBetDenars)
				{
					this._expectedBetDenars = value;
					base.OnPropertyChangedWithValue(value, "ExpectedBetDenars");
				}
			}
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000154 RID: 340 RVA: 0x00007B02 File Offset: 0x00005D02
		// (set) Token: 0x06000155 RID: 341 RVA: 0x00007B0A File Offset: 0x00005D0A
		[DataSourceProperty]
		public string BetOddsText
		{
			get
			{
				return this._betOddsText;
			}
			set
			{
				if (value != this._betOddsText)
				{
					this._betOddsText = value;
					base.OnPropertyChangedWithValue<string>(value, "BetOddsText");
				}
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000156 RID: 342 RVA: 0x00007B2D File Offset: 0x00005D2D
		// (set) Token: 0x06000157 RID: 343 RVA: 0x00007B35 File Offset: 0x00005D35
		[DataSourceProperty]
		public string BettedDenarsText
		{
			get
			{
				return this._bettedDenarsText;
			}
			set
			{
				if (value != this._bettedDenarsText)
				{
					this._bettedDenarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "BettedDenarsText");
				}
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000158 RID: 344 RVA: 0x00007B58 File Offset: 0x00005D58
		// (set) Token: 0x06000159 RID: 345 RVA: 0x00007B60 File Offset: 0x00005D60
		[DataSourceProperty]
		public string OverallExpectedDenarsText
		{
			get
			{
				return this._overallExpectedDenarsText;
			}
			set
			{
				if (value != this._overallExpectedDenarsText)
				{
					this._overallExpectedDenarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "OverallExpectedDenarsText");
				}
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x0600015A RID: 346 RVA: 0x00007B83 File Offset: 0x00005D83
		// (set) Token: 0x0600015B RID: 347 RVA: 0x00007B8B File Offset: 0x00005D8B
		[DataSourceProperty]
		public string CurrentExpectedDenarsText
		{
			get
			{
				return this._currentExpectedDenarsText;
			}
			set
			{
				if (value != this._currentExpectedDenarsText)
				{
					this._currentExpectedDenarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentExpectedDenarsText");
				}
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x0600015C RID: 348 RVA: 0x00007BAE File Offset: 0x00005DAE
		// (set) Token: 0x0600015D RID: 349 RVA: 0x00007BB6 File Offset: 0x00005DB6
		[DataSourceProperty]
		public string TotalDenarsText
		{
			get
			{
				return this._totalDenarsText;
			}
			set
			{
				if (value != this._totalDenarsText)
				{
					this._totalDenarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalDenarsText");
				}
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x0600015E RID: 350 RVA: 0x00007BD9 File Offset: 0x00005DD9
		// (set) Token: 0x0600015F RID: 351 RVA: 0x00007BE1 File Offset: 0x00005DE1
		[DataSourceProperty]
		public string AcceptText
		{
			get
			{
				return this._acceptText;
			}
			set
			{
				if (value != this._acceptText)
				{
					this._acceptText = value;
					base.OnPropertyChangedWithValue<string>(value, "AcceptText");
				}
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000160 RID: 352 RVA: 0x00007C04 File Offset: 0x00005E04
		// (set) Token: 0x06000161 RID: 353 RVA: 0x00007C0C File Offset: 0x00005E0C
		[DataSourceProperty]
		public string CancelText
		{
			get
			{
				return this._cancelText;
			}
			set
			{
				if (value != this._cancelText)
				{
					this._cancelText = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelText");
				}
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x06000162 RID: 354 RVA: 0x00007C2F File Offset: 0x00005E2F
		// (set) Token: 0x06000163 RID: 355 RVA: 0x00007C37 File Offset: 0x00005E37
		[DataSourceProperty]
		public bool IsCurrentMatchActive
		{
			get
			{
				return this._isCurrentMatchActive;
			}
			set
			{
				this._isCurrentMatchActive = value;
				base.OnPropertyChangedWithValue(value, "IsCurrentMatchActive");
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00007C4C File Offset: 0x00005E4C
		// (set) Token: 0x06000165 RID: 357 RVA: 0x00007C54 File Offset: 0x00005E54
		[DataSourceProperty]
		public TournamentMatchVM CurrentMatch
		{
			get
			{
				return this._currentMatch;
			}
			set
			{
				if (value != this._currentMatch)
				{
					TournamentMatchVM currentMatch = this._currentMatch;
					if (currentMatch != null && currentMatch.IsValid)
					{
						this._currentMatch.State = 2;
						this._currentMatch.Refresh(false);
						int num = this._rounds.FindIndex((TournamentRoundVM r) => r.Matches.Any((TournamentMatchVM m) => m.Match == this.Tournament.LastMatch));
						if (num < this.Tournament.Rounds.Length - 1)
						{
							this._rounds[num + 1].Initialize();
						}
					}
					this._currentMatch = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "CurrentMatch");
					if (this._currentMatch != null)
					{
						this._currentMatch.State = 1;
					}
				}
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00007CFD File Offset: 0x00005EFD
		// (set) Token: 0x06000167 RID: 359 RVA: 0x00007D17 File Offset: 0x00005F17
		[DataSourceProperty]
		public bool IsTournamentIncomplete
		{
			get
			{
				return this.Tournament == null || this.Tournament.CurrentMatch != null;
			}
			set
			{
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x06000168 RID: 360 RVA: 0x00007D19 File Offset: 0x00005F19
		// (set) Token: 0x06000169 RID: 361 RVA: 0x00007D21 File Offset: 0x00005F21
		[DataSourceProperty]
		public int ActiveRoundIndex
		{
			get
			{
				return this._activeRoundIndex;
			}
			set
			{
				if (value != this._activeRoundIndex)
				{
					this.OnNewRoundStarted(this._activeRoundIndex, value);
					this._activeRoundIndex = value;
					base.OnPropertyChangedWithValue(value, "ActiveRoundIndex");
					this.RefreshBetProperties();
				}
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600016A RID: 362 RVA: 0x00007D52 File Offset: 0x00005F52
		// (set) Token: 0x0600016B RID: 363 RVA: 0x00007D5A File Offset: 0x00005F5A
		[DataSourceProperty]
		public bool CanPlayerJoin
		{
			get
			{
				return this._canPlayerJoin;
			}
			set
			{
				if (value != this._canPlayerJoin)
				{
					this._canPlayerJoin = value;
					base.OnPropertyChangedWithValue(value, "CanPlayerJoin");
				}
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x0600016C RID: 364 RVA: 0x00007D78 File Offset: 0x00005F78
		// (set) Token: 0x0600016D RID: 365 RVA: 0x00007D80 File Offset: 0x00005F80
		[DataSourceProperty]
		public bool HasPrizeItem
		{
			get
			{
				return this._hasPrizeItem;
			}
			set
			{
				if (value != this._hasPrizeItem)
				{
					this._hasPrizeItem = value;
					base.OnPropertyChangedWithValue(value, "HasPrizeItem");
				}
			}
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x0600016E RID: 366 RVA: 0x00007D9E File Offset: 0x00005F9E
		// (set) Token: 0x0600016F RID: 367 RVA: 0x00007DA6 File Offset: 0x00005FA6
		[DataSourceProperty]
		public string JoinTournamentText
		{
			get
			{
				return this._joinTournamentText;
			}
			set
			{
				if (value != this._joinTournamentText)
				{
					this._joinTournamentText = value;
					base.OnPropertyChangedWithValue<string>(value, "JoinTournamentText");
				}
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000170 RID: 368 RVA: 0x00007DC9 File Offset: 0x00005FC9
		// (set) Token: 0x06000171 RID: 369 RVA: 0x00007DD1 File Offset: 0x00005FD1
		[DataSourceProperty]
		public string SkipRoundText
		{
			get
			{
				return this._skipRoundText;
			}
			set
			{
				if (value != this._skipRoundText)
				{
					this._skipRoundText = value;
					base.OnPropertyChangedWithValue<string>(value, "SkipRoundText");
				}
			}
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000172 RID: 370 RVA: 0x00007DF4 File Offset: 0x00005FF4
		// (set) Token: 0x06000173 RID: 371 RVA: 0x00007DFC File Offset: 0x00005FFC
		[DataSourceProperty]
		public string WatchRoundText
		{
			get
			{
				return this._watchRoundText;
			}
			set
			{
				if (value != this._watchRoundText)
				{
					this._watchRoundText = value;
					base.OnPropertyChangedWithValue<string>(value, "WatchRoundText");
				}
			}
		}

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000174 RID: 372 RVA: 0x00007E1F File Offset: 0x0000601F
		// (set) Token: 0x06000175 RID: 373 RVA: 0x00007E27 File Offset: 0x00006027
		[DataSourceProperty]
		public string LeaveText
		{
			get
			{
				return this._leaveText;
			}
			set
			{
				if (value != this._leaveText)
				{
					this._leaveText = value;
					base.OnPropertyChangedWithValue<string>(value, "LeaveText");
				}
			}
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000176 RID: 374 RVA: 0x00007E4A File Offset: 0x0000604A
		// (set) Token: 0x06000177 RID: 375 RVA: 0x00007E52 File Offset: 0x00006052
		[DataSourceProperty]
		public TournamentRoundVM Round1
		{
			get
			{
				return this._round1;
			}
			set
			{
				if (value != this._round1)
				{
					this._round1 = value;
					base.OnPropertyChangedWithValue<TournamentRoundVM>(value, "Round1");
				}
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000178 RID: 376 RVA: 0x00007E70 File Offset: 0x00006070
		// (set) Token: 0x06000179 RID: 377 RVA: 0x00007E78 File Offset: 0x00006078
		[DataSourceProperty]
		public TournamentRoundVM Round2
		{
			get
			{
				return this._round2;
			}
			set
			{
				if (value != this._round2)
				{
					this._round2 = value;
					base.OnPropertyChangedWithValue<TournamentRoundVM>(value, "Round2");
				}
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x0600017A RID: 378 RVA: 0x00007E96 File Offset: 0x00006096
		// (set) Token: 0x0600017B RID: 379 RVA: 0x00007E9E File Offset: 0x0000609E
		[DataSourceProperty]
		public TournamentRoundVM Round3
		{
			get
			{
				return this._round3;
			}
			set
			{
				if (value != this._round3)
				{
					this._round3 = value;
					base.OnPropertyChangedWithValue<TournamentRoundVM>(value, "Round3");
				}
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x0600017C RID: 380 RVA: 0x00007EBC File Offset: 0x000060BC
		// (set) Token: 0x0600017D RID: 381 RVA: 0x00007EC4 File Offset: 0x000060C4
		[DataSourceProperty]
		public TournamentRoundVM Round4
		{
			get
			{
				return this._round4;
			}
			set
			{
				if (value != this._round4)
				{
					this._round4 = value;
					base.OnPropertyChangedWithValue<TournamentRoundVM>(value, "Round4");
				}
			}
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x0600017E RID: 382 RVA: 0x00007EE2 File Offset: 0x000060E2
		[DataSourceProperty]
		public bool InitializationOver
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x0600017F RID: 383 RVA: 0x00007EE5 File Offset: 0x000060E5
		// (set) Token: 0x06000180 RID: 384 RVA: 0x00007EED File Offset: 0x000060ED
		[DataSourceProperty]
		public string TournamentTitle
		{
			get
			{
				return this._tournamentTitle;
			}
			set
			{
				if (value != this._tournamentTitle)
				{
					this._tournamentTitle = value;
					base.OnPropertyChangedWithValue<string>(value, "TournamentTitle");
				}
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000181 RID: 385 RVA: 0x00007F10 File Offset: 0x00006110
		// (set) Token: 0x06000182 RID: 386 RVA: 0x00007F18 File Offset: 0x00006118
		[DataSourceProperty]
		public bool IsOver
		{
			get
			{
				return this._isOver;
			}
			set
			{
				if (this._isOver != value)
				{
					this._isOver = value;
					base.OnPropertyChangedWithValue(value, "IsOver");
				}
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000183 RID: 387 RVA: 0x00007F36 File Offset: 0x00006136
		// (set) Token: 0x06000184 RID: 388 RVA: 0x00007F3E File Offset: 0x0000613E
		[DataSourceProperty]
		public string WinnerIntro
		{
			get
			{
				return this._winnerIntro;
			}
			set
			{
				if (value != this._winnerIntro)
				{
					this._winnerIntro = value;
					base.OnPropertyChangedWithValue<string>(value, "WinnerIntro");
				}
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000185 RID: 389 RVA: 0x00007F61 File Offset: 0x00006161
		// (set) Token: 0x06000186 RID: 390 RVA: 0x00007F69 File Offset: 0x00006169
		[DataSourceProperty]
		public MBBindingList<TournamentRewardVM> BattleRewards
		{
			get
			{
				return this._battleRewards;
			}
			set
			{
				if (value != this._battleRewards)
				{
					this._battleRewards = value;
					base.OnPropertyChangedWithValue<MBBindingList<TournamentRewardVM>>(value, "BattleRewards");
				}
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000187 RID: 391 RVA: 0x00007F87 File Offset: 0x00006187
		// (set) Token: 0x06000188 RID: 392 RVA: 0x00007F8F File Offset: 0x0000618F
		[DataSourceProperty]
		public bool IsWinnerHero
		{
			get
			{
				return this._isWinnerHero;
			}
			set
			{
				if (value != this._isWinnerHero)
				{
					this._isWinnerHero = value;
					base.OnPropertyChangedWithValue(value, "IsWinnerHero");
				}
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000189 RID: 393 RVA: 0x00007FAD File Offset: 0x000061AD
		// (set) Token: 0x0600018A RID: 394 RVA: 0x00007FB5 File Offset: 0x000061B5
		[DataSourceProperty]
		public bool IsBetWindowEnabled
		{
			get
			{
				return this._isBetWindowEnabled;
			}
			set
			{
				if (value != this._isBetWindowEnabled)
				{
					this._isBetWindowEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsBetWindowEnabled");
				}
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x0600018B RID: 395 RVA: 0x00007FD3 File Offset: 0x000061D3
		// (set) Token: 0x0600018C RID: 396 RVA: 0x00007FDB File Offset: 0x000061DB
		[DataSourceProperty]
		public BannerImageIdentifierVM WinnerBanner
		{
			get
			{
				return this._winnerBanner;
			}
			set
			{
				if (value != this._winnerBanner)
				{
					this._winnerBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "WinnerBanner");
				}
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x0600018D RID: 397 RVA: 0x00007FF9 File Offset: 0x000061F9
		// (set) Token: 0x0600018E RID: 398 RVA: 0x00008001 File Offset: 0x00006201
		[DataSourceProperty]
		public HintViewModel SkipAllRoundsHint
		{
			get
			{
				return this._skipAllRoundsHint;
			}
			set
			{
				if (value != this._skipAllRoundsHint)
				{
					this._skipAllRoundsHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "SkipAllRoundsHint");
				}
			}
		}

		// Token: 0x04000083 RID: 131
		private readonly List<TournamentRoundVM> _rounds;

		// Token: 0x04000084 RID: 132
		private int _thisRoundBettedAmount;

		// Token: 0x04000085 RID: 133
		private bool _isPlayerParticipating;

		// Token: 0x04000086 RID: 134
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000087 RID: 135
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000088 RID: 136
		private TournamentRoundVM _round1;

		// Token: 0x04000089 RID: 137
		private TournamentRoundVM _round2;

		// Token: 0x0400008A RID: 138
		private TournamentRoundVM _round3;

		// Token: 0x0400008B RID: 139
		private TournamentRoundVM _round4;

		// Token: 0x0400008C RID: 140
		private int _activeRoundIndex = -1;

		// Token: 0x0400008D RID: 141
		private string _joinTournamentText;

		// Token: 0x0400008E RID: 142
		private string _skipRoundText;

		// Token: 0x0400008F RID: 143
		private string _watchRoundText;

		// Token: 0x04000090 RID: 144
		private string _leaveText;

		// Token: 0x04000091 RID: 145
		private bool _canPlayerJoin;

		// Token: 0x04000092 RID: 146
		private TournamentMatchVM _currentMatch;

		// Token: 0x04000093 RID: 147
		private bool _isCurrentMatchActive;

		// Token: 0x04000094 RID: 148
		private string _betTitleText;

		// Token: 0x04000095 RID: 149
		private string _betDescriptionText;

		// Token: 0x04000096 RID: 150
		private string _betOddsText;

		// Token: 0x04000097 RID: 151
		private string _bettedDenarsText;

		// Token: 0x04000098 RID: 152
		private string _overallExpectedDenarsText;

		// Token: 0x04000099 RID: 153
		private string _currentExpectedDenarsText;

		// Token: 0x0400009A RID: 154
		private string _totalDenarsText;

		// Token: 0x0400009B RID: 155
		private string _acceptText;

		// Token: 0x0400009C RID: 156
		private string _cancelText;

		// Token: 0x0400009D RID: 157
		private string _prizeItemName;

		// Token: 0x0400009E RID: 158
		private string _tournamentPrizeText;

		// Token: 0x0400009F RID: 159
		private string _currentWagerText;

		// Token: 0x040000A0 RID: 160
		private int _wageredDenars = -1;

		// Token: 0x040000A1 RID: 161
		private int _expectedBetDenars = -1;

		// Token: 0x040000A2 RID: 162
		private string _betText;

		// Token: 0x040000A3 RID: 163
		private int _maximumBetValue;

		// Token: 0x040000A4 RID: 164
		private string _tournamentWinnerTitle;

		// Token: 0x040000A5 RID: 165
		private TournamentParticipantVM _tournamentWinner;

		// Token: 0x040000A6 RID: 166
		private string _tournamentTitle;

		// Token: 0x040000A7 RID: 167
		private bool _isOver;

		// Token: 0x040000A8 RID: 168
		private bool _hasPrizeItem;

		// Token: 0x040000A9 RID: 169
		private bool _isWinnerHero;

		// Token: 0x040000AA RID: 170
		private bool _isBetWindowEnabled;

		// Token: 0x040000AB RID: 171
		private string _winnerIntro;

		// Token: 0x040000AC RID: 172
		private ItemImageIdentifierVM _prizeVisual;

		// Token: 0x040000AD RID: 173
		private BannerImageIdentifierVM _winnerBanner;

		// Token: 0x040000AE RID: 174
		private MBBindingList<TournamentRewardVM> _battleRewards;

		// Token: 0x040000AF RID: 175
		private HintViewModel _skipAllRoundsHint;
	}
}
