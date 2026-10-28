using NavalDLC.DWA;
using NavalDLC.Missions.NavalPhysics;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects;

public class ShipDWAAgentDelegate : IDWAAgentDelegate
{
	private DWAAgentState _state;

	private float _detectionRadius;

	private bool _hasTarget;

	private Vec2 _targetPos;

	private Vec2 _targetHeadingDir;

	private Vec2 _shipToTargetDir;

	private Vec2 _shipToTargetNormalDir;

	private Vec2 _shipToTargetTangentDir;

	private float _dotShipFwdToTargetHeading;

	private float _targetSpeed;

	private float _shipToTargetDistance;

	private float _timeHorizon;

	private (float dV, float dOmega) _selectedAction;

	public int Id { get; private set; }

	public MissionShip OwnerShip { get; private set; }

	public float ShapeOffsetY { get; private set; }

	public float ShapeComOffsetY { get; private set; }

	public ref readonly DWAAgentState State => ref _state;

	public float NeighborDistance => _detectionRadius;

	public float MaxLinearSpeed => OwnerShip.MissionShipObject.MaxLinearSpeed;

	public float MaxLinearAcceleration => OwnerShip.MissionShipObject.MaxLinearAccel;

	public float MaxAngularSpeed => OwnerShip.MissionShipObject.MaxAngularSpeed;

	public float MaxAngularAcceleration => OwnerShip.MissionShipObject.MaxAngularAccel;

	bool IDWAAgentDelegate.AvoidAgentCollisions
	{
		get
		{
			if (OwnerShip.IsAIControlled)
			{
				return OwnerShip.AIController.AvoidShipCollisions;
			}
			return false;
		}
	}

	bool IDWAAgentDelegate.AvoidObstacleCollisions
	{
		get
		{
			if (OwnerShip.IsAIControlled)
			{
				return OwnerShip.AIController.AvoidObstacleCollisions;
			}
			return false;
		}
	}

	public ShipDWAAgentDelegate(MissionShip ownerShip, in DWASimulatorParameters parameters)
	{
		Id = -1;
		OwnerShip = ownerShip;
		BoundingBox physicsBoundingBoxWithoutChildren = ownerShip.Physics.PhysicsBoundingBoxWithoutChildren;
		Vec3 physicsBoundingBoxSizeWithoutChildren = ownerShip.Physics.PhysicsBoundingBoxSizeWithoutChildren;
		ShapeOffsetY = ((physicsBoundingBoxWithoutChildren.min + physicsBoundingBoxWithoutChildren.max) * 0.5f).y;
		Vec3 localCenterOfMass = ownerShip.Physics.LocalCenterOfMass;
		ShapeComOffsetY = ShapeOffsetY - localCenterOfMass.y;
		_state.ShapeHalfSize = new Vec2(physicsBoundingBoxSizeWithoutChildren.x / 2f, physicsBoundingBoxSizeWithoutChildren.y / 2f);
		_state.ShapeOffset = new Vec2(0f, ShapeComOffsetY);
		SetTimeHorizon(parameters.TimeHorizon);
	}

	void IDWAAgentDelegate.Initialize(int id)
	{
		Id = id;
		CacheDynamicParameters();
	}

	void IDWAAgentDelegate.SetParameters(in DWASimulatorParameters parameters)
	{
		SetTimeHorizon(parameters.TimeHorizon);
	}

	float IDWAAgentDelegate.GetSafetyFactor()
	{
		return 1f;
	}

	bool IDWAAgentDelegate.CanPlanTrajectory()
	{
		if (!OwnerShip.IsAIControlled)
		{
			return false;
		}
		return true;
	}

	bool IDWAAgentDelegate.HasArrivedAtTarget()
	{
		float postionErrorSquared;
		float rotationError;
		if (_hasTarget)
		{
			return OwnerShip.AIController.HasArrivedAtTarget(out postionErrorSquared, out rotationError);
		}
		return false;
	}

	bool IDWAAgentDelegate.IsAgentEligibleNeighbor(int targetAgentId, IDWAAgentDelegate targetAgentDelegate)
	{
		foreach (MissionShip shipCollisionIgnore in OwnerShip.AIController.ShipCollisionIgnoreList)
		{
			if (shipCollisionIgnore.DWAAgentId == targetAgentId)
			{
				return false;
			}
		}
		return true;
	}

	bool IDWAAgentDelegate.IsObstacleSegmentEligibleNeighbor(IDWAObstacleVertex obstacle1, IDWAObstacleVertex obstacle2)
	{
		return true;
	}

	void IDWAAgentDelegate.OnStateUpdate()
	{
		CacheDynamicParameters();
		_hasTarget = false;
		if (OwnerShip.IsAIControlled)
		{
			_hasTarget = OwnerShip.AIController.GetNextTarget(out var targetPosition, out var targetDirection, out var targetSpeed);
			if (_hasTarget)
			{
				CacheShipTrajectoryData(in targetPosition, in targetDirection, targetSpeed);
			}
		}
	}

