using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyRecentGameItemVM : ViewModel
{
	private readonly Action<MPLobbyRecentGamePlayerItemVM> _onActivatePlayerActions;

	public MBBindingList<MPLobbyRecentGamePlayerItemVM> _playersA;

	public MBBindingList<MPLobbyRecentGamePlayerItemVM> _playersB;

	private string _lastSeenPlayersText;

	private string _factionNameA;

	private string _factionNameB;

	private string _cultureA;

	private string _cultureB;

	private string _scoreA;

	private string _scoreB;

	private string _gameMode;

	private string _date;

	private string _seperator;

	private int _playerResultIndex;

	private int _matchResultIndex;

	private HintViewModel _abandonedHint;

	private HintViewModel _wonHint;

	private HintViewModel _lostHint;

	public MatchHistoryData MatchInfo { get; private set; }

	[DataSourceProperty]
	public string LastSeenPlayersText
	{
		get
		{
			return _lastSeenPlayersText;
		}
		set
		{
			if (value != _lastSeenPlayersText)
			{
				_lastSeenPlayersText = value;
				OnPropertyChangedWithValue(value, "LastSeenPlayersText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyRecentGamePlayerItemVM> PlayersA
	{
		get
		{
			return _playersA;
		}
		set
		{
			if (value != _playersA)
			{
				_playersA = value;
				OnPropertyChangedWithValue(value, "PlayersA");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyRecentGamePlayerItemVM> PlayersB
	{
		get
		{
			return _playersB;
		}
		set
		{
			if (value != _playersB)
			{
				_playersB = value;
				OnPropertyChangedWithValue(value, "PlayersB");
			}
		}
	}

	[DataSourceProperty]
	public string CultureA
	{
		get
		{
			return _cultureA;
		}
		set
		{
			if (value != _cultureA)
			{
				_cultureA = value;
				OnPropertyChangedWithValue(value, "CultureA");
			}
		}
	}

	[DataSourceProperty]
	public string CultureB
	{
		get
		{
			return _cultureB;
		}
		set
		{
			if (value != _cultureB)
			{
				_cultureB = value;
				OnPropertyChangedWithValue(value, "CultureB");
			}
		}
	}

	[DataSourceProperty]
	public string FactionNameA
	{
		get
		{
			return _factionNameA;
		}
		set
		{
			if (value != _factionNameA)
			{
				_factionNameA = value;
				OnPropertyChangedWithValue(value, "FactionNameA");
			}
		}
	}

	[DataSourceProperty]
	public string FactionNameB
	{
		get
		{
			return _factionNameB;
		}
		set
		{
			if (value != _factionNameB)
			{
				_factionNameB = value;
				OnPropertyChangedWithValue(value, "FactionNameB");
			}
		}
	}

	[DataSourceProperty]
	public string ScoreA
	{
		get
		{
			return _scoreA;
		}
		set
		{
			if (value != _scoreA)
			{
				_scoreA = value;
				OnPropertyChangedWithValue(value, "ScoreA");
			}
		}
	}

	[DataSourceProperty]
	public string ScoreB
	{
		get
		{
			return _scoreB;
		}
		set
		{
			if (value != _scoreB)
			{
				_scoreB = value;
				OnPropertyChangedWithValue(value, "ScoreB");
			}
		}
	}

	[DataSourceProperty]
	public string GameMode
	{
		get
		{
			return _gameMode;
		}
		set
		{
			if (value != _gameMode)
			{
				_gameMode = value;
				OnPropertyChangedWithValue(value, "GameMode");
			}
		}
	}

	[DataSourceProperty]
	public string Date
	{
		get
		{
			return _date;
		}
		set
		{
			if (value != _date)
			{
				_date = value;
				OnPropertyChangedWithValue(value, "Date");
			}
		}
	}

	[DataSourceProperty]
	public string Seperator
	{
		get
		{
			return _seperator;
		}
		set
		{
			if (value != _seperator)
			{
				_seperator = value;
				OnPropertyChangedWithValue(value, "Seperator");
			}
		}
	}

	[DataSourceProperty]
	public int MatchResultIndex
	{
		get
		{
			return _matchResultIndex;
		}
		set
		{
			if (value != _matchResultIndex)
			{
				_matchResultIndex = value;
				OnPropertyChangedWithValue(value, "MatchResultIndex");
			}
		}
	}

	[DataSourceProperty]
	public int PlayerResultIndex
	{
		get
		{
			return _playerResultIndex;
		}
		set
		{
			if (value != _playerResultIndex)
			{
				_playerResultIndex = value;
				OnPropertyChangedWithValue(value, "PlayerResultIndex");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AbandonedHint
	{
		get
		{
			return _abandonedHint;
		}
		set
		{
			if (value != _abandonedHint)
			{
				_abandonedHint = value;
				OnPropertyChangedWithValue(value, "AbandonedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel WonHint
	{
		get
		{
			return _wonHint;
		}
		set
		{
			if (value != _wonHint)
			{
				_wonHint = value;
				OnPropertyChangedWithValue(value, "WonHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LostHint
	{
		get
		{
			return _lostHint;
		}
		set
		{
			if (value != _lostHint)
			{
				_lostHint = value;
				OnPropertyChangedWithValue(value, "LostHint");
			}
		}
	}

	public MPLobbyRecentGameItemVM(Action<MPLobbyRecentGamePlayerItemVM> onActivatePlayerActions)
	{
		_onActivatePlayerActions = onActivatePlayerActions;
		PlayersA = new MBBindingList<MPLobbyRecentGamePlayerItemVM>();
		PlayersB = new MBBindingList<MPLobbyRecentGamePlayerItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		LastSeenPlayersText = new TextObject("{=NJolh9ye}Recent Games").ToString();
		Seperator = new TextObject("{=4NaOKslb}-").ToString();
		PlayersA.ApplyActionOnAllItems(delegate(MPLobbyRecentGamePlayerItemVM x)
		{
			x.RefreshValues();
		});
		PlayersB.ApplyActionOnAllItems(delegate(MPLobbyRecentGamePlayerItemVM x)
		{
			x.RefreshValues();
		});
		AbandonedHint = new HintViewModel(new TextObject("{=eQPSEUml}Abandoned"));
		WonHint = new HintViewModel(new TextObject("{=IS4SifJG}Won"));
		LostHint = new HintViewModel(new TextObject("{=b2aqL7T2}Lost"));
		if (MatchInfo != null)
		{
			FillFrom(MatchInfo);
		}
	}

	public void FillFrom(MatchHistoryData match)
	{
		MatchInfo = match;
		PlayersA.Clear();
		PlayersB.Clear();
		GameMode = GameTexts.FindText("str_multiplayer_official_game_type_name", match.GameType).ToString();
		PlayerResultIndex = ((match.WinnerTeam != -1) ? 1 : 0);
		CultureA = match.Faction1;
		FactionNameA = GetLocalizedCultureNameFromStringID(match.Faction1);
		ScoreA = match.AttackerScore.ToString();
		CultureB = match.Faction2;
		FactionNameB = GetLocalizedCultureNameFromStringID(match.Faction2);
		ScoreB = match.DefenderScore.ToString();
		MatchResultIndex = ((match.DefenderScore != match.AttackerScore) ? ((match.DefenderScore <= match.AttackerScore) ? 1 : 2) : 0);
		Date = LocalizedTextManager.GetDateFormattedByLanguage(BannerlordConfig.Language, match.MatchDate);
		foreach (PlayerInfo player in match.Players)
		{
			PlayerId playerId = PlayerId.FromString(player.PlayerId);
			if (!MultiplayerPlayerHelper.IsBlocked(playerId))
			{
				MPLobbyRecentGamePlayerItemVM item = new MPLobbyRecentGamePlayerItemVM(playerId, match, _onActivatePlayerActions);
				if (match.WinnerTeam != -1 && playerId == NetworkMain.GameClient.PlayerID)
				{
					PlayerResultIndex = ((player.TeamNo == match.WinnerTeam) ? 1 : 2);
				}
				if (player.TeamNo == 1)
				{
					PlayersA.Add(item);
				}
				else
				{
					PlayersB.Add(item);
				}
			}
		}
	}

	public void OnFriendListUpdated(bool forceUpdate = false)
	{
		foreach (MPLobbyRecentGamePlayerItemVM item in PlayersA)
		{
			item.UpdateNameAndAvatar(forceUpdate);
		}
		foreach (MPLobbyRecentGamePlayerItemVM item2 in PlayersB)
		{
			item2.UpdateNameAndAvatar(forceUpdate);
		}
	}

	private static string GetLocalizedCultureNameFromStringID(string cultureID)
	{
		switch (cultureID)
		{
		case "sturgia":
			return new TextObject("{=PjO7oY16}Sturgia").ToString();
		case "vlandia":
			return new TextObject("{=FjwRsf1C}Vlandia").ToString();
		case "battania":
			return new TextObject("{=0B27RrYJ}Battania").ToString();
		case "empire":
			return new TextObject("{=empirefaction}Empire").ToString();
		case "khuzait":
			return new TextObject("{=sZLd6VHi}Khuzait").ToString();
		case "aserai":
			return new TextObject("{=aseraifaction}Aserai").ToString();
		default:
			Debug.FailedAssert("Unidentified culture id: " + cultureID, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Profile\\MPLobbyRecentGameItemVM.cs", "GetLocalizedCultureNameFromStringID", 384);
			return "";
		}
	}
}
