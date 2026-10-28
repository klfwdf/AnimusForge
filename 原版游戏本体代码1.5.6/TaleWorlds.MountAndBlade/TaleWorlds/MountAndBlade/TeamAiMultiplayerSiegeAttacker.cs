namespace TaleWorlds.MountAndBlade;

public class TeamAiMultiplayerSiegeAttacker : TeamAISiegeComponent
{
	public TeamAiMultiplayerSiegeAttacker(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
	}

	public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
	{
	}
}
