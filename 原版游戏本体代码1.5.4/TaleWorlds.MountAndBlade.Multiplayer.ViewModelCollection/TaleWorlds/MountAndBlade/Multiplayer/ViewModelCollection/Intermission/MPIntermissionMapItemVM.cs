using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Intermission;

public class MPIntermissionMapItemVM : ViewModel
{
	private readonly Action<MPIntermissionMapItemVM> _onPlayerVoted;

	private bool _isSelected;

	private string _mapID;

	private string _mapName;

	private int _votes;

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
	public string MapID
	{
		get
		{
			return _mapID;
		}
		set
		{
			if (value != _mapID)
			{
				_mapID = value;
				OnPropertyChangedWithValue(value, "MapID");
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
	public int Votes
	{
		get
		{
			return _votes;
		}
		set
		{
			if (value != _votes)
			{
				_votes = value;
				OnPropertyChangedWithValue(value, "Votes");
			}
		}
	}

	public MPIntermissionMapItemVM(string mapID, Action<MPIntermissionMapItemVM> onPlayerVoted)
	{
		MapID = mapID;
		_onPlayerVoted = onPlayerVoted;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		if (GameTexts.TryGetText("str_multiplayer_scene_name", out var textObject, MapID))
		{
			MapName = textObject.ToString();
		}
		else
		{
			MapName = MapID;
		}
	}

	public void ExecuteVote()
	{
		_onPlayerVoted(this);
	}
}
