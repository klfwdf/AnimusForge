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
            SpeakerName = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0296"), SpeakerAgentIndex = -1, VisibleAgentIndices = visible };
        return new ConversationMessage { EventSequence = sequence, Role = "user", Content = text, SpeakerName = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0297"), SpeakerAgentIndex = -1,
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
                EventSequence = message.EventSequence, PromptMemorySessionKey = message.PromptMemorySessionKey,
                PromptFactScopeCaptured = message.PromptFactScopeCaptured, PromptIsCurrentFact = message.PromptIsCurrentFact, GameDayIndex = message.GameDayIndex,
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

// Request-only limits. All original facts remain in history/persistence for recall.
internal const int MaxHistoricalPromptFacts = 64;
internal const int MaxHistoricalPromptFactChars = 12000;
internal static int HistoricalFactLimit(int conversationLimit) => conversationLimit > 0
    ? Math.Min(conversationLimit, MaxHistoricalPromptFacts) : MaxHistoricalPromptFacts;

internal static List<ConversationMessage> KeepAfefFactsAndRecentConversationMessages(List<ConversationMessage> messages, int maxConversationMessages)
{
    if (messages == null) return new List<ConversationMessage>();
    int conversationCount = 0, factCount = 0, factChars = 0;
    int factLimit = HistoricalFactLimit(maxConversationMessages);
    var result = new List<ConversationMessage>();
    for (int i = messages.Count - 1; i >= 0; i--)
    {
        var message = messages[i];
        if (message == null) continue;
        if (SceneHistoryMessageAssemblyOwner.IsAfefConversationMessage(message))
        {
            bool current = message.PromptFactScopeCaptured && message.PromptIsCurrentFact;
            int chars = message.Content?.Length ?? 0;
            if (!current && (factCount >= factLimit || chars > MaxHistoricalPromptFactChars - factChars)) continue;
            if (!current) { factCount++; factChars += chars; }
        }
        else
        {
            if (maxConversationMessages > 0 && conversationCount >= maxConversationMessages) continue;
            conversationCount++;
        }
        result.Add(message);
    }
    result.Reverse();
    return result;
}

internal static List<string> KeepAfefFactsAndRecentHistoryLines(List<string> lines, int maxConversationLines)
{
    if (lines == null) return new List<string>();
    int conversationCount = 0, factCount = 0, factChars = 0;
    int factLimit = HistoricalFactLimit(maxConversationLines);
    var result = new List<string>();
    for (int i = lines.Count - 1; i >= 0; i--)
    {
        string line = lines[i] ?? "";
        if (SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(line, out _))
        {
            bool current = line.StartsWith(PromptComposerTexts.Raw("history_section_scope_current"), StringComparison.Ordinal);
            if (!current && (factCount >= factLimit || line.Length > MaxHistoricalPromptFactChars - factChars)) continue;
            if (!current) { factCount++; factChars += line.Length; }
        }
        else
        {
            if (conversationCount >= Math.Max(0, maxConversationLines)) continue;
            conversationCount++;
        }
        result.Add(line);
    }
    result.Reverse();
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
		if (!text2.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0298"), StringComparison.Ordinal))
		{
			return false;
		}
		string text3 = text2.Substring(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0299").Length).Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			return false;
		}
		if (text3.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0300"), StringComparison.Ordinal) && text3.EndsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0301"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text3.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0302"), StringComparison.Ordinal) && text3.Contains(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0303")) && text3.EndsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0304"), StringComparison.Ordinal))
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
