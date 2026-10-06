using System;
using NavalDLC.Missions.ShipInput;
using NavalDLC.Storyline.MissionControllers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class BlockedEstuaryView : MissionView
{
	private const string CameraSpawnId = "sp_camera";

	private const string CameraShipSpawnId = "sp_camera_ship";

	private BlockedEstuaryMissionController _controller;

	private Camera _camera;

	private bool _isInitialized;

	private GameEntity _cameraFrame;

	private GameEntity _shipCameraFrame;

	private MissionMainAgentController _mainAgentController;

	private bool _checkPointReached;

	private MatrixFrame _cameraTargetFrame;

	private bool _useShipCamera;

	private float _switchTimer;

	private float _transitionSpeed = 2f;

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!_isInitialized)
		{
			InitializeView();
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (_isInitialized && !Game.Current.GameStateManager.ActiveStateDisabledByUser && _camera != null)
		{
			UpdateCamera(dt);
			if (!_cameraTargetFrame.IsIdentity && !_cameraTargetFrame.IsZero)
			{
				_camera.Frame = MatrixFrame.Lerp(_camera.Frame, in _cameraTargetFrame, dt * _transitionSpeed);
			}
		}
	}

	public void FadeToBlack(float fadeOutTime, float blackTime, float fadeInTime)
	{
		ScreenFadeController.BeginFadeOutAndIn(fadeOutTime, blackTime, fadeInTime);
		base.MissionScreen.CameraBearing = Agent.Main.LookDirection.RotationZ;
	}

	private void UpdateCamera(float dt)
	{
		if (_controller.CollisionImminent)
		{
			if (_switchTimer <= 2f)
			{
				_switchTimer += dt;
			}
			else
			{
				_useShipCamera = false;
			}
		}
		if (_useShipCamera)
		{
			_transitionSpeed = 4f;
			SetCameraFrame(_shipCameraFrame.GlobalPosition, -_shipCameraFrame.GetGlobalFrame().rotation.u * 2f);
		}
		else if (_controller.CollisionImminent)
		{
			_transitionSpeed = 0.3f;
			SetCameraFrame(_cameraFrame.GlobalPosition, -_cameraFrame.GetGlobalFrame().rotation.u);
		}
	}

	private void SetupCamera()
	{
		_camera = Camera.CreateCamera();
		Vec3 dofParams = Vec3.Zero;
		_cameraFrame.GetCameraParamsFromCameraScript(_camera, ref dofParams);
		base.MissionScreen.CustomCamera = _camera;
		_camera.Frame = base.MissionScreen.CombatCamera.Frame;
		_switchTimer = 0f;
		_useShipCamera = true;
	}

	private void SetCameraFrame(Vec3 position, Vec3 direction)
	{
		MatrixFrame frame = _camera.Frame;
		frame.origin = position;
		frame.rotation.s = Vec3.Side;
		frame.rotation.f = Vec3.Up;
		frame.rotation.u = -direction;
		frame.rotation.Orthonormalize();
		_cameraTargetFrame = frame;
	}

	private void InitializeView()
	{
		_controller = base.Mission.GetMissionBehavior<BlockedEstuaryMissionController>();
		_mainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		BlockedEstuaryMissionController controller = _controller;
		controller.OnCheckPointReachedEvent = (Action)Delegate.Combine(controller.OnCheckPointReachedEvent, new Action(OnCheckPointReached));
		BlockedEstuaryMissionController controller2 = _controller;
		controller2.OnLastExitZoneReachedEvent = (Action)Delegate.Combine(controller2.OnLastExitZoneReachedEvent, new Action(LastExitZoneReached));
		BlockedEstuaryMissionController controller3 = _controller;
		controller3.OnPhaseEnd = (Action)Delegate.Combine(controller3.OnPhaseEnd, new Action(OnPhaseEnd));
		_cameraFrame = GetCameraEntity();
		_shipCameraFrame = GetShipCameraEntity();
		_isInitialized = true;
		MissionShipControlView missionBehavior = base.Mission.GetMissionBehavior<MissionShipControlView>();
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			if (_controller.CurrentPhase != BlockedEstuaryMissionController.BattlePhase.Phase3)
			{
				missionBehavior.SetSailInput(SailInput.Full);
			}
			missionBehavior.SetActiveCameraMode(MissionShipControlView.CameraModes.Back);
		}
	}

	private void OnPhaseEnd()
	{
		if (_camera != null)
		{
			ReleaseCamera();
		}
		FadeToBlack(0.1f, 0.5f, 0.5f);
	}

	private void LastExitZoneReached()
	{
		_mainAgentController.Disable();
		SetupCamera();
	}

	private void OnPlayerDismounted()
	{
		FadeToBlack(0.1f, 0.5f, 0.5f);
	}

	private void OnCheckPointReached()
	{
		if (Agent.Main.HasMount)
		{
			_mainAgentController.Disable();
		}
		_checkPointReached = true;
	}

	public override void OnAgentDismount(Agent agent)
	{
		if (agent.IsMainAgent && _checkPointReached)
		{
			_mainAgentController.Enable();
			OnPlayerDismounted();
		}
	}

	public override void OnMissionScreenFinalize()
	{
		BlockedEstuaryMissionController controller = _controller;
		controller.OnCheckPointReachedEvent = (Action)Delegate.Remove(controller.OnCheckPointReachedEvent, new Action(OnCheckPointReached));
		BlockedEstuaryMissionController controller2 = _controller;
		controller2.OnLastExitZoneReachedEvent = (Action)Delegate.Remove(controller2.OnLastExitZoneReachedEvent, new Action(LastExitZoneReached));
		BlockedEstuaryMissionController controller3 = _controller;
		controller3.OnPhaseEnd = (Action)Delegate.Remove(controller3.OnPhaseEnd, new Action(OnPhaseEnd));
		base.OnMissionScreenFinalize();
	}

	private void ReleaseCamera()
	{
		_mainAgentController.Enable();
		base.MissionScreen.UpdateFreeCamera(base.MissionScreen.CustomCamera.Frame);
		base.MissionScreen.CustomCamera = null;
		_camera.ReleaseCamera();
		_camera = null;
	}

	private GameEntity GetCameraEntity()
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_camera");
		if (gameEntity != null)
		{
			return gameEntity;
		}
		Debug.FailedAssert("Cant find CameraEntity", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\MissionViews\\BlockedEstuaryView.cs", "GetCameraEntity", 217);
		return null;
	}

	private GameEntity GetShipCameraEntity()
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_camera_ship");
		if (gameEntity != null)
		{
			return gameEntity;
		}
		Debug.FailedAssert("Cant find ShipCameraEntity", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\MissionViews\\BlockedEstuaryView.cs", "GetShipCameraEntity", 229);
		return null;
	}
}
