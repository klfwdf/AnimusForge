using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyPlayerRecentGameDataVM : ViewModel
{
	private int _result;

	private string _gameType;

	private string _map;

	private string _date;

	[DataSourceProperty]
	public int Result
	{
		get
		{
			return _result;
		}
		set
		{
			if (value != _result)
			{
				_result = value;
				OnPropertyChangedWithValue(value, "Result");
			}
		}
	}

	[DataSourceProperty]
	public string GameType
	{
		get
		{
			return _gameType;
		}
		set
		{
			if (value != _gameType)
			{
				_gameType = value;
				OnPropertyChangedWithValue(value, "GameType");
			}
		}
	}

	[DataSourceProperty]
	public string Map
	{
		get
		{
			return _map;
		}
		set
		{
			if (value != _map)
			{
				_map = value;
				OnPropertyChangedWithValue(value, "Map");
			}
		}
	}

	[DataSourceProperty]
	public string Date
	{
		get
		{
			return _date;
		}
		set
		{
			if (value != _date)
			{
				_date = value;
				OnPropertyChangedWithValue(value, "Date");
			}
		}
	}

	public MPLobbyPlayerRecentGameDataVM(int result, string gameType, string map, string date)
	{
		Result = result;
		GameType = gameType;
		Map = map;
		Date = date;
	}
}
