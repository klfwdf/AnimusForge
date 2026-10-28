using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class BooleanOptionDataVM : GenericOptionDataVM
{
	private bool _initialValue;

	private readonly IBooleanOptionData _booleanOptionData;

	private bool _optionValue;

	[DataSourceProperty]
	public bool OptionValueAsBoolean
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
				OnPropertyChangedWithValue(value, "OptionValueAsBoolean");
				UpdateValue();
			}
		}
	}

	public BooleanOptionDataVM(OptionsVM optionsVM, IBooleanOptionData option, TextObject name, TextObject description)
		: base(optionsVM, option, name, description, OptionsVM.OptionsDataType.BooleanOption)
	{
		_booleanOptionData = option;
		_initialValue = option.GetValue(forceRefresh: false).Equals(1f);
		OptionValueAsBoolean = _initialValue;
	}

	public override void UpdateValue()
	{
		Option.SetValue(OptionValueAsBoolean ? 1 : 0);
		Option.Commit();
		_optionsVM.SetConfig(Option, OptionValueAsBoolean ? 1 : 0);
	}

	public override void Cancel()
	{
		OptionValueAsBoolean = _initialValue;
		UpdateValue();
	}

	public override void SetValue(float value)
	{
		OptionValueAsBoolean = (int)value == 1;
	}

	public override void ResetData()
	{
		OptionValueAsBoolean = (int)Option.GetDefaultValue() == 1;
	}

	public override bool IsChanged()
	{
		return _initialValue != OptionValueAsBoolean;
	}

	public override void ApplyValue()
	{
		if (_initialValue != OptionValueAsBoolean)
		{
			_initialValue = OptionValueAsBoolean;
		}
	}
}
