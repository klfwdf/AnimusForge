using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Galaxy.Api;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService.GOG;

public class GOGFriendListService : IFriendListService
{
	private GOGPlatformServices _gogPlatformServices;

	bool IFriendListService.InGameStatusFetchable => false;

	bool IFriendListService.AllowsFriendOperations => false;

	bool IFriendListService.CanInvitePlayersToPlatformSession => false;

	bool IFriendListService.IncludeInAllFriends => true;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<PlayerId> OnFriendRemoved;

	public event Action OnFriendListChanged;

	public GOGFriendListService(GOGPlatformServices gogPlatformServices)
	{
		_gogPlatformServices = gogPlatformServices;
	}

	string IFriendListService.GetServiceCodeName()
	{
		return "GOG";
	}

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=!}GOG");
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.GOG;
	}

	public void RequestFriendList()
	{
		IFriends friends = GalaxyInstance.Friends();
		FriendListListener friendListListener = new FriendListListener();
		friends.RequestFriendList(friendListListener);
		while (!friendListListener.GotResult)
		{
			GalaxyInstance.ProcessData();
			Thread.Sleep(5);
		}
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		if (GalaxyInstance.User().IsLoggedOn())
		{
			IFriends friends = GalaxyInstance.Friends();
			int friendCount = (int)friends.GetFriendCount();
			int i = 0;
			while (i < friendCount)
			{
				yield return friends.GetFriendByIndex((uint)i).ToPlayerId();
				int num = i + 1;
				i = num;
			}
		}
	}

	Task<bool> IFriendListService.GetUserOnlineStatus(PlayerId providedId)
	{
		return Task.FromResult(result: false);
	}

	Task<bool> IFriendListService.IsPlayingThisGame(PlayerId providedId)
	{
		return Task.FromResult(result: false);
	}

	async Task<string> IFriendListService.GetUserName(PlayerId providedId)
	{
		return await _gogPlatformServices.GetUserName(providedId);
	}

	Task<PlayerId> IFriendListService.GetUserWithName(string name)
	{
		return Task.FromResult(PlayerId.Empty);
	}

	internal void HandleOnUserStatusChanged(PlayerId playerId)
	{
		if (this.OnUserStatusChanged != null)
		{
			this.OnUserStatusChanged(playerId);
		}
	}

	private void Dummy()
	{
		if (this.OnFriendRemoved != null)
		{
			this.OnFriendRemoved(default(PlayerId));
		}
		if (this.OnFriendListChanged != null)
		{
			this.OnFriendListChanged();
		}
	}

	IEnumerable<PlayerId> IFriendListService.GetPendingRequests()
	{
		return null;
	}

	IEnumerable<PlayerId> IFriendListService.GetReceivedRequests()
	{
		return null;
	}
}
