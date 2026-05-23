using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BattleSpawnPathSelector
{
	private readonly Mission _mission;

	private Path _initialPath;

	private BattleSideSpawnPathSelector[] _battleSideSelectors;

	public bool IsInitialized { get; private set; }

	public Path InitialPath => _initialPath;

	public BattleSpawnPathSelector(Mission mission)
	{
		IsInitialized = false;
		_initialPath = null;
		_mission = mission;
	}

	public void Initialize()
	{
		float pathPivotOffset;
		float pathLength;
		bool isPathInverted;
		Path path = FindBestInitialPath(_mission, out pathPivotOffset, out pathLength, out isPathInverted);
		if (path != null)
		{
			_initialPath = path;
			_battleSideSelectors = new BattleSideSpawnPathSelector[2];
			_battleSideSelectors[0] = new BattleSideSpawnPathSelector(_mission, path, pathPivotOffset, isPathInverted);
			float initialPivotOffset = MathF.Clamp(pathLength - pathPivotOffset, 1f, pathLength - 1f);
			_battleSideSelectors[1] = new BattleSideSpawnPathSelector(_mission, path, initialPivotOffset, !isPathInverted);
			IsInitialized = true;
		}
		else
		{
			_initialPath = null;
			IsInitialized = false;
		}
	}

	public bool HasPath(Path path)
	{
		if (!IsInitialized)
		{
			Debug.FailedAssert("BattleSpawnPathSelector must be initialized.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Deployment\\BattleSpawnPathSelector.cs", "HasPath", 68);
			return false;
		}
		BattleSideSpawnPathSelector battleSideSpawnPathSelector = _battleSideSelectors[1];
		BattleSideSpawnPathSelector battleSideSpawnPathSelector2 = _battleSideSelectors[0];
		if (path != null)
		{
			if (!(_initialPath.Pointer == path.Pointer) && !battleSideSpawnPathSelector.HasReinforcementPath(path))
			{
				return battleSideSpawnPathSelector2.HasReinforcementPath(path);
			}
			return true;
		}
		return false;
	}

	public bool GetInitialPathDataOfSide(BattleSideEnum side, out SpawnPathData pathPathData)
	{
		if (!IsInitialized)
		{
			Debug.FailedAssert("BattleSpawnPathSelector must be initialized.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Deployment\\BattleSpawnPathSelector.cs", "GetInitialPathDataOfSide", 82);
			pathPathData = null;
			return false;
		}
		pathPathData = _battleSideSelectors[(int)side].InitialSpawnPath;
		return pathPathData.IsValid;
	}

	public MBReadOnlyList<SpawnPathData> GetReinforcementPathsDataOfSide(BattleSideEnum side)
	{
		if (!IsInitialized)
		{
			Debug.FailedAssert("BattleSpawnPathSelector must be initialized.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Deployment\\BattleSpawnPathSelector.cs", "GetReinforcementPathsDataOfSide", 96);
			return null;
		}
		return _battleSideSelectors[(int)side].ReinforcementPaths;
	}

	public static Path FindBestInitialPath(Mission mission, out float pathPivotOffset, out float pathLength, out bool isPathInverted)
	{
		pathPivotOffset = 0f;
		isPathInverted = false;
		pathLength = 0f;
		MBList<Path> allSpawnPaths = MBSceneUtilities.GetAllSpawnPaths(mission.Scene);
		if (allSpawnPaths.IsEmpty())
		{
			return null;
		}
		int num = 2;
		foreach (Path item in allSpawnPaths)
		{
			num = MathF.Max(item.NumberOfPoints, num);
		}
		Path path = null;
		if (mission.HasSceneMapPatch())
		{
			Path path2 = null;
			bool flag = false;
			float num2 = float.MinValue;
			MatrixFrame[] array = new MatrixFrame[num];
			mission.GetPatchSceneEncounterPosition(out var position);
			Vec2 asVec = position.AsVec2;
			mission.GetPatchSceneEncounterDirection(out var direction);
			foreach (Path item2 in allSpawnPaths)
			{
				if (item2.NumberOfPoints > 1)
				{
					item2.GetPoints(array);
					float num3 = 0f;
					for (int i = 1; i < item2.NumberOfPoints; i++)
					{
						Vec2 asVec2 = array[i - 1].origin.AsVec2;
						Vec2 v = (array[i].origin.AsVec2 - asVec2).Normalized();
						float num4 = direction.DotProduct(v);
						float num5 = 1000f / (1f + asVec2.Distance(asVec));
						num3 += num5 * num4;
					}
					num3 /= (float)(item2.NumberOfPoints - 1);
					bool flag2 = false;
					if (num3 < 0f)
					{
						num3 = 0f - num3;
						flag2 = true;
					}
					if (num3 >= num2)
					{
						path2 = item2;
						num2 = num3;
						flag = flag2;
					}
				}
			}
			if (path2 != null)
			{
				path2.GetPoints(array);
				float num6 = array[0].origin.AsVec2.DistanceSquared(asVec);
				float value = 0f;
				float num7 = 0f;
				for (int j = 1; j < path2.NumberOfPoints; j++)
				{
					float num8 = array[j].origin.AsVec2.DistanceSquared(asVec);
					num7 += path2.GetArcLength(j - 1);
					if (num8 < num6)
					{
						num6 = num8;
						value = num7;
					}
				}
				path = path2;
				pathLength = path.GetTotalLength();
				pathPivotOffset = MathF.Clamp(value, 1f, pathLength - 1f);
				isPathInverted = flag;
			}
		}
		else
		{
			Path randomElement = allSpawnPaths.GetRandomElement();
			if (randomElement.NumberOfPoints > 1)
			{
				path = randomElement;
				float num9 = 0.37f + MBRandom.RandomFloat * 0.26f;
				pathLength = path.GetTotalLength();
				pathPivotOffset = MathF.Clamp(pathLength * num9, 1f, pathLength - 1f);
				isPathInverted = false;
			}
		}
		return path;
	}
}
