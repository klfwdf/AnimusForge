using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x0200000A RID: 10
	public static class ItemHelper
	{
		// Token: 0x06000048 RID: 72 RVA: 0x000051F4 File Offset: 0x000033F4
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

		// Token: 0x06000049 RID: 73 RVA: 0x0000528C File Offset: 0x0000348C
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

		// Token: 0x0600004A RID: 74 RVA: 0x00005328 File Offset: 0x00003528
		public static bool CheckComparability(ItemObject item, ItemObject comparedItem)
		{
			if (item == null || comparedItem == null)
			{
				return false;
			}
			if (item.PrimaryWeapon != null && comparedItem.PrimaryWeapon != null && ((item.PrimaryWeapon.IsMeleeWeapon && comparedItem.PrimaryWeapon.IsMeleeWeapon) || (item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (!item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && !comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (item.PrimaryWeapon.IsShield && comparedItem.PrimaryWeapon.IsShield)))
			{
				WeaponComponentData primaryWeapon = item.PrimaryWeapon;
				return ItemHelper.IsWeaponComparableWithUsage(comparedItem, primaryWeapon.WeaponDescriptionId);
			}
			return item.Type == comparedItem.Type;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00005414 File Offset: 0x00003614
		public static bool CheckComparability(ItemObject item, ItemObject comparedItem, int usageIndex)
		{
			if (item == null || comparedItem == null)
			{
				return false;
			}
			if (item.PrimaryWeapon != null && ((item.PrimaryWeapon.IsMeleeWeapon && comparedItem.PrimaryWeapon.IsMeleeWeapon) || (item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (!item.PrimaryWeapon.IsRangedWeapon && item.PrimaryWeapon.IsConsumable && !comparedItem.PrimaryWeapon.IsRangedWeapon && comparedItem.PrimaryWeapon.IsConsumable) || (item.PrimaryWeapon.IsShield && comparedItem.PrimaryWeapon.IsShield)))
			{
				WeaponComponentData weaponComponentData = item.Weapons[usageIndex];
				return ItemHelper.IsWeaponComparableWithUsage(comparedItem, weaponComponentData.WeaponDescriptionId);
			}
			return item.Type == comparedItem.Type;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000054FB File Offset: 0x000036FB
		private static TextObject GetDamageDescription(int damage, DamageTypes damageType)
		{
			TextObject textObject = new TextObject("{=vvCwVo7i}{DAMAGE} {DAMAGE_TYPE}", null);
			textObject.SetTextVariable("DAMAGE", damage);
			textObject.SetTextVariable("DAMAGE_TYPE", GameTexts.FindText("str_damage_types", damageType.ToString()));
			return textObject;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00005538 File Offset: 0x00003738
		public static TextObject GetSwingDamageText(WeaponComponentData weapon, ItemModifier itemModifier)
		{
			int modifiedSwingDamage = weapon.GetModifiedSwingDamage(itemModifier);
			DamageTypes swingDamageType = weapon.SwingDamageType;
			return ItemHelper.GetDamageDescription(modifiedSwingDamage, swingDamageType);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000555C File Offset: 0x0000375C
		public static TextObject GetMissileDamageText(WeaponComponentData weapon, ItemModifier itemModifier)
		{
			int modifiedMissileDamage = weapon.GetModifiedMissileDamage(itemModifier);
			DamageTypes damageType = ((weapon.WeaponClass == WeaponClass.ThrowingAxe) ? weapon.SwingDamageType : weapon.ThrustDamageType);
			return ItemHelper.GetDamageDescription(modifiedMissileDamage, damageType);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00005590 File Offset: 0x00003790
		public static TextObject GetThrustDamageText(WeaponComponentData weapon, ItemModifier itemModifier)
		{
			int modifiedThrustDamage = weapon.GetModifiedThrustDamage(itemModifier);
			DamageTypes thrustDamageType = weapon.ThrustDamageType;
			return ItemHelper.GetDamageDescription(modifiedThrustDamage, thrustDamageType);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000055B1 File Offset: 0x000037B1
		public static TextObject NumberOfItems(int number, ItemObject item)
		{
			TextObject textObject = new TextObject("{=siWNDxgo}{.%}{?NUMBER_OF_ITEM > 1}{NUMBER_OF_ITEM} {PLURAL(ITEM)}{?}one {ITEM}{\\?}{.%}", null);
			textObject.SetTextVariable("ITEM", item.Name);
			textObject.SetTextVariable("NUMBER_OF_ITEM", number);
			return textObject;
		}
	}
}
