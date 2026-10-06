using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyPartyInvitationPopupVM : ViewModel
{
	private bool _isEnabled;

	private float _remainingAnswerDuration;

	private float _maxAnswerDuration;

	private string _title;

	private string _message;

	private MPLobbyPlayerBaseVM _invitingPlayer;

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
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			if (value != _message)
			{
				_message = value;
				OnPropertyChangedWithValue(value, "Message");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerBaseVM InvitingPlayer
	{
		get
		{
			return _invitingPlayer;
		}
		set
		{
			if (value != _invitingPlayer)
			{
				_invitingPlayer = value;
				OnPropertyChangedWithValue(value, "InvitingPlayer");
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

	public MPLobbyPartyInvitationPopupVM()
	{
		RefreshValues();
		MaxAnswerDuration = 60f;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = new TextObject("{=QDNcl3DH}Party Invitation").ToString();
		Message = new TextObject("{=AaAcmalE}You've been invited to join a party by").ToString();
	}

	public void OpenWith(PlayerId invitingPlayerID)
	{
		RemainingAnswerDuration = MaxAnswerDuration;
		InvitingPlayer = new MPLobbyPlayerBaseVM(invitingPlayerID);
		IsEnabled = true;
	}

	public void Close()
	{
		if (IsEnabled)
		{
			ExecuteDecline();
		}
	}

	public void OnTick(float dt)
	{
		if (IsEnabled)
		{
			RemainingAnswerDuration -= dt;
			if (RemainingAnswerDuration <= 0f)
			{
				ExecuteDecline();
			}
		}
	}

	private void ExecuteAccept()
	{
		IsEnabled = false;
		NetworkMain.GameClient.AcceptPartyInvitation();
	}

	private void ExecuteDecline()
	{
		IsEnabled = false;
		NetworkMain.GameClient.DeclinePartyInvitation();
	}
}
