using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x0200013A RID: 314
	public class ClanIncomeSortControllerVM : ViewModel
	{
		// Token: 0x06001D35 RID: 7477 RVA: 0x0006BCD4 File Offset: 0x00069ED4
		public ClanIncomeSortControllerVM(MBBindingList<ClanFinanceWorkshopItemVM> workshopList, MBBindingList<ClanSupporterGroupVM> supporterList, MBBindingList<ClanFinanceAlleyItemVM> alleyList)
		{
			this._workshopList = workshopList;
			this._supporterList = supporterList;
			this._alleyList = alleyList;
			this._workshopNameComparer = new ClanIncomeSortControllerVM.WorkshopItemNameComparer();
			this._supporterNameComparer = new ClanIncomeSortControllerVM.SupporterItemNameComparer();
			this._alleyNameComparer = new ClanIncomeSortControllerVM.AlleyItemNameComparer();
			this._workshopLocationComparer = new ClanIncomeSortControllerVM.WorkshopItemLocationComparer();
			this._alleyLocationComparer = new ClanIncomeSortControllerVM.AlleyItemLocationComparer();
			this._workshopIncomeComparer = new ClanIncomeSortControllerVM.WorkshopItemIncomeComparer();
			this._supporterIncomeComparer = new ClanIncomeSortControllerVM.SupporterItemIncomeComparer();
			this._alleyIncomeComparer = new ClanIncomeSortControllerVM.AlleyItemIncomeComparer();
		}

		// Token: 0x06001D36 RID: 7478 RVA: 0x0006BD54 File Offset: 0x00069F54
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.LocationText = GameTexts.FindText("str_tooltip_label_location", null).ToString();
			this.IncomeText = GameTexts.FindText("str_income", null).ToString();
		}

		// Token: 0x06001D37 RID: 7479 RVA: 0x0006BDAC File Offset: 0x00069FAC
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
			this._workshopNameComparer.SetSortMode(this.NameState == 1);
			this._supporterNameComparer.SetSortMode(this.NameState == 1);
			this._alleyNameComparer.SetSortMode(this.NameState == 1);
			this._workshopList.Sort(this._workshopNameComparer);
			this._supporterList.Sort(this._supporterNameComparer);
			this._alleyList.Sort(this._alleyNameComparer);
			this.IsNameSelected = true;
		}

		// Token: 0x06001D38 RID: 7480 RVA: 0x0006BE60 File Offset: 0x0006A060
		public void ExecuteSortByLocation()
		{
			int locationState = this.LocationState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.LocationState = (locationState + 1) % 3;
			if (this.LocationState == 0)
			{
				int locationState2 = this.LocationState;
				this.LocationState = locationState2 + 1;
			}
			this._workshopLocationComparer.SetSortMode(this.LocationState == 1);
			this._alleyLocationComparer.SetSortMode(this.LocationState == 1);
			this._workshopList.Sort(this._workshopLocationComparer);
			this._alleyList.Sort(this._alleyLocationComparer);
			this.IsLocationSelected = true;
		}

		// Token: 0x06001D39 RID: 7481 RVA: 0x0006BEF0 File Offset: 0x0006A0F0
		public void ExecuteSortByIncome()
		{
			int incomeState = this.IncomeState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.IncomeState = (incomeState + 1) % 3;
			if (this.IncomeState == 0)
			{
				int incomeState2 = this.IncomeState;
				this.IncomeState = incomeState2 + 1;
			}
			this._workshopIncomeComparer.SetSortMode(this.IncomeState == 1);
			this._supporterIncomeComparer.SetSortMode(this.IncomeState == 1);
			this._alleyIncomeComparer.SetSortMode(this.IncomeState == 1);
			this._workshopList.Sort(this._workshopIncomeComparer);
			this._supporterList.Sort(this._supporterIncomeComparer);
			this._alleyList.Sort(this._alleyIncomeComparer);
			this.IsIncomeSelected = true;
		}

		// Token: 0x06001D3A RID: 7482 RVA: 0x0006BFA4 File Offset: 0x0006A1A4
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.NameState = (int)state;
			this.LocationState = (int)state;
			this.IncomeState = (int)state;
			this.IsNameSelected = false;
			this.IsLocationSelected = false;
			this.IsIncomeSelected = false;
		}

		// Token: 0x06001D3B RID: 7483 RVA: 0x0006BFD0 File Offset: 0x0006A1D0
		public void ResetAllStates()
		{
			this.SetAllStates(CampaignUIHelper.SortState.Default);
		}

		// Token: 0x170009EC RID: 2540
		// (get) Token: 0x06001D3C RID: 7484 RVA: 0x0006BFD9 File Offset: 0x0006A1D9
		// (set) Token: 0x06001D3D RID: 7485 RVA: 0x0006BFE1 File Offset: 0x0006A1E1
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

		// Token: 0x170009ED RID: 2541
		// (get) Token: 0x06001D3E RID: 7486 RVA: 0x0006BFFF File Offset: 0x0006A1FF
		// (set) Token: 0x06001D3F RID: 7487 RVA: 0x0006C007 File Offset: 0x0006A207
		[DataSourceProperty]
		public int LocationState
		{
			get
			{
				return this._locationState;
			}
			set
			{
				if (value != this._locationState)
				{
					this._locationState = value;
					base.OnPropertyChangedWithValue(value, "LocationState");
				}
			}
		}

		// Token: 0x170009EE RID: 2542
		// (get) Token: 0x06001D40 RID: 7488 RVA: 0x0006C025 File Offset: 0x0006A225
		// (set) Token: 0x06001D41 RID: 7489 RVA: 0x0006C02D File Offset: 0x0006A22D
		[DataSourceProperty]
		public int IncomeState
		{
			get
			{
				return this._incomeState;
			}
			set
			{
				if (value != this._incomeState)
				{
					this._incomeState = value;
					base.OnPropertyChangedWithValue(value, "IncomeState");
				}
			}
		}

		// Token: 0x170009EF RID: 2543
		// (get) Token: 0x06001D42 RID: 7490 RVA: 0x0006C04B File Offset: 0x0006A24B
		// (set) Token: 0x06001D43 RID: 7491 RVA: 0x0006C053 File Offset: 0x0006A253
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

		// Token: 0x170009F0 RID: 2544
		// (get) Token: 0x06001D44 RID: 7492 RVA: 0x0006C071 File Offset: 0x0006A271
		// (set) Token: 0x06001D45 RID: 7493 RVA: 0x0006C079 File Offset: 0x0006A279
		[DataSourceProperty]
		public bool IsLocationSelected
		{
			get
			{
				return this._isLocationSelected;
			}
			set
			{
				if (value != this._isLocationSelected)
				{
					this._isLocationSelected = value;
					base.OnPropertyChangedWithValue(value, "IsLocationSelected");
				}
			}
		}

		// Token: 0x170009F1 RID: 2545
		// (get) Token: 0x06001D46 RID: 7494 RVA: 0x0006C097 File Offset: 0x0006A297
		// (set) Token: 0x06001D47 RID: 7495 RVA: 0x0006C09F File Offset: 0x0006A29F
		[DataSourceProperty]
		public bool IsIncomeSelected
		{
			get
			{
				return this._isIncomeSelected;
			}
			set
			{
				if (value != this._isIncomeSelected)
				{
					this._isIncomeSelected = value;
					base.OnPropertyChangedWithValue(value, "IsIncomeSelected");
				}
			}
		}

		// Token: 0x170009F2 RID: 2546
		// (get) Token: 0x06001D48 RID: 7496 RVA: 0x0006C0BD File Offset: 0x0006A2BD
		// (set) Token: 0x06001D49 RID: 7497 RVA: 0x0006C0C5 File Offset: 0x0006A2C5
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

		// Token: 0x170009F3 RID: 2547
		// (get) Token: 0x06001D4A RID: 7498 RVA: 0x0006C0E8 File Offset: 0x0006A2E8
		// (set) Token: 0x06001D4B RID: 7499 RVA: 0x0006C0F0 File Offset: 0x0006A2F0
		[DataSourceProperty]
		public string LocationText
		{
			get
			{
				return this._locationText;
			}
			set
			{
				if (value != this._locationText)
				{
					this._locationText = value;
					base.OnPropertyChangedWithValue<string>(value, "LocationText");
				}
			}
		}

		// Token: 0x170009F4 RID: 2548
		// (get) Token: 0x06001D4C RID: 7500 RVA: 0x0006C113 File Offset: 0x0006A313
		// (set) Token: 0x06001D4D RID: 7501 RVA: 0x0006C11B File Offset: 0x0006A31B
		[DataSourceProperty]
		public string IncomeText
		{
			get
			{
				return this._incomeText;
			}
			set
			{
				if (value != this._incomeText)
				{
					this._incomeText = value;
					base.OnPropertyChangedWithValue<string>(value, "IncomeText");
				}
			}
		}

		// Token: 0x04000DA0 RID: 3488
		private readonly MBBindingList<ClanFinanceWorkshopItemVM> _workshopList;

		// Token: 0x04000DA1 RID: 3489
		private readonly MBBindingList<ClanSupporterGroupVM> _supporterList;

		// Token: 0x04000DA2 RID: 3490
		private readonly MBBindingList<ClanFinanceAlleyItemVM> _alleyList;

		// Token: 0x04000DA3 RID: 3491
		private readonly ClanIncomeSortControllerVM.WorkshopItemNameComparer _workshopNameComparer;

		// Token: 0x04000DA4 RID: 3492
		private readonly ClanIncomeSortControllerVM.SupporterItemNameComparer _supporterNameComparer;

		// Token: 0x04000DA5 RID: 3493
		private readonly ClanIncomeSortControllerVM.AlleyItemNameComparer _alleyNameComparer;

		// Token: 0x04000DA6 RID: 3494
		private readonly ClanIncomeSortControllerVM.WorkshopItemLocationComparer _workshopLocationComparer;

		// Token: 0x04000DA7 RID: 3495
		private readonly ClanIncomeSortControllerVM.AlleyItemLocationComparer _alleyLocationComparer;

		// Token: 0x04000DA8 RID: 3496
		private readonly ClanIncomeSortControllerVM.WorkshopItemIncomeComparer _workshopIncomeComparer;

		// Token: 0x04000DA9 RID: 3497
		private readonly ClanIncomeSortControllerVM.SupporterItemIncomeComparer _supporterIncomeComparer;

		// Token: 0x04000DAA RID: 3498
		private readonly ClanIncomeSortControllerVM.AlleyItemIncomeComparer _alleyIncomeComparer;

		// Token: 0x04000DAB RID: 3499
		private int _nameState;

		// Token: 0x04000DAC RID: 3500
		private int _locationState;

		// Token: 0x04000DAD RID: 3501
		private int _incomeState;

		// Token: 0x04000DAE RID: 3502
		private bool _isNameSelected;

		// Token: 0x04000DAF RID: 3503
		private bool _isLocationSelected;

		// Token: 0x04000DB0 RID: 3504
		private bool _isIncomeSelected;

		// Token: 0x04000DB1 RID: 3505
		private string _nameText;

		// Token: 0x04000DB2 RID: 3506
		private string _locationText;

		// Token: 0x04000DB3 RID: 3507
		private string _incomeText;

		// Token: 0x020002A6 RID: 678
		public abstract class WorkshopItemComparerBase : IComparer<ClanFinanceWorkshopItemVM>
		{
			// Token: 0x06002627 RID: 9767 RVA: 0x0008262C File Offset: 0x0008082C
			public void SetSortMode(bool isAcending)
			{
				this._isAcending = isAcending;
			}

			// Token: 0x06002628 RID: 9768
			public abstract int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y);

			// Token: 0x0400132E RID: 4910
			protected bool _isAcending;
		}

		// Token: 0x020002A7 RID: 679
		public abstract class SupporterItemComparerBase : IComparer<ClanSupporterGroupVM>
		{
			// Token: 0x0600262A RID: 9770 RVA: 0x0008263D File Offset: 0x0008083D
			public void SetSortMode(bool isAcending)
			{
				this._isAcending = isAcending;
			}

			// Token: 0x0600262B RID: 9771
			public abstract int Compare(ClanSupporterGroupVM x, ClanSupporterGroupVM y);

			// Token: 0x0400132F RID: 4911
			protected bool _isAcending;
		}

		// Token: 0x020002A8 RID: 680
		public abstract class AlleyItemComparerBase : IComparer<ClanFinanceAlleyItemVM>
		{
			// Token: 0x0600262D RID: 9773 RVA: 0x0008264E File Offset: 0x0008084E
			public void SetSortMode(bool isAcending)
			{
				this._isAcending = isAcending;
			}

			// Token: 0x0600262E RID: 9774
			public abstract int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y);

			// Token: 0x04001330 RID: 4912
			protected bool _isAcending;
		}

		// Token: 0x020002A9 RID: 681
		public class WorkshopItemNameComparer : ClanIncomeSortControllerVM.WorkshopItemComparerBase
		{
			// Token: 0x06002630 RID: 9776 RVA: 0x0008265F File Offset: 0x0008085F
			public override int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y)
			{
				if (this._isAcending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x020002AA RID: 682
		public class SupporterItemNameComparer : ClanIncomeSortControllerVM.SupporterItemComparerBase
		{
			// Token: 0x06002632 RID: 9778 RVA: 0x00082696 File Offset: 0x00080896
			public override int Compare(ClanSupporterGroupVM x, ClanSupporterGroupVM y)
			{
				if (this._isAcending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x020002AB RID: 683
		public class AlleyItemNameComparer : ClanIncomeSortControllerVM.AlleyItemComparerBase
		{
			// Token: 0x06002634 RID: 9780 RVA: 0x000826CD File Offset: 0x000808CD
			public override int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y)
			{
				if (this._isAcending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x020002AC RID: 684
		public class WorkshopItemLocationComparer : ClanIncomeSortControllerVM.WorkshopItemComparerBase
		{
			// Token: 0x06002636 RID: 9782 RVA: 0x00082704 File Offset: 0x00080904
			public override int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y)
			{
				int num = this.GetDistanceToMainParty(y).CompareTo(this.GetDistanceToMainParty(x));
				if (this._isAcending)
				{
					return num * -1;
				}
				return num;
			}

			// Token: 0x06002637 RID: 9783 RVA: 0x00082738 File Offset: 0x00080938
			private float GetDistanceToMainParty(ClanFinanceWorkshopItemVM item)
			{
				return item.Workshop.Settlement.Position.Distance(Hero.MainHero.GetCampaignPosition());
			}
		}

		// Token: 0x020002AD RID: 685
		public class AlleyItemLocationComparer : ClanIncomeSortControllerVM.AlleyItemComparerBase
		{
			// Token: 0x06002639 RID: 9785 RVA: 0x00082770 File Offset: 0x00080970
			public override int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y)
			{
				int num = this.GetDistanceToMainParty(y).CompareTo(this.GetDistanceToMainParty(x));
				if (this._isAcending)
				{
					return num * -1;
				}
				return num;
			}

			// Token: 0x0600263A RID: 9786 RVA: 0x000827A4 File Offset: 0x000809A4
			private float GetDistanceToMainParty(ClanFinanceAlleyItemVM item)
			{
				return item.Alley.Settlement.Position.Distance(Hero.MainHero.GetCampaignPosition());
			}
		}

		// Token: 0x020002AE RID: 686
		public class WorkshopItemIncomeComparer : ClanIncomeSortControllerVM.WorkshopItemComparerBase
		{
			// Token: 0x0600263C RID: 9788 RVA: 0x000827DC File Offset: 0x000809DC
			public override int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y)
			{
				if (this._isAcending)
				{
					return y.Workshop.ProfitMade.CompareTo(x.Workshop.ProfitMade) * -1;
				}
				return y.Workshop.ProfitMade.CompareTo(x.Workshop.ProfitMade);
			}
		}

		// Token: 0x020002AF RID: 687
		public class SupporterItemIncomeComparer : ClanIncomeSortControllerVM.SupporterItemComparerBase
		{
			// Token: 0x0600263E RID: 9790 RVA: 0x00082838 File Offset: 0x00080A38
			public override int Compare(ClanSupporterGroupVM x, ClanSupporterGroupVM y)
			{
				if (this._isAcending)
				{
					return y.TotalInfluenceBonus.CompareTo(x.TotalInfluenceBonus) * -1;
				}
				return y.TotalInfluenceBonus.CompareTo(x.TotalInfluenceBonus);
			}
		}

		// Token: 0x020002B0 RID: 688
		public class AlleyItemIncomeComparer : ClanIncomeSortControllerVM.AlleyItemComparerBase
		{
			// Token: 0x06002640 RID: 9792 RVA: 0x00082880 File Offset: 0x00080A80
			public override int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y)
			{
				if (this._isAcending)
				{
					return y.Income.CompareTo(x.Income) * -1;
				}
				return y.Income.CompareTo(x.Income);
			}
		}
	}
}
