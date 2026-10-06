using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.SaveLoad;

public class SavedGamePropertyVM : ViewModel
{
	public enum SavedGameProperty
	{
		None = -1,
		Health,
		Gold,
		Influence,
		PartySize,
		Food,
		Fiefs,
		Ships
	}

	private TextObject _valueText;

	private HintViewModel _hint;

	private string _propertyType;

	private string _value;

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

	[DataSourceProperty]
	public string PropertyType
	{
		get
		{
			return _propertyType;
		}
		set
		{
			if (value != _propertyType)
			{
				_propertyType = value;
				OnPropertyChangedWithValue(value, "PropertyType");
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

	public SavedGamePropertyVM(SavedGameProperty type, TextObject value, TextObject hint)
	{
		PropertyType = type.ToString();
		_valueText = value;
		Hint = new HintViewModel(hint);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Value = _valueText.ToString();
	}
}
