using System;
using System.Collections.Generic;

namespace AnimusForge.SceneActions.Core
{
    public enum SceneInputSource
    {
        PlayerSceneShout,
        NpcSceneShoutReply,
        BattleSpeechPerformance
    }

    public enum ResolverSource
    {
        ForceExact,
        ForceFramedExact,
        ExactCommand,
        NpcStageDirection,
        AiClassifier,
        ForceNaturalLanguage,
        ForceFramedNaturalLanguage,
        NpcConsentLocal,
        NpcConsentClassifier,
        ImplicitEmotionInference,
        BattleSpeechSemantic
    }

    public enum ParseStatus
    {
        NoAction,
        Matched,
        Invalid
    }

    public enum IntentKind
    {
        PlayAction,
        ExitOwnedState,
        ReleaseOwnedAction,
        DrawWeapon,
        SheatheWeapon
    }

    public enum ActionMode
    {
        OneShot,
        RandomGroup,
        Stateful,
        Looping
    }

    public enum TargetMode
    {
        Player,
        Primary,
        FramedSelection
    }

    public enum SchedulerOverflowPolicy
    {
        Reject,
        TruncateStable
    }

    public enum ReleaseStage
    {
        Candidate,
        Experimental,
        Validated
    }

    public enum OwnedStatePhase
    {
        Entering,
        Holding,
        Exiting
    }

    public enum ExecutionResultCode
    {
        Queued,
        AcceptedByEngine,
        HoldingObserved,
        CompletedObserved,
        AlreadyStanding,
        NoOwnedAction,
        NoUsableWeapon,
        AlreadyWielded,
        AlreadySheathed,
        PreviousActionNotReleased,
        NoTarget,
        DuplicateRequest,
        Expired,
        QueueFull,
        BatchTooLarge,
        ReleaseStageBlocked,
        ProviderUnavailable,
        AgentNotFound,
        AgentInactive,
        AgentNonHuman,
        ActionIndexMissing,
        EngineCriticalState,
        SetActionRejected,
        Interrupted,
        Cancelled,
        MissionChanged,
        InvalidCommand,
        InvalidClassifierOutput,
        ClassifierUnavailable,
        ClassifierTimeout,
        AwaitingConsent,
        ConsentRefused,
        ConsentUnclear,
        ExecutorException
    }

    public sealed class RuntimeIdentity
    {
        public RuntimeIdentity(
            string gameVersion,
            string runtimeBuildId,
            int runtimeAdapterContract)
        {
            GameVersion = gameVersion ?? string.Empty;
            RuntimeBuildId = runtimeBuildId ?? string.Empty;
            RuntimeAdapterContract = runtimeAdapterContract;
        }

        public string GameVersion { get; }
        public string RuntimeBuildId { get; }
        public int RuntimeAdapterContract { get; }
    }

    public sealed class ParseDecision
    {
        private ParseDecision(
            ParseStatus status,
            string intentKey,
            ActionProgramV2 program,
            ActionProgramV3 programV3,
            ActionProgramV4 programV4,
            TargetMode? targetOverride,
            ResolverSource? resolver,
            string error,
            bool stopResolution,
            string classifierText,
            bool aiFallbackRequested,
            bool bypassNpcConsent)
        {
            Status = status;
            IntentKey = intentKey;
            Program = program;
            ProgramV3 = programV3;
            ProgramV4 = programV4;
            TargetOverride = targetOverride;
            Resolver = resolver;
            Error = error;
            StopResolution = stopResolution;
            ClassifierText = classifierText;
            AiFallbackRequested = aiFallbackRequested;
            BypassNpcConsent = bypassNpcConsent;
        }

        public ParseStatus Status { get; }
        public string IntentKey { get; }
        public ActionProgramV2 Program { get; }
        public ActionProgramV3 ProgramV3 { get; }
        public ActionProgramV4 ProgramV4 { get; }
        public TargetMode? TargetOverride { get; }
        public ResolverSource? Resolver { get; }
        public string Error { get; }
        public bool StopResolution { get; }
        public string ClassifierText { get; }
        public bool AiFallbackRequested { get; }
        public bool BypassNpcConsent { get; }

