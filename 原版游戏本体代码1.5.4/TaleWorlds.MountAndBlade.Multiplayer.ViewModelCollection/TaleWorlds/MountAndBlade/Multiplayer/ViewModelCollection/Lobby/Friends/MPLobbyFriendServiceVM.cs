using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyFriendServiceVM : ViewModel
{
	private readonly struct FriendServiceEvent
	{
		public enum EventTypes
		{
			FriendListChanged,
			UserStatusChanged,
			FriendRemoved
		}

		public readonly EventTypes Type;

		public readonly PlayerId ProvidedID;

		public FriendServiceEvent(EventTypes type, PlayerId providedId = default(PlayerId))
		{
			Type = type;
			ProvidedID = providedId;
		}
	}

	private class PlayerStateComparer : IComparer<MPLobbyPlayerBaseVM>
	{
		public int Compare(MPLobbyPlayerBaseVM x, MPLobbyPlayerBaseVM y)
		{
			int stateImportanceOrder = GetStateImportanceOrder(x.State);
			int stateImportanceOrder2 = GetStateImportanceOrder(y.State);
			if (stateImportanceOrder != stateImportanceOrder2)
			{
				return stateImportanceOrder.CompareTo(stateImportanceOrder2);
			}
			return x.Name.CompareTo(y.Name);
		}

		private int GetStateImportanceOrder(AnotherPlayerState state)
		{
			return state switch
			{
				AnotherPlayerState.AtLobby => 0, 
				AnotherPlayerState.InParty => 1, 
				AnotherPlayerState.InMultiplayerGame => 2, 
				_ => int.MaxValue, 
			};
		}
	}

	private const string _inviteFailedSoundEvent = "event:/ui/notification/quest_update";

	public readonly IFriendListService FriendListService;

	private readonly Action<MPLobbyPlayerBaseVM> _activatePlayerActions;

	private bool _populatingFriends;

	private bool _isInGameFriendsRelevant;

	private const float UpdateInterval = 2f;

	private float _lastUpdateTimePassed;

	private const float StateRequestInterval = 10f;

	private float _lastStateRequestTimePassed;

	private bool _isStateRequestActive;

	private readonly PlayerStateComparer _playerStateComparer;

	private Action<PlayerId> _onFriendRequestAnswered;

	private bool _isPartyAvailable;

	private static Dictionary<PlayerId, long> _friendRequestsInProcess = new Dictionary<PlayerId, long>();

	private const int BlockedFriendRequestTimeout = 10000;

	private readonly List<FriendServiceEvent> _friendServiceEventQueue = new List<FriendServiceEvent>();

	private bool _isInGameStatusActive;

	private MPLobbyFriendGroupVM _inGameFriends;

	private MPLobbyFriendGroupVM _onlineFriends;

	private MPLobbyFriendGroupVM _offlineFriends;

	private string _inGameText;

	private string _onlineText;

	private string _offlineText;

	private string _serviceName;

	private HintViewModel _serviceNameHint;

	private MPLobbyFriendGroupVM _friendRequests;

	private bool _gotAnyFriendRequests;

	private MPLobbyFriendGroupVM _pendingRequests;

	private bool _gotAnyPendingRequests;

	public IEnumerable<MPLobbyPlayerBaseVM> AllFriends => InGameFriends.FriendList.Union(OnlineFriends.FriendList.Union(OfflineFriends.FriendList));

	[DataSourceProperty]
	public bool IsInGameStatusActive
	{
		get
		{
			return _isInGameStatusActive;
		}
		set
		{
			if (value != _isInGameStatusActive)
			{
				_isInGameStatusActive = value;
				OnPropertyChangedWithValue(value, "IsInGameStatusActive");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyFriendGroupVM InGameFriends
	{
		get
		{
			return _inGameFriends;
		}
		set
		{
			if (value != _inGameFriends)
			{
				_inGameFriends = value;
				OnPropertyChangedWithValue(value, "InGameFriends");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyFriendGroupVM OnlineFriends
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
				OnPropertyChangedWithValue(value, "OnlineFriends");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyFriendGroupVM OfflineFriends
	{
		get
		{
			return _offlineFriends;
		}
		set
		{
			if (value != _offlineFriends)
			{
				_offlineFriends = value;
				OnPropertyChangedWithValue(value, "OfflineFriends");
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
	public string ServiceName
	{
		get
		{
			return _serviceName;
		}
		set
		{
			if (value != _serviceName)
			{
				_serviceName = value;
				OnPropertyChangedWithValue(value, "ServiceName");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyFriendGroupVM FriendRequests
	{
		get
		{
			return _friendRequests;
		}
		set
		{
			if (value != _friendRequests)
			{
				_friendRequests = value;
				OnPropertyChangedWithValue(value, "FriendRequests");
			}
		}
	}

	[DataSourceProperty]
	public bool GotAnyFriendRequests
	{
		get
		{
			return _gotAnyFriendRequests;
		}
		set
		{
			if (value != _gotAnyFriendRequests)
			{
				_gotAnyFriendRequests = value;
				OnPropertyChangedWithValue(value, "GotAnyFriendRequests");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyFriendGroupVM PendingRequests
	{
		get
		{
			return _pendingRequests;
		}
		set
		{
			if (value != _pendingRequests)
			{
				_pendingRequests = value;
				OnPropertyChangedWithValue(value, "PendingRequests");
			}
		}
	}

	[DataSourceProperty]
	public bool GotAnyPendingRequests
	{
		get
		{
			return _gotAnyPendingRequests;
		}
		set
		{
			if (value != _gotAnyPendingRequests)
			{
				_gotAnyPendingRequests = value;
				OnPropertyChangedWithValue(value, "GotAnyPendingRequests");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ServiceNameHint
	{
		get
		{
			return _serviceNameHint;
		}
		set
		{
			if (value != _serviceNameHint)
			{
				_serviceNameHint = value;
				OnPropertyChangedWithValue(value, "ServiceNameHint");
			}
		}
	}

	public MPLobbyFriendServiceVM(IFriendListService friendListService, Action<PlayerId> onFriendRequestAnswered, Action<MPLobbyPlayerBaseVM> activatePlayerActions)
	{
		FriendListService = friendListService;
		_onFriendRequestAnswered = onFriendRequestAnswered;
		_activatePlayerActions = activatePlayerActions;
		_playerStateComparer = new PlayerStateComparer();
		InGameFriends = new MPLobbyFriendGroupVM(MPLobbyFriendGroupVM.FriendGroupType.InGame);
		OnlineFriends = new MPLobbyFriendGroupVM(MPLobbyFriendGroupVM.FriendGroupType.Online);
		OfflineFriends = new MPLobbyFriendGroupVM(MPLobbyFriendGroupVM.FriendGroupType.Offline);
		FriendRequests = new MPLobbyFriendGroupVM(MPLobbyFriendGroupVM.FriendGroupType.FriendRequests);
		PendingRequests = new MPLobbyFriendGroupVM(MPLobbyFriendGroupVM.FriendGroupType.PendingRequests);
		FriendListServiceType friendListServiceType = friendListService.GetFriendListServiceType();
		_isInGameFriendsRelevant = friendListServiceType != FriendListServiceType.Bannerlord && friendListServiceType != FriendListServiceType.RecentPlayers && friendListServiceType != FriendListServiceType.Clan && friendListServiceType != FriendListServiceType.PlayStation;
		PlatformServices.Instance.OnBlockedUserListUpdated += BlockedUserListChanged;
		FriendListService.OnUserStatusChanged += UserOnlineStatusChanged;
		FriendListService.OnFriendRemoved += FriendRemoved;
		FriendListService.OnFriendListChanged += FriendListChanged;
		ServiceName = friendListService.GetServiceCodeName();
		RefreshValues();
		UpdateCanInviteOtherPlayersToParty();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		InGameText = new TextObject("{=uUoSmCBS}In Bannerlord").ToString();
		OnlineText = new TextObject("{=V305MaOP}Online").ToString();
		OfflineText = new TextObject("{=Zv1lg272}Offline").ToString();
		ServiceNameHint = new HintViewModel(FriendListService.GetServiceLocalizedName());
		InGameFriends.RefreshValues();
		OnlineFriends.RefreshValues();
		OfflineFriends.RefreshValues();
		FriendRequests.RefreshValues();
		PendingRequests.RefreshValues();
	}

	public override void OnFinalize()
	{
		PlatformServices.Instance.OnBlockedUserListUpdated -= BlockedUserListChanged;
		FriendListService.OnUserStatusChanged -= UserOnlineStatusChanged;
		FriendListService.OnFriendRemoved -= FriendRemoved;
		FriendListService.OnFriendListChanged -= FriendListChanged;
		base.OnFinalize();
	}

	public void OnStateActivate()
	{
		_isPartyAvailable = NetworkMain.GameClient.PartySystemAvailable;
		IsInGameStatusActive = FriendListService.InGameStatusFetchable && _isInGameFriendsRelevant;
		GetFriends();
	}

	private async void GetFriends()
	{
		IEnumerable<PlayerId> allFriends = FriendListService.GetAllFriends();
		if (allFriends == null || _populatingFriends)
		{
			return;
		}
		_populatingFriends = true;
		InGameFriends.ClearFriends();
		OnlineFriends.ClearFriends();
		OfflineFriends.ClearFriends();
		foreach (PlayerId item in allFriends)
		{
			await CreateAndAddFriendToList(item);
		}
		_lastStateRequestTimePassed = 11f;
		_populatingFriends = false;
	}

	public void OnTick(float dt)
	{
		HandleFriendServiceEvents();
		UpdateFriendStates(dt);
		_lastUpdateTimePassed += dt;
		if (_lastUpdateTimePassed >= 2f)
		{
			_lastUpdateTimePassed = 0f;
			if (FriendListService.AllowsFriendOperations)
			{
				TickFriendOperations(dt);
			}
		}
		InGameFriends?.Tick();
		OnlineFriends?.Tick();
		OfflineFriends?.Tick();
		FriendRequests?.Tick();
		PendingRequests?.Tick();
	}

	private void TimeoutProcessedFriendRequests()
	{
		PlayerId[] array = _friendRequestsInProcess.Keys.ToArray();
		foreach (PlayerId key in array)
		{
			if (Environment.TickCount - _friendRequestsInProcess[key] > 10000)
			{
				_friendRequestsInProcess.Remove(key);
			}
		}
	}

	private void BlockFriendRequest(PlayerId friendId)
	{
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(friendId);
		NetworkMain.GameClient.RespondToFriendRequest(friendId, dontUseNameForUnknownPlayer, isAccepted: false, isBlocked: true);
	}

	private void ProcessFriendRequest(PlayerId friendId)
	{
		if (_friendRequestsInProcess.ContainsKey(friendId))
		{
			return;
		}
		_friendRequestsInProcess[friendId] = Environment.TickCount;
		PlatformServices.Instance.CheckPrivilege(Privilege.Communication, displayResolveUI: false, delegate(bool privilegeResult)
		{
			if (!privilegeResult)
			{
				BlockFriendRequest(friendId);
			}
			else if (friendId.ProvidedType != NetworkMain.GameClient.PlayerID.ProvidedType)
			{
				AddFriendRequestItem(friendId);
			}
			else
			{
				PlatformServices.Instance.CheckPermissionWithUser(Permission.CommunicateUsingText, friendId, delegate(bool permissionResult)
				{
					if (!permissionResult)
					{
						BlockFriendRequest(friendId);
					}
					else
					{
						AddFriendRequestItem(friendId);
					}
				});
			}
		});
	}

	private void AddFriendRequestItem(PlayerId playerID)
	{
		MPLobbyFriendItemVM mPLobbyFriendItemVM = new MPLobbyFriendItemVM(playerID, _activatePlayerActions, null, _onFriendRequestAnswered);
		mPLobbyFriendItemVM.IsFriendRequest = true;
		mPLobbyFriendItemVM.CanRemove = false;
		FriendRequests.AddFriend(mPLobbyFriendItemVM);
	}

	private void TickFriendOperations(float dt)
	{
		IEnumerable<PlayerId> receivedRequests = FriendListService.GetReceivedRequests();
		if (receivedRequests != null)
		{
			GotAnyFriendRequests = receivedRequests.Any();
			TimeoutProcessedFriendRequests();
			foreach (PlayerId friendId in receivedRequests)
			{
				if (FriendRequests.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == friendId) == null)
				{
					ProcessFriendRequest(friendId);
				}
			}
			int i;
			for (i = FriendRequests.FriendList.Count - 1; i >= 0; i--)
			{
				if (!receivedRequests.FirstOrDefault((PlayerId p) => p == FriendRequests.FriendList[i].ProvidedID).IsValid)
				{
					FriendRequests.RemoveFriend(FriendRequests.FriendList[i]);
				}
			}
		}
		else
		{
			GotAnyFriendRequests = false;
		}
		IEnumerable<PlayerId> pendingRequests = FriendListService.GetPendingRequests();
		if (pendingRequests != null)
		{
			GotAnyPendingRequests = pendingRequests.Any();
			foreach (PlayerId friendId2 in pendingRequests)
			{
				if (PendingRequests.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == friendId2) == null)
				{
					MPLobbyFriendItemVM mPLobbyFriendItemVM = new MPLobbyFriendItemVM(friendId2, _activatePlayerActions);
					mPLobbyFriendItemVM.IsPendingRequest = true;
					mPLobbyFriendItemVM.CanRemove = false;
					PendingRequests.AddFriend(mPLobbyFriendItemVM);
				}
			}
			int i2;
			for (i2 = PendingRequests.FriendList.Count - 1; i2 >= 0; i2--)
			{
				if (!pendingRequests.FirstOrDefault((PlayerId p) => p == PendingRequests.FriendList[i2].ProvidedID).IsValid)
				{
					PendingRequests.RemoveFriend(PendingRequests.FriendList[i2]);
				}
			}
		}
		else
		{
			GotAnyPendingRequests = false;
		}
	}

	private void UpdateFriendStates(float dt)
	{
		if (!NetworkMain.GameClient.AtLobby || _isStateRequestActive)
		{
			return;
		}
		_lastStateRequestTimePassed += dt;
		if (_lastStateRequestTimePassed >= 10f)
		{
			List<PlayerId> list = new List<PlayerId>();
			list.AddRange(InGameFriends.FriendList.Select((MPLobbyFriendItemVM p) => p.ProvidedID));
			list.AddRange(OnlineFriends.FriendList.Select((MPLobbyFriendItemVM p) => p.ProvidedID));
			_lastStateRequestTimePassed = 0f;
			UpdatePlayerStates(list);
		}
	}

	private async void UpdatePlayerStates(List<PlayerId> players)
	{
		if (players == null || players.Count <= 0)
		{
			return;
		}
		_isStateRequestActive = true;
		List<(PlayerId, AnotherPlayerData)> list = await NetworkMain.GameClient.GetOtherPlayersState(players);
		if (list != null)
		{
			foreach (var (playerId, playerData) in list)
			{
				GetFriendWithID(playerId)?.UpdatePlayerState(playerData);
			}
			InGameFriends.FriendList.Sort(_playerStateComparer);
			OnlineFriends.FriendList.Sort(_playerStateComparer);
			OfflineFriends.FriendList.Sort(_playerStateComparer);
		}
		_isStateRequestActive = false;
		UpdateCanInviteOtherPlayersToParty();
	}

	private void HandleFriendServiceEvents()
	{
		bool friendListChanged = false;
		lock (_friendServiceEventQueue)
		{
			for (int i = 0; i < _friendServiceEventQueue.Count; i++)
			{
				HandleFriendServiceEventAux(_friendServiceEventQueue[i], ref friendListChanged);
			}
			_friendServiceEventQueue.Clear();
		}
		if (friendListChanged)
		{
			if (_populatingFriends)
			{
				EnqueueFriendServiceEvent(new FriendServiceEvent(FriendServiceEvent.EventTypes.FriendListChanged));
			}
			else
			{
				GetFriends();
			}
		}
	}

	private void EnqueueFriendServiceEvent(FriendServiceEvent serviceEvent)
	{
		lock (_friendServiceEventQueue)
		{
			_friendServiceEventQueue.Add(serviceEvent);
		}
	}

	private void HandleFriendServiceEventAux(FriendServiceEvent serviceEvent, ref bool friendListChanged)
	{
		switch (serviceEvent.Type)
		{
		case FriendServiceEvent.EventTypes.FriendListChanged:
			friendListChanged = true;
			break;
		case FriendServiceEvent.EventTypes.UserStatusChanged:
			if (!friendListChanged)
			{
				UpdateFriendInList(serviceEvent.ProvidedID);
			}
			break;
		case FriendServiceEvent.EventTypes.FriendRemoved:
			if (!friendListChanged)
			{
				RemoveFriend(serviceEvent.ProvidedID);
			}
			break;
		}
	}

	private void BlockedUserListChanged()
	{
		EnqueueFriendServiceEvent(new FriendServiceEvent(FriendServiceEvent.EventTypes.FriendListChanged));
	}

	private void FriendListChanged()
	{
		EnqueueFriendServiceEvent(new FriendServiceEvent(FriendServiceEvent.EventTypes.FriendListChanged));
	}

	public void ForceRefresh()
	{
		EnqueueFriendServiceEvent(new FriendServiceEvent(FriendServiceEvent.EventTypes.FriendListChanged));
	}

	private void UserOnlineStatusChanged(PlayerId providedId)
	{
		EnqueueFriendServiceEvent(new FriendServiceEvent(FriendServiceEvent.EventTypes.UserStatusChanged, providedId));
	}

	private void FriendRemoved(PlayerId providedId)
	{
		EnqueueFriendServiceEvent(new FriendServiceEvent(FriendServiceEvent.EventTypes.FriendRemoved, providedId));
	}

	private async void UpdateFriendInList(PlayerId providedId)
	{
		await CreateAndAddFriendToList(providedId);
	}

	private void RemoveFriend(PlayerId providedId)
	{
		MPLobbyFriendItemVM mPLobbyFriendItemVM = InGameFriends.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == providedId);
		if (mPLobbyFriendItemVM != null)
		{
			InGameFriends.RemoveFriend(mPLobbyFriendItemVM);
			return;
		}
		mPLobbyFriendItemVM = OnlineFriends.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == providedId);
		if (mPLobbyFriendItemVM != null)
		{
			OnlineFriends.RemoveFriend(mPLobbyFriendItemVM);
			return;
		}
		mPLobbyFriendItemVM = OfflineFriends.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == providedId);
		if (mPLobbyFriendItemVM != null)
		{
			OfflineFriends.RemoveFriend(mPLobbyFriendItemVM);
		}
	}

	private async Task CreateAndAddFriendToList(PlayerId playerId)
	{
		if (MultiplayerPlayerHelper.IsBlocked(playerId))
		{
			return;
		}
		RemoveFriend(playerId);
		MPLobbyPlayerBaseVM.OnlineStatus onlineStatus = await GetOnlineStatus(playerId);
		MPLobbyFriendItemVM mPLobbyFriendItemVM = new MPLobbyFriendItemVM(playerId, _activatePlayerActions, ExecuteInviteToClan)
		{
			CanRemove = (FriendListService.AllowsFriendOperations && FriendListService.IncludeInAllFriends)
		};
		if (_isInGameFriendsRelevant)
		{
			switch (onlineStatus)
			{
			case MPLobbyPlayerBaseVM.OnlineStatus.InGame:
				InGameFriends.AddFriend(mPLobbyFriendItemVM);
				break;
			case MPLobbyPlayerBaseVM.OnlineStatus.Online:
				OnlineFriends.AddFriend(mPLobbyFriendItemVM);
				break;
			case MPLobbyPlayerBaseVM.OnlineStatus.Offline:
				OfflineFriends.AddFriend(mPLobbyFriendItemVM);
				break;
			}
		}
		else if (onlineStatus == MPLobbyPlayerBaseVM.OnlineStatus.InGame || onlineStatus == MPLobbyPlayerBaseVM.OnlineStatus.Online)
		{
			OnlineFriends.AddFriend(mPLobbyFriendItemVM);
		}
		else
		{
			OfflineFriends.AddFriend(mPLobbyFriendItemVM);
		}
		mPLobbyFriendItemVM.OnStatusChanged(onlineStatus, IsInGameStatusActive);
	}

	private MPLobbyPlayerBaseVM GetFriendWithID(PlayerId playerId)
	{
		MPLobbyFriendItemVM mPLobbyFriendItemVM = _onlineFriends.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == playerId);
		if (mPLobbyFriendItemVM != null)
		{
			return mPLobbyFriendItemVM;
		}
		MPLobbyFriendItemVM mPLobbyFriendItemVM2 = _inGameFriends.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == playerId);
		if (mPLobbyFriendItemVM2 != null)
		{
			return mPLobbyFriendItemVM2;
		}
		MPLobbyFriendItemVM mPLobbyFriendItemVM3 = _offlineFriends.FriendList.FirstOrDefault((MPLobbyFriendItemVM p) => p.ProvidedID == playerId);
		if (mPLobbyFriendItemVM3 != null)
		{
			return mPLobbyFriendItemVM3;
		}
		return null;
	}

	public void UpdateCanInviteOtherPlayersToParty()
	{
		OfflineFriends.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
		{
			f.SetOnInvite(null);
		});
		PendingRequests.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
		{
			f.SetOnInvite(null);
		});
		FriendRequests.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
		{
			f.SetOnInvite(null);
		});
		OnlineFriends.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
		{
			f.SetOnInvite(GetOnInvite(f.ProvidedID, f.CurrentOnlineStatus, f.State));
		});
		InGameFriends.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
		{
			f.SetOnInvite(GetOnInvite(f.ProvidedID, f.CurrentOnlineStatus, f.State));
		});
	}

	public void OnFriendListUpdated(bool updateForced = false)
	{
		if (!_populatingFriends)
		{
			InGameFriends.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
			{
				f.UpdateNameAndAvatar(updateForced);
			});
			OnlineFriends.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
			{
				f.UpdateNameAndAvatar(updateForced);
			});
			OfflineFriends.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
			{
				f.UpdateNameAndAvatar(updateForced);
			});
			FriendRequests.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
			{
				f.UpdateNameAndAvatar(updateForced);
			});
			PendingRequests.FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM f)
			{
				f.UpdateNameAndAvatar(updateForced);
			});
		}
	}

	private async Task<MPLobbyPlayerBaseVM.OnlineStatus> GetOnlineStatus(PlayerId playerId)
	{
		bool isOnline = await FriendListService.GetUserOnlineStatus(playerId);
		bool flag = false;
		if (IsInGameStatusActive)
		{
			flag = await FriendListService.IsPlayingThisGame(playerId);
		}
		return (!isOnline) ? MPLobbyPlayerBaseVM.OnlineStatus.Offline : (flag ? MPLobbyPlayerBaseVM.OnlineStatus.InGame : MPLobbyPlayerBaseVM.OnlineStatus.Online);
	}

	private Action<PlayerId> GetOnInvite(PlayerId playerId, MPLobbyPlayerBaseVM.OnlineStatus onlineStatus, AnotherPlayerState state)
	{
		Action<PlayerId> result = null;
		if (PlatformServices.Instance.UsePlatformInvitationService(playerId) && playerId.ProvidedType == NetworkMain.GameClient.PlayerID.ProvidedType)
		{
			result = ((ApplicationPlatform.CurrentPlatform != Platform.GDKDesktop || state == AnotherPlayerState.AtLobby) ? new Action<PlayerId>(ExecuteInviteToPlatformSession) : null);
		}
		else if (onlineStatus == MPLobbyPlayerBaseVM.OnlineStatus.Offline || onlineStatus == MPLobbyPlayerBaseVM.OnlineStatus.None)
		{
			result = null;
		}
		else if (state == AnotherPlayerState.AtLobby)
		{
			result = ExecuteInviteToParty;
		}
		return result;
	}

	private void ExecuteInviteToParty(PlayerId providedId)
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

	private void ExecuteInviteToPlatformSession(PlayerId providedId)
	{
		MPLobbyPlayerBaseVM friend = GetFriendWithID(providedId);
		friend.CanBeInvited = false;
		PlatformServices.Instance.CheckPrivilege(Privilege.Communication, displayResolveUI: true, delegate(bool result)
		{
			if (result)
			{
				PlatformServices.Instance.CheckPermissionWithUser(Permission.PlayMultiplayer, providedId, async delegate(bool permissionResult)
				{
					if (permissionResult)
					{
						if (PlatformServices.Instance.UsePlatformInvitationService(providedId) && (!NetworkMain.GameClient.IsInParty || NetworkMain.GameClient.IsPartyLeader))
						{
							Debug.Print("UsePlatformInvitationService InviteToPlatformSession");
							await NetworkMain.GameClient.InviteToPlatformSession(providedId);
						}
						else
						{
							bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(providedId);
							NetworkMain.GameClient.InviteToParty(providedId, dontUseNameForUnknownPlayer);
						}
					}
					else
					{
						string titleText = new TextObject("{=ZwN6rzTC}No permission").ToString();
						string text = new TextObject("{=wlz3eQWp}No permission to invite player.").ToString();
						InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", new TextObject("{=dismissnotification}Dismiss").ToString(), null, delegate
						{
							InformationManager.HideInquiry();
						}));
					}
					friend.CanBeInvited = true;
				});
			}
			else
			{
				friend.CanBeInvited = true;
			}
		});
	}

	private void ExecuteInviteToClan(PlayerId providedId)
	{
		PlatformServices.Instance.CheckPrivilege(Privilege.Clan, displayResolveUI: true, delegate(bool result)
		{
			if (result)
			{
				bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(providedId);
				NetworkMain.GameClient.InviteToClan(providedId, dontUseNameForUnknownPlayer);
			}
		});
	}
}
