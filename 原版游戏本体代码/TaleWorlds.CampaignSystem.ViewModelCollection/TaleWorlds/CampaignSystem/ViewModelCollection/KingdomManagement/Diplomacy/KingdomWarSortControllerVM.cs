using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x02000074 RID: 116
	public class KingdomWarSortControllerVM : ViewModel
	{
		// Token: 0x0600096C RID: 2412 RVA: 0x00029B6A File Offset: 0x00027D6A
		public KingdomWarSortControllerVM(ref MBBindingList<KingdomWarItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._scoreComparer = new KingdomWarSortControllerVM.ItemScoreComparer();
		}

		// Token: 0x0600096D RID: 2413 RVA: 0x00029B88 File Offset: 0x00027D88
		private void ExecuteSortByScore()
		{
			int scoreState = this.ScoreState;
			this.SetAllStates(CampaignUIHelper.SortState.Default);
			this.ScoreState = (scoreState + 1) % 3;
			if (this.ScoreState == 0)
			{
				int scoreState2 = this.ScoreState;
				this.ScoreState = scoreState2 + 1;
			}
			this._scoreComparer.SetSortMode(this.ScoreState == 1);
			this._listToControl.Sort(this._scoreComparer);
			this.IsScoreSelected = true;
		}

		// Token: 0x0600096E RID: 2414 RVA: 0x00029BF2 File Offset: 0x00027DF2
		private void SetAllStates(CampaignUIHelper.SortState state)
		{
			this.ScoreState = (int)state;
			this.IsScoreSelected = false;
		}

		// Token: 0x170002C9 RID: 713
		// (get) Token: 0x0600096F RID: 2415 RVA: 0x00029C02 File Offset: 0x00027E02
		// (set) Token: 0x06000970 RID: 2416 RVA: 0x00029C0A File Offset: 0x00027E0A
		[DataSourceProperty]
		public int ScoreState
		{
			get
			{
				return this._scoreState;
			}
			set
			{
				if (value != this._scoreState)
				{
					this._scoreState = value;
					base.OnPropertyChangedWithValue(value, "ScoreState");
				}
			}
		}

		// Token: 0x170002CA RID: 714
		// (get) Token: 0x06000971 RID: 2417 RVA: 0x00029C28 File Offset: 0x00027E28
		// (set) Token: 0x06000972 RID: 2418 RVA: 0x00029C30 File Offset: 0x00027E30
		[DataSourceProperty]
		public bool IsScoreSelected
		{
			get
			{
				return this._isScoreSelected;
			}
			set
			{
				if (value != this._isScoreSelected)
				{
					this._isScoreSelected = value;
					base.OnPropertyChangedWithValue(value, "IsScoreSelected");
				}
			}
		}

		// Token: 0x04000426 RID: 1062
		private readonly MBBindingList<KingdomWarItemVM> _listToControl;

		// Token: 0x04000427 RID: 1063
		private readonly KingdomWarSortControllerVM.ItemScoreComparer _scoreComparer;

		// Token: 0x04000428 RID: 1064
		private int _scoreState;

		// Token: 0x04000429 RID: 1065
		private bool _isScoreSelected;

		// Token: 0x020001D6 RID: 470
		public abstract class ItemComparerBase : IComparer<KingdomWarItemVM>
		{
			// Token: 0x06002382 RID: 9090 RVA: 0x0007E4B8 File Offset: 0x0007C6B8
			public void SetSortMode(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x06002383 RID: 9091
			public abstract int Compare(KingdomWarItemVM x, KingdomWarItemVM y);

			// Token: 0x0400110A RID: 4362
			protected bool _isAscending;
		}

		// Token: 0x020001D7 RID: 471
		public class ItemScoreComparer : KingdomWarSortControllerVM.ItemComparerBase
		{
			// Token: 0x06002385 RID: 9093 RVA: 0x0007E4CC File Offset: 0x0007C6CC
			public override int Compare(KingdomWarItemVM x, KingdomWarItemVM y)
			{
				if (this._isAscending)
				{
					return x.Score.CompareTo(y.Score);
				}
				return x.Score.CompareTo(y.Score) * -1;
			}
		}
	}
}
