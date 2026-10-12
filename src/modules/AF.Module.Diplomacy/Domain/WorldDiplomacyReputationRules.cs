using AnimusForge;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyReputationRules
{
	internal const int DefaultNationalPrestige = 100;
	internal const int DefaultInternationalReputation = 50;
	internal const int MaximumInternationalReputationChangePerDocument = 10;
	internal const int InternationalReputationNaturalAnchor = 20;
	internal const int InternationalReputationFastDecayMinimum = 71;
	internal const int InternationalReputationNormalDecayMinimum = 51;
	internal const int InternationalReputationSlowDecayMinimum = 21;
	internal const int InternationalReputationDailyIntervalDays = 1;
	internal const int InternationalReputationFastDecayStep = 3;
	internal const int InternationalReputationNormalDecayStep = 2;

	public static int CalculateInternationalReputationNaturalChange(
		int currentReputation,
		int elapsedDays,
		out int consumedDays)
	{
		int reputation = Math.Max(0, Math.Min(100, currentReputation));
		int remainingDays = Math.Max(0, elapsedDays);
		consumedDays = 0;
		while (remainingDays > 0 && reputation != InternationalReputationNaturalAnchor)
		{
			int intervalDays;
			int step;
			int maximumTicksInBand;
			if (reputation >= InternationalReputationFastDecayMinimum)
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = -InternationalReputationFastDecayStep;
				maximumTicksInBand = (reputation - (InternationalReputationFastDecayMinimum - 1)
					+ InternationalReputationFastDecayStep - 1) / InternationalReputationFastDecayStep;
			}
			else if (reputation >= InternationalReputationNormalDecayMinimum)
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = -InternationalReputationNormalDecayStep;
				maximumTicksInBand = (reputation - (InternationalReputationNormalDecayMinimum - 1)
					+ InternationalReputationNormalDecayStep - 1) / InternationalReputationNormalDecayStep;
			}
			else if (reputation >= InternationalReputationSlowDecayMinimum)
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = -1;
				maximumTicksInBand = reputation - InternationalReputationNaturalAnchor;
			}
			else
			{
				intervalDays = InternationalReputationDailyIntervalDays;
				step = 1;
				maximumTicksInBand = InternationalReputationNaturalAnchor - reputation;
			}

			int availableTicks = remainingDays / intervalDays;
			if (availableTicks <= 0) break;
			int appliedTicks = Math.Min(availableTicks, maximumTicksInBand);
			reputation = Math.Max(0, Math.Min(100, reputation + (step * appliedTicks)));
			int segmentDays = appliedTicks * intervalDays;
			remainingDays -= segmentDays;
			consumedDays += segmentDays;
		}
		return reputation;
	}

	public static string DescribeInternationalReputationNaturalTrend(int value, Func<string, string, string> localize = null)
	{
		if (value >= InternationalReputationFastDecayMinimum) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_0", "Naturally decreases by 3 points daily; requires continued upkeep through actual results");
		if (value >= InternationalReputationNormalDecayMinimum) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_1", "Naturally decreases by 2 points daily; requires active upkeep through actual conduct");
		if (value >= InternationalReputationSlowDecayMinimum) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_2", "Naturally decreases by 1 point daily, gradually returning to normal");
		if (value == InternationalReputationNaturalAnchor) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_3", "Remains stable");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_4", "Naturally recovers by 1 point daily until the stable anchor; actual conduct is still required for a higher assessment");
	}

	public static string DescribeInternationalReputation(int value, Func<string, string, string> localize = null)
	{
		if (value >= 80) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_5", "Widely trusted");
		if (value >= 60) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_6", "Well regarded");
		if (value >= 40) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_7", "Mixed reputation");
		if (value >= 20) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_8", "Poor reputation");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_9", "Widely distrusted");
	}

	public static string DescribeNationalPrestige(int value, Func<string, string, string> localize = null)
	{
		if (value >= 80) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_10", "Outstanding prestige");
		if (value >= 60) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_11", "Good prestige");
		if (value >= 40) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_12", "Damaged prestige");
		if (value >= 20) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_13", "Low prestige");
		if (value >= 1) return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_14", "Authority near collapse");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "ReputationTier_15", "Authority completely lost");
	}

	public static int GetNationalPrestigeRelationTarget(int prestige)
	{
		if (prestige >= 80) return 0;
		if (prestige >= 60) return -2;
		if (prestige >= 40) return -5;
		if (prestige >= 20) return -10;
		if (prestige >= 1) return -15;
		return -20;
	}

	public static string FormatSignedDelta(int value)
	{
		return value > 0
			? "+" + value.ToString(CultureInfo.InvariantCulture)
			: value.ToString(CultureInfo.InvariantCulture);
	}

	public static string FormatSignedStandingDelta(int value, Func<string, string, string> localize = null)
	{
		if (value == 0) return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_UI_StandingNoChange", "No change");
		return value > 0
			? "+" + value.ToString(CultureInfo.InvariantCulture)
			: value.ToString(CultureInfo.InvariantCulture);
	}

	public static string BuildInternationalReputationImpactDeltaText(
		WorldDiplomacyDocument document,
        WorldDiplomacyStandingChange change,
        Func<string, string, string> localize = null)
	{
		int actualDelta = change?.Delta ?? 0;
		int evaluatedDelta = document?.InternationalReputationEvaluationDelta ?? 0;
		if (actualDelta != 0 || evaluatedDelta == 0) return FormatSignedStandingDelta(actualDelta, localize);
		string boundary = change?.After >= 100 && evaluatedDelta > 0
			? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_UI_StandingUpperBound", "Already at the upper limit of 100")
			: change?.After <= 0 && evaluatedDelta < 0
				? WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_UI_StandingLowerBound", "Already at the lower limit of 0")
				: WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_UI_StandingBoundaryNoChange", "The numeric boundary produced no actual change");
		return WorldDiplomacyPromptContractRules.ContractText(localize, "AF_DIP_UI_StandingNoActualChangePrefix", "No actual change (assessment ") + FormatSignedStandingDelta(evaluatedDelta, localize) + (localize == null ? "，" : ", ") + boundary + (localize == null ? "）" : ")");
	}

	public static void ApplyInternationalReputationEvaluation(WorldDiplomacyDocument document, JObject json)
	{
		if (document == null) return;
		string modelReason = WorldDiplomacyTextRules.Limit(
			WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "international_reputation_reason")), 240);
		if (WorldDiplomacyEnvelopeJsonRules.TryReadInteger(json, "international_reputation_delta", out int modelDelta)
			&& modelDelta != 0
			&& !string.IsNullOrWhiteSpace(modelReason))
		{
			document.InternationalReputationEvaluationDelta = Math.Max(-MaximumInternationalReputationChangePerDocument,
				Math.Min(MaximumInternationalReputationChangePerDocument, modelDelta));
			document.InternationalReputationEvaluationReason = modelReason;
			document.InternationalReputationEvaluationSource = "llm";
			return;
		}

		int fallbackDelta = CalculateStructuredInternationalReputationFallback(document, out string fallbackReason);
		document.InternationalReputationEvaluationDelta = fallbackDelta;
		document.InternationalReputationEvaluationReason = WorldDiplomacyTextRules.Limit(fallbackReason, 240);
		document.InternationalReputationEvaluationSource = "local_structured_fallback";
	}

	public static int CalculateStructuredInternationalReputationFallback(
		WorldDiplomacyDocument document,
		out string reason)
	{
		List<string> intents = document.Actions?.Where(x => x != null)
			.Select(x => WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent))
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList() ?? new List<string>();
		if (intents.Count == 0) intents.Add(WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent));
		int score = 0;
		bool hasPositiveAction = false;
		bool hasNegativeAction = false;
		foreach (string intent in intents)
		{
			int actionScore = intent switch
			{
				"accept_peace" or "accept_alliance" or "accept_trade" or "apology" or "concession" => 2,
				"propose_peace" or "propose_alliance" or "propose_trade" or "comply_ultimatum" => 1,
				"break_alliance" or "cancel_trade" or "declare_war" => -2,
				"condemn" or "warning" or "ultimatum" => -1,
				_ => 0
			};
			score += actionScore;
			hasPositiveAction |= actionScore > 0;
			hasNegativeAction |= actionScore < 0;
		}
		string basis = score > 0
			? DiplomacyMiscPromptTexts.Get("Rep_ConstructivePromise", "Constructive commitment, cooperation, or responsibility")
			: score < 0
				? DiplomacyMiscPromptTexts.Get("Rep_PressureOrEscalation", "Public pressure, breach of contract, or conflict escalation")
				: "";
		string move = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(document.NegotiationMove);
		if (score == 0 && document.MadeDiplomaticProgress)
		{
			score = 1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_NewActionableTerms", "This item provided new actionable terms or negotiation progress");
		}
		else if (score == 0 && move is "question" or "clarification" or "acknowledge_concern"
			or "counterproposal" or "conditional_acceptance" or "partial_concession"
			or "request_concession" or "revise_terms")
		{
			score = 1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_NewQuestionsOrTerms", "This item raised new questions, responses, or terms capable of continuing negotiations");
		}
		else if (score == 0 && move is "set_deadline" or "final_offer" or "withdraw_offer"
			or "end_negotiation" or "declare_deadlock")
		{
			score = -1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_TightenedOrEndedNegotiation", "This item tightened, withdrew, or terminated negotiation space");
		}
		else if (score == 0 && string.Equals(document.Tone, "hostile", StringComparison.OrdinalIgnoreCase))
		{
			score = -1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_HostileLanguageSuspicions", "This item intensified international suspicions with hostile rhetoric");
		}
		else if (score == 0 && intents.Any(x => x.StartsWith("reject_", StringComparison.Ordinal)))
		{
			score = 1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_TimelyFormalResponse", "This item responded promptly and explicitly to formal proposals");
		}
		else if (score == 0 && string.Equals(document.Tone, "conciliatory", StringComparison.OrdinalIgnoreCase))
		{
			score = 1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_RestrainedPublicStance", "This item stated positions publicly in a restrained and communicable manner");
		}
		else if (score == 0 && hasPositiveAction && hasNegativeAction)
		{
			score = -1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_OffsettingActionsConservative", "Positive and negative actions in the same item offset; evaluated conservatively by conflict and breach risk");
		}
		else if (score == 0)
		{
			score = -1;
			basis = DiplomacyMiscPromptTexts.Get("Rep_NoNewTermsOrProgress", "This item provided no new terms, explanations, actions, or negotiation progress");
		}
		score = Math.Max(-4, Math.Min(4, score));
		if (score == 0) score = -1;
		reason = DiplomacyMiscPromptTexts.Get("Rep_ConservativeActionPrefix", "Conservative non-zero judgment based on parsed diplomatic actions: ") + basis + ".";
		return score;
	}
	public static int GetNationalPrestige(IReadOnlyDictionary<string, int> prestigeByKingdom, string kingdomId)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		return prestigeByKingdom != null
			&& prestigeByKingdom.TryGetValue(normalizedId, out int value)
			? Math.Max(0, Math.Min(DefaultNationalPrestige, value))
			: DefaultNationalPrestige;
	}
	public static int GetInternationalReputation(IReadOnlyDictionary<string, int> reputationByKingdom, string kingdomId)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultInternationalReputation;
		return reputationByKingdom != null
			&& reputationByKingdom.TryGetValue(normalizedId, out int value)
			? Math.Max(0, Math.Min(100, value))
			: DefaultInternationalReputation;
	}
	public static int ApplyInternationalReputationDelta(
		Dictionary<string, int> reputationByKingdom,
		string kingdomId,
		int delta,
		WorldDiplomacyDocument sourceDocument,
		string reason,
		Func<string, string> resolveKingdomName)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultInternationalReputation;
		int before = GetInternationalReputation(reputationByKingdom, normalizedId);
		int boundedDelta = Math.Max(-WorldDiplomacyReputationRules.MaximumInternationalReputationChangePerDocument,
			Math.Min(WorldDiplomacyReputationRules.MaximumInternationalReputationChangePerDocument, delta));
		int updated = (int)Math.Max(0L, Math.Min(100L, (long)before + boundedDelta));
		reputationByKingdom[normalizedId] = updated;
		WorldDiplomacyDocumentFactRules.RecordDiplomaticStandingChange(sourceDocument, "international_reputation", normalizedId, before, updated, reason,
				resolveKingdomName);
		return updated;
	}
	public static int ApplyNationalPrestigeDelta(
		Dictionary<string, int> prestigeByKingdom,
		string kingdomId,
		int delta,
		WorldDiplomacyDocument sourceDocument,
		string reason,
		Func<string, string> resolveKingdomName)
{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		int before = GetNationalPrestige(prestigeByKingdom, normalizedId);
		int updated = (int)Math.Max(0L, Math.Min(DefaultNationalPrestige, (long)before + delta));
		prestigeByKingdom[normalizedId] = updated;
		WorldDiplomacyDocumentFactRules.RecordDiplomaticStandingChange(sourceDocument, "national_prestige", normalizedId, before, updated, reason,
				resolveKingdomName);
		return updated;
	}


	public static void SettleInternationalReputationForDocument(
		Dictionary<string, int> reputationByKingdom,
		WorldDiplomacyDocument document,
		Func<string, string> resolveKingdomName,
		Action<string> log)
	{
		if (document == null || reputationByKingdom == null || document.InternationalReputationSettled
			|| string.IsNullOrWhiteSpace(document.AuthorKingdomId)) return;
		// A pre-execution rejection confirms no fulfilled acceptance. Preserve the public
		// declaration, but do not reward (or penalize) its unexecuted acceptance as fulfillment.
		bool rejectedAcceptance = document.IsPlayerAuthored
			&& string.Equals(document.AnalysisStatus, "published_action_rejected", StringComparison.Ordinal)
			&& !document.ChangedDiplomaticState
			&& (document.Actions == null || document.Actions.All(x => x == null || !x.ChangedDiplomaticState))
			&& (IsOfferAcceptance(document.Intent) || document.Actions?.Any(x => x != null && IsOfferAcceptance(x.Intent)) == true);
		int delta = rejectedAcceptance ? 0 : Math.Max(-MaximumInternationalReputationChangePerDocument,
			Math.Min(MaximumInternationalReputationChangePerDocument, document.InternationalReputationEvaluationDelta));
		if (rejectedAcceptance)
		{
			document.InternationalReputationEvaluationReason = DiplomacyMiscPromptTexts.Get("Rep_RejectedProposalNoRepChange", "Diplomatic action accepting proposal was rejected before execution, performance unconfirmed; reputation change not settled for this declaration.");
			document.InternationalReputationEvaluationSource = "rejected_acceptance_no_effect";
		}
		else if (delta == 0)
		{
			delta = CalculateStructuredInternationalReputationFallback(document, out string fallbackReason);
			document.InternationalReputationEvaluationReason = WorldDiplomacyTextRules.Limit(fallbackReason, 240);
			document.InternationalReputationEvaluationSource = "local_nonzero_settlement_guard";
		}
		string reason = WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
			document.InternationalReputationEvaluationReason,
			DiplomacyMiscPromptTexts.Get("Rep_PublicPerformanceShiftedPerception", "Public presentation of this declaration shifted international perception.")), 240);
		document.InternationalReputationEvaluationDelta = delta;
		document.InternationalReputationEvaluationReason = reason;
		if (string.IsNullOrWhiteSpace(document.InternationalReputationEvaluationSource))
		{
			document.InternationalReputationEvaluationSource = "legacy_or_unspecified";
		}
		int before = GetInternationalReputation(reputationByKingdom, document.AuthorKingdomId);
		int after = ApplyInternationalReputationDelta(reputationByKingdom, document.AuthorKingdomId, delta, document, reason, resolveKingdomName);
		document.InternationalReputationSettled = true;
		log?.Invoke("international-reputation.settled document=" + document.DocumentId
			+ " author=" + document.AuthorKingdomId
			+ " delta=" + delta.ToString(CultureInfo.InvariantCulture)
			+ " before=" + before.ToString(CultureInfo.InvariantCulture)
			+ " after=" + after.ToString(CultureInfo.InvariantCulture)
			+ " source=" + document.InternationalReputationEvaluationSource);
	}

	private static bool IsOfferAcceptance(string intent)
	{
		string normalized = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
		return normalized.StartsWith("accept_", StringComparison.Ordinal)
			&& !string.IsNullOrWhiteSpace(WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(normalized));
	}

	public static void AnchorInternationalReputationNaturalChangeDays(
		Dictionary<string, int> lastDayByKingdom,
		IEnumerable<string> kingdomIds,
		int today)
	{
		if (lastDayByKingdom == null || kingdomIds == null) return;
		foreach (string kingdomId in kingdomIds)
		{
			lastDayByKingdom[kingdomId] = today;
		}
	}

    public static void ProcessInternationalReputationNaturalChange(
        Dictionary<string, int> reputationByKingdom,
        Dictionary<string, int> lastDayByKingdom,
        IEnumerable<string> kingdomIds,
        int today,
        Action<string> log)
{
    int changedKingdoms = 0;
    int totalAbsoluteChange = 0;
    // This runs once per campaign day over the small live-kingdom set. Catch-up is batched by
    // reputation band, so long time skips never become a per-day or per-point hot loop.
    foreach (string kingdomId in kingdomIds)
    {
        if (!lastDayByKingdom.TryGetValue(kingdomId, out int lastDay))
        {
            // Old saves and newly created kingdoms start tracking now; never apply retroactive decay.
            lastDayByKingdom[kingdomId] = today;
            continue;
        }
        if (lastDay > today)
        {
            lastDayByKingdom[kingdomId] = today;
            continue;
        }

        int before = GetInternationalReputation(reputationByKingdom, kingdomId);
        if (before == InternationalReputationNaturalAnchor)
        {
            // Time spent at the anchor must not accumulate and fire immediately after a declaration.
            lastDayByKingdom[kingdomId] = today;
            continue;
        }

        int elapsedDays = today - lastDay;
        int consumedDays;
        int updated = CalculateInternationalReputationNaturalChange(before, elapsedDays, out consumedDays);
        if (updated == InternationalReputationNaturalAnchor)
        {
            lastDayByKingdom[kingdomId] = today;
        }
        else if (consumedDays > 0)
        {
            lastDayByKingdom[kingdomId] = lastDay + consumedDays;
        }
        if (updated == before) continue;
        reputationByKingdom[kingdomId] = updated;
        changedKingdoms++;
        totalAbsoluteChange += Math.Abs(updated - before);
    }
    if (changedKingdoms > 0)
    {
        log?.Invoke("international reputation natural change kingdoms="
            + changedKingdoms.ToString(CultureInfo.InvariantCulture)
            + " absolute_delta=" + totalAbsoluteChange.ToString(CultureInfo.InvariantCulture));
    }
}
}
