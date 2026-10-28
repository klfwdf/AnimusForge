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
		private void RecordEventSourceMaterial(string materialKind, string label, string snapshotText, string stableKey, string kingdomId, string settlementId, bool includeInWorld, bool includeInKingdom, string actorHeroId = "", string actorKingdomId = "", int dayOverride = -1, string gameDateOverride = "") => _campaignCharacterRecordCapture.RecordEventSourceMaterial(materialKind, label, snapshotText, stableKey, kingdomId, settlementId, includeInWorld, includeInKingdom, actorHeroId, actorKingdomId, dayOverride, gameDateOverride);

	private void RecordNpcActionInternal(Dictionary<string, List<NpcActionEntry>> storage, Hero hero, string text, string stableKey, bool keepOnlyRecentWindow, bool dedupeAcrossWindow, int maxEntries, NpcActionFacts facts, bool isMajor, bool allowNonLordHero = false) => _campaignCharacterRecordCapture.RecordNpcActionInternal(storage, hero, text, stableKey, keepOnlyRecentWindow, dedupeAcrossWindow, maxEntries, facts, isMajor, allowNonLordHero);

	private void RecordPlayerNotorietyActionFromNpcAction(string text, string stableKey, NpcActionFacts facts, bool isMajor) => _campaignCharacterRecordCapture.RecordPlayerNotorietyActionFromNpcAction(text, stableKey, facts, isMajor);

	internal sealed class EventSourceMaterialEntry
	{
		public int Day;

		public int Sequence;

		public string GameDate;

		public string MaterialKind;

		public string Label;

		public string SnapshotText;

		public string StableKey;

		public string KingdomId;

		public string SettlementId;

		public string ActorHeroId;

		public string ActorKingdomId;

		public bool IncludeInWorld;

		public bool IncludeInKingdom;
	}

    private readonly CampaignMaterialRecordOwner _campaignMaterialRecords = new CampaignMaterialRecordOwner();
    private List<EventSourceMaterialEntry> _eventSourceMaterials {
        get => _campaignMaterialRecords.Materials; set => _campaignMaterialRecords.Materials = value;
    }
    private Dictionary<string, EventSourceMaterialEntry> _eventSourceMaterialIndex {
        get => _campaignMaterialRecords.Index; set => _campaignMaterialRecords.Index = value;
    }
    private EventSourceMaterialIndex<EventSourceMaterialEntry> _eventSourceMaterialIndexBinding => _campaignMaterialRecords.Binding;
    private void RebuildEventSourceMaterialIndex() => _campaignMaterialRecords.RebuildEventSourceMaterialIndex();
    private static List<EventSourceMaterialEntry> SanitizeEventSourceMaterials(List<EventSourceMaterialEntry> source)
        => CampaignMaterialRecordOwner.SanitizeEventSourceMaterials(source);
    private static string BuildEventSourceMaterialIndexKey(int day, string stableKey)
        => CampaignMaterialRecordOwner.BuildEventSourceMaterialIndexKey(day, stableKey);
	private static NpcActionEntry CreateNpcActionEntry(Hero hero, string text, string stableKey, int day, int order, int sequence, NpcActionFacts facts, bool isMajor) => CampaignCharacterRecordCaptureAdapter.CreateNpcActionEntry(hero, text, stableKey, day, order, sequence, facts, isMajor);

    private readonly NpcActionRecordOwner _npcActionRecords = new NpcActionRecordOwner();
    private Dictionary<string, HashSet<string>> _npcRecentActionStableKeyIndex => _npcActionRecords.RecentStableKeys;
    private void RefreshNpcRecentActionStableKeyIndexForHero(string heroKey, List<NpcActionEntry> entries)
        => _npcActionRecords.RefreshNpcRecentActionStableKeyIndexForHero(heroKey, entries);
    private bool IsNpcRecentActionStableKeyKnown(string heroKey, string stableKey)
        => _npcActionRecords.IsNpcRecentActionStableKeyKnown(heroKey, stableKey);

	internal sealed class EventImportPayload
	{
		public bool HasWorldSummaryFile;

		public string WorldSummary;

		public bool HasKingdomSummariesFile;

		public Dictionary<string, string> KingdomSummaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public bool HasEventRecordsFile;

		public List<EventRecordEntry> EventRecords = new List<EventRecordEntry>();
	}

    private void ApplyImportedEventData(EventImportPayload payload, bool overwriteExisting)
        => WeeklyEventFiles.ApplyImportedEventData(payload, overwriteExisting);


    private static List<EventRecordEntry> SanitizeEventRecordEntries(List<EventRecordEntry> source)
        => WeeklyEventDataImportOwner.SanitizeEventRecordEntries(source, NeutralizeWeeklyReportScenarioName,
            BuildFallbackWeeklyReportShortSummary, NormalizeWeeklyReportTagText);


    private bool TryLoadEventDataFromImportDir(string importDir, out EventImportPayload payload, out string error)
        => WeeklyEventFiles.TryLoadEventDataFromImportDir(importDir, out payload, out error);


	// Database import "clear": drops the world/kingdom opening summaries and the canonical week-zero records derived from them.
	// Dynamic history from later weeks is left byte-for-byte untouched.
	private int ClearDatabaseOpeningKnowledge()
	{
		bool removedWorldOpeningRecord = (_weeklyEventRecords.Records ?? new List<EventRecordEntry>()).Any((EventRecordEntry x) => WeeklyEventDataImportOwner.IsCanonicalOpeningEvent(x) && string.Equals((x.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase));
		WeeklyEventDataImportOwner.ClearOpeningSummaries(ref _weeklyEventRecords.WorldOpening, ref _weeklyEventRecords.KingdomOpenings, _weeklyReportMaterialRevisions.MarkOpening);
		int removedOpeningRecordCount = _weeklyEventRecords.Records?.RemoveAll(WeeklyEventDataImportOwner.IsCanonicalOpeningEvent) ?? 0;
		_weekZeroShortSummaries.RemovePendingOpeningRequests(WeeklyEventDataImportOwner.IsCanonicalOpeningEventId);
		if (removedWorldOpeningRecord)
		{
			Interlocked.Increment(ref _weeklyEventRecords.PublishedHistoryRevision);
		}
		return removedOpeningRecordCount;
	}


    private static void NormalizeEventRecordEntriesInPlace(List<EventRecordEntry> source)
        => WeeklyEventDataImportOwner.NormalizeEventRecordEntriesInPlace(source, NeutralizeWeeklyReportScenarioName,
            BuildFallbackWeeklyReportShortSummary, NormalizeWeeklyReportTagText);
    private static List<EventMaterialReference> NormalizeEventMaterialReferencesInPlace(List<EventMaterialReference> materials, bool neutralizeWeeklyText)
        => WeeklyEventDataImportOwner.NormalizeEventMaterialReferencesInPlace(materials, neutralizeWeeklyText, NeutralizeWeeklyReportScenarioName);
    private static List<string> NormalizeEventMaterialIdListInPlace(List<string> values)
        => WeeklyEventDataImportOwner.NormalizeEventMaterialIdListInPlace(values);


    private bool SetDeveloperWorldOpeningSummary(string input, long generation)
    {
        if (!IsMemorySourceEditorCurrent(generation)) return false;
        WeeklyEventDataImportOwner.SetWorldOpeningSummary(input, ref _weeklyEventRecords.WorldOpening, _weeklyReportMaterialRevisions.MarkOpening);
        return true;
    }
    private bool ClearDeveloperOpeningSummaries(long generation)
    {
        if (!IsMemorySourceEditorCurrent(generation)) return false;
        WeeklyEventDataImportOwner.ClearOpeningSummaries(ref _weeklyEventRecords.WorldOpening, ref _weeklyEventRecords.KingdomOpenings, _weeklyReportMaterialRevisions.MarkOpening);
        return true;
    }
    private EventRecordEntry ApplyDeveloperEventTitle(EventRecordEntry entry, string input, long generation)
        => ApplyDeveloperEventEdit(entry, input, true, generation);
    private EventRecordEntry ApplyDeveloperEventReport(EventRecordEntry entry, string input, long generation)
        => ApplyDeveloperEventEdit(entry, input, false, generation);
    private EventRecordEntry ApplyDeveloperEventEdit(EventRecordEntry entry, string input, bool editTitle, long generation)
    {
        if (entry == null || !IsMemorySourceEditorCurrent(generation)) return null;
        return WeeklyEventDataImportOwner.ApplyDeveloperEventEdit(entry, input, editTitle, ref _weeklyEventRecords.Records,
            FindWeeklyReportRecordById, BuildPublishedWorldWeeklyProductState,
            stored => BuildDefaultWeeklyReportTitle(new WeeklyEventMaterialPreviewGroup { GroupKind = stored.EventKind,
                KingdomId = stored.ScopeKingdomId }, stored.WeekIndex), SanitizeEventRecordEntries,
            NotifyPublishedWorldWeeklyProductChanged, NotifyWorldMessageWeeklyTimelineChanged);
    }


    private void ResetNpcActionRecordContainers()
        => NpcActionRecordOwner.ResetContainers(ref _memoryBusinessState.MajorActions, ref _memoryBusinessState.MajorActionStorage,
            ref _memoryBusinessState.RecentActions, ref _memoryBusinessState.RecentActionStorage);
    private void EnsureNpcActionRecordContainers()
        => NpcActionRecordOwner.EnsureContainers(ref _memoryBusinessState.MajorActions, ref _memoryBusinessState.MajorActionStorage,
            ref _memoryBusinessState.RecentActions, ref _memoryBusinessState.RecentActionStorage);
}
