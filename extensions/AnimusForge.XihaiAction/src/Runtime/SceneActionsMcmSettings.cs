using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace AnimusForge.XihaiAction
{
    public sealed class SceneActionsMcmSettings :
        AttributeGlobalSettings<SceneActionsMcmSettings>
    {
        private const string GeneralGroup = "{=SAX_MCM_Group_General}General";
        private const string SpeechGroup = "{=SAX_MCM_Group_Speech}Battle speech";
        private const string StageGroup = "{=SAX_MCM_Group_Stage}NPC staging";
        private const string AudienceGroup = "{=SAX_MCM_Group_Audience}Audience response";
        private const string SafetyGroup = "{=SAX_MCM_Group_Safety}Safety and diagnostics";

        public override string Id => "AnimusForge_XihaiAction";
        public override string DisplayName => new TextObject(
            "{=SAX_MCM_Name}Natural Actions and Battle Speech").ToString();
        public override string FolderName => "AnimusForge_XihaiAction";
        public override string FormatType => "json2";

        // Hidden compatibility fields keep existing json2 settings readable. The integrated
        // runtime is always present; users control its natural-language paths with one switch.
        public bool Enabled { get; set; } = true;

        [SettingPropertyBool(
            "{=SAX_MCM_NaturalReplyActions}Natural-language reply actions",
            Order = 0,
            RequireRestart = false,
            HintText = "{=SAX_MCM_NaturalReplyActions_Hint}Recognizes natural action wording in player shouts and NPC replies, uses AF closed-set fallback for complex wording, and recognizes natural battle-speech requests and speech gestures. Disable it to stop these natural-language paths.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public bool NaturalLanguageReplyActionsEnabled { get; set; } = true;

        // Retained as hidden serialization compatibility fields for pre-integration MCM files.
        // Runtime behavior is now governed by NaturalLanguageReplyActionsEnabled.
        public bool ActionsEnabled { get; set; } = true;
        public bool PlayerInputEnabled { get; set; } = true;
        public bool NpcInputEnabled { get; set; } = true;

        public bool DualChannelEnabled { get; set; } = true;

        public bool AfClassifierEnabled { get; set; } = true;
        public bool NaturalSpeechTriggerEnabled { get; set; } = true;
        public bool SpeechTriggerClassifierEnabled { get; set; } = true;
        public bool SpeechSemanticClassifierEnabled { get; set; } = true;

        public bool Kneel { get; set; } = true;
        public bool StandUp { get; set; } = true;
        public bool Xihai { get; set; } = true;
        public bool Cheer { get; set; } = true;
        public bool Applaud { get; set; } = true;
        public bool Respect { get; set; } = true;
        public bool Threat { get; set; } = true;
        public bool Surrender { get; set; } = true;
        public bool Laugh { get; set; } = true;
        public bool Point { get; set; } = true;
        public bool Rage { get; set; } = true;
        public bool Fear { get; set; } = true;
        public bool Disappointed { get; set; } = true;
        public bool Challenge { get; set; } = true;
        public bool Search { get; set; } = true;
        public bool Dance { get; set; } = true;
        public bool Greet { get; set; } = true;
        public bool Agree { get; set; } = true;
        public bool Disagree { get; set; } = true;
        public bool Unsure { get; set; } = true;
        public bool Explain { get; set; } = true;
        public bool Promise { get; set; } = true;
        public bool CrossArms { get; set; } = true;
        public bool DeepBow { get; set; } = true;
        public bool Command { get; set; } = true;
        public bool FollowMe { get; set; } = true;
        public bool CutThroat { get; set; } = true;

        [SettingPropertyBool("{=SAX_MCM_BattleSpeechEnabled}Enable battle speech", Order = 0,
            RequireRestart = false)]
        [SettingPropertyGroup(SpeechGroup, GroupOrder = 3)]
        public bool BattleSpeechEnabled { get; set; } = true;

        [SettingPropertyBool(
            "{=SAX_MCM_TKeyBattleSpeechEnabled}Enable battle-speech recognition from T-key scene shout",
            Order = 1,
            RequireRestart = false,
            HintText = "{=SAX_MCM_TKeyBattleSpeechEnabled_Hint}When enabled, T-key scene-shout text can be routed to the battle-speech channel. The Y-key speech entries remain controlled by the main battle-speech switch and current battle phase.")]
        [SettingPropertyGroup(SpeechGroup)]
        public bool TKeyBattleSpeechEnabled { get; set; } = true;

        [SettingPropertyInteger("{=SAX_MCM_ReplyMin}NPC reply minimum characters", 6, 80,
            "0", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(SpeechGroup)] public int ReplyMinimumChars { get; set; } = 20;
        [SettingPropertyInteger("{=SAX_MCM_ReplyMax}NPC reply maximum characters", 6, 80,
            "0", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(SpeechGroup)] public int ReplyMaximumChars { get; set; } = 60;

        [SettingPropertyBool("{=SAX_MCM_NpcPositioning}Move NPC to the front line", Order = 0,
            RequireRestart = false)]
        [SettingPropertyGroup(StageGroup, GroupOrder = 4)]
        public bool NpcPositioningEnabled { get; set; } = true;
        [SettingPropertyFloatingInteger("{=SAX_MCM_FrontDistance}Distance in front of troops", 2f, 25f,
            "0.0 m", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(StageGroup)] public float FrontDistanceMeters { get; set; } = 10f;
        [SettingPropertyFloatingInteger("{=SAX_MCM_ArrivalRadius}Arrival radius", 0.5f, 4f,
            "0.0 m", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(StageGroup)] public float ArrivalRadiusMeters { get; set; } = 1.5f;
        [SettingPropertyFloatingInteger("{=SAX_MCM_MoveTimeout}Movement timeout", 3f, 45f,
            "0.0 s", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(StageGroup)] public float MovementTimeoutSeconds { get; set; } = 15f;
        // Hidden compatibility fields for older MCM json2 files. Lateral pacing
        // has been removed and none of these values are copied into runtime.
        public bool PacingEnabled { get; set; } = false;
        public bool MountedPacingEnabled { get; set; } = false;
        public bool InfantryPacingEnabled { get; set; } = false;
        public float PacingHalfWidthMeters { get; set; } = 2f;
        public float PacingMinimumIntervalSeconds { get; set; } = 2.5f;
        public float PacingMaximumIntervalSeconds { get; set; } = 4.5f;

        [SettingPropertyBool("{=SAX_MCM_AlliedAudience}Include allied troops", Order = 0,
            RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup, GroupOrder = 5)]
        public bool IncludeAlliedAudience { get; set; } = true;
        [SettingPropertyInteger("{=SAX_MCM_VisualResponders}Maximum visual responders", 1, 128,
            "0", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public int MaximumVisualResponders { get; set; } = 48;
        [SettingPropertyInteger("{=SAX_MCM_VisualWave}Visual responders per wave", 1, 16,
            "0", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public int VisualWaveSize { get; set; } = 6;
        [SettingPropertyInteger("{=SAX_MCM_TickBudget}Maximum actions submitted per tick", 1, 16,
            "0", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public int MaximumVisualSubmissionsPerTick { get; set; } = 6;
        [SettingPropertyBool("{=SAX_MCM_Voices}Native audience voices", Order = 4,
            RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public bool AudienceVoicesEnabled { get; set; } = true;
        [SettingPropertyInteger("{=SAX_MCM_VoiceCount}Audience voice count", 0, 24,
            "0", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public int AudienceVoiceCount { get; set; } = 22;
        [SettingPropertyInteger("{=SAX_MCM_VoiceWave}Voices per wave", 1, 8,
            "0", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public int AudienceVoiceWaveSize { get; set; } = 3;
        [SettingPropertyFloatingInteger("{=SAX_MCM_VoiceInterval}Voice wave interval", 0.05f, 1f,
            "0.00 s", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public float AudienceVoiceWaveIntervalSeconds { get; set; } = 0.18f;
        [SettingPropertyBool("{=SAX_MCM_AudienceReplies}Spoken soldier replies", Order = 8,
            RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public bool AudienceRepliesEnabled { get; set; } = true;
        [SettingPropertyInteger("{=SAX_MCM_AudienceReplyCount}Soldiers giving spoken replies", 0, 24,
            "0", Order = 9, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public int AudienceReplyCount { get; set; } = 16;
        [SettingPropertyFloatingInteger("{=SAX_MCM_AudienceReplyInterval}Spoken reply interval", 0.5f, 3f,
            "0.0 s", Order = 10, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public float AudienceReplyIntervalSeconds { get; set; } = 1.1f;
        [SettingPropertyBool("{=SAX_MCM_Advance}Command and Advance after speech", Order = 11,
            RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public bool TacticalAdvanceEnabled { get; set; } = true;
        [SettingPropertyFloatingInteger("{=SAX_MCM_AdvanceDelay}Advance delay after command gesture", 1.5f, 5f,
            "0.0 s", Order = 12, RequireRestart = false)]
        [SettingPropertyGroup(AudienceGroup)] public float TacticalAdvanceDelaySeconds { get; set; } = 1.8f;

        [SettingPropertyFloatingInteger("{=SAX_MCM_EnemyRadius}Enemy interruption radius", 5f, 75f,
            "0.0 m", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(SafetyGroup, GroupOrder = 6)]
        public float EnemyInterruptRadiusMeters { get; set; } = 35f;
        [SettingPropertyBool("{=SAX_MCM_Notifications}Screen notifications", Order = 1,
            RequireRestart = false)]
        [SettingPropertyGroup(SafetyGroup)] public bool ScreenNotifications { get; set; } = true;
        [SettingPropertyBool("{=SAX_MCM_Diagnostics}Detailed diagnostics", Order = 2,
            RequireRestart = false)]
        [SettingPropertyGroup(SafetyGroup)] public bool DiagnosticsEnabled { get; set; }

        internal static bool TryApplySceneActions(
            SceneActionSettings settings,
            out string error)
        {
            error = null;
            SceneActionsMcmSettings current = GlobalSettings<SceneActionsMcmSettings>.Instance;
            if (current == null)
            {
                return false;
            }
            bool naturalLanguageEnabled = current.NaturalLanguageReplyActionsEnabled;
            settings.Enabled = true;
            settings.PlayerSceneShoutEnabled = naturalLanguageEnabled;
            settings.NpcSceneShoutReplyEnabled = naturalLanguageEnabled;
            settings.AiClassifierEnabled = naturalLanguageEnabled;
            settings.AiClassifierProviderId = naturalLanguageEnabled
                ? "animusforge.main.v130"
                : null;
            settings.DualChannelExperimentalEnabled = naturalLanguageEnabled;
            settings.ScreenBatchSummary = current.DiagnosticsEnabled;
            settings.DeveloperDiagnosticsEnabled = current.DiagnosticsEnabled;
            IReadOnlyList<string> errors = settings.Validate();
            if (errors.Count == 0)
            {
                return true;
            }
            settings.Enabled = false;
            error = string.Join("; ", errors);
            return true;
        }

        internal static bool TryApplyBattleSpeech(
            BattleSpeechSettingsV1 speech,
            BattleSpeechPerformanceSettingsV1 performance,
            BattleSpeechStageSettingsV2 stage,
            out string error)
        {
            error = null;
            SceneActionsMcmSettings current = GlobalSettings<SceneActionsMcmSettings>.Instance;
            if (current == null)
            {
                return false;
            }
            MigrateLegacyBattleSpeechDefaults(current);
            speech.Enabled = current.BattleSpeechEnabled;
            speech.TKeyEnabled = current.TKeyBattleSpeechEnabled;
            speech.EnemyInterruptRadiusMeters = current.EnemyInterruptRadiusMeters;
            speech.ScreenNotifications = current.ScreenNotifications;
            performance.Enabled = speech.Enabled;
            bool naturalLanguageEnabled = current.NaturalLanguageReplyActionsEnabled;
            stage.NaturalTriggerEnabled = naturalLanguageEnabled;
            stage.TriggerClassifierEnabled = naturalLanguageEnabled;
            stage.SemanticClassifierEnabled = naturalLanguageEnabled;
            stage.ReplyMinimumChars = current.ReplyMinimumChars;
            stage.ReplyMaximumChars = current.ReplyMaximumChars;
            stage.NpcPositioningEnabled = current.NpcPositioningEnabled;
            stage.FrontDistanceMeters = current.FrontDistanceMeters;
            stage.ArrivalRadiusMeters = current.ArrivalRadiusMeters;
            stage.MovementTimeoutSeconds = current.MovementTimeoutSeconds;
            stage.PacingEnabled = false;
            stage.MountedPacingEnabled = false;
            stage.InfantryPacingEnabled = false;
            stage.IncludeAlliedAudience = current.IncludeAlliedAudience;
            stage.MaximumVisualResponders = current.MaximumVisualResponders;
            stage.VisualWaveSize = current.VisualWaveSize;
            stage.MaximumVisualSubmissionsPerTick =
                current.MaximumVisualSubmissionsPerTick;
            stage.AudienceVoicesEnabled = current.AudienceVoicesEnabled;
            stage.AudienceVoiceCount = current.AudienceVoiceCount;
            stage.AudienceVoiceWaveSize = current.AudienceVoiceWaveSize;
            stage.AudienceVoiceWaveIntervalSeconds =
                current.AudienceVoiceWaveIntervalSeconds;
            stage.AudienceRepliesEnabled = current.AudienceRepliesEnabled;
            stage.AudienceReplyCount = current.AudienceReplyCount;
            stage.AudienceReplyIntervalSeconds = current.AudienceReplyIntervalSeconds;
            stage.TacticalAdvanceEnabled = current.TacticalAdvanceEnabled;
            stage.TacticalAdvanceDelaySeconds = current.TacticalAdvanceDelaySeconds;

            List<string> errors = speech.Validate()
                .Concat(performance.Validate())
                .Concat(stage.Validate())
                .ToList();
            if (errors.Count == 0)
            {
                return true;
            }
            speech.Enabled = false;
            performance.Enabled = false;
            error = string.Join("; ", errors);
            return true;
        }

        private static void MigrateLegacyBattleSpeechDefaults(
            SceneActionsMcmSettings current)
        {
            bool completeLegacyDefaults = current.ReplyMinimumChars == 50 &&
                                          current.ReplyMaximumChars == 100 &&
                                          Math.Abs(current.FrontDistanceMeters - 8f) < 0.0001f &&
                                          current.AudienceVoiceCount == 12 &&
                                          Math.Abs(current.TacticalAdvanceDelaySeconds - 0.6f) < 0.0001f;
            bool previousIntegratedDefaults = current.ReplyMinimumChars == 6 &&
                                              current.ReplyMaximumChars == 30 &&
                                              Math.Abs(current.FrontDistanceMeters - 10f) < 0.0001f &&
                                              current.AudienceVoiceCount == 22 &&
                                              (Math.Abs(current.TacticalAdvanceDelaySeconds - 0.6f) < 0.0001f ||
                                               Math.Abs(current.TacticalAdvanceDelaySeconds - 1.2f) < 0.0001f);
            bool currentIntegratedDefaults = current.ReplyMinimumChars == 20 &&
                                             current.ReplyMaximumChars == 60 &&
                                             Math.Abs(current.FrontDistanceMeters - 10f) < 0.0001f &&
                                             current.AudienceVoiceCount == 22 &&
                                             Math.Abs(current.TacticalAdvanceDelaySeconds - 1.8f) < 0.0001f;
            bool changed = completeLegacyDefaults;
            if (completeLegacyDefaults)
            {
                current.ReplyMinimumChars = 30;
                current.ReplyMaximumChars = 80;
                current.FrontDistanceMeters = 10f;
                current.AudienceVoiceCount = 22;
                current.TacticalAdvanceDelaySeconds = 1.8f;
            }
            else if (previousIntegratedDefaults)
            {
                current.ReplyMinimumChars = 20;
                current.ReplyMaximumChars = 60;
                changed = true;
            }
            if ((completeLegacyDefaults || previousIntegratedDefaults || currentIntegratedDefaults) &&
                (current.AudienceReplyCount == 4 || current.AudienceReplyCount == 10))
            {
                current.AudienceReplyCount = 16;
                changed = true;
            }
            if (!completeLegacyDefaults &&
                (Math.Abs(current.TacticalAdvanceDelaySeconds - 0.6f) < 0.0001f ||
                 Math.Abs(current.TacticalAdvanceDelaySeconds - 1.2f) < 0.0001f))
            {
                current.TacticalAdvanceDelaySeconds = 1.8f;
                changed = true;
            }
            if (changed)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_MCM",
                    "Migrated legacy battle-speech defaults to the longer speech and visible command timing.");
            }
        }

    }
}
