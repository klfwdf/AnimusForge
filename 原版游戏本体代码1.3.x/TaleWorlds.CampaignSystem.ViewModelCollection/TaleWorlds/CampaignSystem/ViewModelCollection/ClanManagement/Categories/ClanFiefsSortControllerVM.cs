using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x02000138 RID: 312
	public class ClanFiefsSortControllerVM : ViewModel
	{
		// Token: 0x06001CE7 RID: 7399 RVA: 0x0006AC65 File Offset: 0x00068E65
		public ClanFiefsSortControllerVM(List<MBBindingList<ClanSettlementItemVM>> listsToControl)
		{
			this._listsToControl = listsToControl;
			this._nameComparer = new ClanFiefsSortControllerVM.ItemNameComparer();
			this._governorComparer = new ClanFiefsSortControllerVM.ItemGovernorComparer();
			this._profitComparer = new ClanFiefsSortControllerVM.ItemProfitComparer();
		}

		// Token: 0x06001CE8 RID: 7400 RVA: 0x0006AC98 File Offset: 0x00068E98
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.GovernorText = GameTexts.FindText("str_notable_governor", null).ToString();
			this.ProfitText = GameTexts.FindText("str_profit", null).ToString();
		}

		// Token: 0x06001CE9 RID: 7401 RVA: 0x0006ACF0 File Offset: 0x00068EF0
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
			foreach (MBBindingList<ClanSettlementItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._nameComparer);
			}
			this.IsNameSelected = true;
		}

		// Token: 0x06001CEA RID: 7402 RVA: 0x0006AD94 File Offset: 0x00068F94
		public void ExecuteSortByGovernor()
		{
			int governorState = this.GovernorState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.GovernorState = (governorState + 1) % 3;
			if (this.GovernorState == 0)
			{
				int governorState2 = this.GovernorState;
				this.GovernorState = governorState2 + 1;
			}
			this._governorComparer.SetSortMode(this.GovernorState == 1);
			foreach (MBBindingList<ClanSettlementItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._governorComparer);
			}
			this.IsGovernorSelected = true;
		}

		// Token: 0x06001CEB RID: 7403 RVA: 0x0006AE38 File Offset: 0x00069038
		public void ExecuteSortByProfit()
		{
			int profitState = this.ProfitState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.ProfitState = (profitState + 1) % 3;
			if (this.ProfitState == 0)
			{
				int profitState2 = this.ProfitState;
				this.ProfitState = profitState2 + 1;
			}
			this._profitComparer.SetSortMode(this.ProfitState == 1);
			foreach (MBBindingList<ClanSettlementItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._profitComparer);
			}
			this.IsProfitSelected = true;
		}

		// Token: 0x06001CEC RID: 7404 RVA: 0x0006AEDC File Offset: 0x000690DC
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.NameState = (int)state;
			this.GovernorState = (int)state;
			this.ProfitState = (int)state;
			this.IsNameSelected = false;
			this.IsGovernorSelected = false;
			this.IsProfitSelected = false;
		}

		// Token: 0x06001CED RID: 7405 RVA: 0x0006AF08 File Offset: 0x00069108
		public void ResetAllStates()
		{
			this.SetAllStates(CampaignUIHelper.SortState.Default);
		}

		// Token: 0x170009D2 RID: 2514
		// (get) Token: 0x06001CEE RID: 7406 RVA: 0x0006AF11 File Offset: 0x00069111
		// (set) Token: 0x06001CEF RID: 7407 RVA: 0x0006AF19 File Offset: 0x00069119
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

		// Token: 0x170009D3 RID: 2515
		// (get) Token: 0x06001CF0 RID: 7408 RVA: 0x0006AF37 File Offset: 0x00069137
		// (set) Token: 0x06001CF1 RID: 7409 RVA: 0x0006AF3F File Offset: 0x0006913F
		[DataSourceProperty]
		public int GovernorState
		{
			get
			{
				return this._governorState;
			}
			set
			{
				if (value != this._governorState)
				{
					this._governorState = value;
					base.OnPropertyChangedWithValue(value, "GovernorState");
				}
			}
		}

		// Token: 0x170009D4 RID: 2516
		// (get) Token: 0x06001CF2 RID: 7410 RVA: 0x0006AF5D File Offset: 0x0006915D
		// (set) Token: 0x06001CF3 RID: 7411 RVA: 0x0006AF65 File Offset: 0x00069165
		[DataSourceProperty]
		public int ProfitState
		{
			get
			{
				return this._profitState;
			}
			set
			{
				if (value != this._profitState)
				{
					this._profitState = value;
					base.OnPropertyChangedWithValue(value, "ProfitState");
				}
			}
		}

		// Token: 0x170009D5 RID: 2517
		// (get) Token: 0x06001CF4 RID: 7412 RVA: 0x0006AF83 File Offset: 0x00069183
		// (set) Token: 0x06001CF5 RID: 7413 RVA: 0x0006AF8B File Offset: 0x0006918B
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

		// Token: 0x170009D6 RID: 2518
		// (get) Token: 0x06001CF6 RID: 7414 RVA: 0x0006AFA9 File Offset: 0x000691A9
		// (set) Token: 0x06001CF7 RID: 7415 RVA: 0x0006AFB1 File Offset: 0x000691B1
		[DataSourceProperty]
		public bool IsGovernorSelected
		{
			get
			{
				return this._isGovernorSelected;
			}
			set
			{
				if (value != this._isGovernorSelected)
				{
					this._isGovernorSelected = value;
					base.OnPropertyChangedWithValue(value, "IsGovernorSelected");
				}
			}
		}

		// Token: 0x170009D7 RID: 2519
		// (get) Token: 0x06001CF8 RID: 7416 RVA: 0x0006AFCF File Offset: 0x000691CF
		// (set) Token: 0x06001CF9 RID: 7417 RVA: 0x0006AFD7 File Offset: 0x000691D7
		[DataSourceProperty]
		public bool IsProfitSelected
		{
			get
			{
				return this._isProfitSelected;
			}
			set
			{
				if (value != this._isProfitSelected)
				{
					this._isProfitSelected = value;
					base.OnPropertyChangedWithValue(value, "IsProfitSelected");
				}
			}
		}

		// Token: 0x170009D8 RID: 2520
		// (get) Token: 0x06001CFA RID: 7418 RVA: 0x0006AFF5 File Offset: 0x000691F5
		// (set) Token: 0x06001CFB RID: 7419 RVA: 0x0006AFFD File Offset: 0x000691FD
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x170009D9 RID: 2521
		// (get) Token: 0x06001CFC RID: 7420 RVA: 0x0006B020 File Offset: 0x00069220
		// (set) Token: 0x06001CFD RID: 7421 RVA: 0x0006B028 File Offset: 0x00069228
		[DataSourceProperty]
		public string GovernorText
		{
			get
			{
				return this._governorText;
			}
			set
			{
				if (value != this._governorText)
				{
					this._governorText = value;
					base.OnPropertyChangedWithValue<string>(value, "GovernorText");
				}
			}
		}

		// Token: 0x170009DA RID: 2522
		// (get) Token: 0x06001CFE RID: 7422 RVA: 0x0006B04B File Offset: 0x0006924B
		// (set) Token: 0x06001CFF RID: 7423 RVA: 0x0006B053 File Offset: 0x00069253
		[DataSourceProperty]
		public string ProfitText
		{
			get
			{
				return this._profitText;
			}
			set
			{
				if (value != this._profitText)
				{
					this._profitText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProfitText");
				}
			}
		}

		// Token: 0x04000D7D RID: 3453
		private readonly List<MBBindingList<ClanSettlementItemVM>> _listsToControl;

		// Token: 0x04000D7E RID: 3454
		private readonly ClanFiefsSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x04000D7F RID: 3455
		private readonly ClanFiefsSortControllerVM.ItemGovernorComparer _governorComparer;

		// Token: 0x04000D80 RID: 3456
		private readonly ClanFiefsSortControllerVM.ItemProfitComparer _profitComparer;

		// Token: 0x04000D81 RID: 3457
		private int _nameState;

		// Token: 0x04000D82 RID: 3458
		private int _governorState;

		// Token: 0x04000D83 RID: 3459
		private int _profitState;

		// Token: 0x04000D84 RID: 3460
		private bool _isNameSelected;

		// Token: 0x04000D85 RID: 3461
		private bool _isGovernorSelected;

		// Token: 0x04000D86 RID: 3462
		private bool _isProfitSelected;

		// Token: 0x04000D87 RID: 3463
		private string _nameText;

		// Token: 0x04000D88 RID: 3464
		private string _governorText;

		// Token: 0x04000D89 RID: 3465
		private string _profitText;

		// Token: 0x0200029A RID: 666
		public abstract class ItemComparerBase : IComparer<ClanSettlementItemVM>
		{
			// Token: 0x060025F1 RID: 9713 RVA: 0x00081ACB File Offset: 0x0007FCCB
			public void SetSortMode(bool isAcending)
			{
				this._isAcending = isAcending;
			}

			// Token: 0x060025F2 RID: 9714
			public abstract int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y);

			// Token: 0x0400130A RID: 4874
			protected bool _isAcending;
		}

		// Token: 0x0200029B RID: 667
		public class ItemNameComparer : ClanFiefsSortControllerVM.ItemComparerBase
		{
			// Token: 0x060025F4 RID: 9716 RVA: 0x00081ADC File Offset: 0x0007FCDC
			public override int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y)
			{
				if (this._isAcending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x0200029C RID: 668
		public class ItemGovernorComparer : ClanFiefsSortControllerVM.ItemComparerBase
		{
			// Token: 0x060025F6 RID: 9718 RVA: 0x00081B14 File Offset: 0x0007FD14
			public override int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y)
			{
				if (this._isAcending)
				{
					if (y.HasGovernor && x.HasGovernor)
					{
						return y.Governor.NameText.CompareTo(x.Governor.NameText) * -1;
					}
					if (y.HasGovernor)
					{
						return 1;
					}
					if (x.HasGovernor)
					{
						return -1;
					}
					return 0;
				}
				else
				{
					if (y.HasGovernor && x.HasGovernor)
					{
						return y.Governor.NameText.CompareTo(x.Governor.NameText);
					}
					if (y.HasGovernor)
					{
						return 1;
					}
					if (x.HasGovernor)
					{
						return -1;
					}
					return 0;
				}
			}
		}

		// Token: 0x0200029D RID: 669
		public class ItemProfitComparer : ClanFiefsSortControllerVM.ItemComparerBase
		{
			// Token: 0x060025F8 RID: 9720 RVA: 0x00081BB8 File Offset: 0x0007FDB8
			public override int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y)
			{
				if (this._isAcending)
				{
					return y.TotalProfit.Value.CompareTo(x.TotalProfit.Value) * -1;
				}
				return y.TotalProfit.Value.CompareTo(x.TotalProfit.Value);
			}
		}
	}
}
