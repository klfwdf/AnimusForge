using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.SaveLoad;

public class SavedGameModuleInfoVM : ViewModel
{
	private string _definition;

	private string _seperator;

	private string _value;

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
	public string Seperator
	{
		get
		{
			return _seperator;
		}
		set
		{
			if (value != _seperator)
			{
				_seperator = value;
				OnPropertyChangedWithValue(value, "Seperator");
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

	public SavedGameModuleInfoVM(string definition, string seperator, string value)
	{
		Definition = definition;
		Seperator = seperator;
		Value = value;
	}
}
