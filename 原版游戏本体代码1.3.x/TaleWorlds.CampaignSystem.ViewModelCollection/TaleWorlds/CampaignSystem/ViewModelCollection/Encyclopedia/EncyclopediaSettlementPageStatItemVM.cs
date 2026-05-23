using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia
{
	// Token: 0x020000CD RID: 205
	public class EncyclopediaSettlementPageStatItemVM : ViewModel
	{
		// Token: 0x0600135A RID: 4954 RVA: 0x0004DA39 File Offset: 0x0004BC39
		public EncyclopediaSettlementPageStatItemVM(BasicTooltipViewModel basicTooltipViewModel, EncyclopediaSettlementPageStatItemVM.DescriptionType type, string statText)
		{
			this._basicTooltipViewModel = basicTooltipViewModel;
			this._typeString = type.ToString();
			this._statText = statText;
		}

		// Token: 0x17000654 RID: 1620
		// (get) Token: 0x0600135B RID: 4955 RVA: 0x0004DA62 File Offset: 0x0004BC62
		// (set) Token: 0x0600135C RID: 4956 RVA: 0x0004DA6A File Offset: 0x0004BC6A
		[DataSourceProperty]
		public BasicTooltipViewModel BasicTooltipViewModel
		{
			get
			{
				return this._basicTooltipViewModel;
			}
			set
			{
				if (value != this._basicTooltipViewModel)
				{
					this._basicTooltipViewModel = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "BasicTooltipViewModel");
				}
			}
		}

		// Token: 0x17000655 RID: 1621
		// (get) Token: 0x0600135D RID: 4957 RVA: 0x0004DA88 File Offset: 0x0004BC88
		// (set) Token: 0x0600135E RID: 4958 RVA: 0x0004DA90 File Offset: 0x0004BC90
		[DataSourceProperty]
		public string TypeString
		{
			get
			{
				return this._typeString;
			}
			set
			{
				if (value != this._typeString)
				{
					this._typeString = value;
					base.OnPropertyChangedWithValue<string>(value, "TypeString");
				}
			}
		}

		// Token: 0x17000656 RID: 1622
		// (get) Token: 0x0600135F RID: 4959 RVA: 0x0004DAB3 File Offset: 0x0004BCB3
		// (set) Token: 0x06001360 RID: 4960 RVA: 0x0004DABB File Offset: 0x0004BCBB
		[DataSourceProperty]
		public string StatText
		{
			get
			{
				return this._statText;
			}
			set
			{
				if (value != this._statText)
				{
					this._statText = value;
					base.OnPropertyChangedWithValue<string>(value, "StatText");
				}
			}
		}

		// Token: 0x040008DE RID: 2270
		private BasicTooltipViewModel _basicTooltipViewModel;

		// Token: 0x040008DF RID: 2271
		private string _typeString;

		// Token: 0x040008E0 RID: 2272
		private string _statText;

		// Token: 0x0200023A RID: 570
		public enum DescriptionType
		{
			// Token: 0x0400121E RID: 4638
			Wall,
			// Token: 0x0400121F RID: 4639
			Shipyard,
			// Token: 0x04001220 RID: 4640
			Garrison,
			// Token: 0x04001221 RID: 4641
			Militia,
			// Token: 0x04001222 RID: 4642
			Food,
			// Token: 0x04001223 RID: 4643
			Prosperity,
			// Token: 0x04001224 RID: 4644
			Loyalty,
			// Token: 0x04001225 RID: 4645
			Security
		}
	}
}
