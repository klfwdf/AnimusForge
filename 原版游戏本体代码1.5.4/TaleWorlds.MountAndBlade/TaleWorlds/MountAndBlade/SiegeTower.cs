using System;
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

public class SiegeTower : SiegeWeapon, IPathHolder, IPrimarySiegeWeapon, IMoveableSiegeWeapon, ISpawnable
{
	[DefineSynchedMissionObjectType(typeof(SiegeTower))]
	public struct SiegeTowerRecord : ISynchedMissionObjectReadableRecord
	{
		public bool HasArrivedAtTarget { get; private set; }

		public int State { get; private set; }

		public float FallAngularSpeed { get; private set; }

		public float TotalDistanceTraveled { get; private set; }

		public bool ReadFromNetwork(ref bool bufferReadValid)
		{
			HasArrivedAtTarget = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			State = GameNetworkMessage.ReadIntFromPacket(CompressionMission.SiegeTowerGateStateCompressionInfo, ref bufferReadValid);
			FallAngularSpeed = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.SiegeMachineComponentAngularSpeedCompressionInfo, ref bufferReadValid);
			TotalDistanceTraveled = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}
	}

	public enum GateState
	{
		Closed,
		Open,
		GateFalling,
		GateFallingWallDestroyed,
		NumberOfStates
	}

	private const int LeftLadderNavMeshIdLocal = 5;

	private const int MiddleLadderNavMeshIdLocal = 6;

	private const int RightLadderNavMeshIdLocal = 7;

	private const string BreakableWallTag = "breakable_wall";

	private const string DestroyedWallTag = "destroyed";

	private const string NonDestroyedWallTag = "non_destroyed";

	private const string LadderTag = "ladder";

	private const string BattlementDestroyedParticleTag = "particle_spawnpoint";

	public string GateTag = "gate";

	public string GateOpenTag = "gateOpen";

	public string HandleTag = "handle";

	public string GateHandleIdleAnimation = "siegetower_handle_idle";

	private int _gateHandleIdleAnimationIndex = -1;

	public string GateTrembleAnimation = "siegetower_door_stop";

	private int _gateTrembleAnimationIndex = -1;

	public string BattlementDestroyedParticle = "psys_adobe_battlement_destroyed";

	private string _targetWallSegmentTag;

	public bool GhostEntityMove = true;

	public float GhostEntitySpeedMultiplier = 1f;

	private string _sideTag;

	private bool _hasLadders;

	public float WheelDiameter = 1.3f;

	public float MinSpeed = 0.5f;

	public float MaxSpeed = 1f;

	public int GateNavMeshId;

	public int NavMeshIdToDisableOnDestination = -1;

	private int _soilNavMeshID1;

	private int _soilNavMeshID2;

	private int _ditchNavMeshID1;

	private int _ditchNavMeshID2;

	private int _groundToSoilNavMeshID1;

	private int _groundToSoilNavMeshID2;

	private int _soilGenericNavMeshID;

	private int _groundGenericNavMeshID;

	public string BarrierTagToRemove = "barrier";

	private List<GameEntity> _aiBarriers;

	private bool _isGhostMovementOn;

	private bool _hasArrivedAtTarget;

	private GateState _state;

	private SynchedMissionObject _gateObject;

	private SynchedMissionObject _handleObject;

	private SoundEvent _gateOpenSound;

	private int _gateOpenSoundIndex = -1;

	private Mat3 _openStateRotation;

	private Mat3 _closedStateRotation;

	private float _fallAngularSpeed;

	private GameEntity _cleanState;

	private GameEntity _destroyedWallEntity;

	private GameEntity _nonDestroyedWallEntity;

	private GameEntity _battlementDestroyedParticle;

	private StandingPoint _gateStandingPoint;

	private MatrixFrame _gateStandingPointLocalIKFrame;

	private SynchedMissionObject _ditchFillDebris;

	private List<LadderQueueManager> _queueManagers;

	private WallSegment _targetWallSegment;

	private List<SiegeLadder> _sameSideSiegeLadders;

	public MissionObject TargetCastlePosition => _targetWallSegment;

	private WeakGameEntity CleanState
	{
		get
		{
			if (!(_cleanState == null))
			{
				return _cleanState.WeakEntity;
			}
			return base.GameEntity;
		}
	}

	public FormationAI.BehaviorSide WeaponSide { get; private set; }

	public string PathEntity { get; private set; }

	public bool EditorGhostEntityMove => GhostEntityMove;

	public float SiegeWeaponPriority => 20f;

	public int OverTheWallNavMeshID => GetGateNavMeshId();

	public SiegeWeaponMovementComponent MovementComponent { get; private set; }

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
				MovementComponent.SetDestinationNavMeshIdState(!HasArrivedAtTarget);
			}
			if (_hasArrivedAtTarget == value)
			{
				return;
			}
			_hasArrivedAtTarget = value;
			if (_hasArrivedAtTarget)
			{
				ActiveWaitStandingPoint = base.WaitStandingPoints[1];
				if (!GameNetwork.IsClientOrReplay)
				{
					foreach (LadderQueueManager queueManager in _queueManagers)
					{
						CleanState.Scene.SetAbilityOfFacesWithId(queueManager.ManagedNavigationFaceId, isEnabled: true);
						queueManager.Activate();
					}
				}
			}
			else if (!GameNetwork.IsClientOrReplay && GetGateNavMeshId() > 0)
			{
				CleanState.Scene.SetAbilityOfFacesWithId(GetGateNavMeshId(), isEnabled: false);
			}
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetSiegeTowerHasArrivedAtTarget(base.Id));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			else if (GameNetwork.IsClientOrReplay)
			{
				MovementComponent.MoveToTargetAsClient();
			}
		}
	}

	public GateState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (_state != value)
			{
				if (GameNetwork.IsServerOrRecorder)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SetSiegeTowerGateState(base.Id, value));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
				}
				_state = value;
				OnSiegeTowerGateStateChange();
			}
		}
	}

	public override bool IsDeactivated
	{
		get
		{
			if (!MovementComponent.HasArrivedAtTarget || State != GateState.Open)
			{
				return base.IsDeactivated;
			}
			return true;
		}
	}

	public bool HasCompletedAction()
	{
		if (!base.IsDisabled && IsDeactivated && _hasArrivedAtTarget)
		{
			return !base.IsDestroyed;
		}
		return false;
	}

	public int GetGateNavMeshId()
	{
		if (GateNavMeshId == 0)
		{
			if (DynamicNavmeshIdStart == 0)
			{
				return 0;
			}
			return DynamicNavmeshIdStart + 3;
		}
		return GateNavMeshId;
	}

	public List<int> CollectGetDifficultNavmeshIDs()
	{
		List<int> list = new List<int>();
		if (!_hasLadders)
		{
			return list;
		}
		list.Add(DynamicNavmeshIdStart + 1);
		list.Add(DynamicNavmeshIdStart + 5);
		list.Add(DynamicNavmeshIdStart + 6);
		list.Add(DynamicNavmeshIdStart + 7);
		return list;
	}

	public List<int> CollectGetDifficultNavmeshIDsForAttackers()
	{
		List<int> result = new List<int>();
		if (!_hasLadders)
		{
			return result;
		}
		result = CollectGetDifficultNavmeshIDs();
		result.Add(DynamicNavmeshIdStart + 3);
		return result;
	}

	public List<int> CollectGetDifficultNavmeshIDsForDefenders()
	{
		List<int> result = new List<int>();
		if (!_hasLadders)
		{
			return result;
		}
		result = CollectGetDifficultNavmeshIDs();
		result.Add(DynamicNavmeshIdStart + 2);
		return result;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if (!gameEntity.IsValid || !gameEntity.HasScriptOfType<UsableMissionObject>() || gameEntity.HasTag("move"))
		{
			return new TextObject("{=aXjlMBiE}Siege Tower");
		}
		return new TextObject("{=6wZUG0ev}Gate");
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject obj = (usableGameObject.GameEntity.HasTag("move") ? new TextObject("{=rwZAZSvX}{KEY} Move") : new TextObject("{=5oozsaIb}{KEY} Open"));
		obj.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return obj;
	}

	public override void WriteToNetwork()
	{
		base.WriteToNetwork();
		GameNetworkMessage.WriteBoolToPacket(HasArrivedAtTarget);
		GameNetworkMessage.WriteIntToPacket((int)State, CompressionMission.SiegeTowerGateStateCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(_fallAngularSpeed, CompressionMission.SiegeMachineComponentAngularSpeedCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(MovementComponent.GetTotalDistanceTraveledForPathTracker(), CompressionBasic.PositionCompressionInfo);
	}

	public override OrderType GetOrder(BattleSideEnum side)
	{
		if (base.IsDestroyed)
		{
			return OrderType.None;
		}
		if (side == BattleSideEnum.Attacker)
		{
			if (HasCompletedAction())
			{
				return OrderType.Use;
			}
			return OrderType.FollowEntity;
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
		if (HasCompletedAction() || base.IsDestroyed || IsDeactivated)
		{
			targetFlags |= TargetFlags.NotAThreat;
		}
		if (Side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToTower)
		{
			targetFlags |= TargetFlags.DebugThreat;
		}
		return targetFlags | TargetFlags.IsSiegeTower;
	}

	public override float GetTargetValue(List<Vec3> weaponPos)
	{
		return 90f * GetUserMultiplierOfWeapon() * GetDistanceMultiplierOfWeapon(weaponPos[0]) * GetHitPointMultiplierOfWeapon();
	}

	public override void Disable()
	{
		base.Disable();
		SetAbilityOfFaces(enabled: false);
		if (_queueManagers == null)
		{
			return;
		}
		foreach (LadderQueueManager queueManager in _queueManagers)
		{
			CleanState.Scene.SetAbilityOfFacesWithId(queueManager.ManagedNavigationFaceId, isEnabled: false);
			queueManager.DeactivateImmediate();
		}
	}

	public override SiegeEngineType GetSiegeEngineType()
	{
		return DefaultSiegeEngineTypes.SiegeTower;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new SiegeTowerAI(this);
	}

	protected internal override void OnDeploymentStateChanged(bool isDeployed)
	{
		base.OnDeploymentStateChanged(isDeployed);
		if (_ditchFillDebris != null)
		{
			if (!GameNetwork.IsClientOrReplay)
			{
				_ditchFillDebris.SetVisibleSynched(isDeployed);
			}
			if (!GameNetwork.IsClientOrReplay)
			{
				if (isDeployed)
				{
					if (_soilGenericNavMeshID > 0)
					{
						Mission.Current.Scene.SetAbilityOfFacesWithId(_soilGenericNavMeshID, isEnabled: true);
					}
					if (_soilNavMeshID1 > 0 && _groundToSoilNavMeshID1 > 0 && _ditchNavMeshID1 > 0)
					{
						Mission.Current.Scene.SetAbilityOfFacesWithId(_soilNavMeshID1, isEnabled: true);
						Mission.Current.Scene.SwapFaceConnectionsWithID(_groundToSoilNavMeshID1, _ditchNavMeshID1, _soilNavMeshID1, canFail: false);
					}
					if (_soilNavMeshID2 > 0 && _groundToSoilNavMeshID2 > 0 && _ditchNavMeshID2 > 0)
					{
						Mission.Current.Scene.SetAbilityOfFacesWithId(_soilNavMeshID2, isEnabled: true);
						Mission.Current.Scene.SwapFaceConnectionsWithID(_groundToSoilNavMeshID2, _ditchNavMeshID2, _soilNavMeshID2, canFail: false);
					}
					if (_groundGenericNavMeshID > 0)
					{
						Mission.Current.Scene.SetAbilityOfFacesWithId(_groundGenericNavMeshID, isEnabled: false);
					}
				}
				else
				{
					if (_groundGenericNavMeshID > 0)
					{
						Mission.Current.Scene.SetAbilityOfFacesWithId(_groundGenericNavMeshID, isEnabled: true);
					}
					if (_soilNavMeshID1 > 0 && _groundToSoilNavMeshID1 > 0 && _ditchNavMeshID1 > 0)
					{
						Mission.Current.Scene.SwapFaceConnectionsWithID(_groundToSoilNavMeshID1, _soilNavMeshID1, _ditchNavMeshID1, canFail: false);
						Mission.Current.Scene.SetAbilityOfFacesWithId(_soilNavMeshID1, isEnabled: false);
					}
					if (_soilNavMeshID2 > 0 && _groundToSoilNavMeshID2 > 0 && _ditchNavMeshID2 > 0)
					{
						Mission.Current.Scene.SwapFaceConnectionsWithID(_groundToSoilNavMeshID2, _soilNavMeshID2, _ditchNavMeshID2, canFail: false);
						Mission.Current.Scene.SetAbilityOfFacesWithId(_soilNavMeshID2, isEnabled: false);
					}
					if (_soilGenericNavMeshID > 0)
					{
						Mission.Current.Scene.SetAbilityOfFacesWithId(_soilGenericNavMeshID, isEnabled: false);
					}
				}
			}
		}
		if (_sameSideSiegeLadders == null)
		{
			_sameSideSiegeLadders = (from sl in Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeLadder>()
				where sl.WeaponSide == WeaponSide
				select sl).ToList();
		}
		foreach (SiegeLadder sameSideSiegeLadder in _sameSideSiegeLadders)
		{
			sameSideSiegeLadder.GameEntity.SetVisibilityExcludeParents(!isDeployed);
		}
	}

	protected override void AttachDynamicNavmeshToEntity()
	{
		if (NavMeshPrefabName.Length > 0)
		{
			DynamicNavmeshIdStart = Mission.Current.GetNextDynamicNavMeshIdStart();
			CleanState.Scene.ImportNavigationMeshPrefab(NavMeshPrefabName, DynamicNavmeshIdStart);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 1, isConnected: false);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 2, isConnected: true);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 4, isConnected: false, isBlocker: true, autoLocalize: false, finalizeBlockerConvexHullComputation: true);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 5, isConnected: false);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 6, isConnected: false);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 7, isConnected: false);
		}
	}

	protected override WeakGameEntity GetEntityToAttachNavMeshFaces()
	{
		return CleanState;
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		MovementComponent?.OnRemoved();
	}

	public override void SetAbilityOfFaces(bool enabled)
	{
		base.SetAbilityOfFaces(enabled);
		if (_queueManagers == null)
		{
			return;
		}
		foreach (LadderQueueManager queueManager in _queueManagers)
		{
			CleanState.Scene.SetAbilityOfFacesWithId(queueManager.ManagedNavigationFaceId, enabled);
			if (queueManager.IsDeactivated != !enabled)
			{
				if (enabled)
				{
					queueManager.Activate();
				}
				else
				{
					queueManager.DeactivateImmediate();
				}
			}
		}
	}

	protected override float GetDistanceMultiplierOfWeapon(Vec3 weaponPos)
	{
		float minimumDistanceBetweenPositions = GetMinimumDistanceBetweenPositions(weaponPos);
		if (minimumDistanceBetweenPositions < 10f)
		{
			return 1f;
		}
		if (minimumDistanceBetweenPositions < 25f)
		{
			return 0.8f;
		}
		return 0.6f;
	}

	private bool IsNavmeshOnThisTowerAttackerDifficultNavmeshIDs(int testedNavmeshID)
	{
		if (_hasLadders)
		{
			if (testedNavmeshID != DynamicNavmeshIdStart + 1 && testedNavmeshID != DynamicNavmeshIdStart + 5 && testedNavmeshID != DynamicNavmeshIdStart + 6 && testedNavmeshID != DynamicNavmeshIdStart + 7)
			{
				return testedNavmeshID == DynamicNavmeshIdStart + 3;
			}
			return true;
		}
		return false;
	}

	protected override bool IsAgentOnInconvenientNavmesh(Agent agent, StandingPoint standingPoint)
	{
		if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.Siege)
		{
			return false;
		}
		int currentNavigationFaceId = agent.GetCurrentNavigationFaceId();
		if (agent.Team.TeamAI is TeamAISiegeComponent teamAISiegeComponent)
		{
			if (teamAISiegeComponent is TeamAISiegeDefender && currentNavigationFaceId % 10 != 1)
			{
				return true;
			}
			foreach (int difficultNavmeshID in teamAISiegeComponent.DifficultNavmeshIDs)
			{
				if (currentNavigationFaceId == difficultNavmeshID)
				{
					return standingPoint != _gateStandingPoint || !IsNavmeshOnThisTowerAttackerDifficultNavmeshIDs(currentNavigationFaceId);
				}
			}
			if (teamAISiegeComponent is TeamAISiegeAttacker && currentNavigationFaceId % 10 == 1)
			{
				return true;
			}
		}
		return false;
	}

	protected internal override void OnInit()
	{
		_cleanState = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("body"));
		base.OnInit();
		base.DestructionComponent.OnDestroyed += OnDestroyed;
		base.DestructionComponent.BattleSide = BattleSideEnum.Attacker;
		_aiBarriers = base.Scene.FindEntitiesWithTag(BarrierTagToRemove).ToList();
		if (!GameNetwork.IsClientOrReplay && _soilGenericNavMeshID > 0)
		{
			CleanState.Scene.SetAbilityOfFacesWithId(_soilGenericNavMeshID, isEnabled: false);
		}
		List<SynchedMissionObject> list = CleanState.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(GateTag);
		if (list.Count > 0)
		{
			_gateObject = list[0];
		}
		AddRegularMovementComponent();
		List<GameEntity> list2 = base.Scene.FindEntitiesWithTag("breakable_wall").ToList();
		if (!list2.IsEmpty())
		{
			float num = 10000000f;
			GameEntity entity = null;
			MatrixFrame targetFrame = MovementComponent.GetTargetFrame();
			foreach (GameEntity item in list2)
			{
				float lengthSquared = (item.GlobalPosition - targetFrame.origin).LengthSquared;
				if (lengthSquared < num)
				{
					num = lengthSquared;
					entity = item;
				}
			}
			list2 = entity.CollectChildrenEntitiesWithTag("destroyed");
			if (list2.Count > 0)
			{
				_destroyedWallEntity = list2[0];
			}
			list2 = entity.CollectChildrenEntitiesWithTag("non_destroyed");
			if (list2.Count > 0)
			{
				_nonDestroyedWallEntity = list2[0];
			}
			list2 = entity.CollectChildrenEntitiesWithTag("particle_spawnpoint");
			if (list2.Count > 0)
			{
				_battlementDestroyedParticle = list2[0];
			}
		}
		list = CleanState.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(HandleTag);
		_handleObject = ((list.Count < 1) ? null : list[0]);
		_gateHandleIdleAnimationIndex = MBAnimation.GetAnimationIndexWithName(GateHandleIdleAnimation);
		_gateTrembleAnimationIndex = MBAnimation.GetAnimationIndexWithName(GateTrembleAnimation);
		_queueManagers = new List<LadderQueueManager>();
		if (!GameNetwork.IsClientOrReplay)
		{
			List<WeakGameEntity> list3 = CleanState.CollectChildrenEntitiesWithTag("ladder");
			if (list3.Count > 0)
			{
				_hasLadders = true;
				WeakGameEntity weakGameEntity = list3.ElementAt(list3.Count / 2);
				foreach (WeakGameEntity item2 in list3)
				{
					if (item2.Name.Contains("middle"))
					{
						weakGameEntity = item2;
						continue;
					}
					LadderQueueManager firstScriptOfType = item2.GetFirstScriptOfType<LadderQueueManager>();
					firstScriptOfType.Initialize(-1, MatrixFrame.Identity, Vec3.Zero, BattleSideEnum.None, int.MaxValue, 1f, 5f, 5f, 5f, 0f, blockUsage: false, 1f, 0f, 0f, doesManageMultipleIDs: false, -1, -1, int.MaxValue, int.MaxValue);
					firstScriptOfType.DeactivateImmediate();
				}
				int num2 = 0;
				int num3 = 1;
				for (int num4 = base.GameEntity.Name.Length - 1; num4 >= 0; num4--)
				{
					if (char.IsDigit(base.GameEntity.Name[num4]))
					{
						num2 += (base.GameEntity.Name[num4] - 48) * num3;
						num3 *= 10;
					}
					else if (num2 > 0)
					{
						break;
					}
				}
				LadderQueueManager firstScriptOfType2 = weakGameEntity.GetFirstScriptOfType<LadderQueueManager>();
				if (firstScriptOfType2 != null)
				{
					MatrixFrame identity = MatrixFrame.Identity;
					identity.rotation.RotateAboutSide(System.MathF.PI / 2f);
					identity.rotation.RotateAboutForward(System.MathF.PI / 8f);
					firstScriptOfType2.Initialize(DynamicNavmeshIdStart + 5, identity, new Vec3(0f, 0f, 1f), BattleSideEnum.Attacker, list3.Count * 2, System.MathF.PI / 4f, 2f, 1f, 4f, 3f, blockUsage: false, 0.8f, (float)num2 * 2f / 5f, 5f, list3.Count > 1, DynamicNavmeshIdStart + 6, DynamicNavmeshIdStart + 7, num2 * TaleWorlds.Library.MathF.Round((float)list3.Count * 0.666f), list3.Count + 1);
					_queueManagers.Add(firstScriptOfType2);
				}
				base.GameEntity.Scene.MarkFacesWithIdAsLadder(5, isLadder: true);
				base.GameEntity.Scene.MarkFacesWithIdAsLadder(6, isLadder: true);
				base.GameEntity.Scene.MarkFacesWithIdAsLadder(7, isLadder: true);
			}
			else
			{
				_hasLadders = false;
				LadderQueueManager firstScriptOfType3 = CleanState.GetFirstScriptOfType<LadderQueueManager>();
				if (firstScriptOfType3 != null)
				{
					MatrixFrame identity2 = MatrixFrame.Identity;
					identity2.origin.y += 4f;
					identity2.rotation.RotateAboutSide(-System.MathF.PI / 2f);
					identity2.rotation.RotateAboutUp(System.MathF.PI);
					firstScriptOfType3.Initialize(DynamicNavmeshIdStart + 2, identity2, new Vec3(0f, -1f), BattleSideEnum.Attacker, 15, System.MathF.PI / 4f, 2f, 1f, 3f, 1f, blockUsage: false, 0.8f, 4f, 5f, doesManageMultipleIDs: false, -2, -2, int.MaxValue, 15);
					_queueManagers.Add(firstScriptOfType3);
				}
			}
		}
		_state = GateState.Closed;
		_gateOpenSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/siegetower/dooropen");
		_closedStateRotation = _gateObject.GameEntity.GetFrame().rotation;
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
			if (!standingPoint.GameEntity.HasTag("move"))
			{
				_gateStandingPoint = standingPoint;
				standingPoint.IsDeactivated = true;
				_gateStandingPointLocalIKFrame = standingPoint.GameEntity.GetGlobalFrame().TransformToLocal(CleanState.GetGlobalFrame());
				standingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
			}
		}
		if (base.WaitStandingPoints[0].GlobalPosition.z > base.WaitStandingPoints[1].GlobalPosition.z)
		{
			List<GameEntity> waitStandingPoints = base.WaitStandingPoints;
			List<GameEntity> waitStandingPoints2 = base.WaitStandingPoints;
			GameEntity gameEntity = base.WaitStandingPoints[1];
			GameEntity gameEntity2 = base.WaitStandingPoints[0];
			GameEntity gameEntity3 = (waitStandingPoints[0] = gameEntity);
			gameEntity3 = (waitStandingPoints2[1] = gameEntity2);
			ActiveWaitStandingPoint = base.WaitStandingPoints[0];
		}
		IEnumerable<WeakGameEntity> source = from weakGameEntity3 in base.Scene.FindWeakEntitiesWithTag(_targetWallSegmentTag).ToList()
			where weakGameEntity3.HasScriptOfType<WallSegment>()
			select weakGameEntity3;
		if (!source.IsEmpty())
		{
			_targetWallSegment = source.First().GetFirstScriptOfType<WallSegment>();
			_targetWallSegment.AttackerSiegeWeapon = this;
		}
		switch (_sideTag)
		{
		case "left":
			WeaponSide = FormationAI.BehaviorSide.Left;
			break;
		case "middle":
			WeaponSide = FormationAI.BehaviorSide.Middle;
			break;
		case "right":
			WeaponSide = FormationAI.BehaviorSide.Right;
			break;
		default:
			WeaponSide = FormationAI.BehaviorSide.Middle;
			break;
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			if (GetGateNavMeshId() != 0)
			{
				CleanState.Scene.SetAbilityOfFacesWithId(GetGateNavMeshId(), isEnabled: false);
			}
			foreach (LadderQueueManager queueManager in _queueManagers)
			{
				CleanState.Scene.SetAbilityOfFacesWithId(queueManager.ManagedNavigationFaceId, isEnabled: false);
				queueManager.DeactivateImmediate();
			}
		}
		WeakGameEntity weakGameEntity2 = base.Scene.FindWeakEntitiesWithTag("ditch_filler").FirstOrDefault((WeakGameEntity df) => df.HasTag(_sideTag));
		if (weakGameEntity2 != null)
		{
			_ditchFillDebris = weakGameEntity2.GetFirstScriptOfType<SynchedMissionObject>();
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			_gateObject.GameEntity.AttachNavigationMeshFaces(DynamicNavmeshIdStart + 3, isConnected: true);
		}
		SetScriptComponentToTick(GetTickRequirement());
		Mission.Current.AddToWeaponListForFriendlyFirePreventing(this);
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			return base.GetTickRequirement() | TickRequirement.Tick | TickRequirement.TickParallel;
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!CleanState.IsVisibleIncludeParents())
		{
			return;
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				if (standingPoint.GameEntity.HasTag("move"))
				{
					standingPoint.SetIsDeactivatedSynched(MovementComponent.HasArrivedAtTarget);
				}
				else
				{
					standingPoint.SetIsDeactivatedSynched(!MovementComponent.HasArrivedAtTarget || State == GateState.Open || ((State == GateState.GateFalling || State == GateState.GateFallingWallDestroyed) && (standingPoint.UserAgent?.IsPlayerControlled ?? false)));
				}
			}
		}
		if (!GameNetwork.IsClientOrReplay && MovementComponent.HasArrivedAtTarget && !HasArrivedAtTarget)
		{
			HasArrivedAtTarget = true;
			ActiveWaitStandingPoint = base.WaitStandingPoints[1];
		}
		if (!HasArrivedAtTarget)
		{
			return;
		}
		switch (State)
		{
		case GateState.GateFalling:
		{
			MatrixFrame frame2 = _gateObject.GameEntity.GetFrame();
			frame2.rotation.RotateAboutSide(_fallAngularSpeed * dt);
			_gateObject.GameEntity.SetFrame(ref frame2);
			if (Vec3.DotProduct(frame2.rotation.u, _openStateRotation.f) < 0.025f)
			{
				State = GateState.GateFallingWallDestroyed;
			}
			_fallAngularSpeed += dt * 2f * TaleWorlds.Library.MathF.Max(0.3f, 1f - frame2.rotation.u.z);
			break;
		}
		case GateState.GateFallingWallDestroyed:
		{
			MatrixFrame frame = _gateObject.GameEntity.GetFrame();
			frame.rotation.RotateAboutSide(_fallAngularSpeed * dt);
			_gateObject.GameEntity.SetFrame(ref frame);
			float num = Vec3.DotProduct(frame.rotation.u, _openStateRotation.f);
			if (_fallAngularSpeed > 0f && num < 0.05f)
			{
				frame.rotation = _openStateRotation;
				_gateObject.GameEntity.SetFrame(ref frame);
				_gateObject.GameEntity.Skeleton.SetAnimationAtChannel(_gateTrembleAnimationIndex, 0);
				_gateOpenSound?.Stop();
				if (!GameNetwork.IsClientOrReplay)
				{
					State = GateState.Open;
				}
			}
			_fallAngularSpeed += dt * 3f * TaleWorlds.Library.MathF.Max(0.3f, 1f - frame.rotation.u.z);
			break;
		}
		case GateState.Closed:
			if (!GameNetwork.IsClientOrReplay && base.UserCountNotInStruckAction > 0)
			{
				State = GateState.GateFalling;
			}
			break;
		default:
			Debug.FailedAssert("Invalid gate state.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\SiegeTower.cs", "OnTick", 960);
			break;
		case GateState.Open:
			break;
		}
	}

	protected internal override void OnTickParallel(float dt)
	{
		base.OnTickParallel(dt);
		if (!CleanState.IsVisibleIncludeParents())
		{
			return;
		}
		MovementComponent.TickParallelManually(dt);
		if (_gateStandingPoint.HasUser)
		{
			Agent userAgent = _gateStandingPoint.UserAgent;
			if (userAgent.IsInBeingStruckAction)
			{
				userAgent.ClearHandInverseKinematics();
			}
			else
			{
				_gateStandingPoint.UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(in _gateStandingPointLocalIKFrame, CleanState.GetGlobalFrame());
			}
		}
	}

	protected internal override void OnMissionReset()
	{
		base.OnMissionReset();
		if (!GameNetwork.IsClientOrReplay && GetGateNavMeshId() > 0)
		{
			CleanState.Scene.SetAbilityOfFacesWithId(GetGateNavMeshId(), isEnabled: false);
		}
		_state = GateState.Closed;
		_hasArrivedAtTarget = false;
		MatrixFrame frame = _gateObject.GameEntity.GetFrame();
		frame.rotation = _closedStateRotation;
		_handleObject?.GameEntity.Skeleton.SetAnimationAtChannel(-1, 0);
		_gateObject.GameEntity.Skeleton.SetAnimationAtChannel(-1, 0);
		_gateObject.GameEntity.SetFrame(ref frame);
		if (_destroyedWallEntity != null && _nonDestroyedWallEntity != null)
		{
			_nonDestroyedWallEntity.SetVisibilityExcludeParents(visible: false);
			_destroyedWallEntity.SetVisibilityExcludeParents(visible: true);
		}
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.IsDeactivated = !standingPoint.GameEntity.HasTag("move");
		}
	}

	public void OnDestroyed(DestructableComponent destroyedComponent, Agent destroyerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		bool burnAgents = false;
		if (weapon.CurrentUsageItem != null)
		{
			burnAgents = weapon.CurrentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.Burning) && weapon.CurrentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.AffectsArea | WeaponFlags.AffectsAreaBig);
		}
		Mission.Current.KillAgentsOnEntity(destroyedComponent.CurrentState, destroyerAgent, burnAgents);
		foreach (GameEntity aiBarrier in _aiBarriers)
		{
			aiBarrier.SetVisibilityExcludeParents(visible: true);
		}
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
				GhostEntityMove = true;
				MovementComponent.GhostEntitySpeedMultiplier *= 3f;
				MovementComponent.SetGhostVisibility(isVisible: true);
			}
			_isGhostMovementOn = true;
			return;
		}
		if (_isGhostMovementOn)
		{
			RemoveComponent(MovementComponent);
			PathLastNodeFixer component = GetComponent<PathLastNodeFixer>();
			RemoveComponent(component);
			AddRegularMovementComponent();
			MovementComponent.SetGhostVisibility(isVisible: false);
		}
		_isGhostMovementOn = false;
	}

	public MatrixFrame GetInitialFrame()
	{
		return MovementComponent?.GetInitialFrame() ?? CleanState.GetGlobalFrame();
	}

	private void OnSiegeTowerGateStateChange()
	{
		switch (State)
		{
		case GateState.GateFalling:
			_fallAngularSpeed = 0f;
			_gateOpenSound = SoundEvent.CreateEvent(_gateOpenSoundIndex, base.Scene);
			_gateOpenSound.PlayInPosition(_gateObject.GameEntity.GlobalPosition);
			break;
		case GateState.GateFallingWallDestroyed:
			if (_destroyedWallEntity != null && _nonDestroyedWallEntity != null)
			{
				_fallAngularSpeed *= 0.1f;
				_nonDestroyedWallEntity.SetVisibilityExcludeParents(visible: false);
				_destroyedWallEntity.SetVisibilityExcludeParents(visible: true);
				if (_battlementDestroyedParticle != null)
				{
					Mission.Current.AddParticleSystemBurstByName(BattlementDestroyedParticle, _battlementDestroyedParticle.GetGlobalFrame(), synchThroughNetwork: false);
				}
			}
			break;
		case GateState.Closed:
			_handleObject?.GameEntity.Skeleton.SetAnimationAtChannel(_gateHandleIdleAnimationIndex, 0);
			if (!GameNetwork.IsClientOrReplay && GetGateNavMeshId() != 0)
			{
				CleanState.Scene.SetAbilityOfFacesWithId(GetGateNavMeshId(), isEnabled: false);
			}
			break;
		case GateState.Open:
			if (_gateObject.GameEntity.Skeleton.GetAnimationIndexAtChannel(0) != _gateHandleIdleAnimationIndex)
			{
				MatrixFrame frame = _gateObject.GameEntity.GetFrame();
				frame.rotation = _openStateRotation;
				_gateObject.GameEntity.SetFrame(ref frame);
				_gateObject.GameEntity.Skeleton.SetAnimationAtChannel(_gateTrembleAnimationIndex, 0);
				_gateOpenSound?.Stop();
				if (!GameNetwork.IsClientOrReplay && GetGateNavMeshId() != 0)
				{
					CleanState.Scene.SetAbilityOfFacesWithId(GetGateNavMeshId(), isEnabled: true);
				}
			}
			if (!GameNetwork.IsClientOrReplay)
			{
				CleanState.Scene.SetAbilityOfFacesWithId(GetGateNavMeshId(), isEnabled: true);
			}
			{
				foreach (GameEntity aiBarrier in _aiBarriers)
				{
					aiBarrier.SetVisibilityExcludeParents(visible: false);
				}
				break;
			}
		}
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
			MovementSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/siege/siegetower/move"),
			GhostEntitySpeedMultiplier = GhostEntitySpeedMultiplier
		};
		AddComponent(MovementComponent);
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

	private void UpdateGhostEntity()
	{
		WeakGameEntity firstChildEntityWithTag = CleanState.GetFirstChildEntityWithTag("ghost_object");
		if (firstChildEntityWithTag.IsValid && firstChildEntityWithTag.ChildCount > 0)
		{
			MovementComponent.GhostEntitySpeedMultiplier = GhostEntitySpeedMultiplier;
			WeakGameEntity child = firstChildEntityWithTag.GetChild(0);
			MatrixFrame frame = child.GetFrame();
			child.SetFrame(ref frame);
		}
	}

	public void SetSpawnedFromSpawner()
	{
		_spawnedFromSpawner = true;
	}

	public override void OnAfterReadFromNetwork((BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord, bool allowVisibilityUpdate = true)
	{
		base.OnAfterReadFromNetwork(synchedMissionObjectReadableRecord, allowVisibilityUpdate);
		SiegeTowerRecord siegeTowerRecord = (SiegeTowerRecord)(object)synchedMissionObjectReadableRecord.Item2;
		HasArrivedAtTarget = siegeTowerRecord.HasArrivedAtTarget;
		_state = (GateState)siegeTowerRecord.State;
		_fallAngularSpeed = siegeTowerRecord.FallAngularSpeed;
		if (_state == GateState.Open)
		{
			if (_destroyedWallEntity != null && _nonDestroyedWallEntity != null)
			{
				_nonDestroyedWallEntity.SetVisibilityExcludeParents(visible: false);
				_destroyedWallEntity.SetVisibilityExcludeParents(visible: true);
			}
			MatrixFrame frame = _gateObject.GameEntity.GetFrame();
			frame.rotation = _openStateRotation;
			_gateObject.GameEntity.SetFrame(ref frame);
		}
		float totalDistanceTraveled = siegeTowerRecord.TotalDistanceTraveled;
		totalDistanceTraveled += 0.05f;
		MovementComponent.SetTotalDistanceTraveledForPathTracker(totalDistanceTraveled);
		MovementComponent.SetTargetFrameForPathTracker();
	}

	public void AssignParametersFromSpawner(string pathEntityName, string targetWallSegment, string sideTag, int soilNavMeshID1, int soilNavMeshID2, int ditchNavMeshID1, int ditchNavMeshID2, int groundToSoilNavMeshID1, int groundToSoilNavMeshID2, int soilGenericNavMeshID, int groundGenericNavMeshID, Mat3 openStateRotation, string barrierTagToRemove)
	{
		PathEntity = pathEntityName;
		_targetWallSegmentTag = targetWallSegment;
		_sideTag = sideTag;
		_soilNavMeshID1 = soilNavMeshID1;
		_soilNavMeshID2 = soilNavMeshID2;
		_ditchNavMeshID1 = ditchNavMeshID1;
		_ditchNavMeshID2 = ditchNavMeshID2;
		_groundToSoilNavMeshID1 = groundToSoilNavMeshID1;
		_groundToSoilNavMeshID2 = groundToSoilNavMeshID2;
		_soilGenericNavMeshID = soilGenericNavMeshID;
		_groundGenericNavMeshID = groundGenericNavMeshID;
		_openStateRotation = openStateRotation;
		BarrierTagToRemove = barrierTagToRemove;
	}

	public bool GetNavmeshFaceIds(out List<int> navmeshFaceIds)
	{
		navmeshFaceIds = new List<int>
		{
			DynamicNavmeshIdStart + 1,
			DynamicNavmeshIdStart + 3,
			DynamicNavmeshIdStart + 5,
			DynamicNavmeshIdStart + 6,
			DynamicNavmeshIdStart + 7
		};
		return true;
	}

	public void OnFormationFrameChanged(Agent agent, bool hasFrame, WorldPosition frame)
	{
		foreach (LadderQueueManager queueManager in _queueManagers)
		{
			queueManager.OnFormationFrameChanged(agent, hasFrame, frame);
		}
	}
}
