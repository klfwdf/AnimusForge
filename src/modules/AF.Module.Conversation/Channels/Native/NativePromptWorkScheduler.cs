using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static AnimusForge.ShoutBehavior;
namespace AnimusForge;
internal static class NativePromptWorkScheduler
{
private static bool TryAcquireNativeConversationBackgroundPreprocessSlot(long requestId, long runtimeGeneration, string target, int targetAgentIndex)
	{
		for (int attempt = 0; attempt < 2; attempt++)
		{
			long activeRequestId = Interlocked.Read(ref _nativeConversationBackgroundPreprocessActiveRequestId);
			if (activeRequestId == 0L)
			{
				if (Interlocked.CompareExchange(ref _nativeConversationBackgroundPreprocessActiveRequestId, requestId, 0L) == 0L)
				{
					Interlocked.Exchange(ref _nativeConversationBackgroundPreprocessActiveGeneration, runtimeGeneration);
					return true;
				}
				continue;
			}
			long activeGeneration = Interlocked.Read(ref _nativeConversationBackgroundPreprocessActiveGeneration);
			long currentGeneration = SaveRuntimeGuard.CurrentGeneration;
			if (activeGeneration > 0L && activeGeneration != currentGeneration)
			{
				if (Interlocked.CompareExchange(ref _nativeConversationBackgroundPreprocessActiveRequestId, requestId, activeRequestId) == activeRequestId)
				{
					Interlocked.Exchange(ref _nativeConversationBackgroundPreprocessActiveGeneration, runtimeGeneration);
					Logger.Log("ShoutBehavior", "[NativeConversation] replaced stale preprocess owner target=" + target + " agent=" + targetAgentIndex + " oldRequest=" + activeRequestId + " oldGeneration=" + activeGeneration + " currentGeneration=" + currentGeneration + " newRequest=" + requestId);
					FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_stale_owner_replaced", "target=" + target + " agent=" + targetAgentIndex + " oldRequest=" + activeRequestId + " oldGeneration=" + activeGeneration + " currentGeneration=" + currentGeneration + " newRequest=" + requestId, immediate: true);
					return true;
				}
			}
		}
		long busyRequestId = Interlocked.Read(ref _nativeConversationBackgroundPreprocessActiveRequestId);
		long busyGeneration = Interlocked.Read(ref _nativeConversationBackgroundPreprocessActiveGeneration);
		Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background busy target=" + target + " agent=" + targetAgentIndex + " activeRequest=" + busyRequestId + " activeGeneration=" + busyGeneration + " currentGeneration=" + SaveRuntimeGuard.CurrentGeneration);
		FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_busy", "target=" + target + " agent=" + targetAgentIndex + " activeRequest=" + busyRequestId + " activeGeneration=" + busyGeneration, immediate: true);
		return false;
	}

internal static Task<MyBehavior.ShoutPromptContext> RunNativeConversationBackgroundPreprocessAsync(string targetLog, int targetAgentIndex, long runtimeGeneration, Func<MyBehavior.ShoutPromptContext> func)
	{
		string target = string.IsNullOrWhiteSpace(targetLog) ? "unknown" : targetLog.Trim();
		long requestId = Interlocked.Increment(ref _nativeConversationBackgroundPreprocessSequence);
		if (func == null)
		{
			return Task.FromResult(CreateEmptyNativeConversationPromptContext());
		}
		if (!TryAcquireNativeConversationBackgroundPreprocessSlot(requestId, runtimeGeneration, target, targetAgentIndex))
		{
			return Task.FromResult<MyBehavior.ShoutPromptContext>(null);
		}
		return Task.Factory.StartNew(delegate
		{
			ThreadPriority previousPriority = Thread.CurrentThread.Priority;
			Stopwatch sw = Stopwatch.StartNew();
			try
			{
				try
				{
					Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
				}
				catch
				{
				}
				Logger.Log("Logic", "[NativePerf] preprocess_context_background_start target=" + target + " agent=" + targetAgentIndex + " request=" + requestId + " callerThread=" + Thread.CurrentThread.ManagedThreadId + " knowledge=" + AIConfigHandler.KnowledgeRetrievalEnabled + " semanticFirst=" + AIConfigHandler.KnowledgeSemanticFirst + " topK=" + AIConfigHandler.KnowledgeSemanticTopK);
				FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_start", "target=" + target + " agent=" + targetAgentIndex + " request=" + requestId + " thread=" + Thread.CurrentThread.ManagedThreadId + " onnx=True", immediate: true);
				if (SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_preprocess_background_before_run"))
				{
					return CreateEmptyNativeConversationPromptContext();
				}
				using (FreezeWatchdog.Scope("NativeConversation.preprocess_context.background"))
				{
					MyBehavior.ShoutPromptContext ctx = func() ?? CreateEmptyNativeConversationPromptContext();
					sw.Stop();
					Logger.Log("Logic", "[NativePerf] preprocess_context_background_done target=" + target + " agent=" + targetAgentIndex + " request=" + requestId + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " stale=" + SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_preprocess_background_after_run") + " hits=" + ((ctx.PreprocessRuleIds == null || ctx.PreprocessRuleIds.Count == 0) ? "(none)" : string.Join(",", ctx.PreprocessRuleIds)) + " extrasLen=" + ((ctx.Extras ?? "").Length));
					FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_done", "target=" + target + " agent=" + targetAgentIndex + " request=" + requestId + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " extrasLen=" + ((ctx.Extras ?? "").Length), immediate: true);
					return ctx;
				}
			}
			catch (PreprocessFormatException)
			{
				sw.Stop();
				FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_format_exception", "target=" + target + " agent=" + targetAgentIndex + " request=" + requestId + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
				throw;
			}
			catch (Exception ex)
			{
				sw.Stop();
				Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background failed target=" + target + " agent=" + targetAgentIndex + " request=" + requestId + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " error=" + ex.Message);
				FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_exception", ex.GetType().Name + ": " + ex.Message + " target=" + target + " agent=" + targetAgentIndex + " request=" + requestId, immediate: true);
				return CreateEmptyNativeConversationPromptContext();
			}
			finally
			{
				if (Interlocked.CompareExchange(ref _nativeConversationBackgroundPreprocessActiveRequestId, 0L, requestId) == requestId)
				{
					Interlocked.Exchange(ref _nativeConversationBackgroundPreprocessActiveGeneration, 0L);
				}
				try
				{
					Thread.CurrentThread.Priority = previousPriority;
				}
				catch
				{
				}
			}
		}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
	}

internal static async Task<MyBehavior.ShoutPromptContext> AwaitNativeConversationBackgroundPreprocessAsync(Task<MyBehavior.ShoutPromptContext> task, string targetLog, int targetAgentIndex, long runtimeGeneration)
	{
		string target = string.IsNullOrWhiteSpace(targetLog) ? "unknown" : targetLog.Trim();
		if (task == null)
		{
			return null;
		}
		try
		{
			Task completed = await Task.WhenAny(task, Task.Delay(NativeConversationBackgroundPreprocessTimeoutMs)).ConfigureAwait(false);
			if (ReferenceEquals(completed, task))
			{
				return await task.ConfigureAwait(false);
			}
			long timeoutCount = Interlocked.Increment(ref _nativeConversationBackgroundPreprocessTimeoutCount);
			Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background timeout target=" + target + " agent=" + targetAgentIndex + " timeoutMs=" + NativeConversationBackgroundPreprocessTimeoutMs + " taskStatus=" + task.Status + " runtimeGeneration=" + runtimeGeneration + " currentGeneration=" + SaveRuntimeGuard.CurrentGeneration + " timeoutCount=" + timeoutCount);
			FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_timeout", "target=" + target + " agent=" + targetAgentIndex + " timeoutMs=" + NativeConversationBackgroundPreprocessTimeoutMs + " status=" + task.Status + " timeoutCount=" + timeoutCount, immediate: true);
			ObserveNativeConversationBackgroundPreprocessLateCompletion(task, target, targetAgentIndex, timeoutCount);
			return null;
		}
		catch (PreprocessFormatException)
		{
			throw;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background await failed target=" + target + " agent=" + targetAgentIndex + " error=" + ex.Message);
			FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_await_exception", ex.GetType().Name + ": " + ex.Message + " target=" + target + " agent=" + targetAgentIndex, immediate: true);
			return CreateEmptyNativeConversationPromptContext();
		}
	}

private static void ObserveNativeConversationBackgroundPreprocessLateCompletion(Task<MyBehavior.ShoutPromptContext> task, string target, int targetAgentIndex, long timeoutCount)
	{
		try
		{
			task.ContinueWith(delegate(Task<MyBehavior.ShoutPromptContext> completedTask)
			{
				try
				{
					if (completedTask.IsCanceled)
					{
						Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background late canceled target=" + target + " agent=" + targetAgentIndex + " timeoutCount=" + timeoutCount);
						FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_late_canceled", "target=" + target + " agent=" + targetAgentIndex + " timeoutCount=" + timeoutCount, immediate: true);
						return;
					}
					if (completedTask.IsFaulted)
					{
						Exception lateEx = completedTask.Exception?.GetBaseException();
						Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background late fault target=" + target + " agent=" + targetAgentIndex + " timeoutCount=" + timeoutCount + " error=" + (lateEx?.Message ?? "unknown"));
						FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_late_fault", (lateEx?.GetType().Name ?? "Exception") + ": " + (lateEx?.Message ?? "") + " target=" + target + " agent=" + targetAgentIndex + " timeoutCount=" + timeoutCount, immediate: true);
						return;
					}
					MyBehavior.ShoutPromptContext ctx = completedTask.Result;
					Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background late complete target=" + target + " agent=" + targetAgentIndex + " timeoutCount=" + timeoutCount + " hits=" + ((ctx?.PreprocessRuleIds == null || ctx.PreprocessRuleIds.Count == 0) ? "(none)" : string.Join(",", ctx.PreprocessRuleIds)) + " extrasLen=" + ((ctx?.Extras ?? "").Length));
					FreezeWatchdog.Mark("NativeConversation.preprocess_context_background_late_complete", "target=" + target + " agent=" + targetAgentIndex + " timeoutCount=" + timeoutCount + " extrasLen=" + ((ctx?.Extras ?? "").Length), immediate: true);
				}
				catch (Exception ex)
				{
					Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background late observer failed target=" + target + " agent=" + targetAgentIndex + " error=" + ex.Message);
				}
			}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] preprocess background late observer registration failed target=" + target + " agent=" + targetAgentIndex + " error=" + ex.Message);
		}
	}

private static long _nativeConversationBackgroundPreprocessSequence;

private static long _nativeConversationBackgroundPreprocessActiveRequestId;

private static long _nativeConversationBackgroundPreprocessActiveGeneration;

private static long _nativeConversationBackgroundPreprocessTimeoutCount;

private const int NativeConversationBackgroundPreprocessTimeoutMs = 480000;
}
