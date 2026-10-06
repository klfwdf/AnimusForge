using System.Collections.Generic;
using NavalDLC.Storyline.MissionControllers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews.Storyline;

public class Quest5SetPieceBattleMissionView : MissionView
{
	public enum Quest5SetPieceBattleMissionViewState
	{
		None,
		FadeOut,
		Initialize,
		FadeIn,
		End
	}

	private enum ApproachPlayerShipLocationCheckState
	{
		None,
		CheckDistance,
		FadeOut,
		TeleportPlayerShip,
		End
	}

	private enum AllowedSwimRadiusCheckState
	{
		None,
		CheckDistance,
		FadeOut,
		TeleportPlayer,
		End
	}

	private enum EscapeShipStuckCheckState
	{
		None,
		CheckForStuck,
		FadeOut,
		TeleportEscapeShip,
		End
	}

	private enum PurigCutsceneCameraChangeState
	{
		None,
		WaitingForCountDown,
		FadeOut,
		ChangeBackToDefaultCamera,
		End
	}

	private const string PurigShipCutsceneCamTag = "purig_ship_cutscene_cam_tag";

	private const string BarrelTag = "dromon_javelin_barrel";

	private TextObject _restrictionNotificationText = new TextObject("{=GHuQ4xKj}The realm's borders hold firm. You are returned.");

	private Quest5SetPieceBattleMissionViewState _state;

	private Quest5SetPieceBattleMissionController _quest5SetPieceBattleMissionController;

	private ApproachPlayerShipLocationCheckState _approachPlayerShipLocationCheckState;

	private AllowedSwimRadiusCheckState _allowedSwimRadiusCheckState;

	private EscapeShipStuckCheckState _escapeShipStuckCheckState;

	private PurigCutsceneCameraChangeState _purigCutsceneCameraChangeState;

	private MissionTimer _purigCutsceneCameraChangeTimer;

	private MissionTimer _missionEndTimer;

	private bool _isPlayerShipRotationCorrectedAtTheStartOfTheMission;

	private bool _isMainAgentRotatedBeforeBossFight;

	private bool _isBarrelListInitialized;

	public Camera PurigShipCutsceneCamera;

	public Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState LastHitCheckpoint;

	private List<WeakGameEntity> _barrelEntities = new List<WeakGameEntity>();

	public Quest5SetPieceBattleMissionView()
	{
		_state = Quest5SetPieceBattleMissionViewState.None;
		_approachPlayerShipLocationCheckState = ApproachPlayerShipLocationCheckState.None;
		_allowedSwimRadiusCheckState = AllowedSwimRadiusCheckState.None;
		_escapeShipStuckCheckState = EscapeShipStuckCheckState.None;
		_purigCutsceneCameraChangeState = PurigCutsceneCameraChangeState.None;
		_purigCutsceneCameraChangeTimer = null;
	}

	public virtual void PassMissionStateOnTick(Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState currentState)
	{
	}

