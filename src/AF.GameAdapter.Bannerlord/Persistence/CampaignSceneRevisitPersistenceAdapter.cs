using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
internal sealed class CampaignSceneRevisitPersistenceAdapter
{
 private readonly SceneRevisitStateOwner _state;
 internal Dictionary<string,string> Storage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 internal CampaignSceneRevisitPersistenceAdapter(SceneRevisitStateOwner state) { _state=state??throw new ArgumentNullException(nameof(state)); }
	internal void Sync(IDataStore dataStore)
	{
		try
		{
			if (dataStore == null)
			{
				return;
			}
			// Keep revisit markers across saves so "距离上次见面 X 天" survives save/load and scene re-entry.
			Dictionary<string, string> dictionary = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(Storage, "_sceneHeroRevisitDays_v1", "SceneHeroRevisit") : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (dataStore.IsSaving)
			{
				Storage.Clear();
				lock (_state.Gate)
				{
					foreach (KeyValuePair<string, int> sceneHeroRevisitDay in _state.Days)
					{
						if (!string.IsNullOrWhiteSpace(sceneHeroRevisitDay.Key) && sceneHeroRevisitDay.Value >= 0)
						{
							Storage[sceneHeroRevisitDay.Key] = sceneHeroRevisitDay.Value.ToString();
						}
					}
				}
				dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(Storage, "_sceneHeroRevisitDays_v1", "SceneHeroRevisit");
				dataStore.SyncData("_sceneHeroRevisitDays_v1", ref dictionary);
				return;
			}
			dataStore.SyncData("_sceneHeroRevisitDays_v1", ref dictionary);
			Storage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary, "SceneHeroRevisit");
			lock (_state.Gate)
			{
				_state.Days.Clear();
				if (Storage == null)
				{
					return;
				}
				foreach (KeyValuePair<string, string> item in Storage)
				{
					if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value) && int.TryParse(item.Value.Trim(), out var result) && result >= 0)
					{
						_state.Days[item.Key] = result;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] SyncData failed: " + ex.Message);
		}
	}
}
