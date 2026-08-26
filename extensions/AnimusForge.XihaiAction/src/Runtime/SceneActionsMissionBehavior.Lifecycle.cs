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
        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            if (!SceneActionsRuntimeHost.IsInitialized ||
                SceneActionsRuntimeHost.Settings == null ||
                SceneActionsRuntimeHost.Providers == null)
            {
                SceneActionsLog.Warning(
                    "SESSION",
                    "Mission behavior stayed disabled because the composition root is unavailable.");
                return;
            }

            _sessionGeneration = Interlocked.Increment(ref _generationAllocator);
            _sessionCancellation = new CancellationTokenSource();
            _scheduler = new SceneActionScheduleQueue(
                SceneActionsRuntimeHost.Settings.MaxQueuedTargets);
            _providerSession = SceneActionsRuntimeHost.Providers.CreateMissionSession();
            _requestGate = new RequestGate();
            Volatile.Write(ref _closed, 0);
            SceneActionsRuntimeHost.BindSession(this);
            SceneActionsLog.Info(
                "SESSION",
                "Mission session activated. SessionGeneration=" + _sessionGeneration);
        }
        public bool TryEnqueueCapturedEvent(CapturedSceneActionEvent captured)
        {
            if (captured == null ||
                Volatile.Read(ref _closed) != 0 ||
                captured.EventId == Guid.Empty ||
                !ReferenceEquals(captured.SourceMission, Mission))
            {
                return false;
            }
            _inbound.Enqueue(captured);
            return true;
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Volatile.Read(ref _closed) != 0 || !ReferenceEquals(Mission.Current, Mission))
            {
                return;
            }

            try
            {
                double now = Mission.CurrentTime;
                // MCM reflection is deliberately sampled at most once per second.  It
                // runs before any queue drain so a transition from enabled -> disabled
                // can atomically discard ordinary work before it reaches playback.
                SceneActionsRuntimeHost.RefreshMcmOverridesIfDue(now);
                BattleSpeechRuntimeHost.RefreshMcmOverridesIfDue(now);
                DrainInbound(now);
                DrainTrustedCancellations();
                DrainTrustedOneShots(now);
                DrainClassifierCompletions(now);
                DrainConsentClassifierCompletions(now);
                CleanupPendingConsents(now);
                while (_scheduler.TryDequeueDue(now, out ScheduledItem<PlannedTarget> item))
                {
                    try
                    {
                        Execute(item.Value, now);
                    }
                    catch (Exception ex)
                    {
                        FinishPlan(
                            item.Value,
                            ExecutionResultCode.ExecutorException,
                            ex.GetType().Name + ": " + ex.Message);
                    }
                }
                ProgressActionPrograms(now);
                ProgressOwnedStates(now);
                ProgressOwnedLoops(now);
                CleanupCooldowns(now);
                CleanupRecentPlayerContexts(now);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error("SESSION", "Mission tick failed closed.", ex);
            }
        }
        public override void OnAgentRemoved(
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            ReleaseOwnedStateForAgent(
                affectedAgent,
                ExecutionResultCode.AgentInactive,
                "AgentRemoved");
            CancelProgramForAgent(
                affectedAgent,
                ExecutionResultCode.AgentInactive,
                "AgentRemoved");
            ReleaseOwnedLoopForAgent(affectedAgent, false);
            RemovePendingConsentForAgent(affectedAgent, "AgentRemoved");
            RemoveRecentPlayerContext(affectedAgent);
        }
        public override void OnAgentDeleted(Agent affectedAgent)
        {
            base.OnAgentDeleted(affectedAgent);
            ReleaseOwnedStateForAgent(
                affectedAgent,
                ExecutionResultCode.AgentNotFound,
                "AgentDeleted");
            CancelProgramForAgent(
                affectedAgent,
                ExecutionResultCode.AgentNotFound,
                "AgentDeleted");
            ReleaseOwnedLoopForAgent(affectedAgent, false);
            RemovePendingConsentForAgent(affectedAgent, "AgentDeleted");
            RemoveRecentPlayerContext(affectedAgent);
        }
        protected override void OnEndMission()
        {
            CloseSession("OnEndMission");
            base.OnEndMission();
        }
        public override void OnRemoveBehavior()
        {
            CloseSession("OnRemoveBehavior");
            base.OnRemoveBehavior();
        }
        private void DrainInbound(double now)
        {
            while (_inbound.TryDequeue(out CapturedSceneActionEvent captured))
            {
                try
                {
                    ProcessCapturedEvent(captured, now);
                }
                catch (Exception ex)
                {
                    AbortRequest(
                        captured.EventId,
                        ExecutionResultCode.ExecutorException,
                        ex.GetType().Name + ": " + ex.Message);
                }
            }
        }
        private void CloseSession(string origin)
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0)
            {
                return;
            }
            SceneActionsRuntimeHost.UnbindSession(this);
            try
            {
                _sessionCancellation?.Cancel();
            }
            catch
            {
            }

            while (_inbound.TryDequeue(out _))
            {
            }
            while (_classifierCompletions.TryDequeue(out _))
            {
            }
            while (_consentClassifierCompletions.TryDequeue(out _))
            {
            }
            while (_trustedOneShots.TryDequeue(out _))
            {
            }
            while (_trustedCancellations.TryDequeue(out _))
            {
            }
            double closeTime = Mission?.CurrentTime ?? 0d;
            foreach (ProgramTargetExecution execution in
                     _programExecutions.Values.ToArray())
            {
                Agent programAgent = execution.Handle?.Agent;
                if (programAgent != null && ReferenceEquals(programAgent.Mission, Mission))
                {
                    TryReleaseProgramOwnedChannels(execution, programAgent);
                }
                CompleteProgramTarget(
                    execution,
                    ExecutionResultCode.Cancelled,
                    origin,
                    closeTime,
                    false);
            }
            _programExecutions.Clear();
            _programBatches.Clear();
            foreach (ScheduledItem<PlannedTarget> item in _scheduler.CancelAll())
            {
                FinishPlan(item.Value, ExecutionResultCode.Cancelled, origin);
            }
            foreach (PendingClassification pending in _pendingClassifications.Values.ToArray())
            {
                FinishAcceptedRequestWithoutTargets(
                    pending.Captured.EventId,
                    ExecutionResultCode.Cancelled,
                    origin);
            }
            _pendingClassifications.Clear();
            foreach (PendingConsentClassification pending in
                     _pendingConsentClassifications.Values.ToArray())
            {
                FinishAcceptedRequestWithoutTargets(
                    pending.Captured.EventId,
                    ExecutionResultCode.Cancelled,
                    origin);
            }
            _pendingConsentClassifications.Clear();
            _pendingConsents.Clear();
            _pendingConsentHandles.Clear();
            foreach (OwnedActionState state in _ownedStates.Values.ToArray())
            {
                Agent stateAgent = state.Handle?.Agent;
                if (stateAgent != null && ReferenceEquals(stateAgent.Mission, Mission))
                {
                    TryReleaseOwnedChannel(
                        stateAgent,
                        state.Channel,
                        true,
                        state.EnterAction,
                        state.HoldAction,
                        state.ExitAction);
                }
                if (state.ActivePlan != null)
                {
                    FinishPlan(state.ActivePlan, ExecutionResultCode.Cancelled, origin);
                }
            }
            _ownedStates.Clear();
            foreach (OwnedLoopState loop in _ownedLoops.Values.ToArray())
            {
                Agent loopAgent = loop.Handle?.Agent;
                if (loopAgent != null && ReferenceEquals(loopAgent.Mission, Mission))
                {
                    TryReleaseOwnedChannel(loopAgent, loop.Channel, true, loop.Action);
                }
            }
            _ownedLoops.Clear();

            foreach (RequestTracker tracker in _trackers.Values.ToArray())
            {
                foreach (KeyValuePair<string, SessionAgentHandle> target in
                         tracker.UnfinishedTargets.ToArray())
                {
                    PlannedTarget cancelled = new PlannedTarget
                    {
                        RequestId = tracker.RequestId,
                        InputSource = tracker.InputSource,
                        Resolver = tracker.Resolver,
                        Intent = new IntentDefinition { Key = tracker.IntentKey },
                        Handle = target.Value,
                        TargetKey = target.Key
                    };
                    FinishPlan(cancelled, ExecutionResultCode.Cancelled, origin);
                }
            }
            _trackers.Clear();
            _requestGate.Clear();
            _providerSession.Clear();
            _cooldowns.Clear();
            _recentPlayerContexts.Clear();
            _cancelledTrustedOwners.Clear();
            _sessionCancellation?.Dispose();
            _sessionCancellation = null;
            SceneActionsLog.Info(
                "SESSION",
                "Mission session closed. SessionGeneration=" + _sessionGeneration +
                " Origin=" + origin);
        }
        internal void StopFromHost()
        {
            CloseSession("Runtime host shutdown.");
        }

        /// <summary>
        /// Non-terminal MCM shutdown for the ordinary natural-language action
        /// subsystem.  A Mission behavior must remain alive so toggling the AF
        /// setting back on can accept new requests.  BattleSpeech TrustedOneShot
        /// work is kept separate by its non-empty OwnerToken and is intentionally
        /// preserved here.
        /// </summary>
        internal void DisableNaturalLanguageFromHost(string reason)
        {
            if (Volatile.Read(ref _closed) != 0)
            {
                return;
            }

            string disableReason = string.IsNullOrWhiteSpace(reason)
                ? "Natural-language reply actions disabled."
                : reason;
            double now = Mission?.CurrentTime ?? 0d;

            // Cancel in-flight classifier work without destroying the Mission
            // session.  A fresh CTS is installed for a later re-enable.
            try
            {
                _sessionCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            try
            {
                _sessionCancellation?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            _sessionCancellation = new CancellationTokenSource();

            while (_inbound.TryDequeue(out CapturedSceneActionEvent captured))
            {
                FinishNoAction(captured.EventId, disableReason);
            }
            while (_classifierCompletions.TryDequeue(out _))
            {
            }
            while (_consentClassifierCompletions.TryDequeue(out _))
            {
            }

            foreach (PendingClassification pending in
                     _pendingClassifications.Values.ToArray())
            {
                FinishAcceptedRequestWithoutTargets(
                    pending.Captured.EventId,
                    ExecutionResultCode.Cancelled,
                    disableReason,
                    logAsInformation: true);
            }
            _pendingClassifications.Clear();
            foreach (PendingConsentClassification pending in
                     _pendingConsentClassifications.Values.ToArray())
            {
                FinishAcceptedRequestWithoutTargets(
                    pending.Captured.EventId,
                    ExecutionResultCode.Cancelled,
                    disableReason,
                    logAsInformation: true);
            }
            _pendingConsentClassifications.Clear();
            _pendingConsents.Clear();
            _pendingConsentHandles.Clear();

            // Programs are ordinary SceneActions programs and never originate
            // from the trusted BattleSpeech queue.  Release their owned channels
            // before touching the scheduler so no stale kneel/loop survives.
            foreach (ProgramTargetExecution execution in
                     _programExecutions.Values.ToArray())
            {
                FailProgramTarget(
                    execution,
                    ExecutionResultCode.Cancelled,
                    disableReason,
                    now);
            }
            _programBatches.Clear();

            // Remove ordinary scheduled plans while retaining queued speech
            // reactions.  The scheduler has no public selective-delete operation;
            // rebuilding only the trusted subset keeps the invariant explicit and
            // bounded by the existing queue capacity.
            List<ScheduledItem<PlannedTarget>> trustedScheduled =
                new List<ScheduledItem<PlannedTarget>>();
            foreach (ScheduledItem<PlannedTarget> item in _scheduler.CancelAll())
            {
                if (item?.Value != null && item.Value.OwnerToken != Guid.Empty)
                {
                    trustedScheduled.Add(item);
                }
                else
                {
                    FinishPlan(
                        item?.Value,
                        ExecutionResultCode.Cancelled,
                        disableReason);
                }
            }
            foreach (ScheduledItem<PlannedTarget> item in trustedScheduled)
            {
                if (_scheduler.TryEnqueue(
                        item.ExecuteAtMissionTime,
                        item.Value,
                        out long sequence))
                {
                    item.Value.StableSequence = sequence;
                }
                else
                {
                    FinishPlan(
                        item.Value,
                        ExecutionResultCode.QueueFull,
                        "Trusted speech plan could not be restored after MCM refresh.");
                }
            }

            // A trusted BattleSpeech program can own a state transition while
            // ordinary SceneActions is being disabled.  It is identified by the
            // non-empty owner token frozen on its active plan; do not release or
            // remove that state from this ordinary-subsystem cleanup pass.
            foreach (KeyValuePair<int, OwnedActionState> stateEntry in
                     _ownedStates.ToArray())
            {
                OwnedActionState state = stateEntry.Value;
                if (state == null)
                {
                    _ownedStates.Remove(stateEntry.Key);
                    continue;
                }
                if (state.ActivePlan != null &&
                    state.ActivePlan.OwnerToken != Guid.Empty)
                {
                    continue;
                }
                Agent agent = state.Handle?.Agent;
                if (agent != null && ReferenceEquals(agent.Mission, Mission))
                {
                    TryReleaseOwnedChannel(
                        agent,
                        state.Channel,
                        true,
                        state.EnterAction,
                        state.HoldAction,
                        state.ExitAction);
                }
                if (state.ActivePlan != null)
                {
                    FinishPlan(state.ActivePlan, ExecutionResultCode.Cancelled, disableReason);
                }
                _ownedStates.Remove(stateEntry.Key);
            }

            foreach (OwnedLoopState loop in _ownedLoops.Values
                         .Where(value => value != null && value.OwnerToken == Guid.Empty)
                         .ToArray())
            {
                Agent agent = loop.Handle?.Agent;
                if (agent != null && ReferenceEquals(agent.Mission, Mission))
                {
                    ReleaseOwnedLoopForAgent(agent, true);
                }
                else if (agent != null)
                {
                    _ownedLoops.Remove(agent.Index);
                }
            }

            // Finish any ordinary tracker targets that were not represented by a
            // queued item (for example a target waiting behind a program barrier).
            foreach (RequestTracker tracker in _trackers.Values
                         .Where(value => value.InputSource != SceneInputSource.BattleSpeechPerformance)
                         .ToArray())
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
                    }, ExecutionResultCode.Cancelled, disableReason);
                }
            }

            // Cooldowns are shared with the trusted BattleSpeech executor but
            // do not carry an owner token.  Keep them intact so disabling the
            // ordinary subsystem cannot change speech pacing; ordinary input is
            // already gated off by Settings.Enabled.
            _recentPlayerContexts.Clear();
            SceneActionsLog.Info(
                "MCM",
                "Ordinary SceneActions queues, classifiers, programs and owned channels " +
                "were cleared without closing the Mission session. Reason=" + disableReason);
        }
    }
}
