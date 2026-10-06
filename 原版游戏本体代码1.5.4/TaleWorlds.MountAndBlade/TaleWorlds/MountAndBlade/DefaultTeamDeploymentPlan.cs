using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DefaultTeamDeploymentPlan : ITeamDeploymentPlan
{
	public const float DeployZoneMinimumWidth = 50f;

	public const float DeployZoneMaximumWidth = 300f;

	public const float DeployZoneForwardMargin = 10f;

	public const float DeployZoneBackwardsMargin = 20f;

	public const float DeployZoneExtraWidthPerSqrtTroopCount = 4f;

	public const string DefenderDeploymentFrameEntityTag = "defender_infantry";

	public const string AttackerDeploymentFrameEntityTag = "attacker_infantry";

	private Mission _mission;

	private readonly DefaultDeploymentPlan _initialPlan;

	private readonly List<DefaultDeploymentPlan> _reinforcementPlans;

	private DefaultDeploymentPlan _currentReinforcementPlan;

	private MatrixFrame _deploymentZoneFrame;

	private readonly MBList<(string id, MBList<Vec2> points)> _deploymentBoundaries;

	private static ThreadLocal<NavigationPath> _navigationPath = new ThreadLocal<NavigationPath>(() => new NavigationPath());

	public Team Team { get; private set; }

	public bool SpawnWithHorses { get; private set; }

	public DefaultTeamDeploymentPlan(Mission mission, Team team)
	{
		_mission = mission;
		Team = team;
		_deploymentZoneFrame = MatrixFrame.Identity;
		_deploymentBoundaries = new MBList<(string, MBList<Vec2>)>();
		SpawnWithHorses = false;
		_initialPlan = DefaultDeploymentPlan.CreateInitialPlan(_mission, Team);
		_reinforcementPlans = new List<DefaultDeploymentPlan>();
		_currentReinforcementPlan = _initialPlan;
		if (_mission.HasSpawnPath)
		{
			foreach (var item2 in _mission.GetReinforcementPathsDataOfSide(Team.Side))
			{
				DefaultDeploymentPlan item = DefaultDeploymentPlan.CreateReinforcementPlanWithSpawnPath(_mission, Team, item2.pathData, item2.startOffset);
				_reinforcementPlans.Add(item);
			}
			_currentReinforcementPlan = _reinforcementPlans[0];
		}
		else
		{
			DefaultDeploymentPlan defaultDeploymentPlan = DefaultDeploymentPlan.CreateReinforcementPlan(_mission, Team);
			_reinforcementPlans.Add(defaultDeploymentPlan);
			_currentReinforcementPlan = defaultDeploymentPlan;
		}
	}

	public void SetSpawnWithHorses(bool value)
	{
		SpawnWithHorses = value;
		_initialPlan.SetSpawnWithHorses(value);
		foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
		{
			reinforcementPlan.SetSpawnWithHorses(value);
		}
	}

	public void MakeDeploymentPlan(float spawnPathOffset = 0f, float targetOffset = 0f, FormationSceneSpawnEntry[,] formationSceneSpawnEntries = null, bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				reinforcementPlan.MakeDeploymentPlan(formationSceneSpawnEntries);
			}
			return;
		}
		_initialPlan.SetSpawnPathOffset(spawnPathOffset, targetOffset);
		_initialPlan.MakeDeploymentPlan(formationSceneSpawnEntries);
		PlanDeploymentZone();
	}

	public void UpdateReinforcementPlans()
	{
		if (_reinforcementPlans.Count <= 1)
		{
			return;
		}
		foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
		{
			reinforcementPlan.UpdateSafetyScore();
		}
		if (!_currentReinforcementPlan.IsSafeToDeploy)
		{
			_currentReinforcementPlan = _reinforcementPlans.MaxBy((DefaultDeploymentPlan plan) => plan.SafetyScore);
		}
	}

	public void ClearPlan(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				reinforcementPlan.ClearPlan();
			}
			return;
		}
		_initialPlan.ClearPlan();
	}

	public void ClearAddedTroops(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				reinforcementPlan.ClearAddedTroops();
			}
			return;
		}
		_initialPlan.ClearAddedTroops();
	}

	public void AddTroops(FormationClass formationClass, int footTroopCount, int mountedTroopCount, bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				reinforcementPlan.AddTroops(formationClass, footTroopCount, mountedTroopCount);
			}
			return;
		}
		_initialPlan.AddTroops(formationClass, footTroopCount, mountedTroopCount);
	}

	public int GetTroopCount(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.TroopCount;
		}
		return _initialPlan.TroopCount;
	}

	public bool IsFirstPlan(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.PlanCount == 1;
		}
		return _initialPlan.PlanCount == 1;
	}

	public bool IsPlanMade(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.IsPlanMade;
		}
		return _initialPlan.IsPlanMade;
	}

	public MBReadOnlyList<(string id, MBList<Vec2> points)> GetDeploymentBoundaries()
	{
		return _deploymentBoundaries;
	}

	public float GetSpawnPathOffset(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.SpawnPathReinforcementOffset;
		}
		return _initialPlan.SpawnPathOffset;
	}

	public float GetTargetOffset(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.TargetOffset;
		}
		return _initialPlan.TargetOffset;
	}

	public MatrixFrame GetDeploymentZoneFrame()
	{
		return _deploymentZoneFrame;
	}

	public MatrixFrame GetFormationsCenterFrameAndExtents(out Vec2 halfExtents, bool ignoreDimensionlessFormations = true)
	{
		return _initialPlan.ComputeFormationsCenterFrameAndExtents(ignoreDimensionlessFormations, out halfExtents);
	}

	public bool HasDeploymentBoundaries()
	{
		return !_deploymentBoundaries.IsEmpty();
	}

	public IFormationDeploymentPlan GetFormationPlan(FormationClass fClass, bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.GetFormationPlan(fClass);
		}
		return _initialPlan.GetFormationPlan(fClass);
	}

	public Vec3 GetMeanPosition(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.MeanPosition;
		}
		return _initialPlan.MeanPosition;
	}

	public bool IsInitialPlanSuitableForFormations((int, int)[] troopDataPerFormationClass)
	{
		return _initialPlan.IsPlanSuitableForFormations(troopDataPerFormationClass);
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

	public bool GetPathDeploymentBoundaryIntersection(in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition intersection)
	{
		IsPositionInsideDeploymentBoundaries(startPosition.AsVec2, out (string, MBList<Vec2>) containingBoundaryTuple);
		intersection = WorldPosition.Invalid;
		NavigationPath value = _navigationPath.Value;
		if (Mission.Current.Scene.GetPathBetweenAIFaces(startPosition.GetNearestNavMesh(), endPosition.GetNearestNavMesh(), startPosition.AsVec2, endPosition.AsVec2, 0f, value, null) && value.Size > 0)
		{
			Vec2 vec = startPosition.AsVec2;
			(string, MBList<Vec2>) tuple = containingBoundaryTuple;
			Vec2 vec2 = Vec2.Invalid;
			for (int i = 0; i < value.Size; i++)
			{
				Vec2 position = value[i];
				if (IsPositionInsideDeploymentBoundaries(in position, out (string, MBList<Vec2>) containingBoundaryTuple2))
				{
					vec = position;
					tuple = containingBoundaryTuple2;
					continue;
				}
				vec2 = position;
				break;
			}
			if (vec2.IsValid)
			{
				intersection = startPosition;
				intersection.SetVec2(vec);
				Vec2 rayDir = (vec2 - vec).Normalized();
				MBMath.IntersectRayWithPolygon(vec, rayDir, tuple.Item2, out var intersectionPoint);
				intersection.SetVec2(Mission.Current.Scene.GetLastPointOnNavigationMeshFromWorldPositionToDestination(ref intersection, intersectionPoint).AsVec2);
			}
			else
			{
				intersection = endPosition;
			}
		}
		else
		{
			intersection = startPosition;
		}
		_navigationPath.Value.Size = 0;
		return intersection.IsValid;
	}

	public static MBList<Vec2> ComputeDeploymentBoundariesFromMissionBoundaries(ICollection<Vec2> missionBoundaries, in MatrixFrame deploymentFrame, float desiredWidth, float desiredDepth)
	{
		MBList<Vec2> boundary = new MBList<Vec2>();
		if (missionBoundaries.Count > 2)
		{
			Vec2 asVec = deploymentFrame.origin.AsVec2;
			Vec2 vec = deploymentFrame.rotation.s.AsVec2.Normalized();
			Vec2 closestPointOnLineSegment = deploymentFrame.rotation.f.AsVec2;
			Vec2 vec2 = closestPointOnLineSegment.Normalized();
			MBList<Vec2> boundaries = missionBoundaries.ToMBList();
			float maxLength = desiredWidth / 2f;
			List<(Vec2, Vec2)> list = new List<(Vec2, Vec2)>();
			ClampRayToMissionBoundaries(boundaries, asVec, vec, maxLength, out var clampedIntersection);
			AddDeploymentBoundaryPoint(boundary, clampedIntersection);
			ClampRayToMissionBoundaries(boundaries, asVec, -vec, maxLength, out var clampedIntersection2);
			AddDeploymentBoundaryPoint(boundary, clampedIntersection2);
			Vec2 clampedIntersection3;
			bool flag = ClampRayToMissionBoundaries(boundaries, clampedIntersection, -vec2, desiredDepth, out clampedIntersection3);
			float num = 0f;
			if (flag)
			{
				AddDeploymentBoundaryPoint(boundary, clampedIntersection3);
				num = clampedIntersection.Distance(clampedIntersection3);
			}
			Vec2 clampedIntersection4;
			bool flag2 = ClampRayToMissionBoundaries(boundaries, clampedIntersection2, -vec2, desiredDepth, out clampedIntersection4);
			float num2 = 0f;
			if (flag2)
			{
				AddDeploymentBoundaryPoint(boundary, clampedIntersection4);
				num2 = clampedIntersection2.Distance(clampedIntersection4);
			}
			if (flag2 && num < desiredDepth && ClampRayToMissionBoundaries(boundaries, clampedIntersection4, vec, desiredWidth, out var clampedIntersection5) && clampedIntersection5.DistanceToLineSegment(clampedIntersection2, clampedIntersection, out closestPointOnLineSegment) > num)
			{
				AddDeploymentBoundaryPoint(boundary, clampedIntersection5);
			}
			if (flag && num2 < desiredDepth && ClampRayToMissionBoundaries(boundaries, clampedIntersection3, -vec, desiredWidth, out var clampedIntersection6) && clampedIntersection6.DistanceToLineSegment(clampedIntersection2, clampedIntersection, out closestPointOnLineSegment) > num2)
			{
				AddDeploymentBoundaryPoint(boundary, clampedIntersection6);
			}
			if (desiredDepth < float.MaxValue)
			{
				Vec2 vec3 = clampedIntersection - vec2 * desiredDepth;
				Vec2 vec4 = clampedIntersection2 - vec2 * desiredDepth;
				list.Add((vec3, clampedIntersection));
				list.Add((clampedIntersection, clampedIntersection2));
				list.Add((clampedIntersection2, vec4));
				list.Add((vec4, vec3));
			}
			else
			{
				if (flag)
				{
					list.Add((clampedIntersection3, clampedIntersection));
				}
				list.Add((clampedIntersection, clampedIntersection2));
				if (flag2)
				{
					list.Add((clampedIntersection2, clampedIntersection4));
				}
			}
			foreach (Vec2 missionBoundary in missionBoundaries)
			{
				bool flag3 = true;
				foreach (var item in list)
				{
					Vec2 vec5 = missionBoundary - item.Item1;
					Vec2 vec6 = item.Item2 - item.Item1;
					if (vec6.x * vec5.y - vec6.y * vec5.x <= 1E-06f)
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					AddDeploymentBoundaryPoint(boundary, missionBoundary);
				}
			}
			MBSceneUtilities.RadialSortBoundary(ref boundary);
			MBSceneUtilities.FindConvexHull(ref boundary);
		}
		return boundary;
	}

	private void PlanDeploymentZone()
	{
		if (_mission.HasSpawnPath || _mission.IsFieldBattle || _mission.IsNavalRaidBattle)
		{
			if (_mission.HasSpawnPath || Team.Side == BattleSideEnum.Attacker)
			{
				ComputeDeploymentZoneFromFormations(addExtraWidthFromTroopCount: true, useMaxDepth: true);
			}
			else
			{
				ComputeDeploymentZoneFromFormations(addExtraWidthFromTroopCount: false, useMaxDepth: false, 50f);
			}
		}
		else if (_mission.IsSiegeBattle)
		{
			ComputeDeploymentZoneFromSceneDeploymentBoundaries();
		}
		else
		{
			_deploymentZoneFrame = MatrixFrame.Identity;
			_deploymentBoundaries.Clear();
		}
	}

	private void ComputeDeploymentZoneFromFormations(bool addExtraWidthFromTroopCount, bool useMaxDepth, float sideMargin = 0f)
	{
		_initialPlan.GetFirstValidFormationFrame(out _deploymentZoneFrame, checkDimensions: false);
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		for (int i = 0; i < 10; i++)
		{
			FormationClass fClass = (FormationClass)i;
			DefaultFormationDeploymentPlan formationPlan = _initialPlan.GetFormationPlan(fClass);
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
		num += 10f;
		num2 = TaleWorlds.Library.MathF.Abs(num2) + 20f;
		_deploymentZoneFrame.Advance(num);
		float a = (num3 + num4) / 2f;
		_deploymentZoneFrame.Strafe(a);
		float num9 = num3 + TaleWorlds.Library.MathF.Abs(num4);
		_deploymentBoundaries.Clear();
		float num10 = num9 + sideMargin;
		if (addExtraWidthFromTroopCount)
		{
			float num11 = 4f * TaleWorlds.Library.MathF.Sqrt(_initialPlan.TroopCount);
			num10 += num11;
		}
		num10 = Math.Max(num10, 50f);
		num10 = Math.Min(num10, 300f);
		float num12 = num + num2;
		float desiredDepth = (useMaxDepth ? float.MaxValue : num12);
		foreach (KeyValuePair<string, ICollection<Vec2>> boundary in _mission.Boundaries)
		{
			string key = boundary.Key;
			MBList<Vec2> item = ComputeDeploymentBoundariesFromMissionBoundaries(boundary.Value, in _deploymentZoneFrame, num10, desiredDepth);
			_deploymentBoundaries.Add((key, item));
		}
	}

	private void ComputeDeploymentZoneFromSceneDeploymentBoundaries()
	{
		_deploymentBoundaries.Clear();
		foreach (var deploymentBoundary in MBSceneUtilities.GetDeploymentBoundaries(Team.Side))
		{
			MBList<Vec2> boundary = new MBList<Vec2>(deploymentBoundary.boundaryPoints);
			MBSceneUtilities.RadialSortBoundary(ref boundary);
			MBSceneUtilities.FindConvexHull(ref boundary);
			_deploymentBoundaries.Add((deploymentBoundary.tag, boundary));
		}
		_deploymentZoneFrame = _mission.Scene.FindWeakEntityWithTag((Team.Side == BattleSideEnum.Attacker) ? "attacker_infantry" : "defender_infantry").GetGlobalFrame();
	}

	private static void AddDeploymentBoundaryPoint(MBList<Vec2> deploymentBoundaries, Vec2 point)
	{
		if (!deploymentBoundaries.Exists((Vec2 boundaryPoint) => boundaryPoint.Distance(point) <= 0.1f))
		{
			deploymentBoundaries.Add(point);
		}
	}

	private static bool ClampRayToMissionBoundaries(MBList<Vec2> boundaries, Vec2 origin, Vec2 direction, float maxLength, out Vec2 clampedIntersection)
	{
		if (Mission.Current.IsPositionInsideBoundaries(origin) && maxLength < float.MaxValue)
		{
			Vec2 vec = origin + direction * maxLength;
			if (Mission.Current.IsPositionInsideBoundaries(vec))
			{
				clampedIntersection = vec;
				return true;
			}
		}
		if (MBMath.IntersectRayWithPolygon(origin, direction, boundaries, out clampedIntersection))
		{
			return true;
		}
		return false;
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
