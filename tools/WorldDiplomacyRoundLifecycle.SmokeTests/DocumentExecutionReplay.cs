using AnimusForge;
using AnimusForge.Refactor.Domain;

internal static class DocumentExecutionReplay
{
    internal sealed class Port : IWorldDiplomacyDocumentExecutionPort
    {
        internal readonly List<string> Events = new();
        internal readonly List<string> Legal = new() { "declare_war", "statement" };
        internal WorldDiplomacyRound Round;
        internal WorldDiplomacyDocument StoredDocument;
        internal bool AuthorAllowed = true, NoAction, ThrowEffect, ThrowHistory, AfterFirstEffect;
        internal string InvalidTarget;
        internal int Effects, Id;
        public int MaxDiplomaticActionsPerDocument => 4;
        public int MaxRelayParticipants => 3;
        public int CurrentDay => 12;
        public IReadOnlyList<WorldDiplomacyThreat> Threats { get; } = new List<WorldDiplomacyThreat>();
        public string ResolveKingdomId(string id) { return id == "missing" || string.IsNullOrEmpty(id) ? null : id; }
        public bool IsEliminated(string id) { return false; }
        public WorldDiplomacyAuthoritySnapshot CaptureAuthority(string id) => new(id, id != null, false, id == "vassal", "suzerain", !AuthorAllowed, true);
        public WorldDiplomacyRound ResolveRound(string id) { return Round; }
        public WorldDiplomacyDocument ResolveDocument(string id) { return StoredDocument?.DocumentId == id ? StoredDocument : null; }
        public WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string slot, bool external, string sourceId, bool requireAnyOpenPeaceOffer) { return null; }
        public bool IsAtWar(string author, string target) { return true; }
        public bool IsPlayerKingdom(string id) { return false; }
        public string NewId(string prefix) { return prefix + (++Id); }
        public WarPressureEntry FindWarPressure(string source, string target) { return null; }
        public void AddWarPressure(string source, string target, int delta, string reason, string intent) { Events.Add("AddWarPressure"); }
        public List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId) { return values.Where(x => !string.IsNullOrWhiteSpace(x) && x != excludedId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); }
        public void Log(string message) { Events.Add("log:" + message); }
        public void Notify(string message) { Events.Add("notify:" + message); }
    }
    // The orchestration double replicates the previous fat-port effects so the
    // replay keeps asserting the same ordered receipt vocabulary.
    internal sealed class Orch : FakeOrchestration
    {
        private readonly Port _p;
        internal Orch(Port p) { _p = p; }
        public override void PruneInvalidOffers(WorldDiplomacyRound round) { _p.Events.Add("PruneInvalidOffers"); }
        public override bool IsNonRootAiRelayNoActionAllowed(WorldDiplomacyRound round, string slot, string author, string target, bool relay, bool external, WorldDiplomacyDocument source) { return _p.NoAction; }
        public override List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string author, string target) { return _p.Legal; }
        public override List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string author, string target, bool relay, string slot, bool external, WorldDiplomacyDocument source) { return _p.Legal; }
        public override bool TryGetDiplomaticStateViolation(string intent, string author, string target, out string reason) { _p.Events.Add("validate:" + target); reason = "changed"; return target == _p.InvalidTarget || (_p.AfterFirstEffect && _p.Effects > 0); }
        public override bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument doc, string intent, string commitment, string author, string target, out string reason) { reason = ""; return false; }
        public override void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument doc, string reason) { _p.Events.Add("reject:" + reason); }
        public override void ExecuteImmediateIntent(string author, string target, string intent, WorldDiplomacyDocument doc) { Test.True(doc.IsReadyForPublication, "validated declaration is publishable before irreversible effect"); _p.Events.Add("effect:" + target); _p.Effects++; doc.MechanicalResult = "effect " + target; doc.ChangedDiplomaticState = true; if (_p.ThrowEffect) throw new Exception("effect failure"); }
        public override void ProcessDiplomaticThreatDocument(WorldDiplomacyDocument doc, string author, string target, bool recordTargetDecisions) { _p.Events.Add("ProcessDiplomaticThreatDocument"); }
        public override void RecordDiplomaticThreatTargetDecisions(WorldDiplomacyDocument doc, string author, string target, string intent) { _p.Events.Add("RecordDiplomaticThreatTargetDecisions"); }
        public override void RecordDiplomaticThreatTargetDecisionsForActions(WorldDiplomacyDocument doc, string author) { _p.Events.Add("RecordDiplomaticThreatTargetDecisionsForActions"); }
        public override bool DeferUnresolvedRequiredThreatAction(WorldDiplomacyDocument doc, string author, string target, string intent) { return false; }
        public override void ApplyDiplomaticThreatReputationPenalty(WorldDiplomacyThreat threat, WorldDiplomacyDocument doc) { _p.Events.Add("ApplyDiplomaticThreatReputationPenalty"); }
        public override void TrySettleRelayOffer(WorldDiplomacyDocument doc) { _p.Events.Add("TrySettleRelayOffer"); }
        public override void ApplyDiplomaticPressureEffect(WorldDiplomacyDocument doc) { _p.Events.Add("ApplyDiplomaticPressureEffect"); }
        public override void SettleInternationalReputationForDocument(WorldDiplomacyDocument doc) { _p.Events.Add("SettleInternationalReputationForDocument"); }
        public override void AppendCanonicalDocumentEvents(WorldDiplomacyDocument doc) { _p.Events.Add("history"); if (_p.ThrowHistory) throw new Exception("history failure"); }
        public override void HandleRoundDocumentProcessed(WorldDiplomacyDocument doc) { _p.Events.Add("round"); }
        public override void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument doc) { _p.Events.Add("RecordDiplomacyWeeklyMaterial"); }
        public override void ReconcileReachedCourts(WorldDiplomacyDocument doc) { _p.Events.Add("ReconcileAnalyzedPlayerDeclarationWithReachedCourts"); }
        public override void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat) { _p.Events.Add("TryAppendDiplomaticThreatHistoryResult"); }
        public override void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat) { _p.Events.Add("TryAppendDiplomaticThreatDomesticPenaltyHistoryResult"); }
        public override void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat) { _p.Events.Add("TryAppendDiplomaticThreatIssuerRewardHistoryResult"); }
        public override void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent item) { _p.Events.Add("TryAppendDiplomaticThreatNonComplianceHistoryResult"); }
        public override void ScheduleDeferredCanonicalHistoryRetry(string id) { _p.Events.Add("retry"); }
        public override void StartDocumentPropagation(WorldDiplomacyDocument doc, string author) { _p.Events.Add("propagation"); }
    }
    private static WorldDiplomacyDocument Document(params string[] targets) => new()
    {
        DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = targets[0], Intent = "declare_war", Commitment = "binding",
        Actions = targets.Select((id, i) => new WorldDiplomacyDocumentAction { ActionId = "a" + i, TargetKingdomId = id,
            Intent = "declare_war", Commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("declare_war") }).ToList()
    };
    private static void Run(Port p, Orch orch, WorldDiplomacyDocument doc) => WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(
        p, orch, doc, doc.Intent, doc.Commitment, false, "", 1);
    private static (Port p, Orch orch) Fixture(Action<Port> init = null) { var p = new Port(); init?.Invoke(p); return (p, new Orch(p)); }
    internal static void Run()
    {
        var (slotPort, slotOrch) = Fixture();
        var slotStorage = new WorldDiplomacyStorage();
        var slotRound = new WorldDiplomacyRound { RoundId = "r", ResultSettlementPending = true, RelayPlanned = true,
            RelayRouteKingdomIds = new() { "a", "b" } };
        slotStorage.Documents.Add(new WorldDiplomacyDocument { RoundId = "r", AuthorKingdomId = "a", IsReadyForPublication = true });
        slotRound.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", ProposerKingdomId = "a", TargetKingdomId = "c", SourceDocumentId = "offer" });
        slotRound.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", ProposerKingdomId = "a", TargetKingdomId = "d", SourceDocumentId = "excess" });
        slotRound.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", ProposerKingdomId = "a", TargetKingdomId = "vassal" });
        slotStorage.DiplomaticThreats.Add(new WorldDiplomacyThreat { Status = "open", TargetDecision = "pending", StageRoundId = "r",
            IssuerKingdomId = "a", TargetKingdomId = "b", StageDocumentId = "threat" });
        WorldDiplomacyDocumentExecutionApplication.RefreshResultSettlementActionSlots(slotPort, slotStorage, slotRound, slotOrch.PruneInvalidOffers);
        Test.True(slotRound.RelayRouteKingdomIds.SequenceEqual(new[] { "a", "b", "c" })
            && slotRound.PendingOffers[1].Status == "invalidated" && slotRound.PendingOffers[2].Status == "invalidated",
            "slot refresh admits valid new offer targets before rejecting capacity and authority failures");
        Test.True(slotRound.ResultSettlementSlots.Select(s => s.KingdomId).SequenceEqual(new[] { "b", "c" })
            && slotRound.ResultSettlementSlots[0].Kind.Contains("threat_response") && slotRound.ResultSettlementSlots[1].Kind.Contains("offer_response")
            && slotRound.ResultSettlementSlots[0].SourceDocumentIds.Contains("threat") && slotRound.ResultSettlementSlots[1].SourceDocumentIds.Contains("offer"),
            "slot refresh skips already-spoken route authors and prioritizes threat duty after offer duty");
        int slotIds = slotPort.Id;
        WorldDiplomacyDocumentExecutionApplication.RefreshResultSettlementActionSlots(slotPort, slotStorage, slotRound, slotOrch.PruneInvalidOffers);
        Test.True(slotPort.Id == slotIds && slotRound.ResultSettlementSlots.Count == 2, "repeated slot refresh merges existing obligations without duplicate slots");
        slotRound.ResultSettlementPending = false; slotPort.Events.Clear();
        WorldDiplomacyDocumentExecutionApplication.RefreshResultSettlementActionSlots(slotPort, slotStorage, slotRound, slotOrch.PruneInvalidOffers);
        Test.True(slotPort.Events.Count == 0, "non-settling round does not prune or scan slots");
        var (p, orch) = Fixture(); var doc = Document("b", "c"); Run(p, orch, doc);
        Test.True(p.Effects == 2 && p.Events.IndexOf("validate:c") < p.Events.IndexOf("effect:b")
            && p.Events.IndexOf("effect:b") < p.Events.IndexOf("effect:c") && p.Events.IndexOf("history") < p.Events.IndexOf("propagation")
            && p.Events.IndexOf("propagation") < p.Events.IndexOf("round"), "multi action validates entire batch before ordered effects and history/propagation/round");
        Test.True(doc.Actions.All(a => a.ChangedDiplomaticState && a.MechanicalResult.StartsWith("effect ")),
            "every action captures its own actual mechanical receipt");
        foreach (string[] targets in new[] { new[] { "b", "b" }, new[] { "b", "missing" }, new[] { "vassal" } })
        {
            (p, orch) = Fixture(); doc = Document(targets); Run(p, orch, doc);
            Test.True(p.Effects == 0 && !doc.IsReadyForPublication && !p.Events.Contains("history") && p.Events.Any(e => e.StartsWith("reject:")),
                "invalid or duplicate target rejects complete batch before effects/publication");
        }
        (p, orch) = Fixture(x => x.Round = new WorldDiplomacyRound { ResultSettlementPending = true, RelayRouteKingdomIds = new() { "a", "old" } });
        doc = Document("b", "c"); Run(p, orch, doc);
        Test.True(p.Effects == 0 && p.Round.RelayRouteKingdomIds.SequenceEqual(new[] { "a", "old" })
            && p.Events.Contains("reject:result_settlement_target_capacity_reached"), "aggregate capacity rejection cannot partially expand route");
        (p, orch) = Fixture(x => x.Round = new WorldDiplomacyRound { ResultSettlementPending = true, RelayRouteKingdomIds = new() { "a" } });
        doc = Document("c", "b"); Run(p, orch, doc);
        Test.True(p.Effects == 2 && p.Round.RelayRouteKingdomIds.SequenceEqual(new[] { "a", "b", "c" }), "route admission sorted independently of action order");
        (p, orch) = Fixture(x => { x.ThrowEffect = true; x.ThrowHistory = true; }); doc = Document("b", "c"); Run(p, orch, doc);
        Test.True(p.Effects == 2 && doc.Actions.All(a => a.ChangedDiplomaticState) && p.Events.IndexOf("retry") > p.Events.IndexOf("history")
            && p.Events.IndexOf("propagation") > p.Events.IndexOf("retry") && p.Events.Contains("round"),
            "partial applied effect receipts survive exceptions; history retry does not discard declaration or later action");
        (p, orch) = Fixture(x => x.AfterFirstEffect = true); doc = Document("b", "c"); Run(p, orch, doc);
        Test.True(p.Effects == 1 && doc.Actions[0].ChangedDiplomaticState && !doc.Actions[1].ChangedDiplomaticState
            && doc.Actions[1].MechanicalResult.Contains("changed"), "live revalidation between effects prevents newly invalid second action");
        (p, orch) = Fixture(); doc = Document("b"); doc.Actions.Clear(); Run(p, orch, doc);
        Test.True(p.Effects == 1 && p.Events.IndexOf("propagation") < p.Events.IndexOf("history") && p.Events.Contains("round"),
            "legacy single action keeps its existing propagation-before-history ordering");
        (p, orch) = Fixture(x => x.AuthorAllowed = false); doc = Document("b"); Run(p, orch, doc);
        Test.True(p.Effects == 0 && p.Events.Contains("reject:player_controlled_realm_requires_player_authorization"), "AI author authority checked before target work");
        (p, orch) = Fixture(); doc = Document("b"); doc.Actions.Clear(); doc.IsPlayerAuthored = true; doc.Intent = "statement"; Run(p, orch, doc);
        Test.True(p.Effects == 0 && doc.IsReadyForPublication && p.Events.Contains("history"), "player public statement remains publishable without mechanical action");
    }
}
