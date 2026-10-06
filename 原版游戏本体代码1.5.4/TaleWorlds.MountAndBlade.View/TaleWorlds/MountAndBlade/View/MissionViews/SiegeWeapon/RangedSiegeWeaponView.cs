using System;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;

namespace TaleWorlds.MountAndBlade.View.MissionViews.SiegeWeapon;

public class RangedSiegeWeaponView : UsableMissionObjectComponent
{
	private float _cameraYaw;

	private float _cameraPitch;

	private float _cameraRoll;

	private float _cameraInitialYaw;

	private float _cameraInitialPitch;

	private Vec3 _cameraPositionOffset;

	private bool _isInWeaponCameraMode;

	protected bool UsesMouseForAiming;

	public RangedSiegeWeapon RangedSiegeWeapon { get; private set; }

	public MissionScreen MissionScreen { get; private set; }

	public Camera Camera { get; private set; }

	public GameEntity CameraHolder => RangedSiegeWeapon.CameraHolder;

	public Agent PilotAgent => RangedSiegeWeapon.PilotAgent;

	public void Initialize(RangedSiegeWeapon rangedSiegeWeapon, MissionScreen missionScreen)
	{
		RangedSiegeWeapon = rangedSiegeWeapon;
		MissionScreen = missionScreen;
	}

	protected override void OnAdded(Scene scene)
	{
		base.OnAdded(scene);
		if (CameraHolder != null)
		{
			CreateCamera();
		}
	}

	protected override void OnMissionReset()
	{
		base.OnMissionReset();
		if (CameraHolder != null)
		{
			_cameraYaw = _cameraInitialYaw;
			_cameraPitch = _cameraInitialPitch;
			ApplyCameraRotation();
			_isInWeaponCameraMode = false;
			ResetCamera();
		}
	}

	public override bool IsOnTickRequired()
	{
		return true;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!GameNetwork.IsReplay)
		{
			HandleUserInput(dt);
		}
	}

	protected virtual void HandleUserInput(float dt)
	{
		if (CameraHolder != null && ((PilotAgent != null && PilotAgent.IsMainAgent) || RangedSiegeWeapon.PlayerForceUse))
		{
			if (!_isInWeaponCameraMode)
			{
				_isInWeaponCameraMode = true;
				StartUsingWeaponCamera();
			}
			if (RangedSiegeWeapon.PlayerForceUse)
			{
				HandleUserCameraRotation(dt);
			}
		}
		if (_isInWeaponCameraMode && (PilotAgent == null || !PilotAgent.IsMainAgent) && !RangedSiegeWeapon.PlayerForceUse)
		{
			_isInWeaponCameraMode = false;
			ResetCamera();
		}
		HandleUserAiming(dt);
	}

	private void CreateCamera()
	{
		Camera = Camera.CreateCamera();
		float aspectRatio = Screen.AspectRatio;
		Camera.SetFovVertical(System.MathF.PI / 3f, aspectRatio, 0.1f, 12500f);
		Camera.Entity = CameraHolder;
		MatrixFrame frame = CameraHolder.GetFrame();
		Vec3 eulerAngles = frame.rotation.GetEulerAngles();
		_cameraYaw = eulerAngles.z;
		_cameraPitch = eulerAngles.x;
		_cameraRoll = eulerAngles.y;
		_cameraPositionOffset = frame.origin;
		_cameraPositionOffset.RotateAboutZ(0f - _cameraYaw);
		_cameraPositionOffset.RotateAboutX(0f - _cameraPitch);
		_cameraPositionOffset.RotateAboutY(0f - _cameraRoll);
		_cameraInitialYaw = _cameraYaw;
		_cameraInitialPitch = _cameraPitch;
	}

	protected virtual void StartUsingWeaponCamera()
	{
		MissionScreen.CustomCamera = Camera;
		Agent.Main.IsLookDirectionLocked = true;
	}

	private void ResetCamera()
	{
		if (MissionScreen.CustomCamera == Camera)
		{
			MissionScreen.CustomCamera = null;
			if (Agent.Main != null)
			{
				Agent.Main.IsLookDirectionLocked = false;
				MissionScreen.SetExtraCameraParameters(newForceCanZoom: false, 0f);
			}
		}
	}

	protected virtual void HandleUserCameraRotation(float dt)
	{
		float cameraYaw = _cameraYaw;
		float cameraPitch = _cameraPitch;
		if (MissionScreen.SceneLayer.Input.IsGameKeyDown(10))
		{
			_cameraYaw = _cameraInitialYaw;
			_cameraPitch = _cameraInitialPitch;
		}
		_cameraYaw += MissionScreen.SceneLayer.Input.GetMouseMoveX() * dt * 0.2f;
		_cameraPitch += MissionScreen.SceneLayer.Input.GetMouseMoveY() * dt * 0.2f;
		_cameraYaw = MBMath.ClampFloat(_cameraYaw, System.MathF.PI / 2f, 4.712389f);
		_cameraPitch = MBMath.ClampFloat(_cameraPitch, System.MathF.PI / 3f, System.MathF.PI * 5f / 9f);
		if (cameraPitch != _cameraPitch || cameraYaw != _cameraYaw)
		{
			ApplyCameraRotation();
		}
	}

	private void ApplyCameraRotation()
	{
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation.RotateAboutUp(_cameraYaw);
		frame.rotation.RotateAboutSide(_cameraPitch);
		frame.rotation.RotateAboutForward(_cameraRoll);
		frame.Strafe(_cameraPositionOffset.x);
		frame.Advance(_cameraPositionOffset.y);
		frame.Elevate(_cameraPositionOffset.z);
		CameraHolder.SetFrame(ref frame);
	}

	private void HandleUserAiming(float dt)
	{
		bool flag = false;
		float num = 0f;
		float num2 = 0f;
		if (PilotAgent != null && (PilotAgent.IsMainAgent || RangedSiegeWeapon.PlayerForceUse))
		{
			if (UsesMouseForAiming)
			{
				InputContext input = MissionScreen.SceneLayer.Input;
				float num3 = dt * 1666.6666f;
				float num4 = input.GetMouseMoveX() + num3 * input.GetGameKeyAxis("CameraAxisX");
				float num5 = input.GetMouseMoveY() + (0f - num3) * input.GetGameKeyAxis("CameraAxisY");
				if (NativeConfig.InvertMouse)
				{
					num5 *= -1f;
				}
				Vec2 vec = new Vec2(0f - num4, 0f - num5);
				if (vec.IsNonZero())
				{
					float x = vec.Normalize();
					x = TaleWorlds.Library.MathF.Min(5f, TaleWorlds.Library.MathF.Pow(x, 1.5f) * 0.025f);
					vec *= x;
					num = vec.x;
					num2 = vec.y;
				}
			}
			else
			{
				if (MissionScreen.SceneLayer.Input.IsGameKeyDown(2))
				{
					num = 1f;
				}
				else if (MissionScreen.SceneLayer.Input.IsGameKeyDown(3))
				{
					num = -1f;
				}
				if (MissionScreen.SceneLayer.Input.IsGameKeyDown(0))
				{
					num2 = 1f;
				}
				else if (MissionScreen.SceneLayer.Input.IsGameKeyDown(1))
				{
					num2 = -1f;
				}
			}
			if (num != 0f)
			{
				flag = true;
			}
			if (num2 != 0f)
			{
				flag = true;
			}
		}
		if (flag)
		{
			RangedSiegeWeapon.GiveInput(num, num2);
		}
	}

	protected override void OnMissionObjectDisabled()
	{
		ResetCamera();
	}
}
