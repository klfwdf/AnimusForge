using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace TaleWorlds.MountAndBlade;

public class BatteringRam : SiegeWeapon, IPathHolder, IPrimarySiegeWeapon, IMoveableSiegeWeapon, ISpawnable
{
	[DefineSynchedMissionObjectType(typeof(BatteringRam))]
	public struct BatteringRamRecord : ISynchedMissionObjectReadableRecord
	{
		public bool HasArrivedAtTarget { get; private set; }

		public int State { get; private set; }

		public float TotalDistanceTraveled { get; private set; }

		public bool ReadFromNetwork(ref bool bufferReadValid)
		{
			HasArrivedAtTarget = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			State = GameNetworkMessage.ReadIntFromPacket(CompressionMission.BatteringRamStateCompressionInfo, ref bufferReadValid);
			TotalDistanceTraveled = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}
	}

	public enum RamState
	{
		Stable,
		Hitting,
		AfterHit,
		NumberOfStates
	}

	private string _pathEntityName = "Path";

	private const string PullStandingPointTag = "pull";

	private const string RightStandingPointTag = "right";

	private const string IdleAnimation = "batteringram_idle";

	private const string KnockAnimation = "batteringram_fire";

	private const string KnockSlowerAnimation = "batteringram_fire_weak";

	private const string KnockSlowestAnimation = "batteringram_fire_weakest";

	private const float KnockAnimationHitProgress = 0.5f;

	private const float KnockSlowerAnimationHitProgress = 0.56f;

	private const float KnockSlowestAnimationHitProgress = 0.58f;

	private string _gateTag = "gate";

	public bool GhostEntityMove = true;

	public float GhostEntitySpeedMultiplier = 1f;

	private string _sideTag;

	private FormationAI.BehaviorSide _weaponSide;

	public float WheelDiameter = 1.3f;

	public int GateNavMeshId = 7;

	public int DisabledNavMeshID = 8;

	private int _bridgeNavMeshID1 = 8;

	private int _bridgeNavMeshID2 = 8;

	private int _ditchNavMeshID1 = 9;

	private int _ditchNavMeshID2 = 10;

	private int _groundToBridgeNavMeshID1 = 12;

	private int _groundToBridgeNavMeshID2 = 13;

	public int NavMeshIdToDisableOnDestination = -1;

	public float MinSpeed = 0.5f;

	public float MaxSpeed = 1f;

	public float DamageMultiplier = 10f;

	private int _usedPower;

	private float _storedPower;

	private List<StandingPoint> _pullStandingPoints;

	private List<MatrixFrame> _pullStandingPointLocalIKFrames;

	private GameEntity _ditchFillDebris;

	private GameEntity _batteringRamBody;

	private Skeleton _batteringRamBodySkeleton;

	private bool _isGhostMovementOn;

	private bool _isAllStandingPointsDisabled;

	private RamState _state;

	private CastleGate _gate;

	private bool _hasArrivedAtTarget;

	public SiegeWeaponMovementComponent MovementComponent { get; private set; }

	public FormationAI.BehaviorSide WeaponSide => _weaponSide;

	public string PathEntity => _pathEntityName;

	public bool EditorGhostEntityMove => GhostEntityMove;

