using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public class NumericHostGameOptionDataVM : GenericHostGameOptionDataVM
{
	private int _value;

	private int _min;

	private int _max;

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
				OnPropertyChanged("ValueAsString");
				base.OptionType.SetValue(value);
			}
		}
	}

	[DataSourceProperty]
	public string ValueAsString => _value.ToString();

	[DataSourceProperty]
	public int Min
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
	public int Max
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

	public NumericHostGameOptionDataVM(MultiplayerOptions.OptionType optionType, int preferredIndex)
		: base(OptionsVM.OptionsDataType.NumericOption, optionType, preferredIndex)
	{
		RefreshData();
	}

	public override void RefreshData()
	{
		MultiplayerOptionsProperty optionProperty = base.OptionType.GetOptionProperty();
		Min = optionProperty.BoundsMin;
		Max = optionProperty.BoundsMax;
		Value = base.OptionType.GetIntValue();
	}
}
