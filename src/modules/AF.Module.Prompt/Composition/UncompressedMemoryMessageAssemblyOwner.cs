using System;using System.Collections.Generic;using System.Linq;
namespace AnimusForge;
// Detached request-frequency values only; this is not an authoritative memory store.
internal sealed class UncompressedMemoryPromptSnapshot {
 internal int CurrentDay,TargetAgentIndex,HistoryLineLimit,HistoryLineMinimum,HistoryLineMaximum;
 internal string CurrentMemorySessionKey="",MemoryName="",CurrentScene="";
 internal List<UncompressedMemoryDraftSnapshot> Drafts=new();
}
internal sealed class UncompressedMemoryDraftSnapshot {
 internal int GameDayIndex;internal bool HasLlmDialogue,HasCompressedBlock;
 internal List<DailyMemoryLine> Lines;
}
internal static class UncompressedMemoryMessageAssemblyOwner {
 internal static List<ConversationMessage> Assemble(UncompressedMemoryPromptSnapshot snapshot,out int rawMessageCount) {
 var result=new List<ConversationMessage>();
 foreach(var draft in snapshot.Drafts.Where(x=>x!=null).OrderBy(x=>x.GameDayIndex)) {
 bool isToday=draft.GameDayIndex==snapshot.CurrentDay;
 bool hasAfefLines=draft.Lines!=null&&draft.Lines.Any(x=>x!=null&&x.IsAfef&&!string.IsNullOrWhiteSpace(x.Text));
 if(!isToday&&(draft.HasCompressedBlock||(!draft.HasLlmDialogue&&!hasAfefLines)))continue;
 foreach(var line in draft.Lines??new List<DailyMemoryLine>()) {
 if(line==null||string.IsNullOrWhiteSpace(line.Text))continue;
 if(isToday&&IsCurrentActiveMemorySessionLine(line,snapshot.CurrentMemorySessionKey))continue;
 var message=BuildUncompressedMemoryConversationMessage(line,snapshot.MemoryName,snapshot.TargetAgentIndex,snapshot.CurrentDay,snapshot.CurrentScene);
 if(message!=null)result.Add(message);
 }
 }
 rawMessageCount=result.Count;
 return KeepUncompressedFactsAndRecentConversationMessages(result,snapshot.HistoryLineLimit,snapshot.HistoryLineMinimum,snapshot.HistoryLineMaximum);
 }
 internal static string ResolveMemoryLineSceneForPrompt(DailyMemoryLine line,int currentDay,string currentScene) {
 string scene=(line?.Scene??"").Trim();
 if(!IsUnknownMemorySceneLabel(scene))return scene;
 if(line!=null&&line.GameDayIndex==currentDay&&!IsUnknownMemorySceneLabel(currentScene))return currentScene.Trim();
 return "未知场景";
 }
internal static List<ConversationMessage> KeepUncompressedFactsAndRecentConversationMessages(List<ConversationMessage> messages, int maxConversationMessages, int minimum, int maximum)
	{
		if (messages == null || messages.Count == 0)
		{
			return messages ?? new List<ConversationMessage>();
		}
		int limit = Math.Max(minimum, Math.Min(maximum, maxConversationMessages));
		int conversationCount = 0;
		for (int i = 0; i < messages.Count; i++)
		{
			ConversationMessage message = messages[i];
			if (message != null && !string.Equals((message.Role ?? "").Trim(), "system", StringComparison.OrdinalIgnoreCase))
			{
				conversationCount++;
			}
		}
		int removeCount = conversationCount - limit;
		if (removeCount <= 0)
		{
			return messages;
		}
		List<ConversationMessage> result = new List<ConversationMessage>(messages.Count - removeCount);
		for (int i = 0; i < messages.Count; i++)
		{
			ConversationMessage message = messages[i];
			bool isFact = message != null && string.Equals((message.Role ?? "").Trim(), "system", StringComparison.OrdinalIgnoreCase);
			if (!isFact && removeCount > 0)
			{
				removeCount--;
				continue;
			}
			result.Add(message);
		}
		return result;
	}
internal static ConversationMessage BuildUncompressedMemoryConversationMessage(DailyMemoryLine line, string memoryName, int targetAgentIndex, int currentDay, string currentScene)
	{
		if (line == null || string.IsNullOrWhiteSpace(line.Text))
		{
			return null;
		}
		string text = (line.Text ?? "").Replace("\r", "").Trim();
		string speaker = string.IsNullOrWhiteSpace(line.Speaker) ? (line.IsAfef ? "AFEF" : "记录") : line.Speaker.Trim();
		string npcName = string.IsNullOrWhiteSpace(memoryName) ? "NPC" : memoryName.Trim();
		string role = "user";
		int speakerAgentIndex = -1;
		int targetIndex = line.TargetAgentIndex >= 0 ? line.TargetAgentIndex : targetAgentIndex;
		string targetName = string.IsNullOrWhiteSpace(line.TargetName) ? npcName : line.TargetName.Trim();
		if (line.IsAfef)
		{
			role = "system";
			speaker = "AFEF";
			speakerAgentIndex = -1;
			targetIndex = -1;
			targetName = "";
		}
		else if (TryParseDailyMemorySceneShoutLine(text, out var heardSpeaker, out var heardContent))
		{
			role = "assistant";
			speaker = string.IsNullOrWhiteSpace(heardSpeaker) ? "某NPC" : heardSpeaker.Trim();
			text = string.IsNullOrWhiteSpace(heardContent) ? text : heardContent.Trim();
			speakerAgentIndex = -1;
			targetIndex = -1;
			targetName = "";
		}
		else if (IsDailyMemoryLineNpcSpeech(line, npcName))
		{
			role = "assistant";
			speaker = npcName;
			speakerAgentIndex = targetAgentIndex;
			targetIndex = -1;
			targetName = "";
		}
		else if (!IsDailyMemoryLinePlayerSpeech(line, npcName))
		{
			role = "assistant";
			speakerAgentIndex = -1;
			targetIndex = -1;
			targetName = "";
		}
		return new ConversationMessage
		{
			PromptMemorySessionKey = line.MemorySessionKey ?? "",
            GameDayIndex = line.GameDayIndex,
			GameDate = line.GameDate ?? "",
			GameHour = Math.Max(0, Math.Min(23, line.GameHour)),
			Scene = ResolveMemoryLineSceneForPrompt(line, currentDay, currentScene),
			Role = role,
			Content = text,
			SpeakerName = speaker,
			SpeakerAgentIndex = speakerAgentIndex,
			TargetAgentIndex = targetIndex,
			TargetName = targetName,
			PlayerDistanceMeters = -1f
		};
	}
internal static bool IsDailyMemoryLineNpcSpeech(DailyMemoryLine line, string npcName)
	{
		if (line == null)
		{
			return false;
		}
		string speaker = (line.Speaker ?? "").Trim();
		string name = (npcName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(speaker) && !string.IsNullOrWhiteSpace(name) && string.Equals(speaker, name, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string text = (line.Text ?? "").Trim();
		return !string.IsNullOrWhiteSpace(name) && (text.StartsWith(name + ":", StringComparison.Ordinal) || text.StartsWith(name + "：", StringComparison.Ordinal));
	}
internal static bool IsDailyMemoryLinePlayerSpeech(DailyMemoryLine line, string npcName)
	{
		if (line == null)
		{
			return false;
		}
		string speaker = (line.Speaker ?? "").Trim();
		if (ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker))
		{
			return true;
		}
		string text = (line.Text ?? "").Trim();
		string name = (npcName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(name) && (text.Contains("对" + name + "说") || text.Contains("对" + name + "喊") || text.Contains("向" + name + "说") || text.Contains("向" + name + "喊")))
		{
			return true;
		}
		return text.StartsWith("玩家", StringComparison.Ordinal);
	}
internal static bool TryParseDailyMemorySceneShoutLine(string rawText, out string speaker, out string content)
	{
		speaker = "";
		content = "";
		string text = (rawText ?? "").Trim();
		if (!text.StartsWith("[场景喊话]", StringComparison.Ordinal))
		{
			return false;
		}
		string body = text.Substring("[场景喊话]".Length).Trim();
		if (string.IsNullOrWhiteSpace(body))
		{
			return true;
		}
		int delimiter = ConversationRoleClassificationOwner.FindDialogueHistorySpeakerDelimiter(body);
		if (delimiter > 0)
		{
			speaker = body.Substring(0, delimiter).Trim();
			content = body.Substring(delimiter + 1).Trim();
		}
		else
		{
			content = body;
		}
		return true;
	}
internal static bool IsCurrentActiveMemorySessionLine(DailyMemoryLine line, string currentMemorySessionKey)
	{
		if (line == null)
		{
			return false;
		}
		string lineSessionKey = (line.MemorySessionKey ?? "").Trim();
		string activeSessionKey = (currentMemorySessionKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(lineSessionKey) || string.IsNullOrWhiteSpace(activeSessionKey))
		{
			return false;
		}
		return string.Equals(lineSessionKey, activeSessionKey, StringComparison.OrdinalIgnoreCase);
	}
internal static bool IsUnknownMemorySceneLabel(string scene)
	{
		string text = (scene ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		return string.Equals(text, "未知场景", StringComparison.Ordinal)
			|| string.Equals(text, "大地图或未知场景", StringComparison.Ordinal)
			|| string.Equals(text, "某个地方", StringComparison.Ordinal);
	}
}
