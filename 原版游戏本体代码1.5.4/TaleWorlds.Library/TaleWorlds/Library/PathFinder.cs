using System.Collections.Generic;

namespace TaleWorlds.Library;

public abstract class PathFinder
{
	public static float BuildingCost = 5000f;

	public static float WaterCost = 400f;

	public static float ShallowWaterCost = 100f;

	public PathFinder()
	{
	}

	public virtual void Destroy()
	{
	}

	public abstract void Initialize(Vec3 bbSize);

	public abstract bool FindPath(Vec3 wSource, Vec3 wDestination, List<Vec3> path, float craftWidth = 5f);
}
