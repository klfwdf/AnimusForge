using System;

using System.Collections.Concurrent;

using TaleWorlds.Core;

using TaleWorlds.Library;

using TaleWorlds.Localization;



namespace AnimusForge;



/// <summary>

/// Sends single-action failures to Bannerlord's lower-left message feed.

/// Full redacted diagnostics are shown in the message feed and additionally kept in the mod log.

/// </summary>

public static class NonBlockingErrorReport

{

	private const long DuplicateNotificationWindowTicks = TimeSpan.TicksPerSecond * 2;

	private static readonly ConcurrentQueue<Action> DeferredAcknowledgements = new ConcurrentQueue<Action>();

	private static readonly object NotificationLock = new object();

	private static string _lastNotificationKey = "";

	private static long _lastNotificationUtcTicks;



	/// <summary>

	/// Preserves full redacted diagnostics; an optional display projection changes only the visible notification.

	/// This method is only called from UI/main-thread paths or LlmRetryPrompt's main-thread dispatcher.

	/// </summary>

	public static void Show(string title, string detail, bool rememberForAnalysis = true)

		=> Show(title, detail, rememberForAnalysis, null);



	internal static void Show(string title, string detail, bool rememberForAnalysis, string displayDetail)

	{

		string normalizedTitle = Normalize(title, new TextObject("{=AF_UI_ErrorReport_OperationFailed}AnimusForge operation failed").ToString());

		string normalizedDetail = RedactConfiguredApiKeys(Normalize(detail, new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()));

		// The display projection never replaces raw diagnostics in logs, analysis or duplicate suppression.

		string visibleDetail = displayDetail == null ? normalizedDetail : RedactConfiguredApiKeys(Normalize(displayDetail, normalizedDetail));

		string notification = BuildVisibleMessage(normalizedTitle, visibleDetail);

		// 保留最近一次自动错误，供玩家主动从终端请求 AI 分析，避免移除旧弹窗后丢失该功能。

		if (rememberForAnalysis)

		{

			AiErrorAnalysisInquiry.RememberFailure(normalizedTitle, normalizedDetail);

		}

		if (ShouldSuppressDuplicate(normalizedTitle, normalizedDetail))

		{

			return;

		}

		// 相同故障短时间内只保留一次完整 HUD 消息与日志，防止重试循环造成无效分配和磁盘写入。

		Logger.Log("NonBlockingErrorReport", "title=" + normalizedTitle + "\n" + normalizedDetail);

		bool displayed = false;

		try

		{

			InformationManager.DisplayMessage(new InformationMessage(notification, Colors.Red));

			displayed = true;

		}

		catch (Exception ex)

		{

			Logger.Log("NonBlockingErrorReport", "Failed to display the lower-left error message: " + ex.Message);

		}

		if (displayed && rememberForAnalysis)

		{

			// 先让完整错误进入左下角；AI 分析确认框由主线程 tick 以优先队列方式显示，原操作窗口会自动恢复。

			AiErrorAnalysisInquiry.RequestLatestFailureAnalysis();

		}

	}



	/// <summary>

/// Converts one-button error reports; confirmations and retry/abort decisions remain modal.

	/// </summary>

	internal static bool TryRouteAcknowledgementInquiry(InquiryData data)

	{

		if (!CanRouteAcknowledgementInquiry(data))

		{

			return false;

		}

		Show(data.TitleText, data.Text);

		QueueAcknowledgement(data.AffirmativeAction);

		return true;

	}



	/// <summary>

	/// Runs acknowledgement callbacks one application tick later to avoid reopening UI during Harmony's ShowInquiry prefix.

	/// </summary>

	public static void OnApplicationTick()

	{

		if (!DeferredAcknowledgements.TryDequeue(out Action acknowledgement))

		{

			return;

		}

		try

		{

			acknowledgement?.Invoke();

		}

		catch (Exception ex)

		{

			Logger.Log("NonBlockingErrorReport", "Error report confirmation callback failed: " + ex.Message);

		}

	}