        public static ParseDecision Match(
            string intentKey,
            TargetMode? targetOverride,
            ResolverSource resolver)
        {
            return Match(intentKey, targetOverride, resolver, false);
        }

        public static ParseDecision Match(
            string intentKey,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent)
        {
            return MatchProgram(
                ActionProgramV2.FromSingle(intentKey),
                targetOverride,
                resolver,
                bypassNpcConsent);
        }

        public static ParseDecision MatchProgram(
            ActionProgramV2 program,
            TargetMode? targetOverride,
            ResolverSource resolver)
        {
            return MatchProgram(program, targetOverride, resolver, false);
        }

        public static ParseDecision MatchProgram(
            ActionProgramV2 program,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }
            return new ParseDecision(
                ParseStatus.Matched,
                program.SingleIntentKey,
                program,
                ActionProgramV3.FromV2(program),
                ActionProgramV4.FromV2(program),
                targetOverride,
                resolver,
                null,
                true,
                null,
                false,
                bypassNpcConsent);
        }

        public static ParseDecision MatchV3(
            string intentKey,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent = false)
        {
            return MatchProgramV3(
                ActionProgramV3.FromSingle(intentKey),
                targetOverride,
                resolver,
                bypassNpcConsent);
        }

        public static ParseDecision MatchProgramV3(
            ActionProgramV3 program,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent = false)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }
            program.TryToV2(out ActionProgramV2 legacyProgram);
            return new ParseDecision(
                ParseStatus.Matched,
                program.SingleIntentKey,
                legacyProgram,
                program,
                ActionProgramV4.FromV3(program),
                targetOverride,
                resolver,
                null,
                true,
                null,
                false,
                bypassNpcConsent);
        }

        public static ParseDecision MatchV4(
            string intentKey,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent = false)
        {
            return MatchProgramV4(
                ActionProgramV4.FromSingle(intentKey),
                targetOverride,
                resolver,
                bypassNpcConsent);
        }

        public static ParseDecision MatchProgramV4(
            ActionProgramV4 program,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent = false)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }
            program.TryToV3(out ActionProgramV3 legacyProgramV3);
            ActionProgramV2 legacyProgramV2 = null;
            legacyProgramV3?.TryToV2(out legacyProgramV2);
            return new ParseDecision(
                ParseStatus.Matched,
                program.SingleIntentKey,
                legacyProgramV2,
                legacyProgramV3,
                program,
                targetOverride,
                resolver,
                null,
                true,
                null,
                false,
                bypassNpcConsent);
        }

        public static ParseDecision MatchRuntimeControl(
            string intentKey,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent = false)
        {
            return MatchProgramV4(
                ActionProgramV4.FromRuntimeControl(intentKey),
                targetOverride,
                resolver,
                bypassNpcConsent);
        }

        public static ParseDecision None(bool stopResolution = false)
        {
            return new ParseDecision(
                ParseStatus.NoAction,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                stopResolution,
                null,
                false,
                false);
        }

        public static ParseDecision Fallback(
            string classifierText,
            TargetMode? targetOverride)
        {
            return Fallback(classifierText, targetOverride, false);
        }

        public static ParseDecision Fallback(
            string classifierText,
            TargetMode? targetOverride,
            bool bypassNpcConsent)
        {
            if (string.IsNullOrWhiteSpace(classifierText))
            {
                throw new ArgumentException(
                    "Classifier text must not be empty.",
                    nameof(classifierText));
            }
            return new ParseDecision(
                ParseStatus.NoAction,
                null,
                null,
                null,
                null,
                targetOverride,
                null,
                null,
                false,
                classifierText.Trim(),
                true,
                bypassNpcConsent);
        }

        public static ParseDecision Invalid(string error)
        {
            return new ParseDecision(
                ParseStatus.Invalid,
                null,
                null,
                null,
                null,
                null,
                null,
                error,
                true,
                null,
                false,
                false);
        }
    }

    public sealed class ActionVariant
    {
        public string Id { get; set; }
        public string GameVersionEquals { get; set; }
        public string RuntimeBuildId { get; set; }
        public int RuntimeAdapterContract { get; set; }
        public ReleaseStage ReleaseStage { get; set; }
        public bool EnabledByDefault { get; set; }
        public string ValidationReportId { get; set; }
        public int Channel { get; set; } = 1;
        public bool EnforceAll { get; set; }
        public float BlendInSeconds { get; set; } = 0.2f;
        public float ActionSpeed { get; set; } = 1f;
        public IReadOnlyList<string> ActionIds { get; set; } = Array.Empty<string>();
        public string EnterActionId { get; set; }
        public string HoldActionId { get; set; }
        public string ExitActionId { get; set; }
        public float EnterSafetyTimeoutSeconds { get; set; } = 4f;
        public float ExitSafetyTimeoutSeconds { get; set; } = 4f;
    }

    public sealed class ActionDefinition
    {
        public string Key { get; set; }
        public string ProviderId { get; set; }
        public ActionMode Mode { get; set; }
        public string StateTag { get; set; }
        public float CooldownSeconds { get; set; } = 0.5f;
        public IReadOnlyList<ActionVariant> RuntimeVariants { get; set; } =
            Array.Empty<ActionVariant>();
    }

    public sealed class IntentDefinition
    {
        public string Key { get; set; }
        public IntentKind Kind { get; set; }
        public string ActionKey { get; set; }
        public IReadOnlyList<string> AcceptedStateTags { get; set; } =
            Array.Empty<string>();
        public TargetMode DefaultTargetMode { get; set; }
        public bool ClassifierSelectable { get; set; }
    }

    public sealed class AliasDefinition
    {
        public string Text { get; set; }
        public string IntentKey { get; set; }
        public bool AllowForceExact { get; set; }
        public bool AllowExactCommand { get; set; }
        public TargetMode? TargetOverride { get; set; }
    }

    public sealed class SelectedAction
    {
        public SelectedAction(ActionDefinition definition, ActionVariant variant)
        {
            Definition = definition;
            Variant = variant;
        }

        public ActionDefinition Definition { get; }
        public ActionVariant Variant { get; }
    }

    public sealed class ActionOverride
    {
        public bool? Enabled { get; set; }
        public float? BlendInSeconds { get; set; }
        public float? CooldownSeconds { get; set; }
    }

    public sealed class SceneActionSettings
    {
        public bool Enabled { get; set; } = true;
        public bool PlayerSceneShoutEnabled { get; set; } = true;
        public bool NpcSceneShoutReplyEnabled { get; set; }
        public bool ForceExactEnabled { get; set; } = true;
        public bool ExactCommandEnabled { get; set; } = true;
        public bool AiClassifierEnabled { get; set; }
        public string AiClassifierProviderId { get; set; }
        public int ClassifierTimeoutMs { get; set; } = 2500;
        public int ClassifierMaxOutputChars { get; set; } = 64;
        public int RequestTtlMs { get; set; } = 8000;
        public int ConsentReplyTtlMs { get; set; } = 30000;
        public int MaxPendingRequests { get; set; } = 64;
        public int StaggerFromTargetCount { get; set; } = 4;
        public float StaggerSeconds { get; set; } = 0.1f;
        public int MaxBatchTargets { get; set; } = 16;
        public float MaxBatchTailSeconds { get; set; } = 2f;
        public int MaxQueuedTargets { get; set; } = 128;
        public SchedulerOverflowPolicy OverflowPolicy { get; set; } =
            SchedulerOverflowPolicy.Reject;
        public bool ScreenBatchSummary { get; set; }
        public float FailureRateLimitSeconds { get; set; } = 5f;
        public string LogLevel { get; set; } = "information";
        public bool DeveloperDiagnosticsEnabled { get; set; }
        public bool AllowRegisteredActionIdProbe { get; set; }
        public int MaxProgramActions { get; set; } = ActionProgramV4.MaximumActionCount;
        public float StepTimeoutSeconds { get; set; } = 6f;
        public float IntermediateKneelHoldSeconds { get; set; } = 1f;
        public float IntermediateDanceSeconds { get; set; } = 4f;
        public bool DualChannelExperimentalEnabled { get; set; } = true;
        public int ForceMultiTargetThreshold { get; set; } = 3;
        public float ForceStaggerMinSeconds { get; set; } = 0.01f;
        public float ForceStaggerMaxSeconds { get; set; } = 0.02f;
        public IReadOnlyDictionary<string, ActionOverride> ActionOverrides { get; set; } =
            new Dictionary<string, ActionOverride>(StringComparer.Ordinal);
        public IReadOnlyList<AliasDefinition> UserAliases { get; set; } =
            Array.Empty<AliasDefinition>();

        public IReadOnlyList<string> Validate()
        {
            List<string> errors = new List<string>();
            if (ClassifierTimeoutMs <= 0 || ClassifierTimeoutMs >= RequestTtlMs)
            {
                errors.Add("ClassifierTimeoutMs must be positive and below RequestTtlMs.");
            }
            if (AiClassifierEnabled && string.IsNullOrWhiteSpace(AiClassifierProviderId))
            {
                errors.Add("AiClassifierProviderId is required when AI is enabled.");
            }
            if (!AiClassifierEnabled && !string.IsNullOrEmpty(AiClassifierProviderId))
            {
                errors.Add("AiClassifierProviderId is forbidden when AI is disabled.");
            }
            if (RequestTtlMs < 1000 || MaxPendingRequests < 1)
            {
                errors.Add("Request gate limits are invalid.");
            }
            if (ConsentReplyTtlMs < 1000 ||
                ConsentReplyTtlMs > 300000 ||
                ConsentReplyTtlMs <= ClassifierTimeoutMs)
            {
                errors.Add(
                    "ConsentReplyTtlMs must be within 1000..300000 and exceed ClassifierTimeoutMs.");
            }
            if (StaggerFromTargetCount < 1 || StaggerSeconds < 0f)
            {
                errors.Add("Scheduler stagger settings are invalid.");
            }
            if (MaxBatchTargets < 1 || MaxBatchTargets > MaxQueuedTargets)
            {
                errors.Add("MaxBatchTargets must be within MaxQueuedTargets.");
            }
            float worstTail = Math.Max(0, MaxBatchTargets - 1) * StaggerSeconds;
            if (worstTail > MaxBatchTailSeconds + 0.0001f)
            {
                errors.Add("Configured batch can exceed MaxBatchTailSeconds.");
            }
            if (ClassifierMaxOutputChars < 4 || ClassifierMaxOutputChars > 256)
            {
                errors.Add("ClassifierMaxOutputChars is outside the safe range.");
            }
            if (FailureRateLimitSeconds < 0f || FailureRateLimitSeconds > 300f)
            {
                errors.Add("FailureRateLimitSeconds is outside the safe range.");
            }
            if (MaxProgramActions != ActionProgramV4.MaximumActionCount)
            {
                errors.Add("MaxProgramActions must remain frozen at four.");
            }
            if (float.IsNaN(StepTimeoutSeconds) ||
                StepTimeoutSeconds < 0.5f || StepTimeoutSeconds > 30f)
            {
                errors.Add("StepTimeoutSeconds is outside the safe range.");
            }
            if (float.IsNaN(IntermediateKneelHoldSeconds) ||
                IntermediateKneelHoldSeconds < 0f || IntermediateKneelHoldSeconds > 10f ||
                float.IsNaN(IntermediateDanceSeconds) ||
                IntermediateDanceSeconds < 0.5f || IntermediateDanceSeconds > 30f)
            {
                errors.Add("Intermediate state durations are outside the safe range.");
            }
            if (ForceMultiTargetThreshold < 3 || ForceMultiTargetThreshold > 64 ||
                float.IsNaN(ForceStaggerMinSeconds) ||
                float.IsNaN(ForceStaggerMaxSeconds) ||
                ForceStaggerMinSeconds < 0f ||
                ForceStaggerMaxSeconds < ForceStaggerMinSeconds ||
                ForceStaggerMaxSeconds > 0.25f)
            {
                errors.Add("Forced multi-target stagger settings are invalid.");
            }
            if (AllowRegisteredActionIdProbe)
            {
                errors.Add("Production builds do not permit raw action-id probing.");
            }
            return errors;
        }
    }
}
