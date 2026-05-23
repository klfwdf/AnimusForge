using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map;

public class MapCameraView : MapView
{
	public enum CameraFollowMode
	{
		Free,
		FollowParty,
		MoveToPosition
	}

	public struct InputInformation
	{
		public bool IsMainPartyValid;

		public bool IsMapReady;

		public bool IsControlDown;

		public bool IsMouseActive;

		public bool CheatModeEnabled;

		public bool LeftMouseButtonPressed;

		public bool LeftMouseButtonDown;

		public bool LeftMouseButtonReleased;

		public bool MiddleMouseButtonDown;

		public bool RightMouseButtonDown;

		public bool RotateLeftKeyDown;

		public bool RotateRightKeyDown;

		public bool PartyMoveUpKey;

		public bool PartyMoveDownKey;

		public bool PartyMoveLeftKey;

		public bool PartyMoveRightKey;

		public bool CameraFollowModeKeyPressed;

		public bool LeftButtonDraggingMode;

		public bool IsInMenu;

		public bool RayCastForClosestEntityOrTerrainCondition;

		public float MapZoomIn;

		public float MapZoomOut;

		public float DeltaMouseScroll;

		public float MouseSensitivity;

		public float MouseMoveX;

		public float MouseMoveY;

		public float HorizontalCameraInput;

		public float RX;

		public float RY;

		public float RS;

		public float Dt;

		public Vec2 MousePositionPixel;

		public Vec2 ClickedPositionPixel;

		public Vec3 ClickedPosition;

		public Vec3 ProjectedPosition;

		public Vec3 WorldMouseNear;

		public Vec3 WorldMouseFar;
	}

	private const float VerticalHalfViewAngle = 0.34906584f;

	private Vec3 _cameraTarget;

	private float _distanceToIdealCameraTargetToStopCameraSoundEventsSquared;

	private int _cameraMoveSfxSoundEventId;

	private SoundEvent _cameraMoveSfxSoundEvent;

	private bool _doFastCameraMovementToTarget;

	private float _cameraElevation;

	private CampaignVec2 _lastUsedIdealCameraTarget;

	private CampaignVec2 _cameraAnimationTarget;

	private float _cameraAnimationStopDuration;

	private readonly Scene _mapScene;

	protected float _customMaximumCameraHeight;

	private MatrixFrame _cameraFrame;

	protected virtual CameraFollowMode CurrentCameraFollowMode { get; set; }

	public virtual float CameraFastMoveMultiplier { get; protected set; }

	protected virtual float CameraBearing { get; set; }

	protected virtual float MaximumCameraHeight => Math.Max(_customMaximumCameraHeight, Campaign.MapMaximumHeight);

	protected virtual float CameraBearingVelocity { get; set; }

	public virtual float CameraDistance { get; protected set; }

	protected virtual float TargetCameraDistance { get; set; }

	protected virtual float AdditionalElevation { get; set; }

	public virtual bool CameraAnimationInProgress { get; protected set; }

	public virtual bool ProcessCameraInput { get; protected set; }

	public virtual Camera Camera { get; protected set; }

	public virtual MatrixFrame CameraFrame
	{
		get
		{
			return _cameraFrame;
		}
		protected set
		{
			_cameraFrame = value;
		}
	}

	protected virtual Vec3 IdealCameraTarget { get; set; }

	private static MapCameraView Instance { get; set; }

	public MapCameraView()
	{
		Camera = Camera.CreateCamera();
		Camera.SetViewVolume(perspective: true, -0.1f, 0.1f, -0.07f, 0.07f, 0.2f, 300f);
		Camera.Position = new Vec3(0f, 0f, 10f);
		CameraBearing = 0f;
		_cameraElevation = 1f;
		CameraDistance = 38f;
		TargetCameraDistance = 38f;
		ProcessCameraInput = true;
		CameraFastMoveMultiplier = 4f;
		_cameraFrame = MatrixFrame.Identity;
		CurrentCameraFollowMode = CameraFollowMode.FollowParty;
		_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
		Instance = this;
	}

	public virtual void OnActivate(bool leftButtonDraggingMode, Vec3 clickedPosition)
	{
		SetCameraMode(CameraFollowMode.FollowParty);
		CameraBearingVelocity = 0f;
		UpdateMapCamera(leftButtonDraggingMode, clickedPosition);
	}

