using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SiegeWeaponMovementComponent : UsableMissionObjectComponent
{
	public const string GhostObjectTag = "ghost_object";

	private const string WheelTag = "wheel";

	public const string MoveStandingPointTag = "move";

	public float AxleLength = 2.45f;

	public int NavMeshIdToDisableOnDestination = -1;

	private float _ghostObjectPos;

	private List<GameEntity> _wheels;

	private List<StandingPoint> _standingPoints;

	private MatrixFrame[] _standingPointLocalIKFrames;

	private SoundEvent _movementSound;

	private float _wheelCircumference;

	private bool _isMoveSoundPlaying;

	private float _wheelDiameter;

	private Path _path;

	private PathTracker _pathTracker;

	private PathTracker _ghostEntityPathTracker;

	private float _advancementError;

	private float _lastSynchronizedDistance;

	public bool HasApproachedTarget
	{
		get
		{
			if (_pathTracker.PathExists())
			{
				return _pathTracker.PathTraveledPercentage > 0.7f;
			}
			return true;
		}
	}

	public Vec3 Velocity { get; private set; }

	public bool HasArrivedAtTarget
	{
		get
		{
			if (_pathTracker.PathExists())
			{
				return _pathTracker.HasReachedEnd;
			}
			return true;
		}
	}

	public float CurrentSpeed { get; private set; }

	public int MovementSoundCodeID { get; set; }

	public float MinSpeed { get; set; }

	public float MaxSpeed { get; set; }

	public string PathEntityName { get; set; }

	public float GhostEntitySpeedMultiplier { get; set; }

	public float WheelDiameter
	{
		set
		{
			_wheelDiameter = value;
			_wheelCircumference = _wheelDiameter * System.MathF.PI;
		}
	}

	public SynchedMissionObject MainObject { get; set; }

	protected internal override void OnAdded(Scene scene)
	{
		base.OnAdded(scene);
		_path = scene.GetPathWithName(PathEntityName);
		Vec3 scaleVector = MainObject.GameEntity.GetFrame().rotation.GetScaleVector();
		_wheels = GameEntity.CreateFromWeakEntity(MainObject.GameEntity).CollectChildrenEntitiesWithTag("wheel");
		_standingPoints = MainObject.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<StandingPoint>("move");
		_pathTracker = new PathTracker(_path, scaleVector);
		_pathTracker.Reset();
		SetTargetFrame();
		MatrixFrame m = MainObject.GameEntity.GetGlobalFrame();
		_standingPointLocalIKFrames = new MatrixFrame[_standingPoints.Count];
		for (int i = 0; i < _standingPoints.Count; i++)
		{
			_standingPointLocalIKFrames[i] = _standingPoints[i].GameEntity.GetGlobalFrame().TransformToLocal(in m);
			_standingPoints[i].AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
		}
		Velocity = Vec3.Zero;
	}

	public void HighlightPath()
	{
		MatrixFrame[] array = new MatrixFrame[_path.NumberOfPoints];
		_path.GetPoints(array);
		_ = ref array[0];
		for (int i = 1; i < _path.NumberOfPoints; i++)
		{
			_ = array[i];
		}
	}

	public void SetupGhostEntity()
	{
		Path pathWithName = MainObject.Scene.GetPathWithName(PathEntityName);
		Vec3 scaleVector = MainObject.GameEntity.GetFrame().rotation.GetScaleVector();
		_pathTracker = new PathTracker(pathWithName, scaleVector);
		_ghostEntityPathTracker = new PathTracker(pathWithName, scaleVector);
		_ghostObjectPos = ((pathWithName != null) ? pathWithName.GetTotalLength() : 0f);
		_wheels = GameEntity.CreateFromWeakEntity(MainObject.GameEntity).CollectChildrenEntitiesWithTag("wheel");
	}

	private void SetPath()
	{
		Path pathWithName = MainObject.Scene.GetPathWithName(PathEntityName);
		Vec3 scaleVector = MainObject.GameEntity.GetFrame().rotation.GetScaleVector();
		_pathTracker = new PathTracker(pathWithName, scaleVector);
		_ghostEntityPathTracker = new PathTracker(pathWithName, scaleVector);
		_ghostObjectPos = ((pathWithName != null) ? pathWithName.GetTotalLength() : 0f);
		UpdateGhostObject(0f);
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		UpdateGhostObject(dt);
	}

	public void SetGhostVisibility(bool isVisible)
	{
		MainObject.GameEntity.CollectChildrenEntitiesWithTag("ghost_object").FirstOrDefault().SetVisibilityExcludeParents(isVisible);
	}

	public void OnEditorInit()
	{
		SetPath();
		_wheels = GameEntity.CreateFromWeakEntity(MainObject.GameEntity).CollectChildrenEntitiesWithTag("wheel");
	}

	private void UpdateGhostObject(float dt)
	{
		if (_pathTracker.HasChanged)
		{
			SetPath();
			_pathTracker.Advance(_pathTracker.GetPathLength());
			_ghostEntityPathTracker.Advance(_ghostEntityPathTracker.GetPathLength());
		}
		List<WeakGameEntity> list = MainObject.GameEntity.CollectChildrenEntitiesWithTag("ghost_object");
		if (MainObject.GameEntity.IsSelectedOnEditor())
		{
			if (_pathTracker.IsValid)
			{
				float num = 10f;
				if (Input.DebugInput.IsShiftDown())
				{
					num = 1f;
				}
				if (Input.DebugInput.IsKeyDown(InputKey.MouseScrollUp))
				{
					_ghostObjectPos += dt * num;
				}
				else if (Input.DebugInput.IsKeyDown(InputKey.MouseScrollDown))
				{
					_ghostObjectPos -= dt * num;
				}
				_ghostObjectPos = MBMath.ClampFloat(_ghostObjectPos, 0f, _pathTracker.GetPathLength());
			}
			else
			{
				_ghostObjectPos = 0f;
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		WeakGameEntity weakGameEntity = list[0];
		if (MainObject is IPathHolder { EditorGhostEntityMove: not false })
		{
			if (_ghostEntityPathTracker.IsValid)
			{
				_ghostEntityPathTracker.Advance(0.05f * GhostEntitySpeedMultiplier);
				weakGameEntity.SetGlobalFrame(LinearInterpolatedIK(ref _ghostEntityPathTracker));
				if (_ghostEntityPathTracker.HasReachedEnd)
				{
					_ghostEntityPathTracker.Reset();
				}
			}
		}
		else if (_pathTracker.IsValid)
		{
			_pathTracker.Advance(_ghostObjectPos);
			MatrixFrame frame = LinearInterpolatedIK(ref _pathTracker);
			weakGameEntity.SetGlobalFrame(FindGroundFrameForWheels(ref frame));
			_pathTracker.Reset();
		}
	}

	private void RotateWheels(float angleInRadian)
	{
		foreach (GameEntity wheel in _wheels)
		{
			MatrixFrame frame = wheel.GetFrame();
			frame.rotation.RotateAboutSide(angleInRadian);
			wheel.SetFrame(ref frame);
		}
	}

	private MatrixFrame LinearInterpolatedIK(ref PathTracker pathTracker)
	{
		pathTracker.CurrentFrameAndColor(out var frame, out var color);
		return MatrixFrame.Lerp(in frame, FindGroundFrameForWheels(ref frame), color.x);
	}

	public void SetDistanceTraveledAsClient(float distance)
	{
		_advancementError = distance - _pathTracker.TotalDistanceTraveled;
	}

	public override bool IsOnTickRequired()
	{
		return true;
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_ghostEntityPathTracker != null)
		{
			UpdateGhostObject(dt);
		}
		if (!_pathTracker.PathExists() || _pathTracker.HasReachedEnd)
		{
			CurrentSpeed = 0f;
			if (!GameNetwork.IsClientOrReplay)
			{
				foreach (StandingPoint standingPoint in _standingPoints)
				{
					standingPoint.SetIsDeactivatedSynched(value: true);
				}
			}
		}
		TickSound();
	}

	public void TickParallelManually(float dt)
	{
		if (!_pathTracker.PathExists() || _pathTracker.HasReachedEnd)
		{
			return;
		}
		int num = 0;
		foreach (StandingPoint standingPoint2 in _standingPoints)
		{
			if (standingPoint2.HasUser && !standingPoint2.UserAgent.IsInBeingStruckAction)
			{
				num++;
			}
		}
		if (num > 0)
		{
			int count = _standingPoints.Count;
			CurrentSpeed = MBMath.Lerp(MinSpeed, MaxSpeed, (float)(num - 1) / (float)(count - 1));
			MatrixFrame boundEntityGlobalFrame = MainObject.GameEntity.GetGlobalFrame();
			for (int i = 0; i < _standingPoints.Count; i++)
			{
				StandingPoint standingPoint = _standingPoints[i];
				if (!standingPoint.HasUser)
				{
					continue;
				}
				Agent userAgent = standingPoint.UserAgent;
				ActionIndexCache actionIndexCache = userAgent.GetCurrentAction(0);
				ActionIndexCache actionIndexCache2 = userAgent.GetCurrentAction(1);
				if (actionIndexCache != ActionIndexCache.act_usage_siege_machine_push)
				{
					if (userAgent.SetActionChannel(0, in ActionIndexCache.act_usage_siege_machine_push, ignorePriority: false, (AnimFlags)0uL, 0f, CurrentSpeed, MBAnimation.GetAnimationBlendInPeriod(MBActionSet.GetAnimationIndexOfAction(userAgent.ActionSet, in ActionIndexCache.act_usage_siege_machine_push)) * CurrentSpeed))
					{
						actionIndexCache = ActionIndexCache.act_usage_siege_machine_push;
					}
					else if (MBMath.IsBetween((int)userAgent.GetCurrentActionType(0), 48, 52) && actionIndexCache != ActionIndexCache.act_strike_bent_over && userAgent.SetActionChannel(0, in ActionIndexCache.act_strike_bent_over, ignorePriority: false, (AnimFlags)0uL))
					{
						actionIndexCache = ActionIndexCache.act_strike_bent_over;
					}
				}
				if (actionIndexCache2 != ActionIndexCache.act_usage_siege_machine_push)
				{
					if (userAgent.SetActionChannel(1, in ActionIndexCache.act_usage_siege_machine_push, ignorePriority: false, (AnimFlags)0uL, 0f, CurrentSpeed, MBAnimation.GetAnimationBlendInPeriod(MBActionSet.GetAnimationIndexOfAction(userAgent.ActionSet, in ActionIndexCache.act_usage_siege_machine_push)) * CurrentSpeed))
					{
						actionIndexCache2 = ActionIndexCache.act_usage_siege_machine_push;
					}
					else if (MBMath.IsBetween((int)userAgent.GetCurrentActionType(1), 48, 52) && actionIndexCache2 != ActionIndexCache.act_strike_bent_over && userAgent.SetActionChannel(1, in ActionIndexCache.act_strike_bent_over, ignorePriority: false, (AnimFlags)0uL))
					{
						actionIndexCache2 = ActionIndexCache.act_strike_bent_over;
					}
				}
				if (actionIndexCache == ActionIndexCache.act_usage_siege_machine_push)
				{
					userAgent.SetCurrentActionSpeed(0, CurrentSpeed);
				}
				if (actionIndexCache2 == ActionIndexCache.act_usage_siege_machine_push)
				{
					userAgent.SetCurrentActionSpeed(1, CurrentSpeed);
				}
				if ((actionIndexCache == ActionIndexCache.act_usage_siege_machine_push || actionIndexCache == ActionIndexCache.act_strike_bent_over) && (actionIndexCache2 == ActionIndexCache.act_usage_siege_machine_push || actionIndexCache2 == ActionIndexCache.act_strike_bent_over))
				{
					standingPoint.UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(in _standingPointLocalIKFrames[i], in boundEntityGlobalFrame);
					continue;
				}
				standingPoint.UserAgent.ClearHandInverseKinematics();
				if (!GameNetwork.IsClientOrReplay && userAgent.Controller != AgentControllerType.AI)
				{
					userAgent.StopUsingGameObjectMT(isSuccessful: false);
				}
			}
		}
		else
		{
			CurrentSpeed = _advancementError;
		}
		if (CurrentSpeed.ApproximatelyEqualsTo(0f))
		{
			return;
		}
		float num2 = CurrentSpeed * dt;
		if (!_advancementError.ApproximatelyEqualsTo(0f))
		{
			float num3 = 3f * CurrentSpeed * dt * (float)TaleWorlds.Library.MathF.Sign(_advancementError);
			if (TaleWorlds.Library.MathF.Abs(num3) >= TaleWorlds.Library.MathF.Abs(_advancementError))
			{
				num3 = _advancementError;
				_advancementError = 0f;
			}
			else
			{
				_advancementError -= num3;
			}
			num2 += num3;
		}
		_pathTracker.Advance(num2);
		SetTargetFrame();
		float angleInRadian = num2 / _wheelCircumference * 2f * System.MathF.PI;
		RotateWheels(angleInRadian);
		if (GameNetwork.IsServerOrRecorder && _pathTracker.TotalDistanceTraveled - _lastSynchronizedDistance > 1f)
		{
			_lastSynchronizedDistance = _pathTracker.TotalDistanceTraveled;
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetSiegeMachineMovementDistance(MainObject.Id, _lastSynchronizedDistance));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
	}

	public MatrixFrame GetInitialFrame()
	{
		PathTracker pathTracker = new PathTracker(_path, Vec3.One);
		pathTracker.Reset();
		return LinearInterpolatedIK(ref pathTracker);
	}

	private void SetTargetFrame()
	{
		if (_pathTracker.PathExists())
		{
			MatrixFrame frame = LinearInterpolatedIK(ref _pathTracker);
			WeakGameEntity gameEntity = MainObject.GameEntity;
			Velocity = gameEntity.GlobalPosition;
			gameEntity.SetGlobalFrame(in frame, isTeleportation: false);
			Velocity = (gameEntity.GlobalPosition - Velocity).NormalizedCopy() * CurrentSpeed;
		}
	}

	public MatrixFrame GetTargetFrame()
	{
		float totalDistanceTraveled = _pathTracker.TotalDistanceTraveled;
		_pathTracker.Advance(1000000f);
		MatrixFrame currentFrame = _pathTracker.CurrentFrame;
		_pathTracker.Reset();
		_pathTracker.Advance(totalDistanceTraveled);
		return currentFrame;
	}

	public void SetDestinationNavMeshIdState(bool enabled)
	{
		if (NavMeshIdToDisableOnDestination != -1)
		{
			Mission.Current.Scene.SetAbilityOfFacesWithId(NavMeshIdToDisableOnDestination, enabled);
		}
	}

	public void MoveToTargetAsClient()
	{
		if (_pathTracker.IsValid)
		{
			float totalDistanceTraveled = _pathTracker.TotalDistanceTraveled;
			_pathTracker.Advance(1000000f);
			SetTargetFrame();
			float angleInRadian = (_pathTracker.TotalDistanceTraveled - totalDistanceTraveled) / _wheelCircumference * 2f * System.MathF.PI;
			RotateWheels(angleInRadian);
		}
	}

	private void TickSound()
	{
		if (CurrentSpeed > 0f)
		{
			PlayMovementSound();
		}
		else
		{
			StopMovementSound();
		}
	}

	private void PlayMovementSound()
	{
		if (!_isMoveSoundPlaying)
		{
			_movementSound = SoundEvent.CreateEvent(MovementSoundCodeID, MainObject.GameEntity.Scene);
			_movementSound.Play();
			_isMoveSoundPlaying = true;
		}
		_movementSound.SetPosition(MainObject.GameEntity.GlobalPosition);
	}

	private void StopMovementSound()
	{
		if (_isMoveSoundPlaying)
		{
			_movementSound.Stop();
			_isMoveSoundPlaying = false;
		}
	}

	protected internal override void OnMissionReset()
	{
		base.OnMissionReset();
		CurrentSpeed = 0f;
		_lastSynchronizedDistance = 0f;
		_advancementError = 0f;
		_pathTracker.Reset();
		SetTargetFrame();
	}

	public float GetTotalDistanceTraveledForPathTracker()
	{
		return _pathTracker.TotalDistanceTraveled;
	}

	private MatrixFrame FindGroundFrameForWheels(ref MatrixFrame frame)
	{
		return FindGroundFrameForWheelsStatic(ref frame, AxleLength, _wheelDiameter, MainObject.GameEntity, _wheels, MainObject.Scene);
	}

	public void SetTotalDistanceTraveledForPathTracker(float distanceTraveled)
	{
		_pathTracker.TotalDistanceTraveled = distanceTraveled;
	}

	public void SetTargetFrameForPathTracker()
	{
		SetTargetFrame();
	}

	public static MatrixFrame FindGroundFrameForWheelsStatic(ref MatrixFrame frame, float axleLength, float wheelDiameter, WeakGameEntity gameEntity, List<GameEntity> wheels, Scene scene)
	{
		Vec3.StackArray8Vec3 stackArray8Vec = default(Vec3.StackArray8Vec3);
		bool visibilityExcludeParents = gameEntity.GetVisibilityExcludeParents();
		if (visibilityExcludeParents)
		{
			gameEntity.SetVisibilityExcludeParents(visible: false);
		}
		int num = 0;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			foreach (GameEntity wheel in wheels)
			{
				MatrixFrame frame2 = wheel.GetFrame();
				Vec3 vec = frame.TransformToParent(in frame2.origin);
				Vec3 vec2 = vec + frame.rotation.s * axleLength + (wheelDiameter * 0.5f + 0.5f) * frame.rotation.u;
				Vec3 vec3 = vec - frame.rotation.s * axleLength + (wheelDiameter * 0.5f + 0.5f) * frame.rotation.u;
				vec2.z = scene.GetGroundHeightAtPosition(vec2);
				vec3.z = scene.GetGroundHeightAtPosition(vec3);
				stackArray8Vec[num++] = vec2;
				stackArray8Vec[num++] = vec3;
			}
		}
		if (visibilityExcludeParents)
		{
			gameEntity.SetVisibilityExcludeParents(visible: true);
		}
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		Vec3 vec4 = default(Vec3);
		for (int i = 0; i < num; i++)
		{
			vec4 += stackArray8Vec[i];
		}
		vec4 /= (float)num;
		for (int j = 0; j < num; j++)
		{
			Vec3 vec5 = stackArray8Vec[j] - vec4;
			num2 += vec5.x * vec5.x;
			num3 += vec5.x * vec5.y;
			num4 += vec5.y * vec5.y;
			num5 += vec5.x * vec5.z;
			num6 += vec5.y * vec5.z;
		}
		float num7 = num2 * num4 - num3 * num3;
		float x = (num6 * num3 - num5 * num4) / num7;
		float y = (num3 * num5 - num2 * num6) / num7;
		MatrixFrame result = default(MatrixFrame);
		result.origin = vec4;
		result.rotation.u = new Vec3(x, y, 1f);
		result.rotation.u.Normalize();
		result.rotation.f = frame.rotation.f;
		result.rotation.f -= Vec3.DotProduct(result.rotation.f, result.rotation.u) * result.rotation.u;
		result.rotation.f.Normalize();
		result.rotation.s = Vec3.CrossProduct(result.rotation.f, result.rotation.u);
		result.rotation.s.Normalize();
		return result;
	}
}
