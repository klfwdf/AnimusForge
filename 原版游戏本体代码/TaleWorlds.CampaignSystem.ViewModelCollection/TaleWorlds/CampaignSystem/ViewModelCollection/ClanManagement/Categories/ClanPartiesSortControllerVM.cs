using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x0200013E RID: 318
	public class ClanPartiesSortControllerVM : ViewModel
	{
		// Token: 0x06001DB6 RID: 7606 RVA: 0x0006D8B3 File Offset: 0x0006BAB3
		public ClanPartiesSortControllerVM(MBBindingList<MBBindingList<ClanPartyItemVM>> listsToControl)
		{
			this._listsToControl = listsToControl;
			this._nameComparer = new ClanPartiesSortControllerVM.ItemNameComparer();
			this._locationComparer = new ClanPartiesSortControllerVM.ItemLocationComparer();
			this._sizeComparer = new ClanPartiesSortControllerVM.ItemSizeComparer();
			this._shipCountComparer = new ClanPartiesSortControllerVM.ItemShipCountComparer();
		}

		// Token: 0x06001DB7 RID: 7607 RVA: 0x0006D8F0 File Offset: 0x0006BAF0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.LocationText = GameTexts.FindText("str_tooltip_label_location", null).ToString();
			this.SizeText = GameTexts.FindText("str_clan_party_size", null).ToString();
			this.ShipCountText = new TextObject("{=URbKirPS}Ship Count", null).ToString();
		}

		// Token: 0x06001DB8 RID: 7608 RVA: 0x0006D95C File Offset: 0x0006BB5C
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
			foreach (MBBindingList<ClanPartyItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._nameComparer);
			}
			this.IsNameSelected = true;
		}

		// Token: 0x06001DB9 RID: 7609 RVA: 0x0006D9F8 File Offset: 0x0006BBF8
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
			this._locationComparer.SetSortMode(this.LocationState == 1);
			foreach (MBBindingList<ClanPartyItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._locationComparer);
			}
			this.IsLocationSelected = true;
		}

		// Token: 0x06001DBA RID: 7610 RVA: 0x0006DA94 File Offset: 0x0006BC94
		public void ExecuteSortBySize()
		{
			int sizeState = this.SizeState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.SizeState = (sizeState + 1) % 3;
			if (this.SizeState == 0)
			{
				int sizeState2 = this.SizeState;
				this.SizeState = sizeState2 + 1;
			}
			this._sizeComparer.SetSortMode(this.SizeState == 1);
			foreach (MBBindingList<ClanPartyItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._sizeComparer);
			}
			this.IsSizeSelected = true;
		}

		// Token: 0x06001DBB RID: 7611 RVA: 0x0006DB30 File Offset: 0x0006BD30
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
			foreach (MBBindingList<ClanPartyItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._shipCountComparer);
			}
			this.IsShipCountSelected = true;
		}

		// Token: 0x06001DBC RID: 7612 RVA: 0x0006DBCC File Offset: 0x0006BDCC
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.NameState = (int)state;
			this.LocationState = (int)state;
			this.SizeState = (int)state;
			this.ShipCountState = (int)state;
			this.IsNameSelected = false;
			this.IsLocationSelected = false;
			this.IsSizeSelected = false;
			this.IsShipCountSelected = false;
		}

		// Token: 0x06001DBD RID: 7613 RVA: 0x0006DC06 File Offset: 0x0006BE06
		public void ResetAllStates()
		{
			this.SetAllStates(CampaignUIHelper.SortState.Default);
		}

		// Token: 0x17000A1A RID: 2586
		// (get) Token: 0x06001DBE RID: 7614 RVA: 0x0006DC0F File Offset: 0x0006BE0F
		// (set) Token: 0x06001DBF RID: 7615 RVA: 0x0006DC17 File Offset: 0x0006BE17
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

		// Token: 0x17000A1B RID: 2587
		// (get) Token: 0x06001DC0 RID: 7616 RVA: 0x0006DC35 File Offset: 0x0006BE35
		// (set) Token: 0x06001DC1 RID: 7617 RVA: 0x0006DC3D File Offset: 0x0006BE3D
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

		// Token: 0x17000A1C RID: 2588
		// (get) Token: 0x06001DC2 RID: 7618 RVA: 0x0006DC5B File Offset: 0x0006BE5B
		// (set) Token: 0x06001DC3 RID: 7619 RVA: 0x0006DC63 File Offset: 0x0006BE63
		[DataSourceProperty]
		public int SizeState
		{
			get
			{
				return this._sizeState;
			}
			set
			{
				if (value != this._sizeState)
				{
					this._sizeState = value;
					base.OnPropertyChangedWithValue(value, "SizeState");
				}
			}
		}

		// Token: 0x17000A1D RID: 2589
		// (get) Token: 0x06001DC4 RID: 7620 RVA: 0x0006DC81 File Offset: 0x0006BE81
		// (set) Token: 0x06001DC5 RID: 7621 RVA: 0x0006DC89 File Offset: 0x0006BE89
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

		// Token: 0x17000A1E RID: 2590
		// (get) Token: 0x06001DC6 RID: 7622 RVA: 0x0006DCA7 File Offset: 0x0006BEA7
		// (set) Token: 0x06001DC7 RID: 7623 RVA: 0x0006DCAF File Offset: 0x0006BEAF
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

		// Token: 0x17000A1F RID: 2591
		// (get) Token: 0x06001DC8 RID: 7624 RVA: 0x0006DCCD File Offset: 0x0006BECD
		// (set) Token: 0x06001DC9 RID: 7625 RVA: 0x0006DCD5 File Offset: 0x0006BED5
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

		// Token: 0x17000A20 RID: 2592
		// (get) Token: 0x06001DCA RID: 7626 RVA: 0x0006DCF3 File Offset: 0x0006BEF3
		// (set) Token: 0x06001DCB RID: 7627 RVA: 0x0006DCFB File Offset: 0x0006BEFB
		[DataSourceProperty]
		public bool IsSizeSelected
		{
			get
			{
				return this._isSizeSelected;
			}
			set
			{
				if (value != this._isSizeSelected)
				{
					this._isSizeSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSizeSelected");
				}
			}
		}

		// Token: 0x17000A21 RID: 2593
		// (get) Token: 0x06001DCC RID: 7628 RVA: 0x0006DD19 File Offset: 0x0006BF19
		// (set) Token: 0x06001DCD RID: 7629 RVA: 0x0006DD21 File Offset: 0x0006BF21
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

		// Token: 0x17000A22 RID: 2594
		// (get) Token: 0x06001DCE RID: 7630 RVA: 0x0006DD3F File Offset: 0x0006BF3F
		// (set) Token: 0x06001DCF RID: 7631 RVA: 0x0006DD47 File Offset: 0x0006BF47
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

		// Token: 0x17000A23 RID: 2595
		// (get) Token: 0x06001DD0 RID: 7632 RVA: 0x0006DD6A File Offset: 0x0006BF6A
		// (set) Token: 0x06001DD1 RID: 7633 RVA: 0x0006DD72 File Offset: 0x0006BF72
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

		// Token: 0x17000A24 RID: 2596
		// (get) Token: 0x06001DD2 RID: 7634 RVA: 0x0006DD95 File Offset: 0x0006BF95
		// (set) Token: 0x06001DD3 RID: 7635 RVA: 0x0006DD9D File Offset: 0x0006BF9D
		[DataSourceProperty]
		public string SizeText
		{
			get
			{
				return this._sizeText;
			}
			set
			{
				if (value != this._sizeText)
				{
					this._sizeText = value;
					base.OnPropertyChangedWithValue<string>(value, "SizeText");
				}
			}
		}

		// Token: 0x17000A25 RID: 2597
		// (get) Token: 0x06001DD4 RID: 7636 RVA: 0x0006DDC0 File Offset: 0x0006BFC0
		// (set) Token: 0x06001DD5 RID: 7637 RVA: 0x0006DDC8 File Offset: 0x0006BFC8
		[DataSourceProperty]
		public string ShipCountText
		{
			get
			{
				return this._shipCountText;
			}
			set
			{
				if (value != this._shipCountText)
				{
					this._shipCountText = value;
					base.OnPropertyChangedWithValue<string>(value, "ShipCountText");
				}
			}
		}

		// Token: 0x04000DE2 RID: 3554
		private readonly MBBindingList<MBBindingList<ClanPartyItemVM>> _listsToControl;

		// Token: 0x04000DE3 RID: 3555
		private readonly ClanPartiesSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x04000DE4 RID: 3556
		private readonly ClanPartiesSortControllerVM.ItemLocationComparer _locationComparer;

		// Token: 0x04000DE5 RID: 3557
		private readonly ClanPartiesSortControllerVM.ItemSizeComparer _sizeComparer;

		// Token: 0x04000DE6 RID: 3558
		private readonly ClanPartiesSortControllerVM.ItemShipCountComparer _shipCountComparer;

		// Token: 0x04000DE7 RID: 3559
		private int _nameState;

		// Token: 0x04000DE8 RID: 3560
		private int _locationState;

		// Token: 0x04000DE9 RID: 3561
		private int _sizeState;

		// Token: 0x04000DEA RID: 3562
		private int _shipCountState;

		// Token: 0x04000DEB RID: 3563
		private bool _isNameSelected;

		// Token: 0x04000DEC RID: 3564
		private bool _isLocationSelected;

		// Token: 0x04000DED RID: 3565
		private bool _isSizeSelected;

		// Token: 0x04000DEE RID: 3566
		private bool _isShipCountSelected;

		// Token: 0x04000DEF RID: 3567
		private string _nameText;

		// Token: 0x04000DF0 RID: 3568
		private string _locationText;

		// Token: 0x04000DF1 RID: 3569
		private string _sizeText;

		// Token: 0x04000DF2 RID: 3570
		private string _shipCountText;

		// Token: 0x020002B6 RID: 694
		public abstract class ItemComparerBase : IComparer<ClanPartyItemVM>
		{
			// Token: 0x06002656 RID: 9814 RVA: 0x000829DE File Offset: 0x00080BDE
			public void SetSortMode(bool isAcending)
			{
				this._isAcending = isAcending;
			}

			// Token: 0x06002657 RID: 9815
			public abstract int Compare(ClanPartyItemVM x, ClanPartyItemVM y);

			// Token: 0x0400133C RID: 4924
			protected bool _isAcending;
		}

		// Token: 0x020002B7 RID: 695
		public class ItemNameComparer : ClanPartiesSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002659 RID: 9817 RVA: 0x000829EF File Offset: 0x00080BEF
			public override int Compare(ClanPartyItemVM x, ClanPartyItemVM y)
			{
				if (this._isAcending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x020002B8 RID: 696
		public class ItemLocationComparer : ClanPartiesSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600265B RID: 9819 RVA: 0x00082A28 File Offset: 0x00080C28
			public override int Compare(ClanPartyItemVM x, ClanPartyItemVM y)
			{
				int num = this.GetDistanceToMainParty(y).CompareTo(this.GetDistanceToMainParty(x));
				if (this._isAcending)
				{
					return num * -1;
				}
				return num;
			}

			// Token: 0x0600265C RID: 9820 RVA: 0x00082A5C File Offset: 0x00080C5C
			private float GetDistanceToMainParty(ClanPartyItemVM item)
			{
				return item.Party.MobileParty.Position.Distance(Hero.MainHero.GetCampaignPosition());
			}
		}

		// Token: 0x020002B9 RID: 697
		public class ItemSizeComparer : ClanPartiesSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600265E RID: 9822 RVA: 0x00082A94 File Offset: 0x00080C94
			public override int Compare(ClanPartyItemVM x, ClanPartyItemVM y)
			{
				if (this._isAcending)
				{
					return y.Party.MobileParty.MemberRoster.TotalManCount.CompareTo(x.Party.MobileParty.MemberRoster.TotalManCount) * -1;
				}
				return y.Party.MobileParty.MemberRoster.TotalManCount.CompareTo(x.Party.MobileParty.MemberRoster.TotalManCount);
			}
		}

		// Token: 0x020002BA RID: 698
		public class ItemShipCountComparer : ClanPartiesSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002660 RID: 9824 RVA: 0x00082B18 File Offset: 0x00080D18
			public override int Compare(ClanPartyItemVM x, ClanPartyItemVM y)
			{
				if (this._isAcending)
				{
					return y.Party.Ships.Count.CompareTo(x.Party.Ships.Count) * -1;
				}
				return y.Party.Ships.Count.CompareTo(x.Party.Ships.Count);
			}
		}
	}
}
