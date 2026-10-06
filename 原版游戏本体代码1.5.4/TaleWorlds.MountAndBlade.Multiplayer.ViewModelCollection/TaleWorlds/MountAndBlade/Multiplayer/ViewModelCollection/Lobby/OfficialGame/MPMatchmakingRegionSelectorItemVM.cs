using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.OfficialGame;

public class MPMatchmakingRegionSelectorItemVM : SelectorItemVM
{
	private bool _isRegionNone;

	public string RegionCode { get; private set; }

	[DataSourceProperty]
	public bool IsRegionNone
	{
		get
		{
			return _isRegionNone;
		}
		set
		{
			if (value != _isRegionNone)
			{
				_isRegionNone = value;
				OnPropertyChangedWithValue(value, "IsRegionNone");
			}
		}
	}

	public MPMatchmakingRegionSelectorItemVM(string regionCode, TextObject regionName)
		: base(regionName)
	{
		RegionCode = regionCode;
		IsRegionNone = regionCode == "None";
	}
}
