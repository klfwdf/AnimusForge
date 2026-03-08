using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance
{
	// Token: 0x02000135 RID: 309
	public class ClanFinanceTownItemVM : ClanFinanceIncomeItemBaseVM
	{
		// Token: 0x170009B8 RID: 2488
		// (get) Token: 0x06001C9A RID: 7322 RVA: 0x00069A5A File Offset: 0x00067C5A
		// (set) Token: 0x06001C9B RID: 7323 RVA: 0x00069A62 File Offset: 0x00067C62
		public Settlement Settlement { get; private set; }

		// Token: 0x06001C9C RID: 7324 RVA: 0x00069A6C File Offset: 0x00067C6C
		public ClanFinanceTownItemVM(Settlement settlement, TaxType taxType, Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
			: base(onSelection, onRefresh)
		{
			base.IncomeTypeAsEnum = IncomeTypes.Settlement;
			this.Settlement = settlement;
			MBTextManager.SetTextVariable("SETTLEMENT_NAME", settlement.Name.ToString(), false);
			base.Name = ((taxType == TaxType.ProsperityTax) ? GameTexts.FindText("str_prosperity_tax", null).ToString() : GameTexts.FindText("str_trade_tax", null).ToString());
			this.IsUnderSiege = settlement.IsUnderSiege;
			this.IsUnderSiegeHint = new HintViewModel(new TextObject("{=!}PLACEHOLDER | THIS SETTLEMENT IS UNDER SIEGE", null), null);
			this.IsUnderRebellion = settlement.IsUnderRebellionAttack();
			this.IsUnderRebellionHint = new HintViewModel(new TextObject("{=!}PLACEHOLDER | THIS SETTLEMENT IS UNDER REBELLION", null), null);
			if (taxType == TaxType.ProsperityTax && settlement.Town != null)
			{
				float resultNumber = Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(settlement.Town, false).ResultNumber;
				base.Income = (this.IsUnderRebellion ? 0 : ((int)resultNumber));
			}
			else if (taxType == TaxType.TradeTax)
			{
				if (settlement.Town != null)
				{
					base.Income = (int)((float)settlement.Town.TradeTaxAccumulated / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction());
				}
				else if (settlement.Village != null)
				{
					base.Income = ((settlement.Village.VillageState == Village.VillageStates.Looted || settlement.Village.VillageState == Village.VillageStates.BeingRaided) ? 0 : ((int)((float)settlement.Village.TradeTaxAccumulated / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction())));
				}
			}
			base.IncomeValueText = base.DetermineIncomeText(base.Income);
			this.HasGovernor = settlement.IsTown && settlement.Town.Governor != null;
		}

		// Token: 0x06001C9D RID: 7325 RVA: 0x00069C17 File Offset: 0x00067E17
		protected override void PopulateActionList()
		{
		}

		// Token: 0x06001C9E RID: 7326 RVA: 0x00069C19 File Offset: 0x00067E19
		protected override void PopulateStatsList()
		{
		}

		// Token: 0x170009B9 RID: 2489
		// (get) Token: 0x06001C9F RID: 7327 RVA: 0x00069C1B File Offset: 0x00067E1B
		// (set) Token: 0x06001CA0 RID: 7328 RVA: 0x00069C23 File Offset: 0x00067E23
		[DataSourceProperty]
		public bool IsUnderSiege
		{
			get
			{
				return this._isUnderSiege;
			}
			set
			{
				if (value != this._isUnderSiege)
				{
					this._isUnderSiege = value;
					base.OnPropertyChangedWithValue(value, "IsUnderSiege");
				}
			}
		}

		// Token: 0x170009BA RID: 2490
		// (get) Token: 0x06001CA1 RID: 7329 RVA: 0x00069C41 File Offset: 0x00067E41
		// (set) Token: 0x06001CA2 RID: 7330 RVA: 0x00069C49 File Offset: 0x00067E49
		[DataSourceProperty]
		public bool IsUnderRebellion
		{
			get
			{
				return this._isUnderRebellion;
			}
			set
			{
				if (value != this._isUnderRebellion)
				{
					this._isUnderRebellion = value;
					base.OnPropertyChangedWithValue(value, "IsUnderRebellion");
				}
			}
		}

		// Token: 0x170009BB RID: 2491
		// (get) Token: 0x06001CA3 RID: 7331 RVA: 0x00069C67 File Offset: 0x00067E67
		// (set) Token: 0x06001CA4 RID: 7332 RVA: 0x00069C6F File Offset: 0x00067E6F
		[DataSourceProperty]
		public HintViewModel IsUnderSiegeHint
		{
			get
			{
				return this._isUnderSiegeHint;
			}
			set
			{
				if (value != this._isUnderSiegeHint)
				{
					this._isUnderSiegeHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "IsUnderSiegeHint");
				}
			}
		}

		// Token: 0x170009BC RID: 2492
		// (get) Token: 0x06001CA5 RID: 7333 RVA: 0x00069C8D File Offset: 0x00067E8D
		// (set) Token: 0x06001CA6 RID: 7334 RVA: 0x00069C95 File Offset: 0x00067E95
		[DataSourceProperty]
		public HintViewModel IsUnderRebellionHint
		{
			get
			{
				return this._isUnderRebellionHint;
			}
			set
			{
				if (value != this._isUnderRebellionHint)
				{
					this._isUnderRebellionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "IsUnderRebellionHint");
				}
			}
		}

		// Token: 0x170009BD RID: 2493
		// (get) Token: 0x06001CA7 RID: 7335 RVA: 0x00069CB3 File Offset: 0x00067EB3
		// (set) Token: 0x06001CA8 RID: 7336 RVA: 0x00069CBB File Offset: 0x00067EBB
		[DataSourceProperty]
		public bool HasGovernor
		{
			get
			{
				return this._hasGovernor;
			}
			set
			{
				if (value != this._hasGovernor)
				{
					this._hasGovernor = value;
					base.OnPropertyChangedWithValue(value, "HasGovernor");
				}
			}
		}

		// Token: 0x170009BE RID: 2494
		// (get) Token: 0x06001CA9 RID: 7337 RVA: 0x00069CD9 File Offset: 0x00067ED9
		// (set) Token: 0x06001CAA RID: 7338 RVA: 0x00069CE1 File Offset: 0x00067EE1
		[DataSourceProperty]
		public HintViewModel GovernorHint
		{
			get
			{
				return this._governorHint;
			}
			set
			{
				if (value != this._governorHint)
				{
					this._governorHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "GovernorHint");
				}
			}
		}

		// Token: 0x04000D58 RID: 3416
		private bool _isUnderSiege;

		// Token: 0x04000D59 RID: 3417
		private bool _isUnderRebellion;

		// Token: 0x04000D5A RID: 3418
		private HintViewModel _isUnderSiegeHint;

		// Token: 0x04000D5B RID: 3419
		private HintViewModel _isUnderRebellionHint;

		// Token: 0x04000D5C RID: 3420
		private HintViewModel _governorHint;

		// Token: 0x04000D5D RID: 3421
		private bool _hasGovernor;
	}
}
