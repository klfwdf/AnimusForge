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
                Legacy(Kneel, "跪下", "身体实际屈膝跪地、跪伏或跪拜。",
                    new[] { "跪下", "kneel" },
                    new[]
                    {
                        "跪下", "下跪", "跪地", "跪在", "跪在地上",
                        "屈下一膝向其行礼", "屈下一膝", "屈膝跪下", "屈膝跪地", "单膝跪下",
                        "双膝跪下", "单膝着地", "双膝着地"
                    },
                    new[] { "他缓缓屈膝跪下。", "她双膝着地跪伏下来。" },
                    new[] { "他拒绝跪下。", "如果跪下会怎样？" }),
                Legacy(StandUp, "站起", "从本模块拥有的跪姿中实际起身或站直。",
                    new[] { "站起来", "stand up" },
                    new[]
                    {
                        "站起", "起身", "站直", "站直起来", "从跪姿慢慢站直起来",
                        "借力从跪姿站直起来", "从跪着的姿势站起来", "由跪姿重新起立",
                        "从跪姿重新起立", "重新起立", "起立", "站直腰背"
                    },
                    new[] { "他撑地站起身。", "她从跪姿中站了起来。" },
                    new[] { "他想站起来。", "她叫别人起身。" }),
                Legacy(Xihai, "西海动作", "纳粹礼/希特勒礼，或右臂向前斜上方伸直约45度的固定抬手礼。",
                    new[] { "西海", "xihai" },
                    new[]
                    {
                        "纳粹礼", "希特勒礼", "右臂斜向上45度",
                        "右臂斜向上四十五度行礼",
                        "右臂斜向上四十五度",
                        "右臂抬到斜上方约四十五度，手指并拢，掌心向下行礼",
                        "raised his right arm at forty-five degrees in salute"
                    },
                    new[] { "他将右臂向前斜上方伸直约四十五度。", "她明确行了一个纳粹式举手礼。" },
                    new[] { "他普通地抬手致意。", "她挥手向众人问好。" }),
                Legacy(Cheer, "欢呼", "实际喝彩、振臂高呼或高声叫好。",
                    new[] { "欢呼", "cheer" },
                    new[]
                    {
                        "欢呼", "喝彩", "高声叫好", "挥舞双臂叫好", "挥舞双臂喝彩",
                        "兴奋地振臂呐喊", "全场欢声雷动，他挥舞双臂叫好",
                        "raised her fist and cheered loudly"
                    },
                    new[] { "他振臂高声欢呼。", "她兴奋地喝彩起来。" },
                    new[] { "他没有欢呼。", "他谈到了昨夜的欢呼。" }),
                Legacy(Applaud, "鼓掌", "双手实际拍击以鼓掌、拍手或拍掌。",
                    new[] { "鼓掌", "applaud" },
                    new[]
                    {
                        "鼓掌", "拍手", "拍掌", "啪啪拍了几下手", "拍了几下手",
                        "啪啪啪拍手", "双手连续拍击，掌声不断", "clapped her hands"
                    },
                    new[] { "他笑着鼓起掌来。", "她连续拍了拍手。" },
                    new[] { "他拒绝鼓掌。", "掌声这个词是什么意思？" }),
                Legacy(Respect, "普通行礼", "礼节性回礼、欠身、鞠躬、作揖或普通致意；不含抬手敬礼、军礼、西海特征或明确深鞠躬。",
                    new[] { "行礼", "salute" },
                    new[]
                    {
                        "行礼", "鞠躬",
                        "鞠了一躬", "鞠一躬", "致意", "bowed politely", "bowed in respect"
                    },
                    new[]
                    {
                        "他抬手向对方致意。", "她礼貌地鞠了一躬。", "bowed politely",
                        "bowed in respect"
                    },
                    new[]
                    {
                        "他抬手行了一个军礼。", "她站直后抬手敬礼。",
                        "他把右臂斜上伸直四十五度。", "她深深弯腰鞠躬到底。"
                    }),
                Legacy(Threat, "威胁", "实际作出恐吓、挥拳、握拳或恫吓示威动作。",
                    new[] { "威胁", "threaten" },
                    new[]
                    {
                        "威胁", "恐吓", "挥拳示威", "握紧拳头在我面前晃动示威",
                        "握紧拳头晃动示威", "握拳示威", "攥紧拳头作势恐吓", "攥紧双拳在我面前挥拳示威",
                        "咬牙切齿地扬拳恐吓", "clenched his fist in threat",
                        "clenched fist threateningly", "made a threatening fist gesture",
                        "threateningly clenched his fist", "shook a fist threateningly"
                    },
                    new[] { "他握紧拳头作势威胁。", "她挥拳向对手示威。" },
                    new[] { "他只是口头谈论威胁。", "他没有作出恐吓动作。" }),
                Legacy(Surrender, "投降", "实际举手、认输、缴械或放弃抵抗。",
                    new[] { "投降", "surrender" },
                    new[]
                    {
                        "投降", "认输", "放弃抵抗", "丢盔弃甲，双手抱头投降",
                        "把兵器掷在地上举手认输",
                        "dropped his weapon and raised both hands in surrender"
                    },
                    new[] { "他高举双手表示投降。", "她放弃抵抗认输了。" },
                    new[] { "他拒绝投降。", "她命令敌人投降。" }),
                Legacy(Laugh, "大笑", "实际发出明显笑声，或放声、开怀、仰头大笑。",
                    new[] { "大笑", "laugh" },
                    new[]
                    {
                        "大笑", "哈哈大笑", "笑出声", "笑得直不起腰", "笑弯了腰",
                        "笑到弯腰", "发出一阵笑声", "爆发出一阵笑声", "爆发出笑声",
                        "骤然笑了出来", "朗声笑了起来", "放声笑了起来",
                        "放声大笑前仰后合", "burst out laughing",
                        "burst into laughter", "let out a burst of laughter"
                    },
                    new[] { "他仰头放声大笑。", "她骤然发出一阵笑声。" },
                    new[] { "他强忍着没有笑。", "他说出‘大笑’二字。" }),
                Legacy(Point, "指向", "实际伸手或抬手指向某人、某物或某处。",
                    new[] { "指向", "point" },
                    new[]
                    {
                        "指向", "伸手指向", "用手指向", "抬起食指越过人群",
                        "明确朝左侧门口点去", "朝左侧门口点去", "越过人群点去",
                        "抬手朝北门一指", "朝北门一指", "抬手一指", "gestured toward the gate"
                    },
                    new[] { "他伸手指向城门旁边。", "她抬手朝远处一指。" },
                    new[] { "他没有指出方向。", "她要求别人指向门口。" }),
                Legacy(Rage, "愤怒", "实际发怒、暴怒、怒吼或愤怒挥动手臂。",
                    new[] { "愤怒", "rage" },
                    new[]
                    {
                        "愤怒", "发怒", "勃然大怒", "怒不可遏，猛地一跺脚",
                        "怒不可遏地跺脚", "气得挥拳怒吼", "flew into a rage and roared"
                    },
                    new[] { "他勃然大怒并怒吼。", "她愤怒地挥动双臂。" },
                    new[] { "他并未发怒。", "如果愤怒会发生什么？" }),
                Legacy(Fear, "害怕", "实际表现出惊恐、害怕、慌张或瑟瑟发抖。",
                    new[] { "害怕", "fear" },
                    new[]
                    {
                        "害怕", "惊恐", "慌张", "吓得脸色发白，浑身颤抖",
                        "慌忙后退，双手护住头", "有些惶恐地", "惶恐地低下头",
                        "诚惶诚恐地", "局促不安地", "畏缩着", "战战兢兢地",
                        "胆怯地向后缩", "惶惶不安地", "trembled in fear"
                    },
                    new[] { "他惊恐地瑟瑟发抖。", "她害怕得连连后退摆手。" },
                    new[] { "他声称自己并不害怕。", "她只是在解释恐惧。" }),
                Legacy(Disappointed, "失望", "实际垂头丧气、沮丧、摇头叹息或无奈叹气。",
                    new[] { "失望", "disappointed" },
                    new[]
                    {
                        "失望", "沮丧", "摇头叹息", "摇了摇头叹息", "满脸失落，默默垂下头",
                        "垂头丧气地叹了口气", "sighed in disappointment"
                    },
                    new[] { "他失望地摇头叹息。", "她垂头丧气地低下头。" },
                    new[] { "他摇头表示不同意。", "她没有表现出失望。" }),
                Legacy(Challenge, "挑衅", "实际勾手、招手叫阵或作出挑战姿态。",
                    new[] { "挑衅", "challenge" },
                    new[] { "挑衅", "叫阵", "勾手挑战", "beckoned challengingly" },
                    new[] { "他勾手挑衅对手上前。", "她摆出叫阵挑战的姿态。" },
                    new[] { "他友好地招手问候。", "他拒绝接受挑战。" }),
                Legacy(Search, "环顾", "实际环顾、张望、扫视四周或搜寻目标。",
                    new[] { "环顾", "search" },
                    new[]
                    {
                        "环顾", "张望", "扫视四周", "警惕地左顾右盼", "左顾右盼",
                        "环视四周寻找踪迹", "scanned the room"
                    },
                    new[] { "他警惕地扫视四周。", "她左右张望寻找对手。" },
                    new[] { "他没有四处张望。", "她谈论如何搜寻。" }),
                Legacy(Dance, "跳舞", "身体实际随节奏舞动或起舞。",
                    new[] { "跳舞", "dance" },
                    new[]
                    {
                        "跳舞", "起舞", "随节奏舞动", "随着鼓点扭动起来",
                        "随着鼓点扭动身体", "转身跳起舞来", "started dancing"
                    },
                    new[] { "他随节奏翩翩起舞。", "她开心地跳起舞来。" },
                    new[] { "他不打算跳舞。", "她只是说了‘跳舞’。" }),
                NewAction(Greet, ActionMode.RandomGroup, true, "问候",
                    "实际挥手、抬手或招手向他人打招呼；单纯说‘你好’不算动作。",
                    new[] { "问候", "打招呼", "greet" },
                    new[] { "挥了挥手", "挥挥手", "招了招手", "挥手问候", "挥了挥手打招呼", "抬手打招呼", "抬手向来人问候", "抬手向来人打招呼", "招手问候", "招了招手致意", "向他挥手致意", "挥手致意", "微笑着挥手致意", "抬手挥了挥向我问好", "waved in greeting", "greeted with a wave", "waved hello", "挥手打招呼", "打招呼", "左右挥了挥", "抬起手左右挥了挥", "主动打了个招呼" },
                    new[] { "问候", "打招呼", "挥手", "招手", "你好", "hello", "greet", "greeting", "waved" },
                    new[] { "他微笑着挥手问候。", "她朝众人招了招手打招呼。" },
                    new[] { "他说：‘你好。’", "他没有向任何人挥手。" }),
                NewAction(Agree, ActionMode.RandomGroup, true, "点头同意",
                    "实际点头表示同意、赞成或应允；口头同意本身不算动作。",
                    new[] { "点头同意", "点头赞成", "agree" },
                    new[] { "点了点头", "点点头", "点头同意", "点头表示同意", "点头表示赞成", "点了点头表示赞成", "点头应允", "点头答应", "点头认可", "颔首赞同", "郑重地点头表示同意", "nodded in agreement", "nodded firmly in agreement", "nodded yes", "nodded approvingly", "郑重地点了点头", "郑重地点了点头表示同意", "郑重地点了点头表示赞成" },
                    new[] { "同意", "答应", "赞成", "认可", "点头", "agree", "agreed", "agreement" },
                    new[] { "他认真地点头同意。", "她点了点头表示赞成。" },
                    new[] { "他说：‘好，我同意。’", "他没有点头。" }),
                NewAction(Disagree, ActionMode.RandomGroup, true, "摇头否定",
                    "实际摇头表示否定、反对或拒绝；纯口头拒绝不算动作。",
                    new[] { "摇头否定", "摇头反对", "disagree" },
                    new[] { "摇了摇头", "摇摇头", "摇头否定", "摇头表示反对", "摇头表示不同意", "摇了摇头拒绝", "摇头不同意", "摇头表示不赞成", "摇头回绝", "用力摇头表示否定", "摇头拒绝", "shook his head in disagreement", "shook her head no", "shook his head to disagree", "shook his head to show clear disagreement", "摇了几下头", "摆手明确表示反对", "连续摇头表示反对" },
                    new[] { "不同意", "反对", "拒绝", "否定", "摇头", "disagree", "disagreed", "disagreement" },
                    new[] { "他坚定地摇头否定。", "她摇了摇头表示反对。" },
                    new[] { "他说：‘我不同意。’", "他失望地摇头叹息。" }),
                NewAction(Unsure, ActionMode.RandomGroup, true, "摊手犹豫",
                    "实际耸肩、摊手或用身体动作表示不知道、不确定和犹豫。",
                    new[] { "摊手", "摊手表示不知道", "unsure", "shrug" },
                    new[] { "摊了摊手", "耸耸肩", "摊手表示不知道", "摊开双手表示不确定", "耸了耸肩", "迟疑地摊手", "困惑地摊开手", "困惑地摊开双手", "无奈地耸肩", "耸肩摊手，一脸茫然", "犹豫地摊开双手", "shrugged uncertainly", "shrugged, unsure", "shrugged and opened his hands uncertainly", "spread his hands in uncertainty", "摊开双手耸了耸肩", "显得完全拿不准", "拿不准" },
                    new[] { "摊手", "耸肩", "不确定手势", "犹豫动作", "uncertain", "uncertainty" },
                    new[] { "他困惑地摊开双手。", "她迟疑地耸了耸肩。" },
                    new[] { "他说：‘我不知道。’", "他没有做出任何犹豫动作。" }),
                NewAction(Explain, ActionMode.RandomGroup, true, "手势解释",
                    "实际用双手比划、摊手或配合手势进行解释说明。",
                    new[] { "比划解释", "手势解释", "explain" },
                    new[] { "比划着解释", "用手势解释", "摊手说明", "摊开双手作出说明", "一边比划一边解释", "一边比划一边说明", "挥动双手作出解释", "用手比划着讲解", "边说边用双手比划解释", "摊开掌心详细说明", "gestured while explaining", "gestured back and forth while explaining", "gestured back and forth while explaining the plan", "explained with his hands", "explained with a hand gesture", "摊开手掌", "来回比划着说明", "比划着说明", "一边摊开手掌一边来回比划着说明" },
                    new[] { "解释", "说明", "讲解", "比划", "手势", "explaining", "explanation" },
                    new[] { "他一边比划一边解释。", "她摊开双手作出说明。" },
                    new[] { "他只是在口头解释。", "她要求别人作出解释。" }),
                NewAction(Promise, ActionMode.OneShot, true, "举手起誓",
                    "实际举手、立誓或作出明确的承诺手势；口头保证本身不算动作。",
                    new[] { "举手起誓", "郑重起誓", "promise" },
                    new[] { "举手起誓", "抬手立誓", "抬起右手作出承诺", "郑重地作出承诺手势", "举起右手发誓", "做出起誓动作", "庄重地立下誓言", "抬起右手发誓保证", "举手立下誓言", "raised his hand to swear", "made an oath gesture", "raised his hand and swore", "raised his right hand and swore", "raised her right hand and swore", "举起右手郑重起誓", "拍了拍胸口作保证", "作保证" },
                    new[] { "承诺", "保证", "发誓", "起誓", "立誓", "promise", "promised", "swore" },
                    new[] { "他郑重地举手起誓。", "她抬起右手作出承诺。" },
                    new[] { "他说：‘我保证。’", "他拒绝作出承诺。" }),
                NewAction(CrossArms, ActionMode.OneShot, true, "抱臂",
                    "实际将双臂交叉在胸前或抱臂而立，按一次性手势播放。",
                    new[] { "抱臂", "双臂交叉", "cross arms", "crossarms" },
                    new[] { "抱起双臂", "抱臂", "抱臂而立", "双臂抱胸而立", "双臂交叉在胸前", "把手臂交叉在胸前", "胳膊交叉在胸前", "环抱双臂", "交叉双臂站着", "crossed his arms", "folded her arms", "folded his arms", "收回双手", "交叉抱在胸前", "两条手臂交叉抱在胸前" },
                    new[] { "抱臂", "双臂交叉", "交叉手臂", "cross arms", "crossed arms" },
                    new[] { "他抱起双臂站在原地。", "她将双臂交叉在胸前。" },
                    new[] { "他没有抱起手臂。", "两条路线彼此交叉。" }),
                NewAction(DeepBow, ActionMode.OneShot, false, "深鞠躬",
                    "身体明显深弯腰完成深鞠躬；普通鞠躬和普通行礼仍属于respect。",
                    new[] { "深鞠躬", "深深鞠躬", "deep bow", "deepbow" },
                    new[] { "深鞠躬", "深深鞠躬", "深深鞠了一躬", "深深地鞠了一躬", "弯下腰深深鞠了一躬", "弯腰深鞠一躬", "躬身到底", "躬身到底郑重致礼", "九十度鞠躬", "深深弯腰致礼", "深深弯腰鞠躬到底", "深深弯下腰行大礼", "bowed deeply", "made a deep bow", "made a profound bow", "深深鞠躬到底", "弯腰近乎九十度", "弯腰深鞠到底" },
                    new[] { "深鞠躬", "深深鞠躬", "九十度鞠躬", "deep bow" },
                    new[] { "他弯下腰深深鞠了一躬。", "她躬身到底郑重致礼。" },
                    new[] { "他礼貌地普通鞠躬。", "他只是抬手致意。" })
            });

        private static readonly HashSet<string> NewIntentKeys = new HashSet<string>(
            Entries.Skip(16).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly HashSet<string> KneelOverlayKeys = new HashSet<string>(
            Entries.Where(entry => entry.CanOverlayKneel).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly string[] UnsupportedActionCues =
        {
            "哭泣", "大哭", "哭了起来", "坐下", "躺下", "趴下", "转身", "走路",
            "走了两步", "走了过去", "走过去", "走去", "走向", "走到", "迈步", "迈步走向",
            "跑步", "奔跑", "拥抱", "亲吻", "喝酒", "吃东西", "踢腿", "踢人", "攻击",
            "walked over", "walked toward", "walked to", "moved toward", "approached",
            "sat down", "lay down", "hugged", "kissed", "drank", "ate", "kicked", "attacked"
        };

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
        {
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            return string.Join("\n", Entries
                .Where(entry => allowed.Contains(entry.IntentKey))
                .Select(entry =>
                    entry.IntentKey + "（" + entry.DisplayNameZhCn + "）：" +
                    entry.SemanticDescriptionZhCn +
                    " 正例：" + string.Join("；", entry.PositiveExamples) +
                    " 反例：" + string.Join("；", entry.NegativeExamples)));
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
