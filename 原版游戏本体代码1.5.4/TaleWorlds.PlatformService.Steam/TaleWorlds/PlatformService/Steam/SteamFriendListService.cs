using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService.Steam;

public class SteamFriendListService : IFriendListService
{
	private SteamPlatformServices _steamPlatformServices;

	bool IFriendListService.InGameStatusFetchable => true;

	bool IFriendListService.AllowsFriendOperations => false;

	bool IFriendListService.CanInvitePlayersToPlatformSession => false;

	bool IFriendListService.IncludeInAllFriends => true;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<PlayerId> OnFriendRemoved;

	public event Action OnFriendListChanged;

	public SteamFriendListService(SteamPlatformServices steamPlatformServices)
	{
		_steamPlatformServices = steamPlatformServices;
	}

	string IFriendListService.GetServiceCodeName()
	{
		return "Steam";
	}

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=!}Steam");
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.Steam;
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		if (SteamAPI.IsSteamRunning() && _steamPlatformServices.Initialized)
		{
			int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
			int i = 0;
			while (i < friendCount)
			{
				yield return SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate).ToPlayerId();
				int num = i + 1;
				i = num;
			}
		}
	}

	Task<bool> IFriendListService.GetUserOnlineStatus(PlayerId providedId)
	{
		return _steamPlatformServices.GetUserOnlineStatus(providedId);
	}

	Task<bool> IFriendListService.IsPlayingThisGame(PlayerId providedId)
	{
		return _steamPlatformServices.IsPlayingThisGame(providedId);
	}

	Task<string> IFriendListService.GetUserName(PlayerId providedId)
	{
		return _steamPlatformServices.GetUserName(providedId);
	}

	Task<PlayerId> IFriendListService.GetUserWithName(string name)
	{
		return _steamPlatformServices.GetUserWithName(name);
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
