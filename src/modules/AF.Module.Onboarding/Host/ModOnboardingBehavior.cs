using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Modules;
using OnboardingOperationKind = AnimusForge.Refactor.Modules.OnboardingOperationVersionOwner.Kind;
using MCM.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class ModOnboardingBehavior : CampaignBehaviorBase
{
	private enum OnboardingUiStage
	{
		None,
		SetupModeChoice,
		YjApiChoice,
		YjApiKey,
		Welcome,
		DeepSeekApiKeyOwnership,
		QuickPresetApiKey,
		AuxiliaryChoice,
		PostprocessChoice,
		EventRebellionChoice,
		BaseUrlValidation,
		BaseUrlValidationFailure,
		ApiValidation,
		ModelFetch,
		ModelSelect,
		Import,
        PlayerPersona
	}

	internal enum ApiSetupTarget
	{
		Primary,
		Auxiliary,
		ActionPostprocess,
		EventAndRebellion
	}

	internal enum QuickApiPreset
	{
		None,
		DeepSeekFlash,
		DeepSeekPro
	}

	internal enum YjApiSetupMode
	{
		None,
		SingleGroup,
		MultiGroup
	}

	internal enum SaveAndExitStage
	{
		None,
		WaitingForCurrentSave,
		WaitingForRequestedQuickSave
	}

	internal enum ApiValidationFlow
	{
		Normal,
		QuickPresetAll,
		ExistingConfigAll
	}

	internal sealed class ApiValidationTargetInfo
	{
		public ApiSetupTarget Target { get; set; }

		public string DisplayName { get; set; } = "";

		public string ApiUrl { get; set; } = "";

		public string ApiKey { get; set; } = "";

		public string ModelName { get; set; } = "";
	}

	internal sealed class ApiValidationTargetResult
	{
		public ApiValidationTargetInfo Target { get; set; }

		public bool Success { get; set; }

		public string Message { get; set; } = "";

		public string FailureHint { get; set; } = "";
	}

	private const string SetupDoneKey = "_AnimusForge_setup_done_v1";

	private const string DeepSeekApiBaseUrl = "https://api.deepseek.com";

	private const string DeepSeekApiKeysUrl = "https://platform.deepseek.com/api_keys";

    private string YjApiBaseUrl = YjEndpointProbe.DefaultUrl;
    private bool _yjEndpointSelected;
    private CancellationTokenSource _yjEndpointCts;
    private Task<YjEndpointProbe.Result> _yjEndpointTask;
    private long _yjEndpointGeneration;

    private void CancelYjEndpointSelection()
    {
        _yjEndpointCts?.Cancel();
        _yjEndpointCts?.Dispose();
        _yjEndpointCts = null; _yjEndpointTask = null;
        _yjEndpointSelected = false;
    }

    private void BeginYjEndpointSelection()
    {
        if (_yjEndpointTask != null) return;
        _yjEndpointGeneration = SaveRuntimeGuard.CaptureGeneration();
        _yjEndpointCts = new CancellationTokenSource();
        _activeOnboardingStage = OnboardingUiStage.None;
        _welcomeInProgress = true;
        _yjEndpointTask = YjEndpointProbe.SelectAsync(_yjEndpointCts.Token);
        var expected = _yjEndpointTask;
        InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_YJ_LATENCY_TITLE", "YJ route latency test"), OnboardingUiText.T("AF_ONBOARDING_YJ_LATENCY_BODY", "Comparing three YJ routes. After the test, choose a connection method.\nNo key is sent and no model generation is performed."),
            isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", OnboardingUiText.T("AF_COMMON_CANCEL", "Cancel"), null, () => {
                if (!ReferenceEquals(_yjEndpointTask, expected)) return;
                CancelYjEndpointSelection(); _welcomeInProgress = false;
                ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
            }), pauseGameActiveState: true);
    }

    private void ProcessYjEndpointSelection()
    {
        if (_yjEndpointTask == null) return;
        if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(_yjEndpointGeneration))
        { CancelYjEndpointSelection(); _welcomeInProgress = false; return; }
        if (!_yjEndpointTask.IsCompleted) return;
        YjEndpointProbe.Result result = null;
        try { result = _yjEndpointTask.GetAwaiter().GetResult(); } catch (Exception) { }
        CancelYjEndpointSelection(); _welcomeInProgress = false;
        InformationManager.HideInquiry();
        if (result == null)
        {
            InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_YJ_LATENCY_FAILED", "All three YJ routes failed the latency test. Check your network and try again, or use custom settings.")));
            ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
            return;
        }
        YjApiBaseUrl = result.Url; _yjEndpointSelected = true;
        InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_YJ_SELECTED", "Selected YJ route: ") + new Uri(result.Url).Host + OnboardingUiText.T("AF_ONBOARDING_YJ_RESPONSE_TIME", ", response about ") + Math.Round(result.Milliseconds) + OnboardingUiText.T("AF_ONBOARDING_LATENCY_SUFFIX", " ms.")));
        ShowYjApiSetupMenu();
    }

	private const string YjApiKeysUrl = "https://yjapi.manqiaotechnology.com/keys";

	private const string YjApiPurchaseUrl = "https://pay.ldxp.cn/shop/OF6AKWNI";

	private const string YjApiPlayerQqGroupNumber = "1097237977";

	private const string DeepSeekFlashModelName = "deepseek-v4-flash";

	private const string DeepSeekProModelName = "deepseek-v4-pro";

	private bool _setupDone;

	private readonly OnboardingSessionOwner _onboardingSession = new OnboardingSessionOwner();
	private readonly OnboardingDatabaseImportController _databaseImport = new OnboardingDatabaseImportController();
	private readonly OnboardingDismissalOwner<OnboardingUiStage> _dismissalOwner = new OnboardingDismissalOwner<OnboardingUiStage>();
	private readonly OnboardingOperationVersionOwner _operationVersions = new OnboardingOperationVersionOwner();

	private bool _welcomeInProgress;

	private long _suppressWelcomeUntilUtcTicks;

	private bool _apiValidationInProgress;

	private CancellationTokenSource _apiValidationCancellation;

	private volatile bool _pendingApiValidationResult;

	private int _pendingApiValidationVersion;

	private bool _pendingApiValidationSuccess;

	private string _pendingApiValidationMessage = "";

	private string _pendingApiValidationFailureHint = "";

	private bool _apiValidationReturnToModelSelection;

	private bool _baseUrlValidationInProgress;

	private CancellationTokenSource _baseUrlValidationCancellation;

	private volatile bool _pendingBaseUrlValidationResult;
	private int _pendingBaseUrlValidationVersion;

	private bool _pendingBaseUrlValidationSuccess;

	private string _pendingBaseUrlValidationMessage = "";

	private string _pendingValidatedBaseUrl = "";

	private string _lastBaseUrlValidationFailureMessage = "";

	private bool _modelFetchInProgress;

	private CancellationTokenSource _modelFetchCancellation;

	private volatile bool _pendingModelFetchResult;

	private int _pendingModelFetchVersion;

	private bool _pendingModelFetchSuccess;

	private string _pendingModelFetchMessage = "";

	private List<string> _pendingModelFetchModels = new List<string>();

	private List<string> _lastFetchedModelNames = new List<string>();

	private bool _pendingReturnToWelcome;

	private OnboardingUiStage _activeOnboardingStage;


	private ApiSetupTarget _currentApiSetupTarget;

	private bool _apiRepairFlowActive;

	private QuickApiPreset _selectedQuickApiPreset;

	private bool _quickPresetFlowActive;

	private YjApiSetupMode _yjApiSetupMode;

	private bool _yjSingleGroupKeyConfirmed;

	private bool _apiOnlySetupFlowActive;

	private SaveAndExitStage _saveAndExitStage;

	private ApiValidationFlow _apiValidationFlow;


	public static ModOnboardingBehavior Instance { get; private set; }

    internal bool IsSetupUiActive => _welcomeInProgress || _activeOnboardingStage != OnboardingUiStage.None
        || AnimusForgeApiOnboardingPopup.IsOpen;


	public ModOnboardingBehavior()
	{
		Instance = this;
		_currentApiSetupTarget = ApiSetupTarget.Primary;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_AnimusForge_setup_done_v1", ref _setupDone);
		if (!_setupDone)
		{
			_onboardingSession.ResetWelcomeShown();
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
        CancelYjEndpointSelection();
        _activeOnboardingStage = OnboardingUiStage.None;
        _welcomeInProgress = false;
		// Campaign behaviors can survive a return to the campaign setup flow. A new
		// sandbox must never inherit the previous campaign's completed onboarding bit.
		_setupDone = false;
		_onboardingSession.ResetWelcomeShown();
		MarkPendingStartupNotice();
		MarkPendingWelcome();
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
        CancelYjEndpointSelection();
        _activeOnboardingStage = OnboardingUiStage.None;
        _welcomeInProgress = false;
		MarkPendingStartupNotice();
		if (!_setupDone)
		{
			MarkPendingWelcome();
		}
		else if (ShouldPromptActionPostprocessSetup())
		{
			MarkPendingActionPostprocessSetup();
		}
	}

	private void MarkPendingWelcome()
	{
		try
		{
			_onboardingSession.MarkWelcome(DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
	}

	private void MarkPendingStartupNotice()
	{
		try
		{
			_onboardingSession.MarkStartupNotice(DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
	}

	private bool ShouldPromptActionPostprocessSetup()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || !AIConfigHandler.ActionPostprocessEnabled)
			{
				return false;
			}
			return !HasCompleteApiConfigForTarget(settings, ApiSetupTarget.ActionPostprocess);
		}
		catch
		{
			return false;
		}
	}

	private void MarkPendingActionPostprocessSetup()
	{
		try
		{
			_onboardingSession.MarkActionPostprocess(DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
	}

	private void OnTick(float dt)
	{
		try
		{
            ProcessYjEndpointSelection();
			ProcessPendingBaseUrlValidationResult();
			ProcessPendingApiValidationResult();
			ProcessPendingModelFetchResult();
			ProcessPendingReturnToWelcome();
			ProcessUnexpectedOnboardingDismissal();
			if (_onboardingSession.TryClaimStartupNotice(DateTime.UtcNow.Ticks, Campaign.Current != null && Campaign.Current.GameStarted))
			{
				ShowStartupNotice();
			}
			if (MyBehavior.Instance?.IsMissingOnnxGateActive == true) return;
			if (_onboardingSession.TryClaimWelcome(DateTime.UtcNow.Ticks, Campaign.Current != null && Campaign.Current.GameStarted, _setupDone))
			{
				ShowSetupModeChoicePopup(fromGate: false);
			}
			if (_onboardingSession.TryClaimActionPostprocess(DateTime.UtcNow.Ticks, Campaign.Current != null && Campaign.Current.GameStarted, _setupDone))
			{
				ShowActionPostprocessApiSetupPopup(ignoreSuppress: true, allowWhenSetupDone: true);
			}
		}
		catch
		{
		}
	}

	public void OnEngineTick()
	{
		try
		{
            ProcessYjEndpointSelection();
			ProcessPendingBaseUrlValidationResult();
			ProcessPendingApiValidationResult();
			ProcessPendingModelFetchResult();
			ProcessPendingReturnToWelcome();
			ProcessUnexpectedOnboardingDismissal();
		}
		catch
		{
		}
	}

	private bool IsAuxiliaryApiSetupTarget()
	{
		return _currentApiSetupTarget == ApiSetupTarget.Auxiliary;
	}

	private bool IsActionPostprocessApiSetupTarget()
	{
		return _currentApiSetupTarget == ApiSetupTarget.ActionPostprocess;
	}

	private bool IsEventAndRebellionApiSetupTarget()
	{
		return _currentApiSetupTarget == ApiSetupTarget.EventAndRebellion;
	}

	private bool IsYjApiSetupActive()
	{
		return _yjApiSetupMode != YjApiSetupMode.None && (!_setupDone || _apiOnlySetupFlowActive) && !_apiRepairFlowActive;
	}

	private bool IsYjApiSetupAvailable()
	{
		return (!_setupDone || _apiOnlySetupFlowActive) && !_apiRepairFlowActive;
	}

	private void ResetYjApiSetup()
	{
		_yjApiSetupMode = YjApiSetupMode.None;
		_yjSingleGroupKeyConfirmed = false;
	}

	private string CurrentApiDisplayName()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_AUXILIARY", "Preprocessing API");
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_POSTPROCESS", "Postprocessing API");
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_EVENTS", "Events/rebellion API");
		}
		return OnboardingUiText.T("AF_ONBOARDING_API_PRIMARY", "Primary API");
	}

	private string CurrentApiBaseUrlDisplayName()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_AUXILIARY_URL", "Preprocessing API Base URL");
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_POSTPROCESS_URL", "Postprocessing API Base URL");
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_EVENTS_URL", "Events/rebellion API Base URL");
		}
		return OnboardingUiText.T("AF_ONBOARDING_API_PRIMARY_URL", "Primary API Base URL");
	}

	private string CurrentApiKeyDisplayName()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_AUXILIARY_KEY", "Preprocessing API key");
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_POSTPROCESS_KEY", "Postprocessing API key");
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_EVENTS_KEY", "Events/rebellion API key");
		}
		return OnboardingUiText.T("AF_ONBOARDING_API_PRIMARY_KEY", "Primary API key");
	}

	private string CurrentApiModelDisplayName()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_AUXILIARY_MODEL", "Preprocessing model name");
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_POSTPROCESS_MODEL", "Postprocessing model name");
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_EVENTS_MODEL", "Events/rebellion model name");
		}
		return OnboardingUiText.T("AF_ONBOARDING_API_PRIMARY_MODEL", "Primary model name");
	}

	private string CurrentYjApiModelRecommendation()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_AUXILIARY_HINT", "Preprocessing API: choose a fast, lower-cost model.");
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_POSTPROCESS_HINT", "Postprocessing API: choose a model with strong analysis and reasoning, such as GPT.");
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_EVENTS_HINT", "Weekly reports and rebellion API: choose a model with good writing, such as Gemini. Rebellion naming uses this model too.");
		}
		return OnboardingUiText.T("AF_ONBOARDING_API_PRIMARY_HINT", "Primary API (NPC dialogue): choose a model with good writing, such as Gemini.");
	}

	private string CurrentApiBaseUrlExample()
	{
		return "https://api.openai.com/v1";
	}

	private bool ShouldDisplayContextExtractionApiWarningForCurrentTarget()
	{
		return !IsAuxiliaryApiSetupTarget();
	}

	private bool ShouldPromptContextExtractionApiWarning(string rawApiUrl)
	{
		if (!ShouldDisplayContextExtractionApiWarningForCurrentTarget())
		{
			return false;
		}
		return DuelSettings.ShouldWarnForContextExtractionApi(rawApiUrl);
	}

	private bool ShowContextExtractionApiWarningInquiry(Action onContinue, Action onReturn)
	{
		try
		{
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_COMPAT_WARNING_TITLE", "Compatibility warning"), DuelSettings.GetContextExtractionCompatibilityWarningMessage() + OnboardingUiText.T("AF_ONBOARDING_COMPAT_CONTINUE", "\n\nContinue this process?"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_COMMON_CONTINUE", "Continue"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate
			{
				onContinue?.Invoke();
			}, delegate
			{
				onReturn?.Invoke();
			}), pauseGameActiveState: true);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void SetApiSetupTarget(ApiSetupTarget target)
	{
		_currentApiSetupTarget = target;
	}

	private void SetApiRepairFlowActive(bool active)
	{
		_apiRepairFlowActive = active;
	}

	private static string GetApiUrlForTarget(DuelSettings settings, ApiSetupTarget target)
	{
		if (settings == null)
		{
			return "";
		}
		if (target == ApiSetupTarget.Auxiliary)
		{
			return settings.AuxiliaryApiUrl ?? "";
		}
		if (target == ApiSetupTarget.ActionPostprocess)
		{
			return settings.ActionPostprocessApiUrl ?? "";
		}
		if (target == ApiSetupTarget.EventAndRebellion)
		{
			return settings.EventAndRebellionApiUrl ?? "";
		}
		return settings.ApiUrl ?? "";
	}

	private static string GetApiKeyForTarget(DuelSettings settings, ApiSetupTarget target)
	{
		if (settings == null)
		{
			return "";
		}
		if (target == ApiSetupTarget.Auxiliary)
		{
			return settings.AuxiliaryApiKey ?? "";
		}
		if (target == ApiSetupTarget.ActionPostprocess)
		{
			return settings.ActionPostprocessApiKey ?? "";
		}
		if (target == ApiSetupTarget.EventAndRebellion)
		{
			return settings.EventAndRebellionApiKey ?? "";
		}
		return settings.ApiKey ?? "";
	}

	private static string GetModelNameForTarget(DuelSettings settings, ApiSetupTarget target)
	{
		if (settings == null)
		{
			return "";
		}
		if (target == ApiSetupTarget.Auxiliary)
		{
			return settings.AuxiliaryModelName ?? "";
		}
		if (target == ApiSetupTarget.ActionPostprocess)
		{
			return settings.ActionPostprocessModelName ?? "";
		}
		if (target == ApiSetupTarget.EventAndRebellion)
		{
			return settings.GetEffectiveEventAndRebellionModelName() ?? "";
		}
		return settings.ModelName ?? "";
	}

	private static bool HasCompleteApiConfigForTarget(DuelSettings settings, ApiSetupTarget target)
	{
		if (settings == null)
		{
			return false;
		}
		return !string.IsNullOrWhiteSpace(GetApiUrlForTarget(settings, target))
			&& !string.IsNullOrWhiteSpace(GetApiKeyForTarget(settings, target))
			&& !string.IsNullOrWhiteSpace(GetModelNameForTarget(settings, target));
	}

	private static void SetApiUrlForTarget(DuelSettings settings, ApiSetupTarget target, string value)
	{
		if (settings == null)
		{
			return;
		}
		if (target == ApiSetupTarget.Auxiliary)
		{
			settings.AuxiliaryApiUrl = value ?? "";
		}
		else if (target == ApiSetupTarget.ActionPostprocess)
		{
			settings.ActionPostprocessApiUrl = value ?? "";
		}
		else if (target == ApiSetupTarget.EventAndRebellion)
		{
			settings.EventAndRebellionApiUrl = value ?? "";
		}
		else
		{
			settings.ApiUrl = value ?? "";
		}
	}

	private static void SetApiKeyForTarget(DuelSettings settings, ApiSetupTarget target, string value)
	{
		if (settings == null)
		{
			return;
		}
		if (target == ApiSetupTarget.Auxiliary)
		{
			settings.AuxiliaryApiKey = value ?? "";
		}
		else if (target == ApiSetupTarget.ActionPostprocess)
		{
			settings.ActionPostprocessApiKey = value ?? "";
		}
		else if (target == ApiSetupTarget.EventAndRebellion)
		{
			settings.EventAndRebellionApiKey = value ?? "";
		}
		else
		{
			settings.ApiKey = value ?? "";
		}
	}

	private void SetYjApiBaseUrlForAllTargets(DuelSettings settings)
	{
		SetApiUrlForTarget(settings, ApiSetupTarget.Primary, YjApiBaseUrl);
		SetApiUrlForTarget(settings, ApiSetupTarget.Auxiliary, YjApiBaseUrl);
		SetApiUrlForTarget(settings, ApiSetupTarget.ActionPostprocess, YjApiBaseUrl);
		SetApiUrlForTarget(settings, ApiSetupTarget.EventAndRebellion, YjApiBaseUrl);
	}

	private static void SetYjApiKeyForAllTargets(DuelSettings settings, string value)
	{
		SetApiKeyForTarget(settings, ApiSetupTarget.Primary, value);
		SetApiKeyForTarget(settings, ApiSetupTarget.Auxiliary, value);
		SetApiKeyForTarget(settings, ApiSetupTarget.ActionPostprocess, value);
		SetApiKeyForTarget(settings, ApiSetupTarget.EventAndRebellion, value);
	}

	internal static void SetModelNameForTarget(DuelSettings settings, ApiSetupTarget target, string value)
	{
		if (settings == null)
		{
			return;
		}
		if (target == ApiSetupTarget.Auxiliary)
		{
			settings.AuxiliaryModelName = value ?? "";
			settings.ForceAuxiliaryModelDropdownToManual();
		}
		else if (target == ApiSetupTarget.ActionPostprocess)
		{
			settings.ActionPostprocessModelName = value ?? "";
			settings.ForceActionPostprocessModelDropdownToManual();
		}
		else if (target == ApiSetupTarget.EventAndRebellion)
		{
			settings.EventAndRebellionModelName = value ?? "";
			settings.ForceEventAndRebellionModelDropdownToManual();
		}
		else
		{
			settings.ModelName = value ?? "";
			settings.ForceMainModelDropdownToManual();
		}
	}

	private static void ApplyYjGeminiPresetThinkingDefaults(DuelSettings settings, ApiSetupTarget target, string modelName)
	{
		if (settings == null || !YjThinkingCompat.IsYjGeminiEndpoint(GetApiUrlForTarget(settings, target), modelName))
		{
			return;
		}
		string normalizedModel = (modelName ?? string.Empty).Trim();
		bool isGemini37FlashHigh = normalizedModel.StartsWith("gemini-3.7-flash-high", StringComparison.OrdinalIgnoreCase);
		bool thinkingEnabled = true;
		string effort = isGemini37FlashHigh ? DuelSettings.ReasoningEffortHigh : DuelSettings.ReasoningEffortMinimal;
		switch (target)
		{
		case ApiSetupTarget.Auxiliary:
			settings.AuxiliaryApiThinkingEnabled = thinkingEnabled;
			settings.SetAuxiliaryApiReasoningEffortForExternal(effort);
			break;
		case ApiSetupTarget.ActionPostprocess:
			settings.ActionPostprocessApiThinkingEnabled = thinkingEnabled;
			settings.SetActionPostprocessApiReasoningEffortForExternal(effort);
			break;
		case ApiSetupTarget.EventAndRebellion:
			settings.EventAndRebellionApiThinkingEnabled = thinkingEnabled;
			settings.SetEventAndRebellionApiReasoningEffortForExternal(effort);
			break;
		default:
			settings.MainApiThinkingEnabled = thinkingEnabled;
			settings.SetMainApiReasoningEffortForExternal(effort);
			break;
		}
	}

	private void ReopenCurrentApiEntry(bool ignoreSuppress = true)
	{
		if (IsYjApiSetupActive())
		{
			OpenYjApiKeyInput();
			return;
		}
		if (_quickPresetFlowActive && !IsAuxiliaryApiSetupTarget() && !IsActionPostprocessApiSetupTarget() && !IsEventAndRebellionApiSetupTarget())
		{
			ShowQuickPresetApiKeyInput(_selectedQuickApiPreset);
			return;
		}
		if (_apiRepairFlowActive)
		{
			if (IsAuxiliaryApiSetupTarget())
			{
				ShowAuxiliaryApiRepairPopup();
			}
			else if (IsActionPostprocessApiSetupTarget())
			{
				ShowActionPostprocessApiRepairPopup();
			}
			else if (IsEventAndRebellionApiSetupTarget())
			{
				ShowEventAndRebellionApiRepairPopup();
			}
			else
			{
				ShowApiRepairPopup();
			}
		}
		else if (IsAuxiliaryApiSetupTarget())
		{
			ShowAuxiliaryApiSetupPopup(ignoreSuppress);
		}
		else if (IsActionPostprocessApiSetupTarget())
		{
			ShowActionPostprocessApiSetupPopup(ignoreSuppress, allowWhenSetupDone: true);
		}
		else
		{
			ShowWelcomePopup(fromGate: true, ignoreSuppress: ignoreSuppress);
		}
	}

	private void ProcessPendingReturnToWelcome()
	{
		if (!_pendingReturnToWelcome)
		{
			return;
		}
		_pendingReturnToWelcome = false;
		ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
	}

	private void ProcessUnexpectedOnboardingDismissal()
	{
		if (_saveAndExitStage != SaveAndExitStage.None)
		{
			_dismissalOwner.Reset();
			return;
		}
		if ((_setupDone && !_apiOnlySetupFlowActive) || _pendingReturnToWelcome || _pendingBaseUrlValidationResult || _pendingApiValidationResult || _pendingModelFetchResult)
		{
			_dismissalOwner.Reset();
			return;
		}
		if (_activeOnboardingStage != OnboardingUiStage.SetupModeChoice && _activeOnboardingStage != OnboardingUiStage.YjApiChoice && _activeOnboardingStage != OnboardingUiStage.YjApiKey && _activeOnboardingStage != OnboardingUiStage.Welcome && _activeOnboardingStage != OnboardingUiStage.DeepSeekApiKeyOwnership && _activeOnboardingStage != OnboardingUiStage.QuickPresetApiKey && _activeOnboardingStage != OnboardingUiStage.AuxiliaryChoice && _activeOnboardingStage != OnboardingUiStage.PostprocessChoice && _activeOnboardingStage != OnboardingUiStage.EventRebellionChoice && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidation && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidationFailure && _activeOnboardingStage != OnboardingUiStage.ApiValidation && _activeOnboardingStage != OnboardingUiStage.ModelFetch && _activeOnboardingStage != OnboardingUiStage.ModelSelect && _activeOnboardingStage != OnboardingUiStage.Import)
		{
			_dismissalOwner.Reset();
			return;
		}
		if (InformationManager.IsAnyInquiryActive() || AnimusForgeApiOnboardingPopup.IsOpen)
		{
			_dismissalOwner.Reset();
			return;
		}
		if (!_dismissalOwner.TryClaim(_activeOnboardingStage, DateTime.UtcNow.Ticks, out OnboardingUiStage pendingUnexpectedResumeStage))
		{
			return;
		}
		_welcomeInProgress = false;
		switch (pendingUnexpectedResumeStage)
		{
		case OnboardingUiStage.SetupModeChoice:
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			break;
		case OnboardingUiStage.YjApiChoice:
			ShowYjApiSetupMenu();
			break;
		case OnboardingUiStage.YjApiKey:
			OpenYjApiKeyInput();
			break;
		case OnboardingUiStage.DeepSeekApiKeyOwnership:
			ShowDeepSeekApiKeyOwnershipInquiry(_selectedQuickApiPreset);
			break;
		case OnboardingUiStage.QuickPresetApiKey:
			ShowQuickPresetApiKeyInput(_selectedQuickApiPreset);
			break;
		case OnboardingUiStage.AuxiliaryChoice:
			ShowAuxiliaryApiSetupPopup(ignoreSuppress: true);
			break;
		case OnboardingUiStage.PostprocessChoice:
			ShowActionPostprocessApiSetupPopup(ignoreSuppress: true, allowWhenSetupDone: true);
			break;
		case OnboardingUiStage.EventRebellionChoice:
			ShowEventAndRebellionApiRepairPopup();
			break;
		case OnboardingUiStage.BaseUrlValidation:
			if (_baseUrlValidationInProgress)
			{
				ShowBaseUrlValidationProgressPopup();
			}
			break;
		case OnboardingUiStage.BaseUrlValidationFailure:
			ShowBaseUrlValidationFailurePopup();
			break;
		case OnboardingUiStage.ApiValidation:
			if (_apiValidationInProgress)
			{
				ShowApiValidationProgressPopup();
			}
			break;
		case OnboardingUiStage.ModelFetch:
			if (_modelFetchInProgress)
			{
				ShowModelFetchProgressPopup();
			}
			break;
		case OnboardingUiStage.ModelSelect:
			ShowModelSelectionPopup();
			break;
		case OnboardingUiStage.Import:
			ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
			break;
		case OnboardingUiStage.Welcome:
			ReopenCurrentApiEntry(ignoreSuppress: true);
			break;
		}
	}

	private void ProcessPendingBaseUrlValidationResult()
	{
		if (!_pendingBaseUrlValidationResult)
		{
			return;
		}
		int pendingBaseUrlValidationVersion = _pendingBaseUrlValidationVersion;
		bool pendingBaseUrlValidationSuccess = _pendingBaseUrlValidationSuccess;
		string pendingBaseUrlValidationMessage = _pendingBaseUrlValidationMessage ?? "";
		string pendingValidatedBaseUrl = (_pendingValidatedBaseUrl ?? "").Trim();
		_pendingBaseUrlValidationResult = false;
		_pendingBaseUrlValidationVersion = 0;
		_pendingBaseUrlValidationSuccess = false;
		_pendingBaseUrlValidationMessage = "";
		_pendingValidatedBaseUrl = "";
		if (!_operationVersions.IsCurrent(OnboardingOperationKind.BaseUrlValidation, pendingBaseUrlValidationVersion))
		{
			return;
		}
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		InformationManager.HideInquiry();
		if (pendingBaseUrlValidationSuccess)
		{
			_lastBaseUrlValidationFailureMessage = "";
			if (!string.IsNullOrWhiteSpace(pendingBaseUrlValidationMessage))
			{
				InformationManager.DisplayMessage(new InformationMessage(pendingBaseUrlValidationMessage));
			}
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_SAVE_URL_FAILED", "MCM settings could not be read; the Base URL cannot be saved right now.")));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			SetApiUrlForTarget(settings, _currentApiSetupTarget, pendingValidatedBaseUrl);
			TryPersistMcmSettings(settings);
			OpenApiKeyInput();
		}
		else
		{
			// Base URL 输入框本身已有返回入口；失败详情改为左下角消息后直接回到输入，避免额外报告遮挡操作。
			_lastBaseUrlValidationFailureMessage = pendingBaseUrlValidationMessage;
			ShowBaseUrlValidationFailurePopup();
		}
	}

	private void ProcessPendingModelFetchResult()
	{
		if (!_pendingModelFetchResult)
		{
			return;
		}
		int pendingModelFetchVersion = _pendingModelFetchVersion;
		bool pendingModelFetchSuccess = _pendingModelFetchSuccess;
		string pendingModelFetchMessage = _pendingModelFetchMessage ?? "";
		List<string> list = _pendingModelFetchModels?.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
		_pendingModelFetchResult = false;
		_pendingModelFetchVersion = 0;
		_pendingModelFetchSuccess = false;
		_pendingModelFetchMessage = "";
		_pendingModelFetchModels = new List<string>();
		if (!_operationVersions.IsCurrent(OnboardingOperationKind.ModelFetch, pendingModelFetchVersion))
		{
			return;
		}
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		InformationManager.HideInquiry();
		_lastFetchedModelNames = list;
		if (!string.IsNullOrWhiteSpace(pendingModelFetchMessage))
		{
			if (pendingModelFetchSuccess)
			{
				// 成功状态同样放入左下角，模型选择框只负责呈现可操作选项。
				InformationManager.DisplayMessage(new InformationMessage(pendingModelFetchMessage, Colors.Green));
			}
			else
			{
				NonBlockingErrorReport.Show(OnboardingUiText.T("AF_ONBOARDING_MODEL_FETCH_FAILED", "Model list fetch failed"), pendingModelFetchMessage);
			}
		}
		ShowModelSelectionPopup();
	}

	private void ProcessPendingApiValidationResult()
	{
		if (!_pendingApiValidationResult)
		{
			return;
		}
		int pendingApiValidationVersion = _pendingApiValidationVersion;
		bool pendingApiValidationSuccess = _pendingApiValidationSuccess;
		string pendingApiValidationMessage = _pendingApiValidationMessage ?? "";
		string pendingApiValidationFailureHint = _pendingApiValidationFailureHint ?? "";
		_pendingApiValidationResult = false;
		_pendingApiValidationVersion = 0;
		_pendingApiValidationSuccess = false;
		_pendingApiValidationMessage = "";
		_pendingApiValidationFailureHint = "";
		if (!_operationVersions.IsCurrent(OnboardingOperationKind.ApiValidation, pendingApiValidationVersion))
		{
			return;
		}
		bool apiValidationReturnToModelSelection = _apiValidationReturnToModelSelection;
		_apiValidationReturnToModelSelection = false;
		ApiValidationFlow apiValidationFlow = _apiValidationFlow;
		_apiValidationFlow = ApiValidationFlow.Normal;
		bool setupMenuValidation = apiValidationFlow == ApiValidationFlow.QuickPresetAll || apiValidationFlow == ApiValidationFlow.ExistingConfigAll;
		bool quickPresetPrimaryValidation = !setupMenuValidation && _quickPresetFlowActive && !IsAuxiliaryApiSetupTarget() && !IsActionPostprocessApiSetupTarget() && !IsEventAndRebellionApiSetupTarget();
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		InformationManager.HideInquiry();
		if (pendingApiValidationSuccess && !string.IsNullOrWhiteSpace(pendingApiValidationMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(pendingApiValidationMessage));
		}
		if (pendingApiValidationSuccess)
		{
			if (setupMenuValidation)
			{
				_quickPresetFlowActive = false;
				_selectedQuickApiPreset = QuickApiPreset.None;
				if (_apiOnlySetupFlowActive)
				{
					CompleteApiSetupOnlyFlow();
					return;
				}
				ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			if (quickPresetPrimaryValidation)
			{
				_quickPresetFlowActive = false;
				_selectedQuickApiPreset = QuickApiPreset.None;
				if (_apiOnlySetupFlowActive)
				{
					CompleteApiSetupOnlyFlow();
					return;
				}
				ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			if (IsYjApiSetupActive())
			{
				AdvanceYjApiSetupAfterValidation();
				return;
			}
			if (IsAuxiliaryApiSetupTarget())
			{
				DuelSettings settings = DuelSettings.GetSettings();
				if (settings != null)
				{
					settings.UseAuxiliaryRuleApi = true;
					TryPersistMcmSettings(settings);
				}
			}
			if (_apiRepairFlowActive)
			{
				SetApiRepairFlowActive(active: false);
			}
			else if (IsActionPostprocessApiSetupTarget())
			{
				if (_apiOnlySetupFlowActive)
				{
					CompleteApiSetupOnlyFlow();
					return;
				}
				ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
			}
			else if (IsAuxiliaryApiSetupTarget())
			{
				ShowActionPostprocessApiSetupPopup(ignoreSuppress: true);
			}
			else if (!_setupDone || _apiOnlySetupFlowActive)
			{
				ShowAuxiliaryApiSetupPopup(ignoreSuppress: true);
			}
			else
			{
				ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
			}
		}
		else if (_apiRepairFlowActive || !_setupDone || _apiOnlySetupFlowActive)
		{
			string fullFailureText = pendingApiValidationMessage;
			if (!string.IsNullOrWhiteSpace(pendingApiValidationFailureHint))
			{
				fullFailureText = (fullFailureText + OnboardingUiText.T("AF_ONBOARDING_TROUBLESHOOT", "\n\nTroubleshooting: ") + pendingApiValidationFailureHint).Trim();
			}
			// 失败详情可能含完整模型/API 响应；完整且脱敏后的文本直接显示在左下角，并保留给按需 AI 分析。
			NonBlockingErrorReport.Show(OnboardingUiText.T("AF_ONBOARDING_API_CONNECTION_FAILED", "API connection failed"), fullFailureText);
			if (setupMenuValidation)
			{
				_quickPresetFlowActive = false;
				_selectedQuickApiPreset = QuickApiPreset.None;
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			}
			else if (quickPresetPrimaryValidation)
			{
				ShowQuickPresetApiKeyInput(_selectedQuickApiPreset);
			}
			else if (apiValidationReturnToModelSelection)
			{
				ShowModelSelectionPopup();
			}
			else
			{
				ReopenCurrentApiEntry(ignoreSuppress: true);
			}
		}
	}

	private static void ShowStartupNotice()
	{
		try
		{
			string moduleVersionText = GetModuleVersionText();
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_STARTED", "AnimusForge started, version: ") + moduleVersionText + OnboardingUiText.T("AF_COMMON_PERIOD", "."), Colors.Yellow));
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MOD_WARNING", "This version is not recommended with other gameplay mods. If a crash occurs, check your mod load order first!"), Colors.Yellow));
		}
		catch
		{
		}
	}

	public static bool EnsureSetupReady()
	{
		ModOnboardingBehavior modOnboardingBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<ModOnboardingBehavior>();
		if (modOnboardingBehavior == null)
		{
			return true;
		}
		if (modOnboardingBehavior._setupDone)
		{
			return true;
		}
		modOnboardingBehavior.ShowSetupModeChoicePopup(fromGate: true);
		return false;
	}

	public static bool OpenApiRepairFlow()
	{
		ModOnboardingBehavior modOnboardingBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<ModOnboardingBehavior>();
		if (modOnboardingBehavior == null)
		{
			return false;
		}
		modOnboardingBehavior.ShowApiRepairPopup();
		return true;
	}

	public static bool OpenApiSetupOnlyFlow()
	{
		ModOnboardingBehavior modOnboardingBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<ModOnboardingBehavior>();
		if (modOnboardingBehavior == null)
		{
			return false;
		}
		return modOnboardingBehavior.OpenApiSetupOnlyFlowCore();
	}

	private bool OpenApiSetupOnlyFlowCore()
	{
		try
		{
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_BUSY", "An AnimusForge setup or API test is already running. Complete or cancel it first.")));
				return false;
			}
			ResetYjApiSetup();
			_apiOnlySetupFlowActive = _setupDone;
			_onboardingSession.CancelWelcome();
			_pendingReturnToWelcome = false;
			_dismissalOwner.Reset();
			_quickPresetFlowActive = false;
			_selectedQuickApiPreset = QuickApiPreset.None;
			SetApiRepairFlowActive(active: false);
			InformationManager.HideInquiry();
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			return true;
		}
		catch (Exception ex)
		{
			_apiOnlySetupFlowActive = false;
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_RECONFIGURE_FAILED", "Failed to open API reconfiguration: ") + ex.Message));
			return false;
		}
	}

	private void CancelApiSetupOnlyFlow()
	{
		ResetYjApiSetup();
		_apiOnlySetupFlowActive = false;
		_quickPresetFlowActive = false;
		_selectedQuickApiPreset = QuickApiPreset.None;
		SetApiRepairFlowActive(active: false);
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		_dismissalOwner.Reset();
		InformationManager.HideInquiry();
		InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_RECONFIGURED_CANCELLED", "API reconfiguration cancelled.")));
	}

	private void CompleteApiSetupOnlyFlow()
	{
		ResetYjApiSetup();
		_apiOnlySetupFlowActive = false;
		_quickPresetFlowActive = false;
		_selectedQuickApiPreset = QuickApiPreset.None;
		SetApiRepairFlowActive(active: false);
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		_dismissalOwner.Reset();
		TryPersistMcmSettings(DuelSettings.GetSettings());
		InformationManager.HideInquiry();
		InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_RECONFIGURED_DONE", "API reconfiguration complete: settings were written to MCM and the game was resumed. Database import and first-use setup are skipped.")));
	}

	public static bool OpenAuxiliaryApiRepairFlow()
	{
		ModOnboardingBehavior modOnboardingBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<ModOnboardingBehavior>();
		if (modOnboardingBehavior == null)
		{
			return false;
		}
		modOnboardingBehavior.ShowAuxiliaryApiRepairPopup();
		return true;
	}

	public static bool OpenActionPostprocessApiRepairFlow()
	{
		ModOnboardingBehavior modOnboardingBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<ModOnboardingBehavior>();
		if (modOnboardingBehavior == null)
		{
			return false;
		}
		modOnboardingBehavior.ShowActionPostprocessApiRepairPopup();
		return true;
	}

	public static bool OpenEventAndRebellionApiRepairFlow()
	{
		ModOnboardingBehavior modOnboardingBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<ModOnboardingBehavior>();
		if (modOnboardingBehavior == null)
		{
			return false;
		}
		modOnboardingBehavior.ShowEventAndRebellionApiRepairPopup();
		return true;
	}

	private void ShowWelcomePopup(bool fromGate)
	{
		ShowWelcomePopup(fromGate, ignoreSuppress: false);
	}

	private void ShowSetupModeChoicePopup(bool fromGate)
	{
		ShowSetupModeChoicePopup(fromGate, ignoreSuppress: false);
	}

	private void ShowSetupModeChoicePopup(bool fromGate, bool ignoreSuppress)
	{
        CancelYjEndpointSelection();
		try
		{
			if (AnimusForgeApiOnboardingPopup.IsOpen)
			{
				return;
			}
			if ((!_apiOnlySetupFlowActive && _setupDone) || _welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			bool showYjApiSetupOption = (!_setupDone || _apiOnlySetupFlowActive) && !_apiRepairFlowActive;
			SetApiSetupTarget(ApiSetupTarget.Primary);
			SetApiRepairFlowActive(active: false);
			_quickPresetFlowActive = false;
			_selectedQuickApiPreset = QuickApiPreset.None;
			ResetYjApiSetup();
			long ticks = DateTime.UtcNow.Ticks;
			if (!ignoreSuppress && _suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(fromGate ? 800 : 200).Ticks;
			_activeOnboardingStage = OnboardingUiStage.SetupModeChoice;
			_welcomeInProgress = true;

			if (AnimusForgeApiOnboardingPopup.Show(_apiOnlySetupFlowActive, delegate
			{
				_welcomeInProgress = false;
				if (_apiOnlySetupFlowActive)
				{
					CompleteApiSetupOnlyFlow();
				}
				else
				{
					ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
				}
			}, delegate
			{
				_welcomeInProgress = false;
				if (_apiOnlySetupFlowActive)
				{
					CancelApiSetupOnlyFlow();
				}
				else
				{
					// Explicit close is a dismissal, not a failed popup to reopen next tick.
					_activeOnboardingStage = OnboardingUiStage.None;
					_dismissalOwner.Reset();
					_pendingReturnToWelcome = false;
					_onboardingSession.CancelWelcome();
					InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SETUP_CLOSED", "First-use setup was closed. Reopen it from the terminal; AI features still require setup.")));
				}
			}))
			{
				return;
			}

			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("support", OnboardingUiText.T("AF_ONBOARDING_SUPPORT", "Support the AnimusForge team"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_SUPPORT_HINT", "Open the Afdian support page.")),
				new InquiryElement("deepseek_flash", OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_FLASH", "Play with the official DeepSeek Flash API preset"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_FLASH_HINT", "Automatically fill the DeepSeek Flash preset; enter an API key once.")),
				new InquiryElement("deepseek_pro", OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_PRO", "Play with the official DeepSeek Pro API preset"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_PRO_HINT", "Automatically fill the DeepSeek Pro preset; enter an API key once.")),
				new InquiryElement("custom", OnboardingUiText.T("AF_ONBOARDING_CUSTOM", "Play with fully custom APIs"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_CUSTOM_HINT", "Use the legacy path and fill in Base URL, API key, and model one by one.")),
				new InquiryElement("existing_config", OnboardingUiText.T("AF_ONBOARDING_EXISTING", "Play with the existing configuration"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_EXISTING_HINT", "Test the current MCM configuration directly; weekly report and rebellion APIs are skipped when incomplete."))
			};
			if (showYjApiSetupOption)
			{
				list.Insert(1, new InquiryElement("yj_api", OnboardingUiText.T("AF_ONBOARDING_YJ_PRESET", "Play through the YJ API relay"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_YJ_PRESET_HINT", "Test and choose the fastest YJ route; use one or multiple API key groups.")));
			}
			if (!_apiOnlySetupFlowActive)
			{
				list.Add(new InquiryElement("save_exit", OnboardingUiText.T("AF_ONBOARDING_SAVE_EXIT", "Save and exit"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_SAVE_EXIT_HINT", "Save the current game and return to the main menu.")));
			}
			string text = _apiOnlySetupFlowActive
				? OnboardingUiText.T("AF_ONBOARDING_RECONFIGURE_BODY", "You are reconfiguring APIs from the AnimusForge terminal.\n\nThis flow writes and tests the primary, preprocessing, postprocessing, weekly report, and rebellion APIs, then returns to the game without database import or first-use setup.")
				: OnboardingUiText.T("AF_ONBOARDING_QUICK_SETUP_BODY", "Choose an API setup method for first use.\n\nA recommended preset fills the Base URL, model, reasoning, and temperature for all required APIs; you only need to enter one API key.");
			// API 失败详情由左下角通知承载，快捷配置菜单始终只显示可操作的配置说明。
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(OnboardingUiText.T("AF_ONBOARDING_API_QUICK_SETUP", "AnimusForge - API quick setup"), text, list, isExitShown: false, 0, 1, OnboardingUiText.T("AF_COMMON_CONFIRM", "Confirm"), OnboardingUiText.T("AF_COMMON_CLOSE", "Close"), delegate(List<InquiryElement> selected)
			{
				_welcomeInProgress = false;
				string text2 = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrWhiteSpace(text2))
				{
					if (_apiOnlySetupFlowActive)
					{
						CancelApiSetupOnlyFlow();
					}
					else
					{
						ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
					}
				}
				else if (text2 == "support")
				{
					DuelSettings.OpenAfdianSupportPageForExternal();
					ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				}
				else if (text2 == "deepseek_flash")
				{
					ShowDeepSeekApiKeyOwnershipInquiry(QuickApiPreset.DeepSeekFlash);
				}
				else if (text2 == "deepseek_pro")
				{
					ShowDeepSeekApiKeyOwnershipInquiry(QuickApiPreset.DeepSeekPro);
				}
				else if (text2 == "yj_api")
				{
					ShowYjApiSetupMenu();
				}
				else if (text2 == "custom")
				{
					ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
				}
				else if (text2 == "existing_config")
				{
					BeginValidateExistingApiConfigAndContinue();
				}
				else if (text2 == "save_exit")
				{
					BeginSaveAndExitCurrentGameFromOnboarding();
				}
				else
				{
					ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				}
			}, delegate
			{
				_welcomeInProgress = false;
				if (_apiOnlySetupFlowActive)
				{
					CancelApiSetupOnlyFlow();
				}
				else
				{
					ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				}
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowYjApiSetupMenu()
	{
		try
		{
			if (!IsYjApiSetupAvailable() || _welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
            if (!_yjEndpointSelected) { BeginYjEndpointSelection(); return; }
			_activeOnboardingStage = OnboardingUiStage.YjApiChoice;
			_welcomeInProgress = true;
			string yjBackLabel = _apiOnlySetupFlowActive ? OnboardingUiText.T("AF_ONBOARDING_BACK_RECONFIGURE", "Back to API reconfiguration menu") : OnboardingUiText.T("AF_ONBOARDING_BACK_WELCOME", "Back to first-use setup menu");
			string yjBackDescription = _apiOnlySetupFlowActive ? OnboardingUiText.T("AF_ONBOARDING_BACK_RECONFIGURE_HINT", "Return to the AnimusForge API reconfiguration menu.") : OnboardingUiText.T("AF_ONBOARDING_BACK_WELCOME_HINT", "Return to the AnimusForge first-use API setup menu.");
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("yj_prepare_account", OnboardingUiText.T("AF_ONBOARDING_YJ_ACCOUNT", "Get an API key and buy credits"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_YJ_ACCOUNT_HINT", "Open the YJ API key management and credit purchase pages.")),
				new InquiryElement("yj_multi_group", OnboardingUiText.T("AF_ONBOARDING_YJ_MULTI", "Multiple API key groups"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_YJ_MULTI_HINT", "Enter one key for each API group and fetch models for each group.")),
				new InquiryElement("yj_single_group", OnboardingUiText.T("AF_ONBOARDING_YJ_SINGLE", "Single API key group"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_YJ_SINGLE_HINT", "Enter one key to fill all four APIs; models are still fetched and selected separately.")),
				new InquiryElement("yj_back", yjBackLabel, null, isEnabled: true, yjBackDescription),
				new InquiryElement("yj_copy_qq_group", OnboardingUiText.T("AF_ONBOARDING_YJ_COPY_QQ", "Copy YJ relay QQ group"), null, isEnabled: true, OnboardingUiText.T("AF_ONBOARDING_YJ_COPY_QQ_HINT", "Copy QQ group ") + YjApiPlayerQqGroupNumber + OnboardingUiText.T("AF_ONBOARDING_TO_CLIPBOARD", " to the clipboard."))
			};
			string text = OnboardingUiText.T("AF_ONBOARDING_YJ_BODY", "【Required before playing】\nOpen the key management and credit pages from “Get an API key and buy credits”; create a valid key and add enough credits.\n\nWithout a valid key or credits, models cannot be fetched, connection tests cannot pass, and AI gameplay is unavailable.\nA single-group key is entered once; multi-group mode requires one key per API.");
			if (_apiOnlySetupFlowActive)
			{
				text += OnboardingUiText.T("AF_ONBOARDING_YJ_RECONFIGURE_SUFFIX", "\n\nTerminal API reconfiguration is active: after all four APIs pass, the game resumes without database import.");
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(OnboardingUiText.T("AF_ONBOARDING_YJ_MENU", "YJ API relay"), text, list, isExitShown: false, 0, 1, OnboardingUiText.T("AF_COMMON_CONFIRM", "Confirm"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(List<InquiryElement> selected)
			{
				_welcomeInProgress = false;
				string text2 = selected?.FirstOrDefault()?.Identifier as string;
				if (text2 == "yj_prepare_account")
				{
					OpenYjApiPreparationPages();
					ShowYjApiSetupMenu();
				}
				else if (text2 == "yj_multi_group")
				{
					BeginYjApiSetup(YjApiSetupMode.MultiGroup);
				}
				else if (text2 == "yj_single_group")
				{
					BeginYjApiSetup(YjApiSetupMode.SingleGroup);
				}
				else if (text2 == "yj_copy_qq_group")
				{
					CopyYjApiPlayerQqGroupNumberToClipboard();
					ShowYjApiSetupMenu();
				}
				else
				{
					ResetYjApiSetup();
					ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				}
			}, delegate
			{
				_welcomeInProgress = false;
				ResetYjApiSetup();
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch (Exception ex)
		{
			_welcomeInProgress = false;
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_YJ_MENU_FAILED", "Failed to open the YJ API relay menu: ") + ex.Message));
			ResetYjApiSetup();
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private static void CopyYjApiPlayerQqGroupNumberToClipboard()
	{
		try
		{
			TaleWorlds.InputSystem.Input.SetClipboardText(YjApiPlayerQqGroupNumber);
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_YJ_QQ_COPIED", "YJ community QQ group copied to clipboard: ") + YjApiPlayerQqGroupNumber, new Color(0.35f, 1f, 0.35f)));
		}
		catch
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_YJ_QQ_COPY_FAILED", "Could not copy the QQ group; copy it manually: ") + YjApiPlayerQqGroupNumber, Colors.Yellow));
		}
	}

	private static void OpenYjApiPreparationPages()
	{
		OpenYjApiExternalPage(YjApiKeysUrl, OnboardingUiText.T("AF_ONBOARDING_YJ_KEY_PAGE", "API key management"));
		OpenYjApiExternalPage(YjApiPurchaseUrl, OnboardingUiText.T("AF_ONBOARDING_YJ_CREDIT_PAGE", "Credit purchase"));
	}

	private static void OpenYjApiExternalPage(string url, string pageName)
	{
		// 无默认浏览器时由共享启动器仅在用户点击时执行一次有界本机查找，不影响游戏热路径。
		if (ExternalBrowserLauncher.TryOpen(url, out bool usedLocalBrowserFallback, out string failureMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(usedLocalBrowserFallback ? OnboardingUiText.T("AF_ONBOARDING_SYSTEM_YJ_FALLBACK", "[System] The default browser could not start; opened the YJ API ") + pageName + OnboardingUiText.T("AF_ONBOARDING_PAGE_SUFFIX", " page.") : OnboardingUiText.T("AF_ONBOARDING_SYSTEM_YJ_OPENING", "[System] Opening the YJ API ") + pageName + OnboardingUiText.T("AF_ONBOARDING_PAGE_SUFFIX", " page.")));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SYSTEM_YJ_FAILED", "[System] Failed to open the YJ API ") + pageName + OnboardingUiText.T("AF_ONBOARDING_PAGE_FAILED_SUFFIX", " page failed: ") + failureMessage));
		}
	}

	private void BeginYjApiSetup(YjApiSetupMode mode)
	{
		if (mode == YjApiSetupMode.None || !IsYjApiSetupAvailable())
		{
			ResetYjApiSetup();
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
		ResetYjApiSetup();
		_yjApiSetupMode = mode;
		SetApiSetupTarget(ApiSetupTarget.Primary);
		SetApiRepairFlowActive(active: false);
		BeginYjApiCurrentTargetSetup();
	}

	private void BeginYjApiCurrentTargetSetup()
	{
		if (!IsYjApiSetupActive())
		{
			ResetYjApiSetup();
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_CONFIG_FAILED", "MCM settings could not be read; cannot configure ") + CurrentApiDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
			ShowYjApiSetupMenu();
			return;
		}
		if (_yjApiSetupMode == YjApiSetupMode.SingleGroup && _yjSingleGroupKeyConfirmed)
		{
			if (!string.IsNullOrWhiteSpace(GetApiKeyForTarget(settings, _currentApiSetupTarget)))
			{
				SetApiUrlForTarget(settings, _currentApiSetupTarget, YjApiBaseUrl);
				TryPersistMcmSettings(settings);
				BeginFetchAvailableModelsForSetup();
				return;
			}
			_yjSingleGroupKeyConfirmed = false;
		}
		OpenYjApiKeyInput();
	}

	private void OpenYjApiKeyInput()
	{
		try
		{
			if (!IsYjApiSetupActive())
			{
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_INPUT_FAILED", "MCM settings could not be read; cannot enter ") + CurrentApiKeyDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
				ShowYjApiSetupMenu();
				return;
			}
			_activeOnboardingStage = OnboardingUiStage.YjApiKey;
			_welcomeInProgress = true;
			bool singleGroup = _yjApiSetupMode == YjApiSetupMode.SingleGroup;
			string title = singleGroup ? OnboardingUiText.T("AF_ONBOARDING_YJ_SINGLE_KEY_TITLE", "Enter YJ API single-group key") : OnboardingUiText.T("AF_ONBOARDING_YJ_KEY_TITLE_PREFIX", "Enter YJ API ") + CurrentApiKeyDisplayName();
			string text = singleGroup
				? OnboardingUiText.T("AF_ONBOARDING_YJ_SINGLE_KEY_BODY", "Enter one YJ API key. The tested Base URL and key will be written to all four APIs; each API will then fetch and select its model.")
				: OnboardingUiText.T("AF_ONBOARDING_YJ_MULTI_KEY_BODY_PREFIX", "Enter the YJ API key for ") + CurrentApiDisplayName() + OnboardingUiText.T("AF_ONBOARDING_YJ_MULTI_KEY_BODY_SUFFIX", " group. Only the current API is written, and models available to this key are fetched.");
			InformationManager.ShowTextInquiry(new TextInquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_COMMON_NEXT", "Next"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_KEY_EMPTY", "API key cannot be empty.")));
					OpenYjApiKeyInput();
					return;
				}
				if (singleGroup)
				{
					SetYjApiBaseUrlForAllTargets(settings);
					SetYjApiKeyForAllTargets(settings, text2);
					_yjSingleGroupKeyConfirmed = true;
				}
				else
				{
					SetApiUrlForTarget(settings, _currentApiSetupTarget, YjApiBaseUrl);
					SetApiKeyForTarget(settings, _currentApiSetupTarget, text2);
				}
				TryPersistMcmSettings(settings);
				_welcomeInProgress = false;
				InformationManager.DisplayMessage(new InformationMessage((singleGroup ? OnboardingUiText.T("AF_ONBOARDING_YJ_SINGLE_KEY_LABEL", "YJ API single-group key") : CurrentApiKeyDisplayName()) + OnboardingUiText.T("AF_ONBOARDING_WRITTEN_FETCHING", " was saved to MCM; fetching models for ") + CurrentApiModelDisplayName() + OnboardingUiText.T("AF_ONBOARDING_AVAILABLE_MODELS_SUFFIX", ".")));
				BeginFetchAvailableModelsForSetup();
			}, delegate
			{
				_welcomeInProgress = false;
				ShowYjApiSetupMenu();
			}));
		}
		catch (Exception ex)
		{
			_welcomeInProgress = false;
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_YJ_INPUT_FAILED", "Failed to open the YJ API key input: ") + ex.Message));
			ShowYjApiSetupMenu();
		}
	}

	private void AdvanceYjApiSetupAfterValidation()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				settings.UseAuxiliaryRuleApi = true;
				TryPersistMcmSettings(settings);
			}
		}
		if (_currentApiSetupTarget == ApiSetupTarget.Primary)
		{
			SetApiSetupTarget(ApiSetupTarget.Auxiliary);
			BeginYjApiCurrentTargetSetup();
			return;
		}
		if (_currentApiSetupTarget == ApiSetupTarget.Auxiliary)
		{
			SetApiSetupTarget(ApiSetupTarget.ActionPostprocess);
			BeginYjApiCurrentTargetSetup();
			return;
		}
		if (_currentApiSetupTarget == ApiSetupTarget.ActionPostprocess)
		{
			SetApiSetupTarget(ApiSetupTarget.EventAndRebellion);
			BeginYjApiCurrentTargetSetup();
			return;
		}
		if (_apiOnlySetupFlowActive)
		{
			CompleteApiSetupOnlyFlow();
			return;
		}
		ResetYjApiSetup();
		ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
	}

	private void ShowDeepSeekApiKeyOwnershipInquiry(QuickApiPreset preset)
	{
		try
		{
			if (preset == QuickApiPreset.None)
			{
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			_selectedQuickApiPreset = preset;
			_quickPresetFlowActive = false;
			_activeOnboardingStage = OnboardingUiStage.DeepSeekApiKeyOwnership;
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_KEY_TITLE", "DeepSeek API key confirmation"), OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_KEY_BODY", "Do you have an official DeepSeek API key?\n\nIf not, choose “No” to open the DeepSeek platform and create a key with credits. Return to the game and choose the preset again."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_COMMON_YES", "Yes"), OnboardingUiText.T("AF_COMMON_NO", "No"), delegate
			{
				_welcomeInProgress = false;
				ApplyDeepSeekPresetAndOpenKeyInput(preset);
			}, delegate
			{
				_welcomeInProgress = false;
				OpenDeepSeekApiKeysPage();
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			}), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			_welcomeInProgress = false;
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_KEY_FAILED", "Failed to open the DeepSeek API key confirmation: ") + ex.Message));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private static void OpenDeepSeekApiKeysPage()
	{
		// 与 YJ 页面复用同一受限浏览器发现逻辑，避免缺少默认关联时中断引导。
		if (ExternalBrowserLauncher.TryOpen(DeepSeekApiKeysUrl, out bool usedLocalBrowserFallback, out string failureMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(usedLocalBrowserFallback ? OnboardingUiText.T("AF_ONBOARDING_SYSTEM_DEEPSEEK_FALLBACK", "[System] The default browser could not start; opened the official DeepSeek API keys page.") : OnboardingUiText.T("AF_ONBOARDING_SYSTEM_DEEPSEEK_OPENING", "[System] Opening the official DeepSeek API keys page.")));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SYSTEM_DEEPSEEK_FAILED", "[System] Failed to open the official DeepSeek page: ") + failureMessage));
		}
	}

	private void ApplyDeepSeekPresetAndOpenKeyInput(QuickApiPreset preset)
	{
		try
		{
			if (!ApplyDeepSeekPresetToMcm(preset, ""))
			{
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			_selectedQuickApiPreset = preset;
			_quickPresetFlowActive = true;
			ShowQuickPresetApiKeyInput(preset);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_APPLY_FAILED", "Failed to apply the DeepSeek preset: ") + ex.Message));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private bool ApplyDeepSeekPresetToMcm(QuickApiPreset preset, string apiKey)
	{
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_MCM_FAILED", "MCM settings could not be read; the DeepSeek preset cannot be written right now.")));
			return false;
		}
		string key = (apiKey ?? "").Trim();
		settings.ApiUrl = DeepSeekApiBaseUrl;
		settings.AuxiliaryApiUrl = DeepSeekApiBaseUrl;
		settings.ActionPostprocessApiUrl = DeepSeekApiBaseUrl;
		settings.EventAndRebellionApiUrl = DeepSeekApiBaseUrl;
		if (!string.IsNullOrWhiteSpace(key))
		{
			settings.ApiKey = key;
			settings.AuxiliaryApiKey = key;
			settings.ActionPostprocessApiKey = key;
			settings.EventAndRebellionApiKey = key;
		}
		settings.UseAuxiliaryRuleApi = true;
		settings.MemoryPreprocessMode = 1;
		if (preset == QuickApiPreset.DeepSeekPro)
		{
			SetModelNameForTarget(settings, ApiSetupTarget.Primary, DeepSeekProModelName);
			SetModelNameForTarget(settings, ApiSetupTarget.Auxiliary, DeepSeekProModelName);
			SetModelNameForTarget(settings, ApiSetupTarget.ActionPostprocess, DeepSeekProModelName);
			SetModelNameForTarget(settings, ApiSetupTarget.EventAndRebellion, DeepSeekProModelName);
			settings.MainApiThinkingEnabled = true;
			settings.SetMainApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
			settings.MainApiTemperature = 1f;
			settings.AuxiliaryApiThinkingEnabled = false;
			settings.SetAuxiliaryApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
			settings.AuxiliaryApiTemperature = 0f;
			settings.ActionPostprocessApiThinkingEnabled = true;
			settings.SetActionPostprocessApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
			settings.ActionPostprocessApiTemperature = 0f;
			settings.EventAndRebellionApiThinkingEnabled = false;
			settings.SetEventAndRebellionApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
			settings.EventAndRebellionApiTemperature = 0.8f;
		}
		else
		{
			SetModelNameForTarget(settings, ApiSetupTarget.Primary, DeepSeekFlashModelName);
			SetModelNameForTarget(settings, ApiSetupTarget.Auxiliary, DeepSeekProModelName);
			SetModelNameForTarget(settings, ApiSetupTarget.ActionPostprocess, DeepSeekFlashModelName);
			SetModelNameForTarget(settings, ApiSetupTarget.EventAndRebellion, DeepSeekFlashModelName);
			settings.MainApiThinkingEnabled = true;
			settings.SetMainApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
			settings.MainApiTemperature = 1f;
			settings.AuxiliaryApiThinkingEnabled = false;
			settings.SetAuxiliaryApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
			settings.AuxiliaryApiTemperature = 0f;
			settings.ActionPostprocessApiThinkingEnabled = true;
			settings.SetActionPostprocessApiReasoningEffortForExternal(DuelSettings.ReasoningEffortMax);
			settings.ActionPostprocessApiTemperature = 0f;
			settings.EventAndRebellionApiThinkingEnabled = false;
			settings.SetEventAndRebellionApiReasoningEffortForExternal(DuelSettings.ReasoningEffortHigh);
			settings.EventAndRebellionApiTemperature = 0.8f;
		}
		TryPersistMcmSettings(settings);
		return true;
	}

	private void ShowQuickPresetApiKeyInput(QuickApiPreset preset)
	{
		try
		{
			if (preset == QuickApiPreset.None)
			{
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			_selectedQuickApiPreset = preset;
			_quickPresetFlowActive = true;
			_activeOnboardingStage = OnboardingUiStage.QuickPresetApiKey;
			string presetName = preset == QuickApiPreset.DeepSeekPro ? "deepseek-pro" : "deepseek-flash";
			string text = OnboardingUiText.T("AF_ONBOARDING_PRESET_WRITTEN_PREFIX", "Saved ") + presetName + OnboardingUiText.T("AF_ONBOARDING_PRESET_KEY_BODY", " preset.\nEnter a DeepSeek API key; it will be written to the primary, preprocessing, postprocessing, weekly report, and rebellion APIs.");
			// 上次失败已由左下角通知报告；输入框正文保持稳定，避免原始响应挤占输入控件。
			InformationManager.ShowTextInquiry(new TextInquiryData(OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_KEY_INPUT", "Enter DeepSeek API key"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_SAVE_TEST_APIS", "Save and test all four APIs"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_KEY_EMPTY", "API key cannot be empty.")));
					ShowQuickPresetApiKeyInput(preset);
					return;
				}
				if (!ApplyDeepSeekPresetToMcm(preset, text2))
				{
					ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
					return;
			}
			SetApiSetupTarget(ApiSetupTarget.Primary);
			SetApiRepairFlowActive(active: false);
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_APPLIED", "DeepSeek preset saved to MCM; testing all four APIs.")));
				BeginValidateQuickPresetApiConfigAndContinue();
			}, delegate
			{
				_quickPresetFlowActive = false;
				_selectedQuickApiPreset = QuickApiPreset.None;
				ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			}));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_DEEPSEEK_INPUT_FAILED", "Failed to open the DeepSeek API key input: ") + ex.Message));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void BeginValidateQuickPresetApiConfigAndContinue()
	{
		BeginValidateApiConfigSetAndContinue(ApiValidationFlow.QuickPresetAll, skipIncompleteEventAndRebellion: false);
	}

	private void BeginValidateExistingApiConfigAndContinue()
	{
		BeginValidateApiConfigSetAndContinue(ApiValidationFlow.ExistingConfigAll, skipIncompleteEventAndRebellion: true);
	}

	private void BeginValidateApiConfigSetAndContinue(ApiValidationFlow flow, bool skipIncompleteEventAndRebellion)
	{
		if (_apiValidationInProgress)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_TEST_MCM_FAILED", "MCM settings could not be read; the API preset cannot be tested right now.")));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
		TryPersistMcmSettings(settings);
		List<ApiValidationTargetInfo> targets = new List<ApiValidationTargetInfo>();
		if (!TryAddApiValidationTarget(settings, ApiSetupTarget.Primary, targets, required: true, out var missingPrimary))
		{
			ShowApiConfigSetValidationPrecheckFailure(missingPrimary);
			return;
		}
		if (!TryAddApiValidationTarget(settings, ApiSetupTarget.Auxiliary, targets, required: true, out var missingAuxiliary))
		{
			ShowApiConfigSetValidationPrecheckFailure(missingAuxiliary);
			return;
		}
		if (!TryAddApiValidationTarget(settings, ApiSetupTarget.ActionPostprocess, targets, required: true, out var missingActionPostprocess))
		{
			ShowApiConfigSetValidationPrecheckFailure(missingActionPostprocess);
			return;
		}
		bool eventSkipped = false;
		if (!TryAddApiValidationTarget(settings, ApiSetupTarget.EventAndRebellion, targets, !skipIncompleteEventAndRebellion, out var missingEventAndRebellion))
		{
			if (skipIncompleteEventAndRebellion)
			{
				eventSkipped = true;
			}
			else
			{
				ShowApiConfigSetValidationPrecheckFailure(missingEventAndRebellion);
				return;
			}
		}
		_apiValidationFlow = flow;
		_apiValidationReturnToModelSelection = false;
		_apiValidationInProgress = true;
		int num = _operationVersions.Begin(OnboardingOperationKind.ApiValidation);
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		_apiValidationCancellation = cancellationTokenSource;
		ShowApiValidationProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string text = "";
			string failureHint = "";
			try
			{
				ApiValidationTargetResult[] array = await Task.WhenAll(targets.Select((ApiValidationTargetInfo target) => ValidateApiTargetAsync(target, cancellationTokenSource.Token)));
				List<ApiValidationTargetResult> failedResults = array.Where((ApiValidationTargetResult x) => x == null || !x.Success).ToList();
				if (failedResults.Count == 0)
				{
					flag = true;
					text = BuildApiConfigSetValidationSuccessMessage(flow, array, eventSkipped);
				}
				else
				{
					failureHint = string.Join("\n", failedResults.Select((ApiValidationTargetResult x) => x?.FailureHint).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct());
					text = string.Join("\n\n", failedResults.Select((ApiValidationTargetResult x) => x?.Message ?? OnboardingUiText.T("AF_ONBOARDING_API_SET_FAILED", "API set connection test failed.")));
				}
			}
			catch (OperationCanceledException)
			{
				failureHint = OnboardingUiText.T("AF_ONBOARDING_API_SET_CANCEL_HINT", "You cancelled this test. Return to the first-use menu to choose another setup method.");
				text = OnboardingUiText.T("AF_ONBOARDING_API_SET_CANCELLED", "API set test cancelled.");
			}
			catch (Exception ex)
			{
				failureHint = OnboardingUiText.T("AF_ONBOARDING_API_SET_ERROR_HINT", "This is usually a network, certificate, or proxy issue, or an incorrect API setting.");
				text = OnboardingUiText.T("AF_ONBOARDING_API_SET_EXCEPTION", "API set test error: ") + ex.Message;
			}
			finally
			{
				if (_operationVersions.IsCurrent(OnboardingOperationKind.ApiValidation, num))
				{
					if (ReferenceEquals(_apiValidationCancellation, cancellationTokenSource))
					{
						_apiValidationCancellation = null;
					}
					_apiValidationInProgress = false;
					_pendingApiValidationVersion = num;
					_pendingApiValidationSuccess = flag;
					_pendingApiValidationMessage = text ?? "";
					_pendingApiValidationFailureHint = failureHint ?? "";
					_pendingApiValidationResult = true;
				}
				cancellationTokenSource?.Dispose();
			}
		});
	}

	private static bool TryAddApiValidationTarget(DuelSettings settings, ApiSetupTarget target, List<ApiValidationTargetInfo> targets, bool required, out string missingMessage)
	{
		missingMessage = "";
		string displayName = GetApiDisplayName(target);
		string apiUrl = GetApiUrlForTarget(settings, target).Trim();
		string apiKey = GetApiKeyForTarget(settings, target).Trim();
		string modelName = GetModelNameForTarget(settings, target).Trim();
		if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelName))
		{
			if (required)
			{
				missingMessage = displayName + OnboardingUiText.T("AF_ONBOARDING_API_INCOMPLETE_SUFFIX", " is incomplete; check the Base URL, API key, and model name.");
				return false;
			}
			return false;
		}
		targets?.Add(new ApiValidationTargetInfo
		{
			Target = target,
			DisplayName = displayName,
			ApiUrl = apiUrl,
			ApiKey = apiKey,
			ModelName = modelName
		});
		return true;
	}

	private void ShowApiConfigSetValidationPrecheckFailure(string message)
	{
		// 组合预检没有原始响应，直接给出简短左下角提示并返回干净的配置菜单。
		NonBlockingErrorReport.Show(OnboardingUiText.T("AF_ONBOARDING_API_INCOMPLETE_TITLE", "Incomplete API configuration"), string.IsNullOrWhiteSpace(message) ? OnboardingUiText.T("AF_ONBOARDING_API_INCOMPLETE_BODY", "The API configuration is incomplete, so the set test cannot start.") : message);
		ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
	}

	internal static async Task<ApiValidationTargetResult> ValidateApiTargetAsync(ApiValidationTargetInfo target, CancellationToken cancellationToken)
	{
		ApiValidationTargetResult result = new ApiValidationTargetResult
		{
			Target = target
		};
		if (target == null)
		{
			result.Message = OnboardingUiText.T("AF_ONBOARDING_API_TARGET_EMPTY", "API set test failed: target configuration is empty.");
			result.FailureHint = OnboardingUiText.T("AF_ONBOARDING_RETURN_SETUP", "Return to the first-use menu and choose another setup method.");
			return result;
		}
		try
		{
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(target.ApiUrl);
			JObject requestPayload = new JObject
			{
				["model"] = target.ModelName,
				["messages"] = new JArray
				{
					new JObject
					{
						["role"] = "user",
						["content"] = OnboardingPromptTexts.ConnectionTest
					}
				},
				["stream"] = false
			};
			ApplyApiValidationRequestControls(requestPayload, target.Target, effectiveApiUrl, target.ModelName);
			ConfiguredChatValidationExchange exchange = await SendOnboardingChatValidationAsync(
				"onboarding_combined_validation_" + target.Target.ToString(),
				effectiveApiUrl,
				target.ModelName,
				target.ApiKey,
				requestPayload,
				cancellationToken);
			if (exchange.Result.Status == LlmResultStatus.Cancelled)
			{
				throw new OperationCanceledException(cancellationToken);
			}
			string responseBody = exchange.ResponseBody;
			if (exchange.IsSuccessStatusCode)
			{
				try
				{
					string assistantReply = LlmApiCompat.ExtractAssistantText(JObject.Parse(responseBody));
					if (string.IsNullOrWhiteSpace(assistantReply))
					{
						result.FailureHint = OnboardingUiText.T("AF_ONBOARDING_RESPONSE_NO_MODEL", "The endpoint returned HTTP success, but the response contained no usable model reply.");
						result.Message = LlmRetryPrompt.BuildFailureDetail(target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_REPLY_PARSE_FAILED", "Reply parsing failed."), "", responseBody);
						return result;
					}
					result.Success = true;
					result.Message = target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_SUCCESS", "Connection test succeeded.");
					return result;
				}
				catch (Exception ex)
				{
					result.FailureHint = OnboardingUiText.T("AF_ONBOARDING_RESPONSE_INVALID", "The endpoint returned HTTP success, but the response was not a parseable chat-completion format.");
					result.Message = LlmRetryPrompt.BuildFailureDetail(target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_REPLY_PARSE_EXCEPTION", "Reply parsing failed: ") + ex.Message, "", responseBody);
					return result;
				}
			}
			if (!exchange.HasStatusCode)
			{
				result.FailureHint = OnboardingUiText.T("AF_ONBOARDING_CONNECTION_ERROR_HINT_PREFIX", "This is usually a network, certificate, or proxy issue, or the ") + target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_ERROR_HINT_SUFFIX", " Base URL is incorrect.");
				result.Message = target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_FAILED", "Connection test failed: ") + (exchange.ErrorMessage ?? OnboardingUiText.T("AF_ONBOARDING_UNKNOWN_ERROR", "Unknown error"));
				return result;
			}
			HttpStatusCode statusCode = (HttpStatusCode)exchange.StatusCode;
			result.FailureHint = BuildApiValidationFailureHint(statusCode, responseBody);
			result.Message = target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_FAILED_NEWLINE", "Connection test failed.\n") + BuildApiValidationFailureMessage(effectiveApiUrl, target.ModelName, statusCode, responseBody);
			return result;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			result.FailureHint = OnboardingUiText.T("AF_ONBOARDING_CONNECTION_ERROR_HINT_PREFIX", "This is usually a network, certificate, or proxy issue, or the ") + target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_ERROR_HINT_SUFFIX", " Base URL is incorrect.");
			result.Message = target.DisplayName + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_EXCEPTION", "Connection test error: ") + ex.Message;
			return result;
		}
	}

	private static string BuildApiConfigSetValidationSuccessMessage(ApiValidationFlow flow, IEnumerable<ApiValidationTargetResult> results, bool eventSkipped)
	{
		string label = flow == ApiValidationFlow.ExistingConfigAll ? OnboardingUiText.T("AF_ONBOARDING_EXISTING_TEST_PASSED", "Existing configuration test passed") : OnboardingUiText.T("AF_ONBOARDING_API_SET_PASSED", "API set test passed");
		string names = string.Join("、", (results ?? Enumerable.Empty<ApiValidationTargetResult>()).Where((ApiValidationTargetResult x) => x?.Target != null).Select((ApiValidationTargetResult x) => x.Target.DisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)));
		if (string.IsNullOrWhiteSpace(names))
		{
			names = OnboardingUiText.T("AF_ONBOARDING_CONFIGURED_APIS", "Configured APIs");
		}
		string text = label + "：" + names + OnboardingUiText.T("AF_COMMON_PERIOD", ".");
		if (eventSkipped)
		{
			text += OnboardingUiText.T("AF_ONBOARDING_EVENTS_SKIPPED", "Weekly report and rebellion APIs are incomplete; that test was skipped.");
		}
		return text;
	}

	private static string GetApiDisplayName(ApiSetupTarget target)
	{
		if (target == ApiSetupTarget.Auxiliary)
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_AUXILIARY", "Preprocessing API");
		}
		if (target == ApiSetupTarget.ActionPostprocess)
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_POSTPROCESS", "Postprocessing API");
		}
		if (target == ApiSetupTarget.EventAndRebellion)
		{
			return OnboardingUiText.T("AF_ONBOARDING_API_EVENTS", "Weekly report and rebellion API");
		}
		return OnboardingUiText.T("AF_ONBOARDING_API_PRIMARY", "Primary API");
	}

	private void ShowApiRepairPopup()
	{
		try
		{
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			ResetYjApiSetup();
			SetApiSetupTarget(ApiSetupTarget.Primary);
			SetApiRepairFlowActive(active: true);
			_activeOnboardingStage = OnboardingUiStage.Welcome;
			_welcomeInProgress = true;
			string text = OnboardingUiText.T("AF_ONBOARDING_WEEKLY_EVENT_FAILED", "Weekly event generation failed. Check the Base URL, API key, model name, or network.");
			// 此处只提供修复动作；详细失败内容已在触发时写入左下角消息和日志。
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_ADJUST_API_TITLE", "Adjust API information"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_ENTER_API", "Enter API information"), OnboardingUiText.T("AF_ONBOARDING_TEST_EXISTING", "Test existing configuration"), delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				BeginValidateMcmApiAndContinue();
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowAuxiliaryApiRepairPopup()
	{
		try
		{
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			ResetYjApiSetup();
			SetApiSetupTarget(ApiSetupTarget.Auxiliary);
			SetApiRepairFlowActive(active: true);
			_activeOnboardingStage = OnboardingUiStage.AuxiliaryChoice;
			_welcomeInProgress = true;
			DuelSettings settings = DuelSettings.GetSettings();
			bool hasExistingConfig = HasCompleteApiConfigForTarget(settings, ApiSetupTarget.Auxiliary);
			string text = hasExistingConfig
				? OnboardingUiText.T("AF_ONBOARDING_AUXILIARY_UNAVAILABLE_EXISTING", "The preprocessing API (rule retrieval/routing) is unavailable. Test the existing MCM configuration or re-enter it. This API is required and has no RAG fallback.")
				: OnboardingUiText.T("AF_ONBOARDING_AUXILIARY_UNAVAILABLE", "The preprocessing API (rule retrieval/routing) is unavailable. Check its Base URL, API key, model, or network. This API is required and has no RAG fallback.");
			// 此窗口保留填写/测试动作，不再混入可能很长的失败响应。
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_AUXILIARY_ADJUST", "Adjust preprocessing API"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_AUXILIARY_ENTER", "Enter preprocessing API"), OnboardingUiText.T("AF_ONBOARDING_TEST_EXISTING", "Test existing configuration"), delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				BeginValidateMcmApiAndContinue();
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowActionPostprocessApiRepairPopup()
	{
		try
		{
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			ResetYjApiSetup();
			SetApiSetupTarget(ApiSetupTarget.ActionPostprocess);
			SetApiRepairFlowActive(active: true);
			_activeOnboardingStage = OnboardingUiStage.PostprocessChoice;
			_welcomeInProgress = true;
			DuelSettings settings = DuelSettings.GetSettings();
			bool hasExistingConfig = HasCompleteApiConfigForTarget(settings, ApiSetupTarget.ActionPostprocess);
			string text = hasExistingConfig
				? OnboardingUiText.T("AF_ONBOARDING_POSTPROCESS_UNAVAILABLE_EXISTING", "The postprocessing API is unavailable. Test the existing MCM configuration or re-enter it.\n\nPostprocessing needs stable decisions; prefer a reasoning model or a more capable model.")
				: OnboardingUiText.T("AF_ONBOARDING_POSTPROCESS_UNAVAILABLE", "The postprocessing API is unavailable. Re-enter it or continue using the primary API.\n\nPostprocessing needs stable decisions; prefer a reasoning model or a more capable model.");
			// 此窗口保留填写/回退动作，不再混入可能很长的失败响应。
			string negativeText = hasExistingConfig ? OnboardingUiText.T("AF_ONBOARDING_TEST_EXISTING", "Test existing configuration") : OnboardingUiText.T("AF_ONBOARDING_FALLBACK_PRIMARY", "Continue with primary API");
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_POSTPROCESS_ADJUST", "Adjust postprocessing API"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_POSTPROCESS_ENTER_SHORT", "Enter postprocessing API"), negativeText, delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				if (hasExistingConfig)
				{
					BeginValidateMcmApiAndContinue();
				}
				else
				{
					SetApiRepairFlowActive(active: false);
				}
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowEventAndRebellionApiRepairPopup()
	{
		try
		{
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			ResetYjApiSetup();
			SetApiSetupTarget(ApiSetupTarget.EventAndRebellion);
			SetApiRepairFlowActive(active: true);
			_activeOnboardingStage = OnboardingUiStage.EventRebellionChoice;
			_welcomeInProgress = true;
			DuelSettings settings = DuelSettings.GetSettings();
			bool hasExistingConfig = HasCompleteApiConfigForTarget(settings, ApiSetupTarget.EventAndRebellion);
			string text = hasExistingConfig
				? OnboardingUiText.T("AF_ONBOARDING_EVENTS_UNAVAILABLE_EXISTING", "The events/rebellion API is unavailable. Test the existing MCM configuration or re-enter it.\n\nIt generates weekly reports and rebellion kingdom names; failed naming has no local-name fallback.")
				: OnboardingUiText.T("AF_ONBOARDING_EVENTS_UNAVAILABLE", "The events/rebellion API is unavailable. Re-enter its Base URL, API key, and model, or check the network.\n\nIt generates weekly reports and rebellion kingdom names; failed naming has no local-name fallback.");
			// 此窗口保留填写/测试动作，不再混入可能很长的失败响应。
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_EVENTS_ADJUST", "Adjust events/rebellion API"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_EVENTS_ENTER", "Enter events/rebellion API"), OnboardingUiText.T("AF_ONBOARDING_TEST_EXISTING", "Test existing configuration"), delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				BeginValidateMcmApiAndContinue();
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowWelcomePopup(bool fromGate, bool ignoreSuppress)
	{
		try
		{
			if ((!_apiOnlySetupFlowActive && _setupDone) || _welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			ResetYjApiSetup();
			SetApiSetupTarget(ApiSetupTarget.Primary);
			SetApiRepairFlowActive(active: false);
			long ticks = DateTime.UtcNow.Ticks;
			if (!ignoreSuppress && _suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(fromGate ? 800 : 200).Ticks;
			_activeOnboardingStage = OnboardingUiStage.Welcome;
			string title = OnboardingUiText.T("AF_ONBOARDING_WELCOME_TITLE", "Welcome to AnimusForge");
			string text = OnboardingUiText.T("AF_ONBOARDING_WELCOME_BODY", "Before playing, confirm the primary API. It generates NPC dialogue; AI conversations are unavailable until it is configured.");
			// 欢迎页只负责提供后续动作，失败报告已在测试完成时显示于左下角。
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_PRIMARY_ENTER", "Enter primary API"), OnboardingUiText.T("AF_ONBOARDING_TEST_EXISTING", "Test existing configuration"), delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				BeginValidateMcmApiAndContinue();
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowAuxiliaryApiSetupPopup(bool ignoreSuppress = false)
	{
		try
		{
			if ((!_apiOnlySetupFlowActive && _setupDone) || _welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			ResetYjApiSetup();
			SetApiSetupTarget(ApiSetupTarget.Auxiliary);
			SetApiRepairFlowActive(active: false);
			long ticks = DateTime.UtcNow.Ticks;
			if (!ignoreSuppress && _suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(250.0).Ticks;
			_activeOnboardingStage = OnboardingUiStage.AuxiliaryChoice;
			_welcomeInProgress = true;
			string title = OnboardingUiText.T("AF_ONBOARDING_AUXILIARY_TITLE", "Configure preprocessing API (required)");
			string text = OnboardingUiText.T("AF_ONBOARDING_AUXILIARY_BODY", "The preprocessing API handles rule retrieval/routing. Rule topics are screened at low cost before primary generation. It is required and has no RAG fallback.");
			// 配置页正文不复用失败详情，保证输入与操作按钮始终可见。
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_AUXILIARY_ENTER_SHORT", "Enter preprocessing API"), OnboardingUiText.T("AF_ONBOARDING_TEST_EXISTING", "Test existing configuration"), delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				BeginValidateMcmApiAndContinue();
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowActionPostprocessApiSetupPopup(bool ignoreSuppress = false, bool allowWhenSetupDone = false)
	{
		try
		{
			if ((!allowWhenSetupDone && !_apiOnlySetupFlowActive && _setupDone) || _welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			ResetYjApiSetup();
			SetApiSetupTarget(ApiSetupTarget.ActionPostprocess);
			SetApiRepairFlowActive(active: false);
			long ticks = DateTime.UtcNow.Ticks;
			if (!ignoreSuppress && _suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(250.0).Ticks;
			_activeOnboardingStage = OnboardingUiStage.PostprocessChoice;
			_welcomeInProgress = true;
			DuelSettings settings = DuelSettings.GetSettings();
			bool hasExistingConfig = HasCompleteApiConfigForTarget(settings, ApiSetupTarget.ActionPostprocess);
			string title = OnboardingUiText.T("AF_ONBOARDING_POSTPROCESS_TITLE", "Configure postprocessing API");
			string text = OnboardingUiText.T("AF_ONBOARDING_POSTPROCESS_BODY", "The postprocessing API handles action and emotion tags. It separates postprocessing from preprocessing and primary generation; you can continue using the primary API if needed.\n\nPostprocessing needs stable decisions; prefer a reasoning model or a more capable model.");
			// 配置页正文不复用失败详情，保证输入与回退操作始终可见。
			string negativeText = hasExistingConfig ? OnboardingUiText.T("AF_ONBOARDING_TEST_EXISTING", "Test existing configuration") : OnboardingUiText.T("AF_ONBOARDING_FALLBACK_PRIMARY", "Continue with primary API");
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_POSTPROCESS_ENTER_SHORT", "Enter postprocessing API"), negativeText, delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				if (hasExistingConfig)
				{
					BeginValidateMcmApiAndContinue();
				}
				else
				{
					if (_apiOnlySetupFlowActive)
					{
						CompleteApiSetupOnlyFlow();
					}
					else if (!_setupDone)
					{
						ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
					}
				}
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void OpenApiBaseUrlInput()
	{
		if (IsYjApiSetupActive())
		{
			OpenYjApiKeyInput();
			return;
		}
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_INPUT_FAILED", "MCM settings could not be read; cannot enter ") + CurrentApiBaseUrlDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData(OnboardingUiText.T("AF_ONBOARDING_BASE_URL_TITLE", "Enter Base URL"), OnboardingUiText.T("AF_ONBOARDING_ENTER_PREFIX", "Enter ") + CurrentApiBaseUrlDisplayName() + OnboardingUiText.T("AF_ONBOARDING_EXAMPLE_SUFFIX", ".\nExample: ") + CurrentApiBaseUrlExample(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_COMMON_NEXT", "Next"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiBaseUrlDisplayName() + OnboardingUiText.T("AF_ONBOARDING_EMPTY_SUFFIX", " cannot be empty.")));
					OpenApiBaseUrlInput();
				}
				else
				{
					BeginValidateBaseUrlAndContinue(text2);
				}
			}, delegate
			{
				ReopenCurrentApiEntry(ignoreSuppress: true);
			}));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_OPEN_PREFIX", "Failed to open ") + CurrentApiBaseUrlDisplayName() + OnboardingUiText.T("AF_ONBOARDING_INPUT_FAILED_SUFFIX", " input: ") + ex.Message));
			ReopenCurrentApiEntry(ignoreSuppress: true);
		}
	}

	private void BeginValidateBaseUrlAndContinueCore(string validatedBaseUrl)
	{
		if (_baseUrlValidationInProgress)
		{
			return;
		}
		_baseUrlValidationInProgress = true;
		int num = _operationVersions.Begin(OnboardingOperationKind.BaseUrlValidation);
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		_baseUrlValidationCancellation = cancellationTokenSource;
		ShowBaseUrlValidationProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string message = "";
			try
			{
				ModelCatalogExchange exchange = await new LegacyModelCatalogGateway().ProbeBaseUrlAsync(validatedBaseUrl, cancellationTokenSource.Token);
				string text2 = exchange.ResponseBody;
				if (exchange.Cancelled)
				{
					throw new OperationCanceledException(cancellationTokenSource.Token);
				}
				if (exchange.HasStatusCode && CanUseBaseUrlStatusCode((HttpStatusCode)exchange.StatusCode))
				{
					flag = true;
					message = OnboardingUiText.T("AF_ONBOARDING_BASE_URL_PASSED", "Base URL check passed; enter the API key next.");
				}
				else if (exchange.HasStatusCode)
				{
					message = BuildBaseUrlValidationFailureMessage((HttpStatusCode)exchange.StatusCode, text2);
				}
				else
				{
					message = OnboardingUiText.T("AF_ONBOARDING_BASE_URL_FAILED", "Base URL check failed: ") + ModelCatalogErrorFormatter.Format(exchange.ErrorCode, exchange.ErrorArguments, legacyMessage: exchange.ErrorMessage ?? OnboardingUiText.T("AF_ONBOARDING_UNKNOWN_ERROR", "Unknown error"));
				}
			}
			catch (OperationCanceledException)
			{
				message = OnboardingUiText.T("AF_ONBOARDING_BASE_URL_CANCELLED", "Base URL check cancelled.");
			}
			catch (Exception ex)
			{
				message = OnboardingUiText.T("AF_ONBOARDING_BASE_URL_FAILED", "Base URL check failed: ") + ex.Message;
			}
			finally
			{
				if (_operationVersions.IsCurrent(OnboardingOperationKind.BaseUrlValidation, num))
				{
					if (ReferenceEquals(_baseUrlValidationCancellation, cancellationTokenSource))
					{
						_baseUrlValidationCancellation = null;
					}
					_baseUrlValidationInProgress = false;
					_pendingBaseUrlValidationVersion = num;
					_pendingBaseUrlValidationSuccess = flag;
					_pendingBaseUrlValidationMessage = message ?? "";
					_pendingValidatedBaseUrl = flag ? validatedBaseUrl : "";
					_pendingBaseUrlValidationResult = true;
				}
				cancellationTokenSource?.Dispose();
			}
		});
	}

	private void BeginValidateBaseUrlAndContinue(string rawBaseUrl)
	{
		string text = (rawBaseUrl ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_BASE_URL_EMPTY", "Base URL cannot be empty.")));
			OpenApiBaseUrlInput();
			return;
		}
		if (!Uri.TryCreate(text, UriKind.Absolute, out Uri uriResult) || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_BASE_URL_INVALID", "Base URL is invalid; enter a complete http/https address.")));
			OpenApiBaseUrlInput();
			return;
		}
		if (ShouldPromptContextExtractionApiWarning(text) && ShowContextExtractionApiWarningInquiry(delegate
		{
			BeginValidateBaseUrlAndContinueCore(text);
		}, delegate
		{
			OpenApiBaseUrlInput();
		}))
		{
			return;
		}
		BeginValidateBaseUrlAndContinueCore(text);
	}

	private void ShowBaseUrlValidationProgressPopup()
	{
		try
		{
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.BaseUrlValidation;
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_BASE_URL_CHECKING_TITLE", "Checking Base URL"), OnboardingUiText.T("AF_ONBOARDING_BASE_URL_CHECKING_BODY", "Checking whether the Base URL is available...\n\nYou can enter the API key after the check passes."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_EXIT_SAVE", "Exit current game"), OnboardingUiText.T("AF_ONBOARDING_BACK_PREVIOUS", "Back to previous screen"), ExitCurrentGameFromOnboarding, CancelBaseUrlValidationAndReturn), pauseGameActiveState: true);
		}
		catch
		{
		}
	}

	private void ShowBaseUrlValidationFailurePopup()
	{
		try
		{
			string text = string.IsNullOrWhiteSpace(_lastBaseUrlValidationFailureMessage) ? OnboardingUiText.T("AF_ONBOARDING_BASE_URL_UNAVAILABLE", "The Base URL is unavailable. Check it again and retry.") : _lastBaseUrlValidationFailureMessage;
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.BaseUrlValidationFailure;
			_lastBaseUrlValidationFailureMessage = "";
			// 诊断详情改为左下角消息；仅保留紧凑动作窗，以免丢失原有“退出当前存档”出口。
			NonBlockingErrorReport.Show(OnboardingUiText.T("AF_ONBOARDING_BASE_URL_FAILED_LOWER", "Base URL check failed"), text);
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_BASE_URL_NEXT_TITLE", "What should I do next?"), OnboardingUiText.T("AF_ONBOARDING_BASE_URL_NEXT_BODY", "The Base URL is unavailable. Re-enter it or exit the current game."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_BASE_URL_REENTER", "Re-enter Base URL"), OnboardingUiText.T("AF_ONBOARDING_EXIT_SAVE", "Exit current game"), delegate
			{
				_welcomeInProgress = false;
				_activeOnboardingStage = OnboardingUiStage.None;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				_activeOnboardingStage = OnboardingUiStage.None;
				ExitCurrentGameFromOnboarding();
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void CancelBaseUrlValidationAndReturn()
	{
		CancelBaseUrlValidationCore();
		OpenApiBaseUrlInput();
	}

	private void CancelBaseUrlValidationCore()
	{
		try
		{
			_operationVersions.Cancel(OnboardingOperationKind.BaseUrlValidation);
			_baseUrlValidationInProgress = false;
			_pendingBaseUrlValidationResult = false;
			_pendingBaseUrlValidationVersion = 0;
			_welcomeInProgress = false;
			_activeOnboardingStage = OnboardingUiStage.None;
			try
			{
				_baseUrlValidationCancellation?.Cancel();
			}
			catch
			{
			}
			_baseUrlValidationCancellation = null;
			InformationManager.HideInquiry();
		}
		catch
		{
		}
	}

	private void OpenApiKeyInput()
	{
		if (IsYjApiSetupActive())
		{
			OpenYjApiKeyInput();
			return;
		}
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_INPUT_FAILED", "MCM settings could not be read; cannot enter ") + CurrentApiKeyDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData(OnboardingUiText.T("AF_ONBOARDING_API_KEY_TITLE", "Enter API key"), OnboardingUiText.T("AF_ONBOARDING_ENTER_PREFIX", "Enter ") + CurrentApiKeyDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", "."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_COMMON_NEXT", "Next"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiKeyDisplayName() + OnboardingUiText.T("AF_ONBOARDING_EMPTY_SUFFIX", " cannot be empty.")));
					OpenApiKeyInput();
				}
				else
				{
					SetApiKeyForTarget(settings, _currentApiSetupTarget, text2);
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiKeyDisplayName() + OnboardingUiText.T("AF_ONBOARDING_MCM_WRITTEN_SUFFIX", " was saved to MCM.")));
					BeginFetchAvailableModelsForSetup();
				}
			}, delegate
			{
				OpenApiBaseUrlInput();
			}));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_OPEN_PREFIX", "Failed to open ") + CurrentApiKeyDisplayName() + OnboardingUiText.T("AF_ONBOARDING_INPUT_FAILED_SUFFIX", " input: ") + ex.Message));
			ReopenCurrentApiEntry(ignoreSuppress: true);
		}
	}

	private void BeginFetchAvailableModelsForSetup()
	{
		if (_modelFetchInProgress)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MODEL_FETCH_MCM_FAILED", "MCM settings could not be read; models cannot be fetched right now.")));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		string apiUrl = GetApiUrlForTarget(settings, _currentApiSetupTarget).Trim();
		string apiKey = GetApiKeyForTarget(settings, _currentApiSetupTarget).Trim();
		if (string.IsNullOrWhiteSpace(apiUrl))
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_NEED_BASE_URL", "Enter the Base URL first.")));
			OpenApiBaseUrlInput();
			return;
		}
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_NEED_API_KEY", "Enter the API key first.")));
			OpenApiKeyInput();
			return;
		}
		_modelFetchInProgress = true;
		int num = _operationVersions.Begin(OnboardingOperationKind.ModelFetch);
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		_modelFetchCancellation = cancellationTokenSource;
		ShowModelFetchProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string text = "";
			List<string> list = new List<string>();
			try
			{
				ModelCatalogExchange exchange = await new LegacyModelCatalogGateway().FetchModelsAsync(apiUrl, apiKey, cancellationTokenSource.Token);
				string text2 = exchange.ResponseBody;
				if (exchange.Cancelled)
				{
					throw new OperationCanceledException(cancellationTokenSource.Token);
				}
				if (exchange.IsSuccessStatusCode)
				{
					list = ExtractModelNamesFromResponse(text2);
					if (list.Count > 0)
					{
						flag = true;
						text = OnboardingUiText.T("AF_ONBOARDING_MODELS_FETCHED", "Available models fetched; choose a model.");
					}
					else
					{
						text = LlmRetryPrompt.BuildFailureDetail(OnboardingUiText.T("AF_ONBOARDING_MODELS_UNRECOGNIZED", "The endpoint returned a response, but no usable model list was recognized. You can enter a model manually."), "", text2);
					}
				}
				else if (exchange.HasStatusCode)
				{
					text = BuildModelFetchFailureMessage((HttpStatusCode)exchange.StatusCode, text2);
				}
				else
				{
					text = OnboardingUiText.T("AF_ONBOARDING_MODELS_FAILED", "Failed to fetch models: ") + ModelCatalogErrorFormatter.Format(exchange.ErrorCode, exchange.ErrorArguments, legacyMessage: exchange.ErrorMessage ?? OnboardingUiText.T("AF_ONBOARDING_UNKNOWN_ERROR", "Unknown error"));
				}
			}
			catch (OperationCanceledException)
			{
				text = OnboardingUiText.T("AF_ONBOARDING_MODELS_CANCELLED", "Model list fetch cancelled.");
			}
			catch (Exception ex)
			{
				text = OnboardingUiText.T("AF_ONBOARDING_MODELS_FAILED", "Failed to fetch models: ") + ex.Message;
			}
			finally
			{
				if (_operationVersions.IsCurrent(OnboardingOperationKind.ModelFetch, num))
				{
					if (ReferenceEquals(_modelFetchCancellation, cancellationTokenSource))
					{
						_modelFetchCancellation = null;
					}
					_modelFetchInProgress = false;
					_pendingModelFetchVersion = num;
					_pendingModelFetchSuccess = flag;
					_pendingModelFetchMessage = text ?? "";
					_pendingModelFetchModels = list ?? new List<string>();
					_pendingModelFetchResult = true;
				}
				cancellationTokenSource?.Dispose();
			}
		});
	}

	private void ShowModelFetchProgressPopup()
	{
		try
		{
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.ModelFetch;
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_MODELS_FETCHING_TITLE", "Fetching model list"), OnboardingUiText.T("AF_ONBOARDING_MODELS_FETCHING_BODY", "Fetching models available from this endpoint...\n\nThe next step opens automatically when complete.\nIf fetching keeps failing, go back to re-enter the settings or enter a model manually later."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_EXIT_SAVE", "Exit current game"), OnboardingUiText.T("AF_ONBOARDING_BACK_PREVIOUS", "Back to previous screen"), ExitCurrentGameFromOnboarding, CancelModelFetchAndReturnToApiKey), pauseGameActiveState: true);
		}
		catch
		{
		}
	}

	private void CancelModelFetchAndReturnToApiKey()
	{
		CancelModelFetchCore();
		OpenApiKeyInput();
	}

	private void CancelModelFetchCore()
	{
		try
		{
			_operationVersions.Cancel(OnboardingOperationKind.ModelFetch);
			_modelFetchInProgress = false;
			_pendingModelFetchResult = false;
			_pendingModelFetchVersion = 0;
			_pendingModelFetchSuccess = false;
			_pendingModelFetchMessage = "";
			_pendingModelFetchModels = new List<string>();
			_welcomeInProgress = false;
			_activeOnboardingStage = OnboardingUiStage.None;
			try
			{
				_modelFetchCancellation?.Cancel();
			}
			catch
			{
			}
			_modelFetchCancellation = null;
			InformationManager.HideInquiry();
		}
		catch
		{
		}
	}

	private void ShowModelSelectionPopup()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MODEL_INPUT_MCM_FAILED", "MCM settings could not be read; cannot enter ") + CurrentApiModelDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.ModelSelect;
			bool yjApiSetupActive = IsYjApiSetupActive();
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("__manual__", OnboardingUiText.T("AF_ONBOARDING_MODEL_MANUAL_TITLE", "Enter model name manually"), null));
			if (!yjApiSetupActive)
			{
				list.Add(new InquiryElement("__base_url__", OnboardingUiText.T("AF_ONBOARDING_BASE_URL_REENTER", "Re-enter Base URL"), null));
			}
			list.Add(new InquiryElement("__api_key__", OnboardingUiText.T("AF_ONBOARDING_MODEL_REENTER_KEY", "Re-enter API key"), null));
			foreach (string item in _lastFetchedModelNames.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				list.Add(new InquiryElement(item, item, null));
			}
			list.Add(new InquiryElement("__refresh__", OnboardingUiText.T("AF_ONBOARDING_MODEL_REFRESH", "Fetch model list again"), null));
			list.Add(new InquiryElement("__exit__", OnboardingUiText.T("AF_ONBOARDING_EXIT_SAVE", "Exit current game"), null));
			string text = OnboardingUiText.T("AF_ONBOARDING_MODEL_SELECT_BODY", "Choose an available model.");
			if (_lastFetchedModelNames.Count > 0)
			{
				text += "";
			}
			else
			{
				text += OnboardingUiText.T("AF_ONBOARDING_MODEL_NO_LIST", "\n\nNo model list was fetched; you can enter a model manually.");
			}
			// 网络失败详情已显示在左下角；模型选择窗口保持紧凑，只保留当前可执行的恢复操作。
			if (yjApiSetupActive)
			{
				text = text + OnboardingUiText.T("AF_ONBOARDING_MODEL_RECOMMEND_PREFIX", "\n\n[Channel model suggestion]\n") + CurrentYjApiModelRecommendation() + OnboardingUiText.T("AF_ONBOARDING_MODEL_RECOMMEND_SUFFIX", "\nThe final choice is yours.");
			}
			else
			{
				text += OnboardingUiText.T("AF_ONBOARDING_MODEL_RECOVERY_HINT", "\n\nIf the Base URL or API key is wrong, scroll to the bottom to return and enter it again.");
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(OnboardingUiText.T("AF_ONBOARDING_MODEL_SELECT_TITLE", "Choose model name"), text, list, isExitShown: false, 0, 1, OnboardingUiText.T("AF_COMMON_NEXT", "Next"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(List<InquiryElement> selected)
			{
				_welcomeInProgress = false;
				string text2 = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrWhiteSpace(text2))
				{
					ShowModelSelectionPopup();
				}
				else if (text2 == "__manual__")
				{
					OpenManualModelNameInput();
				}
				else if (text2 == "__refresh__")
				{
					BeginFetchAvailableModelsForSetup();
				}
				else if (text2 == "__base_url__")
				{
					if (IsYjApiSetupActive())
					{
						OpenYjApiKeyInput();
					}
					else
					{
						OpenApiBaseUrlInput();
					}
				}
				else if (text2 == "__api_key__")
				{
					OpenApiKeyInput();
				}
				else if (text2 == "__exit__")
				{
					ExitCurrentGameFromOnboarding();
				}
				else
				{
					SetModelNameForTarget(settings, _currentApiSetupTarget, text2);
					if (IsYjApiSetupActive())
					{
						ApplyYjGeminiPresetThinkingDefaults(settings, _currentApiSetupTarget, text2);
					}
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiModelDisplayName() + OnboardingUiText.T("AF_ONBOARDING_MODEL_TESTING_PREFIX", " was saved to MCM; testing the complete connection: ") + text2));
					BeginValidateMcmApiAndContinue(returnToModelSelection: true);
				}
			}, delegate
			{
				_welcomeInProgress = false;
				OpenApiKeyInput();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MODEL_SELECT_FAILED", "Failed to open model selection: ") + ex.Message));
			ReopenCurrentApiEntry(ignoreSuppress: true);
		}
	}

	private void OpenManualModelNameInput()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MODEL_INPUT_MCM_FAILED", "MCM settings could not be read; cannot enter ") + CurrentApiModelDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData(OnboardingUiText.T("AF_ONBOARDING_MODEL_MANUAL_INPUT", "Enter model name"), OnboardingUiText.T("AF_ONBOARDING_ENTER_MODEL_PREFIX", "Enter") + CurrentApiModelDisplayName() + OnboardingUiText.T("AF_ONBOARDING_MODEL_EXAMPLE", ".\nExample: gpt-4o-mini"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_START_TEST", "Start test"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiModelDisplayName() + OnboardingUiText.T("AF_ONBOARDING_EMPTY_SUFFIX", " cannot be empty.")));
					OpenManualModelNameInput();
				}
				else
				{
					SetModelNameForTarget(settings, _currentApiSetupTarget, text2);
					if (IsYjApiSetupActive())
					{
						ApplyYjGeminiPresetThinkingDefaults(settings, _currentApiSetupTarget, text2);
					}
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiModelDisplayName() + OnboardingUiText.T("AF_ONBOARDING_MODEL_TESTING_PREFIX", " was saved to MCM; testing the complete connection: ") + text2));
					BeginValidateMcmApiAndContinue(returnToModelSelection: true);
				}
			}, delegate
			{
				ShowModelSelectionPopup();
			}));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MODEL_INPUT_FAILED", "Failed to open model name input: ") + ex.Message));
			ReopenCurrentApiEntry(ignoreSuppress: true);
		}
	}

	private static void ApplyApiValidationRequestControls(JObject payload, ApiSetupTarget target, string apiUrl, string modelName)
	{
		if (payload == null)
		{
			return;
		}
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return;
			}
			bool thinkingEnabled;
			string effort;
			float temperature;
			if (target == ApiSetupTarget.Auxiliary)
			{
				thinkingEnabled = settings.AuxiliaryApiThinkingEnabled;
				effort = settings.GetAuxiliaryApiReasoningEffort();
				temperature = settings.GetAuxiliaryApiTemperature();
			}
			else if (target == ApiSetupTarget.ActionPostprocess)
			{
				thinkingEnabled = settings.ActionPostprocessApiThinkingEnabled;
				effort = settings.GetActionPostprocessApiReasoningEffort();
				temperature = settings.GetActionPostprocessApiTemperature();
			}
			else if (target == ApiSetupTarget.EventAndRebellion)
			{
				thinkingEnabled = settings.EventAndRebellionApiThinkingEnabled;
				effort = settings.GetEventAndRebellionApiReasoningEffort();
				temperature = settings.GetEventAndRebellionApiTemperature();
			}
			else
			{
				thinkingEnabled = settings.MainApiThinkingEnabled;
				effort = settings.GetMainApiReasoningEffort();
				temperature = settings.GetMainApiTemperature();
			}
			payload["temperature"] = DuelSettings.ClampApiTemperature(temperature);
			DuelSettings.ApplyThinkingControls(payload, apiUrl, modelName, thinkingEnabled, effort, out var _);
		}
		catch
		{
		}
	}

	private void BeginValidateMcmApiAndContinueCore(string apiUrl, string apiKey, string modelName, bool returnToModelSelection)
	{
		_apiValidationReturnToModelSelection = returnToModelSelection;
		_apiValidationInProgress = true;
		ApiSetupTarget validationTarget = _currentApiSetupTarget;
		int num = _operationVersions.Begin(OnboardingOperationKind.ApiValidation);
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		_apiValidationCancellation = cancellationTokenSource;
		ShowApiValidationProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string text = "";
			string failureHint = "";
			try
			{
				string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(apiUrl);
				JObject requestPayload = new JObject
				{
					["model"] = modelName,
					["messages"] = new JArray
					{
						new JObject
						{
							["role"] = "user",
							["content"] = OnboardingPromptTexts.ConnectionTest
						}
					},
					["stream"] = false
				};
				ApplyApiValidationRequestControls(requestPayload, validationTarget, effectiveApiUrl, modelName);
				ConfiguredChatValidationExchange exchange = await SendOnboardingChatValidationAsync(
					"onboarding_mcm_validation_" + validationTarget.ToString(),
					effectiveApiUrl,
					modelName,
					apiKey,
					requestPayload,
					cancellationTokenSource.Token);
				if (exchange.Result.Status == LlmResultStatus.Cancelled)
				{
					throw new OperationCanceledException(cancellationTokenSource.Token);
				}
				string text2 = exchange.ResponseBody;
				if (exchange.IsSuccessStatusCode)
				{
					try
					{
						JObject jObject = JObject.Parse(text2);
						string text3 = LlmApiCompat.ExtractAssistantText(jObject);
						if (string.IsNullOrWhiteSpace(text3))
						{
							failureHint = OnboardingUiText.T("AF_ONBOARDING_RESPONSE_NO_MODEL", "The endpoint returned HTTP success, but the response contained no usable model reply.");
							text = LlmRetryPrompt.BuildFailureDetail(OnboardingUiText.T("AF_ONBOARDING_MCM_OF", "MCM ") + CurrentApiDisplayName() + OnboardingUiText.T("AF_ONBOARDING_REPLY_PARSE_FAILED", "Reply parsing failed."), "", text2);
						}
						else
						{
							flag = true;
							text = OnboardingUiText.T("AF_ONBOARDING_MCM_OF", "MCM ") + CurrentApiDisplayName() + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_SUCCESS_PREFIX", "connection test succeeded: ") + text3.Trim();
						}
					}
					catch (Exception ex)
					{
						failureHint = OnboardingUiText.T("AF_ONBOARDING_RESPONSE_INVALID", "The endpoint returned HTTP success, but the response was not a parseable chat-completion format.");
						text = LlmRetryPrompt.BuildFailureDetail(OnboardingUiText.T("AF_ONBOARDING_MCM_OF", "MCM ") + CurrentApiDisplayName() + OnboardingUiText.T("AF_ONBOARDING_REPLY_PARSE_EXCEPTION", "Reply parsing failed: ") + ex.Message, "", text2);
					}
				}
				else if (exchange.HasStatusCode)
				{
					HttpStatusCode statusCode = (HttpStatusCode)exchange.StatusCode;
					failureHint = BuildApiValidationFailureHint(statusCode, text2);
					text = BuildApiValidationFailureMessage(effectiveApiUrl, modelName, statusCode, text2);
				}
				else
				{
					failureHint = OnboardingUiText.T("AF_ONBOARDING_CONNECTION_HINT", "This is usually a network, certificate, or proxy issue, or an incorrect Base URL.");
					text = OnboardingUiText.T("AF_ONBOARDING_MCM_OF", "MCM ") + CurrentApiDisplayName() + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_FAILED", "Connection test failed: ") + (exchange.ErrorMessage ?? OnboardingUiText.T("AF_ONBOARDING_UNKNOWN_ERROR", "Unknown error"));
				}
			}
			catch (OperationCanceledException)
			{
				failureHint = OnboardingUiText.T("AF_ONBOARDING_TEST_CANCEL_HINT", "You cancelled this test. Return to retry or change the API settings.");
				text = OnboardingUiText.T("AF_ONBOARDING_TEST_CANCELLED", "Test cancelled; returned to the previous screen.");
			}
			catch (Exception ex)
			{
				failureHint = OnboardingUiText.T("AF_ONBOARDING_CONNECTION_HINT", "This is usually a network, certificate, or proxy issue, or an incorrect Base URL.");
				text = CurrentApiDisplayName() + OnboardingUiText.T("AF_ONBOARDING_CONNECTION_EXCEPTION", "Connection test error: ") + ex.Message;
			}
			finally
			{
				if (_operationVersions.IsCurrent(OnboardingOperationKind.ApiValidation, num))
				{
					if (ReferenceEquals(_apiValidationCancellation, cancellationTokenSource))
					{
						_apiValidationCancellation = null;
					}
					_apiValidationInProgress = false;
					_pendingApiValidationVersion = num;
					_pendingApiValidationSuccess = flag;
					_pendingApiValidationMessage = text ?? "";
					_pendingApiValidationFailureHint = failureHint ?? "";
					_pendingApiValidationResult = true;
				}
				cancellationTokenSource?.Dispose();
			}
		});
	}

	private void BeginValidateMcmApiAndContinue(bool returnToModelSelection = false)
	{
		if (_apiValidationInProgress)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_READ_FAILED", "Could not read MCM settings.")));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		TryPersistMcmSettings(settings);
		string apiUrl = GetApiUrlForTarget(settings, _currentApiSetupTarget).Trim();
		string apiKey = GetApiKeyForTarget(settings, _currentApiSetupTarget).Trim();
		string modelName = GetModelNameForTarget(settings, _currentApiSetupTarget).Trim();
		if (string.IsNullOrWhiteSpace(apiUrl))
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_MISSING_PREFIX", "MCM is missing ") + CurrentApiBaseUrlDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_MISSING_PREFIX", "MCM is missing ") + CurrentApiKeyDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		if (string.IsNullOrWhiteSpace(modelName))
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_MCM_MISSING_PREFIX_NO_SPACE", "MCM is missing ") + CurrentApiModelDisplayName() + OnboardingUiText.T("AF_COMMON_PERIOD", ".")));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		BeginValidateMcmApiAndContinueCore(apiUrl, apiKey, modelName, returnToModelSelection);
	}

	private void ShowApiValidationProgressPopup()
	{
		try
		{
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.ApiValidation;
			string text = (_apiValidationFlow == ApiValidationFlow.QuickPresetAll || _apiValidationFlow == ApiValidationFlow.ExistingConfigAll)
				? OnboardingUiText.T("AF_ONBOARDING_SET_TESTING_BODY", "Testing the API set in parallel using the current MCM settings...\n\nThe next step opens when complete; any required API failure returns to the first-use menu.")
				: OnboardingUiText.T("AF_ONBOARDING_SINGLE_TESTING_PREFIX", "Testing ") + CurrentApiDisplayName() + OnboardingUiText.T("AF_ONBOARDING_SINGLE_TESTING_SUFFIX", " using MCM settings...\n\nThe next step opens when complete.\nYou can exit the current game here if the test keeps failing.");
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_TESTING_TITLE", "Testing existing configuration"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_EXIT_SAVE", "Exit current game"), OnboardingUiText.T("AF_ONBOARDING_BACK_PREVIOUS", "Back to previous screen"), ExitCurrentGameFromOnboarding, CancelApiValidationAndReturnToWelcome), pauseGameActiveState: true);
		}
		catch
		{
		}
	}

	private void CancelApiValidationAndReturnToWelcome()
	{
		bool returnToSetupMenu = _apiValidationFlow == ApiValidationFlow.QuickPresetAll || _apiValidationFlow == ApiValidationFlow.ExistingConfigAll;
		bool returnToModelSelection = _apiValidationReturnToModelSelection;
		CancelApiValidationCore();
		if (returnToSetupMenu)
		{
			_quickPresetFlowActive = false;
			_selectedQuickApiPreset = QuickApiPreset.None;
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
		else if (returnToModelSelection)
		{
			ShowModelSelectionPopup();
		}
		else
		{
			ReopenCurrentApiEntry(ignoreSuppress: true);
		}
	}

	private void CancelApiValidationCore()
	{
		try
		{
			_operationVersions.Cancel(OnboardingOperationKind.ApiValidation);
			_apiValidationInProgress = false;
			_apiValidationFlow = ApiValidationFlow.Normal;
			_apiValidationReturnToModelSelection = false;
			_pendingApiValidationResult = false;
			_pendingApiValidationVersion = 0;
			_pendingApiValidationSuccess = false;
			_pendingApiValidationMessage = "";
			_pendingApiValidationFailureHint = "";
			_welcomeInProgress = false;
			_activeOnboardingStage = OnboardingUiStage.None;
			try
			{
				_apiValidationCancellation?.Cancel();
			}
			catch
			{
			}
			_apiValidationCancellation = null;
			InformationManager.HideInquiry();
		}
		catch
		{
		}
	}

	private void ExitCurrentGameFromOnboarding()
	{
		try
		{
			ResetYjApiSetup();
			_saveAndExitStage = SaveAndExitStage.None;
			_onboardingSession.CancelWelcome();
			_pendingReturnToWelcome = false;
			_pendingApiValidationResult = false;
			_pendingApiValidationVersion = 0;
			_pendingApiValidationSuccess = false;
			_pendingApiValidationMessage = "";
			_pendingApiValidationFailureHint = "";
			_apiValidationFlow = ApiValidationFlow.Normal;
			_apiValidationReturnToModelSelection = false;
			_pendingBaseUrlValidationResult = false;
			_pendingBaseUrlValidationVersion = 0;
			_pendingBaseUrlValidationSuccess = false;
			_pendingBaseUrlValidationMessage = "";
			_pendingValidatedBaseUrl = "";
			_lastBaseUrlValidationFailureMessage = "";
			_pendingModelFetchResult = false;
			_pendingModelFetchVersion = 0;
			_pendingModelFetchSuccess = false;
			_pendingModelFetchMessage = "";
			_pendingModelFetchModels = new List<string>();
			_dismissalOwner.Reset();
			_welcomeInProgress = false;
			_apiRepairFlowActive = false;
			_currentApiSetupTarget = ApiSetupTarget.Primary;
			if (_baseUrlValidationInProgress)
			{
				CancelBaseUrlValidationCore();
			}
			if (_apiValidationInProgress)
			{
				CancelApiValidationCore();
			}
			if (_modelFetchInProgress)
			{
				CancelModelFetchCore();
			}
			else
			{
				_activeOnboardingStage = OnboardingUiStage.None;
				InformationManager.HideInquiry();
			}
			MBGameManager.EndGame();
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_EXIT_FAILED", "Failed to exit the current game: ") + ex.Message));
			ReopenCurrentApiEntry(ignoreSuppress: true);
		}
	}

	private void BeginSaveAndExitCurrentGameFromOnboarding()
	{
		try
		{
			_welcomeInProgress = false;
			_activeOnboardingStage = OnboardingUiStage.None;
			_dismissalOwner.Reset();
			InformationManager.HideInquiry();
			SaveHandler saveHandler = Campaign.Current?.SaveHandler;
			if (saveHandler == null)
			{
				_saveAndExitStage = SaveAndExitStage.None;
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVE_HANDLER_MISSING", "No save handler was found; exiting the current game.")));
				MBGameManager.EndGame();
				return;
			}
			if (saveHandler.IsSaving)
			{
				_saveAndExitStage = SaveAndExitStage.WaitingForCurrentSave;
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVE_IN_PROGRESS", "A save is already in progress; another save will run when it completes, then the game exits.")));
				return;
			}
			_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
			saveHandler.QuickSaveCurrentGame();
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVING_EXIT", "Saving the current game; it will exit when saving completes.")));
		}
		catch (Exception ex)
		{
			_saveAndExitStage = SaveAndExitStage.None;
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVE_EXIT_FAILED", "Save and exit failed: ") + ex.Message));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void OnSaveOver(bool isSuccessful, string saveName)
	{
		if (_saveAndExitStage == SaveAndExitStage.None)
		{
			return;
		}
		try
		{
			if (_saveAndExitStage == SaveAndExitStage.WaitingForCurrentSave)
			{
				SaveHandler saveHandler = Campaign.Current?.SaveHandler;
				if (saveHandler == null)
				{
					_saveAndExitStage = SaveAndExitStage.None;
					InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVE_EXIT_NO_HANDLER", "Save and exit failed: no save handler was found.")));
					ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
					return;
				}
				_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
				saveHandler.QuickSaveCurrentGame();
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVING_EXIT", "Saving the current game; it will exit when saving completes.")));
				return;
			}
			_saveAndExitStage = SaveAndExitStage.None;
			if (isSuccessful)
			{
				MBGameManager.EndGame();
				return;
			}
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVE_CANCELLED", "Saving the current game failed; exit cancelled.")));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
		catch (Exception ex)
		{
			_saveAndExitStage = SaveAndExitStage.None;
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SAVE_EXIT_FAILED", "Save and exit failed: ") + ex.Message));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void ShowImportSetupPopup(bool fromGate)
	{
		ShowImportSetupPopup(fromGate, ignoreSuppress: false);
	}

	private void ShowImportSetupPopup(bool fromGate, bool ignoreSuppress)
	{
		try
		{
			if (_setupDone || _welcomeInProgress)
			{
				return;
			}
			long ticks = DateTime.UtcNow.Ticks;
			if (!ignoreSuppress && _suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(fromGate ? 800 : 200).Ticks;
			_activeOnboardingStage = OnboardingUiStage.Import;
			string text = OnboardingUiText.T("AF_ONBOARDING_IMPORT_BODY", "This content includes world lore, character information, kingdom openings, personalities, and long-term strategies to improve the experience.\n\nSome lore increases CPU load.");
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_IMPORT_TITLE", "Knowledge-base import"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_IMPORT_ALL", "Import all"), OnboardingUiText.T("AF_ONBOARDING_SKIP", "Skip"), delegate
			{
				_welcomeInProgress = false;
				OpenImportFolderPicker(delegate
				{
					ShowImportSetupPopup(fromGate: true);
				});
			}, delegate
			{
				_welcomeInProgress = false;
				ShowSkipImportConfirmation(delegate
				{
					ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
				});
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowSkipImportConfirmation(Action onReturn)
	{
		try
		{
			if (onReturn == null)
			{
				onReturn = delegate
				{
				};
			}
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.Import;
			string text = OnboardingUiText.T("AF_ONBOARDING_SKIP_IMPORT_BODY", "Skip the database import?\nWithout it, NPCs will barely understand your world settings.");
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_SKIP_IMPORT_TITLE", "Skip database import"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_COMMON_CONFIRM", "Confirm"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate
			{
				_welcomeInProgress = false;
				CompleteOnboardingAndOpenPlayerPersonaSetup(onReturn, importedDatabase: false);
			}, delegate
			{
				_welcomeInProgress = false;
				onReturn();
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
			onReturn?.Invoke();
		}
	}

	private void CompleteOnboardingAndOpenPlayerPersonaSetup(Action onReturn, bool importedDatabase)
    {
        long generation = SaveRuntimeGuard.CaptureGeneration();
        Campaign campaign = Campaign.Current;
        bool finished = false;
        bool personaSubmitted = false;
        bool IsCurrent() => !finished && ReferenceEquals(Instance, this)
            && ReferenceEquals(campaign, Campaign.Current) && SaveRuntimeGuard.IsCurrentGeneration(generation);
        void Finish()
        {
            if (!IsCurrent()) return;
            finished = true;
            _setupDone = true;
            _welcomeInProgress = false;
            _activeOnboardingStage = OnboardingUiStage.None;
            (MyBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.QueueMissingOnnxGateCheckAfterOnboarding();
            onReturn?.Invoke();
        }
        try
        {
            ResetYjApiSetup();
            _activeOnboardingStage = OnboardingUiStage.PlayerPersona;
            KnowledgeLibraryBehavior knowledge = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
            if (knowledge == null) { Finish(); return; }
            InformationManager.DisplayMessage(new InformationMessage(importedDatabase
                ? OnboardingUiText.T("AF_ONBOARDING_IMPORT_DONE", "First import complete. Enter the player name, appearance, and background next; these can be skipped.")
                : OnboardingUiText.T("AF_ONBOARDING_IMPORT_SKIPPED", "Database import skipped. Enter the player name, appearance, and background next; these can be skipped.")));
            knowledge.OpenPlayerPersonaSetup(() =>
            {
                if (!IsCurrent() || personaSubmitted) return;
                personaSubmitted = true;
                ShowPeaceSceneConflictChoiceAfterPersona(Finish);
            });
        }
        catch (Exception ex)
        {
            if (!IsCurrent()) return;
            InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_PERSONA_FAILED", "Failed to open the player persona setup: ") + ex.Message));
            Finish();
        }
    }

	private void ShowPeaceSceneConflictChoiceAfterPersona(Action onDone)
	{
		if (onDone == null)
		{
			onDone = delegate
			{
			};
		}
		try
		{
			if (IsCurrentCampaignStoryMode())
			{
				_welcomeInProgress = false;
				ApplyPeaceSceneConflictOnboardingChoice(enabled: false, showMessage: false);
				onDone();
				return;
			}
			_welcomeInProgress = true;
			string text = OnboardingUiText.T("AF_ONBOARDING_SCENE_CONFLICT_BODY", "Allow direct player attacks to trigger scene conflicts?\n\nWhen enabled, attacking NPCs in peaceful town or lord-meeting scenes can trigger AnimusForge conflicts.\n\nWhen disabled, direct attacks use vanilla damage resolution; arguments and taunts can still escalate conflicts.\n\nThis choice is saved to MCM and can be changed from Scene Shout.");
			InformationManager.ShowInquiry(new InquiryData(OnboardingUiText.T("AF_ONBOARDING_SCENE_CONFLICT_TITLE", "AnimusForge - Scene conflict"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_ONBOARDING_ENABLE", "Enable"), OnboardingUiText.T("AF_COMMON_CLOSE", "Close"), delegate
			{
				_welcomeInProgress = false;
				ApplyPeaceSceneConflictOnboardingChoice(enabled: true);
				onDone();
			}, delegate
			{
				_welcomeInProgress = false;
				ApplyPeaceSceneConflictOnboardingChoice(enabled: false);
				onDone();
			}), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			_welcomeInProgress = false;
			try
			{
				Logger.Log("ModOnboarding", OnboardingUiText.T("AF_ONBOARDING_SCENE_CONFLICT_SHOW_FAILED", "[WARN] Failed to show peaceful-scene conflict settings: ") + ex.Message);
			}
			catch
			{
			}
			onDone();
		}
	}

	private static bool IsCurrentCampaignStoryMode()
	{
		try
		{
			Type type = Game.Current?.GameType?.GetType() ?? Campaign.Current?.GetType();
			while (type != null)
			{
				if (string.Equals(type.FullName, "StoryMode.CampaignStoryMode", StringComparison.Ordinal)
					|| string.Equals(type.Name, "CampaignStoryMode", StringComparison.Ordinal))
				{
					return true;
				}
				type = type.BaseType;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void ApplyPeaceSceneConflictOnboardingChoice(bool enabled, bool showMessage = true)
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				if (showMessage)
				{
					InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SCENE_CONFLICT_MCM_FAILED", "MCM settings could not be read; the peaceful-scene conflict option cannot be saved.")));
				}
				return;
			}
			settings.EnablePeaceSceneConflict = enabled;
			TryPersistMcmSettings(settings);
			if (showMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(enabled ? OnboardingUiText.T("AF_ONBOARDING_SCENE_CONFLICT_ENABLED", "Direct attacks can now trigger scene conflicts.") : OnboardingUiText.T("AF_ONBOARDING_SCENE_CONFLICT_DISABLED", "Direct attacks no longer trigger scene conflicts; damage uses vanilla logic.")));
			}
		}
		catch (Exception ex)
		{
			if (showMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_SCENE_CONFLICT_SAVE_FAILED", "Failed to save the peaceful-scene conflict option: ") + ex.Message));
			}
		}
	}

	internal static void TryPersistMcmSettings(DuelSettings settings)
	{
		if (settings == null)
		{
			return;
		}
		try
		{
			if (BaseSettingsProvider.Instance != null)
			{
				BaseSettingsProvider.Instance.SaveSettings(settings);
				return;
			}
			MethodInfo method = settings.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			method?.Invoke(settings, null);
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("ModOnboarding", OnboardingUiText.T("AF_ONBOARDING_MCM_PERSIST_WARN", "[WARN] Failed to persist MCM settings: ") + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static string BuildApiValidationFailureMessage(string effectiveApiUrl, string modelName, HttpStatusCode statusCode, string responseBody)
	{
		string text = OnboardingUiText.T("AF_ONBOARDING_API_VALIDATION_FAILED", "The API information in MCM failed its connection test; setup cannot continue yet.");
		if (!string.IsNullOrWhiteSpace(effectiveApiUrl))
		{
			text = text + OnboardingUiText.T("AF_ONBOARDING_API_ENDPOINT_LABEL", "\nEndpoint: ") + effectiveApiUrl;
		}
		if (!string.IsNullOrWhiteSpace(modelName))
		{
			text = text + OnboardingUiText.T("AF_ONBOARDING_API_MODEL_LABEL", "\nModel: ") + modelName;
		}
		text = text + OnboardingUiText.T("AF_ONBOARDING_API_STATUS_LABEL", "\nStatus: ") + statusCode;
		string text2 = responseBody ?? "";
		if ((int)statusCode == 404)
		{
			text += OnboardingUiText.T("AF_ONBOARDING_API_HINT_URL_MODEL", "\nTroubleshooting: check the Base URL suffix and model name.");
		}
		else if ((int)statusCode == 401 || (int)statusCode == 403)
		{
			text += OnboardingUiText.T("AF_ONBOARDING_API_HINT_KEY", "\nTroubleshooting: check whether the API key is valid.");
		}
		else if ((int)statusCode == 522)
		{
			text += OnboardingUiText.T("AF_ONBOARDING_API_HINT_UPSTREAM", "\nTroubleshooting: the gateway received the request, but the upstream host is unreachable.");
		}
		return LlmRetryPrompt.BuildFailureDetail(text, "", text2);
	}

	private static string BuildApiValidationFailureHint(HttpStatusCode statusCode, string responseBody)
	{
		int num = (int)statusCode;
		string text = (responseBody ?? "").Trim();
		string text2 = text.ToLowerInvariant();
		switch (num)
		{
		case 400:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_400", "The request format is invalid. The Base URL suffix may be wrong or the endpoint may not support chat completions.");
		case 401:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_401", "The API key is invalid, empty, or uses the wrong authentication format.");
		case 402:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_402", "Credits are insufficient, the plan is restricted, or this channel requires a top-up before model calls.");
		case 403:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_403", "This key cannot access the model or endpoint, or was blocked by platform risk controls.");
		case 404:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_404", "The Base URL suffix or endpoint path is wrong, or the model does not exist with this provider.");
		case 408:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_408", "The server took too long to respond, usually because of poor network quality or a slow upstream.");
		case 429:
			if (text2.Contains("quota") || text2.Contains("balance") || text2.Contains("insufficient"))
			{
				return OnboardingUiText.T("AF_ONBOARDING_HTTP_429_QUOTA", "Credits may be exhausted or the balance is too low, so the request was throttled or rejected.");
			}
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_429_RATE", "Requests are too frequent, concurrency is too high, or the account hit a rate limit. Try again shortly.");
		case 500:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_500", "The server failed internally. This is usually not a local input error; try again later.");
		case 502:
		case 503:
		case 504:
		case 522:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_UPSTREAM", "The gateway or upstream service is unavailable, usually due to a provider outage or network path issue.");
		default:
			return OnboardingUiText.T("AF_ONBOARDING_HTTP_UNKNOWN", "First check the Base URL, API key, model name, and network.");
		}
	}

	private static Task<ConfiguredChatValidationExchange> SendOnboardingChatValidationAsync(string providerId, string endpoint, string model, string apiKey, JObject payload, CancellationToken cancellationToken)
	{
		int maxTokens = payload?["max_tokens"]?.Value<int>() ?? 256;
		LlmProviderSnapshot provider = new LlmProviderSnapshot(
			providerId,
			(endpoint ?? "").Trim(),
			(model ?? "").Trim(),
			DuelSettings.LlmRequestTimeoutMilliseconds,
			Math.Max(1, maxTokens));
		LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => apiKey ?? "", disableThinking: true);
		return gateway.SendValidationAsync(provider, payload, cancellationToken);
	}

	private static string BuildModelsApiUrl(string rawUrl)
	{
		return LlmApiCompat.BuildModelListApiUrl(rawUrl);
	}

	private static bool CanUseBaseUrlStatusCode(HttpStatusCode statusCode)
	{
		int num = (int)statusCode;
		return num == 200 || num == 401 || num == 403 || num == 405 || num == 429 || num == 402;
	}

	private static string BuildBaseUrlValidationFailureMessage(HttpStatusCode statusCode, string responseBody)
	{
		string text = OnboardingUiText.T("AF_ONBOARDING_BASE_URL_ERROR", "Base URL check failed.");
		switch ((int)statusCode)
		{
		case 404:
			text += OnboardingUiText.T("AF_ONBOARDING_BASE_URL_ERROR_404", " The address is likely wrong; common causes are an incorrect Base URL suffix or endpoint path.");
			break;
		case 400:
			text += OnboardingUiText.T("AF_ONBOARDING_BASE_URL_ERROR_400", " The address returned an invalid request, so it likely does not support this OpenAI-style endpoint.");
			break;
		case 500:
		case 502:
		case 503:
		case 504:
		case 522:
			text += OnboardingUiText.T("AF_ONBOARDING_BASE_URL_ERROR_UPSTREAM", " The address is temporarily unavailable due to a server or network path issue.");
			break;
		default:
			text += OnboardingUiText.T("AF_ONBOARDING_BASE_URL_ERROR_DEFAULT", " Check that the Base URL is correct.");
			break;
		}
		return LlmRetryPrompt.BuildFailureDetail(text, "", responseBody);
	}

	internal static List<string> ExtractModelNamesFromResponse(string responseBody)
	{
		List<string> list = new List<string>();
		try
		{
			if (string.IsNullOrWhiteSpace(responseBody))
			{
				return list;
			}
			JToken jToken = JToken.Parse(responseBody);
			IEnumerable<JToken> enumerable = Enumerable.Empty<JToken>();
			if (jToken.Type == JTokenType.Object)
			{
				enumerable = ((JObject)jToken)["data"] as JArray ?? ((JObject)jToken)["models"] as JArray ?? new JArray();
			}
			else if (jToken.Type == JTokenType.Array)
			{
				enumerable = (JArray)jToken;
			}
			foreach (JToken item in enumerable)
			{
				string text = item.Type switch
				{
					JTokenType.String => item.ToString(), 
					_ => item["id"]?.ToString() ?? item["model"]?.ToString() ?? item["name"]?.ToString()
				};
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text.Trim());
				}
			}
		}
		catch
		{
		}
		return list.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string BuildModelFetchFailureMessage(HttpStatusCode statusCode, string responseBody)
	{
		string text = OnboardingUiText.T("AF_ONBOARDING_MODEL_ERROR", "Failed to fetch the model list.");
		switch ((int)statusCode)
		{
		case 401:
			text += OnboardingUiText.T("AF_ONBOARDING_MODEL_ERROR_KEY", " The API key is invalid or authentication failed.");
			break;
		case 402:
			text += OnboardingUiText.T("AF_ONBOARDING_MODEL_ERROR_CREDITS", " Credits are insufficient or the channel restricts endpoint calls.");
			break;
		case 403:
			text += OnboardingUiText.T("AF_ONBOARDING_MODEL_ERROR_ACCESS", " This key cannot access the model-list endpoint or was blocked by provider controls.");
			break;
		case 404:
			text += OnboardingUiText.T("AF_ONBOARDING_MODEL_ERROR_URL", " The Base URL is likely wrong or this provider does not support /models.");
			break;
		case 429:
			text += OnboardingUiText.T("AF_ONBOARDING_MODEL_ERROR_RATE", " Requests are too frequent or the account hit a rate limit.");
			break;
		default:
			text += OnboardingUiText.T("AF_ONBOARDING_MODEL_ERROR_MANUAL", " You can enter a model name manually to continue.");
			break;
		}
		return LlmRetryPrompt.BuildFailureDetail(text, "", responseBody);
	}

	private void OpenImportFolderPicker(Action onReturn)
	{
		try
		{
			if (onReturn == null)
			{
				onReturn = delegate
				{
				};
			}
			List<InquiryElement> list2 = new List<InquiryElement>();
			list2.Add(new InquiryElement("__manual__", OnboardingUiText.T("AF_ONBOARDING_IMPORT_MANUAL_PATH", "Enter folder name/path manually"), null));
			list2.Add(new InquiryElement("__latest__", OnboardingUiText.T("AF_ONBOARDING_IMPORT_LATEST", "Use latest player export (automatic)"), null));
			foreach (var folder in PlayerExportsStore.GetImportFolders())
			{
				string sourceLabel = folder.SourceLabel == "Module directory"
					? OnboardingUiText.T("AF_UI_ImportSource_ModuleDirectory", "Module directory")
					: folder.SourceLabel == "Player exports"
						? OnboardingUiText.T("AF_UI_ImportSource_PlayerExports", "Player exports")
						: folder.SourceLabel;
				list2.Add(new InquiryElement(folder.FullPath, OnboardingUiText.ImportFolderDisplayName(folder.SourceLabel, folder.Name) + "  [" + sourceLabel + "]", null));
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(OnboardingUiText.T("AF_ONBOARDING_IMPORT_PICKER_TITLE", "Choose import folder"), OnboardingUiText.T("AF_ONBOARDING_IMPORT_PICKER_BODY", "Choose a module folder or a player-exported world book:"), list2, isExitShown: true, 0, 1, OnboardingUiText.T("AF_ONBOARDING_IMPORT_BUTTON", "Import"), OnboardingUiText.T("AF_COMMON_BACK", "Back"), delegate(List<InquiryElement> selected)
			{
				if (selected == null || selected.Count == 0)
				{
					onReturn();
				}
				else
				{
					string text = selected[0].Identifier as string;
					if (text == "__manual__")
					{
						InformationManager.ShowTextInquiry(new TextInquiryData(OnboardingUiText.T("AF_ONBOARDING_IMPORT_MANUAL_PATH", "Enter folder name/path manually"), OnboardingUiText.T("AF_ONBOARDING_IMPORT_MANUAL_HINT", "Names search module folders first, then player exports; leave empty for the latest export."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, OnboardingUiText.T("AF_COMMON_CONFIRM", "Confirm"), OnboardingUiText.T("AF_COMMON_CANCEL", "Cancel"), delegate(string input)
						{
							string folderName2 = (input ?? "").Trim();
							TryImportRequiredSetAndUnlock(folderName2, onReturn);
						}, delegate
						{
							OpenImportFolderPicker(onReturn);
						}));
					}
					else
					{
						TryImportRequiredSetAndUnlock(text == "__latest__" ? "" : text ?? "", onReturn);
					}
				}
			}, delegate
			{
				onReturn();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_IMPORT_PICKER_FAILED", "Failed to open import selection: ") + ex.Message));
			onReturn?.Invoke();
		}
	}

	private void TryImportRequiredSetAndUnlock(string folderName, Action onReturn)
    {
        try
        {
        Campaign campaign = Campaign.Current;
        MyBehavior my = campaign?.GetCampaignBehavior<MyBehavior>();
        Func<bool> memoryCurrent = my?.CaptureOnboardingImportGuard();
        Func<bool> current = () => ReferenceEquals(Campaign.Current, campaign)
            && ReferenceEquals(campaign?.GetCampaignBehavior<ModOnboardingBehavior>(), this)
            && ReferenceEquals(campaign?.GetCampaignBehavior<MyBehavior>(), my)
            && (memoryCurrent == null || memoryCurrent());
        _databaseImport.Begin(folderName, current, directory =>
        {
            var kingdom = campaign?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
            var result = new OnboardingDatabaseImportController.KingdomInspection { Available = kingdom != null };
            if (kingdom == null) return result;
            result.Valid = kingdom.InspectImportDirectory(directory, out int total, out _, out int skipped, out string error);
            result.Total = total; result.Skipped = skipped; result.Error = error;
            result.Import = () =>
            {
                bool accepted = kingdom.ImportAllFromDirectory(directory, overwriteExisting: true, out string detail);
                if (!accepted) InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_IMPORT_KINGDOM_FAILED", "Import failed: kingdom strategy and personality import could not run. Reason: ") + detail));
                return accepted;
            };
            return result;
        }, directory => my?.CaptureOnboardingImports(directory), HasLoadedVoiceMapping,
        () => { if (current()) CompleteOnboardingAndOpenPlayerPersonaSetup(onReturn, importedDatabase: true); },
        () => { if (current()) OpenImportFolderPicker(onReturn); });

        }
        catch (Exception ex)
        {
			InformationManager.DisplayMessage(new InformationMessage(OnboardingUiText.T("AF_ONBOARDING_IMPORT_FAILED", "Import failed: ") + ex.Message));
			OpenImportFolderPicker(onReturn);
        }
    }

	private static bool InvokePrivateImport(MyBehavior my, string methodName, string folderName)
	{
		try
		{
			if (my == null)
			{
				return false;
			}
			MethodInfo method = typeof(MyBehavior).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (method == null)
			{
				return false;
			}
			method.Invoke(my, new object[1] { folderName ?? "" });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasLoadedVoiceMapping()
	{
		try
		{
			return VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
		}
		catch
		{
			return false;
		}
	}

	private static string GetModuleVersionText()
	{
		try
		{
			string path = Path.Combine(PlayerExportsStore.GetModuleRootPath(), "SubModule.xml");
			if (!File.Exists(path))
			{
				return OnboardingUiText.T("AF_ONBOARDING_UNKNOWN_VERSION", "Unknown version");
			}
			XDocument xDocument = XDocument.Load(path);
			string value = xDocument.Root?.Element("Version")?.Attribute("value")?.Value;
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		catch
		{
		}
		return OnboardingUiText.T("AF_ONBOARDING_UNKNOWN_VERSION", "Unknown version");
	}

	private static string ResolveImportFolderPath(string folderName)
	{
		return PlayerExportsStore.ResolveImportFolderPath(folderName);
	}
}
