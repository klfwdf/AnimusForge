using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class ShallowItemVM : ViewModel
{
	public enum ItemGroup
	{
		None,
		Spear,
		Javelin,
		Bow,
		Crossbow,
		Sword,
		Axe,
		Mace,
		ThrowingAxe,
		ThrowingKnife,
		Ammo,
		Shield,
		Mount,
		Stone
	}

	public class ArmoryItemFlagVM : ViewModel
	{
		private string _icon;

		private HintViewModel _hint;

		[DataSourceProperty]
		public string Icon
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
		public HintViewModel Hint
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

		public ArmoryItemFlagVM(string icon, TextObject hintText)
		{
			Icon = "SPGeneral\\" + icon;
			Hint = new HintViewModel(hintText);
		}
	}

	private readonly Action<ShallowItemVM> _onSelect;

	private AlternativeUsageItemOptionVM _latestUsageOption;

	private Equipment _equipment;

	private EquipmentIndex _equipmentIndex;

	private bool _isInitialized;

	private ItemImageIdentifierVM _icon;

	private string _name;

	private string _typeAsString;

	private bool _isValid;

	private bool _isSelected;

	private bool _hasAnyAlternativeUsage;

	private MBBindingList<ArmoryItemFlagVM> _itemInformationList;

	private MBBindingList<ShallowItemPropertyVM> _propertyList;

	private SelectorVM<AlternativeUsageItemOptionVM> _alternativeUsageSelector;

	public ItemGroup Type { get; private set; }

	[DataSourceProperty]
	public MBBindingList<ArmoryItemFlagVM> ItemInformationList
	{
		get
		{
			return _itemInformationList;
		}
		set
		{
			if (value != _itemInformationList)
			{
				_itemInformationList = value;
				OnPropertyChangedWithValue(value, "ItemInformationList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ShallowItemPropertyVM> PropertyList
	{
		get
		{
			return _propertyList;
		}
		set
		{
			if (value != _propertyList)
			{
				_propertyList = value;
				OnPropertyChangedWithValue(value, "PropertyList");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

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
	public string TypeAsString
	{
		get
		{
			return _typeAsString;
		}
		set
		{
			if (value != _typeAsString)
			{
				_typeAsString = value;
				OnPropertyChangedWithValue(value, "TypeAsString");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool HasAnyAlternativeUsage
	{
		get
		{
			return _hasAnyAlternativeUsage;
		}
		set
		{
			if (value != _hasAnyAlternativeUsage)
			{
				_hasAnyAlternativeUsage = value;
				OnPropertyChangedWithValue(value, "HasAnyAlternativeUsage");
			}
		}
	}

	[DataSourceProperty]
	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (value != _isValid)
			{
				_isValid = value;
				OnPropertyChangedWithValue(value, "IsValid");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<AlternativeUsageItemOptionVM> AlternativeUsageSelector
	{
		get
		{
			return _alternativeUsageSelector;
		}
		set
		{
			if (value != _alternativeUsageSelector)
			{
				_alternativeUsageSelector = value;
				OnPropertyChangedWithValue(value, "AlternativeUsageSelector");
			}
		}
	}

	public ShallowItemVM(Action<ShallowItemVM> onSelect)
	{
		ItemInformationList = new MBBindingList<ArmoryItemFlagVM>();
		PropertyList = new MBBindingList<ShallowItemPropertyVM>();
		AlternativeUsageSelector = new SelectorVM<AlternativeUsageItemOptionVM>(new List<string>(), 0, OnAlternativeUsageChanged);
		_onSelect = onSelect;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RefreshWith(_equipmentIndex, _equipment);
		PropertyList.ApplyActionOnAllItems(delegate(ShallowItemPropertyVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_equipment = null;
	}

	public void RefreshWith(EquipmentIndex equipmentIndex, Equipment equipment)
	{
		_equipment = equipment;
		_equipmentIndex = equipmentIndex;
		ItemObject itemObject = equipment?[equipmentIndex].Item;
		if (itemObject == null || (equipmentIndex == EquipmentIndex.ArmorItemEndSlot && !itemObject.HasHorseComponent) || (equipmentIndex != EquipmentIndex.ArmorItemEndSlot && (itemObject.PrimaryWeapon == null || itemObject.PrimaryWeapon.IsAmmo)))
		{
			IsValid = false;
			Icon = new ItemImageIdentifierVM(null);
			return;
		}
		IsValid = true;
		Name = itemObject.Name.ToString();
		Icon = new ItemImageIdentifierVM(itemObject);
		Type = GetItemGroupType(itemObject);
		TypeAsString = ((Type == ItemGroup.None) ? "" : Type.ToString());
		HasAnyAlternativeUsage = false;
		AlternativeUsageSelector.ItemList.Clear();
		if (itemObject.PrimaryWeapon != null)
		{
			for (int i = 0; i < itemObject.Weapons.Count; i++)
			{
				WeaponComponentData weaponComponentData = itemObject.Weapons[i];
				if (IsItemUsageApplicable(weaponComponentData))
				{
					TextObject textObject = GameTexts.FindText("str_weapon_usage", weaponComponentData.WeaponDescriptionId);
					AlternativeUsageSelector.AddItem(new AlternativeUsageItemOptionVM(weaponComponentData.WeaponDescriptionId, textObject, textObject, AlternativeUsageSelector, i));
					HasAnyAlternativeUsage = true;
				}
			}
		}
		AlternativeUsageSelector.SelectedIndex = -1;
		AlternativeUsageSelector.SelectedIndex = 0;
		_latestUsageOption = AlternativeUsageSelector.ItemList.FirstOrDefault();
		if (_latestUsageOption != null)
		{
			_latestUsageOption.IsSelected = true;
		}
		AlternativeUsageSelector.SetOnChangeAction(OnAlternativeUsageChanged);
		RefreshItemPropertyList(_equipmentIndex, _equipment, AlternativeUsageSelector.SelectedIndex);
		_isInitialized = true;
	}

	private void OnAlternativeUsageChanged(SelectorVM<AlternativeUsageItemOptionVM> selector)
	{
		if (_isInitialized && selector.SelectedIndex >= 0)
		{
			if (_latestUsageOption != null)
			{
				_latestUsageOption.IsSelected = false;
			}
			RefreshItemPropertyList(_equipmentIndex, _equipment, selector.SelectedIndex);
			if (selector.SelectedItem != null)
			{
				selector.SelectedItem.IsSelected = true;
			}
		}
	}

	private void RefreshItemPropertyList(EquipmentIndex equipmentIndex, Equipment equipment, int alternativeIndex)
	{
		ItemObject item = equipment[equipmentIndex].Item;
		ItemModifier itemModifier = equipment[equipmentIndex].ItemModifier;
		PropertyList.Clear();
		if (item.PrimaryWeapon != null)
		{
			WeaponComponentData weaponComponentData = item.Weapons[alternativeIndex];
			ItemObject.ItemTypeEnum itemTypeFromWeaponClass = WeaponComponentData.GetItemTypeFromWeaponClass(weaponComponentData.WeaponClass);
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.OneHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.TwoHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Polearm)
			{
				if (weaponComponentData.SwingDamageType != DamageTypes.Invalid)
				{
					AddProperty(new TextObject("{=yJsE4Ayo}Swing Spd."), (float)weaponComponentData.GetModifiedSwingSpeed(itemModifier) / 145f, weaponComponentData.GetModifiedSwingSpeed(itemModifier));
					AddProperty(new TextObject("{=RNgWFLIO}Swing Dmg."), (float)weaponComponentData.GetModifiedSwingDamage(itemModifier) / 143f, weaponComponentData.GetModifiedSwingDamage(itemModifier));
				}
				if (weaponComponentData.ThrustDamageType != DamageTypes.Invalid)
				{
					AddProperty(new TextObject("{=J0vjDOFO}Thrust Spd."), (float)weaponComponentData.GetModifiedThrustSpeed(itemModifier) / 114f, weaponComponentData.GetModifiedThrustSpeed(itemModifier));
					AddProperty(new TextObject("{=Ie9I2Bha}Thrust Dmg."), (float)weaponComponentData.GetModifiedThrustDamage(itemModifier) / 86f, weaponComponentData.GetModifiedThrustDamage(itemModifier));
				}
				AddProperty(new TextObject("{=ftoSCQ0x}Length"), (float)weaponComponentData.WeaponLength / 315f, weaponComponentData.WeaponLength);
				AddProperty(new TextObject("{=oibdTnXP}Handling"), (float)weaponComponentData.GetModifiedHandling(itemModifier) / 120f, weaponComponentData.GetModifiedHandling(itemModifier));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Thrown)
			{
				AddProperty(new TextObject("{=ftoSCQ0x}Length"), (float)weaponComponentData.WeaponLength / 147f, weaponComponentData.WeaponLength);
				AddProperty(new TextObject("{=s31DnnAf}Damage"), (float)weaponComponentData.GetModifiedThrustDamage(itemModifier) / 94f, weaponComponentData.GetModifiedThrustDamage(itemModifier));
				AddProperty(new TextObject("{=QfTt7YRB}Fire Rate"), (float)weaponComponentData.GetModifiedMissileSpeed(itemModifier) / 115f, weaponComponentData.GetModifiedMissileSpeed(itemModifier));
				AddProperty(new TextObject("{=TAnabTdy}Accuracy"), (float)weaponComponentData.Accuracy / 300f, weaponComponentData.Accuracy);
				AddProperty(new TextObject("{=b31ITmm0}Stack Amnt."), (float)weaponComponentData.GetModifiedStackCount(itemModifier) / 40f, weaponComponentData.GetModifiedStackCount(itemModifier));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Shield)
			{
				AddProperty(new TextObject("{=6GSXsdeX}Speed"), (float)weaponComponentData.GetModifiedThrustSpeed(itemModifier) / 120f, weaponComponentData.GetModifiedThrustSpeed(itemModifier));
				AddProperty(new TextObject("{=GGseMDd3}Durability"), (float)weaponComponentData.GetModifiedMaximumHitPoints(itemModifier) / 500f, weaponComponentData.GetModifiedMaximumHitPoints(itemModifier));
				AddProperty(new TextObject("{=ahiBhAqU}Armor"), (float)weaponComponentData.GetModifiedArmor(itemModifier) / 40f, weaponComponentData.GetModifiedArmor(itemModifier));
				AddProperty(new TextObject("{=4Dd2xgPm}Weight"), item.Weight / 40f, (int)item.Weight);
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Bow || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Crossbow || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Sling)
			{
				int num = 0;
				float num2 = 0f;
				int num3 = 0;
				for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.ExtraWeaponSlot; equipmentIndex2++)
				{
					ItemObject item2 = equipment[equipmentIndex2].Item;
					ItemModifier itemModifier2 = equipment[equipmentIndex2].ItemModifier;
					if (item2 != null && item2.PrimaryWeapon.IsAmmo)
					{
						num += item2.PrimaryWeapon.GetModifiedStackCount(itemModifier2);
						num3 += item2.PrimaryWeapon.GetModifiedThrustDamage(itemModifier2);
						num2 += 1f;
					}
				}
				num3 = TaleWorlds.Library.MathF.Round((float)num3 / num2);
				AddProperty(new TextObject("{=ftoSCQ0x}Length"), (float)weaponComponentData.WeaponLength / 123f, weaponComponentData.WeaponLength);
				AddProperty(new TextObject("{=s31DnnAf}Damage"), (float)(weaponComponentData.GetModifiedThrustDamage(itemModifier) + num3) / 70f, weaponComponentData.GetModifiedThrustDamage(itemModifier) + num3);
				AddProperty(new TextObject("{=QfTt7YRB}Fire Rate"), (float)weaponComponentData.GetModifiedSwingSpeed(itemModifier) / 120f, weaponComponentData.GetModifiedSwingSpeed(itemModifier));
				AddProperty(new TextObject("{=TAnabTdy}Accuracy"), (float)weaponComponentData.Accuracy / 105f, weaponComponentData.Accuracy);
				AddProperty(new TextObject("{=yUpH2mQ4}Ammo"), (float)num / 90f, num);
			}
			ItemInformationList.Clear();
			List<(string, TextObject)> weaponFlagDetails = GetWeaponFlagDetails(weaponComponentData.WeaponFlags);
			for (int i = 0; i < weaponFlagDetails.Count; i++)
			{
				ArmoryItemFlagVM item3 = new ArmoryItemFlagVM(weaponFlagDetails[i].Item1, weaponFlagDetails[i].Item2);
				ItemInformationList.Add(item3);
			}
		}
		if (item.HorseComponent != null)
		{
			EquipmentElement equipmentElement = equipment[EquipmentIndex.ArmorItemEndSlot];
			EquipmentElement harness = equipment[EquipmentIndex.HorseHarness];
			int modifiedMountCharge = equipmentElement.GetModifiedMountCharge(in harness);
			int num4 = (int)(4.33f * (float)equipmentElement.GetModifiedMountSpeed(in harness));
			int modifiedMountManeuver = equipmentElement.GetModifiedMountManeuver(in harness);
			int modifiedMountHitPoints = equipmentElement.GetModifiedMountHitPoints();
			int modifiedMountBodyArmor = harness.GetModifiedMountBodyArmor();
			AddProperty(new TextObject("{=DAVb2Pzg}Charge Dmg."), (float)modifiedMountCharge / 35f, modifiedMountCharge);
			AddProperty(new TextObject("{=6GSXsdeX}Speed"), (float)num4 / 303.1f, num4);
			AddProperty(new TextObject("{=rg7OuWS2}Maneuver"), (float)modifiedMountManeuver / 70f, modifiedMountManeuver);
			AddProperty(new TextObject("{=oBbiVeKE}Hit Points"), (float)modifiedMountHitPoints / 300f, modifiedMountHitPoints);
			AddProperty(new TextObject("{=kftE5nvv}Horse Armor"), (float)modifiedMountBodyArmor / 100f, modifiedMountBodyArmor);
		}
	}

	private static List<(string, TextObject)> GetWeaponFlagDetails(WeaponFlags weaponFlags)
	{
		List<(string, TextObject)> list = new List<(string, TextObject)>();
		if (weaponFlags.HasAnyFlag(WeaponFlags.BonusAgainstShield))
		{
			string item = "WeaponFlagIcons\\bonus_against_shield";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_bonus_against_shield");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
		{
			string item = "WeaponFlagIcons\\can_knock_down";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_knockdown");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanDismount) && !weaponFlags.HasAnyFlag(WeaponFlags.CanHook))
		{
			string item = "WeaponFlagIcons\\can_dismount";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_dismount");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanHook) && !weaponFlags.HasAnyFlag(WeaponFlags.CanDismount))
		{
			string item = "WeaponFlagIcons\\can_dismount";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_hook");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAllFlags(WeaponFlags.CanDismount | WeaponFlags.CanHook))
		{
			string item = "WeaponFlagIcons\\can_dismount";
			TextObject item2 = new TextObject("{=7HA99oUg}Both swing and thrust attacks can dismount riders");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CanCrushThrough))
		{
			string item = "WeaponFlagIcons\\can_crush_through";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_can_crush_through");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithTwoHand))
		{
			string item = "WeaponFlagIcons\\not_usable_with_two_hand";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_not_usable_two_hand");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
		{
			string item = "WeaponFlagIcons\\not_usable_with_one_hand";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_not_usable_one_hand");
			list.Add((item, item2));
		}
		if (weaponFlags.HasAnyFlag(WeaponFlags.CantReloadOnHorseback))
		{
			string item = "WeaponFlagIcons\\cant_reload_on_horseback";
			TextObject item2 = GameTexts.FindText("str_inventory_flag_cant_reload_on_horseback");
			list.Add((item, item2));
		}
		return list;
	}

	private void AddProperty(TextObject name, float fraction, int value)
	{
		PropertyList.Add(new ShallowItemPropertyVM(name, TaleWorlds.Library.MathF.Round(fraction * 1000f), value));
	}

	private static ItemGroup GetItemGroupType(ItemObject item)
	{
		if (item.WeaponComponent != null)
		{
			switch (item.WeaponComponent.PrimaryWeapon.WeaponClass)
			{
			case WeaponClass.Bow:
				return ItemGroup.Bow;
			case WeaponClass.Crossbow:
				return ItemGroup.Crossbow;
			case WeaponClass.OneHandedPolearm:
			case WeaponClass.TwoHandedPolearm:
			case WeaponClass.LowGripPolearm:
				return ItemGroup.Spear;
			case WeaponClass.OneHandedSword:
			case WeaponClass.TwoHandedSword:
				return ItemGroup.Sword;
			case WeaponClass.OneHandedAxe:
			case WeaponClass.TwoHandedAxe:
				return ItemGroup.Axe;
			case WeaponClass.Mace:
			case WeaponClass.TwoHandedMace:
				return ItemGroup.Mace;
			case WeaponClass.Javelin:
				return ItemGroup.Javelin;
			case WeaponClass.ThrowingAxe:
				return ItemGroup.ThrowingAxe;
			case WeaponClass.ThrowingKnife:
				return ItemGroup.ThrowingKnife;
			case WeaponClass.SmallShield:
			case WeaponClass.LargeShield:
				return ItemGroup.Shield;
			case WeaponClass.Arrow:
			case WeaponClass.Bolt:
			case WeaponClass.SlingStone:
			case WeaponClass.Cartridge:
			case WeaponClass.Musket:
				return ItemGroup.Ammo;
			case WeaponClass.Sling:
			case WeaponClass.Stone:
			case WeaponClass.BallistaStone:
				return ItemGroup.Stone;
			default:
				return ItemGroup.None;
			}
		}
		if (item.HasHorseComponent)
		{
			return ItemGroup.Mount;
		}
		return ItemGroup.None;
	}

	[UsedImplicitly]
	public void OnSelect()
	{
		_onSelect(this);
	}

	public static bool IsItemUsageApplicable(WeaponComponentData weapon)
	{
		WeaponDescription obj = ((weapon != null && weapon.WeaponDescriptionId != null) ? MBObjectManager.Instance.GetObject<WeaponDescription>(weapon.WeaponDescriptionId) : null);
		if (obj == null)
		{
			return false;
		}
		return !obj.IsHiddenFromUI;
	}
}
