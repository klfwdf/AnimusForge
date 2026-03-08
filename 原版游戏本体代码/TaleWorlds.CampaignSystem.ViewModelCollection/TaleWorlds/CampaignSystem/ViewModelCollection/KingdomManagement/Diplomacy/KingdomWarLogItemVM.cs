using System;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x02000073 RID: 115
	public class KingdomWarLogItemVM : ViewModel
	{
		// Token: 0x06000963 RID: 2403 RVA: 0x00029A6C File Offset: 0x00027C6C
		public KingdomWarLogItemVM(IEncyclopediaLog log, IFaction effectorFaction)
		{
			this._log = log;
			this.Banner = new BannerImageIdentifierVM(effectorFaction.Banner, true);
			this.RefreshValues();
		}

		// Token: 0x06000964 RID: 2404 RVA: 0x00029A94 File Offset: 0x00027C94
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.WarLogTimeText = this._log.GameTime.ToString();
			this.WarLogText = this._log.GetEncyclopediaText().ToString();
		}

		// Token: 0x06000965 RID: 2405 RVA: 0x00029ADC File Offset: 0x00027CDC
		private void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x170002C6 RID: 710
		// (get) Token: 0x06000966 RID: 2406 RVA: 0x00029AEE File Offset: 0x00027CEE
		// (set) Token: 0x06000967 RID: 2407 RVA: 0x00029AF6 File Offset: 0x00027CF6
		[DataSourceProperty]
		public string WarLogTimeText
		{
			get
			{
				return this._warLogTimeText;
			}
			set
			{
				if (value != this._warLogTimeText)
				{
					this._warLogTimeText = value;
					base.OnPropertyChangedWithValue<string>(value, "WarLogTimeText");
				}
			}
		}

		// Token: 0x170002C7 RID: 711
		// (get) Token: 0x06000968 RID: 2408 RVA: 0x00029B19 File Offset: 0x00027D19
		// (set) Token: 0x06000969 RID: 2409 RVA: 0x00029B21 File Offset: 0x00027D21
		[DataSourceProperty]
		public string WarLogText
		{
			get
			{
				return this._warLogText;
			}
			set
			{
				if (value != this._warLogText)
				{
					this._warLogText = value;
					base.OnPropertyChangedWithValue<string>(value, "WarLogText");
				}
			}
		}

		// Token: 0x170002C8 RID: 712
		// (get) Token: 0x0600096A RID: 2410 RVA: 0x00029B44 File Offset: 0x00027D44
		// (set) Token: 0x0600096B RID: 2411 RVA: 0x00029B4C File Offset: 0x00027D4C
		[DataSourceProperty]
		public BannerImageIdentifierVM Banner
		{
			get
			{
				return this._banner;
			}
			set
			{
				if (value != this._banner)
				{
					this._banner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Banner");
				}
			}
		}

		// Token: 0x04000422 RID: 1058
		private readonly IEncyclopediaLog _log;

		// Token: 0x04000423 RID: 1059
		private string _warLogText;

		// Token: 0x04000424 RID: 1060
		private string _warLogTimeText;

		// Token: 0x04000425 RID: 1061
		private BannerImageIdentifierVM _banner;
	}
}
