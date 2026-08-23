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
        private sealed class PlannedTarget
        {
            public Guid RequestId { get; set; }
            public SceneInputSource InputSource { get; set; }
            public ResolverSource Resolver { get; set; }
            public IntentDefinition Intent { get; set; }
            public SelectedAction SelectedAction { get; set; }
            public SessionAgentHandle Handle { get; set; }
            public string TargetKey { get; set; }
            public int TargetOrdinal { get; set; }
            public string FrozenActionId { get; set; }
            public double QueuedAtMissionTime { get; set; }
            public double ExpiresAtMissionTime { get; set; }
            public long StableSequence { get; set; }
            public ProgramTargetExecution ProgramExecution { get; set; }
            public int ProgramStepIndex { get; set; }
            public Guid OwnerToken { get; set; }
        }

        private sealed class OwnedActionState
        {
            public SessionAgentHandle Handle { get; set; }
            public SelectedAction Definition { get; set; }
            public OwnedStatePhase Phase { get; set; }
            public long StateGeneration { get; set; }
            public ActionIndexCache EnterAction { get; set; }
            public ActionIndexCache HoldAction { get; set; }
            public ActionIndexCache ExitAction { get; set; }
            public int Channel { get; set; }
            public AnimFlags AdditionalFlags { get; set; }
            public double TransitionStartedAt { get; set; }
            public bool EnterObserved { get; set; }
            public bool HoldRequested { get; set; }
            public bool ExitObserved { get; set; }
            public PlannedTarget ActivePlan { get; set; }
        }

        private sealed class OwnedLoopState
        {
            public SessionAgentHandle Handle { get; set; }
            public string LogicalIntentKey { get; set; }
            public SelectedAction SelectedAction { get; set; }
            public ActionIndexCache Action { get; set; }
            public int Channel { get; set; }
            public double StartedAtMissionTime { get; set; }
            public Guid OwnerToken { get; set; }
        }

        private enum ProgramExecutionState
        {
            Waiting,
            Scheduled,
            Running,
            BarrierReady,
            Terminal
        }

        private enum ProgramPlaybackKind
        {
            OneShot,
            Dance,
            Exit
        }

        private sealed class FrozenProgramAction
        {
            public IntentDefinition Intent { get; set; }
            public SelectedAction SelectedAction { get; set; }
            public string FrozenActionId { get; set; }
        }

        private sealed class FrozenProgramStep
        {
            public List<FrozenProgramAction> Actions { get; } =
                new List<FrozenProgramAction>();
        }

        private sealed class ProgramKneelRuntime
        {
            public SelectedAction SelectedAction { get; set; }
            public ActionIndexCache EnterAction { get; set; }
            public ActionIndexCache HoldAction { get; set; }
            public ActionIndexCache ExitAction { get; set; }
            public ActionIndexCache OwnedAction { get; set; }
            public int Channel { get; set; }
            public AnimFlags AdditionalFlags { get; set; }
            public double StartedAtMissionTime { get; set; }
            public double HoldingSinceMissionTime { get; set; }
            public bool AcceptedByEngine { get; set; }
            public bool EnterObserved { get; set; }
            public bool HoldRequested { get; set; }
            public bool Holding { get; set; }
        }

        private sealed class ProgramPlaybackRuntime
        {
            public string LogicalIntentKey { get; set; }
            public SelectedAction SelectedAction { get; set; }
            public ActionIndexCache Action { get; set; }
            public int Channel { get; set; }
            public AnimFlags AdditionalFlags { get; set; }
            public ProgramPlaybackKind Kind { get; set; }
            public double StartedAtMissionTime { get; set; }
            public double ObservedAtMissionTime { get; set; }
            public bool AcceptedByEngine { get; set; }
            public bool Observed { get; set; }
            public bool Completed { get; set; }
        }

        private sealed class ProgramActiveStep
        {
            public double StartedAtMissionTime { get; set; }
            public bool IsDualChannel { get; set; }
            public ProgramPlaybackRuntime Playback { get; set; }
        }

        private sealed class ProgramTargetExecution
        {
            public Guid RequestId { get; set; }
            public SceneInputSource InputSource { get; set; }
            public ResolverSource Resolver { get; set; }
            public SessionAgentHandle Handle { get; set; }
            public string TargetKey { get; set; }
            public int TargetOrdinal { get; set; }
            public ActionProgramV4 Program { get; set; }
            public List<FrozenProgramStep> Steps { get; set; }
            public List<FrozenProgramStep> SequentialFallbackSteps { get; set; }
            public int CurrentStepIndex { get; set; }
            public ProgramExecutionState State { get; set; }
            public ProgramActiveStep ActiveStep { get; set; }
            public ProgramKneelRuntime PersistentKneel { get; set; }
            public PlannedTarget ActivePlan { get; set; }
            public bool UsingSequentialFallback { get; set; }
            public ExecutionResultCode LastSuccessResult { get; set; }
        }

        private sealed class ProgramBatchExecution
        {
            public Guid RequestId { get; set; }
            public bool UseStepBarriers { get; set; }
            public Dictionary<string, ProgramTargetExecution> Targets { get; } =
                new Dictionary<string, ProgramTargetExecution>(StringComparer.Ordinal);
        }

        private sealed class CooldownRecord
        {
            public Agent Agent { get; set; }
            public double UntilMissionTime { get; set; }
        }

        private sealed class RecentPlayerContext
        {
            public Agent Agent { get; set; }
            public string Text { get; set; }
            public double ExpiresAtMissionTime { get; set; }
        }
        private sealed class PendingClassification
        {
            public CapturedSceneActionEvent Captured { get; set; }
            public List<string> AllowedIntentKeys { get; set; }
            public TargetMode? TargetOverride { get; set; }
            public bool FallbackToConsent { get; set; }
            public bool BypassNpcConsent { get; set; }
            public long SessionGeneration { get; set; }
            public double ExpiresAtMissionTime { get; set; }
        }

        private sealed class PendingConsentClassification
        {
            public CapturedSceneActionEvent Captured { get; set; }
            public FrozenConsentRequest FrozenRequest { get; set; }
            public long SessionGeneration { get; set; }
            public double ExpiresAtMissionTime { get; set; }
        }

        private sealed class ClassifierCompletion
        {
            public Guid RequestId { get; set; }
            public long SessionGeneration { get; set; }
            public string Output { get; set; }
            public ExecutionResultCode? Failure { get; set; }
            public string Error { get; set; }
        }

        private sealed class ConsentClassifierCompletion
        {
            public Guid RequestId { get; set; }
            public long SessionGeneration { get; set; }
            public string Output { get; set; }
            public ExecutionResultCode? Failure { get; set; }
            public string Error { get; set; }
        }

        private sealed class RequestTracker
        {
            private readonly HashSet<string> _terminalTargets =
                new HashSet<string>(StringComparer.Ordinal);
            private readonly Dictionary<string, SessionAgentHandle> _targets;

            public RequestTracker(
                Guid requestId,
                string intentKey,
                ResolverSource resolver,
                SceneInputSource inputSource,
                IReadOnlyList<SessionAgentHandle> targets)
            {
                RequestId = requestId;
                IntentKey = intentKey;
                Resolver = resolver;
                InputSource = inputSource;
                _targets = new Dictionary<string, SessionAgentHandle>(StringComparer.Ordinal);
                for (int index = 0; index < targets.Count; index++)
                {
                    _targets.Add(MakeTargetKey(targets[index], index), targets[index]);
                }
            }

            public Guid RequestId { get; }
            public string IntentKey { get; }
            public ResolverSource Resolver { get; }
            public SceneInputSource InputSource { get; }
            public int Requested => _targets.Count;
            public int Accepted { get; private set; }
            public int Skipped { get; private set; }
            public int Failed { get; private set; }
            public int Cancelled { get; private set; }
            public bool IsComplete => _terminalTargets.Count == _targets.Count;
            public IEnumerable<KeyValuePair<string, SessionAgentHandle>> UnfinishedTargets =>
                _targets.Where(pair => !_terminalTargets.Contains(pair.Key));

            public bool TryRecordTerminal(string targetKey, ExecutionResultCode result)
            {
                if (!_targets.ContainsKey(targetKey) || !_terminalTargets.Add(targetKey))
                {
                    return false;
                }
                if (result == ExecutionResultCode.AcceptedByEngine ||
                    result == ExecutionResultCode.HoldingObserved ||
                    result == ExecutionResultCode.CompletedObserved)
                {
                    Accepted++;
                }
                else if (result == ExecutionResultCode.AlreadyStanding ||
                         result == ExecutionResultCode.NoTarget ||
                         result == ExecutionResultCode.ReleaseStageBlocked)
                {
                    Skipped++;
                }
                else if (result == ExecutionResultCode.Cancelled ||
                         result == ExecutionResultCode.MissionChanged)
                {
                    Cancelled++;
                }
                else
                {
                    Failed++;
                }
                return true;
            }
        }
    }
}
