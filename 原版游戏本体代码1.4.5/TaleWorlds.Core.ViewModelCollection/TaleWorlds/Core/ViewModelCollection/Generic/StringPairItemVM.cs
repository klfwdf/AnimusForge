using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic;

public class StringPairItemVM : ViewModel
{
	private string _definition;

	private string _value;

	private BasicTooltipViewModel _hint;

	[DataSourceProperty]
	public string Definition
	{
		get
		{
			return _definition;
		}
		set
		{
			if (value != _definition)
			{
				_definition = value;
				OnPropertyChangedWithValue(value, "Definition");
			}
		}
	}

	[DataSourceProperty]
	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value != _value)
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
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

	public StringPairItemVM(string definition, string value, BasicTooltipViewModel hint = null)
	{
		Definition = definition;
		Value = value;
		Hint = hint;
	}
}
