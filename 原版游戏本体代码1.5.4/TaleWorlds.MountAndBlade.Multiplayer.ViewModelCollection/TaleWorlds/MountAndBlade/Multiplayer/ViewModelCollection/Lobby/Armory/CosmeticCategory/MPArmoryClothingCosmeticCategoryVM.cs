using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticCategory;

public class MPArmoryClothingCosmeticCategoryVM : MPArmoryCosmeticCategoryBaseVM
{
	public readonly MPArmoryCosmeticsVM.ClothingCategory ClothingCategory;

	private List<string> _defaultCosmeticIDs;

	public static event Action<MPArmoryClothingCosmeticCategoryVM> OnSelected;

	public MPArmoryClothingCosmeticCategoryVM(MPArmoryCosmeticsVM.ClothingCategory clothingCategory)
		: base(CosmeticsManager.CosmeticType.Clothing)
	{
		_defaultCosmeticIDs = new List<string>();
		ClothingCategory = clothingCategory;
		base.CosmeticCategoryName = clothingCategory.ToString();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.AvailableCosmetics.ApplyActionOnAllItems(delegate(MPArmoryCosmeticItemBaseVM c)
		{
			c.RefreshValues();
		});
	}

	protected override void ExecuteSelectCategory()
	{
		MPArmoryClothingCosmeticCategoryVM.OnSelected?.Invoke(this);
	}

	private void AddDefaultItem(ItemObject item)
	{
		MPArmoryCosmeticClothingItemVM item2 = new MPArmoryCosmeticClothingItemVM(new ClothingCosmeticElement(item.StringId, CosmeticsManager.CosmeticRarity.Default, 0, new List<string>(), new List<Tuple<string, string>>()), string.Empty)
		{
			IsUnlocked = true,
			IsUnequippable = false
		};
		ItemObject.ItemTypeEnum itemTypeEnum = ClothingCategory.ToItemTypeEnum();
		if (itemTypeEnum == ItemObject.ItemTypeEnum.Invalid || itemTypeEnum == item.ItemType)
		{
			base.AvailableCosmetics.Add(item2);
		}
	}

	public void SetDefaultEquipments(Equipment equipment)
	{
		base.AvailableCosmetics.Clear();
		_defaultCosmeticIDs.Clear();
		if (equipment == null)
		{
			return;
		}
		for (EquipmentIndex equipmentIndex = EquipmentIndex.NumAllWeaponSlots; equipmentIndex < EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
		{
			ItemObject item = equipment[equipmentIndex].Item;
			if (item != null)
			{
				_defaultCosmeticIDs.Add(equipment[equipmentIndex].Item.StringId);
				AddDefaultItem(item);
			}
		}
	}

	public void ReplaceCosmeticWithDefaultItem(MPArmoryCosmeticClothingItemVM cosmetic, MPArmoryCosmeticsVM.ClothingCategory clothingCategory, MultiplayerClassDivisions.MPHeroClass selectedClass, List<string> ownedCosmetics)
	{
		bool num = cosmetic.ClothingCategory == clothingCategory || clothingCategory == MPArmoryCosmeticsVM.ClothingCategory.ClothingCategoriesBegin;
		bool flag = cosmetic.Cosmetic is ClothingCosmeticElement clothingCosmeticElement && (clothingCosmeticElement.ReplaceItemsId.Any((string c) => _defaultCosmeticIDs.Contains(c)) || clothingCosmeticElement.ReplaceItemless.Any((Tuple<string, string> r) => r.Item1 == selectedClass.StringId)) && !base.AvailableCosmetics.Contains(cosmetic);
		if (num && flag)
		{
			base.AvailableCosmetics.Add(cosmetic);
			cosmetic.IsUnlocked = (ownedCosmetics != null && ownedCosmetics.Contains(cosmetic.CosmeticID)) || cosmetic.Cosmetic.IsFree;
		}
	}

	public void OnEquipmentRefreshed(EquipmentIndex equipmentIndex)
	{
		foreach (MPArmoryCosmeticItemBaseVM availableCosmetic in base.AvailableCosmetics)
		{
			if (availableCosmetic is MPArmoryCosmeticClothingItemVM { EquipmentElement: var equipmentElement } && equipmentElement.Item.GetCosmeticEquipmentIndex() == equipmentIndex)
			{
				availableCosmetic.IsUsed = false;
			}
		}
	}
}
