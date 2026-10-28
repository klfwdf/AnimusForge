using System;
using System.Collections.Generic;
using NavalDLC.CustomBattle.CustomBattle.SelectionItem;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleMapSelectionGroupVM : ViewModel
{
	private List<NavalCustomBattleMapItemVM> _customNavalBattleMaps;

	private List<NavalCustomBattleMapItemVM> _customNavalRaidMaps;

	private List<NavalCustomBattleMapItemVM> _availableMaps;

	private SelectorVM<NavalCustomBattleMapItemVM> _mapSelection;

	private SelectorVM<NavalCustomBattleSeasonItemVM> _seasonSelection;

	private SelectorVM<NavalCustomBattleTimeOfDayItemVM> _timeOfDaySelection;

	private SelectorVM<NavalCustomBattleWindStrengthItemVM> _windStrengthSelection;

	private SelectorVM<NavalCustomBattleWindDirectionItemVM> _windDirectionSelection;

	private string _titleText;

	private string _mapText;

	private string _seasonText;

	private string _timeOfDayText;

	private string _windStrengthText;

	private string _windDirectionText;

	private bool _isRaid;

	public int SelectedTimeOfDay { get; private set; }

	public float SelectedWindStrength { get; private set; }

	public NavalCustomBattleWindConfig.Direction SelectedWindDirection { get; private set; }

	public string SelectedSeasonId { get; private set; }

	public NavalCustomBattleMapItemVM SelectedMap { get; private set; }

	[DataSourceProperty]
	public SelectorVM<NavalCustomBattleMapItemVM> MapSelection
	{
		get
		{
			return _mapSelection;
		}
		set
		{
			if (value != _mapSelection)
			{
				_mapSelection = value;
				OnPropertyChangedWithValue(value, "MapSelection");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<NavalCustomBattleSeasonItemVM> SeasonSelection
	{
		get
		{
			return _seasonSelection;
		}
		set
		{
			if (value != _seasonSelection)
			{
				_seasonSelection = value;
				OnPropertyChangedWithValue(value, "SeasonSelection");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<NavalCustomBattleTimeOfDayItemVM> TimeOfDaySelection
	{
		get
		{
			return _timeOfDaySelection;
		}
		set
		{
			if (value != _timeOfDaySelection)
			{
				_timeOfDaySelection = value;
				OnPropertyChangedWithValue(value, "TimeOfDaySelection");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<NavalCustomBattleWindStrengthItemVM> WindStrengthSelection
	{
		get
		{
			return _windStrengthSelection;
		}
		set
		{
			if (value != _windStrengthSelection)
			{
				_windStrengthSelection = value;
				OnPropertyChangedWithValue(value, "WindStrengthSelection");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<NavalCustomBattleWindDirectionItemVM> WindDirectionSelection
	{
		get
		{
			return _windDirectionSelection;
		}
		set
		{
			if (value != _windDirectionSelection)
			{
				_windDirectionSelection = value;
				OnPropertyChangedWithValue(value, "WindDirectionSelection");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string MapText
	{
		get
		{
			return _mapText;
		}
		set
		{
			if (value != _mapText)
			{
				_mapText = value;
				OnPropertyChangedWithValue(value, "MapText");
			}
		}
	}

	[DataSourceProperty]
	public string SeasonText
	{
		get
		{
			return _seasonText;
		}
		set
		{
			if (value != _seasonText)
			{
				_seasonText = value;
				OnPropertyChangedWithValue(value, "SeasonText");
			}
		}
	}

	[DataSourceProperty]
	public string TimeOfDayText
	{
		get
		{
			return _timeOfDayText;
		}
		set
		{
			if (value != _timeOfDayText)
			{
				_timeOfDayText = value;
				OnPropertyChangedWithValue(value, "TimeOfDayText");
			}
		}
	}

	[DataSourceProperty]
	public string WindStrengthText
	{
		get
		{
			return _windStrengthText;
		}
		set
		{
			if (value != _windStrengthText)
			{
				_windStrengthText = value;
				OnPropertyChangedWithValue(value, "WindStrengthText");
			}
		}
	}

	[DataSourceProperty]
	public string WindDirectionText
	{
		get
		{
			return _windDirectionText;
		}
		set
		{
			if (value != _windDirectionText)
			{
				_windDirectionText = value;
				OnPropertyChangedWithValue(value, "WindDirectionText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRaid
	{
		get
		{
			return _isRaid;
		}
		set
		{
			if (value != _isRaid)
			{
				_isRaid = value;
				OnPropertyChangedWithValue(value, "IsRaid");
			}
		}
	}

	public NavalCustomBattleMapSelectionGroupVM()
	{
		_customNavalBattleMaps = new List<NavalCustomBattleMapItemVM>();
		_customNavalRaidMaps = new List<NavalCustomBattleMapItemVM>();
		_availableMaps = _customNavalBattleMaps;
		MapSelection = new SelectorVM<NavalCustomBattleMapItemVM>(0, OnMapSelection);
		SeasonSelection = new SelectorVM<NavalCustomBattleSeasonItemVM>(0, OnSeasonSelection);
		TimeOfDaySelection = new SelectorVM<NavalCustomBattleTimeOfDayItemVM>(0, OnTimeOfDaySelection);
		WindStrengthSelection = new SelectorVM<NavalCustomBattleWindStrengthItemVM>(0, OnWindStrengthSelection);
		WindDirectionSelection = new SelectorVM<NavalCustomBattleWindDirectionItemVM>(0, OnWindDirectionSelection);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_customNavalBattleMaps.Clear();
		_customNavalRaidMaps.Clear();
		foreach (NavalCustomBattleSceneData customNavalBattleScene in NavalCustomGame.Current.CustomNavalBattleScenes)
		{
			NavalCustomBattleMapItemVM item = new NavalCustomBattleMapItemVM(customNavalBattleScene.Name.ToString(), customNavalBattleScene.SceneID, customNavalBattleScene.Terrain, customNavalBattleScene.ForcedSceneLevel);
			_customNavalBattleMaps.Add(item);
		}
		foreach (NavalCustomBattleSceneData customNavalRaidScene in NavalCustomGame.Current.CustomNavalRaidScenes)
		{
			NavalCustomBattleMapItemVM item2 = new NavalCustomBattleMapItemVM(customNavalRaidScene.Name.ToString(), customNavalRaidScene.SceneID, customNavalRaidScene.Terrain, customNavalRaidScene.ForcedSceneLevel);
			_customNavalRaidMaps.Add(item2);
		}
		TitleText = new TextObject("{=customgametitle}Map").ToString();
		MapText = new TextObject("{=customgamemapname}Map").ToString();
		SeasonText = new TextObject("{=xTzDM5XE}Season").ToString();
		TimeOfDayText = new TextObject("{=DszSWnc3}Time of Day").ToString();
		WindStrengthText = new TextObject("{=bbwr1vdO}Wind Strength").ToString();
		WindDirectionText = new TextObject("{=CFUowjPd}Wind Direction").ToString();
		MapSelection.ItemList.Clear();
		SeasonSelection.ItemList.Clear();
		TimeOfDaySelection.ItemList.Clear();
		WindStrengthSelection.ItemList.Clear();
		WindDirectionSelection.ItemList.Clear();
		foreach (NavalCustomBattleMapItemVM availableMap in _availableMaps)
		{
			MapSelection.AddItem(new NavalCustomBattleMapItemVM(availableMap.MapName, availableMap.MapId, availableMap.Terrain, availableMap.ForcedSceneLevel));
		}
		foreach (Tuple<string, string> season in NavalCustomBattleData.Seasons)
		{
			SeasonSelection.AddItem(new NavalCustomBattleSeasonItemVM(season.Item1, season.Item2));
		}
		foreach (Tuple<string, NavalCustomBattleTimeOfDay> item3 in NavalCustomBattleData.TimesOfDay)
		{
			TimeOfDaySelection.AddItem(new NavalCustomBattleTimeOfDayItemVM(item3.Item1, (int)item3.Item2));
		}
		foreach (Tuple<string, float> windStrength in NavalCustomBattleData.WindStrengths)
		{
			WindStrengthSelection.AddItem(new NavalCustomBattleWindStrengthItemVM(windStrength.Item1, windStrength.Item2));
		}
		foreach (Tuple<string, NavalCustomBattleWindConfig.Direction> windDirection in NavalCustomBattleData.WindDirections)
		{
			WindDirectionSelection.AddItem(new NavalCustomBattleWindDirectionItemVM(windDirection.Item1, windDirection.Item2));
		}
		MapSelection.SelectedIndex = 0;
		SeasonSelection.SelectedIndex = 0;
		TimeOfDaySelection.SelectedIndex = 0;
		WindStrengthSelection.SelectedIndex = 0;
		WindDirectionSelection.SelectedIndex = 0;
	}

	private void OnMapSelection(SelectorVM<NavalCustomBattleMapItemVM> selector)
	{
		SelectedMap = selector.SelectedItem;
	}

	private void OnSeasonSelection(SelectorVM<NavalCustomBattleSeasonItemVM> selector)
	{
		SelectedSeasonId = selector.SelectedItem.SeasonId;
	}

	private void OnTimeOfDaySelection(SelectorVM<NavalCustomBattleTimeOfDayItemVM> selector)
	{
		SelectedTimeOfDay = selector.SelectedItem.TimeOfDay;
	}

	private void OnWindStrengthSelection(SelectorVM<NavalCustomBattleWindStrengthItemVM> selector)
	{
		SelectedWindStrength = selector.SelectedItem.WindStrength;
	}

	private void OnWindDirectionSelection(SelectorVM<NavalCustomBattleWindDirectionItemVM> selector)
	{
		SelectedWindDirection = selector.SelectedItem.WindDirection;
	}

	public void OnGameTypeChange(string gameTypeStringId)
	{
		if (gameTypeStringId == "NavalBattle")
		{
			_availableMaps = _customNavalBattleMaps;
		}
		else if (gameTypeStringId == "NavalRaid")
		{
			_availableMaps = _customNavalRaidMaps;
		}
		MapSelection.ItemList.Clear();
		foreach (NavalCustomBattleMapItemVM availableMap in _availableMaps)
		{
			MapSelection.AddItem(availableMap);
		}
		MapSelection.SelectedIndex = 0;
		IsRaid = gameTypeStringId == "NavalRaid";
	}

	public void RandomizeAll()
	{
		MapSelection.ExecuteRandomize();
		SeasonSelection.ExecuteRandomize();
		TimeOfDaySelection.ExecuteRandomize();
		WindStrengthSelection.ExecuteRandomize();
		WindDirectionSelection.ExecuteRandomize();
	}
}
