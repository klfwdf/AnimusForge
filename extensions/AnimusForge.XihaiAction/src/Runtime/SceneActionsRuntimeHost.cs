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
        private const double McmRefreshIntervalSeconds = 1d;
        private static SceneActionsMissionBehavior _activeSession;
        private static bool _initialized;
        // Keep the immutable settings-file result separate from the current
        // MCM overlay result.  An invalid live override must fail closed for
        // the current sample, but it must not make a later corrected MCM value
        // unrecoverable for the rest of the process.
        private static bool _sourceConfigurationValid;
        private static double _nextMcmRefreshMissionTime;
        private static bool _mcmRefreshWarningLogged;

        public static RuntimeIdentity Runtime { get; private set; }
        public static SceneActionSettings Settings { get; private set; }
        public static SceneActionCatalog Catalog { get; private set; }
        public static CommandParser Parser { get; private set; }
        public static ActionProviderRegistry Providers { get; private set; }
        public static string ModuleRoot { get; private set; }
        public static string CatalogHash { get; private set; }
        public static bool ConfigurationValid { get; private set; }

        /// <summary>
        /// Small, non-command prompt hint used by AF's native AI conversation.
        /// The model still writes ordinary dialogue; it may include a natural
        /// stage direction only when the story actually makes the action happen.
        /// The closed-set parser consumes that prose after the reply and never
        /// accepts native act_* ids from user/model text.
        /// </summary>
        internal static string BuildNativeConversationActionInstruction()
        {
            lock (Sync)
            {
                if (!_initialized || !ConfigurationValid || Settings == null || !Settings.Enabled ||
                    !Settings.NpcSceneShoutReplyEnabled)
                {
                    return string.Empty;
                }
            }
            return "【自然语言动作】这是普通角色对话，不是动作菜单。只有当角色确实在当前情境中做了动作时，" +
                   "才在自然回复中简短写出已发生的身体描写，例如‘他慢慢跪下’、‘她抬手指向山口’、" +
                   "‘他抱臂摇头’；不要为了触发动作而凭空添加动作，不要输出动作键、act_*、标签、括号说明或" +
                   "动作清单。纯粹说‘我同意/我不知道/我保证’不等于动作；否定、假设、引用和计划中的动作不要写成已发生。" +
                   "回复仍应先自然回答玩家，动作描写只在语义确实发生时出现。";
        }
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
                _sourceConfigurationValid = load.IsValid;
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
                    _sourceConfigurationValid = false;
                    Catalog = BuiltInContent.CreateRuntimeV4(Runtime);
                    load.Error = ex.Message;
                }

                Parser = new CommandParser(Catalog);
                Providers = new ActionProviderRegistry(ModuleRoot);
                CatalogHash = ComputeCatalogHash(Catalog);
                _nextMcmRefreshMissionTime = 0d;
                _mcmRefreshWarningLogged = false;
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
                _sourceConfigurationValid = false;
                _nextMcmRefreshMissionTime = 0d;
                _mcmRefreshWarningLogged = false;
            }
            activeSession?.StopFromHost();
        }

        /// <summary>
        /// Re-reads only the integrated SceneActions MCM values.  BattleSpeech
        /// has a separate refresh path and is deliberately not changed here.
        /// This is called at mission setup and at a low frequency while a
        /// mission is running, never from the per-request hot path.
        /// </summary>
        internal static bool RefreshMcmOverrides(out string error)
        {
            SceneActionsMissionBehavior session = null;
            bool disableNaturalActions = false;
            bool previousEnabled;
            bool currentEnabled;
            bool invalidOverride = false;
            lock (Sync)
            {
                error = null;
                if (!_initialized || Settings == null)
                {
                    error = "SceneActions runtime is not initialized.";
                    return false;
                }
                if (!_sourceConfigurationValid)
                {
                    error = "SceneActions source configuration is invalid.";
                    return false;
                }

                previousEnabled = Settings.Enabled;
                if (!SceneActionsMcmSettings.TryApplySceneActions(
                        Settings,
                        out string mcmError))
                {
                    error = "Integrated AF MCM is not available yet.";
                    return false;
                }
                if (!string.IsNullOrEmpty(mcmError))
                {
                    Settings.Enabled = false;
                    ConfigurationValid = false;
                    error = "MCM runtime override is invalid: " + mcmError;
                    currentEnabled = false;
                    invalidOverride = true;
                }
                else
                {
                    // A corrected MCM snapshot can recover a previous
                    // fail-closed override as long as the source JSON itself
                    // remains valid.
                    ConfigurationValid = true;
                    currentEnabled = Settings.Enabled;
                }
                disableNaturalActions = previousEnabled && !currentEnabled;
                session = _activeSession;
            }

            if (disableNaturalActions && session != null)
            {
                session.DisableNaturalLanguageFromHost(
                    "Natural-language reply actions disabled in MCM.");
            }
            if (previousEnabled != currentEnabled)
            {
                SceneActionsLog.Info(
                    "MCM",
                    "NaturalLanguageReplyActionsEnabled=" + currentEnabled +
                    " (BattleSpeech is refreshed independently)." );
            }
            return !invalidOverride;
        }

        internal static void RefreshMcmOverridesIfDue(double missionTime)
        {
            bool due;
            lock (Sync)
            {
                if (!_initialized || double.IsNaN(missionTime) ||
                    missionTime < _nextMcmRefreshMissionTime)
                {
                    return;
                }
                _nextMcmRefreshMissionTime = missionTime + McmRefreshIntervalSeconds;
                due = true;
            }
            string error = null;
            bool refreshed = due && RefreshMcmOverrides(out error);
            if (!refreshed)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    lock (Sync)
                    {
                        if (!_mcmRefreshWarningLogged)
                        {
                            _mcmRefreshWarningLogged = true;
                            SceneActionsLog.Warning("MCM", error);
                        }
                    }
                }
                return;
            }
            lock (Sync)
            {
                _mcmRefreshWarningLogged = false;
            }
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
                // Mission.CurrentTime starts again at zero for every Mission.
                // Reset the sampled-MCM deadline here so a long previous Mission
                // cannot postpone the first live refresh of the new one.
                _nextMcmRefreshMissionTime = 0d;
                _mcmRefreshWarningLogged = false;
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
            // Trusted requests are produced by BattleSpeech, not by ordinary
            // SceneActions input.  Check its own gate before taking the
            // SceneActions lock; keeping this order avoids a cross-host lock
            // inversion with BattleSpeech's classifier/claim paths.
            if (!BattleSpeechRuntimeHost.IsPerformanceEnabled)
            {
                return false;
            }
            SceneActionsMissionBehavior session;
            lock (Sync)
            {
                // The ordinary natural-language toggle is deliberately not
                // consulted here: a valid SceneActions MCM=false snapshot still
                // leaves ConfigurationValid=true, so trusted BattleSpeech work
                // remains independent.  A genuinely invalid overlay/source,
                // however, must fail closed because the shared catalog/settings
                // substrate may be unsafe.
                if (!_initialized || !ConfigurationValid || Settings == null)
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
                if (!_initialized || !ConfigurationValid || Settings == null ||
                    !Settings.Enabled)
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
