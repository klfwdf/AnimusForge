using System;
using System.Collections.Concurrent;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Sends acknowledgement-only failures to Bannerlord's lower-left message feed.
/// Full diagnostics stay in the mod log so long API responses never hide the active choices.
/// </summary>
public static class NonBlockingErrorReport
{
	private const int VisibleDetailCharacterLimit = 420;
	private const long DuplicateNotificationWindowTicks = TimeSpan.TicksPerSecond * 2;
	private static readonly ConcurrentQueue<Action> DeferredAcknowledgements = new ConcurrentQueue<Action>();
	private static readonly object NotificationLock = new object();
	private static string _lastNotificationKey = "";
	private static long _lastNotificationUtcTicks;

	/// <summary>
	/// Shows a bounded, non-blocking failure summary and writes the redacted full detail to the log.
	/// This method is only called from UI/main-thread paths or LlmRetryPrompt's main-thread dispatcher.
	/// </summary>
	public static void Show(string title, string detail, bool rememberForAnalysis = true)
	{
		string normalizedTitle = Normalize(title, "AnimusForge 操作失败");
		string normalizedDetail = RedactConfiguredApiKeys(Normalize(detail, "未知错误"));
		string notification = BuildVisibleMessage(normalizedTitle, normalizedDetail);
		// 保留最近一次自动错误，供玩家主动从终端请求 AI 分析，避免移除旧弹窗后丢失该功能。
		if (rememberForAnalysis)
		{
			AiErrorAnalysisInquiry.RememberFailure(normalizedTitle, normalizedDetail);
		}
		if (ShouldSuppressDuplicate(normalizedTitle, normalizedDetail))
		{
			return;
		}
		// 相同故障短时间内只保留一次日志与 HUD 消息，防止重试循环造成无效分配和磁盘写入。
		Logger.Log("NonBlockingErrorReport", "title=" + normalizedTitle + "\n" + normalizedDetail);
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(notification, Colors.Red));
		}
		catch (Exception ex)
		{
			Logger.Log("NonBlockingErrorReport", "左下角错误消息显示失败: " + ex.Message);
		}
	}

	/// <summary>
	/// Converts only one-button acknowledgement reports; confirmations and retry/abort decisions remain modal.
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
			Logger.Log("NonBlockingErrorReport", "错误报告确认回调失败: " + ex.Message);
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
			|| !HasFailureSignal(data.TitleText, data.Text)
			|| !IsAcknowledgementCaption(data.AffirmativeText))
		{
			return false;
		}
		return true;
	}

	private static bool HasFailureSignal(string titleValue, string detailValue)
	{
		string text = ((titleValue ?? "") + "\n" + (detailValue ?? "")).Trim();
		// 标题中性但正文明确说明未初始化/不可用时，同样属于纯错误报告而非详情页。
		return text.IndexOf("失败", StringComparison.Ordinal) >= 0
			|| text.IndexOf("错误", StringComparison.Ordinal) >= 0
			|| text.IndexOf("异常", StringComparison.Ordinal) >= 0
			|| text.IndexOf("报错", StringComparison.Ordinal) >= 0
			|| text.IndexOf("无法", StringComparison.Ordinal) >= 0
			|| text.IndexOf("不可用", StringComparison.Ordinal) >= 0
			|| text.IndexOf("未初始化", StringComparison.Ordinal) >= 0
			// 部分既有纯报告标题使用“被阻塞/超时”等措辞；同样不能让其原始响应重新覆盖操作界面。
			|| text.IndexOf("阻塞", StringComparison.Ordinal) >= 0
			|| text.IndexOf("超时", StringComparison.Ordinal) >= 0
			|| text.IndexOf("中断", StringComparison.Ordinal) >= 0
			|| text.IndexOf("拒绝", StringComparison.Ordinal) >= 0
			|| text.IndexOf("无效", StringComparison.Ordinal) >= 0
			|| text.IndexOf("未配置", StringComparison.Ordinal) >= 0
			|| text.IndexOf("未找到", StringComparison.Ordinal) >= 0
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

	private static bool IsAcknowledgementCaption(string value)
	{
		string caption = (value ?? "").Trim();
		return string.IsNullOrEmpty(caption)
			|| string.Equals(caption, "知道了", StringComparison.Ordinal)
			|| string.Equals(caption, "我知道了", StringComparison.Ordinal)
			|| string.Equals(caption, "关闭", StringComparison.Ordinal)
			|| string.Equals(caption, "返回", StringComparison.Ordinal)
			|| string.Equals(caption, "返回编辑器", StringComparison.Ordinal)
			|| string.Equals(caption, "确定", StringComparison.Ordinal)
			|| string.Equals(caption, "ESC关闭", StringComparison.Ordinal);
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
		string summary = ExtractSummary(detail);
		string prefix = "[AnimusForge] " + title + "：";
		int remaining = Math.Max(0, VisibleDetailCharacterLimit - prefix.Length);
		if (summary.Length > remaining)
		{
			summary = summary.Substring(0, remaining).TrimEnd() + "…";
		}
		return prefix + summary + "（完整详情已写入 AnimusForge 日志）";
	}

	private static string ExtractSummary(string detail)
	{
		string text = Normalize(detail, "未知错误");
		int modelReplyIndex = text.IndexOf("【模型回复（完整）】", StringComparison.Ordinal);
		int rawResponseIndex = text.IndexOf("【API原始响应（完整）】", StringComparison.Ordinal);
		int cutoff = modelReplyIndex >= 0 && rawResponseIndex >= 0
			? Math.Min(modelReplyIndex, rawResponseIndex)
			: Math.Max(modelReplyIndex, rawResponseIndex);
		if (cutoff >= 0)
		{
			text = text.Substring(0, cutoff);
		}
		StringBuilder compact = new StringBuilder(text.Length);
		bool previousWasWhitespace = false;
		for (int i = 0; i < text.Length; i++)
		{
			char character = text[i];
			bool isWhitespace = char.IsWhiteSpace(character);
			if (!isWhitespace || !previousWasWhitespace)
			{
				compact.Append(isWhitespace ? ' ' : character);
			}
			previousWasWhitespace = isWhitespace;
		}
		return Normalize(compact.ToString(), "未知错误");
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
					text = text.Replace(key, "[已隐藏 API Key]");
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
