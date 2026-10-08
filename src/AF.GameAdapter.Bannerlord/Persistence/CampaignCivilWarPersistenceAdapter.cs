using System;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;

// Serialized scratch only; the civil-war module remains the sole business authority.
internal sealed class CampaignCivilWarPersistenceAdapter
{
 private readonly Func<string> _save;
 private readonly Action<string> _load;
 internal string JsonStorage = "";
 internal CampaignCivilWarPersistenceAdapter(Func<string> save, Action<string> load)
 { _save=save; _load=load; }
 internal void Save(IDataStore dataStore)
 {
  JsonStorage = _save();
  CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_af_kingdom_civil_war_v2", JsonStorage, "KingdomCivilWar");
 }
 internal void Load(IDataStore dataStore)
 {
  JsonStorage = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_af_kingdom_civil_war_v2", "KingdomCivilWar");
  _load(JsonStorage);
 }
 internal void ResetForNewCampaign()
 {
  JsonStorage = "";
  _load("");
 }
}
