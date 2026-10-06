namespace TaleWorlds.MountAndBlade;

public class MultiplayerInfo
{
	protected MultiplayerData multiplayerDataValues;

	public MultiplayerData MultiplayerDataValues => multiplayerDataValues;

	public MultiplayerInfo()
	{
		multiplayerDataValues = new MultiplayerData();
	}

	public bool IsMultiplayerTeamAvailable(int peerNo, int teamNo)
	{
		return true;
	}
}
