using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map
{
	// Token: 0x02000043 RID: 67
	public class MapCameraView : MapView
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600021B RID: 539 RVA: 0x00014348 File Offset: 0x00012548
		// (set) Token: 0x0600021C RID: 540 RVA: 0x00014350 File Offset: 0x00012550
		protected virtual MapCameraView.CameraFollowMode CurrentCameraFollowMode { get; set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600021D RID: 541 RVA: 0x00014359 File Offset: 0x00012559
		// (set) Token: 0x0600021E RID: 542 RVA: 0x00014361 File Offset: 0x00012561
		public virtual float CameraFastMoveMultiplier { get; protected set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600021F RID: 543 RVA: 0x0001436A File Offset: 0x0001256A
		// (set) Token: 0x06000220 RID: 544 RVA: 0x00014372 File Offset: 0x00012572
		protected virtual float CameraBearing { get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000221 RID: 545 RVA: 0x0001437B File Offset: 0x0001257B
		protected virtual float MaximumCameraHeight
		{
			get
			{
				return Math.Max(this._customMaximumCameraHeight, Campaign.MapMaximumHeight);
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000222 RID: 546 RVA: 0x0001438D File Offset: 0x0001258D
		// (set) Token: 0x06000223 RID: 547 RVA: 0x00014395 File Offset: 0x00012595
		protected virtual float CameraBearingVelocity { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000224 RID: 548 RVA: 0x0001439E File Offset: 0x0001259E
		// (set) Token: 0x06000225 RID: 549 RVA: 0x000143A6 File Offset: 0x000125A6
		public virtual float CameraDistance { get; protected set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000226 RID: 550 RVA: 0x000143AF File Offset: 0x000125AF
		// (set) Token: 0x06000227 RID: 551 RVA: 0x000143B7 File Offset: 0x000125B7
		protected virtual float TargetCameraDistance { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000228 RID: 552 RVA: 0x000143C0 File Offset: 0x000125C0
		// (set) Token: 0x06000229 RID: 553 RVA: 0x000143C8 File Offset: 0x000125C8
		protected virtual float AdditionalElevation { get; set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x0600022A RID: 554 RVA: 0x000143D1 File Offset: 0x000125D1
		// (set) Token: 0x0600022B RID: 555 RVA: 0x000143D9 File Offset: 0x000125D9
		public virtual bool CameraAnimationInProgress { get; protected set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x0600022C RID: 556 RVA: 0x000143E2 File Offset: 0x000125E2
		// (set) Token: 0x0600022D RID: 557 RVA: 0x000143EA File Offset: 0x000125EA
		public virtual bool ProcessCameraInput { get; protected set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x0600022E RID: 558 RVA: 0x000143F3 File Offset: 0x000125F3
		// (set) Token: 0x0600022F RID: 559 RVA: 0x000143FB File Offset: 0x000125FB
		public virtual Camera Camera { get; protected set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000230 RID: 560 RVA: 0x00014404 File Offset: 0x00012604
		// (set) Token: 0x06000231 RID: 561 RVA: 0x0001440C File Offset: 0x0001260C
		public virtual MatrixFrame CameraFrame
		{
			get
			{
				return this._cameraFrame;
			}
			protected set
			{
				this._cameraFrame = value;
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000232 RID: 562 RVA: 0x00014415 File Offset: 0x00012615
		// (set) Token: 0x06000233 RID: 563 RVA: 0x0001441D File Offset: 0x0001261D
		protected virtual Vec3 IdealCameraTarget { get; set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000234 RID: 564 RVA: 0x00014426 File Offset: 0x00012626
		// (set) Token: 0x06000235 RID: 565 RVA: 0x0001442D File Offset: 0x0001262D
		private static MapCameraView Instance { get; set; }

		// Token: 0x06000236 RID: 566 RVA: 0x00014438 File Offset: 0x00012638
		public MapCameraView()
		{
			this.Camera = Camera.CreateCamera();
			this.Camera.SetViewVolume(true, -0.1f, 0.1f, -0.07f, 0.07f, 0.2f, 300f);
			this.Camera.Position = new Vec3(0f, 0f, 10f, -1f);
			this.CameraBearing = 0f;
			this._cameraElevation = 1f;
			this.CameraDistance = 38f;
			this.TargetCameraDistance = 38f;
			this.ProcessCameraInput = true;
			this.CameraFastMoveMultiplier = 4f;
			this._cameraFrame = MatrixFrame.Identity;
			this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.FollowParty;
			this._mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			MapCameraView.Instance = this;
		}

		// Token: 0x06000237 RID: 567 RVA: 0x00014514 File Offset: 0x00012714
		public virtual void OnActivate(bool leftButtonDraggingMode, Vec3 clickedPosition)
		{
			this.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
			this.CameraBearingVelocity = 0f;
			this.UpdateMapCamera(leftButtonDraggingMode, clickedPosition);
		}

		// Token: 0x06000238 RID: 568 RVA: 0x00014530 File Offset: 0x00012730
		public virtual void Initialize()
		{
			if (MobileParty.MainParty != null && PartyBase.MainParty.IsValid)
			{
				float num = 0f;
				IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
				CampaignVec2 position = MobileParty.MainParty.Position;
				mapSceneWrapper.GetHeightAtPoint(position, ref num);
				position = MobileParty.MainParty.Position;
				this.IdealCameraTarget = new Vec3(position.ToVec2(), num + 1f, -1f);
			}
			this._cameraMoveSfxSoundEventId = SoundEvent.GetEventIdFromString("event:/ui/campaign/focus");
			this._cameraTarget = this.IdealCameraTarget;
		}

		// Token: 0x06000239 RID: 569 RVA: 0x000145BA File Offset: 0x000127BA
		protected internal override void OnFinalize()
		{
			base.OnFinalize();
			MapCameraView.Instance = null;
		}

		// Token: 0x0600023A RID: 570 RVA: 0x000145C8 File Offset: 0x000127C8
		public virtual void SetCameraMode(MapCameraView.CameraFollowMode cameraMode)
		{
			this.CurrentCameraFollowMode = cameraMode;
		}

		// Token: 0x0600023B RID: 571 RVA: 0x000145D1 File Offset: 0x000127D1
		public virtual void ResetCamera(bool resetDistance, bool teleportToMainParty)
		{
			if (teleportToMainParty)
			{
				this.TeleportCameraToMainParty();
			}
			if (resetDistance)
			{
				this.TargetCameraDistance = 15f;
				this.CameraDistance = 15f;
			}
			this.CameraBearing = 0f;
			this._cameraElevation = 1f;
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0001460C File Offset: 0x0001280C
		public virtual void TeleportCameraToMainParty()
		{
			this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.FollowParty;
			Campaign.Current.CameraFollowParty = MobileParty.MainParty.Party;
			this.IdealCameraTarget = this.GetCameraTargetForParty(Campaign.Current.CameraFollowParty);
			this._lastUsedIdealCameraTarget = new CampaignVec2(this.IdealCameraTarget.AsVec2, !MobileParty.MainParty.IsCurrentlyAtSea);
			this._cameraTarget = this.IdealCameraTarget;
		}

		// Token: 0x0600023D RID: 573 RVA: 0x0001467C File Offset: 0x0001287C
		public virtual void FastMoveCameraToMainParty()
		{
			this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.FollowParty;
			Campaign.Current.CameraFollowParty = MobileParty.MainParty.Party;
			this.IdealCameraTarget = this.GetCameraTargetForParty(Campaign.Current.CameraFollowParty);
			this._doFastCameraMovementToTarget = true;
			this.TargetCameraDistance = 15f;
			this.OnFastMoveCameraMovementStart();
		}

		// Token: 0x0600023E RID: 574 RVA: 0x000146D2 File Offset: 0x000128D2
		public virtual void FastMoveCameraToPosition(CampaignVec2 target, bool isInMenu)
		{
			if (!isInMenu)
			{
				this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.MoveToPosition;
				this.IdealCameraTarget = this.GetCameraTargetForPosition(target);
				this._doFastCameraMovementToTarget = true;
				this.TargetCameraDistance = 15f;
				this.OnFastMoveCameraMovementStart();
			}
		}

		// Token: 0x0600023F RID: 575 RVA: 0x00014704 File Offset: 0x00012904
		public void OnFastMoveCameraMovementStart()
		{
			this._distanceToIdealCameraTargetToStopCameraSoundEventsSquared = this.IdealCameraTarget.DistanceSquared(this._cameraTarget) * 0.15f;
			if (this._cameraMoveSfxSoundEvent == null || !this._cameraMoveSfxSoundEvent.IsPlaying())
			{
				this._cameraMoveSfxSoundEvent = SoundEvent.CreateEvent(this._cameraMoveSfxSoundEventId, this._mapScene);
				this._cameraMoveSfxSoundEvent.Play();
			}
		}

		// Token: 0x06000240 RID: 576 RVA: 0x00014769 File Offset: 0x00012969
		public void StopCameraMovementSoundEvents()
		{
			if (this._cameraMoveSfxSoundEvent != null && this._cameraMoveSfxSoundEvent.IsPlaying())
			{
				this._cameraMoveSfxSoundEvent.Release();
			}
		}

		// Token: 0x06000241 RID: 577 RVA: 0x0001478B File Offset: 0x0001298B
		public virtual bool IsCameraLockedToPlayerParty()
		{
			return this.CurrentCameraFollowMode == MapCameraView.CameraFollowMode.FollowParty && Campaign.Current.CameraFollowParty == MobileParty.MainParty.Party;
		}

		// Token: 0x06000242 RID: 578 RVA: 0x000147AE File Offset: 0x000129AE
		public virtual void StartCameraAnimation(CampaignVec2 targetPosition, float animationStopDuration)
		{
			this.CameraAnimationInProgress = true;
			this._cameraAnimationTarget = targetPosition;
			this._cameraAnimationStopDuration = animationStopDuration;
			Campaign.Current.SetTimeSpeed(0);
			Campaign.Current.SetTimeControlModeLock(true);
		}

		// Token: 0x06000243 RID: 579 RVA: 0x000147DB File Offset: 0x000129DB
		public virtual void SiegeEngineClick(MatrixFrame siegeEngineFrame)
		{
			if (this.TargetCameraDistance > 18f)
			{
				this.TargetCameraDistance = 18f;
			}
		}

		// Token: 0x06000244 RID: 580 RVA: 0x000147F5 File Offset: 0x000129F5
		public virtual void OnExit()
		{
			this.ProcessCameraInput = true;
		}

		// Token: 0x06000245 RID: 581 RVA: 0x000147FE File Offset: 0x000129FE
		public virtual void OnEscapeMenuToggled(bool isOpened)
		{
			this.ProcessCameraInput = !isOpened;
		}

		// Token: 0x06000246 RID: 582 RVA: 0x0001480C File Offset: 0x00012A0C
		public virtual void HandleMouse(bool rightMouseButtonPressed, float verticalCameraInput, float mouseMoveY, float dt)
		{
			float num = 0.3f / 700f;
			float num2 = -(700f - MathF.Min(700f, MathF.Max(50f, this.CameraDistance))) * num;
			float maxValue = MathF.Max(num2 + 1E-05f, 1.5550884f - this.CalculateCameraElevation(this.CameraDistance));
			if (rightMouseButtonPressed)
			{
				this.AdditionalElevation = MBMath.ClampFloat(this.AdditionalElevation + mouseMoveY * 0.0015f, num2, maxValue);
			}
			if (verticalCameraInput != 0f)
			{
				this.AdditionalElevation = MBMath.ClampFloat(this.AdditionalElevation - verticalCameraInput * dt, num2, maxValue);
			}
		}

		// Token: 0x06000247 RID: 583 RVA: 0x000148A6 File Offset: 0x00012AA6
		public virtual void HandleLeftMouseButtonClick(bool isMouseActive)
		{
			if (isMouseActive && !Hero.MainHero.IsPrisoner)
			{
				this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.FollowParty;
				Campaign.Current.CameraFollowParty = PartyBase.MainParty;
			}
		}

		// Token: 0x06000248 RID: 584 RVA: 0x000148CD File Offset: 0x00012ACD
		public virtual void OnSetMapSiegeOverlayState(bool isActive, bool isMapSiegeOverlayViewNull)
		{
			if (isActive && isMapSiegeOverlayViewNull && PlayerSiege.PlayerSiegeEvent != null)
			{
				this.TargetCameraDistance = 13f;
			}
		}

		// Token: 0x06000249 RID: 585 RVA: 0x000148E6 File Offset: 0x00012AE6
		public virtual void OnRefreshMapSiegeOverlayRequired(bool isMapSiegeOverlayViewNull)
		{
			if (PlayerSiege.PlayerSiegeEvent != null && isMapSiegeOverlayViewNull)
			{
				this.TargetCameraDistance = 13f;
			}
		}

		// Token: 0x0600024A RID: 586 RVA: 0x00014900 File Offset: 0x00012B00
		public virtual void OnBeforeTick(in MapCameraView.InputInformation inputInformation)
		{
			float num = MathF.Min(1f, MathF.Max(0f, 1f - this.CameraFrame.rotation.f.z)) + 0.15f;
			this._mapScene.SetDepthOfFieldParameters(0.05f, num * 1000f, true);
			this._mapScene.SetDepthOfFieldFocus(0.05f);
			MobileParty mainParty = MobileParty.MainParty;
			if (inputInformation.IsMainPartyValid && this.CameraAnimationInProgress)
			{
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				if (this._cameraAnimationStopDuration > 0f)
				{
					if (this._cameraAnimationTarget.DistanceSquared(this._cameraTarget.AsVec2) < 0.0001f)
					{
						this._cameraAnimationStopDuration = MathF.Max(this._cameraAnimationStopDuration - inputInformation.Dt, 0f);
					}
					else
					{
						this.IdealCameraTarget = this._cameraAnimationTarget.AsVec3() + Vec3.Up;
					}
				}
				else if (MobileParty.MainParty.Position.DistanceSquared(this._cameraTarget.AsVec2) < 0.0001f)
				{
					this.CameraAnimationInProgress = false;
					Campaign.Current.SetTimeControlModeLock(false);
				}
				else
				{
					this.IdealCameraTarget = MobileParty.MainParty.Position.AsVec3() + Vec3.Up;
				}
			}
			bool flag = this.CameraAnimationInProgress;
			if (this.ProcessCameraInput && !this.CameraAnimationInProgress && inputInformation.IsMapReady)
			{
				flag = this.GetMapCameraInput(inputInformation);
			}
			if (flag)
			{
				Vec3 v = this.IdealCameraTarget - this._cameraTarget;
				Vec3 v2 = 10f * v * inputInformation.Dt;
				float num2 = MathF.Sqrt(MathF.Max(this.CameraDistance, 20f)) * 0.15f;
				float num3 = (this._doFastCameraMovementToTarget ? (num2 * 5f) : num2);
				if (v2.LengthSquared > num3 * num3)
				{
					v2 = v2.NormalizedCopy() * num3;
				}
				if (v2.LengthSquared < num2 * num2)
				{
					this._doFastCameraMovementToTarget = false;
				}
				if (this._distanceToIdealCameraTargetToStopCameraSoundEventsSquared > v.LengthSquared)
				{
					this.StopCameraMovementSoundEvents();
				}
				this._cameraTarget += v2;
			}
			else
			{
				this._cameraTarget = this.IdealCameraTarget;
				this._doFastCameraMovementToTarget = false;
				this.StopCameraMovementSoundEvents();
			}
			if (inputInformation.IsMainPartyValid)
			{
				if (inputInformation.CameraFollowModeKeyPressed)
				{
					this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.FollowParty;
				}
				if (!inputInformation.IsInMenu && !inputInformation.MiddleMouseButtonDown && (MobileParty.MainParty == null || MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && (inputInformation.PartyMoveRightKey || inputInformation.PartyMoveLeftKey || inputInformation.PartyMoveUpKey || inputInformation.PartyMoveDownKey))
				{
					float num4 = 0f;
					float num5 = 0f;
					float num6;
					float num7;
					MathF.SinCos(this.CameraBearing, out num6, out num7);
					float num8;
					float num9;
					MathF.SinCos(this.CameraBearing + 1.5707964f, out num8, out num9);
					float num10 = 0.5f;
					if (inputInformation.PartyMoveUpKey)
					{
						num5 += num7 * num10;
						num4 += num6 * num10;
						mainParty.ForceAiNoPathMode = true;
					}
					if (inputInformation.PartyMoveDownKey)
					{
						num5 -= num7 * num10;
						num4 -= num6 * num10;
						mainParty.ForceAiNoPathMode = true;
					}
					if (inputInformation.PartyMoveLeftKey)
					{
						num5 -= num9 * num10;
						num4 -= num8 * num10;
						mainParty.ForceAiNoPathMode = true;
					}
					if (inputInformation.PartyMoveRightKey)
					{
						num5 += num9 * num10;
						num4 += num8 * num10;
						mainParty.ForceAiNoPathMode = true;
					}
					this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.FollowParty;
					CampaignVec2 campaignVec = mainParty.Position + new Vec2(num4, num5);
					MobileParty.NavigationType navigationType;
					NavigationHelper.CanPlayerNavigateToPosition(campaignVec, out navigationType);
					if (navigationType != MobileParty.NavigationType.None && navigationType != MobileParty.NavigationType.All)
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
			this.UpdateMapCamera(inputInformation.LeftButtonDraggingMode, inputInformation.ClickedPosition);
		}

		// Token: 0x0600024B RID: 587 RVA: 0x00014D14 File Offset: 0x00012F14
		protected virtual void UpdateMapCamera(bool _leftButtonDraggingMode, Vec3 _clickedPosition)
		{
			this._lastUsedIdealCameraTarget = new CampaignVec2(this.IdealCameraTarget.AsVec2, true);
			MatrixFrame cameraFrame = this.ComputeMapCamera(ref this._cameraTarget, this.CameraBearing, this._cameraElevation, this.CameraDistance, ref this._lastUsedIdealCameraTarget);
			bool flag = !cameraFrame.origin.NearlyEquals(this._cameraFrame.origin, 1E-05f);
			bool flag2 = !cameraFrame.rotation.NearlyEquals(this._cameraFrame.rotation, 1E-05f);
			if (flag2 || flag)
			{
				Game.Current.EventManager.TriggerEvent<MapScreen.MainMapCameraMoveEvent>(new MapScreen.MainMapCameraMoveEvent(flag2, flag));
			}
			bool isCurrentlyAtSea = MobileParty.MainParty.IsCurrentlyAtSea;
			this._cameraFrame = cameraFrame;
			float num = 0f;
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			CampaignVec2 campaignVec = new CampaignVec2(this._cameraFrame.origin.AsVec2, !isCurrentlyAtSea);
			mapSceneWrapper.GetHeightAtPoint(campaignVec, ref num);
			num += 0.5f;
			if (this._cameraFrame.origin.z < num)
			{
				if (_leftButtonDraggingMode)
				{
					Vec3 v = _clickedPosition - Vec3.DotProduct(_clickedPosition - this._cameraFrame.origin, this._cameraFrame.rotation.s) * this._cameraFrame.rotation.s;
					Vec3 vec = Vec3.CrossProduct((v - this._cameraFrame.origin).NormalizedCopy(), (v - (this._cameraFrame.origin + new Vec3(0f, 0f, num - this._cameraFrame.origin.z, -1f))).NormalizedCopy());
					float a = vec.Normalize();
					this._cameraFrame.origin.z = num;
					this._cameraFrame.rotation.u = this._cameraFrame.rotation.u.RotateAboutAnArbitraryVector(vec, a);
					this._cameraFrame.rotation.f = Vec3.CrossProduct(this._cameraFrame.rotation.u, this._cameraFrame.rotation.s).NormalizedCopy();
					this._cameraFrame.rotation.s = Vec3.CrossProduct(this._cameraFrame.rotation.f, this._cameraFrame.rotation.u);
					Vec3 vec2 = -Vec3.Up;
					Vec3 v2 = -this._cameraFrame.rotation.u;
					Vec3 idealCameraTarget = this.IdealCameraTarget;
					float f;
					if (MBMath.GetRayPlaneIntersectionPoint(vec2, idealCameraTarget, this._cameraFrame.origin, v2, out f))
					{
						this.IdealCameraTarget = this._cameraFrame.origin + v2 * f;
						this._cameraTarget = this.IdealCameraTarget;
					}
					this._cameraElevation = -new Vec2(this._cameraFrame.rotation.f.AsVec2.Length, this._cameraFrame.rotation.f.z).RotationInRadians;
					this.CameraDistance = (this._cameraFrame.origin - this.IdealCameraTarget).Length - 2f;
					this.TargetCameraDistance = this.CameraDistance;
					this.AdditionalElevation = this._cameraElevation - this.CalculateCameraElevation(this.CameraDistance);
					this._lastUsedIdealCameraTarget = new CampaignVec2(this.IdealCameraTarget.AsVec2, true);
					this.ComputeMapCamera(ref this._cameraTarget, this.CameraBearing, this._cameraElevation, this.CameraDistance, ref this._lastUsedIdealCameraTarget);
				}
				else
				{
					float num2 = 0.47123894f;
					int num3 = 0;
					do
					{
						this._cameraElevation += ((this._cameraFrame.origin.z < num) ? num2 : (-num2));
						float num4 = (700f - MathF.Min(700f, MathF.Max(50f, this.CameraDistance))) * -1f * 0.00042857145f;
						float maxValue = MathF.Max(num4 + 1E-05f, 1.5550884f - this.CalculateCameraElevation(this.CameraDistance));
						this.AdditionalElevation = this._cameraElevation - this.CalculateCameraElevation(this.CameraDistance);
						this.AdditionalElevation = MBMath.ClampFloat(this.AdditionalElevation, num4, maxValue);
						this._cameraElevation = this.AdditionalElevation + this.CalculateCameraElevation(this.CameraDistance);
						CampaignVec2 zero = CampaignVec2.Zero;
						this._cameraFrame = this.ComputeMapCamera(ref this._cameraTarget, this.CameraBearing, this._cameraElevation, this.CameraDistance, ref zero);
						IMapScene mapSceneWrapper2 = Campaign.Current.MapSceneWrapper;
						campaignVec = new CampaignVec2(this._cameraFrame.origin.AsVec2, !isCurrentlyAtSea);
						mapSceneWrapper2.GetHeightAtPoint(campaignVec, ref num);
						num += 0.5f;
						if (num2 > 0.0001f)
						{
							num2 *= 0.5f;
						}
						else
						{
							num3++;
						}
					}
					while (num2 > 0.0001f || (this._cameraFrame.origin.z < num && num3 < 5));
					if (this._cameraFrame.origin.z < num)
					{
						this._cameraFrame.origin.z = num;
						Vec3 vec3 = -Vec3.Up;
						Vec3 v3 = -this._cameraFrame.rotation.u;
						Vec3 idealCameraTarget2 = this.IdealCameraTarget;
						float f2;
						if (MBMath.GetRayPlaneIntersectionPoint(vec3, idealCameraTarget2, this._cameraFrame.origin, v3, out f2) && this.CurrentCameraFollowMode != MapCameraView.CameraFollowMode.MoveToPosition)
						{
							this.IdealCameraTarget = this._cameraFrame.origin + v3 * f2;
							this._cameraTarget = this.IdealCameraTarget;
							this.CameraDistance = (this._cameraFrame.origin - this.IdealCameraTarget).Length - 2f;
						}
						this._lastUsedIdealCameraTarget = new CampaignVec2(this.IdealCameraTarget.AsVec2, true);
						this.ComputeMapCamera(ref this._cameraTarget, this.CameraBearing, this._cameraElevation, this.CameraDistance, ref this._lastUsedIdealCameraTarget);
						this.TargetCameraDistance = MathF.Max(this.TargetCameraDistance, this.CameraDistance);
					}
				}
			}
			this.Camera.Frame = this._cameraFrame;
			this.Camera.SetFovVertical(0.6981317f, Screen.AspectRatio, 0.01f, this.MaximumCameraHeight * 4f);
			this._mapScene.SetDepthOfFieldFocus(0f);
			this._mapScene.SetDepthOfFieldParameters(0f, 0f, false);
			MatrixFrame identity = MatrixFrame.Identity;
			identity.rotation = this._cameraFrame.rotation;
			identity.origin = this._cameraTarget;
			IMapScene mapSceneWrapper3 = Campaign.Current.MapSceneWrapper;
			campaignVec = new CampaignVec2(identity.origin.AsVec2, true);
			mapSceneWrapper3.GetHeightAtPoint(campaignVec, ref identity.origin.z);
			identity.origin = MBMath.Lerp(identity.origin, this._cameraFrame.origin, 0.075f, 1E-05f);
			campaignVec = new CampaignVec2(identity.origin.AsVec2, true);
			PathFaceRecord face = campaignVec.Face;
			if (!face.IsValid())
			{
				campaignVec = new CampaignVec2(identity.origin.AsVec2, false);
				face = campaignVec.Face;
			}
			if (face.IsValid())
			{
				TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
				MBMapScene.TickAmbientSounds(this._mapScene, (int)faceTerrainType);
			}
			SoundManager.SetListenerFrame(identity);
		}

		// Token: 0x0600024C RID: 588 RVA: 0x000154C5 File Offset: 0x000136C5
		protected virtual Vec3 GetCameraTargetForPosition(CampaignVec2 targetPosition)
		{
			return targetPosition.AsVec3() + Vec3.Up;
		}

		// Token: 0x0600024D RID: 589 RVA: 0x000154D8 File Offset: 0x000136D8
		protected virtual Vec3 GetCameraTargetForParty(PartyBase party)
		{
			CampaignVec2 campaignVec = CampaignVec2.Zero;
			if (party.IsMobile && party.MobileParty.CurrentSettlement != null)
			{
				campaignVec = party.MobileParty.CurrentSettlement.Position;
			}
			else if (party.IsMobile && party.MobileParty.BesiegedSettlement != null)
			{
				if (PlayerSiege.PlayerSiegeEvent != null)
				{
					Vec2 asVec = party.MobileParty.BesiegedSettlement.Town.BesiegerCampPositions1.First<MatrixFrame>().origin.AsVec2;
					Vec2 pos = Vec2.Lerp(party.MobileParty.TargetPosition.ToVec2(), asVec, 0.75f);
					campaignVec = new CampaignVec2(pos, campaignVec.IsOnLand);
				}
				else
				{
					campaignVec = party.MobileParty.TargetPosition;
				}
			}
			else
			{
				campaignVec = party.Position;
			}
			return this.GetCameraTargetForPosition(campaignVec);
		}

		// Token: 0x0600024E RID: 590 RVA: 0x000155A8 File Offset: 0x000137A8
		protected virtual bool GetMapCameraInput(MapCameraView.InputInformation inputInformation)
		{
			bool flag = false;
			bool result = !inputInformation.LeftButtonDraggingMode;
			if (inputInformation.IsControlDown && inputInformation.CheatModeEnabled)
			{
				flag = true;
				if (inputInformation.DeltaMouseScroll > 0.01f)
				{
					this.CameraFastMoveMultiplier *= 1.25f;
				}
				else if (inputInformation.DeltaMouseScroll < -0.01f)
				{
					this.CameraFastMoveMultiplier *= 0.8f;
				}
				this.CameraFastMoveMultiplier = MBMath.ClampFloat(this.CameraFastMoveMultiplier, 1f, 37.252903f);
			}
			Vec2 vec = Vec2.Zero;
			if (!inputInformation.LeftMouseButtonPressed && inputInformation.LeftMouseButtonDown && !inputInformation.LeftMouseButtonReleased && inputInformation.MousePositionPixel.DistanceSquared(inputInformation.ClickedPositionPixel) > 300f && !inputInformation.IsInMenu)
			{
				Vec3 vec2;
				if (!inputInformation.LeftButtonDraggingMode)
				{
					this.IdealCameraTarget = this._cameraTarget;
					vec2 = this.IdealCameraTarget;
					this._lastUsedIdealCameraTarget = new CampaignVec2(vec2.AsVec2, true);
				}
				vec2 = inputInformation.WorldMouseFar - inputInformation.WorldMouseNear;
				Vec3 v = vec2.NormalizedCopy();
				Vec3 vec3 = -Vec3.Up;
				float f;
				if (MBMath.GetRayPlaneIntersectionPoint(vec3, inputInformation.ClickedPosition, inputInformation.WorldMouseNear, v, out f))
				{
					this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.Free;
					Vec3 vec4 = inputInformation.WorldMouseNear + v * f;
					vec = inputInformation.ClickedPosition.AsVec2 - vec4.AsVec2;
				}
			}
			if (inputInformation.MiddleMouseButtonDown)
			{
				this.TargetCameraDistance += 0.01f * (this.CameraDistance + 20f) * inputInformation.MouseSensitivity * inputInformation.MouseMoveY;
			}
			if (inputInformation.RotateLeftKeyDown)
			{
				this.CameraBearingVelocity = inputInformation.Dt * 2f;
			}
			else if (inputInformation.RotateRightKeyDown)
			{
				this.CameraBearingVelocity = inputInformation.Dt * -2f;
			}
			this.CameraBearingVelocity += inputInformation.HorizontalCameraInput * 1.75f * inputInformation.Dt;
			if (inputInformation.RightMouseButtonDown)
			{
				this.CameraBearingVelocity += 0.01f * inputInformation.MouseSensitivity * inputInformation.MouseMoveX;
			}
			float num = 0.1f;
			if (!inputInformation.IsMouseActive)
			{
				num *= inputInformation.Dt * 10f;
			}
			if (!flag)
			{
				this.TargetCameraDistance -= inputInformation.MapZoomIn * num * (this.CameraDistance + 20f);
				this.TargetCameraDistance += inputInformation.MapZoomOut * num * (this.CameraDistance + 20f);
			}
			PartyBase cameraFollowParty = Campaign.Current.CameraFollowParty;
			this.TargetCameraDistance = MBMath.ClampFloat(this.TargetCameraDistance, 2.5f, (cameraFollowParty != null && cameraFollowParty.IsMobile && (cameraFollowParty.MobileParty.BesiegedSettlement != null || (cameraFollowParty.MobileParty.CurrentSettlement != null && cameraFollowParty.MobileParty.CurrentSettlement.IsUnderSiege))) ? 30f : this.MaximumCameraHeight);
			float num2 = this.TargetCameraDistance - this.CameraDistance;
			float num3 = MathF.Abs(num2);
			float cameraDistance = ((num3 > 0.001f) ? (this.CameraDistance + num2 * inputInformation.Dt * 8f) : this.TargetCameraDistance);
			if (this.CurrentCameraFollowMode == MapCameraView.CameraFollowMode.Free && !inputInformation.RightMouseButtonDown && !inputInformation.LeftMouseButtonDown && num3 >= 0.001f)
			{
				Vec3 vec2 = inputInformation.WorldMouseFar - this.CameraFrame.origin;
				if (vec2.NormalizedCopy().z < -0.2f && inputInformation.RayCastForClosestEntityOrTerrainCondition)
				{
					MatrixFrame matrixFrame = this.ComputeMapCamera(ref this._cameraTarget, this.CameraBearing + this.CameraBearingVelocity, MathF.Min(this.CalculateCameraElevation(cameraDistance) + this.AdditionalElevation, 1.5550884f), cameraDistance, ref this._lastUsedIdealCameraTarget);
					Vec3 vec5 = -Vec3.Up;
					vec2 = inputInformation.WorldMouseFar - this.CameraFrame.origin;
					Vec3 vec6 = vec2.NormalizedCopy();
					vec2 = this.CameraFrame.rotation.TransformToLocal(vec6);
					Vec3 v2 = matrixFrame.rotation.TransformToParent(vec2);
					float f2;
					if (MBMath.GetRayPlaneIntersectionPoint(vec5, inputInformation.ProjectedPosition, matrixFrame.origin, v2, out f2))
					{
						Vec2 asVec = inputInformation.ProjectedPosition.AsVec2;
						vec2 = matrixFrame.origin + v2 * f2;
						vec = asVec - vec2.AsVec2;
						result = false;
					}
				}
			}
			if (inputInformation.RX != 0f || inputInformation.RY != 0f || vec.IsNonZero())
			{
				float f3 = 0.001f * (this.CameraDistance * 0.55f + 15f);
				Vec2 v3 = Vec2.FromRotation(-this.CameraBearing);
				Vec3 vec2 = this.IdealCameraTarget;
				if ((vec2.AsVec2 - this._lastUsedIdealCameraTarget.ToVec2()).LengthSquared > 0.010000001f)
				{
					this.IdealCameraTarget = this._lastUsedIdealCameraTarget.AsVec3();
					this._cameraTarget = this.IdealCameraTarget;
				}
				if (!vec.IsNonZero())
				{
					this.IdealCameraTarget = this._cameraTarget;
				}
				Vec2 vec7 = inputInformation.Dt * 500f * inputInformation.RX * v3.RightVec() * f3 + inputInformation.Dt * 500f * inputInformation.RY * v3 * f3;
				this.IdealCameraTarget = new Vec3(this.IdealCameraTarget.x + vec.x + vec7.x, this.IdealCameraTarget.y + vec.y + vec7.y, this.IdealCameraTarget.z, -1f);
				if (vec.IsNonZero())
				{
					this._cameraTarget = this.IdealCameraTarget;
				}
				this._cameraTarget.AsVec2 = this._cameraTarget.AsVec2 + vec7;
				if (inputInformation.RX != 0f || inputInformation.RY != 0f)
				{
					this.CurrentCameraFollowMode = MapCameraView.CameraFollowMode.Free;
				}
			}
			this.CameraBearing += this.CameraBearingVelocity;
			this.CameraBearingVelocity = 0f;
			this.CameraDistance = cameraDistance;
			this._cameraElevation = MathF.Min(this.CalculateCameraElevation(cameraDistance) + this.AdditionalElevation, 1.5550884f);
			if (this.CurrentCameraFollowMode == MapCameraView.CameraFollowMode.FollowParty && cameraFollowParty != null && cameraFollowParty.IsValid)
			{
				CampaignVec2 campaignVec = new CampaignVec2(Vec2.Zero, cameraFollowParty.IsMobile && cameraFollowParty.MobileParty.IsCurrentlyAtSea);
				if (cameraFollowParty.IsMobile)
				{
					Settlement settlement = cameraFollowParty.MobileParty.CurrentSettlement ?? cameraFollowParty.MobileParty.BesiegedSettlement;
					if (settlement != null)
					{
						if (settlement.SiegeEvent != null)
						{
							if (settlement.HasPort && settlement.SiegeEvent.IsBlockadeActive)
							{
								campaignVec = new CampaignVec2(settlement.PortPosition.ToVec2() * 0.25f + settlement.GatePosition.ToVec2() * 0.75f, true);
							}
							else
							{
								campaignVec = settlement.GatePosition;
							}
						}
						else
						{
							campaignVec = cameraFollowParty.MobileParty.CurrentSettlement.Position;
						}
					}
					else
					{
						campaignVec = cameraFollowParty.Position;
					}
				}
				else
				{
					campaignVec = cameraFollowParty.Position;
				}
				float num4 = 0f;
				Campaign.Current.MapSceneWrapper.GetHeightAtPoint(campaignVec, ref num4);
				this.IdealCameraTarget = new Vec3(campaignVec.X, campaignVec.Y, num4 + 1f, -1f);
			}
			return result;
		}

		// Token: 0x0600024F RID: 591 RVA: 0x00015D58 File Offset: 0x00013F58
		protected virtual MatrixFrame ComputeMapCamera(ref Vec3 cameraTarget, float cameraBearing, float cameraElevation, float cameraDistance, ref CampaignVec2 lastUsedIdealCameraTarget)
		{
			Vec2 asVec = cameraTarget.AsVec2;
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = cameraTarget;
			identity.rotation.RotateAboutSide(1.5707964f);
			identity.rotation.RotateAboutForward(-cameraBearing);
			identity.rotation.RotateAboutSide(-cameraElevation);
			identity.origin += identity.rotation.u * (cameraDistance + 2f);
			Vec2 vec = (Campaign.MapMinimumPosition + Campaign.MapMaximumPosition) * 0.5f;
			float num = Campaign.MapMaximumPosition.y - vec.y;
			float num2 = Campaign.MapMaximumPosition.x - vec.x;
			asVec.x = MBMath.ClampFloat(asVec.x, vec.x - num2, vec.x + num2);
			asVec.y = MBMath.ClampFloat(asVec.y, vec.y - num, vec.y + num);
			float a = MBMath.ClampFloat(lastUsedIdealCameraTarget.X, vec.x - num2, vec.x + num2);
			float b = MBMath.ClampFloat(lastUsedIdealCameraTarget.Y, vec.y - num, vec.y + num);
			lastUsedIdealCameraTarget = new CampaignVec2(new Vec2(a, b), lastUsedIdealCameraTarget.IsOnLand);
			identity.origin.x = identity.origin.x + (asVec.x - cameraTarget.x);
			identity.origin.y = identity.origin.y + (asVec.y - cameraTarget.y);
			return identity;
		}

		// Token: 0x06000250 RID: 592 RVA: 0x00015EF5 File Offset: 0x000140F5
		protected virtual float CalculateCameraElevation(float cameraDistance)
		{
			return cameraDistance * 0.0075f + 0.35f;
		}

		// Token: 0x04000126 RID: 294
		private const float VerticalHalfViewAngle = 0.34906584f;

		// Token: 0x04000127 RID: 295
		private Vec3 _cameraTarget;

		// Token: 0x04000128 RID: 296
		private float _distanceToIdealCameraTargetToStopCameraSoundEventsSquared;

		// Token: 0x04000129 RID: 297
		private int _cameraMoveSfxSoundEventId;

		// Token: 0x0400012A RID: 298
		private SoundEvent _cameraMoveSfxSoundEvent;

		// Token: 0x0400012B RID: 299
		private bool _doFastCameraMovementToTarget;

		// Token: 0x0400012C RID: 300
		private float _cameraElevation;

		// Token: 0x0400012D RID: 301
		private CampaignVec2 _lastUsedIdealCameraTarget;

		// Token: 0x0400012E RID: 302
		private CampaignVec2 _cameraAnimationTarget;

		// Token: 0x0400012F RID: 303
		private float _cameraAnimationStopDuration;

		// Token: 0x04000130 RID: 304
		private readonly Scene _mapScene;

		// Token: 0x04000134 RID: 308
		protected float _customMaximumCameraHeight;

		// Token: 0x0400013C RID: 316
		private MatrixFrame _cameraFrame;

		// Token: 0x020000A0 RID: 160
		public enum CameraFollowMode
		{
			// Token: 0x0400031D RID: 797
			Free,
			// Token: 0x0400031E RID: 798
			FollowParty,
			// Token: 0x0400031F RID: 799
			MoveToPosition
		}

		// Token: 0x020000A1 RID: 161
		public struct InputInformation
		{
			// Token: 0x04000320 RID: 800
			public bool IsMainPartyValid;

			// Token: 0x04000321 RID: 801
			public bool IsMapReady;

			// Token: 0x04000322 RID: 802
			public bool IsControlDown;

			// Token: 0x04000323 RID: 803
			public bool IsMouseActive;

			// Token: 0x04000324 RID: 804
			public bool CheatModeEnabled;

			// Token: 0x04000325 RID: 805
			public bool LeftMouseButtonPressed;

			// Token: 0x04000326 RID: 806
			public bool LeftMouseButtonDown;

			// Token: 0x04000327 RID: 807
			public bool LeftMouseButtonReleased;

			// Token: 0x04000328 RID: 808
			public bool MiddleMouseButtonDown;

			// Token: 0x04000329 RID: 809
			public bool RightMouseButtonDown;

			// Token: 0x0400032A RID: 810
			public bool RotateLeftKeyDown;

			// Token: 0x0400032B RID: 811
			public bool RotateRightKeyDown;

			// Token: 0x0400032C RID: 812
			public bool PartyMoveUpKey;

			// Token: 0x0400032D RID: 813
			public bool PartyMoveDownKey;

			// Token: 0x0400032E RID: 814
			public bool PartyMoveLeftKey;

			// Token: 0x0400032F RID: 815
			public bool PartyMoveRightKey;

			// Token: 0x04000330 RID: 816
			public bool CameraFollowModeKeyPressed;

			// Token: 0x04000331 RID: 817
			public bool LeftButtonDraggingMode;

			// Token: 0x04000332 RID: 818
			public bool IsInMenu;

			// Token: 0x04000333 RID: 819
			public bool RayCastForClosestEntityOrTerrainCondition;

			// Token: 0x04000334 RID: 820
			public float MapZoomIn;

			// Token: 0x04000335 RID: 821
			public float MapZoomOut;

			// Token: 0x04000336 RID: 822
			public float DeltaMouseScroll;

			// Token: 0x04000337 RID: 823
			public float MouseSensitivity;

			// Token: 0x04000338 RID: 824
			public float MouseMoveX;

			// Token: 0x04000339 RID: 825
			public float MouseMoveY;

			// Token: 0x0400033A RID: 826
			public float HorizontalCameraInput;

			// Token: 0x0400033B RID: 827
			public float RX;

			// Token: 0x0400033C RID: 828
			public float RY;

			// Token: 0x0400033D RID: 829
			public float RS;

			// Token: 0x0400033E RID: 830
			public float Dt;

			// Token: 0x0400033F RID: 831
			public Vec2 MousePositionPixel;

			// Token: 0x04000340 RID: 832
			public Vec2 ClickedPositionPixel;

			// Token: 0x04000341 RID: 833
			public Vec3 ClickedPosition;

			// Token: 0x04000342 RID: 834
			public Vec3 ProjectedPosition;

			// Token: 0x04000343 RID: 835
			public Vec3 WorldMouseNear;

			// Token: 0x04000344 RID: 836
			public Vec3 WorldMouseFar;
		}
	}
}
