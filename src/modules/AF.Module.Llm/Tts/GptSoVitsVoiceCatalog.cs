using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Detached, reference-only voice metadata. Loading a catalog never loads a model.
internal sealed class GptSoVitsVoice
{
    internal string Id { get; }
    internal string Label { get; }
    internal bool IsFemale { get; }
    internal bool IsCustom { get; }
    internal string ReferencePath { get; }
    internal string ReferenceHash { get; }
    internal string PromptLanguage { get; }
    internal string PromptText { get; }
    private readonly object _referenceLock = new object();
    private long _verifiedLength = -1;
    private DateTime _verifiedWriteTime;
    private Dictionary<int, GptSoVitsVoice> _moods;

    internal GptSoVitsVoice(string id, string label, bool isFemale, string path, string hash, string language, string prompt, bool isCustom = false)
    {
        Id = id; Label = label; IsFemale = isFemale; ReferencePath = path;
        ReferenceHash = hash; PromptLanguage = language; PromptText = prompt;
        IsCustom = isCustom;
    }

    internal void AddMood(int mood, GptSoVitsVoice reference)
    {
        if (_moods == null) _moods = new Dictionary<int, GptSoVitsVoice>();
        _moods.Add(mood, reference);
    }

    internal GptSoVitsVoice ForMood(int mood)
        => _moods != null && _moods.TryGetValue(mood, out var reference) ? reference : this;

