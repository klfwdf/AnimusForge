using NavalDLC.Storyline.MissionControllers;
using NavalDLC.View.MissionViews;
using NavalDLC.View.MissionViews.Storyline;
using SandBox.View.Missions;
using SandBox.View.Missions.NameMarkers;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(Quest5SetPieceBattleMissionView))]
public class MissionGauntletQuest5SetPieceBattleMissionView : Quest5SetPieceBattleMissionView
{
	private class Quest5CutsceneGauntletLayer : GauntletLayer
	{
		public Quest5CutsceneGauntletLayer(int localOrder, bool shouldClear = false)
			: base("Quest5CutsceneGauntletLayer", localOrder, shouldClear)
		{
		}

		public override bool HitTest()
		{
			return true;
		}
	}

	private bool _disableOrderUI;

	private bool _isOrderUIDisabled;

	private bool _disableShipMarkers;

	private bool _isShipMarkersDisabled;

	private bool _disableStealthBar;

	private bool _isStealthBarDisabled;

	private bool _disableNameMarkers;

	private bool _isNameMarkersDisabled;

	private bool _disableAgentBanners;

	private bool _isAgentBannersDisabled;

	private bool _disableShipHighlights;

	private bool _isShipHighlightsDisabled;

	private bool _isShipCameraUpdatedAtTheStartOfApproachPhase;

	private bool _isShipCameraUpdatedAtTheStartOfPhase3;

	private Quest5CutsceneGauntletLayer _gauntletLayer;

	public MissionGauntletQuest5SetPieceBattleMissionView()
	{
		_gauntletLayer = new Quest5CutsceneGauntletLayer(10);
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		HandleOrderUISuspendStateChange();
		HandleShipMarkersSuspendStateChange();
		HandleStealthBarSuspendStateChange();
		HandleNameMarkersSuspendStateChange();
		HandleAgentBannerSuspendStateChange();
		HandleShipHighlightSuspendStateChange();
	}

	public override void OnConversationEnd()
	{
		base.OnConversationEnd();
		_disableOrderUI = false;
		_disableShipMarkers = false;
		_disableStealthBar = false;
		_disableNameMarkers = false;
		_disableAgentBanners = false;
		_disableShipHighlights = false;
		HandleOrderUISuspendStateChange();
		HandleShipMarkersSuspendStateChange();
		HandleStealthBarSuspendStateChange();
		HandleNameMarkersSuspendStateChange();
		HandleAgentBannerSuspendStateChange();
		HandleShipHighlightSuspendStateChange();
	}

	public override void PassMissionStateOnTick(Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState currentState)
	{
		base.PassMissionStateOnTick(currentState);
		if (!_isShipCameraUpdatedAtTheStartOfApproachPhase && currentState == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip)
		{
			SetActiveCameraModeForShip(MissionShipControlView.CameraModes.Back);
			_isShipCameraUpdatedAtTheStartOfApproachPhase = true;
		}
		if (!_isShipCameraUpdatedAtTheStartOfPhase3 && currentState == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3InProgress)
		{
			SetActiveCameraModeForShip(MissionShipControlView.CameraModes.Back);
			_isShipCameraUpdatedAtTheStartOfPhase3 = true;
		}
		switch (currentState)
		{
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase1Part1:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase1Part2:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1SwimmingPhase:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializeStealthPhasePart1:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializeStealthPhasePart2:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1StealthPhase:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeOut:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = true;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1InitializeShipInteriorPhase:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = true;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeIn:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = true;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ShipInteriorPhase:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = true;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeOut:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = true;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1InitializeGoBackToShip:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = true;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeIn:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = true;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1EscapePhase:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeOut:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = true;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase2Part1:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase2Part2:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase2Part3:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeIn:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = true;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2InProgress:
			_disableOrderUI = true;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeOut:
			_disableOrderUI = true;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase3Part1:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase3Part2:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase3Part3:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeIn:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3InProgress:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeOut:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase4Part1:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase4Part2:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeIn:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4InProgress:
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeOut:
			_disableOrderUI = false;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializeBossFightPart1:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializeBossFightPart2:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeIn:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.StartBossFightConversation:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.BossFightConversationInProgress:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.BossFightInProgressAsDuel:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.BossFightInProgressAsAll:
			_disableOrderUI = true;
			_disableShipMarkers = true;
			_disableStealthBar = true;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			break;
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.None:
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase2Part4:
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.End:
		case Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Exit:
			break;
		}
	}

