using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

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
	private string _titleText = "AnimusForge - API 快捷配置向导";
	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set => SetField(ref _titleText, value, nameof(TitleText));
	}

	private string _subtitleText = "选择最适合你的 AI 方案，点击卡片进入下一步配置";
	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set => SetField(ref _subtitleText, value, nameof(SubtitleText));
	}

	// Multi-API Panel Details
	private string _multiApiModeTag = "完全自定义";
	[DataSourceProperty]
	public string MultiApiModeTag
	{
		get => _multiApiModeTag;
		set => SetField(ref _multiApiModeTag, value, nameof(MultiApiModeTag));
	}

	private string _multiApiModeNotice = "在一个面板中集中设置 4 条 API 的 Base URL、Key 与绑定模型";
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

	private string _primaryStatusText = "待测试";
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

	private string _auxiliaryStatusText = "待测试";
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

	private string _postprocessStatusText = "待测试";
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

	private string _eventStatusText = "待测试";
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
	private string _yjKeyMasked = "未绑定";
	[DataSourceProperty]
	public string YjKeyMasked
	{
		get => _yjKeyMasked;
		set => SetField(ref _yjKeyMasked, value, nameof(YjKeyMasked));
	}

	// Testing Overall State
	private string _testOverallNotice = "正在向 4 条 API 管线发送握手测试请求...";
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

	private string _keyPromptTitle = "填写 API Key";
	[DataSourceProperty]
	public string KeyPromptTitle
	{
		get => _keyPromptTitle;
		set => SetField(ref _keyPromptTitle, value, nameof(KeyPromptTitle));
	}

	private string _keyPromptHint = "请输入 API Key：";
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

		if (!string.IsNullOrWhiteSpace(settings.ApiUrl)) _primaryUrl = settings.ApiUrl;
		if (!string.IsNullOrWhiteSpace(settings.ApiKey)) _primaryKey = settings.ApiKey;
		if (!string.IsNullOrWhiteSpace(settings.ModelName)) _primaryModel = settings.ModelName;

		if (!string.IsNullOrWhiteSpace(settings.AuxiliaryApiUrl)) _auxiliaryUrl = settings.AuxiliaryApiUrl;
		if (!string.IsNullOrWhiteSpace(settings.AuxiliaryApiKey)) _auxiliaryKey = settings.AuxiliaryApiKey;
		if (!string.IsNullOrWhiteSpace(settings.AuxiliaryModelName)) _auxiliaryModel = settings.AuxiliaryModelName;

		if (!string.IsNullOrWhiteSpace(settings.ActionPostprocessApiUrl)) _postprocessUrl = settings.ActionPostprocessApiUrl;
		if (!string.IsNullOrWhiteSpace(settings.ActionPostprocessApiKey)) _postprocessKey = settings.ActionPostprocessApiKey;
		if (!string.IsNullOrWhiteSpace(settings.ActionPostprocessModelName)) _postprocessModel = settings.ActionPostprocessModelName;

		if (!string.IsNullOrWhiteSpace(settings.EventAndRebellionApiUrl)) _eventUrl = settings.EventAndRebellionApiUrl;
		if (!string.IsNullOrWhiteSpace(settings.EventAndRebellionApiKey)) _eventKey = settings.EventAndRebellionApiKey;
		if (!string.IsNullOrWhiteSpace(settings.EventAndRebellionModelName)) _eventModel = settings.EventAndRebellionModelName;
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
				TitleText = "AnimusForge - API 快捷配置向导";
				SubtitleText = "选择最适合你的 AI 方案，点击卡片进入下一步配置";
				break;
			case OnboardingView.YjMenu:
				TitleText = "YJ API 官方中转站 · 专属配置向导";
				SubtitleText = "免魔法国内直连，请选择单分组或多分组接入方式";
				break;
			case OnboardingView.MultiApi:
				TitleText = _isCustomMode ? "完全自定义 API · 多管线集中设置" : "YJ API 多分组模式 · 专线集中配置";
				SubtitleText = _isCustomMode ? "在一个面板中集中设置 4 条 API 的 Base URL、Key 与绑定模型" : "Base URL 已预设为 YJ 专线，支持 4 条管线分别配置 Key 与模型";
				break;
			case OnboardingView.YjModels:
				TitleText = "YJ 官方专线 · 4 处场景模型统一挑选";
				SubtitleText = "已绑定单分组 Key，请为以下 4 处场景挑选模型";
				break;
			case OnboardingView.Testing:
				TitleText = "API 连通性握手测试";
				SubtitleText = "正在并行测试 4 路 API 的响应速度与连通状态...";
				break;
			case OnboardingView.Success:
				TitleText = "🎉 全部 API 连通性测试通过！";
				SubtitleText = "配置已就绪，点击下方按钮保存设置并进入卡拉迪亚";
				break;
		}
	}

	// ================= Actions: Main Cards =================
	public void ExecuteSelectYj()
	{
		SwitchView(OnboardingView.YjMenu);
	}

	public void ExecuteSelectDeepSeekFlash()
	{
		OpenKeyPrompt("DeepSeek Flash 极速推荐", "请输入你的 DeepSeek 官方 API Key (sk-...)：", key =>
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				ShowToast("API Key 不能为空");
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
		OpenKeyPrompt("DeepSeek Pro 深度智谋", "请输入你的 DeepSeek 官方 API Key (sk-...)：", key =>
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				ShowToast("API Key 不能为空");
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
		_isCustomMode = true;
		MultiApiModeTag = "完全自定义";
		MultiApiModeNotice = "在一个面板中集中设置 4 条 API 的 Base URL、Key 与绑定模型";
		SwitchView(OnboardingView.MultiApi);
	}

	// ================= Actions: YJ Submenu =================
	public void ExecuteYjSingleGroup()
	{
		OpenKeyPrompt("YJ 单分组 Key 接入", "请输入一个 YJ API Key，将自动写入 4 条管线：", key =>
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				ShowToast("API Key 不能为空");
				return;
			}
			string yjUrl = "https://yjapi.manqiaotechnology.com/v1";
			PrimaryUrl = yjUrl;
			AuxiliaryUrl = yjUrl;
			PostprocessUrl = yjUrl;
			EventUrl = yjUrl;

			PrimaryKey = key;
			AuxiliaryKey = key;
			PostprocessKey = key;
			EventKey = key;

			YjKeyMasked = key.Length > 8 ? (key.Substring(0, 4) + "..." + key.Substring(key.Length - 4)) : key;
			SwitchView(OnboardingView.YjModels);

			// 自动拉取一次模型
			ExecuteFetchAllYjModels();
		});
	}

	public void ExecuteYjMultiGroup()
	{
		_isCustomMode = false;
		string yjUrl = "https://yjapi.manqiaotechnology.com/v1";
		PrimaryUrl = yjUrl;
		AuxiliaryUrl = yjUrl;
		PostprocessUrl = yjUrl;
		EventUrl = yjUrl;

		MultiApiModeTag = "YJ 官方专线预设";
		MultiApiModeNotice = "已自动填充 YJ Base URL，支持 4 条管线分别填入不同分组 Key";
		SwitchView(OnboardingView.MultiApi);
	}

	public void ExecuteOpenYjWeb()
	{
		ExternalBrowserLauncher.TryOpen("https://yjapi.manqiaotechnology.com/keys", out _, out _);
		ExternalBrowserLauncher.TryOpen("https://pay.ldxp.cn/shop/OF6AKWNI", out _, out _);
		ShowToast("正在打开 YJ API Key 管理页与额度购买页...");
	}

	public void ExecuteCopyQqGroup()
	{
		try
		{
			TaleWorlds.InputSystem.Input.SetClipboardText("1097237977");
			ShowToast("已复制 YJ 交流 QQ 群号：1097237977");
		}
		catch
		{
			ShowToast("复制群号失败，请手动添加：1097237977");
		}
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
		ShowToast("正在拉取 YJ 可用模型列表...");
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
			ShowToast("请先填写 Base URL 与 API Key");
			return;
		}

		Task.Run(async () =>
		{
			try
			{
				ModelCatalogExchange exchange = await new LegacyModelCatalogGateway().FetchModelsAsync(url.Trim(), key.Trim(), CancellationToken.None);
				if (exchange.IsSuccessStatusCode)
				{
					List<string> models = ModOnboardingBehavior.ExtractModelNamesFromResponse(exchange.ResponseBody);
					if (models != null && models.Count > 0)
					{
						ShowToast("成功拉取 " + models.Count + " 个模型");
						onListFetched?.Invoke(models);
						return;
					}
				}
				ShowToast("未能从接口解析出模型列表");
			}
			catch (Exception ex)
			{
				ShowToast("拉取模型失败: " + ex.Message);
			}
		});
	}

	// ================= Actions: Testing Sequence =================
	public void ExecuteStartCombinedTest()
	{
		if (string.IsNullOrWhiteSpace(PrimaryKey) || string.IsNullOrWhiteSpace(PrimaryUrl))
		{
			ShowToast("主 API 配置不完整，请检查 URL 与 Key");
			return;
		}

		SwitchView(OnboardingView.Testing);
		TestOverallNotice = "正在并行向 4 条管线发送握手测试请求...";
		CanCancelTest = true;

		PrimaryStatusText = "正在握手...";
		PrimaryLatencyText = "";
		IsPrimarySuccess = false;
		IsPrimaryFailed = false;

		AuxiliaryStatusText = "正在握手...";
		AuxiliaryLatencyText = "";
		IsAuxiliarySuccess = false;
		IsAuxiliaryFailed = false;

		PostprocessStatusText = "正在握手...";
		PostprocessLatencyText = "";
		IsPostprocessSuccess = false;
		IsPostprocessFailed = false;

		EventStatusText = "正在握手...";
		EventLatencyText = "";
		IsEventSuccess = false;
		IsEventFailed = false;

		_testCts?.Cancel();
		_testCts?.Dispose();
		_testCts = new CancellationTokenSource();
		CancellationToken token = _testCts.Token;

		Task.Run(async () =>
		{
			var targets = new List<ModOnboardingBehavior.ApiValidationTargetInfo>
			{
				new ModOnboardingBehavior.ApiValidationTargetInfo
				{
					Target = ModOnboardingBehavior.ApiSetupTarget.Primary,
					DisplayName = "主 API（正文生成）",
					ApiUrl = PrimaryUrl.Trim(),
					ApiKey = PrimaryKey.Trim(),
					ModelName = PrimaryModel.Trim()
				},
				new ModOnboardingBehavior.ApiValidationTargetInfo
				{
					Target = ModOnboardingBehavior.ApiSetupTarget.Auxiliary,
					DisplayName = "前处理 API（意图路由）",
					ApiUrl = AuxiliaryUrl.Trim(),
					ApiKey = AuxiliaryKey.Trim(),
					ModelName = AuxiliaryModel.Trim()
				},
				new ModOnboardingBehavior.ApiValidationTargetInfo
				{
					Target = ModOnboardingBehavior.ApiSetupTarget.ActionPostprocess,
					DisplayName = "后处理 API（指令标签）",
					ApiUrl = PostprocessUrl.Trim(),
					ApiKey = PostprocessKey.Trim(),
					ModelName = PostprocessModel.Trim()
				},
				new ModOnboardingBehavior.ApiValidationTargetInfo
				{
					Target = ModOnboardingBehavior.ApiSetupTarget.EventAndRebellion,
					DisplayName = "每日与周报 API",
					ApiUrl = EventUrl.Trim(),
					ApiKey = EventKey.Trim(),
					ModelName = EventModel.Trim()
				}
			};

			var tasks = targets.Select(t => TestSingleTargetAsync(t, token)).ToArray();
			ModOnboardingBehavior.ApiValidationTargetResult[] results;
			try
			{
				results = await Task.WhenAll(tasks);
			}
			catch (OperationCanceledException)
			{
				TestOverallNotice = "测试已由用户取消。";
				return;
			}
			catch (Exception ex)
			{
				TestOverallNotice = "测试发生异常：" + ex.Message;
				return;
			}

			bool allPassed = results.All(r => r != null && r.Success);
			if (allPassed)
			{
				TestOverallNotice = "✔ 4 条 API 全部握手成功！即将进入确认界面...";
				await Task.Delay(600);
				SwitchView(OnboardingView.Success);
			}
			else
			{
				TestOverallNotice = "❌ 部分 API 连通性测试未通过，请检查错误提示并返回修改。";
			}
		});
	}

	private async Task<ModOnboardingBehavior.ApiValidationTargetResult> TestSingleTargetAsync(ModOnboardingBehavior.ApiValidationTargetInfo target, CancellationToken token)
	{
		var sw = Stopwatch.StartNew();
		ModOnboardingBehavior.ApiValidationTargetResult res = await ModOnboardingBehavior.ValidateApiTargetAsync(target, token);
		sw.Stop();

		long ms = sw.ElapsedMilliseconds;

		switch (target.Target)
		{
			case ModOnboardingBehavior.ApiSetupTarget.Primary:
				IsPrimarySuccess = res.Success;
				IsPrimaryFailed = !res.Success;
				PrimaryStatusText = res.Success ? "✔ 握手成功" : ("❌ " + (res.Message ?? "连接失败"));
				PrimaryLatencyText = res.Success ? (ms + "ms") : "";
				break;
			case ModOnboardingBehavior.ApiSetupTarget.Auxiliary:
				IsAuxiliarySuccess = res.Success;
				IsAuxiliaryFailed = !res.Success;
				AuxiliaryStatusText = res.Success ? "✔ 握手成功" : ("❌ " + (res.Message ?? "连接失败"));
				AuxiliaryLatencyText = res.Success ? (ms + "ms") : "";
				break;
			case ModOnboardingBehavior.ApiSetupTarget.ActionPostprocess:
				IsPostprocessSuccess = res.Success;
				IsPostprocessFailed = !res.Success;
				PostprocessStatusText = res.Success ? "✔ 握手成功" : ("❌ " + (res.Message ?? "连接失败"));
				PostprocessLatencyText = res.Success ? (ms + "ms") : "";
				break;
			case ModOnboardingBehavior.ApiSetupTarget.EventAndRebellion:
				IsEventSuccess = res.Success;
				IsEventFailed = !res.Success;
				EventStatusText = res.Success ? "✔ 握手成功" : ("❌ " + (res.Message ?? "连接失败"));
				EventLatencyText = res.Success ? (ms + "ms") : "";
				break;
		}

		return res;
	}

	public void ExecuteCancelTest()
	{
		_testCts?.Cancel();
		SwitchView(_currentView == OnboardingView.Testing ? (_isCustomMode ? OnboardingView.MultiApi : OnboardingView.YjModels) : OnboardingView.Main);
	}

	// ================= Actions: Save & Finish =================
	public void ExecuteSaveAndFinish()
	{
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings != null)
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
		_onCancelled?.Invoke();
	}

	public void ExecuteOpenSupport()
	{
		IsSupportModalVisible = true;
	}

	public void ExecuteCloseSupportModal()
	{
		IsSupportModalVisible = false;
	}

	public void ExecuteOpenAfdianWeb()
	{
		DuelSettings.OpenAfdianSupportPageForExternal();
		ShowToast("正在打开爱发电支持页面...");
	}

	// Prompt Overlay Helpers
	private void OpenKeyPrompt(string title, string hint, Action<string> onConfirm)
	{
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
	public void ExecuteEditPrimaryUrl() => OpenTextInquiryForField("编辑主 API Base URL", PrimaryUrl, v => PrimaryUrl = v);
	public void ExecuteEditPrimaryKey() => OpenTextInquiryForField("编辑主 API Key", PrimaryKey, v => PrimaryKey = v);
	public void ExecuteEditAuxiliaryUrl() => OpenTextInquiryForField("编辑前处理 Base URL", AuxiliaryUrl, v => AuxiliaryUrl = v);
	public void ExecuteEditAuxiliaryKey() => OpenTextInquiryForField("编辑前处理 Key", AuxiliaryKey, v => AuxiliaryKey = v);
	public void ExecuteEditPostprocessUrl() => OpenTextInquiryForField("编辑后处理 Base URL", PostprocessUrl, v => PostprocessUrl = v);
	public void ExecuteEditPostprocessKey() => OpenTextInquiryForField("编辑后处理 Key", PostprocessKey, v => PostprocessKey = v);
	public void ExecuteEditEventUrl() => OpenTextInquiryForField("编辑周报 API Base URL", EventUrl, v => EventUrl = v);
	public void ExecuteEditEventKey() => OpenTextInquiryForField("编辑周报 Key", EventKey, v => EventKey = v);
	public void ExecuteEditPromptKey() => OpenTextInquiryForField("填写 API Key", PromptKeyInput, v => PromptKeyInput = v);

	private void OpenTextInquiryForField(string title, string initialText, Action<string> onConfirmed)
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(title, "请输入或粘贴内容：", true, true, "确定", "取消", v =>
		{
			if (v != null) onConfirmed?.Invoke(v.Trim());
		}, null, false, null, initialText ?? ""));
	}

	public void ShowToast(string message)
	{
		ToastMessage = message;
		IsToastVisible = true;
		_toastTimer = 3.0f;
	}

	public void OnTick()
	{
		if (IsToastVisible)
		{
			_toastTimer -= 0.05f;
			if (_toastTimer <= 0f)
			{
				IsToastVisible = false;
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_testCts?.Cancel();
		_testCts?.Dispose();
		_testCts = null;
	}
}
