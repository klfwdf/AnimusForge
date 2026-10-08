using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Save-only scratch; the PatienceOwner remains the only patience state/algorithm authority.
internal sealed class CampaignPatiencePersistenceAdapter
{
 private readonly PatienceOwner<MyBehavior.PatienceState> _owner;
 private readonly object _gate;
 internal Dictionary<string,string> Storage = new Dictionary<string,string>();
 internal CampaignPatiencePersistenceAdapter(PatienceOwner<MyBehavior.PatienceState> owner, object gate)
 { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); _gate = gate ?? throw new ArgumentNullException(nameof(gate)); }
	internal void Sync(IDataStore dataStore)
	{
		if (Storage == null)
		{
			Storage = new Dictionary<string, string>();
		}
		if (dataStore == null)
		{
			return;
		}
		try
		{
			if (dataStore.IsSaving)
			{
				lock (_gate)
				{
					Storage.Clear();
					foreach (KeyValuePair<string, MyBehavior.PatienceState> patienceState in _owner.SaveSnapshot())
					{
						if (!string.IsNullOrWhiteSpace(patienceState.Key) && patienceState.Value != null)
						{
							try
							{
								MyBehavior.PatienceStateSaveModel value = new MyBehavior.PatienceStateSaveModel
								{
									Value = patienceState.Value.Value,
									LastDay = patienceState.Value.LastDay,
									NoInterestRounds = patienceState.Value.NoInterestRounds,
									ExhaustedRefusalCount = patienceState.Value.ExhaustedRefusalCount
								};
								Storage[patienceState.Key] = JsonConvert.SerializeObject(value);
							}
							catch
							{
							}
						}
					}
				}
				Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(Storage, "_patienceStates_v1", "Patience");
				dataStore.SyncData("_patienceStates_v1", ref dictionary);
				return;
			}
			lock (_gate)
			{
				_owner.Clear();
				Storage.Clear();
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			dataStore.SyncData("_patienceStates_v1", ref dictionary2);
			Storage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary2, "Patience");
			if (Storage == null)
			{
				return;
			}
			lock (_gate)
			{
				var loaded = new Dictionary<string, MyBehavior.PatienceState>();
				foreach (KeyValuePair<string, string> item in Storage)
				{
					if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
					{
						continue;
					}
					try
					{
						MyBehavior.PatienceStateSaveModel patienceStateSaveModel = JsonConvert.DeserializeObject<MyBehavior.PatienceStateSaveModel>(item.Value);
						if (patienceStateSaveModel != null)
						{
							loaded[item.Key] = new MyBehavior.PatienceState
							{
								Value = patienceStateSaveModel.Value,
								LastDay = patienceStateSaveModel.LastDay,
								NoInterestRounds = patienceStateSaveModel.NoInterestRounds,
								ExhaustedRefusalCount = patienceStateSaveModel.ExhaustedRefusalCount
							};
						}
					}
					catch
					{
					}
				}
				_owner.Replace(loaded);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Patience", "[ERROR] SyncPatienceData failed: " + ex.Message);
			lock (_gate)
			{
				_owner.Clear();
				Storage = new Dictionary<string, string>();
			}
		}
	}

internal void ResetForCurrentSave()
{
 lock (_gate)
 {
  _owner.Clear();
  Storage = new Dictionary<string,string>();
 }
}
}
