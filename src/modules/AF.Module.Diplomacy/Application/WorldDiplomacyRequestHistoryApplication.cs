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
    internal const int DeclarationInputLimit = 32000;
    internal const int CompressionInputLimit = 128000;
    internal const int ContextCharacterLimit = 16000;
    private static readonly ConditionalWeakTable<WorldDiplomacyStorage, DocumentIndex> Indexes = new();
    internal static void InvalidateDocumentRouting(WorldDiplomacyStorage storage) => Indexes.Remove(storage);
    internal static long InputLimit(WorldDiplomacyJob job, long configuredLimit) => Math.Min(configuredLimit,
        WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress") ? CompressionInputLimit : DeclarationInputLimit);
    internal static long WindowBudget(WorldDiplomacyJob job, Func<string, int> estimateTokens)
    {
        long Count(string value) => Math.Max(0, estimateTokens?.Invoke(value ?? "") ?? (value?.Length ?? 0));
        return Math.Max(0L, Math.Min(16000L, DeclarationInputLimit - Count(job?.SystemPrompt) - Count(job?.UserPrompt) - 4096L));
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
        string query = (round?.RoundTopic ?? "") + " " + (Knows(source) ? source.Title + " " + source.Body : "");
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
    private sealed class DocumentIndex
    {
        private List<WorldDiplomacyDocument> _documents;
        private int _count;
        private readonly Dictionary<string, WorldDiplomacyDocument> _byId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<WorldDiplomacyDocument>> _byRound = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _own = new(StringComparer.OrdinalIgnoreCase);
        internal void Update(List<WorldDiplomacyDocument> documents)
        {
            if (!ReferenceEquals(_documents, documents) || (documents?.Count ?? 0) < _count)
            { _documents = documents; _count = 0; _byId.Clear(); _byRound.Clear(); _own.Clear(); }
            if (documents == null) return;
            for (; _count < documents.Count; _count++)
            {
                var d = documents[_count]; if (d == null || string.IsNullOrEmpty(d.DocumentId)) continue;
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
