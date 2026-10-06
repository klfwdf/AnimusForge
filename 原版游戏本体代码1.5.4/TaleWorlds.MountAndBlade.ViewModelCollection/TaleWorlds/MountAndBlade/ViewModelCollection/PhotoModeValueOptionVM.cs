using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public class PhotoModeValueOptionVM : ViewModel
{
	private readonly Action<float> _onChange;

	private readonly TextObject _valueNameTextObj;

	private float _minValue;

	private float _maxValue;

	private float _currentValue;

	private string _currentValueText;

	private string _valueName;

	[DataSourceProperty]
	public float MinValue
	{
		get
		{
			return _minValue;
		}
		set
		{
			if (value != _minValue)
			{
				_minValue = value;
				OnPropertyChangedWithValue(value, "MinValue");
			}
		}
	}

	[DataSourceProperty]
	public float MaxValue
	{
		get
		{
			return _maxValue;
		}
		set
		{
			if (value != _maxValue)
			{
				_maxValue = value;
				OnPropertyChangedWithValue(value, "MaxValue");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentValue
	{
		get
		{
			return _currentValue;
		}
		set
		{
			if (value != _currentValue)
			{
				_currentValue = value;
				OnPropertyChangedWithValue(value, "CurrentValue");
				_onChange?.Invoke(value);
				CurrentValueText = value.ToString("0.0");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentValueText
	{
		get
		{
			return _currentValueText;
		}
		set
		{
			if (value != _currentValueText)
			{
				_currentValueText = value;
				OnPropertyChangedWithValue(value, "CurrentValueText");
			}
		}
	}

	[DataSourceProperty]
	public string ValueName
	{
		get
		{
			return _valueName;
		}
		set
		{
			if (value != _valueName)
			{
				_valueName = value;
				OnPropertyChangedWithValue(value, "ValueName");
			}
		}
	}

	public PhotoModeValueOptionVM(TextObject valueNameTextObj, float min, float max, float currentValue, Action<float> onChange)
	{
		MinValue = min;
		MaxValue = max;
		_valueNameTextObj = valueNameTextObj;
		_onChange = onChange;
		_currentValue = currentValue;
		CurrentValueText = currentValue.ToString("0.0");
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ValueName = _valueNameTextObj.ToString();
	}
}
