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
		Import
	}

	private enum ApiSetupTarget
	{
		Primary,
		Auxiliary,
		ActionPostprocess,
		EventAndRebellion
	}

	private enum QuickApiPreset
	{
		None,
		DeepSeekFlash,
		DeepSeekPro
	}

	private enum YjApiSetupMode
	{
		None,
		SingleGroup,
		MultiGroup
	}

	private enum SaveAndExitStage
	{
		None,
		WaitingForCurrentSave,
		WaitingForRequestedQuickSave
	}

	private enum ApiValidationFlow
	{
		Normal,
		QuickPresetAll,
		ExistingConfigAll
	}

	private sealed class ApiValidationTargetInfo
	{
		public ApiSetupTarget Target { get; set; }

		public string DisplayName { get; set; } = "";

		public string ApiUrl { get; set; } = "";

		public string ApiKey { get; set; } = "";

		public string ModelName { get; set; } = "";
	}

	private sealed class ApiValidationTargetResult
	{
		public ApiValidationTargetInfo Target { get; set; }

		public bool Success { get; set; }

		public string Message { get; set; } = "";

		public string FailureHint { get; set; } = "";
	}

	private const string SetupDoneKey = "_AnimusForge_setup_done_v1";

	private const string DeepSeekApiBaseUrl = "https://api.deepseek.com";

	private const string DeepSeekApiKeysUrl = "https://platform.deepseek.com/api_keys";

	private const string YjApiBaseUrl = "https://yjapi.manqiaotechnology.com/v1";

	private const string YjApiKeysUrl = "https://yjapi.manqiaotechnology.com/keys";

	private const string YjApiPurchaseUrl = "https://pay.ldxp.cn/shop/OF6AKWNI";

	private const string YjApiPlayerQqGroupNumber = "1097237977";

	private const string DeepSeekFlashModelName = "deepseek-v4-flash";

	private const string DeepSeekProModelName = "deepseek-v4-pro";

	private bool _setupDone;

	private bool _welcomeShownThisSession;

	private bool _welcomeInProgress;

	private long _suppressWelcomeUntilUtcTicks;

	private bool _pendingWelcome;

	private long _pendingWelcomeAfterUtcTicks;

	private bool _apiValidationInProgress;

	private CancellationTokenSource _apiValidationCancellation;

	private int _apiValidationVersion;

	private bool _pendingApiValidationResult;

	private int _pendingApiValidationVersion;

	private bool _pendingApiValidationSuccess;

	private string _pendingApiValidationMessage = "";

	private string _pendingApiValidationFailureHint = "";

	private bool _apiValidationReturnToModelSelection;

	private bool _baseUrlValidationInProgress;

	private CancellationTokenSource _baseUrlValidationCancellation;

	private int _baseUrlValidationVersion;

	private bool _pendingBaseUrlValidationResult;

	private bool _pendingBaseUrlValidationSuccess;

	private string _pendingBaseUrlValidationMessage = "";

	private string _pendingValidatedBaseUrl = "";

	private string _lastBaseUrlValidationFailureMessage = "";

	private bool _modelFetchInProgress;

	private CancellationTokenSource _modelFetchCancellation;

	private int _modelFetchVersion;

	private bool _pendingModelFetchResult;

	private int _pendingModelFetchVersion;

	private bool _pendingModelFetchSuccess;

	private string _pendingModelFetchMessage = "";

	private List<string> _pendingModelFetchModels = new List<string>();

	private List<string> _lastFetchedModelNames = new List<string>();

	private bool _pendingReturnToWelcome;

	private OnboardingUiStage _activeOnboardingStage;

	private OnboardingUiStage _pendingUnexpectedResumeStage;

	private long _pendingUnexpectedResumeAfterUtcTicks;

	private bool _startupNoticeShownThisSession;

	private bool _pendingStartupNotice;

	private long _pendingStartupNoticeAfterUtcTicks;

	private ApiSetupTarget _currentApiSetupTarget;

	private bool _apiRepairFlowActive;

	private QuickApiPreset _selectedQuickApiPreset;

	private bool _quickPresetFlowActive;

	private YjApiSetupMode _yjApiSetupMode;

	private bool _yjSingleGroupKeyConfirmed;

	private bool _apiOnlySetupFlowActive;

	private SaveAndExitStage _saveAndExitStage;

	private ApiValidationFlow _apiValidationFlow;

	private bool _pendingActionPostprocessSetup;

	private long _pendingActionPostprocessSetupAfterUtcTicks;

	private bool _actionPostprocessSetupShownThisSession;

	public static ModOnboardingBehavior Instance { get; private set; }

	public ModOnboardingBehavior()
	{
		Instance = this;
		_currentApiSetupTarget = ApiSetupTarget.Primary;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnGameStarted);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameStarted);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_AnimusForge_setup_done_v1", ref _setupDone);
		if (!_setupDone)
		{
			_welcomeShownThisSession = false;
		}
	}

	private void OnGameStarted(CampaignGameStarter starter)
	{
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
			_pendingWelcome = true;
			_pendingWelcomeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(2.0).Ticks;
		}
		catch
		{
		}
	}

	private void MarkPendingStartupNotice()
	{
		try
		{
			_pendingStartupNotice = true;
			_pendingStartupNoticeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(1.0).Ticks;
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
			_pendingActionPostprocessSetup = true;
			_pendingActionPostprocessSetupAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(3.0).Ticks;
		}
		catch
		{
		}
	}

	private void OnTick(float dt)
	{
		try
		{
			ProcessPendingBaseUrlValidationResult();
			ProcessPendingApiValidationResult();
			ProcessPendingModelFetchResult();
			ProcessPendingReturnToWelcome();
			ProcessUnexpectedOnboardingDismissal();
			if (_pendingStartupNotice && !_startupNoticeShownThisSession && DateTime.UtcNow.Ticks >= _pendingStartupNoticeAfterUtcTicks && Campaign.Current != null && Campaign.Current.GameStarted)
			{
				_pendingStartupNotice = false;
				_startupNoticeShownThisSession = true;
				ShowStartupNotice();
			}
			if (!_setupDone && _pendingWelcome && !_welcomeShownThisSession && DateTime.UtcNow.Ticks >= _pendingWelcomeAfterUtcTicks && Campaign.Current != null && Campaign.Current.GameStarted)
			{
				_pendingWelcome = false;
				_welcomeShownThisSession = true;
				ShowSetupModeChoicePopup(fromGate: false);
			}
			if (_setupDone && _pendingActionPostprocessSetup && !_actionPostprocessSetupShownThisSession && DateTime.UtcNow.Ticks >= _pendingActionPostprocessSetupAfterUtcTicks && Campaign.Current != null && Campaign.Current.GameStarted)
			{
				_pendingActionPostprocessSetup = false;
				_actionPostprocessSetupShownThisSession = true;
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
			return "前处理API";
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return "后处理API";
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return "事件/叛乱API";
		}
		return "主API";
	}

	private string CurrentApiBaseUrlDisplayName()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return "前处理API Base URL";
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return "后处理API Base URL";
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return "事件/叛乱API Base URL";
		}
		return "主API Base URL";
	}

	private string CurrentApiKeyDisplayName()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return "前处理API Key";
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return "后处理API Key";
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return "事件/叛乱API Key";
		}
		return "主API Key";
	}

	private string CurrentApiModelDisplayName()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return "前处理模型名称";
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return "后处理模型名称";
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return "事件/叛乱模型名称";
		}
		return "主模型名称";
	}

	private string CurrentYjApiModelRecommendation()
	{
		if (IsAuxiliaryApiSetupTarget())
		{
			return "前处理API：建议优先选择速度快且价格较低的模型。";
		}
		if (IsActionPostprocessApiSetupTarget())
		{
			return "后处理API：建议选择分析与推理能力较强的模型，例如 GPT。";
		}
		if (IsEventAndRebellionApiSetupTarget())
		{
			return "周报与叛乱API：周报生成建议选择文采较好的模型，例如 Gemini；叛乱命名也会共用此模型。";
		}
		return "主API（NPC 正文）：建议选择文采较好的模型，例如 Gemini。";
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
			InformationManager.ShowInquiry(new InquiryData("兼容性提示", DuelSettings.GetContextExtractionCompatibilityWarningMessage() + "\n\n是否继续当前流程？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "继续", "返回", delegate
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

	private static void SetYjApiBaseUrlForAllTargets(DuelSettings settings)
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

	private static void SetModelNameForTarget(DuelSettings settings, ApiSetupTarget target, string value)
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
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			return;
		}
		if ((_setupDone && !_apiOnlySetupFlowActive) || _pendingReturnToWelcome || _pendingBaseUrlValidationResult || _pendingApiValidationResult || _pendingModelFetchResult)
		{
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			return;
		}
		if (_activeOnboardingStage != OnboardingUiStage.SetupModeChoice && _activeOnboardingStage != OnboardingUiStage.YjApiChoice && _activeOnboardingStage != OnboardingUiStage.YjApiKey && _activeOnboardingStage != OnboardingUiStage.Welcome && _activeOnboardingStage != OnboardingUiStage.DeepSeekApiKeyOwnership && _activeOnboardingStage != OnboardingUiStage.QuickPresetApiKey && _activeOnboardingStage != OnboardingUiStage.AuxiliaryChoice && _activeOnboardingStage != OnboardingUiStage.PostprocessChoice && _activeOnboardingStage != OnboardingUiStage.EventRebellionChoice && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidation && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidationFailure && _activeOnboardingStage != OnboardingUiStage.ApiValidation && _activeOnboardingStage != OnboardingUiStage.ModelFetch && _activeOnboardingStage != OnboardingUiStage.ModelSelect && _activeOnboardingStage != OnboardingUiStage.Import)
		{
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			return;
		}
		if (InformationManager.IsAnyInquiryActive())
		{
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			return;
		}
		if (_pendingUnexpectedResumeStage != _activeOnboardingStage)
		{
			_pendingUnexpectedResumeStage = _activeOnboardingStage;
			_pendingUnexpectedResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(150.0).Ticks;
			return;
		}
		if (DateTime.UtcNow.Ticks < _pendingUnexpectedResumeAfterUtcTicks)
		{
			return;
		}
		OnboardingUiStage pendingUnexpectedResumeStage = _pendingUnexpectedResumeStage;
		_pendingUnexpectedResumeStage = OnboardingUiStage.None;
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
		bool pendingBaseUrlValidationSuccess = _pendingBaseUrlValidationSuccess;
		string pendingBaseUrlValidationMessage = _pendingBaseUrlValidationMessage ?? "";
		string pendingValidatedBaseUrl = (_pendingValidatedBaseUrl ?? "").Trim();
		_pendingBaseUrlValidationResult = false;
		_pendingBaseUrlValidationSuccess = false;
		_pendingBaseUrlValidationMessage = "";
		_pendingValidatedBaseUrl = "";
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
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能保存 Base URL。"));
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
		if (pendingModelFetchVersion != _modelFetchVersion)
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
				NonBlockingErrorReport.Show("模型列表拉取失败", pendingModelFetchMessage);
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
		if (pendingApiValidationVersion != _apiValidationVersion)
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
				fullFailureText = (fullFailureText + "\n\n排查建议：" + pendingApiValidationFailureHint).Trim();
			}
			// 失败详情可能含完整模型/API 响应；完整且脱敏后的文本直接显示在左下角，并保留给按需 AI 分析。
			NonBlockingErrorReport.Show("API 连接失败", fullFailureText);
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
			InformationManager.DisplayMessage(new InformationMessage("AnimusForge 已启动，版本号：" + moduleVersionText + "。", Colors.Yellow));
			InformationManager.DisplayMessage(new InformationMessage("当前版本不建议搭配其他功能性 Mod 游玩；若出现崩溃，请先排查你的 Mod 加载清单！", Colors.Yellow));
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
				InformationManager.DisplayMessage(new InformationMessage("当前已有 AnimusForge 引导或 API 测试正在进行，请先完成或取消当前流程。"));
				return false;
			}
			ResetYjApiSetup();
			_apiOnlySetupFlowActive = true;
			_pendingWelcome = false;
			_pendingReturnToWelcome = false;
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
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
			InformationManager.DisplayMessage(new InformationMessage("打开 API 重新配置失败：" + ex.Message));
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
		_pendingUnexpectedResumeStage = OnboardingUiStage.None;
		InformationManager.HideInquiry();
		InformationManager.DisplayMessage(new InformationMessage("已取消 API 重新配置。"));
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
		_pendingUnexpectedResumeStage = OnboardingUiStage.None;
		TryPersistMcmSettings(DuelSettings.GetSettings());
		InformationManager.HideInquiry();
		InformationManager.DisplayMessage(new InformationMessage("API 重新配置已完成：配置已写入 MCM，已返回游戏，不会进入数据库导入或首次使用流程。"));
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
		try
		{
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
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("support", "支持AnimusForge制作组", null, isEnabled: true, "打开爱发电支持页面。"),
				new InquiryElement("deepseek_flash", "使用官方deepseek-flash推荐API组合进行游玩", null, isEnabled: true, "自动填写 DeepSeek Flash 预设，只需输入一次 API Key。"),
				new InquiryElement("deepseek_pro", "使用官方deepseek-pro推荐API组合进行游玩", null, isEnabled: true, "自动填写 DeepSeek Pro 预设，只需输入一次 API Key。"),
				new InquiryElement("custom", "使用完全自定义的API进行游玩", null, isEnabled: true, "进入旧路径，逐项填写 Base URL、API Key 和模型。"),
				new InquiryElement("existing_config", "使用现有配置进行游玩", null, isEnabled: true, "直接测试当前 MCM 配置；周报和叛乱API未配置完整时会跳过该项。")
			};
			if (showYjApiSetupOption)
			{
				list.Insert(1, new InquiryElement("yj_api", "使用 YJ API 中转站进行游玩", null, isEnabled: true, "固定接入 YJ API 中转站；可选择单分组或多分组 API Key 配置。"));
			}
			if (!_apiOnlySetupFlowActive)
			{
				list.Add(new InquiryElement("save_exit", "保存存档并退出", null, isEnabled: true, "保存当前存档后退出到主界面。"));
			}
			string text = _apiOnlySetupFlowActive
				? "你正在从 AnimusForge 终端重新配置 API。\n\n本流程只会写入并测试主API、前处理API、后处理API、周报和叛乱API配置；测试通过后会直接返回游戏，不会进入数据库导入或首次使用流程。"
				: "请选择首次使用的 API 配置方式。\n\n推荐组合会自动写入主API、前处理API、后处理API、周报和叛乱API的 Base URL、模型、思维链和温度；你只需要填写一次 API Key。";
			// API 失败详情由左下角通知承载，快捷配置菜单始终只显示可操作的配置说明。
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("AnimusForge - API 快捷配置", text, list, isExitShown: false, 0, 1, "确定", "关闭", delegate(List<InquiryElement> selected)
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
			_activeOnboardingStage = OnboardingUiStage.YjApiChoice;
			_welcomeInProgress = true;
			string yjBackLabel = _apiOnlySetupFlowActive ? "返回 API 重新配置菜单" : "返回首次引导菜单";
			string yjBackDescription = _apiOnlySetupFlowActive ? "返回 AnimusForge 的 API 重新配置菜单。" : "返回 AnimusForge 的首次 API 配置菜单。";
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("yj_prepare_account", "获取 API Key 并购买额度", null, isEnabled: true, "依次打开 YJ API Key 管理页和额度购买页。"),
				new InquiryElement("yj_multi_group", "多分组 API Key 输入", null, isEnabled: true, "主API、前处理API、后处理API、周报与叛乱API分别输入一个 Key，并分别拉取该分组的模型。"),
				new InquiryElement("yj_single_group", "单分组 API Key 输入", null, isEnabled: true, "只输入一次 Key，自动写入四条 API；随后仍会为四条 API 分别拉取模型并选择。"),
				new InquiryElement("yj_back", yjBackLabel, null, isEnabled: true, yjBackDescription),
				new InquiryElement("yj_copy_qq_group", "复制YJ中转站 QQ 群号", null, isEnabled: true, "复制 QQ 群号 " + YjApiPlayerQqGroupNumber + " 到剪贴板。")
			};
			string text = "【开始游玩前必须完成】\n点击“获取 API Key 并购买额度”会依次打开 Key 管理页和额度购买页；请创建有效 Key 并充值足够额度。\n\n缺少有效 API Key 或可用额度时，无法拉取模型、通过连接测试或使用 AI 进行游玩。\n单分组 Key 只需输入一次；多分组 Key 需要为四条 API 分别输入。";
			if (_apiOnlySetupFlowActive)
			{
				text += "\n\n当前为终端 API 重新配置：四条 API 完成连接测试后会直接返回游戏，不会进入数据库导入。";
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("YJ API 中转站", text, list, isExitShown: false, 0, 1, "确定", "返回", delegate(List<InquiryElement> selected)
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
			InformationManager.DisplayMessage(new InformationMessage("打开 YJ API 中转站菜单失败：" + ex.Message));
			ResetYjApiSetup();
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private static void CopyYjApiPlayerQqGroupNumberToClipboard()
	{
		try
		{
			TaleWorlds.InputSystem.Input.SetClipboardText(YjApiPlayerQqGroupNumber);
			InformationManager.DisplayMessage(new InformationMessage("YJ 交流 QQ 群号已复制到剪贴板：" + YjApiPlayerQqGroupNumber, new Color(0.35f, 1f, 0.35f)));
		}
		catch
		{
			InformationManager.DisplayMessage(new InformationMessage("复制 QQ 群号失败，请手动复制：" + YjApiPlayerQqGroupNumber, Colors.Yellow));
		}
	}

	private static void OpenYjApiPreparationPages()
	{
		OpenYjApiExternalPage(YjApiKeysUrl, "API Key 管理");
		OpenYjApiExternalPage(YjApiPurchaseUrl, "额度购买");
	}

	private static void OpenYjApiExternalPage(string url, string pageName)
	{
		// 无默认浏览器时由共享启动器仅在用户点击时执行一次有界本机查找，不影响游戏热路径。
		if (ExternalBrowserLauncher.TryOpen(url, out bool usedLocalBrowserFallback, out string failureMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(usedLocalBrowserFallback ? "[系统] 默认浏览器无法启动，已使用本机浏览器打开 YJ API " + pageName + "页面。" : "[系统] 正在打开 YJ API " + pageName + "页面。"));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("[系统] 打开 YJ API " + pageName + "页面失败：" + failureMessage));
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
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能配置 " + CurrentApiDisplayName() + "。"));
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
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写 " + CurrentApiKeyDisplayName() + "。"));
				ShowYjApiSetupMenu();
				return;
			}
			_activeOnboardingStage = OnboardingUiStage.YjApiKey;
			_welcomeInProgress = true;
			bool singleGroup = _yjApiSetupMode == YjApiSetupMode.SingleGroup;
			string title = singleGroup ? "填写 YJ API 单分组 Key" : "填写 YJ API " + CurrentApiKeyDisplayName();
			string text = singleGroup
				? "请输入一个 YJ API Key。确认后会将固定 Base URL 和该 Key 写入主API、前处理API、后处理API、周报与叛乱API；随后会依次拉取四次模型列表并为每条 API 选择模型。"
				: "请输入当前 " + CurrentApiDisplayName() + " 所属分组的 YJ API Key。确认后只会写入当前 API，并拉取该 Key 可用的模型。";
			InformationManager.ShowTextInquiry(new TextInquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "下一步", "返回", delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("API Key 不能为空。"));
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
				InformationManager.DisplayMessage(new InformationMessage((singleGroup ? "YJ API 单分组 Key" : CurrentApiKeyDisplayName()) + " 已写入 MCM，正在拉取 " + CurrentApiModelDisplayName() + " 可用模型。"));
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
			InformationManager.DisplayMessage(new InformationMessage("打开 YJ API Key 输入框失败：" + ex.Message));
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
			InformationManager.ShowInquiry(new InquiryData("DeepSeek API Key 确认", "你是否拥有 DeepSeek 官方 API Key？\n\n如果没有，请点击“无”，前往 DeepSeek 官方开放平台创建 API Key 并充值。完成后返回游戏，再次选择推荐组合。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "有", "无", delegate
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
			InformationManager.DisplayMessage(new InformationMessage("打开 DeepSeek API Key 确认框失败：" + ex.Message));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private static void OpenDeepSeekApiKeysPage()
	{
		// 与 YJ 页面复用同一受限浏览器发现逻辑，避免缺少默认关联时中断引导。
		if (ExternalBrowserLauncher.TryOpen(DeepSeekApiKeysUrl, out bool usedLocalBrowserFallback, out string failureMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(usedLocalBrowserFallback ? "[系统] 默认浏览器无法启动，已使用本机浏览器打开 DeepSeek 官方 API Keys 页面。" : "[系统] 正在打开 DeepSeek 官方 API Keys 页面。"));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("[系统] 打开 DeepSeek 官方页面失败：" + failureMessage));
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
			InformationManager.DisplayMessage(new InformationMessage("写入 DeepSeek 推荐配置失败：" + ex.Message));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private bool ApplyDeepSeekPresetToMcm(QuickApiPreset preset, string apiKey)
	{
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能写入 DeepSeek 推荐配置。"));
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
			string text = "已写入 " + presetName + " 推荐 API 组合。\n请输入 DeepSeek API Key；该 Key 会同时写入主API、前处理API、后处理API、周报和叛乱API。";
			// 上次失败已由左下角通知报告；输入框正文保持稳定，避免原始响应挤占输入控件。
			InformationManager.ShowTextInquiry(new TextInquiryData("填写 DeepSeek API Key", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存并测试四条API", "返回", delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("API Key 不能为空。"));
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
			InformationManager.DisplayMessage(new InformationMessage("DeepSeek 推荐配置已写入 MCM，正在测试四条 API。"));
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
			InformationManager.DisplayMessage(new InformationMessage("打开 DeepSeek API Key 输入框失败：" + ex.Message));
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
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能测试 API 组合。"));
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
		int num = ++_apiValidationVersion;
		ShowApiValidationProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string text = "";
			string failureHint = "";
			CancellationTokenSource cancellationTokenSource = null;
			try
			{
				cancellationTokenSource = new CancellationTokenSource();
				_apiValidationCancellation = cancellationTokenSource;
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
					text = string.Join("\n\n", failedResults.Select((ApiValidationTargetResult x) => x?.Message ?? "API 组合连接测试失败。"));
				}
			}
			catch (OperationCanceledException)
			{
				failureHint = "你已手动取消本次测试，可以回到首次菜单重新选择配置方式。";
				text = "API 组合测试已取消。";
			}
			catch (Exception ex)
			{
				failureHint = "通常是网络异常、证书或代理设置异常，或者某条 API 配置不正确。";
				text = "API 组合测试异常：" + ex.Message;
			}
			finally
			{
				if (num == _apiValidationVersion)
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
				missingMessage = displayName + "未配置完整，请检查 Base URL、API Key 和模型名称。";
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
		NonBlockingErrorReport.Show("API 配置不完整", string.IsNullOrWhiteSpace(message) ? "API 配置未填写完整，无法开始组合测试。" : message);
		ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
	}

	private static async Task<ApiValidationTargetResult> ValidateApiTargetAsync(ApiValidationTargetInfo target, CancellationToken cancellationToken)
	{
		ApiValidationTargetResult result = new ApiValidationTargetResult
		{
			Target = target
		};
		if (target == null)
		{
			result.Message = "API 组合测试失败：目标配置为空。";
			result.FailureHint = "请回到首次菜单重新选择配置方式。";
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
						["content"] = "请回复“连接成功”。"
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
						result.FailureHint = "接口返回了 HTTP 成功状态，但响应结构中没有可用的模型回复。";
						result.Message = LlmRetryPrompt.BuildFailureDetail(target.DisplayName + "回复解析失败。", "", responseBody);
						return result;
					}
					result.Success = true;
					result.Message = target.DisplayName + "连接测试成功。";
					return result;
				}
				catch (Exception ex)
				{
					result.FailureHint = "接口返回了 HTTP 成功状态，但响应不是可解析的聊天补全格式。";
					result.Message = LlmRetryPrompt.BuildFailureDetail(target.DisplayName + "回复解析失败：" + ex.Message, "", responseBody);
					return result;
				}
			}
			if (!exchange.HasStatusCode)
			{
				result.FailureHint = "通常是网络异常、证书或代理设置异常，或者 " + target.DisplayName + " 的 Base URL 填写不正确。";
				result.Message = target.DisplayName + "连接测试失败：" + (exchange.ErrorMessage ?? "未知错误");
				return result;
			}
			HttpStatusCode statusCode = (HttpStatusCode)exchange.StatusCode;
			result.FailureHint = BuildApiValidationFailureHint(statusCode, responseBody);
			result.Message = target.DisplayName + "连接测试失败。\n" + BuildApiValidationFailureMessage(effectiveApiUrl, target.ModelName, statusCode, responseBody);
			return result;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			result.FailureHint = "通常是网络异常、证书或代理设置异常，或者 " + target.DisplayName + " 的 Base URL 填写不正确。";
			result.Message = target.DisplayName + "连接测试异常：" + ex.Message;
			return result;
		}
	}

	private static string BuildApiConfigSetValidationSuccessMessage(ApiValidationFlow flow, IEnumerable<ApiValidationTargetResult> results, bool eventSkipped)
	{
		string label = flow == ApiValidationFlow.ExistingConfigAll ? "现有配置测试通过" : "API 组合测试通过";
		string names = string.Join("、", (results ?? Enumerable.Empty<ApiValidationTargetResult>()).Where((ApiValidationTargetResult x) => x?.Target != null).Select((ApiValidationTargetResult x) => x.Target.DisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)));
		if (string.IsNullOrWhiteSpace(names))
		{
			names = "已配置API";
		}
		string text = label + "：" + names + "。";
		if (eventSkipped)
		{
			text += "周报与叛乱API未配置完整，已跳过该项测试。";
		}
		return text;
	}

	private static string GetApiDisplayName(ApiSetupTarget target)
	{
		if (target == ApiSetupTarget.Auxiliary)
		{
			return "前处理API";
		}
		if (target == ApiSetupTarget.ActionPostprocess)
		{
			return "后处理API";
		}
		if (target == ApiSetupTarget.EventAndRebellion)
		{
			return "周报与叛乱API";
		}
		return "主API";
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
			string text = "周事件自动生成失败，请检查你的 Base URL、API Key、模型名或当前网络环境。";
			// 此处只提供修复动作；详细失败内容已在触发时写入左下角消息和日志。
			InformationManager.ShowInquiry(new InquiryData("调整 API 信息", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写 API 信息", "测试已有配置", delegate
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
				? "前处理API（规则检索/规则路由）当前不可用。你可以直接测试 MCM 中的现有配置，也可以重新填写前处理API信息。前处理API为必填，不提供回退RAG选项。"
				: "前处理API（规则检索/规则路由）当前不可用，请检查前处理API的 Base URL、API Key、模型名称，或当前网络环境。前处理API为必填，不提供回退RAG选项。";
			// 此窗口保留填写/测试动作，不再混入可能很长的失败响应。
			InformationManager.ShowInquiry(new InquiryData("调整前处理API信息", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写前处理API信息", "测试现有配置", delegate
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
				? "后处理API当前不可用。你可以直接测试 MCM 中的现有配置，也可以重新填写后处理API信息。\n\n后处理任务对判定稳定性要求较高，建议优先选择带思考模式的模型，或直接使用更高级模型。"
				: "后处理API当前不可用。你可以重新填写后处理API信息，或继续回退使用主API处理后处理任务。\n\n后处理任务对判定稳定性要求较高，建议优先选择带思考模式的模型，或直接使用更高级模型。";
			// 此窗口保留填写/回退动作，不再混入可能很长的失败响应。
			string negativeText = hasExistingConfig ? "测试现有配置" : "继续使用主API";
			InformationManager.ShowInquiry(new InquiryData("调整后处理API信息", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写后处理API", negativeText, delegate
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
				? "事件/叛乱API当前不可用。你可以直接测试 MCM 中的现有配置，也可以重新填写事件/叛乱API信息。\n\n这个接口用于周报生成与叛乱建国命名；叛乱命名失败后不会再使用本地国名兜底。"
				: "事件/叛乱API当前不可用。请重新填写事件/叛乱API的 Base URL、API Key、模型名称，或检查当前网络环境。\n\n这个接口用于周报生成与叛乱建国命名；叛乱命名失败后不会再使用本地国名兜底。";
			// 此窗口保留填写/测试动作，不再混入可能很长的失败响应。
			InformationManager.ShowInquiry(new InquiryData("调整事件/叛乱API信息", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写事件/叛乱API", "测试现有配置", delegate
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
			string title = "欢迎使用 AnimusForge";
			string text = "开始游玩前，请先确认主API信息。主API用于NPC正文生成，如果未正确配置，AI 对话功能将无法使用。";
			// 欢迎页只负责提供后续动作，失败报告已在测试完成时显示于左下角。
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写主API信息", "测试已有配置", delegate
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
			string title = "配置前处理API（必填）";
			string text = "前处理API专门用于规则检索/规则路由。启用后，规则话题会先走一次低成本筛选，再进入主API正文生成。前处理API为必填，不提供回退RAG选项。";
			// 配置页正文不复用失败详情，保证输入与操作按钮始终可见。
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写前处理API", "测试现有配置", delegate
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
			string title = "配置后处理API";
			string text = "后处理API专门用于动作标签/情绪标签判定。配置后可以把后处理链路和前处理、主API正文生成彻底拆开；如果你暂时不想配置，也可以继续回退使用主API处理后处理任务。\n\n后处理任务对判定稳定性要求较高，建议优先选择带思考模式的模型，或直接使用更高级模型。";
			// 配置页正文不复用失败详情，保证输入与回退操作始终可见。
			string negativeText = hasExistingConfig ? "测试现有配置" : "继续使用主API";
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写后处理API", negativeText, delegate
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
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写 " + CurrentApiBaseUrlDisplayName() + "。"));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData("填写 Base URL", "请输入 " + CurrentApiBaseUrlDisplayName() + "。\n示例：" + CurrentApiBaseUrlExample(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "下一步", "返回", delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiBaseUrlDisplayName() + " 不能为空。"));
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
			InformationManager.DisplayMessage(new InformationMessage("打开 " + CurrentApiBaseUrlDisplayName() + " 输入框失败：" + ex.Message));
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
		int num = ++_baseUrlValidationVersion;
		ShowBaseUrlValidationProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string message = "";
			CancellationTokenSource cancellationTokenSource = null;
			try
			{
				cancellationTokenSource = new CancellationTokenSource();
				_baseUrlValidationCancellation = cancellationTokenSource;
				ModelCatalogExchange exchange = await new LegacyModelCatalogGateway().ProbeBaseUrlAsync(validatedBaseUrl, cancellationTokenSource.Token);
				string text2 = exchange.ResponseBody;
				if (exchange.Cancelled)
				{
					throw new OperationCanceledException(cancellationTokenSource.Token);
				}
				if (exchange.HasStatusCode && CanUseBaseUrlStatusCode((HttpStatusCode)exchange.StatusCode))
				{
					flag = true;
					message = "Base URL 检查通过，可以继续填写 API Key。";
				}
				else if (exchange.HasStatusCode)
				{
					message = BuildBaseUrlValidationFailureMessage((HttpStatusCode)exchange.StatusCode, text2);
				}
				else
				{
					message = "Base URL 检查失败：" + ModelCatalogErrorFormatter.Format(exchange.ErrorCode, exchange.ErrorArguments, legacyMessage: exchange.ErrorMessage ?? "未知错误");
				}
			}
			catch (OperationCanceledException)
			{
				message = "Base URL 检查已取消。";
			}
			catch (Exception ex)
			{
				message = "Base URL 检查失败：" + ex.Message;
			}
			finally
			{
				if (num == _baseUrlValidationVersion)
				{
					if (ReferenceEquals(_baseUrlValidationCancellation, cancellationTokenSource))
					{
						_baseUrlValidationCancellation = null;
					}
					_baseUrlValidationInProgress = false;
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
			InformationManager.DisplayMessage(new InformationMessage("Base URL 不能为空。"));
			OpenApiBaseUrlInput();
			return;
		}
		if (!Uri.TryCreate(text, UriKind.Absolute, out Uri uriResult) || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
		{
			InformationManager.DisplayMessage(new InformationMessage("Base URL 格式不正确，请填写完整的 http/https 地址。"));
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
			InformationManager.ShowInquiry(new InquiryData("正在检查base URL", "正在检查你填写的 Base URL 是否可用，请稍候……\n\n只有检查通过后，才可以进入下一步填写 API Key。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "退出当前存档", "返回上一界面", ExitCurrentGameFromOnboarding, CancelBaseUrlValidationAndReturn), pauseGameActiveState: true);
		}
		catch
		{
		}
	}

	private void ShowBaseUrlValidationFailurePopup()
	{
		try
		{
			string text = string.IsNullOrWhiteSpace(_lastBaseUrlValidationFailureMessage) ? "你填写的 Base URL 当前不可用，请重新检查后再试。" : _lastBaseUrlValidationFailureMessage;
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.BaseUrlValidationFailure;
			_lastBaseUrlValidationFailureMessage = "";
			// 诊断详情改为左下角消息；仅保留紧凑动作窗，以免丢失原有“退出当前存档”出口。
			NonBlockingErrorReport.Show("base URL 检查失败", text);
			InformationManager.ShowInquiry(new InquiryData("下一步怎么做？", "Base URL 当前不可用。请选择重新填写，或退出当前存档。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "重新填写base URL", "退出当前存档", delegate
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
			_baseUrlValidationVersion++;
			_baseUrlValidationInProgress = false;
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
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写 " + CurrentApiKeyDisplayName() + "。"));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData("填写 API Key", "请输入 " + CurrentApiKeyDisplayName() + "。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "下一步", "返回", delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiKeyDisplayName() + " 不能为空。"));
					OpenApiKeyInput();
				}
				else
				{
					SetApiKeyForTarget(settings, _currentApiSetupTarget, text2);
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiKeyDisplayName() + " 已写入 MCM。"));
					BeginFetchAvailableModelsForSetup();
				}
			}, delegate
			{
				OpenApiBaseUrlInput();
			}));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开 " + CurrentApiKeyDisplayName() + " 输入框失败：" + ex.Message));
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
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能拉取模型列表。"));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		string apiUrl = GetApiUrlForTarget(settings, _currentApiSetupTarget).Trim();
		string apiKey = GetApiKeyForTarget(settings, _currentApiSetupTarget).Trim();
		if (string.IsNullOrWhiteSpace(apiUrl))
		{
			InformationManager.DisplayMessage(new InformationMessage("请先填写 Base URL。"));
			OpenApiBaseUrlInput();
			return;
		}
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			InformationManager.DisplayMessage(new InformationMessage("请先填写 API Key。"));
			OpenApiKeyInput();
			return;
		}
		_modelFetchInProgress = true;
		int num = ++_modelFetchVersion;
		ShowModelFetchProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string text = "";
			List<string> list = new List<string>();
			CancellationTokenSource cancellationTokenSource = null;
			try
			{
				cancellationTokenSource = new CancellationTokenSource();
				_modelFetchCancellation = cancellationTokenSource;
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
						text = "已成功拉取可用模型列表，请选择模型名称。";
					}
					else
					{
						text = LlmRetryPrompt.BuildFailureDetail("接口已返回响应，但没有识别出可用模型列表。你也可以手动输入模型名称。", "", text2);
					}
				}
				else if (exchange.HasStatusCode)
				{
					text = BuildModelFetchFailureMessage((HttpStatusCode)exchange.StatusCode, text2);
				}
				else
				{
					text = "拉取模型列表失败：" + ModelCatalogErrorFormatter.Format(exchange.ErrorCode, exchange.ErrorArguments, legacyMessage: exchange.ErrorMessage ?? "未知错误");
				}
			}
			catch (OperationCanceledException)
			{
				text = "模型列表拉取已取消。";
			}
			catch (Exception ex)
			{
				text = "拉取模型列表失败：" + ex.Message;
			}
			finally
			{
				if (num == _modelFetchVersion)
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
			InformationManager.ShowInquiry(new InquiryData("正在拉取模型列表", "正在根据你填写的 Base URL 和 API Key 拉取当前接口可用的模型，请稍候……\n\n拉取完成后将自动进入下一步。\n如果始终无法拉取模型列表，你也可以返回上一界面重新填写，或稍后手动输入模型名称。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "退出当前存档", "返回上一界面", ExitCurrentGameFromOnboarding, CancelModelFetchAndReturnToApiKey), pauseGameActiveState: true);
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
			_modelFetchVersion++;
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
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写" + CurrentApiModelDisplayName() + "。"));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.ModelSelect;
			bool yjApiSetupActive = IsYjApiSetupActive();
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("__manual__", "手动输入模型名称", null));
			if (!yjApiSetupActive)
			{
				list.Add(new InquiryElement("__base_url__", "重新填写base URL", null));
			}
			list.Add(new InquiryElement("__api_key__", "重新填写API key", null));
			foreach (string item in _lastFetchedModelNames.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				list.Add(new InquiryElement(item, item, null));
			}
			list.Add(new InquiryElement("__refresh__", "重新拉取模型列表", null));
			list.Add(new InquiryElement("__exit__", "退出当前存档", null));
			string text = "请选择一个可用模型名称。";
			if (_lastFetchedModelNames.Count > 0)
			{
				text += "";
			}
			else
			{
				text += "\n\n当前没有拉取到模型列表，你可以手动输入模型名称。";
			}
			// 网络失败详情已显示在左下角；模型选择窗口保持紧凑，只保留当前可执行的恢复操作。
			if (yjApiSetupActive)
			{
				text = text + "\n\n【本通道模型选择建议（仅供参考）】\n" + CurrentYjApiModelRecommendation() + "\n最终选择由你自行决定。";
			}
			else
			{
				text += "\n\n如果你的base URL或API key填写错误，那你也可以将本菜单的滑条拉到最底部重新返回填写。";
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("选择模型名称", text, list, isExitShown: false, 0, 1, "下一步", "返回", delegate(List<InquiryElement> selected)
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
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiModelDisplayName() + " 已写入 MCM，正在测试完整连接：" + text2));
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
			InformationManager.DisplayMessage(new InformationMessage("打开模型选择界面失败：" + ex.Message));
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
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写" + CurrentApiModelDisplayName() + "。"));
				ReopenCurrentApiEntry(ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData("手动填写模型名称", "请输入" + CurrentApiModelDisplayName() + "。\n示例：gpt-4o-mini", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "开始测试", "返回", delegate(string input)
			{
				string text2 = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiModelDisplayName() + " 不能为空。"));
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
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiModelDisplayName() + " 已写入 MCM，正在测试完整连接：" + text2));
					BeginValidateMcmApiAndContinue(returnToModelSelection: true);
				}
			}, delegate
			{
				ShowModelSelectionPopup();
			}));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开模型名称输入框失败：" + ex.Message));
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
		int num = ++_apiValidationVersion;
		ShowApiValidationProgressPopup();
		Task.Run(async delegate
		{
			bool flag = false;
			string text = "";
			string failureHint = "";
			CancellationTokenSource cancellationTokenSource = null;
			try
			{
				cancellationTokenSource = new CancellationTokenSource();
				_apiValidationCancellation = cancellationTokenSource;
				string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(apiUrl);
				JObject requestPayload = new JObject
				{
					["model"] = modelName,
					["messages"] = new JArray
					{
						new JObject
						{
							["role"] = "user",
							["content"] = "请回复“连接成功”。"
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
							failureHint = "接口返回了 HTTP 成功状态，但响应结构中没有可用的模型回复。";
							text = LlmRetryPrompt.BuildFailureDetail("MCM 中的" + CurrentApiDisplayName() + "回复解析失败。", "", text2);
						}
						else
						{
							flag = true;
							text = "MCM 中的" + CurrentApiDisplayName() + "连接测试成功：" + text3.Trim();
						}
					}
					catch (Exception ex)
					{
						failureHint = "接口返回了 HTTP 成功状态，但响应不是可解析的聊天补全格式。";
						text = LlmRetryPrompt.BuildFailureDetail("MCM 中的" + CurrentApiDisplayName() + "回复解析失败：" + ex.Message, "", text2);
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
					failureHint = "通常是网络异常、证书或代理设置异常，或者 Base URL 填写不正确。";
					text = "MCM 中的" + CurrentApiDisplayName() + "连接测试失败：" + (exchange.ErrorMessage ?? "未知错误");
				}
			}
			catch (OperationCanceledException)
			{
				failureHint = "你已手动取消本次测试，可以返回上一界面重新测试，或改填 API 信息。";
				text = "测试已取消，已退回上一界面。";
			}
			catch (Exception ex)
			{
				failureHint = "通常是网络异常、证书或代理设置异常，或者 Base URL 填写不正确。";
				text = CurrentApiDisplayName() + "连接测试异常：" + ex.Message;
			}
			finally
			{
				if (num == _apiValidationVersion)
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
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置。"));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		TryPersistMcmSettings(settings);
		string apiUrl = GetApiUrlForTarget(settings, _currentApiSetupTarget).Trim();
		string apiKey = GetApiKeyForTarget(settings, _currentApiSetupTarget).Trim();
		string modelName = GetModelNameForTarget(settings, _currentApiSetupTarget).Trim();
		if (string.IsNullOrWhiteSpace(apiUrl))
		{
			InformationManager.DisplayMessage(new InformationMessage("MCM 中尚未填写 " + CurrentApiBaseUrlDisplayName() + "。"));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			InformationManager.DisplayMessage(new InformationMessage("MCM 中尚未填写 " + CurrentApiKeyDisplayName() + "。"));
			ReopenCurrentApiEntry(ignoreSuppress: true);
			return;
		}
		if (string.IsNullOrWhiteSpace(modelName))
		{
			InformationManager.DisplayMessage(new InformationMessage("MCM 中尚未填写" + CurrentApiModelDisplayName() + "。"));
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
				? "正在按当前 MCM 配置并发测试 API 组合，请稍候……\n\n测试完成后将自动进入下一步；任意必测 API 不通过都会返回首次菜单。"
				: "正在使用 MCM 中的" + CurrentApiDisplayName() + "信息进行连接测试，请稍候……\n\n测试完成后将自动进入下一步。\n如果你的 API 测试始终未成功，你也可以在此界面直接退出存档。";
			InformationManager.ShowInquiry(new InquiryData("正在测试现有配置", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "退出当前存档", "返回上一界面", ExitCurrentGameFromOnboarding, CancelApiValidationAndReturnToWelcome), pauseGameActiveState: true);
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
			_apiValidationVersion++;
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
			_pendingWelcome = false;
			_pendingReturnToWelcome = false;
			_pendingApiValidationResult = false;
			_pendingApiValidationVersion = 0;
			_pendingApiValidationSuccess = false;
			_pendingApiValidationMessage = "";
			_pendingApiValidationFailureHint = "";
			_apiValidationFlow = ApiValidationFlow.Normal;
			_apiValidationReturnToModelSelection = false;
			_pendingBaseUrlValidationResult = false;
			_pendingBaseUrlValidationSuccess = false;
			_pendingBaseUrlValidationMessage = "";
			_pendingValidatedBaseUrl = "";
			_lastBaseUrlValidationFailureMessage = "";
			_pendingModelFetchResult = false;
			_pendingModelFetchVersion = 0;
			_pendingModelFetchSuccess = false;
			_pendingModelFetchMessage = "";
			_pendingModelFetchModels = new List<string>();
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
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
			InformationManager.DisplayMessage(new InformationMessage("退出当前存档失败：" + ex.Message));
			ReopenCurrentApiEntry(ignoreSuppress: true);
		}
	}

	private void BeginSaveAndExitCurrentGameFromOnboarding()
	{
		try
		{
			_welcomeInProgress = false;
			_activeOnboardingStage = OnboardingUiStage.None;
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			InformationManager.HideInquiry();
			SaveHandler saveHandler = Campaign.Current?.SaveHandler;
			if (saveHandler == null)
			{
				_saveAndExitStage = SaveAndExitStage.None;
				InformationManager.DisplayMessage(new InformationMessage("未找到存档保存器，将直接退出当前存档。"));
				MBGameManager.EndGame();
				return;
			}
			if (saveHandler.IsSaving)
			{
				_saveAndExitStage = SaveAndExitStage.WaitingForCurrentSave;
				InformationManager.DisplayMessage(new InformationMessage("检测到当前已有保存进行中，完成后将再保存一次并自动退出。"));
				return;
			}
			_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
			saveHandler.QuickSaveCurrentGame();
			InformationManager.DisplayMessage(new InformationMessage("正在保存当前存档，保存完成后将自动退出。"));
		}
		catch (Exception ex)
		{
			_saveAndExitStage = SaveAndExitStage.None;
			InformationManager.DisplayMessage(new InformationMessage("保存并退出失败：" + ex.Message));
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
					InformationManager.DisplayMessage(new InformationMessage("保存并退出失败：未找到存档保存器。"));
					ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
					return;
				}
				_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
				saveHandler.QuickSaveCurrentGame();
				InformationManager.DisplayMessage(new InformationMessage("正在保存当前存档，保存完成后将自动退出。"));
				return;
			}
			_saveAndExitStage = SaveAndExitStage.None;
			if (isSuccessful)
			{
				MBGameManager.EndGame();
				return;
			}
			InformationManager.DisplayMessage(new InformationMessage("保存当前存档失败，已取消退出。"));
			ShowSetupModeChoicePopup(fromGate: true, ignoreSuppress: true);
		}
		catch (Exception ex)
		{
			_saveAndExitStage = SaveAndExitStage.None;
			InformationManager.DisplayMessage(new InformationMessage("保存并退出失败：" + ex.Message));
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
			string text = "此内容包括世界观、角色信息、各王国开局概括，以及国家性格和长期战略，可以极大提升游戏体验。\n\n但是一些世界观也会加大 CPU 的负担。";
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData("知识库数据导入", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "一键导入", "跳过", delegate
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
			string text = "你确定不载入数据库吗？\n不载入数据库，NPC将对您当前世界的设定几乎完全不理解。";
			InformationManager.ShowInquiry(new InquiryData("跳过数据库导入", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate
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
		try
		{
			ResetYjApiSetup();
			_setupDone = true;
			_activeOnboardingStage = OnboardingUiStage.None;
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				onReturn?.Invoke();
				return;
			}
			if (importedDatabase)
			{
				InformationManager.DisplayMessage(new InformationMessage("首次导入完成：已解锁 AnimusForge 对话/场景喊话。"));
				InformationManager.DisplayMessage(new InformationMessage("接下来请填写玩家称呼、外貌与背景；这些内容也可以直接跳过。"));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过数据库导入。接下来请填写玩家称呼、外貌与背景；这些内容也可以直接跳过。"));
			}
			knowledgeLibraryBehavior.OpenPlayerPersonaSetup(delegate
			{
				ShowPeaceSceneConflictChoiceAfterPersona(delegate
				{
					try
					{
						(MyBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.QueueMissingOnnxGateCheckAfterOnboarding();
					}
					catch
					{
					}
					onReturn?.Invoke();
				});
			});
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开玩家角色介绍失败：" + ex.Message));
			onReturn?.Invoke();
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
			string text = "是否允许玩家直接攻击触发场景冲突？\n\n开启后，玩家直接攻击城镇、领主会面等和平场景内的 NPC 可以触发本模组的场景冲突。\n\n关闭后，本模组不会再把直接攻击转成场景冲突，伤害结算完全交回原版；对话中的吵架/挑衅仍然可以触发冲突升级。\n\n这个选择会同步写入 MCM，之后可在“场景喊话”中随时修改。";
			InformationManager.ShowInquiry(new InquiryData("AnimusForge - 场景冲突", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "开启", "关闭", delegate
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
				Logger.Log("ModOnboarding", "[WARN] 显示和平场景冲突设置失败：" + ex.Message);
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
					InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能保存和平场景冲突选项。"));
				}
				return;
			}
			settings.EnablePeaceSceneConflict = enabled;
			TryPersistMcmSettings(settings);
			if (showMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(enabled ? "已允许玩家直接攻击触发场景冲突。" : "已关闭直接攻击触发场景冲突；伤害结算回到原版逻辑。"));
			}
		}
		catch (Exception ex)
		{
			if (showMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage("保存和平场景冲突选项失败：" + ex.Message));
			}
		}
	}

	private static void TryPersistMcmSettings(DuelSettings settings)
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
				Logger.Log("ModOnboarding", "[WARN] 持久化 MCM 设置失败：" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static string BuildApiValidationFailureMessage(string effectiveApiUrl, string modelName, HttpStatusCode statusCode, string responseBody)
	{
		string text = "MCM 中的 API 信息连接测试失败，暂时不能进入下一步。";
		if (!string.IsNullOrWhiteSpace(effectiveApiUrl))
		{
			text = text + "\n接口：" + effectiveApiUrl;
		}
		if (!string.IsNullOrWhiteSpace(modelName))
		{
			text = text + "\n模型：" + modelName;
		}
		text = text + "\n状态码：" + statusCode;
		string text2 = responseBody ?? "";
		if ((int)statusCode == 404)
		{
			text += "\n排查建议：请检查 Base URL 尾缀和模型名称是否正确。";
		}
		else if ((int)statusCode == 401 || (int)statusCode == 403)
		{
			text += "\n排查建议：请检查 API Key 是否有效。";
		}
		else if ((int)statusCode == 522)
		{
			text += "\n排查建议：网关已收到请求，但上游源站不可达。";
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
			return "请求格式不符合接口要求，通常是 Base URL 尾缀不对，或当前接口并不兼容聊天补全请求格式。";
		case 401:
			return "API Key 无效、为空，或鉴权格式不正确。";
		case 402:
			return "账号额度不足、套餐受限，或当前渠道要求先充值后才能调用模型。";
		case 403:
			return "当前 Key 没有访问该模型或该接口的权限，也可能被平台风控拦截。";
		case 404:
			return "Base URL 尾缀错误、接口路径不对，或模型名称在当前服务商侧不存在。";
		case 408:
			return "服务端长时间未返回，通常是网络质量较差，或上游响应过慢。";
		case 429:
			if (text2.Contains("quota") || text2.Contains("balance") || text2.Contains("insufficient"))
			{
				return "账号额度可能已经用尽，或账户余额不足，导致请求被限流或拒绝。";
			}
			return "请求过于频繁、并发超限，或账号触发了速率限制。稍等片刻后再试。";
		case 500:
			return "服务端内部处理失败，通常不是本地填写错误，建议稍后重试。";
		case 502:
		case 503:
		case 504:
		case 522:
			return "网关或上游服务暂时不可用，通常是服务商侧故障，或当前网络到上游链路异常。";
		default:
			return "请优先检查 Base URL、API Key、模型名称和当前网络环境是否正确。";
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
		string text = "Base URL 检查失败。";
		switch ((int)statusCode)
		{
		case 404:
			text += " 当前地址很可能不正确，常见原因是 Base URL 尾缀或接口路径填写错误。";
			break;
		case 400:
			text += " 当前地址返回了无效请求，说明这个接口大概率不兼容当前的 OpenAI 风格地址。";
			break;
		case 500:
		case 502:
		case 503:
		case 504:
		case 522:
			text += " 当前地址暂时不可用，可能是服务端异常，或网络到服务商链路异常。";
			break;
		default:
			text += " 请检查 Base URL 是否填写正确。";
			break;
		}
		return LlmRetryPrompt.BuildFailureDetail(text, "", responseBody);
	}

	private static List<string> ExtractModelNamesFromResponse(string responseBody)
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
		string text = "拉取模型列表失败。";
		switch ((int)statusCode)
		{
		case 401:
			text += " 当前 API Key 无效，或鉴权未通过。";
			break;
		case 402:
			text += " 当前账号额度不足，或渠道限制了接口调用。";
			break;
		case 403:
			text += " 当前 Key 没有访问模型列表接口的权限，或被服务商风控拦截。";
			break;
		case 404:
			text += " 当前 Base URL 很可能不正确，或该服务商不支持 /models 接口。";
			break;
		case 429:
			text += " 当前请求过于频繁，或账户触发了速率限制。";
			break;
		default:
			text += " 你可以手动输入模型名称继续。";
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
			string playerExportsRootPath = GetPlayerExportsRootPath();
			if (!Directory.Exists(playerExportsRootPath))
			{
				InformationManager.DisplayMessage(new InformationMessage("找不到导出目录：" + playerExportsRootPath));
				onReturn();
				return;
			}
			List<string> list = (from d in new DirectoryInfo(playerExportsRootPath).GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d.Name).ToList();
			List<InquiryElement> list2 = new List<InquiryElement>();
			list2.Add(new InquiryElement("__manual__", "手动输入文件夹名", null));
			foreach (string item in list)
			{
				if (!string.IsNullOrWhiteSpace(item))
				{
					list2.Add(new InquiryElement(item, item, null));
				}
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("选择导入文件夹", "请选择 PlayerExports 下的导出文件夹：", list2, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
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
						InformationManager.ShowTextInquiry(new TextInquiryData("手动输入文件夹名", "请输入 PlayerExports 下的文件夹名：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
						{
							string folderName2 = (input ?? "").Trim();
							if (string.IsNullOrWhiteSpace(folderName2))
							{
								InformationManager.DisplayMessage(new InformationMessage("请输入导入文件夹名，或从列表中选择一个文件夹。"));
								OpenImportFolderPicker(onReturn);
								return;
							}
							TryImportRequiredSetAndUnlock(folderName2, onReturn);
						}, delegate
						{
							OpenImportFolderPicker(onReturn);
						}));
					}
					else
					{
						TryImportRequiredSetAndUnlock(text ?? "", onReturn);
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
			InformationManager.DisplayMessage(new InformationMessage("打开导入选择失败：" + ex.Message));
			onReturn?.Invoke();
		}
	}

	private void TryImportRequiredSetAndUnlock(string folderName, Action onReturn)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("请选择要导入的 PlayerExports 文件夹。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path = Path.Combine(text, "personality_background");
			if (!Directory.Exists(path) || Directory.GetFiles(path, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 personality_background\\*.json"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path2 = Path.Combine(text, "unnamed_persona");
			if (!Directory.Exists(path2) || Directory.GetFiles(path2, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 unnamed_persona\\*.json"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			bool flag = false;
			try
			{
				string path3 = Path.Combine(text, "knowledge", "rules");
				if (Directory.Exists(path3) && Directory.GetFiles(path3, "*.json").Length != 0)
				{
					flag = true;
				}
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				string path4 = Path.Combine(text, "knowledge", "KnowledgeRules.json");
				if (File.Exists(path4))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 knowledge\\rules\\*.json（或 knowledge\\KnowledgeRules.json）"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path5 = Path.Combine(text, "voice_mapping", "VoiceMapping.json");
			string path6 = Path.Combine(text, "VoiceMapping.json");
			if (!File.Exists(path5) && !File.Exists(path6))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 voice_mapping\\VoiceMapping.json。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path7 = Path.Combine(text, "event_data", "WorldOpeningSummary.json");
			string path8 = Path.Combine(text, "event_data", "KingdomOpeningSummaries.json");
			if (!File.Exists(path7) || !File.Exists(path8))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 event_data\\WorldOpeningSummary.json 或 event_data\\KingdomOpeningSummaries.json。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string kingdomProfilesPath = Path.Combine(text, "kingdom_profiles", "KingdomProfiles.json");
			if (!File.Exists(kingdomProfilesPath))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 kingdom_profiles\\KingdomProfiles.json。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			KingdomStrategicProfileBehavior kingdomProfileBehavior = Campaign.Current?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
			if (kingdomProfileBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：国家战略与性格数据行为未初始化。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			if (!kingdomProfileBehavior.InspectImportDirectory(text, out int kingdomProfileTotalCount, out _, out int kingdomProfileSkippedCount, out string kingdomProfileInspectError)
				|| kingdomProfileTotalCount <= 0)
			{
				string reason = string.IsNullOrWhiteSpace(kingdomProfileInspectError)
					? "资料包中没有与当前世界安全匹配的国家卡。"
					: kingdomProfileInspectError;
				InformationManager.DisplayMessage(new InformationMessage("导入失败：国家战略与性格资料无效。原因：" + reason));
				OpenImportFolderPicker(onReturn);
				return;
			}
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：MyBehavior 未初始化。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportPersonaData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 Hero 个性/背景导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportUnnamedPersonaData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 非Hero 描述导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportKnowledgeData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 知识导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportVoiceMappingData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 声音映射导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportEventData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 事件库导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!kingdomProfileBehavior.ImportAllFromDirectory(text, overwriteExisting: true, out string kingdomProfileImportDetail))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行国家战略与性格导入。原因：" + kingdomProfileImportDetail));
				OpenImportFolderPicker(onReturn);
			}
			else if (!HasLoadedVoiceMapping())
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：声音映射未成功载入到当前存档。"));
				OpenImportFolderPicker(onReturn);
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("国家战略与性格已从资料包导入：匹配 " + kingdomProfileTotalCount + " 条；无法匹配 " + kingdomProfileSkippedCount + " 条。"));
				CompleteOnboardingAndOpenPlayerPersonaSetup(onReturn, importedDatabase: true);
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
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

	private static string GetModuleRootPath()
	{
		try
		{
			string location = typeof(SubModule).Assembly.Location;
			string text = (string.IsNullOrEmpty(location) ? "" : Path.GetDirectoryName(Path.GetFullPath(location)));
			DirectoryInfo directoryInfo = (string.IsNullOrEmpty(text) ? null : new DirectoryInfo(text));
			while (directoryInfo != null && directoryInfo.Exists)
			{
				if (File.Exists(Path.Combine(directoryInfo.FullName, "SubModule.xml")))
				{
					return directoryInfo.FullName;
				}
				directoryInfo = directoryInfo.Parent;
			}
		}
		catch
		{
		}
		try
		{
			return Path.GetFullPath(Directory.GetCurrentDirectory());
		}
		catch
		{
			return "";
		}
	}

	private static string GetPlayerExportsRootPath()
	{
		string moduleRootPath = GetModuleRootPath();
		return Path.Combine(moduleRootPath, "PlayerExports");
	}

	private static string GetModuleVersionText()
	{
		try
		{
			string path = Path.Combine(GetModuleRootPath(), "SubModule.xml");
			if (!File.Exists(path))
			{
				return "未知版本";
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
		return "未知版本";
	}

	private static string SanitizeFolderName(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text = text.Replace(oldChar, '_');
		}
		return text.Trim().TrimEnd('.');
	}

	private static string ResolveImportFolderPath(string folderName)
	{
		string playerExportsRootPath = GetPlayerExportsRootPath();
		string text = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		return Path.Combine(playerExportsRootPath, text);
	}
}
