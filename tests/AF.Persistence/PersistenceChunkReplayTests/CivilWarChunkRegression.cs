using AnimusForge;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Compiles the real CampaignSaveChunkHelper with an in-memory IDataStore.
// Host wiring is checked against the live source; this is not a full game load.
internal static class CivilWarChunkRegression
{
    private const string Key = "_af_kingdom_civil_war_v2";
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static void Run(string[] args)
    {
        string repoRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : FindRepoRoot();
        string hostPath = Path.Combine(repoRoot, "src", "AF.GameAdapter.Bannerlord", "Composition", "MyBehavior.cs");
        string source = File.ReadAllText(hostPath);
        Check(IsChunkedHost(source), "civil war host still writes/reads raw JSON instead of chunked storage");
        Check(!IsChunkedHost(source.Replace("CampaignSaveChunkHelper.SaveChunkedString", "dataStore.SyncData")),
            "host wiring guard did not reject an unchunked save mutation");
        Check(!IsChunkedHost(source.Replace("CampaignSaveChunkHelper.LoadChunkedString", "dataStore.SyncData")),
            "host wiring guard did not reject an unchunked load mutation");

        int cases = 0;
        foreach (int bytes in new[] { 240, 241, 11999, 12000, 12001, 32763, 32764, 33365, 100003 })
        {
            string json = MakeJson(bytes, unicode: false);
            RoundTrip(json);
            cases++;
        }
        string unicode = MakeJson(100003, unicode: true);
        RoundTrip(unicode);
        cases++;
        RoundTrip("");
        cases++;

        string incidentSizedJson = MakeJson(33365, unicode: false);
        Check(unchecked((short)(StrictUtf8.GetByteCount(incidentSizedJson) + 4)) == -32167,
            "incident-sized fixture no longer reproduces the original length overflow");
        MemoryDataStore incidentSave = Save(incidentSizedJson);
        Check((int)incidentSave.Values[Key + "__af_chunk_count"] == 3,
            "33365-byte JSON should produce three safe chunks");
        Check((string)incidentSave.Values[Key] == "", "unsafe inline copy was retained");

        // Preserve a readable old inline key, but do not attempt to repair corrupt files.
        MemoryDataStore legacy = new MemoryDataStore(saving: false);
        legacy.Values[Key] = MakeJson(1000, unicode: false);
        string restoredLegacy = CampaignSaveChunkHelper.LoadChunkedString(legacy, Key, "KingdomCivilWar");
        Check(restoredLegacy == (string)legacy.Values[Key], "normal legacy inline key was not readable");
        MemoryDataStore resavedLegacy = Save(restoredLegacy);
        Check((string)resavedLegacy.Values[Key] == "", "legacy resave retained a full inline copy");
        Check(CampaignSaveChunkHelper.LoadChunkedString(resavedLegacy.ForLoading(), Key) == restoredLegacy,
            "normal legacy state changed when resaved in the new format");

        // Reuse a store with stale larger chunks: count must prevent old-tail contamination.
        MemoryDataStore reused = Save(unicode);
        string smaller = MakeJson(241, unicode: false);
        CampaignSaveChunkHelper.SaveChunkedString(reused, Key, smaller, "KingdomCivilWar");
        Check(CampaignSaveChunkHelper.LoadChunkedString(reused.ForLoading(), Key) == smaller,
            "large-to-small save appended stale chunks");
        CampaignSaveChunkHelper.SaveChunkedString(reused, Key, "", "KingdomCivilWar");
        Check(CampaignSaveChunkHelper.LoadChunkedString(reused.ForLoading(), Key) == "",
            "large-to-empty save revived stale state");

        MemoryDataStore missing = incidentSave.ForLoading();
        missing.Values.Remove(Key + "__af_chunk_1");
        Check(CampaignSaveChunkHelper.LoadChunkedString(missing, Key) == "",
            "missing civil war chunk published partial JSON");

        if (args.Length > 1)
        {
            string snapshot = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(snapshot));
            File.WriteAllText(snapshot, JsonSerializer.Serialize(incidentSave.Values), Encoding.UTF8);
        }
        Console.WriteLine($"PASS civilWarChunkRegression roundTripCases={cases} hostWiring=1 negativeWiring=2 " +
            "incidentBytes=33365 incidentChunks=3 utf8Limit=12000 legacyReadAndResave=1 staleTail=2 missingChunk=1");
    }

    private static bool IsChunkedHost(string source)
    {
        const string quotedKey = "\"_af_kingdom_civil_war_v2\"";
        return Regex.IsMatch(source, @"CampaignSaveChunkHelper\.SaveChunkedString\(\s*dataStore\s*,\s*" +
                quotedKey + @"\s*,\s*_civilWarJsonStorage\b")
            && Regex.IsMatch(source, @"_civilWarJsonStorage\s*=\s*CampaignSaveChunkHelper\.LoadChunkedString\(\s*dataStore\s*,\s*" + quotedKey)
            && !Regex.IsMatch(source, @"dataStore\.SyncData(?:<[^>]+>)?\(\s*" + quotedKey);
    }

    private static string MakeJson(int bytes, bool unicode)
    {
        const string prefix = "{\"Version\":4,\"Kingdoms\":{\"synthetic_test\":{\"Note\":\"";
        const string suffix = "\"}}}";
        int remaining = bytes - StrictUtf8.GetByteCount(prefix + suffix);
        Check(remaining >= 0, "fixture size is too small");
        string content;
        if (unicode)
        {
            const string scalarGroup = "汉🙂";
            int groupBytes = StrictUtf8.GetByteCount(scalarGroup);
            content = string.Concat(Enumerable.Repeat(scalarGroup, remaining / groupBytes))
                + new string('a', remaining % groupBytes);
        }
        else content = new string('a', remaining);
        string json = prefix + content + suffix;
        Check(StrictUtf8.GetByteCount(json) == bytes, "fixture UTF-8 size mismatch");
        return json;
    }

    private static MemoryDataStore Save(string json)
    {
        MemoryDataStore store = new MemoryDataStore(saving: true);
        CampaignSaveChunkHelper.SaveChunkedString(store, Key, json, "KingdomCivilWar");
        return store;
    }

    private static void RoundTrip(string json)
    {
        MemoryDataStore save = Save(json);
        foreach (var entry in save.Values)
        {
            Check(StrictUtf8.GetByteCount(entry.Key) + 4 <= short.MaxValue, "unsafe saved key size");
            if (entry.Value is string text)
            {
                int bytes = StrictUtf8.GetByteCount(text); // Throws on split surrogate pairs.
                Check(bytes <= 12000, "saved string exceeded the chunk byte limit");
                Check(unchecked((short)(bytes + 4)) == bytes + 4, "saved entry length overflowed");
            }
        }
        Check((string)save.Values[Key] == (StrictUtf8.GetByteCount(json) <= 240 ? json : ""),
            "unexpected legacy inline storage policy");
        string loaded = CampaignSaveChunkHelper.LoadChunkedString(save.ForLoading(), Key, "KingdomCivilWar");
        Check(loaded == json, "civil war JSON round trip changed content");
        if (loaded.Length > 0)
        {
            using JsonDocument document = JsonDocument.Parse(loaded);
            Check(document.RootElement.GetProperty("Version").GetInt32() == 4, "JSON schema version changed");
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AnimusForge.csproj"))) dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("Pass the repository root as the first argument.");
        return dir.FullName;
    }

    private static void Check(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
    }
}
