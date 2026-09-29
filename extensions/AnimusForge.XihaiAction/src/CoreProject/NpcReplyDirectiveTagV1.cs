using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge.SceneActions.Core
{
    /// <summary>
    /// Closed tag codec for the NPC reply action decided by AF's unified
    /// postprocess.  The postprocess model only names logical V4 intent keys;
    /// the runtime still owns actor, target, consent, evidence and playback.
    /// </summary>
    public static class NpcReplyDirectiveTagV1
    {
        public const string TagPrefix = "[ACTION:SCENE_ACT:";
        public const string RuleTemplateTag = "[ACTION:SCENE_ACT:动作程序]";
        public const string NoneValue = "NONE";
        public const string CatalogPlaceholder = "{scene_action_catalog}";

        private static readonly Regex TagPattern = new Regex(
            "\\[ACTION:SCENE_ACT:([^\\]\\r\\n]{0,96})\\]",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static bool ContainsTag(string text)
        {
            return !string.IsNullOrEmpty(text) && TagPattern.IsMatch(text);
        }

        public static string StripTags(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }
            return TagPattern.Replace(text, string.Empty);
        }

        /// <summary>
        /// Extracts the first valid tag value, returns the text with every tag
        /// removed, and normalizes the value to NONE or a V4 program expression.
        /// Unknown keys, keys outside <paramref name="allowedIntentKeys"/>, and
        /// malformed programs are rejected as a whole.
        /// </summary>
        public static bool TryExtract(
            string text,
            IEnumerable<string> allowedIntentKeys,
            out string normalizedValue,
            out string remainingText,
            out string error)
        {
            normalizedValue = null;
            error = null;
            remainingText = StripTags(text);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            Match match = TagPattern.Match(text);
            if (!match.Success)
            {
                return false;
            }
            return TryNormalizeValue(
                match.Groups[1].Value,
                allowedIntentKeys,
                out normalizedValue,
                out error);
        }

        public static bool TryNormalizeValue(
            string rawValue,
            IEnumerable<string> allowedIntentKeys,
            out string normalizedValue,
            out string error)
        {
            normalizedValue = null;
            error = null;
            string value = (rawValue ?? string.Empty).Trim();
            if (string.Equals(value, NoneValue, StringComparison.OrdinalIgnoreCase))
            {
                normalizedValue = NoneValue;
                return true;
            }
            value = value.ToLowerInvariant();
            if (!ActionProgramV4.TryParseExpression(value, out ActionProgramV4 program, out error))
            {
                return false;
            }
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            string outside = program.Steps
                .SelectMany(step => step.IntentKeys)
                .FirstOrDefault(key => !allowed.Contains(key));
            if (outside != null)
            {
                error = "Directive selected an intent outside the frozen allow-list: " + outside;
                return false;
            }
            normalizedValue = program.ProtocolExpression;
            return true;
        }

        public static string BuildTag(string normalizedValue)
        {
            return TagPrefix + normalizedValue + "]";
        }

        /// <summary>
        /// Converts a normalized directive value to the existing closed
        /// classifier protocol so the runtime reuses one validation path.
        /// </summary>
        public static string ToClassifierProtocolLine(string normalizedValue)
        {
            if (string.IsNullOrEmpty(normalizedValue) ||
                string.Equals(normalizedValue, NoneValue, StringComparison.Ordinal))
            {
                return NoneValue;
            }
            return normalizedValue.IndexOf('>') < 0 && normalizedValue.IndexOf('+') < 0
                ? "PLAY_ACTION " + normalizedValue
                : "PLAY_PROGRAM " + normalizedValue;
        }

        /// <summary>
        /// One line per allowed key: "key（中文名）：语义".  Runtime facts only;
        /// the judging rules live in ActionPostprocessPrompts.json.
        /// </summary>
        public static string BuildCatalogText(IEnumerable<string> allowedIntentKeys)
        {
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            StringBuilder builder = new StringBuilder();
            foreach (SceneActionContractEntryV4 entry in SceneActionFrameworkV4.LogicalActions)
            {
                if (!allowed.Contains(entry.IntentKey))
                {
                    continue;
                }
                builder.Append(entry.IntentKey)
                    .Append('（').Append(entry.DisplayNameZhCn).Append("）：")
                    .Append(entry.SemanticDescriptionZhCn)
                    .Append('\n');
            }
            return builder.ToString().TrimEnd('\n');
        }
    }
}
