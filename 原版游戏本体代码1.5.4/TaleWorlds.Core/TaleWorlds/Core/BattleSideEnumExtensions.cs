namespace TaleWorlds.Core;

public static class BattleSideEnumExtensions
{
	public static bool IsValid(this BattleSideEnum battleSide)
	{
		if (battleSide >= BattleSideEnum.Defender)
		{
			return battleSide < BattleSideEnum.NumSides;
		}
		return false;
	}
}
