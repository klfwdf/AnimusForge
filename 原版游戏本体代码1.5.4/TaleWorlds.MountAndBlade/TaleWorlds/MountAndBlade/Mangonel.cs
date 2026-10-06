using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace TaleWorlds.MountAndBlade;

public class Mangonel : RangedSiegeWeapon, ISpawnable
{
	private const string BodyTag = "body";

	private const string RopeTag = "rope";

	private const string RotateTag = "rotate";

	private const string LeftTag = "left";

	private const string VerticalAdjusterTag = "vertical_adjuster";

	private string _missileBoneName = "end_throwarm";

	private List<StandingPoint> _rotateStandingPoints;

	private SynchedMissionObject _body;

	private SynchedMissionObject _rope;

	private GameEntity _verticalAdjuster;

	private MatrixFrame _verticalAdjusterStartingLocalFrame;

	private Skeleton _verticalAdjusterSkeleton;

	private Skeleton _bodySkeleton;

	private float _timeElapsedAfterLoading;

	private bool _isInitialProjectilePositionUpdated;

	private MatrixFrame[] _standingPointLocalIKFrames;

	private StandingPoint _reloadWithoutPilot;

	public string MangonelBodySkeleton = "mangonel_skeleton";

	public string MangonelBodyFire = "mangonel_fire";

	public string MangonelBodyReload = "mangonel_set_up";

	public string MangonelRopeFire = "mangonel_holder_fire";

	public string MangonelRopeReload = "mangonel_holder_set_up";

	public string MangonelAimAnimation = "mangonel_a_anglearm_state";

	public string ProjectileBoneName = "end_throwarm";

	public string IdleActionName;

	public string ShootActionName;

	public string Reload1ActionName;

	public string Reload2ActionName;

	public string RotateLeftActionName;

	public string RotateRightActionName;

	public string LoadAmmoBeginActionName;

	public string LoadAmmoEndActionName;

	public string Reload2IdleActionName;

	public float ProjectileSpeed = 40f;

	private ActionIndexCache _idleAnimationActionIndex;

	private ActionIndexCache _shootAnimationActionIndex;

	private ActionIndexCache _reload1AnimationActionIndex;

	private ActionIndexCache _reload2AnimationActionIndex;

	private ActionIndexCache _rotateLeftAnimationActionIndex;

	private ActionIndexCache _rotateRightAnimationActionIndex;

	private ActionIndexCache _loadAmmoBeginAnimationActionIndex;

	private ActionIndexCache _loadAmmoEndAnimationActionIndex;

	private ActionIndexCache _reload2IdleActionIndex;

	private sbyte _missileBoneIndex;

	protected override float MaximumBallisticError => 1.5f;

	protected override float ShootingSpeed => ProjectileSpeed;

	protected override float HorizontalAimSensitivity
	{
		get
		{
			if (DefaultSide == BattleSideEnum.Defender)
			{
				return 0.25f;
			}
			float num = 0.05f;
			foreach (StandingPoint rotateStandingPoint in _rotateStandingPoints)
			{
				if (rotateStandingPoint.HasUser && !rotateStandingPoint.UserAgent.IsInBeingStruckAction)
				{
					num += 0.1f;
				}
			}
			return num;
		}
	}

	protected override float VerticalAimSensitivity => 0.1f;

	protected override Vec3 ShootingDirection
	{
		get
		{
			Mat3 rotation = _body.GameEntity.GetGlobalFrame().rotation;
			rotation.RotateAboutSide(0f - CurrentReleaseAngle);
			return rotation.TransformToParent(new Vec3(0f, -1f));
		}
	}

	protected override bool HasAmmo
	{
		get
		{
			if (!base.HasAmmo && base.CurrentlyUsedAmmoPickUpPoint == null && !LoadAmmoStandingPoint.HasUser)
			{
				return LoadAmmoStandingPoint.HasAIMovingTo;
			}
			return true;
		}
		set
		{
			base.HasAmmo = value;
		}
	}

