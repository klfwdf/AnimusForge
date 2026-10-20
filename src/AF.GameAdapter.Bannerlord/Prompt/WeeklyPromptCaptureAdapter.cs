using WeeklyReportPromptProfile = AnimusForge.MyBehavior.WeeklyReportPromptProfile;

using WeeklyReportOutputMode = AnimusForge.MyBehavior.WeeklyReportOutputMode;

using WeeklyReportBatchRequest = AnimusForge.MyBehavior.WeeklyReportBatchRequest;

using System;

using System.Diagnostics;

using System.Threading;

using TaleWorlds.Library;

using TaleWorlds.CampaignSystem;

using System.Collections.Generic;

using System.Linq;

using WeeklyPromptSnapshot = AnimusForge.MyBehavior.WeeklyPromptSnapshot;

using WeeklyPromptReportSnapshot = AnimusForge.MyBehavior.WeeklyPromptReportSnapshot;

using EventRecordEntry = AnimusForge.MyBehavior.EventRecordEntry;

using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;

using WeeklyEventMaterialPreviewGroup = AnimusForge.MyBehavior.WeeklyEventMaterialPreviewGroup;

using System.Text;

namespace AnimusForge.Refactor.Adapters;

internal static class WeeklyPromptCaptureAdapter

{

    internal static string FormatNewsCalendarDate(int day)

    {

        if (day < 0) return AdapterPromptCaptureTexts.PcapText("weekly_date_unknown");

        try { return WeeklyReportArchivePolicy.CalendarDate(CampaignTime.Days(day).ToString()); }

        catch { return AdapterPromptCaptureTexts.PcapText("weekly_date_unknown"); }

    }

	internal static string BuildWeekZeroShortSummarySystemPrompt(string eventKind, string kingdomId, string title)

