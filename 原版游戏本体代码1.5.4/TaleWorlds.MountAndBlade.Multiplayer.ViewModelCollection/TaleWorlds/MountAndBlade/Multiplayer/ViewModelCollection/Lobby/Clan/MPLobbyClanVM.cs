using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanVM : ViewModel
{
	public enum ClanSubPages
	{
		Overview,
		Roster
	}

	private ClanSubPages _currentSubPage;

	private List<LobbyNotification> _activeNotifications;

	private bool _isEnabled;

	private int _selectedSubPageIndex;

	private string _closeText;

	private MPLobbyClanOverviewVM _clanOverview;

	private MPLobbyClanRosterVM _clanRoster;

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
				OnIsEnabledChanged();
			}
		}
	}

	[DataSourceProperty]
	public int SelectedSubPageIndex
	{
		get
		{
			return _selectedSubPageIndex;
		}
		set
		{
			if (value != _selectedSubPageIndex)
			{
				_selectedSubPageIndex = value;
				OnPropertyChanged("SelectedSubPageIndex");
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
	public MPLobbyClanOverviewVM ClanOverview
	{
		get
		{
			return _clanOverview;
		}
		set
		{
			if (value != _clanOverview)
			{
				_clanOverview = value;
				OnPropertyChanged("ClanOverview");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyClanRosterVM ClanRoster
	{
		get
		{
			return _clanRoster;
		}
		set
		{
			if (value != _clanRoster)
			{
				_clanRoster = value;
				OnPropertyChanged("ClanRoster");
			}
		}
	}

	public MPLobbyClanVM(Action openInviteClanMemberPopup)
	{
		ClanOverview = new MPLobbyClanOverviewVM(openInviteClanMemberPopup);
		ClanRoster = new MPLobbyClanRosterVM();
		_activeNotifications = new List<LobbyNotification>();
		TrySetClanSubPage(ClanSubPages.Overview);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CloseText = GameTexts.FindText("str_close").ToString();
		ClanOverview.RefreshValues();
		ClanRoster.RefreshValues();
	}

	private void OnIsEnabledChanged()
	{
		if (!IsEnabled)
		{
			return;
		}
		TrySetClanSubPage(ClanSubPages.Overview);
		foreach (LobbyNotification activeNotification in _activeNotifications)
		{
			NetworkMain.GameClient.MarkNotificationAsRead(activeNotification.Id);
		}
		_activeNotifications.Clear();
	}

	public async void OnClanInfoChanged()
	{
		ClanHomeInfo clanHomeInfo = NetworkMain.GameClient.ClanHomeInfo;
		if (clanHomeInfo == null)
		{
			Debug.FailedAssert("Retrieved clan home info is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Clan\\MPLobbyClanVM.cs", "OnClanInfoChanged", 65);
			return;
		}
		await ClanOverview.RefreshClanInformation(clanHomeInfo);
		ClanRoster.RefreshClanInformation(clanHomeInfo);
		if (!clanHomeInfo.IsInClan)
		{
			ExecuteClosePopup();
		}
	}

	private void ExecuteChangeEnabledSubPage(int subpageIndex)
	{
		TrySetClanSubPage((ClanSubPages)subpageIndex);
	}

	public async void TrySetClanSubPage(ClanSubPages newPage)
	{
		ClanOverview.IsSelected = false;
		ClanRoster.IsSelected = false;
		_currentSubPage = newPage;
		SelectedSubPageIndex = (int)newPage;
		switch (newPage)
		{
		case ClanSubPages.Overview:
			ClanOverview.IsSelected = true;
			await ClanOverview.RefreshClanInformation(NetworkMain.GameClient.ClanHomeInfo);
			break;
		case ClanSubPages.Roster:
			ClanRoster.IsSelected = true;
			ClanRoster.RefreshClanInformation(NetworkMain.GameClient.ClanHomeInfo);
			break;
		}
	}

	public void OnNotificationReceived(LobbyNotification notification)
	{
		if (IsEnabled)
		{
			NetworkMain.GameClient.MarkNotificationAsRead(notification.Id);
		}
		else
		{
			_activeNotifications.Add(notification);
		}
	}

	public void OnPlayerNameUpdated(string playerName)
	{
		ClanRoster?.OnPlayerNameUpdated(playerName);
	}

	public void ExecuteOpenPopup()
	{
		IsEnabled = true;
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}
}
