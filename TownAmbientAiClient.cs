using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

/// <summary>
/// Optional, explicitly authorized AI channel for settlement ambience.  It is
/// fail-closed: a missing switch, endpoint, key, model, rate limit or token
/// budget means no request is sent and callers keep using local dialogue.
/// </summary>
public static class TownAmbientAiClient
{
	private const int CacheLimit = 48;
	private static readonly object BudgetLock = new object();
	private static readonly Queue<DateTime> RecentRequestsUtc = new Queue<DateTime>();
	private static readonly Dictionary<string, string[]> ReplyCache = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
	private static readonly Queue<string> ReplyCacheOrder = new Queue<string>();
	private static int _sessionEstimatedTokens;
	private static int _sessionRequests;
	private static int _requestInFlight;

	public static int SessionEstimatedTokens => Math.Max(0, Volatile.Read(ref _sessionEstimatedTokens));

	public static int SessionRequests => Math.Max(0, Volatile.Read(ref _sessionRequests));

	public static bool HasCompleteConfiguration()
	{
		return !string.IsNullOrWhiteSpace(DuelSettings.GetTownAmbientAiApiUrl())
			&& !string.IsNullOrWhiteSpace(DuelSettings.GetTownAmbientAiApiKey())
			&& !string.IsNullOrWhiteSpace(DuelSettings.GetTownAmbientAiModelName());
	}

	public static bool TryStartReplyGeneration(
		string cacheKey,
		string anchorText,
		string settlementName,
		string sceneTag,
		string timePeriod,
		string playerStatus,
		string topicHints,
		IReadOnlyList<TownAmbientAiSpeaker> responders,
		Action<TownAmbientAiResult> completed,
		out string skipReason)
	{
		skipReason = "";
		if (!DuelSettings.IsTownAmbientAiEnabled())
		{
			skipReason = "disabled";
			return false;
		}
		if (!HasCompleteConfiguration())
		{
			skipReason = "config_incomplete";
			return false;
		}
		if (responders == null || responders.Count == 0 || completed == null)
		{
			skipReason = "no_responders";
			return false;
		}

		string normalizedCacheKey = (cacheKey ?? "").Trim();
		lock (BudgetLock)
		{
			if (!string.IsNullOrWhiteSpace(normalizedCacheKey) && ReplyCache.TryGetValue(normalizedCacheKey, out string[] cached))
			{
				completed(new TownAmbientAiResult
				{
					Success = true,
					FromCache = true,
					Replies = cached.Take(responders.Count).ToArray(),
					SessionEstimatedTokens = _sessionEstimatedTokens,
					SessionRequests = _sessionRequests
				});
				return true;
			}
		}

		if (Interlocked.CompareExchange(ref _requestInFlight, 1, 0) != 0)
		{
			skipReason = "request_in_flight";
			return false;
		}

		object[] messages = BuildReplyMessages(anchorText, settlementName, sceneTag, timePeriod, playerStatus, topicHints, responders);
		int inputEstimate = Logger.EstimateTokensFromMessages(messages);
		int maxOutputTokens = DuelSettings.GetTownAmbientAiMaxOutputTokens();
		int reservedTokens = Math.Max(1, inputEstimate) + maxOutputTokens;
		if (!TryReserveRequest(reservedTokens, out skipReason))
		{
			Interlocked.Exchange(ref _requestInFlight, 0);
			return false;
		}

		_ = Task.Run(async delegate
		{
			TownAmbientAiResult result;
			try
			{
				result = await SendAsync(messages, maxOutputTokens, responders.Count, "town_ambient_ai").ConfigureAwait(false);
				CompleteReservation(reservedTokens, result);
				if (result.Success && !string.IsNullOrWhiteSpace(normalizedCacheKey))
				{
					RememberCache(normalizedCacheKey, result.Replies);
				}
			}
			catch (Exception ex)
			{
				ReleaseReservation(reservedTokens);
				result = new TownAmbientAiResult { Error = ex.Message };
			}
			finally
			{
				Interlocked.Exchange(ref _requestInFlight, 0);
			}
			result.SessionEstimatedTokens = SessionEstimatedTokens;
			result.SessionRequests = SessionRequests;
			try
			{
				completed(result);
			}
			catch
			{
			}
		});
		return true;
	}

