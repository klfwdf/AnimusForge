using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Snapshot operations only; authoritative scene containers and single-use facts remain with their existing owners.
internal static class SceneHistoryProjectionOwner
{
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
                EventSequence = message.EventSequence, GameDayIndex = message.GameDayIndex,
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
}
