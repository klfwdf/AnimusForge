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
    internal static bool IsBattleBlocked => Volatile.Read(ref _battleBlocked) != 0;

    internal static void ObserveMission(object owner, bool blocked, bool ended = false)
    {
        bool cancelLocal;
        lock (Sync)
        {
            if (ended && !ReferenceEquals(owner, _missionOwner)) return;
            if (!ReferenceEquals(owner, _missionOwner)) SceneVoices.Clear();
            _missionOwner = ended ? null : owner;
            cancelLocal = blocked && _battleBlocked == 0;
            Volatile.Write(ref _battleBlocked, blocked ? 1 : 0);
        }
        if (cancelLocal) TtsEngine.Instance.CancelLocalPlayback();
    }

    internal static GptSoVitsVoiceCatalog GetCatalog(DuelSettings settings = null)
    {
        settings = settings ?? DuelSettings.GetSettings();
        string configured = settings?.TtsGptSoVitsManifestPath;
        string path = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AnimusForgeModulePaths.GetCurrentModuleRoot(), "VoicePacks", "manifest.json")
            : configured);
        var file = new FileInfo(path);
        if (!file.Exists) throw new InvalidDataException("音色清单不存在，请在 MCM 设置 manifest.json 路径。");
        lock (Sync)
        {
            if (_catalog == null || _path != path || _writeTime != file.LastWriteTimeUtc || _length != file.Length)
            {
                var catalog = GptSoVitsVoiceCatalog.Load(path);
                _catalog = catalog; _path = path; _writeTime = file.LastWriteTimeUtc; _length = file.Length;
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

    internal static void ClearSceneVoices() { lock (Sync) SceneVoices.Clear(); }

    internal static GptSoVitsSynthesisRequest CaptureRequest(string text, string voiceId, float speed, DuelSettings settings, int mood = 0)
    {
        if (string.IsNullOrWhiteSpace(voiceId)) throw new InvalidDataException("无法确定角色的同性别音色，已回退到文字。");
        return new GptSoVitsSynthesisRequest(settings.TtsGptSoVitsApiUrl, LocalTtsToneTag.Strip(text), GetCatalog(settings).Get(voiceId).ForMood(mood), speed);
    }
}
