using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace TaleWorlds.MountAndBlade;

public class Ballista : RangedSiegeWeapon, ISpawnable
{
	public string NavelTag = "BallistaNavel";

	public string BodyTag = "BallistaBody";

	public string SkeletonTag = "SkeletonEntity";

	public float AnimationHeightDifference;

	private MatrixFrame _ballistaBodyInitialLocalFrame;

	private MatrixFrame _ballistaNavelInitialFrame;

	private MatrixFrame _pilotInitialLocalFrame;

	private MatrixFrame _pilotInitialLocalIKFrame;

	private MatrixFrame _missileInitialLocalFrame;

	[EditableScriptComponentVariable(true, "")]
	protected string IdleActionName = "act_usage_ballista_idle_attacker";

	[EditableScriptComponentVariable(true, "")]
	protected string ReloadActionName = "act_usage_ballista_reload_attacker";

	[EditableScriptComponentVariable(true, "")]
	protected string PlaceAmmoStartActionName = "act_usage_ballista_ammo_place_start_attacker";

	[EditableScriptComponentVariable(true, "")]
	protected string PlaceAmmoEndActionName = "act_usage_ballista_ammo_place_end_attacker";

	[EditableScriptComponentVariable(true, "")]
	protected string PickUpAmmoStartActionName = "act_usage_ballista_ammo_pick_up_start_attacker";

	[EditableScriptComponentVariable(true, "")]
	protected string PickUpAmmoEndActionName = "act_usage_ballista_ammo_pick_up_end_attacker";

	private ActionIndexCache _idleAnimationActionIndex;

	private ActionIndexCache _reloadAnimationActionIndex;

	private ActionIndexCache _placeAmmoStartAnimationActionIndex;

	private ActionIndexCache _placeAmmoEndAnimationActionIndex;

	private ActionIndexCache _pickUpAmmoStartAnimationActionIndex;

	private ActionIndexCache _pickUpAmmoEndAnimationActionIndex;

	[EditableScriptComponentVariable(true, "")]
	public float HorizontalDirectionRestriction = System.MathF.PI / 2f;

	public float BallistaShootingSpeed = 120f;

	private WeaponState _changeToState = WeaponState.Invalid;

	protected SynchedMissionObject ballistaBody { get; private set; }

	protected SynchedMissionObject ballistaNavel { get; private set; }

	public override float DirectionRestriction => HorizontalDirectionRestriction;

	protected override float ShootingSpeed => BallistaShootingSpeed;

	public override Vec3 CanShootAtPointCheckingOffset => new Vec3(0f, 0f, 0.5f);

	protected override bool WeaponMovesDownToReload => true;

	public override string MultipleProjectileId => "ballista_c_projectile_grape";

	public override string MultipleProjectileFlyingId => "ballista_c_projectile_grape_projectile";

	protected override float MaximumBallisticError => 0.5f;

	protected override float HorizontalAimSensitivity => 1f;

	protected override float VerticalAimSensitivity => 1f;

	protected override void RegisterAnimationParameters()
	{
		SkeletonOwnerObjects = new SynchedMissionObject[1];
		Skeletons = new Skeleton[1];
		List<SynchedMissionObject> list = ballistaBody.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(SkeletonTag);
		if (list.Count == 0)
		{
			SkeletonOwnerObjects[0] = ballistaBody;
		}
		else
		{
			SkeletonOwnerObjects[0] = list[0];
		}
		Skeletons[0] = SkeletonOwnerObjects[0].GameEntity.Skeleton;
		base.SkeletonName = "ballista_skeleton";
		base.FireAnimation = "ballista_fire";
		base.FireAnimationIndex = MBAnimation.GetAnimationIndexWithName("ballista_fire");
		base.SetUpAnimation = "ballista_set_up";
		base.SetUpAnimationIndex = MBAnimation.GetAnimationIndexWithName("ballista_set_up");
		_idleAnimationActionIndex = ActionIndexCache.Create(IdleActionName);
		_reloadAnimationActionIndex = ActionIndexCache.Create(ReloadActionName);
		_placeAmmoStartAnimationActionIndex = ActionIndexCache.Create(PlaceAmmoStartActionName);
		_placeAmmoEndAnimationActionIndex = ActionIndexCache.Create(PlaceAmmoEndActionName);
		_pickUpAmmoStartAnimationActionIndex = ActionIndexCache.Create(PickUpAmmoStartActionName);
		_pickUpAmmoEndAnimationActionIndex = ActionIndexCache.Create(PickUpAmmoEndActionName);
	}

	public override SiegeEngineType GetSiegeEngineType()
	{
		return DefaultSiegeEngineTypes.Ballista;
	}

