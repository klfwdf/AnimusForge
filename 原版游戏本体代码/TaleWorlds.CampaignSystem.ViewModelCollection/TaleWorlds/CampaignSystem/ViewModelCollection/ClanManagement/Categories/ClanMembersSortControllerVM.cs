using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x0200013C RID: 316
	public class ClanMembersSortControllerVM : ViewModel
	{
		// Token: 0x06001D81 RID: 7553 RVA: 0x0006CC88 File Offset: 0x0006AE88
		public ClanMembersSortControllerVM(MBBindingList<MBBindingList<ClanLordItemVM>> listsToControl)
		{
			this._listsToControl = listsToControl;
			this._nameComparer = new ClanMembersSortControllerVM.ItemNameComparer();
			this._locationComparer = new ClanMembersSortControllerVM.ItemLocationComparer();
		}

		// Token: 0x06001D82 RID: 7554 RVA: 0x0006CCAD File Offset: 0x0006AEAD
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.LocationText = GameTexts.FindText("str_tooltip_label_location", null).ToString();
		}

		// Token: 0x06001D83 RID: 7555 RVA: 0x0006CCE4 File Offset: 0x0006AEE4
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
			foreach (MBBindingList<ClanLordItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._nameComparer);
			}
			this.IsNameSelected = true;
		}

		// Token: 0x06001D84 RID: 7556 RVA: 0x0006CD80 File Offset: 0x0006AF80
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
			foreach (MBBindingList<ClanLordItemVM> mbbindingList in this._listsToControl)
			{
				mbbindingList.Sort(this._locationComparer);
			}
			this.IsLocationSelected = true;
		}

		// Token: 0x06001D85 RID: 7557 RVA: 0x0006CE1C File Offset: 0x0006B01C
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.NameState = (int)state;
			this.LocationState = (int)state;
			this.IsNameSelected = false;
			this.IsLocationSelected = false;
		}

		// Token: 0x06001D86 RID: 7558 RVA: 0x0006CE3A File Offset: 0x0006B03A
		public void ResetAllStates()
		{
			this.SetAllStates(CampaignUIHelper.SortState.Default);
		}

		// Token: 0x17000A08 RID: 2568
		// (get) Token: 0x06001D87 RID: 7559 RVA: 0x0006CE43 File Offset: 0x0006B043
		// (set) Token: 0x06001D88 RID: 7560 RVA: 0x0006CE4B File Offset: 0x0006B04B
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

		// Token: 0x17000A09 RID: 2569
		// (get) Token: 0x06001D89 RID: 7561 RVA: 0x0006CE69 File Offset: 0x0006B069
		// (set) Token: 0x06001D8A RID: 7562 RVA: 0x0006CE71 File Offset: 0x0006B071
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

		// Token: 0x17000A0A RID: 2570
		// (get) Token: 0x06001D8B RID: 7563 RVA: 0x0006CE8F File Offset: 0x0006B08F
		// (set) Token: 0x06001D8C RID: 7564 RVA: 0x0006CE97 File Offset: 0x0006B097
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

		// Token: 0x17000A0B RID: 2571
		// (get) Token: 0x06001D8D RID: 7565 RVA: 0x0006CEB5 File Offset: 0x0006B0B5
		// (set) Token: 0x06001D8E RID: 7566 RVA: 0x0006CEBD File Offset: 0x0006B0BD
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

		// Token: 0x17000A0C RID: 2572
		// (get) Token: 0x06001D8F RID: 7567 RVA: 0x0006CEDB File Offset: 0x0006B0DB
		// (set) Token: 0x06001D90 RID: 7568 RVA: 0x0006CEE3 File Offset: 0x0006B0E3
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

		// Token: 0x17000A0D RID: 2573
		// (get) Token: 0x06001D91 RID: 7569 RVA: 0x0006CF06 File Offset: 0x0006B106
		// (set) Token: 0x06001D92 RID: 7570 RVA: 0x0006CF0E File Offset: 0x0006B10E
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

		// Token: 0x04000DC9 RID: 3529
		private readonly MBBindingList<MBBindingList<ClanLordItemVM>> _listsToControl;

		// Token: 0x04000DCA RID: 3530
		private readonly ClanMembersSortControllerVM.ItemNameComparer _nameComparer;

		// Token: 0x04000DCB RID: 3531
		private readonly ClanMembersSortControllerVM.ItemLocationComparer _locationComparer;

		// Token: 0x04000DCC RID: 3532
		private int _nameState;

		// Token: 0x04000DCD RID: 3533
		private int _locationState;

		// Token: 0x04000DCE RID: 3534
		private bool _isNameSelected;

		// Token: 0x04000DCF RID: 3535
		private bool _isLocationSelected;

		// Token: 0x04000DD0 RID: 3536
		private string _nameText;

		// Token: 0x04000DD1 RID: 3537
		private string _locationText;

		// Token: 0x020002B2 RID: 690
		public abstract class ItemComparerBase : IComparer<ClanLordItemVM>
		{
			// Token: 0x06002647 RID: 9799 RVA: 0x000828F4 File Offset: 0x00080AF4
			public void SetSortMode(bool isAcending)
			{
				this._isAcending = isAcending;
			}

			// Token: 0x06002648 RID: 9800
			public abstract int Compare(ClanLordItemVM x, ClanLordItemVM y);

			// Token: 0x04001335 RID: 4917
			protected bool _isAcending;
		}

		// Token: 0x020002B3 RID: 691
		public class ItemNameComparer : ClanMembersSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600264A RID: 9802 RVA: 0x00082905 File Offset: 0x00080B05
			public override int Compare(ClanLordItemVM x, ClanLordItemVM y)
			{
				if (this._isAcending)
				{
					return y.Name.CompareTo(x.Name) * -1;
				}
				return y.Name.CompareTo(x.Name);
			}
		}

		// Token: 0x020002B4 RID: 692
		public class ItemLocationComparer : ClanMembersSortControllerVM.ItemComparerBase
		{
			// Token: 0x0600264C RID: 9804 RVA: 0x0008293C File Offset: 0x00080B3C
			public override int Compare(ClanLordItemVM x, ClanLordItemVM y)
			{
				int num = this.GetDistanceToMainHero(y).CompareTo(this.GetDistanceToMainHero(x));
				if (this._isAcending)
				{
					return num * -1;
				}
				return num;
			}

			// Token: 0x0600264D RID: 9805 RVA: 0x00082970 File Offset: 0x00080B70
			private float GetDistanceToMainHero(ClanLordItemVM item)
			{
				return item.GetHero().GetCampaignPosition().Distance(Hero.MainHero.GetCampaignPosition());
			}
		}
	}
}
