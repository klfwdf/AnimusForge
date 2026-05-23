using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CustomBattleSideVM : ViewModel
{
	private readonly TextObject _sideName;

	private readonly bool _isPlayerSide;

	private readonly Action _onCharacterSelected;

	private ArmyCompositionGroupVM _compositionGroup;

	private CustomBattleFactionSelectionVM _factionSelectionGroup;

	private SelectorVM<CharacterItemVM> _characterSelectionGroup;

	private CharacterViewModel _currentSelectedCharacter;

	private MBBindingList<CharacterEquipmentItemVM> _armorsList;

	private MBBindingList<CharacterEquipmentItemVM> _weaponsList;

	private string _name;

	private string _factionText;

	private string _titleText;

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
	public SelectorVM<CharacterItemVM> CharacterSelectionGroup
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
	public ArmyCompositionGroupVM CompositionGroup
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
	public CustomBattleFactionSelectionVM FactionSelectionGroup
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

	public CustomBattleSideVM(TextObject sideName, bool isPlayerSide, TroopTypeSelectionPopUpVM troopTypeSelectionPopUp, Action onCharacterSelected)
	{
		_sideName = sideName;
		_isPlayerSide = isPlayerSide;
		_onCharacterSelected = onCharacterSelected;
		CompositionGroup = new ArmyCompositionGroupVM(troopTypeSelectionPopUp);
		FactionSelectionGroup = new CustomBattleFactionSelectionVM(OnCultureSelection);
		CharacterSelectionGroup = new SelectorVM<CharacterItemVM>(0, OnCharacterSelection);
		ArmorsList = new MBBindingList<CharacterEquipmentItemVM>();
		WeaponsList = new MBBindingList<CharacterEquipmentItemVM>();
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
		foreach (BasicCharacterObject character in CustomBattleData.Characters)
		{
			CharacterSelectionGroup.AddItem(new CharacterItemVM(character));
		}
		CharacterSelectionGroup.SelectedIndex = ((!_isPlayerSide) ? 1 : 0);
		UpdateCharacterVisual();
		_onCharacterSelected?.Invoke();
		CompositionGroup.RefreshValues();
		CharacterSelectionGroup.RefreshValues();
		FactionSelectionGroup.RefreshValues();
	}

	public void OnPlayerTypeChange(CustomBattlePlayerType playerType)
	{
		CompositionGroup.OnPlayerTypeChange(playerType);
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

	private void OnCharacterSelection(SelectorVM<CharacterItemVM> selector)
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

	public void Randomize(CustomBattleSideVM oppositeSide = null)
	{
		CharacterSelectionGroup.ExecuteRandomize();
		FactionSelectionGroup.ExecuteRandomize();
		CompositionGroup.ExecuteRandomize(oppositeSide?.CompositionGroup);
	}
}
