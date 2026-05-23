using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface ITeamDeploymentPlan
{
	Team Team { get; }

	void MakeDeploymentPlan(float spawnPathOffset = 0f, float targetOffset = 0f, FormationSceneSpawnEntry[,] formationSceneSpawnEntries = null, bool isReinforcement = false);

	void ClearPlan(bool isReinforcement = false);

	bool IsFirstPlan(bool isReinforcement = false);

	bool IsPlanMade(bool isReinforcement = false);

	MBReadOnlyList<(string id, MBList<Vec2> points)> GetDeploymentBoundaries();

	float GetSpawnPathOffset(bool isReinforcement = false);

	float GetTargetOffset(bool isReinforcement = false);

	MatrixFrame GetDeploymentFrame();

	bool HasDeploymentBoundaries();

	IFormationDeploymentPlan GetFormationPlan(FormationClass formationIndex, bool isReinforcement = false);

	Vec3 GetMeanPosition(bool isReinforcement = false);

	bool IsPositionInsideDeploymentBoundaries(in Vec2 position, out (string id, MBList<Vec2> points) containingBoundaryTuple);

	Vec2 GetClosestDeploymentBoundaryPosition(in Vec2 position);
}
