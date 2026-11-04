using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

namespace AnimusForge;

// Current sources are exact and known. Other events enter only through topic
// retrieval, never through a recent-event quota. The saved archive is unchanged.
internal static class WorldDiplomacyRequestHistoryApplication
{
    internal static string ContextMarker => DiplomacyMiscPromptTexts.Get("History_ContextMarker", "[Relevant Diplomatic History for this Event]");
    // Retrieval sizing only; this is not a request rejection limit.
    internal const int DeclarationContextBudget = 32000;
    internal const int CompressionInputLimit = 128000;
    internal const int ContextCharacterLimit = 16000;
    private static readonly ConditionalWeakTable<WorldDiplomacyStorage, DocumentIndex> Indexes = new();
    internal static void InvalidateDocumentRouting(WorldDiplomacyStorage storage) => Indexes.Remove(storage);
    internal static long InputLimit(WorldDiplomacyJob job, long configuredLimit) =>
        WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress")
            ? Math.Min(configuredLimit, CompressionInputLimit) : long.MaxValue;
    internal static long WindowBudget(WorldDiplomacyJob job, Func<string, int> estimateTokens)
    {
        long Count(string value) => Math.Max(0, estimateTokens?.Invoke(value ?? "") ?? (value?.Length ?? 0));
        return Math.Max(0L, Math.Min(16000L, DeclarationContextBudget - Count(job?.SystemPrompt) - Count(job?.UserPrompt) - 4096L));
    }
    internal static string Build(WorldDiplomacyStorage storage, WorldDiplomacyJob job, int currentDay,
        long tokenBudget = 16000L, Func<string, int> estimateTokens = null, Func<string, float[]> embed = null)
    {
        if (storage == null || job == null || tokenBudget <= 0) return "";
        long Count(string value) => Math.Max(0, estimateTokens?.Invoke(value) ?? value.Length);
        var text = new StringBuilder(); long remaining = tokenBudget;
        bool Add(string row)
        {
            if (string.IsNullOrWhiteSpace(row)) return false;
            long cost = Count(row) + Environment.NewLine.Length;
            if (cost > remaining || text.Length + row.Length + Environment.NewLine.Length > ContextCharacterLimit) return false;
            text.AppendLine(row); remaining -= cost; return true;
        }
        if (!Add(ContextMarker)) return "";
        var index = Indexes.GetValue(storage, _ => new DocumentIndex()); index.Update(storage.Documents);
        var known = WorldDiplomacyRoundLifecycleRules.CollectKnownDocumentIds(null,
            storage.NobleKnowledge, storage.KingdomKnowledge, null, job.AuthorKingdomId, true, true);
        foreach (var id in index.Own(job.AuthorKingdomId)) known.Add(id);
        bool Knows(WorldDiplomacyDocument d) => d?.IsReadyForPublication == true && known.Contains(d.DocumentId ?? "");
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Document(WorldDiplomacyDocument d)
        {
            if (!Knows(d) || selected.Contains(d.DocumentId ?? "")) return;
            if (Add(JsonConvert.SerializeObject(new { source = d.DocumentId, GameDate = DiplomacyPromptTexts.ProjectCalendarDate(d.GameDate, DiplomacyPromptTexts.Get), d.AuthorKingdomId, d.TargetKingdomId,
                d.Title, d.Body, d.Intent, d.Commitment, d.ChangedDiplomaticState, d.MechanicalResult }))) selected.Add(d.DocumentId);
        }
        Document(index.Find(job.SourceDocumentId));
        foreach (string id in (job.TriggerDocumentIds ?? new()).Take(8)) Document(index.Find(id));
        string roundId = string.IsNullOrWhiteSpace(job.RoundId) ? job.ExchangeId : job.RoundId;
        foreach (var d in index.Round(roundId).Where(Knows).Take(8)) Document(d);
        var round = WorldDiplomacyLiveRoundRules.Live(storage).Concat(storage.CompletedRounds ?? new())
            .FirstOrDefault(x => x?.RoundId == roundId);
        var source = index.Find(job.SourceDocumentId);
        string query = BuildRecallQuery(storage, job, round, Knows(source) ? source : null, Knows);
        // No topic means no historical filler. Never use the system rules as a query.
        if (!string.IsNullOrWhiteSpace(query))
        {
            var snapshot = new WorldDiplomacyMemorySnapshot(storage, job.AuthorKingdomId, known, job.PersonalMemoryRulerId);
            string recalled = WorldDiplomacyMemoryApplication.Build(snapshot, query, day => day.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Math.Min(7000, ContextCharacterLimit - text.Length), embed, selected, deferInitialIndex: embed != null);
            foreach (string row in recalled.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) Add(row);
        }
        return text.ToString();
    }
    internal const int RecallQueryCharacterLimit = 1600;
    // The event topic and the delivered source define the subject. An NPC agenda
    // opened toward the player has neither yet, so its live round falls back to its
    // own seed and then to the titles of the pair's latest known documents. Without
    // a live round there is still no subject and no filler.
    internal static string BuildRecallQuery(WorldDiplomacyStorage storage, WorldDiplomacyJob job,
        WorldDiplomacyRound round, WorldDiplomacyDocument knownSource, Func<WorldDiplomacyDocument, bool> knows)
    {
        string query = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round?.RoundTopic) + " "
            + (knownSource == null ? "" : knownSource.Title + " " + knownSource.Body);
        if (string.IsNullOrWhiteSpace(query) && round != null)
            query = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round.TopicSeedContext, round.EventBackground);
        if (string.IsNullOrWhiteSpace(query) && round != null && !string.IsNullOrEmpty(job.AuthorKingdomId)
            && !string.IsNullOrEmpty(job.TargetKingdomId))
        {
            var titles = new List<string>();
            for (int i = (storage.Documents?.Count ?? 0) - 1; i >= 0 && titles.Count < 3; i--)
            {
                var d = storage.Documents[i];
                if (d == null || !knows(d) || string.IsNullOrWhiteSpace(d.Title)) continue;
                bool pair = (d.AuthorKingdomId == job.AuthorKingdomId && d.TargetKingdomId == job.TargetKingdomId)
                    || (d.AuthorKingdomId == job.TargetKingdomId && d.TargetKingdomId == job.AuthorKingdomId);
                if (pair) titles.Add(d.Title);
            }
            query = string.Join(" ", titles);
        }
        query = query.Trim();
        if (query.Length <= RecallQueryCharacterLimit) return query;
        int length = RecallQueryCharacterLimit;
        if (char.IsHighSurrogate(query[length - 1])) length--;
        return query.Substring(0, length);
    }
    private sealed class DocumentIndex
    {
        private List<WorldDiplomacyDocument> _documents;
        private WorldDiplomacyDocument _last;
        private int _count;
        private readonly Dictionary<string, WorldDiplomacyDocument> _byId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<WorldDiplomacyDocument>> _byRound = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _own = new(StringComparer.OrdinalIgnoreCase);
        internal void Update(List<WorldDiplomacyDocument> documents)
        {
            // Append-only growth is consumed incrementally. A replaced list, a shorter
            // list, or an in-place replacement of the last indexed slot rebuilds.
            if (!ReferenceEquals(_documents, documents) || (documents?.Count ?? 0) < _count
                || (_count > 0 && !ReferenceEquals(documents[_count - 1], _last)))
            { _documents = documents; _count = 0; _last = null; _byId.Clear(); _byRound.Clear(); _own.Clear(); }
            if (documents == null) return;
            for (; _count < documents.Count; _count++)
            {
                var d = _last = documents[_count]; if (d == null || string.IsNullOrEmpty(d.DocumentId)) continue;
                _byId[d.DocumentId] = d;
                if (!string.IsNullOrEmpty(d.AuthorKingdomId))
                {
                    if (!_own.TryGetValue(d.AuthorKingdomId, out var ids)) _own[d.AuthorKingdomId] = ids = new(StringComparer.OrdinalIgnoreCase);
                    ids.Add(d.DocumentId);
                }
                if (string.IsNullOrEmpty(d.RoundId)) continue;
                if (!_byRound.TryGetValue(d.RoundId, out var rows)) _byRound[d.RoundId] = rows = new();
                rows.Add(d); rows.Sort((a, b) => a.Day != b.Day ? b.Day.CompareTo(a.Day) : b.CreatedUtcTicks.CompareTo(a.CreatedUtcTicks));
                if (rows.Count > 32) rows.RemoveAt(rows.Count - 1);
            }
        }
        internal IEnumerable<string> Own(string id) => id != null && _own.TryGetValue(id, out var rows) ? rows : Enumerable.Empty<string>();
        internal WorldDiplomacyDocument Find(string id) => id != null && _byId.TryGetValue(id, out var row) ? row : null;
        internal IEnumerable<WorldDiplomacyDocument> Round(string id) => id != null && _byRound.TryGetValue(id, out var rows) ? rows : Enumerable.Empty<WorldDiplomacyDocument>();
    }
}
