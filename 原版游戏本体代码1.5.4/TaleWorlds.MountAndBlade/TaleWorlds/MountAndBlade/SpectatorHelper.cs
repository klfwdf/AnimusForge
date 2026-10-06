namespace TaleWorlds.MountAndBlade;

public static class SpectatorHelper
{
	public static bool IsPeerSpectator(NetworkCommunicator networkPeer)
	{
		if (networkPeer == null)
		{
			return false;
		}
		if (networkPeer.IsSpectator)
		{
			return true;
		}
		Team team = Mission.Current?.SpectatorTeam;
		if (team == null)
		{
			return false;
		}
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component != null)
		{
			return component.Team == team;
		}
		return false;
	}

	public static bool IsLocalPeerSpectator()
	{
		return IsPeerSpectator(GameNetwork.MyPeer);
	}
}