	protected virtual void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
	{
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_quest5SetPieceBattleMissionController = base.Mission.GetMissionBehavior<Quest5SetPieceBattleMissionController>();
		LastHitCheckpoint = _quest5SetPieceBattleMissionController.LastHitCheckpoint;
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		MissionItemContourControllerView missionBehavior = base.Mission.GetMissionBehavior<MissionItemContourControllerView>();
		if (missionBehavior != null)
		{
			for (int i = 0; i < _barrelEntities.Count; i++)
			{
				missionBehavior.RemoveAdditionalContourEntity(_barrelEntities[i]);
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		PassMissionStateOnTick(_quest5SetPieceBattleMissionController.State);
		HandleAllowedSwimRadiusCheck();
		HandleApproachPlayerShipLocationCheck();
		HandleEscapeShipStuckCheck();
		HandlePurigCutsceneCameraChange();
		if (!_isBarrelListInitialized)
		{
			InitializeBarrelList();
		}
		if (!_isPlayerShipRotationCorrectedAtTheStartOfTheMission && _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip)
		{
			ChangeMainAgentRotation(_quest5SetPieceBattleMissionController.CalculateMissionStartDirection());
			_isPlayerShipRotationCorrectedAtTheStartOfTheMission = true;
		}
		if (_state == Quest5SetPieceBattleMissionViewState.None && (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeOut || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeOut || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeOut || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeOut || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeOut || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeOut))
		{
			_state = Quest5SetPieceBattleMissionViewState.FadeOut;
			SetPlayerMovementEnabled(isPlayerMovementEnabled: false);
		}
		Quest5SetPieceBattleMissionController quest5SetPieceBattleMissionController = _quest5SetPieceBattleMissionController;
		if (quest5SetPieceBattleMissionController != null && quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.End)
		{
			if (_missionEndTimer == null)
			{
				_missionEndTimer = new MissionTimer(1.75f);
			}
			else
			{
				_missionEndTimer.Check();
			}
		}
		switch (_state)
		{
		case Quest5SetPieceBattleMissionViewState.FadeOut:
			ScreenFadeController.BeginFadeOutAndIn(1f, 1f, 1f);
			_state = Quest5SetPieceBattleMissionViewState.Initialize;
			break;
		case Quest5SetPieceBattleMissionViewState.Initialize:
			if (!ScreenFadeController.IsFadedOut)
			{
				break;
			}
			if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeOut)
			{
				_quest5SetPieceBattleMissionController.TriggerPhase1InitializeShipInteriorPhase();
			}
			else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeOut)
			{
				_quest5SetPieceBattleMissionController.TriggerPhase1InitializeGoBackToShipPhase();
			}
			else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeOut)
			{
				_quest5SetPieceBattleMissionController.TriggerInitializePhase2();
			}
			else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeOut)
			{
				_quest5SetPieceBattleMissionController.TriggerInitializePhase3();
			}
			else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeOut)
			{
				_quest5SetPieceBattleMissionController.TriggerInitializePhase4();
			}
			else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeOut)
			{
				_quest5SetPieceBattleMissionController.TriggerInitializeBossFight();
			}
			else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeIn || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeIn || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeIn || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeIn || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeIn || _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeIn)
			{
				if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeIn)
				{
					_quest5SetPieceBattleMissionController.GetIntendedMainAgentDirectionForPhase1InteriorTeleport(out var mainAgentDirection);
					ChangeMainAgentRotation(mainAgentDirection);
				}
				else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeIn)
				{
					_quest5SetPieceBattleMissionController.GetIntendedMainAgentDirectionForPhase1EscapeShipTeleport(out var mainAgentDirection2);
					ChangeMainAgentRotation(mainAgentDirection2);
				}
				else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeIn)
				{
					_purigCutsceneCameraChangeState = PurigCutsceneCameraChangeState.WaitingForCountDown;
				}
				_state = Quest5SetPieceBattleMissionViewState.FadeIn;
			}
			break;
		case Quest5SetPieceBattleMissionViewState.FadeIn:
			if (!_isMainAgentRotatedBeforeBossFight && _quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeIn)
			{
				_isMainAgentRotatedBeforeBossFight = true;
				_quest5SetPieceBattleMissionController.GetIntendedMainAgentDirectionForBossFight(out var direction);
				ChangeMainAgentRotation(direction);
			}
			if (!ScreenFadeController.IsFadeActive)
			{
				if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeIn)
				{
					_quest5SetPieceBattleMissionController.CompletePhase1GoToShipInteriorTransition();
				}
				else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeIn)
				{
					_quest5SetPieceBattleMissionController.CompletePhase1InitializeGoBackToShipTransition();
				}
				else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeIn)
				{
					_quest5SetPieceBattleMissionController.CompletePhase1ToPhase2Transition();
				}
				else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeIn)
				{
					_quest5SetPieceBattleMissionController.CompletePhase2ToPhase3Transition();
				}
				else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeIn)
				{
					_quest5SetPieceBattleMissionController.CompletePhase3ToPhase4Transition();
				}
				else if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeIn)
				{
					_quest5SetPieceBattleMissionController.CompletePhase4ToBossFightTransition();
				}
				SetPlayerMovementEnabled(isPlayerMovementEnabled: true);
				_state = Quest5SetPieceBattleMissionViewState.End;
			}
			break;
		case Quest5SetPieceBattleMissionViewState.End:
			_state = Quest5SetPieceBattleMissionViewState.None;
			break;
		case Quest5SetPieceBattleMissionViewState.None:
			break;
		}
	}

	private void HandleAllowedSwimRadiusCheck()
	{
		if (_allowedSwimRadiusCheckState == AllowedSwimRadiusCheckState.End)
		{
			return;
		}
		switch (_allowedSwimRadiusCheckState)
		{
		case AllowedSwimRadiusCheckState.None:
		{
			Quest5SetPieceBattleMissionController quest5SetPieceBattleMissionController = _quest5SetPieceBattleMissionController;
			if (quest5SetPieceBattleMissionController != null && quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip)
			{
				_allowedSwimRadiusCheckState = AllowedSwimRadiusCheckState.CheckDistance;
			}
			break;
		}
		case AllowedSwimRadiusCheckState.CheckDistance:
			if (_quest5SetPieceBattleMissionController.State >= Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase2Part1)
			{
				_allowedSwimRadiusCheckState = AllowedSwimRadiusCheckState.End;
			}
			else if (_quest5SetPieceBattleMissionController.ShouldTeleportPlayerBetweenTargetPositionAndHidingSpot())
			{
				_allowedSwimRadiusCheckState = AllowedSwimRadiusCheckState.FadeOut;
			}
			break;
		case AllowedSwimRadiusCheckState.FadeOut:
			ScreenFadeController.BeginFadeOutAndIn(0.25f, 0.25f, 0.25f);
			_allowedSwimRadiusCheckState = AllowedSwimRadiusCheckState.TeleportPlayer;
			SetPlayerMovementEnabled(isPlayerMovementEnabled: false);
			break;
		case AllowedSwimRadiusCheckState.TeleportPlayer:
			if (ScreenFadeController.IsFadedOut)
			{
				_quest5SetPieceBattleMissionController.TeleportPlayerBetweenTargetPositionAndHidingSpot(out var mainAgentDirection);
				ChangeMainAgentRotation(mainAgentDirection);
				MBInformationManager.AddQuickInformation(_restrictionNotificationText);
				_allowedSwimRadiusCheckState = AllowedSwimRadiusCheckState.CheckDistance;
				SetPlayerMovementEnabled(isPlayerMovementEnabled: true);
			}
			break;
		}
	}

	private void HandleApproachPlayerShipLocationCheck()
	{
		if (_approachPlayerShipLocationCheckState == ApproachPlayerShipLocationCheckState.End)
		{
			return;
		}
		switch (_approachPlayerShipLocationCheckState)
		{
		case ApproachPlayerShipLocationCheckState.None:
		{
			Quest5SetPieceBattleMissionController quest5SetPieceBattleMissionController = _quest5SetPieceBattleMissionController;
			if (quest5SetPieceBattleMissionController != null && quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip)
			{
				_approachPlayerShipLocationCheckState = ApproachPlayerShipLocationCheckState.CheckDistance;
			}
			break;
		}
		case ApproachPlayerShipLocationCheckState.CheckDistance:
			if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip)
			{
				if (_quest5SetPieceBattleMissionController.ShouldTeleportPlayerShipToStartingPosition())
				{
					_approachPlayerShipLocationCheckState = ApproachPlayerShipLocationCheckState.FadeOut;
				}
			}
			else
			{
				_approachPlayerShipLocationCheckState = ApproachPlayerShipLocationCheckState.End;
			}
			break;
		case ApproachPlayerShipLocationCheckState.FadeOut:
			ScreenFadeController.BeginFadeOutAndIn(0.25f, 0.25f, 0.25f);
			_approachPlayerShipLocationCheckState = ApproachPlayerShipLocationCheckState.TeleportPlayerShip;
			SetPlayerMovementEnabled(isPlayerMovementEnabled: false);
			break;
		case ApproachPlayerShipLocationCheckState.TeleportPlayerShip:
			if (ScreenFadeController.IsFadedOut)
			{
				_quest5SetPieceBattleMissionController.TeleportPlayerShipToStartingPosition(out var mainAgentDirection);
				ChangeMainAgentRotation(mainAgentDirection);
				MBInformationManager.AddQuickInformation(_restrictionNotificationText);
				_approachPlayerShipLocationCheckState = ApproachPlayerShipLocationCheckState.CheckDistance;
				SetPlayerMovementEnabled(isPlayerMovementEnabled: true);
			}
			break;
		}
	}

	private void HandlePurigCutsceneCameraChange()
	{
		switch (_purigCutsceneCameraChangeState)
		{
		case PurigCutsceneCameraChangeState.WaitingForCountDown:
		{
			if (_purigCutsceneCameraChangeTimer == null)
			{
				InitializePurigShipCutsceneCamera();
				_quest5SetPieceBattleMissionController.OnPurigCutsceneStarted();
				break;
			}
			if (_purigCutsceneCameraChangeTimer != null && _purigCutsceneCameraChangeTimer.Check())
			{
				_purigCutsceneCameraChangeTimer = null;
				_purigCutsceneCameraChangeState = PurigCutsceneCameraChangeState.FadeOut;
				SetPlayerMovementEnabled(isPlayerMovementEnabled: false);
			}
			GetCameraFrame(out var cameraFrame3);
			PurigShipCutsceneCamera.Frame = cameraFrame3;
			base.MissionScreen.CustomCamera = PurigShipCutsceneCamera;
			break;
		}
		case PurigCutsceneCameraChangeState.FadeOut:
		{
			ScreenFadeController.BeginFadeOutAndIn();
			_purigCutsceneCameraChangeState = PurigCutsceneCameraChangeState.ChangeBackToDefaultCamera;
			GetCameraFrame(out var cameraFrame2);
			PurigShipCutsceneCamera.Frame = cameraFrame2;
			base.MissionScreen.CustomCamera = PurigShipCutsceneCamera;
			break;
		}
		case PurigCutsceneCameraChangeState.ChangeBackToDefaultCamera:
			if (ScreenFadeController.IsFadedOut)
			{
				base.MissionScreen.CustomCamera = null;
				_purigCutsceneCameraChangeState = PurigCutsceneCameraChangeState.End;
				_quest5SetPieceBattleMissionController.OnPurigShipCutsceneEnded();
				SetPlayerMovementEnabled(isPlayerMovementEnabled: true);
			}
			else
			{
				GetCameraFrame(out var cameraFrame);
				PurigShipCutsceneCamera.Frame = cameraFrame;
				base.MissionScreen.CustomCamera = PurigShipCutsceneCamera;
			}
			break;
		}
	}

	private void InitializePurigShipCutsceneCamera()
	{
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("purig_ship_cutscene_cam_tag");
		if (gameEntity != null && _quest5SetPieceBattleMissionController.Phase4PurigShip != null)
		{
			Vec3 dofParams = Vec3.Invalid;
			PurigShipCutsceneCamera = Camera.CreateCamera();
			gameEntity.GetCameraParamsFromCameraScript(PurigShipCutsceneCamera, ref dofParams);
			PurigShipCutsceneCamera.SetFovVertical(PurigShipCutsceneCamera.GetFovVertical(), Screen.AspectRatio, PurigShipCutsceneCamera.Near, PurigShipCutsceneCamera.Far);
			GetCameraFrame(out var cameraFrame);
			PurigShipCutsceneCamera.Frame = cameraFrame;
			base.MissionScreen.CustomCamera = PurigShipCutsceneCamera;
			_purigCutsceneCameraChangeTimer = new MissionTimer(6f);
		}
	}

	private void GetCameraFrame(out MatrixFrame cameraFrame)
	{
		cameraFrame = PurigShipCutsceneCamera.Frame;
	}

	private void ChangeMainAgentRotation(Vec3 mainAgentDirection)
	{
		Agent.Main.SetMovementDirection(mainAgentDirection.AsVec2.Normalized());
		base.MissionScreen.CameraBearing = mainAgentDirection.RotationZ;
	}

	private void HandleEscapeShipStuckCheck()
	{
		if (_escapeShipStuckCheckState == EscapeShipStuckCheckState.End)
		{
			return;
		}
		switch (_escapeShipStuckCheckState)
		{
		case EscapeShipStuckCheckState.None:
		{
			Quest5SetPieceBattleMissionController quest5SetPieceBattleMissionController = _quest5SetPieceBattleMissionController;
			if (quest5SetPieceBattleMissionController != null && quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2InProgress)
			{
				_escapeShipStuckCheckState = EscapeShipStuckCheckState.CheckForStuck;
			}
			break;
		}
		case EscapeShipStuckCheckState.CheckForStuck:
			if (_quest5SetPieceBattleMissionController.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2InProgress)
			{
				if (_quest5SetPieceBattleMissionController.IsEscapeShipStuck)
				{
					_escapeShipStuckCheckState = EscapeShipStuckCheckState.FadeOut;
					SetPlayerMovementEnabled(isPlayerMovementEnabled: false);
				}
			}
			else
			{
				_escapeShipStuckCheckState = EscapeShipStuckCheckState.End;
			}
			break;
		case EscapeShipStuckCheckState.FadeOut:
			ScreenFadeController.BeginFadeOutAndIn(0.25f, 0.25f, 0.25f);
			_escapeShipStuckCheckState = EscapeShipStuckCheckState.TeleportEscapeShip;
			break;
		case EscapeShipStuckCheckState.TeleportEscapeShip:
			if (ScreenFadeController.IsFadedOut)
			{
				_quest5SetPieceBattleMissionController.HandleEscapeShipStuck();
				SetPlayerMovementEnabled(isPlayerMovementEnabled: true);
				_escapeShipStuckCheckState = EscapeShipStuckCheckState.CheckForStuck;
			}
			break;
		}
	}

	private void InitializeBarrelList()
	{
		MissionItemContourControllerView missionBehavior = base.Mission.GetMissionBehavior<MissionItemContourControllerView>();
		if (missionBehavior == null)
		{
			return;
		}
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("dromon_javelin_barrel"))
		{
			_barrelEntities.Add(item.WeakEntity);
		}
		for (int i = 0; i < _barrelEntities.Count; i++)
		{
			missionBehavior.AddAdditionalContourEntity(_barrelEntities[i]);
		}
		if (!_barrelEntities.IsEmpty())
		{
			_isBarrelListInitialized = true;
		}
	}
}
