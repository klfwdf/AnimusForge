using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class ImplicitEmotionDecisionV1
    {
        internal ImplicitEmotionDecisionV1(
            string intentKey,
            int score,
            IEnumerable<string> evidence)
        {
            IntentKey = intentKey ?? string.Empty;
            Score = score;
            Evidence = Array.AsReadOnly((evidence ?? Enumerable.Empty<string>()).ToArray());
        }

        public string IntentKey { get; }
        public int Score { get; }
        public IReadOnlyList<string> Evidence { get; }
    }

    public static class ImplicitEmotionInferenceV1
    {
        private const int MinimumScore = 5;
        private const int MinimumWinningMargin = 2;

        private static readonly ReadOnlyCollection<string> SupportedKeys =
            Array.AsReadOnly(new[]
            {
                SceneActionFrameworkV4.Fear,
                SceneActionFrameworkV4.Rage,
                SceneActionFrameworkV4.Disappointed,
                SceneActionFrameworkV4.Laugh,
                SceneActionFrameworkV4.Unsure
            });

        private static readonly string[] FearPallorCues =
        {
            "面色微微一白", "脸色一白", "脸色瞬间发白", "脸色骤然发白",
            "血色褪去", "面上失了血色", "唇色发白"
        };
        private static readonly string[] FearFreezeCues =
        {
            "身形僵住", "身体僵住", "动作僵住", "呼吸停了一瞬", "呼吸一滞",
            "喉结滚动", "喉头滚动", "脚步顿住"
        };
        private static readonly string[] FearTremorCues =
        {
            "手指微微发颤", "指尖发颤", "双手轻颤", "声音发紧", "尾音发颤",
            "嘴唇哆嗦", "肩膀轻颤"
        };
        private static readonly string[] FearMaskingCues =
        {
            "很快稳住身形", "强迫自己站直", "努力维持平静", "强作镇定",
            "故作镇定", "勉强稳住呼吸", "把慌乱压了下去"
        };
        private static readonly string[] FearThreatCues =
        {
            "砍下你的头", "砍掉你的头", "砍下你的头颅", "取你性命", "要你的命",
            "处死你", "斩首", "吊死", "杀了你", "弄死你", "割断你的喉咙",
            "提刀把你的头颅砍下来"
        };
        private static readonly string[] FearVulnerabilityCues =
        {
            "无力反抗", "饶我一命", "留我一命", "死个明白", "是否还有活命",
            "不敢违抗", "任凭处置", "求您开恩", "别杀我"
        };
        private static readonly string[] FearNegativeCues =
        {
            "毫无惧色", "面不改色", "神色自若", "根本不怕", "毫不害怕",
            "没有丝毫恐惧", "并不畏惧", "从容不迫", "镇定自若"
        };

        private static readonly string[] RageBodyCues =
        {
            "咬紧牙关", "牙关紧咬", "下颌绷紧", "腮帮绷紧", "手背青筋凸起",
            "额角青筋跳动", "拳头攥得发白", "五指收拢成拳", "指节捏得发白",
            "抓紧剑柄", "掌心被指甲掐出印"
        };
        private static readonly string[] RageMaskingCues =
        {
            "把话咽了回去", "强忍着没有发作", "压住即将出口的话", "声音冷了下来",
            "语气骤然变硬", "眼神骤然冰冷"
        };
        private static readonly string[] RageProvocationCues =
        {
            "废物", "懦夫", "叛徒", "羞辱", "侮辱", "污蔑", "背叛", "栽赃"
        };
        private static readonly string[] RageDialogueCues =
        {
            "你没有资格", "这笔账", "不会忘记", "收回这句话", "别逼我",
            "记住你今天说的话"
        };
        private static readonly string[] RageNegativeCues =
        {
            "没有动气", "并未动气", "神色平和", "毫无怒意", "只是平静地",
            "不以为意"
        };

        private static readonly string[] DisappointedBodyCues =
        {
            "目光黯淡下来", "眼里的光暗了下去", "肩膀缓缓垮下",
            "原本挺直的背脊松了下来", "垂下眼帘", "沉默良久",
            "长长吐出一口气", "轻轻叹了口气", "苦涩地扯了扯嘴角",
            "将准备好的东西收了回去", "慢慢收回伸出的手"
        };
        private static readonly string[] DisappointedContextCues =
        {
            "食言", "承诺作废", "没有兑现", "拒绝了", "落空", "白等", "辜负",
            "原以为"
        };
        private static readonly string[] DisappointedDialogueCues =
        {
            "原来如此", "我明白了", "就当我没说", "罢了", "算了", "到此为止",
            "不必再提"
        };
        private static readonly string[] DisappointedNegativeCues =
        {
            "欣然接受", "喜出望外", "眼前一亮", "并不在意", "如释重负"
        };

        private static readonly string[] LaughBodyCues =
        {
            "嘴角压不住地上扬", "唇角悄悄翘起", "嘴角抽动了一下", "把脸别到一旁",
            "肩膀轻轻抖了两下", "鼻间漏出短促气音", "喉间溢出一声气音",
            "抬手掩住嘴角", "差点呛到", "拍了拍自己的胸口"
        };
        private static readonly string[] LaughContextCues =
        {
            "荒唐", "滑稽", "可笑", "笑话", "离谱", "荒谬"
        };
        private static readonly string[] LaughDialogueCues =
        {
            "这倒有意思", "真有你的", "你认真的吗", "妙极了", "好一个",
            "竟能说出这种话"
        };
        private static readonly string[] LaughNegativeCues =
        {
            "没有任何笑意", "嘴角纹丝不动", "神情严肃", "并未觉得有趣", "冷冷看着"
        };

        private static readonly string[] UnsureBodyCues =
        {
            "几次张口又停住", "话到嘴边又咽下", "手指悬在半空", "抬起的手又放下",
            "脚步迈出半步又收回", "目光在两人之间来回游移", "视线反复扫过众人",
            "反复摩挲指节", "低头沉吟片刻", "迟迟没有回答"
        };
        private static readonly string[] UnsureContextCues =
        {
            "两个选择", "只能选一个", "相互矛盾", "不同说法", "立刻决定",
            "究竟哪一个", "二选一"
        };
        private static readonly string[] UnsureDialogueCues =
        {
            "容我想想", "让我理一理", "这件事没那么简单", "我需要一点时间",
            "两边都有道理", "一时难以决定"
        };
        private static readonly string[] UnsureNegativeCues =
        {
            "立刻作出决定", "毫不迟疑", "当即回答", "没有任何停顿", "早已有了答案"
        };

        public static IReadOnlyList<string> SupportedIntentKeys => SupportedKeys;

        public static bool TryInfer(
            string previousPlayerText,
            string stageDirectionText,
            string fullNpcReplyText,
            IEnumerable<string> allowedIntentKeys,
            out ImplicitEmotionDecisionV1 decision)
        {
            decision = null;
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            if (allowed.Count == 0)
            {
                return false;
            }

            string previous = CommandParser.Normalize(previousPlayerText);
            string stage = CommandParser.Normalize(stageDirectionText);
            string full = CommandParser.Normalize(fullNpcReplyText);
            if (string.IsNullOrEmpty(stage) && string.IsNullOrEmpty(full))
            {
                return false;
            }

            List<Candidate> candidates = new List<Candidate>();
            AddCandidate(candidates, allowed, ScoreFear(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreRage(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreDisappointed(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreLaugh(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreUnsure(previous, stage, full));

            Candidate[] ordered = candidates
                .Where(candidate => candidate.Score >= MinimumScore)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.IntentKey, StringComparer.Ordinal)
                .ToArray();
            if (ordered.Length == 0)
            {
                return false;
            }
            if (ordered.Length > 1 &&
                ordered[0].Score - ordered[1].Score < MinimumWinningMargin)
            {
                return false;
            }

            decision = new ImplicitEmotionDecisionV1(
                ordered[0].IntentKey,
                ordered[0].Score,
                ordered[0].Evidence);
            return true;
        }

        private static Candidate ScoreFear(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Fear);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, FearNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, FearPallorCues) ||
                ContainsOrdered(stage, "面", "失", "血色") ||
                ContainsOrdered(stage, "面色", "白") ||
                ContainsOrdered(stage, "脸色", "白") ||
                ContainsOrdered(stage, "唇", "白"),
                3,
                "visible-pallor");
            candidate.AddIf(
                ContainsAny(stage, FearFreezeCues) ||
                ContainsOrdered(stage, "身形", "僵") ||
                ContainsOrdered(stage, "身体", "僵") ||
                ContainsOrdered(stage, "呼吸", "停") ||
                ContainsOrdered(stage, "呼吸", "滞") ||
                ContainsOrdered(stage, "喉结", "滚"),
                2,
                "involuntary-freeze");
            candidate.AddIf(
                ContainsAny(stage, FearTremorCues) ||
                ContainsOrdered(stage, "手指", "颤") ||
                ContainsOrdered(stage, "指尖", "颤") ||
                ContainsOrdered(stage, "声音", "紧") ||
                ContainsOrdered(stage, "尾音", "颤") ||
                ContainsOrdered(stage, "嘴唇", "哆嗦"),
                3,
                "loss-of-control");
            candidate.Add(stage, FearMaskingCues, 2, "forced-composure");
            candidate.Add(previous, FearThreatCues, 3, "lethal-threat-context");
            candidate.Add(full, FearVulnerabilityCues, 2, "vulnerable-dialogue");
            return candidate;
        }

        private static Candidate ScoreRage(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Rage);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, RageNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, RageBodyCues) ||
                ContainsOrdered(stage, "下颌", "绷紧") ||
                ContainsOrdered(stage, "腮帮", "绷紧") ||
                ContainsOrdered(stage, "五指", "成拳") ||
                ContainsOrdered(stage, "拳头", "发白") ||
                ContainsOrdered(stage, "指节", "发白") ||
                ContainsOrdered(stage, "青筋", "凸起") ||
                ContainsOrdered(stage, "青筋", "跳动") ||
                ContainsOrdered(stage, "掌心", "指甲", "印"),
                3,
                "hostile-body-tension");
            candidate.Add(stage, RageMaskingCues, 3, "suppressed-escalation");
            candidate.Add(previous, RageProvocationCues, 2, "provocation-context");
            candidate.AddIf(
                ContainsAny(full, RageDialogueCues) ||
                ContainsOrdered(full, "逼", "我") ||
                ContainsOrdered(full, "账", "忘") ||
                ContainsOrdered(full, "话", "收回"),
                2,
                "hostile-dialogue");
            return candidate;
        }

        private static Candidate ScoreDisappointed(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Disappointed);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, DisappointedNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, DisappointedBodyCues) ||
                ContainsOrdered(stage, "目光", "黯淡") ||
                ContainsOrdered(stage, "眼", "光", "暗") ||
                ContainsOrdered(stage, "肩膀", "垮") ||
                ContainsOrdered(stage, "背脊", "松") ||
                ContainsOrdered(stage, "眼帘", "垂") ||
                ContainsOrdered(stage, "手", "收回"),
                3,
                "withdrawn-body-language");
            candidate.AddIf(
                ContainsAny(previous, DisappointedContextCues) ||
                ContainsOrdered(previous, "不会", "兑现") ||
                ContainsOrdered(previous, "承诺", "作废") ||
                ContainsOrdered(previous, "拒绝", "提议") ||
                ContainsOrdered(previous, "白", "等"),
                2,
                "broken-expectation-context");
            candidate.AddIf(
                ContainsAny(full, DisappointedDialogueCues) ||
                ContainsOrdered(full, "想得", "太多") ||
                ContainsOrdered(full, "没有", "意义") ||
                ContainsOrdered(full, "放下"),
                2,
                "resigned-dialogue");
            return candidate;
        }

        private static Candidate ScoreLaugh(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Laugh);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, LaughNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, LaughBodyCues) ||
                ContainsOrdered(stage, "嘴角", "上扬") ||
                ContainsOrdered(stage, "唇角", "翘") ||
                ContainsOrdered(stage, "嘴角", "抽动") ||
                ContainsOrdered(stage, "肩膀", "抖") ||
                ContainsOrdered(stage, "鼻间", "气音") ||
                ContainsOrdered(stage, "喉间", "气音") ||
                ContainsOrdered(stage, "手", "掩", "嘴角") ||
                ContainsOrdered(stage, "差点", "呛到") ||
                ContainsOrdered(stage, "拍", "胸口"),
                4,
                "suppressed-amusement-body-language");
            candidate.AddIf(
                ContainsOrdered(stage, "嘴角", "压不住", "上扬") ||
                ContainsOrdered(stage, "唇角", "翘") ||
                ContainsOrdered(stage, "肩膀", "抖") ||
                ContainsOrdered(stage, "鼻间", "气音") ||
                ContainsOrdered(stage, "喉间", "气音") ||
                ContainsOrdered(stage, "手", "掩", "嘴角"),
                1,
                "unmistakable-amusement");
            candidate.AddIf(
                ContainsAny(previous, LaughContextCues) ||
                ContainsOrdered(previous, "竟", "当成") ||
                ContainsOrdered(previous, "居然", "当成") ||
                ContainsOrdered(previous, "拿", "当"),
                1,
                "absurd-context");
            candidate.AddIf(
                ContainsAny(full, LaughDialogueCues) ||
                ContainsOrdered(full, "你", "认真") ||
                ContainsOrdered(full, "这", "有意思") ||
                ContainsOrdered(full, "真有", "你") ||
                ContainsOrdered(full, "不会", "无聊") ||
                ContainsOrdered(full, "场面", "难得") ||
                ContainsOrdered(full, "差点", "信"),
                2,
                "amused-dialogue");
            return candidate;
        }

        private static Candidate ScoreUnsure(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Unsure);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, UnsureNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, UnsureBodyCues) ||
                ContainsOrdered(stage, "张口", "停") ||
                ContainsOrdered(stage, "话", "咽下") ||
                ContainsOrdered(stage, "手指", "半空") ||
                ContainsOrdered(stage, "手", "放下") ||
                ContainsOrdered(stage, "脚步", "收回") ||
                ContainsOrdered(stage, "目光", "来回") ||
                ContainsOrdered(stage, "视线", "反复"),
                3,
                "indecisive-body-language");
            candidate.AddIf(
                ContainsAny(previous, UnsureContextCues) ||
                ContainsOrdered(previous, "命令", "矛盾") ||
                ContainsOrdered(previous, "说法", "不同") ||
                ContainsOrdered(previous, "选择", "一个"),
                2,
                "choice-conflict-context");
            candidate.AddIf(
                ContainsAny(full, UnsureDialogueCues) ||
                ContainsOrdered(full, "让我", "理一理") ||
                ContainsOrdered(full, "需要", "时间") ||
                ContainsOrdered(full, "难以", "决定") ||
                ContainsOrdered(full, "需要", "确认"),
                2,
                "deliberative-dialogue");
            return candidate;
        }

        private static void AddCandidate(
            ICollection<Candidate> candidates,
            ISet<string> allowed,
            Candidate candidate)
        {
            if (candidate != null && allowed.Contains(candidate.IntentKey))
            {
                candidates.Add(candidate);
            }
        }

        private static string Join(params string[] values)
        {
            return string.Join(" ", values.Where(value => !string.IsNullOrEmpty(value)));
        }

        private static bool ContainsOrdered(string text, params string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null || tokens.Length == 0)
            {
                return false;
            }
            int cursor = 0;
            foreach (string rawToken in tokens)
            {
                string token = CommandParser.Normalize(rawToken);
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }
                int index = text.IndexOf(token, cursor, StringComparison.Ordinal);
                if (index < 0)
                {
                    return false;
                }
                cursor = index + token.Length;
            }
            return true;
        }
        private static bool ContainsAny(string text, IEnumerable<string> cues)
        {
            return !string.IsNullOrEmpty(text) &&
                   (cues ?? Enumerable.Empty<string>()).Any(cue =>
                       !string.IsNullOrEmpty(cue) &&
                       text.IndexOf(CommandParser.Normalize(cue), StringComparison.Ordinal) >= 0);
        }

        private sealed class Candidate
        {
            public Candidate(string intentKey)
            {
                IntentKey = intentKey;
            }

            public string IntentKey { get; }
            public int Score { get; private set; }
            public List<string> Evidence { get; } = new List<string>();

            public void Add(
                string text,
                IEnumerable<string> cues,
                int points,
                string evidence)
            {
                AddIf(ContainsAny(text, cues), points, evidence);
            }

            public void AddIf(bool condition, int points, string evidence)
            {
                if (!condition)
                {
                    return;
                }
                Score += points;
                Evidence.Add(evidence);
            }
        }
    }
}