using System;using System.Collections.Generic;using System.Linq;
namespace AnimusForge;
// Detached request-frequency values only; this is not an authoritative memory store.
internal sealed class UncompressedMemoryPromptSnapshot {
 internal int CurrentDay,TargetAgentIndex,HistoryLineLimit,HistoryLineMinimum,HistoryLineMaximum;
 internal string CurrentMemorySessionKey="",MemoryName="",CurrentScene="",PlayerName="";
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
 var message=BuildUncompressedMemoryConversationMessage(line,snapshot.MemoryName,snapshot.TargetAgentIndex,snapshot.CurrentDay,snapshot.CurrentScene,snapshot.PlayerName);
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
 return PromptComposerTexts.Get("residual_unknown_scene");
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
internal static ConversationMessage BuildUncompressedMemoryConversationMessage(DailyMemoryLine line, string memoryName, int targetAgentIndex, int currentDay, string currentScene, string playerName = "")
	{
		if (line == null || string.IsNullOrWhiteSpace(line.Text)) return null;
		ConversationMessage message = ConversationRoleClassificationOwner.ProjectDailyMemorySpeech(line, memoryName, targetAgentIndex, playerName);
		message.PromptMemorySessionKey = line.MemorySessionKey ?? "";
        message.PromptSourceId = string.IsNullOrEmpty(line.MemoryCommitId) ? "" : line.MemoryCommitId + "|" + line.MemoryCommitPart;
		message.GameDayIndex = line.GameDayIndex;
		message.GameDate = line.GameDate ?? "";
		message.GameHour = Math.Max(0, Math.Min(23, line.GameHour));
		message.Scene = ResolveMemoryLineSceneForPrompt(line, currentDay, currentScene);
		return message;
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
return string.Equals(text, PromptComposerTexts.Raw("Scene_LocationUnknown"), StringComparison.Ordinal)
|| string.Equals(text, PromptComposerTexts.Raw("Scene_LocationWorldOrUnknown"), StringComparison.Ordinal)
|| string.Equals(text, PromptComposerTexts.Raw("Scene_LocationSomewhere"), StringComparison.Ordinal)
			|| string.Equals(text, "unknown scene", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "world map or unknown scene", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "somewhere", StringComparison.OrdinalIgnoreCase);
	}
}
