using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MBSceneUtilities
{
	public const int MaxNumberOfSpawnPaths = 32;

	public const string SpawnPathPrefix = "spawn_path_";

	public const string SoftBorderVertexTag = "walk_area_vertex";

	public const string HardBorderVertexTag = "walk_area_vertex_hard";

	public const string SoftBoundaryName = "walk_area";

	public const string SceneBoundaryName = "scene_boundary";

	public const float SceneToHardBoundaryMargin = 100f;

	public const string DefenderDeploymentReferencePositionTag = "defender_infantry";

	public const string AttackerDeploymentReferencePositionTag = "attacker_infantry";

	private const string DeploymentBoundaryTag = "deployment_castle_boundary";

	private const string DeploymentBoundaryTagExpression = "deployment_castle_boundary(_\\d+)*";

	public static MBList<Path> GetAllSpawnPaths(Scene scene)
	{
		MBList<Path> mBList = new MBList<Path>();
		for (int i = 0; i < 32; i++)
		{
			string name = "spawn_path_" + i.ToString("D2");
			Path pathWithName = scene.GetPathWithName(name);
			if (pathWithName != null && pathWithName.NumberOfPoints > 1)
			{
				mBList.Add(pathWithName);
			}
		}
		return mBList;
	}

	public static MBList<Vec2> GetSoftBoundaryPoints(Scene scene)
	{
		MBList<Vec2> mBList = new MBList<Vec2>();
		int softBoundaryVertexCount = scene.GetSoftBoundaryVertexCount();
		if (softBoundaryVertexCount > 2)
		{
			for (int i = 0; i < softBoundaryVertexCount; i++)
			{
				Vec2 softBoundaryVertex = scene.GetSoftBoundaryVertex(i);
				mBList.Add(softBoundaryVertex);
			}
		}
		return mBList;
	}

	public static MBList<Vec2> GetHardBoundaryPoints(Scene scene)
	{
		MBList<Vec2> mBList = new MBList<Vec2>();
		int hardBoundaryVertexCount = scene.GetHardBoundaryVertexCount();
		for (int i = 0; i < hardBoundaryVertexCount; i++)
		{
			Vec2 hardBoundaryVertex = scene.GetHardBoundaryVertex(i);
			mBList.Add(hardBoundaryVertex);
		}
		return mBList;
	}

	public static MBList<Vec2> GetSceneLimitPoints(Scene scene, out Vec2 sceneLimitMin, out Vec2 sceneLimitMax)
	{
		MBList<Vec2> mBList = new MBList<Vec2>();
		scene.GetSceneLimits(out var min, out var max);
		mBList.Add(new Vec2(min.x, min.y));
		mBList.Add(new Vec2(max.x, min.y));
		mBList.Add(new Vec2(max.x, max.y));
		mBList.Add(new Vec2(min.x, max.y));
		sceneLimitMin = min.AsVec2;
		sceneLimitMax = max.AsVec2;
		return mBList;
	}

	public static MBList<(string tag, MBList<Vec2> boundaryPoints, bool insideAllowance)> GetDeploymentBoundaries(BattleSideEnum battleSide)
	{
		IEnumerable<GameEntity> enumerable = Mission.Current.Scene.FindEntitiesWithTagExpression("deployment_castle_boundary(_\\d+)*");
		List<(string, List<GameEntity>)> list = new List<(string, List<GameEntity>)>();
		foreach (GameEntity item4 in enumerable)
		{
			if (!item4.HasTag(battleSide.ToString()))
			{
				continue;
			}
			string[] tags = item4.Tags;
			foreach (string tag in tags)
			{
				if (tag.Contains("deployment_castle_boundary"))
				{
					(string, List<GameEntity>) item = list.FirstOrDefault<(string, List<GameEntity>)>(((string tag, List<GameEntity> boundaryEntities) tuple) => tuple.tag.Equals(tag));
					if (item.Item1 == null)
					{
						item = (tag, new List<GameEntity>());
						list.Add(item);
					}
					item.Item2.Add(item4);
					break;
				}
			}
		}
		MBList<(string, MBList<Vec2>, bool)> mBList = new MBList<(string, MBList<Vec2>, bool)>();
		foreach (var item5 in list)
		{
			string item2 = item5.Item1;
			bool item3 = !item5.Item2.Any((GameEntity e) => e.HasTag("out"));
			MBList<Vec2> boundary = item5.Item2.Select((GameEntity bp) => bp.GlobalPosition.AsVec2).ToMBList();
			RadialSortBoundary(ref boundary);
			mBList.Add((item2, boundary, item3));
		}
		return mBList;
	}

	public static void GetAxisAlignedBoundaryRectangle(List<Vec2> boundaryPoints, out Vec2 boundsMin, out Vec2 boundsMax)
	{
		boundsMin = new Vec2(float.MaxValue, float.MaxValue);
		boundsMax = new Vec2(float.MinValue, float.MinValue);
		for (int i = 0; i < boundaryPoints.Count; i++)
		{
			Vec2 vec = boundaryPoints[i];
			if (vec.x < boundsMin.X)
			{
				boundsMin.x = vec.x;
			}
			if (vec.y < boundsMin.Y)
			{
				boundsMin.y = vec.y;
			}
			if (vec.x > boundsMax.X)
			{
				boundsMax.x = vec.x;
			}
			if (vec.y > boundsMax.Y)
			{
				boundsMax.y = vec.y;
			}
		}
	}

	public static void FindConvexHull(ref MBList<Vec2> boundary)
	{
		Vec2[] array = boundary.ToArray();
		int convexPointCount = 0;
		MBAPI.IMBMission.FindConvexHull(array, boundary.Count, ref convexPointCount);
		boundary = array.ToMBList();
		boundary.RemoveRange(convexPointCount, boundary.Count - convexPointCount);
	}

	public static void RadialSortBoundary(ref MBList<Vec2> boundary)
	{
		if (boundary.Count == 0)
		{
			return;
		}
		Vec2 boundaryCenter = Vec2.Zero;
		foreach (Vec2 item in boundary)
		{
			boundaryCenter += item;
		}
		boundaryCenter.x /= boundary.Count;
		boundaryCenter.y /= boundary.Count;
		boundary = boundary.OrderBy((Vec2 b) => (b - boundaryCenter).RotationInRadians).ToMBList();
	}

	public static void RadialSortBoundary(ref MBList<Vec3> boundary)
	{
		if (boundary.Count == 0)
		{
			return;
		}
		Vec2 boundaryCenter = Vec2.Zero;
		foreach (Vec3 item in boundary)
		{
			boundaryCenter += item.AsVec2;
		}
		boundaryCenter.x /= boundary.Count;
		boundaryCenter.y /= boundary.Count;
		boundary = boundary.OrderBy((Vec3 b) => (b.AsVec2 - boundaryCenter).RotationInRadians).ToMBList();
	}

	public static bool IsConvexAndRadiallySorted(MBList<Vec2> boundary)
	{
		int count = boundary.Count;
		if (count < 3)
		{
			return false;
		}
		Vec2 vec = new Vec2(0f, 0f);
		foreach (Vec2 item in boundary)
		{
			vec += item;
		}
		vec /= (float)count;
		Vec2 vec2 = (boundary[0] - vec).Normalized();
		vec2.RotateCCW(-0.001f);
		Vec2 vec3 = vec2;
		for (int i = 0; i < count; i++)
		{
			Vec2 vec4 = boundary[i];
			vec2 = (vec4 - vec).Normalized();
			if (vec3.AngleBetween(vec2) <= 0f)
			{
				return false;
			}
			vec3 = vec2;
			Vec2 vec5 = boundary[(i + 1) % count];
			if (Vec2.Determinant(vec2: boundary[(i + 2) % count] - vec4, vec1: vec5 - vec4) < 0f)
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsPointInsideBoundaries(in Vec2 point, MBList<Vec2> boundaries, float acceptanceThreshold = 0.05f)
	{
		if (boundaries.Count <= 2)
		{
			return false;
		}
		acceptanceThreshold = MathF.Max(0f, acceptanceThreshold);
		bool result = true;
		for (int i = 0; i < boundaries.Count; i++)
		{
			Vec2 vec = boundaries[i];
			Vec2 vec2 = boundaries[(i + 1) % boundaries.Count] - vec;
			Vec2 vec3 = point - vec;
			if (vec2.x * vec3.y - vec2.y * vec3.x < 0f)
			{
				vec2.Normalize();
				Vec2 vec4 = vec3.DotProduct(vec2) * vec2;
				if ((vec3 - vec4).LengthSquared > acceptanceThreshold * acceptanceThreshold)
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	public static float FindClosestPointToBoundaries(in Vec2 position, MBList<Vec2> boundaries, out Vec2 closestPoint)
	{
		closestPoint = position;
		float num = float.MaxValue;
		for (int i = 0; i < boundaries.Count; i++)
		{
			Vec2 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(boundaries[i], boundaries[(i + 1) % boundaries.Count], in position);
			float num2 = position.DistanceSquared(closestPointOnLineSegmentToPoint);
			if (num2 <= num)
			{
				num = num2;
				closestPoint = closestPointOnLineSegmentToPoint;
			}
		}
		return MathF.Sqrt(num);
	}

	public static float FindClosestPointToBoundariesReturnDistanceSquared(in Vec2 position, MBList<Vec2> boundaries, out Vec2 closestPoint, out bool isPositionInsideBoundaries)
	{
		closestPoint = position;
		float num = float.MaxValue;
		for (int i = 0; i < boundaries.Count; i++)
		{
			Vec2 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(boundaries[i], boundaries[(i + 1) % boundaries.Count], in position);
			float num2 = position.DistanceSquared(closestPointOnLineSegmentToPoint);
			if (num2 <= num)
			{
				num = num2;
				closestPoint = closestPointOnLineSegmentToPoint;
			}
		}
		isPositionInsideBoundaries = IsPointInsideBoundaries(in position, boundaries);
		return MathF.Sqrt(num);
	}
}
