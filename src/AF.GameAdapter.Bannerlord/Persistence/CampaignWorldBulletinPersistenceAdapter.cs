using System;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
internal static class CampaignWorldBulletinPersistenceAdapter
{
 private const string StorageKey = "_af_worldBulletin_v1";
	internal static void Sync(IDataStore dataStore, WorldBulletinStateOwner owner, Action resetTransient)
	{
		try
		{
			if (dataStore.IsSaving)
			{
                string json = owner.ExportJson();
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, StorageKey, json, "WorldBulletin");
				return;
			}
			owner.CorruptRaw = null;
			string loaded = CampaignSaveChunkHelper.LoadChunkedString(dataStore, StorageKey, "WorldBulletin") ?? "";
            owner.ImportJson(loaded);
			resetTransient();
		}
		catch (Exception ex)
		{
			// Save-path failures keep the in-memory state; only the load path may reset it.
			if (!dataStore.IsSaving)
			{
				owner.State = null;
				resetTransient();
			}
			Logger.Log("WorldBulletin", "[ERROR] SyncData isolated (saving=" + dataStore.IsSaving + "): " + ex.Message);
		}
	}
}
