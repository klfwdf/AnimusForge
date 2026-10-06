using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TeamAISiegeDefender : TeamAISiegeComponent
{
	public const float InsideEnemyThresholdRatio = 0.5f;

	public Vec3 MurderHolePosition;

	public List<ArcherPosition> ArcherPositions { get; }

	public TeamAISiegeDefender(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		TeamAISiegeComponent.QuerySystem = new SiegeQuerySystem(Team, TeamAISiegeComponent.SiegeLanes);
		TeamAISiegeComponent.QuerySystem.Expire();
		IEnumerable<GameEntity> source = from ap in currentMission.Scene.FindEntitiesWithTag("archer_position")
			where ap.Parent == null || ap.Parent.IsVisibleIncludeParents()
			select ap;
		ArcherPositions = source.Select((GameEntity ap) => new ArcherPosition(ap, TeamAISiegeComponent.QuerySystem, BattleSideEnum.Defender)).ToList();
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
			formation.AI.AddAiBehavior(new BehaviorDefendCastleKeyPosition(formation));
			formation.AI.AddAiBehavior(new BehaviorEliminateEnemyInsideCastle(formation));
			formation.AI.AddAiBehavior(new BehaviorRetakeCastleKeyPosition(formation));
			formation.AI.AddAiBehavior(new BehaviorRetreatToKeep(formation));
			formation.AI.AddAiBehavior(new BehaviorSallyOut(formation));
			formation.AI.AddAiBehavior(new BehaviorUseMurderHole(formation));
			formation.AI.AddAiBehavior(new BehaviorShootFromCastleWalls(formation));
			formation.AI.AddAiBehavior(new BehaviorSparseSkirmish(formation));
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		foreach (SiegeTower siegeTower in SiegeTowers)
		{
			base.DifficultNavmeshIDs.AddRange(siegeTower.CollectGetDifficultNavmeshIDsForDefenders());
		}
		List<SiegeLane> list = TeamAISiegeComponent.SiegeLanes.ToList();
		TeamAISiegeComponent.SiegeLanes.Clear();
		int i;
		for (i = 0; i < 3; i++)
		{
			TeamAISiegeComponent.SiegeLanes.Add(new SiegeLane((FormationAI.BehaviorSide)i, TeamAISiegeComponent.QuerySystem));
			SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes[i];
			siegeLane.SetPrimarySiegeWeapons(base.PrimarySiegeWeapons.Where((IPrimarySiegeWeapon psw) => psw.WeaponSide == (FormationAI.BehaviorSide)i).ToList());
			siegeLane.SetDefensePoints((from dp in CastleKeyPositions
				where (dp as ICastleKeyPosition).DefenseSide == (FormationAI.BehaviorSide)i
				select dp as ICastleKeyPosition).ToList());
			siegeLane.RefreshLane();
			siegeLane.DetermineLaneState();
			siegeLane.DetermineOrigins();
			if (i < list.Count)
			{
				for (int num = 0; num < Mission.Current.Teams.Count; num++)
				{
					siegeLane.SetLastAssignedFormation(num, list[i].GetLastAssignedFormation(num));
				}
			}
		}
		TeamAISiegeComponent.QuerySystem = new SiegeQuerySystem(Team, TeamAISiegeComponent.SiegeLanes);
		TeamAISiegeComponent.QuerySystem.Expire();
		TeamAISiegeComponent.SiegeLanes.ForEach(delegate(SiegeLane sl)
		{
			sl.SetSiegeQuerySystem(TeamAISiegeComponent.QuerySystem);
		});
		ArcherPositions.ForEach(delegate(ArcherPosition ap)
		{
			ap.OnDeploymentFinished(TeamAISiegeComponent.QuerySystem, BattleSideEnum.Defender);
		});
	}
}
