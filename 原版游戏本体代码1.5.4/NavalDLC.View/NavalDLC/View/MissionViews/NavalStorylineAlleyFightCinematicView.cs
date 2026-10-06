using NavalDLC.Storyline;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class NavalStorylineAlleyFightCinematicView : MissionView
{
	private bool _isInitialized;

	private bool _isCinematicPartActive;

	private NavalStorylineAlleyFightCinematicController _cinematicLogicController;

	private Camera _camera;

	private MatrixFrame _cameraFrame = MatrixFrame.Identity;

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!_isInitialized)
		{
			InitializeView();
		}
		else if (!Game.Current.GameStateManager.ActiveStateDisabledByUser)
		{
			UpdateCamera(dt);
		}
	}

	public override bool IsPhotoModeAllowed()
	{
		return !_isCinematicPartActive;
	}

	private void GetCameraFrame(Vec3 position, Vec3 direction, out MatrixFrame cameraFrame)
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
			Debug.FailedAssert("Combat camera is null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\MissionViews\\NavalStorylineAlleyFightCinematicView.cs", "SetupCamera", 62);
		}
		_cinematicLogicController.GetCameraFrame(out var position, out var forward);
		GetCameraFrame(position, forward, out _cameraFrame);
		_camera.Frame = _cameraFrame;
		base.MissionScreen.CustomCamera = _camera;
	}

	private void UpdateCamera(float dt)
	{
		if (_camera != null)
		{
			_cinematicLogicController.GetCameraFrame(out var position, out var forward);
			GetCameraFrame(position, forward, out _cameraFrame);
			_camera.Frame = _cameraFrame;
		}
	}

	private void ReleaseCamera()
	{
		base.MissionScreen.CustomCamera = null;
		_camera.ReleaseCamera();
	}

	private void OnCinematicStateChanged(NavalStorylineAlleyFightCinematicController.NavalAlleyFightCinematicState state)
	{
		if (_isInitialized)
		{
			float fadeDuration = _cinematicLogicController.GetFadeDuration();
			float fadeDuration2 = _cinematicLogicController.GetFadeDuration();
			switch (state)
			{
			case NavalStorylineAlleyFightCinematicController.NavalAlleyFightCinematicState.InitialFadeOut:
				base.Mission.GetMissionBehavior<MissionMainAgentController>().Disable();
				_isCinematicPartActive = true;
				ScreenFadeController.BeginFadeOutAndIn(fadeDuration, fadeDuration2, fadeDuration);
				break;
			case NavalStorylineAlleyFightCinematicController.NavalAlleyFightCinematicState.InitialFadeIn:
				SetupCamera();
				break;
			case NavalStorylineAlleyFightCinematicController.NavalAlleyFightCinematicState.Completed:
				base.Mission.GetMissionBehavior<MissionMainAgentController>().Enable();
				_isCinematicPartActive = false;
				ReleaseCamera();
				break;
			}
		}
	}

	private void OnFightEnded(float fadeInDuration, float blackDuration, float fadeOutDuration)
	{
		ScreenFadeController.BeginFadeOutAndIn(fadeInDuration, blackDuration, fadeOutDuration);
	}

	private void OnConversationSetup(Vec3 direction)
	{
		base.MissionScreen.CameraBearing = direction.RotationZ;
	}

	private void InitializeView()
	{
		_cinematicLogicController = base.Mission.GetMissionBehavior<NavalStorylineAlleyFightCinematicController>();
		_isInitialized = _cinematicLogicController != null;
		if (_cinematicLogicController != null)
		{
			_cinematicLogicController.OnCinematicStateChanged += OnCinematicStateChanged;
			_cinematicLogicController.OnFightEndedEvent += OnFightEnded;
			_cinematicLogicController.OnConversationSetupEvent += OnConversationSetup;
		}
		MissionAgentLabelView missionBehavior = base.Mission.GetMissionBehavior<MissionAgentLabelView>();
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			missionBehavior.SuspendView();
		}
	}
}
