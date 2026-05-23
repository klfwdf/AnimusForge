using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerJoinGameResponseDataFromHost
{
	public PlayerId PlayerId { get; set; }

	public int PeerIndex { get; set; }

	public int SessionKey { get; set; }

	public bool IsAdmin { get; set; }

	public CustomGameJoinResponse CustomGameJoinResponse { get; set; }
}
