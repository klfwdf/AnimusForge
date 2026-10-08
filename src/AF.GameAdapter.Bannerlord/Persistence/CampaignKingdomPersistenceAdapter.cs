using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
namespace AnimusForge;

// IO boundary only: original domain owners retain all authoritative containers.
internal sealed class CampaignKingdomPersistenceAdapter
{
    private readonly KingdomStabilityOwner _state;
    private readonly RebelKingdomIdentityOwner _rebels;
    internal Dictionary<string,string> StabilityStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string,string> RelationStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string,string> WeeklyStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string,string> RebelStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    internal CampaignKingdomPersistenceAdapter(KingdomStabilityOwner state, RebelKingdomIdentityOwner rebels)
    { _state=state; _rebels=rebels; }
    internal void Save(IDataStore dataStore) => Save(dataStore, _state.Values, ref StabilityStorage,
        _state.RelationOffsets, ref RelationStorage, _state.WeeklyDeltas, ref WeeklyStorage, _rebels, ref RebelStorage);
    internal void Load(IDataStore dataStore) => Load(dataStore, _state.Values, ref StabilityStorage,
        _state.RelationOffsets, ref RelationStorage, _state.WeeklyDeltas, ref WeeklyStorage, _rebels, ref RebelStorage);
    internal static void Save(IDataStore dataStore, Dictionary<string, int> stability, ref Dictionary<string, string> stabilityStorage, Dictionary<string, int> relationOffsets, ref Dictionary<string, string> relationStorage, Dictionary<string, int> weeklyDeltas, ref Dictionary<string, string> weeklyStorage, RebelKingdomIdentityOwner rebelIdentity, ref Dictionary<string, string> rebelStorage)
    {
				stabilityStorage.Clear();
				foreach (KeyValuePair<string, int> kingdomStabilityValue in stability)
				{
					string text3 = (kingdomStabilityValue.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						stabilityStorage[text3] = KingdomStabilityPolicy.ClampKingdomStabilityValue(kingdomStabilityValue.Value).ToString();
					}
				}
				Dictionary<string, string> dictionary13 = CampaignSaveChunkHelper.FlattenStringDictionary(stabilityStorage, "_kingdomStability_v1", "KingdomStability");
				dataStore.SyncData("_kingdomStability_v1", ref dictionary13);
				relationStorage.Clear();
				foreach (KeyValuePair<string, int> kingdomStabilityRelationAppliedOffset in relationOffsets)
				{
					string text4 = (kingdomStabilityRelationAppliedOffset.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						relationStorage[text4] = kingdomStabilityRelationAppliedOffset.Value.ToString();
					}
				}
				Dictionary<string, string> dictionary14b = CampaignSaveChunkHelper.FlattenStringDictionary(relationStorage, "_kingdomStabilityRelationOffsets_v1", "KingdomStabilityRelation");
				dataStore.SyncData("_kingdomStabilityRelationOffsets_v1", ref dictionary14b);
				weeklyStorage.Clear();
				foreach (KeyValuePair<string, int> weeklyReportAppliedStabilityDelta in weeklyDeltas)
				{
					string text5 = (weeklyReportAppliedStabilityDelta.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text5))
					{
						weeklyStorage[text5] = weeklyReportAppliedStabilityDelta.Value.ToString();
					}
				}
				Dictionary<string, string> dictionary14c = CampaignSaveChunkHelper.FlattenStringDictionary(weeklyStorage, "_weeklyReportAppliedStabilityDeltas_v1", "WeeklyReportStability");
				dataStore.SyncData("_weeklyReportAppliedStabilityDeltas_v1", ref dictionary14c);
				rebelStorage.Clear();
				foreach (string rebelKingdomId in rebelIdentity.Snapshot())
				{
					string text6 = (rebelKingdomId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6))
					{
						rebelStorage[text6] = "1";
					}
				}
				Dictionary<string, string> dictionary14d = CampaignSaveChunkHelper.FlattenStringDictionary(rebelStorage, "_modCreatedRebelKingdomIds_v1", "ModCreatedRebelKingdom");
				dataStore.SyncData("_modCreatedRebelKingdomIds_v1", ref dictionary14d);
    }

    internal static void Load(IDataStore dataStore, Dictionary<string, int> stability, ref Dictionary<string, string> stabilityStorage, Dictionary<string, int> relationOffsets, ref Dictionary<string, string> relationStorage, Dictionary<string, int> weeklyDeltas, ref Dictionary<string, string> weeklyStorage, RebelKingdomIdentityOwner rebelIdentity, ref Dictionary<string, string> rebelStorage)
    {
			stability.Clear();
			stabilityStorage.Clear();
			Dictionary<string, string> dictionary14 = new Dictionary<string, string>();
			dataStore.SyncData("_kingdomStability_v1", ref dictionary14);
			stabilityStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary14, "KingdomStability");
			if (stabilityStorage != null)
			{
				foreach (KeyValuePair<string, string> item7 in stabilityStorage)
				{
					string text5 = (item7.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text5) && int.TryParse((item7.Value ?? "").Trim(), out var result))
					{
						stability[text5] = KingdomStabilityPolicy.ClampKingdomStabilityValue(result);
					}
				}
			}
			relationOffsets.Clear();
			relationStorage.Clear();
			Dictionary<string, string> dictionary15 = new Dictionary<string, string>();
			dataStore.SyncData("_kingdomStabilityRelationOffsets_v1", ref dictionary15);
			relationStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary15, "KingdomStabilityRelation");
			if (relationStorage != null)
			{
				foreach (KeyValuePair<string, string> item8 in relationStorage)
				{
					string text6 = (item8.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6) && int.TryParse((item8.Value ?? "").Trim(), out var result2))
					{
						relationOffsets[text6] = MBMath.ClampInt(result2, -100, 100);
					}
				}
			}
			weeklyDeltas.Clear();
			weeklyStorage.Clear();
			Dictionary<string, string> dictionary16 = new Dictionary<string, string>();
			dataStore.SyncData("_weeklyReportAppliedStabilityDeltas_v1", ref dictionary16);
			weeklyStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary16, "WeeklyReportStability");
			if (weeklyStorage != null)
			{
				foreach (KeyValuePair<string, string> item9 in weeklyStorage)
				{
					string text7 = (item9.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text7) && int.TryParse((item9.Value ?? "").Trim(), out var result3))
					{
						weeklyDeltas[text7] = result3;
					}
				}
			}
			rebelIdentity.Clear();
			rebelStorage.Clear();
			Dictionary<string, string> dictionary17 = new Dictionary<string, string>();
			dataStore.SyncData("_modCreatedRebelKingdomIds_v1", ref dictionary17);
			rebelStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary17, "ModCreatedRebelKingdom");
			if (rebelStorage != null)
			{
				foreach (string key in rebelStorage.Keys)
				{
					string text8 = (key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text8))
					{
						rebelIdentity.Mark(text8);
					}
				}
			}
    }


internal void ResetForCurrentSave()
{
 _state.Values = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 StabilityStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 _state.RelationOffsets = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 RelationStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 _state.WeeklyDeltas = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 WeeklyStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 _rebels.Clear();
 RebelStorage = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
}
}
