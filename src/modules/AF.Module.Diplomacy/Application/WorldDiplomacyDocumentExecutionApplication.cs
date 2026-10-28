using AnimusForge;
using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Frozen at the synchronous Application entry. Canonical records remain the one
// writable save state; decisions read stable identities and analyzed action values.
internal sealed class WorldDiplomacyDocumentExecutionCommand
{
    internal readonly string DocumentId;
    internal readonly string RoundId;
    internal readonly string SourceDocumentId;
    internal readonly string AuthorKingdomId;
    internal readonly string ResultSettlementSlotId;
    internal readonly bool IsPlayerAuthored;
    internal readonly bool IsRelayTurn;
    internal readonly bool IsExternalResponseOnly;
    internal readonly bool IsRoundResponseNoActionDeclaration;
    internal readonly bool IsWarResponseNoActionDeclaration;
    internal readonly bool WasReadyForPublication;
    private readonly ActionInput[] _actions;
    internal int ActionCount => _actions.Length;
    internal ActionInput ActionAt(int index) => _actions[index];

    internal readonly struct ActionInput
    {
        internal readonly bool Exists;
        internal readonly string ActionId;
        internal readonly string[] DependencyActionIds;
        internal readonly int ConditionVersion;
        internal readonly string ExecutionState;
        internal readonly string TargetKingdomId;
        internal readonly string TargetKingdomName;
        internal readonly string Intent;
        internal readonly string NegotiationMove;
        internal readonly string Commitment;
        internal readonly string RespondingToOfferDocumentId;
        internal readonly string RespondingToOfferActionId;
        internal readonly string RespondingToThreatDocumentId;
        internal readonly string RespondingToThreatActionId;
        internal readonly bool RequiresResponse;
        internal readonly WorldDiplomacyDialogueTerms TreatyTerms;
        internal readonly bool HasPeaceTerms;
        internal readonly string TributePayerKingdomId;
        internal readonly string TributeReceiverKingdomId;
        internal readonly string CessionSettlementId;
        internal readonly string CessionFromKingdomId;
        internal readonly string CessionToKingdomId;
        internal readonly int DailyTribute;
        internal readonly int DurationDays;
        internal readonly string MechanicalResult;
        internal readonly bool ChangedDiplomaticState;
        internal readonly bool HistoryResultRecorded;
        internal ActionInput(WorldDiplomacyDocumentAction action)
        {
            Exists = action != null;
            ActionId = action?.ActionId;
            DependencyActionIds = action?.DependencyActionIds?.ToArray() ?? Array.Empty<string>();
            ConditionVersion = Math.Max(1, action?.ConditionVersion ?? 1);
            ExecutionState = action?.ExecutionState ?? "";
            TargetKingdomId = action?.TargetKingdomId;
            TargetKingdomName = action?.TargetKingdomName;
            Intent = action?.Intent;
            NegotiationMove = action?.NegotiationMove;
            Commitment = action?.Commitment;
            RespondingToOfferDocumentId = action?.RespondingToOfferDocumentId;
            RespondingToOfferActionId = action?.RespondingToOfferActionId;
            RespondingToThreatDocumentId = action?.RespondingToThreatDocumentId;
            RespondingToThreatActionId = action?.RespondingToThreatActionId;
            RequiresResponse = action?.RequiresResponse == true;
            TreatyTerms = action?.TreatyTerms == null ? null : WorldDiplomacyDialogueTerms.From(action.TreatyTerms.ToTerms());
            HasPeaceTerms = action?.PeaceTerms != null;
            TributePayerKingdomId = action?.PeaceTerms?.TributePayerKingdomId;
            TributeReceiverKingdomId = action?.PeaceTerms?.TributeReceiverKingdomId;
            CessionSettlementId = action?.PeaceTerms?.CessionSettlementId;
            CessionFromKingdomId = action?.PeaceTerms?.CessionFromKingdomId;
            CessionToKingdomId = action?.PeaceTerms?.CessionToKingdomId;
            DailyTribute = action?.PeaceTerms?.DailyTribute ?? 0;
            DurationDays = action?.PeaceTerms?.DurationDays ?? 0;
            MechanicalResult = action?.MechanicalResult;
            ChangedDiplomaticState = action?.ChangedDiplomaticState == true;
            HistoryResultRecorded = action?.HistoryResultRecorded == true;
        }

        internal WorldDiplomacyDocumentAction Materialize() => !Exists ? null : new WorldDiplomacyDocumentAction
        {
            ActionId = ActionId,
            DependencyActionIds = new List<string>(DependencyActionIds),
            ConditionVersion = ConditionVersion,
            ExecutionState = ExecutionState,
            TargetKingdomId = TargetKingdomId,
            TargetKingdomName = TargetKingdomName,
            Intent = Intent,
            NegotiationMove = NegotiationMove,
            Commitment = Commitment,
            RespondingToOfferDocumentId = RespondingToOfferDocumentId,
            RespondingToOfferActionId = RespondingToOfferActionId,
            RespondingToThreatDocumentId = RespondingToThreatDocumentId,
            RespondingToThreatActionId = RespondingToThreatActionId,
            RequiresResponse = RequiresResponse,
            TreatyTerms = TreatyTerms == null ? null : WorldDiplomacyDialogueTerms.From(TreatyTerms.ToTerms()),
            PeaceTerms = !HasPeaceTerms ? null : new WorldDiplomacyPeaceTerms
            {
                TributePayerKingdomId = TributePayerKingdomId,
                TributeReceiverKingdomId = TributeReceiverKingdomId,
                CessionSettlementId = CessionSettlementId,
                CessionFromKingdomId = CessionFromKingdomId,
                CessionToKingdomId = CessionToKingdomId,
                DailyTribute = DailyTribute,
                DurationDays = DurationDays
            },
            MechanicalResult = MechanicalResult,
            ChangedDiplomaticState = ChangedDiplomaticState,
            HistoryResultRecorded = HistoryResultRecorded
        };
    }

