using System.Linq;
using AnimusForge;
using Newtonsoft.Json.Linq;
internal static class AnalysisApplicationReplay
{
    private sealed class Port : IWorldDiplomacyAnalysisPort
    {
        public WorldDiplomacyStorage Storage { get; } = new();
        internal DocumentExecutionReplay.Port Effects = new();
        public IWorldDiplomacyDocumentExecutionPort Execution => Effects;
        public int MaxAutomaticReplyDepth => 3;
        public string KingdomName(string id) => "Realm-" + id;
    }
    private sealed class Orch : FakeOrchestration
    {
        private readonly DocumentExecutionReplay.Port _effects;
        internal Orch(DocumentExecutionReplay.Port effects) { _effects = effects; }
        public override void ProcessAnalyzedDocument(WorldDiplomacyDocument document, string intent, string commitment,
            bool requiresResponse, string tone, float confidence)
            => WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(_effects, this, document,
                intent, commitment, requiresResponse, tone, confidence);
        public override WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(JObject json, string author, string target) => null;
        public override List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId)
            => values.Where(x => !string.IsNullOrWhiteSpace(x) && x != excludedId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        public override void FinalizePublishedDocumentAfterAnalysis(WorldDiplomacyDocument document, string authorId, string targetId, string normalizedIntent, bool recordNoActionDecision)
        { _effects.Events.Add("propagation"); _effects.Events.Add("history"); _effects.Events.Add("round"); }
        public override void StartDocumentPropagation(WorldDiplomacyDocument doc, string author) => _effects.Events.Add("propagation");
        public override void AppendCanonicalDocumentEvents(WorldDiplomacyDocument doc) => _effects.Events.Add("history");
        public override void HandleRoundDocumentProcessed(WorldDiplomacyDocument doc) => _effects.Events.Add("round");
        public override void SettleInternationalReputationForDocument(WorldDiplomacyDocument doc) => _effects.Events.Add("reputation");
        public override void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument doc) => _effects.Events.Add("weekly");
        public override void ReconcileReachedCourts(WorldDiplomacyDocument doc) => _effects.Events.Add("courts");
        public override void ScheduleNextResultSettlementTurn(WorldDiplomacyRound round) => _effects.Events.Add("settlement");
        public override void AdvanceRelay(WorldDiplomacyRound round, bool scheduleImmediately) => _effects.Events.Add("advance");
        public override void CompleteExchange(string id, string reason) => _effects.Events.Add("complete:" + reason);
        public override void CloseActiveRound(string reason) => _effects.Events.Add("close:" + reason);
    }
    internal static void Run()
    {
        var p = new Port();
        var orch = new Orch(p.Effects);
        var document = new WorldDiplomacyDocument { DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = "b", IsPlayerAuthored = true, IsReadyForPublication = true };
        p.Storage.Documents.Add(document); p.Effects.StoredDocument = document;
        var job = new WorldDiplomacyJob { DocumentId = "d", Kind = "analyze" };
        WorldDiplomacyAnalysisApplication.Commit(p, orch, job, "{}");
        Test.True(document.Intent == "statement" && document.Commitment == "non_binding" && document.AnalysisStatus == "fallback"
            && document.IsReadyForPublication && p.Storage.Documents.Contains(document) && p.Effects.Events.Contains("history") && p.Effects.Events.Contains("round"),
            "malformed analysis of published player speech flows through statement execution, history and round progress without deletion");
        p.Effects.Events.Clear(); document.Intent = "unsupported"; document.Commitment = "binding"; document.MechanicalResult = "";
        WorldDiplomacyAnalysisApplication.Suppress(p, orch, document, "late illegal action");
        Test.True(document.AnalysisStatus == "published_action_rejected" && document.Intent == "statement" && document.Commitment == "non_binding"
            && document.IsReadyForPublication && p.Storage.Documents.Contains(document) && p.Effects.Events.Any(x => x.StartsWith("notify:"))
            && p.Effects.Events.IndexOf("propagation") < p.Effects.Events.IndexOf("history"),
            "mechanic rejection preserves player text, normalizes unsupported action and finalizes notification/propagation/history in original order");
        var ai = new WorldDiplomacyDocument { DocumentId = "ai", AuthorKingdomId = "a", TargetKingdomId = "b", RoundId = "r", IsRelayTurn = true };
        p.Storage.Documents.Add(ai); p.Effects.StoredDocument = ai;
        p.Effects.Round = new WorldDiplomacyRound { RoundId = "r", State = "active", RootDocumentId = "ai", ResultSettlementPending = true, RelayWaiting = true };
        p.Storage.ActiveRound = p.Effects.Round;
        p.Effects.Events.Clear(); job.DocumentId = "ai";
        WorldDiplomacyAnalysisApplication.Commit(p, orch, job, "{}");
        Test.True(!p.Storage.Documents.Contains(ai) && p.Effects.Round.RootDocumentId == "" && !p.Effects.Round.RelayWaiting
            && p.Effects.Events.Contains("settlement") && !p.Effects.Events.Contains("history"),
            "unusable AI analysis removes the unpublished document, clears root and advances settlement without publishing a fact");
        p.Effects.StoredDocument = null; p.Effects.Events.Clear();
        WorldDiplomacyAnalysisApplication.Commit(p, orch, job, "{}");
        Test.True(p.Effects.Events.Count == 0, "late analysis for an absent document has no effects");
    }
}
