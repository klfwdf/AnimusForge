using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public static class MultiplayerPlayerHelper
{
	private static IReadOnlyCollection<PlayerId> PlatformBlocks => PlatformServices.Instance.BlockedUsers;

	public static bool IsBlocked(PlayerId playerID)
	{
		if (!PermaMuteList.IsPlayerMuted(playerID))
		{
			if (PlatformBlocks != null)
			{
				return PlatformBlocks.Contains(playerID);
			}
			return false;
		}
		return true;
	}
}
