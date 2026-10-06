namespace TaleWorlds.MountAndBlade.Diamond;

public class ItemData
{
	public string TypeId { get; set; }

	public string ModifierId { get; set; }

	public int? Index { get; set; }

	private ItemType ItemType => ItemList.GetItemTypeOf(TypeId);

	public int Price => GetPriceOf(TypeId, ModifierId);

	public bool IsValid => IsItemValid(TypeId, ModifierId);

	public string ItemKey => TypeId + "|" + ModifierId;

	public void CopyItemData(ItemData itemdata)
	{
		TypeId = itemdata.TypeId;
		ModifierId = itemdata.ModifierId;
		Index = itemdata.Index;
	}

	private static int GetInventoryItemTypeOfItem(ItemType itemType)
	{
		return itemType switch
		{
			ItemType.Horse => 64, 
			ItemType.OneHandedWeapon => 1, 
			ItemType.TwoHandedWeapon => 1, 
			ItemType.Polearm => 1, 
			ItemType.Arrows => 1, 
			ItemType.Bolts => 1, 
			ItemType.Shield => 2, 
			ItemType.Bow => 1, 
			ItemType.Crossbow => 1, 
			ItemType.Thrown => 1, 
			ItemType.Goods => 256, 
			ItemType.HeadArmor => 4, 
			ItemType.BodyArmor => 8, 
			ItemType.LegArmor => 16, 
			ItemType.HandArmor => 32, 
			ItemType.Pistol => 1, 
			ItemType.Musket => 1, 
			ItemType.Bullets => 1, 
			ItemType.Animal => 1024, 
			ItemType.Book => 512, 
			ItemType.HorseHarness => 128, 
			ItemType.Cape => 2048, 
			_ => 0, 
		};
	}

	public bool CanItemToEquipmentDragPossible(int equipmentIndex)
	{
		return CanItemToEquipmentDragPossible(TypeId, equipmentIndex);
	}

	public static bool CanItemToEquipmentDragPossible(string itemTypeId, int equipmentIndex)
	{
		InventoryItemType inventoryItemTypeOfItem = (InventoryItemType)GetInventoryItemTypeOfItem(ItemList.GetItemTypeOf(itemTypeId));
		bool result = false;
		switch (equipmentIndex)
		{
		case 0:
		case 1:
		case 2:
		case 3:
			result = inventoryItemTypeOfItem == InventoryItemType.Weapon || inventoryItemTypeOfItem == InventoryItemType.Shield;
			break;
		case 5:
			result = inventoryItemTypeOfItem == InventoryItemType.HeadArmor;
			break;
		case 6:
			result = inventoryItemTypeOfItem == InventoryItemType.BodyArmor;
			break;
		case 7:
			result = inventoryItemTypeOfItem == InventoryItemType.LegArmor;
			break;
		case 8:
			result = inventoryItemTypeOfItem == InventoryItemType.HandArmor;
			break;
		case 9:
			result = inventoryItemTypeOfItem == InventoryItemType.Cape;
			break;
		case 10:
			result = inventoryItemTypeOfItem == InventoryItemType.Horse;
			break;
		case 11:
			result = inventoryItemTypeOfItem == InventoryItemType.HorseHarness;
			break;
		}
		return result;
	}

	public static int GetPriceOf(string itemId, string modifierId)
	{
		return ItemList.GetPriceOf(itemId, modifierId);
	}

	public static bool IsItemValid(string itemId, string modifierId)
	{
		return ItemList.IsItemValid(itemId, modifierId);
	}
}