	public virtual void Initialize()
	{
		if (MobileParty.MainParty != null && PartyBase.MainParty.IsValid)
		{
			float height = 0f;
			Campaign.Current.MapSceneWrapper.GetHeightAtPoint(MobileParty.MainParty.Position, ref height);
			IdealCameraTarget = new Vec3(MobileParty.MainParty.Position.ToVec2(), height + 1f);
		}
		_cameraMoveSfxSoundEventId = SoundEvent.GetEventIdFromString("event:/ui/campaign/focus");
		_cameraTarget = IdealCameraTarget;
	}

	protected internal override void OnFinalize()
	{
		base.OnFinalize();
		Instance = null;
	}

	public virtual void SetCameraMode(CameraFollowMode cameraMode)
	{
		CurrentCameraFollowMode = cameraMode;
	}

	public virtual void ResetCamera(bool resetDistance, bool teleportToMainParty)
	{
		if (teleportToMainParty)
		{
			TeleportCameraToMainParty();
		}
		if (resetDistance)
		{
			TargetCameraDistance = 15f;
			CameraDistance = 15f;
		}
		CameraBearing = 0f;
		_cameraElevation = 1f;
	}

	public virtual void TeleportCameraToMainParty()
	{
		CurrentCameraFollowMode = CameraFollowMode.FollowParty;
		Campaign.Current.CameraFollowParty = MobileParty.MainParty.Party;
		IdealCameraTarget = GetCameraTargetForParty(Campaign.Current.CameraFollowParty);
		_lastUsedIdealCameraTarget = new CampaignVec2(IdealCameraTarget.AsVec2, !MobileParty.MainParty.IsCurrentlyAtSea);
		_cameraTarget = IdealCameraTarget;
	}

	public virtual void FastMoveCameraToMainParty()
	{
		CurrentCameraFollowMode = CameraFollowMode.FollowParty;
		Campaign.Current.CameraFollowParty = MobileParty.MainParty.Party;
		IdealCameraTarget = GetCameraTargetForParty(Campaign.Current.CameraFollowParty);
		_doFastCameraMovementToTarget = true;
		TargetCameraDistance = 15f;
		OnFastMoveCameraMovementStart();
	}

	public virtual void FastMoveCameraToPosition(CampaignVec2 target, bool isInMenu)
	{
		if (!isInMenu)
		{
			CurrentCameraFollowMode = CameraFollowMode.MoveToPosition;
			IdealCameraTarget = GetCameraTargetForPosition(target);
			_doFastCameraMovementToTarget = true;
			TargetCameraDistance = 15f;
			OnFastMoveCameraMovementStart();
		}
	}

	public void OnFastMoveCameraMovementStart()
	{
		_distanceToIdealCameraTargetToStopCameraSoundEventsSquared = IdealCameraTarget.DistanceSquared(_cameraTarget) * 0.15f;
		if (_cameraMoveSfxSoundEvent == null || !_cameraMoveSfxSoundEvent.IsPlaying())
		{
			_cameraMoveSfxSoundEvent = SoundEvent.CreateEvent(_cameraMoveSfxSoundEventId, _mapScene);
			_cameraMoveSfxSoundEvent.Play();
		}
	}

	public void StopCameraMovementSoundEvents()
	{
		if (_cameraMoveSfxSoundEvent != null && _cameraMoveSfxSoundEvent.IsPlaying())
		{
			_cameraMoveSfxSoundEvent.Release();
		}
	}

	public virtual bool IsCameraLockedToPlayerParty()
	{
		if (CurrentCameraFollowMode == CameraFollowMode.FollowParty)
		{
			return Campaign.Current.CameraFollowParty == MobileParty.MainParty.Party;
		}
		return false;
	}

	public virtual void StartCameraAnimation(CampaignVec2 targetPosition, float animationStopDuration)
	{
		CameraAnimationInProgress = true;
		_cameraAnimationTarget = targetPosition;
		_cameraAnimationStopDuration = animationStopDuration;
		Campaign.Current.SetTimeSpeed(0);
		Campaign.Current.SetTimeControlModeLock(isLocked: true);
	}

	public virtual void SiegeEngineClick(MatrixFrame siegeEngineFrame)
	{
		if (TargetCameraDistance > 18f)
		{
			TargetCameraDistance = 18f;
		}
	}