	protected override void RegisterAnimationParameters()
	{
		SkeletonOwnerObjects = new SynchedMissionObject[2];
		Skeletons = new Skeleton[2];
		SkeletonNames = new string[1];
		FireAnimations = new string[2];
		FireAnimationIndices = new int[2];
		SetUpAnimations = new string[2];
		SetUpAnimationIndices = new int[2];
		SkeletonOwnerObjects[0] = _body;
		Skeletons[0] = _body.GameEntity.Skeleton;
		SkeletonNames[0] = MangonelBodySkeleton;
		FireAnimations[0] = MangonelBodyFire;
		FireAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(MangonelBodyFire);
		SetUpAnimations[0] = MangonelBodyReload;
		SetUpAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(MangonelBodyReload);
		SkeletonOwnerObjects[1] = _rope;
		Skeletons[1] = _rope.GameEntity.Skeleton;
		FireAnimations[1] = MangonelRopeFire;
		FireAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(MangonelRopeFire);
		SetUpAnimations[1] = MangonelRopeReload;
		SetUpAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(MangonelRopeReload);
		_missileBoneName = ProjectileBoneName;
		_idleAnimationActionIndex = ActionIndexCache.Create(IdleActionName);
		_shootAnimationActionIndex = ActionIndexCache.Create(ShootActionName);
		_reload1AnimationActionIndex = ActionIndexCache.Create(Reload1ActionName);
		_reload2AnimationActionIndex = ActionIndexCache.Create(Reload2ActionName);
		_rotateLeftAnimationActionIndex = ActionIndexCache.Create(RotateLeftActionName);
		_rotateRightAnimationActionIndex = ActionIndexCache.Create(RotateRightActionName);
		_loadAmmoBeginAnimationActionIndex = ActionIndexCache.Create(LoadAmmoBeginActionName);
		_loadAmmoEndAnimationActionIndex = ActionIndexCache.Create(LoadAmmoEndActionName);
		_reload2IdleActionIndex = ActionIndexCache.Create(Reload2IdleActionName);
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new MangonelAI(this);
	}

	public override SiegeEngineType GetSiegeEngineType()
	{
		if (DefaultSide != BattleSideEnum.Attacker)
		{
			return DefaultSiegeEngineTypes.Catapult;
		}
		return DefaultSiegeEngineTypes.Onager;
	}