	public RamState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (_state != value)
			{
				_state = value;
			}
		}
	}

	public MissionObject TargetCastlePosition => _gate;

	public float SiegeWeaponPriority => 25f;

	public int OverTheWallNavMeshID => GateNavMeshId;

	public bool HoldLadders => !MovementComponent.HasArrivedAtTarget;

	public bool SendLadders => MovementComponent.HasArrivedAtTarget;

	public bool HasArrivedAtTarget
	{
		get
		{
			return _hasArrivedAtTarget;
		}
		set
		{
			if (!GameNetwork.IsClientOrReplay)
			{
				MovementComponent.SetDestinationNavMeshIdState(!value);
			}
			if (_hasArrivedAtTarget != value)
			{
				_hasArrivedAtTarget = value;
				if (GameNetwork.IsServerOrRecorder)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SetBatteringRamHasArrivedAtTarget(base.Id));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
				}
				else if (GameNetwork.IsClientOrReplay)
				{
					MovementComponent.MoveToTargetAsClient();
				}
			}
		}
	}

	public override bool IsDeactivated
	{
		get
		{
			if (_gate != null && !_gate.IsDestroyed && (_gate.State != CastleGate.GateState.Open || !HasArrivedAtTarget))
			{
				return base.IsDeactivated;
			}
			return true;
		}
	}

	public bool HasCompletedAction()
	{
		if (_gate != null && !_gate.IsDestroyed)
		{
			if (_gate.State == CastleGate.GateState.Open)
			{
				return HasArrivedAtTarget;
			}
			return false;
		}
		return true;
	}

	public override void Disable()
	{
		base.Disable();
		if (!GameNetwork.IsClientOrReplay)
		{
			if (DisabledNavMeshID != 0)
			{
				base.Scene.SetAbilityOfFacesWithId(DisabledNavMeshID, isEnabled: true);
			}
			base.Scene.SetAbilityOfFacesWithId(DynamicNavmeshIdStart + 4, isEnabled: false);
		}
	}

	public override SiegeEngineType GetSiegeEngineType()
	{
		return DefaultSiegeEngineTypes.Ram;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		DestructableComponent firstScriptOfType = base.GameEntity.GetFirstScriptOfType<DestructableComponent>();
		if (firstScriptOfType != null)
		{
			firstScriptOfType.BattleSide = BattleSideEnum.Attacker;
		}
		_state = RamState.Stable;
		IEnumerable<WeakGameEntity> source = from ewgt in base.Scene.FindWeakEntitiesWithTag(_gateTag).ToList()
			where ewgt.HasScriptOfType<CastleGate>()
			select ewgt;
		if (!source.IsEmpty())
		{
			_gate = source.First().GetFirstScriptOfType<CastleGate>();
			_gate.AttackerSiegeWeapon = this;
		}
		AddRegularMovementComponent();
		_batteringRamBody = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("body"));
		_batteringRamBodySkeleton = _batteringRamBody.Skeleton;
		_batteringRamBodySkeleton.SetAnimationAtChannel("batteringram_idle", 0, 1f, 0f);
		_pullStandingPoints = new List<StandingPoint>();
		_pullStandingPointLocalIKFrames = new List<MatrixFrame>();
		MatrixFrame m = base.GameEntity.GetGlobalFrame();
		if (base.StandingPoints != null)
		{
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				standingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
				if (standingPoint.GameEntity.HasTag("pull"))
				{
					standingPoint.IsDeactivated = true;
					_pullStandingPoints.Add(standingPoint);
					_pullStandingPointLocalIKFrames.Add(standingPoint.GameEntity.GetGlobalFrame().TransformToLocal(in m));
					standingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
				}
			}
		}
		switch (_sideTag)
		{
		case "left":
			_weaponSide = FormationAI.BehaviorSide.Left;
			break;
		case "middle":
			_weaponSide = FormationAI.BehaviorSide.Middle;
			break;
		case "right":
			_weaponSide = FormationAI.BehaviorSide.Right;
			break;
		default:
			_weaponSide = FormationAI.BehaviorSide.Middle;
			break;
		}
		_ditchFillDebris = base.Scene.FindEntitiesWithTag("ditch_filler").FirstOrDefault((GameEntity df) => df.HasTag(_sideTag));
		SetScriptComponentToTick(GetTickRequirement());
		Mission.Current.AddToWeaponListForFriendlyFirePreventing(this);
	}

	private void AddRegularMovementComponent()
	{
		MovementComponent = new SiegeWeaponMovementComponent
		{
			PathEntityName = PathEntity,
			MinSpeed = MinSpeed,
			MaxSpeed = MaxSpeed,
			MainObject = this,
			WheelDiameter = WheelDiameter,
			NavMeshIdToDisableOnDestination = NavMeshIdToDisableOnDestination,
			MovementSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/siege/batteringram/move"),
			GhostEntitySpeedMultiplier = GhostEntitySpeedMultiplier
		};
		AddComponent(MovementComponent);
	}

	protected internal override void OnDeploymentStateChanged(bool isDeployed)
	{
		base.OnDeploymentStateChanged(isDeployed);
		if (!(_ditchFillDebris != null))
		{
			return;
		}
		_ditchFillDebris.SetVisibilityExcludeParents(isDeployed);
		if (!GameNetwork.IsClientOrReplay)
		{
			if (isDeployed)
			{
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavMeshID1, isEnabled: true);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavMeshID2, isEnabled: true);
				Mission.Current.Scene.SeparateFacesWithId(_ditchNavMeshID1, _groundToBridgeNavMeshID1);
				Mission.Current.Scene.SeparateFacesWithId(_ditchNavMeshID2, _groundToBridgeNavMeshID2);
				Mission.Current.Scene.MergeFacesWithId(_bridgeNavMeshID1, _groundToBridgeNavMeshID1, 0);
				Mission.Current.Scene.MergeFacesWithId(_bridgeNavMeshID2, _groundToBridgeNavMeshID2, 0);
			}
			else
			{
				Mission.Current.Scene.SeparateFacesWithId(_bridgeNavMeshID1, _groundToBridgeNavMeshID1);
				Mission.Current.Scene.SeparateFacesWithId(_bridgeNavMeshID2, _groundToBridgeNavMeshID2);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavMeshID1, isEnabled: false);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavMeshID2, isEnabled: false);
				Mission.Current.Scene.MergeFacesWithId(_ditchNavMeshID1, _groundToBridgeNavMeshID1, 0);
				Mission.Current.Scene.MergeFacesWithId(_ditchNavMeshID2, _groundToBridgeNavMeshID2, 0);
			}
		}
	}

	public MatrixFrame GetInitialFrame()
	{
		if (MovementComponent != null)
		{
			return MovementComponent.GetInitialFrame();
		}
		return base.GameEntity.GetGlobalFrame();
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			return base.GetTickRequirement() | TickRequirement.Tick | TickRequirement.TickParallel;
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTickParallel(float dt)
	{
		base.OnTickParallel(dt);
		if (!base.GameEntity.IsVisibleIncludeParents())
		{
			return;
		}
		MovementComponent.TickParallelManually(dt);
		MatrixFrame boundEntityGlobalFrame = base.GameEntity.GetGlobalFrame();
		for (int i = 0; i < _pullStandingPoints.Count; i++)
		{
			StandingPoint standingPoint = _pullStandingPoints[i];
			if (standingPoint.HasUser)
			{
				if (standingPoint.UserAgent.IsInBeingStruckAction)
				{
					standingPoint.UserAgent.ClearHandInverseKinematics();
				}
				else
				{
					standingPoint.UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(_pullStandingPointLocalIKFrames[i], in boundEntityGlobalFrame);
				}
			}
		}
		if (MovementComponent.HasArrivedAtTarget && !IsDeactivated)
		{
			int userCountNotInStruckAction = base.UserCountNotInStruckAction;
			if (userCountNotInStruckAction > 0)
			{
				float animationParameterAtChannel = _batteringRamBodySkeleton.GetAnimationParameterAtChannel(0);
				UpdateHitAnimationWithProgress((userCountNotInStruckAction - 1) / 2, animationParameterAtChannel);
			}
		}
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!base.GameEntity.IsVisibleIncludeParents() || GameNetwork.IsClientOrReplay)
		{
			return;
		}
		if (MovementComponent.HasArrivedAtTarget && !HasArrivedAtTarget)
		{
			HasArrivedAtTarget = true;
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				standingPoint.SetIsDeactivatedSynched(standingPoint.GameEntity.HasTag("move"));
			}
			if (DisabledNavMeshID != 0)
			{
				base.GameEntity.Scene.SetAbilityOfFacesWithId(DisabledNavMeshID, isEnabled: false);
			}
		}
		if (!MovementComponent.HasArrivedAtTarget)
		{
			return;
		}
		if (_gate == null || _gate.IsDestroyed || _gate.IsGateOpen)
		{
			if (_isAllStandingPointsDisabled)
			{
				return;
			}
			foreach (StandingPoint standingPoint2 in base.StandingPoints)
			{
				standingPoint2.SetIsDeactivatedSynched(value: true);
			}
			_isAllStandingPointsDisabled = true;
			return;
		}
		if (_isAllStandingPointsDisabled && !IsDeactivated)
		{
			foreach (StandingPoint standingPoint3 in base.StandingPoints)
			{
				standingPoint3.SetIsDeactivatedSynched(value: false);
			}
			_isAllStandingPointsDisabled = false;
		}
		int userCountNotInStruckAction = base.UserCountNotInStruckAction;
		switch (State)
		{
		case RamState.Stable:
			if (userCountNotInStruckAction > 0)
			{
				State = RamState.Hitting;
				SetAbilityOfConditionalFaces(enabled: false);
				_usedPower = userCountNotInStruckAction;
				_storedPower = 0f;
				StartHitAnimationWithProgress((userCountNotInStruckAction - 1) / 2, 0f);
			}
			break;
		case RamState.Hitting:
			if (userCountNotInStruckAction > 0 && _gate != null && !_gate.IsGateOpen)
			{
				int num = (userCountNotInStruckAction - 1) / 2;
				float animationParameterAtChannel = _batteringRamBodySkeleton.GetAnimationParameterAtChannel(0);
				if ((_usedPower - 1) / 2 != num)
				{
					StartHitAnimationWithProgress(num, animationParameterAtChannel);
				}
				_usedPower = userCountNotInStruckAction;
				_storedPower += (float)_usedPower * dt;
				float num2 = num switch
				{
					2 => 0.56f, 
					3 => 0.5f, 
					_ => 0.58f, 
				};
				string animationName = num switch
				{
					2 => "batteringram_fire_weak", 
					3 => "batteringram_fire", 
					_ => "batteringram_fire_weakest", 
				};
				if (animationParameterAtChannel >= num2)
				{
					MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
					float num3 = _storedPower * DamageMultiplier;
					num3 /= animationParameterAtChannel * MBAnimation.GetAnimationDuration(animationName);
					_gate.DestructionComponent.TriggerOnHit(base.PilotAgent, (int)num3, globalFrame.origin, globalFrame.rotation.f, in MissionWeapon.Invalid, -1, this);
					State = RamState.AfterHit;
				}
			}
			else
			{
				_batteringRamBody.GetFirstScriptOfType<SynchedMissionObject>().SetAnimationAtChannelSynched("batteringram_idle", 0);
				State = RamState.Stable;
				SetAbilityOfConditionalFaces(enabled: true);
			}
			break;
		case RamState.AfterHit:
			if (_batteringRamBodySkeleton.GetAnimationParameterAtChannel(0) > 0.999f)
			{
				State = RamState.Stable;
				SetAbilityOfConditionalFaces(enabled: true);
			}
			break;
		}
	}

	private void StartHitAnimationWithProgress(int powerStage, float progress)
	{
		_batteringRamBody.GetFirstScriptOfType<SynchedMissionObject>().SetAnimationAtChannelSynched(powerStage switch
		{
			1 => "batteringram_fire_weak", 
			2 => "batteringram_fire", 
			_ => "batteringram_fire_weakest", 
		}, 0);
		if (progress > 0f)
		{
			_batteringRamBody.GetFirstScriptOfType<SynchedMissionObject>().SetAnimationChannelParameterSynched(0, progress);
		}
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (standingPoint.HasUser && standingPoint.GameEntity.HasTag("pull"))
			{
				ActionIndexCache actionIndexCache = GetActionCodeForStandingPoint(standingPoint, powerStage);
				if (!standingPoint.UserAgent.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, -0.2f, 0.4f, progress) && standingPoint.UserAgent.Controller == AgentControllerType.AI)
				{
					standingPoint.UserAgent.StopUsingGameObject(isSuccessful: false);
				}
			}
		}
	}

	private void UpdateHitAnimationWithProgress(int powerStage, float progress)
	{
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (standingPoint.HasUser && standingPoint.GameEntity.HasTag("pull"))
			{
				ActionIndexCache actionCodeForStandingPoint = GetActionCodeForStandingPoint(standingPoint, powerStage);
				if (standingPoint.UserAgent.GetCurrentAction(1) == actionCodeForStandingPoint)
				{
					standingPoint.UserAgent.SetCurrentActionProgress(1, progress);
				}
			}
		}
	}

	private ActionIndexCache GetActionCodeForStandingPoint(StandingPoint standingPoint, int powerStage)
	{
		bool flag = standingPoint.GameEntity.HasTag("right");
		ActionIndexCache result = ActionIndexCache.act_none;
		switch (powerStage)
		{
		case 0:
			result = (flag ? ActionIndexCache.act_usage_batteringram_left_slowest : ActionIndexCache.act_usage_batteringram_right_slowest);
			break;
		case 1:
			result = (flag ? ActionIndexCache.act_usage_batteringram_left_slower : ActionIndexCache.act_usage_batteringram_right_slower);
			break;
		case 2:
			result = (flag ? ActionIndexCache.act_usage_batteringram_left : ActionIndexCache.act_usage_batteringram_right);
			break;
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\BatteringRam.cs", "GetActionCodeForStandingPoint", 583);
			break;
		}
		return result;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new BatteringRamAI(this);
	}

	protected internal override void OnMissionReset()
	{
		base.OnMissionReset();
		_state = RamState.Stable;
		if (!GameNetwork.IsClientOrReplay)
		{
			SetAbilityOfConditionalFaces(enabled: true);
		}
		_hasArrivedAtTarget = false;
		_batteringRamBodySkeleton.SetAnimationAtChannel("batteringram_idle", 0, 1f, 0f);
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.IsDeactivated = !standingPoint.GameEntity.HasTag("move");
		}
	}

	public override void WriteToNetwork()
	{
		base.WriteToNetwork();
		GameNetworkMessage.WriteBoolToPacket(HasArrivedAtTarget);
		GameNetworkMessage.WriteIntToPacket((int)State, CompressionMission.BatteringRamStateCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(MovementComponent.GetTotalDistanceTraveledForPathTracker(), CompressionBasic.PositionCompressionInfo);
	}

	public void HighlightPath()
	{
		MovementComponent.HighlightPath();
	}

	public void SwitchGhostEntityMovementMode(bool isGhostEnabled)
	{
		if (isGhostEnabled)
		{
			if (!_isGhostMovementOn)
			{
				RemoveComponent(MovementComponent);
				SetUpGhostEntity();
				GhostEntityMove = true;
				SiegeWeaponMovementComponent component = GetComponent<SiegeWeaponMovementComponent>();
				component.GhostEntitySpeedMultiplier *= 3f;
				component.SetGhostVisibility(isVisible: true);
			}
			_isGhostMovementOn = true;
		}
		else
		{
			if (_isGhostMovementOn)
			{
				RemoveComponent(MovementComponent);
				PathLastNodeFixer component2 = GetComponent<PathLastNodeFixer>();
				RemoveComponent(component2);
				AddRegularMovementComponent();
				MovementComponent.SetGhostVisibility(isVisible: false);
			}
			_isGhostMovementOn = false;
		}
	}

	private void SetUpGhostEntity()
	{
		PathLastNodeFixer component = new PathLastNodeFixer
		{
			PathHolder = this
		};
		AddComponent(component);
		MovementComponent = new SiegeWeaponMovementComponent
		{
			PathEntityName = PathEntity,
			MainObject = this,
			GhostEntitySpeedMultiplier = GhostEntitySpeedMultiplier
		};
		AddComponent(MovementComponent);
		MovementComponent.SetupGhostEntity();
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=MaBSSg7I}Battering Ram");
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject obj = (usableGameObject.GameEntity.HasTag("pull") ? new TextObject("{=1cnJtNTt}{KEY} Pull") : new TextObject("{=rwZAZSvX}{KEY} Move"));
		obj.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return obj;
	}

	public override OrderType GetOrder(BattleSideEnum side)
	{
		if (base.IsDestroyed)
		{
			return OrderType.None;
		}
		if (side == BattleSideEnum.Attacker)
		{
			if (!HasCompletedAction())
			{
				return OrderType.FollowEntity;
			}
			return OrderType.Use;
		}
		return OrderType.AttackEntity;
	}

	public override TargetFlags GetTargetFlags()
	{
		TargetFlags targetFlags = TargetFlags.None;
		if (base.UserCountNotInStruckAction > 0)
		{
			targetFlags |= TargetFlags.IsMoving;
		}
		targetFlags |= TargetFlags.IsSiegeEngine;
		targetFlags |= TargetFlags.IsAttacker;
		if (Side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToRam)
		{
			targetFlags |= TargetFlags.DebugThreat;
		}
		if (HasCompletedAction() || base.IsDestroyed || IsDeactivated)
		{
			targetFlags |= TargetFlags.NotAThreat;
		}
		return targetFlags;
	}

	public override float GetTargetValue(List<Vec3> weaponPos)
	{
		return 300f * GetUserMultiplierOfWeapon() * GetDistanceMultiplierOfWeapon(weaponPos[0]) * GetHitPointMultiplierOfWeapon();
	}

	protected override float GetDistanceMultiplierOfWeapon(Vec3 weaponPos)
	{
		float minimumDistanceBetweenPositions = GetMinimumDistanceBetweenPositions(weaponPos);
		if (minimumDistanceBetweenPositions < 100f)
		{
			return 1f;
		}
		if (minimumDistanceBetweenPositions < 625f)
		{
			return 0.8f;
		}
		return 0.6f;
	}

	public void SetSpawnedFromSpawner()
	{
		_spawnedFromSpawner = true;
	}

	public void AssignParametersFromSpawner(string gateTag, string sideTag, int bridgeNavMeshID1, int bridgeNavMeshID2, int ditchNavMeshID1, int ditchNavMeshID2, int groundToBridgeNavMeshID1, int groundToBridgeNavMeshID2, string pathEntityName)
	{
		_gateTag = gateTag;
		_sideTag = sideTag;
		_bridgeNavMeshID1 = bridgeNavMeshID1;
		_bridgeNavMeshID2 = bridgeNavMeshID2;
		_ditchNavMeshID1 = ditchNavMeshID1;
		_ditchNavMeshID2 = ditchNavMeshID2;
		_groundToBridgeNavMeshID1 = groundToBridgeNavMeshID1;
		_groundToBridgeNavMeshID2 = groundToBridgeNavMeshID2;
		_pathEntityName = pathEntityName;
	}

	public override void OnAfterReadFromNetwork((BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord, bool allowVisibilityUpdate = true)
	{
		base.OnAfterReadFromNetwork(synchedMissionObjectReadableRecord, allowVisibilityUpdate);
		BatteringRamRecord batteringRamRecord = (BatteringRamRecord)(object)synchedMissionObjectReadableRecord.Item2;
		HasArrivedAtTarget = batteringRamRecord.HasArrivedAtTarget;
		_state = (RamState)batteringRamRecord.State;
		float totalDistanceTraveled = batteringRamRecord.TotalDistanceTraveled;
		totalDistanceTraveled += 0.05f;
		MovementComponent.SetTotalDistanceTraveledForPathTracker(totalDistanceTraveled);
		MovementComponent.SetTargetFrameForPathTracker();
	}

	public bool GetNavmeshFaceIds(out List<int> navmeshFaceIds)
	{
		navmeshFaceIds = null;
		return false;
	}
}
