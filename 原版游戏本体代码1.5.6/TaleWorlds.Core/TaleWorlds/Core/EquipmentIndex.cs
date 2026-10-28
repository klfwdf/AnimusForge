namespace TaleWorlds.Core;

public enum EquipmentIndex
{
	None = -1,
	WeaponItemBeginSlot = 0,
	Weapon0 = WeaponItemBeginSlot,
	Weapon1 = 1,
	Weapon2 = 2,
	Weapon3 = 3,
	ExtraWeaponSlot = 4,
	NumAllWeaponSlots = 5,
	NumPrimaryWeaponSlots = ExtraWeaponSlot,
	NonWeaponItemBeginSlot = NumAllWeaponSlots,
	ArmorItemBeginSlot = NumAllWeaponSlots,
	Head = NumAllWeaponSlots,
	Body = 6,
	Leg = 7,
	Gloves = 8,
	Cape = 9,
	ArmorItemEndSlot = 10,
	NumAllArmorSlots = NumAllWeaponSlots,
	Horse = ArmorItemEndSlot,
	HorseHarness = 11,
	NumEquipmentSetSlots = 12
}
