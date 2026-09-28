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
        public WorldDiplomacyPeaceTerms ParsePeaceTerms(JObject json, string author, string target) => null;
        public string KingdomName(string id) => "Realm-" + id;
        public void ScheduleSettlement(WorldDiplomacyRound round) => Effects.Events.Add("settlement");
        public void AdvanceRelay(WorldDiplomacyRound round) => Effects.Events.Add("advance");
        public void CompleteExchange(string id, string reason) => Effects.Events.Add("complete:" + reason);
        public void CloseRound(string reason) => Effects.Events.Add("close:" + reason);
    }
    internal static void Run()
    {
        var p = new Port();
        var document = new WorldDiplomacyDocument { DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = "b", IsPlayerAuthored = true, IsReadyForPublication = true };
        p.Storage.Documents.Add(document); p.Effects.StoredDocument = document;
        var job = new WorldDiplomacyJob { DocumentId = "d", Kind = "analyze" };
        WorldDiplomacyAnalysisApplication.Commit(p, job, "{}");
        Test.True(document.Intent == "statement" && document.Commitment == "non_binding" && document.AnalysisStatus == "fallback"
            && document.IsReadyForPublication && p.Storage.Documents.Contains(document) && p.Effects.Events.Contains("history") && p.Effects.Events.Contains("round"),
            "malformed analysis of published player speech flows through statement execution, history and round progress without deletion");
        p.Effects.Events.Clear(); document.Intent = "unsupported"; document.Commitment = "binding"; document.MechanicalResult = "";
        WorldDiplomacyAnalysisApplication.Suppress(p, document, "late illegal action");
        Test.True(document.AnalysisStatus == "published_action_rejected" && document.Intent == "statement" && document.Commitment == "non_binding"
            && document.IsReadyForPublication && p.Storage.Documents.Contains(document) && p.Effects.Events.Any(x => x.StartsWith("notify:"))
            && p.Effects.Events.IndexOf("propagation") < p.Effects.Events.IndexOf("history"),
            "mechanic rejection preserves player text, normalizes unsupported action and finalizes notification/propagation/history in original order");
        var ai = new WorldDiplomacyDocument { DocumentId = "ai", AuthorKingdomId = "a", TargetKingdomId = "b", RoundId = "r", IsRelayTurn = true };
        p.Storage.Documents.Add(ai); p.Effects.StoredDocument = ai;
        p.Effects.Round = new WorldDiplomacyRound { RoundId = "r", State = "active", RootDocumentId = "ai", ResultSettlementPending = true, RelayWaiting = true };
        p.Storage.ActiveRound = p.Effects.Round;
        p.Effects.Events.Clear(); job.DocumentId = "ai";
        WorldDiplomacyAnalysisApplication.Commit(p, job, "{}");
        Test.True(!p.Storage.Documents.Contains(ai) && p.Effects.Round.RootDocumentId == "" && !p.Effects.Round.RelayWaiting
            && p.Effects.Events.Contains("settlement") && !p.Effects.Events.Contains("history"),
            "unusable AI analysis removes the unpublished document, clears root and advances settlement without publishing a fact");
        p.Effects.StoredDocument = null; p.Effects.Events.Clear();
        WorldDiplomacyAnalysisApplication.Commit(p, job, "{}");
        Test.True(p.Effects.Events.Count == 0, "late analysis for an absent document has no effects");
    }
}
