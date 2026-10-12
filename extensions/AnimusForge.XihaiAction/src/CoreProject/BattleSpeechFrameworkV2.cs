using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.SceneActions.Core
{
    public enum BattleSpeechTriggerKindV2
    {
        None,
        ArmPlayerSpeech,
        DeliverPlayerSpeech,
        RequestNpcSpeech,
        Cancel,
        NeedsClassifier,
        OrdinaryScene
    }

    public enum BattleSpeechTacticV2
    {
        None,
        Advance
    }

    public sealed class BattleSpeechTriggerDecisionV2
    {
        internal BattleSpeechTriggerDecisionV2(
            BattleSpeechTriggerKindV2 kind,
            string speechText = null,
            string reason = null,
            bool force = false)
        {
            Kind = kind;
            SpeechText = speechText;
            Reason = reason;
            Force = force;
        }

        public BattleSpeechTriggerKindV2 Kind { get; }
        public string SpeechText { get; }
        public string Reason { get; }
        public bool Force { get; }
        public bool IsControl => Kind != BattleSpeechTriggerKindV2.None &&
                                 Kind != BattleSpeechTriggerKindV2.OrdinaryScene;
    }

    public sealed class BattleSpeechPlanDecisionV2
    {
        public BattleSpeechPlanDecisionV2(
            ActionProgramV4 actionProgram,
            BattleSpeechTacticV2 tactic,
            IReadOnlyList<string> audienceReplies = null)
        {
            ActionProgram = actionProgram;
            Tactic = tactic;
            AudienceReplies = new ReadOnlyCollection<string>(
                (audienceReplies ?? Array.Empty<string>()).ToArray());
        }

        public ActionProgramV4 ActionProgram { get; }
        public BattleSpeechTacticV2 Tactic { get; }
        public IReadOnlyList<string> AudienceReplies { get; }
    }

    /// <summary>
    /// The closed response returned by the dedicated NPC speech request.  It
    /// deliberately reuses the plan decision for the action/tactic/reply
    /// fields, so the runtime can publish one frozen plan without starting a
    /// second semantic-classifier request.
    /// </summary>
    public sealed class BattleSpeechCombinedNpcResponseV2
    {
        public BattleSpeechCombinedNpcResponseV2(
            string speechText,
            BattleSpeechPlanDecisionV2 plan)
        {
            SpeechText = speechText;
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        public string SpeechText { get; }
        public BattleSpeechPlanDecisionV2 Plan { get; }
    }

    public sealed class BattleSpeechTriggerClassifierRequestV2
    {
        public Guid RequestId { get; set; }
        public string PlayerText { get; set; }
        public bool HasPrimaryNpcTarget { get; set; }
    }

    public sealed class BattleSpeechPlanClassifierRequestV2
    {
        public bool? UseChineseText { get; set; }
        public Guid RequestId { get; set; }
        public string SpeechText { get; set; }
        public IReadOnlyList<string> AllowedIntentKeys { get; set; } =
            Array.Empty<string>();
        public bool AllowAdvance { get; set; }
        public int AudienceReplyCount { get; set; }
        public int AudienceReplyMinimumChars { get; set; } = 8;
        public int AudienceReplyMaximumChars { get; set; } = 24;
    }

    public interface IBattleSpeechClassifierV2
    {
        Task<string> ClassifyBattleSpeechTriggerAsync(
            BattleSpeechTriggerClassifierRequestV2 request,
            CancellationToken cancellationToken);

        Task<string> ClassifyBattleSpeechPlanAsync(
            BattleSpeechPlanClassifierRequestV2 request,
            CancellationToken cancellationToken);
    }

    public sealed class BattleSpeechStageSettingsV2
    {
        public bool NaturalTriggerEnabled { get; set; } = true;
        public bool TriggerClassifierEnabled { get; set; } = true;
        public bool SemanticClassifierEnabled { get; set; } = true;
        public string ClassifierProviderId { get; set; } = "animusforge.main.v130";
        public int ClassifierTimeoutMs { get; set; } = 15000;
        public int ReplyMinimumChars { get; set; } = 60;
        public int ReplyMaximumChars { get; set; } = 160;
        public bool NpcPositioningEnabled { get; set; } = true;
        public float FrontDistanceMeters { get; set; } = 10f;
        public float ArrivalRadiusMeters { get; set; } = 1.5f;
        public float MovementTimeoutSeconds { get; set; } = 15f;
        // Retained only so older persisted MCM/json2 documents still deserialize.
        // Lateral pacing was removed; runtime code never reads these values.
        public bool PacingEnabled { get; set; } = false;
        public bool MountedPacingEnabled { get; set; } = false;
        public bool InfantryPacingEnabled { get; set; } = false;
        public float PacingHalfWidthMeters { get; set; } = 2f;
        public float PacingMinimumIntervalSeconds { get; set; } = 2.5f;
        public float PacingMaximumIntervalSeconds { get; set; } = 4.5f;
        public bool IncludeAlliedAudience { get; set; } = true;
        public int MaximumVisualResponders { get; set; } = 60;
        public int VisualWaveSize { get; set; } = 6;
        public int MaximumVisualSubmissionsPerTick { get; set; } = 6;
        public bool AudienceVoicesEnabled { get; set; } = true;
        public int AudienceVoiceCount { get; set; } = 22;
        public int AudienceVoiceWaveSize { get; set; } = 3;
        public float AudienceVoiceWaveIntervalSeconds { get; set; } = 0.18f;
        public bool AudienceRepliesEnabled { get; set; } = true;
        // Spoken replies are emitted in small same-tick waves by the performance
        // behavior; this raises participation without creating an AF request burst.
        public int AudienceReplyCount { get; set; } = 24;
        // Each wave is deterministically randomized from 2 through this cap.
        public int AudienceReplyWaveSize { get; set; } = 5;
        // Spoken-reply bridge calls are bounded independently from visual
        // actions so a large wave cannot monopolize one Mission tick.
        public int MaximumAudienceReplySubmissionsPerTick { get; set; } = 8;
        public int AudienceReplyMinimumChars { get; set; } = 8;
        public int AudienceReplyMaximumChars { get; set; } = 24;
        public float AudienceReplyMinimumIntervalSeconds { get; set; } = 0.2f;
        public float AudienceReplyMaximumIntervalSeconds { get; set; } = 0.5f;
        public float AudienceResponseStartDelaySeconds { get; set; } = 3f;
        public float AudienceFinalReactionHoldSeconds { get; set; } = 2.5f;
        // Retained for old settings readers and old diagnostics; runtime uses
        // the bounded random interval above.
        public float AudienceReplyIntervalSeconds { get; set; } = 1.1f;
        public bool TacticalAdvanceEnabled { get; set; } = true;
        public float TacticalAdvanceDelaySeconds { get; set; } = 1.8f;

        public IReadOnlyList<string> Validate()
        {
            List<string> errors = new List<string>();
            if (string.IsNullOrWhiteSpace(ClassifierProviderId) ||
                ClassifierProviderId.Length > 128 ||
                ClassifierTimeoutMs < 1000 || ClassifierTimeoutMs > 60000)
            {
                errors.Add("Battle speech classifier settings are invalid.");
            }
            if (ReplyMinimumChars < 6 || ReplyMaximumChars > 160 ||
                ReplyMaximumChars < ReplyMinimumChars)
            {
                errors.Add("Battle speech reply length must stay within 6..160 characters.");
            }
            if (FrontDistanceMeters < 2f || FrontDistanceMeters > 25f ||
                ArrivalRadiusMeters < 0.5f || ArrivalRadiusMeters > 4f ||
                MovementTimeoutSeconds < 3f || MovementTimeoutSeconds > 45f)
            {
                errors.Add("Battle speech positioning settings are invalid.");
            }
            if (MaximumVisualResponders < 1 || MaximumVisualResponders > 128 ||
                VisualWaveSize < 1 || VisualWaveSize > 16 ||
                MaximumVisualSubmissionsPerTick < 1 ||
                MaximumVisualSubmissionsPerTick > 16)
            {
                errors.Add("Battle speech visual response settings are invalid.");
            }
            if (AudienceVoiceCount < 0 || AudienceVoiceCount > 24 ||
                AudienceVoiceWaveSize < 1 || AudienceVoiceWaveSize > 8 ||
                AudienceVoiceWaveIntervalSeconds < 0.05f ||
                AudienceVoiceWaveIntervalSeconds > 1f ||
                AudienceReplyCount < 0 ||
                AudienceReplyCount > BattleSpeechFrameworkV2.MaximumAudienceReplies ||
                AudienceReplyWaveSize < 2 || AudienceReplyWaveSize > 20 ||
                MaximumAudienceReplySubmissionsPerTick < 2 ||
                MaximumAudienceReplySubmissionsPerTick > 20 ||
                AudienceReplyMinimumChars < 4 ||
                AudienceReplyMaximumChars > 80 ||
                AudienceReplyMaximumChars < AudienceReplyMinimumChars ||
                AudienceReplyMinimumIntervalSeconds < 0.1f ||
                AudienceReplyMaximumIntervalSeconds > 0.5f ||
                AudienceReplyMaximumIntervalSeconds < AudienceReplyMinimumIntervalSeconds ||
                AudienceResponseStartDelaySeconds < 0.5f ||
                AudienceResponseStartDelaySeconds > 12f ||
                AudienceFinalReactionHoldSeconds < 0.5f ||
                AudienceFinalReactionHoldSeconds > 8f ||
                TacticalAdvanceDelaySeconds < 0.5f || TacticalAdvanceDelaySeconds > 5f)
            {
                errors.Add("Battle speech voice or tactic settings are invalid.");
            }
            return new ReadOnlyCollection<string>(errors);
        }
    }

    public static class BattleSpeechFrameworkV2
    {
        // One AF plan request carries the whole set; the mission behavior
        // emits replies in bounded waves to avoid a request/UI burst.
        public const int MaximumAudienceReplies = 100;

        private static readonly string[] ForcedPlayerSpeechPrefixes =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0001"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0002"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0003"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0004"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0005"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0006"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0007"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0008"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0009"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0010"),
            "force speech", "forced speech", "force battle speech", "forced battle speech", "my speech", "speech", "battle speech"
        };

        private static readonly string[] SpeechMetaMarkers =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0011"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0012"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0013"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0014"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0015"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0016"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0017"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0018"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0019"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0020"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0021"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0022"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0023"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0024"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0025"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0026"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0027"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0028"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0029"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0030"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0031"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0032"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0033"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0034"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0035"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0036")
        };


        public const int ContractVersion = 2;
        public const bool MountedNpcSpeechSupported = true;
        public const float MinimumClosingCommandVisibilitySeconds = 1.8f;

        public static float ResolveClosingCommandDelaySeconds(float configuredSeconds)
        {
            return Math.Max(MinimumClosingCommandVisibilitySeconds, configuredSeconds);
        }

        public static bool ShouldQueueOrdinaryScenePostprocess(
            bool battleSpeechClaimedReply,
            bool ordinaryWorkSelected)
        {
            return !battleSpeechClaimedReply && ordinaryWorkSelected;
        }

        private static string BuildNpcSpeechWritingRules(
            int minimumChars,
            int maximumChars,
            bool combatOnly,
            bool useChinese)
        {
            string toneRule = combatOnly
                ? BattleSpeechText.Text("Prompt_Writing_01", useChinese)
                : BattleSpeechText.Text("Prompt_Writing_02", useChinese);
            return BattleSpeechText.Text("Prompt_Writing_03", useChinese) +
                   BattleSpeechText.Text("Prompt_Writing_04", useChinese) +
                   BattleSpeechText.Text("Prompt_Writing_05", useChinese) +
                   BattleSpeechText.Text("Prompt_Writing_06", useChinese) +
                   BattleSpeechText.Text("Prompt_Writing_07", useChinese) +
                   BattleSpeechText.Text("Prompt_Writing_08", useChinese) +
                   "- " + toneRule + BattleSpeechText.Text("Prompt_Writing_09", useChinese) +
                   BattleSpeechText.Text("Prompt_Writing_10", useChinese) + minimumChars + BattleSpeechText.Text("Prompt_Writing_11", useChinese) +
                   maximumChars + BattleSpeechText.Text("Prompt_Writing_12", useChinese) +
                   BattleSpeechText.Text("Prompt_Writing_13", useChinese);
        }

        public static string BuildNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars)
        {
            return BuildNpcSpeechPromptInstruction(
                minimumChars,
                maximumChars,
                null,
                0,
                null);
        }

        public static string BuildNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt)
        {
            return BuildNpcSpeechPromptInstruction(
                minimumChars,
                maximumChars,
                recentSpeechTexts,
                generationAttempt,
                null);
        }

        public static string BuildNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts)
        {
            return BuildNpcSpeechPromptInstruction(minimumChars, maximumChars, recentSpeechTexts, generationAttempt, battlefieldFacts, true);
        }

        public static string BuildNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts,
            bool useChinese)
        {
            if (minimumChars < 6 || maximumChars > 160 || maximumChars < minimumChars)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumChars),
                    "Battle speech prompt length must stay within 6..160 characters.");
            }
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(useChinese, recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? BattleSpeechText.Text("Prompt_Single_01", useChinese) + (generationAttempt + 1) + BattleSpeechText.Text("Prompt_Single_02", useChinese)
                : string.Empty;
            string battlefieldBlock = battlefieldFacts?.ToPromptBlock(useChinese) ?? string.Empty;
            return BattleSpeechText.Text("Prompt_Single_03", useChinese) +
                   BattleSpeechText.Text("Prompt_Single_04", useChinese) +
                   BuildNpcSpeechWritingRules(minimumChars, maximumChars, combatOnly: false, useChinese: useChinese) +
                   AppendPromptBlock(battlefieldBlock) +
                   AppendPromptBlock(diversity) +
                   AppendPromptBlock(attempt) +
                   BattleSpeechText.Text("Prompt_Single_05", useChinese);
        }

        public static string BuildCombinedNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int audienceReplyCount)
        {
            return BuildCombinedNpcSpeechPromptInstruction(
                minimumChars,
                maximumChars,
                allowedIntentKeys,
                audienceReplyCount,
                8,
                24,
                null,
                0);
        }

        public static string BuildCombinedNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int audienceReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars)
        {
            return BuildCombinedNpcSpeechPromptInstruction(
                minimumChars,
                maximumChars,
                allowedIntentKeys,
                audienceReplyCount,
                audienceReplyMinimumChars,
                audienceReplyMaximumChars,
                null,
                0,
                null);
        }

        public static string BuildCombinedNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int audienceReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt)
        {
            return BuildCombinedNpcSpeechPromptInstruction(
                minimumChars,
                maximumChars,
                allowedIntentKeys,
                audienceReplyCount,
                audienceReplyMinimumChars,
                audienceReplyMaximumChars,
                recentSpeechTexts,
                generationAttempt,
                null);
        }

        public static string BuildCombinedNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int audienceReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts)
        {
            return BuildCombinedNpcSpeechPromptInstruction(minimumChars, maximumChars, allowedIntentKeys, audienceReplyCount, audienceReplyMinimumChars, audienceReplyMaximumChars, recentSpeechTexts, generationAttempt, battlefieldFacts, true);
        }

        public static string BuildCombinedNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int audienceReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts,
            bool useChinese)
        {
            if (minimumChars < 6 || maximumChars > 160 || maximumChars < minimumChars)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumChars));
            }
            ValidateAudienceReplyLength(
                audienceReplyMinimumChars,
                audienceReplyMaximumChars);
            int boundedReplyCount = Math.Max(
                0,
                Math.Min(MaximumAudienceReplies, audienceReplyCount));
            string keys = string.Join(",", (allowedIntentKeys ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal));
            string replyProtocolLine = BuildReplyProtocolLine(boundedReplyCount, useChinese);
            string replyRule = boundedReplyCount == 0
                ? BattleSpeechText.Text("Prompt_Combined_01", useChinese)
                : BattleSpeechText.Text("Prompt_Combined_02", useChinese) + boundedReplyCount +
                  BattleSpeechText.Text("Prompt_Combined_03", useChinese) +
                  audienceReplyMinimumChars + BattleSpeechText.Text("Prompt_Combined_04", useChinese) + audienceReplyMaximumChars +
                  BattleSpeechText.Text("Prompt_Combined_05", useChinese);
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(useChinese, recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? BattleSpeechText.Text("Prompt_Combined_06", useChinese) + (generationAttempt + 1) + BattleSpeechText.Text("Prompt_Combined_07", useChinese)
                : string.Empty;
            string protocolLines =
                BattleSpeechText.Text("Prompt_Combined_08", useChinese) +
                BattleSpeechText.Text("Prompt_Combined_09", useChinese) +
                BattleSpeechText.Text("Prompt_Combined_10", useChinese) +
                BattleSpeechText.Text("Prompt_Combined_11", useChinese) + replyProtocolLine;
            string battlefieldBlock = battlefieldFacts?.ToPromptBlock(useChinese) ?? string.Empty;
            return BattleSpeechText.Text("Prompt_Combined_12", useChinese) +
                   BattleSpeechText.Text("Prompt_Combined_13", useChinese) +
                   BuildNpcSpeechWritingRules(minimumChars, maximumChars, combatOnly: false, useChinese: useChinese) +
                   BattleSpeechText.Text("Prompt_Combined_14", useChinese) +
                   BattleSpeechText.Text("Prompt_Combined_15", useChinese) + keys + BattleSpeechText.Text("Prompt_Combined_16", useChinese) +
                   BattleSpeechText.Text("Prompt_Combined_17", useChinese) +
                   BattleSpeechText.Text("Prompt_Combined_18", useChinese) + replyRule + BattleSpeechText.Text("Prompt_Combined_19", useChinese) +
                   BattleSpeechText.Text("Prompt_Combined_20", useChinese) +
                   AppendPromptBlock(battlefieldBlock) +
                   AppendPromptBlock(diversity) +
                   AppendPromptBlock(attempt) +
                   "\n" + protocolLines;
        }

        public static string BuildCombatNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            int audienceReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts)
        {
            return BuildCombatNpcSpeechPromptInstruction(minimumChars, maximumChars, audienceReplyCount, audienceReplyMinimumChars, audienceReplyMaximumChars, recentSpeechTexts, generationAttempt, battlefieldFacts, true);
        }

        public static string BuildCombatNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            int audienceReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts,
            bool useChinese)
        {
            if (minimumChars < 6 || maximumChars > 160 || maximumChars < minimumChars)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumChars),
                    "Battle speech prompt length must stay within 6..160 characters.");
            }
            ValidateAudienceReplyLength(
                audienceReplyMinimumChars,
                audienceReplyMaximumChars);
            int boundedReplyCount = Math.Max(
                0,
                Math.Min(MaximumAudienceReplies, audienceReplyCount));
            string replyProtocolLine = BuildReplyProtocolLine(boundedReplyCount, useChinese);
            string replyRule = boundedReplyCount == 0
                ? BattleSpeechText.Text("Prompt_Combat_01", useChinese)
                : BattleSpeechText.Text("Prompt_Combat_02", useChinese) + boundedReplyCount + BattleSpeechText.Text("Prompt_Combat_03", useChinese) +
                  audienceReplyMinimumChars + BattleSpeechText.Text("Prompt_Combat_04", useChinese) + audienceReplyMaximumChars +
                  BattleSpeechText.Text("Prompt_Combat_05", useChinese);
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(useChinese, recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? BattleSpeechText.Text("Prompt_Combat_06", useChinese)
                : string.Empty;
            string battlefieldBlock = battlefieldFacts?.ToPromptBlock(useChinese) ?? string.Empty;
            return BattleSpeechText.Text("Prompt_Combat_07", useChinese) +
                   BattleSpeechText.Text("Prompt_Combat_08", useChinese) +
                   BuildNpcSpeechWritingRules(minimumChars, maximumChars, combatOnly: true, useChinese: useChinese) +
                   BattleSpeechText.Text("Prompt_Combat_09", useChinese) + replyRule +
                   BattleSpeechText.Text("Prompt_Combat_10", useChinese) +
                   AppendPromptBlock(battlefieldBlock) +
                   AppendPromptBlock(diversity) +
                   AppendPromptBlock(attempt) +
                   BattleSpeechText.Text("Prompt_Combat_11", useChinese) +
                   BattleSpeechText.Text("Prompt_Combat_12", useChinese) + replyProtocolLine;
        }

        private static string AppendPromptBlock(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : "\n" + value.Trim();
        }

        public static string NormalizeNpcSpeechReply(
            string rawText,
            int minimumChars,
            int maximumChars)
        {
            return NormalizeNpcSpeechReply(
                rawText,
                minimumChars,
                maximumChars,
                out _);
        }

        public static string NormalizeNpcSpeechReply(
            string rawText,
            int minimumChars,
            int maximumChars,
            out string fallbackReason)
        {
            return NormalizeNpcSpeechReply(
                rawText,
                minimumChars,
                maximumChars,
                null,
                out fallbackReason);
        }

        public static string NormalizeNpcSpeechReply(
            string rawText,
            int minimumChars,
            int maximumChars,
            IEnumerable<string> forbiddenNames,
            out string fallbackReason)
        {
            return NormalizeNpcSpeechReply(rawText, minimumChars, maximumChars, forbiddenNames, out fallbackReason, true);
        }

        public static string NormalizeNpcSpeechReply(
            string rawText,
            int minimumChars,
            int maximumChars,
            IEnumerable<string> forbiddenNames,
            out string fallbackReason,
            bool useChinese)
        {
            fallbackReason = null;
            if (minimumChars < 6 || maximumChars > 160 || maximumChars < minimumChars)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumChars),
                    "Battle speech reply length must stay within 6..160 characters.");
            }

            string text = Regex.Replace(
                    rawText ?? string.Empty,
                    @"\*+[^*\r\n]*\*+",
                    string.Empty,
                    RegexOptions.CultureInvariant)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();
            text = Regex.Replace(text, @"\s+", " ").Trim();

            string candidate = text;
            string rejectionReason = GetTroopSpeechRejectionReason(
                candidate,
                forbiddenNames);
            if (rejectionReason == null && HasNaturalSpeech(candidate))
            {
                return candidate;
            }
            fallbackReason = rejectionReason ?? "no_speech_text";

            return SelectLocalFallbackSpeech(
                rawText,
                fallbackReason,
                minimumChars,
                maximumChars, useChinese);
        }

        public static IReadOnlyList<string> GetLocalFallbackSpeechVariants()
        {
            return GetLocalFallbackSpeechVariants(true);
        }

        public static IReadOnlyList<string> GetLocalFallbackSpeechVariants(bool useChinese)
        {
            return new ReadOnlyCollection<string>(BattleSpeechText.List("LocalFallbackSpeeches", useChinese).ToArray());
        }

        public static BattleSpeechCombinedNpcResponseV2 ApplyPoliticalAllegianceGuard(
            BattleSpeechCombinedNpcResponseV2 response,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts,
            int minimumChars,
            int maximumChars,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out bool speechReplaced,
            out int repliesReplaced)
        {
            return ApplyPoliticalAllegianceGuard(response, battlefieldFacts, minimumChars, maximumChars, audienceReplyMinimumChars, audienceReplyMaximumChars, out speechReplaced, out repliesReplaced, true);
        }

        public static BattleSpeechCombinedNpcResponseV2 ApplyPoliticalAllegianceGuard(
            BattleSpeechCombinedNpcResponseV2 response,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts,
            int minimumChars,
            int maximumChars,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out bool speechReplaced,
            out int repliesReplaced,
            bool useChinese)
        {
            speechReplaced = false;
            repliesReplaced = 0;
            if (response == null || battlefieldFacts == null)
            {
                return response;
            }

            if (ContainsDisallowedPoliticalSlogan(response.SpeechText, battlefieldFacts))
            {
                speechReplaced = true;
                string safeSpeech = SelectLocalFallbackSpeech(
                    response.SpeechText,
                    "political_allegiance_mismatch",
                    minimumChars,
                    maximumChars, useChinese);
                IReadOnlyList<string> safeReplies = BuildFallbackAudienceReplies(
                    safeSpeech,
                    response.Plan.AudienceReplies.Count,
                    audienceReplyMinimumChars,
                    audienceReplyMaximumChars, useChinese);
                repliesReplaced = response.Plan.AudienceReplies.Count;
                return new BattleSpeechCombinedNpcResponseV2(
                    safeSpeech,
                    new BattleSpeechPlanDecisionV2(
                        null,
                        BattleSpeechTacticV2.None,
                        safeReplies));
            }

            List<string> replies = response.Plan.AudienceReplies.ToList();
            IReadOnlyList<string> fallbackPool = BuildFallbackAudienceReplies(
                response.SpeechText,
                Math.Max(replies.Count, 1),
                audienceReplyMinimumChars,
                audienceReplyMaximumChars, useChinese);
            HashSet<string> used = new HashSet<string>(
                replies.Where(reply => !ContainsDisallowedPoliticalSlogan(
                    reply,
                    battlefieldFacts)),
                StringComparer.Ordinal);
            int fallbackIndex = 0;
            for (int index = 0; index < replies.Count; index++)
            {
                if (!ContainsDisallowedPoliticalSlogan(replies[index], battlefieldFacts))
                {
                    continue;
                }
                string replacement = null;
                while (fallbackIndex < fallbackPool.Count)
                {
                    string candidate = fallbackPool[fallbackIndex++];
                    if (used.Add(candidate))
                    {
                        replacement = candidate;
                        break;
                    }
                }
                if (string.IsNullOrWhiteSpace(replacement))
                {
                    replacement = BattleSpeechText.Text("PoliticalReplyFallback", useChinese);
                }
                replies[index] = replacement;
                repliesReplaced++;
            }
            if (repliesReplaced == 0)
            {
                return response;
            }
            return new BattleSpeechCombinedNpcResponseV2(
                response.SpeechText,
                new BattleSpeechPlanDecisionV2(
                    response.Plan.ActionProgram,
                    response.Plan.Tactic,
                    replies));
        }

        public static bool ContainsDisallowedPoliticalSlogan(
            string text,
            BattleSpeechBattlefieldFactsV1 battlefieldFacts)
        {
            if (string.IsNullOrWhiteSpace(text) || battlefieldFacts == null)
            {
                return false;
            }
            HashSet<string> candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(battlefieldFacts.SpeakerCultureName) &&
                !battlefieldFacts.IsFriendlyPoliticalName(
                    battlefieldFacts.SpeakerCultureName))
            {
                candidates.Add(battlefieldFacts.SpeakerCultureName);
            }
            foreach (string enemyFactionName in battlefieldFacts.EnemyFactionNames)
            {
                if (!battlefieldFacts.IsFriendlyPoliticalName(enemyFactionName))
                {
                    candidates.Add(enemyFactionName);
                }
            }
            foreach (string candidate in candidates)
            {
                // "Watch for <enemy>!" is a warning, not a pledge of allegiance.
                string rally = "For " + candidate + "!";
                int rallyIndex = text.IndexOf(rally, StringComparison.OrdinalIgnoreCase);
                while (rallyIndex >= 0)
                {
                    int before = rallyIndex - 1;
                    while (before >= 0 && char.IsWhiteSpace(text[before])) before--;
                    if (before < 0 || ".!?;:".IndexOf(text[before]) >= 0) return true;
                    rallyIndex = text.IndexOf(rally, rallyIndex + rally.Length, StringComparison.OrdinalIgnoreCase);
                }
                foreach (string phrase in new[]
                {
                    candidate + SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0037"),
                    SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0038") + candidate,
                    candidate + SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0039"),
                    candidate + SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0040"),
                    candidate + SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0041"),
                    candidate + SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0042"),
                    SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0043") + candidate,
                    SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0044") + candidate,
                    SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0045") + candidate,
                    SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0046") + candidate + SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0047"),
                    SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0048") + candidate,
                    SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0049") + candidate,
                    "Long live " + candidate + "!",
                    "Long live " + candidate + ".",
                    "Glory to " + candidate + "!",
                    "Glory to " + candidate + ".",
                    "We fight for " + candidate + "!",
                    "We swear allegiance to " + candidate,
                    "I swear allegiance to " + candidate,
                    "We pledge allegiance to " + candidate,
                    "I pledge allegiance to " + candidate
                })
                {
                    if (text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string SelectLocalFallbackSpeech(
            string rawText,
            string fallbackReason,
            int minimumChars,
            int maximumChars,
            bool useChinese)
        {
            string[] speeches = BattleSpeechText.List("LocalFallbackSpeeches", useChinese);
            uint seed = StableHash(
                (rawText ?? string.Empty) + ":" + (fallbackReason ?? string.Empty));
            int index = (int)(seed % (uint)speeches.Length);
            return BuildFallbackForLength(
                speeches[index],
                minimumChars,
                maximumChars, useChinese);
        }

        public static string ExtractSpeechBodyForFallback(string rawText)
        {
            string source = rawText ?? string.Empty;
            int contentMarker = source.IndexOf("[CONTENT]", StringComparison.Ordinal);
            if (contentMarker >= 0)
            {
                source = source.Substring(contentMarker + "[CONTENT]".Length);
            }
            string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int begin = Array.FindIndex(lines, line =>
                string.Equals(line.Trim(), "SPEECH_BEGIN", StringComparison.Ordinal));
            if (begin >= 0)
            {
                int end = -1;
                for (int index = begin + 1; index < lines.Length; index++)
                {
                    if (string.Equals(lines[index].Trim(), "SPEECH_END", StringComparison.Ordinal))
                    {
                        end = index;
                        break;
                    }
                }
                if (end > begin)
                {
                    return string.Join(string.Empty, lines.Skip(begin + 1).Take(end - begin - 1)).Trim();
                }
            }
            return lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))?.Trim() ?? string.Empty;
        }

        private static bool HasNaturalSpeech(string text)
        {
            // Package language selects instructions and fallback prose, never
            // which natural language the player/model is permitted to use.
            return !string.IsNullOrEmpty(text) && text.Any(char.IsLetter);
        }

        private static bool ContainsChinese(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.Any(character => character >= '\u4e00' && character <= '\u9fff');
        }

        private static void ValidateAudienceReplyLength(
            int minimumChars,
            int maximumChars)
        {
            if (minimumChars < 1 || maximumChars < minimumChars || maximumChars > 80)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumChars),
                    "Audience reply length must stay within 1..80 characters.");
            }
        }

        private static string BuildFallbackForLength(
            string source,
            int minimumChars,
            int maximumChars,
            bool useChinese)
        {
            string coherentFallback = string.IsNullOrWhiteSpace(source)
                ? BattleSpeechText.List("LocalFallbackSpeeches", useChinese)[0]
                : source.Trim();
            string bounded = TakeCompleteSpeech(
                coherentFallback,
                minimumChars,
                maximumChars, useChinese);
            if (bounded.Length >= minimumChars)
            {
                return bounded;
            }
            string expanded = coherentFallback +
                              BattleSpeechText.Text("FallbackExtraLine", useChinese);
            bounded = TakeCompleteSpeech(expanded, minimumChars, maximumChars, useChinese);
            if (bounded.Length >= minimumChars)
            {
                return bounded;
            }
            string seed = expanded.Substring(
                0,
                Math.Min(maximumChars, expanded.Length));
            if (seed.Length == maximumChars && maximumChars > 0)
            {
                seed = seed.Substring(0, maximumChars - 1) + (useChinese ? "！" : "!");
            }
            return seed;
        }

        private static string TakeCompleteSpeech(
            string text,
            int minimumChars,
            int maximumChars,
            bool useChinese)
        {
            if (text.Length <= maximumChars)
            {
                return text;
            }
            MatchCollection sentences = Regex.Matches(text, @"[^。！？.!?；;]+[。！？.!?；;]");
            string result = string.Empty;
            foreach (Match sentence in sentences)
            {
                if (result.Length + sentence.Value.Length > maximumChars)
                {
                    break;
                }
                result += sentence.Value;
            }
            result = result.Trim();
            if (result.Length >= minimumChars)
            {
                return result;
            }

            string prefix = text.Substring(0, maximumChars).Trim();
            int lastBoundary = prefix.LastIndexOfAny(
                new[] { '。', '！', '？', '.', '!', '?', '；', ';', '，', ',' });
            if (lastBoundary + 1 >= minimumChars)
            {
                return prefix.Substring(0, lastBoundary + 1).Trim();
            }
            if (prefix.Length == maximumChars && maximumChars > 0 &&
                Array.IndexOf(new[] { '。', '！', '？', '.', '!', '?', '；', ';' },
                    prefix[prefix.Length - 1]) < 0)
            {
                prefix = prefix.Substring(0, prefix.Length - 1) + (ContainsChinese(text) ? "。" : ".");
            }
            return prefix;
        }

        private static bool IsValidTroopAddress(string text)
        {
            return GetTroopSpeechRejectionReason(text) == null;
        }

        private static string GetTroopSpeechRejectionReason(
            string text,
            IEnumerable<string> forbiddenNames = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "empty";
            }
            if (SpeechProtocolOnlyPattern.IsMatch(text))
            {
                return "protocol_instead_of_speech";
            }
            if (EnglishSpeechMetaMarkers.Any(marker => text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return "meta_or_test_text";
            }
            if (EnglishPlayerReplyMarkers.Any(marker => text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return "forbidden_player_reply_phrase";
            }
            if (ContainsDirectPlayerAddress(text))
            {
                return "direct_player_address";
            }
            if (forbiddenNames != null && forbiddenNames.Any(name =>
                    !string.IsNullOrWhiteSpace(name) &&
                    text.IndexOf(name.Trim(), StringComparison.Ordinal) >= 0))
            {
                return "player_name";
            }
            if (SpeechMetaMarkers.Any(marker =>
                    text.IndexOf(marker, StringComparison.Ordinal) >= 0))
            {
                return "meta_or_test_text";
            }
            string[] forbidden =
            {
                SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0050"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0051"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0052"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0053"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0054"),
                SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0055"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0056"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0057"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0058"),
                SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0059"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0060"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0061"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0062")
            };
            if (forbidden.Any(value => text.IndexOf(value, StringComparison.Ordinal) >= 0))
            {
                return "forbidden_player_reply_phrase";
            }
            if (text.IndexOf('*') >= 0 || text.IndexOf('[') >= 0 || text.IndexOf(']') >= 0)
            {
                return "stage_marker_or_metadata";
            }
            return null;
        }

        private static bool ContainsDirectPlayerAddress(string text)
        {
            if (Regex.IsMatch(text,
                @"(?:^|[.!?,]\s*)(?:my lord|my lady|your grace|your lordship|sir|madam)(?:[,!:?]|\s+(?:I|we|you|please)\b)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return true;
            }
            string[] titles = { SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0063"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0064"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0065") };
            foreach (string title in titles)
            {
                int searchStart = 0;
                while (searchStart < text.Length)
                {
                    int index = text.IndexOf(
                        title,
                        searchStart,
                        StringComparison.Ordinal);
                    if (index < 0)
                    {
                        break;
                    }

                    // A title at the beginning is a direct form of address.
                    if (index == 0)
                    {
                        return true;
                    }

                    // Preserve third-person descriptions such as
                    // “加尼密诺斯大人骑着马” and “敌方领主逼近”。
                    // A short request prefix, however, indicates that the
                    // title is being used to address the player.
                    string[] directPrefixes =
                    {
                        SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0066"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0067"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0068"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0069"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0070"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0071"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0072"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0073")
                    };
                    if (directPrefixes.Any(prefix =>
                            index >= prefix.Length &&
                            text.Substring(index - prefix.Length, prefix.Length)
                                .Equals(prefix, StringComparison.Ordinal)))
                    {
                        return true;
                    }

                    searchStart = index + title.Length;
                }
            }
            return false;
        }

        public static string SelectClosingCommandActionId(
            bool mounted,
            bool hasWieldedWeapon)
        {
            if (mounted)
            {
                return hasWieldedWeapon
                    ? "act_horse_command"
                    : "act_horse_command_unarmed";
            }
            return hasWieldedWeapon ? "act_command" : "act_command_unarmed";
        }

        private static readonly Regex SpeechProtocolOnlyPattern = new Regex(
            @"\A(?:NONE|ACCEPT|REFUSE|UNCLEAR|(?:PLAY_ACTION|PLAY_PROGRAM|ACTIONS|TACTIC|REPLIES)(?:\s+[^\r\n]*)?)\z",
            RegexOptions.CultureInvariant);

        private static readonly string[] EnglishSpeechMetaMarkers =
        {
            "as an ai assistant", "as an ai language model", "as an ai,", "as a language model",
            "here is the speech", "here is a sample speech", "here's a sample speech", "speech text:", "for testing purposes"
        };

        private static readonly string[] EnglishPlayerReplyMarkers =
        {
            "at your command", "awaiting your orders", "waiting for your orders", "please give me orders",
            "i am just an ordinary soldier", "i'm just an ordinary soldier", "what would you have me do"
        };

        private static readonly string[] NegationCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0074"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0075"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0076"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0077"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0078"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0079"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0080"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0081"),
            "not ", "don't", "do not", "never"
        };

        private static readonly string[] HypotheticalOrQuotedCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0082"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0083"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0084"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0085"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0086"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0087"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0088"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0089"), "if ", "what if", "quote"
        };

        private static readonly string[] AudienceCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0090"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0091"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0092"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0093"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0094"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0095"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0096"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0097"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0098"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0099"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0100"), "troops", "soldiers", "everyone", "army"
        };

        private static readonly string[] SpeechCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0008"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0101"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0102"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0103"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0104"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0105"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0106"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0107"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0108"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0109"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0110"), "speech", "address the troops",
            "rally the troops"
        };

        private static readonly string[] SpeechRequestCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0111"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0112"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0113"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0114"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0115"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0116"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0117"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0118"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0119"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0120"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0121"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0122"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0123"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0124"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0125"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0126"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0127"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0128"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0129"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0130"), "give ", "go give",
            "please give", "address "
        };

        private static readonly string[] SelfCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0131"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0132"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0133"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0134"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0135"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0136"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0137"), "my speech",
            "i will", "let me"
        };

        private static readonly string[] DirectTroopAddressCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0138"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0139"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0140"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0141"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0142"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0097"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0143"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0144"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0145"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0146"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0147"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0090"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0148"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0149"), "troops", "soldiers", "brothers", "everyone"
        };

        private static readonly string[] CollectiveSpeechCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0150"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0151"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0152"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0153"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0154"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0155"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0156"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0157"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0158"), "let us", "let's", "we must", "we will"
        };

        private static readonly string[] DirectRallyCues =
        {
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0159"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0160"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0161"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0162"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0163"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0164"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0165"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0166"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0167"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0168"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0169"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0170"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0171"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0172"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0173"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0174"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0175"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0176"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0038"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0177"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0040"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0178"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0179"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0180"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0181"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0182"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0183"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0184"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0156"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0185"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0186"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0187"),
            SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0188"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0189"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0190"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0191"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0192"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0048"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0193"), SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0194"),
            "fight",
            "enemy", "victory", "forward", "retreat", "battle", "hold the line"
        };

        public static BattleSpeechTriggerDecisionV2 ParsePlayerShout(string rawText)
        {
            string text = Normalize(rawText);
            if (text.Length == 0)
            {
                return None();
            }

            // Forced colon commands must win over the legacy inline speech parser.
            // The colon is the explicit safety boundary; only the player route is
            // accepted here. NPC actor selection belongs to the Y-menu entry.
            if (TryReadForcedSpeech(
                    text,
                    ForcedPlayerSpeechPrefixes,
                    out string forcedPlayerSpeech))
            {
                return BuildForcedDecision(
                    BattleSpeechTriggerKindV2.DeliverPlayerSpeech,
                    forcedPlayerSpeech);
            }
            if (LooksLikeActorRerouteSpeech(text))
            {
                // Old T-key NPC syntax is deliberately a normal AF scene
                // message now. It must not be reclassified as player speech or
                // create an NPC session; Y-menu NPC speech has its own frozen
                // internal entry.
                return new BattleSpeechTriggerDecisionV2(
                    BattleSpeechTriggerKindV2.OrdinaryScene,
                    reason: "NPC speech actor syntax is available only through the Y menu.");
            }
            BattleSpeechCommandDecisionV1 legacy = BattleSpeechFrameworkV1.ParsePlayerShout(rawText);
            if (legacy.IsControl)
            {
                return new BattleSpeechTriggerDecisionV2(MapLegacy(legacy.Kind), legacy.SpeechText, legacy.Error);
            }

            // A delivered speech may legitimately contain words such as "不要" or "如果".
            // Resolve the command-shaped prefix before applying safety cues to requests.
            if (TryReadInlinePlayerSpeech(text, out string speech))
            {
                return new BattleSpeechTriggerDecisionV2(
                    speech.Length == 0
                        ? BattleSpeechTriggerKindV2.ArmPlayerSpeech
                        : BattleSpeechTriggerKindV2.DeliverPlayerSpeech,
                    speech);
            }

            // A player may begin with the actual troop-facing speech instead of
            // saying "演讲" first. Treat only a clear address plus battle-rally
            // rhetoric as a classifier candidate; the classifier still decides
            // PLAYER_SPEECH/NONE, and quoted or reported text remains blocked.
            if (LooksLikeDirectPlayerSpeech(text))
            {
                return new BattleSpeechTriggerDecisionV2(
                    BattleSpeechTriggerKindV2.DeliverPlayerSpeech,
                    speechText: text,
                    reason: null);
            }
            if (LooksLikeDirectPlayerSpeechCandidate(text))
            {
                return new BattleSpeechTriggerDecisionV2(
                    BattleSpeechTriggerKindV2.NeedsClassifier,
                    reason: "Direct troop-facing speech requires closed classification.");
            }
            if (LooksLikeUnlabeledDirectPlayerSpeechCandidate(text))
            {
                return new BattleSpeechTriggerDecisionV2(
                    BattleSpeechTriggerKindV2.NeedsClassifier,
                    reason: "Unlabeled troop-facing prose requires closed classification.");
            }
            if (NegationCues.Any(cue => Contains(text, cue)) ||
                HypotheticalOrQuotedCues.Any(cue => Contains(text, cue)))
            {
                return None();
            }

            bool hasSpeech = SpeechCues.Any(cue => Contains(text, cue));
            if (!hasSpeech)
            {
                return LooksLikeLooseSpeechRequest(text)
                    ? new BattleSpeechTriggerDecisionV2(
                        BattleSpeechTriggerKindV2.NeedsClassifier,
                        reason: "Loose battle-speech request requires closed classification.")
                    : None();
            }

            if (SelfCues.Any(cue => StartsWith(text, cue)))
            {
                return new BattleSpeechTriggerDecisionV2(
                    BattleSpeechTriggerKindV2.ArmPlayerSpeech);
            }
            return new BattleSpeechTriggerDecisionV2(
                BattleSpeechTriggerKindV2.NeedsClassifier,
                reason: "Battle-speech wording is ambiguous and requires closed classification.");
        }

        /// <summary>
        /// Parses text submitted from the dedicated Y-menu speech entry.
        /// The entry has already selected the battle-speech channel, so ordinary
        /// prose is a player speech body and must not be sent through the T-key
            /// scene-shout classifier. The menu itself is the only actor selector.
        /// </summary>
        public static BattleSpeechTriggerDecisionV2 ParseDedicatedSpeechInput(string rawText)
        {
            string text = Normalize(rawText);
            if (text.Length == 0)
            {
                return None();
            }

            BattleSpeechTriggerDecisionV2 explicitDecision = ParsePlayerShout(text);
            if (
                 explicitDecision.Kind == BattleSpeechTriggerKindV2.Cancel ||
                 explicitDecision.Kind == BattleSpeechTriggerKindV2.DeliverPlayerSpeech ||
                 explicitDecision.Kind == BattleSpeechTriggerKindV2.ArmPlayerSpeech &&
                !string.IsNullOrWhiteSpace(explicitDecision.SpeechText))
            {
                return explicitDecision;
            }

            // The Y-menu is an already-authorized speech channel. A body such as
            // "弟兄们，守住阵线" therefore does not need a speech keyword or
            // a second classifier request.
            return new BattleSpeechTriggerDecisionV2(
                BattleSpeechTriggerKindV2.DeliverPlayerSpeech,
                speechText: text);
        }

        /// <summary>
        /// Creates the internal NPC trigger used only by the Y-menu "他人演讲"
        /// entry. It is intentionally not derived from player text, so T-key
        /// input and the lightweight trigger classifier can never retarget an
        /// NPC speaker.
        /// </summary>
        public static BattleSpeechTriggerDecisionV2 ParseDedicatedNpcSpeechInput()
        {
            return new BattleSpeechTriggerDecisionV2(
                BattleSpeechTriggerKindV2.RequestNpcSpeech,
                speechText: null,
                reason: null,
                force: true);
        }

        public static bool TryParseTriggerClassifierOutput(
            string output,
            out BattleSpeechTriggerKindV2 kind)
        {
            kind = BattleSpeechTriggerKindV2.None;
            string value = (output ?? string.Empty).Trim();
            if (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)
            {
                return false;
            }
            if (string.Equals(value, "NONE", StringComparison.Ordinal))
            {
                return true;
            }
            if (string.Equals(value, "PLAYER_SPEECH", StringComparison.Ordinal))
            {
                kind = BattleSpeechTriggerKindV2.ArmPlayerSpeech;
                return true;
            }
            if (string.Equals(value, "ORDINARY_SCENE", StringComparison.Ordinal))
            {
                kind = BattleSpeechTriggerKindV2.OrdinaryScene;
                return true;
            }
            return false;
        }

        public static bool TryParsePlanClassifierOutput(
            string output,
            out BattleSpeechPlanDecisionV2 decision,
            out string error)
        {
            return TryParsePlanClassifierOutput(
                output,
                1,
                24,
                out decision,
                out error);
        }

        public static bool TryParsePlanClassifierOutput(
            string output,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out BattleSpeechPlanDecisionV2 decision,
            out string error)
        {
            return TryParsePlanClassifierOutput(output, audienceReplyMinimumChars, audienceReplyMaximumChars, out decision, out error, true);
        }

        public static bool TryParsePlanClassifierOutput(
            string output,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out BattleSpeechPlanDecisionV2 decision,
            out string error,
            bool useChinese)
        {
            decision = null;
            error = null;
            ValidateAudienceReplyLength(
                audienceReplyMinimumChars,
                audienceReplyMaximumChars);
            string normalized = (output ?? string.Empty).Replace("\r\n", "\n");
            if (normalized.IndexOf('\r') >= 0)
            {
                error = "Classifier output contains unsupported line endings.";
                return false;
            }
            string[] lines = normalized.Split('\n');
            if ((lines.Length != 2 && lines.Length != 3) ||
                lines.Any(line => line != line.Trim()))
            {
                error = "Battle speech classifier output must contain two or three trimmed lines.";
                return false;
            }
            const string actionsPrefix = "ACTIONS ";
            const string tacticPrefix = "TACTIC ";
            if (!lines[0].StartsWith(actionsPrefix, StringComparison.Ordinal) ||
                !lines[1].StartsWith(tacticPrefix, StringComparison.Ordinal))
            {
                error = "Battle speech classifier output prefixes are invalid.";
                return false;
            }

            string actionValue = lines[0].Substring(actionsPrefix.Length);
            ActionProgramV4 program = null;
            if (!string.Equals(actionValue, "NONE", StringComparison.Ordinal))
            {
                string expression;
                if (actionValue.StartsWith("PLAY_ACTION ", StringComparison.Ordinal))
                {
                    expression = actionValue.Substring("PLAY_ACTION ".Length);
                }
                else if (actionValue.StartsWith("PLAY_PROGRAM ", StringComparison.Ordinal))
                {
                    expression = actionValue.Substring("PLAY_PROGRAM ".Length);
                }
                else
                {
                    error = "Battle speech action protocol is invalid.";
                    return false;
                }
                if (!ActionProgramV4.TryParseExpression(expression, out program, out error))
                {
                    return false;
                }
                if (actionValue.StartsWith("PLAY_ACTION ", StringComparison.Ordinal) &&
                    !program.IsSingleAction)
                {
                    error = "PLAY_ACTION must contain exactly one action.";
                    return false;
                }
            }

            string tacticValue = lines[1].Substring(tacticPrefix.Length);
            BattleSpeechTacticV2 tactic;
            if (string.Equals(tacticValue, "NONE", StringComparison.Ordinal))
            {
                tactic = BattleSpeechTacticV2.None;
            }
            else if (string.Equals(tacticValue, "ADVANCE", StringComparison.Ordinal))
            {
                tactic = BattleSpeechTacticV2.Advance;
            }
            else
            {
                error = "Battle speech tactic is outside NONE/ADVANCE.";
                return false;
            }
            List<string> replies = new List<string>();
            if (lines.Length == 3)
            {
                const string repliesPrefix = "REPLIES ";
                if (!lines[2].StartsWith(repliesPrefix, StringComparison.Ordinal))
                {
                    error = "Battle speech audience-reply prefix is invalid.";
                    return false;
                }
                string repliesValue = lines[2].Substring(repliesPrefix.Length);
                if (!string.Equals(repliesValue, "NONE", StringComparison.Ordinal))
                {
                    string[] values = repliesValue.Split('|');
                    if (values.Length == 0)
                    {
                        error = "Battle speech audience replies are empty.";
                        return false;
                    }
                    foreach (string value in values.Take(MaximumAudienceReplies))
                    {
                        string reply = value.Trim();
                        if (!HasNaturalSpeech(reply) ||
                            reply.IndexOfAny(new[] { '\r', '\n', '*', '<', '>', '[', ']' }) >= 0 ||
                            SpeechProtocolOnlyPattern.IsMatch(reply) ||
                            string.Equals(reply, "NONE", StringComparison.Ordinal))
                        {
                            error = "Battle speech audience reply is invalid.";
                            return false;
                        }
                        if (!replies.Contains(reply, StringComparer.Ordinal))
                        {
                            replies.Add(reply);
                        }
                    }
                }
            }
            decision = new BattleSpeechPlanDecisionV2(program, tactic, replies);
            return true;
        }

        public static bool TryParseCombinedNpcSpeechOutput(
            string output,
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int expectedReplyCount,
            out BattleSpeechCombinedNpcResponseV2 response,
            out string error)
        {
            return TryParseCombinedNpcSpeechOutput(
                output,
                minimumChars,
                maximumChars,
                allowedIntentKeys,
                expectedReplyCount,
                8,
                24,
                out response,
                out error);
        }

        public static bool TryParseCombinedNpcSpeechOutput(
            string output,
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int expectedReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out BattleSpeechCombinedNpcResponseV2 response,
            out string error)
        {
            return TryParseCombinedNpcSpeechOutput(output, minimumChars, maximumChars, allowedIntentKeys, expectedReplyCount, audienceReplyMinimumChars, audienceReplyMaximumChars, out response, out error, true);
        }

        public static bool TryParseCombinedNpcSpeechOutput(
            string output,
            int minimumChars,
            int maximumChars,
            IEnumerable<string> allowedIntentKeys,
            int expectedReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out BattleSpeechCombinedNpcResponseV2 response,
            out string error,
            bool useChinese)
        {
            response = null;
            error = null;
            ValidateAudienceReplyLength(
                audienceReplyMinimumChars,
                audienceReplyMaximumChars);
            string normalized = NormalizeCombinedProtocolEnvelope(output);
            if (normalized.IndexOf('\r') >= 0)
            {
                error = "Combined speech output contains unsupported line endings.";
                return false;
            }
            normalized = RepairLegacyActionMarker(normalized);
            string[] lines = normalized.Split('\n');
            if (lines.Length != 5 && lines.Length != 6 ||
                lines.Any(line => line != line.Trim()))
            {
                error = "Combined speech output must contain the closed five/six-line protocol.";
                return false;
            }
            if (!string.Equals(lines[0], "SPEECH_BEGIN", StringComparison.Ordinal) ||
                !string.Equals(lines[2], "SPEECH_END", StringComparison.Ordinal))
            {
                error = "Combined speech markers are invalid.";
                return false;
            }
            RepairMissingCombinedRepliesMarker(lines, expectedReplyCount);
            string speech = NormalizeNpcSpeechReply(
                lines[1],
                minimumChars,
                maximumChars,
                null,
                out string speechFallbackReason, useChinese);
            if (speechFallbackReason != null ||
                !IsValidTroopAddress(speech) ||
                speech.IndexOf("SPEECH_", StringComparison.Ordinal) >= 0)
            {
                error = "Combined speech body is invalid" +
                        (speechFallbackReason == null
                            ? "."
                            : ": " + speechFallbackReason + ".");
                return false;
            }
            string planOutput = lines.Length == 5
                ? lines[3] + "\n" + lines[4]
                : lines[3] + "\n" + lines[4] + "\n" + lines[5];
            planOutput = RepairLegacyActionMarker(planOutput);
            if (!TryParsePlanClassifierOutput(
                    planOutput,
                    audienceReplyMinimumChars,
                    audienceReplyMaximumChars,
                    out BattleSpeechPlanDecisionV2 plan,
                    out error, useChinese))
            {
                return false;
            }
            if (!ProgramUsesAllowedKeys(plan.ActionProgram, allowedIntentKeys))
            {
                error = "Combined speech action is outside the frozen allow-list.";
                return false;
            }
            plan = LimitAudienceReplies(plan, expectedReplyCount, speech,
                audienceReplyMinimumChars, audienceReplyMaximumChars, useChinese);
            response = new BattleSpeechCombinedNpcResponseV2(speech, plan);
            return true;
        }

        public static bool TryParseCombatNpcSpeechOutput(
            string output,
            int minimumChars,
            int maximumChars,
            int expectedReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out BattleSpeechCombinedNpcResponseV2 response,
            out string error)
        {
            return TryParseCombatNpcSpeechOutput(output, minimumChars, maximumChars, expectedReplyCount, audienceReplyMinimumChars, audienceReplyMaximumChars, out response, out error, true);
        }

        public static bool TryParseCombatNpcSpeechOutput(
            string output,
            int minimumChars,
            int maximumChars,
            int expectedReplyCount,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            out BattleSpeechCombinedNpcResponseV2 response,
            out string error,
            bool useChinese)
        {
            response = null;
            error = null;
            ValidateAudienceReplyLength(
                audienceReplyMinimumChars,
                audienceReplyMaximumChars);
            string normalized = NormalizeCombinedProtocolEnvelope(output);
            if (normalized.IndexOf('\r') >= 0)
            {
                error = "Combat speech output contains unsupported line endings.";
                return false;
            }
            string[] lines = normalized.Split('\n');
            if (lines.Length != 4 || lines.Any(line => line != line.Trim()) ||
                !string.Equals(lines[0], "SPEECH_BEGIN", StringComparison.Ordinal) ||
                !string.Equals(lines[2], "SPEECH_END", StringComparison.Ordinal))
            {
                error = "Combat speech output must contain the closed four-line protocol.";
                return false;
            }
            RepairMissingCombinedRepliesMarker(lines, expectedReplyCount);
            string speech = NormalizeNpcSpeechReply(
                lines[1],
                minimumChars,
                maximumChars,
                null,
                out string speechFallbackReason, useChinese);
            if (speechFallbackReason != null ||
                !IsValidTroopAddress(speech) ||
                speech.IndexOf("SPEECH_", StringComparison.Ordinal) >= 0)
            {
                error = "Combat speech body is invalid" +
                        (speechFallbackReason == null
                            ? "."
                            : ": " + speechFallbackReason + ".");
                return false;
            }
            string planOutput = "ACTIONS NONE\nTACTIC NONE\n" + lines[3];
            if (!TryParsePlanClassifierOutput(
                    planOutput,
                    audienceReplyMinimumChars,
                    audienceReplyMaximumChars,
                    out BattleSpeechPlanDecisionV2 plan,
                    out error, useChinese))
            {
                return false;
            }
            plan = LimitAudienceReplies(plan, expectedReplyCount, speech,
                audienceReplyMinimumChars, audienceReplyMaximumChars, useChinese);
            response = new BattleSpeechCombinedNpcResponseV2(speech, plan);
            return true;
        }

        private static BattleSpeechPlanDecisionV2 LimitAudienceReplies(
            BattleSpeechPlanDecisionV2 plan,
            int expectedReplyCount,
            string speech,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars,
            bool useChinese)
        {
            int boundedExpected = Math.Max(
                0,
                Math.Min(MaximumAudienceReplies, expectedReplyCount));
            IReadOnlyList<string> replies;
            if (boundedExpected == 0)
            {
                replies = Array.Empty<string>();
            }
            else if (plan.AudienceReplies.Count > 0)
            {
                List<string> completed = plan.AudienceReplies
                    .Take(boundedExpected)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (completed.Count < boundedExpected)
                {
                    foreach (string fallback in BuildFallbackAudienceReplies(
                                 speech,
                                 boundedExpected,
                                 audienceReplyMinimumChars,
                                 audienceReplyMaximumChars, useChinese))
                    {
                        if (completed.Count >= boundedExpected)
                        {
                            break;
                        }
                        if (!completed.Contains(fallback, StringComparer.Ordinal))
                        {
                            completed.Add(fallback);
                        }
                    }
                }
                replies = completed;
            }
            else
            {
                replies = BuildFallbackAudienceReplies(
                    speech,
                    boundedExpected,
                    audienceReplyMinimumChars,
                    audienceReplyMaximumChars, useChinese);
            }
            return new BattleSpeechPlanDecisionV2(
                plan.ActionProgram,
                plan.Tactic,
                replies);
        }

        private static bool ProgramUsesAllowedKeys(
            ActionProgramV4 program,
            IEnumerable<string> allowedIntentKeys)
        {
            if (program == null)
            {
                return true;
            }
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            return program.Steps.SelectMany(step => step.IntentKeys).All(allowed.Contains);
        }

        private static string BuildReplyProtocolLine(int replyCount,
            bool useChinese)
        {
            int bounded = Math.Max(0, Math.Min(MaximumAudienceReplies, replyCount));
            if (bounded == 0)
            {
                return "REPLIES NONE";
            }
            if (bounded == 1)
            {
                return BattleSpeechText.Text("Prompt_ReplyProtocol_01", useChinese);
            }
            if (bounded == 2)
            {
                return BattleSpeechText.Text("Prompt_ReplyProtocol_02", useChinese);
            }
            return BattleSpeechText.Text("Prompt_ReplyProtocol_03", useChinese) + bounded + ">";
        }

        private static void RepairMissingCombinedRepliesMarker(
            string[] lines,
            int expectedReplyCount)
        {
            if (lines == null || (lines.Length != 6 && lines.Length != 4) ||
                expectedReplyCount <= 0)
            {
                return;
            }

            int replyLineIndex = lines.Length - 1;
            if (lines[replyLineIndex].StartsWith("REPLIES ", StringComparison.Ordinal) ||
                lines[replyLineIndex].StartsWith("ACTIONS ", StringComparison.Ordinal) ||
                lines[replyLineIndex].StartsWith("TACTIC ", StringComparison.Ordinal))
            {
                return;
            }

            int boundedExpected = Math.Max(
                0,
                Math.Min(MaximumAudienceReplies, expectedReplyCount));
            string[] candidates = lines[replyLineIndex]
                .Split('|')
                .Select(value => value.Trim())
                .ToArray();
            if (candidates.Length != boundedExpected ||
                candidates.Any(string.IsNullOrWhiteSpace))
            {
                return;
            }

            // Some reasoning-capable endpoints preserve all reply text in the
            // final content but omit only the literal REPLIES marker. Repair
            // that single known drift; the closed plan parser below still
            // validates every reply, action key, tactic and length boundary.
            lines[replyLineIndex] = "REPLIES " + string.Join("|", candidates);
        }

        private static string RepairLegacyActionMarker(string planOutput)
        {
            string[] lines = (planOutput ?? string.Empty).Split('\n');
            if (lines.Length > 0 &&
                lines[0].StartsWith("ACTIONS PLAY_ACTION ", StringComparison.Ordinal))
            {
                string expression = lines[0].Substring("ACTIONS PLAY_ACTION ".Length);
                if (expression.IndexOf('>') >= 0 || expression.IndexOf('+') >= 0)
                {
                    lines[0] = "ACTIONS PLAY_PROGRAM " +
                               NormalizeLegacyProgramExpression(expression);
                }
            }
            else if (lines.Length > 0 &&
                     lines[0].StartsWith("ACTIONS PLAY_PROGRAM ", StringComparison.Ordinal))
            {
                string expression = lines[0].Substring("ACTIONS PLAY_PROGRAM ".Length);
                lines[0] = "ACTIONS PLAY_PROGRAM " +
                           NormalizeLegacyProgramExpression(expression);
            }
            return string.Join("\n", lines);
        }

        private static string NormalizeLegacyProgramExpression(string expression)
        {
            string normalized = expression ?? string.Empty;
            // AF may repeat PLAY_ACTION/PLAY_PROGRAM before every key. This
            // compatibility pass removes only those known markers and spaces
            // around the sequencing operators; the closed parser still checks
            // every resulting key and the four-action limit.
            normalized = Regex.Replace(
                normalized,
                @"\bPLAY_(?:ACTION|PROGRAM)\s+",
                string.Empty,
                RegexOptions.CultureInvariant);
            return Regex.Replace(
                normalized,
                @"\s*([>+])\s*",
                "$1",
                RegexOptions.CultureInvariant);
        }

        private static string NormalizeCombinedProtocolEnvelope(string output)
        {
            string normalized = (output ?? string.Empty).Replace("\r\n", "\n");
            int begin = normalized.IndexOf("SPEECH_BEGIN", StringComparison.Ordinal);
            int end = begin < 0
                ? -1
                : normalized.IndexOf(
                    "SPEECH_END",
                    begin + "SPEECH_BEGIN".Length,
                    StringComparison.Ordinal);
            if (begin < 0 || end < begin)
            {
                return normalized;
            }

            int endExclusive = end + "SPEECH_END".Length;
            int contentStart = begin + "SPEECH_BEGIN".Length;
            string rawSpeechContent = normalized.Substring(contentStart, end - contentStart);
            string singleLineSpeech = string.Join(
                " ",
                rawSpeechContent
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line)));
            string speechBlock = "SPEECH_BEGIN\n" + singleLineSpeech + "\nSPEECH_END";
            string[] rawSuffixLines = normalized
                .Substring(endExclusive)
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            List<string> suffixLines = new List<string>(3);
            foreach (string line in rawSuffixLines)
            {
                bool knownMarker = line.StartsWith("ACTIONS ", StringComparison.Ordinal) ||
                                   line.StartsWith("TACTIC ", StringComparison.Ordinal) ||
                                   line.StartsWith("REPLIES ", StringComparison.Ordinal);
                bool missingRepliesMarkerCandidate = !suffixLines.Any(existing =>
                                                         existing.StartsWith(
                                                             "REPLIES ",
                                                             StringComparison.Ordinal)) &&
                                                     (suffixLines.Count == 0 ||
                                                      suffixLines.Any(existing =>
                                                         existing.StartsWith(
                                                             "TACTIC ",
                                                             StringComparison.Ordinal))) &&
                                                     line.IndexOf('|') >= 0;
                if (!knownMarker && !missingRepliesMarkerCandidate)
                {
                    continue;
                }
                suffixLines.Add(line);
                if (suffixLines.Count == 3)
                {
                    break;
                }
            }
            if (suffixLines.Count == 0)
            {
                return speechBlock;
            }
            return speechBlock + "\n" + string.Join("\n", suffixLines);
        }

        public static IReadOnlyList<string> BuildFallbackAudienceReplies(
            string speechText,
            int count)
        {
            return BuildFallbackAudienceReplies(speechText, count, 8, 24);
        }

        public static IReadOnlyList<string> BuildFallbackAudienceReplies(
            string speechText,
            int count,
            int minimumChars,
            int maximumChars)
        {
            return BuildFallbackAudienceReplies(speechText, count, minimumChars, maximumChars, (speechText ?? string.Empty).Any(character => character >= '\u3400' && character <= '\u9fff'));
        }

        public static IReadOnlyList<string> BuildFallbackAudienceReplies(
            string speechText,
            int count,
            int minimumChars,
            int maximumChars,
            bool useChinese)
        {
            ValidateAudienceReplyLength(minimumChars, maximumChars);
            int boundedCount = Math.Max(0, Math.Min(MaximumAudienceReplies, count));
            if (boundedCount == 0)
            {
                return Array.Empty<string>();
            }
            bool chinese = useChinese;
            string[] pool = BattleSpeechText.List("AudiencePool", chinese);
            string[] suffixes = BattleSpeechText.List("AudienceSuffixes", chinese);
            IEnumerable<string> expandedPool = pool.Concat(
                pool.SelectMany(value => suffixes.Select(suffix => value + suffix)));
            uint seed = StableHash(speechText ?? string.Empty);
            return expandedPool
                .Select((value, index) => new
                {
                    Value = FitFallbackAudienceReply(
                        value,
                        minimumChars,
                        maximumChars,
                        index, useChinese),
                    Index = index
                })
                .Where(value => !string.IsNullOrWhiteSpace(value.Value))
                .GroupBy(value => value.Value, StringComparer.Ordinal)
                .Select(group => group.First())
                .Select(value => new
                {
                    value.Value,
                    Hash = StableHash(seed + ":reply:" + value.Index)
                })
                .OrderBy(value => value.Hash)
                .Select(value => value.Value)
                .Distinct(StringComparer.Ordinal)
                .Take(boundedCount)
                .ToArray();
        }

        private static string FitFallbackAudienceReply(
            string value,
            int minimumChars,
            int maximumChars,
            int index,
            bool useChinese)
        {
            string result = (value ?? string.Empty).Trim();
            string[] fillers = BattleSpeechText.List("AudienceFillers", useChinese);
            int fillerIndex = Math.Abs(index) % fillers.Length;
            while (result.Length < minimumChars)
            {
                string filler = fillers[fillerIndex++ % fillers.Length];
                result += filler;
            }
            if (result.Length > maximumChars)
            {
                result = result.Substring(0, Math.Max(1, maximumChars - 1)) + (useChinese ? "！" : "!");
            }
            return result;
        }

        public static int ResolveAudienceReplyWaveSize(
            int configuredWaveSize,
            int remainingReplies)
        {
            if (remainingReplies <= 0)
            {
                return 0;
            }
            return Math.Min(
                Math.Min(20, Math.Max(2, configuredWaveSize)),
                remainingReplies);
        }

        public static int ResolveAudienceReplyWaveSize(
            Guid sessionId,
            int waveIndex,
            int configuredMaximumWaveSize,
            int remainingReplies)
        {
            if (remainingReplies <= 0)
            {
                return 0;
            }
            if (remainingReplies == 1)
            {
                return 1;
            }
            int maximum = Math.Min(20, Math.Max(2, configuredMaximumWaveSize));
            int randomSize = 2 + (int)(StableHash(
                (sessionId == Guid.Empty ? "empty" : sessionId.ToString("N")) +
                ":audience-wave-size:" + Math.Max(0, waveIndex)) %
                (uint)(maximum - 1));
            int waveSize = Math.Min(randomSize, remainingReplies);

            // Never leave a one-person tail wave when the remaining audience
            // can be partitioned safely. A final singleton looks accidental
            // and contradicts the same-wave participation contract. For a
            // cap of 2 and a remainder of 3, the only valid partition is one
            // three-person wave, so allow that bounded exception rather than
            // emitting a singleton follow-up.
            if (waveSize < remainingReplies && remainingReplies - waveSize == 1)
            {
                if (waveSize < maximum)
                {
                    waveSize++;
                }
                else if (waveSize > 2)
                {
                    waveSize--;
                }
                else if (remainingReplies <= 8)
                {
                    waveSize = remainingReplies;
                }
            }
            return waveSize;
        }

        public static int ResolveAudienceReplyCount(
            bool enabled,
            int configuredCount,
            int audienceCount)
        {
            if (!enabled || configuredCount <= 0 || audienceCount <= 0)
            {
                return 0;
            }
            int bounded = Math.Min(
                MaximumAudienceReplies,
                Math.Min(configuredCount, audienceCount));
            return audienceCount >= 2 ? Math.Max(2, bounded) : bounded;
        }

        public static double ResolveAudienceReplyWaveDelaySeconds(
            Guid sessionId,
            int waveIndex,
            float minimumSeconds,
            float maximumSeconds)
        {
            if (minimumSeconds < 0.1f || maximumSeconds > 0.5f || maximumSeconds < minimumSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSeconds));
            }
            double fraction = (StableHash(
                (sessionId == Guid.Empty ? "empty" : sessionId.ToString("N")) +
                ":audience-wave:" + Math.Max(0, waveIndex)) % 10000u) / 9999d;
            return minimumSeconds + (maximumSeconds - minimumSeconds) * fraction;
        }

        public static bool ShouldOpenAudienceResponse(
            bool speechCompleted,
            int finalAudienceSubmitted,
            int firstVisualWaveTarget,
            bool allVisualCuesProcessed,
            bool allowDuringSpeech = false)
        {
            if (!speechCompleted && !allowDuringSpeech)
            {
                return false;
            }
            if (finalAudienceSubmitted <= 0)
            {
                return true;
            }
            return finalAudienceSubmitted >= Math.Max(1, firstVisualWaveTarget) ||
                   allVisualCuesProcessed;
        }

        /// <summary>
        /// Combat speeches keep only the written soldier-reply channel. Visual
        /// reactions, native battle cries, and formation commands must not
        /// replace live combat behavior.
        /// </summary>
        public static bool ShouldSubmitAudienceVisuals(
            bool combatSpeechMode,
            bool configuredEnabled)
        {
            return configuredEnabled && !combatSpeechMode;
        }

        public static bool ShouldPlayAudienceVoices(
            bool combatSpeechMode,
            bool configuredEnabled)
        {
            return configuredEnabled && !combatSpeechMode;
        }

        public static bool ShouldIssueTacticalAdvance(
            bool combatSpeechMode,
            bool configuredEnabled)
        {
            return configuredEnabled && !combatSpeechMode;
        }

        public static bool ShouldIssueTacticalAdvance(
            bool speechCompleted,
            bool combatSpeechMode,
            bool configuredEnabled)
        {
            return speechCompleted && ShouldIssueTacticalAdvance(
                combatSpeechMode,
                configuredEnabled);
        }

        public static bool TryResolveLocalActionProgram(
            string speechText,
            out ActionProgramV4 program,
            out bool needsClassifier)
        {
            program = null;
            needsClassifier = false;
            IReadOnlyList<string> actions =
                SceneActionFrameworkV4.ResolveNaturalActionDescription(speechText);
            if (actions.Count == 1)
            {
                program = ActionProgramV4.FromSingle(actions[0]);
                return true;
            }
            if (actions.Count > 1 ||
                SceneActionFrameworkV4.ContainsNaturalActionReference(speechText))
            {
                needsClassifier = true;
            }
            return false;
        }

        public static bool LooksLikeDirectPlayerSpeech(string rawText)
        {
            string text = Normalize(rawText);
            if (!LooksLikeDirectPlayerSpeechCandidate(text))
            {
                return false;
            }
            int rallyCueCount = DirectRallyCues.Count(cue => Contains(text, cue));
            return rallyCueCount >= 2;
        }

        public static bool LooksLikeDirectPlayerSpeechCandidate(string rawText)
        {
            string text = Normalize(rawText);
            if (text.Length < 8 ||
                text.Length > BattleSpeechFrameworkV1.MaximumSpeechChars ||
                text.IndexOfAny(new[] { '"', '\'', '“', '”', '‘', '’' }) >= 0 ||
                text.IndexOf(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0195"), StringComparison.Ordinal) >= 0 ||
                text.IndexOf(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0196"), StringComparison.Ordinal) >= 0 ||
                text.IndexOf(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0197"), StringComparison.Ordinal) >= 0 ||
                text.IndexOf(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0198"), StringComparison.Ordinal) >= 0)
            {
                return false;
            }
            return DirectTroopAddressCues.Any(cue => StartsWith(text, cue)) &&
                   DirectRallyCues.Any(cue => Contains(text, cue));
        }

        private static bool LooksLikeUnlabeledDirectPlayerSpeechCandidate(string rawText)
        {
            string text = Normalize(rawText);
            if (text.Length < 10 ||
                text.Length > BattleSpeechFrameworkV1.MaximumSpeechChars ||
                text.IndexOfAny(new[] { '"', '\'', '“', '”', '‘', '’' }) >= 0 ||
                text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0195"), StringComparison.Ordinal) ||
                text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0196"), StringComparison.Ordinal) ||
                text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0199"), StringComparison.Ordinal) ||
                text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0200"), StringComparison.Ordinal))
            {
                return false;
            }
            if (HypotheticalOrQuotedCues.Any(cue => Contains(text, cue)))
            {
                return false;
            }

            bool addressesAudience = DirectTroopAddressCues.Any(cue => StartsWith(text, cue));
            bool usesCollectiveVoice = CollectiveSpeechCues.Any(cue => Contains(text, cue));
            bool hasRhetoricalShape = text.IndexOfAny(
                new[] { '!', '！', '?', '？', '.', '。', ';', '；' }) >= 0;
            if (addressesAudience && text.Length >= 10)
            {
                return !text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0201"), StringComparison.Ordinal) &&
                       !text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0075"), StringComparison.Ordinal) &&
                       !text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0082"), StringComparison.Ordinal) &&
                       !text.StartsWith(SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0083"), StringComparison.Ordinal);
            }
            return usesCollectiveVoice && text.Length >= 18 && hasRhetoricalShape;
        }

        public static float GetDeterministicPacingOffset(
            Guid sessionId,
            int waypointIndex,
            float halfWidthMeters)
        {
            if (sessionId == Guid.Empty || waypointIndex < 0 || halfWidthMeters <= 0f)
            {
                return 0f;
            }
            uint hash = StableHash(sessionId.ToString("N") + ":pace:" + waypointIndex);
            float unit = (hash % 10001u) / 10000f;
            float offset = ((unit * 2f) - 1f) * halfWidthMeters;
            if (waypointIndex > 0 && Math.Abs(offset) < halfWidthMeters * 0.25f)
            {
                offset = (waypointIndex % 2 == 0 ? 1f : -1f) * halfWidthMeters * 0.5f;
            }
            return offset;
        }

        public static float GetDeterministicPacingInterval(
            Guid sessionId,
            int waypointIndex,
            float minimumSeconds,
            float maximumSeconds)
        {
            if (maximumSeconds <= minimumSeconds)
            {
                return minimumSeconds;
            }
            uint hash = StableHash(sessionId.ToString("N") + ":pace-interval:" + waypointIndex);
            return minimumSeconds + ((hash % 10001u) / 10000f) *
                   (maximumSeconds - minimumSeconds);
        }

        private static BattleSpeechTriggerKindV2 MapLegacy(BattleSpeechCommandKindV1 kind)
        {
            switch (kind)
            {
                case BattleSpeechCommandKindV1.ArmPlayerSpeech:
                    return BattleSpeechTriggerKindV2.ArmPlayerSpeech;
                case BattleSpeechCommandKindV1.DeliverPlayerSpeech:
                    return BattleSpeechTriggerKindV2.DeliverPlayerSpeech;
                case BattleSpeechCommandKindV1.RequestNpcSpeech:
                    // V1 keeps this enum for binary/API compatibility, but V2
                    // never lets a T-key message create an NPC speech session.
                    return BattleSpeechTriggerKindV2.None;
                case BattleSpeechCommandKindV1.Cancel:
                    return BattleSpeechTriggerKindV2.Cancel;
                default:
                    return BattleSpeechTriggerKindV2.None;
            }
        }

        private static readonly Regex EnglishInlineSpeechPattern = new Regex(
            @"^(?:(?:i\s+will|let\s+me|i\s+want\s+to|i'll)\s+)?(?:(?:give|deliver|make)\s+)?(?:a\s+)?(?:battle\s+)?(?:speech|rally|address)(?=$|[：:\s])(?:\s+to\s+(?:the\s+)?(?:troops|soldiers|men|all)(?=$|[：:\s]))?[：:\s]*(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static bool TryReadInlinePlayerSpeech(string text, out string speech)
        {
            speech = null;
            Match match = Regex.Match(
                text,
                SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0202"),
                RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                match = EnglishInlineSpeechPattern.Match(text);
                if (!match.Success)
                {
                    return false;
                }
            }
            speech = match.Groups[1].Value.Trim();
            return true;
        }

        private static BattleSpeechTriggerDecisionV2 BuildForcedDecision(
            BattleSpeechTriggerKindV2 kind,
            string speech)
        {
            speech = (speech ?? string.Empty).Trim();
            if (speech.Length == 0)
            {
                return new BattleSpeechTriggerDecisionV2(
                    kind,
                    speechText: null,
                    reason: "Forced player speech text is empty.",
                    force: true);
            }
            if (speech.Length > BattleSpeechFrameworkV1.MaximumSpeechChars)
            {
                return new BattleSpeechTriggerDecisionV2(
                    kind,
                    speechText: speech,
                    reason: "Forced battle speech exceeds the framework limit.",
                    force: true);
            }
            return new BattleSpeechTriggerDecisionV2(
                kind,
                speechText: speech,
                reason: null,
                force: true);
        }

        private static bool TryReadForcedSpeech(
            string text,
            IEnumerable<string> prefixes,
            out string speech)
        {
            speech = null;
            foreach (string prefix in prefixes)
            {
                if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                int delimiterIndex = prefix.Length;
                while (delimiterIndex < text.Length &&
                       char.IsWhiteSpace(text[delimiterIndex]))
                {
                    delimiterIndex++;
                }
                if (delimiterIndex >= text.Length ||
                    (text[delimiterIndex] != ':' && text[delimiterIndex] != '：'))
                {
                    continue;
                }
                delimiterIndex++;
                while (delimiterIndex < text.Length &&
                       char.IsWhiteSpace(text[delimiterIndex]))
                {
                    delimiterIndex++;
                }
                speech = delimiterIndex >= text.Length
                    ? string.Empty
                    : text.Substring(delimiterIndex).Trim();
                return true;
            }
            return false;
        }

        private static bool LooksLikeLooseSpeechRequest(string text)
        {
            return AudienceCues.Any(cue => Contains(text, cue)) &&
                   SpeechRequestCues.Any(cue => Contains(text, cue)) &&
                   (Contains(text, SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0203")) || Contains(text, SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0204")) || Contains(text, SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0205")));
        }

        private static bool LooksLikeActorRerouteSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
            return Regex.IsMatch(
                       text,
                       SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0206"),
                       RegexOptions.CultureInvariant) ||
                   Regex.IsMatch(
                       text,
                       SceneActionVocabulary.Text("BattleSpeechFrameworkV2.Raw0207"),
                       RegexOptions.CultureInvariant);
        }

        private static string Normalize(string text)
        {
            string value = CommandParser.Normalize(text);
            if (value.StartsWith("*", StringComparison.Ordinal))
            {
                value = value.Substring(1).Trim();
            }
            if (value.EndsWith("*", StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - 1).Trim();
            }
            return value;
        }

        private static bool Contains(string text, string cue)
        {
            return text.IndexOf(cue, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool StartsWith(string text, string cue)
        {
            return text.StartsWith(cue, StringComparison.OrdinalIgnoreCase);
        }

        private static BattleSpeechTriggerDecisionV2 None()
        {
            return new BattleSpeechTriggerDecisionV2(BattleSpeechTriggerKindV2.None);
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return hash;
            }
        }
    }
}
