using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class BattleSpeechMissionBehavior
    {
        private const double MovementReassertIntervalSeconds = 0.25d;
        private const double MovementStallReassertDelaySeconds = 1.25d;
        private const float MovementProgressEpsilonMeters = 0.08f;
        private const float FacingCorrectionDotThreshold = 0.94f;
        private const float FacingRefreshDotThreshold = 0.985f;
        private const double FacingRefreshIntervalSeconds = 1.0d;
        private const double PlanClassificationPlaybackBudgetSeconds = 1d;

        private readonly ConcurrentQueue<BattleSpeechTriggerCompletionV2>
            _triggerCompletions = new ConcurrentQueue<BattleSpeechTriggerCompletionV2>();
        private readonly ConcurrentQueue<BattleSpeechPlanCompletionV2>
            _planCompletions = new ConcurrentQueue<BattleSpeechPlanCompletionV2>();
        private readonly CancellationTokenSource _v2LifetimeCancellation =
            new CancellationTokenSource();
        // Unlike the Mission lifetime token, this token is reset when the
        // BattleSpeech MCM switch is toggled off.  That cancels an in-flight AF
        // request without making a later re-enable permanently unusable.
        private CancellationTokenSource _v2RequestCancellation =
            new CancellationTokenSource();
        private long _triggerGeneration;

        private void StartTriggerClassification(BattleSpeechCapturedInputV1 input)
        {
            BattleSpeechStageSettingsV2 settings = BattleSpeechRuntimeHost.StageSettings;
            if (settings == null || !settings.NaturalTriggerEnabled ||
                !settings.TriggerClassifierEnabled ||
                !SceneActionsRuntimeHost.TryGetBattleSpeechClassifier(
                    settings.ClassifierProviderId,
                    out IBattleSpeechClassifierV2 classifier))
            {
                return;
            }
            long generation = _triggerGeneration;
            BattleSpeechCapturedInputV1 frozen = new BattleSpeechCapturedInputV1
            {
                InputKind = input.InputKind,
                Mission = input.Mission,
                RawText = input.RawText,
                Player = input.Player,
                PrimaryTarget = input.PrimaryTarget,
                FramedTargets = input.FramedTargets?.ToArray() ?? Array.Empty<Agent>(),
                ConversationEpoch = input.ConversationEpoch,
                SubmittedAtMissionTime = input.SubmittedAtMissionTime,
                OriginalAfBehavior = input.OriginalAfBehavior,
                OriginalExtraFact = input.OriginalExtraFact,
                OriginalForcedPrimaryAgentIndex = input.OriginalForcedPrimaryAgentIndex
            };
            BattleSpeechTriggerClassifierRequestV2 request =
                new BattleSpeechTriggerClassifierRequestV2
                {
                    RequestId = Guid.NewGuid(),
                    PlayerText = frozen.RawText,
                    HasPrimaryNpcTarget = frozen.PrimaryTarget != null &&
                                          !ReferenceEquals(frozen.PrimaryTarget, frozen.Player)
                };
            _ = RunTriggerClassificationAsync(
                classifier,
                request,
                frozen,
                generation,
                settings.ClassifierTimeoutMs);
        }

        private async Task RunTriggerClassificationAsync(
            IBattleSpeechClassifierV2 classifier,
            BattleSpeechTriggerClassifierRequestV2 request,
            BattleSpeechCapturedInputV1 input,
            long generation,
            int timeoutMs)
        {
            BattleSpeechTriggerCompletionV2 completion = new BattleSpeechTriggerCompletionV2
            {
                Input = input,
                Generation = generation
            };
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
                       _v2LifetimeCancellation.Token,
                       _v2RequestCancellation.Token))
            {
                timeout.CancelAfter(timeoutMs);
                try
                {
                    completion.RawOutput = await classifier.ClassifyBattleSpeechTriggerAsync(
                            request,
                            timeout.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    completion.Error = "Trigger classifier timed out or was cancelled.";
                }
                catch (Exception ex)
                {
                    completion.Error = ex.GetType().Name + ": " + ex.Message;
                }
            }
            _triggerCompletions.Enqueue(completion);
        }

        private void PrepareSpeech(ActiveBattleSpeechSessionV1 session, string speechText)
        {
            if (!ReferenceEquals(session, _active))
            {
                return;
            }
            PrepareSpeechPlan(session, speechText);
            // Player speech must wait for the dedicated AF plan instead of
            // falling back after the one-second NPC staging grace period.
            // NPC speech keeps the grace period only after reaching its line,
            // so a slow classifier cannot hold an NPC's scripted position
            // indefinitely.
            if (session.PlanClassificationPending &&
                session.SpeakerKind == BattleSpeechSpeakerKindV1.Npc &&
                session.ReachedSpeechLine)
            {
                session.PlanClassificationPlaybackDeadlineMissionTime =
                    Mission.CurrentTime + PlanClassificationPlaybackBudgetSeconds;
            }
            if (session.PlanClassificationPending ||
                (session.SpeakerKind == BattleSpeechSpeakerKindV1.Npc &&
                 !session.ReachedSpeechLine))
            {
                session.PendingSpeechText = (speechText ?? string.Empty).Trim();
                return;
            }
            BeginSpeaking(session, speechText);
        }

        private void PrepareSpeechPlan(ActiveBattleSpeechSessionV1 session, string speechText)
        {
            if (!ReferenceEquals(session, _active) ||
                session.PlanClassificationPending ||
                session.PlanClassificationCompleted)
            {
                return;
            }
            BattleSpeechFrameworkV2.TryResolveLocalActionProgram(
                speechText,
                out ActionProgramV4 localProgram,
                out _);
            session.ActionProgram = localProgram;
            session.Tactic = BattleSpeechTacticV2.None;

            BattleSpeechStageSettingsV2 settings = BattleSpeechRuntimeHost.StageSettings;
            int replyCount = BattleSpeechFrameworkV2.ResolveAudienceReplyCount(
                settings != null && settings.AudienceRepliesEnabled,
                settings?.AudienceReplyCount ?? 0,
                session.Audience?.Length ?? 0);
            session.AudienceReplies = BattleSpeechFrameworkV2
                .BuildFallbackAudienceReplies(
                    speechText,
                    replyCount,
                    settings?.AudienceReplyMinimumChars ?? 8,
                    settings?.AudienceReplyMaximumChars ?? 24)
                .ToArray();
            if (settings == null || !settings.SemanticClassifierEnabled ||
                !SceneActionsRuntimeHost.TryGetBattleSpeechClassifier(
                    settings.ClassifierProviderId,
                    out IBattleSpeechClassifierV2 classifier))
            {
                session.PlanClassificationCompleted = true;
                return;
            }

            string[] allowed = GetEnabledOneShotIntentKeys();
            if (allowed.Length == 0)
            {
                session.PlanClassificationCompleted = true;
                return;
            }
            session.PlanClassificationPending = true;
            session.ClassificationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _v2LifetimeCancellation.Token,
                _v2RequestCancellation.Token);
            session.ClassificationCancellation.CancelAfter(settings.ClassifierTimeoutMs);
            BattleSpeechPlanClassifierRequestV2 request = new BattleSpeechPlanClassifierRequestV2
            {
                RequestId = session.SessionId,
                SpeechText = speechText,
                AllowedIntentKeys = allowed,
                AllowAdvance = false,
                AudienceReplyCount = replyCount,
                AudienceReplyMinimumChars = settings.AudienceReplyMinimumChars,
                AudienceReplyMaximumChars = settings.AudienceReplyMaximumChars
            };
            _ = RunPlanClassificationAsync(
                classifier,
                request,
                session.SessionId,
                session.Generation,
                allowed,
                session.ClassificationCancellation.Token);
        }

        private async Task RunPlanClassificationAsync(
            IBattleSpeechClassifierV2 classifier,
            BattleSpeechPlanClassifierRequestV2 request,
            Guid sessionId,
            long generation,
            string[] allowed,
            CancellationToken cancellationToken)
        {
            BattleSpeechPlanCompletionV2 completion = new BattleSpeechPlanCompletionV2
            {
                SessionId = sessionId,
                Generation = generation,
                AllowedIntentKeys = allowed
            };
            try
            {
                completion.RawOutput = await classifier.ClassifyBattleSpeechPlanAsync(
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                completion.Error = "Speech plan classifier timed out or was cancelled.";
            }
            catch (Exception ex)
            {
                completion.Error = ex.GetType().Name + ": " + ex.Message;
            }
            _planCompletions.Enqueue(completion);
        }

        private void ProcessV2ClassifierCompletions()
        {
            while (_triggerCompletions.TryDequeue(out BattleSpeechTriggerCompletionV2 trigger))
            {
                if (trigger.Generation != _triggerGeneration ||
                    trigger.Input == null ||
                    !ReferenceEquals(trigger.Input.Mission, Mission))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(trigger.Error) ||
                    !BattleSpeechFrameworkV2.TryParseTriggerClassifierOutput(
                        trigger.RawOutput,
                        out BattleSpeechTriggerKindV2 kind))
                {
                    SceneActionsLog.Warning(
                        "BATTLE_SPEECH_CLASSIFIER",
                        "Trigger classification failed closed. " +
                        (trigger.Error ?? "Invalid closed-set output."));
                    ReplayOrdinaryAfShout(trigger.Input);
                    continue;
                }
                ApplyClassifiedTrigger(kind, trigger.Input);
            }

            while (_planCompletions.TryDequeue(out BattleSpeechPlanCompletionV2 plan))
            {
                ActiveBattleSpeechSessionV1 session = _active;
                if (session == null || session.SessionId != plan.SessionId ||
                    session.Generation != plan.Generation)
                {
                    continue;
                }
                if (!session.PlanClassificationPending &&
                    session.PlanClassificationCompleted)
                {
                    continue;
                }
                session.PlanClassificationPending = false;
                session.PlanClassificationCompleted = true;
                session.ClassificationCancellation?.Dispose();
                session.ClassificationCancellation = null;
                string parseError = null;
                if (string.IsNullOrEmpty(plan.Error) &&
                    BattleSpeechFrameworkV2.TryParsePlanClassifierOutput(
                        plan.RawOutput,
                        BattleSpeechRuntimeHost.StageSettings.AudienceReplyMinimumChars,
                        BattleSpeechRuntimeHost.StageSettings.AudienceReplyMaximumChars,
                        out BattleSpeechPlanDecisionV2 decision,
                        out parseError) &&
                    ProgramUsesOnly(decision.ActionProgram, plan.AllowedIntentKeys))
                {
                    if (decision.ActionProgram != null)
                    {
                        session.ActionProgram = decision.ActionProgram;
                    }
                    int maximumReplies = BattleSpeechFrameworkV2.ResolveAudienceReplyCount(
                        BattleSpeechRuntimeHost.StageSettings.AudienceRepliesEnabled,
                        BattleSpeechRuntimeHost.StageSettings.AudienceReplyCount,
                        session.Audience?.Length ?? 0);
                    bool modelRepliesMatchFrozenCount =
                        decision.AudienceReplies.Count == maximumReplies;
                    if (modelRepliesMatchFrozenCount && maximumReplies > 0)
                    {
                        session.AudienceReplies = decision.AudienceReplies
                            .ToArray();
                    }
                    session.Tactic = BattleSpeechTacticV2.None;
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH_CLASSIFIER",
                        "Session=" + session.SessionId.ToString("N") +
                        " Actions=" + (session.ActionProgram?.ProtocolExpression ?? "NONE") +
                        " Tactic=" + session.Tactic +
                        " AudienceReplies=" + session.AudienceReplies.Length +
                        (modelRepliesMatchFrozenCount ? " Source=AF" : " Source=LocalFallback"));
                }
                else
                {
                    SceneActionsLog.Warning(
                        "BATTLE_SPEECH_CLASSIFIER",
                        "Speech plan classification failed closed. Session=" +
                        session.SessionId.ToString("N") + " Reason=" +
                        (plan.Error ?? parseError ?? "Output selected a disabled action."));
                }
                TryResumePendingSpeech(session);
            }
        }

        private void ApplyClassifiedTrigger(
            BattleSpeechTriggerKindV2 kind,
            BattleSpeechCapturedInputV1 input)
        {
            if (kind == BattleSpeechTriggerKindV2.None ||
                kind == BattleSpeechTriggerKindV2.OrdinaryScene)
            {
                ReplayOrdinaryAfShout(input);
                return;
            }
            if (!TryResolvePhase(out BattleSpeechPhaseV1 phase))
            {
                ReplayOrdinaryAfShout(input);
                return;
            }
            Agent player = input.Player ?? Mission.MainAgent ?? Agent.Main;
            if (kind == BattleSpeechTriggerKindV2.ArmPlayerSpeech)
            {
                ActiveBattleSpeechSessionV1 session = StartSession(
                    player,
                    BattleSpeechSpeakerKindV1.Player,
                    phase,
                    BattleSpeechSessionStateV1.AwaitingPlayerSpeech,
                    input.ConversationEpoch,
                    input.RawText,
                    BattleSpeechRuntimeHost.Settings.PlayerCaptureSeconds);
                // Direct troop-facing prose is already the player's speech. The
                // classifier only resolves whether it is a speech request; any
                // PLAYER_SPEECH result is therefore the frozen body itself and
                // must not turn this one-shot input into a second-input arm state.
                if (session != null)
                {
                    PrepareSpeech(session, input.RawText);
                }
            }
            else
            {
                // RequestNpcSpeech is reserved for the Y-menu internal route and
                // can never be produced by the T-key trigger classifier. Treat any
                // unexpected value as an ordinary AF shout rather than allowing a
                // classifier response to retarget an NPC.
                ReplayOrdinaryAfShout(input);
            }
        }

        private static void ReplayOrdinaryAfShout(BattleSpeechCapturedInputV1 input)
        {
            if (!AfCompatV130.TryReplayOriginalPlayerShout(
                    input,
                    observeForBattleSpeech: false,
                    out string replayError))
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_COMPAT",
                    "Classifier fell back to ordinary AF, but replay failed: " +
                    replayError);
            }
            else
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Classifier routed input back to the original AF scene chain.");
            }
        }

        private void EnterCombatSpeechMode(ActiveBattleSpeechSessionV1 session)
        {
            if (session == null || session.CombatSpeechMode)
            {
                return;
            }
            session.CombatSpeechMode = true;
            session.ReachedSpeechLine = true;
            ReleaseOwnedScriptedMovement(session);
            SceneActionsLog.Info(
                "BATTLE_SPEECH_STAGE",
                "Session=" + session.SessionId.ToString("N") +
                " State=CombatInPlace NoScriptedMovement=True");
        }

        private void InitializeV2Stage(ActiveBattleSpeechSessionV1 session)
        {
            session.ReachedSpeechLine = session.SpeakerKind == BattleSpeechSpeakerKindV1.Player;
            session.PlanClassificationCompleted = false;
            if (session.CombatSpeechMode)
            {
                session.ReachedSpeechLine = true;
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_STAGE",
                    "Session=" + session.SessionId.ToString("N") +
                    " State=CombatInPlace NoScriptedMovement=True");
                return;
            }
            if (session.SpeakerKind != BattleSpeechSpeakerKindV1.Npc ||
                !BattleSpeechRuntimeHost.StageSettings.NpcPositioningEnabled ||
                !TryBuildSpeechLine(
                    session.Speaker,
                    out Vec3 center,
                    out Vec2 lineDirection,
                    out Vec2 audienceFacing))
            {
                session.ReachedSpeechLine = true;
                return;
            }
            session.SpeechLineCenter = center;
            session.SpeechLineDirection = lineDirection;
            session.AudienceFacingDirection = audienceFacing;
            session.MovementStartPosition = session.Speaker.Position;
            session.LastMovementProgressPosition = session.Speaker.Position;
            session.LastMovementProgressMissionTime = Mission.CurrentTime;
            session.MovementDeadlineMissionTime = Mission.CurrentTime +
                BattleSpeechRuntimeHost.StageSettings.MovementTimeoutSeconds;
            session.NextMovementReassertMissionTime = Mission.CurrentTime +
                MovementReassertIntervalSeconds;
            if (TrySetScriptedSpeechPosition(session, center, faceAudience: false))
            {
                session.ScriptedMovementOwned = true;
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_STAGE",
                    "Session=" + session.SessionId.ToString("N") +
                    " State=MovingToFront Start=" + session.MovementStartPosition +
                    " Target=" + center +
                    " Distance=" + session.MovementStartPosition.AsVec2
                        .Distance(center.AsVec2).ToString("F2") +
                    " Mounted=" + (session.Speaker.MountAgent != null) +
                    " Controller=" + session.Speaker.Controller +
                    " WasPaused=" + session.SpeakerWasAiPaused);
            }
            else
            {
                session.ReachedSpeechLine = true;
                ReleaseOwnedScriptedMovement(session);
            }
        }

        private bool ProgressV2Stage(ActiveBattleSpeechSessionV1 session, double now)
        {
            // Combat mode is sticky for the whole speech.  Once the first
            // contact is detected, the performance layer suppresses visual
            // presentation, battle cries and Advance; do not keep polling
            // the shared proximity cache for the remainder of this session.
            if (!session.CombatSpeechMode &&
                (IsSpeakerInCombatAction(session.Speaker) ||
                 HasNearbyEnemyThrottled(session.Speaker)))
            {
                EnterCombatSpeechMode(session);
            }
            if (session.SpeakerKind != BattleSpeechSpeakerKindV1.Npc)
            {
                // Player speech has no positioning phase. Do not expire its
                // plan after one second: the speech stays pending until the
                // AF plan completes or its configured classifier timeout
                // produces the normal closed-failure fallback.
                TryResumePendingSpeech(session);
                return true;
            }
            if (!session.ReachedSpeechLine)
            {
                Vec2 currentPosition = session.Speaker.Position.AsVec2;
                if (currentPosition.DistanceSquared(
                        session.LastMovementProgressPosition.AsVec2) >=
                    MovementProgressEpsilonMeters * MovementProgressEpsilonMeters)
                {
                    session.LastMovementProgressPosition = session.Speaker.Position;
                    session.LastMovementProgressMissionTime = now;
                }
                float radius = BattleSpeechRuntimeHost.StageSettings.ArrivalRadiusMeters;
                bool arrived = session.Speaker.Position.AsVec2.DistanceSquared(
                                   session.SpeechLineCenter.AsVec2) <= radius * radius;
                if (!arrived && now < session.MovementDeadlineMissionTime)
                {
                    if (now >= session.NextMovementReassertMissionTime &&
                        now - session.LastMovementProgressMissionTime >=
                            MovementStallReassertDelaySeconds)
                    {
                        session.NextMovementReassertMissionTime = now +
                                MovementReassertIntervalSeconds;
                        if (TrySetScriptedSpeechPosition(
                                session,
                                session.SpeechLineCenter,
                                faceAudience: false))
                            {
                                session.MovementReassertCount++;
                                session.LastMovementProgressMissionTime = now;
                            }
                    }
                    return false;
                }
                session.ReachedSpeechLine = true;
                if (!arrived)
                {
                    AnchorTimedOutSpeakerAtCurrentPosition(session);
                }
                RefreshAudienceFacing(session, now, true);
                TryAnchorSpeakerFacing(session, session.Speaker.Position);
                if (session.PlanClassificationPending &&
                    session.PlanClassificationPlaybackDeadlineMissionTime <= 0d)
                {
                    session.PlanClassificationPlaybackDeadlineMissionTime = now +
                        PlanClassificationPlaybackBudgetSeconds;
                }
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_STAGE",
                    "Session=" + session.SessionId.ToString("N") +
                    (arrived ? " State=AtSpeechLine" : " State=MovementTimedOut") +
                    " Current=" + session.Speaker.Position +
                    " Remaining=" + session.Speaker.Position.AsVec2
                        .Distance(session.SpeechLineCenter.AsVec2).ToString("F2") +
                    " Travelled=" + session.Speaker.Position.AsVec2
                        .Distance(session.MovementStartPosition.AsVec2).ToString("F2") +
                    " Reasserts=" + session.MovementReassertCount +
                    " Mounted=" + (session.Speaker.MountAgent != null) +
                    " Controller=" + session.Speaker.Controller +
                    " Paused=" + session.Speaker.IsPaused);
            }

            // A generated body can finish semantic classification on the same
            // tick that TryResumePendingSpeech transitions the session to
            // Speaking.  Release the deferred AF visual/TTS payload both before
            // and after that transition; the second attempt is required when
            // classification completes during this very tick.
            if (!TryReleaseDeferredNpcReply(session))
            {
                return false;
            }

            ExpirePlanClassificationWaitIfNeeded(session, now);
            TryResumePendingSpeech(session);
            if (!TryReleaseDeferredNpcReply(session))
            {
                return false;
            }
            if (session.State == BattleSpeechSessionStateV1.Speaking)
            {
                RefreshAudienceFacing(session, now, false);
            }
            return true;
        }

        private bool TryReleaseDeferredNpcReply(
            ActiveBattleSpeechSessionV1 session)
        {
            if (!ReferenceEquals(session, _active) ||
                session.PlanClassificationPending ||
                session.DeferredReplayRequested ||
                (session.State != BattleSpeechSessionStateV1.AwaitingNpcReply &&
                 session.State != BattleSpeechSessionStateV1.Speaking))
            {
                return true;
            }
            if (!BattleSpeechRuntimeHost.TryTakeDeferredNpcReply(
                    session.SessionId,
                    out DeferredNpcReplyV2 deferred))
            {
                return true;
            }
            session.DeferredReplayRequested = true;
            if (!AfCompatV130.TryReplayDeferredReply(deferred, out string error))
            {
                CancelActive("Deferred AF reply replay failed: " + error);
                return false;
            }
            SceneActionsLog.Info(
                "BATTLE_SPEECH_STAGE",
                "Session=" + session.SessionId.ToString("N") +
                " State=ReplyReleased ContentLength=" +
                (deferred.Content ?? string.Empty).Length);
            return true;
        }

        private void RefreshAudienceFacing(
            ActiveBattleSpeechSessionV1 session,
            double now,
            bool force)
        {
            const double refreshIntervalSeconds = FacingRefreshIntervalSeconds;
            if (session.AudienceFacingRefreshFailed ||
                (!force && now < session.NextAudienceFacingRefreshMissionTime))
            {
                return;
            }
            session.NextAudienceFacingRefreshMissionTime = now + refreshIntervalSeconds;

            Agent[] audience = session.Audience;
            if (audience == null || audience.Length == 0)
            {
                return;
            }
            float x = 0f;
            float y = 0f;
            int count = 0;
            foreach (Agent soldier in audience)
            {
                if (soldier == null || !soldier.IsActive() ||
                    !ReferenceEquals(soldier.Mission, Mission))
                {
                    continue;
                }
                x += soldier.Position.x;
                y += soldier.Position.y;
                count++;
            }
            if (count == 0)
            {
                return;
            }

            Vec2 facing = new Vec2(
                (x / count) - session.Speaker.Position.x,
                (y / count) - session.Speaker.Position.y);
            if (facing.LengthSquared < 0.01f)
            {
                return;
            }
            Vec2 desiredFacing = facing.Normalized();
            session.AudienceFacingDirection = desiredFacing;
            Vec2 currentFacing = session.Speaker.LookDirection.AsVec2;
            bool currentFacingAligned = currentFacing.LengthSquared > 0.01f &&
                Vec2.DotProduct(currentFacing.Normalized(), desiredFacing) >=
                FacingCorrectionDotThreshold;
            bool desiredFacingChanged = !session.HasAppliedAudienceFacing ||
                Vec2.DotProduct(
                    session.LastAppliedAudienceFacingDirection,
                    desiredFacing) < FacingRefreshDotThreshold;
            if (currentFacingAligned && !desiredFacingChanged)
            {
                return;
            }
            try
            {
                ApplySpeakerAndMountFacing(session);
                session.LastAppliedAudienceFacingDirection = desiredFacing;
                session.HasAppliedAudienceFacing = true;
            }
            catch (Exception ex)
            {
                session.AudienceFacingRefreshFailed = true;
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_STAGE",
                    "Unable to keep the speaker facing the audience.",
                    ex);
            }
        }

        private void TryAnchorSpeakerFacing(
            ActiveBattleSpeechSessionV1 session,
            Vec3 position)
        {
            if (!session.ScriptedMovementOwned)
            {
                return;
            }
            if (!session.SpeechLineFacingAnchored &&
                TrySetScriptedSpeechPosition(session, position, faceAudience: true))
            {
                session.SpeechLineFacingAnchored = true;
            }
            // The movement target is installed once when the speaker arrives.
            // Never reinstall it while speaking: repeated scripted-position
            // refreshes make mounted agents visibly blink and restart movement.
            RefreshAudienceFacing(session, Mission.CurrentTime, true);
            SceneActionsLog.Info(
                "BATTLE_SPEECH_STAGE",
                "Session=" + session.SessionId.ToString("N") +
                " State=FacingAudience Direction=" + session.AudienceFacingDirection);
        }

        private bool TryBuildSpeechLine(
            Agent speaker,
            out Vec3 center,
            out Vec2 lineDirection,
            out Vec2 audienceFacing)
        {
            center = speaker?.Position ?? Vec3.Zero;
            lineDirection = Vec2.Side;
            audienceFacing = Vec2.Forward;
            Agent[] soldiers = Mission.PlayerTeam?.ActiveAgents
                .Where(agent => agent != null && agent.IsActive() && agent.IsHuman &&
                                !ReferenceEquals(agent, speaker))
                .ToArray() ?? Array.Empty<Agent>();
            if (speaker == null || soldiers.Length == 0 || Mission.Scene == null)
            {
                return false;
            }
            Vec2 centroid = new Vec2(
                soldiers.Average(agent => agent.Position.x),
                soldiers.Average(agent => agent.Position.y));
            Vec2 directionSum = Vec2.Zero;
            foreach (Agent soldier in soldiers)
            {
                Vec2 direction = soldier.Formation != null
                    ? soldier.Formation.Direction
                    : soldier.LookDirection.AsVec2;
                if (direction.LengthSquared > 0.01f)
                {
                    directionSum += direction.Normalized();
                }
            }
            Vec2 forward;
            if (speaker.Formation != null &&
                speaker.Formation.Direction.LengthSquared > 0.01f)
            {
                forward = speaker.Formation.Direction.Normalized();
            }
            else
            {
                Vec2 awayFromAudience = speaker.Position.AsVec2 - centroid;
                forward = awayFromAudience.LengthSquared > 0.01f
                    ? awayFromAudience.Normalized()
                    : directionSum.LengthSquared > 0.01f
                        ? directionSum.Normalized()
                        : speaker.LookDirection.AsVec2.Normalized();
            }
            if (forward.LengthSquared < 0.01f)
            {
                forward = Vec2.Forward;
            }
            Vec3 candidate = new Vec3(
                speaker.Position.AsVec2 +
                forward * BattleSpeechRuntimeHost.StageSettings.FrontDistanceMeters,
                speaker.Position.z);
            if (!TryProjectToNavMesh(candidate, out center))
            {
                return false;
            }
            lineDirection = new Vec2(-forward.y, forward.x).Normalized();
            Vec2 towardAudience = centroid - center.AsVec2;
            audienceFacing = towardAudience.LengthSquared > 0.01f
                ? towardAudience.Normalized()
                : -forward;
            return true;
        }

        private bool TryProjectToNavMesh(Vec3 candidate, out Vec3 projected)
        {
            projected = candidate;
            try
            {
                candidate.z = Mission.Scene.GetGroundHeightAtPosition(candidate);
                WorldPosition world = new WorldPosition(Mission.Scene, candidate);
                if (world.GetNearestNavMesh() == UIntPtr.Zero)
                {
                    return false;
                }
                projected = world.GetNavMeshVec3();
                return projected.AsVec2.DistanceSquared(candidate.AsVec2) <= 36f;
            }
            catch
            {
                return false;
            }
        }

        private bool TrySetScriptedSpeechPosition(
            ActiveBattleSpeechSessionV1 session,
            Vec3 target,
            bool faceAudience)
        {
            try
            {
                if (!session.SpeakerAiPauseCaptured)
                {
                    session.SpeakerAiPauseCaptured = true;
                    session.SpeakerWasAiPaused = session.Speaker.IsPaused;
                }
                if (session.Speaker.IsPaused)
                {
                    session.Speaker.SetIsAIPaused(false);
                    session.SpeakerAiPauseChanged = true;
                }
                // Clear a combat AI target before installing our scripted frame.
                // Otherwise the engine can reacquire the enemy between ticks and
                // turn the speaker away from the frozen audience.
                session.Speaker.ClearTargetFrame();
                session.Speaker.SetLookAgent(null);
                session.Speaker.SetMaximumSpeedLimit(-1f, isMultiplier: false);
                Vec2 scriptedFacing = session.AudienceFacingDirection;
                if (!faceAudience)
                {
                    Vec2 travelDirection = target.AsVec2 -
                                           session.Speaker.Position.AsVec2;
                    if (travelDirection.LengthSquared > 0.01f)
                    {
                        scriptedFacing = travelDirection.Normalized();
                    }
                }
                WorldPosition position = new WorldPosition(Mission.Scene, target);
                session.Speaker.SetScriptedPositionAndDirection(
                    ref position,
                    scriptedFacing.RotationInRadians,
                    addHumanLikeDelay: false,
                    Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
                return true;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_STAGE",
                    "Unable to set the NPC speech position.",
                    ex);
                return false;
            }
        }

        private void AnchorTimedOutSpeakerAtCurrentPosition(
            ActiveBattleSpeechSessionV1 session)
        {
            Vec3 current = session.Speaker.Position;
            if (TryProjectToNavMesh(current, out Vec3 projected))
            {
                current = projected;
            }
            session.SpeechLineCenter = current;
            if (!TrySetScriptedSpeechPosition(session, current, faceAudience: true))
            {
                ReleaseOwnedScriptedMovement(session);
            }
            else
            {
                session.SpeechLineFacingAnchored = true;
            }
        }

        private void ExpirePlanClassificationWaitIfNeeded(
            ActiveBattleSpeechSessionV1 session,
            double now)
        {
            if (!session.PlanClassificationPending ||
                session.PlanClassificationPlaybackDeadlineMissionTime <= 0d ||
                now < session.PlanClassificationPlaybackDeadlineMissionTime)
            {
                return;
            }
            session.PlanClassificationPending = false;
            session.PlanClassificationCompleted = true;
            try
            {
                session.ClassificationCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            session.ClassificationCancellation?.Dispose();
            session.ClassificationCancellation = null;
            SceneActionsLog.Warning(
                "BATTLE_SPEECH_CLASSIFIER",
                "Session=" + session.SessionId.ToString("N") +
                " playback wait exceeded 1 second; using the local action and short-reply fallback.");
        }

        private void TryResumePendingSpeech(ActiveBattleSpeechSessionV1 session)
        {
            if (!ReferenceEquals(session, _active) || session.PlanClassificationPending ||
                (session.SpeakerKind == BattleSpeechSpeakerKindV1.Npc &&
                 !session.ReachedSpeechLine) ||
                string.IsNullOrWhiteSpace(session.PendingSpeechText))
            {
                return;
            }
            string speech = session.PendingSpeechText;
            session.PendingSpeechText = null;
            BeginSpeaking(session, speech);
        }

        private void CleanupV2Session(ActiveBattleSpeechSessionV1 session)
        {
            BattleSpeechRuntimeHost.ClearNpcReplyClaim(session.SessionId);
            try
            {
                session.ClassificationCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            session.ClassificationCancellation?.Dispose();
            session.ClassificationCancellation = null;
            ReleaseOwnedScriptedMovement(session);
        }

        private static void ReleaseOwnedScriptedMovement(
            ActiveBattleSpeechSessionV1 session)
        {
            if (session.ScriptedMovementOwned && session.Speaker != null &&
                session.Speaker.IsActive())
            {
                try
                {
                    session.Speaker.DisableScriptedMovement();
                }
                catch (Exception ex)
                {
                    SceneActionsLog.Error(
                        "BATTLE_SPEECH_STAGE",
                        "Failed to release owned scripted movement.",
                    ex);
                }
            }
            if (session.SpeakerAiPauseChanged && session.Speaker != null &&
                session.Speaker.IsActive())
            {
                try
                {
                    Mission speakerMission = session.Speaker.Mission;
                    if (session.SpeakerWasAiPaused && speakerMission != null &&
                        speakerMission.Mode == MissionMode.Deployment &&
                        !speakerMission.IsDeploymentFinished)
                    {
                        session.Speaker.SetIsAIPaused(true);
                    }
                }
                catch (Exception ex)
                {
                    SceneActionsLog.Error(
                        "BATTLE_SPEECH_STAGE",
                        "Failed to restore the speaker AI pause state.",
                        ex);
                }
            }
            session.ScriptedMovementOwned = false;
            session.SpeakerAiPauseChanged = false;
        }

        private static void ApplySpeakerAndMountFacing(
            ActiveBattleSpeechSessionV1 session)
        {
            Vec3 lookDirection = new Vec3(session.AudienceFacingDirection);
            session.Speaker.ClearTargetFrame();
            session.Speaker.SetLookAgent(null);
            session.Speaker.LookDirection = lookDirection;
            Agent mount = session.Speaker.MountAgent;
            if (session.MountFacingUnavailable || mount == null || !mount.IsActive() ||
                !ReferenceEquals(mount.Mission, session.Speaker.Mission))
            {
                return;
            }
            try
            {
                mount.ClearTargetFrame();
                mount.SetLookAgent(null);
                mount.LookDirection = lookDirection;
                Vec2 movementDirection = session.AudienceFacingDirection;
                mount.SetMovementDirection(in movementDirection);
            }
            catch (Exception ex)
            {
                session.MountFacingUnavailable = true;
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_STAGE",
                    "Mount facing failed; rider facing remains active.",
                    ex);
            }
        }

        private string[] GetEnabledOneShotIntentKeys()
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
                .ToArray();
        }

        private static bool ProgramUsesOnly(
            ActionProgramV4 program,
            IEnumerable<string> allowed)
        {
            if (program == null)
            {
                return true;
            }
            HashSet<string> allowSet = new HashSet<string>(
                allowed ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            return program.Steps.SelectMany(step => step.IntentKeys)
                .All(allowSet.Contains);
        }

        private void CloseV2Lifetime()
        {
            try
            {
                _v2LifetimeCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            _v2LifetimeCancellation.Dispose();
            try
            {
                _v2RequestCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            _v2RequestCancellation.Dispose();
            while (_triggerCompletions.TryDequeue(out _)) { }
            while (_planCompletions.TryDequeue(out _)) { }
        }

        private void ResetV2RequestCancellation()
        {
            CancellationTokenSource previous = _v2RequestCancellation;
            try
            {
                previous.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            previous.Dispose();
            _v2RequestCancellation = new CancellationTokenSource();
        }

        private sealed class BattleSpeechTriggerCompletionV2
        {
            public BattleSpeechCapturedInputV1 Input;
            public long Generation;
            public string RawOutput;
            public string Error;
        }

        private sealed class BattleSpeechPlanCompletionV2
        {
            public Guid SessionId;
            public long Generation;
            public string[] AllowedIntentKeys;
            public string RawOutput;
            public string Error;
        }
    }
}
