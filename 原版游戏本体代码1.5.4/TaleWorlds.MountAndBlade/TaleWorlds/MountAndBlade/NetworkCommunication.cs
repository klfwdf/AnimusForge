using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class NetworkCommunication : INetworkCommunication
{
	VirtualPlayer INetworkCommunication.MyPeer => GameNetwork.MyPeer?.VirtualPlayer;
}
