using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class OpenAiTtsVoice
{
    internal OpenAiTtsVoice(string id, string name, int gender) { Id = id; Name = name ?? ""; Gender = gender; }
    internal string Id { get; }
    /// <summary>Server-provided display name, possibly empty.</summary>
    internal string Name { get; }
    internal string Label => Name.Length == 0 ? Id : Name + " (" + Id + ")";
    /// <summary>0 = unknown, 1 = male, 2 = female.</summary>
    internal int Gender { get; }
    internal bool HasHappy { get; private set; }
    internal bool HasAngry { get; private set; }
    internal bool HasSad { get; private set; }
    internal void MarkVariant(LocalTtsTone tone)
    {
        if (tone == LocalTtsTone.Happy) HasHappy = true;
        else if (tone == LocalTtsTone.Angry) HasAngry = true;
        else if (tone == LocalTtsTone.Sad) HasSad = true;
    }
}

// Immutable snapshot of the speech server's /v1/audio/voices list. Fetched off the game
// thread at most once per session per endpoint (or on explicit refresh) and cached under
// Cache/; the API key is only sent as a header and never stored or logged.
internal sealed class OpenAiTtsVoiceCatalog
{
    private const int MaxResponseBytes = 1024 * 1024, MaxVoices = 1024, MaxIdLength = 128, MaxLabelLength = 64;
    private const string CacheFileName = "OpenAiTtsVoices.json";
    private static readonly HttpClient Http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly object Sync = new object();
    private static readonly Dictionary<int, string> SceneVoices = new Dictionary<int, string>();
    private static readonly Random Rng = new Random();
    private static OpenAiTtsVoiceCatalog _current;
    private static bool _cacheLoaded;
    private static string _requestedEndpoint;
    private static Task _fetch;

    private readonly Dictionary<string, OpenAiTtsVoice> _byId;
    private OpenAiTtsVoiceCatalog(string endpoint, List<OpenAiTtsVoice> voices, Dictionary<string, OpenAiTtsVoice> byId)
    { Endpoint = endpoint; Voices = voices.AsReadOnly(); _byId = byId; }

    internal string Endpoint { get; }
    internal IReadOnlyList<OpenAiTtsVoice> Voices { get; }
    internal OpenAiTtsVoice Get(string id) => id != null && _byId.TryGetValue(id, out var voice) ? voice : null;

    internal string PickRandom(bool isFemale)
    {
        var pool = new List<string>();
        foreach (var voice in Voices) if (voice.Gender == (isFemale ? 2 : 1)) pool.Add(voice.Id);
        if (pool.Count == 0) return null;
        lock (Rng) return pool[Rng.Next(pool.Count)];
    }

