using System;
using System.Numerics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipInput;
using NavalDLC.View.MissionViews;
using NavalDLC.ViewModelCollection.Missions.ShipControl;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(MissionShipControlView))]
public class MissionGauntletShipControlView : MissionShipControlView
{
	[Flags]
	public enum ShipControlFeatureFlags
	{
		ShipFocus = 1,
		ShipSelection = 2,
		AttemptBoarding = 4,
		ToggleOarsmen = 8,
		ToggleSails = 0x10,
		CutLoose = 0x20,
		BallistaOrder = 0x40,
		ShootBallista = 0x80,
		ChangeCamera = 0x100
	}

	private GauntletLayer _gauntletLayer;

	private MissionShipControlVM _dataSource;

	private MissionGauntletSingleplayerOrderUIHandler _orderUIHandler;

	private MissionGauntletCrosshair _crosshairView;

	private NavalMissionShipHighlightView _shipHighlightView;

	private MissionGauntletNavalAgentStatus _agentStatusView;

	private MissionShip _playerControlledShip;

	private MissionShip _focusedShip;

	private bool _playerControlledShipHasHybridSails;

	private bool _isAnyBridgeActive;

	private bool _isBattleUIVisible;

	private bool _isPhotoModeActive;

	private bool _lastFirstPersonModeSelection;

	private const float AttemptBoardingDistance = 50f;

	private const float SelectShipDistance = 300f;

