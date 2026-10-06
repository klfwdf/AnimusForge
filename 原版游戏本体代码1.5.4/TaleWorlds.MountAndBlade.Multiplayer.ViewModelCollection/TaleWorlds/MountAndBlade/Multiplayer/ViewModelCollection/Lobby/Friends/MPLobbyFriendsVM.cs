using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyFriendsVM : ViewModel
{
	private const string _inviteFailedSoundEvent = "event:/ui/notification/quest_update";

	private List<LobbyNotification> _activeNotifications;

	private int _activeServiceIndex;

	private bool _isEnabled;

	private bool _isListEnabled = true;

	private bool _isPartyAvailable;

	private bool _isPartyFull;

	private bool _isPlayerActionsActive;

	private bool _isInParty;

	private MPLobbyPartyPlayerVM _player;

	private MBBindingList<MPLobbyPartyPlayerVM> _partyFriends;

	private MBBindingList<StringPairItemWithActionVM> _playerActions;

	private string _titleText;

	private string _inGameText;

	private string _onlineText;

	private string _offlineText;

	private int _totalOnlineFriendCount;

	private int _notificationCount;

	private bool _hasNotification;

	private HintViewModel _friendListHint;

	private MBBindingList<MPLobbyFriendServiceVM> _friendServices;

	private MPLobbyFriendServiceVM _activeService;

	private InputKeyItemVM _toggleInputKey;

	private PlayerId? _partyLeaderId => NetworkMain.GameClient.PlayersInParty.SingleOrDefault((PartyPlayerInLobbyClient p) => p.IsPartyLeader)?.PlayerId;

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
				IsEnabledUpdated();
			}
		}
	}

	[DataSourceProperty]
	public bool IsListEnabled
	{
		get
		{
			return _isListEnabled;
		}
		set
		{
			if (value != _isListEnabled)
			{
				_isListEnabled = value;
				OnPropertyChangedWithValue(value, "IsListEnabled");
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
	public bool IsPartyAvailable
	{
		get
		{
			return _isPartyAvailable;
		}
		set
		{
			if (value != _isPartyAvailable)
			{
				_isPartyAvailable = value;
				OnPropertyChangedWithValue(value, "IsPartyAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPartyFull
	{
		get
		{
			return _isPartyFull;
		}
		set
		{
			if (value != _isPartyFull)
			{
				_isPartyFull = value;
				OnPropertyChangedWithValue(value, "IsPartyFull");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInParty
	{
		get
		{
			return _isInParty;
		}
		set
		{
			if (value != _isInParty)
			{
				_isInParty = value;
				OnPropertyChangedWithValue(value, "IsInParty");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPartyPlayerVM Player
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

	[DataSourceProperty]
	public MBBindingList<MPLobbyPartyPlayerVM> PartyFriends
	{
		get
		{
			return _partyFriends;
		}
		set
		{
			if (value != _partyFriends)
			{
				_partyFriends = value;
				OnPropertyChangedWithValue(value, "PartyFriends");
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
	public string InGameText
	{
		get
		{
			return _inGameText;
		}
		set
		{
			if (value != _inGameText)
			{
				_inGameText = value;
				OnPropertyChangedWithValue(value, "InGameText");
			}
		}
	}

	[DataSourceProperty]
	public string OnlineText
	{
		get
		{
			return _onlineText;
		}
		set
		{
			if (value != _onlineText)
			{
				_onlineText = value;
				OnPropertyChangedWithValue(value, "OnlineText");
			}
		}
	}

	[DataSourceProperty]
	public string OfflineText
	{
		get
		{
			return _offlineText;
		}
		set
		{
			if (value != _offlineText)
			{
				_offlineText = value;
				OnPropertyChangedWithValue(value, "OfflineText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FriendListHint
	{
		get
		{
			return _friendListHint;
		}
		set
		{
			if (value != _friendListHint)
			{
				_friendListHint = value;
				OnPropertyChangedWithValue(value, "FriendListHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyFriendServiceVM> FriendServices
	{
		get
		{
			return _friendServices;
		}
		set
		{
			if (value != _friendServices)
			{
				_friendServices = value;
				OnPropertyChangedWithValue(value, "FriendServices");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyFriendServiceVM ActiveService
	{
		get
		{
			return _activeService;
		}
		set
		{
			if (value != _activeService)
			{
				_activeService = value;
				OnPropertyChangedWithValue(value, "ActiveService");
			}
		}
	}

	[DataSourceProperty]
	public int TotalOnlineFriendCount
	{
		get
		{
			return _totalOnlineFriendCount;
		}
		set
		{
			if (value != _totalOnlineFriendCount)
			{
				_totalOnlineFriendCount = value;
				OnPropertyChangedWithValue(value, "TotalOnlineFriendCount");
			}
		}
	}

	[DataSourceProperty]
	public int NotificationCount
	{
		get
		{
			return _notificationCount;
		}
		set
		{
			if (value != _notificationCount)
			{
				_notificationCount = value;
				OnPropertyChangedWithValue(value, "NotificationCount");
				HasNotification = value > 0;
			}
		}
	}

	[DataSourceProperty]
	public bool HasNotification
	{
		get
		{
			return _hasNotification;
		}
		set
		{
			if (value != _hasNotification)
			{
				_hasNotification = value;
				OnPropertyChangedWithValue(value, "HasNotification");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ToggleInputKey
	{
		get
		{
			return _toggleInputKey;
		}
		set
		{
			if (value != _toggleInputKey)
			{
				_toggleInputKey = value;
				OnPropertyChanged("ToggleInputKey");
			}
		}
	}

	public MPLobbyFriendsVM()
	{
		Player = new MPLobbyPartyPlayerVM(NetworkMain.GameClient.PlayerID, ActivatePlayerActions);
		PartyFriends = new MBBindingList<MPLobbyPartyPlayerVM>();
		PlayerActions = new MBBindingList<StringPairItemWithActionVM>();
		FriendServices = new MBBindingList<MPLobbyFriendServiceVM>();
		IFriendListService[] friendListServices = PlatformServices.Instance.GetFriendListServices();
		for (int i = 0; i < friendListServices.Length; i++)
		{
			MPLobbyFriendServiceVM item = new MPLobbyFriendServiceVM(friendListServices[i], OnFriendRequestAnswered, ActivatePlayerActions);
			FriendServices.Add(item);
		}
		_activeServiceIndex = 0;
		UpdateActiveService();
		_activeNotifications = new List<LobbyNotification>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=abxndmIh}Social").ToString();
		InGameText = new TextObject("{=uUoSmCBS}In Bannerlord").ToString();
		OnlineText = new TextObject("{=V305MaOP}Online").ToString();
		OfflineText = new TextObject("{=Zv1lg272}Offline").ToString();
		FriendListHint = new HintViewModel(new TextObject("{=tjioq56N}Friend List"));
		PartyFriends.ApplyActionOnAllItems(delegate(MPLobbyPartyPlayerVM x)
		{
			x.RefreshValues();
		});
		PlayerActions.ApplyActionOnAllItems(delegate(StringPairItemWithActionVM x)
		{
			x.RefreshValues();
		});
		FriendServices.ApplyActionOnAllItems(delegate(MPLobbyFriendServiceVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		foreach (MPLobbyFriendServiceVM friendService in FriendServices)
		{
			friendService.OnFinalize();
		}
		ToggleInputKey.OnFinalize();
	}

	public void OnStateActivate()
	{
		IsPartyAvailable = NetworkMain.GameClient.PartySystemAvailable;
		GetPartyData();
		foreach (MPLobbyFriendServiceVM friendService in FriendServices)
		{
			friendService.OnStateActivate();
		}
	}

	private void IsEnabledUpdated()
	{
		GetPartyData();
	}

	private void GetPartyData()
	{
		PartyFriends.Clear();
		if (!NetworkMain.GameClient.IsInParty)
		{
			return;
		}
		foreach (PartyPlayerInLobbyClient item in NetworkMain.GameClient.PlayersInParty)
		{
			if (item.WaitingInvitation)
			{
				OnPlayerInvitedToParty(item.PlayerId);
			}
			else
			{
				OnPlayerAddedToParty(item.PlayerId);
			}
		}
	}

	public void OnTick(float dt)
	{
		int num = 0;
		foreach (MPLobbyFriendServiceVM friendService in FriendServices)
		{
			friendService.OnTick(dt);
			if (friendService.FriendListService.IncludeInAllFriends)
			{
				num += friendService.OnlineFriends.FriendList.Count;
				num += friendService.InGameFriends.FriendList.Count;
			}
		}
		TotalOnlineFriendCount = num;
		IsInParty = NetworkMain.GameClient.IsInParty;
		RemoveAnsweredNotifications();
	}

	public void OnPlayerInvitedToParty(PlayerId playerId)
	{
		if (playerId != NetworkMain.GameClient.PlayerData.PlayerId)
		{
			MPLobbyPartyPlayerVM mPLobbyPartyPlayerVM = new MPLobbyPartyPlayerVM(playerId, ActivatePlayerActions);
			mPLobbyPartyPlayerVM.IsWaitingConfirmation = true;
			PartyFriends.Add(mPLobbyPartyPlayerVM);
		}
	}

	public void OnPlayerAddedToParty(PlayerId playerId)
	{
		if (playerId != NetworkMain.GameClient.PlayerData.PlayerId)
		{
			MPLobbyPartyPlayerVM mPLobbyPartyPlayerVM = FindPartyFriend(playerId);
			if (mPLobbyPartyPlayerVM == null)
			{
				mPLobbyPartyPlayerVM = new MPLobbyPartyPlayerVM(playerId, ActivatePlayerActions);
				PartyFriends.Add(mPLobbyPartyPlayerVM);
			}
			else
			{
				mPLobbyPartyPlayerVM.IsWaitingConfirmation = false;
			}
		}
		UpdateCanInviteOtherPlayersToParty();
		UpdatePartyLeader();
	}

	public void OnPlayerRemovedFromParty(PlayerId playerId)
	{
		if (playerId == NetworkMain.GameClient.PlayerData.PlayerId)
		{
			PartyFriends.Clear();
		}
		else
		{
			int num = -1;
			for (int i = 0; i < PartyFriends.Count; i++)
			{
				if (PartyFriends[i].ProvidedID == playerId)
				{
					num = i;
					break;
				}
			}
			if (PartyFriends.Count > 0 && num > -1 && num < PartyFriends.Count)
			{
				PartyFriends.RemoveAt(num);
			}
		}
		UpdateCanInviteOtherPlayersToParty();
		UpdatePartyLeader();
	}

	private MPLobbyPartyPlayerVM FindPartyFriend(PlayerId playerId)
	{
		foreach (MPLobbyPartyPlayerVM partyFriend in PartyFriends)
		{
			if (partyFriend.ProvidedID == playerId)
			{
				return partyFriend;
			}
		}
		return null;
	}

	internal void OnPlayerAssignedPartyLeader()
	{
		UpdatePartyLeader();
	}

	internal void OnClanInfoChanged()
	{
		MPLobbyFriendServiceVM mPLobbyFriendServiceVM = FriendServices.FirstOrDefault((MPLobbyFriendServiceVM f) => f.FriendListService.GetServiceCodeName() == "ClanFriends");
		if (NetworkMain.GameClient.IsInClan && mPLobbyFriendServiceVM == null)
		{
			IFriendListService[] friendListServices = PlatformServices.Instance.GetFriendListServices();
			IFriendListService friendListService = friendListServices.FirstOrDefault((IFriendListService f) => f.GetServiceCodeName() == "ClanFriends");
			if (friendListService != null)
			{
				MPLobbyFriendServiceVM mPLobbyFriendServiceVM2 = new MPLobbyFriendServiceVM(friendListService, OnFriendRequestAnswered, ActivatePlayerActions);
				FriendServices.Insert(friendListServices.Length - 2, mPLobbyFriendServiceVM2);
				mPLobbyFriendServiceVM2.ForceRefresh();
			}
		}
		else if (NetworkMain.GameClient.IsInClan && mPLobbyFriendServiceVM != null)
		{
			mPLobbyFriendServiceVM.ForceRefresh();
		}
		else
		{
			if (NetworkMain.GameClient.IsInClan || mPLobbyFriendServiceVM == null)
			{
				return;
			}
			for (int num = FriendServices.Count - 1; num >= 0; num--)
			{
				IFriendListService friendListService2 = FriendServices[num].FriendListService;
				if (!NetworkMain.GameClient.IsInClan && friendListService2.GetServiceCodeName() == "ClanFriends")
				{
					FriendServices[num].OnFinalize();
					FriendServices.RemoveAt(num);
					break;
				}
			}
		}
	}

	private void ActivatePlayerActions(MPLobbyPlayerBaseVM player)
	{
		PlayerActions.Clear();
		if (player is MPLobbyPartyPlayerVM player2)
		{
			ActivatePartyPlayerActions(player2);
		}
		else if (player is MPLobbyFriendItemVM player3)
		{
			ActivateFriendPlayerActions(player3);
		}
		IsPlayerActionsActive = false;
		IsPlayerActionsActive = PlayerActions.Count > 0;
	}

	private void ExecuteSetPlayerAsLeader(object playerObj)
	{
		MPLobbyPlayerBaseVM mPLobbyPlayerBaseVM = playerObj as MPLobbyPlayerBaseVM;
		NetworkMain.GameClient.PromotePlayerToPartyLeader(mPLobbyPlayerBaseVM.ProvidedID);
		UpdatePartyLeader();
	}

	private void ExecuteKickPlayerFromParty(object playerObj)
	{
		MPLobbyPlayerBaseVM mPLobbyPlayerBaseVM = playerObj as MPLobbyPlayerBaseVM;
		if (NetworkMain.GameClient.IsInParty && NetworkMain.GameClient.IsPartyLeader)
		{
			NetworkMain.GameClient.KickPlayerFromParty(mPLobbyPlayerBaseVM.ProvidedID);
		}
		UpdatePartyLeader();
	}

	private void ExecuteLeaveParty(object playerObj)
	{
		MPLobbyPlayerBaseVM mPLobbyPlayerBaseVM = playerObj as MPLobbyPlayerBaseVM;
		if (NetworkMain.GameClient.IsInParty && mPLobbyPlayerBaseVM.ProvidedID == NetworkMain.GameClient.PlayerData.PlayerId)
		{
			NetworkMain.GameClient.KickPlayerFromParty(NetworkMain.GameClient.PlayerData.PlayerId);
		}
	}

	private void ExecuteInviteFriend(PlayerId providedId)
	{
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(providedId);
		PlatformServices.Instance.CheckPrivilege(Privilege.Communication, displayResolveUI: true, delegate(bool result)
		{
			if (result)
			{
				if (providedId.ProvidedType != NetworkMain.GameClient?.PlayerID.ProvidedType)
				{
					NetworkMain.GameClient.InviteToParty(providedId, dontUseNameForUnknownPlayer);
				}
				else
				{
					PlatformServices.Instance.CheckPermissionWithUser(Permission.PlayMultiplayer, providedId, delegate(bool permissionResult)
					{
						if (permissionResult)
						{
							NetworkMain.GameClient.InviteToParty(providedId, dontUseNameForUnknownPlayer);
						}
						else
						{
							string titleText = new TextObject("{=ZwN6rzTC}No permission").ToString();
							string text = new TextObject("{=wlz3eQWp}No permission to invite player.").ToString();
							InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", new TextObject("{=dismissnotification}Dismiss").ToString(), null, delegate
							{
								InformationManager.HideInquiry();
							}, "event:/ui/notification/quest_update"));
						}
					});
				}
			}
		});
	}

	private void ExecuteRequestFriendship(object playerObj)
	{
		MPLobbyPlayerBaseVM mPLobbyPlayerBaseVM = playerObj as MPLobbyPlayerBaseVM;
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(mPLobbyPlayerBaseVM.ProvidedID);
		NetworkMain.GameClient.AddFriend(mPLobbyPlayerBaseVM.ProvidedID, dontUseNameForUnknownPlayer);
	}

	private void ExecuteTerminateFriendship(object memberObj)
	{
		MPLobbyPlayerBaseVM mPLobbyPlayerBaseVM = memberObj as MPLobbyPlayerBaseVM;
		NetworkMain.GameClient.RemoveFriend(mPLobbyPlayerBaseVM.ProvidedID);
	}

	public void UpdateCanInviteOtherPlayersToParty()
	{
		foreach (MPLobbyFriendServiceVM friendService in FriendServices)
		{
			friendService.UpdateCanInviteOtherPlayersToParty();
		}
	}

	public void UpdatePartyLeader()
	{
		MPLobbyPartyPlayerVM player = Player;
		int isPartyLeader;
		if (NetworkMain.GameClient.IsInParty)
		{
			PlayerId providedID = Player.ProvidedID;
			PlayerId? partyLeaderId = _partyLeaderId;
			isPartyLeader = ((providedID == partyLeaderId) ? 1 : 0);
		}
		else
		{
			isPartyLeader = 0;
		}
		player.IsPartyLeader = (byte)isPartyLeader != 0;
		foreach (MPLobbyPartyPlayerVM partyFriend in PartyFriends)
		{
			PlayerId providedID = partyFriend.ProvidedID;
			PlayerId? partyLeaderId = _partyLeaderId;
			partyFriend.IsPartyLeader = providedID == partyLeaderId;
		}
	}

	private static bool TryGetRequesterID(LobbyNotification notification, out PlayerId requesterID)
	{
		requesterID = default(PlayerId);
		if (notification?.Parameters == null || !notification.Parameters.TryGetValue("friend_requester", out var value) || string.IsNullOrEmpty(value))
		{
			return false;
		}
		try
		{
			requesterID = PlayerId.FromString(value);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private bool IsFriendRequestPending(PlayerId playerID)
	{
		FriendInfo[] friendInfos = NetworkMain.GameClient.FriendInfos;
		if (friendInfos == null)
		{
			return false;
		}
		FriendInfo[] array = friendInfos;
		foreach (FriendInfo friendInfo in array)
		{
			if (friendInfo.Id == playerID)
			{
				return friendInfo.Status == FriendStatus.Received;
			}
		}
		return false;
	}

	private void RefreshNotificationCount()
	{
		NotificationCount = _activeNotifications.Count;
		HasNotification = _activeNotifications.Count > 0;
	}

	private void RemoveAnsweredNotifications(PlayerId? justAnsweredID = null)
	{
		bool flag = false;
		for (int num = _activeNotifications.Count - 1; num >= 0; num--)
		{
			LobbyNotification lobbyNotification = _activeNotifications[num];
			if (!TryGetRequesterID(lobbyNotification, out var requesterID) || !(requesterID != justAnsweredID) || !IsFriendRequestPending(requesterID))
			{
				NetworkMain.GameClient.MarkNotificationAsRead(lobbyNotification.Id);
				_activeNotifications.RemoveAt(num);
				flag = true;
			}
		}
		if (flag)
		{
			RefreshNotificationCount();
		}
	}

	public void OnFriendRequestNotificationsReceived(List<LobbyNotification> notifications)
	{
		foreach (LobbyNotification notification in notifications)
		{
			if (!TryGetRequesterID(notification, out var notificationPlayerID))
			{
				NetworkMain.GameClient.MarkNotificationAsRead(notification.Id);
			}
			else
			{
				if (_activeNotifications.Exists((LobbyNotification n) => TryGetRequesterID(n, out var requesterID) && requesterID == notificationPlayerID))
				{
					continue;
				}
				PlatformServices.Instance.CheckPrivilege(Privilege.Communication, displayResolveUI: false, delegate(bool privilegeResult)
				{
					if (!privilegeResult)
					{
						ProcessNotification(notification, notificationPlayerID, allowed: false);
					}
					else
					{
						PlatformServices.Instance.CheckPermissionWithUser(Permission.CommunicateUsingText, notificationPlayerID, delegate(bool permissionResult)
						{
							ProcessNotification(notification, notificationPlayerID, permissionResult);
						});
					}
				});
			}
		}
	}

	private void ProcessNotification(LobbyNotification notification, PlayerId notificationPlayerID, bool allowed)
	{
		if (!allowed)
		{
			NetworkMain.GameClient.MarkNotificationAsRead(notification.Id);
		}
		else if (MultiplayerPlayerHelper.IsBlocked(notificationPlayerID))
		{
			bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(notificationPlayerID);
			NetworkMain.GameClient.RespondToFriendRequest(notificationPlayerID, dontUseNameForUnknownPlayer, isAccepted: false, isBlocked: true);
			NetworkMain.GameClient.MarkNotificationAsRead(notification.Id);
		}
		else if (!IsFriendRequestPending(notificationPlayerID))
		{
			NetworkMain.GameClient.MarkNotificationAsRead(notification.Id);
		}
		else
		{
			_activeNotifications.Add(notification);
			RefreshNotificationCount();
		}
	}

	private void OnFriendRequestAnswered(PlayerId playerID)
	{
		RemoveAnsweredNotifications(playerID);
	}

	public MBBindingList<MPLobbyPlayerBaseVM> GetAllFriends()
	{
		MBBindingList<MPLobbyPlayerBaseVM> mBBindingList = new MBBindingList<MPLobbyPlayerBaseVM>();
		foreach (MPLobbyFriendServiceVM friendService in FriendServices)
		{
			if (!friendService.FriendListService.IncludeInAllFriends)
			{
				continue;
			}
			foreach (MPLobbyFriendItemVM friend in friendService.InGameFriends.FriendList)
			{
				mBBindingList.Add(friend);
			}
			foreach (MPLobbyFriendItemVM friend2 in friendService.OnlineFriends.FriendList)
			{
				mBBindingList.Add(friend2);
			}
		}
		return mBBindingList;
	}

	public void OnSupportedFeaturesRefreshed(SupportedFeatures supportedFeatures)
	{
		if (!supportedFeatures.SupportsFeatures(Features.BannerlordFriendList))
		{
			MPLobbyFriendServiceVM mPLobbyFriendServiceVM = FriendServices.FirstOrDefault((MPLobbyFriendServiceVM fs) => fs.FriendListService.GetType() == typeof(BannerlordFriendListService));
			mPLobbyFriendServiceVM?.OnFinalize();
			FriendServices.Remove(mPLobbyFriendServiceVM);
		}
	}

	public void OnFriendListUpdated(bool forceUpdate = false)
	{
		foreach (MPLobbyFriendServiceVM friendService in FriendServices)
		{
			friendService.OnFriendListUpdated(forceUpdate);
		}
		Player.UpdateNameAndAvatar(forceUpdate);
		foreach (MPLobbyPartyPlayerVM partyFriend in PartyFriends)
		{
			partyFriend.UpdateNameAndAvatar(forceUpdate);
		}
	}

	public void SetToggleFriendListKey(HotKey hotkey)
	{
		ToggleInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	private void ActivatePartyPlayerActions(MPLobbyPartyPlayerVM player)
	{
		if (NetworkMain.GameClient.IsPartyLeader && player.ProvidedID != NetworkMain.GameClient.PlayerData.PlayerId)
		{
			PartyPlayerInLobbyClient partyPlayerInLobbyClient = NetworkMain.GameClient.PlayersInParty.SingleOrDefault((PartyPlayerInLobbyClient p) => p.PlayerId == player.ProvidedID);
			if (partyPlayerInLobbyClient != null && !partyPlayerInLobbyClient.WaitingInvitation && PlatformServices.InvitationServices == null)
			{
				PlayerActions.Add(new StringPairItemWithActionVM(ExecuteSetPlayerAsLeader, new TextObject("{=P7moPm3F}Set as party leader").ToString(), "PromoteToPartyLeader", player));
			}
			PlayerActions.Add(new StringPairItemWithActionVM(ExecuteKickPlayerFromParty, new TextObject("{=partykick}Kick").ToString(), "Kick", player));
		}
		if (player.ProvidedID == NetworkMain.GameClient.PlayerData.PlayerId)
		{
			if (NetworkMain.GameClient.IsInParty)
			{
				PlayerActions.Add(new StringPairItemWithActionVM(ExecuteLeaveParty, new TextObject("{=9w9JsBYP}Leave party").ToString(), "LeaveParty", player));
			}
			return;
		}
		bool flag = false;
		FriendInfo[] friendInfos = NetworkMain.GameClient.FriendInfos;
		for (int num = 0; num < friendInfos.Length; num++)
		{
			if (friendInfos[num].Id == player.ProvidedID)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			PlayerActions.Add(new StringPairItemWithActionVM(ExecuteRequestFriendship, new TextObject("{=UwkpJq9N}Add As Friend").ToString(), "RequestFriendship", player));
		}
		else
		{
			PlayerActions.Add(new StringPairItemWithActionVM(ExecuteTerminateFriendship, new TextObject("{=2YIVRuRa}Remove From Friends").ToString(), "TerminateFriendship", player));
		}
		MultiplayerPlayerContextMenuHelper.AddLobbyViewProfileOptions(player, PlayerActions);
	}

	private void ActivateFriendPlayerActions(MPLobbyFriendItemVM player)
	{
		if (player.CanSpectate)
		{
			PlayerActions.Add(new StringPairItemWithActionVM(ExecuteSpectateMatch, new TextObject("{=V0zs1LfD}Watch Game").ToString(), "SpectateMatch", player));
		}
		MultiplayerPlayerContextMenuHelper.AddLobbyViewProfileOptions(player, PlayerActions);
	}

	private void ExecuteSpectateMatch(object playerObj)
	{
		(playerObj as MPLobbyPlayerBaseVM)?.ExecuteSpectateMatch();
	}

	private void ExecuteSwitchToNextService()
	{
		if (FriendServices.Count == 0)
		{
			Debug.FailedAssert("Friend service list is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Friends\\MPLobbyFriendsVM.cs", "ExecuteSwitchToNextService", 636);
			return;
		}
		_activeServiceIndex++;
		if (_activeServiceIndex >= FriendServices.Count)
		{
			_activeServiceIndex = 0;
		}
		UpdateActiveService();
	}

	private void ExecuteSwitchToPreviousService()
	{
		if (FriendServices.Count == 0)
		{
			Debug.FailedAssert("Friend service list is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Friends\\MPLobbyFriendsVM.cs", "ExecuteSwitchToPreviousService", 653);
			return;
		}
		_activeServiceIndex--;
		if (_activeServiceIndex < 0)
		{
			_activeServiceIndex = FriendServices.Count - 1;
		}
		UpdateActiveService();
	}

	private void UpdateActiveService()
	{
		if (_activeServiceIndex < 0 || _activeServiceIndex >= FriendServices.Count)
		{
			Debug.FailedAssert($"Multiplayer service index is invalid: {_activeServiceIndex}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Friends\\MPLobbyFriendsVM.cs", "UpdateActiveService", 670);
			if (FriendServices.Count <= 0)
			{
				Debug.FailedAssert("Cancelling service update request.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Friends\\MPLobbyFriendsVM.cs", "UpdateActiveService", 678);
				return;
			}
			Debug.FailedAssert("Defaulting to first available service: " + FriendServices[0].ServiceName, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Friends\\MPLobbyFriendsVM.cs", "UpdateActiveService", 673);
			_activeServiceIndex = 0;
		}
		ActiveService = FriendServices[_activeServiceIndex];
	}
}
