using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService.Epic;

public class EpicFriendListService : IFriendListService
{
	private EpicPlatformServices _epicPlatformServices;

	bool IFriendListService.InGameStatusFetchable => false;

	bool IFriendListService.AllowsFriendOperations => false;

	bool IFriendListService.CanInvitePlayersToPlatformSession => false;

	bool IFriendListService.IncludeInAllFriends => true;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<PlayerId> OnFriendRemoved;

	public event Action OnFriendListChanged;

	public EpicFriendListService(EpicPlatformServices epicPlatformServices)
	{
		_epicPlatformServices = epicPlatformServices;
	}

	string IFriendListService.GetServiceCodeName()
	{
		return "Epic";
	}

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=!}Epic");
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.Epic;
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		return _epicPlatformServices.GetAllFriends();
	}

	Task<bool> IFriendListService.GetUserOnlineStatus(PlayerId providedId)
	{
		return _epicPlatformServices.GetUserOnlineStatus(providedId);
	}

	Task<bool> IFriendListService.IsPlayingThisGame(PlayerId providedId)
	{
		return _epicPlatformServices.IsPlayingThisGame(providedId);
	}

	Task<string> IFriendListService.GetUserName(PlayerId providedId)
	{
		return _epicPlatformServices.GetUserName(providedId);
	}

	Task<PlayerId> IFriendListService.GetUserWithName(string name)
	{
		return _epicPlatformServices.GetUserWithName(name);
	}

	public void UserStatusChanged(PlayerId playerId)
	{
		if (this.OnUserStatusChanged != null)
		{
			this.OnUserStatusChanged(default(PlayerId));
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
}