	{

		string text = string.Equals((eventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? AdapterPromptCaptureTexts.PcapText("weekly_world_zero_short") : (MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay(kingdomId) + AdapterPromptCaptureTexts.PcapText("weekly_zero_short"));

		return PromptComposerTexts.ApplyOutputLanguage(AdapterPromptCaptureTexts.PcapText("weekly_zero_system") + text + AdapterPromptCaptureTexts.PcapText("weekly_title_reference") + PromptComposerTexts.ProjectOpeningTextForPrompt((title ?? "").Trim()));

	}

	internal static string BuildWeekZeroShortSummaryUserPrompt(string summary)

	{

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_zero_user"));

		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_output_format"));

		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_short_format"));

		stringBuilder.AppendLine();

		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_opening_label"));

		stringBuilder.AppendLine(WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(summary));

		return stringBuilder.ToString().TrimEnd();

	}



internal static bool IsWeeklyPromptSiegeAggregateMaterial(EventMaterialReference material)

	{

		if (material == null)

		{

			return false;

		}

		string text = (material.MaterialType ?? "").Trim();

		string text2 = (material.ActionKind ?? "").Trim();

		return string.Equals(text, "prompt_agg_siege", StringComparison.OrdinalIgnoreCase)

			|| string.Equals(text2, "prompt_aggregate:siege", StringComparison.OrdinalIgnoreCase)

			|| string.Equals((material.Label ?? "").Trim(), AdapterPromptCaptureTexts.PcapCompat("PromptCompat_7b4561d6327b"), StringComparison.OrdinalIgnoreCase)

			|| string.Equals((material.Label ?? "").Trim(), "siege and defense", StringComparison.OrdinalIgnoreCase);

	}

internal static bool IsWeeklyPromptArmyAggregateMaterial(EventMaterialReference material)

	{

		if (material == null)

		{

			return false;

		}

		string text = (material.MaterialType ?? "").Trim();

		string text2 = (material.ActionKind ?? "").Trim();

		return string.Equals(text, "prompt_agg_army", StringComparison.OrdinalIgnoreCase)

			|| string.Equals(text2, "prompt_aggregate:army", StringComparison.OrdinalIgnoreCase)

			|| string.Equals((material.Label ?? "").Trim(), AdapterPromptCaptureTexts.PcapCompat("PromptCompat_0fd45e22982a"), StringComparison.OrdinalIgnoreCase)

			|| string.Equals((material.Label ?? "").Trim(), "army actions", StringComparison.OrdinalIgnoreCase);

	}

internal static bool IsWeeklyPromptClanChangeAggregateMaterial(EventMaterialReference material)

	{

		if (material == null)

		{

			return false;

		}

		string text = (material.MaterialType ?? "").Trim();

		string text2 = (material.ActionKind ?? "").Trim();

		return string.Equals(text, "prompt_agg_strategic_shift", StringComparison.OrdinalIgnoreCase)

			|| string.Equals(text2, "prompt_aggregate:strategic_shift", StringComparison.OrdinalIgnoreCase)

			|| string.Equals((material.Label ?? "").Trim(), AdapterPromptCaptureTexts.PcapCompat("PromptCompat_f2bf6b0d88bc"), StringComparison.OrdinalIgnoreCase)

			|| string.Equals((material.Label ?? "").Trim(), "clan and kingdom allegiance", StringComparison.OrdinalIgnoreCase);

	}

internal static string BuildWeeklyPromptAggregateShortSnippet(string text, int maxLen)

	{

		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

		while (text2.Contains("  "))

		{

			text2 = text2.Replace("  ", " ");

		}

		if (string.IsNullOrWhiteSpace(text2))

		{

			return "";

		}

		if (text2.Length <= maxLen)

		{

			return text2;

		}

		return text2.Substring(0, Math.Max(0, maxLen)).TrimEnd();

	}

internal static EventMaterialReference BuildWeeklyPromptAggregateShortCategoryMaterial(WeeklyEventMaterialPreviewGroup group, string category, List<EventMaterialReference> materials)

	{

		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();

		if (list.Count == 0)

		{

			return null;

		}

		Dictionary<string, List<EventMaterialReference>> dictionary = new Dictionary<string, List<EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);

		foreach (EventMaterialReference item in list)

		{

			string text = WeeklyMaterialAggregationOwner.BuildWeeklyPromptAggregateEventKey(item);

			if (!dictionary.TryGetValue(text, out var value))

			{

				value = new List<EventMaterialReference>();

				dictionary[text] = value;

			}

			value.Add(item);

		}

		List<List<EventMaterialReference>> list2 = dictionary.Values.Where((List<EventMaterialReference> x) => x != null && x.Count > 0).OrderBy((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionDay ?? int.MaxValue)).ThenBy((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionSequence ?? int.MaxValue)).ToList();

		List<string> list3 = new List<string>();

		foreach (List<EventMaterialReference> item2 in list2.Take(3))

		{

			EventMaterialReference eventMaterialReference2 = item2.FirstOrDefault((EventMaterialReference y) => y != null);

			string text3 = BuildWeeklyPromptAggregateShortSnippet((eventMaterialReference2?.SnapshotText ?? eventMaterialReference2?.Label ?? category ?? ""), 72);

			if (!string.IsNullOrWhiteSpace(text3))

			{

				list3.Add(text3);

			}

		}

		string text2 = WeeklyMaterialAggregationOwner.GetWeeklyPromptAggregateCategoryLabel(category);

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.Append("[").Append(text2).Append("]");

		stringBuilder.Append(AdapterPromptCaptureTexts.PcapText("weekly_aggregate_count")).Append(list.Count);

		stringBuilder.Append(AdapterPromptCaptureTexts.PcapText("weekly_aggregate_events")).Append(list2.Count);

		if (list3.Count > 0)

		{

			stringBuilder.Append(AdapterPromptCaptureTexts.PcapText("weekly_aggregate_examples")).Append(string.Join("；", list3));

		}

		EventMaterialReference eventMaterialReference = new EventMaterialReference

		{

			MaterialType = "prompt_short_" + (category ?? "").Trim().ToLowerInvariant(),

			Label = text2,

			SnapshotText = stringBuilder.ToString().Trim(),

			KingdomId = (group?.KingdomId ?? "").Trim(),

			ActionKind = "prompt_aggregate_short:" + (category ?? "").Trim().ToLowerInvariant(),

			SourceMaterialCount = list.Count,

			ActionDay = list.Min((EventMaterialReference x) => x?.ActionDay ?? int.MaxValue),

			ActionSequence = list.Min((EventMaterialReference x) => x?.ActionSequence ?? int.MaxValue)

		};

		foreach (EventMaterialReference item3 in list)

		{

			WeeklyMaterialAggregationOwner.AppendMaterialReferenceIds(item3, eventMaterialReference);

		}

		return eventMaterialReference;

	}

internal static EventMaterialReference BuildWeeklyPromptAggregateShortRawMaterial(WeeklyEventMaterialPreviewGroup group, string rawKey, List<EventMaterialReference> materials)

	{

		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();

		if (list.Count == 0)

		{

			return null;

		}

		List<string> list2 = new List<string>();

		foreach (EventMaterialReference item in list.Take(3))

		{

			string text = BuildWeeklyPromptAggregateShortSnippet(!string.IsNullOrWhiteSpace(item.SnapshotText) ? item.SnapshotText : item.Label, 72);

			if (!string.IsNullOrWhiteSpace(text))

			{

				list2.Add(text);

			}

		}

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.Append("[").Append(BuildWeeklyPromptAggregateShortSnippet(rawKey, 32)).Append("]");

		stringBuilder.Append(AdapterPromptCaptureTexts.PcapText("weekly_aggregate_count")).Append(list.Count);

		if (list2.Count > 0)

		{

			stringBuilder.Append(AdapterPromptCaptureTexts.PcapText("weekly_aggregate_examples")).Append(string.Join("；", list2));

		}

		EventMaterialReference eventMaterialReference = new EventMaterialReference

		{

			MaterialType = "prompt_short_raw",

			Label = BuildWeeklyPromptAggregateShortSnippet(rawKey, 48),

			SnapshotText = stringBuilder.ToString().Trim(),

			KingdomId = (group?.KingdomId ?? "").Trim(),

			ActionKind = "prompt_aggregate_short:raw",

			SourceMaterialCount = list.Count,

			ActionDay = list.Min((EventMaterialReference x) => x?.ActionDay ?? int.MaxValue),

			ActionSequence = list.Min((EventMaterialReference x) => x?.ActionSequence ?? int.MaxValue)

		};

		foreach (EventMaterialReference item2 in list)

		{

			WeeklyMaterialAggregationOwner.AppendMaterialReferenceIds(item2, eventMaterialReference);

		}

		return eventMaterialReference;

	}

internal static Dictionary<string, WeeklyPromptReportSnapshot> CaptureLatestWeeklyPromptReportSnapshots(List<EventRecordEntry> records, ISet<string> kingdomIds, out WeeklyPromptReportSnapshot worldReport)

	{

		HashSet<string> hashSet = new HashSet<string>((kingdomIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()), StringComparer.OrdinalIgnoreCase);

		Dictionary<string, WeeklyPromptReportSnapshot> dictionary = new Dictionary<string, WeeklyPromptReportSnapshot>(StringComparer.OrdinalIgnoreCase);

		worldReport = null;

		List<EventRecordEntry> list = records;

		if (list == null || list.Count == 0)

		{

			return dictionary;

		}

		for (int i = 0; i < list.Count; i++)

		{

			EventRecordEntry eventRecordEntry = list[i];

			if (eventRecordEntry == null)

			{

				continue;

			}

			string text = (eventRecordEntry.EventKind ?? "").Trim();

			string text2 = (eventRecordEntry.ScopeKingdomId ?? "").Trim();

			bool flag = string.Equals(text, "world", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(text2);

			bool flag2 = string.Equals(text, "kingdom", StringComparison.OrdinalIgnoreCase) && hashSet.Contains(text2);

			if (!flag && !flag2)

			{

				continue;

			}

			WeeklyPromptReportSnapshot weeklyPromptReportSnapshot = CreateWeeklyPromptReportSnapshot(eventRecordEntry);

			if (weeklyPromptReportSnapshot == null)

			{

				continue;

			}

			if (flag)

			{

				if (IsNewerWeeklyPromptReportSnapshot(weeklyPromptReportSnapshot, worldReport))

				{

					worldReport = weeklyPromptReportSnapshot;

				}

			}

			else if (!dictionary.TryGetValue(text2, out WeeklyPromptReportSnapshot value) || IsNewerWeeklyPromptReportSnapshot(weeklyPromptReportSnapshot, value))

			{

				dictionary[text2] = weeklyPromptReportSnapshot;

			}

		}

		return dictionary;

	}

internal static WeeklyPromptReportSnapshot CreateWeeklyPromptReportSnapshot(EventRecordEntry entry)

	{

		string text = (entry?.EventId ?? "").Trim();

		string text2 = (entry?.Title ?? "").Trim();

		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))

		{

			return null;

		}

		bool flag = text.StartsWith("weekly_report:", StringComparison.OrdinalIgnoreCase);

		string text3 = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(entry.ShortSummary);

		string text4 = flag ? WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(entry.Summary) : (entry.Summary ?? "").Trim();

		if (string.IsNullOrWhiteSpace(text3))

		{

			text3 = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(text4);

		}

		return new WeeklyPromptReportSnapshot(entry.WeekIndex, entry.CreatedDay, flag ? WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(text2) : text2, text3, text4);

	}

internal static bool IsNewerWeeklyPromptReportSnapshot(WeeklyPromptReportSnapshot candidate, WeeklyPromptReportSnapshot current)

