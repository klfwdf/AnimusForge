using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal static class BattleSpeechRuntimeHost
    {
        private static readonly object Sync = new object();
        private static BattleSpeechMissionBehavior _activeSession;
        private static BattleSpeechPerformanceMissionBehavior _performanceEffect;
        private static BattleSpeechReplyClaimV2 _replyClaim;
        private static DeferredNpcReplyV2 _deferredReply;
        private static Mission _performanceMission;
        private static bool _performanceActive;
        private static bool _initialized;
        private static bool _sourceConfigurationValid;
        private static bool _sourcePerformanceConfigurationValid;

        public static BattleSpeechSettingsV1 Settings { get; private set; }
        public static bool ConfigurationValid { get; private set; }
        public static BattleSpeechPerformanceSettingsV1 PerformanceSettings { get; private set; }
        public static bool PerformanceConfigurationValid { get; private set; }
        public static BattleSpeechStageSettingsV2 StageSettings { get; private set; }
        public static string ModuleRoot { get; private set; }
        public static bool IsInitialized
        {
            get { lock (Sync) return _initialized; }
        }

        public static void Initialize(string moduleRoot)
        {
            lock (Sync)
            {
                if (_initialized) return;
                ModuleRoot = moduleRoot;
                Settings = BattleSpeechSettingsLoader.Load(
                    moduleRoot,
                    out bool valid,
                    out string reason);
                ConfigurationValid = valid;
                _sourceConfigurationValid = valid;
                PerformanceSettings = BattleSpeechPerformanceSettingsLoader.Load(
                    moduleRoot,
                    out bool performanceValid,
                    out string performanceReason);
                PerformanceConfigurationValid = performanceValid;
                _sourcePerformanceConfigurationValid = performanceValid;
                StageSettings = new BattleSpeechStageSettingsV2();
                if (valid && performanceValid &&
                    SceneActionsMcmSettings.TryApplyBattleSpeech(
                        Settings,
                        PerformanceSettings,
                        StageSettings,
                        out string mcmError))
                {
                    if (!string.IsNullOrEmpty(mcmError))
                    {
                        valid = false;
                        performanceValid = false;
                        reason = "MCM runtime override is invalid: " + mcmError;
                        performanceReason = reason;
                    }
                    else
                    {
                        SceneActionsLog.Info(
                            "BATTLE_SPEECH",
                            "Validated MCM runtime overrides applied to battle speech.");
                    }
                }
                ConfigurationValid = valid;
                PerformanceConfigurationValid = performanceValid;
                _initialized = true;
                SceneActionsLog.Info(
                    "BATTLE_SPEECH",
                    "Framework V1 initialized. Enabled=" + Settings.Enabled + ". " + reason);
                SceneActionsLog.Info(
                    "BATTLE_SPEECH",
                    "Performance V1 initialized. Enabled=" + PerformanceSettings.Enabled +
                    " Valid=" + performanceValid + ". " + performanceReason);
            }
        }

        internal static bool RefreshMcmOverrides(out string error)
        {
            lock (Sync)
            {
                error = null;
                if (!_initialized || Settings == null || PerformanceSettings == null ||
                    StageSettings == null)
                {
                    error = "Battle speech runtime is not initialized.";
                    return false;
                }
                if (!_sourceConfigurationValid || !_sourcePerformanceConfigurationValid)
                {
                    error = "Battle speech source configuration is invalid.";
                    return false;
                }
                if (!SceneActionsMcmSettings.TryApplyBattleSpeech(
                        Settings,
                        PerformanceSettings,
                        StageSettings,
                        out string mcmError))
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(mcmError))
                {
                    ConfigurationValid = false;
                    PerformanceConfigurationValid = false;
                    error = "MCM runtime override is invalid: " + mcmError;
                    return false;
                }
                ConfigurationValid = true;
                PerformanceConfigurationValid = true;
                return true;
            }
        }

        public static void Shutdown()
        {
            BattleSpeechMissionBehavior session;
            lock (Sync)
            {
                session = _activeSession;
                _activeSession = null;
                _performanceEffect = null;
                _replyClaim = null;
                _deferredReply = null;
                _initialized = false;
                _sourceConfigurationValid = false;
                _sourcePerformanceConfigurationValid = false;
            }
            session?.CloseFromHost("Battle speech host shutdown.");
        }

        public static void BindSession(BattleSpeechMissionBehavior session)
        {
            if (session == null) return;
            BattleSpeechMissionBehavior previous;
            lock (Sync)
            {
                if (ReferenceEquals(_activeSession, session)) return;
                previous = _activeSession;
                _activeSession = session;
            }
            previous?.CloseFromHost("Replaced by a newer Mission session.");
        }

        public static void UnbindSession(BattleSpeechMissionBehavior session)
        {
            lock (Sync)
            {
                if (ReferenceEquals(_activeSession, session)) _activeSession = null;
            }
        }

        internal static void BindPerformanceEffect(
            BattleSpeechPerformanceMissionBehavior effect)
        {
            lock (Sync)
            {
                _performanceEffect = effect;
            }
        }

        internal static void UnbindPerformanceEffect(
            BattleSpeechPerformanceMissionBehavior effect)
        {
            lock (Sync)
            {
                if (ReferenceEquals(_performanceEffect, effect))
                {
                    _performanceEffect = null;
                }
            }
        }

        internal static bool TryForceStopPerformanceForPlayerCommand(
            Mission mission,
            string commandText)
        {
            BattleSpeechPerformanceMissionBehavior effect;
            lock (Sync)
            {
                effect = _performanceEffect;
            }
            return effect != null && effect.TryForceStopForPlayerCommand(
                mission,
                commandText);
        }

        internal static void MarkPerformanceStarted(Mission mission)
        {
            lock (Sync)
            {
                _performanceMission = mission;
                _performanceActive = mission != null;
            }
        }

        internal static void MarkPerformanceEnded(Mission mission)
        {
            lock (Sync)
            {
                if (mission == null || ReferenceEquals(_performanceMission, mission))
                {
                    _performanceMission = null;
                    _performanceActive = false;
                }
            }
        }

        internal static bool IsPerformanceActive(Mission mission)
        {
            lock (Sync)
            {
                return _performanceActive && mission != null &&
                       ReferenceEquals(_performanceMission, mission);
            }
        }

        public static bool SubmitPlayerShout(
            Mission mission,
            string rawText,
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets,
            int conversationEpoch,
            double now,
            bool force = false)
        {
            lock (Sync)
            {
                if (!_initialized || Settings == null || !Settings.TKeyEnabled)
                {
                    return false;
                }
            }
            return Submit(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.PlayerShout,
                Mission = mission,
                RawText = rawText,
                Player = player,
                PrimaryTarget = primaryTarget,
                FramedTargets = (framedTargets ?? Array.Empty<Agent>()).ToArray(),
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now,
                Force = force
            });
        }

        internal static bool SubmitDedicatedSpeech(
            Mission mission,
            string rawText,
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets,
            int conversationEpoch,
            double now)
        {
            return Submit(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.PlayerShout,
                DedicatedSpeechEntry = true,
                Mission = mission,
                RawText = rawText,
                Player = player,
                PrimaryTarget = primaryTarget,
                FramedTargets = (framedTargets ?? Array.Empty<Agent>()).ToArray(),
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now,
                Force = true
            });
        }

        internal static bool SubmitDedicatedNpcSpeech(
            Mission mission,
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets,
            int conversationEpoch,
            double now)
        {
            if (primaryTarget == null || !primaryTarget.IsActive())
            {
                return false;
            }
            return Submit(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.PlayerShout,
                DedicatedSpeechEntry = true,
                Mission = mission,
                RawText = "你演讲：",
                Player = player,
                PrimaryTarget = primaryTarget,
                FramedTargets = (framedTargets ?? Array.Empty<Agent>()).ToArray(),
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now,
                Force = true
            });
        }

        internal static bool CanOpenSpeechMenu(Mission mission)
        {
            BattleSpeechMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || !ConfigurationValid || Settings == null ||
                    !Settings.Enabled || mission == null ||
                    !ReferenceEquals(_activeSession?.Mission, mission))
                {
                    return false;
                }
                session = _activeSession;
            }
            return session != null && session.CanOpenSpeechMenu;
        }

        internal static bool TryOpenSpeechInputFromShoutMenu(
            Mission mission,
            bool npcSpeech,
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets,
            int conversationEpoch)
        {
            BattleSpeechMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || !ConfigurationValid || Settings == null ||
                    !Settings.Enabled || mission == null ||
                    !ReferenceEquals(_activeSession?.Mission, mission))
                {
                    return false;
                }
                session = _activeSession;
            }
            return session != null && session.TryOpenSpeechInputFromShoutMenu(
                npcSpeech,
                player,
                primaryTarget,
                framedTargets,
                conversationEpoch);
        }

        internal static bool SubmitGeneratedNpcReply(
            Mission mission,
            Guid sessionId,
            Agent speaker,
            string replyText,
            object afBehavior,
            object afNpcPacket,
            int conversationEpoch,
            double now)
        {
            return Submit(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.GeneratedNpcReply,
                SessionId = sessionId,
                Mission = mission,
                RawText = replyText,
                Speaker = speaker,
                AfBehavior = afBehavior,
                AfNpcPacket = afNpcPacket,
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now
            });
        }

        internal static bool TryPreRouteForcedPlayerShout(
            Mission mission,
            string rawText,
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets,
            int conversationEpoch,
            double now,
            out bool allowOriginalAfGeneration)
        {
            allowOriginalAfGeneration = false;
            BattleSpeechTriggerDecisionV2 decision =
                BattleSpeechFrameworkV2.ParsePlayerShout(rawText);
            if (!decision.Force)
            {
                return false;
            }

            BattleSpeechMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || !ConfigurationValid || Settings == null ||
                    !Settings.Enabled || !Settings.TKeyEnabled)
                {
                    return false;
                }
                session = _activeSession;
            }
            if (session == null)
            {
                return false;
            }

            // NPC commands use AF once to generate a new speech body from the
            // text after the colon. Claim the route here so SceneActions does not
            // also interpret the same shout, but leave AF's original generation
            // pipeline enabled. ObserveRecordedPlayerMessage will create the
            // reply claim before AF starts the group response.
            if (decision.Kind == BattleSpeechTriggerKindV2.RequestNpcSpeech)
            {
                allowOriginalAfGeneration = true;
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Forced NPC speech claimed for AF-generated body. " +
                    "The colon suffix is treated as a topic/request.");
                return true;
            }

            bool queued = session.TryEnqueue(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.PlayerShout,
                Mission = mission,
                RawText = rawText,
                Player = player,
                PrimaryTarget = primaryTarget,
                FramedTargets = (framedTargets ?? Array.Empty<Agent>()).ToArray(),
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now,
                Force = true
            });
            if (queued)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Forced speech claimed before AF ordinary scene route. " +
                    "Speaker=" +
                    (decision.Kind == BattleSpeechTriggerKindV2.RequestNpcSpeech
                        ? "framed-primary"
                        : "player"));
            }
            return queued;
        }

        /// <summary>
        /// Claims a non-forced T-key speech candidate before AF's original
        /// OnShoutConfirmedWithContext chain starts. Explicit NPC requests are
        /// deliberately left on AF's original generation path so AF produces one
        /// fresh body; the recorded-message observer will bind that reply to the
        /// speech session. Player bodies and classifier candidates are held until
        /// the Mission tick resolves them, preventing an ordinary AF reply from
        /// racing the lightweight channel decision.
        /// </summary>
        internal static bool TryPreRouteNaturalPlayerShout(
            object afBehavior,
            Mission mission,
            string rawText,
            string extraFact,
            int? forcedPrimaryAgentIndex,
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets,
            int conversationEpoch,
            double now,
            out bool allowOriginalAfGeneration)
        {
            allowOriginalAfGeneration = false;
            BattleSpeechTriggerDecisionV2 decision =
                BattleSpeechFrameworkV2.ParsePlayerShout(rawText);
            if (decision.Force ||
                (decision.Kind != BattleSpeechTriggerKindV2.DeliverPlayerSpeech &&
                 decision.Kind != BattleSpeechTriggerKindV2.ArmPlayerSpeech &&
                 decision.Kind != BattleSpeechTriggerKindV2.Cancel &&
                 decision.Kind != BattleSpeechTriggerKindV2.NeedsClassifier &&
                 decision.Kind != BattleSpeechTriggerKindV2.RequestNpcSpeech))
            {
                return false;
            }

            BattleSpeechMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || !ConfigurationValid || Settings == null ||
                    !Settings.Enabled || !Settings.TKeyEnabled || StageSettings == null ||
                    !StageSettings.NaturalTriggerEnabled)
                {
                    return false;
                }
                session = _activeSession;
            }
            if (session == null)
            {
                return false;
            }
            if (decision.Kind == BattleSpeechTriggerKindV2.NeedsClassifier &&
                (!StageSettings.TriggerClassifierEnabled ||
                 !SceneActionsRuntimeHost.TryGetBattleSpeechClassifier(
                     StageSettings.ClassifierProviderId,
                     out IBattleSpeechClassifierV2 _)))
            {
                return false;
            }

            // A locally certain NPC request still needs AF's own response body.
            // Claiming it here prevents SceneActions from interpreting the same
            // text, while returning true lets the original AF generation run once.
            if (decision.Kind == BattleSpeechTriggerKindV2.RequestNpcSpeech)
            {
                if (primaryTarget == null || !primaryTarget.IsActive())
                {
                    return false;
                }
                allowOriginalAfGeneration = true;
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Natural NPC speech claimed for AF-generated body. " +
                    "The original AF chain will publish exactly one reply.");
                return true;
            }

            bool queued = session.TryEnqueue(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.PlayerShout,
                Mission = mission,
                RawText = rawText,
                Player = player,
                PrimaryTarget = primaryTarget,
                FramedTargets = (framedTargets ?? Array.Empty<Agent>()).ToArray(),
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now,
                Force = decision.Force,
                OriginalAfBehavior = afBehavior,
                OriginalExtraFact = extraFact,
                OriginalForcedPrimaryAgentIndex = forcedPrimaryAgentIndex
            });
            if (queued)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Natural speech claimed before AF ordinary route. Kind=" +
                    decision.Kind + " Classifier=" +
                    (decision.Kind == BattleSpeechTriggerKindV2.NeedsClassifier));
            }
            return queued;
        }

        public static bool SubmitQueuedNpcReplyCandidate(
            Mission mission,
            Agent speaker,
            string replyText,
            int conversationEpoch,
            string playerDirectedActionText,
            string playerDirectedNpcReplyText,
            double now)
        {
            replyText = NormalizeClaimedNpcSpeech(speaker, replyText);
            playerDirectedNpcReplyText = NormalizeClaimedNpcSpeech(
                speaker,
                playerDirectedNpcReplyText);
            TryBindQueuedReplyClaim(
                mission,
                speaker,
                replyText,
                conversationEpoch,
                playerDirectedActionText,
                playerDirectedNpcReplyText,
                now);
            return Submit(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.QueuedNpcReplyCandidate,
                Mission = mission,
                RawText = replyText,
                Speaker = speaker,
                ConversationEpoch = conversationEpoch,
                PlayerDirectedActionText = playerDirectedActionText,
                PlayerDirectedNpcReplyText = playerDirectedNpcReplyText,
                SubmittedAtMissionTime = now
            });
        }

        public static bool SubmitShownNpcReply(
            Mission mission,
            Agent speaker,
            string replyText,
            int conversationEpoch,
            double now)
        {
            replyText = NormalizeClaimedNpcSpeech(speaker, replyText);
            return Submit(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.ShownNpcReply,
                Mission = mission,
                RawText = replyText,
                Speaker = speaker,
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now
            });
        }

        private static bool Submit(BattleSpeechCapturedInputV1 input)
        {
            BattleSpeechMissionBehavior session;
            lock (Sync)
            {
                if (!_initialized || !ConfigurationValid || Settings == null || !Settings.Enabled)
                {
                    return false;
                }
                session = _activeSession;
            }
            return session != null && session.TryEnqueue(input);
        }

        internal static void PublishNpcReplyClaim(
            Guid sessionId,
            Mission mission,
            Agent speaker,
            int conversationEpoch,
            string requestText,
            double expiresAtMissionTime)
        {
            if (sessionId == Guid.Empty || mission == null || speaker == null)
            {
                return;
            }
            lock (Sync)
            {
                _replyClaim = new BattleSpeechReplyClaimV2
                {
                    SessionId = sessionId,
                    Mission = mission,
                    SpeakerAgentIndex = speaker.Index,
                    SpeakerName = speaker.Name ?? string.Empty,
                    ConversationEpoch = conversationEpoch,
                    RequestText = requestText ?? string.Empty,
                    ExpiresAtMissionTime = expiresAtMissionTime
                };
                _deferredReply = null;
            }
        }

        internal static bool QueueGeneratedNpcReply(
            Guid sessionId,
            Mission mission,
            Agent speaker,
            string content,
            object behavior,
            object npcPacket)
        {
            if (sessionId == Guid.Empty || mission == null || speaker == null ||
                string.IsNullOrWhiteSpace(content) || behavior == null || npcPacket == null)
            {
                return false;
            }
            lock (Sync)
            {
                if (_replyClaim == null || _replyClaim.SessionId != sessionId ||
                    !ReferenceEquals(_replyClaim.Mission, mission) ||
                    _replyClaim.SpeakerAgentIndex != speaker.Index)
                {
                    return false;
                }
                string normalized = NormalizeClaimedNpcSpeechUnsafe(speaker, content);
                _replyClaim.ExpectedReplyFingerprint =
                    BattleSpeechReplyBindingV1.Fingerprint(normalized);
                _replyClaim.DeferredReplyFingerprint = null;
                _deferredReply = new DeferredNpcReplyV2
                {
                    SessionId = sessionId,
                    Behavior = behavior,
                    NpcPacket = npcPacket,
                    Speaker = speaker,
                    Content = normalized,
                    AllowTts = true,
                    AttachTtsToSceneAgent = true,
                    SuppressInteractionTimeoutArm = true
                };
                return true;
            }
        }

        internal static void ClearNpcReplyClaim(Guid sessionId)
        {
            lock (Sync)
            {
                if (_replyClaim != null && _replyClaim.SessionId == sessionId)
                {
                    _replyClaim = null;
                    _deferredReply = null;
                }
            }
        }

        internal static bool TryGetReplyLengthOverride(
            string npcName,
            out int minimumChars,
            out int maximumChars)
        {
            lock (Sync)
            {
                minimumChars = 0;
                maximumChars = 0;
                if (!_initialized || _replyClaim == null || StageSettings == null)
                {
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(npcName) &&
                    !string.IsNullOrWhiteSpace(_replyClaim.SpeakerName) &&
                    npcName.IndexOf(_replyClaim.SpeakerName, StringComparison.OrdinalIgnoreCase) < 0 &&
                    _replyClaim.SpeakerName.IndexOf(npcName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
                minimumChars = StageSettings.ReplyMinimumChars;
                maximumChars = StageSettings.ReplyMaximumChars;
                return true;
            }
        }

        internal static bool TryGetReplyPromptInstruction(
            string npcName,
            out string instruction)
        {
            instruction = null;
            if (!TryGetReplyLengthOverride(
                    npcName,
                    out int minimumChars,
                    out int maximumChars))
            {
                return false;
            }
            instruction = BattleSpeechFrameworkV2.BuildNpcSpeechPromptInstruction(
                minimumChars,
                maximumChars);
            return true;
        }

        internal static bool TryDeferShownNpcReply(
            object behavior,
            object npcPacket,
            Agent speaker,
            string content,
            bool allowTts,
            bool attachTtsToSceneAgent,
            bool suppressInteractionTimeoutArm,
            int conversationEpoch,
            out bool duplicateSuppressed)
        {
            duplicateSuppressed = false;
            lock (Sync)
            {
                if (!_initialized || _replyClaim == null ||
                    speaker == null || string.IsNullOrWhiteSpace(content) ||
                    !ReferenceEquals(_replyClaim.Mission, Mission.Current) ||
                    speaker.Index != _replyClaim.SpeakerAgentIndex ||
                    (_replyClaim.ConversationEpoch != 0 && conversationEpoch != 0 &&
                     _replyClaim.ConversationEpoch != conversationEpoch))
                {
                    return false;
                }
                string normalizedContent = NormalizeClaimedNpcSpeechUnsafe(speaker, content);
                string fingerprint = BattleSpeechReplyBindingV1.Fingerprint(normalizedContent);
                if (!string.IsNullOrEmpty(_replyClaim.ExpectedReplyFingerprint) &&
                    !string.Equals(
                        _replyClaim.ExpectedReplyFingerprint,
                        fingerprint,
                        StringComparison.Ordinal))
                {
                    return false;
                }
                if (string.Equals(
                    _replyClaim.DeferredReplyFingerprint,
                    fingerprint,
                    StringComparison.Ordinal))
                {
                    duplicateSuppressed = true;
                    return true;
                }
                if (_deferredReply != null)
                {
                    return false;
                }
                _replyClaim.DeferredReplyFingerprint = fingerprint;
                _deferredReply = new DeferredNpcReplyV2
                {
                    SessionId = _replyClaim.SessionId,
                    Behavior = behavior,
                    NpcPacket = npcPacket,
                    Speaker = speaker,
                    Content = normalizedContent,
                    AllowTts = allowTts,
                    AttachTtsToSceneAgent = attachTtsToSceneAgent,
                    SuppressInteractionTimeoutArm = suppressInteractionTimeoutArm
                };
                return true;
            }
        }

        internal static bool IsClaimedSpeechSpeaker(Agent agent)
        {
            lock (Sync)
            {
                return agent != null && _replyClaim != null &&
                       ReferenceEquals(_replyClaim.Mission, agent.Mission) &&
                       _replyClaim.SpeakerAgentIndex == agent.Index;
            }
        }

        private static string NormalizeClaimedNpcSpeech(Agent speaker, string content)
        {
            lock (Sync)
            {
                return NormalizeClaimedNpcSpeechUnsafe(speaker, content);
            }
        }

        private static string NormalizeClaimedNpcSpeechUnsafe(Agent speaker, string content)
        {
            if (speaker == null || _replyClaim == null || StageSettings == null ||
                !ReferenceEquals(_replyClaim.Mission, speaker.Mission) ||
                _replyClaim.SpeakerAgentIndex != speaker.Index)
            {
                return content;
            }
            return BattleSpeechFrameworkV2.NormalizeNpcSpeechReply(
                content,
                StageSettings.ReplyMinimumChars,
                StageSettings.ReplyMaximumChars);
        }

        internal static bool TryTakeDeferredNpcReply(
            Guid sessionId,
            out DeferredNpcReplyV2 deferred)
        {
            lock (Sync)
            {
                deferred = null;
                if (_deferredReply == null || _deferredReply.SessionId != sessionId)
                {
                    return false;
                }
                deferred = _deferredReply;
                _deferredReply = null;
                return true;
            }
        }

        internal static bool IsClaimedNpcReply(
            Mission mission,
            Agent speaker,
            string content)
        {
            lock (Sync)
            {
                if (_replyClaim == null || mission == null || speaker == null ||
                    !ReferenceEquals(_replyClaim.Mission, mission) ||
                    _replyClaim.SpeakerAgentIndex != speaker.Index)
                {
                    return false;
                }
                string fingerprint = BattleSpeechReplyBindingV1.Fingerprint(content);
                return string.IsNullOrEmpty(_replyClaim.ExpectedReplyFingerprint) ||
                       string.Equals(
                           _replyClaim.ExpectedReplyFingerprint,
                           fingerprint,
                           StringComparison.Ordinal);
            }
        }

        private static void TryBindQueuedReplyClaim(
            Mission mission,
            Agent speaker,
            string replyText,
            int conversationEpoch,
            string playerDirectedActionText,
            string playerDirectedNpcReplyText,
            double now)
        {
            lock (Sync)
            {
                if (_replyClaim == null || mission == null || speaker == null ||
                    !ReferenceEquals(_replyClaim.Mission, mission) ||
                    _replyClaim.SpeakerAgentIndex != speaker.Index ||
                    (_replyClaim.ConversationEpoch != 0 && conversationEpoch != 0 &&
                     _replyClaim.ConversationEpoch != conversationEpoch) ||
                    now > _replyClaim.ExpiresAtMissionTime ||
                    !BattleSpeechReplyBindingV1.RequestMatches(
                        _replyClaim.RequestText,
                        playerDirectedActionText) ||
                    !BattleSpeechReplyBindingV1.ReplyMatches(
                        playerDirectedNpcReplyText,
                        replyText))
                {
                    return;
                }
                _replyClaim.ExpectedReplyFingerprint =
                    BattleSpeechReplyBindingV1.Fingerprint(replyText);
            }
        }
    }

    internal sealed class BattleSpeechReplyClaimV2
    {
        public Guid SessionId;
        public Mission Mission;
        public int SpeakerAgentIndex;
        public string SpeakerName;
        public int ConversationEpoch;
        public string RequestText;
        public string ExpectedReplyFingerprint;
        public string DeferredReplyFingerprint;
        public double ExpiresAtMissionTime;
    }

    internal sealed class DeferredNpcReplyV2
    {
        public Guid SessionId;
        public object Behavior;
        public object NpcPacket;
        public Agent Speaker;
        public string Content;
        public bool AllowTts;
        public bool AttachTtsToSceneAgent;
        public bool SuppressInteractionTimeoutArm;
    }

    internal enum BattleSpeechInputKindV1
    {
        PlayerShout,
        QueuedNpcReplyCandidate,
        ShownNpcReply,
        GeneratedNpcReply
    }

    internal sealed class BattleSpeechCapturedInputV1
    {
        public BattleSpeechInputKindV1 InputKind { get; set; }
        public bool DedicatedSpeechEntry { get; set; }
        public Guid SessionId { get; set; }
        public Mission Mission { get; set; }
        public string RawText { get; set; }
        public Agent Player { get; set; }
        public Agent Speaker { get; set; }
        public Agent PrimaryTarget { get; set; }
        public Agent[] FramedTargets { get; set; } = Array.Empty<Agent>();
        public int ConversationEpoch { get; set; }
        public bool Force { get; set; }
        public string PlayerDirectedActionText { get; set; }
        public string PlayerDirectedNpcReplyText { get; set; }
        public object AfBehavior { get; set; }
        public object AfNpcPacket { get; set; }
        public object OriginalAfBehavior { get; set; }
        public string OriginalExtraFact { get; set; }
        public int? OriginalForcedPrimaryAgentIndex { get; set; }
        public double SubmittedAtMissionTime { get; set; }
    }
}
