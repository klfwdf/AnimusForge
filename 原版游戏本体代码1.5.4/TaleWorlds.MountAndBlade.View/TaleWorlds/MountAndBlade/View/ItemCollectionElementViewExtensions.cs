using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View;

public static class ItemCollectionElementViewExtensions
{
	public static string GetMaterialCacheID(object o)
	{
		ItemRosterElement itemRosterElement = (ItemRosterElement)o;
		if (!itemRosterElement.EquipmentElement.Item.IsCraftedWeapon)
		{
			return "InventorySlot_" + itemRosterElement.EquipmentElement.Item.MultiMeshName;
		}
		return "";
	}

	public static MetaMesh GetMultiMesh(this ItemObject item, bool isFemale, bool useSlimVersion, bool needBatchedVersion)
	{
		MetaMesh metaMesh = null;
		if (item != null)
		{
			bool flag = false;
			if (item.HasArmorComponent)
			{
				flag = item.ArmorComponent.MultiMeshHasGenderVariations;
			}
			metaMesh = item.GetMultiMeshCopyWithGenderData(flag && isFemale, useSlimVersion, needBatchedVersion);
			if (metaMesh == null || metaMesh.MeshCount == 0)
			{
				metaMesh = item.GetMultiMeshCopy();
			}
		}
		return metaMesh;
	}

	public static MetaMesh GetMultiMesh(this EquipmentElement equipmentElement, bool isFemale, bool useSlimVersion, bool needBatchedVersion)
	{
		if (equipmentElement.CosmeticItem == null)
		{
			return equipmentElement.Item.GetMultiMesh(isFemale, useSlimVersion, needBatchedVersion);
		}
		return equipmentElement.CosmeticItem.GetMultiMesh(isFemale, useSlimVersion, needBatchedVersion);
	}

	public static MetaMesh GetMultiMesh(this MissionWeapon weapon, bool isFemale, bool useSlimVersion, bool needBatchedVersion)
	{
		return weapon.Item.GetMultiMesh(isFemale, useSlimVersion, needBatchedVersion);
	}

	public static MetaMesh GetItemMeshForInventory(this ItemRosterElement rosterElement, bool isFemale = false)
	{
		if (rosterElement.EquipmentElement.Item.ItemType != ItemObject.ItemTypeEnum.Arrows && rosterElement.EquipmentElement.Item.ItemType != ItemObject.ItemTypeEnum.Bolts && rosterElement.EquipmentElement.Item.ItemType != ItemObject.ItemTypeEnum.SlingStones)
		{
			return rosterElement.EquipmentElement.GetMultiMesh(isFemale, useSlimVersion: false, needBatchedVersion: false);
		}
		return rosterElement.EquipmentElement.Item.GetHolsterMeshCopy();
	}

	public static MetaMesh GetHolsterMeshCopy(this ItemObject item)
	{
		if (item.WeaponDesign != null)
		{
			MetaMesh holsterMesh = CraftedDataViewManager.GetCraftedDataView(item.WeaponDesign).HolsterMesh;
			if (!(holsterMesh != null))
			{
				return null;
			}
			return holsterMesh.CreateCopy();
		}
		if (!string.IsNullOrEmpty(item.HolsterMeshName))
		{
			return MetaMesh.GetCopy(item.HolsterMeshName);
		}
		return null;
	}

	public static MetaMesh GetHolsterMeshIfExists(this ItemObject item)
	{
		if (!(item.WeaponDesign != null))
		{
			return null;
		}
		return CraftedDataViewManager.GetCraftedDataView(item.WeaponDesign).HolsterMesh;
	}

	public static MetaMesh GetHolsterWithWeaponMeshCopy(this ItemObject item, bool needBatchedVersion)
	{
		if (item.WeaponDesign != null)
		{
			CraftedDataView craftedDataView = CraftedDataViewManager.GetCraftedDataView(item.WeaponDesign);
			MetaMesh metaMesh = (needBatchedVersion ? craftedDataView.HolsterMeshWithWeapon : craftedDataView.NonBatchedHolsterMeshWithWeapon);
			if (!(metaMesh != null))
			{
				return null;
			}
			return metaMesh.CreateCopy();
		}
		if (!string.IsNullOrEmpty(item.HolsterWithWeaponMeshName))
		{
			return MetaMesh.GetCopy(item.HolsterWithWeaponMeshName);
		}
		return null;
	}

