using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattleMapItemVM : SelectorItemVM
{
	private string _searchedText;

	public string _nameText;

	public string MapName { get; private set; }

	public string MapId { get; private set; }

	public TerrainType Terrain { get; private set; }

	public string ForcedSceneLevel { get; private set; }

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (_nameText != value)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	public NavalCustomBattleMapItemVM(string mapName, string mapId, TerrainType terrain, string forcedSceneLevel)
		: base(mapName)
	{
		MapName = mapName;
		MapId = mapId;
		NameText = mapName;
		Terrain = terrain;
		ForcedSceneLevel = forcedSceneLevel;
	}

	public void UpdateSearchedText(string searchedText)
	{
		_searchedText = searchedText;
		string text = null;
		if (MapName.IndexOf(_searchedText, StringComparison.OrdinalIgnoreCase) != -1)
		{
			text = MapName.Substring(MapName.IndexOf(_searchedText, StringComparison.OrdinalIgnoreCase), _searchedText.Length);
		}
		if (!string.IsNullOrEmpty(text))
		{
			NameText = MapName.Replace(text, "<a>" + text + "</a>");
		}
		else
		{
			NameText = MapName;
		}
	}
}
