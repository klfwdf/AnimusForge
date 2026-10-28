using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip;

public class RundownLineVM : ViewModel
{
	private string _name;

	private string _valueAsString;

	private float _value;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string ValueAsString
	{
		get
		{
			return _valueAsString;
		}
		set
		{
			if (value != _valueAsString)
			{
				_valueAsString = value;
				OnPropertyChangedWithValue(value, "ValueAsString");
			}
		}
	}

	[DataSourceProperty]
	public float Value
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

	public RundownLineVM(string name, float value)
	{
		Name = name;
		ValueAsString = $"{value:0.##}";
		Value = value;
	}
}
