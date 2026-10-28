using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class MPCombatPerkEffect : MPPerkEffect
{
	protected enum HitType
	{
		Any,
		Melee,
		Ranged
	}

	protected HitType EffectHitType;

	protected DamageTypes? DamageType;

	protected WeaponClass? WeaponClass;

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["hit_type"]?.Value;
		EffectHitType = HitType.Any;
		if (text != null && !Enum.TryParse<HitType>(text, ignoreCase: true, out EffectHitType))
		{
			EffectHitType = HitType.Any;
			Debug.FailedAssert("provided 'hit_type' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\MPCombatPerkEffect.cs", "Deserialize", 31);
		}
		string text2 = node?.Attributes?["damage_type"]?.Value;
		DamageTypes result;
		if (text2 == null || text2.ToLower() == "any")
		{
			DamageType = null;
		}
		else if (Enum.TryParse<DamageTypes>(text2, ignoreCase: true, out result))
		{
			DamageType = result;
		}
		else
		{
			Debug.FailedAssert("provided 'damage_type' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\MPCombatPerkEffect.cs", "Deserialize", 47);
			DamageType = null;
		}
		string text3 = node?.Attributes?["weapon_class"]?.Value;
		if (text3 == null || text3.ToLower() == "any")
		{
			WeaponClass = null;
			return;
		}
		if (Enum.TryParse<WeaponClass>(text3, ignoreCase: true, out var result2))
		{
			WeaponClass = result2;
			return;
		}
		Debug.FailedAssert("provided 'weapon_class' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\MPCombatPerkEffect.cs", "Deserialize", 65);
		WeaponClass = null;
	}

	protected bool IsSatisfied(WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		if ((!DamageType.HasValue || DamageType.Value == damageType) && (!WeaponClass.HasValue || WeaponClass.Value == attackerWeapon?.WeaponClass))
		{
			switch (EffectHitType)
			{
			case HitType.Any:
				return true;
			case HitType.Melee:
				return !IsWeaponRanged(attackerWeapon);
			case HitType.Ranged:
				return IsWeaponRanged(attackerWeapon);
			}
		}
		return false;
	}

	protected bool IsWeaponRanged(WeaponComponentData attackerWeapon)
	{
		if (attackerWeapon != null)
		{
			if (!attackerWeapon.IsConsumable)
			{
				return attackerWeapon.IsRangedWeapon;
			}
			return true;
		}
		return false;
	}
}
