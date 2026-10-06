using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.Diamond.Ranked;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.AfterBattle;

public class MPAfterBattlePopupVM : ViewModel
{
	private enum AfterBattleState
	{
		None,
		GeneralProgression,
		LevelUp,
		RatingChange,
		RankChange
	}

	private AfterBattleState _currentState;

	private bool _hasLeveledUp;

	private int _oldExperience;

	private int _newExperience;

	private List<string> _earnedBadgeIDs;

	private int _lootGained;

	private bool _hasRatingChanged;

	private bool _hasRankChanged;

	private bool _hasFinishedEvaluation;

	private RankBarInfo _oldRankBarInfo;

	private RankBarInfo _newRankBarInfo;

	private string _battleResultsTitleText;

	private string _levelUpTitleText;

	private string _rankProgressTitleText;

	private string _promotedTitleText;

	private string _demotedTitleText;

	private string _evaluationFinishedTitleText;

	private TextObject _pointsGainedTextObj = new TextObject("{=EFU3uo0y}You've gained {POINTS} points");

	private TextObject _pointsLostTextObj = new TextObject("{=oMYz0PvL}You've lost {POINTS} points");

	private readonly Func<string> _getExitText;

	private bool _isEnabled;

	private bool _isShowingGeneralProgression;

	private bool _isShowingNewLevel;

	private bool _isShowingRankProgression;

	private bool _isShowingNewRank;

	private bool _hasLostRating;

	private string _titleText;

	private string _levelText;

	private string _experienceText;

	private string _clickToContinueText;

	private string _reachedLevelText;

	private string _pointsText;

	private string _pointChangeText;

	private string _oldRankID;

	private string _newRankID;

	private string _oldRankName;

	private string _newRankName;

	private int _initialRatio;

	private int _finalRatio;

	private int _numOfLevelUps;

	private int _gainedExperience;

	private int _levelsExperienceRequirment;

	private int _currentLevel;

	private int _nextLevel;

	private int _shownRating;

	private MBBindingList<MPAfterBattleRewardItemVM> _rewardsEarned;

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
	public bool IsShowingGeneralProgression
	{
		get
		{
			return _isShowingGeneralProgression;
		}
		set
		{
			if (value != _isShowingGeneralProgression)
			{
				_isShowingGeneralProgression = value;
				OnPropertyChangedWithValue(value, "IsShowingGeneralProgression");
			}
		}
	}

	[DataSourceProperty]
	public bool IsShowingNewLevel
	{
		get
		{
			return _isShowingNewLevel;
		}
		set
		{
			if (value != _isShowingNewLevel)
			{
				_isShowingNewLevel = value;
				OnPropertyChangedWithValue(value, "IsShowingNewLevel");
			}
		}
	}

	[DataSourceProperty]
	public bool IsShowingRankProgression
	{
		get
		{
			return _isShowingRankProgression;
		}
		set
		{
			if (value != _isShowingRankProgression)
			{
				_isShowingRankProgression = value;
				OnPropertyChangedWithValue(value, "IsShowingRankProgression");
			}
		}
	}

	[DataSourceProperty]
	public bool IsShowingNewRank
	{
		get
		{
			return _isShowingNewRank;
		}
		set
		{
			if (value != _isShowingNewRank)
			{
				_isShowingNewRank = value;
				OnPropertyChangedWithValue(value, "IsShowingNewRank");
			}
		}
	}

