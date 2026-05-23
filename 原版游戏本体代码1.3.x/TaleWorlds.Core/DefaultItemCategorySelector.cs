using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000051 RID: 81
	public class DefaultItemCategorySelector : ItemCategorySelector
	{
		// Token: 0x060006A6 RID: 1702 RVA: 0x00016F84 File Offset: 0x00015184
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
					if (itemObject.Tier == ItemObject.ItemTiers.Tier6)
					{
						return DefaultItemCategories.MeleeWeapons5;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier5)
					{
						return DefaultItemCategories.MeleeWeapons5;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier4)
					{
						return DefaultItemCategories.MeleeWeapons4;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier3)
					{
						return DefaultItemCategories.MeleeWeapons3;
					}
					if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
					{
						return DefaultItemCategories.MeleeWeapons1;
					}
					return DefaultItemCategories.MeleeWeapons2;
				}
				else if (primaryWeapon.IsRangedWeapon)
				{
					if (itemObject.Tier == ItemObject.ItemTiers.Tier6)
					{
						return DefaultItemCategories.RangedWeapons5;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier5)
					{
						return DefaultItemCategories.RangedWeapons5;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier4)
					{
						return DefaultItemCategories.RangedWeapons4;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier3)
					{
						return DefaultItemCategories.RangedWeapons3;
					}
					if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
					{
						return DefaultItemCategories.RangedWeapons1;
					}
					return DefaultItemCategories.RangedWeapons2;
				}
				else if (primaryWeapon.IsShield)
				{
					if (itemObject.Tier == ItemObject.ItemTiers.Tier6)
					{
						return DefaultItemCategories.Shield5;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier5)
					{
						return DefaultItemCategories.Shield5;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier4)
					{
						return DefaultItemCategories.Shield4;
					}
					if (itemObject.Tier == ItemObject.ItemTiers.Tier3)
					{
						return DefaultItemCategories.Shield3;
					}
					if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
					{
						return DefaultItemCategories.Shield1;
					}
					return DefaultItemCategories.Shield2;
				}
				else
				{
					if (primaryWeapon.IsAmmo)
					{
						return DefaultItemCategories.Arrows;
					}
					return DefaultItemCategories.MeleeWeapons1;
				}
			}
			else
			{
				if (itemObject.HasHorseComponent)
				{
					return DefaultItemCategories.Horse;
				}
				if (itemObject.HasArmorComponent)
				{
					ArmorComponent armorComponent = itemObject.ArmorComponent;
					if (itemObject.Type == ItemObject.ItemTypeEnum.HorseHarness)
					{
						if (itemObject.Tier == ItemObject.ItemTiers.Tier6)
						{
							return DefaultItemCategories.HorseEquipment5;
						}
						if (itemObject.Tier == ItemObject.ItemTiers.Tier5)
						{
							return DefaultItemCategories.HorseEquipment5;
						}
						if (itemObject.Tier == ItemObject.ItemTiers.Tier4)
						{
							return DefaultItemCategories.HorseEquipment4;
						}
						if (itemObject.Tier == ItemObject.ItemTiers.Tier3)
						{
							return DefaultItemCategories.HorseEquipment3;
						}
						if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
						{
							return DefaultItemCategories.HorseEquipment;
						}
						return DefaultItemCategories.HorseEquipment2;
					}
					else
					{
						if (itemObject.Tier == ItemObject.ItemTiers.Tier6)
						{
							return DefaultItemCategories.UltraArmor;
						}
						if (itemObject.Tier == ItemObject.ItemTiers.Tier5)
						{
							return DefaultItemCategories.UltraArmor;
						}
						if (itemObject.Tier == ItemObject.ItemTiers.Tier4)
						{
							return DefaultItemCategories.HeavyArmor;
						}
						if (itemObject.Tier == ItemObject.ItemTiers.Tier3)
						{
							return DefaultItemCategories.MediumArmor;
						}
						if (itemObject.Tier != ItemObject.ItemTiers.Tier2)
						{
							return DefaultItemCategories.Garment;
						}
						return DefaultItemCategories.LightArmor;
					}
				}
				else
				{
					if (itemObject.HasSaddleComponent)
					{
						return DefaultItemCategories.HorseEquipment;
					}
					return DefaultItemCategories.Unassigned;
				}
			}
		}
	}
}
