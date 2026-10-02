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
                PeaceTerms = document.PeaceTerms, RespondingToOfferDocumentId = document.RespondingToOfferDocumentId,
                RespondingToOfferActionId = document.RespondingToOfferActionId,
                RespondingToThreatDocumentId = document.RespondingToThreatDocumentId,
                RespondingToThreatActionId = document.RespondingToThreatActionId } }
            : document.Actions;
        // Reject malformed or oversized saved action lists before copying values.
        if (actions.Count < 1 || actions.Count > port.MaxDiplomaticActionsPerDocument) return;
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
        string author = port.ResolveKingdomId(command.AuthorKingdomId);
        if (command.ActionCount < 1 || command.ActionCount > port.MaxDiplomaticActionsPerDocument || author == null) return;
		if (!command.IsPlayerAuthored && !WorldDiplomacyAuthorityRules.HasIndependentAuthority(port.CaptureAuthority(author)))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "controlled_vassal_has_no_diplomatic_authority");
			return;
		}
		if (!command.IsPlayerAuthored && !WorldDiplomacyAuthorityRules.CanAiAuthor(port.CaptureAuthority(author), out string authorBlockReason))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, authorBlockReason);
			return;
		}
		string sourceContextDocumentId = command.SourceDocumentId ?? "";
        WorldDiplomacyRound round = port.ResolveRound(command.RoundId);
        orchestration.PruneInvalidOffers(round);
        WorldDiplomacyDocument validationDocument = legacy ? document : new WorldDiplomacyDocument
        {
            Actions = command.MaterializeActions()
        };
		List<string> targets = new List<string>(command.ActionCount);
		HashSet<string> uniqueTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> newSettlementTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int statementCount = 0;
		for (int index = 0; index < command.ActionCount; index++)
		{
			var input = command.ActionAt(index);
			string target = port.ResolveKingdomId(input.TargetKingdomId);
			string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(input.Intent);
            bool publicStatement = legacy && command.IsPlayerAuthored
                && WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(intent)
                && !WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent);
            bool claimedNoAction = command.IsRoundResponseNoActionDeclaration || command.IsWarResponseNoActionDeclaration;
            bool noAction = intent == "statement";
            if (noAction) statementCount++;
            bool allowedNoAction = noAction && !command.IsPlayerAuthored && claimedNoAction
                && orchestration.IsNonRootAiRelayNoActionAllowed(round, command.ResultSettlementSlotId,
                    author, target, command.IsRelayTurn, command.IsExternalResponseOnly,
                    port.ResolveDocument(command.SourceDocumentId));
            if (legacy && claimedNoAction && !allowedNoAction)
            {
                orchestration.SuppressInvalidDocumentBeforePropagation(document, "stale_round_response_no_action_declaration");
                return;
            }
            if ((!WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent) && !allowedNoAction && !publicStatement)
                || (!legacy && !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, input.Commitment)))
            {
                orchestration.SuppressInvalidDocumentBeforePropagation(document, legacy ? "non_actionable_diplomatic_intent" : "multi_action_is_not_executable");
                if (legacy && command.IsPlayerAuthored && !command.WasReadyForPublication)
                    port.Notify("外交宣言没有发布：正文必须明确包含一项可执行的外交动作。");
                return;
            }
            if (publicStatement) { targets.Add(target); continue; }
            if (!input.Exists || target == null || target == author || port.IsEliminated(target)
                || !WorldDiplomacyAuthorityRules.HasIndependentAuthority(port.CaptureAuthority(target)) || !uniqueTargets.Add(target))
            {
                orchestration.SuppressInvalidDocumentBeforePropagation(document, legacy
                    ? "final_live_state_guard:diplomatic_action_has_no_live_target" : "multi_action_has_invalid_or_duplicate_target");
                if (legacy && command.IsPlayerAuthored && !command.WasReadyForPublication)
                    port.Notify("外交宣言没有发布：正文中的外交动作与当前真实状态不相容。");
                return;
            }
			List<string> finalLiveIntents = command.IsPlayerAuthored
				? orchestration.BuildLegalDiplomaticActionIntents(round, author, target)
				: orchestration.BuildLegalDiplomaticDeclarationIntents(
					round,
					author,
					target,
					command.IsRelayTurn,
					command.ResultSettlementSlotId,
					command.IsExternalResponseOnly,
					port.ResolveDocument(command.SourceDocumentId));
			if (!finalLiveIntents.Contains(intent, StringComparer.OrdinalIgnoreCase))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "final_live_legal_action_guard");
				return;
			}
			if (round?.ResultSettlementPending == true && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, target))
			{
				if (!CanUseResultSettlementTarget(port, round, author, target))
				{
					orchestration.SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
                    if (legacy && command.IsPlayerAuthored && !command.WasReadyForPublication)
                        port.Notify("外交宣言没有发布：本次外交事件已无法再加入新的处理国。");
					return;
				}
				newSettlementTargets.Add(target);
			}
			else if (!legacy && command.IsRelayTurn && round != null && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, target))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "kingdom_not_in_relay_route");
				return;
			}
			if (orchestration.TryGetDiplomaticStateViolation(intent, author, target, out string liveStateReason))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "final_live_state_guard:" + liveStateReason);
                if (legacy && command.IsPlayerAuthored && !command.WasReadyForPublication)
                    port.Notify("外交宣言没有发布：正文中的外交动作与当前真实状态不相容。");
				return;
			}
			if (intent == "propose_peace"
				&& WorldDiplomacyRoundLifecycleRules.IsImmediateWarResponsePeaceSuppressed(round, command.ResultSettlementSlotId,
				author, target, port.ResolveDocument))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "immediate_war_response_peace_suppressed");
				return;
			}
            if (legacy && command.IsPlayerAuthored
                && orchestration.TryGetPlayerWorldStateIntentViolation(document, intent, input.Commitment, author, target, out string playerActionBlockReason))
            {
                orchestration.SuppressInvalidDocumentBeforePropagation(document, "player_action_not_executable:" + playerActionBlockReason);
                if (!command.WasReadyForPublication) port.Notify("外交宣言没有发布：正文中的外交动作与当前真实状态不相容。");
                return;
            }
            if (!legacy) WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, input.Materialize());
			string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
			if (!command.IsPlayerAuthored && !string.IsNullOrWhiteSpace(proposalIntent)
				&& !WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(document, author, target, proposalIntent, port.ResolveRound))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "offer_ownership_guard");
				return;
			}
			if (!command.IsPlayerAuthored && WorldDiplomacyIntentVocabulary.IsPeaceIntent(intent)
				&& !port.IsAtWar(author, target))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "peace_legality_guard");
				return;
			}
			targets.Add(target);
		}
		if (statementCount > 0 && command.ActionCount != 1)
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "statement_must_be_the_only_diplomatic_action");
			return;
		}
        bool legacyPublic = legacy && command.IsPlayerAuthored
            && !WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(command.ActionAt(0).Intent);
        if (!legacyPublic)
        {
		WorldDiplomacyRoundOffer requiredPeaceOffer = port.FindRequiredPeaceOfferResponse(
			round,
			author,
			command.ResultSettlementSlotId,
			command.IsExternalResponseOnly,
			command.SourceDocumentId,
			requireAnyOpenPeaceOffer: command.IsRelayTurn || command.IsPlayerAuthored);
		if (!WorldDiplomacyDocumentFactRules.DocumentContainsRequiredPeaceOfferResponse(validationDocument, requiredPeaceOffer))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "required_peace_offer_response_missing");
			if (command.IsPlayerAuthored && (!legacy || !command.WasReadyForPublication))
			{
				port.Notify("外交宣言没有发布：本篇必须先接受或拒绝当前和平原案。");
			}
			return;
		}
		if (!legacy && WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(validationDocument, port.ResolveDocument))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "multiple_peace_acceptances_have_cross_terms");
			return;
		}
		if (round?.ResultSettlementPending == true
			&& (round.RelayRouteKingdomIds?.Count ?? 0) + newSettlementTargets.Count > port.MaxRelayParticipants)
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
			return;
		}
		foreach (string targetId in newSettlementTargets.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			if (!TryIncludeResultSettlementTarget(port, round, targetId))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
				return;
			}
			WorldDiplomacyResultSlotApplication.AddOrMergeResultSettlementSlot(round, targetId, "route", document.DocumentId, author, prioritize: false, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		}

        }

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
				if (!legacy && !noAction && orchestration.TryGetDiplomaticStateViolation(input.Intent, author, target, out string executionBlockReason))
				{
					document.MechanicalResult = "外交动作未执行：" + executionBlockReason;
					port.Log("multi-target diplomatic action became invalid during batch execution document="
						+ command.DocumentId + " action=" + input.ActionId + " reason=" + executionBlockReason);
				}
				else if (!noAction || legacyPublic)
				{
					effectAttempted = true;
					WorldDiplomacyThreatApplication.ApplyDocumentPressure(document, port.FindWarPressure, port.NormalizeKingdomIdList, port.AddWarPressure);
                    if (!legacyPublic)
                    {
					if (WorldDiplomacyIntentVocabulary.IsImmediateIntent(input.Intent))
                        outcomeKnown &= orchestration.ExecuteImmediateIntent(author, target,
                            WorldDiplomacyIntentVocabulary.NormalizeIntent(input.Intent), document).Known;
					orchestration.ProcessDiplomaticThreatDocument(document, author, target, recordTargetDecisions: legacy);
					outcomeKnown &= orchestration.TrySettleRelayOffer(document) != WorldDiplomacyOfferOutcome.Unknown;
                    }
					orchestration.ApplyDiplomaticPressureEffect(document);
				}
            if (legacy && noAction && !legacyPublic)
                orchestration.RecordDiplomaticThreatTargetDecisions(document, author, target, input.Intent);

			}
			catch (Exception ex)
			{
				outcomeKnown = false;
				if (!legacyPublic && string.IsNullOrWhiteSpace(document.MechanicalResult))
				{
					document.MechanicalResult = "外交机制未执行：" + WorldDiplomacyTextRules.Limit(ex.Message, 180);
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
		orchestration.RecordDiplomaticThreatTargetDecisionsForActions(document, author);
		bool requiredThreatActionDeferred = orchestration.DeferUnresolvedRequiredThreatAction(
			document,
			author,
			targets[0],
			command.ActionAt(0).Intent);
		if (!requiredThreatActionDeferred) WorldDiplomacyThreatApplication.SettleDiplomaticThreatFollowThroughAfterDeclaration(
			document, orchestration.Threats(), author,
			(threat, doc) => orchestration.ApplyDiplomaticThreatReputationPenalty(threat, doc));

		WorldDiplomacyDocumentApplication.SealActions(document, allAddressed, sourceContextDocumentId, receipts);
		orchestration.SettleInternationalReputationForDocument(document);
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
	internal static bool TryIncludeResultSettlementTarget(IWorldDiplomacyDocumentExecutionPort port, WorldDiplomacyRound round, string kingdomId)
	{
		if (round == null) return false;
		round.RelayRouteKingdomIds ??= new List<string>();
		bool alreadyOnRoute = round.RelayRouteKingdomIds.Contains(kingdomId, StringComparer.OrdinalIgnoreCase);
		string kingdom = alreadyOnRoute ? null : port.ResolveKingdomId(kingdomId);
		switch (WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission(
			round.ResultSettlementPending,
			!string.IsNullOrWhiteSpace(kingdomId),
			alreadyOnRoute,
			kingdom != null && !port.IsEliminated(kingdom) && WorldDiplomacyAuthorityRules.HasIndependentAuthority(port.CaptureAuthority(kingdom)),
			round.RelayRouteKingdomIds.Count,
			port.MaxRelayParticipants))
		{
			case WorldDiplomacyRouteAdmission.AlreadyOnRoute:
				return true;
			case WorldDiplomacyRouteAdmission.Denied:
				return false;
		}
		round.RelayRouteKingdomIds.Add(kingdomId);
		round.HardEndDay = WorldDiplomacyRoundLifecycleRules.ExtendHardEndDay(round.HardEndDay, port.CurrentDay, 3);
		WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, kingdomId, "active", mandatoryReply: false);
		participant.SelectedForRelay = true;
		participant.IsPlayerAsync = port.IsPlayerKingdom(kingdom);
		port.Log("round result settlement participant appended round=" + round.RoundId + " kingdom=" + kingdomId);
		WorldDiplomacyResultSlotApplication.AddOrMergeResultSettlementSlot(round, kingdomId, "route",
			round.ResultSettlementTriggerDocumentId, "", prioritize: false, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		return true;
	}

    private static bool CanUseResultSettlementTarget(IWorldDiplomacyDocumentExecutionPort port, WorldDiplomacyRound round, string author, string target)
    {
        if (round == null || author == null) return false;
        return WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(round.ResultSettlementPending, target != null, target == author,
            target != null && port.IsEliminated(target), target != null && WorldDiplomacyAuthorityRules.HasIndependentAuthority(port.CaptureAuthority(target)),
            WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, target), round.RelayRouteKingdomIds?.Count ?? 0, port.MaxRelayParticipants);
    }

	internal static void FinalizePublishedDocumentAfterAnalysis(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyDocument document,
		string author,
		string target,
		string normalizedIntent,
		bool recordNoActionDecision)
	{
		WorldDiplomacyDocumentPublicationApplication.FinalizePublishedDocumentAfterAnalysis(
			document,
			author,
			target,
			normalizedIntent,
			recordNoActionDecision,
			port.Threats,
			orchestration.RecordDiplomaticThreatTargetDecisions,
			orchestration.DeferUnresolvedRequiredThreatAction,
			orchestration.ApplyDiplomaticThreatReputationPenalty,
			orchestration.SettleInternationalReputationForDocument,
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

    internal static void RefreshResultSettlementActionSlots(IWorldDiplomacyDocumentExecutionPort port, WorldDiplomacyStorage storage, WorldDiplomacyRound round, Action<WorldDiplomacyRound> pruneInvalidOffers)
	{
		if (round == null || !round.ResultSettlementPending || !round.RelayPlanned) return;
		WorldDiplomacyResultSlotApplication.InitializeResultSettlementRouteSlots(round, storage?.Documents, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		pruneInvalidOffers?.Invoke(round);
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsOfferOfStatus(x, "open")))
		{
			string target = port.ResolveKingdomId(offer.TargetKingdomId);
			if (target == null || !WorldDiplomacyAuthorityRules.HasIndependentAuthority(port.CaptureAuthority(target))
				|| !TryIncludeResultSettlementTarget(port, round, target))
			{
				offer.Status = "invalidated";
				continue;
			}
			WorldDiplomacyResultSlotApplication.AddOrMergeResultSettlementSlot(round, target, "offer_response",
				offer.SourceDocumentId, offer.ProposerKingdomId, prioritize: true, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		}
		foreach (WorldDiplomacyThreat threat in (storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(x, round?.RoundId)))
		{
			WorldDiplomacyThreatSettlementSlotDecision threatSlot =
				WorldDiplomacyRoundLifecycleRules.EvaluateThreatSettlementSlot(threat);
			if (!threatSlot.Applies) continue;
			WorldDiplomacyResultSlotApplication.AddOrMergeResultSettlementSlot(round, threatSlot.KingdomId, threatSlot.Kind,
				threatSlot.SourceDocumentId, threatSlot.RelatedKingdomId, prioritize: true, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		}
	}
}
