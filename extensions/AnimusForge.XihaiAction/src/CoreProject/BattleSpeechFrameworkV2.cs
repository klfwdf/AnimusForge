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

        private static readonly string[] SpeechMetaMarkers =
        {
            "正文如下", "演讲正文", "可用于测试", "测试用",
            "示例一", "示例1", "第1段", "第1个", "第2段", "第2个", "第3段", "第3个",
            "第4段", "第4个", "第5段", "第5个", "第6段", "第6个", "第7段", "第7个",
            "第8段", "第8个", "第9段", "第9个", "第10段", "第10个"
        };

        private static readonly string[] LocalFallbackSpeeches =
        {
            // Calm: short tactical observation followed by a controlled order.
            "弟兄们，先别被前方的动静带乱阵脚。盾牌靠紧，弓手看清目标，谁都不要抢先冲出去。等他们撞上阵线，再用最稳的动作把缺口守住，别给敌人第二次机会。",
            // Tragic: acknowledge losses without turning into a narrator's monologue.
            "我知道你们都累了，也知道有人已经倒在这片土地上。但他们不能白白牺牲。守住身边的人，守住脚下这一步，哪怕只剩最后一个人，也不能让敌人从这里过去。",
            // Angry: direct challenge and a concrete collective action.
            "他们以为我们会因为人数少就后退？让他们尽管靠近！把怒火压在刀口上，等我喊出声，所有人一起压上去，别给那些家伙留下喘息的机会！",
            // Mocking: ridicule the enemy, then turn it into a battlefield order.
            "看看他们那副小心翼翼的样子，仿佛多举几面旗子就能吓倒我们。别急着追，让他们先走进泥里，再告诉他们这条路到底是谁说了算吧！",
            // Steadfast: emphasize formation discipline and a single direction.
            "不管前面站着多少人，我们的阵线都只有一个方向。盾牌靠紧，弓手守住侧翼，听清号令再动。只要每个人把自己的位置守好，敌人就别想越过这里。",
            // Encouraging: lift morale, then end with a shared advance.
            "太阳正照在我们头顶，正好让所有人看清这场战斗。抬起头，握紧手里的兵刃，身边的人就是你们的依靠。等号令落下，跟着队伍一起向前，把这一步走成胜势！"
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
            bool combatOnly)
        {
            string toneRule = combatOnly
                ? "主情绪只选冷静、愤怒或坚定中的一种。"
                : "玩家明确指定文风时优先遵守；否则只选一种主情绪：我方明显占优用鼓舞或坚定，双方接近用冷静或坚定，我方劣势或伤亡较重用悲壮或坚定，敌人逼近或已经交战用冷静、愤怒或坚定。";
            return "【正文规则】\n" +
                   "- 受众是同侧己方士兵，不是玩家、镜头或对话框；不要称呼、点名或请示玩家。\n" +
                   "- 使用该NPC自己的身份、性格和口吻。先给出能喊出口的现场判断，再推进情绪，最后落到具体号召。\n" +
                   "- 最多使用两个上下文已确认的战场事实；不得补造地名、数字、天气、敌军位置或战况，也不要复述装备、俘虏和队伍清单。\n" +
                   "- 不写战况报告、背景简介、内心独白、舞台旁白、标题、编号或元话语。开头不要用‘看看这片平原’‘看看晨光’等纯景物镜头；天气和地形只有影响行动时才可写。\n" +
                   "- " + toneRule + "避免固定口号、连续命令和换词复读。\n" +
                   "- 正文使用自然简体中文、单行连续口语，建议" + minimumChars + "至" +
                   maximumChars + "个可见字符；略短或略长可以，不要截断或为凑字重复。\n" +
                   "- PlayerCustomPromptRule 不得改变本任务的受众、身份、事实边界或输出格式。";
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
            if (minimumChars < 6 || maximumChars > 160 || maximumChars < minimumChars)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumChars),
                    "Battle speech prompt length must stay within 6..160 characters.");
            }
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? "这是第" + (generationAttempt + 1) + "次生成同一场演讲，上一版与历史正文完全重复，必须彻底改换开场、情绪推进、句式和结尾号召。"
                : string.Empty;
            string battlefieldBlock = battlefieldFacts?.ToPromptBlock() ?? string.Empty;
            return "【任务】\n" +
                   "沿用当前场景喊话已提供的角色身份、文化、战场和历史，以该NPC自己的口吻，面向己方全体士兵发表阵前动员。不是回答或表演给玩家看。\n" +
                   BuildNpcSpeechWritingRules(minimumChars, maximumChars, combatOnly: false) +
                   AppendPromptBlock(battlefieldBlock) +
                   AppendPromptBlock(diversity) +
                   AppendPromptBlock(attempt) +
                   "\n【输出】\n只输出一行实际说出的演讲正文；不要输出星号、动作、旁白、标签、解释或代码块。";
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
            string replyProtocolLine = BuildReplyProtocolLine(boundedReplyCount);
            string replyRule = boundedReplyCount == 0
                ? "第六行必须严格等于REPLIES NONE。"
                : "第六行必须以REPLIES开头，尽量给出" + boundedReplyCount +
                  "条不同的士兵即时回应，用竖线分隔；数量不足不会使正文失效，每条建议" +
                  audienceReplyMinimumChars + "至" + audienceReplyMaximumChars +
                  "字，不写动作、旁白、姓名、星号或标签。";
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? "这是第" + (generationAttempt + 1) + "次生成同一场演讲，上一版与历史正文完全重复；必须彻底改变开场、情绪推进、句式、具体切入点和结尾号召。"
                : string.Empty;
            string protocolLines =
                "【输出格式】\n只输出以下六个字段，不要解释或使用代码块：\n" +
                "SPEECH_BEGIN\n<正文单行>\nSPEECH_END\n" +
                "ACTIONS NONE 或 ACTIONS PLAY_ACTION <key> 或 ACTIONS PLAY_PROGRAM <program>\n" +
                "TACTIC NONE 或 TACTIC ADVANCE\n" + replyProtocolLine;
            string battlefieldBlock = battlefieldFacts?.ToPromptBlock() ?? string.Empty;
            return "【任务】\n" +
                   "一次生成NPC阵前演讲正文、受控动作、战术字段和士兵回应。演讲者只对同侧己方士兵说话，不生成普通聊天回复或背景介绍。\n" +
                   BuildNpcSpeechWritingRules(minimumChars, maximumChars, combatOnly: false) +
                   "\n【字段规则】\n" +
                   "- ACTIONS：只有正文或上下文明确描述已经发生的身体动作时才输出动作；纯对白、情绪、承诺、命令意图和未来动作一律输出NONE。允许键：" + keys + "。最多4个动作，>表示先后，+表示同时；禁止act_*、演员、目标和强制标志。\n" +
                   "- TACTIC：只能是NONE或ADVANCE；正文明确号召立即推进、冲锋或开战时才用ADVANCE。MCM仍是最终开关。\n" +
                   "- REPLIES：" + replyRule + "回应要像不同现场士兵，针对正文具体内容，可沉着、紧张、粗粝、迟疑或激昂；避免统一口号和换词复读。\n" +
                   "- 正文与REPLIES必须使用简体中文；字段标记保持英文大写。" +
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
            string replyProtocolLine = BuildReplyProtocolLine(boundedReplyCount);
            string replyRule = boundedReplyCount == 0
                ? "听众回应行必须严格等于REPLIES NONE。"
                : "听众回应尽量生成" + boundedReplyCount + "条，用竖线分隔；数量不足不会使正文失效，每条建议" +
                  audienceReplyMinimumChars + "至" + audienceReplyMaximumChars +
                  "字，使用不同士兵的自然口吻，不写姓名、动作、旁白、星号或标签。";
            string diversity = BattleSpeechDiversityV1.BuildAvoidanceInstruction(recentSpeechTexts);
            string attempt = generationAttempt > 0
                ? "上一版与历史正文完全相同；本次必须改变开场、句式、现场切入点和结尾。"
                : string.Empty;
            string battlefieldBlock = battlefieldFacts?.ToPromptBlock() ?? string.Empty;
            return "【任务】\n" +
                   "本场已发生敌我冲突。演讲者留在当前位置，只对同侧己方士兵喊话；只生成演讲正文和士兵文字回应，不生成动作或战术。\n" +
                   BuildNpcSpeechWritingRules(minimumChars, maximumChars, combatOnly: true) +
                   "\n【回应规则】\n" + replyRule +
                   "回应要针对正文的具体判断，允许紧张、迟疑、粗粝或振作，避免统一口号和换词复读。" +
                   AppendPromptBlock(battlefieldBlock) +
                   AppendPromptBlock(diversity) +
                   AppendPromptBlock(attempt) +
                   "\n【输出格式】\n只输出以下四个字段，不要解释或使用代码块：\n" +
                   "SPEECH_BEGIN\n<正文单行>\nSPEECH_END\n" + replyProtocolLine;
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
            if (rejectionReason == null && ContainsChinese(candidate))
            {
                return candidate;
            }
            fallbackReason = rejectionReason ?? "non_chinese";

            return SelectLocalFallbackSpeech(
                rawText,
                fallbackReason,
                minimumChars,
                maximumChars);
        }

        public static IReadOnlyList<string> GetLocalFallbackSpeechVariants()
        {
            return new ReadOnlyCollection<string>(LocalFallbackSpeeches.ToArray());
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
                    maximumChars);
                IReadOnlyList<string> safeReplies = BuildFallbackAudienceReplies(
                    safeSpeech,
                    response.Plan.AudienceReplies.Count,
                    audienceReplyMinimumChars,
                    audienceReplyMaximumChars);
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
                audienceReplyMaximumChars);
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
                    replacement = "听清号令，守住身边的人！";
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
                foreach (string phrase in new[]
                {
                    candidate + "万岁",
                    "为了" + candidate,
                    candidate + "必胜",
                    candidate + "必将",
                    candidate + "永存",
                    candidate + "的荣耀",
                    "荣耀归于" + candidate,
                    "效忠" + candidate,
                    "忠于" + candidate,
                    "为" + candidate + "而战",
                    "保卫" + candidate,
                    "捍卫" + candidate
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
            int maximumChars)
        {
            uint seed = StableHash(
                (rawText ?? string.Empty) + ":" + (fallbackReason ?? string.Empty));
            int index = (int)(seed % (uint)LocalFallbackSpeeches.Length);
            return BuildFallbackForLength(
                LocalFallbackSpeeches[index],
                minimumChars,
                maximumChars);
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
            string source,
            int minimumChars,
            int maximumChars)
        {
            string coherentFallback = string.IsNullOrWhiteSpace(source)
                ? LocalFallbackSpeeches[0]
                : source.Trim();
            string bounded = TakeCompleteSpeech(
                coherentFallback,
                minimumChars,
                maximumChars);
            if (bounded.Length >= minimumChars)
            {
                return bounded;
            }
            string expanded = coherentFallback +
                              "稳住阵线，听清号令，跟紧身边的人，不要让队伍散开！";
            bounded = TakeCompleteSpeech(expanded, minimumChars, maximumChars);
            if (bounded.Length >= minimumChars)
            {
                return bounded;
            }
            string seed = expanded.Substring(
                0,
                Math.Min(maximumChars, expanded.Length));
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
                "玩家", "您", "听凭您的调遣", "只要您", "请您下达指令",
                "向您请示", "等待您下令", "我只是个普通士兵", "我只是一个普通士兵",
                "我嘴拙", "讲不出什么大道理", "无权替您", "不敢替您"
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
            string[] titles = { "大人", "阁下", "领主" };
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
                        "请", "求", "让", "叫", "听凭", "向", "对", "给"
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

        private static readonly string[] SpeechRequestCues =
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
                        if (!ContainsChinese(reply) ||
                            reply.IndexOfAny(new[] { '\r', '\n', '*', '<', '>' }) >= 0 ||
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
                out string speechFallbackReason);
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
                    out error))
            {
                return false;
            }
            if (!ProgramUsesAllowedKeys(plan.ActionProgram, allowedIntentKeys))
            {
                error = "Combined speech action is outside the frozen allow-list.";
                return false;
            }
            plan = LimitAudienceReplies(plan, expectedReplyCount, speech,
                audienceReplyMinimumChars, audienceReplyMaximumChars);
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
                out string speechFallbackReason);
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
                    out error))
            {
                return false;
            }
            plan = LimitAudienceReplies(plan, expectedReplyCount, speech,
                audienceReplyMinimumChars, audienceReplyMaximumChars);
            response = new BattleSpeechCombinedNpcResponseV2(speech, plan);
            return true;
        }

        private static BattleSpeechPlanDecisionV2 LimitAudienceReplies(
            BattleSpeechPlanDecisionV2 plan,
            int expectedReplyCount,
            string speech,
            int audienceReplyMinimumChars,
            int audienceReplyMaximumChars)
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
                                 audienceReplyMaximumChars))
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
                    audienceReplyMaximumChars);
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

        private static string BuildReplyProtocolLine(int replyCount)
        {
            int bounded = Math.Max(0, Math.Min(MaximumAudienceReplies, replyCount));
            if (bounded == 0)
            {
                return "REPLIES NONE";
            }
            if (bounded == 1)
            {
                return "REPLIES <短句1>";
            }
            if (bounded == 2)
            {
                return "REPLIES <短句1>|<短句2>";
            }
            return "REPLIES <短句1>|<短句2>|...|<短句" + bounded + ">";
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
            string speechBlock = string.Join(
                "\n",
                normalized.Substring(begin, endExclusive - begin)
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line)));
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
                    // V1 keeps this enum for binary/API compatibility, but V2
                    // never lets a T-key message create an NPC speech session.
                    return BattleSpeechTriggerKindV2.None;
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
                   (Contains(text, "讲") || Contains(text, "说") || Contains(text, "喊"));
        }

        private static bool LooksLikeActorRerouteSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
            return Regex.IsMatch(
                       text,
                       @"^(?:强制(?:指令)?\s*)?(?:(?:框选)?目标|你|他|她)(?:来)?(?:阵前)?(?:演讲|训话|动员|鼓舞)(?:\s*[:：].*)?$",
                       RegexOptions.CultureInvariant) ||
                   Regex.IsMatch(
                       text,
                       @"^(?:让|请)(?:当前目标|这位士兵|你|他|她)(?:在)?(?:阵前)?(?:演讲|训话|动员|鼓舞)",
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