	{

		if (candidate == null)

		{

			return false;

		}

		if (current == null || candidate.WeekIndex != current.WeekIndex)

		{

			return current == null || candidate.WeekIndex > current.WeekIndex;

		}

		if (candidate.CreatedDay != current.CreatedDay)

		{

			return candidate.CreatedDay > current.CreatedDay;

		}

		return StringComparer.OrdinalIgnoreCase.Compare(candidate.Title, current.Title) < 0;

	}

internal static string BuildWeeklyShortReportsPromptBlockFromSnapshot(IEnumerable<string> kingdomIds, IReadOnlyDictionary<string, WeeklyPromptReportSnapshot> reports, IReadOnlyDictionary<string, string> kingdomDisplays)

	{

		StringBuilder stringBuilder = new StringBuilder();

		int num = 0;

		foreach (string kingdomId in kingdomIds ?? Enumerable.Empty<string>())

		{

			string text = (kingdomId ?? "").Trim();

			if (string.IsNullOrWhiteSpace(text) || reports == null || !reports.TryGetValue(text, out WeeklyPromptReportSnapshot value))

			{

				continue;

			}

			string text2 = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(value.ShortSummary ?? value.Summary);

			if (string.IsNullOrWhiteSpace(text2))

			{

				continue;

			}

			if (num == 0)

			{

				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_three_header"));

				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_three_intro"));

			}

			num++;

			string text3 = (kingdomDisplays != null && kingdomDisplays.TryGetValue(text, out string value2)) ? value2 : text;

			stringBuilder.Append("- ").Append(string.IsNullOrWhiteSpace(text3) ? text : text3);

			if (value.WeekIndex >= 0)

			{

				stringBuilder.Append("（").Append(WeeklyEventRecordStateOwner.BuildWeeklyEpicWeekLabel(value.WeekIndex, FormatNewsCalendarDate)).Append("）");

			}

			stringBuilder.Append("：").AppendLine(text2);

		}

		return num > 0 ? stringBuilder.ToString().TrimEnd() : "";

	}

internal static string BuildSingleWeeklyFullReportPromptBlockFromSnapshot(string header, WeeklyPromptReportSnapshot entry)

	{

		if (entry == null)

		{

			return "";

		}

		string text = (entry.Summary ?? "").Trim();

		if (string.IsNullOrWhiteSpace(text))

		{

			text = (entry.ShortSummary ?? "").Trim();

		}

		if (string.IsNullOrWhiteSpace(text))

		{

			return "";

		}

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine("【" + ((header ?? "").Trim()) + "】");

		if (!string.IsNullOrWhiteSpace(entry.Title))

		{

			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_title_label") + entry.Title.Trim());

		}

		if (entry.WeekIndex >= 0)

		{

			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_date_label") + WeeklyEventRecordStateOwner.BuildWeeklyEpicWeekLabel(entry.WeekIndex, FormatNewsCalendarDate));

		}

		stringBuilder.AppendLine(text);

		return stringBuilder.ToString().TrimEnd();

	}

internal static string BuildTriggeredWeeklyFullReportsPromptBlockFromSnapshot(string triggeredRuleInstructions, WeeklyPromptSnapshot weeklyPromptSnapshot)

