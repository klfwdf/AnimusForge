using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;

// Save/load-only, no second event authority or host reference.
internal static class CampaignWeeklyRecordPersistenceAdapter
{
    internal static void SaveOpenings(IDataStore dataStore, Dictionary<string,string> summaries, Dictionary<string,string> storage, string world)
    {
				storage.Clear();
				foreach (KeyValuePair<string, string> item2 in summaries)
				{
					string text = (item2.Key ?? "").Trim();
					string text2 = (item2.Value ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
					{
						storage[text] = text2;
					}
				}
				Dictionary<string, string> dictionary6 = CampaignSaveChunkHelper.FlattenStringDictionary(storage, "_eventKingdomOpeningSummaries_v1", "EventOpeningSummary");
				dataStore.SyncData("_eventKingdomOpeningSummaries_v1", ref dictionary6);
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventWorldOpeningSummary_v1", world ?? "", "EventOpeningSummary");
    }

    internal static void LoadOpenings(IDataStore dataStore, Dictionary<string,string> summaries, ref Dictionary<string,string> storage, ref string world)
    {
			summaries.Clear();
			storage.Clear();
			Dictionary<string, string> dictionary12 = new Dictionary<string, string>();
			dataStore.SyncData("_eventKingdomOpeningSummaries_v1", ref dictionary12);
			storage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary12, "EventOpeningSummary");
			if (storage != null)
			{
				foreach (KeyValuePair<string, string> item6 in storage)
				{
					string text3 = (item6.Key ?? "").Trim();
					string text4 = (item6.Value ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text4))
					{
						summaries[text3] = text4;
					}
				}
			}
			world = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_eventWorldOpeningSummary_v1", "EventOpeningSummary") ?? "";
    }

    internal static void SaveRecords(IDataStore dataStore, List<MyBehavior.EventRecordEntry> records, ref string json, Action<List<MyBehavior.EventRecordEntry>> normalize)
    {
				Stopwatch eventRecordSaveStopwatch = Stopwatch.StartNew();
				try
				{
					normalize(records);
					json = JsonConvert.SerializeObject(records);
				}
				catch (Exception ex5)
				{
					json = "[]";
					Logger.Log("EventRecord", "[ERROR] Serialize event records failed: " + ex5.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventRecordEntries_v1", json ?? "[]", "EventRecord");
				eventRecordSaveStopwatch.Stop();
				Logger.Log("EventRecord", "[SyncData] save entries=" + (records?.Count ?? 0) + " chars=" + (json?.Length ?? 0) + " ms=" + Math.Round(eventRecordSaveStopwatch.Elapsed.TotalMilliseconds, 2));
				json = "";
    }

    internal static void LoadRecords(IDataStore dataStore, ref List<MyBehavior.EventRecordEntry> records, ref string json, Action<List<MyBehavior.EventRecordEntry>> normalize)
    {
			Stopwatch eventRecordLoadStopwatch = Stopwatch.StartNew();
			records.Clear();
			json = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_eventRecordEntries_v1", "EventRecord") ?? "";
			if (!string.IsNullOrWhiteSpace(json))
			{
				try
				{
					List<MyBehavior.EventRecordEntry> list4 = JsonConvert.DeserializeObject<List<MyBehavior.EventRecordEntry>>(json) ?? new List<MyBehavior.EventRecordEntry>();
					normalize(list4);
					records = list4;
				}
				catch (Exception ex7)
				{
					Logger.Log("EventRecord", "[ERROR] Deserialize event records failed: " + ex7.Message);
					records = new List<MyBehavior.EventRecordEntry>();
				}
			}
			eventRecordLoadStopwatch.Stop();
			Logger.Log("EventRecord", "[SyncData] load entries=" + (records?.Count ?? 0) + " chars=" + (json?.Length ?? 0) + " ms=" + Math.Round(eventRecordLoadStopwatch.Elapsed.TotalMilliseconds, 2));
			json = "";
    }

}