	public virtual void OnExit()
	{
		ProcessCameraInput = true;
	}

	public virtual void OnEscapeMenuToggled(bool isOpened)
	{
		ProcessCameraInput = !isOpened;
	}

	public virtual void HandleMouse(bool rightMouseButtonPressed, float verticalCameraInput, float mouseMoveY, float dt)
	{
		float num = 0.3f / 700f;
		float num2 = (0f - (700f - TaleWorlds.Library.MathF.Min(700f, TaleWorlds.Library.MathF.Max(50f, CameraDistance)))) * num;
		float maxValue = TaleWorlds.Library.MathF.Max(num2 + 1E-05f, System.MathF.PI * 99f / 200f - CalculateCameraElevation(CameraDistance));
		if (rightMouseButtonPressed)
		{
			AdditionalElevation = MBMath.ClampFloat(AdditionalElevation + mouseMoveY * 0.0015f, num2, maxValue);
		}
		if (verticalCameraInput != 0f)
		{
			AdditionalElevation = MBMath.ClampFloat(AdditionalElevation - verticalCameraInput * dt, num2, maxValue);
		}
	}

	public virtual void HandleLeftMouseButtonClick(bool isMouseActive)
	{
		if (isMouseActive && !Hero.MainHero.IsPrisoner)
		{
			CurrentCameraFollowMode = CameraFollowMode.FollowParty;
			Campaign.Current.CameraFollowParty = PartyBase.MainParty;
		}
	}

	public virtual void OnSetMapSiegeOverlayState(bool isActive, bool isMapSiegeOverlayViewNull)
	{
		if (isActive && isMapSiegeOverlayViewNull && PlayerSiege.PlayerSiegeEvent != null)
		{
			TargetCameraDistance = 13f;
		}
	}

	public virtual void OnRefreshMapSiegeOverlayRequired(bool isMapSiegeOverlayViewNull)
	{
		if (PlayerSiege.PlayerSiegeEvent != null && isMapSiegeOverlayViewNull)
		{
			TargetCameraDistance = 13f;
		}
	}

