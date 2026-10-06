using NavalDLC.Storyline.MissionControllers;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews.Storyline;

public class Quest5SetPieceBattleInteriorConversationCameraView : MissionView
{
	private enum CameraChangeState
	{
		None,
		FadeOutBeforeConversation,
		ConversationInProgress,
		FadeOutAfterConversation,
		ChangeCameraBack,
		End
	}

	private Quest5SetPieceBattleMissionController _quest5SetPieceBattleMissionController;

	private float _fadeInDuration = 0.25f;

	private Camera _interiorConversationCamera;

	private CameraChangeState _state;

	private bool _sisterConversationStarted;

	public override void AfterStart()
	{
		base.AfterStart();
		_quest5SetPieceBattleMissionController = Mission.Current.GetMissionBehavior<Quest5SetPieceBattleMissionController>();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		switch (_state)
		{
		case CameraChangeState.None:
			if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ShipInteriorPhase && _quest5SetPieceBattleMissionController.Phase1InteriorCameraSisterEntity != null)
			{
				_state = CameraChangeState.FadeOutBeforeConversation;
			}
			break;
		case CameraChangeState.FadeOutBeforeConversation:
			ScreenFadeController.BeginFadeOutAndIn(0.25f, 0.25f, 0.25f);
			_state = CameraChangeState.ConversationInProgress;
			break;
		case CameraChangeState.ConversationInProgress:
			if (_quest5SetPieceBattleMissionController.Phase1InteriorCameraSisterEntity != null)
			{
				if (ScreenFadeController.IsFadedOut)
				{
					if (_interiorConversationCamera == null)
					{
						Vec3 dofParams = Vec3.Invalid;
						_interiorConversationCamera = Camera.CreateCamera();
						_quest5SetPieceBattleMissionController.Phase1InteriorCameraSisterEntity.GetCameraParamsFromCameraScript(_interiorConversationCamera, ref dofParams);
						_interiorConversationCamera.SetFovVertical(_interiorConversationCamera.GetFovVertical(), Screen.AspectRatio, _interiorConversationCamera.Near, _interiorConversationCamera.Far);
					}
					Agent.Main.AgentVisuals.SetVisible(value: false);
				}
				else if (ScreenFadeController.IsFadingIn)
				{
					_fadeInDuration -= dt;
				}
				if (_fadeInDuration <= 0f && !_sisterConversationStarted)
				{
					_sisterConversationStarted = true;
					MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
					missionBehavior.DisableStartConversation(isDisabled: false);
					missionBehavior.StartConversation(_quest5SetPieceBattleMissionController.SisterAgent, setActionsInstantly: false);
				}
				if (_interiorConversationCamera != null)
				{
					_interiorConversationCamera.Frame = _quest5SetPieceBattleMissionController.Phase1InteriorCameraSisterEntity.GetGlobalFrame();
					base.MissionScreen.CustomCamera = _interiorConversationCamera;
				}
			}
			else
			{
				_state = CameraChangeState.FadeOutAfterConversation;
			}
			break;
		case CameraChangeState.FadeOutAfterConversation:
			ScreenFadeController.BeginFadeOutAndIn(0.25f, 0.25f, 0.25f);
			_state = CameraChangeState.ChangeCameraBack;
			break;
		case CameraChangeState.ChangeCameraBack:
			if (ScreenFadeController.IsFadedOut)
			{
				Agent.Main.AgentVisuals.SetVisible(value: true);
				base.MissionScreen.CustomCamera = null;
				_state = CameraChangeState.End;
			}
			break;
		case CameraChangeState.End:
			break;
		}
	}
}
