using System;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Missions.ShipInput;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.SiegeWeapon;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection;

namespace NavalDLC.View.MissionViews;

public class MissionShipControlView : MissionBattleUIBaseView
{
	public enum CameraModes
	{
		Back,
		Shoulder,
		Front,
		NumPositions
	}

	protected SailInput SailControl;

	protected NavalShipsLogic NavalShipsLogic;

	private Vec3 _lastCameraOffset;

	private float _lastCameraFovMultiplier = 1f;

	private bool _wasOrderMenuOpenLastFrame;

	protected bool IsAimingWithRangedWeapon;

	private float _backCameraDistanceMultiplier = 1f;

	private float _lastForwardKeyPressTime;

	private float _lastBackwardKeyPressTime;

	private int _lastAccelerationAxisInput;

	public CameraModes ActiveCameraMode { get; protected set; }

	public ShipControllerMachine ControllerMachine { get; private set; }

	protected bool IsAimingWithRangedWeaponAndAllowed
	{
		get
		{
			if (IsAimingWithRangedWeapon)
			{
				return IsAimingWithRangedWeaponAllowed;
			}
			return false;
		}
	}

	protected bool IsAimingWithRangedWeaponAllowed
	{
		get
		{
			if (!base.Mission.IsOrderMenuOpen && !_wasOrderMenuOpenLastFrame && RangedSiegeWeapon != null && !RangedSiegeWeapon.IsDisabled)
			{
				return !RangedSiegeWeapon.IsDestroyed;
			}
			return false;
		}
	}

	protected bool IsDisplayingADialog
	{
		get
		{
			MissionScreen missionScreen = base.MissionScreen;
			if (missionScreen == null || !((IMissionScreen)missionScreen).GetDisplayDialog())
			{
				MissionScreen missionScreen2 = base.MissionScreen;
				if (missionScreen2 == null || !missionScreen2.IsRadialMenuActive)
				{
					return base.Mission?.IsOrderMenuOpen ?? false;
				}
			}
			return true;
		}
	}

	protected RangedSiegeWeapon RangedSiegeWeapon { get; private set; }

