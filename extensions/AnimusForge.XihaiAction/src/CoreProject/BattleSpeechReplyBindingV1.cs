using System;
using System.Text.RegularExpressions;

namespace AnimusForge.SceneActions.Core
{
    public static class BattleSpeechReplyBindingV1
    {
        public const double DefaultCandidateTtlSeconds = 60d;

        public static string Fingerprint(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = CommandParser.Normalize(text).Replace("\r", string.Empty);
            normalized = Regex.Replace(
                normalized,
                "<think\\b[^>]*>.*?</think>",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            normalized = Regex.Replace(
                normalized,
                "\\[REASONING\\].*?(?=\\[CONTENT\\]|$)",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            normalized = Regex.Replace(
                normalized,
                "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|NO_CONTINUE|END|RELAY\\s*:[^\\]]*|CONTENT)\\]",
                " ",
                RegexOptions.IgnoreCase);
            normalized = Regex.Replace(
                normalized,
                "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)",
                " ",
                RegexOptions.IgnoreCase);
            normalized = Regex.Replace(
                normalized,
                "\\*\\*.*?\\*\\*|\\*.*?\\*|（.*?）|\\(.*?\\)",
                " ",
                RegexOptions.Singleline);
            normalized = Regex.Replace(
                normalized,
                "^【[^】\\r\\n]{1,40}】",
                " ");
            return string.Join(
                " ",
                normalized.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries));
        }

        public static bool RequestMatches(string expectedRequest, string observedRequest)
        {
            string expected = Fingerprint(expectedRequest);
            string observed = Fingerprint(observedRequest);
            return expected.Length > 0 &&
                   string.Equals(expected, observed, StringComparison.Ordinal);
        }

        public static bool ReplyMatches(string expectedReply, string displayedReply)
        {
            string expected = Fingerprint(expectedReply);
            string displayed = Fingerprint(displayedReply);
            if (expected.Length == 0 || displayed.Length == 0)
            {
                return false;
            }
            return string.Equals(expected, displayed, StringComparison.Ordinal);
        }

        public static bool IsFresh(
            double now,
            double observedAt,
            double ttlSeconds = DefaultCandidateTtlSeconds)
        {
            return !double.IsNaN(now) &&
                   !double.IsInfinity(now) &&
                   !double.IsNaN(observedAt) &&
                   !double.IsInfinity(observedAt) &&
                   ttlSeconds >= 0d &&
                   now >= observedAt &&
                   now - observedAt <= ttlSeconds;
        }
    }
}
