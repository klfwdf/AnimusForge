using System;
using System.Threading;
using Helpers;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View.Map.Visuals;

public class MobilePartyVisual : MapEntityVisual<PartyBase>
{
	private const float PartyScale = 0.3f;

	private const float HorseAnimationSpeedFactor = 1.3f;

	private float _speed;

	private float _entityAlpha;

	private float _transitionStartRotation;

	private Vec2 _lastFrameVisualPositionWithoutError;

	private bool _isEntityMovingCache;

	private bool _isInTransitionProgressCached;

	private float _bearingRotation;

	private (string, GameEntityComponent) _cachedBannerComponent;

	private (string, GameEntity) _cachedBannerEntity;

	private Scene _mapScene;

	public override float BearingRotation => _bearingRotation;

	private Scene MapScene
	{
		get
		{
			if (_mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
			{
				_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			}
			return _mapScene;
		}
	}

	public override MapEntityVisual AttachedTo
	{
		get
		{
			if (base.MapEntity.MobileParty?.AttachedTo != null)
			{
				return MobilePartyVisualManager.Current.GetVisualOfEntity(base.MapEntity.MobileParty.AttachedTo.Party);
			}
			return null;
		}
	}

	public override CampaignVec2 InteractionPositionForPlayer => ((IInteractablePoint)base.MapEntity).GetInteractionPosition(MobileParty.MainParty);

	public override bool IsMobileEntity => base.MapEntity.IsMobile;

	public override bool IsMainEntity => base.MapEntity == PartyBase.MainParty;

	public GameEntity StrategicEntity { get; private set; }

	public AgentVisuals HumanAgentVisuals { get; private set; }

	public AgentVisuals MountAgentVisuals { get; private set; }

	public AgentVisuals CaravanMountAgentVisuals { get; private set; }

	public MobilePartyVisual(PartyBase partyBase)
		: base(partyBase)
	{
		CircleLocalFrame = MatrixFrame.Identity;
	}

	public override bool IsEnemyOf(IFaction faction)
	{
		return FactionManager.IsAtWarAgainstFaction(base.MapEntity.MapFaction, faction.MapFaction);
	}

	public override bool IsInSameFaction(IFaction faction)
	{
		return DiplomacyHelper.IsSameFactionAndNotEliminated(base.MapEntity.MapFaction, faction.MapFaction);
	}

	public override bool IsAllyOf(IFaction faction)
	{
		return DiplomacyHelper.HasAllianceWithFaction(base.MapEntity.MapFaction, faction.MapFaction);
	}

	internal void OnPartyRemoved()
	{
		if (StrategicEntity != null)
		{
			RemoveVisualFromVisualsOfEntities();
			ReleaseResources();
			StrategicEntity.Remove(111);
		}
	}

	public override void OnTrackAction()
	{
		MobileParty mobileParty = base.MapEntity.MobileParty;
		if (mobileParty != null)
		{
			if (Campaign.Current.VisualTrackerManager.CheckTracked(mobileParty))
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(mobileParty);
			}
			else
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(mobileParty);
			}
		}
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		MobileParty.NavigationType navigationType;
		if (IsMainEntity)
		{
			MobileParty.MainParty.SetMoveModeHold();
		}
		else if (base.MapEntity.MobileParty.IsCurrentlyAtSea == MobileParty.MainParty.IsCurrentlyAtSea && NavigationHelper.CanPlayerNavigateToPosition(base.MapEntity.MobileParty.Position, out navigationType))
		{
			if (followModifierUsed)
			{
				MobileParty.MainParty.SetMoveEscortParty(base.MapEntity.MobileParty, navigationType, isTargetingPort: false);
			}
			else
			{
				MobileParty.MainParty.SetMoveEngageParty(base.MapEntity.MobileParty, navigationType);
			}
		}
		return true;
	}

	public override void OnHover()
	{
		if (base.MapEntity.MapEvent != null)
		{
			InformationManager.ShowTooltip(typeof(MapEvent), base.MapEntity.MapEvent);
		}
		else
		{
			if (!base.MapEntity.IsMobile || !base.MapEntity.IsVisible)
			{
				return;
			}
			if (base.MapEntity.MobileParty.Army != null && base.MapEntity.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(base.MapEntity.MobileParty))
			{
				if (base.MapEntity.MobileParty.Army.LeaderParty.SiegeEvent != null)
				{
					InformationManager.ShowTooltip(typeof(SiegeEvent), base.MapEntity.MobileParty.Army.LeaderParty.SiegeEvent);
					return;
				}
				InformationManager.ShowTooltip(typeof(Army), base.MapEntity.MobileParty.Army, false, true);
			}
			else if (base.MapEntity.MobileParty.SiegeEvent != null)
			{
				InformationManager.ShowTooltip(typeof(SiegeEvent), base.MapEntity.MobileParty.SiegeEvent);
			}
			else
			{
				InformationManager.ShowTooltip(typeof(MobileParty), base.MapEntity.MobileParty, false, true);
			}
		}
	}

	public override Vec3 GetVisualPosition()
	{
		return base.MapEntity.MobileParty.VisualPosition2DWithoutError.ToVec3(base.MapEntity.Position.AsVec3().Z);
	}

	public override void ReleaseResources()
	{
		ResetPartyIcon();
	}

	public override bool IsVisibleOrFadingOut()
	{
		return _entityAlpha > 0f;
	}

	public override void OnOpenEncyclopedia()
	{
		if (base.MapEntity.MobileParty.IsLordParty && base.MapEntity.MobileParty.LeaderHero != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(base.MapEntity.MobileParty.LeaderHero.EncyclopediaLink);
		}
	}

	internal void Tick(float dt, float realDt, ref int dirtyPartiesCount, ref MobilePartyVisual[] dirtyPartiesList)
	{
		if (StrategicEntity == null)
		{
			return;
		}
		if (base.MapEntity.IsVisualDirty && (_entityAlpha > 0f || base.MapEntity.IsVisible))
		{
			int num = Interlocked.Increment(ref dirtyPartiesCount);
			dirtyPartiesList[num] = this;
		}
		if (!IsVisibleOrFadingOut() || !(StrategicEntity != null) || (base.MapEntity.MobileParty.IsCurrentlyAtSea && !base.MapEntity.MobileParty.IsTransitionInProgress))
		{
			return;
		}
		UpdateBearingRotation(realDt, dt);
		_speed = (base.MapEntity.MobileParty.IsActive ? base.MapEntity.MobileParty.Speed : 0f);
		float num2 = ((MountAgentVisuals != null) ? 1.3f : 1f);
		float speed = TaleWorlds.Library.MathF.Min(0.25f * num2 * _speed / 0.3f, 20f);
		bool isEntityMoving = IsEntityMovingVisually();
		HumanAgentVisuals?.Tick(MountAgentVisuals, dt, isEntityMoving, speed);
		MountAgentVisuals?.Tick(null, dt, isEntityMoving, speed);
		CaravanMountAgentVisuals?.Tick(null, dt, isEntityMoving, speed);
		MobileParty mobileParty = base.MapEntity.MobileParty;
		MatrixFrame frame = MatrixFrame.Identity;
		frame.origin = GetVisualPosition();
		if (mobileParty.Army != null && mobileParty.AttachedTo == mobileParty.Army.LeaderParty && (base.MapEntity.MapEvent == null || !base.MapEntity.MapEvent.IsFieldBattle))
		{
			MatrixFrame frame2 = StrategicEntity.GetFrame();
			Vec2 vec = frame.origin.AsVec2 - frame2.origin.AsVec2;
			if (vec.Length / dt > 20f)
			{
				frame.rotation.RotateAboutUp(_bearingRotation);
			}
			else if (mobileParty.CurrentSettlement == null)
			{
				float a = MBMath.LerpRadians(frame2.rotation.f.AsVec2.RotationInRadians, (vec + Vec2.FromRotation(_bearingRotation) * 0.01f).RotationInRadians, Math.Min(6f * dt, 1f), 0.03f * dt, 10f * dt);
				frame.rotation.RotateAboutUp(a);
			}
			else
			{
				float rotationInRadians = frame2.rotation.f.AsVec2.RotationInRadians;
				frame.rotation.RotateAboutUp(rotationInRadians);
			}
		}
		else if (mobileParty.CurrentSettlement == null)
		{
			frame.rotation.RotateAboutUp(GetVisualRotation());
		}
		if (!StrategicEntity.GetFrame().NearlyEquals(frame))
		{
			StrategicEntity.SetFrame(ref frame);
			if (HumanAgentVisuals != null)
			{
				MatrixFrame frame3 = frame;
				frame3.rotation.ApplyScaleLocal(HumanAgentVisuals.GetScale());
				HumanAgentVisuals.GetWeakEntity().SetFrame(ref frame3);
			}
			if (MountAgentVisuals != null)
			{
				MatrixFrame frame4 = frame;
				frame4.rotation.ApplyScaleLocal(MountAgentVisuals.GetScale());
				MountAgentVisuals.GetWeakEntity().SetFrame(ref frame4);
			}
			if (CaravanMountAgentVisuals != null)
			{
				MatrixFrame frame5 = frame.TransformToParent(CaravanMountAgentVisuals.GetFrame());
				frame5.rotation.ApplyScaleLocal(CaravanMountAgentVisuals.GetScale());
				CaravanMountAgentVisuals.GetWeakEntity().SetFrame(ref frame5);
			}
		}
		ApplyWindEffect();
	}

	private void ApplyWindEffect()
	{
		if (HumanAgentVisuals != null && !HumanAgentVisuals.GetEquipment()[EquipmentIndex.ExtraWeaponSlot].IsEmpty)
		{
			HumanAgentVisuals.SetClothWindToWeaponAtIndex(-StrategicEntity.GetGlobalFrame().rotation.f, isLocal: false, EquipmentIndex.ExtraWeaponSlot);
		}
		if (_cachedBannerComponent.Item2 != null && _cachedBannerComponent.Item2 is ClothSimulatorComponent clothSimulatorComponent)
		{
			clothSimulatorComponent.SetForcedWind(-StrategicEntity.GetGlobalFrame().rotation.f, isLocal: false);
		}
	}

	internal void OnStartup()
	{
		if (base.MapEntity.IsMobile)
		{
			StrategicEntity = GameEntity.CreateEmpty(MapScene);
			if (!base.MapEntity.IsVisible)
			{
				StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
			}
		}
		CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(base.MapEntity);
		if (0 == 0)
		{
			CircleLocalFrame = MatrixFrame.Identity;
			if ((visualPartyLeader != null && visualPartyLeader.HasMount()) || base.MapEntity.MobileParty.IsCaravan)
			{
				MatrixFrame circleLocalFrame = CircleLocalFrame;
				Mat3 rotation = circleLocalFrame.rotation;
				rotation.ApplyScaleLocal(0.4625f);
				circleLocalFrame.rotation = rotation;
				CircleLocalFrame = circleLocalFrame;
			}
			else
			{
				MatrixFrame circleLocalFrame2 = CircleLocalFrame;
				Mat3 rotation2 = circleLocalFrame2.rotation;
				rotation2.ApplyScaleLocal(0.3725f);
				circleLocalFrame2.rotation = rotation2;
				CircleLocalFrame = circleLocalFrame2;
			}
		}
		_bearingRotation = base.MapEntity.MobileParty.Bearing.RotationInRadians;
		StrategicEntity.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
		if (HumanAgentVisuals != null)
		{
			WeakGameEntity weakEntity = HumanAgentVisuals.GetWeakEntity();
			if (weakEntity != WeakGameEntity.Invalid)
			{
				weakEntity.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
			}
		}
		if (MountAgentVisuals != null)
		{
			WeakGameEntity weakEntity2 = MountAgentVisuals.GetWeakEntity();
			if (weakEntity2 != WeakGameEntity.Invalid)
			{
				weakEntity2.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
			}
		}
		if (CaravanMountAgentVisuals != null)
		{
			WeakGameEntity weakEntity3 = CaravanMountAgentVisuals.GetWeakEntity();
			if (weakEntity3 != WeakGameEntity.Invalid)
			{
				weakEntity3.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
			}
		}
		StrategicEntity.SetReadyToRender(ready: true);
		StrategicEntity.SetEntityEnvMapVisibility(value: false);
		_entityAlpha = 0f;
		if (base.MapEntity.IsVisible)
		{
			if (base.MapEntity.MobileParty.IsTransitionInProgress)
			{
				TickFadingState(0.1f, 0.1f);
			}
			else
			{
				_entityAlpha = 1f;
			}
		}
		AddVisualToVisualsOfEntities();
	}

	internal void TickFadingState(float realDt, float dt)
	{
		if ((!base.MapEntity.MobileParty.IsTransitionInProgress || !base.MapEntity.IsVisible) && ((_entityAlpha < 1f && base.MapEntity.IsVisible) || (_entityAlpha > 0f && !base.MapEntity.IsVisible)))
		{
			if (base.MapEntity.IsVisible)
			{
				if (_entityAlpha <= 0f)
				{
					StrategicEntity.SetVisibilityExcludeParents(visible: true);
					if (HumanAgentVisuals != null)
					{
						WeakGameEntity weakEntity = HumanAgentVisuals.GetWeakEntity();
						if (weakEntity != WeakGameEntity.Invalid)
						{
							weakEntity.SetVisibilityExcludeParents(visible: true);
						}
					}
					if (MountAgentVisuals != null)
					{
						WeakGameEntity weakEntity2 = MountAgentVisuals.GetWeakEntity();
						if (weakEntity2 != WeakGameEntity.Invalid)
						{
							weakEntity2.SetVisibilityExcludeParents(visible: true);
						}
					}
					if (CaravanMountAgentVisuals != null)
					{
						WeakGameEntity weakEntity3 = CaravanMountAgentVisuals.GetWeakEntity();
						if (weakEntity3 != WeakGameEntity.Invalid)
						{
							weakEntity3.SetVisibilityExcludeParents(visible: true);
						}
					}
				}
				_entityAlpha = TaleWorlds.Library.MathF.Min(_entityAlpha + TaleWorlds.Library.MathF.Max(realDt, 1E-05f), 1f);
				StrategicEntity.SetAlpha(_entityAlpha);
				if (HumanAgentVisuals != null)
				{
					WeakGameEntity weakEntity4 = HumanAgentVisuals.GetWeakEntity();
					if (weakEntity4 != WeakGameEntity.Invalid)
					{
						weakEntity4.SetAlpha(_entityAlpha);
					}
				}
				if (MountAgentVisuals != null)
				{
					WeakGameEntity weakEntity5 = MountAgentVisuals.GetWeakEntity();
					if (weakEntity5 != WeakGameEntity.Invalid)
					{
						weakEntity5.SetAlpha(_entityAlpha);
					}
				}
				if (CaravanMountAgentVisuals != null)
				{
					WeakGameEntity weakEntity6 = CaravanMountAgentVisuals.GetWeakEntity();
					if (weakEntity6 != WeakGameEntity.Invalid)
					{
						weakEntity6.SetAlpha(_entityAlpha);
					}
				}
				StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
				return;
			}
			_entityAlpha = TaleWorlds.Library.MathF.Max(_entityAlpha - TaleWorlds.Library.MathF.Max(realDt, 1E-05f), 0f);
			StrategicEntity.SetAlpha(_entityAlpha);
			if (HumanAgentVisuals != null)
			{
				WeakGameEntity weakEntity7 = HumanAgentVisuals.GetWeakEntity();
				if (weakEntity7 != WeakGameEntity.Invalid)
				{
					weakEntity7.SetAlpha(_entityAlpha);
				}
			}
			if (MountAgentVisuals != null)
			{
				WeakGameEntity weakEntity8 = MountAgentVisuals.GetWeakEntity();
				if (weakEntity8 != WeakGameEntity.Invalid)
				{
					weakEntity8.SetAlpha(_entityAlpha);
				}
			}
			if (CaravanMountAgentVisuals != null)
			{
				WeakGameEntity weakEntity9 = CaravanMountAgentVisuals.GetWeakEntity();
				if (weakEntity9 != WeakGameEntity.Invalid)
				{
					weakEntity9.SetAlpha(_entityAlpha);
				}
			}
			if (!(_entityAlpha <= 0f))
			{
				return;
			}
			StrategicEntity.SetVisibilityExcludeParents(visible: false);
			if (HumanAgentVisuals != null)
			{
				WeakGameEntity weakEntity10 = HumanAgentVisuals.GetWeakEntity();
				if (weakEntity10 != WeakGameEntity.Invalid)
				{
					weakEntity10.SetVisibilityExcludeParents(visible: false);
				}
			}
			if (MountAgentVisuals != null)
			{
				WeakGameEntity weakEntity11 = MountAgentVisuals.GetWeakEntity();
				if (weakEntity11 != WeakGameEntity.Invalid)
				{
					weakEntity11.SetVisibilityExcludeParents(visible: false);
				}
			}
			if (CaravanMountAgentVisuals != null)
			{
				WeakGameEntity weakEntity12 = CaravanMountAgentVisuals.GetWeakEntity();
				if (weakEntity12 != WeakGameEntity.Invalid)
				{
					weakEntity12.SetVisibilityExcludeParents(visible: false);
				}
			}
			StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
		}
		else if (base.MapEntity.MobileParty.IsTransitionInProgress)
		{
			if ((base.MapEntity.MobileParty.Army == null || base.MapEntity.MobileParty.Army.LeaderParty == base.MapEntity.MobileParty || base.MapEntity.MobileParty.AttachedTo == null) && IsMobileEntity && GetTransitionProgress() < 1f)
			{
				TickTransitionFadeState(dt);
			}
		}
		else
		{
			MobilePartyVisualManager.Current.UnRegisterFadingVisual(this);
		}
	}

	private void UpdateBearingRotation(float realDt, float dt)
	{
		float num = MBMath.WrapAngle(base.MapEntity.MobileParty.Bearing.RotationInRadians - _bearingRotation);
		float num2 = ((base.MapEntity.MapEvent != null) ? realDt : dt);
		_bearingRotation += num * TaleWorlds.Library.MathF.Min(num2 * 30f, 1f);
		_bearingRotation = MBMath.WrapAngle(_bearingRotation);
	}

	private void TickTransitionFadeState(float dt)
	{
		float transitionProgress = GetTransitionProgress();
		if (base.MapEntity.MobileParty.IsCurrentlyAtSea)
		{
			_entityAlpha = transitionProgress;
			HumanAgentVisuals?.GetEntity()?.SetAlpha(_entityAlpha);
			MountAgentVisuals?.GetEntity()?.SetAlpha(_entityAlpha);
			CaravanMountAgentVisuals?.GetEntity()?.SetAlpha(_entityAlpha);
			if (HumanAgentVisuals != null)
			{
				MatrixFrame frame = HumanAgentVisuals.GetEntity().GetFrame();
				CampaignVec2 campaignVec = base.MapEntity.MobileParty.EndPositionForNavigationTransition + base.MapEntity.MobileParty.ArmyPositionAdder;
				float x = TaleWorlds.Library.MathF.Lerp(frame.origin.X, campaignVec.X, dt);
				float y = TaleWorlds.Library.MathF.Lerp(frame.origin.Y, campaignVec.Y, dt);
				float z = TaleWorlds.Library.MathF.Lerp(frame.origin.z, campaignVec.AsVec3().Z, dt);
				frame.origin = new Vec3(x, y, z);
				HumanAgentVisuals.GetEntity()?.SetFrame(ref frame, isTeleportation: false);
				MountAgentVisuals?.GetEntity()?.SetFrame(ref frame, isTeleportation: false);
				CaravanMountAgentVisuals?.GetEntity()?.SetFrame(ref frame, isTeleportation: false);
			}
		}
		else
		{
			_entityAlpha = 1f - transitionProgress;
			HumanAgentVisuals?.GetEntity()?.SetAlpha(_entityAlpha);
			MountAgentVisuals?.GetEntity()?.SetAlpha(_entityAlpha);
			CaravanMountAgentVisuals?.GetEntity()?.SetAlpha(_entityAlpha);
		}
	}

	internal void ValidateIsDirty()
	{
		if (base.MapEntity.MemberRoster.TotalManCount != 0)
		{
			RefreshPartyIcon();
			if ((_entityAlpha < 1f && base.MapEntity.IsVisible) || (_entityAlpha > 0f && !base.MapEntity.IsVisible))
			{
				MobilePartyVisualManager.Current.RegisterFadingVisual(this);
			}
		}
		else
		{
			ResetPartyIcon();
		}
	}

	private void RefreshPartyIcon()
	{
		if (!base.MapEntity.IsVisualDirty)
		{
			return;
		}
		base.MapEntity.OnVisualsUpdated();
		bool clearBannerComponentCache = true;
		bool clearBannerEntityCache = true;
		ResetPartyIcon();
		MatrixFrame circleLocalFrame = CircleLocalFrame;
		circleLocalFrame.origin = Vec3.Zero;
		CircleLocalFrame = circleLocalFrame;
		if (base.MapEntity.MobileParty?.CurrentSettlement != null)
		{
			AddVisualToVisualsOfEntities();
			if (!base.MapEntity.MobileParty.MapFaction.IsAtWarWith(base.MapEntity.MobileParty.CurrentSettlement.MapFaction) && base.MapEntity.LeaderHero?.ClanBanner != null)
			{
				string bannerCode = base.MapEntity.LeaderHero.ClanBanner.BannerCode;
				if (!string.IsNullOrEmpty(bannerCode))
				{
					MatrixFrame identity = MatrixFrame.Identity;
					Vec3 bannerPositionForParty = SettlementVisualManager.Current.GetSettlementVisual(base.MapEntity.MobileParty.CurrentSettlement).GetBannerPositionForParty(base.MapEntity.MobileParty);
					if (bannerPositionForParty.IsValid)
					{
						identity.origin = bannerPositionForParty;
						identity.origin = StrategicEntity.GetGlobalFrame().TransformToLocal(in identity.origin);
						float num = MBMath.Map((float)base.MapEntity.NumberOfAllMembers / 400f * ((base.MapEntity.MobileParty.Army != null && base.MapEntity.MobileParty.Army.LeaderParty == base.MapEntity.MobileParty) ? 1.25f : 1f), 0f, 1f, 0.2f, 0.5f);
						identity = identity.Elevate(0f - num);
						identity.rotation.ApplyScaleLocal(num);
						identity.rotation = StrategicEntity.GetGlobalFrame().rotation.TransformToLocal(in identity.rotation);
						StrategicEntity.AddSphereAsBody(identity.origin + Vec3.Up * 0.3f, 0.15f, BodyFlags.None);
						clearBannerComponentCache = false;
						string text = "campaign_flag";
						if (_cachedBannerComponent.Item1 == bannerCode + text)
						{
							_cachedBannerComponent.Item2.GetFirstMetaMesh().Frame = identity;
							StrategicEntity.AddComponent(_cachedBannerComponent.Item2);
						}
						else
						{
							MetaMesh bannerOfCharacter = GetBannerOfCharacter(new Banner(bannerCode), text);
							bannerOfCharacter.Frame = identity;
							int componentCount = StrategicEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator);
							StrategicEntity.AddMultiMesh(bannerOfCharacter);
							if (StrategicEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > componentCount)
							{
								_cachedBannerComponent.Item1 = bannerCode + text;
								_cachedBannerComponent.Item2 = StrategicEntity.GetComponentAtIndex(componentCount, GameEntity.ComponentType.ClothSimulator);
							}
						}
					}
				}
			}
			else
			{
				StrategicEntity.RemovePhysics();
			}
		}
		else if (base.MapEntity.MobileParty != null && (base.MapEntity.MobileParty.IsCurrentlyAtSea || base.MapEntity.MobileParty.IsTransitionInProgress))
		{
			RemoveVisualFromVisualsOfEntities();
			if (base.MapEntity.MobileParty.IsTransitionInProgress)
			{
				if (base.MapEntity.MobileParty.Army == null || base.MapEntity.MobileParty.Army.LeaderParty == base.MapEntity.MobileParty || base.MapEntity.MobileParty.AttachedTo == null)
				{
					AddMobileIconComponents(base.MapEntity, ref clearBannerComponentCache, ref clearBannerEntityCache);
				}
				if (!_isInTransitionProgressCached)
				{
					AddVisualToVisualsOfEntities();
					OnTransitionStarted();
				}
			}
			if (base.MapEntity.MobileParty.IsTransitionInProgress != _isInTransitionProgressCached)
			{
				if (_isInTransitionProgressCached)
				{
					OnTransitionEnded();
				}
				else
				{
					OnTransitionStarted();
				}
			}
		}
		else
		{
			AddVisualToVisualsOfEntities();
			InitializePartyCollider(base.MapEntity);
			AddMobileIconComponents(base.MapEntity, ref clearBannerComponentCache, ref clearBannerEntityCache);
		}
		if (clearBannerComponentCache)
		{
			_cachedBannerComponent = (null, null);
		}
		if (clearBannerEntityCache)
		{
			_cachedBannerEntity = (null, null);
		}
		StrategicEntity.CheckResources(addToQueue: true, checkFaceResources: false);
		if (IsMobileEntity)
		{
			_isInTransitionProgressCached = base.MapEntity.MobileParty.IsTransitionInProgress;
		}
	}

	private void AddMobileIconComponents(PartyBase party, ref bool clearBannerComponentCache, ref bool clearBannerEntityCache)
	{
		uint contourColor = (FactionManager.IsAtWarAgainstFaction(party.MapFaction, Hero.MainHero.MapFaction) ? 4294905856u : 4278206719u);
		if (IsPartOfBesiegerCamp(party))
		{
			AddTentEntityForParty(StrategicEntity, party, ref clearBannerComponentCache);
		}
		else
		{
			if (PartyBaseHelper.GetVisualPartyLeader(party) == null)
			{
				return;
			}
			string bannerKey = null;
			if (party.LeaderHero?.ClanBanner != null)
			{
				bannerKey = party.LeaderHero.ClanBanner.BannerCode;
			}
			ActionIndexCache leaderAction = ActionIndexCache.act_none;
			ActionIndexCache mountAction = ActionIndexCache.act_none;
			MapEvent mapEvent = ((party.MobileParty.Army != null && party.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(party.MobileParty)) ? party.MobileParty.Army.LeaderParty.MapEvent : party.MapEvent);
			GetMeleeWeaponToWield(party, out var wieldedItemIndex);
			if (mapEvent != null && (mapEvent.EventType == MapEvent.BattleTypes.FieldBattle || mapEvent.EventType == MapEvent.BattleTypes.Raid || mapEvent.EventType == MapEvent.BattleTypes.SiegeOutside || mapEvent.EventType == MapEvent.BattleTypes.SallyOut))
			{
				GetPartyBattleAnimation(party, wieldedItemIndex, out leaderAction, out mountAction);
			}
			uint teamColor = (uint)(((int?)party.MapFaction?.Color) ?? (-3357781));
			uint teamColor2 = (uint)(((int?)party.MapFaction?.Color2) ?? (-3357781));
			AddCharacterToPartyIcon(party, PartyBaseHelper.GetVisualPartyLeader(party), contourColor, bannerKey, wieldedItemIndex, teamColor, teamColor2, in leaderAction, in mountAction, MBRandom.NondeterministicRandomFloat * 0.7f, ref clearBannerEntityCache);
			if (party.IsMobile)
			{
				GetMountAndHarnessVisualIdsForPartyIcon(out var mountStringId, out var harnessStringId);
				if (!string.IsNullOrEmpty(mountStringId))
				{
					AddMountToPartyIcon(new Vec3(0.3f, -0.25f), mountStringId, harnessStringId, contourColor, PartyBaseHelper.GetVisualPartyLeader(party));
				}
			}
		}
	}

	private void AddMountToPartyIcon(Vec3 positionOffset, string mountItemId, string harnessItemId, uint contourColor, CharacterObject character)
	{
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(mountItemId);
		Monster monster = itemObject.HorseComponent.Monster;
		ItemObject item = null;
		if (!string.IsNullOrEmpty(harnessItemId))
		{
			item = Game.Current.ObjectManager.GetObject<ItemObject>(harnessItemId);
		}
		Equipment equipment = new Equipment();
		equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(itemObject);
		equipment[EquipmentIndex.HorseHarness] = new EquipmentElement(item);
		AgentVisualsData data = new AgentVisualsData().Equipment(equipment).Scale(itemObject.ScaleFactor * 0.3f).Frame(new MatrixFrame(Mat3.Identity, in positionOffset))
			.ActionSet(MBGlobals.GetActionSet(monster.ActionSetCode + "_map"))
			.Scene(MapScene)
			.Monster(monster)
			.PrepareImmediately(prepareImmediately: false)
			.UseScaledWeapons(useScaledWeapons: true)
			.HasClippingPlane(hasClippingPlane: true)
			.MountCreationKey(MountCreationKey.GetRandomMountKeyString(itemObject, character.GetMountKeySeed()));
		CaravanMountAgentVisuals = AgentVisuals.Create(data, "PartyIcon " + mountItemId, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		CaravanMountAgentVisuals.GetEntity().SetContourColor(contourColor, alwaysVisible: false);
		MatrixFrame m = CaravanMountAgentVisuals.GetFrame();
		m.rotation.ApplyScaleLocal(CaravanMountAgentVisuals.GetScale());
		m = StrategicEntity.GetFrame().TransformToParent(in m);
		CaravanMountAgentVisuals.GetEntity().SetFrame(ref m);
		float speed = TaleWorlds.Library.MathF.Min(0.325f * _speed / 0.3f, 20f);
		CaravanMountAgentVisuals.Tick(null, 0.0001f, IsEntityMovingVisually(), speed);
		CaravanMountAgentVisuals.GetEntity().Skeleton.ForceUpdateBoneFrames();
	}

	private void AddCharacterToPartyIcon(PartyBase party, CharacterObject characterObject, uint contourColor, string bannerKey, int wieldedItemIndex, uint teamColor1, uint teamColor2, in ActionIndexCache leaderAction, in ActionIndexCache mountAction, float animationStartDuration, ref bool clearBannerEntityCache)
	{
		Equipment equipment = characterObject.Equipment.Clone();
		bool flag = !string.IsNullOrEmpty(bannerKey) && (((characterObject.IsPlayerCharacter || characterObject.HeroObject.Clan == Clan.PlayerClan) && Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.BannerEligibleTier) || (!characterObject.IsPlayerCharacter && (!characterObject.IsHero || (characterObject.IsHero && characterObject.HeroObject.Clan != Clan.PlayerClan))));
		int leftWieldedItemIndex = 4;
		if (flag)
		{
			ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>("campaign_banner_small");
			equipment[EquipmentIndex.ExtraWeaponSlot] = new EquipmentElement(item);
		}
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterObject.Race);
		MBActionSet actionSetWithSuffix = MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterObject.IsFemale, flag ? "_map_with_banner" : "_map");
		AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(useMorphAnims: true).Equipment(equipment).BodyProperties(characterObject.GetBodyProperties(characterObject.Equipment))
			.SkeletonType(characterObject.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.Scale(0.3f)
			.Frame(StrategicEntity.GetFrame())
			.ActionSet(actionSetWithSuffix)
			.Scene(MapScene)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(prepareImmediately: false)
			.RightWieldedItemIndex(wieldedItemIndex)
			.HasClippingPlane(hasClippingPlane: true)
			.UseScaledWeapons(useScaledWeapons: true)
			.ClothColor1(teamColor1)
			.ClothColor2(teamColor2)
			.CharacterObjectStringId(characterObject.StringId)
			.AddColorRandomness(!characterObject.IsHero)
			.Race(characterObject.Race);
		if (flag)
		{
			Banner banner = new Banner(bannerKey);
			agentVisualsData.Banner(banner).LeftWieldedItemIndex(leftWieldedItemIndex);
			if (_cachedBannerEntity.Item1 == bannerKey + "campaign_banner_small")
			{
				agentVisualsData.CachedWeaponEntity(EquipmentIndex.ExtraWeaponSlot, _cachedBannerEntity.Item2);
			}
		}
		if (!party.MobileParty.IsCurrentlyAtSea || party.MobileParty.IsTransitionInProgress)
		{
			HumanAgentVisuals = AgentVisuals.Create(agentVisualsData, "PartyIcon " + characterObject.Name, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		}
		if (HumanAgentVisuals != null)
		{
			if (flag)
			{
				GameEntity entity = HumanAgentVisuals.GetEntity();
				GameEntity child = entity.GetChild(entity.ChildCount - 1);
				if (child.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > 0)
				{
					clearBannerEntityCache = false;
					_cachedBannerEntity = (bannerKey + "campaign_banner_small", child);
				}
			}
			if (leaderAction != ActionIndexCache.act_none)
			{
				float actionAnimationDuration = MBActionSet.GetActionAnimationDuration(actionSetWithSuffix, in leaderAction);
				if (actionAnimationDuration < 1f)
				{
					HumanAgentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, in leaderAction, animationStartDuration);
				}
				else
				{
					HumanAgentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, in leaderAction, animationStartDuration / actionAnimationDuration);
				}
			}
		}
		if (characterObject.HasMount() && (!party.MobileParty.IsCurrentlyAtSea || party.MobileParty.IsTransitionInProgress))
		{
			Monster monster = characterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent.Monster;
			MBActionSet actionSet = MBGlobals.GetActionSet(monster.ActionSetCode + "_map");
			AgentVisualsData agentVisualsData2 = new AgentVisualsData().Equipment(characterObject.Equipment).Scale(characterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].Item.ScaleFactor * 0.3f).Frame(MatrixFrame.Identity)
				.ActionSet(actionSet)
				.Scene(MapScene)
				.Monster(monster)
				.PrepareImmediately(prepareImmediately: false)
				.UseScaledWeapons(useScaledWeapons: true)
				.HasClippingPlane(hasClippingPlane: true)
				.MountCreationKey(MountCreationKey.GetRandomMountKeyString(characterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, characterObject.GetMountKeySeed()));
			MountAgentVisuals = AgentVisuals.Create(agentVisualsData2, string.Concat("PartyIcon ", characterObject.Name, " mount"), isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
			if (mountAction != ActionIndexCache.act_none)
			{
				float actionAnimationDuration2 = MBActionSet.GetActionAnimationDuration(actionSet, in mountAction);
				if (actionAnimationDuration2 < 1f)
				{
					MountAgentVisuals.GetWeakEntity().Skeleton.SetAgentActionChannel(0, in mountAction, animationStartDuration);
				}
				else
				{
					MountAgentVisuals.GetWeakEntity().Skeleton.SetAgentActionChannel(0, in mountAction, animationStartDuration / actionAnimationDuration2);
				}
			}
			MountAgentVisuals.GetWeakEntity().SetContourColor(contourColor, alwaysVisible: false);
			MatrixFrame frame = StrategicEntity.GetFrame();
			frame.rotation.ApplyScaleLocal(agentVisualsData2.ScaleData);
			MountAgentVisuals.GetWeakEntity().SetFrame(ref frame);
		}
		float num = ((MountAgentVisuals != null) ? 1.3f : 1f);
		float speed = TaleWorlds.Library.MathF.Min(0.25f * num * _speed / 0.3f, 20f);
		if (MountAgentVisuals != null)
		{
			MountAgentVisuals.Tick(null, 0.0001f, IsEntityMovingVisually(), speed);
			MountAgentVisuals.GetWeakEntity().Skeleton.ForceUpdateBoneFrames();
		}
		if (HumanAgentVisuals != null)
		{
			WeakGameEntity weakEntity = HumanAgentVisuals.GetWeakEntity();
			weakEntity.SetContourColor(contourColor, alwaysVisible: false);
			MatrixFrame frame2 = StrategicEntity.GetFrame();
			frame2.rotation.ApplyScaleLocal(agentVisualsData.ScaleData);
			weakEntity.SetFrame(ref frame2);
			HumanAgentVisuals.Tick(MountAgentVisuals, 0.0001f, IsEntityMovingVisually(), speed);
			weakEntity.Skeleton.ForceUpdateBoneFrames();
		}
	}

	private bool IsEntityMovingVisually()
	{
		if (base.MapEntity.IsMobile && base.MapEntity.MapEvent != null)
		{
			_isEntityMovingCache = false;
		}
		else
		{
			if (!(Campaign.Current.CampaignDt > 0f))
			{
				MobileParty mobileParty = base.MapEntity.MobileParty;
				if (mobileParty == null || !mobileParty.IsMainParty || !Campaign.Current.IsMainPartyWaiting)
				{
					goto IL_00af;
				}
			}
			_isEntityMovingCache = false;
			MobileParty mobileParty2 = base.MapEntity.MobileParty;
			if (mobileParty2 != null && !mobileParty2.VisualPosition2DWithoutError.NearlyEquals(_lastFrameVisualPositionWithoutError))
			{
				_lastFrameVisualPositionWithoutError = base.MapEntity.MobileParty.VisualPosition2DWithoutError;
				_isEntityMovingCache = true;
			}
		}
		goto IL_00af;
		IL_00af:
		if (_isInTransitionProgressCached)
		{
			_isEntityMovingCache = true;
		}
		return _isEntityMovingCache;
	}

	public static MetaMesh GetBannerOfCharacter(Banner banner, string bannerMeshName)
	{
		MetaMesh copy = MetaMesh.GetCopy(bannerMeshName);
		for (int i = 0; i < copy.MeshCount; i++)
		{
			Mesh meshAtIndex = copy.GetMeshAtIndex(i);
			if (meshAtIndex.HasTag("dont_use_tableau"))
			{
				continue;
			}
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
					uint num = (uint)tableauMaterial.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
					ulong shaderFlags = tableauMaterial.GetShaderFlags();
					tableauMaterial.SetShaderFlags(shaderFlags | num);
				};
				banner.GetTableauTextureLarge(BannerDebugInfo.CreateManual("MobilePartyVisual"), setAction);
				MapScreen.Instance.CharacterBannerMaterialCache[key] = tableauMaterial;
			}
			meshAtIndex.SetMaterial(tableauMaterial);
		}
		return copy;
	}

	public void AddTentEntityForParty(GameEntity strategicEntity, PartyBase party, ref bool clearBannerComponentCache)
	{
		GameEntity gameEntity = GameEntity.CreateEmpty(strategicEntity.Scene);
		gameEntity.AddMultiMesh(MetaMesh.GetCopy("map_icon_siege_camp_tent"));
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation.ApplyScaleLocal(1.2f);
		gameEntity.SetFrame(ref frame);
		string text = null;
		if (party.LeaderHero?.ClanBanner != null)
		{
			text = party.LeaderHero.ClanBanner.BannerCode;
		}
		bool flag = party.MobileParty.Army != null && party.MobileParty.Army.LeaderParty == party.MobileParty;
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin.z += (flag ? 0.2f : 0.15f);
		float scaleAmount = MBMath.Map(party.CalculateCurrentStrength() / 500f * ((party.MobileParty.Army != null && flag) ? 1f : 0.8f), 0f, 1f, 0.15f, 0.5f);
		identity.rotation.ApplyScaleLocal(scaleAmount);
		if (!string.IsNullOrEmpty(text))
		{
			clearBannerComponentCache = false;
			string text2 = "campaign_flag";
			if (_cachedBannerComponent.Item1 == text + text2)
			{
				_cachedBannerComponent.Item2.GetFirstMetaMesh().Frame = identity;
				gameEntity.AddComponent(_cachedBannerComponent.Item2);
			}
			else
			{
				MetaMesh bannerOfCharacter = GetBannerOfCharacter(new Banner(text), text2);
				bannerOfCharacter.Frame = identity;
				int componentCount = gameEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator);
				gameEntity.AddMultiMesh(bannerOfCharacter);
				if (gameEntity.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > componentCount)
				{
					_cachedBannerComponent.Item1 = text + text2;
					_cachedBannerComponent.Item2 = gameEntity.GetComponentAtIndex(componentCount, GameEntity.ComponentType.ClothSimulator);
				}
			}
		}
		strategicEntity.AddChild(gameEntity);
		gameEntity.SetVisibilityExcludeParents(visible: true);
	}

	internal void ClearVisualMemory()
	{
		ResetPartyIcon();
		base.MapEntity.SetVisualAsDirty();
		_cachedBannerEntity = (null, null);
	}

	private void GetMeleeWeaponToWield(PartyBase party, out int wieldedItemIndex)
	{
		wieldedItemIndex = -1;
		CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(party);
		if (visualPartyLeader == null)
		{
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			if (visualPartyLeader.Equipment[i].Item != null && visualPartyLeader.Equipment[i].Item.PrimaryWeapon.IsMeleeWeapon)
			{
				wieldedItemIndex = i;
				break;
			}
		}
	}

	private static void GetPartyBattleAnimation(PartyBase party, int wieldedItemIndex, out ActionIndexCache leaderAction, out ActionIndexCache mountAction)
	{
		leaderAction = ActionIndexCache.act_none;
		mountAction = ActionIndexCache.act_none;
		if (party.MobileParty.Army == null || !party.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(party.MobileParty))
		{
			_ = party.MapEvent;
		}
		else
		{
			_ = party.MobileParty.Army.LeaderParty.MapEvent;
		}
		CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(party);
		if (party.MapEvent?.MapEventSettlement != null && visualPartyLeader != null && !visualPartyLeader.HasMount())
		{
			leaderAction = ActionIndexCache.act_map_raid;
			return;
		}
		if (wieldedItemIndex > -1 && visualPartyLeader?.Equipment[wieldedItemIndex].Item != null)
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
			}
			else
			{
				leaderAction = ActionIndexCache.act_map_attack_unarmed;
			}
		}
	}

	private void GetMountAndHarnessVisualIdsForPartyIcon(out string mountStringId, out string harnessStringId)
	{
		mountStringId = "";
		harnessStringId = "";
		if (base.MapEntity.IsMobile)
		{
			base.MapEntity.MobileParty.PartyComponent?.GetMountAndHarnessVisualIdsForPartyIcon(base.MapEntity, out mountStringId, out harnessStringId);
		}
	}

	private void InitializePartyCollider(PartyBase party)
	{
		if (StrategicEntity != null && party.IsMobile)
		{
			StrategicEntity.AddSphereAsBody(new Vec3(0f, 0f, 0f, -1f), 0.5f, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
		}
	}

	private void ResetPartyIcon()
	{
		if (HumanAgentVisuals != null)
		{
			HumanAgentVisuals.Reset();
			HumanAgentVisuals = null;
		}
		if (MountAgentVisuals != null)
		{
			MountAgentVisuals.Reset();
			MountAgentVisuals = null;
		}
		if (CaravanMountAgentVisuals != null)
		{
			CaravanMountAgentVisuals.Reset();
			CaravanMountAgentVisuals = null;
		}
		if (StrategicEntity != null)
		{
			if ((StrategicEntity.EntityFlags & EntityFlags.Ignore) != 0)
			{
				StrategicEntity.RemoveFromPredisplayEntity();
			}
			StrategicEntity.ClearComponents();
		}
		_bearingRotation = base.MapEntity.MobileParty.Bearing.RotationInRadians;
		MobilePartyVisualManager.Current.UnRegisterFadingVisual(this);
	}

	private float GetTransitionProgress()
	{
		if (IsMobileEntity && base.MapEntity.MobileParty.IsTransitionInProgress && base.MapEntity.MobileParty.NavigationTransitionDuration != CampaignTime.Zero)
		{
			float num = (float)base.MapEntity.MobileParty.NavigationTransitionDuration.ToHours;
			if (base.MapEntity.MobileParty.Army?.LeaderParty == base.MapEntity.MobileParty && base.MapEntity.MobileParty.AttachedParties.Count > 0)
			{
				float val = base.MapEntity.MobileParty.AttachedParties.MaxQ((MobileParty x) => (float)x.NavigationTransitionDuration.ToHours);
				num = Math.Max(num, val);
			}
			return MBMath.ClampFloat(base.MapEntity.MobileParty.NavigationTransitionStartTime.ElapsedHoursUntilNow / num, 0f, 1f);
		}
		return 1f;
	}

	private void OnTransitionStarted()
	{
		MobilePartyVisualManager.Current.RegisterFadingVisual(this);
		_transitionStartRotation = (base.MapEntity.MobileParty.EndPositionForNavigationTransition.ToVec2() - base.MapEntity.Position.ToVec2()).RotationInRadians;
	}

	private void OnTransitionEnded()
	{
	}

	private float GetVisualRotation()
	{
		if (base.MapEntity.IsMobile && base.MapEntity.MapEvent != null && base.MapEntity.MapEvent.IsFieldBattle)
		{
			return GetMapEventVisualRotation();
		}
		if (base.MapEntity.IsMobile && base.MapEntity.MobileParty.IsTransitionInProgress)
		{
			return _transitionStartRotation;
		}
		return _bearingRotation;
	}

	private float GetMapEventVisualRotation()
	{
		if (base.MapEntity.MapEventSide.OtherSide.LeaderParty != null && base.MapEntity.MapEventSide.OtherSide.LeaderParty.IsMobile && base.MapEntity.MapEventSide.OtherSide.LeaderParty.IsMobile)
		{
			return (base.MapEntity.MapEventSide.OtherSide.LeaderParty.MobileParty.VisualPosition2DWithoutError - base.MapEntity.MobileParty.VisualPosition2DWithoutError).Normalized().RotationInRadians;
		}
		return _bearingRotation;
	}

	private void AddVisualToVisualsOfEntities()
	{
		if (!MapScreen.VisualsOfEntities.ContainsKey(StrategicEntity.Pointer))
		{
			MapScreen.VisualsOfEntities.Add(StrategicEntity.Pointer, this);
		}
	}

	private void RemoveVisualFromVisualsOfEntities()
	{
		MapScreen.VisualsOfEntities.Remove(StrategicEntity.Pointer);
		foreach (GameEntity child in StrategicEntity.GetChildren())
		{
			MapScreen.VisualsOfEntities.Remove(child.Pointer);
		}
	}

	private bool IsPartOfBesiegerCamp(PartyBase party)
	{
		if (party.MobileParty.BesiegedSettlement?.SiegeEvent != null)
		{
			return party.MobileParty.BesiegedSettlement.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(party);
		}
		return false;
	}
}
