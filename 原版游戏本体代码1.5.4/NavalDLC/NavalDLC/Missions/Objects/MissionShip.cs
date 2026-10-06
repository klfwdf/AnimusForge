using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.DWA;
using NavalDLC.Missions.AI.UsableMachineAIs;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Missions.ShipInput;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Usables;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Missions.Objects;

public class MissionShip : MissionObject
{
	public enum ShipInstanceType
	{
		None,
		MissionShip,
		EditorShip
	}

	public enum SailState : byte
	{
		Intact,
		Burning,
		Destroyed
	}

	public struct ShipCollisionData
	{
		public MissionShip CollidingShip;

		public Vec3 ContactPosAvg;

		public float Damage;

		public ShipCollisionData(MissionShip collidingShip, Vec3 contactPosAvg, float damage)
		{
			CollidingShip = collidingShip;
			ContactPosAvg = contactPosAvg;
			Damage = damage;
		}
	}

	private struct ShipToEntityCollisionStatus
	{
		public readonly GameEntity CollidingEntity;

		public IntPtr CollidingBodyPtr;

		public PhysicsEventType CurrentCollisionState { get; private set; }

		public ShipToEntityCollisionStatus(WeakGameEntity collidingEntity, PhysicsEventType collisionEventType)
		{
			CollidingEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(collidingEntity);
			CollidingBodyPtr = IntPtr.Zero;
			CurrentCollisionState = collisionEventType;
		}

		public ShipToEntityCollisionStatus(IntPtr collidingBodyPtr, PhysicsEventType collisionEventType)
		{
			CollidingEntity = null;
			CollidingBodyPtr = collidingBodyPtr;
			CurrentCollisionState = collisionEventType;
		}

		public void UpdateCurrentCollisionState(PhysicsEventType newCollisionState)
		{
			CurrentCollisionState = newCollisionState;
		}
	}

	private const int DetanglingDuration = 6;

	private const float DamageCooldownForShipInSeconds = 2f;

	private const float CollisionDirectionSpeedThresholdToDamage = 3f;

	private const float MaxSoundPositionUpdateDistanceSquared = 10000f;

	public const string OuterDeckTroopSpTag = "sp_troop_outer_deck";

	public const string InnerDeckTroopSpTag = "sp_troop_inner_deck";

	public const string CaptainTroopSpTag = "sp_troop_captain";

	public const string CrewTroopSpTag = "sp_troop_crew_spawn";

	public const string RallyPointTag = "rally_point";

	public const string BannerTag = "banner_with_faction_color";

	public const string SailMeshTag = "sail_mesh_entity";

	public const float NavmeshDisableLimit = 0.35f;

	private static TextObject PlayerSideShipSinkingText = new TextObject("{=jX6yqP3T}A friendly ship has started to sink!");

	private static TextObject EnemySideShipSinkingText = new TextObject("{=nvTWWBib}An enemy ship has started to sink!");

	private readonly MBList<MissionShip> _temporaryMissionShipContainer = new MBList<MissionShip>();

	private readonly MBQueue<MissionShip> _temporaryMissionShipQueue = new MBQueue<MissionShip>();

	private static readonly int _scrapeSoundEventID = SoundEvent.GetEventIdFromString("event:/physics/vessel/ship_scraping");

	private readonly QueryData<bool> _anyActiveFormationTroopOnShip;

	private SailInput _customSailSetting;

	private MBList<(int, SoundEvent)> _scrapeSoundEvents;

	private MissionShipObject _missionShipObject;

	public bool ShouldUpdateSoundPos;

	private NavalAgentMoraleInteractionLogic _moraleInteractionLogic;

	private MBList<MatrixFrame> _outerDeckLocalFrames;

	private MBList<MatrixFrame> _innerDeckLocalFrames;

	private MBList<MatrixFrame> _crewSpawnLocalFrames;

	private int _nextDeckSpawnFrameIndex;

	private bool _autoUpdateController = true;

	private int _nextCrewSpawnFrameIndex;

	private MBList<ShipAttachmentMachine> _attachmentMachines;

	private MBList<IShipEventListener> _shipEventListeners;

	private bool _isCapsized;

	private MBList<ShipAttachmentPointMachine> _attachmentPointMachines;

	private MBList<ShipShieldComponent> _shields;

	private Timer _capsizeDamageTimer;

	private MBList<GameEntity> _bannerEntities;

	private MBList<GameEntity> _sailMeshEntities;

	private WorldPosition _cachedWorldPositionOnDeck;

	private bool _isCachedWorldPositionOnDeckDirty = true;

	private GameEntity _playerStandingPointEntity;

	private bool _isRemoved;

	private bool _foldSailsOnBridgeConnection = true;

	private HashSet<MissionShip> _visitedMissionShips;

	private float _nextPermanentBurnDamageTime;

	private float _nextFireHitPointRestoreTime;

	private Vec2[] _localPhysicsBoundingBoxXYPlaneVertices;

	private Vec2[] _scaledLocalPhysicsBoundingBoxXYPlaneVertices;

	private Vec2[] _physicsBoundingBoxXYPlaneVertices;

	private Vec2[] _criticalZoneVertices;

	private MissionShip _detanglingMissionShip;

	private Vec3 _detanglingMissionShipAverageContactPosition;

	private MissionTimer _detanglingMissionShipTimer;

	private ShipInputProcessor _inputProcessor;

	private NavalDLC.Missions.ShipActuators.ShipActuators _actuators;

	private ShipInputRecord _inputRecord;

	private NavalDLC.Missions.NavalPhysics.NavalPhysics _physics;

	private float[] _partialHitPoints;

	private MBList<ShipOarMachine> _leftSideShipOarMachines;

	private MBList<ShipOarMachine> _rightSideShipOarMachines;

	private MBList<ShipOarMachine> _shipOarMachines;

	private MBList<ShipUnmannedOar> _shipUnmannedOars;

	private MBList<ClimbingMachine> _climbingMachines;

	private MBList<DestructableComponent> _allDestructibleComponents;

	private ShipDWAAgentDelegate _dwaAgentDelegate;

	private MissionShipRam _ram;

	private MBList<AmmoBarrelBase> _ammoBarrels;

	private float _connectionBlockedShipTime;

	private float _disconnectionBlockedShipTime;

	private MBList<SailVisual> _sailVisuals;

	private BoundingBox _localBoundingBoxCached;

	private bool _localBoundingBoxCacheInvalid = true;

	private List<ShipToEntityCollisionStatus> _currentCollisionStatesToShips = new List<ShipToEntityCollisionStatus>();

	private readonly Dictionary<MissionShip, float> _shipDamageCooldowns = new Dictionary<MissionShip, float>();

	private readonly ConcurrentQueue<ShipCollisionData> _shipCollisionData = new ConcurrentQueue<ShipCollisionData>();

	private static uint _missionShipScriptNameHash = Managed.GetStringHashCode("MissionShip");

	public static int MaxShipIndex { get; private set; }

	public bool AnyActiveFormationTroopOnShip => _anyActiveFormationTroopOnShip.Value;

	public int Index { get; private set; }

	public bool IsRemoved => _isRemoved;

	public MatrixFrame GlobalFrame => base.GameEntity.GetGlobalFrame();

	public MBReadOnlyList<MatrixFrame> OuterDeckLocalFrames => _outerDeckLocalFrames;

	public MBReadOnlyList<MatrixFrame> InnerDeckLocalFrames => _innerDeckLocalFrames;

	public MBReadOnlyList<MatrixFrame> CrewSpawnLocalFrames => _crewSpawnLocalFrames;

	public int DeckFrameCount => _innerDeckLocalFrames.Count + _outerDeckLocalFrames.Count;

	public MBReadOnlyList<GameEntity> BannerEntities => _bannerEntities;

	public MBReadOnlyList<GameEntity> SailMeshEntities => _sailMeshEntities;

	public Banner Banner
	{
		get
		{
			if (!ShipHelper.TryGetShipBanner(ShipOrigin, out var banner, Captain) && Team != null)
			{
				return Team.Banner;
			}
			return banner;
		}
	}

	public (uint sailColor1, uint sailColor2) SailColors
	{
		get
		{
			if (!ShipHelper.TryGetSailColors(ShipOrigin, out (uint, uint) sailColors, Captain) && Team != null)
			{
				return (sailColor1: Team.Color, sailColor2: Team.Color2);
			}
			return sailColors;
		}
	}

	public NavalDLC.Missions.NavalPhysics.NavalPhysics Physics => _physics;

	public float MaxHealth => ShipOrigin.MaxHitPoints;

	public float MaxFireHealth => ShipOrigin.MaxFireHitPoints;

	public float MaxPartialHealth => MaxHealth * _missionShipObject.PartialHitPointsRatio;

	public int TotalCrewCapacity => ShipOrigin.TotalCrewCapacity;

	public int CrewSizeOnMainDeck { get; private set; }

	public int CrewSizeOnLowerDeck => ShipOrigin.TotalCrewCapacity - CrewSizeOnMainDeck;

	public bool HasController => Controller != null;

	public AIShipController AIController => (AIShipController)Controller;

	public bool IsAIControlled
	{
		get
		{
			if (HasController)
			{
				return Controller.IsAIControlled;
			}
			return false;
		}
	}

	public bool IsPlayerControlled
	{
		get
		{
			if (HasController)
			{
				return Controller.IsPlayerControlled;
			}
			return false;
		}
	}

	public bool IsFormationAndShipAIControlled
	{
		get
		{
			if (Formation != null && Formation.IsAIControlled)
			{
				return IsAIControlled;
			}
			return false;
		}
	}

	public PlayerShipController PlayerController => (PlayerShipController)Controller;

	public FormationClass FormationIndex => Formation?.FormationIndex ?? FormationClass.NumberOfAllFormations;

	public BattleSideEnum BattleSide => Team?.Side ?? BattleSideEnum.None;

	public MissionShipObject MissionShipObject => _missionShipObject;

	public NavalShipsLogic ShipsLogic { get; private set; }

	public Team Team => Formation?.Team;

	public Formation Formation { get; private set; }

	public Agent Captain => Formation?.Captain;

	public bool IsInitialized => _missionShipObject != null;

	public bool IsRetreating => false;

	public SailState ShipSailState { get; private set; }

	public bool HasCustomSailSetting { get; private set; }

	public bool IsSinking
	{
		get
		{
			if (_physics.NavalSinkingState != NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sinking)
			{
				return _physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sunk;
			}
			return true;
		}
	}

	public bool IsSunk => _physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sunk;

	public ShipOrder ShipOrder { get; private set; }

	public IShipOrigin ShipOrigin { get; private set; }

	public bool IsPlayerShip => Agent.Main?.GetComponent<AgentNavalComponent>()?.FormationShip == this;

	public MatrixFrame RallyFrame { get; private set; }

	public float HitPoints => ShipOrigin.HitPoints;

	public float FireHitPoints { get; private set; }

	public float BurntHullDamageTotal { get; private set; }

	public float VisualRudderRotationPercentage => _actuators.VisualRudderLocalRotation / MissionShipObject.RudderRotationMax;

	public float VisualRudderRotation => _actuators.VisualRudderLocalRotation;

	public float VisualRudderPullDirection => _actuators.VisualRudderPullDirection;

	public float SailTargetSetting => _actuators.Sails.FirstOrDefault()?.TargetSailSetting ?? 0f;

	public MBReadOnlyList<MissionSail> Sails => _actuators.Sails;

	public ulong ShipUniqueBitwiseID { get; private set; }

	public ulong ShipIslandCombinedID { get; private set; }

	public bool IsShipOrderActive { get; private set; } = true;

	public bool IsClimbingMachineStandAloneTickingActive { get; private set; }

	public MBReadOnlyList<ShipAttachmentMachine> AttachmentMachines => _attachmentMachines;

	public MBReadOnlyList<ShipAttachmentPointMachine> AttachmentPointMachines => _attachmentPointMachines;

	public MBReadOnlyList<ShipShieldComponent> Shields => _shields;

	public ClimbingMachineDetachment ClimbingMachineDetachment { get; private set; }

	public MBReadOnlyList<ShipOarMachine> LeftSideShipOarMachines => _leftSideShipOarMachines;

	public MBReadOnlyList<ShipOarMachine> RightSideShipOarMachines => _rightSideShipOarMachines;

	public MBReadOnlyList<ShipOarMachine> ShipOarMachines => _shipOarMachines;

	public MBReadOnlyList<ClimbingMachine> ClimbingMachines => _climbingMachines;

	public MBReadOnlyList<ShipUnmannedOar> ShipUnmannedOars => _shipUnmannedOars;

	public MBReadOnlyList<DestructableComponent> AllDestructableComponents => _allDestructibleComponents;

	public ShipControllerMachine ShipControllerMachine { get; private set; }

	public float MaxSailHitPoints => ShipOrigin.MaxSailHitPoints;

	public float SailHitPoints => ShipOrigin.SailHitPoints;

	public bool IsDeployed { get; private set; }

	public bool CanBeTakenOver { get; private set; } = true;

	public TeamSideEnum OriginalTeamSide { get; private set; } = TeamSideEnum.None;

	public Agent SailBurnerAgent { get; private set; }

	public SoundEvent SailBurningSoundEvent { get; private set; }

	public ShipController Controller { get; private set; }

	public RangedSiegeWeapon ShipSiegeWeapon { get; private set; }

	public bool IsShipNavmeshDisabled { get; private set; }

	public bool HasDWAAgent => _dwaAgentDelegate != null;

	public int DWAAgentId => _dwaAgentDelegate.Id;

	public ref readonly DWAAgentState DWAAgentState => ref _dwaAgentDelegate.State;

	public ShipPlacementDetachment ShipPlacementDetachment { get; private set; }

	public bool HasPlayerStandingPointEntity => PlayerStandingPointEntity != null;

	public GameEntity PlayerStandingPointEntity => _playerStandingPointEntity;

	public override TextObject HitObjectName => new TextObject("{=1nbU1tV5}Ship");

	public bool BeingAbandoned { get; private set; }

	public static uint MissionShipScriptNameHash => _missionShipScriptNameHash;

	public MissionShip()
	{
		_anyActiveFormationTroopOnShip = new QueryData<bool>(delegate
		{
			Formation formation = Formation;
			if (formation != null && formation.CountOfUnits > 0)
			{
				foreach (IFormationUnit allUnit in Formation.Arrangement.GetAllUnits())
				{
					if (allUnit is Agent agent)
					{
						AgentMovementMode agentMovementMode = agent.MovementMode & AgentMovementMode.WaterDiving;
						if (agentMovementMode != AgentMovementMode.WaterSurface && agentMovementMode != AgentMovementMode.WaterDiving)
						{
							return true;
						}
					}
				}
				foreach (Agent detachedUnit in Formation.DetachedUnits)
				{
					Agent current;
					if ((current = detachedUnit) != null)
					{
						AgentMovementMode agentMovementMode2 = current.MovementMode & AgentMovementMode.WaterDiving;
						if (agentMovementMode2 != AgentMovementMode.WaterSurface && agentMovementMode2 != AgentMovementMode.WaterDiving)
						{
							return true;
						}
					}
				}
			}
			return false;
		}, 5f);
	}

