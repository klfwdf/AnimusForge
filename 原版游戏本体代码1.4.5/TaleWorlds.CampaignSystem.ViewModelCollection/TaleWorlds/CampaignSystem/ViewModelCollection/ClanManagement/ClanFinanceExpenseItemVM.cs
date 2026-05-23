using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanFinanceExpenseItemVM : ViewModel
{
	private const int UIWageSliderMaxLimit = 2000;

	private const int UIWageSliderMinLimit = 100;

	private readonly MobileParty _mobileParty;

	private bool _isEnabled;

	private int _minWage;

	private int _maxWage;

	private int _currentWage;

	private int _currentWageLimit;

	private string _currentWageText;

	private string _currentWageLimitText;

	private string _currentWageValueText;

	private string _currentWageLimitValueText;

	private string _unlimitedWageText;

	private string _titleText;

	private bool _isUnlimitedWage;

	private HintViewModel _wageLimitHint;

	private BasicTooltipViewModel _currentWageTooltip;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel WageLimitHint
	{
		get
		{
			return _wageLimitHint;
		}
		set
		{
			if (value != _wageLimitHint)
			{
				_wageLimitHint = value;
				OnPropertyChangedWithValue(value, "WageLimitHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CurrentWageTooltip
	{
		get
		{
			return _currentWageTooltip;
		}
		set
		{
			if (value != _currentWageTooltip)
			{
				_currentWageTooltip = value;
				OnPropertyChangedWithValue(value, "CurrentWageTooltip");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentWageText
	{
		get
		{
			return _currentWageText;
		}
		set
		{
			if (value != _currentWageText)
			{
				_currentWageText = value;
				OnPropertyChangedWithValue(value, "CurrentWageText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentWageLimitText
	{
		get
		{
			return _currentWageLimitText;
		}
		set
		{
			if (value != _currentWageLimitText)
			{
				_currentWageLimitText = value;
				OnPropertyChangedWithValue(value, "CurrentWageLimitText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentWageValueText
	{
		get
		{
			return _currentWageValueText;
		}
		set
		{
			if (value != _currentWageValueText)
			{
				_currentWageValueText = value;
				OnPropertyChangedWithValue(value, "CurrentWageValueText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentWageLimitValueText
	{
		get
		{
			return _currentWageLimitValueText;
		}
		set
		{
			if (value != _currentWageLimitValueText)
			{
				_currentWageLimitValueText = value;
				OnPropertyChangedWithValue(value, "CurrentWageLimitValueText");
			}
		}
	}

	[DataSourceProperty]
	public string UnlimitedWageText
	{
		get
		{
			return _unlimitedWageText;
		}
		set
		{
			if (value != _unlimitedWageText)
			{
				_unlimitedWageText = value;
				OnPropertyChangedWithValue(value, "UnlimitedWageText");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentWage
	{
		get
		{
			return _currentWage;
		}
		set
		{
			if (value != _currentWage)
			{
				_currentWage = value;
				OnPropertyChangedWithValue(value, "CurrentWage");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentWageLimit
	{
		get
		{
			return _currentWageLimit;
		}
		set
		{
			if (value != _currentWageLimit)
			{
				_currentWageLimit = value;
				OnPropertyChangedWithValue(value, "CurrentWageLimit");
				OnCurrentWageLimitUpdated(value);
			}
		}
	}

	[DataSourceProperty]
	public int MinWage
	{
		get
		{
			return _minWage;
		}
		set
		{
			if (value != _minWage)
			{
				_minWage = value;
				OnPropertyChangedWithValue(value, "MinWage");
			}
		}
	}

	[DataSourceProperty]
	public int MaxWage
	{
		get
		{
			return _maxWage;
		}
		set
		{
			if (value != _maxWage)
			{
				_maxWage = value;
				OnPropertyChangedWithValue(value, "MaxWage");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUnlimitedWage
	{
		get
		{
			return _isUnlimitedWage;
		}
		set
		{
			if (value != _isUnlimitedWage)
			{
				_isUnlimitedWage = value;
				OnPropertyChangedWithValue(value, "IsUnlimitedWage");
				OnUnlimitedWageToggled(value);
			}
		}
	}

	public ClanFinanceExpenseItemVM(MobileParty mobileParty)
	{
		_mobileParty = mobileParty;
		CurrentWageTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyWageTooltip(mobileParty));
		MinWage = 100;
		MaxWage = 2000;
		CurrentWage = _mobileParty.TotalWage;
		CurrentWageValueText = CurrentWage.ToString();
		IsUnlimitedWage = !_mobileParty.HasLimitedWage();
		CurrentWageLimit = ((_mobileParty.PaymentLimit == Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit) ? 2000 : _mobileParty.PaymentLimit);
		IsEnabled = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CurrentWageText = new TextObject("{=pnFgwLYG}Current Wage").ToString();
		CurrentWageLimitText = new TextObject("{=sWWxrafa}Current Limit").ToString();
		TitleText = new TextObject("{=qdoJOH0j}Party Wage").ToString();
		UnlimitedWageText = new TextObject("{=WySAapWO}Unlimited Wage").ToString();
		WageLimitHint = new HintViewModel(new TextObject("{=w0slxNAl}If limit is lower than current wage, party will not recruit troops until wage is reduced to the limit. If limit is higher than current wage, party will keep recruiting."));
		UpdateCurrentWageLimitText();
	}

	private void OnCurrentWageLimitUpdated(int newValue)
	{
		if (!IsUnlimitedWage)
		{
			_mobileParty.SetWagePaymentLimit(newValue);
		}
		UpdateCurrentWageLimitText();
	}

	private void OnUnlimitedWageToggled(bool newValue)
	{
		CurrentWageLimit = 2000;
		if (newValue)
		{
			_mobileParty.SetWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
		}
		else
		{
			_mobileParty.SetWagePaymentLimit(2000);
		}
		UpdateCurrentWageLimitText();
	}

	private void UpdateCurrentWageLimitText()
	{
		CurrentWageLimitValueText = (IsUnlimitedWage ? new TextObject("{=lC5xsoSh}Unlimited").ToString() : CurrentWageLimit.ToString());
	}
}
