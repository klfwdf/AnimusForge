using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyPartyJoinRequestPopupVM : ViewModel
{
	private PlayerId _viaPlayerId;

	private bool _isEnabled;

	private float _remainingAnswerDuration;

	private float _maxAnswerDuration;

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
	public string AcceptJoinRequestText
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
				OnPropertyChanged("AcceptJoinRequestText");
			}
		}
	}

	[DataSourceProperty]
	public string JoiningPlayerText
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
				OnPropertyChanged("JoiningPlayerText");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerBaseVM JoiningPlayer
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
				OnPropertyChanged("JoiningPlayer");
			}
		}
	}

	[DataSourceProperty]
	public float RemainingAnswerDuration
	{
		get
		{
			return _remainingAnswerDuration;
		}
		set
		{
			if (value != _remainingAnswerDuration)
			{
				_remainingAnswerDuration = value;
				OnPropertyChangedWithValue(value, "RemainingAnswerDuration");
			}
		}
	}

	[DataSourceProperty]
	public float MaxAnswerDuration
	{
		get
		{
			return _maxAnswerDuration;
		}
		set
		{
			if (value != _maxAnswerDuration)
			{
				_maxAnswerDuration = value;
				OnPropertyChangedWithValue(value, "MaxAnswerDuration");
			}
		}
	}

	public MPLobbyPartyJoinRequestPopupVM()
	{
		RefreshValues();
		MaxAnswerDuration = 30f;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=re37GzKI}Party Join Request").ToString();
		AcceptJoinRequestText = new TextObject("{=Ogr2N5bx}Accept request to join to your party?").ToString();
	}

	public void OpenWith(PlayerId joiningPlayer, PlayerId viaPlayerId, string viaPlayerName)
	{
		_viaPlayerId = viaPlayerId;
		JoiningPlayer = new MPLobbyPlayerBaseVM(joiningPlayer);
		RemainingAnswerDuration = MaxAnswerDuration;
		if (viaPlayerId == NetworkMain.GameClient.PlayerID)
		{
			TextObject textObject = new TextObject("{=BcEN71ts}Player wants to join your party.");
			JoiningPlayerText = textObject.ToString();
		}
		else
		{
			TextObject textObject = new TextObject("{=q3uBjUyB}Player wants to join your party through your party member <a style=\"Strong\"><b>{PLAYER_NAME}</b></a>.");
			GameTexts.SetVariable("PLAYER_NAME", viaPlayerName);
			JoiningPlayerText = textObject.ToString();
		}
		IsEnabled = true;
	}

	public void OpenWithNewParty(PlayerId joiningPlayer)
	{
		JoiningPlayer = new MPLobbyPlayerBaseVM(joiningPlayer);
		JoiningPlayerText = "";
		RemainingAnswerDuration = MaxAnswerDuration;
		IsEnabled = true;
	}

	public void Close()
	{
		if (IsEnabled)
		{
			ExecuteDeclineJoinRequest();
		}
	}

	public void OnTick(float dt)
	{
		if (IsEnabled)
		{
			RemainingAnswerDuration -= dt;
			if (RemainingAnswerDuration <= 0f)
			{
				ExecuteDeclineJoinRequest();
			}
		}
	}

	private void ExecuteAcceptJoinRequest()
	{
		PlatformServices.Instance.CheckPrivilege(Privilege.Communication, displayResolveUI: true, delegate(bool result)
		{
			if (result)
			{
				PlatformServices.Instance.CheckPermissionWithUser(Permission.PlayMultiplayer, JoiningPlayer.ProvidedID, delegate(bool permissionResult)
				{
					if (permissionResult)
					{
						NetworkMain.GameClient.AcceptPartyJoinRequest(JoiningPlayer.ProvidedID);
					}
					else
					{
						NetworkMain.GameClient.DeclinePartyJoinRequest(JoiningPlayer.ProvidedID, PartyJoinDeclineReason.NoPlatformPermission);
					}
					IsEnabled = false;
				});
			}
			else
			{
				NetworkMain.GameClient.DeclinePartyJoinRequest(JoiningPlayer.ProvidedID, PartyJoinDeclineReason.NoPlatformPermission);
				IsEnabled = false;
			}
		});
	}

	private void ExecuteDeclineJoinRequest()
	{
		NetworkMain.GameClient.DeclinePartyJoinRequest(JoiningPlayer.ProvidedID, PartyJoinDeclineReason.DeclinedByLeader);
		IsEnabled = false;
	}
}
