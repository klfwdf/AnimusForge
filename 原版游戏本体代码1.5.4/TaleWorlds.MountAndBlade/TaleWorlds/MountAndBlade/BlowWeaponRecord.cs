using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Blow_weapon_record", false, null)]
public struct BlowWeaponRecord
{
	public Vec3 StartingPosition;

	public Vec3 CurrentPosition;

	public Vec3 Velocity;

	public ItemFlags ItemFlags;

	public WeaponFlags WeaponFlags;

	public WeaponClass WeaponClass;

	public sbyte BoneNoToAttach;

	public int AffectorWeaponSlotOrMissileIndex;

	public float Weight;

	[MarshalAs(UnmanagedType.U1)]
	[CustomEngineStructMemberData(true)]
	private bool _isMissile;

	[MarshalAs(UnmanagedType.U1)]
	private bool _isMaterialMetal;

	public bool IsMissile => _isMissile;

	public bool IsShield
	{
		get
		{
			if (!WeaponFlags.HasAnyFlag(WeaponFlags.WeaponMask))
			{
				return WeaponFlags.HasAllFlags(WeaponFlags.HasHitPoints | WeaponFlags.CanBlockRanged);
			}
			return false;
		}
	}

	public bool IsRanged => WeaponFlags.HasAnyFlag(WeaponFlags.RangedWeapon);

	public bool IsAmmo
	{
		get
		{
			if (!WeaponFlags.HasAnyFlag(WeaponFlags.WeaponMask))
			{
				return WeaponFlags.HasAnyFlag(WeaponFlags.Consumable);
			}
			return false;
		}
	}

	public void FillAsMeleeBlow(ItemObject item, WeaponComponentData weaponComponentData, int affectorWeaponSlot, sbyte weaponAttachBoneIndex)
	{
		_isMissile = false;
		if (weaponComponentData != null)
		{
			ItemFlags = item.ItemFlags;
			WeaponFlags = weaponComponentData.WeaponFlags;
			WeaponClass = weaponComponentData.WeaponClass;
			BoneNoToAttach = weaponAttachBoneIndex;
			AffectorWeaponSlotOrMissileIndex = affectorWeaponSlot;
			Weight = item.Weight;
			_isMaterialMetal = weaponComponentData.PhysicsMaterial.Contains("metal");
		}
		else
		{
			_isMaterialMetal = false;
			AffectorWeaponSlotOrMissileIndex = -1;
		}
	}

	public void FillAsMissileBlow(ItemObject item, WeaponComponentData weaponComponentData, int missileIndex, sbyte weaponAttachBoneIndex, Vec3 startingPosition, Vec3 currentPosition, Vec3 velocity)
	{
		_isMissile = true;
		StartingPosition = startingPosition;
		CurrentPosition = currentPosition;
		Velocity = velocity;
		ItemFlags = item.ItemFlags;
		WeaponFlags = weaponComponentData.WeaponFlags;
		WeaponClass = weaponComponentData.WeaponClass;
		BoneNoToAttach = weaponAttachBoneIndex;
		AffectorWeaponSlotOrMissileIndex = missileIndex;
		Weight = item.Weight;
		_isMaterialMetal = weaponComponentData.PhysicsMaterial.Contains("metal");
	}

	public bool HasWeapon()
	{
		return AffectorWeaponSlotOrMissileIndex >= 0;
	}

	public int GetHitSound(bool isOwnerHumanoid, bool isCriticalBlow, bool isLowBlow, bool isNonTipThrust, AgentAttackType attackType, DamageTypes damageType)
	{
		int result;
		if (!HasWeapon())
		{
			result = ((!isOwnerHumanoid) ? CombatSoundContainer.SoundCodeMissionCombatChargeDamage : ((attackType == AgentAttackType.Kick) ? CombatSoundContainer.SoundCodeMissionCombatKick : (isCriticalBlow ? CombatSoundContainer.SoundCodeMissionCombatPunchHigh : ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatPunchMed : CombatSoundContainer.SoundCodeMissionCombatPunchLow))));
		}
		else if (IsRanged || IsAmmo)
		{
			switch (WeaponClass)
			{
			case WeaponClass.ThrowingAxe:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatThrowingAxeMed : CombatSoundContainer.SoundCodeMissionCombatThrowingAxeLow) : CombatSoundContainer.SoundCodeMissionCombatThrowingAxeHigh);
				break;
			case WeaponClass.ThrowingKnife:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatThrowingDaggerMed : CombatSoundContainer.SoundCodeMissionCombatThrowingDaggerLow) : CombatSoundContainer.SoundCodeMissionCombatThrowingDaggerHigh);
				break;
			case WeaponClass.Javelin:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatMissileMed : CombatSoundContainer.SoundCodeMissionCombatMissileLow) : CombatSoundContainer.SoundCodeMissionCombatMissileHigh);
				break;
			case WeaponClass.Sling:
			case WeaponClass.Stone:
			case WeaponClass.BallistaStone:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatThrowingStoneMed : CombatSoundContainer.SoundCodeMissionCombatThrowingStoneLow) : CombatSoundContainer.SoundCodeMissionCombatThrowingStoneHigh);
				break;
			case WeaponClass.Boulder:
			case WeaponClass.BallistaBoulder:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatBoulderMed : CombatSoundContainer.SoundCodeMissionCombatBoulderLow) : CombatSoundContainer.SoundCodeMissionCombatBoulderHigh);
				break;
			default:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatMissileMed : CombatSoundContainer.SoundCodeMissionCombatMissileLow) : CombatSoundContainer.SoundCodeMissionCombatMissileHigh);
				break;
			}
		}
		else if (IsShield)
		{
			result = ((!_isMaterialMetal) ? CombatSoundContainer.SoundCodeMissionCombatWoodShieldBash : CombatSoundContainer.SoundCodeMissionCombatMetalShieldBash);
		}
		else if (attackType == AgentAttackType.Bash)
		{
			result = CombatSoundContainer.SoundCodeMissionCombatBluntLow;
		}
		else
		{
			if (isNonTipThrust)
			{
				damageType = DamageTypes.Blunt;
			}
			switch (damageType)
			{
			case DamageTypes.Cut:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatCutMed : CombatSoundContainer.SoundCodeMissionCombatCutLow) : CombatSoundContainer.SoundCodeMissionCombatCutHigh);
				break;
			case DamageTypes.Pierce:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatPierceMed : CombatSoundContainer.SoundCodeMissionCombatPierceLow) : CombatSoundContainer.SoundCodeMissionCombatPierceHigh);
				break;
			case DamageTypes.Blunt:
				result = ((!isCriticalBlow) ? ((!isLowBlow) ? CombatSoundContainer.SoundCodeMissionCombatBluntMed : CombatSoundContainer.SoundCodeMissionCombatBluntLow) : CombatSoundContainer.SoundCodeMissionCombatBluntHigh);
				break;
			default:
				result = CombatSoundContainer.SoundCodeMissionCombatBluntMed;
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\BlowWeaponRecord.cs", "GetHitSound", 250);
				break;
			}
		}
		return result;
	}
}
