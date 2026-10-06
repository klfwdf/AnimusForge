using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Deployment;

public class NavalTeamDeploymentPlan : ITeamDeploymentPlan
{
	public const float DeployZoneMinimumWidth = 100f;

	public const float DeployZoneMaximumWidth = 400f;

	public const float RiverDeployZoneMaximumWidth = 200f;

	public const float RiverDeployZoneMarginWidth = 10f;

	public const float DeployZoneForwardMargin = 50f;

	public const float DeployZoneBackwardsMargin = 50f;

	public const float DeployZoneExtraWidthPerShip = 20f;

	private Mission _mission;

	private readonly NavalDeploymentPlan _initialPlan;

	private MatrixFrame _deploymentZoneFrame;

	private readonly MBList<(string id, MBList<Vec2> points)> _deploymentBoundaries;

	private readonly MBList<Vec2> _meanBoundaryPositions;

	public Team Team { get; private set; }

	internal NavalTeamDeploymentPlan(Mission mission, Team team)
	{
		_mission = mission;
		Team = team;
		_deploymentZoneFrame = MatrixFrame.Identity;
		_deploymentBoundaries = new MBList<(string, MBList<Vec2>)>();
		_meanBoundaryPositions = new MBList<Vec2>();
		bool isRiverPlan = mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.NavalBattle && _mission.Scene.GetNavmeshFaceCountBetweenTwoIds(1, 1) > 0;
		bool isRaidPlan = mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.NavalRaid;
		_initialPlan = NavalDeploymentPlan.CreatePlan(_mission, team, isRiverPlan, isRaidPlan);
	}

	public void MakeDeploymentPlan(float spawnPathOffset, float targetOffset = 0f, FormationSceneSpawnEntry[,] formationSpawnEntries = null, bool isReinforcement = false)
	{
		_initialPlan.MakeDeploymentPlan(spawnPathOffset, targetOffset, formationSpawnEntries);
		if (_initialPlan.IsRiverPlan || _initialPlan.IsRaidPlan)
		{
			float fixedWidth = (_initialPlan.IsRiverPlan ? 200f : 0f);
			ComputeDeploymentZoneFromFormations(addExtraWidthFromShipCount: false, useMaxDepth: false, fixedWidth);
		}
		else
		{
			ComputeDeploymentZoneFromFormations(addExtraWidthFromShipCount: true, useMaxDepth: true);
		}
	}

	public void ClearPlan(bool isReinforcement = false)
	{
		_initialPlan.ClearPlan();
		_meanBoundaryPositions.Clear();
	}

	public void ClearAddedShips()
	{
		_initialPlan.ClearAddedShips();
	}

	internal void AddShip(FormationClass formationClass, IShipOrigin shipOrigin)
	{
		_initialPlan.AddShip(formationClass, shipOrigin);
	}

	internal bool RemoveShip(FormationClass formationIndex)
	{
		return _initialPlan.RemoveShip(formationIndex);
	}

	public bool IsFirstPlan(bool isReinforcement = false)
	{
		return _initialPlan.PlanCount == 1;
	}

	public bool IsPlanMade(bool isReinforcement = false)
	{
		return _initialPlan.IsPlanMade;
	}

	public int GetShipCount()
	{
		return _initialPlan.ShipCount;
	}

	public float GetSpawnPathOffset(bool isReinforcement = false)
	{
		return _initialPlan.SpawnPathOffset;
	}

	public MBReadOnlyList<(string id, MBList<Vec2> points)> GetDeploymentBoundaries()
	{
		return _deploymentBoundaries;
	}

	public MatrixFrame GetFormationsCenterFrameAndExtents(out Vec2 halfExtents, bool ignoreDimensionlessFormations = true)
	{
		return _initialPlan.ComputeFormationsCenterFrameAndExtents(ignoreDimensionlessFormations, out halfExtents);
	}

	public float GetTargetOffset(bool isReinforcement = false)
	{
		return _initialPlan.TargetOffset;
	}

	public MatrixFrame GetDeploymentZoneFrame()
	{
		UpdateDeploymentFrameZ();
		return _deploymentZoneFrame;
	}

	public bool HasDeploymentBoundaries()
	{
		return !_deploymentBoundaries.IsEmpty();
	}

	public IFormationDeploymentPlan GetFormationPlan(FormationClass fClass, bool isReinforcement = false)
	{
		return _initialPlan.GetFormationPlan(fClass);
	}

	public Vec3 GetMeanPosition(bool isReinforcement = false)
	{
		return _initialPlan.MeanPosition;
	}

	public bool IsPositionInsideDeploymentBoundaries(in Vec2 position, out (string id, MBList<Vec2> points) containingBoundaryTuple)
	{
		bool result = false;
		containingBoundaryTuple = (id: "", points: null);
		foreach (var deploymentBoundary in _deploymentBoundaries)
		{
			MBList<Vec2> item = deploymentBoundary.points;
			if (MBSceneUtilities.IsPointInsideBoundaries(in position, item))
			{
				containingBoundaryTuple = deploymentBoundary;
				result = true;
				break;
			}
		}
		return result;
	}

