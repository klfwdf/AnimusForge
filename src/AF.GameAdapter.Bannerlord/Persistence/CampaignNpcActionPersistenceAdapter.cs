using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;

// SyncData-only adapter; operates on original authoritative stores, with no host instance.
internal static class CampaignNpcActionPersistenceAdapter
{
    internal static void Save(IDataStore dataStore, MemoryBusinessStateOwner state, Func<List<NpcActionEntry>, bool, List<NpcActionEntry>> sanitize)
    {
				OwnerJsonStorageCodec.Serialize(state.MajorActions, state.MajorActionStorage, skipWhitespaceKeys: false, skipEmptyLists: true, list => sanitize(list, false), (key, ex) => Logger.Log("NpcAction", "[ERROR] Serialize major actions for " + key + ": " + ex.Message));
				Dictionary<string, string> dictionary3 = CampaignSaveChunkHelper.FlattenStringDictionary(state.MajorActionStorage, "_npcMajorActions_v1", "NpcAction");
				dataStore.SyncData("_npcMajorActions_v1", ref dictionary3);
				OwnerJsonStorageCodec.Serialize(state.RecentActions, state.RecentActionStorage, skipWhitespaceKeys: false, skipEmptyLists: true, null, (key, ex) => Logger.Log("NpcAction", "[ERROR] Serialize recent actions for " + key + ": " + ex.Message));
				Dictionary<string, string> dictionary4 = CampaignSaveChunkHelper.FlattenStringDictionary(state.RecentActionStorage, "_npcRecentActions_v1", "NpcAction");
				dataStore.SyncData("_npcRecentActions_v1", ref dictionary4);
    }

    internal static void Load(IDataStore dataStore, MemoryBusinessStateOwner state, Func<List<NpcActionEntry>, bool, List<NpcActionEntry>> sanitize)
    {
			state.MajorActions.Clear();
			state.MajorActionStorage.Clear();
			Dictionary<string, string> dictionary9 = new Dictionary<string, string>();
			dataStore.SyncData("_npcMajorActions_v1", ref dictionary9);
			state.MajorActionStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary9, "NpcAction");
			if (state.MajorActionStorage != null)
			{
				foreach (KeyValuePair<string, string> item3 in state.MajorActionStorage)
				{
					if (string.IsNullOrEmpty(item3.Key) || string.IsNullOrEmpty(item3.Value))
					{
						continue;
					}
					try
					{
						List<NpcActionEntry> list2 = JsonConvert.DeserializeObject<List<NpcActionEntry>>(item3.Value) ?? new List<NpcActionEntry>();
						list2 = sanitize(list2, false);
						if (list2.Count > 0)
						{
							state.MajorActions[item3.Key] = list2;
						}
					}
					catch (Exception ex4)
					{
						Logger.Log("NpcAction", "[ERROR] Deserialize major actions for " + item3.Key + ": " + ex4.Message);
					}
				}
			}
			state.RecentActions.Clear();
			state.RecentActionStorage.Clear();
			Dictionary<string, string> dictionary10 = new Dictionary<string, string>();
			dataStore.SyncData("_npcRecentActions_v1", ref dictionary10);
			state.RecentActionStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary10, "NpcAction");
			if (state.RecentActionStorage != null)
			{
				foreach (KeyValuePair<string, string> item4 in state.RecentActionStorage)
				{
					if (string.IsNullOrEmpty(item4.Key) || string.IsNullOrEmpty(item4.Value))
					{
						continue;
					}
					try
					{
						List<NpcActionEntry> list3 = JsonConvert.DeserializeObject<List<NpcActionEntry>>(item4.Value) ?? new List<NpcActionEntry>();
						list3 = sanitize(list3, true);
						if (list3.Count > 0)
						{
							state.RecentActions[item4.Key] = list3;
						}
					}
					catch (Exception ex5)
					{
						Logger.Log("NpcAction", "[ERROR] Deserialize recent actions for " + item4.Key + ": " + ex5.Message);
					}
				}
			}
    }
}
