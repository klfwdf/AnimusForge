using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace TaleWorlds.MountAndBlade;

public class Trebuchet : RangedSiegeWeapon, ISpawnable
{
	public const float TrebuchetDirectionRestriction = System.MathF.PI * 4f / 9f;

	private const string BodyTag = "body";

	private const string SlingTag = "sling";

	private const string RopeTag = "rope";

	private const string RotateTag = "rotate";

	private const string VerticalAdjusterTag = "vertical_adjuster";

	private const string MissileBoneName = "bn_projectile_holder";

	private const string RotateObjectTag = "rotate_entity";

	public float ProjectileSpeed = 45f;

	private SynchedMissionObject _body;

	private SynchedMissionObject _sling;

	private SynchedMissionObject _rope;

	public string IdleWithAmmoAnimation;

	public string IdleEmptyAnimation;

	public string BodyFireAnimation;

	public string BodySetUpAnimation;

	public string SlingFireAnimation;

	public string SlingSetUpAnimation;

	public string RopeFireAnimation;

	public string RopeSetUpAnimation;

	public string VerticalAdjusterAnimation;

	private GameEntity _verticalAdjuster;

	private Skeleton _verticalAdjusterSkeleton;

	private MatrixFrame _verticalAdjusterStartingLocalFrame;

	private float _timeElapsedAfterLoading;

	private bool _shootAnimPlayed;

	private MatrixFrame[] _standingPointLocalIKFrames;

	private List<StandingPointWithWeaponRequirement> _ammoLoadPoints;

	private sbyte _missileBoneIndex;

	public override float DirectionRestriction => System.MathF.PI * 4f / 9f;

	protected override float ShootingSpeed => ProjectileSpeed;

	protected override float HorizontalAimSensitivity => 0.1f;

	protected override float VerticalAimSensitivity => 0.075f;

