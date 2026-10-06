using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;

public class SpectatorWeaponSlotVM : ViewModel
{
	private const int TitleTextHeight = 1;

	private const int PropertyTextHeight = 0;

	private const string DefaultSlotState = "Default";

	private const string EquippedSlotState = "Equipped";

	private readonly ItemModifier _itemModifier;

	private readonly int _ammoCount;

	private readonly int _averageAmmoDamage;

	private ItemImageIdentifierVM _icon;

	private BasicTooltipViewModel _hint;

	private bool _isEquipped;

	private string _slotState = "Default";

	public ItemObject Item { get; private set; }

	public EquipmentIndex SlotIndex { get; private set; }

	[DataSourceProperty]
	public ItemImageIdentifierVM Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			if (value != _icon)
			{
				_icon = value;
				OnPropertyChangedWithValue(value, "Icon");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEquipped
	{
		get
		{
			return _isEquipped;
		}
		set
		{
			if (value != _isEquipped)
			{
				_isEquipped = value;
				OnPropertyChangedWithValue(value, "IsEquipped");
				RefreshSlotState();
			}
		}
	}

	[DataSourceProperty]
	public string SlotState
	{
		get
		{
			return _slotState;
		}
		set
		{
			if (value != _slotState)
			{
				_slotState = value;
				OnPropertyChangedWithValue(value, "SlotState");
			}
		}
	}

	public SpectatorWeaponSlotVM(ItemObject item, EquipmentIndex slotIndex, MissionEquipment equipment)
	{
		Item = item;
		SlotIndex = slotIndex;
		_itemModifier = equipment[slotIndex].ItemModifier;
		ResolveAmmoTotals(equipment, out _ammoCount, out _averageAmmoDamage);
		Icon = new ItemImageIdentifierVM(item);
		Hint = new BasicTooltipViewModel(() => GetTooltipProperties());
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Icon?.OnFinalize();
		Icon = null;
		Hint = null;
		Item = null;
	}

	private static void ResolveAmmoTotals(MissionEquipment equipment, out int ammoCount, out int averageAmmoDamage)
	{
		ammoCount = 0;
		averageAmmoDamage = 0;
		if (equipment == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
		{
			MissionWeapon missionWeapon = equipment[equipmentIndex];
			ItemObject item = missionWeapon.Item;
			if (item?.PrimaryWeapon != null && item.PrimaryWeapon.IsAmmo)
			{
				ammoCount += item.PrimaryWeapon.GetModifiedStackCount(missionWeapon.ItemModifier);
				num2 += item.PrimaryWeapon.GetModifiedThrustDamage(missionWeapon.ItemModifier);
				num++;
			}
		}
		if (num > 0)
		{
			averageAmmoDamage = MathF.Round((float)num2 / (float)num);
		}
	}

	private List<TooltipProperty> GetTooltipProperties()
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (Item == null)
		{
			return list;
		}
		list.Add(new TooltipProperty(string.Empty, GetDisplayName().ToString(), 1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		WeaponComponentData primaryWeapon = Item.PrimaryWeapon;
		if (primaryWeapon == null)
		{
			return list;
		}
		switch (WeaponComponentData.GetItemTypeFromWeaponClass(primaryWeapon.WeaponClass))
		{
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
		case ItemObject.ItemTypeEnum.Polearm:
			if (primaryWeapon.SwingDamageType != DamageTypes.Invalid)
			{
				AddProperty(list, new TextObject("{=yJsE4Ayo}Swing Spd."), primaryWeapon.GetModifiedSwingSpeed(_itemModifier));
				AddProperty(list, new TextObject("{=RNgWFLIO}Swing Dmg."), primaryWeapon.GetModifiedSwingDamage(_itemModifier));
			}
			if (primaryWeapon.ThrustDamageType != DamageTypes.Invalid)
			{
				AddProperty(list, new TextObject("{=J0vjDOFO}Thrust Spd."), primaryWeapon.GetModifiedThrustSpeed(_itemModifier));
				AddProperty(list, new TextObject("{=Ie9I2Bha}Thrust Dmg."), primaryWeapon.GetModifiedThrustDamage(_itemModifier));
			}
			AddProperty(list, new TextObject("{=ftoSCQ0x}Length"), primaryWeapon.WeaponLength);
			AddProperty(list, new TextObject("{=oibdTnXP}Handling"), primaryWeapon.GetModifiedHandling(_itemModifier));
			break;
		case ItemObject.ItemTypeEnum.Thrown:
			AddProperty(list, new TextObject("{=ftoSCQ0x}Length"), primaryWeapon.WeaponLength);
			AddProperty(list, new TextObject("{=s31DnnAf}Damage"), primaryWeapon.GetModifiedThrustDamage(_itemModifier));
			AddProperty(list, new TextObject("{=QfTt7YRB}Fire Rate"), primaryWeapon.GetModifiedMissileSpeed(_itemModifier));
			AddProperty(list, new TextObject("{=TAnabTdy}Accuracy"), primaryWeapon.Accuracy);
			AddProperty(list, new TextObject("{=b31ITmm0}Stack Amnt."), primaryWeapon.GetModifiedStackCount(_itemModifier));
			break;
		case ItemObject.ItemTypeEnum.Shield:
			AddProperty(list, new TextObject("{=6GSXsdeX}Speed"), primaryWeapon.GetModifiedThrustSpeed(_itemModifier));
			AddProperty(list, new TextObject("{=GGseMDd3}Durability"), primaryWeapon.GetModifiedMaximumHitPoints(_itemModifier));
			AddProperty(list, new TextObject("{=ahiBhAqU}Armor"), primaryWeapon.GetModifiedArmor(_itemModifier));
			AddProperty(list, new TextObject("{=4Dd2xgPm}Weight"), (int)Item.Weight);
			break;
		case ItemObject.ItemTypeEnum.Bow:
		case ItemObject.ItemTypeEnum.Crossbow:
		case ItemObject.ItemTypeEnum.Sling:
			AddProperty(list, new TextObject("{=ftoSCQ0x}Length"), primaryWeapon.WeaponLength);
			AddProperty(list, new TextObject("{=s31DnnAf}Damage"), primaryWeapon.GetModifiedThrustDamage(_itemModifier) + _averageAmmoDamage);
			AddProperty(list, new TextObject("{=QfTt7YRB}Fire Rate"), primaryWeapon.GetModifiedSwingSpeed(_itemModifier));
			AddProperty(list, new TextObject("{=TAnabTdy}Accuracy"), primaryWeapon.Accuracy);
			AddProperty(list, new TextObject("{=yUpH2mQ4}Ammo"), _ammoCount);
			break;
		}
		AddWeaponFlagProperties(list, primaryWeapon.WeaponFlags);
		return list;
	}

	private TextObject GetDisplayName()
	{
		if (_itemModifier == null)
		{
			return Item.Name;
		}
		TextObject name = _itemModifier.Name;
		name.SetTextVariable("ITEMNAME", Item.Name);
		return name;
	}

	private static void AddWeaponFlagProperties(List<TooltipProperty> properties, WeaponFlags weaponFlags)
	{
		List<TextObject> weaponFlagTexts = GetWeaponFlagTexts(weaponFlags);
		if (weaponFlagTexts.Count != 0)
		{
			properties.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
			for (int i = 0; i < weaponFlagTexts.Count; i++)
			{
				properties.Add(new TooltipProperty(string.Empty, weaponFlagTexts[i].ToString(), 0));
			}
		}
	}

	private static List<TextObject> GetWeaponFlagTexts(WeaponFlags weaponFlags)
	{
		List<TextObject> list = new List<TextObject>();
		if (weaponFlags.HasAnyFlag(WeaponFlags.BonusAgainstShield))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_bonus_against_shield"));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_can_knockdown"));
		}
		if (weaponFlags.HasAllFlags(WeaponFlags.CanDismount | WeaponFlags.CanHook))
		{
			list.Add(new TextObject("{=7HA99oUg}Both swing and thrust attacks can dismount riders"));
		}
		else if (weaponFlags.HasAnyFlag(WeaponFlags.CanDismount))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_can_dismount"));
		}
		else if (weaponFlags.HasAnyFlag(WeaponFlags.CanHook))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_can_hook"));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanCrushThrough))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_can_crush_through"));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithTwoHand))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_not_usable_two_hand"));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_not_usable_one_hand"));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CantReloadOnHorseback))
		{
			list.Add(GameTexts.FindText("str_inventory_flag_cant_reload_on_horseback"));
		}
		return list;
	}

	private static void AddProperty(List<TooltipProperty> properties, TextObject name, int value)
	{
		properties.Add(new TooltipProperty(name.ToString(), value.ToString(), 0));
	}

	public void SetEquipped(bool isEquipped)
	{
		IsEquipped = isEquipped;
		RefreshSlotState();
	}

	private void RefreshSlotState()
	{
		SlotState = (_isEquipped ? "Equipped" : "Default");
	}
}
