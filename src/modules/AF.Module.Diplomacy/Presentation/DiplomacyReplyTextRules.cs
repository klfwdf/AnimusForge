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
    private sealed class ReceiptPattern { public string Key { get; set; } public string Value { get; set; } }
    private sealed class ReceiptCorpus { public string[] ReplyWrappers { get; set; } public ReceiptPattern[] Patterns { get; set; } }
    private static readonly Lazy<ReceiptCorpus> Corpus = new Lazy<ReceiptCorpus>(() =>
    {
        using (var stream = typeof(DiplomacyReplyTextRules).Assembly.GetManifestResourceStream("AnimusForge.Diplomacy.ReceiptCompatibility.json"))
        using (var reader = new StreamReader(stream ?? throw new InvalidDataException("Missing embedded diplomacy receipt compatibility.")))
            return JsonConvert.DeserializeObject<ReceiptCorpus>(reader.ReadToEnd());
    });

    internal static string CleanNpcText(string text, string speakerName = null)
    {
        if (string.IsNullOrEmpty(text)) return text;
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
            foreach (string wrapper in Corpus.Value.ReplyWrappers)
                if (line.StartsWith(wrapper, StringComparison.Ordinal)) { line = line.Substring(wrapper.Length).TrimStart(); break; }
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
        foreach (var pattern in Corpus.Value.Patterns) AddPattern(patterns, pattern.Key, pattern.Value);
        return new Regex("\\A(?:" + string.Join("|", patterns) + ")\\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
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
