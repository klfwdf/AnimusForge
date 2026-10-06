using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions.Handlers;

namespace TaleWorlds.MountAndBlade;

public class BattleDeploymentMissionController : DeploymentMissionController
{
	protected DefaultBattleMissionAgentSpawnLogic MissionAgentSpawnLogic;

	private BattleDeploymentHandler _battleDeploymentHandler;

	public BattleDeploymentMissionController(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_battleDeploymentHandler = base.Mission.GetMissionBehavior<BattleDeploymentHandler>();
		MissionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
	}

	protected override void OnAfterStart()
	{
		for (int i = 0; i < 2; i++)
		{
			MissionAgentSpawnLogic.SetSpawnTroops((BattleSideEnum)i, spawnTroops: false);
		}
		MissionAgentSpawnLogic.SetReinforcementsSpawnEnabled(value: false);
	}

	protected override void OnSetupTeamsOfSide(BattleSideEnum battleSide)
	{
		MissionAgentSpawnLogic.SetSpawnTroops(battleSide, spawnTroops: true, enforceSpawning: true);
		SetupAgentAIStatesForSide(battleSide);
		MissionAgentSpawnLogic.OnSideDeploymentOver(battleSide);
	}

	protected override void OnSetupTeamsFinished()
	{
		_battleDeploymentHandler.HandleGeneralsDeploymentFrames();
		base.Mission.IsTeleportingAgents = true;
	}

	protected override void BeforeDeploymentFinished()
	{
		base.Mission.IsTeleportingAgents = false;
	}

	protected override void AfterDeploymentFinished()
	{
		MissionAgentSpawnLogic.SetReinforcementsSpawnEnabled(value: true);
		base.Mission.RemoveMissionBehavior(_battleDeploymentHandler);
	}
}
