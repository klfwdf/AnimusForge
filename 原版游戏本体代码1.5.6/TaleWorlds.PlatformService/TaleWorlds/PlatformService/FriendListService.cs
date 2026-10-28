using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService;

public static class FriendListService
{
	public static IEnumerable<PlayerId> GetAllFriendsInAllPlatforms()
	{
		IFriendListService[] friendListServices = PlatformServices.Instance.GetFriendListServices();
		IFriendListService[] array = friendListServices;
		foreach (IFriendListService friendListService in array)
		{
			if (!friendListService.IncludeInAllFriends)
			{
				continue;
			}
			IEnumerable<PlayerId> allFriends = friendListService.GetAllFriends();
			if (allFriends == null)
			{
				continue;
			}
			foreach (PlayerId item in allFriends)
			{
				yield return item;
			}
		}
	}
}
