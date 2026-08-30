using AnimusForge;
using System.Collections;
using TaleWorlds.CampaignSystem;

static void AssertTrue(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
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
AssertTrue(CampaignSaveChunkHelper.LoadChunkedString(missingChunk, "large") == string.Empty, "missing chunk did not fail closed");
MemoryDataStore corruptCount = largeSave.ForLoading();
corruptCount.Values["large__af_chunk_count"] = 262145;
AssertTrue(CampaignSaveChunkHelper.LoadChunkedString(corruptCount, "large") == string.Empty, "oversized chunk count did not fail closed");
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
