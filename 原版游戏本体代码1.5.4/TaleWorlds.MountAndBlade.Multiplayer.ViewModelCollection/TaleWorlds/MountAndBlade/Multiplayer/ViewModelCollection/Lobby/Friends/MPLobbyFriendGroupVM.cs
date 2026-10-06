using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyFriendGroupVM : ViewModel
{
	private readonly struct FriendOperation
	{
		public enum OperationTypes
		{
			Add,
			Remove,
			Clear
		}

		public readonly MPLobbyFriendItemVM Friend;

		public readonly OperationTypes Type;

		public FriendOperation(OperationTypes type, MPLobbyFriendItemVM friend)
		{
			Type = type;
			Friend = friend;
		}
	}

	public enum FriendGroupType
	{
		InGame,
		Online,
		Offline,
		FriendRequests,
		PendingRequests
	}

	private readonly List<FriendOperation> _friendOperationQueue;

	private string _title;

	private FriendGroupType _groupType;

	private MBBindingList<MPLobbyFriendItemVM> _friendList;

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
	public FriendGroupType GroupType
	{
		get
		{
			return _groupType;
		}
		set
		{
			if (value != _groupType)
			{
				_groupType = value;
				OnPropertyChanged("GroupType");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyFriendItemVM> FriendList
	{
		get
		{
			return _friendList;
		}
		set
		{
			if (value != _friendList)
			{
				_friendList = value;
				OnPropertyChangedWithValue(value, "FriendList");
			}
		}
	}

	public MPLobbyFriendGroupVM(FriendGroupType groupType)
	{
		GroupType = groupType;
		_friendOperationQueue = new List<FriendOperation>();
		FriendList = new MBBindingList<MPLobbyFriendItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		switch (GroupType)
		{
		case FriendGroupType.InGame:
			Title = new TextObject("{=uUoSmCBS}In Bannerlord").ToString();
			break;
		case FriendGroupType.Online:
			Title = new TextObject("{=V305MaOP}Online").ToString();
			break;
		case FriendGroupType.Offline:
			Title = new TextObject("{=Zv1lg272}Offline").ToString();
			break;
		case FriendGroupType.FriendRequests:
			Title = new TextObject("{=K8CGzQYL}Received Requests").ToString();
			break;
		case FriendGroupType.PendingRequests:
			Title = new TextObject("{=QwbVdMLi}Sent Requests").ToString();
			break;
		}
		FriendList.ApplyActionOnAllItems(delegate(MPLobbyFriendItemVM x)
		{
			x.RefreshValues();
		});
	}

	public void Tick()
	{
		lock (_friendOperationQueue)
		{
			for (int i = 0; i < _friendOperationQueue.Count; i++)
			{
				HandleFriendOperationAux(_friendOperationQueue[i]);
			}
			_friendOperationQueue.Clear();
		}
	}

	private void HandleFriendOperationAux(FriendOperation operation)
	{
		switch (operation.Type)
		{
		case FriendOperation.OperationTypes.Add:
			FriendList.Add(operation.Friend);
			operation.Friend.UpdateNameAndAvatar();
			break;
		case FriendOperation.OperationTypes.Remove:
			FriendList.Remove(operation.Friend);
			break;
		case FriendOperation.OperationTypes.Clear:
			FriendList.Clear();
			break;
		}
	}

	private void EnqueueFriendOperation(FriendOperation operation)
	{
		lock (_friendOperationQueue)
		{
			_friendOperationQueue.Add(operation);
		}
	}

	public void ClearFriends()
	{
		EnqueueFriendOperation(new FriendOperation(FriendOperation.OperationTypes.Clear, null));
	}

	public void AddFriend(MPLobbyFriendItemVM player)
	{
		if (player.ProvidedID != NetworkMain.GameClient.PlayerID)
		{
			EnqueueFriendOperation(new FriendOperation(FriendOperation.OperationTypes.Add, player));
		}
	}

	public void RemoveFriend(MPLobbyFriendItemVM player)
	{
		if (player.ProvidedID != NetworkMain.GameClient.PlayerID)
		{
			EnqueueFriendOperation(new FriendOperation(FriendOperation.OperationTypes.Remove, player));
		}
	}
}
