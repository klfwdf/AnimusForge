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

namespace SandBox.ViewModelCollection.Tournament;

public class TournamentVM : ViewModel
{
	private readonly List<TournamentRoundVM> _rounds;

	private int _thisRoundBettedAmount;

	private bool _isPlayerParticipating;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private TournamentRoundVM _round1;

	private TournamentRoundVM _round2;

	private TournamentRoundVM _round3;

	private TournamentRoundVM _round4;

	private int _activeRoundIndex = -1;

	private string _joinTournamentText;

	private string _skipRoundText;

	private string _watchRoundText;

	private string _leaveText;

	private bool _canPlayerJoin;

	private TournamentMatchVM _currentMatch;

	private bool _isCurrentMatchActive;

	private string _betTitleText;

	private string _betDescriptionText;

	private string _betOddsText;

	private string _bettedDenarsText;

	private string _overallExpectedDenarsText;

	private string _currentExpectedDenarsText;

	private string _totalDenarsText;

	private string _acceptText;

	private string _cancelText;

	private string _prizeItemName;

	private string _tournamentPrizeText;

	private string _currentWagerText;

	private int _wageredDenars = -1;

	private int _expectedBetDenars = -1;

	private string _betText;

	private int _maximumBetValue;

	private string _tournamentWinnerTitle;

	private TournamentParticipantVM _tournamentWinner;

	private string _tournamentTitle;

	private bool _isOver;

	private bool _hasPrizeItem;

	private bool _isWinnerHero;

	private bool _isBetWindowEnabled;

	private string _winnerIntro;

	private ItemImageIdentifierVM _prizeVisual;

	private BannerImageIdentifierVM _winnerBanner;

	private MBBindingList<TournamentRewardVM> _battleRewards;

	private HintViewModel _skipAllRoundsHint;

	public Action DisableUI { get; }

