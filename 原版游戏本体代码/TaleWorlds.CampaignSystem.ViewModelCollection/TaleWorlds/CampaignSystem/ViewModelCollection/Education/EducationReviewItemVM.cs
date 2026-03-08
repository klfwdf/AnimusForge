using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F5 RID: 245
	public class EducationReviewItemVM : ViewModel
	{
		// Token: 0x06001606 RID: 5638 RVA: 0x00055EAF File Offset: 0x000540AF
		public void UpdateWith(string gainText)
		{
			this.GainText = gainText;
		}

		// Token: 0x1700074A RID: 1866
		// (get) Token: 0x06001607 RID: 5639 RVA: 0x00055EB8 File Offset: 0x000540B8
		// (set) Token: 0x06001608 RID: 5640 RVA: 0x00055EC0 File Offset: 0x000540C0
		[DataSourceProperty]
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				if (value != this._title)
				{
					this._title = value;
					base.OnPropertyChangedWithValue<string>(value, "Title");
				}
			}
		}

		// Token: 0x1700074B RID: 1867
		// (get) Token: 0x06001609 RID: 5641 RVA: 0x00055EE3 File Offset: 0x000540E3
		// (set) Token: 0x0600160A RID: 5642 RVA: 0x00055EEB File Offset: 0x000540EB
		[DataSourceProperty]
		public string GainText
		{
			get
			{
				return this._gainText;
			}
			set
			{
				if (value != this._gainText)
				{
					this._gainText = value;
					base.OnPropertyChangedWithValue<string>(value, "GainText");
				}
			}
		}

		// Token: 0x04000A07 RID: 2567
		private string _title;

		// Token: 0x04000A08 RID: 2568
		private string _gainText;
	}
}
