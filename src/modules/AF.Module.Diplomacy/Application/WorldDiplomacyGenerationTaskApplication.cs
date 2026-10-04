using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;

namespace AnimusForge;

// Owns generation admission, queued job composition, and terminal rejection recovery.
internal static class WorldDiplomacyGenerationTaskApplication
{
    internal static void AbandonRejectedGeneration(
        WorldDiplomacyJob job,
        string authorKingdomId,
        string targetKingdomId,
        string reason,
        WorldDiplomacyStorage storage,
        int currentDay,
        int maxTechnicalFailures,
        Func<string, WorldDiplomacyRound> resolveRound,
        Action<string> closeActiveRound,
        Action<WorldDiplomacyRound> scheduleResultSettlement,
        Action<WorldDiplomacyRound> advanceRelay,
        Action<string, string> completeExchange,
        Action<string> log)
    {
        if (job == null) return;
        log?.Invoke("generated declaration abandoned without publication job=" + job.JobId
            + " author=" + (authorKingdomId ?? "") + " target=" + (targetKingdomId ?? "")
            + " reason=" + (reason ?? ""));
        WorldDiplomacyRound round = resolveRound?.Invoke(FirstNonEmpty(job.RoundId, job.ExchangeId));
        if (job.IsRelayTurn && round != null && IsActiveRoundState(round.State))
        {
            round.RelayWaiting = false;
            round.ConsecutiveTechnicalGenerationFailures =
                NextTechnicalFailureCount(
                    round.ConsecutiveTechnicalGenerationFailures,
                    maxTechnicalFailures);
            log?.Invoke("round technical generation failure round=" + round.RoundId
                + " consecutive=" + round.ConsecutiveTechnicalGenerationFailures.ToString(CultureInfo.InvariantCulture)
                + " reason=" + (reason ?? ""));
            switch (EvaluateRejectedGenerationAction(
                new WorldDiplomacyRejectedGenerationInput
                {
                    IsRelayTurn = true,
                    RoundActive = true,
                    CircuitBreakerTripped = ShouldTripTechnicalCircuitBreaker(
                        round.ConsecutiveTechnicalGenerationFailures,
                        maxTechnicalFailures),
                    ResultSettlementPending = round.ResultSettlementPending
                }))
            {
                case WorldDiplomacyRejectedGenerationAction.CloseRound:
                    closeActiveRound?.Invoke("technical_consecutive_generation_rejections");
                    return;
                case WorldDiplomacyRejectedGenerationAction.SkipSettlementSlotAndReschedule:
                    SkipResultSettlementSlot(round, job.ResultSettlementSlotId, job.AuthorKingdomId, "generation_rejected", storage?.DiplomaticThreats, currentDay, log);
                    scheduleResultSettlement?.Invoke(round);
                    return;
                default:
                    advanceRelay?.Invoke(round);
                    return;
            }
        }
        completeExchange?.Invoke(job.ExchangeId, "technical_generation_rejected");
        if (round != null
            && WorldDiplomacyLiveRoundRules.Contains(storage, round)
            && IsActiveRoundState(round.State)
            && string.IsNullOrWhiteSpace(round.RootDocumentId))
        {
            closeActiveRound?.Invoke("technical_generation_rejected");
        }
    }
	internal static void PrepareGenerationJob(
        string authorId,
        string targetId,
        WorldDiplomacyExchange exchange,
        bool isResponse,
        WorldDiplomacyDocument sourceDocument,
        int priority,
        bool externalResponseOnly,
        bool isReminder,
        string roundId,
        bool isRelayTurn,
        bool allowUntargeted,
        string previousKingdomId,
        int scheduledDay,
        string resultSettlementSlotId,
        WorldDiplomacyStorage storage,
        int currentDay,
        int generationMaxTokens,
        int maxAutomaticDocumentsPerRound,
        Func<string, WorldDiplomacyRound> resolveRound,
        Action<WorldDiplomacyRound> pruneInvalidOffers,
        Func<string, string> getAuthorBlockReason,
        Func<string, bool> hasIndependentAuthority,
        Func<string, string> resolvePartyId,
        Func<string, bool> isEliminatedParty,
        Func<WorldDiplomacyRound, string, string, bool, string, bool, WorldDiplomacyDocument, List<string>> legalDeclarationIntents,
        Func<WorldDiplomacyRound, string, List<string>> getSettlementTargetIds,
        Func<WorldDiplomacyRound, string, string, bool> isSingleTargetActionable,
        Func<string, WorldDiplomacyRound, List<string>> getDefaultTargetIds,
        Action<string, string> completeExchange,
        Action<WorldDiplomacyRound> scheduleSettlementTurn,
        Action<WorldDiplomacyRound> advanceRelay,
        Action<string> closeActiveRound,
        Func<WorldDiplomacyRound, string> getCommonContract,
        Func<(int minimum, int maximum)> declarationCharRange,
        Action syncCanonicalHistory,
        Func<WorldDiplomacyRound, string, string, WorldDiplomacyDocument, bool, string> buildRelayPrompt,
        Func<string, string, WorldDiplomacyExchange, bool, WorldDiplomacyDocument, bool, string, bool, List<string>, bool, string> buildGenerationPrompt,
        Func<string, string> createId,
        Func<WorldDiplomacyJob, string> buildLegalSignature,
        Func<WorldDiplomacyJob, bool> ensureStrategicProfile,
        Action<WorldDiplomacyJob> captureCanonicalHistory,
        Action<WorldDiplomacyJob, string, string, string> abandonGeneration,
        Func<string, string, bool> isAtWar,
        Action<WorldDiplomacyJob> enqueue,
        Action<string> log)
	{
		if (string.IsNullOrWhiteSpace(authorId) || (string.IsNullOrWhiteSpace(targetId) && !allowUntargeted))
		{
			completeExchange?.Invoke(exchange?.ExchangeId, "invalid_generation_parties");
			WorldDiplomacyRound invalidRound = resolveRound?.Invoke(FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId));
			if (invalidRound?.ResultSettlementPending == true)
			{
				SkipResultSettlementSlot(invalidRound, resultSettlementSlotId, authorId, "invalid_generation_parties", storage?.DiplomaticThreats, currentDay, log);
				scheduleSettlementTurn?.Invoke(invalidRound);
			}
			return;
		}
		WorldDiplomacyRound owningRound = resolveRound?.Invoke(FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId));
		pruneInvalidOffers?.Invoke(owningRound);
		bool isResultSettlementTurn = owningRound?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(resultSettlementSlotId);
		string authorBlockReason = getAuthorBlockReason?.Invoke(authorId);
		if (!string.IsNullOrEmpty(authorBlockReason))
		{
			log?.Invoke("generation blocked by author authority author=" + (authorId ?? "")
				+ " reason=" + authorBlockReason + " source=" + (sourceDocument?.DocumentId ?? ""));
			completeExchange?.Invoke(exchange?.ExchangeId, authorBlockReason);
			if (isRelayTurn && owningRound != null)
			{
				owningRound.RelayWaiting = false;
				if (owningRound.ResultSettlementPending)
				{
					SkipResultSettlementSlot(owningRound, resultSettlementSlotId, authorId, authorBlockReason, storage?.DiplomaticThreats, currentDay, log);
					scheduleSettlementTurn?.Invoke(owningRound);
				}
				else advanceRelay?.Invoke(owningRound);
			}
			return;
		}
		if (hasIndependentAuthority?.Invoke(authorId) != true)
		{
			log?.Invoke("generation skipped for diplomatically controlled vassal author=" + (authorId ?? "")
				+ " round=" + (owningRound?.RoundId ?? ""));
			completeExchange?.Invoke(exchange?.ExchangeId, "controlled_vassal_has_no_diplomatic_authority");
			if (isRelayTurn && owningRound != null)
			{
				owningRound.RelayWaiting = false;
				if (owningRound.ResultSettlementPending)
				{
					SkipResultSettlementSlot(owningRound, resultSettlementSlotId, authorId, "controlled_vassal", storage?.DiplomaticThreats, currentDay, log);
					scheduleSettlementTurn?.Invoke(owningRound);
				}
				else advanceRelay?.Invoke(owningRound);
			}
			return;
		}
		bool playerPriorityResponse = externalResponseOnly && sourceDocument?.IsPlayerAuthored == true;
		List<string> actionableTargetIds;
		if (playerPriorityResponse)
		{
			string priorityTargetId = resolvePartyId?.Invoke(sourceDocument?.AuthorKingdomId)
				?? resolvePartyId?.Invoke(targetId);
			actionableTargetIds = !string.IsNullOrWhiteSpace(priorityTargetId)
				&& (legalDeclarationIntents?.Invoke(owningRound, authorId, priorityTargetId,
						isRelayTurn, resultSettlementSlotId, true, sourceDocument)?.Count ?? 0) > 0
					? new List<string> { priorityTargetId }
					: new List<string>();
		}
		else if (isRelayTurn && owningRound != null)
		{
			actionableTargetIds = isResultSettlementTurn
				? getSettlementTargetIds?.Invoke(owningRound, authorId) ?? new List<string>()
				: (owningRound.RelayRouteKingdomIds ?? new List<string>())
					.Select(id => resolvePartyId?.Invoke(id))
					.Where(id => !string.IsNullOrWhiteSpace(id)
						&& !string.Equals(id, authorId, StringComparison.Ordinal)
						&& isEliminatedParty?.Invoke(id) == false
						&& hasIndependentAuthority?.Invoke(id) == true
						&& (legalDeclarationIntents?.Invoke(owningRound, authorId, id,
								true, resultSettlementSlotId, externalResponseOnly, sourceDocument)?.Count ?? 0) > 0)
					.Distinct()
					.ToList();
		}
		else if (!string.IsNullOrWhiteSpace(targetId))
		{
			actionableTargetIds = isSingleTargetActionable?.Invoke(owningRound, authorId, targetId) == true
				? new List<string> { targetId }
				: new List<string>();
		}
		else
		{
			actionableTargetIds = getDefaultTargetIds?.Invoke(authorId, owningRound) ?? new List<string>();
		}
		if (actionableTargetIds.Count == 0)
		{
			log?.Invoke("generation skipped because no actionable diplomatic target remains author=" + authorId
				+ " round=" + (owningRound?.RoundId ?? ""));
			completeExchange?.Invoke(exchange?.ExchangeId, "no_actionable_diplomatic_target");
			if (isRelayTurn && owningRound != null)
			{
				owningRound.RelayWaiting = false;
				if (owningRound.ResultSettlementPending)
				{
					SkipResultSettlementSlot(owningRound, resultSettlementSlotId, authorId, "no_actionable_target", storage?.DiplomaticThreats, currentDay, log);
					scheduleSettlementTurn?.Invoke(owningRound);
				}
				else
				{
					WorldDiplomacyRoundParticipant participant = (owningRound.Participants ?? new List<WorldDiplomacyRoundParticipant>())
						.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, authorId, StringComparison.OrdinalIgnoreCase));
					if (participant != null && !participant.MandatoryReplyPending) participant.State = "withdrawn";
					advanceRelay?.Invoke(owningRound);
				}
			}
			else if (owningRound != null && string.IsNullOrWhiteSpace(owningRound.RootDocumentId))
			{
				closeActiveRound?.Invoke("technical_no_actionable_diplomatic_target");
			}
			return;
		}
		if (owningRound != null)
		{
			if (!playerPriorityResponse && !isResultSettlementTurn
				&& (owningRound.AutomaticCircuitBreakerTripped || owningRound.AutomaticDocumentsStarted >= maxAutomaticDocumentsPerRound))
			{
				TripAutomaticRoundCircuitBreaker(storage, owningRound, "automatic_document_limit", log);
				completeExchange?.Invoke(exchange?.ExchangeId, "automatic_round_circuit_breaker");
				return;
			}
		}
		string frozenCommonContract = getCommonContract?.Invoke(owningRound);
		(int minimumCharacters, int maximumCharacters) = declarationCharRange?.Invoke() ?? (0, 0);
		string systemPrompt = isRelayTurn
			? WorldDiplomacyPromptContractRules.BuildRelayGenerationSystemPrompt(frozenCommonContract, minimumCharacters, maximumCharacters)
			: WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt(frozenCommonContract, minimumCharacters, maximumCharacters);
		syncCanonicalHistory?.Invoke();
		bool includeEmbeddedRoundPlan = !isRelayTurn && !isResponse && owningRound != null && string.IsNullOrWhiteSpace(owningRound.RootDocumentId);
		List<string> roundPlanCandidates = new List<string>();
		if (includeEmbeddedRoundPlan)
		{
			roundPlanCandidates = actionableTargetIds
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		string dynamicPrompt = isRelayTurn
			? buildRelayPrompt?.Invoke(owningRound, authorId, targetId, sourceDocument, externalResponseOnly)
			: buildGenerationPrompt?.Invoke(authorId, targetId, exchange, isResponse, sourceDocument, isReminder, roundId,
				allowUntargeted, roundPlanCandidates, externalResponseOnly);
		string userPrompt = WorldDiplomacyPromptContractRules.BuildDeclareModePrompt(dynamicPrompt);
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = createId?.Invoke("diplomacy_generate"),
			Kind = "generate",
			Priority = priority,
			CreatedDay = scheduledDay >= 0 ? scheduledDay : currentDay,
			ExchangeId = exchange?.ExchangeId ?? roundId ?? "",
			RoundId = FirstNonEmpty(roundId, exchange?.ExchangeId),
			AuthorKingdomId = authorId,
			TargetKingdomId = targetId ?? "",
			SourceDocumentId = sourceDocument?.DocumentId ?? "",
			IsResponse = isResponse,
			ForcedIntent = "",
			IsExternalResponseOnly = externalResponseOnly,
			IsReminder = isReminder,
			IsRelayTurn = isRelayTurn,
			AllowUntargeted = allowUntargeted,
			PreviousKingdomId = previousKingdomId ?? "",
			ResultSettlementSlotId = resultSettlementSlotId ?? "",
			AllowAutonomousNoAction = false,
			CandidateKingdomIds = isResultSettlementTurn
				? new List<string>(actionableTargetIds)
				: roundPlanCandidates,
			PresentedThreatDocumentIds = SelectPresentedThreatStageDocumentIds(storage?.DiplomaticThreats, authorId),
			PresentedThreatFollowThroughDocumentIds = SelectNoncompliedThreatStageDocumentIds(storage?.DiplomaticThreats, authorId),
			WasAtWarWhenQueued = !string.IsNullOrWhiteSpace(targetId) && isAtWar?.Invoke(authorId, targetId) == true,
			SystemPrompt = systemPrompt,
			UserPrompt = userPrompt,
			CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey,
			ProfiledKingdomId = "",
			MaxTokens = generationMaxTokens
		};
		job.PresentedLegalActionSignature = buildLegalSignature?.Invoke(job);
		if (ensureStrategicProfile?.Invoke(job) != true)
		{
			abandonGeneration?.Invoke(job, authorId, targetId, "missing_kingdom_strategic_profile");
			return;
		}
		captureCanonicalHistory?.Invoke(job);
		if (owningRound != null && !playerPriorityResponse) owningRound.AutomaticDocumentsStarted++;
		enqueue?.Invoke(job);
	}
}
