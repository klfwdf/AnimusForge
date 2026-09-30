using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Main-thread analyzed-document validation and ordered execution over canonical records.
internal static class WorldDiplomacyDocumentExecutionApplication
{
	internal static void ProcessAnalyzedDocument(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyDocument document,
		string intent,
		string commitment,
		bool requiresResponse,
		string tone,
		float confidence)
	{
		if (document?.Actions?.Count > 0)
		{
			ProcessAnalyzedMultiActionDocument(port, orchestration, document);
			return;
		}
		string author = port.ResolveKingdomId(document.AuthorKingdomId);
		string target = port.ResolveKingdomId(document.TargetKingdomId);
		if (author == null)
		{
			return;
		}
		if (!document.IsPlayerAuthored && !port.HasIndependentWorldDiplomacyAuthority(author))
		{
			port.Log("controlled vassal document blocked before propagation document=" + document.DocumentId
				+ " author=" + author);
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "controlled_vassal_has_no_diplomatic_authority");
			return;
		}
		if (!document.IsPlayerAuthored && !port.CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			port.Log("AI document blocked before propagation document=" + document.DocumentId + " author=" + author
				+ " reason=" + authorBlockReason);
			orchestration.SuppressInvalidDocumentBeforePropagation(document, authorBlockReason);
			return;
		}
		string normalizedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
		WorldDiplomacyRound owningRound = port.ResolveRound(document.RoundId);
		orchestration.PruneInvalidOffers(owningRound);
		bool claimedRoundResponseNoAction = document.IsRoundResponseNoActionDeclaration
			|| document.IsWarResponseNoActionDeclaration;
		bool allowedRoundResponseNoAction = !document.IsPlayerAuthored
			&& claimedRoundResponseNoAction
			&& string.Equals(normalizedIntent, "statement", StringComparison.OrdinalIgnoreCase)
			&& orchestration.IsNonRootAiRelayNoActionAllowed(
				owningRound,
				document.ResultSettlementSlotId,
				author,
				target,
				document.IsRelayTurn,
				document.IsExternalResponseOnly,
				port.ResolveDocument(document.SourceDocumentId));
		if (claimedRoundResponseNoAction && !allowedRoundResponseNoAction)
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "stale_round_response_no_action_declaration");
			return;
		}
		bool allowedPlayerPublicIntent = document.IsPlayerAuthored
			&& WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent(normalizedIntent)
			&& !WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(normalizedIntent);
		bool allowedNoAction = allowedRoundResponseNoAction || allowedPlayerPublicIntent;
		if (!WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(normalizedIntent) && !allowedNoAction)
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "non_actionable_diplomatic_intent");
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				port.Notify("外交宣言没有发布：正文必须明确包含一项可执行的外交动作。");
			}
			return;
		}
		if (allowedPlayerPublicIntent)
		{
			document.IsReadyForPublication = true;
			try
			{
				WorldDiplomacyWarPressureRules.ApplyDocumentPressure(document, port.FindWarPressure, port.NormalizeKingdomIdList, port.AddWarPressure);
				orchestration.ApplyDiplomaticPressureEffect(document);
			}
			catch (Exception ex)
			{
				port.Log("player public-statement effect failed without hiding declaration document="
					+ document.DocumentId + " intent=" + normalizedIntent + " error=" + ex.Message);
			}
			FinalizePublishedDocumentAfterAnalysis(port, orchestration, document, author, target, normalizedIntent, recordNoActionDecision: true);
			return;
		}
		if (owningRound?.ResultSettlementPending == true
			&& target != null
			&& !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(owningRound, target)
			&& !CanUseResultSettlementTarget(port, owningRound, author, target))
		{
			port.Log("result-settlement document target blocked because participant expansion is unavailable document=" + document.DocumentId
				+ " author=" + author + " target=" + target);
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				port.Notify("外交宣言没有发布：本次外交事件已无法再加入新的处理国。");
			}
			return;
		}
		string liveStateBlockReason = "";
		bool invalidLiveTarget = target == null || target == author || port.IsEliminated(target)
			|| !port.HasIndependentWorldDiplomacyAuthority(target);
		if (invalidLiveTarget
			|| orchestration.TryGetDiplomaticStateViolation(normalizedIntent, author, target, out liveStateBlockReason))
		{
			if (invalidLiveTarget) liveStateBlockReason = "diplomatic_action_has_no_live_target";
			port.Log("diplomatic action blocked by final live-state guard document=" + document.DocumentId
				+ " author=" + author + " target=" + (target ?? "")
				+ " intent=" + normalizedIntent + " reason=" + liveStateBlockReason);
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "final_live_state_guard:" + liveStateBlockReason);
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				port.Notify("外交宣言没有发布：正文中的外交动作与当前真实状态不相容。");
			}
			return;
		}
		List<string> finalLiveIntents = document.IsPlayerAuthored
			? orchestration.BuildLegalDiplomaticActionIntents(owningRound, author, target)
			: orchestration.BuildLegalDiplomaticDeclarationIntents(
				owningRound,
				author,
				target,
				document.IsRelayTurn,
				document.ResultSettlementSlotId,
				document.IsExternalResponseOnly,
				port.ResolveDocument(document.SourceDocumentId));
		if (!finalLiveIntents.Contains(normalizedIntent, StringComparer.OrdinalIgnoreCase))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "final_live_legal_action_guard");
			return;
		}
		WorldDiplomacyRoundOffer requiredPeaceOffer = port.FindRequiredPeaceOfferResponse(
			owningRound,
			author,
			document.ResultSettlementSlotId,
			document.IsExternalResponseOnly,
			document.SourceDocumentId,
			requireAnyOpenPeaceOffer: document.IsRelayTurn || document.IsPlayerAuthored);
		if (!WorldDiplomacyDocumentFactRules.DocumentContainsRequiredPeaceOfferResponse(document, requiredPeaceOffer))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "required_peace_offer_response_missing");
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				port.Notify("外交宣言没有发布：本篇必须先接受或拒绝当前和平原案。");
			}
			return;
		}
		if (normalizedIntent == "propose_peace"
			&& WorldDiplomacyRoundLifecycleRules.IsImmediateWarResponsePeaceSuppressed(owningRound, document.ResultSettlementSlotId,
				author, target, port.ResolveDocument))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "immediate_war_response_peace_suppressed");
			return;
		}
		if (document.IsPlayerAuthored
			&& orchestration.TryGetPlayerWorldStateIntentViolation(document, normalizedIntent, commitment, author, target, out string playerActionBlockReason))
		{
			port.Log("player diplomatic action blocked before execution document=" + document.DocumentId
				+ " author=" + author + " target=" + (target ?? "")
				+ " intent=" + normalizedIntent + " reason=" + playerActionBlockReason);
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "player_action_not_executable:" + playerActionBlockReason);
			if (!document.IsReadyForPublication)
			{
				port.Notify("外交宣言没有发布：正文中的外交动作与当前真实状态不相容。");
			}
			return;
		}
		string responseProposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(normalizedIntent);
		if (!document.IsPlayerAuthored && !string.IsNullOrWhiteSpace(responseProposalIntent)
			&& (target == null || !WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(document, author, target, responseProposalIntent, port.ResolveRound)))
		{
			port.Log("invalid AI offer response blocked before propagation document=" + document.DocumentId
				+ " author=" + author + " target=" + (target ?? "") + " intent=" + normalizedIntent);
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "offer_ownership_guard");
			return;
		}
		if (!document.IsPlayerAuthored && target != null
			&& WorldDiplomacyIntentVocabulary.IsPeaceIntent(normalizedIntent)
			&& !port.IsAtWar(author, target))
		{
			port.Log("illegal AI peace intent blocked before propagation document=" + document.DocumentId
				+ " author=" + author + " target=" + target + " intent=" + normalizedIntent);
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "peace_legality_guard");
			return;
		}
		bool appendedResultSettlementTarget = owningRound?.ResultSettlementPending == true
			&& target != null && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(owningRound, target);
		if (appendedResultSettlementTarget
			&& !TryIncludeResultSettlementTarget(port, owningRound, target))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
			return;
		}
		if (appendedResultSettlementTarget)
		{
			WorldDiplomacyRoundLifecycleRules.AddOrMergeResultSettlementSlot(owningRound, target, "route",
				document.DocumentId, author, prioritize: false, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		}
		// Make the validated declaration minimally publishable before any irreversible game
		// action. Full geographic propagation is filled in below.
		document.IsReadyForPublication = true;
		try
		{
			if (!allowedNoAction)
			{
				WorldDiplomacyWarPressureRules.ApplyDocumentPressure(document, port.FindWarPressure, port.NormalizeKingdomIdList, port.AddWarPressure);
				if (target != null && target != author && WorldDiplomacyIntentVocabulary.IsImmediateIntent(normalizedIntent))
				{
					orchestration.ExecuteImmediateIntent(author, target, normalizedIntent, document);
				}
				orchestration.ProcessDiplomaticThreatDocument(document, author, target);
				orchestration.TrySettleRelayOffer(document);
				orchestration.ApplyDiplomaticPressureEffect(document);
			}
			else if (allowedNoAction)
			{
				// This is mechanically inert, but it is still the kingdom's next published
				// declaration for any already-presented threat decision.
				orchestration.RecordDiplomaticThreatTargetDecisions(document, author, target, normalizedIntent);
			}
		}
		catch (Exception ex)
		{
			if (string.IsNullOrWhiteSpace(document.MechanicalResult))
			{
				document.MechanicalResult = "外交机制未执行：" + WorldDiplomacyTextRules.Limit(ex.Message, 180);
			}
			port.Log("diplomatic mechanism failed without discarding valid declaration document=" + document.DocumentId
				+ " intent=" + normalizedIntent + " error=" + ex.Message);
		}
		FinalizePublishedDocumentAfterAnalysis(port, orchestration, document, author, target, normalizedIntent, allowedNoAction);
	}
	internal static void ProcessAnalyzedMultiActionDocument(IWorldDiplomacyDocumentExecutionPort port,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument document)
	{
		List<WorldDiplomacyDocumentAction> actions = document?.Actions;
		string author = port.ResolveKingdomId(document?.AuthorKingdomId);
		if (document == null || actions == null || actions.Count < 1
			|| actions.Count > port.MaxDiplomaticActionsPerDocument || author == null) return;
		if (!document.IsPlayerAuthored && !port.HasIndependentWorldDiplomacyAuthority(author))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "controlled_vassal_has_no_diplomatic_authority");
			return;
		}
		if (!document.IsPlayerAuthored && !port.CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, authorBlockReason);
			return;
		}
		string sourceContextDocumentId = document.SourceDocumentId ?? "";
		WorldDiplomacyRound round = port.ResolveRound(document.RoundId);
		orchestration.PruneInvalidOffers(round);
		List<string> targets = new List<string>(actions.Count);
		HashSet<string> uniqueTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> newSettlementTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int statementCount = 0;
		for (int index = 0; index < actions.Count; index++)
		{
			WorldDiplomacyDocumentAction action = actions[index];
			string target = port.ResolveKingdomId(action?.TargetKingdomId);
			string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(action?.Intent);
			if (action == null || target == null || target == author || port.IsEliminated(target)
				|| !port.HasIndependentWorldDiplomacyAuthority(target) || !uniqueTargets.Add(target))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "multi_action_has_invalid_or_duplicate_target");
				return;
			}
			bool noAction = intent == "statement";
			if (noAction) statementCount++;
			bool allowedNoAction = noAction && !document.IsPlayerAuthored
				&& (document.IsRoundResponseNoActionDeclaration || document.IsWarResponseNoActionDeclaration)
				&& orchestration.IsNonRootAiRelayNoActionAllowed(
					round,
					document.ResultSettlementSlotId,
					author,
					target,
					document.IsRelayTurn,
					document.IsExternalResponseOnly,
					port.ResolveDocument(document.SourceDocumentId));
			if ((!WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent(intent) && !allowedNoAction)
				|| !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, action.Commitment))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "multi_action_is_not_executable");
				return;
			}
			List<string> finalLiveIntents = document.IsPlayerAuthored
				? orchestration.BuildLegalDiplomaticActionIntents(round, author, target)
				: orchestration.BuildLegalDiplomaticDeclarationIntents(
					round,
					author,
					target,
					document.IsRelayTurn,
					document.ResultSettlementSlotId,
					document.IsExternalResponseOnly,
					port.ResolveDocument(document.SourceDocumentId));
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
					return;
				}
				newSettlementTargets.Add(target);
			}
			else if (document.IsRelayTurn && round != null && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, target))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "kingdom_not_in_relay_route");
				return;
			}
			if (orchestration.TryGetDiplomaticStateViolation(intent, author, target, out string liveStateReason))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "final_live_state_guard:" + liveStateReason);
				return;
			}
			if (intent == "propose_peace"
				&& WorldDiplomacyRoundLifecycleRules.IsImmediateWarResponsePeaceSuppressed(round, document.ResultSettlementSlotId,
				author, target, port.ResolveDocument))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "immediate_war_response_peace_suppressed");
				return;
			}
			WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, action);
			string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
			if (!document.IsPlayerAuthored && !string.IsNullOrWhiteSpace(proposalIntent)
				&& !WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(document, author, target, proposalIntent, port.ResolveRound))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "offer_ownership_guard");
				return;
			}
			if (!document.IsPlayerAuthored && WorldDiplomacyIntentVocabulary.IsPeaceIntent(intent)
				&& !port.IsAtWar(author, target))
			{
				orchestration.SuppressInvalidDocumentBeforePropagation(document, "peace_legality_guard");
				return;
			}
			targets.Add(target);
		}
		if (statementCount > 0 && actions.Count != 1)
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "statement_must_be_the_only_diplomatic_action");
			return;
		}
		WorldDiplomacyRoundOffer requiredPeaceOffer = port.FindRequiredPeaceOfferResponse(
			round,
			author,
			document.ResultSettlementSlotId,
			document.IsExternalResponseOnly,
			document.SourceDocumentId,
			requireAnyOpenPeaceOffer: document.IsRelayTurn || document.IsPlayerAuthored);
		if (!WorldDiplomacyDocumentFactRules.DocumentContainsRequiredPeaceOfferResponse(document, requiredPeaceOffer))
		{
			orchestration.SuppressInvalidDocumentBeforePropagation(document, "required_peace_offer_response_missing");
			if (document.IsPlayerAuthored)
			{
				port.Notify("外交宣言没有发布：本篇必须先接受或拒绝当前和平原案。");
			}
			return;
		}
		if (WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(document, port.ResolveDocument))
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
			WorldDiplomacyRoundLifecycleRules.AddOrMergeResultSettlementSlot(round, targetId, "route", document.DocumentId, author, prioritize: false, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		}

		document.IsReadyForPublication = true;
		List<string> allAddressed = port.NormalizeKingdomIdList(actions.Select(x => x.TargetKingdomId), author);
		for (int index = 0; index < actions.Count; index++)
		{
			WorldDiplomacyDocumentAction action = actions[index];
			string target = targets[index];
			WorldDiplomacyDocumentApplication.BeginAction(document, action, target);
			bool noAction = string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent), "statement", StringComparison.OrdinalIgnoreCase);
			try
			{
				if (!noAction && orchestration.TryGetDiplomaticStateViolation(action.Intent, author, target, out string executionBlockReason))
				{
					document.MechanicalResult = "外交动作未执行：" + executionBlockReason;
					port.Log("multi-target diplomatic action became invalid during batch execution document="
						+ document.DocumentId + " action=" + action.ActionId + " reason=" + executionBlockReason);
				}
				else if (!noAction)
				{
					WorldDiplomacyWarPressureRules.ApplyDocumentPressure(document, port.FindWarPressure, port.NormalizeKingdomIdList, port.AddWarPressure);
					if (WorldDiplomacyIntentVocabulary.IsImmediateIntent(action.Intent)) orchestration.ExecuteImmediateIntent(author, target, WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent), document);
					orchestration.ProcessDiplomaticThreatDocument(document, author, target, recordTargetDecisions: false);
					orchestration.TrySettleRelayOffer(document);
					orchestration.ApplyDiplomaticPressureEffect(document);
				}
			}
			catch (Exception ex)
			{
				if (string.IsNullOrWhiteSpace(document.MechanicalResult))
				{
					document.MechanicalResult = "外交机制未执行：" + WorldDiplomacyTextRules.Limit(ex.Message, 180);
				}
				port.Log("multi-target diplomatic action failed without discarding declaration document=" + document.DocumentId
					+ " action=" + action.ActionId + " intent=" + action.Intent + " error=" + ex.Message);
			}
			WorldDiplomacyDocumentApplication.CaptureActionResult(document, action);
		}
		orchestration.RecordDiplomaticThreatTargetDecisionsForActions(document, author);
		bool requiredThreatActionDeferred = orchestration.DeferUnresolvedRequiredThreatAction(
			document,
			author,
			targets[0],
			actions[0].Intent);
		if (!requiredThreatActionDeferred) WorldDiplomacyRoundLifecycleRules.SettleDiplomaticThreatFollowThroughAfterDeclaration(
			document, orchestration.Threats(), author,
			(threat, doc) => orchestration.ApplyDiplomaticThreatReputationPenalty(threat, doc));

		WorldDiplomacyDocumentApplication.SealActions(document, allAddressed, sourceContextDocumentId);
		orchestration.SettleInternationalReputationForDocument(document);
		try
		{
			orchestration.AppendCanonicalDocumentEvents(document);
			WorldDiplomacyRoundLifecycleRules.FinalizeDiplomaticThreatHistoryAfterDocument(document,
			orchestration.Threats(), orchestration.TryAppendDiplomaticThreatHistoryResult,
			orchestration.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult,
			orchestration.TryAppendDiplomaticThreatIssuerRewardHistoryResult);
			WorldDiplomacyRoundLifecycleRules.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document,
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
			kingdom != null && !port.IsEliminated(kingdom) && port.HasIndependentWorldDiplomacyAuthority(kingdom),
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
		WorldDiplomacyRoundLifecycleRules.AddOrMergeResultSettlementSlot(round, kingdomId, "route",
			round.ResultSettlementTriggerDocumentId, "", prioritize: false, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		return true;
	}

    private static bool CanUseResultSettlementTarget(IWorldDiplomacyDocumentExecutionPort port, WorldDiplomacyRound round, string author, string target)
    {
        if (round == null || author == null) return false;
        return WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(round.ResultSettlementPending, target != null, target == author,
            target != null && port.IsEliminated(target), target != null && port.HasIndependentWorldDiplomacyAuthority(target),
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
		WorldDiplomacyRoundLifecycleRules.InitializeResultSettlementRouteSlots(round, storage?.Documents, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		pruneInvalidOffers?.Invoke(round);
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsOfferOfStatus(x, "open")))
		{
			string target = port.ResolveKingdomId(offer.TargetKingdomId);
			if (target == null || !port.HasIndependentWorldDiplomacyAuthority(target)
				|| !TryIncludeResultSettlementTarget(port, round, target))
			{
				offer.Status = "invalidated";
				continue;
			}
			WorldDiplomacyRoundLifecycleRules.AddOrMergeResultSettlementSlot(round, target, "offer_response",
				offer.SourceDocumentId, offer.ProposerKingdomId, prioritize: true, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		}
		foreach (WorldDiplomacyThreat threat in (storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(x, round?.RoundId)))
		{
			WorldDiplomacyThreatSettlementSlotDecision threatSlot =
				WorldDiplomacyRoundLifecycleRules.EvaluateThreatSettlementSlot(threat);
			if (!threatSlot.Applies) continue;
			WorldDiplomacyRoundLifecycleRules.AddOrMergeResultSettlementSlot(round, threatSlot.KingdomId, threatSlot.Kind,
				threatSlot.SourceDocumentId, threatSlot.RelatedKingdomId, prioritize: true, (r, id) => TryIncludeResultSettlementTarget(port, r, id), port.NewId);
		}
	}
}
