using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

internal static class LobbyTauntHelper
{
	public static Equipment PrepareForTaunt(Equipment originalEquipment, TauntCosmeticElement taunt, bool doNotAddComplimentaryWeapons = false)
	{
		MBReadOnlyList<TauntUsageManager.TauntUsage> mBReadOnlyList = TauntUsageManager.Instance.GetUsageSet(taunt.Id)?.GetUsages();
		if (mBReadOnlyList == null || mBReadOnlyList.Count == 0)
		{
			return originalEquipment;
		}
		Equipment equipment = new Equipment(originalEquipment);
		equipment.GetInitialWeaponIndicesToEquip(out var mainHandWeaponIndex, out var offHandWeaponIndex, out var isMainHandNotUsableWithOneHand);
		WeaponComponentData mainHandWeapon = ((mainHandWeaponIndex == EquipmentIndex.None) ? null : equipment[mainHandWeaponIndex].Item?.PrimaryWeapon);
		WeaponComponentData offhandWeapon = null;
		if (!isMainHandNotUsableWithOneHand && offHandWeaponIndex != EquipmentIndex.None)
		{
			offhandWeapon = equipment[offHandWeaponIndex].Item?.PrimaryWeapon;
		}
		foreach (TauntUsageManager.TauntUsage item in mBReadOnlyList)
		{
			if (item.IsSuitable(isLeftStance: false, isOnFoot: true, mainHandWeapon, offhandWeapon))
			{
				return equipment;
			}
		}
		TauntUsageManager.TauntUsage tauntUsage = mBReadOnlyList.FirstOrDefault((TauntUsageManager.TauntUsage u) => !u.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresBow | TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresShield)) ?? mBReadOnlyList[0];
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			equipment[equipmentIndex] = default(EquipmentElement);
		}
		List<ItemObject> list = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().ToList();
		list.Sort((ItemObject first, ItemObject second) => first.Value.CompareTo(second.Value));
		EquipmentIndex eqIndex = EquipmentIndex.WeaponItemBeginSlot;
		if (tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresBow))
		{
			ItemObject randomElementWithPredicate = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassBow(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
			if (!equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate)))
			{
				return equipment;
			}
			ItemObject randomElementWithPredicate2 = list.GetRandomElementWithPredicate(delegate(ItemObject i)
			{
				WeaponComponentData primaryWeapon = i.PrimaryWeapon;
				return primaryWeapon != null && primaryWeapon.WeaponClass == WeaponClass.Arrow;
			});
			equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate2));
			return equipment;
		}
		if (tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresShield))
		{
			ItemObject randomElementWithPredicate3 = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassShield(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
			if (!equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate3)))
			{
				return equipment;
			}
			if (!tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded))
			{
				ItemObject randomElementWithPredicate4 = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassOneHanded(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
				equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate4));
				return equipment;
			}
		}
		if (!tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded))
		{
			ItemObject randomElementWithPredicate5 = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassTwoHanded(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
			if (!equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate5)))
			{
				return equipment;
			}
			if (tauntUsage.IsSuitable(isLeftStance: false, isOnFoot: true, randomElementWithPredicate5.PrimaryWeapon, null))
			{
				return equipment;
			}
		}
		if (!tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded))
		{
			ItemObject randomElementWithPredicate6 = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassOneHanded(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
			if (!equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate6)))
			{
				return equipment;
			}
			if (tauntUsage.IsSuitable(isLeftStance: false, isOnFoot: true, randomElementWithPredicate6.PrimaryWeapon, null))
			{
				return equipment;
			}
		}
		if (!tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForShield))
		{
			ItemObject randomElementWithPredicate7 = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassShield(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
			if (!equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate7)))
			{
				return equipment;
			}
		}
		if (!tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForBow))
		{
			ItemObject randomElementWithPredicate8 = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassBow(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
			if (!equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate8)))
			{
				return equipment;
			}
			ItemObject randomElementWithPredicate9 = list.GetRandomElementWithPredicate(delegate(ItemObject i)
			{
				WeaponComponentData primaryWeapon = i.PrimaryWeapon;
				return primaryWeapon != null && primaryWeapon.WeaponClass == WeaponClass.Arrow;
			});
			equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate9));
			return equipment;
		}
		if (!tauntUsage.UsageFlag.HasAnyFlag(TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForCrossbow))
		{
			ItemObject randomElementWithPredicate10 = list.GetRandomElementWithPredicate((ItemObject i) => CosmeticsManagerHelper.IsWeaponClassCrossbow(i.PrimaryWeapon?.WeaponClass ?? WeaponClass.Undefined));
			if (!equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate10)))
			{
				return equipment;
			}
			ItemObject randomElementWithPredicate11 = list.GetRandomElementWithPredicate(delegate(ItemObject i)
			{
				WeaponComponentData primaryWeapon = i.PrimaryWeapon;
				return primaryWeapon != null && primaryWeapon.WeaponClass == WeaponClass.Bolt;
			});
			equipment.TryAddElement(ref eqIndex, new EquipmentElement(randomElementWithPredicate11));
			return equipment;
		}
		return equipment;
	}

	private static Tuple<EquipmentIndex, EquipmentElement, WeaponComponentData> GetWeaponInfoOfType(this Equipment equipment, WeaponClass type)
	{
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			WeaponComponentData weaponComponentData = equipment[equipmentIndex].Item?.Weapons?.FirstOrDefault((WeaponComponentData w) => w.WeaponClass == type);
			if (weaponComponentData != null)
			{
				return new Tuple<EquipmentIndex, EquipmentElement, WeaponComponentData>(equipmentIndex, equipment[equipmentIndex], weaponComponentData);
			}
		}
		return null;
	}

	private static Tuple<EquipmentIndex, EquipmentElement, WeaponComponentData> GetWeaponInfoOfPredicate(this Equipment equipment, Predicate<WeaponComponentData> predicate)
	{
		if (predicate == null)
		{
			return null;
		}
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			WeaponComponentData weaponComponentData = equipment[equipmentIndex].Item?.Weapons?.FirstOrDefault((WeaponComponentData w) => predicate(w));
			if (weaponComponentData != null)
			{
				return new Tuple<EquipmentIndex, EquipmentElement, WeaponComponentData>(equipmentIndex, equipment[equipmentIndex], weaponComponentData);
			}
		}
		return null;
	}

	private static Tuple<EquipmentIndex, EquipmentElement, WeaponComponentData> GetTwoHandedWeaponInfo(this Equipment equipment)
	{
		return equipment.GetWeaponInfoOfType(WeaponClass.TwoHandedAxe) ?? equipment.GetWeaponInfoOfType(WeaponClass.TwoHandedSword) ?? equipment.GetWeaponInfoOfType(WeaponClass.TwoHandedMace) ?? equipment.GetWeaponInfoOfType(WeaponClass.TwoHandedPolearm);
	}

	private static bool TryAddElement(this Equipment equipment, ref EquipmentIndex eqIndex, EquipmentElement element)
	{
		if (eqIndex < EquipmentIndex.WeaponItemBeginSlot || eqIndex > EquipmentIndex.Weapon1)
		{
			return false;
		}
		if (Equipment.IsItemFitsToSlot(eqIndex, element.Item))
		{
			equipment[eqIndex] = element;
			eqIndex++;
		}
		return true;
	}

	private static void SwapItems(this Equipment equipment, EquipmentIndex first, EquipmentIndex second)
	{
		EquipmentElement value = equipment[first];
		equipment[first] = equipment[second];
		equipment[second] = value;
	}
}
