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

	[JsonProperty("creativePremise")]
	public string CreativePremise { get; set; }

	[JsonProperty("policyName")]
	public string PolicyName { get; set; }

	[JsonProperty("policyContent")]
	public string PolicyContent { get; set; }

	[JsonProperty("policyDigest")]
	public string PolicyDigest { get; set; }

	[JsonProperty("eventPremise")]
	public string EventPremise { get; set; }

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

	public string Preview => "System:\n" + (SystemPrompt ?? "");
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

	public static async Task<NpcPolicyApiCallResult> CallEventAndRebellionApiWithRetriesAsync(string systemPrompt, int maxTokens, int hardTimeoutMilliseconds, string source, long runtimeGeneration, int maxAttempts = DefaultMaxAttempts)
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
			NpcPolicyApiCallResult result = await CallEventAndRebellionApiOnceAsync(systemPrompt, Math.Max(1, maxTokens), Math.Max(1000, hardTimeoutMilliseconds), source, runtimeGeneration);
			result.AttemptsUsed = attempt;
			finalResult = result;
			RecordPromptExchangeSafe(source, attempt, attempts, systemPrompt, result);
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

	private static async Task<NpcPolicyApiCallResult> CallEventAndRebellionApiOnceAsync(string systemPrompt, int maxTokens, int hardTimeoutMilliseconds, string source, long runtimeGeneration)
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
			JArray messages = BuildMessageArray(systemPrompt);
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

	private static void RecordPromptExchangeSafe(string source, int attempt, int attempts, string systemPrompt, NpcPolicyApiCallResult result)
	{
		try
		{
			string responseForLog = result.Success ? (result.Content ?? "") : ("错误: " + (result.ErrorMessage ?? "未知错误"));
			Logger.LogEventPromptExchange((source ?? "NpcPolicy") + " [尝试 " + attempt.ToString(CultureInfo.InvariantCulture) + "/" + attempts.ToString(CultureInfo.InvariantCulture) + "]", BuildPromptPreview(systemPrompt), responseForLog);
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

	private static JArray BuildMessageArray(string systemPrompt)
	{
		return new JArray
		{
			new JObject
			{
				["role"] = "system",
				["content"] = systemPrompt ?? ""
			}
		};
	}

	private static string BuildPromptPreview(string systemPrompt)
	{
		return "System:\n" + (systemPrompt ?? "");
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
		PolicySystemLog.Write("NpcParse", kind + "-parse-failed", message, "raw_sample:\n" + Clip(raw) + "\n\nextracted_sample:\n" + Clip(extracted));
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
	private const int HardContextChars = 96000;
	private const int PolicyKnowledgeTargetChars = 380;
	private const int PolicyKnowledgeMinChars = 220;
	private const int PolicyKnowledgeMaxChars = 450;
	private const string PolicyKnowledgeRagFocus = "统治合法性 权力基础 政治目标 制度约束 支持者反对者 社会矛盾";
	private const int PolicyMaxTokens = 8000;
	private const int FailedGenerationBackoffHours = 6;
	private const int PolicyApiHardTimeoutMilliseconds = 540000;
	private const double PolicyCommitFrameBudgetMs = 1.0;
	private static readonly string[] PolicyKnowledgeGovernanceTerms =
	{
		"合法", "王权", "统治", "皇帝", "女皇", "大公", "可汗", "至高王", "元老院", "波耶", "那颜", "封臣", "贵族", "氏族", "部落", "酋长",
		"军队", "亲兵", "继承", "自治", "土地", "税", "政策", "法律", "权利", "利益", "支持", "反对", "矛盾", "争议", "评价", "忠诚",
		"民众", "商人", "农户", "宗教", "信仰", "传统", "名望", "威望", "权力", "政治"
	};
	private static readonly string[] PolicyKnowledgeGeographyTerms =
	{
		"位于", "东面", "西面", "南面", "北面", "高原", "山脉", "山岭", "河流", "湖泊", "峡谷", "地形", "地貌", "流入", "发源", "海湾", "气候", "森林", "草原"
	};

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
		AnimusForgeWorldEventInboxEntry feedbackEntry = BuildPolicyFeedbackWorldEvent(record);
		AnimusForgeWorldEventBehavior.UpsertWorldEventForExternal(feedbackEntry, markUnread: true);
		RecordUnifiedPolicyWeeklyMaterial(record);
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
				if (string.IsNullOrWhiteSpace(job.SystemPrompt))
				{
					NpcPolicyPrompt prompt = BuildPolicyPrompt(job.Context);
					job.SystemPrompt = prompt.SystemPrompt;
					job.PromptPreview = prompt.Preview;
				}
				PolicyTraceLog("generation-job-built", BuildPolicyJobTracePrefix(job)
					+ " systemPromptChars=" + (job.SystemPrompt?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " messageCount=1");
				PolicyTraceLog("generation-batch-call-start", BuildPolicyJobTracePrefix(job), job.PromptPreview);
				NpcPolicyApiCallResult apiResult = await NpcPolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(job.SystemPrompt, job.MaxTokens, job.HardTimeoutMilliseconds, "NpcRulerPolicy", job.RuntimeGeneration, 3);
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
			NpcPolicyApiCallResult apiResult = await NpcPolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(singlePrompt.SystemPrompt, job.MaxTokens, job.HardTimeoutMilliseconds, "NpcRulerPolicySingleFallback", job.RuntimeGeneration, 3);
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
					context.PublicFeedbackEntry = BuildPolicyFeedbackWorldEvent(record);
					if (context.PublicFeedbackEntry != null)
					{
						AnimusForgeWorldEventBehavior.UpsertWorldEventForExternal(context.PublicFeedbackEntry, markUnread: true);
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
		PolicySystemLog.Write("NpcDetail", stage, message, detail);
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
				.Append(" creativePremise=").Append(record.CreativePremise)
				.Append(" eventPremise=").Append(record.EventPremise)
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
		context.TodayAlreadyGenerated = CountNpcPoliciesGeneratedOnDay(currentDay);
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
		sb.AppendLine("Current date: " + context.GameDate);
		sb.AppendLine(BuildCampaignCalendarContext());
		List<NpcRulerPolicyKingdomContext> targets = (context.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>()).Where(x => x != null).ToList();
		foreach (NpcRulerPolicyKingdomContext item in targets)
		{
			string targetBlock = BuildKingdomPromptContext(item);
			sb.AppendLine(targetBlock);
			PolicySystemLog.Write("Context", "target",
				"kingdom=" + (item.KingdomId ?? "")
				+ " chars=" + targetBlock.Length.ToString(CultureInfo.InvariantCulture)
				+ " knowledgeGroundingChars=" + (item.KnowledgeGrounding?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " policyGroundingChars=" + item.PolicyGroundingChars.ToString(CultureInfo.InvariantCulture)
				+ " personalityChars=" + item.PersonalityChars.ToString(CultureInfo.InvariantCulture)
				+ " backgroundChars=" + item.BackgroundChars.ToString(CultureInfo.InvariantCulture)
				+ " currentWorldFactsChars=" + (item.CurrentWorldFacts?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " policyMemoryChars=" + (item.PolicyMemory?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " recentPhenomenonChars=" + (item.RecentWorldPhenomenon?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " foreignDirectPressureChars=" + (item.ForeignDirectPressure?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " mechanicalFactsChars=" + (item.MechanicalFacts?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " ownPolicies=" + item.PolicyMemoryCount.ToString(CultureInfo.InvariantCulture)
				+ " recentPhenomena=" + item.RecentWorldPhenomenonCount.ToString(CultureInfo.InvariantCulture)
				+ " foreignDirectPressures=" + item.ForeignDirectPressureCount.ToString(CultureInfo.InvariantCulture));
		}
		if (sb.Length > HardContextChars)
		{
			throw new InvalidOperationException("NPC policy mandatory context exceeds hard safety limit: chars=" + sb.Length.ToString(CultureInfo.InvariantCulture));
		}
		int ownPolicyCount = targets.Sum(x => x?.PolicyMemoryCount ?? 0);
		int recentPhenomenonCount = targets.Sum(x => x?.RecentWorldPhenomenonCount ?? 0);
		int foreignDirectPressureCount = targets.Sum(x => x?.ForeignDirectPressureCount ?? 0);
		PolicySystemLog.Write("Context", "batch",
			"targets=" + targets.Count.ToString(CultureInfo.InvariantCulture)
			+ " chars=" + sb.Length.ToString(CultureInfo.InvariantCulture)
			+ " estimatedTokens=" + Math.Ceiling(sb.Length * 0.6d).ToString(CultureInfo.InvariantCulture)
			+ " ownPolicies=" + ownPolicyCount.ToString(CultureInfo.InvariantCulture)
			+ " recentPhenomena=" + recentPhenomenonCount.ToString(CultureInfo.InvariantCulture)
			+ " foreignDirectPressures=" + foreignDirectPressureCount.ToString(CultureInfo.InvariantCulture));
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
			prosperity += " 村庄平均户数=" + FormatNumber(villages.Average(x => x.Village.Hearth));
		}
		List<NpcRulerPolicyAllowedEffectTarget> allowedTargets = BuildAllowedEffectTargets(kingdom);
		string policies = SafeReadVanillaPolicies(kingdom);
		List<string> policyMemoryItems = BuildPolicyMemoryContexts(kingdomId);
		string recentWorldPhenomenon = BuildRecentWorldPhenomenonContext(kingdomId);
		List<string> foreignDirectPressures = BuildForeignDirectPressureContexts(kingdomId);
		MyBehavior.GetNpcPersonaForExternal(ruler, out string personality, out string background);
		string clanContext = BuildClanSnapshot(kingdom);
		string diplomacyContext = BuildDiplomacyNeighborSummary(kingdom);
		string policyGrounding = BuildNpcPolicyKnowledgeContext(kingdom, ruler, clanContext, diplomacyContext);
		string compactPersonality = CompressCompleteText(personality, 120, 120);
		string compactBackground = CompressCompleteText(background, 140, 140);
		string knowledgeGrounding = "RulerPersona{name=" + (ruler?.Name?.ToString() ?? "未知")
			+ ",personality=" + compactPersonality
			+ ",background=" + compactBackground + "}"
			+ (string.IsNullOrWhiteSpace(policyGrounding) ? "" : "\nPolicyGrounding{" + policyGrounding + "}");
		string currentWorldFacts = "Target{kingdomId=" + kingdomId
			+ ",name=" + kingdomName
			+ ",rulerHeroId=" + (ruler?.StringId ?? "")
			+ ",rulerName=" + (ruler?.Name?.ToString() ?? "")
			+ ",culture=" + (kingdom.Culture?.Name?.ToString() ?? kingdom.Culture?.StringId ?? "未知")
			+ ",kingdomTitle=" + (kingdom.EncyclopediaTitle?.ToString() ?? "")
			+ ",rulerTitle=" + (kingdom.EncyclopediaRulerTitle?.ToString() ?? "")
			+ ",war=" + diplomacyContext + "}";
		string mechanicalFacts = "AllowedEffectTargets{" + BuildAllowedEffectTargetsPrompt(allowedTargets) + "}"
			+ " | SettlementScale{" + BuildSettlementSnapshot(towns, villages, prosperity) + "}"
			+ " | KingdomStability{value=" + SafeKingdomStability(kingdom).ToString(CultureInfo.InvariantCulture) + "/100}"
			+ " | VanillaPolicyMechanics{labels=" + policies + ",note=仅为原版玩法政策名称，不证明存在同名政治机构}";
		return new NpcRulerPolicyKingdomContext
		{
			KingdomId = kingdomId,
			KingdomName = kingdomName,
			RulerHeroId = ruler?.StringId ?? "",
			RulerName = ruler?.Name?.ToString() ?? "",
			KnowledgeGrounding = knowledgeGrounding,
			PolicyGroundingChars = policyGrounding.Length,
			PersonalityChars = compactPersonality.Length,
			BackgroundChars = compactBackground.Length,
			CurrentWorldFacts = currentWorldFacts,
			PolicyMemory = policyMemoryItems.Count == 0 ? "" : string.Join("\n", policyMemoryItems),
			RecentWorldPhenomenon = recentWorldPhenomenon ?? "",
			ForeignDirectPressure = foreignDirectPressures.Count == 0 ? "" : string.Join("\n", foreignDirectPressures),
			MechanicalFacts = mechanicalFacts,
			PolicyMemoryCount = policyMemoryItems.Count,
			RecentWorldPhenomenonCount = string.IsNullOrWhiteSpace(recentWorldPhenomenon) ? 0 : 1,
			ForeignDirectPressureCount = foreignDirectPressures.Count,
			AllowedEffectTargets = allowedTargets
		};
	}

	private static string BuildKingdomPromptContext(NpcRulerPolicyKingdomContext context)
	{
		if (context == null)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		AppendNpcPolicyPromptBlock(sb, "CurrentWorldFacts", context.CurrentWorldFacts);
		AppendNpcPolicyPromptBlock(sb, "KnowledgeGrounding", context.KnowledgeGrounding);
		AppendNpcPolicyPromptBlock(sb, "PolicyMemory", context.PolicyMemory);
		AppendNpcPolicyPromptBlock(sb, "RecentWorldPhenomenon", context.RecentWorldPhenomenon);
		AppendNpcPolicyPromptBlock(sb, "ForeignDirectPressure", context.ForeignDirectPressure);
		AppendNpcPolicyPromptBlock(sb, "MechanicalFacts", context.MechanicalFacts);
		return sb.ToString().TrimEnd();
	}

	private static void AppendNpcPolicyPromptBlock(StringBuilder sb, string name, string content)
	{
		if (sb == null || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		sb.AppendLine(name + "{");
		sb.AppendLine(content.Trim());
		sb.AppendLine("}");
	}

	private static string BuildNpcPolicyKnowledgeContext(Kingdom kingdom, Hero ruler, string clanContext, string diplomacyContext)
	{
		string query = Compact("统治者政策知识；统治者=" + (ruler?.Name?.ToString() ?? "")
			+ "；王国=" + GetKingdomName(kingdom)
			+ "；只检索其合法性、权力基础、政治目标、制度约束、支持者、反对者、争议和社会矛盾；排除纯地理与无关国家");
		string secondaryInput = Compact("当前国情：文化=" + (kingdom?.Culture?.Name?.ToString() ?? ruler?.Culture?.Name?.ToString() ?? "")
			+ "；执政结构=" + clanContext
			+ "；战争外交=" + diplomacyContext);
		return RetrieveNpcPolicyKnowledgeContext(kingdom, ruler, query, secondaryInput, BuildNpcPolicyRulerKnowledgeMentionedEntities(kingdom, ruler), "knowledge-policy");
	}

	private static string RetrieveNpcPolicyKnowledgeContext(Kingdom kingdom, Hero ruler, string query, string secondaryInput, MentionedWorldEntities mentionedEntities, string logCategory)
	{
		string kingdomId = kingdom?.StringId ?? "";
		string cultureId = kingdom?.Culture?.StringId ?? ruler?.Culture?.StringId ?? "";
		string raw = "";
		string compact = "";
		int keptSentenceCount = 0;
		int droppedSentenceCount = 0;
		bool libraryAvailable = KnowledgeLibraryBehavior.Instance != null;
		bool semanticEnabled = false;
		string fallbackReason = "";
		try
		{
			semanticEnabled = AIConfigHandler.KnowledgeRetrievalEnabled;
			if (ruler == null)
			{
				fallbackReason = "ruler_missing";
			}
			else if (!libraryAvailable)
			{
				fallbackReason = "library_unavailable";
			}
			else
			{
				using (PerfProbe.Scope("PolicyContext.KnowledgeRetrieval"))
				{
					raw = AIConfigHandler.GetLoreContext(query, ruler, secondaryInput, mentionedEntities) ?? "";
				}
				using (PerfProbe.Scope("PolicyContext.KnowledgeCompression"))
				{
					compact = CompressNpcPolicyKnowledgeContext(raw, kingdom, ruler, out keptSentenceCount, out droppedSentenceCount);
				}
				if (string.IsNullOrWhiteSpace(compact))
				{
					fallbackReason = string.IsNullOrWhiteSpace(raw) ? "no_match" : "no_policy_knowledge_after_filter";
				}
			}
		}
		catch (Exception ex)
		{
			fallbackReason = "exception:" + ex.GetType().Name;
			compact = "";
		}
		PolicySystemLog.Write("Context", logCategory,
			"kingdomId=" + kingdomId
			+ " cultureId=" + cultureId
			+ " queryChars=" + query.Length.ToString(CultureInfo.InvariantCulture)
			+ " secondaryChars=" + secondaryInput.Length.ToString(CultureInfo.InvariantCulture)
			+ " ragMode=target_plus_governance"
			+ " ragFocus=" + PolicyKnowledgeRagFocus
			+ " mentionCount=" + CountNpcPolicyKnowledgeMentions(mentionedEntities).ToString(CultureInfo.InvariantCulture)
			+ " rawChars=" + raw.Length.ToString(CultureInfo.InvariantCulture)
			+ " compactChars=" + compact.Length.ToString(CultureInfo.InvariantCulture)
			+ " keptSentences=" + keptSentenceCount.ToString(CultureInfo.InvariantCulture)
			+ " droppedSentences=" + droppedSentenceCount.ToString(CultureInfo.InvariantCulture)
			+ " libraryAvailable=" + libraryAvailable.ToString(CultureInfo.InvariantCulture)
			+ " semanticEnabled=" + semanticEnabled.ToString(CultureInfo.InvariantCulture)
			+ " hit=" + (!string.IsNullOrWhiteSpace(compact)).ToString(CultureInfo.InvariantCulture)
			+ (string.IsNullOrWhiteSpace(fallbackReason) ? "" : " fallback=" + fallbackReason));
		return compact;
	}

	private static MentionedWorldEntities BuildNpcPolicyRulerKnowledgeMentionedEntities(Kingdom kingdom, Hero ruler)
	{
		MentionedWorldEntities entities = new MentionedWorldEntities();
		AddNpcPolicyKnowledgeEntity(entities.Entities, ruler?.Name?.ToString(), ruler?.StringId);
		Clan rulingClan = kingdom?.RulingClan ?? ruler?.Clan;
		AddNpcPolicyKnowledgeEntity(entities.Entities, rulingClan?.Name?.ToString(), rulingClan?.StringId);
		AddNpcPolicyKnowledgeEntity(entities.Entities, GetKingdomName(kingdom), kingdom?.StringId);
		AddNpcPolicyKnowledgeEntity(entities.Entities, PolicyKnowledgeRagFocus, null);
		return entities;
	}

	private static void AddNpcPolicyKnowledgeEntity(List<string> target, string displayName, string fallbackId)
	{
		string value = string.IsNullOrWhiteSpace(displayName) ? (fallbackId ?? "").Trim() : displayName.Trim();
		if (!string.IsNullOrWhiteSpace(value) && target != null && !target.Contains(value, StringComparer.OrdinalIgnoreCase))
		{
			target.Add(value);
		}
	}

	private static int CountNpcPolicyKnowledgeMentions(MentionedWorldEntities entities)
	{
		if (entities == null)
		{
			return 0;
		}
		return entities.Entities?.Count ?? 0;
	}

	private static IEnumerable<string> SplitKnowledgeSentences(string raw)
	{
		string text = (raw ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		foreach (string rawLine in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			foreach (string sentence in Regex.Split(Compact(rawLine), @"(?<=[。！？!?；;])"))
			{
				string compact = Compact(sentence);
				if (!string.IsNullOrWhiteSpace(compact))
				{
					yield return compact;
				}
			}
		}
	}

	private static string NormalizeKnowledgeSentenceKey(string value)
	{
		return Regex.Replace((value ?? "").ToLowerInvariant(), @"[\s\p{P}\p{S}]+", "");
	}

	private static string CompressNpcPolicyKnowledgeContext(string raw, Kingdom kingdom, Hero ruler, out int keptSentenceCount, out int droppedSentenceCount)
	{
		keptSentenceCount = 0;
		droppedSentenceCount = 0;
		string text = (raw ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		const string knowledgeHeader = "参与互动让你的脑海里浮现了这些知识";
		int knowledgeStart = text.IndexOf(knowledgeHeader, StringComparison.Ordinal);
		if (knowledgeStart >= 0)
		{
			text = text.Substring(knowledgeStart + knowledgeHeader.Length).Trim();
		}
		else if (text.IndexOf("【玩家外貌信息（常驻）】", StringComparison.Ordinal) >= 0)
		{
			return "";
		}
		List<KeyValuePair<int, string>> candidates = new List<KeyValuePair<int, string>>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> targetAnchors = new[]
		{
			GetKingdomName(kingdom),
			kingdom?.StringId,
			ruler?.Name?.ToString(),
			ruler?.StringId,
			kingdom?.RulingClan?.Name?.ToString(),
			kingdom?.RulingClan?.StringId
		}.Select(x => (x ?? "").Trim()).Where(x => x.Length >= 2).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> foreignAssociationAnchors = new[]
		{
			GetKingdomName(kingdom),
			kingdom?.StringId,
			ruler?.Name?.ToString(),
			ruler?.StringId
		}.Select(x => (x ?? "").Trim()).Where(x => x.Length >= 2).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> foreignKingdomNames = GetNpcPolicyForeignKingdomKnowledgeNames(kingdom);
		int consideredSentenceCount = 0;
		foreach (string rawLine in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string line = Compact(rawLine);
			if (string.IsNullOrWhiteSpace(line)
				|| line.StartsWith("【以下是关于（", StringComparison.Ordinal)
				|| line.StartsWith("【玩家外貌信息", StringComparison.Ordinal)
				|| line.IndexOf("与玩家面对面互动时", StringComparison.Ordinal) >= 0)
			{
				continue;
			}
			foreach (string sentence in Regex.Split(line, @"(?<=[。！？!?；;])"))
			{
				string candidate = Compact(Regex.Replace(sentence ?? "", @"(?<![A-Za-z])[A-Za-z](?![A-Za-z])", ""));
				if (string.IsNullOrWhiteSpace(candidate))
				{
					continue;
				}
				consideredSentenceCount++;
				string key = NormalizeKnowledgeSentenceKey(candidate);
				bool hasTargetAnchor = ContainsAnyNpcPolicyKnowledgeTerm(candidate, targetAnchors);
				bool hasGovernanceTerm = ContainsAnyNpcPolicyKnowledgeTerm(candidate, PolicyKnowledgeGovernanceTerms);
				bool isPureGeography = ContainsAnyNpcPolicyKnowledgeTerm(candidate, PolicyKnowledgeGeographyTerms) && !hasGovernanceTerm;
				bool hasUnanchoredForeignKingdom = ContainsAnyNpcPolicyKnowledgeTerm(candidate, foreignKingdomNames)
					&& !ContainsAnyNpcPolicyKnowledgeTerm(candidate, foreignAssociationAnchors);
				if (candidate.Length > PolicyKnowledgeMaxChars || key.Length < 6 || !seen.Add(key) || (!hasTargetAnchor && !hasGovernanceTerm) || isPureGeography || hasUnanchoredForeignKingdom)
				{
					continue;
				}
				int score = (hasTargetAnchor ? 4 : 0) + PolicyKnowledgeGovernanceTerms.Count(term => candidate.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
				candidates.Add(new KeyValuePair<int, string>(score, candidate));
			}
		}
		StringBuilder result = new StringBuilder();
		foreach (KeyValuePair<int, string> scoredCandidate in candidates.OrderByDescending(x => x.Key))
		{
			string candidate = scoredCandidate.Value;
			int separatorChars = result.Length > 0 ? 1 : 0;
			int nextLength = result.Length + separatorChars + candidate.Length;
			if (nextLength <= PolicyKnowledgeTargetChars || (result.Length < PolicyKnowledgeMinChars && nextLength <= PolicyKnowledgeMaxChars))
			{
				if (result.Length > 0) result.Append(' ');
				result.Append(candidate);
				keptSentenceCount++;
			}
		}
		droppedSentenceCount = Math.Max(0, consideredSentenceCount - keptSentenceCount);
		return result.ToString().Trim();
	}

	private static bool ContainsAnyNpcPolicyKnowledgeTerm(string text, IEnumerable<string> terms)
	{
		return !string.IsNullOrWhiteSpace(text)
			&& (terms ?? Enumerable.Empty<string>()).Any(term => !string.IsNullOrWhiteSpace(term) && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static List<string> GetNpcPolicyForeignKingdomKnowledgeNames(Kingdom targetKingdom)
	{
		try
		{
			return (Kingdom.All ?? Enumerable.Empty<Kingdom>())
				.Where(x => x != null && x != targetKingdom)
				.SelectMany(x => new[] { GetKingdomName(x), x.StringId })
				.Select(x => (x ?? "").Trim())
				.Where(x => x.Length >= 2)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return new List<string>();
		}
	}

	private static string CompressCompleteText(string raw, int targetChars, int maxChars)
	{
		string text = Compact(raw);
		if (string.IsNullOrWhiteSpace(text) || maxChars <= 0)
		{
			return "";
		}
		if (text.Length <= maxChars)
		{
			return text;
		}
		List<string> candidates = new List<string>();
		foreach (string sentence in Regex.Split(text, @"(?<=[。！？!?；;])"))
		{
			string candidate = Compact(sentence);
			if (candidate.Length <= maxChars)
			{
				if (!string.IsNullOrWhiteSpace(candidate)) candidates.Add(candidate);
				continue;
			}
			foreach (string clause in Regex.Split(candidate, @"(?<=[，,：:])"))
			{
				string compactClause = Compact(clause);
				if (!string.IsNullOrWhiteSpace(compactClause) && compactClause.Length <= maxChars)
				{
					candidates.Add(compactClause);
				}
			}
		}
		StringBuilder result = new StringBuilder();
		foreach (string candidate in candidates)
		{
			int nextLength = result.Length + (result.Length > 0 ? 1 : 0) + candidate.Length;
			if (nextLength > maxChars)
			{
				continue;
			}
			if (result.Length > 0) result.Append(' ');
			result.Append(candidate);
			if (result.Length >= targetChars)
			{
				break;
			}
		}
		return result.ToString().Trim();
	}

	private List<string> BuildPolicyMemoryContexts(string kingdomId)
	{
		return GetRecentPolicyRecordsInternal(kingdomId, 2)
			.AsEnumerable()
			.Reverse()
			.Select(BuildPolicyMemoryContext)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList();
	}

	private static string BuildPolicyMemoryContext(NpcRulerPolicyRecord record)
	{
		if (record == null)
		{
			return "";
		}
		return "Policy{name=" + Limit(Compact(record.PolicyName), 30)
			+ ",decision=" + CompressCompleteText(FirstNonEmpty(record.PolicyDigest, record.PolicyContent, record.ImpactSummary), 60, 80)
			+ ",effects=" + Limit(Compact(BuildEffectSummary(record.Effects)), 80) + "}";
	}

	private string BuildRecentWorldPhenomenonContext(string kingdomId)
	{
		NpcRulerPolicyRecord record = GetRecentPolicyRecordsInternal(kingdomId, 1).FirstOrDefault();
		if (record == null)
		{
			return "";
		}
		string summary = CompressCompleteText(FirstNonEmpty(record.FeedbackDigest, record.EventPremise, record.PublicFeedback), 45, 60);
		if (string.IsNullOrWhiteSpace(summary))
		{
			return "";
		}
		return "Phenomenon{summary=" + summary + "}";
	}

	private List<string> BuildForeignDirectPressureContexts(string targetKingdomId)
	{
		string targetId = (targetKingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(targetId))
		{
			return new List<string>();
		}
		return _policyRecords.Values
			.Select(DeserializeRecord)
			.Where(record => record != null
				&& !string.IsNullOrWhiteSpace(record.KingdomId)
				&& !string.Equals((record.KingdomId ?? "").Trim(), targetId, StringComparison.OrdinalIgnoreCase))
			.Select(record => new
			{
				Record = record,
				DirectEffects = (record.Effects ?? new List<NpcRulerPolicyEffectDto>())
					.Where(effect => IsActivePolicyEffect(effect)
						&& HasAnyDailyDelta(effect)
						&& string.Equals((effect.TargetKingdomId ?? "").Trim(), targetId, StringComparison.OrdinalIgnoreCase))
					.ToList()
			})
			.Where(item => item.DirectEffects.Count > 0)
			.OrderByDescending(item => item.Record.Day)
			.ThenByDescending(item => item.Record.CreatedUtcTicks)
			.Take(2)
			.Select(item => "Pressure{sourceKingdomName=" + Compact(item.Record.KingdomName)
				+ ",directMeasure=" + CompressCompleteText(FirstNonEmpty(item.Record.PolicyDigest, item.Record.PolicyContent, item.Record.ImpactSummary), 50, 60)
				+ ",directEffects=" + Limit(Compact(BuildEffectSummary(item.DirectEffects)), 80) + "}")
			.ToList();
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
				.Append("；衍生事件=").Append(Limit(record.FeedbackDigest, 70)).AppendLine();
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
		if (Math.Abs(effect.HearthDailyDeltaPerVillage) > 0.0001f) values.Add("户数" + FormatSigned(effect.HearthDailyDeltaPerVillage));
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
		return "Calendar{daysInSeason=" + daysInSeason.ToString(CultureInfo.InvariantCulture)
			+ ",daysInYear=" + daysInYear.ToString(CultureInfo.InvariantCulture) + "}";
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

	private int CountNpcPoliciesGeneratedOnDay(int currentDay)
	{
		return _policyRecords.Values
			.Select(DeserializeRecord)
			.Count(x => x != null && !x.IsPlayerPolicy && x.Day == currentDay);
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
			return "ruling=" + ruling
				+ ",clanCount=" + clans.Count.ToString(CultureInfo.InvariantCulture);
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
			return "townOrCastleCount=" + safeTowns.Count.ToString(CultureInfo.InvariantCulture)
				+ ",villageCount=" + safeVillages.Count.ToString(CultureInfo.InvariantCulture)
				+ ",avg=" + (string.IsNullOrWhiteSpace(prosperitySummary) ? "未知" : prosperitySummary);
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
		return ComposePolicyPrompt(context, ResolveNpcRulerPolicyEditablePrompt());
	}

	private static NpcPolicyPrompt ComposePolicyPrompt(NpcRulerPolicyBatchContext context, string editablePrompt)
	{
		editablePrompt = (editablePrompt ?? "").Trim();
		int targetCount = Math.Max(0, context?.Kingdoms?.Count ?? 0);
		StringBuilder contract = new StringBuilder();
		contract.AppendLine("【不可覆盖的技术契约】");
		contract.AppendLine("只输出严格 JSON，不输出 Markdown、解释、隐藏标签、玩家操作、扣费或原版 PolicyObject。根对象只能是 {\"policies\":[...]}；目标数量=" + targetCount.ToString(CultureInfo.InvariantCulture) + "，必须为下方每个 Target 各输出 1 条，不得遗漏、重复或增加王国。");
		contract.AppendLine("每条 policy 必须按下列顺序和形状包含全部字段，不得增删或改名：{\"kingdomId\":\"...\",\"kingdomName\":\"...\",\"rulerHeroId\":\"...\",\"rulerName\":\"...\",\"creativePremise\":\"...\",\"policyName\":\"...\",\"policyContent\":\"...\",\"policyDigest\":\"...\",\"eventPremise\":\"...\",\"derivedEventTitle\":\"...\",\"derivedEventContent\":\"...\",\"derivedEventDigest\":\"...\",\"impactSummary\":\"...\",\"effects\":[{\"targetKingdomId\":\"...\",\"targetKingdomName\":\"...\",\"prosperityDailyDeltaPerTown\":0,\"foodDailyDeltaPerTown\":0,\"hearthDailyDeltaPerVillage\":0,\"loyaltyDailyDeltaPerTown\":0,\"securityDailyDeltaPerTown\":0,\"militiaDailyDeltaPerTown\":0,\"kingdomStabilityDailyDelta\":0,\"durationDays\":1,\"reason\":\"...\"}]}。");
		contract.AppendLine("身份字段必须复制对应 Target。effects 必须是数组并留在同一 policy 内；示例中的 0 仅表示数值类型，整条政策至少有一项 daily delta 非 0。durationDays 必须是正整数；daily delta 必须是有限数值；kingdomStabilityDailyDelta 按整数语义输出。");
		contract.AppendLine("effect 目标只能来自该 Target 的 AllowedEffectTargets，每条政策最多一个 self 和一个 warEnemy。外国目标必须在 policyName 或 policyContent 中点名，且数值只能来自 policyContent 明确写出的直接跨国措施；不得重定向非法目标或从同期现象、摘要、传闻及连锁推测生成外国 effect。");
		contract.AppendLine("prosperityDailyDeltaPerTown 与 militiaDailyDeltaPerTown 按每座城镇和城堡结算；foodDailyDeltaPerTown、loyaltyDailyDeltaPerTown、securityDailyDeltaPerTown 按每座城镇结算；hearthDailyDeltaPerVillage 按每座村庄结算；kingdomStabilityDailyDelta 对王国整体结算一次。");
		contract.AppendLine("derivedEventTitle、derivedEventContent、derivedEventDigest 必须描述 eventPremise 的同一现象，事件不得产生 effects。impactSummary 与 effects 只描述政策影响。JSON 字段使用 ASCII 双引号，字符串中的换行和控制字符必须转义；结构完整性优先。");
		string fixedContract = contract.ToString().TrimEnd();
		string dynamicContext = context?.CompactWorldContext ?? "";
		StringBuilder system = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(editablePrompt))
		{
			system.AppendLine(editablePrompt);
			system.AppendLine();
		}
		system.AppendLine(fixedContract);
		system.AppendLine();
		system.AppendLine("【目标王国动态快照】");
		system.Append(dynamicContext);
		string systemPrompt = system.ToString().TrimEnd();
		PolicySystemLog.Write("Context", "prompt",
			"messageCount=1"
			+ " systemPromptChars=" + systemPrompt.Length.ToString(CultureInfo.InvariantCulture)
			+ " editablePromptChars=" + editablePrompt.Length.ToString(CultureInfo.InvariantCulture)
			+ " fixedContractChars=" + fixedContract.Length.ToString(CultureInfo.InvariantCulture)
			+ " dynamicContextChars=" + dynamicContext.Length.ToString(CultureInfo.InvariantCulture)
			+ " targets=" + targetCount.ToString(CultureInfo.InvariantCulture));
		return new NpcPolicyPrompt
		{
			SystemPrompt = systemPrompt
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
			if (effects.Count == 0)
			{
				string rejection = "policy-normalize-rejected batch=" + (context?.BatchId ?? "")
					+ " kingdom=" + (target.KingdomId ?? "")
					+ " policy=" + Limit(raw?.PolicyName ?? "", MaxNameChars)
					+ " reason=no-valid-effects";
				Log(rejection);
				PolicyTraceLog("policy-normalize-rejected", rejection, "该目标没有可落地的非零 effects，本次不保存政策、事件或成功生成时间。");
				continue;
			}
			string policyId = FirstNonEmpty(raw?.PolicyId, "npc_ruler_policy:" + (context?.BatchId ?? "") + ":" + target.KingdomId);
			string fallbackEvent = "政策公布后，一件起初无人重视的地方插曲迅速传开，并为此后的局势留下了一个尚未被各方看清的新事实。";
			string eventPremise = CompressCompleteText(FirstNonEmpty(raw?.EventPremise, raw?.FeedbackDigest, raw?.PublicFeedback, fallbackEvent), 70, 120);
			NpcRulerPolicyRecord record = new NpcRulerPolicyRecord
			{
				Version = 3,
				PolicyId = Limit(policyId, 160),
				BatchId = context?.BatchId ?? "",
				KingdomId = target.KingdomId,
				KingdomName = target.KingdomName,
				RulerHeroId = target.RulerHeroId,
				RulerName = target.RulerName,
				CreativePremise = CompressCompleteText(FirstNonEmpty(raw?.CreativePremise, raw?.PolicyDigest, raw?.ImpactSummary,
					target.RulerName + "决定用一项只属于" + target.KingdomName + "当前处境的新政改变局面。"), 70, 120),
				PolicyName = Limit(FirstNonEmpty(raw?.PolicyName, target.KingdomName + "政令"), MaxNameChars),
				PolicyContent = FirstNonEmpty(raw?.PolicyContent, raw?.ImpactSummary, "即日起施行新的王国政令，各地须依照当前国情逐步落实。"),
				PolicyDigest = Compact(FirstNonEmpty(raw?.PolicyDigest, raw?.ImpactSummary)),
				EventPremise = eventPremise,
				PublicFeedback = Limit(FirstNonEmpty(raw?.PublicFeedback, fallbackEvent), 0),
				FeedbackTitle = Limit(FirstNonEmpty(raw?.FeedbackTitle, "《" + FirstNonEmpty(raw?.PolicyName, target.KingdomName + "政令") + "》的余波"), MaxNameChars),
				FeedbackDigest = Compact(FirstNonEmpty(raw?.FeedbackDigest, fallbackEvent)),
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
			string detail = record.PolicyContent ?? "";
			AnimusForgeWorldEventInboxEntry entry = new AnimusForgeWorldEventInboxEntry
			{
				EventId = "npc_ruler_policy:" + NormalizeKeyPart(record.PolicyId),
				EventKind = "npc_ruler_policy",
				KindLabel = "统治者政策",
				HeaderRightText = "统治者政策",
				BodySectionTitleText = "政策内容",
				ImpactSectionTitleText = "政策影响效果",
				ImpactText = BuildEffectSummary(record.Effects),
				Title = Limit(title, 90),
				Summary = Limit(FirstNonEmpty(record.ImpactSummary, record.PolicyContent), 260),
				DetailText = Limit(detail, 1200),
				KingdomId = record.KingdomId ?? "",
				KingdomName = record.KingdomName ?? "",
				ActorHeroId = record.RulerHeroId ?? "",
				ActorHeroName = record.RulerName ?? "",
				Day = Math.Max(0, record.Day),
				GameDate = record.GameDate ?? "",
				CreatedUtcTicks = record.CreatedUtcTicks > 0L ? record.CreatedUtcTicks : DateTime.UtcNow.Ticks,
				StableKey = "npc_ruler_policy:" + (record.PolicyId ?? ""),
				IsRead = false
			};
			AnimusForgeWorldEventBehavior.UpsertWorldEventForExternal(entry, markUnread: true);
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
			NpcRulerPolicyEffectDto effect = effects[effectIndex];
			if (!TryBuildNpcActiveEffectRegistration(record, effect, effectIndex, out PolicyActiveEffectRegistration registration, out string failureReason))
			{
				effect.RemainingDays = 0;
				effect.IsEnded = true;
				UpdatePolicyEffectStateForExternal(record.PolicyId, effect.EffectId, effect.TargetKingdomId, 0, isEnded: true);
				PolicySystemLog.Write("Npc", "active-rejected", "policyId=" + (record.PolicyId ?? "") + " target=" + (effect.TargetKingdomId ?? "") + " reason=" + failureReason);
				return 0;
			}
			bool created = CustomPolicyBehavior.TryRegisterPolicyActiveEffectForExternal(registration, out string effectId, out failureReason);
			bool alreadyActive = !created && (failureReason ?? "").StartsWith("重复政策效果", StringComparison.Ordinal);
			effect.EffectId = string.IsNullOrWhiteSpace(effectId) ? registration.EffectId : effectId;
			effect.RemainingDays = created || alreadyActive ? registration.DurationDays : 0;
			effect.IsEnded = !created && !alreadyActive;
			UpdatePolicyEffectStateForExternal(record.PolicyId, effect.EffectId, effect.TargetKingdomId, effect.RemainingDays, effect.IsEnded);
			return created ? 1 : 0;
		}
		catch (Exception ex)
		{
			Log("custom-policy-bridge-failed " + ex.Message);
		}
		return 0;
	}

	private static AnimusForgeWorldEventInboxEntry BuildPolicyFeedbackWorldEvent(NpcRulerPolicyRecord policy)
	{
		if (policy == null || string.IsNullOrWhiteSpace(policy.PublicFeedback)) return null;
		string eventId = "policy_feedback:" + NormalizeKeyPart(policy.PolicyId);
		return new AnimusForgeWorldEventInboxEntry
		{
			EventId = eventId,
			EventKind = "ruler_policy_feedback",
			KindLabel = "政策衍生事件",
			HeaderRightText = "关联政策：《" + (policy.PolicyName ?? "") + "》",
			BodySectionTitleText = "事件经过",
			Title = FirstNonEmpty(policy.FeedbackTitle, "《" + FirstNonEmpty(policy.PolicyName, "新政策") + "》的余波"),
			Summary = policy.FeedbackDigest ?? "",
			DetailText = policy.PublicFeedback ?? "",
			KingdomId = policy.KingdomId ?? "",
			KingdomName = policy.KingdomName ?? "",
			ActorHeroId = policy.RulerHeroId ?? "",
			ActorHeroName = policy.RulerName ?? "",
			Day = Math.Max(0, policy.Day),
			GameDate = policy.GameDate ?? "",
			CreatedUtcTicks = policy.CreatedUtcTicks > 1 ? policy.CreatedUtcTicks - 1 : DateTime.UtcNow.Ticks,
			StableKey = eventId,
			IsRead = false
		};
	}

	private static bool TryBuildNpcActiveEffectRegistration(NpcRulerPolicyRecord policy, NpcRulerPolicyEffectDto effect, int effectIndex, out PolicyActiveEffectRegistration registration, out string failureReason)
	{
		registration = null;
		failureReason = "";
		if (policy == null || effect == null || effect.DurationDays <= 0 || !TryValidateNpcPolicyEffectNumbers(effect, out int stability))
		{
			failureReason = "政策效果数据无效";
			return false;
		}
		Kingdom issuer = ResolveNpcPolicyKingdomById(policy.KingdomId);
		Kingdom target = ResolveNpcPolicyKingdomById(effect.TargetKingdomId);
		if (issuer == null || issuer.IsEliminated || target == null || target.IsEliminated)
		{
			failureReason = "发布国或目标王国不存在/已灭亡";
			return false;
		}
		if (target != issuer)
		{
			if (!issuer.IsAtWarWith(target))
			{
				failureReason = "跨国效果失效：当前未交战";
				return false;
			}
			string policyText = ((policy.PolicyName ?? "") + " " + (policy.PolicyContent ?? "")).Trim();
			if (!BuildNpcPolicyKingdomMentionCandidates(target).Any(x => !string.IsNullOrWhiteSpace(x) && policyText.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				failureReason = "跨国效果失效：政策未明确提及目标国";
				return false;
			}
		}
		if (Math.Abs(effect.ProsperityDailyDeltaPerTown) <= 0.0001f && Math.Abs(effect.FoodDailyDeltaPerTown) <= 0.0001f && Math.Abs(effect.HearthDailyDeltaPerVillage) <= 0.0001f && Math.Abs(effect.LoyaltyDailyDeltaPerTown) <= 0.0001f && Math.Abs(effect.SecurityDailyDeltaPerTown) <= 0.0001f && Math.Abs(effect.MilitiaDailyDeltaPerTown) <= 0.0001f && stability == 0)
		{
			failureReason = "没有可落地的每日数值";
			return false;
		}
		string effectId = "npc_ruler_policy:" + NormalizeKeyPart(policy.PolicyId) + ":" + NormalizeKeyPart(target.StringId) + ":" + Math.Max(0, effectIndex).ToString(CultureInfo.InvariantCulture);
		registration = new PolicyActiveEffectRegistration
		{
			EffectId = effectId,
			RecordId = policy.PolicyId ?? effectId,
			PolicyName = policy.PolicyName ?? "",
			DateText = policy.GameDate ?? "",
			SubmittedDay = Math.Max(0, policy.Day > 0 ? policy.Day : GetCurrentCampaignDay()),
			TargetKingdomId = target.StringId ?? effect.TargetKingdomId ?? "",
			TargetKingdomName = GetKingdomName(target),
			ProsperityDailyDeltaPerTown = effect.ProsperityDailyDeltaPerTown,
			FoodDailyDeltaPerTown = effect.FoodDailyDeltaPerTown,
			HearthDailyDeltaPerVillage = effect.HearthDailyDeltaPerVillage,
			LoyaltyDailyDeltaPerTown = effect.LoyaltyDailyDeltaPerTown,
			SecurityDailyDeltaPerTown = effect.SecurityDailyDeltaPerTown,
			MilitiaDailyDeltaPerTown = effect.MilitiaDailyDeltaPerTown,
			KingdomStabilityDailyDelta = stability,
			DurationDays = effect.DurationDays,
			Reason = effect.Reason ?? policy.ImpactSummary ?? ""
		};
		return true;
	}

	private static void InvokeNpcRulerPolicyWeeklyMaterialBridge(NpcRulerPolicyRecord record)
	{
		try
		{
			if (record == null)
			{
				return;
			}
			RecordUnifiedPolicyWeeklyMaterial(record);
		}
		catch (Exception ex)
		{
			Log("weekly-material-bridge-failed policy=" + (record?.PolicyId ?? "") + " error=" + ex.Message);
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

	private static void RecordUnifiedPolicyWeeklyMaterial(NpcRulerPolicyRecord policy)
	{
		if (policy == null || string.IsNullOrWhiteSpace(policy.PolicyId))
		{
			return;
		}
		List<NpcRulerPolicyEffectDto> effects = (policy.Effects ?? new List<NpcRulerPolicyEffectDto>()).Where(x => x != null && !x.IsEnded && x.RemainingDays > 0 && x.DurationDays > 0).ToList();
		bool multipleTargets = effects.Select(x => (x.TargetKingdomId ?? "").Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;
		for (int i = 0; i < effects.Count; i++)
		{
			NpcRulerPolicyEffectDto effect = effects[i];
			string targetId = FirstNonEmpty(effect.TargetKingdomId, policy.KingdomId);
			if (string.IsNullOrWhiteSpace(targetId)) continue;
			string targetName = Limit(FirstNonEmpty(effect.TargetKingdomName, policy.KingdomName, targetId), 50);
			string policyName = Limit(FirstNonEmpty(policy.PolicyName, "未命名政策"), 70);
			string snapshot = Limit("统治者政策。发布国：" + Limit(FirstNonEmpty(policy.KingdomName, policy.KingdomId), 50)
				+ "。发布者：" + Limit(policy.RulerName, 50) + "。政策：《" + policyName + "》"
				+ "。政策摘要：" + Limit(policy.PolicyDigest, 140) + "。衍生事件：" + Limit(policy.FeedbackDigest, 70)
				+ "。目标王国：" + targetName + "。每日影响：" + Limit(BuildEffectSummary(new List<NpcRulerPolicyEffectDto> { effect }), 100) + "。", 320);
			string effectKey = FirstNonEmpty(effect.EffectId, targetId + ":" + i.ToString(CultureInfo.InvariantCulture));
			bool foreign = !string.IsNullOrWhiteSpace(policy.KingdomId) && !string.Equals(targetId, policy.KingdomId, StringComparison.OrdinalIgnoreCase);
			MyBehavior.RecordPolicySystemWeeklyMaterialForExternal("ruler_policy", "统治者政策 - " + targetName + " / " + policyName, snapshot,
				"unified_policy:" + policy.PolicyId + ":" + effectKey, targetId, multipleTargets || foreign,
				policy.RulerHeroId ?? "", policy.KingdomId ?? "", Math.Max(0, policy.Day), policy.GameDate ?? "");
			PolicySystemLog.Write("Weekly", "material-recorded", "policyId=" + policy.PolicyId + " target=" + targetId + " chars=" + snapshot.Length.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static bool TryDeserializePolicyRecords(string json, out List<NpcRulerPolicyRecord> records, out Exception exception)
	{
		records = new List<NpcRulerPolicyRecord>();
		exception = null;
		try
		{
			string compatibleJson = NormalizeGeneratedPolicyEventFieldNames(json);
			if (compatibleJson.TrimStart().StartsWith("[", StringComparison.Ordinal))
			{
				records = JsonConvert.DeserializeObject<List<NpcRulerPolicyRecord>>(compatibleJson) ?? new List<NpcRulerPolicyRecord>();
			}
			else
			{
				NpcRulerPolicyResponse response = JsonConvert.DeserializeObject<NpcRulerPolicyResponse>(compatibleJson);
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

	private static string NormalizeGeneratedPolicyEventFieldNames(string json)
	{
		string compatible = json ?? "";
		compatible = Regex.Replace(compatible, @"""derivedEventPremise""(?=\s*:)", "\"eventPremise\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		compatible = Regex.Replace(compatible, @"""derivedEventTitle""(?=\s*:)", "\"feedbackTitle\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		compatible = Regex.Replace(compatible, @"""derivedEventContent""(?=\s*:)", "\"publicFeedback\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		compatible = Regex.Replace(compatible, @"""derivedEventDigest""(?=\s*:)", "\"feedbackDigest\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		return compatible;
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
			string compatibleFragment = NormalizeGeneratedPolicyEventFieldNames(fragment);
			try
			{
				NpcRulerPolicyRecord record = JsonConvert.DeserializeObject<NpcRulerPolicyRecord>(compatibleFragment);
				if (record != null)
				{
					records.Add(record);
					continue;
				}
			}
			catch
			{
			}
			string repaired = RepairNpcPolicyJson(compatibleFragment);
			if (string.Equals(repaired, compatibleFragment, StringComparison.Ordinal))
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
			}
			return "战争=" + (wars.Count == 0 ? "无" : string.Join("、", wars));
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
			return string.IsNullOrWhiteSpace(policies) ? "无" : Limit(policies, 180);
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
		List<NpcRulerPolicyEffectDto> validEffects = (effects ?? new List<NpcRulerPolicyEffectDto>()).Where(x => x != null).ToList();
		if (validEffects.Count == 0)
		{
			return "";
		}
		return string.Join("；", validEffects.Select(effect =>
			"【" + FirstNonEmpty(effect.TargetKingdomName, effect.TargetKingdomId, "目标王国") + "】"
			+ "每日繁荣" + FormatSigned(effect.ProsperityDailyDeltaPerTown)
			+ " 粮食" + FormatSigned(effect.FoodDailyDeltaPerTown)
			+ " 户数" + FormatSigned(effect.HearthDailyDeltaPerVillage)
			+ " 忠诚" + FormatSigned(effect.LoyaltyDailyDeltaPerTown)
			+ " 治安" + FormatSigned(effect.SecurityDailyDeltaPerTown)
			+ " 民兵" + FormatSigned(effect.MilitiaDailyDeltaPerTown)
			+ " 稳定度" + FormatSigned(effect.KingdomStabilityDailyDelta)
			+ "，持续" + effect.DurationDays.ToString(CultureInfo.InvariantCulture) + "天"));
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
		public string KnowledgeGrounding;
		public int PolicyGroundingChars;
		public int PersonalityChars;
		public int BackgroundChars;
		public string CurrentWorldFacts;
		public string PolicyMemory;
		public string RecentWorldPhenomenon;
		public string ForeignDirectPressure;
		public string MechanicalFacts;
		public int PolicyMemoryCount;
		public int RecentWorldPhenomenonCount;
		public int ForeignDirectPressureCount;
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