    internal void VerifyReference()
    {
        lock (_referenceLock)
        {
            var file = new FileInfo(ReferencePath);
            if (!file.Exists) throw new FileNotFoundException("voice_reference_missing");
            if (file.Length == 0 || file.Length > 8 * 1024 * 1024)
                throw new InvalidDataException("voice_reference_empty_or_over_8mb");
            if (_verifiedLength == file.Length && _verifiedWriteTime == file.LastWriteTimeUtc) return;
            using (var input = file.OpenRead())
            using (var sha = SHA256.Create())
            {
                string hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
                if (!string.Equals(hash, ReferenceHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("voice_reference_hash_mismatch");
            }
            _verifiedLength = file.Length;
            _verifiedWriteTime = file.LastWriteTimeUtc;
        }
    }
}

internal sealed class GptSoVitsVoiceCatalog
{
    private readonly Dictionary<string, GptSoVitsVoice> _byId = new Dictionary<string, GptSoVitsVoice>(StringComparer.Ordinal);
    private readonly List<GptSoVitsVoice> _male = new List<GptSoVitsVoice>();
    private readonly List<GptSoVitsVoice> _female = new List<GptSoVitsVoice>();
    private readonly List<GptSoVitsVoice> _custom = new List<GptSoVitsVoice>();
    private readonly Dictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Random _random = new Random();
    private readonly object _randomLock = new object();

    // Explicit discovery only: root legacy catalog plus one package directory per voice.
    // Callers cache the result; no recursive scan or model loading.
    internal static GptSoVitsVoiceCatalog LoadDirectory(string root, out string[] diagnostics)
    {
        AnimusForgeDataPaths.ValidatePath(root);
        var result = new GptSoVitsVoiceCatalog();
        var notices = new List<string>();
        var manifests = new List<Tuple<string, string>>();
        var rejected = new HashSet<string>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        string legacy = Path.Combine(root, "manifest.json");
        if (File.Exists(legacy)) manifests.Add(Tuple.Create(legacy, ""));
        foreach (string category in new[] { "Male", "Female", "Custom" })
        {
            string directory = Path.Combine(root, category);
            if (!Directory.Exists(directory)) continue;
            try
            {
                AnimusForgeDataPaths.ValidatePath(directory);
                string direct = Path.Combine(directory, "manifest.json");
                if (File.Exists(direct)) manifests.Add(Tuple.Create(direct, category.ToLowerInvariant()));
                int entries = 0;
                foreach (string folder in Directory.EnumerateDirectories(directory).Take(129).OrderBy(p => p, StringComparer.Ordinal))
                {
                    if (++entries > 128) { notices.Add("voice_directory_limit:" + category); break; }
                    try { AnimusForgeDataPaths.ValidatePath(folder); }
                    catch { notices.Add("voice_path_rejected:" + category); continue; }
                    string manifest = Path.Combine(folder, "manifest.json");
                    if (File.Exists(manifest)) manifests.Add(Tuple.Create(manifest, category.ToLowerInvariant()));
                }
            }
            catch { notices.Add("voice_directory_rejected:" + category); }
        }
        long bytes = 0;
        foreach (var entry in manifests.Take(128))
        {
            try
            {
                AnimusForgeDataPaths.ValidatePath(entry.Item1);
                long size = new FileInfo(entry.Item1).Length;
                if (size > 2 * 1024 * 1024 || (bytes += size) > 8 * 1024 * 1024) throw new InvalidDataException("voice_metadata_limit");
                var catalog = Load(entry.Item1);
                if (entry.Item2.Length != 0 && catalog._byId.Values.Any(v => entry.Item2 == "custom" ? !v.IsCustom : v.IsCustom || v.IsFemale != (entry.Item2 == "female")))
                    throw new InvalidDataException("voice_folder_category_mismatch");
                if (result._byId.Count + catalog._byId.Count > 512) throw new InvalidDataException("voice_count_limit");
                foreach (var pair in catalog._byId)
                {
                    if (rejected.Contains(pair.Key)) continue;
                    if (result._byId.TryGetValue(pair.Key, out var previous))
                    {
                        if (SameVoice(previous, pair.Value)) continue;
                        result._byId.Remove(pair.Key); result._male.Remove(previous); result._female.Remove(previous); result._custom.Remove(previous);
                        rejected.Add(pair.Key); notices.Add("voice_id_conflict:" + pair.Key); continue;
                    }
                    result._byId.Add(pair.Key, pair.Value);
                    if (pair.Value.IsCustom) result._custom.Add(pair.Value);
                    else (pair.Value.IsFemale ? result._female : result._male).Add(pair.Value);
                }
                foreach (var alias in catalog._aliases)
                {
                    if (aliases.TryGetValue(alias.Key, out string target) && target != alias.Value)
                    { rejected.Add(alias.Key); notices.Add("voice_alias_conflict:" + alias.Key); }
                    else aliases[alias.Key] = alias.Value;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException || ex is Newtonsoft.Json.JsonException)
            { notices.Add("voice_pack_rejected:" + Path.GetFileName(Path.GetDirectoryName(entry.Item1)) + ":" + ex.GetType().Name); }
        }
        foreach (var alias in aliases)
            if (!rejected.Contains(alias.Key) && !result._byId.ContainsKey(alias.Key) && result._byId.ContainsKey(alias.Value)) result._aliases.Add(alias.Key, alias.Value);
        if (result._byId.Count == 0) throw new InvalidDataException("voice_catalog_empty");
        if (manifests.Count > 128) notices.Add("voice_manifest_limit");
        diagnostics = notices.ToArray();
        return result;
    }

    private static bool SameVoice(GptSoVitsVoice left, GptSoVitsVoice right)
    {
        if (left.IsFemale != right.IsFemale || left.IsCustom != right.IsCustom) return false;
        for (int mood = 0; mood < 4; mood++)
        {
            var a = left.ForMood(mood); var b = right.ForMood(mood);
            if (!string.Equals(a.ReferencePath, b.ReferencePath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(a.ReferenceHash, b.ReferenceHash, StringComparison.OrdinalIgnoreCase) ||
                a.PromptLanguage != b.PromptLanguage || a.PromptText != b.PromptText) return false;
        }
        return true;
    }

    internal static GptSoVitsVoiceCatalog Load(string manifestPath)
    {
        var file = new FileInfo(manifestPath);
        if (!file.Exists || file.Length > 2 * 1024 * 1024)
            throw new InvalidDataException("voice_manifest_missing_or_over_2mb");
        var document = JObject.Parse(File.ReadAllText(file.FullName));
        var profiles = document["profiles"] as JArray;
        if (profiles == null || profiles.Count == 0 || profiles.Count > 512)
            throw new InvalidDataException("voice_manifest_invalid_format");
        string root = file.DirectoryName;
        var catalog = new GptSoVitsVoiceCatalog();
        foreach (JObject profile in profiles)
        {
            string id = (string)profile["id"];
            string gender = (string)profile["speaker_metadata"]?["gender"];
            string category = (string)profile["category"] ?? gender;
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id.Length > 128 || catalog._byId.ContainsKey(id) ||
                (gender != "male" && gender != "female"))
                throw new InvalidDataException("voice_id_or_gender_invalid_or_duplicate");
            if (category != "custom" && category != gender)
                throw new InvalidDataException("gpt_sovits_catalog_category_invalid");
            var inference = profile["inference_reference"] as JObject;
            if (inference != null && !string.Equals((string)inference["source_sha256"], (string)profile["reference_sha256"], StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("voice_reference_source_hash_mismatch");
            string relative = (string)(inference ?? profile)["reference_file"];
            string hash = (string)(inference ?? profile)["reference_sha256"];
            string language = (string)profile["prompt_lang"];
            string prompt = (string)profile["prompt_text"];
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || string.IsNullOrWhiteSpace(hash) || hash.Length != 64 ||
                (language != "zh" && language != "en" && language != "ja" && language != "ko" && language != "yue") ||
                string.IsNullOrWhiteSpace(prompt) || prompt.Length > 1000)
                throw new InvalidDataException("voice_reference_or_transcript_invalid");
            string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("voice_reference_outside_package");
            string label = ((string)profile["label"] ?? id) + " · " + ((string)profile["style_target"] ?? "");
            if (label.Length > 256) throw new InvalidDataException("voice_label_too_long");
            var voice = new GptSoVitsVoice(id, label, gender == "female", path, hash, language, prompt, category == "custom");
            if (profile["emotion_variants"] != null)
            {
                if (!(profile["emotion_variants"] is JObject moods) || moods.Count > 4)
                    throw new InvalidDataException("gpt_sovits_catalog_moods_invalid");
                foreach (var mood in moods.Properties())
                {
                    int index = mood.Name == "neutral" ? 0 : mood.Name == "happy" ? 1 : mood.Name == "angry" ? 2 : mood.Name == "sad" ? 3 : -1;
                    if (index < 0 || !(mood.Value is JObject reference)) throw new InvalidDataException("gpt_sovits_catalog_moods_invalid");
                    voice.AddMood(index, ReadMoodReference(reference, voice, root));
                }
            }
            catalog._byId.Add(id, voice);
            if (voice.IsCustom) catalog._custom.Add(voice);
            else (voice.IsFemale ? catalog._female : catalog._male).Add(voice);
        }
        if (document["aliases"] != null)
        {
            if (!(document["aliases"] is JObject aliases) || aliases.Count > 512) throw new InvalidDataException("gpt_sovits_catalog_alias_invalid");
            foreach (var alias in aliases.Properties())
            {
                string target = alias.Value.Type == JTokenType.String ? (string)alias.Value : null;
                if (string.IsNullOrWhiteSpace(alias.Name) || alias.Name != alias.Name.Trim() || alias.Name.Length > 128 ||
                    catalog._byId.ContainsKey(alias.Name) || target == null || !catalog._byId.ContainsKey(target))
                    throw new InvalidDataException("gpt_sovits_catalog_alias_invalid");
                catalog._aliases.Add(alias.Name, target);
            }
        }
        return catalog;
    }

    private static GptSoVitsVoice ReadMoodReference(JObject reference, GptSoVitsVoice voice, string root)
    {
        var inference = reference["inference_reference"] as JObject;
        if (inference != null && !string.Equals((string)inference["source_sha256"], (string)reference["reference_sha256"], StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("gpt_sovits_catalog_mood_provenance_invalid");
        string relative = (string)(inference ?? reference)["reference_file"];
        string hash = (string)(inference ?? reference)["reference_sha256"];
        string language = (string)reference["prompt_lang"];
        string prompt = (string)reference["prompt_text"];
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || string.IsNullOrWhiteSpace(hash) || hash.Length != 64 ||
            (language != "zh" && language != "en" && language != "ja" && language != "ko" && language != "yue") ||
            string.IsNullOrWhiteSpace(prompt) || prompt.Length > 1000)
            throw new InvalidDataException("gpt_sovits_catalog_mood_reference_invalid");
        string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("gpt_sovits_catalog_mood_path_invalid");
        return new GptSoVitsVoice(voice.Id, voice.Label, voice.IsFemale, path, hash, language, prompt, voice.IsCustom);
    }

    internal IReadOnlyList<GptSoVitsVoice> ForGender(bool isFemale) => (isFemale ? _female : _male).AsReadOnly();

    // UI-only allocation. Custom voices are listed for explicit selection only,
    // including cross-gender choices made deliberately by the player.
    internal IReadOnlyList<GptSoVitsVoice> ForSelection(bool isFemale)
    {
        var choices = new List<GptSoVitsVoice>(isFemale ? _female : _male);
        choices.AddRange(_custom);
        return choices.AsReadOnly();
    }

    internal GptSoVitsVoice Get(string id)
    {
        string resolved = id ?? "";
        if (_aliases.TryGetValue(resolved, out var target)) resolved = target;
        if (!_byId.TryGetValue(resolved, out var voice))
            throw new InvalidDataException("assigned_voice_missing_reselect_in_encyclopedia");
        return voice;
    }

    internal GptSoVitsVoice ResolveAssignment(string savedId, bool isFemale)
    {
        if (!string.IsNullOrWhiteSpace(savedId))
        {
            var voice = Get(savedId);
            if (!voice.IsCustom && voice.IsFemale != isFemale) throw new InvalidDataException("assigned_voice_gender_mismatch_reselect_in_encyclopedia");
            return voice;
        }
        var pool = isFemale ? _female : _male;
        if (pool.Count == 0) throw new InvalidDataException("voice_gender_pool_empty_text_only");
        lock (_randomLock) return pool[_random.Next(pool.Count)];
    }
}
