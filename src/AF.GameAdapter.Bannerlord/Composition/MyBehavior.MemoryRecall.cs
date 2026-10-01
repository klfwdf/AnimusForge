using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
	private static string BuildMemoryRecallQueryText(Hero hero, string currentInput, string secondaryInput, IEnumerable<DailyMemoryDraft> drafts, string capturedScene = null)
	{
		return MemoryRecallContextOwner.BuildMemoryRecallQueryText(currentInput, secondaryInput, drafts, capturedScene ?? ResolveCurrentMemorySceneLabel());
	}

	private bool TryBuildMemoryRecallCandidates(Hero hero, List<CompressedMemoryBlock> blocks, string currentInput, string secondaryInput, List<DailyMemoryDraft> drafts, int candidateLimit, out List<MemoryRecallCandidate> candidates, out string error, long runtimeGeneration, HistoryPromptSnapshot snapshot = null)
	{

        MemoryRecallRequest request = CaptureMemoryRecallRequest(hero?.StringId, currentInput, secondaryInput, snapshot, blocks, drafts);
        if (snapshot == null && blocks != null && blocks.Count > candidateLimit)
            request.RecallQuery = BuildMemoryRecallQueryText(hero, currentInput, secondaryInput, drafts, request.Scene);
        try { return MemoryRecallContextOwner.TryBuildMemoryRecallCandidates(blocks, currentInput, secondaryInput, drafts, candidateLimit, out candidates, out error, runtimeGeneration, request); }
        finally { PublishMemoryRecallFailure(request); }
	}

	private static void AssignMemoryCandidateDisplayIds(List<MemoryRecallCandidate> candidates)
	{
		MemoryRecallContextOwner.AssignMemoryCandidateDisplayIds(candidates);
	}

	private bool TrySelectMemoryIdsWithPreprocess(List<MemoryRecallCandidate> candidates, int finalCount, string currentInput, string secondaryInput, out List<int> selectedIds, out string error, long runtimeGeneration, HistoryPromptSnapshot snapshot = null)
	{

        MemoryRecallRequest request = CaptureMemoryRecallRequest(null, currentInput, secondaryInput, snapshot, new List<CompressedMemoryBlock>(), new List<DailyMemoryDraft>());
        try { return MemoryRecallContextOwner.TrySelectMemoryIdsWithPreprocess(candidates, finalCount, currentInput, secondaryInput, out selectedIds, out error, runtimeGeneration, request); }
        finally { PublishMemoryRecallFailure(request); }
	}

	private string BuildCompressedMemoryContextById(string memoryId, string currentInput, string secondaryInput, HistoryPromptSnapshot snapshot = null)
	{

        var request = CaptureMemoryRecallRequest(memoryId, currentInput, secondaryInput, snapshot, null, null);
        try { return MemoryRecallContextOwner.BuildCompressedMemoryContextById(memoryId, currentInput, secondaryInput, request); }
        finally { PublishMemoryRecallFailure(request); }
	}

	private static string FormatPastAfefLineForPrompt(string text)
	{
		return MemoryRecallContextOwner.FormatPastAfefLineForPrompt(text);
	}

	private List<ArchiveHit> FindRelevantArchiveHits(List<HistoryLineEntry> older, List<WeightedRecallQueryInput> queryInputs, int returnCap, out bool onnxUsed, out string matchMode, out int rerankPerIntent, out int recallPerIntent)
	{
		return HistoryArchiveRecallOwner.FindRelevantArchiveHits(older, queryInputs, returnCap, out onnxUsed, out matchMode, out rerankPerIntent, out recallPerIntent);
	}

    private MemoryRecallRequest CaptureMemoryRecallRequest(string memoryId, string currentInput, string secondaryInput,
        HistoryPromptSnapshot snapshot, List<CompressedMemoryBlock> blocks, List<DailyMemoryDraft> drafts)
    {
        string id = NormalizeMemoryHeroId(memoryId);
        blocks = blocks ?? snapshot?.Blocks ?? LoadCompressedMemoryBlocksById(id);
        drafts = drafts ?? (snapshot == null ? LoadDailyMemoryDraftsById(id) : null);
        string scene = snapshot?.Scene ?? ResolveCurrentMemorySceneLabel();
        int finalCount = snapshot?.FinalCount ?? GetMemoryFinalInjectCountFromSettings();
        int candidateLimit = snapshot?.CandidateLimit ?? GetMemoryCandidateLimitFromSettings();
        return new MemoryRecallRequest
        {
            Generation = snapshot?.Generation ?? SaveRuntimeGuard.CaptureGeneration(),
            GameDay = snapshot?.GameDay ?? GetCurrentGameDayIndexSafe(), Scene = scene,
            FinalCount = finalCount,
            CandidateLimit = candidateLimit,
            PreprocessMode = snapshot?.PreprocessMode ?? GetMemoryPreprocessModeFromSettings(),
            BlockCount = snapshot?.BlockCount ?? blocks.Count, DraftCount = snapshot?.DraftCount ?? (drafts?.Count ?? 0),
            Blocks = blocks, Drafts = drafts,
            RecallQuery = snapshot?.RecallQuery ?? (blocks.Count > Math.Max(finalCount, candidateLimit) ? BuildMemoryRecallQueryText(null, currentInput, secondaryInput, drafts, scene) : ""),
            FormatFailureDetail = (reason, reply) => LlmRetryPrompt.BuildFailureDetail(reason, reply)
        };
    }

    private void PublishMemoryRecallFailure(MemoryRecallRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.BlockingTitle))
            ShowCompressedMemoryBlockingPopup(request.BlockingTitle, request.BlockingMessage, request.Generation);
    }
}
