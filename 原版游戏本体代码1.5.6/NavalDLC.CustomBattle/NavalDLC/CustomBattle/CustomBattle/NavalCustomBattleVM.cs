using System.Collections.Generic;
using System.Linq;
using NavalDLC.CustomBattle.CustomBattle.SelectionItem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.CustomBattle;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleVM : ViewModel
{
	public NavalCustomBattleShipItemVM FocusedShipItem;

	private readonly ICustomBattleProvider _nextCustomBattleProvider;

	private NavalCustomBattleTroopTypeSelectionPopUpVM _troopTypeSelectionPopUp;

	private NavalCustomBattleShipSelectionPopUpVM _shipSelectionPopUp;

	private NavalCustomBattleSideVM _enemySide;

	private NavalCustomBattleSideVM _playerSide;

	private NavalCustomBattleMapSelectionGroupVM _mapSelectionGroup;

	private NavalCustomBattleGameTypeSelectionGroupVM _gameTypeSelectionGroup;

	private string _randomizeButtonText;

	private string _backButtonText;

	private string _startButtonText;

	private string _switchButtonText;

	private string _titleText;

	private bool _canConfirm;

	private HintViewModel _confirmHint;

	private bool _canSwitchMode;

	private HintViewModel _switchHint;

	private InputKeyItemVM _startInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _randomizeInputKey;

	[DataSourceProperty]
	public NavalCustomBattleTroopTypeSelectionPopUpVM TroopTypeSelectionPopUp
	{
		get
		{
			return _troopTypeSelectionPopUp;
		}
		set
		{
			if (value != _troopTypeSelectionPopUp)
			{
				_troopTypeSelectionPopUp = value;
				OnPropertyChangedWithValue(value, "TroopTypeSelectionPopUp");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleShipSelectionPopUpVM ShipSelectionPopUp
	{
		get
		{
			return _shipSelectionPopUp;
		}
		set
		{
			if (value != _shipSelectionPopUp)
			{
				_shipSelectionPopUp = value;
				OnPropertyChangedWithValue(value, "ShipSelectionPopUp");
			}
		}
	}

	[DataSourceProperty]
	public string RandomizeButtonText
	{
		get
		{
			return _randomizeButtonText;
		}
		set
		{
			if (value != _randomizeButtonText)
			{
				_randomizeButtonText = value;
				OnPropertyChangedWithValue(value, "RandomizeButtonText");
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
	public string BackButtonText
	{
		get
		{
			return _backButtonText;
		}
		set
		{
			if (value != _backButtonText)
			{
				_backButtonText = value;
				OnPropertyChangedWithValue(value, "BackButtonText");
			}
		}
	}

	[DataSourceProperty]
	public string StartButtonText
	{
		get
		{
			return _startButtonText;
		}
		set
		{
			if (value != _startButtonText)
			{
				_startButtonText = value;
				OnPropertyChangedWithValue(value, "StartButtonText");
			}
		}
	}

	[DataSourceProperty]
	public string SwitchButtonText
	{
		get
		{
			return _switchButtonText;
		}
		set
		{
			if (value != _switchButtonText)
			{
				_switchButtonText = value;
				OnPropertyChangedWithValue(value, "SwitchButtonText");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleSideVM EnemySide
	{
		get
		{
			return _enemySide;
		}
		set
		{
			if (value != _enemySide)
			{
				_enemySide = value;
				OnPropertyChangedWithValue(value, "EnemySide");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleSideVM PlayerSide
	{
		get
		{
			return _playerSide;
		}
		set
		{
			if (value != _playerSide)
			{
				_playerSide = value;
				OnPropertyChangedWithValue(value, "PlayerSide");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleMapSelectionGroupVM MapSelectionGroup
	{
		get
		{
			return _mapSelectionGroup;
		}
		set
		{
			if (value != _mapSelectionGroup)
			{
				_mapSelectionGroup = value;
				OnPropertyChangedWithValue(value, "MapSelectionGroup");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleGameTypeSelectionGroupVM GameTypeSelectionGroup
	{
		get
		{
			return _gameTypeSelectionGroup;
		}
		set
		{
			if (value != _gameTypeSelectionGroup)
			{
				_gameTypeSelectionGroup = value;
				OnPropertyChangedWithValue(value, "GameTypeSelectionGroup");
			}
		}
	}

	[DataSourceProperty]
	public bool CanConfirm
	{
		get
		{
			return _canConfirm;
		}
		set
		{
			if (value != _canConfirm)
			{
				_canConfirm = value;
				OnPropertyChangedWithValue(value, "CanConfirm");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ConfirmHint
	{
		get
		{
			return _confirmHint;
		}
		set
		{
			if (value != _confirmHint)
			{
				_confirmHint = value;
				OnPropertyChangedWithValue(value, "ConfirmHint");
			}
		}
	}

	[DataSourceProperty]
	public bool CanSwitchMode
	{
		get
		{
			return _canSwitchMode;
		}
		set
		{
			if (value != _canSwitchMode)
			{
				_canSwitchMode = value;
				OnPropertyChangedWithValue(value, "CanSwitchMode");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SwitchHint
	{
		get
		{
			return _switchHint;
		}
		set
		{
			if (value != _switchHint)
			{
				_switchHint = value;
				OnPropertyChangedWithValue(value, "SwitchHint");
			}
		}
	}

	public InputKeyItemVM StartInputKey
	{
		get
		{
			return _startInputKey;
		}
		set
		{
			if (value != _startInputKey)
			{
				_startInputKey = value;
				OnPropertyChangedWithValue(value, "StartInputKey");
			}
		}
	}

	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
			}
		}
	}

	public InputKeyItemVM RandomizeInputKey
	{
		get
		{
			return _randomizeInputKey;
		}
		set
		{
			if (value != _randomizeInputKey)
			{
				_randomizeInputKey = value;
				OnPropertyChangedWithValue(value, "RandomizeInputKey");
			}
		}
	}

	public NavalCustomBattleVM()
	{
		TroopTypeSelectionPopUp = new NavalCustomBattleTroopTypeSelectionPopUpVM();
		ShipSelectionPopUp = new NavalCustomBattleShipSelectionPopUpVM();
		PlayerSide = new NavalCustomBattleSideVM(new TextObject("{=BC7n6qxk}PLAYER"), isPlayerSide: true, TroopTypeSelectionPopUp, ShipSelectionPopUp, OnShipFocused, UpdateCanConfirm, OnSelectedCharactersChanged);
		EnemySide = new NavalCustomBattleSideVM(new TextObject("{=35IHscBa}ENEMY"), isPlayerSide: false, TroopTypeSelectionPopUp, ShipSelectionPopUp, OnShipFocused, UpdateCanConfirm, OnSelectedCharactersChanged);
		OnSelectedCharactersChanged();
		MapSelectionGroup = new NavalCustomBattleMapSelectionGroupVM();
		GameTypeSelectionGroup = new NavalCustomBattleGameTypeSelectionGroupVM(OnGameTypeChange, UpdateIsLandSide);
		CanSwitchMode = CustomBattleFactory.GetProviderCount() > 1;
		if (CanSwitchMode)
		{
			_nextCustomBattleProvider = CustomBattleFactory.CollectNextProvider(typeof(NavalCustomBattleProvider));
			SwitchHint = new HintViewModel(new TextObject("{=Jfe53wbr}Switch to {PROVIDER_NAME}").SetTextVariable("PROVIDER_NAME", _nextCustomBattleProvider.GetName()));
		}
		ConfirmHint = new HintViewModel();
		UpdateCanConfirm();
		RefreshValues();
	}

	private static NavalCustomBattleCompositionData GetBattleCompositionDataFromCompositionGroup(NavalCustomBattleArmyCompositionGroupVM compositionGroup)
	{
		return new NavalCustomBattleCompositionData((float)compositionGroup.RangedInfantryComposition.CompositionValue / 100f, (float)compositionGroup.MeleeCavalryComposition.CompositionValue / 100f, (float)compositionGroup.RangedCavalryComposition.CompositionValue / 100f);
	}

	private static List<BasicCharacterObject>[] GetTroopSelections(NavalCustomBattleArmyCompositionGroupVM armyComposition)
	{
		return new List<BasicCharacterObject>[4]
		{
			(from x in armyComposition.MeleeInfantryComposition.TroopTypes
				where x.IsSelected
				select x.Character).ToList(),
			(from x in armyComposition.RangedInfantryComposition.TroopTypes
				where x.IsSelected
				select x.Character).ToList(),
			(from x in armyComposition.MeleeCavalryComposition.TroopTypes
				where x.IsSelected
				select x.Character).ToList(),
			(from x in armyComposition.RangedCavalryComposition.TroopTypes
				where x.IsSelected
				select x.Character).ToList()
		};
	}

	public void SetActiveState(bool isActive)
	{
		if (isActive)
		{
			EnemySide.UpdateCharacterVisual();
			PlayerSide.UpdateCharacterVisual();
		}
		else
		{
			EnemySide.CurrentSelectedCharacter = null;
			PlayerSide.CurrentSelectedCharacter = null;
		}
	}

	private void OnSelectedCharactersChanged()
	{
		if (PlayerSide?.CharacterSelectionGroup == null || EnemySide?.CharacterSelectionGroup == null)
		{
			return;
		}
		BasicCharacterObject basicCharacterObject = PlayerSide.CharacterSelectionGroup.SelectedItem?.Character;
		BasicCharacterObject basicCharacterObject2 = EnemySide.CharacterSelectionGroup.SelectedItem?.Character;
		foreach (NavalCustomBattleCharacterItemVM item in PlayerSide.CharacterSelectionGroup.ItemList)
		{
			item.CanBeSelected = item.Character != basicCharacterObject2;
		}
		foreach (NavalCustomBattleCharacterItemVM item2 in EnemySide.CharacterSelectionGroup.ItemList)
		{
			item2.CanBeSelected = item2.Character != basicCharacterObject;
		}
	}

	private void OnGameTypeChange(string gameTypeStringId)
	{
		MapSelectionGroup.OnGameTypeChange(gameTypeStringId);
		UpdateIsLandSide();
		PlayerSide?.OnGameTypeChange(gameTypeStringId);
		EnemySide?.OnGameTypeChange(gameTypeStringId);
		UpdateCanConfirm();
	}

	private void UpdateIsLandSide()
	{
		if (PlayerSide != null && EnemySide != null && GameTypeSelectionGroup != null)
		{
			if (GameTypeSelectionGroup.SelectedGameTypeStringId == "NavalRaid")
			{
				PlayerSide.IsLandSide = GameTypeSelectionGroup.SelectedPlayerSide == NavalCustomBattlePlayerSide.Defender;
				EnemySide.IsLandSide = GameTypeSelectionGroup.SelectedPlayerSide == NavalCustomBattlePlayerSide.Attacker;
			}
			else
			{
				PlayerSide.IsLandSide = false;
				EnemySide.IsLandSide = false;
			}
		}
	}

	private void UpdateCanConfirm()
	{
		if (PlayerSide == null || EnemySide == null || GameTypeSelectionGroup == null)
		{
			return;
		}
		List<string> list = new List<string>();
		if (GameTypeSelectionGroup.SelectedGameTypeStringId == "NavalRaid")
		{
			CanConfirm = (PlayerSide.IsLandSide || PlayerSide.ShipSelectionGroup.ShipSelectionItems.All((NavalCustomBattleShipSelectionItemVM x) => !x.IsRelevant || !x.HasSelectedItem || x.IsSelectedItemEligible)) && (EnemySide.IsLandSide || EnemySide.ShipSelectionGroup.ShipSelectionItems.All((NavalCustomBattleShipSelectionItemVM x) => !x.IsRelevant || !x.HasSelectedItem || x.IsSelectedItemEligible));
			if (!CanConfirm)
			{
				if (!PlayerSide.IsLandSide)
				{
					list.AddRange(from x in PlayerSide.ShipSelectionGroup.ShipSelectionItems
						where x.IsRelevant && x.HasSelectedItem && !x.IsSelectedItemEligible
						select x.SelectedItem.Name);
				}
				if (!EnemySide.IsLandSide)
				{
					list.AddRange(from x in EnemySide.ShipSelectionGroup.ShipSelectionItems
						where x.IsRelevant && x.HasSelectedItem && !x.IsSelectedItemEligible
						select x.SelectedItem.Name);
				}
				list = list.Distinct().ToList();
			}
		}
		else
		{
			CanConfirm = true;
		}
		ConfirmHint.HintText = (CanConfirm ? null : new TextObject("{=MC7KdXJm}Following ship types are not eligible for the selected game mode: {INELIGIBLE_SHIPS}").SetTextVariable("INELIGIBLE_SHIPS", string.Join(", ", list)));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RandomizeButtonText = GameTexts.FindText("str_randomize").ToString();
		StartButtonText = GameTexts.FindText("str_start").ToString();
		BackButtonText = GameTexts.FindText("str_back").ToString();
		SwitchButtonText = GameTexts.FindText("str_switch").ToString();
		TitleText = GameTexts.FindText("str_naval_custom_battle").ToString();
		EnemySide.RefreshValues();
		PlayerSide.RefreshValues();
		MapSelectionGroup.RefreshValues();
		GameTypeSelectionGroup.RefreshValues();
		TroopTypeSelectionPopUp?.RefreshValues();
		ShipSelectionPopUp?.RefreshValues();
	}

	public void ExecuteBack()
	{
		Game.Current.GameStateManager.PopState();
	}

	private NavalCustomBattleData PrepareBattleData()
	{
		BasicCharacterObject selectedCharacter = PlayerSide.SelectedCharacter;
		BasicCharacterObject selectedCharacter2 = EnemySide.SelectedCharacter;
		int armySize = PlayerSide.CompositionGroup.ArmySize;
		int armySize2 = EnemySide.CompositionGroup.ArmySize;
		bool isPlayerAttacker = GameTypeSelectionGroup.SelectedPlayerSide == NavalCustomBattlePlayerSide.Attacker;
		BasicCultureObject faction = PlayerSide.FactionSelectionGroup.SelectedItem.Faction;
		BasicCultureObject faction2 = EnemySide.FactionSelectionGroup.SelectedItem.Faction;
		List<IShipOrigin>[] customBattleShipLists = NavalCustomBattleHelper.GetCustomBattleShipLists(PlayerSide.IsLandSide ? new List<IShipOrigin>() : PlayerSide.ShipSelectionGroup.GetSelectedShips(), EnemySide.IsLandSide ? new List<IShipOrigin>() : EnemySide.ShipSelectionGroup.GetSelectedShips());
		int num = (PlayerSide.IsLandSide ? 1 : customBattleShipLists[0].Count);
		int num2 = (EnemySide.IsLandSide ? 1 : customBattleShipLists[1].Count);
		int[] troopCounts = NavalCustomBattleHelper.GetTroopCounts(armySize, num, GetBattleCompositionDataFromCompositionGroup(PlayerSide.CompositionGroup));
		int[] troopCounts2 = NavalCustomBattleHelper.GetTroopCounts(armySize2, num2, GetBattleCompositionDataFromCompositionGroup(EnemySide.CompositionGroup));
		List<BasicCharacterObject>[] troopSelections = GetTroopSelections(PlayerSide.CompositionGroup);
		List<BasicCharacterObject>[] troopSelections2 = GetTroopSelections(EnemySide.CompositionGroup);
		List<BasicCharacterObject> list = new List<BasicCharacterObject>();
		foreach (BasicCharacterObject character in NavalCustomBattleData.Characters)
		{
			if (character != selectedCharacter && character != selectedCharacter2)
			{
				list.Add(character);
			}
		}
		CustomBattleCombatant[] customBattleParties = NavalCustomBattleHelper.GetCustomBattleParties(selectedCharacter, selectedCharacter2, list, faction, troopCounts, troopSelections, num, faction2, troopCounts2, troopSelections2, num2, isPlayerAttacker);
		return NavalCustomBattleHelper.PrepareBattleData(selectedCharacter, customBattleParties[0], customBattleShipLists[0], customBattleParties[1], customBattleShipLists[1], GameTypeSelectionGroup.SelectedGameTypeStringId, MapSelectionGroup.SelectedMap?.MapId, MapSelectionGroup.SelectedSeasonId, MapSelectionGroup.SelectedTimeOfDay, MapSelectionGroup.SelectedWindStrength, MapSelectionGroup.SelectedWindDirection, MapSelectionGroup.SelectedMap.Terrain, MapSelectionGroup.SelectedMap.ForcedSceneLevel);
	}

	public void ExecuteStart()
	{
		if (CanConfirm)
		{
			NavalCustomBattleHelper.StartGame(PrepareBattleData());
		}
	}

	public void ExecuteRandomize()
	{
		int targetDeckSize = MBRandom.RandomInt(40, 500);
		MapSelectionGroup.RandomizeAll();
		GameTypeSelectionGroup.RandomizeAll();
		PlayerSide.Randomize(targetDeckSize);
		EnemySide.Randomize(targetDeckSize);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		StartInputKey.OnFinalize();
		CancelInputKey.OnFinalize();
		ResetInputKey.OnFinalize();
		RandomizeInputKey.OnFinalize();
		TroopTypeSelectionPopUp?.OnFinalize();
		ShipSelectionPopUp?.OnFinalize();
	}

	public void ExecuteSwitchToNextCustomBattle()
	{
		if (CanSwitchMode)
		{
			ExecuteBack();
			GameStateManager.Current = Module.CurrentModule.GlobalGameStateManager;
			_nextCustomBattleProvider.StartCustomBattle();
		}
	}

	private void OnShipFocused(NavalCustomBattleShipItemVM focusedItem)
	{
		FocusedShipItem = focusedItem;
	}

	public void SetStartInputKey(HotKey hotkey)
	{
		StartInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
		TroopTypeSelectionPopUp?.SetCancelInputKey(hotkey);
		ShipSelectionPopUp?.SetCloseInputKey(hotkey);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
		TroopTypeSelectionPopUp?.SetResetInputKey(hotkey);
	}

	public void SetRandomizeInputKey(HotKey hotkey)
	{
		RandomizeInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCycleTierInputKey(HotKey hotkey)
	{
		PlayerSide.SetCycleTierInputKey(hotkey);
		EnemySide.SetCycleTierInputKey(hotkey);
	}
}
