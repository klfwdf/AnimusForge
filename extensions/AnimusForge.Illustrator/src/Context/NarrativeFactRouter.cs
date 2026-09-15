using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace AnimusForge.Illustrator.Context
{
    /// <summary>只处理冻结后的文本；每次生成/快照执行，不扫描游戏对象，不追加模型请求。</summary>
    internal static class NarrativeFactRouter
    {
        private static readonly Regex Tags = new Regex(@"<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex Headings = new Regex(@"【[^】]*】|\[[^\]\r\n]*\]", RegexOptions.Compiled);
        private static readonly Regex Quotes = new Regex("[“\"「『][^”\"」』\\r\\n]*[”\"」』]", RegexOptions.Compiled);
        private static readonly Regex DirectSpeech = new Regex("(?:说道|说|喊道|宣称|声称|写道|表示|said|says|claimed)\\s*[:：]?\\s*[“\"「『][^”\"」』]*[”\"」』]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Sentences = new Regex(@"[^。！？!?\r\n]+[。！？!?]?", RegexOptions.Compiled);
        private static readonly string[] EventTerms = {
            "围城", "围攻", "围困", "攻城", "进攻", "击败", "战败", "战胜", "获胜", "撤退", "逃离", "被俘", "俘虏", "处决", "释放", "获释", "拒绝", "未能", "没有", "取消",
            "占领", "易主", "移交", "归还", "投降", "停战", "和谈", "结盟", "宣战", "签订", "劫掠", "焚烧",
            "死亡", "去世", "继位", "婚", "宴", "比武", "抵达", "出发", "行军", "失踪", "救援", "粮", "商", "瘟疫",
            "siege", "battle", "attack", "defeat", "victory", "retreat", "release", "prison", "peace", "died", "failed", "not "
        };
        private static readonly string[] VisualTerms = {
            "外貌", "肤", "发色", "头发", "红发", "白发", "黑发", "金发", "银发", "眼", "瞳", "胡须", "伤疤", "面容",
            "身材", "体型", "身高", "种族", "精灵", "矮人", "兽人", "兽耳", "尖耳", "鳞", "翅膀", "尾巴",
            "穿", "佩", "服饰", "衣", "袍", "甲", "头巾", "冠", "纹章", "徽记", "旗帜", "建筑", "屋", "城墙",
            "skin", "hair", "eyes", "scar", "race", "elf", "elves", "dwarf", "dwarves", "orc", "wear", "armor", "armour", "robe", "herald"
        };

        public static string CleanText(string text)
        {
            // 先删标签再解码：保留超链接显示文本，绝不把 href/ID 带入提示词。
            return WebUtility.HtmlDecode(Tags.Replace(text ?? string.Empty, string.Empty)).Trim();
        }

        private static IEnumerable<string> Parts(string text)
        {
            foreach (Match match in Sentences.Matches(CleanText(text)))
            {
                string part = Headings.Replace(match.Value, string.Empty).Trim(' ', '\t', '#', '*', '-', ':', '：');
                if (!string.IsNullOrWhiteSpace(part)) yield return part;
            }
        }

        private static bool ContainsAny(string text, string[] terms)
        {
            foreach (string term in terms)
                if (text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static string BuildEventEvidence(string title, string subtitle, string body)
        {
            var facts = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string[] sources = { body, subtitle, title };
            for (int i = 0; i < sources.Length; i++)
            {
                foreach (string sentence in Parts(sources[i]))
                {
                    // 引语是当事人说法而非已发生事实，不能从中臆造胜负。
                    string fact = DirectSpeech.Replace(sentence, string.Empty).Trim(' ', ':', '：', '。');
                    if (fact.Length == 0) continue;
                    bool eventClause = ContainsAny(fact, EventTerms);
                    if (i > 0 && !eventClause) continue;
                    if (fact != sentence.Trim(' ', ':', '：', '。') && !eventClause) continue;
                    if (seen.Add(fact)) facts.Add(fact);
                }
            }
            // 保留完整证据句，不按关键词重组主谓宾，不把未知/计划/失败改写成成功。
            return facts.Count > 0 ? string.Join("。\n", facts) + "。"
                : "事件行动与结果未获明确叙述；不得推定战胜、陷落、处决或死亡。";
        }

        public static string ExtractVisualEvidence(params string[] backgrounds)
        {
            var facts = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string text in backgrounds)
                foreach (string part in Parts(text))
                    if (ContainsAny(part, VisualTerms) && seen.Add(part)) facts.Add(part);
            return string.Join("\n", facts);
        }

        public static string SceneEvidence(string raw, string knownFacts)
        {
            var facts = new List<string>();
            foreach (string part in Parts(raw))
                if ((knownFacts ?? string.Empty).IndexOf(part.TrimEnd('。'), StringComparison.Ordinal) < 0) facts.Add(part);
            return string.Join("\n", facts);
        }

        public static bool HasNarrativeEcho(string output, string narrative, string hardFacts)
        {
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(narrative)) return false;
            var candidates = new List<string>(Parts(narrative));
            foreach (Match quote in Quotes.Matches(narrative)) candidates.Add(quote.Value.Trim('“', '”', '"', '「', '」', '『', '』'));
            foreach (string candidate in candidates)
            {
                string text = candidate.Trim(' ', '。', '！', '？', '!', '?', '"', '“', '”');
                if (text.Length < 8) continue; // 短人名/地名共享是合法的，不作泄漏判定。
                if (output.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (hardFacts ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) return true;
            }
            return false;
        }
    }
}
