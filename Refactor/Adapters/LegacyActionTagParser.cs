using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Detached parser for the legacy ACTION tag protocol. Parsing is deliberately
/// separate from game execution: this type produces an immutable ActionPlan
/// and never resolves a Hero, Agent, item, settlement or other game object.
/// </summary>
public sealed class LegacyActionTagParser : IActionPostprocessor
{
    private static readonly string[] SupportedFamilies =
    {
        "ACTION", "A", "ADP", "AD", "ASS", "GUI", "ATT", "ATP", "RELAY", "FOL", "STP", "END"
    };

    private readonly int _maxActions;

    public LegacyActionTagParser(int maxActions = 64)
    {
        _maxActions = Math.Max(1, maxActions);
    }

    public ActionPlan Parse(string rawText, PostprocessContext context)
    {
        List<ActionRequest> actions = new List<ActionRequest>();
        string raw = rawText ?? string.Empty;
        if (context == null || context.AllowedTagFamilies.Count == 0)
        {
            return new ActionPlan(actions, raw);
        }

        foreach (string candidate in ExtractCandidates(raw))
        {
            if (actions.Count >= _maxActions)
            {
                break;
            }
            int separator = candidate.IndexOf(':');
            string body = candidate.Length > 2 ? candidate.Substring(1, candidate.Length - 2) : string.Empty;
            separator = body.IndexOf(':');
            string family = (separator >= 0 ? body.Substring(0, separator) : body).Trim().ToUpperInvariant();
            string payload = separator >= 0 ? body.Substring(separator + 1) : string.Empty;
            string[] segments = payload.Split(new[] { ':' }, StringSplitOptions.None);
            string normalizedTag = BuildNormalizedTag(family, segments);
            if (!IsAllowed(normalizedTag, context.AllowedTagFamilies))
            {
                continue;
            }

            string targetId = GetTargetId(family, segments);
            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int parameterStart = GetParameterStart(family, segments);
            for (int i = parameterStart; i < segments.Length; i++)
            {
                string segment = (segments[i] ?? string.Empty).Trim();
                if (segment.Length == 0)
                {
                    continue;
                }
                int equalsIndex = segment.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string key = segment.Substring(0, equalsIndex).Trim();
                    if (key.Length > 0)
                    {
                        parameters[key] = segment.Substring(equalsIndex + 1).Trim();
                    }
                }
                else
                {
                    parameters[IsGiveAssetAction(family, segments) && i == segments.Length - 1
                        ? "quantity"
                        : "arg" + (i - 1)] = segment;
                }
            }
            actions.Add(new ActionRequest(normalizedTag, targetId, parameters));
        }
        return new ActionPlan(actions, raw);
    }

    /// <summary>
    /// Returns true when the raw postprocess output contains a recognized
    /// protocol tag that is not present in the supplied allowlist. Ignoring
    /// such a tag would let the raw trace differ from the authorized plan
    /// without notifying the executor.
    /// </summary>
    public bool HasDisallowedProtocolTag(string rawText, PostprocessContext context)
    {
        if (context == null || context.AllowedTagFamilies.Count == 0)
        {
            return ExtractCandidates(rawText).Any();
        }
        foreach (string candidate in ExtractCandidates(rawText))
        {
            string normalizedTag = NormalizeCandidateTag(candidate);
            if (!IsAllowed(normalizedTag, context.AllowedTagFamilies))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Removes only recognized protocol candidates selected by the caller.
    /// Balanced scanning is shared with Parse so RichText such as [ROT] inside
    /// a GIVE_ASSET token is preserved and duplicate tags remain ordered.
    /// </summary>
    public static string RemoveProtocolTags(string rawText, Func<string, bool> shouldRemove)
    {
        string raw = rawText ?? string.Empty;
        if (raw.Length == 0 || shouldRemove == null)
        {
            return raw;
        }

        StringBuilder result = new StringBuilder(raw.Length);
        int cursor = 0;
        foreach (ProtocolTagSpan span in ExtractCandidateSpans(raw))
        {
            string normalizedTag = NormalizeCandidateTag(span.Value);
            bool remove;
            try
            {
                remove = shouldRemove(normalizedTag);
            }
            catch
            {
                remove = false;
            }
            if (!remove)
            {
                continue;
            }

            if (span.Start > cursor)
            {
                result.Append(raw, cursor, span.Start - cursor);
            }
            cursor = span.Start + span.Length;
        }

        if (cursor == 0)
        {
            return raw;
        }
        if (cursor < raw.Length)
        {
            result.Append(raw, cursor, raw.Length - cursor);
        }
        return result.ToString();
    }

    /// <summary>
    /// Extracts balanced protocol tags instead of using a simple closing
    /// bracket regex. Existing GIVE_ASSET tokens may contain RichText such as
    /// <c>[ROT]</c>; treating that inner pair as the end of the outer action
    /// silently drops valid Reward/Trade actions. If a malformed candidate
    /// contains a later protocol opener, recovery starts at that later opener
    /// so one broken tag cannot consume the next valid tag.
    /// </summary>
    private static IEnumerable<string> ExtractCandidates(string text)
    {
        return ExtractCandidateSpans(text).Select(span => span.Value);
    }

    private static IEnumerable<ProtocolTagSpan> ExtractCandidateSpans(string text)
    {
        string raw = text ?? string.Empty;
        int candidateStart = -1;
        int depth = 0;
        for (int i = 0; i < raw.Length; i++)
        {
            if (raw[i] == '[')
            {
                if (candidateStart < 0)
                {
                    if (IsProtocolOpener(raw, i))
                    {
                        candidateStart = i;
                        depth = 1;
                    }
                    continue;
                }

                // A nested protocol opener means the outer candidate is
                // malformed. Recover at the nested opener; ordinary nested
                // markup (for example [ROT]) remains part of the candidate.
                if (IsProtocolOpener(raw, i))
                {
                    candidateStart = i;
                    depth = 1;
                }
                else
                {
                    depth++;
                }
                continue;
            }

            if (raw[i] != ']' || candidateStart < 0)
            {
                continue;
            }

            depth--;
            if (depth != 0)
            {
                continue;
            }

            string candidate = raw.Substring(candidateStart, i - candidateStart + 1);
            int candidateLength = i - candidateStart + 1;
            int candidateStartOffset = candidateStart;
            candidateStart = -1;
            if (IsProtocolCandidate(candidate))
            {
                yield return new ProtocolTagSpan(candidateStartOffset, candidateLength, candidate);
            }
        }
    }

    private struct ProtocolTagSpan
    {
        public ProtocolTagSpan(int candidateStart, int candidateLength, string value)
        {
            Start = candidateStart;
            Length = candidateLength;
            Value = value ?? string.Empty;
        }

        public int Start { get; }
        public int Length { get; }
        public string Value { get; }
    }

    private static bool IsProtocolCandidate(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || candidate[0] != '[' || candidate[candidate.Length - 1] != ']')
        {
            return false;
        }
        return IsProtocolOpener(candidate, 0);
    }

    private static string NormalizeCandidateTag(string candidate)
    {
        string body = candidate.Length > 2 ? candidate.Substring(1, candidate.Length - 2) : string.Empty;
        int separator = body.IndexOf(':');
        string family = (separator >= 0 ? body.Substring(0, separator) : body).Trim().ToUpperInvariant();
        string payload = separator >= 0 ? body.Substring(separator + 1) : string.Empty;
        return BuildNormalizedTag(family, payload.Split(new[] { ':' }, StringSplitOptions.None));
    }

    private static bool IsProtocolOpener(string text, int start)
    {
        if (string.IsNullOrEmpty(text) || start < 0 || start >= text.Length || text[start] != '[')
        {
            return false;
        }
        int familyStart = start + 1;
        foreach (string family in SupportedFamilies)
        {
            if (familyStart + family.Length > text.Length
                || string.Compare(text, familyStart, family, 0, family.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                continue;
            }
            int next = familyStart + family.Length;
            if (next >= text.Length || text[next] == ':' || text[next] == ']')
            {
                return true;
            }
        }
        return false;
    }

    private static string BuildNormalizedTag(string family, string[] segments)
    {
        string normalizedFamily = (family ?? string.Empty).Trim().ToUpperInvariant();
        string actionName = segments != null && segments.Length > 0
            ? (segments[0] ?? string.Empty).Trim()
            : string.Empty;
        if (normalizedFamily == "ACTION" || normalizedFamily == "A")
        {
            return string.IsNullOrWhiteSpace(actionName)
                ? normalizedFamily
                : normalizedFamily + ":" + actionName;
        }
        return normalizedFamily;
    }

    private static string GetTargetId(string family, string[] segments)
    {
        if (segments == null || segments.Length == 0)
        {
            return string.Empty;
        }
        string normalizedFamily = (family ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedFamily == "ACTION")
        {
            if (IsGiveAssetAction(normalizedFamily, segments) && segments.Length >= 3)
            {
                return string.Join(":", segments.Skip(1).Take(segments.Length - 2)).Trim();
            }
            return segments.Length > 1 ? (segments[1] ?? string.Empty).Trim() : string.Empty;
        }
        if (normalizedFamily == "AD" || normalizedFamily == "ADP")
        {
            return (segments[0] ?? string.Empty).Trim();
        }
        if (normalizedFamily == "A")
        {
            return segments.Length > 1 ? (segments[1] ?? string.Empty).Trim() : string.Empty;
        }
        return segments.Length > 0 ? (segments[0] ?? string.Empty).Trim() : string.Empty;
    }

    private static int GetParameterStart(string family, string[] segments)
    {
        string normalizedFamily = (family ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedFamily == "ACTION" || normalizedFamily == "A")
        {
            if (IsGiveAssetAction(normalizedFamily, segments) && segments.Length >= 3)
            {
                return segments.Length - 1;
            }
            return Math.Min(2, segments?.Length ?? 0);
        }
        return Math.Min(1, segments?.Length ?? 0);
    }

    private static bool IsGiveAssetAction(string family, string[] segments)
    {
        return (string.Equals(family, "ACTION", StringComparison.OrdinalIgnoreCase)
                || string.Equals(family, "A", StringComparison.OrdinalIgnoreCase))
            && segments != null
            && segments.Length > 0
            && string.Equals(segments[0], "GIVE_ASSET", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowed(string tag, IEnumerable<string> allowedFamilies)
    {
        string normalizedTag = NormalizeProtocolText(tag);
        string tagFamily = GetFamily(normalizedTag);
        foreach (string family in allowedFamilies ?? Enumerable.Empty<string>())
        {
            string normalized = NormalizeProtocolText(family);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }
            if (normalized == "*"
                || normalized == tagFamily
                || normalized == tagFamily + ":*"
                || normalized == normalizedTag
                || (normalized.EndsWith("*", StringComparison.Ordinal)
                    && normalizedTag.StartsWith(normalized.Substring(0, normalized.Length - 1), StringComparison.Ordinal))
                || (normalized.EndsWith(":*", StringComparison.Ordinal)
                    && normalizedTag.StartsWith(normalized.Substring(0, normalized.Length - 1), StringComparison.Ordinal))
                || IsParameterizedHeroJoinTemplate(normalized, normalizedTag))
            {
                return true;
            }
        }
        return false;
    }

    private static string NormalizeProtocolText(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal) && normalized.EndsWith("]", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(1, normalized.Length - 2).Trim();
        }
        return normalized.ToUpperInvariant();
    }

    private static string GetFamily(string tag)
    {
        int separator = (tag ?? string.Empty).IndexOf(':');
        return separator > 0 ? tag.Substring(0, separator) : tag ?? string.Empty;
    }

    private static bool IsParameterizedHeroJoinTemplate(string allowed, string actual)
    {
        if (!allowed.Equals("A:H_J_P_P_C&L", StringComparison.OrdinalIgnoreCase)
            && !allowed.Equals("A:H_J_P_P_C/L", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return actual.Equals("A:H_J_P_P_C", StringComparison.OrdinalIgnoreCase)
            || actual.Equals("A:H_J_P_P_L", StringComparison.OrdinalIgnoreCase);
    }
}
