using AnimusForge;
using System.Collections;
using TaleWorlds.CampaignSystem;

static void AssertTrue(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}

static void AssertCorrupt(Action action, string label)
{
    try { action(); } catch (CampaignSaveChunkHelper.PersistenceIntegrityException) { return; }
    throw new Exception("Expected integrity rejection: " + label);
}

string ascii = new string('a', 100);
MemoryDataStore save = new MemoryDataStore(saving: true);
CampaignSaveChunkHelper.SaveChunkedString(save, "small", ascii);
AssertTrue(save.Values.ContainsKey("small__af_chunk_count"), "small string chunk count was not persisted");
AssertTrue(save.Values.ContainsKey("small"), "small string legacy inline key was not persisted");
MemoryDataStore load = save.ForLoading();
AssertTrue(CampaignSaveChunkHelper.LoadChunkedString(load, "small") == ascii, "small string round-trip failed");

string unicode = string.Concat(Enumerable.Repeat("汉字🙂é", 5000));
MemoryDataStore largeSave = new MemoryDataStore(saving: true);
CampaignSaveChunkHelper.SaveChunkedString(largeSave, "large", unicode);
int chunkCount = (int)largeSave.Values["large__af_chunk_count"];
AssertTrue(chunkCount > 1, "large UTF-8 string was not chunked");
AssertTrue((string)largeSave.Values["large"] == string.Empty, "large string retained an unsafe inline copy");
MemoryDataStore largeLoad = largeSave.ForLoading();
AssertTrue(CampaignSaveChunkHelper.LoadChunkedString(largeLoad, "large") == unicode, "large UTF-8 string round-trip failed");
foreach (object value in largeSave.Values.Values)
{
    if (value is string text && text.Length > 0)
    {
        AssertTrue(!text.Contains('\uFFFD'), "chunk contained a replacement character");
    }
}

MemoryDataStore missingChunk = largeSave.ForLoading();
missingChunk.Values.Remove("large__af_chunk_1");
AssertCorrupt(() => CampaignSaveChunkHelper.LoadChunkedString(missingChunk, "large"), "missing chunk");
MemoryDataStore corruptCount = largeSave.ForLoading();
corruptCount.Values["large__af_chunk_count"] = 262145;
AssertCorrupt(() => CampaignSaveChunkHelper.LoadChunkedString(corruptCount, "large"), "oversized chunk count");
MemoryDataStore inlineFallback = new MemoryDataStore(saving: false);
inlineFallback.Values["legacy"] = "legacy inline";
AssertTrue(CampaignSaveChunkHelper.LoadChunkedString(inlineFallback, "legacy") == "legacy inline", "legacy inline fallback failed");

Dictionary<string, string> source = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["small"] = "value",
    ["large"] = unicode,
    ["empty"] = string.Empty
};
Dictionary<string, string> flattened = CampaignSaveChunkHelper.FlattenStringDictionary(source, "dict");
AssertTrue(flattened.ContainsKey("small") && flattened["small"] == "value", "small dictionary value changed");
AssertTrue(flattened.ContainsKey("__af_chunkcount__:large"), "large dictionary count marker missing");
AssertTrue(flattened.ContainsKey("__af_chunk__:large:0"), "large dictionary first chunk missing");
Dictionary<string, string> restored = CampaignSaveChunkHelper.RestoreStringDictionary(flattened);
AssertTrue(restored["small"] == "value" && restored["large"] == unicode, "dictionary chunk restore failed");
AssertTrue(!restored.ContainsKey("__af_chunkcount__:large"), "dictionary metadata leaked into restored values");

Dictionary<string, string> corruptDictionary = new Dictionary<string, string>(flattened, StringComparer.OrdinalIgnoreCase);
corruptDictionary.Remove("__af_chunk__:large:1");
Dictionary<string, string> restoredCorrupt = CampaignSaveChunkHelper.RestoreStringDictionary(corruptDictionary);
AssertTrue(!restoredCorrupt.ContainsKey("large"), "corrupt dictionary value was published");
AssertTrue(restoredCorrupt.ContainsKey("small"), "corrupt dictionary removed unrelated value");

ThrowingDataStore throwing = new ThrowingDataStore();
string safeValue = "safe";
AssertTrue(!CampaignSaveChunkHelper.SafeSyncData(throwing, "throw", ref safeValue), "SafeSyncData did not isolate datastore exception");

