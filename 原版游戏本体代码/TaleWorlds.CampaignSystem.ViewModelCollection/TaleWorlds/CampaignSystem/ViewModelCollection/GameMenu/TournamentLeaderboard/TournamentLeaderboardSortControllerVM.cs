using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard
{
	// Token: 0x020000AE RID: 174
	public class TournamentLeaderboardSortControllerVM : ViewModel
	{
		// Token: 0x060010BC RID: 4284 RVA: 0x00043868 File Offset: 0x00041A68
		public TournamentLeaderboardSortControllerVM(ref MBBindingList<TournamentLeaderboardEntryItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._prizeComparer = new TournamentLeaderboardSortControllerVM.ItemPrizeComparer();
			this._nameComparer = new TournamentLeaderboardSortControllerVM.ItemNameComparer();
			this._placementComparer = new TournamentLeaderboardSortControllerVM.ItemPlacementComparer();
			this._victoriesComparer = new TournamentLeaderboardSortControllerVM.ItemVictoriesComparer();
		}

		// Token: 0x060010BD RID: 4285 RVA: 0x000438A4 File Offset: 0x00041AA4
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

		// Token: 0x060010BE RID: 4286 RVA: 0x00043910 File Offset: 0x00041B10
		public void ExecuteSortByPrize()
		{
			int prizeState = this.PrizeState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.PrizeState = (prizeState + 1) % 3;
			if (this.PrizeState == 0)
			{
				int prizeState2 = this.PrizeState;
				this.PrizeState = prizeState2 + 1;
			}
			this._prizeComparer.SetSortMode(this.PrizeState == 1);
			this._listToControl.Sort(this._prizeComparer);
			this.IsPrizeSelected = true;
		}

		// Token: 0x060010BF RID: 4287 RVA: 0x0004397C File Offset: 0x00041B7C
		public void ExecuteSortByPlacement()
		{
			int placementState = this.PlacementState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.PlacementState = (placementState + 1) % 3;
			if (this.PlacementState == 0)
			{
				int placementState2 = this.PlacementState;
				this.PlacementState = placementState2 + 1;
			}
			this._placementComparer.SetSortMode(this.PlacementState == 1);
			this._listToControl.Sort(this._placementComparer);
			this.IsPlacementSelected = true;
		}

		// Token: 0x060010C0 RID: 4288 RVA: 0x000439E8 File Offset: 0x00041BE8
		public void ExecuteSortByVictories()
		{
			int victoriesState = this.VictoriesState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.VictoriesState = (victoriesState + 1) % 3;
			if (this.VictoriesState == 0)
			{
				int victoriesState2 = this.VictoriesState;
				this.VictoriesState = victoriesState2 + 1;
			}
			this._victoriesComparer.SetSortMode(this.VictoriesState == 1);
			this._listToControl.Sort(this._victoriesComparer);
			this.IsVictoriesSelected = true;
		}

		// Token: 0x060010C1 RID: 4289 RVA: 0x00043A52 File Offset: 0x00041C52
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.NameState = (int)state;
			this.PrizeState = (int)state;
			this.PlacementState = (int)state;
			this.VictoriesState = (int)state;
			this.IsNameSelected = false;
			this.IsVictoriesSelected = false;
			this.IsPrizeSelected = false;
			this.IsPlacementSelected = false;
		}

		// Token: 0x17000572 RID: 1394
		// (get) Token: 0x060010C2 RID: 4290 RVA: 0x00043A8C File Offset: 0x00041C8C
		// (set) Token: 0x060010C3 RID: 4291 RVA: 0x00043A94 File Offset: 0x00041C94
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

		// Token: 0x17000573 RID: 1395
		// (get) Token: 0x060010C4 RID: 4292 RVA: 0x00043AB2 File Offset: 0x00041CB2
		// (set) Token: 0x060010C5 RID: 4293 RVA: 0x00043ABA File Offset: 0x00041CBA
		[DataSourceProperty]
		public int VictoriesState
		{
			get
			{
				return this._victoriesState;
			}
			set
			{
				if (value != this._victoriesState)
				{
					this._victoriesState = value;
					base.OnPropertyChangedWithValue(value, "VictoriesState");
				}
			}
		}

		// Token: 0x17000574 RID: 1396
		// (get) Token: 0x060010C6 RID: 4294 RVA: 0x00043AD8 File Offset: 0x00041CD8
		// (set) Token: 0x060010C7 RID: 4295 RVA: 0x00043AE0 File Offset: 0x00041CE0
		[DataSourceProperty]
		public int PrizeState
		{
			get
			{
				return this._prizeState;
			}
			set
			{
				if (value != this._prizeState)
				{
					this._prizeState = value;
					base.OnPropertyChangedWithValue(value, "PrizeState");
				}
			}
		}

		// Token: 0x17000575 RID: 1397
		// (get) Token: 0x060010C8 RID: 4296 RVA: 0x00043AFE File Offset: 0x00041CFE
		// (set) Token: 0x060010C9 RID: 4297 RVA: 0x00043B06 File Offset: 0x00041D06
		[DataSourceProperty]
		public int PlacementState
		{
			get
			{
				return this._placementState;
			}
			set
			{
				if (value != this._placementState)
				{
					this._placementState = value;
					base.OnPropertyChangedWithValue(value, "PlacementState");
				}
			}
		}

		// Token: 0x17000576 RID: 1398
		// (get) Token: 0x060010CA RID: 4298 RVA: 0x00043B24 File Offset: 0x00041D24
		// (set) Token: 0x060010CB RID: 4299 RVA: 0x00043B2C File Offset: 0x00041D2C
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

		// Token: 0x17000577 RID: 1399
		// (get) Token: 0x060010CC RID: 4300 RVA: 0x00043B4A File Offset: 0x00041D4A
		// (set) Token: 0x060010CD RID: 4301 RVA: 0x00043B52 File Offset: 0x00041D52
		[DataSourceProperty]
		public bool IsPrizeSelected
		{
			get
			{
				return this._isPrizeSelected;
			}
			set
			{
				if (value != this._isPrizeSelected)
				{
					this._isPrizeSelected = value;
					base.OnPropertyChangedWithValue(value, "IsPrizeSelected");
				}
			}
		}

		// Token: 0x17000578 RID: 1400
		// (get) Token: 0x060010CE RID: 4302 RVA: 0x00043B70 File Offset: 0x00041D70
		// (set) Token: 0x060010CF RID: 4303 RVA: 0x00043B78 File Offset: 0x00041D78
		[DataSourceProperty]
		public bool IsPlacementSelected
		{
			get
			{
				return this._isPlacementSelected;
			}
			set
			{
				if (value != this._isPlacementSelected)
				{
					this._isPlacementSelected = value;
					base.OnPropertyChangedWithValue(value, "IsPlacementSelected");
				}
			}
		}

		// Token: 0x17000579 RID: 1401
		// (get) Token: 0x060010D0 RID: 4304 RVA: 0x00043B96 File Offset: 0x00041D96
		// (set) Token: 0x060010D1 RID: 4305 RVA: 0x00043B9E File Offset: 0x00041D9E
		[DataSourceProperty]
		public bool IsVictoriesSelected
		{
			get
			{
				return this._isVictoriesSelected;
			}
			set
			{
				if (value != this._isVictoriesSelected)
				{
					this._isVictoriesSelected = value;
					base.OnPropertyChangedWithValue(value, "IsVictoriesSelected");
				}
			}
		}

		// Token: 0x040007A2 RID: 1954
		private readonly MBBindingList<TournamentLeaderboardEntryItemVM> _listToControl;

		// Token: 0x040007A3 RID: 1955
		private readonly TournamentLeaderboardSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x040007A4 RID: 1956
		private readonly TournamentLeaderboardSortControllerVM.ItemPrizeComparer _prizeComparer;

		// Token: 0x040007A5 RID: 1957
		private readonly TournamentLeaderboardSortControllerVM.ItemPlacementComparer _placementComparer;

		// Token: 0x040007A6 RID: 1958
		private readonly TournamentLeaderboardSortControllerVM.ItemVictoriesComparer _victoriesComparer;

		// Token: 0x040007A7 RID: 1959
		private int _nameState;

		// Token: 0x040007A8 RID: 1960
		private int _prizeState;

		// Token: 0x040007A9 RID: 1961
		private int _placementState;

		// Token: 0x040007AA RID: 1962
		private int _victoriesState;

		// Token: 0x040007AB RID: 1963
		private bool _isNameSelected;

		// Token: 0x040007AC RID: 1964
		private bool _isPrizeSelected;

		// Token: 0x040007AD RID: 1965
		private bool _isPlacementSelected;

		// Token: 0x040007AE RID: 1966
		private bool _isVictoriesSelected;

		// Token: 0x0200021F RID: 543
		public abstract class ItemComparerBase : IComparer<TournamentLeaderboardEntryItemVM>
		{
			// Token: 0x06002464 RID: 9316 RVA: 0x0007F966 File Offset: 0x0007DB66
			public void SetSortMode(bool isAcending)
			{
				this._isAcending = isAcending;
			}

			// Token: 0x06002465 RID: 9317
			public abstract int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y);

			// Token: 0x040011DD RID: 4573
			protected bool _isAcending;
		}

		// Token: 0x02000220 RID: 544
		public class ItemNameComparer : TournamentLeaderboardSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002467 RID: 9319 RVA: 0x0007F977 File Offset: 0x0007DB77
			public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
			{
				if (this._isAcending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x02000221 RID: 545
		public class ItemPrizeComparer : TournamentLeaderboardSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002469 RID: 9321 RVA: 0x0007F9B0 File Offset: 0x0007DBB0
			public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
			{
				if (this._isAcending)
				{
					return y.PrizeValue.CompareTo(x.PrizeValue) * -1;
				}
				return y.PrizeValue.CompareTo(x.PrizeValue);
			}
		}

		// Token: 0x02000222 RID: 546
		public class ItemPlacementComparer : TournamentLeaderboardSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600246B RID: 9323 RVA: 0x0007F9F8 File Offset: 0x0007DBF8
			public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
			{
				if (this._isAcending)
				{
					return y.PlacementOnLeaderboard.CompareTo(x.PlacementOnLeaderboard) * -1;
				}
				return y.PlacementOnLeaderboard.CompareTo(x.PlacementOnLeaderboard);
			}
		}

		// Token: 0x02000223 RID: 547
		public class ItemVictoriesComparer : TournamentLeaderboardSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600246D RID: 9325 RVA: 0x0007FA40 File Offset: 0x0007DC40
			public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
			{
				if (this._isAcending)
				{
					return y.Victories.CompareTo(x.Victories) * -1;
				}
				return y.Victories.CompareTo(x.Victories);
			}
		}
	}
}
