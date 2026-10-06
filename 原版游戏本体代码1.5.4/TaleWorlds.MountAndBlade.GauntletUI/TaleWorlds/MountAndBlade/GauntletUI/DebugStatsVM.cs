using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI;

internal class DebugStatsVM : ViewModel
{
	private string _gameVersion;

	[DataSourceProperty]
	public string GameVersion
	{
		get
		{
			return _gameVersion;
		}
		set
		{
			if (value != _gameVersion)
			{
				_gameVersion = value;
				OnPropertyChangedWithValue(value, "GameVersion");
			}
		}
	}

	public DebugStatsVM()
	{
		GameVersion = ApplicationVersion.FromParametersFile().ToString();
	}
}
