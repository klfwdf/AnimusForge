using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionMainAgentEquipmentControllerVM : ViewModel
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
		Banner,
		Stone
	}

	private TextObject _replaceWithLocalizedText;

	private TextObject _dropLocalizedText;

	private SpawnedItemEntity _focusedWeaponEntity;

	private readonly Action<EquipmentIndex> _onDropEquipment;

	private readonly Action<SpawnedItemEntity, EquipmentIndex> _onEquipItem;

	private readonly TextObject _pickText = new TextObject("{=d5SNB0HV}Pick {ITEM_NAME}");

	private bool _isDropControllerActive;

	private bool _isEquipControllerActive;

	private string _selectedItemText;

	private string _dropText;

	private string _equipText;

	private string _focusedItemText;

	private MBBindingList<EquipmentActionItemVM> _dropActions;

	private MBBindingList<EquipmentActionItemVM> _equipActions;

	[DataSourceProperty]
	public bool IsDropControllerActive
	{
		get
		{
			return _isDropControllerActive;
		}
		set
		{
			if (value != _isDropControllerActive)
			{
				_isDropControllerActive = value;
				OnPropertyChangedWithValue(value, "IsDropControllerActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEquipControllerActive
	{
		get
		{
			return _isEquipControllerActive;
		}
		set
		{
			if (value != _isEquipControllerActive)
			{
				_isEquipControllerActive = value;
				OnPropertyChangedWithValue(value, "IsEquipControllerActive");
			}
		}
	}

	[DataSourceProperty]
	public string DropText
	{
		get
		{
			return _dropText;
		}
		set
		{
			if (value != _dropText)
			{
				_dropText = value;
				OnPropertyChangedWithValue(value, "DropText");
			}
		}
	}

	[DataSourceProperty]
	public string EquipText
	{
		get
		{
			return _equipText;
		}
		set
		{
			if (value != _equipText)
			{
				_equipText = value;
				OnPropertyChangedWithValue(value, "EquipText");
			}
		}
	}

	[DataSourceProperty]
	public string FocusedItemText
	{
		get
		{
			return _focusedItemText;
		}
		set
		{
			if (value != _focusedItemText)
			{
				_focusedItemText = value;
				OnPropertyChangedWithValue(value, "FocusedItemText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectedItemText
	{
		get
		{
			return _selectedItemText;
		}
		set
		{
			if (value != _selectedItemText)
			{
				_selectedItemText = value;
				OnPropertyChangedWithValue(value, "SelectedItemText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EquipmentActionItemVM> DropActions
	{
		get
		{
			return _dropActions;
		}
		set
		{
			if (value != _dropActions)
			{
				_dropActions = value;
				OnPropertyChangedWithValue(value, "DropActions");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EquipmentActionItemVM> EquipActions
	{
		get
		{
			return _equipActions;
		}
		set
		{
			if (value != _equipActions)
			{
				_equipActions = value;
				OnPropertyChangedWithValue(value, "EquipActions");
			}
		}
	}

	public MissionMainAgentEquipmentControllerVM(Action<EquipmentIndex> onDropEquipment, Action<SpawnedItemEntity, EquipmentIndex> onEquipItem)
	{
		_onDropEquipment = onDropEquipment;
		_onEquipItem = onEquipItem;
		DropActions = new MBBindingList<EquipmentActionItemVM>();
		EquipActions = new MBBindingList<EquipmentActionItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_dropLocalizedText = GameTexts.FindText("str_inventory_drop");
		_replaceWithLocalizedText = GameTexts.FindText("str_replace_with");
	}

	public void OnDropControllerToggle(bool isActive)
	{
		SelectedItemText = "";
		if (isActive && Agent.Main != null)
		{
			DropActions.Clear();
			DropActions.Add(new EquipmentActionItemVM(GameTexts.FindText("str_cancel").ToString(), "None", null, OnItemSelected));
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				MissionWeapon weapon = Agent.Main.Equipment[equipmentIndex];
				if (!weapon.IsEmpty)
				{
					string itemTypeAsString = GetItemTypeAsString(weapon.Item);
					bool isCurrentlyWielded = IsWieldedWeaponAtIndex(equipmentIndex);
					string weaponName = GetWeaponName(weapon);
					DropActions.Add(new EquipmentActionItemVM(weaponName, itemTypeAsString, equipmentIndex, OnItemSelected, isCurrentlyWielded));
				}
			}
		}
		else
		{
			EquipmentActionItemVM equipmentActionItemVM = DropActions.SingleOrDefault((EquipmentActionItemVM a) => a.IsSelected);
			if (equipmentActionItemVM != null)
			{
				HandleDropItemActionSelection(equipmentActionItemVM.Identifier);
			}
		}
		IsDropControllerActive = isActive;
	}

	private void HandleDropItemActionSelection(object selectedItem)
	{
		if (selectedItem is EquipmentIndex obj)
		{
			_onDropEquipment(obj);
		}
		else if (selectedItem != null)
		{
			Debug.FailedAssert("Unidentified action on drop wheel", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\HUD\\MissionMainAgentEquipmentControllerVM.cs", "HandleDropItemActionSelection", 106);
		}
	}

	public void SetCurrentFocusedWeaponEntity(SpawnedItemEntity weaponEntity)
	{
		_focusedWeaponEntity = weaponEntity;
	}

	public void OnEquipControllerToggle(bool isActive)
	{
		SelectedItemText = "";
		FocusedItemText = "";
		if (isActive && Agent.Main != null)
		{
			EquipActions.Clear();
			EquipActions.Add(new EquipmentActionItemVM(GameTexts.FindText("str_cancel").ToString(), "None", null, OnItemSelected));
			if (_focusedWeaponEntity.WeaponCopy.Item.Type == ItemObject.ItemTypeEnum.Shield && DoesPlayerHaveAtLeastOneShield())
			{
				_pickText.SetTextVariable("ITEM_NAME", _focusedWeaponEntity.WeaponCopy.Item.Name.ToString());
				FocusedItemText = _pickText.ToString();
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
				{
					MissionWeapon weapon = Agent.Main.Equipment[equipmentIndex];
					if (!weapon.IsEmpty && weapon.Item.Type == ItemObject.ItemTypeEnum.Shield)
					{
						string itemTypeAsString = GetItemTypeAsString(weapon.Item);
						bool isCurrentlyWielded = IsWieldedWeaponAtIndex(equipmentIndex);
						string weaponName = GetWeaponName(weapon);
						EquipActions.Add(new EquipmentActionItemVM(weaponName, itemTypeAsString, equipmentIndex, OnItemSelected, isCurrentlyWielded));
					}
				}
			}
			else
			{
				Agent main = Agent.Main;
				if (main != null && main.CanInteractableWeaponBePickedUp(_focusedWeaponEntity))
				{
					_pickText.SetTextVariable("ITEM_NAME", _focusedWeaponEntity.WeaponCopy.Item.Name.ToString());
					FocusedItemText = _pickText.ToString();
					bool flag = Agent.Main.WillDropWieldedShield(_focusedWeaponEntity);
					for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.ExtraWeaponSlot; equipmentIndex2++)
					{
						MissionWeapon weapon2 = Mission.Current.MainAgent.Equipment[equipmentIndex2];
						if (!weapon2.IsEmpty && (!flag || weapon2.IsShield()))
						{
							string itemTypeAsString2 = GetItemTypeAsString(weapon2.Item);
							bool isCurrentlyWielded2 = IsWieldedWeaponAtIndex(equipmentIndex2);
							string weaponName2 = GetWeaponName(weapon2);
							EquipActions.Add(new EquipmentActionItemVM(weaponName2, itemTypeAsString2, equipmentIndex2, OnItemSelected, isCurrentlyWielded2));
						}
					}
				}
				else
				{
					FocusedItemText = _focusedWeaponEntity.WeaponCopy.Item.Name.ToString();
					EquipmentActionItemVM item = new EquipmentActionItemVM(GameTexts.FindText("str_pickup_to_equip").ToString(), "PickUp", _focusedWeaponEntity, OnItemSelected)
					{
						IsSelected = true
					};
					EquipActions.Add(item);
				}
			}
			EquipmentIndex itemIndexThatQuickPickUpWouldReplace = MissionEquipment.SelectWeaponPickUpSlot(Agent.Main, _focusedWeaponEntity.WeaponCopy, _focusedWeaponEntity.IsStuckMissile());
			EquipmentActionItemVM equipmentActionItemVM = EquipActions.SingleOrDefault((EquipmentActionItemVM a) => a.Identifier is EquipmentIndex equipmentIndex3 && equipmentIndex3 == itemIndexThatQuickPickUpWouldReplace);
			if (equipmentActionItemVM != null)
			{
				equipmentActionItemVM.IsSelected = true;
			}
		}
		else
		{
			EquipmentActionItemVM equipmentActionItemVM2 = EquipActions.SingleOrDefault((EquipmentActionItemVM a) => a.IsSelected);
			if (equipmentActionItemVM2 != null)
			{
				HandleEquipItemActionSelection(equipmentActionItemVM2.Identifier);
			}
		}
		IsEquipControllerActive = isActive;
	}

	public void OnCancelEquipController()
	{
		IsEquipControllerActive = false;
		EquipActions.Clear();
	}

	public void OnCancelDropController()
	{
		IsDropControllerActive = false;
		DropActions.Clear();
	}

	private void HandleEquipItemActionSelection(object selectedItem)
	{
		if (selectedItem is EquipmentIndex arg && _focusedWeaponEntity != null)
		{
			_onEquipItem(_focusedWeaponEntity, arg);
		}
		else if (selectedItem is SpawnedItemEntity arg2)
		{
			_onEquipItem(arg2, EquipmentIndex.None);
		}
		else if (selectedItem != null)
		{
			Debug.FailedAssert("Unidentified action on drop wheel", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\HUD\\MissionMainAgentEquipmentControllerVM.cs", "HandleEquipItemActionSelection", 223);
		}
	}

	private void OnItemSelected(EquipmentActionItemVM item)
	{
		if (IsEquipControllerActive)
		{
			if (item.Identifier == null || item.Identifier is SpawnedItemEntity)
			{
				EquipText = "";
			}
			else
			{
				EquipText = _replaceWithLocalizedText.ToString();
			}
		}
		else if (item.Identifier == null)
		{
			DropText = "";
		}
		else
		{
			DropText = _dropLocalizedText.ToString();
		}
		SelectedItemText = item.ActionText;
	}

	private string GetWeaponName(MissionWeapon weapon)
	{
		string text = weapon.Item.Name.ToString();
		WeaponComponentData currentUsageItem = weapon.CurrentUsageItem;
		if (currentUsageItem != null && currentUsageItem.IsShield)
		{
			text = text + " (" + weapon.HitPoints + " / " + weapon.ModifiedMaxHitPoints + ")";
		}
		else
		{
			WeaponComponentData currentUsageItem2 = weapon.CurrentUsageItem;
			if (currentUsageItem2 != null && currentUsageItem2.IsConsumable && weapon.ModifiedMaxAmount > 1)
			{
				text = text + " (" + weapon.Amount + " / " + weapon.ModifiedMaxAmount + ")";
			}
		}
		return text;
	}

	public static string GetItemTypeAsString(ItemObject item)
	{
		if (item.ItemComponent is WeaponComponent)
		{
			switch ((item.ItemComponent as WeaponComponent).PrimaryWeapon.WeaponClass)
			{
			case WeaponClass.Bow:
				return "Bow";
			case WeaponClass.Crossbow:
				return "Crossbow";
			case WeaponClass.OneHandedPolearm:
			case WeaponClass.TwoHandedPolearm:
			case WeaponClass.LowGripPolearm:
				return "Spear";
			case WeaponClass.Dagger:
			case WeaponClass.OneHandedSword:
			case WeaponClass.TwoHandedSword:
				return "Sword";
			case WeaponClass.OneHandedAxe:
			case WeaponClass.TwoHandedAxe:
				return "Axe";
			case WeaponClass.Mace:
			case WeaponClass.TwoHandedMace:
				return "Mace";
			case WeaponClass.Javelin:
				return "Javelin";
			case WeaponClass.ThrowingAxe:
				return "ThrowingAxe";
			case WeaponClass.ThrowingKnife:
				return "ThrowingKnife";
			case WeaponClass.SmallShield:
			case WeaponClass.LargeShield:
				return "Shield";
			case WeaponClass.Arrow:
			case WeaponClass.Bolt:
			case WeaponClass.SlingStone:
			case WeaponClass.Cartridge:
			case WeaponClass.Musket:
				return "Ammo";
			case WeaponClass.Banner:
				return "Banner";
			case WeaponClass.Sling:
			case WeaponClass.Stone:
			case WeaponClass.BallistaStone:
				return "Stone";
			default:
				return "None";
			}
		}
		if (item.ItemComponent is HorseComponent)
		{
			return "Mount";
		}
		return "None";
	}

	private bool DoesPlayerHaveAtLeastOneShield()
	{
		EquipmentIndex offhandWieldedItemIndex = Agent.Main.GetOffhandWieldedItemIndex();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			if (equipmentIndex != offhandWieldedItemIndex && !Agent.Main.Equipment[equipmentIndex].IsEmpty && Mission.Current.MainAgent.Equipment[equipmentIndex].Item.Type == ItemObject.ItemTypeEnum.Shield)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsWieldedWeaponAtIndex(EquipmentIndex index)
	{
		if (index != Agent.Main.GetPrimaryWieldedItemIndex())
		{
			return index == Agent.Main.GetOffhandWieldedItemIndex();
		}
		return true;
	}
}
