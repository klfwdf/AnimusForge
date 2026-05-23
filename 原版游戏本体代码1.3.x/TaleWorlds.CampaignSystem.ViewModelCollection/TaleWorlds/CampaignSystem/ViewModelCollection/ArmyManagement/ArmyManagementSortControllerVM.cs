using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement
{
	// Token: 0x0200015D RID: 349
	public class ArmyManagementSortControllerVM : ViewModel
	{
		// Token: 0x0600212F RID: 8495 RVA: 0x00078288 File Offset: 0x00076488
		public ArmyManagementSortControllerVM(MBBindingList<ArmyManagementItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._distanceComparer = new ArmyManagementSortControllerVM.ItemDistanceComparer();
			this._costComparer = new ArmyManagementSortControllerVM.ItemCostComparer();
			this._strengthComparer = new ArmyManagementSortControllerVM.ItemStrengthComparer();
			this._nameComparer = new ArmyManagementSortControllerVM.ItemNameComparer();
			this._clanComparer = new ArmyManagementSortControllerVM.ItemClanComparer();
			this._shipCountComparer = new ArmyManagementSortControllerVM.ItemShipCountComparer();
		}

		// Token: 0x06002130 RID: 8496 RVA: 0x000782E4 File Offset: 0x000764E4
		public void ExecuteSortByDistance()
		{
			int distanceState = this.DistanceState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.DistanceState = (distanceState + 1) % 3;
			if (this.DistanceState == 0)
			{
				int distanceState2 = this.DistanceState;
				this.DistanceState = distanceState2 + 1;
			}
			this._distanceComparer.SetSortMode(this.DistanceState == 1);
			this._listToControl.Sort(this._distanceComparer);
			this.IsDistanceSelected = true;
		}

		// Token: 0x06002131 RID: 8497 RVA: 0x00078350 File Offset: 0x00076550
		public void ExecuteSortByCost()
		{
			int costState = this.CostState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.CostState = (costState + 1) % 3;
			if (this.CostState == 0)
			{
				int costState2 = this.CostState;
				this.CostState = costState2 + 1;
			}
			this._costComparer.SetSortMode(this.CostState == 1);
			this._listToControl.Sort(this._costComparer);
			this.IsCostSelected = true;
		}

		// Token: 0x06002132 RID: 8498 RVA: 0x000783BC File Offset: 0x000765BC
		public void ExecuteSortByStrength()
		{
			int strengthState = this.StrengthState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.StrengthState = (strengthState + 1) % 3;
			if (this.StrengthState == 0)
			{
				int strengthState2 = this.StrengthState;
				this.StrengthState = strengthState2 + 1;
			}
			this._strengthComparer.SetSortMode(this.StrengthState == 1);
			this._listToControl.Sort(this._strengthComparer);
			this.IsStrengthSelected = true;
		}

		// Token: 0x06002133 RID: 8499 RVA: 0x00078428 File Offset: 0x00076628
		public void ExecuteSortByName()
		{
			int nameState = this.NameState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.NameState = (nameState + 1) % 3;
			if (this.NameState == 0)
			{
				int nameState2 = this.NameState;
				this.NameState = nameState2 + 1;
			}
			this._nameComparer.SetSortMode(this.NameState == 1);
			this._listToControl.Sort(this._nameComparer);
			this.IsNameSelected = true;
		}

		// Token: 0x06002134 RID: 8500 RVA: 0x00078494 File Offset: 0x00076694
		public void ExecuteSortByClan()
		{
			int clanState = this.ClanState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.ClanState = (clanState + 1) % 3;
			if (this.ClanState == 0)
			{
				int clanState2 = this.ClanState;
				this.ClanState = clanState2 + 1;
			}
			this._clanComparer.SetSortMode(this.ClanState == 1);
			this._listToControl.Sort(this._clanComparer);
			this.IsClanSelected = true;
		}

		// Token: 0x06002135 RID: 8501 RVA: 0x00078500 File Offset: 0x00076700
		public void ExecuteSortByShipCount()
		{
			int shipCountState = this.ShipCountState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.ShipCountState = (shipCountState + 1) % 3;
			if (this.ShipCountState == 0)
			{
				int shipCountState2 = this.ShipCountState;
				this.ShipCountState = shipCountState2 + 1;
			}
			this._shipCountComparer.SetSortMode(this.ShipCountState == 1);
			this._listToControl.Sort(this._shipCountComparer);
			this.IsShipCountSelected = true;
		}

		// Token: 0x06002136 RID: 8502 RVA: 0x0007856C File Offset: 0x0007676C
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.DistanceState = (int)state;
			this.CostState = (int)state;
			this.StrengthState = (int)state;
			this.NameState = (int)state;
			this.ClanState = (int)state;
			this.ShipCountState = (int)state;
			this.IsDistanceSelected = false;
			this.IsCostSelected = false;
			this.IsNameSelected = false;
			this.IsClanSelected = false;
			this.IsStrengthSelected = false;
			this.IsShipCountSelected = false;
		}

		// Token: 0x17000B50 RID: 2896
		// (get) Token: 0x06002137 RID: 8503 RVA: 0x000785CD File Offset: 0x000767CD
		// (set) Token: 0x06002138 RID: 8504 RVA: 0x000785D5 File Offset: 0x000767D5
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

		// Token: 0x17000B51 RID: 2897
		// (get) Token: 0x06002139 RID: 8505 RVA: 0x000785F3 File Offset: 0x000767F3
		// (set) Token: 0x0600213A RID: 8506 RVA: 0x000785FB File Offset: 0x000767FB
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

		// Token: 0x17000B52 RID: 2898
		// (get) Token: 0x0600213B RID: 8507 RVA: 0x00078619 File Offset: 0x00076819
		// (set) Token: 0x0600213C RID: 8508 RVA: 0x00078621 File Offset: 0x00076821
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

		// Token: 0x17000B53 RID: 2899
		// (get) Token: 0x0600213D RID: 8509 RVA: 0x0007863F File Offset: 0x0007683F
		// (set) Token: 0x0600213E RID: 8510 RVA: 0x00078647 File Offset: 0x00076847
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

		// Token: 0x17000B54 RID: 2900
		// (get) Token: 0x0600213F RID: 8511 RVA: 0x00078665 File Offset: 0x00076865
		// (set) Token: 0x06002140 RID: 8512 RVA: 0x0007866D File Offset: 0x0007686D
		[DataSourceProperty]
		public int ClanState
		{
			get
			{
				return this._clanState;
			}
			set
			{
				if (value != this._clanState)
				{
					this._clanState = value;
					base.OnPropertyChangedWithValue(value, "ClanState");
				}
			}
		}

		// Token: 0x17000B55 RID: 2901
		// (get) Token: 0x06002141 RID: 8513 RVA: 0x0007868B File Offset: 0x0007688B
		// (set) Token: 0x06002142 RID: 8514 RVA: 0x00078693 File Offset: 0x00076893
		[DataSourceProperty]
		public int ShipCountState
		{
			get
			{
				return this._shipCountState;
			}
			set
			{
				if (value != this._shipCountState)
				{
					this._shipCountState = value;
					base.OnPropertyChangedWithValue(value, "ShipCountState");
				}
			}
		}

		// Token: 0x17000B56 RID: 2902
		// (get) Token: 0x06002143 RID: 8515 RVA: 0x000786B1 File Offset: 0x000768B1
		// (set) Token: 0x06002144 RID: 8516 RVA: 0x000786B9 File Offset: 0x000768B9
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

		// Token: 0x17000B57 RID: 2903
		// (get) Token: 0x06002145 RID: 8517 RVA: 0x000786D7 File Offset: 0x000768D7
		// (set) Token: 0x06002146 RID: 8518 RVA: 0x000786DF File Offset: 0x000768DF
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

		// Token: 0x17000B58 RID: 2904
		// (get) Token: 0x06002147 RID: 8519 RVA: 0x000786FD File Offset: 0x000768FD
		// (set) Token: 0x06002148 RID: 8520 RVA: 0x00078705 File Offset: 0x00076905
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

		// Token: 0x17000B59 RID: 2905
		// (get) Token: 0x06002149 RID: 8521 RVA: 0x00078723 File Offset: 0x00076923
		// (set) Token: 0x0600214A RID: 8522 RVA: 0x0007872B File Offset: 0x0007692B
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

		// Token: 0x17000B5A RID: 2906
		// (get) Token: 0x0600214B RID: 8523 RVA: 0x00078749 File Offset: 0x00076949
		// (set) Token: 0x0600214C RID: 8524 RVA: 0x00078751 File Offset: 0x00076951
		[DataSourceProperty]
		public bool IsClanSelected
		{
			get
			{
				return this._isClanSelected;
			}
			set
			{
				if (value != this._isClanSelected)
				{
					this._isClanSelected = value;
					base.OnPropertyChangedWithValue(value, "IsClanSelected");
				}
			}
		}

		// Token: 0x17000B5B RID: 2907
		// (get) Token: 0x0600214D RID: 8525 RVA: 0x0007876F File Offset: 0x0007696F
		// (set) Token: 0x0600214E RID: 8526 RVA: 0x00078777 File Offset: 0x00076977
		[DataSourceProperty]
		public bool IsShipCountSelected
		{
			get
			{
				return this._isShipCountSelected;
			}
			set
			{
				if (value != this._isShipCountSelected)
				{
					this._isShipCountSelected = value;
					base.OnPropertyChangedWithValue(value, "IsShipCountSelected");
				}
			}
		}

		// Token: 0x04000F6E RID: 3950
		private readonly MBBindingList<ArmyManagementItemVM> _listToControl;

		// Token: 0x04000F6F RID: 3951
		private readonly ArmyManagementSortControllerVM.ItemDistanceComparer _distanceComparer;

		// Token: 0x04000F70 RID: 3952
		private readonly ArmyManagementSortControllerVM.ItemCostComparer _costComparer;

		// Token: 0x04000F71 RID: 3953
		private readonly ArmyManagementSortControllerVM.ItemStrengthComparer _strengthComparer;

		// Token: 0x04000F72 RID: 3954
		private readonly ArmyManagementSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x04000F73 RID: 3955
		private readonly ArmyManagementSortControllerVM.ItemClanComparer _clanComparer;

		// Token: 0x04000F74 RID: 3956
		private readonly ArmyManagementSortControllerVM.ItemShipCountComparer _shipCountComparer;

		// Token: 0x04000F75 RID: 3957
		private int _distanceState;

		// Token: 0x04000F76 RID: 3958
		private int _costState;

		// Token: 0x04000F77 RID: 3959
		private int _strengthState;

		// Token: 0x04000F78 RID: 3960
		private int _nameState;

		// Token: 0x04000F79 RID: 3961
		private int _clanState;

		// Token: 0x04000F7A RID: 3962
		private int _shipCountState;

		// Token: 0x04000F7B RID: 3963
		private bool _isNameSelected;

		// Token: 0x04000F7C RID: 3964
		private bool _isCostSelected;

		// Token: 0x04000F7D RID: 3965
		private bool _isStrengthSelected;

		// Token: 0x04000F7E RID: 3966
		private bool _isDistanceSelected;

		// Token: 0x04000F7F RID: 3967
		private bool _isClanSelected;

		// Token: 0x04000F80 RID: 3968
		private bool _isShipCountSelected;

		// Token: 0x020002E7 RID: 743
		public abstract class ItemComparerBase : IComparer<ArmyManagementItemVM>
		{
			// Token: 0x06002711 RID: 10001 RVA: 0x00083DF1 File Offset: 0x00081FF1
			public void SetSortMode(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x06002712 RID: 10002
			public abstract int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y);

			// Token: 0x06002713 RID: 10003 RVA: 0x00083DFA File Offset: 0x00081FFA
			protected int ResolveEquality(ArmyManagementItemVM x, ArmyManagementItemVM y)
			{
				return x.LeaderNameText.CompareTo(y.LeaderNameText);
			}

			// Token: 0x040013D9 RID: 5081
			protected bool _isAscending;
		}

		// Token: 0x020002E8 RID: 744
		public class ItemDistanceComparer : ArmyManagementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002715 RID: 10005 RVA: 0x00083E18 File Offset: 0x00082018
			public override int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y)
			{
				int num = y.DistInTime.CompareTo(x.DistInTime);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020002E9 RID: 745
		public class ItemCostComparer : ArmyManagementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002717 RID: 10007 RVA: 0x00083E5C File Offset: 0x0008205C
			public override int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y)
			{
				int num = y.Cost.CompareTo(x.Cost);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020002EA RID: 746
		public class ItemStrengthComparer : ArmyManagementSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002719 RID: 10009 RVA: 0x00083EA0 File Offset: 0x000820A0
			public override int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y)
			{
				int num = y.Strength.CompareTo(x.Strength);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				int num2 = y.ShipCount.CompareTo(x.ShipCount);
				if (num2 != 0)
				{
					return num2 * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020002EB RID: 747
		public class ItemNameComparer : ArmyManagementSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600271B RID: 10011 RVA: 0x00083F0B File Offset: 0x0008210B
			public override int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y)
			{
				if (this._isAscending)
				{
					return y.LeaderNameText.CompareTo(x.LeaderNameText) * -1;
				}
				return y.LeaderNameText.CompareTo(x.LeaderNameText);
			}
		}

		// Token: 0x020002EC RID: 748
		public class ItemClanComparer : ArmyManagementSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600271D RID: 10013 RVA: 0x00083F44 File Offset: 0x00082144
			public override int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y)
			{
				int num = y.Clan.Name.ToString().CompareTo(x.Clan.Name.ToString());
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}

		// Token: 0x020002ED RID: 749
		public class ItemShipCountComparer : ArmyManagementSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600271F RID: 10015 RVA: 0x00083F9C File Offset: 0x0008219C
			public override int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y)
			{
				int num = y.ShipCount.CompareTo(x.ShipCount);
				if (num != 0)
				{
					return num * (this._isAscending ? (-1) : 1);
				}
				return base.ResolveEquality(x, y);
			}
		}
	}
}
