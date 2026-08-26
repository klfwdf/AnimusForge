using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed class BattleSpeechPerformanceMissionBehavior : MissionBehavior,
        IBattleSpeechRuntimeEffectV1
    {
        private IDisposable _registration;
        private ActivePerformance _active;
        private readonly Dictionary<string, ActionIndexCache> _closingCommandActions =
            new Dictionary<string, ActionIndexCache>(StringComparer.Ordinal);
        private readonly Dictionary<string, ActionIndexCache> _speechOpeningActions =
            new Dictionary<string, ActionIndexCache>(StringComparer.Ordinal);
        private const string SpeechOpeningActionId = "act_af_speech_nacisword1";
        private const double AdvanceAudienceCleanupSettleSeconds = 0.12d;
        private const double AdvanceAudienceCleanupTimeoutSeconds = 0.75d;
        private bool _closed = true;
        private bool _mcmDisabled;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        internal bool IsSessionActive => !_closed;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            if (!BattleSpeechRuntimeHost.IsInitialized ||
                !BattleSpeechRuntimeHost.ConfigurationValid ||
                BattleSpeechRuntimeHost.Settings == null ||
                !BattleSpeechRuntimeHost.PerformanceConfigurationValid ||
                BattleSpeechRuntimeHost.PerformanceSettings == null)
            {
                return;
            }
            _registration = BattleSpeechApiV1.RegisterRuntimeEffect(this);
            _closed = false;
            _mcmDisabled = !BattleSpeechRuntimeHost.IsPerformanceEnabled;
            BattleSpeechRuntimeHost.BindPerformanceEffect(this);
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Mission performance effect activated.");
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_closed || !ReferenceEquals(Mission.Current, Mission))
            {
                return;
            }
            if (!BattleSpeechRuntimeHost.IsPerformanceEnabled)
            {
                DisableFromHost(
                    "BattleSpeech presentation disabled in MCM; active reactions were discarded.");
                return;
            }
            if (_mcmDisabled)
            {
                _mcmDisabled = false;
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_MCM",
                    "BattleSpeech performance re-enabled for the active Mission.");
            }
            if (_active == null)
            {
                return;
            }
            try
            {
                Progress(Mission.CurrentTime);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Performance tick failed closed.",
                    ex);
                CancelActive("Performance exception.");
            }
        }

        public override void OnAgentHit(
            Agent affectedAgent,
            Agent affectorAgent,
            in MissionWeapon affectorWeapon,
            in Blow blow,
            in AttackCollisionData attackCollisionData)
        {
            base.OnAgentHit(
                affectedAgent,
                affectorAgent,
                in affectorWeapon,
                in blow,
                in attackCollisionData);
            BattleSpeechEnemyProximityCache.Invalidate(
                Mission,
                affectedAgent,
                affectorAgent);
            if (_active != null &&
                (ReferenceEquals(_active.Speaker, affectedAgent) ||
                 ReferenceEquals(_active.Speaker, affectorAgent)))
            {
                // A hit/attack must not cancel an in-combat speech.  Mark the
                // performance in-place and remove every presentation channel
                // owned by this speech; the speaker remains free to fight.
                EnterCombatPerformanceMode(_active, "AgentHit");
            }
        }

        public void OnSpeechStarted(BattleSpeechRuntimeContextV1 speech)
        {
            if (_closed || !BattleSpeechRuntimeHost.IsPerformanceEnabled ||
                speech == null || !ReferenceEquals(speech.Mission, Mission))
            {
                return;
            }
            CancelActive("Replaced by a newer speech performance.");
            float duration = (float)Math.Max(
                1d,
                speech.Snapshot.EndsAtMissionTime - speech.Snapshot.StartedAtMissionTime);
            BattleSpeechPerformanceSettingsV1 settings =
                CreateEffectivePerformanceSettings();
            Agent[] frozenAudience = FilterFrozenAudience(
                speech.FrozenAudience,
                speech.Speaker);
            BattleSpeechPerformancePlanV1 plan =
                BattleSpeechPerformancePlannerV1.CreateFromProgramOrSpeech(
                speech.Snapshot.SessionId,
                speech.ActionProgram,
                speech.Snapshot.SpeechText,
                duration,
                frozenAudience.Length,
                settings);
            List<RuntimeCue> cues = plan.SpeakerCues
                .Select(cue => new RuntimeCue(cue, false))
                .Concat(plan.AudienceCues.Select(cue => new RuntimeCue(cue, true)))
                .OrderBy(cue => cue.Cue.OffsetSeconds)
                .ThenBy(cue => cue.IsAudience ? 1 : 0)
                .ThenBy(cue => cue.Cue.AudienceOrdinal)
                .ToList();
            _active = new ActivePerformance
            {
                OwnerToken = speech.Snapshot.SessionId,
                SpeakerKind = speech.Snapshot.SpeakerKind,
                Mission = speech.Mission,
                Speaker = speech.Speaker,
                Phase = speech.Snapshot.Phase,
                Audience = frozenAudience,
                StartedAtMissionTime = speech.Snapshot.StartedAtMissionTime,
                SpeechEndsAtMissionTime = speech.Snapshot.EndsAtMissionTime,
                Tactic = speech.Tactic,
                TacticDecisionProvided = speech.TacticDecisionProvided,
                CombatSpeechMode = speech.CombatSpeechMode,
                TailEndsAtMissionTime = speech.Snapshot.StartedAtMissionTime +
                                        plan.TailEndOffsetSeconds,
                LastAudiencePresentationAtMissionTime =
                    speech.Snapshot.StartedAtMissionTime,
                Cues = cues,
                Settings = settings,
                VoiceAudienceOrdinals = SelectVoiceAudienceOrdinals(
                    speech.Snapshot.SessionId,
                    frozenAudience.Length,
                    BattleSpeechRuntimeHost.StageSettings.AudienceVoiceCount),
                AudienceReplies = (speech.AudienceReplies ?? Array.Empty<string>())
                    .Take(BattleSpeechRuntimeHost.StageSettings.AudienceReplyCount)
                    .ToArray(),
                ReplyAudienceOrdinals = SelectAudienceOrdinals(
                    speech.Snapshot.SessionId,
                    frozenAudience.Length,
                    Math.Min(
                        speech.AudienceReplies?.Length ?? 0,
                        BattleSpeechRuntimeHost.StageSettings.AudienceReplyCount),
                        "reply"),
                OwnedActionIndices = ResolvePerformanceActionIndices(cues),
                FirstVisualWaveTarget = Math.Min(
                    BattleSpeechRuntimeHost.StageSettings.VisualWaveSize,
                    plan.AudienceCues.Count(cue => cue.OffsetSeconds >= duration))
            };
            TryPlaySpeechOpeningGesture(_active);
            BattleSpeechRuntimeHost.MarkPerformanceStarted(speech.Mission);
            BattleSpeechEnemyProximityCache.Invalidate(Mission, speech.Speaker, null);
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Session=" + _active.OwnerToken.ToString("N") +
                " SpeakerCues=" + plan.SpeakerCues.Count +
                " AudienceCues=" + plan.AudienceCues.Count +
                " SpeakerMounted=" + (speech.Speaker.MountAgent != null) +
                " VoiceCandidates=" + _active.VoiceAudienceOrdinals.Length +
                " SpokenReplyCandidates=" + _active.ReplyAudienceOrdinals.Length +
                " TailEnd=" + _active.TailEndsAtMissionTime.ToString("F2"));
        }

        public void OnSpeechCompleted(BattleSpeechRuntimeContextV1 speech)
        {
            if (!Matches(speech))
            {
                return;
            }
            _active.Completed = true;
            if (BattleSpeechRuntimeHost.StageSettings.TacticalAdvanceEnabled &&
                !_active.CombatSpeechMode)
            {
                TryHoldSpeakerAtSpeechLine(_active);
            }
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Session=" + _active.OwnerToken.ToString("N") +
                " SpeechState=Completed; final audience waves enabled.");
        }

        public void OnSpeechCancelled(BattleSpeechRuntimeContextV1 speech, string reason)
        {
            if (Matches(speech))
            {
                CancelActive(reason ?? "Battle speech cancelled.");
            }
        }

        internal bool TryForceStopForPlayerCommand(
            Mission mission,
            string commandText)
        {
            if (_closed || _active == null || !ReferenceEquals(Mission, mission) ||
                !ReferenceEquals(Mission.Current, mission))
            {
                return false;
            }
            ActivePerformance performance = _active;
            int released = ReleaseOwnedPerformanceChannels(performance);
            CancelActive(
                "Player command superseded speech reactions: " +
                (commandText ?? string.Empty));
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Session=" + performance.OwnerToken.ToString("N") +
                " State=ForceClearedByPlayerCommand ReleasedChannels=" + released);
            return true;
        }

        protected override void OnEndMission()
        {
            Close("OnEndMission");
            base.OnEndMission();
        }

        public override void OnMissionStateFinalized()
        {
            Close("OnMissionStateFinalized");
            base.OnMissionStateFinalized();
        }

        public override void OnRemoveBehavior()
        {
            Close("OnRemoveBehavior");
            base.OnRemoveBehavior();
        }

        private void EnterCombatPerformanceMode(
            ActivePerformance performance,
            string trigger)
        {
            if (performance == null || performance.CombatSpeechMode)
            {
                return;
            }
            _mcmDisabled = false;
            performance.CombatSpeechMode = true;
            int releasedAudience = ReleaseOwnedAudiencePerformanceChannels(performance);
            ReleaseHeldSpeaker(performance);
            CancelTrustedPlaybackOnce(
                performance,
                "Combat speech mode entered; visual presentation cancelled.");
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Session=" + performance.OwnerToken.ToString("N") +
                " State=CombatInPlace Trigger=" +
                (trigger ?? "Unknown") +
                " ReleasedAudienceChannels=" + releasedAudience);
        }

        private void Progress(double now)
        {
            ActivePerformance performance = _active;
            if (performance == null ||
                !ReferenceEquals(performance.Mission, Mission) ||
                !IsEligiblePerformanceActor(performance.Speaker) ||
                !IsPhaseOpen())
            {
                CancelActive("Performance speaker became unavailable.");
                return;
            }
            if (!performance.CombatSpeechMode &&
                HasNearbyEnemyThrottled(performance.Speaker))
            {
                EnterCombatPerformanceMode(performance, "EnemyNearby");
            }

            int submittedThisTick = 0;
            while (performance.NextCueIndex < performance.Cues.Count &&
                   submittedThisTick < BattleSpeechRuntimeHost.StageSettings
                       .MaximumVisualSubmissionsPerTick)
            {
                RuntimeCue runtimeCue = performance.Cues[performance.NextCueIndex];
                if (!BattleSpeechFrameworkV2.ShouldSubmitAudienceVisuals(
                        performance.CombatSpeechMode,
                        configuredEnabled: true))
                {
                    // Combat speeches retain only written audience replies.
                    // Fast-forward every queued visual cue without waiting for
                    // its original presentation offset.
                    performance.NextCueIndex++;
                    continue;
                }
                double due = performance.StartedAtMissionTime + runtimeCue.Cue.OffsetSeconds;
                if (now + 0.0001d < due)
                {
                    break;
                }
                bool finalAudienceCue = runtimeCue.IsAudience &&
                                        due >= performance.SpeechEndsAtMissionTime;
                if (finalAudienceCue && !performance.Completed)
                {
                    break;
                }
                if (!runtimeCue.IsAudience && performance.Completed)
                {
                    performance.NextCueIndex++;
                    continue;
                }
                if (!runtimeCue.IsAudience &&
                    IsSpeechOpeningGesturePlaying(performance))
                {
                    // The dedicated arrival clip owns the speaker presentation.
                    // Once accepted, do not let a generic semantic cue replace
                    // it even if Bannerlord reports the enforced full-body clip
                    // through another channel. Audience cues continue normally.
                    performance.NextCueIndex++;
                    if (!performance.SpeakerCueSuppressedByOpeningLogged)
                    {
                        performance.SpeakerCueSuppressedByOpeningLogged = true;
                        SceneActionsLog.Info(
                            "BATTLE_SPEECH_PERFORMANCE",
                            "Session=" + performance.OwnerToken.ToString("N") +
                            " SpeakerCue=Suppressed Reason=nacisword1 owns speaker presentation.");
                    }
                    continue;
                }
                if (runtimeCue.IsAudience && !finalAudienceCue && performance.Completed)
                {
                    performance.NextCueIndex++;
                    continue;
                }
                Agent actor = runtimeCue.IsAudience
                    ? ResolveAudienceActor(performance, runtimeCue.Cue.AudienceOrdinal)
                    : performance.Speaker;
                bool canPlayGesture = runtimeCue.IsAudience
                    ? CanPlayAudienceGesture(actor)
                    : CanPlaySpeakerGesture(actor);
                if (!canPlayGesture)
                {
                    if (!runtimeCue.IsAudience && !performance.SpeakerGestureSkippedLogged)
                    {
                        performance.SpeakerGestureSkippedLogged = true;
                        SceneActionsLog.Info(
                            "BATTLE_SPEECH_PERFORMANCE",
                            "Session=" + performance.OwnerToken.ToString("N") +
                            " SpeakerGesture=Skipped Mounted=" +
                            (actor?.MountAgent != null));
                    }
                    performance.NextCueIndex++;
                    continue;
                }
                Guid requestId = Guid.NewGuid();
                if (!SceneActionsRuntimeHost.TrySubmitTrustedOneShot(
                    requestId,
                    performance.OwnerToken,
                    Mission,
                    actor,
                    runtimeCue.Cue.IntentKey,
                    now,
                    runtimeCue.IsAudience
                        ? "battle-speech-audience"
                        : "battle-speech-speaker"))
                {
                    if (performance.Completed && now >= performance.TailEndsAtMissionTime)
                    {
                        performance.NextCueIndex++;
                        SceneActionsLog.Warning(
                            "BATTLE_SPEECH_PERFORMANCE",
                            "Session=" + performance.OwnerToken.ToString("N") +
                            " Cue=" + runtimeCue.Cue.IntentKey +
                            " State=Dropped Reason=Trusted queue remained unavailable past the tail deadline.");
                    }
                    break;
                }
                performance.NextCueIndex++;
                submittedThisTick++;
                if (finalAudienceCue)
                {
                    performance.FinalAudienceSubmitted++;
                }
                if (runtimeCue.IsAudience)
                {
                    MarkAudiencePresentation(performance, now);
                }
                performance.TailEndsAtMissionTime = Math.Max(
                    performance.TailEndsAtMissionTime,
                    now + performance.Settings.PerformanceTailSeconds);
            }

            TryOpenAudienceResponse(performance, now);
            ProgressAudienceVoicesAndTactic(performance, now);

            if (performance.Completed &&
                performance.NextCueIndex >= performance.Cues.Count &&
                now >= performance.TailEndsAtMissionTime &&
                ResponseWorkCompleted(performance) &&
                submittedThisTick == 0)
            {
                FinishTail("Performance tail completed.");
            }
        }

        private Agent ResolveAudienceActor(ActivePerformance performance, int ordinal)
        {
            if (ordinal < 0 || ordinal >= performance.Audience.Length)
            {
                return null;
            }
            return performance.Audience[ordinal];
        }

        private Agent[] FilterFrozenAudience(
            IEnumerable<Agent> audience,
            Agent speaker)
        {
            return (audience ?? Enumerable.Empty<Agent>())
                .Where(actor => actor != null &&
                                !ReferenceEquals(actor, speaker) &&
                                !ReferenceEquals(actor, Mission.MainAgent) &&
                                !ReferenceEquals(actor, Agent.Main) &&
                                ReferenceEquals(actor.Mission, Mission))
                .Distinct()
                .ToArray();
        }

        private bool IsEligiblePerformanceActor(Agent actor)
        {
            if (actor == null ||
                !ReferenceEquals(actor.Mission, Mission) ||
                !actor.IsActive() ||
                actor.Health <= 0f ||
                !actor.IsHuman ||
                actor.Team == null ||
                !actor.Team.IsValid ||
                actor.IsRunningAway ||
                actor.IsRetreating() ||
                actor.IsUsingGameObject)
            {
                return false;
            }
            return true;
        }

        private bool CanPlayAudienceGesture(Agent actor)
        {
            // Audience reactions are deliberately kept to infantry.  Native
            // conversation gestures are not guaranteed to have a horse-safe
            // animation path, and skipping mounted listeners avoids replacing
            // their combat/mount channel on every reaction wave.
            return IsEligiblePerformanceActor(actor) &&
                   !actor.IsInBeingStruckAction &&
                   actor.MountAgent == null;
        }

        private bool CanPlaySpeakerGesture(Agent actor)
        {
            // A mounted lord is still a valid speech performer.  The rider's
            // channel is owned and released by this performance just like an
            // infantry speaker's channel; mount facing is handled by the
            // staging behavior.
            return IsEligiblePerformanceActor(actor) &&
                   !actor.IsInBeingStruckAction;
        }

        private void TryPlaySpeechOpeningGesture(ActivePerformance performance)
        {
            if (performance == null)
            {
                return;
            }
            if (performance.CombatSpeechMode)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " SpeechOpeningGesture=Skipped Reason=CombatSpeechInPlace");
                return;
            }
            Agent speaker = performance.Speaker;
            if (!CanPlaySpeakerGesture(speaker))
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " SpeechOpeningGesture=Skipped Mounted=" +
                    (speaker?.MountAgent != null));
                return;
            }
            // Mounted speeches remain supported, but the custom nacisword1
            // opening clip is infantry-only.  Do not submit it to a rider's
            // channel where the clip can be rejected or corrupt mount
            // presentation; the speech and later tactical flow continue.
            if (speaker.MountAgent != null)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " SpeechOpeningGesture=Skipped Mounted=True Reason=nacisword1 is infantry-only.");
                return;
            }
            if (SceneActionsRuntimeHost.Providers == null ||
                !SceneActionsRuntimeHost.Providers.SpeechOpeningStaticReady)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " SpeechOpeningGesture=Skipped Reason=" +
                    (SceneActionsRuntimeHost.Providers?.SpeechOpeningStaticReason ??
                     "Action provider registry unavailable."));
                return;
            }
            try
            {
                if (!_speechOpeningActions.TryGetValue(
                        SpeechOpeningActionId,
                        out ActionIndexCache action))
                {
                    action = ActionIndexCache.Create(SpeechOpeningActionId);
                    _speechOpeningActions[SpeechOpeningActionId] = action;
                }
                if (action == ActionIndexCache.act_none)
                {
                    SceneActionsLog.Warning(
                        "BATTLE_SPEECH_PERFORMANCE",
                        "Session=" + performance.OwnerToken.ToString("N") +
                        " SpeechOpeningGesture=Skipped Reason=Action index unavailable.");
                    return;
                }
                bool accepted = speaker.SetActionChannel(
                    1,
                    in action,
                    // This is the user-selected speech pose. At the frozen
                    // speech line it deliberately replaces an existing upper
                    // gesture and applies to the full body.
                    ignorePriority: true,
                    additionalFlags: AnimFlags.anf_enforce_all,
                    blendWithNextActionFactor: 0f,
                    actionSpeed: 1f,
                    blendInPeriod: 0.18f);
                if (!accepted)
                {
                    SceneActionsLog.Warning(
                        "BATTLE_SPEECH_PERFORMANCE",
                        "Session=" + performance.OwnerToken.ToString("N") +
                        " SpeechOpeningGesture=RejectedByEngine");
                    return;
                }
                performance.SpeechOpeningAction = action;
                performance.SpeechOpeningAccepted = true;
                if (!performance.OwnedActionIndices.Any(existing => existing == action))
                {
                    performance.OwnedActionIndices.Add(action);
                }
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " SpeechOpeningGesture=Accepted Action=" + SpeechOpeningActionId +
                    " Animation=nacisword1 Forced=True");
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " SpeechOpeningGesture=Failed " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool IsSpeechOpeningGesturePlaying(
            ActivePerformance performance)
        {
            if (performance == null ||
                !performance.SpeechOpeningAccepted ||
                performance.Speaker == null ||
                !performance.Speaker.IsActive())
            {
                return false;
            }
            return true;
        }

        private bool CanPlayAudienceVoice(Agent actor)
        {
            return IsEligiblePerformanceActor(actor);
        }

        private bool IsPhaseOpen()
        {
            if (Mission == null ||
                Mission.CurrentState != Mission.State.Continuing ||
                !Mission.IsLoadingFinished ||
                Mission.MissionEnded ||
                Mission.MissionIsEnding)
            {
                return false;
            }
            return Mission.Mode == MissionMode.Deployment || Mission.Mode == MissionMode.Battle;
        }

        private static BattleSpeechPerformanceSettingsV1 CreateEffectivePerformanceSettings()
        {
            BattleSpeechPerformanceSettingsV1 source =
                BattleSpeechRuntimeHost.PerformanceSettings;
            BattleSpeechStageSettingsV2 stage = BattleSpeechRuntimeHost.StageSettings;
            return new BattleSpeechPerformanceSettingsV1
            {
                Enabled = source.Enabled,
                SpeakerGesturesEnabled = source.SpeakerGesturesEnabled,
                MaxSpeakerGestures = source.MaxSpeakerGestures,
                MinimumSpeakerGestureSpacingSeconds =
                    source.MinimumSpeakerGestureSpacingSeconds,
                AudienceReactionsEnabled = source.AudienceReactionsEnabled,
                // MCM exposes an actual responder-count cap. Use the full frozen audience
                // here so that the cap, rather than a hidden ratio, determines the count.
                AudienceParticipationRatio = 1f,
                MaximumAudiencePerformers = Math.Min(
                    source.MaximumAudiencePerformers,
                    stage.MaximumVisualResponders),
                AudienceWaveSize = stage.VisualWaveSize,
                AudienceWaveIntervalSeconds = source.AudienceWaveIntervalSeconds,
                AudienceMemberStaggerSeconds = source.AudienceMemberStaggerSeconds,
                AudienceFinalDelaySeconds = source.AudienceFinalDelaySeconds,
                PerformanceTailSeconds = source.PerformanceTailSeconds
            };
        }

        private static int[] SelectVoiceAudienceOrdinals(
            Guid sessionId,
            int audienceCount,
            int maximumVoices)
        {
            return SelectAudienceOrdinals(
                sessionId,
                audienceCount,
                maximumVoices,
                "voice");
        }

        private static int[] SelectAudienceOrdinals(
            Guid sessionId,
            int audienceCount,
            int maximumActors,
            string channel)
        {
            if (sessionId == Guid.Empty || maximumActors <= 0 || audienceCount <= 0)
            {
                return Array.Empty<int>();
            }
            return Enumerable.Range(0, audienceCount)
                .Select(ordinal => new
                {
                    Ordinal = ordinal,
                    Hash = StableSelectionHash(
                        sessionId.ToString("N") + ":" + channel + ":" + ordinal)
                })
                .OrderBy(value => value.Hash)
                .ThenBy(value => value.Ordinal)
                .Take(maximumActors)
                .Select(value => value.Ordinal)
                .ToArray();
        }

        private static uint StableSelectionHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return hash;
            }
        }

        private void TryOpenAudienceResponse(ActivePerformance performance, double now)
        {
            if (performance.ResponseOpened)
            {
                return;
            }
            BattleSpeechStageSettingsV2 stage = BattleSpeechRuntimeHost.StageSettings;
            double responseStartAt = performance.StartedAtMissionTime +
                                     Math.Max(
                                         0.5d,
                                         stage?.AudienceResponseStartDelaySeconds ?? 3d);
            if (now < responseStartAt)
            {
                return;
            }
            bool allVisualCuesProcessed = performance.NextCueIndex >= performance.Cues.Count;
            if (!BattleSpeechFrameworkV2.ShouldOpenAudienceResponse(
                    performance.Completed,
                    performance.FinalAudienceSubmitted,
                    performance.FirstVisualWaveTarget,
                    allVisualCuesProcessed,
                    // A 60-160 character speech is normally longer than the
                    // MCM delay by itself.  Do not open the audience channel
                    // while the body is still being presented; the delay is
                    // the minimum gate, not permission to talk over it.
                    allowDuringSpeech: false))
            {
                return;
            }
            performance.ResponseOpened = true;
            performance.NextReplyAtMissionTime = now;
            performance.NextVoiceAtMissionTime = now;
            performance.TailEndsAtMissionTime = Math.Max(
                performance.TailEndsAtMissionTime,
                now + performance.Settings.PerformanceTailSeconds);
            SceneActionsLog.Info(
                "BATTLE_SPEECH_RESPONSE",
                "Session=" + performance.OwnerToken.ToString("N") +
                " State=Opened VoiceCandidates=" +
                performance.VoiceAudienceOrdinals.Length +
                " SpokenReplyCandidates=" + performance.ReplyAudienceOrdinals.Length +
                " FinalVisualSubmitted=" + performance.FinalAudienceSubmitted +
                " StartDelay=" + (now - performance.StartedAtMissionTime).ToString("F2"));
        }

        private static void MarkAudiencePresentation(
            ActivePerformance performance,
            double now,
            float visualDurationSeconds = 0f)
        {
            if (performance == null)
            {
                return;
            }
            BattleSpeechStageSettingsV2 stage = BattleSpeechRuntimeHost.StageSettings;
            double configuredHold = Math.Max(
                0.5d,
                stage?.AudienceFinalReactionHoldSeconds ?? 2.5d);
            double visualHold = Math.Max(0d, visualDurationSeconds);
            performance.LastAudiencePresentationAtMissionTime = Math.Max(
                performance.LastAudiencePresentationAtMissionTime,
                now + Math.Max(configuredHold, visualHold));
            performance.TailEndsAtMissionTime = Math.Max(
                performance.TailEndsAtMissionTime,
                performance.LastAudiencePresentationAtMissionTime);
        }

        private void ProgressAudienceVoicesAndTactic(
            ActivePerformance performance,
            double now)
        {
            if (!performance.ResponseOpened)
            {
                return;
            }
            BattleSpeechStageSettingsV2 stage = BattleSpeechRuntimeHost.StageSettings;
            bool repliesDone = !stage.AudienceRepliesEnabled ||
                               performance.NextReplyIndex >=
                               performance.ReplyAudienceOrdinals.Length;
            if (!repliesDone && now >= performance.NextReplyAtMissionTime)
            {
                int remaining = performance.ReplyAudienceOrdinals.Length -
                                performance.NextReplyIndex;
                if (performance.ReplyWaveRemaining <= 0)
                {
                    performance.ReplyWaveRemaining =
                        BattleSpeechFrameworkV2.ResolveAudienceReplyWaveSize(
                            performance.OwnerToken,
                            performance.ReplyWaveIndex,
                            stage.AudienceReplyWaveSize,
                            remaining);
                }
                int waveSize = performance.ReplyWaveRemaining;
                int submissionsThisTick = Math.Min(
                    waveSize,
                    stage.MaximumAudienceReplySubmissionsPerTick);
                float longestVisualDuration = 0f;
                int playedThisWave = 0;
                for (int waveOffset = 0;
                     waveOffset < submissionsThisTick;
                     waveOffset++)
                {
                    int replyIndex = performance.NextReplyIndex++;
                    performance.ReplyWaveRemaining--;
                    int ordinal = performance.ReplyAudienceOrdinals[replyIndex];
                    Agent actor = ResolveAudienceActor(performance, ordinal);
                    string reply = replyIndex < performance.AudienceReplies.Length
                        ? performance.AudienceReplies[replyIndex]
                        : string.Empty;
                    float visualDuration = 0f;
                    string replyError = null;
                    bool played = CanPlayAudienceVoice(actor) &&
                                  AfCompatV130.TryShowAudienceReply(
                                      actor,
                                      reply,
                                      !performance.CombatSpeechMode,
                                      out visualDuration,
                                      out replyError);
                    longestVisualDuration = Math.Max(
                        longestVisualDuration,
                        Math.Min(3f, visualDuration));
                    if (!played)
                    {
                        SceneActionsLog.Warning(
                            "BATTLE_SPEECH_REPLY",
                            "Session=" + performance.OwnerToken.ToString("N") +
                            " Agent=" + (actor?.Index ?? -1) +
                            " State=Skipped Reason=" +
                            (replyError ?? "Actor unavailable."));
                    }
                    else
                    {
                        playedThisWave++;
                    }
                }
                bool waveCompleted = performance.ReplyWaveRemaining <= 0;
                double randomDelay = waveCompleted
                    ? BattleSpeechFrameworkV2.ResolveAudienceReplyWaveDelaySeconds(
                        performance.OwnerToken,
                        performance.ReplyWaveIndex++,
                        stage.AudienceReplyMinimumIntervalSeconds,
                        stage.AudienceReplyMaximumIntervalSeconds)
                    : 0.01d;
                performance.NextReplyAtMissionTime = now + Math.Max(
                    waveCompleted ? randomDelay : 0.01d,
                    longestVisualDuration);
                if (playedThisWave > 0)
                {
                    MarkAudiencePresentation(
                        performance,
                        now,
                        longestVisualDuration);
                }
                performance.TailEndsAtMissionTime = Math.Max(
                    performance.TailEndsAtMissionTime,
                    performance.NextReplyAtMissionTime +
                    performance.Settings.PerformanceTailSeconds);
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_REPLY",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " WaveSize=" + waveSize +
                    " SubmittedThisTick=" + submissionsThisTick +
                    " WaveCompleted=" + waveCompleted +
                    " Played=" + playedThisWave +
                    " NextDelay=" + randomDelay.ToString("F2"));
            }

            // Native battle cries are an independent response channel. They
            // start with the first audience reply wave instead of waiting for
            // every spoken reply to finish.
            if (BattleSpeechFrameworkV2.ShouldPlayAudienceVoices(
                    performance.CombatSpeechMode,
                    stage.AudienceVoicesEnabled) &&
                now >= performance.NextVoiceAtMissionTime &&
                performance.NextVoiceIndex < performance.VoiceAudienceOrdinals.Length)
            {
                int played = 0;
                int attempted = 0;
                while (played < stage.AudienceVoiceWaveSize &&
                       performance.NextVoiceIndex < performance.VoiceAudienceOrdinals.Length)
                {
                    int ordinal = performance.VoiceAudienceOrdinals[
                        performance.NextVoiceIndex++];
                    Agent actor = ResolveAudienceActor(performance, ordinal);
                    attempted++;
                    if (CanPlayAudienceVoice(actor) &&
                        TryPlayAudienceVoice(actor, performance.NextVoiceIndex))
                    {
                        played++;
                    }
                }
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_VOICE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " WavePlayed=" + played +
                    " Attempted=" + attempted +
                    " Remaining=" +
                    (performance.VoiceAudienceOrdinals.Length -
                     performance.NextVoiceIndex));
                performance.NextVoiceAtMissionTime = now +
                    stage.AudienceVoiceWaveIntervalSeconds;
                if (played > 0)
                {
                    MarkAudiencePresentation(performance, now);
                }
            }

            // Written replies and battle cries may overlap the speech. The
            // command gesture and Advance order remain a strict completion
            // boundary so an unfinished speech can never move the formations.
            if (!performance.Completed)
            {
                return;
            }

            // The NPC speech completion is the command boundary. Native
            // battle cries continue in parallel, but they must not delay the
            // speaker's command gesture or the subsequent Advance order.
            if (performance.AdvanceResolved)
            {
                return;
            }
            if (!BattleSpeechFrameworkV2.ShouldIssueTacticalAdvance(
                    performance.Completed,
                    performance.CombatSpeechMode,
                    stage.TacticalAdvanceEnabled))
            {
                performance.AdvanceResolved = true;
                ReleaseHeldSpeaker(performance);
                if (performance.CombatSpeechMode)
                {
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH_TACTIC",
                        "Session=" + performance.OwnerToken.ToString("N") +
                        " TacticSuppressed=CombatSpeechInPlace");
                }
                return;
            }
            if (!performance.CommandSubmitted)
            {
                if (performance.CommandSubmitDeadlineMissionTime <= 0d)
                {
                    performance.CommandSubmitDeadlineMissionTime = now + 3d;
                }
                if (now < performance.NextCommandSubmitAtMissionTime)
                {
                    return;
                }
                if (!TryPlayClosingCommand(
                        performance.Speaker,
                        out string actionId,
                        out string commandError))
                {
                    if (now >= performance.CommandSubmitDeadlineMissionTime)
                    {
                        performance.AdvanceResolved = true;
                        ReleaseHeldSpeaker(performance);
                        SceneActionsLog.Warning(
                            "BATTLE_SPEECH_TACTIC",
                            "Session=" + performance.OwnerToken.ToString("N") +
                            " State=Cancelled Reason=Closing command was rejected for 3 seconds; " +
                            "Advance was not issued. Detail=" + commandError);
                        return;
                    }
                    performance.NextCommandSubmitAtMissionTime = now + 0.1d;
                    return;
                }
                performance.CommandSubmitted = true;
                performance.ClosingCommandAction = _closingCommandActions[actionId];
                performance.ClosingCommandAccepted = true;
                if (!performance.OwnedActionIndices.Any(existing =>
                        existing == performance.ClosingCommandAction))
                {
                    performance.OwnedActionIndices.Add(performance.ClosingCommandAction);
                }
                performance.AdvanceAtMissionTime = Math.Max(
                    now + BattleSpeechFrameworkV2.ResolveClosingCommandDelaySeconds(
                        stage.TacticalAdvanceDelaySeconds),
                    performance.LastAudiencePresentationAtMissionTime);
                performance.TailEndsAtMissionTime = Math.Max(
                    performance.TailEndsAtMissionTime,
                    performance.AdvanceAtMissionTime +
                    performance.Settings.PerformanceTailSeconds);
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_TACTIC",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " State=CommandSubmitted Action=" + actionId + " AdvanceAt=" +
                    performance.AdvanceAtMissionTime.ToString("F2") +
                    " AudienceHoldUntil=" +
                    performance.LastAudiencePresentationAtMissionTime.ToString("F2"));
                return;
            }
            if (now >= performance.AdvanceAtMissionTime)
            {
                // Clear the audience presentation before changing formation AI.
                // A direct Advance on the same tick as a full-body gesture can
                // leave the animation root-motion state active for one or more
                // frames, producing the observed floating/walking transition.
                if (!performance.AudienceCleanupRequested)
                {
                    performance.AudienceCleanupRequested = true;
                    performance.AudienceCleanupReadyAtMissionTime =
                        now + AdvanceAudienceCleanupSettleSeconds;
                    performance.AudienceCleanupDeadlineMissionTime =
                        now + AdvanceAudienceCleanupTimeoutSeconds;
                    performance.AudienceCleanupReleasedChannels +=
                        ReleaseOwnedAudiencePerformanceChannels(performance);
                    CancelTrustedPlaybackOnce(
                        performance,
                        "Advance preflight; audience presentation is ending.");
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH_TACTIC",
                        "Session=" + performance.OwnerToken.ToString("N") +
                        " State=AdvanceCleanupRequested ReleasedChannels=" +
                        performance.AudienceCleanupReleasedChannels +
                        " ReadyAt=" +
                        performance.AudienceCleanupReadyAtMissionTime.ToString("F2"));
                    return;
                }

                performance.AudienceCleanupReleasedChannels +=
                    ReleaseOwnedAudiencePerformanceChannels(performance);
                bool audienceClear = AreOwnedAudienceChannelsClear(performance);
                if (!audienceClear &&
                    now < performance.AudienceCleanupDeadlineMissionTime)
                {
                    return;
                }
                if (!audienceClear)
                {
                    performance.AdvanceResolved = true;
                    ReleaseHeldSpeaker(performance);
                    SceneActionsLog.Warning(
                        "BATTLE_SPEECH_TACTIC",
                        "Session=" + performance.OwnerToken.ToString("N") +
                        " State=AdvanceSuppressed Reason=Audience action channel did not settle before timeout; " +
                        "floating transition risk was blocked.");
                    return;
                }
                if (now < performance.AudienceCleanupReadyAtMissionTime)
                {
                    return;
                }

                performance.AdvanceResolved = true;
                // Release the speaker hold before changing formation AI so the
                // command animation and scripted position cannot overlap the
                // first movement frame either.
                ReleaseHeldSpeaker(performance);
                ApplyPlayerTeamAdvance(performance);
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_PERFORMANCE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " State=AudienceActionsClearedBeforeAdvance ReleasedChannels=" +
                    performance.AudienceCleanupReleasedChannels);

            }
        }

        private static bool AreOwnedAudienceChannelsClear(ActivePerformance performance)
        {
            if (performance == null)
            {
                return true;
            }
            ActionIndexCache[] ownedActions = performance.OwnedActionIndices.ToArray();
            if (ownedActions.Length == 0)
            {
                return true;
            }
            foreach (Agent actor in (performance.Audience ?? Array.Empty<Agent>())
                         .Where(agent => agent != null)
                         .GroupBy(agent => agent.Index)
                         .Select(group => group.First()))
            {
                if (!actor.IsActive() || !ReferenceEquals(actor.Mission, performance.Mission))
                {
                    continue;
                }
                try
                {
                    ActionIndexCache current = actor.GetCurrentAction(1);
                    if (ownedActions.Any(action => action != ActionIndexCache.act_none &&
                                                   action == current))
                    {
                        return false;
                    }
                }
                catch
                {
                    // A disappearing or engine-invalid agent is not allowed to
                    // hold the whole formation at the Advance barrier.
                }
            }
            return true;
        }
        private static bool TryPlayAudienceVoice(Agent actor, int ordinal)
        {
            try
            {
                actor.MakeVoice(
                    ordinal % 2 == 0
                        ? SkinVoiceManager.VoiceType.Victory
                        : SkinVoiceManager.VoiceType.Yell,
                    SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
                return true;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_VOICE",
                    "Native audience voice failed closed for Agent=" + actor?.Index,
                    ex);
                return false;
            }
        }

        private bool TryPlayClosingCommand(
            Agent speaker,
            out string actionId,
            out string error)
        {
            actionId = null;
            error = null;
            if (!IsEligiblePerformanceActor(speaker))
            {
                error = "The speaker is no longer eligible.";
                return false;
            }
            EquipmentIndex wielded = speaker.GetPrimaryWieldedItemIndex();
            bool hasWieldedWeapon = wielded >= EquipmentIndex.WeaponItemBeginSlot &&
                                    wielded < EquipmentIndex.NumAllWeaponSlots;
            actionId = BattleSpeechFrameworkV2.SelectClosingCommandActionId(
                speaker.MountAgent != null,
                hasWieldedWeapon);
            try
            {
                if (!_closingCommandActions.TryGetValue(
                        actionId,
                        out ActionIndexCache action))
                {
                    action = ActionIndexCache.Create(actionId);
                    _closingCommandActions[actionId] = action;
                }
                if (action == ActionIndexCache.act_none)
                {
                    error = "Native action is unavailable: " + actionId;
                    return false;
                }
                bool accepted = speaker.SetActionChannel(
                    1,
                    in action,
                    ignorePriority: false,
                    additionalFlags: 0,
                    blendWithNextActionFactor: 0f,
                    actionSpeed: 1f,
                    blendInPeriod: 0.18f);
                if (!accepted)
                {
                    error = "SetActionChannel returned false for " + actionId;
                }
                return accepted;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private void ApplyPlayerTeamAdvance(ActivePerformance performance)
        {
            Team playerTeam = Mission.PlayerTeam;
            OrderController controller = playerTeam?.PlayerOrderController;
            if (controller == null)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_TACTIC",
                    "Player order controller was unavailable; Advance was not issued.");
                return;
            }
            Formation[] previousSelection = controller.SelectedFormations
                .Where(formation => formation != null)
                .ToArray();
            Formation[] commandedFormations = Array.Empty<Formation>();
            int verified = 0;
            bool gesturesEnabled = controller.BackupAndDisableGesturesEnabled();
            try
            {
                controller.SelectAllFormations(uiFeedback: false);
                commandedFormations = controller.SelectedFormations
                    .Where(formation => formation != null &&
                                        formation.CountOfUnits > 0)
                    .ToArray();
                if (commandedFormations.Length == 0)
                {
                    SceneActionsLog.Warning(
                        "BATTLE_SPEECH_TACTIC",
                        "No selectable player formations were available; Advance was not issued.");
                    return;
                }
                controller.SetOrder(OrderType.Advance);
                verified = commandedFormations.Count(formation =>
                    formation.GetReadonlyMovementOrderReference().OrderEnum ==
                    MovementOrder.MovementOrderEnum.Advance);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_TACTIC",
                    "The player-team Advance command failed closed.",
                    ex);
            }
            finally
            {
                try
                {
                    controller.ClearSelectedFormations();
                    foreach (Formation formation in previousSelection)
                    {
                        if (formation.CountOfUnits > 0 &&
                            controller.IsFormationSelectable(formation))
                        {
                            controller.SelectFormation(formation);
                        }
                    }
                }
                catch (Exception restoreError)
                {
                    SceneActionsLog.Error(
                        "BATTLE_SPEECH_TACTIC",
                        "Advance was issued, but restoring the player's formation selection failed.",
                        restoreError);
                }
                finally
                {
                    controller.RestoreGesturesEnabled(gesturesEnabled);
                }
            }
            SceneActionsLog.Info(
                "BATTLE_SPEECH_TACTIC",
                "Session=" + performance.OwnerToken.ToString("N") +
                " Tactic=Advance CommandedFormations=" + commandedFormations.Length +
                " VerifiedAdvance=" + verified +
                " RestoredSelection=" + previousSelection.Length);
        }

        private void TryHoldSpeakerAtSpeechLine(ActivePerformance performance)
        {
            Agent speaker = performance?.Speaker;
            if (speaker == null || ReferenceEquals(speaker, Mission.MainAgent) ||
                !speaker.IsActive() || Mission.Scene == null)
            {
                return;
            }
            Agent[] audience = performance.Audience ?? Array.Empty<Agent>();
            Agent[] activeAudience = audience
                .Where(agent => agent != null && agent.IsActive() &&
                                ReferenceEquals(agent.Mission, Mission))
                .ToArray();
            if (activeAudience.Length == 0)
            {
                return;
            }
            Vec2 center = new Vec2(
                activeAudience.Average(agent => agent.Position.x),
                activeAudience.Average(agent => agent.Position.y));
            Vec2 facing = center - speaker.Position.AsVec2;
            if (facing.LengthSquared < 0.01f)
            {
                return;
            }
            facing = facing.Normalized();
            try
            {
                TaleWorlds.Library.Vec3 lookDirection =
                    new TaleWorlds.Library.Vec3(facing);
                speaker.SetLookAgent(null);
                speaker.LookDirection = lookDirection;
                TaleWorlds.Engine.WorldPosition position =
                    new TaleWorlds.Engine.WorldPosition(Mission.Scene, speaker.Position);
                speaker.ClearTargetFrame();
                speaker.SetScriptedPositionAndDirection(
                    ref position,
                    facing.RotationInRadians,
                    addHumanLikeDelay: false,
                    Agent.AIScriptedFrameFlags.NoAttack |
                    Agent.AIScriptedFrameFlags.DoNotRun);
                performance.SpeakerHoldOwned = true;
                Agent mount = speaker.MountAgent;
                if (mount != null && mount.IsActive() &&
                    ReferenceEquals(mount.Mission, Mission))
                {
                    try
                    {
                        mount.SetLookAgent(null);
                        mount.LookDirection = lookDirection;
                        Vec2 movementDirection = facing;
                        mount.SetMovementDirection(in movementDirection);
                        mount.ClearTargetFrame();
                        TaleWorlds.Engine.WorldPosition mountPosition =
                            new TaleWorlds.Engine.WorldPosition(Mission.Scene, mount.Position);
                        mount.SetScriptedPositionAndDirection(
                            ref mountPosition,
                            facing.RotationInRadians,
                            addHumanLikeDelay: false);
                        performance.SpeakerHoldMount = mount;
                    }
                    catch (Exception mountError)
                    {
                        SceneActionsLog.Error(
                            "BATTLE_SPEECH_STAGE",
                            "Unable to hold the mount for the closing command; rider hold remains active.",
                            mountError);
                    }
                }
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_STAGE",
                    "Session=" + performance.OwnerToken.ToString("N") +
                    " State=HeldForCommand FacingAudience=" + facing);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_STAGE",
                    "Unable to hold the speaker for the closing command.",
                    ex);
            }
        }

        private static void ReleaseHeldSpeaker(ActivePerformance performance)
        {
            if (performance == null)
            {
                return;
            }
            if (performance.SpeechOpeningAccepted && performance.Speaker != null &&
                performance.Speaker.IsActive())
            {
                SceneActionChannelOwner.TryReleaseOwnedChannelWithContext(
                    performance.Speaker,
                    1,
                    ownershipAccepted: true,
                    diagnosticContext: "BattleSpeechPerformance speech opening",
                    performance.SpeechOpeningAction);
                performance.SpeechOpeningAccepted = false;
                performance.SpeechOpeningAction = ActionIndexCache.act_none;
            }
            if (performance.ClosingCommandAccepted && performance.Speaker != null &&
                performance.Speaker.IsActive())
            {
                SceneActionChannelOwner.TryReleaseOwnedChannelWithContext(
                    performance.Speaker,
                    1,
                    ownershipAccepted: true,
                    diagnosticContext: "BattleSpeechPerformance closing command",
                    performance.ClosingCommandAction);
                performance.ClosingCommandAccepted = false;
                performance.ClosingCommandAction = ActionIndexCache.act_none;
            }
            if (!performance.SpeakerHoldOwned)
            {
                return;
            }
            try
            {
                if (performance.Speaker != null && performance.Speaker.IsActive())
                {
                    performance.Speaker.DisableScriptedMovement();
                }
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_STAGE",
                    "Unable to release the closing-command speaker hold.",
                    ex);
            }
            try
            {
                if (performance.SpeakerHoldMount != null &&
                    performance.SpeakerHoldMount.IsActive())
                {
                    performance.SpeakerHoldMount.DisableScriptedMovement();
                }
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_STAGE",
                    "Unable to release the closing-command mount hold.",
                    ex);
            }
            performance.SpeakerHoldOwned = false;
            performance.SpeakerHoldMount = null;
        }

        private static bool ResponseWorkCompleted(ActivePerformance performance)
        {
            if (!performance.ResponseOpened)
            {
                return true;
            }
            bool voicesDone = !BattleSpeechFrameworkV2.ShouldPlayAudienceVoices(
                                  performance.CombatSpeechMode,
                                  BattleSpeechRuntimeHost.StageSettings.AudienceVoicesEnabled) ||
                              performance.NextVoiceIndex >=
                              performance.VoiceAudienceOrdinals.Length;
            bool repliesDone = !BattleSpeechRuntimeHost.StageSettings.AudienceRepliesEnabled ||
                               performance.NextReplyIndex >=
                               performance.ReplyAudienceOrdinals.Length;
            bool tacticDone = performance.AdvanceResolved;
            return repliesDone && voicesDone && tacticDone;
        }

        private bool HasNearbyEnemyThrottled(Agent speaker)
        {
            return BattleSpeechEnemyProximityCache.HasNearbyEnemy(
                Mission,
                speaker,
                BattleSpeechRuntimeHost.Settings.EnemyInterruptRadiusMeters,
                BattleSpeechRuntimeHost.Settings.EnemyScanIntervalSeconds);
        }

        private bool Matches(BattleSpeechRuntimeContextV1 speech)
        {
            return speech != null &&
                   _active != null &&
                   ReferenceEquals(speech.Mission, Mission) &&
                   speech.Snapshot.SessionId == _active.OwnerToken &&
                   ReferenceEquals(speech.Speaker, _active.Speaker);
        }

        private void FinishTail(string reason)
        {
            ActivePerformance performance = _active;
            if (performance == null)
            {
                return;
            }
            _active = null;
            BattleSpeechRuntimeHost.MarkPerformanceEnded(Mission);
            int released = ReleaseOwnedPerformanceChannels(performance);
            ReleaseHeldSpeaker(performance);
            CancelTrustedPlaybackOnce(performance, reason);
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Session=" + performance.OwnerToken.ToString("N") +
                " State=Completed Reason=" + reason +
                " ReleasedChannels=" + released);
        }

        private static List<ActionIndexCache> ResolvePerformanceActionIndices(
            IEnumerable<RuntimeCue> cues)
        {
            List<ActionIndexCache> result = new List<ActionIndexCache>();
            foreach (RuntimeCue cue in cues ?? Enumerable.Empty<RuntimeCue>())
            {
                if (cue?.Cue == null ||
                    !SceneActionsRuntimeHost.Catalog.TryGetIntent(
                        cue.Cue.IntentKey,
                        out IntentDefinition intent) ||
                    !SceneActionsRuntimeHost.Catalog.TrySelectAction(
                        intent.ActionKey,
                        SceneActionsRuntimeHost.Runtime,
                        SceneActionsRuntimeHost.Settings,
                        out SelectedAction selected,
                        out _))
                {
                    continue;
                }
                foreach (string actionId in selected.Variant.ActionIds ??
                         Array.Empty<string>())
                {
                    try
                    {
                        ActionIndexCache action = ActionIndexCache.Create(actionId);
                        if (action != ActionIndexCache.act_none &&
                            !result.Any(existing => existing == action))
                        {
                            result.Add(action);
                        }
                    }
                    catch
                    {
                        // Force-clear only touches action indexes that the
                        // engine exposed in this Mission. Queued work is still
                        // cancelled through the trusted owner below.
                    }
                }
            }
            return result;
        }

        private static int ReleaseOwnedAudiencePerformanceChannels(
            ActivePerformance performance)
        {
            return ReleaseOwnedPerformanceChannels(performance, includeSpeaker: false);
        }

        private static int ReleaseOwnedPerformanceChannels(
            ActivePerformance performance)
        {
            return ReleaseOwnedPerformanceChannels(performance, includeSpeaker: true);
        }

        private static int ReleaseOwnedPerformanceChannels(
            ActivePerformance performance,
            bool includeSpeaker)
        {
            if (performance == null || performance.OwnedActionIndices.Count == 0)
            {
                return 0;
            }
            int released = 0;
            ActionIndexCache[] ownedActions = performance.OwnedActionIndices.ToArray();
            IEnumerable<Agent> actors = (performance.Audience ?? Array.Empty<Agent>())
                .Concat(includeSpeaker
                    ? new[] { performance.Speaker }
                    : Array.Empty<Agent>())
                .Where(actor => actor != null)
                .GroupBy(actor => actor.Index)
                .Select(group => group.First());
            foreach (Agent actor in actors)
            {
                if (!actor.IsActive() || !ReferenceEquals(actor.Mission, performance.Mission))
                {
                    continue;
                }
                try
                {
                    ActionIndexCache current = actor.GetCurrentAction(1);
                    if (!ownedActions.Any(action => action == current))
                    {
                        continue;
                    }
                    if (SceneActionChannelOwner.TryReleaseOwnedChannelWithContext(
                            actor,
                            1,
                            ownershipAccepted: true,
                            diagnosticContext: includeSpeaker
                                ? "BattleSpeechPerformance finish"
                                : "BattleSpeechPerformance audience advance",
                            ownedActions))
                    {
                        released++;
                    }
                }
                catch
                {
                    // A disappearing agent is already safe to skip.
                }
            }
            return released;
        }

        private void CancelActive(string reason)
        {
            ActivePerformance performance = _active;
            if (performance == null)
            {
                return;
            }
            _active = null;
            BattleSpeechRuntimeHost.MarkPerformanceEnded(Mission);
            int released = ReleaseOwnedPerformanceChannels(performance);
            ReleaseHeldSpeaker(performance);
            CancelTrustedPlaybackOnce(performance, reason);
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Session=" + performance.OwnerToken.ToString("N") +
                " State=Cancelled Reason=" + reason +
                   " ReleasedChannels=" + released);
        }

        private void CancelTrustedPlaybackOnce(
            ActivePerformance performance,
            string reason)
        {
            if (performance == null || performance.TrustedPlaybackCancelled)
            {
                return;
            }
            if (SceneActionsRuntimeHost.TryCancelTrustedPlayback(
                    Mission,
                    performance.OwnerToken,
                    reason))
            {
                performance.TrustedPlaybackCancelled = true;
            }
        }

        private void Close(string reason)
        {
            if (_closed)
            {
                return;
            }
            CancelActive(reason);
            BattleSpeechRuntimeHost.UnbindPerformanceEffect(this);
            BattleSpeechRuntimeHost.MarkPerformanceEnded(Mission);
            BattleSpeechEnemyProximityCache.Reset(Mission);
            _closed = true;
            _registration?.Dispose();
            _registration = null;
            SceneActionsLog.Info(
                "BATTLE_SPEECH_PERFORMANCE",
                "Mission performance effect closed. " + reason);
        }

        /// <summary>
        /// Clears the active presentation while retaining the registered Mission
        /// behavior.  This is deliberately distinct from Close(), which is only
        /// for Mission teardown and disposes the API registration.
        /// </summary>
        internal void DisableFromHost(string reason)
        {
            if (_closed)
            {
                return;
            }
            bool transitioned = !_mcmDisabled;
            _mcmDisabled = true;
            if (_active != null)
            {
                CancelActive(reason ?? "BattleSpeech presentation disabled.");
            }
            else
            {
                BattleSpeechRuntimeHost.MarkPerformanceEnded(Mission);
            }
            if (transitioned)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_MCM",
                    "BattleSpeech performance disabled without closing Mission behavior. Reason=" +
                    (reason ?? string.Empty));
            }
        }

        private sealed class RuntimeCue
        {
            public RuntimeCue(BattleSpeechPerformanceCueV1 cue, bool isAudience)
            {
                Cue = cue;
                IsAudience = isAudience;
            }

            public BattleSpeechPerformanceCueV1 Cue { get; }
            public bool IsAudience { get; }
        }

        private sealed class ActivePerformance
        {
            public Guid OwnerToken;
            public Mission Mission;
            public Agent Speaker;
            public BattleSpeechSpeakerKindV1 SpeakerKind;
            public BattleSpeechPhaseV1 Phase;
            public Agent[] Audience;
            public List<RuntimeCue> Cues;
            public int NextCueIndex;
            public double StartedAtMissionTime;
            public double SpeechEndsAtMissionTime;
            public double TailEndsAtMissionTime;
            public double LastAudiencePresentationAtMissionTime;
            public bool Completed;
            public BattleSpeechTacticV2 Tactic;
            public bool TacticDecisionProvided;
            public bool CombatSpeechMode;
            public BattleSpeechPerformanceSettingsV1 Settings;
            public int[] VoiceAudienceOrdinals = Array.Empty<int>();
            public string[] AudienceReplies = Array.Empty<string>();
            public int[] ReplyAudienceOrdinals = Array.Empty<int>();
            public int FirstVisualWaveTarget;
            public int FinalAudienceSubmitted;
            public bool ResponseOpened;
            public int NextVoiceIndex;
            public double NextVoiceAtMissionTime;
            public int NextReplyIndex;
            public int ReplyWaveIndex;
            public int ReplyWaveRemaining;
            public double NextReplyAtMissionTime;
            public double AdvanceAtMissionTime;
            public bool AdvanceResolved;
            public bool CommandSubmitted;
            public ActionIndexCache ClosingCommandAction = ActionIndexCache.act_none;
            public bool ClosingCommandAccepted;
            public bool TrustedPlaybackCancelled;
            public double NextCommandSubmitAtMissionTime;
            public double CommandSubmitDeadlineMissionTime;
            public bool AudienceCleanupRequested;
            public double AudienceCleanupReadyAtMissionTime;
            public double AudienceCleanupDeadlineMissionTime;
            public int AudienceCleanupReleasedChannels;
            public bool SpeakerHoldOwned;
            public Agent SpeakerHoldMount;
            public bool SpeakerGestureSkippedLogged;
            public bool SpeakerCueSuppressedByOpeningLogged;
            public ActionIndexCache SpeechOpeningAction = ActionIndexCache.act_none;
            public bool SpeechOpeningAccepted;
            public List<ActionIndexCache> OwnedActionIndices =
                new List<ActionIndexCache>();
        }
    }
}