	public static MetaMesh GetHolsterWithWeaponMeshIfExists(this ItemObject item)
	{
		if (!(item.WeaponDesign != null))
		{
			return null;
		}
		return CraftedDataViewManager.GetCraftedDataView(item.WeaponDesign).HolsterMeshWithWeapon;
	}

	public static MetaMesh GetFlyingMeshCopy(this ItemObject item, bool needBatchedVersion)
	{
		WeaponComponentData weaponComponentData = item.WeaponComponent?.PrimaryWeapon;
		if (item.WeaponDesign != null && weaponComponentData != null)
		{
			if (weaponComponentData.IsRangedWeapon && weaponComponentData.IsConsumable)
			{
				CraftedDataView craftedDataView = CraftedDataViewManager.GetCraftedDataView(item.WeaponDesign);
				MetaMesh metaMesh = (needBatchedVersion ? craftedDataView.WeaponMesh : craftedDataView.NonBatchedWeaponMesh);
				if (!(metaMesh != null))
				{
					return null;
				}
				return metaMesh.CreateCopy();
			}
			return null;
		}
		if (!string.IsNullOrEmpty(item.FlyingMeshName))
		{
			return MetaMesh.GetCopy(item.FlyingMeshName);
		}
		return null;
	}

	public static MetaMesh GetFlyingMeshIfExists(this ItemObject item)
	{
		return item?.WeaponComponent?.PrimaryWeapon.GetFlyingMeshIfExists(item);
	}

	internal static Material GetTableauMaterial(this ItemObject item, Banner banner)
	{
		Material tableauMaterial = null;
		if (item != null && item.IsUsingTableau)
		{
			Material material = null;
			MetaMesh multiMeshCopy = item.GetMultiMeshCopy();
			int meshCount = multiMeshCopy.MeshCount;
			for (int i = 0; i < meshCount; i++)
			{
				Mesh meshAtIndex = multiMeshCopy.GetMeshAtIndex(i);
				if (!meshAtIndex.HasTag("dont_use_tableau"))
				{
					material = meshAtIndex.GetMaterial();
					meshAtIndex.ManualInvalidate();
					break;
				}
				meshAtIndex.ManualInvalidate();
			}
			multiMeshCopy.ManualInvalidate();
			if (meshCount == 0 || material == null)
			{
				multiMeshCopy = item.GetMultiMeshCopy();
				Mesh meshAtIndex2 = multiMeshCopy.GetMeshAtIndex(0);
				material = meshAtIndex2.GetMaterial();
				meshAtIndex2.ManualInvalidate();
				multiMeshCopy.ManualInvalidate();
			}
			if (banner != null)
			{
				BannerDebugInfo debugInfo = BannerDebugInfo.CreateManual("ItemCollectionElementViewExtensions");
				if (material == null)
				{
					material = Material.GetDefaultTableauSampleMaterial(transparency: true);
				}
				uint flagMask = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
				Dictionary<Tuple<Material, Banner>, Material> dictionary = null;
				if (ViewSubModule.BannerTexturedMaterialCache != null)
				{
					dictionary = ViewSubModule.BannerTexturedMaterialCache;
				}
				if (dictionary != null)
				{
					if (dictionary.ContainsKey(new Tuple<Material, Banner>(material, banner)))
					{
						tableauMaterial = dictionary[new Tuple<Material, Banner>(material, banner)];
					}
					else
					{
						tableauMaterial = material.CreateCopy();
						Action<Texture> setAction = delegate(Texture tex)
						{
							ulong shaderFlags = tableauMaterial.GetShaderFlags();
							tableauMaterial.SetShaderFlags(shaderFlags | flagMask);
							tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
						};
						banner.GetTableauTextureSmall(in debugInfo, setAction);
						dictionary.Add(new Tuple<Material, Banner>(material, banner), tableauMaterial);
					}
				}
				else
				{
					tableauMaterial = material.CreateCopy();
					Action<Texture> setAction2 = delegate(Texture tex)
					{
						ulong shaderFlags = tableauMaterial.GetShaderFlags();
						tableauMaterial.SetShaderFlags(shaderFlags | flagMask);
						tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
					};
					banner.GetTableauTextureSmall(in debugInfo, setAction2);
				}
			}
		}
		return tableauMaterial;
	}

