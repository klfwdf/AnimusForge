using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies
{
	// Token: 0x0200008B RID: 139
	public class KingdomArmySortControllerVM : ViewModel
	{
		// Token: 0x06000BCB RID: 3019 RVA: 0x0003105C File Offset: 0x0002F25C
		public KingdomArmySortControllerVM(ref MBBindingList<KingdomArmyItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._ownerComparer = new KingdomArmySortControllerVM.ItemOwnerComparer();
			this._strengthComparer = new KingdomArmySortControllerVM.ItemStrengthComparer();
			this._nameComparer = new KingdomArmySortControllerVM.ItemNameComparer();
			this._partiesComparer = new KingdomArmySortControllerVM.ItemPartiesComparer();
			this._distanceComparer = new KingdomArmySortControllerVM.ItemDistanceComparer();
		}

		// Token: 0x06000BCC RID: 3020 RVA: 0x000310B0 File Offset: 0x0002F2B0
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

		// Token: 0x06000BCD RID: 3021 RVA: 0x00031118 File Offset: 0x0002F318
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

		// Token: 0x06000BCE RID: 3022 RVA: 0x00031180 File Offset: 0x0002F380
		private void ExecuteSortByStrength()
		{
			int strengthState = this.StrengthState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.StrengthState = (strengthState + 1) % 3;
			if (this.StrengthState == 0)
			{
				this.StrengthState++;
			}
			this._strengthComparer.SetSortMode(this.StrengthState == 1);
			this._listToControl.Sort(this._strengthComparer);
			this.IsStrengthSelected = true;
		}

		// Token: 0x06000BCF RID: 3023 RVA: 0x000311E8 File Offset: 0x0002F3E8
		private void ExecuteSortByParties()
		{
			int partiesState = this.PartiesState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.PartiesState = (partiesState + 1) % 3;
			if (this.PartiesState == 0)
			{
				this.PartiesState++;
			}
			this._partiesComparer.SetSortMode(this.PartiesState == 1);
			this._listToControl.Sort(this._partiesComparer);
			this.IsPartiesSelected = true;
		}

		// Token: 0x06000BD0 RID: 3024 RVA: 0x00031250 File Offset: 0x0002F450
		private void ExecuteSortByDistance()
		{
			int distanceState = this.DistanceState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.DistanceState = (distanceState + 1) % 3;
			if (this.DistanceState == 0)
			{
				this.DistanceState++;
			}
			this._distanceComparer.SetSortMode(this.DistanceState == 1);
			this._listToControl.Sort(this._distanceComparer);
			this.IsDistanceSelected = true;
		}

		// Token: 0x06000BD1 RID: 3025 RVA: 0x000312B8 File Offset: 0x0002F4B8
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.NameState = (int)state;
			this.OwnerState = (int)state;
			this.StrengthState = (int)state;
			this.PartiesState = (int)state;
			this.DistanceState = (int)state;
			this.IsNameSelected = false;
			this.IsOwnerSelected = false;
			this.IsStrengthSelected = false;
			this.IsPartiesSelected = false;
			this.IsDistanceSelected = false;
		}

		// Token: 0x170003C3 RID: 963
		// (get) Token: 0x06000BD2 RID: 3026 RVA: 0x0003130B File Offset: 0x0002F50B
		// (set) Token: 0x06000BD3 RID: 3027 RVA: 0x00031313 File Offset: 0x0002F513
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

		// Token: 0x170003C4 RID: 964
		// (get) Token: 0x06000BD4 RID: 3028 RVA: 0x00031331 File Offset: 0x0002F531
		// (set) Token: 0x06000BD5 RID: 3029 RVA: 0x00031339 File Offset: 0x0002F539
		[DataSourceProperty]
		public int PartiesState
		{
			get
			{
				return this._partiesState;
			}
			set
			{
				if (value != this._partiesState)
				{
					this._partiesState = value;
					base.OnPropertyChangedWithValue(value, "PartiesState");
				}
			}
		}

		// Token: 0x170003C5 RID: 965
		// (get) Token: 0x06000BD6 RID: 3030 RVA: 0x00031357 File Offset: 0x0002F557
		// (set) Token: 0x06000BD7 RID: 3031 RVA: 0x0003135F File Offset: 0x0002F55F
		[DataSourceProperty]
		public int StrengthState
		{
			get
			{
				return this._strengthState;
			}
			set
			{
				if (value != this._strengthState)
				{
					this._strengthState = value;
					base.OnPropertyChangedWithValue(value, "StrengthState");
				}
			}
		}

		// Token: 0x170003C6 RID: 966
		// (get) Token: 0x06000BD8 RID: 3032 RVA: 0x0003137D File Offset: 0x0002F57D
		// (set) Token: 0x06000BD9 RID: 3033 RVA: 0x00031385 File Offset: 0x0002F585
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

		// Token: 0x170003C7 RID: 967
		// (get) Token: 0x06000BDA RID: 3034 RVA: 0x000313A3 File Offset: 0x0002F5A3
		// (set) Token: 0x06000BDB RID: 3035 RVA: 0x000313AB File Offset: 0x0002F5AB
		[DataSourceProperty]
		public int DistanceState
		{
			get
			{
				return this._distanceState;
			}
			set
			{
				if (value != this._distanceState)
				{
					this._distanceState = value;
					base.OnPropertyChangedWithValue(value, "DistanceState");
				}
			}
		}

		// Token: 0x170003C8 RID: 968
		// (get) Token: 0x06000BDC RID: 3036 RVA: 0x000313C9 File Offset: 0x0002F5C9
		// (set) Token: 0x06000BDD RID: 3037 RVA: 0x000313D1 File Offset: 0x0002F5D1
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

		// Token: 0x170003C9 RID: 969
		// (get) Token: 0x06000BDE RID: 3038 RVA: 0x000313EF File Offset: 0x0002F5EF
		// (set) Token: 0x06000BDF RID: 3039 RVA: 0x000313F7 File Offset: 0x0002F5F7
		[DataSourceProperty]
		public bool IsPartiesSelected
		{
			get
			{
				return this._isPartiesSelected;
			}
			set
			{
				if (value != this._isPartiesSelected)
				{
					this._isPartiesSelected = value;
					base.OnPropertyChangedWithValue(value, "IsPartiesSelected");
				}
			}
		}

		// Token: 0x170003CA RID: 970
		// (get) Token: 0x06000BE0 RID: 3040 RVA: 0x00031415 File Offset: 0x0002F615
		// (set) Token: 0x06000BE1 RID: 3041 RVA: 0x0003141D File Offset: 0x0002F61D
		[DataSourceProperty]
		public bool IsStrengthSelected
		{
			get
			{
				return this._isStrengthSelected;
			}
			set
			{
				if (value != this._isStrengthSelected)
				{
					this._isStrengthSelected = value;
					base.OnPropertyChangedWithValue(value, "IsStrengthSelected");
				}
			}
		}

		// Token: 0x170003CB RID: 971
		// (get) Token: 0x06000BE2 RID: 3042 RVA: 0x0003143B File Offset: 0x0002F63B
		// (set) Token: 0x06000BE3 RID: 3043 RVA: 0x00031443 File Offset: 0x0002F643
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

		// Token: 0x170003CC RID: 972
		// (get) Token: 0x06000BE4 RID: 3044 RVA: 0x00031461 File Offset: 0x0002F661
		// (set) Token: 0x06000BE5 RID: 3045 RVA: 0x00031469 File Offset: 0x0002F669
		[DataSourceProperty]
		public bool IsDistanceSelected
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
					base.OnPropertyChangedWithValue(value, "IsDistanceSelected");
				}
			}
		}

		// Token: 0x0400053C RID: 1340
		private readonly MBBindingList<KingdomArmyItemVM> _listToControl;

		// Token: 0x0400053D RID: 1341
		private readonly KingdomArmySortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x0400053E RID: 1342
		private readonly KingdomArmySortControllerVM.ItemOwnerComparer _ownerComparer;

		// Token: 0x0400053F RID: 1343
		private readonly KingdomArmySortControllerVM.ItemStrengthComparer _strengthComparer;

		// Token: 0x04000540 RID: 1344
		private readonly KingdomArmySortControllerVM.ItemPartiesComparer _partiesComparer;

		// Token: 0x04000541 RID: 1345
		private readonly KingdomArmySortControllerVM.ItemDistanceComparer _distanceComparer;

		// Token: 0x04000542 RID: 1346
		private int _nameState;

		// Token: 0x04000543 RID: 1347
		private int _ownerState;

		// Token: 0x04000544 RID: 1348
		private int _strengthState;

		// Token: 0x04000545 RID: 1349
		private int _partiesState;

		// Token: 0x04000546 RID: 1350
		private int _distanceState;

		// Token: 0x04000547 RID: 1351
		private bool _isNameSelected;

		// Token: 0x04000548 RID: 1352
		private bool _isOwnerSelected;

		// Token: 0x04000549 RID: 1353
		private bool _isStrengthSelected;

		// Token: 0x0400054A RID: 1354
		private bool _isPartiesSelected;

		// Token: 0x0400054B RID: 1355
		private bool _isDistanceSelected;

		// Token: 0x020001EE RID: 494
		public abstract class ItemComparerBase : IComparer<KingdomArmyItemVM>
		{
			// Token: 0x060023D0 RID: 9168 RVA: 0x0007E9AE File Offset: 0x0007CBAE
			public void SetSortMode(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x060023D1 RID: 9169
			public abstract int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y);

			// Token: 0x060023D2 RID: 9170 RVA: 0x0007E9B7 File Offset: 0x0007CBB7
			protected int ResolveEquality(KingdomArmyItemVM x, KingdomArmyItemVM y)
			{
				return x.ArmyName.CompareTo(y.ArmyName);
			}

			// Token: 0x0400114B RID: 4427
			protected bool _isAscending;
		}

		// Token: 0x020001EF RID: 495
		public class ItemNameComparer : KingdomArmySortControllerVM.ItemComparerBase
		{
			// Token: 0x060023D4 RID: 9172 RVA: 0x0007E9D2 File Offset: 0x0007CBD2
			public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
			{
				if (this._isAscending)
				{
					return y.ArmyName.CompareTo(x.ArmyName) * -1;
				}
				return y.ArmyName.CompareTo(x.ArmyName);
			}
		}

		// Token: 0x020001F0 RID: 496
		public class ItemOwnerComparer : KingdomArmySortControllerVM.ItemComparerBase
		{
			// Token: 0x060023D6 RID: 9174 RVA: 0x0007EA0C File Offset: 0x0007CC0C
			public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
			{
				int num = y.Leader.NameText.ToString().CompareTo(x.Leader.NameText.ToString());
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001F1 RID: 497
		public class ItemStrengthComparer : KingdomArmySortControllerVM.ItemComparerBase
		{
			// Token: 0x060023D8 RID: 9176 RVA: 0x0007EA64 File Offset: 0x0007CC64
			public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
			{
				int num = y.Strength.CompareTo(x.Strength);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001F2 RID: 498
		public class ItemPartiesComparer : KingdomArmySortControllerVM.ItemComparerBase
		{
			// Token: 0x060023DA RID: 9178 RVA: 0x0007EAA8 File Offset: 0x0007CCA8
			public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
			{
				int num = y.Parties.Count.CompareTo(x.Parties.Count);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020001F3 RID: 499
		public class ItemDistanceComparer : KingdomArmySortControllerVM.ItemComparerBase
		{
			// Token: 0x060023DC RID: 9180 RVA: 0x0007EAF8 File Offset: 0x0007CCF8
			public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
			{
				int num = y.DistanceToMainParty.CompareTo(x.DistanceToMainParty);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}
	}
}
