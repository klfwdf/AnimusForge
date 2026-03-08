using System;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000E7 RID: 231
	public class EncyclopediaHistoryEventVM : EncyclopediaLinkVM
	{
		// Token: 0x06001578 RID: 5496 RVA: 0x0005446B File Offset: 0x0005266B
		public EncyclopediaHistoryEventVM(IEncyclopediaLog log)
		{
			this._log = log;
			this.RefreshValues();
		}

		// Token: 0x06001579 RID: 5497 RVA: 0x00054480 File Offset: 0x00052680
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.HistoryEventTimeText = this._log.GameTime.ToString();
			this.HistoryEventText = this._log.GetEncyclopediaText().ToString();
		}

		// Token: 0x0600157A RID: 5498 RVA: 0x000544C8 File Offset: 0x000526C8
		public void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x1700071A RID: 1818
		// (get) Token: 0x0600157B RID: 5499 RVA: 0x000544DA File Offset: 0x000526DA
		// (set) Token: 0x0600157C RID: 5500 RVA: 0x000544E2 File Offset: 0x000526E2
		[DataSourceProperty]
		public string HistoryEventTimeText
		{
			get
			{
				return this._historyEventTimeText;
			}
			set
			{
				if (value != this._historyEventTimeText)
				{
					this._historyEventTimeText = value;
					base.OnPropertyChangedWithValue<string>(value, "HistoryEventTimeText");
				}
			}
		}

		// Token: 0x1700071B RID: 1819
		// (get) Token: 0x0600157D RID: 5501 RVA: 0x00054505 File Offset: 0x00052705
		// (set) Token: 0x0600157E RID: 5502 RVA: 0x0005450D File Offset: 0x0005270D
		[DataSourceProperty]
		public string HistoryEventText
		{
			get
			{
				return this._historyEventText;
			}
			set
			{
				if (value != this._historyEventText)
				{
					this._historyEventText = value;
					base.OnPropertyChangedWithValue<string>(value, "HistoryEventText");
				}
			}
		}

		// Token: 0x040009C7 RID: 2503
		private readonly IEncyclopediaLog _log;

		// Token: 0x040009C8 RID: 2504
		private string _historyEventText;

		// Token: 0x040009C9 RID: 2505
		private string _historyEventTimeText;
	}
}