	private static bool CanRouteAcknowledgementInquiry(InquiryData data)

	{

		if (data == null

			|| !data.IsAffirmativeOptionShown

			|| data.IsNegativeOptionShown

			|| data.NegativeAction != null

			|| !string.IsNullOrWhiteSpace(data.NegativeText)

			|| data.TimeoutAction != null

			|| HasSuccessfulTitle(data.TitleText)

			|| !HasFailureSignal(data.TitleText, data.Text))

		{

			return false;

		}

		return true;

	}



	private static bool HasSuccessfulTitle(string titleValue)

	{

		string title = titleValue ?? "";

		return !HasFailureSignal(title, null)

			&& (title.IndexOf(LlmSourceTexts.Get("Signal_Completed"), StringComparison.Ordinal) >= 0

				|| title.IndexOf(LlmSourceTexts.Get("Signal_Success"), StringComparison.Ordinal) >= 0

				|| title.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0

				|| title.IndexOf("succeeded", StringComparison.OrdinalIgnoreCase) >= 0

				|| title.IndexOf("successful", StringComparison.OrdinalIgnoreCase) >= 0

				|| title.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0);

	}



	private static bool HasFailureSignal(string titleValue, string detailValue)

	{

		string text = ((titleValue ?? "") + "\n" + (detailValue ?? "")).Trim();

		// 标题中性但正文明确说明未初始化/不可用时，同样属于纯错误报告而非详情页。

		return text.IndexOf(LlmSourceTexts.Get("Signal_Failed"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Error"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Exception"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_ErrorDetail"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Cannot"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Unavailable"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Uninitialized"), StringComparison.Ordinal) >= 0

			// 部分既有纯报告标题使用“被阻塞/超时”等措辞；同样不能让其原始响应重新覆盖操作界面。

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Blocked"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Timeout"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Interrupted"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Refused"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Invalid"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_Unconfigured"), StringComparison.Ordinal) >= 0

			|| text.IndexOf(LlmSourceTexts.Get("Signal_NotFound"), StringComparison.Ordinal) >= 0

			|| text.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("failure", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("blocked", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0

			|| text.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0;

	}



	private static void QueueAcknowledgement(Action acknowledgement)

	{

		if (acknowledgement != null)

		{

			DeferredAcknowledgements.Enqueue(acknowledgement);

		}

	}



	private static string BuildVisibleMessage(string title, string detail)

	{

		// 用户需要在左下角直接阅读模型回复和 API 原始响应，不能在此处再裁剪或移除任何诊断段落。

		var text = new TextObject("{=AF_UI_ErrorReport_VisibleMessage}[AnimusForge] {TITLE}:\n{DETAIL}");

		text.SetTextVariable("TITLE", title);

		text.SetTextVariable("DETAIL", Normalize(detail, new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()));

		return text.ToString();

	}



	private static bool ShouldSuppressDuplicate(string title, string detail)

	{

		string key = title + "\n" + detail;

		long now = DateTime.UtcNow.Ticks;

		lock (NotificationLock)

		{

			if (string.Equals(key, _lastNotificationKey, StringComparison.Ordinal)

				&& now - _lastNotificationUtcTicks < DuplicateNotificationWindowTicks)

			{

				return true;

			}

			_lastNotificationKey = key;

			_lastNotificationUtcTicks = now;

			return false;

		}

	}



	private static string RedactConfiguredApiKeys(string value)

	{

		string text = value ?? "";

		try

		{

			DuelSettings settings = DuelSettings.GetSettings();

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

					text = text.Replace(key, "[API key redacted]");

				}

			}

		}

		catch

		{

			// 错误通知不能因设置读取失败而再次打断原流程。

		}

		return text;

	}



	private static string Normalize(string value, string fallback)

	{

		string text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();

		return string.IsNullOrWhiteSpace(text) ? (fallback ?? "") : text;

	}

}

