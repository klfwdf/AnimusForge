using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Source.Objects.Siege;

namespace TaleWorlds.MountAndBlade;

public class CastleGate : UsableMachine, IPointDefendable, ICastleKeyPosition, ITargetable
{
	public enum DoorOwnership
	{
		Defenders,
		Attackers
	}

	public enum GateState
	{
		Open,
		Closed
	}

	public const string OuterGateTag = "outer_gate";

	public const string InnerGateTag = "inner_gate";

	private const float ExtraColliderScaleFactor = 1.1f;

	private const string LeftDoorBodyTag = "collider_l";

	private const string RightDoorBodyTag = "collider_r";

	private const string RightDoorAgentOnlyBodyTag = "collider_agent_r";

	private const string OpenTag = "open";

	private const string CloseTag = "close";

	private const string MiddlePositionTag = "middle_pos";

	private const string WaitPositionTag = "wait_pos";

	private const string LeftDoorAgentOnlyBodyTag = "collider_agent_l";

	private const int HeavyBlowDamageLimit = 200;

	private static int _batteringRamHitSoundId = -1;

	public DoorOwnership OwningTeam;

	public string OpeningAnimationName = "castle_gate_a_opening";

	public string ClosingAnimationName = "castle_gate_a_closing";

	public string HitAnimationName = "castle_gate_a_hit";

	public string PlankHitAnimationName = "castle_gate_a_plank_hit";

	public string HitMeleeAnimationName = "castle_gate_a_hit_melee";

	public string DestroyAnimationName = "castle_gate_a_break";

	public int NavigationMeshId = 1000;

	public int NavigationMeshIdToDisableOnOpen = -1;

	public string LeftDoorBoneName = "bn_bottom_l";

	public string RightDoorBoneName = "bn_bottom_r";

	public string ExtraCollisionObjectTagRight = "extra_collider_r";

	public string ExtraCollisionObjectTagLeft = "extra_collider_l";

	private int _openingAnimationIndex = -1;

	private int _closingAnimationIndex = -1;

	private bool _leftExtraColliderDisabled;

	private bool _rightExtraColliderDisabled;

	private bool _civilianMission;

	public bool ActivateExtraColliders = true;

	public string SideTag;

	private bool _openNavMeshIdDisabled;

	private SynchedMissionObject _door;

	private Skeleton _doorSkeleton;

	private GameEntity _extraColliderRight;

	private GameEntity _extraColliderLeft;

	private readonly List<GameEntity> _attackOnlyDoorColliders;

	private float _previousAnimationProgress = -1f;

	private GameEntity _agentColliderRight;

	private GameEntity _agentColliderLeft;

	private LadderQueueManager _queueManager;

	private bool _afterMissionStartTriggered;

	private sbyte _rightDoorBoneIndex;

	private sbyte _leftDoorBoneIndex;

	private AgentPathNavMeshChecker _pathChecker;

	public bool AutoOpen;

	private SynchedMissionObject _plank;

	private WorldFrame _middleFrame;

	private WorldFrame _defenseWaitFrame;

	private Action DestructibleComponentOnMissionReset;

	public TacticalPosition MiddlePosition { get; private set; }

	private static int BatteringRamHitSoundIdCache
	{
		get
		{
			if (_batteringRamHitSoundId == -1)
			{
				_batteringRamHitSoundId = SoundEvent.GetEventIdFromString("event:/mission/siege/door/hit");
			}
			return _batteringRamHitSoundId;
		}
	}

	public TacticalPosition WaitPosition { get; private set; }

	public override FocusableObjectType FocusableObjectType => FocusableObjectType.Gate;

	public GateState State { get; private set; }

	public bool IsGateOpen
	{
		get
		{
			if (State != GateState.Open)
			{
				return base.IsDestroyed;
			}
			return true;
		}
	}

	public IPrimarySiegeWeapon AttackerSiegeWeapon { get; set; }

	public IEnumerable<DefencePoint> DefencePoints { get; protected set; }

	public FormationAI.BehaviorSide DefenseSide { get; private set; }

	public WorldFrame MiddleFrame => _middleFrame;

	public WorldFrame DefenseWaitFrame => _defenseWaitFrame;

	public CastleGate()
	{
		_attackOnlyDoorColliders = new List<GameEntity>();
	}

	public Vec3 GetPosition()
	{
		return base.GameEntity.GlobalPosition;
	}

