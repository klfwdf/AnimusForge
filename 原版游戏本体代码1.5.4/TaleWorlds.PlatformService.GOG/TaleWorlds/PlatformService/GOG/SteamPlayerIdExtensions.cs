using Galaxy.Api;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService.GOG;

public static class SteamPlayerIdExtensions
{
	public static PlayerId ToPlayerId(this GalaxyID galaxyID)
	{
		return new PlayerId(5, 0uL, galaxyID.ToUint64());
	}

	public static GalaxyID ToGOGID(this PlayerId playerId)
	{
		if (playerId.IsValidGOGId())
		{
			return new GalaxyID(playerId.Part4);
		}
		return new GalaxyID(0uL);
	}

	public static bool IsValidGOGId(this PlayerId playerId)
	{
		if (playerId.IsValid)
		{
			return playerId.ProvidedType == PlayerIdProvidedTypes.GOG;
		}
		return false;
	}
}