MemoryDataStore nullChunk = largeSave.ForLoading();
nullChunk.Values["large__af_chunk_1"] = null;
AssertCorrupt(() => CampaignSaveChunkHelper.LoadChunkedString(nullChunk, "large"), "null chunk");
MemoryDataStore changedChunk = largeSave.ForLoading();
changedChunk.Values["large__af_chunk_0"] = ((string)changedChunk.Values["large__af_chunk_0"]).Replace("汉", "字");
AssertCorrupt(() => CampaignSaveChunkHelper.LoadChunkedString(changedChunk, "large"), "same-byte-length change");
MemoryDataStore oldChunks = largeSave.ForLoading();
oldChunks.Values.Remove("large__af_integrity_bytes"); oldChunks.Values.Remove("large__af_integrity_sha256");
AssertTrue(CampaignSaveChunkHelper.LoadChunkedString(oldChunks, "large") == unicode, "old chunk format compatibility");
oldChunks.Values["large__af_chunk_1"] = null;
AssertCorrupt(() => CampaignSaveChunkHelper.LoadChunkedString(oldChunks, "large"), "old null chunk");
AssertCorrupt(() => CampaignSaveChunkHelper.SaveChunkedString(new ThrowingDataStore(), "failure", unicode), "write exception");
AssertCorrupt(() => CampaignSaveChunkHelper.SaveChunkedString(new RejectingDataStore(), "failure", unicode), "write false");
var nativeSave = new AddOnlyDataStore();
CampaignSaveChunkHelper.SaveChunkedString(nativeSave, "native", unicode);
AssertTrue(CampaignSaveChunkHelper.LoadChunkedString(new ReadValuesDataStore(nativeSave.Values), "native") == unicode, "native Add-only writer");
Console.WriteLine("PASS integrity: old-inline/old-chunks/new-checksum/null/missing/corrupt/failed-write/native-add-only");

CivilWarChunkRegression.Run(args);

Console.WriteLine("PASS persistenceChunkReplay smallInline=1 utf8Boundary=1 missingChunk=1 oversizeCount=1 legacyFallback=1 dictionaryRoundTrip=1 corruptDictionary=1 safeSyncIsolation=1");

internal sealed class MemoryDataStore : IDataStore
{
    public MemoryDataStore(bool saving)
    {
        IsSaving = saving;
        IsLoading = !saving;
    }

    private MemoryDataStore(Dictionary<string, object> values)
    {
        Values = values;
        IsLoading = true;
    }

    public bool IsSaving { get; }
    public bool IsLoading { get; }
    public Dictionary<string, object> Values { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public bool SyncData<T>(string key, ref T data)
    {
        if (IsSaving)
        {
            Values[key] = data;
            return true;
        }
        if (!Values.TryGetValue(key, out object value)) return false;
        if (value is T typed)
        {
            data = typed;
            return true;
        }
        if (value == null)
        {
            data = default;
            return true;
        }
        return false;
    }

    public MemoryDataStore ForLoading()
    {
        return new MemoryDataStore(new Dictionary<string, object>(Values, StringComparer.Ordinal));
    }
}

internal sealed class ThrowingDataStore : IDataStore
{
    public bool IsSaving => true;
    public bool IsLoading => false;
    public bool SyncData<T>(string key, ref T data) => throw new InvalidOperationException("synthetic failure");
}

internal sealed class RejectingDataStore : IDataStore {
 public bool IsSaving => true; public bool IsLoading => false;
 public bool SyncData<T>(string key, ref T value) => false;
}
internal sealed class AddOnlyDataStore : IDataStore {
 public readonly Dictionary<string, object> Values = new();
 public bool IsSaving => true; public bool IsLoading => false;
 public bool SyncData<T>(string key, ref T value) { Values.Add(key,value); return true; }
}
internal sealed class ReadValuesDataStore : IDataStore {
 readonly Dictionary<string, object> values;
 public ReadValuesDataStore(Dictionary<string, object> v) { values=v; }
 public bool IsSaving => false; public bool IsLoading => true;
 public bool SyncData<T>(string key, ref T value) { if(!values.TryGetValue(key,out var v))return false;value=(T)v;return true; }
}
