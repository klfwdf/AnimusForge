namespace TaleWorlds.MountAndBlade;

public interface IBattlePowerCalculationLogic : IMissionBehavior
{
	float GetTotalTeamPower(Team team);
}
