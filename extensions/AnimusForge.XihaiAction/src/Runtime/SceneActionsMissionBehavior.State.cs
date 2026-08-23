using System;
using System.Collections.Generic;
using AnimusForge.SceneActions.Core;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Owns mutable per-mission ledgers used by SceneActionsMissionBehavior.
    /// Keeping the storage in a separate partial makes the next routing/execution
    /// splits possible without changing the established mission call flow.
    /// </summary>
    internal sealed partial class SceneActionsMissionBehavior
    {
        private sealed class SceneActionStateStore
        {
            public Dictionary<Guid, PendingClassification> PendingClassifications { get; } =
                new Dictionary<Guid, PendingClassification>();

            public Dictionary<Guid, PendingConsentClassification> PendingConsentClassifications { get; } =
                new Dictionary<Guid, PendingConsentClassification>();

            public PendingConsentLedger PendingConsents { get; } = new PendingConsentLedger();

            public Dictionary<string, SessionAgentHandle> PendingConsentHandles { get; } =
                new Dictionary<string, SessionAgentHandle>(StringComparer.Ordinal);

            public Dictionary<Guid, RequestTracker> Trackers { get; } =
                new Dictionary<Guid, RequestTracker>();

            // These two ledgers are the channel-ownership state. They are intentionally
            // separate from queued requests so cleanup can never confuse ownership with work.
            public Dictionary<int, OwnedActionState> OwnedStates { get; } =
                new Dictionary<int, OwnedActionState>();

            public Dictionary<int, OwnedLoopState> OwnedLoops { get; } =
                new Dictionary<int, OwnedLoopState>();

            public Dictionary<string, ProgramTargetExecution> ProgramExecutions { get; } =
                new Dictionary<string, ProgramTargetExecution>(StringComparer.Ordinal);

            public Dictionary<Guid, ProgramBatchExecution> ProgramBatches { get; } =
                new Dictionary<Guid, ProgramBatchExecution>();

            public Dictionary<string, CooldownRecord> Cooldowns { get; } =
                new Dictionary<string, CooldownRecord>(StringComparer.Ordinal);

            public Dictionary<int, RecentPlayerContext> RecentPlayerContexts { get; } =
                new Dictionary<int, RecentPlayerContext>();

            public HashSet<Guid> CancelledTrustedOwners { get; } =
                new HashSet<Guid>();
        }
    }
}
