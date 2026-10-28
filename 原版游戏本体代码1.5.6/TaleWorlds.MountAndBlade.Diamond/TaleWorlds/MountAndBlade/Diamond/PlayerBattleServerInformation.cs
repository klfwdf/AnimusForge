using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerBattleServerInformation
{
	public int PeerIndex { get; set; }

	public int SessionKey { get; set; }

	public PlayerBattleServerInformation()
	{
	}

	public PlayerBattleServerInformation(int peerIndex, int sessionKey)
	{
		PeerIndex = peerIndex;
		SessionKey = sessionKey;
	}
}
