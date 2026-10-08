using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;

// Save-storage/reset adapter; operates on original authoritative stores, with no host instance.
internal sealed class CampaignShownRecordPersistenceAdapter
{
    private readonly ShownResourceRecordOwner _owner;
    internal Dictionary<string, string> Storage = new Dictionary<string, string>();
    internal CampaignShownRecordPersistenceAdapter(ShownResourceRecordOwner owner) { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); }
    internal void Save(IDataStore store) => Save(store, _owner.Records, Storage);
    internal void Load(IDataStore store) => Load(store, _owner.Records, ref Storage);

    internal static string NormalizeKey(string targetKey) => (targetKey ?? "").Trim().ToLowerInvariant();
    internal static void Save(IDataStore dataStore, Dictionary<string, MyBehavior.HeroShownRecord> records, Dictionary<string, string> storage)
    {
				storage.Clear();
				foreach (KeyValuePair<string, MyBehavior.HeroShownRecord> shownRecord in records)
				{
					if (string.IsNullOrWhiteSpace(shownRecord.Key) || shownRecord.Value == null)
					{
						continue;
					}
					bool flag = Math.Max(0, shownRecord.Value.ShownGold) > 0;
					if (!flag && shownRecord.Value.ShownItems != null)
					{
						foreach (KeyValuePair<string, int> shownItem in shownRecord.Value.ShownItems)
						{
							if (!string.IsNullOrWhiteSpace(shownItem.Key) && shownItem.Value > 0)
							{
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						continue;
					}
					try
					{
						storage[shownRecord.Key] = JsonConvert.SerializeObject(shownRecord.Value);
					}
					catch (Exception ex)
					{
						Logger.Log("TradeShown", "[ERROR] Serialize shown record for " + shownRecord.Key + ": " + ex.Message);
					}
				}
				Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(storage, "_shownRecords_v1", "TradeShown");
				dataStore.SyncData("_shownRecords_v1", ref dictionary);
    }

    internal static void Load(IDataStore dataStore, Dictionary<string, MyBehavior.HeroShownRecord> records, ref Dictionary<string, string> storage)
    {
			records.Clear();
			storage.Clear();
			Dictionary<string, string> dictionary7 = new Dictionary<string, string>();
			dataStore.SyncData("_shownRecords_v1", ref dictionary7);
			storage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary7, "TradeShown");
			if (storage != null)
			{
				foreach (KeyValuePair<string, string> shownRecord2 in storage)
				{
					if (string.IsNullOrWhiteSpace(shownRecord2.Key) || string.IsNullOrWhiteSpace(shownRecord2.Value))
					{
						continue;
					}
					try
					{
						MyBehavior.HeroShownRecord heroShownRecord = JsonConvert.DeserializeObject<MyBehavior.HeroShownRecord>(shownRecord2.Value);
						if (heroShownRecord != null)
						{
							if (heroShownRecord.ShownGold < 0)
							{
								heroShownRecord.ShownGold = 0;
							}
							if (heroShownRecord.ShownItems == null)
							{
								heroShownRecord.ShownItems = new Dictionary<string, int>();
							}
							else
							{
								heroShownRecord.ShownItems = heroShownRecord.ShownItems.Where((KeyValuePair<string, int> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0).ToDictionary((KeyValuePair<string, int> x) => x.Key, (KeyValuePair<string, int> x) => x.Value);
							}
							records[NormalizeKey(shownRecord2.Key)] = heroShownRecord;
						}
					}
					catch (Exception ex2)
					{
						Logger.Log("TradeShown", "[ERROR] Deserialize shown record for " + shownRecord2.Key + ": " + ex2.Message);
					}
				}
			}
    }

internal void ResetForCurrentSave() { _owner.ResetForCurrentSave(); Storage = new Dictionary<string,string>(); }
}
