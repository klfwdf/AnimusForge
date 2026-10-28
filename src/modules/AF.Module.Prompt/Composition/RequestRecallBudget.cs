using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Request-only transport markers. Never persist these strings or expose them as speech.
internal sealed class RequestRecallBudget
{
    private const string Diplomatic = "<af-recall-diplomacy>";
    private const string Personal = "<af-recall-personal>";
    private const string End = "</af-recall>";
    internal const int MaximumChars = 16000;
    private sealed class Part { internal int Section, Start, Length; internal string Text, Selected; internal bool Diplomacy; }
    private readonly string[] _sections;
    private readonly List<Part> _parts = new();
    private readonly HashSet<string> _texts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _documentBodies = new(StringComparer.Ordinal);
    private int _remaining;
    internal bool Active { get; }
    internal int UsedChars { get; private set; }
    internal static string WrapDiplomacy(string text) => Wrap(Diplomatic, text);
    internal static string WrapPersonal(string text) => Wrap(Personal, text);
    private static string Wrap(string marker, string text) => string.IsNullOrWhiteSpace(text) ? text : marker + "\n" + text + "\n" + End;

    internal RequestRecallBudget(IEnumerable<string> sections, int maximumChars = MaximumChars)
    {
        _remaining = Math.Max(0, maximumChars);
        _sections = (sections ?? Enumerable.Empty<string>()).Select(x => x ?? "").ToArray();
        for (int i = 0; i < _sections.Length; i++)
        {
            string text = _sections[i]; int offset = 0;
            while (offset < text.Length)
            {
                int a = text.IndexOf(Diplomatic, offset, StringComparison.Ordinal), b = text.IndexOf(Personal, offset, StringComparison.Ordinal);
                if (a < 0 && b < 0) break;
                bool diplomatic = a >= 0 && (b < 0 || a < b); int start = diplomatic ? a : b;
                int body = start + (diplomatic ? Diplomatic.Length : Personal.Length);
                int end = text.IndexOf(End, body, StringComparison.Ordinal);
                if (end < 0) break; // Unknown/malformed input is not interpreted as a memory boundary.
                _parts.Add(new Part { Section = i, Start = start, Length = end + End.Length - start,
                    Text = text.Substring(body, end - body).Trim(), Diplomacy = diplomatic });
                offset = end + End.Length;
            }
        }
        Active = _parts.Any(x => x.Diplomacy);
        foreach (var part in _parts.Where(x => x.Diplomacy))
        {
            part.Selected = TakeParagraphs(part.Text, true);
            CaptureDocumentBodies(part.Selected);
        }
    }
    private bool Spend(int chars)
    {
        if (chars > _remaining) return false;
        _remaining -= chars; UsedChars += chars; return true;
    }
    private string TakeParagraphs(string text, bool lines = false)
    {
        if (!Active) return text;
        var selected = new List<string>();
        // Keep whole source rows/paragraphs; dropping a request projection never deletes the archive.
        foreach (string item in Regex.Split((text ?? "").Replace("\r", ""), lines ? "\n" : "\n[ \\t]*\n").Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            string key = item.Trim();
            if (_texts.Contains(key) || !Spend(item.Length + (selected.Count > 0 ? 2 : 0))) continue;
            _texts.Add(key); selected.Add(item);
        }
        return string.Join("\n\n", selected);
    }
    internal string[] FinishSections()
    {
        foreach (var part in _parts.Where(x => !x.Diplomacy)) part.Selected ??= TakeParagraphs(part.Text);
        var result = (string[])_sections.Clone();
        foreach (var part in _parts.OrderByDescending(x => x.Start))
            result[part.Section] = result[part.Section].Remove(part.Start, part.Length).Insert(part.Start, part.Selected ?? part.Text);
        return result;
    }
    internal List<ConversationMessage> SelectMessages(IEnumerable<ConversationMessage> messages,
        Func<ConversationMessage, int> projectedChars, Func<ConversationMessage, bool> current = null)
    {
        var original = (messages ?? Enumerable.Empty<ConversationMessage>()).Where(x => x != null).ToList();
        if (!Active) return original;
        var keep = new HashSet<ConversationMessage>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = original.Count - 1; i >= 0; i--)
        {
            var entry = original[i];
            if (current?.Invoke(entry) == true) { keep.Add(entry); continue; }
            var group = new List<ConversationMessage> { entry };
            // Preserve adjacent turn pairs as a unit, so an isolated "yes" never acquires a different question.
            if (entry.Role == "assistant" && i > 0 && original[i - 1].Role == "user"
                && current?.Invoke(original[i - 1]) != true) group.Insert(0, original[--i]);
            group = group.Where(x => !DuplicatesDiplomaticDocument(x)).ToList();
            int cost = group.Sum(x => Math.Max(0, projectedChars(x)));
            if (cost == 0 || cost > _remaining) continue;
            var fresh = new List<ConversationMessage>();
            foreach (var item in group)
            {
                // Unknown provenance or the same words on another day are not duplicate evidence.
                string identity = item.EventSequence + "|" + item.PromptMemorySessionKey + "|" + item.GameDayIndex + "|" + item.GameHour + "|" + item.Role
                    + "|" + item.SpeakerHeroId + "|" + item.SpeakerName + "|" + item.TargetHeroId + "|" + item.Content;
                if (item.EventSequence <= 0 || string.IsNullOrEmpty(item.PromptMemorySessionKey) || item.GameDayIndex < 0 || seen.Add(identity)) fresh.Add(item);
            }
            if (group.Count > 1 && fresh.Count > 0 && fresh.Count < group.Count) fresh = group;
            if (Spend(fresh.Sum(x => Math.Max(0, projectedChars(x))))) foreach (var item in fresh) keep.Add(item);
        }
        return original.Where(keep.Contains).ToList();
    }
    private static readonly Regex JsonField = new("\\\"(?<key>source|Body)\\\"\\s*:\\s*\\\"(?<text>(?:\\\\.|[^\\\"\\\\])*)\\\"", RegexOptions.Compiled);
    private void CaptureDocumentBodies(string text)
    {
        foreach (string line in text.Split('\n'))
        {
            string source = null, body = null;
            try
            {
                foreach (Match match in JsonField.Matches(line))
                    if (match.Groups["key"].Value == "source") source = Regex.Unescape(match.Groups["text"].Value.Replace("\\/", "/"));
                    else body = Regex.Unescape(match.Groups["text"].Value.Replace("\\/", "/"));
            }
            catch { continue; }
            if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(body)) _documentBodies[source] = body;
        }
    }
    internal bool IsDuplicateDiplomaticCopy(ConversationMessage item) => item != null && DuplicatesDiplomaticDocument(item);
    private bool DuplicatesDiplomaticDocument(ConversationMessage item)
    {
        if (item.Role != "system") return false;
        string text = item.Content ?? "";
        // Recognize only exact old producer wrappers; ordinary recollections/quotes and extra facts survive.
        string[] prefixes = { "I have officially published a diplomatic document, source=", "The court where I reside has officially received a diplomatic document, source=",
            PromptComposerTexts.Raw("recall_legacy_diplomacy_published"), PromptComposerTexts.Raw("recall_legacy_diplomacy_received") };
        foreach (string prefix in prefixes)
        {
            int start = text.IndexOf(prefix, StringComparison.Ordinal); if (start < 0) continue;
            if (start > 0 && !string.Equals(text.Substring(0, start).Trim(), PromptComposerTexts.Raw("afef_npc_action"), StringComparison.Ordinal)) continue;
            start += prefix.Length; int stop = text.IndexOfAny(new[] { ',', PromptComposerTexts.Raw("recall_legacy_source_separator")[0] }, start); if (stop < 0) continue;
            if (!_documentBodies.TryGetValue(text.Substring(start, stop - start), out var body)) continue;
            string marker = prefix[0] > 127 ? PromptComposerTexts.Raw("recall_legacy_diplomacy_body") : ". Original text: ";
            string ending = prefix[0] > 127 ? PromptComposerTexts.Raw("recall_legacy_diplomacy_ending")
                : ". This entry only confirms the document was published or received; it does not prove the proposal was accepted or diplomatic actions executed.";
            int original = text.IndexOf(marker, stop, StringComparison.Ordinal);
            if (original >= 0 && text.EndsWith(ending, StringComparison.Ordinal)
                && text.Substring(original + marker.Length, text.Length - original - marker.Length - ending.Length) == body) return true;
        }
        return false;
    }
}
