using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Per-NPC frozen consent coordination. This boundary owns registration, local/AI
    /// consent resolution, request matching, and single-speaker consumption.
    /// </summary>
    internal sealed partial class SceneActionsMissionBehavior
    {
        private void RegisterPendingNpcConsents(
            CapturedSceneActionEvent captured,
            ParseDecision decision,
            IntentDefinition intent,
            TargetMode mode,
            double now)
        {
            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            if (!TryValidateProgramReady(
                decision.ProgramV4,
                out ExecutionResultCode programFailure,
                out string programReason))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    programFailure,
                    programReason + " Consent was not requested.");
                return;
            }

            CleanupPendingConsents(now);
            List<Agent> targets = ResolveTargets(captured, mode);
            if (targets.Count == 0)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.NoTarget,
                    "The consent target snapshot is empty or invalid.");
                return;
            }

            int permittedCount = targets.Count;
            if (targets.Count > settings.MaxBatchTargets)
            {
                if (settings.OverflowPolicy == SchedulerOverflowPolicy.Reject)
                {
                    FinishAcceptedRequestWithoutTargets(
                        captured.EventId,
                        ExecutionResultCode.BatchTooLarge,
                        "Consent target count exceeds maxBatchTargets.");
                    return;
                }
                permittedCount = settings.MaxBatchTargets;
            }

            List<SessionAgentHandle> handles = targets.Take(permittedCount)
                .Select(agent => new SessionAgentHandle
                {
                    SessionGeneration = _sessionGeneration,
                    Mission = Mission,
                    Agent = agent,
                    AgentIndex = agent.Index
                })
                .ToList();
            int additionalEntries = handles.Count(handle =>
            {
                string targetKey = MakeConsentTargetKey(handle);
                return !_pendingConsents.TryGet(
                    targetKey,
                    _sessionGeneration,
                    now,
                    out _);
            });
            int availableEntries = Math.Max(
                0,
                settings.MaxPendingRequests - _pendingConsents.Count);
            if (additionalEntries > availableEntries &&
                settings.OverflowPolicy == SchedulerOverflowPolicy.Reject)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.QueueFull,
                    "Pending NPC consent capacity is insufficient for the whole target snapshot.");
                return;
            }

            int registered = 0;
            int remainingNewSlots = availableEntries;
            double expiresAt = captured.SubmittedAtMissionTime +
                               (settings.ConsentReplyTtlMs / 1000d);
            foreach (SessionAgentHandle handle in handles)
            {
                string targetKey = MakeConsentTargetKey(handle);
                bool replacing = _pendingConsents.TryGet(
                    targetKey,
                    _sessionGeneration,
                    now,
                    out _);
                if (!replacing && remainingNewSlots <= 0)
                {
                    break;
                }
                if (!replacing)
                {
                    remainingNewSlots--;
                }

                FrozenConsentRequest frozen = new FrozenConsentRequest(
                    captured.EventId,
                    targetKey,
                    decision.ProgramV4,
                    _sessionGeneration,
                    captured.SubmittedAtMissionTime,
                    expiresAt);
                FrozenConsentRequest replaced = _pendingConsents.Register(frozen);
                _pendingConsentHandles[targetKey] = handle;
                registered++;
                SceneActionsLog.Info(
                    "CONSENT",
                    "RequestId=" + captured.EventId.ToString("N") +
                    " SessionGeneration=" + _sessionGeneration +
                    " Agent=" + handle.AgentIndex +
                    " Program=" + decision.ProgramV4.ProtocolExpression +
                    " State=AwaitingReply" +
                    (replaced == null
                        ? string.Empty
                        : " ReplacedRequestId=" + replaced.RequestId.ToString("N")));
            }

            if (registered == 0)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.QueueFull,
                    "No pending NPC consent slot was available.");
                return;
            }
            FinishAcceptedRequestWithoutTargets(
                captured.EventId,
                ExecutionResultCode.AwaitingConsent,
                "Frozen consent requests registered for " + registered +
                " of " + targets.Count + " target(s).",
                logAsInformation: true);
        }

        private bool ResolvePendingNpcConsent(
            CapturedSceneActionEvent captured,
            double now)
        {
            if (!TryGetPendingNpcConsent(
                captured.Speaker,
                now,
                out FrozenConsentRequest pending,
                out _))
            {
                return false;
            }

            if (ConsentReplyInterpreter.TryResolveLocal(
                captured.RawText,
                out ConsentDecision localDecision))
            {
                ApplyConsentDecision(
                    captured,
                    pending,
                    localDecision,
                    ResolverSource.NpcConsentLocal,
                    now);
                return true;
            }

            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            if (!settings.AiClassifierEnabled)
            {
                ApplyConsentDecision(
                    captured,
                    pending,
                    ConsentDecision.Unclear,
                    ResolverSource.NpcConsentLocal,
                    now);
                return true;
            }
            if (!SceneActionsRuntimeHost.TryGetConsentClassifier(
                settings.AiClassifierProviderId,
                out IAuxiliaryConsentClassifierV1 classifier))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.ClassifierUnavailable,
                    "Configured provider has no closed-set consent classifier.");
                return true;
            }

            StartConsentClassification(captured, pending, classifier);
            return true;
        }

        private void ApplyConsentDecision(
            CapturedSceneActionEvent captured,
            FrozenConsentRequest frozen,
            ConsentDecision decision,
            ResolverSource resolver,
            double now)
        {
            if (!TryGetPendingNpcConsent(
                    captured.Speaker,
                    now,
                    out FrozenConsentRequest current,
                    out _) ||
                current.RequestId != frozen.RequestId ||
                !string.Equals(
                    current.ProgramExpression,
                    frozen.ProgramExpression,
                    StringComparison.Ordinal))
            {
                FinishNoAction(
                    captured.EventId,
                    "Consent result no longer matches the speaker's frozen request.");
                return;
            }

            if (decision == ConsentDecision.Unclear)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.ConsentUnclear,
                    "NPC reply did not clearly accept or refuse; frozen request remains pending.",
                    logAsInformation: true);
                return;
            }

            if (!_pendingConsents.TryConsume(
                current.TargetKey,
                current.RequestId,
                _sessionGeneration,
                now,
                out FrozenConsentRequest consumed))
            {
                FinishNoAction(captured.EventId, "Frozen consent request changed before consumption.");
                return;
            }
            _pendingConsentHandles.Remove(consumed.TargetKey);

            if (decision == ConsentDecision.Refuse)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.ConsentRefused,
                    "NPC explicitly refused the frozen request.",
                    logAsInformation: true);
                return;
            }

            ParseDecision accepted = ParseDecision.MatchProgramV4(
                consumed.ProgramV4,
                null,
                resolver);
            BuildAndQueuePlans(captured, accepted, now);
        }

        private bool TryGetPendingNpcConsent(
            Agent speaker,
            double now,
            out FrozenConsentRequest request,
            out SessionAgentHandle handle)
        {
            request = null;
            handle = null;
            if (speaker == null || speaker.Index < 0)
            {
                return false;
            }

            string targetKey = MakeConsentTargetKey(_sessionGeneration, speaker.Index);
            if (!_pendingConsents.TryGet(
                targetKey,
                _sessionGeneration,
                now,
                out request))
            {
                _pendingConsentHandles.Remove(targetKey);
                return false;
            }
            if (!_pendingConsentHandles.TryGetValue(targetKey, out handle) ||
                handle.SessionGeneration != _sessionGeneration ||
                !ReferenceEquals(handle.Mission, Mission) ||
                !ReferenceEquals(handle.Agent, speaker) ||
                handle.AgentIndex != speaker.Index)
            {
                _pendingConsents.TryRemove(targetKey, out _);
                _pendingConsentHandles.Remove(targetKey);
                request = null;
                handle = null;
                return false;
            }
            return true;
        }

        private void ConsumePendingConsentForSpeaker(
            Agent speaker,
            double now,
            string reason)
        {
            if (!TryGetPendingNpcConsent(
                speaker,
                now,
                out FrozenConsentRequest current,
                out SessionAgentHandle handle))
            {
                return;
            }
            if (_pendingConsents.TryConsume(
                current.TargetKey,
                current.RequestId,
                _sessionGeneration,
                now,
                out _))
            {
                _pendingConsentHandles.Remove(current.TargetKey);
                SceneActionsLog.Info(
                    "CONSENT",
                    "RequestId=" + current.RequestId.ToString("N") +
                    " SessionGeneration=" + _sessionGeneration +
                    " Agent=" + handle.AgentIndex +
                    " Intent=" + current.IntentKey +
                    " State=SupersededByActualAction Reason=" + reason);
            }
        }
    }
}
