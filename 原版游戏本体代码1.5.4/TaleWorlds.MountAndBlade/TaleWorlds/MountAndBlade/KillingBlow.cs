using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Killing_blow", false, null)]
public struct KillingBlow
{
	public Vec3 RagdollImpulseLocalPoint;

	public Vec3 RagdollImpulseAmount;

	public int DeathAction;

	public DamageTypes DamageType;

	public AgentAttackType AttackType;

	public int OwnerId;

	public BoneBodyPartType VictimBodyPart;

	public int WeaponClass;

	public Agent.KillInfo OverrideKillInfo;

	public Vec3 BlowPosition;

	public WeaponFlags WeaponRecordWeaponFlags;

	public int WeaponItemKind;

	public int InflictedDamage;

	[MarshalAs(UnmanagedType.U1)]
	public bool IsMissile;

	[MarshalAs(UnmanagedType.U1)]
	public bool IsValid;

	public KillingBlow(Blow b, Vec3 ragdollImpulsePoint, Vec3 ragdollImpulseAmount, int deathAction, int weaponItemKind, Agent.KillInfo overrideKillInfo = Agent.KillInfo.Invalid)
	{
		RagdollImpulseLocalPoint = ragdollImpulsePoint;
		RagdollImpulseAmount = ragdollImpulseAmount;
		DeathAction = deathAction;
		OverrideKillInfo = overrideKillInfo;
		DamageType = b.DamageType;
		AttackType = b.AttackType;
		OwnerId = b.OwnerId;
		VictimBodyPart = b.VictimBodyPart;
		WeaponClass = (int)b.WeaponRecord.WeaponClass;
		BlowPosition = b.GlobalPosition;
		WeaponRecordWeaponFlags = b.WeaponRecord.WeaponFlags;
		WeaponItemKind = weaponItemKind;
		InflictedDamage = b.InflictedDamage;
		IsMissile = b.IsMissile;
		IsValid = true;
	}

	public bool IsHeadShot()
	{
		return VictimBodyPart == BoneBodyPartType.Head;
	}
}
