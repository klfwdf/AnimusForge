using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans
{
	// Token: 0x02000087 RID: 135
	public class KingdomClanSortControllerVM : ViewModel
	{
		// Token: 0x06000B4E RID: 2894 RVA: 0x0002FA98 File Offset: 0x0002DC98
		public KingdomClanSortControllerVM(ref MBBindingList<KingdomClanItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._influenceComparer = new KingdomClanSortControllerVM.ItemInfluenceComparer();
			this._membersComparer = new KingdomClanSortControllerVM.ItemMembersComparer();
			this._nameComparer = new KingdomClanSortControllerVM.ItemNameComparer();
			this._fiefsComparer = new KingdomClanSortControllerVM.ItemFiefsComparer();
			this._typeComparer = new KingdomClanSortControllerVM.ItemTypeComparer();
		}

		// Token: 0x06000B4F RID: 2895 RVA: 0x0002FAEC File Offset: 0x0002DCEC
		public void SortByCurrentState()
		{
			if (this.IsNameSelected)
			{
				this._listToControl.Sort(this._nameComparer);
				return;
			}
			if (this.IsTypeSelected)
			{
				this._listToControl.Sort(this._typeComparer);
				return;
			}
			if (this.IsInfluenceSelected)
			{
				this._listToControl.Sort(this._influenceComparer);
				return;
			}
			if (this.IsMembersSelected)
			{
				this._listToControl.Sort(this._membersComparer);
				return;
			}
			if (this.IsFiefsSelected)
			{
				this._listToControl.Sort(this._fiefsComparer);
			}
		}

		// Token: 0x06000B50 RID: 2896 RVA: 0x0002FB7C File Offset: 0x0002DD7C
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

		// Token: 0x06000B51 RID: 2897 RVA: 0x0002FBE4 File Offset: 0x0002DDE4
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

		// Token: 0x06000B52 RID: 2898 RVA: 0x0002FC4C File Offset: 0x0002DE4C
		private void ExecuteSortByInfluence()
		{
			int influenceState = this.InfluenceState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.InfluenceState = (influenceState + 1) % 3;
			if (this.InfluenceState == 0)
			{
				this.InfluenceState++;
			}
			this._influenceComparer.SetSortMode(this.InfluenceState == 1);
			this._listToControl.Sort(this._influenceComparer);
			this.IsInfluenceSelected = true;
		}

		// Token: 0x06000B53 RID: 2899 RVA: 0x0002FCB4 File Offset: 0x0002DEB4
		private void ExecuteSortByMembers()
		{
			int membersState = this.MembersState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.MembersState = (membersState + 1) % 3;
			if (this.MembersState == 0)
			{
				this.MembersState++;
			}
			this._membersComparer.SetSortMode(this.MembersState == 1);
			this._listToControl.Sort(this._membersComparer);
			this.IsMembersSelected = true;
		}

		// Token: 0x06000B54 RID: 2900 RVA: 0x0002FD1C File Offset: 0x0002DF1C
		private void ExecuteSortByFiefs()
		{
			int fiefsState = this.FiefsState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.FiefsState = (fiefsState + 1) % 3;
			if (this.FiefsState == 0)
			{
				this.FiefsState++;
			}
			this._fiefsComparer.SetSortMode(this.FiefsState == 1);
			this._listToControl.Sort(this._fiefsComparer);
			this.IsFiefsSelected = true;
		}

		// Token: 0x06000B55 RID: 2901 RVA: 0x0002FD84 File Offset: 0x0002DF84
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.InfluenceState = (int)state;
			this.FiefsState = (int)state;
			this.MembersState = (int)state;
			this.NameState = (int)state;
			this.TypeState = (int)state;
			this.IsInfluenceSelected = false;
			this.IsFiefsSelected = false;
			this.IsNameSelected = false;
			this.IsMembersSelected = false;
			this.IsTypeSelected = false;
		}

		// Token: 0x17000396 RID: 918
		// (get) Token: 0x06000B56 RID: 2902 RVA: 0x0002FDD7 File Offset: 0x0002DFD7
		// (set) Token: 0x06000B57 RID: 2903 RVA: 0x0002FDDF File Offset: 0x0002DFDF
		[DataSourceProperty]
		public int InfluenceState
		{
			get
			{
				return this._influenceState;
			}
			set
			{
				if (value != this._influenceState)
				{
					this._influenceState = value;
					base.OnPropertyChangedWithValue(value, "InfluenceState");
				}
			}
		}

		// Token: 0x17000397 RID: 919
		// (get) Token: 0x06000B58 RID: 2904 RVA: 0x0002FDFD File Offset: 0x0002DFFD
		// (set) Token: 0x06000B59 RID: 2905 RVA: 0x0002FE05 File Offset: 0x0002E005
		[DataSourceProperty]
		public int FiefsState
		{
			get
			{
				return this._fiefsState;
			}
			set
			{
				if (value != this._fiefsState)
				{
					this._fiefsState = value;
					base.OnPropertyChangedWithValue(value, "FiefsState");
				}
			}
		}

		// Token: 0x17000398 RID: 920
		// (get) Token: 0x06000B5A RID: 2906 RVA: 0x0002FE23 File Offset: 0x0002E023
		// (set) Token: 0x06000B5B RID: 2907 RVA: 0x0002FE2B File Offset: 0x0002E02B
		[DataSourceProperty]
		public int MembersState
		{
			get
			{
				return this._membersState;
			}
			set
			{
				if (value != this._membersState)
				{
					this._membersState = value;
					base.OnPropertyChangedWithValue(value, "MembersState");
				}
			}
		}

		// Token: 0x17000399 RID: 921
		// (get) Token: 0x06000B5C RID: 2908 RVA: 0x0002FE49 File Offset: 0x0002E049
		// (set) Token: 0x06000B5D RID: 2909 RVA: 0x0002FE51 File Offset: 0x0002E051
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

		// Token: 0x1700039A RID: 922
		// (get) Token: 0x06000B5E RID: 2910 RVA: 0x0002FE6F File Offset: 0x0002E06F
		// (set) Token: 0x06000B5F RID: 2911 RVA: 0x0002FE77 File Offset: 0x0002E077
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

		// Token: 0x1700039B RID: 923
		// (get) Token: 0x06000B60 RID: 2912 RVA: 0x0002FE95 File Offset: 0x0002E095
		// (set) Token: 0x06000B61 RID: 2913 RVA: 0x0002FE9D File Offset: 0x0002E09D
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

		// Token: 0x1700039C RID: 924
		// (get) Token: 0x06000B62 RID: 2914 RVA: 0x0002FEBB File Offset: 0x0002E0BB
		// (set) Token: 0x06000B63 RID: 2915 RVA: 0x0002FEC3 File Offset: 0x0002E0C3
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

		// Token: 0x1700039D RID: 925
		// (get) Token: 0x06000B64 RID: 2916 RVA: 0x0002FEE1 File Offset: 0x0002E0E1
		// (set) Token: 0x06000B65 RID: 2917 RVA: 0x0002FEE9 File Offset: 0x0002E0E9
		[DataSourceProperty]
		public bool IsFiefsSelected
		{
			get
			{
				return this._isFiefsSelected;
			}
			set
			{
				if (value != this._isFiefsSelected)
				{
					this._isFiefsSelected = value;
					base.OnPropertyChangedWithValue(value, "IsFiefsSelected");
				}
			}
		}

		// Token: 0x1700039E RID: 926
		// (get) Token: 0x06000B66 RID: 2918 RVA: 0x0002FF07 File Offset: 0x0002E107
		// (set) Token: 0x06000B67 RID: 2919 RVA: 0x0002FF0F File Offset: 0x0002E10F
		[DataSourceProperty]
		public bool IsMembersSelected
		{
			get
			{
				return this._isMembersSelected;
			}
			set
			{
				if (value != this._isMembersSelected)
				{
					this._isMembersSelected = value;
					base.OnPropertyChangedWithValue(value, "IsMembersSelected");
				}
			}
		}

		// Token: 0x1700039F RID: 927
		// (get) Token: 0x06000B68 RID: 2920 RVA: 0x0002FF2D File Offset: 0x0002E12D
		// (set) Token: 0x06000B69 RID: 2921 RVA: 0x0002FF35 File Offset: 0x0002E135
		[DataSourceProperty]
		public bool IsInfluenceSelected
		{
			get
			{
				return this._isDistanceSelected;
			}
			set
			{
				if (value != this._isDistanceSelected)
				{
					this._isDistanceSelected = value;
					base.OnPropertyChangedWithValue(value, "IsInfluenceSelected");
				}
			}
		}

		// Token: 0x04000503 RID: 1283
		private readonly MBBindingList<KingdomClanItemVM> _listToControl;

		// Token: 0x04000504 RID: 1284
		private readonly KingdomClanSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x04000505 RID: 1285
		private readonly KingdomClanSortControllerVM.ItemTypeComparer _typeComparer;

		// Token: 0x04000506 RID: 1286
		private readonly KingdomClanSortControllerVM.ItemInfluenceComparer _influenceComparer;

		// Token: 0x04000507 RID: 1287
		private readonly KingdomClanSortControllerVM.ItemMembersComparer _membersComparer;

		// Token: 0x04000508 RID: 1288
		private readonly KingdomClanSortControllerVM.ItemFiefsComparer _fiefsComparer;

		// Token: 0x04000509 RID: 1289
		private int _influenceState;

		// Token: 0x0400050A RID: 1290
		private int _fiefsState;

		// Token: 0x0400050B RID: 1291
		private int _membersState;

		// Token: 0x0400050C RID: 1292
		private int _nameState;

		// Token: 0x0400050D RID: 1293
		private int _typeState;

		// Token: 0x0400050E RID: 1294
		private bool _isNameSelected;

		// Token: 0x0400050F RID: 1295
		private bool _isTypeSelected;

		// Token: 0x04000510 RID: 1296
		private bool _isFiefsSelected;

		// Token: 0x04000511 RID: 1297
		private bool _isMembersSelected;

		// Token: 0x04000512 RID: 1298
		private bool _isDistanceSelected;

		// Token: 0x020001E7 RID: 487
		public abstract class ItemComparerBase : IComparer<KingdomClanItemVM>
		{
			// Token: 0x060023BD RID: 9149 RVA: 0x0007E7A8 File Offset: 0x0007C9A8
			public void SetSortMode(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x060023BE RID: 9150
			public abstract int Compare(KingdomClanItemVM x, KingdomClanItemVM y);

			// Token: 0x060023BF RID: 9151 RVA: 0x0007E7B1 File Offset: 0x0007C9B1
			protected int ResolveEquality(KingdomClanItemVM x, KingdomClanItemVM y)
			{
				return x.Clan.Name.ToString().CompareTo(y.Clan.Name.ToString());
			}

			// Token: 0x04001146 RID: 4422
			protected bool _isAscending;
		}

		// Token: 0x020001E8 RID: 488
		public class ItemNameComparer : KingdomClanSortControllerVM.ItemComparerBase
		{
			// Token: 0x060023C1 RID: 9153 RVA: 0x0007E7E0 File Offset: 0x0007C9E0
			public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
			{
				if (this._isAscending)
				{
					return y.Clan.Name.ToString().CompareTo(x.Clan.Name.ToString()) * -1;
				}
				return y.Clan.Name.ToString().CompareTo(x.Clan.Name.ToString());
			}
		}

		// Token: 0x020001E9 RID: 489
		public class ItemTypeComparer : KingdomClanSortControllerVM.ItemComparerBase
		{
			// Token: 0x060023C3 RID: 9155 RVA: 0x0007E84C File Offset: 0x0007CA4C
			public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
			{
				int num = y.ClanType.CompareTo(x.ClanType);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001EA RID: 490
		public class ItemInfluenceComparer : KingdomClanSortControllerVM.ItemComparerBase
		{
			// Token: 0x060023C5 RID: 9157 RVA: 0x0007E890 File Offset: 0x0007CA90
			public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
			{
				int num = y.Influence.CompareTo(x.Influence);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001EB RID: 491
		public class ItemMembersComparer : KingdomClanSortControllerVM.ItemComparerBase
		{
			// Token: 0x060023C7 RID: 9159 RVA: 0x0007E8D4 File Offset: 0x0007CAD4
			public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
			{
				int num = y.Members.Count.CompareTo(x.Members.Count);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001EC RID: 492
		public class ItemFiefsComparer : KingdomClanSortControllerVM.ItemComparerBase
		{
			// Token: 0x060023C9 RID: 9161 RVA: 0x0007E924 File Offset: 0x0007CB24
			public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
			{
				int num = y.Fiefs.Count.CompareTo(x.Fiefs.Count);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}
	}
}
