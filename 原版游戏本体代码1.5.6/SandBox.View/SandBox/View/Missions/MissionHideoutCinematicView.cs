using SandBox.Missions.MissionLogics.Hideout;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionHideoutCinematicView : MissionView
{
	private bool _isInitialized;

	private HideoutCinematicController _cinematicLogicController;

	private HideoutCinematicController.HideoutCinematicState _currentState;

	private HideoutCinematicController.HideoutCinematicState _nextState;

	private Camera _camera;

	private MatrixFrame _cameraFrame = MatrixFrame.Identity;

	private readonly Vec3 _cameraOffset = new Vec3(0.3f, 0.3f, 1.2f);

	private Vec3 _cameraMoveDir = Vec3.Forward;

	private float _cameraSpeed;

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!_isInitialized)
		{
			InitializeView();
		}
		else if (!Game.Current.GameStateManager.ActiveStateDisabledByUser && (_currentState == HideoutCinematicController.HideoutCinematicState.Cinematic || _nextState == HideoutCinematicController.HideoutCinematicState.Cinematic))
		{
			UpdateCamera(dt);
		}
	}

	private void SetCameraFrame(Vec3 position, Vec3 direction, out MatrixFrame cameraFrame)
	{
		cameraFrame.origin = position;
		cameraFrame.rotation.s = Vec3.Side;
		cameraFrame.rotation.f = Vec3.Up;
		cameraFrame.rotation.u = -direction;
		cameraFrame.rotation.Orthonormalize();
	}

	private void SetupCamera()
	{
		_camera = Camera.CreateCamera();
		Camera combatCamera = base.MissionScreen.CombatCamera;
		if (combatCamera != null)
		{
			_camera.FillParametersFrom(combatCamera);
		}
		else
		{
			Debug.FailedAssert("Combat camera is null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Missions\\MissionHideoutCinematicView.cs", "SetupCamera", 66);
		}
		_cinematicLogicController.GetBossStandingEyePosition(out var eyePosition);
		_cinematicLogicController.GetPlayerStandingEyePosition(out var eyePosition2);
		Vec3 vec = (eyePosition - eyePosition2).NormalizedCopy();
		_cinematicLogicController.GetScenePrefabParameters(out var innerRadius, out var outerRadius, out var walkDistance);
		float num = innerRadius + outerRadius + 1.5f * walkDistance;
		_cameraSpeed = num / MathF.Max(_cinematicLogicController.CinematicDuration, 0.1f);
		_cameraMoveDir = -vec;
		SetCameraFrame(eyePosition, vec, out _cameraFrame);
		Vec3 vec2 = _cameraFrame.origin + _cameraOffset.x * _cameraFrame.rotation.s + _cameraOffset.y * _cameraFrame.rotation.f + _cameraOffset.z * _cameraFrame.rotation.u;
		Vec3 direction = (eyePosition - vec2).NormalizedCopy();
		SetCameraFrame(vec2, direction, out _cameraFrame);
		_camera.Frame = _cameraFrame;
		base.MissionScreen.CustomCamera = _camera;
	}

	private void UpdateCamera(float dt)
	{
		Vec3 vec = _cameraFrame.origin + _cameraMoveDir * _cameraSpeed * dt;
		_cinematicLogicController.GetBossStandingEyePosition(out var eyePosition);
		Vec3 direction = (eyePosition - vec).NormalizedCopy();
		SetCameraFrame(vec, direction, out _cameraFrame);
		_camera.Frame = _cameraFrame;
	}

	private void ReleaseCamera()
	{
		base.MissionScreen.UpdateFreeCamera(base.MissionScreen.CustomCamera.Frame);
		base.MissionScreen.CustomCamera = null;
		_camera.ReleaseCamera();
	}

	private void OnCinematicStateChanged(HideoutCinematicController.HideoutCinematicState state)
	{
		if (_isInitialized)
		{
			_currentState = state;
			if (_currentState == HideoutCinematicController.HideoutCinematicState.PreCinematic)
			{
				SetupCamera();
			}
			else if (_currentState == HideoutCinematicController.HideoutCinematicState.PostCinematic)
			{
				ReleaseCamera();
			}
		}
	}

	private void OnCinematicTransition(HideoutCinematicController.HideoutCinematicState nextState, float duration)
	{
		if (_isInitialized)
		{
			switch (nextState)
			{
			case HideoutCinematicController.HideoutCinematicState.InitialFadeOut:
			case HideoutCinematicController.HideoutCinematicState.PostCinematic:
				ScreenFadeController.BeginFadeOut(duration);
				break;
			case HideoutCinematicController.HideoutCinematicState.Cinematic:
			case HideoutCinematicController.HideoutCinematicState.Completed:
				ScreenFadeController.BeginFadeIn(duration);
				break;
			}
			_nextState = nextState;
		}
	}

	private void InitializeView()
	{
		_cinematicLogicController = base.Mission.GetMissionBehavior<HideoutCinematicController>();
		_isInitialized = _cinematicLogicController != null;
		if (_cinematicLogicController != null)
		{
			_cinematicLogicController.OnCinematicStateChanged += OnCinematicStateChanged;
			_cinematicLogicController.OnCinematicTransition += OnCinematicTransition;
		}
	}
}
