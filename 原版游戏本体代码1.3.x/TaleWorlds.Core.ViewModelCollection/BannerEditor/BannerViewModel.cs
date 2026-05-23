using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.BannerEditor
{
	// Token: 0x0200002E RID: 46
	public class BannerViewModel : ViewModel
	{
		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060001E8 RID: 488 RVA: 0x000060DC File Offset: 0x000042DC
		public Banner Banner { get; }

		// Token: 0x060001E9 RID: 489 RVA: 0x000060E4 File Offset: 0x000042E4
		public BannerViewModel(Banner banner)
		{
			this.Banner = banner;
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060001EA RID: 490 RVA: 0x000060F3 File Offset: 0x000042F3
		// (set) Token: 0x060001EB RID: 491 RVA: 0x00006100 File Offset: 0x00004300
		[DataSourceProperty]
		public string BannerCode
		{
			get
			{
				return this.Banner.BannerCode;
			}
			set
			{
				if (value != this.Banner.BannerCode)
				{
					this.Banner.Deserialize(value);
					base.OnPropertyChangedWithValue<string>(value, "BannerCode");
				}
			}
		}
	}
}
