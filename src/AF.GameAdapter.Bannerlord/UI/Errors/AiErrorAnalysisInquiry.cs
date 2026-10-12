using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Asks whether to analyze the latest non-blocking error, then keeps analysis output in the lower-left message feed.
/// Network work only starts after the player confirms and never runs on the game thread.
/// </summary>
public static class AiErrorAnalysisInquiry
{
	private const int AnalysisMaxInputChars = 24000;
	private const int AnalysisMaxOutputTokens = 1000;
	private const int AnalysisRequestTimeoutMilliseconds = 60000;
	private const int AnalysisCacheCapacity = 16;
	private const int MainThreadActionsPerTick = 2;
	private static string AnalysisSystemPrompt => AdapterPromptCaptureTexts.PcapText("error_analysis_system");

	private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();
	private static readonly object AnalysisCacheLock = new object();
	private static readonly object LatestFailureLock = new object();
	private static readonly Dictionary<string, string> AnalysisCache = new Dictionary<string, string>(StringComparer.Ordinal);
	private static readonly Queue<string> AnalysisCacheOrder = new Queue<string>();
	private static int _patchState;
	private static int _analysisInProgress;
	private static int _analysisPromptPending;
	private static int _analysisPromptActive;
	private static string _latestFailureTitle = "";
	private static string _latestFailureDetail = "";
	[ThreadStatic]
	private static bool _suppressEnhancement;

	private enum AnalysisFailureKind
	{
		ConfigurationUnavailable,
		ConfigurationIncomplete,
		Http,
		Request,
		EmptyReply,
		Timeout,
		Exception
	}

	// Carry detached error data across the worker boundary; resolve language on publication.
	private sealed class AnalysisFailure
	{
		public readonly AnalysisFailureKind Kind;
		public readonly string Detail;

		public AnalysisFailure(AnalysisFailureKind kind, string detail = "")
		{
			Kind = kind;
			Detail = detail ?? "";
		}
	}

	public static void EnsurePatched(Harmony harmony)
	{
		if (harmony == null || Interlocked.CompareExchange(ref _patchState, 1, 0) != 0)
		{
			return;
		}
		try
		{
			MethodInfo target = AccessTools.Method(typeof(InformationManager), nameof(InformationManager.ShowInquiry), new Type[3]
			{
				typeof(InquiryData),
				typeof(bool),
				typeof(bool)
			});
			MethodInfo prefix = AccessTools.Method(typeof(AiErrorAnalysisInquiry), nameof(ShowInquiryPrefix));
			if (target == null || prefix == null)
			{
				throw new MissingMethodException("InformationManager.ShowInquiry(InquiryData,bool,bool)");
			}
			harmony.Patch(target, prefix: new HarmonyMethod(prefix));
			Logger.Log("AiErrorAnalysis", "Enabled the single-button error popup AI analysis entry.");
		}
		catch
		{
			Volatile.Write(ref _patchState, 0);
			throw;
		}
	}

