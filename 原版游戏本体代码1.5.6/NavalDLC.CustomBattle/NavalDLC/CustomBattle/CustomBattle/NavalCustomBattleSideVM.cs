using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.CustomBattle.CustomBattle.SelectionItem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleSideVM : ViewModel
{
	private readonly TextObject _sideName;

	private readonly bool _isPlayerSide;

	private readonly Action _onCharacterSelected;

	private readonly Action _onShipSelected;

	private NavalCustomBattleArmyCompositionGroupVM _compositionGroup;

	private NavalCustomBattleFactionSelectionVM _factionSelectionGroup;

	private SelectorVM<NavalCustomBattleCharacterItemVM> _characterSelectionGroup;

	private NavalCustomBattleShipSelectionGroupVM _shipSelectionGroup;

	private CharacterViewModel _currentSelectedCharacter;

	private MBBindingList<CharacterEquipmentItemVM> _armorsList;

	private MBBindingList<CharacterEquipmentItemVM> _weaponsList;

	private string _name;

	private string _factionText;

	private string _titleText;

	private bool _isRaid;

	private bool _isLandSide;

	public BasicCharacterObject SelectedCharacter { get; private set; }

	[DataSourceProperty]
	public CharacterViewModel CurrentSelectedCharacter
	{
		get
		{
			return _currentSelectedCharacter;
		}
		set
		{
			if (value != _currentSelectedCharacter)
			{
				_currentSelectedCharacter = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedCharacter");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterEquipmentItemVM> ArmorsList
	{
		get
		{
			return _armorsList;
		}
		set
		{
			if (value != _armorsList)
			{
				_armorsList = value;
				OnPropertyChangedWithValue(value, "ArmorsList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterEquipmentItemVM> WeaponsList
	{
		get
		{
			return _weaponsList;
		}
		set
		{
			if (value != _weaponsList)
			{
				_weaponsList = value;
				OnPropertyChangedWithValue(value, "WeaponsList");
			}
		}
	}

	[DataSourceProperty]
	public string FactionText
	{
		get
		{
			return _factionText;
		}
		set
		{
			if (value != _factionText)
			{
				_factionText = value;
				OnPropertyChangedWithValue(value, "FactionText");
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
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<NavalCustomBattleCharacterItemVM> CharacterSelectionGroup
	{
		get
		{
			return _characterSelectionGroup;
		}
		set
		{
			if (value != _characterSelectionGroup)
			{
				_characterSelectionGroup = value;
				OnPropertyChangedWithValue(value, "CharacterSelectionGroup");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleArmyCompositionGroupVM CompositionGroup
	{
		get
		{
			return _compositionGroup;
		}
		set
		{
			if (value != _compositionGroup)
			{
				_compositionGroup = value;
				OnPropertyChangedWithValue(value, "CompositionGroup");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleFactionSelectionVM FactionSelectionGroup
	{
		get
		{
			return _factionSelectionGroup;
		}
		set
		{
			if (value != _factionSelectionGroup)
			{
				_factionSelectionGroup = value;
				OnPropertyChangedWithValue(value, "FactionSelectionGroup");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleShipSelectionGroupVM ShipSelectionGroup
	{
		get
		{
			return _shipSelectionGroup;
		}
		set
		{
			if (value != _shipSelectionGroup)
			{
				_shipSelectionGroup = value;
				OnPropertyChangedWithValue(value, "ShipSelectionGroup");
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
				ShipSelectionGroup.IsRaid = value;
				UpdateTroopCountLimits();
			}
		}
	}

	[DataSourceProperty]
	public bool IsLandSide
	{
		get
		{
			return _isLandSide;
		}
		set
		{
			if (value != _isLandSide)
			{
				_isLandSide = value;
				OnPropertyChangedWithValue(value, "IsLandSide");
				CompositionGroup.IsLand = value;
				UpdateTroopCountLimits();
			}
		}
	}

	public NavalCustomBattleSideVM(TextObject sideName, bool isPlayerSide, NavalCustomBattleTroopTypeSelectionPopUpVM troopTypeSelectionPopUp, NavalCustomBattleShipSelectionPopUpVM shipSelectionPopUp, Action<NavalCustomBattleShipItemVM> onShipFocused, Action onShipSelected, Action onCharacterSelected)
	{
		_sideName = sideName;
		_isPlayerSide = isPlayerSide;
		_onCharacterSelected = onCharacterSelected;
		_onShipSelected = onShipSelected;
		CompositionGroup = new NavalCustomBattleArmyCompositionGroupVM(troopTypeSelectionPopUp);
		FactionSelectionGroup = new NavalCustomBattleFactionSelectionVM(OnCultureSelection);
		CharacterSelectionGroup = new SelectorVM<NavalCustomBattleCharacterItemVM>(0, OnCharacterSelection);
		ShipSelectionGroup = new NavalCustomBattleShipSelectionGroupVM(_isPlayerSide, shipSelectionPopUp, OnShipSelectedOrUpgraded, onShipFocused);
		ArmorsList = new MBBindingList<CharacterEquipmentItemVM>();
		WeaponsList = new MBBindingList<CharacterEquipmentItemVM>();
		UpdateTroopCountLimits();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _sideName.ToString();
		FactionText = GameTexts.FindText("str_faction").ToString();
		if (_isPlayerSide)
		{
			TitleText = new TextObject("{=bLXleed8}Player Character").ToString();
		}
		else
		{
			TitleText = new TextObject("{=QAYngoNQ}Enemy Character").ToString();
		}
		CharacterSelectionGroup.ItemList.Clear();
		foreach (BasicCharacterObject character in NavalCustomBattleData.Characters)
		{
			CharacterSelectionGroup.AddItem(new NavalCustomBattleCharacterItemVM(character));
		}
		CharacterSelectionGroup.SelectedIndex = ((!_isPlayerSide) ? 1 : 0);
		UpdateCharacterVisual();
		_onCharacterSelected?.Invoke();
		CompositionGroup.RefreshValues();
		CharacterSelectionGroup.RefreshValues();
		FactionSelectionGroup.RefreshValues();
		ShipSelectionGroup.RefreshValues();
	}

	private void OnShipSelectedOrUpgraded()
	{
		_onShipSelected?.Invoke();
		UpdateTroopCountLimits();
	}

	public void OnGameTypeChange(string gameTypeStringId)
	{
		IsRaid = gameTypeStringId == "NavalRaid";
	}

	private void UpdateTroopCountLimits()
	{
		if (ShipSelectionGroup != null && CompositionGroup != null)
		{
			List<IShipOrigin> selectedShips = ShipSelectionGroup.GetSelectedShips();
			int minTroopCount = (IsLandSide ? 1 : (selectedShips.Count() * 4));
			int maxTroopCount = (IsLandSide ? BannerlordConfig.MaxBattleSize : (IsRaid ? selectedShips.Sum((IShipOrigin x) => x.MainDeckCrewCapacity) : selectedShips.Sum((IShipOrigin x) => x.TotalCrewCapacity)));
			int skeletalSize = (IsLandSide ? 1 : selectedShips.Sum((IShipOrigin x) => x.SkeletalCrewCapacity));
			int deckSize = (IsLandSide ? 1 : selectedShips.Sum((IShipOrigin x) => x.MainDeckCrewCapacity));
			CompositionGroup.UpdateTroopCountLimits(minTroopCount, maxTroopCount, skeletalSize, deckSize);
		}
	}

	private void OnCultureSelection(BasicCultureObject selectedCulture)
	{
		CompositionGroup.SetCurrentSelectedCulture(selectedCulture);
		if (CurrentSelectedCharacter != null)
		{
			CurrentSelectedCharacter.ArmorColor1 = selectedCulture.Color;
			CurrentSelectedCharacter.ArmorColor2 = selectedCulture.Color2;
			CurrentSelectedCharacter.BannerCodeText = selectedCulture.Banner.BannerCode;
		}
	}

	private void OnCharacterSelection(SelectorVM<NavalCustomBattleCharacterItemVM> selector)
	{
		BasicCharacterObject character = selector.SelectedItem.Character;
		SelectedCharacter = character;
		UpdateCharacterVisual();
		_onCharacterSelected?.Invoke();
	}

	public void UpdateCharacterVisual()
	{
		CurrentSelectedCharacter = new CharacterViewModel(CharacterViewModel.StanceTypes.EmphasizeFace);
		CurrentSelectedCharacter.FillFrom(SelectedCharacter, -1, FactionSelectionGroup?.SelectedItem?.Faction.Banner.BannerCode);
		CurrentSelectedCharacter.SetEquipment(EquipmentIndex.ArmorItemEndSlot, EquipmentElement.Invalid);
		if (FactionSelectionGroup?.SelectedItem != null)
		{
			CurrentSelectedCharacter.ArmorColor1 = FactionSelectionGroup.SelectedItem.Faction.Color;
			CurrentSelectedCharacter.ArmorColor2 = FactionSelectionGroup.SelectedItem.Faction.Color2;
		}
		ArmorsList.Clear();
		ArmorsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.NumAllWeaponSlots].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.Cape].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.Body].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.Gloves].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.Leg].Item));
		WeaponsList.Clear();
		WeaponsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.WeaponItemBeginSlot].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.Weapon1].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.Weapon2].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.Weapon3].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(SelectedCharacter.Equipment[EquipmentIndex.ExtraWeaponSlot].Item));
	}

	public void Randomize(int targetDeckSize)
	{
		CharacterSelectionGroup.ExecuteRandomize();
		FactionSelectionGroup.ExecuteRandomize();
		ShipSelectionGroup.ExecuteRandomize(targetDeckSize);
		CompositionGroup.ExecuteRandomize(targetDeckSize);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CharacterSelectionGroup?.OnFinalize();
		FactionSelectionGroup?.OnFinalize();
		ShipSelectionGroup?.OnFinalize();
		CompositionGroup?.OnFinalize();
	}

	public void SetCycleTierInputKey(HotKey hotkey)
	{
		ShipSelectionGroup.SetCycleTierInputKey(hotkey);
	}
}
