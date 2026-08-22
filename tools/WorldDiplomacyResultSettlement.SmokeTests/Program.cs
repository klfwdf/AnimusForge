using System.Text;
using AnimusForge;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    internal static void Equal<T>(T expected, T actual, string message)
        where T : notnull
    {
        _assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}; expected={expected}; actual={actual}");
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private static int Main()
    {
        ConfirmedResultBoundary();
        InitializeOnlyUnspokenSelectedKingdoms();
        SuccessfulWarResponseIsUniqueAndFirst();
        DirectedOpenActionsCreateTargetObligations();
        ConsumptionAndCloseBoundary();
        MalformedDuplicateSlotsAreMerged();
        BehaviorIntegrationContract();

        Console.WriteLine($"World diplomacy result-settlement smoke tests passed ({Test.Assertions} assertions).");
        return 0;
    }

    private static void ConfirmedResultBoundary()
    {
        foreach (string acceptIntent in new[] { "accept_peace", "accept_alliance", "accept_trade" })
        {
            Test.Equal(
                WorldDiplomacyConfirmedResultKind.OfferAccepted,
                Evaluate(acceptIntent, changed: true, hasOffer: true, offerStatus: "accepted"),
                acceptIntent + " must settle only when its exact offer is accepted");
            Test.Equal(
                WorldDiplomacyConfirmedResultKind.None,
                Evaluate(acceptIntent, changed: true, hasOffer: true, offerStatus: "open"),
                acceptIntent + " must not settle while its exact offer remains open");
        }
        foreach (string rejectIntent in new[] { "reject_peace", "reject_alliance", "reject_trade" })
        {
            Test.Equal(
                WorldDiplomacyConfirmedResultKind.OfferRejected,
                Evaluate(rejectIntent, hasOffer: true, offerStatus: "rejected"),
                rejectIntent + " must identify the exact rejected offer without treating it as a round result");
            Test.Equal(
                WorldDiplomacyConfirmedResultKind.None,
                Evaluate(rejectIntent, hasOffer: false, offerStatus: "rejected"),
                rejectIntent + " must not settle without its exact source offer");
        }
        foreach (string stateChangeIntent in new[] { "break_alliance", "cancel_trade", "declare_war" })
        {
            Test.Equal(
                WorldDiplomacyConfirmedResultKind.DiplomaticStateChanged,
                Evaluate(stateChangeIntent, changed: true),
                stateChangeIntent + " must settle after a confirmed mechanic state change");
            Test.Equal(
                WorldDiplomacyConfirmedResultKind.None,
                Evaluate(stateChangeIntent, changed: false),
                stateChangeIntent + " must not settle after a failed mechanic action");
        }
        foreach (string proposalIntent in new[] { "propose_peace", "propose_alliance", "propose_trade" })
        {
            Test.Equal(
                WorldDiplomacyConfirmedResultKind.None,
                Evaluate(proposalIntent),
                proposalIntent + " is an open proposal/counterproposal, not a settled result");
        }
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.None,
            Evaluate("propose_trade"),
            "an open proposal is not a confirmed result");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.OfferAccepted,
            Evaluate("accept_trade", changed: true, hasOffer: true, offerStatus: "accepted"),
            "an exact accepted offer is a result");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.OfferAccepted,
            Evaluate("accept_alliance", changed: true, hasOffer: true, offerStatus: "partially_executed"),
            "a partially executed exact acceptance is a result");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.None,
            Evaluate("accept_trade", changed: true, hasOffer: true, offerStatus: "execution_failed"),
            "a failed acceptance must not settle the round merely because a flag is stale");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.None,
            Evaluate("accept_trade", changed: true, hasOffer: false, offerStatus: "accepted"),
            "an internal acceptance without its exact source offer is not confirmed");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.OfferRejected,
            Evaluate("reject_peace", hasOffer: true, offerStatus: "rejected"),
            "an exact rejection must remain distinguishable so only that proposal closes");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.None,
            Evaluate("reject_peace", hasOffer: false, offerStatus: "rejected"),
            "a rejection without an exact open offer cannot settle the round");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.DiplomaticStateChanged,
            Evaluate("declare_war", changed: true),
            "successful war is a confirmed state result");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.None,
            Evaluate("declare_war", changed: false),
            "failed war execution is not a confirmed result");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.DiplomaticStateChanged,
            Evaluate("break_alliance", changed: true),
            "successful alliance break is a result");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.ThreatComplied,
            Evaluate("comply_ultimatum", threatStatus: "complied"),
            "linked threat compliance is a result");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.ThreatEnforced,
            Evaluate("declare_war", changed: true, threatStatus: "enforced"),
            "linked enforced ultimatum is resolved");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.ThreatBreached,
            Evaluate("warning", threatStatus: "breached"),
            "a breached threat is terminal but deadlocked");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.ThreatResolvedByWar,
            Evaluate("declare_war", changed: true, threatStatus: "breached"),
            "a warning skipped directly to successful war is still resolved");
        Test.True(
            !WorldDiplomacyResultSettlementRules.IsResolvedOutcome(WorldDiplomacyConfirmedResultKind.ThreatBreached),
            "threat breach must retain a deadlocked outcome");
		Test.True(
			!WorldDiplomacyResultSettlementRules.IsConfirmedResult(WorldDiplomacyConfirmedResultKind.OfferRejected)
			&& !WorldDiplomacyResultSettlementRules.IsResolvedOutcome(WorldDiplomacyConfirmedResultKind.OfferRejected),
			"rejection closes only its proposal and must not resolve the whole negotiation round");
        Test.Equal(
            WorldDiplomacyConfirmedResultKind.DiplomaticStateChanged,
            Evaluate("accept_peace", changed: true, externalFact: true),
            "an explicit external resolved fact may confirm an acceptance without an internal offer DTO");
		Test.Equal(
			WorldDiplomacyConfirmedResultKind.None,
			Evaluate("statement"),
			"a war-target no-action response must never manufacture a confirmed diplomatic result");
    }

    private static void InitializeOnlyUnspokenSelectedKingdoms()
    {
        List<WorldDiplomacyResultSettlementSlot> slots = new();
        string[] selected = { "aserai", "south_empire", "sturgia", "SOUTH_EMPIRE", " " };
        HashSet<string> spoken = new(StringComparer.Ordinal) { "ASERAI" };

        int opened = WorldDiplomacyResultSettlementRules.InitializeUnspokenSelectedSlots(selected, spoken, slots);

        Test.Equal(2, opened, "only two unique unspoken selected kingdoms should open slots");
        Test.Equal(2, slots.Count, "selected duplicates must not duplicate slots");
        Test.Equal("south_empire", slots[0].KingdomId, "route order must be preserved");
        Test.Equal("sturgia", slots[1].KingdomId, "route order must be preserved");
        Test.True(slots.All(x => x.Kind == WorldDiplomacyResultSettlementRules.RouteSlotKind),
            "initial selected slots must be route obligations");
    }

    private static void SuccessfulWarResponseIsUniqueAndFirst()
    {
        List<WorldDiplomacyResultSettlementSlot> slots = new();
        WorldDiplomacyResultSettlementRules.AddOrPromotePendingTarget(
            slots, "battania", "route", "", "", prioritize: false);
        WorldDiplomacyResultSettlementRules.AddOrPromotePendingTarget(
            slots, "sturgia", "route", "", "", prioritize: false);

        WorldDiplomacyResultSettlementSlot? warSlot = WorldDiplomacyResultSettlementRules.ApplySuccessfulWarTarget(
            slots, "sturgia", "war-doc-1", "khuzait");
        WorldDiplomacyResultSettlementRules.ApplySuccessfulWarTarget(
            slots, "STURGIA", "war-doc-1", "KHUZAIT");

        Test.True(warSlot != null, "successful war must create a response slot");
        Test.Equal("sturgia", slots[0].KingdomId, "war target must move to the front");
        Test.Equal(WorldDiplomacyResultSettlementRules.WarResponseSlotKind, slots[0].Kind,
            "route slot must upgrade to war response");
        Test.Equal(2, slots.Count, "repeated war registration must not duplicate the kingdom slot");
        Test.Equal(1, slots[0].SourceDocumentIds.Count, "source documents must deduplicate case-insensitively");
        Test.Equal(1, slots[0].RelatedKingdomIds.Count, "related aggressors must deduplicate case-insensitively");

        Test.True(WorldDiplomacyResultSettlementRules.ConsumeSpeakerSlot(slots, "sturgia", out _),
            "war response slot should be consumable");
        WorldDiplomacyResultSettlementRules.ApplySuccessfulWarTarget(
            slots, "sturgia", "war-doc-2", "vlandia");
        Test.Equal(WorldDiplomacyResultSettlementRules.PendingStatus, slots[0].Status,
            "a later successful war must reopen an already consumed target slot");
        Test.Equal(2, slots[0].SourceDocumentIds.Count, "reopened slot must retain both source documents");
    }

    private static void DirectedOpenActionsCreateTargetObligations()
    {
        List<WorldDiplomacyResultSettlementSlot> slots = new();

        Test.True(WorldDiplomacyResultSettlementRules.TryAddPendingActionTarget(
                slots, "propose_trade", "south_empire", "trade-doc", "aserai", false, out var offerSlot),
            "a proposal must create an offer-response target slot");
        Test.Equal(WorldDiplomacyResultSettlementRules.OfferResponseSlotKind, offerSlot.Kind,
            "proposal must use offer-response kind");
        Test.True(WorldDiplomacyResultSettlementRules.TryAddPendingActionTarget(
                slots, "warning", "SOUTH_EMPIRE", "warning-doc", "aserai", false, out var threatSlot),
            "a warning must create a threat-response target slot");
        Test.True(ReferenceEquals(offerSlot, threatSlot),
            "multiple obligations to one kingdom must share one persisted slot");
        Test.Equal(WorldDiplomacyResultSettlementRules.ThreatResponseSlotKind, threatSlot.Kind,
            "the stronger pending obligation must upgrade the slot kind");
        Test.Equal(2, threatSlot.SourceDocumentIds.Count, "merged slot must retain both action sources");

        Test.True(!WorldDiplomacyResultSettlementRules.TryAddPendingActionTarget(
                slots, "accept_trade", "aserai", "accept-doc", "south_empire", true, out _),
            "a closed acceptance must not manufacture another response loop");
        Test.True(!WorldDiplomacyResultSettlementRules.TryAddPendingActionTarget(
                slots, "declare_war", "sturgia", "failed-war", "khuzait", false, out _),
            "failed war execution must not grant a response turn");
        Test.True(WorldDiplomacyResultSettlementRules.TryAddPendingActionTarget(
                slots, "declare_war", "sturgia", "war-doc", "khuzait", true, out var warSlot),
            "successful war execution must grant a response turn");
        Test.True(ReferenceEquals(warSlot, slots[0]), "successful war response must be first");
    }

    private static void ConsumptionAndCloseBoundary()
    {
        List<WorldDiplomacyResultSettlementSlot> slots = new();
        WorldDiplomacyResultSettlementRules.AddOrPromotePendingTarget(
            slots, "aserai", "route", "", "", prioritize: false);
        WorldDiplomacyResultSettlementRules.AddOrPromotePendingTarget(
            slots, "battania", "route", "", "", prioritize: false);

        Test.True(!WorldDiplomacyResultSettlementRules.CanClose(slots, hasUnresolvedActions: false),
            "round cannot close while a selected speaker remains");
        Test.True(WorldDiplomacyResultSettlementRules.ConsumeSpeakerSlot(slots, "ASERAI", out var consumed),
            "speaker consumption must be case-insensitive");
        Test.Equal(WorldDiplomacyResultSettlementRules.ConsumedStatus, consumed.Status,
            "consumption must be persisted on the slot");
        Test.Equal("battania", WorldDiplomacyResultSettlementRules.GetNextPendingSlot(slots)!.KingdomId,
            "scheduler must move to the next pending selected kingdom");
        Test.True(WorldDiplomacyResultSettlementRules.ConsumeSpeakerSlot(slots, "battania", out _),
            "last selected speaker should be consumed");
        Test.True(!WorldDiplomacyResultSettlementRules.CanClose(slots, hasUnresolvedActions: true),
            "unrefreshed unresolved actions must block premature closure");
        Test.True(WorldDiplomacyResultSettlementRules.CanClose(slots, hasUnresolvedActions: false),
            "round closes only after all slots and unresolved actions are clear");

        WorldDiplomacyResultSettlementRules.AddOrPromotePendingTarget(
            slots, "aserai", "offer_response", "new-offer", "battania", prioritize: false);
        Test.True(!WorldDiplomacyResultSettlementRules.CanClose(slots, hasUnresolvedActions: true),
            "a new directed action must reopen its already-spoken target");
    }

    private static void MalformedDuplicateSlotsAreMerged()
    {
        List<WorldDiplomacyResultSettlementSlot> slots = new()
        {
            new()
            {
                SlotId = "old-a",
                KingdomId = "vlandia",
                Kind = "route",
                Status = "consumed",
                SourceDocumentIds = new() { "doc-a" }
            },
            new()
            {
                SlotId = "old-b",
                KingdomId = "VLANDIA",
                Kind = "threat_response",
                Status = "pending",
                SourceDocumentIds = new() { "doc-b" },
                RelatedKingdomIds = new() { "sturgia" }
            }
        };

        WorldDiplomacyResultSettlementSlot merged = WorldDiplomacyResultSettlementRules.AddOrPromotePendingTarget(
            slots, "vlandia", "offer_response", "doc-c", "aserai", prioritize: false)!;

        Test.Equal(1, slots.Count, "malformed duplicate persisted slots must collapse to one kingdom slot");
        Test.Equal(WorldDiplomacyResultSettlementRules.PendingStatus, merged.Status,
            "a pending duplicate must keep the merged slot pending");
        Test.Equal(WorldDiplomacyResultSettlementRules.ThreatResponseSlotKind, merged.Kind,
            "merge must keep the strongest existing obligation kind");
        Test.Equal(3, merged.SourceDocumentIds.Count, "duplicate repair must preserve all source document ids");
        Test.Equal(2, merged.RelatedKingdomIds.Count, "duplicate repair must preserve and extend related kingdoms");
    }

    private static void BehaviorIntegrationContract()
    {
        string source = File.ReadAllText(FindRepositoryFile("WorldDiplomacyBehavior.cs"), Encoding.UTF8);

        Test.True(source.Contains("private const int ResultSettlementStateSchemaVersion = 1;", StringComparison.Ordinal)
                  && source.Contains("_storage.ResultSettlementStateSchemaVersion = ResultSettlementStateSchemaVersion;", StringComparison.Ordinal)
                  && source.Contains("[JsonProperty(\"resultSettlementStateSchemaVersion\")]", StringComparison.Ordinal),
            "result settlement persistence must have an explicit v1 schema initialized for new games");
        Test.True(source.Contains("[JsonProperty(\"resultSettlementPending\")]", StringComparison.Ordinal)
                  && source.Contains("[JsonProperty(\"resultSettlementSlots\")]", StringComparison.Ordinal)
                  && source.Contains("[JsonProperty(\"resultSettlementSlotId\")]", StringComparison.Ordinal),
            "round state, queue slots, jobs, documents, and relay arrivals must persist settlement ownership");

        string externalFactPublication = ExtractSection(
            source,
            "private void NotifyExternalDiplomacyResolvedInternal(",
            "private bool CanExternalDiplomacyFactJoinRound(");
        Test.True(externalFactPublication.Contains("fact.AnalysisStatus = \"external_fact\"", StringComparison.Ordinal)
                  && externalFactPublication.Contains("WorldDiplomacyRound activeRound = _storage.ActiveRound", StringComparison.Ordinal)
                  && externalFactPublication.Contains("CanExternalDiplomacyFactJoinRound(activeRound, initiator, target)", StringComparison.Ordinal)
                  && externalFactPublication.Contains("fact.RoundId = round?.RoundId ?? \"\"", StringComparison.Ordinal),
            "an external fact must join an existing active round only through the explicit relevance gate");
        Test.True(externalFactPublication.Contains(
                      "bool appendedExternalSettlementTarget = round?.ResultSettlementPending == true",
                      StringComparison.Ordinal)
                  && externalFactPublication.Contains(
                      "!TryIncludeResultSettlementTarget(round, target.StringId)",
                      StringComparison.Ordinal)
                  && externalFactPublication.Contains(
                      "AddOrMergeResultSettlementSlot(round, target.StringId, \"route\"",
                      StringComparison.Ordinal),
            "a relevant external fact may append its new settlement target and must create a drain slot for it");
        int externalPropagation = externalFactPublication.IndexOf("StartDocumentPropagation(fact, initiator)", StringComparison.Ordinal);
        int unrelatedRoundReturn = externalFactPublication.IndexOf("if (round == null)", externalPropagation, StringComparison.Ordinal);
        int externalRoundProgress = externalFactPublication.IndexOf("HandleRoundDocumentProcessed(fact)", StringComparison.Ordinal);
        Test.True(externalPropagation >= 0 && unrelatedRoundReturn > externalPropagation
                  && externalRoundProgress > unrelatedRoundReturn
                  && externalFactPublication.Contains("fact.RoundProgressHandled = true", StringComparison.Ordinal),
            "an unrelated external fact may propagate globally but must return before mutating active-round progress");

        string externalJoinGate = ExtractSection(
            source,
            "private bool CanExternalDiplomacyFactJoinRound(",
            "private static bool Patch_Kingdom_AddDecision_Prefix(");
        Test.True(externalJoinGate.Contains("round.State, \"active\"", StringComparison.Ordinal)
                  && externalJoinGate.Contains("route.Contains(initiator.StringId", StringComparison.Ordinal)
                  && externalJoinGate.Contains("route.Contains(target.StringId", StringComparison.Ordinal)
                  && externalJoinGate.Contains("if (bothSelected) return true", StringComparison.Ordinal),
            "an ordinary external fact may join when both parties are already selected");
        Test.True(externalJoinGate.Contains("if (round.ResultSettlementPending", StringComparison.Ordinal)
                  && externalJoinGate.Contains(
                      "route.Contains(initiator.StringId, StringComparer.OrdinalIgnoreCase)",
                      StringComparison.Ordinal)
                  && externalJoinGate.Contains(
                      "CanUseResultSettlementTarget(round, initiator, target)",
                      StringComparison.Ordinal),
            "during settlement, only an already-selected initiator may bring in a still-usable new target");
        Test.True(externalJoinGate.Contains("round.PendingOffers", StringComparison.Ordinal)
                  && externalJoinGate.Contains("x.Status, \"open\"", StringComparison.Ordinal)
                  && externalJoinGate.Contains("x.ProposerKingdomId, initiator.StringId", StringComparison.Ordinal)
                  && externalJoinGate.Contains("x.TargetKingdomId, target.StringId", StringComparison.Ordinal)
                  && externalJoinGate.Contains("x.ProposerKingdomId, target.StringId", StringComparison.Ordinal)
                  && externalJoinGate.Contains("x.TargetKingdomId, initiator.StringId", StringComparison.Ordinal),
            "outside settlement expansion, off-route external parties may join only through their matching still-open bilateral offer");

        string propagation = ExtractSection(
            source,
            "private void StartDocumentPropagation(",
            "private void RetryDeferredDocumentPropagation(");
        Test.True(propagation.Contains("WorldDiplomacyRound round = ResolveRound(document.RoundId)", StringComparison.Ordinal)
                  && propagation.Contains("document.AnalysisStatus, \"external_fact\"", StringComparison.Ordinal)
                  && propagation.Contains("round = EnsureActiveRound(", StringComparison.Ordinal),
            "propagation must distinguish a roundless external fact from an ordinary round-opening document");
        int noRoundGuard = propagation.IndexOf("if (round == null", StringComparison.Ordinal);
        int externalFactException = propagation.IndexOf("document.AnalysisStatus, \"external_fact\"", noRoundGuard, StringComparison.Ordinal);
        int ensureRound = propagation.IndexOf("round = EnsureActiveRound(", externalFactException, StringComparison.Ordinal);
        Test.True(noRoundGuard >= 0 && externalFactException > noRoundGuard && ensureRound > externalFactException,
            "a roundless external fact must bypass EnsureActiveRound instead of contaminating an unrelated round");

        string migration = ExtractSection(
            source,
            "private void MigrateResultSettlementStateIfNeeded()",
            "private void NormalizeStorage(");
        Test.True(migration.Contains(
                "_storage.ResultSettlementStateSchemaVersion >= ResultSettlementStateSchemaVersion",
                StringComparison.Ordinal)
                  && migration.Contains("_storage.ActiveRound", StringComparison.Ordinal)
                  && migration.Contains("round.State, \"active\"", StringComparison.Ordinal)
                  && migration.Contains("!round.ResultSettlementPending", StringComparison.Ordinal)
                  && migration.Contains("x.IsReadyForPublication", StringComparison.Ordinal)
                  && migration.Contains("x.RoundId, round.RoundId", StringComparison.Ordinal)
                  && migration.Contains("TryGetConfirmedRoundResult(document, round", StringComparison.Ordinal)
                  && migration.Contains("BeginOrExtendRoundResultSettlement(round, document", StringComparison.Ordinal)
                  && migration.Contains(
                      "_storage.ResultSettlementStateSchemaVersion = ResultSettlementStateSchemaVersion",
                      StringComparison.Ordinal),
            "a legacy active round must replay only its published documents once to recover settlement state");
        Test.True(migration.Contains("RemoveAnsweredMigratedWarResponseSlots(round, published);", StringComparison.Ordinal),
            "v0 migration must remove war-response duties already answered later in the same round");
        string answeredWarMigration = ExtractSection(
            source,
            "private static void RemoveAnsweredMigratedWarResponseSlots(",
            "private void NormalizeStorage(");
		Test.True(answeredWarMigration.Contains("SettlementSlotHasKind(slot, \"war_response\")", StringComparison.Ordinal)
				  && answeredWarMigration.Contains("DocumentHasSuccessfulWarAgainst(x, slot.KingdomId)", StringComparison.Ordinal),
			"migration cleanup must identify only successful wars targeting the response-slot kingdom");
		string successfulWarHelper = ExtractMethod(
			source,
			"private static bool DocumentHasSuccessfulWarAgainst(");
		int multiActionBranch = successfulWarHelper.IndexOf("document.Actions?.Count > 0", StringComparison.Ordinal);
		int legacyFallback = successfulWarHelper.IndexOf("return document.ChangedDiplomaticState", StringComparison.Ordinal);
		Test.True(multiActionBranch >= 0
			&& successfulWarHelper.Contains("document.Actions.Any", StringComparison.Ordinal)
			&& successfulWarHelper.Contains("x.ChangedDiplomaticState", StringComparison.Ordinal)
			&& successfulWarHelper.Contains("NormalizeIntent(x.Intent) == \"declare_war\"", StringComparison.Ordinal)
			&& successfulWarHelper.Contains("x.TargetKingdomId, targetKingdomId", StringComparison.Ordinal)
			&& legacyFallback > multiActionBranch,
			"migration war audit must prefer exact action facts and consult the legacy primary mirror only when actions are absent or empty");
        Test.True(answeredWarMigration.Contains("response.AuthorKingdomId, slot.KingdomId", StringComparison.Ordinal)
                  && answeredWarMigration.Contains("response.Day > war.Day", StringComparison.Ordinal)
                  && answeredWarMigration.Contains("response.CreatedUtcTicks > war.CreatedUtcTicks", StringComparison.Ordinal),
            "a migrated war response is obsolete only after the target kingdom actually published after that war document");
        Test.True(answeredWarMigration.Contains("round.ResultSettlementSlots.Remove(slot)", StringComparison.Ordinal)
                  && answeredWarMigration.Contains("!string.Equals(x, \"war_response\"", StringComparison.Ordinal)
                  && answeredWarMigration.Contains("slot.Kind = string.Join(\"+\", remainingKinds)", StringComparison.Ordinal),
            "migration must remove a pure obsolete war slot or strip only its war-response kind from a mixed obligation");
        string normalization = ExtractSection(
            source,
            "private void NormalizeStorage(",
            "private void TrimRecentBattleFacts(");
        Test.True(normalization.Contains("if (allowWorldValidation)", StringComparison.Ordinal)
                  && normalization.Contains("MigrateResultSettlementStateIfNeeded();", StringComparison.Ordinal),
            "result-settlement migration must run during validated save loading, not on hot-path ticks");
        string slotNormalization = ExtractSection(
            source,
            "List<WorldDiplomacyResultSettlementSlot> normalizedSettlementSlots",
            "round.LlmTranscript ??=");
        foreach (string activeStatus in new[] { "pending", "inflight", "scheduled", "waiting_player" })
        {
            Test.True(slotNormalization.Contains("x.Status, \"" + activeStatus + "\"", StringComparison.Ordinal),
                "slot normalization must retain active status: " + activeStatus);
        }
        Test.True(slotNormalization.Contains("if (pending.Count == 0) continue;", StringComparison.Ordinal)
                  && !slotNormalization.Contains("x.Status, \"consumed\"", StringComparison.Ordinal)
                  && !slotNormalization.Contains("x.Status, \"skipped\"", StringComparison.Ordinal),
            "terminal duplicate slots must be discarded instead of resurrected during normalization");
        Test.True(slotNormalization.Contains("originalCurrentSettlementSlotId", StringComparison.Ordinal)
                  && slotNormalization.Contains("pending.FirstOrDefault(x =>", StringComparison.Ordinal)
                  && slotNormalization.Contains("normalizedCurrentSettlementSlotId = slot.SlotId", StringComparison.Ordinal)
                  && slotNormalization.Contains("round.ResultSettlementCurrentSlotId = normalizedCurrentSettlementSlotId", StringComparison.Ordinal),
            "duplicate-slot normalization must select and remap the persisted current active slot");
        Test.True(!slotNormalization.Contains("slot.Status = \"pending\";", StringComparison.Ordinal),
            "normalization must not erase scheduled/inflight/waiting-player state by resetting every merged slot to pending");
        Test.True(slotNormalization.Contains("pending.SelectMany(x => x.SourceDocumentIds", StringComparison.Ordinal)
                  && slotNormalization.Contains("pending.SelectMany(x => x.RelatedKingdomIds", StringComparison.Ordinal)
                  && slotNormalization.Contains("pending.SelectMany(x => (x.Kind ?? \"\").Split('+')", StringComparison.Ordinal),
            "all active duplicates must merge their kinds, source documents, and related kingdoms");
        Test.True(slotNormalization.Contains(
                "if (string.IsNullOrWhiteSpace(normalizedCurrentSettlementSlotId)) round.ResultSettlementPlayerWaitingSinceDay = 0",
                StringComparison.Ordinal),
            "player wait timing must survive when its current slot is successfully remapped");

        string confirmedResult = ExtractSection(
            source,
            "private bool TryGetConfirmedRoundResult(",
            "private static bool SettlementSlotHasKind(");
        foreach (string required in new[]
        {
            "ResponseIntentToProposalIntent(intent)",
            "document.RespondingToOfferDocumentId",
            "WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(",
            "matchedOffer != null",
            "matchedOffer?.Status",
            "WorldDiplomacyResultSettlementRules.IsConfirmedResult(resultKind)",
            "WorldDiplomacyConfirmedResultKind.OfferAccepted",
            "WorldDiplomacyConfirmedResultKind.OfferRejected",
            "\"declare_war\" => \"war_declared\"",
            "\"break_alliance\" => \"alliance_broken\"",
            "\"cancel_trade\" => \"trade_cancelled\""
        })
        {
            Test.True(confirmedResult.Contains(required, StringComparison.Ordinal),
                "production confirmed-result gate is missing: " + required);
        }

        string roundProgress = ExtractSection(
            source,
            "private void HandleRoundDocumentProcessed(",
            "private void RetryDeferredRoundProgress(");
        int confirmedGate = roundProgress.IndexOf("TryGetConfirmedRoundResult(document, round", StringComparison.Ordinal);
        int beginSettlement = roundProgress.IndexOf("BeginOrExtendRoundResultSettlement(round, document", StringComparison.Ordinal);
        int rootBranch = roundProgress.IndexOf("if (isRootDocument)", StringComparison.Ordinal);
        Test.True(confirmedGate >= 0 && beginSettlement > confirmedGate && rootBranch > beginSettlement,
            "every published confirmed result must enter or extend settlement before root/relay branching");
        Test.True(roundProgress.IndexOf("ConsumeResultSettlementSpeaker(round, document)", StringComparison.Ordinal) < confirmedGate,
            "a valid published settlement document must consume its current slot before opening follow-up obligations");

        string routeInitialization = ExtractSection(
            source,
            "private void InitializeResultSettlementRouteSlots(",
            "private bool IsThreatRelevantToResultSettlement(");
        Test.True(routeInitialization.Contains("x.IsReadyForPublication", StringComparison.Ordinal)
                  && routeInitialization.Contains("x.RoundId, round.RoundId", StringComparison.Ordinal)
                  && routeInitialization.Contains(".Select(x => x.AuthorKingdomId)", StringComparison.Ordinal)
                  && routeInitialization.Contains("if (!spoken.Contains(kingdomId))", StringComparison.Ordinal)
                  && routeInitialization.Contains("round.RelayRouteKingdomIds", StringComparison.Ordinal)
                  && routeInitialization.Contains("ResultSettlementRouteInitialized = true", StringComparison.Ordinal),
            "route settlement slots must be initialized once from selected kingdoms minus actual published authors");

        string targetGate = ExtractSection(
            source,
            "private bool CanUseResultSettlementTarget(",
            "private bool TryIncludeResultSettlementTarget(");
        Test.True(targetGate.Contains("round?.ResultSettlementPending != true", StringComparison.Ordinal)
                  && targetGate.Contains("target.IsEliminated", StringComparison.Ordinal)
                  && targetGate.Contains("!HasIndependentWorldDiplomacyAuthority(target)", StringComparison.Ordinal)
                  && targetGate.Contains("RoundRouteContainsKingdom(round, target.StringId)", StringComparison.Ordinal)
                  && targetGate.Contains("< MaxRelayParticipants", StringComparison.Ordinal),
            "settlement target expansion must accept existing participants and cap only valid new independent kingdoms");
        string targetInclusion = ExtractSection(
            source,
            "private bool TryIncludeResultSettlementTarget(",
            "private void AddOrMergeResultSettlementSlot(");
        Test.True(targetInclusion.Contains("round.RelayRouteKingdomIds.Count >= MaxRelayParticipants", StringComparison.Ordinal)
                  && targetInclusion.Contains("round.RelayRouteKingdomIds.Add(kingdomId)", StringComparison.Ordinal)
                  && targetInclusion.Contains(
                      "round.HardEndDay = Math.Max(round.HardEndDay, CurrentDay() + 3)",
                      StringComparison.Ordinal)
                  && targetInclusion.Contains("EnsureRoundParticipant(round, kingdomId", StringComparison.Ordinal)
                  && targetInclusion.Contains("participant.SelectedForRelay = true", StringComparison.Ordinal)
                  && targetInclusion.Contains("AddOrMergeResultSettlementSlot(round, kingdomId, \"route\"", StringComparison.Ordinal),
            "a new settlement target must monotonically join route, participants, and slots while extending the bounded hard end");

        string slotMutation = ExtractSection(
            source,
            "private void AddOrMergeResultSettlementSlot(",
            "private void InitializeResultSettlementRouteSlots(");
        Test.True(slotMutation.Contains("!TryIncludeResultSettlementTarget(round, kingdomId)", StringComparison.Ordinal)
                  && slotMutation.Contains("FirstOrDefault(x => x != null", StringComparison.Ordinal)
                  && slotMutation.Contains("x.KingdomId, kingdomId", StringComparison.Ordinal)
                  && slotMutation.Contains("if (prioritize)", StringComparison.Ordinal)
                  && slotMutation.Contains("Insert(0, slot)", StringComparison.Ordinal),
            "settlement obligations must deduplicate by kingdom and allow urgent obligations to be promoted");

        string actionRefresh = ExtractSection(
            source,
            "private void RefreshResultSettlementActionSlots(",
            "private void AddWarResponseResultSettlementSlot(");
        Test.True(actionRefresh.Contains("x.Status, \"open\"", StringComparison.Ordinal)
                  && actionRefresh.Contains("!TryIncludeResultSettlementTarget(round, target.StringId)", StringComparison.Ordinal)
                  && actionRefresh.Contains("\"offer_response\"", StringComparison.Ordinal)
                  && actionRefresh.Contains("\"threat_response\"", StringComparison.Ordinal)
                  && actionRefresh.Contains("\"threat_followthrough\"", StringComparison.Ordinal)
                  && actionRefresh.Contains("prioritize: true", StringComparison.Ordinal),
            "open offers and current threat duties may include new valid targets before creating or promoting settlement obligations");

        string actionableTargets = ExtractSection(
            source,
            "private List<Kingdom> GetResultSettlementActionableTargets(",
            "private void SkipResultSettlementSlot(");
        Test.True(actionableTargets.Contains("return Kingdom.All", StringComparison.Ordinal)
                  && actionableTargets.Contains("CanUseResultSettlementTarget(round, author, x)", StringComparison.Ordinal)
				  && actionableTargets.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
				  && actionableTargets.Contains("isRelayTurn: true", StringComparison.Ordinal)
				  && actionableTargets.Contains("round.ResultSettlementCurrentSlotId", StringComparison.Ordinal)
                  && actionableTargets.Contains("OrderBy(x => x.StringId", StringComparison.Ordinal),
            "settlement generation must consider all currently executable independent targets, not only the original relay route");

        string warResponse = ExtractSection(
            source,
            "private void AddWarResponseResultSettlementSlot(",
            "private void BeginOrExtendRoundResultSettlement(");
		Test.True(warResponse.Contains("document.Actions", StringComparison.Ordinal)
				  && warResponse.Contains("x.ChangedDiplomaticState", StringComparison.Ordinal)
				  && warResponse.Contains("NormalizeIntent(x.Intent)", StringComparison.Ordinal)
				  && warResponse.Contains("document.DocumentId", StringComparison.Ordinal)
				  && warResponse.Contains("action.ActionId", StringComparison.Ordinal)
				  && warResponse.Contains("#", StringComparison.Ordinal)
                  && warResponse.Contains("\"war_response\"", StringComparison.Ordinal)
                  && warResponse.Contains("prioritize: true", StringComparison.Ordinal),
			"each successful, previously unregistered war action must grant and prioritize its own target response using docId#actionId");

        string begin = ExtractSection(
            source,
            "private void BeginOrExtendRoundResultSettlement(",
            "private void ConsumeResultSettlementSpeaker(");
        Test.True(begin.Contains("round.ResultSettlementPending = true", StringComparison.Ordinal)
                  && begin.Contains("InitializeResultSettlementRouteSlots(round)", StringComparison.Ordinal)
                  && begin.Contains("AddWarResponseResultSettlementSlot(round, document)", StringComparison.Ordinal)
                  && begin.Contains("RefreshResultSettlementActionSlots(round)", StringComparison.Ordinal),
            "confirmed results must freeze unspoken route obligations and then add directed/war obligations");
        Test.True(begin.Contains("int settlementWindowDays = Math.Max(14", StringComparison.Ordinal)
                  && begin.Contains("((round.RelayRouteKingdomIds?.Count ?? 0) + 2) * 2", StringComparison.Ordinal)
                  && begin.Contains(
                      "round.HardEndDay = Math.Max(round.HardEndDay, CurrentDay() + settlementWindowDays)",
                      StringComparison.Ordinal),
            "opening settlement must extend the old relay deadline by a route-bounded window");

        string scheduler = ExtractSection(
            source,
            "private void ScheduleNextResultSettlementTurn(",
            "private void HandleRoundDocumentProcessed(");
        Test.True(!scheduler.Contains("FindNextRelayIndex", StringComparison.Ordinal)
                  && scheduler.Contains("round.ResultSettlementSlots", StringComparison.Ordinal)
                  && scheduler.Contains("FirstOrDefault(x => x != null)", StringComparison.Ordinal),
            "settlement scheduling must drain its persisted slot queue instead of re-entering ordinary relay rotation");
        Test.True(scheduler.Contains("GetResultSettlementActionableTargets(round, receiver).Count == 0", StringComparison.Ordinal)
                  && scheduler.Contains("SkipResultSettlementSlot(round, slot.SlotId, slot.KingdomId, \"no_legal_action\")", StringComparison.Ordinal),
            "a settlement speaker with no legal action must be skipped without an LLM request");
        int emptySlots = scheduler.IndexOf("if (slot == null)", StringComparison.Ordinal);
        int ordinaryClose = scheduler.IndexOf("CloseActiveRound(", StringComparison.Ordinal);
        Test.True(emptySlots >= 0 && ordinaryClose > emptySlots,
            "ordinary result settlement may close only after the refreshed slot queue is empty");

        string relayScheduler = ExtractSection(
            source,
            "private void ScheduleNextRelayHop(",
            "private static bool HasOpenRoundOffers(");
        Test.True(relayScheduler.Contains("if (round?.ResultSettlementPending == true)", StringComparison.Ordinal)
                  && relayScheduler.IndexOf("ScheduleNextResultSettlementTurn(round)", StringComparison.Ordinal)
                     < relayScheduler.IndexOf("FindNextRelayIndex", StringComparison.Ordinal),
            "ordinary relay scheduling must immediately delegate an active result-settlement round");

        string arrivals = ExtractSection(
            source,
            "private void ProcessRelayArrivals(",
            "private void AdvanceRelay(");
        Test.True(arrivals.Contains("if (round.ResultSettlementPending)", StringComparison.Ordinal)
                  && arrivals.Contains("arrival.ResultSettlementSlotId", StringComparison.Ordinal)
                  && arrivals.Contains("resultSettlementSlotId: settlementSlot.SlotId", StringComparison.Ordinal),
            "settlement relay arrival must claim the matching slot and pass its id into generation");

        string enqueue = ExtractSection(
            source,
            "private void EnqueueGenerationJob(",
            "private bool EnsureGenerationJobHasKingdomStrategicProfile(");
        Test.True(enqueue.Contains("string resultSettlementSlotId = null", StringComparison.Ordinal)
                  && enqueue.Contains("ResultSettlementSlotId = resultSettlementSlotId", StringComparison.Ordinal),
            "generation jobs must persist their settlement slot id");
        Test.True(enqueue.Contains(
                "bool isResultSettlementTurn = owningRound?.ResultSettlementPending == true",
                StringComparison.Ordinal)
                  && enqueue.Contains("&& !string.IsNullOrWhiteSpace(resultSettlementSlotId)", StringComparison.Ordinal)
                  && enqueue.Contains("if (!playerPriorityResponse && !isResultSettlementTurn", StringComparison.Ordinal)
                  && enqueue.Contains("AutomaticDocumentsStarted >= MaxAutomaticDocumentsPerRound", StringComparison.Ordinal),
            "a slot-owned settlement turn must bypass the obsolete 12-document relay cap without weakening ordinary rounds");
        Test.True(enqueue.Contains("SkipResultSettlementSlot(owningRound, resultSettlementSlotId", StringComparison.Ordinal)
                  && enqueue.Contains("ScheduleNextResultSettlementTurn(owningRound)", StringComparison.Ordinal),
            "generation preflight failure or no-action must skip the owned slot and continue draining");

        string generatedCommit = ExtractSection(
            source,
            "private void CommitGeneratedDocument(",
            "private bool TryGetGeneratedIntentLegalityViolation(");
        Test.True(generatedCommit.Contains("document.ResultSettlementSlotId = job.ResultSettlementSlotId", StringComparison.Ordinal),
            "a validated generated document must retain the slot id until publication consumes it");

		string analyzedPublication = ExtractMethod(
			source,
			"private void ProcessAnalyzedMultiActionDocument(");
		int finalStateGuard = analyzedPublication.IndexOf("TryGetDiplomaticStateViolation(", StringComparison.Ordinal);
		int aggregateCapacity = analyzedPublication.IndexOf(
			"newSettlementTargets.Count > MaxRelayParticipants",
			StringComparison.Ordinal);
		int includeTarget = analyzedPublication.IndexOf("TryIncludeResultSettlementTarget(", aggregateCapacity, StringComparison.Ordinal);
		int addTargetSlot = analyzedPublication.IndexOf("AddOrMergeResultSettlementSlot(", includeTarget, StringComparison.Ordinal);
        int markPublishable = analyzedPublication.IndexOf("document.IsReadyForPublication = true", StringComparison.Ordinal);
		Test.True(finalStateGuard >= 0 && aggregateCapacity > finalStateGuard
				  && includeTarget > aggregateCapacity && addTargetSlot > includeTarget
                  && markPublishable > addTargetSlot,
			"all new action targets must pass live-state and aggregate capacity checks before any route mutation or publication");

        string repair = ExtractSection(
            source,
            "private bool EnqueueGeneratedDeclarationRepair(",
            "private List<string> GetAuthorizedGenerationTargetIds(");
        Test.True(repair.Contains("ResultSettlementSlotId = source.ResultSettlementSlotId", StringComparison.Ordinal),
            "semantic/format repair must retain the original settlement slot id");
        string abandon = ExtractSection(
            source,
            "private void AbandonRejectedGeneration(",
            "private void SuppressInvalidDocumentBeforePropagation(");
        Test.True(abandon.Contains("SkipResultSettlementSlot(round, job.ResultSettlementSlotId", StringComparison.Ordinal)
                  && abandon.Contains("ScheduleNextResultSettlementTurn(round)", StringComparison.Ordinal),
            "a final repair failure must skip rather than consume or strand its settlement slot");

        string offerResponseAppender = ExtractSection(
            source,
            "private static void AppendOpenOfferResponseIntents(",
            "private List<string> BuildLegalDiplomaticActionIntents(");
        Test.True(offerResponseAppender.Contains("ProposalIntentToResponseIntent(proposalIntent, accepted: true)", StringComparison.Ordinal)
                  && offerResponseAppender.Contains("ProposalIntentToResponseIntent(proposalIntent, accepted: false)", StringComparison.Ordinal),
            "an open offer slot must derive only its accept/reject response pair");
        string legalActions = ExtractSection(
            source,
            "private List<string> BuildLegalDiplomaticActionIntents(",
            "private List<Kingdom> GetActionableDiplomaticTargets(");
        Test.True(legalActions.Contains("bool hasAnswerableOfferForTarget = currentSlot != null", StringComparison.Ordinal)
                  && legalActions.Contains("x.ProposerKingdomId, target.StringId", StringComparison.Ordinal)
                  && legalActions.Contains("actions.Clear();", StringComparison.Ordinal)
                  && legalActions.Contains("AppendOpenOfferResponseIntents(round, author, target, actions);", StringComparison.Ordinal)
                  && legalActions.IndexOf("return actions.Distinct", StringComparison.Ordinal)
                     > legalActions.IndexOf("actions.Clear();", StringComparison.Ordinal),
            "only the exact proposer-target pair's open offer may hide actions other than accept/reject");

        string potentialActions = ExtractSection(
            source,
            "private List<string> BuildPotentialDiplomaticActionIntents(",
            "private static string DescribePotentialDiplomaticActions(");
        int alreadyAtWar = potentialActions.IndexOf("if (atWar)", StringComparison.Ordinal);
        int peaceOnlyReturn = potentialActions.IndexOf("return actions;", alreadyAtWar, StringComparison.Ordinal);
        int warDeclaration = potentialActions.IndexOf("actions.Add(\"declare_war\")", StringComparison.Ordinal);
        Test.True(alreadyAtWar >= 0 && peaceOnlyReturn > alreadyAtWar && warDeclaration > peaceOnlyReturn,
            "a war target may answer once but cannot re-declare war on the existing aggressor");

        string lifecycle = ExtractSection(
            source,
            "private void ProcessRoundLifecycle()",
            "private void TripAutomaticRoundCircuitBreaker(");
        Test.True(lifecycle.Contains("currentSlot.Status, \"waiting_player\"", StringComparison.Ordinal)
                  && lifecycle.Contains("round.ResultSettlementPlayerWaitingSinceDay + 5", StringComparison.Ordinal)
                  && lifecycle.Contains("\"player_timeout\"", StringComparison.Ordinal),
            "a player-owned settlement slot must expire after its bounded waiting window");
        Test.True(lifecycle.Contains("if (day >= round.HardEndDay)", StringComparison.Ordinal)
                  && lifecycle.Contains("if (round.ResultSettlementPending)", StringComparison.Ordinal)
                  && lifecycle.Contains("round.ResultSettlementSlots?.Clear();", StringComparison.Ordinal)
                  && lifecycle.Contains("CloseActiveRound(\"result_settlement_hard_end\")", StringComparison.Ordinal),
            "the cap bypass remains bounded by the extended hard-end deadline");

		MultiActionSettlementContract(source);
		PendingOfferSourceActionNormalizationContract(source);
		ImmediateWarResponsePeaceSuppressionContract(source);
		PeaceOfferResponseExclusivityContract(source);
		MandatoryPeaceOfferCoverageContract(source);
		PeaceOfferTermsExecutabilityContract(source);
		MultiplePeaceAcceptanceCessionContract(source);
		WarResponseNoActionSettlementContract(source);
    }

	private static void MultiActionSettlementContract(string source)
	{
		string offerDto = ExtractSection(
			source,
			"public sealed class WorldDiplomacyRoundOffer",
			"public sealed class WorldDiplomacyOfferCooldown");
		Test.True(offerDto.Contains("[JsonProperty(\"sourceActionId\")]", StringComparison.Ordinal)
			&& offerDto.Contains("public string SourceActionId", StringComparison.Ordinal),
			"offers sharing one source document must retain exact per-action ownership");

		string offerSettlement = ExtractMethod(source, "private void TrySettleRelayOffer(");
		Test.True(offerSettlement.Contains("SourceActionId", StringComparison.Ordinal)
			&& offerSettlement.Contains("ProcessingActionId", StringComparison.Ordinal)
			&& offerSettlement.Contains("RespondingToOfferActionId", StringComparison.Ordinal)
			&& offerSettlement.Contains("ResolveOfferedPeaceTerms(source, resolvedOffer.SourceActionId)", StringComparison.Ordinal),
			"proposal registration and acceptance/rejection must match one document/action source pair");
		Test.True(offerSettlement.Contains("RemoveAll", StringComparison.Ordinal)
			&& offerSettlement.Contains("SourceDocumentId", StringComparison.Ordinal)
			&& offerSettlement.Contains("SourceActionId", StringComparison.Ordinal),
			"registering a second proposal in one document must preserve the first proposal action");

		string confirmedResult = ExtractMethod(source, "private bool TryGetConfirmedRoundResult(");
		Test.True(confirmedResult.Contains("document.Actions", StringComparison.Ordinal)
			&& confirmedResult.Contains("action.ChangedDiplomaticState", StringComparison.Ordinal)
			&& confirmedResult.Contains("action.RespondingToOfferDocumentId", StringComparison.Ordinal)
			&& confirmedResult.Contains("action.RespondingToOfferActionId", StringComparison.Ordinal),
			"a successful sibling action must never turn a failed action into a confirmed result");

		string analyzedPublication = ExtractMethod(
			source,
			"private void ProcessAnalyzedMultiActionDocument(");
		int settlementCapacityPreflight = analyzedPublication.IndexOf(
			"newSettlementTargets.Count > MaxRelayParticipants",
			StringComparison.Ordinal);
		int firstSettlementTargetMutation = analyzedPublication.IndexOf(
			"TryIncludeResultSettlementTarget(",
			StringComparison.Ordinal);
		Test.True(settlementCapacityPreflight >= 0
			&& analyzedPublication.Contains("MaxRelayParticipants", StringComparison.Ordinal)
			&& firstSettlementTargetMutation > settlementCapacityPreflight,
			"all distinct new action targets must pass one capacity preflight before any settlement route mutation");
		int actionLoop = analyzedPublication.LastIndexOf(
			"for (int index = 0; index < actions.Count; index++)",
			StringComparison.Ordinal);
		int setActionContext = analyzedPublication.IndexOf(
			"document.ProcessingActionId = action.ActionId",
			actionLoop,
			StringComparison.Ordinal);
		int registerOffer = analyzedPublication.IndexOf(
			"TrySettleRelayOffer(document",
			setActionContext,
			StringComparison.Ordinal);
		Test.True(actionLoop >= 0 && setActionContext > actionLoop && registerOffer > setActionContext,
			"all offers and directed mechanics in the document must be registered before round settlement refreshes once");

		string warResponse = ExtractSection(
			source,
			"private void AddWarResponseResultSettlementSlot(",
			"private void BeginOrExtendRoundResultSettlement(");
		Test.True(warResponse.Contains("document.Actions", StringComparison.Ordinal)
			&& warResponse.Contains("x.ChangedDiplomaticState", StringComparison.Ordinal)
			&& warResponse.Contains("action.TargetKingdomId", StringComparison.Ordinal)
			&& warResponse.Contains("document.DocumentId", StringComparison.Ordinal)
			&& warResponse.Contains("action.ActionId", StringComparison.Ordinal)
			&& warResponse.Contains("#", StringComparison.Ordinal),
			"one document may create several independently deduplicated successful-war response slots");

		string warAudit = ExtractMethod(source, "private bool IsWarResponseNoActionAllowed(");
		Test.True(warAudit.Contains("war.Actions", StringComparison.Ordinal)
			&& warAudit.Contains("x.ChangedDiplomaticState", StringComparison.Ordinal)
			&& warAudit.Contains("x.TargetKingdomId", StringComparison.Ordinal)
			&& warAudit.Contains("war.Actions == null || war.Actions.Count == 0", StringComparison.Ordinal),
			"a no-action war response must audit the exact successful war action, not the legacy primary action mirror");

		string canonicalEvents = ExtractMethod(source, "private void AppendCanonicalDocumentEvents(");
		Test.True(canonicalEvents.Contains("document.Actions", StringComparison.Ordinal)
			&& canonicalEvents.Contains("action.ActionId", StringComparison.Ordinal)
			&& canonicalEvents.Contains("action.TargetKingdomId", StringComparison.Ordinal)
			&& canonicalEvents.Contains(".ChangedDiplomaticState", StringComparison.Ordinal),
			"canonical facts must keep each target/action result separate after partial execution");
	}

	private static void PendingOfferSourceActionNormalizationContract(string source)
	{
		string normalization = ExtractMethod(source, "private void NormalizeStorage(");
		int buildDocumentIndex = normalization.IndexOf(
			"Dictionary<string, WorldDiplomacyDocument> normalizedDocumentsById = BuildDocumentIndex(_storage.Documents)",
			StringComparison.Ordinal);
		int normalizeOfferBinding = normalization.IndexOf(
			"NormalizePendingOfferSourceActionBinding(offer, normalizedDocumentsById)",
			buildDocumentIndex,
			StringComparison.Ordinal);
		Test.True(buildDocumentIndex >= 0 && normalizeOfferBinding > buildDocumentIndex,
			"save normalization must build one document index before repairing pending-offer action ownership");

		string binding = ExtractMethod(
			source,
			"private static bool NormalizePendingOfferSourceActionBinding(");
		int sourceLookup = binding.IndexOf(
			"documentsById.TryGetValue(offer.SourceDocumentId ?? \"\", out WorldDiplomacyDocument source)",
			StringComparison.Ordinal);
		int missingSourceInvalidation = binding.IndexOf(
			"offer.Status = \"invalidated\"",
			sourceLookup,
			StringComparison.Ordinal);
		Test.True(sourceLookup >= 0
			&& binding.Contains("source.IsReadyForPublication", StringComparison.Ordinal)
			&& binding.Contains("source.AuthorKingdomId, offer.ProposerKingdomId", StringComparison.Ordinal)
			&& missingSourceInvalidation > sourceLookup,
			"an open offer whose source is missing, unpublished, or owned by another proposer must be invalidated");

		int legacyActions = binding.IndexOf(
			"source.Actions == null || source.Actions.Count == 0",
			StringComparison.Ordinal);
		int legacyBlankNoOp = binding.IndexOf(
			"if (string.IsNullOrWhiteSpace(offer.SourceActionId)) return false;",
			legacyActions,
			StringComparison.Ordinal);
		int clearLegacyActionId = binding.IndexOf(
			"offer.SourceActionId = \"\"",
			legacyBlankNoOp,
			StringComparison.Ordinal);
		Test.True(legacyActions >= 0 && legacyBlankNoOp > legacyActions && clearLegacyActionId > legacyBlankNoOp,
			"legacy sources with null or empty Actions must keep a blank action id and clear only stale nonblank ids");

		int collectMatches = binding.IndexOf(
			"List<WorldDiplomacyDocumentAction> matches = source.Actions",
			clearLegacyActionId,
			StringComparison.Ordinal);
		int uniqueMatch = binding.IndexOf(
			"matches.Count == 1 && !string.IsNullOrWhiteSpace(matches[0].ActionId)",
			collectMatches,
			StringComparison.Ordinal);
		int rebindActionId = binding.IndexOf(
			"offer.SourceActionId = normalizedActionId",
			uniqueMatch,
			StringComparison.Ordinal);
		Test.True(collectMatches >= 0
			&& binding.Contains("x.TargetKingdomId, offer.TargetKingdomId", StringComparison.Ordinal)
			&& binding.Contains("NormalizeIntent(x.Intent), NormalizeIntent(offer.Intent)", StringComparison.Ordinal)
			&& binding.Contains(".Take(2)", StringComparison.Ordinal)
			&& uniqueMatch > collectMatches
			&& rebindActionId > uniqueMatch,
			"an old multi-action offer may rebind only to one exact target-and-intent action id");

		int unresolvedOpenGate = binding.IndexOf(
			"if (!isOpen) return false;",
			rebindActionId,
			StringComparison.Ordinal);
		int unresolvedInvalidation = binding.IndexOf(
			"offer.Status = \"invalidated\"",
			unresolvedOpenGate,
			StringComparison.Ordinal);
		Test.True(unresolvedOpenGate > rebindActionId && unresolvedInvalidation > unresolvedOpenGate,
			"zero or ambiguous multi-action matches must invalidate an open offer instead of guessing an action id");
	}

	private static void ImmediateWarResponsePeaceSuppressionContract(string source)
	{
		string suppression = ExtractMethod(
			source,
			"private bool IsImmediateWarResponsePeaceSuppressed(");
		Test.True(suppression.Contains("IsWarResponseNoActionAllowed(", StringComparison.Ordinal)
			&& suppression.Contains("FirstNonEmpty(slotId, round?.ResultSettlementCurrentSlotId)", StringComparison.Ordinal)
			&& suppression.Contains("author", StringComparison.Ordinal)
			&& suppression.Contains("target", StringComparison.Ordinal),
			"immediate-peace suppression must reuse the exact persisted war-response authorization instead of inferring from war state alone");

		string warAudit = ExtractMethod(
			source,
			"private bool IsWarResponseNoActionAllowed(");
		foreach (string exactBoundary in new[]
		{
			"round?.ResultSettlementPending",
			"round.State, \"active\"",
			"round.ResultSettlementCurrentSlotId, slotId",
			"x.SlotId, slotId",
			"x.KingdomId, author.StringId",
			"SettlementSlotHasKind(slot, \"war_response\")",
			"slot.RelatedKingdomIds.Contains(target.StringId",
			"slot.SourceDocumentIds",
			"ResolveDocument(sourceDocumentId)",
			"war?.IsReadyForPublication",
			"war.RoundId, round.RoundId",
			"war.AuthorKingdomId, target.StringId",
			"war.Actions?.Any",
			"x.ChangedDiplomaticState",
			"NormalizeIntent(x.Intent)",
			"\"declare_war\"",
			"x.TargetKingdomId, author.StringId"
		})
		{
			Test.True(warAudit.Contains(exactBoundary, StringComparison.Ordinal),
				"war-response peace suppression is missing exact slot/source/pair boundary: " + exactBoundary);
		}
		Test.True(warAudit.Contains("war.Actions == null || war.Actions.Count == 0", StringComparison.Ordinal),
			"legacy primary war fields may be used only when the source document has no persisted actions");

		string warRegistration = ExtractMethod(
			source,
			"private void AddWarResponseResultSettlementSlot(");
		int actionKey = warRegistration.IndexOf(
			"(document.DocumentId ?? \"\") + \"#\" + (action.ActionId ?? \"\")",
			StringComparison.Ordinal);
		int deduplicateAction = warRegistration.IndexOf(
			"ResultSettlementWarDocumentIds.Contains(actionKey",
			actionKey,
			StringComparison.Ordinal);
		int addActionSlot = warRegistration.IndexOf(
			"AddOrMergeResultSettlementSlot(round, action.TargetKingdomId, \"war_response\"",
			deduplicateAction,
			StringComparison.Ordinal);
		Test.True(warRegistration.Contains("document.Actions?.Count > 0", StringComparison.Ordinal)
			&& warRegistration.Contains("x.ChangedDiplomaticState", StringComparison.Ordinal)
			&& warRegistration.Contains("NormalizeIntent(x.Intent) == \"declare_war\"", StringComparison.Ordinal)
			&& actionKey >= 0 && deduplicateAction > actionKey && addActionSlot > deduplicateAction,
			"a successful war inside a multi-action document must own its response duty through the persisted docId#actionId key");

		string potentialActions = ExtractMethod(
			source,
			"private List<string> BuildPotentialDiplomaticActionIntents(");
		Test.True(potentialActions.Contains("if (atWar)", StringComparison.Ordinal)
			&& potentialActions.Contains("actions.Add(\"propose_peace\")", StringComparison.Ordinal),
			"ordinary wars, including the next round, must still expose propose_peace before the narrow current-slot filter runs");

		string legalActions = ExtractMethod(
			source,
			"private List<string> BuildLegalDiplomaticActionIntents(");
		int buildPotential = legalActions.IndexOf("BuildPotentialDiplomaticActionIntents(author, target)", StringComparison.Ordinal);
		int checkSuppression = legalActions.IndexOf("IsImmediateWarResponsePeaceSuppressed(", buildPotential, StringComparison.Ordinal);
		int removePeace = legalActions.IndexOf("actions.RemoveAll", checkSuppression, StringComparison.Ordinal);
		int offerHandling = legalActions.IndexOf("round?.ResultSettlementPending", removePeace, StringComparison.Ordinal);
		Test.True(buildPotential >= 0 && checkSuppression > buildPotential && removePeace > checkSuppression
			&& offerHandling > removePeace
			&& legalActions.Contains("\"propose_peace\"", StringComparison.Ordinal),
			"only the exact current war-response pair may lose propose_peace; third-country actions and later-round peace remain untouched");

		string declarationActions = ExtractMethod(
			source,
			"private List<string> BuildLegalDiplomaticDeclarationIntents(");
		int filteredActions = declarationActions.IndexOf("BuildLegalDiplomaticActionIntents(round, author, target)", StringComparison.Ordinal);
		int statementAuthorization = declarationActions.IndexOf("IsNonRootAiRelayNoActionAllowed(", filteredActions, StringComparison.Ordinal);
		int addStatement = declarationActions.IndexOf("intents.Add(\"statement\")", statementAuthorization, StringComparison.Ordinal);
		Test.True(filteredActions >= 0 && statementAuthorization > filteredActions && addStatement > statementAuthorization,
			"hiding immediate peace must not remove the defender's independently authorized statement response");

		string relayPrompt = ExtractMethod(
			source,
			"private string BuildRelayConversationTurnPrompt(");
		int legalActionMap = relayPrompt.IndexOf(
			"BuildLegalDiplomaticDeclarationIntentMap(",
			StringComparison.Ordinal);
		int targetLegalActions = relayPrompt.IndexOf(
			"legalActionsByTarget.TryGetValue(id, out List<string> targetActions)",
			legalActionMap,
			StringComparison.Ordinal);
		int includePeaceTerms = relayPrompt.IndexOf(
			"includePeaceNegotiationTerms = targetActions.Any",
			targetLegalActions,
			StringComparison.Ordinal);
		int legalPeaceIntent = relayPrompt.IndexOf(
			"\"propose_peace\"",
			includePeaceTerms,
			StringComparison.Ordinal);
		int appendTargetFacts = relayPrompt.IndexOf(
			"AppendDiplomaticTargetDecisionContext(",
			legalPeaceIntent,
			StringComparison.Ordinal);
		Test.True(legalActionMap >= 0 && targetLegalActions > legalActionMap
			&& includePeaceTerms > targetLegalActions
			&& legalPeaceIntent > includePeaceTerms
			&& appendTargetFacts > legalPeaceIntent,
			"relay targets must derive peace-term visibility from the shared live propose_peace action rather than every peace response intent");

		string targetFacts = ExtractMethod(
			source,
			"private void AppendDiplomaticTargetDecisionContext(");
		int buildWarFacts = targetFacts.IndexOf(
			"BuildWarDecisionContext(author, target, peaceTermsVisible)",
			StringComparison.Ordinal);
		Test.True(targetFacts.Contains("situation?.IsAtWar == true", StringComparison.Ordinal)
			&& targetFacts.Contains("战争硬性状态：双方已经交战", StringComparison.Ordinal)
			&& targetFacts.Contains("includePeaceNegotiationTerms", StringComparison.Ordinal)
			&& targetFacts.Contains("IsImmediateWarResponsePeaceSuppressed(", StringComparison.Ordinal)
			&& buildWarFacts >= 0,
			"the shared target snapshot must always expose war facts while independently suppressing fresh-response negotiation terms");

		string warDecision = ExtractMethod(source, "private string BuildWarDecisionContext(");
		int durableWarFacts = warDecision.IndexOf("双方总体军力=", StringComparison.Ordinal);
		int termsGate = warDecision.IndexOf("if (includePeaceNegotiationTerms)", StringComparison.Ordinal);
		int negotiationTerms = warDecision.IndexOf("仅在本篇可选和平动作时使用的议和条件", termsGate, StringComparison.Ordinal);
		Test.True(durableWarFacts >= 0 && termsGate > durableWarFacts && negotiationTerms > termsGate,
			"fresh defenders must retain duration, strength, progress, and other-war facts; only peace terms may be hidden until later rounds");

		string dynamicOptions = ExtractMethod(
			source,
			"private string BuildCurrentLegalDiplomaticOptions(");
		Test.True(dynamicOptions.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& dynamicOptions.Contains("resultSettlementSlotId", StringComparison.Ordinal),
			"the visible per-target prompt options must come from the shared slot-aware legal declaration source");

		string signature = ExtractMethod(
			source,
			"private string BuildGenerationLegalActionSignature(");
		Test.True(signature.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& signature.Contains("job.ResultSettlementSlotId", StringComparison.Ordinal)
			&& signature.Contains("responseSource", StringComparison.Ordinal),
			"stale-prompt signatures must hash the same slot-aware action set shown to the model");

		string repair = ExtractMethod(
			source,
			"private bool EnqueueGeneratedDeclarationRepair(");
		Test.True(repair.Contains("BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal)
			&& repair.Contains("source.ResultSettlementSlotId", StringComparison.Ordinal)
			&& repair.Contains("repair.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(repair)", StringComparison.Ordinal),
			"repair prompts and their refreshed signature must retain the same war-response slot filter");

		string generatedLegality = ExtractMethod(
			source,
			"private bool TryGetGeneratedSingleActionLegalityViolation(");
		Test.True(generatedLegality.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& generatedLegality.Contains("job.ResultSettlementSlotId", StringComparison.Ordinal)
			&& generatedLegality.Contains("responseSource", StringComparison.Ordinal)
			&& generatedLegality.Contains("intent_not_in_current_legal_action_list", StringComparison.Ordinal),
			"a draft that invents immediate peace must be rejected by the same action set used by prompt and signature");

		string singlePublication = ExtractMethod(
			source,
			"private void ProcessAnalyzedDocument(");
		int singleFinalGuard = singlePublication.IndexOf(
			"IsImmediateWarResponsePeaceSuppressed(owningRound, document.ResultSettlementSlotId, author, target)",
			StringComparison.Ordinal);
		int singlePublish = singlePublication.IndexOf("document.IsReadyForPublication = true", singleFinalGuard, StringComparison.Ordinal);
		Test.True(singlePublication.Contains("normalizedIntent == \"propose_peace\"", StringComparison.Ordinal)
			&& singlePublication.Contains("immediate_war_response_peace_suppressed", StringComparison.Ordinal)
			&& singleFinalGuard >= 0 && singlePublish > singleFinalGuard,
			"legacy single-action publication must recheck the exact live slot before any peace proposal can publish or execute");

		string multiPublication = ExtractMethod(
			source,
			"private void ProcessAnalyzedMultiActionDocument(");
		int multiFinalGuard = multiPublication.IndexOf(
			"IsImmediateWarResponsePeaceSuppressed(round, document.ResultSettlementSlotId, author, target)",
			StringComparison.Ordinal);
		int multiPublish = multiPublication.IndexOf("document.IsReadyForPublication = true", multiFinalGuard, StringComparison.Ordinal);
		Test.True(multiPublication.Contains("intent == \"propose_peace\"", StringComparison.Ordinal)
			&& multiPublication.Contains("immediate_war_response_peace_suppressed", StringComparison.Ordinal)
			&& multiFinalGuard >= 0 && multiPublish > multiFinalGuard,
			"each action in a multi-target document must cross the same final live war-response peace guard before the batch executes");
	}

	private static void PeaceOfferResponseExclusivityContract(string source)
	{
		string uniqueOfferResolver = ExtractSection(
			source,
			"private static bool TryResolveUniqueOpenProposalForRound(",
			"private bool TryDeriveGeneratedDiplomaticStructure(");
		foreach (string exactOfferBoundary in new[]
		{
			"x.Status, \"open\"",
			"NormalizeIntent(x.Intent), proposalIntent",
			"x.ProposerKingdomId, proposer.StringId",
			"x.TargetKingdomId, responder.StringId",
			".Take(2)",
			"matches.Count != 1"
		})
		{
			Test.True(uniqueOfferResolver.Contains(exactOfferBoundary, StringComparison.Ordinal),
				"exclusive peace response is missing exact open/unique/pair ownership: " + exactOfferBoundary);
		}

		string legalActions = ExtractMethod(
			source,
			"private List<string> BuildLegalDiplomaticActionIntents(");
		int buildPotential = legalActions.IndexOf(
			"BuildPotentialDiplomaticActionIntents(author, target)",
			StringComparison.Ordinal);
		int uniquePeaceOffer = legalActions.IndexOf(
			"TryResolveUniqueOpenProposalForRound(",
			buildPotential,
			StringComparison.Ordinal);
		int clearOrdinaryActions = uniquePeaceOffer >= 0
			? legalActions.IndexOf("actions.Clear();", uniquePeaceOffer, StringComparison.Ordinal)
			: -1;
		int addAccept = clearOrdinaryActions >= 0
			? legalActions.IndexOf("actions.Add(\"accept_peace\")", clearOrdinaryActions, StringComparison.Ordinal)
			: -1;
		int addReject = addAccept >= 0
			? legalActions.IndexOf("actions.Add(\"reject_peace\")", addAccept, StringComparison.Ordinal)
			: -1;
		int exclusiveReturn = addReject >= 0
			? legalActions.IndexOf("return actions", addReject, StringComparison.Ordinal)
			: -1;
		Test.True(buildPotential >= 0 && uniquePeaceOffer > buildPotential
			&& clearOrdinaryActions > uniquePeaceOffer
			&& addAccept > clearOrdinaryActions
			&& addReject > addAccept
			&& exclusiveReturn > addReject,
			"one unique target-to-author peace offer must replace that pair's actions with accept_peace/reject_peace only");

		string exclusiveBranch = uniquePeaceOffer >= 0 && exclusiveReturn > uniquePeaceOffer
			? legalActions.Substring(uniquePeaceOffer, exclusiveReturn - uniquePeaceOffer)
			: "";
		Test.True(exclusiveBranch.Contains("round", StringComparison.Ordinal)
			&& exclusiveBranch.Contains("author", StringComparison.Ordinal)
			&& exclusiveBranch.Contains("target", StringComparison.Ordinal)
			&& exclusiveBranch.Contains("\"propose_peace\"", StringComparison.Ordinal)
			&& !exclusiveBranch.Contains("AppendOpenOfferResponseIntents", StringComparison.Ordinal),
			"peace exclusivity must bind the exact target-to-author pair and must not admit sibling offer types");
		int genericOfferResponses = exclusiveReturn >= 0
			? legalActions.IndexOf(
				"AppendOpenOfferResponseIntents(round, author, target, actions)",
				exclusiveReturn,
				StringComparison.Ordinal)
			: -1;
		Test.True(genericOfferResponses > exclusiveReturn,
			"zero, non-peace, closed, or non-unique peace offers must fall through to ordinary pair-scoped offer handling");

		string declarationActions = ExtractMethod(
			source,
			"private List<string> BuildLegalDiplomaticDeclarationIntents(");
		int mustAnswerPeace = declarationActions.IndexOf("bool mustAnswerPeaceOffer", StringComparison.Ordinal);
		int exactPeaceResponses = declarationActions.IndexOf("IsExclusivePeaceOfferResponseSet(intents)", StringComparison.Ordinal);
		int statementGate = declarationActions.IndexOf(
			"if (!mustAnswerPeaceOffer && IsNonRootAiRelayNoActionAllowed(",
			exactPeaceResponses,
			StringComparison.Ordinal);
		int addStatement = declarationActions.IndexOf("intents.Add(\"statement\")", statementGate, StringComparison.Ordinal);
		Test.True(mustAnswerPeace >= 0 && exactPeaceResponses > mustAnswerPeace
			&& statementGate > exactPeaceResponses && addStatement > statementGate,
			"the exact incoming peace pair must expose only accept/reject; statement remains governed by the ordinary live gate for every other pair");
		string exclusiveSet = ExtractMethod(source, "private static bool IsExclusivePeaceOfferResponseSet(");
		Test.True(exclusiveSet.Contains("\"accept_peace\"", StringComparison.Ordinal)
			&& exclusiveSet.Contains("\"reject_peace\"", StringComparison.Ordinal)
			&& exclusiveSet.Contains("return hasIntent", StringComparison.Ordinal),
			"the shared peace-response classifier must accept a nonempty accept/reject-only set");

		string dynamicOptions = ExtractMethod(
			source,
			"private string BuildCurrentLegalDiplomaticOptions(");
		Test.True(dynamicOptions.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& !dynamicOptions.Contains("不可用", StringComparison.Ordinal)
			&& !dynamicOptions.Contains("不能再次提出和平", StringComparison.Ordinal),
			"dynamic options must inherit the shared allow-list without explaining hidden actions in prompt prose");

		string openOfferPrompt = ExtractMethod(
			source,
			"private static void AppendOpenOfferAnswerRequirement(");
		Test.True(openOfferPrompt.Contains("必须按当前可选动作处理", StringComparison.Ordinal)
			&& openOfferPrompt.Contains("accept_*只表示无条件接受全部原条件并立即生效", StringComparison.Ordinal)
			&& !openOfferPrompt.Contains("反提", StringComparison.Ordinal),
			"offer-response prompts must not advertise a counter-proposal that the live peace pair hides");

		string analysisContract = ExtractMethod(source, "private static string BuildAnalysisModeContract(");
		Test.True(analysisContract.Contains("和平原案只能原样接受或明确拒绝", StringComparison.Ordinal)
			&& analysisContract.Contains("不得改写条款或另提和平方案", StringComparison.Ordinal)
			&& analysisContract.Contains("accept_peace由系统继承原案", StringComparison.Ordinal)
			&& !analysisContract.Contains("提出不同条件属于新反提案", StringComparison.Ordinal),
			"player analysis must classify an incoming peace offer as accept/reject only");

		string relayPrompt = ExtractMethod(source, "private string BuildRelayConversationTurnPrompt(");
		string targetedPrompt = ExtractMethod(source, "private string BuildGenerationPrompt(");
		Test.True(relayPrompt.Contains("BuildSourceActionFactForTarget(prioritySource, author.StringId)", StringComparison.Ordinal)
			&& targetedPrompt.Contains("BuildSourceActionFactForTarget(sourceDocument, author.StringId)", StringComparison.Ordinal)
			&& targetedPrompt.Contains("BuildPeaceOfferTermsFact(sourceDocument, author.StringId)", StringComparison.Ordinal)
			&& relayPrompt.Contains("不得附加、修改条款或另提和平方案", StringComparison.Ordinal)
			&& targetedPrompt.Contains("不得附加、修改条款或另提和平方案", StringComparison.Ordinal),
			"AI response prompts must bind the source action to the current responder and forbid conditional rejection/counter-terms");
		string sourceActionFact = ExtractMethod(source, "private static string BuildSourceActionFactForTarget(");
		Test.True(sourceActionFact.Contains("ResolveSourceActionForTarget(source, targetKingdomId)", StringComparison.Ordinal)
			&& sourceActionFact.Contains("source.Actions?.Count > 0", StringComparison.Ordinal)
			&& sourceActionFact.Contains("action.ActionId", StringComparison.Ordinal),
			"multi-action source prompts must use the action aimed at the current responder and never fall back to the first-action mirror");
		string peaceTermsFact = ExtractMethod(source, "private static string BuildPeaceOfferTermsFact(");
		string peaceTermsFormatter = ExtractMethod(source, "private static string FormatPeaceTermsForPrompt(");
		Test.True(peaceTermsFact.Contains("ResolveSourceActionForTarget(source, targetKingdomId)", StringComparison.Ordinal)
			&& peaceTermsFact.Contains("action.PeaceTerms", StringComparison.Ordinal)
			&& peaceTermsFormatter.Contains("DailyTribute", StringComparison.Ordinal)
			&& peaceTermsFormatter.Contains("DurationDays", StringComparison.Ordinal)
			&& peaceTermsFormatter.Contains("CessionSettlementId", StringComparison.Ordinal),
			"response prompts must expose exact source-action tribute, duration, and cession terms without alternatives");
		string relaySourceContext = ExtractMethod(source, "private void AppendRelayResponseSourceContext(");
		Test.True(relaySourceContext.Contains("round.PendingOffers", StringComparison.Ordinal)
			&& relaySourceContext.Contains("offer.TargetKingdomId, author.StringId", StringComparison.Ordinal)
			&& relaySourceContext.Contains("sourceIds.Add(offer.SourceDocumentId)", StringComparison.Ordinal)
			&& relaySourceContext.Contains("answerablePeaceOfferSourceIds", StringComparison.Ordinal)
			&& relaySourceContext.Contains(".Take(4)", StringComparison.Ordinal),
			"ordinary relay prompts must include and prioritize the current speaker's pending peace source while retaining a bounded source scan");

		string commitAnalysis = ExtractMethod(source, "private void CommitAnalysis(");
		int bindPlayerOffer = commitAnalysis.IndexOf("ReconcilePlayerDeclarationWithOpenOffer(", StringComparison.Ordinal);
		int copyOriginalTerms = commitAnalysis.IndexOf("document.PeaceTerms = ClonePeaceTerms(", bindPlayerOffer, StringComparison.Ordinal);
		int publishPlayerDocument = commitAnalysis.IndexOf("ProcessAnalyzedDocument(", copyOriginalTerms, StringComparison.Ordinal);
		Test.True(bindPlayerOffer >= 0 && copyOriginalTerms > bindPlayerOffer && publishPlayerDocument > copyOriginalTerms
			&& commitAnalysis.Contains("ResolveOfferedPeaceTerms(", StringComparison.Ordinal)
			&& commitAnalysis.Contains("document.RespondingToOfferActionId", StringComparison.Ordinal),
			"player accept_peace must inherit the exact source action terms before final publication validation");
		string offeredTerms = ExtractMethod(source, "private static WorldDiplomacyPeaceTerms ResolveOfferedPeaceTerms(");
		Test.True(offeredTerms.Contains("source.Actions?.Count > 0", StringComparison.Ordinal)
			&& offeredTerms.Contains("ResolveDocumentAction(source, sourceActionId)?.PeaceTerms", StringComparison.Ordinal)
			&& offeredTerms.Contains("return source.PeaceTerms", StringComparison.Ordinal),
			"multi-action peace offers must resolve by action id, with document-level terms reserved for legacy single-action sources");

		string signature = ExtractMethod(
			source,
			"private string BuildGenerationLegalActionSignature(");
		Test.True(signature.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& signature.Contains("job.ResultSettlementSlotId", StringComparison.Ordinal),
			"stale-action signatures must inherit peace-response exclusivity from the shared builder");

		string repair = ExtractMethod(
			source,
			"private bool EnqueueGeneratedDeclarationRepair(");
		Test.True(repair.Contains("BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal)
			&& repair.Contains("BuildGenerationLegalActionSignature(repair)", StringComparison.Ordinal),
			"repair must refresh both options and signature from the same exclusive peace-response source");
		int requiredOfferLookup = repair.IndexOf(
			"WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(",
			StringComparison.Ordinal);
		int requiredOfferRepair = repair.IndexOf(
			"string.Equals(reason, \"required_peace_offer_response_missing\"",
			requiredOfferLookup,
			StringComparison.Ordinal);
		int nextRepairBranch = repair.IndexOf(
			"else if (reason.StartsWith(\"offer_response_\"",
			requiredOfferRepair,
			StringComparison.Ordinal);
		string requiredOfferRepairBranch = requiredOfferRepair >= 0 && nextRepairBranch > requiredOfferRepair
			? repair.Substring(requiredOfferRepair, nextRepairBranch - requiredOfferRepair)
			: "";
		Test.True(requiredOfferLookup >= 0
			&& repair.IndexOf("repairRound", requiredOfferLookup, StringComparison.Ordinal) > requiredOfferLookup
			&& repair.IndexOf("source.ResultSettlementSlotId", requiredOfferLookup, StringComparison.Ordinal) > requiredOfferLookup
			&& repair.IndexOf("source.IsExternalResponseOnly", requiredOfferLookup, StringComparison.Ordinal) > requiredOfferLookup
			&& repair.IndexOf("source.SourceDocumentId", requiredOfferLookup, StringComparison.Ordinal) > requiredOfferLookup
			&& repair.IndexOf("requireAnyOpenPeaceOffer: source.IsRelayTurn", requiredOfferLookup, StringComparison.Ordinal) > requiredOfferLookup
			&& requiredOfferRepair > requiredOfferLookup,
			"the missing-required-peace repair must resolve the exact live slot/source obligation before writing its correction");
		Test.True(requiredOfferRepairBranch.Contains("actions必须答复和平原案：来源=", StringComparison.Ordinal)
			&& requiredOfferRepairBranch.Contains("requiredPeaceOffer.SourceDocumentId", StringComparison.Ordinal)
			&& requiredOfferRepairBranch.Contains("requiredPeaceOffer.SourceActionId", StringComparison.Ordinal)
			&& requiredOfferRepairBranch.Contains("requiredPeaceOffer.ProposerKingdomId", StringComparison.Ordinal)
			&& requiredOfferRepairBranch.Contains("只能原样接受或明确拒绝，其他合法对象动作可保留", StringComparison.Ordinal)
			&& !requiredOfferRepairBranch.Contains("PendingOffers", StringComparison.Ordinal)
			&& !requiredOfferRepairBranch.Contains("foreach", StringComparison.Ordinal),
			"the repair must give one short exact source/action/proposer correction without dumping an offer list");

		string generatedLegality = ExtractMethod(
			source,
			"private bool TryGetGeneratedSingleActionLegalityViolation(");
		Test.True(generatedLegality.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& generatedLegality.Contains("intent_not_in_current_legal_action_list", StringComparison.Ordinal),
			"generated single and multi actions must inherit exclusivity through their shared per-action legality check");

		string singlePublication = ExtractMethod(source, "private void ProcessAnalyzedDocument(");
		int singleFinalLegalSet = singlePublication.IndexOf(
			"BuildLegalDiplomaticDeclarationIntents(",
			StringComparison.Ordinal);
		int singlePublish = singlePublication.IndexOf("document.IsReadyForPublication = true", singleFinalLegalSet, StringComparison.Ordinal);
		Test.True(singleFinalLegalSet >= 0 && singlePublish > singleFinalLegalSet
			&& singlePublication.Contains("document.ResultSettlementSlotId", StringComparison.Ordinal)
			&& singlePublication.Contains("normalizedIntent", StringComparison.Ordinal),
			"single-action publication must recheck the shared live legal set before publishing an obsolete counter-proposal");

		string multiPublication = ExtractMethod(source, "private void ProcessAnalyzedMultiActionDocument(");
		int multiFinalLegalSet = multiPublication.IndexOf(
			"BuildLegalDiplomaticDeclarationIntents(",
			StringComparison.Ordinal);
		int multiPublish = multiPublication.IndexOf("document.IsReadyForPublication = true", multiFinalLegalSet, StringComparison.Ordinal);
		Test.True(multiFinalLegalSet >= 0 && multiPublish > multiFinalLegalSet
			&& multiPublication.Contains("document.ResultSettlementSlotId", StringComparison.Ordinal)
			&& multiPublication.Contains("intent", StringComparison.Ordinal),
			"every multi-target action must recheck the shared live legal set before the batch becomes publishable");
	}

	private static void MandatoryPeaceOfferCoverageContract(string source)
	{
		string generatedLegality = ExtractMethod(
			source,
			"private bool TryGetGeneratedIntentLegalityViolation(");
		int generatedRequiredOffer = generatedLegality.IndexOf(
			"WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(",
			StringComparison.Ordinal);
		int generatedRelayFallback = generatedLegality.IndexOf(
			"requireAnyOpenPeaceOffer: job.IsRelayTurn",
			generatedRequiredOffer,
			StringComparison.Ordinal);
		int generatedCoverage = generatedLegality.IndexOf(
			"GeneratedActionsContainRequiredPeaceOfferResponse(actions, requiredPeaceOffer)",
			generatedRelayFallback,
			StringComparison.Ordinal);
		Test.True(generatedRequiredOffer >= 0
			&& generatedRelayFallback > generatedRequiredOffer
			&& generatedCoverage > generatedRelayFallback,
			"every ordinary relay draft must answer an incoming open peace offer even when it is not the explicit source document");

		string relayPrompt = ExtractMethod(source, "private string BuildRelayConversationTurnPrompt(");
		int relayRequiredOffer = relayPrompt.IndexOf(
			"WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(",
			StringComparison.Ordinal);
		int relayFallback = relayPrompt.IndexOf(
			"requireAnyOpenPeaceOffer: true",
			relayRequiredOffer,
			StringComparison.Ordinal);
		int relaySourceContext = relayPrompt.IndexOf(
			"AppendRelayResponseSourceContext(",
			relayFallback,
			StringComparison.Ordinal);
		Test.True(relayRequiredOffer >= 0 && relayFallback > relayRequiredOffer
			&& relaySourceContext > relayFallback
			&& relayPrompt.IndexOf(
				"requiredPeaceOffer?.SourceDocumentId",
				relaySourceContext,
				StringComparison.Ordinal) > relaySourceContext,
			"ordinary relay prompts must select any still-open incoming peace offer and pass that required source into response context");

		string submitPlayer = ExtractMethod(source, "private void SubmitPlayerDocument(");
		int currentPlayerSlot = submitPlayer.IndexOf(
			"WorldDiplomacyResultSettlementSlot playerSettlementSlot = round?.ResultSettlementPending == true",
			StringComparison.Ordinal);
		int stampPlayerSlot = submitPlayer.IndexOf(
			"document.ResultSettlementSlotId = playerSettlementSlot.SlotId ?? \"\"",
			currentPlayerSlot,
			StringComparison.Ordinal);
		int addPlayerDocument = submitPlayer.IndexOf("AddDocument(document)", stampPlayerSlot, StringComparison.Ordinal);
		Test.True(currentPlayerSlot >= 0
			&& submitPlayer.IndexOf("x.SlotId, round.ResultSettlementCurrentSlotId", currentPlayerSlot, StringComparison.Ordinal) > currentPlayerSlot
			&& submitPlayer.IndexOf("x.KingdomId, playerKingdom.StringId", currentPlayerSlot, StringComparison.Ordinal) > currentPlayerSlot
			&& submitPlayer.IndexOf("x.Status, \"waiting_player\"", currentPlayerSlot, StringComparison.Ordinal) > currentPlayerSlot
			&& stampPlayerSlot > currentPlayerSlot
			&& addPlayerDocument > stampPlayerSlot,
			"player analysis may inherit only the current result-settlement slot owned by the player and already waiting for player input");

		string singlePublication = ExtractMethod(source, "private void ProcessAnalyzedDocument(");
		string multiPublication = ExtractMethod(source, "private void ProcessAnalyzedMultiActionDocument(");
		foreach ((string method, string name) in new[]
		{
			(singlePublication, "single-action"),
			(multiPublication, "multi-action")
		})
		{
			int requiredOffer = method.IndexOf(
				"WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(",
				StringComparison.Ordinal);
			int playerFallback = method.IndexOf(
				"requireAnyOpenPeaceOffer: document.IsRelayTurn || document.IsPlayerAuthored",
				requiredOffer,
				StringComparison.Ordinal);
			int finalCoverage = method.IndexOf(
				"DocumentContainsRequiredPeaceOfferResponse(document, requiredPeaceOffer)",
				playerFallback,
				StringComparison.Ordinal);
			Test.True(requiredOffer >= 0 && playerFallback > requiredOffer && finalCoverage > playerFallback,
				name + " final publication must require ordinary relay and player documents to answer any pending peace offer");
		}

		string relaySources = ExtractMethod(source, "private void AppendRelayResponseSourceContext(");
		int requiredRelaySourceFirst = relaySources.IndexOf(
			".OrderByDescending(x => string.Equals(x.DocumentId, requiredSourceDocumentId",
			StringComparison.Ordinal);
		int ordinaryRelaySourceSecond = relaySources.IndexOf(
			".ThenByDescending(x => string.Equals(x.DocumentId, responseSource?.DocumentId",
			requiredRelaySourceFirst,
			StringComparison.Ordinal);
		int peaceSourceThird = relaySources.IndexOf(
			".ThenByDescending(x => answerablePeaceOfferSourceIds.Contains(x.DocumentId))",
			ordinaryRelaySourceSecond,
			StringComparison.Ordinal);
		int relayTakeFour = relaySources.IndexOf(".Take(4)", peaceSourceThird, StringComparison.Ordinal);
		Test.True(requiredRelaySourceFirst >= 0
			&& ordinaryRelaySourceSecond > requiredRelaySourceFirst
			&& peaceSourceThird > ordinaryRelaySourceSecond
			&& relayTakeFour > peaceSourceThird,
			"relay source context must rank the mandatory peace source first while retaining its four-document bound");

		string playerAnalysis = ExtractMethod(source, "private string BuildAnalysisPrompt(");
		int prunePlayerOffers = playerAnalysis.IndexOf(
			"if (document.IsPlayerAuthored) PruneInvalidOffers(analysisRound)",
			StringComparison.Ordinal);
		int playerRequiredOffer = playerAnalysis.IndexOf(
			"WorldDiplomacyRoundOffer requiredPlayerPeaceOffer = FindRequiredPeaceOfferResponse(",
			prunePlayerOffers,
			StringComparison.Ordinal);
		int playerRequireAny = playerAnalysis.IndexOf(
			"requireAnyOpenPeaceOffer: true",
			playerRequiredOffer,
			StringComparison.Ordinal);
		int playerRequiredFirst = playerAnalysis.IndexOf(
			".OrderByDescending(x => requiredPlayerPeaceOffer != null",
			playerRequireAny,
			StringComparison.Ordinal);
		int playerRequiredDocument = playerAnalysis.IndexOf(
			"x.SourceDocumentId, requiredPlayerPeaceOffer.SourceDocumentId",
			playerRequiredFirst,
			StringComparison.Ordinal);
		int playerRequiredAction = playerAnalysis.IndexOf(
			"x.SourceActionId ?? \"\", requiredPlayerPeaceOffer.SourceActionId ?? \"\"",
			playerRequiredDocument,
			StringComparison.Ordinal);
		int playerTakeFour = playerAnalysis.IndexOf(".Take(4)", playerRequiredAction, StringComparison.Ordinal);
		Test.True(prunePlayerOffers >= 0
			&& playerAnalysis.IndexOf("AppendDiplomaticThreatAnalysisContext", prunePlayerOffers, StringComparison.Ordinal) > prunePlayerOffers
			&& playerRequiredOffer > prunePlayerOffers && playerRequireAny > playerRequiredOffer
			&& playerRequiredFirst > playerRequireAny
			&& playerRequiredDocument > playerRequiredFirst
			&& playerRequiredAction > playerRequiredDocument
			&& playerTakeFour > playerRequiredAction,
			"player analysis must require any incoming peace offer, rank its exact source/action first, and still cap the list at four");
	}

	private static void PeaceOfferTermsExecutabilityContract(string source)
	{
		string executable = ExtractMethod(
			source,
			"private static bool AreOfferedPeaceTermsCurrentlyExecutable(");
		int resolveExactTerms = executable.IndexOf(
			"ResolveOfferedPeaceTerms(source, offer.SourceActionId)",
			StringComparison.Ordinal);
		int promisedTribute = executable.IndexOf(
			"int promisedTribute = Math.Max(0, terms.DailyTribute)",
			resolveExactTerms,
			StringComparison.Ordinal);
		int promisedDuration = executable.IndexOf(
			"int promisedDuration = Math.Max(0, terms.DurationDays)",
			promisedTribute,
			StringComparison.Ordinal);
		int exactTribute = executable.IndexOf(
			"DiplomacyPeaceTermsService.ClampTributeAmount(payer, promisedTribute) != promisedTribute",
			promisedDuration,
			StringComparison.Ordinal);
		int exactDuration = executable.IndexOf(
			") != promisedDuration",
			exactTribute,
			StringComparison.Ordinal);
		int noTributeDuration = executable.IndexOf(
			"else if (promisedDuration != 0)",
			exactDuration,
			StringComparison.Ordinal);
		Test.True(resolveExactTerms >= 0
			&& executable.Contains("ResolveDocumentAction(source, offer.SourceActionId) == null", StringComparison.Ordinal)
			&& promisedTribute > resolveExactTerms
			&& promisedDuration > promisedTribute
			&& executable.Contains("payer == null || receiver == null || payer == receiver", StringComparison.Ordinal)
			&& executable.Contains("payer != proposer && payer != target", StringComparison.Ordinal)
			&& executable.Contains("receiver != proposer && receiver != target", StringComparison.Ordinal)
			&& exactTribute > promisedDuration
			&& executable.IndexOf("hasTribute: true", exactTribute, StringComparison.Ordinal) > exactTribute
			&& exactDuration > exactTribute
			&& noTributeDuration > exactDuration,
			"a peace offer remains executable only when tribute and duration survive runtime normalization exactly without shrinking");

		int anyCession = executable.IndexOf("bool hasAnyCession", noTributeDuration, StringComparison.Ordinal);
		int resolveCession = executable.IndexOf(
			"Settlement settlement = ResolveSettlementById(terms.CessionSettlementId)",
			anyCession,
			StringComparison.Ordinal);
		int ownerStillFrom = executable.IndexOf(
			"settlement.OwnerClan?.Kingdom == from",
			resolveCession,
			StringComparison.Ordinal);
		int receiverHasRuler = executable.IndexOf(
			"to.RulingClan?.Leader != null",
			ownerStillFrom,
			StringComparison.Ordinal);
		Test.True(anyCession >= 0
			&& executable.Contains("terms.CessionFromKingdomId", StringComparison.Ordinal)
			&& executable.Contains("terms.CessionToKingdomId", StringComparison.Ordinal)
			&& executable.Contains("terms.CessionSettlementId", StringComparison.Ordinal)
			&& resolveCession > anyCession
			&& executable.IndexOf("from == proposer || from == target", resolveCession, StringComparison.Ordinal) > resolveCession
			&& executable.IndexOf("to == proposer || to == target", resolveCession, StringComparison.Ordinal) > resolveCession
			&& ownerStillFrom > resolveCession
			&& receiverHasRuler > ownerStillFrom,
			"a cession offer must still be owned by the promised source kingdom and have a valid receiving ruler before acceptance");

		string prune = ExtractMethod(source, "private void PruneInvalidOffers(");
		int pruneDocumentIndex = prune.IndexOf(
			"documentsById ??= BuildDocumentIndex(_storage.Documents)",
			StringComparison.Ordinal);
		int pruneExecutability = prune.IndexOf(
			"AreOfferedPeaceTermsCurrentlyExecutable(offer, source, proposer, target)",
			pruneDocumentIndex,
			StringComparison.Ordinal);
		int pruneInvalidation = prune.IndexOf("offer.Status = \"invalidated\"", pruneExecutability, StringComparison.Ordinal);
		Test.True(pruneDocumentIndex >= 0 && pruneExecutability > pruneDocumentIndex && pruneInvalidation > pruneExecutability,
			"offer pruning must invalidate a peace offer whose exact stored terms can no longer execute");

		foreach ((string method, string pruneCall, string name) in new[]
		{
			(ExtractMethod(source, "private void ProcessAnalyzedDocument("), "PruneInvalidOffers(owningRound)", "single-action"),
			(ExtractMethod(source, "private void ProcessAnalyzedMultiActionDocument("), "PruneInvalidOffers(round)", "multi-action")
		})
		{
			int pruneBeforePublication = method.IndexOf(pruneCall, StringComparison.Ordinal);
			int finalLiveLegalSet = method.IndexOf(
				"List<string> finalLiveIntents",
				pruneBeforePublication,
				StringComparison.Ordinal);
			int readyForPublication = method.IndexOf(
				"document.IsReadyForPublication = true",
				finalLiveLegalSet,
				StringComparison.Ordinal);
			Test.True(pruneBeforePublication >= 0
				&& finalLiveLegalSet > pruneBeforePublication
				&& readyForPublication > finalLiveLegalSet,
				name + " analyzed publication must prune stale peace terms before rebuilding the final live legal set and before publication");
		}

		string settleOffer = ExtractMethod(source, "private void TrySettleRelayOffer(");
		int acceptExecutability = settleOffer.IndexOf(
			"AreOfferedPeaceTermsCurrentlyExecutable(resolvedOffer, source, proposer, target)",
			StringComparison.Ordinal);
		int acceptFailure = settleOffer.IndexOf(
			"和平原案条款已无法原样履行",
			acceptExecutability,
			StringComparison.Ordinal);
		int cloneExactTerms = settleOffer.IndexOf(
			"ClonePeaceTerms(ResolveOfferedPeaceTerms(source, resolvedOffer.SourceActionId))",
			acceptFailure,
			StringComparison.Ordinal);
		int executePeace = settleOffer.IndexOf("ExecuteMakePeace(proposer, target, document)", cloneExactTerms, StringComparison.Ordinal);
		Test.True(acceptExecutability >= 0
			&& acceptFailure > acceptExecutability
			&& settleOffer.IndexOf("return;", acceptFailure, StringComparison.Ordinal) > acceptFailure
			&& cloneExactTerms > acceptFailure
			&& executePeace > cloneExactTerms,
			"peace acceptance must revalidate exact terms, abort on drift, then clone the original terms before executing peace");
	}

	private static void MultiplePeaceAcceptanceCessionContract(string source)
	{
		string generatedCheck = ExtractMethod(
			source,
			"private bool GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(");
		int generatedCountSafe = generatedCheck.IndexOf(
			"if (acceptances.Count <= 1) return false;",
			StringComparison.Ordinal);
		int generatedLoop = generatedCheck.IndexOf("foreach (JObject acceptance in acceptances)", generatedCountSafe, StringComparison.Ordinal);
		int generatedSource = generatedCheck.IndexOf(
			"ResolveDocument(ReadString(acceptance, \"responding_to_offer_document_id\"))",
			generatedLoop,
			StringComparison.Ordinal);
		int generatedTerms = generatedCheck.IndexOf(
			"ReadString(acceptance, \"responding_to_offer_action_id\")",
			generatedSource,
			StringComparison.Ordinal);
		int generatedCessionUnsafe = generatedCheck.IndexOf(
			"if (source == null || PeaceTermsContainCession(terms)) return true;",
			generatedTerms,
			StringComparison.Ordinal);
		int generatedNoCessionSafe = generatedCheck.LastIndexOf("return false;", StringComparison.Ordinal);
		Test.True(generatedCountSafe >= 0
			&& generatedCheck.Contains("NormalizeIntent(ReadString(x, \"intent\", \"author_intent.intent\"))", StringComparison.Ordinal)
			&& generatedCheck.Contains("\"accept_peace\"", StringComparison.Ordinal)
			&& generatedLoop > generatedCountSafe
			&& generatedSource > generatedLoop
			&& generatedTerms > generatedSource
			&& generatedCessionUnsafe > generatedTerms
			&& generatedNoCessionSafe > generatedCessionUnsafe,
			"generated peace acceptances are safe at count zero or one, and several remain safe only when every exact source action has no cession");

		string documentCheck = ExtractMethod(
			source,
			"private bool DocumentHasUnsafeMultiplePeaceAcceptances(");
		int documentCountSafe = documentCheck.IndexOf(
			"if (acceptances.Count <= 1) return false;",
			StringComparison.Ordinal);
		int documentLoop = documentCheck.IndexOf(
			"foreach (WorldDiplomacyDocumentAction acceptance in acceptances)",
			documentCountSafe,
			StringComparison.Ordinal);
		int documentSource = documentCheck.IndexOf(
			"ResolveDocument(acceptance.RespondingToOfferDocumentId)",
			documentLoop,
			StringComparison.Ordinal);
		int documentTerms = documentCheck.IndexOf(
			"ResolveOfferedPeaceTerms(source, acceptance.RespondingToOfferActionId)",
			documentSource,
			StringComparison.Ordinal);
		int documentCessionUnsafe = documentCheck.IndexOf(
			"if (source == null || PeaceTermsContainCession(terms)) return true;",
			documentTerms,
			StringComparison.Ordinal);
		int documentNoCessionSafe = documentCheck.LastIndexOf("return false;", StringComparison.Ordinal);
		Test.True(documentCountSafe >= 0
			&& documentCheck.Contains("NormalizeIntent(x.Intent)", StringComparison.Ordinal)
			&& documentCheck.Contains("\"accept_peace\"", StringComparison.Ordinal)
			&& documentLoop > documentCountSafe
			&& documentSource > documentLoop
			&& documentTerms > documentSource
			&& documentCessionUnsafe > documentTerms
			&& documentNoCessionSafe > documentCessionUnsafe,
			"persisted multi-action peace acceptances must reject any missing or cession-bearing source and otherwise allow several no-cession acceptances");

		string cessionTerms = ExtractMethod(source, "private static bool PeaceTermsContainCession(");
		Test.True(cessionTerms.Contains("terms.CessionFromKingdomId", StringComparison.Ordinal)
			&& cessionTerms.Contains("terms.CessionToKingdomId", StringComparison.Ordinal)
			&& cessionTerms.Contains("terms.CessionSettlementId", StringComparison.Ordinal)
			&& cessionTerms.Contains("||", StringComparison.Ordinal),
			"any populated cession party or settlement field must make a peace source cession-bearing");

		string cessionOptions = ExtractMethod(
			source,
			"private bool HasCessionBoundMultiplePeaceAcceptanceOptions(");
		int collectAcceptingTargets = cessionOptions.IndexOf(
			"HashSet<string> acceptingTargets = new HashSet<string>(legalActionsByTarget",
			StringComparison.Ordinal);
		int singleAcceptanceSafe = cessionOptions.IndexOf(
			"if (acceptingTargets.Count <= 1) return false;",
			collectAcceptingTargets,
			StringComparison.Ordinal);
		int offerScan = cessionOptions.IndexOf(
			"foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers",
			singleAcceptanceSafe,
			StringComparison.Ordinal);
		int cessionFound = cessionOptions.IndexOf(
			"PeaceTermsContainCession(ResolveOfferedPeaceTerms(source, offer.SourceActionId))",
			offerScan,
			StringComparison.Ordinal);
		int noCessionSafe = cessionOptions.LastIndexOf("return false;", StringComparison.Ordinal);
		Test.True(collectAcceptingTargets >= 0
			&& cessionOptions.Contains("x.Value?.Contains(\"accept_peace\"", StringComparison.Ordinal)
			&& cessionOptions.Contains(".Select(x => x.Key)", StringComparison.Ordinal)
			&& singleAcceptanceSafe > collectAcceptingTargets
			&& offerScan > singleAcceptanceSafe
			&& cessionOptions.Contains("offer.Status, \"open\"", StringComparison.Ordinal)
			&& cessionOptions.Contains("offer.TargetKingdomId, author.StringId", StringComparison.Ordinal)
			&& cessionOptions.Contains("acceptingTargets.Contains(offer.ProposerKingdomId", StringComparison.Ordinal)
			&& cessionOptions.Contains("NormalizeIntent(offer.Intent), \"propose_peace\"", StringComparison.Ordinal)
			&& cessionOptions.Contains("documentsById.TryGetValue(offer.SourceDocumentId", StringComparison.Ordinal)
			&& cessionFound > offerScan
			&& noCessionSafe > cessionFound,
			"the relay warning predicate must require at least two uniquely answerable peace targets and one exact open source action containing cession");

		string relayPrompt = ExtractMethod(source, "private string BuildRelayConversationTurnPrompt(");
		int cessionPromptGate = relayPrompt.IndexOf(
			"if (HasCessionBoundMultiplePeaceAcceptanceOptions(round, author, legalActionsByTarget))",
			StringComparison.Ordinal);
		int cessionPromptLine = relayPrompt.IndexOf(
			"本篇最多接受一份",
			cessionPromptGate,
			StringComparison.Ordinal);
		int relaySourceContext = relayPrompt.IndexOf(
			"AppendRelayResponseSourceContext(",
			cessionPromptLine,
			StringComparison.Ordinal);
		Test.True(cessionPromptGate >= 0
			&& cessionPromptLine > cessionPromptGate
			&& relaySourceContext > cessionPromptLine
			&& relayPrompt.Split("本篇最多接受一份", StringSplitOptions.None).Length == 2,
			"relay generation must receive one short pre-generation acceptance limit only when the cession predicate is true");

		string generatedLegality = ExtractMethod(
			source,
			"private bool TryGetGeneratedIntentLegalityViolation(");
		int generatedGuard = generatedLegality.IndexOf(
			"GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(actions)",
			StringComparison.Ordinal);
		int generatedReason = generatedLegality.IndexOf(
			"reason = \"multiple_peace_acceptances_have_cross_terms\"",
			generatedGuard,
			StringComparison.Ordinal);
		Test.True(generatedGuard >= 0
			&& generatedReason > generatedGuard
			&& generatedLegality.IndexOf("return true;", generatedReason, StringComparison.Ordinal) > generatedReason,
			"a generated batch with any cession-bearing peace acceptance must be rejected with the dedicated repair reason");

		string repair = ExtractMethod(source, "private bool EnqueueGeneratedDeclarationRepair(");
		int repairBranch = repair.IndexOf(
			"string.Equals(reason, \"multiple_peace_acceptances_have_cross_terms\"",
			StringComparison.Ordinal);
		int nextRepairBranch = repair.IndexOf("else if (reason.StartsWith(\"offer_response_\"", repairBranch, StringComparison.Ordinal);
		string repairText = repairBranch >= 0 && nextRepairBranch > repairBranch
			? repair.Substring(repairBranch, nextRepairBranch - repairBranch)
			: "";
		Test.True(repairText.Contains("同一篇接受多份和平原案时不得包含割地", StringComparison.Ordinal)
			&& repairText.Contains("只保留一份含割地的接受", StringComparison.Ordinal)
			&& repairText.Contains("其他原案改为明确拒绝或留待下一篇处理", StringComparison.Ordinal),
			"the dedicated repair must tell the model how to reduce a cross-cession multi-accept batch without inventing terms");

		string multiPublication = ExtractMethod(source, "private void ProcessAnalyzedMultiActionDocument(");
		int finalGuard = multiPublication.IndexOf(
			"DocumentHasUnsafeMultiplePeaceAcceptances(document)",
			StringComparison.Ordinal);
		int finalReason = multiPublication.IndexOf(
			"SuppressInvalidDocumentBeforePropagation(document, \"multiple_peace_acceptances_have_cross_terms\")",
			finalGuard,
			StringComparison.Ordinal);
		int publication = multiPublication.IndexOf(
			"document.IsReadyForPublication = true",
			finalReason,
			StringComparison.Ordinal);
		Test.True(finalGuard >= 0
			&& finalReason > finalGuard
			&& multiPublication.IndexOf("return;", finalReason, StringComparison.Ordinal) > finalReason
			&& publication > finalReason,
			"the persisted cession-bearing multi-accept guard must abort before the document becomes publishable");

		string singlePublication = ExtractMethod(source, "private void ProcessAnalyzedDocument(");
		int multiActionDispatch = singlePublication.IndexOf("document?.Actions?.Count > 0", StringComparison.Ordinal);
		int callMultiPublication = singlePublication.IndexOf(
			"ProcessAnalyzedMultiActionDocument(document)",
			multiActionDispatch,
			StringComparison.Ordinal);
		int returnAfterDispatch = singlePublication.IndexOf("return;", callMultiPublication, StringComparison.Ordinal);
		int singleReady = singlePublication.IndexOf("document.IsReadyForPublication = true", returnAfterDispatch, StringComparison.Ordinal);
		Test.True(multiActionDispatch >= 0
			&& callMultiPublication > multiActionDispatch
			&& returnAfterDispatch > callMultiPublication
			&& singleReady > returnAfterDispatch,
			"every persisted multi-action document must enter the guarded multi-action publication path instead of its legacy primary mirror");
	}

	private static void WarResponseNoActionSettlementContract(string source)
	{
		string warAudit = ExtractMethod(
			source,
			"private bool IsWarResponseNoActionAllowed(");
		Test.True(warAudit.Contains("round?.ResultSettlementPending", StringComparison.Ordinal)
			&& warAudit.Contains("round.ResultSettlementCurrentSlotId", StringComparison.Ordinal)
			&& warAudit.Contains("SettlementSlotHasKind(slot, \"war_response\")", StringComparison.Ordinal)
			&& warAudit.Contains("slot.SourceDocumentIds", StringComparison.Ordinal)
			&& warAudit.Contains("war.Actions", StringComparison.Ordinal)
			&& warAudit.Contains("x.ChangedDiplomaticState", StringComparison.Ordinal)
			&& warAudit.Contains("NormalizeIntent(x.Intent)", StringComparison.Ordinal)
			&& warAudit.Contains("war.AuthorKingdomId, target.StringId", StringComparison.Ordinal)
			&& warAudit.Contains("x.TargetKingdomId, author.StringId", StringComparison.Ordinal)
			&& warAudit.Contains("war.Actions == null || war.Actions.Count == 0", StringComparison.Ordinal),
			"a successful-war response statement must retain its exact source audit marker");
		string relayAuthorization = ExtractMethod(
			source,
			"private bool IsNonRootAiRelayNoActionAllowed(");
		Test.True(relayAuthorization.Contains("resultSettlementSlotId", StringComparison.Ordinal)
			&& relayAuthorization.Contains("round.ResultSettlementCurrentSlotId", StringComparison.Ordinal)
			&& relayAuthorization.Contains("author.StringId", StringComparison.Ordinal)
			&& relayAuthorization.Contains("target.StringId", StringComparison.Ordinal)
			&& relayAuthorization.Contains("IsPlayerKingdom(author)", StringComparison.Ordinal),
			"result-settlement statement authorization must bind the exact current AI speaker and target");
		Test.True(relayAuthorization.Contains("bool hasRelatedKingdom", StringComparison.Ordinal)
			&& relayAuthorization.Contains("? slot.RelatedKingdomIds.Contains(target.StringId", StringComparison.Ordinal)
			&& relayAuthorization.Contains(": RoundRouteContainsKingdom(round, target.StringId)", StringComparison.Ordinal),
			"an offer/threat/war obligation slot may statement only toward a related kingdom; only a pure route slot may address another route member");

		string consume = ExtractMethod(
			source,
			"private void ConsumeResultSettlementSpeaker(");
		Test.True(consume.Contains("round.ResultSettlementSlots.Remove(slot)", StringComparison.Ordinal)
			&& !consume.Contains("InvalidateUnserviceableResultSettlementObligations", StringComparison.Ordinal)
			&& !consume.Contains("PendingOffers", StringComparison.Ordinal)
			&& !consume.Contains("DiplomaticThreats", StringComparison.Ordinal),
			"publishing the response may consume its speaking slot but must not invalidate open offer or threat obligations");

		string refresh = ExtractSection(
			source,
			"private void RefreshResultSettlementActionSlots(",
			"private void AddWarResponseResultSettlementSlot(");
		foreach (string remainingObligation in new[]
		{
			"\"offer_response\"",
			"\"threat_response\"",
			"\"threat_followthrough\""
		})
		{
			Test.True(refresh.Contains(remainingObligation, StringComparison.Ordinal),
				"refresh must be able to rebuild an unresolved obligation after the war response: " + remainingObligation);
		}

		string roundProgress = ExtractSection(
			source,
			"private void HandleRoundDocumentProcessed(",
			"private void RetryDeferredRoundProgress(");
		int expireOffers = roundProgress.IndexOf(
			"ExpireUnansweredSettlementOffersForNoActionDeclaration(round, document)",
			StringComparison.Ordinal);
		int consumeSlot = roundProgress.IndexOf("ConsumeResultSettlementSpeaker(round, document)", StringComparison.Ordinal);
		int settlementContinuation = roundProgress.IndexOf("if (round.ResultSettlementPending)", consumeSlot + 1, StringComparison.Ordinal);
		int refreshRemaining = roundProgress.IndexOf("RefreshResultSettlementActionSlots(round)", settlementContinuation, StringComparison.Ordinal);
		int scheduleRemaining = roundProgress.IndexOf("ScheduleNextResultSettlementTurn(round)", refreshRemaining, StringComparison.Ordinal);
		int aiSettlementContinuation = roundProgress.IndexOf("if (round.ResultSettlementPending)", scheduleRemaining + 1, StringComparison.Ordinal);
		int aiRefreshRemaining = roundProgress.IndexOf("RefreshResultSettlementActionSlots(round)", aiSettlementContinuation, StringComparison.Ordinal);
		int aiScheduleRemaining = roundProgress.IndexOf("ScheduleNextResultSettlementTurn(round)", aiRefreshRemaining, StringComparison.Ordinal);
		Test.True(expireOffers >= 0 && consumeSlot > expireOffers
			&& settlementContinuation > consumeSlot
			&& refreshRemaining > settlementContinuation
			&& scheduleRemaining > refreshRemaining
			&& aiSettlementContinuation > scheduleRemaining
			&& aiRefreshRemaining > aiSettlementContinuation
			&& aiScheduleRemaining > aiRefreshRemaining,
			"a published response statement must consume this speaking turn, then refresh and drain remaining obligations");
		string offerExpiry = ExtractMethod(
			source,
			"private void ExpireUnansweredSettlementOffersForNoActionDeclaration(");
		Test.True(offerExpiry.Contains("document.IsRoundResponseNoActionDeclaration", StringComparison.Ordinal)
			&& offerExpiry.Contains("document.ResultSettlementSlotId", StringComparison.Ordinal)
			&& offerExpiry.Contains("slot.SourceDocumentIds", StringComparison.Ordinal)
			&& offerExpiry.Contains("offer.Status = \"expired\"", StringComparison.Ordinal)
			&& !offerExpiry.Contains("offer.Status = \"rejected\"", StringComparison.Ordinal),
			"a no-action answer must expire only its slot-owned open offers so refresh cannot requeue the same speaker");
		Test.True(!roundProgress.Contains("if (document.IsAutonomousNoActionDeclaration)", StringComparison.Ordinal)
			&& !roundProgress.Contains("CloseActiveRound(\"autonomous_no_action_declaration\")", StringComparison.Ordinal),
			"no relay statement may inherit the retired root-level immediate-close shortcut");
		Test.True(!roundProgress.Contains(
				"if (document.IsRoundResponseNoActionDeclaration)\n\t\t{\n\t\t\tround.RoundStatus = \"closed\"",
				StringComparison.Ordinal),
			"a response statement must not close the round while any selected or directed obligation remains");

		string scheduler = ExtractSection(
			source,
			"private void ScheduleNextResultSettlementTurn(",
			"private void HandleRoundDocumentProcessed(");
		int schedulerRefresh = scheduler.IndexOf("RefreshResultSettlementActionSlots(round)", StringComparison.Ordinal);
		int emptyQueue = scheduler.IndexOf("if (slot == null)", schedulerRefresh, StringComparison.Ordinal);
		int close = scheduler.IndexOf("CloseActiveRound(", emptyQueue, StringComparison.Ordinal);
		int bindCurrentSlot = scheduler.IndexOf("round.ResultSettlementCurrentSlotId = slot.SlotId", emptyQueue, StringComparison.Ordinal);
		int preflightLegalActions = scheduler.IndexOf("GetResultSettlementActionableTargets(round, receiver)", bindCurrentSlot, StringComparison.Ordinal);
		Test.True(schedulerRefresh >= 0 && emptyQueue > schedulerRefresh && close > emptyQueue,
			"save/resume scheduling may close only after refreshed offer, threat, and other kingdom slots are empty");
		Test.True(bindCurrentSlot > emptyQueue && preflightLegalActions > bindCurrentSlot,
			"the scheduler must bind the candidate slot before statement-aware legal-action preflight evaluates it");
	}

    private static WorldDiplomacyConfirmedResultKind Evaluate(
        string intent,
        bool changed = false,
        bool hasOffer = false,
        string offerStatus = "",
        string threatStatus = "",
        bool externalFact = false)
    {
        return WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(
            new WorldDiplomacyResultObservation(
                intent,
                changed,
                hasOffer,
                offerStatus,
                threatStatus,
                externalFact));
    }

    private static string FindRepositoryFile(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file.", fileName);
    }

    private static string ExtractSection(string source, string startMarker, string endMarker)
    {
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Test.True(start >= 0, "missing start marker: " + startMarker);
        int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Test.True(end > start, "missing end marker: " + endMarker);
        return source.Substring(start, end - start);
    }

	private static string ExtractMethod(string source, string marker)
	{
		int start = source.IndexOf(marker, StringComparison.Ordinal);
		Test.True(start >= 0, "missing method marker: " + marker);
		int openBrace = source.IndexOf('{', start + marker.Length);
		Test.True(openBrace > start, "missing method body: " + marker);
		int depth = 0;
		for (int index = openBrace; index < source.Length; index++)
		{
			char value = source[index];
			if (value == '{') depth++;
			else if (value == '}' && --depth == 0) return source.Substring(start, index - start + 1);
		}
		throw new InvalidOperationException("unterminated method body: " + marker);
	}
}
