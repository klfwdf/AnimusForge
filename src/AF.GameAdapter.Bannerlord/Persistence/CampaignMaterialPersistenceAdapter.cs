using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using EventSourceMaterialEntry = AnimusForge.MyBehavior.EventSourceMaterialEntry;
namespace AnimusForge;

// IO only: original record owner retains its list, index and rebuild lifecycle.
internal sealed class CampaignMaterialPersistenceAdapter
{
    private readonly CampaignMaterialRecordOwner _owner;
    internal string JsonStorage = "";
    internal CampaignMaterialPersistenceAdapter(CampaignMaterialRecordOwner owner) { _owner = owner; }
    internal void Save(IDataStore store) => Save(store, _owner, ref JsonStorage);
    internal void Load(IDataStore store) => Load(store, _owner, ref JsonStorage);

    internal static void Save(IDataStore dataStore, CampaignMaterialRecordOwner owner, ref string json)
    {
				try
				{
					json = JsonConvert.SerializeObject(CampaignMaterialRecordOwner.SanitizeEventSourceMaterials(owner.Materials));
				}
				catch (Exception ex6)
				{
					json = "[]";
					Logger.Log("EventMaterial", "[ERROR] Serialize event source materials failed: " + ex6.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventSourceMaterials_v1", json ?? "[]", "EventMaterial");
				json = "";
    }

    internal static void Load(IDataStore dataStore, CampaignMaterialRecordOwner owner, ref string json)
    {
			owner.Materials.Clear();
			json = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_eventSourceMaterials_v1", "EventMaterial") ?? "";
			if (!string.IsNullOrWhiteSpace(json))
			{
				try
				{
					List<EventSourceMaterialEntry> list5 = JsonConvert.DeserializeObject<List<EventSourceMaterialEntry>>(json) ?? new List<EventSourceMaterialEntry>();
					owner.Materials = CampaignMaterialRecordOwner.SanitizeEventSourceMaterials(list5);
				}
				catch (Exception ex8)
				{
					Logger.Log("EventMaterial", "[ERROR] Deserialize event source materials failed: " + ex8.Message);
					owner.Materials = new List<EventSourceMaterialEntry>();
				}
			}
			json = "";
    }

}
