using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

namespace AnimusForge;

internal readonly struct WorldDiplomacyMemorySnapshot
{
    internal WorldDiplomacyMemorySnapshot(WorldDiplomacyStorage storage, string kingdomId, HashSet<string> knownIds, string rulerId = "")
    { Storage = storage; KingdomId = kingdomId; KnownIds = knownIds; RulerId = rulerId; }
    internal WorldDiplomacyStorage Storage { get; }
    internal string KingdomId { get; }
    internal string RulerId { get; }
    internal HashSet<string> KnownIds { get; }
}
internal interface IWorldDiplomacyMemorySource
{
    bool TryCapture(string heroId, string kingdomOverride, out WorldDiplomacyMemorySnapshot snapshot);
    string FormatDate(int day);
}
internal interface IWorldDiplomacySemanticSource
{
    float[] Embed(string text);
}
internal static class WorldDiplomacyMemoryApplication
{
    // Shared by courier, free conversation and scene shout. Index construction is
    // mutation-driven at a request boundary, never a frame or day archive scan.
    private static readonly ConditionalWeakTable<WorldDiplomacyStorage, Index> Indexes = new();
    private sealed class Row
    {
        internal string Id, EventId, Search, OwnerKingdom, OwnerRuler;
        internal string[] Sources;
        internal List<string> ActorIds;
        internal int Day;
        internal bool Public;
        internal Func<string> Render;
    }
    private sealed class Index
    {
        internal bool Dirty = true;
        internal int Documents = -1, Boundaries = -1, Summaries = -1, Aid = -1, Arrangements = -1;
        internal readonly List<Row> Rows = new();
        internal readonly Dictionary<string, (string Text, float[] Vector)> Vectors = new(StringComparer.Ordinal);
        internal int VectorCursor;
        internal long HistoryRevision = -1;
        internal readonly Dictionary<string, List<Row>> Terms = new(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string, List<Row>> Experiences = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, WorldDiplomacyDocument> ByDocument = new(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string, WorldDiplomacyRound> ByEvent = new(StringComparer.OrdinalIgnoreCase);
        internal void Add(Row row)
        {
            row.Search = row.Search + " " + row.Id + " " + row.EventId + " " + string.Join(" ",row.Sources ?? Array.Empty<string>());
            Rows.Add(row);
            foreach (string term in Tokens(row.Search))
            {
                if (!Terms.TryGetValue(term, out var rows)) Terms[term] = rows = new();
                rows.Add(row);
            }
        }
        internal void Ensure(WorldDiplomacyStorage storage, Func<int, string> date)
        {
            if (!Dirty && Documents == storage.Documents.Count && Boundaries == storage.EventAnnouncements.Count && Summaries == storage.RoundSummaries.Count && Aid == storage.AidRequests.Count && Arrangements == storage.DialogueArrangements.Count && HistoryRevision == (storage.CanonicalHistory?.Revision ?? 0)) return;
            Terms.Clear(); Experiences.Clear(); ByDocument.Clear(); ByEvent.Clear(); Rows.Clear(); VectorCursor = 0;
            foreach (var round in WorldDiplomacyLiveRoundRules.Live(storage).Concat(storage.CompletedRounds))
                if (round != null && !string.IsNullOrEmpty(round.RoundId)) ByEvent[round.RoundId] = round;
            foreach (var doc in storage.Documents)
            {
                if (doc == null || string.IsNullOrEmpty(doc.DocumentId)) continue;
                ByDocument[doc.DocumentId] = doc;
                if (!doc.IsReadyForPublication || doc.IsCompressed) continue;
                Add(new Row { Id = "document:" + doc.DocumentId, EventId = doc.RoundId, Day = doc.Day,
                    Sources = new[] { doc.DocumentId }, Search = doc.Title + " " + doc.Body + " " + doc.AuthorKingdomName + " " + doc.TargetKingdomName + " " + doc.MechanicalResult,
                    Render = () => JsonConvert.SerializeObject(new { source = doc.DocumentId, day = date(doc.Day), doc.Title, doc.Body,
                        doc.AuthorKingdomId, doc.Intent, doc.Commitment, doc.ChangedDiplomaticState, doc.MechanicalResult }) });
            }
            foreach (var boundary in storage.EventAnnouncements.Where(x => x?.State is "public" or "applied"))
            {
                var evidence = (boundary.ProgressEvidenceIds ?? new()).Concat(boundary.ConfirmedDocumentIds ?? new()).Distinct().ToArray();
                Add(new Row { Id = boundary.Id, EventId = boundary.EventId, Public = boundary.State == "public", Day = boundary.Day, Sources = evidence,
                    Search = boundary.Title + " " + boundary.Body + " " + string.Join(" ", boundary.Narrative.Where(x => x.Public).Select(x => x.Text)),
                    Render = () => JsonConvert.SerializeObject(new { source = boundary.Id, boundary.EventId, boundary.Stage, boundary.Title, boundary.Body,
                        narrative = boundary.Narrative.Where(x => x.Public).Select(x => new { x.Kind, x.Source, x.Text, x.SubjectKingdomId, x.HeroIds }) }) });
                foreach (var fact in boundary.Narrative.Where(x => x.Public && x.HeroIds?.Count > 0))
                {
                    var experience = new Row { Id = "experience:" + fact.Id, EventId = boundary.EventId, Public = boundary.State == "public", Day = boundary.Day,
                        ActorIds = fact.HeroIds,
                        Search = boundary.Title + " " + fact.Text + " " + fact.Source + " " + fact.Id,
                        Sources = boundary.State == "public" ? new[] { boundary.Id,fact.Id,fact.Source } : evidence,
                        Render = () => JsonConvert.SerializeObject(new { source = boundary.Id, fact_id = fact.Id,
                            boundary.Title, fact.Kind, fact.Source, fact.Text, fact.HeroIds,
                            personal_experience = fact.Kind == "adopted_npc_story" || fact.Kind == "public_fact" }) };
                    Add(experience);
                    foreach (string hero in fact.HeroIds)
                    {
                        if (!Experiences.TryGetValue(hero, out var rows)) Experiences[hero] = rows = new();
                        rows.Add(experience);
                    }
                }
            }
            foreach (var summary in storage.RoundSummaries.Where(x => x != null))
            {
                if ((summary.Facts == null || summary.Facts.Count == 0) && summary.SourceDocumentIds?.Count > 0)
                    Add(new Row { Id = "legacy-summary:" + summary.RoundId, EventId = summary.RoundId, Day = summary.CreatedDay,
                        Sources = summary.SourceDocumentIds.ToArray(), Search = summary.Summary,
                        Render = () => JsonConvert.SerializeObject(new { source_event = summary.RoundId, sources = summary.SourceDocumentIds, summary.Summary }) });
                int i = 0;
                // Each compressed fact keeps its exact source visibility. Never
                // expose the entire summary merely because one source is known.
                foreach (var fact in (summary.Facts ?? new()).Where(x => x != null && x.SourceDocumentIds?.Count > 0))
                {
                    string text = WorldDiplomacyDocumentFactRules.FormatRoundFactForPrompt(fact);
                    Add(new Row { Id = "summary:" + summary.RoundId + ":" + i++, EventId = summary.RoundId, Day = summary.CreatedDay,
                        Sources = fact.SourceDocumentIds.ToArray(), Search = text, Render = () => text });
                }
            }
            foreach (var aid in storage.AidRequests.Where(x => x != null))
            {
                ByEvent.TryGetValue(aid.RoundId ?? "", out var round);
                Add(new Row { Id = "aid:" + aid.Id, EventId = aid.RoundId, Day = aid.Day,
                    OwnerKingdom = aid.CallingKingdomId, OwnerRuler = aid.AcceptedRulerId,
                    Sources = string.IsNullOrEmpty(aid.RequestDocumentId) ? null : new[] { aid.RequestDocumentId },
                    Search = DiplomacyPromptTexts.Get("Memory_AidTopic", "Allied military aid and requests to join a war") + " " + round?.RoundTopic
                        + " " + aid.CallingKingdomId + " " + aid.CalledKingdomId + " " + aid.EnemyKingdomId,
                    Render = () => JsonConvert.SerializeObject(new { current_aid = aid }) });
            }
            foreach (var item in storage.DialogueArrangements.Where(x => x != null))
            {
                ByEvent.TryGetValue(item.RoundId ?? "", out var round);
                Add(new Row { Id = "commitment:" + item.ArrangementId, EventId = item.RoundId, Day = item.CreatedDay,
                    OwnerRuler = item.RulerId, Sources = string.IsNullOrEmpty(item.DocumentId) ? null : new[] { item.DocumentId },
                    Search = round?.RoundTopic + " " + item.Action + " " + item.ActorKingdomId + " " + item.TargetKingdomId,
                    Render = () => JsonConvert.SerializeObject(new { commitment = item.ArrangementId, item.ActorKingdomId,
                        item.TargetKingdomId, item.Action, item.Move, item.Terms, item.Status, item.DocumentId }) });
            }
            foreach (var fact in storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new())
            {
                if (fact == null || string.IsNullOrEmpty(fact.SourceId)) continue;
                Add(new Row { Id = "protected:" + fact.Sequence + ":" + fact.SourceId,
                    Sources = new[] { fact.SourceId, fact.RelatedSourceId }.Where(x => !string.IsNullOrEmpty(x)).ToArray(),
                    Search = fact.Text, Render = () => JsonConvert.SerializeObject(new { protected_fact = fact }) });
            }
            HistoryRevision = storage.CanonicalHistory?.Revision ?? 0;
            var retained = new HashSet<string>(Rows.Select(x => x.Id), StringComparer.Ordinal);
            foreach (string key in Vectors.Keys.Where(x => !retained.Contains(x)).ToList()) Vectors.Remove(key);
            Aid = storage.AidRequests.Count; Arrangements = storage.DialogueArrangements.Count;
            Documents = storage.Documents.Count; Boundaries = storage.EventAnnouncements.Count; Summaries = storage.RoundSummaries.Count; Dirty = false;
        }
    }
    private static readonly HashSet<string> Stop = new(DiplomacyCompatibilityTexts.Get("Memory_LexicalStopWords").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
    private static readonly Regex Words = new(@"[A-Za-z0-9_]+|[\p{IsCJKUnifiedIdeographs}]+", RegexOptions.Compiled);
    private static HashSet<string> Tokens(string text)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Words.Matches(text ?? ""))
        {
            string word = match.Value;
            if (word[0] >= 0x4e00)
            {
                for (int i = 0; i + 1 < word.Length; i++) { string pair = word.Substring(i, 2); if (!Stop.Contains(pair)) result.Add(pair); }
            }
            else if (word.Length >= 3 && !Stop.Contains(word)) result.Add(word);
        }
        return result;
    }
    private static double Cosine(float[] left, float[] right)
    {
        if (left.Length != right.Length) return 0;
        double dot = 0, a = 0, b = 0;
        for (int i = 0; i < left.Length; i++) { dot += left[i] * right[i]; a += left[i] * left[i]; b += right[i] * right[i]; }
        return a <= 0 || b <= 0 ? 0 : dot / Math.Sqrt(a * b);
    }
    internal static void Invalidate(WorldDiplomacyStorage storage)
    { if (storage != null && Indexes.TryGetValue(storage, out var index)) index.Dirty = true; }

