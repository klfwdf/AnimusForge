using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;

public class MultiplayerAdminPanelNumericOptionVM : MultiplayerAdminPanelOptionBaseVM
{
	private int? _minValue;

	private int? _maxValue;

	private new readonly IAdminPanelNumericOption _option;

	private bool _isNumericOption;

	private int _intValue;

	[DataSourceProperty]
	public bool IsNumericOption
	{
		get
		{
			return _isNumericOption;
		}
		set
		{
			if (value != _isNumericOption)
			{
				_isNumericOption = value;
				OnPropertyChangedWithValue(value, "IsNumericOption");
			}
		}
	}

	[DataSourceProperty]
	public int IntValue
	{
		get
		{
			return _intValue;
		}
		set
		{
			if (value != _intValue)
			{
				value = GetClampedInt(value);
				_intValue = value;
				OnPropertyChangedWithValue(value, "IntValue");
				_option.SetValue(value);
			}
		}
	}

	public int MinValueInt
	{
		get
		{
			if (!_minValue.HasValue)
			{
				return int.MinValue;
			}
			return _minValue.Value;
		}
		set
		{
			if (value != _minValue)
			{
				_minValue = value;
				OnPropertyChangedWithValue(value, "MinValueInt");
			}
		}
	}

	public int MaxValueInt
	{
		get
		{
			if (!_maxValue.HasValue)
			{
				return int.MaxValue;
			}
			return _maxValue.Value;
		}
		set
		{
			if (value != _maxValue)
			{
				_maxValue = value;
				OnPropertyChangedWithValue(value, "MaxValueInt");
			}
		}
	}

	public MultiplayerAdminPanelNumericOptionVM(IAdminPanelNumericOption option)
		: base(option)
	{
		_option = option;
		_minValue = _option.GetMinimumValue();
		_maxValue = _option.GetMaximumValue();
		IntValue = _option.GetValue();
		IsNumericOption = true;
	}

	public override void UpdateValues()
	{
		base.UpdateValues();
		IntValue = _option.GetValue();
	}

	private int GetClampedInt(int value)
	{
		if (_minValue.HasValue)
		{
			value = MathF.Max(value, _minValue.Value);
		}
		if (_maxValue.HasValue)
		{
			value = MathF.Min(value, _maxValue.Value);
		}
		return value;
	}
}
