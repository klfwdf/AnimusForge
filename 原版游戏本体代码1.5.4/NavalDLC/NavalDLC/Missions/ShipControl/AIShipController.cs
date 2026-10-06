using System;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipInput;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.ShipControl;

public class AIShipController : ShipController
{
	public enum TargetMode
	{
		None,
		Position,
		State,
		Ship,
		ShipOffset
	}

	public const float ProportionalControllerSamplingPeriod = 1f / 30f;

	private const float LateralInputAccelerationThreshold = 0.01f;

	private const float LongitudinalInputAccelerationThreshold = 0.01f;

	private const float RaisedSailInputThresholdMultiplier = 0.2f;

	private const float FullSailInputThresholdMultiplier = 0.6f;

	private TargetMode _targetMode;

	private NavalState _targetState;

	private NavalVec _targetOffset;

	private bool _stopOnArrival;

	private bool _ignoreTargetShipCollision;

	private uint _rowerLateralDebounceCounter;

	private uint _rowerLongitudinalDebounceCounter;

	private uint _rudderLateralDebounceCounter;

	private uint _sailDebounceCounter;

	private ShipInputRecord _inputRecord;

	private NavalShipsLogic _navalShipsLogic;

	private NavigationPath _navigationPath;

	private int _lastNavPathPointIndex = -1;

	private UIntPtr _lastNavPathStartFace;

	private UIntPtr _lastNavPathTargetFace;

	private Vec2 _lastNavPathTargetPosition;

	private float _navPathTargetDriftAccumulator;

	private float _lastNavPathHardRecomputeTime;

	private bool _collisionChecksActive = true;

	private bool _avoidShipCollisions = true;

	private bool _avoidObstacleCollisions = true;

	private MBList<MissionShip> _shipCollisionIgnoreList = new MBList<MissionShip>();

	public MissionShip TargetShip { get; private set; }

	internal MBReadOnlyList<MissionShip> ShipCollisionIgnoreList => _shipCollisionIgnoreList;

	public bool CanAvoidCollisions
	{
		get
		{
			if (_ownerShip.HasDWAAgent && CollisionChecksActive)
			{
				if (!AvoidShipCollisions)
				{
					return AvoidObstacleCollisions;
				}
				return true;
			}
			return false;
		}
	}

	internal bool CollisionChecksActive => _collisionChecksActive;

	internal bool AvoidShipCollisions => _avoidShipCollisions;

	internal bool AvoidObstacleCollisions => _avoidObstacleCollisions;

	internal float DesiredLinearAcceleration { get; private set; }

	internal float DesiredAngularAcceleration { get; private set; }

	public bool HasTargetState => _targetMode != TargetMode.None;

	public bool HasTarget => _targetMode != TargetMode.None;

	public bool HasNavigationPath
	{
		get
		{
			if (_navigationPath == null && _navalShipsLogic.SeaPathfindingEnabled)
			{
				_navigationPath = new NavigationPath();
				_lastNavPathPointIndex = -1;
			}
			return _navigationPath != null;
		}
	}

	public AIShipController(MissionShip ownerShip)
		: base(ownerShip)
	{
		_controllerType = ShipControllerType.AI;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		ClearTarget();
	}

	public override ShipInputRecord Update(float dt)
	{
		ShipInputRecord inputRecord = ShipInputRecord.None();
		if (UpdateTargetState())
		{
			float postionErrorSquared;
			float rotationError;
			bool flag = HasArrivedAtTarget(out postionErrorSquared, out rotationError);
			if (_stopOnArrival && flag)
			{
				ClearTarget();
				inputRecord = ShipInputRecord.Stop();
			}
			else if (!flag)
			{
				ShipInputRecord oldInputRecord = inputRecord;
				Vec2 shipForward2D = _ownerShip.GlobalFrame.rotation.f.AsVec2.Normalized();
				Vec3 shipLocalVelocity = _ownerShip.GameEntity.GetGlobalFrame().rotation.TransformToLocal(_ownerShip.Physics.LinearVelocity);
				DecideControl(in oldInputRecord, in shipForward2D, _ownerShip.Scene.GetGlobalWindVelocity(), DesiredAngularAcceleration, DesiredLinearAcceleration, _ownerShip.MissionShipObject.MaxLinearAccel, _ownerShip.MissionShipObject.MaxAngularAccel, out inputRecord, shipLocalVelocity, _ownerShip.ShipOrder.EnforceSailUsage);
			}
		}
		_inputRecord = inputRecord;
		return _inputRecord;
	}

