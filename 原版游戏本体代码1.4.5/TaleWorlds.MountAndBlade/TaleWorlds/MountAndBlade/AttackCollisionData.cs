using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Attack_collision_data", false, null)]
public struct AttackCollisionData
{
	[MarshalAs(UnmanagedType.U1)]
	private bool _attackBlockedWithShield;

	[MarshalAs(UnmanagedType.U1)]
	private bool _correctSideShieldBlock;

	[MarshalAs(UnmanagedType.U1)]
	private bool _isAlternativeAttack;

	[MarshalAs(UnmanagedType.U1)]
	private bool _isColliderAgent;

	[MarshalAs(UnmanagedType.U1)]
	private bool _collidedWithShieldOnBack;

	[MarshalAs(UnmanagedType.U1)]
	private bool _isMissile;

	[MarshalAs(UnmanagedType.U1)]
	private bool _missileBlockedWithWeapon;

	[MarshalAs(UnmanagedType.U1)]
	private bool _missileHasPhysics;

	[MarshalAs(UnmanagedType.U1)]
	private bool _entityExists;

	[MarshalAs(UnmanagedType.U1)]
	private bool _thrustTipHit;

	[MarshalAs(UnmanagedType.U1)]
	private bool _missileGoneUnderWater;

	[MarshalAs(UnmanagedType.U1)]
	private bool _missileGoneOutOfBorder;

	[MarshalAs(UnmanagedType.U1)]
	private bool _collidedWithLastBoneSegment;

	private int _collisionResult;

	private Vec3 _weaponBlowDir;

	[CustomEngineStructMemberData(true)]
	public float BaseMagnitude;

	[CustomEngineStructMemberData(true)]
	public float MovementSpeedDamageModifier;

	[CustomEngineStructMemberData(true)]
	public int AbsorbedByArmor;

	[CustomEngineStructMemberData(true)]
	public int InflictedDamage;

	[CustomEngineStructMemberData(true)]
	public int SelfInflictedDamage;

	[MarshalAs(UnmanagedType.U1)]
	[CustomEngineStructMemberData(true)]
	public bool IsShieldBroken;

	[MarshalAs(UnmanagedType.U1)]
	[CustomEngineStructMemberData(true)]
	public bool IsSneakAttack;

	public bool AttackBlockedWithShield => _attackBlockedWithShield;

	public bool CorrectSideShieldBlock => _correctSideShieldBlock;

	public bool IsAlternativeAttack => _isAlternativeAttack;

	public bool IsColliderAgent => _isColliderAgent;

	public bool CollidedWithShieldOnBack => _collidedWithShieldOnBack;

	public bool IsMissile => _isMissile;

	public bool MissileBlockedWithWeapon => _missileBlockedWithWeapon;

	public bool MissileHasPhysics => _missileHasPhysics;

	public bool EntityExists => _entityExists;

	public bool ThrustTipHit => _thrustTipHit;

	public bool MissileGoneUnderWater => _missileGoneUnderWater;

	public bool MissileGoneOutOfBorder => _missileGoneOutOfBorder;

	public bool CollidedWithLastBoneSegment => _collidedWithLastBoneSegment;

	public bool IsHorseCharge => ChargeVelocity > 0f;

	public bool IsFallDamage => FallSpeed > 0f;

	public CombatCollisionResult CollisionResult => (CombatCollisionResult)_collisionResult;

	public int AffectorWeaponSlotOrMissileIndex { get; }

	public int StrikeType { get; }

	public int DamageType { get; }

	public sbyte CollisionBoneIndex { get; private set; }

	public BoneBodyPartType VictimHitBodyPart { get; }

	public sbyte AttackBoneIndex { get; private set; }

	public Agent.UsageDirection AttackDirection { get; }

	public int PhysicsMaterialIndex { get; private set; }

	public CombatHitResultFlags CollisionHitResultFlags { get; private set; }

	public float AttackProgress { get; }

