using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TeamAISiegeAttacker : TeamAISiegeComponent
{
	private readonly MBList<ArcherPosition> _archerPositions;

	public MBReadOnlyList<ArcherPosition> ArcherPositions => _archerPositions;

	public TeamAISiegeAttacker(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		IEnumerable<GameEntity> source = currentMission.Scene.FindEntitiesWithTag("archer_position_attacker");
		_archerPositions = source.Select((GameEntity ap) => new ArcherPosition(ap, TeamAISiegeComponent.QuerySystem, BattleSideEnum.Attacker)).ToMBList();
	}

	public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
	{
		if (formation.AI.GetBehavior<BehaviorCharge>() == null)
		{
			formation.ForceCalculateCaches();
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
			formation.AI.AddAiBehavior(new BehaviorAssaultWalls(formation));
			formation.AI.AddAiBehavior(new BehaviorShootFromSiegeTower(formation));
			formation.AI.AddAiBehavior(new BehaviorUseSiegeMachines(formation));
			formation.AI.AddAiBehavior(new BehaviorWaitForLadders(formation));
			formation.AI.AddAiBehavior(new BehaviorSparseSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorRetreatToKeep(formation));
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		foreach (SiegeTower siegeTower in SiegeTowers)
		{
			base.DifficultNavmeshIDs.AddRange(siegeTower.CollectGetDifficultNavmeshIDsForAttackers());
		}
		foreach (ArcherPosition archerPosition in _archerPositions)
		{
			archerPosition.OnDeploymentFinished(TeamAISiegeComponent.QuerySystem, BattleSideEnum.Attacker);
		}
	}

	public override void OnFormationFrameChanged(Agent agent, bool isFrameEnabled, WorldPosition frame)
	{
		base.OnFormationFrameChanged(agent, isFrameEnabled, frame);
		foreach (SiegeTower siegeTower in SiegeTowers)
		{
			if (agent.IsInLadderQueue || siegeTower.HasCompletedAction())
			{
				siegeTower.OnFormationFrameChanged(agent, isFrameEnabled, frame);
			}
		}
		foreach (SiegeLadder ladder in base.Ladders)
		{
			if (agent.IsInLadderQueue || ladder.State == SiegeLadder.LadderState.OnWall)
			{
				ladder.OnFormationFrameChanged(agent, isFrameEnabled, frame);
			}
		}
	}
}
