using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace AnimusForge;

internal static class GptSoVitsVoiceRuntime
{
    private static readonly object Sync = new object();
    private static readonly Dictionary<int, GptSoVitsVoice> SceneVoices = new Dictionary<int, GptSoVitsVoice>();
    private static GptSoVitsVoiceCatalog _catalog;
    private static string _path;
    private static DateTime _writeTime;
    private static long _length;
    private static object _missionOwner;
    private static int _battleBlocked;
    private static object _primaryMission, _primaryAgent;
    private static int _primaryIndex = -1, _primaryEpoch;

    internal static void BindPrimarySceneSpeaker(object mission, object agent, int agentIndex, int epoch)
    {
        bool changed;
        lock (Sync)
        {
            changed = !ReferenceEquals(_primaryMission, mission) || !ReferenceEquals(_primaryAgent, agent) || _primaryIndex != agentIndex || _primaryEpoch != epoch;
            _primaryMission = mission; _primaryAgent = agent; _primaryIndex = agentIndex; _primaryEpoch = epoch;
        }
        if (changed) TtsEngine.Instance.CancelLocalPlayback();
    }

    internal static bool IsPrimarySceneSpeaker(object mission, object agent, int agentIndex, int epoch)
    {
        lock (Sync) return mission != null && agent != null && agentIndex >= 0 &&
            ReferenceEquals(_primaryMission, mission) && ReferenceEquals(_primaryAgent, agent) &&
            _primaryIndex == agentIndex && _primaryEpoch == epoch;
    }

    private static void ClearPrimaryLocked() { _primaryMission = null; _primaryAgent = null; _primaryIndex = -1; _primaryEpoch = 0; }
    internal static bool IsBattleBlocked => Volatile.Read(ref _battleBlocked) != 0;

    internal static void ObserveMission(object owner, bool blocked, bool ended = false)
    {
        bool cancelLocal;
        lock (Sync)
        {
            if (ended && !ReferenceEquals(owner, _missionOwner)) return;
            if (!ReferenceEquals(owner, _missionOwner) || ended) { SceneVoices.Clear(); ClearPrimaryLocked(); }
            _missionOwner = ended ? null : owner;
            cancelLocal = blocked && _battleBlocked == 0;
            Volatile.Write(ref _battleBlocked, blocked ? 1 : 0);
        }
        if (cancelLocal) TtsEngine.Instance.CancelLocalPlayback();
    }

    internal static GptSoVitsVoiceCatalog GetCatalog(DuelSettings settings = null)
    {
        settings = settings ?? DuelSettings.GetSettings();
        string configured = settings?.UsesManagedGptSoVits == true ? null : settings?.TtsGptSoVitsManifestPath;
        string path = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AnimusForgeModulePaths.GetCurrentModuleRoot(), "VoicePacks", "manifest.json")
            : configured);
        bool installedDirectory = string.IsNullOrWhiteSpace(configured);
        lock (Sync) if (installedDirectory && _catalog != null && _path == path) return _catalog;
        var file = new FileInfo(path);
        if (!installedDirectory && !file.Exists) throw new InvalidDataException("manual_voice_manifest_missing");
        lock (Sync)
        {
            if (_catalog == null || _path != path || _writeTime != file.LastWriteTimeUtc || _length != file.Length)
            {
                var catalog = installedDirectory ? GptSoVitsVoiceCatalog.LoadDirectory(Path.GetDirectoryName(path), out _) : GptSoVitsVoiceCatalog.Load(path);
                _catalog = catalog; _path = path; _writeTime = file.LastWriteTimeUtc; _length = file.Exists ? file.Length : 0;
                SceneVoices.Clear();
            }
            return _catalog;
        }
    }

    internal static string ResolveTransient(bool isFemale, int agentIndex)
    {
        var catalog = GetCatalog();
        lock (Sync)
        {
            if (agentIndex >= 0 && SceneVoices.TryGetValue(agentIndex, out var cached) && cached.IsFemale == isFemale)
                return cached.Id;
            var picked = catalog.ResolveAssignment(null, isFemale);
            if (agentIndex >= 0) SceneVoices[agentIndex] = picked;
            return picked.Id;
        }
    }

    internal static void InvalidateCatalog() { lock (Sync) { _catalog = null; SceneVoices.Clear(); } }

    internal static void ClearSceneVoices() { lock (Sync) { SceneVoices.Clear(); ClearPrimaryLocked(); } }

    // Capture on the game thread; immutable value only crosses the postprocess/queue boundary.
    internal static GptSoVitsLanguage CaptureSpeechLanguage(bool isPrimarySpeaker = true)
    {
        var settings = DuelSettings.GetSettings();
        if (settings?.IsSelectedTtsEnabled != true
            || (settings.TtsUseGptSoVits && (!isPrimarySpeaker || IsBattleBlocked))) return null;
        var language = GptSoVitsLanguage.Parse(settings.TtsGptSoVitsOutputLanguage);
        // Volcengine auto speech does not wait for tone metadata. OpenAI-compatible speech waits
        // (bounded) for the postprocess MOOD to choose a listed voice variant. Local primary-only
        // policy stays local; cloud group participants may request their own translation.
        return settings.TtsUseGptSoVits || language.IsForced || settings.UsesOpenAiTts ? language : null;
    }

    internal static GptSoVitsSynthesisRequest CaptureRequest(string text, string voiceId, float speed, DuelSettings settings, int mood = 0)
    {
        if (!GptSoVitsLanguage.Parse(settings.TtsGptSoVitsOutputLanguage).IsSupported)
            throw new InvalidOperationException(GptSoVitsUiText.Text("LanguageInvalid", "Unsupported speech language. Enter Chinese, Cantonese, English, Japanese, or leave blank for auto."));
        if (string.IsNullOrWhiteSpace(voiceId)) throw new InvalidDataException("voice_assignment_missing_text_only");
        GptSoVitsManagedRuntime.Observe(settings);
        if (!GptSoVitsManagedRuntime.TryCapture(settings.TtsGptSoVitsApiUrl, out string endpoint, out string instance))
            throw new InvalidOperationException(GptSoVitsUiText.Text("RuntimeNotReady", "The local voice runtime is preparing or unavailable. This reply remains text-only; check the local runtime status."));
        if (instance != null && GptSoVitsLanguage.Parse(settings.TtsGptSoVitsOutputLanguage).IsForced && !GptSoVitsManagedRuntime.SupportsLanguageSelection)
            throw new InvalidOperationException(GptSoVitsUiText.Text("LanguageRuntimeOld", "Update the AF-GPTSOVITS package to use forced speech languages. This package supports automatic language only."));
        return new GptSoVitsSynthesisRequest(endpoint, LocalTtsToneTag.Strip(text), GetCatalog(settings).Get(voiceId).ForMood(mood), speed, instance, GptSoVitsLanguage.Parse(settings.TtsGptSoVitsOutputLanguage));
    }
}
