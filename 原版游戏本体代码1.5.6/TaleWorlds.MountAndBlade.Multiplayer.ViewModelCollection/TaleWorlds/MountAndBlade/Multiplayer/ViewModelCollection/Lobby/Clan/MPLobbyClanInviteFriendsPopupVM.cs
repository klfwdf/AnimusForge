using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanInviteFriendsPopupVM : ViewModel
{
	private Func<MBBindingList<MPLobbyPlayerBaseVM>> _getAllFriends;

	private bool _isEnabled;

	private string _titleText;

	private string _inviteText;

	private string _closeText;

	private string _selectPlayersText;

	private MBBindingList<MPLobbyPlayerBaseVM> _onlineFriends;

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
	public string InviteText
	{
		get
		{
			return _inviteText;
		}
		set
		{
			if (value != _inviteText)
			{
				_inviteText = value;
				OnPropertyChanged("InviteText");
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
				OnPropertyChanged("CloseText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectPlayersText
	{
		get
		{
			return _selectPlayersText;
		}
		set
		{
			if (value != _selectPlayersText)
			{
				_selectPlayersText = value;
				OnPropertyChanged("SelectPlayersText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyPlayerBaseVM> OnlineFriends
	{
		get
		{
			return _onlineFriends;
		}
		set
		{
			if (value != _onlineFriends)
			{
				_onlineFriends = value;
				OnPropertyChanged("OnlineFriends");
			}
		}
	}

	public MPLobbyClanInviteFriendsPopupVM(Func<MBBindingList<MPLobbyPlayerBaseVM>> getAllFriends)
	{
		_getAllFriends = getAllFriends;
		OnlineFriends = new MBBindingList<MPLobbyPlayerBaseVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=v4hVLpap}Invite Players to Clan").ToString();
		InviteText = new TextObject("{=aZnS9ECC}Invite").ToString();
		CloseText = new TextObject("{=yQtzabbe}Close").ToString();
		SelectPlayersText = new TextObject("{=ZAejS7WF}Select players to invite to your clan").ToString();
	}

	public void Open()
	{
		if (NetworkMain.GameClient.ClanID == Guid.Empty || NetworkMain.GameClient.ClanInfo == null)
		{
			return;
		}
		IEnumerable<PlayerId> source = NetworkMain.GameClient.ClanInfo.Players.Select((ClanPlayer c) => c.PlayerId);
		OnlineFriends.Clear();
		foreach (MPLobbyPlayerBaseVM onlineFriend in _getAllFriends())
		{
			if (!source.Contains(onlineFriend.ProvidedID) && !OnlineFriends.Any((MPLobbyPlayerBaseVM f) => f.ProvidedID == onlineFriend.ProvidedID))
			{
				OnlineFriends.Add(onlineFriend);
			}
		}
		IsEnabled = true;
	}

	private void ExecuteSendInvitation()
	{
		foreach (MPLobbyPlayerBaseVM onlineFriend in OnlineFriends)
		{
			if (onlineFriend.IsSelected)
			{
				onlineFriend.ExecuteInviteToClan();
			}
		}
		ExecuteClosePopup();
	}

	private void ResetSelection()
	{
		foreach (MPLobbyPlayerBaseVM onlineFriend in OnlineFriends)
		{
			onlineFriend.IsSelected = false;
		}
	}

	public void ExecuteClosePopup()
	{
		if (IsEnabled)
		{
			ResetSelection();
			IsEnabled = false;
		}
	}
}