	public static async Task<TownAmbientAiResult> TestConnectionAsync()
	{
		if (!HasCompleteConfiguration())
		{
			return new TownAmbientAiResult { Error = "请先填写环境 AI 的 API 地址、Key 和模型名称。" };
		}
		object[] messages =
		{
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["role"] = "system",
				["content"] = "你是接口连通性测试助手。只回复两个汉字：正常。"
			},
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["role"] = "user",
				["content"] = "测试环境AI接口。"
			}
		};
		TownAmbientAiResult result = await SendAsync(messages, 96, 0, "town_ambient_ai_test").ConfigureAwait(false);
		RecordStandaloneTestUsage(result);
		result.SessionEstimatedTokens = SessionEstimatedTokens;
		result.SessionRequests = SessionRequests;
		return result;
	}

	private static object[] BuildReplyMessages(string anchorText, string settlementName, string sceneTag, string timePeriod, string playerStatus, string topicHints, IReadOnlyList<TownAmbientAiSpeaker> responders)
	{
		string speakerList = string.Join("；", responders.Select((speaker, index) => string.Format(
			"回应者{0}：身份={1}，性别={2}",
			index + 1,
			string.IsNullOrWhiteSpace(speaker.Role) ? "居民" : speaker.Role.Trim(),
			speaker.IsFemale ? "女" : "男")));
		string system = "你为中世纪城镇生活生成极短的多人接话。只写人物真正说出口的话，不写动作、旁白、姓名、冒号、引号或身份标签。每句8到28个汉字，口语自然；必须符合回应者职业、性别、地点和上一句内容。不得凭空把普通旅人称为陛下或大人。严格输出JSON：{\"replies\":[\"第一句\",\"第二句\"]}，数组长度必须与回应者数量相同。";
		string user = string.Format(
			"地点={0}；区域={1}；时间={2}；玩家身份={3}；上一句={4}；话题提示={5}；{6}",
			string.IsNullOrWhiteSpace(settlementName) ? "定居点" : settlementName.Trim(),
			string.IsNullOrWhiteSpace(sceneTag) ? "街道" : sceneTag.Trim(),
			string.IsNullOrWhiteSpace(timePeriod) ? "白天" : timePeriod.Trim(),
			string.IsNullOrWhiteSpace(playerStatus) ? "普通旅人" : playerStatus.Trim(),
			(anchorText ?? "").Trim(),
			string.IsNullOrWhiteSpace(topicHints) ? "自然接话，不引入新主题" : topicHints.Trim(),
			speakerList);
		return new object[]
		{
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["role"] = "system",
				["content"] = system
			},
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["role"] = "user",
				["content"] = user
			}
		};
	}

	private static bool TryReserveRequest(int reservedTokens, out string reason)
	{
		reason = "";
		int requestsPerMinute = DuelSettings.GetTownAmbientAiRequestsPerMinute();
		int budget = DuelSettings.GetTownAmbientAiSessionTokenBudget();
		if (requestsPerMinute <= 0)
		{
			reason = "rate_limit_zero";
			return false;
		}
		if (budget <= 0)
		{
			reason = "budget_zero";
			return false;
		}
		lock (BudgetLock)
		{
			DateTime cutoff = DateTime.UtcNow.AddMinutes(-1);
			while (RecentRequestsUtc.Count > 0 && RecentRequestsUtc.Peek() < cutoff)
			{
				RecentRequestsUtc.Dequeue();
			}
			if (RecentRequestsUtc.Count >= requestsPerMinute)
			{
				reason = "rate_limited";
				return false;
			}
			if (_sessionEstimatedTokens + reservedTokens > budget)
			{
				reason = "session_budget_exhausted";
				return false;
			}
			_sessionEstimatedTokens += reservedTokens;
			_sessionRequests++;
			RecentRequestsUtc.Enqueue(DateTime.UtcNow);
			return true;
		}
	}

	private static void CompleteReservation(int reservedTokens, TownAmbientAiResult result)
	{
		if (result == null)
		{
			ReleaseReservation(reservedTokens);
			return;
		}
		int actual = Math.Max(0, result.InputTokens) + Math.Max(0, result.OutputTokens);
		lock (BudgetLock)
		{
			_sessionEstimatedTokens = Math.Max(0, _sessionEstimatedTokens - reservedTokens + actual);
		}
	}

	private static void RecordStandaloneTestUsage(TownAmbientAiResult result)
	{
		if (result == null)
		{
			return;
		}
		int actual = Math.Max(0, result.InputTokens) + Math.Max(0, result.OutputTokens);
		lock (BudgetLock)
		{
			_sessionEstimatedTokens += actual;
			_sessionRequests++;
		}
	}

	private static void ReleaseReservation(int reservedTokens)
	{
		lock (BudgetLock)
		{
			_sessionEstimatedTokens = Math.Max(0, _sessionEstimatedTokens - Math.Max(0, reservedTokens));
			_sessionRequests = Math.Max(0, _sessionRequests - 1);
		}
	}

	private static void RememberCache(string key, string[] replies)
	{
		if (string.IsNullOrWhiteSpace(key) || replies == null || replies.Length == 0)
		{
			return;
		}
		lock (BudgetLock)
		{
			if (!ReplyCache.ContainsKey(key))
			{
				ReplyCacheOrder.Enqueue(key);
			}
			ReplyCache[key] = replies.ToArray();
			while (ReplyCacheOrder.Count > CacheLimit)
			{
				string oldKey = ReplyCacheOrder.Dequeue();
				ReplyCache.Remove(oldKey);
			}
		}
	}

	private static async Task<TownAmbientAiResult> SendAsync(object[] messages, int maxOutputTokens, int expectedReplies, string mode)
	{
		string rawUrl = DuelSettings.GetTownAmbientAiApiUrl();
		string apiUrl = DuelSettings.GetEffectiveApiUrl(rawUrl);
		string apiKey = DuelSettings.GetTownAmbientAiApiKey();
		string model = DuelSettings.GetTownAmbientAiModelName();
		if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
		{
			return new TownAmbientAiResult { Error = "环境 AI 配置不完整。" };
		}

		Logger.RecordMessageDump("town_ambient_ai_request", messages, mode);
		try
		{
			List<object> stableMessages = messages?.ToList() ?? new List<object>();
			PromptPackage prompt = LegacyPromptPackageAdapter.FromLegacyMessages(stableMessages, maxOutputTokens, model);
			TraceContext trace = new TraceContext(
				"town-ambient-" + (mode ?? "request"),
				0,
				0,
				"town-ambient",
				"shared");
			LlmProviderSnapshot provider = new LlmProviderSnapshot("town-ambient", apiUrl, model, 45000, maxOutputTokens);
			LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => apiKey, temperature: 0.75f);
			LlmGenerateResult generated = await gateway.GenerateAsync(
				new LlmGenerateRequest(
					trace,
					provider,
					prompt,
					InteractionStage.MainReply),
				CancellationToken.None).ConfigureAwait(false);
			if (generated.Status != LlmResultStatus.Succeeded)
			{
				return new TownAmbientAiResult { Error = generated.ErrorCode };
			}
			string content = generated.RawText.Trim();
			int inputTokens = Logger.EstimateTokensFromMessages(stableMessages);
			int outputTokens = Logger.EstimateTokens(content);
			Logger.RecordTokenStats(inputTokens, outputTokens, stableMessages, content, mode, "shared_gateway");
			if (expectedReplies <= 0)
			{
				if (string.IsNullOrWhiteSpace(content))
				{
					return new TownAmbientAiResult
					{
						Error = "接口返回成功，但没有可读的模型正文。若模型只返回推理，请提高最大输出 Tokens。",
						InputTokens = inputTokens,
						OutputTokens = outputTokens,
						RawResponse = TruncateDiagnosticText(content)
					};
				}
				return new TownAmbientAiResult
				{
					Success = true,
					Replies = new[] { SanitizeSpokenLine(content) },
					InputTokens = inputTokens,
					OutputTokens = outputTokens,
					ModelReply = TruncateDiagnosticText(content)
				};
			}

			string[] replies = ParseReplies(content, expectedReplies);
			if (replies.Length == 0)
			{
				return new TownAmbientAiResult
				{
					Error = "环境 AI 返回内容无法解析为接话数组。",
					InputTokens = inputTokens,
					OutputTokens = outputTokens,
					ModelReply = TruncateDiagnosticText(content),
					RawResponse = TruncateDiagnosticText(content)
				};
			}
			return new TownAmbientAiResult
			{
				Success = true,
				Replies = replies,
				InputTokens = inputTokens,
				OutputTokens = outputTokens,
				ModelReply = TruncateDiagnosticText(content)
			};
		}
		catch (OperationCanceledException)
		{
			return new TownAmbientAiResult { Error = "环境 AI 请求超时（45秒）。" };
		}
		catch (Exception ex)
		{
			Logger.Log("TownAmbientAI", "request_exception model=" + model + " error=" + ex.Message);
			return new TownAmbientAiResult { Error = ex.Message };
		}
	}

	private static string TruncateDiagnosticText(string text)
	{
		string value = (text ?? "").Trim();
		return value.Length <= 8000 ? value : value.Substring(0, 8000) + "\n…（响应过长，已截断）";
	}

	private static int ReadUsageToken(JObject envelope, string openAiName, string anthropicName)
	{
		JToken usage = envelope?["usage"];
		if (usage == null)
		{
			return 0;
		}
		int value = (int?)usage[openAiName] ?? (int?)usage[anthropicName] ?? 0;
		if (value > 0)
		{
			return value;
		}
		if (string.Equals(openAiName, "prompt_tokens", StringComparison.OrdinalIgnoreCase))
		{
			return (int?)usage["promptTokenCount"] ?? (int?)usage["inputTokenCount"] ?? (int?)usage["input_tokens"] ?? 0;
		}
		if (string.Equals(openAiName, "completion_tokens", StringComparison.OrdinalIgnoreCase))
		{
			return (int?)usage["candidatesTokenCount"] ?? (int?)usage["outputTokenCount"] ?? (int?)usage["output_tokens"] ?? 0;
		}
		return 0;
	}

	private static string[] ParseReplies(string content, int expectedReplies)
	{
		string text = (content ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return Array.Empty<string>();
		}
		try
		{
			if (text.StartsWith("```") && text.EndsWith("```"))
			{
				int firstLineBreak = text.IndexOf('\n');
				text = firstLineBreak >= 0 ? text.Substring(firstLineBreak + 1, text.Length - firstLineBreak - 4).Trim() : text.Trim('`').Trim();
			}
			JToken parsed = JToken.Parse(text);
			JArray replies = parsed as JArray;
			if (replies == null && parsed is JObject json)
			{
				replies = json["replies"] as JArray
					?? json["responses"] as JArray
					?? json["messages"] as JArray
					?? json["answers"] as JArray
					?? json["data"] as JArray;
				if (replies == null)
				{
					JToken nested = json["replies"] ?? json["response"] ?? json["text"] ?? json["content"];
					if (nested?.Type == JTokenType.String)
					{
						return ParseReplies(nested.ToString(), expectedReplies);
					}
				}
			}
			if (replies == null)
			{
				return Array.Empty<string>();
			}
			return replies
				.Select(token => SanitizeSpokenLine(token?.ToString()))
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.Take(Math.Max(1, expectedReplies))
				.ToArray();
		}
		catch (JsonException)
		{
			return Array.Empty<string>();
		}
	}

	private static string SanitizeSpokenLine(string value)
	{
		string text = (value ?? "").Trim()
			.Replace("「", "")
			.Replace("」", "")
			.Replace("“", "")
			.Replace("”", "")
			.Replace("\"", "");
		int colon = text.IndexOf('：');
		if (colon >= 0 && colon < 12)
		{
			text = text.Substring(colon + 1).Trim();
		}
		if (text.Length > 48)
		{
			text = text.Substring(0, 48).TrimEnd('，', '。', '！', '？') + "。";
		}
		return text;
	}
}

public sealed class TownAmbientAiSpeaker
{
	public string Role { get; set; } = "居民";

	public bool IsFemale { get; set; }
}

public sealed class TownAmbientAiResult
{
	public bool Success { get; set; }

	public bool FromCache { get; set; }

	public string[] Replies { get; set; } = Array.Empty<string>();

	public int InputTokens { get; set; }

	public int OutputTokens { get; set; }

	public int SessionEstimatedTokens { get; set; }

	public int SessionRequests { get; set; }

	public string Error { get; set; } = "";

	public string ModelReply { get; set; } = "";

	public string RawResponse { get; set; } = "";
}
