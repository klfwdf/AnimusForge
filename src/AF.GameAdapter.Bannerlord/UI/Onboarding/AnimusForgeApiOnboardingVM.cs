using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class AnimusForgeApiOnboardingVM : ViewModel
{
	public enum OnboardingView
	{
		Main,
		YjMenu,
		MultiApi,
		YjModels,
		Testing,
		Success
	}

	private readonly bool _isApiOnlyFlow;
	private readonly Action _onCompleted;
	private readonly Action _onCancelled;

	private OnboardingView _currentView = OnboardingView.Main;
	private bool _isCustomMode = true;
    private bool _usingExistingConfig;
    private bool _testSucceeded;

	// View Visibilities
	[DataSourceProperty]
	public bool IsMainViewVisible => _currentView == OnboardingView.Main;

	[DataSourceProperty]
	public bool IsYjMenuViewVisible => _currentView == OnboardingView.YjMenu;

	[DataSourceProperty]
	public bool IsMultiApiViewVisible => _currentView == OnboardingView.MultiApi;

	[DataSourceProperty]
	public bool IsYjModelsViewVisible => _currentView == OnboardingView.YjModels;

	[DataSourceProperty]
	public bool IsTestingViewVisible => _currentView == OnboardingView.Testing;

	[DataSourceProperty]
	public bool IsSuccessViewVisible => _currentView == OnboardingView.Success;

	// Headers
	private string _titleText = Localize("{=AF_UI_Onboarding_Title}AnimusForge - API Setup Wizard");
	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set => SetField(ref _titleText, value, nameof(TitleText));
	}

	private string _subtitleText = "";
	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set => SetField(ref _subtitleText, value, nameof(SubtitleText));
	}

	// Multi-API Panel Details
	private string _multiApiModeTag = Localize("{=AF_UI_Onboarding_CustomMode}Fully custom");
	[DataSourceProperty]
	public string MultiApiModeTag
	{
		get => _multiApiModeTag;
		set => SetField(ref _multiApiModeTag, value, nameof(MultiApiModeTag));
	}

	private string _multiApiModeNotice = Localize("{=AF_UI_Onboarding_ApiPanelNotice}Configure the Base URL, key, and bound model for four API pipelines in one panel.");
	[DataSourceProperty]
	public string MultiApiModeNotice
	{
		get => _multiApiModeNotice;
		set => SetField(ref _multiApiModeNotice, value, nameof(MultiApiModeNotice));
	}

	// 1. Primary API (主角色对话)
	private string _primaryUrl = "https://api.deepseek.com";
	[DataSourceProperty]
	public string PrimaryUrl
	{
		get => _primaryUrl;
		set => SetField(ref _primaryUrl, value, nameof(PrimaryUrl));
	}

	private string _primaryKey = "";
	[DataSourceProperty]
	public string PrimaryKey
	{
		get => _primaryKey;
		set => SetField(ref _primaryKey, value, nameof(PrimaryKey));
	}

	private string _primaryModel = "deepseek-chat";
	[DataSourceProperty]
	public string PrimaryModel
	{
		get => _primaryModel;
		set => SetField(ref _primaryModel, value, nameof(PrimaryModel));
	}

	private SelectorVM<SelectorItemVM> _primaryModelSelector;
	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> PrimaryModelSelector
	{
		get => _primaryModelSelector;
		set => SetField(ref _primaryModelSelector, value, nameof(PrimaryModelSelector));
	}

	private string _primaryStatusText = Localize("{=AF_UI_Onboarding_Status_Pending}Not tested");
	[DataSourceProperty]
	public string PrimaryStatusText
	{
		get => _primaryStatusText;
		set => SetField(ref _primaryStatusText, value, nameof(PrimaryStatusText));
	}

	private string _primaryLatencyText = "";
	[DataSourceProperty]
	public string PrimaryLatencyText
	{
		get => _primaryLatencyText;
		set => SetField(ref _primaryLatencyText, value, nameof(PrimaryLatencyText));
	}

	private bool _isPrimarySuccess;
	[DataSourceProperty]
	public bool IsPrimarySuccess
	{
		get => _isPrimarySuccess;
		set => SetField(ref _isPrimarySuccess, value, nameof(IsPrimarySuccess));
	}

	private bool _isPrimaryFailed;
	[DataSourceProperty]
	public bool IsPrimaryFailed
	{
		get => _isPrimaryFailed;
		set => SetField(ref _isPrimaryFailed, value, nameof(IsPrimaryFailed));
	}

	// 2. Auxiliary API (前处理意图分类)
	private string _auxiliaryUrl = "https://api.deepseek.com";
	[DataSourceProperty]
	public string AuxiliaryUrl
	{
		get => _auxiliaryUrl;
		set => SetField(ref _auxiliaryUrl, value, nameof(AuxiliaryUrl));
	}

	private string _auxiliaryKey = "";
	[DataSourceProperty]
	public string AuxiliaryKey
	{
		get => _auxiliaryKey;
		set => SetField(ref _auxiliaryKey, value, nameof(AuxiliaryKey));
	}

	private string _auxiliaryModel = "deepseek-chat";
	[DataSourceProperty]
	public string AuxiliaryModel
	{
		get => _auxiliaryModel;
		set => SetField(ref _auxiliaryModel, value, nameof(AuxiliaryModel));
	}

	private SelectorVM<SelectorItemVM> _auxiliaryModelSelector;
	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> AuxiliaryModelSelector
	{
		get => _auxiliaryModelSelector;
		set => SetField(ref _auxiliaryModelSelector, value, nameof(AuxiliaryModelSelector));
	}

	private string _auxiliaryStatusText = Localize("{=AF_UI_Onboarding_Status_Pending}Not tested");
	[DataSourceProperty]
	public string AuxiliaryStatusText
	{
		get => _auxiliaryStatusText;
		set => SetField(ref _auxiliaryStatusText, value, nameof(AuxiliaryStatusText));
	}

	private string _auxiliaryLatencyText = "";
	[DataSourceProperty]
	public string AuxiliaryLatencyText
	{
		get => _auxiliaryLatencyText;
		set => SetField(ref _auxiliaryLatencyText, value, nameof(AuxiliaryLatencyText));
	}

	private bool _isAuxiliarySuccess;
	[DataSourceProperty]
	public bool IsAuxiliarySuccess
	{
		get => _isAuxiliarySuccess;
		set => SetField(ref _isAuxiliarySuccess, value, nameof(IsAuxiliarySuccess));
	}

	private bool _isAuxiliaryFailed;
	[DataSourceProperty]
	public bool IsAuxiliaryFailed
	{
		get => _isAuxiliaryFailed;
		set => SetField(ref _isAuxiliaryFailed, value, nameof(IsAuxiliaryFailed));
	}

	// 3. ActionPostprocess API (后处理指令标签)
	private string _postprocessUrl = "https://api.deepseek.com";
	[DataSourceProperty]
	public string PostprocessUrl
	{
		get => _postprocessUrl;
		set => SetField(ref _postprocessUrl, value, nameof(PostprocessUrl));
	}

	private string _postprocessKey = "";
	[DataSourceProperty]
	public string PostprocessKey
	{
		get => _postprocessKey;
		set => SetField(ref _postprocessKey, value, nameof(PostprocessKey));
	}

	private string _postprocessModel = "deepseek-chat";
	[DataSourceProperty]
	public string PostprocessModel
	{
		get => _postprocessModel;
		set => SetField(ref _postprocessModel, value, nameof(PostprocessModel));
	}

	private SelectorVM<SelectorItemVM> _postprocessModelSelector;
	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> PostprocessModelSelector
	{
		get => _postprocessModelSelector;
		set => SetField(ref _postprocessModelSelector, value, nameof(PostprocessModelSelector));
	}

	private string _postprocessStatusText = Localize("{=AF_UI_Onboarding_Status_Pending}Not tested");
	[DataSourceProperty]
	public string PostprocessStatusText
	{
		get => _postprocessStatusText;
		set => SetField(ref _postprocessStatusText, value, nameof(PostprocessStatusText));
	}

	private string _postprocessLatencyText = "";
	[DataSourceProperty]
	public string PostprocessLatencyText
	{
		get => _postprocessLatencyText;
		set => SetField(ref _postprocessLatencyText, value, nameof(PostprocessLatencyText));
	}

	private bool _isPostprocessSuccess;
	[DataSourceProperty]
	public bool IsPostprocessSuccess
	{
		get => _isPostprocessSuccess;
		set => SetField(ref _isPostprocessSuccess, value, nameof(IsPostprocessSuccess));
	}

	private bool _isPostprocessFailed;
	[DataSourceProperty]
	public bool IsPostprocessFailed
	{
		get => _isPostprocessFailed;
		set => SetField(ref _isPostprocessFailed, value, nameof(IsPostprocessFailed));
	}

	// 4. EventAndRebellion API (每日与周报写作)
	private string _eventUrl = "https://api.deepseek.com";
	[DataSourceProperty]
	public string EventUrl
	{
		get => _eventUrl;
		set => SetField(ref _eventUrl, value, nameof(EventUrl));
	}

	private string _eventKey = "";
	[DataSourceProperty]
	public string EventKey
	{
		get => _eventKey;
		set => SetField(ref _eventKey, value, nameof(EventKey));
	}

	private string _eventModel = "deepseek-chat";
	[DataSourceProperty]
	public string EventModel
	{
		get => _eventModel;
		set => SetField(ref _eventModel, value, nameof(EventModel));
	}

	private SelectorVM<SelectorItemVM> _eventModelSelector;
	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> EventModelSelector
	{
		get => _eventModelSelector;
		set => SetField(ref _eventModelSelector, value, nameof(EventModelSelector));
	}

	private string _eventStatusText = Localize("{=AF_UI_Onboarding_Status_Pending}Not tested");
	[DataSourceProperty]
	public string EventStatusText
	{
		get => _eventStatusText;
		set => SetField(ref _eventStatusText, value, nameof(EventStatusText));
	}

	private string _eventLatencyText = "";
	[DataSourceProperty]
	public string EventLatencyText
	{
		get => _eventLatencyText;
		set => SetField(ref _eventLatencyText, value, nameof(EventLatencyText));
	}

	private bool _isEventSuccess;
	[DataSourceProperty]
	public bool IsEventSuccess
	{
		get => _isEventSuccess;
		set => SetField(ref _isEventSuccess, value, nameof(IsEventSuccess));
	}

	private bool _isEventFailed;
	[DataSourceProperty]
	public bool IsEventFailed
	{
		get => _isEventFailed;
		set => SetField(ref _isEventFailed, value, nameof(IsEventFailed));
	}

	// YJ Single Group Info
	private string _yjKeyMasked = Localize("{=AF_UI_Onboarding_Yj_Unbound}Not bound");
	[DataSourceProperty]
	public string YjKeyMasked
	{
		get => _yjKeyMasked;
		set => SetField(ref _yjKeyMasked, value, nameof(YjKeyMasked));
	}

	// Testing Overall State
	private string _testOverallNotice = Localize("{=AF_UI_Onboarding_Testing}Sending handshake test requests to four API pipelines...");
	[DataSourceProperty]
	public string TestOverallNotice
	{
		get => _testOverallNotice;
		set => SetField(ref _testOverallNotice, value, nameof(TestOverallNotice));
	}

	private bool _canCancelTest = true;
	[DataSourceProperty]
	public bool CanCancelTest
	{
		get => _canCancelTest;
		set => SetField(ref _canCancelTest, value, nameof(CanCancelTest));
	}

	// Key Prompt Overlay
	private bool _isKeyPromptVisible;
	[DataSourceProperty]
	public bool IsKeyPromptVisible
	{
		get => _isKeyPromptVisible;
		set => SetField(ref _isKeyPromptVisible, value, nameof(IsKeyPromptVisible));
	}

	private string _keyPromptTitle = Localize("{=AF_UI_Onboarding_KeyPrompt_Title}Enter API key");
	[DataSourceProperty]
	public string KeyPromptTitle
	{
		get => _keyPromptTitle;
		set => SetField(ref _keyPromptTitle, value, nameof(KeyPromptTitle));
	}

	private string _keyPromptHint = Localize("{=AF_UI_Onboarding_KeyPrompt_Hint}Enter the API key:");
	[DataSourceProperty]
	public string KeyPromptHint
	{
		get => _keyPromptHint;
		set => SetField(ref _keyPromptHint, value, nameof(KeyPromptHint));
	}

	private string _promptKeyInput = "";
	[DataSourceProperty]
	public string PromptKeyInput
	{
		get => _promptKeyInput;
		set => SetField(ref _promptKeyInput, value, nameof(PromptKeyInput));
	}

	private Action<string> _keyPromptConfirmAction;

	// Support Modal Overlay
	private bool _isSupportModalVisible;
	[DataSourceProperty]
	public bool IsSupportModalVisible
	{
		get => _isSupportModalVisible;
		set => SetField(ref _isSupportModalVisible, value, nameof(IsSupportModalVisible));
	}

	// Toast Notification
	private bool _isToastVisible;
	[DataSourceProperty]
	public bool IsToastVisible
	{
		get => _isToastVisible;
		set => SetField(ref _isToastVisible, value, nameof(IsToastVisible));
	}

	private string _toastMessage = "";
	[DataSourceProperty]
	public string ToastMessage
	{
		get => _toastMessage;
		set => SetField(ref _toastMessage, value, nameof(ToastMessage));
	}

	private float _toastTimer;
	private CancellationTokenSource _testCts;
    private CancellationTokenSource _yjProbeCts;
    private string _selectedYjUrl = YjEndpointProbe.DefaultUrl;
    internal Func<CancellationToken, Task<YjEndpointProbe.Result>> ProbeYjEndpoints = YjEndpointProbe.SelectAsync;
	private readonly OnboardingUiDispatchOwner _uiDispatch = new OnboardingUiDispatchOwner();

	// Static labels from the onboarding prefab are resolved through TextObject so the XML remains language-neutral.
	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_001Text => Localize("{=AF_UI_Onboarding_Prefab_001}YJ API relay");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_002Text => Localize("{=AF_UI_Onboarding_Prefab_002}Custom API setup");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_003Text => Localize("{=AF_UI_Onboarding_Prefab_003}Use existing API setup");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_004Text => Localize("{=AF_UI_Onboarding_Prefab_004}Direct access through major domestic carriers without a proxy. Enter one key for single-group mode to choose four models, or use the multi-group panel with preset YJ URLs.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_005Text => Localize("{=AF_UI_Onboarding_Prefab_005}1. Single-group mode: enter a key ➔ choose models (recommended)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_006Text => Localize("{=AF_UI_Onboarding_Prefab_006}Enter one key, then choose models for dialogue, preprocessing, postprocessing, and reports in one panel.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_007Text => Localize("{=AF_UI_Onboarding_Prefab_007}Enter key ➔");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_008Text => Localize("{=AF_UI_Onboarding_Prefab_008}2. Multi-group mode: open the multi-API panel (YJ URLs preset)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_009Text => Localize("{=AF_UI_Onboarding_Prefab_009}Open the multi-API panel with the YJ Base URL filled in; enter a different key for each of the four routes.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_010Text => Localize("{=AF_UI_Onboarding_Prefab_010}Open multi-API panel ➔");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_011Text => Localize("{=AF_UI_Onboarding_Prefab_011}3. Get an API key and credits (open website)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_012Text => Localize("{=AF_UI_Onboarding_Prefab_012}Open https://www.shenlanqaq.com/ to get an API key and credits, then return and enter the key.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_013Text => Localize("{=AF_UI_Onboarding_Prefab_013}Open website ↗");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_017Text => Localize("{=AF_UI_Onboarding_Prefab_017}1. Main API (character dialogue)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_018Text => Localize("{=AF_UI_Onboarding_Prefab_018}Model:");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_019Text => Localize("{=AF_UI_Onboarding_Prefab_019}Fetch models");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_020Text => Localize("{=AF_UI_Onboarding_Prefab_020}2. Preprocessing API (intent routing)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_021Text => Localize("{=AF_UI_Onboarding_Prefab_021}3. Postprocessing API (action tag extraction)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_022Text => Localize("{=AF_UI_Onboarding_Prefab_022}4. Daily and weekly report API (world overview)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_023Text => Localize("{=AF_UI_Onboarding_Prefab_023}🔑 Bound key:");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_024Text => Localize("{=AF_UI_Onboarding_Prefab_024}Refresh all models 🔄");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_025Text => Localize("{=AF_UI_Onboarding_Prefab_025}1. Main API dialogue model (immersive roleplay)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_026Text => Localize("{=AF_UI_Onboarding_Prefab_026}Recommended: deepseek-chat or gpt-4o-mini for dialogue with NPCs.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_027Text => Localize("{=AF_UI_Onboarding_Prefab_027}2. Preprocessing intent model (fast routing)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_028Text => Localize("{=AF_UI_Onboarding_Prefab_028}Recommended fast route: deepseek-chat, for player intent and scene mechanisms.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_029Text => Localize("{=AF_UI_Onboarding_Prefab_029}3. Postprocessing action-tag model (action execution)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_030Text => Localize("{=AF_UI_Onboarding_Prefab_030}Extracts duel, trade, recruitment, and other gameplay tags from replies.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_031Text => Localize("{=AF_UI_Onboarding_Prefab_031}4. Daily and weekly writing model (world events)");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_032Text => Localize("{=AF_UI_Onboarding_Prefab_032}Summarizes Calradia wars, settlement development, and rebellion reports.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_033Text => Localize("{=AF_UI_Onboarding_Prefab_033}1. Main API handshake status:");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_034Text => Localize("{=AF_UI_Onboarding_Prefab_034}2. Preprocessing intent handshake status:");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_035Text => Localize("{=AF_UI_Onboarding_Prefab_035}3. Postprocessing action-tag handshake status:");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_036Text => Localize("{=AF_UI_Onboarding_Prefab_036}4. Daily and weekly API handshake status:");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_037Text => Localize("{=AF_UI_Onboarding_Prefab_037}All four API tests passed; configuration is ready!");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_038Text => FormatLocalized("{=AF_UI_Onboarding_Prefab_038}✔ Main API: {MODEL}", "MODEL", PrimaryModel);

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_039Text => FormatLocalized("{=AF_UI_Onboarding_Prefab_039}✔ Preprocessing API: {MODEL}", "MODEL", AuxiliaryModel);

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_040Text => FormatLocalized("{=AF_UI_Onboarding_Prefab_040}✔ Postprocessing API: {MODEL}", "MODEL", PostprocessModel);

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_041Text => FormatLocalized("{=AF_UI_Onboarding_Prefab_041}✔ Daily and weekly API: {MODEL}", "MODEL", EventModel);

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_042Text => Localize("{=AF_UI_Onboarding_Prefab_042}Click below to save. A new save continues with world-book and character setup; completed saves return to the game.");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_043Text => Localize("{=AF_UI_Onboarding_Prefab_043}Support the team");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_044Text => Localize("{=AF_UI_Onboarding_Prefab_044}Close and exit");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_045Text => Localize("{=AF_UI_Onboarding_Prefab_045}⇦ Back");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_046Text => Localize("{=AF_UI_Onboarding_Prefab_046}Run connectivity test now ➔");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_047Text => Localize("{=AF_UI_Onboarding_Prefab_047}Finish selection and test connectivity ➔");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_048Text => Localize("{=AF_UI_Onboarding_Prefab_048}Cancel test");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_049Text => Localize("{=AF_UI_Onboarding_Prefab_049}Save and enter Calradia ➔");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_050Text => Localize("{=AF_UI_Onboarding_Prefab_050}Paste");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_051Text => Localize("{=AF_UI_Onboarding_Prefab_051}Cancel");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_052Text => Localize("{=AF_UI_Onboarding_Prefab_052}Confirm ➔");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_053Text => Localize("{=AF_UI_Onboarding_Prefab_053}Support the AnimusForge team");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_054Text => Localize("{=AF_UI_Onboarding_Prefab_054}AnimusForge is free and open source. Your support helps us continue immersive AI gameplay, world-map evolution, and new features. Visit Afdian to support us!");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_055Text => Localize("{=AF_UI_Onboarding_Prefab_055}Close");

	[DataSourceProperty]
	public string AF_UI_Onboarding_Prefab_056Text => Localize("{=AF_UI_Onboarding_Prefab_056}Visit Afdian ↗");

	[DataSourceProperty]
	public string AF_UI_Onboarding_DeepSeekFlashButtonText => Localize("{=AF_UI_Onboarding_DeepSeekFlashButton}DeepSeek Flash");

	[DataSourceProperty]
	public string AF_UI_Onboarding_DeepSeekProButtonText => Localize("{=AF_UI_Onboarding_DeepSeekProButton}DeepSeek Pro");

	[DataSourceProperty]
	public string AF_UI_Onboarding_BaseUrlLabelText => Localize("{=AF_UI_Onboarding_BaseUrlLabel}Base URL:");

	[DataSourceProperty]
	public string AF_UI_Onboarding_ApiKeyLabelText => Localize("{=AF_UI_Onboarding_ApiKeyLabel}API Key:");


	public AnimusForgeApiOnboardingVM(bool isApiOnlyFlow, Action onCompleted, Action onCancelled)
	{
		_isApiOnlyFlow = isApiOnlyFlow;
		_onCompleted = onCompleted;
		_onCancelled = onCancelled;

		LoadSettingsFromMcm();
		InitSelectors();
	}

	private void LoadSettingsFromMcm()
	{
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;

		_primaryUrl = settings.ApiUrl ?? "";
		_primaryKey = settings.ApiKey ?? "";
		_primaryModel = settings.ModelName ?? "";

		_auxiliaryUrl = settings.AuxiliaryApiUrl ?? "";
		_auxiliaryKey = settings.AuxiliaryApiKey ?? "";
		_auxiliaryModel = settings.AuxiliaryModelName ?? "";

		_postprocessUrl = settings.ActionPostprocessApiUrl ?? "";
		_postprocessKey = settings.ActionPostprocessApiKey ?? "";
		_postprocessModel = settings.ActionPostprocessModelName ?? "";

		_eventUrl = settings.EventAndRebellionApiUrl ?? "";
		_eventKey = settings.EventAndRebellionApiKey ?? "";
		_eventModel = settings.EventAndRebellionModelName ?? "";
	}

	private void InitSelectors()
	{
		var defaultOptions = new List<string>
		{
			"deepseek-v4-flash",
			"deepseek-v4-pro",
			"deepseek-chat",
			"deepseek-reasoner",
			"gpt-4o-mini",
			"gpt-4o",
			"claude-3-5-sonnet",
			"gemini-2.5-flash"
		};

        foreach (string model in new[] { _primaryModel, _auxiliaryModel, _postprocessModel, _eventModel })
            if (!string.IsNullOrWhiteSpace(model) && !defaultOptions.Contains(model)) defaultOptions.Add(model);

		PrimaryModelSelector = CreateSelector(defaultOptions, _primaryModel, i =>
		{
			if (i >= 0 && i < defaultOptions.Count) _primaryModel = defaultOptions[i];
		});

		AuxiliaryModelSelector = CreateSelector(defaultOptions, _auxiliaryModel, i =>
		{
			if (i >= 0 && i < defaultOptions.Count) _auxiliaryModel = defaultOptions[i];
		});

		PostprocessModelSelector = CreateSelector(defaultOptions, _postprocessModel, i =>
		{
			if (i >= 0 && i < defaultOptions.Count) _postprocessModel = defaultOptions[i];
		});

		EventModelSelector = CreateSelector(defaultOptions, _eventModel, i =>
		{
			if (i >= 0 && i < defaultOptions.Count) _eventModel = defaultOptions[i];
		});
	}

	private SelectorVM<SelectorItemVM> CreateSelector(List<string> options, string selectedValue, Action<int> onSelectionChanged)
	{
		if (options == null || options.Count == 0)
		{
			options = new List<string> { "deepseek-chat", "deepseek-reasoner" };
		}
        // Never silently replace custom/persisted models with the first built-in preset.
        if (!string.IsNullOrWhiteSpace(selectedValue) && !options.Contains(selectedValue))
            options.Add(selectedValue);
        int selectedIndex = options.IndexOf(selectedValue);
        if (selectedIndex < 0) selectedIndex = 0;

		return new SelectorVM<SelectorItemVM>(options, selectedIndex, s =>
		{
			if (s != null && s.SelectedIndex >= 0 && s.SelectedIndex < options.Count)
			{
				onSelectionChanged?.Invoke(s.SelectedIndex);
			}
		});
	}

	private void SwitchView(OnboardingView view)
	{
        CancelYjProbe();
		_currentView = view;
		OnPropertyChanged(nameof(IsMainViewVisible));
		OnPropertyChanged(nameof(IsYjMenuViewVisible));
		OnPropertyChanged(nameof(IsMultiApiViewVisible));
		OnPropertyChanged(nameof(IsYjModelsViewVisible));
		OnPropertyChanged(nameof(IsTestingViewVisible));
		OnPropertyChanged(nameof(IsSuccessViewVisible));

		switch (view)
		{
			case OnboardingView.Main:
				TitleText = Localize("{=AF_UI_Onboarding_Title}AnimusForge - API Setup Wizard");
				SubtitleText = "";
				break;
			case OnboardingView.YjMenu:
				TitleText = Localize("{=AF_UI_Onboarding_YjMenuTitle}YJ API relay - Setup wizard");
				SubtitleText = Localize("{=AF_UI_Onboarding_YjMenuSubtitle}Direct domestic access without a proxy. Choose single-group or multi-group access.");
				break;
			case OnboardingView.MultiApi:
				TitleText = _isCustomMode
					? Localize("{=AF_UI_Onboarding_CustomTitle}Fully custom API - Multi-pipeline setup")
					: Localize("{=AF_UI_Onboarding_YjTitle}YJ API multi-group mode - Dedicated setup");
				SubtitleText = _isCustomMode
					? Localize("{=AF_UI_Onboarding_ApiPanelNotice}Configure the Base URL, key, and bound model for four API pipelines in one panel.")
					: Localize("{=AF_UI_Onboarding_YjNotice}The Base URL is preset to the YJ endpoint. Configure keys and models for all four pipelines.");
				break;
			case OnboardingView.YjModels:
				TitleText = Localize("{=AF_UI_Onboarding_YjModelsTitle}YJ API dedicated route - Model selection");
				SubtitleText = Localize("{=AF_UI_Onboarding_YjModelsSubtitle}A single-group key is bound. Choose models for the four pipelines below.");
				break;
			case OnboardingView.Testing:
				TitleText = Localize("{=AF_UI_Onboarding_TestTitle}API connectivity handshake test");
				SubtitleText = Localize("{=AF_UI_Onboarding_TestSubtitle}Testing response speed and connectivity for four API routes in parallel...");
				break;
			case OnboardingView.Success:
				TitleText = Localize("{=AF_UI_Onboarding_SuccessTitle}🎉 All API connectivity tests passed!");
				SubtitleText = Localize("{=AF_UI_Onboarding_SuccessSubtitle}The configuration is ready. Click the button below to save it and enter Calradia.");
				break;
		}
	}

	// ================= Actions: Main Cards =================
	public void ExecuteSelectYj()
	{
        if (_yjProbeCts != null) return;
        _usingExistingConfig = false;
        var cts = _yjProbeCts = new CancellationTokenSource();
        ShowToast(new TextObject("{=AF_UI_Onboarding_YjTesting}Testing the three YJ routes; the access-mode selection will open when it finishes...").ToString());
        _toastTimer = 9f;
        _ = ProbeYjAndOpenAsync(cts);
	}

    private async Task ProbeYjAndOpenAsync(CancellationTokenSource cts)
    {
        try
        {
            var result = await ProbeYjEndpoints(cts.Token).ConfigureAwait(false);
            _uiDispatch.Post(() => {
                if (!ReferenceEquals(_yjProbeCts, cts) || cts.IsCancellationRequested) return;
                _yjProbeCts = null;
                if (result == null) { ShowToast(new TextObject("{=AF_UI_Onboarding_YjAllFailed}All three YJ routes failed the speed test. Check the network and retry, or use custom configuration.").ToString()); return; }
                _selectedYjUrl = result.Url;
                SwitchView(OnboardingView.YjMenu);
                var selectedRoute = new TextObject("{=AF_UI_Onboarding_YjSelectedRoute}Selected route: {HOST} · response about {MS} ms; choose an access mode");
                selectedRoute.SetTextVariable("HOST", new Uri(result.Url).Host);
				selectedRoute.SetTextVariable("MS", Math.Round(result.Milliseconds).ToString(CultureInfo.InvariantCulture));
                SubtitleText = selectedRoute.ToString();
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception) { _uiDispatch.Post(() => { if (ReferenceEquals(_yjProbeCts, cts)) { _yjProbeCts = null; ShowToast(new TextObject("{=AF_UI_Onboarding_YjProbeFailed}YJ route speed test failed. Please retry.").ToString()); } }); }
        finally { cts.Dispose(); }
    }

    private void CancelYjProbe()
    {
        var cts = _yjProbeCts; _yjProbeCts = null;
        try { cts?.Cancel(); } catch (ObjectDisposedException) { }
    }

	public void ExecuteSelectDeepSeekFlash()
	{
        _usingExistingConfig = false;
		OpenKeyPrompt(new TextObject("{=AF_UI_Onboarding_FlashTitle}DeepSeek Flash - Fast recommendation").ToString(), new TextObject("{=AF_UI_Onboarding_FlashHint}Enter your official DeepSeek API key (sk-...):").ToString(), key =>
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				ShowToast(new TextObject("{=AF_UI_Onboarding_KeyEmpty}API key cannot be empty.").ToString());
				return;
			}
			string baseUrl = "https://api.deepseek.com";
			PrimaryUrl = baseUrl;
			AuxiliaryUrl = baseUrl;
			PostprocessUrl = baseUrl;
			EventUrl = baseUrl;

			PrimaryKey = key;
			AuxiliaryKey = key;
			PostprocessKey = key;
			EventKey = key;

			// Flash 预设：Primary/Postprocess/Event 用极速版，Auxiliary 用 Pro（需要深度规则能力）
			PrimaryModel = "deepseek-v4-flash";
			AuxiliaryModel = "deepseek-v4-pro";
			PostprocessModel = "deepseek-v4-flash";
			EventModel = "deepseek-v4-flash";

			// 写入 thinking / effort / temperature（与 ApplyDeepSeekPresetToMcm 保持一致）
			DuelSettings s = DuelSettings.GetSettings();
			if (s != null)
			{
				s.MainApiThinkingEnabled = true;
				s.SetMainApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
				s.MainApiTemperature = 1f;

				s.AuxiliaryApiThinkingEnabled = false;
				s.SetAuxiliaryApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
				s.AuxiliaryApiTemperature = 0f;

				s.ActionPostprocessApiThinkingEnabled = true;
				s.SetActionPostprocessApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
				s.ActionPostprocessApiTemperature = 0f;

				s.EventAndRebellionApiThinkingEnabled = false;
				s.SetEventAndRebellionApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
				s.EventAndRebellionApiTemperature = 0.8f;
			}

			InitSelectors();
			ExecuteStartCombinedTest();
		});
	}

	public void ExecuteSelectDeepSeekPro()
	{
        _usingExistingConfig = false;
		OpenKeyPrompt(new TextObject("{=AF_UI_Onboarding_ProTitle}DeepSeek Pro - Deep reasoning").ToString(), new TextObject("{=AF_UI_Onboarding_FlashHint}Enter your official DeepSeek API key (sk-...):").ToString(), key =>
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				ShowToast(new TextObject("{=AF_UI_Onboarding_KeyEmpty}API key cannot be empty.").ToString());
				return;
			}
			string baseUrl = "https://api.deepseek.com";
			PrimaryUrl = baseUrl;
			AuxiliaryUrl = baseUrl;
			PostprocessUrl = baseUrl;
			EventUrl = baseUrl;

			PrimaryKey = key;
			AuxiliaryKey = key;
			PostprocessKey = key;
			EventKey = key;

			// Pro 预设：4条管线全用 v4-pro
			PrimaryModel = "deepseek-v4-pro";
			AuxiliaryModel = "deepseek-v4-pro";
			PostprocessModel = "deepseek-v4-pro";
			EventModel = "deepseek-v4-pro";

			// 写入 thinking / effort / temperature（与 ApplyDeepSeekPresetToMcm 保持一致）
			DuelSettings s = DuelSettings.GetSettings();
			if (s != null)
			{
				s.MainApiThinkingEnabled = true;
				s.SetMainApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
				s.MainApiTemperature = 1f;

				s.AuxiliaryApiThinkingEnabled = false;
				s.SetAuxiliaryApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
				s.AuxiliaryApiTemperature = 0f;

				s.ActionPostprocessApiThinkingEnabled = true;
				s.SetActionPostprocessApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
				s.ActionPostprocessApiTemperature = 0f;

				s.EventAndRebellionApiThinkingEnabled = false;
				s.SetEventAndRebellionApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
				s.EventAndRebellionApiTemperature = 0.8f;
			}

			InitSelectors();
			ExecuteStartCombinedTest();
		});
	}

	public void ExecuteSelectCustom()
	{
        _usingExistingConfig = false;
		_isCustomMode = true;
		MultiApiModeTag = Localize("{=AF_UI_Onboarding_CustomMode}Fully custom");
		MultiApiModeNotice = Localize("{=AF_UI_Onboarding_ApiPanelNotice}Configure the Base URL, key, and bound model for four API pipelines in one panel.");
		SwitchView(OnboardingView.MultiApi);
	}

	// ================= Actions: YJ Submenu =================
	public void ExecuteYjSingleGroup()
	{
		OpenKeyPrompt(new TextObject("{=AF_UI_Onboarding_YjSingleTitle}YJ single-group key setup").ToString(), new TextObject("{=AF_UI_Onboarding_YjSingleHint}Enter one YJ API key; it will be applied to all four pipelines:").ToString(), key =>
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				ShowToast(new TextObject("{=AF_UI_Onboarding_KeyEmpty}API key cannot be empty.").ToString());
				return;
			}
			string yjUrl = _selectedYjUrl;
			PrimaryUrl = yjUrl;
			AuxiliaryUrl = yjUrl;
			PostprocessUrl = yjUrl;
			EventUrl = yjUrl;

			PrimaryKey = key;
			AuxiliaryKey = key;
			PostprocessKey = key;
			EventKey = key;

			YjKeyMasked = key.Length > 8 ? (key.Substring(0, 4) + "..." + key.Substring(key.Length - 4)) : "****";
			SwitchView(OnboardingView.YjModels);

			// 自动拉取一次模型
			ExecuteFetchAllYjModels();
		});
	}

	public void ExecuteYjMultiGroup()
	{
		_isCustomMode = false;
		string yjUrl = _selectedYjUrl;
		PrimaryUrl = yjUrl;
		AuxiliaryUrl = yjUrl;
		PostprocessUrl = yjUrl;
		EventUrl = yjUrl;

		MultiApiModeTag = Localize("{=AF_UI_Onboarding_YjPreset}YJ official route preset");
		MultiApiModeNotice = Localize("{=AF_UI_Onboarding_YjPresetNotice}YJ Base URLs are filled automatically. Enter a different group key for each of the four pipelines.");
		SwitchView(OnboardingView.MultiApi);
	}

	public void ExecuteOpenYjWeb()
	{
		ExternalBrowserLauncher.TryOpen(YjEndpointProbe.WebsiteUrl, out _, out _);
		ShowToast(new TextObject("{=AF_UI_Onboarding_YjOpenManagement}Opening the YJ website...").ToString());
	}

	// ================= Actions: Model Fetching =================
	public void ExecuteFetchPrimaryModels()
	{
		FetchModelsForTarget(PrimaryUrl, PrimaryKey, list =>
		{
			PrimaryModelSelector = CreateSelector(list, PrimaryModel, i =>
			{
				if (i >= 0 && i < list.Count) PrimaryModel = list[i];
			});
		});
	}

	public void ExecuteFetchAuxiliaryModels()
	{
		FetchModelsForTarget(AuxiliaryUrl, AuxiliaryKey, list =>
		{
			AuxiliaryModelSelector = CreateSelector(list, AuxiliaryModel, i =>
			{
				if (i >= 0 && i < list.Count) AuxiliaryModel = list[i];
			});
		});
	}

	public void ExecuteFetchPostprocessModels()
	{
		FetchModelsForTarget(PostprocessUrl, PostprocessKey, list =>
		{
			PostprocessModelSelector = CreateSelector(list, PostprocessModel, i =>
			{
				if (i >= 0 && i < list.Count) PostprocessModel = list[i];
			});
		});
	}

	public void ExecuteFetchEventModels()
	{
		FetchModelsForTarget(EventUrl, EventKey, list =>
		{
			EventModelSelector = CreateSelector(list, EventModel, i =>
			{
				if (i >= 0 && i < list.Count) EventModel = list[i];
			});
		});
	}

	public void ExecuteFetchAllYjModels()
	{
		ShowToast(new TextObject("{=AF_UI_Onboarding_YjFetchingModels}Fetching available YJ models...").ToString());
		FetchModelsForTarget(PrimaryUrl, PrimaryKey, list =>
		{
			PrimaryModelSelector = CreateSelector(list, PrimaryModel, i =>
			{
				if (i >= 0 && i < list.Count) PrimaryModel = list[i];
			});
			AuxiliaryModelSelector = CreateSelector(list, AuxiliaryModel, i =>
			{
				if (i >= 0 && i < list.Count) AuxiliaryModel = list[i];
			});
			PostprocessModelSelector = CreateSelector(list, PostprocessModel, i =>
			{
				if (i >= 0 && i < list.Count) PostprocessModel = list[i];
			});
			EventModelSelector = CreateSelector(list, EventModel, i =>
			{
				if (i >= 0 && i < list.Count) EventModel = list[i];
			});
		});
	}

	private void FetchModelsForTarget(string url, string key, Action<List<string>> onListFetched)
	{
		if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
		{
			ShowToast(new TextObject("{=AF_UI_Onboarding_MissingEndpoint}Enter the Base URL and API key first.").ToString());
			return;
		}

		string endpoint = url.Trim();
		string apiKey = key.Trim();
		Task.Run(async () =>
		{
			try
			{
				ModelCatalogExchange exchange = await new LegacyModelCatalogGateway().FetchModelsAsync(endpoint, apiKey, CancellationToken.None);
				if (exchange.IsSuccessStatusCode)
				{
					List<string> models = ModOnboardingBehavior.ExtractModelNamesFromResponse(exchange.ResponseBody);
					if (models != null && models.Count > 0)
					{
						_uiDispatch.Post(() =>
						{
							var fetched = new TextObject("{=AF_UI_Onboarding_ModelsFetched}Fetched {COUNT} models successfully");
							fetched.SetTextVariable("COUNT", models.Count);
							ShowToast(fetched.ToString());
							onListFetched?.Invoke(models);
						});
						return;
					}
				}
				_uiDispatch.Post(() => ShowToast(new TextObject("{=AF_UI_Onboarding_ModelParseFailed}The endpoint returned no parseable model list.").ToString()));
			}
			catch (Exception)
			{
				_uiDispatch.Post(() => ShowToast(new TextObject("{=AF_UI_Onboarding_ModelFetchFailed}Model fetch failed. Check the network and API configuration.").ToString()));
			}
		});
	}

	// ================= Actions: Testing Sequence =================
    public void ExecuteUseExistingConfig()
    {
        LoadSettingsFromMcm();
        _usingExistingConfig = true;
        _isCustomMode = true;
        ExecuteStartCombinedTest();
    }

	public void ExecuteStartCombinedTest()
	{
		if (string.IsNullOrWhiteSpace(PrimaryKey) || string.IsNullOrWhiteSpace(PrimaryUrl))
		{
			ShowToast(new TextObject("{=AF_UI_Onboarding_PrimaryIncomplete}The main API configuration is incomplete. Check the URL and key.").ToString());
			return;
		}

		_testSucceeded = false;
		SwitchView(OnboardingView.Testing);
		TestOverallNotice = Localize("{=AF_UI_Onboarding_TestPreparing}Preparing the API connectivity test...");
		CanCancelTest = true;

		PrimaryStatusText = new TextObject("{=AF_UI_Onboarding_Handshaking}Handshaking...").ToString();
		PrimaryLatencyText = "";
		IsPrimarySuccess = false;
		IsPrimaryFailed = false;

		AuxiliaryStatusText = new TextObject("{=AF_UI_Onboarding_Handshaking}Handshaking...").ToString();
		AuxiliaryLatencyText = "";
		IsAuxiliarySuccess = false;
		IsAuxiliaryFailed = false;

		PostprocessStatusText = new TextObject("{=AF_UI_Onboarding_Handshaking}Handshaking...").ToString();
		PostprocessLatencyText = "";
		IsPostprocessSuccess = false;
		IsPostprocessFailed = false;

		EventStatusText = new TextObject("{=AF_UI_Onboarding_Handshaking}Handshaking...").ToString();
		EventLatencyText = "";
		IsEventSuccess = false;
		IsEventFailed = false;

		_testCts?.Cancel();
		_testCts?.Dispose();
		_testCts = new CancellationTokenSource();
		CancellationToken token = _testCts.Token;
		int generation = _uiDispatch.BeginTest();
		var targets = new List<ModOnboardingBehavior.ApiValidationTargetInfo>
		{
			new ModOnboardingBehavior.ApiValidationTargetInfo { Target = ModOnboardingBehavior.ApiSetupTarget.Primary, DisplayName = Localize("{=AF_UI_Onboarding_Target_Primary}Main API (response generation)"), ApiUrl = PrimaryUrl.Trim(), ApiKey = PrimaryKey.Trim(), ModelName = PrimaryModel.Trim() },
			new ModOnboardingBehavior.ApiValidationTargetInfo { Target = ModOnboardingBehavior.ApiSetupTarget.Auxiliary, DisplayName = Localize("{=AF_UI_Onboarding_Target_Auxiliary}Preprocessing API (intent routing)"), ApiUrl = AuxiliaryUrl.Trim(), ApiKey = AuxiliaryKey.Trim(), ModelName = AuxiliaryModel.Trim() },
			new ModOnboardingBehavior.ApiValidationTargetInfo { Target = ModOnboardingBehavior.ApiSetupTarget.ActionPostprocess, DisplayName = Localize("{=AF_UI_Onboarding_Target_Postprocess}Postprocessing API (action tags)"), ApiUrl = PostprocessUrl.Trim(), ApiKey = PostprocessKey.Trim(), ModelName = PostprocessModel.Trim() },
			new ModOnboardingBehavior.ApiValidationTargetInfo { Target = ModOnboardingBehavior.ApiSetupTarget.EventAndRebellion, DisplayName = Localize("{=AF_UI_Onboarding_Target_Event}Daily and weekly-report API"), ApiUrl = EventUrl.Trim(), ApiKey = EventKey.Trim(), ModelName = EventModel.Trim() }
		};

        if (_usingExistingConfig && (string.IsNullOrWhiteSpace(EventUrl) || string.IsNullOrWhiteSpace(EventKey) || string.IsNullOrWhiteSpace(EventModel)))
        {
            targets.RemoveAt(targets.Count - 1);
            EventStatusText = new TextObject("{=AF_UI_Onboarding_EventSkipped}Not configured; skipping the optional weekly-report API.").ToString();
        }
        int targetCount = targets.Count;
		TestOverallNotice = FormatLocalized("{=AF_UI_Onboarding_TestingMany}Testing {COUNT} API pipelines in parallel...", "COUNT", targetCount.ToString());
		Task.Run(async () =>
		{
			var tasks = targets.Select(t => TestSingleTargetAsync(t, token, generation)).ToArray();
			ModOnboardingBehavior.ApiValidationTargetResult[] results;
			try
			{
				results = await Task.WhenAll(tasks);
			}
			catch (OperationCanceledException)
			{
				_uiDispatch.PostTest(generation, () => TestOverallNotice = Localize("{=AF_UI_Onboarding_TestCancelled}The test was cancelled by the user."));
				return;
			}
			catch (Exception)
			{
				_uiDispatch.PostTest(generation, () => TestOverallNotice = Localize("{=AF_UI_Onboarding_TestException}The test encountered an error. Check the network and API configuration."));
				return;
			}

			bool allPassed = results.All(r => r != null && r.Success);
			if (allPassed)
			{
                _uiDispatch.PostTest(generation, () =>
                {
                    _testSucceeded = true;
                    TestOverallNotice = FormatLocalized("{=AF_UI_Onboarding_HandshakeManySuccess}✔ {COUNT} API handshakes succeeded! Opening the confirmation screen...", "COUNT", targetCount.ToString());
                });
				try
				{
					await Task.Delay(600, token);
					_uiDispatch.PostTest(generation, () => SwitchView(OnboardingView.Success));
				}
				catch (OperationCanceledException)
				{
				}
			}
			else
			{
				_uiDispatch.PostTest(generation, () => TestOverallNotice = Localize("{=AF_UI_Onboarding_TestPartialFailure}❌ Some API connectivity tests failed. Check the error details and return to edit."));
			}
		});
	}

	private async Task<ModOnboardingBehavior.ApiValidationTargetResult> TestSingleTargetAsync(ModOnboardingBehavior.ApiValidationTargetInfo target, CancellationToken token, int generation)
	{
		var sw = Stopwatch.StartNew();
		ModOnboardingBehavior.ApiValidationTargetResult res = await ModOnboardingBehavior.ValidateApiTargetAsync(target, token);
		sw.Stop();

		long ms = sw.ElapsedMilliseconds;

		_uiDispatch.PostTest(generation, () => ApplyTestTargetResult(target.Target, res, ms));
		return res;
	}

	private void ApplyTestTargetResult(ModOnboardingBehavior.ApiSetupTarget target, ModOnboardingBehavior.ApiValidationTargetResult res, long ms)
	{
		string status = res.Success
			? Localize("{=AF_UI_Onboarding_HandshakeSuccess}Handshake succeeded")
			: ("❌ " + (res.FailureHint ?? Localize("{=AF_UI_Onboarding_Status_Failed}Connection failed")));
		switch (target)
		{
			case ModOnboardingBehavior.ApiSetupTarget.Primary:
				IsPrimarySuccess = res.Success;
				IsPrimaryFailed = !res.Success;
				PrimaryStatusText = status;
				PrimaryLatencyText = res.Success ? (ms + "ms") : "";
				break;
			case ModOnboardingBehavior.ApiSetupTarget.Auxiliary:
				IsAuxiliarySuccess = res.Success;
				IsAuxiliaryFailed = !res.Success;
				AuxiliaryStatusText = status;
				AuxiliaryLatencyText = res.Success ? (ms + "ms") : "";
				break;
			case ModOnboardingBehavior.ApiSetupTarget.ActionPostprocess:
				IsPostprocessSuccess = res.Success;
				IsPostprocessFailed = !res.Success;
				PostprocessStatusText = status;
				PostprocessLatencyText = res.Success ? (ms + "ms") : "";
				break;
			case ModOnboardingBehavior.ApiSetupTarget.EventAndRebellion:
				IsEventSuccess = res.Success;
				IsEventFailed = !res.Success;
				EventStatusText = status;
				EventLatencyText = res.Success ? (ms + "ms") : "";
				break;
		}
	}

	public void ExecuteCancelTest()
	{
		_uiDispatch.CancelTest();
		_testCts?.Cancel();
        bool usedExistingConfig = _usingExistingConfig;
        _usingExistingConfig = false;
        SwitchView(usedExistingConfig ? OnboardingView.Main
            : _currentView == OnboardingView.Testing ? (_isCustomMode ? OnboardingView.MultiApi : OnboardingView.YjModels) : OnboardingView.Main);
	}

	// ================= Actions: Save & Finish =================
	public void ExecuteSaveAndFinish()
	{
        if (!_testSucceeded || _currentView != OnboardingView.Success)
        {
            ShowToast(new TextObject("{=AF_UI_Onboarding_TestRequired}Complete the API connectivity test first.").ToString());
            return;
        }
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings != null && !_usingExistingConfig)
		{
			settings.ApiUrl = PrimaryUrl.Trim();
			settings.ApiKey = PrimaryKey.Trim();
			ModOnboardingBehavior.SetModelNameForTarget(settings, ModOnboardingBehavior.ApiSetupTarget.Primary, PrimaryModel.Trim());

			settings.AuxiliaryApiUrl = AuxiliaryUrl.Trim();
			settings.AuxiliaryApiKey = AuxiliaryKey.Trim();
			ModOnboardingBehavior.SetModelNameForTarget(settings, ModOnboardingBehavior.ApiSetupTarget.Auxiliary, AuxiliaryModel.Trim());
			settings.UseAuxiliaryRuleApi = true;

			settings.ActionPostprocessApiUrl = PostprocessUrl.Trim();
			settings.ActionPostprocessApiKey = PostprocessKey.Trim();
			ModOnboardingBehavior.SetModelNameForTarget(settings, ModOnboardingBehavior.ApiSetupTarget.ActionPostprocess, PostprocessModel.Trim());

			settings.EventAndRebellionApiUrl = EventUrl.Trim();
			settings.EventAndRebellionApiKey = EventKey.Trim();
			ModOnboardingBehavior.SetModelNameForTarget(settings, ModOnboardingBehavior.ApiSetupTarget.EventAndRebellion, EventModel.Trim());

			ModOnboardingBehavior.TryPersistMcmSettings(settings);
		}

		_onCompleted?.Invoke();
	}

	// ================= Actions: Navigation & Dialogs =================
	public void ExecuteBackToMain()
	{
		_usingExistingConfig = false;
		SwitchView(OnboardingView.Main);
	}

	public void ExecuteBackToYjMenu()
	{
		SwitchView(OnboardingView.YjMenu);
	}

	public void ExecuteBackFromMultiApi()
	{
		SwitchView(_isCustomMode ? OnboardingView.Main : OnboardingView.YjMenu);
	}

	public void ExecuteClose()
	{
        CancelYjProbe();
		_uiDispatch.CancelTest();
		_testCts?.Cancel();
		_onCancelled?.Invoke();
	}

	public void ExecuteOpenSupport()
	{
        CancelYjProbe();
		IsSupportModalVisible = true;
	}

	public void ExecuteCloseSupportModal()
	{
		IsSupportModalVisible = false;
	}

	public void ExecuteOpenAfdianWeb()
	{
		DuelSettings.OpenAfdianSupportPageForExternal();
		ShowToast(new TextObject("{=AF_UI_Onboarding_OpenSupport}Opening the Afdian support page...").ToString());
	}

	// Prompt Overlay Helpers
	private void OpenKeyPrompt(string title, string hint, Action<string> onConfirm)
	{
        CancelYjProbe();
		KeyPromptTitle = title;
		KeyPromptHint = hint;
		PromptKeyInput = "";
		_keyPromptConfirmAction = onConfirm;
		IsKeyPromptVisible = true;
	}

	public void ExecuteConfirmPromptKey()
	{
		IsKeyPromptVisible = false;
		_keyPromptConfirmAction?.Invoke(PromptKeyInput?.Trim() ?? "");
	}

	public void ExecuteCancelPromptKey()
	{
		IsKeyPromptVisible = false;
		_keyPromptConfirmAction = null;
	}

	// Direct input helper popups for pasting
	public void ExecuteEditPrimaryUrl() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditPrimaryUrl}Edit main API Base URL").ToString(), PrimaryUrl, v => PrimaryUrl = v);
	public void ExecuteEditPrimaryKey() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditPrimaryKey}Edit main API key").ToString(), PrimaryKey, v => PrimaryKey = v);
	public void ExecuteEditAuxiliaryUrl() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditAuxUrl}Edit preprocessing Base URL").ToString(), AuxiliaryUrl, v => AuxiliaryUrl = v);
	public void ExecuteEditAuxiliaryKey() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditAuxKey}Edit preprocessing key").ToString(), AuxiliaryKey, v => AuxiliaryKey = v);
	public void ExecuteEditPostprocessUrl() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditPostUrl}Edit postprocessing Base URL").ToString(), PostprocessUrl, v => PostprocessUrl = v);
	public void ExecuteEditPostprocessKey() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditPostKey}Edit postprocessing key").ToString(), PostprocessKey, v => PostprocessKey = v);
	public void ExecuteEditEventUrl() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditEventUrl}Edit weekly-report API Base URL").ToString(), EventUrl, v => EventUrl = v);
	public void ExecuteEditEventKey() => OpenTextInquiryForField(new TextObject("{=AF_UI_Onboarding_EditEventKey}Edit weekly-report key").ToString(), EventKey, v => EventKey = v);
	public void ExecuteEditPromptKey() => PromptKeyInput = TaleWorlds.InputSystem.Input.GetClipboardText() ?? PromptKeyInput;

	private void OpenTextInquiryForField(string title, string initialText, Action<string> onConfirmed)
	{
        // Keep field editing inside the owning 4000-layer popup: vanilla inquiries sit below it.
        KeyPromptTitle = title;
        KeyPromptHint = Localize("{=AF_UI_Onboarding_EditHint}Enter or paste the value (Ctrl+V), then confirm to save or cancel to keep the old value.");
        PromptKeyInput = initialText ?? "";
        _keyPromptConfirmAction = onConfirmed;
        IsKeyPromptVisible = true;
	}

	public void ShowToast(string message)
	{
		ToastMessage = message;
		IsToastVisible = true;
		_toastTimer = 3.0f;
	}

	public void OnTick()
	{
		try
		{
			_uiDispatch.Pump(32);
		}
		catch (Exception)
		{
			ShowToast(Localize("{=AF_UI_Onboarding_UpdateFailed}The interface update failed. Retry the current operation."));
		}
		if (IsToastVisible)
		{
			_toastTimer -= 0.05f;
			if (_toastTimer <= 0f)
			{
				IsToastVisible = false;
			}
		}
	}

	private static string Localize(string value) => new TextObject(value).ToString();

	private static string FormatLocalized(string value, string variable, string replacement)
	{
		return DeveloperUiText.FormatTemplate(value, (variable, (object)replacement));
	}

	public override void OnFinalize()
	{
        CancelYjProbe();
		_uiDispatch.Close();
		base.OnFinalize();
		_testCts?.Cancel();
		_testCts?.Dispose();
		_testCts = null;
	}
}
