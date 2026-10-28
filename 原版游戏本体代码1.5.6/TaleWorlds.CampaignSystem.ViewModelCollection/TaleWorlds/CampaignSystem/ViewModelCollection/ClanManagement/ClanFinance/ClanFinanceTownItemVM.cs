using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;

public class ClanFinanceTownItemVM : ClanFinanceIncomeItemBaseVM
{
	private bool _isUnderSiege;

	private bool _isUnderRebellion;

	private HintViewModel _isUnderSiegeHint;

	private HintViewModel _isUnderRebellionHint;

	private HintViewModel _governorHint;

	private bool _hasGovernor;

	public Settlement Settlement { get; private set; }

	[DataSourceProperty]
	public bool IsUnderSiege
	{
		get
		{
			return _isUnderSiege;
		}
		set
		{
			if (value != _isUnderSiege)
			{
				_isUnderSiege = value;
				OnPropertyChangedWithValue(value, "IsUnderSiege");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUnderRebellion
	{
		get
		{
			return _isUnderRebellion;
		}
		set
		{
			if (value != _isUnderRebellion)
			{
				_isUnderRebellion = value;
				OnPropertyChangedWithValue(value, "IsUnderRebellion");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel IsUnderSiegeHint
	{
		get
		{
			return _isUnderSiegeHint;
		}
		set
		{
			if (value != _isUnderSiegeHint)
			{
				_isUnderSiegeHint = value;
				OnPropertyChangedWithValue(value, "IsUnderSiegeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel IsUnderRebellionHint
	{
		get
		{
			return _isUnderRebellionHint;
		}
		set
		{
			if (value != _isUnderRebellionHint)
			{
				_isUnderRebellionHint = value;
				OnPropertyChangedWithValue(value, "IsUnderRebellionHint");
			}
		}
	}

	[DataSourceProperty]
	public bool HasGovernor
	{
		get
		{
			return _hasGovernor;
		}
		set
		{
			if (value != _hasGovernor)
			{
				_hasGovernor = value;
				OnPropertyChangedWithValue(value, "HasGovernor");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel GovernorHint
	{
		get
		{
			return _governorHint;
		}
		set
		{
			if (value != _governorHint)
			{
				_governorHint = value;
				OnPropertyChangedWithValue(value, "GovernorHint");
			}
		}
	}

	public ClanFinanceTownItemVM(Settlement settlement, TaxType taxType, Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
		: base(onSelection, onRefresh)
	{
		base.IncomeTypeAsEnum = IncomeTypes.Settlement;
		Settlement = settlement;
		MBTextManager.SetTextVariable("SETTLEMENT_NAME", settlement.Name.ToString());
		base.Name = ((taxType == TaxType.ProsperityTax) ? GameTexts.FindText("str_prosperity_tax").ToString() : GameTexts.FindText("str_trade_tax").ToString());
		IsUnderSiege = settlement.IsUnderSiege;
		IsUnderSiegeHint = new HintViewModel(new TextObject("{=!}PLACEHOLDER | THIS SETTLEMENT IS UNDER SIEGE"));
		IsUnderRebellion = settlement.IsUnderRebellionAttack();
		IsUnderRebellionHint = new HintViewModel(new TextObject("{=!}PLACEHOLDER | THIS SETTLEMENT IS UNDER REBELLION"));
		if (taxType == TaxType.ProsperityTax && settlement.Town != null)
		{
			float resultNumber = Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(settlement.Town).ResultNumber;
			base.Income = ((!IsUnderRebellion) ? ((int)resultNumber) : 0);
		}
		else if (taxType == TaxType.TradeTax)
		{
			if (settlement.Town != null)
			{
				base.Income = (int)((float)settlement.Town.TradeTaxAccumulated / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction());
			}
			else if (settlement.Village != null)
			{
				base.Income = ((settlement.Village.VillageState != Village.VillageStates.Looted && settlement.Village.VillageState != Village.VillageStates.BeingRaided) ? ((int)((float)settlement.Village.TradeTaxAccumulated / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction())) : 0);
			}
		}
		base.IncomeValueText = DetermineIncomeText(base.Income);
		HasGovernor = settlement.IsTown && settlement.Town.Governor != null;
	}

	protected override void PopulateActionList()
	{
	}

	protected override void PopulateStatsList()
	{
	}
}
