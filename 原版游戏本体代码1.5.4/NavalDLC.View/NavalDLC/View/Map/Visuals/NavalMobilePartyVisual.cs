using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Helpers;
using NavalDLC.Missions.Objects;
using NavalDLC.View.Map.Managers;
using SandBox;
using SandBox.View;
using SandBox.View.Map;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace NavalDLC.View.Map.Visuals;

public class NavalMobilePartyVisual : MapEntityVisual<PartyBase>
{
	private struct ShipOar
	{
		internal WeakGameEntity _oarEntity;

		internal float _sideSign;
	}

	public struct BlockadeShipVisual
	{
		public GameEntity ShipEntity;

		public float RockingPhase;
	}

	private class ShipFoamDecal
	{
		internal Decal _splashFoamDecal;

		internal MatrixFrame _currentFrame;

		internal float _cumulativeDtTillStart;

		internal Vec3 _randomScale;

		internal Vec3 _currentSpeed;

		internal Vec3 _sideVectorStart;

		internal Vec3 _sideVectorEnd;

		internal ShipFoamDecal()
		{
			_splashFoamDecal = null;
			_currentFrame = MatrixFrame.Identity;
			_sideVectorStart = Vec3.Zero;
			_sideVectorEnd = Vec3.Zero;
			_cumulativeDtTillStart = 0f;
			_randomScale = new Vec3(1f, 1f, 1f);
			_currentSpeed = Vec3.Zero;
		}
	}

	private const float DefaultWaterLevelZ = 2.58f;

	private const float SailWindVisualAmplifier = 5f;

	private const float BannerWindVisualAmplifier = 3f;

	private const string LeftOarTag = "oar_gate_left";

	private const string RightOarTag = "oar_gate_right";

	private const string BodyMeshTag = "body_mesh";

	private const int NumberOfSplashDecal = 20;

	private float _entityAlpha;

	private bool _isSailFolded;

	private float _sailAlpha;

	private Scene _mapScene;

	private AgentVisuals _raidAgentVisuals;

	private string _flagShipId;

	private bool _isVisualInRaftState;

	private MatrixFrame _firstOarRotationFrameCached = MatrixFrame.Identity;

	private MatrixFrame _secondOarRotationFrameCached = MatrixFrame.Identity;

	private readonly Dictionary<Ship, BlockadeShipVisual> _shipToBlockadeShipVisualCache = new Dictionary<Ship, BlockadeShipVisual>();

	private readonly List<ShipOar> _oars = new List<ShipOar>();

	private readonly List<SailVisual> _sailVisualCache = new List<SailVisual>();

	private SoundEvent _sailingSoundEvent;

	private float _oarPhase;

	private float _rockingPhase;

	private float _swayingAngle;

	private float _rollingAngle;

	private CampaignVec2 _targetPositionForSwaying;

	private float _lastFrameLerpedAngle;

	private GameEntity _shipEntity;

	private WeakGameEntity _bodyMeshEntity;

	private GameEntity _currentCollidedBridgeEntity;

	private float _bearingRotation;

	private GameEntity _shipMovementParticleEntity;

	private ParticleSystem _shipMovementParticle;

	private GameEntity _shipStillMovementParticleEntity;

	private ParticleSystem _shipStillMovementParticle;

	private BoundingBox _wakeBB;

	private Scene _ownerSceneCached;

	private ShipFoamDecal[] _splashFoamDecals = new ShipFoamDecal[20];

	private Vec3 _lastDecalSpawnPosition = Vec3.Zero;

	private float _nextDecalSpawnMetersSq = 0.09f;

	private int _nextDecalToUse;

	private ShipHull _ferryShipHull;

	private (string, GameEntity) _cachedBannerEntity;

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

	public override float BearingRotation => _bearingRotation;

	public override MapEntityVisual AttachedTo
	{
		get
		{
			if (base.MapEntity.MobileParty?.AttachedTo != null)
			{
				return NavalMobilePartyVisualManager.Current.GetVisualOfEntity(base.MapEntity.MobileParty.AttachedTo.Party);
			}
			return null;
		}
	}

	public override CampaignVec2 InteractionPositionForPlayer => ((IInteractablePoint)base.MapEntity).GetInteractionPosition(MobileParty.MainParty);

	public override bool IsMobileEntity => base.MapEntity.IsMobile;

	public override bool IsMainEntity => base.MapEntity == PartyBase.MainParty;

	public GameEntity StrategicEntity { get; private set; }

	public NavalMobilePartyVisual(PartyBase partyBase)
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
			_isVisualInRaftState = false;
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

	internal void Tick(float dt, float realDt, ref int dirtyPartiesCount, ref NavalMobilePartyVisual[] dirtyPartiesList)
	{
		if (StrategicEntity == null)
		{
			return;
		}
		if (base.MapEntity.MobileParty.IsNavalVisualDirty && (_entityAlpha > 0f || base.MapEntity.IsVisible))
		{
			int num = Interlocked.Increment(ref dirtyPartiesCount);
			dirtyPartiesList[num] = this;
		}
		if (!HasNavalVisual())
		{
			return;
		}
		if (!base.MapEntity.MobileParty.IsTransitionInProgress)
		{
			if (IsVisibleOrFadingOut() && StrategicEntity != null)
			{
				UpdateEntityPosition(dt, realDt, isVisible: true);
			}
		}
		else if (GetTransitionProgress() <= 1f)
		{
			TickTransitionFadeState(dt);
		}
		if (_raidAgentVisuals != null)
		{
			float speed = TaleWorlds.Library.MathF.Min(0.25f, 20f);
			_raidAgentVisuals.Tick(null, dt, isEntityMoving: false, speed);
		}
	}

