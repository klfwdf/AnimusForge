namespace TaleWorlds.MountAndBlade;

public class TeamAiMultiplayerSiegeDefender : TeamAISiegeComponent
{
	public TeamAiMultiplayerSiegeDefender(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
	}

	public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
	{
	}
}
