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
	private void SaveDevDailyMemoryDraftsAfterEdit(Hero npc, List<DailyMemoryDraft> drafts, int affectedDayIndex, string reason, List<DailyMemoryLine> previousLines = null)
	{
        MemoryDeveloperEditOwner.SaveDevDailyMemoryDraftsAfterEdit(_memoryBusinessState, CaptureDailyDeveloperEditContext(npc), CaptureDailyDeveloperEditEffects(npc, affectedDayIndex, reason), drafts, affectedDayIndex, reason, previousLines);
    }

	private void SyncDialogueHistoryForDailyMemoryDraftEdit(Hero npc, int affectedDayIndex, IEnumerable<DailyMemoryLine> previousLines, IEnumerable<DailyMemoryLine> currentLines, string reason)
	{
        MemoryDeveloperEditOwner.SyncDialogueHistoryForDailyMemoryDraftEdit(CaptureDailyDeveloperEditContext(npc), CaptureDailyDeveloperEditEffects(npc, affectedDayIndex, reason), affectedDayIndex, previousLines, currentLines, reason);
    }

	private static List<AnimusForgeDialogueHistoryEntry> BuildNativeConversationHistoryEntriesForDailyMemoryEdit(Hero npc, IEnumerable<DailyMemoryLine> lines) => MemoryHistoryCommitBannerlordAdapter.BuildNativeConversationHistoryEntriesForDailyMemoryEdit(npc, lines);

	private static List<DailyMemoryLine> CloneDevDailyMemoryLines(IEnumerable<DailyMemoryLine> lines)
	{ return MemoryDeveloperEditOwner.CloneDevDailyMemoryLines(lines); }

	private static Dictionary<string, int> BuildDailyMemorySyncLineCounts(IEnumerable<DailyMemoryLine> lines)
	{ return MemoryDeveloperEditOwner.BuildDailyMemorySyncLineCounts(lines); }

	private static Dictionary<string, int> BuildDailyMemorySyncCountDelta(Dictionary<string, int> source, Dictionary<string, int> target)
	{ return MemoryDeveloperEditOwner.BuildDailyMemorySyncCountDelta(source, target); }

	private static int RemoveDialogueHistoryLinesByCounts(DialogueDay day, Dictionary<string, int> removeCounts)
	{ return MemoryDeveloperEditOwner.RemoveDialogueHistoryLinesByCounts(day, removeCounts); }

	private static List<string> BuildDailyMemorySyncAddedDialogueLines(IEnumerable<DailyMemoryLine> currentLines, Dictionary<string, int> addCounts)
	{ return MemoryDeveloperEditOwner.BuildDailyMemorySyncAddedDialogueLines(currentLines, addCounts); }

	private static string BuildDialogueHistoryLineForDailyMemorySync(DailyMemoryLine line)
	{ return MemoryDeveloperEditOwner.BuildDialogueHistoryLineForDailyMemorySync(line); }

	private static string NormalizeDialogueHistoryLineForDailyMemorySync(string line)
	{ return MemoryDeveloperEditOwner.NormalizeDialogueHistoryLineForDailyMemorySync(line); }

	private static string ResolveDailyMemorySyncGameDate(int dayIndex, IEnumerable<DailyMemoryLine> currentLines, IEnumerable<DailyMemoryLine> previousLines)
	{ return MemoryDeveloperEditOwner.ResolveDailyMemorySyncGameDate(dayIndex, currentLines, previousLines, ResolveDailyDeveloperEditFallbackDate); }

	private static void NormalizeDevDailyMemoryDraftForSave(Hero npc, DailyMemoryDraft draft)
	{ MemoryDeveloperEditOwner.NormalizeDevDailyMemoryDraftForSave(GetMemoryHeroId(npc), npc?.Name?.ToString(), draft); }


    internal bool DeleteDevCompressedMemoryBlockData(Hero npc, string blockId, long generation)
    {
        if (npc == null || !IsMemorySourceEditorCurrent(generation)) return false;
        return MemoryDeveloperEditOwner.DeleteBlockForAuthority(() => LoadCompressedMemoryBlocks(npc), () => GetMemoryHeroId(npc),
            blockId, _memoryBusinessState, MarkMemoryOverviewDirty, blocks => TryEnqueueMemoryOverviewForHero(npc, blocks),
            () => LoadCompressedMemoryBlocks(npc), message => Logger.Log("MemoryOverview", message));
    }

    internal bool TryApplyDevCompressedMemoryBlockDataMutation(Hero npc, string blockId,
        Action<CompressedMemoryBlock> mutate, long generation)
    {
        if (npc == null || !IsMemorySourceEditorCurrent(generation)) return false;
        return MemoryDeveloperEditOwner.EditBlockForAuthority(() => LoadCompressedMemoryBlocks(npc), () => GetMemoryHeroId(npc), () => npc.Name?.ToString(),
            blockId, mutate, _memoryBusinessState, MarkMemoryOverviewDirty, blocks => TryEnqueueMemoryOverviewForHero(npc, blocks),
            () => LoadCompressedMemoryBlocks(npc), message => Logger.Log("MemoryOverview", message));
    }

    private void InvalidateDevMemoryOverviewData(Hero npc, string reason)
    {
        MemoryDeveloperEditOwner.InvalidateOverviewForAuthority(() => GetMemoryHeroId(npc), _memoryBusinessState,
            () => LoadCompressedMemoryBlocks(npc), blocks => TryEnqueueMemoryOverviewForHero(npc, blocks), reason,
            message => Logger.Log("MemoryOverview", message));
    }

    internal bool TryApplyDevDialogueHistoryLineDataMutation(Hero npc, int day, int lineIndex, string input, long generation)
    {
        if (npc == null || !IsMemorySourceEditorCurrent(generation)) return false;
        return MemoryDeveloperEditOwner.EditDialogueHistoryLine(LoadDialogueHistory(npc), day, lineIndex, input,
            records => SaveDialogueHistory(npc, records));
    }

    internal bool DeleteDevDailyMemoryDraftData(Hero npc, int day, long generation)
    {
        if (!IsMemorySourceEditorCurrent(generation)) return false;
        MemoryDeveloperEditOwner.DeleteDailyDraft(_memoryBusinessState, CaptureDailyDeveloperEditContext(npc),
            CaptureDailyDeveloperEditEffects(npc, day, "delete_draft"), day);
        return true;
    }

    internal bool ClearDevCompressedMemoryData(Hero npc, long generation)
    {
        if (!IsMemorySourceEditorCurrent(generation)) return false;
        MemoryDeveloperEditOwner.ClearMemoryWithHistorySync(_memoryBusinessState, CaptureDailyDeveloperEditContext(npc),
            CaptureMemoryImportExportState(), day => CaptureDailyDeveloperEditEffects(npc, day, "clear_compressed_memory"));
        return true;
    }

    internal bool ClearDevDialogueHistoryData(Hero npc, long generation)
    {
        if (!IsMemorySourceEditorCurrent(generation)) return false;
        MemoryDeveloperEditOwner.ClearDialogueHistory(_memoryBusinessState, npc.StringId ?? string.Empty,
            () => ShoutBehavior.ClearNativeConversationSessionHistoryForExternal(npc, npc.CharacterObject, npc.Name?.ToString()));
        return true;
    }

    internal bool TryApplyDevDailyMemoryLineDataMutation(Hero npc, int day, int lineIndex,
        Action<DailyMemoryDraft, DailyMemoryLine> mutate, long generation)
    {
        if (!IsMemorySourceEditorCurrent(generation)) return false;
        return MemoryDeveloperEditOwner.MutateDailyLine(_memoryBusinessState, CaptureDailyDeveloperEditContext(npc),
            CaptureDailyDeveloperEditEffects(npc, day, "edit_line"), day, lineIndex, mutate);
    }

    internal bool TryApplyDevDailyMemoryDraftDataMutation(Hero npc, int day, Action<DailyMemoryDraft> mutate,
        string reason, long generation)
    {
        if (!IsMemorySourceEditorCurrent(generation)) return false;
        return MemoryDeveloperEditOwner.MutateDailyDraft(_memoryBusinessState, CaptureDailyDeveloperEditContext(npc),
            CaptureDailyDeveloperEditEffects(npc, day, reason), day, mutate, reason);
    }

    internal bool ApplyImportedDialogueHistory(Dictionary<string, List<DialogueDay>> imported,
        bool overwriteExisting, long importGeneration)
    {
        if (!IsMemorySourceEditorCurrent(importGeneration)) return false;
        MemoryImportExportOwner.ApplyDialogueHistoryImports(_memoryBusinessState, imported, overwriteExisting);
        return true;
    }

    private static MemoryDailyDeveloperEditContext CaptureDailyDeveloperEditContext(Hero npc) => new MemoryDailyDeveloperEditContext { HeroId = GetMemoryHeroId(npc), HeroName = npc?.Name?.ToString(), HasHero = npc != null };
    private MemoryDailyDeveloperEditEffects CaptureDailyDeveloperEditEffects(Hero npc, int day, string reason) => new MemoryDailyDeveloperEditEffects
    {
        LoadHistory = () => LoadDialogueHistory(npc), SaveHistory = records => SaveDialogueHistory(npc, records),
        ResolveGameDate = ResolveDailyDeveloperEditFallbackDate,
        SyncNativeHistory = (oldLines, newLines) => ShoutBehavior.SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(npc, npc.CharacterObject, npc.Name?.ToString(), day, BuildNativeConversationHistoryEntriesForDailyMemoryEdit(npc, oldLines), BuildNativeConversationHistoryEntriesForDailyMemoryEdit(npc, newLines), reason),
        Log = message => Logger.Log("CompressedMemory", message)
    };
    private static string ResolveDailyDeveloperEditFallbackDate(int day)
    {
        try { if ((int)CampaignTime.Now.ToDays == day) return CampaignTime.Now.ToString(); } catch { }
        return "";
    }
}
