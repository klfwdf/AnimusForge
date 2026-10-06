using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FFASpawnFrameBehavior : SpawnFrameBehaviorBase
{
	public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
	{
		return GetSpawnFrameFromSpawnPoints(SpawnPoints.ToList(), null, hasMount);
	}
}