	protected override Vec3 ShootingDirection
	{
		get
		{
			Mat3 rotation = RotationObject.GameEntity.GetGlobalFrame().rotation;
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

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = (usableGameObject.GameEntity.HasTag(AmmoPickUpTag) ? new TextObject("{=bNYm3K6b}{KEY} Pick Up") : (usableGameObject.GameEntity.HasTag("reload") ? new TextObject((base.PilotStandingPoint == usableGameObject) ? "{=fEQAPJ2e}{KEY} Use" : "{=Na81xuXn}{KEY} Rearm") : (usableGameObject.GameEntity.HasTag("rotate") ? new TextObject("{=5wx4BF5h}{KEY} Rotate") : ((!usableGameObject.GameEntity.HasTag("ammoload")) ? TextObject.GetEmpty() : new TextObject("{=ibC4xPoo}{KEY} Load Ammo")))));
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if (!gameEntity.HasTag(AmmoPickUpTag))
		{
			return new TextObject("{=4Skg9QhO}Trebuchet");
		}
		return new TextObject("{=pzfbPbWW}Boulder");
	}

	protected override void RegisterAnimationParameters()
	{
		SkeletonOwnerObjects = new SynchedMissionObject[3];
		Skeletons = new Skeleton[3];
		SkeletonNames = new string[3];
		FireAnimations = new string[3];
		FireAnimationIndices = new int[3];
		SetUpAnimations = new string[3];
		SetUpAnimationIndices = new int[3];
		SkeletonOwnerObjects[0] = _body;
		Skeletons[0] = _body.GameEntity.Skeleton;
		SkeletonNames[0] = "trebuchet_a_skeleton";
		FireAnimations[0] = BodyFireAnimation;
		FireAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(BodyFireAnimation);
		SetUpAnimations[0] = BodySetUpAnimation;
		SetUpAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(BodySetUpAnimation);
		SkeletonOwnerObjects[1] = _sling;
		Skeletons[1] = _sling.GameEntity.Skeleton;
		SkeletonNames[1] = "trebuchet_a_sling_skeleton";
		FireAnimations[1] = SlingFireAnimation;
		FireAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(SlingFireAnimation);
		SetUpAnimations[1] = SlingSetUpAnimation;
		SetUpAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(SlingSetUpAnimation);
		SkeletonOwnerObjects[2] = _rope;
		Skeletons[2] = _rope.GameEntity.Skeleton;
		SkeletonNames[2] = "trebuchet_a_rope_skeleton";
		FireAnimations[2] = RopeFireAnimation;
		FireAnimationIndices[2] = MBAnimation.GetAnimationIndexWithName(RopeFireAnimation);
		SetUpAnimations[2] = RopeSetUpAnimation;
		SetUpAnimationIndices[2] = MBAnimation.GetAnimationIndexWithName(RopeSetUpAnimation);
	}

	public override SiegeEngineType GetSiegeEngineType()
	{
		return DefaultSiegeEngineTypes.Trebuchet;
	}

	protected override void GetSoundEventIndices()
	{
		MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/trebuchet/move");
		ReloadSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/trebuchet/reload");
		FireSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/trebuchet/fire");
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new TrebuchetAI(this);
	}

	protected internal override void OnInit()
	{
		List<SynchedMissionObject> list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("body");
		_body = list[0];
		list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("sling");
		_sling = list[0];
		list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("rope");
		_rope = list[0];
		List<WeakGameEntity> list2 = base.GameEntity.CollectChildrenEntitiesWithTag("vertical_adjuster");
		_verticalAdjuster = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(list2[0]);
		_verticalAdjusterSkeleton = _verticalAdjuster.Skeleton;
		_verticalAdjusterSkeleton.SetAnimationAtChannel(VerticalAdjusterAnimation, 0);
		_verticalAdjusterStartingLocalFrame = _verticalAdjuster.GetFrame();
		_verticalAdjusterStartingLocalFrame = _body.GameEntity.GetBoneEntitialFrameWithIndex(0).TransformToLocal(in _verticalAdjusterStartingLocalFrame);
		list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("rotate_entity");
		RotationObject = list[0];
		base.OnInit();
		TimeGapBetweenShootActionAndProjectileLeaving = 1.6f;
		TimeGapBetweenShootingEndAndReloadingStart = 0f;
		_ammoLoadPoints = new List<StandingPointWithWeaponRequirement>();
		if (base.StandingPoints != null)
		{
			for (int i = 0; i < base.StandingPoints.Count; i++)
			{
				if (base.StandingPoints[i].GameEntity.HasTag("ammoload"))
				{
					_ammoLoadPoints.Add(base.StandingPoints[i] as StandingPointWithWeaponRequirement);
				}
				else if (base.StandingPoints[i] != base.PilotStandingPoint && !base.StandingPoints[i].GameEntity.HasTag(AmmoPickUpTag) && !GameNetwork.IsClientOrReplay)
				{
					base.StandingPoints[i].SetIsDisabledForPlayersSynched(value: true);
				}
			}
			MatrixFrame m = _body.GameEntity.GetGlobalFrame();
			_standingPointLocalIKFrames = new MatrixFrame[base.StandingPoints.Count];
			for (int j = 0; j < base.StandingPoints.Count; j++)
			{
				_standingPointLocalIKFrames[j] = base.StandingPoints[j].GameEntity.GetGlobalFrame().TransformToLocal(in m);
				base.StandingPoints[j].AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
			}
		}
		ApplyAimChange();
		if (!GameNetwork.IsClientOrReplay)
		{
			SetActivationLoadAmmoPoint(activate: false);
			EnemyRangeToStopUsing = 11f;
			MachinePositionOffsetToStopUsingLocal = new Vec2(0f, 2.8f);
			_sling.SetAnimationAtChannelSynched((base.State == WeaponState.Idle) ? IdleWithAmmoAnimation : IdleEmptyAnimation, 0);
		}
		_missileBoneIndex = Skeleton.GetBoneIndexFromName(_sling.GameEntity.Skeleton.GetName(), "bn_projectile_holder");
		_shootAnimPlayed = false;
		UpdateAmmoMesh();
		SetScriptComponentToTick(GetTickRequirement());
		UpdateProjectilePosition();
	}

	public override void AfterMissionStart()
	{
		if (base.AmmoPickUpPoints != null)
		{
			foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
			{
				ammoPickUpPoint.LockUserFrames = true;
			}
		}
		if (_ammoLoadPoints == null)
		{
			return;
		}
		foreach (StandingPointWithWeaponRequirement ammoLoadPoint in _ammoLoadPoints)
		{
			ammoLoadPoint.LockUserFrames = true;
		}
	}

	protected override void OnRangedSiegeWeaponStateChange()
	{
		base.OnRangedSiegeWeaponStateChange();
		if (base.State == WeaponState.WaitingBeforeIdle)
		{
			UpdateProjectilePosition();
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			switch (base.State)
			{
			case WeaponState.Shooting:
				base.Projectile.SetVisibleSynched(value: false);
				break;
			case WeaponState.Reloading:
				_shootAnimPlayed = false;
				break;
			case WeaponState.Idle:
				base.Projectile.SetVisibleSynched(value: true);
				break;
			case WeaponState.LoadingAmmo:
				_sling.SetAnimationAtChannelSynched(IdleEmptyAnimation, 0);
				break;
			}
		}
	}

	public override float ProcessTargetValue(float baseValue, TargetFlags flags)
	{
		if (flags.HasAnyFlag(TargetFlags.NotAThreat))
		{
			return -1000f;
		}
		if (flags.HasAllFlags(TargetFlags.IsSiegeEngine | TargetFlags.IsAttacker))
		{
			baseValue *= 1.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
		{
			baseValue *= 2.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsStructure))
		{
			baseValue *= 0.1f;
		}
		if (flags.HasAnyFlag(TargetFlags.DebugThreat))
		{
			baseValue *= 10000f;
		}
		return baseValue;
	}

	public override TargetFlags GetTargetFlags()
	{
		TargetFlags targetFlags = TargetFlags.None;
		targetFlags |= TargetFlags.IsFlammable;
		targetFlags |= TargetFlags.IsSiegeEngine;
		targetFlags |= TargetFlags.IsAttacker;
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
					MissionWeapon weapon = new MissionWeapon(OriginalMissileItem, null, null);
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
		{
			if (GameNetwork.IsClientOrReplay)
			{
				break;
			}
			bool flag = false;
			{
				foreach (StandingPointWithWeaponRequirement ammoLoadPoint in _ammoLoadPoints)
				{
					if (flag)
					{
						if (ammoLoadPoint.IsDeactivated)
						{
							if ((ammoLoadPoint.HasUser || ammoLoadPoint.HasAIMovingTo) && (ammoLoadPoint.UserAgent == ReloaderAgent || ammoLoadPoint.MovingAgent == ReloaderAgent))
							{
								SendReloaderAgentToOriginalPoint();
							}
							ammoLoadPoint.SetIsDeactivatedSynched(value: true);
						}
					}
					else if (ammoLoadPoint.HasUser)
					{
						flag = true;
						Agent userAgent2 = ammoLoadPoint.UserAgent;
						ActionIndexCache currentAction2 = userAgent2.GetCurrentAction(1);
						if (currentAction2 == ActionIndexCache.act_usage_trebuchet_load_ammo && userAgent2.GetCurrentActionProgress(1) > 0.56f)
						{
							EquipmentIndex primaryWieldedItemIndex = userAgent2.GetPrimaryWieldedItemIndex();
							if (primaryWieldedItemIndex != EquipmentIndex.None && userAgent2.Equipment[primaryWieldedItemIndex].CurrentUsageItem.WeaponClass == OriginalMissileItem.PrimaryWeapon.WeaponClass)
							{
								ChangeProjectileEntityServer(userAgent2, userAgent2.Equipment[primaryWieldedItemIndex].Item.StringId);
								userAgent2.RemoveEquippedWeapon(primaryWieldedItemIndex);
								_timeElapsedAfterLoading = 0f;
								base.Projectile.SetVisibleSynched(value: true);
								_sling.SetAnimationAtChannelSynched(IdleWithAmmoAnimation, 0);
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
							if (!(currentAction2 != ActionIndexCache.act_usage_trebuchet_load_ammo) || userAgent2.SetActionChannel(1, in ActionIndexCache.act_usage_trebuchet_load_ammo, ignorePriority: false, (AnimFlags)0uL))
							{
								continue;
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
					else if (ammoLoadPoint.HasAIMovingTo)
					{
						Agent movingAgent = ammoLoadPoint.MovingAgent;
						EquipmentIndex primaryWieldedItemIndex2 = movingAgent.GetPrimaryWieldedItemIndex();
						if (primaryWieldedItemIndex2 == EquipmentIndex.None || movingAgent.Equipment[primaryWieldedItemIndex2].CurrentUsageItem.WeaponClass != OriginalMissileItem.PrimaryWeapon.WeaponClass)
						{
							movingAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
							SendAgentToAmmoPickup(movingAgent);
						}
					}
				}
				break;
			}
		}
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
		if (base.State == WeaponState.WaitingBeforeProjectileLeaving)
		{
			UpdateProjectilePosition();
		}
		float parameter = MBMath.ClampFloat((CurrentReleaseAngle - BottomReleaseAngleRestriction) / (TopReleaseAngleRestriction - BottomReleaseAngleRestriction), 0f, 1f);
		_verticalAdjusterSkeleton.SetAnimationParameterAtChannel(0, parameter);
		MatrixFrame frame = _body.GameEntity.GetBoneEntitialFrameWithIndex(0).TransformToParent(in _verticalAdjusterStartingLocalFrame);
		_verticalAdjuster.SetFrame(ref frame);
		MatrixFrame boundEntityGlobalFrame = _body.GameEntity.GetGlobalFrame();
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			if (!base.StandingPoints[i].HasUser)
			{
				continue;
			}
			if (base.StandingPoints[i].UserAgent.IsInBeingStruckAction)
			{
				base.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
			}
			else if (base.StandingPoints[i] != base.PilotStandingPoint)
			{
				if (base.StandingPoints[i].UserAgent.GetCurrentAction(1) == ActionIndexCache.act_usage_trebuchet_reload_2)
				{
					base.StandingPoints[i].UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(in _standingPointLocalIKFrames[i], in boundEntityGlobalFrame);
				}
				else
				{
					base.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
				}
			}
			else
			{
				base.StandingPoints[i].UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(in _standingPointLocalIKFrames[i], in boundEntityGlobalFrame);
			}
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			if (base.PilotAgent != null)
			{
				ActionIndexCache currentAction = base.PilotAgent.GetCurrentAction(1);
				if (base.State == WeaponState.WaitingBeforeProjectileLeaving || base.State == WeaponState.Shooting || base.State == WeaponState.WaitingBeforeReloading)
				{
					if (!_shootAnimPlayed && currentAction != ActionIndexCache.act_usage_trebuchet_shoot)
					{
						_shootAnimPlayed = base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_trebuchet_shoot, ignorePriority: false, (AnimFlags)0uL);
					}
					else if (currentAction != ActionIndexCache.act_usage_trebuchet_shoot && !base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_trebuchet_reload_idle, ignorePriority: false, (AnimFlags)0uL) && base.PilotAgent.Controller != AgentControllerType.AI)
					{
						base.PilotAgent.StopUsingGameObjectMT();
					}
				}
				else if (currentAction != ActionIndexCache.act_usage_trebuchet_reload && currentAction != ActionIndexCache.act_usage_trebuchet_shoot && !base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_trebuchet_idle, ignorePriority: false, (AnimFlags)0uL) && base.PilotAgent.Controller != AgentControllerType.AI)
				{
					base.PilotAgent.StopUsingGameObjectMT();
				}
			}
			if (base.State != WeaponState.Reloading)
			{
				foreach (StandingPoint reloadStandingPoint in ReloadStandingPoints)
				{
					if (reloadStandingPoint.HasUser && reloadStandingPoint != base.PilotStandingPoint)
					{
						Agent userAgent = reloadStandingPoint.UserAgent;
						if (!userAgent.SetActionChannel(1, in ActionIndexCache.act_usage_trebuchet_reload_2_idle, ignorePriority: false, (AnimFlags)0uL) && userAgent.Controller != AgentControllerType.AI)
						{
							userAgent.StopUsingGameObjectMT();
						}
					}
				}
			}
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				if (standingPoint.HasUser && ReloadStandingPoints.IndexOf(standingPoint) < 0 && (!(standingPoint is StandingPointWithWeaponRequirement) || (_ammoLoadPoints.IndexOf((StandingPointWithWeaponRequirement)standingPoint) < 0 && base.AmmoPickUpPoints.IndexOf(standingPoint) < 0)))
				{
					Agent userAgent2 = standingPoint.UserAgent;
					if (!userAgent2.SetActionChannel(1, in ActionIndexCache.act_usage_trebuchet_reload_2_idle, ignorePriority: false, (AnimFlags)0uL) && userAgent2.Controller != AgentControllerType.AI)
					{
						userAgent2.StopUsingGameObjectMT();
					}
				}
			}
		}
		WeaponState state = base.State;
		if (state != WeaponState.Reloading)
		{
			return;
		}
		for (int j = 0; j < ReloadStandingPoints.Count; j++)
		{
			if (!ReloadStandingPoints[j].HasUser)
			{
				continue;
			}
			Agent userAgent3 = ReloadStandingPoints[j].UserAgent;
			ActionIndexCache currentAction2 = userAgent3.GetCurrentAction(1);
			if (currentAction2 == ActionIndexCache.act_usage_trebuchet_reload || currentAction2 == ActionIndexCache.act_usage_trebuchet_reload_2)
			{
				userAgent3.SetCurrentActionProgress(1, Skeletons[0].GetAnimationParameterAtChannel(0));
			}
			else if (!GameNetwork.IsClientOrReplay)
			{
				ActionIndexCache actionIndexCache = ActionIndexCache.act_usage_trebuchet_reload;
				if (ReloadStandingPoints[j].GameEntity.HasTag("right"))
				{
					actionIndexCache = ActionIndexCache.act_usage_trebuchet_reload_2;
				}
				if (!userAgent3.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, -0.2f, 0.4f, Skeletons[0].GetAnimationParameterAtChannel(0)) && userAgent3.Controller != AgentControllerType.AI)
				{
					userAgent3.StopUsingGameObjectMT();
				}
			}
		}
	}

	protected override void SetActivationLoadAmmoPoint(bool activate)
	{
		foreach (StandingPointWithWeaponRequirement ammoLoadPoint in _ammoLoadPoints)
		{
			ammoLoadPoint.SetIsDeactivatedSynched(!activate);
		}
	}

	protected override void UpdateProjectilePosition()
	{
		MatrixFrame frame = _sling.GameEntity.GetBoneEntitialFrameWithIndex(_missileBoneIndex);
		base.Projectile.GameEntity.SetFrame(ref frame);
	}

	protected internal override bool IsStandingPointNotUsedOnAccountOfBeingAmmoLoad(StandingPoint standingPoint)
	{
		if (_ammoLoadPoints.Contains(standingPoint) && LoadAmmoStandingPoint != standingPoint)
		{
			return true;
		}
		return base.IsStandingPointNotUsedOnAccountOfBeingAmmoLoad(standingPoint);
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
