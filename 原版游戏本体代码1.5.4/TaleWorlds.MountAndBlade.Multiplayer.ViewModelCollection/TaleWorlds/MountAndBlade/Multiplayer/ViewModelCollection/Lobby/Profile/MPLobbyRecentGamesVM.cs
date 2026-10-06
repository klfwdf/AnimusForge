using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyRecentGamesVM : ViewModel
{
	private MatchHistoryData _currentMatchOfTheActivePlayer;

	private bool _isEnabled;

	private bool _gotItems;

	private bool _isPlayerActionsActive;

	private string _recentGamesText;

	private string _noRecentGamesFoundText;

	private string _closeText;

	private MBBindingList<StringPairItemWithActionVM> _playerActions;

	private MBBindingList<MPLobbyRecentGameItemVM> _games;

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
	public bool GotItems
	{
		get
		{
			return _gotItems;
		}
		set
		{
			if (value != _gotItems)
			{
				_gotItems = value;
				OnPropertyChangedWithValue(value, "GotItems");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerActionsActive
	{
		get
		{
			return _isPlayerActionsActive;
		}
		set
		{
			if (value != _isPlayerActionsActive)
			{
				_isPlayerActionsActive = value;
				OnPropertyChangedWithValue(value, "IsPlayerActionsActive");
			}
		}
	}

	[DataSourceProperty]
	public string RecentGamesText
	{
		get
		{
			return _recentGamesText;
		}
		set
		{
			if (value != _recentGamesText)
			{
				_recentGamesText = value;
				OnPropertyChangedWithValue(value, "RecentGamesText");
			}
		}
	}

	[DataSourceProperty]
	public string NoRecentGamesFoundText
	{
		get
		{
			return _noRecentGamesFoundText;
		}
		set
		{
			if (value != _noRecentGamesFoundText)
			{
				_noRecentGamesFoundText = value;
				OnPropertyChangedWithValue(value, "NoRecentGamesFoundText");
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemWithActionVM> PlayerActions
	{
		get
		{
			return _playerActions;
		}
		set
		{
			if (value != _playerActions)
			{
				_playerActions = value;
				OnPropertyChangedWithValue(value, "PlayerActions");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyRecentGameItemVM> Games
	{
		get
		{
			return _games;
		}
		set
		{
			if (value != _games)
			{
				_games = value;
				OnPropertyChangedWithValue(value, "Games");
			}
		}
	}

	public MPLobbyRecentGamesVM()
	{
		_games = new MBBindingList<MPLobbyRecentGameItemVM>();
		PlayerActions = new MBBindingList<StringPairItemWithActionVM>();
		NoRecentGamesFoundText = new TextObject("{=TzYWE9tA}No Recent Games Found").ToString();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RecentGamesText = new TextObject("{=NJolh9ye}Recent Games").ToString();
		CloseText = GameTexts.FindText("str_close").ToString();
		Games.ApplyActionOnAllItems(delegate(MPLobbyRecentGameItemVM x)
		{
			x.RefreshValues();
		});
	}

	public void RefreshData(MBReadOnlyList<MatchHistoryData> matches)
	{
		Games.Clear();
		if (matches != null)
		{
			foreach (MatchHistoryData item in matches.OrderByDescending((MatchHistoryData m) => m.MatchDate))
			{
				if (item != null)
				{
					MPLobbyRecentGameItemVM mPLobbyRecentGameItemVM = new MPLobbyRecentGameItemVM(ActivatePlayerActions);
					mPLobbyRecentGameItemVM.FillFrom(item);
					Games.Add(mPLobbyRecentGameItemVM);
				}
			}
		}
		GotItems = matches.Count > 0;
	}

	public void ActivatePlayerActions(MPLobbyRecentGamePlayerItemVM playerVM)
	{
		PlayerActions.Clear();
		_currentMatchOfTheActivePlayer = playerVM.MatchOfThePlayer;
		if (playerVM.ProvidedID != NetworkMain.GameClient.PlayerID)
		{
			StringPairItemWithActionVM stringPairItemWithActionVM = new StringPairItemWithActionVM(ExecuteReport, GameTexts.FindText("str_mp_scoreboard_context_report").ToString(), "Report", playerVM);
			if (MultiplayerReportPlayerManager.IsPlayerReportedOverLimit(playerVM.ProvidedID))
			{
				stringPairItemWithActionVM.IsEnabled = false;
				stringPairItemWithActionVM.Hint.HintText = new TextObject("{=klkYFik9}You've already reported this player.");
			}
			PlayerActions.Add(stringPairItemWithActionVM);
			bool flag = false;
			FriendInfo[] friendInfos = NetworkMain.GameClient.FriendInfos;
			for (int i = 0; i < friendInfos.Length; i++)
			{
				if (friendInfos[i].Id == playerVM.ProvidedID)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				PlayerActions.Add(new StringPairItemWithActionVM(ExecuteRequestFriendship, new TextObject("{=UwkpJq9N}Add As Friend").ToString(), "RequestFriendship", playerVM));
			}
			else
			{
				PlayerActions.Add(new StringPairItemWithActionVM(ExecuteTerminateFriendship, new TextObject("{=2YIVRuRa}Remove From Friends").ToString(), "TerminateFriendship", playerVM));
			}
			MultiplayerPlayerContextMenuHelper.AddLobbyViewProfileOptions(playerVM, PlayerActions);
		}
		IsPlayerActionsActive = false;
		IsPlayerActionsActive = PlayerActions.Count > 0;
	}

	private void ExecuteRequestFriendship(object playerObj)
	{
		MPLobbyRecentGamePlayerItemVM mPLobbyRecentGamePlayerItemVM = playerObj as MPLobbyRecentGamePlayerItemVM;
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(mPLobbyRecentGamePlayerItemVM.ProvidedID);
		NetworkMain.GameClient.AddFriend(mPLobbyRecentGamePlayerItemVM.ProvidedID, dontUseNameForUnknownPlayer);
	}

	private void ExecuteTerminateFriendship(object memberObj)
	{
		MPLobbyRecentGamePlayerItemVM mPLobbyRecentGamePlayerItemVM = memberObj as MPLobbyRecentGamePlayerItemVM;
		NetworkMain.GameClient.RemoveFriend(mPLobbyRecentGamePlayerItemVM.ProvidedID);
	}

	private void ExecuteReport(object playerObj)
	{
		MPLobbyRecentGamePlayerItemVM mPLobbyRecentGamePlayerItemVM = playerObj as MPLobbyRecentGamePlayerItemVM;
		MultiplayerReportPlayerManager.RequestReportPlayer(_currentMatchOfTheActivePlayer.MatchId, mPLobbyRecentGamePlayerItemVM.ProvidedID, mPLobbyRecentGamePlayerItemVM.Name, isRequestedFromMission: false);
	}

	public void ExecuteOpenPopup()
	{
		IsEnabled = true;
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}

	public void OnFriendListUpdated(bool forceUpdate = false)
	{
		foreach (MPLobbyRecentGameItemVM game in Games)
		{
			game.OnFriendListUpdated(forceUpdate);
		}
	}
}
