using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
public partial class MyBehavior {
private List<ConversationMessage> CaptureAndBuildUncompressedMemoryRoleMessages(Hero hero, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false)
	{
		if (hero == null)
		{
			return new List<ConversationMessage>();
		}
		string heroName = (hero.Name?.ToString() ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(heroName))
		{
			heroName = "NPC";
		}
		return CaptureAndBuildUncompressedMemoryRoleMessagesById(GetMemoryHeroId(hero), heroName, targetAgentIndex, includeCurrentActiveSceneSession);
	}
private List<ConversationMessage> CaptureAndBuildUncompressedMemoryRoleMessagesById(string memoryId, string memoryName, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false)
	{
		Stopwatch sw = Stopwatch.StartNew();
        long generation = SaveRuntimeGuard.CaptureGeneration();
		List<ConversationMessage> result = new List<ConversationMessage>();
		string normalizedMemoryId = NormalizeMemoryHeroId(memoryId);
		if (!IsMemoryEntityEligibleForCompressedMemory(normalizedMemoryId))
		{
			return result;
		}
		List<DailyMemoryDraft> drafts = LoadDailyMemoryDraftsById(normalizedMemoryId);
		if (drafts == null || drafts.Count <= 0)
		{
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] uncompressed_memory_done hero=" + normalizedMemoryId + " drafts=0 messages=0 agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			if (IsNonHeroMemoryId(normalizedMemoryId))
			{
				LogNonHeroMemoryTrace("stage=uncompressed_build_done memoryId=" + normalizedMemoryId + " drafts=0 draftLines=0 messages=0 agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			}
			return result;
		}
		int currentDay = GetCurrentGameDayIndexSafe();
		int currentSceneSessionId = includeCurrentActiveSceneSession ? -1 : GetCurrentSceneSessionIdForDailyMemorySuppression();
		int currentDialogueSessionId = GetCurrentNativeConversationMemorySessionIdForSuppression();
		string currentMemorySessionKey = !includeCurrentActiveSceneSession && (currentSceneSessionId >= 0 || currentDialogueSessionId >= 0)
			? BuildCurrentMemorySessionKey(currentSceneSessionId, currentDialogueSessionId)
			: "";
        var snapshot = new UncompressedMemoryPromptSnapshot {
            CurrentDay = currentDay, CurrentMemorySessionKey = currentMemorySessionKey,
            TargetAgentIndex = targetAgentIndex, MemoryName = string.IsNullOrWhiteSpace(memoryName) ? "NPC" : memoryName.Trim(),
            HistoryLineMinimum = DuelSettings.DailyConversationHistoryLineLimitMin,
            HistoryLineMaximum = DuelSettings.DailyConversationHistoryLineLimitMax
        };
        bool needsCurrentScene = false;
        foreach (DailyMemoryDraft draft in drafts)
        {
            if (draft == null) continue;
            var captured = new UncompressedMemoryDraftSnapshot {
                GameDayIndex = draft.GameDayIndex, HasLlmDialogue = draft.HasLlmDialogue,
                HasCompressedBlock = draft.GameDayIndex != currentDay && HasCompressedMemoryBlock(normalizedMemoryId, draft.GameDayIndex)
            };
            // Already-compressed prior days cannot contribute lines; do not copy their payload.
            captured.Lines = captured.HasCompressedBlock ? null : draft.Lines?.Select(line => line?.CopyForSummary()).ToList();
            snapshot.Drafts.Add(captured);
            if (captured.Lines != null && captured.Lines.Any(line => line != null && line.GameDayIndex == currentDay
                && !string.IsNullOrWhiteSpace(line.Text)
                && UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(line.Scene))) needsCurrentScene = true;
        }
        if (needsCurrentScene)
        {
            try { snapshot.CurrentScene = ResolveCurrentMemorySceneLabel(); } catch { }
        }
        int historyLineLimit = DuelSettings.GetDailyConversationHistoryLineLimitForExternal();
        snapshot.HistoryLineLimit = historyLineLimit;
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return result;
        result = UncompressedMemoryMessageAssemblyOwner.Assemble(snapshot, out int rawMessageCount);
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return new List<ConversationMessage>();
		sw.Stop();
		Logger.Log("Logic", "[MemoryPerf] uncompressed_memory_done hero=" + normalizedMemoryId + " drafts=" + drafts.Count + " rawMessages=" + rawMessageCount + " messages=" + result.Count + " historyLineLimit=" + historyLineLimit + " agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		if (IsNonHeroMemoryId(normalizedMemoryId))
		{
			LogNonHeroMemoryTrace("stage=uncompressed_build_done memoryId=" + normalizedMemoryId + " drafts=" + drafts.Count + " draftLines=" + CountDailyMemoryDraftLines(drafts) + " rawMessages=" + rawMessageCount + " messages=" + result.Count + " historyLineLimit=" + historyLineLimit + " agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " currentDay=" + currentDay + " suppressedSceneSession=" + currentSceneSessionId + " suppressedDialogueSession=" + currentDialogueSessionId + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		}
		return result;
	}
private static string ResolveCapturedMemoryLineSceneForPrompt(DailyMemoryLine line)
    {
        if (!UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(line?.Scene))
            return UncompressedMemoryMessageAssemblyOwner.ResolveMemoryLineSceneForPrompt(line, -1, "");
        try
        {
            int currentDay = GetCurrentGameDayIndexSafe();
            string fallback = line != null && line.GameDayIndex == currentDay ? ResolveCurrentMemorySceneLabel() : "";
            return UncompressedMemoryMessageAssemblyOwner.ResolveMemoryLineSceneForPrompt(line, currentDay, fallback);
        }
        catch { return "未知场景"; }
    }
}