	{

		bool flag = PromptRuleBlockText.Has(triggeredRuleInstructions, "npc_major_actions");

		bool flag2 = PromptRuleBlockText.Has(triggeredRuleInstructions, "surroundings");

		if (!flag && !flag2)

		{

			return "";

		}

		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		StringBuilder stringBuilder = new StringBuilder();

		if (flag)

		{

			string text = weeklyPromptSnapshot.NpcFullReport;

			if (!string.IsNullOrWhiteSpace(text) && hashSet.Add("kingdom:" + (weeklyPromptSnapshot.NpcKingdomId ?? "").Trim()))

			{

				stringBuilder.AppendLine(text);

			}

			string text2 = weeklyPromptSnapshot.WorldFullReport;

			if (!string.IsNullOrWhiteSpace(text2) && hashSet.Add("world"))

			{

				if (stringBuilder.Length > 0)

				{

					stringBuilder.AppendLine();

				}

				stringBuilder.AppendLine(text2);

			}

		}

		if (flag2)

		{

			string text3 = weeklyPromptSnapshot.SurroundingsFullReport;

			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add("kingdom:" + (weeklyPromptSnapshot.SurroundingsKingdomId ?? "").Trim()))

			{

				if (stringBuilder.Length > 0)

				{

					stringBuilder.AppendLine();

				}

				stringBuilder.AppendLine(text3);

			}

		}

		return stringBuilder.ToString().TrimEnd();

	}

internal static string BuildSingleWeeklyFullReportPromptBlock(string header, EventRecordEntry entry)

	{

		if (entry == null)

		{

			return "";

		}

		string text = (entry.Summary ?? "").Trim();

		if (string.IsNullOrWhiteSpace(text))

		{

			text = (entry.ShortSummary ?? "").Trim();

		}

		if (string.IsNullOrWhiteSpace(text))

		{

			return "";

		}

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine("【" + ((header ?? "").Trim()) + "】");

		if (!string.IsNullOrWhiteSpace(entry.Title))

		{

			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_title_label") + entry.Title.Trim());

		}

		if (entry.WeekIndex >= 0)

		{

			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_date_label") + WeeklyEventRecordStateOwner.BuildWeeklyEpicWeekLabel(entry.WeekIndex, FormatNewsCalendarDate));

		}

		stringBuilder.AppendLine(text);

		return stringBuilder.ToString().TrimEnd();

	}



    internal sealed class CapturePorts

    {

        internal Func<bool> BulletinEnabled;

        internal Func<List<WorldBulletinEvent>> BulletinEvents;

        internal Func<EventRecordEntry> LatestBulletin;

        internal Func<List<EventRecordEntry>> Records;

        internal Action EnsureOpening;

        internal Func<Hero,CharacterObject,string,string> NpcKingdom, SurroundingsKingdom;

        internal Func<List<Kingdom>> EditableKingdoms;

        internal Func<Kingdom,bool> Eligible;

        internal Func<List<string>,List<string>> Proximity;

        internal Func<string,bool,bool,IEnumerable<string>,IEnumerable<string>,List<string>> SelectSnapshot;

        internal Func<string,bool,List<string>> SelectLive;

        internal Func<string,string,EventRecordEntry> Latest;

    }



internal static WeeklyPromptSnapshot CaptureWeeklyPromptSnapshot(CapturePorts ports, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride)

	{

		using FreezeWatchdog.ScopeToken scopeToken = FreezeWatchdog.Scope("WeeklyPrompt.Capture.mainthread");

		Stopwatch stopwatch = Stopwatch.StartNew();

		// Bulletin mode selects factual NPC knowledge independently of publication.

		// Disabling automatic publication must not revive legacy weekly reports.

		if (ports.BulletinEnabled())

		{

			WeeklyPromptSnapshot bulletinSnapshot = WorldBulletinNpcPromptCaptureAdapter.CaptureWorldBulletinNpcSnapshot(ports, targetHero, targetCharacter, kingdomIdOverride);

			stopwatch.Stop();

			FreezeWatchdog.Mark("WeeklyPrompt.Capture.done", "mode=bulletin ms=" + Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2) + " facts=" + (ports.BulletinEvents()?.Count ?? 0), immediate: true);

			return bulletinSnapshot;

		}

		ports.EnsureOpening();

		string text = ports.NpcKingdom(targetHero, targetCharacter, kingdomIdOverride);

		string text2 = ports.SurroundingsKingdom(targetHero, targetCharacter, kingdomIdOverride);

		List<Kingdom> list = ports.EditableKingdoms();

		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		List<string> list2 = new List<string>();

		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (Kingdom item in list)

		{

			string text3 = (item?.StringId ?? "").Trim();

			if (string.IsNullOrWhiteSpace(text3))

			{

				continue;

			}

			string text4 = (item?.Name?.ToString() ?? "").Trim();

			dictionary[text3] = string.IsNullOrWhiteSpace(text4) ? text3 : text4;

			if (ports.Eligible(item) && hashSet.Add(text3))

			{

				list2.Add(text3);

			}

		}

		List<string> kingdomIdsByPlayerProximity = ports.Proximity(list2);

		bool flag = !string.IsNullOrWhiteSpace(text) && hashSet.Contains(text);

		List<string> list3 = ports.SelectSnapshot(text, false, flag, kingdomIdsByPlayerProximity, list2);

		List<string> list4 = ports.SelectSnapshot(text, true, flag, kingdomIdsByPlayerProximity, list2);

		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (string item2 in list3)

		{

			hashSet2.Add(item2);

		}

		foreach (string item3 in list4)

		{

			hashSet2.Add(item3);

		}

		if (!string.IsNullOrWhiteSpace(text))

		{

			hashSet2.Add(text);

		}

		if (!string.IsNullOrWhiteSpace(text2))

		{

			hashSet2.Add(text2);

		}

		Dictionary<string, WeeklyPromptReportSnapshot> dictionary2 = CaptureLatestWeeklyPromptReportSnapshots(ports.Records(), hashSet2, out WeeklyPromptReportSnapshot worldReport);

		dictionary2.TryGetValue(text, out WeeklyPromptReportSnapshot value);

		dictionary2.TryGetValue(text2, out WeeklyPromptReportSnapshot value2);

		string text5 = BuildWeeklyShortReportsPromptBlockFromSnapshot(list3, dictionary2, dictionary);

		string text6 = BuildWeeklyShortReportsPromptBlockFromSnapshot(list4, dictionary2, dictionary);

		string text7 = BuildSingleWeeklyFullReportPromptBlockFromSnapshot(AdapterPromptCaptureTexts.PcapText("weekly_npc_kingdom_full"), value);

		string text8 = BuildSingleWeeklyFullReportPromptBlockFromSnapshot(AdapterPromptCaptureTexts.PcapText("weekly_world_full"), worldReport);

		string text9 = BuildSingleWeeklyFullReportPromptBlockFromSnapshot(AdapterPromptCaptureTexts.PcapText("weekly_nearby_full"), value2);

		stopwatch.Stop();

		Logger.Log("Logic", "[NativePerf] weekly_prompt_snapshot_mainthread target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " ms=" + Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2) + " candidates=" + hashSet2.Count + " records=" + dictionary2.Count + " entries=" + (ports.Records()?.Count ?? 0));

		FreezeWatchdog.Mark("WeeklyPrompt.Capture.done", "ms=" + Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2) + " candidates=" + hashSet2.Count + " records=" + dictionary2.Count + " entries=" + (ports.Records()?.Count ?? 0), immediate: true);

		return new WeeklyPromptSnapshot(text5, text6, text7, text8, text9, text, text2);

	}

