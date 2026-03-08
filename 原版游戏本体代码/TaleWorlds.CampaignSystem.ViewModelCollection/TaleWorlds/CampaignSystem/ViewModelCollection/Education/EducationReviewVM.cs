using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F4 RID: 244
	public class EducationReviewVM : ViewModel
	{
		// Token: 0x060015FB RID: 5627 RVA: 0x00055D28 File Offset: 0x00053F28
		public EducationReviewVM(int pageCount)
		{
			this._pageCount = pageCount;
			this.ReviewList = new MBBindingList<EducationReviewItemVM>();
			for (int i = 0; i < this._pageCount - 1; i++)
			{
				this.ReviewList.Add(new EducationReviewItemVM());
			}
			this.RefreshValues();
		}

		// Token: 0x060015FC RID: 5628 RVA: 0x00055D98 File Offset: 0x00053F98
		public override void RefreshValues()
		{
			for (int i = 0; i < this.ReviewList.Count; i++)
			{
				this._educationPageTitle.SetTextVariable("NUMBER", i + 1);
				this.ReviewList[i].Title = this._educationPageTitle.ToString();
			}
			this.StageCompleteText = this._stageCompleteTextObject.ToString();
		}

		// Token: 0x060015FD RID: 5629 RVA: 0x00055DFC File Offset: 0x00053FFC
		public void SetGainForStage(int pageIndex, string gainText)
		{
			if (pageIndex >= 0 && pageIndex < this._pageCount)
			{
				this.ReviewList[pageIndex].UpdateWith(gainText);
			}
		}

		// Token: 0x060015FE RID: 5630 RVA: 0x00055E1D File Offset: 0x0005401D
		public void SetCurrentPage(int currentPageIndex)
		{
			this.IsEnabled = currentPageIndex == this._pageCount - 1;
		}

		// Token: 0x17000747 RID: 1863
		// (get) Token: 0x060015FF RID: 5631 RVA: 0x00055E30 File Offset: 0x00054030
		// (set) Token: 0x06001600 RID: 5632 RVA: 0x00055E38 File Offset: 0x00054038
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000748 RID: 1864
		// (get) Token: 0x06001601 RID: 5633 RVA: 0x00055E56 File Offset: 0x00054056
		// (set) Token: 0x06001602 RID: 5634 RVA: 0x00055E5E File Offset: 0x0005405E
		[DataSourceProperty]
		public string StageCompleteText
		{
			get
			{
				return this._stageCompleteText;
			}
			set
			{
				if (value != this._stageCompleteText)
				{
					this._stageCompleteText = value;
					base.OnPropertyChangedWithValue<string>(value, "StageCompleteText");
				}
			}
		}

		// Token: 0x17000749 RID: 1865
		// (get) Token: 0x06001603 RID: 5635 RVA: 0x00055E81 File Offset: 0x00054081
		// (set) Token: 0x06001604 RID: 5636 RVA: 0x00055E89 File Offset: 0x00054089
		[DataSourceProperty]
		public MBBindingList<EducationReviewItemVM> ReviewList
		{
			get
			{
				return this._reviewList;
			}
			set
			{
				if (value != this._reviewList)
				{
					this._reviewList = value;
					base.OnPropertyChangedWithValue<MBBindingList<EducationReviewItemVM>>(value, "ReviewList");
				}
			}
		}

		// Token: 0x04000A01 RID: 2561
		private readonly int _pageCount;

		// Token: 0x04000A02 RID: 2562
		private readonly TextObject _educationPageTitle = new TextObject("{=m1Yynagz}Page {NUMBER}", null);

		// Token: 0x04000A03 RID: 2563
		private readonly TextObject _stageCompleteTextObject = new TextObject("{=flxDkoMh}Stage Complete", null);

		// Token: 0x04000A04 RID: 2564
		private MBBindingList<EducationReviewItemVM> _reviewList;

		// Token: 0x04000A05 RID: 2565
		private bool _isEnabled;

		// Token: 0x04000A06 RID: 2566
		private string _stageCompleteText;
	}
}
