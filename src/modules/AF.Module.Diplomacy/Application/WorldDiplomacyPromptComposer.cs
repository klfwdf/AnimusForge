using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Synchronous prompt policy. Only stable IDs, canonical records and value facts cross
// this port; nothing here retains a Campaign object or dispatches background work.
internal static class WorldDiplomacyPromptComposer
{
    private static void AppendPlayerContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, IWorldDiplomacyOrchestration orchestration,
        string author, string target, WorldDiplomacyRound round, WorldDiplomacyDocument source = null)
    {
        if (source?.IsPlayerAuthored == true || (orchestration is WorldDiplomacyOrchestration owner
            && owner.IsPlayerDiplomacyContext(author, target, round, source)))
            sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_000", "player_diplomacy=true; Player-related diplomacy: autonomous AI limits on cooldowns, war count, civil war, round turns, action count, one action per target, and mandatory priority responses do not apply. Preserve all explicit actions and their order; do not refuse publication or alter demands because of these limits. Actual targets and source offers must still be accurate."));
    }

    internal static string BilateralStateLabel(IWorldDiplomacyPromptWorld world, string authorId, string targetId)
    {
        if (world.ResolveKingdom(authorId) == null || world.ResolveKingdom(targetId) == null) return world.LocalizePrompt("AF_DIP_Prompt_Composer_001", "Unknown");
        return WorldDiplomacyTextRules.BuildBilateralStateLabel(
            world.IsAtWar(authorId, targetId),
            world.IsAlly(authorId, targetId),
            world.HasTradeAgreement(authorId, targetId), world.LocalizePrompt);
    }

	internal static string BuildRoundPlanSystemPrompt(IWorldDiplomacyPromptWorld world, WorldDiplomacyRound round)
	{
		StringBuilder sb = WorldDiplomacyPromptContractRules.CreateSystemPromptBuilder(world.GetCommonDiplomacyContract(round));
	sb.AppendLine(WorldDiplomacyPromptContractRules.MarkerForPrompt(WorldDiplomacyPromptContractRules.RoundPlanTaskMarker, world.LocalizePrompt) + world.LocalizePrompt("AF_DIP_Prompt_Composer_002", "MODE=ROUND_PLAN in the final message determines the task and output structure."));
		return PromptComposerTexts.ApplyOutputLanguage(sb.ToString().TrimEnd());
	}

	internal static string BuildRoundPlanPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument root, List<string> candidateIds)
	{
		StringBuilder sb = new StringBuilder();
        AppendPlayerContext(sb, world, orchestration, root.AuthorKingdomId, root.TargetKingdomId, world.ResolveRound(root.RoundId), root);
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot))
		{
			sb.AppendLine(vassalageSnapshot);
		}
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_003", "[Opening declaration]"));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_004", "Initiating kingdom=") + root.AuthorKingdomId + "=" + root.AuthorKingdomName);
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_005", "Title=") + root.Title);
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_006", "Body=") + WorldDiplomacyTextRules.Limit(root.Body, 2200));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_007", "Explicitly addressed=") + string.Join(",", root.AddressedKingdomIds ?? new List<string>()));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_008", "Mentioned=") + string.Join(",", root.MentionedKingdomIds ?? new List<string>()));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_009", "Maximum total participants, including the initiator=") + world.GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_010", "Candidate kingdoms:"));
		foreach (string id in candidateIds ?? new List<string>())
		{
			string kingdom = world.ResolveKingdom(id);
			if (kingdom == null) continue;
			WorldDiplomacyRound rootRound = world.ResolveRound(root.RoundId);
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(world, world.ResolveKingdom(root.AuthorKingdomId), kingdom, rootRound,
				rootRound == null
					? orchestration.BuildPotentialDiplomaticActionIntents(world.ResolveKingdom(root.AuthorKingdomId), kingdom)
					: orchestration.BuildLegalDiplomaticActionIntents(rootRound, world.ResolveKingdom(root.AuthorKingdomId), kingdom)));
			string policy = world.BuildPolicySnapshot(id);
		if (!string.IsNullOrWhiteSpace(policy)) sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_011", "  Policies=") + WorldDiplomacyTextRules.Limit(policy, 500));
		}
	sb.AppendLine("【MODE=ROUND_PLAN】");
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_012", "Select participants for this event once, based on the opening diplomatic declaration and the candidates' actual interests. Observer kingdoms will not be repeatedly reassessed later."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_013", "Any kingdom explicitly addressed by the declaration must participate. Select only kingdoms that will actually intervene in these negotiations, not mere observers or commentators. The participant count is a ceiling, not a quota; use only candidate IDs."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_014", "The event's cause does not predetermine its outcome. Participants should be able to advance currently legal alliances, alliance termination, trade, trade termination, war declarations or peace, or propose, accept, reject or counter terms."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_015", "Output only JSON: {\"topic\":\"brief diplomatic issue\",\"selected_kingdom_ids\":[\"ID\"],\"reason\":\"brief reason\"}"));
		return sb.ToString().TrimEnd();
	}

	internal static string BuildRelayConversationTurnPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration,
		WorldDiplomacyRound round,
		string author,
		string previous,
		WorldDiplomacyDocument prioritySource = null,
		bool priorityResponseOnly = false)
	{
		orchestration.PruneInvalidOffers(round);
		StringBuilder sb = new StringBuilder();
        AppendPlayerContext(sb, world, orchestration, author, previous, round, prioritySource);
		List<string> legalTargetIds = round?.ResultSettlementPending == true
			? orchestration.GetResultSettlementActionableTargetIds(round, author)
			: (round?.RelayRouteKingdomIds ?? new List<string>())
				.Where(x => !string.Equals(x, author, StringComparison.OrdinalIgnoreCase)).ToList();
		Dictionary<string, List<string>> legalActionsByTarget = orchestration.BuildLegalDiplomaticDeclarationIntentMap(
			round,
			author,
			legalTargetIds,
			isRelayTurn: true,
			resultSettlementSlotId: round?.ResultSettlementCurrentSlotId,
			isExternalResponseOnly: priorityResponseOnly,
			responseSource: prioritySource);
		legalTargetIds = legalActionsByTarget.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		List<string> exclusivePeaceResponseTargetIds = legalActionsByTarget
			.Where(x => WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(x.Value))
			.Select(x => x.Key)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_016", "[Current state for this diplomatic document]"));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_017", "Declarations in the long-term archive are published diplomatic documents. Respond afresh to the current situation. Meaningful negotiations are allowed, but do not repeat historical statements or reiterate positions without substance. Only mechanical diplomatic acts count as round action progress; negotiation statements do not extend the round indefinitely."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_018", "Topic=") + (round.RoundTopic ?? ""));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_019", "Document delivery and publication order=") + string.Join(">", round.RelayRouteKingdomIds ?? new List<string>()));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_020", "Publisher of this document=") + author + "=" + world.KingdomName(author) + world.LocalizePrompt("AF_DIP_Prompt_Composer_021", ", authorizing ruler=") + world.RulerName(author));
		if (priorityResponseOnly && prioritySource != null)
		{
			string priorityActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(prioritySource, author, world.LocalizePrompt);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_022", "[Priority task: respond to the player kingdom's declaration]"));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_023", "The following declaration from the player kingdom has reached this court and directly addresses this kingdom. This document must respond to it directly instead of following the original document sequence to discuss other kingdoms: source=")
			+ prioritySource.DocumentId + world.LocalizePrompt("AF_DIP_Prompt_Composer_024", "|issuing kingdom=") + prioritySource.AuthorKingdomId + world.LocalizePrompt("AF_DIP_Prompt_Composer_025", "|title=") + prioritySource.Title
			+ (string.IsNullOrWhiteSpace(priorityActionFact) ? "" : world.LocalizePrompt("AF_DIP_Prompt_Composer_026", "|actions relevant to this kingdom=") + priorityActionFact));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_027", "Choose a currently available action. If the player issued a condemnation or ultimatum, use comply_ultimatum only for unconditional submission. Any other actual action is settled as non-compliance, and the threat source fields must remain empty."));
		}
		AppendDiplomaticAuthorDecisionContext(sb, world, author, round.RoundId);
		AppendOtherKingdomRelationshipContext(sb, world, author, legalTargetIds);
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_028", "Most recent document delivered to this court came from=") + (previous ?? "") + "=" + world.KingdomName(previous));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_029", "The sender is merely the source of the latest document, not a program-selected target. This kingdom must choose from the allowed action targets below."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_030", "Allowed action targets=") + string.Join(",", legalTargetIds));
		sb.AppendLine(WorldDiplomacyPromptContractRules.BuildCurrentLegalDiplomaticOptions(legalActionsByTarget, world.LocalizePrompt));
		WorldDiplomacyRoundOffer requiredPeaceOffer = world.FindRequiredPeaceOfferResponse(
			round,
			author,
			round.ResultSettlementCurrentSlotId,
			priorityResponseOnly,
			prioritySource?.DocumentId,
			requireAnyOpenPeaceOffer: true);
		if (exclusivePeaceResponseTargetIds.Count > 0)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_031", "Response to an original peace offer: target=") + string.Join(",", exclusivePeaceResponseTargetIds)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_032", "; choose only accept_peace to accept unchanged or reject_peace to explicitly reject. Do not add or modify terms or propose another peace plan."));
		}
		if (orchestration.HasCessionBoundMultiplePeaceAcceptanceOptions(round, author, legalActionsByTarget))
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_033", "Multiple original peace offers include territorial cessions: accept at most one in this document; reject the others or leave them for the next document."));
		}
		if (requiredPeaceOffer != null)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_034", "This document must answer an original peace offer: source=") + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_035", "|proposing kingdom=") + requiredPeaceOffer.ProposerKingdomId + "。");
		}
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_036", "If statement is currently available, it represents a structured negotiation move, not a mechanical diplomatic act. Fill in negotiation_move and actually perform that move in the body; the document will still be delivered to the next kingdom on the original route. Do not pass vague positions off as new progress."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_037", "For annexation, tribute, garrison or full vassalage, use the propose_/accept_/reject_ actions for annexation/tributary/garrison/vassal. New offers include treaty_terms={receiving_kingdom_id:receiving or overlord kingdom ID,joining_kingdom_id:joining or subject kingdom ID}; make these roles explicit in the body too. Accepting an original offer must bind responding_to_offer_document_id and responding_to_offer_action_id and adopt all original terms. withdraw_offer may only stand alone to withdraw a specific, unaccepted offer made by this kingdom; it cannot reverse effects that already occurred."));
		AppendRelayResponseSourceContext(
			sb,
			world,
			round,
			author,
			prioritySource,
			requiredPeaceOffer?.SourceDocumentId);
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => WorldDiplomacyRoundLifecycleRules.IsOfferOfStatus(x, "open")))
		{
			bool canAnswer = string.Equals(offer.TargetKingdomId, author, StringComparison.OrdinalIgnoreCase);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_038", "Offer awaiting response=") + offer.Intent + world.LocalizePrompt("AF_DIP_Prompt_Composer_035", "|proposing kingdom=") + offer.ProposerKingdomId + world.LocalizePrompt("AF_DIP_Prompt_Composer_040", "|target kingdom=") + offer.TargetKingdomId + world.LocalizePrompt("AF_DIP_Prompt_Composer_041", "|source=") + offer.SourceDocumentId
			+ (canAnswer ? world.LocalizePrompt("AF_DIP_Prompt_Composer_042", "|response eligibility=this kingdom may accept or reject") : world.LocalizePrompt("AF_DIP_Prompt_Composer_043", "|response eligibility=this kingdom is not the target and may not accept or reject; it may only make a new offer or choose another legal action")));
		}
		int age = Math.Max(0, world.CurrentDay() - round.StartedDay);
		int targetDays = Math.Max(1, round.SoftEndDay - round.StartedDay);
		int remainingDays = Math.Max(0, targetDays - age);
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_044", "These negotiations have lasted=") + age.ToString(CultureInfo.InvariantCulture) + world.LocalizePrompt("AF_DIP_Prompt_Composer_045", " days; expected duration=") + targetDays.ToString(CultureInfo.InvariantCulture)
		+ world.LocalizePrompt("AF_DIP_Prompt_Composer_046", " days; time until expected closure=") + remainingDays.ToString(CultureInfo.InvariantCulture) + world.LocalizePrompt("AF_DIP_Prompt_Composer_047", " days; current document exchange stage=") + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture));
		WorldDiplomacyPromptContractRules.AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays, world.LocalizePrompt);
		WorldDiplomacyPromptContractRules.AppendOpenOfferAnswerRequirement(sb, round, author, world.LocalizePrompt);
	if (age * 100 >= targetDays * 85) sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_048", "The final stage has begun: choose a currently available action that can bring the situation to a conclusion."));
	else if (age * 100 >= targetDays * 70) sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_049", "The round is in its later stages: prioritize resolving differences and reaching a definite outcome."));
		if (!string.IsNullOrWhiteSpace(round.ExternalOpeningContext))
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_050", "[Known external developments in this diplomatic event]"));
			sb.AppendLine(WorldDiplomacyTextRules.Limit(round.ExternalOpeningContext, 1800));
		}
		string gatheringContext = world.BuildGatheringSnapshot(round.RelayRouteKingdomIds, 3);
		if (!string.IsNullOrWhiteSpace(gatheringContext))
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_051", "[Recent relevant feasts]"));
			sb.AppendLine(WorldDiplomacyTextRules.Limit(gatheringContext, 900));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_052", "Feasts are merely public developments that can be used or commented upon. They do not predetermine praise, mockery or hostility, nor do they automatically produce diplomatic outcomes."));
		}
		List<string> peaceProposalTargetIds = new List<string>();
		foreach (string id in legalTargetIds)
		{
			string other = world.ResolveKingdom(id);
			if (other == null || other == author) continue;
			if (!legalActionsByTarget.TryGetValue(id, out List<string> targetActions)) continue;
			bool includePeaceNegotiationTerms = targetActions.Any(x => string.Equals(
				WorldDiplomacyIntentVocabulary.NormalizeIntent(x),
				"propose_peace",
				StringComparison.OrdinalIgnoreCase));
			AppendDiplomaticTargetDecisionContext(
				sb,
				world,
				round,
				author,
				other,
				includePeaceNegotiationTerms,
				targetActions);
			if (includePeaceNegotiationTerms)
			{
				peaceProposalTargetIds.Add(id);
			}
		}
		if (peaceProposalTargetIds.Count > 0)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_053", "Targets to whom peace may currently be proposed=")
			+ string.Join(",", peaceProposalTargetIds) + "。");
		}
		return sb.ToString().TrimEnd();
	}

	internal static string BuildAutonomousOpeningPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration, string author, string roundId, List<string> candidateIds)
	{
		if (author == null) return "";
		StringBuilder sb = new StringBuilder();
		AppendDiplomaticAuthorDecisionContext(sb, world, author, roundId);
		WorldDiplomacyRound round = world.ResolveRound(roundId);
        AppendPlayerContext(sb, world, orchestration, author, null, round);
		if (!string.IsNullOrWhiteSpace(round?.ExternalOpeningContext))
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_054", "[External diplomatic events that have already occurred]"));
			sb.AppendLine(WorldDiplomacyTextRules.Limit(round.ExternalOpeningContext, 1800));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_055", "This is a real event that this kingdom may use or respond to; it does not predetermine this kingdom's target, position or action."));
		}
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_056", "[Determine participants for this diplomatic event in the same response]"));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_057", "Based on the issuing kingdom's national profile and the actual current situation, independently choose one or more targets, actions and participating kingdoms. Use only candidate IDs and currently available actions for each target."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_058", "Include round_plan in the same JSON. Maximum total participants, including the initiator=") + world.GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + world.LocalizePrompt("AF_DIP_Prompt_Composer_059", ". Directly addressed kingdoms must appear in selected_kingdom_ids. Select only kingdoms that actually need to join this ongoing document exchange; do not fill the quota."));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_060", "[Available diplomatic targets and immediate hard facts]"));
		foreach (string id in candidateIds ?? new List<string>())
		{
			string candidate = world.ResolveKingdom(id);
			if (candidate == null || candidate == author || world.IsEliminated(candidate) || !world.HasIndependentWorldDiplomacyAuthority(candidate)) continue;
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(world, author, candidate, round,
				round == null
					? orchestration.BuildPotentialDiplomaticActionIntents(author, candidate)
					: orchestration.BuildLegalDiplomaticActionIntents(round, author, candidate)));
			if (world.IsAtWar(author, candidate))
			{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_061", "  War assessment=") + WorldDiplomacyTextRules.CompactPromptFact(BuildWarDecisionContext(world, author, candidate, true), 900));
			}
		}
		int activity = world.GetActivityLevel();
		sb.AppendLine(activity switch
		{
		0 => world.LocalizePrompt("AF_DIP_Prompt_Composer_062", "Diplomatic activity is low: prefer lower-cost offers or responses, but still take at least one actual action."),
		2 => world.LocalizePrompt("AF_DIP_Prompt_Composer_063", "Diplomatic activity is high: seek opportunities to advance national goals more actively, but do not start wars without cause."),

		_ => world.LocalizePrompt("AF_DIP_Prompt_Composer_064", "Diplomatic activity is normal: balance national goals against the cost of responding.")

		});
		return sb.ToString();
	}

	internal static string BuildGenerationPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration,
		string author,
		string target,
		WorldDiplomacyExchange exchange,
		bool isResponse,
		WorldDiplomacyDocument sourceDocument,
		bool isReminder,
		string roundId,
		bool allowUntargeted,
		List<string> roundPlanCandidateIds,
		bool isExternalResponseOnly)
	{
		if (author == null) return "";
		if (target == null && !isResponse)
		{
			return BuildAutonomousOpeningPrompt(world, orchestration, author, roundId, roundPlanCandidateIds);
		}
		if (target == null) return "";
		string authorId = author;
		string targetId = target;
		string resolvedRoundId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId);
		WorldDiplomacyRound activeRound = world.ResolveRound(resolvedRoundId);
		List<string> relevantKingdomIds = new List<string> { authorId, targetId };
		if (activeRound?.RelayRouteKingdomIds != null) relevantKingdomIds.AddRange(activeRound.RelayRouteKingdomIds);
		string gatheringSnapshot = world.BuildGatheringSnapshot(relevantKingdomIds, 3);
		StringBuilder sb = new StringBuilder();
        AppendPlayerContext(sb, world, orchestration, author, target, activeRound, sourceDocument);
		AppendDiplomaticAuthorDecisionContext(sb, world, author, resolvedRoundId);
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_065", "[Targets and legal actions for this document]"));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_066", "Primary target kingdom:") + world.KingdomName(target) + "（ID=" + targetId + world.LocalizePrompt("AF_DIP_Prompt_Composer_067", "); ruler:") + world.RulerName(target));
		List<string> legalActions = orchestration.BuildLegalDiplomaticDeclarationIntents(
			activeRound,
			author,
			target,
			isRelayTurn: false,
			resultSettlementSlotId: null,
			isExternalResponseOnly: isExternalResponseOnly,
			responseSource: sourceDocument);
		bool statementAllowed = legalActions.Contains("statement", StringComparer.OrdinalIgnoreCase);
		bool exclusivePeaceResponse = WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(legalActions);
		bool canProposePeace = legalActions.Any(x => string.Equals(
			WorldDiplomacyIntentVocabulary.NormalizeIntent(x),
			"propose_peace",
			StringComparison.OrdinalIgnoreCase));
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_068", "Legal actions for this document=") + string.Join("、", legalActions) + world.LocalizePrompt("AF_DIP_Prompt_Composer_069", ". Choose one of them."));
		if (exclusivePeaceResponse)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_070", "For the original peace offer, choose only accept_peace to accept unchanged or reject_peace to explicitly reject. Do not add or modify terms or propose another peace plan."));
		}
		WorldDiplomacyRoundOffer requiredPeaceOffer = world.FindRequiredPeaceOfferResponse(
			activeRound,
			author,
			resultSettlementSlotId: null,
			isExternalResponseOnly: isExternalResponseOnly,
			sourceDocumentId: sourceDocument?.DocumentId,
			requireAnyOpenPeaceOffer: false);
		if (requiredPeaceOffer != null)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_034", "This document must answer an original peace offer: source=") + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_035", "|proposing kingdom=") + requiredPeaceOffer.ProposerKingdomId + "。");
		}
		if (allowUntargeted)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_073", "The program has not locked in a target. Choose one or more legal candidates and perform one action per target."));
		}
		if (!string.IsNullOrWhiteSpace(activeRound?.ExternalOpeningContext))
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_074", "[External cause of this diplomatic event]"));
			sb.AppendLine(WorldDiplomacyTextRules.Limit(activeRound.ExternalOpeningContext, 1800));
		}
		if (!string.IsNullOrWhiteSpace(gatheringSnapshot))
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_051", "[Recent relevant feasts]"));
			sb.AppendLine(WorldDiplomacyTextRules.Limit(gatheringSnapshot, 900));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_076", "Feasts are merely public developments that rulers may use, assess or respond to. They do not predetermine a ruler's attitude or automatically produce diplomatic outcomes."));
		}
		AppendDiplomaticTargetDecisionContext(
			sb,
			world,
			activeRound,
			author,
			target,
			includePeaceNegotiationTerms: canProposePeace,
			legalActions: legalActions);
		AppendRulerCaptivityDecisionContext(sb, world, author, target);
		if (isResponse || isExternalResponseOnly || sourceDocument != null)
		{
			AppendOtherKingdomRelationshipContext(sb, world, author, new[] { targetId });
		}
		if (roundPlanCandidateIds != null && roundPlanCandidateIds.Count > 0)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_056", "[Determine participants for this diplomatic event in the same response]"));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_078", "Fill in round_plan while drafting the opening declaration. Maximum total participants, including the initiator=") + world.GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + world.LocalizePrompt("AF_DIP_Prompt_Composer_079", ". Kingdoms explicitly addressed by the declaration must be selected first. Select others only if they have real war, alliance, trade, security or political interests and can take diplomatic action; do not fill the roster merely for spectacle. Candidate summary:"));
			foreach (string candidateId in roundPlanCandidateIds)
			{
				string candidate = world.ResolveKingdom(candidateId);
				if (candidate == null) continue;
				sb.AppendLine(BuildCompactRoundPlanCandidateLine(world, author, candidate, activeRound,
					activeRound == null
						? orchestration.BuildPotentialDiplomaticActionIntents(author, candidate)
						: orchestration.BuildLegalDiplomaticActionIntents(activeRound, author, candidate)));
			}
		}
		if (activeRound != null)
		{
			int age = Math.Max(0, world.CurrentDay() - activeRound.StartedDay);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_080", "The current diplomatic event has lasted ") + age.ToString(CultureInfo.InvariantCulture) + world.LocalizePrompt("AF_DIP_Prompt_Composer_081", " days; the soft time scale is ") + Math.Max(1, activeRound.SoftEndDay - activeRound.StartedDay).ToString(CultureInfo.InvariantCulture) + world.LocalizePrompt("AF_DIP_Prompt_Composer_082", " days.")
				+ (statementAllowed
				? world.LocalizePrompt("AF_DIP_Prompt_Composer_083", "If statement is currently listed, you may perform a structured negotiation move. Fill in negotiation_move and introduce new substance; do not merely rephrase a position.")
				: world.LocalizePrompt("AF_DIP_Prompt_Composer_084", "When approaching or exceeding the soft time scale, choose a currently legal action that can conclude negotiations. Do not delay with empty declarations of a final position.")));
		}
		if (isResponse && sourceDocument != null)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_085", "The following public diplomatic declaration has been delivered. Respond using a currently legal action for this document."));
			string sourceActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(sourceDocument, author, world.LocalizePrompt);
			string sourcePeaceTerms = WorldDiplomacyDocumentFactRules.BuildPeaceOfferTermsFact(sourceDocument, author, world.LocalizePrompt);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_086", "Source document ID:") + sourceDocument.DocumentId + world.LocalizePrompt("AF_DIP_Prompt_Composer_087", "; proposing kingdom=") + sourceDocument.AuthorKingdomId
			+ (string.IsNullOrWhiteSpace(sourceActionFact) ? "" : world.LocalizePrompt("AF_DIP_Prompt_Composer_088", "; actions relevant to this kingdom=") + sourceActionFact));
		if (!string.IsNullOrWhiteSpace(sourcePeaceTerms)) sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_089", "Original peace offer terms=") + sourcePeaceTerms);
		if (requiredPeaceOffer != null)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_090", "Original offer: source=") + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_032", "; choose only accept_peace to accept unchanged or reject_peace to explicitly reject. Do not add or modify terms or propose another peace plan."));
		}
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_005", "Title=") + sourceDocument.Title);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_006", "Body=") + WorldDiplomacyTextRules.Limit(sourceDocument.Body, 2200));
		}
		if (isReminder)
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_094", "The target kingdom has not responded for some time. This document must still take an actual action; do not merely urge a response, complain or assume acceptance."));
		}
		int activity = world.GetActivityLevel();
		sb.AppendLine(activity switch
		{
		0 => world.LocalizePrompt("AF_DIP_Prompt_Composer_095", "Diplomatic activity is low: prefer restraint, caution and practical interests, but serious conflicts may still escalate."),
		2 => world.LocalizePrompt("AF_DIP_Prompt_Composer_096", "Diplomatic activity is high: more actively propose claims, cooperation or conflict plans that others can answer, but do not start wars without cause."),
		_ => world.LocalizePrompt("AF_DIP_Prompt_Composer_097", "Diplomatic activity is standard: act according to current interests and constraints.")

		});
		return sb.ToString();
	}

	internal static string BuildAnalysisPrompt(IWorldDiplomacyPromptWorld world,
		IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument document)
	{
		StringBuilder sb = new StringBuilder();
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_098", "Issuing kingdom:") + document.AuthorKingdomName + "（ID=" + document.AuthorKingdomId + "）");
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_099", "For annexation, tribute, garrison and full vassalage, use the propose_/accept_/reject_ actions for annexation/tributary/garrison/vassal respectively. New offers must include treaty_terms={receiving_kingdom_id:receiving or overlord kingdom ID,joining_kingdom_id:joining or subject kingdom ID}. Acceptance, rejection or withdrawal must accurately bind responding_to_offer_document_id and responding_to_offer_action_id. Use withdraw_offer only to withdraw this kingdom's own unaccepted original offer. The player's text is already public; ambiguity or an invalid action must not block the declaration."));
		string documentAuthor = world.ResolveKingdom(document.AuthorKingdomId);
		WorldDiplomacyRound analysisRound = world.ResolveRound(document.RoundId);
		if (document.IsPlayerAuthored) orchestration.PruneInvalidOffers(analysisRound);
		if (documentAuthor != null) AppendDiplomaticThreatAnalysisContext(sb, world, documentAuthor);
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
	if (document.IsPlayerAuthored && document.SubjectReleaseTokens?.Count > 0)
            sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_100", "Direct subject kingdom IDs when this declaration was submitted (use release_subject for explicit release):") + string.Join(",", document.SubjectReleaseTokens.Keys));
        sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_101", "Candidate target kingdoms:"));
		foreach (string kingdom in world.KingdomIds().Where(x => x != null && !world.IsEliminated(x) && !string.Equals(x, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			sb.AppendLine("- " + kingdom + " = " + world.KingdomName(kingdom));
		}
		if (!string.IsNullOrWhiteSpace(document.TargetKingdomId))
		{
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_102", "Current system candidate for primary target:") + document.TargetKingdomId + " = " + document.TargetKingdomName);
			string author = world.ResolveKingdom(document.AuthorKingdomId);
			string candidateTarget = world.ResolveKingdom(document.TargetKingdomId);
			if (author != null && candidateTarget != null && world.IsAtWar(author, candidateTarget))
			{
				bool canProposePeace = orchestration.BuildLegalDiplomaticActionIntents(analysisRound, author, candidateTarget)
					.Any(x => string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x), "propose_peace", StringComparison.OrdinalIgnoreCase));
				sb.AppendLine(BuildWarDecisionContext(world, author, candidateTarget, canProposePeace));
			}
		}
		if (document.IsPlayerAuthored)
		{
			WorldDiplomacyRoundOffer requiredPlayerPeaceOffer = world.FindRequiredPeaceOfferResponse(
				analysisRound,
				documentAuthor,
				document.ResultSettlementSlotId,
				isExternalResponseOnly: false,
				sourceDocumentId: document.SourceDocumentId,
				requireAnyOpenPeaceOffer: true);
            var analysisOffers = orchestration is WorldDiplomacyOrchestration live
                ? live.PlayerAnalysisOffers(document.AuthorKingdomId)
                : (analysisRound?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
                    .Where(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(x, document.AuthorKingdomId));
			List<WorldDiplomacyRoundOffer> openOffers = analysisOffers
				.OrderByDescending(x => requiredPlayerPeaceOffer != null
					&& WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceDocumentId, requiredPlayerPeaceOffer.SourceDocumentId)
					&& string.Equals(x.SourceActionId ?? "", requiredPlayerPeaceOffer.SourceActionId ?? "", StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(x => x.CreatedDay)
				.Take(13)
				.ToList();
			if (openOffers.Count > 0)
			{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_103", "Unresolved original offers known to this kingdom: its own may be withdrawn; foreign offers may be accepted or rejected. Changed terms constitute a new offer."));
				foreach (WorldDiplomacyRoundOffer offer in openOffers.Take(12))
				{
					WorldDiplomacyDocument source = world.ResolveDocument(offer.SourceDocumentId);
                    var treaty = WorldDiplomacyDocumentFactRules.ResolveDocumentAction(source, offer.SourceActionId)?.TreatyTerms ?? source?.TreatyTerms;
					bool isPeaceOffer = string.Equals(
						WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent),
						"propose_peace",
						StringComparison.OrdinalIgnoreCase);
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_104", "- Source=") + offer.SourceDocumentId + world.LocalizePrompt("AF_DIP_Prompt_Composer_105", "|action=") + offer.SourceActionId + world.LocalizePrompt("AF_DIP_Prompt_Composer_106", "|type=") + offer.Intent
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_035", "|proposing kingdom=") + offer.ProposerKingdomId + "=" + world.KingdomName(world.ResolveKingdom(offer.ProposerKingdomId))
                + world.LocalizePrompt("AF_DIP_Prompt_Composer_108", "|receiving kingdom=") + offer.TargetKingdomId
                + world.LocalizePrompt("AF_DIP_Prompt_Composer_109", "|treaty roles=") + treaty?.ReceivingKingdomId + "/" + treaty?.JoiningKingdomId
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_025", "|title=") + WorldDiplomacyTextRules.Limit(source?.Title, 80) + world.LocalizePrompt("AF_DIP_Prompt_Composer_111", "|summary=") + WorldDiplomacyTextRules.Limit(source?.Body, 240)
						+ (isPeaceOffer
					? world.LocalizePrompt("AF_DIP_Prompt_Composer_112", "|original terms=") + WorldDiplomacyOfferContractRules.FormatPeaceTermsForPrompt(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offer.SourceActionId), world.LocalizePrompt)
						+ world.LocalizePrompt("AF_DIP_Prompt_Composer_113", "|response=accept unchanged or explicitly reject")
							: ""));
				}
                if (openOffers.Count > 12) sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_114", "Other original offers are not listed; do not assume uniqueness or guess source IDs."));
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_115", "Acceptance, rejection or withdrawal must bind the source document and action IDs. Acceptance inherits the original terms; changed terms are extracted as a new offer. Extract only the player's meaning here; do not judge the player's expression using round-turn limits. If there is only commentary and no actual action, return statement or condemn."));
			}
		}
		WorldDiplomacyDocument sourceDocument = world.ResolveDocument(document.SourceDocumentId);
		if (sourceDocument != null)
		{
			sb.AppendLine(document.IsPlayerAuthored
                ? world.LocalizePrompt("PlayerReply_AdvisoryContext", "The player opened drafting from the following document, which provides background only. Whether to respond, whom to respond to and what action to take are determined by the player's text. This entry does not revive an old proposal or force participation in the original negotiations:")
                : world.LocalizePrompt("AF_DIP_Prompt_Composer_116", "This document responds to:"));
            sb.AppendLine(world.LocalizePrompt("PlayerReply_ContextDocumentId", "Background document ID: ") + sourceDocument.DocumentId);
			string sourceActionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(sourceDocument, document.AuthorKingdomId, world.LocalizePrompt);
			string sourcePeaceTerms = WorldDiplomacyDocumentFactRules.BuildPeaceOfferTermsFact(sourceDocument, document.AuthorKingdomId, world.LocalizePrompt);
		if (!string.IsNullOrWhiteSpace(sourceActionFact)) sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_117", "Actions relevant to this kingdom=") + sourceActionFact);
		if (!string.IsNullOrWhiteSpace(sourcePeaceTerms)) sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_089", "Original peace offer terms=") + sourcePeaceTerms);
		sb.AppendLine(sourceDocument.AuthorKingdomName + "《" + sourceDocument.Title + "》：" + WorldDiplomacyTextRules.Limit(sourceDocument.Body, 1400));
		}
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_119", "Document title:") + document.Title);
	sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_120", "Document body:") + document.Body);
	sb.AppendLine("【MODE=ANALYZE】");
		sb.AppendLine(WorldDiplomacyPromptContractRules.BuildAnalysisModeContract(world.LocalizePrompt));
		return sb.ToString().TrimEnd();
	}
	internal static string BuildFallbackAnalysisJson(WorldDiplomacyDocument document, string targetKingdomId)
	{
		return new JObject
		{
			["status"] = "fallback",
			["title_summary"] = WorldDiplomacyTextRules.BuildFallbackDocumentTitle(document, "statement", DiplomacyPromptTexts.Get),
			["responding_to_offer_document_id"] = "",
			["responding_to_threat_document_id"] = "",
			["primary_target_kingdom_id"] = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document?.TargetKingdomId, targetKingdomId),
			["addressed_kingdom_ids"] = new JArray(),
			["mentioned_kingdom_ids"] = new JArray(),
			["intent"] = "statement",
			["commitment"] = "non_binding",
			["requires_response"] = false,
			["tone"] = "neutral",
			["confidence"] = 0.0,
			["international_reputation_delta"] = 0,
		["international_reputation_reason"] = DiplomacyMiscPromptTexts.Get("PromptComposer_SemanticEvalFallback", "Semantic analysis service did not complete evaluation; assigned non-zero score by local structured rules.")
		}.ToString(Formatting.None);
	}

	// Prompt section policy migrated from the host: every branch, ordering, and
	// candidate filter below is owned here; the world port supplies leaf facts only.
	private static void AppendDiplomaticThreatDynamicContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, string authorId, string roundId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		IReadOnlyList<WorldDiplomacyThreat> threats = world.DiplomaticThreats() ?? (IReadOnlyList<WorldDiplomacyThreat>)new List<WorldDiplomacyThreat>();
		int prestige = world.NationalPrestige(authorId);
		int reputation = world.InternationalReputation(authorId);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_122", "[This kingdom's national prestige, international reputation trends and unresolved threats; internal dynamic facts: do not publish the numbers in the document]"));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_123", "Current national prestige=") + prestige.ToString(CultureInfo.InvariantCulture)
			+ "/100：" + WorldDiplomacyReputationRules.DescribeNationalPrestige(prestige, world.LocalizePrompt) + "）");
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_124", "National prestige measures whether this kingdom follows through on threats and commitments. Low prestige undermines the credibility of threats and dynamically reduces formal vassal clan leaders' relations with the ruler according to the prestige tier. Restored prestige removes this dynamic relation penalty."));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_125", "Current international reputation=") + reputation.ToString(CultureInfo.InvariantCulture)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_126", "/100 (foreign public assessment tier=") + WorldDiplomacyReputationRules.DescribeInternationalReputation(reputation, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_127", "); current natural trend=") + WorldDiplomacyReputationRules.DescribeInternationalReputationNaturalTrend(reputation, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_128", ". Use the exact value, tier and trend to plan how to maintain, repair or spend this strategic capital for core interests. When the actual situation allows it, you may proactively issue substantive declarations to maintain reputation. Do not stay silent, avoid legitimate positions, mechanically switch actions or publish empty rhetoric just for reputation."));
		List<string> recentReputationReasons = WorldDiplomacyTextRules.GetRecentOwnInternationalReputationReasons(
				world.Documents(), authorId, world.CurrentDay(), world.NegativeReputationFactRetentionDays(), world.FormatCampaignDate, world.LocalizePrompt);
		if (recentReputationReasons.Count == 0)
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_129", "There are no recent settled international reputation events for this kingdom to review; do not invent reasons for gains or losses."));
		}
		else
		{
			foreach (string recentReason in recentReputationReasons)
			{
				sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_130", "Recent international reputation facts=") + recentReason + "。");
			}
		}
		List<WorldDiplomacyStandingChange> recentChanges = WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency((world.Documents() ?? (IReadOnlyList<WorldDiplomacyDocument>)new List<WorldDiplomacyDocument>())
				.Where(x => x?.DiplomaticStandingChanges != null && x.IsReadyForPublication))
			.SelectMany(x => x.DiplomaticStandingChanges.AsEnumerable().Reverse())
			.Where(x => x != null
				&& string.Equals(x.KingdomId, authorId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase))
			.Take(4)
			.ToList();
		foreach (WorldDiplomacyStandingChange change in recentChanges)
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_131", "Recent national prestige settlement=") + WorldDiplomacyReputationRules.FormatSignedDelta(change.Delta) + world.LocalizePrompt("AF_DIP_Prompt_Composer_132", "; reason=") + change.Reason + "。");
		}

		WorldDiplomacyThreat outbound = threats.FirstOrDefault(x => WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(x?.Status)
			&& string.Equals(x.IssuerKingdomId, authorId, StringComparison.OrdinalIgnoreCase));
		if (WorldDiplomacyRoundLifecycleRules.IsThreatDecisionNoncomplied(outbound))
		{
			WorldDiplomacyDocument source = world.ResolveDocument(outbound.StageDocumentId);
			if (WorldDiplomacyRoundLifecycleRules.IsThreatAtStage(outbound, "warning"))
			{
				sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_133", "Mandatory consequence notice:") + world.KingdomName(outbound.TargetKingdomId) + "（ID=" + outbound.TargetKingdomId
					+ world.LocalizePrompt("AF_DIP_Prompt_Composer_134", ") has decided not to comply with this kingdom's condemnation. This is this kingdom's next declaration after that condemnation. Prefer escalating to a war ultimatum against that kingdom (intent=ultimatum); otherwise 20 national prestige will be deducted immediately after publication. The ultimatum must continue the same military dispute and core demand, not switch issues. Original condemnation title=")
					+ WorldDiplomacyTextRules.Limit(source?.Title, 80) + world.LocalizePrompt("AF_DIP_Prompt_Composer_135", "; original condemnation summary=") + WorldDiplomacyTextRules.Limit(source?.Body, 260) + "。");
			}
			else
			{
				sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_133", "Mandatory consequence notice:") + world.KingdomName(outbound.TargetKingdomId) + "（ID=" + outbound.TargetKingdomId
					+ world.LocalizePrompt("AF_DIP_Prompt_Composer_137", ") has decided not to comply with this kingdom's ultimatum. This is this kingdom's next declaration after that ultimatum. Prefer declaring war on that kingdom (intent=declare_war); otherwise 15 national prestige will be deducted immediately after publication. Still consider the consequences of war."));
			}
		}
		else if (outbound != null)
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_138", "This kingdom already has a pending ")
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStageFormal(outbound.Stage)
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_139", ": target=") + outbound.TargetKingdomId + "=" + world.KingdomName(outbound.TargetKingdomId)
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_140", ", source=") + outbound.StageDocumentId + world.LocalizePrompt("AF_DIP_Prompt_Composer_141", ". The target kingdom has not yet published its one-time decision; do not repeat or escalate this threat before it does."));
		}

		foreach (WorldDiplomacyThreat incoming in WorldDiplomacyRoundLifecycleRules.SelectPendingIncomingThreats(
			threats, authorId))
		{
			WorldDiplomacyDocument source = world.ResolveDocument(incoming.StageDocumentId);
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_142", "Unresolved threat received by this kingdom:")
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(
					WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage(incoming.Stage))
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_143", ": issuing kingdom=") + incoming.IssuerKingdomId + "=" + world.KingdomName(incoming.IssuerKingdomId)
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_144", "; source=") + incoming.StageDocumentId
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_145", "; title=") + WorldDiplomacyTextRules.Limit(source?.Title, 80)
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_146", "; summary=") + WorldDiplomacyTextRules.Limit(source?.Body, 260)
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_147", ". Choosing intent=comply_ultimatum means unconditional submission. Any other intent means non-compliance, and the decision cannot later be reversed. Submission reduces this kingdom's national prestige and lowers every formal vassal clan's relation with the current ruling clan by 20, potentially leading to civil war. Weigh the costs and benefits against the situation, war, national character and long-term strategy."));
			if (!string.IsNullOrWhiteSpace(incoming.PolicyConditionPolicyId))
			{
				sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_148", "Attached policy condition: if this kingdom chooses comply_ultimatum, ")
					+ WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(incoming.PolicyConditionPolicyName, incoming.PolicyConditionPolicyId)
					+ world.LocalizePrompt("AF_DIP_Prompt_Composer_149", " will be canceled by the game mechanism."));
			}
		}

		foreach (WorldDiplomacyThreat notice in WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices(
			threats, authorId, null, 4))
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_150", "Confirmed: ") + world.KingdomName(notice.TargetKingdomId) + world.LocalizePrompt("AF_DIP_Prompt_Composer_151", " has explicitly complied with this kingdom's previous ")
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(
					WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage(notice.Stage))
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_152", ". Subsequent declarations need not declare war or escalate that threat further, and no national prestige will be deducted for it.")
				+ (WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled(notice.PolicyConditionCancellationStatus)
					? world.LocalizePrompt("AF_DIP_Prompt_Composer_153", "The attached policy '") + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(notice.PolicyConditionPolicyName, notice.PolicyConditionPolicyId) + world.LocalizePrompt("AF_DIP_Prompt_Composer_154", "' has been canceled.")
					: ""));
		}
	}
	private static void AppendDiplomaticThreatAnalysisContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, string authorId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		List<WorldDiplomacyThreat> incoming = WorldDiplomacyRoundLifecycleRules.SelectPendingIncomingThreats(
			world.DiplomaticThreats(), authorId);
		if (incoming.Count == 0) return;
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_155", "Unresolved threats currently available for semantic adjudication to bind:"));
		foreach (WorldDiplomacyThreat threat in incoming)
		{
			WorldDiplomacyDocument source = world.ResolveDocument(threat.StageDocumentId);
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_104", "- Source=") + threat.StageDocumentId + world.LocalizePrompt("AF_DIP_Prompt_Composer_106", "|type=") + threat.Stage
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_158", "|issuing kingdom=") + threat.IssuerKingdomId + "=" + world.KingdomName(threat.IssuerKingdomId)
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_025", "|title=") + WorldDiplomacyTextRules.Limit(source?.Title, 80) + world.LocalizePrompt("AF_DIP_Prompt_Composer_111", "|summary=") + WorldDiplomacyTextRules.Limit(source?.Body, 260));
		}
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_161", "Adjudicate comply_ultimatum and bind that source only when the player's body text explicitly, fully and unconditionally submits to one of these threats on behalf of this kingdom. This is a one-time decision: partial or in-principle acceptance, added demands, counterconditions, silence, third-country narration or any other intent immediately counts as non-compliance."));
	}
	private static void AppendDiplomaticAuthorDecisionContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		string authorId,
		string roundId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_162", "[Stable profile of the issuing party]"));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_098", "Issuing kingdom:") + world.KingdomName(authorId) + "（ID=" + authorId + world.LocalizePrompt("AF_DIP_Prompt_Composer_067", "); ruler:") + world.RulerName(authorId));
		string vassalageSnapshot = world.BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		List<string> currentWars = (world.CurrentWarKingdomIds(authorId) ?? (IReadOnlyList<string>)new List<string>())
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.Select(x => x + "=" + world.KingdomName(x))
			.ToList();
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_165", "This kingdom is currently at war with=") + (currentWars.Count == 0 ? "[]" : "[" + string.Join(",", currentWars) + "]") + world.LocalizePrompt("AF_DIP_Prompt_Composer_166", ". This states only the war status; it does not authorize diplomatic actions outside the listed options."));
		AppendRulerCaptivityDecisionContext(sb, world, authorId, null);
		AppendDiplomaticThreatDynamicContext(sb, world, authorId, roundId);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_167", "[Issuing ruler's personality and voice]"));
		sb.AppendLine(world.RulerVoiceContext(authorId));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_168", "Express the position according to this ruler’s character, situation and actual priorities, using the institutional facts below and the shared writing guidance."));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_169", "[Institutional, legitimacy and ceremonial voice of the issuing kingdom]"));
		sb.AppendLine(world.RealmInstitutionalVoiceContext(authorId));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_170", "Hard facts about current in-game identities and government take precedence over retrieved background. Background may supplement tone, but must not alter ruler titles or government or invent institutions."));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_171", "[Authoritative people and family relationships]"));
		sb.AppendLine(world.AuthorRulerFamilyContext(authorId));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_172", "Only the direct family relationships listed in this section are facts. Use them only when this diplomacy actually concerns dynasties, marriage alliances, hostages or royal security."));
		string policySnapshot = world.BuildPolicySnapshot(authorId);
		if (!string.IsNullOrWhiteSpace(policySnapshot))
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_173", "[Issuing kingdom's policy snapshot]"));
			sb.AppendLine(policySnapshot);
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_174", "Policies inform current goals, interests and pressures; they do not prove diplomatic or military outcomes that have not been explicitly supplied."));
		}
	}
	private static void AppendDiplomaticTargetDecisionContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		WorldDiplomacyRound round,
		string authorId,
		string targetId,
		bool includePeaceNegotiationTerms,
		IReadOnlyCollection<string> legalActions)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(targetId)
			|| string.Equals(authorId, targetId, StringComparison.OrdinalIgnoreCase)) return;
		WorldDiplomacyRealmRelationProfile relationProfile = world.RelationProfile(authorId, targetId);
		WorldDiplomacyBorderRelation border = world.BorderRelation(authorId, targetId);
		WarSituationSnapshot situation = world.WarSituation(authorId, targetId);
		string bilateralFamily = world.BilateralRulerFamilyContext(authorId, targetId);
		string recentBattles = world.RecentBilateralBattleContext(authorId, targetId);
		string nativeReasons = world.RecentNativeSignalContext(authorId, targetId);
		string targetPolicy = world.BuildPolicySnapshot(targetId);
		int relation = world.RulerRelation(authorId, targetId);
		int culturalFiefs = world.CulturalClaimCount(authorId, targetId);
		int pressure = world.WarPressure(authorId, targetId);
		bool peaceTermsVisible = includePeaceNegotiationTerms
			&& !WorldDiplomacyRoundLifecycleRules.IsImmediateWarResponsePeaceSuppressed(round, round?.ResultSettlementCurrentSlotId,
				authorId, targetId, world.ResolveDocument);

		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_175", "[Hard facts for decisions about the target]") + targetId + "】");
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_176", "Target kingdom=") + world.KingdomName(targetId) + "（ID=" + targetId + world.LocalizePrompt("AF_DIP_Prompt_Composer_177", "); ruler=") + world.RulerName(targetId));
		int targetPrestige = world.NationalPrestige(targetId);
		int targetReputation = world.InternationalReputation(targetId);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_178", "Target kingdom's national prestige=") + targetPrestige.ToString(CultureInfo.InvariantCulture)
			+ "/100：" + WorldDiplomacyReputationRules.DescribeNationalPrestige(targetPrestige, world.LocalizePrompt) + world.LocalizePrompt("AF_DIP_Prompt_Composer_179", "); foreign public international reputation tier for this kingdom=")
			+ WorldDiplomacyReputationRules.DescribeInternationalReputation(targetReputation, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_180", ". Low national prestige makes its threats less credible, but may also drive it to riskier follow-through to avoid losing more prestige. International reputation informs the credibility of commitments, terms of cooperation and diplomatic risk; it does not imply friendliness, a preference for peace or immunity from war declarations."));
		string reputationConflictOpportunity = WorldDiplomacyTextRules.BuildLowReputationConflictOpportunityContext(
				targetReputation, legalActions, world.Documents(), targetId,
				world.CurrentDay(), world.NegativeReputationFactRetentionDays(), world.FormatCampaignDate, world.LocalizePrompt);
		if (!string.IsNullOrWhiteSpace(reputationConflictOpportunity))
		{
			sb.AppendLine(reputationConflictOpportunity);
		}
		if (!string.IsNullOrWhiteSpace(bilateralFamily)) sb.AppendLine(bilateralFamily);
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_181", "Current relations=") + BilateralStateLabel(world, authorId, targetId)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_182", "; overall relations between the two kingdoms' nobles=") + WorldDiplomacyTextRules.DescribeRealmRelationProfile(relationProfile, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_183", "; rulers' personal relations=") + WorldDiplomacyTextRules.DescribeRulerRelation(relation, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_184", "; geography=") + (border.SharesBorder ? WorldDiplomacyTextRules.DescribeBorderRelation(border, world.LocalizePrompt) : world.LocalizePrompt("AF_DIP_Prompt_Composer_185", "No shared border"))
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_186", "; overall military strength=") + WorldDiplomacyTextRules.DescribeStrengthBalance(situation.AuthorStrength, situation.TargetStrength, world.LocalizePrompt) + "。");
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_187", "Number of the issuing kingdom's culturally affiliated towns and castles held by the target=") + culturalFiefs.ToString(CultureInfo.InvariantCulture)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_188", "; border and political pressure=") + WorldDiplomacyTextRules.DescribeWarPressure(pressure, world.LocalizePrompt) + world.LocalizePrompt("AF_DIP_Prompt_Composer_189", ". These are for the court's judgment only; do not express them as scores or thresholds."));
		if (!string.IsNullOrWhiteSpace(targetPolicy))
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_190", "Target kingdom's policies=") + WorldDiplomacyTextRules.Limit(targetPolicy, 700));
		}
		if (!string.IsNullOrWhiteSpace(nativeReasons))
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_191", "Recent native diplomatic motivation material]"));
			sb.AppendLine(WorldDiplomacyTextRules.Limit(nativeReasons, 800));
		}
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_192", "Recent bilateral battle facts."));
		sb.AppendLine(WorldDiplomacyTextRules.Limit(recentBattles, 1500));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_193", "Refer to specific battles only using the hard facts above. Do not invent unlisted battles, outcomes, troop counts, casualties or prisoners."));
		if (situation?.IsAtWar == true)
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_194", "Hard war status: the parties are already at war; do not declare war again."));
			sb.AppendLine(BuildWarDecisionContext(world, authorId, targetId, peaceTermsVisible));
		}
		else
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_195", "Hard war status: the parties are not currently at war. Historical hostility, unification claims or border friction do not mean war already exists."));
		}
	}
	private static void AppendRelayResponseSourceContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		WorldDiplomacyRound round,
		string authorId,
		WorldDiplomacyDocument responseSource,
		string requiredSourceDocumentId)
	{
		if (string.IsNullOrWhiteSpace(authorId)) return;
		WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
			sb, round, authorId, responseSource, requiredSourceDocumentId,
			world.Documents(), world.LocalizePrompt);
	}
	private static string BuildCompactRoundPlanCandidateLine(
		IWorldDiplomacyPromptWorld world,
		string authorId,
		string candidateId,
		WorldDiplomacyRound round,
		IReadOnlyList<string> legalActions)
	{
		List<string> actions = legalActions as List<string> ?? legalActions?.ToList() ?? new List<string>();
		string line = BuildCompactDiplomaticRelationshipLine(world, authorId, candidateId)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_196", "; available actions=") + WorldDiplomacyPromptContractRules.DescribePotentialDiplomaticActions(actions, world.LocalizePrompt);
		string captivityHint = BuildRulerCaptivityTargetHint(world, authorId, candidateId);
		if (!string.IsNullOrWhiteSpace(captivityHint)) line += "\n  " + captivityHint;
		string reputationConflictOpportunity = string.IsNullOrWhiteSpace(candidateId) ? "" : WorldDiplomacyTextRules.BuildLowReputationConflictOpportunityContext(
				world.InternationalReputation(candidateId), actions, world.Documents(), candidateId,
				world.CurrentDay(), world.NegativeReputationFactRetentionDays(), world.FormatCampaignDate, world.LocalizePrompt);
		return string.IsNullOrWhiteSpace(reputationConflictOpportunity)
			? line
			: line + "\n  " + reputationConflictOpportunity;
	}
	private static void AppendOtherKingdomRelationshipContext(
		StringBuilder sb,
		IWorldDiplomacyPromptWorld world,
		string authorId,
		IEnumerable<string> detailedTargetIds)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		HashSet<string> excludedIds = new HashSet<string>(
			(detailedTargetIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase)
		{
			authorId
		};
		List<string> otherKingdomIds = (world.IndependentKingdomIds() ?? (IReadOnlyList<string>)new List<string>())
			.Where(x => !excludedIds.Contains(x))
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (otherKingdomIds.Count == 0) return;

		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_197", "[Relations between this kingdom and other kingdoms]"));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_198", "The following information is for overall judgment only and grants no extra actions. Action targets are still limited to those currently available for this document."));
		foreach (string otherId in otherKingdomIds)
		{
			sb.AppendLine(BuildCompactDiplomaticRelationshipLine(world, authorId, otherId));
			if (world.IsAtWar(authorId, otherId))
			{
				sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_199", "  War situation=") + WorldDiplomacyTextRules.CompactPromptFact(BuildWarDecisionContext(world, authorId, otherId, false), 650));
			}
		}
	}
	private static string BuildCompactDiplomaticRelationshipLine(IWorldDiplomacyPromptWorld world, string authorId, string candidateId)
	{
		if (string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(candidateId)) return "";
		string policy = WorldDiplomacyTextRules.CompactPromptFact(world.BuildPolicySnapshot(candidateId), 180);
		StringBuilder sb = new StringBuilder();
		WorldDiplomacyRealmRelationProfile relationProfile = world.RelationProfile(authorId, candidateId);
		WorldDiplomacyBorderRelation border = world.BorderRelation(authorId, candidateId);
		WarSituationSnapshot strengthSituation = world.WarSituation(authorId, candidateId);
		int candidateReputation = world.InternationalReputation(candidateId);
		sb.Append("- ").Append(candidateId).Append('=').Append(world.KingdomName(candidateId))
			.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_200", "; with this kingdom=")).Append(BilateralStateLabel(world, authorId, candidateId))
			.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_182", "; overall relations between the two kingdoms' nobles=")).Append(WorldDiplomacyTextRules.DescribeRealmRelationProfile(relationProfile, world.LocalizePrompt))
			.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_183", "; rulers' personal relations=")).Append(WorldDiplomacyTextRules.DescribeRulerRelation(world.RulerRelation(authorId, candidateId), world.LocalizePrompt))
			.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_184", "; geography=")).Append(border.SharesBorder ? WorldDiplomacyTextRules.DescribeBorderRelation(border, world.LocalizePrompt) : world.LocalizePrompt("AF_DIP_Prompt_Composer_185", "No shared border"))
			.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_186", "; overall military strength=")).Append(WorldDiplomacyTextRules.DescribeStrengthBalance(strengthSituation.AuthorStrength, strengthSituation.TargetStrength, world.LocalizePrompt))
			.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_206", "; national prestige=")).Append(world.NationalPrestige(candidateId).ToString(CultureInfo.InvariantCulture))
			.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_207", "; foreign public international reputation tier=")).Append(WorldDiplomacyReputationRules.DescribeInternationalReputation(candidateReputation, world.LocalizePrompt));
		if (!string.IsNullOrWhiteSpace(policy)) sb.Append(world.LocalizePrompt("AF_DIP_Prompt_Composer_208", "; policy leanings=")).Append(policy);
		return sb.ToString();
	}
	private static string BuildWarDecisionContext(
		IWorldDiplomacyPromptWorld world,
		string authorId,
		string targetId,
		bool includePeaceNegotiationTerms)
	{
		WarSituationSnapshot snapshot = world.WarSituation(authorId, targetId);
		if (snapshot?.IsAtWar != true) return "";
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_209", "[War situation for the ruler's judgment only] The war has ") + WorldDiplomacyTextRules.DescribeWarDuration(snapshot.WarDays, world.DaysPerYear(), world.LocalizePrompt) + "。");
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_210", "Overall military strength of both sides=") + WorldDiplomacyTextRules.DescribeStrengthBalance(snapshot.AuthorStrength, snapshot.TargetStrength, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_211", "; recent course of the war=") + WorldDiplomacyTextRules.DescribeWarProgress(snapshot.AuthorProgress, snapshot.TargetProgress, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_212", "; issuing kingdom=") + WorldDiplomacyTextRules.DescribeOtherWarBurden(snapshot.AuthorOtherWars, world.LocalizePrompt)
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_213", "; target kingdom=") + WorldDiplomacyTextRules.DescribeOtherWarBurden(snapshot.TargetOtherWars, world.LocalizePrompt) + world.LocalizePrompt("AF_DIP_Prompt_Composer_214", ". These are overall assessments. Render them only in in-world language; do not disclose scores, score differences, openness values or military strength numbers."));
		if (includePeaceNegotiationTerms)
		{
			IReadOnlyList<string> targetCanCede = world.CessionCandidates(targetId, authorId, snapshot.TargetCessionScore);
			IReadOnlyList<string> authorCanCede = world.CessionCandidates(authorId, targetId, snapshot.AuthorCessionScore);
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_215", "[Peace terms: use only when peace actions are available for this document] Peace pressure on the issuing kingdom=") + WorldDiplomacyTextRules.DescribePeacePressure(snapshot.AuthorPeacePressure, world.LocalizePrompt)
				+ world.LocalizePrompt("AF_DIP_Prompt_Composer_216", "; peace pressure on the target kingdom=") + WorldDiplomacyTextRules.DescribePeacePressure(snapshot.TargetPeacePressure, world.LocalizePrompt) + "。");
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_217", "Tribute and territorial cessions may coexist. Indicative daily tribute: if the issuing kingdom pays, approximately ") + snapshot.AuthorSuggestedTribute + world.LocalizePrompt("AF_DIP_Prompt_Composer_218", "; if the target kingdom pays, approximately ") + snapshot.TargetSuggestedTribute + world.LocalizePrompt("AF_DIP_Prompt_Composer_219", ". Negotiation is allowed, but do not exceed the legal ceilings supplied by the task."));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_220", "Fiefs the target kingdom may currently legally offer to cede to the issuing kingdom=") + WorldDiplomacyTextRules.FormatCessionCandidates(targetCanCede) + world.LocalizePrompt("AF_DIP_Prompt_Composer_221", "; fiefs the issuing kingdom may currently legally offer to cede to the target kingdom=") + WorldDiplomacyTextRules.FormatCessionCandidates(authorCanCede) + world.LocalizePrompt("AF_DIP_Prompt_Composer_222", ". If the list is empty, do not propose or agree to territorial cessions or invent settlement names. Prioritize lost territory not yet recovered in this war. Towns appear in the list only when the war situation is severely unfavorable."));
		}
		return sb.ToString().TrimEnd();
	}
	private static string BuildRulerCaptivityTargetHint(IWorldDiplomacyPromptWorld world, string authorId, string targetId)
	{
		WorldDiplomacyRulerCaptivity captivity = world.AuthorRulerCaptivity(authorId);
		if (captivity == null || !captivity.IsPrisoner) return "";
		bool holderKnown = !string.IsNullOrWhiteSpace(captivity.HolderKingdomId);
		if (holderKnown && string.Equals(captivity.HolderKingdomId, targetId, StringComparison.OrdinalIgnoreCase))
			return world.LocalizePrompt("AF_DIP_Prompt_Composer_223", "The ruler is imprisoned or controlled by the current target kingdom: prioritize a truce, peace and executable concessions more strongly, but do not bypass currently legal actions.");
		if (holderKnown)
			return world.LocalizePrompt("AF_DIP_Prompt_Composer_224", "The ruler is imprisoned or controlled by another kingdom: this kingdom's overall position has worsened. Give stability and negotiation greater weight; do not mistake the current target for the captor.");
		return world.LocalizePrompt("AF_DIP_Prompt_Composer_225", "The ruler is a prisoner, but the imprisoning or controlling party is unknown: this kingdom's position has worsened. Give stability and negotiation greater weight; do not guess the captor.");
	}
	private static void AppendRulerCaptivityDecisionContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, string authorId, string targetId)
	{
		if (sb == null || string.IsNullOrWhiteSpace(authorId)) return;
		WorldDiplomacyRulerCaptivity captivity = world.AuthorRulerCaptivity(authorId);
		if (captivity == null || !captivity.IsPrisoner) return;
		bool holderKnown = !string.IsNullOrWhiteSpace(captivity.HolderKingdomId);
		bool currentTargetIsHolder = holderKnown && !string.IsNullOrWhiteSpace(targetId)
			&& string.Equals(captivity.HolderKingdomId, targetId, StringComparison.OrdinalIgnoreCase);
		string pressure = string.IsNullOrWhiteSpace(targetId)
			? world.LocalizePrompt("AF_DIP_Prompt_Composer_226", "Assess in relation to the specific diplomatic target")
			: currentTargetIsHolder ? world.LocalizePrompt("AF_DIP_Prompt_Composer_227", "Yes") : holderKnown ? world.LocalizePrompt("AF_DIP_Prompt_Composer_228", "No") : world.LocalizePrompt("AF_DIP_Prompt_Composer_001", "Unknown");
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_230", "[Current circumstances of this kingdom's ruler]"));
		sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_231", "This kingdom's ruler is a prisoner: yes; current imprisoning/controlling party=")
			+ (holderKnown ? captivity.HolderKingdomName + "（ID=" + captivity.HolderKingdomId + "）" : world.LocalizePrompt("AF_DIP_Prompt_Composer_001", "Unknown"))
			+ world.LocalizePrompt("AF_DIP_Prompt_Composer_233", "; captivity pressure concerning this document's target=") + pressure + "。");
		if (string.IsNullOrWhiteSpace(targetId))
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_234", "The ruler's captivity increases this kingdom's concern for stability, truces and negotiations. If this document's target is the actual imprisoning or controlling party, give concessions and compromise even greater weight. Do not guess an unknown captor or bypass currently legal actions."));
		}
		else if (currentTargetIsHolder)
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_235", "The ruler is imprisoned or controlled by the current diplomatic target. This kingdom is at a clear disadvantage; give greater weight to a truce, peace, concessions and avoiding a wider war. Terms less favorable than usual may be accepted if executable. This does not authorize unconditional acceptance of nonexistent offers or illegal terms, or bypassing currently legal actions."));
		}
		else if (holderKnown)
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_236", "This kingdom's ruler is imprisoned or controlled by another kingdom. Its overall position has worsened; reduce pointless diplomatic escalation and give stability and negotiation greater weight. Do not automatically accept the current target's terms or assume that target is the captor."));
		}
		else
		{
			sb.AppendLine(world.LocalizePrompt("AF_DIP_Prompt_Composer_237", "This kingdom's ruler is a prisoner, but the current imprisoning/controlling party cannot be reliably confirmed. Its position has worsened; give stability and negotiation greater weight. Do not guess the captor or automatically assume the current target is the captor."));
		}
	}
}
