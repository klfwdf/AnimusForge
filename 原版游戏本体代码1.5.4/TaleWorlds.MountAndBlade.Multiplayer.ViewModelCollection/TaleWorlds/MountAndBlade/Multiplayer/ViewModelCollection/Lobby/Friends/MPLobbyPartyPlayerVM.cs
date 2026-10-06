using System;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyPartyPlayerVM : MPLobbyPlayerBaseVM
{
	private Action<MPLobbyPartyPlayerVM> _onActivatePlayerActions;

	private bool _isWaitingConfirmation;

	private bool _isPartyLeader;

	[DataSourceProperty]
	public bool IsWaitingConfirmation
	{
		get
		{
			return _isWaitingConfirmation;
		}
		set
		{
			if (value != _isWaitingConfirmation)
			{
				_isWaitingConfirmation = value;
				OnPropertyChangedWithValue(value, "IsWaitingConfirmation");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPartyLeader
	{
		get
		{
			return _isPartyLeader;
		}
		set
		{
			if (value != _isPartyLeader)
			{
				_isPartyLeader = value;
				OnPropertyChangedWithValue(value, "IsPartyLeader");
			}
		}
	}

	public MPLobbyPartyPlayerVM(PlayerId id, Action<MPLobbyPartyPlayerVM> onActivatePlayerActions)
		: base(id)
	{
		_onActivatePlayerActions = onActivatePlayerActions;
	}

	private void ExecuteActivatePlayerActions()
	{
		_onActivatePlayerActions(this);
	}
}
