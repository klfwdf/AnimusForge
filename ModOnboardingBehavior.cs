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
		Welcome,
		AuxiliaryChoice,
		PostprocessChoice,
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
		ActionPostprocess
	}

	private const string SetupDoneKey = "_AnimusForge_setup_done_v1";

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

	private bool _pendingApiValidationSuccess;

	private string _pendingApiValidationMessage = "";

	private string _pendingApiValidationFailureHint = "";

	private bool _showApiValidationFailedHint;

	private string _lastApiValidationFailureHint = "";

	private bool _apiValidationReturnToModelSelection;

	private bool _showModelSelectionValidationFailedHint;

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

	private bool _pendingModelFetchSuccess;

	private string _pendingModelFetchMessage = "";

	private List<string> _pendingModelFetchModels = new List<string>();

	private string _lastModelFetchMessage = "";

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
				ShowWelcomePopup(fromGate: false);
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
		return "主模型名称";
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
		else
		{
			settings.ApiKey = value ?? "";
		}
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
		}
		else if (target == ApiSetupTarget.ActionPostprocess)
		{
			settings.ActionPostprocessModelName = value ?? "";
		}
		else
		{
			settings.ModelName = value ?? "";
		}
	}

	private void ReopenCurrentApiEntry(bool ignoreSuppress = true)
	{
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
		ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
	}

	private void ProcessUnexpectedOnboardingDismissal()
	{
		if (_setupDone || _pendingReturnToWelcome || _pendingBaseUrlValidationResult || _pendingApiValidationResult || _pendingModelFetchResult)
		{
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			return;
		}
		if (_activeOnboardingStage != OnboardingUiStage.Welcome && _activeOnboardingStage != OnboardingUiStage.AuxiliaryChoice && _activeOnboardingStage != OnboardingUiStage.PostprocessChoice && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidation && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidationFailure && _activeOnboardingStage != OnboardingUiStage.ApiValidation && _activeOnboardingStage != OnboardingUiStage.ModelFetch && _activeOnboardingStage != OnboardingUiStage.ModelSelect && _activeOnboardingStage != OnboardingUiStage.Import)
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
		case OnboardingUiStage.AuxiliaryChoice:
			ShowAuxiliaryApiSetupPopup(ignoreSuppress: true);
			break;
		case OnboardingUiStage.PostprocessChoice:
			ShowActionPostprocessApiSetupPopup(ignoreSuppress: true, allowWhenSetupDone: true);
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
		bool pendingModelFetchSuccess = _pendingModelFetchSuccess;
		string pendingModelFetchMessage = _pendingModelFetchMessage ?? "";
		List<string> list = _pendingModelFetchModels?.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
		_pendingModelFetchResult = false;
		_pendingModelFetchSuccess = false;
		_pendingModelFetchMessage = "";
		_pendingModelFetchModels = new List<string>();
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		InformationManager.HideInquiry();
		_showModelSelectionValidationFailedHint = false;
		_lastFetchedModelNames = list;
		_lastModelFetchMessage = pendingModelFetchMessage;
		if (!pendingModelFetchSuccess && !string.IsNullOrWhiteSpace(pendingModelFetchMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(pendingModelFetchMessage));
		}
		ShowModelSelectionPopup();
	}

	private void ProcessPendingApiValidationResult()
	{
		if (!_pendingApiValidationResult)
		{
			return;
		}
		bool pendingApiValidationSuccess = _pendingApiValidationSuccess;
		string pendingApiValidationMessage = _pendingApiValidationMessage ?? "";
		string pendingApiValidationFailureHint = _pendingApiValidationFailureHint ?? "";
		_pendingApiValidationResult = false;
		_pendingApiValidationSuccess = false;
		_pendingApiValidationMessage = "";
		_pendingApiValidationFailureHint = "";
		bool apiValidationReturnToModelSelection = _apiValidationReturnToModelSelection;
		_apiValidationReturnToModelSelection = false;
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		InformationManager.HideInquiry();
		if (pendingApiValidationSuccess && !string.IsNullOrWhiteSpace(pendingApiValidationMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(pendingApiValidationMessage));
		}
		if (pendingApiValidationSuccess)
		{
			_showApiValidationFailedHint = false;
			_showModelSelectionValidationFailedHint = false;
			_lastApiValidationFailureHint = "";
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
				ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
			}
			else if (IsAuxiliaryApiSetupTarget())
			{
				ShowActionPostprocessApiSetupPopup(ignoreSuppress: true);
			}
			else if (!_setupDone)
			{
				ShowAuxiliaryApiSetupPopup(ignoreSuppress: true);
			}
			else
			{
				ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
			}
		}
		else if (_apiRepairFlowActive || !_setupDone)
		{
			if (apiValidationReturnToModelSelection)
			{
				_showApiValidationFailedHint = false;
				_showModelSelectionValidationFailedHint = true;
				_lastApiValidationFailureHint = pendingApiValidationFailureHint;
				ShowModelSelectionPopup();
			}
			else
			{
				_showApiValidationFailedHint = true;
				_showModelSelectionValidationFailedHint = false;
				_lastApiValidationFailureHint = pendingApiValidationFailureHint;
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
		modOnboardingBehavior.ShowWelcomePopup(fromGate: true);
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

	private void ShowWelcomePopup(bool fromGate)
	{
		ShowWelcomePopup(fromGate, ignoreSuppress: false);
	}

	private void ShowApiRepairPopup()
	{
		try
		{
			SetApiSetupTarget(ApiSetupTarget.Primary);
			SetApiRepairFlowActive(active: true);
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			_activeOnboardingStage = OnboardingUiStage.Welcome;
			_welcomeInProgress = true;
			string text = "周事件自动生成失败，请检查你的 Base URL、API Key、模型名或当前网络环境。";
			if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
			{
				text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
			}
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
			SetApiSetupTarget(ApiSetupTarget.Auxiliary);
			SetApiRepairFlowActive(active: true);
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			_activeOnboardingStage = OnboardingUiStage.AuxiliaryChoice;
			_welcomeInProgress = true;
			DuelSettings settings = DuelSettings.GetSettings();
			bool hasExistingConfig = HasCompleteApiConfigForTarget(settings, ApiSetupTarget.Auxiliary);
			string text = hasExistingConfig
				? "前处理API（规则检索/规则路由）当前不可用。你可以直接测试 MCM 中的现有配置，也可以重新填写前处理API信息。"
				: "前处理API（规则检索/规则路由）当前不可用，请检查前处理API的 Base URL、API Key、模型名称，或当前网络环境。";
			if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
			{
				text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
			}
			string negativeText = hasExistingConfig ? "测试现有配置" : "回退回RAG检索";
			InformationManager.ShowInquiry(new InquiryData("调整前处理API信息", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写前处理API信息", negativeText, delegate
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
					DuelSettings settings2 = DuelSettings.GetSettings();
					if (settings2 != null)
					{
						settings2.UseAuxiliaryRuleApi = false;
						TryPersistMcmSettings(settings2);
					}
					ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
				}
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
			SetApiSetupTarget(ApiSetupTarget.ActionPostprocess);
			SetApiRepairFlowActive(active: true);
			if (_welcomeInProgress || _apiValidationInProgress || _baseUrlValidationInProgress || _modelFetchInProgress)
			{
				return;
			}
			_activeOnboardingStage = OnboardingUiStage.PostprocessChoice;
			_welcomeInProgress = true;
			DuelSettings settings = DuelSettings.GetSettings();
			bool hasExistingConfig = HasCompleteApiConfigForTarget(settings, ApiSetupTarget.ActionPostprocess);
			string text = hasExistingConfig
				? "后处理API当前不可用。你可以直接测试 MCM 中的现有配置，也可以重新填写后处理API信息。\n\n后处理任务对判定稳定性要求较高，建议优先选择带思考模式的模型，或直接使用更高级模型。"
				: "后处理API当前不可用。你可以重新填写后处理API信息，或继续回退使用主API处理后处理任务。\n\n后处理任务对判定稳定性要求较高，建议优先选择带思考模式的模型，或直接使用更高级模型。";
			if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
			{
				text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
			}
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

	private void ShowWelcomePopup(bool fromGate, bool ignoreSuppress)
	{
		try
		{
			SetApiSetupTarget(ApiSetupTarget.Primary);
			SetApiRepairFlowActive(active: false);
			if (_setupDone || _welcomeInProgress || _apiValidationInProgress)
			{
				return;
			}
			long ticks = DateTime.UtcNow.Ticks;
			if (!ignoreSuppress && _suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(fromGate ? 800 : 200).Ticks;
			_activeOnboardingStage = OnboardingUiStage.Welcome;
			string title = "欢迎使用 AnimusForge";
			string text = "开始游玩前，请先确认主API信息。主API用于NPC正文生成，如果未正确配置，AI 对话功能将无法使用。";
			if (_showApiValidationFailedHint)
			{
				title = "主API连接失败";
				text = "主API测试连接失败，请检查你的网络环境，或者重新填写主API信息。";
				if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
				{
					text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
				}
			}
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
			SetApiSetupTarget(ApiSetupTarget.Auxiliary);
			SetApiRepairFlowActive(active: false);
			if (_setupDone || _welcomeInProgress || _apiValidationInProgress)
			{
				return;
			}
			long ticks = DateTime.UtcNow.Ticks;
			if (!ignoreSuppress && _suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(250.0).Ticks;
			_activeOnboardingStage = OnboardingUiStage.AuxiliaryChoice;
			_welcomeInProgress = true;
			DuelSettings settings = DuelSettings.GetSettings();
			bool hasExistingConfig = HasCompleteApiConfigForTarget(settings, ApiSetupTarget.Auxiliary);
			string title = "配置前处理API";
			string text = "前处理API专门用于规则检索/规则路由。启用后，规则话题会先走一次低成本筛选，再进入主API正文生成；如果你暂时不想配置，也可以继续使用传统RAG检索。";
			if (_showApiValidationFailedHint)
			{
				title = "前处理API连接失败";
				text = hasExistingConfig
					? "刚才的前处理API连接测试没有通过。你可以重新填写前处理API信息，或者再次测试 MCM 中的现有配置。"
					: "刚才的前处理API连接测试没有通过。你可以重新填写前处理API信息，或者先继续使用传统RAG检索。";
				if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
				{
					text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
				}
			}
			string negativeText = hasExistingConfig ? "测试现有配置" : "回退回RAG检索";
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写前处理API", negativeText, delegate
			{
				_welcomeInProgress = false;
				_showApiValidationFailedHint = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				_showApiValidationFailedHint = false;
				if (hasExistingConfig)
				{
					BeginValidateMcmApiAndContinue();
				}
				else
				{
					DuelSettings settings2 = DuelSettings.GetSettings();
					if (settings2 != null)
					{
						settings2.UseAuxiliaryRuleApi = false;
						TryPersistMcmSettings(settings2);
					}
					ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
				}
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
			SetApiSetupTarget(ApiSetupTarget.ActionPostprocess);
			SetApiRepairFlowActive(active: false);
			if ((!allowWhenSetupDone && _setupDone) || _welcomeInProgress || _apiValidationInProgress)
			{
				return;
			}
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
			if (_showApiValidationFailedHint)
			{
				title = "后处理API连接失败";
				text = hasExistingConfig
					? "刚才的后处理API连接测试没有通过。你可以重新填写后处理API信息，或者再次测试 MCM 中的现有配置。\n\n后处理任务对判定稳定性要求较高，建议优先选择带思考模式的模型，或直接使用更高级模型。"
					: "刚才的后处理API连接测试没有通过。你可以重新填写后处理API信息，或者先继续使用主API处理后处理任务。\n\n后处理任务对判定稳定性要求较高，建议优先选择带思考模式的模型，或直接使用更高级模型。";
				if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
				{
					text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
				}
			}
			string negativeText = hasExistingConfig ? "测试现有配置" : "继续使用主API";
			InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "填写后处理API", negativeText, delegate
			{
				_welcomeInProgress = false;
				_showApiValidationFailedHint = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
				_showApiValidationFailedHint = false;
				if (hasExistingConfig)
				{
					BeginValidateMcmApiAndContinue();
				}
				else
				{
					if (!_setupDone)
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
				string modelsApiUrl = BuildModelsApiUrl(validatedBaseUrl);
				using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, modelsApiUrl);
				HttpResponseMessage httpResponseMessage = await DuelSettings.GlobalClient.SendAsync(request, cancellationTokenSource.Token);
				string text2 = await httpResponseMessage.Content.ReadAsStringAsync();
				if (CanUseBaseUrlStatusCode(httpResponseMessage.StatusCode))
				{
					flag = true;
					message = "Base URL 检查通过，可以继续填写 API Key。";
				}
				else
				{
					message = BuildBaseUrlValidationFailureMessage(httpResponseMessage.StatusCode, text2);
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
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.BaseUrlValidationFailure;
			string text = string.IsNullOrWhiteSpace(_lastBaseUrlValidationFailureMessage) ? "你填写的 Base URL 当前不可用，请重新检查后再试。" : _lastBaseUrlValidationFailureMessage;
			InformationManager.ShowInquiry(new InquiryData("base URL 检查失败", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "重新填写base URL", "退出当前存档", delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, delegate
			{
				_welcomeInProgress = false;
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
				string modelsApiUrl = BuildModelsApiUrl(apiUrl);
				using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, modelsApiUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
				HttpResponseMessage httpResponseMessage = await DuelSettings.GlobalClient.SendAsync(request, cancellationTokenSource.Token);
				string text2 = await httpResponseMessage.Content.ReadAsStringAsync();
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					list = ExtractModelNamesFromResponse(text2);
					if (list.Count > 0)
					{
						flag = true;
						text = "已成功拉取可用模型列表，请选择模型名称。";
					}
					else
					{
						text = "接口已返回响应，但没有识别出可用模型列表。你也可以手动输入模型名称。";
					}
				}
				else
				{
					text = BuildModelFetchFailureMessage(httpResponseMessage.StatusCode, text2);
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
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("__manual__", "手动输入模型名称", null));
			list.Add(new InquiryElement("__base_url__", "重新填写base URL", null));
			list.Add(new InquiryElement("__api_key__", "重新填写API key", null));
			foreach (string item in _lastFetchedModelNames.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				list.Add(new InquiryElement(item, item, null));
			}
			list.Add(new InquiryElement("__refresh__", "重新拉取模型列表", null));
			list.Add(new InquiryElement("__exit__", "退出当前存档", null));
			string title = "选择模型名称";
			string text = "请选择一个可用模型名称。";
			if (_showModelSelectionValidationFailedHint)
			{
				title = "API 连接失败";
				text = "请重新检查base URL，API key，或模型。";
				if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
				{
					text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
				}
			}
			if (_lastFetchedModelNames.Count > 0)
			{
				text += "\n\n已从当前接口拉取到可用模型列表，你也可以选择手动输入。";
			}
			else
			{
				text += "\n\n当前没有拉取到模型列表，你可以手动输入模型名称。";
			}
			if (!string.IsNullOrWhiteSpace(_lastModelFetchMessage))
			{
				text = text + "\n\n提示：" + _lastModelFetchMessage;
			}
			text += "\n\n如果你的base URL或API key填写错误，那你也可以将本菜单的滑条拉到最底部重新返回填写。";
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, list, isExitShown: false, 0, 1, "下一步", "返回", delegate(List<InquiryElement> selected)
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
					_showModelSelectionValidationFailedHint = false;
					OpenApiBaseUrlInput();
				}
				else if (text2 == "__api_key__")
				{
					_showModelSelectionValidationFailedHint = false;
					OpenApiKeyInput();
				}
				else if (text2 == "__exit__")
				{
					_showModelSelectionValidationFailedHint = false;
					ExitCurrentGameFromOnboarding();
				}
				else
				{
					_showModelSelectionValidationFailedHint = false;
					SetModelNameForTarget(settings, _currentApiSetupTarget, text2);
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage(CurrentApiModelDisplayName() + " 已写入 MCM，正在测试完整连接：" + text2));
					BeginValidateMcmApiAndContinue(returnToModelSelection: true);
				}
			}, delegate
			{
				_welcomeInProgress = false;
				_showModelSelectionValidationFailedHint = false;
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
					_showModelSelectionValidationFailedHint = false;
					SetModelNameForTarget(settings, _currentApiSetupTarget, text2);
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

	private void BeginValidateMcmApiAndContinueCore(string apiUrl, string apiKey, string modelName, bool returnToModelSelection)
	{
		_apiValidationReturnToModelSelection = returnToModelSelection;
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
				string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(apiUrl);
				var value = new
				{
					model = modelName,
					messages = new[]
					{
						new
						{
							role = "user",
							content = "请回复“连接成功”。"
						}
					},
					stream = false
				};
				string jsonBody = JsonConvert.SerializeObject(value);
				StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
				DuelSettings.GlobalClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
				HttpResponseMessage httpResponseMessage = await DuelSettings.GlobalClient.PostAsync(effectiveApiUrl, (HttpContent)(object)content, cancellationTokenSource.Token);
				string text2 = await httpResponseMessage.Content.ReadAsStringAsync();
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					flag = true;
					try
					{
						JObject jObject = JObject.Parse(text2);
						string text3 = jObject["choices"]?[0]?["message"]?["content"]?.ToString();
						text = string.IsNullOrWhiteSpace(text3) ? ("MCM 中的" + CurrentApiDisplayName() + "连接测试成功，可以进入下一步。") : ("MCM 中的" + CurrentApiDisplayName() + "连接测试成功：" + text3.Trim());
					}
					catch
					{
						text = "MCM 中的" + CurrentApiDisplayName() + "连接测试成功，可以进入下一步。";
					}
				}
				else
				{
					failureHint = BuildApiValidationFailureHint(httpResponseMessage.StatusCode, text2);
					text = BuildApiValidationFailureMessage(effectiveApiUrl, modelName, httpResponseMessage.StatusCode, text2);
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
			InformationManager.ShowInquiry(new InquiryData("正在测试现有配置", "正在使用 MCM 中的" + CurrentApiDisplayName() + "信息进行连接测试，请稍候……\n\n测试完成后将自动进入下一步。\n如果你的 API 测试始终未成功，你也可以在此界面直接退出存档。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "退出当前存档", "返回上一界面", ExitCurrentGameFromOnboarding, CancelApiValidationAndReturnToWelcome), pauseGameActiveState: true);
		}
		catch
		{
		}
	}

	private void CancelApiValidationAndReturnToWelcome()
	{
		CancelApiValidationCore();
		if (_apiValidationReturnToModelSelection)
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
			_pendingWelcome = false;
			_pendingReturnToWelcome = false;
			_pendingApiValidationResult = false;
			_pendingApiValidationSuccess = false;
			_pendingApiValidationMessage = "";
			_pendingApiValidationFailureHint = "";
			_pendingBaseUrlValidationResult = false;
			_pendingBaseUrlValidationSuccess = false;
			_pendingBaseUrlValidationMessage = "";
			_pendingValidatedBaseUrl = "";
			_lastBaseUrlValidationFailureMessage = "";
			_pendingModelFetchResult = false;
			_pendingModelFetchSuccess = false;
			_pendingModelFetchMessage = "";
			_pendingModelFetchModels = new List<string>();
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			_welcomeInProgress = false;
			_apiRepairFlowActive = false;
			_currentApiSetupTarget = ApiSetupTarget.Primary;
			_showApiValidationFailedHint = false;
			_lastApiValidationFailureHint = "";
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
			string text = "首次在此存档中使用 AnimusForge。\n\n你现在可以导入编辑器导出的 JSON 数据，或先跳过这一步，直接继续填写角色信息。\n\n首次导入会同时载入：人物个性、未命名NPC、知识库、声音映射，以及事件库（开局概要/事件记录）。";
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData("AnimusForge - 首次使用", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "一键导入", "跳过", delegate
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
				InformationManager.DisplayMessage(new InformationMessage("接下来请填写玩家称呼与角色介绍；角色介绍也可以直接跳过。"));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过数据库导入。接下来请填写玩家称呼与角色介绍；角色介绍也可以直接跳过。"));
			}
			knowledgeLibraryBehavior.OpenPlayerPersonaSetup(delegate
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
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开玩家角色介绍失败：" + ex.Message));
			onReturn?.Invoke();
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
		string text2 = (responseBody ?? "").Trim();
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
		if (!string.IsNullOrWhiteSpace(text2))
		{
			if (text2.Length > 220)
			{
				text2 = text2.Substring(0, 220).Trim();
			}
			text = text + "\n返回：" + text2;
		}
		return text;
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

	private static string BuildModelsApiUrl(string rawUrl)
	{
		string text = (rawUrl ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = text.TrimEnd('/');
		if (text.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(0, text.Length - "/chat/completions".Length);
		}
		else if (text.EndsWith("/completions", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(0, text.Length - "/completions".Length);
		}
		return text.TrimEnd('/') + "/models";
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
		string text2 = (responseBody ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120).Trim();
			}
			text = text + " 返回摘要：" + text2;
		}
		return text;
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
		string text2 = (responseBody ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120).Trim();
			}
			text = text + " 返回摘要：" + text2;
		}
		return text;
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
			list2.Add(new InquiryElement("__latest__", "自动选择最新导出（推荐）", null));
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
						InformationManager.ShowTextInquiry(new TextInquiryData("手动输入文件夹名", "请输入 PlayerExports 下的文件夹名（留空=最新）：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
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
						string folderName = ((text == "__latest__") ? "" : (text ?? ""));
						TryImportRequiredSetAndUnlock(folderName, onReturn);
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
			else if (!HasLoadedVoiceMapping())
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：声音映射未成功载入到当前存档。"));
				OpenImportFolderPicker(onReturn);
			}
			else
			{
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
		return text.Trim();
	}

	private static string FindLatestExportFolder(string root)
	{
		try
		{
			if (!Directory.Exists(root))
			{
				return null;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(root);
			return (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).FirstOrDefault()?.FullName;
		}
		catch
		{
			return null;
		}
	}

	private static string ResolveImportFolderPath(string folderName)
	{
		string playerExportsRootPath = GetPlayerExportsRootPath();
		string text = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text))
		{
			return FindLatestExportFolder(playerExportsRootPath);
		}
		return Path.Combine(playerExportsRootPath, text);
	}
}
