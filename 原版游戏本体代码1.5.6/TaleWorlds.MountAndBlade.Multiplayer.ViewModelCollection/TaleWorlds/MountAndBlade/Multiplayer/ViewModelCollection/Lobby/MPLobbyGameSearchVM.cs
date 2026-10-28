using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyGameSearchVM : ViewModel
{
	private float _waitingTimeElapsed;

	private string _shortTimeTextFormat = "mm\\:ss";

	private string _longTimeTextFormat = "hh\\:mm\\:ss";

	private bool _isEnabled;

	private bool _canCancelSearch;

	private bool _canEnterPracticeBattle;

	private bool _showStats;

	private string _titleText;

	private string _gameTypesText;

	private string _cancelText;

	private string _practiceText;

	private string _averageWaitingTime;

	private string _averageWaitingTimeDescription;

	private string _currentWaitingTime;

	private string _currentWaitingTimeDescription;

	public MPCustomGameVM.CustomGameMode CustomGameMode { get; private set; }

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
	public bool CanEnterPracticeBattle
	{
		get
		{
			return _canEnterPracticeBattle;
		}
		set
		{
			if (value != _canEnterPracticeBattle)
			{
				_canEnterPracticeBattle = value;
				OnPropertyChangedWithValue(value, "CanEnterPracticeBattle");
			}
		}
	}

	[DataSourceProperty]
	public bool CanCancelSearch
	{
		get
		{
			return _canCancelSearch;
		}
		set
		{
			if (value != _canCancelSearch)
			{
				_canCancelSearch = value;
				OnPropertyChangedWithValue(value, "CanCancelSearch");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowStats
	{
		get
		{
			return _showStats;
		}
		set
		{
			if (value != _showStats)
			{
				_showStats = value;
				OnPropertyChangedWithValue(value, "ShowStats");
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
	public string GameTypesText
	{
		get
		{
			return _gameTypesText;
		}
		set
		{
			if (value != _gameTypesText)
			{
				_gameTypesText = value;
				OnPropertyChangedWithValue(value, "GameTypesText");
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
	public string PracticeText
	{
		get
		{
			return _practiceText;
		}
		set
		{
			if (value != _practiceText)
			{
				_practiceText = value;
				OnPropertyChangedWithValue(value, "PracticeText");
			}
		}
	}

	[DataSourceProperty]
	public string AverageWaitingTime
	{
		get
		{
			return _averageWaitingTime;
		}
		set
		{
			if (value != _averageWaitingTime)
			{
				_averageWaitingTime = value;
				OnPropertyChangedWithValue(value, "AverageWaitingTime");
			}
		}
	}

	[DataSourceProperty]
	public string AverageWaitingTimeDescription
	{
		get
		{
			return _averageWaitingTimeDescription;
		}
		set
		{
			if (value != _averageWaitingTimeDescription)
			{
				_averageWaitingTimeDescription = value;
				OnPropertyChangedWithValue(value, "AverageWaitingTimeDescription");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentWaitingTime
	{
		get
		{
			return _currentWaitingTime;
		}
		set
		{
			if (value != _currentWaitingTime)
			{
				_currentWaitingTime = value;
				OnPropertyChangedWithValue(value, "CurrentWaitingTime");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentWaitingTimeDescription
	{
		get
		{
			return _currentWaitingTimeDescription;
		}
		set
		{
			if (value != _currentWaitingTimeDescription)
			{
				_currentWaitingTimeDescription = value;
				OnPropertyChangedWithValue(value, "CurrentWaitingTimeDescription");
			}
		}
	}

	public MPLobbyGameSearchVM()
	{
		GameTypesText = new TextObject("{=cK5DE88I}N/A").ToString();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (CustomGameMode == MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			TitleText = new TextObject("{=dkPL25g9}Waiting for an opponent team").ToString();
			GameTypesText = "";
			ShowStats = false;
		}
		else
		{
			TitleText = new TextObject("{=FD7EQDmW}Looking for game").ToString();
			ShowStats = true;
		}
		GameTexts.SetVariable("STR1", "");
		GameTexts.SetVariable("STR2", new TextObject("{=mFMPj9zg}Searching for matches"));
		CurrentWaitingTimeDescription = GameTexts.FindText("str_STR1_space_STR2").ToString();
		GameTexts.SetVariable("STR2", new TextObject("{=18yFEEIL}Estimated wait time"));
		AverageWaitingTimeDescription = GameTexts.FindText("str_STR1_space_STR2").ToString();
		CancelText = new TextObject("{=3CpNUnVl}Cancel").ToString();
		PracticeText = new TextObject("{=cjBboOaH}Practice while waiting").ToString();
	}

	public void OnTick(float dt)
	{
		if (IsEnabled)
		{
			_waitingTimeElapsed += dt;
			CurrentWaitingTime = SecondsToString(_waitingTimeElapsed);
		}
	}

	public void SetEnabled(bool enabled)
	{
		IsEnabled = enabled;
		if (enabled)
		{
			CanCancelSearch = true;
			CanEnterPracticeBattle = false;
			_waitingTimeElapsed = 0f;
		}
		RefreshValues();
		if (CustomGameMode != MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			UpdateCanCancel();
		}
	}

	public void UpdateData(MatchmakingWaitTimeStats matchmakingWaitTimeStats, string[] gameTypeInfo)
	{
		ShowStats = true;
		CustomGameMode = MPCustomGameVM.CustomGameMode.CustomServer;
		TitleText = new TextObject("{=FD7EQDmW}Looking for game").ToString();
		int num = 0;
		string[] array = GameTypesText.Replace(" ", "").Split(new char[1] { ',' });
		string[] array2 = array;
		foreach (string gameType in array2)
		{
			WaitTimeStatType statType = WaitTimeStatType.SoloDuo;
			if (NetworkMain.GameClient.PlayersInParty.Count >= 3 && NetworkMain.GameClient.PlayersInParty.Count <= 5)
			{
				statType = WaitTimeStatType.Party;
			}
			else if (NetworkMain.GameClient.IsPartyFull)
			{
				statType = WaitTimeStatType.Premade;
			}
			num += matchmakingWaitTimeStats.GetWaitTime(MultiplayerMain.GetUserCurrentRegion(), gameType, statType);
		}
		AverageWaitingTime = SecondsToString(num / array.Length);
		if (gameTypeInfo != null)
		{
			GameTypesText = MPLobbyVM.GetLocalizedGameTypesString(gameTypeInfo);
		}
	}

	public void UpdatePremadeGameData()
	{
		ShowStats = false;
		CustomGameMode = MPCustomGameVM.CustomGameMode.PremadeGame;
		TitleText = new TextObject("{=dkPL25g9}Waiting for an opponent team").ToString();
		GameTypesText = "";
	}

	public void OnJoinPremadeGameRequestSuccessful()
	{
		TitleText = new TextObject("{=5coyTZOI}Game is starting!").ToString();
	}

	public void OnRequestedToCancelSearchBattle()
	{
		CanCancelSearch = false;
		CanEnterPracticeBattle = false;
	}

	public void UpdateCanCancel()
	{
		CanCancelSearch = !NetworkMain.GameClient.IsInParty || NetworkMain.GameClient.IsPartyLeader;
		CanEnterPracticeBattle = false;
	}

	private void ExecuteCancel()
	{
		if (CustomGameMode != MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			NetworkMain.GameClient.CancelFindGame();
		}
		else
		{
			NetworkMain.GameClient.CancelCreatingPremadeGame();
		}
	}

	private string SecondsToString(float seconds)
	{
		return TimeSpan.FromSeconds(seconds).ToString((seconds >= 3600f) ? _longTimeTextFormat : _shortTimeTextFormat);
	}
}
