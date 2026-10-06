using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyPlayerProfileVM : ViewModel
{
	private readonly LobbyState _lobbyState;

	private PlayerId _activePlayerID;

	private PlayerData _activePlayerData;

	private bool _isStatsReceived;

	private bool _isRatingReceived;

	private bool _isEnabled;

	private bool _isDataLoading;

	private string _statsTitleText;

	private string _closeText;

	private MPLobbyPlayerBaseVM _player;

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
	public bool IsDataLoading
	{
		get
		{
			return _isDataLoading;
		}
		set
		{
			if (value != _isDataLoading)
			{
				_isDataLoading = value;
				OnPropertyChangedWithValue(value, "IsDataLoading");
			}
		}
	}

	[DataSourceProperty]
	public string StatsTitleText
	{
		get
		{
			return _statsTitleText;
		}
		set
		{
			if (value != _statsTitleText)
			{
				_statsTitleText = value;
				OnPropertyChangedWithValue(value, "StatsTitleText");
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

	public MPLobbyPlayerProfileVM(LobbyState lobbyState)
	{
		_lobbyState = lobbyState;
		Player = new MPLobbyPlayerBaseVM(PlayerId.Empty);
		MPLobbyPlayerBaseVM player = Player;
		player.OnRankInfoChanged = (Action<string>)Delegate.Combine(player.OnRankInfoChanged, new Action<string>(OnPlayerRankInfoChanged));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CloseText = GameTexts.FindText("str_close").ToString();
		StatsTitleText = new TextObject("{=GmU1to3Y}Statistics").ToString();
		Player?.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		MPLobbyPlayerBaseVM player = Player;
		player.OnRankInfoChanged = (Action<string>)Delegate.Remove(player.OnRankInfoChanged, new Action<string>(OnPlayerRankInfoChanged));
	}

	public async void SetPlayerID(PlayerId playerID)
	{
		IsEnabled = true;
		IsDataLoading = true;
		_activePlayerID = playerID;
		_activePlayerData = await NetworkMain.GameClient.GetAnotherPlayerData(playerID);
		if (_activePlayerData != null)
		{
			PlatformServices.Instance?.CheckPrivilege(Privilege.Chat, displayResolveUI: true, delegate(bool result)
			{
				if (!result)
				{
					PlatformServices.Instance.ShowRestrictedInformation();
				}
			});
			PlatformServices.Instance.CheckPermissionWithUser(Permission.ViewUserGeneratedContent, _activePlayerID, delegate(bool hasPermission)
			{
				Player.IsBannerlordIDSupported = hasPermission;
			});
			await _lobbyState.UpdateHasUserGeneratedContentPrivilege(showResolveUI: true);
			Player.UpdateWith(_activePlayerData);
			if (NetworkMain.GameClient.SupportedFeatures.SupportsFeatures(Features.Clan))
			{
				Player.UpdateClanInfo();
			}
			Player.RefreshCharacterVisual();
			Player.UpdateStats(OnStatsReceived);
			Player.UpdateRating(OnRatingReceived);
		}
		else
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=bhQiSzOU}Profile is not available").ToString(), new TextObject("{=goQ0MZhr}This player does not have an active Bannerlord player profile.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), null, ExecuteClosePopup, null));
		}
	}

	public void OpenWith(PlayerId playerID)
	{
		PlatformServices.Instance.CheckPermissionWithUser(Permission.ViewUserGeneratedContent, playerID, delegate(bool hasBannerlordIDPrivilege)
		{
			Player.IsBannerlordIDSupported = hasBannerlordIDPrivilege;
			SetPlayerID(playerID);
		});
	}

	public void UpdatePlayerData(PlayerData playerData, bool updateStatistics = false, bool updateRating = false)
	{
		IsDataLoading = true;
		_activePlayerID = playerData.PlayerId;
		Player?.UpdateWith(playerData);
		if (updateStatistics)
		{
			Player.UpdateStats(OnStatsReceived);
		}
		if (updateRating)
		{
			Player.UpdateRating(OnRatingReceived);
		}
		Player.UpdateExperienceData();
		Player.RefreshSelectableGameTypes(isRankedOnly: false, Player.UpdateDisplayedRankInfo);
		IsDataLoading = false;
	}

	private void OnPlayerRankInfoChanged(string gameType)
	{
		Player.FilterStatsForGameMode(gameType);
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
		IsDataLoading = false;
	}

	public void OnClanInfoChanged()
	{
		if (Player.ProvidedID == NetworkMain.GameClient.PlayerID)
		{
			Player.UpdateClanInfo();
		}
	}

	private void OnStatsReceived()
	{
		_isStatsReceived = true;
		CheckAndUpdateStatsAndRatingData();
	}

	private void OnRatingReceived()
	{
		_isRatingReceived = true;
		CheckAndUpdateStatsAndRatingData();
	}

	public void OnPlayerNameUpdated(string playerName)
	{
		Player?.UpdateNameAndAvatar(forceUpdate: true);
	}

	private void CheckAndUpdateStatsAndRatingData()
	{
		if (_isRatingReceived && _isStatsReceived)
		{
			Player.UpdateExperienceData();
			Player.RefreshSelectableGameTypes(isRankedOnly: false, Player.UpdateDisplayedRankInfo);
			IsDataLoading = false;
		}
	}
}
