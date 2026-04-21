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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class ModOnboardingBehavior : CampaignBehaviorBase
{
	private enum OnboardingUiStage
	{
		None,
		Welcome,
		BaseUrlValidation,
		BaseUrlValidationFailure,
		ApiValidation,
		ModelFetch,
		ModelSelect,
		Import
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

	public static ModOnboardingBehavior Instance { get; private set; }

	public ModOnboardingBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)OnGameStarted);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)OnGameStarted);
		CampaignEvents.TickEvent.AddNonSerializedListener((object)this, (Action<float>)OnTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData<bool>("_AnimusForge_setup_done_v1", ref _setupDone);
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

	private void ProcessPendingReturnToWelcome()
	{
		if (_pendingReturnToWelcome)
		{
			_pendingReturnToWelcome = false;
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void ProcessUnexpectedOnboardingDismissal()
	{
		if (_setupDone || _pendingReturnToWelcome || _pendingBaseUrlValidationResult || _pendingApiValidationResult || _pendingModelFetchResult)
		{
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
		}
		else if (_activeOnboardingStage != OnboardingUiStage.Welcome && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidation && _activeOnboardingStage != OnboardingUiStage.BaseUrlValidationFailure && _activeOnboardingStage != OnboardingUiStage.ApiValidation && _activeOnboardingStage != OnboardingUiStage.ModelFetch && _activeOnboardingStage != OnboardingUiStage.ModelSelect && _activeOnboardingStage != OnboardingUiStage.Import)
		{
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
		}
		else if (InformationManager.IsAnyInquiryActive())
		{
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
		}
		else if (_pendingUnexpectedResumeStage != _activeOnboardingStage)
		{
			_pendingUnexpectedResumeStage = _activeOnboardingStage;
			_pendingUnexpectedResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(150.0).Ticks;
		}
		else
		{
			if (DateTime.UtcNow.Ticks < _pendingUnexpectedResumeAfterUtcTicks)
			{
				return;
			}
			OnboardingUiStage pendingUnexpectedResumeStage = _pendingUnexpectedResumeStage;
			_pendingUnexpectedResumeStage = OnboardingUiStage.None;
			_welcomeInProgress = false;
			switch (pendingUnexpectedResumeStage)
			{
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
				ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
				break;
			}
		}
	}

	private void ProcessPendingBaseUrlValidationResult()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		if (!_pendingBaseUrlValidationResult)
		{
			return;
		}
		bool pendingBaseUrlValidationSuccess = _pendingBaseUrlValidationSuccess;
		string text = _pendingBaseUrlValidationMessage ?? "";
		string apiUrl = (_pendingValidatedBaseUrl ?? "").Trim();
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
			if (!string.IsNullOrWhiteSpace(text))
			{
				InformationManager.DisplayMessage(new InformationMessage(text));
			}
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能保存 Base URL。"));
				ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			}
			else
			{
				settings.ApiUrl = apiUrl;
				TryPersistMcmSettings(settings);
				OpenApiKeyInput();
			}
		}
		else
		{
			_lastBaseUrlValidationFailureMessage = text;
			ShowBaseUrlValidationFailurePopup();
		}
	}

	private void ProcessPendingModelFetchResult()
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		if (_pendingModelFetchResult)
		{
			bool pendingModelFetchSuccess = _pendingModelFetchSuccess;
			string text = _pendingModelFetchMessage ?? "";
			List<string> lastFetchedModelNames = _pendingModelFetchModels?.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
			_pendingModelFetchResult = false;
			_pendingModelFetchSuccess = false;
			_pendingModelFetchMessage = "";
			_pendingModelFetchModels = new List<string>();
			_welcomeInProgress = false;
			_activeOnboardingStage = OnboardingUiStage.None;
			InformationManager.HideInquiry();
			_showModelSelectionValidationFailedHint = false;
			_lastFetchedModelNames = lastFetchedModelNames;
			_lastModelFetchMessage = text;
			if (!pendingModelFetchSuccess && !string.IsNullOrWhiteSpace(text))
			{
				InformationManager.DisplayMessage(new InformationMessage(text));
			}
			ShowModelSelectionPopup();
		}
	}

	private void ProcessPendingApiValidationResult()
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		if (!_pendingApiValidationResult)
		{
			return;
		}
		bool pendingApiValidationSuccess = _pendingApiValidationSuccess;
		string text = _pendingApiValidationMessage ?? "";
		string lastApiValidationFailureHint = _pendingApiValidationFailureHint ?? "";
		_pendingApiValidationResult = false;
		_pendingApiValidationSuccess = false;
		_pendingApiValidationMessage = "";
		_pendingApiValidationFailureHint = "";
		bool apiValidationReturnToModelSelection = _apiValidationReturnToModelSelection;
		_apiValidationReturnToModelSelection = false;
		_welcomeInProgress = false;
		_activeOnboardingStage = OnboardingUiStage.None;
		InformationManager.HideInquiry();
		if (pendingApiValidationSuccess && !string.IsNullOrWhiteSpace(text))
		{
			InformationManager.DisplayMessage(new InformationMessage(text));
		}
		if (pendingApiValidationSuccess)
		{
			_showApiValidationFailedHint = false;
			_showModelSelectionValidationFailedHint = false;
			_lastApiValidationFailureHint = "";
			ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
		}
		else if (!_setupDone)
		{
			if (apiValidationReturnToModelSelection)
			{
				_showApiValidationFailedHint = false;
				_showModelSelectionValidationFailedHint = true;
				_lastApiValidationFailureHint = lastApiValidationFailureHint;
				ShowModelSelectionPopup();
			}
			else
			{
				_showApiValidationFailedHint = true;
				_showModelSelectionValidationFailedHint = false;
				_lastApiValidationFailureHint = lastApiValidationFailureHint;
				ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			}
		}
	}

	private static void ShowStartupNotice()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
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
		object obj = Instance;
		if (obj == null)
		{
			Campaign current = Campaign.Current;
			obj = ((current != null) ? current.GetCampaignBehavior<ModOnboardingBehavior>() : null);
		}
		ModOnboardingBehavior modOnboardingBehavior = (ModOnboardingBehavior)obj;
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
		object obj = Instance;
		if (obj == null)
		{
			Campaign current = Campaign.Current;
			obj = ((current != null) ? current.GetCampaignBehavior<ModOnboardingBehavior>() : null);
		}
		ModOnboardingBehavior modOnboardingBehavior = (ModOnboardingBehavior)obj;
		if (modOnboardingBehavior == null)
		{
			return false;
		}
		modOnboardingBehavior.ShowApiRepairPopup();
		return true;
	}

	private void ShowWelcomePopup(bool fromGate)
	{
		ShowWelcomePopup(fromGate, ignoreSuppress: false);
	}

	private void ShowApiRepairPopup()
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		try
		{
			if (!_welcomeInProgress && !_apiValidationInProgress && !_baseUrlValidationInProgress && !_modelFetchInProgress)
			{
				_activeOnboardingStage = OnboardingUiStage.Welcome;
				_welcomeInProgress = true;
				string text = "周事件自动生成失败，请检查你的 Base URL、API Key、模型名或当前网络环境。";
				if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
				{
					text = text + "\n\n排查建议：" + _lastApiValidationFailureHint;
				}
				InformationManager.ShowInquiry(new InquiryData("调整 API 信息", text, true, true, "填写 API 信息", "测试已有配置", (Action)delegate
				{
					_welcomeInProgress = false;
					OpenApiBaseUrlInput();
				}, (Action)delegate
				{
					_welcomeInProgress = false;
					BeginValidateMcmApiAndContinue();
				}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
			}
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowWelcomePopup(bool fromGate, bool ignoreSuppress)
	{
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		try
		{
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
			string text = "欢迎使用 AnimusForge";
			string text2 = "开始游玩前，请先确认 API 信息，否则AI对话功能将无法使用。";
			if (_showApiValidationFailedHint)
			{
				text = "API 连接失败";
				text2 = "测试链接报错!请检查你的网络环境，或者重新填写 API 信息！";
				if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
				{
					text2 = text2 + "\n\n排查建议：" + _lastApiValidationFailureHint;
				}
			}
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData(text, text2, true, true, "填写 API 信息", "测试已有配置", (Action)delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, (Action)delegate
			{
				_welcomeInProgress = false;
				BeginValidateMcmApiAndContinue();
			}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void OpenApiBaseUrlInput()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写 Base URL。"));
				ShowWelcomePopup(fromGate: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData("填写base URL", "请输入 Base URL。\n示例：https://api.deepseek.com/v1", true, true, "下一步", "返回", (Action<string>)delegate(string input)
			{
				//IL_0021: Unknown result type (might be due to invalid IL or missing references)
				//IL_002b: Expected O, but got Unknown
				string text = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					InformationManager.DisplayMessage(new InformationMessage("Base URL 不能为空。"));
					OpenApiBaseUrlInput();
				}
				else
				{
					BeginValidateBaseUrlAndContinue(text);
				}
			}, (Action)delegate
			{
				ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开 Base URL 输入框失败：" + ex.Message));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void BeginValidateBaseUrlAndContinue(string rawBaseUrl)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		if (_baseUrlValidationInProgress)
		{
			return;
		}
		string text = (rawBaseUrl ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			InformationManager.DisplayMessage(new InformationMessage("Base URL 不能为空。"));
			OpenApiBaseUrlInput();
			return;
		}
		if (!Uri.TryCreate(text, UriKind.Absolute, out var result) || (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps))
		{
			InformationManager.DisplayMessage(new InformationMessage("Base URL 格式不正确，请填写完整的 http/https 地址。"));
			OpenApiBaseUrlInput();
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
				string modelsApiUrl = BuildModelsApiUrl(text);
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, modelsApiUrl);
				try
				{
					HttpResponseMessage httpResponseMessage = await ((HttpMessageInvoker)DuelSettings.GlobalClient).SendAsync(request, cancellationTokenSource.Token);
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
				finally
				{
					((IDisposable)request)?.Dispose();
				}
			}
			catch (OperationCanceledException)
			{
				message = "Base URL 检查已取消。";
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				message = "Base URL 检查失败：" + ex3.Message;
			}
			finally
			{
				if (num == _baseUrlValidationVersion)
				{
					if (_baseUrlValidationCancellation == cancellationTokenSource)
					{
						_baseUrlValidationCancellation = null;
					}
					_baseUrlValidationInProgress = false;
					_pendingBaseUrlValidationSuccess = flag;
					_pendingBaseUrlValidationMessage = message ?? "";
					_pendingValidatedBaseUrl = (flag ? text : "");
					_pendingBaseUrlValidationResult = true;
				}
				cancellationTokenSource?.Dispose();
			}
		});
	}

	private void ShowBaseUrlValidationProgressPopup()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		try
		{
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.BaseUrlValidation;
			InformationManager.ShowInquiry(new InquiryData("正在检查base URL", "正在检查你填写的 Base URL 是否可用，请稍候……\n\n只有检查通过后，才可以进入下一步填写 API Key。", true, true, "退出当前存档", "返回上一界面", (Action)ExitCurrentGameFromOnboarding, (Action)CancelBaseUrlValidationAndReturn, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
		}
	}

	private void ShowBaseUrlValidationFailurePopup()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		try
		{
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.BaseUrlValidationFailure;
			string text = (string.IsNullOrWhiteSpace(_lastBaseUrlValidationFailureMessage) ? "你填写的 Base URL 当前不可用，请重新检查后再试。" : _lastBaseUrlValidationFailureMessage);
			InformationManager.ShowInquiry(new InquiryData("base URL 检查失败", text, true, true, "重新填写base URL", "退出当前存档", (Action)delegate
			{
				_welcomeInProgress = false;
				OpenApiBaseUrlInput();
			}, (Action)delegate
			{
				_welcomeInProgress = false;
				ExitCurrentGameFromOnboarding();
			}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
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
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写 API Key。"));
				ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData("填写API Key", "请输入 API Key。", true, true, "下一步", "返回", (Action<string>)delegate(string input)
			{
				//IL_005a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0064: Expected O, but got Unknown
				//IL_0021: Unknown result type (might be due to invalid IL or missing references)
				//IL_002b: Expected O, but got Unknown
				string text = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					InformationManager.DisplayMessage(new InformationMessage("API Key 不能为空。"));
					OpenApiKeyInput();
				}
				else
				{
					settings.ApiKey = text;
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage("API Key 已写入 MCM。"));
					BeginFetchAvailableModelsForSetup();
				}
			}, (Action)delegate
			{
				OpenApiBaseUrlInput();
			}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开 API Key 输入框失败：" + ex.Message));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void BeginFetchAvailableModelsForSetup()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		if (_modelFetchInProgress)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能拉取模型列表。"));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
		string apiUrl = (settings.ApiUrl ?? "").Trim();
		string apiKey = (settings.ApiKey ?? "").Trim();
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
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, modelsApiUrl);
				try
				{
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
					HttpResponseMessage httpResponseMessage = await ((HttpMessageInvoker)DuelSettings.GlobalClient).SendAsync(request, cancellationTokenSource.Token);
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
				finally
				{
					((IDisposable)request)?.Dispose();
				}
			}
			catch (OperationCanceledException)
			{
				text = "模型列表拉取已取消。";
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				text = "拉取模型列表失败：" + ex3.Message;
			}
			finally
			{
				if (num == _modelFetchVersion)
				{
					if (_modelFetchCancellation == cancellationTokenSource)
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
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		try
		{
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.ModelFetch;
			InformationManager.ShowInquiry(new InquiryData("正在拉取模型列表", "正在根据你填写的 Base URL 和 API Key 拉取当前接口可用的模型，请稍候……\n\n拉取完成后将自动进入下一步。\n如果始终无法拉取模型列表，你也可以返回上一界面重新填写，或稍后手动输入模型名称。", true, true, "退出当前存档", "返回上一界面", (Action)ExitCurrentGameFromOnboarding, (Action)CancelModelFetchAndReturnToApiKey, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
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
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Expected O, but got Unknown
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写模型名称。"));
				ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.ModelSelect;
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement((object)"__manual__", "手动输入模型名称", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"__base_url__", "重新填写base URL", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"__api_key__", "重新填写API key", (ImageIdentifier)null));
			foreach (string item in _lastFetchedModelNames.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				list.Add(new InquiryElement((object)item, item, (ImageIdentifier)null));
			}
			list.Add(new InquiryElement((object)"__refresh__", "重新拉取模型列表", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"__exit__", "退出当前存档", (ImageIdentifier)null));
			string text = "选择模型名称";
			string text2 = "请选择一个可用模型名称。";
			if (_showModelSelectionValidationFailedHint)
			{
				text = "API 连接失败";
				text2 = "请重新检查base URL，API key，或模型。";
				if (!string.IsNullOrWhiteSpace(_lastApiValidationFailureHint))
				{
					text2 = text2 + "\n\n排查建议：" + _lastApiValidationFailureHint;
				}
			}
			text2 = ((_lastFetchedModelNames.Count <= 0) ? (text2 + "\n\n当前没有拉取到模型列表，你可以手动输入模型名称。") : (text2 + "\n\n已从当前接口拉取到可用模型列表，你也可以选择手动输入。"));
			if (!string.IsNullOrWhiteSpace(_lastModelFetchMessage))
			{
				text2 = text2 + "\n\n提示：" + _lastModelFetchMessage;
			}
			text2 += "\n\n如果你的base URL或API key填写错误，那你也可以将本菜单的滑条拉到最底部重新返回填写。";
			MultiSelectionInquiryData val = new MultiSelectionInquiryData(text, text2, list, false, 0, 1, "下一步", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
			{
				//IL_0147: Unknown result type (might be due to invalid IL or missing references)
				//IL_0151: Expected O, but got Unknown
				_welcomeInProgress = false;
				string text3 = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrWhiteSpace(text3))
				{
					ShowModelSelectionPopup();
				}
				else
				{
					switch (text3)
					{
					case "__manual__":
						OpenManualModelNameInput();
						break;
					case "__refresh__":
						BeginFetchAvailableModelsForSetup();
						break;
					case "__base_url__":
						_showModelSelectionValidationFailedHint = false;
						OpenApiBaseUrlInput();
						break;
					case "__api_key__":
						_showModelSelectionValidationFailedHint = false;
						OpenApiKeyInput();
						break;
					case "__exit__":
						_showModelSelectionValidationFailedHint = false;
						ExitCurrentGameFromOnboarding();
						break;
					default:
						_showModelSelectionValidationFailedHint = false;
						settings.ModelName = text3;
						TryPersistMcmSettings(settings);
						InformationManager.DisplayMessage(new InformationMessage("模型名称已写入 MCM，正在测试完整连接：" + text3));
						BeginValidateMcmApiAndContinue(returnToModelSelection: true);
						break;
					}
				}
			}, (Action<List<InquiryElement>>)delegate
			{
				_welcomeInProgress = false;
				_showModelSelectionValidationFailedHint = false;
				OpenApiKeyInput();
			}, "", false);
			MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开模型选择界面失败：" + ex.Message));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void OpenManualModelNameInput()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能填写模型名称。"));
				ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
				return;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData("手动填写模型名称", "请输入模型名称。\n示例：deepseek-chat", true, true, "开始测试", "返回", (Action<string>)delegate(string input)
			{
				//IL_006c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0076: Expected O, but got Unknown
				//IL_0021: Unknown result type (might be due to invalid IL or missing references)
				//IL_002b: Expected O, but got Unknown
				string text = (input ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					InformationManager.DisplayMessage(new InformationMessage("模型名称不能为空。"));
					OpenManualModelNameInput();
				}
				else
				{
					_showModelSelectionValidationFailedHint = false;
					settings.ModelName = text;
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage("模型名称已写入 MCM，正在测试完整连接：" + text));
					BeginValidateMcmApiAndContinue(returnToModelSelection: true);
				}
			}, (Action)delegate
			{
				ShowModelSelectionPopup();
			}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开模型名称输入框失败：" + ex.Message));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void BeginValidateMcmApiAndContinue(bool returnToModelSelection = false)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Expected O, but got Unknown
		if (_apiValidationInProgress)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置。"));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
		TryPersistMcmSettings(settings);
		string apiUrl = (settings.ApiUrl ?? "").Trim();
		string apiKey = (settings.ApiKey ?? "").Trim();
		string modelName = (settings.ModelName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(apiUrl))
		{
			InformationManager.DisplayMessage(new InformationMessage("MCM 中尚未填写 Base URL。"));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			InformationManager.DisplayMessage(new InformationMessage("MCM 中尚未填写 API Key。"));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
		if (string.IsNullOrWhiteSpace(modelName))
		{
			InformationManager.DisplayMessage(new InformationMessage("MCM 中尚未填写模型名称。"));
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
			return;
		}
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
				string jsonBody = JsonConvert.SerializeObject((object)value);
				StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
				DuelSettings.GlobalClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
				HttpResponseMessage httpResponseMessage = await DuelSettings.GlobalClient.PostAsync(effectiveApiUrl, (HttpContent)content, cancellationTokenSource.Token);
				string text2 = await httpResponseMessage.Content.ReadAsStringAsync();
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					flag = true;
					try
					{
						JObject jObject = JObject.Parse(text2);
						JToken obj = jObject["choices"];
						object obj2;
						if (obj == null)
						{
							obj2 = null;
						}
						else
						{
							JToken obj3 = obj[(object)0];
							if (obj3 == null)
							{
								obj2 = null;
							}
							else
							{
								JToken obj4 = obj3[(object)"message"];
								obj2 = ((obj4 == null) ? null : ((object)obj4[(object)"content"])?.ToString());
							}
						}
						string text3 = (string)obj2;
						text = (string.IsNullOrWhiteSpace(text3) ? "MCM 中的 API 信息连接测试成功，可以进入下一步。" : ("MCM 中的 API 信息连接测试成功：" + text3.Trim()));
					}
					catch
					{
						text = "MCM 中的 API 信息连接测试成功，可以进入下一步。";
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
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				failureHint = "通常是网络异常、证书或代理设置异常，或者 Base URL 填写不正确。";
				text = "API 连接测试异常：" + ex3.Message;
			}
			finally
			{
				if (num == _apiValidationVersion)
				{
					if (_apiValidationCancellation == cancellationTokenSource)
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

	private void ShowApiValidationProgressPopup()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		try
		{
			_welcomeInProgress = true;
			_activeOnboardingStage = OnboardingUiStage.ApiValidation;
			InformationManager.ShowInquiry(new InquiryData("正在测试已有配置", "正在使用 MCM 中的 API 信息进行连接测试，请稍候……\n\n测试完成后将自动进入下一步。\n如果你的API测试始终未成功，你也可以在此界面直接退出存档。", true, true, "退出当前存档", "返回上一界面", (Action)ExitCurrentGameFromOnboarding, (Action)CancelApiValidationAndReturnToWelcome, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
		}
	}

	private void CancelApiValidationAndReturnToWelcome()
	{
		CancelApiValidationCore();
		_pendingReturnToWelcome = true;
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
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
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
			ShowWelcomePopup(fromGate: true, ignoreSuppress: true);
		}
	}

	private void ShowImportSetupPopup(bool fromGate)
	{
		ShowImportSetupPopup(fromGate, ignoreSuppress: false);
	}

	private void ShowImportSetupPopup(bool fromGate, bool ignoreSuppress)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
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
			InformationManager.ShowInquiry(new InquiryData("AnimusForge - 首次使用", text, true, true, "一键导入", "跳过", (Action)delegate
			{
				_welcomeInProgress = false;
				OpenImportFolderPicker(delegate
				{
					ShowImportSetupPopup(fromGate: true);
				});
			}, (Action)delegate
			{
				_welcomeInProgress = false;
				ShowSkipImportConfirmation(delegate
				{
					ShowImportSetupPopup(fromGate: true, ignoreSuppress: true);
				});
			}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void ShowSkipImportConfirmation(Action onReturn)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
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
			InformationManager.ShowInquiry(new InquiryData("跳过数据库导入", text, true, true, "确定", "返回", (Action)delegate
			{
				_welcomeInProgress = false;
				CompleteOnboardingAndOpenPlayerPersonaSetup(onReturn, importedDatabase: false);
			}, (Action)delegate
			{
				_welcomeInProgress = false;
				onReturn();
			}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
			_welcomeInProgress = false;
			onReturn?.Invoke();
		}
	}

	private void CompleteOnboardingAndOpenPlayerPersonaSetup(Action onReturn, bool importedDatabase)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		try
		{
			_setupDone = true;
			_activeOnboardingStage = OnboardingUiStage.None;
			object obj = KnowledgeLibraryBehavior.Instance;
			if (obj == null)
			{
				Campaign current = Campaign.Current;
				obj = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = (KnowledgeLibraryBehavior)obj;
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
		try
		{
			(((object)settings)?.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null))?.Invoke(settings, null);
		}
		catch
		{
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
		if (statusCode == HttpStatusCode.NotFound)
		{
			text += "\n排查建议：请检查 Base URL 尾缀和模型名称是否正确。";
		}
		else if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
		{
			text += "\n排查建议：请检查 API Key 是否有效。";
		}
		else if (statusCode == (HttpStatusCode)522)
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
		string text = (responseBody ?? "").Trim();
		string text2 = text.ToLowerInvariant();
		switch ((int)statusCode)
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
		return statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.MethodNotAllowed || statusCode == (HttpStatusCode)429 || statusCode == HttpStatusCode.PaymentRequired;
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
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Invalid comparison between Unknown and I4
		List<string> list = new List<string>();
		try
		{
			if (string.IsNullOrWhiteSpace(responseBody))
			{
				return list;
			}
			JToken val = JToken.Parse(responseBody);
			IEnumerable<JToken> enumerable = Enumerable.Empty<JToken>();
			if ((int)val.Type == 1)
			{
				JToken obj = ((JObject)val)["data"];
				enumerable = (IEnumerable<JToken>)(((obj is JArray) ? obj : null) ?? ((object)/*isinst with value type is only supported in some contexts*/) ?? ((object)new JArray()));
			}
			else if ((int)val.Type == 2)
			{
				enumerable = (IEnumerable<JToken>)(JArray)val;
			}
			foreach (JToken item in enumerable)
			{
				JTokenType type = item.Type;
				if (1 == 0)
				{
				}
				string text = (((int)type != 8) ? (((object)item[(object)"id"])?.ToString() ?? ((object)item[(object)"model"])?.ToString() ?? ((object)item[(object)"name"])?.ToString()) : ((object)item).ToString());
				if (1 == 0)
				{
				}
				string text2 = text;
				if (!string.IsNullOrWhiteSpace(text2))
				{
					list.Add(text2.Trim());
				}
			}
		}
		catch
		{
		}
		return list.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static string BuildModelFetchFailureMessage(HttpStatusCode statusCode, string responseBody)
	{
		string text = "拉取模型列表失败。";
		text = (int)statusCode switch
		{
			401 => text + " 当前 API Key 无效，或鉴权未通过。", 
			402 => text + " 当前账号额度不足，或渠道限制了接口调用。", 
			403 => text + " 当前 Key 没有访问模型列表接口的权限，或被服务商风控拦截。", 
			404 => text + " 当前 Base URL 很可能不正确，或该服务商不支持 /models 接口。", 
			429 => text + " 当前请求过于频繁，或账户触发了速率限制。", 
			_ => text + " 你可以手动输入模型名称继续。", 
		};
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
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Expected O, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Expected O, but got Unknown
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
			list2.Add(new InquiryElement((object)"__latest__", "自动选择最新导出（推荐）", (ImageIdentifier)null));
			list2.Add(new InquiryElement((object)"__manual__", "手动输入文件夹名", (ImageIdentifier)null));
			foreach (string item in list)
			{
				if (!string.IsNullOrWhiteSpace(item))
				{
					list2.Add(new InquiryElement((object)item, item, (ImageIdentifier)null));
				}
			}
			MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择导入文件夹", "请选择 PlayerExports 下的导出文件夹：", list2, true, 0, 1, "导入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
			{
				//IL_0084: Unknown result type (might be due to invalid IL or missing references)
				//IL_0090: Expected O, but got Unknown
				if (selected == null || selected.Count == 0)
				{
					onReturn();
				}
				else
				{
					string text = selected[0].Identifier as string;
					if (text == "__manual__")
					{
						InformationManager.ShowTextInquiry(new TextInquiryData("手动输入文件夹名", "请输入 PlayerExports 下的文件夹名（留空=最新）：", true, true, "确定", "取消", (Action<string>)delegate(string input)
						{
							string folderName2 = (input ?? "").Trim();
							TryImportRequiredSetAndUnlock(folderName2, onReturn);
						}, (Action)delegate
						{
							OpenImportFolderPicker(onReturn);
						}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
					}
					else
					{
						string folderName = ((text == "__latest__") ? "" : (text ?? ""));
						TryImportRequiredSetAndUnlock(folderName, onReturn);
					}
				}
			}, (Action<List<InquiryElement>>)delegate
			{
				onReturn();
			}, "", false);
			MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开导入选择失败：" + ex.Message));
			onReturn?.Invoke();
		}
	}

	private void TryImportRequiredSetAndUnlock(string folderName, Action onReturn)
	{
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Expected O, but got Unknown
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected O, but got Unknown
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Expected O, but got Unknown
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Expected O, but got Unknown
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Expected O, but got Unknown
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Expected O, but got Unknown
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Expected O, but got Unknown
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Expected O, but got Unknown
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Expected O, but got Unknown
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			string text = Path.Combine(GetModuleRootPath(), "SubModule.xml");
			if (!File.Exists(text))
			{
				return "未知版本";
			}
			XDocument xDocument = XDocument.Load(text);
			string text2 = xDocument.Root?.Element("Version")?.Attribute("value")?.Value;
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.Trim();
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
		char[] array = invalidFileNameChars;
		foreach (char oldChar in array)
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
