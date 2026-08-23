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
        public int ReplyMinimumChars { get; set; } = 30;
        public int ReplyMaximumChars { get; set; } = 80;
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
        public int AudienceReplyMinimumChars { get; set; } = 8;
        public int AudienceReplyMaximumChars { get; set; } = 24;
        public float AudienceReplyMinimumIntervalSeconds { get; set; } = 0.2f;
        public float AudienceReplyMaximumIntervalSeconds { get; set; } = 0.5f;
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
            if (ReplyMinimumChars < 6 || ReplyMaximumChars > 80 ||
                ReplyMaximumChars < ReplyMinimumChars)
            {
                errors.Add("Battle speech reply length must stay within 6..80 characters.");
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
                AudienceReplyMinimumChars < 4 ||
                AudienceReplyMaximumChars > 80 ||
                AudienceReplyMaximumChars < AudienceReplyMinimumChars ||
                AudienceReplyMinimumIntervalSeconds < 0.1f ||
                AudienceReplyMaximumIntervalSeconds > 0.5f ||
                AudienceReplyMaximumIntervalSeconds < AudienceReplyMinimumIntervalSeconds ||
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
            "强制指令我来演讲",
            "强制指令我演讲",
            "强制指令演讲",
            "强制演讲",
            "强制我演讲",
            "强制我来演讲",
            "我强制演讲",
            "演讲",
            "我演讲",
            "我来演讲"
        };

        private static readonly string[] ForcedNpcSpeechPrefixes =
        {
            "强制指令框选目标演讲",
            "强制指令目标演讲",
            "强制指令你来演讲",
            "强制指令你演讲",
            "强制指令他来演讲",
            "强制指令他演讲",
            "强制指令她来演讲",
            "强制指令她演讲",
            "强制你来演讲",
            "强制你演讲",
            "强制他来演讲",
            "强制他演讲",
            "强制她来演讲",
            "强制她演讲",
            "强制目标演讲",
            "强制框选目标演讲",
            "你来演讲",
            "他来演讲",
            "她来演讲",
            "你演讲",
            "他演讲",
            "她演讲",
            "目标演讲",
            "框选目标演讲"
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

        public static string BuildNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars)
        {
            return BuildNpcSpeechPromptInstruction(
                minimumChars,
                maximumChars,
                null,
                0);
        }

        public static string BuildNpcSpeechPromptInstruction(
            int minimumChars,
            int maximumChars,
            IEnumerable<string> recentSpeechTexts,
            int generationAttempt)
        {
            if (minimumChars < 6 || maximumChars > 80 || maximumChars < minimumChars)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumChars),
                    "Battle speech prompt length must stay within 6..80 characters.");
            }
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? "这是第" + (generationAttempt + 1) + "次生成同一场演讲，上一版与历史正文完全重复，必须彻底改换开场、情绪推进、句式和结尾号召。"
                : string.Empty;
            return "【阵前演讲正文生成任务，优先于上面的常规回复格式】沿用当前场景喊话已经提供的" +
                   "角色身份、文化、战场局势和历史，以该角色自己的口吻，面向己方全体士兵说一段" +
                   "真正的战前动员。你不是在回答或表演给玩家看。只输出实际说出的演讲正文，不得" +
                   "输出星号、动作描写、旁白、内心想法、标签或格式说明。可以自然称呼士兵，也可以" +
                   "直接开口；不得强制套用固定称呼。若玩家消息使用‘你演讲：’、‘他演讲：’、" +
                   "‘她演讲：’或‘目标演讲：’，冒号后的内容只是主题或风格要求，不得逐字复述。" +
                   "若没有提供具体主题，就根据当前战场和该角色背景自行确定最合适的动员重点。" +
                   "必须优先使用上下文中真实提供的具体战场细节，例如敌军所在方向或位置、地形、天气、" +
                   "己方阵线和眼前正在发生的情况；上下文没有提供的细节不得臆造。必须体现该NPC自己的" +
                   "性格、身份和当前情绪，并先通过观察、判断或回忆推动情绪，再逐步走向号召，不能一开头" +
                   "就堆口号。每次选择一种主风格：愤怒、悲壮、冷静、嘲讽、坚定或鼓舞；不要混成旁白。" +
                   "不要固定套用‘弟兄们、家园、战旗、号角、胜利’等高频词，除非当前语境确实需要。" +
                   "最后给出符合该NPC身份和说话方式的战斗号召，不要统一写成‘全军前进’。" +
                   "避免空洞口号堆砌，不得为了凑字重复同一句、同一短语，不得连续罗列三条以上同类命令。" +
                   "不要回答玩家，不要向玩家反问、请示或索要差使，" +
                   "不要称呼玩家为大人、阁下、领主或您，也不要自称嘴拙、无权或拒绝演讲。正文" +
                   "长度必须为" + minimumChars + "至" + maximumChars + "个可见字符。" +
                   diversity + attempt;
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
                0);
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
            if (minimumChars < 6 || maximumChars > 80 || maximumChars < minimumChars)
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
            string replyRule = boundedReplyCount == 0
                ? "REPLIES NONE"
                : "REPLIES 后面必须给出恰好" + boundedReplyCount +
                  "条不同的士兵即时回应，用竖线分隔；每条" +
                  audienceReplyMinimumChars + "至" + audienceReplyMaximumChars +
                  "字，不写动作、旁白、姓名、星号或标签。";
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? "这是第" + (generationAttempt + 1) + "次生成同一场演讲，上一版与历史正文完全重复；必须彻底改变开场、情绪推进、句式、具体切入点和结尾号召。"
                : string.Empty;
            return "【阵前演讲单请求协议】这是一次性生成任务。你要同时生成正文、受控动作、战术字段和" +
                   "士兵回应，不要再生成普通NPC回复，也不要输出解释。正文必须面向己方士兵而不是玩家，" +
                   "沿用当前上下文真实的角色身份、性格、情绪和战场细节；先观察或判断，再推进情绪，最后" +
                   "给出符合该NPC口吻的号召。正文和REPLIES必须使用简体中文，不得输出英文正文、英文回应或" +
                   "英文格式说明；角色背景即使以英文提供，也要转换为自然中文。不要臆造上下文没有的地形、天气或敌军位置，不要把主题原句" +
                   "机械复述，不要套用统一口号。正文长度必须为" + minimumChars + "至" + maximumChars +
                   "个可见字符，且必须是一行实际说出的内容。输出格式必须严格为六行（回复数为0时仍保留" +
                   "第五行）：\nSPEECH_BEGIN\n<正文单行>\nSPEECH_END\nACTIONS NONE 或 ACTIONS PLAY_ACTION <key> 或 ACTIONS PLAY_PROGRAM <program>\nTACTIC NONE\n" +
                   replyRule + "\n动作key只能从以下冻结白名单选择：" + keys +
                   "。动作最多4个，>表示先后，+表示同时；不得输出act_*、目标、演员、强制标志或其他战术。" +
                   "TACTIC只能输出NONE或ADVANCE；只有正文明确提出立即向前推进、冲锋或开战号召时才输出ADVANCE，" +
                   "否则输出NONE；本地MCM仍是总开关。士兵回应必须像刚听完演讲的不同" +
                   "现场士兵：老兵沉着、新兵紧张但振作、粗犷者短促、谨慎者可带迟疑、狂热者可激昂；必须回应" +
                   "正文中的具体内容，避免所有人同声同句。不要反复使用‘为了胜利’‘为了家园’‘听候您的号令’" +
                   "‘全军向前’‘我们必胜’‘绝不后退’，不要称呼玩家为您、大人或领主。" +
                   diversity + attempt;
        }

        public static string NormalizeNpcSpeechReply(
            string rawText,
            int minimumChars,
            int maximumChars)
        {
            if (minimumChars < 6 || maximumChars > 80 || maximumChars < minimumChars)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumChars),
                    "Battle speech reply length must stay within 6..80 characters.");
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

            string candidate = TakeCompleteSpeech(text, minimumChars, maximumChars);
            if (IsValidTroopAddress(candidate, minimumChars, maximumChars) &&
                ContainsChinese(candidate))
            {
                return candidate;
            }

            string[] fallbacks =
            {
                "看清前方的敌人，也记住身后等待我们归来的人。稳住阵线，守好彼此的侧翼，号角一响便随我向前。今天不靠空话取胜，要靠每个人的勇气、纪律和手中的兵刃，把胜利带回家吧！",
                "敌人就在前方，但我们并非孤军奋战。守住同伴的侧翼，听清号角，随战旗一同向前，把胜利带回家！",
                "弟兄们，守住彼此的侧翼，跟紧战旗，一同迎敌！",
                "稳住阵线，准备迎敌！",
                "全军听我号令！"
            };
            string fallback = fallbacks.FirstOrDefault(value =>
                value.Length >= minimumChars && value.Length <= maximumChars);
            return fallback ?? BuildFallbackForLength(minimumChars, maximumChars);
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
            int minimumChars,
            int maximumChars)
        {
            const string coherentFallback =
                "看清前方的敌人，也记住身后等待我们归来的人。稳住阵线，守好彼此的侧翼，号角一响便随我向前。今天不靠空话取胜，要靠每个人的勇气、纪律和手中的兵刃，把胜利带回家吧！";
            string bounded = TakeCompleteSpeech(
                coherentFallback,
                minimumChars,
                maximumChars);
            if (bounded.Length >= minimumChars)
            {
                return bounded;
            }
            string seed = coherentFallback.Substring(
                0,
                Math.Min(maximumChars, coherentFallback.Length));
            if (seed.Length == maximumChars && maximumChars > 0)
            {
                seed = seed.Substring(0, maximumChars - 1) + "！";
            }
            return seed;
        }

        private static string TakeCompleteSpeech(
            string text,
            int minimumChars,
            int maximumChars)
        {
            if (text.Length <= maximumChars)
            {
                return text;
            }
            MatchCollection sentences = Regex.Matches(text, @"[^。！？!?；;]+[。！？!?；;]");
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
                new[] { '。', '！', '？', '!', '?', '；', ';', '，', ',' });
            if (lastBoundary + 1 >= minimumChars)
            {
                return prefix.Substring(0, lastBoundary + 1).Trim();
            }
            if (prefix.Length == maximumChars && maximumChars > 0 &&
                Array.IndexOf(new[] { '。', '！', '？', '!', '?', '；', ';' },
                    prefix[prefix.Length - 1]) < 0)
            {
                prefix = prefix.Substring(0, prefix.Length - 1) + "。";
            }
            return prefix;
        }

        private static bool IsValidTroopAddress(
            string text,
            int minimumChars,
            int maximumChars)
        {
            if (string.IsNullOrWhiteSpace(text) ||
                text.Length < minimumChars || text.Length > maximumChars)
            {
                return false;
            }
            string[] forbidden =
            {
                "大人", "阁下", "领主", "玩家", "您", "请示", "听凭您的调遣",
                "只要您", "下达指令", "我只是", "嘴拙", "讲不出", "无权", "不敢"
            };
            return !forbidden.Any(value => text.IndexOf(value, StringComparison.Ordinal) >= 0) &&
                   text.IndexOf('*') < 0 && text.IndexOf('[') < 0 && text.IndexOf(']') < 0;
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

        private static readonly string[] NegationCues =
        {
            "不要", "别让", "不许", "不用", "取消", "停止", "没让", "并未",
            "not ", "don't", "do not", "never"
        };

        private static readonly string[] HypotheticalOrQuotedCues =
        {
            "如果", "假如", "要是", "比如", "所谓", "这句话", "这几个字",
            "假设", "if ", "what if", "quote"
        };

        private static readonly string[] AudienceCues =
        {
            "大家", "众人", "士兵", "将士", "弟兄", "兄弟", "队伍", "全军",
            "部队", "军队", "战士", "troops", "soldiers", "everyone", "army"
        };

        private static readonly string[] SpeechCues =
        {
            "演讲", "训话", "动员", "鼓舞", "讲几句", "讲两句", "讲俩句",
            "说几句", "说两句", "说俩句", "喊几句", "speech", "address the troops",
            "rally the troops"
        };

        private static readonly string[] RequestCues =
        {
            "来给", "去给", "上前", "请你", "让你", "你来", "你去", "劳烦",
            "麻烦", "替我", "给大家", "跟大家", "向大家", "对大家", "给士兵",
            "跟士兵", "向士兵", "能不能", "能否", "可否", "give ", "go give",
            "please give", "address "
        };

        private static readonly string[] SelfCues =
        {
            "我来", "我要", "由我", "我给", "我向", "我跟", "让我来", "my speech",
            "i will", "let me"
        };

        private static readonly string[] DirectTroopAddressCues =
        {
            "弟兄们", "兄弟们", "将士们", "战士们", "士兵们", "全军",
            "各位将士", "各位", "诸位", "同袍们", "同胞们", "大家",
            "所有人", "伙计们", "troops", "soldiers", "brothers", "everyone"
        };

        private static readonly string[] CollectiveSpeechCues =
        {
            "我们", "咱们", "吾等", "共同", "一起", "跟我", "听我",
            "告诉你们", "记住", "let us", "let's", "we must", "we will"
        };

        private static readonly string[] DirectRallyCues =
        {
            "家园", "敌人", "该死", "前方", "冲锋", "战斗", "战场", "胜利",
            "后退", "不能退", "不后退", "阵线", "勇气", "荣耀", "守住", "杀敌",
            "迎敌", "随我", "为了", "绝不", "必将", "必须", "战争", "进攻",
            "前进", "自由", "复仇", "报仇", "听我", "听令", "号令", "掠夺",
            "袭击", "人民", "拼了", "上啊", "杀过去", "保卫", "家人", "家乡",
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
            // The colon is the explicit safety boundary; without it, existing V1/V2
            // commands keep their original semantics.
            if (TryReadForcedSpeech(
                    text,
                    ForcedPlayerSpeechPrefixes,
                    out string forcedPlayerSpeech))
            {
                return BuildForcedDecision(
                    BattleSpeechTriggerKindV2.DeliverPlayerSpeech,
                    forcedPlayerSpeech,
                    speakerIsNpc: false);
            }
            if (TryReadForcedSpeech(
                    text,
                    ForcedNpcSpeechPrefixes,
                    out string forcedNpcSpeech))
            {
                return BuildForcedDecision(
                    BattleSpeechTriggerKindV2.RequestNpcSpeech,
                    forcedNpcSpeech,
                    speakerIsNpc: true);
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
            bool hasAudience = AudienceCues.Any(cue => Contains(text, cue));
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
            if (hasAudience && RequestCues.Any(cue => Contains(text, cue)))
            {
                return new BattleSpeechTriggerDecisionV2(
                    BattleSpeechTriggerKindV2.RequestNpcSpeech);
            }
            return new BattleSpeechTriggerDecisionV2(
                BattleSpeechTriggerKindV2.NeedsClassifier,
                reason: "Battle-speech wording is ambiguous and requires closed classification.");
        }

        /// <summary>
        /// Parses text submitted from the dedicated Y-menu speech entry.
        /// The entry has already selected the battle-speech channel, so ordinary
        /// prose is a player speech body and must not be sent through the T-key
        /// scene-shout classifier. Explicit NPC/forced commands still retain
        /// their normal actor and topic semantics.
        /// </summary>
        public static BattleSpeechTriggerDecisionV2 ParseDedicatedSpeechInput(string rawText)
        {
            string text = Normalize(rawText);
            if (text.Length == 0)
            {
                return None();
            }

            BattleSpeechTriggerDecisionV2 explicitDecision = ParsePlayerShout(text);
            if (explicitDecision.Kind == BattleSpeechTriggerKindV2.RequestNpcSpeech ||
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
            if (string.Equals(value, "NPC_SPEECH", StringComparison.Ordinal))
            {
                kind = BattleSpeechTriggerKindV2.RequestNpcSpeech;
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
                    if (values.Length == 0 ||
                        values.Length > MaximumAudienceReplies)
                    {
                        error = "Battle speech audience replies exceed the closed limit.";
                        return false;
                    }
                    foreach (string value in values)
                    {
                        string reply = value.Trim();
                        if (reply.Length < audienceReplyMinimumChars ||
                            reply.Length > audienceReplyMaximumChars ||
                            !ContainsChinese(reply) ||
                            reply != value ||
                            reply.IndexOfAny(new[] { '\r', '\n', '*', '<', '>' }) >= 0 ||
                            replies.Contains(reply, StringComparer.Ordinal))
                        {
                            error = "Battle speech audience reply is invalid or duplicated.";
                            return false;
                        }
                        replies.Add(reply);
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
            string speech = NormalizeNpcSpeechReply(lines[1], minimumChars, maximumChars);
            if (!IsValidTroopAddress(speech, minimumChars, maximumChars) ||
                speech.IndexOf("SPEECH_", StringComparison.Ordinal) >= 0)
            {
                error = "Combined speech body is invalid.";
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
                    out error))
            {
                return false;
            }
            if (!ProgramUsesAllowedKeys(plan.ActionProgram, allowedIntentKeys))
            {
                error = "Combined speech action is outside the frozen allow-list.";
                return false;
            }
            int boundedExpected = Math.Max(0, Math.Min(MaximumAudienceReplies, expectedReplyCount));
            if (plan.AudienceReplies.Count != boundedExpected)
            {
                error = "Combined speech audience reply count does not match the frozen audience.";
                return false;
            }
            response = new BattleSpeechCombinedNpcResponseV2(speech, plan);
            return true;
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
            string speechBlock = normalized.Substring(begin, endExclusive - begin).Trim();
            string[] suffixLines = normalized
                .Substring(endExclusive)
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line =>
                    line.StartsWith("ACTIONS ", StringComparison.Ordinal) ||
                    line.StartsWith("TACTIC ", StringComparison.Ordinal) ||
                    line.StartsWith("REPLIES ", StringComparison.Ordinal))
                .Take(3)
                .ToArray();
            if (suffixLines.Length == 0)
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
            ValidateAudienceReplyLength(minimumChars, maximumChars);
            int boundedCount = Math.Max(0, Math.Min(MaximumAudienceReplies, count));
            if (boundedCount == 0)
            {
                return Array.Empty<string>();
            }
            bool chinese = (speechText ?? string.Empty).Any(character =>
                character >= '\u3400' && character <= '\u9fff');
            string[] pool = chinese
                ? new[]
                {
                    "北坡不能丢，盾牌靠紧！", "我有点怕，但我不会逃！", "弓弦已经拉满了！", "伤口还能撑住，跟上！",
                    "别散开，盯住前面的旗！", "他们敢过来就让他们撞上来！", "我守左侧，你们别松手！", "听见了，今天不退！",
                    "风里全是尘，先看清敌人的位置！", "老兵说得对，阵线要靠紧！", "我跟你守住这条线！", "盾牌举起来，别让他们钻缝！",
                    "要是他们冲来，我先顶住！", "我还站得住，继续往前！", "别喊空话了，跟紧队列！", "北面的弟兄别落单！",
                    "我听见了，准备迎上去！", "弓手先稳住，别急着放箭！", "今天这一步不能让！", "把侧翼看好，别被绕后！",
                    "我会跟上，不留在后面！", "他们的位置看得很清楚！", "让我先把盾墙撑起来！", "这次我们一起扛住！"
                }
                : new[]
                {
                    "The north slope holds; lock shields!", "I am afraid, but I will stand!", "Bows ready; wait for the mark!", "The wound can wait; keep up!",
                    "Do not scatter; watch the banner!", "Let them crash into our wall!", "I hold the left; stay close!", "I heard you. No step back!",
                    "Dust is hiding them; keep your eyes open!", "The old hand is right; close ranks!", "I will hold this line with you!", "Raise shields; leave no gap!",
                    "If they charge, I will meet them!", "I can still stand; move on!", "No empty cries; keep formation!", "Keep the north flank together!",
                    "I heard it; ready to meet them!", "Archers steady; do not loose early!", "We cannot yield this ground!", "Watch the flank for a turn!",
                    "I will follow; I will not lag!", "Their position is clear now!", "Let me brace the shield wall!", "We hold together this time!"
                };
            string[] suffixes = chinese
                ? new[] { "我会跟上。", "我来守住。", "这次不退。", "听见号令了。", "我盯住侧翼。" }
                : new[] { " I will keep up.", " I will hold.", " No retreat this time.", " I heard the order.", " I will watch the flank." };
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
                        index),
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
            int index)
        {
            string result = (value ?? string.Empty).Trim();
            string[] fillers =
            {
                "我会跟紧队列。", "别让侧翼空着。", "听见号令就行动。",
                "把眼前这条线守住。", "我不会留在后面。"
            };
            int fillerIndex = Math.Abs(index) % fillers.Length;
            while (result.Length < minimumChars)
            {
                string filler = fillers[fillerIndex++ % fillers.Length];
                result += filler;
            }
            if (result.Length > maximumChars)
            {
                result = result.Substring(0, Math.Max(1, maximumChars - 1)) + "！";
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
            bool allVisualCuesProcessed)
        {
            if (!speechCompleted)
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
                text.IndexOf("他说", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("她说", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("他喊", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("她喊", StringComparison.Ordinal) >= 0)
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
                text.StartsWith("他说", StringComparison.Ordinal) ||
                text.StartsWith("她说", StringComparison.Ordinal) ||
                text.StartsWith("他说道", StringComparison.Ordinal) ||
                text.StartsWith("她说道", StringComparison.Ordinal))
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
                return !text.StartsWith("不要让", StringComparison.Ordinal) &&
                       !text.StartsWith("别让", StringComparison.Ordinal) &&
                       !text.StartsWith("如果", StringComparison.Ordinal) &&
                       !text.StartsWith("假如", StringComparison.Ordinal);
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
                    return BattleSpeechTriggerKindV2.RequestNpcSpeech;
                case BattleSpeechCommandKindV1.Cancel:
                    return BattleSpeechTriggerKindV2.Cancel;
                default:
                    return BattleSpeechTriggerKindV2.None;
            }
        }

        private static bool TryReadInlinePlayerSpeech(string text, out string speech)
        {
            speech = null;
            Match match = Regex.Match(
                text,
                "^(?:我来|我要|由我|我)(?:给|向|跟)?(?:大家|众人|士兵|将士|弟兄|兄弟|队伍|全军)?(?:作)?(?:阵前|战前)?(?:演讲|训话|动员|鼓舞|讲几句|讲两句|讲俩句|说几句|说两句|说俩句)[：:\\s]*(.*)$",
                RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                return false;
            }
            speech = match.Groups[1].Value.Trim();
            return true;
        }

        private static BattleSpeechTriggerDecisionV2 BuildForcedDecision(
            BattleSpeechTriggerKindV2 kind,
            string speech,
            bool speakerIsNpc)
        {
            speech = (speech ?? string.Empty).Trim();
            if (speech.Length == 0 && !speakerIsNpc)
            {
                return new BattleSpeechTriggerDecisionV2(
                    kind,
                    speechText: null,
                    reason: speakerIsNpc
                        ? "Forced NPC speech text is empty."
                        : "Forced player speech text is empty.",
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
                   RequestCues.Any(cue => Contains(text, cue)) &&
                   (Contains(text, "讲") || Contains(text, "说") || Contains(text, "喊"));
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