	public TournamentBehavior Tournament { get; }

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public string TournamentWinnerTitle
	{
		get
		{
			return _tournamentWinnerTitle;
		}
		set
		{
			if (value != _tournamentWinnerTitle)
			{
				_tournamentWinnerTitle = value;
				OnPropertyChangedWithValue(value, "TournamentWinnerTitle");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM TournamentWinner
	{
		get
		{
			return _tournamentWinner;
		}
		set
		{
			if (value != _tournamentWinner)
			{
				_tournamentWinner = value;
				OnPropertyChangedWithValue(value, "TournamentWinner");
			}
		}
	}

	[DataSourceProperty]
	public int MaximumBetValue
	{
		get
		{
			return _maximumBetValue;
		}
		set
		{
			if (value != _maximumBetValue)
			{
				_maximumBetValue = value;
				OnPropertyChangedWithValue(value, "MaximumBetValue");
				_wageredDenars = -1;
				WageredDenars = 0;
			}
		}
	}

	[DataSourceProperty]
	public bool IsBetButtonEnabled
	{
		get
		{
			if (PlayerCanJoinMatch() && Tournament.GetMaximumBet() > _thisRoundBettedAmount)
			{
				return Hero.MainHero.Gold > 0;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public string BetText
	{
		get
		{
			return _betText;
		}
		set
		{
			if (value != _betText)
			{
				_betText = value;
				OnPropertyChangedWithValue(value, "BetText");
			}
		}
	}

	[DataSourceProperty]
	public string BetTitleText
	{
		get
		{
			return _betTitleText;
		}
		set
		{
			if (value != _betTitleText)
			{
				_betTitleText = value;
				OnPropertyChangedWithValue(value, "BetTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentWagerText
	{
		get
		{
			return _currentWagerText;
		}
		set
		{
			if (value != _currentWagerText)
			{
				_currentWagerText = value;
				OnPropertyChangedWithValue(value, "CurrentWagerText");
			}
		}
	}

	[DataSourceProperty]
	public string BetDescriptionText
	{
		get
		{
			return _betDescriptionText;
		}
		set
		{
			if (value != _betDescriptionText)
			{
				_betDescriptionText = value;
				OnPropertyChangedWithValue(value, "BetDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public ItemImageIdentifierVM PrizeVisual
	{
		get
		{
			return _prizeVisual;
		}
		set
		{
			if (value != _prizeVisual)
			{
				_prizeVisual = value;
				OnPropertyChangedWithValue(value, "PrizeVisual");
			}
		}
	}

	[DataSourceProperty]
	public string PrizeItemName
	{
		get
		{
			return _prizeItemName;
		}
		set
		{
			if (value != _prizeItemName)
			{
				_prizeItemName = value;
				OnPropertyChangedWithValue(value, "PrizeItemName");
			}
		}
	}

	[DataSourceProperty]
	public string TournamentPrizeText
	{
		get
		{
			return _tournamentPrizeText;
		}
		set
		{
			if (value != _tournamentPrizeText)
			{
				_tournamentPrizeText = value;
				OnPropertyChangedWithValue(value, "TournamentPrizeText");
			}
		}
	}

	[DataSourceProperty]
	public int WageredDenars
	{
		get
		{
			return _wageredDenars;
		}
		set
		{
			if (value != _wageredDenars)
			{
				_wageredDenars = value;
				OnPropertyChangedWithValue(value, "WageredDenars");
				ExpectedBetDenars = ((_wageredDenars != 0) ? Tournament.GetExpectedDenarsForBet(_wageredDenars) : 0);
			}
		}
	}

	[DataSourceProperty]
	public int ExpectedBetDenars
	{
		get
		{
			return _expectedBetDenars;
		}
		set
		{
			if (value != _expectedBetDenars)
			{
				_expectedBetDenars = value;
				OnPropertyChangedWithValue(value, "ExpectedBetDenars");
			}
		}
	}

	[DataSourceProperty]
	public string BetOddsText
	{
		get
		{
			return _betOddsText;
		}
		set
		{
			if (value != _betOddsText)
			{
				_betOddsText = value;
				OnPropertyChangedWithValue(value, "BetOddsText");
			}
		}
	}

	[DataSourceProperty]
	public string BettedDenarsText
	{
		get
		{
			return _bettedDenarsText;
		}
		set
		{
			if (value != _bettedDenarsText)
			{
				_bettedDenarsText = value;
				OnPropertyChangedWithValue(value, "BettedDenarsText");
			}
		}
	}

	[DataSourceProperty]
	public string OverallExpectedDenarsText
	{
		get
		{
			return _overallExpectedDenarsText;
		}
		set
		{
			if (value != _overallExpectedDenarsText)
			{
				_overallExpectedDenarsText = value;
				OnPropertyChangedWithValue(value, "OverallExpectedDenarsText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentExpectedDenarsText
	{
		get
		{
			return _currentExpectedDenarsText;
		}
		set
		{
			if (value != _currentExpectedDenarsText)
			{
				_currentExpectedDenarsText = value;
				OnPropertyChangedWithValue(value, "CurrentExpectedDenarsText");
			}
		}
	}

	[DataSourceProperty]
	public string TotalDenarsText
	{
		get
		{
			return _totalDenarsText;
		}
		set
		{
			if (value != _totalDenarsText)
			{
				_totalDenarsText = value;
				OnPropertyChangedWithValue(value, "TotalDenarsText");
			}
		}
	}

	[DataSourceProperty]
	public string AcceptText
	{
		get
		{
			return _acceptText;
		}
		set
		{
			if (value != _acceptText)
			{
				_acceptText = value;
				OnPropertyChangedWithValue(value, "AcceptText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentMatchActive
	{
		get
		{
			return _isCurrentMatchActive;
		}
		set
		{
			_isCurrentMatchActive = value;
			OnPropertyChangedWithValue(value, "IsCurrentMatchActive");
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM CurrentMatch
	{
		get
		{
			return _currentMatch;
		}
		set
		{
			if (value == _currentMatch)
			{
				return;
			}
			TournamentMatchVM currentMatch = _currentMatch;
			if (currentMatch != null && currentMatch.IsValid)
			{
				_currentMatch.State = 2;
				_currentMatch.Refresh(forceRefresh: false);
				int num = _rounds.FindIndex((TournamentRoundVM r) => r.Matches.Any((TournamentMatchVM m) => m.Match == Tournament.LastMatch));
				if (num < Tournament.Rounds.Length - 1)
				{
					_rounds[num + 1].Initialize();
				}
			}
			_currentMatch = value;
			OnPropertyChangedWithValue(value, "CurrentMatch");
			if (_currentMatch != null)
			{
				_currentMatch.State = 1;
			}
		}
	}

	[DataSourceProperty]
	public bool IsTournamentIncomplete
	{
		get
		{
			if (Tournament != null)
			{
				return Tournament.CurrentMatch != null;
			}
			return true;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public int ActiveRoundIndex
	{
		get
		{
			return _activeRoundIndex;
		}
		set
		{
			if (value != _activeRoundIndex)
			{
				OnNewRoundStarted(_activeRoundIndex, value);
				_activeRoundIndex = value;
				OnPropertyChangedWithValue(value, "ActiveRoundIndex");
				RefreshBetProperties();
			}
		}
	}

	[DataSourceProperty]
	public bool CanPlayerJoin
	{
		get
		{
			return _canPlayerJoin;
		}
		set
		{
			if (value != _canPlayerJoin)
			{
				_canPlayerJoin = value;
				OnPropertyChangedWithValue(value, "CanPlayerJoin");
			}
		}
	}

	[DataSourceProperty]
	public bool HasPrizeItem
	{
		get
		{
			return _hasPrizeItem;
		}
		set
		{
			if (value != _hasPrizeItem)
			{
				_hasPrizeItem = value;
				OnPropertyChangedWithValue(value, "HasPrizeItem");
			}
		}
	}

	[DataSourceProperty]
	public string JoinTournamentText
	{
		get
		{
			return _joinTournamentText;
		}
		set
		{
			if (value != _joinTournamentText)
			{
				_joinTournamentText = value;
				OnPropertyChangedWithValue(value, "JoinTournamentText");
			}
		}
	}

	[DataSourceProperty]
	public string SkipRoundText
	{
		get
		{
			return _skipRoundText;
		}
		set
		{
			if (value != _skipRoundText)
			{
				_skipRoundText = value;
				OnPropertyChangedWithValue(value, "SkipRoundText");
			}
		}
	}

	[DataSourceProperty]
	public string WatchRoundText
	{
		get
		{
			return _watchRoundText;
		}
		set
		{
			if (value != _watchRoundText)
			{
				_watchRoundText = value;
				OnPropertyChangedWithValue(value, "WatchRoundText");
			}
		}
	}

	[DataSourceProperty]
	public string LeaveText
	{
		get
		{
			return _leaveText;
		}
		set
		{
			if (value != _leaveText)
			{
				_leaveText = value;
				OnPropertyChangedWithValue(value, "LeaveText");
			}
		}
	}

	[DataSourceProperty]
	public TournamentRoundVM Round1
	{
		get
		{
			return _round1;
		}
		set
		{
			if (value != _round1)
			{
				_round1 = value;
				OnPropertyChangedWithValue(value, "Round1");
			}
		}
	}

	[DataSourceProperty]
	public TournamentRoundVM Round2
	{
		get
		{
			return _round2;
		}
		set
		{
			if (value != _round2)
			{
				_round2 = value;
				OnPropertyChangedWithValue(value, "Round2");
			}
		}
	}

	[DataSourceProperty]
	public TournamentRoundVM Round3
	{
		get
		{
			return _round3;
		}
		set
		{
			if (value != _round3)
			{
				_round3 = value;
				OnPropertyChangedWithValue(value, "Round3");
			}
		}
	}

	[DataSourceProperty]
	public TournamentRoundVM Round4
	{
		get
		{
			return _round4;
		}
		set
		{
			if (value != _round4)
			{
				_round4 = value;
				OnPropertyChangedWithValue(value, "Round4");
			}
		}
	}

	[DataSourceProperty]
	public bool InitializationOver => true;

	[DataSourceProperty]
	public string TournamentTitle
	{
		get
		{
			return _tournamentTitle;
		}
		set
		{
			if (value != _tournamentTitle)
			{
				_tournamentTitle = value;
				OnPropertyChangedWithValue(value, "TournamentTitle");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOver
	{
		get
		{
			return _isOver;
		}
		set
		{
			if (_isOver != value)
			{
				_isOver = value;
				OnPropertyChangedWithValue(value, "IsOver");
			}
		}
	}

	[DataSourceProperty]
	public string WinnerIntro
	{
		get
		{
			return _winnerIntro;
		}
		set
		{
			if (value != _winnerIntro)
			{
				_winnerIntro = value;
				OnPropertyChangedWithValue(value, "WinnerIntro");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<TournamentRewardVM> BattleRewards
	{
		get
		{
			return _battleRewards;
		}
		set
		{
			if (value != _battleRewards)
			{
				_battleRewards = value;
				OnPropertyChangedWithValue(value, "BattleRewards");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWinnerHero
	{
		get
		{
			return _isWinnerHero;
		}
		set
		{
			if (value != _isWinnerHero)
			{
				_isWinnerHero = value;
				OnPropertyChangedWithValue(value, "IsWinnerHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBetWindowEnabled
	{
		get
		{
			return _isBetWindowEnabled;
		}
		set
		{
			if (value != _isBetWindowEnabled)
			{
				_isBetWindowEnabled = value;
				OnPropertyChangedWithValue(value, "IsBetWindowEnabled");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM WinnerBanner
	{
		get
		{
			return _winnerBanner;
		}
		set
		{
			if (value != _winnerBanner)
			{
				_winnerBanner = value;
				OnPropertyChangedWithValue(value, "WinnerBanner");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SkipAllRoundsHint
	{
		get
		{
			return _skipAllRoundsHint;
		}
		set
		{
			if (value != _skipAllRoundsHint)
			{
				_skipAllRoundsHint = value;
				OnPropertyChangedWithValue(value, "SkipAllRoundsHint");
			}
		}
	}

	public TournamentVM(Action disableUI, TournamentBehavior tournamentBehavior)
	{
		DisableUI = disableUI;
		CurrentMatch = new TournamentMatchVM();
		Round1 = new TournamentRoundVM();
		Round2 = new TournamentRoundVM();
		Round3 = new TournamentRoundVM();
		Round4 = new TournamentRoundVM();
		_rounds = new List<TournamentRoundVM> { Round1, Round2, Round3, Round4 };
		_tournamentWinner = new TournamentParticipantVM();
		Tournament = tournamentBehavior;
		WinnerIntro = GameTexts.FindText("str_tournament_winner_intro").ToString();
		BattleRewards = new MBBindingList<TournamentRewardVM>();
		for (int i = 0; i < _rounds.Count; i++)
		{
			_rounds[i].Initialize(Tournament.Rounds[i], GameTexts.FindText("str_tournament_round", i.ToString()));
		}
		Refresh();
		Tournament.TournamentEnd += OnTournamentEnd;
		PrizeVisual = (HasPrizeItem ? new ItemImageIdentifierVM(Tournament.TournamentGame.Prize) : new ItemImageIdentifierVM(null));
		SkipAllRoundsHint = new HintViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		LeaveText = GameTexts.FindText("str_tournament_leave").ToString();
		SkipRoundText = GameTexts.FindText("str_tournament_skip_round").ToString();
		WatchRoundText = GameTexts.FindText("str_tournament_watch_round").ToString();
		JoinTournamentText = GameTexts.FindText("str_tournament_join_tournament").ToString();
		BetText = GameTexts.FindText("str_bet").ToString();
		AcceptText = GameTexts.FindText("str_accept").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		TournamentWinnerTitle = GameTexts.FindText("str_tournament_winner_title").ToString();
		BetTitleText = GameTexts.FindText("str_wager").ToString();
		GameTexts.SetVariable("MAX_AMOUNT", Tournament.GetMaximumBet());
		GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		BetDescriptionText = GameTexts.FindText("str_tournament_bet_description").ToString();
		TournamentPrizeText = GameTexts.FindText("str_tournament_prize").ToString();
		PrizeItemName = Tournament.TournamentGame.Prize.Name.ToString();
		MBTextManager.SetTextVariable("SETTLEMENT_NAME", Tournament.Settlement.Name);
		TournamentTitle = GameTexts.FindText("str_tournament").ToString();
		CurrentWagerText = GameTexts.FindText("str_tournament_current_wager").ToString();
		SkipAllRoundsHint.HintText = new TextObject("{=GaOE4bdd}Skip All Rounds");
		_round1?.RefreshValues();
		_round2?.RefreshValues();
		_round3?.RefreshValues();
		_round4?.RefreshValues();
		_currentMatch?.RefreshValues();
		_tournamentWinner?.RefreshValues();
	}

	public void ExecuteBet()
	{
		_thisRoundBettedAmount += WageredDenars;
		Tournament.PlaceABet(WageredDenars);
		RefreshBetProperties();
	}

	public void ExecuteJoinTournament()
	{
		if (PlayerCanJoinMatch())
		{
			Tournament.StartMatch();
			IsCurrentMatchActive = true;
			CurrentMatch.Refresh(forceRefresh: true);
			CurrentMatch.State = 3;
			DisableUI();
			IsCurrentMatchActive = true;
		}
	}

	public void ExecuteSkipRound()
	{
		if (IsTournamentIncomplete)
		{
			Tournament.SkipMatch();
		}
		Refresh();
	}

	public void ExecuteSkipAllRounds()
	{
		int num = 0;
		int num2 = Tournament.Rounds.Sum((TournamentRound r) => r.Matches.Length);
		while (!CanPlayerJoin && Tournament.CurrentRound?.CurrentMatch != null && num < num2)
		{
			ExecuteSkipRound();
			num++;
		}
	}

	public void ExecuteWatchRound()
	{
		if (!PlayerCanJoinMatch())
		{
			Tournament.StartMatch();
			IsCurrentMatchActive = true;
			CurrentMatch.Refresh(forceRefresh: true);
			CurrentMatch.State = 3;
			DisableUI();
			IsCurrentMatchActive = true;
		}
	}

	public void ExecuteLeave()
	{
		if (CurrentMatch != null)
		{
			List<TournamentMatch> list = new List<TournamentMatch>();
			for (int i = Tournament.CurrentRoundIndex; i < Tournament.Rounds.Length; i++)
			{
				list.AddRange(Tournament.Rounds[i].Matches.Where((TournamentMatch x) => x.State != TournamentMatch.MatchState.Finished));
			}
			if (list.Any((TournamentMatch x) => x.Participants.Any((TournamentParticipant y) => y.Character == CharacterObject.PlayerCharacter)))
			{
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_forfeit").ToString(), GameTexts.FindText("str_tournament_forfeit_game").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), EndTournamentMission, null), pauseGameActiveState: true);
				return;
			}
		}
		EndTournamentMission();
	}

	private void EndTournamentMission()
	{
		Tournament.EndTournamentViaLeave();
		Mission.Current.EndMission();
	}

	private void RefreshBetProperties()
	{
		TextObject textObject = new TextObject("{=L9GnQvsq}Stake: {BETTED_DENARS}");
		textObject.SetTextVariable("BETTED_DENARS", Tournament.BettedDenars);
		BettedDenarsText = textObject.ToString();
		TextObject textObject2 = new TextObject("{=xzzSaN4b}Expected: {OVERALL_EXPECTED_DENARS}");
		textObject2.SetTextVariable("OVERALL_EXPECTED_DENARS", Tournament.OverallExpectedDenars);
		OverallExpectedDenarsText = textObject2.ToString();
		TextObject textObject3 = new TextObject("{=yF5fpwNE}Total: {TOTAL}");
		textObject3.SetTextVariable("TOTAL", Tournament.PlayerDenars);
		TotalDenarsText = textObject3.ToString();
		OnPropertyChanged("IsBetButtonEnabled");
		MaximumBetValue = TaleWorlds.Library.MathF.Min(Tournament.GetMaximumBet() - _thisRoundBettedAmount, Hero.MainHero.Gold);
		GameTexts.SetVariable("NORMALIZED_EXPECTED_GOLD", (int)(Tournament.BetOdd * 100f));
		GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		BetOddsText = GameTexts.FindText("str_tournament_bet_odd").ToString();
	}

	private void OnNewRoundStarted(int prevRoundIndex, int currentRoundIndex)
	{
		_isPlayerParticipating = Tournament.IsPlayerParticipating;
		_thisRoundBettedAmount = 0;
	}

	public void Refresh()
	{
		IsCurrentMatchActive = false;
		CurrentMatch = _rounds[Tournament.CurrentRoundIndex].Matches.Find((TournamentMatchVM m) => m.IsValid && m.Match == Tournament.CurrentMatch);
		ActiveRoundIndex = Tournament.CurrentRoundIndex;
		CanPlayerJoin = PlayerCanJoinMatch();
		OnPropertyChanged("IsTournamentIncomplete");
		OnPropertyChanged("InitializationOver");
		OnPropertyChanged("IsBetButtonEnabled");
		HasPrizeItem = Tournament.TournamentGame.Prize != null && !IsOver;
	}

	private void OnTournamentEnd()
	{
		TournamentParticipantVM[] array = Round4.Matches.Last((TournamentMatchVM m) => m.IsValid).GetParticipants().ToArray();
		TournamentParticipantVM tournamentParticipantVM = array[0];
		TournamentParticipantVM tournamentParticipantVM2 = array[1];
		TournamentWinner = Round4.Matches.Last((TournamentMatchVM m) => m.IsValid).GetParticipants().First((TournamentParticipantVM p) => p.Participant == Tournament.Winner);
		TournamentWinner.Refresh();
		if (TournamentWinner.Participant.Character.IsHero)
		{
			Hero heroObject = TournamentWinner.Participant.Character.HeroObject;
			TournamentWinner.Character.ArmorColor1 = heroObject.MapFaction.Color;
			TournamentWinner.Character.ArmorColor2 = heroObject.MapFaction.Color2;
		}
		else
		{
			CultureObject culture = TournamentWinner.Participant.Character.Culture;
			TournamentWinner.Character.ArmorColor1 = culture.Color;
			TournamentWinner.Character.ArmorColor2 = culture.Color2;
		}
		IsWinnerHero = Tournament.Winner.Character.IsHero;
		if (IsWinnerHero)
		{
			WinnerBanner = new BannerImageIdentifierVM(Tournament.Winner.Character.HeroObject.ClanBanner, nineGrid: true);
		}
		if (TournamentWinner.Participant.Character.IsPlayerCharacter)
		{
			TournamentParticipantVM tournamentParticipantVM3 = ((tournamentParticipantVM == TournamentWinner) ? tournamentParticipantVM2 : tournamentParticipantVM);
			GameTexts.SetVariable("TOURNAMENT_FINAL_OPPONENT", tournamentParticipantVM3.Name);
			WinnerIntro = GameTexts.FindText("str_tournament_result_won").ToString();
			if (Tournament.TournamentGame.TournamentWinRenown > 0f)
			{
				GameTexts.SetVariable("RENOWN", Tournament.TournamentGame.TournamentWinRenown.ToString("F1"));
				BattleRewards.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_renown").ToString()));
			}
			if (Tournament.TournamentGame.TournamentWinInfluence > 0f)
			{
				float tournamentWinInfluence = Tournament.TournamentGame.TournamentWinInfluence;
				TextObject textObject = GameTexts.FindText("str_tournament_influence");
				textObject.SetTextVariable("INFLUENCE", tournamentWinInfluence.ToString("F1"));
				textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
				BattleRewards.Add(new TournamentRewardVM(textObject.ToString()));
			}
			if (Tournament.TournamentGame.Prize != null)
			{
				string content = Tournament.TournamentGame.Prize.Name.ToString();
				GameTexts.SetVariable("REWARD", content);
				BattleRewards.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_reward").ToString(), new ItemImageIdentifierVM(Tournament.TournamentGame.Prize)));
			}
			if (Tournament.OverallExpectedDenars > 0)
			{
				int overallExpectedDenars = Tournament.OverallExpectedDenars;
				TextObject textObject2 = GameTexts.FindText("str_tournament_bet");
				textObject2.SetTextVariable("BET", overallExpectedDenars);
				textObject2.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				BattleRewards.Add(new TournamentRewardVM(textObject2.ToString()));
			}
		}
		else if (tournamentParticipantVM.Participant.Character.IsPlayerCharacter || tournamentParticipantVM2.Participant.Character.IsPlayerCharacter)
		{
			TournamentParticipantVM tournamentParticipantVM4 = ((tournamentParticipantVM == TournamentWinner) ? tournamentParticipantVM : tournamentParticipantVM2);
			GameTexts.SetVariable("TOURNAMENT_FINAL_OPPONENT", tournamentParticipantVM4.Name);
			WinnerIntro = GameTexts.FindText("str_tournament_result_eliminated_at_final").ToString();
		}
		else
		{
			int num = 3;
			bool num2 = Round3.GetParticipants().Any((TournamentParticipantVM p) => p.Participant.Character.IsPlayerCharacter);
			bool flag = Round2.GetParticipants().Any((TournamentParticipantVM p) => p.Participant.Character.IsPlayerCharacter);
			bool flag2 = Round1.GetParticipants().Any((TournamentParticipantVM p) => p.Participant.Character.IsPlayerCharacter);
			if (num2)
			{
				num = 3;
			}
			else if (flag)
			{
				num = 2;
			}
			else if (flag2)
			{
				num = 1;
			}
			bool flag3 = tournamentParticipantVM == TournamentWinner;
			GameTexts.SetVariable("TOURNAMENT_FINAL_PARTICIPANT_A", flag3 ? tournamentParticipantVM.Name : tournamentParticipantVM2.Name);
			GameTexts.SetVariable("TOURNAMENT_FINAL_PARTICIPANT_B", flag3 ? tournamentParticipantVM2.Name : tournamentParticipantVM.Name);
			if (_isPlayerParticipating)
			{
				GameTexts.SetVariable("TOURNAMENT_ELIMINATED_ROUND", num.ToString());
				WinnerIntro = GameTexts.FindText("str_tournament_result_eliminated").ToString();
			}
			else
			{
				WinnerIntro = GameTexts.FindText("str_tournament_result_spectator").ToString();
			}
		}
		IsOver = true;
	}

	private bool PlayerCanJoinMatch()
	{
		if (IsTournamentIncomplete)
		{
			return Tournament.CurrentMatch.Participants.Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
		}
		return false;
	}

	public void OnAgentRemoved(Agent agent)
	{
		if (!IsCurrentMatchActive || !agent.IsHuman)
		{
			return;
		}
		TournamentParticipant participant = CurrentMatch.Match.GetParticipant(agent.Origin.UniqueSeed);
		if (participant != null)
		{
			CurrentMatch.GetParticipants().First((TournamentParticipantVM p) => p.Participant == participant).IsDead = true;
		}
	}

	public void ExecuteShowPrizeItemTooltip()
	{
		if (HasPrizeItem)
		{
			InformationManager.ShowTooltip(typeof(ItemObject), new EquipmentElement(Tournament.TournamentGame.Prize));
		}
	}

	public void ExecuteHidePrizeItemTooltip()
	{
		MBInformationManager.HideInformations();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
