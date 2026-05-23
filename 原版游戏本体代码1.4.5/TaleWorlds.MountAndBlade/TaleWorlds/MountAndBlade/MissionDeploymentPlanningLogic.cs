using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionDeploymentPlanningLogic : MissionLogic, IMissionDeploymentPlan
{
	public virtual void Initialize()
	{
		throw new NotImplementedException();
	}

	public virtual void ClearAll()
	{
		throw new NotImplementedException();
	}

	public virtual void MakeDefaultDeploymentPlans()
	{
		throw new NotImplementedException();
	}

	public virtual void MakeDeploymentPlan(Team team, float spawnPathOffset = 0f, float targetPathOffset = 0f)
	{
		throw new NotImplementedException();
	}

	public virtual bool RemakeDeploymentPlan(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual void ClearDeploymentPlan(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual bool IsPlanMade(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual bool IsPlanMade(Team team, out bool isFirstPlan)
	{
		throw new NotImplementedException();
	}

	public virtual bool IsPositionInsideDeploymentBoundaries(Team team, in Vec2 position)
	{
		throw new NotImplementedException();
	}

	public virtual bool HasDeploymentBoundaries(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual MBReadOnlyList<(string id, MBList<Vec2> points)> GetDeploymentBoundaries(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual bool SupportsReinforcements()
	{
		throw new NotImplementedException();
	}

	public virtual void UpdateReinforcementPlan(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual bool SupportsNavmesh(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual bool HasPlayerSpawnFrame(BattleSideEnum battleSide)
	{
		throw new NotImplementedException();
	}

	public virtual bool GetPlayerSpawnFrame(BattleSideEnum battleSide, out WorldPosition position, out Vec2 direction)
	{
		throw new NotImplementedException();
	}

	public virtual Vec2 GetClosestDeploymentBoundaryPosition(Team team, in Vec2 position)
	{
		throw new NotImplementedException();
	}

	public virtual void ProjectPositionToDeploymentBoundaries(Team team, ref WorldPosition position)
	{
		throw new NotImplementedException();
	}

	public virtual bool GetPathDeploymentBoundaryIntersection(Team team, in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition foundPosition)
	{
		throw new NotImplementedException();
	}

	public virtual MatrixFrame GetDeploymentFrame(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual IFormationDeploymentPlan GetFormationPlan(Team team, FormationClass fClass, bool isReinforcement = false)
	{
		throw new NotImplementedException();
	}

	public virtual float GetSpawnPathOffset(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual MatrixFrame GetZoomFocusFrame(Team team)
	{
		throw new NotImplementedException();
	}

	public virtual float GetZoomOffset(Team team, float fovAngle)
	{
		throw new NotImplementedException();
	}
}