internal static string BuildWeeklyShortReportsPromptBlock(CapturePorts ports, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride, bool excludeNpcKingdom, WeeklyPromptSnapshot weeklyPromptSnapshot = null)

	{

		if (weeklyPromptSnapshot == null && ports.BulletinEnabled() && TWParallel.IsMainThread())

		{

			weeklyPromptSnapshot = WorldBulletinNpcPromptCaptureAdapter.CaptureWorldBulletinNpcSnapshot(ports, targetHero, targetCharacter, kingdomIdOverride);

		}

		if (weeklyPromptSnapshot != null)

		{

			FreezeWatchdog.Mark("WeeklyPrompt.Short.snapshot", "excludeNpc=" + excludeNpcKingdom + " thread=" + Thread.CurrentThread.ManagedThreadId);

			return excludeNpcKingdom ? weeklyPromptSnapshot.ShortReportsExcludingNpc : weeklyPromptSnapshot.ShortReportsIncludingNpc;

		}

		if (!TWParallel.IsMainThread())

		{

			Logger.Log("EventWeeklyReport", "[WeeklyPrompt][WARN] skipped live short-report query off the main thread.");

			return "";

		}

		ports.EnsureOpening();

		FreezeWatchdog.Mark("WeeklyPrompt.Short.resolve_npc_kingdom_start", "thread=" + Thread.CurrentThread.ManagedThreadId);

		string weeklyReportNpcKingdomId = ports.NpcKingdom(targetHero, targetCharacter, kingdomIdOverride);

		FreezeWatchdog.Mark("WeeklyPrompt.Short.resolve_npc_kingdom_done", "kingdom=" + (weeklyReportNpcKingdomId ?? "") + " thread=" + Thread.CurrentThread.ManagedThreadId);

		FreezeWatchdog.Mark("WeeklyPrompt.Short.select_kingdoms_start", "npcKingdom=" + (weeklyReportNpcKingdomId ?? "") + " excludeNpc=" + excludeNpcKingdom);

		List<string> list = ports.SelectLive(weeklyReportNpcKingdomId, excludeNpcKingdom);

		FreezeWatchdog.Mark("WeeklyPrompt.Short.select_kingdoms_done", "count=" + (list?.Count ?? 0) + " ids=" + ((list == null || list.Count == 0) ? "(none)" : string.Join(",", list)));

		if (list.Count == 0)

		{

			return "";

		}

		StringBuilder stringBuilder = new StringBuilder();

		int num = 0;

		foreach (string item in list)

		{

			FreezeWatchdog.Mark("WeeklyPrompt.Short.record_start", "kingdom=" + (item ?? ""));

			EventRecordEntry latestWeeklyReportRecord = ports.Latest("kingdom", item);

			FreezeWatchdog.Mark("WeeklyPrompt.Short.record_done", "kingdom=" + (item ?? "") + " found=" + (latestWeeklyReportRecord != null));

			string text = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(latestWeeklyReportRecord?.ShortSummary ?? latestWeeklyReportRecord?.Summary);

			if (latestWeeklyReportRecord == null || string.IsNullOrWhiteSpace(text))

			{

				continue;

			}

			if (num == 0)

			{

				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_three_header"));

				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("weekly_three_intro"));

			}

			num++;

			stringBuilder.Append("- ").Append(MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay(item));

			if (latestWeeklyReportRecord.WeekIndex >= 0)

			{

				stringBuilder.Append("（").Append(WeeklyEventRecordStateOwner.BuildWeeklyEpicWeekLabel(latestWeeklyReportRecord.WeekIndex, FormatNewsCalendarDate)).Append("）");

			}

			stringBuilder.Append("：").AppendLine(text);

		}

		return num > 0 ? stringBuilder.ToString().TrimEnd() : "";

	}

