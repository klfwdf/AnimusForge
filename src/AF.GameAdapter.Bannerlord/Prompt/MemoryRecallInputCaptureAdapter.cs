using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using HistoryPromptSnapshot = AnimusForge.MyBehavior.HistoryPromptSnapshot;
using TaleWorlds.MountAndBlade;
namespace AnimusForge.Refactor.Adapters;
internal static class MemoryRecallInputCaptureAdapter
{
internal static string ResolveCapturedMemoryLineSceneForPrompt(DailyMemoryLine line)
    {
        if (!UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(line?.Scene))
            return UncompressedMemoryMessageAssemblyOwner.ResolveMemoryLineSceneForPrompt(line, -1, "");
        try
        {
            int currentDay = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
            string fallback = line != null && line.GameDayIndex == currentDay ? SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel() : "";
            return UncompressedMemoryMessageAssemblyOwner.ResolveMemoryLineSceneForPrompt(line, currentDay, fallback);
        }
        catch { return "未知场景"; }
    }
	internal static bool IsActiveSceneSessionHistoryLine(string line) => MemoryHistoryCommitBannerlordAdapter.IsActiveSceneSessionHistoryLine(line);
	internal static bool IsLoreInjectionHistoryLine(string line) => MemoryBusinessStateOwner.IsLoreInjectionHistoryLine(line);
	internal static bool IsPlayerTurnStartLine(string line) => MemoryBusinessStateOwner.IsPlayerTurnStartLine(line);
	internal static bool TryStripPlayerSpeechPrefix(string line, out string stripped)
	{
		stripped = "";
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		DialogueHistoryLedger.TryStripSceneSessionMarker(text, out text, out var _);
		if (text.StartsWith("玩家:", StringComparison.Ordinal))
		{
			stripped = text.Substring("玩家:".Length).Trim();
			return true;
		}
		if (text.StartsWith("玩家：", StringComparison.Ordinal))
		{
			stripped = text.Substring("玩家：".Length).Trim();
			return true;
		}
		int num = text.IndexOf("（player）对", StringComparison.Ordinal);
		if (num >= 0)
		{
			int num2 = text.IndexOf("说:", num, StringComparison.Ordinal);
			if (num2 < 0)
			{
				num2 = text.IndexOf("说：", num, StringComparison.Ordinal);
			}
			if (num2 >= 0)
			{
				stripped = text.Substring(num2 + 2).Trim();
				return true;
			}
		}
		return false;
	}

    internal sealed class CapturePorts
    {
        internal Func<string,List<CompressedMemoryBlock>> LoadBlocks;
        internal Func<string,List<DailyMemoryDraft>> LoadDrafts;
        internal Func<int> FinalCount, CandidateLimit, PreprocessMode;
        internal Action<string,string,long> ShowBlockingFailure;
    }

internal static string BuildMemoryRecallQueryText(Hero hero, string currentInput, string secondaryInput, IEnumerable<DailyMemoryDraft> drafts, string capturedScene = null)
    {
		return MemoryRecallContextOwner.BuildMemoryRecallQueryText(currentInput, secondaryInput, drafts, capturedScene ?? SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel());
	}
internal static bool TryBuildMemoryRecallCandidates(CapturePorts ports, Hero hero, List<CompressedMemoryBlock> blocks, string currentInput, string secondaryInput, List<DailyMemoryDraft> drafts, int candidateLimit, out List<MemoryRecallCandidate> candidates, out string error, long runtimeGeneration, HistoryPromptSnapshot snapshot = null)
    {

        MemoryRecallRequest request = CaptureMemoryRecallRequest(ports, hero?.StringId, currentInput, secondaryInput, snapshot, blocks, drafts);
        if (snapshot == null && blocks != null && blocks.Count > candidateLimit)
            request.RecallQuery = BuildMemoryRecallQueryText(hero, currentInput, secondaryInput, drafts, request.Scene);
        try { return MemoryRecallContextOwner.TryBuildMemoryRecallCandidates(blocks, currentInput, secondaryInput, drafts, candidateLimit, out candidates, out error, runtimeGeneration, request); }
        finally { PublishMemoryRecallFailure(ports, request); }
	}
internal static bool TrySelectMemoryIdsWithPreprocess(CapturePorts ports, List<MemoryRecallCandidate> candidates, int finalCount, string currentInput, string secondaryInput, out List<int> selectedIds, out string error, long runtimeGeneration, HistoryPromptSnapshot snapshot = null)
    {

        MemoryRecallRequest request = CaptureMemoryRecallRequest(ports, null, currentInput, secondaryInput, snapshot, new List<CompressedMemoryBlock>(), new List<DailyMemoryDraft>());
        try { return MemoryRecallContextOwner.TrySelectMemoryIdsWithPreprocess(candidates, finalCount, currentInput, secondaryInput, out selectedIds, out error, runtimeGeneration, request); }
        finally { PublishMemoryRecallFailure(ports, request); }
	}
internal static string BuildCompressedMemoryContextById(CapturePorts ports, string memoryId, string currentInput, string secondaryInput, HistoryPromptSnapshot snapshot = null)
    {

        var request = CaptureMemoryRecallRequest(ports, memoryId, currentInput, secondaryInput, snapshot, null, null);
        try { return MemoryRecallContextOwner.BuildCompressedMemoryContextById(memoryId, currentInput, secondaryInput, request); }
        finally { PublishMemoryRecallFailure(ports, request); }
	}
internal static MemoryRecallRequest CaptureMemoryRecallRequest(CapturePorts ports, string memoryId, string currentInput, string secondaryInput,
        HistoryPromptSnapshot snapshot, List<CompressedMemoryBlock> blocks, List<DailyMemoryDraft> drafts)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        blocks = blocks ?? snapshot?.Blocks ?? ports.LoadBlocks(id);
        drafts = drafts ?? (snapshot == null ? ports.LoadDrafts(id) : null);
        string scene = snapshot?.Scene ?? SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel();
        int finalCount = snapshot?.FinalCount ?? ports.FinalCount();
        int candidateLimit = snapshot?.CandidateLimit ?? ports.CandidateLimit();
        return new MemoryRecallRequest
        {
            Generation = snapshot?.Generation ?? SaveRuntimeGuard.CaptureGeneration(),
            GameDay = snapshot?.GameDay ?? MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe(), Scene = scene,
            FinalCount = finalCount,
            CandidateLimit = candidateLimit,
            PreprocessMode = snapshot?.PreprocessMode ?? ports.PreprocessMode(),
            BlockCount = snapshot?.BlockCount ?? blocks.Count, DraftCount = snapshot?.DraftCount ?? (drafts?.Count ?? 0),
            Blocks = blocks, Drafts = drafts,
            RecallQuery = snapshot?.RecallQuery ?? (blocks.Count > Math.Max(finalCount, candidateLimit) ? BuildMemoryRecallQueryText(null, currentInput, secondaryInput, drafts, scene) : ""),
            FormatFailureDetail = (reason, reply) => LlmRetryPrompt.BuildFailureDetail(reason, reply)
        };
    }
internal static void PublishMemoryRecallFailure(CapturePorts ports, MemoryRecallRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.BlockingTitle))
            ports.ShowBlockingFailure(request.BlockingTitle, request.BlockingMessage, request.Generation);
    }
}