	private void HandleOrderUISuspendStateChange()
	{
		if (_disableOrderUI)
		{
			if (!_isOrderUIDisabled)
			{
				GauntletOrderUIHandler missionBehavior = base.Mission.GetMissionBehavior<GauntletOrderUIHandler>();
				if (missionBehavior != null && missionBehavior.IsViewCreated)
				{
					SetMissionViewVisibility<GauntletOrderUIHandler>(isVisible: false);
					_isOrderUIDisabled = true;
				}
			}
		}
		else if (_isOrderUIDisabled)
		{
			GauntletOrderUIHandler missionBehavior2 = base.Mission.GetMissionBehavior<GauntletOrderUIHandler>();
			if (missionBehavior2 != null && missionBehavior2.IsViewCreated)
			{
				SetMissionViewVisibility<GauntletOrderUIHandler>(isVisible: true);
				_isOrderUIDisabled = false;
			}
		}
	}

	private void HandleShipMarkersSuspendStateChange()
	{
		if (_disableShipMarkers)
		{
			if (!_isShipMarkersDisabled)
			{
				MissionGauntletNavalShipMarker missionBehavior = base.Mission.GetMissionBehavior<MissionGauntletNavalShipMarker>();
				if (missionBehavior != null && missionBehavior.IsViewCreated)
				{
					SetMissionViewVisibility<MissionGauntletNavalShipMarker>(isVisible: false);
					_isShipMarkersDisabled = true;
				}
			}
		}
		else if (_isShipMarkersDisabled)
		{
			MissionGauntletNavalShipMarker missionBehavior2 = base.Mission.GetMissionBehavior<MissionGauntletNavalShipMarker>();
			if (missionBehavior2 != null && missionBehavior2.IsViewCreated)
			{
				SetMissionViewVisibility<MissionGauntletNavalShipMarker>(isVisible: true);
				_isShipMarkersDisabled = false;
			}
		}
	}

	private void HandleStealthBarSuspendStateChange()
	{
		if (_disableStealthBar)
		{
			if (!_isStealthBarDisabled)
			{
				MissionAgentAlarmStateView missionBehavior = base.Mission.GetMissionBehavior<MissionAgentAlarmStateView>();
				if (missionBehavior != null && missionBehavior.IsReady())
				{
					SetMissionViewVisibility<MissionAgentAlarmStateView>(isVisible: false);
					_isStealthBarDisabled = true;
				}
			}
		}
		else if (_isStealthBarDisabled)
		{
			MissionAgentAlarmStateView missionBehavior2 = base.Mission.GetMissionBehavior<MissionAgentAlarmStateView>();
			if (missionBehavior2 != null && missionBehavior2.IsReady())
			{
				SetMissionViewVisibility<MissionAgentAlarmStateView>(isVisible: true);
				_isStealthBarDisabled = false;
			}
		}
	}

	private void HandleNameMarkersSuspendStateChange()
	{
		if (_disableNameMarkers)
		{
			if (!_isNameMarkersDisabled)
			{
				MissionNameMarkerUIHandler missionBehavior = base.Mission.GetMissionBehavior<MissionNameMarkerUIHandler>();
				if (missionBehavior != null && missionBehavior.IsReady())
				{
					SetMissionViewVisibility<MissionNameMarkerUIHandler>(isVisible: false);
					_isNameMarkersDisabled = true;
				}
			}
		}
		else if (_isNameMarkersDisabled)
		{
			MissionNameMarkerUIHandler missionBehavior2 = base.Mission.GetMissionBehavior<MissionNameMarkerUIHandler>();
			if (missionBehavior2 != null && missionBehavior2.IsReady())
			{
				SetMissionViewVisibility<MissionNameMarkerUIHandler>(isVisible: true);
				_isNameMarkersDisabled = false;
			}
		}
	}