	protected internal override void OnInit()
	{
		List<SynchedMissionObject> list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("rope");
		if (list.Count > 0)
		{
			_rope = list[0];
		}
		list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("body");
		_body = list[0];
		_bodySkeleton = _body.GameEntity.Skeleton;
		RotationObject = _body;
		List<WeakGameEntity> list2 = base.GameEntity.CollectChildrenEntitiesWithTag("vertical_adjuster");
		_verticalAdjuster = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(list2[0]);
		_verticalAdjusterSkeleton = _verticalAdjuster.Skeleton;
		if (_verticalAdjusterSkeleton != null)
		{
			_verticalAdjusterSkeleton.SetAnimationAtChannel(MangonelAimAnimation, 0);
		}
		_verticalAdjusterStartingLocalFrame = _verticalAdjuster.GetFrame();
		_verticalAdjusterStartingLocalFrame = _body.GameEntity.GetBoneEntitialFrameWithIndex(0).TransformToLocal(in _verticalAdjusterStartingLocalFrame);
		base.OnInit();
		TimeGapBetweenShootActionAndProjectileLeaving = 0.23f;
		TimeGapBetweenShootingEndAndReloadingStart = 0f;
		_rotateStandingPoints = new List<StandingPoint>();
		if (base.StandingPoints != null)
		{
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				if (standingPoint.GameEntity.HasTag("rotate"))
				{
					if (standingPoint.GameEntity.HasTag("left") && _rotateStandingPoints.Count > 0)
					{
						_rotateStandingPoints.Insert(0, standingPoint);
					}
					else
					{
						_rotateStandingPoints.Add(standingPoint);
					}
				}
			}
			MatrixFrame frame = _body.GameEntity.GetGlobalFrame();
			_standingPointLocalIKFrames = new MatrixFrame[base.StandingPoints.Count];
			for (int i = 0; i < base.StandingPoints.Count; i++)
			{
				_standingPointLocalIKFrames[i] = base.StandingPoints[i].GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame);
				base.StandingPoints[i].AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
			}
		}
		_missileBoneIndex = Skeleton.GetBoneIndexFromName(Skeletons[0].GetName(), _missileBoneName);
		ApplyAimChange();
		foreach (StandingPoint reloadStandingPoint in ReloadStandingPoints)
		{
			if (reloadStandingPoint != base.PilotStandingPoint)
			{
				_reloadWithoutPilot = reloadStandingPoint;
			}
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			SetActivationLoadAmmoPoint(activate: false);
		}
		EnemyRangeToStopUsing = 9f;
		SetScriptComponentToTick(GetTickRequirement());
		if (base.AmmoPickUpPoints != null)
		{
			foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
			{
				ammoPickUpPoint.LockUserFrames = true;
			}
		}
		_isInitialProjectilePositionUpdated = false;
	}

	protected internal override void OnEditorInit()
	{
	}

	public override void OnPilotAssignedDuringSpawn()
	{
		base.PilotAgent.SetActionChannel(1, in _idleAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL);
		MatrixFrame globalFrame = base.PilotStandingPoint.GameEntity.GetGlobalFrame();
		base.PilotAgent.TeleportToPosition(globalFrame.origin);
		base.PilotAgent.DisableScriptedMovement();
		base.PilotAgent.SetMovementDirection(globalFrame.rotation.f.AsVec2.Normalized());
	}

	protected override bool CanRotate()
	{
		if (base.State != WeaponState.Idle && base.State != WeaponState.LoadingAmmo)
		{
			return base.State == WeaponState.WaitingBeforeIdle;
		}
		return true;
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
		if (!base.GameEntity.IsVisibleIncludeParents())
		{
			return;
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
			{
				if (!ammoPickUpPoint.HasUser)
				{
					continue;
				}
				Agent userAgent = ammoPickUpPoint.UserAgent;
				ActionIndexCache currentAction = userAgent.GetCurrentAction(1);
				if (currentAction == ActionIndexCache.act_pickup_boulder_begin)
				{
					continue;
				}
				if (currentAction == ActionIndexCache.act_pickup_boulder_end)
				{
					MissionWeapon weapon = new MissionWeapon(OriginalMissileItem, null, null, 1);
					userAgent.EquipWeaponToExtraSlotAndWield(ref weapon);
					userAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
					ConsumeAmmo();
					if (userAgent.IsAIControlled)
					{
						if (!LoadAmmoStandingPoint.HasUser && !LoadAmmoStandingPoint.IsDeactivated)
						{
							userAgent.AIMoveToGameObjectEnable(LoadAmmoStandingPoint, this, base.Ai.GetScriptedFrameFlags(userAgent));
							continue;
						}
						if (ReloaderAgentOriginalPoint != null && !ReloaderAgentOriginalPoint.HasUser && !ReloaderAgentOriginalPoint.HasAIMovingTo)
						{
							userAgent.AIMoveToGameObjectEnable(ReloaderAgentOriginalPoint, this, base.Ai.GetScriptedFrameFlags(userAgent));
							continue;
						}
						ReloaderAgent?.Formation?.AttachUnit(ReloaderAgent);
						ReloaderAgent = null;
					}
				}
				else if (!userAgent.SetActionChannel(1, in ActionIndexCache.act_pickup_boulder_begin, ignorePriority: false, (AnimFlags)0uL) && userAgent.Controller != AgentControllerType.AI)
				{
					userAgent.StopUsingGameObject();
				}
			}
		}
		switch (base.State)
		{
		case WeaponState.WaitingBeforeIdle:
			_timeElapsedAfterLoading += dt;
			if (_timeElapsedAfterLoading > 1f)
			{
				base.State = WeaponState.Idle;
			}
			break;
		case WeaponState.LoadingAmmo:
			if (GameNetwork.IsClientOrReplay)
			{
				break;
			}
			if (LoadAmmoStandingPoint.HasUser)
			{
				Agent userAgent2 = LoadAmmoStandingPoint.UserAgent;
				if (userAgent2.GetCurrentAction(1) == _loadAmmoEndAnimationActionIndex)
				{
					EquipmentIndex primaryWieldedItemIndex = userAgent2.GetPrimaryWieldedItemIndex();
					if (primaryWieldedItemIndex != EquipmentIndex.None && userAgent2.Equipment[primaryWieldedItemIndex].CurrentUsageItem.WeaponClass == OriginalMissileItem.PrimaryWeapon.WeaponClass)
					{
						ChangeProjectileEntityServer(userAgent2, userAgent2.Equipment[primaryWieldedItemIndex].Item.StringId);
						userAgent2.RemoveEquippedWeapon(primaryWieldedItemIndex);
						_timeElapsedAfterLoading = 0f;
						base.Projectile.SetVisibleSynched(value: true);
						base.State = WeaponState.WaitingBeforeIdle;
					}
					else
					{
						userAgent2.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
						if (!userAgent2.IsPlayerControlled)
						{
							SendAgentToAmmoPickup(userAgent2);
						}
					}
				}
				else
				{
					if (!(userAgent2.GetCurrentAction(1) != _loadAmmoBeginAnimationActionIndex) || userAgent2.SetActionChannel(1, in _loadAmmoBeginAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL))
					{
						break;
					}
					for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
					{
						if (!userAgent2.Equipment[equipmentIndex].IsEmpty && userAgent2.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass == OriginalMissileItem.PrimaryWeapon.WeaponClass)
						{
							userAgent2.RemoveEquippedWeapon(equipmentIndex);
						}
					}
					userAgent2.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
					if (!userAgent2.IsPlayerControlled)
					{
						SendAgentToAmmoPickup(userAgent2);
					}
				}
			}
			else if (LoadAmmoStandingPoint.HasAIMovingTo)
			{
				Agent movingAgent = LoadAmmoStandingPoint.MovingAgent;
				EquipmentIndex primaryWieldedItemIndex2 = movingAgent.GetPrimaryWieldedItemIndex();
				if (primaryWieldedItemIndex2 == EquipmentIndex.None || movingAgent.Equipment[primaryWieldedItemIndex2].CurrentUsageItem.WeaponClass != OriginalMissileItem.PrimaryWeapon.WeaponClass)
				{
					movingAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
					SendAgentToAmmoPickup(movingAgent);
				}
			}
			break;
		case WeaponState.Reloading:
		case WeaponState.ReloadingPaused:
			break;
		}
	}

	protected internal override void OnTickParallel(float dt)
	{
		base.OnTickParallel(dt);
		if (!base.GameEntity.IsVisibleIncludeParents())
		{
			return;
		}
		if (!_isInitialProjectilePositionUpdated)
		{
			_isInitialProjectilePositionUpdated = true;
			UpdateProjectilePosition();
		}
		if (base.State == WeaponState.WaitingBeforeProjectileLeaving)
		{
			UpdateProjectilePosition();
		}
		if (_verticalAdjusterSkeleton != null)
		{
			float parameter = MBMath.ClampFloat((CurrentReleaseAngle - BottomReleaseAngleRestriction) / (TopReleaseAngleRestriction - BottomReleaseAngleRestriction), 0f, 1f);
			_verticalAdjusterSkeleton.SetAnimationParameterAtChannel(0, parameter);
		}
		MatrixFrame frame = Skeletons[0].GetBoneEntitialFrameWithIndex(0).TransformToParent(in _verticalAdjusterStartingLocalFrame);
		_verticalAdjuster.SetFrame(ref frame);
		MatrixFrame boundEntityGlobalFrame = _body.GameEntity.GetGlobalFrame();
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			if (!base.StandingPoints[i].HasUser)
			{
				continue;
			}
			if (base.StandingPoints[i].UserAgent.IsInBeingStruckAction || base.AmmoPickUpPoints.IndexOf(base.StandingPoints[i]) >= 0)
			{
				base.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
				continue;
			}
			ActionIndexCache currentAction = base.StandingPoints[i].UserAgent.GetCurrentAction(1);
			float currentActionProgress = base.StandingPoints[i].UserAgent.GetCurrentActionProgress(1);
			if (currentAction != _reload2IdleActionIndex && (currentAction != _reload2AnimationActionIndex || currentActionProgress > 0.1f) && (currentAction != _shootAnimationActionIndex || currentActionProgress < 0.15f))
			{
				base.StandingPoints[i].UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(in _standingPointLocalIKFrames[i], in boundEntityGlobalFrame);
			}
			else
			{
				base.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
			}
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			for (int j = 0; j < _rotateStandingPoints.Count; j++)
			{
				StandingPoint standingPoint = _rotateStandingPoints[j];
				if (standingPoint.HasUser && !standingPoint.UserAgent.SetActionChannel(1, (j == 0) ? _rotateLeftAnimationActionIndex : _rotateRightAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL) && standingPoint.UserAgent.Controller != AgentControllerType.AI)
				{
					standingPoint.UserAgent.StopUsingGameObjectMT();
				}
			}
			if (base.PilotAgent != null)
			{
				ActionIndexCache currentAction2 = base.PilotAgent.GetCurrentAction(1);
				if (base.State == WeaponState.WaitingBeforeProjectileLeaving)
				{
					if (base.PilotAgent.IsInBeingStruckAction)
					{
						if (currentAction2 != ActionIndexCache.act_none && currentAction2 != ActionIndexCache.act_strike_bent_over)
						{
							base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_strike_bent_over, ignorePriority: false, (AnimFlags)0uL);
						}
					}
					else if (!base.PilotAgent.SetActionChannel(1, in _shootAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL) && base.PilotAgent.Controller != AgentControllerType.AI)
					{
						base.PilotAgent.StopUsingGameObjectMT();
					}
				}
				else if (!base.PilotAgent.SetActionChannel(1, in _idleAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL) && currentAction2 != _reload1AnimationActionIndex && currentAction2 != _shootAnimationActionIndex && base.PilotAgent.Controller != AgentControllerType.AI)
				{
					base.PilotAgent.StopUsingGameObjectMT();
				}
			}
			if (_reloadWithoutPilot.HasUser)
			{
				Agent userAgent = _reloadWithoutPilot.UserAgent;
				if (!userAgent.SetActionChannel(1, in _reload2IdleActionIndex, ignorePriority: false, (AnimFlags)0uL) && userAgent.GetCurrentAction(1) != _reload2AnimationActionIndex && userAgent.Controller != AgentControllerType.AI)
				{
					userAgent.StopUsingGameObjectMT();
				}
			}
		}
		WeaponState state = base.State;
		if (state != WeaponState.Reloading)
		{
			return;
		}
		foreach (StandingPoint reloadStandingPoint in ReloadStandingPoints)
		{
			if (!reloadStandingPoint.HasUser)
			{
				continue;
			}
			ActionIndexCache currentAction3 = reloadStandingPoint.UserAgent.GetCurrentAction(1);
			if (currentAction3 == _reload1AnimationActionIndex || currentAction3 == _reload2AnimationActionIndex)
			{
				reloadStandingPoint.UserAgent.SetCurrentActionProgress(1, _bodySkeleton.GetAnimationParameterAtChannel(0));
			}
			else if (!GameNetwork.IsClientOrReplay)
			{
				ActionIndexCache actionIndexCache = ((reloadStandingPoint == base.PilotStandingPoint) ? _reload1AnimationActionIndex : _reload2AnimationActionIndex);
				if (!reloadStandingPoint.UserAgent.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, -0.2f, 0.4f, _bodySkeleton.GetAnimationParameterAtChannel(0)) && reloadStandingPoint.UserAgent.Controller != AgentControllerType.AI)
				{
					reloadStandingPoint.UserAgent.StopUsingGameObjectMT();
				}
			}
		}
	}

	protected override void SetActivationLoadAmmoPoint(bool activate)
	{
		LoadAmmoStandingPoint.SetIsDeactivatedSynched(!activate);
	}

	protected override void UpdateProjectilePosition()
	{
		MatrixFrame frame = Skeletons[0].GetBoneEntitialFrameWithIndex(_missileBoneIndex);
		base.Projectile.GameEntity.SetFrame(ref frame);
	}

	protected override void OnRangedSiegeWeaponStateChange()
	{
		base.OnRangedSiegeWeaponStateChange();
		switch (base.State)
		{
		case WeaponState.WaitingBeforeIdle:
			UpdateProjectilePosition();
			break;
		case WeaponState.Shooting:
			if (!GameNetwork.IsClientOrReplay)
			{
				base.Projectile.SetVisibleSynched(value: false);
			}
			else
			{
				base.Projectile.GameEntity.SetVisibilityExcludeParents(visible: false);
			}
			break;
		case WeaponState.Idle:
			if (!GameNetwork.IsClientOrReplay)
			{
				base.Projectile.SetVisibleSynched(value: true);
			}
			else
			{
				base.Projectile.GameEntity.SetVisibilityExcludeParents(visible: true);
			}
			break;
		}
	}

	protected override void GetSoundEventIndices()
	{
		MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/move");
		ReloadSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/reload");
		FireSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/fire");
	}

	protected override void ApplyAimChange()
	{
		base.ApplyAimChange();
		ShootingDirection.Normalize();
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if (!gameEntity.HasTag(AmmoPickUpTag))
		{
			return new TextObject("{=NbpcDXtJ}Mangonel");
		}
		return new TextObject("{=pzfbPbWW}Boulder");
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = (usableGameObject.GameEntity.HasTag("reload") ? new TextObject((base.PilotStandingPoint == usableGameObject) ? "{=fEQAPJ2e}{KEY} Use" : "{=Na81xuXn}{KEY} Rearm") : (usableGameObject.GameEntity.HasTag("rotate") ? new TextObject("{=5wx4BF5h}{KEY} Rotate") : (usableGameObject.GameEntity.HasTag(AmmoPickUpTag) ? new TextObject("{=bNYm3K6b}{KEY} Pick Up") : ((!usableGameObject.GameEntity.HasTag("ammoload")) ? new TextObject("{=fEQAPJ2e}{KEY} Use") : new TextObject("{=ibC4xPoo}{KEY} Load Ammo")))));
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TargetFlags GetTargetFlags()
	{
		TargetFlags targetFlags = TargetFlags.None;
		targetFlags |= TargetFlags.IsFlammable;
		targetFlags |= TargetFlags.IsSiegeEngine;
		if (Side == BattleSideEnum.Attacker)
		{
			targetFlags |= TargetFlags.IsAttacker;
		}
		if (base.IsDestroyed || IsDeactivated)
		{
			targetFlags |= TargetFlags.NotAThreat;
		}
		if (Side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToMangonels)
		{
			targetFlags |= TargetFlags.DebugThreat;
		}
		if (Side == BattleSideEnum.Defender && DebugSiegeBehavior.DebugAttackState == DebugSiegeBehavior.DebugStateAttacker.DebugAttackersToMangonels)
		{
			targetFlags |= TargetFlags.DebugThreat;
		}
		return targetFlags;
	}

	public override float GetTargetValue(List<Vec3> weaponPos)
	{
		return 40f * GetUserMultiplierOfWeapon() * GetDistanceMultiplierOfWeapon(weaponPos[0]) * GetHitPointMultiplierOfWeapon();
	}

	public override float ProcessTargetValue(float baseValue, TargetFlags flags)
	{
		if (flags.HasAnyFlag(TargetFlags.NotAThreat))
		{
			return -1000f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
		{
			baseValue *= 10000f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsStructure))
		{
			baseValue *= 2.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSmall))
		{
			baseValue *= 8f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsMoving))
		{
			baseValue *= 8f;
		}
		if (flags.HasAnyFlag(TargetFlags.DebugThreat))
		{
			baseValue *= 10000f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSiegeTower))
		{
			baseValue *= 8f;
		}
		return baseValue;
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		return GetDetachmentWeightAuxForExternalAmmoWeapons(side);
	}

	public void SetSpawnedFromSpawner()
	{
		_spawnedFromSpawner = true;
	}
}
