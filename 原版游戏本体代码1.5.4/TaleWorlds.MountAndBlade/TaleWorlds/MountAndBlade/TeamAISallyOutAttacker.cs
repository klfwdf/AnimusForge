using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TeamAISallyOutAttacker : TeamAISiegeComponent
{
	public MBList<GameEntity> ArcherPositions;

	public readonly List<UsableMachine> BesiegerRangedSiegeWeapons;

	public TeamAISallyOutAttacker(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		ArcherPositions = currentMission.Scene.FindEntitiesWithTag("archer_position").ToMBList();
		BesiegerRangedSiegeWeapons = new List<UsableMachine>(from w in currentMission.ActiveMissionObjects.FindAllWithType<RangedSiegeWeapon>()
			where w.Side == BattleSideEnum.Attacker && !w.IsDisabled
			select w);
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
			formation.AI.AddAiBehavior(new BehaviorShootFromCastleWalls(formation));
			formation.AI.AddAiBehavior(new BehaviorDestroySiegeWeapons(formation));
			formation.AI.AddAiBehavior(new BehaviorSparseSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorDefend(formation));
			formation.AI.AddAiBehavior(new BehaviorRetreatToCastle(formation));
			formation.AI.AddAiBehavior(new BehaviorRetreatToKeep(formation));
			formation.AI.AddAiBehavior(new BehaviorDefendCastleKeyPosition(formation));
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		if (base.CurrentTactic != null)
		{
			base.CurrentTactic.ResetTactic();
		}
	}
}