	private void HandleAgentBannerSuspendStateChange()
	{
		if (_disableAgentBanners)
		{
			if (!_isAgentBannersDisabled)
			{
				MissionAgentLabelView missionBehavior = base.Mission.GetMissionBehavior<MissionAgentLabelView>();
				if (missionBehavior != null && missionBehavior.IsReady())
				{
					SetMissionViewVisibility<MissionAgentLabelView>(isVisible: false);
					_isAgentBannersDisabled = true;
				}
			}
		}
		else if (_isAgentBannersDisabled)
		{
			MissionAgentLabelView missionBehavior2 = base.Mission.GetMissionBehavior<MissionAgentLabelView>();
			if (missionBehavior2 != null && missionBehavior2.IsReady())
			{
				SetMissionViewVisibility<MissionAgentLabelView>(isVisible: true);
				_isAgentBannersDisabled = false;
			}
		}
	}

	private void HandleShipHighlightSuspendStateChange()
	{
		if (_disableShipHighlights)
		{
			if (!_isShipHighlightsDisabled)
			{
				MissionGauntletShipControlView missionBehavior = base.Mission.GetMissionBehavior<MissionGauntletShipControlView>();
				if (missionBehavior != null && missionBehavior.IsReady())
				{
					missionBehavior.SuspendFeature(MissionGauntletShipControlView.ShipControlFeatureFlags.ShipFocus);
					_isShipHighlightsDisabled = true;
				}
			}
		}
		else if (_isShipHighlightsDisabled)
		{
			MissionGauntletShipControlView missionBehavior2 = base.Mission.GetMissionBehavior<MissionGauntletShipControlView>();
			if (missionBehavior2 != null && missionBehavior2.IsReady())
			{
				missionBehavior2.ResumeFeature(MissionGauntletShipControlView.ShipControlFeatureFlags.ShipFocus);
				_isShipHighlightsDisabled = false;
			}
		}
	}

	private void SetMissionViewVisibility<T>(bool isVisible) where T : MissionView
	{
		T missionBehavior = base.Mission.GetMissionBehavior<T>();
		if (missionBehavior != null)
		{
			if (isVisible)
			{
				missionBehavior.ResumeView();
			}
			else
			{
				missionBehavior.SuspendView();
			}
		}
		else
		{
			Debug.FailedAssert("Trying to set visibility of mission view: " + typeof(T).Name + " but it does not exist in the mission!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.GauntletUI\\MissionViews\\MissionGauntletQuest5SetPieceBattleMissionView.cs", "SetMissionViewVisibility", 695);
		}
	}

	private void SetActiveCameraModeForShip(MissionShipControlView.CameraModes mode)
	{
		MissionGauntletShipControlView missionBehavior = base.Mission.GetMissionBehavior<MissionGauntletShipControlView>();
		if (missionBehavior != null && missionBehavior.IsReady())
		{
			missionBehavior.SetActiveCameraMode(mode);
		}
	}

	protected override void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
	{
		base.SetPlayerMovementEnabled(isPlayerMovementEnabled);
		for (int i = 0; i < base.Mission.MissionBehaviors.Count; i++)
		{
			if (base.Mission.MissionBehaviors[i] is MissionBattleUIBaseView missionBattleUIBaseView)
			{
				if (!isPlayerMovementEnabled)
				{
					missionBattleUIBaseView.SuspendView();
				}
				else
				{
					missionBattleUIBaseView.ResumeView();
				}
			}
		}
		if (isPlayerMovementEnabled)
		{
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			_disableOrderUI = false;
			_disableShipMarkers = false;
			_disableStealthBar = false;
			_disableNameMarkers = false;
			_disableAgentBanners = false;
			_disableShipHighlights = false;
			HandleOrderUISuspendStateChange();
			HandleShipMarkersSuspendStateChange();
			HandleStealthBarSuspendStateChange();
			HandleNameMarkersSuspendStateChange();
			HandleAgentBannerSuspendStateChange();
			HandleShipHighlightSuspendStateChange();
		}
		else
		{
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
			_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		}
	}
}
