using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public enum BattleSpeechCommandKindV1
    {
        None,
        ArmPlayerSpeech,
        DeliverPlayerSpeech,
        RequestNpcSpeech,
        Cancel
    }

    public enum BattleSpeechSpeakerKindV1
    {
        Player,
        Npc
    }

    public enum BattleSpeechPhaseV1
    {
        Deployment,
        PreEngagement
    }

    public enum BattleSpeechSessionStateV1
    {
        AwaitingPlayerSpeech,
        AwaitingNpcReply,
        Speaking,
        Completed,
        Cancelled
    }

    public sealed class BattleSpeechCommandDecisionV1
    {
        internal BattleSpeechCommandDecisionV1(
            BattleSpeechCommandKindV1 kind,
            string speechText,
            string error)
        {
            Kind = kind;
            SpeechText = speechText;
            Error = error;
        }

        public BattleSpeechCommandKindV1 Kind { get; }
        public string SpeechText { get; }
        public string Error { get; }
        public bool IsControl => Kind != BattleSpeechCommandKindV1.None;
        public bool IsValid => string.IsNullOrEmpty(Error);
    }

    public sealed class BattleSpeechSessionSnapshotV1
    {
        public BattleSpeechSessionSnapshotV1(
            Guid sessionId,
            BattleSpeechSessionStateV1 state,
            BattleSpeechSpeakerKindV1 speakerKind,
            BattleSpeechPhaseV1 phase,
            int speakerAgentIndex,
            string speakerName,
            IReadOnlyList<int> audienceAgentIndices,
            string speechText,
            double startedAtMissionTime,
            double endsAtMissionTime)
        {
            if (sessionId == Guid.Empty)
            {
                throw new ArgumentException("Session id must not be empty.", nameof(sessionId));
            }
            SessionId = sessionId;
            State = state;
            SpeakerKind = speakerKind;
            Phase = phase;
            SpeakerAgentIndex = speakerAgentIndex;
            SpeakerName = speakerName ?? string.Empty;
            AudienceAgentIndices = new ReadOnlyCollection<int>(
                (audienceAgentIndices ?? Array.Empty<int>()).Distinct().ToArray());
            SpeechText = speechText ?? string.Empty;
            StartedAtMissionTime = startedAtMissionTime;
            EndsAtMissionTime = endsAtMissionTime;
        }

        public Guid SessionId { get; }
        public BattleSpeechSessionStateV1 State { get; }
        public BattleSpeechSpeakerKindV1 SpeakerKind { get; }
        public BattleSpeechPhaseV1 Phase { get; }
        public int SpeakerAgentIndex { get; }
        public string SpeakerName { get; }
        public IReadOnlyList<int> AudienceAgentIndices { get; }
        public int AudienceCount => AudienceAgentIndices.Count;
        public string SpeechText { get; }
        public double StartedAtMissionTime { get; }
        public double EndsAtMissionTime { get; }
    }

    public sealed class BattleSpeechSettingsV1
    {
        public bool Enabled { get; set; } = true;
        // Keeps the dedicated Y-menu channel available while allowing users to
        // disable only natural/explicit battle-speech recognition from T.
        public bool TKeyEnabled { get; set; } = true;
        public bool AllowDeployment { get; set; } = true;
        public bool AllowPreEngagement { get; set; } = true;
        public float PlayerCaptureSeconds { get; set; } = 60f;
        public float NpcReplySeconds { get; set; } = 60f;
        public int MaxSpeechChars { get; set; } = BattleSpeechFrameworkV1.MaximumSpeechChars;
        public float AudienceRadiusMeters { get; set; } = 80f;
        public float EnemyInterruptRadiusMeters { get; set; } = 35f;
        public float EnemyScanIntervalSeconds { get; set; } = 0.25f;
        public int MinimumAudience { get; set; } = 1;
        public int MaximumAudience { get; set; } = 512;
        public float MinimumDurationSeconds { get; set; } = 6f;
        public float MaximumDurationSeconds { get; set; } = 45f;
        public float CharactersPerSecond { get; set; } = 8f;
        public bool ScreenNotifications { get; set; } = true;

        public IReadOnlyList<string> Validate()
        {
            List<string> errors = new List<string>();
            if (!AllowDeployment && !AllowPreEngagement)
            {
                errors.Add("At least one battle-speech phase must be enabled.");
            }
            if (PlayerCaptureSeconds < 5f || PlayerCaptureSeconds > 180f ||
                NpcReplySeconds < 5f || NpcReplySeconds > 180f)
            {
                errors.Add("Capture and NPC reply timeouts must be within 5..180 seconds.");
            }
            if (MaxSpeechChars < 32 || MaxSpeechChars > BattleSpeechFrameworkV1.MaximumSpeechChars)
            {
                errors.Add("MaxSpeechChars must be within 32..4096.");
            }
            if (AudienceRadiusMeters < 10f || AudienceRadiusMeters > 300f ||
                EnemyInterruptRadiusMeters < 5f ||
                EnemyInterruptRadiusMeters >= AudienceRadiusMeters)
            {
                errors.Add("Audience/enemy radii are invalid.");
            }
            if (EnemyScanIntervalSeconds < 0.05f || EnemyScanIntervalSeconds > 2f)
            {
                errors.Add("EnemyScanIntervalSeconds must be within 0.05..2 seconds.");
            }
            if (MinimumAudience < 1 || MaximumAudience < MinimumAudience || MaximumAudience > 1024)
            {
                errors.Add("Audience limits are invalid.");
            }
            if (MinimumDurationSeconds < 1f ||
                MaximumDurationSeconds < MinimumDurationSeconds ||
                MaximumDurationSeconds > 120f ||
                CharactersPerSecond < 1f || CharactersPerSecond > 30f)
            {
                errors.Add("Speech duration settings are invalid.");
            }
            return errors;
        }
    }

    public static class BattleSpeechFrameworkV1
    {
        public const int ContractVersion = 1;
        public const int MaximumSpeechChars = 4096;

        private static readonly string[] CancelCommands =
        {
            "取消阵前演讲", "结束阵前演讲", "停止阵前演讲",
            "取消战前演讲", "结束战前演讲",
            "cancel battle speech", "stop battle speech"
        };

        private static readonly string[] ArmPlayerCommands =
        {
            "开始阵前演讲", "我开始阵前演讲", "开始战前演讲",
            "我开始战前演讲", "我来向士兵们演讲", "我要向士兵们演讲",
            "start battle speech", "begin battle speech"
        };

        private static readonly string[] NpcCommands =
        {
            "让你阵前演讲", "请你阵前演讲", "你来阵前演讲",
            "让你在阵前演讲", "请你在阵前演讲", "你来向士兵们演讲",
            "让他阵前演讲", "请他阵前演讲", "让她阵前演讲",
            "请她阵前演讲", "让当前目标阵前演讲", "请当前目标阵前演讲",
            "让这位士兵阵前演讲", "请这位士兵阵前演讲",
            "让你战前动员", "请你战前动员", "让你阵前训话",
            "请你阵前训话", "ask you to give a battle speech",
            "give the troops a battle speech"
        };

        private static readonly string[] InlinePlayerPrefixes =
        {
            "我阵前演讲", "我在阵前演讲", "我来阵前演讲",
            "我向士兵们演讲", "我的阵前演讲", "阵前演讲",
            "我战前演讲", "battle speech"
        };

        public static BattleSpeechCommandDecisionV1 ParsePlayerShout(string rawText)
        {
            string text = NormalizeControlText(rawText);
            if (text.Length == 0)
            {
                return None();
            }

            if (CancelCommands.Contains(text, StringComparer.Ordinal))
            {
                return new BattleSpeechCommandDecisionV1(
                    BattleSpeechCommandKindV1.Cancel,
                    null,
                    null);
            }
            if (ArmPlayerCommands.Contains(text, StringComparer.Ordinal))
            {
                return new BattleSpeechCommandDecisionV1(
                    BattleSpeechCommandKindV1.ArmPlayerSpeech,
                    null,
                    null);
            }
            if (NpcCommands.Contains(text, StringComparer.Ordinal))
            {
                return new BattleSpeechCommandDecisionV1(
                    BattleSpeechCommandKindV1.RequestNpcSpeech,
                    null,
                    null);
            }

            foreach (string prefix in InlinePlayerPrefixes)
            {
                if (!TryReadInlineSpeech(text, prefix, out string speech))
                {
                    continue;
                }
                if (speech.Length == 0)
                {
                    return new BattleSpeechCommandDecisionV1(
                        BattleSpeechCommandKindV1.DeliverPlayerSpeech,
                        null,
                        "Inline battle speech is empty.");
                }
                if (speech.Length > MaximumSpeechChars)
                {
                    return new BattleSpeechCommandDecisionV1(
                        BattleSpeechCommandKindV1.DeliverPlayerSpeech,
                        null,
                        "Inline battle speech exceeds the framework limit.");
                }
                return new BattleSpeechCommandDecisionV1(
                    BattleSpeechCommandKindV1.DeliverPlayerSpeech,
                    speech,
                    null);
            }

            return None();
        }

        public static float EstimateDurationSeconds(
            string speechText,
            BattleSpeechSettingsV1 settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            int length = (speechText ?? string.Empty).Trim().Length;
            float estimated = length / settings.CharactersPerSecond;
            return Math.Max(
                settings.MinimumDurationSeconds,
                Math.Min(settings.MaximumDurationSeconds, estimated));
        }

        private static BattleSpeechCommandDecisionV1 None()
        {
            return new BattleSpeechCommandDecisionV1(
                BattleSpeechCommandKindV1.None,
                null,
                null);
        }

        private static bool TryReadInlineSpeech(
            string text,
            string prefix,
            out string speech)
        {
            speech = null;
            if (!text.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }
            if (text.Length == prefix.Length)
            {
                speech = string.Empty;
                return true;
            }
            char boundary = text[prefix.Length];
            if (boundary != ':' && boundary != '：' && !char.IsWhiteSpace(boundary))
            {
                return false;
            }
            int start = prefix.Length;
            while (start < text.Length &&
                   (text[start] == ':' || text[start] == '：' || char.IsWhiteSpace(text[start])))
            {
                start++;
            }
            speech = start >= text.Length ? string.Empty : text.Substring(start).Trim();
            return true;
        }

        private static string NormalizeControlText(string rawText)
        {
            string text = CommandParser.Normalize(rawText);
            if (text.Length == 0)
            {
                return string.Empty;
            }
            if (text[0] == '*')
            {
                text = text.Substring(1).Trim();
            }
            if (text.Length > 0 && text[text.Length - 1] == '*')
            {
                text = text.Substring(0, text.Length - 1).Trim();
            }
            return text;
        }
    }
}
