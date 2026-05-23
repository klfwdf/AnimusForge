using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class BannerlordFriendListService : IFriendListService
{
	protected List<FriendInfo> Friends;

	bool IFriendListService.InGameStatusFetchable => false;

	bool IFriendListService.AllowsFriendOperations => true;

	bool IFriendListService.CanInvitePlayersToPlatformSession => PlatformServices.InvitationServices != null;

	bool IFriendListService.IncludeInAllFriends => true;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<PlayerId> OnFriendRemoved;

	public event Action OnFriendListChanged;

	public BannerlordFriendListService()
	{
		Friends = new List<FriendInfo>();
	}

	string IFriendListService.GetServiceCodeName()
	{
		return "TaleWorlds";
	}

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=!}TaleWorlds");
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.Bannerlord;
	}

	IEnumerable<PlayerId> IFriendListService.GetPendingRequests()
	{
		return from f in Friends
			where f.Status == FriendStatus.Pending
			select f.Id;
	}

	IEnumerable<PlayerId> IFriendListService.GetReceivedRequests()
	{
		return from f in Friends
			where f.Status == FriendStatus.Received
			select f.Id;
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		return from f in Friends
			where f.Status == FriendStatus.Accepted
			select f.Id;
	}

	Task<bool> IFriendListService.GetUserOnlineStatus(PlayerId providedId)
	{
		foreach (FriendInfo friend in Friends)
		{
			if (friend.Id.Equals(providedId))
			{
				return Task.FromResult(friend.IsOnline);
			}
		}
		return Task.FromResult(result: false);
	}

	Task<bool> IFriendListService.IsPlayingThisGame(PlayerId providedId)
	{
		return ((IFriendListService)this).GetUserOnlineStatus(providedId);
	}

	Task<string> IFriendListService.GetUserName(PlayerId providedId)
	{
		foreach (FriendInfo friend in Friends)
		{
			if (friend.Id.Equals(providedId))
			{
				return Task.FromResult(friend.Name);
			}
		}
		return Task.FromResult<string>(null);
	}

	Task<PlayerId> IFriendListService.GetUserWithName(string name)
	{
		foreach (FriendInfo friend in Friends)
		{
			if (friend.Name == name)
			{
				return Task.FromResult(friend.Id);
			}
		}
		return Task.FromResult(default(PlayerId));
	}

	public void OnFriendListReceived(FriendInfo[] friends)
	{
		List<FriendInfo> friends2 = Friends;
		Friends = new List<FriendInfo>(friends);
		List<PlayerId> list = null;
		bool flag = false;
		foreach (FriendInfo friend in Friends)
		{
			int num = friends2.FindIndex((FriendInfo o) => o.Id.Equals(friend.Id));
			if (num < 0)
			{
				flag = true;
			}
			else
			{
				FriendInfo friendInfo = friends2[num];
				friends2.RemoveAt(num);
				if (friendInfo.Status != friend.Status)
				{
					flag = true;
				}
				else if (friendInfo.IsOnline != friend.IsOnline)
				{
					if (list == null)
					{
						list = new List<PlayerId>();
					}
					list.Add(friendInfo.Id);
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (flag)
		{
			this.OnFriendListChanged?.Invoke();
			return;
		}
		if (friends2.Count > 0)
		{
			foreach (FriendInfo item in friends2)
			{
				this.OnFriendRemoved?.Invoke(item.Id);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (PlayerId item2 in list)
		{
			this.OnUserStatusChanged?.Invoke(item2);
		}
	}
}
