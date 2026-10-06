using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Blow", false, null)]
public struct Blow
{
	public BlowWeaponRecord WeaponRecord;

	public Vec3 GlobalPosition;

	public Vec3 Direction;

	public Vec3 SwingDirection;

	public int InflictedDamage;

	public int SelfInflictedDamage;

	public float BaseMagnitude;

	public float DefenderStunPeriod;

	public float AttackerStunPeriod;

	public float AbsorbedByArmor;

	public float MovementSpeedDamageModifier;

	public StrikeType StrikeType;

	public AgentAttackType AttackType;

	[CustomEngineStructMemberData("blow_flags")]
	public BlowFlags BlowFlag;

	public int OwnerId;

	public sbyte BoneIndex;

	public BoneBodyPartType VictimBodyPart;

	public DamageTypes DamageType;

	[MarshalAs(UnmanagedType.U1)]
	public bool NoIgnore;

	[MarshalAs(UnmanagedType.U1)]
	public bool DamageCalculated;

	[MarshalAs(UnmanagedType.U1)]
	public bool IsFallDamage;

	public float DamagedPercentage;

	public bool IsMissile => WeaponRecord.IsMissile;

	public Blow(int ownerId)
	{
		this = default(Blow);
		OwnerId = ownerId;
	}

	public bool IsBlowCrit(int maxHitPointsOfVictim)
	{
		return (float)InflictedDamage > (float)maxHitPointsOfVictim * 0.5f;
	}

	public bool IsBlowLow(int maxHitPointsOfVictim)
	{
		return (float)InflictedDamage <= (float)maxHitPointsOfVictim * 0.1f;
	}

	public bool IsHeadShot()
	{
		return VictimBodyPart == BoneBodyPartType.Head;
	}
}