	public static void OnApplicationTick()
	{
		int processed = 0;
		while (processed < MainThreadActionsPerTick && MainThreadActions.TryDequeue(out var action))
		{
			processed++;
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("AiErrorAnalysis", "Main-thread callback failed: " + ex.Message);
			}
		}
		// 统一在已有主线程 tick 中延后执行被拦截错误报告的“返回/关闭”回调，避免 Harmony 前缀重入 UI。
		NonBlockingErrorReport.OnApplicationTick();
		TryShowPendingAnalysisPrompt();
	}

	/// <summary>
	/// Caches the latest automatic error so the player can still request AI analysis after error popups became non-blocking.
	/// </summary>
	internal static void RememberFailure(string title, string detail)
	{
		string safeTitle = NormalizeText(title, Ui("AF_UI_ErrorAnalysis_ErrorTitle", "AnimusForge error"));
		string safeDetail = LimitForAnalysis(NormalizeText(detail, Ui("AF_UI_Retry_UnknownError", "Unknown error")));
		lock (LatestFailureLock)
		{
			_latestFailureTitle = safeTitle;
			_latestFailureDetail = safeDetail;
		}
	}

	/// <summary>
	/// Queues a yes/no analysis confirmation that temporarily takes priority over an actionable inquiry.
	/// </summary>
	internal static void RequestLatestFailureAnalysis()
	{
		// 只保留一个待确认标记；连续错误始终分析最新一条，避免在 UI 队列中堆积多个相同确认框。
		Interlocked.Exchange(ref _analysisPromptPending, 1);
	}

	/// <summary>
	/// Opens the same confirmation flow for the terminal action; analysis never starts without an explicit yes/no choice.
	/// </summary>
	public static void AnalyzeLatestFailure()
	{
		string detail;
		lock (LatestFailureLock)
		{
			detail = _latestFailureDetail;
		}
		if (string.IsNullOrWhiteSpace(detail))
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_ErrorAnalysis_NoRecentError", "[AnimusForge] There is no recent error available for analysis."), Colors.Yellow));
			return;
		}
		// 终端入口也遵守统一确认规则，避免任何入口绕过“是否分析”的玩家选择。
		RequestLatestFailureAnalysis();
	}

	public static bool TryGetLatestFailure(out string title, out string detail)
	{
		lock (LatestFailureLock)
		{
			title = _latestFailureTitle ?? "";
			detail = _latestFailureDetail ?? "";
			return !string.IsNullOrWhiteSpace(detail);
		}
	}

	private static void TryShowPendingAnalysisPrompt()
	{
		if (Volatile.Read(ref _analysisPromptPending) == 0
			|| Volatile.Read(ref _analysisInProgress) != 0
			|| Volatile.Read(ref _analysisPromptActive) != 0)
		{
			return;
		}
		string title;
		string detail;
		lock (LatestFailureLock)
		{
			title = _latestFailureTitle;
			detail = _latestFailureDetail;
		}
		if (string.IsNullOrWhiteSpace(detail))
		{
			Interlocked.Exchange(ref _analysisPromptPending, 0);
			return;
		}
		if (Interlocked.CompareExchange(ref _analysisPromptActive, 1, 0) != 0)
		{
			return;
		}
		if (Interlocked.CompareExchange(ref _analysisPromptPending, 0, 1) != 1)
		{
			Interlocked.Exchange(ref _analysisPromptActive, 0);
			return;
		}
		try
		{
			_suppressEnhancement = true;
			InformationManager.ShowInquiry(new InquiryData(
				Ui("AF_UI_ErrorAnalysis_Title", "Analyze the error?"),
				UiFormat("AF_UI_ErrorAnalysis_ConfirmBody", "The full details of “{TITLE}” were shown in the lower-left message feed.\n\nUse the preprocessing API to analyze the cause and suggest next steps? The current action window will resume after this choice.", ("TITLE", NormalizeText(title, Ui("AF_UI_ErrorAnalysis_ErrorTitle", "AnimusForge error")))),
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: true,
				Ui("AF_UI_ErrorAnalysis_Analyze", "Analyze"),
				Ui("AF_UI_ErrorAnalysis_Skip", "Skip for now"),
				delegate
				{
					// 先解除确认框占用，避免分析期间的新错误被旧确认框永久阻塞。
					Interlocked.Exchange(ref _analysisPromptActive, 0);
					BeginAnalysis(title, detail, null);
				},
				delegate
				{
					// 玩家明确暂不分析后允许后续新错误正常发起新的确认。
					Interlocked.Exchange(ref _analysisPromptActive, 0);
				}), pauseGameActiveState: true, prioritize: true);
		}
		catch (Exception ex)
		{
			Interlocked.Exchange(ref _analysisPromptActive, 0);
			Logger.Log("AiErrorAnalysis", "Failed to display error analysis confirmation: " + ex.Message);
		}
		finally
		{
			_suppressEnhancement = false;
		}
	}

	private static bool ShowInquiryPrefix(ref InquiryData data)
	{
		try
		{
			if (_suppressEnhancement || !HasAnimusForgeCaller())
			{
				return true;
			}
			// 单动作报错统一转到左下角并排入“是否分析”确认；多动作窗口仍保留其实际决策。
			if (NonBlockingErrorReport.TryRouteAcknowledgementInquiry(data))
			{
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("AiErrorAnalysis", "Failed to forward error analysis confirmation: " + ex.Message);
		}
		return true;
	}

	private static bool HasAnimusForgeCaller()
	{
		try
		{
			StackFrame[] frames = new StackTrace(false).GetFrames();
			if (frames == null)
			{
				return false;
			}
			for (int i = 0; i < frames.Length; i++)
			{
				Type declaringType = frames[i]?.GetMethod()?.DeclaringType;
				if (declaringType == null || IsAnalysisHelperType(declaringType))
				{
					continue;
				}
				string assemblyName = declaringType.Assembly?.GetName()?.Name ?? "";
				if (assemblyName.StartsWith("AnimusForge", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsAnalysisHelperType(Type type)
	{
		string fullName = type?.FullName ?? "";
		string helperName = typeof(AiErrorAnalysisInquiry).FullName ?? "AnimusForge.AiErrorAnalysisInquiry";
		return string.Equals(fullName, helperName, StringComparison.Ordinal)
			|| fullName.StartsWith(helperName + "+", StringComparison.Ordinal);
	}

	private static void BeginAnalysis(string title, string detail, Action originalCloseAction)
	{
		string boundedDetail = LimitForAnalysis(detail);
		string cacheKey = BuildCacheKey(title, boundedDetail);
		if (TryGetCachedAnalysis(cacheKey, out var cachedAnalysis))
		{
			ShowAnalysisResult(title, cachedAnalysis, originalCloseAction);
			return;
		}
		if (Interlocked.CompareExchange(ref _analysisInProgress, 1, 0) != 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_ErrorAnalysis_AlreadyRunning", "[AnimusForge] Another error is already being analyzed. Please wait."), Colors.Yellow));
			SafeInvoke(originalCloseAction);
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_ErrorAnalysis_Starting", "[AnimusForge] Calling the preprocessing API to analyze the error..."), Colors.Yellow));
		_ = Task.Run(delegate
		{
			bool success = TryAnalyzeWithPreprocessApi(title, boundedDetail, out var analysis, out var error);
			MainThreadActions.Enqueue(delegate
			{
				Interlocked.Exchange(ref _analysisInProgress, 0);
				if (success)
				{
					StoreCachedAnalysis(cacheKey, analysis);
					ShowAnalysisResult(title, analysis, originalCloseAction);
				}
				else
				{
					ShowAnalysisFailure(title, boundedDetail, error, originalCloseAction);
				}
			});
		});
	}

	private static bool TryAnalyzeWithPreprocessApi(string title, string detail, out string analysis, out AnalysisFailure error)
	{
		analysis = "";
		error = null;
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				error = new AnalysisFailure(AnalysisFailureKind.ConfigurationUnavailable);
				return false;
			}
			string apiUrl = DuelSettings.GetEffectiveApiUrl(settings.AuxiliaryApiUrl ?? "");
			string apiKey = (settings.AuxiliaryApiKey ?? "").Trim();
			string modelName = settings.GetEffectiveAuxiliaryModelName();
			if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelName))
			{
				error = new AnalysisFailure(AnalysisFailureKind.ConfigurationIncomplete);
				return false;
			}
			string safeTitle = RedactConfiguredApiKeys(title, settings);
			string safeDetail = RedactConfiguredApiKeys(detail, settings);
			PromptPackage prompt = new PromptPackage(
				new[]
				{
					new PromptMessage("system", AnalysisSystemPrompt),
					new PromptMessage("user", AdapterPromptCaptureTexts.PcapText("error_analysis_title") + safeTitle + "\n<error>\n" + safeDetail + "\n</error>")
				},
				AnalysisMaxOutputTokens,
				modelName);
			long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
#if BANNERLORD_1_4_OR_GREATER
			string apiLine = "1.4";
#else
			string apiLine = "1.3";
#endif
			TraceContext trace = new TraceContext(
				"af-error-analysis-" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture),
				runtimeGeneration,
				runtimeGeneration,
				"error-analysis",
				apiLine);
			LlmProviderSnapshot provider = new LlmProviderSnapshot(
				"auxiliary-error-analysis",
				apiUrl,
				modelName,
				AnalysisRequestTimeoutMilliseconds,
				AnalysisMaxOutputTokens);
			LlmGenerateResult generated = new LegacyConfiguredChatGateway(
				_ => apiKey,
				temperature: 0f,
				disableThinking: true)
				.GenerateAsync(
					new LlmGenerateRequest(trace, provider, prompt, InteractionStage.MainReply),
					CancellationToken.None)
				.GetAwaiter()
				.GetResult();
			string controlMode = "shared-gateway";
			if (generated == null || generated.Status != LlmResultStatus.Succeeded)
			{
				int? statusCode = generated?.Metadata?.StatusCode;
				error = statusCode.HasValue
					? new AnalysisFailure(AnalysisFailureKind.Http, statusCode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
					: new AnalysisFailure(AnalysisFailureKind.Request, generated?.ErrorCode ?? "empty_result");
				Logger.Log("AiErrorAnalysis", "Analysis request failed status=" + (statusCode?.ToString() ?? "unknown") + " model=" + modelName + " mode=" + controlMode);
				return false;
			}
			analysis = NormalizeText(generated.RawText, "");
			if (string.IsNullOrWhiteSpace(analysis))
			{
				error = new AnalysisFailure(AnalysisFailureKind.EmptyReply);
				return false;
			}
			// 分析结果直接显示在左下角，因此保留模型完整回复，不再为旧弹窗做额外截断。
			Logger.Log("AiErrorAnalysis", "Analysis completed model=" + modelName + " inputChars=" + detail.Length + " outputChars=" + analysis.Length + " mode=" + controlMode);
			return true;
		}
		catch (OperationCanceledException)
		{
			error = new AnalysisFailure(AnalysisFailureKind.Timeout);
			return false;
		}
		catch (Exception ex)
		{
			error = new AnalysisFailure(AnalysisFailureKind.Exception, ex.GetType().Name + ": " + ex.Message);
			Logger.Log("AiErrorAnalysis", "Analysis request exception: " + error.Detail);
			return false;
		}
	}

	private static void ShowAnalysisResult(string originalTitle, string analysis, Action onClosed)
	{
		// 分析结果是信息展示而非新决策，必须和原错误一样留在左下角，不再创建遮挡选项的结果弹窗。
		NonBlockingErrorReport.Show(
			UiFormat("AF_UI_ErrorAnalysis_ResultTitle", "AI error analysis - {TITLE}", ("TITLE", NormalizeText(originalTitle, Ui("AF_UI_ErrorAnalysis_ErrorTitle", "AnimusForge error")))),
			NormalizeText(analysis, Ui("AF_UI_ErrorAnalysis_EmptyAnalysis", "No analysis was generated.")),
			rememberForAnalysis: false);
		SafeInvoke(onClosed);
	}

	private static void ShowAnalysisFailure(string originalTitle, string originalDetail, AnalysisFailure error, Action onClosed)
	{
		string text = UiFormat("AF_UI_ErrorAnalysis_FailureBody", "The preprocessing API could not complete the error analysis.\n\n[Analysis request error]\n{ERROR}\n\n[Original error]\n{TITLE}\n{DETAIL}",
			("ERROR", ResolveAnalysisFailure(error)),
			("TITLE", NormalizeText(originalTitle, Ui("AF_UI_ErrorAnalysis_ErrorTitle", "AnimusForge error"))),
			("DETAIL", NormalizeText(originalDetail, Ui("AF_UI_Retry_UnknownError", "Unknown error"))));
		// AI 分析失败本身也是纯诊断报告，保持为左下角消息并恢复原来的关闭回调。
		NonBlockingErrorReport.Show(Ui("AF_UI_ErrorAnalysis_FailedTitle", "AI analysis failed"), text, rememberForAnalysis: false);
		SafeInvoke(onClosed);
	}

	private static string ResolveAnalysisFailure(AnalysisFailure error)
	{
		if (error == null)
		{
			return Ui("AF_UI_Retry_UnknownError", "Unknown error");
		}
		switch (error.Kind)
		{
			case AnalysisFailureKind.ConfigurationUnavailable:
				return Ui("AF_UI_ErrorAnalysis_ConfigUnavailable", "Could not read the MCM preprocessing API configuration.");
			case AnalysisFailureKind.ConfigurationIncomplete:
				return Ui("AF_UI_ErrorAnalysis_ConfigIncomplete", "The preprocessing API URL, key, or model name is not fully configured.");
			case AnalysisFailureKind.Http:
				return UiFormat("AF_UI_ErrorAnalysis_HttpFailure", "The preprocessing API returned HTTP {STATUS}.", ("STATUS", error.Detail));
			case AnalysisFailureKind.Request:
				return UiFormat("AF_UI_ErrorAnalysis_RequestFailure", "The preprocessing API request failed: {CODE}", ("CODE", error.Detail));
			case AnalysisFailureKind.EmptyReply:
				return Ui("AF_UI_ErrorAnalysis_EmptyReply", "The preprocessing API responded, but no usable model reply could be parsed.");
			case AnalysisFailureKind.Timeout:
				return Ui("AF_UI_ErrorAnalysis_Timeout", "The preprocessing API analysis exceeded 60 seconds; the request was canceled.");
			default:
				return NormalizeText(error.Detail, Ui("AF_UI_Retry_UnknownError", "Unknown error"));
		}
	}

	private static string LimitForAnalysis(string value)
	{
		string text = NormalizeText(value, AdapterPromptCaptureTexts.PcapText("error_analysis_unknown"));
		if (text.Length <= AnalysisMaxInputChars)
		{
			return text;
		}
		int headLength = AnalysisMaxInputChars * 2 / 3;
		int tailLength = AnalysisMaxInputChars - headLength;
		return text.Substring(0, headLength)
			+ AdapterPromptCaptureTexts.PcapText("error_analysis_truncated")
			+ text.Substring(text.Length - tailLength);
	}

	private static string NormalizeText(string value, string fallback)
	{
		string text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		return string.IsNullOrWhiteSpace(text) ? (fallback ?? "") : text;
	}

	private static string RedactConfiguredApiKeys(string value, DuelSettings settings)
	{
		string text = value ?? "";
		if (settings == null)
		{
			return text;
		}
		string[] keys = new string[4]
		{
			settings.ApiKey,
			settings.AuxiliaryApiKey,
			settings.ActionPostprocessApiKey,
			settings.EventAndRebellionApiKey
		};
		for (int i = 0; i < keys.Length; i++)
		{
			string key = (keys[i] ?? "").Trim();
			if (key.Length >= 6)
			{
				text = text.Replace(key, AdapterPromptCaptureTexts.PcapText("error_analysis_redacted"));
			}
		}
		return text;
	}

	private static string BuildCacheKey(string title, string detail)
	{
		unchecked
		{
			ulong hash = 14695981039346656037UL;
			string text = (title ?? "") + "\n" + (detail ?? "");
			for (int i = 0; i < text.Length; i++)
			{
				hash ^= text[i];
				hash *= 1099511628211UL;
			}
			return text.Length + ":" + hash.ToString("X16");
		}
	}

	private static bool TryGetCachedAnalysis(string key, out string analysis)
	{
		lock (AnalysisCacheLock)
		{
			return AnalysisCache.TryGetValue(key ?? "", out analysis);
		}
	}

	private static void StoreCachedAnalysis(string key, string analysis)
	{
		if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(analysis))
		{
			return;
		}
		lock (AnalysisCacheLock)
		{
			if (AnalysisCache.ContainsKey(key))
			{
				AnalysisCache[key] = analysis;
				return;
			}
			while (AnalysisCache.Count >= AnalysisCacheCapacity && AnalysisCacheOrder.Count > 0)
			{
				AnalysisCache.Remove(AnalysisCacheOrder.Dequeue());
			}
			AnalysisCache[key] = analysis;
			AnalysisCacheOrder.Enqueue(key);
		}
	}

	private static string Ui(string id, string fallback)
	{
		return new TextObject("{=" + id + "}" + fallback).ToString();
	}

	private static string UiFormat(string id, string fallback, params (string Key, object Value)[] variables)
	{
		return DeveloperUiText.Format(id, fallback, variables);
	}

	private static void SafeInvoke(Action action)
	{
		try
		{
			action?.Invoke();
		}
		catch (Exception ex)
		{
			Logger.Log("AiErrorAnalysis", "Original popup close callback failed: " + ex.Message);
		}
	}
}
