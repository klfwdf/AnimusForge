using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public static class ShoutNetwork
{
	private const int DefaultPrimaryMaxTokens = DuelSettings.DefaultGeneralApiMaxTokens;

#if DEBUG
	private static readonly object StreamingTransportOverrideLock = new object();
	private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _streamingTransportOverride;
	private static readonly object NonStreamingTransportOverrideLock = new object();
	private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _nonStreamingTransportOverride;

	/// <summary>
	/// Installs a scoped, opt-in HTTP sender for local SSE replay. The override
	/// is available only in Debug builds and affects the streaming send point
	/// exclusively; ShoutNetwork still owns SSE parsing, filtering, retries,
	/// stale checks and callbacks. It must not be used by the normal game path.
	/// </summary>
	public static IDisposable PushStreamingTransportOverrideForExternal(
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender)
	{
		if (sender == null)
		{
			throw new ArgumentNullException(nameof(sender));
		}
		lock (StreamingTransportOverrideLock)
		{
			if (_streamingTransportOverride != null)
			{
				throw new InvalidOperationException("ShoutNetwork streaming transport override is already active.");
			}
			_streamingTransportOverride = sender;
		}
		return new StreamingTransportOverrideScope(sender);
	}

	private sealed class StreamingTransportOverrideScope : IDisposable
	{
		private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sender;
		private int _disposed;

		public StreamingTransportOverrideScope(
			Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender)
		{
			_sender = sender;
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
			{
				return;
			}
			lock (StreamingTransportOverrideLock)
			{
				if (ReferenceEquals(_streamingTransportOverride, _sender))
				{
					_streamingTransportOverride = null;
				}
			}
		}
	}

	/// <summary>
	/// Installs a scoped, opt-in HTTP sender for local non-streaming replay.
	/// The override affects only the primary non-streaming send point; request
	/// construction, thinking fallback, response parsing, stale checks and
	/// legacy error text remain owned by ShoutNetwork. It is available only in
	/// Debug builds and must not be used by the normal game path.
	/// </summary>
	public static IDisposable PushNonStreamingTransportOverrideForExternal(
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender)
	{
		if (sender == null)
		{
			throw new ArgumentNullException(nameof(sender));
		}
		lock (NonStreamingTransportOverrideLock)
		{
			if (_nonStreamingTransportOverride != null)
			{
				throw new InvalidOperationException("ShoutNetwork non-streaming transport override is already active.");
			}
			_nonStreamingTransportOverride = sender;
		}
		return new NonStreamingTransportOverrideScope(sender);
	}

	private sealed class NonStreamingTransportOverrideScope : IDisposable
	{
		private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sender;
		private int _disposed;

		public NonStreamingTransportOverrideScope(
			Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender)
		{
			_sender = sender;
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
			{
				return;
			}
			lock (NonStreamingTransportOverrideLock)
			{
				if (ReferenceEquals(_nonStreamingTransportOverride, _sender))
				{
					_nonStreamingTransportOverride = null;
				}
			}
		}
	}
#endif

	private static Task<HttpResponseMessage> SendPrimaryNonStreamingRequestAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
#if DEBUG
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender = Volatile.Read(ref _nonStreamingTransportOverride);
		if (sender != null)
		{
			return sender(request, cancellationToken);
		}
#endif
		return DuelSettings.GlobalClient.SendAsync(request, cancellationToken);
	}

	private static Task<HttpResponseMessage> SendPrimaryStreamingRequestAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
#if DEBUG
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender = Volatile.Read(ref _streamingTransportOverride);
		if (sender != null)
		{
			return sender(request, cancellationToken);
		}
