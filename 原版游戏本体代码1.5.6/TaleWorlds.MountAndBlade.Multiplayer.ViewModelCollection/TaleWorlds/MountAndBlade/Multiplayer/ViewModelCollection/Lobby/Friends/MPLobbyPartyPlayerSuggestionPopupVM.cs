using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyPartyPlayerSuggestionPopupVM : ViewModel
{
	public class PlayerPartySuggestionData
	{
		public PlayerId PlayerId { get; private set; }

		public string PlayerName { get; private set; }

		public PlayerId SuggestingPlayerId { get; private set; }

		public string SuggestingPlayerName { get; private set; }

		public PlayerPartySuggestionData(PlayerId playerId, string playerName, PlayerId suggestingPlayerId, string suggestingPlayerName)
		{
			PlayerId = playerId;
			PlayerName = playerName;
			SuggestingPlayerId = suggestingPlayerId;
			SuggestingPlayerName = suggestingPlayerName;
		}
	}

	private PlayerId _suggestedPlayerId;

	private bool _isEnabled;

	private string _titleText;

	private string _doYouWantToInviteText;

	private string _playerSuggestedText;

	private MPLobbyPlayerBaseVM _suggestedPlayer;

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
				OnPropertyChanged("IsEnabled");
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
				OnPropertyChanged("TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DoYouWantToInviteText
	{
		get
		{
			return _doYouWantToInviteText;
		}
		set
		{
			if (value != _doYouWantToInviteText)
			{
				_doYouWantToInviteText = value;
				OnPropertyChanged("DoYouWantToInviteText");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerSuggestedText
	{
		get
		{
			return _playerSuggestedText;
		}
		set
		{
			if (value != _playerSuggestedText)
			{
				_playerSuggestedText = value;
				OnPropertyChanged("PlayerSuggestedText");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerBaseVM SuggestedPlayer
	{
		get
		{
			return _suggestedPlayer;
		}
		set
		{
			if (value != _suggestedPlayer)
			{
				_suggestedPlayer = value;
				OnPropertyChanged("SuggestedPlayer");
			}
		}
	}

	public MPLobbyPartyPlayerSuggestionPopupVM()
	{
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=q2Y7aHSF}Invite Suggestion").ToString();
		DoYouWantToInviteText = new TextObject("{=VFqoa6vD}Do you want to invite this player to your party?").ToString();
	}

	public void OpenWith(PlayerPartySuggestionData data)
	{
		_suggestedPlayerId = data.PlayerId;
		SuggestedPlayer = new MPLobbyPlayerBaseVM(data.PlayerId);
		TextObject textObject = new TextObject("{=C7OHivNl}Your friend <a style=\"Strong\"><b>{PLAYER_NAME}</b></a> wants you to invite the player below to your party.");
		GameTexts.SetVariable("PLAYER_NAME", data.SuggestingPlayerName);
		PlayerSuggestedText = textObject.ToString();
		IsEnabled = true;
	}

	public void Close()
	{
		IsEnabled = false;
	}

	private void ExecuteAcceptSuggestion()
	{
		PlatformServices.Instance.CheckPrivilege(Privilege.Communication, displayResolveUI: true, delegate(bool result)
		{
			if (result)
			{
				PlatformServices.Instance.CheckPermissionWithUser(Permission.PlayMultiplayer, _suggestedPlayerId, async delegate(bool permissionResult)
				{
					if (permissionResult)
					{
						if (PlatformServices.Instance.UsePlatformInvitationService(_suggestedPlayerId))
						{
							await NetworkMain.GameClient.InviteToPlatformSession(_suggestedPlayerId);
						}
						else
						{
							bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(_suggestedPlayerId);
							NetworkMain.GameClient.InviteToParty(_suggestedPlayerId, dontUseNameForUnknownPlayer);
						}
					}
					Close();
				});
			}
			else
			{
				Close();
			}
		});
	}

	private void ExecuteDeclineSuggestion()
	{
		Close();
	}
}
