using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// The saved archive and a declaration's reading window have different lifetimes.
// Append-only document indexes grow once per new document, not once per tick/request.
internal static class WorldDiplomacyRequestHistoryApplication
{
    internal const string ContextMarker = "【本次外交相关历史】";
    internal const int DeclarationInputLimit = 32000;
    internal const int CompressionInputLimit = 128000;
    internal const int ContextCharacterLimit = 20000;
    private const int RecentDays = 21;
    private static readonly ConditionalWeakTable<WorldDiplomacyStorage, DocumentIndex> Indexes =
        new ConditionalWeakTable<WorldDiplomacyStorage, DocumentIndex>();
    internal static void InvalidateDocumentRouting(WorldDiplomacyStorage storage) => Indexes.Remove(storage);

    internal static long InputLimit(WorldDiplomacyJob job, long configuredLimit) =>
        Math.Min(configuredLimit, WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress")
            ? CompressionInputLimit : DeclarationInputLimit);

    internal static long WindowBudget(WorldDiplomacyJob job, Func<string, int> estimateTokens)
    {
        long Count(string value) => Math.Max(0, estimateTokens?.Invoke(value ?? "") ?? (value?.Length ?? 0));
        // Leave room for framing plus one rejected answer/correction. Rules and
        // exact current source documents are never silently truncated to fit.
        return Math.Max(0L, Math.Min(16000L, DeclarationInputLimit - Count(job?.SystemPrompt)
            - Count(job?.UserPrompt) - 4096L));
    }

