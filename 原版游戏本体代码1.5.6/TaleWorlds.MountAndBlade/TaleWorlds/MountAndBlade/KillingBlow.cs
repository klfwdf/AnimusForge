using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Killing_blow", false, null)]
public struct KillingBlow(Blow b, Vec3 ragdollImpulsePoint, Vec3 ragdollImpulseAmount, int deathAction, int weaponItemKind, Agent.KillInfo overrideKillInfo = Agent.KillInfo.Invalid)
{
	public Vec3 RagdollImpulseLocalPoint = ragdollImpulsePoint;

	public Vec3 RagdollImpulseAmount = ragdollImpulseAmount;

	public int DeathAction = deathAction;

	public DamageTypes DamageType = b.DamageType;

	public AgentAttackType AttackType = b.AttackType;

	public int OwnerId = b.OwnerId;

	public BoneBodyPartType VictimBodyPart = b.VictimBodyPart;

	public int WeaponClass = (int)b.WeaponRecord.WeaponClass;

	public Agent.KillInfo OverrideKillInfo = overrideKillInfo;

	public Vec3 BlowPosition = b.GlobalPosition;

	public WeaponFlags WeaponRecordWeaponFlags = b.WeaponRecord.WeaponFlags;

	public int WeaponItemKind = weaponItemKind;

	public int InflictedDamage = b.InflictedDamage;

	[MarshalAs(UnmanagedType.U1)]
	public bool IsMissile = b.IsMissile;

	[MarshalAs(UnmanagedType.U1)]
	public bool IsValid = true;

	public bool IsHeadShot()
	{
		return VictimBodyPart == BoneBodyPartType.Head;
	}
}
