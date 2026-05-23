using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200000A RID: 10
	public class AgentOriginUtilities
	{
		// Token: 0x06000045 RID: 69 RVA: 0x000023EC File Offset: 0x000005EC
		public static TroopTraitsMask GetDefaultTraitsMask(IAgentOriginBase origin)
		{
			TroopTraitsMask troopTraitsMask = TroopTraitsMask.None;
			if (origin.Troop.IsMounted)
			{
				troopTraitsMask |= TroopTraitsMask.Mount;
			}
			if (origin.Troop.IsRanged)
			{
				troopTraitsMask |= TroopTraitsMask.Ranged;
			}
			else
			{
				troopTraitsMask |= TroopTraitsMask.Melee;
			}
			if (origin.HasShield)
			{
				troopTraitsMask |= TroopTraitsMask.Shield;
			}
			if (origin.HasSpear)
			{
				troopTraitsMask |= TroopTraitsMask.Spear;
			}
			if (origin.HasThrownWeapon)
			{
				troopTraitsMask |= TroopTraitsMask.Thrown;
			}
			if (origin.HasHeavyArmor)
			{
				troopTraitsMask |= TroopTraitsMask.Armor;
			}
			return troopTraitsMask;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002458 File Offset: 0x00000658
		public static void GetDefaultTroopTraits(BasicCharacterObject troop, out bool hasThrownWeapon, out bool hasSpear, out bool hasShield, out bool hasHeavyArmor)
		{
			Equipment firstBattleEquipment = troop.FirstBattleEquipment;
			hasThrownWeapon = false;
			hasSpear = false;
			hasShield = false;
			hasHeavyArmor = false;
			if (firstBattleEquipment != null)
			{
				for (int i = 0; i < 5; i++)
				{
					EquipmentElement equipmentElement = firstBattleEquipment[i];
					if (!equipmentElement.IsEmpty)
					{
						WeaponClass weaponClass = equipmentElement.Item.PrimaryWeapon.WeaponClass;
						if (weaponClass - WeaponClass.OneHandedPolearm > 2)
						{
							if (weaponClass - WeaponClass.ThrowingAxe > 2)
							{
								if (weaponClass - WeaponClass.SmallShield <= 1)
								{
									hasShield = true;
								}
							}
							else
							{
								hasThrownWeapon = true;
							}
						}
						else
						{
							hasSpear = true;
						}
					}
				}
				if (firstBattleEquipment.GetHumanBodyArmorSum() > 24f)
				{
					hasHeavyArmor = true;
				}
			}
		}
	}
}