	public ShipControlFeatureFlags SuspendedFeatures { get; private set; }

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new MissionShipControlVM();
		_gauntletLayer = new GauntletLayer("MissionShipControl", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MissionShipControl", _dataSource);
		_orderUIHandler = base.Mission.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
		_crosshairView = base.Mission.GetMissionBehavior<MissionGauntletCrosshair>();
		_shipHighlightView = base.Mission.GetMissionBehavior<NavalMissionShipHighlightView>();
		_agentStatusView = base.Mission.GetMissionBehavior<MissionGauntletNavalAgentStatus>();
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(HotKeyManager.GetCategory("NavalShipControlsHotKeyCategory")))
		{
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("NavalShipControlsHotKeyCategory"));
		}
		base.MissionScreen.AddLayer(_gauntletLayer);
		SetControlKeys();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		_dataSource.OnFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_dataSource = null;
		_gauntletLayer = null;
	}

	protected override void OnCreateView()
	{
		base.OnCreateView();
		_isBattleUIVisible = true;
	}

	protected override void OnDestroyView()
	{
		base.OnDestroyView();
		_isBattleUIVisible = false;
	}

	public void SuspendFeature(ShipControlFeatureFlags feature)
	{
		SuspendedFeatures |= feature;
	}

	public bool IsFeatureSuspended(ShipControlFeatureFlags feature)
	{
		return (SuspendedFeatures & feature) != 0;
	}

	public void ResumeFeature(ShipControlFeatureFlags feature)
	{
		SuspendedFeatures &= ~feature;
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		_isPhotoModeActive = true;
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		_isPhotoModeActive = false;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		UpdateVisibility();
		MissionShip playerControlledShip = _playerControlledShip;
		_playerControlledShip = NavalShipsLogic?.PlayerControlledShip;
		_isAnyBridgeActive = _playerControlledShip?.GetIsAnyBridgeActive() ?? false;
		if (playerControlledShip != _playerControlledShip)
		{
			if (_playerControlledShip != null)
			{
				_crosshairView?.SuspendView();
				_lastFirstPersonModeSelection = base.Mission.CameraIsFirstPerson;
				base.Mission.CameraIsFirstPerson = false;
			}
			else
			{
				_crosshairView?.ResumeView();
				base.Mission.CameraIsFirstPerson = _lastFirstPersonModeSelection;
				if (IsAimingWithRangedWeapon)
				{
					IsAimingWithRangedWeapon = false;
					playerControlledShip?.OnSetRangedWeaponControlMode(value: false);
				}
			}
		}
		if (_playerControlledShip != null && IsAimingWithRangedWeapon && !GetIsRangedWeaponAvailable())
		{
			IsAimingWithRangedWeapon = false;
			_playerControlledShip.OnSetRangedWeaponControlMode(value: false);
		}
		UpdateShipValues();
		RefreshControlKeys();
		UpdateFocusedShip();
		TickInput();
	}

	private void UpdateHitPoints()
	{
		if (_dataSource != null)
		{
			if (_playerControlledShip == null)
			{
				_dataSource.ShipHitPoints.IsRelevant = false;
				_dataSource.SailHitPoints.IsRelevant = false;
				_dataSource.FireHitPoints.IsRelevant = false;
				return;
			}
			_dataSource.ShipHitPoints.IsRelevant = true;
			_dataSource.SailHitPoints.IsRelevant = true;
			_dataSource.FireHitPoints.IsRelevant = true;
			_dataSource.ShipHitPoints.ActiveHitPoints = TaleWorlds.Library.MathF.Round(_playerControlledShip.HitPoints);
			_dataSource.ShipHitPoints.MaxHitPoints = TaleWorlds.Library.MathF.Round(_playerControlledShip.MaxHealth);
			_dataSource.SailHitPoints.ActiveHitPoints = TaleWorlds.Library.MathF.Round(_playerControlledShip.SailHitPoints);
			_dataSource.SailHitPoints.MaxHitPoints = TaleWorlds.Library.MathF.Round(_playerControlledShip.MaxSailHitPoints);
			_dataSource.FireHitPoints.ActiveHitPoints = TaleWorlds.Library.MathF.Round(_playerControlledShip.FireHitPoints);
			_dataSource.FireHitPoints.MaxHitPoints = TaleWorlds.Library.MathF.Round(_playerControlledShip.MaxFireHealth);
		}
	}

	private void TickInput()
	{
		InputContext inputContext = base.MissionScreen?.SceneLayer?.Input;
		if (inputContext == null || _playerControlledShip == null || base.MissionScreen.IsPhotoModeEnabled || base.IsDisplayingADialog || base.MissionScreen.IsCheatGhostMode)
		{
			return;
		}
		if (inputContext.IsGameKeyReleased(111))
		{
			if (GetCanToggleOarsmen())
			{
				int num = (_playerControlledShip.ShipOrder.OarsmenLevel + 2) % 3;
				_playerControlledShip.ShipOrder.SetOrderOarsmenLevel(num);
				TextObject textObject = null;
				switch (num)
				{
				case 0:
					textObject = new TextObject("{=RtRNkfMA}Stop using the oars!");
					break;
				case 1:
					textObject = new TextObject("{=a7CzRLXb}Use oars in half power!");
					break;
				case 2:
					textObject = new TextObject("{=RKthVuaC}Use oars in full power!");
					break;
				}
				if (textObject != null)
				{
					DisplayCommandForSelectedFormations(textObject);
				}
			}
			else if (GetCanCutLoose() && !GetIsCutLooseTemporarilyBlocked())
			{
				_playerControlledShip.ShipOrder.SetCutLoose(enable: true);
				DisplayCommandForSelectedFormations(new TextObject("{=siE18G0C}Cut loose!"));
			}
		}
		if (inputContext.IsGameKeyReleased(110) && GetCanToggleSail())
		{
			SailControl = (SailControl.IsMax() ? SailControl.Min(_playerControlledShipHasHybridSails) : SailControl.Raise(_playerControlledShipHasHybridSails));
			switch (SailControl)
			{
			case SailInput.Raised:
				DisplayCommandForSelectedFormations(new TextObject("{=kWfyfiVA}Furl sails!"));
				break;
			case SailInput.SquareSailsRaised:
				DisplayCommandForSelectedFormations(new TextObject("{=kGtL9Kea}Furl square sails!"));
				break;
			case SailInput.Full:
				DisplayCommandForSelectedFormations(new TextObject("{=75VaP7bL}Open sails!"));
				break;
			}
		}
		if (inputContext.IsGameKeyReleased(112) && GetCanChangeCamera())
		{
			base.ActiveCameraMode = (CameraModes)((int)(base.ActiveCameraMode + 1) % 3);
		}
		if (inputContext.IsGameKeyReleased(113) && GetCanSelectShip())
		{
			int num2 = _focusedShip.Formation?.Index ?? (-1);
			if (num2 >= 0)
			{
				_orderUIHandler.SelectFormationAtIndex(num2);
			}
		}
		if (inputContext.IsGameKeyReleased(114) && GetCanAttemptBoarding())
		{
			if (GetIsCancelBoardingAvailable())
			{
				_playerControlledShip.ShipOrder.SetBoardingTargetShip(null);
				DisplayCommandForSelectedFormations(new TextObject("{=U6Z4GFPW}Stop boarding!"));
			}
			else if (!GetIsAttemptBoardingTemporarilyBlocked())
			{
				_playerControlledShip.ShipOrder.SetBoardingTargetShip(_focusedShip);
				DisplayCommandForSelectedFormations(new TextObject("{=HSALr4nl}Board {SHIP_NAME}!").SetTextVariable("SHIP_NAME", (_focusedShip.Team == null || _focusedShip.Team.TeamSide == TeamSideEnum.EnemyTeam) ? _focusedShip.ShipOrigin.Hull.Name : _focusedShip.ShipOrigin.Name));
			}
		}
		if (inputContext.IsGameKeyReleased(115) && GetCanToggleRangedWeaponOrderMode())
		{
			IsAimingWithRangedWeapon = !IsAimingWithRangedWeapon;
			_playerControlledShip.OnSetRangedWeaponControlMode(IsAimingWithRangedWeapon);
		}
		if (inputContext.IsGameKeyReleased(9) && GetCanShootBallista())
		{
			_playerControlledShip.ShootBallista();
		}
	}

	private void DisplayCommandForSelectedFormations(TextObject command)
	{
		TextObject textObject = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
		textObject.SetTextVariable("STR1", _playerControlledShip?.ShipOrigin?.Name ?? new TextObject("{=wXCM8BnW}Crew"));
		textObject.SetTextVariable("STR2", command);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
	}

	private void UpdateFocusedShip()
	{
		if (base.Mission.Scene == null || _playerControlledShip == null || base.MissionScreen.IsPhotoModeEnabled || base.IsDisplayingADialog || IsFeatureSuspended(ShipControlFeatureFlags.ShipFocus))
		{
			_dataSource?.SetTargetedShip(null);
			SetFocusedShip(null);
			_dataSource.SetBoardingTargetShip(null);
			return;
		}
		MatrixFrame lastFinalRenderCameraFrame = base.Mission.Scene.LastFinalRenderCameraFrame;
		Vec2 screenCenter = Screen.RealScreenResolution * 0.5f;
		float closestDistance = float.MaxValue;
		float focusRadius = Screen.RealScreenResolutionHeight / 4f;
		MissionShip closestShip = null;
		Vec3 globalPosition = _playerControlledShip.GameEntity.GlobalPosition;
		Vec3 hitScreenPosition = Vec3.Zero;
		for (int i = 0; i < NavalShipsLogic.AllShips.Count; i++)
		{
			CheckFocusableShip(NavalShipsLogic.AllShips[i], globalPosition, 100f, 350f, lastFinalRenderCameraFrame, screenCenter, ref hitScreenPosition, ref closestDistance, focusRadius, ref closestShip, out var directHitFound);
			if (directHitFound)
			{
				break;
			}
		}
		SetFocusedShip(closestShip);
		if (_dataSource != null)
		{
			_dataSource.SetTargetedShip(closestShip, hitScreenPosition.x, hitScreenPosition.y - 70f, hitScreenPosition.z);
			_dataSource.TargetedShipHasAction = !MBCommon.IsPaused && (GetCanAttemptBoarding() || GetCanSelectShip());
			_dataSource.IsCancelBoardingOrderAvailable = GetIsCancelBoardingAvailable();
		}
	}

	private void CheckFocusableShip(MissionShip focusableShip, Vec3 playerShipPosition, float enemyFocusDistance, float friendlyFocusDistance, MatrixFrame cameraFrame, Vec2 screenCenter, ref Vec3 hitScreenPosition, ref float closestDistance, float focusRadius, ref MissionShip closestShip, out bool directHitFound)
	{
		directHitFound = false;
		if (focusableShip.IsDisabled || focusableShip.IsSinking || focusableShip == _playerControlledShip)
		{
			return;
		}
		Vec3 globalPosition = focusableShip.GameEntity.GlobalPosition;
		if ((focusableShip.BattleSide == base.Mission.PlayerEnemyTeam.Side && globalPosition.DistanceSquared(playerShipPosition) > enemyFocusDistance * enemyFocusDistance) || (focusableShip.BattleSide == base.Mission.PlayerTeam.Side && globalPosition.DistanceSquared(playerShipPosition) > friendlyFocusDistance * friendlyFocusDistance))
		{
			return;
		}
		Vec3 shipFocusPosition = GetShipFocusPosition(focusableShip);
		float screenX = -5000f;
		float screenY = -5000f;
		float w = -5000f;
		MBWindowManager.WorldToScreenInsideUsableArea(base.MissionScreen.CombatCamera, shipFocusPosition, ref screenX, ref screenY, ref w);
		float resultLength = 0f;
		if (focusableShip.GameEntity.RayHitEntity(cameraFrame.origin, -cameraFrame.rotation.u, friendlyFocusDistance, ref resultLength))
		{
			hitScreenPosition = new Vec3(screenX, screenY, w);
			closestShip = focusableShip;
			directHitFound = true;
			return;
		}
		Vec2 v = new Vec2(screenX, screenY);
		float num = v.Distance(screenCenter);
		if (w > 0f && num < closestDistance && screenCenter.DistanceSquared(v) < focusRadius * focusRadius)
		{
			closestShip = focusableShip;
			closestDistance = num;
			hitScreenPosition = new Vec3(screenX, screenY, w);
		}
	}

	private void SetFocusedShip(MissionShip ship)
	{
		_focusedShip = ship;
		_shipHighlightView?.OnShipFocused(ship);
	}

	private Vec3 GetShipFocusPosition(MissionShip ship)
	{
		return ship.GameEntity.GlobalPosition + Vec3.Up * 3f;
	}

	private void UpdateShipValues()
	{
		if (_dataSource != null)
		{
			_dataSource.IsControllingShip = _playerControlledShip != null;
			_dataSource.IsUsingBallistaRemotely = base.IsAimingWithRangedWeaponAndAllowed;
			_dataSource.IsUsingBallistaDirectly = base.DirectlyControlledRangedSiegeWeapon != null;
			if (base.RangedSiegeWeapon != null || base.DirectlyControlledRangedSiegeWeapon != null)
			{
				_dataSource.BallistaAmmoCount = base.RangedSiegeWeapon?.AmmoCount ?? base.DirectlyControlledRangedSiegeWeapon.AmmoCount;
				_dataSource.IsAmmoCountWarned = _dataSource.BallistaAmmoCount <= 3;
			}
		}
		if (_playerControlledShip == null || base.Mission.Scene == null || _dataSource == null)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = true;
		bool flag4 = true;
		foreach (MissionSail sail in _playerControlledShip.Sails)
		{
			if (sail.SailObject.Type == SailType.Lateen)
			{
				flag = true;
				if (sail.TargetSailSetting <= 0f)
				{
					flag4 = false;
				}
			}
			else if (sail.SailObject.Type == SailType.Square)
			{
				flag2 = true;
				if (sail.TargetSailSetting <= 0f)
				{
					flag3 = false;
				}
			}
		}
		_playerControlledShipHasHybridSails = flag && flag2;
		if (_playerControlledShipHasHybridSails)
		{
			if (flag4 && flag3)
			{
				_dataSource.SetSailState(SailInput.Full);
			}
			else if (!flag4 && !flag3)
			{
				_dataSource.SetSailState(SailInput.Raised);
			}
			else
			{
				_dataSource.SetSailState(SailInput.SquareSailsRaised);
			}
		}
		else if (flag)
		{
			_dataSource.SetSailState(flag4 ? SailInput.Full : SailInput.Raised);
		}
		else
		{
			_dataSource.SetSailState(flag3 ? SailInput.Full : SailInput.Raised);
		}
		_dataSource.SetOarsmanLevel(_playerControlledShip.ShipOrder.OarsmenLevel);
		_dataSource.SetSailType(flag, flag2);
		Vec2 to = base.Mission.Scene.GetGlobalWindStrengthVector().Normalized();
		Vec2 vec = _playerControlledShip.GlobalFrame.rotation.f.AsVec2.Normalized();
		_dataSource.ProjectedWindDirection = GetProjection(vec, to).Normalized();
		UpdateHitPoints();
		MissionShipControlVM dataSource = _dataSource;
		MissionShip playerControlledShip = _playerControlledShip;
		dataSource.IsCutLooseOrderActive = playerControlledShip != null && playerControlledShip.ShipOrder.GetIsCuttingLoose() && _isAnyBridgeActive;
		_dataSource.IsAttemptBoardingOrderActive = _playerControlledShip?.ShipOrder.GetIsAttemptingBoarding() ?? false;
		if (_dataSource.IsAttemptBoardingOrderActive)
		{
			MissionShip missionShip = _playerControlledShip?.ShipOrder.GetBoardingTargetShip();
			if (missionShip != null)
			{
				Vec3 shipFocusPosition = GetShipFocusPosition(missionShip);
				float screenX = -5000f;
				float screenY = -5000f;
				float w = -5000f;
				MBWindowManager.WorldToScreenInsideUsableArea(base.MissionScreen.CombatCamera, shipFocusPosition, ref screenX, ref screenY, ref w);
				_dataSource.SetBoardingTargetShip(missionShip, screenX, screenY - 70f, w);
			}
			else
			{
				_dataSource.SetBoardingTargetShip(null);
			}
		}
		else
		{
			_dataSource.SetBoardingTargetShip(null);
		}
	}

	private static Vec2 GetProjection(Vec2 from, Vec2 to)
	{
		Vec2 vb = from.Normalized();
		return new Vector2(y: Vec2.DotProduct(to, new Vec2(0f - vb.y, vb.x)), x: Vec2.DotProduct(to, vb));
	}

	private void SetControlKeys()
	{
		GameKeyContext category = HotKeyManager.GetCategory("NavalShipControlsHotKeyCategory");
		GameKeyContext category2 = HotKeyManager.GetCategory("CombatHotKeyCategory");
		_dataSource.SetChangeCameraKey(category.GetGameKey(112));
		_dataSource.SetCutLooseKey(category.GetGameKey(111));
		_dataSource.SetToggleOarsmenKey(category.GetGameKey(111));
		_dataSource.SetToggleSailKey(category.GetGameKey(110));
		_dataSource.SetToggleBallistaKey(category.GetGameKey(115));
		_dataSource.SetAttemptBoardingKey(category.GetGameKey(114));
		_dataSource.SetStopUsingShipKey(category2.GetGameKey(13));
	}

	private void RefreshControlKeys()
	{
		if (_playerControlledShip == null || base.MissionScreen.IsPhotoModeEnabled || base.IsDisplayingADialog)
		{
			if (_dataSource != null)
			{
				_dataSource.ChangeCameraKey.IsVisible = false;
				_dataSource.CutLooseKey.IsVisible = false;
				_dataSource.ToggleOarsmenKey.IsVisible = false;
				_dataSource.ToggleSailKey.IsVisible = false;
				_dataSource.ToggleBallistaKey.IsVisible = false;
				_dataSource.AttemptBoardingKey.IsVisible = false;
				_dataSource.StopUsingShipKey.IsVisible = false;
			}
			_agentStatusView?.UpdateShipInteractionTexts(null);
			return;
		}
		if (_dataSource != null)
		{
			_dataSource.ChangeCameraKey.IsVisible = GetCanChangeCamera();
			_dataSource.CutLooseKey.IsVisible = GetCanCutLoose();
			_dataSource.CutLooseKey.IsDisabled = GetIsCutLooseTemporarilyBlocked();
			_dataSource.ToggleOarsmenKey.IsVisible = GetCanToggleOarsmen();
			_dataSource.ToggleSailKey.IsVisible = GetCanToggleSail();
			_dataSource.ToggleBallistaKey.IsVisible = GetCanToggleRangedWeaponOrderMode();
			_dataSource.AttemptBoardingKey.IsVisible = GetCanAttemptBoarding();
			_dataSource.AttemptBoardingKey.IsDisabled = !GetIsCancelBoardingAvailable() && GetIsAttemptBoardingTemporarilyBlocked();
			_dataSource.StopUsingShipKey.IsVisible = true;
		}
		MissionGauntletNavalAgentStatus agentStatusView = _agentStatusView;
		if (agentStatusView != null)
		{
			IShipOrigin origin = _focusedShip?.ShipOrigin;
			MissionShip focusedShip = _focusedShip;
			agentStatusView.UpdateShipInteractionTexts(origin, focusedShip != null && focusedShip.Team?.TeamSide == TeamSideEnum.EnemyTeam, GetCanSelectShip(), GetCanAttemptBoarding(), GetIsAttemptBoardingTemporarilyBlocked(), GetIsCancelBoardingAvailable());
		}
	}

	private bool GetCanAttemptBoarding()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.AttemptBoarding))
		{
			return false;
		}
		if (_focusedShip != null && !_focusedShip.IsConnectionPermanentlyBlocked() && _focusedShip.ShipOrder.IsBoardingAvailable && !_playerControlledShip.GetIsThereActiveBridgeTo(_focusedShip) && (GetIsCancelBoardingAvailable() ? (_focusedShip.GameEntity.GlobalPosition.Distance(_playerControlledShip.GameEntity.GlobalPosition) <= 300f) : (_focusedShip.GameEntity.GlobalPosition.Distance(_playerControlledShip.GameEntity.GlobalPosition) <= 50f)))
		{
			return !base.IsAimingWithRangedWeaponAndAllowed;
		}
		return false;
	}

	private bool GetIsAttemptBoardingTemporarilyBlocked()
	{
		MissionShip focusedShip = _focusedShip;
		if (focusedShip == null || !focusedShip.IsConnectionBlocked())
		{
			return _playerControlledShip.ShipOrder.GetBoardingTargetShip() == _focusedShip;
		}
		return true;
	}

	private bool GetIsCancelBoardingAvailable()
	{
		MissionShip playerControlledShip = _playerControlledShip;
		if (playerControlledShip != null && playerControlledShip.ShipOrder.GetIsAttemptingBoarding())
		{
			return _playerControlledShip.ShipOrder.GetBoardingTargetShip() == _focusedShip;
		}
		return false;
	}

	private bool GetCanChangeCamera()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.ChangeCamera))
		{
			return false;
		}
		return !base.IsAimingWithRangedWeaponAndAllowed;
	}

	private bool GetCanCutLoose()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.CutLoose))
		{
			return false;
		}
		return _isAnyBridgeActive;
	}

	private bool GetIsCutLooseTemporarilyBlocked()
	{
		if (!_playerControlledShip.ShipOrder.GetIsCuttingLoose())
		{
			return _playerControlledShip.IsDisconnectionBlocked();
		}
		return true;
	}

	private bool GetCanSelectShip()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.ShipSelection))
		{
			return false;
		}
		if (_orderUIHandler != null && _focusedShip?.Formation != null && _focusedShip.Formation.CountOfUnits > 0 && _focusedShip.Team.IsPlayerTeam && _focusedShip.Formation.PlayerOwner == Agent.Main && _focusedShip.GameEntity.GlobalPosition.Distance(_playerControlledShip.GameEntity.GlobalPosition) <= 300f)
		{
			return !base.IsAimingWithRangedWeaponAndAllowed;
		}
		return false;
	}

	private bool GetCanToggleOarsmen()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.ToggleOarsmen))
		{
			return false;
		}
		if (!_isAnyBridgeActive)
		{
			return !_playerControlledShip.ShipOrder.IsOarsmenLevelLocked();
		}
		return false;
	}

	private bool GetCanToggleSail()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.ToggleSails))
		{
			return false;
		}
		if (!_isAnyBridgeActive)
		{
			return _playerControlledShip.ShipSailState == MissionShip.SailState.Intact;
		}
		return false;
	}

	private bool GetCanToggleRangedWeaponOrderMode()
	{
		if (GetIsRangedWeaponAvailable())
		{
			return base.IsAimingWithRangedWeaponAllowed;
		}
		return false;
	}

	private bool GetIsRangedWeaponAvailable()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.BallistaOrder))
		{
			return false;
		}
		if (_playerControlledShip.ShipSiegeWeapon != null && !_playerControlledShip.ShipSiegeWeapon.IsDisabled && !_playerControlledShip.ShipSiegeWeapon.IsDeactivated)
		{
			return !_playerControlledShip.ShipSiegeWeapon.IsDestroyed;
		}
		return false;
	}

	private bool GetCanShootBallista()
	{
		if (IsFeatureSuspended(ShipControlFeatureFlags.ShootBallista))
		{
			return false;
		}
		if (base.IsAimingWithRangedWeaponAndAllowed && _playerControlledShip.ShipSiegeWeapon != null)
		{
			return _playerControlledShip.ShipSiegeWeapon.UserCountNotInStruckAction > 0;
		}
		return false;
	}

	private void UpdateVisibility()
	{
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = ((_isBattleUIVisible && !_isPhotoModeActive && !base.IsViewSuspended) ? 1 : 0);
		}
	}
}
