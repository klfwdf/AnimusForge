using TaleWorlds.Library;

namespace NavalDLC.DWA;

public interface IDWAObstacleVertex
{
	int Id { get; }

	Vec2 Point { get; }

	float PointZ { get; }
}
