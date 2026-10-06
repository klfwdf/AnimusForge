using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;

public class MPArmoryCosmeticClothingItemVM : MPArmoryCosmeticItemBaseVM
{
	public EquipmentElement EquipmentElement { get; }

	public MPArmoryCosmeticsVM.ClothingCategory ClothingCategory { get; }

	public ClothingCosmeticElement ClothingCosmeticElement { get; }

	public MPArmoryCosmeticClothingItemVM(CosmeticElement cosmetic, string cosmeticID)
		: base(cosmetic, cosmeticID, CosmeticsManager.CosmeticType.Clothing)
	{
		ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(cosmetic.Id);
		EquipmentElement = new EquipmentElement(itemObject);
		base.Icon = new ItemImageIdentifierVM(itemObject);
		ClothingCategory = GetCosmeticCategory();
		ClothingCosmeticElement = cosmetic as ClothingCosmeticElement;
		base.ItemType = (int)EquipmentElement.Item.ItemType;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = EquipmentElement.Item?.Name.ToString();
	}

	private MPArmoryCosmeticsVM.ClothingCategory GetCosmeticCategory()
	{
		return EquipmentElement.Item.Type switch
		{
			ItemObject.ItemTypeEnum.BodyArmor => MPArmoryCosmeticsVM.ClothingCategory.BodyArmor, 
			ItemObject.ItemTypeEnum.HeadArmor => MPArmoryCosmeticsVM.ClothingCategory.HeadArmor, 
			ItemObject.ItemTypeEnum.Cape => MPArmoryCosmeticsVM.ClothingCategory.Cape, 
			ItemObject.ItemTypeEnum.HandArmor => MPArmoryCosmeticsVM.ClothingCategory.HandArmor, 
			ItemObject.ItemTypeEnum.LegArmor => MPArmoryCosmeticsVM.ClothingCategory.LegArmor, 
			_ => MPArmoryCosmeticsVM.ClothingCategory.Invalid, 
		};
	}
}
