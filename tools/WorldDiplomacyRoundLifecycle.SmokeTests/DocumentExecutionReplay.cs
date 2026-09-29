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
        public bool HasIndependentWorldDiplomacyAuthority(string id) { return id != "vassal"; }
        public bool CanAiAuthorDiplomaticDocument(string id, out string reason) { reason = "blocked_author"; return AuthorAllowed; }
        public WorldDiplomacyRound ResolveRound(string id) { return Round; }
        public WorldDiplomacyDocument ResolveDocument(string id) { return StoredDocument?.DocumentId == id ? StoredDocument : null; }
        public void PruneInvalidOffers(WorldDiplomacyRound round) { Events.Add("PruneInvalidOffers"); }
        public bool IsNonRootAiRelayNoActionAllowed(WorldDiplomacyRound round, string slot, string author, string target, bool relay, bool external, WorldDiplomacyDocument source) { return NoAction; }
        public List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string author, string target) { return Legal; }
        public List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string author, string target, bool relay, string slot, bool external, WorldDiplomacyDocument source) { return Legal; }
        public bool TryGetDiplomaticStateViolation(string intent, string author, string target, out string reason) { Events.Add("validate:" + target); reason = "changed"; return target == InvalidTarget || (AfterFirstEffect && Effects > 0); }
        public bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument doc, string intent, string commitment, string author, string target, out string reason) { reason = ""; return false; }
        public WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string slot, bool external, string sourceId, bool requireAnyOpenPeaceOffer) { return null; }
        public bool IsAtWar(string author, string target) { return true; }
        public bool IsPlayerKingdom(string id) { return false; }
        public string NewId(string prefix) { return prefix + (++Id); }
        public void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument doc, string reason) { Events.Add("reject:" + reason); }
        public void ExecuteImmediateIntent(string author, string target, string intent, WorldDiplomacyDocument doc) { Test.True(doc.IsReadyForPublication, "validated declaration is publishable before irreversible effect"); Events.Add("effect:" + target); Effects++; doc.MechanicalResult = "effect " + target; doc.ChangedDiplomaticState = true; if (ThrowEffect) throw new Exception("effect failure"); }
        public void ProcessDiplomaticThreatDocument(WorldDiplomacyDocument doc, string author, string target, bool recordTargetDecisions = true) { Events.Add("ProcessDiplomaticThreatDocument"); }
        public void RecordDiplomaticThreatTargetDecisions(WorldDiplomacyDocument doc, string author, string target, string intent) { Events.Add("RecordDiplomaticThreatTargetDecisions"); }
        public void RecordDiplomaticThreatTargetDecisionsForActions(WorldDiplomacyDocument doc, string author) { Events.Add("RecordDiplomaticThreatTargetDecisionsForActions"); }
        public bool DeferUnresolvedRequiredThreatAction(WorldDiplomacyDocument doc, string author, string target, string intent) { return false; }
        public void ApplyDiplomaticThreatReputationPenalty(WorldDiplomacyThreat threat, WorldDiplomacyDocument doc) { Events.Add("ApplyDiplomaticThreatReputationPenalty"); }
        public void TrySettleRelayOffer(WorldDiplomacyDocument doc) { Events.Add("TrySettleRelayOffer"); }
        public void ApplyDiplomaticPressureEffect(WorldDiplomacyDocument doc) { Events.Add("ApplyDiplomaticPressureEffect"); }
        public void SettleInternationalReputationForDocument(WorldDiplomacyDocument doc) { Events.Add("SettleInternationalReputationForDocument"); }
        public void AppendCanonicalDocumentEvents(WorldDiplomacyDocument doc) { Events.Add("history"); if (ThrowHistory) throw new Exception("history failure"); }
        public void HandleRoundDocumentProcessed(WorldDiplomacyDocument doc) { Events.Add("round"); }
        public void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument doc) { Events.Add("RecordDiplomacyWeeklyMaterial"); }
        public void ReconcileAnalyzedPlayerDeclarationWithReachedCourts(WorldDiplomacyDocument doc) { Events.Add("ReconcileAnalyzedPlayerDeclarationWithReachedCourts"); }
        public void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat) { Events.Add("TryAppendDiplomaticThreatHistoryResult"); }
        public void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat) { Events.Add("TryAppendDiplomaticThreatDomesticPenaltyHistoryResult"); }
        public void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat) { Events.Add("TryAppendDiplomaticThreatIssuerRewardHistoryResult"); }
        public void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent item) { Events.Add("TryAppendDiplomaticThreatNonComplianceHistoryResult"); }
        public void ScheduleDeferredCanonicalHistoryRetry(string id) { Events.Add("retry"); }
        public void StartDocumentPropagation(WorldDiplomacyDocument doc, string author) { Events.Add("propagation"); }
        public WarPressureEntry FindWarPressure(string source, string target) { return null; }
        public void AddWarPressure(string source, string target, int delta, string reason, string intent) { Events.Add("AddWarPressure"); }
        public List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId) { return values.Where(x => !string.IsNullOrWhiteSpace(x) && x != excludedId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); }
        public void Log(string message) { Events.Add("log:" + message); }
        public void Notify(string message) { Events.Add("notify:" + message); }
    }
    private static WorldDiplomacyDocument Document(params string[] targets) => new()
    {
        DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = targets[0], Intent = "declare_war", Commitment = "binding",
        Actions = targets.Select((id, i) => new WorldDiplomacyDocumentAction { ActionId = "a" + i, TargetKingdomId = id,
            Intent = "declare_war", Commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("declare_war") }).ToList()
    };
    private static void Run(Port p, WorldDiplomacyDocument doc) => WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(
        p, doc, doc.Intent, doc.Commitment, false, "", 1);
    internal static void Run()
    {
        var slotPort = new Port();
        var slotStorage = new WorldDiplomacyStorage();
        var slotRound = new WorldDiplomacyRound { RoundId = "r", ResultSettlementPending = true, RelayPlanned = true,
            RelayRouteKingdomIds = new() { "a", "b" } };
        slotStorage.Documents.Add(new WorldDiplomacyDocument { RoundId = "r", AuthorKingdomId = "a", IsReadyForPublication = true });
        slotRound.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", ProposerKingdomId = "a", TargetKingdomId = "c", SourceDocumentId = "offer" });
        slotRound.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", ProposerKingdomId = "a", TargetKingdomId = "d", SourceDocumentId = "excess" });
        slotRound.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", ProposerKingdomId = "a", TargetKingdomId = "vassal" });
        slotStorage.DiplomaticThreats.Add(new WorldDiplomacyThreat { Status = "open", TargetDecision = "pending", StageRoundId = "r",
            IssuerKingdomId = "a", TargetKingdomId = "b", StageDocumentId = "threat" });
        WorldDiplomacyDocumentExecutionApplication.RefreshResultSettlementActionSlots(slotPort, slotStorage, slotRound);
        Test.True(slotRound.RelayRouteKingdomIds.SequenceEqual(new[] { "a", "b", "c" })
            && slotRound.PendingOffers[1].Status == "invalidated" && slotRound.PendingOffers[2].Status == "invalidated",
            "slot refresh admits valid new offer targets before rejecting capacity and authority failures");
        Test.True(slotRound.ResultSettlementSlots.Select(s => s.KingdomId).SequenceEqual(new[] { "b", "c" })
            && slotRound.ResultSettlementSlots[0].Kind.Contains("threat_response") && slotRound.ResultSettlementSlots[1].Kind.Contains("offer_response")
            && slotRound.ResultSettlementSlots[0].SourceDocumentIds.Contains("threat") && slotRound.ResultSettlementSlots[1].SourceDocumentIds.Contains("offer"),
            "slot refresh skips already-spoken route authors and prioritizes threat duty after offer duty");
        int slotIds = slotPort.Id;
        WorldDiplomacyDocumentExecutionApplication.RefreshResultSettlementActionSlots(slotPort, slotStorage, slotRound);
        Test.True(slotPort.Id == slotIds && slotRound.ResultSettlementSlots.Count == 2, "repeated slot refresh merges existing obligations without duplicate slots");
        slotRound.ResultSettlementPending = false; slotPort.Events.Clear();
        WorldDiplomacyDocumentExecutionApplication.RefreshResultSettlementActionSlots(slotPort, slotStorage, slotRound);
        Test.True(slotPort.Events.Count == 0, "non-settling round does not prune or scan slots");
        var p = new Port(); var doc = Document("b", "c"); Run(p, doc);
        Test.True(p.Effects == 2 && p.Events.IndexOf("validate:c") < p.Events.IndexOf("effect:b")
            && p.Events.IndexOf("effect:b") < p.Events.IndexOf("effect:c") && p.Events.IndexOf("history") < p.Events.IndexOf("propagation")
            && p.Events.IndexOf("propagation") < p.Events.IndexOf("round"), "multi action validates entire batch before ordered effects and history/propagation/round");
        Test.True(doc.Actions.All(a => a.ChangedDiplomaticState && a.MechanicalResult.StartsWith("effect ")),
            "every action captures its own actual mechanical receipt");
        foreach (string[] targets in new[] { new[] { "b", "b" }, new[] { "b", "missing" }, new[] { "vassal" } })
        {
            p = new Port(); doc = Document(targets); Run(p, doc);
            Test.True(p.Effects == 0 && !doc.IsReadyForPublication && !p.Events.Contains("history") && p.Events.Any(e => e.StartsWith("reject:")),
                "invalid or duplicate target rejects complete batch before effects/publication");
        }
        p = new Port { Round = new WorldDiplomacyRound { ResultSettlementPending = true, RelayRouteKingdomIds = new() { "a", "old" } } };
        doc = Document("b", "c"); Run(p, doc);
        Test.True(p.Effects == 0 && p.Round.RelayRouteKingdomIds.SequenceEqual(new[] { "a", "old" })
            && p.Events.Contains("reject:result_settlement_target_capacity_reached"), "aggregate capacity rejection cannot partially expand route");
        p = new Port { Round = new WorldDiplomacyRound { ResultSettlementPending = true, RelayRouteKingdomIds = new() { "a" } } };
        doc = Document("c", "b"); Run(p, doc);
        Test.True(p.Effects == 2 && p.Round.RelayRouteKingdomIds.SequenceEqual(new[] { "a", "b", "c" }), "route admission sorted independently of action order");
        p = new Port { ThrowEffect = true, ThrowHistory = true }; doc = Document("b", "c"); Run(p, doc);
        Test.True(p.Effects == 2 && doc.Actions.All(a => a.ChangedDiplomaticState) && p.Events.IndexOf("retry") > p.Events.IndexOf("history")
            && p.Events.IndexOf("propagation") > p.Events.IndexOf("retry") && p.Events.Contains("round"),
            "partial applied effect receipts survive exceptions; history retry does not discard declaration or later action");
        p = new Port { AfterFirstEffect = true }; doc = Document("b", "c"); Run(p, doc);
        Test.True(p.Effects == 1 && doc.Actions[0].ChangedDiplomaticState && !doc.Actions[1].ChangedDiplomaticState
            && doc.Actions[1].MechanicalResult.Contains("changed"), "live revalidation between effects prevents newly invalid second action");
        p = new Port(); doc = Document("b"); doc.Actions.Clear(); Run(p, doc);
        Test.True(p.Effects == 1 && p.Events.IndexOf("propagation") < p.Events.IndexOf("history") && p.Events.Contains("round"),
            "legacy single action keeps its existing propagation-before-history ordering");
        p = new Port { AuthorAllowed = false }; doc = Document("b"); Run(p, doc);
        Test.True(p.Effects == 0 && p.Events.Contains("reject:blocked_author"), "AI author authority checked before target work");
        p = new Port(); doc = Document("b"); doc.Actions.Clear(); doc.IsPlayerAuthored = true; doc.Intent = "statement"; Run(p, doc);
        Test.True(p.Effects == 0 && doc.IsReadyForPublication && p.Events.Contains("history"), "player public statement remains publishable without mechanical action");
    }
}
