using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D1 RID: 209
	public class EncyclopediaContentPageVM : EncyclopediaPageVM
	{
		// Token: 0x060013AA RID: 5034 RVA: 0x0004EA56 File Offset: 0x0004CC56
		public EncyclopediaContentPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
		}

		// Token: 0x060013AB RID: 5035 RVA: 0x0004EA81 File Offset: 0x0004CC81
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PreviousButtonLabel = this._previousButtonLabelText.ToString();
			this.NextButtonLabel = this._nextButtonLabelText.ToString();
		}

		// Token: 0x060013AC RID: 5036 RVA: 0x0004EAAC File Offset: 0x0004CCAC
		public void InitializeQuickNavigation(EncyclopediaListVM list)
		{
			if (list != null && list.Items != null)
			{
				List<EncyclopediaListItemVM> list2 = (from x in list.Items
					where !x.IsFiltered
					select x).ToList<EncyclopediaListItemVM>();
				int count = list2.Count;
				int num = list2.FindIndex((EncyclopediaListItemVM x) => x.Object == base.Obj);
				if (count > 1 && num > -1)
				{
					if (num > 0)
					{
						this._previousItem = list2[num - 1];
						this.PreviousButtonHint = new HintViewModel(new TextObject(this._previousItem.Name, null), null);
						this.IsPreviousButtonEnabled = true;
					}
					if (num < count - 1)
					{
						this._nextItem = list2[num + 1];
						this.NextButtonHint = new HintViewModel(new TextObject(this._nextItem.Name, null), null);
						this.IsNextButtonEnabled = true;
					}
				}
			}
		}

		// Token: 0x060013AD RID: 5037 RVA: 0x0004EB8C File Offset: 0x0004CD8C
		public void ExecuteGoToNextItem()
		{
			if (this._nextItem != null)
			{
				this._nextItem.Execute();
				return;
			}
			Debug.FailedAssert("If the next button is enabled then next item should not be null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Pages\\EncyclopediaContentPageVM.cs", "ExecuteGoToNextItem", 66);
		}

		// Token: 0x060013AE RID: 5038 RVA: 0x0004EBB8 File Offset: 0x0004CDB8
		public void ExecuteGoToPreviousItem()
		{
			if (this._previousItem != null)
			{
				this._previousItem.Execute();
				return;
			}
			Debug.FailedAssert("If the previous button is enabled then previous item should not be null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Pages\\EncyclopediaContentPageVM.cs", "ExecuteGoToPreviousItem", 78);
		}

		// Token: 0x17000674 RID: 1652
		// (get) Token: 0x060013AF RID: 5039 RVA: 0x0004EBE4 File Offset: 0x0004CDE4
		// (set) Token: 0x060013B0 RID: 5040 RVA: 0x0004EBEC File Offset: 0x0004CDEC
		[DataSourceProperty]
		public bool IsPreviousButtonEnabled
		{
			get
			{
				return this._isPreviousButtonEnabled;
			}
			set
			{
				if (value != this._isPreviousButtonEnabled)
				{
					this._isPreviousButtonEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsPreviousButtonEnabled");
				}
			}
		}

		// Token: 0x17000675 RID: 1653
		// (get) Token: 0x060013B1 RID: 5041 RVA: 0x0004EC0A File Offset: 0x0004CE0A
		// (set) Token: 0x060013B2 RID: 5042 RVA: 0x0004EC12 File Offset: 0x0004CE12
		[DataSourceProperty]
		public bool IsNextButtonEnabled
		{
			get
			{
				return this._isNextButtonEnabled;
			}
			set
			{
				if (value != this._isNextButtonEnabled)
				{
					this._isNextButtonEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsNextButtonEnabled");
				}
			}
		}

		// Token: 0x17000676 RID: 1654
		// (get) Token: 0x060013B3 RID: 5043 RVA: 0x0004EC30 File Offset: 0x0004CE30
		// (set) Token: 0x060013B4 RID: 5044 RVA: 0x0004EC38 File Offset: 0x0004CE38
		[DataSourceProperty]
		public string PreviousButtonLabel
		{
			get
			{
				return this._previousButtonLabel;
			}
			set
			{
				if (value != this._previousButtonLabel)
				{
					this._previousButtonLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "PreviousButtonLabel");
				}
			}
		}

		// Token: 0x17000677 RID: 1655
		// (get) Token: 0x060013B5 RID: 5045 RVA: 0x0004EC5B File Offset: 0x0004CE5B
		// (set) Token: 0x060013B6 RID: 5046 RVA: 0x0004EC63 File Offset: 0x0004CE63
		[DataSourceProperty]
		public string NextButtonLabel
		{
			get
			{
				return this._nextButtonLabel;
			}
			set
			{
				if (value != this._nextButtonLabel)
				{
					this._nextButtonLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "NextButtonLabel");
				}
			}
		}

		// Token: 0x17000678 RID: 1656
		// (get) Token: 0x060013B7 RID: 5047 RVA: 0x0004EC86 File Offset: 0x0004CE86
		// (set) Token: 0x060013B8 RID: 5048 RVA: 0x0004EC8E File Offset: 0x0004CE8E
		[DataSourceProperty]
		public HintViewModel PreviousButtonHint
		{
			get
			{
				return this._previousButtonHint;
			}
			set
			{
				if (value != this._previousButtonHint)
				{
					this._previousButtonHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PreviousButtonHint");
				}
			}
		}

		// Token: 0x17000679 RID: 1657
		// (get) Token: 0x060013B9 RID: 5049 RVA: 0x0004ECAC File Offset: 0x0004CEAC
		// (set) Token: 0x060013BA RID: 5050 RVA: 0x0004ECB4 File Offset: 0x0004CEB4
		[DataSourceProperty]
		public HintViewModel NextButtonHint
		{
			get
			{
				return this._nextButtonHint;
			}
			set
			{
				if (value != this._nextButtonHint)
				{
					this._nextButtonHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "NextButtonHint");
				}
			}
		}

		// Token: 0x04000902 RID: 2306
		private EncyclopediaListItemVM _previousItem;

		// Token: 0x04000903 RID: 2307
		private EncyclopediaListItemVM _nextItem;

		// Token: 0x04000904 RID: 2308
		private TextObject _previousButtonLabelText = new TextObject("{=zlcMGAbn}Previous Page", null);

		// Token: 0x04000905 RID: 2309
		private TextObject _nextButtonLabelText = new TextObject("{=QFfMd5q3}Next Page", null);

		// Token: 0x04000906 RID: 2310
		private bool _isPreviousButtonEnabled;

		// Token: 0x04000907 RID: 2311
		private bool _isNextButtonEnabled;

		// Token: 0x04000908 RID: 2312
		private string _previousButtonLabel;

		// Token: 0x04000909 RID: 2313
		private string _nextButtonLabel;

		// Token: 0x0400090A RID: 2314
		private HintViewModel _previousButtonHint;

		// Token: 0x0400090B RID: 2315
		private HintViewModel _nextButtonHint;
	}
}
