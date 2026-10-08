using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Separate current-DLL evidence. Does not alter the pinned J17 source/hash oracle.
internal static class CurrentDllCompatibilityReplay
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool value, string label)
    {
        checks++;
        if (!value) throw new InvalidOperationException(label);
    }
    private static object Get(object value, string name) => value.GetType().GetProperty(name, All)?.GetValue(value)
        ?? value.GetType().GetField(name, All)?.GetValue(value);
    private static void Set(object value, string name, object data) => value.GetType().GetField(name, All).SetValue(value, data);
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, All).Invoke(value, args);

    public static int Main(string[] args)
    {
        try
        {
            string dll = Path.GetFullPath(args[0]);
            string sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll)));
            Check(sha.Equals(args[1], StringComparison.OrdinalIgnoreCase), "candidate DLL hash mismatch");
            AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
            {
                if (new AssemblyName(request.Name).Name == "System.Security.Permissions")
                    return Assembly.LoadFrom(args[2]); // Explicit SDK leaf for net472 Newtonsoft in the net8 replay host.
                string path = Path.Combine(Path.GetDirectoryName(dll), new AssemblyName(request.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            Assembly af = Assembly.LoadFrom(dll);
            Check(af.GetName().Name == "AnimusForge" && af.GetName().Version == new Version(1, 5, 4, 0), "approved assembly identity");
            Type war = af.GetType("AFWarStatsTerminal.Behaviors.AfWarStatsBehavior", true);
            Type storeType = war.GetMethod("SyncData").GetParameters()[0].ParameterType;
            DataStoreProxy Store(bool saving, Dictionary<string, object> data = null)
            {
                var proxy = (DataStoreProxy)DispatchProxy.Create(storeType, typeof(DataStoreProxy));
                proxy.Saving = saving;
                if (data != null) foreach (var pair in data) proxy.Values[pair.Key] = DataStoreProxy.Clone(pair.Value);
                return proxy;
            }
            object NewWar() => Activator.CreateInstance(war);
            object Load(Dictionary<string, object> values)
            {
                object owner = NewWar();
                Call(owner, "SyncData", Store(false, values));
                return owner;
            }
            Dictionary<string, object> Save(object owner)
            {
                var store = Store(true);
                Call(owner, "SyncData", store);
                return store.Values;
            }

            Type record = war.GetNestedType("WarStatsRecord", BindingFlags.NonPublic);
            object first = Activator.CreateInstance(record, true);
            Set(first, "KillsA", 123); Set(first, "CasualtiesB", 45); Set(first, "StartDay", 0);
            string[] counters = { "RaidsA", "RaidsB", "LostTownsA", "LostTownsB", "LostCastlesA", "LostCastlesB" };
            for (int i = 0; i < counters.Length; i++) Set(first, counters[i], i + 1);
            object host = NewWar();
            object ledger = Get(host, "_ledger");
            ((IDictionary)Get(host, "_activeWars")).Add("|", first);
            Call(ledger, "ArchiveAndRemove", "|", first, 9);
            object second = Activator.CreateInstance(record, true);
            Set(second, "KillsA", 7); Set(second, "RaidsA", 8);
            ((IDictionary)Get(host, "_activeWars")).Add("|", second);
            Check(ReferenceEquals(Get(ledger, "ActiveWars"), Get(host, "_activeWars")), "single ledger authority");
            var saved = Save(host);
            Check(saved["_af_war_stats_active_weariness_v6"] is List<string> a && a.Single() == "8,0,0,0,0,0", "v6 active write");
            Check(saved["_af_war_stats_history_weariness_v6"] is List<string> h && h.Single() == "1,2,3,4,5,6", "v6 archive write");
            object loaded = Load(saved);
            object loadedActive = ((IDictionary)Get(loaded, "_activeWars"))["|"];
            object loadedHistory = ((IList)Get(loaded, "_historicalWars"))[0];
            Check((int)Get(loadedActive, "KillsA") == 7 && (int)Get(loadedHistory, "KillsA") == 123, "old counters survive v6 roundtrip");
            for (int i = 0; i < counters.Length; i++) Check((int)Get(loadedHistory, counters[i]) == i + 1, "v6 archived incident " + counters[i]);
            Check((int)Get(loadedActive, "RaidsA") == 8, "reopened pair incidents remain separate");
            Set(first, "RaidsA", 99);
            Check((int)Get(((IList)Get(host, "_historicalWars"))[0], "RaidsA") == 1, "archive is a detached snapshot");
            Check(JsonSerializer.Serialize(saved) == JsonSerializer.Serialize(Save(loaded)), "load/resave idempotence");

            var old = saved.Where(p => !p.Key.EndsWith("weariness_v6", StringComparison.Ordinal)).ToDictionary(p => p.Key, p => p.Value);
            object oldLoaded = Load(old);
            object oldActive = ((IDictionary)Get(oldLoaded, "_activeWars"))["|"];
            object oldHistory = ((IList)Get(oldLoaded, "_historicalWars"))[0];
            Check((int)Get(oldActive, "KillsA") == 7 && (int)Get(oldHistory, "KillsA") == 123, "pre-v6 active/history preserved");
            foreach (string name in counters) Check((int)Get(oldActive, name) == 0 && (int)Get(oldHistory, name) == 0, "missing v6 default " + name);
            var resavedOld = Save(oldLoaded);
            foreach (var pair in old) Check(JsonSerializer.Serialize(pair.Value) == JsonSerializer.Serialize(resavedOld[pair.Key]), "legacy key/value preserved " + pair.Key);
            foreach (object value in new object[] { null, new List<string>(), new List<string> { "bad" }, new List<string> { "1,2,3,4,5,x" }, new List<string> { "2147483648,2,3,4,5,6" } })
            {
                var malformed = new Dictionary<string, object>(old) { ["_af_war_stats_active_weariness_v6"] = value };
                object row = ((IDictionary)Get(Load(malformed), "_activeWars"))["|"];
                Check(counters.All(name => (int)Get(row, name) == 0) && (int)Get(row, "KillsA") == 7, "malformed v6 must not publish partial state");
            }
            var negative = new Dictionary<string, object>(old) { ["_af_war_stats_active_weariness_v6"] = new List<string> { "-1,2,-3,4,-5,6" } };
            object clamped = ((IDictionary)Get(Load(negative), "_activeWars"))["|"];
            Check(counters.Select(name => (int)Get(clamped, name)).SequenceEqual(new[] { 0, 2, 0, 4, 0, 6 }), "negative incident clamp");
            var legacy = new Dictionary<string, object>
            {
                ["_af_war_stats_data_version_v2"] = 1,
                ["_af_war_stats_pair_keys_v1"] = new List<string> { "|", "", "missing" },
                ["_af_war_stats_casualties_a_v1"] = new List<int> { 8, 20 },
                ["_af_war_stats_casualties_b_v1"] = new List<int> { 5, 30 }
            };
            object v1 = Load(legacy);
            IDictionary legacyRows = (IDictionary)Get(v1, "_legacyRecords");
            Check(legacyRows.Count == 1 && (int)Get(legacyRows["|"], "InflictedByA") == 8 && (bool)Get(v1, "_legacyMigrationPending"), "v1 migration queue preserves complete rows");
            int fallback = (int)war.GetMethod("CalculateWeariness", All).Invoke(null, new object[] { 0, 0, 0, 0, 10, 7, 0, 0, 0 });
            Check(fallback == 6, "pre-v6 net-fief-loss weariness fallback");

            // Execute the actual adapter and helper, with only IDataStore/delegated domain leaves controlled.
            const string civilKey = "_af_kingdom_civil_war_v2";
            Type adapter = af.GetType("AnimusForge.CampaignCivilWarPersistenceAdapter", true);
            string payload = "", restored = null;
            object civil = Activator.CreateInstance(adapter, All, null,
                new object[] { (Func<string>)(() => payload), (Action<string>)(text => restored = text) }, null);
            foreach (string text in new[] { "", "{}", "{\"Version\":4,\"Kingdoms\":{}}", string.Concat(Enumerable.Repeat("汉🙂é", 6000)) })
            {
                payload = text;
                var write = Store(true);
                Call(civil, "Save", write);
                Call(civil, "Load", Store(false, write.Values));
                Check(restored == text, "civil-war current chunk roundtrip");
                Check(write.Values.Values.OfType<string>().All(s => Encoding.UTF8.GetByteCount(s) <= 12000), "civil-war UTF8 chunk bound");
                if (Encoding.UTF8.GetByteCount(text) > 240) Check((string)write.Values[civilKey] == "", "no oversized legacy inline copy");
            }
            const string oldJson = "{\"Version\":4,\"ClanExitUntilDay\":{\"saved-clan\":144},\"Operations\":{},\"Kingdoms\":{}}";
            Call(civil, "Load", Store(false, new Dictionary<string, object> { [civilKey] = oldJson }));
            Check(restored == oldJson, "civil-war old inline fallback");
            payload = restored;
            var converted = Store(true); Call(civil, "Save", converted); Call(civil, "Load", Store(false, converted.Values));
            Check(restored == oldJson, "old inline to current chunks is lossless");
            Type module = af.GetType("AnimusForge.CivilWarModuleAdapter", true);
            object business = Activator.CreateInstance(module, true);
            Call(business, "Load", oldJson);
            string businessSaved = (string)Call(business, "Save");
            if (string.IsNullOrEmpty(businessSaved))
            {
                // Surface a serializer environment failure hidden by the production logging leaf.
                Type codec = af.GetType("AnimusForge.KingdomCivilWarStorage", true).Assembly
                    .GetReferencedAssemblies().Where(n => n.Name == "Newtonsoft.Json").Select(Assembly.Load).Single()
                    .GetType("Newtonsoft.Json.JsonConvert", true);
                codec.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { Get(Get(business, "_owner"), "Storage") });
                throw new InvalidOperationException("actual civil-war Save returned empty");
            }
            using (var json = JsonDocument.Parse(businessSaved))
            {
                Check(json.RootElement.GetProperty("ClanExitUntilDay").GetProperty("saved-clan").GetInt32() == 144, "actual old civil-war owner field preserved");
                Check(!json.RootElement.GetProperty("ProtectedClanUntilDay").EnumerateObject().Any(), "missing protection dictionary defaults empty");
            }
            Call(business, "Load", oldJson.Replace("\"Operations\":{}", "\"ProtectedClanUntilDay\":{\"protected-clan\":200},\"Operations\":{}"));
            using (var json = JsonDocument.Parse((string)Call(business, "Save")))
                Check(json.RootElement.GetProperty("ProtectedClanUntilDay").GetProperty("protected-clan").GetInt32() == 200, "new protection field owner roundtrip");
            Type coupBridge = af.GetType("AnimusForge.CoupSystem.CoupRebellionBridge", true);
            Check(Activator.CreateInstance(coupBridge.GetNestedType("AfAccess", BindingFlags.NonPublic), true) != null,
                "actual Coup reflection and typed-memory seam resolves the refactored host");
            foreach (var spec in new[]
            {
                ("AnimusForge.CoupSystem.CoupCampaignBehavior", new[] { "_afCoupSession_v1" }),
                ("AnimusForge.CoupSystem.CoupCaptivityBehavior", new[] { "af_coup_detentions_v1" }),
                ("AnimusForge.CoupSystem.CoupRebellionBridge", new[] { "_afCoupRebellionBridge_v1", "_afCoupOutcomeBridge_v1" })
            })
            {
                object coup = Activator.CreateInstance(af.GetType(spec.Item1, true), true);
                // Rejected old data must remain recoverable, not become a successful empty save.
                string rejected = "{invalid-synthetic-payload:" + new string('a', 33365);
                var data = spec.Item2.ToDictionary(key => key, _ => (object)rejected);
                Call(coup, "SyncData", Store(false, data));
                Check(!(bool)Get(coup, "_saveValid"), "malformed old Coup state rejected " + spec.Item1);
                var convertedCoup = Store(true); Call(coup, "SyncData", convertedCoup);
                foreach (string key in spec.Item2)
                {
                    Check((int)convertedCoup.Values[key + "__af_chunk_count"] > 1, "Coup converted to safe chunks " + key);
                    object restoredCoup = Activator.CreateInstance(af.GetType(spec.Item1, true), true);
                    Call(restoredCoup, "SyncData", Store(false, convertedCoup.Values));
                    var savedAgain = Store(true); Call(restoredCoup, "SyncData", savedAgain);
                    Check(JsonSerializer.Serialize(convertedCoup.Values) == JsonSerializer.Serialize(savedAgain.Values), "rejected Coup state retained on reload " + key);
                }
            }
            Console.WriteLine($"PASS currentDllPersistence assertions={checks} WarStats=v1/pre-v6/v6/archive/null/malformed/idempotence CivilWar=inline/chunks/UTF8/actual-owner-defaults Coup=reflection/4keys/rejected-payload-retention liveSave=NOT_RUN sha256={sha}");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}

public class DataStoreProxy : DispatchProxy
{
    internal bool Saving;
    internal Dictionary<string, object> Values = new(StringComparer.Ordinal);
    internal static object Clone(object value) => value == null ? null
        : JsonSerializer.Deserialize(JsonSerializer.Serialize(value, value.GetType()), value.GetType());
    protected override object Invoke(MethodInfo method, object[] args)
    {
        if (method.Name == "get_IsSaving") return Saving;
        if (method.Name == "get_IsLoading") return !Saving;
        if (method.Name != "SyncData") throw new NotSupportedException(method.Name);
        string key = (string)args[0];
        if (Saving) { Values[key] = Clone(args[1]); return true; }
        if (!Values.TryGetValue(key, out object value)) return false;
        Type type = method.GetGenericArguments().Single();
        if (value != null && !type.IsInstanceOfType(value)) throw new InvalidOperationException("storage type mismatch " + key);
        args[1] = Clone(value);
        return true;
    }
}
