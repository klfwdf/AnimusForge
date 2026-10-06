namespace TaleWorlds.Core;

public class AgentOriginUtilities
{
	public static TroopTraitsMask GetDefaultTraitsMask(IAgentOriginBase origin)
	{
		TroopTraitsMask troopTraitsMask = TroopTraitsMask.None;
		if (origin.Troop.IsMounted)
		{
			troopTraitsMask |= TroopTraitsMask.Mount;
		}
		troopTraitsMask = ((!origin.Troop.IsRanged) ? (troopTraitsMask | TroopTraitsMask.Melee) : (troopTraitsMask | TroopTraitsMask.Ranged));
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

	public static void GetDefaultTroopTraits(BasicCharacterObject troop, out bool hasThrownWeapon, out bool hasSpear, out bool hasShield, out bool hasHeavyArmor)
	{
		Equipment firstBattleEquipment = troop.FirstBattleEquipment;
		hasThrownWeapon = false;
		hasSpear = false;
		hasShield = false;
		hasHeavyArmor = false;
		if (firstBattleEquipment == null)
		{
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			EquipmentElement equipmentElement = firstBattleEquipment[i];
			if (!equipmentElement.IsEmpty)
			{
				switch (equipmentElement.Item.PrimaryWeapon.WeaponClass)
				{
				case WeaponClass.ThrowingAxe:
				case WeaponClass.ThrowingKnife:
				case WeaponClass.Javelin:
					hasThrownWeapon = true;
					break;
				case WeaponClass.SmallShield:
				case WeaponClass.LargeShield:
					hasShield = true;
					break;
				case WeaponClass.OneHandedPolearm:
				case WeaponClass.TwoHandedPolearm:
				case WeaponClass.LowGripPolearm:
					hasSpear = true;
					break;
				}
			}
		}
		if (firstBattleEquipment.GetHumanBodyArmorSum() > 24f)
		{
			hasHeavyArmor = true;
		}
	}
}
