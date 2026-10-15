using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

// Dedicated MCM owner. DuelSettings retains a backward-compatible runtime projection.
public sealed class SpeechSettings : AttributeGlobalSettings<SpeechSettings>
{
    internal const string SettingsId = "AnimusForge_TTS_v1";
    private const string General = "{=AF_TTS_UI_General}1. Provider and playback";
    private const string Local = "{=AF_TTS_UI_Local}2. Local GPT-SoVITS";
    private const string Cloud = "{=AF_TTS_UI_Cloud}3. Volcengine TTS API";
    private const string Custom = "{=AF_TTS_UI_Custom}4. Custom provider (reserved)";
    private static readonly object Sync = new object();
    private static volatile SpeechSettings _appliedSettings;
    private static volatile DuelSettings _appliedLegacy;
    private static volatile int _appliedRevision = -1;
    private static bool _synchronizing;
    private volatile int _revision;
    private bool _migrationCompleted;

    public override string Id => SettingsId;
    public override string DisplayName => Ui("Title", "AnimusForge Speech TTS Local deployment / API");
    public override string FolderName => "AnimusForge";
    public override string FormatType => "json";
    private static string Ui(string key, string fallback) => new TextObject("{=AF_TTS_UI_" + key + "}" + fallback).ToString();
    private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; _revision++; OnPropertyChanged(name);
    }

    // Persisted in the complete backend schema, hidden only in the frontend.
    // A preset reset must not reimport stale legacy credentials over newer values.
    [SettingPropertyBool("Migration state", Order = 999, RequireRestart = false)]
    [SettingPropertyGroup(General)]
    public bool LegacyMigrationCompleted { get => _migrationCompleted; set { if (value) _migrationCompleted = true; } }

    private bool _enabled = true;
    [SettingPropertyBool("{=AF_TTS_UI_Enable}Enable speech", Order = 0, RequireRestart = false)]
    [SettingPropertyGroup(General)]
    public bool EnableTtsSpeech { get => _enabled; set => Set(ref _enabled, value); }

    // MCM serializes a Dropdown as its integer index, retaining the existing
    // ProviderMode JSON key and 0/1/2 values for settings and presets.
    private readonly Dropdown<string> _provider = new Dropdown<string>(new[]
    {
        "{=AF_TTS_UI_ProviderLocal}Local GPT-SoVITS",
        "{=AF_TTS_UI_ProviderCloud}Volcengine TTS API",
        "{=AF_TTS_UI_ProviderCustom}Custom (coming later)"
    }, 0);
    [SettingPropertyDropdown("{=AF_TTS_UI_Provider}Speech provider", Order = 1, RequireRestart = false,
        HintText = "{=AF_TTS_UI_ProviderHint}Choose local GPT-SoVITS or Volcengine TTS API. Custom is reserved and outputs text only. Switching preserves credentials; local failures never fall back to paid cloud speech.")]
    [SettingPropertyGroup(General)]
    public Dropdown<string> ProviderMode
    {
        get => _provider;
        set => _provider.SelectedIndex = Math.Max(0, Math.Min(2, value?.SelectedIndex ?? 0));
    }

    private void ProviderSelectionChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(Dropdown<string>.SelectedIndex)) return;
        int index = Math.Max(0, Math.Min(2, _provider.SelectedIndex));
        if (index != _provider.SelectedIndex) { _provider.SelectedIndex = index; return; }
        // MCM edits SelectedIndex in place, without invoking the property setter.
        // Invalidate the existing bounded runtime projection on that event only.
        _revision++;
        OnPropertyChanged(nameof(ProviderMode));
    }

    private float _volume = 1f;
    [SettingPropertyFloatingInteger("{=AF_TTS_UI_Volume}Main voice volume", 0f, 1f, "0.00", Order = 2, RequireRestart = false)]
    [SettingPropertyGroup(General)]
    public float TtsVolcDedicatedVolume { get => _volume; set => Set(ref _volume, value); }

    private bool _sceneWinmm = true;
    [SettingPropertyBool("{=AF_TTS_UI_SceneWinmm}Scene playback through winmm (no echo)", Order = 3, RequireRestart = false)]
    [SettingPropertyGroup(General)]
    public bool TtsSceneUseWinmmAudible { get => _sceneWinmm; set => Set(ref _sceneWinmm, value); }

    private float _lipVolume;
    [SettingPropertyFloatingInteger("{=AF_TTS_UI_LipVolume}Lip-sync channel volume", 0f, 1f, "0.00", Order = 4, RequireRestart = false)]
    [SettingPropertyGroup(General)]
    public float TtsLipSyncSoundEventVolume { get => _lipVolume; set => Set(ref _lipVolume, value); }

    private string _localUrl = "http://127.0.0.1:9880/tts";
    [SettingPropertyText("{=AF_TTS_UI_LocalUrl}Local GPT-SoVITS API URL", -1, true, "", Order = 0, RequireRestart = false)]
    [SettingPropertyGroup(Local)]
    public string TtsGptSoVitsApiUrl { get => _localUrl; set => Set(ref _localUrl, value ?? ""); }

    private string _manifest = "";
    [SettingPropertyText("{=AF_TTS_UI_Manifest}Voice manifest path (blank: installed pack)", -1, true, "", Order = 1, RequireRestart = false,
        HintText = "{=AF_TTS_UI_ManifestHint}Five male and five female default voices. Custom voices are manual-only and can be assigned in the encyclopedia before any conversation.")]
    [SettingPropertyGroup(Local)]
    public string TtsGptSoVitsManifestPath { get => _manifest; set => Set(ref _manifest, value ?? ""); }

    private float _localSpeed = 1f;
    [SettingPropertyFloatingInteger("{=AF_TTS_UI_LocalSpeed}Local speech speed", 0.5f, 2f, "0.00", Order = 2, RequireRestart = false)]
    [SettingPropertyGroup(Local)]
    public float TtsGptSoVitsSpeed { get => _localSpeed; set => Set(ref _localSpeed, value); }

    private int _mood;
    [SettingPropertyInteger("{=AF_TTS_UI_Mood}Audition tone: 0 Calm / 1 Happy / 2 Angry / 3 Sad", 0, 3, "0", Order = 3, RequireRestart = false,
        HintText = "{=AF_TTS_UI_MoodHint}For manual voice tests only. Dialogue uses the hidden TTS_TONE tag; missing or invalid tags use Calm. Missing reference tones use the same voice base reference.")]
    [SettingPropertyGroup(Local)]
    public int TtsGptSoVitsMood { get => _mood; set => Set(ref _mood, Math.Max(0, Math.Min(3, value))); }

    private string _cloudUrl = "https://openspeech.bytedance.com/api/v1/tts";
    [SettingPropertyText("{=AF_TTS_UI_CloudUrl}Volcengine TTS API URL", -1, true, "", Order = 0, RequireRestart = false,
        HintText = "{=AF_TTS_UI_CloudUrlHint}Supports existing V1, V3 HTTP unidirectional and Agent Plan gateways. See official documentation below. SSE/WS is not implemented.")]
    [SettingPropertyGroup(Cloud)]
    public string TtsVolcDedicatedApiUrl { get => _cloudUrl; set => Set(ref _cloudUrl, value ?? ""); }

    private string _key = "";
    [SettingPropertyText("{=AF_TTS_UI_Key}Volcengine token / API key", -1, true, "", Order = 1, RequireRestart = false,
        HintText = "{=AF_TTS_UI_KeyHint}V1 uses the legacy token; V3 uses the speech-service API key. Agent Plan requires its dedicated key. Switching does not erase credentials.")]
    [SettingPropertyGroup(Cloud)]
    public string TtsVolcDedicatedApiKey { get => _key; set => Set(ref _key, value ?? ""); }

    private string _appId = "";
    [SettingPropertyText("{=AF_TTS_UI_AppId}Volcengine App ID (V1)", -1, true, "", Order = 2, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public string TtsVolcDedicatedAppKey { get => _appId; set => Set(ref _appId, value ?? ""); }

    private string _resource = "";
    [SettingPropertyText("{=AF_TTS_UI_Resource}Volcengine resource ID", -1, true, "", Order = 3, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public string TtsVolcDedicatedResourceId { get => _resource; set => Set(ref _resource, value ?? ""); }

    private string _speaker = "";
    [SettingPropertyText("{=AF_TTS_UI_Speaker}Volcengine voice ID", -1, true, "", Order = 4, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public string TtsVolcDedicatedSpeaker { get => _speaker; set => Set(ref _speaker, value ?? ""); }

    private string _additions = "{}";
    [SettingPropertyText("{=AF_TTS_UI_Additions}Additional parameters (JSON)", -1, true, "", Order = 5, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public string TtsVolcDedicatedAdditionsJson { get => _additions; set => Set(ref _additions, value ?? "{}"); }

    private string _format = "wav";
    [SettingPropertyText("{=AF_TTS_UI_Format}Audio format (wav / pcm)", -1, true, "", Order = 6, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public string TtsVolcDedicatedAudioFormat { get => _format; set => Set(ref _format, value ?? "wav"); }

    private int _rate = 24000;
    [SettingPropertyInteger("{=AF_TTS_UI_Rate}Sample rate", 8000, 24000, "0", Order = 7, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public int TtsVolcDedicatedSampleRate { get => _rate; set => Set(ref _rate, value); }

    private float _cloudSpeed = 1f;
    [SettingPropertyFloatingInteger("{=AF_TTS_UI_CloudSpeed}Cloud speech speed", 0.1f, 2f, "0.00", Order = 8, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public float TtsVolcDedicatedSpeed { get => _cloudSpeed; set => Set(ref _cloudSpeed, value); }

    [SettingPropertyButton("{=AF_TTS_UI_TestLocal}Test local speech (save first)", -1, true, "", Content = "{=AF_TTS_UI_Test}Test", Order = 4, RequireRestart = false)]
    [SettingPropertyGroup(Local)]
    public Action TestLocal { get; set; }

    [SettingPropertyButton("{=AF_TTS_UI_TestCloud}Test Volcengine speech (may incur charges)", -1, true, "", Content = "{=AF_TTS_UI_Test}Test", Order = 9, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public Action TestCloud { get; set; }

    [SettingPropertyButton("{=AF_TTS_UI_OpenConsole}Official Volcengine speech console", -1, true, "", Content = "{=AF_TTS_UI_Open}Open", Order = 10, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public Action OpenConsole { get; set; }

    [SettingPropertyButton("{=AF_TTS_UI_OpenDocs}Official TTS API documentation", -1, true, "", Content = "{=AF_TTS_UI_Open}Open", Order = 11, RequireRestart = false)]
    [SettingPropertyGroup(Cloud)]
    public Action OpenDocs { get; set; }

    [SettingPropertyButton("{=AF_TTS_UI_CustomInfo}Custom provider: not implemented yet", -1, true, "", Content = "{=AF_TTS_UI_Open}Open", Order = 0, RequireRestart = false)]
    [SettingPropertyGroup(Custom)]
    public Action CustomInfo { get; set; }

    public SpeechSettings()
    {
        _provider.PropertyChanged += ProviderSelectionChanged;
        TestLocal = () => TestSpeech(false);
        TestCloud = () => TestSpeech(true);
        OpenConsole = () => OpenOfficialUrl("https://console.volcengine.com/speech/app");
        OpenDocs = () => OpenOfficialUrl("https://docs.volcengine.com/docs/DoubaoVoice/HTTPChunkedSSEUnidirectionalStreaming-V3?lang=zh");
        CustomInfo = () => InformationManager.DisplayMessage(new InformationMessage(Ui("CustomNotice", "Custom provider is reserved; no request is sent. Custom local voices can already be assigned manually in the encyclopedia.")));
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != "SaveTriggered" || !ReferenceEquals(this, GlobalSettings<SpeechSettings>.Instance)) return;
            Synchronize(DuelSettings.GetSettings());
            DuelSettings.SaveCurrentSettings();
        };
    }
    private void TestSpeech(bool cloud)
    {
        if (!ReferenceEquals(this, GlobalSettings<SpeechSettings>.Instance))
        {
            InformationManager.DisplayMessage(new InformationMessage(Ui("SaveFirst", "Save the TTS settings before testing.")));
            return;
        }
        var legacy = DuelSettings.GetSettings();
        Synchronize(legacy);
        if (cloud) legacy.TestTtsVolcDedicatedVoice?.Invoke();
        else legacy.TestTtsGptSoVitsVoice?.Invoke();
    }
    private static void OpenOfficialUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { InformationManager.DisplayMessage(new InformationMessage(url)); }
    }

    internal void ImportLegacy(DuelSettings legacy)
    {
        EnableTtsSpeech = legacy.EnableTtsSpeech && (legacy.TtsUseGptSoVits || legacy.TtsVolcDedicatedEnabled);
        ProviderMode.SelectedIndex = legacy.TtsUseGptSoVits ? 0 : legacy.TtsVolcDedicatedEnabled ? 1 : 0;
        TtsVolcDedicatedVolume = legacy.TtsVolcDedicatedVolume;
        TtsSceneUseWinmmAudible = legacy.TtsSceneUseWinmmAudible;
        TtsLipSyncSoundEventVolume = legacy.TtsLipSyncSoundEventVolume;
        TtsGptSoVitsApiUrl = legacy.TtsGptSoVitsApiUrl;
        TtsGptSoVitsSpeed = legacy.TtsGptSoVitsSpeed;
        TtsVolcDedicatedApiUrl = legacy.TtsVolcDedicatedApiUrl;
        TtsVolcDedicatedApiKey = legacy.TtsVolcDedicatedApiKey;
        TtsVolcDedicatedAppKey = legacy.TtsVolcDedicatedAppKey;
        TtsVolcDedicatedResourceId = legacy.TtsVolcDedicatedResourceId;
        TtsVolcDedicatedSpeaker = legacy.TtsVolcDedicatedSpeaker;
        TtsVolcDedicatedAdditionsJson = legacy.TtsVolcDedicatedAdditionsJson;
        TtsVolcDedicatedAudioFormat = legacy.TtsVolcDedicatedAudioFormat;
        TtsVolcDedicatedSampleRate = legacy.TtsVolcDedicatedSampleRate;
        TtsVolcDedicatedSpeed = legacy.TtsVolcDedicatedSpeed;
        // The requested curated installed catalog becomes the new default. Old
        // external packs/config files are not deleted and can be selected manually.
        TtsGptSoVitsManifestPath = "";
        LegacyMigrationCompleted = true;
    }
    internal void ApplyToLegacy(DuelSettings legacy)
    {
        legacy.TtsUseGptSoVits = ProviderMode.SelectedIndex == 0;
        legacy.TtsVolcDedicatedEnabled = ProviderMode.SelectedIndex == 1;
        legacy.EnableTtsSpeech = EnableTtsSpeech;
        legacy.TtsVolcDedicatedVolume = TtsVolcDedicatedVolume;
        legacy.TtsSceneUseWinmmAudible = TtsSceneUseWinmmAudible;
        legacy.TtsLipSyncSoundEventVolume = TtsLipSyncSoundEventVolume;
        legacy.TtsGptSoVitsApiUrl = TtsGptSoVitsApiUrl;
        legacy.TtsGptSoVitsManifestPath = TtsGptSoVitsManifestPath;
        legacy.TtsGptSoVitsSpeed = TtsGptSoVitsSpeed;
        legacy.TtsGptSoVitsMood = TtsGptSoVitsMood;
        legacy.TtsVolcDedicatedApiUrl = TtsVolcDedicatedApiUrl;
        legacy.TtsVolcDedicatedApiKey = TtsVolcDedicatedApiKey;
        legacy.TtsVolcDedicatedAppKey = TtsVolcDedicatedAppKey;
        legacy.TtsVolcDedicatedResourceId = TtsVolcDedicatedResourceId;
        legacy.TtsVolcDedicatedSpeaker = TtsVolcDedicatedSpeaker;
        legacy.TtsVolcDedicatedAdditionsJson = TtsVolcDedicatedAdditionsJson;
        legacy.TtsVolcDedicatedAudioFormat = TtsVolcDedicatedAudioFormat;
        legacy.TtsVolcDedicatedSampleRate = TtsVolcDedicatedSampleRate;
        legacy.TtsVolcDedicatedSpeed = TtsVolcDedicatedSpeed;
    }
    // Identity/revision checks on unchanged reads; no reflection, full settings
    // scan or disk IO on each GetSettings/Tick. Work is bounded to the TTS fields.
    internal static void Synchronize(DuelSettings legacy, bool allowMigration = true)
    {
        var settings = GlobalSettings<SpeechSettings>.Instance;
        if (settings == null || legacy == null) return;
        if (ReferenceEquals(settings, _appliedSettings) && ReferenceEquals(legacy, _appliedLegacy) && _appliedRevision == settings._revision) return;
        lock (Sync)
        {
            if (_synchronizing || (!settings.LegacyMigrationCompleted && !allowMigration)) return;
            _synchronizing = true;
            try
            {
                bool imported = !settings.LegacyMigrationCompleted;
                if (imported) settings.ImportLegacy(legacy);
                settings.ApplyToLegacy(legacy);
                _appliedSettings = settings; _appliedLegacy = legacy; _appliedRevision = settings._revision;
                if (imported) BaseSettingsProvider.Instance?.SaveSettings(settings);
            }
            finally { _synchronizing = false; }
        }
    }
}