	public float CollisionDistanceOnWeapon { get; }

	public float AttackerStunPeriod { get; set; }

	public float DefenderStunPeriod { get; set; }

	public float MissileTotalDamage { get; }

	public float MissileStartingBaseSpeed { get; }

	public float ChargeVelocity { get; }

	public float FallSpeed { get; private set; }

	public Vec3 WeaponRotUp { get; }

	public Vec3 WeaponBlowDir => _weaponBlowDir;

	public Vec3 CollisionGlobalPosition { get; private set; }

	public Vec3 MissileVelocity { get; }

	public Vec3 MissileStartingPosition { get; }

	public Vec3 VictimAgentCurVelocity { get; }

	public Vec3 CollisionGlobalNormal { get; }

	public Vec3 LastBoneSegmentRotUp { get; }

	public Vec3 LastBoneSegmentSwingDir { get; }

	public void SetCollisionBoneIndexForAreaDamage(sbyte boneIndex)
	{
		CollisionBoneIndex = boneIndex;
	}

	public void UpdateCollisionPositionAndBoneForReflect(int inflictedDamage, Vec3 position, sbyte boneIndex)
	{
		InflictedDamage = inflictedDamage;
		CollisionGlobalPosition = position;
		AttackBoneIndex = boneIndex;
	}

	private AttackCollisionData(bool attackBlockedWithShield, bool correctSideShieldBlock, bool isAlternativeAttack, bool isColliderAgent, bool collidedWithShieldOnBack, bool isMissile, bool missileBlockedWithWeapon, bool missileHasPhysics, bool entityExists, bool thrustTipHit, bool missileGoneUnderWater, bool missileGoneOutOfBorder, bool collidedWithLastBoneSegment, CombatCollisionResult collisionResult, int affectorWeaponSlotOrMissileIndex, int StrikeType, int DamageType, sbyte CollisionBoneIndex, BoneBodyPartType VictimHitBodyPart, sbyte AttackBoneIndex, Agent.UsageDirection AttackDirection, int PhysicsMaterialIndex, CombatHitResultFlags CollisionHitResultFlags, float AttackProgress, float CollisionDistanceOnWeapon, float AttackerStunPeriod, float DefenderStunPeriod, float MissileTotalDamage, float MissileStartingBaseSpeed, float ChargeVelocity, float FallSpeed, Vec3 WeaponRotUp, Vec3 weaponBlowDir, Vec3 CollisionGlobalPosition, Vec3 MissileVelocity, Vec3 MissileStartingPosition, Vec3 VictimAgentCurVelocity, Vec3 GroundNormal, Vec3 LastBoneSegmentRotUp, Vec3 LastBoneSegmentSwingDir)
	{
		_attackBlockedWithShield = attackBlockedWithShield;
		_correctSideShieldBlock = correctSideShieldBlock;
		_isAlternativeAttack = isAlternativeAttack;
		_isColliderAgent = isColliderAgent;
		_collidedWithShieldOnBack = collidedWithShieldOnBack;
		_isMissile = isMissile;
		_missileBlockedWithWeapon = missileBlockedWithWeapon;
		_missileHasPhysics = missileHasPhysics;
		_entityExists = entityExists;
		_thrustTipHit = thrustTipHit;
		_missileGoneUnderWater = missileGoneUnderWater;
		_missileGoneOutOfBorder = missileGoneOutOfBorder;
		_collidedWithLastBoneSegment = collidedWithLastBoneSegment;
		_collisionResult = (int)collisionResult;
		AffectorWeaponSlotOrMissileIndex = affectorWeaponSlotOrMissileIndex;
		this.StrikeType = StrikeType;
		this.DamageType = DamageType;
		this.CollisionBoneIndex = CollisionBoneIndex;
		this.VictimHitBodyPart = VictimHitBodyPart;
		this.AttackBoneIndex = AttackBoneIndex;
		this.AttackDirection = AttackDirection;
		this.PhysicsMaterialIndex = PhysicsMaterialIndex;
		this.CollisionHitResultFlags = CollisionHitResultFlags;
		this.AttackProgress = AttackProgress;
		this.CollisionDistanceOnWeapon = CollisionDistanceOnWeapon;
		this.AttackerStunPeriod = AttackerStunPeriod;
		this.DefenderStunPeriod = DefenderStunPeriod;
		this.MissileTotalDamage = MissileTotalDamage;
		this.MissileStartingBaseSpeed = MissileStartingBaseSpeed;
		this.ChargeVelocity = ChargeVelocity;
		this.FallSpeed = FallSpeed;
		this.WeaponRotUp = WeaponRotUp;
		_weaponBlowDir = weaponBlowDir;
		this.CollisionGlobalPosition = CollisionGlobalPosition;
		this.MissileVelocity = MissileVelocity;
		this.MissileStartingPosition = MissileStartingPosition;
		this.VictimAgentCurVelocity = VictimAgentCurVelocity;
		CollisionGlobalNormal = GroundNormal;
		this.LastBoneSegmentRotUp = LastBoneSegmentRotUp;
		this.LastBoneSegmentSwingDir = LastBoneSegmentSwingDir;
		BaseMagnitude = 0f;
		MovementSpeedDamageModifier = 0f;
		AbsorbedByArmor = 0;
		InflictedDamage = 0;
		SelfInflictedDamage = 0;
		IsShieldBroken = false;
		IsSneakAttack = false;
	}

