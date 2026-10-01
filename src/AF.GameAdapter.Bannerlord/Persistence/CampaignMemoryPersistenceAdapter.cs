using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using DialogueDay = AnimusForge.MyBehavior.DialogueDay;

namespace AnimusForge;

// Executes only at Campaign SyncData. Stores are the existing Memory authority;
// JSON scratch values retain the legacy host aliases, keys and partial-failure order.
internal sealed class CampaignMemoryPersistenceAdapter
{
    private readonly MemoryBusinessStateOwner _state;
    internal string DailyQueueJson = "";
    internal string OverviewQueueJson = "";
    internal string MajorQueueJson = "";
    internal CampaignMemoryPersistenceAdapter(MemoryBusinessStateOwner state)
        => _state = state ?? throw new ArgumentNullException(nameof(state));

    internal void Save(IDataStore dataStore, Action<string> trace, Func<string, bool> isNonHeroMemoryId)
    {
				OwnerJsonStorageCodec.Serialize(_state.History, _state.HistoryStorage, skipWhitespaceKeys: false, skipEmptyLists: false, null, (key, ex) => Logger.Log("DialogueHistory", "[ERROR] Serialize history for " + key + ": " + ex.Message));
				trace("stage=sync_save_dialogue_storage owners=" + _state.HistoryStorage.Keys.Count(isNonHeroMemoryId) + " storageBytes=" + _state.HistoryStorage.Where((KeyValuePair<string, string> item) => isNonHeroMemoryId(item.Key)).Sum((KeyValuePair<string, string> item) => (item.Value ?? "").Length));
				Dictionary<string, string> dictionary2 = CampaignSaveChunkHelper.FlattenStringDictionary(_state.HistoryStorage, "_dialogueHistory_v2", "DialogueHistory");
				dataStore.SyncData("_dialogueHistory_v2", ref dictionary2);
				OwnerJsonStorageCodec.Serialize(_state.Drafts, _state.DraftStorage, skipWhitespaceKeys: true, skipEmptyLists: true, list => MemoryRecordRules.SanitizeDailyMemoryDrafts(list) ?? new List<DailyMemoryDraft>(), (key, ex) => Logger.Log("CompressedMemory", "[ERROR] Serialize daily memory drafts for " + key + ": " + ex.Message));
				trace("stage=sync_save_daily_storage owners=" + _state.DraftStorage.Keys.Count(isNonHeroMemoryId) + " storageBytes=" + _state.DraftStorage.Where((KeyValuePair<string, string> item) => isNonHeroMemoryId(item.Key)).Sum((KeyValuePair<string, string> item) => (item.Value ?? "").Length));
				Dictionary<string, string> dictionaryMemoryDrafts = CampaignSaveChunkHelper.FlattenStringDictionary(_state.DraftStorage, "_af_dailyMemoryDrafts_v1", "CompressedMemory");
				dataStore.SyncData("_af_dailyMemoryDrafts_v1", ref dictionaryMemoryDrafts);
				OwnerJsonStorageCodec.Serialize(_state.Blocks, _state.BlockStorage, skipWhitespaceKeys: true, skipEmptyLists: true, list => MemoryRecordRules.SanitizeCompressedMemoryBlocks(list) ?? new List<CompressedMemoryBlock>(), (key, ex) => Logger.Log("CompressedMemory", "[ERROR] Serialize memory blocks for " + key + ": " + ex.Message));
				Dictionary<string, string> dictionaryMemoryBlocks = CampaignSaveChunkHelper.FlattenStringDictionary(_state.BlockStorage, "_af_compressedMemoryBlocks_v1", "CompressedMemory");
				dataStore.SyncData("_af_compressedMemoryBlocks_v1", ref dictionaryMemoryBlocks);
				try
				{
					DailyQueueJson = JsonConvert.SerializeObject(MemoryRecordRules.SanitizeMemorySummaryQueue(_state.DailyQueue));
				}
				catch (Exception ex)
				{
					DailyQueueJson = "[]";
					Logger.Log("CompressedMemory", "[ERROR] Serialize memory summary queue failed: " + ex.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_af_memorySummaryQueue_v1", DailyQueueJson ?? "[]", "CompressedMemory");
				_state.OverviewStorage.Clear();
				foreach (KeyValuePair<string, MemoryOverviewState> overviewStateEntry in _state.Overviews)
				{
					if (!string.IsNullOrWhiteSpace(overviewStateEntry.Key) && overviewStateEntry.Value != null && (!string.IsNullOrWhiteSpace(overviewStateEntry.Value.Summary) || !string.IsNullOrWhiteSpace(overviewStateEntry.Value.LastError)))
					{
						try
						{
							MemoryOverviewState state = MemoryRecordRules.SanitizeMemoryOverviewState(overviewStateEntry.Value);
							if (state != null && !string.IsNullOrWhiteSpace(state.HeroId) && (!string.IsNullOrWhiteSpace(state.Summary) || !string.IsNullOrWhiteSpace(state.LastError)))
							{
								_state.OverviewStorage[state.HeroId] = JsonConvert.SerializeObject(state);
							}
						}
						catch (Exception ex)
						{
							Logger.Log("MemoryOverview", "[ERROR] Serialize memory overview for " + overviewStateEntry.Key + ": " + ex.Message);
						}
					}
				}
				Dictionary<string, string> dictionaryMemoryOverviewStates = CampaignSaveChunkHelper.FlattenStringDictionary(_state.OverviewStorage, "_af_memoryOverviewStates_v1", "MemoryOverview");
				dataStore.SyncData("_af_memoryOverviewStates_v1", ref dictionaryMemoryOverviewStates);
				try
				{
					OverviewQueueJson = JsonConvert.SerializeObject(MemoryRecordRules.SanitizeMemoryOverviewQueue(_state.OverviewQueue));
				}
				catch (Exception ex)
				{
					OverviewQueueJson = "[]";
					Logger.Log("MemoryOverview", "[ERROR] Serialize memory overview queue failed: " + ex.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_af_memoryOverviewQueue_v1", OverviewQueueJson ?? "[]", "MemoryOverview");
				_state.MajorStorage.Clear();
				foreach (KeyValuePair<string, MajorActionSummaryState> majorSummary in _state.MajorSummaries)
				{
					if (!string.IsNullOrWhiteSpace(majorSummary.Key) && majorSummary.Value != null && (!string.IsNullOrWhiteSpace(majorSummary.Value.Summary) || !string.IsNullOrWhiteSpace(majorSummary.Value.LastError)))
					{
						try
						{
							MajorActionSummaryState state = MemoryRecordRules.SanitizeMajorActionSummaryState(majorSummary.Value);
							if (state != null && !string.IsNullOrWhiteSpace(state.HeroId) && (!string.IsNullOrWhiteSpace(state.Summary) || !string.IsNullOrWhiteSpace(state.LastError)))
							{
								_state.MajorStorage[state.HeroId] = JsonConvert.SerializeObject(state);
							}
						}
						catch (Exception ex)
						{
							Logger.Log("NpcMajorSummary", "[ERROR] Serialize major action summary for " + majorSummary.Key + ": " + ex.Message);
						}
					}
				}
				Dictionary<string, string> dictionaryMajorActionSummaries = CampaignSaveChunkHelper.FlattenStringDictionary(_state.MajorStorage, "_af_npcMajorActionSummaries_v1", "NpcMajorSummary");
				dataStore.SyncData("_af_npcMajorActionSummaries_v1", ref dictionaryMajorActionSummaries);
				try
				{
					MajorQueueJson = JsonConvert.SerializeObject(MemoryRecordRules.SanitizeMajorActionSummaryQueue(_state.MajorQueue));
				}
				catch (Exception ex)
				{
					MajorQueueJson = "[]";
					Logger.Log("NpcMajorSummary", "[ERROR] Serialize major action summary queue failed: " + ex.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_af_npcMajorActionSummaryQueue_v1", MajorQueueJson ?? "[]", "NpcMajorSummary");
    }

    internal void Load(IDataStore dataStore, Action historyRestored, Action dailyRestored)
    {
			_state.History.Clear();
			_state.HistoryStorage.Clear();
			Dictionary<string, string> dictionary8 = new Dictionary<string, string>();
			dataStore.SyncData("_dialogueHistory_v2", ref dictionary8);
			_state.HistoryStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary8, "DialogueHistory");
			OwnerJsonStorageCodec.Deserialize(_state.HistoryStorage, _state.History, null, null, skipWhitespaceKeys: false, (key, ex) => Logger.Log("DialogueHistory", "[ERROR] Deserialize history for " + key + ": " + ex.Message));
            historyRestored();
			_state.Drafts.Clear();
			_state.DraftStorage.Clear();
			Dictionary<string, string> dictionaryMemoryDraftsLoad = new Dictionary<string, string>();
			dataStore.SyncData("_af_dailyMemoryDrafts_v1", ref dictionaryMemoryDraftsLoad);
			_state.DraftStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionaryMemoryDraftsLoad, "CompressedMemory");
			OwnerJsonStorageCodec.Deserialize(_state.DraftStorage, _state.Drafts, MemoryRecordRules.NormalizeMemoryHeroId, MemoryRecordRules.SanitizeDailyMemoryDrafts, skipWhitespaceKeys: true, (key, ex) => Logger.Log("CompressedMemory", "[ERROR] Deserialize daily memory drafts for " + key + ": " + ex.Message));
            dailyRestored();
			_state.Blocks.Clear();
			_state.BlockStorage.Clear();
			Dictionary<string, string> dictionaryMemoryBlocksLoad = new Dictionary<string, string>();
			dataStore.SyncData("_af_compressedMemoryBlocks_v1", ref dictionaryMemoryBlocksLoad);
			_state.BlockStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionaryMemoryBlocksLoad, "CompressedMemory");
			if (_state.BlockStorage != null)
			{
				foreach (KeyValuePair<string, string> memoryBlockEntry in _state.BlockStorage)
				{
					if (string.IsNullOrWhiteSpace(memoryBlockEntry.Key) || string.IsNullOrWhiteSpace(memoryBlockEntry.Value))
					{
						continue;
					}
					try
					{
						List<CompressedMemoryBlock> listMemoryBlocks = JsonConvert.DeserializeObject<List<CompressedMemoryBlock>>(memoryBlockEntry.Value) ?? new List<CompressedMemoryBlock>();
						listMemoryBlocks = MemoryRecordRules.SanitizeCompressedMemoryBlocks(listMemoryBlocks);
						if (listMemoryBlocks.Count > 0)
						{
							_state.Blocks[MemoryRecordRules.NormalizeMemoryHeroId(memoryBlockEntry.Key)] = listMemoryBlocks;
						}
					}
					catch (Exception ex)
					{
						Logger.Log("CompressedMemory", "[ERROR] Deserialize memory blocks for " + memoryBlockEntry.Key + ": " + ex.Message);
					}
				}
			}
			DailyQueueJson = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_af_memorySummaryQueue_v1", "CompressedMemory") ?? "";
			try
			{
				_state.DailyQueue = MemoryRecordRules.SanitizeMemorySummaryQueue(JsonConvert.DeserializeObject<List<MemorySummaryJob>>(DailyQueueJson) ?? new List<MemorySummaryJob>());
			}
			catch (Exception ex)
			{
				Logger.Log("CompressedMemory", "[ERROR] Deserialize memory summary queue failed: " + ex.Message);
				_state.DailyQueue = new List<MemorySummaryJob>();
			}
			_state.Overviews.Clear();
			_state.OverviewStorage.Clear();
			Dictionary<string, string> dictionaryMemoryOverviewStatesLoad = new Dictionary<string, string>();
			dataStore.SyncData("_af_memoryOverviewStates_v1", ref dictionaryMemoryOverviewStatesLoad);
			_state.OverviewStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionaryMemoryOverviewStatesLoad, "MemoryOverview");
			if (_state.OverviewStorage != null)
			{
				foreach (KeyValuePair<string, string> overviewEntry in _state.OverviewStorage)
				{
					if (string.IsNullOrWhiteSpace(overviewEntry.Key) || string.IsNullOrWhiteSpace(overviewEntry.Value))
					{
						continue;
					}
					try
					{
						MemoryOverviewState state = MemoryRecordRules.SanitizeMemoryOverviewState(JsonConvert.DeserializeObject<MemoryOverviewState>(overviewEntry.Value));
						if (state != null && !string.IsNullOrWhiteSpace(state.HeroId) && (!string.IsNullOrWhiteSpace(state.Summary) || !string.IsNullOrWhiteSpace(state.LastError)))
						{
							_state.Overviews[state.HeroId] = state;
						}
					}
					catch (Exception ex)
					{
						Logger.Log("MemoryOverview", "[ERROR] Deserialize memory overview for " + overviewEntry.Key + ": " + ex.Message);
					}
				}
			}
			OverviewQueueJson = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_af_memoryOverviewQueue_v1", "MemoryOverview") ?? "";
			try
			{
				_state.OverviewQueue = MemoryRecordRules.SanitizeMemoryOverviewQueue(JsonConvert.DeserializeObject<List<MemoryOverviewJob>>(OverviewQueueJson) ?? new List<MemoryOverviewJob>());
			}
			catch (Exception ex)
			{
				Logger.Log("MemoryOverview", "[ERROR] Deserialize memory overview queue failed: " + ex.Message);
				_state.OverviewQueue = new List<MemoryOverviewJob>();
			}
			_state.MajorSummaries.Clear();
			_state.MajorStorage.Clear();
			Dictionary<string, string> dictionaryMajorActionSummariesLoad = new Dictionary<string, string>();
			dataStore.SyncData("_af_npcMajorActionSummaries_v1", ref dictionaryMajorActionSummariesLoad);
			_state.MajorStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionaryMajorActionSummariesLoad, "NpcMajorSummary");
			if (_state.MajorStorage != null)
			{
				foreach (KeyValuePair<string, string> summaryEntry in _state.MajorStorage)
				{
					if (string.IsNullOrWhiteSpace(summaryEntry.Key) || string.IsNullOrWhiteSpace(summaryEntry.Value))
					{
						continue;
					}
					try
					{
						MajorActionSummaryState state = MemoryRecordRules.SanitizeMajorActionSummaryState(JsonConvert.DeserializeObject<MajorActionSummaryState>(summaryEntry.Value));
						if (state != null && !string.IsNullOrWhiteSpace(state.HeroId) && (!string.IsNullOrWhiteSpace(state.Summary) || !string.IsNullOrWhiteSpace(state.LastError)))
						{
							_state.MajorSummaries[state.HeroId] = state;
						}
					}
					catch (Exception ex)
					{
						Logger.Log("NpcMajorSummary", "[ERROR] Deserialize major action summary for " + summaryEntry.Key + ": " + ex.Message);
					}
				}
			}
			MajorQueueJson = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_af_npcMajorActionSummaryQueue_v1", "NpcMajorSummary") ?? "";
			try
			{
				_state.MajorQueue = MemoryRecordRules.SanitizeMajorActionSummaryQueue(JsonConvert.DeserializeObject<List<MajorActionSummaryJob>>(MajorQueueJson) ?? new List<MajorActionSummaryJob>());
			}
			catch (Exception ex)
			{
				Logger.Log("NpcMajorSummary", "[ERROR] Deserialize major action summary queue failed: " + ex.Message);
				_state.MajorQueue = new List<MajorActionSummaryJob>();
			}
    }
}
