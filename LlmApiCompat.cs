using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

public static class LlmApiCompat
{
	private const string AnthropicVersion = "2023-06-01";

	public static bool IsAnthropicCompatibleUrl(string apiUrl)
	{
		string text = (apiUrl ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		try
		{
			if (Uri.TryCreate(text, UriKind.Absolute, out var result))
			{
				string host = (result.Host ?? "").ToLowerInvariant();
				string path = (result.AbsolutePath ?? "").ToLowerInvariant();
				return host.Contains("anthropic") || path.Contains("/anthropic") || path.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase);
			}
		}
		catch
		{
		}
		return text.IndexOf("/anthropic", StringComparison.OrdinalIgnoreCase) >= 0 || text.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsOfficialDeepSeekUrl(string apiUrl)
	{
		try
		{
			return Uri.TryCreate((apiUrl ?? string.Empty).Trim(), UriKind.Absolute, out Uri uri)
				&& string.Equals(uri.Host, "api.deepseek.com", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public static string GetEffectiveChatApiUrl(string rawUrl)
	{
		string text = (rawUrl ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		if (IsAnthropicCompatibleUrl(text))
		{
			return BuildAnthropicMessagesUrl(text);
		}
		try
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out var result))
			{
				return text;
			}
			string path = (result.AbsolutePath ?? "").Trim();
			string normalizedPath = path.TrimEnd('/').ToLowerInvariant();
			if (normalizedPath.EndsWith("/chat/completions", StringComparison.Ordinal))
			{
				return text;
			}
			if (normalizedPath.EndsWith("/v1", StringComparison.Ordinal))
			{
				return text.TrimEnd('/') + "/chat/completions";
			}
			string suffix = text.EndsWith("/", StringComparison.Ordinal) ? "v1/chat/completions" : "/v1/chat/completions";
			return text + suffix;
		}
		catch
		{
			return text;
		}
	}

	public static string BuildModelListApiUrl(string rawApiUrl)
	{
		string text = (rawApiUrl ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out var result))
			{
				return text.TrimEnd('/') + "/models";
			}
			string path = (result.AbsolutePath ?? "").Trim();
			if (string.IsNullOrWhiteSpace(path))
			{
				path = "/v1";
			}
			string normalizedPath = path.TrimEnd('/');
			normalizedPath = StripKnownChatSuffix(normalizedPath);
			if (string.IsNullOrWhiteSpace(normalizedPath))
			{
				normalizedPath = "/v1";
			}
			if (IsAnthropicCompatibleUrl(text) && !normalizedPath.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
			{
				normalizedPath = normalizedPath.TrimEnd('/') + "/v1";
			}
			UriBuilder uriBuilder = new UriBuilder(result)
			{
				Path = normalizedPath.TrimEnd('/') + "/models",
				Query = ""
			};
			return uriBuilder.Uri.ToString();
		}
		catch
		{
			return text.TrimEnd('/') + "/models";
		}
	}

	public static string PrepareChatRequestJson(string apiUrl, JObject openAiStylePayload)
	{
		if (openAiStylePayload == null)
		{
			return "{}";
		}
		JObject payload = IsAnthropicCompatibleUrl(apiUrl) ? ConvertOpenAiChatPayloadToAnthropic(openAiStylePayload) : (JObject)openAiStylePayload.DeepClone();
		return payload.ToString(Formatting.None);
	}

	public static JObject PrepareChatRequestPayload(string apiUrl, JObject openAiStylePayload)
	{
		if (openAiStylePayload == null)
		{
			return new JObject();
		}
		return IsAnthropicCompatibleUrl(apiUrl) ? ConvertOpenAiChatPayloadToAnthropic(openAiStylePayload) : (JObject)openAiStylePayload.DeepClone();
	}

	public static void ApplyAuthenticationHeaders(HttpRequestMessage request, string apiUrl, string apiKey)
	{
		if (request == null)
		{
			return;
		}
		string key = (apiKey ?? "").Trim();
		if (IsAnthropicCompatibleUrl(apiUrl))
		{
			request.Headers.Remove("x-api-key");
			request.Headers.Remove("anthropic-version");
			request.Headers.Remove("Authorization");
			request.Headers.TryAddWithoutValidation("x-api-key", key);
			request.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
			if (!IsOfficialAnthropicHost(apiUrl))
			{
				request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + key);
			}
			return;
		}
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
	}

