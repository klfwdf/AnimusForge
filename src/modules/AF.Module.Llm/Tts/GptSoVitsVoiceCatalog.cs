using System;
using System.Collections.Generic;
using System.IO;
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
            if (!file.Exists) throw new FileNotFoundException("所选声音的参考录音缺失。");
            if (file.Length == 0 || file.Length > 8 * 1024 * 1024)
                throw new InvalidDataException("参考录音为空或超过首版 8 MB 限制。");
            if (_verifiedLength == file.Length && _verifiedWriteTime == file.LastWriteTimeUtc) return;
            using (var input = file.OpenRead())
            using (var sha = SHA256.Create())
            {
                string hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
                if (!string.Equals(hash, ReferenceHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("所选声音的参考录音校验失败。");
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

    internal static GptSoVitsVoiceCatalog Load(string manifestPath)
    {
        var file = new FileInfo(manifestPath);
        if (!file.Exists || file.Length > 2 * 1024 * 1024)
            throw new InvalidDataException("GPT-SoVITS 音色清单缺失或超过 2 MB，请在 MCM 设置 manifest.json 路径。");
        var document = JObject.Parse(File.ReadAllText(file.FullName));
        var profiles = document["profiles"] as JArray;
        if (profiles == null || profiles.Count == 0 || profiles.Count > 512)
            throw new InvalidDataException("GPT-SoVITS 音色清单格式无效。");
        string root = file.DirectoryName;
        var catalog = new GptSoVitsVoiceCatalog();
        foreach (JObject profile in profiles)
        {
            string id = (string)profile["id"];
            string gender = (string)profile["speaker_metadata"]?["gender"];
            string category = (string)profile["category"] ?? gender;
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id.Length > 128 || catalog._byId.ContainsKey(id) ||
                (gender != "male" && gender != "female"))
                throw new InvalidDataException("音色 ID 或性别无效，或存在重复 ID。");
            if (category != "custom" && category != gender)
                throw new InvalidDataException("gpt_sovits_catalog_category_invalid");
            var inference = profile["inference_reference"] as JObject;
            if (inference != null && !string.Equals((string)inference["source_sha256"], (string)profile["reference_sha256"], StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("推理参考录音的来源校验不匹配。");
            string relative = (string)(inference ?? profile)["reference_file"];
            string hash = (string)(inference ?? profile)["reference_sha256"];
            string language = (string)profile["prompt_lang"];
            string prompt = (string)profile["prompt_text"];
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || string.IsNullOrWhiteSpace(hash) || hash.Length != 64 ||
                (language != "zh" && language != "en" && language != "ja" && language != "ko" && language != "yue") ||
                string.IsNullOrWhiteSpace(prompt) || prompt.Length > 1000)
                throw new InvalidDataException("音色参考录音或参考文本配置无效。");
            string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("参考录音不能指向语音包之外。");
            string label = ((string)profile["label"] ?? id) + " · " + ((string)profile["style_target"] ?? "");
            if (label.Length > 256) throw new InvalidDataException("音色显示名称过长。");
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
            throw new InvalidDataException("已固定的音色不在当前语音包中，请在角色百科重新选择声音。");
        return voice;
    }

    internal GptSoVitsVoice ResolveAssignment(string savedId, bool isFemale)
    {
        if (!string.IsNullOrWhiteSpace(savedId))
        {
            var voice = Get(savedId);
            if (!voice.IsCustom && voice.IsFemale != isFemale) throw new InvalidDataException("固定音色与角色性别不符，请在百科重新选择。");
            return voice;
        }
        var pool = isFemale ? _female : _male;
        if (pool.Count == 0) throw new InvalidDataException("当前语音包没有同性别声音，已回退到文字。");
        lock (_randomLock) return pool[_random.Next(pool.Count)];
    }
}
