using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationReviewStageVM : CharacterCreationStageBaseVM
{
	private bool _isBannerAndClanNameSet;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private MBBindingList<InputKeyItemVM> _cameraControlKeys;

	private string _name = "";

	private string _nameTextQuestion = "";

	private MBBindingList<CharacterCreationReviewStageItemVM> _reviewList;

	private CharacterCreationGainedPropertiesVM _gainedPropertiesController;

	private BannerImageIdentifierVM _clanBanner;

	private HintViewModel _cannotAdvanceReasonHint;

	private bool _characterGamepadControlsEnabled;

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
	public MBBindingList<InputKeyItemVM> CameraControlKeys
	{
		get
		{
			return _cameraControlKeys;
		}
		set
		{
			if (value != _cameraControlKeys)
			{
				_cameraControlKeys = value;
				OnPropertyChangedWithValue(value, "CameraControlKeys");
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
				CharacterCreationManager.CharacterCreationContent.SetMainCharacterName(value);
				OnPropertyChangedWithValue(value, "Name");
				OnRefresh();
			}
		}
	}

	[DataSourceProperty]
	public string NameTextQuestion
	{
		get
		{
			return _nameTextQuestion;
		}
		set
		{
			if (value != _nameTextQuestion)
			{
				_nameTextQuestion = value;
				OnPropertyChangedWithValue(value, "NameTextQuestion");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterCreationReviewStageItemVM> ReviewList
	{
		get
		{
			return _reviewList;
		}
		set
		{
			if (value != _reviewList)
			{
				_reviewList = value;
				OnPropertyChangedWithValue(value, "ReviewList");
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
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CannotAdvanceReasonHint
	{
		get
		{
			return _cannotAdvanceReasonHint;
		}
		set
		{
			if (value != _cannotAdvanceReasonHint)
			{
				_cannotAdvanceReasonHint = value;
				OnPropertyChangedWithValue(value, "CannotAdvanceReasonHint");
			}
		}
	}

	[DataSourceProperty]
	public bool CharacterGamepadControlsEnabled
	{
		get
		{
			return _characterGamepadControlsEnabled;
		}
		set
		{
			if (value != _characterGamepadControlsEnabled)
			{
				_characterGamepadControlsEnabled = value;
				OnPropertyChangedWithValue(value, "CharacterGamepadControlsEnabled");
			}
		}
	}

	public CharacterCreationReviewStageVM(CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText, bool isBannerAndClanNameSet)
		: base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
	{
		ReviewList = new MBBindingList<CharacterCreationReviewStageItemVM>();
		base.Title = new TextObject("{=txjiykNa}Review").ToString();
		base.Description = characterCreationManager.CharacterCreationContent.ReviewPageDescription.ToString();
		_isBannerAndClanNameSet = isBannerAndClanNameSet;
		CannotAdvanceReasonHint = new HintViewModel();
		ClanBanner = new BannerImageIdentifierVM(Clan.PlayerClan.Banner);
		GainedPropertiesController = new CharacterCreationGainedPropertiesVM(CharacterCreationManager);
		Name = characterCreationManager.CharacterCreationContent.MainCharacterName;
		NameTextQuestion = new TextObject("{=mHVmrwRQ}Enter your name").ToString();
		AddReviewedItems();
		CameraControlKeys = new MBBindingList<InputKeyItemVM>();
	}

	private void AddReviewedItems()
	{
		string text = string.Empty;
		CultureObject selectedCulture = CharacterCreationManager.CharacterCreationContent.SelectedCulture;
		IEnumerable<FeatObject> culturalFeats = selectedCulture.GetCulturalFeats((FeatObject x) => x.IsPositive);
		IEnumerable<FeatObject> culturalFeats2 = selectedCulture.GetCulturalFeats((FeatObject x) => !x.IsPositive);
		foreach (FeatObject item3 in culturalFeats)
		{
			GameTexts.SetVariable("STR1", text);
			GameTexts.SetVariable("STR2", item3.Description);
			text = GameTexts.FindText("str_string_newline_string").ToString();
		}
		foreach (FeatObject item4 in culturalFeats2)
		{
			GameTexts.SetVariable("STR1", text);
			GameTexts.SetVariable("STR2", item4.Description);
			text = GameTexts.FindText("str_string_newline_string").ToString();
		}
		CharacterCreationReviewStageItemVM item = new CharacterCreationReviewStageItemVM(new TextObject("{=K6GYskvJ}Culture:").ToString(), CharacterCreationManager.CharacterCreationContent.SelectedCulture.Name.ToString(), text);
		ReviewList.Add(item);
		foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> selectedOption in CharacterCreationManager.SelectedOptions)
		{
			NarrativeMenu key = selectedOption.Key;
			NarrativeMenuOption value = selectedOption.Value;
			item = new CharacterCreationReviewStageItemVM(key.Title.ToString(), value.Text.ToString(), value.PositiveEffectText.ToString());
			ReviewList.Add(item);
		}
		if (_isBannerAndClanNameSet)
		{
			CharacterCreationReviewStageItemVM item2 = new CharacterCreationReviewStageItemVM(new BannerImageIdentifierVM(Clan.PlayerClan.Banner, nineGrid: true), GameTexts.FindText("str_clan").ToString(), Clan.PlayerClan.Name.ToString(), null);
			ReviewList.Add(item2);
		}
	}

	public void ExecuteRandomizeName()
	{
		Name = NameGenerator.Current.GenerateFirstNameForPlayer(CharacterCreationManager.CharacterCreationContent.SelectedCulture, Hero.MainHero.IsFemale).ToString();
	}

	private void OnRefresh()
	{
		TextObject textObject = GameTexts.FindText("str_generic_character_firstname");
		textObject.SetTextVariable("CHARACTER_FIRSTNAME", new TextObject(Name));
		TextObject textObject2 = GameTexts.FindText("str_generic_character_name");
		textObject2.SetTextVariable("CHARACTER_NAME", new TextObject(Name));
		textObject2.SetTextVariable("CHARACTER_GENDER", Hero.MainHero.IsFemale ? 1 : 0);
		textObject.SetTextVariable("CHARACTER_GENDER", Hero.MainHero.IsFemale ? 1 : 0);
		Hero.MainHero.SetName(textObject2, textObject);
		base.CanAdvance = CanAdvanceToNextStage();
	}

	public override void OnNextStage()
	{
		_affirmativeAction();
	}

	public override void OnPreviousStage()
	{
		_negativeAction();
	}

	public override bool CanAdvanceToNextStage()
	{
		TextObject hintText = TextObject.GetEmpty();
		bool result = true;
		if (string.IsNullOrEmpty(Name) || string.IsNullOrWhiteSpace(Name))
		{
			hintText = new TextObject("{=IRcy3pWJ}Name cannot be empty");
			result = false;
		}
		Tuple<bool, string> tuple = CampaignUIHelper.IsStringApplicableForHeroName(Name);
		if (!tuple.Item1)
		{
			if (!string.IsNullOrEmpty(tuple.Item2))
			{
				hintText = new TextObject("{=!}" + tuple.Item2);
			}
			result = false;
		}
		CannotAdvanceReasonHint.HintText = hintText;
		return result;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
		foreach (InputKeyItemVM cameraControlKey in CameraControlKeys)
		{
			cameraControlKey.OnFinalize();
		}
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void AddCameraControlInputKey(HotKey hotKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameKey gameKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameAxisKey gameAxisKey, TextObject keyName)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), keyName, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}
}
