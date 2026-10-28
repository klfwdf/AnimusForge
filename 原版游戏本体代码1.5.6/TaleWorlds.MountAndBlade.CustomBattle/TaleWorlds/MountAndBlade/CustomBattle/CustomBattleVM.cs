using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;
using TaleWorlds.MountAndBlade.View.CustomBattle;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CustomBattleVM : ViewModel
{
	private readonly ICustomBattleProvider _nextCustomBattleProvider;

	private CustomBattleState _customBattleState;

	private TroopTypeSelectionPopUpVM _troopTypeSelectionPopUp;

	private CustomBattleSideVM _enemySide;

	private CustomBattleSideVM _playerSide;

	private bool _isAttackerCustomMachineSelectionEnabled;

	private bool _isDefenderCustomMachineSelectionEnabled;

	private GameTypeSelectionGroupVM _gameTypeSelectionGroup;

	private MapSelectionGroupVM _mapSelectionGroup;

	private string _randomizeButtonText;

	private string _backButtonText;

	private string _startButtonText;

	private string _titleText;

	private string _switchButtonText;

	private bool _CanSwitchMode;

	private HintViewModel _switchHint;

	private MBBindingList<CustomBattleSiegeMachineVM> _attackerMeleeMachines;

	private MBBindingList<CustomBattleSiegeMachineVM> _attackerRangedMachines;

	private MBBindingList<CustomBattleSiegeMachineVM> _defenderMachines;

	private InputKeyItemVM _startInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _randomizeInputKey;

	[DataSourceProperty]
	public TroopTypeSelectionPopUpVM TroopTypeSelectionPopUp
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
	public bool IsAttackerCustomMachineSelectionEnabled
	{
		get
		{
			return _isAttackerCustomMachineSelectionEnabled;
		}
		set
		{
			if (value != _isAttackerCustomMachineSelectionEnabled)
			{
				_isAttackerCustomMachineSelectionEnabled = value;
				OnPropertyChangedWithValue(value, "IsAttackerCustomMachineSelectionEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDefenderCustomMachineSelectionEnabled
	{
		get
		{
			return _isDefenderCustomMachineSelectionEnabled;
		}
		set
		{
			if (value != _isDefenderCustomMachineSelectionEnabled)
			{
				_isDefenderCustomMachineSelectionEnabled = value;
				OnPropertyChangedWithValue(value, "IsDefenderCustomMachineSelectionEnabled");
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
	public CustomBattleSideVM EnemySide
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
	public CustomBattleSideVM PlayerSide
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
	public GameTypeSelectionGroupVM GameTypeSelectionGroup
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
	public MapSelectionGroupVM MapSelectionGroup
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
	public MBBindingList<CustomBattleSiegeMachineVM> AttackerMeleeMachines
	{
		get
		{
			return _attackerMeleeMachines;
		}
		set
		{
			if (value != _attackerMeleeMachines)
			{
				_attackerMeleeMachines = value;
				OnPropertyChangedWithValue(value, "AttackerMeleeMachines");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CustomBattleSiegeMachineVM> AttackerRangedMachines
	{
		get
		{
			return _attackerRangedMachines;
		}
		set
		{
			if (value != _attackerRangedMachines)
			{
				_attackerRangedMachines = value;
				OnPropertyChangedWithValue(value, "AttackerRangedMachines");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CustomBattleSiegeMachineVM> DefenderMachines
	{
		get
		{
			return _defenderMachines;
		}
		set
		{
			if (value != _defenderMachines)
			{
				_defenderMachines = value;
				OnPropertyChangedWithValue(value, "DefenderMachines");
			}
		}
	}

	[DataSourceProperty]
	public bool CanSwitchMode
	{
		get
		{
			return _CanSwitchMode;
		}
		set
		{
			if (value != _CanSwitchMode)
			{
				_CanSwitchMode = value;
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

	private static CustomBattleCompositionData GetBattleCompositionDataFromCompositionGroup(ArmyCompositionGroupVM compositionGroup)
	{
		return new CustomBattleCompositionData((float)compositionGroup.RangedInfantryComposition.CompositionValue / 100f, (float)compositionGroup.MeleeCavalryComposition.CompositionValue / 100f, (float)compositionGroup.RangedCavalryComposition.CompositionValue / 100f);
	}

	private static List<BasicCharacterObject>[] GetTroopSelections(ArmyCompositionGroupVM armyComposition)
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

	private static void FillSiegeMachines(List<MissionSiegeWeapon> machines, MBBindingList<CustomBattleSiegeMachineVM> vmMachines)
	{
		foreach (CustomBattleSiegeMachineVM vmMachine in vmMachines)
		{
			if (vmMachine.SiegeEngineType != null)
			{
				machines.Add(MissionSiegeWeapon.CreateDefaultWeapon(vmMachine.SiegeEngineType));
			}
		}
	}

	public CustomBattleVM(CustomBattleState battleState)
	{
		_customBattleState = battleState;
		IsAttackerCustomMachineSelectionEnabled = false;
		TroopTypeSelectionPopUp = new TroopTypeSelectionPopUpVM();
		PlayerSide = new CustomBattleSideVM(new TextObject("{=BC7n6qxk}PLAYER"), isPlayerSide: true, TroopTypeSelectionPopUp, OnSelectedCharactersChanged);
		EnemySide = new CustomBattleSideVM(new TextObject("{=35IHscBa}ENEMY"), isPlayerSide: false, TroopTypeSelectionPopUp, OnSelectedCharactersChanged);
		OnSelectedCharactersChanged();
		MapSelectionGroup = new MapSelectionGroupVM();
		GameTypeSelectionGroup = new GameTypeSelectionGroupVM(OnPlayerTypeChange, OnGameTypeChange);
		AttackerMeleeMachines = new MBBindingList<CustomBattleSiegeMachineVM>();
		for (int i = 0; i < 3; i++)
		{
			AttackerMeleeMachines.Add(new CustomBattleSiegeMachineVM(null, OnMeleeMachineSelection, OnResetMachineSelection));
		}
		AttackerRangedMachines = new MBBindingList<CustomBattleSiegeMachineVM>();
		for (int j = 0; j < 4; j++)
		{
			AttackerRangedMachines.Add(new CustomBattleSiegeMachineVM(null, OnAttackerRangedMachineSelection, OnResetMachineSelection));
		}
		DefenderMachines = new MBBindingList<CustomBattleSiegeMachineVM>();
		for (int k = 0; k < 4; k++)
		{
			DefenderMachines.Add(new CustomBattleSiegeMachineVM(null, OnDefenderRangedMachineSelection, OnResetMachineSelection));
		}
		CanSwitchMode = CustomBattleFactory.GetProviderCount() > 1;
		if (CanSwitchMode)
		{
			_nextCustomBattleProvider = CustomBattleFactory.CollectNextProvider(typeof(CustomBattleProvider));
			SwitchHint = new HintViewModel(new TextObject("{=Jfe53wbr}Switch to {PROVIDER_NAME}").SetTextVariable("PROVIDER_NAME", _nextCustomBattleProvider.GetName()));
		}
		RefreshValues();
		SetDefaultSiegeMachines();
	}

	private void SetDefaultSiegeMachines()
	{
		AttackerMeleeMachines[0].SetMachineType(DefaultSiegeEngineTypes.SiegeTower);
		AttackerMeleeMachines[1].SetMachineType(DefaultSiegeEngineTypes.Ram);
		AttackerMeleeMachines[2].SetMachineType(DefaultSiegeEngineTypes.SiegeTower);
		AttackerRangedMachines[0].SetMachineType(DefaultSiegeEngineTypes.Trebuchet);
		AttackerRangedMachines[1].SetMachineType(DefaultSiegeEngineTypes.Onager);
		AttackerRangedMachines[2].SetMachineType(DefaultSiegeEngineTypes.Onager);
		AttackerRangedMachines[3].SetMachineType(DefaultSiegeEngineTypes.FireBallista);
		DefenderMachines[0].SetMachineType(DefaultSiegeEngineTypes.FireCatapult);
		DefenderMachines[1].SetMachineType(DefaultSiegeEngineTypes.FireCatapult);
		DefenderMachines[2].SetMachineType(DefaultSiegeEngineTypes.Catapult);
		DefenderMachines[3].SetMachineType(DefaultSiegeEngineTypes.FireBallista);
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
		foreach (CharacterItemVM item in PlayerSide.CharacterSelectionGroup.ItemList)
		{
			item.CanBeSelected = item.Character != basicCharacterObject2;
		}
		foreach (CharacterItemVM item2 in EnemySide.CharacterSelectionGroup.ItemList)
		{
			item2.CanBeSelected = item2.Character != basicCharacterObject;
		}
	}

	private void OnPlayerTypeChange(CustomBattlePlayerType playerType)
	{
		PlayerSide.OnPlayerTypeChange(playerType);
	}

	private void OnGameTypeChange(string gameTypeStringId)
	{
		MapSelectionGroup.OnGameTypeChange(gameTypeStringId);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RandomizeButtonText = GameTexts.FindText("str_randomize").ToString();
		StartButtonText = GameTexts.FindText("str_start").ToString();
		BackButtonText = GameTexts.FindText("str_back").ToString();
		SwitchButtonText = GameTexts.FindText("str_switch").ToString();
		TitleText = GameTexts.FindText("str_custom_battle").ToString();
		EnemySide.RefreshValues();
		PlayerSide.RefreshValues();
		AttackerMeleeMachines.ApplyActionOnAllItems(delegate(CustomBattleSiegeMachineVM x)
		{
			x.RefreshValues();
		});
		AttackerRangedMachines.ApplyActionOnAllItems(delegate(CustomBattleSiegeMachineVM x)
		{
			x.RefreshValues();
		});
		DefenderMachines.ApplyActionOnAllItems(delegate(CustomBattleSiegeMachineVM x)
		{
			x.RefreshValues();
		});
		MapSelectionGroup.RefreshValues();
		TroopTypeSelectionPopUp?.RefreshValues();
	}

	private void OnResetMachineSelection(CustomBattleSiegeMachineVM selectedSlot)
	{
		selectedSlot.SetMachineType(null);
	}

	private void OnMeleeMachineSelection(CustomBattleSiegeMachineVM selectedSlot)
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement(null, GameTexts.FindText("str_empty").ToString(), null));
		foreach (SiegeEngineType allAttackerMeleeMachine in CustomBattleData.GetAllAttackerMeleeMachines())
		{
			list.Add(new InquiryElement(allAttackerMeleeMachine, allAttackerMeleeMachine.Name.ToString(), null));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=MVOWsP48}Select a Melee Machine").ToString(), string.Empty, list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), "", delegate(List<InquiryElement> selectedElements)
		{
			selectedSlot.SetMachineType(selectedElements.FirstOrDefault()?.Identifier as SiegeEngineType);
		}, null));
	}

	private void OnAttackerRangedMachineSelection(CustomBattleSiegeMachineVM selectedSlot)
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement(null, GameTexts.FindText("str_empty").ToString(), null));
		foreach (SiegeEngineType allAttackerRangedMachine in CustomBattleData.GetAllAttackerRangedMachines())
		{
			list.Add(new InquiryElement(allAttackerRangedMachine, allAttackerRangedMachine.Name.ToString(), null));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=SLZzfNPr}Select a Ranged Machine").ToString(), string.Empty, list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), "", delegate(List<InquiryElement> selectedElements)
		{
			selectedSlot.SetMachineType(selectedElements.FirstOrDefault()?.Identifier as SiegeEngineType);
		}, null));
	}

	private void OnDefenderRangedMachineSelection(CustomBattleSiegeMachineVM selectedSlot)
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement(null, GameTexts.FindText("str_empty").ToString(), null));
		foreach (SiegeEngineType allDefenderRangedMachine in CustomBattleData.GetAllDefenderRangedMachines())
		{
			list.Add(new InquiryElement(allDefenderRangedMachine, allDefenderRangedMachine.Name.ToString(), null));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=SLZzfNPr}Select a Ranged Machine").ToString(), string.Empty, list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), "", delegate(List<InquiryElement> selectedElements)
		{
			selectedSlot.SetMachineType(selectedElements.FirstOrDefault()?.Identifier as SiegeEngineType);
		}, null));
	}

	private void ExecuteRandomizeAttackerSiegeEngines()
	{
		MBList<SiegeEngineType> mBList = new MBList<SiegeEngineType>();
		mBList.AddRange(CustomBattleData.GetAllAttackerMeleeMachines());
		mBList.Add(null);
		foreach (CustomBattleSiegeMachineVM attackerMeleeMachine in _attackerMeleeMachines)
		{
			attackerMeleeMachine.SetMachineType(mBList.GetRandomElement());
		}
		mBList.Clear();
		mBList.AddRange(CustomBattleData.GetAllAttackerRangedMachines());
		mBList.Add(null);
		foreach (CustomBattleSiegeMachineVM attackerRangedMachine in _attackerRangedMachines)
		{
			attackerRangedMachine.SetMachineType(mBList.GetRandomElement());
		}
	}

	private void ExecuteRandomizeDefenderSiegeEngines()
	{
		MBList<SiegeEngineType> mBList = new MBList<SiegeEngineType>();
		mBList.AddRange(CustomBattleData.GetAllDefenderRangedMachines());
		mBList.Add(null);
		foreach (CustomBattleSiegeMachineVM defenderMachine in _defenderMachines)
		{
			defenderMachine.SetMachineType(mBList.GetRandomElement());
		}
	}

	public void ExecuteBack()
	{
		Debug.Print("EXECUTE BACK - PRESSED", 0, Debug.DebugColor.Green);
		Game.Current.GameStateManager.PopState();
	}

	private CustomBattleData PrepareBattleData()
	{
		BasicCharacterObject selectedCharacter = PlayerSide.SelectedCharacter;
		BasicCharacterObject selectedCharacter2 = EnemySide.SelectedCharacter;
		int num = PlayerSide.CompositionGroup.ArmySize;
		int armySize = EnemySide.CompositionGroup.ArmySize;
		bool isPlayerAttacker = GameTypeSelectionGroup.SelectedPlayerSide == CustomBattlePlayerSide.Attacker;
		bool num2 = GameTypeSelectionGroup.SelectedPlayerType == CustomBattlePlayerType.Commander;
		BasicCharacterObject playerSideGeneralCharacter = null;
		if (!num2)
		{
			MBList<BasicCharacterObject> mBList = CustomBattleData.Characters.ToMBList();
			mBList.Remove(selectedCharacter);
			mBList.Remove(selectedCharacter2);
			playerSideGeneralCharacter = mBList.GetRandomElement();
			num--;
		}
		int[] troopCounts = CustomBattleHelper.GetTroopCounts(num, GetBattleCompositionDataFromCompositionGroup(PlayerSide.CompositionGroup));
		int[] troopCounts2 = CustomBattleHelper.GetTroopCounts(armySize, GetBattleCompositionDataFromCompositionGroup(EnemySide.CompositionGroup));
		List<BasicCharacterObject>[] troopSelections = GetTroopSelections(PlayerSide.CompositionGroup);
		List<BasicCharacterObject>[] troopSelections2 = GetTroopSelections(EnemySide.CompositionGroup);
		BasicCultureObject faction = PlayerSide.FactionSelectionGroup.SelectedItem.Faction;
		BasicCultureObject faction2 = EnemySide.FactionSelectionGroup.SelectedItem.Faction;
		CustomBattleCombatant[] customBattleParties = CustomBattleHelper.GetCustomBattleParties(selectedCharacter, playerSideGeneralCharacter, selectedCharacter2, faction, troopCounts, troopSelections, faction2, troopCounts2, troopSelections2, isPlayerAttacker);
		List<MissionSiegeWeapon> list = null;
		List<MissionSiegeWeapon> list2 = null;
		float[] wallHitPointsPercentages = null;
		if (GameTypeSelectionGroup.SelectedGameTypeString == "Siege")
		{
			list = new List<MissionSiegeWeapon>();
			list2 = new List<MissionSiegeWeapon>();
			FillSiegeMachines(list, _attackerMeleeMachines);
			FillSiegeMachines(list, _attackerRangedMachines);
			FillSiegeMachines(list2, _defenderMachines);
			wallHitPointsPercentages = CustomBattleHelper.GetWallHitpointPercentages(MapSelectionGroup.SelectedWallBreachedCount);
		}
		return CustomBattleHelper.PrepareBattleData(selectedCharacter, playerSideGeneralCharacter, customBattleParties[0], customBattleParties[1], GameTypeSelectionGroup.SelectedPlayerSide, GameTypeSelectionGroup.SelectedPlayerType, GameTypeSelectionGroup.SelectedGameTypeString, MapSelectionGroup.SelectedMap?.MapId, MapSelectionGroup.SelectedSeasonId, MapSelectionGroup.SelectedTimeOfDay, list, list2, wallHitPointsPercentages, MapSelectionGroup.SelectedSceneLevel, MapSelectionGroup.IsSallyOutSelected, MapSelectionGroup.SelectedMap?.ForcedSceneLevel);
	}

	public void ExecuteStart()
	{
		CustomBattleHelper.StartGame(PrepareBattleData());
		Debug.Print("EXECUTE START - PRESSED", 0, Debug.DebugColor.Green);
	}

	public void ExecuteRandomize()
	{
		GameTypeSelectionGroup.RandomizeAll();
		MapSelectionGroup.RandomizeAll();
		PlayerSide.Randomize();
		EnemySide.Randomize(PlayerSide);
		ExecuteRandomizeAttackerSiegeEngines();
		ExecuteRandomizeDefenderSiegeEngines();
		Debug.Print("EXECUTE RANDOMIZE - PRESSED", 0, Debug.DebugColor.Green);
	}

	private void ExecuteDoneDefenderCustomMachineSelection()
	{
		IsDefenderCustomMachineSelectionEnabled = false;
	}

	private void ExecuteDoneAttackerCustomMachineSelection()
	{
		IsAttackerCustomMachineSelectionEnabled = false;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		StartInputKey.OnFinalize();
		CancelInputKey.OnFinalize();
		ResetInputKey.OnFinalize();
		RandomizeInputKey.OnFinalize();
		TroopTypeSelectionPopUp?.OnFinalize();
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

	public void SetStartInputKey(HotKey hotkey)
	{
		StartInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
		TroopTypeSelectionPopUp?.SetCancelInputKey(hotkey);
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
}
