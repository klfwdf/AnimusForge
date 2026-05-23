namespace TaleWorlds.MountAndBlade;

public class BaseNetworkComponentData : UdpNetworkComponent
{
	public const float MaxIntermissionStateTime = 240f;

	public int CurrentBattleIndex { get; private set; }

	public void UpdateCurrentBattleIndex(int currentBattleIndex)
	{
		CurrentBattleIndex = currentBattleIndex;
	}
}