internal static string BuildTriggeredWeeklyFullReportsPromptBlock(CapturePorts ports, string triggeredRuleInstructions, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null, WeeklyPromptSnapshot weeklyPromptSnapshot = null)

	{

		if (weeklyPromptSnapshot == null && ports.BulletinEnabled() && TWParallel.IsMainThread())

		{

			weeklyPromptSnapshot = WorldBulletinNpcPromptCaptureAdapter.CaptureWorldBulletinNpcSnapshot(ports, targetHero, targetCharacter, kingdomIdOverride);

		}

		if (weeklyPromptSnapshot != null)

		{

			FreezeWatchdog.Mark("WeeklyPrompt.Full.snapshot", "thread=" + Thread.CurrentThread.ManagedThreadId);

			return BuildTriggeredWeeklyFullReportsPromptBlockFromSnapshot(triggeredRuleInstructions, weeklyPromptSnapshot);

		}

		if (!TWParallel.IsMainThread())

		{

			Logger.Log("EventWeeklyReport", "[WeeklyPrompt][WARN] skipped live full-report query off the main thread.");

			return "";

		}

		FreezeWatchdog.Mark("WeeklyPrompt.Full.start", "thread=" + Thread.CurrentThread.ManagedThreadId);

		bool flag = PromptRuleBlockText.Has(triggeredRuleInstructions, "npc_major_actions");

		bool flag3 = PromptRuleBlockText.Has(triggeredRuleInstructions, "surroundings");

		if (!flag && !flag3)

		{

			FreezeWatchdog.Mark("WeeklyPrompt.Full.skip", "reason=no_triggered_weekly_rule");

			return "";

		}

		FreezeWatchdog.Mark("WeeklyPrompt.Full.resolve_kingdoms_start", "major=" + flag + " surroundings=" + flag3);

		string weeklyReportNpcKingdomId = ports.NpcKingdom(targetHero, targetCharacter, kingdomIdOverride);

		string weeklyReportSurroundingsKingdomId = ports.SurroundingsKingdom(targetHero, targetCharacter, kingdomIdOverride);

		FreezeWatchdog.Mark("WeeklyPrompt.Full.resolve_kingdoms_done", "npc=" + (weeklyReportNpcKingdomId ?? "") + " surroundings=" + (weeklyReportSurroundingsKingdomId ?? ""));

		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		StringBuilder stringBuilder = new StringBuilder();

		if (flag)

		{

			FreezeWatchdog.Mark("WeeklyPrompt.Full.npc_record_start", "kingdom=" + (weeklyReportNpcKingdomId ?? ""));

			EventRecordEntry latestWeeklyReportRecord = ports.Latest("kingdom", weeklyReportNpcKingdomId);

			FreezeWatchdog.Mark("WeeklyPrompt.Full.npc_record_done", "found=" + (latestWeeklyReportRecord != null));

			string singleWeeklyFullReportPromptBlock = BuildSingleWeeklyFullReportPromptBlock(AdapterPromptCaptureTexts.PcapText("weekly_npc_kingdom_full"), latestWeeklyReportRecord);

			if (!string.IsNullOrWhiteSpace(singleWeeklyFullReportPromptBlock) && hashSet.Add("kingdom:" + (weeklyReportNpcKingdomId ?? "").Trim()))

			{

				stringBuilder.AppendLine(singleWeeklyFullReportPromptBlock);

			}

			FreezeWatchdog.Mark("WeeklyPrompt.Full.world_record_start");

			EventRecordEntry latestWeeklyReportRecord2 = ports.Latest("world", "");

			FreezeWatchdog.Mark("WeeklyPrompt.Full.world_record_done", "found=" + (latestWeeklyReportRecord2 != null));

			string singleWeeklyFullReportPromptBlock2 = BuildSingleWeeklyFullReportPromptBlock(AdapterPromptCaptureTexts.PcapText("weekly_world_full"), latestWeeklyReportRecord2);

			if (!string.IsNullOrWhiteSpace(singleWeeklyFullReportPromptBlock2) && hashSet.Add("world"))

			{

				if (stringBuilder.Length > 0)

				{

					stringBuilder.AppendLine();

				}

				stringBuilder.AppendLine(singleWeeklyFullReportPromptBlock2);

			}

		}

		if (flag3)

		{

			FreezeWatchdog.Mark("WeeklyPrompt.Full.surroundings_record_start", "kingdom=" + (weeklyReportSurroundingsKingdomId ?? ""));

			EventRecordEntry latestWeeklyReportRecord3 = ports.Latest("kingdom", weeklyReportSurroundingsKingdomId);

			FreezeWatchdog.Mark("WeeklyPrompt.Full.surroundings_record_done", "found=" + (latestWeeklyReportRecord3 != null));

			string singleWeeklyFullReportPromptBlock3 = BuildSingleWeeklyFullReportPromptBlock(AdapterPromptCaptureTexts.PcapText("weekly_nearby_full"), latestWeeklyReportRecord3);

			if (!string.IsNullOrWhiteSpace(singleWeeklyFullReportPromptBlock3) && hashSet.Add("kingdom:" + (weeklyReportSurroundingsKingdomId ?? "").Trim()))

			{

				if (stringBuilder.Length > 0)

				{

					stringBuilder.AppendLine();

				}

				stringBuilder.AppendLine(singleWeeklyFullReportPromptBlock3);

			}

		}

		string result = stringBuilder.ToString().TrimEnd();

		FreezeWatchdog.Mark("WeeklyPrompt.Full.done", "chars=" + result.Length);

		return result;

	}



    internal sealed class RequestPromptCapturePorts

    {

        internal Func<MyBehavior.WeeklyReportPromptProfile> Profile;

        internal Func<string> WritingRequirements;

        internal Func<WeeklyEventMaterialPreviewGroup, string> Stability;

        internal Func<WeeklyEventMaterialPreviewGroup, int, string> Previous;

        internal Func<WeeklyEventMaterialPreviewGroup, string> Materials;

        internal Func<WeeklyEventMaterialPreviewGroup, int, string> DefaultTitle;

    }



