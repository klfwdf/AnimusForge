using System;
using System.Collections.Generic;
using SandBox.AdvancedStartOptions;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.CampaignStartingOptions;

public class CampaignStartingOptionsVM : ViewModel
{
	private readonly Action<SandBox.AdvancedStartOptions.AdvancedStartOptions> _onConfirm;

	private readonly Action _onClose;

	private readonly SandBox.AdvancedStartOptions.AdvancedStartOptions _stagedOptions;

	private MBBindingList<StartingOptionCategoryVM> _categories;

	private string _titleLabel;

	private string _startGameLabel;

	private string _backLabel;

	private string _confirmLabel;

	private string _defaultStartTitle;

	private string _defaultStartDescription;

	private string _advancedStartTitle;

	private string _advancedStartDescription;

	private string _advancedStartSecondaryDescription;

	private bool _isASOShown;

	private bool _isDefaultStartSelected;

	private bool _isAdvancedStartSelected;

	private bool _canConfirmStartType;

	private MBBindingList<StartingOptionTitleDescriptionTupleVM> _relevantOptionTexts;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	public StartingOptionVM FocusedOption { get; private set; }

	[DataSourceProperty]
	public bool IsASOShown
	{
		get
		{
			return _isASOShown;
		}
		set
		{
			if (value != _isASOShown)
			{
				_isASOShown = value;
				OnPropertyChangedWithValue(value, "IsASOShown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDefaultStartSelected
	{
		get
		{
			return _isDefaultStartSelected;
		}
		set
		{
			if (value != _isDefaultStartSelected)
			{
				_isDefaultStartSelected = value;
				OnPropertyChangedWithValue(value, "IsDefaultStartSelected");
				CanConfirmStartType = value || IsAdvancedStartSelected;
			}
		}
	}

	[DataSourceProperty]
	public bool IsAdvancedStartSelected
	{
		get
		{
			return _isAdvancedStartSelected;
		}
		set
		{
			if (value != _isAdvancedStartSelected)
			{
				_isAdvancedStartSelected = value;
				OnPropertyChangedWithValue(value, "IsAdvancedStartSelected");
				CanConfirmStartType = value || IsDefaultStartSelected;
			}
		}
	}

	[DataSourceProperty]
	public bool CanConfirmStartType
	{
		get
		{
			return _canConfirmStartType;
		}
		set
		{
			if (value != _canConfirmStartType)
			{
				_canConfirmStartType = value;
				OnPropertyChangedWithValue(value, "CanConfirmStartType");
			}
		}
	}

	[DataSourceProperty]
	public string DefaultStartTitle
	{
		get
		{
			return _defaultStartTitle;
		}
		set
		{
			if (value != _defaultStartTitle)
			{
				_defaultStartTitle = value;
				OnPropertyChangedWithValue(value, "DefaultStartTitle");
			}
		}
	}

	[DataSourceProperty]
	public string DefaultStartDescription
	{
		get
		{
			return _defaultStartDescription;
		}
		set
		{
			if (value != _defaultStartDescription)
			{
				_defaultStartDescription = value;
				OnPropertyChangedWithValue(value, "DefaultStartDescription");
			}
		}
	}

	[DataSourceProperty]
	public string AdvancedStartTitle
	{
		get
		{
			return _advancedStartTitle;
		}
		set
		{
			if (value != _advancedStartTitle)
			{
				_advancedStartTitle = value;
				OnPropertyChangedWithValue(value, "AdvancedStartTitle");
			}
		}
	}

	[DataSourceProperty]
	public string AdvancedStartDescription
	{
		get
		{
			return _advancedStartDescription;
		}
		set
		{
			if (value != _advancedStartDescription)
			{
				_advancedStartDescription = value;
				OnPropertyChangedWithValue(value, "AdvancedStartDescription");
			}
		}
	}

	[DataSourceProperty]
	public string AdvancedStartSecondaryDescription
	{
		get
		{
			return _advancedStartSecondaryDescription;
		}
		set
		{
			if (value != _advancedStartSecondaryDescription)
			{
				_advancedStartSecondaryDescription = value;
				OnPropertyChangedWithValue(value, "AdvancedStartSecondaryDescription");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StartingOptionCategoryVM> Categories
	{
		get
		{
			return _categories;
		}
		set
		{
			if (value != _categories)
			{
				_categories = value;
				OnPropertyChangedWithValue(value, "Categories");
			}
		}
	}

	[DataSourceProperty]
	public string TitleLabel
	{
		get
		{
			return _titleLabel;
		}
		set
		{
			if (value != _titleLabel)
			{
				_titleLabel = value;
				OnPropertyChangedWithValue(value, "TitleLabel");
			}
		}
	}

	[DataSourceProperty]
	public string StartGameLabel
	{
		get
		{
			return _startGameLabel;
		}
		set
		{
			if (value != _startGameLabel)
			{
				_startGameLabel = value;
				OnPropertyChangedWithValue(value, "StartGameLabel");
			}
		}
	}

	[DataSourceProperty]
	public string BackLabel
	{
		get
		{
			return _backLabel;
		}
		set
		{
			if (value != _backLabel)
			{
				_backLabel = value;
				OnPropertyChangedWithValue(value, "BackLabel");
			}
		}
	}

	[DataSourceProperty]
	public string ConfirmLabel
	{
		get
		{
			return _confirmLabel;
		}
		set
		{
			if (value != _confirmLabel)
			{
				_confirmLabel = value;
				OnPropertyChangedWithValue(value, "ConfirmLabel");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StartingOptionTitleDescriptionTupleVM> RelevantOptionTexts
	{
		get
		{
			return _relevantOptionTexts;
		}
		set
		{
			if (value != _relevantOptionTexts)
			{
				_relevantOptionTexts = value;
				OnPropertyChangedWithValue(value, "RelevantOptionTexts");
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

	public CampaignStartingOptionsVM(SandBox.AdvancedStartOptions.AdvancedStartOptions startOptions, Action<SandBox.AdvancedStartOptions.AdvancedStartOptions> onConfirm, Action onClose)
	{
		_onConfirm = onConfirm;
		_onClose = onClose;
		_stagedOptions = startOptions;
		Categories = new MBBindingList<StartingOptionCategoryVM>();
		BuildCategories();
		RelevantOptionTexts = new MBBindingList<StartingOptionTitleDescriptionTupleVM>();
		StartingOptionVM.OnOptionFocusBegin += OnOptionFocusBegin;
		StartingOptionVM.OnOptionFocusEnd += OnOptionFocusEnd;
		StartingOptionVM.OnOptionChanged += OnOptionChanged;
		OnOptionChanged();
		RefreshValues();
	}

	private void BuildCategories()
	{
		Dictionary<string, StartingOptionCategoryVM> dictionary = new Dictionary<string, StartingOptionCategoryVM>();
		IReadOnlyList<AdvancedStartOption> allOptions = _stagedOptions.GetAllOptions();
		for (int i = 0; i < allOptions.Count; i++)
		{
			AdvancedStartOption advancedStartOption = allOptions[i];
			string categoryId = advancedStartOption.CategoryId;
			if (string.IsNullOrEmpty(categoryId))
			{
				Debug.FailedAssert("Empty category id", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\CampaignStartingOptions\\CampaignStartingOptionsVM.cs", "BuildCategories", 54);
				continue;
			}
			if (!dictionary.TryGetValue(categoryId, out var value))
			{
				TextObject categoryName = Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_category_name", categoryId);
				value = CreateCategory(categoryId, categoryName);
				dictionary.Add(categoryId, value);
				Categories.Add(value);
			}
			StartingOptionVM startingOptionVM = new StartingOptionVM(advancedStartOption, _stagedOptions);
			if (advancedStartOption.StringId == "Seed")
			{
				startingOptionVM.AllowRandomization = true;
				if (!_stagedOptions.HasAnyChange())
				{
					startingOptionVM.ExecuteRandomize();
				}
			}
			value.Options.Add(startingOptionVM);
		}
	}

	private static StartingOptionCategoryVM CreateCategory(string categoryId, TextObject categoryName)
	{
		if (!(categoryId == "general"))
		{
			if (categoryId == "globalmodifiers")
			{
				return new GlobalModifiersCategoryVM(categoryId, categoryName);
			}
			return new StartingOptionCategoryVM(categoryId, categoryName);
		}
		return new GeneralCategoryVM(categoryId, categoryName);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleLabel = new TextObject("{=LuaC2Lz2}Advanced Starting Options").ToString();
		StartGameLabel = new TextObject("{=lBQXP6Wj}Start Game").ToString();
		BackLabel = new TextObject("{=E1OwmQFb}Back").ToString();
		ConfirmLabel = new TextObject("{=5Unqsx3N}Confirm").ToString();
		DefaultStartTitle = new TextObject("{=*}Default Start").ToString();
		DefaultStartDescription = new TextObject("{=*}Default starting options, handcrafted and maintained.").ToString();
		AdvancedStartTitle = new TextObject("{=LuaC2Lz2}Advanced Starting Options").ToString();
		AdvancedStartDescription = new TextObject("{=*}Tailor your starting options to your liking.").ToString();
		AdvancedStartSecondaryDescription = new TextObject("{=*}Game balance may be affected. Recommended for experienced players only.").ToString();
		Categories.ApplyActionOnAllItems(delegate(StartingOptionCategoryVM x)
		{
			x.RefreshValues();
		});
		RefreshRelevantOptionTexts();
	}

	public void SetRandomizeInputKey(HotKey hotkey)
	{
		for (int i = 0; i < Categories.Count; i++)
		{
			StartingOptionCategoryVM startingOptionCategoryVM = Categories[i];
			for (int j = 0; j < startingOptionCategoryVM.Options.Count; j++)
			{
				StartingOptionVM startingOptionVM = startingOptionCategoryVM.Options[j];
				if (startingOptionVM.AllowRandomization)
				{
					startingOptionVM.SetRandomizeInputKey(hotkey);
				}
			}
		}
	}

	private void OnOptionFocusBegin(StartingOptionVM optionVM)
	{
		if (optionVM != null)
		{
			FocusedOption = optionVM;
		}
	}

	private void OnOptionFocusEnd(StartingOptionVM optionVM)
	{
		if (optionVM != null && FocusedOption == optionVM)
		{
			FocusedOption = null;
		}
	}

	public void ExecuteSelectDefaultStart()
	{
		IsDefaultStartSelected = true;
		IsAdvancedStartSelected = false;
	}

	public void ExecuteSelectAdvancedStart()
	{
		IsDefaultStartSelected = false;
		IsAdvancedStartSelected = true;
	}

	public void ExecuteConfirm()
	{
		if (IsASOShown)
		{
			_onConfirm?.Invoke(_stagedOptions);
		}
		else if (CanConfirmStartType)
		{
			if (IsDefaultStartSelected)
			{
				_onConfirm?.Invoke(new SandBox.AdvancedStartOptions.AdvancedStartOptions());
			}
			else if (IsAdvancedStartSelected)
			{
				IsASOShown = true;
			}
		}
	}

	public void ExecuteCancel()
	{
		if (IsASOShown)
		{
			IsASOShown = false;
		}
		else
		{
			_onClose?.Invoke();
		}
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	private void OnOptionChanged()
	{
		Categories.ApplyActionOnAllItems(delegate(StartingOptionCategoryVM x)
		{
			x.UpdateOptionStates();
		});
		RefreshRelevantOptionTexts();
	}

	private void RefreshRelevantOptionTexts()
	{
		RelevantOptionTexts.Clear();
		for (int i = 0; i < Categories.Count; i++)
		{
			StartingOptionCategoryVM startingOptionCategoryVM = Categories[i];
			if (!string.IsNullOrEmpty(startingOptionCategoryVM.DescriptionText))
			{
				RelevantOptionTexts.Add(new StartingOptionTitleDescriptionTupleVM(startingOptionCategoryVM.Name, startingOptionCategoryVM.DescriptionText));
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		StartingOptionVM.OnOptionFocusBegin -= OnOptionFocusBegin;
		StartingOptionVM.OnOptionFocusEnd -= OnOptionFocusEnd;
		StartingOptionVM.OnOptionChanged -= OnOptionChanged;
		Categories.ApplyActionOnAllItems(delegate(StartingOptionCategoryVM x)
		{
			x.OnFinalize();
		});
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
