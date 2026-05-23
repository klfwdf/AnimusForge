using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements
{
	// Token: 0x02000068 RID: 104
	public class KingdomSettlementSortControllerVM : ViewModel
	{
		// Token: 0x06000816 RID: 2070 RVA: 0x00025208 File Offset: 0x00023408
		public KingdomSettlementSortControllerVM(MBBindingList<KingdomSettlementItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._typeComparer = new KingdomSettlementSortControllerVM.ItemTypeComparer();
			this._prosperityComparer = new KingdomSettlementSortControllerVM.ItemProsperityComparer();
			this._defendersComparer = new KingdomSettlementSortControllerVM.ItemDefendersComparer();
			this._ownerComparer = new KingdomSettlementSortControllerVM.ItemOwnerComparer();
			this._nameComparer = new KingdomSettlementSortControllerVM.ItemNameComparer();
		}

		// Token: 0x06000817 RID: 2071 RVA: 0x0002525C File Offset: 0x0002345C
		private void ExecuteSortByType()
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

		// Token: 0x06000818 RID: 2072 RVA: 0x000252C4 File Offset: 0x000234C4
		private void ExecuteSortByName()
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

		// Token: 0x06000819 RID: 2073 RVA: 0x0002532C File Offset: 0x0002352C
		private void ExecuteSortByOwner()
		{
			int ownerState = this.OwnerState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.OwnerState = (ownerState + 1) % 3;
			if (this.OwnerState == 0)
			{
				this.OwnerState++;
			}
			this._ownerComparer.SetSortMode(this.OwnerState == 1);
			this._listToControl.Sort(this._ownerComparer);
			this.IsOwnerSelected = true;
		}

		// Token: 0x0600081A RID: 2074 RVA: 0x00025394 File Offset: 0x00023594
		private void ExecuteSortByProsperity()
		{
			int prosperityState = this.ProsperityState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.ProsperityState = (prosperityState + 1) % 3;
			if (this.ProsperityState == 0)
			{
				this.ProsperityState++;
			}
			this._prosperityComparer.SetSortMode(this.ProsperityState == 1);
			this._listToControl.Sort(this._prosperityComparer);
			this.IsProsperitySelected = true;
		}

		// Token: 0x0600081B RID: 2075 RVA: 0x000253FC File Offset: 0x000235FC
		private void ExecuteSortByDefenders()
		{
			int defendersState = this.DefendersState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.DefendersState = (defendersState + 1) % 3;
			if (this.DefendersState == 0)
			{
				int defendersState2 = this.DefendersState;
				this.DefendersState = defendersState2 + 1;
			}
			this._defendersComparer.SetSortMode(this.DefendersState == 1);
			this._listToControl.Sort(this._defendersComparer);
			this.IsDefendersSelected = true;
		}

		// Token: 0x0600081C RID: 2076 RVA: 0x00025468 File Offset: 0x00023668
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.TypeState = (int)state;
			this.NameState = (int)state;
			this.OwnerState = (int)state;
			this.ProsperityState = (int)state;
			this.DefendersState = (int)state;
			this.IsTypeSelected = false;
			this.IsNameSelected = false;
			this.IsProsperitySelected = false;
			this.IsOwnerSelected = false;
			this.IsDefendersSelected = false;
		}

		// Token: 0x1700024A RID: 586
		// (get) Token: 0x0600081D RID: 2077 RVA: 0x000254BB File Offset: 0x000236BB
		// (set) Token: 0x0600081E RID: 2078 RVA: 0x000254C3 File Offset: 0x000236C3
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

		// Token: 0x1700024B RID: 587
		// (get) Token: 0x0600081F RID: 2079 RVA: 0x000254E1 File Offset: 0x000236E1
		// (set) Token: 0x06000820 RID: 2080 RVA: 0x000254E9 File Offset: 0x000236E9
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

		// Token: 0x1700024C RID: 588
		// (get) Token: 0x06000821 RID: 2081 RVA: 0x00025507 File Offset: 0x00023707
		// (set) Token: 0x06000822 RID: 2082 RVA: 0x0002550F File Offset: 0x0002370F
		[DataSourceProperty]
		public int OwnerState
		{
			get
			{
				return this._ownerState;
			}
			set
			{
				if (value != this._ownerState)
				{
					this._ownerState = value;
					base.OnPropertyChangedWithValue(value, "OwnerState");
				}
			}
		}

		// Token: 0x1700024D RID: 589
		// (get) Token: 0x06000823 RID: 2083 RVA: 0x0002552D File Offset: 0x0002372D
		// (set) Token: 0x06000824 RID: 2084 RVA: 0x00025535 File Offset: 0x00023735
		[DataSourceProperty]
		public int ProsperityState
		{
			get
			{
				return this._prosperityState;
			}
			set
			{
				if (value != this._prosperityState)
				{
					this._prosperityState = value;
					base.OnPropertyChangedWithValue(value, "ProsperityState");
				}
			}
		}

		// Token: 0x1700024E RID: 590
		// (get) Token: 0x06000825 RID: 2085 RVA: 0x00025553 File Offset: 0x00023753
		// (set) Token: 0x06000826 RID: 2086 RVA: 0x0002555B File Offset: 0x0002375B
		[DataSourceProperty]
		public int DefendersState
		{
			get
			{
				return this._defendersState;
			}
			set
			{
				if (value != this._defendersState)
				{
					this._defendersState = value;
					base.OnPropertyChangedWithValue(value, "DefendersState");
				}
			}
		}

		// Token: 0x1700024F RID: 591
		// (get) Token: 0x06000827 RID: 2087 RVA: 0x00025579 File Offset: 0x00023779
		// (set) Token: 0x06000828 RID: 2088 RVA: 0x00025581 File Offset: 0x00023781
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

		// Token: 0x17000250 RID: 592
		// (get) Token: 0x06000829 RID: 2089 RVA: 0x0002559F File Offset: 0x0002379F
		// (set) Token: 0x0600082A RID: 2090 RVA: 0x000255A7 File Offset: 0x000237A7
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

		// Token: 0x17000251 RID: 593
		// (get) Token: 0x0600082B RID: 2091 RVA: 0x000255C5 File Offset: 0x000237C5
		// (set) Token: 0x0600082C RID: 2092 RVA: 0x000255CD File Offset: 0x000237CD
		[DataSourceProperty]
		public bool IsDefendersSelected
		{
			get
			{
				return this._isDefendersSelected;
			}
			set
			{
				if (value != this._isDefendersSelected)
				{
					this._isDefendersSelected = value;
					base.OnPropertyChangedWithValue(value, "IsDefendersSelected");
				}
			}
		}

		// Token: 0x17000252 RID: 594
		// (get) Token: 0x0600082D RID: 2093 RVA: 0x000255EB File Offset: 0x000237EB
		// (set) Token: 0x0600082E RID: 2094 RVA: 0x000255F3 File Offset: 0x000237F3
		[DataSourceProperty]
		public bool IsOwnerSelected
		{
			get
			{
				return this._isOwnerSelected;
			}
			set
			{
				if (value != this._isOwnerSelected)
				{
					this._isOwnerSelected = value;
					base.OnPropertyChangedWithValue(value, "IsOwnerSelected");
				}
			}
		}

		// Token: 0x17000253 RID: 595
		// (get) Token: 0x0600082F RID: 2095 RVA: 0x00025611 File Offset: 0x00023811
		// (set) Token: 0x06000830 RID: 2096 RVA: 0x00025619 File Offset: 0x00023819
		[DataSourceProperty]
		public bool IsProsperitySelected
		{
			get
			{
				return this._isProsperitySelected;
			}
			set
			{
				if (value != this._isProsperitySelected)
				{
					this._isProsperitySelected = value;
					base.OnPropertyChangedWithValue(value, "IsProsperitySelected");
				}
			}
		}

		// Token: 0x0400037A RID: 890
		private readonly MBBindingList<KingdomSettlementItemVM> _listToControl;

		// Token: 0x0400037B RID: 891
		private readonly KingdomSettlementSortControllerVM.ItemTypeComparer _typeComparer;

		// Token: 0x0400037C RID: 892
		private readonly KingdomSettlementSortControllerVM.ItemProsperityComparer _prosperityComparer;

		// Token: 0x0400037D RID: 893
		private readonly KingdomSettlementSortControllerVM.ItemDefendersComparer _defendersComparer;

		// Token: 0x0400037E RID: 894
		private readonly KingdomSettlementSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x0400037F RID: 895
		private readonly KingdomSettlementSortControllerVM.ItemOwnerComparer _ownerComparer;

		// Token: 0x04000380 RID: 896
		private int _typeState;

		// Token: 0x04000381 RID: 897
		private int _nameState;

		// Token: 0x04000382 RID: 898
		private int _ownerState;

		// Token: 0x04000383 RID: 899
		private int _prosperityState;

		// Token: 0x04000384 RID: 900
		private int _defendersState;

		// Token: 0x04000385 RID: 901
		private bool _isTypeSelected;

		// Token: 0x04000386 RID: 902
		private bool _isNameSelected;

		// Token: 0x04000387 RID: 903
		private bool _isOwnerSelected;

		// Token: 0x04000388 RID: 904
		private bool _isProsperitySelected;

		// Token: 0x04000389 RID: 905
		private bool _isDefendersSelected;

		// Token: 0x020001C5 RID: 453
		public abstract class ItemComparerBase : IComparer<KingdomSettlementItemVM>
		{
			// Token: 0x0600234C RID: 9036 RVA: 0x0007DEAD File Offset: 0x0007C0AD
			public void SetSortMode(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x0600234D RID: 9037
			public abstract int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y);

			// Token: 0x0600234E RID: 9038 RVA: 0x0007DEB6 File Offset: 0x0007C0B6
			protected int ResolveEquality(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				return x.Settlement.Name.ToString().CompareTo(y.Settlement.Name.ToString());
			}

			// Token: 0x040010F2 RID: 4338
			protected bool _isAscending;
		}

		// Token: 0x020001C6 RID: 454
		public class ItemNameComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002350 RID: 9040 RVA: 0x0007DEE8 File Offset: 0x0007C0E8
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				if (this._isAscending)
				{
					return y.Settlement.Name.ToString().CompareTo(x.Settlement.Name.ToString()) * -1;
				}
				return y.Settlement.Name.ToString().CompareTo(x.Settlement.Name.ToString());
			}
		}

		// Token: 0x020001C7 RID: 455
		public class ItemClanComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002352 RID: 9042 RVA: 0x0007DF54 File Offset: 0x0007C154
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				int num = y.Settlement.OwnerClan.Name.ToString().CompareTo(x.Settlement.OwnerClan.Name.ToString());
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001C8 RID: 456
		public class ItemOwnerComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002354 RID: 9044 RVA: 0x0007DFB4 File Offset: 0x0007C1B4
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				int num = y.Owner.NameText.CompareTo(x.Owner.NameText);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001C9 RID: 457
		public class ItemVillagesComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002356 RID: 9046 RVA: 0x0007E000 File Offset: 0x0007C200
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				int num = y.Villages.Count.CompareTo(x.Villages.Count);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001CA RID: 458
		public class ItemTypeComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002358 RID: 9048 RVA: 0x0007E050 File Offset: 0x0007C250
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				int num = y.Settlement.IsCastle.CompareTo(x.Settlement.IsCastle);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001CB RID: 459
		public class ItemProsperityComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600235A RID: 9050 RVA: 0x0007E0A0 File Offset: 0x0007C2A0
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				int num = y.Prosperity.CompareTo(x.Prosperity);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001CC RID: 460
		public class ItemFoodComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600235C RID: 9052 RVA: 0x0007E0E4 File Offset: 0x0007C2E4
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				float num = ((y.Settlement.Town != null) ? y.Settlement.Town.FoodStocks : 0f);
				float value = ((x.Settlement.Town != null) ? x.Settlement.Town.FoodStocks : 0f);
				int num2 = num.CompareTo(value);
				if (num2 != 0)
				{
					return num2 * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001CD RID: 461
		public class ItemGarrisonComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600235E RID: 9054 RVA: 0x0007E168 File Offset: 0x0007C368
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				int num = y.Garrison.CompareTo(x.Garrison);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001CE RID: 462
		private class ItemDefendersComparer : KingdomSettlementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002360 RID: 9056 RVA: 0x0007E1AC File Offset: 0x0007C3AC
			public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
			{
				int num = y.Defenders.CompareTo(x.Defenders);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}
	}
}
