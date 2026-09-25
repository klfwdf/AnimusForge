using System;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Adapters;

internal sealed class BannerlordWorldDiplomacyPersistenceAdapter
{
    internal const string SaveKey = "_af_world_diplomacy_v1";
    internal const string Source = "WorldDiplomacy";

    public void Save(IDataStore dataStore, WorldDiplomacyStorage storage)
    {
        string json = JsonConvert.SerializeObject(storage);
        CampaignSaveChunkHelper.SaveChunkedString(dataStore, SaveKey, json, Source);
    }

    public WorldDiplomacyStorage Load(IDataStore dataStore, out string error)
    {
        try
        {
            string json = CampaignSaveChunkHelper.LoadChunkedString(dataStore, SaveKey, Source);
            error = "";
            return string.IsNullOrWhiteSpace(json)
                ? new WorldDiplomacyStorage()
                : JsonConvert.DeserializeObject<WorldDiplomacyStorage>(json) ?? new WorldDiplomacyStorage();
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return new WorldDiplomacyStorage();
        }
    }
}