	internal void UpdateEntityPosition(float dt, float realDt, bool isVisible = false)
	{
		MobileParty mobileParty = base.MapEntity.MobileParty;
		UpdateBearingRotation(realDt);
		MatrixFrame entityFrame = MatrixFrame.Identity;
		entityFrame.origin = GetVisualPosition();
		MatrixFrame localFrame = StrategicEntity.GetLocalFrame();
		Vec2 vec = entityFrame.origin.AsVec2 - localFrame.origin.AsVec2;
		float length = vec.Length;
		float num = ((dt > 0f) ? (length / dt) : 0f);
		if (mobileParty.Army != null && mobileParty.AttachedTo == mobileParty.Army.LeaderParty && (base.MapEntity.MapEvent == null || !base.MapEntity.MapEvent.IsFieldBattle))
		{
			if (num > 20f)
			{
				entityFrame.rotation.RotateAboutUp(_bearingRotation);
			}
			else if (mobileParty.CurrentSettlement == null)
			{
				float a = MBMath.LerpRadians(localFrame.rotation.f.AsVec2.RotationInRadians, (vec + Vec2.FromRotation(_bearingRotation) * 0.01f).RotationInRadians, Math.Min(6f * dt, 1f), 0.03f * dt, 10f * dt);
				entityFrame.rotation.RotateAboutUp(a);
			}
			else
			{
				float rotationInRadians = localFrame.rotation.f.AsVec2.RotationInRadians;
				entityFrame.rotation.RotateAboutUp(rotationInRadians);
			}
		}
		else if (mobileParty.CurrentSettlement == null)
		{
			entityFrame.rotation.RotateAboutUp(GetVisualRotation());
			Vec3 zero = Vec3.Zero;
			for (int i = -2; i <= 2; i++)
			{
				for (int j = -2; j <= 2; j++)
				{
					Vec2 position = entityFrame.origin.AsVec2 + new Vec2((float)i * 0.5f, (float)j * 0.5f);
					Campaign.Current.MapSceneWrapper.GetTerrainHeightAndNormal(position, out var height, out var normal);
					if (height < 2.58f)
					{
						normal = Vec3.Up;
					}
					zero += normal;
				}
			}
			zero /= TaleWorlds.Library.MathF.Pow(5f, 2f);
			float num2 = Vec3.DotProduct(entityFrame.rotation.u, zero);
			float num3 = Vec3.DotProduct(entityFrame.rotation.f, zero);
			Vec3 vec2 = entityFrame.rotation.u * num2;
			Vec3 vec3 = entityFrame.rotation.f * num3;
			Vec3 v = vec2 + vec3;
			float num4 = Vec3.AngleBetweenTwoVectors(entityFrame.rotation.u, v) * 0.5f;
			float num5 = ((num3 < 0f) ? 1f : (-1f));
			_lastFrameLerpedAngle = TaleWorlds.Library.MathF.Lerp(_lastFrameLerpedAngle, num5 * num4, 0.1f);
			entityFrame.rotation.RotateAboutSide(_lastFrameLerpedAngle);
		}
		if (base.MapEntity.MobileParty.IsMainParty && MobileParty.MainParty.IsCurrentlyAtSea)
		{
			CheckBridgeFadeState();
		}
		if (_shipEntity != null && !base.MapEntity.MobileParty.IsInRaftState && isVisible)
		{
			Vec2 windForPosition = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(base.MapEntity.Position);
			ApplyWindEffect(windForPosition, entityFrame.rotation.f.AsVec2, realDt, dt);
			TickSailingSound(num);
			TickOars(dt, realDt);
			ShipHull shipHull = (base.MapEntity.MobileParty.IsInFerryState ? GetFerryShipHull() : base.MapEntity.FlagShip.ShipHull);
			TickIdleShipAnimation(shipHull, ref _rockingPhase, ref entityFrame);
			TickSwayingAnimation(ref entityFrame);
			float speedUpMultiplier = Campaign.Current.SpeedUpMultiplier;
			float num6 = realDt;
			if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward && !Campaign.Current.IsMainPartyWaiting)
			{
				num6 *= speedUpMultiplier;
			}
			else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward)
			{
				num6 *= speedUpMultiplier;
			}
			TickFoamDecals(num6);
		}
		if (!_shipToBlockadeShipVisualCache.IsEmpty())
		{
			foreach (KeyValuePair<Ship, BlockadeShipVisual> item in _shipToBlockadeShipVisualCache.ToList())
			{
				BlockadeShipVisual value = item.Value;
				MatrixFrame entityFrame2 = value.ShipEntity.GetLocalFrame();
				TickIdleShipAnimation(item.Key.ShipHull, ref value.RockingPhase, ref entityFrame2, isBlockadeShip: true);
				value.ShipEntity.SetLocalFrame(ref entityFrame2, isTeleportation: true);
				_shipToBlockadeShipVisualCache[item.Key] = value;
			}
		}
		if (!StrategicEntity.GetFrame().NearlyEquals(entityFrame))
		{
			StrategicEntity.SetFrame(ref entityFrame);
		}
	}

	internal void OnStartup()
	{
		if (base.MapEntity.IsMobile)
		{
			StrategicEntity = GameEntity.CreateEmpty(NavalMobilePartyVisualManager.Current.MapScene);
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
		StrategicEntity.SetReadyToRender(ready: true);
		StrategicEntity.SetEntityEnvMapVisibility(value: false);
		_entityAlpha = (base.MapEntity.IsVisible ? 1f : 0f);
		_sailAlpha = 1f;
		AddVisualToVisualsOfEntities();
	}

	internal void TickFadingState(float realDt)
	{
		if ((_entityAlpha < 1f && base.MapEntity.IsVisible) || (_entityAlpha > 0f && !base.MapEntity.IsVisible))
		{
			if (base.MapEntity.IsVisible)
			{
				if (_entityAlpha <= 0f)
				{
					foreach (BlockadeShipVisual value in _shipToBlockadeShipVisualCache.Values)
					{
						value.ShipEntity.SetVisibilityExcludeParents(visible: true);
					}
					StrategicEntity.SetVisibilityExcludeParents(visible: true);
				}
				_entityAlpha = TaleWorlds.Library.MathF.Min(_entityAlpha + TaleWorlds.Library.MathF.Max(realDt, 1E-05f), 1f);
				StrategicEntity.SetAlpha(_entityAlpha);
				StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
				{
					foreach (BlockadeShipVisual value2 in _shipToBlockadeShipVisualCache.Values)
					{
						value2.ShipEntity.SetAlpha(_entityAlpha);
					}
					return;
				}
			}
			_entityAlpha = TaleWorlds.Library.MathF.Max(_entityAlpha - TaleWorlds.Library.MathF.Max(realDt, 1E-05f), 0f);
			StrategicEntity.SetAlpha(_entityAlpha);
			foreach (BlockadeShipVisual value3 in _shipToBlockadeShipVisualCache.Values)
			{
				value3.ShipEntity.SetAlpha(_entityAlpha);
			}
			if (!(_entityAlpha <= 0f))
			{
				return;
			}
			StrategicEntity.SetVisibilityExcludeParents(visible: false);
			foreach (BlockadeShipVisual value4 in _shipToBlockadeShipVisualCache.Values)
			{
				value4.ShipEntity.SetVisibilityExcludeParents(visible: false);
			}
			StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
			ShipFoamDecal[] splashFoamDecals = _splashFoamDecals;
			foreach (ShipFoamDecal shipFoamDecal in splashFoamDecals)
			{
				if (shipFoamDecal != null && shipFoamDecal._splashFoamDecal != null)
				{
					shipFoamDecal._splashFoamDecal.SetIsVisible(value: false);
				}
			}
		}
		else
		{
			NavalMobilePartyVisualManager.Current.UnRegisterFadingVisual(this);
		}
	}

	private void TickTransitionFadeState(float dt)
	{
		if (GetTransitionProgress() > 0f && base.MapEntity.MobileParty.IsCurrentlyAtSea && _shipEntity != null && base.MapEntity.IsVisible)
		{
			CampaignVec2 campaignVec = base.MapEntity.MobileParty.EndPositionForNavigationTransition - base.MapEntity.MobileParty.Position.ToVec2();
			MatrixFrame globalFrame = StrategicEntity.GetGlobalFrame();
			float smallestDifferenceBetweenTwoAngles = MBMath.GetSmallestDifferenceBetweenTwoAngles(campaignVec.LeftVec().RotationInRadians, globalFrame.rotation.f.AsVec2.RotationInRadians);
			float smallestDifferenceBetweenTwoAngles2 = MBMath.GetSmallestDifferenceBetweenTwoAngles(campaignVec.RightVec().RotationInRadians, globalFrame.rotation.f.AsVec2.RotationInRadians);
			float valueTo = ((TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles2) > TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles)) ? smallestDifferenceBetweenTwoAngles : smallestDifferenceBetweenTwoAngles2);
			float f = TaleWorlds.Library.MathF.Lerp(0f, valueTo, dt * 5f);
			MatrixFrame frame = StrategicEntity.GetLocalFrame();
			frame.Rotate(TaleWorlds.Library.MathF.Abs(f), in Vec3.Up);
			StrategicEntity.SetLocalFrame(ref frame, isTeleportation: false);
			MatrixFrame frame2 = StrategicEntity.GetGlobalFrame();
			CampaignVec2 campaignVec2 = base.MapEntity.MobileParty.Position + base.MapEntity.MobileParty.ArmyPositionAdder * 0.7f;
			float x = TaleWorlds.Library.MathF.Lerp(frame2.origin.X, campaignVec2.X, dt * 5f);
			float y = TaleWorlds.Library.MathF.Lerp(frame2.origin.Y, campaignVec2.Y, dt * 5f);
			frame2.origin = new Vec3(x, y, frame2.origin.z);
			StrategicEntity.SetGlobalFrame(in frame2);
		}
	}

	internal void ClearVisualMemory()
	{
		ResetPartyIcon();
		base.MapEntity.SetVisualAsDirty();
	}

	internal void ValidateIsDirty()
	{
		if (base.MapEntity.MemberRoster.TotalManCount != 0)
		{
			RefreshPartyIcon();
			if ((_entityAlpha < 1f && base.MapEntity.IsVisible) || (_entityAlpha > 0f && !base.MapEntity.IsVisible))
			{
				if (base.MapEntity.MobileParty.IsTransitionInProgress && !StrategicEntity.GlobalPosition.IsNonZero)
				{
					UpdateEntityPosition(0.1f, 0.1f);
				}
				NavalMobilePartyVisualManager.Current.RegisterFadingVisual(this);
			}
		}
		else
		{
			ResetPartyIcon();
		}
	}

	private void RefreshPartyIcon()
	{
		if (!base.MapEntity.MobileParty.IsNavalVisualDirty)
		{
			return;
		}
		base.MapEntity.MobileParty.OnNavalVisualsUpdated();
		bool clearBannerEntityCache = true;
		if (_raidAgentVisuals != null)
		{
			_raidAgentVisuals.SetVisible(value: false);
			_raidAgentVisuals.Reset();
			_raidAgentVisuals = null;
		}
		MatrixFrame circleLocalFrame = CircleLocalFrame;
		circleLocalFrame.origin = Vec3.Zero;
		CircleLocalFrame = circleLocalFrame;
		if (!HasNavalVisual())
		{
			if (base.MapEntity.MobileParty.Ships.Count == 0 || base.MapEntity.MobileParty.IsInRaftState)
			{
				ResetPartyIcon();
			}
			else
			{
				RemoveBlockadeVisuals();
				HideNavalVisual();
			}
			RemoveVisualFromVisualsOfEntities();
		}
		else
		{
			AddVisualToVisualsOfEntities();
			if (base.MapEntity.MobileParty.BesiegedSettlement?.SiegeEvent != null && base.MapEntity.MobileParty.BesiegedSettlement.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(base.MapEntity))
			{
				HideNavalVisual();
				if (base.MapEntity.MobileParty.BesiegedSettlement.SiegeEvent.IsBlockadeActive)
				{
					NavalDLCViewHelpers.BlockadeVisualHelper.AddBlockadeVisuals(_shipToBlockadeShipVisualCache, base.MapEntity, StrategicEntity);
				}
				else
				{
					RemoveBlockadeVisuals();
				}
			}
			else if (base.MapEntity.MobileParty != null && (base.MapEntity.MobileParty.IsCurrentlyAtSea || base.MapEntity.MobileParty.IsTransitionInProgress) && (base.MapEntity.MobileParty.CurrentSettlement == null || base.MapEntity.MobileParty.IsTargetingPort))
			{
				if (base.MapEntity.MobileParty.IsInRaftState)
				{
					ResetPartyIcon();
					AddRaftVisual();
				}
				else if (base.MapEntity.MobileParty.IsInFerryState)
				{
					ResetPartyIcon();
					Ship flagship = new Ship(GetFerryShipHull());
					AddShipVisual(flagship);
				}
				else if (base.MapEntity.Ships.Count > 0)
				{
					Ship flagShip = base.MapEntity.FlagShip;
					AddShipVisual(flagShip);
				}
				InitializePartyCollider(base.MapEntity);
				if (base.MapEntity.MobileParty.IsVisible && base.MapEntity.MobileParty.MapEvent != null)
				{
					Settlement mapEventSettlement = base.MapEntity.MobileParty.MapEvent.MapEventSettlement;
					if (mapEventSettlement != null && mapEventSettlement.IsVillage && _raidAgentVisuals == null)
					{
						AddRaidPartyVisual(base.MapEntity.MobileParty.Party, ref clearBannerEntityCache);
					}
				}
			}
		}
		if (clearBannerEntityCache)
		{
			_cachedBannerEntity = (null, null);
		}
		StrategicEntity.CheckResources(addToQueue: true, checkFaceResources: false);
	}

	private void AddRaidPartyVisual(PartyBase party, ref bool clearBannerEntityCache)
	{
		uint contourColor = (FactionManager.IsAtWarAgainstFaction(party.MapFaction, Hero.MainHero.MapFaction) ? 4294905856u : 4278206719u);
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = base.MapEntity.MobileParty.MapEvent.MapEventSettlement.Position.AsVec3();
		if (base.MapEntity.MapEventSide.OtherSide.LeaderParty != null)
		{
			Vec2 vec = ((base.MapEntity.MapEventSide.OtherSide.LeaderParty.IsMobile ? base.MapEntity.MapEventSide.OtherSide.LeaderParty.MobileParty.VisualPosition2DWithoutError : base.MapEntity.MapEventSide.OtherSide.LeaderParty.Position.ToVec2()) - base.MapEntity.MobileParty.VisualPosition2DWithoutError).Normalized();
			if (base.MapEntity.MapEventSide.OtherSide.LeaderParty.IsMobile)
			{
				identity.origin -= vec.ToVec3() * 0.5f;
			}
			identity.rotation.RotateAboutUp(vec.RotationInRadians);
		}
		_raidAgentVisuals = SandBoxViewHelpers.MobilePartyVisualHelper.GetHumanAgentPartyVisual(MapScene, identity, party, contourColor, ActionIndexCache.act_map_raid, ref clearBannerEntityCache, ref _cachedBannerEntity, out var animationDuration);
		if (_raidAgentVisuals != null)
		{
			float num = MBRandom.NondeterministicRandomFloat * 0.7f;
			_raidAgentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, in ActionIndexCache.act_map_raid, (animationDuration < 1f) ? num : (num / animationDuration));
			WeakGameEntity weakEntity = _raidAgentVisuals.GetWeakEntity();
			float speed = TaleWorlds.Library.MathF.Min(0.25f, 20f);
			_raidAgentVisuals.Tick(null, 0.0001f, isEntityMoving: false, speed);
			weakEntity.Skeleton.ForceUpdateBoneFrames();
		}
	}

	private void InitializePartyCollider(PartyBase party)
	{
		if (StrategicEntity != null && party.IsMobile)
		{
			if (_shipEntity != null && _bodyMeshEntity.IsValid)
			{
				UpdateEntityPosition(0.1f, 0.1f);
				Vec3 eulerAngles = StrategicEntity.GetGlobalFrame().rotation.GetEulerAngles();
				Vec3 eulerAngles2 = _bodyMeshEntity.GetGlobalFrame().rotation.GetEulerAngles();
				BoundingBox localPhysicsBoundingBox = _bodyMeshEntity.GetLocalPhysicsBoundingBox(includeChildren: false);
				localPhysicsBoundingBox.max.RotateAboutZ(eulerAngles.RotationZ - eulerAngles2.RotationZ);
				localPhysicsBoundingBox.min.RotateAboutZ(eulerAngles.RotationZ - eulerAngles2.RotationZ);
				float num = TaleWorlds.Library.MathF.Abs(localPhysicsBoundingBox.max.x - localPhysicsBoundingBox.min.x) / 40f;
				float num2 = num / 2f;
				float num3 = TaleWorlds.Library.MathF.Max(localPhysicsBoundingBox.max.y, localPhysicsBoundingBox.min.y);
				float num4 = TaleWorlds.Library.MathF.Min(localPhysicsBoundingBox.max.y, localPhysicsBoundingBox.min.y);
				GameEntityPhysicsExtensions.AddCapsuleAsBody(p1: new Vec3(0f, num3 / 20f - num2, num2 + 0.01f), p2: new Vec3(0f, num4 / 20f + num2, num2 + 0.01f), gameEntity: StrategicEntity, radius: num, bodyFlags: BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			}
			else
			{
				StrategicEntity.AddCapsuleAsBody(new Vec3(0f, 0.5f), new Vec3(0f, -0.5f), 0.5f, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			}
		}
	}

	private void ResetPartyIcon()
	{
		if (StrategicEntity != null)
		{
			if ((StrategicEntity.EntityFlags & EntityFlags.Ignore) != 0)
			{
				StrategicEntity.RemoveFromPredisplayEntity();
			}
			StrategicEntity.ClearComponents();
		}
		if (_shipEntity != null)
		{
			_shipEntity.ClearComponents();
			_sailVisualCache.Clear();
			_oars.Clear();
			_shipEntity = null;
			_sailingSoundEvent?.Stop();
			_sailingSoundEvent = null;
			_oarPhase = 0f;
		}
		if (_raidAgentVisuals != null)
		{
			_raidAgentVisuals.Reset();
			_raidAgentVisuals = null;
		}
		RemoveBlockadeVisuals();
		if (_currentCollidedBridgeEntity != null)
		{
			_currentCollidedBridgeEntity.SetAlpha(1f);
			_currentCollidedBridgeEntity = null;
		}
		_bearingRotation = base.MapEntity.MobileParty.Bearing.RotationInRadians;
		_isVisualInRaftState = false;
		NavalMobilePartyVisualManager.Current.UnRegisterFadingVisual(this);
		ShipFoamDecal[] splashFoamDecals = _splashFoamDecals;
		foreach (ShipFoamDecal shipFoamDecal in splashFoamDecals)
		{
			if (shipFoamDecal != null && shipFoamDecal._splashFoamDecal != null)
			{
				_ownerSceneCached.RemoveDecalInstance(shipFoamDecal._splashFoamDecal, "editor_set");
				shipFoamDecal._splashFoamDecal = null;
			}
		}
	}

	private void HideNavalVisual()
	{
		StrategicEntity.SetVisibilityExcludeParents(visible: false);
		_bearingRotation = base.MapEntity.MobileParty.Bearing.RotationInRadians;
		if (_currentCollidedBridgeEntity != null)
		{
			_currentCollidedBridgeEntity.SetAlpha(1f);
			_currentCollidedBridgeEntity = null;
		}
		ShipFoamDecal[] splashFoamDecals = _splashFoamDecals;
		foreach (ShipFoamDecal shipFoamDecal in splashFoamDecals)
		{
			if (shipFoamDecal != null && shipFoamDecal._splashFoamDecal != null)
			{
				shipFoamDecal._splashFoamDecal.SetIsVisible(value: false);
			}
		}
		NavalMobilePartyVisualManager.Current.UnRegisterFadingVisual(this);
	}

	private float GetTransitionProgress()
	{
		if (IsMobileEntity && base.MapEntity.MobileParty.IsTransitionInProgress && base.MapEntity.MobileParty.NavigationTransitionDuration != CampaignTime.Zero)
		{
			return MBMath.ClampFloat(base.MapEntity.MobileParty.NavigationTransitionStartTime.ElapsedHoursUntilNow / (float)base.MapEntity.MobileParty.NavigationTransitionDuration.ToHours, 0f, 1f);
		}
		return 1f;
	}

	private float GetVisualRotation()
	{
		if (base.MapEntity.IsMobile && base.MapEntity.MapEvent != null && base.MapEntity.MapEvent.IsFieldBattle)
		{
			return GetMapEventVisualRotation();
		}
		return _bearingRotation;
	}

	private float GetMapEventVisualRotation()
	{
		if (base.MapEntity.MapEventSide.OtherSide.LeaderParty != null && base.MapEntity.MapEventSide.OtherSide.LeaderParty.IsMobile)
		{
			Vec2 vec = (base.MapEntity.MapEventSide.OtherSide.LeaderParty.MobileParty.VisualPosition2DWithoutError - base.MapEntity.MobileParty.VisualPosition2DWithoutError).Normalized();
			if (base.MapEntity.MapEvent.IsNavalMapEvent)
			{
				vec.RotateCCW(System.MathF.PI / 2f);
			}
			return vec.RotationInRadians;
		}
		return _bearingRotation;
	}

	private void CollectOars()
	{
		_oars.Clear();
		foreach (WeakGameEntity item3 in _shipEntity.WeakEntity.CollectChildrenEntitiesWithTagAsEnumarable("oar_gate_left"))
		{
			WeakGameEntity firstChildEntityWithTag = item3.GetFirstChildEntityWithTag("upgrade_slot");
			if (firstChildEntityWithTag != null)
			{
				ShipOar item = new ShipOar
				{
					_oarEntity = firstChildEntityWithTag,
					_sideSign = 1f
				};
				_oars.Add(item);
			}
		}
		foreach (WeakGameEntity item4 in _shipEntity.WeakEntity.CollectChildrenEntitiesWithTagAsEnumarable("oar_gate_right"))
		{
			WeakGameEntity firstChildEntityWithTag2 = item4.GetFirstChildEntityWithTag("upgrade_slot");
			if (firstChildEntityWithTag2 != null)
			{
				ShipOar item2 = new ShipOar
				{
					_oarEntity = firstChildEntityWithTag2,
					_sideSign = -1f
				};
				_oars.Add(item2);
			}
		}
		_firstOarRotationFrameCached = MatrixFrame.Identity;
		_secondOarRotationFrameCached = MatrixFrame.Identity;
		_firstOarRotationFrameCached.rotation.RotateAboutSide(-0.17453292f);
		_secondOarRotationFrameCached.rotation.RotateAboutSide(-0.14835298f);
	}

	private void UpdateBearingRotation(float realDt)
	{
		float num = MBMath.WrapAngle(base.MapEntity.MobileParty.Bearing.RotationInRadians - _bearingRotation);
		float num2 = realDt / 2f;
		float num3 = (((base.MapEntity.MobileParty.NextTargetPosition.ToVec2() - base.MapEntity.MobileParty.VisualPosition2DWithoutError).Length < 2f) ? 7.5f : 3f);
		_bearingRotation += num * TaleWorlds.Library.MathF.Min(num2 * num3, 1f);
		_bearingRotation = MBMath.WrapAngle(_bearingRotation);
	}

	private float GetOarVerticalAngle(float phase, float verticalBaseAngle, float verticalRotationAngle)
	{
		return verticalBaseAngle + TaleWorlds.Library.MathF.Cos(0f - phase) * verticalRotationAngle;
	}

	private void TickSailingSound(float speed)
	{
		_sailingSoundEvent.SetPosition(GetVisualPosition());
		if (!_sailingSoundEvent.IsPlaying())
		{
			_sailingSoundEvent.Play();
		}
		_sailingSoundEvent.SetParameter("ShipSpeed", speed);
	}

	private MatrixFrame ComputeOarFrame(ShipOar oar)
	{
		MatrixFrame identity = MatrixFrame.Identity;
		identity.rotation.RotateAboutForward(oar._sideSign * _oarPhase);
		return _secondOarRotationFrameCached.TransformToParent(identity.TransformToParent(in _firstOarRotationFrameCached));
	}

	private void TickOars(float dt, float realDt)
	{
		if (IsMoving())
		{
			float num = ((dt > 0f) ? dt : (realDt * 0.25f));
			float num2 = (base.MapEntity.MobileParty.IsActive ? base.MapEntity.MobileParty.LastCalculatedBaseSpeed : 0f);
			_oarPhase += num * num2 * 1.87f;
		}
		foreach (ShipOar oar in _oars)
		{
			MatrixFrame frame = ComputeOarFrame(oar);
			WeakGameEntity oarEntity = oar._oarEntity;
			oarEntity.SetFrame(ref frame, isTeleportation: false);
		}
	}

	private void AddShipVisual(Ship flagship)
	{
		if (!base.MapEntity.IsActive)
		{
			return;
		}
		_isSailFolded = true;
		if (_flagShipId == flagship.ShipHull.StringId && _shipEntity != null && _isVisualInRaftState == base.MapEntity.MobileParty.IsInRaftState)
		{
			NavalDLCViewHelpers.ShipVisualHelper.RefreshShipVisuals(_shipEntity.WeakEntity, flagship, _sailVisualCache);
		}
		else
		{
			if (StrategicEntity != null)
			{
				if ((StrategicEntity.EntityFlags & EntityFlags.Ignore) != 0)
				{
					StrategicEntity.RemoveFromPredisplayEntity();
				}
				StrategicEntity.ClearComponents();
			}
			if (_shipEntity != null)
			{
				_shipEntity.ClearComponents();
				_sailVisualCache.Clear();
				_shipEntity = null;
			}
			else
			{
				_sailingSoundEvent = SoundEvent.CreateEventFromString("event:/map/army/sail", NavalMobilePartyVisualManager.Current.MapScene);
				_sailingSoundEvent.SetPosition(GetVisualPosition());
			}
			_shipEntity = NavalDLCViewHelpers.ShipVisualHelper.GetShipEntityForCampaign(flagship, StrategicEntity.Scene, flagship.GetShipVisualSlotInfos());
			NavalDLCViewHelpers.ShipVisualHelper.CollectSailVisuals(_shipEntity.WeakEntity, _sailVisualCache);
			CollectOars();
			float num = 50f;
			foreach (SailVisual item in _sailVisualCache)
			{
				if (item.Type == SailVisual.SailType.LateenSail)
				{
					MatrixFrame frame = item.SailYawRotationEntity.GetLocalFrame();
					frame.rotation = Mat3.Identity;
					frame.rotation.RotateAboutUp(num * (System.MathF.PI / 180f));
					item.SailYawRotationEntity.SetFrame(ref frame, isTeleportation: false);
				}
			}
			_bodyMeshEntity = _shipEntity.WeakEntity.GetFirstChildEntityWithTagRecursive("body_mesh");
			StrategicEntity.AddChild(_shipEntity);
			_shipEntity.SetVisibilityExcludeParents(visible: true);
			_flagShipId = flagship.ShipHull.StringId;
			_ownerSceneCached = _shipEntity.Scene;
			_shipMovementParticleEntity = GameEntity.CreateEmpty(_ownerSceneCached, isModifiableFromEditor: false, createPhysics: false, callScriptCallbacks: false);
			_shipMovementParticleEntity.Name = "movement_particle";
			_shipEntity.AddChild(_shipMovementParticleEntity);
			MatrixFrame frame2 = MatrixFrame.Identity;
			if (_bodyMeshEntity.IsValid)
			{
				MetaMesh metaMesh = _bodyMeshEntity.GetMetaMesh(0);
				if (metaMesh != null)
				{
					_wakeBB = metaMesh.GetBoundingBox();
					frame2.origin.y += _wakeBB.max.y * 0.8f;
					frame2.rotation.ApplyScaleLocal(20f);
					_shipMovementParticleEntity.SetFrame(ref frame2);
				}
			}
			_shipMovementParticleEntity.SetLocalFrame(ref frame2, isTeleportation: true);
			_lastDecalSpawnPosition = _shipEntity.GetGlobalFrame().origin;
			for (int i = 0; i < 20; i++)
			{
				_splashFoamDecals[i] = new ShipFoamDecal();
			}
			MatrixFrame boneLocalFrame = MatrixFrame.Identity;
			_shipMovementParticle = ParticleSystem.CreateParticleSystemAttachedToEntity("psys_campaign_ship_trail", _shipMovementParticleEntity, ref boneLocalFrame);
			_shipStillMovementParticleEntity = GameEntity.CreateEmpty(_ownerSceneCached, isModifiableFromEditor: false, createPhysics: false, callScriptCallbacks: false);
			_shipStillMovementParticleEntity.Name = "movement_particle_still";
			_shipEntity.AddChild(_shipStillMovementParticleEntity);
			_shipStillMovementParticleEntity.SetFrame(ref frame2);
			_shipStillMovementParticle = ParticleSystem.CreateParticleSystemAttachedToEntity("psys_campaign_ship_trail_still", _shipStillMovementParticleEntity, ref boneLocalFrame);
			_shipStillMovementParticleEntity.SetVisibilityExcludeParents(visible: false);
		}
		_shipEntity.SetAlpha(GetTransitionProgress());
		StrategicEntity.SetAlpha(GetTransitionProgress());
		StrategicEntity.SetVisibilityExcludeParents(visible: true);
		_isVisualInRaftState = false;
	}

	private bool IsMoving()
	{
		bool result = false;
		if (base.MapEntity.MobileParty != null && base.MapEntity.MobileParty.IsMainParty)
		{
			result = !Campaign.Current.IsMainPartyWaiting;
		}
		else
		{
			MobileParty mobileParty = base.MapEntity.MobileParty;
			if (mobileParty != null && !mobileParty.Position.NearlyEquals(base.MapEntity.MobileParty.NextTargetPosition.ToVec2()))
			{
				result = true;
			}
		}
		return result;
	}

	private void TickIdleShipAnimation(ShipHull shipHull, ref float rockingPhase, ref MatrixFrame entityFrame, bool isBlockadeShip = false)
	{
		if (MBMath.WrapAngle(base.MapEntity.MobileParty.Bearing.RotationInRadians - _bearingRotation).ApproximatelyEqualsTo(0f, 0.003f))
		{
			float num = 1f;
			float num2 = System.MathF.PI / 40f;
			if (shipHull.Type == ShipHull.ShipType.Light)
			{
				num = 2f;
			}
			else if (shipHull.Type == ShipHull.ShipType.Medium)
			{
				num = 1.5f;
			}
			rockingPhase += num * 0.01f;
			if (_swayingAngle != 0f)
			{
				_swayingAngle = 0f;
				rockingPhase = System.MathF.PI / 2f;
			}
			if (TaleWorlds.Library.MathF.Abs(_rollingAngle) > num2)
			{
				num2 = TaleWorlds.Library.MathF.Abs(_rollingAngle);
			}
			rockingPhase = MBMath.WrapAngle(rockingPhase);
			float num3 = MBMath.Map(TaleWorlds.Library.MathF.Cos(rockingPhase), -1f, 1f, 0f - num2, num2);
			if (isBlockadeShip)
			{
				Vec3 eulerAngles = entityFrame.rotation.GetEulerAngles();
				eulerAngles.y = num3 - eulerAngles.y;
				entityFrame.rotation.RotateAboutForward(eulerAngles.Y);
			}
			else
			{
				_rollingAngle = MBMath.LerpRadians(_rollingAngle, num3, 0.01f, 0f, num2);
				entityFrame.rotation.RotateAboutForward(_rollingAngle);
			}
		}
	}

	private void TickFoamDecals(float dt)
	{
		MatrixFrame globalFrame = _shipEntity.GetGlobalFrame();
		Vec3 scaleAmountXYZ = new Vec3(0.013f, 0.025f, 1f) * 1.176f * 2f;
		Vec3 v = scaleAmountXYZ * 17.5f;
		ShipFoamDecal[] splashFoamDecals = _splashFoamDecals;
		foreach (ShipFoamDecal shipFoamDecal in splashFoamDecals)
		{
			if (shipFoamDecal._splashFoamDecal != null && shipFoamDecal._cumulativeDtTillStart < 3.15f)
			{
				shipFoamDecal._cumulativeDtTillStart += dt;
				float num = 1f;
				float y = 4f;
				if (shipFoamDecal._cumulativeDtTillStart > 0.45f)
				{
					float num2 = shipFoamDecal._cumulativeDtTillStart - 0.45f;
					num = TaleWorlds.Library.MathF.Clamp(1f - num2 / 2.7f, 0f, 1f);
				}
				else
				{
					num = TaleWorlds.Library.MathF.Clamp(shipFoamDecal._cumulativeDtTillStart / 0.45f, 0f, 1f);
				}
				float num3 = 0.475f;
				float alpha = TaleWorlds.Library.MathF.Pow(num, y) * _entityAlpha * (0.95f - num3) + num3;
				shipFoamDecal._splashFoamDecal.SetAlpha(alpha);
				shipFoamDecal._currentFrame.origin += shipFoamDecal._currentSpeed * dt;
				Vec3 currentSpeed = shipFoamDecal._currentSpeed;
				float num4 = currentSpeed.Normalize();
				num4 = TaleWorlds.Library.MathF.Max(num4 - dt * 2.5f, 0f);
				shipFoamDecal._currentSpeed = num4 * currentSpeed;
				float x = TaleWorlds.Library.MathF.Clamp(shipFoamDecal._cumulativeDtTillStart / 3.15f, 0f, 1f);
				x = TaleWorlds.Library.MathF.Pow(x, 0.4f);
				Vec3 scaleAmountXYZ2 = Vec3.Lerp(scaleAmountXYZ, v, x);
				scaleAmountXYZ2.x *= shipFoamDecal._randomScale.x;
				scaleAmountXYZ2.y *= shipFoamDecal._randomScale.y;
				scaleAmountXYZ2.z *= shipFoamDecal._randomScale.z;
				float num5 = 3.15f;
				float percent = TaleWorlds.Library.MathF.Clamp(shipFoamDecal._cumulativeDtTillStart / num5, 0f, 1f);
				Vec3 s = Vec3.Slerp(shipFoamDecal._sideVectorStart, shipFoamDecal._sideVectorEnd, percent);
				s.Normalize();
				shipFoamDecal._currentFrame.rotation.s = s;
				shipFoamDecal._currentFrame.rotation.u = Vec3.Up;
				shipFoamDecal._currentFrame.rotation.f = -shipFoamDecal._currentFrame.rotation.s.CrossProductWithUp();
				shipFoamDecal._currentFrame.rotation.ApplyScaleLocal(in scaleAmountXYZ2);
				shipFoamDecal._splashFoamDecal.Frame = shipFoamDecal._currentFrame;
			}
			else if (shipFoamDecal._splashFoamDecal != null)
			{
				shipFoamDecal._splashFoamDecal.SetIsVisible(value: false);
			}
		}
		Vec3 origin = globalFrame.origin;
		float num6 = _lastDecalSpawnPosition.DistanceSquared(origin);
		if (_nextDecalSpawnMetersSq < num6)
		{
			Vec3 vec = globalFrame.rotation.f.NormalizedCopy() * 0.5f;
			Vec3 s2 = globalFrame.rotation.s;
			s2.z = 0f;
			s2.Normalize();
			ShipFoamDecal shipFoamDecal2 = _splashFoamDecals[_nextDecalToUse];
			if (shipFoamDecal2._splashFoamDecal == null)
			{
				Decal decal = Decal.CreateDecal();
				decal.SetMaterial(Material.GetFromResource("decal_water_foam"));
				_ownerSceneCached.AddDecalInstance(decal, "editor_set", deletable: true);
				shipFoamDecal2._splashFoamDecal = decal;
			}
			shipFoamDecal2._splashFoamDecal.SetIsVisible(value: true);
			Vec3 origin2 = origin;
			origin2 -= globalFrame.rotation.f * _wakeBB.max.z * 1.85f;
			float num7 = (0.5f + (MBRandom.RandomFloat - 0.5f) * 0.5f) * 0.33f;
			_nextDecalSpawnMetersSq = num7 * num7;
			Vec3 v2 = s2;
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = origin2;
			identity.rotation.u = Vec3.Up;
			Vec3 vec2 = globalFrame.rotation.TransformToParent(in v2);
			vec2.z = 0f;
			vec2.Normalize();
			identity.rotation.s = vec2;
			identity.rotation.f = -identity.rotation.s.CrossProductWithUp();
			identity.rotation.f.Normalize();
			shipFoamDecal2._cumulativeDtTillStart = 0f;
			float num8 = 0.6f;
			shipFoamDecal2._randomScale = Vec3.One * (0.9f + MBRandom.RandomFloat * 0.2f) * num8;
			shipFoamDecal2._randomScale.x *= 1f * MBRandom.RandomFloat + 0.4f;
			identity.rotation.ApplyScaleLocal(in scaleAmountXYZ);
			shipFoamDecal2._splashFoamDecal.Frame = identity;
			shipFoamDecal2._splashFoamDecal.SetAlpha(0f);
			shipFoamDecal2._currentFrame = identity;
			int num9 = MBRandom.RandomInt(3);
			float vectorArgument = (float)(num9 % 2) * 0.5f;
			float vectorArgument2 = (float)(num9 / 2) * 0.5f;
			shipFoamDecal2._splashFoamDecal.SetVectorArgument(vectorArgument, vectorArgument2, -0.5f, -0.5f);
			float num10 = 0.16f * (0.8f + MBRandom.RandomFloat * 0.4f);
			float num11 = 0.45f * (0.8f + MBRandom.RandomFloat * 0.4f);
			shipFoamDecal2._currentSpeed = vec * num11 + identity.rotation.s * vec.Length * num10;
			float a = -0.34906584f * (0.8f + MBRandom.RandomFloat * 0.4f);
			shipFoamDecal2._sideVectorStart = vec2;
			shipFoamDecal2._sideVectorStart.RotateAboutZ(System.MathF.PI / 2f);
			shipFoamDecal2._sideVectorEnd = shipFoamDecal2._sideVectorStart;
			shipFoamDecal2._sideVectorEnd.RotateAboutZ(a);
			Vec2 data = new Vec2(2.5f, 2.5f);
			shipFoamDecal2._splashFoamDecal.OverrideRoadBoundaryP0(data);
			Vec2 data2 = new Vec2(MBRandom.RandomFloat, MBRandom.RandomFloat);
			shipFoamDecal2._splashFoamDecal.OverrideRoadBoundaryP1(data2);
			_nextDecalToUse = (_nextDecalToUse + 1) % 20;
			_lastDecalSpawnPosition = origin;
		}
	}

	private void TickSwayingAnimation(ref MatrixFrame entityFrame)
	{
		float num = MBMath.WrapAngle(base.MapEntity.MobileParty.Bearing.RotationInRadians - _bearingRotation);
		if (!num.ApproximatelyEqualsTo(0f, 0.003f))
		{
			float epsilon = 0.5f;
			float amount = 0.1f;
			if (base.MapEntity.MobileParty.TargetParty != null)
			{
				epsilon = 1.5f;
				amount = 0.01f * MBMath.Map(num, 0f, System.MathF.PI, 1f, 10f);
			}
			if (_swayingAngle == 0f || !_targetPositionForSwaying.Distance(base.MapEntity.MobileParty.NextTargetPosition).ApproximatelyEqualsTo(0f, epsilon))
			{
				_swayingAngle = num;
				_targetPositionForSwaying = base.MapEntity.MobileParty.NextTargetPosition;
			}
			float x = ((!(_swayingAngle >= 0f)) ? MBMath.Map(num, _swayingAngle, 0f, -System.MathF.PI, 0f) : MBMath.Map(num, 0f, _swayingAngle, 0f, System.MathF.PI));
			float num2 = MBMath.Map(TaleWorlds.Library.MathF.Abs(_swayingAngle), 0f, System.MathF.PI, 0f, System.MathF.PI / 5f);
			float valueTo = MBMath.Map(TaleWorlds.Library.MathF.Sin(x), -1f, 1f, 0f - num2, num2);
			_rollingAngle = MBMath.LerpRadians(_rollingAngle, valueTo, amount, 0f, num2);
			entityFrame.rotation.RotateAboutForward(_rollingAngle);
		}
	}

	private void CheckBridgeFadeState()
	{
		if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(base.MapEntity.MobileParty.CurrentNavigationFace) == TerrainType.UnderBridge)
		{
			GameEntity nearbyBridgeToParty = NavalMobilePartyVisualManager.Current.GetNearbyBridgeToParty(base.MapEntity);
			nearbyBridgeToParty?.SetAlpha(0.3f);
			if (_currentCollidedBridgeEntity != nearbyBridgeToParty)
			{
				_currentCollidedBridgeEntity?.SetAlpha(1f);
				_currentCollidedBridgeEntity = nearbyBridgeToParty;
			}
		}
		else
		{
			_currentCollidedBridgeEntity?.SetAlpha(1f);
			_currentCollidedBridgeEntity = null;
		}
	}

	private void ApplyWindEffect(Vec2 windVector, Vec2 shipDirection, float realDt, float dt)
	{
		if (TaleWorlds.Library.MathF.Abs(windVector.AngleBetween(shipDirection).ToDegrees()) > 80f)
		{
			if (!_isSailFolded && _sailVisualCache.Count > 0)
			{
				_isSailFolded = true;
				NavalDLCViewHelpers.ShipVisualHelper.FoldSails(_sailVisualCache);
			}
		}
		else if (_isSailFolded && _sailVisualCache.Count > 0)
		{
			_isSailFolded = false;
			NavalDLCViewHelpers.ShipVisualHelper.UnfoldSails(_sailVisualCache);
		}
		if (!base.MapEntity.MobileParty.IsMainParty)
		{
			if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(base.MapEntity.MobileParty.CurrentNavigationFace) == TerrainType.UnderBridge)
			{
				_sailAlpha = TaleWorlds.Library.MathF.Max(_sailAlpha - TaleWorlds.Library.MathF.Max(realDt, 1E-05f), 0.01f);
				if (_sailAlpha > 0.00999f)
				{
					foreach (SailVisual item in _sailVisualCache)
					{
						item.SetSailEntityAlpha(_sailAlpha);
					}
				}
			}
			else
			{
				_sailAlpha = TaleWorlds.Library.MathF.Min(_sailAlpha + TaleWorlds.Library.MathF.Max(realDt, 1E-05f), 1f);
				if (_sailAlpha < 1.00001f)
				{
					foreach (SailVisual item2 in _sailVisualCache)
					{
						item2.SetSailEntityAlpha(_sailAlpha);
					}
				}
			}
		}
		float length = windVector.Length;
		Vec3 vec = windVector.Normalized().ToVec3();
		if (_sailVisualCache.Any() && !_isSailFolded)
		{
			float num = TaleWorlds.Library.MathF.Clamp(length * 5f, 0.5f, 2.5f);
			foreach (SailVisual item3 in _sailVisualCache)
			{
				item3?.SailClothComponent?.SetForcedWind(vec * num, isLocal: false);
			}
		}
		if (_sailVisualCache.Any())
		{
			float num2 = TaleWorlds.Library.MathF.Clamp(length * 3f, 0.3f, 2.5f);
			foreach (SailVisual item4 in _sailVisualCache)
			{
				item4?.SailTopBannerClothComponent?.SetForcedWind(vec * num2, isLocal: false);
			}
		}
		if (_raidAgentVisuals != null && !_raidAgentVisuals.GetEquipment()[EquipmentIndex.ExtraWeaponSlot].IsEmpty)
		{
			_raidAgentVisuals.SetClothWindToWeaponAtIndex(-StrategicEntity.GetGlobalFrame().rotation.f, isLocal: false, EquipmentIndex.ExtraWeaponSlot);
		}
	}

	private void AddRaftVisual()
	{
		_shipEntity = GameEntity.Instantiate(StrategicEntity.Scene, "raft", MatrixFrame.Identity);
		StrategicEntity.AddChild(_shipEntity);
		bool isMainParty = base.MapEntity.MobileParty.IsMainParty;
		_shipEntity.SetVisibilityExcludeParents(isMainParty);
		_shipEntity.SetAlpha(isMainParty ? 1f : 0f);
		_sailingSoundEvent = SoundEvent.CreateEventFromString("event:/map/army/sail", NavalMobilePartyVisualManager.Current.MapScene);
		_sailingSoundEvent.SetPosition(GetVisualPosition());
		_isVisualInRaftState = true;
		_bodyMeshEntity = _shipEntity.WeakEntity.GetFirstChildEntityWithTagRecursive("body_mesh");
	}

	private void RemoveBlockadeVisuals()
	{
		if (_shipToBlockadeShipVisualCache.IsEmpty())
		{
			return;
		}
		foreach (KeyValuePair<Ship, BlockadeShipVisual> item in _shipToBlockadeShipVisualCache)
		{
			item.Value.ShipEntity.SetVisibilityExcludeParents(visible: false);
			item.Value.ShipEntity.ClearComponents();
		}
		_shipToBlockadeShipVisualCache.Clear();
	}

	private bool HasNavalVisual()
	{
		if ((base.MapEntity.MobileParty.Ships.Count <= 0 && !base.MapEntity.MobileParty.IsInNavalAutoTravel) || !base.MapEntity.MobileParty.IsCurrentlyAtSea || (base.MapEntity.MobileParty.CurrentSettlement != null && !base.MapEntity.MobileParty.IsTargetingPort))
		{
			if (base.MapEntity.MobileParty.Ships.Count > 0 && base.MapEntity.MobileParty.SiegeEvent?.BesiegedSettlement != null)
			{
				return base.MapEntity.MobileParty.SiegeEvent?.IsBlockadeActive ?? false;
			}
			return false;
		}
		return true;
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

	private ShipHull GetFerryShipHull()
	{
		if (_ferryShipHull == null)
		{
			_ferryShipHull = NavalDLCViewHelpers.ShipVisualHelper.GetFerryShipHullForSettlement(Campaign.Current.PlayerDataForNavalAutoTravel.DeparturedVillageWithFerry);
		}
		return _ferryShipHull;
	}
}