	[DataSourceProperty]
	public bool HasLostRating
	{
		get
		{
			return _hasLostRating;
		}
		set
		{
			if (value != _hasLostRating)
			{
				_hasLostRating = value;
				OnPropertyChangedWithValue(value, "HasLostRating");
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
	public string LevelText
	{
		get
		{
			return _levelText;
		}
		set
		{
			if (value != _levelText)
			{
				_levelText = value;
				OnPropertyChangedWithValue(value, "LevelText");
			}
		}
	}

	[DataSourceProperty]
	public string ExperienceText
	{
		get
		{
			return _experienceText;
		}
		set
		{
			if (value != _experienceText)
			{
				_experienceText = value;
				OnPropertyChangedWithValue(value, "ExperienceText");
			}
		}
	}

	[DataSourceProperty]
	public string ClickToContinueText
	{
		get
		{
			return _clickToContinueText;
		}
		set
		{
			if (value != _clickToContinueText)
			{
				_clickToContinueText = value;
				OnPropertyChangedWithValue(value, "ClickToContinueText");
			}
		}
	}

	[DataSourceProperty]
	public string ReachedLevelText
	{
		get
		{
			return _reachedLevelText;
		}
		set
		{
			if (value != _reachedLevelText)
			{
				_reachedLevelText = value;
				OnPropertyChangedWithValue(value, "ReachedLevelText");
			}
		}
	}

	[DataSourceProperty]
	public string PointsText
	{
		get
		{
			return _pointsText;
		}
		set
		{
			if (value != _pointsText)
			{
				_pointsText = value;
				OnPropertyChangedWithValue(value, "PointsText");
			}
		}
	}

	[DataSourceProperty]
	public string PointChangedText
	{
		get
		{
			return _pointChangeText;
		}
		set
		{
			if (value != _pointChangeText)
			{
				_pointChangeText = value;
				OnPropertyChangedWithValue(value, "PointChangedText");
			}
		}
	}

	[DataSourceProperty]
	public string OldRankID
	{
		get
		{
			return _oldRankID;
		}
		set
		{
			if (value != _oldRankID)
			{
				_oldRankID = value;
				OnPropertyChangedWithValue(value, "OldRankID");
			}
		}
	}

	[DataSourceProperty]
	public string NewRankID
	{
		get
		{
			return _newRankID;
		}
		set
		{
			if (value != _newRankID)
			{
				_newRankID = value;
				OnPropertyChangedWithValue(value, "NewRankID");
			}
		}
	}

	[DataSourceProperty]
	public string OldRankName
	{
		get
		{
			return _oldRankName;
		}
		set
		{
			if (value != _oldRankName)
			{
				_oldRankName = value;
				OnPropertyChangedWithValue(value, "OldRankName");
			}
		}
	}

	[DataSourceProperty]
	public string NewRankName
	{
		get
		{
			return _newRankName;
		}
		set
		{
			if (value != _newRankName)
			{
				_newRankName = value;
				OnPropertyChangedWithValue(value, "NewRankName");
			}
		}
	}

	[DataSourceProperty]
	public int FinalRatio
	{
		get
		{
			return _finalRatio;
		}
		set
		{
			if (value != _finalRatio)
			{
				_finalRatio = value;
				OnPropertyChangedWithValue(value, "FinalRatio");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfLevelUps
	{
		get
		{
			return _numOfLevelUps;
		}
		set
		{
			if (value != _numOfLevelUps)
			{
				_numOfLevelUps = value;
				OnPropertyChangedWithValue(value, "NumOfLevelUps");
			}
		}
	}

	[DataSourceProperty]
	public int InitialRatio
	{
		get
		{
			return _initialRatio;
		}
		set
		{
			if (value != _initialRatio)
			{
				_initialRatio = value;
				OnPropertyChangedWithValue(value, "InitialRatio");
			}
		}
	}

	[DataSourceProperty]
	public int GainedExperience
	{
		get
		{
			return _gainedExperience;
		}
		set
		{
			if (value != _gainedExperience)
			{
				_gainedExperience = value;
				OnPropertyChangedWithValue(value, "GainedExperience");
			}
		}
	}

	[DataSourceProperty]
	public int LevelsExperienceRequirment
	{
		get
		{
			return _levelsExperienceRequirment;
		}
		set
		{
			if (value != _levelsExperienceRequirment)
			{
				_levelsExperienceRequirment = value;
				OnPropertyChangedWithValue(value, "LevelsExperienceRequirment");
			}
		}
	}

	[DataSourceProperty]
	public int NextLevel
	{
		get
		{
			return _nextLevel;
		}
		set
		{
			if (value != _nextLevel)
			{
				_nextLevel = value;
				OnPropertyChangedWithValue(value, "NextLevel");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentLevel
	{
		get
		{
			return _currentLevel;
		}
		set
		{
			if (value != _currentLevel)
			{
				_currentLevel = value;
				OnPropertyChangedWithValue(value, "CurrentLevel");
			}
		}
	}

	[DataSourceProperty]
	public int ShownRating
	{
		get
		{
			return _shownRating;
		}
		set
		{
			if (value != _shownRating)
			{
				_shownRating = value;
				OnPropertyChangedWithValue(value, "ShownRating");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPAfterBattleRewardItemVM> RewardsEarned
	{
		get
		{
			return _rewardsEarned;
		}
		set
		{
			if (value != _rewardsEarned)
			{
				_rewardsEarned = value;
				OnPropertyChangedWithValue(value, "RewardsEarned");
			}
		}
	}

	public MPAfterBattlePopupVM(Func<string> getExitText)
	{
		_getExitText = getExitText;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_battleResultsTitleText = new TextObject("{=pguhTmXw}Battle Results").ToString();
		_levelUpTitleText = new TextObject("{=0tUYng4e}Leveled Up!").ToString();
		_rankProgressTitleText = new TextObject("{=XEGaQB2G}Rank Progression").ToString();
		_promotedTitleText = new TextObject("{=bn0v5ST0}Promoted!").ToString();
		_demotedTitleText = new TextObject("{=HUndnpNw}Demoted!").ToString();
		_evaluationFinishedTitleText = new TextObject("{=2KZLf51A}Evaluation Matches Finished").ToString();
		LevelText = GameTexts.FindText("str_level").ToString();
		ExperienceText = new TextObject("{=SwSaXwQg}exp").ToString();
		PointsText = new TextObject("{=4dRTWSN3}Points").ToString();
	}

	public void OpenWith(int oldExperience, int newExperience, List<string> badgesEarned, int lootGained, RankBarInfo oldRankBarInfo, RankBarInfo newRankBarInfo)
	{
		ClickToContinueText = _getExitText?.Invoke();
		_oldExperience = oldExperience;
		_newExperience = newExperience;
		_earnedBadgeIDs = badgesEarned;
		_lootGained = lootGained;
		_oldRankBarInfo = oldRankBarInfo;
		_newRankBarInfo = newRankBarInfo;
		_hasRatingChanged = oldRankBarInfo != null && newRankBarInfo != null && !oldRankBarInfo.IsEvaluating && !newRankBarInfo.IsEvaluating;
		_hasRankChanged = _hasRatingChanged && oldRankBarInfo.RankId != newRankBarInfo.RankId;
		_hasFinishedEvaluation = _oldRankBarInfo != null && _newRankBarInfo != null && _oldRankBarInfo.IsEvaluating && !_newRankBarInfo.IsEvaluating;
		AdvanceState();
		IsEnabled = true;
	}

	private void AdvanceState()
	{
		HideInfo();
		switch (_currentState)
		{
		case AfterBattleState.None:
			_currentState = AfterBattleState.GeneralProgression;
			ShowGeneralProgression();
			break;
		case AfterBattleState.GeneralProgression:
			if (_hasLeveledUp)
			{
				_currentState = AfterBattleState.LevelUp;
				ShowLevelUp();
			}
			else if (_hasRatingChanged)
			{
				_currentState = AfterBattleState.RatingChange;
				ShowRankProgression();
			}
			else if (_hasFinishedEvaluation)
			{
				_currentState = AfterBattleState.RankChange;
				ShowRankChange();
			}
			else
			{
				Disable();
			}
			break;
		case AfterBattleState.LevelUp:
			if (_hasRatingChanged)
			{
				_currentState = AfterBattleState.RatingChange;
				ShowRankProgression();
			}
			else if (_hasFinishedEvaluation)
			{
				_currentState = AfterBattleState.RankChange;
				ShowRankChange();
			}
			else
			{
				Disable();
			}
			break;
		case AfterBattleState.RatingChange:
			if (_hasRankChanged)
			{
				_currentState = AfterBattleState.RankChange;
				ShowRankChange();
			}
			else
			{
				Disable();
			}
			break;
		case AfterBattleState.RankChange:
			Disable();
			break;
		}
	}

	private void ShowGeneralProgression()
	{
		TitleText = _battleResultsTitleText;
		InitialRatio = 0;
		FinalRatio = 0;
		NumOfLevelUps = 0;
		PlayerDataExperience playerDataExperience = new PlayerDataExperience(_oldExperience);
		PlayerDataExperience playerDataExperience2 = new PlayerDataExperience(_newExperience);
		GainedExperience = _newExperience - _oldExperience;
		CurrentLevel = playerDataExperience.Level;
		NextLevel = CurrentLevel + 1;
		InitialRatio = (int)((float)playerDataExperience.ExperienceInCurrentLevel / (float)(playerDataExperience.ExperienceToNextLevel + playerDataExperience.ExperienceInCurrentLevel) * 100f);
		FinalRatio = (int)((float)playerDataExperience2.ExperienceInCurrentLevel / (float)(playerDataExperience2.ExperienceToNextLevel + playerDataExperience2.ExperienceInCurrentLevel) * 100f);
		NumOfLevelUps = playerDataExperience2.Level - playerDataExperience.Level;
		_hasLeveledUp = NumOfLevelUps > 0;
		HasLostRating = GainedExperience < 0;
		float num = (float)NumOfLevelUps + (float)FinalRatio / 100f;
		LevelsExperienceRequirment = (int)((float)_newExperience / num);
		RewardsEarned = new MBBindingList<MPAfterBattleRewardItemVM>();
		foreach (string earnedBadgeID in _earnedBadgeIDs)
		{
			Badge byId = BadgeManager.GetById(earnedBadgeID);
			if (byId != null)
			{
				RewardsEarned.Add(new MPAfterBattleBadgeRewardItemVM(byId));
			}
		}
		if (_lootGained > 0)
		{
			int num2 = _lootGained - _earnedBadgeIDs.Count * Parameters.LootRewardPerBadgeEarned;
			int additionalLootFromBadges = _lootGained - num2;
			RewardsEarned.Add(new MPAfterBattleLootRewardItemVM(num2, additionalLootFromBadges));
		}
		IsShowingGeneralProgression = true;
	}

	private void ShowLevelUp()
	{
		TitleText = _levelUpTitleText;
		int level = new PlayerDataExperience(_newExperience).Level;
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_level"));
		GameTexts.SetVariable("STR2", level);
		ReachedLevelText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		SoundEvent.PlaySound2D("event:/ui/multiplayer/levelup");
		IsShowingNewLevel = true;
	}

	private void ShowRankProgression()
	{
		TitleText = _rankProgressTitleText;
		OldRankID = _oldRankBarInfo.RankId;
		NewRankID = _newRankBarInfo.RankId;
		OldRankName = MPLobbyVM.GetLocalizedRankName(OldRankID);
		NewRankName = MPLobbyVM.GetLocalizedRankName(NewRankID);
		HasLostRating = _oldRankBarInfo.Rating > _newRankBarInfo.Rating;
		ShownRating = _newRankBarInfo.Rating;
		InitialRatio = (int)_oldRankBarInfo.ProgressPercentage;
		FinalRatio = (int)_newRankBarInfo.ProgressPercentage;
		NumOfLevelUps = Ranks.RankIds.IndexOf(NewRankID) - Ranks.RankIds.IndexOf(OldRankID);
		if (HasLostRating)
		{
			_pointsLostTextObj.SetTextVariable("POINTS", _oldRankBarInfo.Rating - _newRankBarInfo.Rating);
			PointChangedText = _pointsLostTextObj.ToString();
		}
		else
		{
			_pointsGainedTextObj.SetTextVariable("POINTS", _newRankBarInfo.Rating - _oldRankBarInfo.Rating);
			PointChangedText = _pointsGainedTextObj.ToString();
		}
		IsShowingRankProgression = true;
	}

	private void ShowRankChange()
	{
		if (OldRankID != string.Empty && OldRankID != null)
		{
			bool flag = Ranks.RankIds.IndexOf(OldRankID) < Ranks.RankIds.IndexOf(NewRankID);
			TitleText = (flag ? _promotedTitleText : _demotedTitleText);
			IsShowingNewRank = true;
		}
		else if (_hasFinishedEvaluation)
		{
			OldRankID = string.Empty;
			NewRankID = _newRankBarInfo.RankId;
			OldRankName = MPLobbyVM.GetLocalizedRankName(OldRankID);
			NewRankName = MPLobbyVM.GetLocalizedRankName(NewRankID);
			TitleText = _evaluationFinishedTitleText;
			IsShowingNewRank = true;
		}
	}

	private void HideInfo()
	{
		IsShowingGeneralProgression = false;
		IsShowingNewLevel = false;
		IsShowingRankProgression = false;
		IsShowingNewRank = false;
	}

	private void Disable()
	{
		HideInfo();
		ShownRating = 0;
		_currentState = AfterBattleState.None;
		IsEnabled = false;
	}

	public void ExecuteClose()
	{
		if (IsEnabled)
		{
			AdvanceState();
		}
	}
}