	public Vec2 GetClosestDeploymentBoundaryPosition(in Vec2 position)
	{
		Vec2 result = position;
		float num = float.MaxValue;
		foreach (var deploymentBoundary in _deploymentBoundaries)
		{
			MBList<Vec2> item = deploymentBoundary.points;
			if (item.Count > 2)
			{
				Vec2 closestPoint;
				float num2 = MBSceneUtilities.FindClosestPointToBoundaries(in position, item, out closestPoint);
				if (num2 < num)
				{
					num = num2;
					result = closestPoint;
				}
			}
		}
		return result;
	}

	public Vec2 GetMeanBoundaryPosition(int boundaryIndex = 0)
	{
		return _meanBoundaryPositions[boundaryIndex];
	}

	private void ComputeDeploymentZoneFromFormations(bool addExtraWidthFromShipCount, bool useMaxDepth, float fixedWidth = 0f)
	{
		_initialPlan.GetFirstValidFormationFrame(out _deploymentZoneFrame, checkDimensions: false);
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		for (int i = 0; i < 10; i++)
		{
			FormationClass fClass = (FormationClass)i;
			IFormationDeploymentPlan formationPlan = GetFormationPlan(fClass);
			if (formationPlan.HasFrame() && formationPlan.PlannedTroopCount > 0)
			{
				MatrixFrame matrixFrame = _deploymentZoneFrame.TransformToLocal(formationPlan.GetFrame());
				float num5 = formationPlan.PlannedDepth * 0.5f;
				float num6 = formationPlan.PlannedWidth * 0.5f;
				Vec3 s = matrixFrame.rotation.s;
				Vec3 f = matrixFrame.rotation.f;
				float num7 = TaleWorlds.Library.MathF.Abs(s.x) * num6 + TaleWorlds.Library.MathF.Abs(f.x) * num5;
				float num8 = TaleWorlds.Library.MathF.Abs(s.y) * num6 + TaleWorlds.Library.MathF.Abs(f.y) * num5;
				num = Math.Max(matrixFrame.origin.y + num8, num);
				num2 = Math.Min(matrixFrame.origin.y - num8, num2);
				num3 = Math.Max(matrixFrame.origin.x + num7, num3);
				num4 = Math.Min(matrixFrame.origin.x - num7, num4);
			}
		}
		num += 50f;
		num2 = TaleWorlds.Library.MathF.Abs(num2) + 50f;
		_deploymentZoneFrame.Advance(num);
		float a = (num3 + num4) * 0.5f;
		_deploymentZoneFrame.Strafe(a);
		float num9 = num3 + TaleWorlds.Library.MathF.Abs(num4);
		_deploymentBoundaries.Clear();
		_meanBoundaryPositions.Clear();
		float desiredWidth;
		if (fixedWidth > 1E-05f)
		{
			desiredWidth = fixedWidth;
		}
		else
		{
			desiredWidth = num9;
			if (addExtraWidthFromShipCount)
			{
				float num10 = 20f * (float)_initialPlan.ShipCount;
				desiredWidth += num10;
			}
			desiredWidth = Math.Max(desiredWidth, 100f);
			desiredWidth = Math.Min(desiredWidth, 400f);
		}
		float num11 = num + num2;
		float desiredDepth = (useMaxDepth ? float.MaxValue : num11);
		foreach (KeyValuePair<string, ICollection<Vec2>> boundary in _mission.Boundaries)
		{
			string key = boundary.Key;
			MBList<Vec2> mBList = DefaultTeamDeploymentPlan.ComputeDeploymentBoundariesFromMissionBoundaries(boundary.Value, in _deploymentZoneFrame, desiredWidth, desiredDepth);
			_deploymentBoundaries.Add((key, mBList));
			Vec2 item = new Vec2(mBList.Average((Vec2 v) => v.x), mBList.Average((Vec2 v) => v.y));
			_meanBoundaryPositions.Add(item);
		}
		UpdateDeploymentFrameZ();
	}

	private void UpdateDeploymentFrameZ()
	{
		_deploymentZoneFrame.origin.z = _mission.Scene.GetWaterLevelAtPosition(_deploymentZoneFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
	}

	bool ITeamDeploymentPlan.IsPositionInsideDeploymentBoundaries(in Vec2 position, out (string id, MBList<Vec2> points) containingBoundaryTuple)
	{
		return IsPositionInsideDeploymentBoundaries(in position, out containingBoundaryTuple);
	}

	Vec2 ITeamDeploymentPlan.GetClosestDeploymentBoundaryPosition(in Vec2 position)
	{
		return GetClosestDeploymentBoundaryPosition(in position);
	}
}
