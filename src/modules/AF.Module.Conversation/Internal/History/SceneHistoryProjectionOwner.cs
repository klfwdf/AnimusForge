using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Snapshot operations only; authoritative scene containers and single-use facts remain with their existing owners.
internal static class SceneHistoryProjectionOwner
{
    internal static ConversationMessage BuildNativeSceneBridgeMessage(string text, string kind, string speaker, string targetName,
        int targetAgentIndex, long sequence, int playerTargetAgentIndex, string playerTargetName, float distance, ConversationSpeechTextOptions options)
    {
        string normalizedKind = (kind ?? "").Trim().ToLowerInvariant();
        var visible = new List<int> { targetAgentIndex };
        if (normalizedKind == "npc") return new ConversationMessage {
            EventSequence = sequence, Role = "assistant", Content = ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(text, options),
            SpeakerName = targetName, SpeakerAgentIndex = targetAgentIndex, VisibleAgentIndices = visible };
        if (normalizedKind == "fact") return new ConversationMessage {
            EventSequence = sequence, Role = "system", Content = ConversationSpeechTextRules.NormalizeNativeConversationFactLineForPrompt(text, speaker),
            SpeakerName = "系统", SpeakerAgentIndex = -1, VisibleAgentIndices = visible };
        return new ConversationMessage { EventSequence = sequence, Role = "user", Content = text, SpeakerName = "你", SpeakerAgentIndex = -1,
            TargetAgentIndex = playerTargetAgentIndex, TargetName = playerTargetName, PlayerDistanceMeters = distance, VisibleAgentIndices = visible };
    }
    internal static List<ConversationMessage> CapturePromptMessages(IEnumerable<ConversationMessage> source)
    {
        var result = new List<ConversationMessage>();
        if (source == null) return result;
        foreach (var message in source)
        {
            if (message == null) continue;
            // Visibility was already resolved by the scene history owner. Prompt assembly
            // consumes only these scalars and must not retain any mutable visibility lists.
            result.Add(new ConversationMessage
            {
                EventSequence = message.EventSequence, PromptMemorySessionKey = message.PromptMemorySessionKey, GameDayIndex = message.GameDayIndex,
                GameDate = message.GameDate, GameHour = message.GameHour, Scene = message.Scene,
                Role = message.Role, Content = message.Content, SpeakerName = message.SpeakerName,
                SpeakerAgentIndex = message.SpeakerAgentIndex, SpeakerHeroId = message.SpeakerHeroId,
                TargetAgentIndex = message.TargetAgentIndex, TargetName = message.TargetName,
                TargetHeroId = message.TargetHeroId, PlayerDistanceMeters = message.PlayerDistanceMeters,
                VisibleAgentIndices = null, VisibleHeroIds = null
            });
        }
        return result;
    }
internal static void AppendConversationMessages(List<ConversationMessage> target, IEnumerable<ConversationMessage> source)
	{
		if (target == null || source == null)
		{
			return;
		}
		foreach (ConversationMessage msg in source)
		{
			if (msg != null)
			{
				target.Add(msg);
			}
		}
	}

internal static List<ConversationMessage> SortConversationMessagesByEventSequence(List<ConversationMessage> messages)
	{
		if (messages == null || messages.Count <= 1)
		{
			return messages ?? new List<ConversationMessage>();
		}
		return messages.Select((ConversationMessage message, int index) => new { Message = message, Index = index })
			.Where(x => x.Message != null)
			.OrderBy(x => x.Message.EventSequence > 0L ? 0 : 1)
			.ThenBy(x => x.Message.EventSequence > 0L ? x.Message.EventSequence : x.Index)
			.ThenBy(x => x.Index)
			.Select(x => x.Message)
			.ToList();
	}

internal static List<ConversationMessage> KeepAfefFactsAndRecentConversationMessages(List<ConversationMessage> messages, int maxConversationMessages)
	{
		if (messages == null || messages.Count == 0 || maxConversationMessages <= 0)
		{
			return messages ?? new List<ConversationMessage>();
		}
		int conversationCount = 0;
		HashSet<int> keepIndexes = new HashSet<int>();
		for (int i = messages.Count - 1; i >= 0; i--)
		{
			ConversationMessage message = messages[i];
			if (SceneHistoryMessageAssemblyOwner.IsAfefConversationMessage(message))
			{
				keepIndexes.Add(i);
				continue;
			}
			if (conversationCount < maxConversationMessages)
			{
				keepIndexes.Add(i);
				conversationCount++;
			}
		}
		List<ConversationMessage> result = new List<ConversationMessage>();
		for (int i = 0; i < messages.Count; i++)
		{
			if (keepIndexes.Contains(i))
			{
				result.Add(messages[i]);
			}
		}
		return result;
	}

internal static List<string> KeepAfefFactsAndRecentHistoryLines(List<string> lines, int maxConversationLines)
	{
		if (lines == null || lines.Count == 0)
		{
			return lines ?? new List<string>();
		}
		int limit = maxConversationLines;
		int conversationCount = 0;
		for (int i = 0; i < lines.Count; i++)
		{
			if (!SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(lines[i], out var _))
			{
				conversationCount++;
			}
		}
		int removeCount = conversationCount - limit;
		if (removeCount <= 0)
		{
			return lines;
		}
		List<string> result = new List<string>(lines.Count - removeCount);
		for (int i = 0; i < lines.Count; i++)
		{
			string line = lines[i];
			if (removeCount > 0 && !SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(line, out var _))
			{
				removeCount--;
				continue;
			}
			result.Add(line);
		}
		return result;
	}

internal static string NormalizeSceneHeroId(string heroId)
	{
		return (heroId ?? "").Trim();
	}
internal static bool IsSameSceneHeroId(string left, string right)
	{
		string text = NormalizeSceneHeroId(left);
		string text2 = NormalizeSceneHeroId(right);
		return !string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && string.Equals(text, text2, StringComparison.OrdinalIgnoreCase);
	}
internal static bool ContainsSceneHeroId(IEnumerable<string> heroIds, string heroId)
	{
		string text = NormalizeSceneHeroId(heroId);
		if (string.IsNullOrWhiteSpace(text) || heroIds == null)
		{
			return false;
		}
		foreach (string item in heroIds)
		{
			if (IsSameSceneHeroId(item, text))
			{
				return true;
			}
		}
		return false;
	}
internal static bool IsSceneHistoryVisibleToAgentOrHero(ConversationMessage msg, int viewerAgentIndex, string viewerHeroId)
	{
		if (msg == null)
		{
			return false;
		}
		if (viewerAgentIndex < 0)
		{
			return true;
		}
		List<int> visibleAgentIndices = msg.VisibleAgentIndices;
		List<string> visibleHeroIds = msg.VisibleHeroIds;
		bool hasAgentVisibility = visibleAgentIndices != null && visibleAgentIndices.Count > 0;
		bool hasHeroVisibility = visibleHeroIds != null && visibleHeroIds.Count > 0;
		if (!hasAgentVisibility && !hasHeroVisibility)
		{
			return true;
		}
		if (ContainsSceneHeroId(visibleHeroIds, viewerHeroId))
		{
			return true;
		}
		if (hasAgentVisibility)
		{
			for (int i = 0; i < visibleAgentIndices.Count; i++)
			{
				if (visibleAgentIndices[i] == viewerAgentIndex)
				{
					return true;
				}
			}
		}
		return false;
	}
internal static bool IsSceneHistoryMessageRelatedToHero(ConversationMessage msg, string heroId)
	{
		if (msg == null || string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		return IsSameSceneHeroId(msg.SpeakerHeroId, heroId)
			|| IsSameSceneHeroId(msg.TargetHeroId, heroId)
			|| ContainsSceneHeroId(msg.VisibleHeroIds, heroId);
	}
internal static bool DoesSceneHistoryBucketRelateToHero(IEnumerable<ConversationMessage> messages, string heroId)
	{
		if (messages == null || string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		foreach (ConversationMessage msg in messages)
		{
			if (IsSceneHistoryMessageRelatedToHero(msg, heroId))
			{
				return true;
			}
		}
		return false;
	}
internal static string BuildSceneHistoryMergeKey(ConversationMessage msg, ConversationSpeechTextOptions options)
	{
		if (msg == null)
		{
			return "";
		}
		string role = (msg.Role ?? "").Trim().ToLowerInvariant();
		if (msg.EventSequence > 0L)
		{
			return "seq|" + msg.EventSequence + "|" + role;
		}
		return SceneHistoryMessageAssemblyOwner.BuildConversationMessageDedupeKey(msg, options);
	}
internal static bool IsSingleUseSceneNpcFactText(string text)
	{
		string text2 = (text ?? "").Trim();
		if (!text2.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			return false;
		}
		string text3 = text2.Substring("[AFEF NPC行为补充]".Length).Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			return false;
		}
		if (text3.StartsWith("今天稍早时候刚与", StringComparison.Ordinal) && text3.EndsWith("见过面。", StringComparison.Ordinal))
		{
			return true;
		}
		if (text3.StartsWith("距离你上次与", StringComparison.Ordinal) && text3.Contains("见面，已有") && text3.EndsWith("天了。", StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}
internal static bool IsSceneConversationTurn(ConversationMessage msg)
	{
		string text = (msg?.Role ?? "").Trim();
		return text.Equals("user", StringComparison.OrdinalIgnoreCase) || text.Equals("assistant", StringComparison.OrdinalIgnoreCase);
	}
internal static void RemoveExpiredSingleUseSceneNpcFacts(List<ConversationMessage> history)
	{
		if (history == null || history.Count == 0)
		{
			return;
		}
		bool flag = false;
		for (int num = history.Count - 1; num >= 0; num--)
		{
			ConversationMessage conversationMessage = history[num];
			if (flag && IsSingleUseSceneNpcFactText(conversationMessage?.Content))
			{
				history.RemoveAt(num);
				continue;
			}
			if (IsSceneConversationTurn(conversationMessage))
			{
				flag = true;
			}
		}
	}
internal static string GetLatestSceneNpcUtteranceFromHistory(List<ConversationMessage> history, int targetAgentIndex)
	{
		if (history == null || history.Count == 0 || targetAgentIndex < 0)
		{
			return "";
		}
		try
		{
			bool seenCurrentPlayerTurn = false;
			for (int i = history.Count - 1; i >= 0; i--)
			{
				ConversationMessage conversationMessage = history[i];
				if (conversationMessage == null)
				{
					continue;
				}
				string text = (conversationMessage.Role ?? "").Trim();
				if (text.Equals("user", StringComparison.OrdinalIgnoreCase))
				{
					if (!seenCurrentPlayerTurn)
					{
						seenCurrentPlayerTurn = true;
						continue;
					}
					break;
				}
				if (!seenCurrentPlayerTurn || !text.Equals("assistant", StringComparison.OrdinalIgnoreCase) || conversationMessage.SpeakerAgentIndex != targetAgentIndex)
				{
					continue;
				}
				string text2 = SceneHistoryMessageAssemblyOwner.NormalizeStrictSceneAssistantContent(conversationMessage.Content, conversationMessage.SpeakerName);
				if (!string.IsNullOrWhiteSpace(text2) && !ConversationSpeechTextRules.IsLeakedPromptLineForShout(text2))
				{
					return text2;
				}
			}
		}
		catch
		{
		}
		return "";
	}
}
