namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public static class MultiplayerSpectatorHelper
{
	public static bool IsLocalPeerSpectator()
	{
		return GameNetwork.MyPeer?.GetComponent<MissionPeer>()?.IsSpectator ?? false;
	}

	public static bool IsLocalPeerStreamer()
	{
		return GameNetwork.IsMyPeerStreamer;
	}
}
