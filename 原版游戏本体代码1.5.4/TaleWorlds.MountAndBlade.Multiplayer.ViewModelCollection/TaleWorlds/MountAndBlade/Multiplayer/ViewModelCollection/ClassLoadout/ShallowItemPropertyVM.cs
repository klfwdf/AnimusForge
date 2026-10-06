using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class ShallowItemPropertyVM : ViewModel
{
	private readonly TextObject _propertyName;

	private string _nameText;

	private int _permille;

	private int _value;

	[DataSourceProperty]
	public int Value
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
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public int Permille
	{
		get
		{
			return _permille;
		}
		set
		{
			if (value != _permille)
			{
				_permille = value;
				OnPropertyChangedWithValue(value, "Permille");
			}
		}
	}

	public ShallowItemPropertyVM(TextObject propertyName, int permille, int value)
	{
		_propertyName = propertyName;
		Permille = permille;
		Value = value;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = _propertyName.ToString();
	}
}
