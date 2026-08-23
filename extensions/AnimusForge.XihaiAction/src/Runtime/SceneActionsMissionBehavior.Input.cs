using AnimusForge.SceneActions.Core;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Input and permission routing for a Mission session. Keeping this boundary separate
    /// makes source/target authority reviewable without mixing it with playback state.
    /// </summary>
    internal sealed partial class SceneActionsMissionBehavior
    {
        private void ProcessCapturedEvent(CapturedSceneActionEvent captured, double now)
        {
            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            if (!SceneActionInputRouter.IsEnabled(settings, captured.InputSource))
            {
                return;
            }
            if (!ReferenceEquals(captured.SourceMission, Mission))
            {
                LogRequestOnly(captured.EventId, ExecutionResultCode.MissionChanged, "source Mission mismatch");
                return;
            }
            if (!_requestGate.TryAccept(
                captured.EventId,
                captured.SubmittedAtMissionTime,
                now,
                settings,
                out ExecutionResultCode gateFailure))
            {
                LogRequestOnly(captured.EventId, gateFailure, "request gate rejected the event");
                return;
            }

            if (SceneActionInputRouter.IsPlayer(captured.InputSource))
            {
                ProcessPlayerCapturedEvent(captured, settings, now);
            }
            else
            {
                ProcessNpcCapturedEvent(captured, settings, now);
            }
        }

        private void ProcessPlayerCapturedEvent(
            CapturedSceneActionEvent captured,
            SceneActionSettings settings,
            double now)
        {
            RememberRecentPlayerContext(captured, now);
            ParseDecision decision = SceneActionsRuntimeHost.Parser.ParsePlayerText(
                captured.RawText,
                settings);
            if (decision.Status == ParseStatus.Invalid)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    decision.Error);
                return;
            }
            if (decision.Status == ParseStatus.Matched)
            {
                RouteResolvedPlayerIntent(captured, decision, now);
                return;
            }
            if (decision.StopResolution)
            {
                FinishNoAction(
                    captured.EventId,
                    "Starred stage text is not eligible for AI fallback.");
                return;
            }
            if (!decision.AiFallbackRequested)
            {
                FinishNoAction(
                    captured.EventId,
                    "The deterministic parser did not request AI fallback.");
                return;
            }
            if (!settings.AiClassifierEnabled)
            {
                FinishNoAction(
                    captured.EventId,
                    "No deterministic action and AI classifier is disabled.");
                return;
            }
            StartClassification(captured, decision, now, false, null);
        }

        private void ProcessNpcCapturedEvent(
            CapturedSceneActionEvent captured,
            SceneActionSettings settings,
            double now)
        {
            string previousPlayerText = ConsumeRecentPlayerContext(
                captured.Speaker,
                now);
            ParseDecision decision = SceneActionsRuntimeHost.Parser.ParseNpcReplyText(
                captured.RawText,
                captured.Speaker?.Name);
            if (decision.Status == ParseStatus.Invalid)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    decision.Error);
                return;
            }
            if (decision.Status == ParseStatus.Matched)
            {
                ConsumePendingConsentForSpeaker(
                    captured.Speaker,
                    now,
                    "NPC reply contained an actual action description.");
                BuildAndQueuePlans(captured, decision, now);
                return;
            }
            if (decision.AiFallbackRequested)
            {
                // When a stage direction contains several explicit performed
                // cues, do not let a lossy/partial AF classifier response erase
                // one of them. The parser only receives the extracted stage
                // text, so this remains closed-set and does not infer from
                // ordinary dialogue.
                if (SceneActionsRuntimeHost.Parser.TryBuildDeterministicNpcProgram(
                        decision.ClassifierText,
                        captured.Speaker?.Name,
                        out ActionProgramV4 explicitProgram))
                {
                    ParseDecision explicitDecision = ParseDecision.MatchProgramV4(
                        explicitProgram,
                        null,
                        ResolverSource.NpcStageDirection);
                    ConsumePendingConsentForSpeaker(
                        captured.Speaker,
                        now,
                        "NPC reply contained multiple explicit action cues.");
                    BuildAndQueuePlans(captured, explicitDecision, now);
                    SceneActionsLog.Info(
                        "NPC_ACTION",
                        "Explicit multi-action stage direction was frozen locally. " +
                        "Program=" + explicitProgram.ProtocolExpression);
                    return;
                }
                bool hasPendingConsent = TryGetPendingNpcConsent(
                    captured.Speaker,
                    now,
                    out _,
                    out _);
                if (TryResolveImplicitEmotion(
                    captured,
                    decision,
                    previousPlayerText,
                    out ParseDecision inferredEmotion))
                {
                    ConsumePendingConsentForSpeaker(
                        captured.Speaker,
                        now,
                        "NPC reply resolved to a context-inferred emotion expression.");
                    BuildAndQueuePlans(captured, inferredEmotion, now);
                    return;
                }
                if (settings.AiClassifierEnabled)
                {
                    StartClassification(
                        captured,
                        decision,
                        now,
                        hasPendingConsent,
                        previousPlayerText);
                    return;
                }
                if (hasPendingConsent && ResolvePendingNpcConsent(captured, now))
                {
                    return;
                }
                FinishNoAction(
                    captured.EventId,
                    "NPC action description needs AI fallback, but it is disabled.");
                return;
            }
            if (decision.StopResolution)
            {
                FinishNoAction(
                    captured.EventId,
                    "NPC stage directions contained no single whitelisted action.");
                return;
            }
            if (ResolvePendingNpcConsent(captured, now))
            {
                return;
            }

            FinishNoAction(
                captured.EventId,
                "NPC reply had no action and no pending request for this speaker.");
        }

        private void RouteResolvedPlayerIntent(
            CapturedSceneActionEvent captured,
            ParseDecision decision,
            double now)
        {
            if (decision?.ProgramV4 == null)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Resolved decision has no frozen V4 action program.");
                return;
            }
            string routingIntentKey = decision.ProgramV4.Steps[0].IntentKeys[0];
            if (!SceneActionsRuntimeHost.Catalog.TryGetIntent(
                routingIntentKey,
                out IntentDefinition intent))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Resolved intent is absent from the catalog.");
                return;
            }

            if (!SceneActionPermissionRouter.TryResolveTargetMode(
                    decision,
                    intent,
                    out TargetMode mode))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.InvalidCommand,
                    "Resolved intent or decision is missing; permission routing failed closed.");
                return;
            }
            if (!SceneActionPermissionRouter.RequiresNpcConsent(decision, mode))
            {
                // A new player command supersedes the battle-speech audience
                // reaction immediately. This is intentionally performed only
                // after a closed-set action has been resolved, so ordinary
                // prose does not scan or touch every audience agent.
                BattleSpeechRuntimeHost.TryForceStopPerformanceForPlayerCommand(
                    Mission,
                    captured.RawText);
                BuildAndQueuePlans(captured, decision, now);
                return;
            }
            RegisterPendingNpcConsents(captured, decision, intent, mode, now);
        }

        // Compatibility anchor retained for the offline verifier and older reflection
        // integrations. Policy decisions still live in SceneActionPermissionRouter.
        private static bool ShouldRegisterNpcConsent(
            ParseDecision decision,
            TargetMode mode)
        {
            return SceneActionPermissionRouter.RequiresNpcConsent(decision, mode);
        }
    }
}
