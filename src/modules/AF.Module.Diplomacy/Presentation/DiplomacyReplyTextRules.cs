using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

// NPC prose projection only. Player input, AFEF facts and stored records are opaque.
// Both canonical languages and UI variants are embedded, so old text remains identifiable
// after changing packs/UI language. Loaded once; no disk IO, Tick work or live game objects.
internal static class DiplomacyReplyTextRules
{
    private static readonly string[] Keys =
    {
        "OralDiplomacy_SubmissionRejected", "OralDiplomacy_Uncommitted", "OralDiplomacy_InvalidFormat",
        "OralDiplomacy_Unauthorized", "OralDiplomacy_SubmitFailedPrefix", "OralDiplomacy_MultipleProposals",
        "OralDiplomacy_UnavailableProposal", "OralDiplomacy_Published", "OralDiplomacy_Executed",
        "OralDiplomacy_DeferredPrefix", "OralDiplomacy_CancelledPrefix", "OralDiplomacy_NotReady",
        "OralDiplomacy_DuplicatePrefix", "OralDiplomacy_ReleaseSubjectUnavailable",
        "OralDiplomacy_ReleaseSubjectInvalidFormat", "OralDiplomacy_ReleaseSubjectStateChanged",
        "OralDiplomacy_CommitmentDisabled", "DialogueControl_InvalidFormat",
        "DialogueControl_AmbiguousSourceReason", "DialogueControl_AlreadyUpdatedPrefix",
        "DialogueControl_CannotWithdraw", "DialogueControl_WithdrawIncomplete", "DialogueControl_InvalidStatus",
        "DialogueControl_HasFormalResult", "DialogueControl_UpdatedPrefix", "DialogueControl_WithdrawStandalone",
        "DialogueControl_WithdrawUnauthorized", "DialogueControl_WithdrawSuccess",
        "Commitment_InvalidTerms", "Commitment_AiDisabled", "Commitment_UnsupportedAdditionalTerms",
        "Commitment_TributeMissingDetails", "Commitment_FormalPipelineRequired",
        "OralTag_VassalageUnavailable", "OralTag_AiDisabled", "SubjectRelease_InvalidFormat",
        "SubjectRelease_PreconditionsChanged", "SubjectRelease_AuthorityOrTreatyChanged",
        "SubjectRelease_UnconfirmedStillExists", "SubjectRelease_MustBeMainThread",
        "SubjectRelease_WorldDiplomacyUnavailable", "SubjectRelease_VassalageTerminatedFormat"
    };
    private static readonly Lazy<Regex> ReceiptLine = new Lazy<Regex>(BuildReceiptLine);
    private static readonly Lazy<Dictionary<string, string[]>> Variants = new Lazy<Dictionary<string, string[]>>(LoadVariants);

    internal static bool MatchesExact(string key, string text)
    {
        if (!Variants.Value.TryGetValue(key, out string[] variants)) return false;
        foreach (string variant in variants)
            if (string.Equals(variant, text, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static IEnumerable<string> SourcesFor(string key)
    {
        yield return DiplomacyMiscPromptTexts.Get(key, "");
        foreach (string id in new[] { key, "AF_DIP_STATUS_" + key })
            if (Variants.Value.TryGetValue(id, out string[] values))
                foreach (string value in values) yield return value;
    }

    internal static string CleanNpcText(string text, string speakerName = null)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string replyPrefix = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0462");
        StringBuilder result = null;
        int keptStart = 0;
        for (int start = 0; start < text.Length;)
        {
            int newline = text.IndexOf('\n', start);
            int end = newline < 0 ? text.Length : newline;
            string line = text.Substring(start, end - start).Trim();
            // These are established speech wrappers, not arbitrary labels or quoted text.
            if (!string.IsNullOrEmpty(speakerName))
            {
                if (line.StartsWith(speakerName, StringComparison.Ordinal) && line.Length > speakerName.Length
                    && (line[speakerName.Length] == ':' || line[speakerName.Length] == '：'))
                    line = line.Substring(speakerName.Length + 1).TrimStart();
            }
            if (line.StartsWith(replyPrefix, StringComparison.Ordinal)) line = line.Substring(replyPrefix.Length).TrimStart();
            int next = newline < 0 ? text.Length : newline + 1;
            if (line.Length > 0 && ReceiptLine.Value.IsMatch(line))
            {
                if (result == null) result = new StringBuilder(text.Length);
                result.Append(text, keptStart, start - keptStart);
                keptStart = next;
            }
            start = next;
        }
        if (result == null) return text;
        result.Append(text, keptStart, text.Length - keptStart);
        return result.ToString().TrimEnd('\r', '\n');
    }

    private static Regex BuildReceiptLine()
    {
        var patterns = new HashSet<string>(StringComparer.Ordinal);
        var values = Variants.Value;
        foreach (string key in Keys)
            foreach (string id in new[] { key, "AF_DIP_STATUS_" + key })
                if (values.TryGetValue(id, out string[] variants))
                    foreach (string value in variants) AddPattern(patterns, id, value);
        return new Regex("\\A(?:" + string.Join("|", patterns) + ")\\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static Dictionary<string, string[]> LoadVariants()
    {
        var assembly = typeof(DiplomacyReplyTextRules).Assembly;
        // Recognition-only legacy receipts: both languages remain readable after a pack switch.
        using (var stream = assembly.GetManifestResourceStream("AnimusForge.Diplomacy.ReceiptCompatibility.json"))
        using (var reader = new StreamReader(stream ?? throw new InvalidDataException("Missing embedded diplomacy receipt compatibility.")))
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string[]>>(reader.ReadToEnd());
        }
    }

    private static void AddPattern(HashSet<string> patterns, string key, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        // Match a whole known generated line. Quotes, mentions and ordinary prose are retained.
        string pattern = Regex.Escape(value.Trim());
        pattern = Regex.Replace(pattern, @"\\\{(?:[0-9]+|[A-Z_]+)}", _ => "[^\\r\\n]*");
        if (key.EndsWith("Prefix", StringComparison.Ordinal)) pattern += "[^\\r\\n]*";
        patterns.Add(pattern);
    }
}
