using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService;

public interface IFriendListService
{
	bool InGameStatusFetchable { get; }

	bool AllowsFriendOperations { get; }

	bool CanInvitePlayersToPlatformSession { get; }

	bool IncludeInAllFriends { get; }

	event Action<PlayerId> OnUserStatusChanged;

	event Action<PlayerId> OnFriendRemoved;

	event Action OnFriendListChanged;

	string GetServiceCodeName();

	TextObject GetServiceLocalizedName();

	FriendListServiceType GetFriendListServiceType();

	IEnumerable<PlayerId> GetAllFriends();

	Task<bool> GetUserOnlineStatus(PlayerId providedId);

	Task<bool> IsPlayingThisGame(PlayerId providedId);

	Task<string> GetUserName(PlayerId providedId);

	Task<PlayerId> GetUserWithName(string name);

	IEnumerable<PlayerId> GetPendingRequests();

	IEnumerable<PlayerId> GetReceivedRequests();
}
