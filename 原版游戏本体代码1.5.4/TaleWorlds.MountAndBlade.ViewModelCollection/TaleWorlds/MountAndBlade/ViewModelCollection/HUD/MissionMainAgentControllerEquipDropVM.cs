using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionMainAgentControllerEquipDropVM : ViewModel
{
	private EquipmentActionItemVM _lastSelectedItem;

	private Action<EquipmentIndex> _toggleItem;

	private TextObject _dropTextObject = new TextObject("{=d1tCz15N}Hold to Drop");

	private MBBindingList<ControllerEquippedItemVM> _equipActions;

	private ControllerEquippedItemVM _equippedExtraWeapon;

	private bool _isActive;

	private bool _haveExtraWeapon;

	private string _holdToDropText;

	private string _pressToEquipText;

	[DataSourceProperty]
	public MBBindingList<ControllerEquippedItemVM> EquippedWeapons
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
				OnPropertyChangedWithValue(value, "EquippedWeapons");
			}
		}
	}

	[DataSourceProperty]
	public ControllerEquippedItemVM EquippedExtraWeapon
	{
		get
		{
			return _equippedExtraWeapon;
		}
		set
		{
			if (value != _equippedExtraWeapon)
			{
				_equippedExtraWeapon = value;
				OnPropertyChangedWithValue(value, "EquippedExtraWeapon");
			}
		}
	}

	[DataSourceProperty]
	public string HoldToDropText
	{
		get
		{
			return _holdToDropText;
		}
		set
		{
			if (value != _holdToDropText)
			{
				_holdToDropText = value;
				OnPropertyChangedWithValue(value, "HoldToDropText");
			}
		}
	}

	[DataSourceProperty]
	public string PressToEquipText
	{
		get
		{
			return _pressToEquipText;
		}
		set
		{
			if (value != _pressToEquipText)
			{
				_pressToEquipText = value;
				OnPropertyChangedWithValue(value, "PressToEquipText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool HaveExtraWeapon
	{
		get
		{
			return _haveExtraWeapon;
		}
		set
		{
			if (value != _haveExtraWeapon)
			{
				_haveExtraWeapon = value;
				OnPropertyChangedWithValue(value, "HaveExtraWeapon");
			}
		}
	}

	public MissionMainAgentControllerEquipDropVM(Action<EquipmentIndex> toggleItem)
	{
		_toggleItem = toggleItem;
		EquippedWeapons = new MBBindingList<ControllerEquippedItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PressToEquipText = new TextObject("{=HEEZhL90}Press to Equip").ToString();
		HoldToDropText = _dropTextObject.ToString();
	}

	private bool IsMainAgentAvailable()
	{
		Agent main = Agent.Main;
		if (main != null && main.IsActive() && !Agent.Main.IsUsingGameObject)
		{
			return !Agent.Main.IsInWater();
		}
		return false;
	}

	public void InitializeMainAgentPropterties()
	{
		Mission.Current.OnMainAgentChanged += OnMainAgentChanged;
		OnMainAgentChanged(null);
	}

	private void OnMainAgentChanged(Agent oldAgent)
	{
		if (oldAgent != null)
		{
			oldAgent.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)Delegate.Remove(oldAgent.OnMainAgentWieldedItemChange, new Agent.OnMainAgentWieldedItemChangeDelegate(OnMainAgentWeaponChange));
		}
		if (Agent.Main != null)
		{
			Agent main = Agent.Main;
			main.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)Delegate.Combine(main.OnMainAgentWieldedItemChange, new Agent.OnMainAgentWieldedItemChangeDelegate(OnMainAgentWeaponChange));
		}
	}

	private void OnMainAgentWeaponChange()
	{
		UpdateItemsWieldStatus();
	}

	public void OnToggle(bool isEnabled)
	{
		EquippedWeapons.ApplyActionOnAllItems(delegate(ControllerEquippedItemVM o)
		{
			o.OnFinalize();
		});
		EquippedWeapons.Clear();
		EquippedExtraWeapon = null;
		HaveExtraWeapon = false;
		if (isEnabled)
		{
			PressToEquipText = (IsMainAgentAvailable() ? new TextObject("{=HEEZhL90}Press to Equip").ToString() : string.Empty);
			EquippedWeapons.Add(new ControllerEquippedItemVM(GameTexts.FindText("str_cancel").ToString(), null, "None", null, OnItemSelected));
			int num = 0;
			int totalNumberOfWeaponsOnMainAgent = GetTotalNumberOfWeaponsOnMainAgent();
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
			{
				MissionWeapon weapon = Agent.Main.Equipment[equipmentIndex];
				if (!weapon.IsEmpty)
				{
					string itemTypeAsString = MissionMainAgentEquipmentControllerVM.GetItemTypeAsString(weapon.Item);
					string weaponName = GetWeaponName(weapon);
					EquippedWeapons.Add(new ControllerEquippedItemVM(weaponName, itemTypeAsString, equipmentIndex, GetWeaponHotKey(num, totalNumberOfWeaponsOnMainAgent), OnItemSelected));
					num++;
				}
			}
			MissionWeapon weapon2 = Agent.Main.Equipment[EquipmentIndex.ExtraWeaponSlot];
			HaveExtraWeapon = !weapon2.IsEmpty;
			if (HaveExtraWeapon)
			{
				string itemTypeAsString2 = MissionMainAgentEquipmentControllerVM.GetItemTypeAsString(weapon2.Item);
				string weaponName2 = GetWeaponName(weapon2);
				EquippedExtraWeapon = new ControllerEquippedItemVM(weaponName2, itemTypeAsString2, EquipmentIndex.ExtraWeaponSlot, GetWeaponHotKey(4, totalNumberOfWeaponsOnMainAgent), OnItemSelected);
				num++;
			}
			UpdateItemsWieldStatus();
		}
		else
		{
			if (_lastSelectedItem != null && _lastSelectedItem.Identifier is EquipmentIndex)
			{
				_toggleItem?.Invoke((EquipmentIndex)_lastSelectedItem.Identifier);
			}
			_lastSelectedItem = null;
		}
		IsActive = isEnabled;
	}

	private void OnItemSelected(EquipmentActionItemVM selectedItem)
	{
		if (_lastSelectedItem != selectedItem)
		{
			_lastSelectedItem = selectedItem;
		}
	}

	public void OnCancelHoldController()
	{
	}

	public void OnWeaponDroppedAtIndex(int droppedWeaponIndex)
	{
		OnToggle(isEnabled: true);
	}

	private bool IsWieldedWeaponAtIndex(EquipmentIndex index)
	{
		if (index != Agent.Main.GetPrimaryWieldedItemIndex())
		{
			return index == Agent.Main.GetOffhandWieldedItemIndex();
		}
		return true;
	}

	public void OnWeaponEquippedAtIndex(int equippedWeaponIndex)
	{
		UpdateItemsWieldStatus();
	}

	public void SetDropProgressForIndex(EquipmentIndex eqIndex, float progress)
	{
		for (int i = 0; i < EquippedWeapons.Count; i++)
		{
			float dropProgress = ((EquippedWeapons[i].Identifier is EquipmentIndex equipmentIndex && equipmentIndex == eqIndex && progress > 0.2f) ? progress : 0f);
			EquippedWeapons[i].DropProgress = dropProgress;
		}
		if (HaveExtraWeapon)
		{
			float dropProgress2 = ((EquippedExtraWeapon.Identifier is EquipmentIndex equipmentIndex2 && equipmentIndex2 == eqIndex && progress > 0.2f) ? progress : 0f);
			EquippedExtraWeapon.DropProgress = dropProgress2;
		}
	}

	private void UpdateItemsWieldStatus()
	{
		for (int i = 0; i < EquippedWeapons.Count; i++)
		{
			if (EquippedWeapons[i].Identifier is EquipmentIndex index)
			{
				EquippedWeapons[i].IsWielded = IsWieldedWeaponAtIndex(index);
			}
		}
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

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (Agent.Main != null)
		{
			Agent main = Agent.Main;
			main.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)Delegate.Remove(main.OnMainAgentWieldedItemChange, new Agent.OnMainAgentWieldedItemChangeDelegate(OnMainAgentWeaponChange));
		}
		Mission.Current.OnMainAgentChanged -= OnMainAgentChanged;
		EquippedWeapons.ApplyActionOnAllItems(delegate(ControllerEquippedItemVM o)
		{
			o.OnFinalize();
		});
		EquippedWeapons.Clear();
	}

	private static HotKey GetWeaponHotKey(int currentIndexOfWeapon, int totalNumOfWeapons)
	{
		switch (currentIndexOfWeapon)
		{
		case 0:
			if (totalNumOfWeapons == 1)
			{
				return HotKeyManager.GetCategory("CombatHotKeyCategory").GetHotKey("ControllerEquipDropWeapon4");
			}
			if (totalNumOfWeapons > 1)
			{
				return HotKeyManager.GetCategory("CombatHotKeyCategory").GetHotKey("ControllerEquipDropWeapon1");
			}
			Debug.FailedAssert("Wrong number of total weapons!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\HUD\\MissionMainAgentControllerEquipDropVM.cs", "GetWeaponHotKey", 222);
			break;
		case 1:
			if (totalNumOfWeapons == 2)
			{
				return HotKeyManager.GetCategory("CombatHotKeyCategory").GetHotKey("ControllerEquipDropWeapon3");
			}
			if (totalNumOfWeapons > 2)
			{
				return HotKeyManager.GetCategory("CombatHotKeyCategory").GetHotKey("ControllerEquipDropWeapon4");
			}
			break;
		case 2:
			return HotKeyManager.GetCategory("CombatHotKeyCategory").GetHotKey("ControllerEquipDropWeapon3");
		case 3:
			return HotKeyManager.GetCategory("CombatHotKeyCategory").GetHotKey("ControllerEquipDropWeapon2");
		case 4:
			return HotKeyManager.GetCategory("CombatHotKeyCategory").GetHotKey("ControllerEquipDropExtraWeapon");
		default:
			Debug.FailedAssert("Wrong index of current weapon. Cannot be higher than 3", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\HUD\\MissionMainAgentControllerEquipDropVM.cs", "GetWeaponHotKey", 250);
			break;
		}
		return null;
	}

	public void OnGamepadActiveChanged(bool isActive)
	{
		HoldToDropText = (isActive ? _dropTextObject.ToString() : string.Empty);
	}

	private int GetTotalNumberOfWeaponsOnMainAgent()
	{
		int num = 0;
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
		{
			if (!Agent.Main.Equipment[equipmentIndex].IsEmpty)
			{
				num++;
			}
		}
		return num;
	}
}
