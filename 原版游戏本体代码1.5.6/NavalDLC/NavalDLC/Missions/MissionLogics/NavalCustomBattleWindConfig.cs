namespace NavalDLC.Missions.MissionLogics;

public class NavalCustomBattleWindConfig
{
	public enum Direction
	{
		TowardsDefender,
		TowardsAttacker,
		Side,
		Random
	}

	public static Direction WindDirection;
}
