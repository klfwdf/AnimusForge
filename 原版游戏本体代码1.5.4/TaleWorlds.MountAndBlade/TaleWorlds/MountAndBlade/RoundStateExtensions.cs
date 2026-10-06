namespace TaleWorlds.MountAndBlade;

public static class RoundStateExtensions
{
	public static bool StateHasVisualTimer(this MultiplayerRoundState roundState)
	{
		if ((uint)(roundState - 1) <= 1u)
		{
			return true;
		}
		return false;
	}
}
