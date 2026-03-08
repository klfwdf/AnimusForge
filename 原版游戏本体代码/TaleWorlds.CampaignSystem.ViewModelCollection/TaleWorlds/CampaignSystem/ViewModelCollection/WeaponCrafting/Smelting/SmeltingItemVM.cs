using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting
{
	// Token: 0x02000111 RID: 273
	public class SmeltingItemVM : ViewModel
	{
		// Token: 0x17000858 RID: 2136
		// (get) Token: 0x060018E4 RID: 6372 RVA: 0x0005EA5A File Offset: 0x0005CC5A
		// (set) Token: 0x060018E3 RID: 6371 RVA: 0x0005EA51 File Offset: 0x0005CC51
		public EquipmentElement EquipmentElement { get; private set; }

		// Token: 0x060018E5 RID: 6373 RVA: 0x0005EA64 File Offset: 0x0005CC64
		public SmeltingItemVM(EquipmentElement equipmentElement, Action<SmeltingItemVM> onSelection, Action<SmeltingItemVM, bool> onItemLockedStateChange, bool isLocked, int numOfItems)
		{
			this._onSelection = onSelection;
			this._onItemLockedStateChange = onItemLockedStateChange;
			this.EquipmentElement = equipmentElement;
			this.Yield = new MBBindingList<CraftingResourceItemVM>();
			this.InputMaterials = new MBBindingList<CraftingResourceItemVM>();
			this.LockHint = new HintViewModel(GameTexts.FindText("str_inventory_lock", null), null);
			int[] smeltingOutputForItem = Campaign.Current.Models.SmithingModel.GetSmeltingOutputForItem(equipmentElement.Item);
			for (int i = 0; i < smeltingOutputForItem.Length; i++)
			{
				if (smeltingOutputForItem[i] > 0)
				{
					this.Yield.Add(new CraftingResourceItemVM((CraftingMaterials)i, smeltingOutputForItem[i], 0));
				}
				else if (smeltingOutputForItem[i] < 0)
				{
					this.InputMaterials.Add(new CraftingResourceItemVM((CraftingMaterials)i, -smeltingOutputForItem[i], 0));
				}
			}
			this.IsLocked = isLocked;
			this.Visual = new ItemImageIdentifierVM(equipmentElement.Item, "");
			this.NumOfItems = numOfItems;
			this.HasMoreThanOneItem = this.NumOfItems > 1;
			this.RefreshValues();
		}

		// Token: 0x060018E6 RID: 6374 RVA: 0x0005EB58 File Offset: 0x0005CD58
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.EquipmentElement.Item.Name.ToString();
		}

		// Token: 0x060018E7 RID: 6375 RVA: 0x0005EB89 File Offset: 0x0005CD89
		public void ExecuteSelection()
		{
			this._onSelection(this);
		}

		// Token: 0x060018E8 RID: 6376 RVA: 0x0005EB97 File Offset: 0x0005CD97
		public void ExecuteShowItemTooltip()
		{
			InformationManager.ShowTooltip(typeof(ItemObject), new object[] { this.EquipmentElement });
		}

		// Token: 0x060018E9 RID: 6377 RVA: 0x0005EBBC File Offset: 0x0005CDBC
		public void ExecuteHideItemTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x17000859 RID: 2137
		// (get) Token: 0x060018EA RID: 6378 RVA: 0x0005EBC3 File Offset: 0x0005CDC3
		// (set) Token: 0x060018EB RID: 6379 RVA: 0x0005EBCB File Offset: 0x0005CDCB
		[DataSourceProperty]
		public ItemImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<ItemImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x1700085A RID: 2138
		// (get) Token: 0x060018EC RID: 6380 RVA: 0x0005EBE9 File Offset: 0x0005CDE9
		// (set) Token: 0x060018ED RID: 6381 RVA: 0x0005EBF1 File Offset: 0x0005CDF1
		[DataSourceProperty]
		public MBBindingList<CraftingResourceItemVM> Yield
		{
			get
			{
				return this._yield;
			}
			set
			{
				if (value != this._yield)
				{
					this._yield = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingResourceItemVM>>(value, "Yield");
				}
			}
		}

		// Token: 0x1700085B RID: 2139
		// (get) Token: 0x060018EE RID: 6382 RVA: 0x0005EC0F File Offset: 0x0005CE0F
		// (set) Token: 0x060018EF RID: 6383 RVA: 0x0005EC17 File Offset: 0x0005CE17
		[DataSourceProperty]
		public MBBindingList<CraftingResourceItemVM> InputMaterials
		{
			get
			{
				return this._inputMaterials;
			}
			set
			{
				if (value != this._inputMaterials)
				{
					this._inputMaterials = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingResourceItemVM>>(value, "InputMaterials");
				}
			}
		}

		// Token: 0x1700085C RID: 2140
		// (get) Token: 0x060018F0 RID: 6384 RVA: 0x0005EC35 File Offset: 0x0005CE35
		// (set) Token: 0x060018F1 RID: 6385 RVA: 0x0005EC3D File Offset: 0x0005CE3D
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x1700085D RID: 2141
		// (get) Token: 0x060018F2 RID: 6386 RVA: 0x0005EC60 File Offset: 0x0005CE60
		// (set) Token: 0x060018F3 RID: 6387 RVA: 0x0005EC68 File Offset: 0x0005CE68
		[DataSourceProperty]
		public int NumOfItems
		{
			get
			{
				return this._numOfItems;
			}
			set
			{
				if (value != this._numOfItems)
				{
					this._numOfItems = value;
					base.OnPropertyChangedWithValue(value, "NumOfItems");
				}
			}
		}

		// Token: 0x1700085E RID: 2142
		// (get) Token: 0x060018F4 RID: 6388 RVA: 0x0005EC86 File Offset: 0x0005CE86
		// (set) Token: 0x060018F5 RID: 6389 RVA: 0x0005EC8E File Offset: 0x0005CE8E
		[DataSourceProperty]
		public bool HasMoreThanOneItem
		{
			get
			{
				return this._hasMoreThanOneItem;
			}
			set
			{
				if (value != this._hasMoreThanOneItem)
				{
					this._hasMoreThanOneItem = value;
					base.OnPropertyChangedWithValue(value, "HasMoreThanOneItem");
				}
			}
		}

		// Token: 0x1700085F RID: 2143
		// (get) Token: 0x060018F6 RID: 6390 RVA: 0x0005ECAC File Offset: 0x0005CEAC
		// (set) Token: 0x060018F7 RID: 6391 RVA: 0x0005ECB4 File Offset: 0x0005CEB4
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000860 RID: 2144
		// (get) Token: 0x060018F8 RID: 6392 RVA: 0x0005ECD2 File Offset: 0x0005CED2
		// (set) Token: 0x060018F9 RID: 6393 RVA: 0x0005ECDA File Offset: 0x0005CEDA
		[DataSourceProperty]
		public HintViewModel LockHint
		{
			get
			{
				return this._lockHint;
			}
			set
			{
				if (value != this._lockHint)
				{
					this._lockHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "LockHint");
				}
			}
		}

		// Token: 0x17000861 RID: 2145
		// (get) Token: 0x060018FA RID: 6394 RVA: 0x0005ECF8 File Offset: 0x0005CEF8
		// (set) Token: 0x060018FB RID: 6395 RVA: 0x0005ED00 File Offset: 0x0005CF00
		[DataSourceProperty]
		public bool IsLocked
		{
			get
			{
				return this._isLocked;
			}
			set
			{
				if (value != this._isLocked)
				{
					this._isLocked = value;
					base.OnPropertyChangedWithValue(value, "IsLocked");
					this._onItemLockedStateChange(this, value);
				}
			}
		}

		// Token: 0x04000B72 RID: 2930
		private readonly Action<SmeltingItemVM> _onSelection;

		// Token: 0x04000B73 RID: 2931
		private readonly Action<SmeltingItemVM, bool> _onItemLockedStateChange;

		// Token: 0x04000B74 RID: 2932
		private ItemImageIdentifierVM _visual;

		// Token: 0x04000B75 RID: 2933
		private string _name;

		// Token: 0x04000B76 RID: 2934
		private int _numOfItems;

		// Token: 0x04000B77 RID: 2935
		private MBBindingList<CraftingResourceItemVM> _inputMaterials;

		// Token: 0x04000B78 RID: 2936
		private MBBindingList<CraftingResourceItemVM> _yield;

		// Token: 0x04000B79 RID: 2937
		private HintViewModel _lockHint;

		// Token: 0x04000B7A RID: 2938
		private bool _isSelected;

		// Token: 0x04000B7B RID: 2939
		private bool _isLocked;

		// Token: 0x04000B7C RID: 2940
		private bool _hasMoreThanOneItem;
	}
}