	public override OrderType GetOrder(BattleSideEnum side)
	{
		if (!base.IsDestroyed)
		{
			if (side != BattleSideEnum.Attacker)
			{
				return OrderType.Use;
			}
			return OrderType.AttackEntity;
		}
		return OrderType.None;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		DestructableComponent firstScriptOfType = base.GameEntity.GetFirstScriptOfType<DestructableComponent>();
		if (firstScriptOfType != null)
		{
			firstScriptOfType.OnNextDestructionState += OnNextDestructionState;
			DestructibleComponentOnMissionReset = firstScriptOfType.OnMissionReset;
			if (!GameNetwork.IsClientOrReplay)
			{
				firstScriptOfType.OnDestroyed += OnDestroyed;
				firstScriptOfType.OnHitTaken += OnHitTaken;
				firstScriptOfType.OnCalculateDestructionStateIndex = (Func<int, int, int, int>)Delegate.Combine(firstScriptOfType.OnCalculateDestructionStateIndex, new Func<int, int, int, int>(OnCalculateDestructionStateIndex));
			}
			firstScriptOfType.BattleSide = BattleSideEnum.Defender;
		}
		CollectGameEntities(calledFromOnInit: true);
		base.GameEntity.SetAnimationSoundActivation(activate: true);
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		_queueManager = base.GameEntity.GetFirstScriptOfType<LadderQueueManager>();
		if (_queueManager == null)
		{
			WeakGameEntity weakGameEntity = base.GameEntity.GetChildren().FirstOrDefault((WeakGameEntity ce) => ce.HasScriptOfType<LadderQueueManager>());
			if (weakGameEntity.IsValid)
			{
				_queueManager = weakGameEntity.GetFirstScriptOfType<LadderQueueManager>();
			}
		}
		if (_queueManager != null)
		{
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin.y -= 2f;
			identity.rotation.RotateAboutSide(-System.MathF.PI / 2f);
			identity.rotation.RotateAboutForward(System.MathF.PI);
			_queueManager.Initialize(_queueManager.ManagedNavigationFaceId, identity, -identity.rotation.u, BattleSideEnum.Defender, 15, System.MathF.PI / 5f, 3f, 2.2f, 0f, 0f, blockUsage: false, 1f, 2.1474836E+09f, 5f, doesManageMultipleIDs: false, -2, -2, int.MaxValue, 15);
			_queueManager.Activate();
		}
		switch (SideTag)
		{
		case "left":
			DefenseSide = FormationAI.BehaviorSide.Left;
			break;
		case "middle":
			DefenseSide = FormationAI.BehaviorSide.Middle;
			break;
		case "right":
			DefenseSide = FormationAI.BehaviorSide.Right;
			break;
		default:
			DefenseSide = FormationAI.BehaviorSide.BehaviorSideNotSet;
			break;
		}
		List<WeakGameEntity> list = base.GameEntity.CollectChildrenEntitiesWithTag("middle_pos");
		if (list.Count > 0)
		{
			WeakGameEntity weakGameEntity2 = list.FirstOrDefault();
			MiddlePosition = weakGameEntity2.GetFirstScriptOfType<TacticalPosition>();
			MatrixFrame globalFrame = weakGameEntity2.GetGlobalFrame();
			_middleFrame = new WorldFrame(globalFrame.rotation, globalFrame.origin.ToWorldPosition());
			_middleFrame.Origin.GetGroundVec3();
		}
		else
		{
			MatrixFrame globalFrame2 = base.GameEntity.GetGlobalFrame();
			_middleFrame = new WorldFrame(globalFrame2.rotation, globalFrame2.origin.ToWorldPosition());
		}
		List<WeakGameEntity> list2 = base.GameEntity.CollectChildrenEntitiesWithTag("wait_pos");
		if (list2.Count > 0)
		{
			WeakGameEntity weakGameEntity3 = list2.FirstOrDefault();
			WaitPosition = weakGameEntity3.GetFirstScriptOfType<TacticalPosition>();
			MatrixFrame globalFrame3 = weakGameEntity3.GetGlobalFrame();
			_defenseWaitFrame = new WorldFrame(globalFrame3.rotation, globalFrame3.origin.ToWorldPosition());
			_defenseWaitFrame.Origin.GetGroundVec3();
		}
		else
		{
			_defenseWaitFrame = _middleFrame;
		}
		_openingAnimationIndex = MBAnimation.GetAnimationIndexWithName(OpeningAnimationName);
		_closingAnimationIndex = MBAnimation.GetAnimationIndexWithName(ClosingAnimationName);
		SetScriptComponentToTick(GetTickRequirement());
		OnCheckForProblems();
	}