	void IDWAAgentDelegate.UpdateSelectedAction(float dV, float dOmega)
	{
		if (OwnerShip.IsAIControlled)
		{
			OwnerShip.AIController.UpdateTrajectory(dV, dOmega);
		}
		_selectedAction = (dV: dV, dOmega: dOmega);
	}

	float IDWAAgentDelegate.GetGoalDirection(out Vec2 goalDir)
	{
		goalDir = _shipToTargetDir;
		return _shipToTargetDistance;
	}

	(float dV, float dOmega) IDWAAgentDelegate.GetSelectedAction()
	{
		return _selectedAction;
	}

	float IDWAAgentDelegate.ComputeGoalCost(int sampleIndex, in DWAAgentState sampleState, (float distance, float amount) targetOcclusion)
	{
		if (!_hasTarget)
		{
			return 0f;
		}
		float num = 16f;
		float num2 = 16f;
		float num3 = 1f;
		float num4 = 0.5f;
		float num5 = 0.1f;
		Vec2 shapeCenter = sampleState.ShapeCenter;
		Vec2 direction = sampleState.Direction;
		Vec2 linearVelocity = sampleState.LinearVelocity;
		Vec2 vb;
		Vec2 va = (vb = _targetPos - shapeCenter);
		float num6 = vb.Normalize();
		if (num6 <= 1E-06f)
		{
			vb = _targetHeadingDir;
		}
		float distance = MathF.Abs(Vec2.DotProduct(va, _shipToTargetNormalDir));
		float x = sampleState.ShapeHalfSize.x;
		float y = sampleState.ShapeHalfSize.y;
		float num7 = 2f * y;
		float num8 = MBMath.SmoothStep(0.15f, 0.85f, targetOcclusion.amount);
		float num9 = DWAHelpers.GateNear(targetOcclusion.distance, num7, num7);
		float num10 = MathF.Clamp(num8 * num9, 0f, 1f);
		float num11 = 1f - num10;
		float num12 = _timeHorizon * MaxLinearSpeed;
		float num13 = _shipToTargetDistance - num6;
		float num14 = 0f;
		if (num13 >= 0f)
		{
			float a = MathF.Min(_shipToTargetDistance, num12);
			num14 = MathF.Clamp(num13 / MathF.Max(a, 0.001f), 0f, 1f);
		}
		float num15 = 8f * (1f - num14);
		float num16 = 0f;
		if (num13 < 0f)
		{
			num16 = (0f - num13) / num12;
			num16 = MathF.Min(num16, 1f);
		}
		float num17 = num * num16;
		float num18 = MathF.Clamp(Vec2.DotProduct(direction, _targetHeadingDir), -1f, 1f);
		float num19 = MathF.Clamp(Vec2.DotProduct(direction, vb), -1f, 1f);
		float num20 = 0.5f * (1f - num18);
		float num21 = 0.5f * (1f - num19);
		float num22 = DWAHelpers.GateNear(distance, 0.5f * x, 0.5f * x);
		float num23 = num22 * num20 + (1f - num22) * num21;
		float num24 = 0.2f + 0.8f * DWAHelpers.GateNear(num6, 2.5f * num7, x);
		float num25 = num2 * (num11 * num11) * num24 * num23;
		float num26 = MathF.Clamp(Vec2.DotProduct(linearVelocity, direction) / MaxLinearSpeed, -1f, 1f);
		float num27 = DWAHelpers.GateFar(num6, 2f * num7, num7);
		float num28 = num3 * num11 * num27 * MathF.Max(0f, 0f - num26);
		float num29 = Vec2.DotProduct(linearVelocity, _targetHeadingDir);
		float value = MathF.Abs(_targetSpeed - num29) / MaxLinearSpeed;
		float num30 = DWAHelpers.GateNear(num6, 3f * num7, num7);
		float num31 = num4 * num11 * num30 * MathF.Clamp(value, 0f, 1f);
		Vec2 vb2 = OwnerShip.Scene.GetGlobalWindVelocity();
		if (vb2.Normalize() <= 1E-06f)
		{
			vb2 = _targetHeadingDir;
		}
		float num32 = MathF.Clamp(Vec2.DotProduct(direction, vb2), -1f, 1f);
		float num33 = 0.5f * (1f - num32);
		float num34 = DWAHelpers.GateFar(num6, 2f * num7, num7);
		float num35 = num5 * num11 * num34 * num33;
		return num15 + num17 + num25 + num28 + num31 + num35;
	}