    internal static string Build(IWorldDiplomacyMemorySource source, string heroId, string kingdomOverride,
        string input, IReadOnlyList<string> ruleIds, bool proactive)
    {
        if (string.IsNullOrWhiteSpace(input) || !source.TryCapture(heroId, kingdomOverride, out var snapshot)) return "";
        Func<string, float[]> embed = source is IWorldDiplomacySemanticSource semantic ? semantic.Embed : null;
        return Build(snapshot, input, source.FormatDate, 7000, embed);
    }
    internal static string Build(WorldDiplomacyMemorySnapshot snapshot, string query, Func<int, string> date, int maximumChars = 7000, Func<string, float[]> embed = null, ISet<string> excludedDocuments = null)
    {
        var storage = snapshot.Storage;
        if (storage == null || string.IsNullOrWhiteSpace(query) || maximumChars < 256) return "";
        storage.EventAnnouncements ??= new();
        var index = Indexes.GetValue(storage, _ => new Index()); index.Ensure(storage, date);
        bool Visible(Row row) => !(row.Id.StartsWith("document:", StringComparison.Ordinal)
            && excludedDocuments?.Contains(row.Id.Substring("document:".Length)) == true) && (row.Public
            || (!string.IsNullOrEmpty(row.OwnerRuler) && row.OwnerRuler == snapshot.RulerId)
            || row.ActorIds?.Contains(snapshot.RulerId) == true
            || (!string.IsNullOrEmpty(row.OwnerKingdom) && row.OwnerKingdom == snapshot.KingdomId)
            || (row.Sources?.Length > 0 && row.Sources.All(x => snapshot.KnownIds?.Contains(x) == true)));
        var scores = new Dictionary<Row, double>();
        foreach (string term in Tokens(query))
        {
            if (!index.Terms.TryGetValue(term, out var rows)) continue;
            foreach (var row in rows)
            {
                if (!Visible(row)) continue;
                scores.TryGetValue(row, out double score);
                // Rare distinguishing terms outweigh a shared word in many newer records.
                scores[row] = score + 1 + Math.Log(1 + (double)index.Rows.Count / rows.Count);
            }
        }
        // Local inference only. Cache by stable source and unchanged content; warm at
        // most 24 visible records per request, never in Tick or for hidden sources.
        if (embed != null)
        {
            try
            {
                float[] queryVector = embed(query);
                if (queryVector?.Length > 0)
                {
                    int generated = 0, examined = 0, originalCursor = index.VectorCursor;
                    var prepared = new List<(Row Row, string Text, float[] Vector)>();
                    while (generated < 24 && examined++ < index.Rows.Count)
                    {
                        if (index.VectorCursor >= index.Rows.Count) index.VectorCursor = 0;
                        var row = index.Rows[index.VectorCursor++];
                        if (!Visible(row)) continue;
                        string text = row.Search ?? "";
                        if (index.Vectors.TryGetValue(row.Id, out var cached) && cached.Text == text) continue;
                        generated++;
                        // Embedding input is bounded; the archived record is never truncated.
                        try
                        {
                            float[] vector = embed(text.Length > 1400 ? text.Substring(0, 1400) : text);
                            if (vector?.Length > 0) prepared.Add((row, text, vector));
                        }
                        catch (WorldDiplomacyPreparationException) { index.VectorCursor = originalCursor; throw; }
                    }
                    // Retry the same at-most-24 batch. Publishing a partial batch
                    // would advance the cursor and silently change relevance.
                    foreach (var item in prepared) index.Vectors[item.Row.Id] = (item.Text, item.Vector);
                    foreach (var row in index.Rows)
                    {
                        if (!Visible(row) || !index.Vectors.TryGetValue(row.Id, out var cached) || cached.Text != row.Search) continue;
                        double similarity = Cosine(queryVector, cached.Vector);
                        if (similarity < 0.65) continue;
                        scores.TryGetValue(row, out double lexical);
                        scores[row] = lexical + similarity * 2;
                    }
                }
            }
            catch (WorldDiplomacyPreparationException) { throw; }
            catch { /* Keep lexical retrieval available when local inference fails. */ }
        }
        // Preserve relative relevance within personal evidence. Recency breaks ties
        // only; an unrelated fallback cannot displace a matching record or obligation.
        if (!string.IsNullOrEmpty(snapshot.RulerId) && index.Experiences.TryGetValue(snapshot.RulerId, out var own))
        {
            foreach (var row in own.Where(scores.ContainsKey)) scores[row] = scores[row] * 1.25 + 1;
            if(scores.Count==0)
                foreach(var row in own.OrderByDescending(x=>x.Day).Take(1)) scores[row]=0.1;
        }
        foreach(var row in scores.Keys.ToList())
            if(query.IndexOf(row.Id,StringComparison.OrdinalIgnoreCase)>=0
                || (row.Sources??Array.Empty<string>()).Any(x=>!string.IsNullOrEmpty(x)&&query.IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0))
                scores[row]+=20;
        if (scores.Count == 0) return "";
        var selected = scores.OrderByDescending(x => x.Value).ThenByDescending(x => x.Key.Day).Select(x => x.Key).Take(12).ToList();
        string header = DiplomacyPromptTexts.Get("EventPublicKnowledgeHeader", "[Known diplomatic records: distinguish worldwide event boundaries from delivered declarations; source-qualified allegations are not verified facts.]");
        int bodyBudget = maximumChars - header.Length - 1;
        var body = new StringBuilder(); var used = new HashSet<string>(StringComparer.Ordinal); var selectedEvents = new HashSet<string>(selected.Select(x => x.EventId));
        void Add(string id, string value)
        {
            if (!used.Add(id) || string.IsNullOrWhiteSpace(value) || value.Length + body.Length + Environment.NewLine.Length > bodyBudget) return;
            body.AppendLine(value);
        }
        // Reserve the best directly relevant personal original, then current truth,
        // then remaining ranked whole records. Never clip an original to fit.
        var personal=selected.FirstOrDefault(x=>x.Id.StartsWith("experience:",StringComparison.Ordinal));
        if(personal!=null) Add(personal.Id,personal.Render());
        // Lossless current truth before historical prose; no copied execution owner.
        foreach (string eventId in selectedEvents)
        {
            if (string.IsNullOrEmpty(eventId) || !index.ByEvent.TryGetValue(eventId, out var round)) continue;
            foreach (var offer in (round.PendingOffers ?? new()).Where(x => x?.Status == "open" && snapshot.KnownIds?.Contains(x.SourceDocumentId) == true))
            {
                if (!index.ByDocument.TryGetValue(offer.SourceDocumentId, out var source)) continue;
                var action = source.Actions?.FirstOrDefault(x => x.ActionId == offer.SourceActionId);
                Add("offer:" + offer.SourceDocumentId + ":" + offer.SourceActionId, JsonConvert.SerializeObject(new { current_offer = offer,
                    terms = action?.TreatyTerms ?? source.TreatyTerms, peace_terms = action?.PeaceTerms ?? source.PeaceTerms,
                    conditions = action?.DependencyActionIds }));
            }
            foreach (var item in storage.DialogueArrangements.Where(x => x.RoundId == eventId
                && (x.RulerId == snapshot.RulerId || snapshot.KnownIds?.Contains(x.DocumentId) == true)
                && x.Status is "accepted" or "deferred" or "execution_uncertain" or "published" or "executed"))
                Add("commitment:" + item.ArrangementId, JsonConvert.SerializeObject(new { commitment = item.ArrangementId, item.ActorKingdomId,
                    item.TargetKingdomId, item.Action, item.Move, item.Terms, item.Status, item.DocumentId }));
        }
        foreach (var row in selected) Add(row.Id, row.Render());
        if (body.Length == 0) return "";
        return header + "\n" + body.ToString().TrimEnd();
    }
}
