using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;

public static class LobbyCosmeticExtensions
{
	public static ItemObject.ItemTypeEnum ToItemTypeEnum(this MPArmoryCosmeticsVM.ClothingCategory cosmeticCategory)
	{
		return cosmeticCategory switch
		{
			MPArmoryCosmeticsVM.ClothingCategory.ClothingCategoriesBegin => ItemObject.ItemTypeEnum.Invalid, 
			MPArmoryCosmeticsVM.ClothingCategory.BodyArmor => ItemObject.ItemTypeEnum.BodyArmor, 
			MPArmoryCosmeticsVM.ClothingCategory.HeadArmor => ItemObject.ItemTypeEnum.HeadArmor, 
			MPArmoryCosmeticsVM.ClothingCategory.Cape => ItemObject.ItemTypeEnum.Cape, 
			MPArmoryCosmeticsVM.ClothingCategory.HandArmor => ItemObject.ItemTypeEnum.HandArmor, 
			MPArmoryCosmeticsVM.ClothingCategory.LegArmor => ItemObject.ItemTypeEnum.LegArmor, 
			_ => ItemObject.ItemTypeEnum.Invalid, 
		};
	}

	public static EquipmentIndex GetCosmeticEquipmentIndex(this ItemObject itemObject)
	{
		if (itemObject == null)
		{
			return EquipmentIndex.None;
		}
		return itemObject.Type switch
		{
			ItemObject.ItemTypeEnum.BodyArmor => EquipmentIndex.Body, 
			ItemObject.ItemTypeEnum.LegArmor => EquipmentIndex.Leg, 
			ItemObject.ItemTypeEnum.Cape => EquipmentIndex.Cape, 
			ItemObject.ItemTypeEnum.HandArmor => EquipmentIndex.Gloves, 
			ItemObject.ItemTypeEnum.HeadArmor => EquipmentIndex.NumAllWeaponSlots, 
			ItemObject.ItemTypeEnum.HorseHarness => EquipmentIndex.HorseHarness, 
			ItemObject.ItemTypeEnum.Horse => EquipmentIndex.ArmorItemEndSlot, 
			_ => EquipmentIndex.None, 
		};
	}
}
