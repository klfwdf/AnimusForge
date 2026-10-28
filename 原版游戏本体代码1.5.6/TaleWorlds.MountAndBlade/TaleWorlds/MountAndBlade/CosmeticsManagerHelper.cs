using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public static class CosmeticsManagerHelper
{
	public static Dictionary<int, List<int>> GetUsedIndicesFromIds(Dictionary<string, List<string>> usedCosmetics)
	{
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		MBReadOnlyList<MultiplayerClassDivisions.MPHeroClass> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<MultiplayerClassDivisions.MPHeroClass>();
		foreach (KeyValuePair<string, List<string>> usedCosmetic in usedCosmetics)
		{
			int num = -1;
			for (int i = 0; i < objectTypeList.Count; i++)
			{
				if (objectTypeList[i].StringId == usedCosmetic.Key)
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				continue;
			}
			List<int> list = new List<int>();
			foreach (string item in usedCosmetic.Value)
			{
				int num2 = -1;
				for (int j = 0; j < CosmeticsManager.CosmeticElementsList.Count; j++)
				{
					if (CosmeticsManager.CosmeticElementsList[j].Id == item)
					{
						num2 = j;
						break;
					}
				}
				if (num2 >= 0)
				{
					list.Add(num2);
				}
			}
			if (list.Count > 0)
			{
				dictionary.Add(num, list);
			}
		}
		return dictionary;
	}

	public static ActionIndexCache GetSuitableTauntAction(Agent agent, int tauntIndex)
	{
		if (agent.Equipment == null)
		{
			return ActionIndexCache.act_none;
		}
		WeaponComponentData currentUsageItem = agent.WieldedWeapon.CurrentUsageItem;
		WeaponComponentData currentUsageItem2 = agent.WieldedOffhandWeapon.CurrentUsageItem;
		return ActionIndexCache.Create(TauntUsageManager.Instance.GetAction(tauntIndex, agent.GetIsLeftStance(), !agent.HasMount, currentUsageItem, currentUsageItem2));
	}

	public static TauntUsageManager.TauntUsage.TauntUsageFlag GetActionNotUsableReason(Agent agent, int tauntIndex)
	{
		WeaponComponentData currentUsageItem = agent.WieldedWeapon.CurrentUsageItem;
		WeaponComponentData currentUsageItem2 = agent.WieldedOffhandWeapon.CurrentUsageItem;
		return TauntUsageManager.Instance.GetIsActionNotSuitableReason(tauntIndex, agent.GetIsLeftStance(), !agent.HasMount, currentUsageItem, currentUsageItem2);
	}

	public static string GetSuitableTauntActionForEquipment(Equipment equipment, TauntCosmeticElement taunt)
	{
		if (equipment == null)
		{
			return null;
		}
		equipment.GetInitialWeaponIndicesToEquip(out var mainHandWeaponIndex, out var offHandWeaponIndex, out var _);
		WeaponComponentData mainHandWeapon = ((mainHandWeaponIndex == EquipmentIndex.None) ? null : equipment[mainHandWeaponIndex].Item?.PrimaryWeapon);
		WeaponComponentData offhandWeapon = ((offHandWeaponIndex == EquipmentIndex.None) ? null : equipment[offHandWeaponIndex].Item?.PrimaryWeapon);
		return TauntUsageManager.Instance.GetAction(TauntUsageManager.Instance.GetIndexOfAction(taunt.Id), isLeftStance: false, onFoot: true, mainHandWeapon, offhandWeapon);
	}

	public static bool IsWeaponClassOneHanded(WeaponClass weaponClass)
	{
		if (weaponClass != WeaponClass.OneHandedAxe && weaponClass != WeaponClass.OneHandedPolearm)
		{
			return weaponClass == WeaponClass.OneHandedSword;
		}
		return true;
	}

	public static bool IsWeaponClassTwoHanded(WeaponClass weaponClass)
	{
		if (weaponClass != WeaponClass.TwoHandedAxe && weaponClass != WeaponClass.TwoHandedMace && weaponClass != WeaponClass.TwoHandedPolearm)
		{
			return weaponClass == WeaponClass.TwoHandedSword;
		}
		return true;
	}

	public static bool IsWeaponClassShield(WeaponClass weaponClass)
	{
		if (weaponClass != WeaponClass.LargeShield)
		{
			return weaponClass == WeaponClass.SmallShield;
		}
		return true;
	}

	public static bool IsWeaponClassBow(WeaponClass weaponClass)
	{
		return weaponClass == WeaponClass.Bow;
	}

	public static bool IsWeaponClassCrossbow(WeaponClass weaponClass)
	{
		return weaponClass == WeaponClass.Crossbow;
	}

	public static WeaponClass[] GetComplimentaryWeaponClasses(WeaponClass weaponClass)
	{
		switch (weaponClass)
		{
		case WeaponClass.OneHandedSword:
		case WeaponClass.OneHandedAxe:
		case WeaponClass.Mace:
		case WeaponClass.Pick:
		case WeaponClass.OneHandedPolearm:
		case WeaponClass.LowGripPolearm:
		case WeaponClass.Stone:
		case WeaponClass.ThrowingAxe:
		case WeaponClass.ThrowingKnife:
		case WeaponClass.Javelin:
		case WeaponClass.BallistaStone:
			return new WeaponClass[2]
			{
				WeaponClass.SmallShield,
				WeaponClass.LargeShield
			};
		case WeaponClass.Arrow:
			return new WeaponClass[1] { WeaponClass.Bow };
		case WeaponClass.Bolt:
			return new WeaponClass[1] { WeaponClass.Crossbow };
		case WeaponClass.SlingStone:
			return new WeaponClass[1] { WeaponClass.Sling };
		case WeaponClass.Bow:
			return new WeaponClass[1] { WeaponClass.Arrow };
		case WeaponClass.Crossbow:
			return new WeaponClass[1] { WeaponClass.Bolt };
		case WeaponClass.Sling:
			return new WeaponClass[1] { WeaponClass.SlingStone };
		case WeaponClass.SmallShield:
		case WeaponClass.LargeShield:
			return new WeaponClass[4]
			{
				WeaponClass.OneHandedAxe,
				WeaponClass.OneHandedSword,
				WeaponClass.OneHandedPolearm,
				WeaponClass.Mace
			};
		default:
			return new WeaponClass[0];
		}
	}
}
