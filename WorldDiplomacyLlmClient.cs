using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class WorldDiplomacyApiCallResult
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
	public int AttemptsUsed;
	public string ResolvedRoute = "";
	public bool ThinkingRetryPlain;
}

internal static class WorldDiplomacyLlmClient
{
	private const int DefaultMaxAttempts = 2;
	private const int MaxRetryDelaySeconds = 10;

	public static bool IsConfigured(out string errorMessage)
	{
		return TryResolveApiConfig(DuelSettings.GetSettings(), out _, out _, out _, out _, out errorMessage);
	}

	public static async Task<WorldDiplomacyApiCallResult> CallMessagesWithRetriesAsync(
		JArray messages,
		int maxTokens,
		int hardTimeoutMilliseconds,
		string source,
		long runtimeGeneration,
		int maxAttempts = DefaultMaxAttempts)
	{
		WorldDiplomacyApiCallResult finalResult = new WorldDiplomacyApiCallResult();
		JArray stableMessages = messages == null ? new JArray() : (JArray)messages.DeepClone();
		if (stableMessages.Count == 0)
		{
			finalResult.ErrorMessage = "messages are empty";
			return finalResult;
		}

		int attempts = Math.Max(1, maxAttempts);
		for (int attempt = 1; attempt <= attempts; attempt++)
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "world_diplomacy_api_attempt"))
			{
				finalResult.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				finalResult.AttemptsUsed = attempt;
				return finalResult;
			}

			WorldDiplomacyApiCallResult result = await CallOnceAsync(
				stableMessages,
				Math.Max(1, maxTokens),
				Math.Max(1000, hardTimeoutMilliseconds),
				source,
				runtimeGeneration);
			result.AttemptsUsed = attempt;
			finalResult = result;
			if (result.Success || result.IsAuthFailure || result.IsQuotaLimit || attempt >= attempts)
			{
				return result;
			}

			int delaySeconds = Math.Max(1, Math.Min(MaxRetryDelaySeconds, result.RetryAfterSeconds ?? attempt * 2));
			await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
		}
		return finalResult;
	}

	private static async Task<WorldDiplomacyApiCallResult> CallOnceAsync(
		JArray stableMessages,
		int maxTokens,
		int hardTimeoutMilliseconds,
		string source,
		long runtimeGeneration)
	{
		WorldDiplomacyApiCallResult result = new WorldDiplomacyApiCallResult();
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (!TryResolveApiConfig(settings, out string apiUrl, out string apiKey, out string modelName, out string route, out string configError))
			{
				result.ErrorMessage = configError;
				return result;
			}

			result.ResolvedRoute = route;
			JArray messages = (JArray)stableMessages.DeepClone();
			JObject body = BuildRequestBody(modelName, messages, maxTokens, ResolveTemperature(settings, route));
			DuelSettings.ApplyThinkingControls(body, apiUrl, modelName, thinkingEnabled: false, DuelSettings.ReasoningEffortHigh, out string thinkingMode);
			string requestBody = LlmApiCompat.PrepareChatRequestJson(apiUrl, body);
			Log(source, "request route=" + route
				+ " model=" + modelName
				+ " maxTokens=" + maxTokens.ToString(CultureInfo.InvariantCulture)
				+ " thinking=" + thinkingMode);

			WorldDiplomacyHttpExchange exchange = await SendAndReadAsync(
				apiUrl,
				apiKey,
				requestBody,
				hardTimeoutMilliseconds,
				runtimeGeneration,
				source + "_response",
				result);
			if (exchange == null)
			{
				return result;
			}

			try
			{
				if (ShouldRetryWithoutThinkingControls(exchange.Response, exchange.ResponseBody, thinkingMode))
				{
					exchange.Dispose();
					exchange = null;
					JObject plainBody = (JObject)body.DeepClone();
					DuelSettings.RemoveThinkingControls(plainBody);
					result.ThinkingRetryPlain = true;
					thinkingMode += "_retry_plain";
					string plainRequestBody = LlmApiCompat.PrepareChatRequestJson(apiUrl, plainBody);
					exchange = await SendAndReadAsync(
						apiUrl,
						apiKey,
						plainRequestBody,
						hardTimeoutMilliseconds,
						runtimeGeneration,
						source + "_plain_retry_response",
						result);
					if (exchange == null)
					{
						return result;
					}
				}

				return CompleteResult(exchange, result, messages, route, modelName, thinkingMode, source);
			}
			finally
			{
				exchange?.Dispose();
			}
		}
		catch (OperationCanceledException)
		{
			result.IsTimeout = true;
			result.ErrorMessage = "world diplomacy api timeout after " + hardTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms";
			return result;
		}
		catch (Exception ex)
		{
			result.ErrorMessage = ex.Message;
			Log(source, "api exception: " + ex);
			return result;
		}
	}

	private static async Task<WorldDiplomacyHttpExchange> SendAndReadAsync(
		string apiUrl,
		string apiKey,
		string requestBody,
		int hardTimeoutMilliseconds,
		long runtimeGeneration,
		string staleSource,
		WorldDiplomacyApiCallResult result)
	{
		using CancellationTokenSource timeout = new CancellationTokenSource(hardTimeoutMilliseconds);
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
		LlmApiCompat.ApplyAuthenticationHeaders(request, apiUrl, apiKey);
		request.Content = new StringContent(requestBody ?? "", Encoding.UTF8, "application/json");

		HttpResponseMessage response;
		try
		{
			response = await DuelSettings.GlobalClient.SendAsync(request, timeout.Token);
		}
		catch (OperationCanceledException)
		{
			result.IsTimeout = true;
			result.ErrorMessage = "world diplomacy api timeout after " + hardTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms";
			return null;
		}

		bool keepResponse = false;
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, staleSource))
			{
				result.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return null;
			}

			string responseBody = await response.Content.ReadAsStringAsync();
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, staleSource + "_body"))
			{
				result.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return null;
			}

			keepResponse = true;
			return new WorldDiplomacyHttpExchange(response, responseBody, requestBody);
		}
		finally
		{
			if (!keepResponse)
			{
				response.Dispose();
			}
		}
	}

	private static WorldDiplomacyApiCallResult CompleteResult(
		WorldDiplomacyHttpExchange exchange,
		WorldDiplomacyApiCallResult result,
		JArray messages,
		string route,
		string modelName,
		string thinkingMode,
		string source)
	{
		HttpResponseMessage response = exchange.Response;
		string responseBody = exchange.ResponseBody ?? "";
		result.StatusCode = (int)response.StatusCode;
		result.ResponseBody = responseBody;
		if (!response.IsSuccessStatusCode)
		{
			ApplyHttpFailure(result, response, responseBody);
			RecordTokenStats(messages, "", "world_diplomacy_api_http_error", exchange.RequestBody, route, modelName, thinkingMode, responseBody);
			return result;
		}

		try
		{
			JObject json = JObject.Parse(responseBody.Trim());
			result.FinishReason = (json.SelectToken("choices[0].finish_reason")?.ToString()
				?? json["finish_reason"]?.ToString()
				?? "").Trim();
			ApplyUsageStats(result, json);
			result.Content = (LlmApiCompat.ExtractAssistantText(json) ?? "").Trim();
		}
		catch (Exception ex)
		{
			try
			{
				result.Content = (LlmApiCompat.ExtractAssistantText(responseBody) ?? "").Trim();
			}
			catch
			{
				result.Content = "";
			}
			Log(source, "response parse failed: " + ex.Message);
		}

		string finishReason = (result.FinishReason ?? "").Trim().ToLowerInvariant();
		if (finishReason == "length")
		{
			result.IsOutputTruncated = true;
			result.ErrorMessage = "LLM output truncated because finish_reason=length";
		}
		else if (finishReason == "content_filter")
		{
			result.ErrorMessage = "LLM output blocked because finish_reason=content_filter";
		}
		else if (string.IsNullOrWhiteSpace(result.Content))
		{
			result.ErrorMessage = "LLM returned empty content";
		}
		else
		{
			result.Success = string.IsNullOrWhiteSpace(finishReason)
				|| finishReason == "stop"
				|| finishReason == "end_turn"
				|| finishReason == "completed";
			if (!result.Success)
			{
				result.ErrorMessage = "LLM request ended with finish_reason=" + finishReason;
			}
		}

		RecordTokenStats(messages, result.Content, "world_diplomacy_api", exchange.RequestBody, route, modelName, thinkingMode, responseBody);
		return result;
	}

	private static void ApplyUsageStats(WorldDiplomacyApiCallResult result, JObject json)
	{
		result.PromptTokens = ReadIntToken(json, "usage.prompt_tokens", "usage.input_tokens");
		result.CompletionTokens = ReadIntToken(json, "usage.completion_tokens", "usage.output_tokens");
		result.TotalTokens = ReadIntToken(json, "usage.total_tokens");
		result.PromptCacheHitTokens = ReadIntToken(json, "usage.prompt_cache_hit_tokens", "usage.prompt_tokens_details.cached_tokens", "usage.cache_read_input_tokens");
		result.PromptCacheMissTokens = ReadIntToken(json, "usage.prompt_cache_miss_tokens", "usage.cache_creation_input_tokens");
		if (!result.PromptCacheMissTokens.HasValue && result.PromptTokens.HasValue && result.PromptCacheHitTokens.HasValue)
		{
			result.PromptCacheMissTokens = Math.Max(0, result.PromptTokens.Value - result.PromptCacheHitTokens.Value);
		}
	}

	private static int? ReadIntToken(JObject json, params string[] paths)
	{
		foreach (string path in paths ?? Array.Empty<string>())
		{
			try
			{
				JToken token = json?.SelectToken(path);
				if (token?.Type == JTokenType.Integer)
				{
					return Math.Max(0, token.Value<int>());
				}
				if (token != null && int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
				{
					return Math.Max(0, value);
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void ApplyHttpFailure(WorldDiplomacyApiCallResult result, HttpResponseMessage response, string responseBody)
	{
		int status = (int)response.StatusCode;
		result.IsAuthFailure = response.StatusCode == HttpStatusCode.Unauthorized
			|| response.StatusCode == HttpStatusCode.Forbidden
			|| ContainsAny(responseBody, "invalid api key", "authentication failed", "unauthorized", "forbidden");
		result.IsQuotaLimit = ContainsAny(responseBody, "quota", "insufficient balance", "insufficient credit", "billing");
		result.IsRateLimit = status == 429 || ContainsAny(responseBody, "rate limit", "too many requests");
		result.IsRequestsPerMinuteLimit = result.IsRateLimit
			&& ContainsAny(responseBody, "rpm", "requests per minute", "request per minute", "requests/min");
		result.RetryAfterSeconds = ReadRetryAfterSeconds(response);
		result.ErrorMessage = "HTTP " + status.ToString(CultureInfo.InvariantCulture)
			+ (string.IsNullOrWhiteSpace(response.ReasonPhrase) ? "" : " " + response.ReasonPhrase)
			+ (string.IsNullOrWhiteSpace(responseBody) ? "" : ": " + Limit(responseBody, 1200));
	}

	private static int? ReadRetryAfterSeconds(HttpResponseMessage response)
	{
		try
		{
			if (response?.Headers?.RetryAfter?.Delta != null)
			{
				return Math.Max(1, (int)Math.Ceiling(response.Headers.RetryAfter.Delta.Value.TotalSeconds));
			}
			if (response?.Headers?.RetryAfter?.Date != null)
			{
				return Math.Max(1, (int)Math.Ceiling((response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds));
			}
		}
		catch
		{
		}
		return null;
	}

	private static JObject BuildRequestBody(string modelName, JArray messages, int maxTokens, float temperature)
	{
		return new JObject
		{
			["model"] = modelName ?? "",
			["messages"] = messages ?? new JArray(),
			["stream"] = false,
			["max_tokens"] = Math.Max(1, maxTokens),
			["temperature"] = DuelSettings.ClampApiTemperature(temperature)
		};
	}

	private static bool TryResolveApiConfig(
		DuelSettings settings,
		out string apiUrl,
		out string apiKey,
		out string modelName,
		out string route,
		out string errorMessage)
	{
		apiUrl = "";
		apiKey = "";
		modelName = "";
		route = "event_rebellion_fallback_main";
		errorMessage = "请检查 MCM 的事件/叛乱 API 或主 API 设置。";
		if (settings == null)
		{
			return false;
		}

		string eventUrl = (settings.EventAndRebellionApiUrl ?? "").Trim();
		string eventKey = (settings.EventAndRebellionApiKey ?? "").Trim();
		string eventModel = settings.GetEffectiveEventAndRebellionModelName();
		string selectedEventModel = settings.GetEventAndRebellionSelectedModelOption();
		bool hasEventField = !string.IsNullOrWhiteSpace(eventUrl)
			|| !string.IsNullOrWhiteSpace(eventKey)
			|| !string.IsNullOrWhiteSpace((settings.EventAndRebellionModelName ?? "").Trim())
			|| !string.IsNullOrWhiteSpace(selectedEventModel);
		if (hasEventField)
		{
			apiUrl = DuelSettings.GetEffectiveApiUrl(eventUrl);
			apiKey = eventKey;
			modelName = eventModel;
			if (!string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName))
			{
				route = "event_rebellion_dedicated";
				errorMessage = "";
				return true;
			}
		}

		apiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl ?? "");
		apiKey = (settings.ApiKey ?? "").Trim();
		modelName = settings.GetEffectiveMainModelName();
		if (!string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName))
		{
			route = hasEventField ? "event_rebellion_partial_fallback_main" : "event_rebellion_fallback_main";
			errorMessage = "";
			return true;
		}
		return false;
	}

	private static float ResolveTemperature(DuelSettings settings, string route)
	{
		try
		{
			return (route ?? "").StartsWith("event_rebellion_dedicated", StringComparison.OrdinalIgnoreCase)
				? settings.GetEventAndRebellionApiTemperature()
				: settings.GetMainApiTemperature();
		}
		catch
		{
			return 0.8f;
		}
	}

	private static bool ShouldRetryWithoutThinkingControls(HttpResponseMessage response, string responseBody, string thinkingMode)
	{
		return response != null
			&& !response.IsSuccessStatusCode
			&& !string.Equals(thinkingMode, "plain", StringComparison.OrdinalIgnoreCase)
			&& ContainsAny(responseBody, "thinking", "reasoning_effort", "output_config", "budget_tokens")
			&& ContainsAny(responseBody, "unsupported", "unknown", "invalid", "unexpected", "not allowed", "not supported");
	}

	private static void RecordTokenStats(
		JArray messages,
		string content,
		string mode,
		string requestBody,
		string route,
		string modelName,
		string thinkingMode,
		string responseBody)
	{
		try
		{
			string output = "[WORLD DIPLOMACY API]\nroute=" + route
				+ "\nmodel=" + modelName
				+ "\nthinking=" + thinkingMode
				+ "\ncontent=\n" + (content ?? "")
				+ "\nraw_response=\n" + Limit(responseBody, 12000);
			Logger.RecordTokenStats(
				Logger.EstimateTokensFromMessages(messages),
				Logger.EstimateTokens(content),
				messages,
				output,
				mode,
				requestBody);
		}
		catch
		{
		}
	}

	private static bool ContainsAny(string text, params string[] values)
	{
		string source = text ?? "";
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value) && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string Limit(string text, int maxChars)
	{
		string value = text ?? "";
		return value.Length <= maxChars ? value : value.Substring(0, Math.Max(0, maxChars));
	}

	private static void Log(string source, string message)
	{
		try
		{
			Logger.Log(source ?? "WorldDiplomacy", "[AF-WORLD-DIPLOMACY-API] " + (message ?? ""));
		}
		catch
		{
		}
	}

	private sealed class WorldDiplomacyHttpExchange : IDisposable
	{
		public HttpResponseMessage Response { get; }
		public string ResponseBody { get; }
		public string RequestBody { get; }

		public WorldDiplomacyHttpExchange(HttpResponseMessage response, string responseBody, string requestBody)
		{
			Response = response;
			ResponseBody = responseBody ?? "";
			RequestBody = requestBody ?? "";
		}

		public void Dispose()
		{
			Response?.Dispose();
		}
	}
}
