namespace TaleWorlds.Core;

public static class TeamSideEnumExtensions
{
	public static bool IsValid(this TeamSideEnum teamSide)
	{
		if (teamSide >= TeamSideEnum.PlayerTeam)
		{
			return teamSide < TeamSideEnum.NumSides;
		}
		return false;
	}
}
