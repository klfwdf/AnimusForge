using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000EE RID: 238
	public class EncyclopediaUnitEquipmentSetSelectorItemVM : SelectorItemVM
	{
		// Token: 0x17000730 RID: 1840
		// (get) Token: 0x060015B7 RID: 5559 RVA: 0x00054C61 File Offset: 0x00052E61
		// (set) Token: 0x060015B8 RID: 5560 RVA: 0x00054C69 File Offset: 0x00052E69
		public Equipment EquipmentSet { get; private set; }

		// Token: 0x060015B9 RID: 5561 RVA: 0x00054C72 File Offset: 0x00052E72
		public EncyclopediaUnitEquipmentSetSelectorItemVM(Equipment equipmentSet, string name = "")
			: base(name)
		{
			this.EquipmentSet = equipmentSet;
			this.LeftEquipmentList = new MBBindingList<CharacterEquipmentItemVM>();
			this.RightEquipmentList = new MBBindingList<CharacterEquipmentItemVM>();
			this.RefreshEquipment();
		}

		// Token: 0x060015BA RID: 5562 RVA: 0x00054CA0 File Offset: 0x00052EA0
		private void RefreshEquipment()
		{
			this.LeftEquipmentList.Clear();
			this.LeftEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.NumAllWeaponSlots].Item));
			this.LeftEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.Cape].Item));
			this.LeftEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.Body].Item));
			this.LeftEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.Gloves].Item));
			this.LeftEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.Leg].Item));
			this.LeftEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.ArmorItemEndSlot].Item));
			this.RightEquipmentList.Clear();
			this.RightEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.WeaponItemBeginSlot].Item));
			this.RightEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.Weapon1].Item));
			this.RightEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.Weapon2].Item));
			this.RightEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.Weapon3].Item));
			this.RightEquipmentList.Add(new CharacterEquipmentItemVM(this.EquipmentSet[EquipmentIndex.ExtraWeaponSlot].Item));
		}

		// Token: 0x17000731 RID: 1841
		// (get) Token: 0x060015BB RID: 5563 RVA: 0x00054E51 File Offset: 0x00053051
		// (set) Token: 0x060015BC RID: 5564 RVA: 0x00054E59 File Offset: 0x00053059
		[DataSourceProperty]
		public MBBindingList<CharacterEquipmentItemVM> LeftEquipmentList
		{
			get
			{
				return this._leftEquipmentList;
			}
			set
			{
				if (value != this._leftEquipmentList)
				{
					this._leftEquipmentList = value;
					base.OnPropertyChangedWithValue<MBBindingList<CharacterEquipmentItemVM>>(value, "LeftEquipmentList");
				}
			}
		}

		// Token: 0x17000732 RID: 1842
		// (get) Token: 0x060015BD RID: 5565 RVA: 0x00054E77 File Offset: 0x00053077
		// (set) Token: 0x060015BE RID: 5566 RVA: 0x00054E7F File Offset: 0x0005307F
		[DataSourceProperty]
		public MBBindingList<CharacterEquipmentItemVM> RightEquipmentList
		{
			get
			{
				return this._rightEquipmentList;
			}
			set
			{
				if (value != this._rightEquipmentList)
				{
					this._rightEquipmentList = value;
					base.OnPropertyChangedWithValue<MBBindingList<CharacterEquipmentItemVM>>(value, "RightEquipmentList");
				}
			}
		}

		// Token: 0x040009E3 RID: 2531
		private MBBindingList<CharacterEquipmentItemVM> _leftEquipmentList;

		// Token: 0x040009E4 RID: 2532
		private MBBindingList<CharacterEquipmentItemVM> _rightEquipmentList;
	}
}
