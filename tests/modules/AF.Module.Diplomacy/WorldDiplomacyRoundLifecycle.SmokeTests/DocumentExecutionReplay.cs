using AnimusForge;
using AnimusForge.Refactor.Domain;

internal static class DocumentExecutionReplay
{
    internal sealed class Port : IWorldDiplomacyDocumentExecutionPort
    {
        internal readonly List<string> Events = new();
        internal readonly List<string> Legal = new() { "declare_war", "statement" };
        internal WorldDiplomacyRound Round;
        internal WorldDiplomacyOrchestration Owner;
        internal WorldDiplomacyDocument StoredDocument;
        internal bool AuthorAllowed = true, NoAction, ThrowEffect, ThrowHistory, AfterFirstEffect;
        internal bool UnknownImmediate, UnknownOffer, PlayerStateBlocked;
        internal bool RestrictRound;
        internal WorldDiplomacyRoundOffer RequiredPeace;
        internal int RequiredPeaceReads;
        internal Action BeforeFirstResolve;
        internal string InvalidTarget;
        internal int Effects, Id;
        public int MaxDiplomaticActionsPerDocument => 4;
        public int MaxRelayParticipants => 3;
        public int CurrentDay => 12;
        public IReadOnlyList<WorldDiplomacyThreat> Threats { get; } = new List<WorldDiplomacyThreat>();
        public string ResolveKingdomId(string id)
        {
            Action callback = BeforeFirstResolve;
            BeforeFirstResolve = null;
            callback?.Invoke();
            return id == "missing" || string.IsNullOrEmpty(id) ? null : id;
        }
        public bool IsEliminated(string id) { return false; }
        public WorldDiplomacyAuthoritySnapshot CaptureAuthority(string id) => new(id, id != null, false, id == "vassal", "suzerain", !AuthorAllowed, true);
        public WorldDiplomacyRound ResolveRound(string id) { return Owner?.ResolveRound(id) ?? Round; }
        public WorldDiplomacyDocument ResolveDocument(string id) { return Owner?.ResolveDocument(id) ?? (StoredDocument?.DocumentId == id ? StoredDocument : null); }
        public WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string slot, bool external, string sourceId, bool requireAnyOpenPeaceOffer)
        { RequiredPeaceReads++; return RequiredPeace; }
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
        public override List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string author, string target)
            => _p.RestrictRound && round != null ? new() { "accept_peace", "reject_peace" } : _p.Legal;
        public override List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string author, string target, bool relay, string slot, bool external, WorldDiplomacyDocument source) { return _p.Legal; }
        public override bool TryGetDiplomaticStateViolation(string intent, string author, string target, out string reason) { _p.Events.Add("validate:" + target); reason = "changed"; return target == _p.InvalidTarget || (_p.AfterFirstEffect && _p.Effects > 0); }
        public override bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument doc, string intent, string commitment, string author, string target, out string reason) { reason = "player-state-blocked"; return _p.PlayerStateBlocked || (_p.AfterFirstEffect && _p.Effects > 0); }
        public override void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument doc, string reason) { _p.Events.Add("reject:" + reason); }
        public override WorldDiplomacyImmediateActionReceipt ExecuteImmediateIntent(string author, string target, string intent, WorldDiplomacyDocument doc)
        {
            Test.True(doc.IsReadyForPublication, "validated declaration is publishable before irreversible effect");
            _p.Events.Add("effect:" + target);
            _p.Events.Add("effect-record:" + doc.TargetKingdomId + ":" + doc.PeaceTerms?.DailyTribute + ":" + doc.RespondingToOfferActionId);
            _p.Effects++;
            doc.MechanicalResult = _p.UnknownImmediate ? "result unknown" : "effect " + target;
            doc.ChangedDiplomaticState = !_p.UnknownImmediate;
            if (_p.ThrowEffect) throw new Exception("effect failure");
            return new WorldDiplomacyImmediateActionReceipt(!_p.UnknownImmediate, doc.MechanicalResult,
                known: !_p.UnknownImmediate);
        }
        public override void ProcessDiplomaticThreatDocument(WorldDiplomacyDocument doc, string author, string target, bool recordTargetDecisions) { _p.Events.Add("ProcessDiplomaticThreatDocument"); }
        public override void RecordDiplomaticThreatTargetDecisions(WorldDiplomacyDocument doc, string author, string target, string intent) { _p.Events.Add("RecordDiplomaticThreatTargetDecisions"); }
        public override void RecordDiplomaticThreatTargetDecisionsForActions(WorldDiplomacyDocument doc, string author) { _p.Events.Add("RecordDiplomaticThreatTargetDecisionsForActions"); }
        public override bool DeferUnresolvedRequiredThreatAction(WorldDiplomacyDocument doc, string author, string target, string intent) { return false; }
        public override void ApplyDiplomaticThreatReputationPenalty(WorldDiplomacyThreat threat, WorldDiplomacyDocument doc) { _p.Events.Add("ApplyDiplomaticThreatReputationPenalty"); }
        public override WorldDiplomacyOfferOutcome TrySettleRelayOffer(WorldDiplomacyDocument doc)
        {
            _p.Events.Add("TrySettleRelayOffer");
            return _p.UnknownOffer ? WorldDiplomacyOfferOutcome.Unknown : WorldDiplomacyOfferOutcome.None;
        }
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
        foreach (bool stateBlocked in new[] { false, true })
        {
            var (blockedPort, blockedOrch) = Fixture(p => { p.Legal.Clear(); p.InvalidTarget = stateBlocked ? "b" : null; });
            var blockedDocument = Document("b");
            blockedDocument.IsPlayerAuthored = false;
            Run(blockedPort, blockedOrch, blockedDocument);
            string expected = "final_live_legal_action_guard" + (stateBlocked ? ":changed" : "");
            Test.True(blockedPort.Effects == 0 && blockedPort.Events.Contains("reject:" + expected)
                && blockedPort.Events.Any(x => x.StartsWith("log:final diplomacy action rejected") && x.Contains("reason=" + expected)),
                "rejected AI war preserves specific state reason or contextual fallback");
        }
        foreach (bool legacyPlayer in new[] { false, true })
        {
            var (playerPort, playerOrch) = Fixture(p => { p.Legal.Clear(); p.InvalidTarget = "b"; });
            var playerDocument = Document("b");
            playerDocument.IsPlayerAuthored = true;
            if (legacyPlayer) playerDocument.Actions.Clear();
            Run(playerPort, playerOrch, playerDocument);
            Test.True(playerPort.Effects == 1 && !playerPort.Events.Any(x => x.StartsWith("reject:")),
                "player legacy and structured war bypass AI list and AI state checks");
        }
        foreach (bool flatPlayer in new[] { false, true })
        {
            var (blockedPlayerPort, blockedPlayerOrch) = Fixture(p => p.PlayerStateBlocked = true);
            var blockedPlayerDocument = Document("b");
            blockedPlayerDocument.IsPlayerAuthored = true;
            if (flatPlayer) blockedPlayerDocument.Actions.Clear();
            Run(blockedPlayerPort, blockedPlayerOrch, blockedPlayerDocument);
            Test.True(blockedPlayerPort.Effects == 0 && blockedPlayerPort.Events.Contains("reject:final_live_state_guard:player-state-blocked"),
                "player admission cannot bypass retained live-state restrictions");
        }
        var (batchPlayerPort, batchPlayerOrch) = Fixture(p => p.AfterFirstEffect = true);
        var batchPlayerDocument = Document("b", "c");
        batchPlayerDocument.IsPlayerAuthored = true;
        Run(batchPlayerPort, batchPlayerOrch, batchPlayerDocument);
        Test.True(batchPlayerPort.Effects == 1 && batchPlayerDocument.Actions[1].MechanicalResult.Contains("player-state-blocked"),
            "player batch rechecks retained state before each effect");
        var frozenDocument = Document("b");
        frozenDocument.RoundId = "round-1";
        frozenDocument.SourceDocumentId = "source-1";
        frozenDocument.Actions[0].RespondingToOfferActionId = "offer-v1";
        frozenDocument.Actions[0].PeaceTerms = new WorldDiplomacyPeaceTerms { DailyTribute = 12, CessionSettlementId = "castle-1" };
        var frozen = new WorldDiplomacyDocumentExecutionCommand(frozenDocument, frozenDocument.Actions);
        frozenDocument.RoundId = "round-2";
        frozenDocument.SourceDocumentId = "source-2";
        frozenDocument.Actions[0].TargetKingdomId = "changed";
        frozenDocument.Actions[0].RespondingToOfferActionId = "offer-v2";
        frozenDocument.Actions[0].PeaceTerms.DailyTribute = 99;
        Test.True(frozen.DocumentId == "d" && frozen.RoundId == "round-1"
            && frozen.SourceDocumentId == "source-1" && frozen.AuthorKingdomId == "a"
            && frozen.ActionCount == 1 && frozen.ActionAt(0).TargetKingdomId == "b"
            && frozen.ActionAt(0).RespondingToOfferActionId == "offer-v1"
            && frozen.ActionAt(0).HasPeaceTerms && frozen.ActionAt(0).DailyTribute == 12
            && frozen.ActionAt(0).CessionSettlementId == "castle-1",
            "document command freezes IDs, offer version and peace terms before effect admission");
        var (snapshotPort, snapshotOrch) = Fixture();
        var snapshotDocument = Document("b");
        snapshotDocument.Actions[0].RespondingToOfferActionId = "offer-v1";
        snapshotDocument.Actions[0].PeaceTerms = new WorldDiplomacyPeaceTerms { DailyTribute = 12 };
        snapshotPort.BeforeFirstResolve = () =>
        {
            snapshotDocument.Actions[0].TargetKingdomId = "c";
            snapshotDocument.Actions[0].RespondingToOfferActionId = "offer-v2";
            snapshotDocument.Actions[0].PeaceTerms.DailyTribute = 99;
            snapshotDocument.Actions.Clear();
        };
        WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(snapshotPort, snapshotOrch,
            snapshotDocument, snapshotDocument.Intent, snapshotDocument.Commitment, false, "", 1,
            out List<WorldDiplomacyDocumentActionReceipt> snapshotReceipts);
        Test.True(snapshotPort.Events.Contains("effect:b")
            && snapshotPort.Events.Contains("effect-record:b:12:offer-v1")
            && snapshotDocument.TargetKingdomId == "b"
            && snapshotDocument.Actions[0].TargetKingdomId == "b"
            && snapshotDocument.Actions[0].PeaceTerms.DailyTribute == 12
            && snapshotDocument.Actions[0].RespondingToOfferActionId == "offer-v1"
            && snapshotReceipts.Count == 1 && snapshotReceipts[0].OutcomeKnown,
            "entry snapshot remains the executed and saved input after a synchronous port mutates the canonical action");
        (snapshotPort, snapshotOrch) = Fixture(x => x.UnknownImmediate = true);
        snapshotDocument = Document("b");
        WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(snapshotPort, snapshotOrch,
            snapshotDocument, snapshotDocument.Intent, snapshotDocument.Commitment, false, "", 1,
            out snapshotReceipts);
        Test.True(snapshotReceipts.Count == 1 && snapshotReceipts[0].EffectAttempted
            && !snapshotReceipts[0].OutcomeKnown && !snapshotReceipts[0].Applied
            && snapshotDocument.Actions[0].MechanicalResult == "result unknown",
            "a nonthrowing game port can report an unknown immediate-effect outcome");
        (snapshotPort, snapshotOrch) = Fixture(x => x.UnknownOffer = true);
        snapshotDocument = Document("b");
        WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(snapshotPort, snapshotOrch,
            snapshotDocument, snapshotDocument.Intent, snapshotDocument.Commitment, false, "", 1,
            out snapshotReceipts);
        Test.True(snapshotReceipts.Count == 1 && snapshotReceipts[0].Applied
            && !snapshotReceipts[0].OutcomeKnown && snapshotDocument.ChangedDiplomaticState,
            "a confirmed immediate effect survives an unknown later offer-settlement receipt");
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
        (p, orch) = Fixture(); doc = Document("b", "c", "d", "e", "f"); Run(p, orch, doc);
        Test.True(p.Effects == 0 && p.Events.Count == 0 && !doc.IsReadyForPublication,
            "oversized persisted action lists are rejected before snapshot capture and live-world work");
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
        Test.True(doc.Actions.Count == 0 && doc.ProcessingActionId == "", "flat save representation is not converted to stored actions");
        (p, orch) = Fixture(); doc = Document("missing"); doc.Actions = null; doc.IsPlayerAuthored = true; doc.Intent = "statement";
        Run(p, orch, doc);
        Test.True(doc.IsReadyForPublication && p.Effects == 0 && doc.Actions == null, "legacy public declaration needs no live foreign target and keeps null actions");
        (p, orch) = Fixture(x => x.NoAction = true); doc = Document("b"); doc.Actions = null;
        doc.Intent = "statement"; doc.IsRoundResponseNoActionDeclaration = true; Run(p, orch, doc);
        Test.True(doc.IsReadyForPublication && p.Effects == 0 && p.Events.Contains("RecordDiplomaticThreatTargetDecisions"), "legacy relay no-action declaration remains mechanically inert");
        (p, orch) = Fixture(); doc = Document("b"); doc.Actions = null; doc.Intent = "statement";
        doc.IsRoundResponseNoActionDeclaration = true; Run(p, orch, doc);
        Test.True(!doc.IsReadyForPublication && p.Events.Contains("reject:stale_round_response_no_action_declaration"), "stale no-action declaration cannot publish");
        foreach (bool flat in new[] { false, true })
        {
            (p, orch) = Fixture(x => x.InvalidTarget = "b"); doc = Document("b");
            if (flat) doc.Actions = null;
            Run(p, orch, doc);
            Test.True(p.Effects == 0 && !p.Events.Contains("history") && p.Events.Contains("reject:final_live_state_guard:changed"), "one executor rejects changed live state in both representations");
        }
        (p, orch) = Fixture(x => x.AuthorAllowed = false); doc = Document("b"); Run(p, orch, doc);
        Test.True(p.Effects == 0 && p.Events.Contains("reject:player_controlled_realm_requires_player_authorization"), "AI author authority checked before target work");
        (p, orch) = Fixture(); doc = Document("b"); doc.Actions.Clear(); doc.IsPlayerAuthored = true; doc.Intent = "statement"; Run(p, orch, doc);
        Test.True(p.Effects == 0 && doc.IsReadyForPublication && p.Events.Contains("history"), "player public statement remains publishable without mechanical action");
    }
}
