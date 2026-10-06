using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService.Epic;

public class EpicThirdPartyFriendListService : IFriendListService
{
	bool IFriendListService.InGameStatusFetchable => false;

	bool IFriendListService.AllowsFriendOperations => false;

	bool IFriendListService.CanInvitePlayersToPlatformSession => false;

	bool IFriendListService.IncludeInAllFriends => true;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<PlayerId> OnFriendRemoved;

	public event Action OnFriendListChanged;

	string IFriendListService.GetServiceCodeName()
	{
		return "EpicThirdParty";
	}

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=!}Epic Third Party");
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.EpicThirdParty;
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		return null;
	}

	Task<bool> IFriendListService.GetUserOnlineStatus(PlayerId providedId)
	{
		return Task.FromResult(result: false);
	}

	Task<bool> IFriendListService.IsPlayingThisGame(PlayerId providedId)
	{
		return Task.FromResult(result: false);
	}

	Task<string> IFriendListService.GetUserName(PlayerId providedId)
	{
		return Task.FromResult("-");
	}

	Task<PlayerId> IFriendListService.GetUserWithName(string name)
	{
		return Task.FromResult(PlayerId.Empty);
	}

	private void Dummy()
	{
		if (this.OnUserStatusChanged != null)
		{
			this.OnUserStatusChanged(default(PlayerId));
		}
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
