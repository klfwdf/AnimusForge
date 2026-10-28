using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TeamAIGeneral : TeamAIComponent
{
	private int _numberOfEnemiesInShootRange;

	private int _numberOfEnemiesCloseToAttack;

	public TeamAIGeneral(Mission currentMission, Team currentTeam, float thinkTimerTime = 10f, float applyTimerTime = 1f)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
	}

	public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
	{
		if (GameNetwork.IsServer)
		{
			formation.ForceCalculateCaches();
			if (formation.AI.GetBehavior<BehaviorCharge>() == null)
			{
				if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
				{
					formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
				}
				else if (formation.FormationIndex == FormationClass.Bodyguard)
				{
					formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));
				}
				formation.AI.AddAiBehavior(new BehaviorCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
				formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
				formation.AI.AddAiBehavior(new BehaviorReserve(formation));
				formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
				formation.AI.AddAiBehavior(new BehaviorStop(formation));
				formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPInfantry(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPLastFlagLastStand(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPMounted(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPMountedRanged(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPRanged(formation));
			}
		}
		else
		{
			if (GameNetwork.IsClientOrReplay)
			{
				return;
			}
			formation.ForceCalculateCaches();
			if (formation.AI.GetBehavior<BehaviorCharge>() == null)
			{
				if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
				{
					formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
				}
				else if (formation.FormationIndex == FormationClass.Bodyguard)
				{
					formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));
				}
				formation.AI.AddAiBehavior(new BehaviorCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
				formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
				formation.AI.AddAiBehavior(new BehaviorReserve(formation));
				formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
				formation.AI.AddAiBehavior(new BehaviorStop(formation));
				formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorAdvance(formation));
				formation.AI.AddAiBehavior(new BehaviorCautiousAdvance(formation));
				formation.AI.AddAiBehavior(new BehaviorCavalryScreen(formation));
				formation.AI.AddAiBehavior(new BehaviorDefend(formation));
				formation.AI.AddAiBehavior(new BehaviorDefensiveRing(formation));
				formation.AI.AddAiBehavior(new BehaviorFireFromInfantryCover(formation));
				formation.AI.AddAiBehavior(new BehaviorFlank(formation));
				formation.AI.AddAiBehavior(new BehaviorHoldHighGround(formation));
				formation.AI.AddAiBehavior(new BehaviorHorseArcherSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorMountedSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorProtectFlank(formation));
				formation.AI.AddAiBehavior(new BehaviorScreenedSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmishBehindFormation(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmishLine(formation));
				formation.AI.AddAiBehavior(new BehaviorVanguard(formation));
				formation.AI.AddAiBehavior(new BehaviorShootFromCliff(formation));
			}
		}
	}

	private void UpdateVariables()
	{
		TeamQuerySystem querySystem = Team.QuerySystem;
		_numberOfEnemiesInShootRange = 0;
		_numberOfEnemiesCloseToAttack = 0;
		Vec2 averagePosition = querySystem.AveragePosition;
		foreach (Agent agent in Mission.Agents)
		{
			if (!agent.IsMount && agent.Team.IsValid && agent.Team.IsEnemyOf(Team))
			{
				float num = agent.Position.DistanceSquared(new Vec3(averagePosition.x, averagePosition.y));
				if (num < 40000f)
				{
					_numberOfEnemiesInShootRange++;
				}
				if (num < 1600f)
				{
					_numberOfEnemiesCloseToAttack++;
				}
			}
		}
	}

	public override void OnDeploymentFinished()
	{
		foreach (Formation item in Team.FormationsIncludingEmpty)
		{
			item.OnDeploymentFinished();
		}
	}

	protected override void DebugTick(float dt)
	{
	}
}
