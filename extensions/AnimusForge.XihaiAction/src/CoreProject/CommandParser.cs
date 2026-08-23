using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge.SceneActions.Core
{
    public sealed class CommandParser
    {
        private const string ForceFramedPrefix = "强制";
        private static readonly Regex ClassifierOutputPattern = new Regex(
            "^PLAY_ACTION ([a-z0-9][a-z0-9_-]*)$",
            RegexOptions.CultureInvariant);
        private static readonly Regex ClassifierProgramOutputPattern = new Regex(
            "^PLAY_PROGRAM ([a-z0-9][a-z0-9_-]*(?:[+>][a-z0-9][a-z0-9_-]*){0,3})$",
            RegexOptions.CultureInvariant);
        private static readonly Regex NpcStageDirectionPattern = new Regex(
            "(?:\\*\\*(?<stage>[^*]{1,512})\\*\\*|(?<!\\*)\\*(?<stage>[^*]{1,512})\\*(?!\\*))",
            RegexOptions.CultureInvariant);
        private static readonly Regex PostposedKneelActorPattern = new Regex(
            "(?:跪在|跪坐在|单膝跪在|双膝跪在)[^，。；！？]{0,24}的(?<actor>[\\p{L}·“”]{1,24})",
            RegexOptions.CultureInvariant);
        private static readonly Regex NpcClauseBoundaryPattern = new Regex(
            "[。！？；;!?]+|(?:然后|随后|随即|接着|继而|转而|而是|反而|但是|不过|却|但)",
            RegexOptions.CultureInvariant);
        private static readonly Regex RawActionIdPattern = new Regex(
            "act_",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly string[] NonSelfPlayerPrefixes =
        {
            "我们",
            "我方",
            "我的",
            "我等",
            "我辈",
            "我军",
            "我队",
            "我部",
            "我手下",
            "我麾下",
            "我让",
            "我叫",
            "我令",
            "我命令",
            "我要求",
            "我请他们",
            "我请大家"
        };
        private static readonly string[] NpcThirdPartyDirectiveVerbs =
        {
            "命令",
            "要求",
            "吩咐",
            "迫使",
            "逼迫",
            "强迫",
            "让",
            "叫",
            "请",
            "要",
            "逼"
        };
        private static readonly string[] NpcThirdPartyRecipients =
        {
            "他们",
            "她们",
            "所有人",
            "众人",
            "大家",
            "部下",
            "士兵",
            "我们",
            "你们",
            "我",
            "你",
            "他",
            "她"
        };
        private static readonly string[] ClearlyNonPerformedNpcMarkers =
        {
            "拒绝", "不愿", "不肯", "并未", "未曾", "从未", "绝不",
            "不再", "毫无", "只是想", "仅想", "只想", "想要", "试图",
            "准备", "打算", "正要", "刚要", "差点", "险些", "几乎",
            "如果", "假如", "倘若", "若是", "要是", "万一", "是否",
            "请解释", "只是说", "说出", "提到", "谈到", "讨论", "引用",
            "回忆", "先前", "此前", "曾经", "昨天", "口头表示", "声称",
            "考虑", "没有付诸行动", "没有做出任何动作", "没有任何动作",
            "最终忍住", "眼下站着没动", "站着没动", "一动不动", "纹丝不动",
            "不是否认", "并非否认", "不是拒绝", "并非拒绝"
        };
        private static readonly string[] StopOwnedActionCues =
        {
            "停止欢呼", "别再欢呼", "不再欢呼", "停止跳舞", "别跳了",
            "停止当前动作", "停止动作", "停下当前动作", "结束当前动作",
            "结束此项", "结束这个动作", "收起动作", "恢复正常", "恢复站姿",
            "恢复端正站姿", "回到正常站姿", "站好", "放下手臂", "把手放下",
            "将手放下", "收回手臂", "垂下手臂", "结束行礼", "停止行礼",
            "stop cheering",
            "stop dancing", "stop action", "return to idle"
        };
        private static readonly string[] DrawWeaponCues =
        {
            "拔剑", "抽剑", "拔出剑", "抽出剑", "拔出佩剑", "抽出佩剑",
            "拔出武器", "抽出武器", "剑已出鞘", "拔剑出鞘", "抽剑出鞘",
            "剑身出鞘", "draw weapon", "draw sword",
            "drew his sword", "drew her sword", "unsheathed his sword",
            "unsheathed her sword"
        };
        private static readonly string[] SheatheWeaponCues =
        {
            "收剑", "还剑入鞘", "收剑入鞘", "将剑插回鞘中", "把剑收回鞘中",
            "收起武器", "武器入鞘", "剑已入鞘", "sheathe weapon",
            "sheathed his sword", "sheathed her sword", "put his sword away",
            "put her sword away"
        };
        private static readonly string[] RuntimeControlNonPerformedMarkers =
        {
            "拒绝", "不愿", "不肯", "并未", "未曾", "从未", "绝不", "没有",
            "只是想", "仅想", "只想", "想要", "试图", "准备", "打算", "正要",
            "刚要", "差点", "险些", "几乎", "如果", "假如", "倘若", "若是",
            "要是", "万一", "是否", "请解释", "只是说", "说出", "提到", "谈到",
            "讨论", "引用", "回忆", "先前", "此前", "曾经", "昨天", "口头表示",
            "声称", "考虑", "没有付诸行动", "没有做出任何动作", "没有任何动作",
            "最终忍住", "眼下站着没动", "站着没动", "一动不动", "纹丝不动"
        };

        private readonly SceneActionCatalog _catalog;
        private readonly bool _useV3;
        private readonly bool _useV4;

        public CommandParser(SceneActionCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _useV3 = SceneActionFrameworkV3.LogicalActions.All(entry =>
                _catalog.Intents.ContainsKey(entry.IntentKey));
            _useV4 = SceneActionFrameworkV4.LogicalActions.All(entry =>
                _catalog.Intents.ContainsKey(entry.IntentKey));
        }

        public ParseDecision ParsePlayerText(string rawText, SceneActionSettings settings)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return ParseDecision.None();
            }
            if (rawText.IndexOf('\r') >= 0 || rawText.IndexOf('\n') >= 0)
            {
                return ParseDecision.Invalid("Commands must be a single line.");
            }

            string trimmed = rawText.Trim();
            if (IsSingleLeadingStar(trimmed))
            {
                string forcedText = Normalize(trimmed.Substring(1));
                if (forcedText.StartsWith(ForceFramedPrefix, StringComparison.Ordinal))
                {
                    return ParseForcedFramedImmediate(
                        forcedText.Substring(ForceFramedPrefix.Length),
                        settings);
                }

                TargetMode runtimeControlTarget = TargetMode.FramedSelection;
                string runtimeControlText = forcedText;
                if (HasPlayerTargetPrefix(runtimeControlText))
                {
                    runtimeControlTarget = TargetMode.Player;
                    runtimeControlText = Normalize(runtimeControlText.Substring(1));
                }
                if (TryResolveRuntimeControlCommand(
                    runtimeControlText,
                    out string runtimeControlIntent))
                {
                    return ParseDecision.MatchRuntimeControl(
                        runtimeControlIntent,
                        runtimeControlTarget,
                        ResolverSource.ForceNaturalLanguage,
                        IsImmediatePlayerRuntimeControl(runtimeControlIntent));
                }
                if (settings?.ForceExactEnabled != false &&
                    TryResolveForcedTarget(
                        forcedText,
                        out AliasDefinition forcedAlias,
                        out TargetMode forcedTarget))
                {
                    return MatchDecision(
                        forcedAlias.IntentKey,
                        forcedTarget,
                        ResolverSource.ForceExact);
                }

                TargetMode naturalTarget = TargetMode.FramedSelection;
                string naturalText = forcedText;
                if (HasPlayerTargetPrefix(naturalText))
                {
                    naturalTarget = TargetMode.Player;
                    naturalText = Normalize(naturalText.Substring(1));
                }

                if (RawActionIdPattern.IsMatch(naturalText))
                {
                    return ParseDecision.Invalid(
                        "Raw action ids are not valid forced commands.");
                }

                IReadOnlyList<string> naturalIntents =
                    ResolveNaturalActionDescription(naturalText);
                if (ContainsUnsupportedActionReference(naturalText))
                {
                    return ParseDecision.Invalid(
                        "Forced action description contains an unsupported action.");
                }
                if (HasSuppressedKnownActionReference(
                    naturalText,
                    naturalIntents))
                {
                    return ParseDecision.Invalid(
                        "Forced action description contains a negated, quoted, hypothetical, or unperformed known action.");
                }
                if (naturalIntents.Count == 1)
                {
                    return MatchDecision(
                        naturalIntents[0],
                        naturalTarget,
                        ResolverSource.ForceNaturalLanguage);
                }
                if (naturalIntents.Count > 1)
                {
                    return ParseDecision.Fallback(naturalText, naturalTarget);
                }
                if (ContainsNaturalActionReference(naturalText))
                {
                    return ParseDecision.Invalid(
                        "Forced action description references a known action that was not performed.");
                }
                if (string.IsNullOrEmpty(naturalText))
                {
                    return ParseDecision.Invalid(
                        "Forced action description is empty.");
                }
                return ParseDecision.Fallback(naturalText, naturalTarget);
            }

            if (trimmed.StartsWith("*", StringComparison.Ordinal))
            {
                return ParseDecision.None(stopResolution: true);
            }

            if (settings?.ExactCommandEnabled != false)
            {
                string exactText = Normalize(trimmed);
                if (_catalog.TryGetExactAlias(exactText, out AliasDefinition exactAlias))
                {
                    return MatchDecision(
                        exactAlias.IntentKey,
                        exactAlias.TargetOverride,
                        ResolverSource.ExactCommand);
                }
            }

            return ParseDecision.None();
        }

        private ParseDecision ParseForcedFramedImmediate(
            string rawActionText,
            SceneActionSettings settings)
        {
            string actionText = Normalize(rawActionText);
            if (string.IsNullOrEmpty(actionText))
            {
                return ParseDecision.Invalid(
                    "Forced framed command action is empty.");
            }
            if (RawActionIdPattern.IsMatch(actionText))
            {
                return ParseDecision.Invalid(
                    "Raw action ids are not valid forced framed commands.");
            }
            if (TryResolveRuntimeControlCommand(
                actionText,
                out string runtimeControlIntent))
            {
                return ParseDecision.MatchRuntimeControl(
                    runtimeControlIntent,
                    TargetMode.FramedSelection,
                    ResolverSource.ForceFramedNaturalLanguage,
                    true);
            }
            if (settings?.ForceExactEnabled != false &&
                _catalog.TryGetForceAlias(actionText, out AliasDefinition exactAlias))
            {
                return MatchDecision(
                    exactAlias.IntentKey,
                    TargetMode.FramedSelection,
                    ResolverSource.ForceFramedExact,
                    true);
            }

            IReadOnlyList<string> naturalIntents =
                ResolveNaturalActionDescription(actionText);
            if (ContainsUnsupportedActionReference(actionText))
            {
                return ParseDecision.Invalid(
                    "Forced framed action description contains an unsupported action.");
            }
            if (HasSuppressedKnownActionReference(
                actionText,
                naturalIntents))
            {
                return ParseDecision.Invalid(
                    "Forced framed action description contains a negated, quoted, hypothetical, or unperformed known action.");
            }
            if (naturalIntents.Count == 1)
            {
                return MatchDecision(
                    naturalIntents[0],
                    TargetMode.FramedSelection,
                    ResolverSource.ForceFramedNaturalLanguage,
                    true);
            }
            if (naturalIntents.Count > 1)
            {
                return ParseDecision.Fallback(
                    actionText,
                    TargetMode.FramedSelection,
                    true);
            }
            if (ContainsNaturalActionReference(actionText))
            {
                return ParseDecision.Invalid(
                    "Forced framed action description references a known action that was not performed.");
            }
            return ParseDecision.Fallback(
                actionText,
                TargetMode.FramedSelection,
                true);
        }

        public ParseDecision ParseNpcReplyClassifierOutput(string output)
        {
            return ParseClassifierOutput(output);
        }

        public ParseDecision ParseNpcReplyText(string rawText)
        {
            return ParseNpcReplyText(rawText, null);
        }

        public ParseDecision ParseNpcReplyText(string rawText, string speakerName)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return ParseDecision.None();
            }

            List<string> resolvedIntents = new List<string>();
            bool sawStageDirection = false;
            List<string> fallbackDescriptions = new List<string>();
            List<string> allStageDescriptions = new List<string>();
            foreach (Match match in NpcStageDirectionPattern.Matches(rawText))
            {
                sawStageDirection = true;
                string stageText = Normalize(match.Groups["stage"].Value);
                if (string.IsNullOrEmpty(stageText))
                {
                    continue;
                }
                allStageDescriptions.Add(stageText);
                if (RawActionIdPattern.IsMatch(stageText))
                {
                    return ParseDecision.Invalid(
                        "NPC stage direction contains a raw action id.");
                }
                if (TryResolveNpcRuntimeControlDescription(
                    stageText,
                    out string runtimeControlIntent))
                {
                    if (!resolvedIntents.Contains(
                        runtimeControlIntent,
                        StringComparer.Ordinal))
                    {
                        resolvedIntents.Add(runtimeControlIntent);
                    }
                    continue;
                }
                if (ContainsThirdPartyActionDirective(stageText))
                {
                    // The closed classifier keeps the actor frozen to the replying NPC.
                    // Let it distinguish a command gesture performed by the speaker from
                    // a purely verbal instruction to somebody else.
                    fallbackDescriptions.Add(stageText);
                    continue;
                }
                IReadOnlyList<string> rawCandidateIntents =
                    ResolveNpcReplyDescriptionByClauses(stageText);
                List<string> candidateIntents = rawCandidateIntents
                    .Where(intent => !ShouldSuppressNpcIntent(
                        stageText,
                        speakerName,
                        intent))
                    .ToList();
                bool removedBenignCandidate =
                    candidateIntents.Count < rawCandidateIntents.Count;
                if (ContainsBlockingUnsupportedActionReference(
                    stageText,
                    candidateIntents))
                {
                    // Unsupported actions can coexist with an independently performed
                    // whitelisted gesture. The closed classifier may select only the
                    // whitelisted part and can never emit a native action id or target.
                    fallbackDescriptions.Add(stageText);
                    continue;
                }
                if (candidateIntents.Count == 0 &&
                    IsAmbiguousPerformedHeadShake(stageText))
                {
                    fallbackDescriptions.Add(stageText);
                    continue;
                }
                if (candidateIntents.Count == 0 &&
                    IsClearlyNonPerformedNpcAction(stageText) &&
                    ContainsNaturalActionReference(stageText))
                {
                    continue;
                }
                if (candidateIntents.Count == 0 &&
                    !removedBenignCandidate &&
                    HasSuppressedKnownActionReference(
                        stageText,
                        candidateIntents))
                {
                    // A negated, hypothetical, quoted, attempted, or otherwise
                    // non-performed known action is a confirmed no-op. It must not
                    // veto another independently performed stage direction.
                    continue;
                }
                if (candidateIntents.Count == 0)
                {
                    if (removedBenignCandidate)
                    {
                        if (ShouldClassifySuppressedNpcCandidate(
                            stageText,
                            rawCandidateIntents))
                        {
                            fallbackDescriptions.Add(stageText);
                        }
                        continue;
                    }
                    if (ContainsNaturalActionReference(stageText))
                    {
                        // The text refers to a controlled action but the deterministic
                        // cues cannot prove which gesture was actually performed.
                        // Route it to AF's target-blind, closed-set classifier.
                        fallbackDescriptions.Add(stageText);
                    }
                    else
                    {
                        fallbackDescriptions.Add(stageText);
                    }
                    continue;
                }
                foreach (string candidateIntent in candidateIntents)
                {
                    if (!resolvedIntents.Contains(candidateIntent, StringComparer.Ordinal))
                    {
                        resolvedIntents.Add(candidateIntent);
                    }
                }
            }

            if (resolvedIntents.Count > 1 ||
                (resolvedIntents.Count > 0 && fallbackDescriptions.Count > 0))
            {
                return ParseDecision.Fallback(
                    string.Join(" ", allStageDescriptions),
                    null);
            }
            if (resolvedIntents.Count == 1)
            {
                return MatchDecision(
                    resolvedIntents[0],
                    null,
                    ResolverSource.NpcStageDirection);
            }
            if (fallbackDescriptions.Count > 0)
            {
                return ParseDecision.Fallback(
                    string.Join(" ", fallbackDescriptions),
                    null);
            }
            return ParseDecision.None(stopResolution: sawStageDirection);
        }

        /// <summary>
        /// Builds a program from explicit, already-performed NPC stage cues when
        /// the normal path would otherwise ask the remote classifier. This is
        /// deliberately limited to the text extracted from a stage direction;
        /// it never scans ordinary dialogue and refuses third-party commands,
        /// negation, hypotheticals, and unsupported mixed actions.
        /// </summary>
        public bool TryBuildDeterministicNpcProgram(
            string stageText,
            string speakerName,
            out ActionProgramV4 program)
        {
            program = null;
            string normalized = Normalize(stageText);
            if (string.IsNullOrEmpty(normalized) ||
                !_useV4 ||
                ContainsThirdPartyActionDirective(normalized) ||
                IsClearlyNonPerformedNpcAction(normalized))
            {
                return false;
            }

            IReadOnlyList<string> rawCandidates =
                ResolveNpcReplyDescriptionByClauses(normalized);
            List<string> candidates = rawCandidates
                .Where(key => IsLogicalIntent(key) &&
                              !ShouldSuppressNpcIntent(normalized, speakerName, key))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (candidates.Count == 0 ||
                candidates.Count > ActionProgramV4.MaximumActionCount ||
                ContainsBlockingUnsupportedActionReference(normalized, candidates))
            {
                return false;
            }

            try
            {
                ActionProgramV4 rawProgram = new ActionProgramV4(new[]
                {
                    new ActionProgramStepV4(candidates)
                });
                return rawProgram.TryNormalizeForExecution(out program, out _);
            }
            catch
            {
                program = null;
                return false;
            }
        }

        private bool ContainsThirdPartyActionDirective(string stageText)
        {
            foreach (string verb in NpcThirdPartyDirectiveVerbs)
            {
                int verbSearchFrom = 0;
                while (verbSearchFrom <= stageText.Length - verb.Length)
                {
                    int verbIndex = stageText.IndexOf(
                        verb,
                        verbSearchFrom,
                        StringComparison.Ordinal);
                    if (verbIndex < 0)
                    {
                        break;
                    }

                    int recipientStart = verbIndex + verb.Length;
                    while (recipientStart < stageText.Length &&
                           (char.IsWhiteSpace(stageText[recipientStart]) ||
                            stageText[recipientStart] == '着'))
                    {
                        recipientStart++;
                    }
                    foreach (string recipient in NpcThirdPartyRecipients)
                    {
                        if (recipientStart + recipient.Length > stageText.Length ||
                            !string.Equals(
                                stageText.Substring(recipientStart, recipient.Length),
                                recipient,
                                StringComparison.Ordinal))
                        {
                            continue;
                        }

                        int directedStart = recipientStart + recipient.Length;
                        while (directedStart < stageText.Length &&
                               char.IsWhiteSpace(stageText[directedStart]))
                        {
                            directedStart++;
                        }
                        string directedText = stageText.Substring(directedStart);
                        if (ContainsNaturalActionReference(directedText))
                        {
                            return true;
                        }
                    }
                    verbSearchFrom = verbIndex + verb.Length;
                }
            }
            return false;
        }

        private IReadOnlyList<string> ResolveNpcReplyDescriptionByClauses(string stageText)
        {
            List<string> resolved = new List<string>(
                ResolveNpcReplyDescription(stageText));
            foreach (string clause in NpcClauseBoundaryPattern.Split(stageText ?? string.Empty))
            {
                string normalizedClause = Normalize(clause);
                if (string.IsNullOrEmpty(normalizedClause))
                {
                    continue;
                }
                foreach (string intent in ResolveNpcReplyDescription(normalizedClause))
                {
                    if (!resolved.Contains(intent, StringComparer.Ordinal))
                    {
                        resolved.Add(intent);
                    }
                }
            }
            return resolved;
        }

        private bool ContainsBlockingUnsupportedActionReference(
            string stageText,
            IEnumerable<string> performedIntents)
        {
            if (!ContainsUnsupportedActionReference(stageText))
            {
                return false;
            }
            if (!(performedIntents ?? Enumerable.Empty<string>()).Any())
            {
                return true;
            }

            string withoutContextMovement = stageText ?? string.Empty;
            foreach (string cue in new[]
            {
                "转身", "走路", "走了两步", "走了过去", "走过去",
                "走向", "走到", "走去", "迈步走向"
            })
            {
                withoutContextMovement = withoutContextMovement.Replace(cue, string.Empty);
            }
            return ContainsUnsupportedActionReference(withoutContextMovement);
        }

        private static bool ShouldSuppressNpcIntent(
            string stageText,
            string speakerName,
            string intentKey)
        {
            if (string.Equals(intentKey, SceneActionFrameworkV2.Dance, StringComparison.Ordinal))
            {
                bool hasExplicitDance = ContainsAny(stageText, new[]
                {
                    "跳舞", "舞蹈", "舞动", "起舞", "跳起舞", "翩翩"
                });
                if (!hasExplicitDance && ContainsAny(stageText, new[]
                {
                    "转了半圈", "转半圈", "旋转半圈"
                }))
                {
                    return true;
                }
            }

            if (string.Equals(intentKey, SceneActionFrameworkV1.Applaud, StringComparison.Ordinal))
            {
                bool hasExplicitApplause = ContainsAny(stageText, new[]
                {
                    "鼓掌", "拍手叫好", "拍手喝彩", "拍起手来", "鼓起掌", "抚掌"
                });
                if (!hasExplicitApplause && ContainsAny(stageText, new[]
                {
                    "拍了拍手上的灰", "拍掉手上的灰", "拍去手上的灰",
                    "拍了拍衣服", "拍了拍胸口", "拍了拍肩膀",
                    "拍了拍马鞍", "拍了拍桌子", "拍了拍脸"
                }))
                {
                    return true;
                }
            }

            if (string.Equals(intentKey, SceneActionFrameworkV3.Disagree, StringComparison.Ordinal) &&
                ContainsAny(stageText, new[]
                {
                    "不是否认", "并非否认", "不是拒绝", "并非拒绝",
                    "不是不同意", "并非不同意"
                }))
            {
                return true;
            }

            if (string.Equals(intentKey, SceneActionFrameworkV1.StandUp, StringComparison.Ordinal) &&
                ContainsAny(stageText, new[]
                {
                    "站直身体", "站直身子", "挺直身躯", "挺直腰背", "站直腰背"
                }) &&
                !ContainsAny(stageText, new[]
                {
                    "站起来", "站起身", "站了起来", "从跪姿", "从地上",
                    "撑地", "爬起", "起身", "重新起立"
                }))
            {
                return true;
            }

            if (string.Equals(intentKey, SceneActionFrameworkV2.Challenge, StringComparison.Ordinal) &&
                ContainsAny(stageText, new[] { "挑衅的姿态", "充满挑衅" }) &&
                !ContainsAny(stageText, new[]
                {
                    "勾手", "招手挑衅", "拍胸叫阵", "叫阵", "勾了勾手指"
                }))
            {
                return true;
            }

            return string.Equals(intentKey, SceneActionFrameworkV1.Kneel, StringComparison.Ordinal) &&
                   ContainsExplicitThirdPartyKneel(stageText, speakerName);
        }

        private static bool ShouldClassifySuppressedNpcCandidate(
            string stageText,
            IEnumerable<string> rawCandidateIntents)
        {
            return (rawCandidateIntents ?? Enumerable.Empty<string>()).Contains(
                       SceneActionFrameworkV3.Disagree,
                       StringComparer.Ordinal) &&
                   ContainsAny(stageText, new[] { "摇头", "摇了摇头", "摇摇头" }) &&
                   !ContainsAny(stageText, new[]
                   {
                       "不是否认", "并非否认", "不是拒绝", "并非拒绝",
                       "不是不同意", "并非不同意"
                   });
        }

        private static bool IsClearlyNonPerformedNpcAction(string stageText)
        {
            return ContainsAny(stageText, ClearlyNonPerformedNpcMarkers);
        }

        private static bool IsAmbiguousPerformedHeadShake(string stageText)
        {
            return ContainsAny(stageText, new[] { "摇头", "摇了摇头", "摇摇头" }) &&
                   !IsClearlyNonPerformedNpcAction(stageText) &&
                   !ContainsAny(stageText, new[]
                   {
                       "不是否认", "并非否认", "不是拒绝", "并非拒绝",
                       "不是不同意", "并非不同意"
                   });
        }

        private static bool ContainsExplicitThirdPartyKneel(
            string stageText,
            string speakerName)
        {
            foreach (Match match in PostposedKneelActorPattern.Matches(stageText ?? string.Empty))
            {
                string actor = match.Groups["actor"].Value.Trim('“', '”');
                if (string.IsNullOrEmpty(actor) ||
                    actor.StartsWith("我", StringComparison.Ordinal) ||
                    actor.StartsWith("自己", StringComparison.Ordinal) ||
                    actor.StartsWith("他", StringComparison.Ordinal) ||
                    actor.StartsWith("她", StringComparison.Ordinal) ||
                    actor.StartsWith("NPC", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(speakerName) &&
                    (actor.IndexOf(speakerName, StringComparison.Ordinal) >= 0 ||
                     speakerName.IndexOf(actor, StringComparison.Ordinal) >= 0))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private static bool ContainsAny(string text, IEnumerable<string> cues)
        {
            return !string.IsNullOrEmpty(text) &&
                   (cues ?? Enumerable.Empty<string>()).Any(cue =>
                       !string.IsNullOrEmpty(cue) &&
                       text.IndexOf(cue, StringComparison.Ordinal) >= 0);
        }

        private static bool TryResolveRuntimeControlCommand(
            string text,
            out string intentKey)
        {
            intentKey = null;
            string normalized = Normalize(text);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            bool stopsNamedAction =
                (normalized.StartsWith("停止", StringComparison.Ordinal) ||
                 normalized.StartsWith("别再", StringComparison.Ordinal) ||
                 normalized.StartsWith("不要再", StringComparison.Ordinal) ||
                 normalized.StartsWith("不再", StringComparison.Ordinal)) &&
                (ContainsAny(normalized, DrawWeaponCues) ||
                 ContainsAny(normalized, SheatheWeaponCues) ||
                 normalized.IndexOf("欢呼", StringComparison.Ordinal) >= 0 ||
                 normalized.IndexOf("跳舞", StringComparison.Ordinal) >= 0 ||
                 normalized.IndexOf("动作", StringComparison.Ordinal) >= 0);
            if (stopsNamedAction || ContainsAny(normalized, StopOwnedActionCues))
            {
                intentKey = SceneActionRuntimeControlsV1.StopAction;
                return true;
            }
            if (ContainsAny(normalized, SheatheWeaponCues))
            {
                intentKey = SceneActionRuntimeControlsV1.SheatheWeapon;
                return true;
            }
            if (ContainsAny(normalized, DrawWeaponCues))
            {
                intentKey = SceneActionRuntimeControlsV1.DrawWeapon;
                return true;
            }
            return false;
        }

        private static bool IsImmediatePlayerRuntimeControl(string intentKey)
        {
            // Stopping a currently playing action is an owner-side cleanup
            // command. It must not wait for an NPC consent round, otherwise a
            // cheer/gesture submitted by the battle-speech performance can
            // continue while the player is already issuing a replacement order.
            return string.Equals(
                intentKey,
                SceneActionRuntimeControlsV1.StopAction,
                StringComparison.Ordinal);
        }

        private static bool TryResolveNpcRuntimeControlDescription(
            string stageText,
            out string intentKey)
        {
            intentKey = null;
            if (string.IsNullOrEmpty(stageText) ||
                ContainsAny(stageText, RuntimeControlNonPerformedMarkers))
            {
                return false;
            }

            bool stop = ContainsAny(stageText, StopOwnedActionCues);
            bool sheathe = ContainsAny(stageText, SheatheWeaponCues);
            bool draw = ContainsAny(stageText, DrawWeaponCues) ||
                        (stageText.IndexOf("握住剑柄", StringComparison.Ordinal) >= 0 &&
                         ContainsAny(stageText, new[] { "抽出", "拔出", "出鞘" }));
            int count = (stop ? 1 : 0) + (sheathe ? 1 : 0) + (draw ? 1 : 0);
            if (count != 1)
            {
                return false;
            }
            intentKey = stop
                ? SceneActionRuntimeControlsV1.StopAction
                : sheathe
                    ? SceneActionRuntimeControlsV1.SheatheWeapon
                    : SceneActionRuntimeControlsV1.DrawWeapon;
            return true;
        }

        public ParseDecision ParseClassifierOutput(string output)
        {
            if (string.IsNullOrEmpty(output) ||
                output.IndexOf('\r') >= 0 ||
                output.IndexOf('\n') >= 0)
            {
                return ParseDecision.Invalid("Classifier output must be one exact line.");
            }

            string normalizedOutput = output.Trim();
            if (string.Equals(normalizedOutput, "NONE", StringComparison.Ordinal))
            {
                return ParseDecision.None();
            }

            Match match = ClassifierOutputPattern.Match(normalizedOutput);
            if (match.Success)
            {
                string intentKey = match.Groups[1].Value;
                if (!_catalog.IsClassifierSelectable(intentKey) ||
                    !IsLogicalIntent(intentKey))
                {
                    return ParseDecision.Invalid("Classifier selected a non-whitelisted intent.");
                }
                return MatchDecision(intentKey, null, ResolverSource.AiClassifier);
            }

            Match programMatch = ClassifierProgramOutputPattern.Match(normalizedOutput);
            if (!programMatch.Success)
            {
                return ParseDecision.Invalid("Malformed classifier output.");
            }
            if (_useV4)
            {
                if (!ActionProgramV4.TryParseExpression(
                    programMatch.Groups[1].Value,
                    out ActionProgramV4 programV4,
                    out string programV4Error))
                {
                    return ParseDecision.Invalid(programV4Error);
                }
                if (programV4.Steps.SelectMany(step => step.IntentKeys)
                    .Any(key => !_catalog.IsClassifierSelectable(key)))
                {
                    return ParseDecision.Invalid(
                        "Classifier program selected a non-whitelisted V4 intent.");
                }
                if (!programV4.TryNormalizeForExecution(
                    out ActionProgramV4 normalizedProgramV4,
                    out string normalizationV4Error))
                {
                    return ParseDecision.Invalid(normalizationV4Error);
                }
                return ParseDecision.MatchProgramV4(
                    normalizedProgramV4,
                    null,
                    ResolverSource.AiClassifier);
            }
            if (_useV3)
            {
                if (!ActionProgramV3.TryParseExpression(
                    programMatch.Groups[1].Value,
                    out ActionProgramV3 programV3,
                    out string programV3Error))
                {
                    return ParseDecision.Invalid(programV3Error);
                }
                if (programV3.Steps.SelectMany(step => step.IntentKeys)
                    .Any(key => !_catalog.IsClassifierSelectable(key)))
                {
                    return ParseDecision.Invalid(
                        "Classifier program selected a non-whitelisted V3 intent.");
                }
                if (!programV3.TryNormalizeForExecution(
                    out ActionProgramV3 normalizedProgramV3,
                    out string normalizationV3Error))
                {
                    return ParseDecision.Invalid(normalizationV3Error);
                }
                return ParseDecision.MatchProgramV3(
                    normalizedProgramV3,
                    null,
                    ResolverSource.AiClassifier);
            }

            if (!ActionProgramV2.TryParseExpression(
                programMatch.Groups[1].Value,
                out ActionProgramV2 program,
                out string programError))
            {
                return ParseDecision.Invalid(programError);
            }
            if (program.Steps.SelectMany(step => step.IntentKeys)
                .Any(key => !_catalog.IsClassifierSelectable(key)))
            {
                return ParseDecision.Invalid(
                    "Classifier program selected a non-whitelisted intent.");
            }
            if (!program.TryNormalizeForExecution(
                out ActionProgramV2 normalizedProgram,
                out string normalizationError))
            {
                return ParseDecision.Invalid(normalizationError);
            }
            return ParseDecision.MatchProgram(
                normalizedProgram,
                null,
                ResolverSource.AiClassifier);
        }

        private ParseDecision MatchDecision(
            string intentKey,
            TargetMode? targetOverride,
            ResolverSource resolver,
            bool bypassNpcConsent = false)
        {
            if (SceneActionRuntimeControlsV1.IsControlIntent(intentKey))
            {
                return ParseDecision.MatchRuntimeControl(
                    intentKey,
                    targetOverride,
                    resolver,
                    bypassNpcConsent);
            }
            return _useV4
                ? ParseDecision.MatchV4(
                    intentKey,
                    targetOverride,
                    resolver,
                    bypassNpcConsent)
                : _useV3
                    ? ParseDecision.MatchV3(
                    intentKey,
                    targetOverride,
                    resolver,
                    bypassNpcConsent)
                    : ParseDecision.Match(
                    intentKey,
                    targetOverride,
                    resolver,
                    bypassNpcConsent);
        }

        private IReadOnlyList<string> ResolveNaturalActionDescription(string text)
        {
            return _useV4
                ? SceneActionFrameworkV4.ResolveNaturalActionDescription(text)
                : _useV3
                    ? SceneActionFrameworkV3.ResolveNaturalActionDescription(text)
                    : SceneActionFrameworkV2.ResolveNaturalActionDescription(text);
        }

        private IReadOnlyList<string> ResolveNpcReplyDescription(string text)
        {
            return _useV4
                ? SceneActionFrameworkV4.ResolveNpcReplyDescription(text)
                : _useV3
                    ? SceneActionFrameworkV3.ResolveNpcReplyDescription(text)
                    : SceneActionFrameworkV2.ResolveNpcReplyDescription(text);
        }

        private bool ContainsUnsupportedActionReference(string text)
        {
            return _useV4
                ? SceneActionFrameworkV4.ContainsUnsupportedActionReference(text)
                : _useV3
                    ? SceneActionFrameworkV3.ContainsUnsupportedActionReference(text)
                    : SceneActionFrameworkV2.ContainsUnsupportedActionReference(text);
        }

        private bool HasSuppressedKnownActionReference(
            string text,
            IEnumerable<string> performedIntents)
        {
            return _useV4
                ? SceneActionFrameworkV4.HasSuppressedKnownActionReference(
                    text,
                    performedIntents)
                : _useV3
                    ? SceneActionFrameworkV3.HasSuppressedKnownActionReference(
                    text,
                    performedIntents)
                    : SceneActionFrameworkV2.HasSuppressedKnownActionReference(
                    text,
                    performedIntents);
        }

        private bool ContainsNaturalActionReference(string text)
        {
            return _useV4
                ? SceneActionFrameworkV4.ContainsNaturalActionReference(text)
                : _useV3
                    ? SceneActionFrameworkV3.ContainsNaturalActionReference(text)
                    : SceneActionFrameworkV2.ContainsNaturalActionReference(text);
        }

        private bool IsLogicalIntent(string intentKey)
        {
            return _useV4
                ? SceneActionFrameworkV4.IsLogicalIntent(intentKey)
                : _useV3
                    ? SceneActionFrameworkV3.IsLogicalIntent(intentKey)
                    : SceneActionFrameworkV2.IsLogicalIntent(intentKey);
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = text.Trim().Normalize(NormalizationForm.FormC);
            StringBuilder builder = new StringBuilder(normalized.Length);
            foreach (char character in normalized)
            {
                if (character >= 'A' && character <= 'Z')
                {
                    builder.Append((char)(character + 32));
                }
                else
                {
                    builder.Append(character);
                }
            }
            return builder.ToString();
        }

        private bool TryResolveForcedTarget(
            string forcedText,
            out AliasDefinition alias,
            out TargetMode target)
        {
            alias = null;
            target = TargetMode.FramedSelection;

            if (HasPlayerTargetPrefix(forcedText))
            {
                if (_catalog.TryGetForceAlias(forcedText, out alias))
                {
                    target = TargetMode.Player;
                    return true;
                }

                string actionText = Normalize(forcedText.Substring(1));
                if (!actionText.StartsWith("我", StringComparison.Ordinal) &&
                    _catalog.TryGetForceAlias(actionText, out alias))
                {
                    target = TargetMode.Player;
                    return true;
                }
                return false;
            }

            return _catalog.TryGetForceAlias(forcedText, out alias);
        }

        private static bool HasPlayerTargetPrefix(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.StartsWith("我", StringComparison.Ordinal) &&
                   !Array.Exists(
                       NonSelfPlayerPrefixes,
                       prefix => text.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static bool IsSingleLeadingStar(string text)
        {
            if (string.IsNullOrEmpty(text) || text[0] != '*')
            {
                return false;
            }
            if (text.Length == 1)
            {
                return false;
            }
            return text.IndexOf('*', 1) < 0;
        }
    }
}
