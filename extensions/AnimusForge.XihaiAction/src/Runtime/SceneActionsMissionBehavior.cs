using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior : MissionBehavior
    {
        private const double RecentPlayerContextTtlSeconds = 60d;
        private const int MaxRecentPlayerContextChars = 2048;
        private static long _generationAllocator;

        private readonly ConcurrentQueue<CapturedSceneActionEvent> _inbound =
            new ConcurrentQueue<CapturedSceneActionEvent>();
        private readonly ConcurrentQueue<ClassifierCompletion> _classifierCompletions =
            new ConcurrentQueue<ClassifierCompletion>();
        private readonly ConcurrentQueue<ConsentClassifierCompletion>
            _consentClassifierCompletions =
                new ConcurrentQueue<ConsentClassifierCompletion>();
        private readonly ConcurrentQueue<TrustedOneShotRequest> _trustedOneShots =
            new ConcurrentQueue<TrustedOneShotRequest>();
        private readonly ConcurrentQueue<TrustedPlaybackCancellation> _trustedCancellations =
            new ConcurrentQueue<TrustedPlaybackCancellation>();
        // All mutable per-mission ledgers live in the state-store partial. The aliases keep
        // the existing execution code and behavior stable while making ownership explicit.
        private readonly SceneActionStateStore _stateStore;

        private readonly Dictionary<Guid, PendingClassification> _pendingClassifications;
        private readonly Dictionary<Guid, PendingConsentClassification>
            _pendingConsentClassifications;
        private readonly PendingConsentLedger _pendingConsents;
        private readonly Dictionary<string, SessionAgentHandle> _pendingConsentHandles;
        private readonly Dictionary<Guid, RequestTracker> _trackers;
        private readonly Dictionary<int, OwnedActionState> _ownedStates;
        private readonly Dictionary<int, OwnedLoopState> _ownedLoops;
        private readonly Dictionary<string, ProgramTargetExecution> _programExecutions;
        private readonly Dictionary<Guid, ProgramBatchExecution> _programBatches;
        private readonly Dictionary<string, CooldownRecord> _cooldowns;
        private readonly Dictionary<int, RecentPlayerContext> _recentPlayerContexts;
        private readonly HashSet<Guid> _cancelledTrustedOwners;

        private CancellationTokenSource _sessionCancellation;
        private SceneActionScheduleQueue _scheduler;
        private MissionActionProviderSession _providerSession;
        private RequestGate _requestGate;
        private long _sessionGeneration;
        private int _closed = 1;

        public SceneActionsMissionBehavior()
        {
            _stateStore = new SceneActionStateStore();
            _pendingClassifications = _stateStore.PendingClassifications;
            _pendingConsentClassifications = _stateStore.PendingConsentClassifications;
            _pendingConsents = _stateStore.PendingConsents;
            _pendingConsentHandles = _stateStore.PendingConsentHandles;
            _trackers = _stateStore.Trackers;
            _ownedStates = _stateStore.OwnedStates;
            _ownedLoops = _stateStore.OwnedLoops;
            _programExecutions = _stateStore.ProgramExecutions;
            _programBatches = _stateStore.ProgramBatches;
            _cooldowns = _stateStore.Cooldowns;
            _recentPlayerContexts = _stateStore.RecentPlayerContexts;
            _cancelledTrustedOwners = _stateStore.CancelledTrustedOwners;
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        internal bool IsSessionActive => Volatile.Read(ref _closed) == 0;

    }
}
