using System;
using System.Linq;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyRankProgressInformationVM : ViewModel
{
	private MPLobbyPlayerBaseVM _basePlayer;

	private readonly Func<string> _getExitText;

	private TextObject _ratingRemainingTitleTextObject = new TextObject("{=7gQkFJqA}{RATING} points remaining to next rank");

	private TextObject _finalRankTextObject = new TextObject("{=6mZymVS8}You are at the final rank");

	private TextObject _evaluationTextObject = new TextObject("{=Ise5gWw3}{PLAYED_GAMES} / {TOTAL_GAMES} Evaluation matches played");

	private bool _isEnabled;

	private bool _isAtFinalRank;

	private bool _isEvaluating;

	private string _titleText;

	private string _clickToCloseText;

	private string _currentRankTitleText;

	private string _ratingRemainingTitleText;

	private string _currentRankID;

	private string _previousRankID;

	private string _nextRankID;

	private int _currentRating;

	private int _nextRankRating;

	private int _ratingRatio;

	private MPLobbyPlayerBaseVM _player;

	private MBBindingList<StringPairItemVM> _allRanks;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAtFinalRank
	{
		get
		{
			return _isAtFinalRank;
		}
		set
		{
			if (value != _isAtFinalRank)
			{
				_isAtFinalRank = value;
				OnPropertyChangedWithValue(value, "IsAtFinalRank");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEvaluating
	{
		get
		{
			return _isEvaluating;
		}
		set
		{
			if (value != _isEvaluating)
			{
				_isEvaluating = value;
				OnPropertyChangedWithValue(value, "IsEvaluating");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ClickToCloseText
	{
		get
		{
			return _clickToCloseText;
		}
		set
		{
			if (value != _clickToCloseText)
			{
				_clickToCloseText = value;
				OnPropertyChangedWithValue(value, "ClickToCloseText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentRankTitleText
	{
		get
		{
			return _currentRankTitleText;
		}
		set
		{
			if (value != _currentRankTitleText)
			{
				_currentRankTitleText = value;
				OnPropertyChangedWithValue(value, "CurrentRankTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string RatingRemainingTitleText
	{
		get
		{
			return _ratingRemainingTitleText;
		}
		set
		{
			if (value != _ratingRemainingTitleText)
			{
				_ratingRemainingTitleText = value;
				OnPropertyChangedWithValue(value, "RatingRemainingTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentRankID
	{
		get
		{
			return _currentRankID;
		}
		set
		{
			if (value != _currentRankID)
			{
				_currentRankID = value;
				OnPropertyChangedWithValue(value, "CurrentRankID");
			}
		}
	}

	[DataSourceProperty]
	public string PreviousRankID
	{
		get
		{
			return _previousRankID;
		}
		set
		{
			if (value != _previousRankID)
			{
				_previousRankID = value;
				OnPropertyChangedWithValue(value, "PreviousRankID");
			}
		}
	}

	[DataSourceProperty]
	public string NextRankID
	{
		get
		{
			return _nextRankID;
		}
		set
		{
			if (value != _nextRankID)
			{
				_nextRankID = value;
				OnPropertyChangedWithValue(value, "NextRankID");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentRating
	{
		get
		{
			return _currentRating;
		}
		set
		{
			if (value != _currentRating)
			{
				_currentRating = value;
				OnPropertyChangedWithValue(value, "CurrentRating");
			}
		}
	}

	[DataSourceProperty]
	public int NextRankRating
	{
		get
		{
			return _nextRankRating;
		}
		set
		{
			if (value != _nextRankRating)
			{
				_nextRankRating = value;
				OnPropertyChangedWithValue(value, "NextRankRating");
			}
		}
	}

	[DataSourceProperty]
	public int RatingRatio
	{
		get
		{
			return _ratingRatio;
		}
		set
		{
			if (value != _ratingRatio)
			{
				_ratingRatio = value;
				OnPropertyChangedWithValue(value, "RatingRatio");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerBaseVM Player
	{
		get
		{
			return _player;
		}
		set
		{
			if (value != _player)
			{
				_player = value;
				OnPropertyChangedWithValue(value, "Player");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> AllRanks
	{
		get
		{
			return _allRanks;
		}
		set
		{
			if (value != _allRanks)
			{
				_allRanks = value;
				OnPropertyChangedWithValue(value, "AllRanks");
			}
		}
	}

	public MPLobbyRankProgressInformationVM(Func<string> getExitText)
	{
		_getExitText = getExitText;
		AllRanks = new MBBindingList<StringPairItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=XEGaQB2G}Rank Progression").ToString();
		AllRanks.Clear();
		string[] rankIds = Ranks.RankIds;
		foreach (string rank in rankIds)
		{
			AllRanks.Add(new StringPairItemVM(rank, string.Empty, new BasicTooltipViewModel(() => MPLobbyVM.GetLocalizedRankName(rank))));
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ExecuteClosePopup();
	}

	public void OpenWith(MPLobbyPlayerBaseVM player)
	{
		IsEnabled = true;
		ClickToCloseText = _getExitText?.Invoke();
		if (player.RankInfo == null)
		{
			Debug.FailedAssert("Can't request rank progression information of another player.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Profile\\MPLobbyRankProgressInformationVM.cs", "OpenWith", 54);
			return;
		}
		_basePlayer = player;
		Player = new MPLobbyPlayerBaseVM(player.ProvidedID);
		Player.UpdateRating(OnRatingReceived);
	}

	private void OnRatingReceived()
	{
		Player?.RefreshSelectableGameTypes(isRankedOnly: true, RefreshRankInfo, _basePlayer.GameTypes.FirstOrDefault((MPLobbyGameTypeVM gt) => gt.IsSelected)?.GameTypeID);
	}

	private void RefreshRankInfo(string gameType)
	{
		GameTypeRankInfo gameTypeRankInfo = Player.RankInfo?.FirstOrDefault((GameTypeRankInfo r) => r.GameType == gameType);
		if (gameTypeRankInfo != null && gameTypeRankInfo.RankBarInfo != null)
		{
			RankBarInfo rankBarInfo = gameTypeRankInfo.RankBarInfo;
			CurrentRankID = rankBarInfo.RankId;
			CurrentRankTitleText = MPLobbyVM.GetLocalizedRankName(CurrentRankID);
			CurrentRating = rankBarInfo.Rating;
			NextRankRating = CurrentRating + rankBarInfo.RatingToNextRank;
			AllRanks.ApplyActionOnAllItems(delegate(StringPairItemVM r)
			{
				r.Value = " ";
			});
			StringPairItemVM stringPairItemVM = AllRanks.FirstOrDefault((StringPairItemVM r) => r.Definition == CurrentRankID);
			if (stringPairItemVM != null)
			{
				stringPairItemVM.Value = new TextObject("{=sWnQva5O}Current Rank").ToString();
			}
			IsAtFinalRank = string.IsNullOrEmpty(rankBarInfo.NextRankId);
			IsEvaluating = rankBarInfo.IsEvaluating;
			if (rankBarInfo.IsEvaluating)
			{
				RatingRatio = TaleWorlds.Library.MathF.Floor((float)rankBarInfo.EvaluationMatchesPlayed / (float)rankBarInfo.TotalEvaluationMatchesRequired * 100f);
				NextRankID = string.Empty;
				PreviousRankID = string.Empty;
				CurrentRating = rankBarInfo.EvaluationMatchesPlayed;
				NextRankRating = rankBarInfo.TotalEvaluationMatchesRequired;
				_evaluationTextObject.SetTextVariable("PLAYED_GAMES", rankBarInfo.EvaluationMatchesPlayed);
				_evaluationTextObject.SetTextVariable("TOTAL_GAMES", rankBarInfo.TotalEvaluationMatchesRequired);
				RatingRemainingTitleText = _evaluationTextObject.ToString();
			}
			else if (IsAtFinalRank)
			{
				RatingRatio = 100;
				NextRankID = string.Empty;
				PreviousRankID = string.Empty;
				RatingRemainingTitleText = _finalRankTextObject.ToString();
			}
			else
			{
				RatingRatio = TaleWorlds.Library.MathF.Floor(rankBarInfo.ProgressPercentage);
				NextRankID = rankBarInfo.NextRankId;
				PreviousRankID = rankBarInfo.PreviousRankId;
				_ratingRemainingTitleTextObject.SetTextVariable("RATING", rankBarInfo.RatingToNextRank);
				RatingRemainingTitleText = _ratingRemainingTitleTextObject.ToString();
			}
		}
		else
		{
			IsEnabled = false;
		}
	}

	public void ExecuteClosePopup()
	{
		Player = null;
		IsEnabled = false;
	}
}
