using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyGameTypeVM : ViewModel
{
	private readonly Action<string> _onSelection;

	public readonly bool IsCasual;

	private bool _isSelected;

	private string _gameTypeID;

	private HintViewModel _hint;

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
				if (value)
				{
					OnSelected();
				}
			}
		}
	}

	[DataSourceProperty]
	public string GameTypeID
	{
		get
		{
			return _gameTypeID;
		}
		set
		{
			if (value != _gameTypeID)
			{
				_gameTypeID = value;
				OnPropertyChangedWithValue(value, "GameTypeID");
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

	public MPLobbyGameTypeVM(string gameType, bool isCasual, Action<string> onSelection)
	{
		GameTypeID = gameType;
		IsCasual = isCasual;
		_onSelection = onSelection;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Hint = new HintViewModel(GameTexts.FindText("str_multiplayer_game_stats_description", GameTypeID));
	}

	private void OnSelected()
	{
		_onSelection?.Invoke(GameTypeID);
	}
}