	public virtual void OnBeforeTick(in InputInformation inputInformation)
	{
		float num = TaleWorlds.Library.MathF.Min(1f, TaleWorlds.Library.MathF.Max(0f, 1f - CameraFrame.rotation.f.z)) + 0.15f;
		_mapScene.SetDepthOfFieldParameters(0.05f, num * 1000f, isVignetteOn: true);
		_mapScene.SetDepthOfFieldFocus(0.05f);
		MobileParty mainParty = MobileParty.MainParty;
		if (inputInformation.IsMainPartyValid && CameraAnimationInProgress)
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			if (_cameraAnimationStopDuration > 0f)
			{
				if (_cameraAnimationTarget.DistanceSquared(_cameraTarget.AsVec2) < 0.0001f)
				{
					_cameraAnimationStopDuration = TaleWorlds.Library.MathF.Max(_cameraAnimationStopDuration - inputInformation.Dt, 0f);
				}
				else
				{
					IdealCameraTarget = _cameraAnimationTarget.AsVec3() + Vec3.Up;
				}
			}
			else if (MobileParty.MainParty.Position.DistanceSquared(_cameraTarget.AsVec2) < 0.0001f)
			{
				CameraAnimationInProgress = false;
				Campaign.Current.SetTimeControlModeLock(isLocked: false);
			}
			else
			{
				IdealCameraTarget = MobileParty.MainParty.Position.AsVec3() + Vec3.Up;
			}
		}
		bool flag = CameraAnimationInProgress;
		if (ProcessCameraInput && !CameraAnimationInProgress && inputInformation.IsMapReady)
		{
			flag = GetMapCameraInput(inputInformation);
		}
		if (flag)
		{
			Vec3 vec = IdealCameraTarget - _cameraTarget;
			Vec3 vec2 = 10f * vec * inputInformation.Dt;
			float num2 = TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Max(CameraDistance, 20f)) * 0.15f;
			float num3 = (_doFastCameraMovementToTarget ? (num2 * 5f) : num2);
			if (vec2.LengthSquared > num3 * num3)
			{
				vec2 = vec2.NormalizedCopy() * num3;
			}
			if (vec2.LengthSquared < num2 * num2)
			{
				_doFastCameraMovementToTarget = false;
			}
			if (_distanceToIdealCameraTargetToStopCameraSoundEventsSquared > vec.LengthSquared)
			{
				StopCameraMovementSoundEvents();
			}
			_cameraTarget += vec2;
		}
		else
		{
			_cameraTarget = IdealCameraTarget;
			_doFastCameraMovementToTarget = false;
			StopCameraMovementSoundEvents();
		}
		if (inputInformation.IsMainPartyValid)
		{
			if (inputInformation.CameraFollowModeKeyPressed)
			{
				CurrentCameraFollowMode = CameraFollowMode.FollowParty;
			}
			if (!inputInformation.IsInMenu && !inputInformation.MiddleMouseButtonDown && (MobileParty.MainParty == null || MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && (inputInformation.PartyMoveRightKey || inputInformation.PartyMoveLeftKey || inputInformation.PartyMoveUpKey || inputInformation.PartyMoveDownKey))
			{
				float num4 = 0f;
				float num5 = 0f;
				TaleWorlds.Library.MathF.SinCos(CameraBearing, out var sa, out var ca);
				TaleWorlds.Library.MathF.SinCos(CameraBearing + System.MathF.PI / 2f, out var sa2, out var ca2);
				float num6 = 0.5f;
				if (inputInformation.PartyMoveUpKey)
				{
					num5 += ca * num6;
					num4 += sa * num6;
					mainParty.ForceAiNoPathMode = true;
				}
				if (inputInformation.PartyMoveDownKey)
				{
					num5 -= ca * num6;
					num4 -= sa * num6;
					mainParty.ForceAiNoPathMode = true;
				}
				if (inputInformation.PartyMoveLeftKey)
				{
					num5 -= ca2 * num6;
					num4 -= sa2 * num6;
					mainParty.ForceAiNoPathMode = true;
				}
				if (inputInformation.PartyMoveRightKey)
				{
					num5 += ca2 * num6;
					num4 += sa2 * num6;
					mainParty.ForceAiNoPathMode = true;
				}
				CurrentCameraFollowMode = CameraFollowMode.FollowParty;
				CampaignVec2 campaignVec = mainParty.Position + new Vec2(num4, num5);
				if (NavigationHelper.CanPlayerNavigateToPosition(campaignVec, out var _))
				{
					mainParty.SetMoveGoToPoint(campaignVec, mainParty.NavigationCapability);
					Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
				}
			}
			else if (mainParty.ForceAiNoPathMode)
			{
				mainParty.SetMoveGoToPoint(mainParty.Position, mainParty.NavigationCapability);
			}
		}
		UpdateMapCamera(inputInformation.LeftButtonDraggingMode, inputInformation.ClickedPosition);
	}

	protected virtual void UpdateMapCamera(bool _leftButtonDraggingMode, Vec3 _clickedPosition)
	{
		_lastUsedIdealCameraTarget = new CampaignVec2(IdealCameraTarget.AsVec2, isOnLand: true);
		MatrixFrame cameraFrame = ComputeMapCamera(ref _cameraTarget, CameraBearing, _cameraElevation, CameraDistance, ref _lastUsedIdealCameraTarget);
		bool flag = !cameraFrame.origin.NearlyEquals(in _cameraFrame.origin);
		bool flag2 = !cameraFrame.rotation.NearlyEquals(in _cameraFrame.rotation);
		if (flag2 || flag)
		{
			Game.Current.EventManager.TriggerEvent(new MapScreen.MainMapCameraMoveEvent(flag2, flag));
		}
		bool isCurrentlyAtSea = MobileParty.MainParty.IsCurrentlyAtSea;
		_cameraFrame = cameraFrame;
		float height = 0f;
		Campaign.Current.MapSceneWrapper.GetHeightAtPoint(new CampaignVec2(_cameraFrame.origin.AsVec2, !isCurrentlyAtSea), ref height);
		height += 0.5f;
		if (_cameraFrame.origin.z < height)
		{
			if (_leftButtonDraggingMode)
			{
				Vec3 vec = _clickedPosition;
				vec -= Vec3.DotProduct(vec - _cameraFrame.origin, _cameraFrame.rotation.s) * _cameraFrame.rotation.s;
				Vec3 vec2 = Vec3.CrossProduct((vec - _cameraFrame.origin).NormalizedCopy(), (vec - (_cameraFrame.origin + new Vec3(0f, 0f, height - _cameraFrame.origin.z))).NormalizedCopy());
				float a = vec2.Normalize();
				_cameraFrame.origin.z = height;
				_cameraFrame.rotation.u = _cameraFrame.rotation.u.RotateAboutAnArbitraryVector(vec2, a);
				_cameraFrame.rotation.f = Vec3.CrossProduct(_cameraFrame.rotation.u, _cameraFrame.rotation.s).NormalizedCopy();
				_cameraFrame.rotation.s = Vec3.CrossProduct(_cameraFrame.rotation.f, _cameraFrame.rotation.u);
				Vec3 planeNormal = -Vec3.Up;
				Vec3 rayDirection = -_cameraFrame.rotation.u;
				if (MBMath.GetRayPlaneIntersectionPoint(in planeNormal, IdealCameraTarget, in _cameraFrame.origin, in rayDirection, out var t))
				{
					IdealCameraTarget = _cameraFrame.origin + rayDirection * t;
					_cameraTarget = IdealCameraTarget;
				}
				_cameraElevation = 0f - new Vec2(_cameraFrame.rotation.f.AsVec2.Length, _cameraFrame.rotation.f.z).RotationInRadians;
				CameraDistance = (_cameraFrame.origin - IdealCameraTarget).Length - 2f;
				TargetCameraDistance = CameraDistance;
				AdditionalElevation = _cameraElevation - CalculateCameraElevation(CameraDistance);
				_lastUsedIdealCameraTarget = new CampaignVec2(IdealCameraTarget.AsVec2, isOnLand: true);
				ComputeMapCamera(ref _cameraTarget, CameraBearing, _cameraElevation, CameraDistance, ref _lastUsedIdealCameraTarget);
			}
			else
			{
				float num = 0.47123894f;
				int num2 = 0;
				do
				{
					_cameraElevation += ((_cameraFrame.origin.z < height) ? num : (0f - num));
					float num3 = (700f - TaleWorlds.Library.MathF.Min(700f, TaleWorlds.Library.MathF.Max(50f, CameraDistance))) * -1f * 0.00042857145f;
					float maxValue = TaleWorlds.Library.MathF.Max(num3 + 1E-05f, System.MathF.PI * 99f / 200f - CalculateCameraElevation(CameraDistance));
					AdditionalElevation = _cameraElevation - CalculateCameraElevation(CameraDistance);
					AdditionalElevation = MBMath.ClampFloat(AdditionalElevation, num3, maxValue);
					_cameraElevation = AdditionalElevation + CalculateCameraElevation(CameraDistance);
					CampaignVec2 lastUsedIdealCameraTarget = CampaignVec2.Zero;
					_cameraFrame = ComputeMapCamera(ref _cameraTarget, CameraBearing, _cameraElevation, CameraDistance, ref lastUsedIdealCameraTarget);
					Campaign.Current.MapSceneWrapper.GetHeightAtPoint(new CampaignVec2(_cameraFrame.origin.AsVec2, !isCurrentlyAtSea), ref height);
					height += 0.5f;
					if (num > 0.0001f)
					{
						num *= 0.5f;
					}
					else
					{
						num2++;
					}
				}
				while (num > 0.0001f || (_cameraFrame.origin.z < height && num2 < 5));
				if (_cameraFrame.origin.z < height)
				{
					_cameraFrame.origin.z = height;
					Vec3 planeNormal2 = -Vec3.Up;
					Vec3 rayDirection2 = -_cameraFrame.rotation.u;
					if (MBMath.GetRayPlaneIntersectionPoint(in planeNormal2, IdealCameraTarget, in _cameraFrame.origin, in rayDirection2, out var t2) && CurrentCameraFollowMode != CameraFollowMode.MoveToPosition)
					{
						IdealCameraTarget = _cameraFrame.origin + rayDirection2 * t2;
						_cameraTarget = IdealCameraTarget;
						CameraDistance = (_cameraFrame.origin - IdealCameraTarget).Length - 2f;
					}
					_lastUsedIdealCameraTarget = new CampaignVec2(IdealCameraTarget.AsVec2, isOnLand: true);
					ComputeMapCamera(ref _cameraTarget, CameraBearing, _cameraElevation, CameraDistance, ref _lastUsedIdealCameraTarget);
					TargetCameraDistance = TaleWorlds.Library.MathF.Max(TargetCameraDistance, CameraDistance);
				}
			}
		}
		Camera.Frame = _cameraFrame;
		Camera.SetFovVertical(0.6981317f, Screen.AspectRatio, 0.01f, MaximumCameraHeight * 4f);
		_mapScene.SetDepthOfFieldFocus(0f);
		_mapScene.SetDepthOfFieldParameters(0f, 0f, isVignetteOn: false);
		MatrixFrame identity = MatrixFrame.Identity;
		identity.rotation = _cameraFrame.rotation;
		identity.origin = _cameraTarget;
		Campaign.Current.MapSceneWrapper.GetHeightAtPoint(new CampaignVec2(identity.origin.AsVec2, isOnLand: true), ref identity.origin.z);
		identity.origin = MBMath.Lerp(identity.origin, _cameraFrame.origin, 0.075f, 1E-05f);
		PathFaceRecord face = new CampaignVec2(identity.origin.AsVec2, isOnLand: true).Face;
		if (!face.IsValid())
		{
			face = new CampaignVec2(identity.origin.AsVec2, isOnLand: false).Face;
		}
		if (face.IsValid())
		{
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
			MBMapScene.TickAmbientSounds(_mapScene, (int)faceTerrainType);
		}
		SoundManager.SetListenerFrame(identity);
	}

	protected virtual Vec3 GetCameraTargetForPosition(CampaignVec2 targetPosition)
	{
		return targetPosition.AsVec3() + Vec3.Up;
	}

	protected virtual Vec3 GetCameraTargetForParty(PartyBase party)
	{
		CampaignVec2 zero = CampaignVec2.Zero;
		if (party.IsMobile && party.MobileParty.CurrentSettlement != null)
		{
			zero = party.MobileParty.CurrentSettlement.Position;
		}
		else if (party.IsMobile && party.MobileParty.BesiegedSettlement != null)
		{
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				Vec2 asVec = party.MobileParty.BesiegedSettlement.Town.BesiegerCampPositions1.First().origin.AsVec2;
				Vec2 pos = Vec2.Lerp(party.MobileParty.TargetPosition.ToVec2(), asVec, 0.75f);
				zero = new CampaignVec2(pos, zero.IsOnLand);
			}
			else
			{
				zero = party.MobileParty.TargetPosition;
			}
		}
		else
		{
			zero = party.Position;
		}
		return GetCameraTargetForPosition(zero);
	}

	protected virtual bool GetMapCameraInput(InputInformation inputInformation)
	{
		bool flag = false;
		bool result = !inputInformation.LeftButtonDraggingMode;
		if (inputInformation.IsControlDown && inputInformation.CheatModeEnabled)
		{
			flag = true;
			if (inputInformation.DeltaMouseScroll > 0.01f)
			{
				CameraFastMoveMultiplier *= 1.25f;
			}
			else if (inputInformation.DeltaMouseScroll < -0.01f)
			{
				CameraFastMoveMultiplier *= 0.8f;
			}
			CameraFastMoveMultiplier = MBMath.ClampFloat(CameraFastMoveMultiplier, 1f, 37.252903f);
		}
		Vec2 vec = Vec2.Zero;
		if (!inputInformation.LeftMouseButtonPressed && inputInformation.LeftMouseButtonDown && !inputInformation.LeftMouseButtonReleased && inputInformation.MousePositionPixel.DistanceSquared(inputInformation.ClickedPositionPixel) > 300f && !inputInformation.IsInMenu)
		{
			if (!inputInformation.LeftButtonDraggingMode)
			{
				IdealCameraTarget = _cameraTarget;
				_lastUsedIdealCameraTarget = new CampaignVec2(IdealCameraTarget.AsVec2, isOnLand: true);
			}
			Vec3 rayDirection = (inputInformation.WorldMouseFar - inputInformation.WorldMouseNear).NormalizedCopy();
			if (MBMath.GetRayPlaneIntersectionPoint(-Vec3.Up, in inputInformation.ClickedPosition, in inputInformation.WorldMouseNear, in rayDirection, out var t))
			{
				CurrentCameraFollowMode = CameraFollowMode.Free;
				Vec3 vec2 = inputInformation.WorldMouseNear + rayDirection * t;
				vec = inputInformation.ClickedPosition.AsVec2 - vec2.AsVec2;
			}
		}
		if (inputInformation.MiddleMouseButtonDown)
		{
			TargetCameraDistance += 0.01f * (CameraDistance + 20f) * inputInformation.MouseSensitivity * inputInformation.MouseMoveY;
		}
		if (inputInformation.RotateLeftKeyDown)
		{
			CameraBearingVelocity = inputInformation.Dt * 2f;
		}
		else if (inputInformation.RotateRightKeyDown)
		{
			CameraBearingVelocity = inputInformation.Dt * -2f;
		}
		CameraBearingVelocity += inputInformation.HorizontalCameraInput * 1.75f * inputInformation.Dt;
		if (inputInformation.RightMouseButtonDown)
		{
			CameraBearingVelocity += 0.01f * inputInformation.MouseSensitivity * inputInformation.MouseMoveX;
		}
		float num = 0.1f;
		if (!inputInformation.IsMouseActive)
		{
			num *= inputInformation.Dt * 10f;
		}
		if (!flag)
		{
			TargetCameraDistance -= inputInformation.MapZoomIn * num * (CameraDistance + 20f);
			TargetCameraDistance += inputInformation.MapZoomOut * num * (CameraDistance + 20f);
		}
		PartyBase cameraFollowParty = Campaign.Current.CameraFollowParty;
		TargetCameraDistance = MBMath.ClampFloat(TargetCameraDistance, 2.5f, (cameraFollowParty != null && cameraFollowParty.IsMobile && (cameraFollowParty.MobileParty.BesiegedSettlement != null || (cameraFollowParty.MobileParty.CurrentSettlement != null && cameraFollowParty.MobileParty.CurrentSettlement.IsUnderSiege))) ? 30f : MaximumCameraHeight);
		float num2 = TargetCameraDistance - CameraDistance;
		float num3 = TaleWorlds.Library.MathF.Abs(num2);
		float cameraDistance = ((num3 > 0.001f) ? (CameraDistance + num2 * inputInformation.Dt * 8f) : TargetCameraDistance);
		if (CurrentCameraFollowMode == CameraFollowMode.Free && !inputInformation.RightMouseButtonDown && !inputInformation.LeftMouseButtonDown && num3 >= 0.001f && (inputInformation.WorldMouseFar - CameraFrame.origin).NormalizedCopy().z < -0.2f && inputInformation.RayCastForClosestEntityOrTerrainCondition)
		{
			MatrixFrame matrixFrame = ComputeMapCamera(ref _cameraTarget, CameraBearing + CameraBearingVelocity, TaleWorlds.Library.MathF.Min(CalculateCameraElevation(cameraDistance) + AdditionalElevation, System.MathF.PI * 99f / 200f), cameraDistance, ref _lastUsedIdealCameraTarget);
			Vec3 planeNormal = -Vec3.Up;
			Vec3 v = (inputInformation.WorldMouseFar - CameraFrame.origin).NormalizedCopy();
			Vec3 rayDirection2 = matrixFrame.rotation.TransformToParent(CameraFrame.rotation.TransformToLocal(in v));
			if (MBMath.GetRayPlaneIntersectionPoint(in planeNormal, in inputInformation.ProjectedPosition, in matrixFrame.origin, in rayDirection2, out var t2))
			{
				vec = inputInformation.ProjectedPosition.AsVec2 - (matrixFrame.origin + rayDirection2 * t2).AsVec2;
				result = false;
			}
		}
		if (inputInformation.RX != 0f || inputInformation.RY != 0f || vec.IsNonZero())
		{
			float num4 = 0.001f * (CameraDistance * 0.55f + 15f);
			Vec2 vec3 = Vec2.FromRotation(0f - CameraBearing);
			if ((IdealCameraTarget.AsVec2 - _lastUsedIdealCameraTarget.ToVec2()).LengthSquared > 0.010000001f)
			{
				IdealCameraTarget = _lastUsedIdealCameraTarget.AsVec3();
				_cameraTarget = IdealCameraTarget;
			}
			if (!vec.IsNonZero())
			{
				IdealCameraTarget = _cameraTarget;
			}
			Vec2 vec4 = inputInformation.Dt * 500f * inputInformation.RX * vec3.RightVec() * num4 + inputInformation.Dt * 500f * inputInformation.RY * vec3 * num4;
			IdealCameraTarget = new Vec3(IdealCameraTarget.x + vec.x + vec4.x, IdealCameraTarget.y + vec.y + vec4.y, IdealCameraTarget.z);
			if (vec.IsNonZero())
			{
				_cameraTarget = IdealCameraTarget;
			}
			_cameraTarget.AsVec2 += vec4;
			if (inputInformation.RX != 0f || inputInformation.RY != 0f)
			{
				CurrentCameraFollowMode = CameraFollowMode.Free;
			}
		}
		CameraBearing += CameraBearingVelocity;
		CameraBearingVelocity = 0f;
		CameraDistance = cameraDistance;
		_cameraElevation = TaleWorlds.Library.MathF.Min(CalculateCameraElevation(cameraDistance) + AdditionalElevation, System.MathF.PI * 99f / 200f);
		if (CurrentCameraFollowMode == CameraFollowMode.FollowParty && cameraFollowParty != null && cameraFollowParty.IsValid)
		{
			bool flag2 = false;
			Vec2 pos;
			if (cameraFollowParty.IsMobile)
			{
				Settlement settlement = cameraFollowParty.MobileParty.CurrentSettlement ?? cameraFollowParty.MobileParty.BesiegedSettlement ?? cameraFollowParty.MapEvent?.MapEventSettlement;
				if (settlement != null && cameraFollowParty.MobileParty.IsMainParty)
				{
					pos = settlement.Position.ToVec2();
					if (settlement.HasPort)
					{
						pos += settlement.PortPosition.ToVec2();
						if (settlement.IsUnderSiege)
						{
							pos += settlement.SiegeEvent.BesiegerCamp.LeaderParty.Position.ToVec2();
							pos /= 3f;
						}
						else
						{
							pos *= 0.5f;
						}
					}
				}
				else
				{
					pos = ((cameraFollowParty.MapEvent != null) ? cameraFollowParty.MapEvent.Position.ToVec2() : cameraFollowParty.Position.ToVec2());
				}
				flag2 = !cameraFollowParty.MobileParty.IsCurrentlyAtSea;
			}
			else
			{
				pos = cameraFollowParty.Position.ToVec2();
				flag2 = true;
			}
			float height = 0f;
			Campaign.Current.MapSceneWrapper.GetHeightAtPoint(new CampaignVec2(pos, flag2), ref height);
			IdealCameraTarget = new Vec3(pos.X, pos.Y, height + 1f);
		}
		return result;
	}

	protected virtual MatrixFrame ComputeMapCamera(ref Vec3 cameraTarget, float cameraBearing, float cameraElevation, float cameraDistance, ref CampaignVec2 lastUsedIdealCameraTarget)
	{
		Vec2 asVec = cameraTarget.AsVec2;
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = cameraTarget;
		identity.rotation.RotateAboutSide(System.MathF.PI / 2f);
		identity.rotation.RotateAboutForward(0f - cameraBearing);
		identity.rotation.RotateAboutSide(0f - cameraElevation);
		identity.origin += identity.rotation.u * (cameraDistance + 2f);
		Vec2 vec = (Campaign.MapMinimumPosition + Campaign.MapMaximumPosition) * 0.5f;
		float num = Campaign.MapMaximumPosition.y - vec.y;
		float num2 = Campaign.MapMaximumPosition.x - vec.x;
		asVec.x = MBMath.ClampFloat(asVec.x, vec.x - num2, vec.x + num2);
		asVec.y = MBMath.ClampFloat(asVec.y, vec.y - num, vec.y + num);
		float a = MBMath.ClampFloat(lastUsedIdealCameraTarget.X, vec.x - num2, vec.x + num2);
		float b = MBMath.ClampFloat(lastUsedIdealCameraTarget.Y, vec.y - num, vec.y + num);
		lastUsedIdealCameraTarget = new CampaignVec2(new Vec2(a, b), lastUsedIdealCameraTarget.IsOnLand);
		identity.origin.x += asVec.x - cameraTarget.x;
		identity.origin.y += asVec.y - cameraTarget.y;
		return identity;
	}

	protected virtual float CalculateCameraElevation(float cameraDistance)
	{
		return cameraDistance * 0.0075f + 0.35f;
	}
}
