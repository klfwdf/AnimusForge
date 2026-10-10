using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class SceneActionContractEntryV3
    {
        internal SceneActionContractEntryV3(
            string intentKey,
            IntentKind kind,
            string actionKey,
            ActionMode? playbackMode,
            bool canOverlayKneel,
            string displayNameZhCn,
            string semanticDescriptionZhCn,
            IEnumerable<string> exactAliases,
            IEnumerable<string> performedCues,
            IEnumerable<string> referenceCues,
            IEnumerable<string> positiveExamples,
            IEnumerable<string> negativeExamples)
        {
            IntentKey = intentKey;
            Kind = kind;
            ActionKey = actionKey;
            PlaybackMode = playbackMode;
            CanOverlayKneel = canOverlayKneel;
            DisplayNameZhCn = displayNameZhCn;
            SemanticDescriptionZhCn = semanticDescriptionZhCn;
            ExactAliases = Freeze(exactAliases);
            PerformedCues = Freeze(performedCues);
            ReferenceCues = Freeze(referenceCues);
            PositiveExamples = Freeze(positiveExamples);
            NegativeExamples = Freeze(negativeExamples);
        }

        public string IntentKey { get; }
        public IntentKind Kind { get; }
        public string ActionKey { get; }
        public ActionMode? PlaybackMode { get; }
        public bool CanOverlayKneel { get; }
        public string DisplayNameZhCn { get; }
        public string SemanticDescriptionZhCn { get; }
        public IReadOnlyList<string> ExactAliases { get; }
        public IReadOnlyList<string> PerformedCues { get; }
        public IReadOnlyList<string> ReferenceCues { get; }
        public IReadOnlyList<string> PositiveExamples { get; }
        public IReadOnlyList<string> NegativeExamples { get; }

        private static IReadOnlyList<string> Freeze(IEnumerable<string> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray());
        }
    }

    /// <summary>
    /// V3 freezes the complete twenty-four-intent semantic contract. Local parsing
    /// and the AF classifier prompt consume this same table; raw engine action ids
    /// are deliberately absent from the semantic surface.
    /// </summary>
    public static class SceneActionFrameworkV3
    {
        public const int ContractVersion = 3;

        public const string Kneel = SceneActionFrameworkV2.Kneel;
        public const string StandUp = SceneActionFrameworkV2.StandUp;
        public const string Xihai = SceneActionFrameworkV2.Xihai;
        public const string Cheer = SceneActionFrameworkV2.Cheer;
        public const string Applaud = SceneActionFrameworkV2.Applaud;
        public const string Respect = SceneActionFrameworkV2.Respect;
        public const string Threat = SceneActionFrameworkV2.Threat;
        public const string Surrender = SceneActionFrameworkV2.Surrender;
        public const string Laugh = SceneActionFrameworkV2.Laugh;
        public const string Point = SceneActionFrameworkV2.Point;
        public const string Rage = SceneActionFrameworkV2.Rage;
        public const string Fear = SceneActionFrameworkV2.Fear;
        public const string Disappointed = SceneActionFrameworkV2.Disappointed;
        public const string Challenge = SceneActionFrameworkV2.Challenge;
        public const string Search = SceneActionFrameworkV2.Search;
        public const string Dance = SceneActionFrameworkV2.Dance;
        public const string Greet = "greet";
        public const string Agree = "agree";
        public const string Disagree = "disagree";
        public const string Unsure = "unsure";
        public const string Explain = "explain";
        public const string Promise = "promise";
        public const string CrossArms = "cross_arms";
        public const string DeepBow = "deep_bow";

        private static readonly ReadOnlyCollection<SceneActionContractEntryV3> Entries =
            Array.AsReadOnly(new[]
            {
                Legacy(Kneel, SceneActionCatalogText.Get("kneel", true).Name, SceneActionCatalogText.Get("kneel", true).Description,
                    new[] { "跪下", "kneel" },
                    new[]
                    {
                        "跪下", "下跪", "跪地", "跪在", "跪在地上",
                        "屈下一膝向其行礼", "屈下一膝", "屈膝跪下", "屈膝跪地", "单膝跪下",
                        "双膝跪下", "单膝着地", "双膝着地"
                    },
                    SceneActionCatalogText.Get("kneel", true).Positive,
                    SceneActionCatalogText.Get("kneel", true).Negative),
                Legacy(StandUp, SceneActionCatalogText.Get("stand_up", true).Name, SceneActionCatalogText.Get("stand_up", true).Description,
                    new[] { "站起来", "stand up" },
                    new[]
                    {
                        "站起", "起身", "站直", "站直起来", "从跪姿慢慢站直起来",
                        "借力从跪姿站直起来", "从跪着的姿势站起来", "由跪姿重新起立",
                        "从跪姿重新起立", "重新起立", "起立", "站直腰背"
                    },
                    SceneActionCatalogText.Get("stand_up", true).Positive,
                    SceneActionCatalogText.Get("stand_up", true).Negative),
                Legacy(Xihai, SceneActionCatalogText.Get("xihai", true).Name, SceneActionCatalogText.Get("xihai", true).Description,
                    new[] { "西海", "xihai" },
                    new[]
                    {
                        "纳粹礼", "希特勒礼", "右臂斜向上45度",
                        "右臂斜向上四十五度行礼",
                        "右臂斜向上四十五度",
                        "右臂抬到斜上方约四十五度，手指并拢，掌心向下行礼",
                        "raised his right arm at forty-five degrees in salute"
                    },
                    SceneActionCatalogText.Get("xihai", true).Positive,
                    SceneActionCatalogText.Get("xihai", true).Negative),
                Legacy(Cheer, SceneActionCatalogText.Get("cheer", true).Name, SceneActionCatalogText.Get("cheer", true).Description,
                    new[] { "欢呼", "cheer" },
                    new[]
                    {
                        "欢呼", "喝彩", "高声叫好", "挥舞双臂叫好", "挥舞双臂喝彩",
                        "兴奋地振臂呐喊", "全场欢声雷动，他挥舞双臂叫好",
                        "raised her fist and cheered loudly"
                    },
                    SceneActionCatalogText.Get("cheer", true).Positive,
                    SceneActionCatalogText.Get("cheer", true).Negative),
                Legacy(Applaud, SceneActionCatalogText.Get("applaud", true).Name, SceneActionCatalogText.Get("applaud", true).Description,
                    new[] { "鼓掌", "applaud" },
                    new[]
                    {
                        "鼓掌", "拍手", "拍掌", "啪啪拍了几下手", "拍了几下手",
                        "啪啪啪拍手", "双手连续拍击，掌声不断", "clapped her hands"
                    },
                    SceneActionCatalogText.Get("applaud", true).Positive,
                    SceneActionCatalogText.Get("applaud", true).Negative),
                Legacy(Respect, SceneActionCatalogText.Get("respect", true).Name, SceneActionCatalogText.Get("respect", true).Description,
                    new[] { "行礼", "salute" },
                    new[]
                    {
                        "行礼", "鞠躬",
                        "鞠了一躬", "鞠一躬", "致意", "bowed politely", "bowed in respect"
                    },
                    SceneActionCatalogText.Get("respect", true).Positive,
                    SceneActionCatalogText.Get("respect", true).Negative),
                Legacy(Threat, SceneActionCatalogText.Get("threat", true).Name, SceneActionCatalogText.Get("threat", true).Description,
                    new[] { "威胁", "threaten" },
                    new[]
                    {
                        "威胁", "恐吓", "挥拳示威", "握紧拳头在我面前晃动示威",
                        "握紧拳头晃动示威", "握拳示威", "攥紧拳头作势恐吓", "攥紧双拳在我面前挥拳示威",
                        "咬牙切齿地扬拳恐吓", "clenched his fist in threat",
                        "clenched fist threateningly", "made a threatening fist gesture",
                        "threateningly clenched his fist", "shook a fist threateningly"
                    },
                    SceneActionCatalogText.Get("threat", true).Positive,
                    SceneActionCatalogText.Get("threat", true).Negative),
                Legacy(Surrender, SceneActionCatalogText.Get("surrender", true).Name, SceneActionCatalogText.Get("surrender", true).Description,
                    new[] { "投降", "surrender" },
                    new[]
                    {
                        "投降", "认输", "放弃抵抗", "丢盔弃甲，双手抱头投降",
                        "把兵器掷在地上举手认输",
                        "dropped his weapon and raised both hands in surrender"
                    },
                    SceneActionCatalogText.Get("surrender", true).Positive,
                    SceneActionCatalogText.Get("surrender", true).Negative),
                Legacy(Laugh, SceneActionCatalogText.Get("laugh", true).Name, SceneActionCatalogText.Get("laugh", true).Description,
                    new[] { "大笑", "laugh" },
                    new[]
                    {
                        "大笑", "哈哈大笑", "笑出声", "笑得直不起腰", "笑弯了腰",
                        "笑到弯腰", "发出一阵笑声", "爆发出一阵笑声", "爆发出笑声",
                        "骤然笑了出来", "朗声笑了起来", "放声笑了起来",
                        "放声大笑前仰后合", "burst out laughing",
                        "burst into laughter", "let out a burst of laughter"
                    },
                    SceneActionCatalogText.Get("laugh", true).Positive,
                    SceneActionCatalogText.Get("laugh", true).Negative),
                Legacy(Point, SceneActionCatalogText.Get("point", true).Name, SceneActionCatalogText.Get("point", true).Description,
                    new[] { "指向", "point" },
                    new[]
                    {
                        "指向", "伸手指向", "用手指向", "抬起食指越过人群",
                        "明确朝左侧门口点去", "朝左侧门口点去", "越过人群点去",
                        "抬手朝北门一指", "朝北门一指", "抬手一指", "gestured toward the gate"
                    },
                    SceneActionCatalogText.Get("point", true).Positive,
                    SceneActionCatalogText.Get("point", true).Negative),
                Legacy(Rage, SceneActionCatalogText.Get("rage", true).Name, SceneActionCatalogText.Get("rage", true).Description,
                    new[] { "愤怒", "rage" },
                    new[]
                    {
                        "愤怒", "发怒", "勃然大怒", "怒不可遏，猛地一跺脚",
                        "怒不可遏地跺脚", "气得挥拳怒吼", "flew into a rage and roared"
                    },
                    SceneActionCatalogText.Get("rage", true).Positive,
                    SceneActionCatalogText.Get("rage", true).Negative),
                Legacy(Fear, SceneActionCatalogText.Get("fear", true).Name, SceneActionCatalogText.Get("fear", true).Description,
                    new[] { "害怕", "fear" },
                    new[]
                    {
                        "害怕", "惊恐", "慌张", "吓得脸色发白，浑身颤抖",
                        "慌忙后退，双手护住头", "有些惶恐地", "惶恐地低下头",
                        "诚惶诚恐地", "局促不安地", "畏缩着", "战战兢兢地",
                        "胆怯地向后缩", "惶惶不安地", "trembled in fear"
                    },
                    SceneActionCatalogText.Get("fear", true).Positive,
                    SceneActionCatalogText.Get("fear", true).Negative),
                Legacy(Disappointed, SceneActionCatalogText.Get("disappointed", true).Name, SceneActionCatalogText.Get("disappointed", true).Description,
                    new[] { "失望", "disappointed" },
                    new[]
                    {
                        "失望", "沮丧", "摇头叹息", "摇了摇头叹息", "满脸失落，默默垂下头",
                        "垂头丧气地叹了口气", "sighed in disappointment"
                    },
                    SceneActionCatalogText.Get("disappointed", true).Positive,
                    SceneActionCatalogText.Get("disappointed", true).Negative),
                Legacy(Challenge, SceneActionCatalogText.Get("challenge", true).Name, SceneActionCatalogText.Get("challenge", true).Description,
                    new[] { "挑衅", "challenge" },
                    new[] { "挑衅", "叫阵", "勾手挑战", "beckoned challengingly" },
                    SceneActionCatalogText.Get("challenge", true).Positive,
                    SceneActionCatalogText.Get("challenge", true).Negative),
                Legacy(Search, SceneActionCatalogText.Get("search", true).Name, SceneActionCatalogText.Get("search", true).Description,
                    new[] { "环顾", "search" },
                    new[]
                    {
                        "环顾", "张望", "扫视四周", "警惕地左顾右盼", "左顾右盼",
                        "环视四周寻找踪迹", "scanned the room"
                    },
                    SceneActionCatalogText.Get("search", true).Positive,
                    SceneActionCatalogText.Get("search", true).Negative),
                Legacy(Dance, SceneActionCatalogText.Get("dance", true).Name, SceneActionCatalogText.Get("dance", true).Description,
                    new[] { "跳舞", "dance" },
                    new[]
                    {
                        "跳舞", "起舞", "随节奏舞动", "随着鼓点扭动起来",
                        "随着鼓点扭动身体", "转身跳起舞来", "started dancing"
                    },
                    SceneActionCatalogText.Get("dance", true).Positive,
                    SceneActionCatalogText.Get("dance", true).Negative),
                NewAction(Greet, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("greet", true).Name,
                    SceneActionCatalogText.Get("greet", true).Description,
                    new[] { "问候", "打招呼", "greet" },
                    new[] { "挥了挥手", "挥挥手", "招了招手", "挥手问候", "挥了挥手打招呼", "抬手打招呼", "抬手向来人问候", "抬手向来人打招呼", "招手问候", "招了招手致意", "向他挥手致意", "挥手致意", "微笑着挥手致意", "抬手挥了挥向我问好", "waved in greeting", "greeted with a wave", "waved hello", "挥手打招呼", "打招呼", "左右挥了挥", "抬起手左右挥了挥", "主动打了个招呼" },
                    new[] { "问候", "打招呼", "挥手", "招手", "你好", "hello", "greet", "greeting", "waved" },
                    SceneActionCatalogText.Get("greet", true).Positive,
                    SceneActionCatalogText.Get("greet", true).Negative),
                NewAction(Agree, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("agree", true).Name,
                    SceneActionCatalogText.Get("agree", true).Description,
                    new[] { "点头同意", "点头赞成", "agree" },
                    new[] { "点了点头", "点点头", "点头同意", "点头表示同意", "点头表示赞成", "点了点头表示赞成", "点头应允", "点头答应", "点头认可", "颔首赞同", "郑重地点头表示同意", "nodded in agreement", "nodded firmly in agreement", "nodded yes", "nodded approvingly", "郑重地点了点头", "郑重地点了点头表示同意", "郑重地点了点头表示赞成" },
                    new[] { "同意", "答应", "赞成", "认可", "点头", "agree", "agreed", "agreement" },
                    SceneActionCatalogText.Get("agree", true).Positive,
                    SceneActionCatalogText.Get("agree", true).Negative),
                NewAction(Disagree, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("disagree", true).Name,
                    SceneActionCatalogText.Get("disagree", true).Description,
                    new[] { "摇头否定", "摇头反对", "disagree" },
                    new[] { "摇了摇头", "摇摇头", "摇头否定", "摇头表示反对", "摇头表示不同意", "摇了摇头拒绝", "摇头不同意", "摇头表示不赞成", "摇头回绝", "用力摇头表示否定", "摇头拒绝", "shook his head in disagreement", "shook her head no", "shook his head to disagree", "shook his head to show clear disagreement", "摇了几下头", "摆手明确表示反对", "连续摇头表示反对" },
                    new[] { "不同意", "反对", "拒绝", "否定", "摇头", "disagree", "disagreed", "disagreement" },
                    SceneActionCatalogText.Get("disagree", true).Positive,
                    SceneActionCatalogText.Get("disagree", true).Negative),
                NewAction(Unsure, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("unsure", true).Name,
                    SceneActionCatalogText.Get("unsure", true).Description,
                    new[] { "摊手", "摊手表示不知道", "unsure", "shrug" },
                    new[] { "摊了摊手", "耸耸肩", "摊手表示不知道", "摊开双手表示不确定", "耸了耸肩", "迟疑地摊手", "困惑地摊开手", "困惑地摊开双手", "无奈地耸肩", "耸肩摊手，一脸茫然", "犹豫地摊开双手", "shrugged uncertainly", "shrugged, unsure", "shrugged and opened his hands uncertainly", "spread his hands in uncertainty", "摊开双手耸了耸肩", "显得完全拿不准", "拿不准" },
                    new[] { "摊手", "耸肩", "不确定手势", "犹豫动作", "uncertain", "uncertainty" },
                    SceneActionCatalogText.Get("unsure", true).Positive,
                    SceneActionCatalogText.Get("unsure", true).Negative),
                NewAction(Explain, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("explain", true).Name,
                    SceneActionCatalogText.Get("explain", true).Description,
                    new[] { "比划解释", "手势解释", "explain" },
                    new[] { "比划着解释", "用手势解释", "摊手说明", "摊开双手作出说明", "一边比划一边解释", "一边比划一边说明", "挥动双手作出解释", "用手比划着讲解", "边说边用双手比划解释", "摊开掌心详细说明", "gestured while explaining", "gestured back and forth while explaining", "gestured back and forth while explaining the plan", "explained with his hands", "explained with a hand gesture", "摊开手掌", "来回比划着说明", "比划着说明", "一边摊开手掌一边来回比划着说明" },
                    new[] { "解释", "说明", "讲解", "比划", "手势", "explaining", "explanation" },
                    SceneActionCatalogText.Get("explain", true).Positive,
                    SceneActionCatalogText.Get("explain", true).Negative),
                NewAction(Promise, ActionMode.OneShot, true, SceneActionCatalogText.Get("promise", true).Name,
                    SceneActionCatalogText.Get("promise", true).Description,
                    new[] { "举手起誓", "郑重起誓", "promise" },
                    new[] { "举手起誓", "抬手立誓", "抬起右手作出承诺", "郑重地作出承诺手势", "举起右手发誓", "做出起誓动作", "庄重地立下誓言", "抬起右手发誓保证", "举手立下誓言", "raised his hand to swear", "made an oath gesture", "raised his hand and swore", "raised his right hand and swore", "raised her right hand and swore", "举起右手郑重起誓", "拍了拍胸口作保证", "作保证" },
                    new[] { "承诺", "保证", "发誓", "起誓", "立誓", "promise", "promised", "swore" },
                    SceneActionCatalogText.Get("promise", true).Positive,
                    SceneActionCatalogText.Get("promise", true).Negative),
                NewAction(CrossArms, ActionMode.OneShot, true, SceneActionCatalogText.Get("cross_arms", true).Name,
                    SceneActionCatalogText.Get("cross_arms", true).Description,
                    new[] { "抱臂", "双臂交叉", "cross arms", "crossarms" },
                    new[] { "抱起双臂", "抱臂", "抱臂而立", "双臂抱胸而立", "双臂交叉在胸前", "把手臂交叉在胸前", "胳膊交叉在胸前", "环抱双臂", "交叉双臂站着", "crossed his arms", "folded her arms", "folded his arms", "收回双手", "交叉抱在胸前", "两条手臂交叉抱在胸前" },
                    new[] { "抱臂", "双臂交叉", "交叉手臂", "cross arms", "crossed arms" },
                    SceneActionCatalogText.Get("cross_arms", true).Positive,
                    SceneActionCatalogText.Get("cross_arms", true).Negative),
                NewAction(DeepBow, ActionMode.OneShot, false, SceneActionCatalogText.Get("deep_bow", true).Name,
                    SceneActionCatalogText.Get("deep_bow", true).Description,
                    new[] { "深鞠躬", "深深鞠躬", "deep bow", "deepbow" },
                    new[] { "深鞠躬", "深深鞠躬", "深深鞠了一躬", "深深地鞠了一躬", "弯下腰深深鞠了一躬", "弯腰深鞠一躬", "躬身到底", "躬身到底郑重致礼", "九十度鞠躬", "深深弯腰致礼", "深深弯腰鞠躬到底", "深深弯下腰行大礼", "bowed deeply", "made a deep bow", "made a profound bow", "深深鞠躬到底", "弯腰近乎九十度", "弯腰深鞠到底" },
                    new[] { "深鞠躬", "深深鞠躬", "九十度鞠躬", "deep bow" },
                    SceneActionCatalogText.Get("deep_bow", true).Positive,
                    SceneActionCatalogText.Get("deep_bow", true).Negative)
            });

        private static readonly HashSet<string> NewIntentKeys = new HashSet<string>(
            Entries.Skip(16).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly HashSet<string> KneelOverlayKeys = new HashSet<string>(
            Entries.Where(entry => entry.CanOverlayKneel).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly string[] UnsupportedActionCues = SceneActionVocabulary.List("SceneActionFrameworkV3.UnsupportedActionCues");

        public static IReadOnlyList<SceneActionContractEntryV3> LogicalActions => Entries;

        public static bool IsLogicalIntent(string intentKey)
        {
            return Entries.Any(entry => string.Equals(
                entry.IntentKey,
                intentKey,
                StringComparison.Ordinal));
        }

        public static bool CanOverlayKneel(string intentKey)
        {
            return KneelOverlayKeys.Contains(intentKey ?? string.Empty);
        }

        public static IReadOnlyList<string> ResolveNpcReplyDescription(string text)
        {
            return ResolveNaturalActionDescription(text);
        }

        public static IReadOnlyList<string> ResolveNaturalActionDescription(string text)
        {
            string normalized = CommandParser.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
            {
                return Array.Empty<string>();
            }

            List<string> resolved = new List<string>(
                SceneActionFrameworkV2.ResolveNaturalActionDescription(normalized));
            foreach (SceneActionContractEntryV3 entry in Entries.Take(16))
            {
                SceneActionContractEntryV2 legacy = SceneActionFrameworkV2.LogicalActions
                    .Single(source => string.Equals(
                        source.IntentKey,
                        entry.IntentKey,
                        StringComparison.Ordinal));
                HashSet<string> v2Cues = new HashSet<string>(
                    legacy.NaturalLanguageAliases.Select(CommandParser.Normalize),
                    StringComparer.Ordinal);
                if (entry.PerformedCues
                    .Select(CommandParser.Normalize)
                    .Where(cue => !v2Cues.Contains(cue))
                    .Any(cue => SceneActionFrameworkV1.ContainsPerformedCue(
                        normalized,
                        cue)))
                {
                    resolved.Add(entry.IntentKey);
                }
            }
            foreach (SceneActionContractEntryV3 entry in Entries.Skip(16))
            {
                if (entry.PerformedCues.Any(cue =>
                        SceneActionFrameworkV1.ContainsPerformedCue(
                            normalized,
                            CommandParser.Normalize(cue))))
                {
                    resolved.Add(entry.IntentKey);
                }
            }

            if (resolved.Contains(DeepBow, StringComparer.Ordinal) &&
                resolved.Contains(Respect, StringComparer.Ordinal) &&
                !ContainsIndependentRespectOutsideDeepBow(normalized))
            {
                resolved.RemoveAll(key => string.Equals(key, Respect, StringComparison.Ordinal));
            }
            if (resolved.Contains(Xihai, StringComparer.Ordinal) &&
                resolved.Contains(Respect, StringComparer.Ordinal) &&
                !ContainsIndependentRespectOutsideXihai(normalized))
            {
                resolved.RemoveAll(key => string.Equals(key, Respect, StringComparison.Ordinal));
            }

            List<CueSpan> performedMatches = FindCueSpans(
                normalized,
                entry => entry.PerformedCues);
            HashSet<string> matchedKeys = new HashSet<string>(
                performedMatches.Select(match => match.IntentKey),
                StringComparer.Ordinal);
            HashSet<string> preferredKeys = new HashSet<string>(
                SelectLongestCueSpans(performedMatches)
                    .Select(match => match.IntentKey),
                StringComparer.Ordinal);
            resolved.RemoveAll(key =>
                matchedKeys.Contains(key) && !preferredKeys.Contains(key));

            return resolved.Distinct(StringComparer.Ordinal).ToArray();
        }

        public static IReadOnlyList<string> ResolveNaturalActionReferences(string text)
        {
            string normalized = CommandParser.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
            {
                return Array.Empty<string>();
            }
            return SelectLongestCueSpans(FindCueSpans(
                    normalized,
                    entry => entry.ReferenceCues.Concat(entry.PerformedCues)))
                .OrderBy(match => match.Start)
                .ThenByDescending(match => match.Length)
                .Select(match => match.IntentKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static List<CueSpan> FindCueSpans(
            string normalized,
            Func<SceneActionContractEntryV3, IEnumerable<string>> selectCues)
        {
            List<CueSpan> matches = new List<CueSpan>();
            foreach (SceneActionContractEntryV3 entry in Entries)
            {
                foreach (string cue in selectCues(entry)
                             .Select(CommandParser.Normalize)
                             .Where(cue => !string.IsNullOrEmpty(cue))
                             .Distinct(StringComparer.Ordinal))
                {
                    int searchFrom = 0;
                    while (searchFrom <= normalized.Length - cue.Length)
                    {
                        int start = SceneActionFrameworkV1.IndexOfCue(
                            normalized,
                            cue,
                            searchFrom);
                        if (start < 0)
                        {
                            break;
                        }
                        if (string.Equals(entry.IntentKey, Xihai, StringComparison.Ordinal) &&
                            SceneActionFrameworkV1.IsXihaiEquipmentMention(
                                normalized,
                                start,
                                cue.Length))
                        {
                            searchFrom = start + Math.Max(1, cue.Length);
                            continue;
                        }
                        matches.Add(new CueSpan(entry.IntentKey, start, cue.Length));
                        searchFrom = start + Math.Max(1, cue.Length);
                    }
                }
            }
            return matches;
        }

        private static IEnumerable<CueSpan> SelectLongestCueSpans(
            IReadOnlyList<CueSpan> matches)
        {
            return matches.Where(match => !matches.Any(longer =>
                !string.Equals(
                    longer.IntentKey,
                    match.IntentKey,
                    StringComparison.Ordinal) &&
                longer.Length > match.Length &&
                longer.Start <= match.Start &&
                longer.End >= match.End));
        }

        public static bool ContainsNaturalActionReference(string text)
        {
            return ResolveNaturalActionReferences(text).Count > 0 ||
                   SceneActionFrameworkV2.ContainsNaturalActionReference(text);
        }

        public static bool ContainsUnsupportedActionReference(string text)
        {
            string normalized = CommandParser.Normalize(text);
            bool danceOwnsTurn = ResolveNaturalActionDescription(normalized)
                .Contains(Dance, StringComparer.Ordinal);
            return UnsupportedActionCues.Any(cue =>
                string.Equals(cue, "转身", StringComparison.Ordinal) && danceOwnsTurn
                    ? false
                    : SceneActionFrameworkV1.IndexOfCue(
                        normalized,
                        CommandParser.Normalize(cue)) >= 0);
        }

        public static bool HasSuppressedKnownActionReference(
            string text,
            IEnumerable<string> performedIntents)
        {
            HashSet<string> performed = new HashSet<string>(
                performedIntents ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            HashSet<string> legacyCoverage = new HashSet<string>(performed, StringComparer.Ordinal);
            if (performed.Contains(DeepBow))
            {
                legacyCoverage.Add(Respect);
            }
            if (performed.Contains(Cheer))
            {
                // V1/V2 use the broad "挥舞双臂" cue for rage; a longer
                // cheer gesture owns that overlap when it is actually performed.
                legacyCoverage.Add(Rage);
            }
            if (performed.Contains(Agree) || performed.Contains(Greet))
            {
                // Generic "颔首/致意" is a respect reference covered by the
                // more specific performed gesture.
                legacyCoverage.Add(Respect);
            }
            if (SceneActionFrameworkV2.HasSuppressedKnownActionReference(
                text,
                legacyCoverage))
            {
                return true;
            }

            if (performed.Contains(Disappointed))
            {
                legacyCoverage.Add(Disagree);
            }

            return ResolveNaturalActionReferences(text)
                .Where(NewIntentKeys.Contains)
                .Any(key => !performed.Contains(key) &&
                            HasUnsuppressedReference(text, key));
        }

        internal static bool HasUnsuppressedReference(
            string text,
            string intentKey)
        {
            string normalized = CommandParser.Normalize(text);
            SceneActionContractEntryV3 entry = Entries.Single(value =>
                string.Equals(value.IntentKey, intentKey, StringComparison.Ordinal));
            foreach (string cue in entry.ReferenceCues.Concat(entry.PerformedCues))
            {
                string normalizedCue = CommandParser.Normalize(cue);
                if (string.IsNullOrEmpty(normalizedCue))
                {
                    continue;
                }
                int searchFrom = 0;
                while (searchFrom <= normalized.Length - normalizedCue.Length)
                {
                    int index = SceneActionFrameworkV1.IndexOfCue(
                        normalized,
                        normalizedCue,
                        searchFrom);
                    if (index < 0)
                    {
                        break;
                    }
                    if (IsReferenceCueContextSuppressed(
                        normalized,
                        intentKey,
                        normalizedCue,
                        index) ||
                        SceneActionFrameworkV1.ContainsPerformedCue(
                            normalized,
                            normalizedCue))
                    {
                        if (!IsReferenceCueContextSuppressed(
                            normalized,
                            intentKey,
                            normalizedCue,
                            index))
                        {
                            return true;
                        }
                    }
                    searchFrom = index + Math.Max(1, normalizedCue.Length);
                }
            }
            return false;
        }

        private static bool IsReferenceCueContextSuppressed(
            string normalized,
            string intentKey,
            string cue,
            int cueIndex)
        {
            if (!string.Equals(intentKey, Disagree, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.Equals(cue, "拒绝", StringComparison.Ordinal))
            {
                string after = normalized.Substring(
                    cueIndex + cue.Length,
                    Math.Min(
                        normalized.Length - cueIndex - cue.Length,
                        8));
                return after.StartsWith("投降", StringComparison.Ordinal) ||
                       after.StartsWith("跪下", StringComparison.Ordinal) ||
                       after.StartsWith("行礼", StringComparison.Ordinal) ||
                       after.StartsWith("起誓", StringComparison.Ordinal) ||
                       after.StartsWith("承诺", StringComparison.Ordinal);
            }
            return false;
        }
        public static string BuildClassifierDefinitionBlock(IEnumerable<string> allowedIntentKeys)
            => BuildClassifierDefinitionBlock(allowedIntentKeys, true);

        public static string BuildClassifierDefinitionBlock(IEnumerable<string> allowedIntentKeys, bool useChinese)
        {
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            return string.Join("\n", Entries
                .Where(entry => allowed.Contains(entry.IntentKey))
                .Select(entry =>
                    SceneActionCatalogText.FormatEntry(entry.IntentKey, useChinese, true)));
        }

        public static void ValidateCatalog(SceneActionCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            SceneActionFrameworkV1.ValidateCatalog(catalog);
            HashSet<string> expectedIntents = new HashSet<string>(
                Entries.Select(entry => entry.IntentKey),
                StringComparer.Ordinal);
            HashSet<string> expectedActions = new HashSet<string>(
                Entries.Where(entry => entry.Kind == IntentKind.PlayAction)
                    .Select(entry => entry.ActionKey),
                StringComparer.Ordinal);
            if (!expectedIntents.SetEquals(catalog.Intents.Keys) ||
                !expectedActions.SetEquals(catalog.Actions.Keys))
            {
                throw new InvalidOperationException(
                    "Catalog does not match the twenty-four-action SceneActionFrameworkV3 contract.");
            }
            foreach (SceneActionContractEntryV3 entry in Entries)
            {
                if (!catalog.TryGetIntent(entry.IntentKey, out IntentDefinition intent) ||
                    intent.Kind != entry.Kind ||
                    !string.Equals(intent.ActionKey, entry.ActionKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Catalog intent drifted from SceneActionFrameworkV3: " + entry.IntentKey);
                }
                if (entry.Kind == IntentKind.PlayAction &&
                    (!catalog.Actions.TryGetValue(entry.ActionKey, out ActionDefinition action) ||
                     action.Mode != entry.PlaybackMode))
                {
                    throw new InvalidOperationException(
                        "Catalog action mode drifted from SceneActionFrameworkV3: " + entry.ActionKey);
                }
                if (string.IsNullOrWhiteSpace(entry.DisplayNameZhCn) ||
                    string.IsNullOrWhiteSpace(entry.SemanticDescriptionZhCn) ||
                    entry.ExactAliases.Count == 0 ||
                    entry.PerformedCues.Count == 0 ||
                    entry.PositiveExamples.Count < 2 ||
                    entry.NegativeExamples.Count < 2)
                {
                    throw new InvalidOperationException(
                        "V3 semantic definition is incomplete: " + entry.IntentKey);
                }
            }
        }

        private static bool ContainsIndependentRespectOutsideDeepBow(string normalized)
        {
            string remainder = normalized;
            SceneActionContractEntryV3 deepBow = Entries.Single(entry =>
                string.Equals(entry.IntentKey, DeepBow, StringComparison.Ordinal));
            foreach (string cue in deepBow.PerformedCues
                         .Select(CommandParser.Normalize)
                         .OrderByDescending(value => value.Length))
            {
                remainder = remainder.Replace(cue, string.Empty);
            }
            return SceneActionFrameworkV2.ResolveNaturalActionDescription(remainder)
                .Contains(Respect, StringComparer.Ordinal);
        }

        private static bool ContainsIndependentRespectOutsideXihai(string normalized)
        {
            SceneActionContractEntryV3 respect = Entries.Single(entry =>
                string.Equals(entry.IntentKey, Respect, StringComparison.Ordinal));
            foreach (string cue in respect.PerformedCues
                         .Select(CommandParser.Normalize)
                         .OrderByDescending(value => value.Length))
            {
                int searchFrom = 0;
                while (searchFrom <= normalized.Length - cue.Length)
                {
                    int index = SceneActionFrameworkV1.IndexOfCue(
                        normalized,
                        cue,
                        searchFrom);
                    if (index < 0)
                    {
                        break;
                    }
                    if (!IsXihaiClause(normalized, index) &&
                        !IsNegatedAlternativeClause(normalized, index))
                    {
                        return true;
                    }
                    searchFrom = index + Math.Max(1, cue.Length);
                }
            }
            return false;
        }

        private static bool IsNegatedAlternativeClause(
            string normalized,
            int cueIndex)
        {
            int start = cueIndex;
            while (start > 0 && !IsClauseSeparator(normalized[start - 1]))
            {
                start--;
            }

            string beforeCue = normalized.Substring(start, cueIndex - start);
            return beforeCue.IndexOf("而非", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("而不是", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("并非", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("不是", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("rather than", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("ratherthan", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("not ordinary", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("notordinary", StringComparison.Ordinal) >= 0;
        }

        private static bool IsXihaiClause(string normalized, int cueIndex)
        {
            int start = cueIndex;
            while (start > 0 && !IsClauseSeparator(normalized[start - 1]))
            {
                start--;
            }
            int end = cueIndex;
            while (end < normalized.Length && !IsClauseSeparator(normalized[end]))
            {
                end++;
            }

            string clause = normalized.Substring(start, end - start);
            if (clause.IndexOf("西海", StringComparison.Ordinal) >= 0 ||
                clause.IndexOf("纳粹", StringComparison.Ordinal) >= 0 ||
                clause.IndexOf("希特勒", StringComparison.Ordinal) >= 0 ||
                clause.IndexOf("德意志", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            bool hasRightArm = clause.IndexOf("右臂", StringComparison.Ordinal) >= 0 ||
                               clause.IndexOf("右手", StringComparison.Ordinal) >= 0 ||
                               clause.IndexOf("手臂", StringComparison.Ordinal) >= 0;
            bool hasUpwardGeometry = clause.IndexOf("斜上", StringComparison.Ordinal) >= 0 ||
                                     clause.IndexOf("向前上方", StringComparison.Ordinal) >= 0 ||
                                     clause.IndexOf("45", StringComparison.Ordinal) >= 0 ||
                                     clause.IndexOf("四十五", StringComparison.Ordinal) >= 0;
            return hasRightArm && hasUpwardGeometry;
        }

        private static bool IsClauseSeparator(char character)
        {
            return character == '，' || character == ',' || character == '；' ||
                   character == ';' || character == '。' || character == '.' ||
                   character == '！' || character == '!' || character == '？' ||
                   character == '?';
        }

        private sealed class CueSpan
        {
            public CueSpan(string intentKey, int start, int length)
            {
                IntentKey = intentKey;
                Start = start;
                Length = length;
            }

            public string IntentKey { get; }
            public int Start { get; }
            public int Length { get; }
            public int End => Start + Length;
        }

        private static SceneActionContractEntryV3 Legacy(
            string intentKey,
            string displayName,
            string description,
            IEnumerable<string> exactAliases,
            IEnumerable<string> referenceCues,
            IEnumerable<string> positiveExamples,
            IEnumerable<string> negativeExamples)
        {
            SceneActionContractEntryV2 source = SceneActionFrameworkV2.LogicalActions
                .Single(entry => string.Equals(
                    entry.IntentKey,
                    intentKey,
                    StringComparison.Ordinal));
            return new SceneActionContractEntryV3(
                source.IntentKey,
                source.Kind,
                source.ActionKey,
                source.PlaybackMode,
                source.CanOverlayKneel,
                displayName,
                description,
                exactAliases,
                source.NaturalLanguageAliases.Concat(
                    referenceCues ?? Enumerable.Empty<string>()),
                source.NaturalLanguageAliases.Concat(referenceCues ?? Enumerable.Empty<string>()),
                positiveExamples,
                negativeExamples);
        }

        private static SceneActionContractEntryV3 NewAction(
            string key,
            ActionMode mode,
            bool canOverlayKneel,
            string displayName,
            string description,
            IEnumerable<string> exactAliases,
            IEnumerable<string> performedCues,
            IEnumerable<string> referenceCues,
            IEnumerable<string> positiveExamples,
            IEnumerable<string> negativeExamples)
        {
            return new SceneActionContractEntryV3(
                key,
                IntentKind.PlayAction,
                key,
                mode,
                canOverlayKneel,
                displayName,
                description,
                exactAliases,
                performedCues,
                referenceCues,
                positiveExamples,
                negativeExamples);
        }
    }
}