#endif
		return DuelSettings.GlobalClient.SendAsync(request, (HttpCompletionOption)1, cancellationToken);
	}

	private sealed class PlayerReferenceStreamFilter
	{
		private string _pending = "";

		private readonly string _lowProfileActualPlayerName = ResolveLowProfileActualPlayerNameForRedaction();

		public string Push(string text)
		{
			string text2 = (_pending ?? "") + (text ?? "");
			_pending = "";
			if (string.IsNullOrEmpty(text2))
			{
				return "";
			}
			int pendingLength = GetTrailingPlayerReferencePrefixLength(text2, _lowProfileActualPlayerName);
			if (pendingLength > 0)
			{
				_pending = text2.Substring(text2.Length - pendingLength);
				text2 = text2.Substring(0, text2.Length - pendingLength);
			}
			return ApplyPlayerDynamicNameToMainText(text2);
		}

		public string Flush()
		{
			string text = _pending ?? "";
			_pending = "";
			return ApplyPlayerDynamicNameToMainText(text);
		}

		private static int GetTrailingPlayerReferencePrefixLength(string text, string actualPlayerName)
		{
			int result = GetTrailingProperPrefixLength(text, "玩家");
			if (!string.IsNullOrWhiteSpace(actualPlayerName))
			{
				result = Math.Max(result, GetTrailingProperPrefixLength(text, actualPlayerName));
			}
			return result;
		}

		private static int GetTrailingProperPrefixLength(string text, string token)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token) || token.Length <= 1)
			{
				return 0;
			}
			int maxLength = Math.Min(text.Length, token.Length - 1);
			for (int length = maxLength; length > 0; length--)
			{
				if (text.EndsWith(token.Substring(0, length), StringComparison.Ordinal))
				{
					return length;
				}
			}
			return 0;
		}
	}

	private static string BuildTokenStatsOutputContent(string finalContent, string reasoningContent = null)
	{
		string content = ApplyPlayerDynamicNameToMainText(finalContent ?? "").Trim();
		string reasoning = (reasoningContent ?? "").Trim();
		if (string.IsNullOrWhiteSpace(reasoning))
		{
			return content;
		}
		return "[REASONING]\n" + reasoning + "\n[CONTENT]\n" + content;
	}

	private static string TrimForApiLog(string text, int maxChars = 2000)
	{
		text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, maxChars) + "...";
	}

	private static void LogPrimaryRawResponse(string phase, string body)
	{
		try
		{
			Logger.Log("ShoutNetwork", "[PrimaryChatRaw][" + phase + "] " + TrimForApiLog(body));
		}
		catch
		{
		}
	}

	private const string EmptyResponseRetryMarker = "[AF_EMPTY_RESPONSE_RETRY]";
	private const string EmptyResponseRetryInstruction = EmptyResponseRetryMarker + " 上一次模型响应为空白。请严格按既有角色、格式和字数要求，直接输出NPC本轮回复；禁止只输出空白、换行或无内容。";

	private static bool HasEmptyResponseRetryMarker(List<object> messages)
	{
		try
		{
			foreach (object message in messages ?? new List<object>())
			{
				if (TryReadMessage(message, out var _, out var content) && (content ?? "").IndexOf(EmptyResponseRetryMarker, StringComparison.Ordinal) >= 0)
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

	private const string GenericContinuationInstruction =
		"请继续完成当前请求，只输出最终结果。";

	private const string BattleSpeechContinuationInstruction =
		"请继续完成当前阵前演讲请求，只输出协议规定的最终结果，不要生成普通NPC回复。";

	private static bool IsBattleSpeechRequest(IEnumerable<object> messages)
	{
		foreach (object message in messages ?? Enumerable.Empty<object>())
		{
			if (!TryReadMessage(message, out _, out string content))
			{
				continue;
			}
			string text = content ?? string.Empty;
			if (text.IndexOf("【阵前演讲", StringComparison.Ordinal) >= 0 ||
				text.IndexOf("SPEECH_BEGIN", StringComparison.Ordinal) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string GetLastMessageRole(
		IEnumerable<object> messages,
		out string lastContent)
	{
		string lastRole = string.Empty;
		lastContent = string.Empty;
		foreach (object message in messages ?? Enumerable.Empty<object>())
		{
			if (!TryReadMessage(message, out string role, out string content))
			{
				continue;
			}
			if (string.IsNullOrWhiteSpace(role) && string.IsNullOrWhiteSpace(content))
			{
				continue;
			}
			lastRole = (role ?? string.Empty).Trim();
			lastContent = content ?? string.Empty;
		}
		return lastRole;
	}

	private static List<object> EnsureFinalUserTurn(
		IEnumerable<object> messages,
		out string originalLastRole)
	{
		List<object> result = new List<object>();
		foreach (object message in messages ?? Enumerable.Empty<object>())
		{
			result.Add(message);
		}

		string lastContent;
		originalLastRole = GetLastMessageRole(result, out lastContent);
		if (string.Equals(originalLastRole, "user", StringComparison.OrdinalIgnoreCase) &&
			!string.IsNullOrWhiteSpace(lastContent))
		{
			return result;
		}

		result.Add(new
		{
			role = "user",
			content = IsBattleSpeechRequest(result)
				? BattleSpeechContinuationInstruction
				: GenericContinuationInstruction
		});
		return result;
	}

	private static void LogNormalizedMessageTail(
		string mode,
		string originalLastRole,
		IEnumerable<object> messages)
	{
		if (string.Equals(originalLastRole, "user", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string finalRole = GetLastMessageRole(messages, out _);
		int count = messages == null ? 0 : messages.Count();
		Logger.Log(
			"ShoutNetwork",
			"[PrimaryChat] normalized message tail mode=" + (mode ?? string.Empty) +
			" originalLastRole=" + (originalLastRole ?? string.Empty) +
			" finalRole=" + finalRole +
			" messages=" + count);
	}
	private static List<object> BuildEmptyResponseRetryMessages(List<object> messages)
	{
		List<object> list = new List<object>();
		bool flag = false;
		foreach (object message in messages ?? new List<object>())
		{
			if (!flag && TryReadMessage(message, out var role, out var content) && string.Equals((role ?? "").Trim(), "system", StringComparison.OrdinalIgnoreCase))
			{
				string text = string.IsNullOrWhiteSpace(content) ? EmptyResponseRetryInstruction : (content.TrimEnd() + "\n\n" + EmptyResponseRetryInstruction);
				list.Add(new
				{
					role = role,
					content = text
				});
				flag = true;
			}
			else
			{
				list.Add(message);
			}
		}
		if (!flag)
		{
			list.Insert(0, new
			{
				role = "system",
				content = EmptyResponseRetryInstruction
			});
		}
		return EnsureFinalUserTurn(list, out _);
	}

	private static string ExtractTextFromGeminiCandidateParts(JToken candidate)
	{
		try
		{
			if (candidate == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			JToken parts = candidate.SelectToken("content.parts") ?? candidate.SelectToken("delta.content.parts");
			if (parts is JArray jArray)
			{
				foreach (JToken item in jArray)
				{
					string text = item?["text"]?.ToString() ?? "";
					if (!string.IsNullOrEmpty(text))
					{
						stringBuilder.Append(text);
					}
				}
			}
			string directText = candidate.SelectToken("content.parts[0].text")?.ToString()
				?? candidate.SelectToken("delta.content.parts[0].text")?.ToString()
				?? candidate.SelectToken("output")?.ToString();
			if (stringBuilder.Length == 0 && !string.IsNullOrEmpty(directText))
			{
				stringBuilder.Append(directText);
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return "";
		}
	}

	private static string ExtractPrimaryResponseText(JObject responseJson)
	{
		return LlmApiCompat.ExtractAssistantText(responseJson);
	}

	private static string ExtractPrimaryReasoningText(JObject responseJson)
	{
		if (responseJson == null)
		{
			return "";
		}
		return LlmApiCompat.ExtractReasoningText(responseJson);
	}

	private static string ExtractPrimaryStreamDelta(JObject chunk)
	{
		return LlmApiCompat.ExtractStreamDeltaText(chunk);
	}

	private static bool ContainsAnyIgnoreCase(string text, params string[] patterns)
	{
		text = text ?? "";
		if (patterns == null || patterns.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < patterns.Length; i++)
		{
			string text2 = (patterns[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryApplyPrimaryThinkingControls(JObject payload, DuelSettings settings, string apiUrl, string modelName, bool forceDisableThinking, out string thinkingMode)
	{
		if (forceDisableThinking)
		{
			bool controlsApplied = DuelSettings.ApplyThinkingControls(payload, apiUrl, modelName, thinkingEnabled: false, DuelSettings.ReasoningEffortHigh, out thinkingMode);
			if (!controlsApplied)
			{
				DuelSettings.RemoveThinkingControls(payload);
				thinkingMode = "plain_forced";
			}
			return controlsApplied;
		}
		bool thinkingEnabled = settings?.MainApiThinkingEnabled ?? true;
		string effort = settings?.GetMainApiReasoningEffort() ?? DuelSettings.ReasoningEffortHigh;
		return DuelSettings.ApplyThinkingControls(payload, apiUrl, modelName, thinkingEnabled, effort, out thinkingMode);
	}

	private static bool LooksLikeThinkingControlError(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		bool flag = ContainsAnyIgnoreCase(text, "thinking", "reasoning_effort", "output_config");
		bool flag2 = ContainsAnyIgnoreCase(text, "unsupported", "unknown", "invalid", "unexpected", "not allowed", "not supported", "extra inputs are not permitted");
		return flag && flag2;
	}

	private static bool TryResolvePrimaryModelByDropdownState(DuelSettings settings, out string modelName, out string selectedOption, out bool manualSelected)
	{
		modelName = "";
		selectedOption = "";
		manualSelected = true;
		if (settings == null)
		{
			return false;
		}
		selectedOption = (settings.GetMainSelectedModelOption() ?? "").Trim();
		manualSelected = string.Equals(selectedOption, "*手动填写*", StringComparison.Ordinal);
		if (manualSelected)
		{
			modelName = (settings.ModelName ?? "").Trim();
		}
		else
		{
			modelName = selectedOption;
		}
		if (string.IsNullOrWhiteSpace(modelName))
		{
			modelName = (settings.GetEffectiveMainModelName() ?? "").Trim();
		}
		return !string.IsNullOrWhiteSpace(modelName);
	}

	private static int ResolvePrimaryMaxTokens(DuelSettings settings)
	{
		try
		{
			return settings?.GetMainApiMaxTokens() ?? DefaultPrimaryMaxTokens;
		}
		catch
		{
			return DefaultPrimaryMaxTokens;
		}
	}

	private static JObject BuildPrimaryChatPayload(List<object> messages, DuelSettings settings, string apiUrl, string modelName, int actualMaxTokens, bool stream, out string thinkingMode, bool forceDisableThinking = false)
	{
		messages = EnsureFinalUserTurn(messages, out _);
		JObject jObject = new JObject
		{
			["model"] = modelName ?? "",
			["max_tokens"] = Math.Max(16, actualMaxTokens),
			["temperature"] = settings?.GetMainApiTemperature() ?? 0.8f
		};
		if (stream)
		{
			jObject["stream"] = true;
		}
		JArray jArray = new JArray();
		foreach (object message in messages ?? new List<object>())
		{
			dynamic val = message;
			jArray.Add(new JObject
			{
				["role"] = val.role,
				["content"] = val.content
			});
		}
		jObject["messages"] = jArray;
		TryApplyPrimaryThinkingControls(
			jObject,
			settings,
			apiUrl,
			modelName,
			forceDisableThinking || IsBattleSpeechRequest(messages),
			out thinkingMode);
		return jObject;
	}

	private static List<object> ApplyPlayerDisplayNameToOutgoingMessages(List<object> messages)
	{
		try
		{
			if (messages == null || messages.Count == 0)
			{
				return messages ?? new List<object>();
			}
			List<object> list = new List<object>(messages.Count);
			foreach (object message in messages)
			{
				if (TryReadMessage(message, out var role, out var content))
				{
					list.Add(new
					{
						role = role,
						content = ApplyPlayerDynamicNameToMainText(content)
					});
				}
				else
				{
					list.Add(message);
				}
			}
			return list;
		}
		catch
		{
			return messages ?? new List<object>();
		}
	}

	private static bool TryReadMessage(object message, out string role, out string content)
	{
		role = "";
		content = "";
		if (message == null)
		{
			return false;
		}
		try
		{
			if (message is JObject jObject)
			{
				role = (string)jObject["role"] ?? "";
				content = (string)jObject["content"] ?? "";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (message is IDictionary<string, object> dictionary)
			{
				if (dictionary.TryGetValue("role", out var value) && value != null)
				{
					role = value.ToString();
				}
				if (dictionary.TryGetValue("content", out var value2) && value2 != null)
				{
					content = value2.ToString();
				}
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Type type = message.GetType();
			PropertyInfo propertyInfo = type.GetProperty("role") ?? type.GetProperty("Role");
			PropertyInfo propertyInfo2 = type.GetProperty("content") ?? type.GetProperty("Content");
			if (propertyInfo != null)
			{
				object value3 = propertyInfo.GetValue(message, null);
				if (value3 != null)
				{
					role = value3.ToString();
				}
			}
			if (propertyInfo2 != null)
			{
				object value4 = propertyInfo2.GetValue(message, null);
				if (value4 != null)
				{
					content = value4.ToString();
				}
			}
			return propertyInfo != null || propertyInfo2 != null;
		}
		catch
		{
			return false;
		}
	}

	public static void RecordPrimaryRequestBodyForTokenStats(List<object> messages, int maxTokens, string mode)
	{
		try
		{
			List<object> normalizedMessages = ApplyPlayerDisplayNameToOutgoingMessages(messages);
			normalizedMessages = EnsureFinalUserTurn(normalizedMessages, out string originalLastRole);
			LogNormalizedMessageTail("token_stats", originalLastRole, normalizedMessages);
			int inputTokens = Logger.EstimateTokensFromMessages(normalizedMessages);
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
			{
				Logger.RecordTokenStats(inputTokens, 0, normalizedMessages, "[PRIMARY REQUEST NOT SENT]\nreason=missing_api_key", mode, null);
				return;
			}
			if (!TryResolvePrimaryModelByDropdownState(settings, out var effectiveModelName, out var selectedOption, out var manualSelected))
			{
				string output = "[PRIMARY REQUEST NOT SENT]\nreason=missing_model_name\nselectedOption=" + (selectedOption ?? "") + "\nmanualSelected=" + manualSelected;
				Logger.RecordTokenStats(inputTokens, 0, normalizedMessages, output, mode, null);
				return;
			}
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			int actualMaxTokens = ResolvePrimaryMaxTokens(settings);
			JObject payload = BuildPrimaryChatPayload(normalizedMessages, settings, effectiveApiUrl, effectiveModelName, actualMaxTokens, stream: false, out var thinkingMode);
			string requestBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, payload);
			string pending = "[PRIMARY REQUEST PENDING]\nmode=" + (mode ?? "") + "\nmaxTokens=" + Math.Max(16, maxTokens) + "\nactualMaxTokens=" + actualMaxTokens + "\nthinkingMode=" + (thinkingMode ?? "");
			Logger.RecordTokenStats(inputTokens, 0, normalizedMessages, pending, mode, requestBody);
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("ShoutNetwork", "[PrimaryChat] request body token prelog failed: " + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static string BuildPlayerPersonaRawNamePlaceholder(int index)
	{
		return "\uE104AFPN" + index + "\uE105";
	}

	internal static string ProtectPlayerPersonaRawNameReferencesForExternal(string text, out List<string> rawNames)
	{
		rawNames = null;
		string source = text ?? "";
		if (source.IndexOf('\uE100') < 0)
		{
			return source;
		}
		string beginMarker = KnowledgeLibraryBehavior.PlayerPersonaRawNameBeginMarker;
		string endMarker = KnowledgeLibraryBehavior.PlayerPersonaRawNameEndMarker;
		int start = source.IndexOf(beginMarker, StringComparison.Ordinal);
		if (start < 0)
		{
			return source.IndexOf(endMarker, StringComparison.Ordinal) < 0 ? source : source.Replace(endMarker, "");
		}
		StringBuilder result = new StringBuilder(source.Length);
		int cursor = 0;
		while (start >= 0)
		{
			result.Append(source, cursor, start - cursor);
			int valueStart = start + beginMarker.Length;
			int end = source.IndexOf(endMarker, valueStart, StringComparison.Ordinal);
			if (end < 0)
			{
				result.Append(source, valueStart, source.Length - valueStart);
				cursor = source.Length;
				break;
			}
			rawNames ??= new List<string>();
			rawNames.Add(source.Substring(valueStart, end - valueStart));
			result.Append(BuildPlayerPersonaRawNamePlaceholder(rawNames.Count - 1));
			cursor = end + endMarker.Length;
			start = source.IndexOf(beginMarker, cursor, StringComparison.Ordinal);
		}
		if (cursor < source.Length)
		{
			result.Append(source, cursor, source.Length - cursor);
		}
		return KnowledgeLibraryBehavior.StripPlayerPersonaRawNameMarkersForExternal(result.ToString());
	}

	internal static string RestorePlayerPersonaRawNameReferencesForExternal(string text, List<string> rawNames)
	{
		string result = text ?? "";
		if (rawNames == null || rawNames.Count == 0)
		{
			return result;
		}
		for (int i = 0; i < rawNames.Count; i++)
		{
			result = result.Replace(BuildPlayerPersonaRawNamePlaceholder(i), rawNames[i] ?? "");
		}
		return KnowledgeLibraryBehavior.StripPlayerPersonaRawNameMarkersForExternal(result);
	}

	private static string ApplyPlayerDynamicNameToMainText(string text)
	{
		try
		{
			string text2 = text ?? "";
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			text2 = ProtectPlayerPersonaRawNameReferencesForExternal(text2, out var rawPlayerPersonaNames);
			string text3 = ResolvePlayerDynamicNameForOutgoingText();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "玩家";
			}
			const string text4 = "__AFEF_PLAYER_FACT__";
			const string text5 = "__PLAYER_MARRIAGE_SECTION__";
			if (!string.Equals(text3, "玩家", StringComparison.Ordinal))
			{
				text2 = text2.Replace("[AFEF" + text3 + "行为补充]", text4);
				text2 = text2.Replace("【" + text3 + "家族可婚配未婚成员（事实清单）】", text5);
			}
			text2 = text2.Replace("[AFEF玩家行为补充]", text4);
			text2 = text2.Replace("【玩家家族可婚配未婚成员（事实清单）】", text5);
			string actualPlayerName = ResolveLowProfileActualPlayerNameForRedaction();
			if (!string.IsNullOrWhiteSpace(actualPlayerName) && !string.Equals(actualPlayerName, text3, StringComparison.Ordinal))
			{
				text2 = text2.Replace(actualPlayerName, text3);
			}
			text2 = NormalizeLegacyDuelStakeText(text2, "玩家");
			if (!string.Equals(text3, "玩家", StringComparison.Ordinal))
			{
				text2 = text2.Replace("玩家", text3);
			}
			text2 = NormalizeLegacyDuelStakeText(text2, text3);
			text2 = text2.Replace(text4, "[AFEF玩家行为补充]");
			text2 = text2.Replace(text5, "【玩家家族可婚配未婚成员（事实清单）】");
			return RestorePlayerPersonaRawNameReferencesForExternal(text2, rawPlayerPersonaNames);
		}
		catch
		{
			return KnowledgeLibraryBehavior.StripPlayerPersonaRawNameMarkersForExternal(text ?? "");
		}
	}

	private static string NormalizeLegacyDuelStakeText(string text, string playerName)
	{
		try
		{
			string text2 = text ?? "";
			string text3 = (playerName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
			{
				return text2;
			}
			string pattern = Regex.Escape(text3);
			text2 = Regex.Replace(text2, "你已经将\\s*(\\d+)\\s*第纳尔交给\\s*" + pattern + "\\s*（决斗赌注）[。.]?", "你在决斗中输给了 " + text3 + "，并已按赌注将 $1 第纳尔交给 " + text3 + "。");
			text2 = Regex.Replace(text2, "你已经将\\s*(\\d+)\\s*个\\s*([^（\\r\\n]+?)\\s*交给\\s*" + pattern + "\\s*（决斗赌注）[。.]?", "你在决斗中输给了 " + text3 + "，并已按赌注将 $1 个 $2 交给 " + text3 + "。");
			text2 = Regex.Replace(text2, "你从\\s*([^（\\r\\n]+?)\\s*收到了\\s*(\\d+)\\s*第纳尔（决斗赌注）[。.]?", "你在决斗中击败了 $1，并从 $1 收到了 $2 第纳尔（决斗赌注）。");
			text2 = Regex.Replace(text2, "你从\\s*([^（\\r\\n]+?)\\s*收到了\\s*(\\d+)\\s*个\\s*([^（\\r\\n]+?)\\s*（决斗赌注）[。.]?", "你在决斗中击败了 $1，并从 $1 收到了 $2 个 $3（决斗赌注）。");
			return text2;
		}
		catch
		{
			return text ?? "";
		}
	}

	private static string ResolvePlayerDynamicNameForOutgoingText()
	{
		if (PlayerNotorietyBehavior.IsLowProfileModeEnabledForExternal())
		{
			try
			{
				string lowProfileName = (MyBehavior.BuildPlayerPublicDisplayNameForExternal((Hero)null) ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(lowProfileName))
				{
					return lowProfileName;
				}
			}
			catch
			{
			}
		}
		try
		{
			string text = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		try
		{
			return (Hero.MainHero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveLowProfileActualPlayerNameForRedaction()
	{
		if (!PlayerNotorietyBehavior.IsLowProfileModeEnabledForExternal())
		{
			return "";
		}
		try
		{
			return (Hero.MainHero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	public static async Task<string> CallApiWithMessages(List<object> messages, int maxTokens, bool recordTokenStats = true, int? overrideMaxTokens = null, bool forceDisableThinking = false, bool promptRetryOnError = false, CancellationToken cancellationToken = default(CancellationToken), float? overrideTemperature = null)
	{
		LlmRetryPrompt.CaptureMainThreadContext();
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		messages = ApplyPlayerDisplayNameToOutgoingMessages(messages);
		messages = EnsureFinalUserTurn(messages, out string originalLastRole);
		LogNormalizedMessageTail("non_stream", originalLastRole, messages);
		Stopwatch sw = Stopwatch.StartNew();
		int msgCount = messages?.Count ?? 0;
		int inputTokens = Logger.EstimateTokensFromMessages(messages);
		Logger.Obs("Network", "request_start", new Dictionary<string, object>
		{
			["mode"] = "non_stream",
			["maxTokens"] = DefaultPrimaryMaxTokens,
			["messages"] = msgCount
		});
		FreezeWatchdog.Mark("PrimaryChat.non_stream.start", "messages=" + msgCount + " maxTokens=" + maxTokens, immediate: true);
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "non_stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_api_key"
				});
				Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
				string configError = LlmRetryPrompt.BuildFailureDetail("（错误：未配置 API Key）", "");
				if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", configError))
				{
					return await CallApiWithMessages(messages, maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking, promptRetryOnError, cancellationToken, overrideTemperature);
				}
				return configError;
			}
			if (!TryResolvePrimaryModelByDropdownState(settings, out var effectiveModelName, out var selectedOption, out var manualSelected))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "non_stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_model_name",
					["selectedOption"] = selectedOption ?? "",
					["manualSelected"] = manualSelected
				});
				Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
				string configError = LlmRetryPrompt.BuildFailureDetail("（错误：未配置模型名称）", "");
				if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", configError))
				{
					return await CallApiWithMessages(messages, maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking, promptRetryOnError, cancellationToken, overrideTemperature);
				}
				return configError;
			}
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			int configuredMaxTokens = ResolvePrimaryMaxTokens(settings);
			int actualMaxTokens = overrideMaxTokens.HasValue ? Math.Max(16, DuelSettings.ClampApiMaxTokens(overrideMaxTokens.Value, configuredMaxTokens)) : configuredMaxTokens;
			JObject payload = BuildPrimaryChatPayload(messages, settings, effectiveApiUrl, effectiveModelName, actualMaxTokens, stream: false, out var thinkingMode, forceDisableThinking);
			if (overrideTemperature.HasValue)
			{
				payload["temperature"] = DuelSettings.ClampApiTemperature(overrideTemperature.Value);
			}
			string jsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, payload);
			string requestBodyForTokenStats = jsonBody;
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
			try
			{
				LlmApiCompat.ApplyAuthenticationHeaders(request, effectiveApiUrl, settings.ApiKey);
				request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
				FreezeWatchdog.Mark("PrimaryChat.non_stream.send_begin", "model=" + effectiveModelName + " maxTokens=" + actualMaxTokens, immediate: true);
				HttpResponseMessage response = await SendPrimaryNonStreamingRequestAsync(request, cancellationToken);
				FreezeWatchdog.Mark("PrimaryChat.non_stream.response", "status=" + (int)response.StatusCode + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
				if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_response"))
				{
					response.Dispose();
					return SaveRuntimeGuard.BuildStaleRequestErrorText();
				}
				string str = await response.Content.ReadAsStringAsync();
				if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_body"))
				{
					response.Dispose();
					return SaveRuntimeGuard.BuildStaleRequestErrorText();
				}
				LogPrimaryRawResponse("non_stream_status_" + (int)response.StatusCode, str);
				if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.BadRequest && thinkingMode != "plain" && LooksLikeThinkingControlError(str))
				{
					Logger.Log("ShoutNetwork", "[PrimaryChat] thinking payload rejected; retrying without thinking controls.");
					response.Dispose();
					JObject payload2 = BuildPrimaryChatPayload(messages, settings, effectiveApiUrl, effectiveModelName, actualMaxTokens, stream: false, out var _);
					if (overrideTemperature.HasValue)
					{
						payload2["temperature"] = DuelSettings.ClampApiTemperature(overrideTemperature.Value);
					}
					DuelSettings.RemoveThinkingControls(payload2);
					string jsonBody2 = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, payload2);
					requestBodyForTokenStats = jsonBody2;
					using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
					LlmApiCompat.ApplyAuthenticationHeaders(httpRequestMessage, effectiveApiUrl, settings.ApiKey);
					httpRequestMessage.Content = (HttpContent)new StringContent(jsonBody2, Encoding.UTF8, "application/json");
					FreezeWatchdog.Mark("PrimaryChat.non_stream.retry_send_begin", "model=" + effectiveModelName, immediate: true);
					response = await SendPrimaryNonStreamingRequestAsync(httpRequestMessage, cancellationToken);
					FreezeWatchdog.Mark("PrimaryChat.non_stream.retry_response", "status=" + (int)response.StatusCode + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
					if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_retry_response"))
					{
						response.Dispose();
						return SaveRuntimeGuard.BuildStaleRequestErrorText();
					}
					str = await response.Content.ReadAsStringAsync();
					if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_retry_body"))
					{
						response.Dispose();
						return SaveRuntimeGuard.BuildStaleRequestErrorText();
					}
					LogPrimaryRawResponse("non_stream_retry_status_" + (int)response.StatusCode, str);
					thinkingMode = "thinking_retry_plain";
				}
				if (response.IsSuccessStatusCode)
				{
					try
					{
						JObject responseJson = JObject.Parse(str);
						string content = ExtractPrimaryResponseText(responseJson);
						string reasoning = ExtractPrimaryReasoningText(responseJson);
						if (string.IsNullOrWhiteSpace(content))
						{
							LogPrimaryRawResponse("non_stream_empty_content", str);
							if (!HasEmptyResponseRetryMarker(messages))
							{
								Logger.Log("ShoutNetwork", "[PrimaryChat] empty content; retrying once with explicit non-empty instruction and thinking disabled.");
								if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_empty_retry"))
								{
									return SaveRuntimeGuard.BuildStaleRequestErrorText();
								}
								string retryContent = await CallApiWithMessages(BuildEmptyResponseRetryMessages(messages), maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking: true, promptRetryOnError, cancellationToken, overrideTemperature);
								if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_empty_retry_complete"))
								{
									return SaveRuntimeGuard.BuildStaleRequestErrorText();
								}
								return retryContent;
							}
							string emptyError = LlmRetryPrompt.BuildFailureDetail("（API响应格式错误: 模型回复为空）", "", str);
							if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", emptyError))
							{
								return await CallApiWithMessages(messages, maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking, promptRetryOnError, cancellationToken, overrideTemperature);
							}
							return emptyError;
						}
						content = ApplyPlayerDynamicNameToMainText(content);
						sw.Stop();
						Logger.Obs("Network", "request_complete", new Dictionary<string, object>
						{
							["mode"] = "non_stream",
							["ok"] = true,
							["status"] = (int)response.StatusCode,
							["thinkingMode"] = thinkingMode,
							["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
							["resultLen"] = content.Length
						});
						Logger.Metric("network.non_stream", ok: true, sw.Elapsed.TotalMilliseconds);
						if (recordTokenStats)
						{
							string outputContent = BuildTokenStatsOutputContent(content, reasoning);
							Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent), messages, outputContent, "non_stream", requestBodyForTokenStats);
						}
						FreezeWatchdog.Mark("PrimaryChat.non_stream.complete", "resultLen=" + content.Length + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
						if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_complete"))
						{
							return SaveRuntimeGuard.BuildStaleRequestErrorText();
						}
						return content.Trim();
					}
					catch (Exception parseEx)
					{
						LogPrimaryRawResponse("non_stream_parse_error", str);
						sw.Stop();
						Logger.Obs("Network", "parse_error", new Dictionary<string, object>
						{
							["mode"] = "non_stream",
							["status"] = (int)response.StatusCode,
							["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
						});
						Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
						string parseError = LlmRetryPrompt.BuildFailureDetail("（API响应格式错误: " + parseEx.Message + "）", "", str);
						FreezeWatchdog.Mark("PrimaryChat.non_stream.parse_error", "elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
						if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", parseError))
						{
							return await CallApiWithMessages(messages, maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking, promptRetryOnError, cancellationToken, overrideTemperature);
						}
						return parseError;
					}
				}
				sw.Stop();
				Logger.Obs("Network", "request_complete", new Dictionary<string, object>
				{
					["mode"] = "non_stream",
					["ok"] = false,
					["status"] = (int)response.StatusCode,
					["thinkingMode"] = thinkingMode,
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
				});
				Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
				string httpError = LlmRetryPrompt.BuildFailureDetail($"（API请求失败: {response.StatusCode}）", "", str);
				FreezeWatchdog.Mark("PrimaryChat.non_stream.http_error", "status=" + (int)response.StatusCode + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
				if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", httpError))
				{
					return await CallApiWithMessages(messages, maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking, promptRetryOnError, cancellationToken, overrideTemperature);
				}
				return httpError;
			}
			finally
			{
				((IDisposable)request)?.Dispose();
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			sw.Stop();
			Logger.Obs("Network", "request_error", new Dictionary<string, object>
			{
				["mode"] = "non_stream",
				["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
			string exceptionError = LlmRetryPrompt.BuildFailureDetail("（程序错误: " + ex.Message + "）", "");
			FreezeWatchdog.Mark("PrimaryChat.non_stream.exception", ex.GetType().Name + ": " + ex.Message + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
			if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", exceptionError))
			{
				return await CallApiWithMessages(messages, maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking, promptRetryOnError, cancellationToken, overrideTemperature);
			}
			return exceptionError;
		}
	}

	public static async Task CallApiWithMessagesStream(List<object> messages, int maxTokens, Action<string> onChunk, Action<string> onComplete, Action<string> onError, CancellationToken cancellationToken = default(CancellationToken), bool promptRetryOnError = true)
	{
		LlmRetryPrompt.CaptureMainThreadContext();
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		messages = ApplyPlayerDisplayNameToOutgoingMessages(messages);
		messages = EnsureFinalUserTurn(messages, out string originalLastRole);
		LogNormalizedMessageTail("stream", originalLastRole, messages);
		PlayerReferenceStreamFilter outputFilter = new PlayerReferenceStreamFilter();
		StringBuilder fullText = new StringBuilder();
		StringBuilder fullReasoning = new StringBuilder();
		StringBuilder rawStreamResponse = new StringBuilder();
		Stopwatch sw = Stopwatch.StartNew();
		double firstChunkMs = -1.0;
		int chunkCount = 0;
		int msgCount = messages?.Count ?? 0;
		int inputTokens = Logger.EstimateTokensFromMessages(messages);
		string requestBodyForTokenStats = "";
		Logger.Obs("Network", "request_start", new Dictionary<string, object>
		{
			["mode"] = "stream",
			["maxTokens"] = DefaultPrimaryMaxTokens,
			["messages"] = msgCount
		});
		FreezeWatchdog.Mark("PrimaryChat.stream.start", "messages=" + msgCount + " maxTokens=" + maxTokens, immediate: true);
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_api_key"
				});
				Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
				string configError = LlmRetryPrompt.BuildFailureDetail("（错误：未配置 API Key）", "");
				if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", configError))
				{
					await CallApiWithMessagesStream(messages, maxTokens, onChunk, onComplete, onError, cancellationToken, promptRetryOnError);
					return;
				}
				onError?.Invoke(configError);
				return;
			}
			if (!TryResolvePrimaryModelByDropdownState(settings, out var effectiveModelName, out var selectedOption, out var manualSelected))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_model_name",
					["selectedOption"] = selectedOption ?? "",
					["manualSelected"] = manualSelected
				});
				Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
				string configError = LlmRetryPrompt.BuildFailureDetail("（错误：未配置模型名称）", "");
				if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", configError))
				{
					await CallApiWithMessagesStream(messages, maxTokens, onChunk, onComplete, onError, cancellationToken, promptRetryOnError);
					return;
				}
				onError?.Invoke(configError);
				return;
			}
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			int actualMaxTokens = ResolvePrimaryMaxTokens(settings);
			JObject payload = BuildPrimaryChatPayload(messages, settings, effectiveApiUrl, effectiveModelName, actualMaxTokens, stream: true, out var thinkingMode);
			string jsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, payload);
			requestBodyForTokenStats = jsonBody;
			Logger.RecordTokenStats(
				inputTokens,
				0,
				messages,
				"[PRIMARY REQUEST PENDING]\nmode=stream\nmaxTokens=" + Math.Max(16, maxTokens) + "\nactualMaxTokens=" + actualMaxTokens + "\nthinkingMode=" + (thinkingMode ?? ""),
				"stream_pending",
				requestBodyForTokenStats);
			bool streamSucceeded = false;
			Exception lastStreamException = null;
			for (int attempt = 1; attempt <= 2; attempt++)
			{
				if (streamSucceeded)
				{
					break;
				}
				try
				{
					HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
					try
					{
						LlmApiCompat.ApplyAuthenticationHeaders(request, effectiveApiUrl, settings.ApiKey);
						request.Headers.ConnectionClose = true;
						request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
						FreezeWatchdog.Mark("PrimaryChat.stream.send_begin", "attempt=" + attempt + " model=" + effectiveModelName + " maxTokens=" + actualMaxTokens, immediate: true);
						HttpResponseMessage response = await SendPrimaryStreamingRequestAsync(request, cancellationToken);
						FreezeWatchdog.Mark("PrimaryChat.stream.response", "attempt=" + attempt + " status=" + (int)response.StatusCode + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
						if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_response"))
						{
							response.Dispose();
							return;
						}
						if (!response.IsSuccessStatusCode)
						{
							string errBody = await response.Content.ReadAsStringAsync();
							if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_error_body"))
							{
								response.Dispose();
								return;
							}
							LogPrimaryRawResponse("stream_status_" + (int)response.StatusCode, errBody);
							if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && thinkingMode != "plain" && LooksLikeThinkingControlError(errBody) && attempt < 2)
							{
								Logger.Log("ShoutNetwork", "[PrimaryChat] stream thinking payload rejected; retrying without thinking controls.");
								response.Dispose();
								JObject retryPayload = BuildPrimaryChatPayload(messages, settings, effectiveApiUrl, effectiveModelName, actualMaxTokens, stream: true, out var _);
								DuelSettings.RemoveThinkingControls(retryPayload);
								jsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, retryPayload);
								requestBodyForTokenStats = jsonBody;
								thinkingMode += "_retry_plain";
								continue;
							}
							sw.Stop();
							Logger.Obs("Network", "request_complete", new Dictionary<string, object>
							{
								["mode"] = "stream",
								["ok"] = false,
								["status"] = (int)response.StatusCode,
								["attempt"] = attempt,
								["thinkingMode"] = thinkingMode,
								["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
							});
							Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
							string httpError = LlmRetryPrompt.BuildFailureDetail($"（API请求失败: {response.StatusCode}）", "", errBody);
							FreezeWatchdog.Mark("PrimaryChat.stream.http_error", "attempt=" + attempt + " status=" + (int)response.StatusCode + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
							if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", httpError))
							{
								await CallApiWithMessagesStream(messages, maxTokens, onChunk, onComplete, onError, cancellationToken, promptRetryOnError);
								return;
							}
							onError?.Invoke(httpError);
							return;
						}
						FreezeWatchdog.Mark("PrimaryChat.stream.content_stream_wait_begin", "attempt=" + attempt + " thread=" + Thread.CurrentThread.ManagedThreadId);
						using Stream stream = await response.Content.ReadAsStreamAsync();
						FreezeWatchdog.Mark("PrimaryChat.stream.content_stream_wait_end", "attempt=" + attempt + " thread=" + Thread.CurrentThread.ManagedThreadId);
						using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						int readSequence = 0;
						while (true)
						{
							string text;
							readSequence++;
							FreezeWatchdog.Mark("PrimaryChat.stream.read_line_begin", "attempt=" + attempt + " read=" + readSequence + " chunks=" + chunkCount + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " thread=" + Thread.CurrentThread.ManagedThreadId);
							string line = (text = await reader.ReadLineAsync());
							FreezeWatchdog.Mark("PrimaryChat.stream.read_line_end", "attempt=" + attempt + " read=" + readSequence + " null=" + (text == null) + " chunks=" + chunkCount + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " thread=" + Thread.CurrentThread.ManagedThreadId);
							if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_read"))
							{
								return;
							}
							if (text == null || cancellationToken.IsCancellationRequested)
							{
								break;
							}
							line = line.Trim();
							if (string.IsNullOrEmpty(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}
							string data = line.Substring(5).Trim();
							if (data == "[DONE]")
							{
								break;
							}
							if (rawStreamResponse.Length > 0)
							{
								rawStreamResponse.AppendLine();
							}
							rawStreamResponse.Append(data);
							try
							{
								JObject chunk = JObject.Parse(data);
								string reasoningDelta = LlmApiCompat.ExtractStreamReasoningText(chunk);
								if (!string.IsNullOrEmpty(reasoningDelta))
								{
									fullReasoning.Append(reasoningDelta);
								}
								string delta = ExtractPrimaryStreamDelta(chunk);
								if (!string.IsNullOrEmpty(delta))
								{
									string text2 = outputFilter.Push(delta);
									if (chunkCount == 0)
									{
										firstChunkMs = sw.Elapsed.TotalMilliseconds;
										Logger.Obs("Network", "first_chunk", new Dictionary<string, object>
										{
											["mode"] = "stream",
											["firstChunkMs"] = Math.Round(firstChunkMs, 2)
										});
										FreezeWatchdog.Mark("PrimaryChat.stream.first_chunk", "attempt=" + attempt + " firstChunkMs=" + Math.Round(firstChunkMs, 2), immediate: true);
									}
									chunkCount++;
									if (!string.IsNullOrEmpty(text2))
									{
										fullText.Append(text2);
										try
										{
											if (!SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_chunk"))
											{
												FreezeWatchdog.Mark("PrimaryChat.stream.chunk_callback_begin", "read=" + readSequence + " chunk=" + chunkCount + " deltaLen=" + text2.Length + " thread=" + Thread.CurrentThread.ManagedThreadId);
												onChunk?.Invoke(text2);
												FreezeWatchdog.Mark("PrimaryChat.stream.chunk_callback_end", "read=" + readSequence + " chunk=" + chunkCount + " thread=" + Thread.CurrentThread.ManagedThreadId);
											}
										}
										catch
										{
										}
									}
								}
								else if (chunkCount == 0 && fullText.Length == 0)
								{
									LogPrimaryRawResponse("stream_unparsed_chunk", data);
								}
							}
							catch (Exception parseEx)
							{
								LogPrimaryRawResponse("stream_chunk_parse_error", parseEx.Message + "\n" + data);
							}
						}
					}
					finally
					{
						((IDisposable)request)?.Dispose();
					}
					string text3 = outputFilter.Flush();
					if (!string.IsNullOrEmpty(text3))
					{
						fullText.Append(text3);
						try
						{
							if (!SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_flush"))
							{
								onChunk?.Invoke(text3);
							}
						}
						catch
						{
						}
					}
					streamSucceeded = true;
				}
				catch (Exception ex)
				{
					lastStreamException = ex;
					if (attempt < 2 && fullText.Length == 0)
					{
						await Task.Delay(500, cancellationToken);
					}
				}
			}
			if (!streamSucceeded)
			{
				string fallback = await CallApiWithMessages(messages, maxTokens, recordTokenStats: false, promptRetryOnError: false);
				if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_fallback"))
				{
					return;
				}
				if (!string.IsNullOrWhiteSpace(fallback) && !fallback.StartsWith("（错误") && !LlmRetryPrompt.IsRetryableLlmError(fallback))
				{
					sw.Stop();
					Logger.Obs("Network", "request_complete", new Dictionary<string, object>
					{
						["mode"] = "stream",
						["ok"] = true,
						["fallback"] = true,
						["chunkCount"] = chunkCount,
						["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
						["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["resultLen"] = fallback.Length
					});
					Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
					Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(fallback), messages, BuildTokenStatsOutputContent(fallback), "stream_fallback", requestBodyForTokenStats);
					FreezeWatchdog.Mark("PrimaryChat.stream.fallback_complete", "resultLen=" + fallback.Length + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
					if (!SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_fallback_complete"))
					{
						onComplete?.Invoke(fallback.Trim());
					}
					return;
				}
				if (fullText.Length > 0)
				{
					string text4 = outputFilter.Flush();
					if (!string.IsNullOrEmpty(text4))
					{
						fullText.Append(text4);
					}
					sw.Stop();
					Logger.Obs("Network", "request_complete", new Dictionary<string, object>
					{
						["mode"] = "stream",
						["ok"] = true,
						["fallback"] = false,
						["partial"] = true,
						["chunkCount"] = chunkCount,
						["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
						["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["resultLen"] = fullText.Length
					});
					Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
					string outputContent3 = BuildTokenStatsOutputContent(fullText.ToString(), fullReasoning.ToString());
					Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent3), messages, outputContent3, "stream_partial", requestBodyForTokenStats);
					FreezeWatchdog.Mark("PrimaryChat.stream.partial_complete", "resultLen=" + fullText.Length + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
					if (!SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_partial_complete"))
					{
						onComplete?.Invoke(ApplyPlayerDynamicNameToMainText(fullText.ToString()).Trim());
					}
					return;
				}
				if (lastStreamException != null)
				{
					sw.Stop();
					Logger.Obs("Network", "request_error", new Dictionary<string, object>
					{
						["mode"] = "stream",
						["fallback"] = true,
						["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["message"] = lastStreamException.Message,
						["type"] = lastStreamException.GetType().Name
					});
					Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
					string streamError = LlmRetryPrompt.BuildFailureDetail("（程序错误: " + lastStreamException.Message + "）", fullText.ToString(), rawStreamResponse.ToString());
					FreezeWatchdog.Mark("PrimaryChat.stream.exception_no_content", lastStreamException.GetType().Name + ": " + lastStreamException.Message + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
					if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", streamError))
					{
						await CallApiWithMessagesStream(messages, maxTokens, onChunk, onComplete, onError, cancellationToken, promptRetryOnError);
						return;
					}
					onError?.Invoke(streamError);
					return;
				}
			}
			string finalText = fullText.ToString().Trim();
			finalText = ApplyPlayerDynamicNameToMainText(finalText);
			if (string.IsNullOrWhiteSpace(finalText))
			{
				LogPrimaryRawResponse("stream_empty_final", "chunkCount=" + chunkCount + "; no parsed text from response\n" + rawStreamResponse);
				string retryFailure = "";
				if (!HasEmptyResponseRetryMarker(messages))
				{
					Logger.Log("ShoutNetwork", "[PrimaryChat] empty stream final; retrying once with explicit non-empty instruction and thinking disabled.");
					string retry = await CallApiWithMessages(BuildEmptyResponseRetryMessages(messages), maxTokens, recordTokenStats: false, forceDisableThinking: true, promptRetryOnError: false);
					if (SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_empty_retry"))
					{
						return;
					}
					if (!string.IsNullOrWhiteSpace(retry) && !retry.StartsWith("（错误") && !LlmRetryPrompt.IsRetryableLlmError(retry))
					{
						if (!SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_empty_retry_complete"))
						{
							onComplete?.Invoke(retry.Trim());
						}
						return;
					}
					retryFailure = retry ?? "";
				}
				string rawAttempts = rawStreamResponse.ToString();
				if (!string.IsNullOrWhiteSpace(retryFailure))
				{
					rawAttempts = rawAttempts + (string.IsNullOrWhiteSpace(rawAttempts) ? "" : "\n\n") + "【非流式空回复重试结果】\n" + retryFailure;
				}
				string emptyStreamError = LlmRetryPrompt.BuildFailureDetail("（API响应格式错误: 流式响应没有可解析的模型回复）", "", rawAttempts);
				if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", emptyStreamError))
				{
					await CallApiWithMessagesStream(messages, maxTokens, onChunk, onComplete, onError, cancellationToken, promptRetryOnError);
					return;
				}
				onError?.Invoke(emptyStreamError);
				return;
			}
			sw.Stop();
			Logger.Obs("Network", "request_complete", new Dictionary<string, object>
			{
				["mode"] = "stream",
				["ok"] = true,
				["fallback"] = false,
				["chunkCount"] = chunkCount,
				["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
				["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
				["resultLen"] = finalText.Length
			});
			Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
			string outputContent2 = BuildTokenStatsOutputContent(finalText, fullReasoning.ToString());
			Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent2), messages, outputContent2, "stream", requestBodyForTokenStats);
			FreezeWatchdog.Mark("PrimaryChat.stream.complete", "resultLen=" + finalText.Length + " chunks=" + chunkCount + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
			if (!SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_complete"))
			{
				FreezeWatchdog.Mark("PrimaryChat.stream.complete_callback_begin", "resultLen=" + finalText.Length + " chunks=" + chunkCount + " thread=" + Thread.CurrentThread.ManagedThreadId);
				onComplete?.Invoke(finalText);
				FreezeWatchdog.Mark("PrimaryChat.stream.complete_callback_end", "resultLen=" + finalText.Length + " chunks=" + chunkCount + " thread=" + Thread.CurrentThread.ManagedThreadId);
			}
		}
		catch (OperationCanceledException)
		{
			sw.Stop();
			Logger.Obs("Network", "request_cancelled", new Dictionary<string, object>
			{
				["mode"] = "stream",
				["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
				["partialLen"] = fullText.Length
			});
			Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
			string outputContent4 = BuildTokenStatsOutputContent(fullText.ToString(), fullReasoning.ToString());
			Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent4), messages, outputContent4, "stream_cancelled", requestBodyForTokenStats);
			FreezeWatchdog.Mark("PrimaryChat.stream.cancelled", "partialLen=" + fullText.Length + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
			onComplete?.Invoke(ApplyPlayerDynamicNameToMainText(fullText.ToString()).Trim());
		}
		catch (Exception ex3)
		{
			string partial = ApplyPlayerDynamicNameToMainText(fullText.ToString()).Trim();
			if (!string.IsNullOrEmpty(partial))
			{
				sw.Stop();
				Logger.Obs("Network", "request_complete", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["ok"] = true,
					["partial"] = true,
					["chunkCount"] = chunkCount,
					["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["resultLen"] = partial.Length
				});
				Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
				string outputContent5 = BuildTokenStatsOutputContent(partial, fullReasoning.ToString());
				Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent5), messages, outputContent5, "stream_exception_partial", requestBodyForTokenStats);
				FreezeWatchdog.Mark("PrimaryChat.stream.exception_partial", ex3.GetType().Name + ": " + ex3.Message + " partialLen=" + partial.Length + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
				onComplete?.Invoke(partial);
			}
			else
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = ex3.Message,
					["type"] = ex3.GetType().Name
				});
				Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
				string streamError = LlmRetryPrompt.BuildFailureDetail("（程序错误: " + ex3.Message + "）", "", rawStreamResponse.ToString());
				FreezeWatchdog.Mark("PrimaryChat.stream.exception", ex3.GetType().Name + ": " + ex3.Message + " elapsedMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
				if (promptRetryOnError && await LlmRetryPrompt.PromptRetryAsync("正文生成", streamError))
				{
					await CallApiWithMessagesStream(messages, maxTokens, onChunk, onComplete, onError, cancellationToken, promptRetryOnError);
					return;
				}
				onError?.Invoke(streamError);
			}
		}
	}
}