	void IDWAAgentDelegate.ComputeExternalAccelerationsOnState(float dt, in DWAAgentState state, out Vec2 extLinearAcc, out float extAngularAcc)
	{
		extLinearAcc = Vec2.Zero;
		extAngularAcc = 0f;
		MatrixFrame globalFrame = default(MatrixFrame);
		globalFrame.origin = state.Position3D;
		globalFrame.rotation.f = state.Direction.ToVec3();
		globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		NavalDLC.Missions.NavalPhysics.NavalPhysics physics = OwnerShip.Physics;
		NavalDLC.Missions.NavalPhysics.NavalPhysics.BuoyancyComputationResult buoyancyComputationResult = new NavalDLC.Missions.NavalPhysics.NavalPhysics.BuoyancyComputationResult
		{
			NetGlobalBuoyancyForce = physics.Mass * -MBGlobals.GravitationalAcceleration,
			SimulatingAirFriction = true,
			SubmergedHeightFactor = 1f,
			SubmergedFloaterCountFactor = 1f,
			PitchSubmergedAreaFactor = 1f,
			RollSubmergedAreaFactor = 1f
		};
		NavalDLC.Missions.NavalPhysics.NavalPhysics.DragForceComputationResult dragComputationResult = default(NavalDLC.Missions.NavalPhysics.NavalPhysics.DragForceComputationResult);
		MatrixFrame centerOfMassGlobalFrame = default(MatrixFrame);
		centerOfMassGlobalFrame.rotation = globalFrame.rotation;
		centerOfMassGlobalFrame.origin = globalFrame.TransformToParent(physics.LocalCenterOfMass);
		NavalDLC.Missions.NavalPhysics.NavalPhysics.ComputeAngularDrag(dt, 1, state.AngularVelocity * Vec3.Up, in centerOfMassGlobalFrame, physics.MassSpaceInertia, in physics.PhysicsParameters, in buoyancyComputationResult, in physics.AngularDragTerm, in physics.AngularDampingTerm, physics.AngularDragYSideComponentTerm, physics.AngularDampingYSideComponentTerm, ref dragComputationResult);
		NavalDLC.Missions.NavalPhysics.NavalPhysics.ComputeLinearDrag(dt, 1, state.LinearVelocity.ToVec3(), in globalFrame, physics.Mass, physics.LocalCenterOfMass, in physics.PhysicsParameters, in buoyancyComputationResult, physics.LinearDragTerm, physics.LinearDampingTerm, physics.ConstantLinearDampingTerm, physics.MinFloaterEntitialBottomPos, physics.MaxFloaterEntitialTopPos, ref dragComputationResult, out var _);
		extLinearAcc += (dragComputationResult.LateralDragForceGlobal.AsVec2 + dragComputationResult.LongitudinalDragForceGlobal.AsVec2) / physics.Mass;
		extAngularAcc += dragComputationResult.AngularDragTorqueGlobal.z / physics.MassSpaceInertia.z;
	}

	private void CacheDynamicParameters()
	{
		MatrixFrame globalFrame = OwnerShip.GlobalFrame;
		_state.Position = globalFrame.origin.AsVec2;
		_state.PositionZ = globalFrame.origin.z;
		_state.Direction = globalFrame.rotation.f.AsVec2.Normalized();
		_state.LinearVelocity = OwnerShip.Physics.LinearVelocity.AsVec2;
		_state.AngularVelocity = OwnerShip.Physics.AngularVelocity.z;
	}

	private static float ComputeDetectionRadius(float halfLength, float timeHorizon, float maxLinearSpeed)
	{
		return 4f * halfLength + timeHorizon * maxLinearSpeed;
	}

	private void CacheShipTrajectoryData(in Vec2 targetPos, in Vec2 targetDir, float targetSpeed)
	{
		_targetPos = targetPos;
		_targetSpeed = targetSpeed;
		Vec2 vec = OwnerShip.GlobalFrame.rotation.f.AsVec2.Normalized();
		_shipToTargetDir = _targetPos - State.Position;
		_shipToTargetDistance = _shipToTargetDir.Normalize();
		if (_shipToTargetDistance <= 1E-06f)
		{
			_shipToTargetDir = vec;
			_shipToTargetDistance = 0f;
		}
		_targetHeadingDir = targetDir;
		if (_targetHeadingDir.Normalize() <= 1E-06f)
		{
			_targetHeadingDir = vec;
		}
		_dotShipFwdToTargetHeading = Vec2.DotProduct(vec, _targetHeadingDir);
		Vec2 vec2 = State.Position - _targetPos;
		Vec2 vec3 = Vec2.DotProduct(vec2, _targetHeadingDir) * _targetHeadingDir;
		Vec2 vec4 = vec2 - vec3;
		if (vec3.LengthSquared >= 1E-05f)
		{
			_shipToTargetTangentDir = -vec3;
			_shipToTargetTangentDir.Normalize();
		}
		else
		{
			_shipToTargetTangentDir = -_targetHeadingDir;
		}
		if (vec4.LengthSquared >= 1E-05f)
		{
			_shipToTargetNormalDir = -vec4;
		}
		else
		{
			_shipToTargetNormalDir = (-_targetHeadingDir).LeftVec();
		}
	}

	private void SetTimeHorizon(float timeHorizon)
	{
		_timeHorizon = timeHorizon;
		_detectionRadius = ComputeDetectionRadius(_state.ShapeHalfSize.y, timeHorizon, OwnerShip.MissionShipObject.MaxLinearSpeed);
	}
}