internal static string BuildWeeklyReportFullOnDemandSystemPrompt(RequestPromptCapturePorts ports, WeeklyEventMaterialPreviewGroup group)

	{

		WeeklyReportPromptProfile weeklyReportPromptProfile = ports.Profile();

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_031"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_032"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_033"));

		WeeklyPromptMaterialOwner.AppendWeeklyReportWritingRequirements(stringBuilder, ports.WritingRequirements());

		WeeklyPromptMaterialOwner.AppendWeeklyReportVillageRaidWritingRule(stringBuilder);

		WeeklyPromptMaterialOwner.AppendWeeklyReportPoliticalReasonWritingRule(stringBuilder, true);

		WeeklyPromptMaterialOwner.AppendWeeklyReportSettlementReasonWritingRule(stringBuilder);

		stringBuilder.AppendLine(" ");

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_034"));

		stringBuilder.AppendLine(string.Format(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_035"), weeklyReportPromptProfile.Label));

		stringBuilder.AppendLine(string.Format(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_036"), weeklyReportPromptProfile.MinWords, weeklyReportPromptProfile.MaxWords));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_037"));

		stringBuilder.AppendLine(" ");

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_038"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_039"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_040"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_041"));

		stringBuilder.AppendLine(" ");

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_042"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_043"));

		return PromptComposerTexts.ApplyOutputLanguage(stringBuilder.ToString().TrimEnd());

	}

internal static string BuildWeeklyReportFullOnDemandUserPrompt(RequestPromptCapturePorts ports, WeeklyEventMaterialPreviewGroup group, int weekIndex)

	{

		WeeklyReportPromptProfile weeklyReportPromptProfile = ports.Profile();

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_044"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_045") + (string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_046") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_047")));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_048") + weekIndex + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_049"));

		if (!string.IsNullOrWhiteSpace(group?.KingdomId))

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_050") + MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay(group.KingdomId));

		}

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_051") + weeklyReportPromptProfile.Label);

		stringBuilder.AppendLine(" ");

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_052"));

		stringBuilder.AppendLine(ports.Materials(group));

		stringBuilder.AppendLine();

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_053"));

		return stringBuilder.ToString().TrimEnd();

	}

internal static string BuildWeeklyReportUserPrompt(RequestPromptCapturePorts ports, WeeklyEventMaterialPreviewGroup group, int weekIndex, int startDay, int endDay)

	{

		WeeklyReportPromptProfile weeklyReportPromptProfile = ports.Profile();

		bool flag = (group?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly;

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_054"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_055") + (string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_056") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_057")));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_058") + (flag ? "title_short_tags_only" : "full_report"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_059") + weekIndex + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_060"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_061") + Math.Max(0, startDay) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_062") + Math.Max(startDay, endDay) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_063"));

		if (!string.IsNullOrWhiteSpace(group?.KingdomId))

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_064") + MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay(group.KingdomId));

		}

		string weeklyReportCurrentKingdomStabilityTierText = ports.Stability(group);

		if (!string.IsNullOrWhiteSpace(weeklyReportCurrentKingdomStabilityTierText))

		{

			stringBuilder.AppendLine(weeklyReportCurrentKingdomStabilityTierText);

		}

		if (string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_065"));

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_066"));

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_067"));

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_068"));

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_069"));

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_070"));

		}

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_071") + weeklyReportPromptProfile.Label);

		stringBuilder.AppendLine(" ");

		if (!flag && (group?.IncludePreviousReportInPrompt ?? true))

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_072"));

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_073"));

			stringBuilder.AppendLine(ports.Previous(group, weekIndex));

			stringBuilder.AppendLine();

		}

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_074"));

		stringBuilder.AppendLine(ports.Materials(group));

		stringBuilder.AppendLine();

		if (flag)

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_075"));

		}

		else

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_076"));

		}

		return stringBuilder.ToString().TrimEnd();

	}

