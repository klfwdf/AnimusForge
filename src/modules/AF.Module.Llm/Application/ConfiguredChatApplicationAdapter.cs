using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using Newtonsoft.Json.Linq;
using static AnimusForge.Refactor.Adapters.LlmRequestConfigurationCaptureAdapter;
using static AnimusForge.MemoryBusinessStateOwner;
using ApiCallResult = AnimusForge.MyBehavior.ApiCallResult;

namespace AnimusForge.Refactor.Adapters;

internal static class ConfiguredChatApplicationAdapter
{
    private const int RebelKingdomNamingTimeoutMs = RebellionNamingOwner.TimeoutMs;
	internal static string TrimUniversalApiRawForLog(string text, int maxChars = 3000)
	{
		text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, maxChars) + "...";
	}

	internal static bool LooksLikeUniversalThinkingControlError(string responseBody)
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

	internal static string ExtractUniversalGeminiCandidateText(JToken candidate)
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

	internal static string ExtractUniversalStreamDelta(JObject json)
	{
		return LlmApiCompat.ExtractStreamDeltaText(json);
	}

	internal static bool IsUniversalStreamNonContentChunk(JObject json)
	{
		return LlmApiCompat.IsNonContentStreamChunk(json);
	}

	internal static string ExtractUniversalContentTokenText(JToken token)
	{
		if (token == null)
		{
			return "";
		}
		if (token.Type == JTokenType.String)
		{
			return token.ToString();
		}
		if (token is JArray array)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (JToken item in array)
			{
				string text = "";
				if (item != null)
				{
					if (item.Type == JTokenType.String)
					{
						text = item.ToString();
					}
					else
					{
						text = item["text"]?.ToString()
							?? item["content"]?.ToString()
							?? item.SelectToken("text.value")?.ToString()
							?? "";
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					stringBuilder.Append(text);
				}
			}
			return stringBuilder.ToString();
		}
		return token.ToString();
	}

	internal static string ExtractUniversalNonStreamContent(JObject json)
	{
		return LlmApiCompat.ExtractAssistantText(json);
	}

	internal static async Task<ApiCallResult> CallWeeklyReportApiDetailed(string systemPrompt, string userPrompt)
	{
		ApiCallResult legacyResult = new ApiCallResult();
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (!TryResolveUniversalApiConfig(settings, ConfiguredChatRoute.EventAndRebellion, out string apiUrl, out string apiKey, out string modelName, out string resolvedRoute, out string errorMessage))
			{
				legacyResult.ErrorMessage = errorMessage;
				return legacyResult;
			}
			int maxTokens = ResolveUniversalMaxTokens(settings, resolvedRoute);
			float temperature = ResolveUniversalApiTemperature(settings, resolvedRoute);
			ResolveUniversalThinkingSettings(settings, resolvedRoute, out bool thinkingEnabled, out string reasoningEffort);
			long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
			PromptPackage prompt = new PromptPackage(
				new[]
				{
					new PromptMessage("system", systemPrompt ?? string.Empty),
					new PromptMessage("user", userPrompt ?? string.Empty)
				},
				maxTokens,
				modelName);
			LlmProviderSnapshot provider = new LlmProviderSnapshot(
				resolvedRoute,
				apiUrl,
				modelName,
				DuelSettings.LlmRequestTimeoutMilliseconds,
				maxTokens);
			TraceContext trace = new TraceContext(
				"af-weekly-report-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
				runtimeGeneration,
				runtimeGeneration,
				"weekly-report",
				#if BANNERLORD_1_4_OR_GREATER
				"1.4"
				#else
				"1.3"
				#endif
			);
			LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(
				_ => apiKey,
				temperature: temperature,
				disableThinking: false,
				retryWithoutThinkingOnBadRequest: true,
				thinkingEnabled: thinkingEnabled,
				reasoningEffort: reasoningEffort);
			LlmGenerateResult generated = await gateway.GenerateAsync(
				new LlmGenerateRequest(trace, provider, prompt, InteractionStage.MainReply),
				CancellationToken.None).ConfigureAwait(false);
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "weekly_report_gateway_response"))
			{
				legacyResult.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return legacyResult;
			}
			LlmGenerateMetadata metadata = generated?.Metadata ?? LlmGenerateMetadata.Empty;
			legacyResult.Success = generated?.Status == LlmResultStatus.Succeeded;
			legacyResult.Content = CleanAIResponse(generated?.RawText ?? string.Empty);
			legacyResult.ErrorMessage = generated?.ErrorCode ?? string.Empty;
			legacyResult.StatusCode = metadata.StatusCode;
			legacyResult.IsRateLimit = metadata.IsRateLimit;
			legacyResult.IsRequestsPerMinuteLimit = metadata.IsRequestsPerMinuteLimit;
			legacyResult.IsQuotaLimit = metadata.IsQuotaLimit;
			legacyResult.RetryAfterSeconds = metadata.RetryAfterSeconds;
			Logger.Log("EventWeeklyReport", "[Gateway] route=" + resolvedRoute + " status=" + (metadata.StatusCode?.ToString(CultureInfo.InvariantCulture) ?? "unknown") + " success=" + legacyResult.Success + " error=" + (legacyResult.ErrorMessage ?? ""));
			return legacyResult;
		}
		catch (Exception ex)
		{
			legacyResult.ErrorMessage = LlmRetryPrompt.BuildFailureDetail(ex.Message, legacyResult.Content, "");
			return legacyResult;
		}
	}

	internal static async Task<ApiCallResult> CallRebelKingdomNamingGatewayDetailed(string systemPrompt, string userPrompt)
	{
		ApiCallResult legacyResult = new ApiCallResult();
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (!TryResolveUniversalApiConfig(settings, ConfiguredChatRoute.EventAndRebellion, out string apiUrl, out string apiKey, out string modelName, out string resolvedRoute, out string errorMessage))
			{
				legacyResult.ErrorMessage = errorMessage;
				return legacyResult;
			}
			int maxTokens = ResolveUniversalMaxTokens(settings, resolvedRoute);
			ResolveUniversalThinkingSettings(settings, resolvedRoute, out bool thinkingEnabled, out string reasoningEffort);
			PromptPackage prompt = new PromptPackage(
				new[]
				{
					new PromptMessage("system", systemPrompt ?? string.Empty),
					new PromptMessage("user", userPrompt ?? string.Empty)
				},
				maxTokens,
				modelName);
			string apiLine;
#if BANNERLORD_1_4_OR_GREATER
			apiLine = "1.4";
#else
			apiLine = "1.3";
#endif
			TraceContext trace = new TraceContext(
				"af-rebel-naming-" + runtimeGeneration.ToString(CultureInfo.InvariantCulture) + "-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
				runtimeGeneration,
				runtimeGeneration,
				"kingdom-rebellion",
				apiLine);
			LlmProviderSnapshot provider = new LlmProviderSnapshot(
				resolvedRoute,
				apiUrl,
				modelName,
				RebelKingdomNamingTimeoutMs,
				maxTokens);
			LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(
				_ => apiKey,
				temperature: ResolveUniversalApiTemperature(settings, resolvedRoute),
				disableThinking: false,
				retryWithoutThinkingOnBadRequest: true,
				thinkingEnabled: thinkingEnabled,
				reasoningEffort: reasoningEffort);
			LlmGenerateResult generated = await gateway.GenerateAsync(
				new LlmGenerateRequest(trace, provider, prompt, InteractionStage.MainReply),
				CancellationToken.None).ConfigureAwait(false);
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "rebel_naming_gateway_response"))
			{
				legacyResult.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return legacyResult;
			}
			LlmGenerateMetadata metadata = generated?.Metadata ?? LlmGenerateMetadata.Empty;
			legacyResult.Success = generated?.Status == LlmResultStatus.Succeeded;
			legacyResult.Content = CleanAIResponse(generated?.RawText ?? string.Empty);
			legacyResult.ErrorMessage = generated?.ErrorCode == "cancelled"
				? "叛乱命名请求超时（60 秒）。"
				: (generated?.ErrorCode ?? string.Empty);
			legacyResult.StatusCode = metadata.StatusCode;
			legacyResult.IsRateLimit = metadata.IsRateLimit;
			legacyResult.IsRequestsPerMinuteLimit = metadata.IsRequestsPerMinuteLimit;
			legacyResult.IsQuotaLimit = metadata.IsQuotaLimit;
			legacyResult.RetryAfterSeconds = metadata.RetryAfterSeconds;
			Logger.Log("KingdomRebellion", "[Gateway] route=" + resolvedRoute + " status=" + (metadata.StatusCode?.ToString(CultureInfo.InvariantCulture) ?? "unknown") + " success=" + legacyResult.Success + " error=" + (legacyResult.ErrorMessage ?? ""));
			return legacyResult;
		}
		catch (Exception ex)
		{
			legacyResult.ErrorMessage = LlmRetryPrompt.BuildFailureDetail(ex.Message, legacyResult.Content, "");
			return legacyResult;
		}
	}

	internal static async Task<ApiCallResult> CallAuxiliaryGatewayDetailed(string systemPrompt, string userPrompt, string source, int maxTokens, bool forceThinkingDisabled)
	{
		ApiCallResult legacyResult = new ApiCallResult();
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (!TryResolveUniversalApiConfig(settings, ConfiguredChatRoute.Auxiliary, out string apiUrl, out string apiKey, out string modelName, out string resolvedRoute, out string errorMessage))
			{
				legacyResult.ErrorMessage = errorMessage;
				return legacyResult;
			}
			int effectiveMaxTokens = maxTokens > 0
				? maxTokens
				: ResolveUniversalMaxTokens(settings, resolvedRoute);
			PromptPackage prompt = new PromptPackage(
				new[]
				{
					new PromptMessage("system", systemPrompt ?? string.Empty),
					new PromptMessage("user", userPrompt ?? string.Empty)
				},
				Math.Max(1, effectiveMaxTokens),
				modelName);
			LlmProviderSnapshot provider = new LlmProviderSnapshot(
				resolvedRoute,
				apiUrl,
				modelName,
				DuelSettings.LlmRequestTimeoutMilliseconds,
				Math.Max(1, effectiveMaxTokens));
			string apiLine;
#if BANNERLORD_1_4_OR_GREATER
			apiLine = "1.4";
#else
			apiLine = "1.3";
#endif
			TraceContext trace = new TraceContext(
				"af-auxiliary-" + (source ?? "summary") + "-" + runtimeGeneration.ToString(CultureInfo.InvariantCulture) + "-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
				runtimeGeneration,
				runtimeGeneration,
				"auxiliary",
				apiLine);
			ResolveUniversalThinkingSettings(settings, resolvedRoute, out bool thinkingEnabled, out string reasoningEffort);
			LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(
				_ => apiKey,
				temperature: ResolveUniversalApiTemperature(settings, resolvedRoute),
				disableThinking: forceThinkingDisabled,
				retryWithoutThinkingOnBadRequest: !forceThinkingDisabled,
				thinkingEnabled: thinkingEnabled,
				reasoningEffort: reasoningEffort);
			LlmGenerateResult generated = await gateway.GenerateAsync(
				new LlmGenerateRequest(trace, provider, prompt, InteractionStage.MainReply),
				CancellationToken.None).ConfigureAwait(false);
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "auxiliary_gateway_response:" + (source ?? "summary")))
			{
				legacyResult.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return legacyResult;
			}
			LlmGenerateMetadata metadata = generated?.Metadata ?? LlmGenerateMetadata.Empty;
			legacyResult.Success = generated?.Status == LlmResultStatus.Succeeded;
			legacyResult.Content = CleanAIResponse(generated?.RawText ?? string.Empty);
			legacyResult.ErrorMessage = generated?.ErrorCode ?? string.Empty;
			legacyResult.StatusCode = metadata.StatusCode;
			legacyResult.IsRateLimit = metadata.IsRateLimit;
			legacyResult.IsRequestsPerMinuteLimit = metadata.IsRequestsPerMinuteLimit;
			legacyResult.IsQuotaLimit = metadata.IsQuotaLimit;
			legacyResult.RetryAfterSeconds = metadata.RetryAfterSeconds;
			Logger.Log("Logic", "[AuxiliaryGateway] source=" + (source ?? "summary") + " route=" + resolvedRoute + " status=" + (metadata.StatusCode?.ToString(CultureInfo.InvariantCulture) ?? "unknown") + " success=" + legacyResult.Success + " error=" + (legacyResult.ErrorMessage ?? ""));
			return legacyResult;
		}
		catch (Exception ex)
		{
			legacyResult.ErrorMessage = LlmRetryPrompt.BuildFailureDetail(ex.Message, legacyResult.Content, "");
			return legacyResult;
		}
	}

	internal static async Task<ApiCallResult> CallUniversalApiDetailed(string sys, string user, bool logToEventLogs = false, string eventLogSource = "EventWeeklyReport", ConfiguredChatRoute route = ConfiguredChatRoute.Main, bool streamResponse = true, bool forceThinkingDisabled = false)
	{
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		ApiCallResult apiCallResult = new ApiCallResult();
		Action<string> apiLog = delegate(string message)
		{
			if (logToEventLogs)
			{
				Logger.LogEvent(eventLogSource, message);
			}
			else
			{
				Logger.Log("Logic", message);
			}
		};
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (!TryResolveUniversalApiConfig(settings, route, out string effectiveApiUrl, out string apiKey, out string modelName, out string resolvedRoute, out string errorMessage))
			{
				apiCallResult.ErrorMessage = errorMessage;
				return apiCallResult;
			}

			int maxTokens = ResolveUniversalMaxTokens(settings, resolvedRoute);
			float temperature = ResolveUniversalApiTemperature(settings, resolvedRoute);
			ResolveUniversalThinkingSettings(settings, resolvedRoute, out bool thinkingEnabled, out string reasoningEffort);
			PromptPackage prompt = new PromptPackage(
				new[]
				{
					new PromptMessage("system", sys ?? string.Empty),
					new PromptMessage("user", user ?? string.Empty)
				},
				maxTokens,
				modelName);
#if BANNERLORD_1_4_OR_GREATER
			string apiLine = "1.4";
#else
			string apiLine = "1.3";
#endif
			TraceContext trace = new TraceContext(
				"af-universal-" + runtimeGeneration.ToString(CultureInfo.InvariantCulture) + "-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
				runtimeGeneration,
				runtimeGeneration,
				"universal-" + (resolvedRoute ?? "main"),
				apiLine);
			LlmProviderSnapshot provider = new LlmProviderSnapshot(
				resolvedRoute,
				effectiveApiUrl,
				modelName,
				DuelSettings.LlmRequestTimeoutMilliseconds,
				maxTokens);
			LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(
				_ => apiKey,
				temperature: temperature,
				disableThinking: forceThinkingDisabled,
				retryWithoutThinkingOnBadRequest: true,
				thinkingEnabled: thinkingEnabled,
				reasoningEffort: reasoningEffort);

			apiLog("[Gateway] route=" + resolvedRoute
				+ " model=" + modelName
				+ " stream=" + streamResponse
				+ " temperature=" + temperature.ToString("0.00", CultureInfo.InvariantCulture)
				+ " thinking=" + (forceThinkingDisabled ? "disabled" : (thinkingEnabled ? reasoningEffort : "disabled"))
				+ " systemChars=" + (sys ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture)
				+ " userChars=" + (user ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture));

			ConfiguredChatGenerationExchange exchange = await gateway.GenerateExchangeAsync(
				new LlmGenerateRequest(trace, provider, prompt, InteractionStage.MainReply),
				streamResponse,
				onDelta: null,
				CancellationToken.None).ConfigureAwait(false);
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "universal_api_gateway_response:" + (eventLogSource ?? resolvedRoute)))
			{
				apiCallResult.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
				return apiCallResult;
			}

			LlmGenerateResult generated = exchange?.Result;
			LlmGenerateMetadata metadata = generated?.Metadata ?? LlmGenerateMetadata.Empty;
			string responseBody = exchange?.ResponseBody ?? string.Empty;
			if (string.IsNullOrWhiteSpace(responseBody))
			{
				responseBody = exchange?.RawStreamSample ?? string.Empty;
			}
			apiCallResult.StatusCode = exchange != null && exchange.StatusCode > 0
				? exchange.StatusCode
				: metadata.StatusCode;
			apiCallResult.ResponseBody = responseBody;
			apiCallResult.RetryAfterSeconds = metadata.RetryAfterSeconds;
			apiCallResult.IsQuotaLimit = apiCallResult.StatusCode == 429 && IsQuotaLimitResponseBody(responseBody);
			apiCallResult.IsRequestsPerMinuteLimit = apiCallResult.StatusCode == 429
				&& !apiCallResult.IsQuotaLimit
				&& IsRequestsPerMinuteLimitResponseBody(responseBody);
			apiCallResult.IsRateLimit = metadata.IsRateLimit
				|| apiCallResult.IsRequestsPerMinuteLimit
				|| (!apiCallResult.IsQuotaLimit && IsGenericRateLimitResponseBody(responseBody));
			apiCallResult.Success = generated?.Status == LlmResultStatus.Succeeded;
			apiCallResult.Content = CleanAIResponse(generated?.RawText ?? string.Empty);
			if (!apiCallResult.Success)
			{
				if (apiCallResult.StatusCode.HasValue)
				{
					apiCallResult.ErrorMessage = BuildApiCallFailureMessage(
						(HttpStatusCode)apiCallResult.StatusCode.Value,
						responseBody,
						apiCallResult.RetryAfterSeconds,
						apiCallResult.IsRateLimit,
						apiCallResult.IsRequestsPerMinuteLimit,
						apiCallResult.IsQuotaLimit);
				}
				else
				{
					apiCallResult.ErrorMessage = generated?.ErrorCode ?? "configured_gateway_failure";
				}
			}
			else if (string.IsNullOrWhiteSpace(apiCallResult.Content))
			{
				apiCallResult.Success = false;
				apiCallResult.ErrorMessage = LlmRetryPrompt.BuildFailureDetail(
					"API 已响应，但没有解析出模型回复。",
					apiCallResult.Content,
					responseBody);
			}

			JArray tokenStatsMessages = null;
			try
			{
				tokenStatsMessages = JObject.Parse(exchange?.RequestBody ?? string.Empty)["messages"] as JArray;
			}
			catch
			{
				tokenStatsMessages = new JArray
				{
					new JObject { ["role"] = "system", ["content"] = sys ?? string.Empty },
					new JObject { ["role"] = "user", ["content"] = user ?? string.Empty }
				};
			}
			if (apiCallResult.Success && tokenStatsMessages != null)
			{
				bool skipTokenStatsLog = logToEventLogs
					&& string.Equals((eventLogSource ?? string.Empty).Trim(), "EventWeeklyReport", StringComparison.OrdinalIgnoreCase);
				if (!skipTokenStatsLog)
				{
					Logger.RecordTokenStats(
						Logger.EstimateTokensFromMessages(tokenStatsMessages),
						Logger.EstimateTokens(apiCallResult.Content),
						tokenStatsMessages,
						"[UNIVERSAL API GATEWAY]\nroute=" + resolvedRoute + "\nmodel=" + modelName + "\ncontrol_mode=" + (exchange?.ControlMode ?? string.Empty) + "\nai_response=\n" + apiCallResult.Content + "\nraw_response_sample=\n" + TrimUniversalApiRawForLog(responseBody),
						"universal_api",
						exchange?.RequestBody ?? string.Empty);
				}
			}
			apiLog("[Gateway] status=" + (apiCallResult.StatusCode?.ToString(CultureInfo.InvariantCulture) ?? "unknown")
				+ " success=" + apiCallResult.Success
				+ " error=" + (apiCallResult.ErrorMessage ?? string.Empty)
				+ " contentChars=" + (apiCallResult.Content ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture));
			return apiCallResult;
		}
		catch (Exception exception)
		{
			apiLog("[ERROR] CallUniversalApi Gateway 异常: " + exception);
			apiCallResult.ErrorMessage = LlmRetryPrompt.BuildFailureDetail(
				exception.Message,
				apiCallResult.Content,
				apiCallResult.ResponseBody);
			return apiCallResult;
		}
	}

	internal static async Task<string> CallUniversalApi(string sys, string user)
	{
		ApiCallResult apiCallResult = await CallUniversalApiDetailed(sys, user);
		if (apiCallResult.Success)
		{
			return apiCallResult.Content ?? "";
		}
		return "错误: " + (apiCallResult.ErrorMessage ?? "未知错误");
	}

	internal static async Task<string> CallAuxiliaryApiTextForExternal(string sys, string user, string source, Func<bool> ownerAvailable)
	{
		try
		{
			if (!ownerAvailable())
			{
				LlmRetryPrompt.ShowFailurePopup((source ?? "ExternalAuxiliary") + " 失败", LlmRetryPrompt.BuildFailureDetail("找不到 MyBehavior，无法调用辅助模型。", ""));
				return "";
			}
			ApiCallResult apiCallResult = await CallAuxiliaryGatewayDetailed(sys, user, source, 0, forceThinkingDisabled: true);
			if (apiCallResult.Success)
			{
				return apiCallResult.Content ?? "";
			}
			LlmRetryPrompt.ShowFailurePopup((source ?? "ExternalAuxiliary") + " 失败", apiCallResult.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("辅助模型调用失败。", apiCallResult.Content, apiCallResult.ResponseBody));
			return "";
		}
		catch (Exception ex)
		{
			Logger.Log(source ?? "ExternalAuxiliary", "[ERROR] CallAuxiliaryApiTextForExternal: " + ex.Message);
			LlmRetryPrompt.ShowFailurePopup((source ?? "ExternalAuxiliary") + " 失败", LlmRetryPrompt.BuildFailureDetail(ex.Message, ""));
			return "";
		}
	}

	internal static bool IsQuotaLimitResponseBody(string responseBody)
	{
		return ContainsAnyIgnoreCase(responseBody, "quota", "balance", "insufficient", "credit", "billing", "额度", "余额", "欠费");
	}

	internal static bool IsRequestsPerMinuteLimitResponseBody(string responseBody)
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
		bool flag = ContainsAnyIgnoreCase(text, "request", "requests", "请求", "req");
		bool flag2 = ContainsAnyIgnoreCase(text, "minute", "min", "/min", "per min", "per-minute", "每分钟");
		return flag && flag2;
	}

	internal static bool IsGenericRateLimitResponseBody(string responseBody)
	{
		return ContainsAnyIgnoreCase(responseBody, "rate limit", "too many requests", "ratelimit", "限流", "请求过于频繁", "请求频率过高", "速率限制");
	}

	internal static int? TryGetRetryAfterSeconds(HttpResponseMessage response)
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
				string text = values?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x));
				if (int.TryParse((text ?? "").Trim(), out var result))
				{
					return Math.Max(0, result);
				}
				if (DateTimeOffset.TryParse(text, out var result2))
				{
					return Math.Max(0, (int)Math.Ceiling((result2 - DateTimeOffset.UtcNow).TotalSeconds));
				}
			}
		}
		catch
		{
		}
		return null;
	}

	internal static bool HasRequestsPerMinuteRateLimitHeaders(HttpResponseMessage response)
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
				if (string.IsNullOrWhiteSpace(key))
				{
					continue;
				}
				if (ContainsAnyIgnoreCase(key, "ratelimit", "rate-limit", "limit-requests", "remaining-requests", "reset-requests"))
				{
					return true;
				}
				string text = string.Join(" ", item.Value ?? Enumerable.Empty<string>());
				if (IsRequestsPerMinuteLimitResponseBody(text))
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

	internal static string BuildApiCallFailureMessage(HttpStatusCode statusCode, string responseBody, int? retryAfterSeconds, bool isRateLimit, bool isRequestsPerMinuteLimit, bool isQuotaLimit)
	{
		string text = ((int)statusCode).ToString() + " " + statusCode;
		string text2 = (responseBody ?? "").Trim();
		StringBuilder stringBuilder = new StringBuilder();
		if (isRequestsPerMinuteLimit)
		{
			stringBuilder.Append("请求疑似触发了 RPM（每分钟请求数）限流");
		}
		else if (isQuotaLimit)
		{
			stringBuilder.Append("账号额度或余额不足，导致请求被拒绝");
		}
		else if (isRateLimit)
		{
			stringBuilder.Append("请求触发了速率限制");
		}
		else
		{
			stringBuilder.Append("接口请求失败");
		}
		stringBuilder.Append("（HTTP ").Append(text).Append("）");
		if (retryAfterSeconds.HasValue)
		{
			stringBuilder.Append("，建议等待 ").Append(retryAfterSeconds.Value).Append(" 秒后再试");
		}
		return LlmRetryPrompt.BuildFailureDetail(stringBuilder.ToString(), "", text2);
	}

}
