using System;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public static class LlmRetryPrompt
{
	private const int MaxErrorPreviewChars = 900;

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
			|| text.StartsWith("timeout_", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("http_", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("empty_content", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return text.IndexOf("TaskCanceledException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("HttpRequestException", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("operation was canceled", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("A task was canceled", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static string BuildRetryDescription(string stageName, string error)
	{
		string stage = string.IsNullOrWhiteSpace(stageName) ? "LLM请求" : stageName.Trim();
		string detail = (error ?? "未知错误").Replace("\r", " ").Replace("\n", " ").Trim();
		if (detail.Length > MaxErrorPreviewChars)
		{
			detail = detail.Substring(0, MaxErrorPreviewChars) + "...";
		}
		return stage + "失败：\n\n" + detail + "\n\n是否立即重试？";
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
				InformationManager.ShowInquiry(new InquiryData(
					"AnimusForge 请求失败",
					BuildRetryDescription(stageName, error),
					isAffirmativeOptionShown: true,
					isNegativeOptionShown: true,
					"重试",
					"放弃",
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

		SynchronizationContext context = _mainThreadContext;
		if (context != null && (_mainThreadId == 0 || Thread.CurrentThread.ManagedThreadId != _mainThreadId))
		{
			try
			{
				context.Post(_ => Show(), null);
				return;
			}
			catch
			{
			}
		}
		Show();
	}
}
