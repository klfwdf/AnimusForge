using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000093 RID: 147
	public class SPInventorySortControllerVM : ViewModel
	{
		// Token: 0x17000411 RID: 1041
		// (get) Token: 0x06000CC2 RID: 3266 RVA: 0x00035E76 File Offset: 0x00034076
		// (set) Token: 0x06000CC3 RID: 3267 RVA: 0x00035E7E File Offset: 0x0003407E
		public SPInventorySortControllerVM.InventoryItemSortOption? CurrentSortOption { get; private set; }

		// Token: 0x17000412 RID: 1042
		// (get) Token: 0x06000CC4 RID: 3268 RVA: 0x00035E87 File Offset: 0x00034087
		// (set) Token: 0x06000CC5 RID: 3269 RVA: 0x00035E8F File Offset: 0x0003408F
		public SPInventorySortControllerVM.InventoryItemSortState? CurrentSortState { get; private set; }

		// Token: 0x06000CC6 RID: 3270 RVA: 0x00035E98 File Offset: 0x00034098
		public SPInventorySortControllerVM(ref MBBindingList<SPItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._typeComparer = new SPInventorySortControllerVM.ItemTypeComparer();
			this._nameComparer = new SPInventorySortControllerVM.ItemNameComparer();
			this._quantityComparer = new SPInventorySortControllerVM.ItemQuantityComparer();
			this._costComparer = new SPInventorySortControllerVM.ItemCostComparer();
			this.RefreshValues();
		}

		// Token: 0x06000CC7 RID: 3271 RVA: 0x00035EE5 File Offset: 0x000340E5
		public void SortByOption(SPInventorySortControllerVM.InventoryItemSortOption sortOption, SPInventorySortControllerVM.InventoryItemSortState sortState)
		{
			this.SetAllStates((sortState == SPInventorySortControllerVM.InventoryItemSortState.Ascending) ? SPInventorySortControllerVM.InventoryItemSortState.Descending : SPInventorySortControllerVM.InventoryItemSortState.Ascending);
			if (sortOption == SPInventorySortControllerVM.InventoryItemSortOption.Type)
			{
				this.ExecuteSortByType();
				return;
			}
			if (sortOption == SPInventorySortControllerVM.InventoryItemSortOption.Name)
			{
				this.ExecuteSortByName();
				return;
			}
			if (sortOption == SPInventorySortControllerVM.InventoryItemSortOption.Quantity)
			{
				this.ExecuteSortByQuantity();
				return;
			}
			if (sortOption == SPInventorySortControllerVM.InventoryItemSortOption.Cost)
			{
				this.ExecuteSortByCost();
			}
		}

		// Token: 0x06000CC8 RID: 3272 RVA: 0x00035F1F File Offset: 0x0003411F
		public void SortByDefaultState()
		{
			this.ExecuteSortByType();
		}

		// Token: 0x06000CC9 RID: 3273 RVA: 0x00035F28 File Offset: 0x00034128
		public void SortByCurrentState()
		{
			if (this.IsTypeSelected)
			{
				this._listToControl.Sort(this._typeComparer);
				this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Type);
				return;
			}
			if (this.IsNameSelected)
			{
				this._listToControl.Sort(this._nameComparer);
				this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Name);
				return;
			}
			if (this.IsQuantitySelected)
			{
				this._listToControl.Sort(this._quantityComparer);
				this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Quantity);
				return;
			}
			if (this.IsCostSelected)
			{
				this._listToControl.Sort(this._costComparer);
				this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Cost);
			}
		}

		// Token: 0x06000CCA RID: 3274 RVA: 0x00035FCC File Offset: 0x000341CC
		public void ExecuteSortByName()
		{
			int nameState = this.NameState;
			this.SetAllStates(SPInventorySortControllerVM.InventoryItemSortState.Default);
			this.NameState = (nameState + 1) % 3;
			if (this.NameState == 0)
			{
				this.NameState++;
			}
			this._nameComparer.SetSortMode(this.NameState == 1);
			this.CurrentSortState = new SPInventorySortControllerVM.InventoryItemSortState?((this.NameState == 1) ? SPInventorySortControllerVM.InventoryItemSortState.Ascending : SPInventorySortControllerVM.InventoryItemSortState.Descending);
			this._listToControl.Sort(this._nameComparer);
			this.IsNameSelected = true;
			this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Name);
		}

		// Token: 0x06000CCB RID: 3275 RVA: 0x00036058 File Offset: 0x00034258
		public void ExecuteSortByType()
		{
			int typeState = this.TypeState;
			this.SetAllStates(SPInventorySortControllerVM.InventoryItemSortState.Default);
			this.TypeState = (typeState + 1) % 3;
			if (this.TypeState == 0)
			{
				this.TypeState++;
			}
			this._typeComparer.SetSortMode(this.TypeState == 1);
			this.CurrentSortState = new SPInventorySortControllerVM.InventoryItemSortState?((this.TypeState == 1) ? SPInventorySortControllerVM.InventoryItemSortState.Ascending : SPInventorySortControllerVM.InventoryItemSortState.Descending);
			this._listToControl.Sort(this._typeComparer);
			this.IsTypeSelected = true;
			this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Type);
		}

		// Token: 0x06000CCC RID: 3276 RVA: 0x000360E4 File Offset: 0x000342E4
		public void ExecuteSortByQuantity()
		{
			int quantityState = this.QuantityState;
			this.SetAllStates(SPInventorySortControllerVM.InventoryItemSortState.Default);
			this.QuantityState = (quantityState + 1) % 3;
			if (this.QuantityState == 0)
			{
				this.QuantityState++;
			}
			this._quantityComparer.SetSortMode(this.QuantityState == 1);
			this.CurrentSortState = new SPInventorySortControllerVM.InventoryItemSortState?((this.QuantityState == 1) ? SPInventorySortControllerVM.InventoryItemSortState.Ascending : SPInventorySortControllerVM.InventoryItemSortState.Descending);
			this._listToControl.Sort(this._quantityComparer);
			this.IsQuantitySelected = true;
			this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Quantity);
		}

		// Token: 0x06000CCD RID: 3277 RVA: 0x00036170 File Offset: 0x00034370
		public void ExecuteSortByCost()
		{
			int costState = this.CostState;
			this.SetAllStates(SPInventorySortControllerVM.InventoryItemSortState.Default);
			this.CostState = (costState + 1) % 3;
			if (this.CostState == 0)
			{
				this.CostState++;
			}
			this._costComparer.SetSortMode(this.CostState == 1);
			this.CurrentSortState = new SPInventorySortControllerVM.InventoryItemSortState?((this.CostState == 1) ? SPInventorySortControllerVM.InventoryItemSortState.Ascending : SPInventorySortControllerVM.InventoryItemSortState.Descending);
			this._listToControl.Sort(this._costComparer);
			this.IsCostSelected = true;
			this.CurrentSortOption = new SPInventorySortControllerVM.InventoryItemSortOption?(SPInventorySortControllerVM.InventoryItemSortOption.Cost);
		}

		// Token: 0x06000CCE RID: 3278 RVA: 0x000361FC File Offset: 0x000343FC
		private void SetAllStates(SPInventorySortControllerVM.InventoryItemSortState state)
		{
			this.TypeState = (int)state;
			this.NameState = (int)state;
			this.QuantityState = (int)state;
			this.CostState = (int)state;
			this.IsTypeSelected = false;
			this.IsNameSelected = false;
			this.IsQuantitySelected = false;
			this.IsCostSelected = false;
			this.CurrentSortState = new SPInventorySortControllerVM.InventoryItemSortState?(state);
		}

		// Token: 0x17000413 RID: 1043
		// (get) Token: 0x06000CCF RID: 3279 RVA: 0x0003624D File Offset: 0x0003444D
		// (set) Token: 0x06000CD0 RID: 3280 RVA: 0x00036255 File Offset: 0x00034455
		[DataSourceProperty]
		public int TypeState
		{
			get
			{
				return this._typeState;
			}
			set
			{
				if (value != this._typeState)
				{
					this._typeState = value;
					base.OnPropertyChangedWithValue(value, "TypeState");
				}
			}
		}

		// Token: 0x17000414 RID: 1044
		// (get) Token: 0x06000CD1 RID: 3281 RVA: 0x00036273 File Offset: 0x00034473
		// (set) Token: 0x06000CD2 RID: 3282 RVA: 0x0003627B File Offset: 0x0003447B
		[DataSourceProperty]
		public int NameState
		{
			get
			{
				return this._nameState;
			}
			set
			{
				if (value != this._nameState)
				{
					this._nameState = value;
					base.OnPropertyChangedWithValue(value, "NameState");
				}
			}
		}

		// Token: 0x17000415 RID: 1045
		// (get) Token: 0x06000CD3 RID: 3283 RVA: 0x00036299 File Offset: 0x00034499
		// (set) Token: 0x06000CD4 RID: 3284 RVA: 0x000362A1 File Offset: 0x000344A1
		[DataSourceProperty]
		public int QuantityState
		{
			get
			{
				return this._quantityState;
			}
			set
			{
				if (value != this._quantityState)
				{
					this._quantityState = value;
					base.OnPropertyChangedWithValue(value, "QuantityState");
				}
			}
		}

		// Token: 0x17000416 RID: 1046
		// (get) Token: 0x06000CD5 RID: 3285 RVA: 0x000362BF File Offset: 0x000344BF
		// (set) Token: 0x06000CD6 RID: 3286 RVA: 0x000362C7 File Offset: 0x000344C7
		[DataSourceProperty]
		public int CostState
		{
			get
			{
				return this._costState;
			}
			set
			{
				if (value != this._costState)
				{
					this._costState = value;
					base.OnPropertyChangedWithValue(value, "CostState");
				}
			}
		}

		// Token: 0x17000417 RID: 1047
		// (get) Token: 0x06000CD7 RID: 3287 RVA: 0x000362E5 File Offset: 0x000344E5
		// (set) Token: 0x06000CD8 RID: 3288 RVA: 0x000362ED File Offset: 0x000344ED
		[DataSourceProperty]
		public bool IsTypeSelected
		{
			get
			{
				return this._isTypeSelected;
			}
			set
			{
				if (value != this._isTypeSelected)
				{
					this._isTypeSelected = value;
					base.OnPropertyChangedWithValue(value, "IsTypeSelected");
				}
			}
		}

		// Token: 0x17000418 RID: 1048
		// (get) Token: 0x06000CD9 RID: 3289 RVA: 0x0003630B File Offset: 0x0003450B
		// (set) Token: 0x06000CDA RID: 3290 RVA: 0x00036313 File Offset: 0x00034513
		[DataSourceProperty]
		public bool IsNameSelected
		{
			get
			{
				return this._isNameSelected;
			}
			set
			{
				if (value != this._isNameSelected)
				{
					this._isNameSelected = value;
					base.OnPropertyChangedWithValue(value, "IsNameSelected");
				}
			}
		}

		// Token: 0x17000419 RID: 1049
		// (get) Token: 0x06000CDB RID: 3291 RVA: 0x00036331 File Offset: 0x00034531
		// (set) Token: 0x06000CDC RID: 3292 RVA: 0x00036339 File Offset: 0x00034539
		[DataSourceProperty]
		public bool IsQuantitySelected
		{
			get
			{
				return this._isQuantitySelected;
			}
			set
			{
				if (value != this._isQuantitySelected)
				{
					this._isQuantitySelected = value;
					base.OnPropertyChangedWithValue(value, "IsQuantitySelected");
				}
			}
		}

		// Token: 0x1700041A RID: 1050
		// (get) Token: 0x06000CDD RID: 3293 RVA: 0x00036357 File Offset: 0x00034557
		// (set) Token: 0x06000CDE RID: 3294 RVA: 0x0003635F File Offset: 0x0003455F
		[DataSourceProperty]
		public bool IsCostSelected
		{
			get
			{
				return this._isCostSelected;
			}
			set
			{
				if (value != this._isCostSelected)
				{
					this._isCostSelected = value;
					base.OnPropertyChangedWithValue(value, "IsCostSelected");
				}
			}
		}

		// Token: 0x040005CE RID: 1486
		private MBBindingList<SPItemVM> _listToControl;

		// Token: 0x040005CF RID: 1487
		private SPInventorySortControllerVM.ItemTypeComparer _typeComparer;

		// Token: 0x040005D0 RID: 1488
		private SPInventorySortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x040005D1 RID: 1489
		private SPInventorySortControllerVM.ItemQuantityComparer _quantityComparer;

		// Token: 0x040005D2 RID: 1490
		private SPInventorySortControllerVM.ItemCostComparer _costComparer;

		// Token: 0x040005D5 RID: 1493
		private int _typeState;

		// Token: 0x040005D6 RID: 1494
		private int _nameState;

		// Token: 0x040005D7 RID: 1495
		private int _quantityState;

		// Token: 0x040005D8 RID: 1496
		private int _costState;

		// Token: 0x040005D9 RID: 1497
		private bool _isTypeSelected;

		// Token: 0x040005DA RID: 1498
		private bool _isNameSelected;

		// Token: 0x040005DB RID: 1499
		private bool _isQuantitySelected;

		// Token: 0x040005DC RID: 1500
		private bool _isCostSelected;

		// Token: 0x020001F9 RID: 505
		public enum InventoryItemSortState
		{
			// Token: 0x04001157 RID: 4439
			Default,
			// Token: 0x04001158 RID: 4440
			Ascending,
			// Token: 0x04001159 RID: 4441
			Descending
		}

		// Token: 0x020001FA RID: 506
		public enum InventoryItemSortOption
		{
			// Token: 0x0400115B RID: 4443
			Type,
			// Token: 0x0400115C RID: 4444
			Name,
			// Token: 0x0400115D RID: 4445
			Quantity,
			// Token: 0x0400115E RID: 4446
			Cost
		}

		// Token: 0x020001FB RID: 507
		public abstract class ItemComparer : IComparer<SPItemVM>
		{
			// Token: 0x060023ED RID: 9197 RVA: 0x0007EBEF File Offset: 0x0007CDEF
			public void SetSortMode(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x060023EE RID: 9198
			public abstract int Compare(SPItemVM x, SPItemVM y);

			// Token: 0x060023EF RID: 9199 RVA: 0x0007EBF8 File Offset: 0x0007CDF8
			protected int ResolveEquality(SPItemVM x, SPItemVM y)
			{
				return x.ItemDescription.CompareTo(y.ItemDescription);
			}

			// Token: 0x0400115F RID: 4447
			protected bool _isAscending;
		}

		// Token: 0x020001FC RID: 508
		public class ItemTypeComparer : SPInventorySortControllerVM.ItemComparer
		{
			// Token: 0x060023F1 RID: 9201 RVA: 0x0007EC14 File Offset: 0x0007CE14
			public override int Compare(SPItemVM x, SPItemVM y)
			{
				int itemObjectTypeSortIndex = CampaignUIHelper.GetItemObjectTypeSortIndex(x.ItemRosterElement.EquipmentElement.Item);
				int num = CampaignUIHelper.GetItemObjectTypeSortIndex(y.ItemRosterElement.EquipmentElement.Item).CompareTo(itemObjectTypeSortIndex);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				num = x.ItemCost.CompareTo(y.ItemCost);
				if (num != 0)
				{
					return num;
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001FD RID: 509
		public class ItemNameComparer : SPInventorySortControllerVM.ItemComparer
		{
			// Token: 0x060023F3 RID: 9203 RVA: 0x0007EC99 File Offset: 0x0007CE99
			public override int Compare(SPItemVM x, SPItemVM y)
			{
				if (this._isAscending)
				{
					return y.ItemDescription.CompareTo(x.ItemDescription) * -1;
				}
				return y.ItemDescription.CompareTo(x.ItemDescription);
			}
		}

		// Token: 0x020001FE RID: 510
		public class ItemQuantityComparer : SPInventorySortControllerVM.ItemComparer
		{
			// Token: 0x060023F5 RID: 9205 RVA: 0x0007ECD0 File Offset: 0x0007CED0
			public override int Compare(SPItemVM x, SPItemVM y)
			{
				int num = y.ItemCount.CompareTo(x.ItemCount);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001FF RID: 511
		public class ItemCostComparer : SPInventorySortControllerVM.ItemComparer
		{
			// Token: 0x060023F7 RID: 9207 RVA: 0x0007ED14 File Offset: 0x0007CF14
			public override int Compare(SPItemVM x, SPItemVM y)
			{
				int num = y.ItemCost.CompareTo(x.ItemCost);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}
	}
}
