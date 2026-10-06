using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers;

public static class ItemHelper
{
	public static bool IsWeaponComparableWithUsage(ItemObject item, string comparedUsageId)
	{
		for (int i = 0; i < item.Weapons.Count; i++)
		{
			if (item.Weapons[i].WeaponDescriptionId == comparedUsageId || (comparedUsageId == "OneHandedBastardSword" && item.Weapons[i].WeaponDescriptionId == "OneHandedSword") || (comparedUsageId == "OneHandedSword" && item.Weapons[i].WeaponDescriptionId == "OneHandedBastardSword"))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsWeaponComparableWithUsage(ItemObject item, string comparedUsageId, out int comparableUsageIndex)
	{
		comparableUsageIndex = -1;
		for (int i = 0; i < item.Weapons.Count; i++)
		{
			if (item.Weapons[i].WeaponDescriptionId == comparedUsageId || (comparedUsageId == "OneHandedBastardSword" && item.Weapons[i].WeaponDescriptionId == "OneHandedSword") || (comparedUsageId == "OneHandedSword" && item.Weapons[i].WeaponDescriptionId == "OneHandedBastardSword"))
			{
				comparableUsageIndex = i;
				return true;
			}
		}
		return false;
	}

	public static bool CheckComparability(ItemObject item, ItemObject comparedItem)
	{
		if (item == null || comparedItem == null)
		{
			return false;
		}
		if (item.PrimaryWeapon != null && comparedItem.PrimaryWeapon != null && ((item.PrimaryWeapon.IsMeleeWeapon && comparedItem.PrimaryWeapon.IsMeleeWeapon) || (item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (!item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && !comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (item.PrimaryWeapon.IsShield && comparedItem.PrimaryWeapon.IsShield)))
		{
			WeaponComponentData primaryWeapon = item.PrimaryWeapon;
			return IsWeaponComparableWithUsage(comparedItem, primaryWeapon.WeaponDescriptionId);
		}
		return item.Type == comparedItem.Type;
	}

	public static bool CheckComparability(ItemObject item, ItemObject comparedItem, int usageIndex)
	{
		if (item == null || comparedItem == null)
		{
			return false;
		}
		if (item.PrimaryWeapon != null && ((item.PrimaryWeapon.IsMeleeWeapon && comparedItem.PrimaryWeapon.IsMeleeWeapon) || (item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (!item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && !comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (item.PrimaryWeapon.IsShield && comparedItem.PrimaryWeapon.IsShield)))
		{
			WeaponComponentData weaponComponentData = item.Weapons[usageIndex];
			return IsWeaponComparableWithUsage(comparedItem, weaponComponentData.WeaponDescriptionId);
		}
		return item.Type == comparedItem.Type;
	}

	private static TextObject GetDamageDescription(int damage, DamageTypes damageType)
	{
		TextObject textObject = new TextObject("{=vvCwVo7i}{DAMAGE} {DAMAGE_TYPE}");
		textObject.SetTextVariable("DAMAGE", damage);
		textObject.SetTextVariable("DAMAGE_TYPE", GameTexts.FindText("str_damage_types", damageType.ToString()));
		return textObject;
	}

	public static TextObject GetSwingDamageText(WeaponComponentData weapon, ItemModifier itemModifier)
	{
		int modifiedSwingDamage = weapon.GetModifiedSwingDamage(itemModifier);
		DamageTypes swingDamageType = weapon.SwingDamageType;
		return GetDamageDescription(modifiedSwingDamage, swingDamageType);
	}

	public static TextObject GetMissileDamageText(WeaponComponentData weapon, ItemModifier itemModifier)
	{
		int modifiedMissileDamage = weapon.GetModifiedMissileDamage(itemModifier);
		DamageTypes damageType = ((weapon.WeaponClass == WeaponClass.ThrowingAxe) ? weapon.SwingDamageType : weapon.ThrustDamageType);
		return GetDamageDescription(modifiedMissileDamage, damageType);
	}

	public static TextObject GetThrustDamageText(WeaponComponentData weapon, ItemModifier itemModifier)
	{
		int modifiedThrustDamage = weapon.GetModifiedThrustDamage(itemModifier);
		DamageTypes thrustDamageType = weapon.ThrustDamageType;
		return GetDamageDescription(modifiedThrustDamage, thrustDamageType);
	}

	public static TextObject NumberOfItems(int number, ItemObject item)
	{
		TextObject textObject = new TextObject("{=siWNDxgo}{.%}{?NUMBER_OF_ITEM > 1}{NUMBER_OF_ITEM} {PLURAL(ITEM)}{?}one {ITEM}{\\?}{.%}");
		textObject.SetTextVariable("ITEM", item.Name);
		textObject.SetTextVariable("NUMBER_OF_ITEM", number);
		return textObject;
	}
}