    internal static string Build(WorldDiplomacyStorage storage, WorldDiplomacyJob job, int currentDay,
        long tokenBudget = 16000L, Func<string, int> estimateTokens = null)
    {
        var text = new StringBuilder(ContextMarker);
        text.AppendLine();
        text.AppendLine("以下是本次事件及近期相关材料的有限窗口，并非完整档案。缺席不代表未发生；节选不可用于推断未列出的条款。当前状态和原案精确条款以本次动态材料为准。宣言/提案不等于执行；仅[游戏已执行]是确认结果。");
        long Count(string value) => Math.Max(0, estimateTokens?.Invoke(value) ?? value.Length);
        long remainingTokens = Math.Max(0L, tokenBudget - Count(text.ToString()));
        bool AddSection(string line)
        {
            long tokens = Count(line) + 2L;
            if (tokens > remainingTokens || !Append(text, line)) return false;
            remainingTokens -= tokens;
            return true;
        }
        if (storage == null || job == null) return text.ToString();
        var index = Indexes.GetValue(storage, _ => new DocumentIndex());
        index.Update(storage.Documents);
        var knownIds = WorldDiplomacyRoundLifecycleRules.CollectKnownDocumentIds(null,
            storage.NobleKnowledge, storage.KingdomKnowledge, null, job.AuthorKingdomId, true, true);
        bool KnowsDocument(WorldDiplomacyDocument document) => document != null
            && (string.Equals(document.AuthorKingdomId, job.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
                || knownIds.Contains(document.DocumentId ?? ""));
        bool KnowsSource(string id)
        {
            // Non-document world facts keep their existing visibility. A fact
            // attached to a retained diplomacy document follows its delivery.
            var document = index.Find(id);
            return document == null || KnowsDocument(document);
        }
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kingdoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddId(kingdoms, job.AuthorKingdomId);
        AddId(kingdoms, job.TargetKingdomId);
        foreach (string id in (job.CandidateKingdomIds ?? new List<string>()).Take(8)) AddId(kingdoms, id);
        int documentCount = 0;
        void Document(WorldDiplomacyDocument doc)
        {
            if (doc == null || !doc.IsReadyForPublication || !KnowsDocument(doc) || documentCount >= 16
                || !selected.Add(doc.DocumentId ?? "")) return;
            string line = "[公文 " + Clip(doc.DocumentId, 96) + "; " + Clip(doc.GameDate, 80)
                + "; " + Clip(doc.AuthorKingdomId, 96) + "→" + Clip(doc.TargetKingdomId, 96)
                + "] " + Clip(doc.Title, 160) + "\n[正文节选] " + Clip(doc.Body, 1200);
            if (doc.ChangedDiplomaticState) line += "\n[游戏已执行] " + Clip(doc.MechanicalResult, 600);
            if (AddSection(line)) documentCount++;
        }
        Document(index.Find(job.SourceDocumentId));
        foreach (string id in (job.TriggerDocumentIds ?? new List<string>()).Take(8)) Document(index.Find(id));
        string roundId = string.IsNullOrWhiteSpace(job.RoundId) ? job.ExchangeId : job.RoundId;
        foreach (var doc in index.RecentRound(roundId).Take(8)) Document(doc);
        var recentGroups = string.IsNullOrWhiteSpace(job.TargetKingdomId)
            ? kingdoms.Select(index.RecentKingdom)
            : new[] { index.RecentPair(job.AuthorKingdomId, job.TargetKingdomId) };
        foreach (var recent in recentGroups)
        {
            int added = 0;
            foreach (var doc in recent)
            {
                if (doc.Day < currentDay - RecentDays || doc.Day > currentDay) continue;
                int before = documentCount;
                Document(doc);
                if (documentCount > before && ++added >= 5) break;
            }
        }
        // Round summaries are already a bounded retained collection. Do not inject
        // the unstructured global snapshot, which contains unrelated countries/rules.
        var summaries = (storage.RoundSummaries ?? new List<WorldDiplomacyRoundSummary>())
            .Where(s => s != null && !string.Equals(s.RoundId, roundId, StringComparison.OrdinalIgnoreCase)
                && (s.KingdomIds ?? new List<string>()).Contains(job.AuthorKingdomId, StringComparer.OrdinalIgnoreCase)
                && (s.SourceDocumentIds ?? new List<string>()).All(KnowsSource)
                && (string.IsNullOrWhiteSpace(job.TargetKingdomId)
                    || (s.KingdomIds ?? new List<string>()).Contains(job.TargetKingdomId, StringComparer.OrdinalIgnoreCase)))
            .OrderByDescending(s => s.CreatedDay).Take(2);
        foreach (var summary in summaries)
            AddSection("[相关旧事件摘要 " + Clip(summary.RoundId, 96) + "] " + Clip(summary.Summary, 1400));
        var facts = storage.CanonicalHistory?.Snapshot?.ProtectedFacts;
        if (facts != null)
        {
            foreach (var fact in facts.Where(f => f != null && KnowsSource(f.SourceId) && KnowsSource(f.RelatedSourceId)
                && (kingdoms.Contains(f.AuthorKingdomId ?? "")
                || (f.TargetKingdomIds ?? new List<string>()).Any(kingdoms.Contains)))
                .OrderByDescending(f => f.Sequence).Take(8))
                AddSection("[历史受保护事实; kind=" + Clip(fact.Kind, 64) + "; source="
                    + Clip(fact.SourceId, 96) + "] " + Clip(fact.Text, 600));
        }
        return text.ToString();
    }

    private static void AddId(HashSet<string> ids, string id) { if (!string.IsNullOrWhiteSpace(id)) ids.Add(id); }
    private static bool Append(StringBuilder text, string line)
    {
        if (text.Length + line.Length + 2 > ContextCharacterLimit) return false;
        text.AppendLine(line);
        return true;
    }
    private static string Clip(string value, int length)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length <= length) return value;
        if (char.IsHighSurrogate(value[length - 1])) length--;
        return value.Substring(0, length) + "…";
    }

    private sealed class DocumentIndex
    {
        private List<WorldDiplomacyDocument> _documents;
        private int _count;
        private readonly Dictionary<string, WorldDiplomacyDocument> _byId = new Dictionary<string, WorldDiplomacyDocument>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<WorldDiplomacyDocument>> _byRound = new Dictionary<string, List<WorldDiplomacyDocument>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<WorldDiplomacyDocument>> _byKingdom = new Dictionary<string, List<WorldDiplomacyDocument>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<WorldDiplomacyDocument>> _byPair = new Dictionary<string, List<WorldDiplomacyDocument>>(StringComparer.OrdinalIgnoreCase);
        internal void Update(List<WorldDiplomacyDocument> documents)
        {
            if (!ReferenceEquals(_documents, documents) || (documents?.Count ?? 0) < _count)
            {
                _documents = documents; _count = 0;
                _byId.Clear(); _byRound.Clear(); _byKingdom.Clear(); _byPair.Clear();
            }
            if (documents == null) return;
            for (; _count < documents.Count; _count++)
            {
                var doc = documents[_count];
                if (doc == null) continue;
                _byId[doc.DocumentId ?? ""] = doc;
                Add(_byRound, doc.RoundId, doc, 32);
                Add(_byKingdom, doc.AuthorKingdomId, doc, 32);
                if (!string.Equals(doc.AuthorKingdomId, doc.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
                {
                    Add(_byKingdom, doc.TargetKingdomId, doc, 32);
                    if (!string.IsNullOrWhiteSpace(doc.AuthorKingdomId) && !string.IsNullOrWhiteSpace(doc.TargetKingdomId))
                        Add(_byPair, PairKey(doc.AuthorKingdomId, doc.TargetKingdomId), doc, 32);
                }
                foreach (string id in (doc.AddressedKingdomIds ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase))
                    if (!string.Equals(id, doc.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(id, doc.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
                    {
                        Add(_byKingdom, id, doc, 32);
                        if (!string.IsNullOrWhiteSpace(doc.AuthorKingdomId) && !string.IsNullOrWhiteSpace(id))
                            Add(_byPair, PairKey(doc.AuthorKingdomId, id), doc, 32);
                    }
            }
        }
        private static void Add(Dictionary<string, List<WorldDiplomacyDocument>> index, string key, WorldDiplomacyDocument doc, int capacity)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!index.TryGetValue(key, out var list)) index[key] = list = new List<WorldDiplomacyDocument>();
            list.Add(doc);
            list.Sort((a, b) => a.Day != b.Day ? b.Day.CompareTo(a.Day) : b.CreatedUtcTicks.CompareTo(a.CreatedUtcTicks));
            if (list.Count > capacity) list.RemoveAt(list.Count - 1);
        }
        internal WorldDiplomacyDocument Find(string id) => !string.IsNullOrWhiteSpace(id) && _byId.TryGetValue(id, out var doc) ? doc : null;
        internal IEnumerable<WorldDiplomacyDocument> RecentRound(string id) => Get(_byRound, id);
        internal IEnumerable<WorldDiplomacyDocument> RecentKingdom(string id) => Get(_byKingdom, id);
        internal IEnumerable<WorldDiplomacyDocument> RecentPair(string first, string second) =>
            string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second) ? Enumerable.Empty<WorldDiplomacyDocument>() : Get(_byPair, PairKey(first, second));
        private static string PairKey(string first, string second) => StringComparer.OrdinalIgnoreCase.Compare(first, second) <= 0
            ? first + "\0" + second : second + "\0" + first;
        private static IEnumerable<WorldDiplomacyDocument> Get(Dictionary<string, List<WorldDiplomacyDocument>> index, string id) =>
            !string.IsNullOrWhiteSpace(id) && index.TryGetValue(id, out var list) ? list : Enumerable.Empty<WorldDiplomacyDocument>();
    }
}
