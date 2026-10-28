using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection;

public class PowerLevelComparer : ViewModel
{
	private double _totalStrength;

	private double _totalInitialStrength;

	private double _defenderBattlePower;

	private double _attackerBattlePower;

	private double _defenderBattlePowerValue;

	private double _attackerBattlePowerValue;

	private double _initialDefenderBattlePower;

	private double _initialAttackerBattlePower;

	private double _initialDefenderBattlePowerValue;

	private double _initialAttackerBattlePowerValue;

	private float _defenderRelativePower;

	private float _attackerRelativePower;

	private string _defenderColor = "#5E8C23FF";

	private string _attackerColor = "#A0341EFF";

	private bool _isEnabled = true;

	private HintViewModel _hint;

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
	public double DefenderBattlePower
	{
		get
		{
			return _defenderBattlePower;
		}
		set
		{
			if (value != _defenderBattlePower)
			{
				_defenderBattlePower = value;
				OnPropertyChangedWithValue(value, "DefenderBattlePower");
			}
		}
	}

	[DataSourceProperty]
	public double DefenderBattlePowerValue
	{
		get
		{
			return _defenderBattlePowerValue;
		}
		set
		{
			if (value != _defenderBattlePowerValue)
			{
				_defenderBattlePowerValue = value;
				OnPropertyChangedWithValue(value, "DefenderBattlePowerValue");
			}
		}
	}

	[DataSourceProperty]
	public double AttackerBattlePower
	{
		get
		{
			return _attackerBattlePower;
		}
		set
		{
			if (value != _attackerBattlePower)
			{
				_attackerBattlePower = value;
				OnPropertyChangedWithValue(value, "AttackerBattlePower");
			}
		}
	}

	[DataSourceProperty]
	public double AttackerBattlePowerValue
	{
		get
		{
			return _attackerBattlePowerValue;
		}
		set
		{
			if (value != _attackerBattlePowerValue)
			{
				_attackerBattlePowerValue = value;
				OnPropertyChangedWithValue(value, "AttackerBattlePowerValue");
			}
		}
	}

	[DataSourceProperty]
	public double InitialDefenderBattlePower
	{
		get
		{
			return _initialDefenderBattlePower;
		}
		set
		{
			if (value != _initialDefenderBattlePower)
			{
				_initialDefenderBattlePower = value;
				OnPropertyChangedWithValue(value, "InitialDefenderBattlePower");
			}
		}
	}

	[DataSourceProperty]
	public double InitialAttackerBattlePower
	{
		get
		{
			return _initialAttackerBattlePower;
		}
		set
		{
			if (value != _initialAttackerBattlePower)
			{
				_initialAttackerBattlePower = value;
				OnPropertyChangedWithValue(value, "InitialAttackerBattlePower");
			}
		}
	}

	[DataSourceProperty]
	public double InitialDefenderBattlePowerValue
	{
		get
		{
			return _initialDefenderBattlePowerValue;
		}
		set
		{
			if (value != _initialDefenderBattlePowerValue)
			{
				_initialDefenderBattlePowerValue = value;
				OnPropertyChangedWithValue(value, "InitialDefenderBattlePowerValue");
			}
		}
	}

	[DataSourceProperty]
	public double InitialAttackerBattlePowerValue
	{
		get
		{
			return _initialAttackerBattlePowerValue;
		}
		set
		{
			if (value != _initialAttackerBattlePowerValue)
			{
				_initialAttackerBattlePowerValue = value;
				OnPropertyChangedWithValue(value, "InitialAttackerBattlePowerValue");
			}
		}
	}

	[DataSourceProperty]
	public float DefenderRelativePower
	{
		get
		{
			return _defenderRelativePower;
		}
		set
		{
			if (value != _defenderRelativePower)
			{
				_defenderRelativePower = value;
				OnPropertyChangedWithValue(value, "DefenderRelativePower");
			}
		}
	}

	[DataSourceProperty]
	public float AttackerRelativePower
	{
		get
		{
			return _attackerRelativePower;
		}
		set
		{
			if (value != _attackerRelativePower)
			{
				_attackerRelativePower = value;
				OnPropertyChangedWithValue(value, "AttackerRelativePower");
			}
		}
	}

	[DataSourceProperty]
	public string DefenderColor
	{
		get
		{
			return _defenderColor;
		}
		set
		{
			if (value != _defenderColor)
			{
				_defenderColor = value;
				OnPropertyChangedWithValue(value, "DefenderColor");
			}
		}
	}

	[DataSourceProperty]
	public string AttackerColor
	{
		get
		{
			return _attackerColor;
		}
		set
		{
			if (value != _attackerColor)
			{
				_attackerColor = value;
				OnPropertyChangedWithValue(value, "AttackerColor");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	public PowerLevelComparer(double defenderPower, double attackerPower)
	{
		_totalStrength = defenderPower + attackerPower;
		_totalInitialStrength = _totalStrength;
		InitialDefenderBattlePowerValue = defenderPower;
		InitialAttackerBattlePowerValue = attackerPower;
		InitialDefenderBattlePower = defenderPower / _totalStrength;
		InitialAttackerBattlePower = attackerPower / _totalStrength;
		Update(defenderPower, attackerPower);
		Hint = new HintViewModel(GameTexts.FindText("str_power_levels"));
	}

	public void SetColors(string defenderColor, string attackerColor)
	{
		DefenderColor = defenderColor;
		AttackerColor = attackerColor;
	}

	public void Update(double defenderPower, double attackerPower)
	{
		Update(defenderPower, attackerPower, InitialDefenderBattlePowerValue, InitialAttackerBattlePowerValue);
	}

	public void Update(double defenderPower, double attackerPower, double initialDefenderPower, double initialAttackerPower)
	{
		_totalStrength = defenderPower + attackerPower;
		_totalInitialStrength = initialDefenderPower + initialAttackerPower;
		InitialDefenderBattlePower = initialDefenderPower / (initialDefenderPower + initialAttackerPower);
		InitialAttackerBattlePower = initialAttackerPower / (initialDefenderPower + initialAttackerPower);
		InitialDefenderBattlePowerValue = initialDefenderPower;
		InitialAttackerBattlePowerValue = initialAttackerPower;
		DefenderBattlePower = defenderPower / _totalStrength;
		AttackerBattlePower = attackerPower / _totalStrength;
		DefenderBattlePowerValue = defenderPower;
		AttackerBattlePowerValue = attackerPower;
		DefenderRelativePower = ((initialDefenderPower == 0.0) ? 0f : ((float)(defenderPower / initialDefenderPower)));
		AttackerRelativePower = ((initialAttackerPower == 0.0) ? 0f : ((float)(attackerPower / initialAttackerPower)));
	}
}
