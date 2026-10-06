using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyPlayerStatItemVM : ViewModel
{
	public readonly string GameMode;

	private readonly TextObject _descriptionText;

	private string _description;

	private string _value;

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
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

	public MPLobbyPlayerStatItemVM(string gameMode, TextObject description, string value)
	{
		GameMode = gameMode;
		_descriptionText = description;
		Value = value;
		RefreshValues();
	}

	public MPLobbyPlayerStatItemVM(string gameMode, TextObject description, float value)
		: this(gameMode, description, value.ToString("0.00"))
	{
	}

	public MPLobbyPlayerStatItemVM(string gameMode, TextObject description, int value)
		: this(gameMode, description, value.ToString())
	{
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = _descriptionText?.ToString() ?? "";
	}
}
