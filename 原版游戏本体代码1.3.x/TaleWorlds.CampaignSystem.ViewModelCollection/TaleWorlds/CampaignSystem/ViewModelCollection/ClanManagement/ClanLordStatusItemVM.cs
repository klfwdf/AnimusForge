using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000127 RID: 295
	public class ClanLordStatusItemVM : ViewModel
	{
		// Token: 0x06001ACD RID: 6861 RVA: 0x0006423C File Offset: 0x0006243C
		public ClanLordStatusItemVM(ClanLordStatusItemVM.LordStatus status, TextObject hintText)
		{
			this.Type = (int)status;
			this.Hint = new HintViewModel(hintText, null);
		}

		// Token: 0x1700090B RID: 2315
		// (get) Token: 0x06001ACE RID: 6862 RVA: 0x0006425F File Offset: 0x0006245F
		// (set) Token: 0x06001ACF RID: 6863 RVA: 0x00064267 File Offset: 0x00062467
		[DataSourceProperty]
		public int Type
		{
			get
			{
				return this._type;
			}
			set
			{
				if (value != this._type)
				{
					this._type = value;
					base.OnPropertyChangedWithValue(value, "Type");
				}
			}
		}

		// Token: 0x1700090C RID: 2316
		// (get) Token: 0x06001AD0 RID: 6864 RVA: 0x00064285 File Offset: 0x00062485
		// (set) Token: 0x06001AD1 RID: 6865 RVA: 0x0006428D File Offset: 0x0006248D
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x04000C82 RID: 3202
		private int _type = -1;

		// Token: 0x04000C83 RID: 3203
		private HintViewModel _hint;

		// Token: 0x02000280 RID: 640
		public enum LordStatus
		{
			// Token: 0x040012B7 RID: 4791
			Dead,
			// Token: 0x040012B8 RID: 4792
			Married,
			// Token: 0x040012B9 RID: 4793
			Pregnant,
			// Token: 0x040012BA RID: 4794
			InBattle,
			// Token: 0x040012BB RID: 4795
			InSiege,
			// Token: 0x040012BC RID: 4796
			Child,
			// Token: 0x040012BD RID: 4797
			Prisoner,
			// Token: 0x040012BE RID: 4798
			Sick
		}
	}
}
