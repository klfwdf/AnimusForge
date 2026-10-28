using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education;

public class EducationVM : ViewModel
{
	private readonly Action<bool> _onDone;

	private readonly Action<EducationCampaignBehavior.EducationCharacterProperties[]> _onOptionSelect;

	private readonly Action<List<BasicCharacterObject>, List<Equipment>> _sendPossibleCharactersAndEquipment;

	private readonly IEducationLogic _educationBehavior;

	private readonly Hero _child;

	private readonly TextObject _nextPageTextObj = new TextObject("{=Rvr1bcu8}Next");

	private readonly TextObject _previousPageTextObj = new TextObject("{=WXAaWZVf}Previous");

	private readonly int _pageCount;

	private readonly List<string> _selectedOptions = new List<string>();

	private TextObject _currentPageTitleTextObj;

	private TextObject _currentPageDescriptionTextObj;

	private TextObject _currentPageInstructionTextObj;

	private object _latestOptionId;

	private int _currentPageIndex;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private string _stageTitleText;

	private string _chooseText;

	private string _pageDescriptionText;

	private string _optionEffectText;

	private string _optionDescriptionText;

	private string _nextText;

	private string _previousText;

	private bool _canAdvance;

	private bool _canGoBack;

	private bool _onlyHasOneOption;

	private MBBindingList<EducationOptionVM> _options;

	private EducationGainedPropertiesVM _gainedPropertiesController;