    internal List<WorldDiplomacyDocumentAction> MaterializeActions()
    {
        var actions = new List<WorldDiplomacyDocumentAction>(_actions.Length);
        for (int index = 0; index < _actions.Length; index++) actions.Add(_actions[index].Materialize());
        return actions;
    }

    internal WorldDiplomacyDocumentExecutionCommand(WorldDiplomacyDocument document,
        IReadOnlyList<WorldDiplomacyDocumentAction> actions)
    {
        DocumentId = document.DocumentId;
        RoundId = document.RoundId;
        SourceDocumentId = document.SourceDocumentId;
        AuthorKingdomId = document.AuthorKingdomId;
        ResultSettlementSlotId = document.ResultSettlementSlotId;
        IsPlayerAuthored = document.IsPlayerAuthored;
        IsRelayTurn = document.IsRelayTurn;
        IsExternalResponseOnly = document.IsExternalResponseOnly;
        IsRoundResponseNoActionDeclaration = document.IsRoundResponseNoActionDeclaration;
        IsWarResponseNoActionDeclaration = document.IsWarResponseNoActionDeclaration;
        WasReadyForPublication = document.IsReadyForPublication;
        _actions = new ActionInput[actions.Count];
        for (int index = 0; index < actions.Count; index++) _actions[index] = new ActionInput(actions[index]);
    }
}

// Main-thread analyzed-document validation and ordered execution over canonical records.
internal static class WorldDiplomacyDocumentExecutionApplication
{
    internal static void ProcessAnalyzedDocument(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument document,
        string intent, string commitment, bool requiresResponse, string tone, float confidence)
    {
        ProcessAnalyzedDocument(port, orchestration, document, intent, commitment,
            requiresResponse, tone, confidence, out _);
    }

