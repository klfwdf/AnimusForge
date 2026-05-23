using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationNarrativeStageVM : CharacterCreationStageBaseVM
{
	public Action OnOptionSelection;

	private readonly Action _onMenuChanged;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private CharacterCreationGainedPropertiesVM _gainedPropertiesController;

	private CharacterCreationOptionVM _selectedOption;

	private MBBindingList<CharacterCreationOptionVM> _selectionList;

	[DataSourceProperty]
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

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public CharacterCreationGainedPropertiesVM GainedPropertiesController
	{
		get
		{
			return _gainedPropertiesController;
		}
		set
		{
			if (value != _gainedPropertiesController)
			{
				_gainedPropertiesController = value;
				OnPropertyChangedWithValue(value, "GainedPropertiesController");
			}
		}
	}

	[DataSourceProperty]
	public CharacterCreationOptionVM SelectedOption
	{
		get
		{
			return _selectedOption;
		}
		set
		{
			if (value != _selectedOption)
			{
				_selectedOption = value;
				OnPropertyChangedWithValue(value, "SelectedOption");
				base.AnyItemSelected = SelectedOption != null;
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterCreationOptionVM> SelectionList
	{
		get
		{
			return _selectionList;
		}
		set
		{
			if (value != _selectionList)
			{
				_selectionList = value;
				OnPropertyChangedWithValue(value, "SelectionList");
			}
		}
	}

	public CharacterCreationNarrativeStageVM(CharacterCreationManager characterCreationManagerMenu, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText, Action onMenuChanged)
		: base(characterCreationManagerMenu, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
	{
		_onMenuChanged = onMenuChanged;
		SelectionList = new MBBindingList<CharacterCreationOptionVM>();
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		GainedPropertiesController = new CharacterCreationGainedPropertiesVM(CharacterCreationManager);
	}

	public void RefreshMenu()
	{
		SelectionList.Clear();
		foreach (NarrativeMenuOption suitableNarrativeMenuOption in CharacterCreationManager.GetSuitableNarrativeMenuOptions())
		{
			CharacterCreationOptionVM item = new CharacterCreationOptionVM(OnOptionSelected, suitableNarrativeMenuOption);
			SelectionList.Add(item);
		}
		if (CharacterCreationManager.SelectedOptions.TryGetValue(CharacterCreationManager.CurrentMenu, out var value))
		{
			for (int i = 0; i < SelectionList.Count; i++)
			{
				if (SelectionList[i].Option == value)
				{
					SelectionList[i].ExecuteSelect();
				}
			}
		}
		base.Title = CharacterCreationManager.CurrentMenu.Title.ToString();
		base.Description = CharacterCreationManager.CurrentMenu.Description.ToString();
		GameTexts.SetVariable("SELECTION", base.Title);
		base.SelectionText = GameTexts.FindText("str_char_creation_generic_selection").ToString();
		base.CanAdvance = CanAdvanceToNextStage();
		_onMenuChanged?.Invoke();
	}

	public void OnOptionSelected(CharacterCreationOptionVM option)
	{
		if (SelectedOption != null)
		{
			SelectedOption.IsSelected = false;
		}
		SelectedOption = option;
		if (SelectedOption != null)
		{
			SelectedOption.IsSelected = true;
			CharacterCreationManager.OnNarrativeMenuOptionSelected(_selectedOption.Option);
			SelectedOption.RefreshValues();
		}
		OnOptionSelection?.Invoke();
		base.CanAdvance = CanAdvanceToNextStage();
		GainedPropertiesController.UpdateValues();
	}

	public override void OnNextStage()
	{
		if (CharacterCreationManager.TrySwitchToNextMenu())
		{
			RefreshMenu();
		}
		else
		{
			_affirmativeAction();
		}
	}

	public override void OnPreviousStage()
	{
		if (CharacterCreationManager.TrySwitchToPreviousMenu())
		{
			RefreshMenu();
		}
		else
		{
			_negativeAction();
		}
	}

	public override bool CanAdvanceToNextStage()
	{
		if (SelectionList.Count != 0)
		{
			return SelectionList.Any((CharacterCreationOptionVM s) => s.IsSelected);
		}
		return true;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
