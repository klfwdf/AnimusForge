using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Exercises the actual module owner, not a predecessor Behavior or copied algorithm.
internal static class ConcurrentOralMigrationReplay
{
    internal sealed class Host : FakeOrchestrationHost, IWorldDiplomacyDialogueHost
    {
        internal WorldDiplomacyOrchestration Owner;
        internal bool PublishEnabled;
        internal readonly List<string> Notices = new();
        internal readonly List<string> Facts = new();
        internal bool Dead, Cycle, Partial;
        internal int Effects, Counter, TradeDuration;
        public override int MaxPendingJobs() => 24;
        public override int MaxStoredDocuments() => 2000;
        public override int RoundTargetDurationDays() => 15;
        public override int RoundHardDurationDays(int days) => 18;
        public override int RoundParticipantLimit() => 8;
        public override long HistoryCompressionTriggerTokens() => int.MaxValue;
        public override int EstimateTokens(string value) => Math.Max(1, value?.Length ?? 0);
        public override int CurrentHour() => CurrentDayValue * 24;
        public override string NewId(string prefix) => prefix + "_" + ++Counter;
        public override bool WorldDiplomacyEnabled() => true;
        public override string ResolvePartyId(string id) => string.IsNullOrWhiteSpace(id) ? null : id;
        public override string ResolveRepresentativeId(string id) => id;
        public override bool PartyResolved(string id) => !string.IsNullOrWhiteSpace(id);
        public override bool HasIndependentAuthority(string id) => !string.IsNullOrWhiteSpace(id);
        public override bool IsPlayerParty(string id) => id == "p";
        public override bool IsPlayerAffiliatedParty(string id) => id == "p";
        public override string PlayerKingdomId() => "p";
        public override bool CanAiAuthorParty(string id, out string reason) { reason = ""; return !Dead && id != "p"; }
        public override string PartyNameOrEmpty(string id) => id ?? "";
        public override string PartyRulerId(string id) => string.IsNullOrEmpty(id) ? "" : "ruler_" + id;
        public override string PartyRulerName(string id) => PartyRulerId(id);
        public override IReadOnlyList<string> AllKingdomIds() => new[] { "a", "b", "c", "p" };
        public override bool CampaignHasKingdoms() => true;
        public override bool AllianceKnown() => true;
        public override bool TradeKnown() => true;
        public override void Notify(string value) => Notices.Add(value);
        public override IWorldDiplomacyOfferActionPort OfferAction() => new OfferPort(this);
        public override IWorldDiplomacyPublicationPort Publication() => new PublicationPort(this);
        public override int MaxPropagationArrivalsPerDay() => 1200;
        public override string ResolvePropagationReceiverId(string kingdomId, string settlementId) => kingdomId ?? settlementId;
        public override IWorldDiplomacyHistoryCapturePort HistoryCapture() => new HistoryPort(this);
        public override IWorldDiplomacyDocumentExecutionPort DocumentExecution() => PublishEnabled
            ? new DocumentExecutionReplay.Port { Owner = Owner } : null;
        public override IWorldDiplomacyActionSelectionPort ActionSelection() => PublishEnabled ? new SelectionPort(this) : null;
        public override IWorldDiplomacyThreatBindingPort ThreatBinding() => PublishEnabled ? new SelectionPort(this) : null;
        public override IWorldDiplomacyPrestigePort Prestige() => PublishEnabled ? new PrestigeApplicationReplay.Port() : null;
        public bool RulerAlive(string id) => !Dead;
        public bool IsPlayerRuler(string id) => id == "ruler_p";
        public string RulerName(string id) => id;
        public (bool available, bool cycle, bool existing) TreatyState(string receiving, string joining) => (true, Cycle, false);
        public (bool success, bool changed, string reason) ExecuteTreaty(string intent, string receiving, string joining)
        { Effects++; return (!Partial, true, Partial ? "partial" : "complete"); }
        public MemoryCommitResult CommitFact(string ruler, string source, string fact, int day,
            string location, int hour = -1, string npcName = null, string gameDate = "")
        { Facts.Add(source); return new MemoryCommitResult(MemoryCommitStatus.Applied); }
        public string PersonalMemory(string ruler, string topic, string counterpart) => "PRIVATE_MEMORY";
    }
    private sealed class PublicationPort : IWorldDiplomacyPublicationPort
    {
        private readonly Host _h;
        internal PublicationPort(Host host) => _h = host;
        public WorldDiplomacyStorage Storage => _h.Owner.CurrentStorage;
        public string ResolveKingdomId(string id) => _h.ResolvePartyId(id);
        public bool CanAiAuthor(string id, out string reason) => _h.CanAiAuthorParty(id, out reason);
        public bool HasAuthority(string id) => _h.HasIndependentAuthority(id);
        public bool IsPlayerAffiliated(string id) => _h.IsPlayerAffiliatedParty(id);
        public bool IsPlayerKingdom(string id) => _h.IsPlayerParty(id);
        public bool RepresentsAddressedVassal(string id, WorldDiplomacyDocument document) => false;
        public WorldDiplomacyRound ResolveRound(string id) => _h.Owner.ResolveRound(id);
        public string ResolveOriginSettlementId(string author) => "capital_" + author;
        public WorldDiplomacyPublicationSnapshot CaptureDestinations(string author, string origin) => new(
            new[] { new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "village", Distance = 10 } },
            _h.AllKingdomIds().Where(x => x != author).Select(x => new WorldDiplomacyPropagationApplication.CourtTarget
                { KingdomId = x, SettlementId = "capital_" + x, Distance = 10, IsPlayerAffiliated = x == "p" }).ToList(), 10, 10);
        public int CurrentDay => _h.CurrentDayValue;
        public int ParticipantLimit => 8;
        public int CivilianSpreadDays => 8;
        public int CourtDeliveryDays => 7;
        public void Log(string message) { }
    }
    private sealed class SelectionPort : IWorldDiplomacyActionSelectionPort, IWorldDiplomacyThreatBindingPort
    {
        private readonly Host _h;
        internal SelectionPort(Host h) => _h = h;
        public IEnumerable<string> KingdomIds() => _h.AllKingdomIds();
        public bool HasAuthority(string id) => true;
        public bool IsEliminated(string id) => false;
        public WorldDiplomacyPairFacts CapturePair(string first, string second) => new(false, true, true, false, false);
        public IReadOnlyList<WorldDiplomacyThreat> Threats => Array.Empty<WorldDiplomacyThreat>();
        public int LastFailedRoundDay(WorldDiplomacyOfferCooldownKey key) => -1;
        public int CooldownDays() => 0;
        public int CurrentDay() => _h.CurrentDayValue;
        public WorldDiplomacyDocument ResolveDocument(string id) => _h.Owner.ResolveDocument(id);
        public WorldDiplomacyRound ResolveRound(string id) => _h.Owner.ResolveRound(id);
        public bool IsPolicyActive(string policy, string owner, string affected) => false;
        public string RepresentativeId(string id) => id;
        public string NewId(string kind) => _h.NewId(kind);
        public int EscalationPrestigeReward => 0;
        public int WarPrestigeReward => 0;
        public void Log(string message) { }
        public IWorldDiplomacyWarAdmissionPort CaptureWarAdmission(string first, string second) => new ActionSelectionReplay.Admission(false);
        public IWorldDiplomacyNoActionPort CaptureNoActionPort(string author, string target) => new ActionSelectionReplay.NoActionPort(author, target);
    }
    private sealed class HistoryPort : IWorldDiplomacyHistoryCapturePort
    {
        private readonly Host _h;
        internal HistoryPort(Host h) => _h = h;
        public int CurrentHour() => _h.CurrentHour();
        public long WeeklyRevision() => 0;
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => Array.Empty<WorldDiplomacyWeeklyArtifact>();
    }
    private sealed class OfferPort : IWorldDiplomacyOfferActionPort, IWorldDiplomacyTimedTradePort
    {
        private readonly Host _h;
        internal OfferPort(Host h) => _h = h;
        public WorldDiplomacyStorage Storage => _h.Owner.CurrentStorage;
        public int CurrentDay => _h.CurrentDayValue;
        public WorldDiplomacyRound ResolveRound(string id) => _h.Owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => _h.Owner.ResolveDocument(id);
        public bool ResolveParties(WorldDiplomacyRoundOffer offer) => true;
        public WorldDiplomacyOfferActionReceipt ExecutePeace(string first, string second, WorldDiplomacyPeaceTerms terms) => new(false, "unused");
        public WorldDiplomacyCessionReceipt ApplyCession(string first, string second, WorldDiplomacyPeaceTerms terms) => default;
        public WorldDiplomacyOfferActionReceipt ExecuteAlliance(string first, string second) => new(false, "unused");
        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string first, string second) => new(false, "unused");
        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string first, string second, int durationDays)
        { _h.TradeDuration = durationDays; return new(true, "trade applied"); }
        public WorldDiplomacyOfferActionReceipt ReadPeace(string first, string second, WorldDiplomacyPeaceTerms terms) => new(false, "unused");
        public bool HasTakenEffect(string intent, string first, string second) => false;
        public void Log(string value) { }
    }
    private static object Invoke(WorldDiplomacyOrchestration owner, string method, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
    internal static (Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new Host(); var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; return (h, o);
    }
    private static WorldDiplomacyDocument Doc(string id, WorldDiplomacyRound round, string author, string target, bool player = false) => new()
    { DocumentId = id, RoundId = round.RoundId, ExchangeId = round.RoundId, AuthorKingdomId = author,
        AuthorKingdomName = author, AuthorRulerId = "ruler_" + author, TargetKingdomId = target,
        IsPlayerAuthored = player, IsReadyForPublication = true, AnalysisStatus = "success",
        Body = id + " substantive request", Intent = "statement", Day = 12, CreatedUtcTicks = DateTime.UtcNow.Ticks };

    internal static void Run()
    {
        var (h, o) = Fixture();
        var a = o.EnsureActiveRound("a", "b", false);
        var c = o.EnsureActiveRound("c", "a", false);
        Test.True(a != c && o.CurrentStorage.ConcurrentRounds.Contains(c), "different NPC initiators coexist");
        Test.True(ReferenceEquals(o.EnsureActiveRound("a", "c", false), a), "one ordinary initiated event per NPC");
        Test.True(c.Participants.Any(x => x.KingdomId == "a"), "busy initiator can join another event");
        Test.True(ReferenceEquals(o.ResolveRound(c.RoundId), c), "module resolves concurrent event IDs");
        var p1 = o.EnsureActiveRound("p", "b", true); var p2 = o.EnsureActiveRound("p", "c", true);
        Test.True(p1 != p2, "player multiple initiated events remain allowed");

        // Existing B speech becomes the substantive response in A/B/C's negotiation.
        var root = Doc("trade_root", a, "a", "b"); root.Intent = "propose_trade"; o.CurrentStorage.Documents.Add(root); a.RootDocumentId = root.DocumentId;
        var player = Doc("p_ask", a, "p", "b", true); player.AddressedKingdomIds = new() { "b", "c" };
        o.CurrentStorage.Documents.Add(player);
        a.RelayPlanned = true; a.RelayWaiting = true; a.RelayCursor = 0; a.RelaySequence = 7;
        a.RelayRouteKingdomIds = new() { "a", "b", "c", "p" };
        o.CurrentStorage.RelayArrivals.Add(new() { RoundId = a.RoundId, FromKingdomId = "a", ToKingdomId = "c", DueDay = 14, Sequence = 7 });
        var bJob = new WorldDiplomacyJob { JobId = "b_job", Kind = "generate", RoundId = a.RoundId, AuthorKingdomId = "b", Priority = 20 };
        var cJob = new WorldDiplomacyJob { JobId = "c_job", Kind = "generate", RoundId = a.RoundId, AuthorKingdomId = "c", Priority = 20 };
        o.CurrentStorage.Jobs.AddRange(new[] { bJob, cJob });
        Invoke(o, "RegisterPlayerResponseWork", player, true);
        Test.True(a.PlayerResponses.Count == 2 && a.PlayerResponses.All(x => x.SourceDocumentId == "p_ask"), "all explicitly addressed eligible NPCs owe a response");
        Test.True(bJob.Priority == 20 && bJob.PlayerResponseSourceIds.Count == 0,
            "unreceived player speech persists as an obligation without leaking into queued speech");
        foreach (string receiver in new[] { "b", "c" })
            WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, player, receiver, 12,
                () => false, () => o.ProcessCourtArrival(receiver, player));
        Test.True(o.CurrentStorage.Jobs.Count == 2 && bJob.Priority == 95 && bJob.PlayerResponseSourceIds.SequenceEqual(new[] { "p_ask" }), "player merges into existing country speech instead of side conversation");
        Test.True(a.RelayCursor == 0 && a.RelayWaiting && o.CurrentStorage.RelayArrivals.Single().ToKingdomId == "c",
            "priority merging preserves the original relay cursor, wait state and next speaker");
        var reply = Doc("priority_reply", a, "b", "p"); reply.IsRelayTurn = true; reply.IsExternalResponseOnly = true;
        int moved = 0;
        WorldDiplomacyRoundProgressApplication.HandleRoundDocumentProcessed(reply, o.CurrentStorage,
            o.ResolveRound, o.ResolveDocument, () => 12, (_, _, _, _) => moved++, (_, _) => moved++, (_, _) => moved++,
            _ => moved++, (_, _) => moved++, _ => moved++, _ => moved++, _ => moved++, _ => { });
        Test.True(moved == 0 && a.RelayCursor == 0 && a.RelayWaiting && reply.RoundProgressHandled,
            "completed priority reply uses original external-response accounting without consuming the relay hop");
        var rejectedPriority = new WorldDiplomacyJob { JobId = "rejected_priority", Kind = "generate", RoundId = a.RoundId,
            AuthorKingdomId = "b", IsRelayTurn = true, IsExternalResponseOnly = true };
        o.AbandonRejectedGeneration(rejectedPriority, "b", "p", "test_failure");
        Test.True(a.RelayCursor == 0 && a.RelayWaiting && o.CurrentStorage.RelayArrivals.Count == 1
            && a.ConsecutiveTechnicalGenerationFailures == 0 && a.PlayerResponses.All(x => x.Status == "pending"),
            "failed priority reply retains obligations and does not advance or trip the ordinary relay");
        Invoke(o, "RegisterPlayerResponseWork", player, true);
        Test.True(a.PlayerResponses.Count == 2, "source by country obligations are idempotent");
        string tail = DiplomacyRoundWorkRules.BuildRequestTail(a, o.CurrentStorage.Documents, bJob.PlayerResponseSourceIds);
        Test.True(tail.Contains("trade_root") && tail.Contains("p_ask") && tail.Contains(a.RoundId), "one shared context contains negotiation and player's latest intervention");
        Test.True((bool)Invoke(o, "ValidatePlayerResponseCoverage", bJob, JObject.Parse("{\"answered_player_document_ids\":[]}")) == false, "unanswered claim cannot discharge obligation");
        Test.True((bool)Invoke(o, "ValidatePlayerResponseCoverage", bJob, JObject.Parse("{\"answered_player_document_ids\":[\"p_ask\"]}")), "frozen request source must be covered");
        var lateAsk = Doc("later_ask", a, "p", "b", true); o.CurrentStorage.Documents.Add(lateAsk);
        Invoke(o, "RegisterPlayerResponseWork", lateAsk, false);
        var answer = Doc("b_answer", a, "b", "p"); Invoke(o, "CommitPlayerResponseCoverage", bJob, answer);
        Test.True(a.PlayerResponses.Single(x => x.KingdomId == "b" && x.SourceDocumentId == "p_ask").Status == "answered"
            && a.PlayerResponses.Single(x => x.SourceDocumentId == "later_ask").Status == "pending", "new unseen ask never receives credit from frozen old request");

        o.CurrentStorage.RequestBudget.AdvanceDay(12);
        for (int i = 0; i < 8; i++) Test.True(o.CurrentStorage.RequestBudget.TryAdmit(false), "shared normal/oral quota permits eight actual sends");
        Test.True(!o.CurrentStorage.RequestBudget.TryAdmit(false), "four player requests reserved");
        for (int i = 0; i < 4; i++) Test.True(o.CurrentStorage.RequestBudget.TryAdmit(true), "player reserve remains available");
        Test.True(!o.CurrentStorage.RequestBudget.TryAdmit(true), "total actual sends bounded at twelve");
        o.CurrentStorage.RequestBudget.AdvanceDay(12); Test.True(o.CurrentStorage.RequestBudget.Started == 12, "pause and repeated same day do not refill");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage));
        var (_, reload) = Fixture(); reload.ReplaceStorage(restored);
        Test.True(reload.CurrentStorage.RequestBudget.Started == 12 && reload.ResolveRound(c.RoundId) != null, "save reload keeps spent bank and concurrent event identity");
        reload.CurrentStorage.RequestBudget.AdvanceDay(13); Test.True(reload.CurrentStorage.RequestBudget.Started == 0, "new game day refills once");

        Test.True(o.RequestLeases.TryClaim(bJob, 5, 1024, 1000, true, out var bLease), "first event speech admitted");
        Test.True(!o.RequestLeases.TryClaim(cJob, 5, 1024, 1000, true, out _), "same event cannot generate two speeches simultaneously");
        var other = new WorldDiplomacyJob { JobId = "other", Kind = "generate", RoundId = c.RoundId, AuthorKingdomId = "a" };
        Test.True(o.RequestLeases.TryClaim(other, 5, 1024, 1000, false, out var otherLease), "different event can run concurrently");
        Test.True(!o.RequestLeases.TryRelease(bJob.JobId, 4, bLease.Attempt), "old generation cannot release current attempt");
        Test.True(!o.RequestLeases.TryRelease(bJob.JobId, 5, bLease.Attempt + 1), "wrong attempt cannot release current request");
        bJob.RoundConversationRevision = a.ConversationRevision;
        var completion = new LlmJobResult { Success = true, RequestAttempt = bJob.RequestAttempt, RoundConversationRevision = a.ConversationRevision };
        c.ConversationRevision++;
        Test.True(o.AdmitCompletion(bJob, completion), "unrelated event change does not invalidate current event output");
        a.ConversationRevision++;
        Test.True(!o.AdmitCompletion(bJob, completion) && o.CurrentStorage.Jobs.Contains(bJob), "own event change drops stale speech and retains work");
        bJob.IsRunning = true;
        Test.True(!o.AdmitCompletion(bJob, new LlmJobResult { RequestAttempt = bJob.RequestAttempt + 1 }) && bJob.IsRunning, "late attempt cannot reset new running job");
        bJob.IsRunning = false;
        var deferred = new LlmJobResult { RequestAttempt = bJob.RequestAttempt, Error = "world_diplomacy_request_budget_deferred" };
        Test.True(!o.AdmitCompletion(bJob, deferred) && o.CurrentStorage.Jobs.Contains(bJob), "deferred quota never erases player work");
        var retry = new WorldDiplomacyJob { JobId = "analysis_retry", Kind = "analyze" };
        Test.True(!o.AdmitCompletion(retry, new LlmJobResult { IsServiceFailure = true }) && retry.RetryAfterUtcTicks > DateTime.UtcNow.Ticks,
            "technical retry uses real time while game is paused");
        o.RequestLeases.Reset(); Test.True(bLease.Cancellation.IsCancellationRequested && o.RequestLeases.Count == 0, "runtime reset cancels detached transport");

        var news = Doc("public_news", c, "c", "a"); news.PropagationCompleted = false; o.CurrentStorage.Documents.Add(news);
        o.StartDocumentPropagation(news, "c");
        Test.True(news.PropagationCompleted && o.CurrentStorage.KingdomKnowledge.Single(x => x.KingdomId == "c").DocumentIds.Contains(news.DocumentId)
            && !(bool)Invoke(o, "DialogueDocumentKnown", "a", news.DocumentId), "publication knows only its author until delivery");
        Test.True(o.CurrentStorage.PropagationArrivals.Count(x => x.DocumentId == news.DocumentId) == 4,
            "publication queues three court deliveries and civilian spread instead of granting global knowledge");
        var propagationSave = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage));
        var (deliveryHost, deliveryOwner) = Fixture(); deliveryOwner.ReplaceStorage(propagationSave);
        deliveryHost.CurrentDayValue = 18; deliveryOwner.ProcessPropagationArrivals();
        Test.True(!(bool)Invoke(deliveryOwner, "DialogueDocumentKnown", "a", news.DocumentId), "save reload retains future arrival and no early knowledge");
        deliveryHost.CurrentDayValue = 19; deliveryOwner.ProcessPropagationArrivals();
        Test.True((bool)Invoke(deliveryOwner, "DialogueDocumentKnown", "a", news.DocumentId)
            && deliveryOwner.ResolveDocument(news.DocumentId).HasReachedPlayerCourt, "due court delivery restores ruler knowledge and player notice eligibility");
        int deliveredFacts = deliveryHost.Facts.Count;
        deliveryOwner.ProcessPropagationArrivals();
        Test.True(deliveryHost.Facts.Count == deliveredFacts, "repeated daily delivery cannot duplicate personal memory");
        Test.True(deliveryOwner.CurrentStorage.PropagationArrivals.Count(x => x.DocumentId == news.DocumentId) == 1,
            "civilian spread remains pending after court delivery");
        deliveryHost.CurrentDayValue = 20; deliveryOwner.ProcessPropagationArrivals();
        Test.True(deliveryOwner.CurrentStorage.SettlementKnowledge.Any(x => x.SettlementId == "village" && x.DocumentIds.Contains(news.DocumentId)),
            "civilian location knowledge appears at its own configured date");
        o.CurrentStorage.PlayerOpportunities.Add(new() { RoundId = a.RoundId, ArrivedDay = 10 });
        o.NotifyPlayerWaitRemaining(a); o.NotifyPlayerWaitRemaining(a);
        Test.True(h.Notices.Count(x => x.Contains("剩余 3")) == 1, "remaining player wait shown once per event/day");

        // Real oral consent path remains independent and private while publication is deferred.
        var origin = new DialogueInteractionOrigin("courier", "interaction_1", "session_1", "PRIVATE_PLAYER", "PRIVATE_NPC");
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=b;days=14", origin);
        var arrangement = o.CurrentStorage.DialogueArrangements.Single();
        var oral = o.ResolveRound(arrangement.RoundId);
        Test.True(oral != a && oral.EventSourceType == "dialogue_commitment" && oral.InitiatorKingdomId == "a", "oral commitment independent of occupied ordinary event");
        Test.True(arrangement.SourceSessionId == "session_1" && arrangement.SourcePlayerText == "PRIVATE_PLAYER" && h.Facts.Count > 0,
            "oral consent preserves channel identity and durable personal-memory receipt");
        var drafted = o.ResolveDocument(arrangement.DocumentId);
        Test.True(drafted == null || !drafted.Body.Contains("PRIVATE_"), "private negotiation provenance never leaks into public body");
        o.CancelUnpublishedCommitmentWork(arrangement.ArrangementId, arrangement.Version, "changed_mind");
        Test.True(arrangement.Status == "cancelled" && !WorldDiplomacyLiveRoundRules.Contains(o.CurrentStorage, oral), "cancel unpublished oral work closes only its own event");
        Test.True(WorldDiplomacyLiveRoundRules.Contains(o.CurrentStorage, a) && WorldDiplomacyLiveRoundRules.Contains(o.CurrentStorage, c), "oral cancellation leaves concurrent ordinary negotiations intact");

        FormalReceipt(h, o, c);
        var pendingCount = a.PlayerResponses.Count(x => x.Status == "pending");
        Invoke(o, "CarryUnansweredPlayerResponses", a);
        var followup = o.CurrentStorage.ConcurrentRounds.Single(x => x.EventSourceType == "player_followup");
        Test.True(followup.PlayerResponses.Count == pendingCount && followup.PlayerResponses.All(x => x.OriginalRoundId == a.RoundId), "closing transfers obligations with original source/event identity");
        Test.True(followup.PendingOffers.Count == 0 && followup.ExternalOpeningContext.Contains("不得恢复"), "followup does not revive old treaty proposals");
        var carried = followup.PlayerResponses.First();
        var carriedSource = o.ResolveDocument(carried.SourceDocumentId);
        Test.True(WorldDiplomacyNoActionApplication.IsAllowed(followup, null,
            new ActionSelectionReplay.NoActionPort(carried.KingdomId, carriedSource.AuthorKingdomId), true, false, carriedSource),
            "follow-up responder may substantively reply to the original player statement without an unrelated diplomatic action");
        for (int i = 0; i < 8; i++) WorldDiplomacyDocumentApplication.Add(o.CurrentStorage,
            new WorldDiplomacyDocument { DocumentId = "archive_noise_" + i, Day = 200 + i }, 3, () => { });
        Test.True(followup.PlayerResponses.All(x => o.ResolveDocument(x.SourceDocumentId) != null),
            "archive display limit never prunes pending player sources in follow-up events");
    }

    private static void FormalReceipt(Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound round)
    {
        var proposal = Doc("formal_offer", round, "a", "b"); proposal.Intent = "propose_vassal";
        proposal.TreatyTerms = new() { ReceivingKingdomId = "a", JoiningKingdomId = "b" };
        o.CurrentStorage.Documents.Add(proposal);
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "b", proposal.DocumentId, 12);
        var offer = new WorldDiplomacyRoundOffer { SourceDocumentId = proposal.DocumentId, ProposerKingdomId = "a", TargetKingdomId = "b", Intent = proposal.Intent };
        round.PendingOffers.Add(offer);
        var response = Doc("formal_accept", round, "b", "a"); response.Intent = "accept_vassal";
        response.RespondingToOfferDocumentId = proposal.DocumentId; response.Body = "b accepts a as suzerain";
        o.CurrentStorage.Documents.Add(response);
        Test.True(o.ValidateFormalTreatyDeclaration(response, response.Intent, null, "b", "a", proposal.DocumentId, "", out _), "acceptance uses exact original roles");
        h.Cycle = true;
        Test.True(o.ExecuteFormalTreatyOffer(proposal.Intent, offer, proposal, response) == WorldDiplomacyOfferOutcome.Invalidated && h.Effects == 0,
            "main-thread cycle guard rejects before any effect");
        h.Cycle = false; h.Partial = true;
        Test.True(o.ExecuteFormalTreatyOffer(proposal.Intent, offer, proposal, response) == WorldDiplomacyOfferOutcome.Partial && response.ChangedDiplomaticState,
            "partial effect remains a partial receipt, not complete treaty success");
        h.Partial = false;
        var settled = o.TrySettleRelayOffer(response);
        Test.True(settled == WorldDiplomacyOfferOutcome.Applied && offer.Status == "accepted" && h.Effects == 2,
            "real module settles source-bound acceptance through narrow game effect port");
        Test.True(o.TrySettleRelayOffer(response) == WorldDiplomacyOfferOutcome.Failed && h.Effects == 2, "duplicate acceptance cannot execute twice");
        response.AnsweredPlayerDocumentIds = new() { "p_ask" };
        o.StartDocumentPropagation(response, "b"); o.AppendCanonicalDocumentEvents(response);
        Test.True(response.PropagationCompleted && response.HistoryResultRecorded && o.CurrentStorage.CanonicalHistory.DeltaEntries.Any(x => x.SourceId == response.DocumentId),
            "effect receipt is published and enters the one canonical history");
        Test.True(o.CurrentStorage.CanonicalHistory.DeltaEntries.Any(x => x.AnsweredPlayerDocumentIds.Contains("p_ask")), "player response coverage preserved in canonical history");
        var trade = Doc("timed_trade", round, "a", "b"); trade.Intent = "propose_trade";
        trade.TreatyTerms = new() { DurationDays = 14 }; o.CurrentStorage.Documents.Add(trade);
        var tradeOffer = new WorldDiplomacyRoundOffer { SourceDocumentId = trade.DocumentId,
            ProposerKingdomId = "a", TargetKingdomId = "b", Intent = "propose_trade" };
        round.PendingOffers.Add(tradeOffer);
        var tradeAcceptance = Doc("timed_trade_accept", round, "b", "a");
        tradeAcceptance.Intent = "accept_trade"; tradeAcceptance.RespondingToOfferDocumentId = trade.DocumentId;
        Test.True(o.TrySettleRelayOffer(tradeAcceptance) == WorldDiplomacyOfferOutcome.Applied
            && h.TradeDuration == 14 && tradeAcceptance.TreatyTerms.DurationDays == 14,
            "accepted oral trade duration reaches the actual Application effect port without engine-default substitution");
        var acceptedAction = new WorldDiplomacyDocumentAction();
        WorldDiplomacyDocumentApplication.CaptureActionResult(tradeAcceptance, acceptedAction,
            new WorldDiplomacyDocumentActionReceipt("action", "a", true, true, true, "accepted"));
        Test.True(acceptedAction.TreatyTerms.DurationDays == 14
            && !ReferenceEquals(acceptedAction.TreatyTerms, tradeAcceptance.TreatyTerms),
            "multi-action receipt retains a detached copy of accepted source terms before primary-action mirroring");
        var publicationHost = new Host { PublishEnabled = true };
        var publicationOwner = new WorldDiplomacyOrchestration(publicationHost, new WorldDiplomacyRuntimeState());
        publicationHost.Owner = publicationOwner;
        publicationOwner.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=b;days=14",
            new DialogueInteractionOrigin("courier", "publish", "publish_session", "PRIVATE_PLAYER", "PRIVATE_NPC"));
        var publishedArrangement = publicationOwner.CurrentStorage.DialogueArrangements.Single();
        var publishedDocument = publicationOwner.ResolveDocument(publishedArrangement.DocumentId);
        Test.True(publishedArrangement.Status == "published" && publishedDocument.IsReadyForPublication,
            "actual oral submission publishes through the module document executor: " + publishedArrangement.Status + ": " + publishedArrangement.Reason);
        Test.True(publicationOwner.ResolveRound(publishedArrangement.RoundId).PendingOffers.Any(x => x.SourceDocumentId == publishedDocument.DocumentId)
            && publicationHost.Effects == 0,
            "published oral proposal creates a source-bound offer without prematurely executing a bilateral treaty: " + publishedDocument.MechanicalResult);
        Test.True(!publishedDocument.Body.Contains("PRIVATE_") && publishedDocument.PropagationCompleted
            && publicationOwner.CurrentStorage.PropagationArrivals.Any(x => x.DocumentId == publishedDocument.DocumentId)
            && !(bool)Invoke(publicationOwner, "DialogueDocumentKnown", "b", publishedDocument.DocumentId),
            "oral publication schedules delivery of agreed terms while private provenance stays private");
    }
}