    // An optional synchronous receipt sink for complete Application-level replays.
    internal static void ProcessAnalyzedDocument(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument document,
        string intent, string commitment, bool requiresResponse, string tone, float confidence,
        out List<WorldDiplomacyDocumentActionReceipt> receipts)
    {
        receipts = new List<WorldDiplomacyDocumentActionReceipt>();
        if (document == null) return;
        bool legacy = document.Actions == null || document.Actions.Count == 0;
        // Transient normalization preserves the old save shape and action/source identities.
        IReadOnlyList<WorldDiplomacyDocumentAction> actions = legacy
            ? new[] { new WorldDiplomacyDocumentAction {
                TargetKingdomId = document.TargetKingdomId, TargetKingdomName = document.TargetKingdomName,
                Intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent), Commitment = commitment,
                NegotiationMove = document.NegotiationMove, RequiresResponse = requiresResponse,
                TreatyTerms = document.TreatyTerms, PeaceTerms = document.PeaceTerms, RespondingToOfferDocumentId = document.RespondingToOfferDocumentId,
                RespondingToOfferActionId = document.RespondingToOfferActionId,
                RespondingToThreatDocumentId = document.RespondingToThreatDocumentId,
                RespondingToThreatActionId = document.RespondingToThreatActionId } }
            : document.Actions;
        // Reject malformed or oversized saved action lists before copying values.
        if (actions.Count < 1 || actions.Count > 64) return;
        var command = new WorldDiplomacyDocumentExecutionCommand(document, actions);
        // Keep the saved record, but detach its input action objects from callbacks.
        // The separate validation view never exposes mutable canonical actions.
        List<WorldDiplomacyDocumentAction> frozenActions = command.MaterializeActions();
        if (!legacy) document.Actions = frozenActions;
        ExecuteItems(port, orchestration, document, command, legacy, receipts);
    }

    private static void ExecuteItems(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument document,
        WorldDiplomacyDocumentExecutionCommand command, bool legacy,
        List<WorldDiplomacyDocumentActionReceipt> receipts)
    {
        var conditionalDeferrals = DiplomacyConditionalExecutionRules.DeferredActions(command.MaterializeActions(), port.ResolveDocument);
        if (command.MaterializeActions().Any(x => x.Intent == "declare_war" && conditionalDeferrals.Contains(x.ActionId)))
        {
            orchestration.SuppressInvalidDocumentBeforePropagation(document, "conditional_war_requires_prepublication_repair");
            return;
        }
        string author = port.ResolveKingdomId(command.AuthorKingdomId);
        if (command.ActionCount < 1 || command.ActionCount > 64 || author == null) return;
        if (!WorldDiplomacyAuthorityRules.HasIndependentAuthority(port.CaptureAuthority(author)))
        { orchestration.SuppressInvalidDocumentBeforePropagation(document, "author_has_no_diplomatic_authority"); return; }
        if (!command.IsPlayerAuthored && !WorldDiplomacyAuthorityRules.CanAiAuthor(port.CaptureAuthority(author), out string authorReason))
        { orchestration.SuppressInvalidDocumentBeforePropagation(document, authorReason); return; }
        if (command.IsPlayerAuthored && !port.IsPlayerKingdom(author))
        { orchestration.SuppressInvalidDocumentBeforePropagation(document, "player_is_not_reigning_ruler"); return; }
        string sourceContextDocumentId = command.SourceDocumentId ?? "";
        WorldDiplomacyRound round = port.ResolveRound(command.RoundId);
        if (round?.EventSystemVersion > 0 && !command.IsPlayerAuthored
            && !round.Participants.Any(x => x.KingdomId == author && (x.State == "active"
                || !string.IsNullOrEmpty(x.PendingParticipationSourceId) && x.PendingParticipationSourceId == sourceContextDocumentId))
            && !round.PlayerResponses.Any(x => x.KingdomId == author && x.Status == "pending"
                && (document.AnsweredPlayerDocumentIds.Contains(x.SourceDocumentId)
                    || x.SourceDocumentId == sourceContextDocumentId))
            && !(orchestration is WorldDiplomacyOrchestration endingOwner && endingOwner.CanPublishBoundEndingDecision(round, document)))
        { orchestration.SuppressInvalidDocumentBeforePropagation(document, "author_is_not_event_participant"); return; }
        orchestration.PruneInvalidOffers(round);
        var targets = new List<string>(command.ActionCount);
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < command.ActionCount; index++)
        {
            var input = command.ActionAt(index);
            string target = port.ResolveKingdomId(input.TargetKingdomId);
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(input.Intent);
            if (!input.Exists || !WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent)
                || !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, input.Commitment)
                || !identities.Add((target ?? "") + ":" + intent))
            { orchestration.SuppressInvalidDocumentBeforePropagation(document, "invalid_or_duplicate_action"); return; }
            bool hasEffect = WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent);
            bool subjectRelease = intent == "release_subject" && orchestration is WorldDiplomacyOrchestration releaseOwner
                && releaseOwner.ValidatePlayerSubjectRelease(document, author, target, out _);
            if (hasEffect && (target == null || target == author || port.IsEliminated(target)
                || (!subjectRelease && !WorldDiplomacyAuthorityRules.HasIndependentAuthority(port.CaptureAuthority(target)))))
            { orchestration.SuppressInvalidDocumentBeforePropagation(document, "action_has_no_eligible_target"); return; }
            if (!legacy) WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, input.Materialize());
            if (orchestration is WorldDiplomacyOrchestration owner
                && !owner.ValidateFormalTreatyDeclaration(document, intent, input.Materialize().TreatyTerms,
                    author, target, input.RespondingToOfferDocumentId, input.RespondingToOfferActionId, out string treatyReason))
            { orchestration.SuppressInvalidDocumentBeforePropagation(document, treatyReason); return; }
            if (!conditionalDeferrals.Contains(input.ActionId)
                && orchestration.TryGetPlayerWorldStateIntentViolation(document, intent, input.Commitment, author, target, out string actionReason))
            { orchestration.SuppressInvalidDocumentBeforePropagation(document, "action_not_executable:" + actionReason); return; }
            targets.Add(target);
        }
        if (!legacy && WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(
            new WorldDiplomacyDocument { Actions = command.MaterializeActions() }, port.ResolveDocument))
        { orchestration.SuppressInvalidDocumentBeforePropagation(document, "multiple_peace_acceptances_have_cross_terms"); return; }
        bool legacyPublic = legacy && !WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(command.ActionAt(0).Intent);

		document.IsReadyForPublication = true;
		List<string> inputTargets = null;
        if (!legacy)
        {
            inputTargets = new List<string>(command.ActionCount);
            for (int index = 0; index < command.ActionCount; index++) inputTargets.Add(command.ActionAt(index).TargetKingdomId);
        }
		List<string> allAddressed = legacy ? null : port.NormalizeKingdomIdList(inputTargets, author);
		List<WorldDiplomacyDocumentAction> resultActions = legacy ? null : new List<WorldDiplomacyDocumentAction>(command.ActionCount);
		for (int index = 0; index < command.ActionCount; index++)
		{
			var input = command.ActionAt(index);
			string target = targets[index];
            if (!legacy)
            {
                // Callbacks may replace or mutate the saved list. Rebuild the bounded
                // effect view from private inputs and completed results each time.
                document.Actions = command.MaterializeActions();
                for (int prior = 0; prior < resultActions.Count; prior++)
                    document.Actions[prior] = new WorldDiplomacyDocumentExecutionCommand.ActionInput(resultActions[prior]).Materialize();
            }
            if (!legacy) WorldDiplomacyDocumentApplication.BeginAction(document, input.Materialize(), target);
			bool noAction = string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(input.Intent), "statement", StringComparison.OrdinalIgnoreCase);
			bool effectAttempted = false;
			bool outcomeKnown = true;
			try
			{
                if (conditionalDeferrals.Contains(input.ActionId) && !WorldDiplomacyIntentVocabulary.IsProposalIntent(input.Intent))
                {
                    document.MechanicalResult = DiplomacyPromptTexts.Get("PlayerConditionalDeferred", "The dependent exchange has not taken effect. Its prerequisites and safe coordinated execution must be settled before any part is fulfilled; independent agreed matters remain eligible.");
                    document.ChangedDiplomaticState = false;
                }
                else if (!legacy && !noAction && orchestration.TryGetPlayerWorldStateIntentViolation(document,
                    input.Intent, input.Commitment, author, target, out string executionBlockReason))
				{
					document.MechanicalResult = DiplomacyMiscPromptTexts.Get("Analysis_MechanicNotExecutedPrefix", "Diplomatic action not executed: ")
                        + WorldDiplomacyAnalysisApplication.DescribeRejectedPlayerMechanic(executionBlockReason);
					port.Log("multi-target diplomatic action became invalid during batch execution document="
						+ command.DocumentId + " action=" + input.ActionId + " reason=" + executionBlockReason);
				}
				else if (!noAction || legacyPublic)
				{
					effectAttempted = true;
                    if (!legacyPublic)
                    {
					if (WorldDiplomacyIntentVocabulary.IsImmediateIntent(input.Intent))
                        outcomeKnown &= orchestration.ExecuteImmediateIntent(author, target,
                            WorldDiplomacyIntentVocabulary.NormalizeIntent(input.Intent), document).Known;
					orchestration.ProcessDiplomaticThreatDocument(document, author, target, recordTargetDecisions: legacy);
					outcomeKnown &= orchestration.TrySettleRelayOffer(document) != WorldDiplomacyOfferOutcome.Unknown;
                    }
				}

			}
			catch (Exception ex)
			{
				outcomeKnown = false;
				if (!legacyPublic && string.IsNullOrWhiteSpace(document.MechanicalResult))
				{
					document.MechanicalResult = DiplomacyMiscPromptTexts.Get("Analysis_MechanismNotExecutedPrefix", "Diplomatic mechanism not executed: ") + WorldDiplomacyTextRules.Limit(ex.Message, 180);
				}
				port.Log("multi-target diplomatic action failed without discarding declaration document=" + command.DocumentId
					+ " action=" + input.ActionId + " intent=" + input.Intent + " error=" + ex.Message);
			}
			var receipt = new WorldDiplomacyDocumentActionReceipt(input.ActionId, target,
				effectAttempted, outcomeKnown, document.ChangedDiplomaticState, document.MechanicalResult);
			receipts.Add(receipt);
			if (!legacy)
            {
                WorldDiplomacyDocumentAction resultAction = input.Materialize();
                resultAction.ExecutionState = !outcomeKnown ? "uncertain" : receipt.Applied ? "applied"
                    : effectAttempted ? "not_applied" : "not_attempted";
                if (conditionalDeferrals.Contains(input.ActionId) && !effectAttempted) resultAction.ExecutionState = "deferred_conditions";
                WorldDiplomacyDocumentApplication.CaptureActionResult(document, resultAction, receipt);
                // CaptureActionResult copies peace terms from the mutable document.
                // Detach those terms before the next game-action callback runs.
                resultActions.Add(new WorldDiplomacyDocumentExecutionCommand.ActionInput(resultAction).Materialize());
            }
		}
        if (legacy)
        {
            // Compatibility publication policy: old flat records propagate before history.
            FinalizePublishedDocumentAfterAnalysis(port, orchestration, document, author, targets[0], command.ActionAt(0).Intent,
                legacyPublic || command.ActionAt(0).Intent == "statement");
            return;
        }
		document.Actions = resultActions;
		WorldDiplomacyDocumentApplication.SealActions(document, allAddressed, sourceContextDocumentId, receipts);
        if (orchestration is WorldDiplomacyOrchestration eventOwner)
        {
            if (!eventOwner.FinalizeSilenceBeforePublication(document)) return;
            eventOwner.SettleThreatDeclaration(document);
        }
		try
		{
			orchestration.AppendCanonicalDocumentEvents(document);
			WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatHistoryAfterDocument(document,
			orchestration.Threats(), orchestration.TryAppendDiplomaticThreatHistoryResult,
			orchestration.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult,
			orchestration.TryAppendDiplomaticThreatIssuerRewardHistoryResult);
			WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document,
			orchestration.Threats(), orchestration.TryAppendDiplomaticThreatNonComplianceHistoryResult);
		}
		catch (Exception ex)
		{
			orchestration.ScheduleDeferredCanonicalHistoryRetry(document.DocumentId);
			port.Log("canonical history append deferred document=" + document.DocumentId + " error=" + ex.Message);
		}
		try { orchestration.StartDocumentPropagation(document, author); }
		catch (Exception ex) { port.Log("valid multi-target declaration propagation failed document=" + document.DocumentId + " error=" + ex.Message); }
		try { orchestration.HandleRoundDocumentProcessed(document); }
		catch (Exception ex) { port.Log("valid multi-target declaration round progress deferred document=" + document.DocumentId + " error=" + ex.Message); }
	}




	internal static void FinalizePublishedDocumentAfterAnalysis(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyDocument document,
		string author,
		string target,
		string normalizedIntent,
		bool recordNoActionDecision)
	{
        if (orchestration is WorldDiplomacyOrchestration owner && !owner.FinalizeSilenceBeforePublication(document)) return;
		(orchestration as WorldDiplomacyOrchestration)?.SettleThreatDeclaration(document);
		WorldDiplomacyDocumentPublicationApplication.FinalizePublishedDocumentAfterAnalysis(
			document,
			author,
			target,
			normalizedIntent,
			recordNoActionDecision,
			port.Threats,

			orchestration.StartDocumentPropagation,
			orchestration.RecordDiplomacyWeeklyMaterial,
			orchestration.ReconcileReachedCourts,
			orchestration.AppendCanonicalDocumentEvents,
			orchestration.TryAppendDiplomaticThreatHistoryResult,
			orchestration.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult,
			orchestration.TryAppendDiplomaticThreatIssuerRewardHistoryResult,
			orchestration.TryAppendDiplomaticThreatNonComplianceHistoryResult,
			orchestration.ScheduleDeferredCanonicalHistoryRetry,
			orchestration.HandleRoundDocumentProcessed,
			port.Log);
	}


}
