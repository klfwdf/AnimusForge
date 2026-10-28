using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaUnitEquipmentSetSelectorItemVM : SelectorItemVM
{
	private MBBindingList<CharacterEquipmentItemVM> _leftEquipmentList;

	private MBBindingList<CharacterEquipmentItemVM> _rightEquipmentList;

	public Equipment EquipmentSet { get; private set; }

	[DataSourceProperty]
	public MBBindingList<CharacterEquipmentItemVM> LeftEquipmentList
	{
		get
		{
			return _leftEquipmentList;
		}
		set
		{
			if (value != _leftEquipmentList)
			{
				_leftEquipmentList = value;
				OnPropertyChangedWithValue(value, "LeftEquipmentList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterEquipmentItemVM> RightEquipmentList
	{
		get
		{
			return _rightEquipmentList;
		}
		set
		{
			if (value != _rightEquipmentList)
			{
				_rightEquipmentList = value;
				OnPropertyChangedWithValue(value, "RightEquipmentList");
			}
		}
	}

	public EncyclopediaUnitEquipmentSetSelectorItemVM(Equipment equipmentSet, string name = "")
		: base(name)
	{
		EquipmentSet = equipmentSet;
		LeftEquipmentList = new MBBindingList<CharacterEquipmentItemVM>();
		RightEquipmentList = new MBBindingList<CharacterEquipmentItemVM>();
		RefreshEquipment();
	}

	private void RefreshEquipment()
	{
		LeftEquipmentList.Clear();
		LeftEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.NumAllWeaponSlots].Item));
		LeftEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.Cape].Item));
		LeftEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.Body].Item));
		LeftEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.Gloves].Item));
		LeftEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.Leg].Item));
		LeftEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.ArmorItemEndSlot].Item));
		RightEquipmentList.Clear();
		RightEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.WeaponItemBeginSlot].Item));
		RightEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.Weapon1].Item));
		RightEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.Weapon2].Item));
		RightEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.Weapon3].Item));
		RightEquipmentList.Add(new CharacterEquipmentItemVM(EquipmentSet[EquipmentIndex.ExtraWeaponSlot].Item));
	}
}