	public void SetUsableTeam(Team team)
	{
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (standingPoint is StandingPointWithTeamLimit standingPointWithTeamLimit)
			{
				standingPointWithTeamLimit.UsableTeam = team;
			}
		}
	}

	public override void AfterMissionStart()
	{
		_afterMissionStartTriggered = true;
		base.AfterMissionStart();
		SetInitialStateOfGate();
		InitializeExtraColliderPositions();
		if (!GameNetwork.IsClientOrReplay)
		{
			SetAutoOpenState(Mission.Current.IsSallyOutBattle);
		}
		if (OwningTeam == DoorOwnership.Attackers)
		{
			SetUsableTeam(Mission.Current.AttackerTeam);
		}
		else if (OwningTeam == DoorOwnership.Defenders)
		{
			SetUsableTeam(Mission.Current.DefenderTeam);
		}
		_pathChecker = new AgentPathNavMeshChecker(Mission.Current, base.GameEntity.GetGlobalFrame(), 2f, NavigationMeshId, BattleSideEnum.Defender, AgentPathNavMeshChecker.Direction.BothDirections, 14f, 3f);
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		DestructableComponent firstScriptOfType = base.GameEntity.GetFirstScriptOfType<DestructableComponent>();
		if (firstScriptOfType != null)
		{
			firstScriptOfType.OnNextDestructionState -= OnNextDestructionState;
			if (!GameNetwork.IsClientOrReplay)
			{
				firstScriptOfType.OnDestroyed -= OnDestroyed;
				firstScriptOfType.OnHitTaken -= OnHitTaken;
				firstScriptOfType.OnCalculateDestructionStateIndex = (Func<int, int, int, int>)Delegate.Remove(firstScriptOfType.OnCalculateDestructionStateIndex, new Func<int, int, int, int>(OnCalculateDestructionStateIndex));
			}
		}
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		if (base.GameEntity.HasTag("outer_gate") && base.GameEntity.HasTag("inner_gate"))
		{
			MBDebug.ShowWarning("Castle gate has both the outer gate tag and the inner gate tag.");
		}
	}

	protected internal override void OnMissionReset()
	{
		DestructibleComponentOnMissionReset?.Invoke();
		CollectGameEntities(calledFromOnInit: false);
		base.OnMissionReset();
		SetInitialStateOfGate();
		_previousAnimationProgress = -1f;
	}

	private void SetInitialStateOfGate()
	{
		if (!GameNetwork.IsClientOrReplay && NavigationMeshIdToDisableOnOpen != -1)
		{
			_openNavMeshIdDisabled = false;
			base.Scene.SetAbilityOfFacesWithId(NavigationMeshIdToDisableOnOpen, isEnabled: true);
		}
		if (!_civilianMission)
		{
			_doorSkeleton.SetAnimationAtChannel(_closingAnimationIndex, 0);
			_doorSkeleton.SetAnimationParameterAtChannel(0, 0.99f);
			_doorSkeleton.Freeze(p: false);
			State = GateState.Closed;
			return;
		}
		OpenDoor();
		if (_doorSkeleton != null)
		{
			_door.SetAnimationChannelParameterSynched(0, 1f);
		}
		SetGateNavMeshState(isEnabled: true);
		SetDisabled(isParentObject: true);
		base.GameEntity.GetFirstScriptOfType<DestructableComponent>()?.SetDisabled();
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=6wZUG0ev}Gate");
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		if (!IsDeactivated)
		{
			TextObject textObject = new TextObject(usableGameObject.GameEntity.HasTag("open") ? "{=5oozsaIb}{KEY} Open" : "{=TJj71hPO}{KEY} Close");
			textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			return textObject;
		}
		return TextObject.GetEmpty();
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new CastleGateAI(this);
	}

	public void OpenDoorAndDisableGateForCivilianMission()
	{
		_civilianMission = true;
	}

	public void OpenDoor()
	{
		if (!base.IsDisabled)
		{
			State = GateState.Open;
			if (!AutoOpen)
			{
				SetGateNavMeshState(isEnabled: true);
			}
			else
			{
				SetGateNavMeshStateForEnemies(isEnabled: true);
			}
			int animationIndexAtChannel = _doorSkeleton.GetAnimationIndexAtChannel(0);
			float animationParameterAtChannel = _doorSkeleton.GetAnimationParameterAtChannel(0);
			_door.SetAnimationAtChannelSynched(_openingAnimationIndex, 0);
			if (animationIndexAtChannel == _closingAnimationIndex)
			{
				_door.SetAnimationChannelParameterSynched(0, 1f - animationParameterAtChannel);
			}
			_plank?.SetVisibleSynched(value: false);
		}
	}

	public void CloseDoor()
	{
		if (!base.IsDisabled)
		{
			State = GateState.Closed;
			if (!AutoOpen)
			{
				SetGateNavMeshState(isEnabled: false);
			}
			else
			{
				SetGateNavMeshStateForEnemies(isEnabled: false);
			}
			int animationIndexAtChannel = _doorSkeleton.GetAnimationIndexAtChannel(0);
			float animationParameterAtChannel = _doorSkeleton.GetAnimationParameterAtChannel(0);
			_door.SetAnimationAtChannelSynched(_closingAnimationIndex, 0);
			if (animationIndexAtChannel == _openingAnimationIndex)
			{
				_door.SetAnimationChannelParameterSynched(0, 1f - animationParameterAtChannel);
			}
		}
	}

	private void UpdateDoorBodies(bool updateAnyway)
	{
		if (_attackOnlyDoorColliders.Count == 2)
		{
			float animationParameterAtChannel = _doorSkeleton.GetAnimationParameterAtChannel(0);
			if (!(_previousAnimationProgress != animationParameterAtChannel || updateAnyway))
			{
				return;
			}
			_previousAnimationProgress = animationParameterAtChannel;
			MatrixFrame frame = _doorSkeleton.GetBoneEntitialFrameWithIndex(_leftDoorBoneIndex);
			MatrixFrame frame2 = _doorSkeleton.GetBoneEntitialFrameWithIndex(_rightDoorBoneIndex);
			_attackOnlyDoorColliders[0].SetFrame(ref frame2);
			_attackOnlyDoorColliders[1].SetFrame(ref frame);
			_agentColliderLeft?.SetFrame(ref frame);
			_agentColliderRight?.SetFrame(ref frame2);
			if (!(_extraColliderLeft != null) || !(_extraColliderRight != null))
			{
				return;
			}
			if (State == GateState.Closed)
			{
				if (!_leftExtraColliderDisabled)
				{
					_extraColliderLeft.SetBodyFlags(_extraColliderLeft.BodyFlag | BodyFlags.Disabled);
					_leftExtraColliderDisabled = true;
				}
				if (!_rightExtraColliderDisabled)
				{
					_extraColliderRight.SetBodyFlags(_extraColliderRight.BodyFlag | BodyFlags.Disabled);
					_rightExtraColliderDisabled = true;
				}
				return;
			}
			float num = (frame2.origin - frame.origin).Length * 0.5f;
			float num2 = Vec3.DotProduct(frame2.rotation.s, Vec3.Side) / (frame2.rotation.s.Length * 1f);
			float num3 = TaleWorlds.Library.MathF.Sqrt(1f - num2 * num2);
			float num4 = num * 1.1f;
			float num5 = MBMath.Map(num2, 0.3f, 1f, 0f, 1f) * (num * 0.2f);
			_extraColliderLeft.SetLocalPosition(frame.origin - new Vec3(num4 - num + num5, num * num3));
			_extraColliderRight.SetLocalPosition(frame2.origin - new Vec3(0f - (num4 - num) - num5, num * num3));
			float num6 = 0f;
			if (num2 < 0f)
			{
				num6 = num;
				num6 += num * (0f - num2);
			}
			else
			{
				num6 = num - num * num2;
			}
			num6 = (num4 - num6) / num;
			if (num6 <= 0.0001f)
			{
				if (!_leftExtraColliderDisabled)
				{
					_extraColliderLeft.SetBodyFlags(_extraColliderLeft.BodyFlag | BodyFlags.Disabled);
					_leftExtraColliderDisabled = true;
				}
			}
			else
			{
				if (_leftExtraColliderDisabled)
				{
					_extraColliderLeft.SetBodyFlags((BodyFlags)((uint)_extraColliderLeft.BodyFlag & 0xFFFFFFFEu));
					_leftExtraColliderDisabled = false;
				}
				frame = _extraColliderLeft.GetFrame();
				frame.rotation.Orthonormalize();
				frame.origin -= new Vec3(num4 - num4 * num6);
				_extraColliderLeft.SetFrame(ref frame);
			}
			frame2 = _extraColliderRight.GetFrame();
			frame2.rotation.Orthonormalize();
			float num7 = 0f;
			if (num2 < 0f)
			{
				num7 = num;
				num7 += num * (0f - num2);
			}
			else
			{
				num7 = num - num * num2;
			}
			num7 = (num4 - num7) / num;
			if (num7 <= 0.0001f)
			{
				if (!_rightExtraColliderDisabled)
				{
					_extraColliderRight.SetBodyFlags(_extraColliderRight.BodyFlag | BodyFlags.Disabled);
					_rightExtraColliderDisabled = true;
				}
				return;
			}
			if (_rightExtraColliderDisabled)
			{
				_extraColliderRight.SetBodyFlags((BodyFlags)((uint)_extraColliderRight.BodyFlag & 0xFFFFFFFEu));
				_rightExtraColliderDisabled = false;
			}
			frame2.origin += new Vec3(num4 - num4 * num7);
			_extraColliderRight.SetFrame(ref frame2);
		}
		else if (_attackOnlyDoorColliders.Count == 1)
		{
			MatrixFrame frame3 = _doorSkeleton.GetBoneEntitialFrameWithName(RightDoorBoneName);
			_attackOnlyDoorColliders[0].SetFrame(ref frame3);
			_agentColliderRight?.SetFrame(ref frame3);
		}
	}

	private void SetGateNavMeshState(bool isEnabled)
	{
		if (!GameNetwork.IsClientOrReplay)
		{
			base.Scene.SetAbilityOfFacesWithId(NavigationMeshId, isEnabled);
			if (_queueManager != null)
			{
				_queueManager.Activate();
				base.Scene.SetAbilityOfFacesWithId(_queueManager.ManagedNavigationFaceId, isEnabled);
			}
		}
	}

	private void SetGateNavMeshStateForEnemies(bool isEnabled)
	{
		Team attackerTeam = Mission.Current.AttackerTeam;
		if (attackerTeam == null)
		{
			return;
		}
		foreach (Agent activeAgent in attackerTeam.ActiveAgents)
		{
			if (activeAgent.IsAIControlled)
			{
				activeAgent.SetAgentExcludeStateForFaceGroupId(NavigationMeshId, !isEnabled);
			}
		}
	}

	public void SetAutoOpenState(bool isEnabled)
	{
		AutoOpen = isEnabled;
		if (AutoOpen)
		{
			SetGateNavMeshState(isEnabled: true);
			SetGateNavMeshStateForEnemies(State == GateState.Open);
			return;
		}
		if (State == GateState.Open)
		{
			CloseDoor();
		}
		else
		{
			SetGateNavMeshState(isEnabled: false);
		}
		SetGateNavMeshStateForEnemies(isEnabled: true);
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!base.GameEntity.IsVisibleIncludeParents())
		{
			return;
		}
		if (!GameNetwork.IsClientOrReplay && NavigationMeshIdToDisableOnOpen != -1)
		{
			if (_openNavMeshIdDisabled)
			{
				if (base.IsDestroyed)
				{
					base.Scene.SetAbilityOfFacesWithId(NavigationMeshIdToDisableOnOpen, isEnabled: true);
					_openNavMeshIdDisabled = false;
				}
				else if (State == GateState.Closed)
				{
					int animationIndexAtChannel = _doorSkeleton.GetAnimationIndexAtChannel(0);
					float animationParameterAtChannel = _doorSkeleton.GetAnimationParameterAtChannel(0);
					if (animationIndexAtChannel != _closingAnimationIndex || animationParameterAtChannel > 0.4f)
					{
						base.Scene.SetAbilityOfFacesWithId(NavigationMeshIdToDisableOnOpen, isEnabled: true);
						_openNavMeshIdDisabled = false;
					}
				}
			}
			else if (State == GateState.Open && !base.IsDestroyed)
			{
				base.Scene.SetAbilityOfFacesWithId(NavigationMeshIdToDisableOnOpen, isEnabled: false);
				_openNavMeshIdDisabled = true;
			}
		}
		if (_afterMissionStartTriggered)
		{
			UpdateDoorBodies(updateAnyway: false);
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			ServerTick(dt);
		}
		if (!base.Ai.HasActionCompleted)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			if (base.StandingPoints[i].HasUser || base.StandingPoints[i].HasAIMovingTo)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return;
		}
		bool flag2 = false;
		for (int j = 0; j < base.UserFormations.Count; j++)
		{
			if (base.UserFormations[j].CountOfDetachableNonPlayerUnits > 0)
			{
				flag2 = true;
				break;
			}
		}
		if (!flag2)
		{
			((CastleGateAI)base.Ai).ResetInitialGateState(State);
		}
	}

	protected override bool IsAgentOnInconvenientNavmesh(Agent agent, StandingPoint standingPoint)
	{
		if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.Siege)
		{
			return false;
		}
		int currentNavigationFaceId = agent.GetCurrentNavigationFaceId();
		if (agent.Team.TeamAI is TeamAISiegeComponent teamAISiegeComponent && currentNavigationFaceId % 10 != 1)
		{
			if (base.GameEntity.HasTag("inner_gate"))
			{
				return true;
			}
			if (base.GameEntity.HasTag("outer_gate"))
			{
				CastleGate innerGate = teamAISiegeComponent.InnerGate;
				if (innerGate != null)
				{
					Vec3 vec = base.GameEntity.GlobalPosition - agent.Position;
					Vec3 vec2 = innerGate.GameEntity.GlobalPosition - agent.Position;
					if (vec.AsVec2.DotProduct(vec2.AsVec2) > 0f)
					{
						return true;
					}
				}
			}
			foreach (int difficultNavmeshID in (Mission.Current.DefenderTeam.TeamAI as TeamAISiegeDefender).DifficultNavmeshIDs)
			{
				if (currentNavigationFaceId == difficultNavmeshID)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ServerTick(float dt)
	{
		if (IsDeactivated)
		{
			return;
		}
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (!standingPoint.HasUser)
			{
				continue;
			}
			if (standingPoint.GameEntity.HasTag("open"))
			{
				OpenDoor();
				if (AutoOpen)
				{
					SetAutoOpenState(isEnabled: false);
				}
			}
			else
			{
				CloseDoor();
				if (Mission.Current.IsSallyOutBattle)
				{
					SetAutoOpenState(isEnabled: true);
				}
			}
		}
		if (AutoOpen && _pathChecker != null)
		{
			_pathChecker.Tick(dt);
			if (_pathChecker.HasAgentsUsingPath())
			{
				if (State != GateState.Open)
				{
					OpenDoor();
				}
			}
			else if (State != GateState.Closed)
			{
				CloseDoor();
			}
		}
		if (!(_doorSkeleton != null) || base.IsDestroyed)
		{
			return;
		}
		float animationParameterAtChannel = _doorSkeleton.GetAnimationParameterAtChannel(0);
		foreach (StandingPoint standingPoint2 in base.StandingPoints)
		{
			bool isDeactivatedSynched = animationParameterAtChannel < 1f || standingPoint2.GameEntity.HasTag((State == GateState.Open) ? "open" : "close");
			standingPoint2.SetIsDeactivatedSynched(isDeactivatedSynched);
		}
		if (animationParameterAtChannel >= 1f && State == GateState.Open)
		{
			if (_extraColliderRight != null)
			{
				_extraColliderRight.SetBodyFlags(_extraColliderRight.BodyFlag | BodyFlags.Disabled);
				_rightExtraColliderDisabled = true;
			}
			if (_extraColliderLeft != null)
			{
				_extraColliderLeft.SetBodyFlags(_extraColliderLeft.BodyFlag | BodyFlags.Disabled);
				_leftExtraColliderDisabled = true;
			}
		}
		if (_plank != null && State == GateState.Closed && animationParameterAtChannel > 0.9f)
		{
			_plank.SetVisibleSynched(value: true);
		}
	}

	public TargetFlags GetTargetFlags()
	{
		TargetFlags targetFlags = TargetFlags.None;
		targetFlags |= TargetFlags.IsStructure;
		if (base.IsDestroyed)
		{
			targetFlags |= TargetFlags.NotAThreat;
		}
		if (DebugSiegeBehavior.DebugAttackState == DebugSiegeBehavior.DebugStateAttacker.DebugAttackersToBattlements)
		{
			targetFlags |= TargetFlags.DebugThreat;
		}
		return targetFlags;
	}

	public float GetTargetValue(List<Vec3> weaponPos)
	{
		return 10f;
	}

	public WeakGameEntity GetTargetEntity()
	{
		return base.GameEntity;
	}

	public BattleSideEnum GetSide()
	{
		return BattleSideEnum.Defender;
	}

	public Vec3 GetTargetGlobalVelocity()
	{
		return Vec3.Zero;
	}

	public bool IsDestructable()
	{
		return base.GameEntity.HasScriptOfType<DestructableComponent>();
	}

	public WeakGameEntity Entity()
	{
		return base.GameEntity;
	}

	public (Vec3, Vec3) ComputeGlobalPhysicsBoundingBoxMinMax()
	{
		return base.GameEntity.ComputeGlobalPhysicsBoundingBoxMinMax();
	}

	protected void CollectGameEntities(bool calledFromOnInit)
	{
		CollectDynamicGameEntities(calledFromOnInit);
		if (!GameNetwork.IsClientOrReplay)
		{
			List<WeakGameEntity> list = base.GameEntity.CollectChildrenEntitiesWithTag("plank");
			if (list.Count > 0)
			{
				_plank = list.FirstOrDefault().GetFirstScriptOfType<SynchedMissionObject>();
			}
		}
	}

	protected void OnNextDestructionState()
	{
		CollectDynamicGameEntities(calledFromOnInit: false);
		UpdateDoorBodies(updateAnyway: true);
	}

	protected void CollectDynamicGameEntities(bool calledFromOnInit)
	{
		_attackOnlyDoorColliders.Clear();
		List<WeakGameEntity> list;
		if (calledFromOnInit)
		{
			list = base.GameEntity.CollectChildrenEntitiesWithTag("gate").ToList();
			_leftExtraColliderDisabled = false;
			_rightExtraColliderDisabled = false;
			_agentColliderLeft = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("collider_agent_l"));
			_agentColliderRight = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("collider_agent_r"));
		}
		else
		{
			list = (from x in base.GameEntity.CollectChildrenEntitiesWithTag("gate")
				where x.IsVisibleIncludeParents()
				select x).ToList();
		}
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count > 1)
		{
			int num = int.MinValue;
			int num2 = int.MaxValue;
			WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
			WeakGameEntity weakGameEntity2 = WeakGameEntity.Invalid;
			foreach (WeakGameEntity item in list)
			{
				int num3 = int.Parse(item.Tags.FirstOrDefault((string x) => x.Contains("state_")).Split(new char[1] { '_' }).Last());
				if (num3 > num)
				{
					num = num3;
					weakGameEntity = item;
				}
				if (num3 < num2)
				{
					num2 = num3;
					weakGameEntity2 = item;
				}
			}
			_door = (calledFromOnInit ? weakGameEntity2.GetFirstScriptOfType<SynchedMissionObject>() : weakGameEntity.GetFirstScriptOfType<SynchedMissionObject>());
		}
		else
		{
			_door = list[0].GetFirstScriptOfType<SynchedMissionObject>();
		}
		_doorSkeleton = _door.GameEntity.Skeleton;
		WeakGameEntity weakEntity = _door.GameEntity.CollectChildrenEntitiesWithTag("collider_r").FirstOrDefault();
		if (weakEntity.IsValid)
		{
			_attackOnlyDoorColliders.Add(TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity));
		}
		WeakGameEntity weakEntity2 = _door.GameEntity.CollectChildrenEntitiesWithTag("collider_l").FirstOrDefault();
		if (weakEntity2.IsValid)
		{
			_attackOnlyDoorColliders.Add(TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity2));
		}
		if (!weakEntity.IsValid || !weakEntity2.IsValid)
		{
			_agentColliderLeft?.SetVisibilityExcludeParents(visible: false);
			_agentColliderRight?.SetVisibilityExcludeParents(visible: false);
		}
		WeakGameEntity weakGameEntity3 = _door.GameEntity.CollectChildrenEntitiesWithTag(ExtraCollisionObjectTagLeft).FirstOrDefault();
		if (weakGameEntity3.IsValid)
		{
			if (!ActivateExtraColliders)
			{
				weakGameEntity3.RemovePhysics();
			}
			else
			{
				if (!calledFromOnInit)
				{
					MatrixFrame frame = ((_extraColliderLeft != null) ? _extraColliderLeft.GetFrame() : _doorSkeleton.GetBoneEntitialFrameWithName(LeftDoorBoneName));
					_extraColliderLeft = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakGameEntity3);
					_extraColliderLeft.SetFrame(ref frame);
				}
				else
				{
					_extraColliderLeft = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakGameEntity3);
				}
				if (_leftExtraColliderDisabled)
				{
					_extraColliderLeft.SetBodyFlags(_extraColliderLeft.BodyFlag | BodyFlags.Disabled);
				}
				else
				{
					_extraColliderLeft.SetBodyFlags((BodyFlags)((uint)_extraColliderLeft.BodyFlag & 0xFFFFFFFEu));
				}
			}
		}
		WeakGameEntity weakGameEntity4 = _door.GameEntity.CollectChildrenEntitiesWithTag(ExtraCollisionObjectTagRight).FirstOrDefault();
		if (weakGameEntity4.IsValid)
		{
			if (!ActivateExtraColliders)
			{
				weakGameEntity4.RemovePhysics();
			}
			else
			{
				if (!calledFromOnInit)
				{
					MatrixFrame frame2 = ((_extraColliderRight != null) ? _extraColliderRight.GetFrame() : _doorSkeleton.GetBoneEntitialFrameWithName(RightDoorBoneName));
					_extraColliderRight = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakGameEntity4);
					_extraColliderRight.SetFrame(ref frame2);
				}
				else
				{
					_extraColliderRight = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakGameEntity4);
				}
				if (_rightExtraColliderDisabled)
				{
					_extraColliderRight.SetBodyFlags(_extraColliderRight.BodyFlag | BodyFlags.Disabled);
				}
				else
				{
					_extraColliderRight.SetBodyFlags((BodyFlags)((uint)_extraColliderRight.BodyFlag & 0xFFFFFFFEu));
				}
			}
		}
		if (_door != null && _doorSkeleton != null)
		{
			_leftDoorBoneIndex = Skeleton.GetBoneIndexFromName(_doorSkeleton.GetName(), LeftDoorBoneName);
			_rightDoorBoneIndex = Skeleton.GetBoneIndexFromName(_doorSkeleton.GetName(), RightDoorBoneName);
		}
	}

	private void InitializeExtraColliderPositions()
	{
		if (_extraColliderLeft != null)
		{
			MatrixFrame frame = _doorSkeleton.GetBoneEntitialFrameWithName(LeftDoorBoneName);
			_extraColliderLeft.SetFrame(ref frame);
			_extraColliderLeft.SetVisibilityExcludeParents(visible: true);
		}
		if (_extraColliderRight != null)
		{
			MatrixFrame frame2 = _doorSkeleton.GetBoneEntitialFrameWithName(RightDoorBoneName);
			_extraColliderRight.SetFrame(ref frame2);
			_extraColliderRight.SetVisibilityExcludeParents(visible: true);
		}
		UpdateDoorBodies(updateAnyway: true);
		foreach (GameEntity attackOnlyDoorCollider in _attackOnlyDoorColliders)
		{
			attackOnlyDoorCollider.SetVisibilityExcludeParents(visible: true);
		}
		if (_agentColliderLeft != null)
		{
			_agentColliderLeft.SetVisibilityExcludeParents(visible: true);
		}
		if (_agentColliderRight != null)
		{
			_agentColliderRight.SetVisibilityExcludeParents(visible: true);
		}
	}

	private void OnHitTaken(DestructableComponent hitComponent, Agent hitterAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (!GameNetwork.IsClientOrReplay && inflictedDamage >= 200 && State == GateState.Closed && attackerScriptComponentBehavior is BatteringRam)
		{
			_plank?.SetAnimationAtChannelSynched(PlankHitAnimationName, 0);
			_door.SetAnimationAtChannelSynched(HitAnimationName, 0);
			Mission.Current.MakeSound(BatteringRamHitSoundIdCache, base.GameEntity.GlobalPosition, soundCanBePredicted: false, isReliable: true, -1, -1);
		}
	}

	private void OnDestroyed(DestructableComponent destroyedComponent, Agent destroyerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		_plank?.SetVisibleSynched(value: false);
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.SetIsDeactivatedSynched(value: true);
		}
		if (attackerScriptComponentBehavior is BatteringRam)
		{
			_door.SetAnimationAtChannelSynched(DestroyAnimationName, 0);
		}
		SetGateNavMeshState(isEnabled: true);
	}

	private int OnCalculateDestructionStateIndex(int destructionStateIndex, int inflictedDamage, int destructionStateCount)
	{
		if (inflictedDamage < 200)
		{
			return destructionStateIndex;
		}
		return TaleWorlds.Library.MathF.Min(destructionStateIndex, destructionStateCount - 1);
	}

	protected internal override bool OnCheckForProblems()
	{
		bool result = base.OnCheckForProblems();
		if (base.GameEntity.HasTag("outer_gate") && base.GameEntity.HasTag("inner_gate"))
		{
			MBEditor.AddEntityWarning(base.GameEntity, "This castle gate has both outer and inner tag at the same time.");
			result = true;
		}
		if (base.GameEntity.CollectChildrenEntitiesWithTag("wait_pos").Count != 1)
		{
			MBEditor.AddEntityWarning(base.GameEntity, "There must be one entity with wait position tag under castle gate.");
			result = true;
		}
		if (base.GameEntity.HasTag("outer_gate"))
		{
			uint visibilityMask = base.GameEntity.GetVisibilityLevelMaskIncludingParents();
			WeakGameEntity weakGameEntity = base.GameEntity.GetChildren().FirstOrDefault((WeakGameEntity x) => x.HasTag("middle_pos") && x.GetVisibilityLevelMaskIncludingParents() == visibilityMask);
			if (weakGameEntity.IsValid)
			{
				WeakGameEntity weakGameEntity2 = base.Scene.FindWeakEntitiesWithTag("inner_gate").FirstOrDefault((WeakGameEntity x) => x.GetVisibilityLevelMaskIncludingParents() == visibilityMask);
				if (weakGameEntity2 != null)
				{
					if (weakGameEntity2.HasScriptOfType<CastleGate>())
					{
						Vec2 va = weakGameEntity2.GlobalPosition.AsVec2 - weakGameEntity.GlobalPosition.AsVec2;
						Vec2 vb = base.GameEntity.GlobalPosition.AsVec2 - weakGameEntity.GlobalPosition.AsVec2;
						if (Vec2.DotProduct(va, vb) <= 0f)
						{
							MBEditor.AddEntityWarning(base.GameEntity, "Outer gate's middle position must not be between outer and inner gate.");
							result = true;
						}
					}
					else
					{
						MBEditor.AddEntityWarning(base.GameEntity, weakGameEntity2.Name + " this entity has inner gate tag but doesn't have castle gate script.");
						result = true;
					}
				}
				else
				{
					MBEditor.AddEntityWarning(base.GameEntity, "There is no entity with inner gate tag.");
					result = true;
				}
			}
			else
			{
				MBEditor.AddEntityWarning(base.GameEntity, "Outer gate doesn't have any middle positions");
				result = true;
			}
		}
		Vec3 scaleVector = base.GameEntity.GetGlobalFrame().rotation.GetScaleVector();
		if (TaleWorlds.Library.MathF.Abs(scaleVector.x - scaleVector.y) > 1E-05f || TaleWorlds.Library.MathF.Abs(scaleVector.x - scaleVector.z) > 1E-05f || TaleWorlds.Library.MathF.Abs(scaleVector.y - scaleVector.z) > 1E-05f)
		{
			MBEditor.AddEntityWarning(base.GameEntity, "$$$ Non uniform scale on CastleGate at scene " + base.GameEntity.Scene.GetName());
			result = true;
		}
		return result;
	}

	public Vec3 GetTargetingOffset()
	{
		return Vec3.Zero;
	}
}
