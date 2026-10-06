using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IFormationDeploymentPlan
{
	FormationClass Class { get; }

	FormationClass SpawnClass { get; }

	float PlannedWidth { get; }

	float PlannedDepth { get; }

	int PlannedTroopCount { get; }

	bool HasDimensions { get; }

	bool HasFrame();

	MatrixFrame GetFrame();

	Vec3 GetPosition();

	Vec2 GetDirection();

	WorldPosition CreateNewDeploymentWorldPosition(WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache);
}
