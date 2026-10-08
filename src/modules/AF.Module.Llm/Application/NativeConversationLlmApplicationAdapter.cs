using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;

namespace AnimusForge.Refactor.Adapters;

internal static class NativeConversationLlmApplicationAdapter
{
    private const int NativeConversationMainReplyTimeoutMs = 180000;
	internal static async Task<string> CallNativeConversationApiAsync(List<object> messages, Action<string> onStreamText, ConversationSpeechTextOptions speechOptions, CancellationToken cancellationToken = default(CancellationToken))
	{
		Stopwatch nativeApiWatchSw = Stopwatch.StartNew();
		using CancellationTokenSource requestTimeout = LlmNonStreamingTransport.CreateTimeout(NativeConversationMainReplyTimeoutMs, cancellationToken);
		if (onStreamText == null)
		{
			FreezeWatchdog.Mark("NativeConversation.api_non_stream_start", "messages=" + (messages?.Count ?? 0) + " timeoutMs=" + NativeConversationMainReplyTimeoutMs, immediate: true);
			string result;
			try
			{
				result = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(messages, 5000, promptRetryOnError: false, cancellationToken: requestTimeout.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (requestTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
			{
				return "（API请求失败: 原生对话正文生成超时 " + NativeConversationMainReplyTimeoutMs + "ms）";
			}
			cancellationToken.ThrowIfCancellationRequested();
			if (requestTimeout.IsCancellationRequested)
				return "（API请求失败: 原生对话正文生成超时 " + NativeConversationMainReplyTimeoutMs + "ms）";
			FreezeWatchdog.Mark("NativeConversation.api_non_stream_done", "resultLen=" + ((result ?? "").Length) + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return result;
		}
		FreezeWatchdog.Mark("NativeConversation.api_stream_start", "messages=" + (messages?.Count ?? 0) + " timeoutMs=" + NativeConversationMainReplyTimeoutMs, immediate: true);
		StringBuilder streamed = new StringBuilder();
		LlmVisibleReplyNormalizer.StreamFilter visibleReplyFilter = new LlmVisibleReplyNormalizer.StreamFilter();
		string completed = "";
		string error = "";
		CancellationTokenSource timeoutCts = requestTimeout;
		await LegacyShoutNetworkGateway.SendLegacyMessagesStreamAsync(messages, 5000, delegate(string delta)
		{
			if (timeoutCts.IsCancellationRequested || string.IsNullOrEmpty(delta))
			{
				return;
			}
			string visibleDelta = visibleReplyFilter.Push(delta);
			if (string.IsNullOrEmpty(visibleDelta))
			{
				return;
			}
			streamed.Append(visibleDelta);
			string visible = ScenePromptMessageProjectionComposer.BuildNativeConversationStreamingVisibleText(streamed.ToString(), speechOptions);
			if (!string.IsNullOrWhiteSpace(visible))
			{
				onStreamText(visible);
			}
		}, delegate(string full)
		{
			if (timeoutCts.IsCancellationRequested) return;
			string finalDelta = visibleReplyFilter.Complete(full ?? "");
			if (!string.IsNullOrEmpty(finalDelta))
			{
				streamed.Append(finalDelta);
			}
			completed = (visibleReplyFilter.NormalizedText ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(completed))
			{
				onStreamText(ScenePromptMessageProjectionComposer.BuildNativeConversationStreamingVisibleText(completed, speechOptions));
			}
		}, delegate(string err)
		{
			error = (err ?? "").Trim();
		}, timeoutCts.Token, promptRetryOnError: false).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		if (timeoutCts.IsCancellationRequested && string.IsNullOrWhiteSpace(completed) && streamed.Length == 0 && string.IsNullOrWhiteSpace(error))
		{
			Logger.Log("NativeConversation", "[WARN] main reply timed out before first stream chunk. timeoutMs=" + NativeConversationMainReplyTimeoutMs);
			FreezeWatchdog.Mark("NativeConversation.api_stream_timeout", "elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return "（API请求失败: 原生对话正文生成超时 " + NativeConversationMainReplyTimeoutMs + "ms）";
		}
		if (!string.IsNullOrWhiteSpace(completed))
		{
			FreezeWatchdog.Mark("NativeConversation.api_stream_done", "completedLen=" + completed.Length + " streamedLen=" + streamed.Length + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return completed;
		}
		string fallback = streamed.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(fallback))
		{
			FreezeWatchdog.Mark("NativeConversation.api_stream_fallback", "streamedLen=" + fallback.Length + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return fallback;
		}
		FreezeWatchdog.Mark("NativeConversation.api_stream_error", "errorLen=" + ((error ?? "").Length) + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
		return error ?? "";
	}
}
