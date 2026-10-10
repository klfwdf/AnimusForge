using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public static class LlmRetryPrompt
{
	private static SynchronizationContext _mainThreadContext;
	private static int _mainThreadId;

	public static void CaptureMainThreadContext()
	{
		SynchronizationContext context = SynchronizationContext.Current;
		if (context == null)
		{
			return;
		}
		_mainThreadContext = context;
		_mainThreadId = Thread.CurrentThread.ManagedThreadId;
	}

	public static bool IsRetryableLlmError(string error)
	{
		string text = (error ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("（API请求失败", StringComparison.Ordinal)
			|| text.StartsWith("（程序错误", StringComparison.Ordinal)
			|| text.StartsWith("（API响应格式错误", StringComparison.Ordinal)
			|| text.StartsWith("（错误", StringComparison.Ordinal)
			|| text.StartsWith("timeout_", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("http_", StringComparison.OrdinalIgnoreCase)
			|| text.EndsWith("_config_invalid", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("empty_content", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return text.IndexOf("TaskCanceledException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("HttpRequestException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("JsonReaderException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("JsonSerializationException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("operation was canceled", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("A task was canceled", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static string BuildRetryDescription(string stageName, string error)
	{
		string stage = RetryStageDisplay(stageName);
		// 重试窗口只承载“重试/放弃”这个必要决策；完整诊断已在 ShowRetryPrompt 中转到左下角和日志。
		var text = new TextObject("{=AF_UI_Retry_Description}{STAGE} failed. The full error was shown in the lower-left message feed and written to the log.\n\nRetry now?");
		text.SetTextVariable("STAGE", stage);
		return text.ToString();
	}

	private static string RetryStageDisplay(string stageName)
	{
		string stage = (stageName ?? "").Trim();
		switch (stage)
		{
			case "": case "LLM请求": return new TextObject("{=AF_UI_Retry_StageRequest}LLM request").ToString();
			case "动作后处理": return new TextObject("{=AF_UI_Retry_StagePostprocess}Action postprocessing").ToString();
			case "辅助对话": return new TextObject("{=AF_UI_Retry_StageAuxiliary}Auxiliary conversation").ToString();
			case "前处理规则选择": return new TextObject("{=AF_UI_Retry_StagePreprocess}Preprocessing rule selection").ToString();
			case "正文生成": return new TextObject("{=AF_UI_Retry_StageReply}Reply generation").ToString();
			case "信使回信正文生成": return new TextObject("{=AF_UI_Retry_StageCourierReply}Courier reply generation").ToString();
			case "信使来信正文生成": return new TextObject("{=AF_UI_Retry_StageCourierLetter}Courier letter generation").ToString();
			default: return stage;
		}
	}

	private static string RetryFailureDisplay(string stageName, string error)
	{
		var text = new TextObject("{=AF_UI_Retry_FailureDetail}{STAGE} failed:\n\n{DETAIL}");
		text.SetTextVariable("STAGE", RetryStageDisplay(stageName));
		text.SetTextVariable("DETAIL", NormalizeFullText(error, new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()));
		return text.ToString();
	}

	public static string BuildFailureDetail(string reason, string modelReply, string rawResponse = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(NormalizeFullText(reason, "未知错误"));
		string fullModelReply = NormalizeFullText(modelReply, "");
		string fullRawResponse = NormalizeFullText(rawResponse, "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【模型回复（完整）】");
		stringBuilder.Append(string.IsNullOrWhiteSpace(fullModelReply) ? "（未收到或未能解析出模型回复）" : fullModelReply);
		if (!string.IsNullOrWhiteSpace(fullRawResponse) && !string.Equals(fullRawResponse, fullModelReply, StringComparison.Ordinal))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【API原始响应（完整）】");
			stringBuilder.Append(fullRawResponse);
		}
		return stringBuilder.ToString().TrimEnd();
	}

	public static void ShowFailurePopup(string title, string message)
	{
		void Show()
		{
			// 纯 LLM 失败报告改走左下角消息，避免完整 API 响应覆盖正在操作的菜单。
			NonBlockingErrorReport.Show(
				string.IsNullOrWhiteSpace(title) ? new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString() : title.Trim(),
				NormalizeFullText(message, new TextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString()));
		}

		PostToMainThread(Show);
	}

	public static Task<bool> PromptRetryAsync(string stageName, string error)
	{
		if (!IsRetryableLlmError(error))
		{
			return Task.FromResult(false);
		}
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
		ShowRetryPrompt(stageName, error, completion);
		return completion.Task;
	}

	public static bool PromptRetryBlocking(string stageName, string error)
	{
		if (!IsRetryableLlmError(error))
		{
			return false;
		}
		if (_mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == _mainThreadId)
		{
			ShowFailurePopup(new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(), RetryFailureDisplay(stageName, error));
			return false;
		}
		using ManualResetEventSlim waitHandle = new ManualResetEventSlim(false);
		bool retry = false;
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
		completion.Task.ContinueWith(delegate(Task<bool> task)
		{
			try
			{
				retry = task.Status == TaskStatus.RanToCompletion && task.Result;
			}
			catch
			{
				retry = false;
			}
			waitHandle.Set();
		});
		ShowRetryPrompt(stageName, error, completion);
		waitHandle.Wait();
		return retry;
	}

	private static void ShowRetryPrompt(string stageName, string error, TaskCompletionSource<bool> completion)
	{
		void Show()
		{
			try
			{
				// 保留重试决策窗口，但把可能很长的接口响应从窗口主体迁到非阻塞消息。
				NonBlockingErrorReport.Show(
					new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(),
					RetryFailureDisplay(stageName, error));
				InformationManager.ShowInquiry(new InquiryData(
					new TextObject("{=AF_UI_Retry_RequestFailed}AnimusForge request failed").ToString(),
					BuildRetryDescription(stageName, error),
					isAffirmativeOptionShown: true,
					isNegativeOptionShown: true,
					new TextObject("{=AF_UI_Retry_Retry}Retry").ToString(),
					new TextObject("{=AF_UI_Retry_Abort}Abort").ToString(),
					delegate
					{
						completion.TrySetResult(true);
					},
					delegate
					{
						completion.TrySetResult(false);
					}),
					pauseGameActiveState: true,
					prioritize: true);
			}
			catch
			{
				completion.TrySetResult(false);
			}
		}

		PostToMainThread(Show);
	}

	private static string NormalizeFullText(string value, string fallback)
	{
		string text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		return string.IsNullOrWhiteSpace(text) ? (fallback ?? "") : text;
	}

	private static void PostToMainThread(Action action)
	{
		if (action == null)
		{
			return;
		}
		SynchronizationContext context = _mainThreadContext;
		if (context != null && (_mainThreadId == 0 || Thread.CurrentThread.ManagedThreadId != _mainThreadId))
		{
			try
			{
				context.Post(_ => action(), null);
				return;
			}
			catch
			{
			}
		}
		action();
	}
}