	protected internal override void OnInit()
	{
		ballistaBody = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(BodyTag)[0];
		ballistaNavel = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(NavelTag)[0];
		RotationObject = this;
		base.OnInit();
		UsesMouseForAiming = true;
		GetSoundEventIndices();
		_ballistaNavelInitialFrame = ballistaNavel.GameEntity.GetFrame();
		MatrixFrame m = ballistaBody.GameEntity.GetGlobalFrame();
		_ballistaBodyInitialLocalFrame = ballistaBody.GameEntity.GetFrame();
		MatrixFrame globalFrame = base.PilotStandingPoint.GameEntity.GetGlobalFrame();
		_pilotInitialLocalFrame = base.PilotStandingPoint.GameEntity.GetFrame();
		_pilotInitialLocalIKFrame = globalFrame.TransformToLocal(in m);
		_missileInitialLocalFrame = base.Projectile.GameEntity.GetFrame();
		base.PilotStandingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
		MissileStartingPositionEntityForSimulation = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.Projectile.GameEntity.Parent.GetChildren().FirstOrDefault((WeakGameEntity x) => x.Name == "projectile_leaving_position"));
		EnemyRangeToStopUsing = 7f;
		AttackClickWillReload = true;
		WeaponNeedsClickToReload = true;
		SetScriptComponentToTick(GetTickRequirement());
		ApplyAimChange();
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
		return base.State != WeaponState.Shooting;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new BallistaAI(this);
	}

	protected override void OnRangedSiegeWeaponStateChange()
	{
		base.OnRangedSiegeWeaponStateChange();
		switch (base.State)
		{
		case WeaponState.WaitingBeforeProjectileLeaving:
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
			if (base.AmmoCount > 0)
			{
				if (!GameNetwork.IsClientOrReplay)
				{
					ConsumeAmmo();
				}
				else
				{
					SetAmmo(base.AmmoCount - 1);
				}
			}
			break;
		}
	}

	protected override void HandleUserAiming(float dt)
	{
		if (base.PilotAgent == null)
		{
			TargetReleaseAngle = 0f;
		}
		base.HandleUserAiming(dt);
	}

	protected override void ApplyAimChange()
	{
		MatrixFrame frame = _ballistaNavelInitialFrame;
		frame.rotation.RotateAboutAnArbitraryVector(in _ballistaNavelInitialFrame.rotation.u, CurrentDirection);
		ballistaNavel.GameEntity.SetLocalFrame(ref frame, isTeleportation: false);
		MatrixFrame frame2 = frame.TransformToParent(_ballistaNavelInitialFrame.TransformToLocal(in _pilotInitialLocalFrame));
		base.PilotStandingPoint.GameEntity.SetLocalFrame(ref frame2, isTeleportation: false);
		MatrixFrame frame3 = _ballistaBodyInitialLocalFrame;
		frame3.rotation.RotateAboutAnArbitraryVector(in frame3.rotation.s, 0f - CurrentReleaseAngle);
		ballistaBody.GameEntity.SetLocalFrame(ref frame3, isTeleportation: false);
	}

	protected override void ApplyCurrentDirectionToEntity()
	{
		ApplyAimChange();
	}

	protected override void GetSoundEventIndices()
	{
		MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/ballista/move");
		ReloadSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/ballista/reload");
		FireSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/ballista/fire");
	}

	protected internal override bool IsTargetValid(ITargetable target)
	{
		return !(target is ICastleKeyPosition);
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
		if (_changeToState != WeaponState.Invalid)
		{
			base.State = _changeToState;
			_changeToState = WeaponState.Invalid;
		}
	}

	protected internal override void OnTickParallel(float dt)
	{
		base.OnTickParallel(dt);
		if (!base.GameEntity.IsVisibleIncludeParents())
		{
			return;
		}
		if (base.PilotAgent != null)
		{
			base.PilotAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(in _pilotInitialLocalIKFrame, ballistaBody.GameEntity.GetGlobalFrame(), AnimationHeightDifference);
			ActionIndexCache currentAction = base.PilotAgent.GetCurrentAction(1);
			if (currentAction == _pickUpAmmoEndAnimationActionIndex || currentAction == _placeAmmoStartAnimationActionIndex)
			{
				MatrixFrame frame = base.PilotAgent.Frame.TransformToParent(base.PilotAgent.GetBoneEntitialFrame(base.PilotAgent.Monster.MainHandItemBoneIndex, useBoneMapping: false));
				base.Projectile.GameEntity.SetGlobalFrame(in frame);
			}
			else
			{
				base.Projectile.GameEntity.SetFrame(ref _missileInitialLocalFrame);
			}
		}
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		switch (base.State)
		{
		case WeaponState.Reloading:
			FinalReloadSpeed = MissionGameModels.Current.MissionSiegeEngineCalculationModel.CalculateReloadSpeed(base.PilotAgent, BaseReloadSpeed);
			if (base.PilotAgent != null && !base.PilotAgent.SetActionChannel(1, in _reloadAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL) && base.PilotAgent.Controller != AgentControllerType.AI)
			{
				base.PilotAgent.StopUsingGameObjectMT();
			}
			return;
		case WeaponState.LoadingAmmo:
		{
			bool value = false;
			if (base.PilotAgent != null)
			{
				if (!HasAmmo)
				{
					if (base.PilotAgent.Controller == AgentControllerType.AI)
					{
						base.PilotAgent.StopUsingGameObjectMT();
					}
					return;
				}
				ActionIndexCache currentAction2 = base.PilotAgent.GetCurrentAction(1);
				FinalReloadSpeed = MissionGameModels.Current.MissionSiegeEngineCalculationModel.CalculateReloadSpeed(base.PilotAgent, BaseReloadSpeed);
				base.PilotAgent.SetCurrentActionSpeed(1, FinalReloadSpeed);
				if (currentAction2 != _pickUpAmmoStartAnimationActionIndex && currentAction2 != _pickUpAmmoEndAnimationActionIndex && currentAction2 != _placeAmmoStartAnimationActionIndex && currentAction2 != _placeAmmoEndAnimationActionIndex && !base.PilotAgent.SetActionChannel(1, in _pickUpAmmoStartAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL) && base.PilotAgent.Controller != AgentControllerType.AI)
				{
					base.PilotAgent.StopUsingGameObjectMT();
				}
				else if (currentAction2 == _pickUpAmmoEndAnimationActionIndex || currentAction2 == _placeAmmoStartAnimationActionIndex)
				{
					value = true;
				}
				else if (currentAction2 == _placeAmmoEndAnimationActionIndex)
				{
					value = true;
					_changeToState = WeaponState.WaitingBeforeIdle;
				}
			}
			base.Projectile.SetVisibleSynched(value);
			return;
		}
		case WeaponState.WaitingBeforeIdle:
			if (base.PilotAgent == null)
			{
				_changeToState = WeaponState.Idle;
			}
			else if (base.PilotAgent.GetCurrentAction(1) != _placeAmmoEndAnimationActionIndex)
			{
				if (base.PilotAgent.Controller != AgentControllerType.AI)
				{
					base.PilotAgent.StopUsingGameObjectMT();
				}
				_changeToState = WeaponState.Idle;
			}
			else if (base.PilotAgent.GetCurrentActionProgress(1) > 0.9999f)
			{
				_changeToState = WeaponState.Idle;
				if (base.PilotAgent != null && !base.PilotAgent.SetActionChannel(1, in _idleAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL) && base.PilotAgent.Controller != AgentControllerType.AI)
				{
					base.PilotAgent.StopUsingGameObjectMT();
				}
			}
			return;
		}
		if (base.PilotAgent == null)
		{
			return;
		}
		if (base.PilotAgent.IsInBeingStruckAction)
		{
			if (base.PilotAgent.GetCurrentAction(1) != ActionIndexCache.act_strike_bent_over)
			{
				base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_strike_bent_over, ignorePriority: false, (AnimFlags)0uL);
			}
		}
		else if (!base.PilotAgent.SetActionChannel(1, in _idleAnimationActionIndex, ignorePriority: false, (AnimFlags)0uL) && base.PilotAgent.Controller != AgentControllerType.AI)
		{
			base.PilotAgent.StopUsingGameObjectMT();
		}
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=abbALYlp}Ballista");
	}

	protected override void UpdateAmmoMesh()
	{
		int num = 8 - base.AmmoCount;
		base.GameEntity.SetVectorArgument(0f, num, 0f, 0f);
	}

	public override float ProcessTargetValue(float baseValue, TargetFlags flags)
	{
		if (flags.HasAnyFlag(TargetFlags.NotAThreat))
		{
			return -1000f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
		{
			baseValue *= 0.2f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsStructure))
		{
			baseValue *= 0.05f;
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
		if (Side == BattleSideEnum.Attacker)
		{
			targetFlags |= TargetFlags.IsAttacker;
		}
		targetFlags |= TargetFlags.IsSmall;
		if (base.IsDestroyed || IsDeactivated)
		{
			targetFlags |= TargetFlags.NotAThreat;
		}
		if (Side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToBallistae)
		{
			targetFlags |= TargetFlags.DebugThreat;
		}
		if (Side == BattleSideEnum.Defender && DebugSiegeBehavior.DebugAttackState == DebugSiegeBehavior.DebugStateAttacker.DebugAttackersToBallistae)
		{
			targetFlags |= TargetFlags.DebugThreat;
		}
		return targetFlags;
	}

	public override float GetTargetValue(List<Vec3> weaponPos)
	{
		return 30f * GetUserMultiplierOfWeapon() * GetDistanceMultiplierOfWeapon(weaponPos[0]) * GetHitPointMultiplierOfWeapon();
	}

	public void SetSpawnedFromSpawner()
	{
		_spawnedFromSpawner = true;
	}
}
