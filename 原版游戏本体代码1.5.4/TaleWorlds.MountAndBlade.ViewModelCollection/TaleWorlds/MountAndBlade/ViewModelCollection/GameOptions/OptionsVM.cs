using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Options;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GamepadOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class OptionsVM : ViewModel
{
	public enum OptionsDataType
	{
		None = -1,
		BooleanOption = 0,
		NumericOption = 1,
		MultipleSelectionOption = 3,
		InputOption = 4,
		ActionOption = 5
	}

	public enum OptionsMode
	{
		MainMenu,
		Singleplayer,
		Multiplayer
	}

	private readonly Action _onClose;

	private readonly Action _onBrightnessExecute;

	private readonly Action _onExposureExecute;

	private readonly StringOptionDataVM _overallOption;

	private readonly GenericOptionDataVM _dlssOption;

	private readonly List<GenericOptionDataVM> _dynamicResolutionOptions = new List<GenericOptionDataVM>();

	private readonly GenericOptionDataVM _refreshRateOption;

	private readonly GenericOptionDataVM _resolutionOption;

	private readonly GenericOptionDataVM _monitorOption;

	private readonly GenericOptionDataVM _displayModeOption;

	private readonly bool _autoHandleClose;

	protected readonly GroupedOptionCategoryVM _gameplayOptionCategory;

	private readonly GroupedOptionCategoryVM _audioOptionCategory;

	private readonly GroupedOptionCategoryVM _videoOptionCategory;

	protected readonly GameKeyOptionCategoryVM _gameKeyCategory;

	private readonly GamepadOptionCategoryVM _gamepadCategory;

	private bool _isInitialized;

	private Action<KeyOptionVM> _onKeybindRequest;

	protected readonly GroupedOptionCategoryVM _performanceOptionCategory;

	private readonly int _overallConfigCount = NativeSelectionOptionData.GetOptionsLimit(NativeOptions.NativeOptionsType.OverAll) - 1;

	private bool _isCancelling;

	private readonly IEnumerable<IOptionData> _performanceManagedOptions;

	protected readonly List<GroupedOptionCategoryVM> _groupedCategories;

	private readonly List<ViewModel> _categories;

	private int _categoryIndex;

	private string _optionsLbl;

	private string _cancelLbl;

	private string _doneLbl;

	private string _resetLbl;

	private bool _isDevelopmentMode;

	private bool _isConsole;

	private float _videoMemoryUsageNormalized;

	private string _videoMemoryUsageName;

	private string _videoMemoryUsageText;

	private BrightnessOptionVM _brightnessPopUp;

	private ExposureOptionVM _exposurePopUp;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _previousTabInputKey;

	private InputKeyItemVM _nextTabInputKey;

	private InputKeyItemVM _resetInputKey;

	public OptionsMode CurrentOptionsMode { get; private set; }

	[DataSourceProperty]
	public int CategoryIndex
	{
		get
		{
			return _categoryIndex;
		}
		set
		{
			if (value != _categoryIndex)
			{
				_categoryIndex = value;
				OnPropertyChangedWithValue(value, "CategoryIndex");
			}
		}
	}

	[DataSourceProperty]
	public string OptionsLbl
	{
		get
		{
			return _optionsLbl;
		}
		set
		{
			if (value != _optionsLbl)
			{
				_optionsLbl = value;
				OnPropertyChangedWithValue(value, "OptionsLbl");
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
	public bool IsConsole
	{
		get
		{
			return _isConsole;
		}
		set
		{
			if (value != _isConsole)
			{
				_isConsole = value;
				OnPropertyChangedWithValue(value, "IsConsole");
			}
		}
	}

	public bool IsDevelopmentMode
	{
		get
		{
			return _isDevelopmentMode;
		}
		set
		{
			if (value != _isDevelopmentMode)
			{
				_isDevelopmentMode = value;
				OnPropertyChangedWithValue(value, "IsDevelopmentMode");
			}
		}
	}

	[DataSourceProperty]
	public string VideoMemoryUsageName
	{
		get
		{
			return _videoMemoryUsageName;
		}
		set
		{
			if (_videoMemoryUsageName != value)
			{
				_videoMemoryUsageName = value;
				OnPropertyChangedWithValue(value, "VideoMemoryUsageName");
			}
		}
	}

	[DataSourceProperty]
	public string VideoMemoryUsageText
	{
		get
		{
			return _videoMemoryUsageText;
		}
		set
		{
			if (_videoMemoryUsageText != value)
			{
				_videoMemoryUsageText = value;
				OnPropertyChangedWithValue(value, "VideoMemoryUsageText");
			}
		}
	}

	[DataSourceProperty]
	public float VideoMemoryUsageNormalized
	{
		get
		{
			return _videoMemoryUsageNormalized;
		}
		set
		{
			if (_videoMemoryUsageNormalized != value)
			{
				_videoMemoryUsageNormalized = value;
				OnPropertyChangedWithValue(value, "VideoMemoryUsageNormalized");
			}
		}
	}

	[DataSourceProperty]
	public GameKeyOptionCategoryVM GameKeyOptionGroups => _gameKeyCategory;

	[DataSourceProperty]
	public GamepadOptionCategoryVM GamepadOptions => _gamepadCategory;

	[DataSourceProperty]
	public GroupedOptionCategoryVM PerformanceOptions => _performanceOptionCategory;

	[DataSourceProperty]
	public GroupedOptionCategoryVM AudioOptions => _audioOptionCategory;

	[DataSourceProperty]
	public GroupedOptionCategoryVM GameplayOptions => _gameplayOptionCategory;

	[DataSourceProperty]
	public GroupedOptionCategoryVM VideoOptions => _videoOptionCategory;

	[DataSourceProperty]
	public BrightnessOptionVM BrightnessPopUp
	{
		get
		{
			return _brightnessPopUp;
		}
		set
		{
			if (value != _brightnessPopUp)
			{
				_brightnessPopUp = value;
				OnPropertyChangedWithValue(value, "BrightnessPopUp");
			}
		}
	}

	[DataSourceProperty]
	public ExposureOptionVM ExposurePopUp
	{
		get
		{
			return _exposurePopUp;
		}
		set
		{
			if (value != _exposurePopUp)
			{
				_exposurePopUp = value;
				OnPropertyChangedWithValue(value, "ExposurePopUp");
			}
		}
	}

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
	public InputKeyItemVM PreviousTabInputKey
	{
		get
		{
			return _previousTabInputKey;
		}
		set
		{
			if (value != _previousTabInputKey)
			{
				_previousTabInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousTabInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextTabInputKey
	{
		get
		{
			return _nextTabInputKey;
		}
		set
		{
			if (value != _nextTabInputKey)
			{
				_nextTabInputKey = value;
				OnPropertyChangedWithValue(value, "NextTabInputKey");
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

	public OptionsVM(bool autoHandleClose, OptionsMode optionsMode, Action<KeyOptionVM> onKeybindRequest, Action onBrightnessExecute = null, Action onExposureExecute = null)
	{
		_onKeybindRequest = onKeybindRequest;
		_autoHandleClose = autoHandleClose;
		CurrentOptionsMode = optionsMode;
		_onBrightnessExecute = onBrightnessExecute;
		_onExposureExecute = onExposureExecute;
		_groupedCategories = new List<GroupedOptionCategoryVM>();
		NativeOptions.RefreshOptionsData();
		bool isMultiplayer = CurrentOptionsMode == OptionsMode.Multiplayer;
		bool isMainMenu = CurrentOptionsMode == OptionsMode.MainMenu;
		_gameplayOptionCategory = new GroupedOptionCategoryVM(this, new TextObject("{=2zcrC0h1}Gameplay"), OptionsProvider.GetGameplayOptionCategory(isMainMenu, isMultiplayer), isEnabled: true, isResetSupported: true);
		_audioOptionCategory = new GroupedOptionCategoryVM(this, new TextObject("{=xebFLnH2}Audio"), OptionsProvider.GetAudioOptionCategory(isMultiplayer), isEnabled: true);
		_videoOptionCategory = new GroupedOptionCategoryVM(this, new TextObject("{=gamevideo}Video"), OptionsProvider.GetVideoOptionCategory(isMainMenu, OnBrightnessClick, OnExposureClick, ExecuteBenchmark), isEnabled: true);
		bool flag = false;
		flag = true;
		_performanceOptionCategory = new GroupedOptionCategoryVM(this, new TextObject("{=fM9E7frB}Performance"), OptionsProvider.GetPerformanceOptionCategory(isMultiplayer), flag);
		_groupedCategories.Add(_videoOptionCategory);
		_groupedCategories.Add(_audioOptionCategory);
		_groupedCategories.Add(_gameplayOptionCategory);
		_performanceManagedOptions = _performanceOptionCategory.GetManagedOptions();
		_gameKeyCategory = new GameKeyOptionCategoryVM(_onKeybindRequest, OptionsProvider.GetGameKeyCategoriesList(CurrentOptionsMode == OptionsMode.Multiplayer), OptionsProvider.GetHiddenGameKeys(ModuleHelper.IsModuleActive("NavalDLC")));
		TextObject name = new TextObject("{=SQpGQzTI}Controller");
		_gamepadCategory = new GamepadOptionCategoryVM(this, name, OptionsProvider.GetControllerOptionCategory(), isEnabled: true, isResetSupported: true);
		_categories = new List<ViewModel>();
		_categories.Add(_videoOptionCategory);
		_categories.Add(_performanceOptionCategory);
		_categories.Add(_audioOptionCategory);
		_categories.Add(_gameplayOptionCategory);
		_categories.Add(_gameKeyCategory);
		_categories.Add(_gamepadCategory);
		SetSelectedCategory(0);
		if (onBrightnessExecute == null)
		{
			BrightnessPopUp = new BrightnessOptionVM();
		}
		if (onExposureExecute == null)
		{
			ExposurePopUp = new ExposureOptionVM();
		}
		if (Game.Current != null && _autoHandleClose)
		{
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
		}
		_refreshRateOption = VideoOptions.GetOption(NativeOptions.NativeOptionsType.RefreshRate);
		_resolutionOption = VideoOptions.GetOption(NativeOptions.NativeOptionsType.ScreenResolution);
		_monitorOption = VideoOptions.GetOption(NativeOptions.NativeOptionsType.SelectedMonitor);
		_displayModeOption = VideoOptions.GetOption(NativeOptions.NativeOptionsType.DisplayMode);
		_overallOption = PerformanceOptions.GetOption(NativeOptions.NativeOptionsType.OverAll) as StringOptionDataVM;
		_dlssOption = PerformanceOptions.GetOption(NativeOptions.NativeOptionsType.DLSS);
		_dynamicResolutionOptions = new List<GenericOptionDataVM>
		{
			PerformanceOptions.GetOption(NativeOptions.NativeOptionsType.DynamicResolution),
			PerformanceOptions.GetOption(NativeOptions.NativeOptionsType.DynamicResolutionTarget)
		};
		IsConsole = true;
		_performanceOptionCategory?.InitializeDependentConfigs(UpdateDependentConfigs);
		IsConsole = false;
		RefreshValues();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		_isInitialized = true;
	}

	public OptionsVM(OptionsMode optionsMode, Action onClose, Action<KeyOptionVM> onKeybindRequest, Action onBrightnessExecute = null, Action onExposureExecute = null)
		: this(autoHandleClose: false, optionsMode, onKeybindRequest)
	{
		_onClose = onClose;
		_onBrightnessExecute = onBrightnessExecute;
		_onExposureExecute = onExposureExecute;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		OptionsLbl = new TextObject("{=NqarFr4P}Options").ToString();
		CancelLbl = new TextObject("{=3CpNUnVl}Cancel").ToString();
		DoneLbl = new TextObject("{=WiNRdfsm}Done").ToString();
		ResetLbl = new TextObject("{=mAxXKaXp}Reset").ToString();
		VideoMemoryUsageName = Module.CurrentModule.GlobalTextManager.FindText("str_gpu_memory_usage").ToString();
		GameKeyOptionGroups.RefreshValues();
		GamepadOptions.RefreshValues();
		BrightnessPopUp?.RefreshValues();
		ExposurePopUp?.RefreshValues();
		UpdateVideoMemoryUsage();
		_categories.ForEach(delegate(ViewModel g)
		{
			g.RefreshValues();
		});
	}

	public void ExecuteCloseOptions()
	{
		if (_onClose != null)
		{
			_onClose();
		}
	}

	protected void OnBrightnessClick()
	{
		if (_onBrightnessExecute == null)
		{
			BrightnessPopUp.Visible = true;
		}
		else
		{
			_onBrightnessExecute();
		}
	}

	protected void OnExposureClick()
	{
		if (_onExposureExecute == null)
		{
			ExposurePopUp.Visible = true;
		}
		else
		{
			_onExposureExecute();
		}
	}

	public ViewModel GetActiveCategory()
	{
		if (CategoryIndex >= 0 && CategoryIndex < _categories.Count)
		{
			return _categories[CategoryIndex];
		}
		return null;
	}

	public int GetIndexOfCategory(ViewModel categoryVM)
	{
		return _categories.IndexOf(categoryVM);
	}

	public float GetConfig(IOptionData data)
	{
		if (data.IsNative())
		{
			NativeOptions.NativeOptionsType nativeOptionsType = (NativeOptions.NativeOptionsType)data.GetOptionType();
			if (nativeOptionsType == NativeOptions.NativeOptionsType.OverAll)
			{
				return (float)NativeConfig.AutoGFXQuality;
			}
			return NativeOptions.GetConfig(nativeOptionsType);
		}
		return ManagedOptions.GetConfig((ManagedOptions.ManagedOptionsType)data.GetOptionType());
	}

	public void SetConfig(IOptionData data, float val)
	{
		if (!_isInitialized)
		{
			return;
		}
		UpdateDependentConfigs(data, val);
		UpdateEnabledStates();
		NativeOptions.ConfigQuality autoGFXQuality = NativeConfig.AutoGFXQuality;
		NativeOptions.ConfigQuality configQuality = (IsManagedOptionsConflictWithOverallSettings((int)autoGFXQuality) ? NativeOptions.ConfigQuality.GFXCustom : autoGFXQuality);
		if (data.IsNative())
		{
			NativeOptions.NativeOptionsType nativeOptionsType = (NativeOptions.NativeOptionsType)data.GetOptionType();
			if (nativeOptionsType == NativeOptions.NativeOptionsType.OverAll)
			{
				if (TaleWorlds.Library.MathF.Abs(val - (float)_overallConfigCount) > 0.01f)
				{
					Utilities.SetGraphicsPreset((int)val);
					foreach (GenericOptionDataVM allOption in VideoOptions.AllOptions)
					{
						if (!allOption.IsAction)
						{
							float num = (allOption.IsNative ? GetDefaultOptionForOverallNativeSettings((NativeOptions.NativeOptionsType)allOption.GetOptionType(), (int)val) : GetDefaultOptionForOverallManagedSettings((ManagedOptions.ManagedOptionsType)allOption.GetOptionType(), (int)val));
							if (num >= 0f)
							{
								allOption.SetValue(num);
								allOption.UpdateValue();
							}
						}
					}
					foreach (GenericOptionDataVM allOption2 in PerformanceOptions.AllOptions)
					{
						float num2 = (allOption2.IsNative ? GetDefaultOptionForOverallNativeSettings((NativeOptions.NativeOptionsType)allOption2.GetOptionType(), (int)val) : GetDefaultOptionForOverallManagedSettings((ManagedOptions.ManagedOptionsType)allOption2.GetOptionType(), (int)val));
						if (num2 >= 0f)
						{
							allOption2.SetValue(num2);
							allOption2.UpdateValue();
						}
					}
				}
			}
			else if (_overallOption != null && (_overallOption.Selector.SelectedIndex != _overallConfigCount || configQuality != NativeOptions.ConfigQuality.GFXCustom) && OptionsProvider.GetDefaultNativeOptions().ContainsKey(nativeOptionsType))
			{
				_overallOption.Selector.SelectedIndex = (int)configQuality;
			}
			if (!_isCancelling && (nativeOptionsType == NativeOptions.NativeOptionsType.SelectedAdapter || nativeOptionsType == NativeOptions.NativeOptionsType.SoundDevice))
			{
				InformationManager.ShowInquiry(new InquiryData(Module.CurrentModule.GlobalTextManager.FindText("str_option_restart_required").ToString(), Module.CurrentModule.GlobalTextManager.FindText("str_option_restart_required_desc").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Module.CurrentModule.GlobalTextManager.FindText("str_ok").ToString(), string.Empty, null, null));
			}
			UpdateVideoMemoryUsage();
		}
		else
		{
			ManagedOptions.ManagedOptionsType key = (ManagedOptions.ManagedOptionsType)data.GetOptionType();
			if (_overallOption != null && (_overallOption.Selector.SelectedIndex != _overallConfigCount || configQuality != NativeOptions.ConfigQuality.GFXCustom) && OptionsProvider.GetDefaultManagedOptions().ContainsKey(key))
			{
				_overallOption.Selector.SelectedIndex = (int)configQuality;
			}
		}
	}

	private void UpdateEnabledStates()
	{
		foreach (GenericOptionDataVM item in _groupedCategories.SelectMany((GroupedOptionCategoryVM c) => c.AllOptions))
		{
			item.UpdateEnableState();
		}
		foreach (GenericOptionDataVM allOption in _performanceOptionCategory.AllOptions)
		{
			allOption.UpdateEnableState();
		}
	}

	private void UpdateDependentConfigs(IOptionData data, float val)
	{
		if (!data.IsNative())
		{
			return;
		}
		NativeOptions.NativeOptionsType nativeOptionsType = (NativeOptions.NativeOptionsType)data.GetOptionType();
		if (nativeOptionsType == NativeOptions.NativeOptionsType.SelectedMonitor || nativeOptionsType == NativeOptions.NativeOptionsType.DLSS)
		{
			NativeOptions.RefreshOptionsData();
			_resolutionOption.UpdateData(initUpdate: false);
			_refreshRateOption.UpdateData(initUpdate: false);
		}
		if (nativeOptionsType == NativeOptions.NativeOptionsType.ScreenResolution || nativeOptionsType == NativeOptions.NativeOptionsType.SelectedMonitor)
		{
			NativeOptions.RefreshOptionsData();
			_refreshRateOption.UpdateData(initUpdate: false);
			if (NativeOptions.GetIsDLSSAvailable())
			{
				_dlssOption?.UpdateData(initUpdate: false);
			}
		}
	}

	private bool IsManagedOptionsConflictWithOverallSettings(int overallSettingsOption)
	{
		return _performanceManagedOptions.Any((IOptionData o) => (float)(int)o.GetValue(forceRefresh: false) != GetDefaultOptionForOverallManagedSettings((ManagedOptions.ManagedOptionsType)o.GetOptionType(), overallSettingsOption));
	}

	private float GetDefaultOptionForOverallNativeSettings(NativeOptions.NativeOptionsType option, int overallSettingsOption)
	{
		if (overallSettingsOption >= _overallConfigCount || overallSettingsOption < 0)
		{
			return -1f;
		}
		if (OptionsProvider.GetDefaultNativeOptions().TryGetValue(option, out var value))
		{
			return value[overallSettingsOption];
		}
		return -1f;
	}

	private float GetDefaultOptionForOverallManagedSettings(ManagedOptions.ManagedOptionsType option, int overallSettingsOption)
	{
		if (overallSettingsOption >= _overallConfigCount || overallSettingsOption < 0)
		{
			return -1f;
		}
		if (OptionsProvider.GetDefaultManagedOptions().TryGetValue(option, out var value))
		{
			return value[overallSettingsOption];
		}
		return -1f;
	}

	private bool IsCategoryAvailable(ViewModel category)
	{
		if (category is GameKeyOptionCategoryVM gameKeyOptionCategoryVM)
		{
			return gameKeyOptionCategoryVM.IsEnabled;
		}
		if (category is GamepadOptionCategoryVM gamepadOptionCategoryVM)
		{
			return gamepadOptionCategoryVM.IsEnabled;
		}
		if (category is GroupedOptionCategoryVM groupedOptionCategoryVM)
		{
			return groupedOptionCategoryVM.IsEnabled;
		}
		return true;
	}

	private int GetPreviousAvailableCategoryIndex(int currentCategoryIndex)
	{
		if (--currentCategoryIndex < 0)
		{
			currentCategoryIndex = _categories.Count - 1;
		}
		if (!IsCategoryAvailable(_categories[currentCategoryIndex]))
		{
			return GetPreviousAvailableCategoryIndex(currentCategoryIndex);
		}
		return currentCategoryIndex;
	}

	public void SelectPreviousCategory()
	{
		int previousAvailableCategoryIndex = GetPreviousAvailableCategoryIndex(CategoryIndex);
		SetSelectedCategory(previousAvailableCategoryIndex);
	}

	private int GetNextAvailableCategoryIndex(int currentCategoryIndex)
	{
		if (++currentCategoryIndex >= _categories.Count)
		{
			currentCategoryIndex = 0;
		}
		if (!IsCategoryAvailable(_categories[currentCategoryIndex]))
		{
			return GetNextAvailableCategoryIndex(currentCategoryIndex);
		}
		return currentCategoryIndex;
	}

	public void SelectNextCategory()
	{
		int nextAvailableCategoryIndex = GetNextAvailableCategoryIndex(CategoryIndex);
		SetSelectedCategory(nextAvailableCategoryIndex);
	}

	private void SetSelectedCategory(int index)
	{
		CategoryIndex = index;
	}

	private void OnGamepadActiveStateChanged()
	{
		if (!IsCategoryAvailable(_categories[CategoryIndex]))
		{
			if (GetNextAvailableCategoryIndex(CategoryIndex) > CategoryIndex)
			{
				SelectNextCategory();
			}
			else
			{
				SelectPreviousCategory();
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
		PreviousTabInputKey?.OnFinalize();
		NextTabInputKey?.OnFinalize();
		ResetInputKey?.OnFinalize();
		GamepadOptions?.OnFinalize();
		GameKeyOptionGroups?.OnFinalize();
		ExposurePopUp?.OnFinalize();
		BrightnessPopUp?.OnFinalize();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	protected void HandleCancel(bool autoHandleClose)
	{
		_isCancelling = true;
		_groupedCategories.ForEach(delegate(GroupedOptionCategoryVM c)
		{
			c.Cancel();
		});
		_gameKeyCategory.Cancel();
		_performanceOptionCategory.Cancel();
		CloseScreen(autoHandleClose);
	}

	private void CloseScreen(bool autoHandleClose)
	{
		ExecuteCloseOptions();
		if (autoHandleClose)
		{
			if (Game.Current != null)
			{
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
			}
			ScreenManager.PopScreen();
		}
	}

	public void ExecuteCancel()
	{
		if (IsOptionsChanged())
		{
			string text = new TextObject("{=peUP9ZZj}Are you sure? You made some changes and they will be lost.").ToString();
			InformationManager.ShowInquiry(new InquiryData("", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), delegate
			{
				HandleCancel(_autoHandleClose);
			}, null));
		}
		else
		{
			HandleCancel(_autoHandleClose);
		}
	}

	protected void OnDone()
	{
		ApplyChangedOptions();
		GenericOptionDataVM monitorOption = _monitorOption;
		if (monitorOption == null || !monitorOption.IsChanged())
		{
			GenericOptionDataVM resolutionOption = _resolutionOption;
			if (resolutionOption == null || !resolutionOption.IsChanged())
			{
				GenericOptionDataVM refreshRateOption = _refreshRateOption;
				if (refreshRateOption == null || !refreshRateOption.IsChanged())
				{
					GenericOptionDataVM displayModeOption = _displayModeOption;
					if (displayModeOption == null || !displayModeOption.IsChanged())
					{
						CloseScreen(_autoHandleClose);
						return;
					}
				}
			}
		}
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=lCZMJt2k}Video Options Have Been Changed").ToString(), new TextObject("{=pK4EyTZC}Do you want to keep these settings?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Module.CurrentModule.GlobalTextManager.FindText("str_ok").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate
		{
			_monitorOption?.ApplyValue();
			_resolutionOption?.ApplyValue();
			_refreshRateOption?.ApplyValue();
			_displayModeOption?.ApplyValue();
			CloseScreen(_autoHandleClose);
		}, delegate
		{
			_monitorOption?.Cancel();
			_resolutionOption?.Cancel();
			_refreshRateOption?.Cancel();
			_displayModeOption?.Cancel();
			NativeOptions.ApplyConfigChanges(resizeWindow: true);
		}, "", 10f, delegate
		{
			_monitorOption?.Cancel();
			_resolutionOption?.Cancel();
			_refreshRateOption?.Cancel();
			_displayModeOption?.Cancel();
			NativeOptions.ApplyConfigChanges(resizeWindow: true);
		}));
	}

	private void ApplyChangedOptions()
	{
		int trail_amount = 0;
		int texture_budget = 0;
		int hdr = 0;
		int dof_mode = 0;
		int motion_blur = 0;
		int ssr = 0;
		int num = 0;
		int texture_filtering = 0;
		int sharpen_amount = 0;
		int dynamic_resolution_target = 0;
		IEnumerable<GenericOptionDataVM> enumerable = _groupedCategories.SelectMany((GroupedOptionCategoryVM c) => c.AllOptions);
		foreach (GenericOptionDataVM item in enumerable)
		{
			item.UpdateValue();
			if (item.IsNative && !item.GetOptionData().IsAction())
			{
				switch ((NativeOptions.NativeOptionsType)item.GetOptionType())
				{
				case NativeOptions.NativeOptionsType.TextureBudget:
					texture_budget = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.TextureQuality:
					texture_budget = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.TrailAmount:
					trail_amount = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.Bloom:
					hdr = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.DepthOfField:
					dof_mode = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.MotionBlur:
					motion_blur = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.SSR:
					ssr = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.DisplayMode:
				case NativeOptions.NativeOptionsType.SelectedMonitor:
				case NativeOptions.NativeOptionsType.ScreenResolution:
				case NativeOptions.NativeOptionsType.RefreshRate:
				case NativeOptions.NativeOptionsType.DynamicResolution:
					num = ((num == 1 || item.IsChanged()) ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.TextureFiltering:
					texture_filtering = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.SharpenAmount:
					sharpen_amount = (item.IsChanged() ? 1 : 0);
					break;
				case NativeOptions.NativeOptionsType.DynamicResolutionTarget:
					dynamic_resolution_target = (item.IsChanged() ? 1 : 0);
					break;
				}
			}
		}
		NativeOptions.Apply(texture_budget, sharpen_amount, hdr, dof_mode, motion_blur, ssr, num, texture_filtering, trail_amount, dynamic_resolution_target);
		SaveResult saveResult = NativeOptions.SaveConfig();
		SaveResult saveResult2 = ManagedOptions.SaveConfig();
		if (saveResult != SaveResult.Success || saveResult2 != SaveResult.Success)
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=oZrVNUOk}Error").ToString(), Module.CurrentModule.GlobalTextManager.FindText("str_config_save_result", ((saveResult != SaveResult.Success) ? saveResult : saveResult2).ToString()).ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Module.CurrentModule.GlobalTextManager.FindText("str_ok").ToString(), null, null, null));
		}
		GameKeyOptionGroups.ApplyValues();
		HotKeyManager.MarkForSave();
		enumerable = enumerable.Concat(_performanceOptionCategory.AllOptions);
		enumerable = enumerable.Where((GenericOptionDataVM x) => x != _monitorOption && x != _resolutionOption && x != _refreshRateOption && x != _displayModeOption);
		foreach (GenericOptionDataVM item2 in enumerable)
		{
			item2.ApplyValue();
		}
	}

	protected void ExecuteBenchmark()
	{
		GameStateManager.StateActivateCommand = "state_string.benchmark_start";
		CommandLineFunctionality.CallFunction("benchmark.cpu_benchmark", "", out var _);
	}

	public void ExecuteDone()
	{
		int currentEstimatedGPUMemoryCostMB = Utilities.GetCurrentEstimatedGPUMemoryCostMB();
		int gPUMemoryMB = Utilities.GetGPUMemoryMB();
		if (0 == 0 && gPUMemoryMB <= currentEstimatedGPUMemoryCostMB)
		{
			InformationManager.ShowInquiry(new InquiryData(Module.CurrentModule.GlobalTextManager.FindText("str_gpu_memory_caution_title").ToString(), Module.CurrentModule.GlobalTextManager.FindText("str_gpu_memory_caution_text").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Module.CurrentModule.GlobalTextManager.FindText("str_ok").ToString(), null, delegate
			{
				OnDone();
			}, null));
		}
		else
		{
			OnDone();
		}
	}

	protected void ExecuteReset()
	{
		InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=cDzWYQrz}Reset to default settings?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=oHaWR73d}Ok").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), OnResetToDefaults, null));
	}

	public bool IsOptionsChanged()
	{
		return (_groupedCategories.Any((GroupedOptionCategoryVM c) => c.IsChanged()) || GameKeyOptionGroups.IsChanged()) | _performanceOptionCategory.IsChanged();
	}

	private void OnResetToDefaults()
	{
		_groupedCategories.ForEach(delegate(GroupedOptionCategoryVM g)
		{
			g.ResetData();
		});
		_performanceOptionCategory.ResetData();
	}

	private void UpdateVideoMemoryUsage()
	{
		int currentEstimatedGPUMemoryCostMB = Utilities.GetCurrentEstimatedGPUMemoryCostMB();
		int gPUMemoryMB = Utilities.GetGPUMemoryMB();
		VideoMemoryUsageNormalized = (float)currentEstimatedGPUMemoryCostMB / (float)gPUMemoryMB;
		TextObject textObject = Module.CurrentModule.GlobalTextManager.FindText("str_gpu_memory_usage_value_text");
		textObject.SetTextVariable("ESTIMATED", currentEstimatedGPUMemoryCostMB);
		textObject.SetTextVariable("TOTAL", gPUMemoryMB);
		VideoMemoryUsageText = textObject.ToString();
	}

	internal GenericOptionDataVM GetOptionItem(IOptionData option)
	{
		bool flag = TaleWorlds.InputSystem.Input.ControllerType.IsPlaystation();
		MBTextManager.SetTextVariable("IS_PLAYSTATION", flag ? 1 : 0);
		if (!option.IsAction())
		{
			string text = (option.IsNative() ? ((NativeOptions.NativeOptionsType)option.GetOptionType()/*cast due to .constrained prefix*/).ToString() : ((ManagedOptions.ManagedOptionsType)option.GetOptionType()/*cast due to .constrained prefix*/).ToString());
			TextObject name = Module.CurrentModule.GlobalTextManager.FindText("str_options_type", text);
			TextObject textObject = Module.CurrentModule.GlobalTextManager.FindText("str_options_description", text);
			textObject.SetTextVariable("newline", "\n");
			if (option is IBooleanOptionData)
			{
				return new BooleanOptionDataVM(this, option as IBooleanOptionData, name, textObject)
				{
					ImageIDs = new string[2]
					{
						text + "_0",
						text + "_1"
					}
				};
			}
			if (option is INumericOptionData)
			{
				return new NumericOptionDataVM(this, option as INumericOptionData, name, textObject);
			}
			if (option is ISelectionOptionData)
			{
				ISelectionOptionData selectionOptionData = option as ISelectionOptionData;
				StringOptionDataVM stringOptionDataVM = new StringOptionDataVM(this, selectionOptionData, name, textObject);
				string[] array = new string[selectionOptionData.GetSelectableOptionsLimit()];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = text + "_" + i;
				}
				stringOptionDataVM.ImageIDs = array;
				return stringOptionDataVM;
			}
			if (option is ActionOptionData actionOptionData)
			{
				TextObject optionActionName = Module.CurrentModule.GlobalTextManager.FindText("str_options_type_action", text);
				return new ActionOptionDataVM(actionOptionData.OnAction, this, actionOptionData, name, optionActionName, textObject);
			}
			Debug.FailedAssert("Given option data does not match with any option type!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\GameOptions\\OptionsVM.cs", "GetOptionItem", 897);
			return null;
		}
		if (option is ActionOptionData actionOptionData2)
		{
			string variation = option.GetOptionType() as string;
			TextObject optionActionName2 = Module.CurrentModule.GlobalTextManager.FindText("str_options_type_action", variation);
			TextObject name2 = Module.CurrentModule.GlobalTextManager.FindText("str_options_type", variation);
			TextObject textObject2 = Module.CurrentModule.GlobalTextManager.FindText("str_options_description", variation);
			textObject2.SetTextVariable("newline", "\n");
			return new ActionOptionDataVM(actionOptionData2.OnAction, this, actionOptionData2, name2, optionActionName2, textObject2);
		}
		Debug.FailedAssert("Given option data does not match with any option type!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\GameOptions\\OptionsVM.cs", "GetOptionItem", 920);
		return null;
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetPreviousTabInputKey(HotKey hotkey)
	{
		PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetNextTabInputKey(HotKey hotkey)
	{
		NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
