using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
	private const int RpItemIntroductionAutomaticMaxChars = 600;
	private const int RpItemIntroductionHistoryMessageLimit = 10;
	private const int RpItemIntroductionHistoryReadLimit = 40;
	private const int RpItemIntroductionHistoryMessageMaxChars = 900;
	private const int RpItemIntroductionApiMaxTokens = 360;
	private const float RpItemIntroductionApiTemperature = 0.35f;
	private const int RpItemIntroductionCompletionsPerCampaignTick = 4;
	private const int RpItemIntroductionMaxConcurrentRequests = 2;
	private const int RpItemIntroductionMaxOutstandingRequests = 32;

	private static readonly object RpItemIntroductionRuntimeLock = new object();
	private static readonly Dictionary<string, long> RpItemIntroductionLatestRequestNonces = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
	private static readonly LinkedList<RpItemIntroductionRequest> RpItemIntroductionPendingRequests = new LinkedList<RpItemIntroductionRequest>();
	private static readonly Dictionary<string, LinkedListNode<RpItemIntroductionRequest>> RpItemIntroductionQueuedRequestNodesByItem = new Dictionary<string, LinkedListNode<RpItemIntroductionRequest>>(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<long> RpItemIntroductionActiveRequestNonces = new HashSet<long>();
	private static readonly Queue<RpItemIntroductionCompletion> RpItemIntroductionCompletions = new Queue<RpItemIntroductionCompletion>();
	private static readonly Regex RpItemIntroductionCodeFenceRegex = new Regex("```(?:[A-Za-z0-9_-]+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex RpItemIntroductionActionTagRegex = new Regex("\\[(?:(?:ACTION|AD|ADP|ATT|ATP|ASS|GUI)(?::[^\\]]*)?|A:[^\\]]+|FOL|STP|END)\\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static long RpItemIntroductionRequestNonce;

	/// <summary>
	/// Immutable, caller-provided context for an NPC-created RP item's introduction.
	/// Keep the visible current exchange here because postprocess runs before all channels
	/// have persisted their newest lines into long-term dialogue history.
	/// </summary>
	public sealed class RpItemIntroductionContext
	{
		internal Hero GiverHero { get; }

		internal string NonHeroMemoryId { get; }

		internal string GiverName { get; }

		internal string CurrentPlayerText { get; }

		internal string CurrentNpcText { get; }

		internal bool IncludeNativeConversationSessionHistory { get; }

		internal RpItemIntroductionContext(Hero giverHero, string nonHeroMemoryId, string giverName, string currentPlayerText, string currentNpcText, bool includeNativeConversationSessionHistory)
		{
			GiverHero = giverHero;
			NonHeroMemoryId = nonHeroMemoryId ?? "";
			GiverName = giverName ?? "";
			CurrentPlayerText = currentPlayerText ?? "";
			CurrentNpcText = currentNpcText ?? "";
			IncludeNativeConversationSessionHistory = includeNativeConversationSessionHistory;
		}
	}

	private sealed class RpItemIntroductionDialogueLine
	{
		public string Role;

		public string Text;
	}

	private sealed class RpItemIntroductionCompletion
	{
		public string GeneratedStringId;

		public long Nonce;

		public long RuntimeGeneration;

		public string Introduction;

		public string Error;

		public string Source;
	}

	private sealed class RpItemIntroductionRequest
	{
		public string GeneratedStringId;

		public long Nonce;

		public long RuntimeGeneration;

		public object[] Messages;

		public string Source;
	}

	/// <summary>
	/// Creates the explicit context passed by native/free/scene/courier postprocess callers.
	/// Set <paramref name="includeNativeConversationSessionHistory"/> only for an active
	/// native conversation; other channels must use their own persisted owner history.
	/// </summary>
	public static RpItemIntroductionContext CreateRpItemIntroductionContextForExternal(
		Hero giverHero,
		string nonHeroMemoryId,
		string giverName,
		string currentPlayerText,
		string currentNpcText,
		bool includeNativeConversationSessionHistory = false)
	{
		string resolvedGiverName = CleanRpItemIntroductionContextText(giverName, 160);
		if (string.IsNullOrWhiteSpace(resolvedGiverName))
		{
			resolvedGiverName = giverHero?.Name?.ToString()?.Trim() ?? "";
		}
		return new RpItemIntroductionContext(
			giverHero,
			(nonHeroMemoryId ?? "").Trim(),
			resolvedGiverName,
			CleanRpItemIntroductionContextText(currentPlayerText, RpItemIntroductionHistoryMessageMaxChars),
			CleanRpItemIntroductionContextText(currentNpcText, RpItemIntroductionHistoryMessageMaxChars),
			includeNativeConversationSessionHistory);
	}

	/// <summary>
	/// Player-facing setter. Blank input is deliberately a no-op so cancelling the U-key
	/// editor never erases an existing shared item introduction.
	/// </summary>
	public static bool TrySetGeneratedRpItemIntroductionForExternal(string itemStringId, string introduction, out string error)
	{
		error = "";
		string clean = CleanRpItemIntroductionContextText(introduction, AnimusForgeTextInputSanitizer.MaxCourierLetterChars);
		if (string.IsNullOrWhiteSpace(clean))
		{
			error = "物品介绍为空，未修改已有介绍。";
			return false;
		}
		if (!TrySetGeneratedRpItemIntroductionInternal(itemStringId, clean, "player", out error))
		{
			return false;
		}
		CancelRpItemIntroductionPendingRequest(itemStringId, "player_introduction_saved");
		return true;
	}

	/// <summary>
	/// Returns only a non-empty persisted introduction. Use the detail overload for an
	/// existing generated item whose introduction is still pending or absent.
	/// </summary>
	public static bool TryGetGeneratedRpItemIntroductionForExternal(string itemStringId, uint objectId, out string introduction)
	{
		introduction = "";
		if (!TryGetGeneratedRpItemIntroductionDetailForExternal(itemStringId, objectId, out _, out string body, out _))
		{
			return false;
		}
		introduction = body;
		return !string.IsNullOrWhiteSpace(introduction);
	}

	/// <summary>
	/// Resolves a generated RP item's persistent detail without scanning a roster. The
	/// pending flag is runtime-only and is cleared on a save-load generation reset.
	/// </summary>
	public static bool TryGetGeneratedRpItemIntroductionDetailForExternal(string itemStringId, uint objectId, out string displayName, out string introduction, out bool isPending)
	{
		displayName = "";
		introduction = "";
		isPending = false;
		if (!TryGetGeneratedRpItemIntroductionRecord(itemStringId, objectId, out GeneratedRewardItemRecord record))
		{
			return false;
		}
		displayName = (record.DisplayName ?? "").Trim();
		introduction = CleanRpItemIntroductionContextText(record.RpItemIntroductionText, AnimusForgeTextInputSanitizer.MaxCourierLetterChars);
		string key = (record.GeneratedStringId ?? itemStringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(key))
		{
			lock (RpItemIntroductionRuntimeLock)
			{
				isPending = RpItemIntroductionLatestRequestNonces.ContainsKey(key);
			}
		}
		return true;
	}

	/// <summary>
	/// Starts exactly one asynchronous, no-retry auxiliary request for this successful NPC
	/// RP gift. A later gift for the same deterministic generated id supersedes its result.
	/// </summary>
	public static bool QueueNpcRpItemIntroductionForExternal(string generatedStringId, string itemName, RpItemIntroductionContext context, string logSource = null)
	{
		try
		{
			RewardSystemBehavior instance = Instance;
			string key = (generatedStringId ?? "").Trim();
			if (instance == null || !IsGeneratedRewardItemStringId(key) || !TryGetGeneratedRpItemIntroductionRecord(key, 0u, out GeneratedRewardItemRecord record))
			{
				return false;
			}
			string displayName = CleanRpItemIntroductionContextText(itemName, 512);
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = CleanRpItemIntroductionContextText(record.DisplayName, 512);
			}
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = key;
			}
			RpItemIntroductionContext effectiveContext = context ?? CreateRpItemIntroductionContextForExternal(null, null, "", null, null);
			string giverName = effectiveContext.GiverName;
			if (string.IsNullOrWhiteSpace(giverName))
			{
				giverName = "NPC";
			}
			string dialogue = BuildRpItemIntroductionDialogueSnapshot(effectiveContext, giverName);
			if (!AIConfigHandler.TryBuildRpItemIntroductionPromptsForExternal(displayName, giverName, dialogue, out string systemPrompt, out string userPrompt, out string promptError))
			{
				LogRpItemIntroduction("prompt_skipped item=" + key + " source=" + (logSource ?? "") + " error=" + (promptError ?? ""));
				return false;
			}
			object[] messages = new object[2]
			{
				new
				{
					role = "system",
					content = systemPrompt
				},
				new
				{
					role = "user",
					content = userPrompt
				}
			};
			long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
			long nonce = Interlocked.Increment(ref RpItemIntroductionRequestNonce);
			bool queued = false;
			bool queueFull = false;
			int outstandingCount = 0;
			lock (RpItemIntroductionRuntimeLock)
			{
				if (RpItemIntroductionQueuedRequestNodesByItem.TryGetValue(key, out LinkedListNode<RpItemIntroductionRequest> existingNode))
				{
					RpItemIntroductionPendingRequests.Remove(existingNode);
					RpItemIntroductionQueuedRequestNodesByItem.Remove(key);
				}
				else if (RpItemIntroductionPendingRequests.Count + RpItemIntroductionActiveRequestNonces.Count >= RpItemIntroductionMaxOutstandingRequests)
				{
					queueFull = true;
					outstandingCount = RpItemIntroductionPendingRequests.Count + RpItemIntroductionActiveRequestNonces.Count;
					// A superseded in-flight request must no longer keep the item visibly pending
					// or be allowed to overwrite the later gift after this request was rejected.
					RpItemIntroductionLatestRequestNonces.Remove(key);
				}
				if (!queueFull)
				{
					RpItemIntroductionRequest request = new RpItemIntroductionRequest
					{
						GeneratedStringId = key,
						Nonce = nonce,
						RuntimeGeneration = runtimeGeneration,
						Messages = messages,
						Source = logSource ?? ""
					};
					LinkedListNode<RpItemIntroductionRequest> node = RpItemIntroductionPendingRequests.AddLast(request);
					RpItemIntroductionQueuedRequestNodesByItem[key] = node;
					RpItemIntroductionLatestRequestNonces[key] = nonce;
					queued = true;
				}
			}
			if (!queued)
			{
				LogRpItemIntroduction("queue_full item=" + key + " source=" + (logSource ?? "") + " outstanding=" + outstandingCount.ToString() + " capacity=" + RpItemIntroductionMaxOutstandingRequests.ToString() + " pending_cleared=true");
				return false;
			}
			LogRpItemIntroduction("queued item=" + key + " nonce=" + nonce.ToString() + " source=" + (logSource ?? "") + " dialogueChars=" + dialogue.Length.ToString());
			return true;
		}
		catch (Exception ex)
		{
			LogRpItemIntroduction("queue_failed item=" + (generatedStringId ?? "") + " source=" + (logSource ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			return false;
		}
	}

	private void StartQueuedRpItemIntroductionRequestsOnCampaignTick()
	{
		List<RpItemIntroductionRequest> requestsToStart = null;
		lock (RpItemIntroductionRuntimeLock)
		{
			while (RpItemIntroductionActiveRequestNonces.Count < RpItemIntroductionMaxConcurrentRequests
				&& RpItemIntroductionPendingRequests.First != null)
			{
				LinkedListNode<RpItemIntroductionRequest> node = RpItemIntroductionPendingRequests.First;
				RpItemIntroductionPendingRequests.RemoveFirst();
				RpItemIntroductionRequest request = node.Value;
				string key = (request?.GeneratedStringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(key)
					&& RpItemIntroductionQueuedRequestNodesByItem.TryGetValue(key, out LinkedListNode<RpItemIntroductionRequest> currentNode)
					&& ReferenceEquals(currentNode, node))
				{
					RpItemIntroductionQueuedRequestNodesByItem.Remove(key);
				}
				if (request == null
					|| string.IsNullOrWhiteSpace(key)
					|| !RpItemIntroductionLatestRequestNonces.TryGetValue(key, out long latestNonce)
					|| latestNonce != request.Nonce)
				{
					continue;
				}
				if (!SaveRuntimeGuard.IsCurrentGeneration(request.RuntimeGeneration))
				{
					RpItemIntroductionLatestRequestNonces.Remove(key);
					LogRpItemIntroduction("queued_request_stale_discarded item=" + key + " nonce=" + request.Nonce.ToString());
					continue;
				}
				RpItemIntroductionActiveRequestNonces.Add(request.Nonce);
				(requestsToStart ??= new List<RpItemIntroductionRequest>(RpItemIntroductionMaxConcurrentRequests)).Add(request);
			}
		}
		foreach (RpItemIntroductionRequest request in requestsToStart ?? Enumerable.Empty<RpItemIntroductionRequest>())
		{
			Task.Run(delegate
			{
				RunNpcRpItemIntroductionRequest(request.GeneratedStringId, request.Nonce, request.RuntimeGeneration, request.Messages, request.Source);
			});
			LogRpItemIntroduction("started item=" + request.GeneratedStringId + " nonce=" + request.Nonce.ToString() + " source=" + (request.Source ?? ""));
		}
	}

	private static void RunNpcRpItemIntroductionRequest(string generatedStringId, long nonce, long runtimeGeneration, IEnumerable<object> messages, string logSource)
	{
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "rp_item_introduction_api_start"))
			{
				EnqueueRpItemIntroductionCompletion(new RpItemIntroductionCompletion
				{
					GeneratedStringId = generatedStringId,
					Nonce = nonce,
					RuntimeGeneration = runtimeGeneration,
					Error = "stale_runtime_before_api",
					Source = logSource ?? ""
				});
				return;
			}
			bool success = AIConfigHandler.TryCallAuxiliarySimpleDialogueOnceForExternal(messages, RpItemIntroductionApiMaxTokens, RpItemIntroductionApiTemperature, out string content, out string error);
			string introduction = success ? CleanAutomaticRpItemIntroductionText(content) : "";
			if (string.IsNullOrWhiteSpace(introduction))
			{
				success = false;
				if (string.IsNullOrWhiteSpace(error))
				{
					error = "empty_introduction";
				}
			}
			EnqueueRpItemIntroductionCompletion(new RpItemIntroductionCompletion
			{
				GeneratedStringId = generatedStringId,
				Nonce = nonce,
				RuntimeGeneration = runtimeGeneration,
				Introduction = introduction,
				Error = success ? "" : error,
				Source = logSource ?? ""
			});
		}
		catch (Exception ex)
		{
			EnqueueRpItemIntroductionCompletion(new RpItemIntroductionCompletion
			{
				GeneratedStringId = generatedStringId,
				Nonce = nonce,
				RuntimeGeneration = runtimeGeneration,
				Error = ex.GetType().Name + ":" + ex.Message,
				Source = logSource ?? ""
			});
		}
	}

	private static void EnqueueRpItemIntroductionCompletion(RpItemIntroductionCompletion completion)
	{
		if (completion == null)
		{
			return;
		}
		lock (RpItemIntroductionRuntimeLock)
		{
			RpItemIntroductionCompletions.Enqueue(completion);
		}
	}

	private void DrainRpItemIntroductionCompletionsOnCampaignTick()
	{
		for (int i = 0; i < RpItemIntroductionCompletionsPerCampaignTick; i++)
		{
			RpItemIntroductionCompletion completion = null;
			lock (RpItemIntroductionRuntimeLock)
			{
				if (RpItemIntroductionCompletions.Count > 0)
				{
					completion = RpItemIntroductionCompletions.Dequeue();
				}
			}
			if (completion == null)
			{
				return;
			}
			lock (RpItemIntroductionRuntimeLock)
			{
				RpItemIntroductionActiveRequestNonces.Remove(completion.Nonce);
			}
			if (SaveRuntimeGuard.IsStale(completion.RuntimeGeneration, "rp_item_introduction_commit"))
			{
				lock (RpItemIntroductionRuntimeLock)
				{
					if (RpItemIntroductionLatestRequestNonces.TryGetValue(completion.GeneratedStringId ?? "", out long staleNonce) && staleNonce == completion.Nonce)
					{
						RpItemIntroductionLatestRequestNonces.Remove(completion.GeneratedStringId ?? "");
					}
				}
				continue;
			}
			bool isCurrentRequest;
			lock (RpItemIntroductionRuntimeLock)
			{
				isCurrentRequest = RpItemIntroductionLatestRequestNonces.TryGetValue(completion.GeneratedStringId ?? "", out long latestNonce) && latestNonce == completion.Nonce;
				if (isCurrentRequest)
				{
					RpItemIntroductionLatestRequestNonces.Remove(completion.GeneratedStringId ?? "");
				}
			}
			if (!isCurrentRequest)
			{
				LogRpItemIntroduction("late_result_discarded item=" + (completion.GeneratedStringId ?? "") + " nonce=" + completion.Nonce.ToString());
				continue;
			}
			string setError = "";
			if (!string.IsNullOrWhiteSpace(completion.Introduction) && TrySetGeneratedRpItemIntroductionInternal(completion.GeneratedStringId, completion.Introduction, "npc", out setError))
			{
				LogRpItemIntroduction("committed item=" + completion.GeneratedStringId + " nonce=" + completion.Nonce.ToString() + " chars=" + completion.Introduction.Length.ToString());
			}
			else
			{
				LogRpItemIntroduction("request_failed item=" + (completion.GeneratedStringId ?? "") + " nonce=" + completion.Nonce.ToString() + " source=" + (completion.Source ?? "") + " error=" + (completion.Error ?? setError ?? ""));
			}
		}
	}

	private static void CancelRpItemIntroductionPendingRequest(string itemStringId, string reason)
	{
		string key = (itemStringId ?? "").Trim();
		if (!IsGeneratedRewardItemStringId(key))
		{
			return;
		}
		bool removedLatest = false;
		bool removedQueued = false;
		lock (RpItemIntroductionRuntimeLock)
		{
			removedLatest = RpItemIntroductionLatestRequestNonces.Remove(key);
			if (RpItemIntroductionQueuedRequestNodesByItem.TryGetValue(key, out LinkedListNode<RpItemIntroductionRequest> queuedNode))
			{
				RpItemIntroductionPendingRequests.Remove(queuedNode);
				RpItemIntroductionQueuedRequestNodesByItem.Remove(key);
				removedQueued = true;
			}
		}
		if (removedLatest || removedQueued)
		{
			LogRpItemIntroduction("pending_cancelled item=" + key + " reason=" + (reason ?? "") + " queued=" + removedQueued.ToString());
		}
	}

	private static void ClearRpItemIntroductionRuntimeState(string reason)
	{
		lock (RpItemIntroductionRuntimeLock)
		{
			RpItemIntroductionLatestRequestNonces.Clear();
			RpItemIntroductionPendingRequests.Clear();
			RpItemIntroductionQueuedRequestNodesByItem.Clear();
			RpItemIntroductionActiveRequestNonces.Clear();
			RpItemIntroductionCompletions.Clear();
		}
		LogRpItemIntroduction("runtime_cleared reason=" + (reason ?? ""));
	}

	private static void MergeRpItemIntroductionFromFallback(GeneratedRewardItemRecord preferred, GeneratedRewardItemRecord fallback)
	{
		if (preferred == null || fallback == null || !string.IsNullOrWhiteSpace(preferred.RpItemIntroductionText))
		{
			return;
		}
		string fallbackText = CleanRpItemIntroductionContextText(fallback.RpItemIntroductionText, AnimusForgeTextInputSanitizer.MaxCourierLetterChars);
		if (string.IsNullOrWhiteSpace(fallbackText))
		{
			return;
		}
		preferred.RpItemIntroductionText = fallbackText;
		preferred.RpItemIntroductionSource = string.Equals((fallback.RpItemIntroductionSource ?? "").Trim(), "player", StringComparison.OrdinalIgnoreCase) ? "player" : "npc";
		preferred.RpItemIntroductionLastTouchedDay = Math.Max(0, fallback.RpItemIntroductionLastTouchedDay);
	}

	private static bool TrySetGeneratedRpItemIntroductionInternal(string itemStringId, string introduction, string source, out string error)
	{
		error = "";
		try
		{
			RewardSystemBehavior instance = Instance;
			string key = (itemStringId ?? "").Trim();
			if (instance == null || !IsGeneratedRewardItemStringId(key))
			{
				error = "未找到生成的 RP 物品。";
				return false;
			}
			int maxChars = string.Equals(source, "npc", StringComparison.OrdinalIgnoreCase)
				? RpItemIntroductionAutomaticMaxChars
				: AnimusForgeTextInputSanitizer.MaxCourierLetterChars;
			string clean = CleanRpItemIntroductionContextText(introduction, maxChars);
			if (string.IsNullOrWhiteSpace(clean))
			{
				error = "物品介绍为空。";
				return false;
			}
			instance.EnsureGeneratedRewardItemData();
			GeneratedRewardItemRecord record = instance.GetGeneratedRewardItemRecord(key);
			if (record == null)
			{
				error = "未找到生成的 RP 物品记录。";
				return false;
			}
			record = NormalizeGeneratedRewardItemRecord(key, record);
			if (record == null)
			{
				error = "生成的 RP 物品记录无效。";
				return false;
			}
			record.RpItemIntroductionText = clean;
			record.RpItemIntroductionSource = string.Equals(source, "player", StringComparison.OrdinalIgnoreCase) ? "player" : "npc";
			record.RpItemIntroductionLastTouchedDay = GetCampaignDayIndex();
			record.LastTouchedDay = Math.Max(record.LastTouchedDay, record.RpItemIntroductionLastTouchedDay);
			instance._generatedRewardItemRecords[record.GeneratedStringId] = record;
			RegisterGeneratedRewardManifestRecord(record);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.GetType().Name + ":" + ex.Message;
			return false;
		}
	}

	private static bool TryGetGeneratedRpItemIntroductionRecord(string itemStringId, uint objectId, out GeneratedRewardItemRecord record)
	{
		record = null;
		try
		{
			string key = (itemStringId ?? "").Trim();
			if (IsGeneratedRewardItemStringId(key))
			{
				record = Instance?.GetGeneratedRewardItemRecord(key);
			}
			if (record == null && objectId != 0u)
			{
				EnsureGeneratedRewardManifestLoaded();
				lock (GeneratedRewardItemRegistrationLock)
				{
					GeneratedRewardManifestByObjectId.TryGetValue(objectId, out record);
				}
			}
			if (record == null)
			{
				return false;
			}
			record = NormalizeGeneratedRewardItemRecord(record.GeneratedStringId ?? key, record);
			return record != null;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildRpItemIntroductionDialogueSnapshot(RpItemIntroductionContext context, string giverName)
	{
		List<RpItemIntroductionDialogueLine> lines = new List<RpItemIntroductionDialogueLine>(RpItemIntroductionHistoryMessageLimit + 4);
		if (context?.GiverHero != null)
		{
			AppendRpItemIntroductionHistoryEntries(lines, MyBehavior.GetDialogueHistoryEntriesForExternal(context.GiverHero, RpItemIntroductionHistoryReadLimit));
		}
		else if (!string.IsNullOrWhiteSpace(context?.NonHeroMemoryId))
		{
			AppendRpItemIntroductionHistoryEntries(lines, MyBehavior.GetDialogueHistoryEntriesByIdForExternal(context.NonHeroMemoryId, RpItemIntroductionHistoryReadLimit));
		}
		if (context?.IncludeNativeConversationSessionHistory == true)
		{
			AppendRpItemIntroductionHistoryEntries(lines, ShoutBehavior.GetNativeConversationSessionHistoryEntriesForExternal(RpItemIntroductionHistoryReadLimit));
		}
		AppendRpItemIntroductionDialogueLine(lines, "player", context?.CurrentPlayerText);
		AppendRpItemIntroductionDialogueLine(lines, "npc", context?.CurrentNpcText);
		if (lines.Count > RpItemIntroductionHistoryMessageLimit)
		{
			lines = lines.Skip(lines.Count - RpItemIntroductionHistoryMessageLimit).ToList();
		}
		if (lines.Count == 0)
		{
			return "(No recent dialogue was available.)";
		}
		StringBuilder builder = new StringBuilder(lines.Count * 96);
		foreach (RpItemIntroductionDialogueLine line in lines)
		{
			string speaker = string.Equals(line.Role, "player", StringComparison.Ordinal) ? "Player" : giverName;
			if (string.IsNullOrWhiteSpace(speaker))
			{
				speaker = string.Equals(line.Role, "player", StringComparison.Ordinal) ? "Player" : "NPC";
			}
			builder.Append(speaker).Append(": ").AppendLine(line.Text);
		}
		return builder.ToString().Trim();
	}

	private static void AppendRpItemIntroductionHistoryEntries(List<RpItemIntroductionDialogueLine> target, IEnumerable<AnimusForgeDialogueHistoryEntry> entries)
	{
		if (target == null || entries == null)
		{
			return;
		}
		foreach (AnimusForgeDialogueHistoryEntry entry in entries)
		{
			string kind = (entry?.Kind ?? "").Trim();
			if (!string.Equals(kind, "player", StringComparison.OrdinalIgnoreCase) && !string.Equals(kind, "npc", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			AppendRpItemIntroductionDialogueLine(target, kind, entry?.Text);
		}
	}

	private static void AppendRpItemIntroductionDialogueLine(List<RpItemIntroductionDialogueLine> target, string role, string text)
	{
		if (target == null)
		{
			return;
		}
		string normalizedRole = string.Equals(role, "player", StringComparison.OrdinalIgnoreCase) ? "player" : "npc";
		string clean = CleanRpItemIntroductionContextText(text, RpItemIntroductionHistoryMessageMaxChars);
		if (string.IsNullOrWhiteSpace(clean))
		{
			return;
		}
		// Persistent and native-session histories can overlap. Compare only the recent
		// tail to avoid duplicating the current visible exchange without suppressing a
		// legitimately repeated line from an older conversation.
		int start = Math.Max(0, target.Count - 12);
		for (int i = target.Count - 1; i >= start; i--)
		{
			RpItemIntroductionDialogueLine existing = target[i];
			if (existing != null && string.Equals(existing.Role, normalizedRole, StringComparison.Ordinal) && string.Equals(existing.Text, clean, StringComparison.Ordinal))
			{
				return;
			}
		}
		target.Add(new RpItemIntroductionDialogueLine
		{
			Role = normalizedRole,
			Text = clean
		});
	}

	private static string CleanRpItemIntroductionContextText(string value, int maxChars)
	{
		return AnimusForgeTextInputSanitizer.SanitizeMultiline(value ?? "", Math.Max(1, maxChars)).Trim();
	}

	private static string CleanAutomaticRpItemIntroductionText(string value)
	{
		// Do not trust the auxiliary response to obey the editable prompt. Strip only
		// transport/action residue before persisting a compact visible description.
		string text = LlmVisibleReplyNormalizer.NormalizeComplete(value ?? "");
		text = CleanRpItemIntroductionContextText(text, RpItemIntroductionAutomaticMaxChars + 256);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = RpItemIntroductionCodeFenceRegex.Replace(text, "");
		text = RpItemIntroductionActionTagRegex.Replace(text, " ");
		text = CleanRpItemIntroductionContextText(text, RpItemIntroductionAutomaticMaxChars);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (char.IsLetterOrDigit(text[i]))
			{
				return text;
			}
		}
		return "";
	}

	private static void LogRpItemIntroduction(string message)
	{
		try
		{
			Logger.Log("RewardSystem", "[RpItemIntroduction] " + (message ?? ""));
		}
		catch
		{
		}
	}
}