	public static string ExtractAssistantText(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			return ExtractAssistantText(JObject.Parse(text)).Trim();
		}
		catch
		{
			return "";
		}
	}

	public static string ExtractAssistantText(JObject json)
	{
		if (json == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (json["choices"] is JArray choices)
		{
			foreach (JToken choice in choices)
			{
				AppendIfNotEmpty(stringBuilder, ExtractContentTokenText(choice?["message"]?["content"]));
				AppendIfNotEmpty(stringBuilder, ExtractContentTokenText(choice?["content"]));
				AppendIfNotEmpty(stringBuilder, choice?["text"]?.ToString());
			}
		}
		if (stringBuilder.Length > 0)
		{
			return stringBuilder.ToString();
		}
		AppendIfNotEmpty(stringBuilder, ExtractContentTokenText(json.SelectToken("message.content")));
		AppendIfNotEmpty(stringBuilder, ExtractContentTokenText(json["content"]));
		AppendIfNotEmpty(stringBuilder, json.SelectToken("output_text")?.ToString());
		AppendIfNotEmpty(stringBuilder, json.SelectToken("text")?.ToString());
		AppendIfNotEmpty(stringBuilder, ExtractResponseOutputText(json["output"]));
		AppendIfNotEmpty(stringBuilder, ExtractResponseOutputText(json["response"]));
		if (stringBuilder.Length > 0)
		{
			return stringBuilder.ToString();
		}
		return ExtractGeminiCandidateText(json.SelectToken("candidates[0]"));
	}

	private static string ExtractResponseOutputText(JToken output)
	{
		if (output == null)
		{
			return "";
		}
		if (output.Type == JTokenType.String)
		{
			return output.ToString();
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (output is JArray array)
		{
			foreach (JToken item in array)
			{
				AppendIfNotEmpty(stringBuilder, ExtractContentTokenText(item?["content"]));
				AppendIfNotEmpty(stringBuilder, item?["output_text"]?.ToString());
				AppendIfNotEmpty(stringBuilder, item?["text"]?.ToString());
			}
		}
		else
		{
			AppendIfNotEmpty(stringBuilder, ExtractContentTokenText(output["content"]));
			AppendIfNotEmpty(stringBuilder, output["output_text"]?.ToString());
			AppendIfNotEmpty(stringBuilder, output["text"]?.ToString());
		}
		return stringBuilder.ToString();
	}

	public static string ExtractReasoningText(JObject json)
	{
		if (json == null)
		{
			return "";
		}
		string text = ((string)json.SelectToken("choices[0].message.reasoning_content"))
			?? ((string)json.SelectToken("choices[0].message.reasoning"))
			?? ((string)json.SelectToken("reasoning_content"))
			?? ((string)json.SelectToken("reasoning"))
			?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return ExtractThinkingFromContent(json["content"]);
	}

	public static bool IsReasoningOnlyTokenLimitResponse(string responseBody, out int completionTokens, out int reasoningTokens)
	{
		completionTokens = 0;
		reasoningTokens = 0;
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		try
		{
			return IsReasoningOnlyTokenLimitResponse(JObject.Parse(text), out completionTokens, out reasoningTokens);
		}
		catch
		{
			return false;
		}
	}

	public static bool IsReasoningOnlyTokenLimitResponse(JObject json, out int completionTokens, out int reasoningTokens)
	{
		completionTokens = ReadNonNegativeInt(json?.SelectToken("usage.completion_tokens"));
		reasoningTokens = ReadNonNegativeInt(json?.SelectToken("usage.completion_tokens_details.reasoning_tokens"));
		if (json == null || !string.IsNullOrWhiteSpace(ExtractAssistantText(json)))
		{
			return false;
		}
		string finishReason = (json.SelectToken("choices[0].finish_reason")?.ToString() ?? "").Trim();
		bool hasReasoning = reasoningTokens > 0 || !string.IsNullOrWhiteSpace(ExtractReasoningText(json));
		bool reachedLengthLimit = string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase);
		bool allCompletionTokensWereReasoning = completionTokens > 0 && reasoningTokens >= completionTokens;
		return hasReasoning && (reachedLengthLimit || allCompletionTokensWereReasoning);
	}

	public static string ExtractStreamDeltaText(JObject chunk)
	{
		if (chunk == null)
		{
			return "";
		}
		string delta = "";
		if (chunk["choices"] is JArray choices)
		{
			foreach (JToken choice in choices)
			{
				delta = choice?["delta"]?["content"]?.ToString()
					?? choice?["message"]?["content"]?.ToString()
					?? choice?["text"]?.ToString();
				if (!string.IsNullOrEmpty(delta))
				{
					return delta;
				}
			}
		}
		delta = chunk.SelectToken("delta.text")?.ToString()
			?? chunk.SelectToken("delta.content")?.ToString()
			?? chunk.SelectToken("content_block.text")?.ToString()
			?? chunk.SelectToken("content")?.ToString()
			?? chunk.SelectToken("text")?.ToString();
		if (!string.IsNullOrEmpty(delta))
		{
			return delta;
		}
		return ExtractGeminiCandidateText(chunk.SelectToken("candidates[0]"));
	}

	public static string ExtractStreamReasoningText(JObject chunk)
	{
		if (chunk == null)
		{
			return "";
		}
		return ((string)chunk.SelectToken("choices[0].delta.reasoning_content"))
			?? ((string)chunk.SelectToken("delta.reasoning_content"))
			?? ((string)chunk.SelectToken("reasoning_content"))
			?? ((string)chunk.SelectToken("delta.thinking"))
			?? "";
	}

	public static bool IsNonContentStreamChunk(JObject chunk)
	{
		if (chunk == null)
		{
			return true;
		}
		if (chunk["choices"] is JArray choices)
		{
			if (choices.Count == 0)
			{
				return true;
			}
			bool sawReasoning = false;
			foreach (JToken choice in choices)
			{
				string content = choice?["delta"]?["content"]?.ToString()
					?? choice?["message"]?["content"]?.ToString()
					?? choice?["text"]?.ToString();
				if (!string.IsNullOrEmpty(content))
				{
					return false;
				}
				string reasoning = choice?["delta"]?["reasoning_content"]?.ToString()
					?? choice?["delta"]?["reasoning"]?.ToString()
					?? choice?["reasoning_content"]?.ToString();
				if (!string.IsNullOrEmpty(reasoning))
				{
					sawReasoning = true;
				}
			}
			if (sawReasoning)
			{
				return true;
			}
			return choices.Count == 0 || HasAnyFinishOrRoleMarker(choices);
		}
		string type = (chunk["type"]?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(type))
		{
			return type == "message_start"
				|| type == "message_delta"
				|| type == "message_stop"
				|| type == "content_block_start"
				|| type == "content_block_stop"
				|| type == "ping"
				|| type == "error";
		}
		return chunk["usage"] != null;
	}

	private static string BuildAnthropicMessagesUrl(string rawUrl)
	{
		string text = (rawUrl ?? "").Trim();
		try
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out var result))
			{
				return text.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase) ? text : text.TrimEnd('/') + "/v1/messages";
			}
			string path = (result.AbsolutePath ?? "").TrimEnd('/');
			path = StripKnownChatSuffix(path);
			if (path.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase))
			{
				path = path.Substring(0, path.Length - "/messages".Length);
			}
			if (string.IsNullOrWhiteSpace(path))
			{
				path = "/v1";
			}
			if (!path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
			{
				path = path.TrimEnd('/') + "/v1";
			}
			UriBuilder uriBuilder = new UriBuilder(result)
			{
				Path = path.TrimEnd('/') + "/messages",
				Query = ""
			};
			return uriBuilder.Uri.ToString();
		}
		catch
		{
			return text.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase) ? text : text.TrimEnd('/') + "/v1/messages";
		}
	}

	private static bool IsOfficialAnthropicHost(string apiUrl)
	{
		try
		{
			return Uri.TryCreate((apiUrl ?? "").Trim(), UriKind.Absolute, out var result)
				&& string.Equals(result.Host ?? "", "api.anthropic.com", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static string StripKnownChatSuffix(string path)
	{
		string text = (path ?? "").TrimEnd('/');
		if (text.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(0, text.Length - "/v1/chat/completions".Length);
		}
		if (text.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(0, text.Length - "/chat/completions".Length);
		}
		if (text.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(0, text.Length - "/messages".Length);
		}
		if (text.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(0, text.Length - "/messages".Length);
		}
		if (text.EndsWith("/completions", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(0, text.Length - "/completions".Length);
		}
		return text;
	}

	private static JObject ConvertOpenAiChatPayloadToAnthropic(JObject payload)
	{
		JObject result = new JObject
		{
			["model"] = payload["model"]?.ToString() ?? "",
			["max_tokens"] = ResolveAnthropicMaxTokens(payload)
		};
		if (payload["stream"]?.Type == JTokenType.Boolean && payload["stream"].Value<bool>())
		{
			result["stream"] = true;
		}
		if (payload["temperature"] != null)
		{
			result["temperature"] = ClampDouble(payload["temperature"].Value<double>(), 0.0, 1.0);
		}
		if (payload["top_p"] != null)
		{
			result["top_p"] = ClampDouble(payload["top_p"].Value<double>(), 0.0, 1.0);
		}
		List<string> systemBlocks = new List<string>();
		JArray messages = new JArray();
		if (payload["messages"] is JArray sourceMessages)
		{
			foreach (JToken message in sourceMessages)
			{
				string role = (message?["role"]?.ToString() ?? "user").Trim().ToLowerInvariant();
				JToken content = message?["content"];
				if (role == "system" || role == "developer")
				{
					string systemText = ExtractContentTokenText(content).Trim();
					if (!string.IsNullOrWhiteSpace(systemText))
					{
						systemBlocks.Add(systemText);
					}
					continue;
				}
				string anthropicRole = role == "assistant" ? "assistant" : "user";
				AddAnthropicMessage(messages, anthropicRole, ExtractContentTokenText(content));
			}
		}
		if (messages.Count == 0)
		{
			AddAnthropicMessage(messages, "user", " ");
		}
		if (systemBlocks.Count > 0)
		{
			result["system"] = string.Join("\n\n", systemBlocks);
		}
		result["messages"] = messages;
		ApplyAnthropicThinking(payload, result);
		return result;
	}

	private static int ResolveAnthropicMaxTokens(JObject payload)
	{
		try
		{
			int value = payload["max_tokens"]?.Value<int>() ?? 1024;
			return Math.Max(1, value);
		}
		catch
		{
			return 1024;
		}
	}

	private static void ApplyAnthropicThinking(JObject source, JObject target)
	{
		string type = (source.SelectToken("thinking.type")?.ToString() ?? "").Trim();
		if (!string.Equals(type, "enabled", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		int maxTokens = ResolveAnthropicMaxTokens(target);
		if (maxTokens < 2048)
		{
			return;
		}
		string effort = (source["reasoning_effort"]?.ToString() ?? source.SelectToken("output_config.effort")?.ToString() ?? "").Trim().ToLowerInvariant();
		int preferredBudget = (effort == "max" || effort == "xhigh") ? 4096 : 1024;
		int budget = Math.Min(preferredBudget, Math.Max(1024, maxTokens / 2));
		if (budget >= maxTokens)
		{
			budget = Math.Max(1024, maxTokens - 1024);
		}
		if (budget < 1024 || budget >= maxTokens)
		{
			return;
		}
		target["thinking"] = new JObject
		{
			["type"] = "enabled",
			["budget_tokens"] = budget
		};
	}

	private static void AddAnthropicMessage(JArray messages, string role, string content)
	{
		string normalizedRole = role == "assistant" ? "assistant" : "user";
		string text = string.IsNullOrEmpty(content) ? " " : content;
		if (messages.Count > 0)
		{
			JObject last = messages[messages.Count - 1] as JObject;
			if (string.Equals(last?["role"]?.ToString(), normalizedRole, StringComparison.OrdinalIgnoreCase))
			{
				string previous = last["content"]?.ToString() ?? "";
				last["content"] = string.IsNullOrWhiteSpace(previous) ? text : previous + "\n\n" + text;
				return;
			}
		}
		messages.Add(new JObject
		{
			["role"] = normalizedRole,
			["content"] = text
		});
	}

	private static string ExtractContentTokenText(JToken token)
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
				AppendIfNotEmpty(stringBuilder, ExtractContentPartText(item));
			}
			return stringBuilder.ToString();
		}
		return ExtractContentPartText(token);
	}

	private static int ReadNonNegativeInt(JToken token)
	{
		if (token != null && int.TryParse(token.ToString(), out int value) && value > 0)
		{
			return value;
		}
		return 0;
	}

	private static string ExtractContentPartText(JToken item)
	{
		if (item == null)
		{
			return "";
		}
		if (item.Type == JTokenType.String)
		{
			return item.ToString();
		}
		return item["text"]?.ToString()
			?? item["content"]?.ToString()
			?? item["value"]?.ToString()
			?? item["output_text"]?.ToString()
			?? item.SelectToken("text.value")?.ToString()
			?? "";
	}

	private static string ExtractThinkingFromContent(JToken token)
	{
		if (!(token is JArray array))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (JToken item in array)
		{
			string type = item?["type"]?.ToString() ?? "";
			if (type == "thinking")
			{
				AppendIfNotEmpty(stringBuilder, item?["thinking"]?.ToString());
			}
		}
		return stringBuilder.ToString();
	}

	private static string ExtractGeminiCandidateText(JToken candidate)
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
					AppendIfNotEmpty(stringBuilder, item?["text"]?.ToString());
				}
			}
			string directText = candidate.SelectToken("content.parts[0].text")?.ToString()
				?? candidate.SelectToken("delta.content.parts[0].text")?.ToString()
				?? candidate.SelectToken("output")?.ToString();
			AppendIfNotEmpty(stringBuilder, directText);
			return stringBuilder.ToString();
		}
		catch
		{
			return "";
		}
	}

	private static void AppendIfNotEmpty(StringBuilder builder, string text)
	{
		if (builder == null || string.IsNullOrEmpty(text))
		{
			return;
		}
		builder.Append(text);
	}

	private static bool HasAnyFinishOrRoleMarker(JArray choices)
	{
		foreach (JToken choice in choices)
		{
			if (choice?["finish_reason"] != null || choice?["delta"]?["role"] != null)
			{
				return true;
			}
		}
		return false;
	}

	private static double ClampDouble(double value, double min, double max)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return min;
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}
}
