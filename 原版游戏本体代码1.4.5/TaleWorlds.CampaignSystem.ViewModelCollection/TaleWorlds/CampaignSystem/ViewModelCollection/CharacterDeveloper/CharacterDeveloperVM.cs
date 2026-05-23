using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

public class CharacterDeveloperVM : ViewModel
{
	private readonly Action _closeCharacterDeveloper;

	private readonly List<CharacterDeveloperHeroItemVM> _heroList;

	private readonly IViewDataTracker _viewDataTracker;

	public readonly ReadOnlyCollection<CharacterDeveloperHeroItemVM> HeroList;

	private int _heroIndex;

	private string _latestTutorialElementID;

	private Func<string, TextObject> _getKeyTextFromKeyId;

	private bool _isActivePerkHighlightsApplied;

	private readonly string _availablePerksHighlighId = "AvailablePerks";

	private string _skillsText;

	private string _doneLbl;

	private string _resetLbl;

	private string _cancelLbl;

	private string _unspentFocusPointsText;

	private string _traitsText;

	private string _partyRoleText;

	private HintViewModel _unspentCharacterPointsHint;

	private HintViewModel _unspentAttributePointsHint;

	private HintViewModel _levelHint;

	private HintViewModel _unopenedPerksHint;

	private BasicTooltipViewModel _previousCharacterHint;

	private BasicTooltipViewModel _nextCharacterHint;

	private string _addFocusText;

	private bool _isPlayerAccompanied;

	private string _skillFocusText;

	private ElementNotificationVM _tutorialNotification;

	private HintViewModel _resetHint;

	private HintViewModel _focusVisualHint;

	private CharacterDeveloperHeroItemVM _currentCharacter;

	private string _currentCharacterNameText;

	private SelectorVM<SelectorItemVM> _characterList;

	private int _unopenedPerksNumForCurrentCharacter;

	private bool _hasUnopenedPerksForCurrentCharacter;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _previousCharacterInputKey;

	private InputKeyItemVM _nextCharacterInputKey;

	[DataSourceProperty]
	public string CurrentCharacterNameText
	{
		get
		{
			return _currentCharacterNameText;
		}
		set
		{
			if (value != _currentCharacterNameText)
			{
				_currentCharacterNameText = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterNameText");
			}
		}
	}

