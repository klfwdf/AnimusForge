using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class RecentPlayersFriendListService : BannerlordFriendListService, IFriendListService
{
	bool IFriendListService.IncludeInAllFriends => false;

	bool IFriendListService.CanInvitePlayersToPlatformSession => PlatformServices.InvitationServices != null;

	TextObject IFriendListService.GetServiceLocalizedName()
	{
		return new TextObject("{=XvSRoOzM}Recently Played Players");
	}

	string IFriendListService.GetServiceCodeName()
	{
		return "RecentlyPlayedPlayers";
	}

	IEnumerable<PlayerId> IFriendListService.GetAllFriends()
	{
		return RecentPlayersManager.GetPlayersOrdered();
	}

	FriendListServiceType IFriendListService.GetFriendListServiceType()
	{
		return FriendListServiceType.RecentPlayers;
	}
}
