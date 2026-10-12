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
        private static readonly Regex DirectSpeech = new Regex(IllustratorText.Raw("Residual_NarrativeFactRouter_5ac31d57431c"), RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Sentences = new Regex(@"[^。！？!?\r\n]+[。！？!?]?", RegexOptions.Compiled);
        private static readonly Regex SpeechOrWrittenText = new Regex(
            IllustratorText.Raw("Residual_NarrativeFactRouter_71ac1f4b6fc5"),
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly string[] EventTerms = {
            IllustratorText.Raw("Residual_NarrativeFactRouter_133b42f2747f"), IllustratorText.Raw("Residual_NarrativeFactRouter_12333fae46c7"), IllustratorText.Raw("Residual_NarrativeFactRouter_11cf4a1c0bd9"), IllustratorText.Raw("Residual_NarrativeFactRouter_660e3b93ff8f"), IllustratorText.Raw("Residual_NarrativeFactRouter_76be2c2fb446"), IllustratorText.Raw("Residual_NarrativeFactRouter_ce2ad91ea768"), IllustratorText.Raw("Residual_NarrativeFactRouter_bd5cdcb6f4f6"), IllustratorText.Raw("Residual_NarrativeFactRouter_5c161e4327bc"), IllustratorText.Raw("Residual_NarrativeFactRouter_842f69cf668c"), IllustratorText.Raw("Residual_NarrativeFactRouter_f2adfafb5b50"), IllustratorText.Raw("Residual_NarrativeFactRouter_6c217ce7dc69"), IllustratorText.Raw("Residual_NarrativeFactRouter_68ead4a51ea8"), IllustratorText.Raw("Residual_NarrativeFactRouter_d4d7cb2d3fda"), IllustratorText.Raw("Residual_NarrativeFactRouter_6d6d9ee1cf41"), IllustratorText.Raw("Residual_NarrativeFactRouter_cc3d94550a8c"), IllustratorText.Raw("Residual_NarrativeFactRouter_75dbe6df6e18"), IllustratorText.Raw("Residual_NarrativeFactRouter_136de7a8c46f"), IllustratorText.Raw("Residual_NarrativeFactRouter_4f2d9c358b11"), IllustratorText.Raw("Residual_NarrativeFactRouter_f127804b9227"), IllustratorText.Raw("Residual_NarrativeFactRouter_2cd0f3be8738"),
            IllustratorText.Raw("Residual_NarrativeFactRouter_a9012846ac1c"), IllustratorText.Raw("Residual_NarrativeFactRouter_30c75b614eba"), IllustratorText.Raw("Residual_NarrativeFactRouter_f3e939a52d2c"), IllustratorText.Raw("Residual_NarrativeFactRouter_d94f1d858c94"), IllustratorText.Raw("Residual_NarrativeFactRouter_08f61250ec4d"), IllustratorText.Raw("Residual_NarrativeFactRouter_2731a6af3316"), IllustratorText.Raw("Residual_NarrativeFactRouter_011bca961b98"), IllustratorText.Raw("Residual_NarrativeFactRouter_45e7a9a2d6f8"), IllustratorText.Raw("Residual_NarrativeFactRouter_adc28d52034d"), IllustratorText.Raw("Residual_NarrativeFactRouter_d82817642e49"), IllustratorText.Raw("Residual_NarrativeFactRouter_8108b7f92bc2"), IllustratorText.Raw("Residual_NarrativeFactRouter_4c9ab4541ef9"),
            IllustratorText.Raw("Residual_NarrativeFactRouter_82d3130fa582"), IllustratorText.Raw("Residual_NarrativeFactRouter_96a70c00ebaa"), IllustratorText.Raw("Residual_NarrativeFactRouter_3813a2ebb522"), IllustratorText.Raw("Residual_NarrativeFactRouter_ce5b647eae00"), IllustratorText.Raw("Residual_NarrativeFactRouter_2205cd519cfc"), IllustratorText.Raw("Residual_NarrativeFactRouter_bb892b639c3d"), IllustratorText.Raw("Residual_NarrativeFactRouter_893354ab1272"), IllustratorText.Raw("Residual_NarrativeFactRouter_efdc22fca1c6"), IllustratorText.Raw("Residual_NarrativeFactRouter_085f550d8a61"), IllustratorText.Raw("Residual_NarrativeFactRouter_6a972acd6363"), IllustratorText.Raw("Residual_NarrativeFactRouter_57ec946e84bf"), IllustratorText.Raw("Residual_NarrativeFactRouter_11612b46668a"), IllustratorText.Raw("Residual_NarrativeFactRouter_537ee404352f"), IllustratorText.Raw("Residual_NarrativeFactRouter_d8448689b34a"),
            "siege", "battle", "attack", "defeat", "victory", "retreat", "release", "prison", "peace", "died", "failed", "not "
        };
        private static readonly string[] VisualTerms = {
            IllustratorText.Raw("Residual_NarrativeFactRouter_c1508818118e"), IllustratorText.Raw("Residual_NarrativeFactRouter_a715360bed04"), IllustratorText.Raw("Residual_NarrativeFactRouter_557c2ff0af40"), IllustratorText.Raw("Residual_NarrativeFactRouter_476627a8c1b8"), IllustratorText.Raw("Residual_NarrativeFactRouter_dc4263f00f03"), IllustratorText.Raw("Residual_NarrativeFactRouter_bedff3a5dba1"), IllustratorText.Raw("Residual_NarrativeFactRouter_3c82117c7eb1"), IllustratorText.Raw("Residual_NarrativeFactRouter_8c6666fa899d"), IllustratorText.Raw("Residual_NarrativeFactRouter_ebe43b6fd588"), IllustratorText.Raw("Residual_NarrativeFactRouter_393ee0b5797e"), IllustratorText.Raw("Residual_NarrativeFactRouter_28d00c78273d"), IllustratorText.Raw("Residual_NarrativeFactRouter_5553cf603284"), IllustratorText.Raw("Residual_NarrativeFactRouter_b713626c882c"), IllustratorText.Raw("Residual_NarrativeFactRouter_7c4efdcff7fb"),
            IllustratorText.Raw("Residual_NarrativeFactRouter_61833b2089d5"), IllustratorText.Raw("Residual_NarrativeFactRouter_c8900ba2f5a4"), IllustratorText.Raw("Residual_NarrativeFactRouter_2763b73dd129"), IllustratorText.Raw("Residual_NarrativeFactRouter_b49377b38c6c"), IllustratorText.Raw("Residual_NarrativeFactRouter_4ae171bd66d9"), IllustratorText.Raw("Residual_NarrativeFactRouter_5c478562daa6"), IllustratorText.Raw("Residual_NarrativeFactRouter_4bba868bd743"), IllustratorText.Raw("Residual_NarrativeFactRouter_a3186d2705e1"), IllustratorText.Raw("Residual_NarrativeFactRouter_452e4e2a1e19"), IllustratorText.Raw("Residual_NarrativeFactRouter_d6073c9183c1"), IllustratorText.Raw("Residual_NarrativeFactRouter_aca4c85ba79a"), IllustratorText.Raw("Residual_NarrativeFactRouter_797d828e8c31"),
            IllustratorText.Raw("Residual_NarrativeFactRouter_9fdbcf447095"), IllustratorText.Raw("Residual_NarrativeFactRouter_82d53c0179cb"), IllustratorText.Raw("Residual_NarrativeFactRouter_4bc58093f01e"), IllustratorText.Raw("Residual_NarrativeFactRouter_59186bd4029c"), IllustratorText.Raw("Residual_NarrativeFactRouter_6782f34b65b4"), IllustratorText.Raw("Residual_NarrativeFactRouter_33e3580c0931"), IllustratorText.Raw("Residual_NarrativeFactRouter_d37aef4906bb"), IllustratorText.Raw("Residual_NarrativeFactRouter_b9ac9480719e"), IllustratorText.Raw("Residual_NarrativeFactRouter_38c5c6e4eccf"), IllustratorText.Raw("Residual_NarrativeFactRouter_8c9e2da30557"), IllustratorText.Raw("Residual_NarrativeFactRouter_f5aee0707cc1"), IllustratorText.Raw("Residual_NarrativeFactRouter_5b46860a8e8b"), IllustratorText.Raw("Residual_NarrativeFactRouter_de9ade549e10"), IllustratorText.Raw("Residual_NarrativeFactRouter_f9f9002cf492"),
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
                : IllustratorText.Get("Facts_EventEvidenceUnknown");
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

        public static bool HasNarrativeEcho(string output, string narrative, string hardFacts, bool allowVisualNarration = false)
        {
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(narrative)) return false;
            if (allowVisualNarration)
            {
                // Conversation narration can already describe visible actions. Keep the
                // stricter biography guard for the separately labelled character context.
                int background = narrative.IndexOf(IllustratorText.Raw("Residual_NarrativeFactRouter_7de9dd9804ba"), StringComparison.Ordinal);
                int localizedBackground = narrative.IndexOf(IllustratorText.Get("Hero_292"), StringComparison.Ordinal);
                if (localizedBackground >= 0 && (background < 0 || localizedBackground < background)) background = localizedBackground;
                if (background >= 0)
                {
                    if (HasNarrativeEcho(output, narrative.Substring(background), hardFacts)) return true;
                    narrative = narrative.Substring(0, background);
                }
            }
            var quotedOrSpokenOutput = new List<string>();
            if (allowVisualNarration)
            {
                foreach (Match quote in Quotes.Matches(output)) quotedOrSpokenOutput.Add(quote.Value);
                foreach (string sentence in Parts(output))
                    if (SpeechOrWrittenText.IsMatch(sentence)) quotedOrSpokenOutput.Add(sentence);
            }
            var candidates = new List<string>(Parts(narrative));
            foreach (Match quote in Quotes.Matches(narrative)) candidates.Add(quote.Value.Trim('“', '”', '"', '「', '」', '『', '』'));
            foreach (string candidate in candidates)
            {
                string text = candidate.Trim(' ', '。', '！', '？', '!', '?', '"', '“', '”');
                if (text.Length < 16) continue; // 提高阈值至16字符（整句引用），避免常规生平/地名/身份描述短语误杀整篇导演扩写。
                if (output.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0 ||
                    (hardFacts ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!allowVisualNarration) return true;
                // Reject copied dialogue presented as speech/writing, not an unquoted
                // visual action that the director retained from the current turn.
                foreach (string quotedOrSpoken in quotedOrSpokenOutput)
                    if (quotedOrSpoken.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
