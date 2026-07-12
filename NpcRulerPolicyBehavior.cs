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

public sealed class NpcRulerPolicyRecord
{
	public int Version { get; set; } = 1;

	[JsonProperty("policyId")]
	public string PolicyId { get; set; }

	[JsonProperty("batchId")]
	public string BatchId { get; set; }

	[JsonProperty("kingdomId")]
	public string KingdomId { get; set; }

	[JsonProperty("kingdomName")]
	public string KingdomName { get; set; }

	[JsonProperty("rulerHeroId")]
	public string RulerHeroId { get; set; }

	[JsonProperty("rulerName")]
	public string RulerName { get; set; }

	[JsonProperty("policyName")]
	public string PolicyName { get; set; }

	[JsonProperty("policyContent")]
	public string PolicyContent { get; set; }

	[JsonProperty("policyDigest")]
	public string PolicyDigest { get; set; }

	[JsonProperty("publicFeedback")]
	public string PublicFeedback { get; set; }

	[JsonProperty("feedbackTitle")]
	public string FeedbackTitle { get; set; }

	[JsonProperty("feedbackDigest")]
	public string FeedbackDigest { get; set; }

	[JsonProperty("isPlayerPolicy")]
	public bool IsPlayerPolicy { get; set; }

	[JsonProperty("eventType")]
	public string EventType { get; set; }

	[JsonProperty("impactSummary")]
	public string ImpactSummary { get; set; }

	public int Day { get; set; }

	public string GameDate { get; set; }

	public long CreatedUtcTicks { get; set; }

	[JsonProperty("effects")]
	public List<NpcRulerPolicyEffectDto> Effects { get; set; } = new List<NpcRulerPolicyEffectDto>();
}

public sealed class NpcRulerPolicyEffectDto
{
	[JsonProperty("effectId")]
	public string EffectId { get; set; }

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; }

	[JsonProperty("targetKingdomName")]
	public string TargetKingdomName { get; set; }

	[JsonProperty("prosperityDailyDeltaPerTown")]
	public float ProsperityDailyDeltaPerTown { get; set; }

	[JsonProperty("foodDailyDeltaPerTown")]
	public float FoodDailyDeltaPerTown { get; set; }

	[JsonProperty("hearthDailyDeltaPerVillage")]
	public float HearthDailyDeltaPerVillage { get; set; }

	[JsonProperty("loyaltyDailyDeltaPerTown")]
	public float LoyaltyDailyDeltaPerTown { get; set; }

	[JsonProperty("securityDailyDeltaPerTown")]
	public float SecurityDailyDeltaPerTown { get; set; }

	[JsonProperty("militiaDailyDeltaPerTown")]
	public float MilitiaDailyDeltaPerTown { get; set; }

	[JsonProperty("kingdomStabilityDailyDelta")]
	public float KingdomStabilityDailyDelta { get; set; }

	[JsonProperty("durationDays")]
	public int DurationDays { get; set; }

	[JsonProperty("remainingDays")]
	public int RemainingDays { get; set; }

	[JsonProperty("isEnded")]
	public bool IsEnded { get; set; }

	[JsonProperty("reason")]
	public string Reason { get; set; }
}

internal sealed class NpcPolicyPrompt
{
	public string SystemPrompt = "";

	public string UserPrompt = "";

	public string Preview => "System:\n" + (SystemPrompt ?? "") + "\n\nUser:\n" + (UserPrompt ?? "");
}

internal sealed class NpcPolicyApiCallResult
{
	public bool Success;

	public string Content = "";

	public string ErrorMessage = "";

	public string FinishReason = "";

	public bool IsOutputTruncated;

	public int? PromptTokens;

	public int? CompletionTokens;

	public int? TotalTokens;

	public int? PromptCacheHitTokens;

	public int? PromptCacheMissTokens;

	public int? StatusCode;

	public string ResponseBody = "";

	public bool IsRateLimit;

	public bool IsRequestsPerMinuteLimit;

	public bool IsQuotaLimit;

	public bool IsAuthFailure;

	public bool IsTimeout;

	public int? RetryAfterSeconds;

	public int? RetryAfterSecondsRaw;

	public bool RetryAfterSecondsCapped;

	public int AttemptsUsed;

	public string ResolvedRoute = "";

	public bool ThinkingRetryPlain;
}

internal static class NpcPolicyLlmClient
{
	private const int DefaultMaxAttempts = 3;

	private const int MaxRetryAfterDelaySeconds = 180;

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
		return TryResolveEventAndRebellionApiConfig(DuelSettings.GetSettings(), out var _, out var _, out var _, out var _, out errorMessage);
	}

	public static async Task<NpcPolicyApiCallResult> CallEventAndRebellionApiWithRetriesAsync(string systemPrompt, string userPrompt, int maxTokens, int hardTimeoutMilliseconds, string source, long runtimeGeneration, int maxAttempts = DefaultMaxAttempts)
	{
		NpcPolicyApiCallResult finalResult = new NpcPolicyApiCallResult();
		int attempts = Math.Max(1, maxAttempts);
		for (int attempt = 1; attempt <= attempts; attempt++)
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, (source ?? "NpcPolicy") + "_api_before_attempt"))
			{
				finalResult.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				finalResult.AttemptsUsed = attempt;
				return finalResult;
			}
			NpcPolicyApiCallResult result = await CallEventAndRebellionApiOnceAsync(systemPrompt, userPrompt, Math.Max(1, maxTokens), Math.Max(1000, hardTimeoutMilliseconds), source, runtimeGeneration);
			result.AttemptsUsed = attempt;
			finalResult = result;
			RecordPromptExchangeSafe(source, attempt, attempts, systemPrompt, userPrompt, result);
			if (result.Success)
			{
				return result;
			}
			if (result.IsAuthFailure)
			{
				Log(source, "[HTTP] NPC policy retry stopped because authentication failure was detected. attempts_used=" + attempt.ToString(CultureInfo.InvariantCulture));
				return result;
			}
			if (result.IsQuotaLimit)
			{
				Log(source, "[HTTP] NPC policy retry stopped because quota/balance limit was detected. attempts_used=" + attempt.ToString(CultureInfo.InvariantCulture));
				return result;
			}
			if (result.IsOutputTruncated)
			{
				Log(source, "[HTTP] NPC policy retry stopped because output was truncated by finish_reason=length. attempts_used=" + attempt.ToString(CultureInfo.InvariantCulture));
				return result;
			}
			if (attempt < attempts)
			{
				RetryBackoffPlan backoff = RetryBackoffPlan.FromResult(result);
				Log(source, backoff.BuildLog(attempt, attempts));
				await Task.Delay(backoff.DelayMilliseconds);
			}
		}
		return finalResult;
	}

	private static async Task<NpcPolicyApiCallResult> CallEventAndRebellionApiOnceAsync(string systemPrompt, string userPrompt, int maxTokens, int hardTimeoutMilliseconds, string source, long runtimeGeneration)
	{
		NpcPolicyApiCallResult result = new NpcPolicyApiCallResult();
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (!TryResolveEventAndRebellionApiConfig(settings, out var effectiveApiUrl, out var apiKey, out var modelName, out var resolvedRoute, out var errorMessage))
			{
				result.ErrorMessage = errorMessage;
				return result;
			}
			result.ResolvedRoute = resolvedRoute;
			JArray messages = BuildMessageArray(systemPrompt, userPrompt);
			JObject body = BuildChatRequestBody(modelName, messages, maxTokens, ResolveTemperature(settings, resolvedRoute));
			DuelSettings.ApplyThinkingControls(body, effectiveApiUrl, modelName, thinkingEnabled: false, DuelSettings.ReasoningEffortHigh, out var thinkingMode);
			string jsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, body);
			Log(source, BuildRequestStartLog(resolvedRoute, modelName, maxTokens, thinkingMode, effectiveApiUrl));
			NpcPolicyHttpExchange exchange = await SendAndReadNpcPolicyExchangeAsync(effectiveApiUrl, apiKey, jsonBody, hardTimeoutMilliseconds, source, runtimeGeneration, BuildApiStagePrefix(source, "api"), result);
			if (exchange == null)
			{
				return result;
			}
			try
			{
				if (ShouldRetryWithoutThinkingControls(exchange.Response, exchange.ResponseBody, thinkingMode))
				{
					Log(source, "[HTTP] NPC policy thinking controls rejected; retrying without thinking controls. route=" + resolvedRoute + " thinking_retry_plain=true");
					exchange.Dispose();
					exchange = null;
					JObject retryBody = BuildPlainRetryBody(body);
					string retryJsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, retryBody);
					result.ThinkingRetryPlain = true;
					thinkingMode += "_retry_plain";
					exchange = await SendAndReadNpcPolicyExchangeAsync(effectiveApiUrl, apiKey, retryJsonBody, hardTimeoutMilliseconds, source, runtimeGeneration, BuildApiStagePrefix(source, "api_retry"), result);
					if (exchange == null)
					{
						return result;
					}
					Log(source, "[HTTP] NPC policy thinking plain retry response status=" + ((int)exchange.Response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + (exchange.Response.ReasonPhrase ?? "") + " thinking_retry_plain=true");
				}
				return CompleteApiCallResult(exchange, result, messages, resolvedRoute, modelName, thinkingMode, result.ThinkingRetryPlain, source);
			}
			finally
			{
				exchange?.Dispose();
			}
		}
		catch (Exception ex)
		{
			result.ErrorMessage = ex.Message;
			Log(source, "[ERROR] NPC policy API exception: " + ex);
			return result;
		}
	}

	private static async Task<NpcPolicyHttpExchange> SendAndReadNpcPolicyExchangeAsync(string effectiveApiUrl, string apiKey, string jsonBody, int hardTimeoutMilliseconds, string source, long runtimeGeneration, string staleStagePrefix, NpcPolicyApiCallResult result)
	{
		HttpResponseMessage response = await SendNpcPolicyRequestWithHardTimeoutAsync(effectiveApiUrl, apiKey, jsonBody, hardTimeoutMilliseconds, source, result);
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

	private static JObject BuildChatRequestBody(string modelName, JArray messages, int maxTokens, float temperature)
	{
		return new JObject
		{
			["model"] = modelName,
			["messages"] = messages,
			["max_tokens"] = maxTokens,
			["stream"] = false,
			["temperature"] = temperature,
			["response_format"] = new JObject
			{
				["type"] = "json_object"
			}
		};
	}

	private static JObject BuildPlainRetryBody(JObject originalBody)
	{
		JObject retryBody = (JObject)originalBody.DeepClone();
		DuelSettings.RemoveThinkingControls(retryBody);
		return retryBody;
	}

	private static bool ShouldRetryWithoutThinkingControls(HttpResponseMessage response, string responseBody, string thinkingMode)
	{
		return response != null
			&& !response.IsSuccessStatusCode
			&& response.StatusCode == HttpStatusCode.BadRequest
			&& thinkingMode != "plain"
			&& LooksLikeNpcThinkingControlError(responseBody);
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

	private static void RecordPromptExchangeSafe(string source, int attempt, int attempts, string systemPrompt, string userPrompt, NpcPolicyApiCallResult result)
	{
		try
		{
			string responseForLog = result.Success ? (result.Content ?? "") : ("错误: " + (result.ErrorMessage ?? "未知错误"));
			Logger.LogEventPromptExchange((source ?? "NpcPolicy") + " [尝试 " + attempt.ToString(CultureInfo.InvariantCulture) + "/" + attempts.ToString(CultureInfo.InvariantCulture) + "]", BuildPromptPreview(systemPrompt, userPrompt), responseForLog);
		}
		catch
		{
		}
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

	private static async Task<HttpResponseMessage> SendNpcPolicyRequestWithHardTimeoutAsync(string effectiveApiUrl, string apiKey, string jsonBody, int hardTimeoutMilliseconds, string source, NpcPolicyApiCallResult result)
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
		LlmApiCompat.ApplyAuthenticationHeaders(request, effectiveApiUrl, apiKey);
		request.Content = new StringContent(jsonBody ?? "{}", Encoding.UTF8, "application/json");
		using CancellationTokenSource timeoutCts = new CancellationTokenSource();
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

	private static bool TryResolveEventAndRebellionApiConfig(DuelSettings settings, out string effectiveApiUrl, out string apiKey, out string modelName, out string resolvedRoute, out string errorMessage)
	{
		effectiveApiUrl = "";
		apiKey = "";
		modelName = "";
		resolvedRoute = "event_rebellion_fallback_main";
		errorMessage = "请检查 MCM 的事件/叛乱 API 或主 API 设置。";
		if (settings == null)
		{
			return false;
		}
		string eventUrl = (settings.EventAndRebellionApiUrl ?? "").Trim();
		string eventKey = (settings.EventAndRebellionApiKey ?? "").Trim();
		string eventModel = settings.GetEffectiveEventAndRebellionModelName();
		string eventSelected = settings.GetEventAndRebellionSelectedModelOption();
		bool hasEventField = !string.IsNullOrWhiteSpace(eventUrl)
			|| !string.IsNullOrWhiteSpace(eventKey)
			|| !string.IsNullOrWhiteSpace((settings.EventAndRebellionModelName ?? "").Trim())
			|| !string.IsNullOrWhiteSpace(eventSelected);
		if (hasEventField)
		{
			effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(eventUrl);
			apiKey = eventKey;
			modelName = eventModel;
			if (!string.IsNullOrWhiteSpace(effectiveApiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName))
			{
				resolvedRoute = "event_rebellion_dedicated";
				errorMessage = "";
				return true;
			}
		}
		effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl ?? "");
		apiKey = (settings.ApiKey ?? "").Trim();
		modelName = settings.GetEffectiveMainModelName();
		if (!string.IsNullOrWhiteSpace(effectiveApiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName))
		{
			resolvedRoute = hasEventField ? "event_rebellion_partial_fallback_main" : "event_rebellion_fallback_main";
			errorMessage = "";
			return true;
		}
		return false;
	}

	private static float ResolveTemperature(DuelSettings settings, string resolvedRoute)
	{
		try
		{
			return (resolvedRoute ?? "").StartsWith("event_rebellion_dedicated", StringComparison.OrdinalIgnoreCase)
				? settings.GetEventAndRebellionApiTemperature()
				: settings.GetMainApiTemperature();
		}
		catch
		{
			return 0.8f;
		}
	}

	private static JArray BuildMessageArray(string systemPrompt, string userPrompt)
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
				["content"] = userPrompt ?? ""
			}
		};
	}

	private static string BuildPromptPreview(string systemPrompt, string userPrompt)
	{
		return "System:\n" + (systemPrompt ?? "") + "\n\nUser:\n" + (userPrompt ?? "");
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
		try
		{
			Logger.Log(source ?? "NpcPolicyLlm", message ?? "");
		}
		catch
		{
		}
	}
}

internal static class NpcPolicyDetailedTraceLog
{
	private const int MaxFieldChars = 200000;

	private const string FileName = "NpcPolicy_DetailedTrace.txt";

	internal static void Write(string stage, string message)
	{
		Write(stage, message, null);
	}

	internal static void Write(string stage, string message, string detail)
	{
		try
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
			builder.Append(" [");
			builder.Append(string.IsNullOrWhiteSpace(stage) ? "log" : stage.Trim());
			builder.Append("] ");
			builder.AppendLine(message ?? "");
			if (!string.IsNullOrEmpty(detail))
			{
				builder.AppendLine("--- detail begin ---");
				builder.AppendLine(Clip(detail));
				builder.AppendLine("--- detail end ---");
			}
			Logger.LogToFile(FileName, builder.ToString());
		}
		catch
		{
		}
	}

	private static string Clip(string text)
	{
		if (text == null)
		{
			return "";
		}
		if (text.Length <= MaxFieldChars)
		{
			return text;
		}
		return text.Substring(0, MaxFieldChars)
			+ "\n...[truncated "
			+ (text.Length - MaxFieldChars).ToString(CultureInfo.InvariantCulture)
			+ " chars]";
	}
}

internal static class NpcPolicyStructuredParseLogger
{
	private const int SampleChars = 1200;

