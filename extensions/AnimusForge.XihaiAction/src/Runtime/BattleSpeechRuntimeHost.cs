using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal static class BattleSpeechRuntimeHost
    {
        private static readonly object Sync = new object();
        private static readonly AsyncLocal<BattleSpeechPromptScopeV2> ReplyPromptScope =
            new AsyncLocal<BattleSpeechPromptScopeV2>();
        private const int MaximumSpeechHistoryEntries = 48;
        private const int MaximumSpeechHistoryPerSpeaker = 6;
        private static readonly List<GeneratedSpeechHistoryEntryV1> SpeechHistory =
            new List<GeneratedSpeechHistoryEntryV1>();
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
                if (Settings != null)
                {
                    StageSettings.MaximumAudienceReplySubmissionsPerTick =
                        Settings.MaximumAudienceReplySubmissionsPerTick;
                }
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
                SpeechHistory.Clear();
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
                DedicatedNpcSpeechEntry = true,
                Mission = mission,
                RawText = string.Empty,
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
            int speakerAgentIndex,
            string replyText,
            object afBehavior,
            object afNpcPacket,
            BattleSpeechCombinedNpcResponseV2 combinedResponse,
            int conversationEpoch,
            double now)
        {
            bool accepted = Submit(new BattleSpeechCapturedInputV1
            {
                InputKind = BattleSpeechInputKindV1.GeneratedNpcReply,
                SessionId = sessionId,
                Mission = mission,
                RawText = replyText,
                SpeakerAgentIndex = speakerAgentIndex,
                AfBehavior = afBehavior,
                AfNpcPacket = afNpcPacket,
                CombinedResponse = combinedResponse,
                ConversationEpoch = conversationEpoch,
                SubmittedAtMissionTime = now
            });
            if (accepted)
            {
                RecordGeneratedNpcSpeech(mission, speakerAgentIndex, replyText, now);
            }
            return accepted;
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
                    "Forced player speech claimed before AF ordinary scene route. Speaker=player");
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
                  decision.Kind != BattleSpeechTriggerKindV2.NeedsClassifier))
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
            double expiresAtMissionTime,
            bool combinedRequest = false,
            int audienceReplyCount = 0)
        {
            if (sessionId == Guid.Empty || mission == null || speaker == null)
            {
                return;
            }
            BattleSpeechReplyClaimV2 claim;
            lock (Sync)
            {
                claim = new BattleSpeechReplyClaimV2
                {
                    SessionId = sessionId,
                    Mission = mission,
                    SpeakerAgentIndex = speaker.Index,
                    SpeakerName = speaker.Name ?? string.Empty,
                    ConversationEpoch = conversationEpoch,
                    RequestText = requestText ?? string.Empty,
                    ExpiresAtMissionTime = expiresAtMissionTime,
                    CombinedRequest = combinedRequest,
                    AudienceReplyCount = Math.Max(0, audienceReplyCount),
                    RecentSpeechTexts = GetRecentSpeechTextsUnsafe(mission, speaker.Index),
                    RegenerationAttempts = 0
                };
                claim.PromptSnapshot = CreateReplyPromptSnapshotUnsafe(claim);
                _replyClaim = claim;
                _deferredReply = null;
            }
            if (claim.PromptSnapshot != null)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_INPUT",
                    "Speech prompt length snapshot frozen. Session=" +
                    sessionId.ToString("N") +
                    " Minimum=" + claim.PromptSnapshot.MinimumChars +
                    " Maximum=" + claim.PromptSnapshot.MaximumChars);
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
            minimumChars = 0;
            maximumChars = 0;
            if (!TryCaptureReplyPromptSnapshot(npcName, out BattleSpeechReplyPromptSnapshotV2 snapshot))
            {
                return false;
            }
            minimumChars = snapshot.MinimumChars;
            maximumChars = snapshot.MaximumChars;
            return true;
        }

        internal static bool TryGetReplyPromptInstruction(
            string npcName,
            out string instruction)
        {
            instruction = null;
            if (!TryCaptureReplyPromptSnapshot(
                    npcName,
                    out BattleSpeechReplyPromptSnapshotV2 snapshot))
            {
                return false;
            }
            if (snapshot.CombinedRequest)
            {
                instruction = BattleSpeechFrameworkV2.BuildCombinedNpcSpeechPromptInstruction(
                    snapshot.MinimumChars,
                    snapshot.MaximumChars,
                    snapshot.AllowedIntentKeys,
                    snapshot.AudienceReplyCount,
                    snapshot.AudienceReplyMinimumChars,
                    snapshot.AudienceReplyMaximumChars,
                    snapshot.RecentSpeechTexts,
                    snapshot.RegenerationAttempts);
            }
            else
            {
                instruction = BattleSpeechFrameworkV2.BuildNpcSpeechPromptInstruction(
                    snapshot.MinimumChars,
                    snapshot.MaximumChars,
                    snapshot.RecentSpeechTexts,
                    snapshot.RegenerationAttempts);
            }
            return true;
        }

        internal static IDisposable BeginNpcSpeechPromptScope(
            Guid sessionId,
            Mission mission,
            int speakerAgentIndex)
        {
            BattleSpeechPromptScopeV2 previous = ReplyPromptScope.Value;
            ReplyPromptScope.Value = new BattleSpeechPromptScopeV2(
                sessionId,
                mission,
                speakerAgentIndex);
            return new ScopeLeaseV2(previous);
        }

        internal static void RestoreNpcSpeechPromptScope(
            BattleSpeechPromptScopeV2 previous)
        {
            ReplyPromptScope.Value = previous;
        }

        internal static bool TryGetActiveReplyPromptSnapshot(
            out BattleSpeechReplyPromptSnapshotV2 snapshot)
        {
            snapshot = ReplyPromptScope.Value?.PromptSnapshot;
            return snapshot != null;
        }

        internal static bool HasNpcSpeechPromptScope => ReplyPromptScope.Value != null;

        internal static bool IsReplyPromptReplacementRequired(string npcName)
        {
            BattleSpeechPromptScopeV2 scope = ReplyPromptScope.Value;
            // A claimed request may outlive the global claim while AF is still
            // unwinding its asynchronous prompt chain.  Once this execution
            // context has a frozen snapshot, it must remain fail-closed rather
            // than falling through to AF's player-facing prompt.
            if (scope?.PromptSnapshot != null)
            {
                return true;
            }
            lock (Sync)
            {
                if (!_initialized || _replyClaim == null || StageSettings == null)
                {
                    return false;
                }
                if (scope != null)
                {
                    return scope.SessionId == _replyClaim.SessionId &&
                           ReferenceEquals(scope.Mission, _replyClaim.Mission) &&
                           scope.SpeakerAgentIndex == _replyClaim.SpeakerAgentIndex;
                }
                return NamesMatchClaimUnsafe(npcName, _replyClaim);
            }
        }

        private static bool TryCaptureReplyPromptSnapshot(
            string npcName,
            out BattleSpeechReplyPromptSnapshotV2 snapshot)
        {
            snapshot = null;
            BattleSpeechPromptScopeV2 scope = ReplyPromptScope.Value;
            if (scope?.PromptSnapshot != null)
            {
                snapshot = scope.PromptSnapshot;
                return true;
            }
            lock (Sync)
            {
                if (!_initialized || _replyClaim == null || StageSettings == null)
                {
                    return false;
                }
                bool matched = scope != null
                    ? scope.SessionId == _replyClaim.SessionId &&
                      ReferenceEquals(scope.Mission, _replyClaim.Mission) &&
                      scope.SpeakerAgentIndex == _replyClaim.SpeakerAgentIndex
                    : NamesMatchClaimUnsafe(npcName, _replyClaim);
                if (!matched)
                {
                    return false;
                }
                // All mutable claim, MCM and whitelist values are copied while
                // holding the same lock. The claim snapshot is created once at
                // session admission so prompt construction and reply display
                // cannot observe different MCM values.
                snapshot = _replyClaim.PromptSnapshot ??
                           CreateReplyPromptSnapshotUnsafe(_replyClaim);
                _replyClaim.PromptSnapshot = snapshot;
                if (snapshot == null)
                {
                    return false;
                }
                if (scope != null)
                {
                    scope.PromptSnapshot = snapshot;
                }
                return true;
            }
        }

        private static bool NamesMatchClaimUnsafe(
            string npcName,
            BattleSpeechReplyClaimV2 claim)
        {
            // Name matching is only a legacy last resort.  An empty display
            // name must never match every active Claim; the strong binding is
            // the SessionId/Mission/AgentIndex scope above.
            if (string.IsNullOrWhiteSpace(npcName) ||
                string.IsNullOrWhiteSpace(claim?.SpeakerName))
            {
                return false;
            }
            string requested = npcName.Trim();
            string claimed = claim.SpeakerName.Trim();
            return requested.IndexOf(claimed, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   claimed.IndexOf(requested, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool TryGetCombinedSpeechContract(
            out int minimumChars,
            out int maximumChars,
            out string[] allowedIntentKeys,
            out int audienceReplyCount)
        {
            return TryGetCombinedSpeechContract(
                out minimumChars,
                out maximumChars,
                out allowedIntentKeys,
                out audienceReplyCount,
                out _,
                out _);
        }

        internal static bool TryBeginSpeechRegeneration(
            Agent speaker,
            string candidate,
            out double similarity)
        {
            similarity = 0d;
            return speaker != null && TryBeginSpeechRegeneration(
                speaker.Mission,
                speaker.Index,
                candidate,
                out similarity);
        }

        internal static bool TryBeginSpeechRegeneration(
            Mission mission,
            int speakerAgentIndex,
            string candidate,
            out double similarity)
        {
            similarity = 0d;
            lock (Sync)
            {
                if (mission == null || speakerAgentIndex < 0 || _replyClaim == null ||
                    !ReferenceEquals(_replyClaim.Mission, mission) ||
                    _replyClaim.SpeakerAgentIndex != speakerAgentIndex ||
                    _replyClaim.RegenerationAttempts >= 1)
                {
                    return false;
                }
                string[] previous = GetRecentSpeechTextsUnsafe(
                    _replyClaim.Mission,
                    speakerAgentIndex);
                if (!BattleSpeechDiversityV1.IsExactRepeat(
                        candidate,
                        previous,
                        out similarity))
                {
                    return false;
                }
                _replyClaim.RegenerationAttempts++;
                return true;
            }
        }

        private static void RecordGeneratedNpcSpeech(
            Mission mission,
            int speakerAgentIndex,
            string content,
            double now)
        {
            if (mission == null || speakerAgentIndex < 0 || string.IsNullOrWhiteSpace(content))
            {
                return;
            }
            lock (Sync)
            {
                SpeechHistory.Add(new GeneratedSpeechHistoryEntryV1
                {
                    Mission = mission,
                    SpeakerAgentIndex = speakerAgentIndex,
                    Content = content,
                    MissionTime = now
                });
                int perSpeaker = 0;
                for (int i = SpeechHistory.Count - 1; i >= 0; i--)
                {
                    GeneratedSpeechHistoryEntryV1 entry = SpeechHistory[i];
                    if (ReferenceEquals(entry.Mission, mission) &&
                        entry.SpeakerAgentIndex == speakerAgentIndex)
                    {
                        perSpeaker++;
                        if (perSpeaker > MaximumSpeechHistoryPerSpeaker)
                        {
                            SpeechHistory.RemoveAt(i);
                        }
                    }
                }
                while (SpeechHistory.Count > MaximumSpeechHistoryEntries)
                {
                    SpeechHistory.RemoveAt(0);
                }
            }
        }

        private static string[] GetRecentSpeechTextsUnsafe(
            Mission mission,
            int speakerAgentIndex)
        {
            var result = new List<string>(MaximumSpeechHistoryPerSpeaker);
            for (int i = SpeechHistory.Count - 1; i >= 0; i--)
            {
                GeneratedSpeechHistoryEntryV1 entry = SpeechHistory[i];
                if (ReferenceEquals(entry.Mission, mission) &&
                    entry.SpeakerAgentIndex == speakerAgentIndex)
                {
                    result.Add(entry.Content);
                    if (result.Count >= MaximumSpeechHistoryPerSpeaker)
                    {
                        break;
                    }
                }
            }
            return result.ToArray();
        }

        internal static bool TryGetCombinedSpeechContract(
            out int minimumChars,
            out int maximumChars,
            out string[] allowedIntentKeys,
            out int audienceReplyCount,
            out int audienceReplyMinimumChars,
            out int audienceReplyMaximumChars)
        {
            BattleSpeechReplyPromptSnapshotV2 snapshot = ReplyPromptScope.Value?.PromptSnapshot;
            if (snapshot == null)
            {
                lock (Sync)
                {
                    if (!_initialized || _replyClaim == null ||
                        !_replyClaim.CombinedRequest || StageSettings == null)
                    {
                        minimumChars = 0;
                        maximumChars = 0;
                        allowedIntentKeys = Array.Empty<string>();
                        audienceReplyCount = 0;
                        audienceReplyMinimumChars = 0;
                        audienceReplyMaximumChars = 0;
                        return false;
                    }
                    snapshot = new BattleSpeechReplyPromptSnapshotV2(
                        _replyClaim.SessionId,
                        _replyClaim.Mission,
                        _replyClaim.SpeakerAgentIndex,
                        _replyClaim.SpeakerName,
                        true,
                        StageSettings.ReplyMinimumChars,
                        StageSettings.ReplyMaximumChars,
                        GetEnabledSpeechIntentKeys().ToArray(),
                        Math.Min(
                            BattleSpeechFrameworkV2.MaximumAudienceReplies,
                            Math.Max(0, _replyClaim.AudienceReplyCount)),
                        StageSettings.AudienceReplyMinimumChars,
                        StageSettings.AudienceReplyMaximumChars,
                        _replyClaim.RecentSpeechTexts?.ToArray() ?? Array.Empty<string>(),
                        _replyClaim.RegenerationAttempts);
                }
            }
            if (!snapshot.CombinedRequest)
            {
                minimumChars = 0;
                maximumChars = 0;
                allowedIntentKeys = Array.Empty<string>();
                audienceReplyCount = 0;
                audienceReplyMinimumChars = 0;
                audienceReplyMaximumChars = 0;
                return false;
            }
            minimumChars = snapshot.MinimumChars;
            maximumChars = snapshot.MaximumChars;
            allowedIntentKeys = snapshot.AllowedIntentKeys.ToArray();
            audienceReplyCount = snapshot.AudienceReplyCount;
            audienceReplyMinimumChars = snapshot.AudienceReplyMinimumChars;
            audienceReplyMaximumChars = snapshot.AudienceReplyMaximumChars;
            return true;
        }

        internal static string[] GetEnabledSpeechIntentKeys()
        {
            try
            {
                return SceneActionFrameworkV4.LogicalActions
                    .Where(entry => entry.PlaybackMode == ActionMode.OneShot)
                    .Select(entry => entry.IntentKey)
                    .Where(key =>
                    {
                        if (!SceneActionsRuntimeHost.Catalog.Intents.TryGetValue(
                                key,
                                out IntentDefinition intent) ||
                            !SceneActionsRuntimeHost.Catalog.Actions.TryGetValue(
                                intent.ActionKey,
                                out ActionDefinition action))
                        {
                            return false;
                        }
                        return !SceneActionsRuntimeHost.Settings.ActionOverrides.TryGetValue(
                                   action.Key,
                                   out ActionOverride value) ||
                               value.Enabled != false;
                    })
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        internal static bool HasActiveNpcSpeechClaim()
        {
            lock (Sync)
            {
                return _initialized &&
                       _replyClaim != null &&
                       StageSettings != null;
            }
        }

        internal static bool IsActiveNpcSpeechSystemPrompt(string systemPrompt)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                return false;
            }

            try
            {
                bool hasSpeechPromptMarker =
                    systemPrompt.IndexOf(
                        "【阵前演讲正文生成任务",
                        StringComparison.Ordinal) >= 0 ||
                    systemPrompt.IndexOf(
                        "【阵前演讲单请求协议】",
                        StringComparison.Ordinal) >= 0;
                if (!hasSpeechPromptMarker)
                {
                    return false;
                }

                BattleSpeechPromptScopeV2 scope = ReplyPromptScope.Value;
                // Keep a prompt chain that already captured a speech snapshot
                // on the speech contract even if session cleanup cleared the
                // process-wide claim before a later strict-system callback.
                if (scope?.PromptSnapshot != null)
                {
                    return true;
                }
                lock (Sync)
                {
                    if (!_initialized || _replyClaim == null || StageSettings == null ||
                        (scope != null
                            ? scope.SessionId != _replyClaim.SessionId ||
                              !ReferenceEquals(scope.Mission, _replyClaim.Mission) ||
                              scope.SpeakerAgentIndex != _replyClaim.SpeakerAgentIndex
                            : !ReferenceEquals(_replyClaim.Mission, Mission.Current)))
                    {
                        return false;
                    }

                    Mission currentMission = scope?.Mission ?? Mission.Current;
                    double now = currentMission?.CurrentTime ?? double.NaN;
                    return !double.IsNaN(now) && !double.IsInfinity(now) &&
                           !double.IsNaN(_replyClaim.ExpiresAtMissionTime) &&
                           !double.IsInfinity(_replyClaim.ExpiresAtMissionTime) &&
                           now <= _replyClaim.ExpiresAtMissionTime;
                }
            }
            catch
            {
                return false;
            }
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
            if (speaker == null || _replyClaim == null ||
                !ReferenceEquals(_replyClaim.Mission, speaker.Mission) ||
                _replyClaim.SpeakerAgentIndex != speaker.Index)
            {
                return content;
            }
            BattleSpeechReplyPromptSnapshotV2 snapshot =
                _replyClaim.PromptSnapshot ?? CreateReplyPromptSnapshotUnsafe(_replyClaim);
            _replyClaim.PromptSnapshot = snapshot;
            if (snapshot == null)
            {
                return content;
            }
            return BattleSpeechFrameworkV2.NormalizeNpcSpeechReply(
                content,
                snapshot.MinimumChars,
                snapshot.MaximumChars);
        }

        private static BattleSpeechReplyPromptSnapshotV2 CreateReplyPromptSnapshotUnsafe(
            BattleSpeechReplyClaimV2 claim)
        {
            if (claim == null || StageSettings == null)
            {
                return null;
            }
            return new BattleSpeechReplyPromptSnapshotV2(
                claim.SessionId,
                claim.Mission,
                claim.SpeakerAgentIndex,
                claim.SpeakerName,
                claim.CombinedRequest,
                StageSettings.ReplyMinimumChars,
                StageSettings.ReplyMaximumChars,
                GetEnabledSpeechIntentKeys(),
                claim.AudienceReplyCount,
                StageSettings.AudienceReplyMinimumChars,
                StageSettings.AudienceReplyMaximumChars,
                claim.RecentSpeechTexts?.ToArray() ?? Array.Empty<string>(),
                claim.RegenerationAttempts);
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

    internal sealed class BattleSpeechReplyPromptSnapshotV2
    {
        public BattleSpeechReplyPromptSnapshotV2(
            Guid sessionId,
            Mission mission,
            int speakerAgentIndex,
            string speakerName,
            bool combinedRequest,
            int minimumChars,
            int maximumChars,
            string[] allowedIntentKeys,
            int audienceReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            string[] recentSpeechTexts,
            int regenerationAttempts)
        {
            SessionId = sessionId;
            Mission = mission;
            SpeakerAgentIndex = speakerAgentIndex;
            SpeakerName = speakerName ?? string.Empty;
            CombinedRequest = combinedRequest;
            MinimumChars = minimumChars;
            MaximumChars = maximumChars;
            AllowedIntentKeys = (allowedIntentKeys ?? Array.Empty<string>()).ToArray();
            AudienceReplyCount = Math.Min(
                BattleSpeechFrameworkV2.MaximumAudienceReplies,
                Math.Max(0, audienceReplyCount));
            AudienceReplyMinimumChars = audienceReplyMinimumChars;
            AudienceReplyMaximumChars = audienceReplyMaximumChars;
            RecentSpeechTexts = (recentSpeechTexts ?? Array.Empty<string>()).ToArray();
            RegenerationAttempts = Math.Max(0, regenerationAttempts);
        }

        public Guid SessionId { get; }
        public Mission Mission { get; }
        public int SpeakerAgentIndex { get; }
        public string SpeakerName { get; }
        public bool CombinedRequest { get; }
        public int MinimumChars { get; }
        public int MaximumChars { get; }
        public string[] AllowedIntentKeys { get; }
        public int AudienceReplyCount { get; }
        public int AudienceReplyMinimumChars { get; }
        public int AudienceReplyMaximumChars { get; }
        public string[] RecentSpeechTexts { get; }
        public int RegenerationAttempts { get; }
    }

    internal sealed class BattleSpeechPromptScopeV2
    {
        public BattleSpeechPromptScopeV2(
            Guid sessionId,
            Mission mission,
            int speakerAgentIndex)
        {
            SessionId = sessionId;
            Mission = mission;
            SpeakerAgentIndex = speakerAgentIndex;
        }

        public Guid SessionId { get; }
        public Mission Mission { get; }
        public int SpeakerAgentIndex { get; }
        public BattleSpeechReplyPromptSnapshotV2 PromptSnapshot { get; set; }
    }

    internal sealed class ScopeLeaseV2 : IDisposable
    {
        private readonly BattleSpeechPromptScopeV2 _previous;
        private bool _disposed;

        public ScopeLeaseV2(BattleSpeechPromptScopeV2 previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            ReplyPromptScopeRestorer.Restore(_previous);
        }
    }

    // Keeps AsyncLocal restoration private to the host while allowing the small
    // disposable lease above to remain a plain data object.
    internal static class ReplyPromptScopeRestorer
    {
        internal static void Restore(BattleSpeechPromptScopeV2 previous)
        {
            BattleSpeechRuntimeHost.RestoreNpcSpeechPromptScope(previous);
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
        public bool CombinedRequest;
        public int AudienceReplyCount;
        public string[] RecentSpeechTexts;
        public int RegenerationAttempts;
        public BattleSpeechReplyPromptSnapshotV2 PromptSnapshot;
    }

    internal sealed class GeneratedSpeechHistoryEntryV1
    {
        public Mission Mission;
        public int SpeakerAgentIndex;
        public string Content;
        public double MissionTime;
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
        GeneratedNpcReply,
        DedicatedNpcSpeechRetry
    }

    internal sealed class BattleSpeechCapturedInputV1
    {
        public BattleSpeechInputKindV1 InputKind { get; set; }
        public bool DedicatedSpeechEntry { get; set; }
        public bool DedicatedNpcSpeechEntry { get; set; }
        public Guid SessionId { get; set; }
        public Mission Mission { get; set; }
        public string RawText { get; set; }
        public Agent Player { get; set; }
        public Agent Speaker { get; set; }
        public int SpeakerAgentIndex { get; set; } = -1;
        public Agent PrimaryTarget { get; set; }
        public Agent[] FramedTargets { get; set; } = Array.Empty<Agent>();
        public int ConversationEpoch { get; set; }
        public bool Force { get; set; }
        public string PlayerDirectedActionText { get; set; }
        public string PlayerDirectedNpcReplyText { get; set; }
        public object AfBehavior { get; set; }
        public object AfNpcPacket { get; set; }
        public BattleSpeechCombinedNpcResponseV2 CombinedResponse { get; set; }
        public object OriginalAfBehavior { get; set; }
        public string OriginalExtraFact { get; set; }
        public int? OriginalForcedPrimaryAgentIndex { get; set; }
        public double SubmittedAtMissionTime { get; set; }
    }
}
