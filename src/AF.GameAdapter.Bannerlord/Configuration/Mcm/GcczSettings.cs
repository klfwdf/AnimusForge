using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AnimusForge.SiegeAftermathIntervention;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using Newtonsoft.Json;
using TaleWorlds.Localization;

namespace AnimusForge;

// Dedicated MCM frontend; original DuelSettings fields remain the sole saved owners.
public sealed class GcczSettings : AttributeGlobalSettings<GcczSettings>
{
    public override string Id => "AnimusForge_GCCZ_v1";
    public override string DisplayName => new TextObject("{=AF_GCCZ_McmTitle}AnimusForge Siege Aftermath (GCCZ)").ToString();
    public override string FolderName => "AnimusForge";
    public override string FormatType => "json";
    private static DuelSettings Owner => DuelSettings.GetSettings();
    private const string GeneralGroup = "{=AF_GCCZ_McmGeneral}Scene settings";
    private const string GcczLogGroup = "{=AF_GCCZ_McmLog}GCCZ logs";
    private const string SetsLogGroup = "{=AF_GCCZ_McmSetsLog}SETS logs";
    private const string EscortLogGroup = "{=AF_GCCZ_McmEscortLog}Noble-prisoner escort logs";
    // MCM creates editable snapshots and preset instances. Setters must never write
    // through those copies into live DuelSettings or save while the UI is cloning.
    private bool? _unlimited, _diagnosticLog, _verboseLog;
    private int? _responseLimit, _initialCooperation;
    private bool? _setsDiagnosticLog, _setsVerboseLog;
    private int? _gcczLogSize, _setsLogSize, _otherFollowerLimit;
    private void Set<T>(ref T? field, T value, T ownerValue, [CallerMemberName] string name = null) where T : struct
    {
        // Undo returns the live draft to its owner's value. Drop that override so
        // subsequent terminal changes do not get hidden by a cancelled MCM edit.
        if (ReferenceEquals(this, GlobalSettings<GcczSettings>.Instance) && EqualityComparer<T>.Default.Equals(value, ownerValue))
        { field = null; OnPropertyChanged(name); return; }
        if (field.HasValue && EqualityComparer<T>.Default.Equals(field.Value, value)) return;
        field = value; OnPropertyChanged(name);
    }
    public GcczSettings()
    {
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != "SaveTriggered" || !ReferenceEquals(this, GlobalSettings<GcczSettings>.Instance)) return;
            var owner = Owner;
            if (owner == null) return;
            if (_unlimited.HasValue) owner.GcczNpcResponseUnlimited = _unlimited.Value;
            if (_responseLimit.HasValue) owner.GcczNpcResponseLimit = _responseLimit.Value;
            if (_initialCooperation.HasValue) owner.GcczVillageInitialAssistance = _initialCooperation.Value;
            if (_diagnosticLog.HasValue) owner.EnableGcczDiagnosticLog = _diagnosticLog.Value;
            if (_verboseLog.HasValue) owner.EnableGcczVerboseDiagnosticLog = _verboseLog.Value;
            if (_gcczLogSize.HasValue) owner.GcczDiagnosticLogMaxSizeMegabytes = _gcczLogSize.Value;
            if (_setsDiagnosticLog.HasValue) owner.EnableSetsDiagnosticLog = _setsDiagnosticLog.Value;
            if (_setsVerboseLog.HasValue) owner.EnableSetsVerboseDiagnosticLog = _setsVerboseLog.Value;
            if (_setsLogSize.HasValue) owner.SetsDiagnosticLogMaxSizeMegabytes = _setsLogSize.Value;
            if (_otherFollowerLimit.HasValue) owner.SetsOtherSettlementFollowerLimit = _otherFollowerLimit.Value;
            DuelSettings.SaveCurrentSettings();
            _unlimited = _diagnosticLog = _verboseLog = null;
            _responseLimit = _initialCooperation = null;
            _setsDiagnosticLog = _setsVerboseLog = null;
            _gcczLogSize = _setsLogSize = _otherFollowerLimit = null;
        };
    }

    [JsonIgnore]
    [SettingPropertyBool("{=gccz_npc_response_unlimited_name}NPC Response Unlimited", Order = 0, RequireRestart = false, HintText = "{=gccz_npc_response_unlimited_hint}Removes the configured numeric cap in active GCCZ town scenes while retaining event deduplication and queue safety.")]
    [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
    public bool Unlimited { get => _unlimited ?? Owner?.GcczNpcResponseUnlimited ?? false; set => Set(ref _unlimited, value, Owner?.GcczNpcResponseUnlimited ?? false); }
    [JsonIgnore]
    [SettingPropertyInteger("{=gccz_npc_response_limit_name}NPC Response Limit", 1, 10, "0", Order = 1, RequireRestart = false, HintText = "{=gccz_npc_response_limit_hint}Limits ambient NPC replies per player utterance, semantic action, or player attack event in an active GCCZ town scene.")]
    [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
    public int ResponseLimit { get => _responseLimit ?? Owner?.GcczNpcResponseLimit ?? 10; set => Set(ref _responseLimit, Math.Max(1, Math.Min(10, value)), Owner?.GcczNpcResponseLimit ?? 10); }
    [JsonIgnore]
    [SettingPropertyInteger("{=AF_GCCZ_VILLAGE_Initial}GCCZ village initial cooperation", 0, 5, "0", Order = 2, RequireRestart = false,
        HintText = "{=AF_GCCZ_CooperationCap}Only new villages use the initial value. Hidden cooperation is saved per village and capped at 6.")]
    [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
    public int InitialCooperation { get => _initialCooperation ?? Owner?.GcczVillageInitialAssistance ?? 0; set => Set(ref _initialCooperation, Math.Max(0, Math.Min(5, value)), Owner?.GcczVillageInitialAssistance ?? 0); }
    [JsonIgnore]
    [SettingPropertyInteger("{=AF_MCM_Sets_OtherFollowerLimit}Other-settlement escort limit", SetsSettlementEntryProfile.OtherSettlementSelectedFollowerMinLimit, SetsSettlementEntryProfile.OtherSettlementSelectedFollowerMaxLimit, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Sets_OtherFollowerLimit_Hint}Maximum ordinary soldiers configured and brought when entering a town, castle, or village not owned by the player. Default: 10; lowering it trims excess saved configuration.")]
    [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
    public int SetsOtherSettlementFollowerLimit { get => _otherFollowerLimit ?? Owner?.SetsOtherSettlementFollowerLimit ?? SetsSettlementEntryProfile.OtherSettlementSelectedFollowerDefaultLimit; set => Set(ref _otherFollowerLimit, SetsSettlementEntryProfile.ClampOtherSettlementSelectedFollowerLimit(value), Owner?.SetsOtherSettlementFollowerLimit ?? SetsSettlementEntryProfile.OtherSettlementSelectedFollowerDefaultLimit); }
    [JsonIgnore]
    [SettingPropertyButton("{=AF_GCCZ_TownTutorial}GCCZ town tutorial", -1, true, "", Content = "{=AF_GCCZ_VILLAGE_View}View tutorial", Order = 10, RequireRestart = false, HintText = "{=gccz_town_manual_setting_hint}Open a paged manual describing GCCZ town operations.")]
    [SettingPropertyGroup("{=AF_GCCZ_TutorialGroup}Tutorials", GroupOrder = 1)]
    public Action TownTutorial { get; set; } = GcczTownManualInquiryPresenter.Open;
    [JsonIgnore]
    [SettingPropertyButton("{=AF_GCCZ_VILLAGE_Tutorial}GCCZ village tutorial", -1, true, "", Content = "{=AF_GCCZ_VILLAGE_View}View tutorial", Order = 11, RequireRestart = false, HintText = "{=AF_GCCZ_McmVillageTutorialHint}Read about friendly and foreign villages, aid, cooperation, culture changes and departure settlement.")]
    [SettingPropertyGroup("{=AF_GCCZ_TutorialGroup}Tutorials", GroupOrder = 1)]
    public Action VillageTutorial { get; set; } = () => GcczVillageCastleTutorialPresenter.Show(false);
    [JsonIgnore]
    [SettingPropertyButton("{=AF_GCCZ_CastleTutorial}GCCZ castle tutorial", -1, true, "", Content = "{=AF_GCCZ_VILLAGE_View}View tutorial", Order = 12, RequireRestart = false, HintText = "{=AF_GCCZ_McmCastleTutorialHint}Read about castle entry, prisoner handling, companions and departure settlement.")]
    [SettingPropertyGroup("{=AF_GCCZ_TutorialGroup}Tutorials", GroupOrder = 1)]
    public Action CastleTutorial { get; set; } = () => GcczVillageCastleTutorialPresenter.Show(true);
    [JsonIgnore]
    [SettingPropertyBool("{=AF_MCM_Gccz_EnableLog}Write GCCZ log", Order = 20, RequireRestart = false, HintText = "{=AF_MCM_Gccz_EnableLog_Hint}Writes the separate GCCZ_Debug.log. Disabling stops new log entries but still allows exporting or clearing the existing log.")]
    [SettingPropertyGroup(GcczLogGroup, GroupOrder = 2)]
    public bool DiagnosticLog { get => _diagnosticLog ?? Owner?.EnableGcczDiagnosticLog ?? true; set => Set(ref _diagnosticLog, value, Owner?.EnableGcczDiagnosticLog ?? true); }
    [JsonIgnore]
    [SettingPropertyBool("{=AF_MCM_Gccz_VerboseLog}GCCZ verbose log", Order = 21, RequireRestart = false, HintText = "{=AF_MCM_Gccz_VerboseLog_Hint}Records high-frequency diagnostics such as NPC response limits. Enable while troubleshooting; phase changes, outcomes, and exceptions remain logged when disabled.")]
    [SettingPropertyGroup(GcczLogGroup, GroupOrder = 2)]
    public bool VerboseLog { get => _verboseLog ?? Owner?.EnableGcczVerboseDiagnosticLog ?? false; set => Set(ref _verboseLog, value, Owner?.EnableGcczVerboseDiagnosticLog ?? false); }
    [JsonIgnore]
    [SettingPropertyInteger("{=AF_MCM_Gccz_LogLimit}GCCZ log size limit (MB)", 1, 128, "0", Order = 22, RequireRestart = false, HintText = "{=AF_MCM_Gccz_LogLimit_Hint}When GCCZ_Debug.log reaches the limit it rotates automatically and keeps GCCZ_Debug.previous.log. Default: 16 MB.")]
    [SettingPropertyGroup(GcczLogGroup, GroupOrder = 2)]
    public int GcczDiagnosticLogMaxSizeMegabytes { get => _gcczLogSize ?? Owner?.GcczDiagnosticLogMaxSizeMegabytes ?? 16; set => Set(ref _gcczLogSize, Math.Max(1, Math.Min(128, value)), Owner?.GcczDiagnosticLogMaxSizeMegabytes ?? 16); }
    [JsonIgnore]
    [SettingPropertyButton("{=AF_MCM_Gccz_Export}Export GCCZ_Debug.log", -1, true, "", Content = "{=AF_MCM_Gccz_Export_Content}Export to desktop", Order = 23, RequireRestart = false, HintText = "{=AF_MCM_Gccz_Export_Hint}Copies GCCZ_Debug.log from the module Logs folder to the desktop with a timestamped file name.")]
    [SettingPropertyGroup(GcczLogGroup, GroupOrder = 2)]
    public Action ExportGcczDebugLog { get; set; } = () => Owner?.ExportGcczDebugLog?.Invoke();
    [JsonIgnore]
    [SettingPropertyButton("{=AF_MCM_Gccz_Clear}Clear GCCZ_Debug.log", -1, true, "", Content = "{=AF_MCM_Gccz_Clear_Content}Clear now", Order = 24, RequireRestart = false, HintText = "{=AF_MCM_Gccz_Clear_Hint}Clears only the current GCCZ_Debug.log and does not delete the previous rotated log.")]
    [SettingPropertyGroup(GcczLogGroup, GroupOrder = 2)]
    public Action ClearGcczDebugLog { get; set; } = () => Owner?.ClearGcczDebugLog?.Invoke();

    [JsonIgnore]
    [SettingPropertyBool("{=AF_MCM_Sets_EnableLog}Write SETS log", Order = 30, RequireRestart = false, HintText = "{=AF_MCM_Sets_EnableLog_Hint}Writes the separate SETS.log for settlement-entry escort and internal riot diagnostics. Disabling stops new entries but still allows exporting or clearing the existing log.")]
    [SettingPropertyGroup(SetsLogGroup, GroupOrder = 3)]
    public bool EnableSetsDiagnosticLog { get => _setsDiagnosticLog ?? Owner?.EnableSetsDiagnosticLog ?? true; set => Set(ref _setsDiagnosticLog, value, Owner?.EnableSetsDiagnosticLog ?? true); }
    [JsonIgnore]
    [SettingPropertyBool("{=AF_MCM_Sets_VerboseLog}SETS verbose log", Order = 31, RequireRestart = false, HintText = "{=AF_MCM_Sets_VerboseLog_Hint}Records high-frequency diagnostics such as per-frame guards, command refreshes, and spawn progress. Enable while troubleshooting; conflict start, waves, victory, and exceptions remain logged when disabled.")]
    [SettingPropertyGroup(SetsLogGroup, GroupOrder = 3)]
    public bool EnableSetsVerboseDiagnosticLog { get => _setsVerboseLog ?? Owner?.EnableSetsVerboseDiagnosticLog ?? false; set => Set(ref _setsVerboseLog, value, Owner?.EnableSetsVerboseDiagnosticLog ?? false); }
    [JsonIgnore]
    [SettingPropertyInteger("{=AF_MCM_Sets_LogLimit}SETS log size limit (MB)", 1, 128, "0", Order = 32, RequireRestart = false, HintText = "{=AF_MCM_Sets_LogLimit_Hint}When SETS.log reaches the limit it rotates automatically and keeps SETS.previous.log. Default: 16 MB.")]
    [SettingPropertyGroup(SetsLogGroup, GroupOrder = 3)]
    public int SetsDiagnosticLogMaxSizeMegabytes { get => _setsLogSize ?? Owner?.SetsDiagnosticLogMaxSizeMegabytes ?? 16; set => Set(ref _setsLogSize, Math.Max(1, Math.Min(128, value)), Owner?.SetsDiagnosticLogMaxSizeMegabytes ?? 16); }
    [JsonIgnore]
    [SettingPropertyButton("{=AF_MCM_Sets_Export}Export SETS.log", -1, true, "", Content = "{=AF_MCM_Sets_Export_Content}Export to desktop", Order = 33, RequireRestart = false, HintText = "{=AF_MCM_Sets_Export_Hint}Copies SETS.log from the module Logs folder to the desktop with a timestamped file name.")]
    [SettingPropertyGroup(SetsLogGroup, GroupOrder = 3)]
    public Action ExportSetsDiagnosticLog { get; set; } = () => Owner?.ExportSetsDiagnosticLog?.Invoke();
    [JsonIgnore]
    [SettingPropertyButton("{=AF_MCM_Sets_Clear}Clear SETS.log", -1, true, "", Content = "{=AF_MCM_Sets_Clear_Content}Clear now", Order = 34, RequireRestart = false, HintText = "{=AF_MCM_Sets_Clear_Hint}Clears only the current SETS.log and does not delete the previous rotated log.")]
    [SettingPropertyGroup(SetsLogGroup, GroupOrder = 3)]
    public Action ClearSetsDiagnosticLog { get; set; } = () => Owner?.ClearSetsDiagnosticLog?.Invoke();

    [JsonIgnore]
    [SettingPropertyButton("{=AF_MCM_Escort_Export}Export noble-prisoner escort log", -1, true, "", Content = "{=AF_MCM_Escort_Export_Content}Export to desktop", Order = 40, RequireRestart = false, HintText = "{=AF_MCM_Escort_Export_Hint}Copies NoblePrisonerEscort.log to the desktop for troubleshooting prisoner spawning, scene executions, and wilderness-meeting retreats.")]
    [SettingPropertyGroup(EscortLogGroup, GroupOrder = 4)]
    public Action ExportNoblePrisonerEscortLog { get; set; } = () => Owner?.ExportNoblePrisonerEscortLog?.Invoke();
    [JsonIgnore]
    [SettingPropertyButton("{=AF_MCM_Escort_Clear}Clear noble-prisoner escort log", -1, true, "", Content = "{=AF_MCM_Escort_Clear_Content}Clear now", Order = 41, RequireRestart = false, HintText = "{=AF_MCM_Escort_Clear_Hint}Clears only the current NoblePrisonerEscort.log and does not delete the previous rotated log.")]
    [SettingPropertyGroup(EscortLogGroup, GroupOrder = 4)]
    public Action ClearNoblePrisonerEscortLog { get; set; } = () => Owner?.ClearNoblePrisonerEscortLog?.Invoke();
}
