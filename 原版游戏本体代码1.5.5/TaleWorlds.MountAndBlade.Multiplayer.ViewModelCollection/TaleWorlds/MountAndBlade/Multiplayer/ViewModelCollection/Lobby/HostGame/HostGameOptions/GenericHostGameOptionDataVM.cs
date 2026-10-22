using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public abstract class GenericHostGameOptionDataVM : ViewModel
{
	private int _index;

	private int _category;

	private string _name;

	private bool _isEnabled;

	private bool _isVisible;

	private HintViewModel _hint;

	public MultiplayerOptions.OptionType OptionType { get; }

	public int PreferredIndex { get; }

	public HostGameOptionPresentation Presentation { get; }

	public Action<GenericHostGameOptionDataVM> OnValueChanged { get; set; }

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

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

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

	internal GenericHostGameOptionDataVM(OptionsVM.OptionsDataType type, MultiplayerOptions.OptionType optionType, int preferredIndex)
	{
		Category = (int)type;
		OptionType = optionType;
		PreferredIndex = preferredIndex;
		Index = preferredIndex;
		Presentation = HostGameOptionPresentation.Get(optionType);
		IsEnabled = true;
		IsVisible = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string id = (Presentation.PairedTeam2Option.HasValue ? "str_multiplayer_host_game_pair" : "str_multiplayer_option");
		Name = GameTexts.FindText(id, OptionType.ToString()).ToString();
		Hint = new HintViewModel(GetDescriptionText());
	}

	private TextObject GetDescriptionText()
	{
		if (GameTexts.TryGetText("str_multiplayer_option_description", out var textObject, OptionType.ToString()))
		{
			return textObject;
		}
		return new TextObject("{=!}" + OptionType.GetOptionProperty().Description);
	}

	public abstract void RefreshData();

	protected void NotifyValueChanged()
	{
		OnValueChanged?.Invoke(this);
	}
}
