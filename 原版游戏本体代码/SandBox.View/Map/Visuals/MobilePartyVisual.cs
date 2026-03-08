using System;
using System.Threading;
using Helpers;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View.Map.Visuals
{
	// Token: 0x02000063 RID: 99
	public class MobilePartyVisual : MapEntityVisual<PartyBase>
	{
		// Token: 0x1700006D RID: 109
		// (get) Token: 0x060003EC RID: 1004 RVA: 0x0001E667 File Offset: 0x0001C867
		public override float BearingRotation
		{
			get
			{
				return this._bearingRotation;
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x060003ED RID: 1005 RVA: 0x0001E670 File Offset: 0x0001C870
		private Scene MapScene
		{
			get
			{
				if (this._mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
				{
					this._mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
				}
				return this._mapScene;
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x060003EE RID: 1006 RVA: 0x0001E6BE File Offset: 0x0001C8BE
		public override MapEntityVisual AttachedTo
		{
			get
			{
				MobileParty mobileParty = base.MapEntity.MobileParty;
				if (((mobileParty != null) ? mobileParty.AttachedTo : null) != null)
				{
					return MobilePartyVisualManager.Current.GetVisualOfEntity(base.MapEntity.MobileParty.AttachedTo.Party);
				}
				return null;
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x060003EF RID: 1007 RVA: 0x0001E6FA File Offset: 0x0001C8FA
		public override CampaignVec2 InteractionPositionForPlayer
		{
			get
			{
				return ((IInteractablePoint)base.MapEntity).GetInteractionPosition(MobileParty.MainParty);
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x060003F0 RID: 1008 RVA: 0x0001E70C File Offset: 0x0001C90C
		public override bool IsMobileEntity
		{
			get
			{
				return base.MapEntity.IsMobile;
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x060003F1 RID: 1009 RVA: 0x0001E719 File Offset: 0x0001C919
		public override bool IsMainEntity
		{
			get
			{
				return base.MapEntity == PartyBase.MainParty;
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x060003F2 RID: 1010 RVA: 0x0001E728 File Offset: 0x0001C928
		// (set) Token: 0x060003F3 RID: 1011 RVA: 0x0001E730 File Offset: 0x0001C930
		public GameEntity StrategicEntity { get; private set; }

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x060003F4 RID: 1012 RVA: 0x0001E739 File Offset: 0x0001C939
		// (set) Token: 0x060003F5 RID: 1013 RVA: 0x0001E741 File Offset: 0x0001C941
		public AgentVisuals HumanAgentVisuals { get; private set; }

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x060003F6 RID: 1014 RVA: 0x0001E74A File Offset: 0x0001C94A
		// (set) Token: 0x060003F7 RID: 1015 RVA: 0x0001E752 File Offset: 0x0001C952
		public AgentVisuals MountAgentVisuals { get; private set; }

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x060003F8 RID: 1016 RVA: 0x0001E75B File Offset: 0x0001C95B
		// (set) Token: 0x060003F9 RID: 1017 RVA: 0x0001E763 File Offset: 0x0001C963
		public AgentVisuals CaravanMountAgentVisuals { get; private set; }

		// Token: 0x060003FA RID: 1018 RVA: 0x0001E76C File Offset: 0x0001C96C
		public MobilePartyVisual(PartyBase partyBase)
			: base(partyBase)
		{
			this.CircleLocalFrame = MatrixFrame.Identity;
		}

		// Token: 0x060003FB RID: 1019 RVA: 0x0001E780 File Offset: 0x0001C980
		public override bool IsEnemyOf(IFaction faction)
		{
			return FactionManager.IsAtWarAgainstFaction(base.MapEntity.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x0001E79C File Offset: 0x0001C99C
		public override bool IsAllyOf(IFaction faction)
		{
			return DiplomacyHelper.IsSameFactionAndNotEliminated(base.MapEntity.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x0001E7B8 File Offset: 0x0001C9B8
		internal void OnPartyRemoved()
		{
			if (this.StrategicEntity != null)
			{
				this.RemoveVisualFromVisualsOfEntities();
				this.ReleaseResources();
				this.StrategicEntity.Remove(111);
			}
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x0001E7E4 File Offset: 0x0001C9E4
		public override void OnTrackAction()
		{
			MobileParty mobileParty = base.MapEntity.MobileParty;
			if (mobileParty != null)
			{
				if (Campaign.Current.VisualTrackerManager.CheckTracked(mobileParty))
				{
					Campaign.Current.VisualTrackerManager.RemoveTrackedObject(mobileParty, false);
					return;
				}
				Campaign.Current.VisualTrackerManager.RegisterObject(mobileParty);
			}
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x0001E834 File Offset: 0x0001CA34
		public override bool OnMapClick(bool followModifierUsed)
		{
			MobileParty.NavigationType navigationType;
			if (this.IsMainEntity)
			{
				MobileParty.MainParty.SetMoveModeHold();
			}
			else if (base.MapEntity.MobileParty.IsCurrentlyAtSea == MobileParty.MainParty.IsCurrentlyAtSea && NavigationHelper.CanPlayerNavigateToPosition(base.MapEntity.MobileParty.Position, out navigationType))
			{
				if (followModifierUsed)
				{
					MobileParty.MainParty.SetMoveEscortParty(base.MapEntity.MobileParty, navigationType, false);
				}
				else
				{
					MobileParty.MainParty.SetMoveEngageParty(base.MapEntity.MobileParty, navigationType);
				}
			}
			return true;
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x0001E8C0 File Offset: 0x0001CAC0
		public override void OnHover()
		{
			if (base.MapEntity.MapEvent != null)
			{
				InformationManager.ShowTooltip(typeof(MapEvent), new object[] { base.MapEntity.MapEvent });
				return;
			}
			if (base.MapEntity.IsMobile && base.MapEntity.IsVisible)
			{
				if (base.MapEntity.MobileParty.Army != null && base.MapEntity.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(base.MapEntity.MobileParty))
				{
					if (base.MapEntity.MobileParty.Army.LeaderParty.SiegeEvent != null)
					{
						InformationManager.ShowTooltip(typeof(SiegeEvent), new object[] { base.MapEntity.MobileParty.Army.LeaderParty.SiegeEvent });
						return;
					}
					InformationManager.ShowTooltip(typeof(Army), new object[]
					{
						base.MapEntity.MobileParty.Army,
						false,
						true
					});
					return;
				}
				else
				{
					if (base.MapEntity.MobileParty.SiegeEvent != null)
					{
						InformationManager.ShowTooltip(typeof(SiegeEvent), new object[] { base.MapEntity.MobileParty.SiegeEvent });
						return;
					}
					InformationManager.ShowTooltip(typeof(MobileParty), new object[]
					{
						base.MapEntity.MobileParty,
						false,
						true
					});
				}
			}
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x0001EA54 File Offset: 0x0001CC54
		public override Vec3 GetVisualPosition()
		{
			return base.MapEntity.MobileParty.VisualPosition2DWithoutError.ToVec3(base.MapEntity.Position.AsVec3().Z);
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x0001EA94 File Offset: 0x0001CC94
		public override void ReleaseResources()
		{
			this.ResetPartyIcon();
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0001EA9C File Offset: 0x0001CC9C
		public override bool IsVisibleOrFadingOut()
		{
			return this._entityAlpha > 0f;
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0001EAAC File Offset: 0x0001CCAC
		public override void OnOpenEncyclopedia()
		{
			if (base.MapEntity.MobileParty.IsLordParty && base.MapEntity.MobileParty.LeaderHero != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(base.MapEntity.MobileParty.LeaderHero.EncyclopediaLink);
			}
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x0001EB04 File Offset: 0x0001CD04
		internal void Tick(float dt, float realDt, ref int dirtyPartiesCount, ref MobilePartyVisual[] dirtyPartiesList)
		{
			if (this.StrategicEntity == null)
			{
				return;
			}
			if (base.MapEntity.IsVisualDirty && (this._entityAlpha > 0f || base.MapEntity.IsVisible))
			{
				int num = Interlocked.Increment(ref dirtyPartiesCount);
				dirtyPartiesList[num] = this;
			}
			if (this.IsVisibleOrFadingOut() && this.StrategicEntity != null && (!base.MapEntity.MobileParty.IsCurrentlyAtSea || base.MapEntity.MobileParty.IsTransitionInProgress))
			{
				this.UpdateBearingRotation(realDt, dt);
				this._speed = (base.MapEntity.MobileParty.IsActive ? base.MapEntity.MobileParty.Speed : 0f);
				float num2 = ((this.MountAgentVisuals != null) ? 1.3f : 1f);
				float speed = MathF.Min(0.25f * num2 * this._speed / 0.3f, 20f);
				bool isEntityMoving = this.IsEntityMovingVisually();
				AgentVisuals humanAgentVisuals = this.HumanAgentVisuals;
				if (humanAgentVisuals != null)
				{
					humanAgentVisuals.Tick(this.MountAgentVisuals, dt, isEntityMoving, speed);
				}
				AgentVisuals mountAgentVisuals = this.MountAgentVisuals;
				if (mountAgentVisuals != null)
				{
					mountAgentVisuals.Tick(null, dt, isEntityMoving, speed);
				}
				AgentVisuals caravanMountAgentVisuals = this.CaravanMountAgentVisuals;
				if (caravanMountAgentVisuals != null)
				{
					caravanMountAgentVisuals.Tick(null, dt, isEntityMoving, speed);
				}
				MobileParty mobileParty = base.MapEntity.MobileParty;
				MatrixFrame identity = MatrixFrame.Identity;
				identity.origin = this.GetVisualPosition();
				if (mobileParty.Army != null && mobileParty.AttachedTo == mobileParty.Army.LeaderParty && (base.MapEntity.MapEvent == null || !base.MapEntity.MapEvent.IsFieldBattle))
				{
					MatrixFrame frame = this.StrategicEntity.GetFrame();
					Vec2 v = identity.origin.AsVec2 - frame.origin.AsVec2;
					if (v.Length / dt > 20f)
					{
						identity.rotation.RotateAboutUp(this._bearingRotation);
					}
					else if (mobileParty.CurrentSettlement == null)
					{
						float a = MBMath.LerpRadians(frame.rotation.f.AsVec2.RotationInRadians, (v + Vec2.FromRotation(this._bearingRotation) * 0.01f).RotationInRadians, Math.Min(6f * dt, 1f), 0.03f * dt, 10f * dt);
						identity.rotation.RotateAboutUp(a);
					}
					else
					{
						float rotationInRadians = frame.rotation.f.AsVec2.RotationInRadians;
						identity.rotation.RotateAboutUp(rotationInRadians);
					}
				}
				else if (mobileParty.CurrentSettlement == null)
				{
					identity.rotation.RotateAboutUp(this.GetVisualRotation());
				}
				MatrixFrame frame2 = this.StrategicEntity.GetFrame();
				if (!frame2.NearlyEquals(identity, 1E-05f))
				{
					this.StrategicEntity.SetFrame(ref identity, true);
					if (this.HumanAgentVisuals != null)
					{
						MatrixFrame matrixFrame = identity;
						matrixFrame.rotation.ApplyScaleLocal(this.HumanAgentVisuals.GetScale());
						this.HumanAgentVisuals.GetWeakEntity().SetFrame(ref matrixFrame, true);
					}
					if (this.MountAgentVisuals != null)
					{
						MatrixFrame matrixFrame2 = identity;
						matrixFrame2.rotation.ApplyScaleLocal(this.MountAgentVisuals.GetScale());
						this.MountAgentVisuals.GetWeakEntity().SetFrame(ref matrixFrame2, true);
					}
					if (this.CaravanMountAgentVisuals != null)
					{
						frame2 = this.CaravanMountAgentVisuals.GetFrame();
						MatrixFrame matrixFrame3 = identity.TransformToParent(frame2);
						matrixFrame3.rotation.ApplyScaleLocal(this.CaravanMountAgentVisuals.GetScale());
						this.CaravanMountAgentVisuals.GetWeakEntity().SetFrame(ref matrixFrame3, true);
					}
				}
				this.ApplyWindEffect();
			}
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0001EEC4 File Offset: 0x0001D0C4
		private void ApplyWindEffect()
		{
			if (this.HumanAgentVisuals != null && !this.HumanAgentVisuals.GetEquipment()[EquipmentIndex.ExtraWeaponSlot].IsEmpty)
			{
				this.HumanAgentVisuals.SetClothWindToWeaponAtIndex(-this.StrategicEntity.GetGlobalFrame().rotation.f, false, EquipmentIndex.ExtraWeaponSlot);
			}
			ClothSimulatorComponent clothSimulatorComponent;
			if (this._cachedBannerComponent.Item2 != null && (clothSimulatorComponent = this._cachedBannerComponent.Item2 as ClothSimulatorComponent) != null)
			{
				float f = (this.IsPartOfBesiegerCamp(base.MapEntity) ? 6f : 1f);
				clothSimulatorComponent.SetForcedWind(-this.StrategicEntity.GetGlobalFrame().rotation.f * f, false);
			}
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0001EF84 File Offset: 0x0001D184
		internal void OnStartup()
		{
			bool flag = false;
			if (base.MapEntity.IsMobile)
			{
				this.StrategicEntity = GameEntity.CreateEmpty(this.MapScene, true, true, true);
				if (!base.MapEntity.IsVisible)
				{
					this.StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
				}
			}
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(base.MapEntity);
			if (!flag)
			{
				this.CircleLocalFrame = MatrixFrame.Identity;
				if ((visualPartyLeader != null && visualPartyLeader.HasMount()) || base.MapEntity.MobileParty.IsCaravan)
				{
					MatrixFrame circleLocalFrame = this.CircleLocalFrame;
					Mat3 rotation = circleLocalFrame.rotation;
					rotation.ApplyScaleLocal(0.4625f);
					circleLocalFrame.rotation = rotation;
					this.CircleLocalFrame = circleLocalFrame;
				}
				else
				{
					MatrixFrame circleLocalFrame2 = this.CircleLocalFrame;
					Mat3 rotation2 = circleLocalFrame2.rotation;
					rotation2.ApplyScaleLocal(0.3725f);
					circleLocalFrame2.rotation = rotation2;
					this.CircleLocalFrame = circleLocalFrame2;
				}
			}
			this._bearingRotation = base.MapEntity.MobileParty.Bearing.RotationInRadians;
			this.StrategicEntity.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
			if (this.HumanAgentVisuals != null)
			{
				WeakGameEntity weakEntity = this.HumanAgentVisuals.GetWeakEntity();
				if (weakEntity != WeakGameEntity.Invalid)
				{
					weakEntity.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
				}
			}
			if (this.MountAgentVisuals != null)
			{
				WeakGameEntity weakEntity2 = this.MountAgentVisuals.GetWeakEntity();
				if (weakEntity2 != WeakGameEntity.Invalid)
				{
					weakEntity2.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
				}
			}
			if (this.CaravanMountAgentVisuals != null)
			{
				WeakGameEntity weakEntity3 = this.CaravanMountAgentVisuals.GetWeakEntity();
				if (weakEntity3 != WeakGameEntity.Invalid)
				{
					weakEntity3.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
				}
			}
			this.StrategicEntity.SetReadyToRender(true);
			this.StrategicEntity.SetEntityEnvMapVisibility(false);
			this._entityAlpha = 0f;
			if (base.MapEntity.IsVisible)
			{
				if (base.MapEntity.MobileParty.IsTransitionInProgress)
				{
					this.TickFadingState(0.1f, 0.1f);
				}
				else
				{
					this._entityAlpha = 1f;
				}
			}
			this.AddVisualToVisualsOfEntities();
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x0001F19C File Offset: 0x0001D39C
		internal void TickFadingState(float realDt, float dt)
		{
			if ((!base.MapEntity.MobileParty.IsTransitionInProgress || !base.MapEntity.IsVisible) && ((this._entityAlpha < 1f && base.MapEntity.IsVisible) || (this._entityAlpha > 0f && !base.MapEntity.IsVisible)))
			{
				if (base.MapEntity.IsVisible)
				{
					if (this._entityAlpha <= 0f)
					{
						this.StrategicEntity.SetVisibilityExcludeParents(true);
						if (this.HumanAgentVisuals != null)
						{
							WeakGameEntity weakEntity = this.HumanAgentVisuals.GetWeakEntity();
							if (weakEntity != WeakGameEntity.Invalid)
							{
								weakEntity.SetVisibilityExcludeParents(true);
							}
						}
						if (this.MountAgentVisuals != null)
						{
							WeakGameEntity weakEntity2 = this.MountAgentVisuals.GetWeakEntity();
							if (weakEntity2 != WeakGameEntity.Invalid)
							{
								weakEntity2.SetVisibilityExcludeParents(true);
							}
						}
						if (this.CaravanMountAgentVisuals != null)
						{
							WeakGameEntity weakEntity3 = this.CaravanMountAgentVisuals.GetWeakEntity();
							if (weakEntity3 != WeakGameEntity.Invalid)
							{
								weakEntity3.SetVisibilityExcludeParents(true);
							}
						}
					}
					this._entityAlpha = MathF.Min(this._entityAlpha + MathF.Max(realDt, 1E-05f), 1f);
					this.StrategicEntity.SetAlpha(this._entityAlpha);
					if (this.HumanAgentVisuals != null)
					{
						WeakGameEntity weakEntity4 = this.HumanAgentVisuals.GetWeakEntity();
						if (weakEntity4 != WeakGameEntity.Invalid)
						{
							weakEntity4.SetAlpha(this._entityAlpha);
						}
					}
					if (this.MountAgentVisuals != null)
					{
						WeakGameEntity weakEntity5 = this.MountAgentVisuals.GetWeakEntity();
						if (weakEntity5 != WeakGameEntity.Invalid)
						{
							weakEntity5.SetAlpha(this._entityAlpha);
						}
					}
					if (this.CaravanMountAgentVisuals != null)
					{
						WeakGameEntity weakEntity6 = this.CaravanMountAgentVisuals.GetWeakEntity();
						if (weakEntity6 != WeakGameEntity.Invalid)
						{
							weakEntity6.SetAlpha(this._entityAlpha);
						}
					}
					this.StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
					return;
				}
				this._entityAlpha = MathF.Max(this._entityAlpha - MathF.Max(realDt, 1E-05f), 0f);
				this.StrategicEntity.SetAlpha(this._entityAlpha);
				if (this.HumanAgentVisuals != null)
				{
					WeakGameEntity weakEntity7 = this.HumanAgentVisuals.GetWeakEntity();
					if (weakEntity7 != WeakGameEntity.Invalid)
					{
						weakEntity7.SetAlpha(this._entityAlpha);
					}
				}
				if (this.MountAgentVisuals != null)
				{
					WeakGameEntity weakEntity8 = this.MountAgentVisuals.GetWeakEntity();
					if (weakEntity8 != WeakGameEntity.Invalid)
					{
						weakEntity8.SetAlpha(this._entityAlpha);
					}
				}
				if (this.CaravanMountAgentVisuals != null)
				{
					WeakGameEntity weakEntity9 = this.CaravanMountAgentVisuals.GetWeakEntity();
					if (weakEntity9 != WeakGameEntity.Invalid)
					{
						weakEntity9.SetAlpha(this._entityAlpha);
					}
				}
				if (this._entityAlpha <= 0f)
				{
					this.StrategicEntity.SetVisibilityExcludeParents(false);
					if (this.HumanAgentVisuals != null)
					{
						WeakGameEntity weakEntity10 = this.HumanAgentVisuals.GetWeakEntity();
						if (weakEntity10 != WeakGameEntity.Invalid)
						{
							weakEntity10.SetVisibilityExcludeParents(false);
						}
					}
					if (this.MountAgentVisuals != null)
					{
						WeakGameEntity weakEntity11 = this.MountAgentVisuals.GetWeakEntity();
						if (weakEntity11 != WeakGameEntity.Invalid)
						{
							weakEntity11.SetVisibilityExcludeParents(false);
						}
					}
					if (this.CaravanMountAgentVisuals != null)
					{
						WeakGameEntity weakEntity12 = this.CaravanMountAgentVisuals.GetWeakEntity();
						if (weakEntity12 != WeakGameEntity.Invalid)
						{
							weakEntity12.SetVisibilityExcludeParents(false);
						}
					}
					this.StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
					return;
				}
			}
			else if (base.MapEntity.MobileParty.IsTransitionInProgress)
			{
				if ((base.MapEntity.MobileParty.Army == null || base.MapEntity.MobileParty.Army.LeaderParty == base.MapEntity.MobileParty || base.MapEntity.MobileParty.AttachedTo == null) && this.IsMobileEntity && this.GetTransitionProgress() < 1f)
				{
					this.TickTransitionFadeState(dt);
					return;
				}
			}
			else
			{
				MobilePartyVisualManager.Current.UnRegisterFadingVisual(this);
			}
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x0001F584 File Offset: 0x0001D784
		private void UpdateBearingRotation(float realDt, float dt)
		{
			float num = MBMath.WrapAngle(base.MapEntity.MobileParty.Bearing.RotationInRadians - this._bearingRotation);
			float num2 = ((base.MapEntity.MapEvent != null) ? realDt : dt);
			this._bearingRotation += num * MathF.Min(num2 * 30f, 1f);
			this._bearingRotation = MBMath.WrapAngle(this._bearingRotation);
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x0001F5FC File Offset: 0x0001D7FC
		private void TickTransitionFadeState(float dt)
		{
			float transitionProgress = this.GetTransitionProgress();
			if (base.MapEntity.MobileParty.IsCurrentlyAtSea)
			{
				this._entityAlpha = transitionProgress;
				AgentVisuals humanAgentVisuals = this.HumanAgentVisuals;
				if (humanAgentVisuals != null)
				{
					GameEntity entity = humanAgentVisuals.GetEntity();
					if (entity != null)
					{
						entity.SetAlpha(this._entityAlpha);
					}
				}
				AgentVisuals mountAgentVisuals = this.MountAgentVisuals;
				if (mountAgentVisuals != null)
				{
					GameEntity entity2 = mountAgentVisuals.GetEntity();
					if (entity2 != null)
					{
						entity2.SetAlpha(this._entityAlpha);
					}
				}
				AgentVisuals caravanMountAgentVisuals = this.CaravanMountAgentVisuals;
				if (caravanMountAgentVisuals != null)
				{
					GameEntity entity3 = caravanMountAgentVisuals.GetEntity();
					if (entity3 != null)
					{
						entity3.SetAlpha(this._entityAlpha);
					}
				}
				if (this.HumanAgentVisuals != null)
				{
					MatrixFrame frame = this.HumanAgentVisuals.GetEntity().GetFrame();
					CampaignVec2 campaignVec = base.MapEntity.MobileParty.EndPositionForNavigationTransition + base.MapEntity.MobileParty.ArmyPositionAdder;
					float x = MathF.Lerp(frame.origin.X, campaignVec.X, dt, 1E-05f);
					float y = MathF.Lerp(frame.origin.Y, campaignVec.Y, dt, 1E-05f);
					float z = MathF.Lerp(frame.origin.z, campaignVec.AsVec3().Z, dt, 1E-05f);
					frame.origin = new Vec3(x, y, z, -1f);
					GameEntity entity4 = this.HumanAgentVisuals.GetEntity();
					if (entity4 != null)
					{
						entity4.SetFrame(ref frame, false);
					}
					AgentVisuals mountAgentVisuals2 = this.MountAgentVisuals;
					if (mountAgentVisuals2 != null)
					{
						GameEntity entity5 = mountAgentVisuals2.GetEntity();
						if (entity5 != null)
						{
							entity5.SetFrame(ref frame, false);
						}
					}
					AgentVisuals caravanMountAgentVisuals2 = this.CaravanMountAgentVisuals;
					if (caravanMountAgentVisuals2 == null)
					{
						return;
					}
					GameEntity entity6 = caravanMountAgentVisuals2.GetEntity();
					if (entity6 == null)
					{
						return;
					}
					entity6.SetFrame(ref frame, false);
					return;
				}
			}
			else
			{
				this._entityAlpha = 1f - transitionProgress;
				AgentVisuals humanAgentVisuals2 = this.HumanAgentVisuals;
				if (humanAgentVisuals2 != null)
				{
					GameEntity entity7 = humanAgentVisuals2.GetEntity();
					if (entity7 != null)
					{
						entity7.SetAlpha(this._entityAlpha);
					}
				}
				AgentVisuals mountAgentVisuals3 = this.MountAgentVisuals;
				if (mountAgentVisuals3 != null)
				{
					GameEntity entity8 = mountAgentVisuals3.GetEntity();
					if (entity8 != null)
					{
						entity8.SetAlpha(this._entityAlpha);
					}
				}
				AgentVisuals caravanMountAgentVisuals3 = this.CaravanMountAgentVisuals;
				if (caravanMountAgentVisuals3 == null)
				{
					return;
				}
				GameEntity entity9 = caravanMountAgentVisuals3.GetEntity();
				if (entity9 == null)
				{
					return;
				}
				entity9.SetAlpha(this._entityAlpha);
			}
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x0001F818 File Offset: 0x0001DA18
		internal void ValidateIsDirty()
		{
			if (base.MapEntity.MemberRoster.TotalManCount != 0)
			{
				this.RefreshPartyIcon();
				if ((this._entityAlpha < 1f && base.MapEntity.IsVisible) || (this._entityAlpha > 0f && !base.MapEntity.IsVisible))
				{
					MobilePartyVisualManager.Current.RegisterFadingVisual(this);
					return;
				}
			}
			else
			{
				this.ResetPartyIcon();
			}
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x0001F884 File Offset: 0x0001DA84
		private void RefreshPartyIcon()
		{
			if (base.MapEntity.IsVisualDirty)
			{
				base.MapEntity.OnVisualsUpdated();
				bool flag = true;
				bool flag2 = true;
				this.ResetPartyIcon();
				MatrixFrame circleLocalFrame = this.CircleLocalFrame;
				circleLocalFrame.origin = Vec3.Zero;
				this.CircleLocalFrame = circleLocalFrame;
				MobileParty mobileParty = base.MapEntity.MobileParty;
				if (((mobileParty != null) ? mobileParty.CurrentSettlement : null) != null)
				{
					this.AddVisualToVisualsOfEntities();
					if (!base.MapEntity.MobileParty.MapFaction.IsAtWarWith(base.MapEntity.MobileParty.CurrentSettlement.MapFaction))
					{
						Hero leaderHero = base.MapEntity.LeaderHero;
						if (((leaderHero != null) ? leaderHero.ClanBanner : null) != null)
						{
							string bannerCode = base.MapEntity.LeaderHero.ClanBanner.BannerCode;
							if (string.IsNullOrEmpty(bannerCode))
							{
								goto IL_3FB;
							}
							MatrixFrame matrixFrame = MatrixFrame.Identity;
							Vec3 bannerPositionForParty = SettlementVisualManager.Current.GetSettlementVisual(base.MapEntity.MobileParty.CurrentSettlement).GetBannerPositionForParty(base.MapEntity.MobileParty);
							if (!bannerPositionForParty.IsValid)
							{
								goto IL_3FB;
							}
							matrixFrame.origin = bannerPositionForParty;
							MatrixFrame globalFrame = this.StrategicEntity.GetGlobalFrame();
							matrixFrame.origin = globalFrame.TransformToLocal(matrixFrame.origin);
							float num = MBMath.Map((float)base.MapEntity.NumberOfAllMembers / 400f * ((base.MapEntity.MobileParty.Army != null && base.MapEntity.MobileParty.Army.LeaderParty == base.MapEntity.MobileParty) ? 1.25f : 1f), 0f, 1f, 0.2f, 0.5f);
							matrixFrame = matrixFrame.Elevate(-num);
							matrixFrame.rotation.ApplyScaleLocal(num);
							globalFrame = this.StrategicEntity.GetGlobalFrame();
							matrixFrame.rotation = globalFrame.rotation.TransformToLocal(matrixFrame.rotation);
							this.StrategicEntity.AddSphereAsBody(matrixFrame.origin + Vec3.Up * 0.3f, 0.15f, BodyFlags.None);
							flag = false;
							string text = "campaign_flag";
							if (this._cachedBannerComponent.Item1 == bannerCode + text)
							{
								this._cachedBannerComponent.Item2.GetFirstMetaMesh().Frame = matrixFrame;
								this.StrategicEntity.AddComponent(this._cachedBannerComponent.Item2);
								goto IL_3FB;
							}
							MetaMesh bannerOfCharacter = MobilePartyVisual.GetBannerOfCharacter(new Banner(bannerCode), text);
							bannerOfCharacter.Frame = matrixFrame;
							int componentCount = this.StrategicEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator);
							this.StrategicEntity.AddMultiMesh(bannerOfCharacter, true);
							if (this.StrategicEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > componentCount)
							{
								this._cachedBannerComponent.Item1 = bannerCode + text;
								this._cachedBannerComponent.Item2 = this.StrategicEntity.GetComponentAtIndex(componentCount, GameEntity.ComponentType.ClothSimulator);
								goto IL_3FB;
							}
							goto IL_3FB;
						}
					}
					this.StrategicEntity.RemovePhysics(false);
				}
				else if (base.MapEntity.MobileParty != null && (base.MapEntity.MobileParty.IsCurrentlyAtSea || base.MapEntity.MobileParty.IsTransitionInProgress))
				{
					this.RemoveVisualFromVisualsOfEntities();
					if (base.MapEntity.MobileParty.IsTransitionInProgress)
					{
						if (base.MapEntity.MobileParty.Army == null || base.MapEntity.MobileParty.Army.LeaderParty == base.MapEntity.MobileParty || base.MapEntity.MobileParty.AttachedTo == null)
						{
							this.AddMobileIconComponents(base.MapEntity, ref flag, ref flag2);
						}
						if (!this._isInTransitionProgressCached)
						{
							this.AddVisualToVisualsOfEntities();
							this.OnTransitionStarted();
						}
					}
					if (base.MapEntity.MobileParty.IsTransitionInProgress != this._isInTransitionProgressCached)
					{
						if (this._isInTransitionProgressCached)
						{
							this.OnTransitionEnded();
						}
						else
						{
							this.OnTransitionStarted();
						}
					}
				}
				else
				{
					this.AddVisualToVisualsOfEntities();
					this.InitializePartyCollider(base.MapEntity);
					this.AddMobileIconComponents(base.MapEntity, ref flag, ref flag2);
				}
				IL_3FB:
				if (flag)
				{
					this._cachedBannerComponent = new ValueTuple<string, GameEntityComponent>(null, null);
				}
				if (flag2)
				{
					this._cachedBannerEntity = new ValueTuple<string, GameEntity>(null, null);
				}
				this.StrategicEntity.CheckResources(true, false);
				if (this.IsMobileEntity)
				{
					this._isInTransitionProgressCached = base.MapEntity.MobileParty.IsTransitionInProgress;
				}
			}
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x0001FCD8 File Offset: 0x0001DED8
		private void AddMobileIconComponents(PartyBase party, ref bool clearBannerComponentCache, ref bool clearBannerEntityCache)
		{
			uint contourColor = (FactionManager.IsAtWarAgainstFaction(party.MapFaction, Hero.MainHero.MapFaction) ? 4294905856U : 4278206719U);
			if (this.IsPartOfBesiegerCamp(party))
			{
				this.AddTentEntityForParty(this.StrategicEntity, party, ref clearBannerComponentCache);
				return;
			}
			if (PartyBaseHelper.GetVisualPartyLeader(party) != null)
			{
				string bannerKey = null;
				Hero leaderHero = party.LeaderHero;
				if (((leaderHero != null) ? leaderHero.ClanBanner : null) != null)
				{
					bannerKey = party.LeaderHero.ClanBanner.BannerCode;
				}
				ActionIndexCache act_none = ActionIndexCache.act_none;
				ActionIndexCache act_none2 = ActionIndexCache.act_none;
				MapEvent mapEvent = ((party.MobileParty.Army != null && party.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(party.MobileParty)) ? party.MobileParty.Army.LeaderParty.MapEvent : party.MapEvent);
				int wieldedItemIndex;
				this.GetMeleeWeaponToWield(party, out wieldedItemIndex);
				if (mapEvent != null && (mapEvent.EventType == MapEvent.BattleTypes.FieldBattle || mapEvent.EventType == MapEvent.BattleTypes.Raid || mapEvent.EventType == MapEvent.BattleTypes.SiegeOutside || mapEvent.EventType == MapEvent.BattleTypes.SallyOut))
				{
					MobilePartyVisual.GetPartyBattleAnimation(party, wieldedItemIndex, out act_none, out act_none2);
				}
				IFaction mapFaction = party.MapFaction;
				uint teamColor = ((mapFaction != null) ? mapFaction.Color : 4291609515U);
				IFaction mapFaction2 = party.MapFaction;
				uint teamColor2 = ((mapFaction2 != null) ? mapFaction2.Color2 : 4291609515U);
				this.AddCharacterToPartyIcon(party, PartyBaseHelper.GetVisualPartyLeader(party), contourColor, bannerKey, wieldedItemIndex, teamColor, teamColor2, act_none, act_none2, MBRandom.NondeterministicRandomFloat * 0.7f, ref clearBannerEntityCache);
				if (party.IsMobile)
				{
					string text;
					string harnessItemId;
					this.GetMountAndHarnessVisualIdsForPartyIcon(out text, out harnessItemId);
					if (!string.IsNullOrEmpty(text))
					{
						this.AddMountToPartyIcon(new Vec3(0.3f, -0.25f, 0f, -1f), text, harnessItemId, contourColor, PartyBaseHelper.GetVisualPartyLeader(party));
					}
				}
			}
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x0001FE80 File Offset: 0x0001E080
		private void AddMountToPartyIcon(Vec3 positionOffset, string mountItemId, string harnessItemId, uint contourColor, CharacterObject character)
		{
			ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>(mountItemId);
			Monster monster = @object.HorseComponent.Monster;
			ItemObject item = null;
			if (!string.IsNullOrEmpty(harnessItemId))
			{
				item = Game.Current.ObjectManager.GetObject<ItemObject>(harnessItemId);
			}
			Equipment equipment = new Equipment();
			equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(@object, null, null, false);
			equipment[EquipmentIndex.HorseHarness] = new EquipmentElement(item, null, null, false);
			AgentVisualsData agentVisualsData = new AgentVisualsData().Equipment(equipment).Scale(@object.ScaleFactor * 0.3f);
			Mat3 identity = Mat3.Identity;
			AgentVisualsData data = agentVisualsData.Frame(new MatrixFrame(ref identity, ref positionOffset)).ActionSet(MBGlobals.GetActionSet(monster.ActionSetCode + "_map")).Scene(this.MapScene)
				.Monster(monster)
				.PrepareImmediately(false)
				.UseScaledWeapons(true)
				.HasClippingPlane(true)
				.MountCreationKey(MountCreationKey.GetRandomMountKeyString(@object, character.GetMountKeySeed()));
			this.CaravanMountAgentVisuals = AgentVisuals.Create(data, "PartyIcon " + mountItemId, false, false, false);
			this.CaravanMountAgentVisuals.GetEntity().SetContourColor(new uint?(contourColor), false);
			MatrixFrame matrixFrame = this.CaravanMountAgentVisuals.GetFrame();
			matrixFrame.rotation.ApplyScaleLocal(this.CaravanMountAgentVisuals.GetScale());
			matrixFrame = this.StrategicEntity.GetFrame().TransformToParent(matrixFrame);
			this.CaravanMountAgentVisuals.GetEntity().SetFrame(ref matrixFrame, true);
			float speed = MathF.Min(0.325f * this._speed / 0.3f, 20f);
			this.CaravanMountAgentVisuals.Tick(null, 0.0001f, this.IsEntityMovingVisually(), speed);
			this.CaravanMountAgentVisuals.GetEntity().Skeleton.ForceUpdateBoneFrames();
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x00020040 File Offset: 0x0001E240
		private void AddCharacterToPartyIcon(PartyBase party, CharacterObject characterObject, uint contourColor, string bannerKey, int wieldedItemIndex, uint teamColor1, uint teamColor2, in ActionIndexCache leaderAction, in ActionIndexCache mountAction, float animationStartDuration, ref bool clearBannerEntityCache)
		{
			Equipment equipment = characterObject.Equipment.Clone(false);
			bool flag = !string.IsNullOrEmpty(bannerKey) && (((characterObject.IsPlayerCharacter || characterObject.HeroObject.Clan == Clan.PlayerClan) && Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.BannerEligibleTier) || (!characterObject.IsPlayerCharacter && (!characterObject.IsHero || (characterObject.IsHero && characterObject.HeroObject.Clan != Clan.PlayerClan))));
			int leftWieldedItemIndex = 4;
			if (flag)
			{
				ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>("campaign_banner_small");
				equipment[EquipmentIndex.ExtraWeaponSlot] = new EquipmentElement(@object, null, null, false);
			}
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterObject.Race);
			MBActionSet actionSetWithSuffix = MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterObject.IsFemale, flag ? "_map_with_banner" : "_map");
			AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(true).Equipment(equipment).BodyProperties(characterObject.GetBodyProperties(characterObject.Equipment, -1))
				.SkeletonType(characterObject.IsFemale ? SkeletonType.Female : SkeletonType.Male)
				.Scale(0.3f)
				.Frame(this.StrategicEntity.GetFrame())
				.ActionSet(actionSetWithSuffix)
				.Scene(this.MapScene)
				.Monster(baseMonsterFromRace)
				.PrepareImmediately(false)
				.RightWieldedItemIndex(wieldedItemIndex)
				.HasClippingPlane(true)
				.UseScaledWeapons(true)
				.ClothColor1(teamColor1)
				.ClothColor2(teamColor2)
				.CharacterObjectStringId(characterObject.StringId)
				.AddColorRandomness(!characterObject.IsHero)
				.Race(characterObject.Race);
			if (flag)
			{
				Banner banner = new Banner(bannerKey);
				agentVisualsData.Banner(banner).LeftWieldedItemIndex(leftWieldedItemIndex);
				if (this._cachedBannerEntity.Item1 == bannerKey + "campaign_banner_small")
				{
					agentVisualsData.CachedWeaponEntity(EquipmentIndex.ExtraWeaponSlot, this._cachedBannerEntity.Item2);
				}
			}
			if (!party.MobileParty.IsCurrentlyAtSea || party.MobileParty.IsTransitionInProgress)
			{
				this.HumanAgentVisuals = AgentVisuals.Create(agentVisualsData, "PartyIcon " + characterObject.Name, false, false, false);
			}
			if (this.HumanAgentVisuals != null)
			{
				if (flag)
				{
					GameEntity entity = this.HumanAgentVisuals.GetEntity();
					GameEntity child = entity.GetChild(entity.ChildCount - 1);
					if (child.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > 0)
					{
						clearBannerEntityCache = false;
						this._cachedBannerEntity = new ValueTuple<string, GameEntity>(bannerKey + "campaign_banner_small", child);
					}
				}
				if (leaderAction != ActionIndexCache.act_none)
				{
					float actionAnimationDuration = MBActionSet.GetActionAnimationDuration(actionSetWithSuffix, leaderAction);
					if (actionAnimationDuration < 1f)
					{
						this.HumanAgentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, leaderAction, animationStartDuration, -0.2f, true, 0f);
					}
					else
					{
						this.HumanAgentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, leaderAction, animationStartDuration / actionAnimationDuration, -0.2f, true, 0f);
					}
				}
			}
			if (characterObject.HasMount() && (!party.MobileParty.IsCurrentlyAtSea || party.MobileParty.IsTransitionInProgress))
			{
				Monster monster = characterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent.Monster;
				MBActionSet actionSet = MBGlobals.GetActionSet(monster.ActionSetCode + "_map");
				AgentVisualsData agentVisualsData2 = new AgentVisualsData().Equipment(characterObject.Equipment).Scale(characterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].Item.ScaleFactor * 0.3f).Frame(MatrixFrame.Identity)
					.ActionSet(actionSet)
					.Scene(this.MapScene)
					.Monster(monster)
					.PrepareImmediately(false)
					.UseScaledWeapons(true)
					.HasClippingPlane(true)
					.MountCreationKey(MountCreationKey.GetRandomMountKeyString(characterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, characterObject.GetMountKeySeed()));
				this.MountAgentVisuals = AgentVisuals.Create(agentVisualsData2, "PartyIcon " + characterObject.Name + " mount", false, false, false);
				if (mountAction != ActionIndexCache.act_none)
				{
					float actionAnimationDuration2 = MBActionSet.GetActionAnimationDuration(actionSet, mountAction);
					if (actionAnimationDuration2 < 1f)
					{
						this.MountAgentVisuals.GetWeakEntity().Skeleton.SetAgentActionChannel(0, mountAction, animationStartDuration, -0.2f, true, 0f);
					}
					else
					{
						this.MountAgentVisuals.GetWeakEntity().Skeleton.SetAgentActionChannel(0, mountAction, animationStartDuration / actionAnimationDuration2, -0.2f, true, 0f);
					}
				}
				this.MountAgentVisuals.GetWeakEntity().SetContourColor(new uint?(contourColor), false);
				MatrixFrame frame = this.StrategicEntity.GetFrame();
				frame.rotation.ApplyScaleLocal(agentVisualsData2.ScaleData);
				this.MountAgentVisuals.GetWeakEntity().SetFrame(ref frame, true);
			}
			float num = ((this.MountAgentVisuals != null) ? 1.3f : 1f);
			float speed = MathF.Min(0.25f * num * this._speed / 0.3f, 20f);
			if (this.MountAgentVisuals != null)
			{
				this.MountAgentVisuals.Tick(null, 0.0001f, this.IsEntityMovingVisually(), speed);
				this.MountAgentVisuals.GetWeakEntity().Skeleton.ForceUpdateBoneFrames();
			}
			if (this.HumanAgentVisuals != null)
			{
				WeakGameEntity weakEntity = this.HumanAgentVisuals.GetWeakEntity();
				weakEntity.SetContourColor(new uint?(contourColor), false);
				MatrixFrame frame2 = this.StrategicEntity.GetFrame();
				frame2.rotation.ApplyScaleLocal(agentVisualsData.ScaleData);
				weakEntity.SetFrame(ref frame2, true);
				this.HumanAgentVisuals.Tick(this.MountAgentVisuals, 0.0001f, this.IsEntityMovingVisually(), speed);
				weakEntity.Skeleton.ForceUpdateBoneFrames();
			}
		}

		// Token: 0x06000410 RID: 1040 RVA: 0x00020604 File Offset: 0x0001E804
		private bool IsEntityMovingVisually()
		{
			if (base.MapEntity.IsMobile && base.MapEntity.MapEvent != null)
			{
				this._isEntityMovingCache = false;
			}
			else
			{
				if (Campaign.Current.CampaignDt <= 0f)
				{
					MobileParty mobileParty = base.MapEntity.MobileParty;
					if (mobileParty == null || !mobileParty.IsMainParty || !Campaign.Current.IsMainPartyWaiting)
					{
						goto IL_AF;
					}
				}
				this._isEntityMovingCache = false;
				MobileParty mobileParty2 = base.MapEntity.MobileParty;
				if (mobileParty2 != null && !mobileParty2.VisualPosition2DWithoutError.NearlyEquals(this._lastFrameVisualPositionWithoutError, 1E-05f))
				{
					this._lastFrameVisualPositionWithoutError = base.MapEntity.MobileParty.VisualPosition2DWithoutError;
					this._isEntityMovingCache = true;
				}
			}
			IL_AF:
			if (this._isInTransitionProgressCached)
			{
				this._isEntityMovingCache = true;
			}
			return this._isEntityMovingCache;
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x000206D8 File Offset: 0x0001E8D8
		public static MetaMesh GetBannerOfCharacter(Banner banner, string bannerMeshName)
		{
			MetaMesh copy = MetaMesh.GetCopy(bannerMeshName, true, false);
			for (int i = 0; i < copy.MeshCount; i++)
			{
				Mesh meshAtIndex = copy.GetMeshAtIndex(i);
				if (!meshAtIndex.HasTag("dont_use_tableau"))
				{
					Material material = meshAtIndex.GetMaterial();
					Material tableauMaterial = null;
					Tuple<Material, Banner> key = new Tuple<Material, Banner>(material, banner);
					if (MapScreen.Instance.CharacterBannerMaterialCache.ContainsKey(key))
					{
						tableauMaterial = MapScreen.Instance.CharacterBannerMaterialCache[key];
					}
					else
					{
						tableauMaterial = material.CreateCopy();
						Action<Texture> setAction = delegate(Texture tex)
						{
							tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
							uint num = (uint)tableauMaterial.GetShader().GetMaterialShaderFlagMask("use_tableau_blending", true);
							ulong shaderFlags = tableauMaterial.GetShaderFlags();
							tableauMaterial.SetShaderFlags(shaderFlags | (ulong)num);
						};
						BannerDebugInfo bannerDebugInfo = BannerDebugInfo.CreateManual("MobilePartyVisual");
						banner.GetTableauTextureLarge(bannerDebugInfo, setAction);
						MapScreen.Instance.CharacterBannerMaterialCache[key] = tableauMaterial;
					}
					meshAtIndex.SetMaterial(tableauMaterial);
				}
			}
			return copy;
		}

		// Token: 0x06000412 RID: 1042 RVA: 0x000207C0 File Offset: 0x0001E9C0
		public void AddTentEntityForParty(GameEntity strategicEntity, PartyBase party, ref bool clearBannerComponentCache)
		{
			GameEntity gameEntity = GameEntity.CreateEmpty(strategicEntity.Scene, true, true, true);
			gameEntity.AddMultiMesh(MetaMesh.GetCopy("map_icon_siege_camp_tent", true, false), true);
			MatrixFrame identity = MatrixFrame.Identity;
			identity.rotation.ApplyScaleLocal(1.2f);
			gameEntity.SetFrame(ref identity, true);
			string text = null;
			Hero leaderHero = party.LeaderHero;
			if (((leaderHero != null) ? leaderHero.ClanBanner : null) != null)
			{
				text = party.LeaderHero.ClanBanner.BannerCode;
			}
			bool flag = party.MobileParty.Army != null && party.MobileParty.Army.LeaderParty == party.MobileParty;
			MatrixFrame identity2 = MatrixFrame.Identity;
			identity2.origin.z = identity2.origin.z + (flag ? 0.2f : 0.15f);
			identity2.rotation.RotateAboutUp(1.5707964f);
			float scaleAmount = MBMath.Map(party.CalculateCurrentStrength() / 500f * ((party.MobileParty.Army != null && flag) ? 1f : 0.8f), 0f, 1f, 0.15f, 0.5f);
			identity2.rotation.ApplyScaleLocal(scaleAmount);
			if (!string.IsNullOrEmpty(text))
			{
				clearBannerComponentCache = false;
				string text2 = "campaign_flag";
				if (this._cachedBannerComponent.Item1 == text + text2)
				{
					this._cachedBannerComponent.Item2.GetFirstMetaMesh().Frame = identity2;
					strategicEntity.AddComponent(this._cachedBannerComponent.Item2);
				}
				else
				{
					MetaMesh bannerOfCharacter = MobilePartyVisual.GetBannerOfCharacter(new Banner(text), text2);
					bannerOfCharacter.Frame = identity2;
					int componentCount = gameEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator);
					gameEntity.AddMultiMesh(bannerOfCharacter, true);
					if (gameEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > componentCount)
					{
						this._cachedBannerComponent.Item1 = text + text2;
						this._cachedBannerComponent.Item2 = gameEntity.GetComponentAtIndex(componentCount, GameEntity.ComponentType.ClothSimulator);
					}
				}
			}
			strategicEntity.AddChild(gameEntity, false);
			gameEntity.SetVisibilityExcludeParents(true);
		}

		// Token: 0x06000413 RID: 1043 RVA: 0x000209AE File Offset: 0x0001EBAE
		internal void ClearVisualMemory()
		{
			this.ResetPartyIcon();
			base.MapEntity.SetVisualAsDirty();
			this._cachedBannerEntity = new ValueTuple<string, GameEntity>(null, null);
		}

		// Token: 0x06000414 RID: 1044 RVA: 0x000209D0 File Offset: 0x0001EBD0
		private void GetMeleeWeaponToWield(PartyBase party, out int wieldedItemIndex)
		{
			wieldedItemIndex = -1;
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(party);
			if (visualPartyLeader != null)
			{
				for (int i = 0; i < 5; i++)
				{
					if (visualPartyLeader.Equipment[i].Item != null && visualPartyLeader.Equipment[i].Item.PrimaryWeapon.IsMeleeWeapon)
					{
						wieldedItemIndex = i;
						return;
					}
				}
			}
		}

		// Token: 0x06000415 RID: 1045 RVA: 0x00020A30 File Offset: 0x0001EC30
		private static void GetPartyBattleAnimation(PartyBase party, int wieldedItemIndex, out ActionIndexCache leaderAction, out ActionIndexCache mountAction)
		{
			leaderAction = ActionIndexCache.act_none;
			mountAction = ActionIndexCache.act_none;
			if (party.MobileParty.Army == null || !party.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(party.MobileParty))
			{
				MapEvent mapEvent = party.MapEvent;
			}
			else
			{
				MapEvent mapEvent2 = party.MobileParty.Army.LeaderParty.MapEvent;
			}
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(party);
			MapEvent mapEvent3 = party.MapEvent;
			if (((mapEvent3 != null) ? mapEvent3.MapEventSettlement : null) != null && visualPartyLeader != null && !visualPartyLeader.HasMount())
			{
				leaderAction = ActionIndexCache.act_map_raid;
				return;
			}
			if (wieldedItemIndex > -1 && ((visualPartyLeader != null) ? visualPartyLeader.Equipment[wieldedItemIndex].Item : null) != null)
			{
				WeaponComponent weaponComponent = visualPartyLeader.Equipment[wieldedItemIndex].Item.WeaponComponent;
				if (weaponComponent != null && weaponComponent.PrimaryWeapon.IsMeleeWeapon)
				{
					if (visualPartyLeader.HasMount())
					{
						if (visualPartyLeader.Equipment[10].Item.HorseComponent.Monster.MonsterUsage == "camel")
						{
							if (weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.OneHandedWeapon || weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.TwoHandedWeapon)
							{
								leaderAction = ActionIndexCache.act_map_rider_camel_attack_1h;
								mountAction = ActionIndexCache.act_map_mount_attack_1h;
							}
							else if (weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.Polearm)
							{
								if (weaponComponent.PrimaryWeapon.SwingDamageType == DamageTypes.Invalid)
								{
									leaderAction = ActionIndexCache.act_map_rider_camel_attack_1h_spear;
									mountAction = ActionIndexCache.act_map_mount_attack_spear;
								}
								else if (weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.TwoHandedPolearm)
								{
									leaderAction = ActionIndexCache.act_map_rider_camel_attack_1h_swing;
									mountAction = ActionIndexCache.act_map_mount_attack_swing;
								}
								else
								{
									leaderAction = ActionIndexCache.act_map_rider_camel_attack_2h_swing;
									mountAction = ActionIndexCache.act_map_mount_attack_swing;
								}
							}
						}
						else if (weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.OneHandedWeapon || weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.TwoHandedWeapon)
						{
							leaderAction = ActionIndexCache.act_map_rider_horse_attack_1h;
							mountAction = ActionIndexCache.act_map_mount_attack_1h;
						}
						else if (weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.Polearm)
						{
							if (weaponComponent.PrimaryWeapon.SwingDamageType == DamageTypes.Invalid)
							{
								leaderAction = ActionIndexCache.act_map_rider_horse_attack_1h_spear;
								mountAction = ActionIndexCache.act_map_mount_attack_spear;
							}
							else if (weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.TwoHandedPolearm)
							{
								leaderAction = ActionIndexCache.act_map_rider_horse_attack_1h_swing;
								mountAction = ActionIndexCache.act_map_mount_attack_swing;
							}
							else
							{
								leaderAction = ActionIndexCache.act_map_rider_horse_attack_2h_swing;
								mountAction = ActionIndexCache.act_map_mount_attack_swing;
							}
						}
					}
					else if (weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.OneHandedAxe || weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.Mace || weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.OneHandedSword)
					{
						leaderAction = ActionIndexCache.act_map_attack_1h;
					}
					else if (weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.TwoHandedAxe || weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.TwoHandedMace || weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.TwoHandedSword)
					{
						leaderAction = ActionIndexCache.act_map_attack_2h;
					}
					else if (weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.OneHandedPolearm || weaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.TwoHandedPolearm)
					{
						leaderAction = ActionIndexCache.act_map_attack_spear_1h_or_2h;
					}
				}
			}
			if (leaderAction == ActionIndexCache.act_none)
			{
				if (visualPartyLeader.HasMount())
				{
					HorseComponent horseComponent = visualPartyLeader.Equipment[10].Item.HorseComponent;
					leaderAction = ((horseComponent.Monster.MonsterUsage == "camel") ? ActionIndexCache.act_map_rider_camel_attack_unarmed : ActionIndexCache.act_map_rider_horse_attack_unarmed);
					mountAction = ActionIndexCache.act_map_mount_attack_unarmed;
					return;
				}
				leaderAction = ActionIndexCache.act_map_attack_unarmed;
			}
		}

		// Token: 0x06000416 RID: 1046 RVA: 0x00020DB7 File Offset: 0x0001EFB7
		private void GetMountAndHarnessVisualIdsForPartyIcon(out string mountStringId, out string harnessStringId)
		{
			mountStringId = "";
			harnessStringId = "";
			if (base.MapEntity.IsMobile)
			{
				PartyComponent partyComponent = base.MapEntity.MobileParty.PartyComponent;
				if (partyComponent == null)
				{
					return;
				}
				partyComponent.GetMountAndHarnessVisualIdsForPartyIcon(base.MapEntity, out mountStringId, out harnessStringId);
			}
		}

		// Token: 0x06000417 RID: 1047 RVA: 0x00020DF8 File Offset: 0x0001EFF8
		private void InitializePartyCollider(PartyBase party)
		{
			if (this.StrategicEntity != null && party.IsMobile)
			{
				this.StrategicEntity.AddSphereAsBody(new Vec3(0f, 0f, 0f, -1f), 0.5f, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			}
		}

		// Token: 0x06000418 RID: 1048 RVA: 0x00020E4C File Offset: 0x0001F04C
		private void ResetPartyIcon()
		{
			if (this.HumanAgentVisuals != null)
			{
				this.HumanAgentVisuals.Reset();
				this.HumanAgentVisuals = null;
			}
			if (this.MountAgentVisuals != null)
			{
				this.MountAgentVisuals.Reset();
				this.MountAgentVisuals = null;
			}
			if (this.CaravanMountAgentVisuals != null)
			{
				this.CaravanMountAgentVisuals.Reset();
				this.CaravanMountAgentVisuals = null;
			}
			if (this.StrategicEntity != null)
			{
				if ((this.StrategicEntity.EntityFlags & EntityFlags.Ignore) != (EntityFlags)0U)
				{
					this.StrategicEntity.RemoveFromPredisplayEntity();
				}
				this.StrategicEntity.ClearComponents();
			}
			this._bearingRotation = base.MapEntity.MobileParty.Bearing.RotationInRadians;
			MobilePartyVisualManager.Current.UnRegisterFadingVisual(this);
		}

		// Token: 0x06000419 RID: 1049 RVA: 0x00020F08 File Offset: 0x0001F108
		private float GetTransitionProgress()
		{
			if (this.IsMobileEntity && base.MapEntity.MobileParty.IsTransitionInProgress && base.MapEntity.MobileParty.NavigationTransitionDuration != CampaignTime.Zero)
			{
				float num = (float)base.MapEntity.MobileParty.NavigationTransitionDuration.ToHours;
				Army army = base.MapEntity.MobileParty.Army;
				if (((army != null) ? army.LeaderParty : null) == base.MapEntity.MobileParty && base.MapEntity.MobileParty.AttachedParties.Count > 0)
				{
					float val = base.MapEntity.MobileParty.AttachedParties.MaxQ((MobileParty x) => (float)x.NavigationTransitionDuration.ToHours);
					num = Math.Max(num, val);
				}
				return MBMath.ClampFloat(base.MapEntity.MobileParty.NavigationTransitionStartTime.ElapsedHoursUntilNow / num, 0f, 1f);
			}
			return 1f;
		}

		// Token: 0x0600041A RID: 1050 RVA: 0x0002101C File Offset: 0x0001F21C
		private void OnTransitionStarted()
		{
			MobilePartyVisualManager.Current.RegisterFadingVisual(this);
			this._transitionStartRotation = (base.MapEntity.MobileParty.EndPositionForNavigationTransition.ToVec2() - base.MapEntity.Position.ToVec2()).RotationInRadians;
		}

		// Token: 0x0600041B RID: 1051 RVA: 0x00021072 File Offset: 0x0001F272
		private void OnTransitionEnded()
		{
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x00021074 File Offset: 0x0001F274
		private float GetVisualRotation()
		{
			if (base.MapEntity.IsMobile && base.MapEntity.MapEvent != null && base.MapEntity.MapEvent.IsFieldBattle)
			{
				return this.GetMapEventVisualRotation();
			}
			if (base.MapEntity.IsMobile && base.MapEntity.MobileParty.IsTransitionInProgress)
			{
				return this._transitionStartRotation;
			}
			return this._bearingRotation;
		}

		// Token: 0x0600041D RID: 1053 RVA: 0x000210E0 File Offset: 0x0001F2E0
		private float GetMapEventVisualRotation()
		{
			if (base.MapEntity.MapEventSide.OtherSide.LeaderParty != null && base.MapEntity.MapEventSide.OtherSide.LeaderParty.IsMobile && base.MapEntity.MapEventSide.OtherSide.LeaderParty.IsMobile)
			{
				return (base.MapEntity.MapEventSide.OtherSide.LeaderParty.MobileParty.VisualPosition2DWithoutError - base.MapEntity.MobileParty.VisualPosition2DWithoutError).Normalized().RotationInRadians;
			}
			return this._bearingRotation;
		}

		// Token: 0x0600041E RID: 1054 RVA: 0x00021187 File Offset: 0x0001F387
		private void AddVisualToVisualsOfEntities()
		{
			if (!MapScreen.VisualsOfEntities.ContainsKey(this.StrategicEntity.Pointer))
			{
				MapScreen.VisualsOfEntities.Add(this.StrategicEntity.Pointer, this);
			}
		}

		// Token: 0x0600041F RID: 1055 RVA: 0x000211B8 File Offset: 0x0001F3B8
		private void RemoveVisualFromVisualsOfEntities()
		{
			MapScreen.VisualsOfEntities.Remove(this.StrategicEntity.Pointer);
			foreach (GameEntity gameEntity in this.StrategicEntity.GetChildren())
			{
				MapScreen.VisualsOfEntities.Remove(gameEntity.Pointer);
			}
		}

		// Token: 0x06000420 RID: 1056 RVA: 0x0002122C File Offset: 0x0001F42C
		private bool IsPartOfBesiegerCamp(PartyBase party)
		{
			Settlement besiegedSettlement = party.MobileParty.BesiegedSettlement;
			return ((besiegedSettlement != null) ? besiegedSettlement.SiegeEvent : null) != null && party.MobileParty.BesiegedSettlement.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(party, MapEvent.BattleTypes.Siege);
		}

		// Token: 0x040001FA RID: 506
		private const float PartyScale = 0.3f;

		// Token: 0x040001FB RID: 507
		private const float HorseAnimationSpeedFactor = 1.3f;

		// Token: 0x040001FC RID: 508
		private float _speed;

		// Token: 0x040001FD RID: 509
		private float _entityAlpha;

		// Token: 0x040001FE RID: 510
		private float _transitionStartRotation;

		// Token: 0x040001FF RID: 511
		private Vec2 _lastFrameVisualPositionWithoutError;

		// Token: 0x04000200 RID: 512
		private bool _isEntityMovingCache;

		// Token: 0x04000201 RID: 513
		private bool _isInTransitionProgressCached;

		// Token: 0x04000202 RID: 514
		private float _bearingRotation;

		// Token: 0x04000203 RID: 515
		private ValueTuple<string, GameEntityComponent> _cachedBannerComponent;

		// Token: 0x04000204 RID: 516
		private ValueTuple<string, GameEntity> _cachedBannerEntity;

		// Token: 0x04000205 RID: 517
		private Scene _mapScene;
	}
}
