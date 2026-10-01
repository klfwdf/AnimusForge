using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EventRecordEntry=AnimusForge.MyBehavior.EventRecordEntry;
using EventMaterialReference=AnimusForge.MyBehavior.EventMaterialReference;
using EventImportPayload=AnimusForge.MyBehavior.EventImportPayload;
namespace AnimusForge;
// These operations mutate only the existing save-owned containers passed by reference.
// No copied live dictionary, new state table, or cross-domain transaction is introduced.
internal static class WeeklyEventDataImportOwner
{
    internal static void ApplyOpening(EventImportPayload payload, bool overwriteExisting,
        ref string worldSummary, ref Dictionary<string,string> kingdomSummaries, Action markOpening)
    {
		if (payload.HasWorldSummaryFile && (overwriteExisting || string.IsNullOrWhiteSpace(worldSummary)))
		{
			worldSummary = (payload.WorldSummary ?? "").Trim();
		}
		if (payload.HasKingdomSummariesFile)
		{
			if (kingdomSummaries == null)
			{
				kingdomSummaries = new Dictionary<string, string>();
			}
			foreach (KeyValuePair<string, string> item in payload.KingdomSummaries)
			{
				string text = (item.Key ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string text2 = (item.Value ?? "").Trim();
				if (overwriteExisting)
				{
					if (string.IsNullOrWhiteSpace(text2))
					{
						kingdomSummaries.Remove(text);
					}
					else
					{
						kingdomSummaries[text] = text2;
					}
				}
				else if (!kingdomSummaries.ContainsKey(text) && !string.IsNullOrWhiteSpace(text2))
				{
					kingdomSummaries[text] = text2;
				}
			}
		}
		if (payload.HasWorldSummaryFile || payload.HasKingdomSummariesFile)
		{
			markOpening();
		}

    }
    internal static void ApplyRecords(EventImportPayload payload, bool overwriteExisting,
        ref List<EventRecordEntry> records, Func<List<EventRecordEntry>,List<EventRecordEntry>> sanitize)
    {
			if (records == null)
			{
				records = new List<EventRecordEntry>();
			}
			if (overwriteExisting)
			{
				Dictionary<string, EventRecordEntry> dictionary = new Dictionary<string, EventRecordEntry>(StringComparer.OrdinalIgnoreCase);
				foreach (EventRecordEntry eventRecordEntry in records)
				{
					string text3 = (eventRecordEntry?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						dictionary[text3] = eventRecordEntry;
					}
				}
				foreach (EventRecordEntry eventRecordEntry2 in payload.EventRecords)
				{
					string text4 = (eventRecordEntry2?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						dictionary[text4] = eventRecordEntry2;
					}
				}
				records = sanitize(dictionary.Values.ToList());
			}
			else
			{
				HashSet<string> hashSet = new HashSet<string>(records.Where((EventRecordEntry x) => x != null && !string.IsNullOrWhiteSpace(x.EventId)).Select((EventRecordEntry x) => x.EventId.Trim()), StringComparer.OrdinalIgnoreCase);
				foreach (EventRecordEntry eventRecordEntry3 in payload.EventRecords)
				{
					string text5 = (eventRecordEntry3?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text5) && hashSet.Add(text5))
					{
						records.Add(eventRecordEntry3);
					}
				}
				records = sanitize(records);
			}
    }
	internal static List<EventRecordEntry> SanitizeEventRecordEntries(List<EventRecordEntry> source, Func<string,string> neutralize, Func<string,string> shortSummary, Func<string,string> normalizeTags)
	{
		List<EventRecordEntry> list = new List<EventRecordEntry>();
		if (source == null)
		{
			return list;
		}
		foreach (EventRecordEntry item in source)
		{
			if (item == null)
			{
				continue;
			}
			string text = (item.EventId ?? "").Trim();
			string text2 = (item.Title ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			bool flag = text.StartsWith("weekly_report:", StringComparison.OrdinalIgnoreCase);
			EventRecordEntry eventRecordEntry = new EventRecordEntry
			{
				EventId = text,
				WeekIndex = Math.Max(0, item.WeekIndex),
				EventKind = (item.EventKind ?? "").Trim(),
				ScopeKingdomId = (item.ScopeKingdomId ?? "").Trim(),
				Title = flag ? neutralize(text2) : text2,
				ShortSummary = shortSummary(item.ShortSummary),
				Summary = flag ? neutralize(item.Summary) : (item.Summary ?? "").Trim(),
				TagText = normalizeTags(item.TagText),
				PromptText = flag ? neutralize(item.PromptText) : (item.PromptText ?? "").Trim(),
				CreatedDay = Math.Max(0, item.CreatedDay),
				CreatedDate = (item.CreatedDate ?? "").Trim(),
				Materials = new List<EventMaterialReference>()
			};
			if (string.IsNullOrWhiteSpace(eventRecordEntry.ShortSummary))
			{
				eventRecordEntry.ShortSummary = shortSummary(eventRecordEntry.Summary);
			}
			if (item.Materials != null)
			{
				foreach (EventMaterialReference material in item.Materials)
				{
					if (material == null)
					{
						continue;
					}
					eventRecordEntry.Materials.Add(new EventMaterialReference
					{
						MaterialType = (material.MaterialType ?? "").Trim(),
						Label = flag ? neutralize(material.Label) : (material.Label ?? "").Trim(),
						SnapshotText = flag ? neutralize(material.SnapshotText) : (material.SnapshotText ?? "").Trim(),
						HeroId = (material.HeroId ?? "").Trim(),
						KingdomId = (material.KingdomId ?? "").Trim(),
						SettlementId = (material.SettlementId ?? "").Trim(),
						RecentOnly = material.RecentOnly,
						ActionKind = (material.ActionKind ?? "").Trim(),
						ActorHeroId = (material.ActorHeroId ?? "").Trim(),
						ActorClanId = (material.ActorClanId ?? "").Trim(),
						ActorKingdomId = (material.ActorKingdomId ?? "").Trim(),
						TargetHeroId = (material.TargetHeroId ?? "").Trim(),
						TargetClanId = (material.TargetClanId ?? "").Trim(),
						TargetKingdomId = (material.TargetKingdomId ?? "").Trim(),
						SettlementOwnerHeroId = (material.SettlementOwnerHeroId ?? "").Trim(),
						SettlementOwnerClanId = (material.SettlementOwnerClanId ?? "").Trim(),
						SettlementOwnerKingdomId = (material.SettlementOwnerKingdomId ?? "").Trim(),
						PreviousSettlementOwnerHeroId = (material.PreviousSettlementOwnerHeroId ?? "").Trim(),
						PreviousSettlementOwnerClanId = (material.PreviousSettlementOwnerClanId ?? "").Trim(),
						PreviousSettlementOwnerKingdomId = (material.PreviousSettlementOwnerKingdomId ?? "").Trim(),
						LocationText = (material.LocationText ?? "").Trim(),
						Won = material.Won,
						RelatedHeroIds = new List<string>((material.RelatedHeroIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						RelatedClanIds = new List<string>((material.RelatedClanIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						RelatedKingdomIds = new List<string>((material.RelatedKingdomIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						SourceStableKeys = new List<string>((material.SourceStableKeys ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						SourceActionKinds = new List<string>((material.SourceActionKinds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						SourceMaterialCount = Math.Max(0, material.SourceMaterialCount),
						ActionStableKey = (material.ActionStableKey ?? "").Trim(),
						ActionDay = material.ActionDay,
						ActionOrder = material.ActionOrder,
						ActionSequence = material.ActionSequence
					});
				}
			}
			list.Add(eventRecordEntry);
		}
		return list.OrderByDescending((EventRecordEntry x) => x.WeekIndex).ThenByDescending((EventRecordEntry x) => x.CreatedDay).ThenBy((EventRecordEntry x) => x.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}
	internal static bool TryLoadEventDataFromImportDir(string importDir, out EventImportPayload payload, out string error, Func<string,string> readWorldSummary, Func<string,Dictionary<string,string>> readKingdomSummaries, Func<string,List<EventRecordEntry>> readRecords, Func<List<EventRecordEntry>,List<EventRecordEntry>> sanitize)
	{
		payload = new EventImportPayload();
		error = "";
		try
		{
			string text = Path.Combine(importDir, "event_data");
			string text2 = Directory.Exists(text) ? text : importDir;
			string path = Path.Combine(text2, "WorldOpeningSummary.json");
			if (File.Exists(path))
			{
				payload.HasWorldSummaryFile = true;
				string worldSummary = readWorldSummary(path);
				payload.WorldSummary = (worldSummary ?? "").Trim();
			}
			string path2 = Path.Combine(text2, "KingdomOpeningSummaries.json");
			if (File.Exists(path2))
			{
				payload.HasKingdomSummariesFile = true;
				Dictionary<string, string> dictionary = readKingdomSummaries(path2) ?? new Dictionary<string, string>();
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					string text3 = (item.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						payload.KingdomSummaries[text3] = (item.Value ?? "").Trim();
					}
				}
			}
			string path3 = Path.Combine(text2, "EventRecords.json");
			if (File.Exists(path3))
			{
				payload.HasEventRecordsFile = true;
				List<EventRecordEntry> source = readRecords(path3) ?? new List<EventRecordEntry>();
				payload.EventRecords = sanitize(source);
			}
			if (!payload.HasWorldSummaryFile && !payload.HasKingdomSummariesFile && !payload.HasEventRecordsFile)
			{
				error = "找不到 event_data\\WorldOpeningSummary.json、event_data\\KingdomOpeningSummaries.json 或 event_data\\EventRecords.json。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}
    internal static int ReplaceOpening(EventImportPayload payload, ref string worldSummary,
        ref Dictionary<string,string> kingdomSummaries, ref List<EventRecordEntry> records,
        Action markOpening, out bool removedWorldOpeningRecord)
    {
		if (payload == null || !payload.HasWorldSummaryFile || !payload.HasKingdomSummariesFile)
		{
			throw new InvalidOperationException("开局知识重载计划不完整。");
		}
		worldSummary = (payload.WorldSummary ?? "").Trim();
		kingdomSummaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> sourceSummary in payload.KingdomSummaries ?? new Dictionary<string, string>())
		{
			string kingdomId = (sourceSummary.Key ?? "").Trim();
			string summary = (sourceSummary.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(kingdomId) && !string.IsNullOrWhiteSpace(summary))
			{
				kingdomSummaries[kingdomId] = summary;
			}
		}
		markOpening();
		if (records == null)
		{
			records = new List<EventRecordEntry>();
		}
		removedWorldOpeningRecord = records.Any((EventRecordEntry x) => IsCanonicalOpeningEvent(x) && string.Equals((x.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase));
		int removedOpeningRecordCount = records.RemoveAll(IsCanonicalOpeningEvent);
		return removedOpeningRecordCount;
    }
    internal static bool IsCanonicalOpeningEvent(EventRecordEntry entry)
        => entry != null && entry.WeekIndex == 0 && IsCanonicalOpeningEventId(entry.EventId);
    internal static bool IsCanonicalOpeningEventId(string eventId)
    {
        string normalizedEventId = (eventId ?? "").Trim();
        return normalizedEventId.StartsWith("weekly_report:world:0:", StringComparison.OrdinalIgnoreCase)
            || normalizedEventId.StartsWith("weekly_report:kingdom:0:", StringComparison.OrdinalIgnoreCase);
    }

	internal static void NormalizeEventRecordEntriesInPlace(List<EventRecordEntry> source, Func<string,string> neutralize, Func<string,string> shortSummary, Func<string,string> normalizeTags)
	{
		if (source == null)
		{
			return;
		}
		for (int num = source.Count - 1; num >= 0; num--)
		{
			EventRecordEntry eventRecordEntry = source[num];
			if (eventRecordEntry == null)
			{
				source.RemoveAt(num);
				continue;
			}
			string text = (eventRecordEntry.EventId ?? "").Trim();
			string text2 = (eventRecordEntry.Title ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
			{
				source.RemoveAt(num);
				continue;
			}
			bool flag = text.StartsWith("weekly_report:", StringComparison.OrdinalIgnoreCase);
			eventRecordEntry.EventId = text;
			eventRecordEntry.WeekIndex = Math.Max(0, eventRecordEntry.WeekIndex);
			eventRecordEntry.EventKind = (eventRecordEntry.EventKind ?? "").Trim();
			eventRecordEntry.ScopeKingdomId = (eventRecordEntry.ScopeKingdomId ?? "").Trim();
			eventRecordEntry.Title = flag ? neutralize(text2) : text2;
			eventRecordEntry.ShortSummary = shortSummary(eventRecordEntry.ShortSummary);
			eventRecordEntry.Summary = flag ? neutralize(eventRecordEntry.Summary) : (eventRecordEntry.Summary ?? "").Trim();
			eventRecordEntry.TagText = normalizeTags(eventRecordEntry.TagText);
			eventRecordEntry.PromptText = flag ? neutralize(eventRecordEntry.PromptText) : (eventRecordEntry.PromptText ?? "").Trim();
			eventRecordEntry.CreatedDay = Math.Max(0, eventRecordEntry.CreatedDay);
			eventRecordEntry.CreatedDate = (eventRecordEntry.CreatedDate ?? "").Trim();
			if (string.IsNullOrWhiteSpace(eventRecordEntry.ShortSummary))
			{
				eventRecordEntry.ShortSummary = shortSummary(eventRecordEntry.Summary);
			}
			eventRecordEntry.Materials = NormalizeEventMaterialReferencesInPlace(eventRecordEntry.Materials, flag, neutralize);
		}
		List<EventRecordEntry> list = source.OrderByDescending((EventRecordEntry x) => x.WeekIndex).ThenByDescending((EventRecordEntry x) => x.CreatedDay).ThenBy((EventRecordEntry x) => x.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		source.Clear();
		source.AddRange(list);
	}

	internal static List<EventMaterialReference> NormalizeEventMaterialReferencesInPlace(List<EventMaterialReference> materials, bool neutralizeWeeklyText, Func<string,string> neutralize)
	{
		List<EventMaterialReference> list = materials ?? new List<EventMaterialReference>();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			EventMaterialReference eventMaterialReference = list[num];
			if (eventMaterialReference == null)
			{
				list.RemoveAt(num);
				continue;
			}
			eventMaterialReference.MaterialType = (eventMaterialReference.MaterialType ?? "").Trim();
			eventMaterialReference.Label = neutralizeWeeklyText ? neutralize(eventMaterialReference.Label) : (eventMaterialReference.Label ?? "").Trim();
			eventMaterialReference.SnapshotText = neutralizeWeeklyText ? neutralize(eventMaterialReference.SnapshotText) : (eventMaterialReference.SnapshotText ?? "").Trim();
			eventMaterialReference.HeroId = (eventMaterialReference.HeroId ?? "").Trim();
			eventMaterialReference.KingdomId = (eventMaterialReference.KingdomId ?? "").Trim();
			eventMaterialReference.SettlementId = (eventMaterialReference.SettlementId ?? "").Trim();
			eventMaterialReference.ActionKind = (eventMaterialReference.ActionKind ?? "").Trim();
			eventMaterialReference.ActorHeroId = (eventMaterialReference.ActorHeroId ?? "").Trim();
			eventMaterialReference.ActorClanId = (eventMaterialReference.ActorClanId ?? "").Trim();
			eventMaterialReference.ActorKingdomId = (eventMaterialReference.ActorKingdomId ?? "").Trim();
			eventMaterialReference.TargetHeroId = (eventMaterialReference.TargetHeroId ?? "").Trim();
			eventMaterialReference.TargetClanId = (eventMaterialReference.TargetClanId ?? "").Trim();
			eventMaterialReference.TargetKingdomId = (eventMaterialReference.TargetKingdomId ?? "").Trim();
			eventMaterialReference.SettlementOwnerHeroId = (eventMaterialReference.SettlementOwnerHeroId ?? "").Trim();
			eventMaterialReference.SettlementOwnerClanId = (eventMaterialReference.SettlementOwnerClanId ?? "").Trim();
			eventMaterialReference.SettlementOwnerKingdomId = (eventMaterialReference.SettlementOwnerKingdomId ?? "").Trim();
			eventMaterialReference.PreviousSettlementOwnerHeroId = (eventMaterialReference.PreviousSettlementOwnerHeroId ?? "").Trim();
			eventMaterialReference.PreviousSettlementOwnerClanId = (eventMaterialReference.PreviousSettlementOwnerClanId ?? "").Trim();
			eventMaterialReference.PreviousSettlementOwnerKingdomId = (eventMaterialReference.PreviousSettlementOwnerKingdomId ?? "").Trim();
			eventMaterialReference.LocationText = (eventMaterialReference.LocationText ?? "").Trim();
			eventMaterialReference.RelatedHeroIds = NormalizeEventMaterialIdListInPlace(eventMaterialReference.RelatedHeroIds);
			eventMaterialReference.RelatedClanIds = NormalizeEventMaterialIdListInPlace(eventMaterialReference.RelatedClanIds);
			eventMaterialReference.RelatedKingdomIds = NormalizeEventMaterialIdListInPlace(eventMaterialReference.RelatedKingdomIds);
			eventMaterialReference.SourceStableKeys = NormalizeEventMaterialIdListInPlace(eventMaterialReference.SourceStableKeys);
			eventMaterialReference.SourceActionKinds = NormalizeEventMaterialIdListInPlace(eventMaterialReference.SourceActionKinds);
			eventMaterialReference.SourceMaterialCount = Math.Max(0, eventMaterialReference.SourceMaterialCount);
			eventMaterialReference.ActionStableKey = (eventMaterialReference.ActionStableKey ?? "").Trim();
		}
		return list;
	}

	internal static List<string> NormalizeEventMaterialIdListInPlace(List<string> values)
	{
		List<string> list = values ?? new List<string>();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			string text = (list[num] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				list.RemoveAt(num);
			}
			else
			{
				list[num] = text;
			}
		}
		return list;
	}


    internal static void RestoreOpeningAndRecords(string worldSummary, Dictionary<string,string> openingSummaries,
        List<EventRecordEntry> restoredRecords, ref string currentWorldSummary,
        ref Dictionary<string,string> currentOpeningSummaries, ref List<EventRecordEntry> currentRecords, Action markOpening)
    {
        currentWorldSummary = worldSummary ?? "";
        currentOpeningSummaries = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string,string> summary in openingSummaries ?? new Dictionary<string,string>())
            currentOpeningSummaries[summary.Key ?? ""] = summary.Value ?? "";
        markOpening();
        currentRecords = restoredRecords;
    }
}
