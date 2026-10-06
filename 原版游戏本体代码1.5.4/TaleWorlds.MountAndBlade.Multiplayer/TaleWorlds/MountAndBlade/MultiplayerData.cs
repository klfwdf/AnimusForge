using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerData : MBMultiplayerData
{
	public readonly int AutoTeamBalanceLimit = 50;

	public MultiplayerData()
	{
		new List<NetworkCommunicator>();
	}

	public bool IsMultiplayerTeamAvailable(int peerNo, int teamNo)
	{
		return false;
	}
}
