using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        internal bool TryEnqueueTrustedOneShot(TrustedOneShotRequest request)
        {
            if (request == null ||
                !IsSessionActive ||
                request.RequestId == Guid.Empty ||
                request.OwnerToken == Guid.Empty ||
                !ReferenceEquals(request.Mission, Mission) ||
                request.Target == null ||
                !ReferenceEquals(request.Target.Mission, Mission))
            {
                return false;
            }
            _trustedOneShots.Enqueue(request);
            return true;
        }

        internal bool TryEnqueueTrustedCancellation(TrustedPlaybackCancellation cancellation)
        {
            if (cancellation == null ||
                !IsSessionActive ||
                cancellation.OwnerToken == Guid.Empty ||
                !ReferenceEquals(cancellation.Mission, Mission))
            {
                return false;
            }
            _trustedCancellations.Enqueue(cancellation);
            return true;
        }

        private void DrainTrustedCancellations()
        {
            while (_trustedCancellations.TryDequeue(
                       out TrustedPlaybackCancellation cancellation))
            {
                if (cancellation == null ||
                    cancellation.OwnerToken == Guid.Empty ||
                    !ReferenceEquals(cancellation.Mission, Mission))
                {
                    continue;
                }
                _cancelledTrustedOwners.Add(cancellation.OwnerToken);
                foreach (OwnedLoopState loop in _ownedLoops.Values
                             .Where(value =>
                                 value != null &&
                                 value.OwnerToken == cancellation.OwnerToken)
                             .ToArray())
                {
                    Agent agent = loop.Handle?.Agent;
                    if (agent != null && ReferenceEquals(agent.Mission, Mission))
                    {
                        ReleaseOwnedLoopForAgent(agent, true);
                    }
                }
                SceneActionsLog.Info(
                    "TRUSTED",
                    "Owner=" + cancellation.OwnerToken.ToString("N") +
                    " State=Cancelled Reason=" + (cancellation.Reason ?? string.Empty));
            }
        }

        private void DrainTrustedOneShots(double now)
        {
            while (_trustedOneShots.TryDequeue(out TrustedOneShotRequest request))
            {
                QueueTrustedOneShot(request, now);
            }
        }

        private void QueueTrustedOneShot(TrustedOneShotRequest request, double now)
        {
            if (request == null ||
                _cancelledTrustedOwners.Contains(request.OwnerToken) ||
                !ReferenceEquals(request.Mission, Mission) ||
                request.Target == null ||
                !ReferenceEquals(request.Target.Mission, Mission) ||
                !BattleSpeechPerformancePlannerV1.IsTrustedOneShotIntent(request.IntentKey))
            {
                SceneActionsLog.Warning(
                    "TRUSTED",
                    "Rejected trusted one-shot before request-gate admission.");
                return;
            }
            if (!_requestGate.TryAccept(
                    request.RequestId,
                    request.SubmittedAtMissionTime,
                    now,
                    SceneActionsRuntimeHost.Settings,
                    out ExecutionResultCode gateFailure))
            {
                LogRequestOnly(
                    request.RequestId,
                    gateFailure,
                    "Trusted one-shot request gate rejected the request.");
                return;
            }
            if (!SceneActionsRuntimeHost.Catalog.TryGetIntent(
                    request.IntentKey,
                    out IntentDefinition intent) ||
                intent.Kind != IntentKind.PlayAction)
            {
                FinishAcceptedRequestWithoutTargets(
                    request.RequestId,
                    ExecutionResultCode.InvalidCommand,
                    "Trusted one-shot intent is absent or not a play action.");
                return;
            }
            if (!SceneActionsRuntimeHost.Catalog.TrySelectAction(
                    intent.ActionKey,
                    SceneActionsRuntimeHost.Runtime,
                    SceneActionsRuntimeHost.Settings,
                    out SelectedAction selected,
                    out ExecutionResultCode selectionFailure))
            {
                FinishAcceptedRequestWithoutTargets(
                    request.RequestId,
                    selectionFailure,
                    "Trusted one-shot action is not enabled for this runtime.");
                return;
            }
            if (selected.Definition.Mode != ActionMode.OneShot &&
                selected.Definition.Mode != ActionMode.RandomGroup)
            {
                FinishAcceptedRequestWithoutTargets(
                    request.RequestId,
                    ExecutionResultCode.InvalidCommand,
                    "Trusted performance accepts only OneShot or RandomGroup actions.");
                return;
            }
            if (_scheduler.Count >= _scheduler.Capacity)
            {
                FinishAcceptedRequestWithoutTargets(
                    request.RequestId,
                    ExecutionResultCode.QueueFull,
                    "Trusted one-shot scheduler is full.");
                return;
            }

            SessionAgentHandle handle = new SessionAgentHandle
            {
                SessionGeneration = _sessionGeneration,
                Mission = Mission,
                Agent = request.Target,
                AgentIndex = request.Target.Index
            };
            RequestTracker tracker = new RequestTracker(
                request.RequestId,
                request.IntentKey,
                ResolverSource.BattleSpeechSemantic,
                SceneInputSource.BattleSpeechPerformance,
                new[] { handle });
            _trackers.Add(request.RequestId, tracker);
            PlannedTarget plan = new PlannedTarget
            {
                RequestId = request.RequestId,
                OwnerToken = request.OwnerToken,
                InputSource = SceneInputSource.BattleSpeechPerformance,
                Resolver = ResolverSource.BattleSpeechSemantic,
                Intent = intent,
                SelectedAction = selected,
                Handle = handle,
                TargetKey = MakeTargetKey(handle, 0),
                TargetOrdinal = 0,
                QueuedAtMissionTime = now,
                ExpiresAtMissionTime = request.SubmittedAtMissionTime +
                    (SceneActionsRuntimeHost.Settings.RequestTtlMs / 1000d)
            };
            IReadOnlyList<string> actionIds = selected.Variant.ActionIds;
            if (string.Equals(
                    request.DiagnosticSource,
                    "battle-speech-audience",
                    StringComparison.Ordinal) &&
                string.Equals(
                    request.IntentKey,
                    SceneActionFrameworkV4.Cheer,
                    StringComparison.Ordinal))
            {
                string[] standardCheerActions = actionIds
                    .Where(actionId => actionId != null &&
                                       actionId.StartsWith(
                                           "act_cheer_",
                                           StringComparison.Ordinal))
                    .ToArray();
                if (standardCheerActions.Length > 0)
                {
                    actionIds = standardCheerActions;
                }
            }
            int variantIndex = selected.Definition.Mode == ActionMode.RandomGroup
                ? DeterministicSelector.PickIndex(
                    request.RequestId.ToString("N"),
                    handle.StableId,
                    0,
                    actionIds.Count)
                : 0;
            plan.FrozenActionId = actionIds[variantIndex];
            if (!_scheduler.TryEnqueue(now, plan, out long sequence))
            {
                FinishPlan(plan, ExecutionResultCode.QueueFull, "Trusted scheduler enqueue failed.");
                return;
            }
            plan.StableSequence = sequence;
            SceneActionsLog.Info(
                "TRUSTED",
                FormatPlan(plan) +
                " Owner=" + request.OwnerToken.ToString("N") +
                " Source=" + (request.DiagnosticSource ?? string.Empty) +
                " Result=Queued");
        }

        private bool IsTrustedPlaybackBlocked(
            PlannedTarget plan,
            Agent agent,
            out string reason)
        {
            reason = null;
            if (plan == null || plan.OwnerToken == Guid.Empty || agent == null)
            {
                return false;
            }
            if (_ownedStates.TryGetValue(agent.Index, out OwnedActionState state) &&
                ReferenceEquals(state.Handle?.Agent, agent))
            {
                reason = "Battle speech performance will not replace an owned state action.";
                return true;
            }
            if (_programExecutions.Values.Any(execution =>
                    execution != null &&
                    ReferenceEquals(execution.Handle?.Agent, agent)))
            {
                reason = "Battle speech performance will not interrupt an active action program.";
                return true;
            }
            if (_ownedLoops.TryGetValue(agent.Index, out OwnedLoopState loop) &&
                ReferenceEquals(loop.Handle?.Agent, agent))
            {
                if (loop.OwnerToken == plan.OwnerToken)
                {
                    return false;
                }
                reason = "Battle speech performance will not replace another SceneActions request.";
                return true;
            }
            return false;
        }
    }
}
