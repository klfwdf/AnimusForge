using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000122 RID: 290
	public class ClanFinanceExpenseItemVM : ViewModel
	{
		// Token: 0x06001A52 RID: 6738 RVA: 0x000630D0 File Offset: 0x000612D0
		public ClanFinanceExpenseItemVM(MobileParty mobileParty)
		{
			this._mobileParty = mobileParty;
			this.CurrentWageTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyWageTooltip(mobileParty));
			this.MinWage = 100;
			this.MaxWage = 2000;
			this.CurrentWage = this._mobileParty.TotalWage;
			this.CurrentWageValueText = this.CurrentWage.ToString();
			this.IsUnlimitedWage = !this._mobileParty.HasLimitedWage();
			this.CurrentWageLimit = ((this._mobileParty.PaymentLimit == Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit) ? 2000 : this._mobileParty.PaymentLimit);
			this.IsEnabled = true;
			this.RefreshValues();
		}

		// Token: 0x06001A53 RID: 6739 RVA: 0x000631A8 File Offset: 0x000613A8
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.CurrentWageText = new TextObject("{=pnFgwLYG}Current Wage", null).ToString();
			this.CurrentWageLimitText = new TextObject("{=sWWxrafa}Current Limit", null).ToString();
			this.TitleText = new TextObject("{=qdoJOH0j}Party Wage", null).ToString();
			this.UnlimitedWageText = new TextObject("{=WySAapWO}Unlimited Wage", null).ToString();
			this.WageLimitHint = new HintViewModel(new TextObject("{=w0slxNAl}If limit is lower than current wage, party will not recruit troops until wage is reduced to the limit. If limit is higher than current wage, party will keep recruiting.", null), null);
			this.UpdateCurrentWageLimitText();
		}

		// Token: 0x06001A54 RID: 6740 RVA: 0x00063230 File Offset: 0x00061430
		private void OnCurrentWageLimitUpdated(int newValue)
		{
			if (!this.IsUnlimitedWage)
			{
				this._mobileParty.SetWagePaymentLimit(newValue);
			}
			this.UpdateCurrentWageLimitText();
		}

		// Token: 0x06001A55 RID: 6741 RVA: 0x0006324C File Offset: 0x0006144C
		private void OnUnlimitedWageToggled(bool newValue)
		{
			this.CurrentWageLimit = 2000;
			if (newValue)
			{
				this._mobileParty.SetWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
			}
			else
			{
				this._mobileParty.SetWagePaymentLimit(2000);
			}
			this.UpdateCurrentWageLimitText();
		}

		// Token: 0x06001A56 RID: 6742 RVA: 0x000632A0 File Offset: 0x000614A0
		private void UpdateCurrentWageLimitText()
		{
			this.CurrentWageLimitValueText = (this.IsUnlimitedWage ? new TextObject("{=lC5xsoSh}Unlimited", null).ToString() : this.CurrentWageLimit.ToString());
		}

		// Token: 0x170008DA RID: 2266
		// (get) Token: 0x06001A57 RID: 6743 RVA: 0x000632DB File Offset: 0x000614DB
		// (set) Token: 0x06001A58 RID: 6744 RVA: 0x000632E3 File Offset: 0x000614E3
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

		// Token: 0x170008DB RID: 2267
		// (get) Token: 0x06001A59 RID: 6745 RVA: 0x00063301 File Offset: 0x00061501
		// (set) Token: 0x06001A5A RID: 6746 RVA: 0x00063309 File Offset: 0x00061509
		[DataSourceProperty]
		public HintViewModel WageLimitHint
		{
			get
			{
				return this._wageLimitHint;
			}
			set
			{
				if (value != this._wageLimitHint)
				{
					this._wageLimitHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "WageLimitHint");
				}
			}
		}

		// Token: 0x170008DC RID: 2268
		// (get) Token: 0x06001A5B RID: 6747 RVA: 0x00063327 File Offset: 0x00061527
		// (set) Token: 0x06001A5C RID: 6748 RVA: 0x0006332F File Offset: 0x0006152F
		[DataSourceProperty]
		public BasicTooltipViewModel CurrentWageTooltip
		{
			get
			{
				return this._currentWageTooltip;
			}
			set
			{
				if (value != this._currentWageTooltip)
				{
					this._currentWageTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CurrentWageTooltip");
				}
			}
		}

		// Token: 0x170008DD RID: 2269
		// (get) Token: 0x06001A5D RID: 6749 RVA: 0x0006334D File Offset: 0x0006154D
		// (set) Token: 0x06001A5E RID: 6750 RVA: 0x00063355 File Offset: 0x00061555
		[DataSourceProperty]
		public string CurrentWageText
		{
			get
			{
				return this._currentWageText;
			}
			set
			{
				if (value != this._currentWageText)
				{
					this._currentWageText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentWageText");
				}
			}
		}

		// Token: 0x170008DE RID: 2270
		// (get) Token: 0x06001A5F RID: 6751 RVA: 0x00063378 File Offset: 0x00061578
		// (set) Token: 0x06001A60 RID: 6752 RVA: 0x00063380 File Offset: 0x00061580
		[DataSourceProperty]
		public string CurrentWageLimitText
		{
			get
			{
				return this._currentWageLimitText;
			}
			set
			{
				if (value != this._currentWageLimitText)
				{
					this._currentWageLimitText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentWageLimitText");
				}
			}
		}

		// Token: 0x170008DF RID: 2271
		// (get) Token: 0x06001A61 RID: 6753 RVA: 0x000633A3 File Offset: 0x000615A3
		// (set) Token: 0x06001A62 RID: 6754 RVA: 0x000633AB File Offset: 0x000615AB
		[DataSourceProperty]
		public string CurrentWageValueText
		{
			get
			{
				return this._currentWageValueText;
			}
			set
			{
				if (value != this._currentWageValueText)
				{
					this._currentWageValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentWageValueText");
				}
			}
		}

		// Token: 0x170008E0 RID: 2272
		// (get) Token: 0x06001A63 RID: 6755 RVA: 0x000633CE File Offset: 0x000615CE
		// (set) Token: 0x06001A64 RID: 6756 RVA: 0x000633D6 File Offset: 0x000615D6
		[DataSourceProperty]
		public string CurrentWageLimitValueText
		{
			get
			{
				return this._currentWageLimitValueText;
			}
			set
			{
				if (value != this._currentWageLimitValueText)
				{
					this._currentWageLimitValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentWageLimitValueText");
				}
			}
		}

		// Token: 0x170008E1 RID: 2273
		// (get) Token: 0x06001A65 RID: 6757 RVA: 0x000633F9 File Offset: 0x000615F9
		// (set) Token: 0x06001A66 RID: 6758 RVA: 0x00063401 File Offset: 0x00061601
		[DataSourceProperty]
		public string UnlimitedWageText
		{
			get
			{
				return this._unlimitedWageText;
			}
			set
			{
				if (value != this._unlimitedWageText)
				{
					this._unlimitedWageText = value;
					base.OnPropertyChangedWithValue<string>(value, "UnlimitedWageText");
				}
			}
		}

		// Token: 0x170008E2 RID: 2274
		// (get) Token: 0x06001A67 RID: 6759 RVA: 0x00063424 File Offset: 0x00061624
		// (set) Token: 0x06001A68 RID: 6760 RVA: 0x0006342C File Offset: 0x0006162C
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x170008E3 RID: 2275
		// (get) Token: 0x06001A69 RID: 6761 RVA: 0x0006344F File Offset: 0x0006164F
		// (set) Token: 0x06001A6A RID: 6762 RVA: 0x00063457 File Offset: 0x00061657
		[DataSourceProperty]
		public int CurrentWage
		{
			get
			{
				return this._currentWage;
			}
			set
			{
				if (value != this._currentWage)
				{
					this._currentWage = value;
					base.OnPropertyChangedWithValue(value, "CurrentWage");
				}
			}
		}

		// Token: 0x170008E4 RID: 2276
		// (get) Token: 0x06001A6B RID: 6763 RVA: 0x00063475 File Offset: 0x00061675
		// (set) Token: 0x06001A6C RID: 6764 RVA: 0x0006347D File Offset: 0x0006167D
		[DataSourceProperty]
		public int CurrentWageLimit
		{
			get
			{
				return this._currentWageLimit;
			}
			set
			{
				if (value != this._currentWageLimit)
				{
					this._currentWageLimit = value;
					base.OnPropertyChangedWithValue(value, "CurrentWageLimit");
					this.OnCurrentWageLimitUpdated(value);
				}
			}
		}

		// Token: 0x170008E5 RID: 2277
		// (get) Token: 0x06001A6D RID: 6765 RVA: 0x000634A2 File Offset: 0x000616A2
		// (set) Token: 0x06001A6E RID: 6766 RVA: 0x000634AA File Offset: 0x000616AA
		[DataSourceProperty]
		public int MinWage
		{
			get
			{
				return this._minWage;
			}
			set
			{
				if (value != this._minWage)
				{
					this._minWage = value;
					base.OnPropertyChangedWithValue(value, "MinWage");
				}
			}
		}

		// Token: 0x170008E6 RID: 2278
		// (get) Token: 0x06001A6F RID: 6767 RVA: 0x000634C8 File Offset: 0x000616C8
		// (set) Token: 0x06001A70 RID: 6768 RVA: 0x000634D0 File Offset: 0x000616D0
		[DataSourceProperty]
		public int MaxWage
		{
			get
			{
				return this._maxWage;
			}
			set
			{
				if (value != this._maxWage)
				{
					this._maxWage = value;
					base.OnPropertyChangedWithValue(value, "MaxWage");
				}
			}
		}

		// Token: 0x170008E7 RID: 2279
		// (get) Token: 0x06001A71 RID: 6769 RVA: 0x000634EE File Offset: 0x000616EE
		// (set) Token: 0x06001A72 RID: 6770 RVA: 0x000634F6 File Offset: 0x000616F6
		[DataSourceProperty]
		public bool IsUnlimitedWage
		{
			get
			{
				return this._isUnlimitedWage;
			}
			set
			{
				if (value != this._isUnlimitedWage)
				{
					this._isUnlimitedWage = value;
					base.OnPropertyChangedWithValue(value, "IsUnlimitedWage");
					this.OnUnlimitedWageToggled(value);
				}
			}
		}

		// Token: 0x04000C39 RID: 3129
		private const int UIWageSliderMaxLimit = 2000;

		// Token: 0x04000C3A RID: 3130
		private const int UIWageSliderMinLimit = 100;

		// Token: 0x04000C3B RID: 3131
		private readonly MobileParty _mobileParty;

		// Token: 0x04000C3C RID: 3132
		private bool _isEnabled;

		// Token: 0x04000C3D RID: 3133
		private int _minWage;

		// Token: 0x04000C3E RID: 3134
		private int _maxWage;

		// Token: 0x04000C3F RID: 3135
		private int _currentWage;

		// Token: 0x04000C40 RID: 3136
		private int _currentWageLimit;

		// Token: 0x04000C41 RID: 3137
		private string _currentWageText;

		// Token: 0x04000C42 RID: 3138
		private string _currentWageLimitText;

		// Token: 0x04000C43 RID: 3139
		private string _currentWageValueText;

		// Token: 0x04000C44 RID: 3140
		private string _currentWageLimitValueText;

		// Token: 0x04000C45 RID: 3141
		private string _unlimitedWageText;

		// Token: 0x04000C46 RID: 3142
		private string _titleText;

		// Token: 0x04000C47 RID: 3143
		private bool _isUnlimitedWage;

		// Token: 0x04000C48 RID: 3144
		private HintViewModel _wageLimitHint;

		// Token: 0x04000C49 RID: 3145
		private BasicTooltipViewModel _currentWageTooltip;
	}
}
