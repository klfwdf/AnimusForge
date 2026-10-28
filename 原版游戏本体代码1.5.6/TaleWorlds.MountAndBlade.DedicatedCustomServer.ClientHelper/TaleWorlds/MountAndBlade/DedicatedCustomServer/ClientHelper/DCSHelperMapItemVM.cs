using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.ClientHelper;

public class DCSHelperMapItemVM : ViewModel
{
	private readonly Action<DCSHelperMapItemVM> _onSelection;

	private readonly UniqueSceneId _identifiers;

	private bool _isSelected;

	private bool _existsLocally;

	private bool _isCautionSpriteVisible;

	private bool _currentlyPlaying;

	private string _currentlyPlayingText;

	private string _mapName;

	private string _mapPathClean;

	private BasicTooltipViewModel _localMapHint;

	[DataSourceProperty]
	public string ExclamationMark => "!";

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool ExistsLocally
	{
		get
		{
			return _existsLocally;
		}
		set
		{
			if (value != _existsLocally)
			{
				_existsLocally = value;
				LocalMapHint?.SetToolipCallback(_existsLocally ? new Func<List<TooltipProperty>>(GetTooltipProperties) : null);
				OnPropertyChangedWithValue(value, "ExistsLocally");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCautionSpriteVisible
	{
		get
		{
			return _isCautionSpriteVisible;
		}
		set
		{
			if (value != _isCautionSpriteVisible)
			{
				_isCautionSpriteVisible = value;
				OnPropertyChangedWithValue(value, "IsCautionSpriteVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool CurrentlyPlaying
	{
		get
		{
			return _currentlyPlaying;
		}
		set
		{
			if (value != _currentlyPlaying)
			{
				_currentlyPlaying = value;
				OnPropertyChangedWithValue(value, "CurrentlyPlaying");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentlyPlayingText
	{
		get
		{
			return _currentlyPlayingText;
		}
		set
		{
			if (value != _currentlyPlayingText)
			{
				_currentlyPlayingText = value;
				OnPropertyChangedWithValue(value, "CurrentlyPlayingText");
			}
		}
	}

	[DataSourceProperty]
	public string MapName
	{
		get
		{
			return _mapName;
		}
		set
		{
			if (value != _mapName)
			{
				_mapName = value;
				OnPropertyChangedWithValue(value, "MapName");
			}
		}
	}

	[DataSourceProperty]
	public string MapPathClean
	{
		get
		{
			return _mapPathClean;
		}
		set
		{
			if (value != _mapPathClean)
			{
				_mapPathClean = value;
				OnPropertyChangedWithValue(value, "MapPathClean");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel LocalMapHint
	{
		get
		{
			return _localMapHint;
		}
		set
		{
			if (value != _localMapHint)
			{
				_localMapHint = value;
				OnPropertyChangedWithValue(value, "LocalMapHint");
			}
		}
	}

	public DCSHelperMapItemVM(string mapName, Action<DCSHelperMapItemVM> onSelection, bool currentlyPlaying, UniqueSceneId identifiers)
	{
		_mapName = mapName;
		_onSelection = onSelection;
		_currentlyPlaying = currentlyPlaying;
		_currentlyPlayingText = new TextObject("{=fy9RJLYf}(Currently playing)").ToString();
		_identifiers = identifiers;
		LocalMapHint = new BasicTooltipViewModel();
	}

	public void ExecuteToggleSelection()
	{
		_onSelection?.Invoke(this);
	}

	public void RefreshLocalMapData()
	{
		if (Utilities.TryGetFullFilePathOfScene(MapName, out var fullPath))
		{
			IsSelected = false;
			MapPathClean = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(fullPath));
			ExistsLocally = true;
			IsCautionSpriteVisible = GetIsCautionSpriteVisible(fullPath);
		}
		else
		{
			MapPathClean = null;
			ExistsLocally = false;
		}
	}

	private bool GetIsCautionSpriteVisible(string existingLocalMapPath)
	{
		if (_identifiers != null && Utilities.TryGetUniqueIdentifiersForSceneFile(existingLocalMapPath, out var identifiers))
		{
			if (!(_identifiers.Revision != identifiers.Revision))
			{
				return _identifiers.UniqueToken != identifiers.UniqueToken;
			}
			return true;
		}
		return true;
	}

	private List<TooltipProperty> GetTooltipProperties()
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (IsCautionSpriteVisible)
		{
			list.Add(new TooltipProperty("", new TextObject("{=maLeU9XO}The map played on the server may not be identical to the local version.").ToString(), 0));
			list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
		}
		if (ExistsLocally)
		{
			list.Add(new TooltipProperty("", new TextObject("{=E8bDYaJq}This map already exists at {MAP_PATH}").SetTextVariable("MAP_PATH", MapPathClean).ToString(), 0));
		}
		return list;
	}
}
