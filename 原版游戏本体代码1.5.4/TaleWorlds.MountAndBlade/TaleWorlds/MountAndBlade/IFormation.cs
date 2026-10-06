using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IFormation
{
	float Interval { get; }

	float Distance { get; }

	float UnitDiameter { get; }

	float MinimumInterval { get; }

	float MaximumInterval { get; }

	float MinimumDistance { get; }

	float MaximumDistance { get; }

	int? OverridenUnitCount { get; }

	bool GetIsLocalPositionAvailable(Vec2 localPosition, Vec2? nearestAvailableUnitPositionLocal);

	bool BatchUnitPositions(MBArrayList<Vec2i> orderedPositionIndices, MBArrayList<Vec2> orderedLocalPositions, MBList2D<int> availabilityTable, MBList2D<WorldPosition> globalPositionTable, int fileCount, int rankCount);

	IFormationUnit GetClosestUnitTo(Vec2 localPosition, MBList<IFormationUnit> unitsWithSpaces = null, float? maxDistance = null);

	IFormationUnit GetClosestUnitTo(IFormationUnit targetUnit, MBList<IFormationUnit> unitsWithSpaces = null, float? maxDistance = null);

	void OnUnitAddedOrRemoved();

	void SetUnitToFollow(IFormationUnit unit, IFormationUnit toFollow, Vec2 vector);
}
