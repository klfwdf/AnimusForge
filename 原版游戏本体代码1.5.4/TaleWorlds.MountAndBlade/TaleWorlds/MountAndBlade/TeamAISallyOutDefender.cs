using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TeamAISallyOutDefender : TeamAISiegeComponent
{
	public readonly Func<WorldPosition> DefensePosition;

	public List<ArcherPosition> ArcherPositions { get; }

	public TeamAISallyOutDefender(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		TeamAISallyOutDefender teamAISallyOutDefender = this;
		TeamAISiegeComponent.QuerySystem = new SiegeQuerySystem(Team, TeamAISiegeComponent.SiegeLanes);
		TeamAISiegeComponent.QuerySystem.Expire();
		DefensePosition = () => new WorldPosition(currentMission.Scene, UIntPtr.Zero, teamAISallyOutDefender.Ram.GameEntity.GlobalPosition, hasValidZ: false);
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
			formation.AI.AddAiBehavior(new BehaviorDefendSiegeWeapon(formation));
			formation.AI.AddAiBehavior(new BehaviorSparseSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorSkirmishLine(formation));
			formation.AI.AddAiBehavior(new BehaviorScreenedSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorProtectFlank(formation));
			formation.AI.AddAiBehavior(new BehaviorFlank(formation));
			formation.AI.AddAiBehavior(new BehaviorHorseArcherSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorDefend(formation));
		}
	}

	public Vec3 CalculateSallyOutReferencePosition(FormationAI.BehaviorSide side)
	{
		return side switch
		{
			FormationAI.BehaviorSide.Left => SiegeTowers.FirstOrDefault((SiegeTower st) => st.WeaponSide == FormationAI.BehaviorSide.Left)?.GameEntity.GlobalPosition ?? Ram.GameEntity.GlobalPosition, 
			FormationAI.BehaviorSide.Right => SiegeTowers.FirstOrDefault((SiegeTower st) => st.WeaponSide == FormationAI.BehaviorSide.Right)?.GameEntity.GlobalPosition ?? Ram.GameEntity.GlobalPosition, 
			_ => Ram.GameEntity.GlobalPosition, 
		};
	}

	public override void OnDeploymentFinished()
	{
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
