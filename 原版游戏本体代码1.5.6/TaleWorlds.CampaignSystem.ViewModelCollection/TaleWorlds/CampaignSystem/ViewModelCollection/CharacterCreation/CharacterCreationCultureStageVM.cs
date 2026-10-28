using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationCultureStageVM : CharacterCreationStageBaseVM
{
	private Action<CultureObject> _onCultureSelected;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private bool _isActive;

	private MBBindingList<CharacterCreationCultureVM> _cultures;

	private CharacterCreationCultureVM _currentSelectedCulture;

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
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterCreationCultureVM> Cultures
	{
		get
		{
			return _cultures;
		}
		set
		{
			if (value != _cultures)
			{
				_cultures = value;
				OnPropertyChangedWithValue(value, "Cultures");
			}
		}
	}

	[DataSourceProperty]
	public CharacterCreationCultureVM CurrentSelectedCulture
	{
		get
		{
			return _currentSelectedCulture;
		}
		set
		{
			if (value != _currentSelectedCulture)
			{
				_currentSelectedCulture = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedCulture");
			}
		}
	}

	public CharacterCreationCultureStageVM(CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText, Action<CultureObject> onCultureSelected)
		: base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
	{
		_onCultureSelected = onCultureSelected;
		TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreationContent currentContent = (GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager.CharacterCreationContent;
		Cultures = new MBBindingList<CharacterCreationCultureVM>();
		base.Title = GameTexts.FindText("str_culture").ToString();
		base.Description = new TextObject("{=fz2kQjFS}Choose your character's culture:").ToString();
		base.SelectionText = new TextObject("{=MaHMOzL2}Character Culture").ToString();
		foreach (CultureObject culture in currentContent.GetCultures())
		{
			CharacterCreationCultureVM item = new CharacterCreationCultureVM(culture, OnCultureSelection);
			Cultures.Add(item);
		}
		SortCultureList(Cultures);
		if (currentContent.SelectedCulture != null)
		{
			CharacterCreationCultureVM characterCreationCultureVM = Cultures.FirstOrDefault((CharacterCreationCultureVM c) => c.Culture == currentContent.SelectedCulture);
			if (characterCreationCultureVM != null)
			{
				OnCultureSelection(characterCreationCultureVM);
			}
		}
	}

	private void SortCultureList(MBBindingList<CharacterCreationCultureVM> listToWorkOn)
	{
		int swapFromIndex = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("vlan")));
		Swap(listToWorkOn, swapFromIndex, 0);
		int swapFromIndex2 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("stur")));
		Swap(listToWorkOn, swapFromIndex2, 1);
		int swapFromIndex3 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("empi")));
		Swap(listToWorkOn, swapFromIndex3, 2);
		int swapFromIndex4 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("aser")));
		Swap(listToWorkOn, swapFromIndex4, 3);
		int swapFromIndex5 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("khuz")));
		Swap(listToWorkOn, swapFromIndex5, 4);
	}

	public void OnCultureSelection(CharacterCreationCultureVM selectedCulture)
	{
		InitializePlayersFaceKeyAccordingToCultureSelection(selectedCulture);
		foreach (CharacterCreationCultureVM item in Cultures.Where((CharacterCreationCultureVM c) => c.IsSelected))
		{
			item.IsSelected = false;
		}
		selectedCulture.IsSelected = true;
		CurrentSelectedCulture = selectedCulture;
		base.AnyItemSelected = true;
		base.CanAdvance = CanAdvanceToNextStage();
		_onCultureSelected?.Invoke(selectedCulture.Culture);
	}

	private void InitializePlayersFaceKeyAccordingToCultureSelection(CharacterCreationCultureVM selectedCulture)
	{
		if (selectedCulture.Culture.DefaultCharacterCreationBodyProperty != null)
		{
			CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(selectedCulture.Culture.DefaultCharacterCreationBodyProperty.BodyPropertyMax, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
			Hero.MainHero.Culture = selectedCulture.Culture;
		}
	}

	private void Swap(MBBindingList<CharacterCreationCultureVM> listToWorkOn, int swapFromIndex, int swapToIndex)
	{
		if (swapFromIndex != swapToIndex)
		{
			CharacterCreationCultureVM value = listToWorkOn[swapToIndex];
			listToWorkOn[swapToIndex] = listToWorkOn[swapFromIndex];
			listToWorkOn[swapFromIndex] = value;
		}
	}

	public override void OnNextStage()
	{
		if (CurrentSelectedCulture == null)
		{
			Debug.FailedAssert("Selected culture can't be null at this stage", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterCreation\\CharacterCreationCultureStageVM.cs", "OnNextStage", 111);
			return;
		}
		(GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager.CharacterCreationContent.SetSelectedCulture(CurrentSelectedCulture.Culture, CharacterCreationManager);
		_affirmativeAction();
	}

	public override void OnPreviousStage()
	{
		_negativeAction();
	}

	public override bool CanAdvanceToNextStage()
	{
		return Cultures.Any((CharacterCreationCultureVM s) => s.IsSelected);
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