internal static string BuildWeeklyBatchReportSystemPrompt(RequestPromptCapturePorts ports, WeeklyReportBatchRequest batch)

	{

		WeeklyReportPromptProfile weeklyReportPromptProfile = ports.Profile();

		bool flag = (batch?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly;

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_077"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_078"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_079") + (flag ? "title_short_tags_only" : "full_report") + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_080"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_081"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_082"));

		WeeklyPromptMaterialOwner.AppendWeeklyReportWritingRequirements(stringBuilder, ports.WritingRequirements());

		WeeklyPromptMaterialOwner.AppendWeeklyReportVillageRaidWritingRule(stringBuilder);

		WeeklyPromptMaterialOwner.AppendWeeklyReportPoliticalReasonWritingRule(stringBuilder, !flag);

		if (!flag)

		{

			WeeklyPromptMaterialOwner.AppendWeeklyReportSettlementReasonWritingRule(stringBuilder);

		}

		stringBuilder.AppendLine(" ");

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_083"));

		stringBuilder.AppendLine(string.Format(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_084"), weeklyReportPromptProfile.Label));

		if (!flag)

		{

			stringBuilder.AppendLine(string.Format(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_085"), weeklyReportPromptProfile.MinWords, weeklyReportPromptProfile.MaxWords));

		}

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_086"));

		stringBuilder.AppendLine(" ");

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_087"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_088"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_089"));

		stringBuilder.AppendLine("[REPORT_BLOCK_BEGIN]");

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_090"));

		stringBuilder.AppendLine("mode=" + (flag ? "title_short_tags_only" : "full_report"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_091"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_092"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_093"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_094"));

		if (!flag)

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_095"));

		}

		stringBuilder.AppendLine("[TAGS]");

		stringBuilder.AppendLine("STAB_FLAT");

		stringBuilder.AppendLine("[REPORT_BLOCK_END]");

		if (flag)

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_096"));

		}

		else

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_097"));

		}

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_098"));

		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_099"));

		return PromptComposerTexts.ApplyOutputLanguage(stringBuilder.ToString().TrimEnd());

	}

internal static string BuildWeeklyBatchReportUserPrompt(RequestPromptCapturePorts ports, WeeklyReportBatchRequest batch)

	{

		StringBuilder stringBuilder = new StringBuilder();

		List<WeeklyEventMaterialPreviewGroup> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();

		stringBuilder.AppendLine("[BATCH]");

		stringBuilder.AppendLine("batch_mode=" + (((batch?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report"));

		stringBuilder.AppendLine("week_index=" + ((batch != null) ? batch.WeekIndex : 0));

		stringBuilder.AppendLine("day_range=" + ((batch != null) ? batch.StartDay : 0) + "-" + ((batch != null) ? batch.EndDay : 0));

		stringBuilder.AppendLine("block_count=" + list.Count);

		foreach (WeeklyEventMaterialPreviewGroup item in list)

		{

			stringBuilder.AppendLine();

			stringBuilder.AppendLine(BuildWeeklyBatchPromptInputBlock(ports, item, (batch != null) ? batch.WeekIndex : 0));

		}

		return stringBuilder.ToString().TrimEnd();

	}

internal static string BuildWeeklyBatchPromptInputBlock(RequestPromptCapturePorts ports, WeeklyEventMaterialPreviewGroup group, int weekIndex)

	{

		bool flag = string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);

		bool flag2 = (group?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly;

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(flag ? "[WORLD_BLOCK]" : "[KINGDOM_BLOCK]");

		stringBuilder.AppendLine("report_id=" + WeeklyGenerationRules.BuildWeeklyReportGroupReportId(group));

		stringBuilder.AppendLine("mode=" + (flag2 ? "title_short_tags_only" : "full_report"));

		stringBuilder.AppendLine("kind=" + (flag ? "world" : "kingdom"));

		if (!flag)

		{

			stringBuilder.AppendLine("kingdom_id=" + ((group?.KingdomId ?? "").Trim()));

			stringBuilder.AppendLine("kingdom_name=" + MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay(group?.KingdomId));

			string weeklyReportCurrentKingdomStabilityTierText = ports.Stability(group);

			if (!string.IsNullOrWhiteSpace(weeklyReportCurrentKingdomStabilityTierText))

			{

				stringBuilder.AppendLine("stability=" + weeklyReportCurrentKingdomStabilityTierText.Replace(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0887"), "").Trim());

			}

		}

		stringBuilder.AppendLine("title_hint=" + ports.DefaultTitle(group, weekIndex));

		if (!flag2 && (group?.IncludePreviousReportInPrompt ?? true))

		{

			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_100"));

			stringBuilder.AppendLine("previous_report=");

			stringBuilder.AppendLine(ports.Previous(group, weekIndex));

		}

		stringBuilder.AppendLine("materials=");

		stringBuilder.AppendLine(ports.Materials(group));

		return stringBuilder.ToString().TrimEnd();

	}

internal static WeeklyPromptSnapshot CaptureWeeklyPromptSnapshotForExternal(Func<CapturePorts> resolvePorts, Hero targetHero, CharacterObject targetCharacter = null, string kingdomIdOverride = null)

	{

		try

		{

			if (!TWParallel.IsMainThread())

			{

				Logger.Log("EventWeeklyReport", "[WeeklyPrompt][WARN] refused off-main-thread snapshot capture.");

				return WeeklyPromptSnapshot.Empty;

			}

			CapturePorts ports = resolvePorts();

			return ports != null ? CaptureWeeklyPromptSnapshot(ports, targetHero, targetCharacter, kingdomIdOverride) : WeeklyPromptSnapshot.Empty;

		}

		catch (Exception ex)

		{

			Logger.Log("EventWeeklyReport", "[WeeklyPrompt][WARN] main-thread snapshot capture failed: " + ex.Message);

			return WeeklyPromptSnapshot.Empty;

		}

	}

internal static string BuildWeeklyReportPromptMaterialLines(WeeklyEventMaterialPreviewGroup group)

	{

		List<EventMaterialReference> list = (group?.PromptMaterials != null && group.PromptMaterials.Count > 0) ? group.PromptMaterials : group?.Materials;

		return MemoryEntityIdentityBannerlordAdapter.AnnotateWeeklyReportRulerInMaterialText(WeeklyEventRecordStateOwner.BuildWeeklyReportMaterialLines(list, MemoryEntityIdentityBannerlordAdapter.BuildWeeklyReportMaterialLine), group);

	}

}

