using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public static class MBEquipmentMissionExtensions
{
	public static SkinMask GetSkinMeshesMask(this Equipment equipment)
	{
		SkinMask skinMask = SkinMask.AllVisible;
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			bool flag = equipment[(int)equipmentIndex].Item != null && equipment[(int)equipmentIndex].Item.HasArmorComponent;
			bool flag2 = equipment[(int)equipmentIndex].CosmeticItem != null && equipment[(int)equipmentIndex].CosmeticItem.HasArmorComponent;
			if (((equipmentIndex == EquipmentIndex.NumAllWeaponSlots || equipmentIndex == EquipmentIndex.Body || equipmentIndex == EquipmentIndex.Leg || equipmentIndex == EquipmentIndex.Gloves || equipmentIndex == EquipmentIndex.Cape) && flag) || flag2)
			{
				skinMask = ((equipment[equipmentIndex].CosmeticItem != null) ? (skinMask & equipment[(int)equipmentIndex].CosmeticItem.ArmorComponent.MeshesMask) : (skinMask & equipment[(int)equipmentIndex].Item.ArmorComponent.MeshesMask));
			}
		}
		return skinMask;
	}
}
