using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BattleSideSpawnPathSelector
{
	public const float MaxNeighborCount = 2f;

	private readonly Mission _mission;

	private readonly SpawnPathData _initialSpawnPath;

	private readonly MBList<SpawnPathData> _reinforcementSpawnPaths;

	public SpawnPathData InitialSpawnPath => _initialSpawnPath;

	public MBReadOnlyList<SpawnPathData> ReinforcementPaths => _reinforcementSpawnPaths;

	public BattleSideSpawnPathSelector(Mission mission, Path initialPath, float initialPivotOffset, bool initialPathIsInverted)
	{
		_mission = mission;
		SpawnPathData.SnapMethod snapType = (mission.IsNavalBattle ? SpawnPathData.SnapMethod.SnapToWaterLevel : (mission.IsFieldBattle ? SpawnPathData.SnapMethod.SnapToTerrain : SpawnPathData.SnapMethod.DontSnap));
		_initialSpawnPath = SpawnPathData.Create(_mission.Scene, initialPath, initialPivotOffset, initialPathIsInverted, snapType);
		_reinforcementSpawnPaths = new MBList<SpawnPathData>();
		FindReinforcementPaths();
	}

	public bool HasReinforcementPath(Path path)
	{
		if (path != null)
		{
			return _reinforcementSpawnPaths.Exists((SpawnPathData pathData) => pathData.Path.Pointer == path.Pointer);
		}
		return false;
	}

	private void FindReinforcementPaths()
	{
		_reinforcementSpawnPaths.Clear();
		float pivotOffset = _initialSpawnPath.PathLength / 2f;
		SpawnPathData spawnPathData = SpawnPathData.Create(_initialSpawnPath.Scene, _initialSpawnPath.Path, pivotOffset, _initialSpawnPath.IsInverted);
		_reinforcementSpawnPaths.Add(spawnPathData);
		MBList<Path> allSpawnPaths = MBSceneUtilities.GetAllSpawnPaths(_mission.Scene);
		if (allSpawnPaths.Count == 0)
		{
			return;
		}
		bool flag = false;
		if (allSpawnPaths.Count > 1)
		{
			MatrixFrame[] array = new MatrixFrame[100];
			spawnPathData.Path.GetPoints(array);
			MatrixFrame matrixFrame = (spawnPathData.IsInverted ? array[spawnPathData.Path.NumberOfPoints - 1] : array[0]);
			SortedList<float, SpawnPathData> sortedList = new SortedList<float, SpawnPathData>();
			foreach (Path item in allSpawnPaths)
			{
				if (item.NumberOfPoints > 1 && item.Pointer != spawnPathData.Path.Pointer)
				{
					item.GetPoints(array);
					MatrixFrame matrixFrame2 = array[0];
					MatrixFrame matrixFrame3 = array[item.NumberOfPoints - 1];
					float key = matrixFrame2.origin.DistanceSquared(matrixFrame.origin);
					float key2 = matrixFrame3.origin.DistanceSquared(matrixFrame.origin);
					float pivotOffset2 = item.GetTotalLength() / 2f;
					sortedList.Add(key, SpawnPathData.Create(_initialSpawnPath.Scene, item, pivotOffset2));
					sortedList.Add(key2, SpawnPathData.Create(_initialSpawnPath.Scene, item, pivotOffset2, isInverted: true));
				}
				else
				{
					flag = flag || spawnPathData.Path.Pointer == item.Pointer;
				}
			}
			int num = 0;
			{
				foreach (KeyValuePair<float, SpawnPathData> item2 in sortedList)
				{
					_reinforcementSpawnPaths.Add(item2.Value);
					num++;
					if ((float)num >= 2f)
					{
						break;
					}
				}
				return;
			}
		}
		flag = spawnPathData.Path.Pointer == allSpawnPaths[0].Pointer;
	}
}
