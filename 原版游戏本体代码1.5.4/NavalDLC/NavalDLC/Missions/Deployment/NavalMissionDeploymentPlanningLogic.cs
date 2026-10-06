using System.Linq;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Deployment;

public class NavalMissionDeploymentPlanningLogic : MissionDeploymentPlanningLogic
{
	private Mission _mission;

	private MBList<(Team team, NavalTeamDeploymentPlan plan)> _teamDeploymentPlans = new MBList<(Team, NavalTeamDeploymentPlan)>();

	public NavalMissionDeploymentPlanningLogic(Mission mission)
	{
		_mission = mission;
	}

	public override void Initialize()
	{
		_teamDeploymentPlans.Clear();
		foreach (Team team in _mission.Teams)
		{
			NavalTeamDeploymentPlan item = new NavalTeamDeploymentPlan(_mission, team);
			_teamDeploymentPlans.Add((team, item));
		}
	}

	public override void ClearDeploymentPlan(Team team)
	{
		GetTeamPlan(team).ClearPlan();
	}

	public override bool SupportsReinforcements()
	{
		return false;
	}

	public override void UpdateReinforcementPlan(Team team)
	{
		Debug.FailedAssert("Naval mission deployment planning logic does not support reinforcements plans that can be updated", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Deployment\\NavalMissionDeploymentPlanningLogic.cs", "UpdateReinforcementPlan", 43);
	}

	public override bool SupportsNavmesh(Team team)
	{
		return false;
	}

	public override bool HasPlayerSpawnFrame(BattleSideEnum battleSide)
	{
		return false;
	}

	public override bool GetPlayerSpawnFrame(BattleSideEnum battleSide, out WorldPosition position, out Vec2 direction)
	{
		position = WorldPosition.Invalid;
		direction = Vec2.Invalid;
		return false;
	}

	public void ClearAddedShips(Team team)
	{
		GetTeamPlan(team).ClearAddedShips();
	}

	public override void ClearAll()
	{
		foreach (var teamDeploymentPlan in _teamDeploymentPlans)
		{
			teamDeploymentPlan.plan.ClearAddedShips();
			teamDeploymentPlan.plan.ClearPlan();
		}
	}

	public void AddShip(Team team, FormationClass formationIndex, IShipOrigin shipOrigin)
	{
		GetTeamPlan(team).AddShip(formationIndex, shipOrigin);
	}

	public bool RemoveShip(Team team, FormationClass formationIndex)
	{
		return GetTeamPlan(team).RemoveShip(formationIndex);
	}

	public override void MakeDeploymentPlan(Team team, float spawnPathOffset = 0f, float targetOffset = 0f)
	{
		NavalTeamDeploymentPlan teamPlan = GetTeamPlan(team);
		if (!IsPlanMade(team))
		{
			teamPlan.MakeDeploymentPlan(spawnPathOffset, targetOffset);
			if (IsPlanMade(team, out var isFirstPlan))
			{
				_mission.OnDeploymentPlanMade(team, isFirstPlan);
			}
		}
	}

	public override bool RemakeDeploymentPlan(Team team)
	{
		IsPlanMade(team);
		float spawnPathOffset = GetSpawnPathOffset(team);
		float targetOffset = GetTargetOffset(team);
		ClearAddedShips(team);
		ClearDeploymentPlan(team);
		NavalShipsLogic missionBehavior = _mission.GetMissionBehavior<NavalShipsLogic>();
		for (int i = 0; i < 11; i++)
		{
			FormationClass formationIndex = (FormationClass)i;
			ShipAssignment shipAssignment = missionBehavior.GetShipAssignment(team.TeamSide, formationIndex);
			if (shipAssignment.IsSet)
			{
				AddShip(team, formationIndex, shipAssignment.ShipOrigin);
			}
		}
		MakeDeploymentPlan(team, spawnPathOffset, targetOffset);
		return IsPlanMade(team);
	}

	public override bool IsPositionInsideDeploymentBoundaries(Team team, in Vec2 position)
	{
		(string, MBList<Vec2>) containingBoundaryTuple;
		return GetTeamPlan(team).IsPositionInsideDeploymentBoundaries(in position, out containingBoundaryTuple);
	}

	public override Vec2 GetClosestDeploymentBoundaryPosition(Team team, in Vec2 position)
	{
		return GetTeamPlan(team).GetClosestDeploymentBoundaryPosition(in position);
	}

	public override void ProjectPositionToDeploymentBoundaries(Team team, ref WorldPosition position)
	{
		Debug.FailedAssert("Naval deployment plan does not support projection of position to deployment boundaries as it does not support a navmesh", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Deployment\\NavalMissionDeploymentPlanningLogic.cs", "ProjectPositionToDeploymentBoundaries", 161);
	}

	public override bool GetPathDeploymentBoundaryIntersection(Team team, in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition intersection)
	{
		Debug.FailedAssert("Naval deployment plan does not support finding boundary intersection between positions as it does not support a navmesh", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Deployment\\NavalMissionDeploymentPlanningLogic.cs", "GetPathDeploymentBoundaryIntersection", 167);
		intersection = WorldPosition.Invalid;
		return false;
	}

	public override float GetSpawnPathOffset(Team team)
	{
		return GetTeamPlan(team).GetSpawnPathOffset();
	}

	public override MatrixFrame GetZoomFocusFrame(Team team)
	{
		Vec2 halfExtents;
		return GetFormationsCenterFrameAndExtents(team, out halfExtents);
	}

	public override float GetZoomOffset(Team team, float fovAngle)
	{
		NavalTeamDeploymentPlan teamPlan = GetTeamPlan(team);
		Vec2 halfExtents;
		MatrixFrame formationsCenterFrameAndExtents = GetFormationsCenterFrameAndExtents(team, out halfExtents);
		float num = float.MinValue;
		for (int i = 0; i < 11; i++)
		{
			IFormationDeploymentPlan formationPlan = teamPlan.GetFormationPlan((FormationClass)i);
			if (formationPlan.HasFrame())
			{
				float b = formationPlan.GetFrame().origin.AsVec2.DistanceSquared(formationsCenterFrameAndExtents.origin.AsVec2);
				num = MathF.Max(num, b);
			}
		}
		return (MathF.Sqrt(num) + 20f) / MathF.Max(MathF.Tan(fovAngle / 2f), 0.01f);
	}

	public override IFormationDeploymentPlan GetFormationPlan(Team team, FormationClass fClass, bool isReinforcement = false)
	{
		return GetTeamPlan(team).GetFormationPlan(fClass);
	}

	public override bool IsPlanMade(Team team)
	{
		return GetTeamPlanAux(team)?.IsPlanMade() ?? false;
	}

	public override bool IsPlanMade(Team team, out bool isFirstPlan)
	{
		isFirstPlan = false;
		NavalTeamDeploymentPlan teamPlanAux = GetTeamPlanAux(team);
		if (teamPlanAux != null && teamPlanAux.IsPlanMade())
		{
			isFirstPlan = teamPlanAux.IsFirstPlan();
			return true;
		}
		return false;
	}

	public override bool HasDeploymentBoundaries(Team team)
	{
		return GetTeamPlanAux(team)?.HasDeploymentBoundaries() ?? false;
	}

	public override MatrixFrame GetDeploymentZoneFrame(Team team)
	{
		return GetTeamPlan(team).GetDeploymentZoneFrame();
	}

	public override MatrixFrame GetFormationsCenterFrameAndExtents(Team team, out Vec2 halfExtents, bool ignoreDimensionlessFormations = true)
	{
		return GetTeamPlan(team).GetFormationsCenterFrameAndExtents(out halfExtents, ignoreDimensionlessFormations);
	}

	public float GetTargetOffset(Team team)
	{
		return GetTeamPlan(team).GetTargetOffset();
	}

	public override MBReadOnlyList<(string, MBList<Vec2>)> GetDeploymentBoundaries(Team team)
	{
		return GetTeamPlan(team).GetDeploymentBoundaries();
	}

	public virtual bool GetMeanBoundaryPosition(Team team, out Vec2 meanPosition, int boundaryIndex = 0)
	{
		NavalTeamDeploymentPlan teamPlan = GetTeamPlan(team);
		if (teamPlan.HasDeploymentBoundaries())
		{
			meanPosition = teamPlan.GetMeanBoundaryPosition(boundaryIndex);
			return true;
		}
		meanPosition = Vec2.Invalid;
		return false;
	}

	private NavalTeamDeploymentPlan GetTeamPlan(Team team)
	{
		return GetTeamPlanAux(team);
	}

	private NavalTeamDeploymentPlan GetTeamPlanAux(Team team)
	{
		return _teamDeploymentPlans.FirstOrDefault(((Team team, NavalTeamDeploymentPlan plan) tdp) => tdp.team == team).plan;
	}
}
