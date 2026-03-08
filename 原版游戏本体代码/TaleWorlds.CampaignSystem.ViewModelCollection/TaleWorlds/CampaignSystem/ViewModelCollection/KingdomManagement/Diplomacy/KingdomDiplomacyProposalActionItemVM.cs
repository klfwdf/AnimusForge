using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x0200006E RID: 110
	public class KingdomDiplomacyProposalActionItemVM : ViewModel
	{
		// Token: 0x060008DF RID: 2271 RVA: 0x00027984 File Offset: 0x00025B84
		public KingdomDiplomacyProposalActionItemVM(TextObject nameText, TextObject explanationText, int influenceCost, bool isEnabled, TextObject hintText, Action action)
		{
			this._nameText = nameText;
			this._explanationText = explanationText;
			this._action = action;
			this.InfluenceCost = influenceCost;
			this.IsEnabled = isEnabled;
			this.Hint = new HintViewModel(hintText, null);
			this.RefreshValues();
		}

		// Token: 0x060008E0 RID: 2272 RVA: 0x000279D0 File Offset: 0x00025BD0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._nameText.ToString();
			this.Explanation = this._explanationText.ToString();
		}

		// Token: 0x060008E1 RID: 2273 RVA: 0x000279FA File Offset: 0x00025BFA
		public void ExecuteAction()
		{
			Action action = this._action;
			if (action == null)
			{
				return;
			}
			action();
		}

		// Token: 0x1700029B RID: 667
		// (get) Token: 0x060008E2 RID: 2274 RVA: 0x00027A0C File Offset: 0x00025C0C
		// (set) Token: 0x060008E3 RID: 2275 RVA: 0x00027A14 File Offset: 0x00025C14
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x1700029C RID: 668
		// (get) Token: 0x060008E4 RID: 2276 RVA: 0x00027A37 File Offset: 0x00025C37
		// (set) Token: 0x060008E5 RID: 2277 RVA: 0x00027A3F File Offset: 0x00025C3F
		[DataSourceProperty]
		public string Explanation
		{
			get
			{
				return this._explanation;
			}
			set
			{
				if (value != this._explanation)
				{
					this._explanation = value;
					base.OnPropertyChangedWithValue<string>(value, "Explanation");
				}
			}
		}

		// Token: 0x1700029D RID: 669
		// (get) Token: 0x060008E6 RID: 2278 RVA: 0x00027A62 File Offset: 0x00025C62
		// (set) Token: 0x060008E7 RID: 2279 RVA: 0x00027A6A File Offset: 0x00025C6A
		[DataSourceProperty]
		public int InfluenceCost
		{
			get
			{
				return this._influenceCost;
			}
			set
			{
				if (value != this._influenceCost)
				{
					this._influenceCost = value;
					base.OnPropertyChangedWithValue(value, "InfluenceCost");
				}
			}
		}

		// Token: 0x1700029E RID: 670
		// (get) Token: 0x060008E8 RID: 2280 RVA: 0x00027A88 File Offset: 0x00025C88
		// (set) Token: 0x060008E9 RID: 2281 RVA: 0x00027A90 File Offset: 0x00025C90
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

		// Token: 0x1700029F RID: 671
		// (get) Token: 0x060008EA RID: 2282 RVA: 0x00027AAE File Offset: 0x00025CAE
		// (set) Token: 0x060008EB RID: 2283 RVA: 0x00027AB6 File Offset: 0x00025CB6
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

		// Token: 0x040003E4 RID: 996
		private readonly TextObject _nameText;

		// Token: 0x040003E5 RID: 997
		private readonly TextObject _explanationText;

		// Token: 0x040003E6 RID: 998
		private readonly Action _action;

		// Token: 0x040003E7 RID: 999
		private string _name;

		// Token: 0x040003E8 RID: 1000
		private string _explanation;

		// Token: 0x040003E9 RID: 1001
		private bool _isEnabled;

		// Token: 0x040003EA RID: 1002
		private int _influenceCost;

		// Token: 0x040003EB RID: 1003
		private HintViewModel _hint;
	}
}
