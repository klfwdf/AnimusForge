using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AnimusForge.SceneActions.Core;
using Newtonsoft.Json.Linq;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Compatibility bridge for the integrated AF MCM.  This type deliberately
    /// does not inherit AttributeGlobalSettings and has no MCM attributes, so
    /// only AnimusForge.DuelSettings is registered by MCM.  Reflection keeps
    /// the standalone XihaiAction build independent from the AF assembly.
    /// Reflection metadata is cached and this bridge runs only at initialization
    /// or an explicit MCM refresh, never from Mission Tick.
    /// </summary>
    internal static class SceneActionsMcmSettings
    {
        // Version 2 re-runs the battle-speech defaults migration for users who
        // already completed the original integrated-MCM migration.  The old
        // version could leave the legacy 30/80 speech-length profile active.
        private const int IntegratedMigrationVersion = 2;
        private const string DuelSettingsTypeName = "AnimusForge.DuelSettings";
        private const string LegacySettingsFileName = "AnimusForge_XihaiAction.json";
        private const int MaximumAudienceVoiceCount = 24;
        private const int MaximumAudienceVoiceWaveSize = 8;

        private static readonly object AccessorSync = new object();
        private static readonly string[] MigratedPropertyNames =
        {
            "NaturalLanguageReplyActionsEnabled", "BattleSpeechEnabled", "TKeyBattleSpeechEnabled",
            "ReplyMinimumChars", "ReplyMaximumChars", "NpcPositioningEnabled", "FrontDistanceMeters",
            "ArrivalRadiusMeters", "MovementTimeoutSeconds", "IncludeAlliedAudience", "MaximumVisualResponders",
            "VisualWaveSize", "MaximumVisualSubmissionsPerTick", "AudienceVoicesEnabled", "AudienceVoiceCount",
            "AudienceVoiceWaveSize", "AudienceVoiceWaveIntervalSeconds", "AudienceRepliesEnabled", "AudienceReplyCount",
            "AudienceReplyWaveSize", "MaximumAudienceReplySubmissionsPerTick", "AudienceReplyMinimumChars",
            "AudienceReplyMaximumChars", "AudienceReplyMinimumIntervalSeconds", "AudienceReplyMaximumIntervalSeconds",
            "AudienceReplyIntervalSeconds", "TacticalAdvanceEnabled", "TacticalAdvanceDelaySeconds",
            "EnemyInterruptRadiusMeters", "ScreenNotifications", "DiagnosticsEnabled", "PacingEnabled",
            "MountedPacingEnabled", "InfantryPacingEnabled", "PacingHalfWidthMeters", "PacingMinimumIntervalSeconds",
            "PacingMaximumIntervalSeconds", "DualChannelEnabled", "AfClassifierEnabled", "NaturalSpeechTriggerEnabled",
            "SpeechTriggerClassifierEnabled", "SpeechSemanticClassifierEnabled", "Kneel", "StandUp", "Xihai", "Cheer",
            "Applaud", "Respect", "Threat", "Surrender", "Laugh", "Point", "Rage", "Fear", "Disappointed",
            "Challenge", "Search", "Dance", "Greet", "Agree", "Disagree", "Unsure", "Explain", "Promise",
            "CrossArms", "DeepBow", "Command", "FollowMe", "CutThroat"
        };

        private static bool _accessorSearched;
        private static MethodInfo _getSettingsMethod;
        private static readonly Dictionary<string, PropertyInfo> Properties =
            new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

        internal static bool TryApplySceneActions(SceneActionSettings settings, out string error)
        {
            error = null;
            if (!TryReadSnapshot(out Snapshot current))
            {
                return false;
            }
            bool naturalLanguageEnabled = current.NaturalLanguageReplyActionsEnabled;
            settings.Enabled = true;
            settings.PlayerSceneShoutEnabled = naturalLanguageEnabled;
            settings.NpcSceneShoutReplyEnabled = naturalLanguageEnabled;
            settings.AiClassifierEnabled = naturalLanguageEnabled;
            settings.AiClassifierProviderId = naturalLanguageEnabled ? "animusforge.main.v130" : null;
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
            if (!TryReadSnapshot(out Snapshot current))
            {
                return false;
            }
            speech.Enabled = current.BattleSpeechEnabled;
            speech.TKeyEnabled = current.TKeyBattleSpeechEnabled;
            speech.EnemyInterruptRadiusMeters = current.EnemyInterruptRadiusMeters;
            speech.ScreenNotifications = current.ScreenNotifications;
            speech.MaximumAudienceReplySubmissionsPerTick = current.MaximumAudienceReplySubmissionsPerTick;
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
            stage.MaximumVisualSubmissionsPerTick = current.MaximumVisualSubmissionsPerTick;
            stage.AudienceVoicesEnabled = current.AudienceVoicesEnabled;
            stage.AudienceVoiceCount = current.AudienceVoiceCount;
            stage.AudienceVoiceWaveSize = current.AudienceVoiceWaveSize;
            stage.AudienceVoiceWaveIntervalSeconds = current.AudienceVoiceWaveIntervalSeconds;
            stage.AudienceRepliesEnabled = current.AudienceRepliesEnabled;
            stage.AudienceReplyCount = current.AudienceReplyCount;
            stage.AudienceReplyWaveSize = current.AudienceReplyWaveSize;
            stage.MaximumAudienceReplySubmissionsPerTick = current.MaximumAudienceReplySubmissionsPerTick;
            stage.AudienceReplyMinimumChars = current.AudienceReplyMinimumChars;
            stage.AudienceReplyMaximumChars = current.AudienceReplyMaximumChars;
            stage.AudienceReplyMinimumIntervalSeconds = current.AudienceReplyMinimumIntervalSeconds;
            stage.AudienceReplyMaximumIntervalSeconds = current.AudienceReplyMaximumIntervalSeconds;
            stage.AudienceReplyIntervalSeconds = current.AudienceReplyIntervalSeconds;
            stage.TacticalAdvanceEnabled = current.TacticalAdvanceEnabled;
            stage.TacticalAdvanceDelaySeconds = current.TacticalAdvanceDelaySeconds;
            List<string> errors = speech.Validate().Concat(performance.Validate()).Concat(stage.Validate()).ToList();
            if (errors.Count == 0)
            {
                return true;
            }
            speech.Enabled = false;
            performance.Enabled = false;
            error = string.Join("; ", errors);
            return true;
        }

        private static bool TryReadSnapshot(out Snapshot snapshot)
        {
            snapshot = null;
            object settings = ResolveDuelSettings();
            if (settings == null)
            {
                SceneActionsLog.Warning("MCM", "AF DuelSettings was not found; integrated MCM overrides are unavailable.");
                return false;
            }
            EnsureLegacyMigration(settings);
            bool correctedPersistedValues = false;
            snapshot = new Snapshot
            {
                NaturalLanguageReplyActionsEnabled = Read(settings, "NaturalLanguageReplyActionsEnabled", true),
                BattleSpeechEnabled = Read(settings, "BattleSpeechEnabled", true),
                TKeyBattleSpeechEnabled = Read(settings, "TKeyBattleSpeechEnabled", true),
                ReplyMinimumChars = Read(settings, "ReplyMinimumChars", 60),
                ReplyMaximumChars = Read(settings, "ReplyMaximumChars", 160),
                NpcPositioningEnabled = Read(settings, "NpcPositioningEnabled", true),
                FrontDistanceMeters = Read(settings, "FrontDistanceMeters", 10f),
                ArrivalRadiusMeters = Read(settings, "ArrivalRadiusMeters", 1.5f),
                MovementTimeoutSeconds = Read(settings, "MovementTimeoutSeconds", 15f),
                IncludeAlliedAudience = Read(settings, "IncludeAlliedAudience", true),
                MaximumVisualResponders = Read(settings, "MaximumVisualResponders", 60),
                VisualWaveSize = Read(settings, "VisualWaveSize", 6),
                MaximumVisualSubmissionsPerTick = Read(settings, "MaximumVisualSubmissionsPerTick", 6),
                AudienceVoicesEnabled = Read(settings, "AudienceVoicesEnabled", true),
                AudienceVoiceCount = ReadClampedInt(
                    settings,
                    "AudienceVoiceCount",
                    22,
                    0,
                    MaximumAudienceVoiceCount,
                    ref correctedPersistedValues),
                AudienceVoiceWaveSize = ReadClampedInt(
                    settings,
                    "AudienceVoiceWaveSize",
                    3,
                    1,
                    MaximumAudienceVoiceWaveSize,
                    ref correctedPersistedValues),
                AudienceVoiceWaveIntervalSeconds = Read(settings, "AudienceVoiceWaveIntervalSeconds", 0.18f),
                AudienceRepliesEnabled = Read(settings, "AudienceRepliesEnabled", true),
                AudienceReplyCount = Read(settings, "AudienceReplyCount", 24),
                AudienceReplyWaveSize = Read(settings, "AudienceReplyWaveSize", 5),
                MaximumAudienceReplySubmissionsPerTick = Read(settings, "MaximumAudienceReplySubmissionsPerTick", 8),
                AudienceReplyMinimumChars = Read(settings, "AudienceReplyMinimumChars", 8),
                AudienceReplyMaximumChars = Read(settings, "AudienceReplyMaximumChars", 24),
                AudienceReplyMinimumIntervalSeconds = Read(settings, "AudienceReplyMinimumIntervalSeconds", 0.2f),
                AudienceReplyMaximumIntervalSeconds = Read(settings, "AudienceReplyMaximumIntervalSeconds", 0.5f),
                AudienceReplyIntervalSeconds = Read(settings, "AudienceReplyIntervalSeconds", 1.1f),
                TacticalAdvanceEnabled = Read(settings, "TacticalAdvanceEnabled", true),
                TacticalAdvanceDelaySeconds = Read(settings, "TacticalAdvanceDelaySeconds", 1.8f),
                EnemyInterruptRadiusMeters = Read(settings, "EnemyInterruptRadiusMeters", 10f),
                ScreenNotifications = Read(settings, "ScreenNotifications", true),
                DiagnosticsEnabled = Read(settings, "DiagnosticsEnabled", false)
            };
            if (correctedPersistedValues)
            {
                TryPersistSettings(settings);
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_MCM",
                    "Corrected persisted battle-speech voice bounds to Core limits: " +
                    "AudienceVoiceCount=0.." + MaximumAudienceVoiceCount +
                    ", AudienceVoiceWaveSize=1.." + MaximumAudienceVoiceWaveSize + ".");
            }
            return true;
        }

        private static int ReadClampedInt(
            object settings,
            string name,
            int fallback,
            int minimum,
            int maximum,
            ref bool corrected)
        {
            int value = Read(settings, name, fallback);
            int normalized = Math.Max(minimum, Math.Min(maximum, value));
            if (normalized != value)
            {
                Write(settings, name, normalized);
                corrected = true;
            }
            return normalized;
        }

        private static object ResolveDuelSettings()
        {
            lock (AccessorSync)
            {
                EnsureAccessorLocked();
                if (_getSettingsMethod == null)
                {
                    return null;
                }
                try
                {
                    return _getSettingsMethod.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    SceneActionsLog.Warning("MCM", "Unable to read AF DuelSettings: " + ex.GetBaseException().Message);
                    return null;
                }
            }
        }

        private static void EnsureAccessorLocked()
        {
            if (_accessorSearched)
            {
                return;
            }
            _accessorSearched = true;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(DuelSettingsTypeName, false);
                if (type == null)
                {
                    continue;
                }
                MethodInfo method = type.GetMethod("GetSettings", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    continue;
                }
                _getSettingsMethod = method;
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    Properties[property.Name] = property;
                }
                return;
            }
        }

        private static T Read<T>(object settings, string name, T fallback)
        {
            lock (AccessorSync)
            {
                if (!Properties.TryGetValue(name, out PropertyInfo property) || !property.CanRead)
                {
                    return fallback;
                }
                try
                {
                    object value = property.GetValue(settings, null);
                    if (value == null)
                    {
                        return fallback;
                    }
                    if (value is T typed)
                    {
                        return typed;
                    }
                    return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                }
                catch
                {
                    return fallback;
                }
            }
        }

        private static void Write(object settings, string name, object value)
        {
            lock (AccessorSync)
            {
                if (!Properties.TryGetValue(name, out PropertyInfo property) || !property.CanWrite)
                {
                    return;
                }
                try
                {
                    object converted = value;
                    if (value != null && !property.PropertyType.IsInstanceOfType(value))
                    {
                        converted = Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
                    }
                    property.SetValue(settings, converted, null);
                }
                catch
                {
                }
            }
        }

        private static void EnsureLegacyMigration(object settings)
        {
            if (Read(settings, "SceneActionsMcmMigrationVersion", 0) >= IntegratedMigrationVersion)
            {
                return;
            }
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord", "Configs", "ModSettings", "Global",
                "AnimusForge_XihaiAction", LegacySettingsFileName);
            bool imported = false;
            if (File.Exists(path) && IsSceneActionsDefaultProfile(settings))
            {
                try
                {
                    JObject legacy = JObject.Parse(File.ReadAllText(path));
                    foreach (string name in MigratedPropertyNames)
                    {
                        if (!Properties.TryGetValue(name, out PropertyInfo property) ||
                            !legacy.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken token))
                        {
                            continue;
                        }
                        property.SetValue(settings, token.ToObject(property.PropertyType), null);
                        imported = true;
                    }
                    SceneActionsLog.Info("MCM", "Migrated legacy AnimusForge_XihaiAction MCM values into AF DuelSettings.");
                }
                catch (Exception ex)
                {
                    SceneActionsLog.Warning("MCM", "Legacy XihaiAction MCM migration failed: " + ex.GetBaseException().Message);
                }
            }
            MigrateLegacyBattleSpeechDefaults(settings);
            Write(settings, "SceneActionsMcmMigrationVersion", IntegratedMigrationVersion);
            TryPersistSettings(settings);
            if (!imported && !File.Exists(path))
            {
                SceneActionsLog.Info("MCM", "No legacy XihaiAction MCM file found; AF defaults retained.");
            }
        }

        private static bool IsSceneActionsDefaultProfile(object settings)
        {
            return Read(settings, "NaturalLanguageReplyActionsEnabled", true) &&
                Read(settings, "BattleSpeechEnabled", true) &&
                Read(settings, "ReplyMinimumChars", 60) == 60 &&
                Read(settings, "ReplyMaximumChars", 160) == 160 &&
                Math.Abs(Read(settings, "FrontDistanceMeters", 10f) - 10f) < 0.0001f &&
                Read(settings, "AudienceReplyCount", 24) == 24 &&
                Read(settings, "MaximumVisualResponders", 60) == 60 &&
                Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 1.8f) < 0.0001f &&
                Math.Abs(Read(settings, "EnemyInterruptRadiusMeters", 10f) - 10f) < 0.0001f;
        }

        private static void MigrateLegacyBattleSpeechDefaults(object settings)
        {
            bool changed = false;
            if (Read(settings, "AudienceReplyWaveDefaultsVersion", 0) < 1)
            {
                if (Read(settings, "AudienceReplyWaveSize", 5) == 2)
                {
                    Write(settings, "AudienceReplyWaveSize", 5);
                }
                Write(settings, "AudienceReplyWaveDefaultsVersion", 1);
                changed = true;
            }
            if (Read(settings, "CombatSpeechDefaultsVersion", 0) < 1)
            {
                if (Math.Abs(Read(settings, "EnemyInterruptRadiusMeters", 10f) - 35f) < 0.0001f)
                {
                    Write(settings, "EnemyInterruptRadiusMeters", 10f);
                }
                Write(settings, "CombatSpeechDefaultsVersion", 1);
                changed = true;
            }
            bool completeLegacyDefaults = Read(settings, "ReplyMinimumChars", 60) == 50 &&
                Read(settings, "ReplyMaximumChars", 160) == 100 &&
                Math.Abs(Read(settings, "FrontDistanceMeters", 10f) - 8f) < 0.0001f &&
                Read(settings, "AudienceVoiceCount", 22) == 12 &&
                Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 0.6f) < 0.0001f;
            bool previousIntegratedDefaults = Read(settings, "ReplyMinimumChars", 60) == 6 &&
                Read(settings, "ReplyMaximumChars", 160) == 30 &&
                Math.Abs(Read(settings, "FrontDistanceMeters", 10f) - 10f) < 0.0001f &&
                Read(settings, "AudienceVoiceCount", 22) == 22 &&
                (Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 0.6f) < 0.0001f ||
                 Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 1.2f) < 0.0001f);
            bool currentIntegratedDefaults = Read(settings, "ReplyMinimumChars", 60) == 20 &&
                Read(settings, "ReplyMaximumChars", 160) == 60 &&
                Math.Abs(Read(settings, "FrontDistanceMeters", 10f) - 10f) < 0.0001f &&
                Read(settings, "AudienceVoiceCount", 22) == 22 &&
                Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 1.8f) < 0.0001f &&
                Read(settings, "MaximumVisualResponders", 60) == 48 &&
                Read(settings, "AudienceReplyCount", 24) == 16 &&
                Read(settings, "AudienceReplyWaveSize", 5) == 8 &&
                Read(settings, "AudienceReplyMinimumChars", 8) == 8 &&
                Read(settings, "AudienceReplyMaximumChars", 24) == 24 &&
                Math.Abs(Read(settings, "AudienceReplyMinimumIntervalSeconds", 0.2f) - 0.1f) < 0.0001f &&
                Math.Abs(Read(settings, "AudienceReplyMaximumIntervalSeconds", 0.5f) - 0.5f) < 0.0001f;
            bool currentShortDefaults = Read(settings, "ReplyMinimumChars", 60) == 30 &&
                Read(settings, "ReplyMaximumChars", 160) == 80 &&
                Math.Abs(Read(settings, "FrontDistanceMeters", 10f) - 10f) < 0.0001f &&
                Read(settings, "AudienceVoiceCount", 22) == 22 &&
                Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 1.8f) < 0.0001f &&
                Read(settings, "MaximumVisualResponders", 60) == 60 &&
                Read(settings, "AudienceReplyCount", 24) == 24 &&
                Read(settings, "AudienceReplyWaveSize", 5) == 5 &&
                Read(settings, "AudienceReplyMinimumChars", 8) == 8 &&
                Read(settings, "AudienceReplyMaximumChars", 24) == 24 &&
                Math.Abs(Read(settings, "AudienceReplyMinimumIntervalSeconds", 0.2f) - 0.2f) < 0.0001f &&
                Math.Abs(Read(settings, "AudienceReplyMaximumIntervalSeconds", 0.5f) - 0.5f) < 0.0001f;
            bool migrateToCurrentDefaults = completeLegacyDefaults || previousIntegratedDefaults ||
                currentIntegratedDefaults || currentShortDefaults;
            if (migrateToCurrentDefaults)
            {
                Write(settings, "ReplyMinimumChars", 60);
                Write(settings, "ReplyMaximumChars", 160);
                Write(settings, "FrontDistanceMeters", 10f);
                Write(settings, "AudienceVoiceCount", 22);
                Write(settings, "TacticalAdvanceDelaySeconds", 1.8f);
                Write(settings, "MaximumVisualResponders", 60);
                Write(settings, "AudienceReplyCount", 24);
                Write(settings, "AudienceReplyWaveSize", 5);
                Write(settings, "AudienceReplyMinimumChars", 8);
                Write(settings, "AudienceReplyMaximumChars", 24);
                Write(settings, "AudienceReplyMinimumIntervalSeconds", 0.2f);
                Write(settings, "AudienceReplyMaximumIntervalSeconds", 0.5f);
                Write(settings, "AudienceReplyWaveDefaultsVersion", 2);
                changed = true;
            }
            if (Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 0.6f) < 0.0001f ||
                Math.Abs(Read(settings, "TacticalAdvanceDelaySeconds", 1.8f) - 1.2f) < 0.0001f)
            {
                Write(settings, "TacticalAdvanceDelaySeconds", 1.8f);
                changed = true;
            }
            if (!migrateToCurrentDefaults &&
                Math.Abs(Read(settings, "AudienceReplyMinimumIntervalSeconds", 0.2f) - 0.1f) < 0.0001f &&
                Math.Abs(Read(settings, "AudienceReplyMaximumIntervalSeconds", 0.5f) - 0.5f) < 0.0001f &&
                Math.Abs(Read(settings, "AudienceReplyIntervalSeconds", 1.1f) - 1.1f) > 0.0001f)
            {
                float migrated = Math.Max(0.1f, Math.Min(0.5f, Read(settings, "AudienceReplyIntervalSeconds", 1.1f)));
                Write(settings, "AudienceReplyMinimumIntervalSeconds", 0.1f);
                Write(settings, "AudienceReplyMaximumIntervalSeconds", migrated);
                changed = true;
            }
            if (changed)
            {
                SceneActionsLog.Info("BATTLE_SPEECH_MCM", "Migrated legacy battle-speech defaults in AF DuelSettings.");
            }
        }

        private static void TryPersistSettings(object settings)
        {
            try
            {
                Type providerType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("MCM.Common.BaseSettingsProvider", false))
                    .FirstOrDefault(t => t != null);
                object provider = providerType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
                if (provider == null)
                {
                    return;
                }
                MethodInfo save = provider.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "SaveSettings" && m.GetParameters().Length == 1)
                    .FirstOrDefault(m => m.GetParameters()[0].ParameterType.IsInstanceOfType(settings));
                save?.Invoke(provider, new[] { settings });
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning("MCM", "Unable to persist integrated MCM migration: " + ex.GetBaseException().Message);
            }
        }

        private sealed class Snapshot
        {
            public bool NaturalLanguageReplyActionsEnabled;
            public bool BattleSpeechEnabled;
            public bool TKeyBattleSpeechEnabled;
            public int ReplyMinimumChars;
            public int ReplyMaximumChars;
            public bool NpcPositioningEnabled;
            public float FrontDistanceMeters;
            public float ArrivalRadiusMeters;
            public float MovementTimeoutSeconds;
            public bool IncludeAlliedAudience;
            public int MaximumVisualResponders;
            public int VisualWaveSize;
            public int MaximumVisualSubmissionsPerTick;
            public bool AudienceVoicesEnabled;
            public int AudienceVoiceCount;
            public int AudienceVoiceWaveSize;
            public float AudienceVoiceWaveIntervalSeconds;
            public bool AudienceRepliesEnabled;
            public int AudienceReplyCount;
            public int AudienceReplyWaveSize;
            public int MaximumAudienceReplySubmissionsPerTick;
            public int AudienceReplyMinimumChars;
            public int AudienceReplyMaximumChars;
            public float AudienceReplyMinimumIntervalSeconds;
            public float AudienceReplyMaximumIntervalSeconds;
            public float AudienceReplyIntervalSeconds;
            public bool TacticalAdvanceEnabled;
            public float TacticalAdvanceDelaySeconds;
            public float EnemyInterruptRadiusMeters;
            public bool ScreenNotifications;
            public bool DiagnosticsEnabled;
        }
    }
}
