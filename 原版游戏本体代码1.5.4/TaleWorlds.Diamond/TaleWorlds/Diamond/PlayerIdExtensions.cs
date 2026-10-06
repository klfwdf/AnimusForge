using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond;

public static class PlayerIdExtensions
{
	public static PeerId ConvertToPeerId(this PlayerId playerId)
	{
		return new PeerId(playerId.ToByteArray());
	}

	public static PlayerId ConvertToPlayerId(this PeerId peerId)
	{
		return new PlayerId(peerId.ToByteArray());
	}
}