	protected RangedSiegeWeapon DirectlyControlledRangedSiegeWeapon { get; private set; }

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		NavalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
	}

	public override void OnPreMissionTick(float dt)
	{
		base.OnPreMissionTick(dt);
		HandleShipControls(dt);
		HandleShipCamera(dt);
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (!userAgent.IsMainAgent || !(usedObject is StandingPoint standingPoint))
		{
			return;
		}
		UsableMachine usableMachineFromPoint = GetUsableMachineFromPoint(standingPoint);
		MissionShip firstScriptOfType;
		if (usableMachineFromPoint is ShipControllerMachine shipControllerMachine)
		{
			ControllerMachine = shipControllerMachine;
			RangedSiegeWeapon firstScriptInFamilyDescending = shipControllerMachine.GameEntity.Root.GetFirstScriptInFamilyDescending<RangedSiegeWeapon>();
			if (firstScriptInFamilyDescending != null)
			{
				RangedSiegeWeapon = firstScriptInFamilyDescending;
			}
		}
		else if (usableMachineFromPoint is RangedSiegeWeapon { GameEntity: { Root: var root } } rangedSiegeWeapon && (firstScriptOfType = root.GetFirstScriptOfType<MissionShip>()) != null)
		{
			DirectlyControlledRangedSiegeWeapon = rangedSiegeWeapon;
			firstScriptOfType.OnSetRangedWeaponControlMode(value: true);
		}
	}

	public override void OnObjectStoppedBeingUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (userAgent.IsMainAgent && usedObject is StandingPoint standingPoint)
		{
			UsableMachine usableMachineFromPoint = GetUsableMachineFromPoint(standingPoint);
			MissionShip firstScriptOfType;
			if (usableMachineFromPoint is ShipControllerMachine)
			{
				RangedSiegeWeapon?.SetPlayerForceUse(value: false);
				ControllerMachine = null;
				RangedSiegeWeapon = null;
				base.Mission.SetListenerAndAttenuationPosBlendFactor(0f);
			}
			else if (usableMachineFromPoint is RangedSiegeWeapon { GameEntity: { Root: var root } } && (firstScriptOfType = root.GetFirstScriptOfType<MissionShip>()) != null)
			{
				DirectlyControlledRangedSiegeWeapon = null;
				firstScriptOfType.OnSetRangedWeaponControlMode(value: false);
			}
		}
	}

	private static UsableMachine GetUsableMachineFromPoint(StandingPoint standingPoint)
	{
		WeakGameEntity weakGameEntity = standingPoint.GameEntity;
		while (weakGameEntity.IsValid && !weakGameEntity.HasScriptOfType<UsableMachine>())
		{
			weakGameEntity = weakGameEntity.Parent;
		}
		if (weakGameEntity.IsValid)
		{
			UsableMachine firstScriptOfType = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType != null)
			{
				return firstScriptOfType;
			}
		}
		return null;
	}

	private void TickRowerInput(Vec2 inputVec, out RowerLongitudinalInput longitudinalRowerControl, out RowerLongitudinalInput longitudinalControlDoubleTap, out RowerLateralInput lateralRowerControl)
	{
		int num = 0;
		int num2 = 0;
		if (inputVec.LengthSquared > 0f)
		{
			inputVec.Normalize();
			float num3 = inputVec.RotationInRadians.ToDegrees();
			bool flag = false;
			if (num3 < 0f)
			{
				flag = true;
				num3 = 0f - num3;
			}
			if (num3 <= 22.5f)
			{
				num = 1;
			}
			else if (num3 <= 67.5f)
			{
				num = 1;
				num2 = 1;
			}
			else if (num3 <= 112.5f)
			{
				num2 = 1;
			}
			else if (num3 < 157.5f)
			{
				num = -1;
				num2 = 1;
			}
			else
			{
				num = -1;
			}
			if (flag)
			{
				num2 = -num2;
			}
		}
		bool flag2 = num == 1 && _lastAccelerationAxisInput == 1;
		bool flag3 = num == -1 && _lastAccelerationAxisInput == -1;
		_lastAccelerationAxisInput = num;
		bool flag4 = false;
		bool flag5 = false;
		longitudinalRowerControl = (RowerLongitudinalInput)num;
		longitudinalControlDoubleTap = RowerLongitudinalInput.None;
		if (num == 1)
		{
			if (flag2 && _lastForwardKeyPressTime + 0.3f > Time.ApplicationTime)
			{
				longitudinalControlDoubleTap = RowerLongitudinalInput.Forward;
				flag4 = true;
			}
		}
		else if (num == -1 && flag3 && _lastBackwardKeyPressTime + 0.3f > Time.ApplicationTime)
		{
			longitudinalControlDoubleTap = RowerLongitudinalInput.Backward;
			flag5 = true;
		}
		lateralRowerControl = (RowerLateralInput)num2;
		if (!flag4 && flag2)
		{
			_lastForwardKeyPressTime = Time.ApplicationTime;
		}
		if (!flag5 && flag3)
		{
			_lastBackwardKeyPressTime = Time.ApplicationTime;
		}
	}

	private float TickRudderInput(Vec2 inputVec)
	{
		return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Abs(inputVec.x) * 1.4f, 1f) * (float)TaleWorlds.Library.MathF.Sign(inputVec.x);
	}

	private void HandleShipControls(float dt)
	{
		_wasOrderMenuOpenLastFrame = base.Mission.IsOrderMenuOpen;
		MissionShip missionShip = NavalShipsLogic?.PlayerControlledShip;
		if (missionShip == null || !missionShip.IsPlayerControlled)
		{
			return;
		}
		PlayerShipController playerController = missionShip.PlayerController;
		RowerLongitudinalInput longitudinalRowerControl = RowerLongitudinalInput.None;
		RowerLongitudinalInput longitudinalControlDoubleTap = RowerLongitudinalInput.None;
		RowerLateralInput lateralRowerControl = RowerLateralInput.None;
		float rudderLateral = 0f;
		if (!base.MissionScreen.IsCheatGhostMode)
		{
			float gameKeyAxis = base.Input.GetGameKeyAxis("MovementAxisY");
			float gameKeyAxis2 = base.Input.GetGameKeyAxis("MovementAxisX");
			Vec2 inputVec = new Vec2(gameKeyAxis2, gameKeyAxis);
			if (TaleWorlds.Library.MathF.Abs(inputVec.x) <= 0.2f)
			{
				inputVec.x = 0f;
			}
			if (TaleWorlds.Library.MathF.Abs(inputVec.y) <= 0.2f)
			{
				inputVec.y = 0f;
			}
			TickRowerInput(inputVec, out longitudinalRowerControl, out longitudinalControlDoubleTap, out lateralRowerControl);
			rudderLateral = TickRudderInput(inputVec);
		}
		playerController.SetInput(new ShipInputRecord(lateralRowerControl, longitudinalRowerControl, longitudinalControlDoubleTap, rudderLateral, SailControl));
	}

	public void SetSailInput(SailInput sailInput)
	{
		SailControl = sailInput;
	}

	public void SetActiveCameraMode(CameraModes mode)
	{
		ActiveCameraMode = mode;
	}

	private void HandleShipCamera(float dt)
	{
		if (ControllerMachine != null)
		{
			if (RangedSiegeWeapon != null)
			{
				RangedSiegeWeaponView component = RangedSiegeWeapon.GetComponent<RangedSiegeWeaponView>();
				if (component == null)
				{
					component = new BallistaView();
					component.Initialize(RangedSiegeWeapon, base.MissionScreen);
					RangedSiegeWeapon.AddComponent(component);
				}
				RangedSiegeWeapon.SetPlayerForceUse(IsAimingWithRangedWeaponAndAllowed);
			}
			Agent pilotAgent = ControllerMachine.PilotAgent;
			Vec3 v;
			float num;
			Vec3 v2;
			switch (ActiveCameraMode)
			{
			case CameraModes.Back:
				v = ControllerMachine.BackCameraOffset * 0.5f;
				num = ControllerMachine.BackCameraFovMultiplier;
				if (base.Mission.InputManager.IsGameKeyDown(28))
				{
					_backCameraDistanceMultiplier -= 0.5f * dt;
				}
				if (base.Mission.InputManager.IsGameKeyDown(29))
				{
					_backCameraDistanceMultiplier += 0.5f * dt;
				}
				_backCameraDistanceMultiplier = MBMath.ClampFloat(_backCameraDistanceMultiplier, 0.2f, 3f);
				v2 = new Vec3(ControllerMachine.BackCameraTargetLocalPosition.AsVec2, ControllerMachine.BackCameraTargetLocalPosition.z * _backCameraDistanceMultiplier);
				base.Mission.SetListenerAndAttenuationPosBlendFactor(0.33f);
				break;
			case CameraModes.Front:
				v = ControllerMachine.FrontCameraOffset;
				v2 = ControllerMachine.FrontCameraTargetLocalPosition;
				num = ControllerMachine.FrontCameraFovMultiplier;
				base.Mission.SetListenerAndAttenuationPosBlendFactor(1f);
				break;
			default:
				v = ControllerMachine.ShoulderCameraOffset;
				v2 = ControllerMachine.ShoulderCameraTargetLocalPosition;
				num = ControllerMachine.ShoulderCameraFovMultiplier;
				base.Mission.SetListenerAndAttenuationPosBlendFactor(0f);
				break;
			}
			bool flag = (!_lastCameraOffset.NearlyEquals(in v, 0.001f) || TaleWorlds.Library.MathF.Abs(_lastCameraFovMultiplier - num) > 0.001f) && !IsAimingWithRangedWeaponAndAllowed;
			_lastCameraOffset = (flag ? MBMath.Lerp(_lastCameraOffset, v, dt * 5f, 0.001f) : v);
			_lastCameraFovMultiplier = (flag ? MBMath.Lerp(_lastCameraFovMultiplier, num, dt * 5f, 0.001f) : num);
			WeakGameEntity root = ControllerMachine.GameEntity.Root;
			float num2;
			Vec3 vec;
			if (pilotAgent != null)
			{
				num2 = MBMath.WrapAngle(base.MissionScreen.CameraBearing - pilotAgent.MovementDirectionAsAngle);
				vec = pilotAgent.Position;
			}
			else
			{
				num2 = MBMath.WrapAngle(base.MissionScreen.CameraBearing);
				vec = root.GlobalPosition;
			}
			Vec3 vec2 = (v2.IsNonZero ? (vec - ControllerMachine.GameEntity.GetGlobalFrame().TransformToParent(in v2) - ((ActiveCameraMode == CameraModes.Shoulder) ? (ControllerMachine.AttachedShip.GameEntity.GetGlobalFrame().rotation.s.NormalizedCopy() * TaleWorlds.Library.MathF.Sin(num2) * ControllerMachine.ShoulderCameraDistance) : ((ActiveCameraMode == CameraModes.Front) ? (ControllerMachine.AttachedShip.GameEntity.GetGlobalFrame().rotation.f.NormalizedCopy() * TaleWorlds.Library.MathF.Cos(Math.Min(TaleWorlds.Library.MathF.Abs(num2) * 2.5f, System.MathF.PI / 2f)) * 8f) : Vec3.Zero))) : Vec3.Zero);
			Mission.Current.SetCustomCameraFixedDistance((ActiveCameraMode == CameraModes.Front) ? ControllerMachine.FrontCameraDistance : ((ActiveCameraMode == CameraModes.Back) ? (v.Length * _backCameraDistanceMultiplier) : float.MinValue));
			Mission.Current.SetCustomCameraTargetLocalOffset(MBMath.Lerp(Mission.Current.CustomCameraTargetLocalOffset, -vec2, dt * 10f, 0.001f));
			if (ActiveCameraMode == CameraModes.Shoulder)
			{
				if (!flag)
				{
					Mission.Current.SetIgnoredEntityForCamera(null);
				}
			}
			else if (Mission.Current.IgnoredEntityForCamera != root)
			{
				Mission.Current.SetIgnoredEntityForCamera(GameEntity.CreateFromWeakEntity(root));
			}
			Mission.Current.SetCustomCameraIgnoreCollision(ActiveCameraMode == CameraModes.Front);
		}
		else
		{
			_lastCameraOffset = MBMath.Lerp(_lastCameraOffset, Vec3.Zero, dt * 5f, 0.001f);
			_lastCameraFovMultiplier = MBMath.Lerp(_lastCameraFovMultiplier, 1f, dt * 5f, 0.001f);
			Mission.Current.SetCustomCameraFixedDistance(float.MinValue);
			Mission.Current.SetCustomCameraTargetLocalOffset(MBMath.Lerp(Mission.Current.CustomCameraTargetLocalOffset, Vec3.Zero, dt * 5f, 0.001f));
			if (!_lastCameraOffset.IsNonZero)
			{
				Mission.Current.SetIgnoredEntityForCamera(null);
			}
			Mission.Current.SetCustomCameraIgnoreCollision(ignoreCollision: false);
		}
		Mission.Current.SetCustomCameraLocalOffset(_lastCameraOffset);
		Mission.Current.SetCustomCameraFovMultiplier(_lastCameraFovMultiplier);
	}

	protected override void OnCreateView()
	{
	}

	protected override void OnDestroyView()
	{
	}

	protected override void OnSuspendView()
	{
	}

	protected override void OnResumeView()
	{
	}
}