	private EducationReviewVM _review;

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
	public string StageTitleText
	{
		get
		{
			return _stageTitleText;
		}
		set
		{
			if (value != _stageTitleText)
			{
				_stageTitleText = value;
				OnPropertyChangedWithValue(value, "StageTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ChooseText
	{
		get
		{
			return _chooseText;
		}
		set
		{
			if (value != _chooseText)
			{
				_chooseText = value;
				OnPropertyChangedWithValue(value, "ChooseText");
			}
		}
	}

	[DataSourceProperty]
	public string PageDescriptionText
	{
		get
		{
			return _pageDescriptionText;
		}
		set
		{
			if (value != _pageDescriptionText)
			{
				_pageDescriptionText = value;
				OnPropertyChangedWithValue(value, "PageDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string OptionEffectText
	{
		get
		{
			return _optionEffectText;
		}
		set
		{
			if (value != _optionEffectText)
			{
				_optionEffectText = value;
				OnPropertyChangedWithValue(value, "OptionEffectText");
			}
		}
	}

	[DataSourceProperty]
	public string OptionDescriptionText
	{
		get
		{
			return _optionDescriptionText;
		}
		set
		{
			if (value != _optionDescriptionText)
			{
				_optionDescriptionText = value;
				OnPropertyChangedWithValue(value, "OptionDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string NextText
	{
		get
		{
			return _nextText;
		}
		set
		{
			if (value != _nextText)
			{
				_nextText = value;
				OnPropertyChangedWithValue(value, "NextText");
			}
		}
	}

	[DataSourceProperty]
	public string PreviousText
	{
		get
		{
			return _previousText;
		}
		set
		{
			if (value != _previousText)
			{
				_previousText = value;
				OnPropertyChangedWithValue(value, "PreviousText");
			}
		}
	}

	[DataSourceProperty]
	public bool CanAdvance
	{
		get
		{
			return _canAdvance;
		}
		set
		{
			if (value != _canAdvance)
			{
				_canAdvance = value;
				OnPropertyChangedWithValue(value, "CanAdvance");
			}
		}
	}

	[DataSourceProperty]
	public bool CanGoBack
	{
		get
		{
			return _canGoBack;
		}
		set
		{
			if (value != _canGoBack)
			{
				_canGoBack = value;
				OnPropertyChangedWithValue(value, "CanGoBack");
			}
		}
	}

	[DataSourceProperty]
	public bool OnlyHasOneOption
	{
		get
		{
			return _onlyHasOneOption;
		}
		set
		{
			if (value != _onlyHasOneOption)
			{
				_onlyHasOneOption = value;
				OnPropertyChangedWithValue(value, "OnlyHasOneOption");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EducationOptionVM> Options
	{
		get
		{
			return _options;
		}
		set
		{
			if (value != _options)
			{
				_options = value;
				OnPropertyChangedWithValue(value, "Options");
			}
		}
	}

	[DataSourceProperty]
	public EducationGainedPropertiesVM GainedPropertiesController
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
	public EducationReviewVM Review
	{
		get
		{
			return _review;
		}
		set
		{
			if (value != _review)
			{
				_review = value;
				OnPropertyChangedWithValue(value, "Review");
			}
		}
	}

	public EducationVM(Hero child, Action<bool> onDone, Action<EducationCampaignBehavior.EducationCharacterProperties[]> onOptionSelect, Action<List<BasicCharacterObject>, List<Equipment>> sendPossibleCharactersAndEquipment)
	{
		_onDone = onDone;
		_onOptionSelect = onOptionSelect;
		_sendPossibleCharactersAndEquipment = sendPossibleCharactersAndEquipment;
		_child = child;
		_educationBehavior = Campaign.Current.GetCampaignBehavior<IEducationLogic>();
		_educationBehavior.GetStageProperties(_child, out var pageCount);
		_pageCount = pageCount + 1;
		GainedPropertiesController = new EducationGainedPropertiesVM(_child, _pageCount);
		Options = new MBBindingList<EducationOptionVM>();
		Review = new EducationReviewVM(_pageCount);
		CanGoBack = true;
		InitWithStageIndex(0);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		StageTitleText = _currentPageTitleTextObj?.ToString() ?? "";
		PageDescriptionText = _currentPageDescriptionTextObj?.ToString() ?? "";
		ChooseText = _currentPageInstructionTextObj?.ToString() ?? "";
		Options.ApplyActionOnAllItems(delegate(EducationOptionVM o)
		{
			o.RefreshValues();
		});
		foreach (EducationOptionVM option in Options)
		{
			if (option.IsSelected)
			{
				OptionEffectText = option.OptionEffect;
				OptionDescriptionText = option.OptionDescription;
			}
		}
	}

	private void InitWithStageIndex(int index)
	{
		_latestOptionId = null;
		CanAdvance = false;
		_currentPageIndex = index;
		OptionEffectText = "";
		OptionDescriptionText = "";
		Options.Clear();
		if (index < _pageCount - 1)
		{
			List<BasicCharacterObject> list = new List<BasicCharacterObject>();
			List<Equipment> list2 = new List<Equipment>();
			_educationBehavior.GetPageProperties(_child, _selectedOptions.Take(index).ToList(), out var title, out var description, out var instruction, out var defaultProperties, out var availableOptions);
			_currentPageTitleTextObj = title;
			_currentPageDescriptionTextObj = description;
			_currentPageInstructionTextObj = instruction;
			for (int i = 0; i < availableOptions.Length; i++)
			{
				_educationBehavior.GetOptionProperties(_child, availableOptions[i], _selectedOptions, out var optionTitle, out var description2, out var effect, out var attributes, out var skills, out var focusPoints, out var characterProperties);
				Options.Add(new EducationOptionVM(OnOptionSelect, availableOptions[i], optionTitle, description2, effect, isSelected: false, attributes, skills, focusPoints, characterProperties));
				EducationCampaignBehavior.EducationCharacterProperties[] array = characterProperties;
				for (int j = 0; j < array.Length; j++)
				{
					EducationCampaignBehavior.EducationCharacterProperties educationCharacterProperties = array[j];
					if (educationCharacterProperties.Character != null && !list.Contains(educationCharacterProperties.Character))
					{
						list.Add(educationCharacterProperties.Character);
					}
					if (educationCharacterProperties.Equipment != null && !list2.Contains(educationCharacterProperties.Equipment))
					{
						list2.Add(educationCharacterProperties.Equipment);
					}
				}
			}
			OnlyHasOneOption = Options.Count == 1;
			if (_selectedOptions.Count > index)
			{
				string item = _selectedOptions[index];
				int num = availableOptions.IndexOf(item);
				if (num >= 0)
				{
					_onOptionSelect?.Invoke(Options[num].CharacterProperties);
					if (index == _currentPageIndex)
					{
						Options[num].ExecuteAction();
						CanAdvance = true;
					}
				}
			}
			else
			{
				EducationCampaignBehavior.EducationCharacterProperties[] array2 = new EducationCampaignBehavior.EducationCharacterProperties[(defaultProperties == null) ? 1 : defaultProperties.Length];
				for (int k = 0; k < ((defaultProperties != null) ? defaultProperties.Length : 0); k++)
				{
					array2[k] = defaultProperties[k];
					if (array2[k].Character != null && !list.Contains(array2[k].Character))
					{
						list.Add(array2[k].Character);
					}
					if (array2[k].Equipment != null && !list2.Contains(array2[k].Equipment))
					{
						list2.Add(array2[k].Equipment);
					}
				}
				_onOptionSelect?.Invoke(array2);
			}
			if (OnlyHasOneOption)
			{
				Options[0].ExecuteAction();
			}
			_sendPossibleCharactersAndEquipment(list, list2);
		}
		else
		{
			_currentPageTitleTextObj = new TextObject("{=Ck9HT8fQ}Summary");
			_currentPageInstructionTextObj = null;
			_currentPageDescriptionTextObj = null;
			OnlyHasOneOption = false;
			CanAdvance = true;
		}
		StageTitleText = _currentPageTitleTextObj?.ToString() ?? "";
		ChooseText = _currentPageInstructionTextObj?.ToString() ?? "";
		PageDescriptionText = _currentPageDescriptionTextObj?.ToString() ?? "";
		if (_currentPageIndex == 0)
		{
			NextText = _nextPageTextObj.ToString();
			PreviousText = GameTexts.FindText("str_exit").ToString();
		}
		else if (_currentPageIndex == _pageCount - 1)
		{
			NextText = GameTexts.FindText("str_done").ToString();
			PreviousText = _previousPageTextObj.ToString();
		}
		else
		{
			NextText = _nextPageTextObj.ToString();
			PreviousText = _previousPageTextObj.ToString();
		}
		UpdateGainedProperties();
		Review.SetCurrentPage(_currentPageIndex);
	}

	private void OnOptionSelect(object optionIdAsObj)
	{
		if (optionIdAsObj != _latestOptionId)
		{
			string optionId = (string)optionIdAsObj;
			EducationOptionVM educationOptionVM = Options.FirstOrDefault((EducationOptionVM o) => (string)o.Identifier == optionId);
			Options.ApplyActionOnAllItems(delegate(EducationOptionVM o)
			{
				o.IsSelected = false;
			});
			educationOptionVM.IsSelected = true;
			_ = educationOptionVM.ActionText;
			if (_currentPageIndex == _selectedOptions.Count)
			{
				_selectedOptions.Add(optionId);
			}
			else if (_currentPageIndex < _selectedOptions.Count)
			{
				_selectedOptions[_currentPageIndex] = optionId;
			}
			else
			{
				Debug.FailedAssert("Skipped a stage for education!!!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Education\\EducationVM.cs", "OnOptionSelect", 210);
			}
			OptionEffectText = educationOptionVM.OptionEffect;
			OptionDescriptionText = educationOptionVM.OptionDescription;
			_onOptionSelect?.Invoke(educationOptionVM.CharacterProperties);
			UpdateGainedProperties();
			CanAdvance = true;
			_latestOptionId = optionIdAsObj;
			Review.SetGainForStage(_currentPageIndex, OptionEffectText);
		}
	}

	private void UpdateGainedProperties()
	{
		GainedPropertiesController.UpdateWithSelections(_selectedOptions, _currentPageIndex);
	}

	public void ExecuteNextStage()
	{
		if (_currentPageIndex + 1 < _pageCount)
		{
			InitWithStageIndex(_currentPageIndex + 1);
			return;
		}
		_educationBehavior.Finalize(_child, _selectedOptions);
		_onDone?.Invoke(obj: false);
	}

	public void ExecutePreviousStage()
	{
		if (_currentPageIndex > 0)
		{
			InitWithStageIndex(_currentPageIndex - 1);
		}
		else if (_currentPageIndex == 0)
		{
			_onDone?.Invoke(obj: true);
		}
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
