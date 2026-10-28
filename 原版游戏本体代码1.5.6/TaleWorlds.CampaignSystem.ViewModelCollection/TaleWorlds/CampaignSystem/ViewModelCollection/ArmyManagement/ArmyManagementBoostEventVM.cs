using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;

public class ArmyManagementBoostEventVM : ViewModel
{
	public enum BoostCurrency
	{
		Gold,
		Influence
	}

	private readonly Action<ArmyManagementBoostEventVM> _onExecuteEvent;

	private int _amountToPay;

	private int _amountOfCohesionToGain;

	private int _currencyType;

	private string _spendText;

	private string _gainText;

	private bool _isEnabled;

	public BoostCurrency CurrencyToPayForCohesion { get; }

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
	public int AmountToPay
	{
		get
		{
			return _amountToPay;
		}
		set
		{
			if (value != _amountToPay)
			{
				_amountToPay = value;
				OnPropertyChangedWithValue(value, "AmountToPay");
			}
		}
	}

	[DataSourceProperty]
	public int CurrencyType
	{
		get
		{
			return _currencyType;
		}
		set
		{
			if (value != _currencyType)
			{
				_currencyType = value;
				OnPropertyChangedWithValue(value, "CurrencyType");
			}
		}
	}

	[DataSourceProperty]
	public int AmountOfCohesionToGain
	{
		get
		{
			return _amountOfCohesionToGain;
		}
		set
		{
			if (value != _amountOfCohesionToGain)
			{
				_amountOfCohesionToGain = value;
				OnPropertyChangedWithValue(value, "AmountOfCohesionToGain");
			}
		}
	}

	[DataSourceProperty]
	public string SpendText
	{
		get
		{
			return _spendText;
		}
		set
		{
			if (value != _spendText)
			{
				_spendText = value;
				OnPropertyChangedWithValue(value, "SpendText");
			}
		}
	}

	[DataSourceProperty]
	public string GainText
	{
		get
		{
			return _gainText;
		}
		set
		{
			if (value != _gainText)
			{
				_gainText = value;
				OnPropertyChangedWithValue(value, "GainText");
			}
		}
	}

	public ArmyManagementBoostEventVM(BoostCurrency currencyToPayForCohesion, int amountToPay, int amountOfCohesionToGain, Action<ArmyManagementBoostEventVM> onExecuteEvent)
	{
		IsEnabled = true;
		_onExecuteEvent = onExecuteEvent;
		AmountToPay = amountToPay;
		AmountOfCohesionToGain = amountOfCohesionToGain;
		CurrencyToPayForCohesion = currencyToPayForCohesion;
		CurrencyType = (int)currencyToPayForCohesion;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GameTexts.SetVariable("AMOUNT", AmountToPay);
		SpendText = GameTexts.FindText("str_cohesion_boost_spend").ToString();
		GameTexts.SetVariable("GAIN_AMOUNT", AmountOfCohesionToGain);
		GainText = GameTexts.FindText("str_cohesion_boost_gain").ToString();
	}

	private void ExecuteEvent()
	{
		_onExecuteEvent(this);
	}
}
