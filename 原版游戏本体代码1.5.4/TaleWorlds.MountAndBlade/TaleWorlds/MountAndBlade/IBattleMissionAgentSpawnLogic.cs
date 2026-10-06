namespace TaleWorlds.MountAndBlade;

public interface IBattleMissionAgentSpawnLogic : IMissionAgentSpawnLogic, IMissionBehavior
{
	int TotalSpawnNumber { get; }

	int BattleSize { get; }

	int NumberOfAgents { get; }

	MissionSpawnPhase DefenderActivePhase { get; }

	MissionSpawnPhase AttackerActivePhase { get; }

	ref readonly MissionSpawnSettings SpawnSettings { get; }

	IMissionDeploymentPlan DeploymentPlan { get; }
}
