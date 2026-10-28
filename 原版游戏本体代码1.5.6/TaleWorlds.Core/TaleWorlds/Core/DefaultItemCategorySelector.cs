namespace TaleWorlds.Core;

public class DefaultItemCategorySelector : ItemCategorySelector
{
	public override ItemCategory GetItemCategoryForItem(ItemObject itemObject)
	{
		if (itemObject.PrimaryWeapon != null)
		{
			WeaponComponentData primaryWeapon = itemObject.PrimaryWeapon;
			if (itemObject != null && itemObject.HasBannerComponent)
			{
				return DefaultItemCategories.Banners;
			}
			if (primaryWeapon.IsMeleeWeapon)
			{
				if (itemObject.Tier != ItemObject.ItemTiers.Tier6)
				{
					if (itemObject.Tier != ItemObject.ItemTiers.Tier5)
					{
						if (itemObject.Tier != ItemObject.ItemTiers.Tier4)
						{
							if (itemObject.Tier != ItemObject.ItemTiers.Tier3)
							{
								if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
								{
									return DefaultItemCategories.MeleeWeapons1;
								}
								return DefaultItemCategories.MeleeWeapons2;
							}
							return DefaultItemCategories.MeleeWeapons3;
						}
						return DefaultItemCategories.MeleeWeapons4;
					}
					return DefaultItemCategories.MeleeWeapons5;
				}
				return DefaultItemCategories.MeleeWeapons5;
			}
			if (primaryWeapon.IsRangedWeapon)
			{
				if (itemObject.Tier != ItemObject.ItemTiers.Tier6)
				{
					if (itemObject.Tier != ItemObject.ItemTiers.Tier5)
					{
						if (itemObject.Tier != ItemObject.ItemTiers.Tier4)
						{
							if (itemObject.Tier != ItemObject.ItemTiers.Tier3)
							{
								if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
								{
									return DefaultItemCategories.RangedWeapons1;
								}
								return DefaultItemCategories.RangedWeapons2;
							}
							return DefaultItemCategories.RangedWeapons3;
						}
						return DefaultItemCategories.RangedWeapons4;
					}
					return DefaultItemCategories.RangedWeapons5;
				}
				return DefaultItemCategories.RangedWeapons5;
			}
			if (primaryWeapon.IsShield)
			{
				if (itemObject.Tier != ItemObject.ItemTiers.Tier6)
				{
					if (itemObject.Tier != ItemObject.ItemTiers.Tier5)
					{
						if (itemObject.Tier != ItemObject.ItemTiers.Tier4)
						{
							if (itemObject.Tier != ItemObject.ItemTiers.Tier3)
							{
								if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
								{
									return DefaultItemCategories.Shield1;
								}
								return DefaultItemCategories.Shield2;
							}
							return DefaultItemCategories.Shield3;
						}
						return DefaultItemCategories.Shield4;
					}
					return DefaultItemCategories.Shield5;
				}
				return DefaultItemCategories.Shield5;
			}
			if (primaryWeapon.IsAmmo)
			{
				return DefaultItemCategories.Arrows;
			}
			return DefaultItemCategories.MeleeWeapons1;
		}
		if (itemObject.HasHorseComponent)
		{
			return DefaultItemCategories.Horse;
		}
		if (itemObject.HasArmorComponent)
		{
			_ = itemObject.ArmorComponent;
			if (itemObject.Type == ItemObject.ItemTypeEnum.HorseHarness)
			{
				if (itemObject.Tier != ItemObject.ItemTiers.Tier6)
				{
					if (itemObject.Tier != ItemObject.ItemTiers.Tier5)
					{
						if (itemObject.Tier != ItemObject.ItemTiers.Tier4)
						{
							if (itemObject.Tier != ItemObject.ItemTiers.Tier3)
							{
								if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
								{
									return DefaultItemCategories.HorseEquipment;
								}
								return DefaultItemCategories.HorseEquipment2;
							}
							return DefaultItemCategories.HorseEquipment3;
						}
						return DefaultItemCategories.HorseEquipment4;
					}
					return DefaultItemCategories.HorseEquipment5;
				}
				return DefaultItemCategories.HorseEquipment5;
			}
			if (itemObject.Tier != ItemObject.ItemTiers.Tier6)
			{
				if (itemObject.Tier != ItemObject.ItemTiers.Tier5)
				{
					if (itemObject.Tier != ItemObject.ItemTiers.Tier4)
					{
						if (itemObject.Tier != ItemObject.ItemTiers.Tier3)
						{
							if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
							{
								return DefaultItemCategories.Garment;
							}
							return DefaultItemCategories.LightArmor;
						}
						return DefaultItemCategories.MediumArmor;
					}
					return DefaultItemCategories.HeavyArmor;
				}
				return DefaultItemCategories.UltraArmor;
			}
			return DefaultItemCategories.UltraArmor;
		}
		if (itemObject.HasSaddleComponent)
		{
			return DefaultItemCategories.HorseEquipment;
		}
		return DefaultItemCategories.Unassigned;
	}
}
