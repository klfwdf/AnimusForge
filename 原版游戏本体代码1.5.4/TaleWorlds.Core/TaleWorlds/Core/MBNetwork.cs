namespace TaleWorlds.Core;

public static class MBNetwork
{
	public static INetworkCommunication NetworkViewCommunication { get; private set; }

	public static VirtualPlayer MyPeer
	{
		get
		{
			if (NetworkViewCommunication != null)
			{
				return NetworkViewCommunication.MyPeer;
			}
			return null;
		}
	}

	public static void Initialize(INetworkCommunication networkCommunication)
	{
		NetworkViewCommunication = networkCommunication;
	}
}
