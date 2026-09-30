using System.Text;
using AnimusForge;
using AnimusForge.Refactor.Adapters;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private static int Main()
    {
        VerifySaveBoundary();
        VerifyLoadBoundary();
        VerifyFailureSemantics();
        VerifyBehaviorDelegation();
        Console.WriteLine($"World diplomacy persistence-adapter smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifySaveBoundary()
    {
        CampaignSaveChunkHelper.Reset();
        WorldDiplomacyStorage storage = new WorldDiplomacyStorage { Marker = "save-marker" };
        object serialized = null;
        JsonConvert.SerializeHandler = value =>
        {
            serialized = value;
            return "serialized";
        };

        new BannerlordWorldDiplomacyPersistenceAdapter().Save(new FakeDataStore(), storage);

        Test.True(ReferenceEquals(serialized, storage), "save must serialize the canonical storage instance");
        Test.True(CampaignSaveChunkHelper.LastKey == "_af_world_diplomacy_v1",
            "save identity must remain unchanged");
        Test.True(CampaignSaveChunkHelper.LastSource == "WorldDiplomacy"
                  && CampaignSaveChunkHelper.LastSavedJson == "serialized",
            "save must retain the existing chunk source and serialized payload");
    }

    private static void VerifyLoadBoundary()
    {
        CampaignSaveChunkHelper.Reset();
        WorldDiplomacyStorage expected = new WorldDiplomacyStorage { Marker = "loaded" };
        string deserializedJson = "";
        CampaignSaveChunkHelper.NextLoadedJson = "stored-json";
        JsonConvert.DeserializeHandler = (json, type) =>
        {
            deserializedJson = json;
            return expected;
        };

        WorldDiplomacyStorage loaded =
            new BannerlordWorldDiplomacyPersistenceAdapter().Load(new FakeDataStore(), out string error);

        Test.True(ReferenceEquals(loaded, expected) && error.Length == 0,
            "valid persisted JSON must return the deserialized canonical storage");
        Test.True(deserializedJson == "stored-json"
                  && CampaignSaveChunkHelper.LastKey == "_af_world_diplomacy_v1",
            "load must read the existing chunk identity exactly once");

        CampaignSaveChunkHelper.NextLoadedJson = " ";
        JsonConvert.DeserializeHandler = (_, _) => throw new InvalidOperationException("blank JSON was deserialized");
        loaded = new BannerlordWorldDiplomacyPersistenceAdapter().Load(new FakeDataStore(), out error);
        Test.True(loaded != null && error.Length == 0,
            "blank persisted content must keep the existing empty-storage fallback");

        CampaignSaveChunkHelper.NextLoadedJson = "null-result";
        JsonConvert.DeserializeHandler = (_, _) => null;
        loaded = new BannerlordWorldDiplomacyPersistenceAdapter().Load(new FakeDataStore(), out error);
        Test.True(loaded != null && error.Length == 0,
            "a null deserialization result must keep the existing empty-storage fallback");
    }

    private static void VerifyFailureSemantics()
    {
        CampaignSaveChunkHelper.Reset();
        CampaignSaveChunkHelper.ThrowOnLoad = true;
        WorldDiplomacyStorage loaded =
            new BannerlordWorldDiplomacyPersistenceAdapter().Load(new FakeDataStore(), out string error);
        Test.True(loaded != null && error == "synthetic load failure",
            "load failures must fail closed to empty storage and preserve the error message");

        CampaignSaveChunkHelper.Reset();
        CampaignSaveChunkHelper.NextLoadedJson = "corrupt-json";
        JsonConvert.DeserializeHandler = (_, _) => throw new InvalidOperationException("synthetic JSON failure");
        loaded = new BannerlordWorldDiplomacyPersistenceAdapter().Load(new FakeDataStore(), out error);
        Test.True(loaded != null && error == "synthetic JSON failure",
            "deserialization failures must keep the existing empty-storage fallback and error message");

        CampaignSaveChunkHelper.Reset();
        CampaignSaveChunkHelper.ThrowOnSave = true;
        bool saveThrew = false;
        try
        {
            new BannerlordWorldDiplomacyPersistenceAdapter().Save(
                new FakeDataStore(),
                new WorldDiplomacyStorage());
        }
        catch (InvalidOperationException ex)
        {
            saveThrew = ex.Message == "synthetic save failure";
        }
        Test.True(saveThrew, "save failures must preserve the existing propagation behavior");
    }

    private static void VerifyBehaviorDelegation()
    {
        string behavior = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"), Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        string syncData = ExtractMethod(behavior, "public override void SyncData(");
        string adapter = File.ReadAllText(
            FindRepositoryFile("Refactor", "Adapters", "BannerlordWorldDiplomacyPersistenceAdapter.cs"),
            Encoding.UTF8);

        Test.True(syncData.Contains("PersistenceAdapter.Save(dataStore, _storage)", StringComparison.Ordinal)
                  && syncData.Contains("PersistenceAdapter.Load(dataStore, out string loadError)",
                      StringComparison.Ordinal),
            "SyncData must delegate save and load persistence operations");
        Test.True(!syncData.Contains("JsonConvert", StringComparison.Ordinal)
                  && !syncData.Contains("CampaignSaveChunkHelper", StringComparison.Ordinal)
                  && !syncData.Contains("_af_world_diplomacy_v1", StringComparison.Ordinal),
            "SyncData must no longer own JSON, chunking, or persistence identity");
        Test.True(Count(adapter, "_af_world_diplomacy_v1") == 1,
            "the canonical diplomacy save key must have one persistence owner");

        int normalizeBeforeSave = syncData.IndexOf("_orchestration.NormalizeStorage(allowWorldValidation: false);", StringComparison.Ordinal);
        int save = syncData.IndexOf("PersistenceAdapter.Save(dataStore, _storage)", StringComparison.Ordinal);
        int load = syncData.IndexOf("PersistenceAdapter.Load(dataStore, out string loadError)",
            StringComparison.Ordinal);
        int resetAfterLoad = syncData.IndexOf("ResetTransientRuntime(\"load\")", StringComparison.Ordinal);
        int normalizeAfterLoad = syncData.LastIndexOf("_orchestration.NormalizeStorage(allowWorldValidation: false);", StringComparison.Ordinal);
        Test.True(normalizeBeforeSave >= 0 && normalizeBeforeSave < save,
            "storage normalization must still precede saving");
        Test.True(load >= 0 && resetAfterLoad > load && normalizeAfterLoad > resetAfterLoad,
            "load must still reset transient runtime and normalize storage in the existing order");
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Could not locate method: " + signature);
        int openBrace = source.IndexOf('{', start);
        int depth = 0;
        for (int index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0)
            {
                return source.Substring(start, index - start + 1);
            }
        }
        throw new InvalidOperationException("Could not parse method: " + signature);
    }

    private static int Count(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static string FindRepositoryFile(params string[] relativeSegments)
    {
        DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(new[] { current.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeSegments));
    }

    private sealed class FakeDataStore : IDataStore
    {
        public bool IsSaving => false;
        public bool IsLoading => false;
        public bool SyncData<T>(string key, ref T data) => true;
    }
}
