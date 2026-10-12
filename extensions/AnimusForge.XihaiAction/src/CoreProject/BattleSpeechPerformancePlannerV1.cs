using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge.SceneActions.Core
{
    public sealed class BattleSpeechPerformanceSettingsV1
    {
        public bool Enabled { get; set; } = true;
        public bool SpeakerGesturesEnabled { get; set; } = true;
        public int MaxSpeakerGestures { get; set; } = 4;
        public float MinimumSpeakerGestureSpacingSeconds { get; set; } = 1.4f;
        public bool AudienceReactionsEnabled { get; set; } = true;
        public float AudienceParticipationRatio { get; set; } = 0.35f;
        public int MaximumAudiencePerformers { get; set; } = 96;
        public int AudienceWaveSize { get; set; } = 8;
        public float AudienceWaveIntervalSeconds { get; set; } = 0.3f;
        public float AudienceMemberStaggerSeconds { get; set; } = 0.035f;
        public float AudienceFinalDelaySeconds { get; set; } = 0.25f;
        public float PerformanceTailSeconds { get; set; } = 3.5f;

        public IReadOnlyList<string> Validate()
        {
            List<string> errors = new List<string>();
            if (MaxSpeakerGestures < 1 || MaxSpeakerGestures > 4 ||
                MinimumSpeakerGestureSpacingSeconds < 0.75f ||
                MinimumSpeakerGestureSpacingSeconds > 6f)
            {
                errors.Add("Speaker performance settings are invalid.");
            }
            if (AudienceParticipationRatio < 0f || AudienceParticipationRatio > 1f ||
                MaximumAudiencePerformers < 1 || MaximumAudiencePerformers > 256 ||
                AudienceWaveSize < 1 || AudienceWaveSize > 32 ||
                AudienceWaveIntervalSeconds < 0.05f || AudienceWaveIntervalSeconds > 2f ||
                AudienceMemberStaggerSeconds < 0.005f || AudienceMemberStaggerSeconds > 0.25f ||
                AudienceFinalDelaySeconds < 0f || AudienceFinalDelaySeconds > 5f ||
                PerformanceTailSeconds < 1f || PerformanceTailSeconds > 10f)
            {
                errors.Add("Audience performance settings are invalid.");
            }
            return errors;
        }
    }

    public sealed class BattleSpeechPerformanceCueV1
    {
        public BattleSpeechPerformanceCueV1(
            string intentKey,
            float offsetSeconds,
            int audienceOrdinal = -1)
        {
            IntentKey = intentKey ?? throw new ArgumentNullException(nameof(intentKey));
            OffsetSeconds = offsetSeconds;
            AudienceOrdinal = audienceOrdinal;
        }

        public string IntentKey { get; }
        public float OffsetSeconds { get; }
        public int AudienceOrdinal { get; }
        public bool IsAudienceCue => AudienceOrdinal >= 0;
    }

    public sealed class BattleSpeechPerformancePlanV1
    {
        internal BattleSpeechPerformancePlanV1(
            IEnumerable<BattleSpeechPerformanceCueV1> speakerCues,
            IEnumerable<BattleSpeechPerformanceCueV1> audienceCues,
            float tailEndOffsetSeconds)
        {
            SpeakerCues = new ReadOnlyCollection<BattleSpeechPerformanceCueV1>(
                (speakerCues ?? Array.Empty<BattleSpeechPerformanceCueV1>()).ToArray());
            AudienceCues = new ReadOnlyCollection<BattleSpeechPerformanceCueV1>(
                (audienceCues ?? Array.Empty<BattleSpeechPerformanceCueV1>()).ToArray());
            TailEndOffsetSeconds = tailEndOffsetSeconds;
        }

        public IReadOnlyList<BattleSpeechPerformanceCueV1> SpeakerCues { get; }
        public IReadOnlyList<BattleSpeechPerformanceCueV1> AudienceCues { get; }
        public float TailEndOffsetSeconds { get; }
    }

    public static class BattleSpeechPerformancePlannerV1
    {
        private static readonly string[] SpeakerIntentKeys =
        {
            SceneActionFrameworkV4.Explain,
            SceneActionFrameworkV4.Point,
            SceneActionFrameworkV4.Command,
            SceneActionFrameworkV4.Promise,
            SceneActionFrameworkV4.Rage
        };

        private static readonly string[] TrustedOneShotIntentKeys =
            SceneActionFrameworkV4.LogicalActions
                .Where(entry => entry.PlaybackMode == ActionMode.OneShot ||
                                entry.PlaybackMode == ActionMode.RandomGroup)
                .Select(entry => entry.IntentKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        private static readonly IReadOnlyList<string> SpeakerIntentView =
            Array.AsReadOnly(SpeakerIntentKeys);
        private static readonly IReadOnlyList<string> TrustedOneShotIntentView =
            Array.AsReadOnly(TrustedOneShotIntentKeys);

        private static readonly SemanticRule[] Rules =
        {
            new SemanticRule(
                SceneActionFrameworkV4.Promise,
                50,
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0001"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0002"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0003"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0004"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0005"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0006"),
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0007"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0008"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0009"), "i swear", "i promise", "i vow", "my word"),
            new SemanticRule(
                SceneActionFrameworkV4.Rage,
                45,
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0010"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0011"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0012"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0013"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0014"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0015"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0016"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0017"),
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0018"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0019"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0020"), "vengeance", "make them pay", "crush them",
                "no mercy", "never surrender"),
            new SemanticRule(
                SceneActionFrameworkV4.Command,
                40,
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0021"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0022"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0023"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0024"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0025"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0026"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0027"),
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0028"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0029"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0030"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0031"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0032"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0033"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0034"),
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0035"), "hold the line", "stand firm", "advance", "charge", "follow me",
                "raise your weapons"),
            new SemanticRule(
                SceneActionFrameworkV4.Point,
                35,
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0036"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0037"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0038"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0039"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0040"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0041"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0042"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0043"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0044"),
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0045"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0046"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0047"), "ahead", "over there", "behind us", "the walls",
                "that hill", "look there"),
            new SemanticRule(
                SceneActionFrameworkV4.Explain,
                20,
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0048"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0049"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0050"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0051"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0052"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0053"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0054"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0055"),
                SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0056"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0057"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0058"), SceneActionVocabulary.Text("BattleSpeechPerformancePlannerV1.LegacySource0059"), "because", "therefore", "remember",
                "you know", "the reason", "today's battle")
        };

        public static IReadOnlyList<string> SpeakerIntents => SpeakerIntentView;
        public static IReadOnlyList<string> TrustedOneShotIntents => TrustedOneShotIntentView;

        public static bool IsTrustedOneShotIntent(string intentKey)
        {
            return !string.IsNullOrWhiteSpace(intentKey) &&
                   Array.IndexOf(TrustedOneShotIntentKeys, intentKey) >= 0;
        }

        public static IReadOnlyList<int> SelectAudienceResponseOrdinals(
            Guid sessionId,
            IReadOnlyList<BattleSpeechPerformanceCueV1> audienceCues,
            int audienceCount,
            int maximumResponders)
        {
            if (sessionId == Guid.Empty || audienceCount <= 0 || maximumResponders <= 0)
            {
                return Array.Empty<int>();
            }

            int boundedCount = Math.Min(audienceCount, maximumResponders);
            List<int> result = new List<int>(boundedCount);
            HashSet<int> selected = new HashSet<int>();

            foreach (BattleSpeechPerformanceCueV1 cue in
                     (audienceCues ?? Array.Empty<BattleSpeechPerformanceCueV1>())
                     .OrderBy(value => value.OffsetSeconds)
                     .ThenBy(value => value.AudienceOrdinal))
            {
                if (result.Count >= boundedCount)
                {
                    break;
                }
                if (cue.AudienceOrdinal < 0 || cue.AudienceOrdinal >= audienceCount ||
                    !selected.Add(cue.AudienceOrdinal))
                {
                    continue;
                }
                result.Add(cue.AudienceOrdinal);
            }

            if (result.Count < boundedCount)
            {
                foreach (int ordinal in Enumerable.Range(0, audienceCount)
                             .Where(ordinal => !selected.Contains(ordinal))
                             .Select(ordinal => new
                             {
                                 Ordinal = ordinal,
                                 Hash = StableHash(
                                     sessionId.ToString("N") +
                                     ":audience-reply-fallback:" + ordinal)
                             })
                             .OrderBy(value => value.Hash)
                             .ThenBy(value => value.Ordinal)
                             .Take(boundedCount - result.Count)
                             .Select(value => value.Ordinal))
                {
                    result.Add(ordinal);
                }
            }

            return new ReadOnlyCollection<int>(result);
        }

        public static BattleSpeechPerformancePlanV1 Create(
            Guid sessionId,
            string speechText,
            float durationSeconds,
            int audienceCount,
            BattleSpeechPerformanceSettingsV1 settings)
        {
            if (sessionId == Guid.Empty)
            {
                throw new ArgumentException("Session id must not be empty.", nameof(sessionId));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (settings.Validate().Count > 0)
            {
                throw new ArgumentException("Battle speech settings are invalid.", nameof(settings));
            }

            float duration = Math.Max(1f, durationSeconds);
            List<BattleSpeechPerformanceCueV1> speaker = settings.Enabled &&
                                                              settings.SpeakerGesturesEnabled
                ? BuildSpeakerCues(speechText, duration, settings)
                : new List<BattleSpeechPerformanceCueV1>();
            List<BattleSpeechPerformanceCueV1> audience = settings.Enabled &&
                                                               settings.AudienceReactionsEnabled
                ? BuildAudienceCues(sessionId, duration, Math.Max(0, audienceCount), settings)
                : new List<BattleSpeechPerformanceCueV1>();
            float lastCue = audience.Count == 0
                ? duration
                : Math.Max(duration, audience.Max(cue => cue.OffsetSeconds));
            return new BattleSpeechPerformancePlanV1(
                speaker,
                audience,
                lastCue + settings.PerformanceTailSeconds);
        }

        public static BattleSpeechPerformancePlanV1 CreateFromProgram(
            Guid sessionId,
            ActionProgramV4 actionProgram,
            float durationSeconds,
            int audienceCount,
            BattleSpeechPerformanceSettingsV1 settings)
        {
            if (sessionId == Guid.Empty)
            {
                throw new ArgumentException("Session id must not be empty.", nameof(sessionId));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (settings.Validate().Count > 0)
            {
                throw new ArgumentException("Battle speech settings are invalid.", nameof(settings));
            }

            float duration = Math.Max(1f, durationSeconds);
            List<BattleSpeechPerformanceCueV1> speaker = settings.Enabled &&
                                                              settings.SpeakerGesturesEnabled
                ? BuildProgramSpeakerCues(actionProgram, duration, settings)
                : new List<BattleSpeechPerformanceCueV1>();
            List<BattleSpeechPerformanceCueV1> audience = settings.Enabled &&
                                                               settings.AudienceReactionsEnabled
                ? BuildAudienceCues(sessionId, duration, Math.Max(0, audienceCount), settings)
                : new List<BattleSpeechPerformanceCueV1>();
            float lastCue = audience.Count == 0
                ? duration
                : Math.Max(duration, audience.Max(cue => cue.OffsetSeconds));
            return new BattleSpeechPerformancePlanV1(
                speaker,
                audience,
                lastCue + settings.PerformanceTailSeconds);
        }

        public static BattleSpeechPerformancePlanV1 CreateFromProgramOrSpeech(
            Guid sessionId,
            ActionProgramV4 actionProgram,
            string speechText,
            float durationSeconds,
            int audienceCount,
            BattleSpeechPerformanceSettingsV1 settings)
        {
            bool hasTrustedProgramAction = (actionProgram?.Steps ??
                                            Array.Empty<ActionProgramStepV4>())
                .SelectMany(step => step.IntentKeys)
                .Any(IsTrustedOneShotIntent);
            return hasTrustedProgramAction
                ? CreateFromProgram(
                    sessionId,
                    actionProgram,
                    durationSeconds,
                    audienceCount,
                    settings)
                : Create(
                    sessionId,
                    speechText,
                    durationSeconds,
                    audienceCount,
                    settings);
        }

        private static List<BattleSpeechPerformanceCueV1> BuildProgramSpeakerCues(
            ActionProgramV4 actionProgram,
            float duration,
            BattleSpeechPerformanceSettingsV1 settings)
        {
            string[] intentKeys = (actionProgram?.Steps ??
                                   Array.Empty<ActionProgramStepV4>())
                .SelectMany(step => step.IntentKeys)
                .Where(IsTrustedOneShotIntent)
                .Take(settings.MaxSpeakerGestures)
                .ToArray();
            if (intentKeys.Length == 0)
            {
                return new List<BattleSpeechPerformanceCueV1>();
            }

            int durationCapacity = 1 + (int)Math.Floor(
                Math.Max(0f, duration - 1.9f) /
                settings.MinimumSpeakerGestureSpacingSeconds);
            int count = Math.Min(intentKeys.Length, Math.Max(1, durationCapacity));
            float firstOffset = Math.Min(1f, Math.Max(0.65f, duration * 0.18f));
            float lastOffset = Math.Max(firstOffset, duration - 0.9f);
            List<BattleSpeechPerformanceCueV1> result =
                new List<BattleSpeechPerformanceCueV1>(count);
            for (int index = 0; index < count; index++)
            {
                float offset = count == 1
                    ? firstOffset
                    : firstOffset + ((lastOffset - firstOffset) * index / (count - 1));
                result.Add(new BattleSpeechPerformanceCueV1(intentKeys[index], offset));
            }
            return result;
        }

        private static List<BattleSpeechPerformanceCueV1> BuildSpeakerCues(
            string speechText,
            float duration,
            BattleSpeechPerformanceSettingsV1 settings)
        {
            string visibleSpeech = BattleSpeechReplyBindingV1.Fingerprint(speechText);
            string[] clauses = Regex.Split(
                    visibleSpeech,
                    "[，,。！？；;!?：:\\r\\n]+")
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .ToArray();
            List<SemanticCandidate> candidates = new List<SemanticCandidate>();
            for (int clauseIndex = 0; clauseIndex < clauses.Length; clauseIndex++)
            {
                SemanticCandidate candidate = ScoreClause(clauses[clauseIndex], clauseIndex);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }
            if (candidates.Count == 0)
            {
                candidates.Add(new SemanticCandidate(
                    SceneActionFrameworkV4.Explain,
                    1,
                    0));
            }

            int durationCapacity = 1 + (int)Math.Floor(
                Math.Max(0f, duration - 1.9f) /
                settings.MinimumSpeakerGestureSpacingSeconds);
            int limit = Math.Max(
                1,
                Math.Min(settings.MaxSpeakerGestures, durationCapacity));
            List<SemanticCandidate> selected = candidates
                .GroupBy(candidate => candidate.IntentKey, StringComparer.Ordinal)
                .Select(group => group
                    .OrderByDescending(candidate => candidate.Score)
                    .ThenBy(candidate => candidate.ClauseIndex)
                    .First())
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.ClauseIndex)
                .Take(limit)
                .OrderBy(candidate => candidate.ClauseIndex)
                .ToList();

            float firstOffset = Math.Min(1f, Math.Max(0.65f, duration * 0.18f));
            float lastOffset = Math.Max(firstOffset, duration - 0.9f);
            List<BattleSpeechPerformanceCueV1> result = new List<BattleSpeechPerformanceCueV1>();
            for (int index = 0; index < selected.Count; index++)
            {
                float offset = selected.Count == 1
                    ? firstOffset
                    : firstOffset + ((lastOffset - firstOffset) * index / (selected.Count - 1));
                result.Add(new BattleSpeechPerformanceCueV1(
                    selected[index].IntentKey,
                    offset));
            }
            return result;
        }

        private static SemanticCandidate ScoreClause(string clause, int clauseIndex)
        {
            SemanticCandidate best = null;
            foreach (SemanticRule rule in Rules)
            {
                int hits = rule.Cues.Count(cue =>
                    clause.IndexOf(cue, StringComparison.OrdinalIgnoreCase) >= 0);
                if (hits == 0)
                {
                    continue;
                }
                int score = rule.BaseScore + (hits * 10);
                if (best == null || score > best.Score)
                {
                    best = new SemanticCandidate(rule.IntentKey, score, clauseIndex);
                }
            }
            return best;
        }

        private static List<BattleSpeechPerformanceCueV1> BuildAudienceCues(
            Guid sessionId,
            float duration,
            int audienceCount,
            BattleSpeechPerformanceSettingsV1 settings)
        {
            if (audienceCount == 0 || settings.AudienceParticipationRatio <= 0f)
            {
                return new List<BattleSpeechPerformanceCueV1>();
            }
            int performerCount = Math.Min(
                settings.MaximumAudiencePerformers,
                Math.Max(1, (int)Math.Round(
                    audienceCount * settings.AudienceParticipationRatio,
                    MidpointRounding.AwayFromZero)));
            int[] ordinals = Enumerable.Range(0, audienceCount)
                .Select(ordinal => new
                {
                    Ordinal = ordinal,
                    Hash = StableHash(sessionId.ToString("N") + ":audience:" + ordinal)
                })
                .OrderBy(value => value.Hash)
                .ThenBy(value => value.Ordinal)
                .Take(performerCount)
                .Select(value => value.Ordinal)
                .ToArray();

            int midCount = duration >= 8f ? Math.Min(performerCount / 4, 12) : 0;
            List<BattleSpeechPerformanceCueV1> result = new List<BattleSpeechPerformanceCueV1>();
            for (int index = 0; index < ordinals.Length; index++)
            {
                if (index < midCount)
                {
                    float offset = Math.Max(1f, duration * 0.48f) +
                                   (index * settings.AudienceMemberStaggerSeconds);
                    result.Add(new BattleSpeechPerformanceCueV1(
                        SceneActionFrameworkV4.Cheer,
                        Math.Min(duration - 0.75f, offset),
                        ordinals[index]));
                    continue;
                }

                int finalIndex = index - midCount;
                int waveIndex = finalIndex / settings.AudienceWaveSize;
                int memberIndex = finalIndex % settings.AudienceWaveSize;
                float finalOffset = duration + settings.AudienceFinalDelaySeconds +
                                    (waveIndex * settings.AudienceWaveIntervalSeconds) +
                                    (memberIndex * settings.AudienceMemberStaggerSeconds);
                result.Add(new BattleSpeechPerformanceCueV1(
                    SceneActionFrameworkV4.Cheer,
                    finalOffset,
                    ordinals[index]));
            }
            return result.OrderBy(cue => cue.OffsetSeconds).ToList();
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

        private sealed class SemanticRule
        {
            public SemanticRule(string intentKey, int baseScore, params string[] cues)
            {
                IntentKey = intentKey;
                BaseScore = baseScore;
                Cues = cues ?? Array.Empty<string>();
            }

            public string IntentKey { get; }
            public int BaseScore { get; }
            public IReadOnlyList<string> Cues { get; }
        }

        private sealed class SemanticCandidate
        {
            public SemanticCandidate(string intentKey, int score, int clauseIndex)
            {
                IntentKey = intentKey;
                Score = score;
                ClauseIndex = clauseIndex;
            }

            public string IntentKey { get; }
            public int Score { get; }
            public int ClauseIndex { get; }
        }
    }
}
