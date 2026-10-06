using System;
using NavalDLC.CustomBattle.CustomBattle.SelectionItem;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleGameTypeSelectionGroupVM : ViewModel
{
	private readonly Action<string> _onGameTypeChange;

	private readonly Action _onPlayerSideChange;

	private SelectorVM<NavalGameTypeItemVM> _gameTypeSelection;

	private SelectorVM<NavalCustomBattlePlayerSideItemVM> _playerSideSelection;

	private string _gameTypeText;

	private string _playerSideText;

	public NavalCustomBattlePlayerSide SelectedPlayerSide { get; private set; }

	public string SelectedGameTypeStringId { get; private set; }

	[DataSourceProperty]
	public SelectorVM<NavalGameTypeItemVM> GameTypeSelection
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
	public SelectorVM<NavalCustomBattlePlayerSideItemVM> PlayerSideSelection
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

	public NavalCustomBattleGameTypeSelectionGroupVM(Action<string> onGameTypeChange, Action onPlayerSideChange)
	{
		_onGameTypeChange = onGameTypeChange;
		_onPlayerSideChange = onPlayerSideChange;
		PlayerSideSelection = new SelectorVM<NavalCustomBattlePlayerSideItemVM>(0, OnPlayerSideSelection);
		GameTypeSelection = new SelectorVM<NavalGameTypeItemVM>(0, OnGameTypeSelection);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GameTypeText = new TextObject("{=JPimShCw}Game Type").ToString();
		PlayerSideText = new TextObject("{=P3rMg4uZ}Player Side").ToString();
		GameTypeSelection.ItemList.Clear();
		PlayerSideSelection.ItemList.Clear();
		foreach (Tuple<string, string> gameType in NavalCustomBattleData.GameTypes)
		{
			GameTypeSelection.AddItem(new NavalGameTypeItemVM(gameType.Item1, gameType.Item2));
		}
		foreach (Tuple<string, NavalCustomBattlePlayerSide> playerSide in NavalCustomBattleData.PlayerSides)
		{
			PlayerSideSelection.AddItem(new NavalCustomBattlePlayerSideItemVM(playerSide.Item1, playerSide.Item2));
		}
		GameTypeSelection.SelectedIndex = 0;
		PlayerSideSelection.SelectedIndex = 0;
	}

	public void RandomizeAll()
	{
		GameTypeSelection.ExecuteRandomize();
		PlayerSideSelection.ExecuteRandomize();
	}

	private void OnGameTypeSelection(SelectorVM<NavalGameTypeItemVM> selector)
	{
		SelectedGameTypeStringId = selector.SelectedItem.GameTypeStringId;
		_onGameTypeChange(SelectedGameTypeStringId);
	}

	private void OnPlayerSideSelection(SelectorVM<NavalCustomBattlePlayerSideItemVM> selector)
	{
		SelectedPlayerSide = selector.SelectedItem.PlayerSide;
		_onPlayerSideChange?.Invoke();
	}
}
