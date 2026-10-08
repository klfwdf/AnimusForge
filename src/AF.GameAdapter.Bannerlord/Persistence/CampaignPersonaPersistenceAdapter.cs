using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;

// Save-storage/reset adapter; operates on original authoritative stores, with no host instance.
internal sealed class CampaignPersonaPersistenceAdapter
{
    private readonly PersonaProfileStateOwner _owner;
    internal Dictionary<string, string> Storage = new Dictionary<string, string>();
    internal CampaignPersonaPersistenceAdapter(PersonaProfileStateOwner owner) { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); }
    internal void Save(IDataStore store, Func<string, MyBehavior.NpcPersonaProfile, bool> prepare) => Save(store, _owner.Profiles, Storage, prepare);
    internal void Load(IDataStore store) => Load(store, _owner.Profiles, ref Storage);

    internal static void Save(IDataStore dataStore, Dictionary<string, MyBehavior.NpcPersonaProfile> profiles, Dictionary<string, string> storage, Func<string, MyBehavior.NpcPersonaProfile, bool> prepare)
    {
				storage.Clear();
				foreach (KeyValuePair<string, MyBehavior.NpcPersonaProfile> npcPersonaProfile2 in profiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile2.Key) && prepare(npcPersonaProfile2.Key, npcPersonaProfile2.Value))
					{
						try
						{
							string value4 = JsonConvert.SerializeObject(npcPersonaProfile2.Value);
							storage[npcPersonaProfile2.Key] = value4;
						}
						catch (Exception ex4)
						{
							Logger.Log("NpcPersona", "[ERROR] Serialize profile for " + npcPersonaProfile2.Key + ": " + ex4.Message);
						}
					}
				}
				Dictionary<string, string> dictionary5 = CampaignSaveChunkHelper.FlattenStringDictionary(storage, "_npcPersonaProfiles_v1", "NpcPersona");
				dataStore.SyncData("_npcPersonaProfiles_v1", ref dictionary5);
    }

    internal static void Load(IDataStore dataStore, Dictionary<string, MyBehavior.NpcPersonaProfile> profiles, ref Dictionary<string, string> storage)
    {
			profiles.Clear();
			storage.Clear();
			Dictionary<string, string> dictionary11 = new Dictionary<string, string>();
			dataStore.SyncData("_npcPersonaProfiles_v1", ref dictionary11);
			storage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary11, "NpcPersona");
			if (storage != null)
			{
				foreach (KeyValuePair<string, string> item5 in storage)
				{
					if (string.IsNullOrEmpty(item5.Key) || string.IsNullOrEmpty(item5.Value))
					{
						continue;
					}
					try
					{
						MyBehavior.NpcPersonaProfile npcPersonaProfile = JsonConvert.DeserializeObject<MyBehavior.NpcPersonaProfile>(item5.Value);
						if (npcPersonaProfile != null)
						{
							profiles[item5.Key] = npcPersonaProfile;
						}
					}
					catch (Exception ex6)
					{
						Logger.Log("NpcPersona", "[ERROR] Deserialize profile for " + item5.Key + ": " + ex6.Message);
					}
				}
			}
    }

internal void ResetForCurrentSave() { _owner.ResetForCurrentSave(); Storage = new Dictionary<string,string>(); }
}
