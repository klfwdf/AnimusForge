using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class PolicyLlmClient
{
	private const int DefaultMaxAttempts = 3;

	private const int MaxRetryAfterDelaySeconds = 180;

	private const int MaxCapabilityCacheEntries = 32;

	private enum PolicyTokenParameterMode
	{
		MaxTokens,
		MaxCompletionTokens,
		Omit
	}

	private sealed class PolicyRequestCapabilities
	{
		public PolicyTokenParameterMode TokenParameterMode = PolicyTokenParameterMode.MaxTokens;

		public bool OmitThinkingControls;

		public bool OmitTemperature;

		public int? MaxTokensOverride;

		public PolicyRequestCapabilities Clone()
		{
			return new PolicyRequestCapabilities
			{
				TokenParameterMode = TokenParameterMode,
				OmitThinkingControls = OmitThinkingControls,
				OmitTemperature = OmitTemperature,
				MaxTokensOverride = MaxTokensOverride
			};
		}
	}

	private static readonly object CapabilityCacheLock = new object();

	private static readonly Dictionary<string, PolicyRequestCapabilities> CapabilityCache = new Dictionary<string, PolicyRequestCapabilities>(StringComparer.OrdinalIgnoreCase);

	private static readonly Queue<string> CapabilityCacheOrder = new Queue<string>();

	private sealed class NpcPolicyHttpExchange : IDisposable
	{
		public HttpResponseMessage Response { get; private set; }

		public string ResponseBody { get; private set; }

		public string RequestBodyForTokenStats { get; private set; }

		public NpcPolicyHttpExchange(HttpResponseMessage response, string responseBody, string requestBodyForTokenStats)
		{
			Response = response;
			ResponseBody = responseBody ?? "";
			RequestBodyForTokenStats = requestBodyForTokenStats ?? "";
		}

		public void Dispose()
		{
			HttpResponseMessage response = Response;
			Response = null;
			response?.Dispose();
		}
	}

	private readonly struct RetryAfterInfo
	{
		public readonly int? EffectiveSeconds;

		public readonly int? RawSeconds;

		public readonly bool Capped;

		private RetryAfterInfo(int? rawSeconds)
		{
			RawSeconds = rawSeconds;
			EffectiveSeconds = CapRetryAfterSeconds(rawSeconds, out bool capped);
			Capped = capped;
		}

		public static RetryAfterInfo FromResponse(HttpResponseMessage response)
		{
			return new RetryAfterInfo(TryGetRetryAfterSeconds(response));
		}

		public void ApplyTo(NpcPolicyApiCallResult result)
		{
			if (result == null)
			{
				return;
			}
			result.RetryAfterSecondsRaw = RawSeconds;
			result.RetryAfterSeconds = EffectiveSeconds;
			result.RetryAfterSecondsCapped = Capped;
		}
	}

	private readonly struct RetryBackoffPlan
	{
		public readonly int DelayMilliseconds;

		private readonly int? _retryAfterSeconds;

		private readonly int? _retryAfterSecondsRaw;

		private readonly bool _retryAfterCapped;

		private RetryBackoffPlan(int delayMilliseconds, int? retryAfterSeconds, int? retryAfterSecondsRaw, bool retryAfterCapped)
		{
			DelayMilliseconds = delayMilliseconds;
			_retryAfterSeconds = retryAfterSeconds;
			_retryAfterSecondsRaw = retryAfterSecondsRaw;
			_retryAfterCapped = retryAfterCapped;
		}

		public static RetryBackoffPlan FromResult(NpcPolicyApiCallResult result)
		{
			int delayMs = result != null && result.IsRateLimit ? 60000 : 1500;
			if (result != null && result.RetryAfterSeconds.HasValue)
			{
				delayMs = Math.Max(delayMs, result.RetryAfterSeconds.Value * 1000);
			}
			delayMs = Math.Min(delayMs, MaxRetryAfterDelaySeconds * 1000);
			return new RetryBackoffPlan(delayMs, result?.RetryAfterSeconds, result?.RetryAfterSecondsRaw, result?.RetryAfterSecondsCapped ?? false);
		}

		public string BuildLog(int attempt, int attempts)
		{
			return "[HTTP] NPC policy retry backoff attempt=" + attempt.ToString(CultureInfo.InvariantCulture)
				+ "/" + attempts.ToString(CultureInfo.InvariantCulture)
				+ " delayMs=" + DelayMilliseconds.ToString(CultureInfo.InvariantCulture)
				+ " retryAfterSeconds=" + (_retryAfterSeconds?.ToString(CultureInfo.InvariantCulture) ?? "")
				+ " retryAfterSecondsRaw=" + (_retryAfterSecondsRaw?.ToString(CultureInfo.InvariantCulture) ?? "")
				+ " retryAfterCapped=" + (_retryAfterCapped ? "true" : "false");
		}
	}

	public static bool IsConfiguredForNpcPolicy(out string errorMessage)
	{
		return TryResolvePolicyApiConfig(
			DuelSettings.GetNpcRulerPolicyApiSourceForExternal(),
			DuelSettings.GetNpcRulerPolicyFollowSelectedApiTokensForExternal(),
			DuelSettings.GetNpcRulerPolicyCustomMaxTokensForExternal(),
			null,
			out var _,
			out errorMessage);
	}

	public static bool IsConfiguredForLegacyEventApi(out string errorMessage)
	{
		return TryResolvePolicyApiConfig(DuelSettings.PolicyApiSourceEventAndRebellion, true, DuelSettings.DefaultEventAndRebellionApiMaxTokens, null, out var _, out errorMessage);
	}

	public static bool TryResolvePlayerPolicyProfile(string requestedSource, bool followSelectedApiTokens, int customMaxTokens, float fixedTemperature, out PolicyApiExecutionProfile profile, out string errorMessage)
	{
		if (!TryResolvePolicyApiConfig(requestedSource, followSelectedApiTokens, customMaxTokens, fixedTemperature, out profile, out errorMessage))
		{
			return false;
		}
		profile.UseJsonObjectResponse = LlmApiCompat.IsOfficialDeepSeekUrl(profile.EffectiveApiUrl);
		return true;
	}

	public static bool TryResolvePlayerPolicyAutoDraftProfile(
		string requestedSource,
		out PolicyApiExecutionProfile profile,
		out string errorMessage)
	{
		if (!TryResolvePolicyApiConfig(
			requestedSource,
			followSelectedApiTokens: true,
			customMaxTokens: 1200,
			fixedTemperature: null,
			out profile,
			out errorMessage))
		{
			return false;
		}
		profile.MaxTokens = Math.Max(1, Math.Min(profile.MaxTokens, 1200));
		DuelSettings.ResolvePolicyApiThinkingForExternal(
			requestedSource,
			profile.ResolvedRoute,
			out profile.ThinkingEnabled,
			out profile.ReasoningEffort);
		profile.UseJsonObjectResponse = LlmApiCompat.IsOfficialDeepSeekUrl(profile.EffectiveApiUrl);
		return true;
	}

	public static bool TryResolveNpcPolicyProfile(out PolicyApiExecutionProfile profile, out string errorMessage)
	{
		return TryResolvePolicyApiConfig(
			DuelSettings.GetNpcRulerPolicyApiSourceForExternal(),
			DuelSettings.GetNpcRulerPolicyFollowSelectedApiTokensForExternal(),
			DuelSettings.GetNpcRulerPolicyCustomMaxTokensForExternal(),
			null,
			out profile,
			out errorMessage);
	}

	// 保留非政策调用方的既有“事件/叛乱 API，缺失时回退主 API”入口；不启用政策专用 Token/温度兼容降级。
	public static Task<NpcPolicyApiCallResult> CallEventAndRebellionApiWithRetriesAsync(string systemPrompt, int maxTokens, int hardTimeoutMilliseconds, string source, long runtimeGeneration, int maxAttempts = DefaultMaxAttempts)
	{
		if (!TryResolvePolicyApiConfig(DuelSettings.PolicyApiSourceEventAndRebellion, true, maxTokens, null, out PolicyApiExecutionProfile profile, out string errorMessage))
		{
			return Task.FromResult(new NpcPolicyApiCallResult { ErrorMessage = errorMessage });
		}
		profile.MaxTokens = Math.Max(1, maxTokens);
		return CallPolicyApiWithRetriesAsync(BuildMessageArray(systemPrompt), profile, hardTimeoutMilliseconds, source, runtimeGeneration, maxAttempts, default, enablePolicyCompatibility: false);
	}

	public static Task<NpcPolicyApiCallResult> CallPolicyApiWithRetriesAsync(string systemPrompt, PolicyApiExecutionProfile profile, int hardTimeoutMilliseconds, string source, long runtimeGeneration, int maxAttempts = DefaultMaxAttempts, CancellationToken cancellationToken = default)
	{
		return CallPolicyApiWithRetriesAsync(BuildMessageArray(systemPrompt), profile, hardTimeoutMilliseconds, source, runtimeGeneration, maxAttempts, cancellationToken);
	}

	public static async Task<NpcPolicyApiCallResult> CallPolicyApiWithRetriesAsync(JArray messages, PolicyApiExecutionProfile profile, int hardTimeoutMilliseconds, string source, long runtimeGeneration, int maxAttempts = DefaultMaxAttempts, CancellationToken cancellationToken = default, bool enablePolicyCompatibility = true)
	{
		NpcPolicyApiCallResult finalResult = new NpcPolicyApiCallResult();
		if (profile == null || string.IsNullOrWhiteSpace(profile.EffectiveApiUrl) || string.IsNullOrWhiteSpace(profile.ApiKey) || string.IsNullOrWhiteSpace(profile.ModelName))
		{
			finalResult.ErrorMessage = "政策 API 执行配置不完整。";
			return finalResult;
		}
		JArray frozenMessages = messages == null ? new JArray() : (JArray)messages.DeepClone();
		string capabilityKey = BuildCapabilityCacheKey(profile.EffectiveApiUrl, profile.ModelName);
		PolicyRequestCapabilities capabilities = enablePolicyCompatibility ? GetCachedCapabilities(capabilityKey) : new PolicyRequestCapabilities();
		int attempts = Math.Max(1, Math.Min(DefaultMaxAttempts, maxAttempts));
		for (int attempt = 1; attempt <= attempts; attempt++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, (source ?? "NpcPolicy") + "_api_before_attempt"))
			{
				finalResult.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				finalResult.AttemptsUsed = attempt;
				return finalResult;
			}

			NpcPolicyApiCallResult result = new NpcPolicyApiCallResult
			{
				AttemptsUsed = attempt,
				ResolvedRoute = profile.ResolvedRoute,
				ThinkingRetryPlain = capabilities.OmitThinkingControls
			};
			int requestMaxTokens = Math.Max(1, Math.Min(profile.MaxTokens, capabilities.MaxTokensOverride ?? profile.MaxTokens));
			JObject body = BuildCompatibleChatRequestBody(profile, frozenMessages, requestMaxTokens, capabilities, out string thinkingMode);
			string jsonBody = LlmApiCompat.PrepareChatRequestJson(profile.EffectiveApiUrl, body);
		Log(source, BuildRequestStartLog(profile.ResolvedRoute, profile.ModelName, requestMaxTokens, thinkingMode, profile.EffectiveApiUrl)
				+ " tokenField=" + GetTokenParameterLogName(capabilities.TokenParameterMode)
				+ " responseFormat=" + (profile.UseJsonObjectResponse ? "json_object" : "default")
				+ " temperature=" + (capabilities.OmitTemperature ? "omitted" : profile.Temperature.ToString(CultureInfo.InvariantCulture))
				+ " attempt=" + attempt.ToString(CultureInfo.InvariantCulture) + "/" + attempts.ToString(CultureInfo.InvariantCulture));
			NpcPolicyHttpExchange exchange = await SendAndReadNpcPolicyExchangeAsync(
				profile.EffectiveApiUrl,
				profile.ApiKey,
				jsonBody,
				Math.Max(1000, hardTimeoutMilliseconds),
				source,
				runtimeGeneration,
				BuildApiStagePrefix(source, "api_" + attempt.ToString(CultureInfo.InvariantCulture)),
				result,
				cancellationToken);
			if (exchange == null)
			{
				finalResult = result;
				return finalResult;
			}
			try
			{
				result = CompleteApiCallResult(exchange, result, frozenMessages, profile.ResolvedRoute, profile.ModelName, thinkingMode, capabilities.OmitThinkingControls, source);
			}
			finally
			{
				exchange.Dispose();
			}
			result.AttemptsUsed = attempt;
			finalResult = result;
			if (result.Success)
			{
				if (enablePolicyCompatibility)
				{
					CacheSuccessfulCapabilities(capabilityKey, capabilities);
				}
				return result;
			}
			if (result.IsAuthFailure)
			{
				Log(source, "[HTTP] Policy retry stopped because authentication failure was detected. attempts_used=" + attempt.ToString(CultureInfo.InvariantCulture));
				return result;
			}
			if (result.IsQuotaLimit)
			{
				Log(source, "[HTTP] Policy retry stopped because quota/balance limit was detected. attempts_used=" + attempt.ToString(CultureInfo.InvariantCulture));
				return result;
			}
			if (result.IsOutputTruncated)
			{
				Log(source, "[HTTP] Policy retry stopped because output was truncated by finish_reason=length. attempts_used=" + attempt.ToString(CultureInfo.InvariantCulture));
				return result;
			}
			string compatibilityReason = "";
			bool compatibilityChanged = false;
			if (result.StatusCode == (int)HttpStatusCode.BadRequest || (enablePolicyCompatibility && result.StatusCode == 422))
			{
				compatibilityChanged = enablePolicyCompatibility
					? TryApplyPolicyCompatibilityDowngrade(result.ResponseBody, requestMaxTokens, capabilities, out compatibilityReason)
					: TryApplyLegacyThinkingDowngrade(result.ResponseBody, capabilities, out compatibilityReason);
			}
			if (compatibilityChanged)
			{
				Log(source, "[HTTP] Policy compatibility downgrade applied: " + compatibilityReason
					+ " route=" + profile.ResolvedRoute
					+ " attempt=" + attempt.ToString(CultureInfo.InvariantCulture) + "/" + attempts.ToString(CultureInfo.InvariantCulture));
			}
			bool transientFailure = IsTransientPolicyFailure(result);
			if (!compatibilityChanged && !transientFailure)
			{
				return result;
			}
			if (attempt < attempts && transientFailure && !compatibilityChanged)
			{
				RetryBackoffPlan backoff = RetryBackoffPlan.FromResult(result);
				Log(source, backoff.BuildLog(attempt, attempts));
				await Task.Delay(backoff.DelayMilliseconds, cancellationToken);
			}
		}
		return finalResult;
	}

	private static async Task<NpcPolicyHttpExchange> SendAndReadNpcPolicyExchangeAsync(string effectiveApiUrl, string apiKey, string jsonBody, int hardTimeoutMilliseconds, string source, long runtimeGeneration, string staleStagePrefix, NpcPolicyApiCallResult result, CancellationToken cancellationToken)
	{
		HttpResponseMessage response = await SendNpcPolicyRequestWithHardTimeoutAsync(effectiveApiUrl, apiKey, jsonBody, hardTimeoutMilliseconds, source, result, cancellationToken);
		if (response == null)
		{
			return null;
		}
		bool keepResponse = false;
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, staleStagePrefix + "_response"))
			{
				result.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return null;
			}
			string responseBody = await response.Content.ReadAsStringAsync();
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, staleStagePrefix + "_body"))
			{
				result.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return null;
			}
			keepResponse = true;
			return new NpcPolicyHttpExchange(response, responseBody, jsonBody);
		}
		finally
		{
			if (!keepResponse)
			{
				response.Dispose();
			}
		}
	}

	private static NpcPolicyApiCallResult CompleteApiCallResult(NpcPolicyHttpExchange exchange, NpcPolicyApiCallResult result, JArray messages, string resolvedRoute, string modelName, string thinkingMode, bool thinkingRetriedPlain, string source)
	{
		HttpResponseMessage response = exchange.Response;
		string responseBody = exchange.ResponseBody ?? "";
		result.StatusCode = (int)response.StatusCode;
		result.ResponseBody = responseBody;
		if (!response.IsSuccessStatusCode)
		{
			ApplyHttpFailureDetails(result, response, responseBody);
			RecordHttpErrorTokenStatsSafe(messages, resolvedRoute, modelName, response, responseBody, result, thinkingRetriedPlain, exchange.RequestBodyForTokenStats);
			return result;
		}
		string content = "";
		JObject parsed = null;
		try
		{
			parsed = JObject.Parse((responseBody ?? "").Trim());
			result.FinishReason = ExtractFinishReason(parsed);
			ApplyUsageStats(result, parsed);
			content = LlmApiCompat.ExtractAssistantText(parsed);
		}
		catch (Exception ex)
		{
			Log(source, "[HTTP] NPC policy response parse failed: " + ex.Message + " route=" + resolvedRoute + " thinking_retry_plain=" + (thinkingRetriedPlain ? "true" : "false") + " raw=" + TrimForLog(responseBody));
			try
			{
				content = LlmApiCompat.ExtractAssistantText(responseBody);
			}
			catch
			{
				content = "";
			}
		}
		result.Content = (content ?? "").Trim();
		ApplyFinishReasonStatus(result, source, resolvedRoute, thinkingRetriedPlain);
		RecordHttpSuccessTokenStatsSafe(messages, resolvedRoute, modelName, thinkingMode, thinkingRetriedPlain, result.Content, responseBody, exchange.RequestBodyForTokenStats);
		return result;
	}

	private static string ExtractFinishReason(JObject responseJson)
	{
		try
		{
			return (responseJson?.SelectToken("choices[0].finish_reason")?.ToString() ?? responseJson?["finish_reason"]?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static void ApplyUsageStats(NpcPolicyApiCallResult result, JObject responseJson)
	{
		if (result == null || responseJson == null)
		{
			return;
		}
		int? promptTokens = ReadIntToken(responseJson, "usage.prompt_tokens", "usage.input_tokens");
		int? completionTokens = ReadIntToken(responseJson, "usage.completion_tokens", "usage.output_tokens");
		int? totalTokens = ReadIntToken(responseJson, "usage.total_tokens");
		int? cacheHitTokens = ReadIntToken(responseJson, "usage.prompt_cache_hit_tokens", "usage.prompt_tokens_details.cached_tokens", "usage.cache_read_input_tokens");
		int? cacheMissTokens = ReadIntToken(responseJson, "usage.prompt_cache_miss_tokens", "usage.cache_creation_input_tokens");
		if (!cacheMissTokens.HasValue && promptTokens.HasValue && cacheHitTokens.HasValue)
		{
			cacheMissTokens = Math.Max(0, promptTokens.Value - cacheHitTokens.Value);
		}
		result.PromptTokens = promptTokens;
		result.CompletionTokens = completionTokens;
		result.TotalTokens = totalTokens;
		result.PromptCacheHitTokens = cacheHitTokens;
		result.PromptCacheMissTokens = cacheMissTokens;
	}

	private static int? ReadIntToken(JObject json, params string[] paths)
	{
		if (json == null || paths == null)
		{
			return null;
		}
		foreach (string path in paths)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				continue;
			}
			try
			{
				JToken token = json.SelectToken(path);
				if (token == null || token.Type == JTokenType.Null)
				{
					continue;
				}
				if (token.Type == JTokenType.Integer)
				{
					return token.Value<int>();
				}
				if (int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
				{
					return Math.Max(0, parsed);
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void ApplyFinishReasonStatus(NpcPolicyApiCallResult result, string source, string resolvedRoute, bool thinkingRetriedPlain)
	{
		if (result == null)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(result.Content))
		{
			result.Success = false;
			if (LlmApiCompat.IsReasoningOnlyTokenLimitResponse(result.ResponseBody, out int completionTokens, out int reasoningTokens))
			{
				result.ErrorMessage = "LLM returned no assistant content because the output token budget was consumed by reasoning; completion_tokens="
					+ completionTokens.ToString(CultureInfo.InvariantCulture)
					+ " reasoning_tokens=" + reasoningTokens.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				result.ErrorMessage = "LLM returned a successful HTTP response without assistant content";
			}
			Log(source, "[HTTP] NPC policy response contained no assistant content. route=" + resolvedRoute + " thinking_retry_plain=" + (thinkingRetriedPlain ? "true" : "false"));
			return;
		}
		string finishReason = (result.FinishReason ?? "").Trim();
		if (string.IsNullOrWhiteSpace(finishReason))
		{
			result.Success = true;
			Log(source, "[HTTP] NPC policy finish_reason missing; treating response as success for compatibility. route=" + resolvedRoute + " thinking_retry_plain=" + (thinkingRetriedPlain ? "true" : "false"));
			return;
		}
		string normalized = finishReason.ToLowerInvariant();
		if (normalized == "stop")
		{
			result.Success = true;
			return;
		}
		result.Success = false;
		if (normalized == "length")
		{
			result.IsOutputTruncated = true;
			result.ErrorMessage = "LLM output truncated because finish_reason=length; increase max_tokens or reduce batch size";
			return;
		}
		if (normalized == "content_filter")
		{
			result.ErrorMessage = "LLM output blocked because finish_reason=content_filter";
			return;
		}
		if (normalized == "insufficient_system_resource")
		{
			result.ErrorMessage = "LLM output failed because finish_reason=insufficient_system_resource";
			return;
		}
		result.ErrorMessage = "LLM returned non-stop finish_reason=" + finishReason + "; not treating response as successful JSON output";
	}

	private static void ApplyHttpFailureDetails(NpcPolicyApiCallResult result, HttpResponseMessage response, string responseBody)
	{
		RetryAfterInfo.FromResponse(response).ApplyTo(result);
		result.IsAuthFailure = IsAuthenticationFailureResponse(response.StatusCode, responseBody);
		result.IsQuotaLimit = response.StatusCode == (HttpStatusCode)429 && IsQuotaLimitResponseBody(responseBody);
		result.IsRequestsPerMinuteLimit = response.StatusCode == (HttpStatusCode)429 && !result.IsQuotaLimit && (IsRequestsPerMinuteLimitResponseBody(responseBody) || HasRequestsPerMinuteRateLimitHeaders(response));
		result.IsRateLimit = response.StatusCode == (HttpStatusCode)429 || result.IsRequestsPerMinuteLimit || (!result.IsQuotaLimit && IsGenericRateLimitResponseBody(responseBody));
		result.ErrorMessage = BuildApiFailureMessage(response.StatusCode, responseBody, result.RetryAfterSeconds, result.RetryAfterSecondsRaw, result.RetryAfterSecondsCapped, result.IsRateLimit, result.IsRequestsPerMinuteLimit, result.IsQuotaLimit);
	}

	private static JObject BuildCompatibleChatRequestBody(PolicyApiExecutionProfile profile, JArray messages, int maxTokens, PolicyRequestCapabilities capabilities, out string thinkingMode)
	{
		JObject body = new JObject
		{
			["model"] = profile.ModelName,
			["messages"] = messages,
			["stream"] = false
		};
		if (capabilities.TokenParameterMode == PolicyTokenParameterMode.MaxTokens)
		{
			body["max_tokens"] = maxTokens;
		}
		else if (capabilities.TokenParameterMode == PolicyTokenParameterMode.MaxCompletionTokens)
		{
			body["max_completion_tokens"] = maxTokens;
		}
		if (!capabilities.OmitTemperature)
		{
			body["temperature"] = profile.Temperature;
		}
		if (profile.UseJsonObjectResponse)
		{
			body["response_format"] = new JObject { ["type"] = "json_object" };
		}
		if (capabilities.OmitThinkingControls)
		{
			thinkingMode = "plain";
		}
		else
		{
			DuelSettings.ApplyThinkingControls(
				body,
				profile.EffectiveApiUrl,
				profile.ModelName,
				profile.ThinkingEnabled,
				profile.ReasoningEffort,
				out thinkingMode);
		}
		return body;
	}

	private static bool TryApplyPolicyCompatibilityDowngrade(string responseBody, int requestedMaxTokens, PolicyRequestCapabilities capabilities, out string reason)
	{
		List<string> changes = new List<string>();
		bool maxTokensReduced = TryReadAdvertisedMaxTokens(responseBody, requestedMaxTokens, out int advertisedMaxTokens)
			&& (!capabilities.MaxTokensOverride.HasValue || advertisedMaxTokens < capabilities.MaxTokensOverride.Value);
		if (maxTokensReduced)
		{
			capabilities.MaxTokensOverride = advertisedMaxTokens;
			changes.Add("maxTokens=" + advertisedMaxTokens.ToString(CultureInfo.InvariantCulture));
		}
		if (!capabilities.OmitThinkingControls && LooksLikeNpcThinkingControlError(responseBody))
		{
			capabilities.OmitThinkingControls = true;
			changes.Add("thinking=omitted");
		}
		if (!capabilities.OmitTemperature && LooksLikeTemperatureControlError(responseBody))
		{
			capabilities.OmitTemperature = true;
			changes.Add("temperature=omitted");
		}
		if (!maxTokensReduced && LooksLikeTokenParameterError(responseBody, capabilities.TokenParameterMode))
		{
			if (capabilities.TokenParameterMode == PolicyTokenParameterMode.MaxTokens)
			{
				capabilities.TokenParameterMode = PolicyTokenParameterMode.MaxCompletionTokens;
				changes.Add("tokenField=max_completion_tokens");
			}
			else if (capabilities.TokenParameterMode == PolicyTokenParameterMode.MaxCompletionTokens)
			{
				capabilities.TokenParameterMode = PolicyTokenParameterMode.Omit;
				changes.Add("tokenField=omitted");
			}
		}
		reason = string.Join(",", changes);
		return changes.Count > 0;
	}

	private static bool TryApplyLegacyThinkingDowngrade(string responseBody, PolicyRequestCapabilities capabilities, out string reason)
	{
		if (!capabilities.OmitThinkingControls && LooksLikeNpcThinkingControlError(responseBody))
		{
			capabilities.OmitThinkingControls = true;
			reason = "thinking=omitted";
			return true;
		}
		reason = "";
		return false;
	}

	private static bool LooksLikeTokenParameterError(string responseBody, PolicyTokenParameterMode mode)
	{
		string field = mode == PolicyTokenParameterMode.MaxCompletionTokens ? "max_completion_tokens" : "max_tokens";
		return ContainsAnyIgnoreCase(responseBody, field)
			&& ContainsAnyIgnoreCase(responseBody, "unsupported", "not supported", "unknown", "unrecognized", "unexpected", "invalid parameter", "extra inputs", "not permitted", "不支持", "未知", "无法识别", "无效参数");
	}

	private static bool LooksLikeTemperatureControlError(string responseBody)
	{
		return ContainsAnyIgnoreCase(responseBody, "temperature")
			&& ContainsAnyIgnoreCase(responseBody, "unsupported", "not supported", "unknown", "unrecognized", "unexpected", "invalid", "only the default", "not permitted", "不支持", "未知", "无法识别", "无效");
	}

	private static bool TryReadAdvertisedMaxTokens(string responseBody, int requestedMaxTokens, out int advertisedMaxTokens)
	{
		advertisedMaxTokens = 0;
		string text = responseBody ?? "";
		if (!ContainsAnyIgnoreCase(text, "token", "令牌") || !ContainsAnyIgnoreCase(text, "maximum", "max allowed", "at most", "less than or equal", "must be <=", "不能超过", "最大"))
		{
			return false;
		}
		Match match = Regex.Match(text, @"(?i)(?:maximum|max(?:imum)?\s+allowed|at\s+most|less\s+than\s+or\s+equal\s+to|must\s+be\s*<=|不能超过|最大)[^0-9]{0,64}([0-9]{1,6})");
		if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
		{
			return false;
		}
		advertisedMaxTokens = parsed;
		return parsed > 0 && parsed < requestedMaxTokens;
	}

	private static bool IsTransientPolicyFailure(NpcPolicyApiCallResult result)
	{
		int statusCode = result?.StatusCode ?? 0;
		return statusCode == 408 || statusCode == 429 || statusCode >= 500;
	}

	private static string GetTokenParameterLogName(PolicyTokenParameterMode mode)
	{
		switch (mode)
		{
		case PolicyTokenParameterMode.MaxCompletionTokens:
			return "max_completion_tokens";
		case PolicyTokenParameterMode.Omit:
			return "omitted";
		default:
			return "max_tokens";
		}
	}

	private static string BuildCapabilityCacheKey(string effectiveApiUrl, string modelName)
	{
		return (effectiveApiUrl ?? "").Trim().ToLowerInvariant() + "\n" + (modelName ?? "").Trim().ToLowerInvariant();
	}

	private static PolicyRequestCapabilities GetCachedCapabilities(string key)
	{
		lock (CapabilityCacheLock)
		{
			return CapabilityCache.TryGetValue(key ?? "", out PolicyRequestCapabilities cached)
				? cached.Clone()
				: new PolicyRequestCapabilities();
		}
	}

	private static void CacheSuccessfulCapabilities(string key, PolicyRequestCapabilities capabilities)
	{
		if (string.IsNullOrWhiteSpace(key) || capabilities == null)
		{
			return;
		}
		lock (CapabilityCacheLock)
		{
			if (!CapabilityCache.ContainsKey(key))
			{
				while (CapabilityCache.Count >= MaxCapabilityCacheEntries && CapabilityCacheOrder.Count > 0)
				{
					CapabilityCache.Remove(CapabilityCacheOrder.Dequeue());
				}
				CapabilityCacheOrder.Enqueue(key);
			}
			CapabilityCache[key] = capabilities.Clone();
		}
	}

	private static string BuildApiStagePrefix(string source, string stage)
	{
		return (source ?? "NpcPolicy") + "_" + (stage ?? "api");
	}

	private static string BuildRequestStartLog(string resolvedRoute, string modelName, int maxTokens, string thinkingMode, string effectiveApiUrl)
	{
		return "[HTTP] NPC policy request route=" + resolvedRoute
			+ " model=" + modelName
			+ " maxTokens=" + maxTokens.ToString(CultureInfo.InvariantCulture)
			+ " thinking=" + thinkingMode
			+ " url=" + effectiveApiUrl;
	}

	private static void RecordHttpErrorTokenStatsSafe(JArray messages, string resolvedRoute, string modelName, HttpResponseMessage response, string responseBody, NpcPolicyApiCallResult result, bool thinkingRetriedPlain, string requestBodyForTokenStats)
	{
		try
		{
			Logger.RecordTokenStats(Logger.EstimateTokensFromMessages(messages), 0, messages, BuildHttpErrorTokenStatsText(resolvedRoute, modelName, response, responseBody, result, thinkingRetriedPlain), "npc_policy_api_http_error", requestBodyForTokenStats);
		}
		catch
		{
		}
	}

	private static void RecordHttpSuccessTokenStatsSafe(JArray messages, string resolvedRoute, string modelName, string thinkingMode, bool thinkingRetriedPlain, string content, string responseBody, string requestBodyForTokenStats)
	{
		try
		{
			Logger.RecordTokenStats(Logger.EstimateTokensFromMessages(messages), Logger.EstimateTokens(content), messages, BuildHttpSuccessTokenStatsText(resolvedRoute, modelName, thinkingMode, thinkingRetriedPlain, content, responseBody), "npc_policy_api", requestBodyForTokenStats);
		}
		catch
		{
		}
	}

	private static string BuildHttpErrorTokenStatsText(string resolvedRoute, string modelName, HttpResponseMessage response, string responseBody, NpcPolicyApiCallResult result, bool thinkingRetriedPlain)
	{
		return "[NPC POLICY API HTTP ERROR]\nroute=" + resolvedRoute
			+ "\nmodel=" + modelName
			+ "\nstatus=" + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + (response.ReasonPhrase ?? "")
			+ "\nretry_after_seconds=" + (result?.RetryAfterSeconds?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ "\nretry_after_seconds_raw=" + (result?.RetryAfterSecondsRaw?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ "\nretry_after_capped=" + ((result?.RetryAfterSecondsCapped ?? false) ? "true" : "false")
			+ "\nthinking_retry_plain=" + (thinkingRetriedPlain ? "true" : "false")
			+ "\nresponse_body=\n" + (responseBody ?? "");
	}

	private static string BuildHttpSuccessTokenStatsText(string resolvedRoute, string modelName, string thinkingMode, bool thinkingRetriedPlain, string content, string responseBody)
	{
		return "[NPC POLICY API HTTP]\nroute=" + resolvedRoute
			+ "\nmodel=" + modelName
			+ "\ncontrol_mode=" + thinkingMode
			+ "\nthinking_retry_plain=" + (thinkingRetriedPlain ? "true" : "false")
			+ "\nai_response=\n" + (content ?? "")
			+ "\nraw_response_sample=\n" + TrimForLog(responseBody);
	}

	private static async Task<HttpResponseMessage> SendNpcPolicyRequestWithHardTimeoutAsync(string effectiveApiUrl, string apiKey, string jsonBody, int hardTimeoutMilliseconds, string source, NpcPolicyApiCallResult result, CancellationToken cancellationToken)
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
		LlmApiCompat.ApplyAuthenticationHeaders(request, effectiveApiUrl, apiKey);
		request.Content = new StringContent(jsonBody ?? "{}", Encoding.UTF8, "application/json");
		using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		using CancellationTokenSource delayCts = new CancellationTokenSource();
		Task<HttpResponseMessage> apiTask = DuelSettings.GlobalClient.SendAsync(request, timeoutCts.Token);
		Task completed = await Task.WhenAny(apiTask, Task.Delay(hardTimeoutMilliseconds, delayCts.Token));
		if (completed != apiTask)
		{
			CancelNoThrow(timeoutCts);
			MarkHardTimeout(result, source, hardTimeoutMilliseconds);
			_ = ObserveTimedOutApiTaskAsync(apiTask, source);
			return null;
		}
		CancelNoThrow(delayCts);
		try
		{
			return await apiTask;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (TaskCanceledException ex) when (timeoutCts.IsCancellationRequested)
		{
			MarkHardTimeout(result, source, hardTimeoutMilliseconds);
			Log(source, "[HTTP] NPC policy request canceled by hard timeout: " + ex.GetType().Name);
			return null;
		}
		catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
		{
			MarkHardTimeout(result, source, hardTimeoutMilliseconds);
			Log(source, "[HTTP] NPC policy request canceled by hard timeout: " + ex.GetType().Name);
			return null;
		}
	}

	private static async Task ObserveTimedOutApiTaskAsync(Task<HttpResponseMessage> apiTask, string source)
	{
		try
		{
			using HttpResponseMessage lateResponse = await apiTask;
			Log(source, "[HTTP] Timed-out NPC policy request eventually returned after cancellation; response disposed.");
		}
		catch (TaskCanceledException)
		{
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			Log(source, "[HTTP] Timed-out NPC policy request ended with exception after cancellation: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void MarkHardTimeout(NpcPolicyApiCallResult result, string source, int hardTimeoutMilliseconds)
	{
		if (result == null)
		{
			return;
		}
		result.IsTimeout = true;
		result.ErrorMessage = (source ?? "NpcPolicy") + " api timeout after " + hardTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms";
	}

	private static void CancelNoThrow(CancellationTokenSource cancellationTokenSource)
	{
		try
		{
			cancellationTokenSource?.Cancel();
		}
		catch
		{
		}
	}

	private static bool TryResolvePolicyApiConfig(string requestedSource, bool followSelectedApiTokens, int customMaxTokens, float? fixedTemperature, out PolicyApiExecutionProfile profile, out string errorMessage)
	{
		return TryResolvePolicyApiConfig(DuelSettings.GetSettings(), requestedSource, followSelectedApiTokens, customMaxTokens, fixedTemperature, out profile, out errorMessage);
	}

	private static bool TryResolvePolicyApiConfig(DuelSettings settings, string requestedSource, bool followSelectedApiTokens, int customMaxTokens, float? fixedTemperature, out PolicyApiExecutionProfile profile, out string errorMessage)
	{
		profile = null;
		errorMessage = "请检查 MCM 的政策 API 来源或主 API 设置。";
		if (settings == null)
		{
			return false;
		}
		string source = NormalizePolicyApiSource(requestedSource);
		ReadPolicyApiSourceConfig(settings, source, out string selectedUrl, out string selectedKey, out string selectedModel, out int selectedMaxTokens, out float selectedTemperature, out bool hasSelectedField);
		string effectiveSelectedUrl = DuelSettings.GetEffectiveApiUrl(selectedUrl);
		bool selectedComplete = !string.IsNullOrWhiteSpace(effectiveSelectedUrl)
			&& !string.IsNullOrWhiteSpace(selectedKey)
			&& !string.IsNullOrWhiteSpace(selectedModel);

		string resolvedRoute;
		string effectiveApiUrl;
		string apiKey;
		string modelName;
		int effectiveSourceMaxTokens;
		float effectiveSourceTemperature;
		if (source == DuelSettings.PolicyApiSourceMain && selectedComplete)
		{
			resolvedRoute = "main";
			effectiveApiUrl = effectiveSelectedUrl;
			apiKey = selectedKey;
			modelName = selectedModel;
			effectiveSourceMaxTokens = selectedMaxTokens;
			effectiveSourceTemperature = selectedTemperature;
		}
		else if (source != DuelSettings.PolicyApiSourceMain && selectedComplete)
		{
			resolvedRoute = source + "_dedicated";
			effectiveApiUrl = effectiveSelectedUrl;
			apiKey = selectedKey;
			modelName = selectedModel;
			effectiveSourceMaxTokens = selectedMaxTokens;
			effectiveSourceTemperature = selectedTemperature;
		}
		else
		{
			string mainUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl ?? "");
			string mainKey = (settings.ApiKey ?? "").Trim();
			string mainModel = (settings.GetEffectiveMainModelName() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(mainUrl) || string.IsNullOrWhiteSpace(mainKey) || string.IsNullOrWhiteSpace(mainModel))
			{
				errorMessage = source == DuelSettings.PolicyApiSourceMain
					? "主 API 配置不完整，政策生成无法开始。"
					: "所选政策 API 配置不完整，且主 API 也不完整，政策生成无法开始。";
				return false;
			}
			resolvedRoute = source == DuelSettings.PolicyApiSourceMain
				? "main"
				: source + (hasSelectedField ? "_partial_fallback_main" : "_fallback_main");
			effectiveApiUrl = mainUrl;
			apiKey = mainKey;
			modelName = mainModel;
			effectiveSourceMaxTokens = settings.GetMainApiMaxTokens();
			effectiveSourceTemperature = settings.GetMainApiTemperature();
		}

		profile = new PolicyApiExecutionProfile
		{
			RequestedSource = source,
			ResolvedRoute = resolvedRoute,
			EffectiveApiUrl = effectiveApiUrl,
			ApiKey = apiKey,
			ModelName = modelName,
			MaxTokens = followSelectedApiTokens
				? Math.Max(1, effectiveSourceMaxTokens)
				: DuelSettings.ClampPolicyApiMaxTokens(customMaxTokens),
			Temperature = fixedTemperature.HasValue
				? DuelSettings.ClampApiTemperature(fixedTemperature.Value)
				: DuelSettings.ClampApiTemperature(effectiveSourceTemperature)
		};
		errorMessage = "";
		return true;
	}

	private static string NormalizePolicyApiSource(string source)
	{
		string normalized = (source ?? "").Trim().ToLowerInvariant();
		switch (normalized)
		{
		case DuelSettings.PolicyApiSourceAuxiliary:
		case DuelSettings.PolicyApiSourceActionPostprocess:
		case DuelSettings.PolicyApiSourceEventAndRebellion:
			return normalized;
		default:
			return DuelSettings.PolicyApiSourceMain;
		}
	}

	private static void ReadPolicyApiSourceConfig(DuelSettings settings, string source, out string rawUrl, out string apiKey, out string modelName, out int maxTokens, out float temperature, out bool hasAnyField)
	{
		rawUrl = "";
		apiKey = "";
		modelName = "";
		maxTokens = DuelSettings.DefaultGeneralApiMaxTokens;
		temperature = 0.8f;
		hasAnyField = false;
		switch (source)
		{
		case DuelSettings.PolicyApiSourceAuxiliary:
			rawUrl = (settings.AuxiliaryApiUrl ?? "").Trim();
			apiKey = (settings.AuxiliaryApiKey ?? "").Trim();
			modelName = (settings.GetEffectiveAuxiliaryModelName() ?? "").Trim();
			maxTokens = settings.GetAuxiliaryApiMaxTokens();
			temperature = settings.GetAuxiliaryApiTemperature();
			hasAnyField = !string.IsNullOrWhiteSpace(rawUrl) || !string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace((settings.AuxiliaryModelName ?? "").Trim()) || !string.IsNullOrWhiteSpace(settings.GetAuxiliarySelectedModelOption());
			break;
		case DuelSettings.PolicyApiSourceActionPostprocess:
			rawUrl = (settings.ActionPostprocessApiUrl ?? "").Trim();
			apiKey = (settings.ActionPostprocessApiKey ?? "").Trim();
			modelName = (settings.GetEffectiveActionPostprocessModelName() ?? "").Trim();
			maxTokens = settings.GetActionPostprocessApiMaxTokens();
			temperature = settings.GetActionPostprocessApiTemperature();
			hasAnyField = !string.IsNullOrWhiteSpace(rawUrl) || !string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace((settings.ActionPostprocessModelName ?? "").Trim()) || !string.IsNullOrWhiteSpace(settings.GetActionPostprocessSelectedModelOption());
			break;
		case DuelSettings.PolicyApiSourceEventAndRebellion:
			rawUrl = (settings.EventAndRebellionApiUrl ?? "").Trim();
			apiKey = (settings.EventAndRebellionApiKey ?? "").Trim();
			modelName = (settings.GetEffectiveEventAndRebellionModelName() ?? "").Trim();
			maxTokens = settings.GetEventAndRebellionApiMaxTokens();
			temperature = settings.GetEventAndRebellionApiTemperature();
			hasAnyField = !string.IsNullOrWhiteSpace(rawUrl) || !string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace((settings.EventAndRebellionModelName ?? "").Trim()) || !string.IsNullOrWhiteSpace(settings.GetEventAndRebellionSelectedModelOption());
			break;
		default:
			rawUrl = (settings.ApiUrl ?? "").Trim();
			apiKey = (settings.ApiKey ?? "").Trim();
			modelName = (settings.GetEffectiveMainModelName() ?? "").Trim();
			maxTokens = settings.GetMainApiMaxTokens();
			temperature = settings.GetMainApiTemperature();
			hasAnyField = !string.IsNullOrWhiteSpace(rawUrl) || !string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace(modelName);
			break;
		}
	}

	internal static JArray BuildMessageArray(string systemPrompt)
	{
		return new JArray
		{
			new JObject
			{
				["role"] = "system",
				["content"] = systemPrompt ?? ""
			},
			new JObject
			{
				["role"] = "user",
				["content"] = "请根据以上规则生成本次统治者政策，只输出 JSON。"
			}
		};
	}

	private static bool ContainsAnyIgnoreCase(string text, params string[] tokens)
	{
		string source = text ?? "";
		foreach (string token in tokens ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(token) && source.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsAuthenticationFailureResponse(HttpStatusCode statusCode, string responseBody)
	{
		if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
		{
			return true;
		}
		return ContainsAnyIgnoreCase(responseBody, "authentication_error", "authentication fails", "authentication failed", "invalid api key", "api key is invalid", "apikey is invalid", "incorrect api key", "invalid authentication", "unauthorized", "forbidden");
	}

	private static bool IsQuotaLimitResponseBody(string responseBody)
	{
		return ContainsAnyIgnoreCase(responseBody, "quota", "balance", "insufficient", "credit", "billing", "额度", "余额", "欠费");
	}

	private static bool IsRequestsPerMinuteLimitResponseBody(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (ContainsAnyIgnoreCase(text, "rpm", "requests per minute", "request per minute", "requests/min", "request/min", "requests per min", "request per min", "req/min", "req per min", "每分钟请求", "每分钟最多请求"))
		{
			return true;
		}
		return ContainsAnyIgnoreCase(text, "request", "requests", "请求", "req") && ContainsAnyIgnoreCase(text, "minute", "min", "/min", "per min", "per-minute", "每分钟");
	}

	private static bool IsGenericRateLimitResponseBody(string responseBody)
	{
		return ContainsAnyIgnoreCase(responseBody, "rate limit", "too many requests", "ratelimit", "限流", "请求过于频繁", "请求频率过高", "速率限制");
	}

	private static bool LooksLikeNpcThinkingControlError(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		bool hasThinkingField = ContainsAnyIgnoreCase(text, "thinking", "reasoning_effort", "output_config", "budget_tokens");
		bool hasUnsupportedSignal = ContainsAnyIgnoreCase(text, "unsupported", "unknown", "invalid", "unexpected", "not allowed", "not supported", "extra inputs are not permitted");
		return hasThinkingField && hasUnsupportedSignal;
	}

	private static int? CapRetryAfterSeconds(int? retryAfterSecondsRaw, out bool capped)
	{
		capped = false;
		if (!retryAfterSecondsRaw.HasValue)
		{
			return null;
		}
		int raw = Math.Max(0, retryAfterSecondsRaw.Value);
		if (raw > MaxRetryAfterDelaySeconds)
		{
			capped = true;
			return MaxRetryAfterDelaySeconds;
		}
		return raw;
	}

	private static int? TryGetRetryAfterSeconds(HttpResponseMessage response)
	{
		if (response == null)
		{
			return null;
		}
		try
		{
			if (response.Headers?.RetryAfter?.Delta != null)
			{
				return Math.Max(0, (int)Math.Ceiling(response.Headers.RetryAfter.Delta.Value.TotalSeconds));
			}
			if (response.Headers != null && response.Headers.TryGetValues("Retry-After", out var values))
			{
				string text = values?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
				if (int.TryParse((text ?? "").Trim(), out int seconds))
				{
					return Math.Max(0, seconds);
				}
				if (DateTimeOffset.TryParse(text, out var retryAt))
				{
					return Math.Max(0, (int)Math.Ceiling((retryAt - DateTimeOffset.UtcNow).TotalSeconds));
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool HasRequestsPerMinuteRateLimitHeaders(HttpResponseMessage response)
	{
		if (response?.Headers == null)
		{
			return false;
		}
		try
		{
			foreach (KeyValuePair<string, IEnumerable<string>> item in response.Headers)
			{
				string key = (item.Key ?? "").Trim();
				if (ContainsAnyIgnoreCase(key, "ratelimit", "rate-limit", "limit-requests", "remaining-requests", "reset-requests"))
				{
					return true;
				}
				if (IsRequestsPerMinuteLimitResponseBody(string.Join(" ", item.Value ?? Enumerable.Empty<string>())))
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

	private static string BuildApiFailureMessage(HttpStatusCode statusCode, string responseBody, int? retryAfterSeconds, int? retryAfterSecondsRaw, bool retryAfterCapped, bool isRateLimit, bool isRequestsPerMinuteLimit, bool isQuotaLimit)
	{
		StringBuilder builder = new StringBuilder();
		if (isRequestsPerMinuteLimit)
		{
			builder.Append("请求疑似触发了 RPM（每分钟请求数）限流");
		}
		else if (isQuotaLimit)
		{
			builder.Append("账号额度或余额不足，导致请求被拒绝");
		}
		else if (isRateLimit)
		{
			builder.Append("请求触发了速率限制");
		}
		else
		{
			builder.Append("接口请求失败");
		}
		builder.Append("（HTTP ").Append(((int)statusCode).ToString(CultureInfo.InvariantCulture)).Append(" ").Append(statusCode).Append("）");
		if (retryAfterSeconds.HasValue)
		{
			builder.Append("，建议等待 ").Append(retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture)).Append(" 秒后再试");
			if (retryAfterCapped && retryAfterSecondsRaw.HasValue)
			{
				builder.Append("（原始 Retry-After: ").Append(retryAfterSecondsRaw.Value.ToString(CultureInfo.InvariantCulture)).Append(" 秒，已按上限 ").Append(MaxRetryAfterDelaySeconds.ToString(CultureInfo.InvariantCulture)).Append(" 秒截断）");
			}
		}
		string body = (responseBody ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(body))
		{
			builder.Append("：").Append(TrimForLog(body, 1200));
		}
		return builder.ToString();
	}

	private static string TrimForLog(string text, int maxChars = 3000)
	{
		text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, maxChars) + "...";
	}

	private static void Log(string source, string message)
	{
		PolicySystemLog.WriteRuntime("Npc", (source ?? "NpcPolicyLlm") + " " + (message ?? ""));
	}
}

internal static class NpcPolicyStructuredParseLogger
{
	private const int SampleChars = 1200;

	internal static void LogFailure(string logSource, string kind, string batchId, string route, int attempts, string reason, string raw, string extracted)
	{
		string message = kind + "-parse-failed"
			+ " batchId=" + CleanField(batchId)
			+ " route=" + CleanField(route)
			+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
			+ " reason=" + CleanField(reason)
			+ " raw_sample=" + OneLine(Clip(raw))
			+ " extracted_sample=" + OneLine(Clip(extracted));
		PolicySystemLog.Failure("Npc", kind + "-parse-failed", message,
			"raw_sample:\n" + Clip(raw) + "\n\nextracted_sample:\n" + Clip(extracted));
	}

	private static string CleanField(string text)
	{
		return OneLine(text).Trim();
	}

	private static string OneLine(string text)
	{
		return (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\\n").Trim();
	}

	private static string Clip(string text)
	{
		text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (text.Length <= SampleChars)
		{
			return text;
		}
		return text.Substring(0, SampleChars) + "...";
	}
}

internal static class NpcPolicyLlmClient
{
	public static bool IsConfiguredForNpcPolicy(out string errorMessage)
	{
		return PolicyLlmClient.IsConfiguredForLegacyEventApi(out errorMessage);
	}

	public static Task<NpcPolicyApiCallResult> CallEventAndRebellionApiWithRetriesAsync(string systemPrompt, int maxTokens, int hardTimeoutMilliseconds, string source, long runtimeGeneration, int maxAttempts = 3)
	{
		return PolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(systemPrompt, maxTokens, hardTimeoutMilliseconds, source, runtimeGeneration, maxAttempts);
	}
}
