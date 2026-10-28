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
        internal bool UnknownImmediate, UnknownOffer, PlayerStateBlocked, IdentityInvalidAfterFirstEffect;
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

        public bool IsAtWar(string author, string target) { return true; }
        internal string PlayerKingdomId;
        public bool IsPlayerAffiliated(string id) => id != null && id == PlayerKingdomId;
        public bool IsPlayerKingdom(string id) => id == PlayerKingdomId;
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

        public override List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string author, string target)
            => _p.RestrictRound && round != null ? new() { "accept_peace", "reject_peace" } : _p.Legal;
        public override List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string author, string target, bool relay, string slot, bool external, WorldDiplomacyDocument source) { return _p.Legal; }
        public override bool TryGetDiplomaticStateViolation(string intent, string author, string target, out string reason) { _p.Events.Add("validate:" + target); reason = "changed"; return target == _p.InvalidTarget || (_p.AfterFirstEffect && _p.Effects > 0); }
        public override bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument doc, string intent, string commitment, string author, string target, out string reason) { reason = _p.PlayerStateBlocked || _p.IdentityInvalidAfterFirstEffect ? "player-state-blocked" : "changed"; return target == _p.InvalidTarget || (_p.AfterFirstEffect && _p.Effects > 0) || _p.PlayerStateBlocked || (_p.IdentityInvalidAfterFirstEffect && _p.Effects > 0); }
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
    private static (Port p, Orch orch) Fixture(Action<Port> init = null) { var p = new Port { PlayerKingdomId = "a" }; init?.Invoke(p); return (p, new Orch(p)); }
    internal static void Run()
    {
        {
            var (p,orch)=Fixture(); var conditional=Document("b");
            conditional.Actions[0].DependencyActionIds.Add("previous-ultimatum#action_1");
            Run(p,orch,conditional);
            Test.True(p.Effects==0 && !conditional.IsReadyForPublication && !p.Events.Contains("propagation")
                && p.Events.Contains("reject:conditional_war_requires_prepublication_repair"),
                "lower execution owner refuses false war publication even if drafting validation was bypassed");
        }
        foreach (bool stateBlocked in new[] { false, true })
        {
            var (blockedPort, blockedOrch) = Fixture(p => { p.Legal.Clear(); p.InvalidTarget = stateBlocked ? "b" : null; });
            var blockedDocument = Document("b");
            blockedDocument.IsPlayerAuthored = false;
            Run(blockedPort, blockedOrch, blockedDocument);
            Test.True(stateBlocked ? blockedPort.Effects == 0 && blockedPort.Events.Contains("reject:action_not_executable:changed")
                : blockedPort.Effects == 1 && !blockedPort.Events.Any(x => x.StartsWith("reject:")),
                "current state owns execution admission, not an obsolete cached AI action list");
        }
        foreach (bool legacyPlayer in new[] { false, true })
        {
            var (playerPort, playerOrch) = Fixture(p => { p.Legal.Clear(); p.InvalidTarget = "b"; });
            var playerDocument = Document("b"); playerDocument.IsPlayerAuthored = true;
            if (legacyPlayer) playerDocument.Actions.Clear();
            Run(playerPort, playerOrch, playerDocument);
            Test.True(playerPort.Effects == 0 && playerPort.Events.Contains("reject:action_not_executable:changed"),
                "player legacy and structured actions obey the same current state guard");
        }
        foreach (bool flatPlayer in new[] { false, true })
        {
            var (blockedPlayerPort, blockedPlayerOrch) = Fixture(p => p.PlayerStateBlocked = true);
            var blockedPlayerDocument = Document("b");
            blockedPlayerDocument.IsPlayerAuthored = true;
            if (flatPlayer) blockedPlayerDocument.Actions.Clear();
            Run(blockedPlayerPort, blockedPlayerOrch, blockedPlayerDocument);
            Test.True(blockedPlayerPort.Effects == 0 && blockedPlayerPort.Events.Contains("reject:action_not_executable:player-state-blocked"),
                "player admission retains technical identity and source guards");
        }
        var (batchPlayerPort, batchPlayerOrch) = Fixture(p => p.IdentityInvalidAfterFirstEffect = true);
        var batchPlayerDocument = Document("b", "c");
        batchPlayerDocument.IsPlayerAuthored = true;
        Run(batchPlayerPort, batchPlayerOrch, batchPlayerDocument);
        Test.True(batchPlayerPort.Effects == 1 && batchPlayerDocument.Actions[1].MechanicalResult.Contains("player-state-blocked"),
            "player batch rechecks technical identity and source before each effect");
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

    }
}
