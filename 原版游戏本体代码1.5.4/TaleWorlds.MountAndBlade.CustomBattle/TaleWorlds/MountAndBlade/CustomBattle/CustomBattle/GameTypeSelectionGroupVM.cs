using System;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

public class GameTypeSelectionGroupVM : ViewModel
{
	private readonly Action<CustomBattlePlayerType> _onPlayerTypeChange;

	private readonly Action<string> _onGameTypeChange;

	private SelectorVM<GameTypeItemVM> _gameTypeSelection;

	private SelectorVM<PlayerTypeItemVM> _playerTypeSelection;

	private SelectorVM<PlayerSideItemVM> _playerSideSelection;

	private string _gameTypeText;

	private string _playerTypeText;

	private string _playerSideText;

	public string SelectedGameTypeString { get; private set; }

	public CustomBattlePlayerType SelectedPlayerType { get; private set; }

	public CustomBattlePlayerSide SelectedPlayerSide { get; private set; }

	[DataSourceProperty]
	public SelectorVM<GameTypeItemVM> GameTypeSelection
	{
		get
		{
			return _gameTypeSelection;
		}
		set
		{
			if (value != _gameTypeSelection)
			{
				_gameTypeSelection = value;
				OnPropertyChangedWithValue(value, "GameTypeSelection");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<PlayerTypeItemVM> PlayerTypeSelection
	{
		get
		{
			return _playerTypeSelection;
		}
		set
		{
			if (value != _playerTypeSelection)
			{
				_playerTypeSelection = value;
				OnPropertyChangedWithValue(value, "PlayerTypeSelection");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<PlayerSideItemVM> PlayerSideSelection
	{
		get
		{
			return _playerSideSelection;
		}
		set
		{
			if (value != _playerSideSelection)
			{
				_playerSideSelection = value;
				OnPropertyChangedWithValue(value, "PlayerSideSelection");
			}
		}
	}

	[DataSourceProperty]
	public string GameTypeText
	{
		get
		{
			return _gameTypeText;
		}
		set
		{
			if (value != _gameTypeText)
			{
				_gameTypeText = value;
				OnPropertyChangedWithValue(value, "GameTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerTypeText
	{
		get
		{
			return _playerTypeText;
		}
		set
		{
			if (value != _playerTypeText)
			{
				_playerTypeText = value;
				OnPropertyChangedWithValue(value, "PlayerTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerSideText
	{
		get
		{
			return _playerSideText;
		}
		set
		{
			if (value != _playerSideText)
			{
				_playerSideText = value;
				OnPropertyChangedWithValue(value, "PlayerSideText");
			}
		}
	}

	public GameTypeSelectionGroupVM(Action<CustomBattlePlayerType> onPlayerTypeChange, Action<string> onGameTypeChange)
	{
		_onPlayerTypeChange = onPlayerTypeChange;
		_onGameTypeChange = onGameTypeChange;
		GameTypeSelection = new SelectorVM<GameTypeItemVM>(0, OnGameTypeSelection);
		PlayerTypeSelection = new SelectorVM<PlayerTypeItemVM>(0, OnPlayerTypeSelection);
		PlayerSideSelection = new SelectorVM<PlayerSideItemVM>(0, OnPlayerSideSelection);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GameTypeText = new TextObject("{=JPimShCw}Game Type").ToString();
		PlayerTypeText = new TextObject("{=bKg8Mmwb}Player Type").ToString();
		PlayerSideText = new TextObject("{=P3rMg4uZ}Player Side").ToString();
		GameTypeSelection.ItemList.Clear();
		PlayerTypeSelection.ItemList.Clear();
		PlayerSideSelection.ItemList.Clear();
		foreach (Tuple<string, string> gameType in CustomBattleData.GameTypes)
		{
			GameTypeSelection.AddItem(new GameTypeItemVM(gameType.Item1, gameType.Item2));
		}
		foreach (Tuple<string, CustomBattlePlayerType> playerType in CustomBattleData.PlayerTypes)
		{
			PlayerTypeSelection.AddItem(new PlayerTypeItemVM(playerType.Item1, playerType.Item2));
		}
		foreach (Tuple<string, CustomBattlePlayerSide> playerSide in CustomBattleData.PlayerSides)
		{
			PlayerSideSelection.AddItem(new PlayerSideItemVM(playerSide.Item1, playerSide.Item2));
		}
		GameTypeSelection.SelectedIndex = 0;
		PlayerTypeSelection.SelectedIndex = 0;
		PlayerSideSelection.SelectedIndex = 0;
	}

	public void RandomizeAll()
	{
		GameTypeSelection.ExecuteRandomize();
		PlayerTypeSelection.ExecuteRandomize();
		PlayerSideSelection.ExecuteRandomize();
	}

	private void OnGameTypeSelection(SelectorVM<GameTypeItemVM> selector)
	{
		SelectedGameTypeString = selector.SelectedItem.GameTypeStringId;
		_onGameTypeChange(SelectedGameTypeString);
	}

	private void OnPlayerTypeSelection(SelectorVM<PlayerTypeItemVM> selector)
	{
		SelectedPlayerType = selector.SelectedItem.PlayerType;
		_onPlayerTypeChange(SelectedPlayerType);
	}

	private void OnPlayerSideSelection(SelectorVM<PlayerSideItemVM> selector)
	{
		SelectedPlayerSide = selector.SelectedItem.PlayerSide;
	}
}
