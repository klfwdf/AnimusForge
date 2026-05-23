using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IMissionDeploymentPlan
{
	void Initialize();

	void ClearAll();

	void MakeDefaultDeploymentPlans();

	void MakeDeploymentPlan(Team team, float spawnPathOffset = 0f, float targetOffset = 0f);

	bool RemakeDeploymentPlan(Team team);

	void ClearDeploymentPlan(Team team);

	bool IsPlanMade(Team team);

	bool IsPlanMade(Team team, out bool isFirstPlan);

	bool IsPositionInsideDeploymentBoundaries(Team team, in Vec2 position);

	bool HasDeploymentBoundaries(Team team);

	MBReadOnlyList<(string id, MBList<Vec2> points)> GetDeploymentBoundaries(Team team);

	bool SupportsReinforcements();

	void UpdateReinforcementPlan(Team team);

	bool SupportsNavmesh(Team team);

	bool HasPlayerSpawnFrame(BattleSideEnum battleSide);

	bool GetPlayerSpawnFrame(BattleSideEnum battleSide, out WorldPosition position, out Vec2 direction);

	Vec2 GetClosestDeploymentBoundaryPosition(Team team, in Vec2 position);

	void ProjectPositionToDeploymentBoundaries(Team team, ref WorldPosition position);

	bool GetPathDeploymentBoundaryIntersection(Team team, in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition intersection);

	MatrixFrame GetDeploymentFrame(Team team);

	IFormationDeploymentPlan GetFormationPlan(Team team, FormationClass fClass, bool isReinforcement = false);

	float GetSpawnPathOffset(Team team);

	MatrixFrame GetZoomFocusFrame(Team team);

	float GetZoomOffset(Team team, float fovAngle);
}
