using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal static class SceneActionsRuntimeHost
    {
        public const string GameVersion = "v1.4.8.119303";
        public const string RuntimeBuildId =
            "bl148-cs119303-af-structural-contract-v2";
        public const int RuntimeAdapterContract = 2;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, IAuxiliaryTextClassifierV1> Classifiers =
            new Dictionary<string, IAuxiliaryTextClassifierV1>(StringComparer.Ordinal);
        private static readonly Dictionary<string, IAuxiliaryConsentClassifierV1>
            ConsentClassifiers =
                new Dictionary<string, IAuxiliaryConsentClassifierV1>(StringComparer.Ordinal);
        private static readonly Dictionary<string, IBattleSpeechClassifierV2>
            BattleSpeechClassifiers =
                new Dictionary<string, IBattleSpeechClassifierV2>(StringComparer.Ordinal);
        private static SceneActionsMissionBehavior _activeSession;
        private static bool _initialized;

        public static RuntimeIdentity Runtime { get; private set; }
        public static SceneActionSettings Settings { get; private set; }
        public static SceneActionCatalog Catalog { get; private set; }
        public static CommandParser Parser { get; private set; }
        public static ActionProviderRegistry Providers { get; private set; }
        public static string ModuleRoot { get; private set; }
        public static string CatalogHash { get; private set; }
        public static bool ConfigurationValid { get; private set; }
        public static bool IsInitialized
        {
            get
            {
                lock (Sync)
                {
                    return _initialized;
                }
            }
        }

        public static void Initialize(string moduleRoot = null)
        {
            lock (Sync)
            {
                if (_initialized)
                {
                    return;
                }

                string resolvedModuleRoot = ResolveModuleRoot(moduleRoot);
                if (string.IsNullOrWhiteSpace(resolvedModuleRoot))
                {
                    throw new InvalidOperationException("Unable to resolve module root.");
                }
                ModuleRoot = resolvedModuleRoot;
                Runtime = new RuntimeIdentity(
                    GameVersion,
                    RuntimeBuildId,
                    RuntimeAdapterContract);

                string settingsPath = Path.Combine(
                    ModuleRoot,
                    "ModuleData",
                    "SceneActions",
                    "settings.v4.json");
                SettingsLoadResult load = RuntimeSettingsLoader.Load(settingsPath);
                Settings = load.Settings;
                ConfigurationValid = load.IsValid;
                if (load.IsValid && SceneActionsMcmSettings.TryApplySceneActions(
                        Settings,
                        out string mcmError))
                {
                    if (!string.IsNullOrEmpty(mcmError))
                    {
                        ConfigurationValid = false;
                        load.Error = "MCM runtime override is invalid: " + mcmError;
                    }
                    else
                    {
                        SceneActionsLog.Info(
                            "CONFIG",
                            "Validated MCM runtime overrides applied to SceneActions.");
                    }
                }
                try
                {
                    Catalog = BuiltInContent.CreateRuntimeV4(
                        Runtime,
                        load.IsValid ? Settings.UserAliases : null);
                    ValidateOverrideKeys(Settings, Catalog);
                }
                catch (Exception ex)
                {
                    Settings = RuntimeSettingsLoader.CreateAuditedDefault();
                    Settings.Enabled = false;
                    ConfigurationValid = false;
                    Catalog = BuiltInContent.CreateRuntimeV4(Runtime);
                    load.Error = ex.Message;
                }

                Parser = new CommandParser(Catalog);
                Providers = new ActionProviderRegistry(ModuleRoot);
                CatalogHash = ComputeCatalogHash(Catalog);
                _initialized = true;

                if (ConfigurationValid)
                {
                    SceneActionsLog.Info(
                        "CONFIG",
                        load.UsedBuiltInDefault
                            ? "Settings file missing; audited built-in defaults selected."
                            : load.MigratedFromV1
                                ? "settings.v4.json missing; strict v1 settings migrated with V2/V3/V4 audit defaults: " +
                                  load.SourcePath
                                : load.MigratedFromV2
                                    ? "settings.v4.json missing; strict v2 settings migrated with V3/V4 audit defaults: " +
                                      load.SourcePath
                                    : load.MigratedFromV3
                                        ? "settings.v4.json missing; strict v3 settings migrated with V4 audit defaults: " +
                                          load.SourcePath
                                        : "Strict settings loaded: " + load.SourcePath);
                }
                else
                {
                    SceneActionsLog.Warning(
                        "CONFIG",
                        "ConfigurationInvalid; action bypass disabled: " + load.Error);
                }
                SceneActionsLog.Info(
                    "BOOT",
                    "Runtime=" + GameVersion + "/" + RuntimeBuildId +
                    " CatalogHash=" + CatalogHash +
                    " Enabled=" + Settings.Enabled);
                if (Providers.XihaiStaticReady)
                {
                    SceneActionsLog.Info("PROVIDER", "Xihai static probe passed.");
                }
                else
                {
                    SceneActionsLog.Warning(
                        "PROVIDER",
                        "Xihai provider unavailable; Native remains isolated: " +
                        Providers.XihaiStaticReason);
                }
                if (Providers.DanceStaticReady)
                {
                    SceneActionsLog.Info(
                        "PROVIDER",
                        "Warrior dance mapping probe passed (experimental, not visually validated)." );
                }
                else
                {
                    SceneActionsLog.Warning(
                        "PROVIDER",
                        "Dance provider unavailable: " + Providers.DanceStaticReason);
                }
                if (Providers.SpeechOpeningStaticReady)
                {
                    SceneActionsLog.Info(
                        "PROVIDER",
                        "Speech opening nacisword1 mapping probe passed (experimental, not visually validated)." );
                }
                else
                {
                    SceneActionsLog.Warning(
                        "PROVIDER",
                        "Speech opening provider unavailable: " +
                        Providers.SpeechOpeningStaticReason);
                }
            }
        }

        private static string ResolveModuleRoot(string requestedRoot)
        {
            if (IsModuleRoot(requestedRoot))
            {
                return Path.GetFullPath(requestedRoot);
            }

            string assemblyPath = typeof(SceneActionsRuntimeHost).Assembly.Location;
            string current = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            for (int depth = 0; depth < 8 && !string.IsNullOrWhiteSpace(current); depth++)
            {
                if (IsModuleRoot(current))
                {
                    return Path.GetFullPath(current);
                }
                current = Directory.GetParent(current)?.FullName;
            }
            return string.Empty;
        }

        private static bool IsModuleRoot(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   File.Exists(Path.Combine(path, "SubModule.xml")) &&
                   Directory.Exists(Path.Combine(path, "ModuleData"));
        }

        public static void Shutdown()
        {
            SceneActionsMissionBehavior activeSession;
            lock (Sync)
            {
                activeSession = _activeSession;
                _activeSession = null;
                Classifiers.Clear();
                ConsentClassifiers.Clear();
                BattleSpeechClassifiers.Clear();
                _initialized = false;
            }
            activeSession?.StopFromHost();
        }

        public static void BindSession(SceneActionsMissionBehavior behavior)
        {
            if (behavior == null)
            {
                return;
            }
            lock (Sync)
            {
                _activeSession = behavior;
            }
        }

        public static void UnbindSession(SceneActionsMissionBehavior behavior)
        {
            lock (Sync)
            {
                if (ReferenceEquals(_activeSession, behavior))
                {
                    _activeSession = null;
                }
            }
        }

        public static bool SubmitPlayerSceneShout(
            Guid eventId,
            Mission mission,
            string rawText,
            Agent player,
            Agent primary,
            IReadOnlyList<Agent> framedTargets,
            double submittedAtMissionTime)
        {
            return Submit(new CapturedSceneActionEvent
            {
                EventId = eventId,
                InputSource = SceneInputSource.PlayerSceneShout,
                SourceMission = mission,
                RawText = rawText,
                Player = player,
                Speaker = player,
                PrimaryTarget = primary,
                FramedTargets = framedTargets?.ToArray() ?? Array.Empty<Agent>(),
                SubmittedAtMissionTime = submittedAtMissionTime
            });
        }

        public static bool SubmitNpcReply(
            Guid replyId,
            Mission mission,
            Agent speaker,
            string semanticText,
            double submittedAtMissionTime)
        {
            return Submit(new CapturedSceneActionEvent
            {
                EventId = replyId,
                InputSource = SceneInputSource.NpcSceneShoutReply,
                SourceMission = mission,
                RawText = semanticText,
                Speaker = speaker,
                FramedTargets = Array.Empty<Agent>(),
                SubmittedAtMissionTime = submittedAtMissionTime
            });
        }

        internal static bool TrySubmitTrustedOneShot(
            Guid requestId,
            Guid ownerToken,
            Mission mission,
            Agent target,
            string intentKey,
            double submittedAtMissionTime,
            string diagnosticSource)
        {
            SceneActionsMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || !ConfigurationValid || Settings == null || !Settings.Enabled)
                {
                    return false;
                }
                session = _activeSession;
            }
            return session != null && session.TryEnqueueTrustedOneShot(
                new TrustedOneShotRequest
                {
                    RequestId = requestId,
                    OwnerToken = ownerToken,
                    Mission = mission,
                    Target = target,
                    IntentKey = intentKey,
                    SubmittedAtMissionTime = submittedAtMissionTime,
                    DiagnosticSource = diagnosticSource
                });
        }

        internal static bool TryCancelTrustedPlayback(
            Mission mission,
            Guid ownerToken,
            string reason)
        {
            SceneActionsMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || ownerToken == Guid.Empty)
                {
                    return false;
                }
                session = _activeSession;
            }
            return session != null && session.TryEnqueueTrustedCancellation(
                new TrustedPlaybackCancellation
                {
                    Mission = mission,
                    OwnerToken = ownerToken,
                    Reason = reason
                });
        }

        private static bool Submit(CapturedSceneActionEvent captured)
        {
            SceneActionsMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || !Settings.Enabled)
                {
                    return false;
                }
                session = _activeSession;
            }
            return session != null && session.TryEnqueueCapturedEvent(captured);
        }

        public static IDisposable RegisterClassifier(
            string providerId,
            IAuxiliaryTextClassifierV1 classifier)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new ArgumentException("Provider id is required.", nameof(providerId));
            }
            if (classifier == null)
            {
                throw new ArgumentNullException(nameof(classifier));
            }
            lock (Sync)
            {
                IAuxiliaryConsentClassifierV1 consentClassifier =
                    classifier as IAuxiliaryConsentClassifierV1;
                IBattleSpeechClassifierV2 battleSpeechClassifier =
                    classifier as IBattleSpeechClassifierV2;
                if (Classifiers.ContainsKey(providerId) ||
                    (consentClassifier != null && ConsentClassifiers.ContainsKey(providerId)) ||
                    (battleSpeechClassifier != null &&
                     BattleSpeechClassifiers.ContainsKey(providerId)))
                {
                    throw new InvalidOperationException(
                        "Classifier provider is already registered: " + providerId);
                }
                Classifiers.Add(providerId, classifier);
                if (consentClassifier != null)
                {
                    ConsentClassifiers.Add(providerId, consentClassifier);
                }
                if (battleSpeechClassifier != null)
                {
                    BattleSpeechClassifiers.Add(providerId, battleSpeechClassifier);
                }
            }
            return new ClassifierRegistration(providerId, classifier);
        }

        public static bool TryGetClassifier(
            string providerId,
            out IAuxiliaryTextClassifierV1 classifier)
        {
            lock (Sync)
            {
                return Classifiers.TryGetValue(providerId ?? string.Empty, out classifier);
            }
        }

        public static bool TryGetConsentClassifier(
            string providerId,
            out IAuxiliaryConsentClassifierV1 classifier)
        {
            lock (Sync)
            {
                return ConsentClassifiers.TryGetValue(
                    providerId ?? string.Empty,
                    out classifier);
            }
        }

        public static bool TryGetBattleSpeechClassifier(
            string providerId,
            out IBattleSpeechClassifierV2 classifier)
        {
            lock (Sync)
            {
                return BattleSpeechClassifiers.TryGetValue(
                    providerId ?? string.Empty,
                    out classifier);
            }
        }

        private static void UnregisterClassifier(
            string providerId,
            IAuxiliaryTextClassifierV1 classifier)
        {
            lock (Sync)
            {
                if (Classifiers.TryGetValue(providerId, out IAuxiliaryTextClassifierV1 current) &&
                    ReferenceEquals(current, classifier))
                {
                    Classifiers.Remove(providerId);
                }
                if (ConsentClassifiers.TryGetValue(
                        providerId,
                        out IAuxiliaryConsentClassifierV1 currentConsent) &&
                    ReferenceEquals(currentConsent, classifier))
                {
                    ConsentClassifiers.Remove(providerId);
                }
                if (BattleSpeechClassifiers.TryGetValue(
                        providerId,
                        out IBattleSpeechClassifierV2 currentBattleSpeech) &&
                    ReferenceEquals(currentBattleSpeech, classifier))
                {
                    BattleSpeechClassifiers.Remove(providerId);
                }
            }
        }

        private static void ValidateOverrideKeys(
            SceneActionSettings settings,
            SceneActionCatalog catalog)
        {
            if (settings?.ActionOverrides == null)
            {
                return;
            }
            string unknown = settings.ActionOverrides.Keys.FirstOrDefault(
                key => !catalog.Actions.ContainsKey(key));
            if (unknown != null)
            {
                throw new InvalidDataException(
                    "actionOverrides references an unknown action: " + unknown);
            }
        }

        private static string ComputeCatalogHash(SceneActionCatalog catalog)
        {
            StringBuilder canonical = new StringBuilder();
            foreach (ActionDefinition action in catalog.Actions.Values.OrderBy(a => a.Key))
            {
                canonical.Append(action.Key).Append('|').Append(action.ProviderId)
                    .Append('|').Append(action.Mode).Append('|').Append(action.StateTag)
                    .AppendLine();
                foreach (ActionVariant variant in action.RuntimeVariants.OrderBy(v => v.Id))
                {
                    canonical.Append(variant.Id).Append('|')
                        .Append(variant.GameVersionEquals).Append('|')
                        .Append(variant.RuntimeBuildId).Append('|')
                        .Append(variant.RuntimeAdapterContract).Append('|')
                        .Append(variant.ReleaseStage).Append('|')
                        .Append(string.Join(",", variant.ActionIds ?? Array.Empty<string>()))
                        .Append('|').Append(variant.EnterActionId)
                        .Append('|').Append(variant.HoldActionId)
                        .Append('|').Append(variant.ExitActionId).AppendLine();
                }
            }
            foreach (IntentDefinition intent in catalog.Intents.Values.OrderBy(i => i.Key))
            {
                canonical.Append(intent.Key).Append('|').Append(intent.Kind)
                    .Append('|').Append(intent.ActionKey).Append('|')
                    .Append(intent.DefaultTargetMode).AppendLine();
            }
            foreach (KeyValuePair<string, AliasDefinition> alias in
                     catalog.ForceAliases.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                canonical.Append("force|").Append(alias.Key).Append('|')
                    .Append(alias.Value.IntentKey).Append('|')
                    .Append(alias.Value.TargetOverride).AppendLine();
            }
            foreach (KeyValuePair<string, AliasDefinition> alias in
                     catalog.ExactAliases.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                canonical.Append("exact|").Append(alias.Key).Append('|')
                    .Append(alias.Value.IntentKey).Append('|')
                    .Append(alias.Value.TargetOverride).AppendLine();
            }
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private sealed class ClassifierRegistration : IDisposable
        {
            private readonly string _providerId;
            private IAuxiliaryTextClassifierV1 _classifier;

            public ClassifierRegistration(
                string providerId,
                IAuxiliaryTextClassifierV1 classifier)
            {
                _providerId = providerId;
                _classifier = classifier;
            }

            public void Dispose()
            {
                IAuxiliaryTextClassifierV1 classifier = _classifier;
                if (classifier == null)
                {
                    return;
                }
                _classifier = null;
                UnregisterClassifier(_providerId, classifier);
            }
        }
    }
}