	public void SetTargetPosition(in Vec2 targetPosition, bool stopOnArrival = false)
	{
		_targetMode = TargetMode.Position;
		SetTargetShipAux(null);
		_targetOffset = NavalVec.Zero;
		_stopOnArrival = stopOnArrival;
		NavalState newTargetState = new NavalState(in targetPosition, _ownerShip.GlobalFrame.rotation.f.AsVec2.Normalized());
		if (HasNavigationPath)
		{
			ReComputeNavigationPath(_ownerShip.GetNavalState(), in newTargetState);
		}
		_targetState = newTargetState;
	}

	public void SetTargetState(in Vec2 targetPosition, in Vec2 targetDirection, bool stopOnArrival = false)
	{
		_targetMode = TargetMode.State;
		SetTargetShipAux(null);
		_targetOffset = NavalVec.Zero;
		_stopOnArrival = stopOnArrival;
		NavalState newTargetState = new NavalState(in targetPosition, in targetDirection);
		if (HasNavigationPath)
		{
			ReComputeNavigationPath(_ownerShip.GetNavalState(), in newTargetState);
		}
		_targetState = newTargetState;
	}

	public void SetTargetState(in NavalState targetState, bool stopOnArrival = false)
	{
		_targetMode = TargetMode.State;
		SetTargetShipAux(null);
		_targetOffset = NavalVec.Zero;
		_stopOnArrival = stopOnArrival;
		if (HasNavigationPath)
		{
			ReComputeNavigationPath(_ownerShip.GetNavalState(), in targetState);
		}
		_targetState = targetState;
	}

	public void SetTargetShip(in MissionShip targetShip, bool stopOnArrival = false, bool ignoreTargetShipCollision = false)
	{
		_targetMode = TargetMode.Ship;
		SetTargetShipAux(targetShip, ignoreTargetShipCollision);
		_targetOffset = NavalVec.Zero;
		_stopOnArrival = stopOnArrival;
		NavalState newTargetState = TargetShip.GetNavalState();
		if (HasNavigationPath)
		{
			ReComputeNavigationPath(_ownerShip.GetNavalState(), in newTargetState);
		}
		_targetState = newTargetState;
	}

	public void SetTargetShipWithOffset(in MissionShip targetShip, in NavalVec localOffset, bool stopOnArrival = false, bool ignoreTargetShipCollision = false)
	{
		_targetMode = TargetMode.ShipOffset;
		SetTargetShipAux(targetShip, ignoreTargetShipCollision);
		_targetOffset = localOffset;
		_stopOnArrival = stopOnArrival;
		NavalState newTargetState = TargetShip.GetNavalState(in localOffset);
		if (HasNavigationPath)
		{
			ReComputeNavigationPath(_ownerShip.GetNavalState(), in newTargetState);
		}
		_targetState = newTargetState;
	}

	internal void AddShipToCollisionIgnoreListOnAccountOfRamming(MissionShip ship)
	{
		AddShipToCollisionIgnoreList(ship);
	}

	internal void AddShipToCollisionIgnoreList(MissionShip ship)
	{
		if (!_shipCollisionIgnoreList.Contains(ship))
		{
			_shipCollisionIgnoreList.Add(ship);
		}
	}

	internal void SetAvoidShipCollisions(bool value = true)
	{
		_avoidShipCollisions = value;
	}

	internal void RemoveShipFromCollisionIgnoreListOnAccountOfRamming(MissionShip ship)
	{
		RemoveShipFromCollisionIgnoreList(ship);
	}

	internal void RemoveShipFromCollisionIgnoreList(MissionShip ship)
	{
		_shipCollisionIgnoreList.Remove(ship);
	}

	internal void SetAvoidObstacleCollisions(bool value = true)
	{
		_avoidObstacleCollisions = value;
	}

	internal void SetCollisionChecksActive(bool value = true)
	{
		_collisionChecksActive = value;
	}

	internal void ClearShipCollisionIgnoreList()
	{
		_shipCollisionIgnoreList.Clear();
	}

	internal bool CheckShipInCollisionIgnoreList(MissionShip ship)
	{
		return _shipCollisionIgnoreList.Contains(ship);
	}