	internal static void LogFailure(string logSource, string kind, string batchId, string route, int attempts, string reason, string raw, string extracted)
	{
		string source = string.IsNullOrWhiteSpace(logSource) ? "NpcPolicyParse" : logSource.Trim();
		string message = kind + "-parse-failed"
			+ " batchId=" + CleanField(batchId)
			+ " route=" + CleanField(route)
			+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
			+ " reason=" + CleanField(reason)
			+ " raw_sample=" + OneLine(Clip(raw))
			+ " extracted_sample=" + OneLine(Clip(extracted));
		try
		{
			Logger.Log(source, message);
		}
		catch
		{
		}
		NpcPolicyDetailedTraceLog.Write(kind + "-parse-failed", message, "raw_sample:\n" + Clip(raw) + "\n\nextracted_sample:\n" + Clip(extracted));
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

public sealed class NpcRulerPolicyBehavior : CampaignBehaviorBase
{
	private const string SaveKeyPolicyRecords = "_afNpcRulerPolicyRecords_v1";
	private const string SaveKeyLastGeneratedDay = "_afNpcRulerPolicyLastGeneratedDay_v1";
	private const string SaveKeyLastGeneratedHour = "_afNpcRulerPolicyLastGeneratedHour_v1";
	private const float InitialGenerationCheckDelaySeconds = 8f;
	private const int DefaultGenerationIntervalDays = 7;
	private const int DefaultNpcRulerPolicyDailyLimit = 3;
	private const int DefaultNpcRulerPolicyBatchSize = 3;
	private const int MaxPoliciesPerBatch = 12;
	private const int MaxPolicyRecordCount = 180;
	private const int MaxNameChars = 90;
	private const int MaxContentChars = 900;
	private const int MaxFeedbackChars = 500;
	private const int MaxImpactChars = 300;
	private const int MaxReasonChars = 120;
	private const int SoftContextChars = 48000;
	private const int HardContextChars = 96000;
	private const int EditablePolicyPromptMaxChars = 4000;
	private const int PolicyMaxTokens = 8000;
	private const int FailedGenerationBackoffHours = 6;
	private const int PolicyApiHardTimeoutMilliseconds = 540000;
	private const double PolicyCommitFrameBudgetMs = 1.0;

	public static NpcRulerPolicyBehavior Instance { get; private set; }

	private readonly Dictionary<string, string> _policyRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentQueue<PendingNpcPolicyCommitContext> _pendingPolicyCommits = new ConcurrentQueue<PendingNpcPolicyCommitContext>();
	private readonly ConcurrentQueue<NpcPolicyGenerationJob> _pendingPolicySnapshotJobs = new ConcurrentQueue<NpcPolicyGenerationJob>();
	private readonly object _generationStateLock = new object();
	private readonly HashSet<string> _policyGenerationInFlightKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private bool _generationInProgress;
	private string _policyActiveInFlightKey = "";
	private int _lastGeneratedDay = -1;
	private int _lastGeneratedHour = -1;
	private int _lastGenerationAttemptHour = -1;
	private int _lastGenerationFailureHour = -1;
	private int _lastGenerationRetryCount;
	private string _lastGenerationError = "";
	private NpcPolicyRetryContext _lastPolicyRetryContext;
	private int _generationVersion;
	private bool _initialGenerationCheckPending;
	private float _initialGenerationCheckElapsed;

	public NpcRulerPolicyBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		Log("registered");
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (dataStore.IsSaving)
		{
			TrimPolicyRecords();
			Dictionary<string, string> records = CampaignSaveChunkHelper.FlattenStringDictionary(_policyRecords, SaveKeyPolicyRecords, "NpcRulerPolicyRecords");
			int lastGeneratedDay = _lastGeneratedDay;
			int lastGeneratedHour = _lastGeneratedHour;
			dataStore.SyncData(SaveKeyPolicyRecords, ref records);
			dataStore.SyncData(SaveKeyLastGeneratedDay, ref lastGeneratedDay);
			dataStore.SyncData(SaveKeyLastGeneratedHour, ref lastGeneratedHour);
			Log("save-write records=" + _policyRecords.Count.ToString(CultureInfo.InvariantCulture) + " lastGeneratedDay=" + lastGeneratedDay.ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + lastGeneratedHour.ToString(CultureInfo.InvariantCulture));
			return;
		}
		ClearPolicyTransientRuntimeForLoadedSave("sync-load", incrementVersion: true);
		_policyRecords.Clear();
		Dictionary<string, string> stored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPolicyRecords, ref stored);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(stored, "NpcRulerPolicyRecords"))
		{
			string key = (item.Key ?? "").Trim();
			string raw = (item.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(raw))
			{
				continue;
			}
			NpcRulerPolicyRecord record = DeserializeRecord(raw);
			if (record != null && !string.IsNullOrWhiteSpace(record.PolicyId))
			{
				_policyRecords[record.PolicyId] = JsonConvert.SerializeObject(record);
			}
		}
		int lastDay = -1;
		dataStore.SyncData(SaveKeyLastGeneratedDay, ref lastDay);
		_lastGeneratedDay = lastDay;
		int lastHour = -1;
		dataStore.SyncData(SaveKeyLastGeneratedHour, ref lastHour);
		_lastGeneratedHour = lastHour;
		if (_lastGeneratedHour < 0 && _lastGeneratedDay >= 0)
		{
			_lastGeneratedHour = _lastGeneratedDay * 24;
		}
		TrimPolicyRecords();
		Log("save-read records=" + _policyRecords.Count.ToString(CultureInfo.InvariantCulture) + " lastGeneratedDay=" + _lastGeneratedDay.ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + _lastGeneratedHour.ToString(CultureInfo.InvariantCulture));
	}

	public static List<NpcRulerPolicyRecord> GetRecentPolicyRecordsForExternal(string kingdomId = null, int maxCount = 20)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.GetRecentPolicyRecordsInternal(kingdomId, maxCount) ?? new List<NpcRulerPolicyRecord>();
		}
		catch
		{
			return new List<NpcRulerPolicyRecord>();
		}
	}

	public void OnEngineTick()
	{
		ProcessPendingPolicyCommits();
	}

	private void OnCampaignTick(float dt)
	{
		ProcessPendingPolicySnapshotJobs();
		ProcessInitialGenerationCheck(dt);
	}

	public static bool RegisterPlayerPolicyForExternal(NpcRulerPolicyRecord record)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.RegisterPlayerPolicyInternal(record) == true;
		}
		catch (Exception ex)
		{
			Log("player-policy-register-failed policy=" + (record?.PolicyId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	public static void UpdatePolicyEffectStateForExternal(string policyId, string effectId, string targetKingdomId, int remainingDays, bool isEnded)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.UpdatePolicyEffectStateInternal(policyId, effectId, targetKingdomId, remainingDays, isEnded);
		}
		catch (Exception ex)
		{
			Log("policy-effect-state-update-failed policy=" + (policyId ?? "") + " effect=" + (effectId ?? "") + " error=" + ex.Message);
		}
	}

	public static string BuildActivePolicyDialogueContextForExternal(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.BuildActivePolicyDialogueContextInternal(targetHero, targetCharacter, kingdomIdOverride) ?? "";
		}
		catch (Exception ex)
		{
			Log("dialogue-policy-context-failed error=" + ex.Message);
			return "";
		}
	}

	private bool RegisterPlayerPolicyInternal(NpcRulerPolicyRecord record)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.PolicyId) || string.IsNullOrWhiteSpace(record.KingdomId))
		{
			return false;
		}
		record.Version = 3;
		record.IsPlayerPolicy = true;
		record.CreatedUtcTicks = record.CreatedUtcTicks > 0L ? record.CreatedUtcTicks : DateTime.UtcNow.Ticks;
		_policyRecords[record.PolicyId] = JsonConvert.SerializeObject(record);
		TrimPolicyRecords();
		UpsertPolicyWorldEvent(record);
		AnimusForgeWorldEventInboxEntry feedbackEntry = NpcPublicFeedbackEventBehavior.BuildPolicyPublicFeedbackForExternal(record);
		NpcPublicFeedbackEventBehavior.CommitPolicyPublicFeedbackEventForExternal(feedbackEntry);
		MyBehavior.RecordUnifiedPolicyWeeklyMaterialForExternal(record);
		Log("player-policy-registered policy=" + record.PolicyId + " kingdom=" + record.KingdomId);
		return true;
	}

	private void UpdatePolicyEffectStateInternal(string policyId, string effectId, string targetKingdomId, int remainingDays, bool isEnded)
	{
		string id = (policyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyRecords.TryGetValue(id, out string raw))
		{
			return;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record?.Effects == null)
		{
			return;
		}
		string cleanEffectId = (effectId ?? "").Trim();
		string cleanTargetId = (targetKingdomId ?? "").Trim();
		NpcRulerPolicyEffectDto effect = record.Effects.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(cleanEffectId) && string.Equals((x.EffectId ?? "").Trim(), cleanEffectId, StringComparison.OrdinalIgnoreCase));
		if (effect == null)
		{
			effect = record.Effects.FirstOrDefault(x => x != null && string.Equals((x.TargetKingdomId ?? "").Trim(), cleanTargetId, StringComparison.OrdinalIgnoreCase));
		}
		if (effect == null)
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(cleanEffectId))
		{
			effect.EffectId = cleanEffectId;
		}
		effect.RemainingDays = Math.Max(0, remainingDays);
		effect.IsEnded = isEnded || effect.RemainingDays <= 0;
		_policyRecords[id] = JsonConvert.SerializeObject(record);
	}

	private void OnDailyTick()
	{
		TryStartPolicyGeneration("daily", logSkips: true);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		_initialGenerationCheckPending = true;
		_initialGenerationCheckElapsed = 0f;
		Log("session-launched pending-initial-check day=" + GetCurrentCampaignDay().ToString(CultureInfo.InvariantCulture) + " hour=" + GetCurrentCampaignHour().ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + _lastGeneratedHour.ToString(CultureInfo.InvariantCulture));
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ClearPolicyTransientRuntimeForLoadedSave("new_game_created", incrementVersion: true);
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		ClearPolicyTransientRuntimeForLoadedSave("game_loaded", incrementVersion: true);
	}

	private void ClearPolicyTransientRuntimeForLoadedSave(string reason, bool incrementVersion)
	{
		_lastGenerationAttemptHour = -1;
		_lastGenerationFailureHour = -1;
		_lastGenerationRetryCount = 0;
		_lastGenerationError = "";
		_lastPolicyRetryContext = null;
		if (incrementVersion)
		{
			_generationVersion++;
		}
		_initialGenerationCheckPending = false;
		_initialGenerationCheckElapsed = 0f;
		while (_pendingPolicyCommits.TryDequeue(out var _))
		{
		}
		while (_pendingPolicySnapshotJobs.TryDequeue(out var _))
		{
		}
		ResetPolicyGenerationLifecycleForRuntimeClear();
		Log("transient-cleared reason=" + (reason ?? ""));
	}

	private static string BuildPolicyGenerationInFlightKey(int currentDay, int currentHour, NpcRulerPolicyBatchContext context)
	{
		IEnumerable<string> ids = (context?.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
			.Select(x => x.KingdomId.Trim());
		if (!ids.Any())
		{
			ids = (context?.PendingTargets ?? new List<NpcRulerPolicySnapshotTarget>())
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
				.Select(x => x.KingdomId.Trim());
		}
		string kingdomKey = string.Join(",", ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return "npc_policy:" + Math.Max(0, currentHour).ToString(CultureInfo.InvariantCulture)
			+ ":" + NormalizeKeyPart(kingdomKey.Length == 0 ? currentDay.ToString(CultureInfo.InvariantCulture) : kingdomKey);
	}

	// Policy generation lifecycle invariant:
	// - _generationInProgress stays true from reservation until the main-thread pending commit reaches a terminal path.
	// - _policyGenerationInFlightKeys tracks only the API/scheduling slot; background finally may release that slot but must not complete the generation.
	// - _generationVersion is checked before terminal release, so an old commit cannot clear a newer job.
	private bool IsPolicyGenerationBusy(out string activeInFlightKey)
	{
		lock (_generationStateLock)
		{
			activeInFlightKey = _policyActiveInFlightKey ?? "";
			return _generationInProgress || _policyGenerationInFlightKeys.Count > 0;
		}
	}

	private static string NormalizePolicyGenerationInFlightKey(string inFlightKey)
	{
		string key = (inFlightKey ?? "").Trim();
		return string.IsNullOrWhiteSpace(key) ? "npc_policy:unknown" : key;
	}

	private bool TryReservePolicyGenerationLifecycle(string inFlightKey, out string activeInFlightKey)
	{
		string key = NormalizePolicyGenerationInFlightKey(inFlightKey);
		lock (_generationStateLock)
		{
			activeInFlightKey = _policyActiveInFlightKey ?? "";
			if (_generationInProgress || _policyGenerationInFlightKeys.Contains(key))
			{
				return false;
			}
			_policyGenerationInFlightKeys.Add(key);
			_policyActiveInFlightKey = key;
			_generationInProgress = true;
			activeInFlightKey = key;
			return true;
		}
	}

	private void ReleasePolicyGenerationLifecycle(string inFlightKey, bool completeGeneration)
	{
		string key = (inFlightKey ?? "").Trim();
		lock (_generationStateLock)
		{
			if (!string.IsNullOrWhiteSpace(key))
			{
				_policyGenerationInFlightKeys.Remove(key);
			}
			if (completeGeneration)
			{
				_generationInProgress = false;
			}
			if (!_generationInProgress && (string.IsNullOrWhiteSpace(key) || string.Equals(_policyActiveInFlightKey, key, StringComparison.OrdinalIgnoreCase) || _policyGenerationInFlightKeys.Count == 0))
			{
				_policyActiveInFlightKey = _policyGenerationInFlightKeys.FirstOrDefault() ?? "";
			}
		}
	}

	private void ResetPolicyGenerationLifecycleForRuntimeClear()
	{
		lock (_generationStateLock)
		{
			_generationInProgress = false;
			_policyGenerationInFlightKeys.Clear();
			_policyActiveInFlightKey = "";
		}
	}

	private void ProcessInitialGenerationCheck(float dt)
	{
		if (!_initialGenerationCheckPending)
		{
			return;
		}
		if (dt > 0f)
		{
			_initialGenerationCheckElapsed += dt;
		}
		if (_initialGenerationCheckElapsed < InitialGenerationCheckDelaySeconds)
		{
			return;
		}
		_initialGenerationCheckPending = false;
		TryStartPolicyGeneration("session", logSkips: true);
	}

	private void ProcessPendingPolicySnapshotJobs()
	{
		long startTimestamp = Stopwatch.GetTimestamp();
		double budgetMs = PolicyCommitFrameBudgetMs;
		while (!IsPolicyCommitBudgetExceeded(startTimestamp, budgetMs) && _pendingPolicySnapshotJobs.TryPeek(out NpcPolicyGenerationJob job))
		{
			if (job == null || !ProcessPendingPolicySnapshotJob(job, startTimestamp, budgetMs))
			{
				return;
			}
			_pendingPolicySnapshotJobs.TryDequeue(out var _);
		}
	}

	private bool ProcessPendingPolicySnapshotJob(NpcPolicyGenerationJob job, long startTimestamp, double budgetMs)
	{
		if (job == null)
		{
			return true;
		}
		if (job.Version != _generationVersion || SaveRuntimeGuard.IsStale(job.RuntimeGeneration, "npc_policy_snapshot"))
		{
			ReleasePolicyGenerationLifecycle(job.InFlightKey, completeGeneration: true);
			Log("generation-snapshot-discard batch=" + (job.BatchId ?? "") + " reason=stale");
			return true;
		}
		NpcRulerPolicyBatchContext context = job.Context;
		if (context == null)
		{
			FinalizePolicyGenerationFailure(new NpcPolicyGenerationResult { Job = job }, "missing snapshot context");
			return true;
		}
		while (context.SnapshotTargetIndex < context.PendingTargets.Count && !IsPolicyCommitBudgetExceeded(startTimestamp, budgetMs))
		{
			NpcRulerPolicySnapshotTarget target = context.PendingTargets[context.SnapshotTargetIndex++];
			Kingdom kingdom = ResolveNpcPolicyKingdomById(target?.KingdomId);
			NpcRulerPolicyKingdomContext kingdomContext = BuildKingdomContext(kingdom, target);
			if (kingdomContext != null)
			{
				context.Kingdoms.Add(kingdomContext);
			}
			break;
		}
		if (context.SnapshotTargetIndex < context.PendingTargets.Count)
		{
			return false;
		}
		if (context.Kingdoms.Count == 0)
		{
			FinalizePolicyGenerationFailure(new NpcPolicyGenerationResult { Job = job }, "snapshot produced no kingdom contexts");
			return true;
		}
		try
		{
			context.CompactWorldContext = BuildCompactWorldContext(context);
		}
		catch (Exception ex)
		{
			FinalizePolicyGenerationFailure(new NpcPolicyGenerationResult { Job = job }, "context build rejected: " + ex.Message);
			Log("generation-context-rejected batch=" + (job.BatchId ?? "") + " error=" + ex.Message);
			return true;
		}
		Log("generation-snapshot-complete batch=" + (job.BatchId ?? "") + " kingdoms=" + context.Kingdoms.Count.ToString(CultureInfo.InvariantCulture));
		try
		{
			_ = Task.Run(() => ProcessPolicyGenerationJobAsync(job));
		}
		catch (Exception ex)
		{
			FinalizePolicyGenerationFailure(new NpcPolicyGenerationResult { Job = job }, ex.ToString());
		}
		return true;
	}

	private void TryStartPolicyGeneration(string source, bool logSkips)
	{
		bool shouldLogSkips = logSkips || DuelSettings.IsNpcRulerPolicyDebugLogEnabledForExternal();
		if (IsPolicyGenerationBusy(out string activeInFlightKey))
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=in-progress key=" + activeInFlightKey);
			}
			return;
		}
		if (!IsCampaignSessionReady())
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=campaign-not-ready");
			}
			return;
		}
		if (!DuelSettings.IsNpcRulerPolicyEnabledForExternal())
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=disabled");
			}
			return;
		}
		if (!NpcPolicyLlmClient.IsConfiguredForNpcPolicy(out string apiConfigError))
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=api-not-configured error=" + apiConfigError);
			}
			return;
		}
		int currentDay = GetCurrentCampaignDay();
		int currentHour = GetCurrentCampaignHour();
		int intervalDays = Math.Max(1, DuelSettings.GetNpcRulerPolicyIntervalDaysForExternal());
		NormalizeGenerationClock(currentDay, currentHour);
		if (_lastGenerationFailureHour >= 0 && currentHour - _lastGenerationFailureHour < FailedGenerationBackoffHours)
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=failed-backoff currentHour=" + currentHour.ToString(CultureInfo.InvariantCulture) + " lastFailureHour=" + _lastGenerationFailureHour.ToString(CultureInfo.InvariantCulture) + " backoffHours=" + FailedGenerationBackoffHours.ToString(CultureInfo.InvariantCulture));
			}
			return;
		}
		if (_lastGeneratedHour < 0 && _lastGeneratedDay >= 0)
		{
			_lastGeneratedHour = _lastGeneratedDay * 24;
		}
		if (_lastGeneratedHour < 0 && _lastGenerationAttemptHour >= 0 && currentHour - _lastGenerationAttemptHour < 1)
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=recent-failed-attempt currentHour=" + currentHour.ToString(CultureInfo.InvariantCulture) + " lastAttemptHour=" + _lastGenerationAttemptHour.ToString(CultureInfo.InvariantCulture));
			}
			return;
		}
		NpcRulerPolicyBatchContext context = BuildBatchContext(currentDay, currentHour, intervalDays, includeHeavySnapshots: false);
		if (shouldLogSkips || context.PendingTargets.Count > 0)
		{
			Log("generation-selection source=" + (source ?? "") + " " + (context.SelectionDiagnostics ?? ""));
		}
		if (context.PendingTargets.Count == 0)
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=no-eligible-npc-kingdoms " + (context.SelectionDiagnostics ?? ""));
			}
			return;
		}
		string inFlightKey = BuildPolicyGenerationInFlightKey(currentDay, currentHour, context);
		if (!TryReservePolicyGenerationLifecycle(inFlightKey, out string duplicateInFlightKey))
		{
			if (shouldLogSkips)
			{
				Log("generation-skip source=" + (source ?? "") + " reason=duplicate-in-flight key=" + inFlightKey + " activeKey=" + duplicateInFlightKey);
			}
			return;
		}
		NpcPolicyGenerationJob job = null;
		try
		{
			job = new NpcPolicyGenerationJob
			{
				JobId = "npc_policy_job:" + context.BatchId,
				BatchId = context.BatchId,
				TriggerSource = (source ?? "").Trim(),
				Context = context,
				Day = currentDay,
				Hour = currentHour,
				InFlightKey = inFlightKey,
				Version = ++_generationVersion,
				RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
				MaxTokens = PolicyMaxTokens,
				HardTimeoutMilliseconds = PolicyApiHardTimeoutMilliseconds,
				CreatedUtcTicks = DateTime.UtcNow.Ticks
			};
			_lastGenerationAttemptHour = currentHour;
			_lastPolicyRetryContext = null;
			Log("generation-start source=" + (source ?? "") + " batch=" + context.BatchId + " kingdoms=" + context.PendingTargets.Count.ToString(CultureInfo.InvariantCulture) + " day=" + currentDay.ToString(CultureInfo.InvariantCulture) + " hour=" + currentHour.ToString(CultureInfo.InvariantCulture) + " intervalDays=" + intervalDays.ToString(CultureInfo.InvariantCulture));
			PolicyTraceLog("generation-job-selected", BuildPolicyJobTracePrefix(job), context.SelectionDiagnostics ?? "");
			_pendingPolicySnapshotJobs.Enqueue(job);
		}
		catch (Exception ex)
		{
			ReleasePolicyGenerationLifecycle(inFlightKey, completeGeneration: true);
			_lastGenerationAttemptHour = Math.Max(0, currentHour);
			_lastGenerationFailureHour = Math.Max(0, currentHour);
			_lastGenerationRetryCount = 0;
			_lastPolicyRetryContext = null;
			_lastGenerationError = Limit(ex.Message, 800);
			Log("generation-schedule-failed batch=" + (context?.BatchId ?? "") + " key=" + inFlightKey + " version=" + ((job?.Version ?? _generationVersion).ToString(CultureInfo.InvariantCulture)) + " error=" + ex);
		}
	}

	private async Task ProcessPolicyGenerationJobAsync(NpcPolicyGenerationJob job)
	{
		NpcPolicyGenerationResult result = new NpcPolicyGenerationResult
		{
			Job = job
		};
		try
		{
			if (job == null)
			{
				result.Error = "empty policy generation job";
			}
			else if (SaveRuntimeGuard.IsStale(job.RuntimeGeneration, "npc_policy_generation_start"))
			{
				result.Error = SaveRuntimeGuard.BuildStaleRequestErrorText();
			}
			else
			{
				if (string.IsNullOrWhiteSpace(job.SystemPrompt) || string.IsNullOrWhiteSpace(job.UserPrompt))
				{
					NpcPolicyPrompt prompt = BuildPolicyPrompt(job.Context);
					job.SystemPrompt = prompt.SystemPrompt;
					job.UserPrompt = prompt.UserPrompt;
					job.PromptPreview = prompt.Preview;
				}
				PolicyTraceLog("generation-job-built", BuildPolicyJobTracePrefix(job)
					+ " systemPromptChars=" + (job.SystemPrompt?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " userPromptChars=" + (job.UserPrompt?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
				PolicyTraceLog("generation-batch-call-start", BuildPolicyJobTracePrefix(job), job.PromptPreview);
				NpcPolicyApiCallResult apiResult = await NpcPolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(job.SystemPrompt, job.UserPrompt, job.MaxTokens, job.HardTimeoutMilliseconds, "NpcRulerPolicy", job.RuntimeGeneration, 3);
				CopyApiResultToPolicyResult(result, apiResult, accumulateAttempts: false);
				LogPolicyApiMetrics(job, apiResult);
				PolicyTraceLog("generation-batch-api-finished", BuildPolicyApiResultTracePrefix(job, apiResult), apiResult.Success ? apiResult.Content : apiResult.ErrorMessage);
				if (!apiResult.Success)
				{
					result.Error = apiResult.ErrorMessage ?? "API请求失败";
					result.FailureMessages.Add("批量请求失败：" + result.Error);
				}
				else
				{
					List<NpcRulerPolicyRecord> parsed = ParsePolicyRecords(apiResult.Content, job.BatchId, apiResult.ResolvedRoute, apiResult.AttemptsUsed, "policy-batch");
					result.ParsedCount = parsed.Count;
					result.Records = NormalizeGeneratedRecords(job.Context, parsed);
					if (result.Records.Count == 0)
					{
						result.Error = "policy parse/normalize produced no records; raw=" + Limit(apiResult.Content, 800);
						result.FailureMessages.Add(result.Error);
					}
					else
					{
						result.Success = true;
					}
					PolicyTraceLog("generation-batch-parse-finished", BuildPolicyResultTracePrefix(result), result.Success ? "parsed batch policy records" : result.Error);
				}
				List<NpcRulerPolicyKingdomContext> missingTargets = GetMissingPolicyTargets(job.Context, result.Records);
				if (missingTargets.Count > 0)
				{
					string missingIds = string.Join(",", missingTargets.Select(x => x?.KingdomId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
					result.FailureMessages.Add("Batch NPC policy generation missed " + missingTargets.Count.ToString(CultureInfo.InvariantCulture) + " kingdom target(s); no extra LLM fallback was started. missing=" + missingIds);
					PolicyTraceLog("generation-batch-missing-no-fallback", BuildPolicyResultTracePrefix(result), missingIds);
				}
				if ((result.Records ?? new List<NpcRulerPolicyRecord>()).Count > 0)
				{
					result.Success = true;
					result.Error = "";
				}
				else if (string.IsNullOrWhiteSpace(result.Error))
				{
					result.Error = "policy generation produced no committed records";
				}
			}
		}
		catch (Exception ex)
		{
			result.Error = ex.ToString();
			result.FailureMessages.Add(result.Error);
			PolicyTraceLog("generation-exception", BuildPolicyJobTracePrefix(job), ex.ToString());
		}
		finally
		{
			_pendingPolicyCommits.Enqueue(new PendingNpcPolicyCommitContext
			{
				GenerationResult = result
			});
			ReleasePolicyGenerationLifecycle(job?.InFlightKey, completeGeneration: false);
		}
	}

	private async Task TryAppendSingleKingdomPolicyFallbacksAsync(NpcPolicyGenerationJob job, NpcPolicyGenerationResult result, List<NpcRulerPolicyKingdomContext> missingTargets, string reason)
	{
		if (job == null || result == null || missingTargets == null || missingTargets.Count == 0)
		{
			return;
		}
		if (result.IsQuotaLimit || result.IsRateLimit || result.IsRequestsPerMinuteLimit)
		{
			result.FailureMessages.Add("跳过单王国 fallback：批量请求疑似限流或额度问题。reason=" + (reason ?? ""));
			return;
		}
		List<NpcRulerPolicyKingdomContext> targets = missingTargets
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
			.GroupBy(x => x.KingdomId.Trim(), StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.Take(MaxPoliciesPerBatch)
			.ToList();
		if (targets.Count == 0)
		{
			return;
		}
		PolicyTraceLog("generation-single-fallback-start", BuildPolicyJobTracePrefix(job) + " missing=" + targets.Count.ToString(CultureInfo.InvariantCulture) + " reason=" + (reason ?? ""), string.Join("\n", targets.Select(x => x.KingdomId + " " + x.KingdomName)));
		foreach (NpcRulerPolicyKingdomContext target in targets)
		{
			if (SaveRuntimeGuard.IsStale(job.RuntimeGeneration, "npc_policy_single_fallback"))
			{
				result.FailureMessages.Add("单王国 fallback 被丢弃：存档运行代际已变化。");
				return;
			}
			if (HasPolicyRecordForKingdom(result.Records, target.KingdomId))
			{
				continue;
			}
			NpcRulerPolicyBatchContext singleContext = BuildSingleKingdomFallbackContext(job.Context, target);
			NpcPolicyPrompt singlePrompt = BuildPolicyPrompt(singleContext);
			NpcPolicyApiCallResult apiResult = await NpcPolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(singlePrompt.SystemPrompt, singlePrompt.UserPrompt, job.MaxTokens, job.HardTimeoutMilliseconds, "NpcRulerPolicySingleFallback", job.RuntimeGeneration, 3);
			CopyApiResultToPolicyResult(result, apiResult, accumulateAttempts: true);
			PolicyTraceLog("generation-single-fallback-finished", BuildPolicyJobTracePrefix(job) + " target=" + (target.KingdomId ?? "") + " success=" + apiResult.Success.ToString(CultureInfo.InvariantCulture) + " attempts=" + apiResult.AttemptsUsed.ToString(CultureInfo.InvariantCulture), apiResult.Success ? apiResult.Content : apiResult.ErrorMessage);
			if (!apiResult.Success)
			{
				result.FallbackFailureCount++;
				result.FailureMessages.Add("单王国 fallback 失败：" + (target.KingdomName ?? target.KingdomId ?? "") + " - " + (apiResult.ErrorMessage ?? "未知错误"));
				if (apiResult.IsQuotaLimit || apiResult.IsRateLimit || apiResult.IsRequestsPerMinuteLimit)
				{
					result.FailureMessages.Add("单王国 fallback 因限流/额度问题提前停止，避免继续刷请求。");
					return;
				}
				continue;
			}
			List<NpcRulerPolicyRecord> parsed = ParsePolicyRecords(apiResult.Content, job.BatchId, apiResult.ResolvedRoute, apiResult.AttemptsUsed, "policy-single-fallback:" + (target.KingdomId ?? ""));
			result.ParsedCount += parsed.Count;
			List<NpcRulerPolicyRecord> normalized = NormalizeGeneratedRecords(singleContext, parsed);
			NpcRulerPolicyRecord record = normalized.FirstOrDefault(x => x != null && string.Equals((x.KingdomId ?? "").Trim(), (target.KingdomId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
			if (record == null)
			{
				result.FallbackFailureCount++;
				result.FailureMessages.Add("单王国 fallback 解析为空：" + (target.KingdomName ?? target.KingdomId ?? ""));
				continue;
			}
			result.Records = result.Records ?? new List<NpcRulerPolicyRecord>();
			if (!HasPolicyRecordForKingdom(result.Records, record.KingdomId))
			{
				result.Records.Add(record);
				result.FallbackSuccessCount++;
			}
		}
	}

	private void ProcessPendingPolicyCommits()
	{
		if (_pendingPolicyCommits.IsEmpty)
		{
			return;
		}
		long startTimestamp = Stopwatch.GetTimestamp();
		double budgetMs = PolicyCommitFrameBudgetMs;
		while (!IsPolicyCommitBudgetExceeded(startTimestamp, budgetMs) && _pendingPolicyCommits.TryPeek(out PendingNpcPolicyCommitContext context))
		{
			if (!ProcessPendingPolicyCommitContext(context, startTimestamp, budgetMs))
			{
				return;
			}
			_pendingPolicyCommits.TryDequeue(out var _);
		}
	}

	private bool ProcessPendingPolicyCommitContext(PendingNpcPolicyCommitContext context, long startTimestamp, double budgetMs)
	{
		if (context == null)
		{
			return true;
		}
		NpcPolicyGenerationResult result = context.GenerationResult;
		NpcPolicyGenerationJob job = result?.Job;
		try
		{
			if (job == null)
			{
				Log("generation-commit-discard reason=missing-job");
				return true;
			}
			if (job.Version != _generationVersion)
			{
				Log("generation-stale version=" + job.Version.ToString(CultureInfo.InvariantCulture) + " currentVersion=" + _generationVersion.ToString(CultureInfo.InvariantCulture) + " key=" + (job.InFlightKey ?? "") + " action=discard-without-release");
				return true;
			}
			if (SaveRuntimeGuard.IsStale(job.RuntimeGeneration, "npc_policy_generation_commit"))
			{
				ReleasePolicyGenerationLifecycle(job.InFlightKey, completeGeneration: true);
				Log("generation-discard reason=stale-runtime batch=" + (job.BatchId ?? ""));
				return true;
			}
			if (!IsCampaignSessionReady())
			{
				ReleasePolicyGenerationLifecycle(job.InFlightKey, completeGeneration: true);
				Log("generation-discard batch=" + (job.BatchId ?? "") + " reason=campaign-not-ready");
				return true;
			}
			if (!DuelSettings.IsNpcRulerPolicyEnabledForExternal())
			{
				ReleasePolicyGenerationLifecycle(job.InFlightKey, completeGeneration: true);
				Log("generation-discard batch=" + (job.BatchId ?? "") + " reason=disabled-before-complete");
				return true;
			}
			if (result == null || !result.Success)
			{
				FinalizePolicyGenerationFailure(result, result?.Error ?? "unknown policy generation error");
				return true;
			}
			List<NpcRulerPolicyRecord> records = result.Records ?? new List<NpcRulerPolicyRecord>();
			if (context.RecordIndex < records.Count)
			{
				NpcRulerPolicyRecord record = records[context.RecordIndex];
				if (record == null || string.IsNullOrWhiteSpace(record.PolicyId))
				{
					AdvancePendingPolicyRecord(context);
					return false;
				}
				ProcessPendingPolicyCommitStage(context, record);
				return false;
			}
			long finalizeTimestamp = Stopwatch.GetTimestamp();
			using (PerfProbe.Scope("PolicyCommit.Finalize"))
			{
				TrimPolicyRecords();
				_lastGeneratedDay = Math.Max(0, job.Day);
				_lastGeneratedHour = Math.Max(0, job.Hour);
				_lastGenerationFailureHour = -1;
				_lastGenerationRetryCount = 0;
				if (result.FailureMessages != null && result.FailureMessages.Count > 0)
				{
					_lastGenerationError = Limit(string.Join(" | ", result.FailureMessages), 800);
					_lastPolicyRetryContext = CreatePolicyRetryContext(job, result, _lastGenerationError);
					PolicyTraceLog("generation-partial-failures", BuildPolicyResultTracePrefix(result), string.Join("\n", result.FailureMessages));
				}
				else
				{
					_lastGenerationError = "";
					_lastPolicyRetryContext = null;
				}
				ReleasePolicyGenerationLifecycle(job.InFlightKey, completeGeneration: true);
				Log("generation-complete batch=" + (job.BatchId ?? "") + " parsed=" + result.ParsedCount.ToString(CultureInfo.InvariantCulture) + " saved=" + context.SavedCount.ToString(CultureInfo.InvariantCulture) + " inlineFeedback=" + context.PublicFeedbackSavedCount.ToString(CultureInfo.InvariantCulture) + " activeEffects=" + context.ActiveEffectsCreatedCount.ToString(CultureInfo.InvariantCulture) + " attempts=" + result.AttemptsUsed.ToString(CultureInfo.InvariantCulture) + " fallbackSuccess=" + result.FallbackSuccessCount.ToString(CultureInfo.InvariantCulture) + " fallbackFailures=" + result.FallbackFailureCount.ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + _lastGeneratedHour.ToString(CultureInfo.InvariantCulture));
				PolicyTraceLog("generation-commit-complete", BuildPolicyResultTracePrefix(result), BuildPolicyCommitTrace(records, context.SavedCount));
			}
			LogPolicyCommitStageIfOverBudget("PolicyCommit.Finalize", finalizeTimestamp, budgetMs);
			return true;
		}
		catch (Exception ex)
		{
			FinalizePolicyGenerationFailure(result ?? new NpcPolicyGenerationResult { Job = job }, ex.ToString());
			Log("generation-commit-exception " + ex);
			return true;
		}
	}

	private void ProcessPendingPolicyCommitStage(PendingNpcPolicyCommitContext context, NpcRulerPolicyRecord record)
	{
		if (context == null || record == null)
		{
			return;
		}
		string stageName;
		long stageTimestamp = Stopwatch.GetTimestamp();
		switch (context.Stage)
		{
			case PendingNpcPolicyCommitStage.SerializeRecord:
				stageName = "PolicyCommit.SerializeRecord";
				using (PerfProbe.Scope(stageName))
				{
					context.SerializedRecord = JsonConvert.SerializeObject(record);
					context.Stage = PendingNpcPolicyCommitStage.StoreRecord;
				}
				break;
			case PendingNpcPolicyCommitStage.StoreRecord:
				stageName = "PolicyCommit.StoreRecord";
				using (PerfProbe.Scope(stageName))
				{
					_policyRecords[record.PolicyId] = context.SerializedRecord ?? JsonConvert.SerializeObject(record);
					context.SavedCount++;
					context.Stage = PendingNpcPolicyCommitStage.UpsertPolicyEvent;
				}
				break;
			case PendingNpcPolicyCommitStage.UpsertPolicyEvent:
				stageName = "PolicyCommit.UpsertPolicyEvent";
				using (PerfProbe.Scope(stageName))
				{
					UpsertPolicyWorldEvent(record);
					context.Stage = PendingNpcPolicyCommitStage.CommitPublicFeedback;
				}
				break;
			case PendingNpcPolicyCommitStage.RecordPolicyWeeklyMaterial:
				stageName = "PolicyCommit.RecordPolicyWeeklyMaterial";
				using (PerfProbe.Scope(stageName))
				{
					InvokeNpcRulerPolicyWeeklyMaterialBridge(record);
					AdvancePendingPolicyRecord(context);
				}
				break;
			case PendingNpcPolicyCommitStage.CommitPublicFeedback:
				stageName = "PolicyCommit.CommitPublicFeedback";
				using (PerfProbe.Scope(stageName))
				{
					context.PublicFeedbackEntry = NpcPublicFeedbackEventBehavior.BuildPolicyPublicFeedbackForExternal(record, context.RecordIndex);
					if (context.PublicFeedbackEntry != null && NpcPublicFeedbackEventBehavior.CommitPolicyPublicFeedbackEventForExternal(context.PublicFeedbackEntry))
					{
						context.PublicFeedbackSavedCount++;
					}
					context.Stage = PendingNpcPolicyCommitStage.CreateActiveEffect;
				}
				break;
			default:
				stageName = "PolicyCommit.CreateActiveEffect";
				using (PerfProbe.Scope(stageName))
				{
					List<NpcRulerPolicyEffectDto> effects = record.Effects ?? new List<NpcRulerPolicyEffectDto>();
					if (context.ActiveEffectIndex < effects.Count)
					{
						context.ActiveEffectsCreatedCount += InvokeCustomPolicyActiveEffectBridge(record, context.ActiveEffectIndex++);
					}
					if (context.ActiveEffectIndex >= effects.Count)
					{
						context.Stage = PendingNpcPolicyCommitStage.RecordPolicyWeeklyMaterial;
					}
				}
				break;
		}
		LogPolicyCommitStageIfOverBudget(stageName, stageTimestamp, PolicyCommitFrameBudgetMs);
	}

	private static void AdvancePendingPolicyRecord(PendingNpcPolicyCommitContext context)
	{
		if (context == null)
		{
			return;
		}
		context.RecordIndex++;
		context.Stage = PendingNpcPolicyCommitStage.SerializeRecord;
		context.SerializedRecord = null;
		context.PublicFeedbackEntry = null;
		context.ActiveEffectIndex = 0;
	}

	private static void LogPolicyCommitStageIfOverBudget(string stageName, long startTimestamp, double budgetMs)
	{
		double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		if (budgetMs > 0.0 && elapsedMs >= budgetMs)
		{
			Logger.Log("NpcRulerPolicy", "commit-stage-over-budget stage=" + (stageName ?? "")
				+ " elapsedMs=" + elapsedMs.ToString("0.000", CultureInfo.InvariantCulture)
				+ " budgetMs=" + budgetMs.ToString("0.000", CultureInfo.InvariantCulture));
		}
	}

	private void FinalizePolicyGenerationFailure(NpcPolicyGenerationResult result, string error)
	{
		NpcPolicyGenerationJob job = result?.Job;
		if (job == null || job.Version != _generationVersion)
		{
			return;
		}
		ReleasePolicyGenerationLifecycle(job.InFlightKey, completeGeneration: true);
		_lastGenerationFailureHour = Math.Max(0, job.Hour);
		_lastGenerationRetryCount = Math.Max(0, result?.AttemptsUsed ?? 0);
		_lastGenerationError = Limit(error ?? "未知错误", 800);
		_lastPolicyRetryContext = CreatePolicyRetryContext(job, result, _lastGenerationError);
		Log("generation-failed batch=" + (job.BatchId ?? "") + " attempts=" + _lastGenerationRetryCount.ToString(CultureInfo.InvariantCulture) + " rateLimit=" + (result?.IsRateLimit ?? false).ToString(CultureInfo.InvariantCulture) + " rpm=" + (result?.IsRequestsPerMinuteLimit ?? false).ToString(CultureInfo.InvariantCulture) + " quota=" + (result?.IsQuotaLimit ?? false).ToString(CultureInfo.InvariantCulture) + " authFailure=" + (result?.IsAuthFailure ?? false).ToString(CultureInfo.InvariantCulture) + " retryAfter=" + ((result?.RetryAfterSeconds)?.ToString(CultureInfo.InvariantCulture) ?? "") + " rawRetryAfter=" + ((result?.RetryAfterSecondsRaw)?.ToString(CultureInfo.InvariantCulture) ?? "") + " retryAfterCapped=" + ((result?.RetryAfterSecondsCapped ?? false) ? "true" : "false") + " error=" + Limit(_lastGenerationError, 500));
		PolicyTraceLog("generation-failed", BuildPolicyResultTracePrefix(result), _lastGenerationError + "\n\n" + string.Join("\n", result?.FailureMessages ?? new List<string>()));
	}

	private static void CopyApiResultToPolicyResult(NpcPolicyGenerationResult result, NpcPolicyApiCallResult apiResult, bool accumulateAttempts)
	{
		if (result == null || apiResult == null)
		{
			return;
		}
		if (accumulateAttempts)
		{
			result.AttemptsUsed += Math.Max(0, apiResult.AttemptsUsed);
			result.FallbackAttemptsUsed += Math.Max(0, apiResult.AttemptsUsed);
		}
		else
		{
			result.AttemptsUsed = Math.Max(0, apiResult.AttemptsUsed);
			result.BatchAttemptsUsed = Math.Max(0, apiResult.AttemptsUsed);
		}
		result.RawResponse = apiResult.Content ?? result.RawResponse ?? "";
		result.IsRateLimit = result.IsRateLimit || apiResult.IsRateLimit;
		result.IsRequestsPerMinuteLimit = result.IsRequestsPerMinuteLimit || apiResult.IsRequestsPerMinuteLimit;
		result.IsQuotaLimit = result.IsQuotaLimit || apiResult.IsQuotaLimit;
		result.IsAuthFailure = result.IsAuthFailure || apiResult.IsAuthFailure;
		result.RetryAfterSeconds = MaxNullable(result.RetryAfterSeconds, apiResult.RetryAfterSeconds);
		result.RetryAfterSecondsRaw = MaxNullable(result.RetryAfterSecondsRaw, apiResult.RetryAfterSecondsRaw);
		result.RetryAfterSecondsCapped = result.RetryAfterSecondsCapped || apiResult.RetryAfterSecondsCapped;
	}

	private static int? MaxNullable(int? a, int? b)
	{
		if (!a.HasValue)
		{
			return b;
		}
		if (!b.HasValue)
		{
			return a;
		}
		return Math.Max(a.Value, b.Value);
	}

	private static List<NpcRulerPolicyKingdomContext> GetMissingPolicyTargets(NpcRulerPolicyBatchContext context, List<NpcRulerPolicyRecord> records)
	{
		HashSet<string> existing = new HashSet<string>((records ?? new List<NpcRulerPolicyRecord>())
			.Select(x => (x?.KingdomId ?? "").Trim())
			.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		return (context?.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId) && !existing.Contains(x.KingdomId.Trim()))
			.ToList();
	}

	private static bool HasPolicyRecordForKingdom(List<NpcRulerPolicyRecord> records, string kingdomId)
	{
		string id = (kingdomId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(id)
			&& (records ?? new List<NpcRulerPolicyRecord>()).Any(x => x != null && string.Equals((x.KingdomId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
	}

	private static NpcRulerPolicyBatchContext BuildSingleKingdomFallbackContext(NpcRulerPolicyBatchContext source, NpcRulerPolicyKingdomContext target)
	{
		return new NpcRulerPolicyBatchContext
		{
			BatchId = source?.BatchId ?? ("npc_policy_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)),
			Day = Math.Max(0, source?.Day ?? GetCurrentCampaignDay()),
			GameDate = FirstNonEmpty(source?.GameDate, FormatCurrentCampaignDate()),
			CompactWorldContext = source?.CompactWorldContext ?? "",
			Kingdoms = target == null ? new List<NpcRulerPolicyKingdomContext>() : new List<NpcRulerPolicyKingdomContext> { target }
		};
	}

	private static NpcPolicyRetryContext CreatePolicyRetryContext(NpcPolicyGenerationJob job, NpcPolicyGenerationResult result, string reason)
	{
		NpcPolicyRetryContext context = new NpcPolicyRetryContext
		{
			BatchId = job?.BatchId ?? "",
			TriggerSource = job?.TriggerSource ?? "",
			Day = Math.Max(0, job?.Day ?? 0),
			Hour = Math.Max(0, job?.Hour ?? 0),
			FailedReason = Limit(reason ?? "", 800),
			AttemptsUsed = Math.Max(0, result?.AttemptsUsed ?? 0),
			IsRateLimit = result?.IsRateLimit ?? false,
			IsRequestsPerMinuteLimit = result?.IsRequestsPerMinuteLimit ?? false,
			IsQuotaLimit = result?.IsQuotaLimit ?? false,
			IsAuthFailure = result?.IsAuthFailure ?? false,
			RetryAfterSeconds = result?.RetryAfterSeconds,
			FallbackSuccessCount = Math.Max(0, result?.FallbackSuccessCount ?? 0),
			FallbackFailureCount = Math.Max(0, result?.FallbackFailureCount ?? 0)
		};
		foreach (NpcRulerPolicyKingdomContext item in GetMissingPolicyTargets(job?.Context, result?.Records))
		{
			string id = (item?.KingdomId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(id) && !context.FailedKingdomIds.Contains(id, StringComparer.OrdinalIgnoreCase))
			{
				context.FailedKingdomIds.Add(id);
			}
		}
		foreach (string message in result?.FailureMessages ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(message))
			{
				context.FailureMessages.Add(Limit(message, 500));
			}
		}
		return context;
	}

	private static bool IsPolicyCommitBudgetExceeded(long startTimestamp, double budgetMs)
	{
		if (budgetMs <= 0.0)
		{
			return false;
		}
		double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		return elapsedMs >= budgetMs;
	}

	private static void PolicyTraceLog(string stage, string message, string detail = null)
	{
		NpcPolicyDetailedTraceLog.Write(stage, message, detail);
	}

	private static string BuildPolicyJobTracePrefix(NpcPolicyGenerationJob job)
	{
		if (job == null)
		{
			return "job=null";
		}
		return "job=" + (job.JobId ?? "")
			+ " batch=" + (job.BatchId ?? "")
			+ " source=" + (job.TriggerSource ?? "")
			+ " kingdoms=" + ((job.Context?.Kingdoms?.Count) ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " day=" + job.Day.ToString(CultureInfo.InvariantCulture)
			+ " hour=" + job.Hour.ToString(CultureInfo.InvariantCulture)
			+ " version=" + job.Version.ToString(CultureInfo.InvariantCulture);
	}

	private static string BuildPolicyResultTracePrefix(NpcPolicyGenerationResult result)
	{
		if (result == null)
		{
			return "result=null";
		}
		return BuildPolicyJobTracePrefix(result.Job)
			+ " success=" + result.Success.ToString(CultureInfo.InvariantCulture)
			+ " parsed=" + result.ParsedCount.ToString(CultureInfo.InvariantCulture)
			+ " records=" + ((result.Records?.Count) ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " attempts=" + result.AttemptsUsed.ToString(CultureInfo.InvariantCulture)
			+ " authFailure=" + result.IsAuthFailure.ToString(CultureInfo.InvariantCulture)
			+ " retryAfter=" + (result.RetryAfterSeconds?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " rawRetryAfter=" + (result.RetryAfterSecondsRaw?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " retryAfterCapped=" + (result.RetryAfterSecondsCapped ? "true" : "false")
			+ " fallbackSuccess=" + result.FallbackSuccessCount.ToString(CultureInfo.InvariantCulture)
			+ " fallbackFailures=" + result.FallbackFailureCount.ToString(CultureInfo.InvariantCulture);
	}

	private static string BuildPolicyApiResultTracePrefix(NpcPolicyGenerationJob job, NpcPolicyApiCallResult apiResult)
	{
		return BuildPolicyJobTracePrefix(job)
			+ " apiSuccess=" + ((apiResult?.Success ?? false) ? "true" : "false")
			+ " finish_reason=" + (apiResult?.FinishReason ?? "")
			+ " truncated=" + ((apiResult?.IsOutputTruncated ?? false) ? "true" : "false")
			+ " attempts=" + Math.Max(0, apiResult?.AttemptsUsed ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " prompt_tokens=" + FormatMetricInt(apiResult?.PromptTokens)
			+ " completion_tokens=" + FormatMetricInt(apiResult?.CompletionTokens)
			+ " total_tokens=" + FormatMetricInt(apiResult?.TotalTokens)
			+ " prompt_cache_hit_tokens=" + FormatMetricInt(apiResult?.PromptCacheHitTokens)
			+ " prompt_cache_miss_tokens=" + FormatMetricInt(apiResult?.PromptCacheMissTokens);
	}

	private static void LogPolicyApiMetrics(NpcPolicyGenerationJob job, NpcPolicyApiCallResult apiResult)
	{
		string message = BuildPolicyApiMetricsLine(job, apiResult);
		PolicyTraceLog("generation-batch-api-metrics", message);
		Log("generation-api-metrics " + message);
	}

	private static string BuildPolicyApiMetricsLine(NpcPolicyGenerationJob job, NpcPolicyApiCallResult apiResult)
	{
		return "source=NpcRulerPolicy"
			+ " batchId=" + (job?.BatchId ?? "")
			+ " batchSize=" + Math.Max(0, job?.Context?.BatchSize ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " kingdoms=" + Math.Max(0, job?.Context?.Kingdoms?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " maxTokens=" + Math.Max(0, job?.MaxTokens ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " finish_reason=" + (apiResult?.FinishReason ?? "")
			+ " prompt_tokens=" + FormatMetricInt(apiResult?.PromptTokens)
			+ " completion_tokens=" + FormatMetricInt(apiResult?.CompletionTokens)
			+ " total_tokens=" + FormatMetricInt(apiResult?.TotalTokens)
			+ " prompt_cache_hit_tokens=" + FormatMetricInt(apiResult?.PromptCacheHitTokens)
			+ " prompt_cache_miss_tokens=" + FormatMetricInt(apiResult?.PromptCacheMissTokens)
			+ " truncated=" + ((apiResult?.IsOutputTruncated ?? false) ? "true" : "false")
			+ " success=" + ((apiResult?.Success ?? false) ? "true" : "false")
			+ " attempts=" + Math.Max(0, apiResult?.AttemptsUsed ?? 0).ToString(CultureInfo.InvariantCulture);
	}

	private static string FormatMetricInt(int? value)
	{
		return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";
	}

	private static string BuildPolicyCommitTrace(List<NpcRulerPolicyRecord> records, int savedCount)
	{
		StringBuilder builder = new StringBuilder();
		builder.AppendLine("saved=" + savedCount.ToString(CultureInfo.InvariantCulture));
		foreach (NpcRulerPolicyRecord record in records ?? new List<NpcRulerPolicyRecord>())
		{
			if (record == null)
			{
				continue;
			}
			builder.Append("- ").Append(record.KingdomId).Append(" ").Append(record.KingdomName)
				.Append(" :: ").Append(record.PolicyName)
				.Append(" effects=").Append(((record.Effects?.Count) ?? 0).ToString(CultureInfo.InvariantCulture))
				.AppendLine();
		}
		return builder.ToString().TrimEnd();
	}

	private NpcRulerPolicyBatchContext BuildBatchContext(int currentDay, int currentHour, int intervalDays, bool includeHeavySnapshots)
	{
		NpcRulerPolicyBatchContext context = new NpcRulerPolicyBatchContext
		{
			BatchId = "npc_ruler_policy_" + currentDay.ToString(CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8),
			Day = currentDay,
			Hour = currentHour,
			GameDate = FormatCurrentCampaignDate(),
			DayLimit = ResolveNpcRulerPolicyDailyLimit(),
			BatchSize = ResolveNpcRulerPolicyBatchSize()
		};
		Dictionary<string, NpcRulerPolicyRecord> lastGeneratedByKingdom = BuildLastGeneratedPolicyByKingdom();
		List<Kingdom> npcKingdoms = GetNpcRuledKingdoms().ToList();
		context.TodayAlreadyGenerated = CountPoliciesGeneratedOnDay(currentDay);
		context.RemainingDailySlots = Math.Max(0, context.DayLimit - context.TodayAlreadyGenerated);
		List<NpcRulerPolicyGenerationCandidate> candidates = npcKingdoms
			.Select(kingdom => BuildGenerationCandidate(kingdom, lastGeneratedByKingdom, currentDay, intervalDays))
			.Where(x => x != null)
			.ToList();
		List<NpcRulerPolicyGenerationCandidate> eligible = candidates
			.Where(x => x.IsEligible)
			.OrderBy(x => x.LastGeneratedHour >= 0 ? 1 : 0)
			.ThenBy(x => x.LastGeneratedHour < 0 ? int.MinValue : x.LastGeneratedHour)
			.ThenBy(x => x.KingdomName, StringComparer.OrdinalIgnoreCase)
			.ToList();
		context.EligibleCount = eligible.Count;
		context.ExcludedCount = Math.Max(0, candidates.Count - eligible.Count);
		int takeCount = Math.Max(0, Math.Min(context.BatchSize, context.RemainingDailySlots));
		List<NpcRulerPolicyGenerationCandidate> selected = eligible.Take(takeCount).ToList();
		foreach (NpcRulerPolicyGenerationCandidate candidate in selected)
		{
			NpcRulerPolicySnapshotTarget target = new NpcRulerPolicySnapshotTarget
			{
				KingdomId = candidate?.KingdomId ?? "",
				KingdomName = candidate?.KingdomName ?? "",
				LastGeneratedText = candidate?.LastGeneratedText ?? "never"
			};
			context.PendingTargets.Add(target);
			if (includeHeavySnapshots)
			{
				NpcRulerPolicyKingdomContext kingdomContext = BuildKingdomContext(candidate.Kingdom, target);
				if (kingdomContext != null)
				{
					context.Kingdoms.Add(kingdomContext);
				}
			}
		}
		context.SelectionDiagnostics = BuildPolicySelectionDiagnostics(context, candidates, selected, npcKingdoms.Count, intervalDays);
		if (includeHeavySnapshots)
		{
			context.CompactWorldContext = BuildCompactWorldContext(context);
		}
		return context;
	}

	private static string BuildCompactWorldContext(NpcRulerPolicyBatchContext context)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Current date: " + context.GameDate + "; day=" + context.Day.ToString(CultureInfo.InvariantCulture) + "; hour=" + context.Hour.ToString(CultureInfo.InvariantCulture));
		sb.AppendLine(BuildCampaignCalendarContext());
		sb.AppendLine("Selection: eligible=" + context.EligibleCount.ToString(CultureInfo.InvariantCulture)
			+ " excluded=" + context.ExcludedCount.ToString(CultureInfo.InvariantCulture)
			+ " selected=" + context.Kingdoms.Count.ToString(CultureInfo.InvariantCulture)
			+ " dayLimit=" + context.DayLimit.ToString(CultureInfo.InvariantCulture)
			+ " batchSize=" + context.BatchSize.ToString(CultureInfo.InvariantCulture)
			+ " alreadyToday=" + context.TodayAlreadyGenerated.ToString(CultureInfo.InvariantCulture)
			+ " remainingDailySlots=" + context.RemainingDailySlots.ToString(CultureInfo.InvariantCulture));
		sb.AppendLine("Targets: generate exactly 1 ruler policy for each NPC kingdom listed below in TargetKingdomSnapshot; do not generate policies for unlisted kingdoms.");
		List<NpcRulerPolicyKingdomContext> targets = (context.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>()).Where(x => x != null).ToList();
		foreach (NpcRulerPolicyKingdomContext item in targets)
		{
			string targetBlock = BuildKingdomPromptContext(item, includeSupplemental: true);
			sb.AppendLine(targetBlock);
			Log("context-target kingdom=" + (item.KingdomId ?? "")
				+ " chars=" + targetBlock.Length.ToString(CultureInfo.InvariantCulture)
				+ " ownPolicies=" + (item.PreviousPolicyContexts?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " foreignGroups=" + (item.ForeignPolicyGroupContexts?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
		}
		if (sb.Length > SoftContextChars)
		{
			sb.Clear();
			sb.AppendLine("Current date: " + context.GameDate + "; day=" + context.Day.ToString(CultureInfo.InvariantCulture) + "; hour=" + context.Hour.ToString(CultureInfo.InvariantCulture));
			sb.AppendLine(BuildCampaignCalendarContext());
			sb.AppendLine("Targets: generate exactly 1 ruler policy for each TargetKingdomSnapshot below.");
			foreach (NpcRulerPolicyKingdomContext item in targets)
			{
				sb.AppendLine(BuildKingdomPromptContext(item, includeSupplemental: false));
			}
		}
		if (sb.Length > HardContextChars)
		{
			throw new InvalidOperationException("NPC policy mandatory context exceeds hard safety limit: chars=" + sb.Length.ToString(CultureInfo.InvariantCulture));
		}
		int ownPolicyCount = targets.Sum(x => x?.PreviousPolicyContexts?.Count ?? 0);
		int foreignGroupCount = targets.Sum(x => x?.ForeignPolicyGroupContexts?.Count ?? 0);
		string injectedPolicyIds = string.Join(",", Regex.Matches(sb.ToString(), @"policyId=([^,}\r\n]+)")
			.Cast<Match>().Select(x => x.Groups[1].Value.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
		Log("context-built targets=" + targets.Count.ToString(CultureInfo.InvariantCulture)
			+ " chars=" + sb.Length.ToString(CultureInfo.InvariantCulture)
			+ " estimatedTokens=" + Math.Ceiling(sb.Length * 0.6d).ToString(CultureInfo.InvariantCulture)
			+ " ownPolicies=" + ownPolicyCount.ToString(CultureInfo.InvariantCulture)
			+ " foreignGroups=" + foreignGroupCount.ToString(CultureInfo.InvariantCulture)
			+ " policyIds=" + injectedPolicyIds);
		return sb.ToString().TrimEnd();
	}

	private static Kingdom ResolveNpcPolicyKingdomById(string kingdomId)
	{
		string id = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return (Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private NpcRulerPolicyKingdomContext BuildKingdomContext(Kingdom kingdom, NpcRulerPolicySnapshotTarget generation)
	{
		if (kingdom == null)
		{
			return null;
		}
		Hero ruler = kingdom.Leader ?? kingdom.RulingClan?.Leader;
		string kingdomId = kingdom.StringId ?? "";
		string kingdomName = GetKingdomName(kingdom);
		List<Settlement> settlements = GetKingdomSettlements(kingdom);
		List<Settlement> towns = settlements.Where(x => x?.Town != null).ToList();
		List<Settlement> villages = settlements.Where(x => x?.Village != null).ToList();
		string prosperity = towns.Count == 0
			? "无城镇/城堡"
			: "均繁荣=" + FormatNumber(towns.Average(x => x.Town.Prosperity))
				+ " 均粮食=" + FormatNumber(towns.Average(x => x.Town.FoodStocks))
				+ " 均忠诚=" + FormatNumber(towns.Average(x => x.Town.Loyalty))
				+ " 均治安=" + FormatNumber(towns.Average(x => x.Town.Security))
				+ " 均民兵=" + FormatNumber(towns.Average(x => x.Militia));
		if (villages.Count > 0)
		{
			prosperity += " 均炉户=" + FormatNumber(villages.Average(x => x.Village.Hearth));
		}
		List<NpcRulerPolicyAllowedEffectTarget> allowedTargets = BuildAllowedEffectTargets(kingdom);
		string policies = SafeReadVanillaPolicies(kingdom);
		List<string> previousPolicies = BuildLinkedPreviousPolicyContexts(kingdomId);
		List<string> foreignPolicyGroups = BuildForeignPolicyGroupContexts(kingdom);
		string weeklyContext = InvokeWeeklyContextBridge(kingdomId);
		MyBehavior.GetNpcPersonaForExternal(ruler, out string personality, out string background);
		string requiredContext = "- TargetKingdomSnapshot"
			+ " | Issuer{id=" + kingdomId
			+ ",name=" + kingdomName
			+ ",culture=" + (kingdom.Culture?.Name?.ToString() ?? kingdom.Culture?.StringId ?? "未知")
			+ ",kingdomTitle=" + (kingdom.EncyclopediaTitle?.ToString() ?? "")
			+ ",rulerTitle=" + (kingdom.EncyclopediaRulerTitle?.ToString() ?? "")
			+ ",strength=" + FormatNumber(SafeKingdomStrength(kingdom))
			+ ",stability=" + SafeKingdomStability(kingdom).ToString(CultureInfo.InvariantCulture) + "/100"
			+ ",lastGenerated=" + (generation?.LastGeneratedText ?? "never") + "}"
			+ " | AllowedEffectTargets{" + BuildAllowedEffectTargetsPrompt(allowedTargets) + "}"
			+ " | Settlement{" + BuildSettlementSnapshot(towns, villages, prosperity) + "}";
		string personaContext = "RulerPersona{name=" + (ruler?.Name?.ToString() ?? "未知")
			+ ",heroId=" + (ruler?.StringId ?? "")
			+ ",clan=" + (ruler?.Clan?.Name?.ToString() ?? "")
			+ ",traits=" + BuildRulerTraitSummary(ruler)
			+ ",personality=" + Compact(personality)
			+ ",background=" + Compact(background) + "}";
		string supplementalContext = "Clan{" + BuildClanSnapshot(kingdom) + "}"
			+ " | WarAndDiplomacy{" + BuildDiplomacyNeighborSummary(kingdom) + "}"
			+ " | ActivePolicies{" + policies + "}"
			+ (string.IsNullOrWhiteSpace(weeklyContext) ? "" : " | WeeklyReports{" + Compact(weeklyContext) + "}");
		return new NpcRulerPolicyKingdomContext
		{
			KingdomId = kingdomId,
			KingdomName = kingdomName,
			RulerHeroId = ruler?.StringId ?? "",
			RulerName = ruler?.Name?.ToString() ?? "",
			RequiredContext = requiredContext,
			PersonaContext = personaContext,
			PreviousPolicyContexts = previousPolicies,
			ForeignPolicyGroupContexts = foreignPolicyGroups,
			SupplementalContext = supplementalContext,
			AllowedEffectTargets = allowedTargets
		};
	}

	private static string BuildKingdomPromptContext(NpcRulerPolicyKingdomContext context, bool includeSupplemental)
	{
		if (context == null)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(context.RequiredContext);
		if (!string.IsNullOrWhiteSpace(context.PersonaContext))
		{
			sb.AppendLine(context.PersonaContext);
		}
		foreach (string previous in context.PreviousPolicyContexts ?? new List<string>())
		{
			sb.AppendLine(previous);
		}
		foreach (string group in context.ForeignPolicyGroupContexts ?? new List<string>())
		{
			sb.AppendLine(group);
		}
		if (includeSupplemental && !string.IsNullOrWhiteSpace(context.SupplementalContext))
		{
			sb.AppendLine(context.SupplementalContext);
		}
		return sb.ToString().TrimEnd();
	}

	private List<string> BuildLinkedPreviousPolicyContexts(string kingdomId)
	{
		return GetRecentPolicyRecordsInternal(kingdomId, 2)
			.AsEnumerable()
			.Reverse()
			.Select(BuildLinkedPreviousPolicyContext)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList();
	}

	private static string BuildLinkedPreviousPolicyContext(NpcRulerPolicyRecord record)
	{
		if (record == null)
		{
			return "";
		}
		return "PreviousPolicy{policyId=" + Compact(record.PolicyId)
			+ ",date=" + Compact(record.GameDate)
			+ ",name=" + Compact(record.PolicyName)
			+ ",policyDigest=" + Compact(FirstNonEmpty(record.PolicyDigest, record.ImpactSummary))
			+ ",effects=" + Compact(BuildEffectSummary(record.Effects))
			+ ",linkedPublicFeedback=" + Compact(record.FeedbackDigest) + "}";
	}

	private List<string> BuildForeignPolicyGroupContexts(Kingdom targetKingdom)
	{
		if (targetKingdom == null)
		{
			return new List<string>();
		}
		string targetId = targetKingdom.StringId ?? "";
		List<NpcRulerPolicyRecord> all = _policyRecords.Values.Select(DeserializeRecord).Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId) && !string.Equals(x.KingdomId, targetId, StringComparison.OrdinalIgnoreCase)).ToList();
		List<string> result = new List<string>();
		foreach (IGrouping<string, NpcRulerPolicyRecord> source in all.GroupBy(x => x.KingdomId, StringComparer.OrdinalIgnoreCase).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
		{
			Kingdom sourceKingdom = ResolveNpcPolicyKingdomById(source.Key);
			bool atWar = sourceKingdom != null && !sourceKingdom.IsEliminated && targetKingdom.IsAtWarWith(sourceKingdom);
			List<NpcRulerPolicyRecord> incoming = source.Where(x => PolicyAffectsKingdom(x, targetId)).OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks).Take(2).ToList();
			if (!atWar && incoming.Count == 0)
			{
				continue;
			}
			List<NpcRulerPolicyRecord> selected = incoming
				.Concat(atWar ? source.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks) : Enumerable.Empty<NpcRulerPolicyRecord>())
				.GroupBy(x => x.PolicyId ?? "", StringComparer.OrdinalIgnoreCase)
				.Select(x => x.First())
				.Take(2)
				.ToList();
			StringBuilder group = new StringBuilder();
			group.Append("ForeignPolicyGroup{sourceKingdomId=").Append(source.Key)
				.Append(",sourceKingdomName=").Append(Compact(selected.FirstOrDefault()?.KingdomName))
				.Append(",relation=").Append(atWar ? "warEnemy" : "affectsSelf").AppendLine("}");
			foreach (NpcRulerPolicyRecord policy in selected)
			{
				group.Append(" Policy{policyId=").Append(Compact(policy.PolicyId))
					.Append(",date=").Append(Compact(policy.GameDate))
					.Append(",name=").Append(Compact(policy.PolicyName))
					.Append(",policyDigest=").Append(Compact(FirstNonEmpty(policy.PolicyDigest, policy.ImpactSummary)))
					.Append(",effectsOnThisKingdom=").Append(Compact(BuildEffectSummary(policy.Effects?.Where(x => string.Equals(x?.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase)).ToList())))
					.Append(",otherStrategicEffects=").Append(Compact(BuildEffectSummary(policy.Effects?.Where(x => !string.Equals(x?.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase)).ToList())))
					.Append(",feedbackDigest=").Append(Compact(policy.FeedbackDigest)).AppendLine("}");
			}
			result.Add(group.ToString().TrimEnd());
		}
		return result;
	}

	private static bool PolicyAffectsKingdom(NpcRulerPolicyRecord record, string kingdomId)
	{
		return record?.Effects?.Any(x => x != null && string.Equals((x.TargetKingdomId ?? "").Trim(), (kingdomId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) == true;
	}

	private string BuildActivePolicyDialogueContextInternal(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride)
	{
		string targetKingdomId = (kingdomIdOverride ?? "").Trim();
		if (string.IsNullOrWhiteSpace(targetKingdomId))
		{
			targetKingdomId = targetHero?.Clan?.Kingdom?.StringId
				?? targetHero?.MapFaction?.StringId
				?? targetCharacter?.HeroObject?.Clan?.Kingdom?.StringId
				?? targetCharacter?.HeroObject?.MapFaction?.StringId
				?? "";
		}
		if (string.IsNullOrWhiteSpace(targetKingdomId))
		{
			return "";
		}
		string playerKingdomId = Clan.PlayerClan?.Kingdom?.StringId ?? "";
		List<NpcRulerPolicyRecord> active = _policyRecords.Values.Select(DeserializeRecord)
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.PolicyId) && HasActivePolicyEffect(x))
			.ToList();
		List<NpcRulerPolicyRecord> own = SelectActiveDialoguePolicies(active, targetKingdomId, targetKingdomId, 3);
		List<NpcRulerPolicyRecord> player = string.IsNullOrWhiteSpace(playerKingdomId) || string.Equals(playerKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
			? new List<NpcRulerPolicyRecord>()
			: SelectActiveDialoguePolicies(active, playerKingdomId, targetKingdomId, 3);
		HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		StringBuilder sb = new StringBuilder();
		AppendActivePolicyDialogueGroup(sb, "本国仍在生效的政策", own, targetKingdomId, used);
		AppendActivePolicyDialogueGroup(sb, "玩家王国仍在生效的政策", player, targetKingdomId, used);
		if (sb.Length == 0)
		{
			return "";
		}
		string context = "【当前仍在生效的统治者政策】\n" + sb.ToString().TrimEnd();
		Log("dialogue-policy-context targetKingdom=" + targetKingdomId
			+ " own=" + own.Count.ToString(CultureInfo.InvariantCulture)
			+ " player=" + player.Count.ToString(CultureInfo.InvariantCulture)
			+ " chars=" + context.Length.ToString(CultureInfo.InvariantCulture));
		return context;
	}

	private static List<NpcRulerPolicyRecord> SelectActiveDialoguePolicies(List<NpcRulerPolicyRecord> records, string issuerKingdomId, string targetKingdomId, int maxCount)
	{
		return (records ?? new List<NpcRulerPolicyRecord>())
			.Where(x => x != null && string.Equals((x.KingdomId ?? "").Trim(), (issuerKingdomId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => HasActiveEffectOnKingdom(x, targetKingdomId))
			.ThenByDescending(GetMaximumRemainingDays)
			.ThenByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.GroupBy(x => x.PolicyId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.Take(Math.Max(1, maxCount))
			.ToList();
	}

	private static void AppendActivePolicyDialogueGroup(StringBuilder sb, string title, List<NpcRulerPolicyRecord> records, string targetKingdomId, HashSet<string> used)
	{
		List<NpcRulerPolicyRecord> selected = (records ?? new List<NpcRulerPolicyRecord>())
			.Where(x => x != null && used.Add(x.PolicyId ?? ""))
			.ToList();
		if (selected.Count == 0)
		{
			return;
		}
		sb.AppendLine(title + "：");
		foreach (NpcRulerPolicyRecord record in selected)
		{
			NpcRulerPolicyEffectDto effect = (record.Effects ?? new List<NpcRulerPolicyEffectDto>())
				.Where(IsActivePolicyEffect)
				.OrderByDescending(x => string.Equals((x.TargetKingdomId ?? "").Trim(), (targetKingdomId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(x => x.RemainingDays)
				.FirstOrDefault();
			sb.Append("- policyId=").Append(Compact(record.PolicyId))
				.Append("；发布国=").Append(Compact(record.KingdomName))
				.Append("；政策=").Append(Compact(record.PolicyName))
				.Append("；摘要=").Append(Limit(FirstNonEmpty(record.PolicyDigest, record.ImpactSummary), 140))
				.Append("；生效影响=").Append(BuildDialogueEffectSummary(effect))
				.Append("；反馈=").Append(Limit(record.FeedbackDigest, 70)).AppendLine();
		}
	}

	private static bool HasActivePolicyEffect(NpcRulerPolicyRecord record)
	{
		return record?.Effects?.Any(IsActivePolicyEffect) == true;
	}

	private static bool HasActiveEffectOnKingdom(NpcRulerPolicyRecord record, string kingdomId)
	{
		return record?.Effects?.Any(x => IsActivePolicyEffect(x) && string.Equals((x.TargetKingdomId ?? "").Trim(), (kingdomId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) == true;
	}

	private static bool IsActivePolicyEffect(NpcRulerPolicyEffectDto effect)
	{
		return effect != null && !effect.IsEnded && effect.RemainingDays > 0;
	}

	private static int GetMaximumRemainingDays(NpcRulerPolicyRecord record)
	{
		return record?.Effects?.Where(IsActivePolicyEffect).Select(x => x.RemainingDays).DefaultIfEmpty(0).Max() ?? 0;
	}

	private static string BuildDialogueEffectSummary(NpcRulerPolicyEffectDto effect)
	{
		if (effect == null)
		{
			return "无有效影响";
		}
		List<string> values = new List<string>();
		if (Math.Abs(effect.ProsperityDailyDeltaPerTown) > 0.0001f) values.Add("繁荣" + FormatSigned(effect.ProsperityDailyDeltaPerTown));
		if (Math.Abs(effect.FoodDailyDeltaPerTown) > 0.0001f) values.Add("粮食" + FormatSigned(effect.FoodDailyDeltaPerTown));
		if (Math.Abs(effect.HearthDailyDeltaPerVillage) > 0.0001f) values.Add("炉火" + FormatSigned(effect.HearthDailyDeltaPerVillage));
		if (Math.Abs(effect.LoyaltyDailyDeltaPerTown) > 0.0001f) values.Add("忠诚" + FormatSigned(effect.LoyaltyDailyDeltaPerTown));
		if (Math.Abs(effect.SecurityDailyDeltaPerTown) > 0.0001f) values.Add("治安" + FormatSigned(effect.SecurityDailyDeltaPerTown));
		if (Math.Abs(effect.MilitiaDailyDeltaPerTown) > 0.0001f) values.Add("民兵" + FormatSigned(effect.MilitiaDailyDeltaPerTown));
		if (Math.Abs(effect.KingdomStabilityDailyDelta) > 0.0001f) values.Add("稳定" + FormatSigned(effect.KingdomStabilityDailyDelta));
		return Compact(FirstNonEmpty(effect.TargetKingdomName, effect.TargetKingdomId, "目标王国"))
			+ "[" + string.Join("/", values) + ";剩余" + effect.RemainingDays.ToString(CultureInfo.InvariantCulture) + "天]";
	}

	private static List<NpcRulerPolicyAllowedEffectTarget> BuildAllowedEffectTargets(Kingdom issuer)
	{
		List<NpcRulerPolicyAllowedEffectTarget> result = new List<NpcRulerPolicyAllowedEffectTarget>();
		if (issuer == null)
		{
			return result;
		}
		result.Add(BuildAllowedEffectTarget(issuer, isIssuer: true));
		foreach (Kingdom other in Kingdom.All ?? Enumerable.Empty<Kingdom>())
		{
			if (other != null && other != issuer && !other.IsEliminated && issuer.IsAtWarWith(other))
			{
				result.Add(BuildAllowedEffectTarget(other, isIssuer: false));
			}
		}
		return result.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId)).ToList();
	}

	private static NpcRulerPolicyAllowedEffectTarget BuildAllowedEffectTarget(Kingdom kingdom, bool isIssuer)
	{
		if (kingdom == null)
		{
			return null;
		}
		return new NpcRulerPolicyAllowedEffectTarget
		{
			KingdomId = kingdom.StringId ?? "",
			KingdomName = GetKingdomName(kingdom),
			IsIssuer = isIssuer,
			Strength = SafeKingdomStrength(kingdom),
			MentionCandidates = BuildNpcPolicyKingdomMentionCandidates(kingdom)
		};
	}

	private static string BuildAllowedEffectTargetsPrompt(List<NpcRulerPolicyAllowedEffectTarget> targets)
	{
		return string.Join(";", (targets ?? new List<NpcRulerPolicyAllowedEffectTarget>()).Select(x => (x.IsIssuer ? "self" : "warEnemy")
			+ "(id=" + (x.KingdomId ?? "")
			+ ",name=" + (x.KingdomName ?? "")
			+ ",strength=" + FormatNumber(x.Strength) + ")"));
	}

	private static List<string> BuildNpcPolicyKingdomMentionCandidates(Kingdom kingdom)
	{
		return new[]
		{
			kingdom?.StringId,
			GetKingdomName(kingdom),
			kingdom?.Name?.ToString(),
			kingdom?.Leader?.StringId,
			kingdom?.Leader?.Name?.ToString(),
			kingdom?.RulingClan?.StringId,
			kingdom?.RulingClan?.Name?.ToString()
		}.Select(x => (x ?? "").Trim()).Where(x => x.Length >= 2).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string BuildCampaignCalendarContext()
	{
		int daysInSeason = 21;
		int daysInYear = 84;
		try
		{
			daysInSeason = Math.Max(1, CampaignTime.DaysInSeason);
			daysInYear = Math.Max(daysInSeason, CampaignTime.DaysInYear);
		}
		catch
		{
		}
		return "Calendar: daysInSeason=" + daysInSeason.ToString(CultureInfo.InvariantCulture)
			+ "; daysInYear=" + daysInYear.ToString(CultureInfo.InvariantCulture)
			+ "; choose durationDays relative to Bannerlord seasons and years.";
	}

	private Dictionary<string, NpcRulerPolicyRecord> BuildLastGeneratedPolicyByKingdom()
	{
		Dictionary<string, NpcRulerPolicyRecord> result = new Dictionary<string, NpcRulerPolicyRecord>(StringComparer.OrdinalIgnoreCase);
		foreach (NpcRulerPolicyRecord record in _policyRecords.Values.Select(DeserializeRecord).Where(x => x != null))
		{
			string kingdomId = (record.KingdomId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				continue;
			}
			if (!result.TryGetValue(kingdomId, out NpcRulerPolicyRecord existing)
				|| record.Day > existing.Day
				|| (record.Day == existing.Day && record.CreatedUtcTicks > existing.CreatedUtcTicks))
			{
				result[kingdomId] = record;
			}
		}
		return result;
	}

	private int CountPoliciesGeneratedOnDay(int currentDay)
	{
		return _policyRecords.Values
			.Select(DeserializeRecord)
			.Count(x => x != null && x.Day == currentDay);
	}

	private static NpcRulerPolicyGenerationCandidate BuildGenerationCandidate(Kingdom kingdom, Dictionary<string, NpcRulerPolicyRecord> lastGeneratedByKingdom, int currentDay, int intervalDays)
	{
		if (kingdom == null)
		{
			return null;
		}
		string kingdomId = kingdom.StringId ?? "";
		string kingdomName = GetKingdomName(kingdom);
		NpcRulerPolicyRecord lastRecord = null;
		if (!string.IsNullOrWhiteSpace(kingdomId))
		{
			lastGeneratedByKingdom?.TryGetValue(kingdomId, out lastRecord);
		}
		int lastDay = lastRecord == null ? -1 : Math.Max(0, lastRecord.Day);
		int safeIntervalDays = Math.Max(1, intervalDays);
		int daysSince = lastDay < 0 ? int.MaxValue : currentDay - lastDay;
		string exclusionReason = "";
		if (lastDay > currentDay)
		{
			exclusionReason = "future-last-generated";
		}
		else if (lastDay >= currentDay && lastDay >= 0)
		{
			exclusionReason = "already-generated-today";
		}
		else if (lastDay >= 0 && daysSince < safeIntervalDays)
		{
			exclusionReason = "not-due-remainingDays=" + Math.Max(0, safeIntervalDays - daysSince).ToString(CultureInfo.InvariantCulture);
		}
		string lastGeneratedText = lastRecord == null
			? "never"
			: "day=" + lastDay.ToString(CultureInfo.InvariantCulture) + ",policy=" + Limit(lastRecord.PolicyName ?? "", 42);
		return new NpcRulerPolicyGenerationCandidate
		{
			Kingdom = kingdom,
			KingdomId = kingdomId,
			KingdomName = kingdomName,
			LastGeneratedHour = lastDay < 0 ? -1 : lastDay * 24,
			LastGeneratedText = lastGeneratedText,
			ExclusionReason = exclusionReason,
			IsEligible = string.IsNullOrWhiteSpace(exclusionReason)
		};
	}

	private static string BuildPolicySelectionDiagnostics(NpcRulerPolicyBatchContext context, List<NpcRulerPolicyGenerationCandidate> candidates, List<NpcRulerPolicyGenerationCandidate> selected, int npcKingdomCount, int intervalDays)
	{
		List<NpcRulerPolicyGenerationCandidate> safeCandidates = candidates ?? new List<NpcRulerPolicyGenerationCandidate>();
		List<NpcRulerPolicyGenerationCandidate> safeSelected = selected ?? new List<NpcRulerPolicyGenerationCandidate>();
		string selectedIds = string.Join(",", safeSelected
			.Select(x => (x?.KingdomId ?? "").Trim())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Take(MaxPoliciesPerBatch));
		string lastGenerated = string.Join(";", safeCandidates
			.Take(12)
			.Select(x => (x?.KingdomName ?? "unknown") + "=" + (x?.LastGeneratedText ?? "never")));
		string excluded = string.Join(";", safeCandidates
			.Where(x => x != null && !x.IsEligible)
			.Take(12)
			.Select(x => (x.KingdomName ?? "unknown") + ":" + (x.ExclusionReason ?? "") + ":lastGenerated=" + (x.LastGeneratedText ?? "never")));
		return "day=" + Math.Max(0, context?.Day ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " hour=" + Math.Max(0, context?.Hour ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " npcKingdoms=" + Math.Max(0, npcKingdomCount).ToString(CultureInfo.InvariantCulture)
			+ " eligible=" + Math.Max(0, context?.EligibleCount ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " excluded=" + Math.Max(0, context?.ExcludedCount ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " selected=" + safeSelected.Count.ToString(CultureInfo.InvariantCulture)
			+ " dayLimit=" + Math.Max(0, context?.DayLimit ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " batchSize=" + Math.Max(0, context?.BatchSize ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " alreadyToday=" + Math.Max(0, context?.TodayAlreadyGenerated ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " remainingDailySlots=" + Math.Max(0, context?.RemainingDailySlots ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " intervalDays=" + Math.Max(1, intervalDays).ToString(CultureInfo.InvariantCulture)
			+ " selectedIds=" + selectedIds
			+ " lastGenerated=[" + Limit(lastGenerated, 650) + "]"
			+ " excludedSample=[" + Limit(excluded, 650) + "]";
	}

	private static string BuildClanSnapshot(Kingdom kingdom)
	{
		try
		{
			List<Clan> clans = (((IEnumerable<Clan>)kingdom?.Clans) ?? Enumerable.Empty<Clan>())
				.Where(x => x != null)
				.ToList();
			string ruling = kingdom?.RulingClan?.Name?.ToString() ?? "未知";
			string sample = string.Join("、", clans
				.Select(x => x.Name?.ToString())
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct()
				.Take(5));
			return "ruling=" + ruling
				+ ",clanCount=" + clans.Count.ToString(CultureInfo.InvariantCulture)
				+ (string.IsNullOrWhiteSpace(sample) ? "" : ",sample=" + sample);
		}
		catch
		{
			return "读取失败";
		}
	}

	private static string BuildSettlementSnapshot(List<Settlement> towns, List<Settlement> villages, string prosperitySummary)
	{
		try
		{
			List<Settlement> safeTowns = towns ?? new List<Settlement>();
			List<Settlement> safeVillages = villages ?? new List<Settlement>();
			string weakTowns = string.Join("、", safeTowns
				.Where(x => x?.Town != null)
				.OrderBy(x => x.Town.Loyalty)
				.Take(2)
				.Select(x => (x.Name?.ToString() ?? "未知") + "(忠诚" + FormatNumber(x.Town.Loyalty) + "/治安" + FormatNumber(x.Town.Security) + "/粮" + FormatNumber(x.Town.FoodStocks) + ")"));
			return "townOrCastleCount=" + safeTowns.Count.ToString(CultureInfo.InvariantCulture)
				+ ",villageCount=" + safeVillages.Count.ToString(CultureInfo.InvariantCulture)
				+ ",avg=" + (string.IsNullOrWhiteSpace(prosperitySummary) ? "未知" : prosperitySummary)
				+ (string.IsNullOrWhiteSpace(weakTowns) ? "" : ",lowLoyaltySample=" + weakTowns);
		}
		catch
		{
			return "读取失败";
		}
	}

	private static int ResolveNpcRulerPolicyDailyLimit()
	{
		return Clamp(ReadDuelSettingsInt("GetNpcRulerPolicyDailyGenerationLimitForExternal", DefaultNpcRulerPolicyDailyLimit), 1, MaxPoliciesPerBatch);
	}

	private static int ResolveNpcRulerPolicyBatchSize()
	{
		return Clamp(ReadDuelSettingsInt("GetNpcRulerPolicyMaxKingdomsPerRequestForExternal", DefaultNpcRulerPolicyBatchSize), 1, MaxPoliciesPerBatch);
	}

	private static int ReadDuelSettingsInt(string methodName, int fallback)
	{
		try
		{
			MethodInfo method = typeof(DuelSettings).GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(x => string.Equals(x.Name, methodName, StringComparison.Ordinal) && x.GetParameters().Length == 0);
			if (method != null)
			{
				object value = method.Invoke(null, null);
				if (value != null)
				{
					return Convert.ToInt32(value, CultureInfo.InvariantCulture);
				}
			}
		}
		catch
		{
		}
		return fallback;
	}

	private static string ResolveNpcRulerPolicyEditablePrompt()
	{
		try
		{
			MethodInfo method = typeof(DuelSettings).GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(x => string.Equals(x.Name, "GetNpcRulerPolicyPromptForExternal", StringComparison.Ordinal) && x.GetParameters().Length == 0);
			object value = method?.Invoke(null, null);
			return (value?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static NpcPolicyPrompt BuildPolicyPrompt(NpcRulerPolicyBatchContext context)
	{
		string editablePromptFull = ResolveNpcRulerPolicyEditablePrompt();
		string editablePrompt = Limit(editablePromptFull, EditablePolicyPromptMaxChars);
		string fixedSystemContract = "【不可覆盖的内置规则】你是 AnimusForge 的 NPC 统治者政策生成链路。只输出严格 JSON，不要 Markdown、解释、玩家操作、扣费、隐藏标签或原版 PolicyObject。必须严格依据动态快照中的事实。对每个目标先读取王国文化与百科头衔、统治结构、RulerPersona、Clan、战争外交与国家状态，再按时间读取最多两条 PreviousPolicy；同一块内的 linkedPolicyEvent 与该 policyId 的旧政策绑定。policyContent 必须由统治者本人直接宣布、命令、解释或辩护政策，禁止写成旁观者报道统治者做了什么的第三人称新闻摘要。直接发言的文体、自称、称谓及是否使用显式代词，只能依据动态快照中的真实文化、政治结构、统治者身份、实际头衔、性格、经历及家族封臣关系自行判断；不得套用预设称谓清单，不得在代码规则之外臆造统一自称，也允许符合背景且不使用显式代词的法令文体。新政策必须对既有路线和对应事件作出延续、调整、纠正或结束。战争时优先考虑军粮、征召、防御、治安、财政、商路、敌我消耗及本国动员代价；和平时才更自然地休养生产、恢复贸易与建设。只能使用 AllowedEffectTargets：默认影响本国，外国效果仅可指向正文明确点名的当前交战敌国。所有效果每日重复结算；必须结合政策规模、覆盖范围、执行阻力、持续时间和累计结果判断强弱，但不得因为持续多日就把所有字段机械压成 0.x。全国动员、制度改革、严重执行失败或其他足以改变国家现实的政策，可以产生显著乃至很大的正面、负面或混合效果。同一次输出必须按因果顺序完成政策意图、实际 effects、再写恰好一条解释实际结果如何形成的政策衍生事件。事件题材和社会过程完全由当前文化、人物、利益与局势自由决定，不得套用固定事件分类、清单或轮换模板；事件可以呈现成功、失败、部分成功或意外代价，但不产生第二套数值效果。policyContent、impactSummary、publicFeedback、effects 和 reason 必须描述同一实际结果；reason 必须说明当前事实如何促成政策、政策如何产生对应结果。玩家可编辑内容只能调整治理风格、关注重点和表达倾向，不能覆盖这些结构、目标与数据合法性规则。";
		string system = string.IsNullOrWhiteSpace(editablePrompt)
			? fixedSystemContract
			: editablePrompt + "\n\n" + fixedSystemContract;
		int targetCount = Math.Max(0, context?.Kingdoms?.Count ?? 0);
		StringBuilder user = new StringBuilder();
		user.AppendLine("【固定输出规则】");
		user.AppendLine("请输出 JSON 对象：{\"policies\":[...]}。policies 必须为下方每个 TargetKingdomSnapshot 各输出 1 条；目标数量=" + targetCount.ToString(CultureInfo.InvariantCulture) + "。不要输出未列出的王国。");
		user.AppendLine("每个 policy 字段：kingdomId、kingdomName、rulerHeroId、rulerName、policyName、policyContent、policyDigest、impactSummary、feedbackTitle、publicFeedback、feedbackDigest、effects。");
		user.AppendLine("每个 effects 项字段：targetKingdomId、targetKingdomName、prosperityDailyDeltaPerTown、foodDailyDeltaPerTown、hearthDailyDeltaPerVillage、loyaltyDailyDeltaPerTown、securityDailyDeltaPerTown、militiaDailyDeltaPerTown、kingdomStabilityDailyDelta、durationDays、reason。");
		user.AppendLine("JSON 字段边界只能使用 ASCII 双引号。字符串字段内部禁止写入实际换行或其他控制字符；需要表达换行时必须使用 \\n 等 JSON 转义，优先保持字符串为单行。");
		user.AppendLine("目标规则：每条政策默认作用于发布者本国；只有 AllowedEffectTargets 中标为 warEnemy、且政策名称或正文明确提及的当前交战敌国才可成为第二个目标。每条政策最多输出一个本国 effect 和一个敌国 effect；不得影响未列出的国家。");
		user.AppendLine("机制规则：durationDays 必须是正整数。不影响的 daily delta 填 0，至少保留一个与政策正文一致的非 0 daily delta。所有 daily delta 都会在每个游戏日重复结算，并分别应用到目标王国的每座对应城镇、城堡或村庄；稳定度是王国整体每日变化，不按定居点数量叠加。必须考虑每日变化与持续时间共同形成的累计结果，但不要自动把长期政策压成无关痛痒的小数。繁荣、粮食、炉火和民兵具有较大的数值空间，普通措施可以产生明确的个位数或更高变化，大规模改革、动员、灾难和系统性失败可按事实达到几十、几百或其他相称量级；忠诚、治安和稳定度属于 0-100 尺度，应保持相称但仍可在重大政策中显著变化。若稳定度需要变化，kingdomStabilityDailyDelta 必须输出有意义的整数，避免小数归零。不要套用统一档位或硬上限。");
		user.AppendLine("必须使用动态快照中的文化、王国与统治者实际头衔、统治结构、统治者资料、家族封臣关系、关联政策历史、对应政策事件、骑砍历法和当前国情作为事实依据。policyContent 是统治者本人直接发言的政策正文，不是史官或记者摘要；系统不规定任何代词、自称或头衔，必须从动态上下文判断，也可以采用不出现显式代词的直接法令文体。");
		user.AppendLine("policyDigest 用一到两句完整短句压缩政策目的、措施、目标与代价，建议 80-140 个中文字符；feedbackDigest 用一句完整短句压缩主要支持、反对、担忧或社会反应，建议 40-70 个中文字符。新政策若回应旧政策或外国政策，必须在构思中依据对应 policyId 建立延续、封锁、动员、反制、报复、适应或缓和关系，不得混淆来源国。");
		user.AppendLine("impactSummary 是可给周报/事件摘要用的一句话。feedbackTitle 与 publicFeedback 实际保存恰好一条政策衍生事件：标题应具体，正文约 100-180 个中文字符并以完整句子自然收尾，绝不能用省略号代替未写完的内容；必须把 effects.reason 所表达的完整执行原因转化为自然事件叙事，说明当前世界中最合理的人物、势力、利益冲突、意外或执行过程如何使政策产生实际效果。不要选择或声明固定事件类型，不要套用预设题材，不要列数值，不得再写第二条事件。effects.reason 仅作为内部因果数据，事件不追加独立效果。policyContent、impactSummary、publicFeedback、reason 与数值方向和持续时间必须一致；不要为了省 token 删除字段、改短字段名或压成差异/极简 DTO。代码不会再发起第二次事件 LLM 请求。");
		user.AppendLine();
		user.AppendLine("【目标王国动态快照】");
		user.AppendLine(context?.CompactWorldContext ?? "");
		if ((editablePromptFull?.Length ?? 0) > EditablePolicyPromptMaxChars)
		{
			Log("editable-prompt-injection-truncated savedChars=" + editablePromptFull.Length.ToString(CultureInfo.InvariantCulture)
				+ " injectedChars=" + EditablePolicyPromptMaxChars.ToString(CultureInfo.InvariantCulture));
		}
		return new NpcPolicyPrompt
		{
			SystemPrompt = system,
			UserPrompt = user.ToString()
		};
	}

	private List<NpcRulerPolicyRecord> NormalizeGeneratedRecords(NpcRulerPolicyBatchContext context, List<NpcRulerPolicyRecord> records)
	{
		List<NpcRulerPolicyRecord> result = new List<NpcRulerPolicyRecord>();
		HashSet<string> usedKingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, NpcRulerPolicyKingdomContext> byId = (context?.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
			.GroupBy(x => x.KingdomId.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		foreach (NpcRulerPolicyRecord raw in records ?? new List<NpcRulerPolicyRecord>())
		{
			NpcRulerPolicyKingdomContext target = ResolveGeneratedPolicyTarget(context, raw, byId);
			if (target == null || usedKingdomIds.Contains(target.KingdomId))
			{
				continue;
			}
			List<NpcRulerPolicyEffectDto> effects = NormalizeEffects(raw?.Effects, target, raw?.PolicyName, raw?.PolicyContent);
			string policyId = FirstNonEmpty(raw?.PolicyId, "npc_ruler_policy:" + (context?.BatchId ?? "") + ":" + target.KingdomId);
			string fallbackEvent = "法令传至各地后，地方执行者与受影响群体开始按各自利益回应。" + FirstNonEmpty(raw?.ImpactSummary, BuildEffectSummary(effects), "政策的真实成效仍取决于各地能否落实具体措施。") + "这场余波成为判断该政策成败的直接见证。";
			NpcRulerPolicyRecord record = new NpcRulerPolicyRecord
			{
				Version = 3,
				PolicyId = Limit(policyId, 160),
				BatchId = context?.BatchId ?? "",
				KingdomId = target.KingdomId,
				KingdomName = target.KingdomName,
				RulerHeroId = target.RulerHeroId,
				RulerName = target.RulerName,
				PolicyName = Limit(FirstNonEmpty(raw?.PolicyName, target.KingdomName + "政令"), MaxNameChars),
				PolicyContent = FirstNonEmpty(raw?.PolicyContent, raw?.ImpactSummary, "即日起施行新的王国政令，各地须依照当前国情逐步落实。"),
				PolicyDigest = Compact(FirstNonEmpty(raw?.PolicyDigest, raw?.ImpactSummary)),
				PublicFeedback = Limit(FirstNonEmpty(raw?.PublicFeedback, fallbackEvent), 0),
				FeedbackTitle = Limit(FirstNonEmpty(raw?.FeedbackTitle, "《" + FirstNonEmpty(raw?.PolicyName, target.KingdomName + "政令") + "》的余波"), MaxNameChars),
				FeedbackDigest = Compact(FirstNonEmpty(raw?.FeedbackDigest, raw?.ImpactSummary)),
				EventType = "",
				ImpactSummary = Limit(FirstNonEmpty(raw?.ImpactSummary, BuildEffectSummary(effects)), MaxImpactChars),
				Day = Math.Max(0, context?.Day ?? GetCurrentCampaignDay()),
				GameDate = FirstNonEmpty(context?.GameDate, FormatCurrentCampaignDate()),
				CreatedUtcTicks = DateTime.UtcNow.Ticks,
				Effects = effects
			};
			foreach (NpcRulerPolicyEffectDto effect in record.Effects ?? new List<NpcRulerPolicyEffectDto>())
			{
				effect.RemainingDays = Math.Max(0, effect.DurationDays);
				effect.IsEnded = effect.RemainingDays <= 0;
			}
			result.Add(record);
			usedKingdomIds.Add(target.KingdomId);
			if (result.Count >= MaxPoliciesPerBatch)
			{
				break;
			}
		}
		return result;
	}

	private static NpcRulerPolicyKingdomContext ResolveGeneratedPolicyTarget(NpcRulerPolicyBatchContext context, NpcRulerPolicyRecord record, Dictionary<string, NpcRulerPolicyKingdomContext> byId)
	{
		string id = (record?.KingdomId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(id) && byId != null && byId.TryGetValue(id, out NpcRulerPolicyKingdomContext exact))
		{
			return exact;
		}
		string name = (record?.KingdomName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(name))
		{
			NpcRulerPolicyKingdomContext byName = (context?.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>()).FirstOrDefault(x => x != null && string.Equals(x.KingdomName, name, StringComparison.OrdinalIgnoreCase));
			if (byName != null)
			{
				return byName;
			}
		}
		string rulerId = (record?.RulerHeroId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(rulerId))
		{
			NpcRulerPolicyKingdomContext byRuler = (context?.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>()).FirstOrDefault(x => x != null && string.Equals(x.RulerHeroId, rulerId, StringComparison.OrdinalIgnoreCase));
			if (byRuler != null)
			{
				return byRuler;
			}
		}
		return null;
	}

	private static List<NpcRulerPolicyEffectDto> NormalizeEffects(List<NpcRulerPolicyEffectDto> effects, NpcRulerPolicyKingdomContext target, string policyName, string policyContent)
	{
		List<NpcRulerPolicyEffectDto> result = new List<NpcRulerPolicyEffectDto>();
		HashSet<string> usedTargetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool issuerEffectAdded = false;
		bool foreignEffectAdded = false;
		string policyText = Compact((policyName ?? "") + " " + (policyContent ?? ""));
		foreach (NpcRulerPolicyEffectDto effect in effects ?? new List<NpcRulerPolicyEffectDto>())
		{
			if (effect == null)
			{
				continue;
			}
			if (effect.DurationDays <= 0 || !TryValidateNpcPolicyEffectNumbers(effect, out int stability))
			{
				Log("effect-normalize-rejected issuer=" + (target?.KingdomId ?? "") + " reason=invalid-numeric-or-duration");
				continue;
			}
			NpcRulerPolicyAllowedEffectTarget allowedTarget = ResolveAllowedEffectTarget(effect, target);
			if (allowedTarget == null || string.IsNullOrWhiteSpace(allowedTarget.KingdomId))
			{
				Log("effect-normalize-rejected issuer=" + (target?.KingdomId ?? "") + " requestedTarget=" + (effect.TargetKingdomId ?? effect.TargetKingdomName ?? "") + " reason=target-not-allowed");
				continue;
			}
			if (!allowedTarget.IsIssuer && !PolicyTextMentionsAllowedTarget(policyText, allowedTarget))
			{
				Log("effect-normalize-rejected issuer=" + (target?.KingdomId ?? "") + " requestedTarget=" + allowedTarget.KingdomId + " reason=foreign-target-not-mentioned");
				continue;
			}
			if (usedTargetIds.Contains(allowedTarget.KingdomId)
				|| (allowedTarget.IsIssuer && issuerEffectAdded)
				|| (!allowedTarget.IsIssuer && foreignEffectAdded))
			{
				Log("effect-normalize-rejected issuer=" + (target?.KingdomId ?? "") + " requestedTarget=" + allowedTarget.KingdomId + " reason=duplicate-or-extra-target");
				continue;
			}
			NpcRulerPolicyEffectDto normalized = new NpcRulerPolicyEffectDto
			{
				TargetKingdomId = allowedTarget.KingdomId,
				TargetKingdomName = allowedTarget.KingdomName,
				ProsperityDailyDeltaPerTown = effect.ProsperityDailyDeltaPerTown,
				FoodDailyDeltaPerTown = effect.FoodDailyDeltaPerTown,
				HearthDailyDeltaPerVillage = effect.HearthDailyDeltaPerVillage,
				LoyaltyDailyDeltaPerTown = effect.LoyaltyDailyDeltaPerTown,
				SecurityDailyDeltaPerTown = effect.SecurityDailyDeltaPerTown,
				MilitiaDailyDeltaPerTown = effect.MilitiaDailyDeltaPerTown,
				KingdomStabilityDailyDelta = stability,
				DurationDays = effect.DurationDays,
				Reason = Limit(effect.Reason ?? "", MaxReasonChars)
			};
			if (HasAnyDailyDelta(normalized))
			{
				result.Add(normalized);
				usedTargetIds.Add(allowedTarget.KingdomId);
				issuerEffectAdded |= allowedTarget.IsIssuer;
				foreignEffectAdded |= !allowedTarget.IsIssuer;
			}
		}
		return result;
	}

	private static NpcRulerPolicyAllowedEffectTarget ResolveAllowedEffectTarget(NpcRulerPolicyEffectDto effect, NpcRulerPolicyKingdomContext issuer)
	{
		List<NpcRulerPolicyAllowedEffectTarget> allowed = issuer?.AllowedEffectTargets ?? new List<NpcRulerPolicyAllowedEffectTarget>();
		string id = (effect?.TargetKingdomId ?? "").Trim();
		string name = (effect?.TargetKingdomName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
		{
			return allowed.FirstOrDefault(x => x != null && x.IsIssuer);
		}
		if (!string.IsNullOrWhiteSpace(id))
		{
			return allowed.FirstOrDefault(x => x != null && string.Equals((x.KingdomId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		return allowed.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(name) && string.Equals((x.KingdomName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));
	}

	private static bool PolicyTextMentionsAllowedTarget(string policyText, NpcRulerPolicyAllowedEffectTarget target)
	{
		if (target == null || string.IsNullOrWhiteSpace(policyText))
		{
			return false;
		}
		return (target.MentionCandidates ?? new List<string>())
			.Any(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length >= 2 && policyText.IndexOf(x.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static bool TryValidateNpcPolicyEffectNumbers(NpcRulerPolicyEffectDto effect, out int stability)
	{
		stability = 0;
		if (effect == null
			|| !IsFinite(effect.ProsperityDailyDeltaPerTown)
			|| !IsFinite(effect.FoodDailyDeltaPerTown)
			|| !IsFinite(effect.HearthDailyDeltaPerVillage)
			|| !IsFinite(effect.LoyaltyDailyDeltaPerTown)
			|| !IsFinite(effect.SecurityDailyDeltaPerTown)
			|| !IsFinite(effect.MilitiaDailyDeltaPerTown))
		{
			return false;
		}
		return TryConvertNpcPolicyStability(effect.KingdomStabilityDailyDelta, out stability);
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static bool TryConvertNpcPolicyStability(float value, out int result)
	{
		result = 0;
		if (!IsFinite(value))
		{
			return false;
		}
		double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
		if (rounded < int.MinValue || rounded > int.MaxValue)
		{
			return false;
		}
		result = (int)rounded;
		return true;
	}

	private void UpsertPolicyWorldEvent(NpcRulerPolicyRecord record)
	{
		try
		{
			string title = FirstNonEmpty(record.PolicyName, "新政策");
			string detail = "统治者：" + (record.RulerName ?? "未知") + "\n政策：" + (record.PolicyContent ?? "") + "\n影响：" + (record.ImpactSummary ?? "");
			AnimusForgeWorldEventInboxEntry entry = new AnimusForgeWorldEventInboxEntry
			{
				EventId = "npc_ruler_policy:" + NormalizeKeyPart(record.PolicyId),
				EventKind = "npc_ruler_policy",
				Title = Limit(title, 90),
				Summary = Limit(FirstNonEmpty(record.ImpactSummary, record.PolicyContent), 260),
				DetailText = Limit(detail, 1200),
				KingdomId = record.KingdomId ?? "",
				KingdomName = record.KingdomName ?? "",
				ActorHeroId = record.RulerHeroId ?? "",
				ActorHeroName = record.RulerName ?? "",
				PolicyId = record.PolicyId ?? "",
				PolicyName = record.PolicyName ?? "",
				Day = Math.Max(0, record.Day),
				GameDate = record.GameDate ?? "",
				CreatedUtcTicks = record.CreatedUtcTicks > 0L ? record.CreatedUtcTicks : DateTime.UtcNow.Ticks,
				StableKey = "npc_ruler_policy:" + (record.PolicyId ?? ""),
				IsRead = false
			};
			NpcPublicFeedbackEventBehavior.UpsertWorldEventForExternal(entry, markUnread: true);
		}
		catch (Exception ex)
		{
			Log("world-event-upsert-failed policy=" + (record?.PolicyId ?? "") + " error=" + ex.Message);
		}
	}

	private static int InvokeCustomPolicyActiveEffectBridge(NpcRulerPolicyRecord record, int effectIndex)
	{
		try
		{
			if (record == null)
			{
				return 0;
			}
			List<NpcRulerPolicyEffectDto> effects = record.Effects ?? new List<NpcRulerPolicyEffectDto>();
			if (effectIndex < 0 || effectIndex >= effects.Count)
			{
				return 0;
			}
			return CustomPolicyBehavior.TryCreateNpcRulerPolicyActiveEffectForExternal(record, effects[effectIndex], out var _, out var _) ? 1 : 0;
		}
		catch (Exception ex)
		{
			Log("custom-policy-bridge-failed " + ex.Message);
		}
		return 0;
	}

	private static void InvokeNpcRulerPolicyWeeklyMaterialBridge(NpcRulerPolicyRecord record)
	{
		try
		{
			if (record == null)
			{
				return;
			}
			MyBehavior.RecordUnifiedPolicyWeeklyMaterialForExternal(record);
		}
		catch (Exception ex)
		{
			Log("weekly-material-bridge-failed policy=" + (record?.PolicyId ?? "") + " error=" + ex.Message);
		}
	}

	private static string InvokeWeeklyContextBridge(string kingdomId)
	{
		try
		{
			MethodInfo method = typeof(MyBehavior).GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(x => string.Equals(x.Name, "BuildRecentWeeklyReportContextForKingdomExternal", StringComparison.Ordinal));
			if (method == null)
			{
				return "";
			}
			object value = method.Invoke(null, new object[] { kingdomId, 3 });
			return (value as string ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private List<NpcRulerPolicyRecord> GetRecentPolicyRecordsInternal(string kingdomId, int maxCount)
	{
		string filter = (kingdomId ?? "").Trim();
		int limit = Math.Max(1, Math.Min(200, maxCount <= 0 ? 20 : maxCount));
		return _policyRecords.Values
			.Select(DeserializeRecord)
			.Where(x => x != null && (string.IsNullOrWhiteSpace(filter) || string.Equals((x.KingdomId ?? "").Trim(), filter, StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(limit)
			.ToList();
	}

	private static List<NpcRulerPolicyRecord> ParsePolicyRecords(string raw)
	{
		return ParsePolicyRecords(raw, "", "", 0, "policy");
	}

	private static List<NpcRulerPolicyRecord> ParsePolicyRecords(string raw, string batchId, string route, int attempts, string parseSource)
	{
		string json = "";
		try
		{
			json = ExtractJson(raw, out bool ignoredTrailingText);
			if (ignoredTrailingText)
			{
				string message = "policy-json-tail-ignored batchId=" + (batchId ?? "")
					+ " route=" + (route ?? "")
					+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
					+ " source=" + FirstNonEmpty(parseSource, "policy");
				Log(message);
				PolicyTraceLog("policy-json-tail-ignored", message, "extractedChars=" + (json?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " rawChars=" + (raw?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
			}
			if (string.IsNullOrWhiteSpace(json))
			{
				List<NpcRulerPolicyRecord> recoveredWithoutRoot = RecoverPolicyRecordsFromFragments(raw, out int rootlessCandidates, out int rootlessRepaired);
				if (recoveredWithoutRoot.Count > 0)
				{
					LogPolicyFragmentRecovery(batchId, route, attempts, parseSource, recoveredWithoutRoot.Count, rootlessCandidates, rootlessRepaired, "no-complete-root");
					return recoveredWithoutRoot;
				}
				NpcPolicyStructuredParseLogger.LogFailure("NpcRulerPolicy", "policy", batchId, route, attempts, FirstNonEmpty(parseSource, "policy") + ":no-json", raw, json);
				return new List<NpcRulerPolicyRecord>();
			}
			if (!TryDeserializePolicyRecords(json, out List<NpcRulerPolicyRecord> records, out Exception parseException))
			{
				string repaired = RepairNpcPolicyJson(json);
				Exception repairedException = null;
				if (!string.Equals(repaired, json, StringComparison.Ordinal) && TryDeserializePolicyRecords(repaired, out records, out repairedException))
				{
					LogPolicyJsonRepair(batchId, route, attempts, parseSource, parseException, json, repaired);
					json = repaired;
				}
				else
				{
					List<NpcRulerPolicyRecord> recovered = RecoverPolicyRecordsFromFragments(raw, out int fragmentCandidates, out int repairedFragments);
					if (recovered.Count > 0)
					{
						LogPolicyFragmentRecovery(batchId, route, attempts, parseSource, recovered.Count, fragmentCandidates, repairedFragments, "wrapper-parse-failed");
						return recovered;
					}
					throw repairedException ?? parseException ?? new JsonException("policy json parse failed");
				}
			}
			if (records.Count == 0)
			{
				NpcPolicyStructuredParseLogger.LogFailure("NpcRulerPolicy", "policy", batchId, route, attempts, FirstNonEmpty(parseSource, "policy") + ":no-policy-records", raw, json);
			}
			return records;
		}
		catch (Exception ex)
		{
			NpcPolicyStructuredParseLogger.LogFailure("NpcRulerPolicy", "policy", batchId, route, attempts, FirstNonEmpty(parseSource, "policy") + ":" + ex.GetType().Name + ":" + ex.Message, raw, json);
			return new List<NpcRulerPolicyRecord>();
		}
	}

	private static bool TryDeserializePolicyRecords(string json, out List<NpcRulerPolicyRecord> records, out Exception exception)
	{
		records = new List<NpcRulerPolicyRecord>();
		exception = null;
		try
		{
			if ((json ?? "").TrimStart().StartsWith("[", StringComparison.Ordinal))
			{
				records = JsonConvert.DeserializeObject<List<NpcRulerPolicyRecord>>(json) ?? new List<NpcRulerPolicyRecord>();
			}
			else
			{
				NpcRulerPolicyResponse response = JsonConvert.DeserializeObject<NpcRulerPolicyResponse>(json);
				records = response?.Policies ?? new List<NpcRulerPolicyRecord>();
			}
			return true;
		}
		catch (Exception ex)
		{
			exception = ex;
			records = new List<NpcRulerPolicyRecord>();
			return false;
		}
	}

	private static string RepairNpcPolicyJson(string json)
	{
		string repaired = Regex.Replace(json ?? "", @"'(?=\s*,\s*\r?\n\s*""[A-Za-z_])", "\"", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<closing>[’”])(?=\s*,\s*\r?\n\s*""[A-Za-z_])", match => match.Groups["closing"].Value + "\"", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<closing>['’”])(?<newline>\r?\n)\s*}\s*\r?\n\s*]\s*,\s*\r?\n(?<next>\s*""effects""\s*:)", match => match.Groups["closing"].Value + "\"," + match.Groups["newline"].Value + match.Groups["next"].Value, RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"'(?=\s*\r?\n\s*[}\]])", "\"", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<closing>[’”])(?=\s*\r?\n\s*[}\]])", match => match.Groups["closing"].Value + "\"", RegexOptions.CultureInvariant);
		repaired = NormalizeJsonStructuralPunctuation(repaired);
		repaired = Regex.Replace(repaired, @"(?<name>""(?:prosperityDailyDeltaPerTown|foodDailyDeltaPerTown|hearthDailyDeltaPerVillage|loyaltyDailyDeltaPerTown|securityDailyDeltaPerTown|militiaDailyDeltaPerTown|kingdomStabilityDailyDelta|durationDays)""\s*:\s*)null\b", match => match.Groups["name"].Value + "0", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<prefix>[{,]\s*)(?<name>[A-Za-z_][A-Za-z0-9_]*)""?\s*:", match => match.Groups["prefix"].Value + "\"" + match.Groups["name"].Value + "\":", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<value>""(?:\\.|[^""\\])*"")\s*(?<next>""[A-Za-z_][A-Za-z0-9_]*""\s*:)", match => match.Groups["value"].Value + "," + match.Groups["next"].Value, RegexOptions.Singleline | RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<value>-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|true|false|null)\s*(?<next>""[A-Za-z_][A-Za-z0-9_]*""\s*:)", match => match.Groups["value"].Value + "," + match.Groups["next"].Value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<value>[\]}])\s*(?<next>""[A-Za-z_][A-Za-z0-9_]*""\s*:)", match => match.Groups["value"].Value + "," + match.Groups["next"].Value, RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @",\s*(?<close>[\]}])", match => match.Groups["close"].Value, RegexOptions.CultureInvariant);
		return repaired;
	}

	private static List<NpcRulerPolicyRecord> RecoverPolicyRecordsFromFragments(string raw, out int candidateCount, out int repairedCount)
	{
		candidateCount = 0;
		repairedCount = 0;
		List<NpcRulerPolicyRecord> records = new List<NpcRulerPolicyRecord>();
		foreach (string fragment in ExtractCompletePolicyObjectFragments(raw))
		{
			candidateCount++;
			try
			{
				NpcRulerPolicyRecord record = JsonConvert.DeserializeObject<NpcRulerPolicyRecord>(fragment);
				if (record != null)
				{
					records.Add(record);
					continue;
				}
			}
			catch
			{
			}
			string repaired = RepairNpcPolicyJson(fragment);
			if (string.Equals(repaired, fragment, StringComparison.Ordinal))
			{
				continue;
			}
			try
			{
				NpcRulerPolicyRecord record = JsonConvert.DeserializeObject<NpcRulerPolicyRecord>(repaired);
				if (record != null)
				{
					records.Add(record);
					repairedCount++;
				}
			}
			catch
			{
			}
		}
		return records;
	}

	private static List<string> ExtractCompletePolicyObjectFragments(string text)
	{
		List<string> result = new List<string>();
		text = StripJsonCodeFence(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return result;
		}
		int policiesIndex = text.IndexOf("\"policies\"", StringComparison.OrdinalIgnoreCase);
		int arrayStart = policiesIndex >= 0 ? text.IndexOf('[', policiesIndex + 10) : text.IndexOf('[');
		if (arrayStart < 0)
		{
			return result;
		}
		List<char> expectedClosers = new List<char> { ']' };
		bool inString = false;
		bool escaped = false;
		int fragmentStart = -1;
		for (int i = arrayStart + 1; i < text.Length && expectedClosers.Count > 0; i++)
		{
			char ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					escaped = false;
				}
				else if (ch == '\\')
				{
					escaped = true;
				}
				else if (ch == '"')
				{
					inString = false;
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				continue;
			}
			if (ch == '{' || ch == '[')
			{
				if (ch == '{' && expectedClosers.Count == 1)
				{
					fragmentStart = i;
				}
				expectedClosers.Add(ch == '{' ? '}' : ']');
				continue;
			}
			if (ch != '}' && ch != ']')
			{
				continue;
			}
			if (expectedClosers.Count == 0 || expectedClosers[expectedClosers.Count - 1] != ch)
			{
				break;
			}
			expectedClosers.RemoveAt(expectedClosers.Count - 1);
			if (ch == '}' && fragmentStart >= 0 && expectedClosers.Count == 1)
			{
				result.Add(text.Substring(fragmentStart, i - fragmentStart + 1));
				fragmentStart = -1;
			}
		}
		return result;
	}

	private static void LogPolicyFragmentRecovery(string batchId, string route, int attempts, string parseSource, int recoveredCount, int candidateCount, int repairedCount, string reason)
	{
		string message = "policy-fragment-recovery"
			+ " batchId=" + (batchId ?? "")
			+ " route=" + (route ?? "")
			+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
			+ " source=" + FirstNonEmpty(parseSource, "policy")
			+ " reason=" + (reason ?? "")
			+ " candidates=" + Math.Max(0, candidateCount).ToString(CultureInfo.InvariantCulture)
			+ " recovered=" + Math.Max(0, recoveredCount).ToString(CultureInfo.InvariantCulture)
			+ " repaired=" + Math.Max(0, repairedCount).ToString(CultureInfo.InvariantCulture);
		Log(message);
		PolicyTraceLog("policy-fragment-recovery", message, "local recovery completed without another LLM request");
	}

	private static string NormalizeJsonStructuralPunctuation(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? "";
		}
		StringBuilder sb = new StringBuilder(text.Length);
		bool inString = false;
		bool escaped = false;
		for (int i = 0; i < text.Length; i++)
		{
			char ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					sb.Append(ch);
					escaped = false;
				}
				else if (ch == '\\')
				{
					sb.Append(ch);
					escaped = true;
				}
				else if (ch == '"')
				{
					sb.Append(ch);
					inString = false;
				}
				else if (ch == '\r')
				{
					sb.Append("\\n");
					if (i + 1 < text.Length && text[i + 1] == '\n')
					{
						i++;
					}
				}
				else if (ch == '\n')
				{
					sb.Append("\\n");
				}
				else if (ch == '\t')
				{
					sb.Append("\\t");
				}
				else if (ch == '\b')
				{
					sb.Append("\\b");
				}
				else if (ch == '\f')
				{
					sb.Append("\\f");
				}
				else if (ch < ' ')
				{
					sb.Append("\\u");
					sb.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					sb.Append(ch);
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				sb.Append(ch);
				continue;
			}
			if (ch == '，')
			{
				sb.Append(',');
				continue;
			}
			if (ch == '：')
			{
				sb.Append(':');
				continue;
			}
			sb.Append(ch);
		}
		return sb.ToString();
	}

	private static void LogPolicyJsonRepair(string batchId, string route, int attempts, string parseSource, Exception firstException, string originalJson, string repairedJson)
	{
		string message = "policy-parse-repaired"
			+ " batchId=" + (batchId ?? "")
			+ " route=" + (route ?? "")
			+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
			+ " source=" + FirstNonEmpty(parseSource, "policy")
			+ " firstError=" + (firstException == null ? "" : firstException.GetType().Name + ":" + firstException.Message);
		Log(message);
		PolicyTraceLog("policy-parse-repaired", message, "original_sample:\n" + Limit(originalJson, 1200) + "\n\nrepaired_sample:\n" + Limit(repairedJson, 1200));
	}

	private static string ExtractJson(string text)
	{
		return ExtractJson(text, out _);
	}

	private static string ExtractJson(string text, out bool ignoredTrailingText)
	{
		ignoredTrailingText = false;
		text = StripJsonCodeFence(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int objectStart = text.IndexOf('{');
		int arrayStart = text.IndexOf('[');
		int start;
		if (objectStart < 0)
		{
			start = arrayStart;
		}
		else if (arrayStart < 0)
		{
			start = objectStart;
		}
		else
		{
			start = Math.Min(objectStart, arrayStart);
		}
		if (start < 0)
		{
			return "";
		}
		List<char> expectedClosers = new List<char>();
		bool inString = false;
		bool escaped = false;
		for (int i = start; i < text.Length; i++)
		{
			char ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					escaped = false;
				}
				else if (ch == '\\')
				{
					escaped = true;
				}
				else if (ch == '"')
				{
					inString = false;
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				continue;
			}
			if (ch == '{' || ch == '[')
			{
				expectedClosers.Add(ch == '{' ? '}' : ']');
				continue;
			}
			if (ch != '}' && ch != ']')
			{
				continue;
			}
			if (expectedClosers.Count == 0 || expectedClosers[expectedClosers.Count - 1] != ch)
			{
				return "";
			}
			expectedClosers.RemoveAt(expectedClosers.Count - 1);
			if (expectedClosers.Count == 0)
			{
				string trailing = text.Substring(i + 1).Trim();
				ignoredTrailingText = !string.IsNullOrWhiteSpace(trailing);
				return text.Substring(start, i - start + 1);
			}
		}
		return "";
	}

	private static string StripJsonCodeFence(string text)
	{
		text = (text ?? "").Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			text = Regex.Replace(text, "^```(?:json)?", "", RegexOptions.IgnoreCase).Trim();
			text = Regex.Replace(text, "```$", "", RegexOptions.IgnoreCase).Trim();
		}
		return text;
	}

	private static NpcRulerPolicyRecord DeserializeRecord(string raw)
	{
		try
		{
			NpcRulerPolicyRecord record = JsonConvert.DeserializeObject<NpcRulerPolicyRecord>(raw);
			if (record == null || string.IsNullOrWhiteSpace(record.PolicyId))
			{
				return null;
			}
			record.Effects = (record.Effects ?? new List<NpcRulerPolicyEffectDto>()).Where(HasAnyDailyDelta).ToList();
			return record;
		}
		catch
		{
			return null;
		}
	}

	private void TrimPolicyRecords()
	{
		List<NpcRulerPolicyRecord> ordered = _policyRecords.Values.Select(DeserializeRecord).Where(x => x != null)
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.ToList();
		foreach (NpcRulerPolicyRecord extra in ordered.Skip(MaxPolicyRecordCount).ToList())
		{
			string id = (extra.PolicyId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(id))
			{
				_policyRecords.Remove(id);
			}
		}
	}

	private IEnumerable<Kingdom> GetNpcRuledKingdoms()
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			return (Kingdom.All ?? Enumerable.Empty<Kingdom>())
				.Where(k => k != null && !k.IsEliminated)
				.Where(k => (k.Leader ?? k.RulingClan?.Leader) != null)
				.Where(k => mainHero == null || (k.Leader != mainHero && k.RulingClan?.Leader != mainHero))
				.OrderBy(k => GetKingdomName(k), StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return Enumerable.Empty<Kingdom>();
		}
	}

	private static List<Settlement> GetKingdomSettlements(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return new List<Settlement>();
		}
		try
		{
			return Settlement.All.Where(s => s != null && s.MapFaction == kingdom && (s.Town != null || s.Village != null)).ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private string BuildRecentNpcPolicyContext(string kingdomId)
	{
		List<NpcRulerPolicyRecord> recent = GetRecentPolicyRecordsInternal(kingdomId, 3);
		if (recent.Count == 0)
		{
			return "";
		}
		return Limit(string.Join("；", recent.Select(x => x.GameDate + "《" + x.PolicyName + "》" + FirstNonEmpty(x.ImpactSummary, x.PolicyContent))), 500);
	}

	private static string BuildDiplomacyNeighborSummary(Kingdom kingdom)
	{
		try
		{
			List<string> wars = new List<string>();
			List<string> others = new List<string>();
			foreach (Kingdom other in Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (other == null || other == kingdom || other.IsEliminated)
				{
					continue;
				}
				if (kingdom.IsAtWarWith(other))
				{
					wars.Add(GetKingdomName(other));
				}
				else if (others.Count < 5)
				{
					others.Add(GetKingdomName(other));
				}
			}
			return "战争=" + (wars.Count == 0 ? "无" : string.Join("、", wars.Take(5))) + "；其他=" + (others.Count == 0 ? "无" : string.Join("、", others));
		}
		catch
		{
			return "未知";
		}
	}

	private static string BuildRulerTraitSummary(Hero ruler)
	{
		if (ruler == null)
		{
			return "未知";
		}
		try
		{
			List<string> parts = new List<string>
			{
				"Mercy=" + ruler.GetTraitLevel(DefaultTraits.Mercy).ToString(CultureInfo.InvariantCulture),
				"Valor=" + ruler.GetTraitLevel(DefaultTraits.Valor).ToString(CultureInfo.InvariantCulture),
				"Honor=" + ruler.GetTraitLevel(DefaultTraits.Honor).ToString(CultureInfo.InvariantCulture),
				"Generosity=" + ruler.GetTraitLevel(DefaultTraits.Generosity).ToString(CultureInfo.InvariantCulture),
				"Calculating=" + ruler.GetTraitLevel(DefaultTraits.Calculating).ToString(CultureInfo.InvariantCulture)
			};
			return string.Join(",", parts);
		}
		catch
		{
			return "读取失败";
		}
	}

	private static string SafeReadVanillaPolicies(Kingdom kingdom)
	{
		try
		{
			string policies = string.Join("、", kingdom.ActivePolicies.Where(p => p != null).Select(p => p.Name?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
			return string.IsNullOrWhiteSpace(policies) ? "无" : Limit(policies, 300);
		}
		catch
		{
			return "读取失败";
		}
	}

	private static float SafeKingdomStrength(Kingdom kingdom)
	{
		try
		{
			foreach (Clan clan in ((IEnumerable<Clan>)kingdom?.Clans) ?? Enumerable.Empty<Clan>())
			{
				try
				{
					clan?.UpdateCurrentStrength();
				}
				catch
				{
				}
			}
			float value = kingdom?.CurrentTotalStrength ?? 0f;
			return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, value);
		}
		catch
		{
			return 0f;
		}
	}

	private static int SafeKingdomStability(Kingdom kingdom)
	{
		try
		{
			return MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
		}
		catch
		{
			return 50;
		}
	}

	private static bool HasAnyDailyDelta(NpcRulerPolicyEffectDto effect)
	{
		return effect != null
			&& (Math.Abs(effect.ProsperityDailyDeltaPerTown) > 0.0001f
				|| Math.Abs(effect.FoodDailyDeltaPerTown) > 0.0001f
				|| Math.Abs(effect.HearthDailyDeltaPerVillage) > 0.0001f
				|| Math.Abs(effect.LoyaltyDailyDeltaPerTown) > 0.0001f
				|| Math.Abs(effect.SecurityDailyDeltaPerTown) > 0.0001f
				|| Math.Abs(effect.MilitiaDailyDeltaPerTown) > 0.0001f
				|| Math.Abs(effect.KingdomStabilityDailyDelta) > 0.0001f);
	}

	private static string BuildEffectSummary(List<NpcRulerPolicyEffectDto> effects)
	{
		NpcRulerPolicyEffectDto effect = (effects ?? new List<NpcRulerPolicyEffectDto>()).FirstOrDefault();
		if (effect == null)
		{
			return "";
		}
		return "每日繁荣" + FormatSigned(effect.ProsperityDailyDeltaPerTown)
			+ " 粮食" + FormatSigned(effect.FoodDailyDeltaPerTown)
			+ " 炉户" + FormatSigned(effect.HearthDailyDeltaPerVillage)
			+ " 忠诚" + FormatSigned(effect.LoyaltyDailyDeltaPerTown)
			+ " 治安" + FormatSigned(effect.SecurityDailyDeltaPerTown)
			+ " 民兵" + FormatSigned(effect.MilitiaDailyDeltaPerTown)
			+ " 稳定度" + FormatSigned(effect.KingdomStabilityDailyDelta)
			+ " 持续" + effect.DurationDays.ToString(CultureInfo.InvariantCulture) + "天";
	}

	private void NormalizeGenerationClock(int currentDay, int currentHour)
	{
		if (_lastGeneratedHour < 0 && _lastGeneratedDay >= 0)
		{
			_lastGeneratedHour = _lastGeneratedDay * 24;
		}
		if (_lastGeneratedHour > currentHour)
		{
			Log("generation-clock-reset reason=last-generated-in-future currentHour=" + currentHour.ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + _lastGeneratedHour.ToString(CultureInfo.InvariantCulture));
			_lastGeneratedHour = -1;
			_lastGeneratedDay = -1;
		}
		if (_lastGenerationAttemptHour > currentHour)
		{
			_lastGenerationAttemptHour = -1;
		}
		if (_lastGenerationFailureHour > currentHour)
		{
			_lastGenerationFailureHour = -1;
		}
	}

	private static bool IsCampaignSessionReady()
	{
		return Campaign.Current != null;
	}

	private static bool IsMainApiConfiguredForNpcPolicy()
	{
		try
		{
			return NpcPolicyLlmClient.IsConfiguredForNpcPolicy(out var _);
		}
		catch
		{
			return false;
		}
	}

	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static int GetCurrentCampaignHour()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToHours));
		}
		catch
		{
			return GetCurrentCampaignDay() * 24;
		}
	}

	private static string FormatCurrentCampaignDate()
	{
		try
		{
			return CampaignTime.Now.ToString();
		}
		catch
		{
			return "第" + GetCurrentCampaignDay().ToString(CultureInfo.InvariantCulture) + "天";
		}
	}

	private static string GetKingdomName(Kingdom kingdom)
	{
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国";
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}

	private static string Compact(string text)
	{
		return Regex.Replace((text ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim(), "\\s+", " ");
	}

	private static string Limit(string text, int maxChars)
	{
		text = Compact(text);
		if (maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, maxChars).TrimEnd() + "…";
	}

	private static string NormalizeKeyPart(string text)
	{
		text = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "none";
		}
		StringBuilder sb = new StringBuilder();
		foreach (char ch in text)
		{
			if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == ':')
			{
				sb.Append(ch);
			}
		}
		return sb.Length == 0 ? "x" : sb.ToString();
	}

	private static int Clamp(int value, int min, int max)
	{
		return Math.Max(min, Math.Min(max, value));
	}

	private static string FormatNumber(float value)
	{
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	private static string FormatSigned(float value)
	{
		return value >= 0f ? "+" + FormatNumber(value) : FormatNumber(value);
	}

	private static void Log(string message)
	{
		try
		{
			Logger.Log("NpcRulerPolicy", message ?? "");
		}
		catch
		{
		}
	}

	private sealed class NpcRulerPolicyBatchContext
	{
		public string BatchId;
		public int Day;
		public int Hour;
		public string GameDate;
		public string CompactWorldContext;
		public string SelectionDiagnostics;
		public int DayLimit;
		public int BatchSize;
		public int TodayAlreadyGenerated;
		public int RemainingDailySlots;
		public int EligibleCount;
		public int ExcludedCount;
		public List<NpcRulerPolicyKingdomContext> Kingdoms = new List<NpcRulerPolicyKingdomContext>();
		public List<NpcRulerPolicySnapshotTarget> PendingTargets = new List<NpcRulerPolicySnapshotTarget>();
		public int SnapshotTargetIndex;
	}

	private sealed class NpcRulerPolicySnapshotTarget
	{
		public string KingdomId;
		public string KingdomName;
		public string LastGeneratedText;
	}

	private sealed class NpcRulerPolicyKingdomContext
	{
		public string KingdomId;
		public string KingdomName;
		public string RulerHeroId;
		public string RulerName;
		public string RequiredContext;
		public string PersonaContext;
		public List<string> PreviousPolicyContexts = new List<string>();
		public List<string> ForeignPolicyGroupContexts = new List<string>();
		public string SupplementalContext;
		public List<NpcRulerPolicyAllowedEffectTarget> AllowedEffectTargets = new List<NpcRulerPolicyAllowedEffectTarget>();
	}

	private sealed class NpcRulerPolicyAllowedEffectTarget
	{
		public string KingdomId;
		public string KingdomName;
		public bool IsIssuer;
		public float Strength;
		public List<string> MentionCandidates = new List<string>();
	}

	private sealed class NpcRulerPolicyGenerationCandidate
	{
		public Kingdom Kingdom;
		public string KingdomId;
		public string KingdomName;
		public int LastGeneratedHour = -1;
		public string LastGeneratedText;
		public string ExclusionReason;
		public bool IsEligible;
	}

	private sealed class NpcPolicyGenerationJob
	{
		public string JobId = "";
		public string BatchId = "";
		public string TriggerSource = "";
		public NpcRulerPolicyBatchContext Context;
		public string SystemPrompt = "";
		public string UserPrompt = "";
		public string PromptPreview = "";
		public int Day;
		public int Hour;
		public string InFlightKey = "";
		public int Version;
		public long RuntimeGeneration;
		public int MaxTokens;
		public int HardTimeoutMilliseconds;
		public long CreatedUtcTicks;
	}

	private sealed class NpcPolicyGenerationResult
	{
		public NpcPolicyGenerationJob Job;
		public bool Success;
		public string RawResponse = "";
		public string Error = "";
		public List<NpcRulerPolicyRecord> Records = new List<NpcRulerPolicyRecord>();
		public List<string> FailureMessages = new List<string>();
		public int ParsedCount;
		public int AttemptsUsed;
		public int BatchAttemptsUsed;
		public int FallbackAttemptsUsed;
		public int FallbackSuccessCount;
		public int FallbackFailureCount;
		public bool IsRateLimit;
		public bool IsRequestsPerMinuteLimit;
		public bool IsQuotaLimit;
		public bool IsAuthFailure;
		public int? RetryAfterSeconds;
		public int? RetryAfterSecondsRaw;
		public bool RetryAfterSecondsCapped;
	}

	private sealed class NpcPolicyRetryContext
	{
		public string BatchId = "";
		public string TriggerSource = "";
		public int Day;
		public int Hour;
		public string FailedReason = "";
		public int AttemptsUsed;
		public bool IsRateLimit;
		public bool IsRequestsPerMinuteLimit;
		public bool IsQuotaLimit;
		public bool IsAuthFailure;
		public int? RetryAfterSeconds;
		public int FallbackSuccessCount;
		public int FallbackFailureCount;
		public List<string> FailedKingdomIds = new List<string>();
		public List<string> FailureMessages = new List<string>();
	}

	private sealed class PendingNpcPolicyCommitContext
	{
		public NpcPolicyGenerationResult GenerationResult;
		public int RecordIndex;
		public int SavedCount;
		public int PublicFeedbackSavedCount;
		public int ActiveEffectsCreatedCount;
		public PendingNpcPolicyCommitStage Stage;
		public string SerializedRecord;
		public AnimusForgeWorldEventInboxEntry PublicFeedbackEntry;
		public int ActiveEffectIndex;
	}

	private enum PendingNpcPolicyCommitStage
	{
		SerializeRecord,
		StoreRecord,
		UpsertPolicyEvent,
		RecordPolicyWeeklyMaterial,
		CommitPublicFeedback,
		CreateActiveEffect
	}

	private sealed class NpcRulerPolicyResponse
	{
		[JsonProperty("policies")]
		public List<NpcRulerPolicyRecord> Policies { get; set; }
	}
}

// NPC ruler policy public-feedback events and shared world-event inbox.
public sealed class AnimusForgeWorldEventInboxEntry
{
	public int Version { get; set; } = 1;
	public string EventId { get; set; }
	public string EventKind { get; set; }
	public string EventType { get; set; }
	public string Title { get; set; }
	public string Summary { get; set; }
	public string DetailText { get; set; }
	public string KingdomId { get; set; }
	public string KingdomName { get; set; }
	public string ActorHeroId { get; set; }
	public string ActorHeroName { get; set; }
	public string PolicyId { get; set; }
	public string PolicyName { get; set; }
	public int Day { get; set; }
	public string GameDate { get; set; }
	public long CreatedUtcTicks { get; set; }
	public string StableKey { get; set; }
	public bool IsRead { get; set; }
}

public sealed class NpcPublicFeedbackEventBehavior : CampaignBehaviorBase
{
	private const string SaveKeyEventRecords = "_afWorldEventInboxRecords_v1";
	private const string SaveKeyUnreadEventIds = "_afWorldEventInboxUnread_v1";
	private const int MaxEventRecordCount = 240;
	private const int MaxTitleChars = 90;
	private const int MaxSummaryChars = 260;
	private const int MaxDetailChars = 1200;
	private const int MaxFeedbackContextItems = 3;
	private const int FeedbackMaxTokens = 5000;
	private const int FeedbackApiHardTimeoutMilliseconds = 540000;
	private const int FeedbackCommitMaxEventsPerTick = 4;
	private const double FeedbackCommitFrameBudgetMs = 3.0;

	public static NpcPublicFeedbackEventBehavior Instance { get; private set; }

	private readonly Dictionary<string, string> _eventRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _unreadEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentQueue<PendingNpcPolicyCommitContext> _pendingFeedbackCommits = new ConcurrentQueue<PendingNpcPolicyCommitContext>();
	private readonly object _feedbackGenerationStateLock = new object();
	private readonly HashSet<string> _feedbackGenerationInFlightKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private bool _feedbackGenerationInProgress;
	private string _feedbackActiveInFlightKey = "";
	private string _feedbackActiveRequestFingerprint = "";
	private PendingFeedbackGenerationRequest _pendingFeedbackRequest;
	private int _lastFeedbackRetryCount;
	private string _lastFeedbackError = "";
	private NpcFeedbackRetryContext _lastFeedbackRetryContext;
	private int _feedbackGenerationVersion;
	private long _version;

	public NpcPublicFeedbackEventBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		try
		{
			Logger.Log("NpcPublicFeedbackEvent", "registered");
		}
		catch
		{
		}
		if (Campaign.Current == null)
		{
			return;
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (dataStore.IsSaving)
		{
			TrimEventRecords();
			Dictionary<string, string> records = CampaignSaveChunkHelper.FlattenStringDictionary(_eventRecords, SaveKeyEventRecords, "WorldEventInbox");
			dataStore.SyncData(SaveKeyEventRecords, ref records);
			List<string> unread = _unreadEventIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			dataStore.SyncData(SaveKeyUnreadEventIds, ref unread);
			Log("save-write records=" + _eventRecords.Count.ToString(CultureInfo.InvariantCulture) + " unread=" + unread.Count.ToString(CultureInfo.InvariantCulture));
			return;
		}
		ClearFeedbackTransientRuntimeForLoadedSave("sync-load", incrementVersion: true);
		_eventRecords.Clear();
		_unreadEventIds.Clear();
		Dictionary<string, string> stored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyEventRecords, ref stored);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(stored, "WorldEventInbox"))
		{
			string key = (item.Key ?? "").Trim();
			string raw = (item.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(raw))
			{
				continue;
			}
			try
			{
				AnimusForgeWorldEventInboxEntry entry = JsonConvert.DeserializeObject<AnimusForgeWorldEventInboxEntry>(raw);
				if (entry != null && !string.IsNullOrWhiteSpace(entry.EventId))
				{
					_eventRecords[key] = JsonConvert.SerializeObject(NormalizeEntry(entry));
				}
			}
			catch (Exception ex)
			{
				Log("save-load-skip key=" + key + " error=" + ex.Message);
			}
		}
		List<string> unreadIds = new List<string>();
		dataStore.SyncData(SaveKeyUnreadEventIds, ref unreadIds);
		foreach (string id in unreadIds ?? new List<string>())
		{
			string clean = (id ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(clean) && _eventRecords.ContainsKey(clean))
			{
				_unreadEventIds.Add(clean);
			}
		}
		TrimEventRecords();
		_version++;
		Log("save-read records=" + _eventRecords.Count.ToString(CultureInfo.InvariantCulture) + " unread=" + _unreadEventIds.Count.ToString(CultureInfo.InvariantCulture));
	}

	public void OnEngineTick()
	{
		ProcessPendingFeedbackCommits();
		TryStartPendingFeedbackGeneration("engine-tick");
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ClearFeedbackTransientRuntimeForLoadedSave("new_game_created", incrementVersion: true);
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		ClearFeedbackTransientRuntimeForLoadedSave("game_loaded", incrementVersion: true);
	}

	private void ClearFeedbackTransientRuntimeForLoadedSave(string reason, bool incrementVersion)
	{
		_lastFeedbackRetryCount = 0;
		_lastFeedbackError = "";
		_lastFeedbackRetryContext = null;
		while (_pendingFeedbackCommits.TryDequeue(out var _))
		{
		}
		lock (_feedbackGenerationStateLock)
		{
			_feedbackGenerationInProgress = false;
			if (incrementVersion)
			{
				_feedbackGenerationVersion++;
			}
			_feedbackGenerationInFlightKeys.Clear();
			_feedbackActiveInFlightKey = "";
			_feedbackActiveRequestFingerprint = "";
			_pendingFeedbackRequest = null;
		}
		Log("transient-cleared reason=" + (reason ?? ""));
	}

	private enum FeedbackGenerationBeginStatus
	{
		Started,
		Busy,
		DuplicateInFlight
	}

	private static string NormalizeFeedbackInFlightKey(string inFlightKey)
	{
		string key = (inFlightKey ?? "").Trim();
		return string.IsNullOrWhiteSpace(key) ? "npc_feedback:unknown" : key;
	}

	private static string BuildFeedbackGenerationInFlightKey(string batchId, IReadOnlyList<NpcRulerPolicyRecord> policies)
	{
		string batch = (batchId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(batch))
		{
			return "npc_feedback:" + NormalizeKeyPart(batch);
		}
		string policyKey = string.Join(",", (policies ?? Array.Empty<NpcRulerPolicyRecord>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.PolicyId))
			.Select(x => x.PolicyId.Trim())
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return "npc_feedback:" + NormalizeKeyPart(policyKey);
	}

	private static string BuildFeedbackRequestFingerprint(IReadOnlyList<NpcRulerPolicyRecord> policies)
	{
		string policyKey = string.Join("|", (policies ?? Array.Empty<NpcRulerPolicyRecord>())
			.Where(x => x != null)
			.Select(x => NormalizeKeyPart(x.PolicyId) + "@" + NormalizeKeyPart(x.KingdomId))
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return string.IsNullOrWhiteSpace(policyKey) ? "none" : policyKey;
	}

	private static string BuildPolicyIdsForLog(IReadOnlyList<NpcRulerPolicyRecord> policies)
	{
		return Limit(string.Join(",", (policies ?? Array.Empty<NpcRulerPolicyRecord>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.PolicyId))
			.Select(x => x.PolicyId.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(24)), 500);
	}

	private FeedbackGenerationBeginStatus TryBeginFeedbackGenerationInFlight(string inFlightKey, string requestFingerprint, out string activeInFlightKey)
	{
		string key = NormalizeFeedbackInFlightKey(inFlightKey);
		string fingerprint = requestFingerprint ?? "";
		lock (_feedbackGenerationStateLock)
		{
			activeInFlightKey = _feedbackActiveInFlightKey ?? "";
			if (_feedbackGenerationInProgress || _feedbackGenerationInFlightKeys.Count > 0)
			{
				bool sameKey = _feedbackGenerationInFlightKeys.Contains(key) || string.Equals(activeInFlightKey, key, StringComparison.OrdinalIgnoreCase);
				bool sameRequest = sameKey && string.Equals(_feedbackActiveRequestFingerprint ?? "", fingerprint, StringComparison.Ordinal);
				return sameRequest ? FeedbackGenerationBeginStatus.DuplicateInFlight : FeedbackGenerationBeginStatus.Busy;
			}
			_feedbackGenerationInFlightKeys.Add(key);
			_feedbackActiveInFlightKey = key;
			_feedbackActiveRequestFingerprint = fingerprint;
			_feedbackGenerationInProgress = true;
			activeInFlightKey = key;
			return FeedbackGenerationBeginStatus.Started;
		}
	}

	private int NextFeedbackGenerationVersion()
	{
		lock (_feedbackGenerationStateLock)
		{
			return ++_feedbackGenerationVersion;
		}
	}

	private bool TryFinalizeFeedbackGenerationState(string inFlightKey, int version, string reason)
	{
		string key = NormalizeFeedbackInFlightKey(inFlightKey);
		lock (_feedbackGenerationStateLock)
		{
			if (version > 0 && version != _feedbackGenerationVersion)
			{
				Log("feedback-finalize-skip reason=version-mismatch stage=" + (reason ?? "") + " jobVersion=" + version.ToString(CultureInfo.InvariantCulture) + " currentVersion=" + _feedbackGenerationVersion.ToString(CultureInfo.InvariantCulture) + " key=" + key);
				return false;
			}
			_feedbackGenerationInProgress = false;
			_feedbackGenerationInFlightKeys.Remove(key);
			if (string.Equals(_feedbackActiveInFlightKey, key, StringComparison.OrdinalIgnoreCase) || _feedbackGenerationInFlightKeys.Count == 0)
			{
				_feedbackActiveInFlightKey = _feedbackGenerationInFlightKeys.FirstOrDefault() ?? "";
				if (string.IsNullOrWhiteSpace(_feedbackActiveInFlightKey))
				{
					_feedbackActiveRequestFingerprint = "";
				}
			}
			return true;
		}
	}

	private void DiscardPendingFeedbackRequestForRuntime(long runtimeGeneration, string reason)
	{
		bool discarded = false;
		string batchId = "";
		lock (_feedbackGenerationStateLock)
		{
			if (_pendingFeedbackRequest != null && _pendingFeedbackRequest.RuntimeGeneration == runtimeGeneration)
			{
				batchId = _pendingFeedbackRequest.BatchId ?? "";
				_pendingFeedbackRequest = null;
				discarded = true;
			}
		}
		if (discarded)
		{
			Log("feedback-pending-discard reason=" + (reason ?? "") + " batch=" + batchId);
		}
	}

	private void QueuePendingFeedbackRequest(List<NpcRulerPolicyRecord> policySnapshot, string compactWorldContext, string batchId, string inFlightKey, string requestFingerprint, string activeInFlightKey, string reason)
	{
		PendingFeedbackGenerationRequest pending = new PendingFeedbackGenerationRequest
		{
			BatchId = batchId ?? "",
			InFlightKey = NormalizeFeedbackInFlightKey(inFlightKey),
			RequestFingerprint = requestFingerprint ?? "",
			RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
			CompactWorldContext = compactWorldContext ?? "",
			Policies = (policySnapshot ?? new List<NpcRulerPolicyRecord>()).Select(ClonePolicyRecord).Where(x => x != null).ToList()
		};
		string replacedBatch = "";
		lock (_feedbackGenerationStateLock)
		{
			replacedBatch = _pendingFeedbackRequest?.BatchId ?? "";
			_pendingFeedbackRequest = pending;
		}
		Log("feedback-pending-upsert reason=" + (reason ?? "in-progress") + " batch=" + (batchId ?? "") + " activeKey=" + (activeInFlightKey ?? "") + " replacedBatch=" + replacedBatch + " policyIds=" + BuildPolicyIdsForLog(policySnapshot));
	}

	private void TryStartPendingFeedbackGeneration(string reason)
	{
		PendingFeedbackGenerationRequest pending = null;
		lock (_feedbackGenerationStateLock)
		{
			if (_feedbackGenerationInProgress || _pendingFeedbackRequest == null)
			{
				return;
			}
			pending = _pendingFeedbackRequest;
			_pendingFeedbackRequest = null;
		}
		if (pending == null || pending.Policies == null || pending.Policies.Count == 0)
		{
			return;
		}
		if (SaveRuntimeGuard.IsStale(pending.RuntimeGeneration, "npc_policy_feedback_pending_start"))
		{
			Log("feedback-pending-discard reason=stale-runtime batch=" + (pending.BatchId ?? ""));
			return;
		}
		string cleanReason = (reason ?? "").Trim();
		if (string.Equals(cleanReason, "failed", StringComparison.OrdinalIgnoreCase))
		{
			Log("feedback-pending-discard reason=failed batch=" + (pending.BatchId ?? "") + " policies=" + pending.Policies.Count.ToString(CultureInfo.InvariantCulture));
			return;
		}
		Log("feedback-pending-start reason=" + cleanReason + " batch=" + (pending.BatchId ?? "") + " policies=" + pending.Policies.Count.ToString(CultureInfo.InvariantCulture));
		StartFeedbackGeneration(pending.Policies, pending.CompactWorldContext);
	}

	public static long GetInboxVersionForExternal()
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?._version ?? 0L;
		}
		catch
		{
			return 0L;
		}
	}

	public static List<AnimusForgeWorldEventInboxEntry> GetInboxSnapshotForExternal(int maxCount = 80)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.GetInboxSnapshotInternal(maxCount) ?? new List<AnimusForgeWorldEventInboxEntry>();
		}
		catch
		{
			return new List<AnimusForgeWorldEventInboxEntry>();
		}
	}

	public static int GetUnreadCountForExternal()
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?._unreadEventIds.Count ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	public static bool MarkEventReadForExternal(string eventId)
	{
		return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.MarkEventReadInternal(eventId) == true;
	}

	public static void MarkAllReadForExternal()
	{
		(Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.MarkAllReadInternal();
	}

	public static void UpsertWorldEventForExternal(AnimusForgeWorldEventInboxEntry entry, bool markUnread = true)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.UpsertWorldEventInternal(entry, markUnread);
		}
		catch (Exception ex)
		{
			Log("upsert-external-failed " + ex.Message);
		}
	}

	public static AnimusForgeWorldEventInboxEntry BuildPolicyPublicFeedbackForExternal(NpcRulerPolicyRecord policy, int index = 0)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.BuildPolicyPublicFeedbackEntry(policy, index);
		}
		catch (Exception ex)
		{
			Log("inline-policy-feedback-build-failed policy=" + (policy?.PolicyId ?? "") + " error=" + ex.Message);
			return null;
		}
	}

	public static bool CommitPolicyPublicFeedbackEventForExternal(AnimusForgeWorldEventInboxEntry entry)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.CommitPolicyPublicFeedbackEventInternal(entry) == true;
		}
		catch (Exception ex)
		{
			Log("inline-policy-feedback-event-failed event=" + (entry?.EventId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	public static string BuildRecentPublicFeedbackEventContextForKingdomExternal(string kingdomId, int maxItems = MaxFeedbackContextItems)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.BuildRecentPublicFeedbackEventContextForKingdomInternal(kingdomId, maxItems) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static void GenerateFeedbackEventsForPoliciesExternal(IReadOnlyList<NpcRulerPolicyRecord> policies, string compactWorldContext)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<NpcPublicFeedbackEventBehavior>())?.StartFeedbackGeneration(policies, compactWorldContext);
		}
		catch (Exception ex)
		{
			Log("feedback-generation-external-failed " + ex.Message);
		}
	}

	private AnimusForgeWorldEventInboxEntry BuildPolicyPublicFeedbackEntry(NpcRulerPolicyRecord policy, int index)
	{
		if (policy == null || string.IsNullOrWhiteSpace(policy.PublicFeedback))
		{
			return null;
		}
		string kingdomId = FirstNonEmpty(policy.KingdomId);
		string kingdomName = FirstNonEmpty(policy.KingdomName, "某王国");
		string text = Limit(policy.PublicFeedback, MaxDetailChars);
		if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string policyKey = FirstNonEmpty(policy.PolicyId, policy.BatchId + ":" + kingdomId + ":" + Math.Max(0, index).ToString(CultureInfo.InvariantCulture));
		string eventId = "npc_public_feedback:" + NormalizeKeyPart(policyKey);
		string title = Limit(FirstNonEmpty(policy.FeedbackTitle, "《" + FirstNonEmpty(policy.PolicyName, "新政策") + "》的余波"), MaxTitleChars);
		long policyTicks = policy.CreatedUtcTicks > 0L ? policy.CreatedUtcTicks : DateTime.UtcNow.Ticks;
		return new AnimusForgeWorldEventInboxEntry
		{
			EventId = eventId,
			EventKind = "npc_ruler_policy_event",
			EventType = "",
			Title = title,
			Summary = Limit(text, MaxSummaryChars),
			DetailText = text,
			KingdomId = kingdomId,
			KingdomName = kingdomName,
			ActorHeroId = policy.RulerHeroId ?? "",
			ActorHeroName = policy.RulerName ?? "",
			PolicyId = policy.PolicyId ?? "",
			PolicyName = policy.PolicyName ?? "",
			Day = Math.Max(0, policy.Day > 0 ? policy.Day : GetCurrentCampaignDay()),
			GameDate = FirstNonEmpty(policy.GameDate, FormatCurrentCampaignDate()),
			CreatedUtcTicks = policyTicks > 1L ? policyTicks - 1L : policyTicks,
			StableKey = eventId,
			IsRead = false
		};
	}

	private bool CommitPolicyPublicFeedbackEventInternal(AnimusForgeWorldEventInboxEntry entry)
	{
		if (entry == null || string.IsNullOrWhiteSpace(entry.EventId))
		{
			return false;
		}
		UpsertWorldEventInternal(entry, markUnread: true);
		Log("inline-policy-feedback-committed policy=" + (entry.PolicyId ?? "") + " event=" + (entry.EventId ?? ""));
		return true;
	}

	private void StartFeedbackGeneration(IReadOnlyList<NpcRulerPolicyRecord> policies, string compactWorldContext)
	{
		if (!IsCampaignSessionReady())
		{
			Log("feedback-skip reason=campaign-not-ready");
			return;
		}
		List<NpcRulerPolicyRecord> policySnapshot = (policies ?? Array.Empty<NpcRulerPolicyRecord>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.PolicyId) && !string.IsNullOrWhiteSpace(x.KingdomId))
			.Take(12)
			.Select(ClonePolicyRecord)
			.ToList();
		if (policySnapshot.Count == 0)
		{
			return;
		}
		string batchId = policySnapshot.FirstOrDefault()?.BatchId;
		string inFlightKey = BuildFeedbackGenerationInFlightKey(batchId, policySnapshot);
		string requestFingerprint = BuildFeedbackRequestFingerprint(policySnapshot);
		FeedbackGenerationBeginStatus beginStatus = TryBeginFeedbackGenerationInFlight(inFlightKey, requestFingerprint, out string activeInFlightKey);
		if (beginStatus == FeedbackGenerationBeginStatus.DuplicateInFlight)
		{
			Log("feedback-skip reason=duplicate-in-flight batch=" + (batchId ?? "") + " key=" + inFlightKey + " activeKey=" + activeInFlightKey + " skippedPolicyIds=" + BuildPolicyIdsForLog(policySnapshot));
			return;
		}
		if (beginStatus == FeedbackGenerationBeginStatus.Busy)
		{
			string pendingReason = string.Equals(NormalizeFeedbackInFlightKey(inFlightKey), NormalizeFeedbackInFlightKey(activeInFlightKey), StringComparison.OrdinalIgnoreCase)
				? "same-key-updated"
				: "in-progress";
			QueuePendingFeedbackRequest(policySnapshot, compactWorldContext, batchId, inFlightKey, requestFingerprint, activeInFlightKey, pendingReason);
			return;
		}
		if (!NpcPolicyLlmClient.IsConfiguredForNpcPolicy(out string apiConfigError))
		{
			TryFinalizeFeedbackGenerationState(inFlightKey, 0, "api-not-configured");
			Log("feedback-skip reason=api-not-configured error=" + apiConfigError);
			TryStartPendingFeedbackGeneration("api-not-configured");
			return;
		}
		NpcPolicyPrompt prompt;
		try
		{
			prompt = BuildFeedbackPrompt(policySnapshot, compactWorldContext);
		}
		catch (Exception ex)
		{
			TryFinalizeFeedbackGenerationState(inFlightKey, 0, "message-build-failed");
			Log("feedback-message-build-failed batch=" + (batchId ?? "") + " error=" + ex);
			TryStartPendingFeedbackGeneration("message-build-failed");
			return;
		}
		_lastFeedbackRetryContext = null;
		NpcPolicyFeedbackJob job = new NpcPolicyFeedbackJob
		{
			JobId = "npc_policy_feedback:" + NormalizeKeyPart(batchId),
			BatchId = batchId ?? "",
			Policies = policySnapshot,
			CompactWorldContext = compactWorldContext ?? "",
			SystemPrompt = prompt.SystemPrompt,
			UserPrompt = prompt.UserPrompt,
			PromptPreview = prompt.Preview,
			InFlightKey = inFlightKey,
			Version = NextFeedbackGenerationVersion(),
			RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
			MaxTokens = FeedbackMaxTokens,
			HardTimeoutMilliseconds = FeedbackApiHardTimeoutMilliseconds,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		Log("feedback-start batch=" + (batchId ?? "") + " policies=" + policySnapshot.Count.ToString(CultureInfo.InvariantCulture));
		FeedbackTraceLog("feedback-job-built", BuildFeedbackJobTracePrefix(job), job.PromptPreview);
		try
		{
			_ = Task.Run(() => ProcessFeedbackGenerationJobAsync(job));
		}
		catch (Exception ex)
		{
			TryFinalizeFeedbackGenerationState(inFlightKey, job.Version, "schedule-failed");
			_lastFeedbackError = ex.Message;
			Log("feedback-schedule-failed batch=" + (batchId ?? "") + " error=" + ex);
			TryStartPendingFeedbackGeneration("schedule-failed");
		}
	}

	private async Task ProcessFeedbackGenerationJobAsync(NpcPolicyFeedbackJob job)
	{
		NpcPolicyFeedbackResult result = new NpcPolicyFeedbackResult
		{
			Job = job
		};
		try
		{
			if (job == null)
			{
				result.Error = "empty feedback generation job";
			}
			else if (SaveRuntimeGuard.IsStale(job.RuntimeGeneration, "npc_policy_feedback_start"))
			{
				result.Error = SaveRuntimeGuard.BuildStaleRequestErrorText();
			}
			else
			{
				FeedbackTraceLog("feedback-batch-call-start", BuildFeedbackJobTracePrefix(job), job.PromptPreview);
				NpcPolicyApiCallResult apiResult = await NpcPolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(job.SystemPrompt, job.UserPrompt, job.MaxTokens, job.HardTimeoutMilliseconds, "NpcPublicFeedbackEvent", job.RuntimeGeneration, 3);
				CopyApiResultToFeedbackResult(result, apiResult, accumulateAttempts: false);
				FeedbackTraceLog("feedback-batch-call-finished", BuildFeedbackResultTracePrefix(result), apiResult.Success ? apiResult.Content : apiResult.ErrorMessage);
				if (!apiResult.Success)
				{
					result.Error = apiResult.ErrorMessage ?? "API请求失败";
					result.FailureMessages.Add("反馈批量请求失败：" + result.Error);
				}
				else
				{
					NpcPublicFeedbackResponse response = ParseFeedbackResponse(apiResult.Content, job.BatchId, apiResult.ResolvedRoute, apiResult.AttemptsUsed, "feedback-batch");
					result.Events = response?.Events ?? new List<NpcPublicFeedbackEventDto>();
					FeedbackTraceLog("feedback-batch-parse-finished", BuildFeedbackResultTracePrefix(result), "events=" + result.Events.Count.ToString(CultureInfo.InvariantCulture));
					if (result.Events.Count == 0)
					{
						result.Error = "feedback parse produced no events; raw=" + Limit(apiResult.Content, 500);
						result.FailureMessages.Add(result.Error);
					}
					else
					{
						result.Success = true;
					}
				}
				List<NpcRulerPolicyRecord> missingPolicies = GetMissingFeedbackPolicies(job.Policies, result.Events);
				if (missingPolicies.Count > 0)
				{
					string missingIds = string.Join(",", missingPolicies.Select(x => x?.PolicyId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
					result.FailureMessages.Add("Batch public feedback missed " + missingPolicies.Count.ToString(CultureInfo.InvariantCulture) + " policy target(s); weekly-report flow keeps batch result and does not start single-policy fallback. missing=" + missingIds);
					FeedbackTraceLog("feedback-batch-missing-no-fallback", BuildFeedbackResultTracePrefix(result), missingIds);
				}
				if ((result.Events ?? new List<NpcPublicFeedbackEventDto>()).Count > 0)
				{
					result.Success = true;
					result.Error = "";
				}
				else if (string.IsNullOrWhiteSpace(result.Error))
				{
					result.Error = "feedback generation produced no events";
				}
			}
		}
		catch (Exception ex)
		{
			result.Error = ex.ToString();
			result.FailureMessages.Add(result.Error);
			FeedbackTraceLog("feedback-exception", BuildFeedbackJobTracePrefix(job), ex.ToString());
		}
		finally
		{
			_pendingFeedbackCommits.Enqueue(new PendingNpcPolicyCommitContext
			{
				FeedbackResult = result
			});
			FeedbackTraceLog("feedback-commit-enqueued", BuildFeedbackResultTracePrefix(result), "queued=true");
		}
	}

	private async Task TryAppendSinglePolicyFeedbackFallbacksAsync(NpcPolicyFeedbackJob job, NpcPolicyFeedbackResult result, List<NpcRulerPolicyRecord> missingPolicies, string reason)
	{
		if (job == null || result == null || missingPolicies == null || missingPolicies.Count == 0)
		{
			return;
		}
		if (result.IsQuotaLimit || result.IsRateLimit || result.IsRequestsPerMinuteLimit)
		{
			result.FailureMessages.Add("跳过民众反馈单政策 fallback：批量请求疑似限流或额度问题。reason=" + (reason ?? ""));
			return;
		}
		List<NpcRulerPolicyRecord> targets = missingPolicies
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.PolicyId))
			.GroupBy(x => x.PolicyId.Trim(), StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.Take(12)
			.ToList();
		if (targets.Count == 0)
		{
			return;
		}
		FeedbackTraceLog("feedback-single-fallback-start", BuildFeedbackJobTracePrefix(job) + " missing=" + targets.Count.ToString(CultureInfo.InvariantCulture) + " reason=" + (reason ?? ""), string.Join("\n", targets.Select(x => x.PolicyId + " " + x.KingdomName + " " + x.PolicyName)));
		foreach (NpcRulerPolicyRecord policy in targets)
		{
			if (SaveRuntimeGuard.IsStale(job.RuntimeGeneration, "npc_policy_feedback_single_fallback"))
			{
				result.FailureMessages.Add("民众反馈单政策 fallback 被丢弃：存档运行代际已变化。");
				return;
			}
			if (HasFeedbackEventForPolicy(result.Events, policy))
			{
				continue;
			}
			NpcPolicyPrompt prompt = BuildFeedbackPrompt(new List<NpcRulerPolicyRecord> { policy }, job.CompactWorldContext);
			NpcPolicyApiCallResult apiResult = await NpcPolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(prompt.SystemPrompt, prompt.UserPrompt, job.MaxTokens, job.HardTimeoutMilliseconds, "NpcPublicFeedbackSingleFallback", job.RuntimeGeneration, 3);
			CopyApiResultToFeedbackResult(result, apiResult, accumulateAttempts: true);
			FeedbackTraceLog("feedback-single-fallback-finished", BuildFeedbackJobTracePrefix(job) + " policy=" + (policy.PolicyId ?? "") + " success=" + apiResult.Success.ToString(CultureInfo.InvariantCulture) + " attempts=" + apiResult.AttemptsUsed.ToString(CultureInfo.InvariantCulture), apiResult.Success ? apiResult.Content : apiResult.ErrorMessage);
			if (!apiResult.Success)
			{
				result.FallbackFailureCount++;
				result.FailureMessages.Add("民众反馈单政策 fallback 失败：" + FirstNonEmpty(policy.PolicyName, policy.KingdomName, policy.PolicyId) + " - " + (apiResult.ErrorMessage ?? "未知错误"));
				if (apiResult.IsQuotaLimit || apiResult.IsRateLimit || apiResult.IsRequestsPerMinuteLimit)
				{
					result.FailureMessages.Add("民众反馈 fallback 因限流/额度问题提前停止，避免继续刷请求。");
					return;
				}
				continue;
			}
			NpcPublicFeedbackResponse response = ParseFeedbackResponse(apiResult.Content, job.BatchId, apiResult.ResolvedRoute, apiResult.AttemptsUsed, "feedback-single-fallback:" + (policy.PolicyId ?? ""));
			List<NpcPublicFeedbackEventDto> events = response?.Events ?? new List<NpcPublicFeedbackEventDto>();
			NpcPublicFeedbackEventDto dto = events.FirstOrDefault(x => x != null);
			if (dto == null)
			{
				result.FallbackFailureCount++;
				result.FailureMessages.Add("民众反馈单政策 fallback 解析为空：" + FirstNonEmpty(policy.PolicyName, policy.KingdomName, policy.PolicyId));
				continue;
			}
			dto.PolicyId = FirstNonEmpty(dto.PolicyId, policy.PolicyId);
			dto.KingdomId = FirstNonEmpty(dto.KingdomId, policy.KingdomId);
			dto.KingdomName = FirstNonEmpty(dto.KingdomName, policy.KingdomName);
			dto.PolicyName = FirstNonEmpty(dto.PolicyName, policy.PolicyName);
			result.Events = result.Events ?? new List<NpcPublicFeedbackEventDto>();
			if (!HasFeedbackEventForPolicy(result.Events, policy))
			{
				result.Events.Add(dto);
				result.FallbackSuccessCount++;
			}
		}
	}

	private void ProcessPendingFeedbackCommits()
	{
		long startTimestamp = Stopwatch.GetTimestamp();
		double budgetMs = FeedbackCommitFrameBudgetMs;
		while (!IsFeedbackCommitBudgetExceeded(startTimestamp, budgetMs) && _pendingFeedbackCommits.TryPeek(out PendingNpcPolicyCommitContext context))
		{
			if (!ProcessPendingFeedbackCommitContext(context, startTimestamp, budgetMs))
			{
				return;
			}
			_pendingFeedbackCommits.TryDequeue(out var _);
		}
	}

	private bool ProcessPendingFeedbackCommitContext(PendingNpcPolicyCommitContext context, long startTimestamp, double budgetMs)
	{
		if (context == null)
		{
			return true;
		}
		NpcPolicyFeedbackResult result = context.FeedbackResult;
		NpcPolicyFeedbackJob job = result?.Job;
		try
		{
			if (job == null)
			{
				Log("feedback-commit-discard reason=missing-job");
				return true;
			}
			if (job.Version != _feedbackGenerationVersion)
			{
				Log("feedback-stale version=" + job.Version.ToString(CultureInfo.InvariantCulture));
				return true;
			}
			if (SaveRuntimeGuard.IsStale(job.RuntimeGeneration, "npc_policy_feedback_commit"))
			{
				if (TryFinalizeFeedbackGenerationState(job.InFlightKey, job.Version, "stale-runtime"))
				{
					DiscardPendingFeedbackRequestForRuntime(job.RuntimeGeneration, "stale-runtime");
					Log("feedback-discard reason=stale-runtime batch=" + (job.BatchId ?? ""));
					TryStartPendingFeedbackGeneration("stale-runtime");
				}
				return true;
			}
			if (!IsCampaignSessionReady())
			{
				TryFinalizeFeedbackGenerationState(job.InFlightKey, job.Version, "campaign-not-ready");
				Log("feedback-discard batch=" + (job.BatchId ?? "") + " reason=campaign-not-ready");
				return true;
			}
			if (result == null || !result.Success)
			{
				FinalizeFeedbackFailure(result, result?.Error ?? "unknown feedback generation error");
				return true;
			}
			List<NpcPublicFeedbackEventDto> events = result.Events ?? new List<NpcPublicFeedbackEventDto>();
			while (context.EventIndex < events.Count
				&& context.EventsCommittedThisTick < FeedbackCommitMaxEventsPerTick
				&& !IsFeedbackCommitBudgetExceeded(startTimestamp, budgetMs))
			{
				NpcPublicFeedbackEventDto dto = events[context.EventIndex];
				CommitFeedbackEvent(job, dto, context.EventIndex, ref context.RecordedCount);
				context.EventIndex++;
				context.EventsCommittedThisTick++;
			}
			context.EventsCommittedThisTick = 0;
			if (context.EventIndex < events.Count)
			{
				return false;
			}
			if (!TryFinalizeFeedbackGenerationState(job.InFlightKey, job.Version, "completed"))
			{
				return true;
			}
			_lastFeedbackRetryCount = 0;
			if (result.FailureMessages != null && result.FailureMessages.Count > 0)
			{
				_lastFeedbackError = Limit(string.Join(" | ", result.FailureMessages), 800);
				_lastFeedbackRetryContext = CreateFeedbackRetryContext(job, result, _lastFeedbackError);
				FeedbackTraceLog("feedback-partial-failures", BuildFeedbackResultTracePrefix(result), string.Join("\n", result.FailureMessages));
			}
			else
			{
				_lastFeedbackError = "";
				_lastFeedbackRetryContext = null;
			}
			Log("feedback-complete batch=" + (job.BatchId ?? "") + " parsed=" + events.Count.ToString(CultureInfo.InvariantCulture) + " recorded=" + context.RecordedCount.ToString(CultureInfo.InvariantCulture) + " attempts=" + result.AttemptsUsed.ToString(CultureInfo.InvariantCulture) + " fallbackSuccess=" + result.FallbackSuccessCount.ToString(CultureInfo.InvariantCulture) + " fallbackFailures=" + result.FallbackFailureCount.ToString(CultureInfo.InvariantCulture));
			FeedbackTraceLog("feedback-commit-complete", BuildFeedbackResultTracePrefix(result), "recorded=" + context.RecordedCount.ToString(CultureInfo.InvariantCulture));
			TryStartPendingFeedbackGeneration("completed");
			return true;
		}
		catch (Exception ex)
		{
			FinalizeFeedbackFailure(result ?? new NpcPolicyFeedbackResult { Job = job }, ex.ToString());
			Log("feedback-commit-exception " + ex);
			return true;
		}
	}

	private void CommitFeedbackEvent(NpcPolicyFeedbackJob job, NpcPublicFeedbackEventDto dto, int index, ref int recorded)
	{
		if (job == null || dto == null)
		{
			return;
		}
		NpcRulerPolicyRecord policy = MatchPolicy(job.Policies, dto);
		string kingdomId = FirstNonEmpty(dto.KingdomId, policy?.KingdomId);
		string kingdomName = FirstNonEmpty(dto.KingdomName, policy?.KingdomName);
		string eventId = "npc_public_feedback:" + NormalizeKeyPart(job.BatchId) + ":" + NormalizeKeyPart(kingdomId) + ":" + index.ToString(CultureInfo.InvariantCulture);
		string title = FirstNonEmpty(dto.EventTitle, kingdomName + "民众反馈");
		string text = FirstNonEmpty(dto.EventText, dto.Summary);
		if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		AnimusForgeWorldEventInboxEntry entry = new AnimusForgeWorldEventInboxEntry
		{
			EventId = eventId,
			EventKind = "npc_public_feedback",
			Title = title,
			Summary = Limit(text, MaxSummaryChars),
			DetailText = text,
			KingdomId = kingdomId,
			KingdomName = kingdomName,
			ActorHeroId = policy?.RulerHeroId ?? "",
			ActorHeroName = policy?.RulerName ?? "",
			PolicyId = FirstNonEmpty(dto.PolicyId, policy?.PolicyId),
			PolicyName = FirstNonEmpty(dto.PolicyName, policy?.PolicyName),
			Day = Math.Max(0, policy?.Day ?? GetCurrentCampaignDay()),
			GameDate = FirstNonEmpty(policy?.GameDate, FormatCurrentCampaignDate()),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			StableKey = eventId,
			IsRead = false
		};
		UpsertWorldEventInternal(entry, markUnread: true);
		MyBehavior.RecordNpcPublicFeedbackEventMaterialForExternal(entry.EventId, entry.Title, entry.Summary, entry.DetailText, entry.KingdomId, entry.KingdomName, entry.ActorHeroId, entry.ActorHeroName, entry.PolicyId, entry.PolicyName, entry.Day, entry.GameDate, includeInWorld: true);
		recorded++;
	}

	private void FinalizeFeedbackFailure(NpcPolicyFeedbackResult result, string error)
	{
		NpcPolicyFeedbackJob job = result?.Job;
		if (job == null || !TryFinalizeFeedbackGenerationState(job.InFlightKey, job.Version, "failed"))
		{
			return;
		}
		_lastFeedbackRetryCount = Math.Max(0, result?.AttemptsUsed ?? 0);
		_lastFeedbackError = Limit(error ?? "未知错误", 800);
		_lastFeedbackRetryContext = CreateFeedbackRetryContext(job, result, _lastFeedbackError);
		Log("feedback-failed batch=" + (job.BatchId ?? "") + " attempts=" + _lastFeedbackRetryCount.ToString(CultureInfo.InvariantCulture) + " rateLimit=" + (result?.IsRateLimit ?? false).ToString(CultureInfo.InvariantCulture) + " rpm=" + (result?.IsRequestsPerMinuteLimit ?? false).ToString(CultureInfo.InvariantCulture) + " quota=" + (result?.IsQuotaLimit ?? false).ToString(CultureInfo.InvariantCulture) + " authFailure=" + (result?.IsAuthFailure ?? false).ToString(CultureInfo.InvariantCulture) + " retryAfter=" + ((result?.RetryAfterSeconds)?.ToString(CultureInfo.InvariantCulture) ?? "") + " rawRetryAfter=" + ((result?.RetryAfterSecondsRaw)?.ToString(CultureInfo.InvariantCulture) ?? "") + " retryAfterCapped=" + ((result?.RetryAfterSecondsCapped ?? false) ? "true" : "false") + " error=" + Limit(_lastFeedbackError, 300));
		FeedbackTraceLog("feedback-failed", BuildFeedbackResultTracePrefix(result), _lastFeedbackError + "\n\n" + string.Join("\n", result?.FailureMessages ?? new List<string>()));
		DiscardPendingFeedbackRequestForRuntime(job.RuntimeGeneration, "failed");
	}

	private static void CopyApiResultToFeedbackResult(NpcPolicyFeedbackResult result, NpcPolicyApiCallResult apiResult, bool accumulateAttempts)
	{
		if (result == null || apiResult == null)
		{
			return;
		}
		if (accumulateAttempts)
		{
			result.AttemptsUsed += Math.Max(0, apiResult.AttemptsUsed);
			result.FallbackAttemptsUsed += Math.Max(0, apiResult.AttemptsUsed);
		}
		else
		{
			result.AttemptsUsed = Math.Max(0, apiResult.AttemptsUsed);
			result.BatchAttemptsUsed = Math.Max(0, apiResult.AttemptsUsed);
		}
		result.RawResponse = apiResult.Content ?? result.RawResponse ?? "";
		result.IsRateLimit = result.IsRateLimit || apiResult.IsRateLimit;
		result.IsRequestsPerMinuteLimit = result.IsRequestsPerMinuteLimit || apiResult.IsRequestsPerMinuteLimit;
		result.IsQuotaLimit = result.IsQuotaLimit || apiResult.IsQuotaLimit;
		result.IsAuthFailure = result.IsAuthFailure || apiResult.IsAuthFailure;
		result.RetryAfterSeconds = MaxNullable(result.RetryAfterSeconds, apiResult.RetryAfterSeconds);
		result.RetryAfterSecondsRaw = MaxNullable(result.RetryAfterSecondsRaw, apiResult.RetryAfterSecondsRaw);
		result.RetryAfterSecondsCapped = result.RetryAfterSecondsCapped || apiResult.RetryAfterSecondsCapped;
	}

	private static int? MaxNullable(int? a, int? b)
	{
		if (!a.HasValue)
		{
			return b;
		}
		if (!b.HasValue)
		{
			return a;
		}
		return Math.Max(a.Value, b.Value);
	}

	private static List<NpcRulerPolicyRecord> GetMissingFeedbackPolicies(List<NpcRulerPolicyRecord> policies, List<NpcPublicFeedbackEventDto> events)
	{
		return (policies ?? new List<NpcRulerPolicyRecord>())
			.Where(policy => policy != null && !HasFeedbackEventForPolicy(events, policy))
			.ToList();
	}

	private static bool HasFeedbackEventForPolicy(List<NpcPublicFeedbackEventDto> events, NpcRulerPolicyRecord policy)
	{
		if (policy == null)
		{
			return false;
		}
		string policyId = (policy.PolicyId ?? "").Trim();
		string kingdomId = (policy.KingdomId ?? "").Trim();
		string policyName = (policy.PolicyName ?? "").Trim();
		return (events ?? new List<NpcPublicFeedbackEventDto>()).Any(dto =>
			dto != null
			&& ((!string.IsNullOrWhiteSpace(kingdomId) && string.Equals((dto.KingdomId ?? "").Trim(), kingdomId, StringComparison.OrdinalIgnoreCase))
				|| (!string.IsNullOrWhiteSpace(policyName) && string.Equals((dto.PolicyName ?? "").Trim(), policyName, StringComparison.OrdinalIgnoreCase))
				|| (!string.IsNullOrWhiteSpace(policyId) && string.Equals((dto.PolicyId ?? "").Trim(), policyId, StringComparison.OrdinalIgnoreCase))));
	}

	private static NpcFeedbackRetryContext CreateFeedbackRetryContext(NpcPolicyFeedbackJob job, NpcPolicyFeedbackResult result, string reason)
	{
		NpcFeedbackRetryContext context = new NpcFeedbackRetryContext
		{
			BatchId = job?.BatchId ?? "",
			FailedReason = Limit(reason ?? "", 800),
			AttemptsUsed = Math.Max(0, result?.AttemptsUsed ?? 0),
			IsRateLimit = result?.IsRateLimit ?? false,
			IsRequestsPerMinuteLimit = result?.IsRequestsPerMinuteLimit ?? false,
			IsQuotaLimit = result?.IsQuotaLimit ?? false,
			IsAuthFailure = result?.IsAuthFailure ?? false,
			RetryAfterSeconds = result?.RetryAfterSeconds,
			FallbackSuccessCount = Math.Max(0, result?.FallbackSuccessCount ?? 0),
			FallbackFailureCount = Math.Max(0, result?.FallbackFailureCount ?? 0)
		};
		foreach (NpcRulerPolicyRecord policy in GetMissingFeedbackPolicies(job?.Policies, result?.Events))
		{
			string id = (policy?.PolicyId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(id) && !context.FailedPolicyIds.Contains(id, StringComparer.OrdinalIgnoreCase))
			{
				context.FailedPolicyIds.Add(id);
			}
		}
		foreach (string message in result?.FailureMessages ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(message))
			{
				context.FailureMessages.Add(Limit(message, 500));
			}
		}
		return context;
	}

	private static bool IsFeedbackCommitBudgetExceeded(long startTimestamp, double budgetMs)
	{
		if (budgetMs <= 0.0)
		{
			return false;
		}
		double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		return elapsedMs >= budgetMs;
	}

	private static void FeedbackTraceLog(string stage, string message, string detail = null)
	{
		NpcPolicyDetailedTraceLog.Write(stage, message, detail);
	}

	private static string BuildFeedbackJobTracePrefix(NpcPolicyFeedbackJob job)
	{
		if (job == null)
		{
			return "job=null";
		}
		return "job=" + (job.JobId ?? "")
			+ " batch=" + (job.BatchId ?? "")
			+ " policies=" + ((job.Policies?.Count) ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " version=" + job.Version.ToString(CultureInfo.InvariantCulture);
	}

	private static string BuildFeedbackResultTracePrefix(NpcPolicyFeedbackResult result)
	{
		if (result == null)
		{
			return "result=null";
		}
		return BuildFeedbackJobTracePrefix(result.Job)
			+ " success=" + result.Success.ToString(CultureInfo.InvariantCulture)
			+ " events=" + ((result.Events?.Count) ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " attempts=" + result.AttemptsUsed.ToString(CultureInfo.InvariantCulture)
			+ " authFailure=" + result.IsAuthFailure.ToString(CultureInfo.InvariantCulture)
			+ " retryAfter=" + (result.RetryAfterSeconds?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " rawRetryAfter=" + (result.RetryAfterSecondsRaw?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " retryAfterCapped=" + (result.RetryAfterSecondsCapped ? "true" : "false")
			+ " fallbackSuccess=" + result.FallbackSuccessCount.ToString(CultureInfo.InvariantCulture)
			+ " fallbackFailures=" + result.FallbackFailureCount.ToString(CultureInfo.InvariantCulture);
	}

	private List<AnimusForgeWorldEventInboxEntry> GetInboxSnapshotInternal(int maxCount)
	{
		int limit = Math.Max(1, Math.Min(200, maxCount <= 0 ? 80 : maxCount));
		return _eventRecords.Values
			.Select(DeserializeEntry)
			.Where(x => x != null)
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(limit)
			.Select(CloneEntry)
			.ToList();
	}

	private void UpsertWorldEventInternal(AnimusForgeWorldEventInboxEntry entry, bool markUnread)
	{
		AnimusForgeWorldEventInboxEntry normalized = NormalizeEntry(entry);
		if (normalized == null || string.IsNullOrWhiteSpace(normalized.EventId))
		{
			return;
		}
		string eventId = normalized.EventId.Trim();
		if (_eventRecords.TryGetValue(eventId, out string raw) && !string.IsNullOrWhiteSpace(raw))
		{
			AnimusForgeWorldEventInboxEntry old = DeserializeEntry(raw);
			if (old != null && old.IsRead && !markUnread)
			{
				normalized.IsRead = true;
			}
		}
		if (markUnread)
		{
			normalized.IsRead = false;
			_unreadEventIds.Add(eventId);
		}
		else if (normalized.IsRead)
		{
			_unreadEventIds.Remove(eventId);
		}
		_eventRecords[eventId] = JsonConvert.SerializeObject(normalized);
		TrimEventRecords();
		_version++;
	}

	private bool MarkEventReadInternal(string eventId)
	{
		string id = (eventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_eventRecords.TryGetValue(id, out string raw))
		{
			return false;
		}
		AnimusForgeWorldEventInboxEntry entry = DeserializeEntry(raw);
		if (entry == null)
		{
			return false;
		}
		if (entry.IsRead)
		{
			_unreadEventIds.Remove(id);
			return false;
		}
		entry.IsRead = true;
		_eventRecords[id] = JsonConvert.SerializeObject(entry);
		_unreadEventIds.Remove(id);
		_version++;
		return true;
	}

	private void MarkAllReadInternal()
	{
		foreach (string id in _eventRecords.Keys.ToList())
		{
			AnimusForgeWorldEventInboxEntry entry = DeserializeEntry(_eventRecords[id]);
			if (entry == null)
			{
				continue;
			}
			entry.IsRead = true;
			_eventRecords[id] = JsonConvert.SerializeObject(entry);
		}
		_unreadEventIds.Clear();
		_version++;
	}

	private string BuildRecentPublicFeedbackEventContextForKingdomInternal(string kingdomId, int maxItems)
	{
		string id = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return "";
		}
		List<AnimusForgeWorldEventInboxEntry> list = _eventRecords.Values
			.Select(DeserializeEntry)
			.Where(x => x != null && string.Equals((x.KingdomId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase)
				&& (string.Equals((x.EventKind ?? "").Trim(), "npc_public_feedback", StringComparison.OrdinalIgnoreCase)
					|| string.Equals((x.EventKind ?? "").Trim(), "npc_ruler_policy_event", StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(Math.Max(1, Math.Min(6, maxItems <= 0 ? MaxFeedbackContextItems : maxItems)))
			.ToList();
		if (list.Count == 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【近期政策衍生事件】");
		foreach (AnimusForgeWorldEventInboxEntry item in list)
		{
			sb.Append("- ").Append(string.IsNullOrWhiteSpace(item.GameDate) ? ("第" + item.Day.ToString(CultureInfo.InvariantCulture) + "天") : item.GameDate.Trim())
				.Append(" ").Append(Limit(FirstNonEmpty(item.Title, item.PolicyName, "政策余波"), 42));
			string summary = Limit(FirstNonEmpty(item.Summary, item.DetailText), 100);
			if (!string.IsNullOrWhiteSpace(summary))
			{
				sb.Append("：").Append(summary);
			}
			sb.AppendLine();
		}
		return sb.ToString().Trim();
	}

	private void TrimEventRecords()
	{
		List<AnimusForgeWorldEventInboxEntry> ordered = _eventRecords.Values.Select(DeserializeEntry).Where(x => x != null)
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.ToList();
		foreach (AnimusForgeWorldEventInboxEntry extra in ordered.Skip(MaxEventRecordCount).ToList())
		{
			string id = (extra.EventId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(id))
			{
				_eventRecords.Remove(id);
				_unreadEventIds.Remove(id);
			}
		}
	}

	private static NpcPolicyPrompt BuildFeedbackPrompt(List<NpcRulerPolicyRecord> policies, string compactWorldContext)
	{
		StringBuilder user = new StringBuilder();
		user.AppendLine("【世界与王国简表】");
		user.AppendLine(string.IsNullOrWhiteSpace(compactWorldContext) ? "（无额外上下文）" : compactWorldContext.Trim());
		user.AppendLine();
		user.AppendLine("【刚发布的统治者政策】");
		foreach (NpcRulerPolicyRecord policy in policies)
		{
			user.AppendLine("- policyId=" + (policy.PolicyId ?? "") + "; kingdomId=" + (policy.KingdomId ?? "") + "; kingdom=" + (policy.KingdomName ?? "") + "; ruler=" + (policy.RulerName ?? "") + "; policy=" + (policy.PolicyName ?? ""));
			user.AppendLine("  内容：" + Limit(policy.PolicyContent ?? "", 180));
			user.AppendLine("  每日影响：" + Limit(policy.ImpactSummary ?? "", 160));
		}
		user.AppendLine();
		user.AppendLine("请只输出 JSON 对象：{\"events\":[...]}。events 数量尽量与政策数量一致。每个 event 包含 policyId、kingdomId、kingdomName、policyName、eventTitle、eventText。eventText 是第三人称民众反馈事件，可写街市、军营、村庄、贵族厅堂或商队如何议论，但不要输出政策数值，不要编造不存在的具体人物。不要 Markdown。依据政策分别生成，不要合并。每条 80-180 个中文字符。 ");
		string system = "你是卡拉迪亚大陆民众反馈事件撰写器。你只负责把统治者政策转成可记录的民间事件，不负责决定政策数值。政策与民众反馈必须分开：不要修改政策，不要输出每日效果，只输出 JSON。";
		return new NpcPolicyPrompt
		{
			SystemPrompt = system,
			UserPrompt = user.ToString()
		};
	}

	private static NpcPublicFeedbackResponse ParseFeedbackResponse(string raw)
	{
		return ParseFeedbackResponse(raw, "", "", 0, "feedback");
	}

	private static NpcPublicFeedbackResponse ParseFeedbackResponse(string raw, string batchId, string route, int attempts, string parseSource)
	{
		string json = "";
		try
		{
			json = ExtractJsonObject(raw);
			if (string.IsNullOrWhiteSpace(json))
			{
				NpcPolicyStructuredParseLogger.LogFailure("NpcPublicFeedbackEvent", "feedback", batchId, route, attempts, FirstNonEmpty(parseSource, "feedback") + ":no-json", raw, json);
				return null;
			}
			NpcPublicFeedbackResponse response = JsonConvert.DeserializeObject<NpcPublicFeedbackResponse>(json);
			if ((response?.Events?.Count ?? 0) == 0)
			{
				NpcPolicyStructuredParseLogger.LogFailure("NpcPublicFeedbackEvent", "feedback", batchId, route, attempts, FirstNonEmpty(parseSource, "feedback") + ":no-events", raw, json);
			}
			return response;
		}
		catch (Exception ex)
		{
			NpcPolicyStructuredParseLogger.LogFailure("NpcPublicFeedbackEvent", "feedback", batchId, route, attempts, FirstNonEmpty(parseSource, "feedback") + ":" + ex.GetType().Name + ":" + ex.Message, raw, json);
			return null;
		}
	}

	private static string ExtractJsonObject(string text)
	{
		text = (text ?? "").Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			text = System.Text.RegularExpressions.Regex.Replace(text, "^```(?:json)?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
			text = System.Text.RegularExpressions.Regex.Replace(text, "```$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
		}
		int start = text.IndexOf('{');
		int end = text.LastIndexOf('}');
		return start >= 0 && end > start ? text.Substring(start, end - start + 1) : "";
	}

	private static NpcRulerPolicyRecord MatchPolicy(List<NpcRulerPolicyRecord> policies, NpcPublicFeedbackEventDto dto)
	{
		if (policies == null || dto == null)
		{
			return null;
		}
		string policyId = (dto.PolicyId ?? "").Trim();
		string kingdomId = (dto.KingdomId ?? "").Trim();
		string policyName = (dto.PolicyName ?? "").Trim();
		return policies.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(policyId) && string.Equals((x.PolicyId ?? "").Trim(), policyId, StringComparison.OrdinalIgnoreCase))
			?? policies.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(kingdomId) && string.Equals((x.KingdomId ?? "").Trim(), kingdomId, StringComparison.OrdinalIgnoreCase))
			?? policies.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(policyName) && string.Equals((x.PolicyName ?? "").Trim(), policyName, StringComparison.OrdinalIgnoreCase));
	}

	private static AnimusForgeWorldEventInboxEntry NormalizeEntry(AnimusForgeWorldEventInboxEntry entry)
	{
		if (entry == null)
		{
			return null;
		}
		string id = (entry.EventId ?? entry.StableKey ?? Guid.NewGuid().ToString("N")).Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		return new AnimusForgeWorldEventInboxEntry
		{
			Version = Math.Max(1, entry.Version),
			EventId = id,
			EventKind = Limit((entry.EventKind ?? "world_event").Trim(), 60),
			EventType = Limit((entry.EventType ?? "").Trim(), 40),
			Title = Limit(FirstNonEmpty(entry.Title, "AnimusForge 事件"), MaxTitleChars),
			Summary = Limit(FirstNonEmpty(entry.Summary, entry.DetailText), MaxSummaryChars),
			DetailText = Limit(FirstNonEmpty(entry.DetailText, entry.Summary), MaxDetailChars),
			KingdomId = Limit((entry.KingdomId ?? "").Trim(), 80),
			KingdomName = Limit((entry.KingdomName ?? "").Trim(), 80),
			ActorHeroId = Limit((entry.ActorHeroId ?? "").Trim(), 80),
			ActorHeroName = Limit((entry.ActorHeroName ?? "").Trim(), 80),
			PolicyId = Limit((entry.PolicyId ?? "").Trim(), 120),
			PolicyName = Limit((entry.PolicyName ?? "").Trim(), 90),
			Day = Math.Max(0, entry.Day <= 0 ? GetCurrentCampaignDay() : entry.Day),
			GameDate = Limit(FirstNonEmpty(entry.GameDate, FormatCurrentCampaignDate()), 40),
			CreatedUtcTicks = entry.CreatedUtcTicks > 0L ? entry.CreatedUtcTicks : DateTime.UtcNow.Ticks,
			StableKey = Limit(FirstNonEmpty(entry.StableKey, id), 180),
			IsRead = entry.IsRead
		};
	}

	private static AnimusForgeWorldEventInboxEntry CloneEntry(AnimusForgeWorldEventInboxEntry entry)
	{
		return NormalizeEntry(entry);
	}

	private static NpcRulerPolicyRecord ClonePolicyRecord(NpcRulerPolicyRecord policy)
	{
		if (policy == null)
		{
			return null;
		}
		return new NpcRulerPolicyRecord
		{
			Version = policy.Version,
			PolicyId = policy.PolicyId,
			BatchId = policy.BatchId,
			KingdomId = policy.KingdomId,
			KingdomName = policy.KingdomName,
			RulerHeroId = policy.RulerHeroId,
			RulerName = policy.RulerName,
			PolicyName = policy.PolicyName,
			PolicyContent = policy.PolicyContent,
			PublicFeedback = policy.PublicFeedback,
			FeedbackTitle = policy.FeedbackTitle,
			EventType = policy.EventType,
			ImpactSummary = policy.ImpactSummary,
			Day = policy.Day,
			GameDate = policy.GameDate
		};
	}

	private static AnimusForgeWorldEventInboxEntry DeserializeEntry(string raw)
	{
		try
		{
			return string.IsNullOrWhiteSpace(raw) ? null : NormalizeEntry(JsonConvert.DeserializeObject<AnimusForgeWorldEventInboxEntry>(raw));
		}
		catch
		{
			return null;
		}
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}

	private static string NormalizeKeyPart(string text)
	{
		text = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "none";
		}
		StringBuilder sb = new StringBuilder();
		foreach (char ch in text)
		{
			if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')
			{
				sb.Append(ch);
			}
		}
		return sb.Length == 0 ? "x" : sb.ToString();
	}

	private static string Limit(string text, int maxChars)
	{
		text = (text ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, maxChars).TrimEnd() + "…";
	}

	private static bool IsCampaignSessionReady()
	{
		return Campaign.Current != null;
	}

	internal static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	internal static string FormatCurrentCampaignDate()
	{
		try
		{
			return CampaignTime.Now.ToString();
		}
		catch
		{
			return "第" + GetCurrentCampaignDay().ToString(CultureInfo.InvariantCulture) + "天";
		}
	}

	private static void Log(string message)
	{
		try
		{
			Logger.Log("NpcPublicFeedbackEvent", message ?? "");
		}
		catch
		{
		}
	}

	private sealed class NpcPolicyFeedbackJob
	{
		public string JobId = "";
		public string BatchId = "";
		public List<NpcRulerPolicyRecord> Policies = new List<NpcRulerPolicyRecord>();
		public string CompactWorldContext = "";
		public string SystemPrompt = "";
		public string UserPrompt = "";
		public string PromptPreview = "";
		public string InFlightKey = "";
		public int Version;
		public long RuntimeGeneration;
		public int MaxTokens;
		public int HardTimeoutMilliseconds;
		public long CreatedUtcTicks;
	}

	private sealed class NpcPolicyFeedbackResult
	{
		public NpcPolicyFeedbackJob Job;
		public bool Success;
		public string RawResponse = "";
		public string Error = "";
		public List<NpcPublicFeedbackEventDto> Events = new List<NpcPublicFeedbackEventDto>();
		public List<string> FailureMessages = new List<string>();
		public int AttemptsUsed;
		public int BatchAttemptsUsed;
		public int FallbackAttemptsUsed;
		public int FallbackSuccessCount;
		public int FallbackFailureCount;
		public bool IsRateLimit;
		public bool IsRequestsPerMinuteLimit;
		public bool IsQuotaLimit;
		public bool IsAuthFailure;
		public int? RetryAfterSeconds;
		public int? RetryAfterSecondsRaw;
		public bool RetryAfterSecondsCapped;
	}

	private sealed class NpcFeedbackRetryContext
	{
		public string BatchId = "";
		public string FailedReason = "";
		public int AttemptsUsed;
		public bool IsRateLimit;
		public bool IsRequestsPerMinuteLimit;
		public bool IsQuotaLimit;
		public bool IsAuthFailure;
		public int? RetryAfterSeconds;
		public int FallbackSuccessCount;
		public int FallbackFailureCount;
		public List<string> FailedPolicyIds = new List<string>();
		public List<string> FailureMessages = new List<string>();
	}

	private sealed class PendingFeedbackGenerationRequest
	{
		public string BatchId = "";
		public string InFlightKey = "";
		public string RequestFingerprint = "";
		public long RuntimeGeneration;
		public List<NpcRulerPolicyRecord> Policies = new List<NpcRulerPolicyRecord>();
		public string CompactWorldContext = "";
	}

	private sealed class PendingNpcPolicyCommitContext
	{
		public NpcPolicyFeedbackResult FeedbackResult;
		public int EventIndex;
		public int RecordedCount;
		public int EventsCommittedThisTick;
	}

	private sealed class NpcPublicFeedbackResponse
	{
		[JsonProperty("events")]
		public List<NpcPublicFeedbackEventDto> Events { get; set; }
	}

	private sealed class NpcPublicFeedbackEventDto
	{
		[JsonProperty("policyId")]
		public string PolicyId { get; set; }

		[JsonProperty("kingdomId")]
		public string KingdomId { get; set; }

		[JsonProperty("kingdomName")]
		public string KingdomName { get; set; }

		[JsonProperty("policyName")]
		public string PolicyName { get; set; }

		[JsonProperty("eventTitle")]
		public string EventTitle { get; set; }

		[JsonProperty("eventText")]
		public string EventText { get; set; }

		[JsonProperty("summary")]
		public string Summary { get; set; }
	}
}

// Bridge into CustomPolicyBehavior daily active-effect ledger.
public sealed partial class CustomPolicyBehavior
{

	public static void CreateNpcRulerPolicyActiveEffectsForExternal(List<NpcRulerPolicyRecord> records)
	{
		try
		{
			if (Campaign.Current == null)
			{
				PolicyDebugLog("npc-ruler-policy-active-effects-skip", "Campaign.Current 为空，跳过 NPC 政策落地");
				return;
			}
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				PolicyDebugLog("npc-ruler-policy-active-effects-skip", "CustomPolicyBehavior 未注册");
				return;
			}
			int created = 0;
			foreach (NpcRulerPolicyRecord policy in records ?? new List<NpcRulerPolicyRecord>())
			{
				List<NpcRulerPolicyEffectDto> effects = policy?.Effects ?? new List<NpcRulerPolicyEffectDto>();
				for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
				{
					if (behavior.TryCreateNpcRulerPolicyActiveEffectInternal(policy, effects[effectIndex], effectIndex, out var _, out var _))
					{
						created++;
					}
				}
			}
			PolicyDebugLog("npc-ruler-policy-active-effects-created", "records=" + (records?.Count ?? 0).ToString(CultureInfo.InvariantCulture) + " created=" + created.ToString(CultureInfo.InvariantCulture));
		}
		catch (Exception ex)
		{
			PolicyDebugLog("npc-ruler-policy-active-effects-exception", ex.ToString());
		}
	}
	public static bool TryCreateNpcRulerPolicyActiveEffectForExternal(NpcRulerPolicyRecord policy, NpcRulerPolicyEffectDto effect, out string effectId, out string failureReason)
	{
		effectId = "";
		failureReason = "";
		try
		{
			if (Campaign.Current == null)
			{
				failureReason = "Campaign.Current 为空";
				return false;
			}
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "CustomPolicyBehavior 未注册";
				return false;
			}
			int effectIndex = ResolveNpcRulerPolicyEffectIndex(policy, effect);
			bool created = behavior.TryCreateNpcRulerPolicyActiveEffectInternal(policy, effect, effectIndex, out effectId, out failureReason);
			bool alreadyActive = !created && (failureReason ?? "").StartsWith("重复 NPC 政策效果", StringComparison.Ordinal);
			int remainingDays = created || alreadyActive ? Math.Max(0, effect?.DurationDays ?? 0) : 0;
			bool isEnded = !created && !alreadyActive;
			if (effect != null)
			{
				effect.EffectId = effectId ?? "";
				effect.RemainingDays = remainingDays;
				effect.IsEnded = isEnded;
			}
			NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(policy?.PolicyId, effectId, effect?.TargetKingdomId, remainingDays, isEnded);
			return created;
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			PolicyDebugLog("npc-ruler-policy-active-effect-exception", ex.ToString());
			return false;
		}
	}

	private bool TryCreateNpcRulerPolicyActiveEffectInternal(NpcRulerPolicyRecord policy, NpcRulerPolicyEffectDto effect, out string effectId, out string failureReason)
	{
		int effectIndex = ResolveNpcRulerPolicyEffectIndex(policy, effect);
		return TryCreateNpcRulerPolicyActiveEffectInternal(policy, effect, effectIndex, out effectId, out failureReason);
	}

	private bool TryCreateNpcRulerPolicyActiveEffectInternal(NpcRulerPolicyRecord policy, NpcRulerPolicyEffectDto effect, int effectIndex, out string effectId, out string failureReason)
	{
		effectId = "";
		failureReason = "";
		if (!TryBuildNpcRulerPolicyActiveEffectContext(policy, effect, effectIndex, out NpcRulerPolicyActiveEffectBuildContext context, out failureReason))
		{
			return false;
		}
		effectId = context.EffectId;
		if (TryFindExistingNpcRulerPolicyActiveEffect(context, out var _))
		{
			failureReason = "重复 NPC 政策效果: " + context.EffectId;
			LogNpcRulerPolicyDuplicateActiveEffect(context);
			return false;
		}
		ActivePolicyEffectSaveData activeEffect = CreateNpcRulerPolicyActiveEffectSaveData(context);
		_activePolicyEffects[activeEffect.EffectId] = JsonConvert.SerializeObject(activeEffect);
		LogNpcRulerPolicyActiveEffectCreated(context, activeEffect);
		return true;
	}

	private bool TryBuildNpcRulerPolicyActiveEffectContext(NpcRulerPolicyRecord policy, NpcRulerPolicyEffectDto effect, int effectIndex, out NpcRulerPolicyActiveEffectBuildContext context, out string failureReason)
	{
		context = null;
		failureReason = "";
		if (policy == null)
		{
			failureReason = "policy 为空";
			return false;
		}
		if (effect == null)
		{
			failureReason = "effect 为空";
			return false;
		}
		if (Campaign.Current == null)
		{
			failureReason = "Campaign.Current 为空";
			return false;
		}
		Kingdom target = ResolveKingdomByIdOrName(effect.TargetKingdomId, effect.TargetKingdomName);
		if (target == null || target.IsEliminated)
		{
			failureReason = "目标王国不存在或已经灭亡: " + ((effect.TargetKingdomId ?? effect.TargetKingdomName) ?? "");
			return false;
		}
		Kingdom issuer = ResolveKingdomByIdOrName(policy.KingdomId, policy.KingdomName);
		if (issuer == null || issuer.IsEliminated)
		{
			failureReason = "政策发布王国不存在或已经灭亡";
			return false;
		}
		if (target != issuer)
		{
			if (!issuer.IsAtWarWith(target))
			{
				failureReason = "跨国政策效果已失效：发布国与目标国当前不处于战争";
				return false;
			}
			string policyText = ((policy.PolicyName ?? "") + " " + (policy.PolicyContent ?? "")).Trim();
			if (!NpcRulerPolicyTextMentionsKingdom(policyText, target))
			{
				failureReason = "跨国政策效果已拒绝：政策名称或正文没有明确提及目标国";
				return false;
			}
		}
		int duration = effect.DurationDays;
		if (duration <= 0)
		{
			failureReason = "持续天数无效";
			return false;
		}
		if (!TryReadFiniteNpcPolicyEffect(effect, out float prosperity, out float food, out float hearth, out float loyalty, out float security, out float militia, out int stability))
		{
			failureReason = "政策效果包含 NaN、无穷值或无法转换的稳定度";
			return false;
		}
		if (Math.Abs(prosperity) <= 0.0001f && Math.Abs(food) <= 0.0001f && Math.Abs(hearth) <= 0.0001f && Math.Abs(loyalty) <= 0.0001f && Math.Abs(security) <= 0.0001f && Math.Abs(militia) <= 0.0001f && stability == 0)
		{
			failureReason = "没有可落地的每日数值";
			return false;
		}
		int submittedDay = Math.Max(0, policy.Day > 0 ? policy.Day : GetCurrentCampaignDay());
		int stableEffectIndex = Math.Max(0, effectIndex);
		string stableEffectId = BuildNpcRulerPolicyEffectId(policy, effect, target, stableEffectIndex);
		context = new NpcRulerPolicyActiveEffectBuildContext
		{
			Policy = policy,
			Effect = effect,
			TargetKingdom = target,
			EffectIndex = stableEffectIndex,
			EffectId = stableEffectId,
			RecordId = ResolveNpcRulerPolicyRecordId(policy, stableEffectId),
			PolicyName = LimitDisplayChars(policy.PolicyName ?? "", 100),
			DateText = string.IsNullOrWhiteSpace(policy.GameDate) ? NpcPublicFeedbackEventBehavior.FormatCurrentCampaignDate() : policy.GameDate.Trim(),
			SubmittedDay = submittedDay,
			DurationDays = duration,
			ProsperityDailyDeltaPerTown = prosperity,
			FoodDailyDeltaPerTown = food,
			HearthDailyDeltaPerVillage = hearth,
			LoyaltyDailyDeltaPerTown = loyalty,
			SecurityDailyDeltaPerTown = security,
			MilitiaDailyDeltaPerTown = militia,
			KingdomStabilityDailyDelta = stability,
			Reason = LimitDisplayChars(effect.Reason ?? policy.ImpactSummary ?? "", 240)
		};
		return true;
	}

	private bool TryFindExistingNpcRulerPolicyActiveEffect(NpcRulerPolicyActiveEffectBuildContext context, out string rawActiveEffect)
	{
		rawActiveEffect = "";
		return context != null
			&& !string.IsNullOrWhiteSpace(context.EffectId)
			&& _activePolicyEffects.TryGetValue(context.EffectId, out rawActiveEffect);
	}

	private static ActivePolicyEffectSaveData CreateNpcRulerPolicyActiveEffectSaveData(NpcRulerPolicyActiveEffectBuildContext context)
	{
		return new ActivePolicyEffectSaveData
		{
			EffectId = context?.EffectId ?? "",
			RecordId = context?.RecordId ?? "",
			PolicyName = context?.PolicyName ?? "",
			DateText = context?.DateText ?? "",
			SubmittedDay = Math.Max(0, context?.SubmittedDay ?? GetCurrentCampaignDay()),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			TargetKingdomId = context?.TargetKingdom?.StringId ?? (context?.Effect?.TargetKingdomId ?? ""),
			TargetKingdomName = GetKingdomName(context?.TargetKingdom),
			ProsperityDailyDeltaPerTown = context?.ProsperityDailyDeltaPerTown ?? 0f,
			FoodDailyDeltaPerTown = context?.FoodDailyDeltaPerTown ?? 0f,
			HearthDailyDeltaPerVillage = context?.HearthDailyDeltaPerVillage ?? 0f,
			LoyaltyDailyDeltaPerTown = context?.LoyaltyDailyDeltaPerTown ?? 0f,
			SecurityDailyDeltaPerTown = context?.SecurityDailyDeltaPerTown ?? 0f,
			MilitiaDailyDeltaPerTown = context?.MilitiaDailyDeltaPerTown ?? 0f,
			KingdomStabilityDailyDelta = context?.KingdomStabilityDailyDelta ?? 0,
			TotalDurationDays = Math.Max(0, context?.DurationDays ?? 0),
			RemainingDays = Math.Max(0, context?.DurationDays ?? 0),
			LastAppliedDay = Math.Max(0, context?.SubmittedDay ?? GetCurrentCampaignDay()),
			Reason = context?.Reason ?? "",
			Ended = false,
			EndReason = ""
		};
	}

	private void LogNpcRulerPolicyDuplicateActiveEffect(NpcRulerPolicyActiveEffectBuildContext context)
	{
		string duplicateLine = BuildNpcRulerPolicyActiveEffectLogLine(context)
			+ " submittedDay=" + Math.Max(0, context?.SubmittedDay ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " activeEffects=" + _activePolicyEffects.Count.ToString(CultureInfo.InvariantCulture);
		PolicyEffectLedgerLog("npc-active-skip-duplicate", duplicateLine);
		PolicyDebugLog("npc-active-skip-duplicate", duplicateLine);
	}

	private void LogNpcRulerPolicyActiveEffectCreated(NpcRulerPolicyActiveEffectBuildContext context, ActivePolicyEffectSaveData activeEffect)
	{
		string createdLine = BuildNpcRulerPolicyActiveEffectLogLine(context)
			+ " recordId=" + SanitizeNpcPolicyLogValue(activeEffect?.RecordId)
			+ " policy=\"" + SanitizeNpcPolicyLogText(activeEffect?.PolicyName) + "\""
			+ " submittedDay=" + Math.Max(0, activeEffect?.SubmittedDay ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " duration=" + Math.Max(0, context?.DurationDays ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " activeEffects=" + _activePolicyEffects.Count.ToString(CultureInfo.InvariantCulture);
		PolicyEffectLedgerLog("npc-active-created", createdLine);
		PolicyDebugLog("npc-active-effects-created", createdLine);
	}

	private static int ResolveNpcRulerPolicyEffectIndex(NpcRulerPolicyRecord policy, NpcRulerPolicyEffectDto effect)
	{
		List<NpcRulerPolicyEffectDto> effects = policy?.Effects;
		if (effects == null || effects.Count <= 0 || effect == null)
		{
			return 0;
		}
		for (int i = 0; i < effects.Count; i++)
		{
			if (object.ReferenceEquals(effects[i], effect))
			{
				return i;
			}
		}
		for (int i = 0; i < effects.Count; i++)
		{
			if (NpcRulerPolicyEffectsEquivalent(effects[i], effect))
			{
				return i;
			}
		}
		return 0;
	}

	private static bool NpcRulerPolicyEffectsEquivalent(NpcRulerPolicyEffectDto left, NpcRulerPolicyEffectDto right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		return string.Equals((left.TargetKingdomId ?? "").Trim(), (right.TargetKingdomId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
			&& string.Equals((left.TargetKingdomName ?? "").Trim(), (right.TargetKingdomName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
			&& Math.Abs(left.ProsperityDailyDeltaPerTown - right.ProsperityDailyDeltaPerTown) < 0.0001f
			&& Math.Abs(left.FoodDailyDeltaPerTown - right.FoodDailyDeltaPerTown) < 0.0001f
			&& Math.Abs(left.HearthDailyDeltaPerVillage - right.HearthDailyDeltaPerVillage) < 0.0001f
			&& Math.Abs(left.LoyaltyDailyDeltaPerTown - right.LoyaltyDailyDeltaPerTown) < 0.0001f
			&& Math.Abs(left.SecurityDailyDeltaPerTown - right.SecurityDailyDeltaPerTown) < 0.0001f
			&& Math.Abs(left.MilitiaDailyDeltaPerTown - right.MilitiaDailyDeltaPerTown) < 0.0001f
			&& Math.Abs(left.KingdomStabilityDailyDelta - right.KingdomStabilityDailyDelta) < 0.0001f
			&& left.DurationDays == right.DurationDays
			&& string.Equals((left.Reason ?? "").Trim(), (right.Reason ?? "").Trim(), StringComparison.Ordinal);
	}

	private static string BuildNpcRulerPolicyEffectId(NpcRulerPolicyRecord policy, NpcRulerPolicyEffectDto effect, Kingdom target, int effectIndex)
	{
		return "npc_ruler_policy:"
			+ ResolveNpcRulerPolicyStablePolicyKey(policy)
			+ ":"
			+ NormalizeNpcPolicyKeyPart(target?.StringId ?? effect?.TargetKingdomId ?? effect?.TargetKingdomName)
			+ ":"
			+ Math.Max(0, effectIndex).ToString(CultureInfo.InvariantCulture);
	}

	private static string ResolveNpcRulerPolicyStablePolicyKey(NpcRulerPolicyRecord policy)
	{
		if (!string.IsNullOrWhiteSpace(policy?.PolicyId))
		{
			return NormalizeNpcPolicyKeyPart(policy.PolicyId);
		}
		return "missing:"
			+ NormalizeNpcPolicyKeyPart(FirstNonEmptyNpcPolicyValue(policy?.BatchId, policy?.KingdomId, policy?.RulerHeroId, (policy?.Day ?? 0).ToString(CultureInfo.InvariantCulture)))
			+ ":"
			+ ComputeStableNpcPolicyHash(BuildNpcRulerPolicyFallbackSource(policy));
	}

	private static string ResolveNpcRulerPolicyRecordId(NpcRulerPolicyRecord policy, string effectId)
	{
		return string.IsNullOrWhiteSpace(policy?.PolicyId) ? (effectId ?? "") : policy.PolicyId.Trim();
	}

	private static string BuildNpcRulerPolicyFallbackSource(NpcRulerPolicyRecord policy)
	{
		StringBuilder builder = new StringBuilder();
		AppendNpcPolicyFallbackSourceField(builder, policy?.BatchId);
		AppendNpcPolicyFallbackSourceField(builder, policy?.KingdomId);
		AppendNpcPolicyFallbackSourceField(builder, policy?.RulerHeroId);
		AppendNpcPolicyFallbackSourceField(builder, (policy?.Day ?? 0).ToString(CultureInfo.InvariantCulture));
		AppendNpcPolicyFallbackSourceField(builder, policy?.GameDate);
		AppendNpcPolicyFallbackSourceField(builder, policy?.PolicyName);
		AppendNpcPolicyFallbackSourceField(builder, policy?.PolicyContent);
		AppendNpcPolicyFallbackSourceField(builder, policy?.ImpactSummary);
		return builder.ToString();
	}

	private static void AppendNpcPolicyFallbackSourceField(StringBuilder builder, string value)
	{
		string normalized = (value ?? "").Trim();
		builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
		builder.Append(':');
		builder.Append(normalized);
		builder.Append('|');
	}

	private static string FirstNonEmptyNpcPolicyValue(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "none";
	}

	private static string ComputeStableNpcPolicyHash(string value)
	{
		unchecked
		{
			ulong hash = 14695981039346656037UL;
			foreach (char ch in value ?? "")
			{
				hash ^= ch;
				hash *= 1099511628211UL;
			}
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	private static string BuildNpcRulerPolicyActiveEffectLogLine(NpcRulerPolicyActiveEffectBuildContext context)
	{
		StringBuilder builder = new StringBuilder();
		AppendNpcPolicyLogField(builder, "policyId", context?.Policy?.PolicyId, quoted: false);
		AppendNpcPolicyLogField(builder, "rulerHeroId", context?.Policy?.RulerHeroId, quoted: false);
		AppendNpcPolicyLogField(builder, "rulerName", context?.Policy?.RulerName, quoted: true);
		AppendNpcPolicyLogField(builder, "targetKingdomId", context?.TargetKingdom?.StringId, quoted: false);
		AppendNpcPolicyLogField(builder, "targetKingdomName", GetKingdomName(context?.TargetKingdom), quoted: true);
		AppendNpcPolicyLogField(builder, "effectId", context?.EffectId, quoted: false);
		AppendNpcPolicyLogField(builder, "effectIndex", Math.Max(0, context?.EffectIndex ?? 0).ToString(CultureInfo.InvariantCulture), quoted: false);
		return builder.ToString();
	}

	private static void AppendNpcPolicyLogField(StringBuilder builder, string name, string value, bool quoted)
	{
		if (builder == null || string.IsNullOrWhiteSpace(name))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.Append(' ');
		}
		builder.Append(name.Trim());
		builder.Append('=');
		if (quoted)
		{
			builder.Append('"');
			builder.Append(SanitizeNpcPolicyLogText(value));
			builder.Append('"');
			return;
		}
		builder.Append(SanitizeNpcPolicyLogValue(value));
	}

	private static string SanitizeNpcPolicyLogValue(string value)
	{
		return (value ?? "").Trim().Replace("\r", " ").Replace("\n", " ");
	}

	private static string SanitizeNpcPolicyLogText(string value)
	{
		return SanitizeNpcPolicyLogValue(value).Replace("\"", "'");
	}

	private sealed class NpcRulerPolicyActiveEffectBuildContext
	{
		public NpcRulerPolicyRecord Policy;

		public NpcRulerPolicyEffectDto Effect;

		public Kingdom TargetKingdom;

		public int EffectIndex;

		public string EffectId;

		public string RecordId;

		public string PolicyName;

		public string DateText;

		public int SubmittedDay;

		public int DurationDays;

		public float ProsperityDailyDeltaPerTown;

		public float FoodDailyDeltaPerTown;

		public float HearthDailyDeltaPerVillage;

		public float LoyaltyDailyDeltaPerTown;

		public float SecurityDailyDeltaPerTown;

		public float MilitiaDailyDeltaPerTown;

		public int KingdomStabilityDailyDelta;

		public string Reason;
	}

	private static bool TryReadFiniteNpcPolicyEffect(NpcRulerPolicyEffectDto effect, out float prosperity, out float food, out float hearth, out float loyalty, out float security, out float militia, out int stability)
	{
		prosperity = effect?.ProsperityDailyDeltaPerTown ?? 0f;
		food = effect?.FoodDailyDeltaPerTown ?? 0f;
		hearth = effect?.HearthDailyDeltaPerVillage ?? 0f;
		loyalty = effect?.LoyaltyDailyDeltaPerTown ?? 0f;
		security = effect?.SecurityDailyDeltaPerTown ?? 0f;
		militia = effect?.MilitiaDailyDeltaPerTown ?? 0f;
		stability = 0;
		if (float.IsNaN(prosperity) || float.IsInfinity(prosperity)
			|| float.IsNaN(food) || float.IsInfinity(food)
			|| float.IsNaN(hearth) || float.IsInfinity(hearth)
			|| float.IsNaN(loyalty) || float.IsInfinity(loyalty)
			|| float.IsNaN(security) || float.IsInfinity(security)
			|| float.IsNaN(militia) || float.IsInfinity(militia)
			|| effect == null || float.IsNaN(effect.KingdomStabilityDailyDelta) || float.IsInfinity(effect.KingdomStabilityDailyDelta))
		{
			return false;
		}
		double rounded = Math.Round(effect.KingdomStabilityDailyDelta, MidpointRounding.AwayFromZero);
		if (rounded < int.MinValue || rounded > int.MaxValue)
		{
			return false;
		}
		stability = (int)rounded;
		return true;
	}

	private static bool NpcRulerPolicyTextMentionsKingdom(string policyText, Kingdom kingdom)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(policyText))
		{
			return false;
		}
		string[] candidates =
		{
			kingdom.StringId ?? "",
			GetKingdomName(kingdom),
			kingdom.Name?.ToString() ?? "",
			kingdom.Leader?.StringId ?? "",
			kingdom.Leader?.Name?.ToString() ?? "",
			kingdom.RulingClan?.StringId ?? "",
			kingdom.RulingClan?.Name?.ToString() ?? ""
		};
		return candidates.Any(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length >= 2 && policyText.IndexOf(x.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static string NormalizeNpcPolicyKeyPart(string value)
	{
		value = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return "none";
		}
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		foreach (char ch in value)
		{
			if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == ':')
			{
				sb.Append(ch);
			}
		}
		return sb.Length == 0 ? "x" : sb.ToString();
	}
}