	public static AttackCollisionData GetAttackCollisionDataForDebugPurpose(bool _attackBlockedWithShield, bool _correctSideShieldBlock, bool _isAlternativeAttack, bool _isColliderAgent, bool _collidedWithShieldOnBack, bool _isMissile, bool _isMissileBlockedWithWeapon, bool _missileHasPhysics, bool _entityExists, bool _thrustTipHit, bool _missileGoneUnderWater, bool _missileGoneOutOfBorder, CombatCollisionResult collisionResult, int affectorWeaponSlotOrMissileIndex, int StrikeType, int DamageType, sbyte CollisionBoneIndex, BoneBodyPartType VictimHitBodyPart, sbyte AttackBoneIndex, Agent.UsageDirection AttackDirection, int PhysicsMaterialIndex, CombatHitResultFlags CollisionHitResultFlags, float AttackProgress, float CollisionDistanceOnWeapon, float AttackerStunPeriod, float DefenderStunPeriod, float MissileTotalDamage, float MissileInitialSpeed, float ChargeVelocity, float FallSpeed, Vec3 WeaponRotUp, Vec3 _weaponBlowDir, Vec3 CollisionGlobalPosition, Vec3 MissileVelocity, Vec3 MissileStartingPosition, Vec3 VictimAgentCurVelocity, Vec3 GroundNormal)
	{
		return new AttackCollisionData(_attackBlockedWithShield, _correctSideShieldBlock, _isAlternativeAttack, _isColliderAgent, _collidedWithShieldOnBack, _isMissile, _isMissileBlockedWithWeapon, _missileHasPhysics, _entityExists, _thrustTipHit, _missileGoneUnderWater, _missileGoneOutOfBorder, collidedWithLastBoneSegment: false, collisionResult, affectorWeaponSlotOrMissileIndex, StrikeType, DamageType, CollisionBoneIndex, VictimHitBodyPart, AttackBoneIndex, AttackDirection, PhysicsMaterialIndex, CollisionHitResultFlags, AttackProgress, CollisionDistanceOnWeapon, AttackerStunPeriod, DefenderStunPeriod, MissileTotalDamage, MissileInitialSpeed, ChargeVelocity, FallSpeed, WeaponRotUp, _weaponBlowDir, CollisionGlobalPosition, MissileVelocity, MissileStartingPosition, VictimAgentCurVelocity, GroundNormal, Vec3.Zero, Vec3.Zero);
	}
}