	public static MatrixFrame GetCameraFrameForInventory(this ItemRosterElement itemRosterElement)
	{
		return MatrixFrame.Identity;
	}

	public static MatrixFrame GetItemFrameForInventory(this ItemRosterElement itemRosterElement)
	{
		MatrixFrame result = MatrixFrame.Identity;
		Mat3 identity = Mat3.Identity;
		float num = 0.95f;
		Vec3 positionShift = new Vec3(0f, 0f, 0f, -1f);
		MetaMesh itemMeshForInventory = itemRosterElement.GetItemMeshForInventory();
		if (itemMeshForInventory != null)
		{
			switch (itemRosterElement.EquipmentElement.Item.ItemType)
			{
			case ItemObject.ItemTypeEnum.HeadArmor:
			case ItemObject.ItemTypeEnum.BodyArmor:
			case ItemObject.ItemTypeEnum.Cape:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutUp(System.MathF.PI);
				identity.RotateAboutSide(System.MathF.PI * -3f / 50f);
				break;
			case ItemObject.ItemTypeEnum.Horse:
			case ItemObject.ItemTypeEnum.Animal:
			case ItemObject.ItemTypeEnum.HorseHarness:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutUp(System.MathF.PI / 2f);
				break;
			case ItemObject.ItemTypeEnum.OneHandedWeapon:
			case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			case ItemObject.ItemTypeEnum.Polearm:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutForward(-System.MathF.PI / 4f);
				break;
			case ItemObject.ItemTypeEnum.HandArmor:
				identity.RotateAboutSide(System.MathF.PI * 11f / 20f);
				num = 2.1f;
				positionShift = new Vec3(0f, -0.4f);
				break;
			case ItemObject.ItemTypeEnum.LegArmor:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutUp(System.MathF.PI);
				identity.RotateAboutSide(-System.MathF.PI / 10f);
				identity.RotateAboutUp(0.47123894f);
				break;
			case ItemObject.ItemTypeEnum.Shield:
				identity.RotateAboutUp(System.MathF.PI);
				break;
			case ItemObject.ItemTypeEnum.Crossbow:
				identity.RotateAboutForward(System.MathF.PI * 3f / 4f);
				identity.RotateAboutSide(System.MathF.PI * -3f / 4f);
				identity.RotateAboutUp(-System.MathF.PI / 2f);
				break;
			case ItemObject.ItemTypeEnum.Bow:
			case ItemObject.ItemTypeEnum.Sling:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutForward(-System.MathF.PI / 4f);
				break;
			case ItemObject.ItemTypeEnum.Arrows:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutForward(-System.MathF.PI / 4f);
				break;
			case ItemObject.ItemTypeEnum.Bolts:
			case ItemObject.ItemTypeEnum.SlingStones:
			case ItemObject.ItemTypeEnum.Thrown:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutForward(-System.MathF.PI / 4f);
				break;
			case ItemObject.ItemTypeEnum.Goods:
				identity.RotateAboutSide(-1.0995574f);
				identity.RotateAboutUp(System.MathF.PI / 4f);
				break;
			case ItemObject.ItemTypeEnum.Book:
				identity.RotateAboutSide(-System.MathF.PI / 5f);
				identity.RotateAboutUp(-0.47123894f);
				break;
			}
			if (itemRosterElement.EquipmentElement.Item.IsCraftedWeapon)
			{
				num *= 0.55f;
			}
			result = itemRosterElement.EquipmentElement.Item.GetScaledFrame(identity, itemMeshForInventory, num, positionShift);
			if (itemRosterElement.EquipmentElement.Item.IsCraftedWeapon)
			{
				result.Elevate(-0.01f * ((float)itemRosterElement.EquipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponLength / 2f));
			}
		}
		return result;
	}