	public void BreakAllExistingConnections()
	{
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null)
			{
				attachmentMachine.CurrentAttachment.Destroy();
				attachmentMachine.CheckCurrentAttachmentAndInitializeRopeBoundingBox();
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null)
			{
				attachmentPointMachine.CurrentAttachment.AttachmentSource?.CheckCurrentAttachmentAndInitializeRopeBoundingBox();
				attachmentPointMachine.CurrentAttachment.Destroy();
			}
		}
	}

	public bool IsConnectionBlocked()
	{
		return _connectionBlockedShipTime > Mission.Current.CurrentTime;
	}

	public bool IsConnectionPermanentlyBlocked()
	{
		return _connectionBlockedShipTime.ApproximatelyEqualsTo(float.MaxValue);
	}

	public bool IsDisconnectionBlocked()
	{
		return _disconnectionBlockedShipTime > Mission.Current.CurrentTime;
	}

	public void BlockConnection()
	{
		_connectionBlockedShipTime = float.MaxValue;
	}

	public void ResetDisconnectionBlock()
	{
		_disconnectionBlockedShipTime = 0f;
	}

	public void ResetConnectionBlock()
	{
		_connectionBlockedShipTime = 0f;
	}

	public void SetShipOrderActive(bool isOrderActive)
	{
		IsShipOrderActive = isOrderActive;
	}

	public void SetShipClimbingOrderStandAloneTickingActive(bool isShipClimbingMachineStandaloneTickingActive)
	{
		IsClimbingMachineStandAloneTickingActive = isShipClimbingMachineStandaloneTickingActive;
	}

	public void SetFoldSailsOnBridgeConnection(bool value)
	{
		_foldSailsOnBridgeConnection = value;
	}

	public void SetOriginalTeamSide(TeamSideEnum teamSide)
	{
		OriginalTeamSide = teamSide;
	}

	public void OnShipConnected(ShipAttachmentMachine.ShipAttachment currentAttachment)
	{
		if (currentAttachment.AttachmentTarget.OwnerShip != this || currentAttachment.AttachmentSource.OwnerShip.BattleSide == BattleSide)
		{
			return;
		}
		bool flag = true;
		foreach (ShipAttachmentMachine attachmentMachine in AttachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != currentAttachment && attachmentMachine.CurrentAttachment?.AttachmentTarget?.OwnerShip == this && attachmentMachine.CurrentAttachment.AttachmentSource.OwnerShip.BattleSide != BattleSide)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			_disconnectionBlockedShipTime = Mission.Current.CurrentTime + 30f;
			_connectionBlockedShipTime = 0f;
		}
	}

	public void SetPlayerStandingPointEntity(GameEntity entity = null)
	{
		_playerStandingPointEntity = entity;
	}

	public void OnShipDisconnected(ShipAttachmentMachine.ShipAttachment currentAttachment)
	{
		if (ShipsLogic.CanHaveConnectionCooldown && currentAttachment.AttachmentTarget.OwnerShip == this && _connectionBlockedShipTime <= 0f)
		{
			_connectionBlockedShipTime = Mission.Current.CurrentTime + 30f;
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		if (_isRemoved)
		{
			return TickRequirement.None;
		}
		TickRequirement tickRequirement = TickRequirement.TickParallel | TickRequirement.FixedTick | TickRequirement.FixedParallelTick;
		if (Mission.Current != null)
		{
			tickRequirement |= TickRequirement.Tick;
		}
		return tickRequirement;
	}

	public override void OnDeploymentFinished()
	{
		FinalizeDeployment(initializeMachines: false);
	}

	public void FinalizeDeployment(bool initializeMachines)
	{
		if (initializeMachines)
		{
			foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
			{
				attachmentMachine.OnDeploymentFinished();
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
			{
				attachmentPointMachine.OnDeploymentFinished();
			}
			foreach (ShipOarMachine leftSideShipOarMachine in _leftSideShipOarMachines)
			{
				leftSideShipOarMachine.OnDeploymentFinished();
			}
			foreach (ShipOarMachine rightSideShipOarMachine in _rightSideShipOarMachines)
			{
				rightSideShipOarMachine.OnDeploymentFinished();
			}
			foreach (ClimbingMachine climbingMachine in _climbingMachines)
			{
				climbingMachine.OnDeploymentFinished();
			}
			ShipControllerMachine.OnDeploymentFinished();
			ShipSiegeWeapon?.OnDeploymentFinished();
			foreach (AmmoBarrelBase ammoBarrel in _ammoBarrels)
			{
				ammoBarrel.OnDeploymentFinished();
			}
			_ram?.OnDeploymentFinished();
		}
		SetSiegeWeaponsInitialAmmoCount();
		CrewSizeOnMainDeck = MissionGameModels.Current.MissionShipParametersModel.CalculateMainDeckCrewSize(ShipOrigin, Formation.GetFirstUnit());
		SetAnchor(isAnchored: false);
		base.GameEntity.GetFirstScriptOfTypeRecursive<ShipWaterEffects>()?.EnableWakeAndParticles();
		base.GameEntity.GetFirstScriptOfTypeRecursive<ShipFloatsamManager>()?.EnableFloatsamSystem();
		IsDeployed = true;
	}

	private void SetSiegeWeaponsInitialAmmoCount()
	{
		if (ShipSiegeWeapon != null)
		{
			int startAmmo = MissionGameModels.Current.MissionSiegeEngineCalculationModel.CalculateShipSiegeWeaponAmmoCount(ShipOrigin, Captain, ShipSiegeWeapon);
			ShipSiegeWeapon.SetStartAmmo(startAmmo);
		}
	}

	public override void SetAbilityOfFaces(bool enabled)
	{
		if (DynamicNavmeshIdStart > 0)
		{
			for (int i = 0; i < 49; i++)
			{
				base.GameEntity.Scene.SetAbilityOfFacesWithId(DynamicNavmeshIdStart + i, enabled);
			}
		}
	}

	public bool IsAgentOnShipNavmesh(int testedNavmeshID)
	{
		if (testedNavmeshID >= DynamicNavmeshIdStart)
		{
			return testedNavmeshID < DynamicNavmeshIdStart + 50;
		}
		return false;
	}

	public float GetPartialHitPoints(int index)
	{
		return _partialHitPoints[index];
	}

	public void SetController(ShipControllerType controllerType, bool autoUpdateController = true)
	{
		_autoUpdateController = autoUpdateController;
		if ((HasController ? Controller.ControllerType : ShipControllerType.None) != controllerType)
		{
			switch (controllerType)
			{
			case ShipControllerType.Player:
				Controller = new PlayerShipController(this);
				break;
			case ShipControllerType.AI:
				Controller = new AIShipController(this);
				break;
			default:
				Controller = null;
				break;
			}
			ShipsLogic.OnShipControllerChanged(this);
		}
	}

	public void SetCanBeTakenOver(bool value)
	{
		CanBeTakenOver = value;
	}

	public MBReadOnlyList<MissionShip> GetShipsConnectedWithBridges()
	{
		_temporaryMissionShipContainer.Clear();
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null && attachmentMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected && _temporaryMissionShipContainer.IndexOf(attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip) < 0)
			{
				_temporaryMissionShipContainer.Add(attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip);
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null && attachmentPointMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected && _temporaryMissionShipContainer.IndexOf(attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip) < 0)
			{
				_temporaryMissionShipContainer.Add(attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip);
			}
		}
		return _temporaryMissionShipContainer;
	}

	public void SetInputRecord(in ShipInputRecord record)
	{
		_inputRecord = record;
	}

	public void SetOarAppliedForceMultiplierForStoryMission(float forceMultiplier)
	{
		_actuators.SetOarAppliedForceMultiplierForStoryMission(forceMultiplier);
	}

	public bool SearchShipConnection(MissionShip soughtShip, bool isDirect, bool findEnemy, bool enforceActive, bool acceptNotBridgedConnections)
	{
		_temporaryMissionShipQueue.Clear();
		_visitedMissionShips.Clear();
		bool flag = false;
		foreach (MissionShip item in acceptNotBridgedConnections ? GetConnectedShips() : GetShipsConnectedWithBridges())
		{
			if (item != this && item.Team != null)
			{
				if (item == soughtShip)
				{
					flag = true;
				}
				if (isDirect && (item == soughtShip || (findEnemy == Team?.IsEnemyOf(item.Team) && (!enforceActive || item.AnyActiveFormationTroopOnShip))))
				{
					_temporaryMissionShipQueue.Clear();
					_visitedMissionShips.Clear();
					return true;
				}
				_temporaryMissionShipQueue.Enqueue(item);
			}
		}
		_visitedMissionShips.Add(this);
		while (_temporaryMissionShipQueue.Count > 0)
		{
			MissionShip missionShip = _temporaryMissionShipQueue.Dequeue();
			_visitedMissionShips.Add(missionShip);
			if ((!flag && missionShip == soughtShip) || (missionShip.Team != null && findEnemy == Team?.IsEnemyOf(missionShip.Team) && (!enforceActive || missionShip.AnyActiveFormationTroopOnShip)))
			{
				_temporaryMissionShipQueue.Clear();
				_visitedMissionShips.Clear();
				return true;
			}
			foreach (MissionShip item2 in acceptNotBridgedConnections ? missionShip.GetConnectedShips() : missionShip.GetShipsConnectedWithBridges())
			{
				if (!_visitedMissionShips.Contains(item2))
				{
					_temporaryMissionShipQueue.Enqueue(item2);
				}
			}
		}
		_temporaryMissionShipQueue.Clear();
		_visitedMissionShips.Clear();
		return false;
	}

	public void SetFormation(Formation newFormation)
	{
		if (Formation != newFormation)
		{
			if (Formation != null)
			{
				ShipOrder.FormationLeaveShip();
				Formation.OnUnitAttached -= OnUnitAttached;
			}
			Formation = newFormation;
			if (newFormation != null)
			{
				ShipOrder.FormationJoinShip(Formation);
				Formation.OnUnitAttached += OnUnitAttached;
			}
		}
	}

	private void ProcessDetanglingShips()
	{
		if (_detanglingMissionShip != null)
		{
			float remainingTimeInSeconds = _detanglingMissionShipTimer.GetRemainingTimeInSeconds();
			if (remainingTimeInSeconds <= 3f)
			{
				float detanglingHarshness = 1f - remainingTimeInSeconds / 3f;
				DetangleShip(detanglingHarshness);
			}
		}
	}

	private void AddDetanglingShip(MissionShip ship, Vec3 contactPosAvg)
	{
		if (_detanglingMissionShip == null || _detanglingMissionShip == ship)
		{
			if (_detanglingMissionShip == null)
			{
				_detanglingMissionShipTimer.Reset();
			}
			_detanglingMissionShip = ship;
			_detanglingMissionShipAverageContactPosition = contactPosAvg;
		}
	}

	private void RemoveDetanglingShip(MissionShip ship)
	{
		if (ship == _detanglingMissionShip)
		{
			_detanglingMissionShip = null;
			_detanglingMissionShipTimer.Reset();
		}
	}

	public static float CalculateShipAlignWithVectorZTorque(MissionShip ship, Vec3 alignVector)
	{
		Mat3 rotation = ship.GameEntity.GetBodyWorldTransform().rotation;
		float num = TaleWorlds.Library.MathF.Atan2(rotation.f.y, rotation.f.x);
		float angle = TaleWorlds.Library.MathF.Atan2(alignVector.y, alignVector.x) - num;
		angle = MBMath.WrapAngle(angle);
		if (TaleWorlds.Library.MathF.Abs(angle) > System.MathF.PI / 2f)
		{
			angle = ((!(angle > 0f)) ? (angle + System.MathF.PI) : (angle - System.MathF.PI));
		}
		return angle * 0.5f * ship.Physics.Mass * 50f - ship.Physics.AngularVelocity.z * ship.Physics.Mass * 60f;
	}

	private void DetangleShip(float detanglingHarshness)
	{
		MatrixFrame shipFrame = _detanglingMissionShip.GameEntity.GetBodyWorldTransform();
		if (!(shipFrame.TransformToLocal(in _detanglingMissionShipAverageContactPosition).z < 0f))
		{
			return;
		}
		MatrixFrame shipFrame2 = base.GameEntity.GetBodyWorldTransform();
		Vec2[] polygon = CalculateBoundingXYGlobalPlaneFromLocal(in shipFrame2, 0.9f);
		Vec2[] polygon2 = _detanglingMissionShip.CalculateBoundingXYGlobalPlaneFromLocal(in shipFrame, 0.9f);
		if (MBMath.CheckPolygonIntersection(polygon, polygon2))
		{
			Vec3 vec = (shipFrame.origin - _detanglingMissionShipAverageContactPosition).NormalizedCopy();
			if (vec.AsVec2.LengthSquared < 0.01f)
			{
				vec.x = shipFrame.rotation.f.x;
				vec.y = shipFrame.rotation.f.y;
				vec.Normalize();
			}
			float num = 2f * detanglingHarshness;
			float mass = _detanglingMissionShip.Physics.Mass;
			float num2 = TaleWorlds.Library.MathF.Min(_detanglingMissionShip.Physics.Mass, Physics.Mass);
			Vec3 globalForceVec = vec * mass * num;
			Vec3 localPos = shipFrame.TransformToLocal(in _detanglingMissionShipAverageContactPosition);
			float num3 = num2 * 5f;
			if (globalForceVec.LengthSquared >= num3 * num3)
			{
				globalForceVec.Normalize();
				globalForceVec *= num3;
			}
			_detanglingMissionShip.Physics.ApplyGlobalForceAtLocalPos(in localPos, in globalForceVec);
			Vec3 localPos2 = shipFrame2.TransformToLocal(in _detanglingMissionShipAverageContactPosition);
			Physics.ApplyGlobalForceAtLocalPos(in localPos2, -globalForceVec);
		}
		float z = CalculateShipAlignWithVectorZTorque(_detanglingMissionShip, shipFrame2.rotation.s);
		_detanglingMissionShip.Physics.ApplyTorque(new Vec3(0f, 0f, z));
	}

	private void InitializeDetanglingShipInformation()
	{
		_detanglingMissionShip = null;
		_detanglingMissionShipTimer = new MissionTimer(6f);
		_detanglingMissionShipAverageContactPosition = Vec3.Zero;
	}

	private void InitializeLocalPhysicsBoundingXYPlane()
	{
		_localPhysicsBoundingBoxXYPlaneVertices = new Vec2[4];
		_scaledLocalPhysicsBoundingBoxXYPlaneVertices = new Vec2[4];
		Vec3 min = Physics.PhysicsBoundingBoxWithoutChildren.min;
		Vec3 max = Physics.PhysicsBoundingBoxWithoutChildren.max;
		_localPhysicsBoundingBoxXYPlaneVertices[0] = new Vec2(min.x, min.y);
		_localPhysicsBoundingBoxXYPlaneVertices[1] = new Vec2(min.x, max.y);
		_localPhysicsBoundingBoxXYPlaneVertices[2] = new Vec2(max.x, max.y);
		_localPhysicsBoundingBoxXYPlaneVertices[3] = new Vec2(max.x, min.y);
		_scaledLocalPhysicsBoundingBoxXYPlaneVertices[0] = _localPhysicsBoundingBoxXYPlaneVertices[0];
		_scaledLocalPhysicsBoundingBoxXYPlaneVertices[1] = _localPhysicsBoundingBoxXYPlaneVertices[1];
		_scaledLocalPhysicsBoundingBoxXYPlaneVertices[2] = _localPhysicsBoundingBoxXYPlaneVertices[2];
		_scaledLocalPhysicsBoundingBoxXYPlaneVertices[3] = _localPhysicsBoundingBoxXYPlaneVertices[3];
	}

	public Vec2[] CalculateBoundingXYGlobalPlaneFromLocal(in MatrixFrame shipFrame, float scale = 1f)
	{
		_physicsBoundingBoxXYPlaneVertices[0] = shipFrame.TransformToParent(_localPhysicsBoundingBoxXYPlaneVertices[0] * scale);
		_physicsBoundingBoxXYPlaneVertices[1] = shipFrame.TransformToParent(_localPhysicsBoundingBoxXYPlaneVertices[1] * scale);
		_physicsBoundingBoxXYPlaneVertices[2] = shipFrame.TransformToParent(_localPhysicsBoundingBoxXYPlaneVertices[2] * scale);
		_physicsBoundingBoxXYPlaneVertices[3] = shipFrame.TransformToParent(_localPhysicsBoundingBoxXYPlaneVertices[3] * scale);
		return _physicsBoundingBoxXYPlaneVertices;
	}

	public Vec2[] GetLocalPhysicsBoundingBoxXYPlaneVertices(float scale = 1f)
	{
		if (scale == 1f)
		{
			return _localPhysicsBoundingBoxXYPlaneVertices;
		}
		for (int i = 0; i < 4; i++)
		{
			_scaledLocalPhysicsBoundingBoxXYPlaneVertices[i] = _localPhysicsBoundingBoxXYPlaneVertices[i] * scale;
		}
		return _scaledLocalPhysicsBoundingBoxXYPlaneVertices;
	}

	public void SetSinkingState(NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState state)
	{
		switch (state)
		{
		case NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sinking:
		{
			for (int i = 0; i < _partialHitPoints.Length; i++)
			{
				_partialHitPoints[i] = 0f;
				_physics.SetTargetDurabilityOfPart(i, 0f);
			}
			base.GameEntity.AddBodyFlags(BodyFlags.Sinking);
			foreach (UsableMachine item in base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<UsableMachine>())
			{
				item.SetScriptComponentToTick(item.GetTickRequirement());
			}
			SetController(ShipControllerType.None);
			if (Team != null)
			{
				if (Team == Mission.Current.PlayerTeam || Team == Mission.Current.PlayerAllyTeam)
				{
					MBInformationManager.AddQuickInformation(PlayerSideShipSinkingText);
				}
				else if (Team == Mission.Current.PlayerEnemyTeam)
				{
					MBInformationManager.AddQuickInformation(EnemySideShipSinkingText);
				}
			}
			ClimbingMachineDetachment.Deactivate();
			break;
		}
		case NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sunk:
			SetDisabled(isParentObject: true);
			SoundManager.StartOneShotEvent("event:/mission/movement/vessel/ship_sink", base.GameEntity.GlobalPosition);
			break;
		}
		_physics.SetSinkingState(state);
	}

	public void SetAnchor(bool isAnchored, bool anchorInPlace = false, float forceMultiplier = 1f)
	{
		_physics.SetAnchor(isAnchored, anchorInPlace, forceMultiplier);
	}

	public void SetAnchorFrame(in Vec2 position, in Vec2 direction, float forceMultiplier = 1f)
	{
		_physics.SetAnchorFrame(in position, in direction, forceMultiplier);
	}

	public void DealCollisionDamage(MissionShip hitterShip, bool isRamDamage, Vec3 point, float damage)
	{
		int inflictedDamage;
		int modifiedDamage;
		DamageTypes damageType;
		bool isFatalDamage;
		float num = DealDamage(damage, hitterShip, out inflictedDamage, out modifiedDamage, out damageType, out isFatalDamage);
		bool flag = hitterShip?.IsPlayerShip ?? false;
		if (Agent.Main != null && Agent.Main.IsActive() && (flag || IsPlayerShip) && inflictedDamage > 0)
		{
			CombatLogData combatLog = new CombatLogData(isVictimAgentSameAsAttackerAgent: false, flag, flag, doesAttackerAgentHaveRiderAgent: false, isAttackerAgentRiderAgentMine: false, isAttackerAgentMount: false, IsPlayerShip, IsPlayerShip, isVictimAgentDead: false, doesVictimAgentHaveRiderAgent: false, isVictimAgentRiderAgentIsMine: false, isVictimAgentMount: false, this, isVictimRiderAgentSameAsAttackerAgent: false, crushedThrough: false, chamber: false, 0f);
			combatLog.InflictedDamage = inflictedDamage;
			combatLog.ModifiedDamage = modifiedDamage;
			combatLog.DamageType = damageType;
			combatLog.IsFatalDamage = isFatalDamage;
			combatLog.IsEntityToEntityCollisionDamage = true;
			if (isRamDamage)
			{
				combatLog.IsSpecialDamage = true;
			}
			Mission.Current.AddCombatLogSafe(null, null, combatLog);
		}
		_moraleInteractionLogic?.OnShipRammed(hitterShip, this);
		Vec3 vec = base.GameEntity.GetBodyWorldTransform().TransformToLocal(in point);
		int partIndexAtPosition = _physics.GetPartIndexAtPosition(vec);
		if (partIndexAtPosition < 0 || partIndexAtPosition >= _partialHitPoints.Length)
		{
			Debug.FailedAssert($"DealRammingDamage: GetPartIndexAtPosition for localPos {vec} returned {partIndexAtPosition}.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Objects\\MissionShip.cs", "DealCollisionDamage", 1252);
			return;
		}
		_partialHitPoints[partIndexAtPosition] = TaleWorlds.Library.MathF.Max(0f, _partialHitPoints[partIndexAtPosition] - num);
		_physics.SetTargetDurabilityOfPart(partIndexAtPosition, _partialHitPoints[partIndexAtPosition] / MaxPartialHealth);
	}

	public void ResetFormationPositioning()
	{
		GetWorldPositionOnDeck(out var worldPosition);
		Formation.SetPositioning(worldPosition, GlobalFrame.rotation.f.AsVec2.Normalized());
	}

	public float DealDamage(float rawDamage, MissionShip rammingShip, out int inflictedDamage, out int modifiedDamage, out DamageTypes damageType, out bool isFatalDamage)
	{
		float hitPoints = HitPoints;
		ShipOrigin.OnShipDamaged(rawDamage, rammingShip?.ShipOrigin, out var modifiedDamage2);
		modifiedDamage = (int)modifiedDamage2;
		float result = hitPoints - HitPoints;
		damageType = DamageTypes.Blunt;
		isFatalDamage = false;
		if (HitPoints <= 0f && _physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating)
		{
			SetSinkingState(NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sinking);
			_moraleInteractionLogic?.OnShipSunk(this);
			isFatalDamage = true;
		}
		if (HitPoints / ShipOrigin.MaxHitPoints <= 0.1f && hitPoints / ShipOrigin.MaxHitPoints > 0.1f)
		{
			ShipsLogic.OnShipLowHealth(this);
		}
		inflictedDamage = (int)rawDamage;
		return result;
	}

	public float DealDamageToSails(Agent attackerAgent, float rawDamage, float inflictedDamage, MissionSail sailHit)
	{
		float sailHitPoints = SailHitPoints;
		ShipOrigin.OnSailDamaged(rawDamage, inflictedDamage);
		float result = sailHitPoints - SailHitPoints;
		sailHit?.OnSailHit(attackerAgent, rawDamage, inflictedDamage);
		if (SailHitPoints <= 0f && ShipSailState == SailState.Intact)
		{
			foreach (MissionSail sail in Sails)
			{
				sail.StartFire();
			}
			SailBurnerAgent = attackerAgent;
			ShipSailState = SailState.Burning;
			if (!SailBurningSoundEvent.IsPlaying())
			{
				SailBurningSoundEvent.Play();
			}
		}
		return result;
	}

	public bool GetIsConnected()
	{
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment?.AttachmentTarget?.OwnerShip != null)
			{
				return true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment?.AttachmentSource?.OwnerShip != null)
			{
				return true;
			}
		}
		return false;
	}

	public bool GetIsConnectedToEnemyWithoutBridges()
	{
		bool result = false;
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (Team != null && attachmentMachine.CurrentAttachment?.AttachmentTarget?.OwnerShip?.Team != null && attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip.Team.IsEnemyOf(Team))
			{
				if (attachmentMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected || attachmentMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown)
				{
					return false;
				}
				result = true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (Team != null && attachmentPointMachine.CurrentAttachment?.AttachmentSource?.OwnerShip?.Team != null && attachmentPointMachine.CurrentAttachment.AttachmentTarget.OwnerShip.Team.IsEnemyOf(Team))
			{
				if (attachmentPointMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected || attachmentPointMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown)
				{
					return false;
				}
				result = true;
			}
		}
		return result;
	}

	public bool HasThrownOrActiveBridgeConnections()
	{
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment?.AttachmentTarget != null)
			{
				ShipAttachmentMachine.ShipAttachment.ShipAttachmentState state = attachmentMachine.CurrentAttachment.State;
				if (state == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown || state == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected)
				{
					return true;
				}
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment?.AttachmentSource != null)
			{
				ShipAttachmentMachine.ShipAttachment.ShipAttachmentState state2 = attachmentPointMachine.CurrentAttachment.State;
				if (state2 == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown || state2 == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HasMachine(UsableMachine usableMachine)
	{
		if (ShipControllerMachine == usableMachine)
		{
			return true;
		}
		if (_shipOarMachines != null && _shipOarMachines.Contains(usableMachine))
		{
			return true;
		}
		if (ShipSiegeWeapon == usableMachine)
		{
			return true;
		}
		if (_attachmentMachines != null && _attachmentMachines.Contains(usableMachine))
		{
			return true;
		}
		if (_attachmentPointMachines != null && _attachmentPointMachines.Contains(usableMachine))
		{
			return true;
		}
		if (_climbingMachines != null && _climbingMachines.Contains(usableMachine))
		{
			return true;
		}
		return false;
	}

	public bool IsShipInCriticalZoneBetween(MissionShip ship2, MBReadOnlyList<MissionShip> allShips)
	{
		Vec2[] criticalZoneVerticesBetween = GetCriticalZoneVerticesBetween(ship2);
		foreach (MissionShip allShip in allShips)
		{
			if (allShip != this && allShip != ship2)
			{
				Vec2[] array = allShip.CalculateBoundingXYGlobalPlaneFromLocal(allShip.GameEntity.GetBodyWorldTransform());
				if (MBMath.CheckPolygonIntersection(criticalZoneVerticesBetween, array))
				{
					return true;
				}
				if (MBMath.CheckPointInsidePolygon(in criticalZoneVerticesBetween[0], in criticalZoneVerticesBetween[1], in criticalZoneVerticesBetween[2], in criticalZoneVerticesBetween[3], in array[0]))
				{
					return true;
				}
			}
		}
		return false;
	}

	public Vec2[] GetCriticalZoneVerticesBetween(MissionShip otherShip)
	{
		float num = 6.35f;
		MatrixFrame shipFrame = base.GameEntity.GetBodyWorldTransform();
		MatrixFrame shipFrame2 = otherShip.GameEntity.GetBodyWorldTransform();
		Vec2[] array = CalculateBoundingXYGlobalPlaneFromLocal(in shipFrame);
		Vec2[] array2 = otherShip.CalculateBoundingXYGlobalPlaneFromLocal(in shipFrame2);
		Vec2 point = array[0];
		Vec2 point2 = array[3];
		Vec2 vec = array[0];
		Vec2 vec2 = array[1];
		Vec2 vec3 = array[3];
		Vec2 vec4 = array[2];
		Vec2 lineSegmentBegin = array2[0];
		Vec2 lineSegmentEnd = array2[1];
		Vec2 lineSegmentBegin2 = array2[3];
		Vec2 lineSegmentEnd2 = array2[2];
		float distanceSquareOfPointToLineSegment = MBMath.GetDistanceSquareOfPointToLineSegment(in lineSegmentBegin, in lineSegmentEnd, point);
		float distanceSquareOfPointToLineSegment2 = MBMath.GetDistanceSquareOfPointToLineSegment(in lineSegmentBegin, in lineSegmentEnd, point2);
		Vec2 vec5;
		Vec2 vec6;
		if (distanceSquareOfPointToLineSegment < distanceSquareOfPointToLineSegment2)
		{
			vec5 = vec;
			vec6 = vec2;
		}
		else
		{
			vec5 = vec3;
			vec6 = vec4;
		}
		distanceSquareOfPointToLineSegment2 = MBMath.GetDistanceSquareOfPointToLineSegment(in lineSegmentBegin2, in lineSegmentEnd2, point);
		Vec2 vec7;
		Vec2 vec8;
		if (distanceSquareOfPointToLineSegment < distanceSquareOfPointToLineSegment2)
		{
			vec7 = lineSegmentBegin;
			vec8 = lineSegmentEnd;
		}
		else
		{
			vec7 = lineSegmentBegin2;
			vec8 = lineSegmentEnd2;
		}
		Vec2 point3 = MBMath.ProjectPointOntoLine(vec7, vec5, vec6);
		Vec2 point4 = MBMath.ProjectPointOntoLine(vec8, vec5, vec6);
		Vec2 point5 = MBMath.ProjectPointOntoLine(vec5, vec7, vec8);
		Vec2 point6 = MBMath.ProjectPointOntoLine(vec6, vec7, vec8);
		Vec3 f = shipFrame.rotation.f;
		Vec3 f2 = shipFrame2.rotation.f;
		int num2 = ((!(Vec3.DotProduct(f, f2) < 0f)) ? 1 : (-1));
		point3 = MBMath.ClampToAxisAlignedRectangle(point3, vec5, vec6);
		point4 = MBMath.ClampToAxisAlignedRectangle(point4, vec5, vec6);
		point5 = MBMath.ClampToAxisAlignedRectangle(point5, vec7, vec8);
		point6 = MBMath.ClampToAxisAlignedRectangle(point6, vec7, vec8);
		Vec2 vec9 = (vec6 - vec5).Normalized();
		Vec2 vec10 = (vec8 - vec7).Normalized();
		point3 += num * vec9 * num2;
		point4 -= num * vec9 * num2;
		point5 += num * vec10 * num2;
		point6 -= num * vec10 * num2;
		if (num2 > 0)
		{
			_criticalZoneVertices[0] = point3;
			_criticalZoneVertices[1] = point5;
			_criticalZoneVertices[2] = point6;
			_criticalZoneVertices[3] = point4;
		}
		else
		{
			_criticalZoneVertices[0] = point5;
			_criticalZoneVertices[1] = point4;
			_criticalZoneVertices[2] = point3;
			_criticalZoneVertices[3] = point6;
		}
		return _criticalZoneVertices;
	}

	public bool GetIsConnectedToEnemy()
	{
		Team team = Team;
		bool flag2;
		if (team != null)
		{
			foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
			{
				ShipAttachmentMachine.ShipAttachment currentAttachment = attachmentMachine.CurrentAttachment;
				if (currentAttachment == null)
				{
					continue;
				}
				bool? flag = currentAttachment.AttachmentTarget?.OwnerShip.Team?.IsEnemyOf(team);
				flag2 = true;
				if (flag != flag2)
				{
					continue;
				}
				flag2 = true;
				goto IL_0123;
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
			{
				ShipAttachmentMachine.ShipAttachment currentAttachment2 = attachmentPointMachine.CurrentAttachment;
				if (currentAttachment2 == null || currentAttachment2.AttachmentSource.OwnerShip.Team?.IsEnemyOf(team) != true)
				{
					continue;
				}
				flag2 = true;
				goto IL_0123;
			}
		}
		return false;
		IL_0123:
		return flag2;
	}

	public bool GetIsConnectedToEnemy(out MissionShip connectedEnemyShip)
	{
		Team team = Team;
		bool flag2;
		if (team != null)
		{
			foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
			{
				ShipAttachmentMachine.ShipAttachment currentAttachment = attachmentMachine.CurrentAttachment;
				if (currentAttachment == null)
				{
					continue;
				}
				bool? flag = currentAttachment.AttachmentTarget?.OwnerShip.Team?.IsEnemyOf(team);
				flag2 = true;
				if (flag != flag2)
				{
					continue;
				}
				connectedEnemyShip = attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip;
				flag2 = true;
				goto IL_015b;
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
			{
				ShipAttachmentMachine.ShipAttachment currentAttachment2 = attachmentPointMachine.CurrentAttachment;
				if (currentAttachment2 == null || currentAttachment2.AttachmentSource.OwnerShip.Team?.IsEnemyOf(team) != true)
				{
					continue;
				}
				connectedEnemyShip = attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip;
				flag2 = true;
				goto IL_015b;
			}
		}
		connectedEnemyShip = null;
		return false;
		IL_015b:
		return flag2;
	}

	public bool GetIsConnectedToEnemyWithSide(out Vec2 direction)
	{
		direction = Vec2.Zero;
		bool flag = false;
		bool flag3;
		if (Team != null)
		{
			foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
			{
				ShipAttachmentMachine.ShipAttachment currentAttachment = attachmentMachine.CurrentAttachment;
				if (currentAttachment == null)
				{
					continue;
				}
				bool? flag2 = currentAttachment.AttachmentTarget?.OwnerShip.Team?.IsEnemyOf(Team);
				flag3 = true;
				if (flag2 != flag3)
				{
					continue;
				}
				flag = true;
				Vec2 asVec = base.GameEntity.GetGlobalFrame().TransformToLocal(attachmentMachine.GameEntity.GlobalPosition).AsVec2;
				if (direction.x * asVec.x >= 0f)
				{
					direction += asVec;
					continue;
				}
				direction = Vec2.Zero;
				flag3 = true;
				goto IL_0231;
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
			{
				ShipAttachmentMachine.ShipAttachment currentAttachment2 = attachmentPointMachine.CurrentAttachment;
				if (currentAttachment2 == null || currentAttachment2.AttachmentSource.OwnerShip.Team?.IsEnemyOf(Team) != true)
				{
					continue;
				}
				flag = true;
				Vec2 asVec = base.GameEntity.GetGlobalFrame().TransformToLocal(attachmentPointMachine.GameEntity.GlobalPosition).AsVec2;
				if (direction.x * asVec.x >= 0f)
				{
					direction += asVec;
					continue;
				}
				direction = Vec2.Zero;
				flag3 = true;
				goto IL_0231;
			}
			if (flag)
			{
				direction.Normalize();
			}
			return flag;
		}
		return false;
		IL_0231:
		return flag3;
	}

	public void OnShipObjectUpdated()
	{
		_actuators.OnShipObjectUpdated();
		InitializeNavalPhysics();
	}

	public MBReadOnlyList<MissionShip> GetConnectedShips()
	{
		_temporaryMissionShipContainer.Clear();
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment?.AttachmentTarget?.OwnerShip != null)
			{
				_temporaryMissionShipContainer.Add(attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip);
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment?.AttachmentSource?.OwnerShip != null)
			{
				_temporaryMissionShipContainer.Add(attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip);
			}
		}
		return _temporaryMissionShipContainer;
	}

	public int GetDynamicNavmeshIdStart()
	{
		return DynamicNavmeshIdStart;
	}

	public bool GetBridgeWithEnemyActive()
	{
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.IsShipAttachmentMachineBridgeWithEnemy())
			{
				return true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.IsShipAttachmentMachinePointBridgeWithEnemy())
			{
				return true;
			}
		}
		return false;
	}

	public bool GetIsAnyBridgeActive()
	{
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.IsShipAttachmentMachineBridged())
			{
				return true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.IsShipAttachmentPointBridged())
			{
				return true;
			}
		}
		return false;
	}

	public void GetWorldPositionOnDeck(out WorldPosition worldPosition)
	{
		if (_isCachedWorldPositionOnDeckDirty)
		{
			_cachedWorldPositionOnDeck = GlobalFrame.TransformToParent(RallyFrame).origin.ToWorldPosition().GetNavMeshVec3().ToWorldPosition();
			_isCachedWorldPositionOnDeckDirty = false;
		}
		worldPosition = _cachedWorldPositionOnDeck;
	}

	public NavalState GetNavalState(in NavalVec localOffset)
	{
		MatrixFrame globalFrame = GlobalFrame;
		Vec2 vec = globalFrame.rotation.s.AsVec2.Normalized();
		Vec2 vec2 = globalFrame.rotation.f.AsVec2.Normalized();
		Vec2 position = GlobalFrame.origin.AsVec2 + localOffset.DeltaPosition.x * vec + localOffset.DeltaPosition.y * vec2;
		Vec2 direction = vec2;
		direction.RotateCCW(localOffset.DeltaOrientation);
		float num = Vec2.DotProduct(_physics.LinearVelocity.AsVec2, vec2);
		num += localOffset.DeltaSpeed;
		return new NavalState(in position, in direction, num);
	}

	public FacingOrder GetFacingOrderToRallyPoint()
	{
		return FacingOrder.FacingOrderLookAtDirection(((!RallyFrame.IsZero) ? GlobalFrame.TransformToParent(RallyFrame) : GlobalFrame).rotation.f.AsVec2.Normalized());
	}

	public MovementOrder GetMovementOrderToRallyPoint()
	{
		MatrixFrame obj = ((!RallyFrame.IsZero) ? GlobalFrame.TransformToParent(RallyFrame) : GlobalFrame);
		return MovementOrder.MovementOrderMove(obj.origin.ToWorldPosition());
	}

	public void SetPositioningOrdersToRallyPoint(bool applyToPlayerFormation, bool playersOrder)
	{
		if (applyToPlayerFormation || Formation.PlayerOwner != Mission.Current.MainAgent || !Formation.HasPlayerControlledTroop)
		{
			MatrixFrame matrixFrame = ((!RallyFrame.IsZero) ? GlobalFrame.TransformToParent(RallyFrame) : GlobalFrame);
			WorldPosition position = matrixFrame.origin.ToWorldPosition();
			Vec2 direction = matrixFrame.rotation.f.AsVec2.Normalized();
			Formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
			Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
			Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(direction));
		}
		if (applyToPlayerFormation)
		{
			ShipOrder.JoinPlayerFormationToPlacementDetachment(playersOrder);
		}
	}

	public MBReadOnlyList<MissionShip> GetNavmeshConnectedShips()
	{
		_temporaryMissionShipContainer.Clear();
		ulong num = 0uL;
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment == null || !attachmentMachine.CurrentAttachment.IsNavmeshConnected)
			{
				continue;
			}
			MissionShip ownerShip = attachmentMachine.CurrentAttachment.AttachmentSource.OwnerShip;
			MissionShip ownerShip2 = attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip;
			if (ownerShip != this)
			{
				if ((num & ownerShip.ShipUniqueBitwiseID) == 0L)
				{
					_temporaryMissionShipContainer.Add(ownerShip);
					num |= ownerShip.ShipUniqueBitwiseID;
				}
			}
			else if ((num & ownerShip2.ShipUniqueBitwiseID) == 0L)
			{
				_temporaryMissionShipContainer.Add(ownerShip2);
				num |= ownerShip2.ShipUniqueBitwiseID;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment == null || !attachmentPointMachine.CurrentAttachment.IsNavmeshConnected)
			{
				continue;
			}
			MissionShip ownerShip3 = attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip;
			MissionShip ownerShip4 = attachmentPointMachine.CurrentAttachment.AttachmentTarget.OwnerShip;
			if (ownerShip3 != this)
			{
				if ((num & ownerShip3.ShipUniqueBitwiseID) == 0L)
				{
					_temporaryMissionShipContainer.Add(ownerShip3);
					num |= ownerShip3.ShipUniqueBitwiseID;
				}
			}
			else if ((num & ownerShip4.ShipUniqueBitwiseID) == 0L)
			{
				_temporaryMissionShipContainer.Add(ownerShip4);
				num |= ownerShip4.ShipUniqueBitwiseID;
			}
		}
		return _temporaryMissionShipContainer;
	}

	public int ComputeActiveShipAttachmentCount()
	{
		int num = 0;
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment?.AttachmentTarget != null)
			{
				num++;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null)
			{
				num++;
			}
		}
		return num;
	}

	public void UpdateSailBurningSoundPosition()
	{
		Vec3 v = Vec3.Zero;
		if (Sails.Count > 0)
		{
			foreach (MissionSail sail in Sails)
			{
				v += sail.CenterOfSailForceShipLocal;
			}
			v /= (float)Sails.Count;
		}
		else
		{
			v = base.GameEntity.CenterOfMass;
		}
		Vec3 position = base.GameEntity.GetGlobalFrame().TransformToParent(in v);
		SailBurningSoundEvent.SetPosition(position);
	}

	protected override void OnSaveAsPrefab()
	{
	}

	public MissionShip GetOutermostConnectedShipFromSide(bool rightSide, out bool effectiveSideOfOutermostShip, ulong aggregateShipUniqueID)
	{
		if ((aggregateShipUniqueID & ShipUniqueBitwiseID) != 0L)
		{
			effectiveSideOfOutermostShip = rightSide;
			return this;
		}
		aggregateShipUniqueID |= ShipUniqueBitwiseID;
		MatrixFrame globalFrame = GlobalFrame;
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if ((!rightSide ^ (globalFrame.TransformToLocal(attachmentMachine.GameEntity.GetGlobalFrame()).origin.x > 0f)) && attachmentMachine.CurrentAttachment?.AttachmentTarget != null)
			{
				MissionShip ownerShip = attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip;
				return ownerShip.GetOutermostConnectedShipFromSide((globalFrame.rotation.f.AsVec2.DotProduct(ownerShip.GlobalFrame.rotation.f.AsVec2) >= 0f) ? rightSide : (!rightSide), out effectiveSideOfOutermostShip, aggregateShipUniqueID);
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if ((!rightSide ^ (globalFrame.TransformToLocal(attachmentPointMachine.GameEntity.GetGlobalFrame()).origin.x > 0f)) && attachmentPointMachine.CurrentAttachment?.AttachmentSource != null)
			{
				MissionShip ownerShip2 = attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip;
				return ownerShip2.GetOutermostConnectedShipFromSide((globalFrame.rotation.f.AsVec2.DotProduct(ownerShip2.GlobalFrame.rotation.f.AsVec2) >= 0f) ? rightSide : (!rightSide), out effectiveSideOfOutermostShip, aggregateShipUniqueID);
			}
		}
		effectiveSideOfOutermostShip = rightSide;
		return this;
	}

	protected override void OnFixedTick(float fixedDt)
	{
		if (!_isRemoved)
		{
			ProcessDetanglingShips();
		}
	}

	protected override void OnInit()
	{
		InitializeLists(isForCheckingForProblems: false);
		base.GameEntity.SetHasCustomBoundingBoxValidationSystem(hasCustomBoundingBox: true);
		base.OnInit();
	}

	protected override void OnBoundingBoxValidate()
	{
		base.GameEntity.RelaxLocalBoundingBox(in _localBoundingBoxCached);
	}

	public bool GetIsAgentOnShip(Agent agent, bool bypassSteppedShipCheck = false)
	{
		if (!bypassSteppedShipCheck && (agent.IsInWater() || agent.GetComponent<AgentNavalComponent>()?.SteppedShip == null))
		{
			return false;
		}
		int currentNavigationFaceId = agent.GetCurrentNavigationFaceId();
		return IsAgentOnShipNavmesh(currentNavigationFaceId);
	}

	public bool GetNextCrewSpawnGlobalFrame(out MatrixFrame crewSpawnGlobalFrame)
	{
		if (_crewSpawnLocalFrames != null && !_crewSpawnLocalFrames.IsEmpty())
		{
			int nextCrewSpawnFrameIndex = _nextCrewSpawnFrameIndex;
			_nextCrewSpawnFrameIndex = (_nextCrewSpawnFrameIndex + 1) % _crewSpawnLocalFrames.Count;
			crewSpawnGlobalFrame = GlobalFrame.TransformToParent(_crewSpawnLocalFrames[nextCrewSpawnFrameIndex]);
			return true;
		}
		crewSpawnGlobalFrame = MatrixFrame.Identity;
		return false;
	}

	public MatrixFrame GetNextOuterInnerSpawnGlobalFrame()
	{
		int nextDeckSpawnFrameIndex = _nextDeckSpawnFrameIndex;
		_nextDeckSpawnFrameIndex = (_nextDeckSpawnFrameIndex + 1) % DeckFrameCount;
		return GlobalFrame.TransformToParent((nextDeckSpawnFrameIndex >= OuterDeckLocalFrames.Count) ? InnerDeckLocalFrames[nextDeckSpawnFrameIndex - OuterDeckLocalFrames.Count] : OuterDeckLocalFrames[nextDeckSpawnFrameIndex]);
	}

	public MatrixFrame GetMiddleInnerSpawnGlobalFrame()
	{
		return GlobalFrame.TransformToParent(InnerDeckLocalFrames[InnerDeckLocalFrames.Count / 2]);
	}

	public MatrixFrame GetCaptainSpawnGlobalFrame()
	{
		return GlobalFrame.TransformToParent(InnerDeckLocalFrames[InnerDeckLocalFrames.Count - 1]);
	}

	public NavalState GetNavalState()
	{
		MatrixFrame globalFrame = GlobalFrame;
		Vec2 direction = globalFrame.rotation.f.AsVec2.Normalized();
		float speed = Vec2.DotProduct(_physics.LinearVelocity.AsVec2, direction);
		return new NavalState(globalFrame.origin.AsVec2, in direction, speed);
	}

	public bool GetIsThereActiveBridgeTo(MissionShip targetShip)
	{
		foreach (ShipAttachmentMachine attachmentMachine in AttachmentMachines)
		{
			if (attachmentMachine.IsShipAttachmentMachineBridged() && attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip == targetShip)
			{
				return true;
			}
		}
		foreach (ShipAttachmentMachine attachmentMachine2 in targetShip.AttachmentMachines)
		{
			if (attachmentMachine2.IsShipAttachmentMachineBridged() && attachmentMachine2.CurrentAttachment.AttachmentTarget.OwnerShip == this)
			{
				return true;
			}
		}
		return false;
	}

	public MBReadOnlyList<MissionShip> GetFullyConnectedShips()
	{
		_temporaryMissionShipContainer.Clear();
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null && attachmentMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected && _temporaryMissionShipContainer.IndexOf(attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip) < 0)
			{
				_temporaryMissionShipContainer.Add(attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip);
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null && attachmentPointMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected && _temporaryMissionShipContainer.IndexOf(attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip) < 0)
			{
				_temporaryMissionShipContainer.Add(attachmentPointMachine.CurrentAttachment.AttachmentSource.OwnerShip);
			}
		}
		return _temporaryMissionShipContainer;
	}

	public void EnableBlockers()
	{
		base.GameEntity.Scene.SetAbilityOfFacesWithId(DynamicNavmeshIdStart + 49, isEnabled: true);
		base.GameEntity.Scene.SetBlockerDirectionForFacesWithId(DynamicNavmeshIdStart + 49, base.GameEntity.GetGlobalFrame().rotation.f.AsVec2.RotationInRadians);
	}

	public MissionSail CheckHitSails(Agent attackerAgent, Mission.Missile missile, in Vec3 missileOldPosition, in Vec3 missilePosition, in MissionWeapon missileWeapon)
	{
		bool flag = false;
		if (!base.IsDisabled && (flag || (Team != null && Team.IsEnemyOf(attackerAgent.Team))) && missileWeapon.CurrentUsageItem != null && missileWeapon.CurrentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.Burning))
		{
			foreach (MissionSail sail in Sails)
			{
				if (missile.AlreadyHitEntityToIgnore != sail.SailEntity && sail.GetVisualSailEnabled() && sail.IntersectLineSegmentWithSail(in missileOldPosition, in missilePosition))
				{
					return sail;
				}
			}
		}
		return null;
	}

	protected override void AttachDynamicNavmeshToEntity()
	{
		if (Mission.Current != null && NavMeshPrefabName.Length > 0)
		{
			DynamicNavmeshIdStart = Mission.Current.GetNextDynamicNavMeshIdStart();
			base.GameEntity.Scene.ImportNavigationMeshPrefab(NavMeshPrefabName, DynamicNavmeshIdStart);
			AttachDynamicNavmeshFromMachines(_attachmentMachines, _attachmentPointMachines);
			if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.NavalRaid)
			{
				string text = NavMeshPrefabName.Add("_deactivator_dnm");
				text = text.Remove(text.Length - 1);
				base.GameEntity.Scene.ImportNavigationMeshPrefab(text, DynamicNavmeshIdStart + 45);
				GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 49, isConnected: false, isBlocker: true, autoLocalize: false, finalizeBlockerConvexHullComputation: true);
				base.GameEntity.Scene.SetAbilityOfFacesWithId(DynamicNavmeshIdStart + 49, isEnabled: false);
			}
		}
	}

	protected override bool OnHit(Agent attackerAgent, int inflictedDamage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage, out float finalDamage, out float fireDamage, out float modifiedFireDamage)
	{
		reportDamage = false;
		finalDamage = 0f;
		fireDamage = -1f;
		modifiedFireDamage = -1f;
		bool flag = false;
		if (!Mission.Current.DisableDying && Mission.Current.Mode != MissionMode.Conversation && Mission.Current.Mode != MissionMode.CutScene && !base.IsDisabled && weapon.CurrentUsageItem != null && (flag || (Team != null && Team.IsEnemyOf(attackerAgent.Team))))
		{
			bool num = weapon.CurrentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.Burning);
			bool num2 = ShipsLogic.IsMissileFromShipSiegeEngine(affectorWeaponSlotOrMissileIndex);
			float missileVelocityLengthOnFirstSailHit = ShipsLogic.GetMissileVelocityLengthOnFirstSailHit(affectorWeaponSlotOrMissileIndex);
			bool flag2 = missileVelocityLengthOnFirstSailHit >= 0f;
			if (num2)
			{
				float baseDamage = inflictedDamage;
				inflictedDamage = MissionGameModels.Current.MissionSiegeEngineCalculationModel.CalculateDamage(attackerAgent, baseDamage);
				finalDamage = DealDamage(inflictedDamage, null, out var _, out var _, out var _, out var _);
				reportDamage = true;
			}
			if (num)
			{
				fireDamage = weapon.CurrentUsageItem.FireDamage;
				if (flag2 && Mission.Current.TryGetMissileVelocityFromMissileIndex(affectorWeaponSlotOrMissileIndex, out var velocity))
				{
					fireDamage *= velocity.Length / missileVelocityLengthOnFirstSailHit;
				}
				foreach (MissionSail sail in Sails)
				{
					if (sail.GetVisualSailEnabled())
					{
						float inflictedDamage3 = MissionGameModels.Current.AgentApplyDamageModel.CalculateSailFireDamage(attackerAgent, ShipOrigin, fireDamage, damageFromShipMachine: true);
						DealDamageToSails(attackerAgent, fireDamage, inflictedDamage3, null);
						break;
					}
				}
				if (FireHitPoints > 0f)
				{
					float num3 = DealFireDamage(fireDamage);
					modifiedFireDamage = num3 - fireDamage;
					if (num3 > 40f)
					{
						base.GameEntity.GetFirstScriptOfTypeRecursive<ShipBurningSystem>()?.RegisterBlow(impactPosition);
					}
					reportDamage = true;
					if (FireHitPoints <= 0f)
					{
						DealDamageToSails(attackerAgent, SailHitPoints, SailHitPoints, null);
						PrepareForAbandonment();
						base.GameEntity.GetFirstScriptOfTypeRecursive<ShipBurningSystem>()?.StartFire();
						ShipsLogic.OnShipBurned(this);
					}
				}
			}
		}
		ShipsLogic.OnShipHit(this, attackerAgent, (int)finalDamage, impactPosition, impactDirection, in weapon, affectorWeaponSlotOrMissileIndex);
		return true;
	}

	public float DealFireDamage(float fireDamage)
	{
		float num = MissionGameModels.Current.AgentApplyDamageModel.CalculateHullFireDamage(fireDamage, ShipOrigin);
		FireHitPoints -= num;
		return num;
	}

	protected override void OnTick(float dt)
	{
		_isCachedWorldPositionOnDeckDirty = true;
		if (!_isRemoved)
		{
			if (Mission.Current.IsDeploymentFinished)
			{
				if (_autoUpdateController)
				{
					UpdateController();
				}
				if (IsShipOrderActive)
				{
					ShipOrder.Tick();
				}
				else if (IsClimbingMachineStandAloneTickingActive)
				{
					ShipOrder.TickClimbingMachines();
				}
			}
			if (HasController)
			{
				_inputRecord = Controller.Update(dt);
			}
			if (HasCustomSailSetting)
			{
				_inputRecord.SetSail(_customSailSetting);
			}
			if (_inputRecord.Sail != SailInput.Raised && _foldSailsOnBridgeConnection && HasThrownOrActiveBridgeConnections())
			{
				_inputRecord.SetSail(SailInput.Raised);
			}
			HandleCapsizing();
			float num = TaleWorlds.Library.MathF.Max(_physics.PhysicsBoundingBoxWithoutChildren.max.z, _physics.PhysicsBoundingBoxSizeWithoutChildren.y * 0.5f);
			Vec3 globalPosition = base.GameEntity.GlobalPosition;
			if (Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sinking && globalPosition.z + num < Mission.Current.Scene.GetWaterLevelAtPosition(globalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false))
			{
				SetSinkingState(NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sunk);
				ShipSailState = SailState.Destroyed;
				if (SailBurningSoundEvent.IsPlaying())
				{
					SailBurningSoundEvent.Stop();
				}
				ShipsLogic.OnShipSunk(this);
			}
			bool flag = IsShipUpsideDown();
			if (flag != IsShipNavmeshDisabled)
			{
				SetAbilityOfShipNavmeshFaces(!flag);
				IsShipNavmeshDisabled = flag;
				ShipOrder.ManageShipDetachments();
			}
			UpdateSailBurningSoundPosition();
			if (ShipSailState == SailState.Burning)
			{
				bool flag2 = true;
				foreach (MissionSail sail in Sails)
				{
					if (!sail.IsBurningFinished())
					{
						flag2 = false;
					}
				}
				if (flag2)
				{
					ShipSailState = SailState.Destroyed;
					if (SailBurningSoundEvent.IsPlaying())
					{
						SailBurningSoundEvent.Stop();
					}
					ShipsLogic.OnSailsDead(this);
				}
			}
			if (FireHitPoints <= 0f && BurntHullDamageTotal < MaxHealth * 0.5f && !IsSinking && Mission.Current.CurrentTime > _nextPermanentBurnDamageTime && !Mission.Current.DisableDying && Mission.Current.Mode != MissionMode.Conversation && Mission.Current.Mode != MissionMode.CutScene)
			{
				BurntHullDamageTotal += DealDamage(MaxHealth * 0.03f, null, out var _, out var _, out var _, out var _);
				_nextPermanentBurnDamageTime = Mission.Current.CurrentTime + 1f;
			}
			if (FireHitPoints > 0f && FireHitPoints < MaxFireHealth && Mission.Current.CurrentTime > _nextFireHitPointRestoreTime)
			{
				FireHitPoints += MaxFireHealth * 0.005f;
				if (FireHitPoints > MaxFireHealth)
				{
					FireHitPoints = MaxFireHealth;
				}
				_nextFireHitPointRestoreTime = Mission.Current.CurrentTime + 1f;
			}
			if (IsDisconnectionBlocked())
			{
				bool flag3 = false;
				foreach (ShipAttachmentPointMachine attachmentPointMachine in AttachmentPointMachines)
				{
					if (attachmentPointMachine.IsShipAttachmentPointConnectedToEnemy())
					{
						flag3 = true;
						break;
					}
				}
				if (!flag3)
				{
					ResetDisconnectionBlock();
				}
			}
			HandleQueuedShipCollisions();
		}
		_actuators.Update(dt);
		if (_localBoundingBoxCacheInvalid)
		{
			ComputeStaticLocalBoundingBox();
			base.GameEntity.RecomputeBoundingBox();
			_localBoundingBoxCacheInvalid = false;
		}
	}

	public void PrepareForAbandonment()
	{
		BeingAbandoned = true;
		foreach (UsableMachine item in base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<UsableMachine>())
		{
			if (!(item is ShipAttachmentPointMachine))
			{
				item.SetIsDisabledForAI(isDisabledForAI: true);
				item.SetScriptComponentToTick(item.GetTickRequirement());
			}
			foreach (StandingPoint standingPoint in item.StandingPoints)
			{
				standingPoint.SetIsDisabledForPlayersSynched(value: true);
			}
		}
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item2 in children)
		{
			if (item2.BodyFlag.HasAnyFlag(BodyFlags.Barrier) || item2.BodyFlag.HasAnyFlag(BodyFlags.Barrier3D) || item2.BodyFlag.HasAnyFlag(BodyFlags.AILimiter))
			{
				item2.SetVisibilityExcludeParents(visible: false);
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in AttachmentPointMachines)
		{
			attachmentPointMachine.SetEnemyRangeToStopUsing(0f);
			attachmentPointMachine.SetIsDisabledForAI(isDisabledForAI: false);
			foreach (StandingPoint standingPoint2 in attachmentPointMachine.StandingPoints)
			{
				if (standingPoint2 == attachmentPointMachine.PilotStandingPoint)
				{
					standingPoint2.LockUserFrames = true;
				}
			}
			foreach (GameEntity rampPhysics in attachmentPointMachine.RampPhysicsList)
			{
				rampPhysics.SetVisibilityExcludeParents(visible: true);
			}
		}
		ShipOrder.StopUsingMachines(formationLeaving: false);
		IsShipOrderActive = false;
		SetController(ShipControllerType.None);
		ShipsLogic.OnShipPreparedForAbandonment(this);
	}

	protected override void OnParallelFixedTick(float fixedDt)
	{
		ShouldUpdateSoundPos = SoundManager.GetListenerFrame().origin.DistanceSquared(base.GameEntity.GetBodyWorldTransform().origin) < 10000f;
		ShipActuatorRecord actuatorInput = _inputProcessor.OnParallelFixedTick(fixedDt, in _inputRecord);
		ShipForceRecord record = _actuators.OnParallelFixedTick(fixedDt, in actuatorInput);
		_physics.SetShipForceRecord(in record);
	}

	protected override void OnPhysicsCollision(ref PhysicsContact contactPairList, WeakGameEntity entity0, WeakGameEntity entity1)
	{
		SoundEvent soundEvent = null;
		Vec3 vec = new Vec3(0f, 0f, 0f, -1f);
		Vec3 vec2 = new Vec3(0f, 0f, 0f, -1f);
		int num = 0;
		int num2 = -1;
		bool flag = false;
		StackArray.StackArray3Int stackArray3Int = default(StackArray.StackArray3Int);
		Vec3 zero = Vec3.Zero;
		for (int i = 0; i < contactPairList.NumberOfContactPairs; i++)
		{
			stackArray3Int[(int)contactPairList[i].ContactEventType]++;
			for (int j = 0; j < contactPairList[i].NumberOfContacts; j++)
			{
				num++;
				vec += contactPairList[i][j].Position;
				vec2 += contactPairList[i][j].Normal;
				zero += contactPairList[i][j].Impulse;
			}
		}
		int num3 = -1;
		for (int num4 = _currentCollisionStatesToShips.Count - 1; num4 >= 0; num4--)
		{
			if (_currentCollisionStatesToShips[num4].CollidingEntity != null && _currentCollisionStatesToShips[num4].CollidingEntity.Scene == null)
			{
				_currentCollisionStatesToShips.RemoveAt(num4);
				if (num3 >= 0)
				{
					num3--;
				}
			}
			else if ((_currentCollisionStatesToShips[num4].CollidingEntity != null && entity1 != null && _currentCollisionStatesToShips[num4].CollidingEntity.Root == entity1.Root) || (_currentCollisionStatesToShips[num4].CollidingEntity == null && entity1 == null && _currentCollisionStatesToShips[num4].CollidingBodyPtr == contactPairList.body1))
			{
				num3 = num4;
			}
		}
		MissionShip missionShip = ((entity1 != null) ? (entity1.GetFirstScriptWithNameHash(MissionShipScriptNameHash) as MissionShip) : null);
		PhysicsEventType physicsEventType = PhysicsEventType.CollisionEnd;
		if (contactPairList.NumberOfContactPairs > 0)
		{
			if (num3 >= 0)
			{
				physicsEventType = _currentCollisionStatesToShips[num3].CurrentCollisionState;
			}
			switch (physicsEventType)
			{
			case PhysicsEventType.CollisionStart:
				if (stackArray3Int[1] > 0)
				{
					physicsEventType = PhysicsEventType.CollisionStay;
				}
				else if (stackArray3Int[0] == 0 && stackArray3Int[2] > 0)
				{
					physicsEventType = PhysicsEventType.CollisionEnd;
				}
				break;
			case PhysicsEventType.CollisionStay:
				if (stackArray3Int[0] == 0 && stackArray3Int[1] == 0)
				{
					physicsEventType = PhysicsEventType.CollisionEnd;
				}
				break;
			case PhysicsEventType.CollisionEnd:
				if (stackArray3Int[0] > 0 || stackArray3Int[1] > 0)
				{
					physicsEventType = PhysicsEventType.CollisionStart;
				}
				break;
			}
			if (num3 >= 0)
			{
				if (physicsEventType == PhysicsEventType.CollisionEnd)
				{
					_currentCollisionStatesToShips.RemoveAt(num3);
					num3 = -1;
				}
				else
				{
					_currentCollisionStatesToShips[num3].UpdateCurrentCollisionState(physicsEventType);
				}
			}
			else if (physicsEventType != PhysicsEventType.CollisionEnd)
			{
				if (entity1 != null)
				{
					_currentCollisionStatesToShips.Add(new ShipToEntityCollisionStatus(entity1.Root, physicsEventType));
				}
				else
				{
					_currentCollisionStatesToShips.Add(new ShipToEntityCollisionStatus(contactPairList.body1, physicsEventType));
				}
			}
			flag = physicsEventType != PhysicsEventType.CollisionEnd && missionShip != null;
		}
		vec /= (float)num;
		vec2 /= (float)num;
		if (num3 != -1 && missionShip != null)
		{
			switch (_currentCollisionStatesToShips[num3].CurrentCollisionState)
			{
			case PhysicsEventType.CollisionStay:
				AddDetanglingShip(missionShip, vec);
				break;
			case PhysicsEventType.CollisionEnd:
				RemoveDetanglingShip(missionShip);
				break;
			}
		}
		if (missionShip != null)
		{
			for (int k = 0; k < _scrapeSoundEvents.Count; k++)
			{
				if (_scrapeSoundEvents[k].Item1 == missionShip.Index)
				{
					soundEvent = _scrapeSoundEvents[k].Item2;
					num2 = k;
					break;
				}
			}
			if (flag)
			{
				if (soundEvent == null)
				{
					soundEvent = SoundEvent.CreateEvent(_scrapeSoundEventID, base.GameEntity.Scene);
					_scrapeSoundEvents.Add((missionShip.Index, soundEvent));
					missionShip._scrapeSoundEvents.Add((Index, soundEvent));
				}
				if (!soundEvent.IsPlaying())
				{
					soundEvent.Play();
				}
				Vec3 v = vec2.CrossProductWithUp();
				float num5 = Vec3.DotProduct(v, Physics.LinearVelocity);
				float num6 = Vec3.DotProduct(v, missionShip.Physics.LinearVelocity);
				float value = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Abs(num5 - num6) / 10f, 1f);
				soundEvent.SetParameter("ForceContinuous", value);
				soundEvent.SetPosition(vec);
				if (!IsPlayerControlled && !missionShip.IsPlayerControlled)
				{
					soundEvent.SetParameter("VibrationSend", 0f);
				}
			}
			else
			{
				if (soundEvent != null && soundEvent.IsPlaying())
				{
					soundEvent.Stop();
					soundEvent = null;
				}
				if (num2 != -1 && num2 < _scrapeSoundEvents.Count)
				{
					_scrapeSoundEvents.RemoveAt(num2);
				}
			}
		}
		if (num <= 0 || ShipsLogic == null)
		{
			return;
		}
		bool flag2 = num3 < 0 && physicsEventType == PhysicsEventType.CollisionStart;
		if (flag2 && missionShip != null && CanDealDamage(missionShip))
		{
			MatrixFrame bodyWorldTransform = base.GameEntity.GetBodyWorldTransform();
			MatrixFrame bodyWorldTransform2 = missionShip.GameEntity.GetBodyWorldTransform();
			BoundingBox physicsBoundingBoxWithoutChildren = Physics.PhysicsBoundingBoxWithoutChildren;
			Vec3 vec3 = bodyWorldTransform.TransformToParent(in physicsBoundingBoxWithoutChildren.center);
			Vec3 v2 = vec - vec3;
			v2.z = 0f;
			v2.Normalize();
			float num7 = Vec3.DotProduct(v2, bodyWorldTransform.rotation.f);
			if (num7 > 0f && TaleWorlds.Library.MathF.Acos(num7) * (180f / System.MathF.PI) < MissionShipObject.BowAngleLimitFromCenterline)
			{
				Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = base.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(vec);
				Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody2 = missionShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(vec);
				_ = linearVelocityAtGlobalPointForEntityWithDynamicBody - linearVelocityAtGlobalPointForEntityWithDynamicBody2;
				Vec3 vec4 = bodyWorldTransform.origin + Vec3.DotProduct(vec - bodyWorldTransform.origin, bodyWorldTransform.rotation.f) * bodyWorldTransform.rotation.f;
				float num8 = Vec3.DotProduct(v2: (bodyWorldTransform2.origin + Vec3.DotProduct(vec - bodyWorldTransform2.origin, bodyWorldTransform2.rotation.f) * bodyWorldTransform2.rotation.f - vec4).NormalizedCopy(), v1: linearVelocityAtGlobalPointForEntityWithDynamicBody - linearVelocityAtGlobalPointForEntityWithDynamicBody2);
				if (num8 >= 3f)
				{
					float num9 = 12f * (float)Math.Sqrt(Physics.Mass / 500f) * 0.8f * num8;
					missionShip.QueueShipCollision(this, vec, num9);
					QueueShipCollision(missionShip, vec, num9 * 0.2f);
					UpdateDamageCooldown(missionShip);
				}
			}
		}
		ShipsLogic.OnShipCollision(this, entity1, contactPairList.body1Flags, vec, zero, flag2);
	}

	private void HandleQueuedShipCollisions()
	{
		ShipCollisionData result;
		while (_shipCollisionData.TryDequeue(out result))
		{
			DealCollisionDamage(result.CollidingShip, isRamDamage: false, result.ContactPosAvg, result.Damage);
		}
	}

	private void QueueShipCollision(MissionShip collidingShip, Vec3 contactPosAvg, float damage)
	{
		_shipCollisionData.Enqueue(new ShipCollisionData(collidingShip, contactPosAvg, damage));
	}

	public bool CanDealDamage(MissionShip targetShip)
	{
		float currentTime = Mission.Current.CurrentTime;
		if (_shipDamageCooldowns.TryGetValue(targetShip, out var value))
		{
			return currentTime - value >= 2f;
		}
		return true;
	}

	public void UpdateDamageCooldown(MissionShip targetShip)
	{
		float currentTime = Mission.Current.CurrentTime;
		_shipDamageCooldowns[targetShip] = currentTime;
	}

	protected override bool OnCheckForProblems()
	{
		InitializeLists(isForCheckingForProblems: true);
		return false;
	}

	private void ProcessCulledAttachmentMachineList<AttachmentMachineType>(MBList<AttachmentMachineType> attachmentList, MBList<AttachmentMachineType> culledList, List<WeakGameEntity> battlementShieldEntityList)
	{
		foreach (AttachmentMachineType attachment in attachmentList)
		{
			bool flag = false;
			Vec3 vec = Vec3.Invalid;
			if (attachment is ShipAttachmentMachine { GameEntity: var gameEntity })
			{
				vec = gameEntity.GetGlobalFrame().origin;
			}
			else if (attachment is ShipAttachmentPointMachine { GameEntity: var gameEntity2 })
			{
				vec = gameEntity2.GetGlobalFrame().origin;
			}
			foreach (WeakGameEntity battlementShieldEntity in battlementShieldEntityList)
			{
				Vec3 origin = battlementShieldEntity.GetGlobalFrame().origin;
				if (vec.DistanceSquared(origin) <= 6.25f)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				culledList.Add(attachment);
			}
		}
	}

	private void DisableAttachmentMachinesOverlappingWithBattlementShields()
	{
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenWithTagRecursive(list, "aft");
		MBList<ShipAttachmentMachine> mBList = new MBList<ShipAttachmentMachine>();
		MBList<ShipAttachmentPointMachine> mBList2 = new MBList<ShipAttachmentPointMachine>();
		ProcessCulledAttachmentMachineList(_attachmentMachines, mBList, list);
		ProcessCulledAttachmentMachineList(_attachmentPointMachines, mBList2, list);
		foreach (ShipAttachmentMachine item in mBList)
		{
			item.Disable();
			item.GameEntity.SetVisibilityExcludeParents(visible: false);
			_attachmentMachines.Remove(item);
		}
		foreach (ShipAttachmentPointMachine item2 in mBList2)
		{
			item2.Disable();
			item2.GameEntity.SetVisibilityExcludeParents(visible: false);
			_attachmentPointMachines.Remove(item2);
		}
	}

	internal void InitForMission(int shipIndex, ulong shipUniqueBitwiseID, ShipAssignment shipAssignment, NavalShipsLogic shipsLogic)
	{
		if (!IsInitialized)
		{
			ShipsLogic = shipsLogic;
			ValidateShipAndDescendantEntitiesAndBoundingBoxes();
			Index = shipIndex;
			MaxShipIndex = TaleWorlds.Library.MathF.Max(Index, MaxShipIndex);
			_shields = base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<ShipShieldComponent>();
			DisableAttachmentMachinesOverlappingWithBattlementShields();
			base.GameEntity.Scene.SetFixedTickCallbackActive(isActive: true);
			base.GameEntity.Scene.SetOnCollisionFilterCallbackActive(isActive: true);
			_missionShipObject = shipAssignment.MissionShipObject;
			ShipOrigin = shipAssignment.ShipOrigin;
			ShipSailState = ((!(ShipOrigin.SailHitPoints > 250f)) ? SailState.Destroyed : SailState.Intact);
			FireHitPoints = ShipOrigin.MaxFireHitPoints;
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.CollectChildrenEntitiesWithTag("rally_point")[0]);
			RallyFrame = GlobalFrame.TransformToLocal(gameEntity.GetGlobalFrame());
			LoadSpawnPoints();
			LoadShipBanners();
			_capsizeDamageTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
			ClimbingMachineDetachment = new ClimbingMachineDetachment(base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<ClimbingMachine>());
			Team team = Mission.GetTeam(shipAssignment.TeamSide);
			Formation = team.GetFormation(shipAssignment.FormationIndex);
			_inputRecord = ShipInputRecord.None();
			Formation.OnUnitRemoved += OnFormationUnitRemoved;
			SetController(ShipControllerType.AI);
			_inputProcessor = new ShipInputProcessor(this);
			_actuators = new NavalDLC.Missions.ShipActuators.ShipActuators(this);
			foreach (MissionSail sail in _actuators.Sails)
			{
				sail.ForceFold();
			}
			InitializeNavalPhysics();
			_visitedMissionShips = new HashSet<MissionShip>();
			InitializeDetanglingShipInformation();
			InitializeLocalPhysicsBoundingXYPlane();
			_physicsBoundingBoxXYPlaneVertices = new Vec2[4];
			_criticalZoneVertices = new Vec2[4];
			float element = MaxPartialHealth - (MaxHealth - HitPoints) / 6f;
			_partialHitPoints = Enumerable.Repeat(element, 6).ToArray();
			InitializePartialDurabilities();
			_moraleInteractionLogic = Mission.Current.GetMissionBehavior<NavalAgentMoraleInteractionLogic>();
			ShipsLogic.ShipSpawnedEvent += OnShipSpawned;
			ShipsLogic.ShipTransferredToFormationEvent += OnShipTransferred;
			ShipsLogic.ShipRemovedEvent += OnShipRemoved;
			ShipOrder = new ShipOrder(this, Formation);
			ResetFormationPositioning();
			_scrapeSoundEvents = new MBList<(int, SoundEvent)>();
			int eventIdFromString = SoundEvent.GetEventIdFromString("event:/mission/ambient/detail/fire/fire_big");
			SailBurningSoundEvent = SoundEvent.CreateEvent(eventIdFromString, Mission.Current.Scene);
			UpdateSailBurningSoundPosition();
			ShipSiegeWeapon?.SetForcedUse(value: false);
			InitializeShipBoundingBox();
			_shipEventListeners = new MBList<IShipEventListener>();
			foreach (ScriptComponentBehavior item2 in base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<ScriptComponentBehavior>())
			{
				if (item2 is IShipEventListener item)
				{
					_shipEventListeners.Add(item);
				}
			}
			ShipUniqueBitwiseID = shipUniqueBitwiseID;
			ShipIslandCombinedID = ShipUniqueBitwiseID;
			Formation.OnUnitAttached += OnUnitAttached;
			if (!base.GameEntity.IsInEditorScene())
			{
				ClearFloaterVolumes();
				WeakGameEntity firstChildEntityWithName = base.GameEntity.GetFirstChildEntityWithName("knobs_holder");
				if (firstChildEntityWithName.IsValid)
				{
					firstChildEntityWithName.SetEntityFlags(firstChildEntityWithName.EntityFlags | EntityFlags.DoNotTick);
				}
				WeakGameEntity firstChildEntityWithName2 = base.GameEntity.GetFirstChildEntityWithName("brazier_holder");
				if (firstChildEntityWithName2.IsValid)
				{
					firstChildEntityWithName2.SetEntityFlags(firstChildEntityWithName2.EntityFlags | EntityFlags.DoNotTick);
				}
				List<WeakGameEntity> children = new List<WeakGameEntity>();
				base.GameEntity.GetChildrenRecursive(ref children);
				foreach (WeakGameEntity item3 in children)
				{
					item3.SetForceDecalsToRender(value: true);
					item3.SetForceNotAffectedBySeason(value: true);
				}
			}
			_anyActiveFormationTroopOnShip.Expire();
		}
		else
		{
			Debug.FailedAssert("The ship is already initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Objects\\MissionShip.cs", "InitForMission", 3240);
		}
	}

	private void OnFormationUnitRemoved(Formation formation, Agent agent)
	{
		if (BattleSide != Mission.Current.PlayerTeam.Side && formation.CountOfUnits == 0)
		{
			ShipControllerMachine.PilotStandingPoint.SetUsableByPlayerOnly();
		}
	}

	private void InitializeNavalPhysics()
	{
		ShipPhysicsReference physicsReference = _missionShipObject.PhysicsReference;
		NavalDLC.Missions.NavalPhysics.NavalPhysics.NavalPhysicsParameters physicsParameters = new NavalDLC.Missions.NavalPhysics.NavalPhysics.NavalPhysicsParameters
		{
			OverrideMass = _missionShipObject.Mass,
			MassMultiplier = 1f + ((ShipOrigin != null) ? ShipOrigin.ShipWeightFactor : 0f),
			MomentOfInertiaMultiplier = _missionShipObject.MomentOfInertiaMultiplier,
			FloatingForceMultiplier = ((ShipOrigin != null) ? ShipOrigin.Hull.FloatingForceMultiplier : 1f),
			MaximumSubmergedVolumeRatio = _missionShipObject.MaximumSubmergedVolumeRatio,
			ForwardDragMultiplier = 1f + ((ShipOrigin != null) ? ShipOrigin.ForwardDragFactor : 0f),
			LinearFrictionMultiplier = _missionShipObject.LinearFrictionMultiplier,
			AngularFrictionMultiplier = _missionShipObject.AngularFrictionMultiplier,
			TorqueMultiplierOfLateralBuoyantForces = _missionShipObject.TorqueMultiplierOfLateralBuoyantForces,
			TorqueMultiplierOfVerticalBuoyantForces = _missionShipObject.TorqueMultiplierOfVerticalBuoyantForces,
			UpSideDownFrictionMultiplier = 3f,
			MaxLinearSpeedForLateralDragCenterShift = _missionShipObject.MaxLinearSpeed,
			MaxLateralDragShift = _missionShipObject.MaxLateralDragShift,
			LateralDragShiftCriticalAngle = _missionShipObject.LateralDragShiftCriticalAngle,
			StepAgentWeightMultiplier = 2f,
			MakeAgentsStepToEntityEvenUnderWater = true
		};
		_physics = base.GameEntity.GetFirstScriptOfType<NavalDLC.Missions.NavalPhysics.NavalPhysics>();
		_physics.Initialize(physicsParameters, physicsReference);
	}

	internal void OnShipSpawned(MissionShip spawnedShip)
	{
		foreach (IShipEventListener shipEventListener in _shipEventListeners)
		{
			shipEventListener.OnShipSpawned(spawnedShip);
		}
	}

	internal void OnShipRemoved(MissionShip removedShip)
	{
		foreach (ShipAttachmentMachine attachmentMachine in AttachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null)
			{
				attachmentMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
				attachmentMachine.CurrentAttachment.Destroy();
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in AttachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null)
			{
				attachmentPointMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
				attachmentPointMachine.CurrentAttachment.Destroy();
			}
		}
		foreach (IShipEventListener shipEventListener in _shipEventListeners)
		{
			shipEventListener.OnShipRemoved(removedShip);
		}
		if (IsAIControlled)
		{
			AIController.RemoveShipFromCollisionIgnoreList(removedShip);
		}
		_actuators.OnShipRemoved(removedShip);
	}

	protected override void OnTickParallel(float dt)
	{
		_actuators.OnTickParallel(dt);
		NavalDLC.Missions.NavalPhysics.NavalPhysics physics = _physics;
		Formation formation = Formation;
		physics.SetContinuousDriftSpeed((formation != null && formation.CountOfUnits > 0) ? 0f : 1f);
	}

	private void ClearFloaterVolumes()
	{
		WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (child.Name == "floater_volume_holder")
			{
				weakGameEntity = child;
				break;
			}
		}
		if (weakGameEntity.IsValid)
		{
			weakGameEntity.RemoveAllChildren();
		}
	}

	internal void SetRemoved(bool value)
	{
		_isRemoved = value;
	}

	internal void OnShipTransferred(MissionShip ship, Formation oldFormation)
	{
		foreach (IShipEventListener shipEventListener in _shipEventListeners)
		{
			shipEventListener.OnShipTransferred(ship, oldFormation);
		}
	}

	public IDWAAgentDelegate CreateDWAAgent(in DWASimulatorParameters parameters)
	{
		if (_dwaAgentDelegate == null)
		{
			_dwaAgentDelegate = new ShipDWAAgentDelegate(this, in parameters);
		}
		else
		{
			((IDWAAgentDelegate)_dwaAgentDelegate).SetParameters(in parameters);
		}
		return _dwaAgentDelegate;
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		ShipsLogic.ShipSpawnedEvent -= OnShipSpawned;
		ShipsLogic.ShipRemovedEvent -= OnShipRemoved;
		ShipsLogic.ShipTransferredToFormationEvent -= OnShipTransferred;
		ShipOrder.OnOwnerShipRemoved();
		base.GameEntity.GetFirstScriptOfTypeRecursive<ShipWaterEffects>()?.DeregisterWaterMeshMaterials();
	}

	public void MoveShipToTheTargetWithDirection(MatrixFrame currentFrame, Vec2 targetPosition, Vec2 targetDirection, float maxAcceleration, float maxAngularAcceleration, float fixedDt)
	{
		float num = TaleWorlds.Library.MathF.Atan2(targetDirection.y, targetDirection.x);
		Vec3 origin = currentFrame.origin;
		Vec3 linearVelocity = Physics.LinearVelocity;
		Vec3 angularVelocity = Physics.AngularVelocity;
		float mass = Physics.Mass;
		float num2 = TaleWorlds.Library.MathF.Atan2(currentFrame.rotation.f.y, currentFrame.rotation.f.x);
		Vec2 vec = (targetPosition - origin.AsVec2) / fixedDt;
		float num3 = MBMath.WrapAngle(num - num2) / fixedDt;
		Vec2 vec2 = (vec - linearVelocity.AsVec2) / fixedDt;
		vec2.ClampMagnitude(0f, maxAcceleration);
		float num4 = (num3 - angularVelocity.z) / fixedDt;
		float num5 = TaleWorlds.Library.MathF.Sign(num4);
		float num6 = TaleWorlds.Library.MathF.Clamp(num4 * num5, 0f, maxAngularAcceleration);
		num4 = num5 * num6;
		Vec2 vec3 = vec2 * mass;
		Physics.ApplyForceToDynamicBody(vec3.ToVec3());
		Physics.ApplyTorque(new Vec3(0f, 0f, num4), GameEntityPhysicsExtensions.ForceMode.Acceleration);
	}

	internal void UpdateController()
	{
		if (IsSinking)
		{
			return;
		}
		if (ShipControllerMachine.PilotAgent != null && ShipControllerMachine.PilotAgent.IsPlayerControlled)
		{
			if (!IsPlayerControlled)
			{
				if (Formation != null && Formation.IsAIControlled)
				{
					Formation.SetControlledByAI(isControlledByAI: false);
				}
				SetController(ShipControllerType.Player);
				PlayerController.SetInput(in _inputRecord);
			}
			else if (Formation != null && Formation.IsAIControlled)
			{
				ShipControllerMachine.PilotAgent.StopUsingGameObject();
				SetController(ShipControllerType.AI);
			}
		}
		else if (IsPlayerShip)
		{
			if (Formation == null)
			{
				return;
			}
			if (Formation.IsAIControlled && !HasController)
			{
				SetController(ShipControllerType.AI);
			}
			else if (!Formation.IsAIControlled)
			{
				if (IsAIControlled)
				{
					ShipOrder.SetShipStopOrder();
					SetController(ShipControllerType.None);
					SetInputRecord(ShipInputRecord.Stop());
				}
				else if (HasController)
				{
					SetController(ShipControllerType.None);
				}
			}
		}
		else if (!IsAIControlled)
		{
			SetController(ShipControllerType.AI);
		}
	}

	private void HandleCapsizing()
	{
		bool flag = Vec3.DotProduct(base.GameEntity.GetLocalFrame().rotation.u, Vec3.Up) < -0.5f;
		if (_isCapsized != flag)
		{
			_isCapsized = flag;
			if (flag)
			{
				_capsizeDamageTimer.Reset(Mission.Current.CurrentTime);
			}
		}
		if (_isCapsized && _capsizeDamageTimer.Check(Mission.Current.CurrentTime) && !Mission.Current.DisableDying && Mission.Current.Mode != MissionMode.Conversation && Mission.Current.Mode != MissionMode.CutScene)
		{
			DealDamage(MaxHealth * 0.05f, null, out var _, out var _, out var _, out var _);
		}
	}

	private void ValidateShipAndDescendantEntitiesAndBoundingBoxes()
	{
		base.GameEntity.ValidateBoundingBox();
	}

	private void OnUnitAttached(Formation formation, Agent agent)
	{
		if (formation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Move)
		{
			SetPositioningOrdersToRallyPoint(applyToPlayerFormation: true, playersOrder: false);
		}
	}

	private void ComputeStaticLocalBoundingBox()
	{
		_localBoundingBoxCached.BeginRelaxation();
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			child.ValidateBoundingBox();
			BoundingBox localBoundingBox = child.GetLocalBoundingBox();
			_localBoundingBoxCached.RelaxWithBoundingBox(localBoundingBox);
		}
		_localBoundingBoxCacheInvalid = false;
	}

	private void InitializePartialDurabilities()
	{
		for (int i = 0; i < 6; i++)
		{
			_physics.SetTargetDurabilityOfPart(i, _partialHitPoints[i] / MaxPartialHealth);
		}
	}

	private void InitializeShipBoundingBox()
	{
		foreach (ShipOarMachine leftSideShipOarMachine in _leftSideShipOarMachines)
		{
			leftSideShipOarMachine.ArrangeOarBoundingBox();
		}
		foreach (ShipOarMachine rightSideShipOarMachine in _rightSideShipOarMachines)
		{
			rightSideShipOarMachine.ArrangeOarBoundingBox();
		}
		foreach (ShipUnmannedOar shipUnmannedOar in _shipUnmannedOars)
		{
			shipUnmannedOar.ArrangeOarBoundingBox();
		}
		foreach (MissionSail sail in _actuators.Sails)
		{
			List<GameEntity> children = new List<GameEntity>();
			sail.SailEntity.GetChildrenRecursive(ref children);
			foreach (GameEntity item in children)
			{
				item.EntityFlags |= EntityFlags.DoesNotAffectParentsLocalBb;
			}
			sail.SailEntity.SetHasCustomBoundingBoxValidationSystem(hasCustomBoundingBox: true);
			sail.SailEntity.SetBoundingboxDirty();
		}
	}

	private void RecalculateShipIsland()
	{
		ShipIslandCombinedID = 0uL;
		ulong islandMask = 0uL;
		BuildIslandMaskRecursive(this, ref islandMask);
		ulong visitedShipsMask = 0uL;
		ApplyIslandMaskRecursive(this, islandMask, ref visitedShipsMask);
	}

	private void BuildIslandMaskRecursive(MissionShip ship, ref ulong islandMask)
	{
		ulong shipUniqueBitwiseID = ship.ShipUniqueBitwiseID;
		if ((islandMask & shipUniqueBitwiseID) != 0L)
		{
			return;
		}
		islandMask |= shipUniqueBitwiseID;
		foreach (MissionShip navmeshConnectedShip in ship.GetNavmeshConnectedShips())
		{
			BuildIslandMaskRecursive(navmeshConnectedShip, ref islandMask);
		}
	}

	private void ApplyIslandMaskRecursive(MissionShip ship, ulong finalIslandMask, ref ulong visitedShipsMask)
	{
		ulong shipUniqueBitwiseID = ship.ShipUniqueBitwiseID;
		if ((visitedShipsMask & shipUniqueBitwiseID) != 0L)
		{
			return;
		}
		visitedShipsMask |= shipUniqueBitwiseID;
		ship.ShipIslandCombinedID = finalIslandMask;
		foreach (MissionShip navmeshConnectedShip in ship.GetNavmeshConnectedShips())
		{
			ApplyIslandMaskRecursive(navmeshConnectedShip, finalIslandMask, ref visitedShipsMask);
		}
	}

	private bool IsShipUpsideDown()
	{
		return base.GameEntity.GetLocalFrame().rotation.u.z <= 0.35f;
	}

	private void SetAbilityOfShipNavmeshFaces(bool enable)
	{
		Mission.Current.Scene.SetAbilityOfFacesWithId(DynamicNavmeshIdStart, enable);
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			int faceGroupId = DynamicNavmeshIdStart + attachmentPointMachine.RelatedShipNavmeshOffset;
			Mission.Current.Scene.SetAbilityOfFacesWithId(faceGroupId, enable);
		}
	}

	private void AttachDynamicNavmeshFromMachines(MBList<ShipAttachmentMachine> shipAttachmentMachines, MBList<ShipAttachmentPointMachine> shipAttachmentPointMachines)
	{
		SetAbilityOfFaces(base.GameEntity.IsValid && base.GameEntity.GetPhysicsState());
		for (int i = 0; i < shipAttachmentPointMachines.Count; i++)
		{
			int faceGroupId = DynamicNavmeshIdStart + shipAttachmentPointMachines[i].RelatedShipNavmeshOffset;
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(faceGroupId, isConnected: false, isBlocker: false, autoLocalize: false, finalizeBlockerConvexHullComputation: false, updateEntityFrame: false);
		}
		GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart, isConnected: false);
	}

	private bool CheckAttachedNavmeshSanity(bool isEditorMode)
	{
		bool result = true;
		if (isEditorMode)
		{
			base.GameEntity.Scene.ClearNavMesh();
			base.GameEntity.Scene.ImportNavigationMeshPrefabWithFrame(NavMeshPrefabName, base.GameEntity.GetGlobalFrame());
		}
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		MBList<ShipAttachmentMachine> mBList = new MBList<ShipAttachmentMachine>();
		base.GameEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			foreach (ShipAttachmentMachine scriptComponent in item.GetScriptComponents<ShipAttachmentMachine>())
			{
				mBList.Add(scriptComponent);
			}
		}
		MBList<ShipAttachmentPointMachine> mBList2 = new MBList<ShipAttachmentPointMachine>();
		foreach (WeakGameEntity item2 in children)
		{
			foreach (ShipAttachmentPointMachine scriptComponent2 in item2.GetScriptComponents<ShipAttachmentPointMachine>())
			{
				mBList2.Add(scriptComponent2);
			}
		}
		if (!CheckAttachedNavmeshSanityAux(mBList, mBList2, isEditorMode))
		{
			result = false;
		}
		if (!CheckSpawnPointsNavMeshSanityAux(isEditorMode))
		{
			result = false;
		}
		if (isEditorMode)
		{
			base.GameEntity.Scene.ClearNavMesh();
		}
		return result;
	}

	private bool CheckPhysicsOfChildren()
	{
		bool result = true;
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			int physicsTriangleCount = item.GetPhysicsTriangleCount();
			if (physicsTriangleCount > 4000)
			{
				string msg = $"Physics body has too much polygon {physicsTriangleCount} for ship part: '{base.GameEntity.Name}' - '{item.Name}'.";
				MBEditor.AddEntityWarning(base.GameEntity, msg);
			}
		}
		return result;
	}

	private bool CheckSpawnPoints(bool fromEditor)
	{
		bool result = true;
		if (MBObjectManager.Instance == null)
		{
			return result;
		}
		MBReadOnlyList<MissionShipObject> objects = MBObjectManager.Instance.GetObjects((MissionShipObject x) => x.Prefab == base.GameEntity.Name);
		if (objects.Count == 0)
		{
			return result;
		}
		MissionShipObject missionShipObject = objects[0];
		MBReadOnlyList<ShipHull> objects2 = MBObjectManager.Instance.GetObjects((ShipHull x) => x.MissionShipObjectId == missionShipObject.StringId);
		if (objects2.Count == 0)
		{
			return result;
		}
		ShipHull shipHull = objects2[0];
		if (shipHull.TotalCrewCapacity != shipHull.MainDeckCrewCapacity && base.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_crew_spawn").ToMBList().Count == 0)
		{
			string msg = "Ship with reinforcements '" + base.GameEntity.Name + "' does not have any crew spawn point.";
			if (fromEditor)
			{
				MBEditor.AddEntityWarning(base.GameEntity, msg);
			}
		}
		MBList<WeakGameEntity> mBList = base.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_outer_deck").ToMBList();
		MBList<WeakGameEntity> mBList2 = base.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_inner_deck").ToMBList();
		int num = mBList.Count + mBList2.Count;
		if (base.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_captain").Count == 0)
		{
			string msg2 = "Ship '" + base.GameEntity.Name + "' must have at least one captain spawn entity.";
			if (fromEditor)
			{
				MBEditor.AddEntityWarning(base.GameEntity, msg2);
			}
		}
		else
		{
			num++;
		}
		float num2 = 1f + Math.Max(NavalPerks.Boatswain.PopularCaptain.PrimaryBonus, NavalPerks.Boatswain.PopularCaptain.SecondaryBonus);
		int num3 = (int)((float)shipHull.MainDeckCrewCapacity * num2);
		if (num < num3)
		{
			string msg3 = $"Ship '{base.GameEntity.Name}': Main deck crew spawn point count {num}" + $"should be equal or greater than the value set in ship hull xml (including perks): {num3}.";
			if (fromEditor)
			{
				MBEditor.AddEntityWarning(base.GameEntity, msg3);
			}
		}
		return result;
	}

	private bool CheckOarCount(bool fromEditor)
	{
		bool result = true;
		if (MBObjectManager.Instance == null)
		{
			return result;
		}
		MBReadOnlyList<MissionShipObject> objects = MBObjectManager.Instance.GetObjects((MissionShipObject x) => x.Prefab == base.GameEntity.Name);
		if (objects.Count == 0)
		{
			return result;
		}
		int oarCount = objects[0].OarCount;
		int count = base.GameEntity.CollectChildrenEntitiesWithTag("oar_gate_left").Count;
		int count2 = base.GameEntity.CollectChildrenEntitiesWithTag("oar_gate_right").Count;
		if (count + count2 != oarCount)
		{
			string msg = "Oar count set in prefab does not match oar count set in mission ship xml for ship '" + base.GameEntity.Name + "'.";
			if (fromEditor)
			{
				MBEditor.AddEntityWarning(base.GameEntity, msg);
			}
			result = false;
		}
		return result;
	}

	private bool CheckSpawnPointsNavMeshSanityAux(bool fromEditor)
	{
		bool result = true;
		int faceGroupId;
		foreach (WeakGameEntity item in base.GameEntity.CollectChildrenEntitiesWithTag("rally_point"))
		{
			Vec3 position = item.GetGlobalFrame().origin;
			if (base.GameEntity.Scene.GetNavigationMeshForPosition(in position, out faceGroupId, 1.5f, excludeDynamicNavigationMeshes: false) == UIntPtr.Zero)
			{
				string msg = "Rally point '" + item.Name + "' is not on any navigation mesh face in ship '" + base.GameEntity.Name + "'.";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(item, msg);
				}
				result = false;
			}
		}
		foreach (WeakGameEntity item2 in base.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_captain"))
		{
			Vec3 position2 = item2.GetGlobalFrame().origin;
			if (base.GameEntity.Scene.GetNavigationMeshForPosition(in position2, out faceGroupId, 1.5f, excludeDynamicNavigationMeshes: false) == UIntPtr.Zero)
			{
				string msg2 = "Captain spawn point '" + item2.Name + "' is not on any navigation mesh face in ship '" + base.GameEntity.Name + "'.";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(item2, msg2);
				}
				result = false;
			}
		}
		foreach (WeakGameEntity item3 in base.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_outer_deck"))
		{
			Vec3 position3 = item3.GetGlobalFrame().origin;
			if (base.GameEntity.Scene.GetNavigationMeshForPosition(in position3, out faceGroupId, 1.5f, excludeDynamicNavigationMeshes: false) == UIntPtr.Zero)
			{
				string msg3 = "Outer deck spawn point '" + item3.Name + "' is not on any navigation mesh face in ship '" + base.GameEntity.Name + "'.";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(item3, msg3);
				}
				result = false;
			}
		}
		foreach (WeakGameEntity item4 in base.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_inner_deck"))
		{
			Vec3 position4 = item4.GetGlobalFrame().origin;
			if (base.GameEntity.Scene.GetNavigationMeshForPosition(in position4, out var _, 1.5f, excludeDynamicNavigationMeshes: false) == UIntPtr.Zero)
			{
				string msg4 = "Inner deck spawn point '" + item4.Name + "' is not on any navigation mesh face in ship '" + base.GameEntity.Name + "'.";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(item4, msg4);
				}
				result = false;
			}
		}
		return result;
	}

	private bool CheckAttachedNavmeshSanityAux(MBList<ShipAttachmentMachine> shipAttachmentMachines, MBList<ShipAttachmentPointMachine> shipAttachmentPointMachines, bool fromEditor)
	{
		bool result = true;
		PathFaceRecord[] faceRecords = new PathFaceRecord[fromEditor ? base.GameEntity.Scene.GetNavMeshFaceCount() : base.GameEntity.GetAttachedNavmeshFaceCount()];
		if (fromEditor)
		{
			base.GameEntity.Scene.GetAllNavmeshFaceRecords(faceRecords);
		}
		else
		{
			base.GameEntity.GetAttachedNavmeshFaceRecords(faceRecords);
		}
		HashSet<int> uniqueIdsFaces = new HashSet<int>();
		HashSet<int> hashSet = new HashSet<int>();
		HashSet<int> hashSet2 = new HashSet<int>();
		PathFaceRecord[] array = new PathFaceRecord[base.GameEntity.Scene.GetNavmeshFaceCountBetweenTwoIds(DynamicNavmeshIdStart, DynamicNavmeshIdStart + 50)];
		base.GameEntity.Scene.GetNavmeshFaceRecordsBetweenTwoIds(DynamicNavmeshIdStart, DynamicNavmeshIdStart + 50, array);
		PathFaceRecord[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			PathFaceRecord record = array2[i];
			if (record.FaceGroupIndex < DynamicNavmeshIdStart || record.FaceGroupIndex > DynamicNavmeshIdStart + 50)
			{
				string msg = $"The face with id {record.FaceGroupIndex - DynamicNavmeshIdStart} must not be attached to {base.GameEntity.Name}. Ids must be between 0 and {50}.";
				if (fromEditor)
				{
					MBEditor.AddNavMeshWarning(base.GameEntity.Scene, record, msg);
				}
				result = false;
			}
			else if (record.FaceGroupIndex > DynamicNavmeshIdStart && !uniqueIdsFaces.Add(record.FaceGroupIndex))
			{
				string msg2 = $"Attached navmesh must have faces with unique group ids. Id: {record.FaceGroupIndex - DynamicNavmeshIdStart} is not unique";
				if (fromEditor)
				{
					MBEditor.AddNavMeshWarning(base.GameEntity.Scene, record, msg2);
				}
				result = false;
			}
		}
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			PathFaceRecord faceRecord = array2[i];
			if (faceRecord.FaceGroupIndex != DynamicNavmeshIdStart && !base.GameEntity.Scene.HasNavmeshFaceUnsharedEdges(in faceRecord))
			{
				string msg3 = $"The face with id {faceRecord.FaceGroupIndex - DynamicNavmeshIdStart} must not be fully enclosed; it must have at least one unshared edge.";
				if (fromEditor)
				{
					MBEditor.AddNavMeshWarning(base.GameEntity.Scene, faceRecord, msg3);
				}
				result = false;
			}
		}
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		string name = base.GameEntity.Name;
		bool flag = false;
		foreach (ShipAttachmentMachine shipAttachmentMachine in shipAttachmentMachines)
		{
			int num = DynamicNavmeshIdStart + shipAttachmentMachine.RelatedShipNavmeshOffset;
			if (num <= DynamicNavmeshIdStart || num > DynamicNavmeshIdStart + 50)
			{
				string msg4 = $"{name}: Every {shipAttachmentMachine.GameEntity.Name}'s RelatedShipNavmeshOffset must be between 1 and {50}.";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(shipAttachmentMachine.GameEntity, msg4);
				}
				result = false;
			}
			if (!hashSet.Add(shipAttachmentMachine.RelatedShipNavmeshOffset))
			{
				flag = true;
				list.Add(shipAttachmentMachine.GameEntity);
			}
			if (uniqueIdsFaces.Contains(shipAttachmentMachine.RelatedShipNavmeshOffset + DynamicNavmeshIdStart))
			{
				uniqueIdsFaces.Remove(shipAttachmentMachine.RelatedShipNavmeshOffset + DynamicNavmeshIdStart);
			}
			MatrixFrame globalFrame = shipAttachmentMachine.GameEntity.GetGlobalFrame();
			if (base.GameEntity.Scene.GetNavigationMeshForPosition(in globalFrame.origin, out var faceGroupId, 1.5f, excludeDynamicNavigationMeshes: false) == UIntPtr.Zero)
			{
				string msg5 = $"{name}: shipAttachmentMachine with related id {shipAttachmentMachine.RelatedShipNavmeshOffset} is not on any navmesh face";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(shipAttachmentMachine.GameEntity, msg5);
				}
				result = false;
			}
			else if (faceGroupId != num)
			{
				string msg6 = $"{name}: ShipAttachmentMachine script with nav mesh id {shipAttachmentMachine.RelatedShipNavmeshOffset} is not on a face with the same id. Current face id: {faceGroupId}";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(shipAttachmentMachine.GameEntity, msg6);
				}
				result = false;
			}
		}
		if (flag)
		{
			foreach (WeakGameEntity item in list)
			{
				string msg7 = name + ": shipAttachmentMachine '" + item.Name + "' must have a unique RelatedShipNavmeshOffset with respect to other ShipAttachmentMachines";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(item, msg7);
				}
				result = false;
			}
			flag = false;
			list.Clear();
		}
		foreach (ShipAttachmentPointMachine shipAttachmentPointMachine in shipAttachmentPointMachines)
		{
			int num2 = DynamicNavmeshIdStart + shipAttachmentPointMachine.RelatedShipNavmeshOffset;
			if (num2 <= DynamicNavmeshIdStart || num2 > DynamicNavmeshIdStart + 50)
			{
				string msg8 = $"{name}: Every {shipAttachmentPointMachine.GameEntity.Name}'s RelatedShipNavmeshOffset must be between 1 and {50}.";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(shipAttachmentPointMachine.GameEntity, msg8);
				}
				result = false;
			}
			if (!hashSet2.Add(shipAttachmentPointMachine.RelatedShipNavmeshOffset))
			{
				flag = true;
				list.Add(shipAttachmentPointMachine.GameEntity);
			}
			if (uniqueIdsFaces.Contains(shipAttachmentPointMachine.RelatedShipNavmeshOffset + DynamicNavmeshIdStart))
			{
				uniqueIdsFaces.Remove(shipAttachmentPointMachine.RelatedShipNavmeshOffset + DynamicNavmeshIdStart);
			}
			MatrixFrame globalFrame2 = shipAttachmentPointMachine.GameEntity.GetGlobalFrame();
			if (base.GameEntity.Scene.GetNavigationMeshForPosition(in globalFrame2.origin, out var faceGroupId2, 1.5f, excludeDynamicNavigationMeshes: false) == UIntPtr.Zero)
			{
				string msg9 = $"{name}: shipAttachmentPointMachine with related id {shipAttachmentPointMachine.RelatedShipNavmeshOffset} is not on any navmesh face";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(shipAttachmentPointMachine.GameEntity, msg9);
				}
				result = false;
			}
			else if (faceGroupId2 != num2)
			{
				string msg10 = $"{name}: ShipAttachmentPointMachine script with nav mesh face id {shipAttachmentPointMachine.RelatedShipNavmeshOffset} is not on a face with the same id. Current face id: {faceGroupId2}";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(shipAttachmentPointMachine.GameEntity, msg10);
				}
				result = false;
			}
		}
		foreach (PathFaceRecord item2 in array.Where((PathFaceRecord pathFaceRecord) => uniqueIdsFaces.Contains(pathFaceRecord.FaceGroupIndex)).ToList())
		{
			string msg11 = $"{name}: The face with id {item2.FaceGroupIndex - DynamicNavmeshIdStart} has not been attached to {base.GameEntity.Name}. " + $"There should be a shipAttachmentMachine or a shipAttachmentPointMachine with RelatedShipNavmeshOffset: {item2.FaceGroupIndex - DynamicNavmeshIdStart}";
			if (fromEditor)
			{
				MBEditor.AddNavMeshWarning(base.GameEntity.Scene, item2, msg11);
			}
			result = false;
		}
		if (flag)
		{
			foreach (WeakGameEntity item3 in list)
			{
				string msg12 = name + ": ShipAttachmentPointMachine '" + item3.Name + "' must have a unique RelatedShipNavmeshOffset with respect to other ShipAttachmentPoints";
				if (fromEditor)
				{
					MBEditor.AddEntityWarning(item3, msg12);
				}
			}
			result = false;
		}
		return result;
	}

	private int DeckSpawnFrameSortingFunction(MatrixFrame deckFrame1, MatrixFrame deckFrame2)
	{
		float value = Vec3.DotProduct(deckFrame1.origin, Vec3.Forward);
		return -Vec3.DotProduct(deckFrame2.origin, Vec3.Forward).CompareTo(value);
	}

	private void InitializeLists(bool isForCheckingForProblems)
	{
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenRecursive(ref children);
		_rightSideShipOarMachines = new MBList<ShipOarMachine>();
		_leftSideShipOarMachines = new MBList<ShipOarMachine>();
		_shipOarMachines = new MBList<ShipOarMachine>();
		_shipUnmannedOars = new MBList<ShipUnmannedOar>();
		_climbingMachines = new MBList<ClimbingMachine>();
		ShipSiegeWeapon = null;
		_allDestructibleComponents = new MBList<DestructableComponent>();
		_ammoBarrels = new MBList<AmmoBarrelBase>();
		foreach (WeakGameEntity item in children)
		{
			if (item.HasScriptOfType<ShipOarMachine>())
			{
				if (item.GetLocalFrame().origin.AsVec2.DotProduct(Vec2.Side) > 0f)
				{
					_rightSideShipOarMachines.Add(item.GetFirstScriptOfType<ShipOarMachine>());
				}
				else
				{
					_leftSideShipOarMachines.Add(item.GetFirstScriptOfType<ShipOarMachine>());
				}
			}
			else if (item.HasScriptOfType<ShipControllerMachine>())
			{
				ShipControllerMachine = item.GetFirstScriptOfType<ShipControllerMachine>();
			}
			else if (item.HasScriptOfType<ClimbingMachine>())
			{
				_climbingMachines.Add(item.GetFirstScriptOfType<ClimbingMachine>());
			}
			else if (item.HasScriptOfType<ShipUnmannedOar>())
			{
				_shipUnmannedOars.Add(item.GetFirstScriptOfType<ShipUnmannedOar>());
			}
			else if (item.HasScriptOfType<RangedSiegeWeapon>())
			{
				ShipSiegeWeapon = item.GetFirstScriptOfType<RangedSiegeWeapon>();
			}
			else if (item.HasScriptOfType<MissionShipRam>())
			{
				_ram = item.GetFirstScriptOfType<MissionShipRam>();
			}
			else if (item.HasScriptOfType<AmmoBarrelBase>())
			{
				_ammoBarrels.Add(item.GetFirstScriptOfType<AmmoBarrelBase>());
			}
			if (item.HasScriptOfType<DestructableComponent>())
			{
				_allDestructibleComponents.Add(item.GetFirstScriptOfType<DestructableComponent>());
			}
		}
		_leftSideShipOarMachines.Sort(delegate(ShipOarMachine oar1, ShipOarMachine oar2)
		{
			float y3 = oar1.GameEntity.GetLocalFrame().origin.y;
			float y4 = oar2.GameEntity.GetLocalFrame().origin.y;
			return y4.CompareTo(y3);
		});
		_rightSideShipOarMachines.Sort(delegate(ShipOarMachine oar1, ShipOarMachine oar2)
		{
			float y3 = oar1.GameEntity.GetLocalFrame().origin.y;
			float y4 = oar2.GameEntity.GetLocalFrame().origin.y;
			return y4.CompareTo(y3);
		});
		for (int num = 0; num < _leftSideShipOarMachines.Count; num++)
		{
			ShipOarMachine shipOarMachine = _leftSideShipOarMachines[num];
			ShipOarMachine shipOarMachine2 = _rightSideShipOarMachines[num];
			float y = shipOarMachine.GameEntity.GetLocalFrame().origin.y;
			float y2 = shipOarMachine2.GameEntity.GetLocalFrame().origin.y;
			Math.Abs(y - y2);
			_shipOarMachines.Add(shipOarMachine);
			_shipOarMachines.Add(shipOarMachine2);
		}
		MBList<ShipControllerMachine> mBList = base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<ShipControllerMachine>();
		ShipControllerMachine = ((mBList.Count > 0) ? mBList[0] : null);
		_attachmentMachines = base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<ShipAttachmentMachine>();
		_attachmentPointMachines = base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<ShipAttachmentPointMachine>();
		_sailVisuals = base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<SailVisual>();
	}

	private void LoadSpawnPoints()
	{
		GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity);
		_outerDeckLocalFrames = new MBList<MatrixFrame>();
		foreach (GameEntity item4 in gameEntity.CollectChildrenEntitiesWithTag("sp_troop_outer_deck").ToMBList())
		{
			MatrixFrame item = gameEntity.GetGlobalFrame().TransformToLocal(item4.GetGlobalFrame());
			_outerDeckLocalFrames.Add(item);
		}
		_innerDeckLocalFrames = new MBList<MatrixFrame>();
		foreach (GameEntity item5 in gameEntity.CollectChildrenEntitiesWithTag("sp_troop_inner_deck").ToMBList())
		{
			MatrixFrame item2 = gameEntity.GetGlobalFrame().TransformToLocal(item5.GetGlobalFrame());
			_innerDeckLocalFrames.Add(item2);
		}
		_crewSpawnLocalFrames = new MBList<MatrixFrame>();
		foreach (GameEntity item6 in gameEntity.CollectChildrenEntitiesWithTag("sp_troop_crew_spawn").ToMBList())
		{
			MatrixFrame item3 = gameEntity.GetGlobalFrame().TransformToLocal(item6.GetGlobalFrame());
			_crewSpawnLocalFrames.Add(item3);
		}
		_outerDeckLocalFrames.Sort(DeckSpawnFrameSortingFunction);
		_innerDeckLocalFrames.Sort(DeckSpawnFrameSortingFunction);
		List<GameEntity> list = gameEntity.CollectChildrenEntitiesWithTag("sp_troop_captain");
		_innerDeckLocalFrames.Add(gameEntity.GetGlobalFrame().TransformToLocal(list[0].GetGlobalFrame()));
		CrewSizeOnMainDeck = TaleWorlds.Library.MathF.Min(DeckFrameCount, ShipOrigin.MainDeckCrewCapacity);
		ShipPlacementDetachment = new ShipPlacementDetachment(in this);
	}

	protected override bool CanPhysicsCollideBetweenTwoEntities(WeakGameEntity myEntity, BodyFlags myEntityBodyFlags, WeakGameEntity otherEntity, BodyFlags otherEntityBodyFlags)
	{
		return !otherEntityBodyFlags.HasAnyFlag(BodyFlags.Moveable) || otherEntityBodyFlags.HasAnyFlag(BodyFlags.Dynamic);
	}

	private void LoadShipBanners()
	{
		GameEntity entity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity);
		_bannerEntities = entity.CollectChildrenEntitiesWithTag("banner_with_faction_color").ToMBList();
		_sailMeshEntities = entity.CollectChildrenEntitiesWithTag("sail_mesh_entity").ToMBList();
	}

	public static bool AreShipsConnected(MissionShip ship1, MissionShip ship2)
	{
		return (ship1.ShipIslandCombinedID & ship2.ShipIslandCombinedID) != 0;
	}

	public void OnSetRangedWeaponControlMode(bool value)
	{
		if (ShipSiegeWeapon != null)
		{
			(ShipSiegeWeapon.Ai as ShipBallistaAI).SetIsUnderDirectControl(value);
		}
		foreach (SailVisual sailVisual in _sailVisuals)
		{
			sailVisual.SetBallistaRopeVisibility(!value);
		}
	}

	public bool IsAgentUsingSiegeWeapon(Agent agent)
	{
		if (ShipSiegeWeapon != null)
		{
			return ShipSiegeWeapon.PilotAgent == agent;
		}
		return false;
	}

	public void SetCustomSailSetting(bool enableCustomSailSetting, SailInput customSailSetting)
	{
		HasCustomSailSetting = enableCustomSailSetting;
		_customSailSetting = customSailSetting;
	}

	public void ShootBallista()
	{
		ShipSiegeWeapon.Shoot();
	}

	public void TryToMaintainConnectionToAnotherShip(MissionShip otherShip, bool forceBridge = true, bool unbreakableBridge = false)
	{
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null)
			{
				continue;
			}
			attachmentMachine.SetPreferredTargetShip(otherShip);
			if (attachmentMachine.LinkedAttachmentPointMachine.CurrentAttachment == null)
			{
				attachmentMachine.SetCanConnectToFriends(canConnectToFriends: true);
				ShipAttachmentPointMachine bestEnemyAttachment = attachmentMachine.GetBestEnemyAttachment(checkAttachmentAlreadyExists: true, checkInteractionDistance: false);
				if (bestEnemyAttachment != null)
				{
					attachmentMachine.ConnectWithAttachmentPointMachine(bestEnemyAttachment, forceBridge, unbreakableBridge);
				}
			}
		}
	}

	public void TryToConnectionToAttachmentMachine(ShipAttachmentMachine otherAttachmentMachine, bool forceBridge = true, bool unbreakableBridge = false)
	{
		ShipAttachmentPointMachine shipAttachmentPointMachine = null;
		if (otherAttachmentMachine.CurrentAttachment == null && otherAttachmentMachine.LinkedAttachmentPointMachine.CurrentAttachment == null)
		{
			shipAttachmentPointMachine = otherAttachmentMachine.GetBestEnemyAttachment(checkAttachmentAlreadyExists: true, checkInteractionDistance: false);
		}
		if (shipAttachmentPointMachine != null)
		{
			otherAttachmentMachine.SetPreferredTargetShip(shipAttachmentPointMachine.OwnerShip);
			otherAttachmentMachine.SetCanConnectToFriends(canConnectToFriends: true);
			otherAttachmentMachine.ConnectWithAttachmentPointMachine(shipAttachmentPointMachine, forceBridge, unbreakableBridge);
		}
	}

	public void DisconnectedWithShip(MissionShip otherShip)
	{
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null && attachmentMachine.GetPreferredTargetShip() == otherShip)
			{
				attachmentMachine.SetPreferredTargetShip(null);
				if (attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip == otherShip)
				{
					attachmentMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
				}
			}
		}
	}

	public void InvalidateLocalBoundingBoxCache()
	{
		_localBoundingBoxCacheInvalid = true;
		base.GameEntity.SetBoundingboxDirty();
	}

	public void InvalidateActiveFormationTroopOnShipCache()
	{
		_anyActiveFormationTroopOnShip.Expire();
	}

	internal void SeparateFromShip(MissionShip otherShip)
	{
		bool flag = false;
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null && attachmentMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected && attachmentMachine.CurrentAttachment.ShipIslandsConnected && (attachmentMachine.CurrentAttachment.AttachmentSource.OwnerShip == otherShip || attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip == otherShip))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			RecalculateShipIsland();
			if ((ShipIslandCombinedID & otherShip.ShipUniqueBitwiseID) == 0L)
			{
				otherShip.RecalculateShipIsland();
			}
		}
	}

	internal static void MergeShipIslands(MissionShip ship1, MissionShip ship2)
	{
		if (ship1.ShipIslandCombinedID == ship2.ShipIslandCombinedID)
		{
			return;
		}
		ulong num = ship1.ShipIslandCombinedID | ship2.ShipIslandCombinedID;
		ship1._temporaryMissionShipQueue.Clear();
		ship1._temporaryMissionShipQueue.Enqueue(ship1);
		while (ship1._temporaryMissionShipQueue.Count > 0)
		{
			MissionShip missionShip = ship1._temporaryMissionShipQueue.Dequeue();
			if (missionShip.ShipIslandCombinedID == num)
			{
				continue;
			}
			missionShip.ShipIslandCombinedID |= num;
			num = missionShip.ShipIslandCombinedID;
			foreach (MissionShip navmeshConnectedShip in missionShip.GetNavmeshConnectedShips())
			{
				if (navmeshConnectedShip.ShipIslandCombinedID != num)
				{
					ship1._temporaryMissionShipQueue.Enqueue(navmeshConnectedShip);
				}
			}
		}
	}
}