	public bool GetRawTargetState(out Vec2 targetPosition, out Vec2 targetDirection, out float targetSpeed)
	{
		if (_targetMode != TargetMode.None)
		{
			targetPosition = _targetState.Position;
			targetDirection = _targetState.Direction;
			targetSpeed = _targetState.Speed;
			return true;
		}
		targetPosition = Vec2.Invalid;
		targetDirection = Vec2.Invalid;
		targetSpeed = 0f;
		return false;
	}

	public bool GetNextTarget(out Vec2 targetPosition, out Vec2 targetDirection, out float targetSpeed)
	{
		if (_targetMode != TargetMode.None)
		{
			if (HasNavigationPath && _navigationPath.Size > 0)
			{
				NavalState nextTargetStateOverPath = GetNextTargetStateOverPath();
				targetPosition = nextTargetStateOverPath.Position;
				targetDirection = nextTargetStateOverPath.Direction;
				targetSpeed = _targetState.Speed;
				return true;
			}
			return GetRawTargetState(out targetPosition, out targetDirection, out targetSpeed);
		}
		targetPosition = Vec2.Invalid;
		targetDirection = Vec2.Invalid;
		targetSpeed = 0f;
		return false;
	}

	public bool HasArrivedAtTarget(out float postionErrorSquared, out float rotationError)
	{
		float num = _ownerShip.Physics.PhysicsBoundingBoxSizeWithoutChildren.y / 20f;
		float num2 = System.MathF.PI / 12f;
		NavalState fromState = _ownerShip.GetNavalState();
		NavalVec navalVec = _targetState - fromState;
		postionErrorSquared = navalVec.DeltaPosition.LengthSquared;
		rotationError = TaleWorlds.Library.MathF.Abs(navalVec.DeltaOrientation);
		if (postionErrorSquared < num * num && rotationError < num2)
		{
			return true;
		}
		return false;
	}

	internal void UpdateTrajectory(float desiredLinearAcceleration, float desiredAngularAcceleration)
	{
		DesiredLinearAcceleration = desiredLinearAcceleration;
		DesiredAngularAcceleration = desiredAngularAcceleration;
	}

	public void ClearTarget()
	{
		if (_ignoreTargetShipCollision)
		{
			RemoveShipFromCollisionIgnoreList(TargetShip);
			_ignoreTargetShipCollision = false;
		}
		TargetShip = null;
		_targetMode = TargetMode.None;
		_targetState = NavalState.Zero;
		if (HasNavigationPath)
		{
			ClearNavigationPathAux();
		}
		_targetOffset = NavalVec.Zero;
		_stopOnArrival = false;
	}

	public bool UpdateTargetState()
	{
		if (_targetMode != TargetMode.None)
		{
			NavalState currentState = _ownerShip.GetNavalState();
			if (_targetMode == TargetMode.Position)
			{
				_targetState.SetTargetDirection(_ownerShip.GlobalFrame.rotation.f.AsVec2.Normalized());
				if (HasNavigationPath)
				{
					if (_navigationPath.Size > 0)
					{
						UpdateNavigationPath(in currentState);
					}
					else
					{
						ReComputeNavigationPath(in currentState, in _targetState);
					}
				}
				return true;
			}
			if (_targetMode == TargetMode.State)
			{
				if (HasNavigationPath)
				{
					if (_navigationPath.Size > 0)
					{
						UpdateNavigationPath(in currentState);
					}
					else
					{
						ReComputeNavigationPath(in currentState, in _targetState);
					}
				}
				return true;
			}
			if (_targetMode == TargetMode.Ship || _targetMode == TargetMode.ShipOffset)
			{
				NavalState newTargetState = ((_targetMode != TargetMode.Ship) ? TargetShip.GetNavalState(in _targetOffset) : TargetShip.GetNavalState());
				if (HasNavigationPath)
				{
					ReComputeNavigationPath(in currentState, in newTargetState);
				}
				_targetState = newTargetState;
				return true;
			}
		}
		return false;
	}

