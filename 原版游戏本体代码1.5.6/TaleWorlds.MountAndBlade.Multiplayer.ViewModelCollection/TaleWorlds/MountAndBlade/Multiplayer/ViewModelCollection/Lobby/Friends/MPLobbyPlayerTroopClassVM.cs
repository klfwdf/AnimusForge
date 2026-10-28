using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyPlayerTroopClassVM : ViewModel
{
	private string _name;

	private CharacterImageIdentifierVM _preview;

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
	public CharacterImageIdentifierVM Preview
	{
		get
		{
			return _preview;
		}
		set
		{
			if (value != _preview)
			{
				_preview = value;
				OnPropertyChangedWithValue(value, "Preview");
			}
		}
	}

	public MPLobbyPlayerTroopClassVM()
	{
		Name = "Varangian Guard";
		Preview = new CharacterImageIdentifierVM(null);
	}
}
