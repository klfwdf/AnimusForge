using TaleWorlds.Library;

namespace NavalDLC.DWA;

public interface IDWAAgentDelegate
{
	ref readonly DWAAgentState State { get; }

	float NeighborDistance { get; }

	float MaxLinearSpeed { get; }

	float MaxLinearAcceleration { get; }

	float MaxAngularSpeed { get; }

	float MaxAngularAcceleration { get; }

	bool AvoidAgentCollisions { get; }

	bool AvoidObstacleCollisions { get; }

	void Initialize(int id);

	void SetParameters(in DWASimulatorParameters parameters);

	float GetSafetyFactor();

	bool CanPlanTrajectory();

	bool HasArrivedAtTarget();

	bool IsAgentEligibleNeighbor(int targetAgentId, IDWAAgentDelegate targetAgentDelegate);

	bool IsObstacleSegmentEligibleNeighbor(IDWAObstacleVertex obstacle1, IDWAObstacleVertex obstacle2);

	void OnStateUpdate();

	void UpdateSelectedAction(float dV, float dOmega);

	float GetGoalDirection(out Vec2 goalDir);

	(float dV, float dOmega) GetSelectedAction();

	void ComputeExternalAccelerationsOnState(float dt, in DWAAgentState state, out Vec2 extLinearAcc, out float extAngularAcc);

	float ComputeGoalCost(int sampleIndex, in DWAAgentState atState, (float distance, float amount) targetOcclusion);
}