	[DataSourceProperty]
	public CharacterDeveloperHeroItemVM CurrentCharacter
	{
		get
		{
			return _currentCharacter;
		}
		set
		{
			if (value == _currentCharacter)
			{
				return;
			}
			if (_currentCharacter != null)
			{
				if (_currentCharacter.IsInspectingAnAttribute)
				{
					_currentCharacter.ExecuteStopInspectingCurrentAttribute();
				}
				if (_currentCharacter.PerkSelection.IsActive)
				{
					_currentCharacter.PerkSelection.ExecuteDeactivate();
				}
			}
			_currentCharacter = value;
			CurrentCharacterNameText = _currentCharacter?.HeroNameText ?? string.Empty;
			OnPropertyChangedWithValue(value, "CurrentCharacter");
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> CharacterList
	{
		get
		{
			return _characterList;
		}
		set
		{
			if (value != _characterList)
			{
				_characterList = value;
				OnPropertyChangedWithValue(value, "CharacterList");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FocusVisualHint
	{
		get
		{
			return _focusVisualHint;
		}
		set
		{
			if (value != _focusVisualHint)
			{
				_focusVisualHint = value;
				OnPropertyChangedWithValue(value, "FocusVisualHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetHint
	{
		get
		{
			return _resetHint;
		}
		set
		{
			if (value != _resetHint)
			{
				_resetHint = value;
				OnPropertyChangedWithValue(value, "ResetHint");
			}
		}
	}

	[DataSourceProperty]
	public ElementNotificationVM TutorialNotification
	{
		get
		{
			return _tutorialNotification;
		}
		set
		{
			if (value != _tutorialNotification)
			{
				_tutorialNotification = value;
				OnPropertyChangedWithValue(value, "TutorialNotification");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerAccompanied
	{
		get
		{
			return _isPlayerAccompanied;
		}
		set
		{
			if (value != _isPlayerAccompanied)
			{
				_isPlayerAccompanied = value;
				OnPropertyChangedWithValue(value, "IsPlayerAccompanied");
			}
		}
	}

	[DataSourceProperty]
	public string UnspentCharacterPointsText
	{
		get
		{
			return _unspentFocusPointsText;
		}
		set
		{
			if (value != _unspentFocusPointsText)
			{
				_unspentFocusPointsText = value;
				OnPropertyChangedWithValue(value, "UnspentCharacterPointsText");
			}
		}
	}

	[DataSourceProperty]
	public string TraitsText
	{
		get
		{
			return _traitsText;
		}
		set
		{
			if (value != _traitsText)
			{
				_traitsText = value;
				OnPropertyChangedWithValue(value, "TraitsText");
			}
		}
	}

	[DataSourceProperty]
	public string PartyRoleText
	{
		get
		{
			return _partyRoleText;
		}
		set
		{
			if (value != _partyRoleText)
			{
				_partyRoleText = value;
				OnPropertyChangedWithValue(value, "PartyRoleText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UnspentCharacterPointsHint
	{
		get
		{
			return _unspentCharacterPointsHint;
		}
		set
		{
			if (value != _unspentCharacterPointsHint)
			{
				_unspentCharacterPointsHint = value;
				OnPropertyChangedWithValue(value, "UnspentCharacterPointsHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UnspentAttributePointsHint
	{
		get
		{
			return _unspentAttributePointsHint;
		}
		set
		{
			if (value != _unspentAttributePointsHint)
			{
				_unspentAttributePointsHint = value;
				OnPropertyChangedWithValue(value, "UnspentAttributePointsHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LevelHint
	{
		get
		{
			return _levelHint;
		}
		set
		{
			if (value != _levelHint)
			{
				_levelHint = value;
				OnPropertyChangedWithValue(value, "LevelHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UnopenedPerksHint
	{
		get
		{
			return _unopenedPerksHint;
		}
		set
		{
			if (value != _unopenedPerksHint)
			{
				_unopenedPerksHint = value;
				OnPropertyChangedWithValue(value, "UnopenedPerksHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel PreviousCharacterHint
	{
		get
		{
			return _previousCharacterHint;
		}
		set
		{
			if (value != _previousCharacterHint)
			{
				_previousCharacterHint = value;
				OnPropertyChangedWithValue(value, "PreviousCharacterHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel NextCharacterHint
	{
		get
		{
			return _nextCharacterHint;
		}
		set
		{
			if (value != _nextCharacterHint)
			{
				_nextCharacterHint = value;
				OnPropertyChangedWithValue(value, "NextCharacterHint");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
			}
		}
	}

	[DataSourceProperty]
	public string ResetLbl
	{
		get
		{
			return _resetLbl;
		}
		set
		{
			if (value != _resetLbl)
			{
				_resetLbl = value;
				OnPropertyChangedWithValue(value, "ResetLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CancelLbl
	{
		get
		{
			return _cancelLbl;
		}
		set
		{
			if (value != _cancelLbl)
			{
				_cancelLbl = value;
				OnPropertyChangedWithValue(value, "CancelLbl");
			}
		}
	}

	[DataSourceProperty]
	public string SkillFocusText
	{
		get
		{
			return _skillFocusText;
		}
		set
		{
			if (value != _skillFocusText)
			{
				_skillFocusText = value;
				OnPropertyChangedWithValue(value, "SkillFocusText");
			}
		}
	}

	[DataSourceProperty]
	public string AddFocusText
	{
		get
		{
			return _addFocusText;
		}
		set
		{
			if (value != _addFocusText)
			{
				_addFocusText = value;
				OnPropertyChangedWithValue(value, "AddFocusText");
			}
		}
	}

	[DataSourceProperty]
	public string SkillsText
	{
		get
		{
			return _skillsText;
		}
		set
		{
			if (value != _skillsText)
			{
				_skillsText = value;
				OnPropertyChangedWithValue(value, "SkillsText");
			}
		}
	}

	[DataSourceProperty]
	public int UnopenedPerksNumForCurrentCharacter
	{
		get
		{
			return _unopenedPerksNumForCurrentCharacter;
		}
		set
		{
			if (value != _unopenedPerksNumForCurrentCharacter)
			{
				_unopenedPerksNumForCurrentCharacter = value;
				OnPropertyChangedWithValue(value, "UnopenedPerksNumForCurrentCharacter");
			}
		}
	}

	[DataSourceProperty]
	public bool HasUnopenedPerksForCurrentCharacter
	{
		get
		{
			return _hasUnopenedPerksForCurrentCharacter;
		}
		set
		{
			if (value != _hasUnopenedPerksForCurrentCharacter)
			{
				_hasUnopenedPerksForCurrentCharacter = value;
				OnPropertyChangedWithValue(value, "HasUnopenedPerksForCurrentCharacter");
			}
		}
	}

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

	[DataSourceProperty]
	public InputKeyItemVM PreviousCharacterInputKey
	{
		get
		{
			return _previousCharacterInputKey;
		}
		set
		{
			if (value != _previousCharacterInputKey)
			{
				_previousCharacterInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousCharacterInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextCharacterInputKey
	{
		get
		{
			return _nextCharacterInputKey;
		}
		set
		{
			if (value != _nextCharacterInputKey)
			{
				_nextCharacterInputKey = value;
				OnPropertyChangedWithValue(value, "NextCharacterInputKey");
			}
		}
	}

	public CharacterDeveloperVM(Action closeCharacterDeveloper)
	{
		_closeCharacterDeveloper = closeCharacterDeveloper;
		TutorialNotification = new ElementNotificationVM();
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		_heroList = new List<CharacterDeveloperHeroItemVM>();
		HeroList = new ReadOnlyCollection<CharacterDeveloperHeroItemVM>(_heroList);
		foreach (Hero applicableHero in GetApplicableHeroes())
		{
			if (applicableHero == null)
			{
				Debug.FailedAssert("Trying to use null hero for character developer", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\CharacterDeveloperVM.cs", ".ctor", 40);
			}
			else if (applicableHero.HeroDeveloper == null)
			{
				Debug.FailedAssert("Hero does not have hero developer", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\CharacterDeveloperVM.cs", ".ctor", 46);
			}
			else if (applicableHero == Hero.MainHero)
			{
				_heroList.Insert(0, new CharacterDeveloperHeroItemVM(applicableHero, OnPerkSelection));
			}
			else
			{
				_heroList.Add(new CharacterDeveloperHeroItemVM(applicableHero, OnPerkSelection));
			}
		}
		_heroIndex = 0;
		CharacterList = new SelectorVM<SelectorItemVM>(new List<string>(), _heroIndex, OnCharacterSelection);
		RefreshCharacterSelector();
		IsPlayerAccompanied = _heroList.Count > 1;
		SetCurrentHero(_heroList[_heroIndex]);
		_viewDataTracker.ClearCharacterNotification();
		UnopenedPerksNumForCurrentCharacter = CurrentCharacter.GetNumberOfUnselectedPerks();
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DoneLbl = GameTexts.FindText("str_done").ToString();
		ResetLbl = GameTexts.FindText("str_reset").ToString();
		CancelLbl = GameTexts.FindText("str_cancel").ToString();
		SkillsText = GameTexts.FindText("str_skills").ToString();
		AddFocusText = GameTexts.FindText("str_add_focus").ToString();
		UnspentCharacterPointsText = GameTexts.FindText("str_character_unspent_character_points").ToString();
		TraitsText = new TextObject("{=FYJC7cDD}Trait(s)").ToString();
		PartyRoleText = new TextObject("{=9FJi2SaE}Party Role").ToString();
		ResetHint = new HintViewModel(GameTexts.FindText("str_reset"));
		SkillFocusText = GameTexts.FindText("str_character_skill_focus").ToString();
		FocusVisualHint = new HintViewModel(new TextObject("{=GwA9oUBC}Your skill focus determines the rate your skill increases with practice"));
		GameTexts.SetVariable("FOCUS_PER_LEVEL", Campaign.Current.Models.CharacterDevelopmentModel.FocusPointsPerLevel);
		GameTexts.SetVariable("ATTRIBUTE_EVERY_LEVEL", Campaign.Current.Models.CharacterDevelopmentModel.LevelsPerAttributePoint);
		UnspentCharacterPointsHint = new HintViewModel(GameTexts.FindText("str_character_points_how_to_get"));
		UnspentAttributePointsHint = new HintViewModel(GameTexts.FindText("str_attribute_points_how_to_get"));
		LevelHint = new HintViewModel(GameTexts.FindText("str_level_tag"));
		UnopenedPerksHint = new HintViewModel(new TextObject("{=jmLg6HQh}Number of available perk unlocks."));
		SetPreviousCharacterHint();
		SetNextCharacterHint();
		CharacterList.RefreshValues();
		CurrentCharacter.RefreshValues();
	}

	private void SetPreviousCharacterHint()
	{
		PreviousCharacterHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("HOTKEY", GetPreviousCharacterKeyText());
			GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_prev_char"));
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
	}

	private void SetNextCharacterHint()
	{
		NextCharacterHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("HOTKEY", GetNextCharacterKeyText());
			GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_next_char"));
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
	}

	public void SelectHero(Hero hero)
	{
		for (int i = 0; i < _heroList.Count; i++)
		{
			if (_heroList[i].Hero == hero)
			{
				_heroIndex = i;
				RefreshCharacterSelector();
				break;
			}
		}
	}

	private void OnCharacterSelection(SelectorVM<SelectorItemVM> newIndex)
	{
		if (newIndex.SelectedIndex >= 0 && newIndex.SelectedIndex < _heroList.Count)
		{
			_heroIndex = newIndex.SelectedIndex;
			SetCurrentHero(_heroList[_heroIndex]);
			UnopenedPerksNumForCurrentCharacter = CurrentCharacter.GetNumberOfUnselectedPerks();
			HasUnopenedPerksForCurrentCharacter = _heroList[_heroIndex].GetNumberOfUnselectedPerks() > 0;
		}
	}

	private void OnPerkSelection()
	{
		RefreshCharacterSelector();
	}

	private void RefreshCharacterSelector()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < _heroList.Count; i++)
		{
			list.Add(_heroList[i].HeroNameText);
		}
		CharacterList.Refresh(list, _heroIndex, OnCharacterSelection);
	}

	public void ExecuteReset()
	{
		foreach (CharacterDeveloperHeroItemVM hero in _heroList)
		{
			hero.ResetChanges(isCancel: false);
		}
		RefreshCharacterSelector();
	}

	public void ExecuteDone()
	{
		ApplyAllChanges();
		_closeCharacterDeveloper();
	}

	public void ExecuteCancel()
	{
		foreach (CharacterDeveloperHeroItemVM hero in _heroList)
		{
			hero.ResetChanges(isCancel: true);
		}
		_closeCharacterDeveloper();
	}

	private void SetCurrentHero(CharacterDeveloperHeroItemVM currentHero)
	{
		SkillObject prevSkill = CurrentCharacter?.Skills.FirstOrDefault((SkillVM s) => s.IsInspected)?.Skill;
		CurrentCharacter = currentHero;
		if (prevSkill != null)
		{
			CurrentCharacter?.SetCurrentSkill(CurrentCharacter.Skills.FirstOrDefault((SkillVM s) => s.Skill == prevSkill));
		}
	}

	public void ApplyAllChanges()
	{
		foreach (CharacterDeveloperHeroItemVM hero in _heroList)
		{
			hero.ApplyChanges();
		}
	}

	public bool IsThereAnyChanges()
	{
		return _heroList.Any((CharacterDeveloperHeroItemVM c) => c.IsThereAnyChanges());
	}

	private List<Hero> GetApplicableHeroes()
	{
		List<Hero> list = new List<Hero>();
		Func<Hero, bool> func = (Hero x) => x != null && x.HeroState != Hero.CharacterStates.Disabled && x.IsAlive && !x.IsChild;
		IEnumerable<Hero> enumerable = Clan.PlayerClan?.Heroes;
		foreach (Hero item in enumerable ?? Enumerable.Empty<Hero>())
		{
			if (func(item))
			{
				list.Add(item);
			}
		}
		enumerable = Clan.PlayerClan?.Companions;
		foreach (Hero item2 in enumerable ?? Enumerable.Empty<Hero>())
		{
			if (func(item2) && !list.Contains(item2))
			{
				list.Add(item2);
			}
		}
		return list;
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (!(obj.NewNotificationElementID != _latestTutorialElementID))
		{
			return;
		}
		if (_latestTutorialElementID != null)
		{
			TutorialNotification.ElementID = string.Empty;
			if (_isActivePerkHighlightsApplied)
			{
				SetAvailablePerksHighlightState(state: false);
				_isActivePerkHighlightsApplied = false;
			}
		}
		_latestTutorialElementID = obj.NewNotificationElementID;
		if (_latestTutorialElementID == null)
		{
			return;
		}
		TutorialNotification.ElementID = _latestTutorialElementID;
		if (!_isActivePerkHighlightsApplied && _latestTutorialElementID == _availablePerksHighlighId)
		{
			SetAvailablePerksHighlightState(state: true);
			_isActivePerkHighlightsApplied = true;
			CurrentCharacter.Skills.FirstOrDefault((SkillVM s) => s.NumOfUnopenedPerks > 0)?.ExecuteInspect();
		}
	}

	private void SetAvailablePerksHighlightState(bool state)
	{
		foreach (SkillVM skill in CurrentCharacter.Skills)
		{
			foreach (PerkVM perk in skill.Perks)
			{
				if (state && perk.CurrentState == PerkVM.PerkStates.EarnedButNotSelected)
				{
					perk.IsTutorialHighlightEnabled = true;
				}
				else if (!state)
				{
					perk.IsTutorialHighlightEnabled = false;
				}
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		CancelInputKey.OnFinalize();
		DoneInputKey.OnFinalize();
		PreviousCharacterInputKey.OnFinalize();
		NextCharacterInputKey.OnFinalize();
		_heroList.ForEach(delegate(CharacterDeveloperHeroItemVM h)
		{
			h.OnFinalize();
		});
	}

	private TextObject GetPreviousCharacterKeyText()
	{
		if (PreviousCharacterInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(PreviousCharacterInputKey.KeyID);
	}

	private TextObject GetNextCharacterKeyText()
	{
		if (NextCharacterInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(NextCharacterInputKey.KeyID);
	}

	public void SetCancelInputKey(HotKey gameKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(gameKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetResetInputKey(HotKey hotKey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetPreviousCharacterInputKey(HotKey hotKey)
	{
		PreviousCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		SetPreviousCharacterHint();
	}

	public void SetNextCharacterInputKey(HotKey hotKey)
	{
		NextCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		SetNextCharacterHint();
	}

	public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
	{
		_getKeyTextFromKeyId = getKeyTextFromKeyId;
	}
}
