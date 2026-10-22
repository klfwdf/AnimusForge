using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public class NumericHostGameOptionDataVM : GenericHostGameOptionDataVM
{
	private int _value;

	private int _min;

	private int _max;

	private int _step;

	private bool _isTeamPair;

	private NumericHostGameOptionDataVM _team2;

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
				NotifyValueChanged();
			}
		}
	}

	[DataSourceProperty]
	public string ValueAsString => base.Presentation.FormatValue(_value);

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

	[DataSourceProperty]
	public int Step
	{
		get
		{
			return _step;
		}
		set
		{
			if (value != _step)
			{
				_step = value;
				OnPropertyChangedWithValue(value, "Step");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTeamPair
	{
		get
		{
			return _isTeamPair;
		}
		set
		{
			if (value != _isTeamPair)
			{
				_isTeamPair = value;
				OnPropertyChangedWithValue(value, "IsTeamPair");
			}
		}
	}

	[DataSourceProperty]
	public NumericHostGameOptionDataVM Team2
	{
		get
		{
			return _team2;
		}
		set
		{
			if (value != _team2)
			{
				_team2 = value;
				OnPropertyChangedWithValue(value, "Team2");
			}
		}
	}

	public NumericHostGameOptionDataVM(MultiplayerOptions.OptionType optionType, int preferredIndex)
		: base(OptionsVM.OptionsDataType.NumericOption, optionType, preferredIndex)
	{
		MultiplayerOptionsProperty optionProperty = base.OptionType.GetOptionProperty();
		Min = optionProperty.BoundsMin;
		Max = optionProperty.BoundsMax;
		Step = base.Presentation.SliderStep;
		if (base.Presentation.PairedTeam2Option.HasValue)
		{
			Team2 = new NumericHostGameOptionDataVM(base.Presentation.PairedTeam2Option.Value, preferredIndex);
			Team2.OnValueChanged = delegate
			{
				NotifyValueChanged();
			};
			IsTeamPair = true;
		}
		RefreshData();
	}

	public override void RefreshData()
	{
		Value = base.OptionType.GetIntValue();
		Team2?.RefreshData();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Team2?.RefreshValues();
		OnPropertyChanged("ValueAsString");
	}
}
