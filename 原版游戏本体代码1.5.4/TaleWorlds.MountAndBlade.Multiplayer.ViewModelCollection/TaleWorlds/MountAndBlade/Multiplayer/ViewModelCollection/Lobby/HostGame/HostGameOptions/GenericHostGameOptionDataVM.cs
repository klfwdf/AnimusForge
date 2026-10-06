using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public abstract class GenericHostGameOptionDataVM : ViewModel
{
	private int _index;

	private int _category;

	private string _name;

	private bool _isEnabled;

	public MultiplayerOptions.OptionType OptionType { get; }

	public int PreferredIndex { get; }

	[DataSourceProperty]
	public int Index
	{
		get
		{
			return _index;
		}
		set
		{
			if (value != _index)
			{
				_index = value;
				OnPropertyChangedWithValue(value, "Index");
			}
		}
	}

	[DataSourceProperty]
	public int Category
	{
		get
		{
			return _category;
		}
		set
		{
			if (value != _category)
			{
				_category = value;
				OnPropertyChangedWithValue(value, "Category");
			}
		}
	}

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
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	internal GenericHostGameOptionDataVM(OptionsVM.OptionsDataType type, MultiplayerOptions.OptionType optionType, int preferredIndex)
	{
		Category = (int)type;
		OptionType = optionType;
		PreferredIndex = preferredIndex;
		Index = preferredIndex;
		IsEnabled = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = GameTexts.FindText("str_multiplayer_option", OptionType.ToString()).ToString();
	}

	public abstract void RefreshData();
}