	public float GetTargetStateZ()
	{
		float result = 0f;
		if (_targetMode != TargetMode.None)
		{
			if (_targetMode == TargetMode.Ship)
			{
				result = TargetShip.GlobalFrame.origin.z;
			}
			else if (_targetMode == TargetMode.ShipOffset)
			{
				Vec3 origin = TargetShip.GlobalFrame.origin;
				float waterLevelAtPosition = _ownerShip.Scene.GetWaterLevelAtPosition(origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
				float num = TaleWorlds.Library.MathF.Max(0f, origin.z - waterLevelAtPosition);
				result = _ownerShip.Scene.GetWaterLevelAtPosition(_targetState.Position, useWaterRenderer: true, checkWaterBodyEntities: false) + num;
			}
			else
			{
				result = _ownerShip.Scene.GetWaterLevelAtPosition(_targetState.Position, useWaterRenderer: true, checkWaterBodyEntities: false);
			}
		}
		return result;
	}

	private ShipInputRecord StabilizeInput(ShipInputRecord inputRecord)
	{
		int num = 5;
		RowerLateralInput rowerLateral = _inputRecord.RowerLateral;
		RowerLongitudinalInput rowerLongitudinal = _inputRecord.RowerLongitudinal;
		RowerLongitudinalInput rowerLongitudinalDoubleTap = _inputRecord.RowerLongitudinalDoubleTap;
		float rudderLateral = _inputRecord.RudderLateral;
		SailInput sail = _inputRecord.Sail;
		if (inputRecord.RowerLateral != rowerLateral)
		{
			_rowerLateralDebounceCounter++;
			if (_rowerLateralDebounceCounter >= num)
			{
				rowerLateral = inputRecord.RowerLateral;
				_rowerLateralDebounceCounter = 0u;
			}
		}
		else
		{
			_rowerLateralDebounceCounter = 0u;
		}
		if (inputRecord.RowerLongitudinal != rowerLongitudinal)
		{
			_rowerLongitudinalDebounceCounter++;
			if (_rowerLongitudinalDebounceCounter >= num)
			{
				rowerLongitudinal = inputRecord.RowerLongitudinal;
				_rowerLongitudinalDebounceCounter = 0u;
			}
		}
		else
		{
			_rowerLongitudinalDebounceCounter = 0u;
		}
		if (inputRecord.RudderLateral != rudderLateral)
		{
			_rudderLateralDebounceCounter++;
			if (_rudderLateralDebounceCounter >= num)
			{
				rudderLateral = inputRecord.RudderLateral;
				_rudderLateralDebounceCounter = 0u;
			}
		}
		else
		{
			_rudderLateralDebounceCounter = 0u;
		}
		if (inputRecord.Sail != sail)
		{
			_sailDebounceCounter++;
			if (_sailDebounceCounter >= num)
			{
				sail = inputRecord.Sail;
				_sailDebounceCounter = 0u;
			}
		}
		else
		{
			_sailDebounceCounter = 0u;
		}
		return new ShipInputRecord(rowerLateral, rowerLongitudinal, rowerLongitudinalDoubleTap, rudderLateral, sail);
	}

	private void SetTargetShipAux(MissionShip targetShip, bool ignoreCollision = false)
	{
		if (_ignoreTargetShipCollision != ignoreCollision || TargetShip != targetShip)
		{
			if (_ignoreTargetShipCollision)
			{
				RemoveShipFromCollisionIgnoreList(TargetShip);
			}
			if (ignoreCollision)
			{
				AddShipToCollisionIgnoreList(targetShip);
			}
			_ignoreTargetShipCollision = ignoreCollision;
			TargetShip = targetShip;
		}
	}

	private static void DecideControl(in ShipInputRecord oldInputRecord, in Vec2 shipForward2D, in Vec2 globalWindVelocity, float desiredAngularAcceleration, in float desiredLinearAcceleration, float maxLinearAcceleration, float maxAngularAcceleration, out ShipInputRecord inputRecord, Vec3 shipLocalVelocity, int enforceSailUsage)
	{
		inputRecord = ShipInputRecord.None();
		float num = TaleWorlds.Library.MathF.Abs(desiredAngularAcceleration);
		float num2 = TaleWorlds.Library.MathF.Abs(desiredLinearAcceleration);
		bool flag = num > 0.3f || (num > 0f && num2 <= 0.001f);
		if (num > 0.01f && flag)
		{
			if (desiredAngularAcceleration > 0f)
			{
				inputRecord.SetRowerLateral(RowerLateralInput.Left);
			}
			else if (desiredAngularAcceleration < 0f)
			{
				inputRecord.SetRowerLateral(RowerLateralInput.Right);
			}
		}
		if (flag)
		{
			if (shipLocalVelocity.y > 1f)
			{
				inputRecord.SetRowerLongitudinal(RowerLongitudinalInput.Backward);
			}
			else if (shipLocalVelocity.y < -1f)
			{
				inputRecord.SetRowerLongitudinal(RowerLongitudinalInput.Forward);
			}
		}
		else if (num2 >= 0.01f)
		{
			if (desiredLinearAcceleration >= 0f)
			{
				inputRecord.SetRowerLongitudinal(RowerLongitudinalInput.Forward);
			}
			else
			{
				inputRecord.SetRowerLongitudinal(RowerLongitudinalInput.Backward);
			}
		}
		float rudderLateral = 0f;
		if (flag)
		{
			rudderLateral = inputRecord.RowerLateral.ToRudderInput();
		}
		else if (desiredAngularAcceleration > 0f)
		{
			rudderLateral = -1f;
		}
		else if (desiredAngularAcceleration < 0f)
		{
			rudderLateral = 1f;
		}
		inputRecord.SetRudderLateral(rudderLateral);
		float num3 = Vec2.DotProduct(globalWindVelocity.Normalized(), shipForward2D) * desiredLinearAcceleration;
		float num4 = 0.2f * maxLinearAcceleration;
		float num5 = 0.6f * maxLinearAcceleration;
		if (enforceSailUsage > 0)
		{
			inputRecord.SetSail(SailInput.Full);
		}
		else if (enforceSailUsage < 0)
		{
			inputRecord.SetSail(SailInput.Raised);
		}
		else if (flag)
		{
			inputRecord.SetSail(SailInput.Raised);
		}
		else if (num3 > num5)
		{
			inputRecord.SetSail(SailInput.Full);
		}
		else if (num3 < num4)
		{
			inputRecord.SetSail(SailInput.Raised);
		}
		else
		{
			inputRecord.SetSail(oldInputRecord.Sail);
		}
	}

	private void ReComputeNavigationPath(in NavalState currentState, in NavalState newTargetState, bool forceRecompute = false)
	{
		if (forceRecompute || ShouldRecomputePath(in currentState, in newTargetState))
		{
			Vec3 position = new Vec3(currentState.Position);
			Vec3 position2 = new Vec3(newTargetState.Position);
			Mission.Current.Scene.SetAbilityOfFacesWithId(1, isEnabled: true);
			UIntPtr nearestNavigationMeshForPosition = Mission.Current.Scene.GetNearestNavigationMeshForPosition(in position, 1000000f, excludeDynamicNavigationMeshes: true);
			UIntPtr nearestNavigationMeshForPosition2 = Mission.Current.Scene.GetNearestNavigationMeshForPosition(in position2, 1000000f, excludeDynamicNavigationMeshes: true);
			if (nearestNavigationMeshForPosition != UIntPtr.Zero && nearestNavigationMeshForPosition2 != UIntPtr.Zero)
			{
				float agentRadius = TaleWorlds.Library.MathF.Lerp(_ownerShip.Physics.PhysicsBoundingBoxSizeWithoutChildren.x, _ownerShip.Physics.PhysicsBoundingBoxSizeWithoutChildren.y, 0.75f);
				bool num = nearestNavigationMeshForPosition == _lastNavPathStartFace && nearestNavigationMeshForPosition2 == _lastNavPathTargetFace;
				bool flag = false;
				bool flag2 = _navigationPath.Size > 0;
				if (!num || !flag2)
				{
					_navigationPath.Size = 0;
					flag2 = Mission.Current.Scene.GetPathBetweenAIFaces(nearestNavigationMeshForPosition, nearestNavigationMeshForPosition2, position.AsVec2, position2.AsVec2, agentRadius, _navigationPath, null);
					flag = true;
				}
				else if (flag2)
				{
					_navigationPath.OverridePathPointAtIndex(_navigationPath.Size - 1, newTargetState.Position);
				}
				Mission.Current.Scene.SetAbilityOfFacesWithId(1, isEnabled: false);
				if (flag2)
				{
					if (flag)
					{
						_lastNavPathPointIndex = 0;
						_lastNavPathHardRecomputeTime = Mission.Current.CurrentTime;
					}
					_lastNavPathStartFace = nearestNavigationMeshForPosition;
					_lastNavPathTargetFace = nearestNavigationMeshForPosition2;
					_lastNavPathTargetPosition = newTargetState.Position;
					_navPathTargetDriftAccumulator = 0f;
					UpdateNavigationPath(in currentState);
				}
				else
				{
					ClearNavigationPathAux();
				}
			}
			else
			{
				ClearNavigationPathAux();
			}
		}
		else
		{
			if (_navigationPath.Size > 0)
			{
				_navigationPath.OverridePathPointAtIndex(_navigationPath.Size - 1, newTargetState.Position);
			}
			float length = (newTargetState.Position - _lastNavPathTargetPosition).Length;
			if (length >= 0.0001f)
			{
				_navPathTargetDriftAccumulator += length;
			}
			_lastNavPathTargetPosition = newTargetState.Position;
			UpdateNavigationPath(in currentState);
		}
	}

	private bool ShouldRecomputePath(in NavalState currentState, in NavalState newTargetState)
	{
		if (_navigationPath.Size == 0)
		{
			return true;
		}
		float num = Mission.Current.CurrentTime - _lastNavPathHardRecomputeTime;
		if ((_lastNavPathTargetPosition - newTargetState.Position).LengthSquared >= 16f)
		{
			return true;
		}
		if (num >= 0.5f)
		{
			if (_navPathTargetDriftAccumulator >= 4f)
			{
				return true;
			}
			if (NavPathStartOrGoalFaceChanged(currentState.Position, newTargetState.Position))
			{
				return true;
			}
		}
		return false;
	}

	private bool NavPathStartOrGoalFaceChanged(in Vec2 currentPos, in Vec2 newTargetPos)
	{
		bool result = false;
		Mission.Current.Scene.SetAbilityOfFacesWithId(1, isEnabled: true);
		Vec3 position = new Vec3(currentPos);
		Vec3 position2 = new Vec3(newTargetPos);
		UIntPtr nearestNavigationMeshForPosition = Mission.Current.Scene.GetNearestNavigationMeshForPosition(in position, 1000000f, excludeDynamicNavigationMeshes: true);
		UIntPtr nearestNavigationMeshForPosition2 = Mission.Current.Scene.GetNearestNavigationMeshForPosition(in position2, 1000000f, excludeDynamicNavigationMeshes: true);
		if (nearestNavigationMeshForPosition == UIntPtr.Zero || nearestNavigationMeshForPosition2 == UIntPtr.Zero)
		{
			result = true;
		}
		else if (nearestNavigationMeshForPosition != _lastNavPathStartFace || nearestNavigationMeshForPosition2 != _lastNavPathTargetFace)
		{
			result = true;
		}
		Mission.Current.Scene.SetAbilityOfFacesWithId(1, isEnabled: false);
		return result;
	}

	private void UpdateNavigationPath(in NavalState currentState)
	{
		Vec2[] pathPoints = _navigationPath.PathPoints;
		int num = _navigationPath.Size - 1;
		Vec2 position = currentState.Position;
		while (_lastNavPathPointIndex < num)
		{
			int lastNavPathPointIndex = _lastNavPathPointIndex;
			int num2 = lastNavPathPointIndex + 1;
			Vec2 vec = pathPoints[lastNavPathPointIndex];
			Vec2 vec2 = pathPoints[num2];
			Vec2 vec3 = position - vec;
			if (vec3.LengthSquared <= 900f)
			{
				_lastNavPathPointIndex++;
				continue;
			}
			Vec2 v = vec2 - vec;
			if (vec3.DotProduct(v) > 0f)
			{
				_lastNavPathPointIndex++;
				continue;
			}
			break;
		}
	}

	private NavalState GetNextTargetStateOverPath()
	{
		if (_lastNavPathPointIndex < _navigationPath.Size - 1)
		{
			return new NavalState(_navigationPath[_lastNavPathPointIndex], (_navigationPath[_lastNavPathPointIndex + 1] - _navigationPath[_lastNavPathPointIndex]).RotationInRadians);
		}
		return _targetState;
	}

	private void ClearNavigationPathAux()
	{
		_navigationPath.Size = 0;
		_lastNavPathPointIndex = -1;
		_lastNavPathStartFace = UIntPtr.Zero;
		_lastNavPathTargetFace = UIntPtr.Zero;
		_lastNavPathTargetPosition = Vec2.Zero;
		_navPathTargetDriftAccumulator = 0f;
	}
}
