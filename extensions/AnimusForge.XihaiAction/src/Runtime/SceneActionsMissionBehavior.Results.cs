using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        private void CleanupPendingConsents(double now)
        {
            foreach (FrozenConsentRequest expired in _pendingConsents.RemoveExpired(
                _sessionGeneration,
                now))
            {
                _pendingConsentHandles.Remove(expired.TargetKey);
                SceneActionsLog.Info(
                    "CONSENT",
                    "RequestId=" + expired.RequestId.ToString("N") +
                    " SessionGeneration=" + _sessionGeneration +
                    " Intent=" + expired.IntentKey +
                    " State=Expired");
            }
        }
        private void RemovePendingConsentForAgent(Agent agent, string reason)
        {
            if (agent == null || agent.Index < 0)
            {
                return;
            }
            string targetKey = MakeConsentTargetKey(_sessionGeneration, agent.Index);
            if (!_pendingConsentHandles.TryGetValue(
                    targetKey,
                    out SessionAgentHandle handle) ||
                !ReferenceEquals(handle.Agent, agent))
            {
                return;
            }
            if (_pendingConsents.TryRemove(
                targetKey,
                out FrozenConsentRequest removed))
            {
                SceneActionsLog.Info(
                    "CONSENT",
                    "RequestId=" + removed.RequestId.ToString("N") +
                    " SessionGeneration=" + _sessionGeneration +
                    " Agent=" + agent.Index +
                    " Intent=" + removed.IntentKey +
                    " State=Cancelled Reason=" + reason);
            }
            _pendingConsentHandles.Remove(targetKey);
        }
        private void FinishPlan(
            PlannedTarget plan,
            ExecutionResultCode result,
            string reason)
        {
            if (plan == null ||
                !_trackers.TryGetValue(plan.RequestId, out RequestTracker tracker) ||
                !tracker.TryRecordTerminal(plan.TargetKey, result))
            {
                return;
            }
            SceneActionsLog.Info(
                "RESULT",
                FormatPlan(plan) + " Result=" + result + " Reason=" + (reason ?? string.Empty));
            if (tracker.IsComplete)
            {
                CompleteTracker(tracker);
            }
        }
        private void CompleteTracker(RequestTracker tracker)
        {
            _trackers.Remove(tracker.RequestId);
            _requestGate.Complete(tracker.RequestId);
            SceneActionsLog.Info(
                "BATCH",
                "RequestId=" + tracker.RequestId.ToString("N") +
                " SessionGeneration=" + _sessionGeneration +
                " Requested=" + tracker.Requested +
                " Accepted=" + tracker.Accepted +
                " Skipped=" + tracker.Skipped +
                " Failed=" + tracker.Failed +
                " Cancelled=" + tracker.Cancelled);
            if (SceneActionsRuntimeHost.Settings.ScreenBatchSummary)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "SceneActions: " + tracker.Accepted + " accepted, " +
                    tracker.Failed + " failed, " + tracker.Skipped + " skipped.",
                    new Color(0.7f, 0.7f, 0.7f, 1f)));
            }
        }
        private void FinishAcceptedRequestWithoutTargets(
            Guid requestId,
            ExecutionResultCode result,
            string reason,
            bool logAsInformation = false)
        {
            _requestGate.Complete(requestId);
            string message = "RequestId=" + requestId.ToString("N") +
                             " SessionGeneration=" + _sessionGeneration +
                             " Result=" + result +
                             " Reason=" + (reason ?? string.Empty);
            if (logAsInformation)
            {
                SceneActionsLog.Info("RESULT", message);
            }
            else
            {
                SceneActionsLog.Warning("RESULT", message);
            }
        }
        private void FinishNoAction(Guid requestId, string reason)
        {
            _requestGate.Complete(requestId);
            SceneActionsLog.Info(
                "RESULT",
                "RequestId=" + requestId.ToString("N") +
                " SessionGeneration=" + _sessionGeneration +
                " Result=NoAction Reason=" + (reason ?? string.Empty));
        }
        private void AbortRequest(
            Guid requestId,
            ExecutionResultCode result,
            string reason)
        {
            if (_trackers.TryGetValue(requestId, out RequestTracker tracker))
            {
                foreach (KeyValuePair<string, SessionAgentHandle> target in
                         tracker.UnfinishedTargets.ToArray())
                {
                    FinishPlan(new PlannedTarget
                    {
                        RequestId = tracker.RequestId,
                        InputSource = tracker.InputSource,
                        Resolver = tracker.Resolver,
                        Intent = new IntentDefinition { Key = tracker.IntentKey },
                        Handle = target.Value,
                        TargetKey = target.Key
                    }, result, reason);
                }
                return;
            }
            _requestGate.Complete(requestId);
            SceneActionsLog.Error(
                "RESULT",
                "RequestId=" + requestId.ToString("N") +
                " SessionGeneration=" + _sessionGeneration +
                " Result=" + result + " Reason=" + reason);
        }
        private void LogRequestOnly(Guid requestId, ExecutionResultCode result, string reason)
        {
            SceneActionsLog.Warning(
                "GATE",
                "RequestId=" + requestId.ToString("N") +
                " SessionGeneration=" + _sessionGeneration +
                " Result=" + result + " Reason=" + reason);
        }
        private string FormatPlan(PlannedTarget plan)
        {
            if (plan == null)
            {
                return "SessionGeneration=" + _sessionGeneration;
            }
            return "RequestId=" + plan.RequestId.ToString("N") +
                   " SessionGeneration=" + _sessionGeneration +
                   " InputSource=" + plan.InputSource +
                   " Resolver=" + plan.Resolver +
                   " Intent=" + plan.Intent.Key +
                   " Provider=" + (plan.SelectedAction?.Definition.ProviderId ?? "owned-state") +
                   " Agent=" + plan.Handle.AgentIndex +
                   " Action=" + (plan.FrozenActionId ?? plan.SelectedAction?.Definition.Key ?? "exit");
        }
        private static string MakeTargetKey(SessionAgentHandle handle, int ordinal)
        {
            return handle.SessionGeneration + ":" + handle.AgentIndex + ":" + ordinal;
        }
        private static string MakeConsentTargetKey(SessionAgentHandle handle)
        {
            return MakeConsentTargetKey(handle.SessionGeneration, handle.AgentIndex);
        }
        private static string MakeConsentTargetKey(long sessionGeneration, int agentIndex)
        {
            return sessionGeneration + ":" + agentIndex;
        }
    }
}