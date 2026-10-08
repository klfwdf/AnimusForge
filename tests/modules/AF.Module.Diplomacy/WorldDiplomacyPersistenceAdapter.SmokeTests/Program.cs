using AnimusForge;
using AnimusForge.Refactor.Adapters;
using TaleWorlds.CampaignSystem;
using System.Text;

internal sealed class NativeRecordsLeaf : IDataStore
{
    private readonly Dictionary<string, object> _records = new(StringComparer.Ordinal);
    internal bool Saving;
    public bool IsSaving => Saving;
    public bool IsLoading => !Saving;
    public bool SyncData<T>(string key, ref T data)
    {
        if (Saving) { _records.Add(key, data); return true; }
        if (!_records.TryGetValue(key, out var value)) return false;
        data = (T)value; return true;
    }
    internal NativeRecordsLeaf Set(string key, object value) { _records[key] = value; return this; }
    internal IEnumerable<KeyValuePair<string, object>> Records => _records;
}

internal static class Program
{
    private const string Key = "_af_world_diplomacy_v1";
    private static int _checks;
    private static void Check(bool value, string label)
    {
        _checks++;
        if (!value) throw new InvalidOperationException(label);
    }
    private static void Main()
    {
        var adapter = new BannerlordWorldDiplomacyPersistenceAdapter();
        adapter.Load(new NativeRecordsLeaf(), out var error);
        Check(adapter.IsHealthy && error == "", "genuinely absent namespace is healthy");
        var saved = new NativeRecordsLeaf { Saving = true };
        adapter.Save(saved, new WorldDiplomacyStorage { Marker = "healthy", Documents = new() { new string('文', 12000) } });
        saved.Saving = false;
        var result = adapter.Load(saved, out error);
        Check(adapter.IsHealthy && error == "" && result.Marker == "healthy" && result.Documents[0].Length == 12000,
            "real helper and JSON roundtrip multiple chunks");
        var rejectedInputs = new[] {
            new NativeRecordsLeaf().Set(Key, "{bad"),
            new NativeRecordsLeaf().Set(Key + "__af_chunk_count", 1).Set(Key, "{}"),
            new NativeRecordsLeaf().Set(Key + "__af_chunk_0", "orphan")
        };
        foreach (var input in rejectedInputs)
        {
            var owner = new BannerlordWorldDiplomacyPersistenceAdapter();
            owner.Load(input, out error);
            Check(!owner.IsHealthy && owner.HasCompleteRejectedEvidence && error.Length > 0, "bad domain quarantines");
            var evidence = new NativeRecordsLeaf { Saving = true };
            owner.Save(evidence, new WorldDiplomacyStorage { Marker = "must not overwrite" });
            Check(evidence.Records.All(x => x.Value is not string text || Encoding.UTF8.GetByteCount(text) <= 12000), "all saved evidence bounded");
            evidence.Saving = false;
            var next = new BannerlordWorldDiplomacyPersistenceAdapter();
            next.Load(evidence, out error);
            Check(!next.IsHealthy && next.HasCompleteRejectedEvidence && error.Length > 0, "new-store reload keeps quarantine");
            next.ResetForNewGame();
            Check(next.IsHealthy, "new campaign resets only its owner");
        }
        Console.WriteLine($"PASS persistenceAdapter checks={_checks}; production helper/adapter + SDK Newtonsoft.Json; native-record/DTO/logger leaves only");
    }
}
