using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Adapters;

internal sealed class BannerlordWorldDiplomacyPersistenceAdapter
{
    internal const string SaveKey = "_af_world_diplomacy_v1";
    internal const string Source = "WorldDiplomacy";
    internal const string QuarantineKey = "_af_world_diplomacy_quarantine_v1";
    private CampaignSaveChunkHelper.StrictStringRead _rejected;
    private string _reason;
    internal bool IsHealthy { get; private set; } = true;
    internal bool HasCompleteRejectedEvidence => !IsHealthy && _rejected?.EvidenceComplete == true;
    internal void ResetForNewGame() { IsHealthy = true; _rejected = null; _reason = null; }

    // Only primitive records from this exact namespace are admitted to the
    // evidence envelope. Runtime state and game objects are never serialized.
    private sealed class Evidence
    {
        public int Version = 1;
        public string Reason, RecordsSha256;
        public List<Record> Records = new List<Record>();
    }
    private sealed class Record
    {
        public string Key, Kind, Text;
        public int Number;
    }

    public void Save(IDataStore dataStore, WorldDiplomacyStorage storage)
    {
        if (!IsHealthy)
        {
            if (_rejected?.EvidenceComplete != true)
                throw new InvalidOperationException("外交存档读取失败且原始证据不完整，已拒绝保存以避免永久丢失数据。");
            var evidence = new Evidence { Reason = _reason };
            foreach (var pair in _rejected.Records.OrderBy(x => x.Key, StringComparer.Ordinal))
                evidence.Records.Add(new Record { Key = pair.Key, Kind = pair.Value == null ? "null" : pair.Value is int ? "int" : "string",
                    Number = pair.Value is int number ? number : 0, Text = pair.Value as string });
            evidence.RecordsSha256 = Digest(evidence.Records);
            CampaignSaveChunkHelper.SaveChunkedStringStrict(dataStore, QuarantineKey, JsonConvert.SerializeObject(evidence));
            CampaignSaveChunkHelper.ReplaySafeRawRecords(dataStore, _rejected.Records, SaveKey);
            return;
        }
        string json = JsonConvert.SerializeObject(storage);
        CampaignSaveChunkHelper.SaveChunkedStringStrict(dataStore, SaveKey, json);
    }

    public WorldDiplomacyStorage Load(IDataStore dataStore, out string error)
    {
        ResetForNewGame();
        try
        {
            var quarantine = CampaignSaveChunkHelper.LoadChunkedStringStrict(dataStore, QuarantineKey);
            if (quarantine.Status != CampaignSaveChunkHelper.StrictReadStatus.Absent)
            {
                if (quarantine.Status != CampaignSaveChunkHelper.StrictReadStatus.Complete)
                    throw new InvalidOperationException("quarantine_evidence_unreadable");
                var evidence = ParseObject(quarantine.Text).ToObject<Evidence>();
                if (evidence?.Version != 1 || evidence.Records == null || evidence.Records.Count == 0 ||
                    !string.Equals(evidence.RecordsSha256, Digest(evidence.Records), StringComparison.Ordinal))
                    throw new InvalidOperationException("quarantine_evidence_invalid");
                var raw = new CampaignSaveChunkHelper.StrictStringRead { EvidenceComplete = true };
                foreach (var record in evidence.Records)
                {
                    if (record == null || string.IsNullOrEmpty(record.Key) ||
                        !(record.Key == SaveKey || record.Key.StartsWith(SaveKey + "__af_chunk_", StringComparison.Ordinal)))
                        throw new InvalidOperationException("quarantine_record_key_invalid");
                    object value;
                    if (record.Kind == "null") value = null;
                    else if (record.Kind == "int") value = record.Number;
                    else if (record.Kind == "string" && record.Text != null) value = record.Text;
                    else throw new InvalidOperationException("quarantine_record_type_invalid");
                    raw.Records.Add(record.Key, value);
                }
                _rejected = raw;
                return Reject(evidence.Reason ?? "saved_quarantine", out error);
            }
            _rejected = CampaignSaveChunkHelper.LoadChunkedStringStrict(dataStore, SaveKey);
            if (_rejected.Status == CampaignSaveChunkHelper.StrictReadStatus.Absent)
            { error = ""; _rejected = null; return new WorldDiplomacyStorage(); }
            if (_rejected.Status != CampaignSaveChunkHelper.StrictReadStatus.Complete)
                return Reject(_rejected.Error ?? "invalid_save_representation", out error);
            WorldDiplomacyStorage loaded = ParseObject(_rejected.Text).ToObject<WorldDiplomacyStorage>();
            if (loaded == null) throw new InvalidOperationException("null_diplomacy_storage");
            error = "";
            _rejected = null;
            return loaded;
        }
        catch (Exception ex)
        {
            return Reject("load_failed:" + ex.GetType().Name, out error);
        }
    }
    private WorldDiplomacyStorage Reject(string reason, out string error)
    { IsHealthy = false; _reason = reason; error = reason; return new WorldDiplomacyStorage(); }
    private static JObject ParseObject(string text)
    {
        using var reader = new JsonTextReader(new StringReader(text ?? ""));
        var token = JToken.ReadFrom(reader);
        if (!(token is JObject value) || reader.Read()) throw new JsonSerializationException("invalid_diplomacy_json_root");
        return value;
    }
    private static string Digest(List<Record> records)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(records)))).Replace("-", "");
    }
}
