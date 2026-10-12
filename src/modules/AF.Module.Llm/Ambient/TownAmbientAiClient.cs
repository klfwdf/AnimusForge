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
			return new TownAmbientAiResult { Error = LlmSourceTexts.Get("TownAmbientErrorMissingCredentials") };
		}
		object[] messages =
		{
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["role"] = "system",
				["content"] = LlmPromptTexts.Format("AF_LLM_PROMPT_TownAmbientTestSystem", "You are an endpoint connectivity test assistant. Reply with exactly: {EXPECTED_RESPONSE}.", ("EXPECTED_RESPONSE", LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientTestExpectedResponse", "OK")))
			},
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["role"] = "user",
				["content"] = LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientTestUser", "Test the ambient AI endpoint.")
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
		string speakerList = string.Join(LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientSpeakerSeparator", "; "), responders.Select((speaker, index) => LlmPromptTexts.Format(
			"AF_LLM_PROMPT_TownAmbientSpeakerLine", "Responder {INDEX}: role={ROLE}, gender={GENDER}",
			("INDEX", (index + 1).ToString()),
			("ROLE", string.IsNullOrWhiteSpace(speaker.Role) || speaker.Role == LlmSourceTexts.Get("TownAmbientDefaultResidentRole") ? LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientResidentRole", "resident") : speaker.Role.Trim()),
			("GENDER", speaker.IsFemale ? LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientGenderFemale", "female") : LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientGenderMale", "male")))));
		string system = LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientSystem", "Generate very short multi-person exchanges for life in a medieval town. Write only words actually spoken by the characters; do not write actions, narration, names, colons, quotation marks, or identity labels. Each line must be 8 to 28 characters in English and sound conversational; match each responder's occupation, gender, location, and the previous line. Do not invent titles such as emperor or lord for an ordinary traveler. Output strict JSON: {\"replies\":[\"Line one\",\"Line two\"]}; the array length must match the number of responders.");
		string user = LlmPromptTexts.Format("AF_LLM_PROMPT_TownAmbientUser", "Location={SETTLEMENT}; area={SCENE}; time={TIME}; player status={PLAYER_STATUS}; previous line={ANCHOR}; topic hints={TOPIC_HINTS}; {RESPONDERS}",
			("SETTLEMENT", string.IsNullOrWhiteSpace(settlementName) ? LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientDefaultSettlement", "settlement") : settlementName.Trim()),
			("SCENE", string.IsNullOrWhiteSpace(sceneTag) ? LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientDefaultScene", "street") : sceneTag.Trim()),
			("TIME", string.IsNullOrWhiteSpace(timePeriod) ? LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientDefaultTime", "daytime") : timePeriod.Trim()),
			("PLAYER_STATUS", string.IsNullOrWhiteSpace(playerStatus) ? LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientDefaultPlayerStatus", "ordinary traveler") : playerStatus.Trim()),
			("ANCHOR", (anchorText ?? "").Trim()),
			("TOPIC_HINTS", string.IsNullOrWhiteSpace(topicHints) ? LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientDefaultTopics", "continue naturally without introducing a new topic") : topicHints.Trim()),
			("RESPONDERS", speakerList));
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
			return new TownAmbientAiResult { Error = LlmSourceTexts.Get("TownAmbientErrorConfigIncomplete") };
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
						Error = LlmSourceTexts.Get("TownAmbientErrorEmptyModelReply"),
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
					Error = LlmSourceTexts.Get("TownAmbientErrorParseFailure"),
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
			return new TownAmbientAiResult { Error = LlmSourceTexts.Get("TownAmbientErrorRequestTimeout") };
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
		return value.Length <= 8000 ? value : value.Substring(0, 8000) + "\n... (response truncated)";
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
			text = text.Substring(0, 48).TrimEnd(LlmSourceTexts.GetCharacterArray("TownAmbientTrailingPunctuation")) + LlmPromptTexts.Get("AF_LLM_PROMPT_SentencePeriod", ".");
		}
		return text;
	}
}

public sealed class TownAmbientAiSpeaker
{
	public string Role { get; set; } = LlmPromptTexts.Get("AF_LLM_PROMPT_TownAmbientResidentRole", "resident");

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
