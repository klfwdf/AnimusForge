using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting
{
	// Token: 0x02000112 RID: 274
	public class SmeltingSortControllerVM : ViewModel
	{
		// Token: 0x060018FC RID: 6396 RVA: 0x0005ED2B File Offset: 0x0005CF2B
		public SmeltingSortControllerVM()
		{
			this._yieldComparer = new SmeltingSortControllerVM.ItemYieldComparer();
			this._typeComparer = new SmeltingSortControllerVM.ItemTypeComparer();
			this._nameComparer = new SmeltingSortControllerVM.ItemNameComparer();
			this.RefreshValues();
		}

		// Token: 0x060018FD RID: 6397 RVA: 0x0005ED5C File Offset: 0x0005CF5C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.SortNameText = new TextObject("{=PDdh1sBj}Name", null).ToString();
			this.SortTypeText = new TextObject("{=zMMqgxb1}Type", null).ToString();
			this.SortYieldText = new TextObject("{=v3OF6vBg}Yield", null).ToString();
		}

		// Token: 0x060018FE RID: 6398 RVA: 0x0005EDB1 File Offset: 0x0005CFB1
		public void SetListToControl(MBBindingList<SmeltingItemVM> listToControl)
		{
			this._listToControl = listToControl;
		}

		// Token: 0x060018FF RID: 6399 RVA: 0x0005EDBC File Offset: 0x0005CFBC
		public void SortByCurrentState()
		{
			if (this.IsNameSelected)
			{
				this._listToControl.Sort(this._nameComparer);
				return;
			}
			if (this.IsYieldSelected)
			{
				this._listToControl.Sort(this._yieldComparer);
				return;
			}
			if (this.IsTypeSelected)
			{
				this._listToControl.Sort(this._typeComparer);
			}
		}

		// Token: 0x06001900 RID: 6400 RVA: 0x0005EE18 File Offset: 0x0005D018
		public void ExecuteSortByName()
		{
			int nameState = this.NameState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.NameState = (nameState + 1) % 3;
			if (this.NameState == 0)
			{
				this.NameState++;
			}
			this._nameComparer.SetSortMode(this.NameState == 1);
			this._listToControl.Sort(this._nameComparer);
			this.IsNameSelected = true;
		}

		// Token: 0x06001901 RID: 6401 RVA: 0x0005EE80 File Offset: 0x0005D080
		public void ExecuteSortByYield()
		{
			int yieldState = this.YieldState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.YieldState = (yieldState + 1) % 3;
			if (this.YieldState == 0)
			{
				this.YieldState++;
			}
			this._yieldComparer.SetSortMode(this.YieldState == 1);
			this._listToControl.Sort(this._yieldComparer);
			this.IsYieldSelected = true;
		}

		// Token: 0x06001902 RID: 6402 RVA: 0x0005EEE8 File Offset: 0x0005D0E8
		public void ExecuteSortByType()
		{
			int typeState = this.TypeState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.TypeState = (typeState + 1) % 3;
			if (this.TypeState == 0)
			{
				this.TypeState++;
			}
			this._typeComparer.SetSortMode(this.TypeState == 1);
			this._listToControl.Sort(this._typeComparer);
			this.IsTypeSelected = true;
		}

		// Token: 0x06001903 RID: 6403 RVA: 0x0005EF50 File Offset: 0x0005D150
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.NameState = (int)state;
			this.TypeState = (int)state;
			this.YieldState = (int)state;
			this.IsNameSelected = false;
			this.IsTypeSelected = false;
			this.IsYieldSelected = false;
		}

		// Token: 0x17000862 RID: 2146
		// (get) Token: 0x06001904 RID: 6404 RVA: 0x0005EF7C File Offset: 0x0005D17C
		// (set) Token: 0x06001905 RID: 6405 RVA: 0x0005EF84 File Offset: 0x0005D184
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

		// Token: 0x17000863 RID: 2147
		// (get) Token: 0x06001906 RID: 6406 RVA: 0x0005EFA2 File Offset: 0x0005D1A2
		// (set) Token: 0x06001907 RID: 6407 RVA: 0x0005EFAA File Offset: 0x0005D1AA
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

		// Token: 0x17000864 RID: 2148
		// (get) Token: 0x06001908 RID: 6408 RVA: 0x0005EFC8 File Offset: 0x0005D1C8
		// (set) Token: 0x06001909 RID: 6409 RVA: 0x0005EFD0 File Offset: 0x0005D1D0
		[DataSourceProperty]
		public int YieldState
		{
			get
			{
				return this._yieldState;
			}
			set
			{
				if (value != this._yieldState)
				{
					this._yieldState = value;
					base.OnPropertyChangedWithValue(value, "YieldState");
				}
			}
		}

		// Token: 0x17000865 RID: 2149
		// (get) Token: 0x0600190A RID: 6410 RVA: 0x0005EFEE File Offset: 0x0005D1EE
		// (set) Token: 0x0600190B RID: 6411 RVA: 0x0005EFF6 File Offset: 0x0005D1F6
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

		// Token: 0x17000866 RID: 2150
		// (get) Token: 0x0600190C RID: 6412 RVA: 0x0005F014 File Offset: 0x0005D214
		// (set) Token: 0x0600190D RID: 6413 RVA: 0x0005F01C File Offset: 0x0005D21C
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

		// Token: 0x17000867 RID: 2151
		// (get) Token: 0x0600190E RID: 6414 RVA: 0x0005F03A File Offset: 0x0005D23A
		// (set) Token: 0x0600190F RID: 6415 RVA: 0x0005F042 File Offset: 0x0005D242
		[DataSourceProperty]
		public bool IsYieldSelected
		{
			get
			{
				return this._isYieldSelected;
			}
			set
			{
				if (value != this._isYieldSelected)
				{
					this._isYieldSelected = value;
					base.OnPropertyChangedWithValue(value, "IsYieldSelected");
				}
			}
		}

		// Token: 0x17000868 RID: 2152
		// (get) Token: 0x06001910 RID: 6416 RVA: 0x0005F060 File Offset: 0x0005D260
		// (set) Token: 0x06001911 RID: 6417 RVA: 0x0005F068 File Offset: 0x0005D268
		[DataSourceProperty]
		public string SortTypeText
		{
			get
			{
				return this._sortTypeText;
			}
			set
			{
				if (value != this._sortTypeText)
				{
					this._sortTypeText = value;
					base.OnPropertyChangedWithValue<string>(value, "SortTypeText");
				}
			}
		}

		// Token: 0x17000869 RID: 2153
		// (get) Token: 0x06001912 RID: 6418 RVA: 0x0005F08B File Offset: 0x0005D28B
		// (set) Token: 0x06001913 RID: 6419 RVA: 0x0005F093 File Offset: 0x0005D293
		[DataSourceProperty]
		public string SortNameText
		{
			get
			{
				return this._sortNameText;
			}
			set
			{
				if (value != this._sortNameText)
				{
					this._sortNameText = value;
					base.OnPropertyChangedWithValue<string>(value, "SortNameText");
				}
			}
		}

		// Token: 0x1700086A RID: 2154
		// (get) Token: 0x06001914 RID: 6420 RVA: 0x0005F0B6 File Offset: 0x0005D2B6
		// (set) Token: 0x06001915 RID: 6421 RVA: 0x0005F0BE File Offset: 0x0005D2BE
		[DataSourceProperty]
		public string SortYieldText
		{
			get
			{
				return this._sortYieldText;
			}
			set
			{
				if (value != this._sortYieldText)
				{
					this._sortYieldText = value;
					base.OnPropertyChangedWithValue<string>(value, "SortYieldText");
				}
			}
		}

		// Token: 0x04000B7D RID: 2941
		private MBBindingList<SmeltingItemVM> _listToControl;

		// Token: 0x04000B7E RID: 2942
		private readonly SmeltingSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x04000B7F RID: 2943
		private readonly SmeltingSortControllerVM.ItemYieldComparer _yieldComparer;

		// Token: 0x04000B80 RID: 2944
		private readonly SmeltingSortControllerVM.ItemTypeComparer _typeComparer;

		// Token: 0x04000B81 RID: 2945
		private int _nameState;

		// Token: 0x04000B82 RID: 2946
		private int _yieldState;

		// Token: 0x04000B83 RID: 2947
		private int _typeState;

		// Token: 0x04000B84 RID: 2948
		private bool _isNameSelected;

		// Token: 0x04000B85 RID: 2949
		private bool _isYieldSelected;

		// Token: 0x04000B86 RID: 2950
		private bool _isTypeSelected;

		// Token: 0x04000B87 RID: 2951
		private string _sortTypeText;

		// Token: 0x04000B88 RID: 2952
		private string _sortNameText;

		// Token: 0x04000B89 RID: 2953
		private string _sortYieldText;

		// Token: 0x02000272 RID: 626
		public abstract class ItemComparerBase : IComparer<SmeltingItemVM>
		{
			// Token: 0x0600255E RID: 9566 RVA: 0x000809D1 File Offset: 0x0007EBD1
			public void SetSortMode(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x0600255F RID: 9567
			public abstract int Compare(SmeltingItemVM x, SmeltingItemVM y);

			// Token: 0x06002560 RID: 9568 RVA: 0x000809DA File Offset: 0x0007EBDA
			protected int ResolveEquality(SmeltingItemVM x, SmeltingItemVM y)
			{
				return x.Name.CompareTo(y.Name);
			}

			// Token: 0x0400129E RID: 4766
			protected bool _isAscending;
		}

		// Token: 0x02000273 RID: 627
		public class ItemNameComparer : SmeltingSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002562 RID: 9570 RVA: 0x000809F5 File Offset: 0x0007EBF5
			public override int Compare(SmeltingItemVM x, SmeltingItemVM y)
			{
				if (this._isAscending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x02000274 RID: 628
		public class ItemYieldComparer : SmeltingSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002564 RID: 9572 RVA: 0x00080A2C File Offset: 0x0007EC2C
			public override int Compare(SmeltingItemVM x, SmeltingItemVM y)
			{
				int num = y.Yield.Count.CompareTo(x.Yield.Count);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x02000275 RID: 629
		public class ItemTypeComparer : SmeltingSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002566 RID: 9574 RVA: 0x00080A7C File Offset: 0x0007EC7C
			public override int Compare(SmeltingItemVM x, SmeltingItemVM y)
			{
				int itemObjectTypeSortIndex = CampaignUIHelper.GetItemObjectTypeSortIndex(x.EquipmentElement.Item);
				int num = CampaignUIHelper.GetItemObjectTypeSortIndex(y.EquipmentElement.Item).CompareTo(itemObjectTypeSortIndex);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}
	}
}