    /// <summary>Speech URL ".../audio/speech" maps to ".../audio/voices"; anything else has no list.</summary>
    internal static string VoicesEndpoint(string speechUrl)
    {
        if (!Uri.TryCreate((speechUrl ?? "").Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || !string.IsNullOrEmpty(uri.UserInfo)) return null;
        string path = uri.AbsolutePath.TrimEnd('/');
        const string suffix = "/audio/speech";
        if (!path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return null;
        return uri.GetLeftPart(UriPartial.Authority) + path.Substring(0, path.Length - suffix.Length) + "/audio/voices";
    }

    // AF speech service voices (11 bases with _happy/_angry/_sad, plus zh_default). Used when the
    // server does not expose /audio/voices, but only if the configured fallback voice is one of
    // these names, so other OpenAI-compatible providers never receive AF voice names.
    private static readonly string[][] BuiltInVoices =
    {
        new[] { "male-clear", "景元·清亮少年", "1" }, new[] { "male-gentle", "丹恒·温和青年", "1" },
        new[] { "male-steady", "瓦尔特·沉稳长者", "1" }, new[] { "male-deep", "杰帕德·低沉英雄", "1" },
        new[] { "male-senior", "桑博·油滑大叔", "1" }, new[] { "female-clear", "三月七·明朗少女", "2" },
        new[] { "female-gentle", "流萤·温柔少女", "2" }, new[] { "female-steady", "姬子·成熟御姐", "2" },
        new[] { "female-firm", "希露瓦·飒爽大姐", "2" }, new[] { "female-senior", "布洛妮娅·冷静少女", "2" },
        new[] { "female-sparkle", "花火·俏皮戏精", "2" }, new[] { "zh_default", "默认中文女声", "0" }
    };
    private static readonly Lazy<OpenAiTtsVoiceCatalog> BuiltIn = new Lazy<OpenAiTtsVoiceCatalog>(() =>
    {
        var raw = new List<Tuple<string, string, int>>();
        foreach (var voice in BuiltInVoices)
        {
            raw.Add(Tuple.Create(voice[0], voice[1], voice[2] == "1" ? 1 : voice[2] == "2" ? 2 : 0));
            if (voice[2] != "0")
                foreach (string suffix in new[] { "_happy", "_angry", "_sad" }) raw.Add(Tuple.Create(voice[0] + suffix, "", 0));
        }
        return Build("builtin", raw);
    });

    internal bool IsBuiltIn => Endpoint == "builtin";

    /// <summary>The fetched/cached list for this speech URL; otherwise the built-in AF list when the
    /// configured fallback voice belongs to it; otherwise null.</summary>
    internal static OpenAiTtsVoiceCatalog For(string speechUrl, string fallbackVoice)
    {
        EnsureCacheLoaded();
        var catalog = Volatile.Read(ref _current);
        string endpoint = catalog == null ? null : VoicesEndpoint(speechUrl);
        if (endpoint != null && string.Equals(catalog.Endpoint, endpoint, StringComparison.OrdinalIgnoreCase)) return catalog;
        return BuiltIn.Value.Get((fallbackVoice ?? "").Trim()) != null ? BuiltIn.Value : null;
    }

    /// <summary>Starts a background fetch unless this endpoint was already requested this session.</summary>
    internal static bool EnsureFetched(string speechUrl, string apiKey, bool force)
    {
        string endpoint = VoicesEndpoint(speechUrl);
        string key = (apiKey ?? "").Trim();
        if (endpoint == null || key.Length == 0) return false;
        lock (Sync)
        {
            if (_fetch != null && !_fetch.IsCompleted) return true;
            if (!force && string.Equals(_requestedEndpoint, endpoint, StringComparison.OrdinalIgnoreCase)) return false;
            _requestedEndpoint = endpoint;
            _fetch = Task.Run(() => FetchAsync(endpoint, key));
            return true;
        }
    }

    internal static bool IsFetching { get { lock (Sync) return _fetch != null && !_fetch.IsCompleted; } }

    /// <summary>Applies a _happy/_angry variant only when the server list advertises it.</summary>
    internal static string ApplyTone(string speechUrl, string fallbackVoice, string voice, LocalTtsTone tone)
    {
        if (string.IsNullOrWhiteSpace(voice) || (tone != LocalTtsTone.Happy && tone != LocalTtsTone.Angry)) return voice;
        var entry = For(speechUrl, fallbackVoice)?.Get(voice.Trim());
        if (entry == null) return voice;
        if (tone == LocalTtsTone.Happy && entry.HasHappy) return entry.Id + "_happy";
        if (tone == LocalTtsTone.Angry && entry.HasAngry) return entry.Id + "_angry";
        return voice;
    }

    /// <summary>Unsaved per-scene voice for non-hero speakers; empty when no list is available.</summary>
    internal static string ResolveTransient(string speechUrl, string fallbackVoice, bool isFemale, int agentIndex)
    {
        var catalog = For(speechUrl, fallbackVoice);
        if (catalog == null) return "";
        lock (Sync)
        {
            if (agentIndex >= 0 && SceneVoices.TryGetValue(agentIndex, out string cached) && catalog.Get(cached)?.Gender == (isFemale ? 2 : 1))
                return cached;
            string picked = catalog.PickRandom(isFemale) ?? "";
            if (agentIndex >= 0 && picked.Length != 0) SceneVoices[agentIndex] = picked;
            return picked;
        }
    }

    internal static void ClearSceneVoices() { lock (Sync) SceneVoices.Clear(); }

    internal static void Publish(OpenAiTtsVoiceCatalog catalog)
    {
        lock (Sync) { Volatile.Write(ref _current, catalog); _cacheLoaded = true; SceneVoices.Clear(); }
    }

    private static async Task FetchAsync(string endpoint, string apiKey)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) { Logger.Log("TtsVoices", "Voice list request failed: HTTP " + (int)response.StatusCode); return; }
            if (response.Content.Headers.ContentLength > MaxResponseBytes) { Logger.Log("TtsVoices", "Voice list rejected: response too large"); return; }
            string body;
            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var buffer = new MemoryStream())
            {
                byte[] chunk = new byte[8192];
                int read;
                while ((read = await stream.ReadAsync(chunk, 0, chunk.Length).ConfigureAwait(false)) > 0)
                {
                    if (buffer.Length + read > MaxResponseBytes) { Logger.Log("TtsVoices", "Voice list rejected: response too large"); return; }
                    buffer.Write(chunk, 0, read);
                }
                body = Encoding.UTF8.GetString(buffer.ToArray());
            }
            var catalog = Parse(endpoint, body);
            if (catalog == null || catalog.Voices.Count == 0) { Logger.Log("TtsVoices", "Voice list rejected: no usable voices"); return; }
            Publish(catalog);
            WriteCache(catalog);
            Logger.Log("TtsVoices", "Voice list loaded: voices=" + catalog.Voices.Count);
        }
        catch (Exception ex) { Logger.Log("TtsVoices", "Voice list request failed: " + ex.GetType().Name); }
    }

    // Accepts {"voices":[...]}, {"data":[...]}, a bare array, or an id->name object; items are
    // strings or objects carrying id/voice_id/voice/name plus optional label and gender.
    internal static OpenAiTtsVoiceCatalog Parse(string endpoint, string json)
    {
        JToken root;
        try { root = JToken.Parse(json ?? ""); }
        catch (JsonException) { return null; }
        JToken list = root;
        if (root is JObject obj) list = obj["voices"] ?? obj["data"] ?? obj["items"] ?? obj;
        var raw = new List<Tuple<string, string, int>>();
        if (list is JArray array)
        {
            foreach (JToken item in array)
            {
                if (raw.Count >= MaxVoices) break;
                if (item.Type == JTokenType.String) { raw.Add(Tuple.Create((string)item, "", 0)); continue; }
                if (!(item is JObject entry)) continue;
                string id = Str(entry["id"]) ?? Str(entry["voice_id"]) ?? Str(entry["voice"]) ?? Str(entry["name"]);
                string label = Str(entry["display_name"]) ?? Str(entry["label"]) ?? Str(entry["description"]) ?? Str(entry["name"]) ?? "";
                raw.Add(Tuple.Create(id, label, ParseGender(Str(entry["gender"]))));
            }
        }
        else if (list is JObject map)
        {
            foreach (var property in map.Properties())
            {
                if (raw.Count >= MaxVoices) break;
                if (property.Value.Type == JTokenType.String) raw.Add(Tuple.Create(property.Name, (string)property.Value, 0));
                else if (property.Value is JObject entry)
                    raw.Add(Tuple.Create(property.Name, Str(entry["display_name"]) ?? Str(entry["label"]) ?? Str(entry["name"]) ?? "", ParseGender(Str(entry["gender"]))));
            }
        }
        return Build(endpoint, raw);
    }

    private static OpenAiTtsVoiceCatalog Build(string endpoint, List<Tuple<string, string, int>> raw)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in raw) if (IsSafeId(item.Item1)) ids.Add(item.Item1.Trim());
        var voices = new List<OpenAiTtsVoice>();
        var byId = new Dictionary<string, OpenAiTtsVoice>(StringComparer.Ordinal);
        var variants = new List<Tuple<string, LocalTtsTone>>();
        foreach (var item in raw)
        {
            if (!IsSafeId(item.Item1)) continue;
            string id = item.Item1.Trim();
            LocalTtsTone tone = ToneSuffix(id, out string baseId);
            if (tone != LocalTtsTone.Neutral && ids.Contains(baseId)) { variants.Add(Tuple.Create(baseId, tone)); continue; }
            if (byId.ContainsKey(id)) continue;
            string name = CleanLabel(item.Item2);
            int gender = item.Item3 != 0 ? item.Item3 : InferGender(id);
            var voice = new OpenAiTtsVoice(id, name == id ? "" : name, gender);
            voices.Add(voice); byId.Add(id, voice);
        }
        foreach (var variant in variants) if (byId.TryGetValue(variant.Item1, out var voice)) voice.MarkVariant(variant.Item2);
        return new OpenAiTtsVoiceCatalog(endpoint, voices, byId);
    }

    private static LocalTtsTone ToneSuffix(string id, out string baseId)
    {
        foreach (var pair in new[] { Tuple.Create("_happy", LocalTtsTone.Happy), Tuple.Create("_angry", LocalTtsTone.Angry), Tuple.Create("_sad", LocalTtsTone.Sad) })
            if (id.Length > pair.Item1.Length && id.EndsWith(pair.Item1, StringComparison.Ordinal)) { baseId = id.Substring(0, id.Length - pair.Item1.Length); return pair.Item2; }
        baseId = id;
        return LocalTtsTone.Neutral;
    }

    private static string Str(JToken token) => token != null && token.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)token) ? ((string)token).Trim() : null;

    private static bool IsSafeId(string id)
    {
        id = id?.Trim();
        if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength) return false;
        foreach (char c in id) if (char.IsControl(c) || char.IsWhiteSpace(c)) return false;
        return true;
    }

    // Names are shown in an inquiry; drop markup/control characters that TextObject or RichText would interpret.
    private static string CleanLabel(string label)
    {
        var text = new StringBuilder();
        foreach (char c in label ?? "") if (!char.IsControl(c) && c != '{' && c != '}' && c != '<' && c != '>' && c != '[' && c != ']') text.Append(c);
        string clean = text.ToString().Trim();
        return clean.Length > MaxLabelLength ? clean.Substring(0, MaxLabelLength) : clean;
    }

    private static int ParseGender(string value)
    {
        switch ((value ?? "").Trim().ToLowerInvariant())
        {
            case "female": case "f": case "woman": case "女": case "女性": return 2;
            case "male": case "m": case "man": case "男": case "男性": return 1;
            default: return 0;
        }
    }

    // Only an explicit gender field or a male-/female- id prefix counts; names such as a
    // "default female" fallback voice must not enter the random first-speech pools.
    private static int InferGender(string id)
    {
        string lower = id.ToLowerInvariant();
        if (lower.StartsWith("female", StringComparison.Ordinal)) return 2;
        if (lower.StartsWith("male", StringComparison.Ordinal)) return 1;
        return 0;
    }

    private static string CachePath() => Path.Combine(AnimusForgeDataPaths.GetCacheDirectory(AnimusForgeDataPaths.GetCurrentRoot()), CacheFileName);

    private static void EnsureCacheLoaded()
    {
        if (Volatile.Read(ref _cacheLoaded)) return;
        lock (Sync)
        {
            if (_cacheLoaded) return;
            _cacheLoaded = true;
            try
            {
                string path = CachePath();
                var file = new FileInfo(path);
                if (!file.Exists || file.Length > MaxResponseBytes) return;
                var document = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
                string endpoint = Str(document["endpoint"]);
                if (endpoint == null || !(document["voices"] is JArray items)) return;
                var raw = new List<Tuple<string, string, int>>();
                foreach (JToken item in items)
                {
                    if (raw.Count >= MaxVoices || !(item is JObject entry)) continue;
                    string id = Str(entry["id"]);
                    int gender = entry["gender"]?.Type == JTokenType.Integer ? (int)entry["gender"] : 0;
                    raw.Add(Tuple.Create(id, Str(entry["name"]) ?? "", gender is 1 or 2 ? gender : 0));
                    foreach (string suffix in new[] { "happy", "angry", "sad" })
                        if (id != null && entry[suffix]?.Type == JTokenType.Boolean && (bool)entry[suffix]) raw.Add(Tuple.Create(id + "_" + suffix, "", 0));
                }
                var catalog = Build(endpoint, raw);
                if (catalog.Voices.Count != 0) Volatile.Write(ref _current, catalog);
            }
            catch (Exception ex) { Logger.Log("TtsVoices", "Voice cache ignored: " + ex.GetType().Name); }
        }
    }

    private static void WriteCache(OpenAiTtsVoiceCatalog catalog)
    {
        try
        {
            var items = new JArray();
            foreach (var voice in catalog.Voices)
                items.Add(new JObject { ["id"] = voice.Id, ["name"] = voice.Name, ["gender"] = voice.Gender, ["happy"] = voice.HasHappy, ["angry"] = voice.HasAngry, ["sad"] = voice.HasSad });
            var document = new JObject { ["endpoint"] = catalog.Endpoint, ["fetchedUtc"] = DateTime.UtcNow.ToString("o"), ["voices"] = items };
            string path = CachePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, document.ToString(Formatting.Indented), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
        catch (Exception ex) { Logger.Log("TtsVoices", "Voice cache not written: " + ex.GetType().Name); }
    }
}