	public static MatrixFrame GetItemFrameForItemTooltip(this ItemRosterElement itemRosterElement)
	{
		MatrixFrame result = MatrixFrame.Identity;
		Mat3 identity = Mat3.Identity;
		float num = 0.85f;
		Vec3 positionShift = new Vec3(0f, 0f, 0f, -1f);
		MetaMesh itemMeshForInventory = itemRosterElement.GetItemMeshForInventory();
		if (itemMeshForInventory != null)
		{
			switch (itemRosterElement.EquipmentElement.Item.ItemType)
			{
			case ItemObject.ItemTypeEnum.HeadArmor:
			case ItemObject.ItemTypeEnum.BodyArmor:
			case ItemObject.ItemTypeEnum.Cape:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutUp(System.MathF.PI);
				break;
			case ItemObject.ItemTypeEnum.Arrows:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				break;
			case ItemObject.ItemTypeEnum.Horse:
			case ItemObject.ItemTypeEnum.Animal:
			case ItemObject.ItemTypeEnum.HorseHarness:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutUp(System.MathF.PI / 2f);
				num = 0.65f;
				break;
			case ItemObject.ItemTypeEnum.OneHandedWeapon:
			case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			case ItemObject.ItemTypeEnum.Polearm:
			case ItemObject.ItemTypeEnum.Bolts:
			case ItemObject.ItemTypeEnum.SlingStones:
			case ItemObject.ItemTypeEnum.Bow:
			case ItemObject.ItemTypeEnum.Crossbow:
			case ItemObject.ItemTypeEnum.Sling:
			case ItemObject.ItemTypeEnum.Thrown:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutForward(-System.MathF.PI / 4f);
				break;
			case ItemObject.ItemTypeEnum.LegArmor:
				identity.RotateAboutSide(-System.MathF.PI / 2f);
				identity.RotateAboutUp(System.MathF.PI);
				break;
			case ItemObject.ItemTypeEnum.HandArmor:
				identity.RotateAboutSide(System.MathF.PI / 2f);
				identity.RotateAboutSide(System.MathF.PI * -2f / 25f);
				num = 2.1f;
				positionShift = new Vec3(0f, -0.4f);
				break;
			case ItemObject.ItemTypeEnum.Shield:
				identity.RotateAboutUp(2.261947f);
				break;
			case ItemObject.ItemTypeEnum.Goods:
				identity.RotateAboutSide(-1.0995574f);
				identity.RotateAboutUp(System.MathF.PI / 4f);
				break;
			case ItemObject.ItemTypeEnum.Book:
				identity.RotateAboutSide(-System.MathF.PI / 5f);
				identity.RotateAboutUp(-0.47123894f);
				break;
			}
			if (itemRosterElement.EquipmentElement.Item.IsCraftedWeapon)
			{
				num *= 0.55f;
			}
			result = itemRosterElement.EquipmentElement.Item.GetScaledFrame(identity, itemMeshForInventory, num, positionShift);
			result.origin.z -= 5f;
		}
		if (itemRosterElement.EquipmentElement.Item.IsCraftedWeapon)
		{
			result.Elevate(-0.01f * ((float)itemRosterElement.EquipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponLength / 2f));
		}
		return result;
	}

	public static void OnGetWeaponData(ref WeaponData weaponData, MissionWeapon weapon, bool isFemale, Banner banner, bool needBatchedVersion)
	{
		MetaMesh multiMesh = weapon.GetMultiMesh(isFemale, useSlimVersion: false, needBatchedVersion);
		weaponData.WeaponMesh = multiMesh;
		MetaMesh holsterMeshCopy = weapon.Item.GetHolsterMeshCopy();
		weaponData.HolsterMesh = holsterMeshCopy;
		MetaMesh holsterWithWeaponMeshCopy = weapon.Item.GetHolsterWithWeaponMeshCopy(needBatchedVersion);
		weaponData.Prefab = weapon.Item.PrefabName;
		weaponData.HolsterMeshWithWeapon = holsterWithWeaponMeshCopy;
		MetaMesh flyingMeshCopy = weapon.Item.GetFlyingMeshCopy(needBatchedVersion);
		weaponData.FlyingMesh = flyingMeshCopy;
		Material tableauMaterial = weapon.Item.GetTableauMaterial(banner);
		weaponData.TableauMaterial = tableauMaterial;
	}
}
