using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class NumericOptionDataVM : GenericOptionDataVM
{
	private float _initialValue;

	private INumericOptionData _numericOptionData;

	private int _discreteIncrementInterval;

	private float _min;

	private float _max;

	private float _optionValue;

	private bool _isDiscrete;

	private bool _updateContinuously;

	[DataSourceProperty]
	public int DiscreteIncrementInterval
	{
		get
		{
			return _discreteIncrementInterval;
		}
		set
		{
			if (value != _discreteIncrementInterval)
			{
				_discreteIncrementInterval = value;
				OnPropertyChangedWithValue(value, "DiscreteIncrementInterval");
			}
		}
	}

	[DataSourceProperty]
	public float Min
	{
		get
		{
			return _min;
		}
		set
		{
			if (value != _min)
			{
				_min = value;
				OnPropertyChangedWithValue(value, "Min");
			}
		}
	}

	[DataSourceProperty]
	public float Max
	{
		get
		{
			return _max;
		}
		set
		{
			if (value != _max)
			{
				_max = value;
				OnPropertyChangedWithValue(value, "Max");
			}
		}
	}

	[DataSourceProperty]
	public float OptionValue
	{
		get
		{
			return _optionValue;
		}
		set
		{
			if (value != _optionValue)
			{
				_optionValue = value;
				OnPropertyChangedWithValue(value, "OptionValue");
				OnPropertyChanged("OptionValueAsString");
				UpdateValue();
			}
		}
	}

	[DataSourceProperty]
	public bool IsDiscrete
	{
		get
		{
			return _isDiscrete;
		}
		set
		{
			if (value != _isDiscrete)
			{
				_isDiscrete = value;
				OnPropertyChangedWithValue(value, "IsDiscrete");
			}
		}
	}

	[DataSourceProperty]
	public bool UpdateContinuously
	{
		get
		{
			return _updateContinuously;
		}
		set
		{
			if (value != _updateContinuously)
			{
				_updateContinuously = value;
				OnPropertyChangedWithValue(value, "UpdateContinuously");
			}
		}
	}

	[DataSourceProperty]
	public string OptionValueAsString => GetValueAsString();

	public NumericOptionDataVM(OptionsVM optionsVM, INumericOptionData option, TextObject name, TextObject description)
		: base(optionsVM, option, name, description, OptionsVM.OptionsDataType.NumericOption)
	{
		_numericOptionData = option;
		_initialValue = _numericOptionData.GetValue(forceRefresh: false);
		Min = _numericOptionData.GetMinValue();
		Max = _numericOptionData.GetMaxValue();
		IsDiscrete = _numericOptionData.GetIsDiscrete();
		DiscreteIncrementInterval = _numericOptionData.GetDiscreteIncrementInterval();
		UpdateContinuously = _numericOptionData.GetShouldUpdateContinuously();
		OptionValue = _initialValue;
	}

	private string GetValueAsString()
	{
		string result = (IsDiscrete ? ((int)_optionValue).ToString() : _optionValue.ToString("F"));
		if (!_numericOptionData.IsNative() && !_numericOptionData.IsAction())
		{
			ManagedOptions.ManagedOptionsType managedOptionsType = (ManagedOptions.ManagedOptionsType)_numericOptionData.GetOptionType();
			if (managedOptionsType == ManagedOptions.ManagedOptionsType.AutoSaveInterval)
			{
				if ((int)Min < (int)_optionValue)
				{
					return result;
				}
				return new TextObject("{=1JlzQIXE}Disabled").ToString();
			}
			return result;
		}
		return result;
	}

	public override void UpdateValue()
	{
		Option.SetValue(OptionValue);
		Option.Commit();
		_optionsVM.SetConfig(Option, OptionValue);
	}

	public override void Cancel()
	{
		OptionValue = _initialValue;
		UpdateValue();
	}

	public override void SetValue(float value)
	{
		OptionValue = value;
	}

	public override void ResetData()
	{
		OptionValue = Option.GetDefaultValue();
	}

	public override bool IsChanged()
	{
		return _initialValue != OptionValue;
	}

	public override void ApplyValue()
	{
		if (_initialValue != OptionValue)
		{
			_initialValue = OptionValue;
		}
	}
}
