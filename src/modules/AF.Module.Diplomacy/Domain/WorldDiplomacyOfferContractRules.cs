using AnimusForge;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOfferContractRules
{

	public static bool IsExclusivePeaceOfferResponseSet(IEnumerable<string> intents)
	{
		if (intents == null) return false;
		bool hasIntent = false;
		foreach (string intent in intents)
		{
			hasIntent = true;
			string normalized = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
			if (!string.Equals(normalized, "accept_peace", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(normalized, "reject_peace", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return hasIntent;
	}

	public static bool IsRequiredPeaceOfferResponse(
		string targetKingdomId,
		string intent,
		string respondingToOfferDocumentId,
		string respondingToOfferActionId,
		WorldDiplomacyRoundOffer requiredOffer)
	{
		if (requiredOffer == null) return true;
		string normalized = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
		return (string.Equals(normalized, "accept_peace", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(normalized, "reject_peace", StringComparison.OrdinalIgnoreCase))
			&& string.Equals(targetKingdomId, requiredOffer.ProposerKingdomId, StringComparison.OrdinalIgnoreCase)
			&& WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(respondingToOfferDocumentId, requiredOffer.SourceDocumentId)
			&& string.Equals(respondingToOfferActionId ?? "", requiredOffer.SourceActionId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	public static bool GeneratedActionsContainRequiredPeaceOfferResponse(
		JArray actions,
		WorldDiplomacyRoundOffer requiredOffer)
	{
		if (requiredOffer == null) return true;
		return actions?.OfType<JObject>().Any(x => IsRequiredPeaceOfferResponse(
			WorldDiplomacyEnvelopeJsonRules.ReadString(x, "target_kingdom_id", "target"),
			WorldDiplomacyEnvelopeJsonRules.ReadString(x, "intent", "author_intent.intent"),
			WorldDiplomacyEnvelopeJsonRules.ReadString(x, "responding_to_offer_document_id"),
			WorldDiplomacyEnvelopeJsonRules.ReadString(x, "responding_to_offer_action_id"),
			requiredOffer)) == true;
	}

	public static bool CommitmentMatchesIntent(string intent, string commitment)
	{
		string normalizedIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
		string normalizedCommitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(commitment);
		if (WorldDiplomacyIntentVocabulary.IsProposalIntent(normalizedIntent)) return normalizedCommitment == "proposal";
		// Treaty acceptance executes immediately, but its commitment is still an offer response.
		if (normalizedIntent.StartsWith("accept_", StringComparison.Ordinal)) return normalizedCommitment == "acceptance";
		if (normalizedIntent.StartsWith("reject_", StringComparison.Ordinal)) return normalizedCommitment == "rejection";
		if (WorldDiplomacyIntentVocabulary.IsImmediateIntent(normalizedIntent)) return normalizedCommitment == "binding";
		if (normalizedIntent is "ultimatum" or "comply_ultimatum" or "apology" or "concession") return normalizedCommitment == "binding";
		if (normalizedIntent is "statement" or "condemn" or "warning") return normalizedCommitment == "non_binding";
		return false;
	}

	public static string ProposalSuccessResult(string proposalIntent)
	{
		return WorldDiplomacyIntentVocabulary.NormalizeIntent(proposalIntent) switch
		{
			"propose_peace" => DiplomacyMiscPromptTexts.Get("Offer_BothSidesPeace", "Both sides have made peace"),
			"propose_alliance" => DiplomacyMiscPromptTexts.Get("Offer_BothSidesAlliance", "Both sides have formed an alliance"),
			"propose_trade" => DiplomacyMiscPromptTexts.Get("Offer_BothSidesTrade", "Both sides have concluded a trade agreement"),
			_ => DiplomacyMiscPromptTexts.Get("Offer_RelationsEffectiveAsAccepted", "Diplomatic relations took effect as accepted")
		};
	}

	public static bool ArePeaceTermsEquivalent(WorldDiplomacyPeaceTerms first, WorldDiplomacyPeaceTerms second)
	{
		if (HasNoPeaceClauses(first)) first = null;
		if (HasNoPeaceClauses(second)) second = null;
		if (first == null || second == null) return first == null && second == null;
		return string.Equals(first.TributePayerKingdomId ?? "", second.TributePayerKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.TributeReceiverKingdomId ?? "", second.TributeReceiverKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& first.DailyTribute == second.DailyTribute
			&& first.DurationDays == second.DurationDays
			&& string.Equals(first.CessionFromKingdomId ?? "", second.CessionFromKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.CessionToKingdomId ?? "", second.CessionToKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.CessionSettlementId ?? "", second.CessionSettlementId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private static bool HasNoPeaceClauses(WorldDiplomacyPeaceTerms terms) => terms == null
        || terms.DailyTribute == 0 && terms.DurationDays == 0
        && string.IsNullOrWhiteSpace(terms.TributePayerKingdomId) && string.IsNullOrWhiteSpace(terms.TributeReceiverKingdomId)
        && string.IsNullOrWhiteSpace(terms.CessionFromKingdomId) && string.IsNullOrWhiteSpace(terms.CessionToKingdomId)
        && string.IsNullOrWhiteSpace(terms.CessionSettlementId);

    // Missing optional clauses inherit the source. Explicit zero, invalid values,
    // or a changed role remain explicit and must survive until validation.
    public static WorldDiplomacyDialogueTerms ParseTreatyTerms(JObject json, string intent)
    {
        intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent);
        if (!WorldDiplomacyIntentVocabulary.IsFormalTreatyIntent(intent) && intent is not "propose_trade" and not "accept_trade") return null;
        if (json?["treaty_terms"] is not JObject value)
            return intent == "accept_trade" && json?["treaty_terms"] != null && json["treaty_terms"].Type != JTokenType.Null
                ? new WorldDiplomacyDialogueTerms { DurationDays = -1 } : null;
        var peace = json["peace_terms"] as JObject;
        if (intent == "accept_trade" && !value.Properties().Any(p => p.Name is "duration_days" or "daily_tribute"
                || !string.IsNullOrWhiteSpace(p.Value?.ToString()))
            && WorldDiplomacyEnvelopeJsonRules.ReadInteger(peace, "duration_days") == 0
            && WorldDiplomacyEnvelopeJsonRules.ReadInteger(peace, "daily_tribute") == 0
            && string.IsNullOrEmpty(WorldDiplomacyEnvelopeJsonRules.ReadString(peace, "cession_settlement_id"))) return null;
        int tribute = value["daily_tribute"] == null ? 0
            : WorldDiplomacyEnvelopeJsonRules.TryReadInteger(value, "daily_tribute", out int amount) ? amount : -1;
        int days = value["duration_days"] == null ? 0
            : WorldDiplomacyEnvelopeJsonRules.TryReadInteger(value, "duration_days", out int duration) ? duration : -1;
        // Do not overwrite explicitly supplied zero with a value from another field.
        if (intent != "accept_trade")
        {
            tribute = WorldDiplomacyEnvelopeJsonRules.ReadInteger(value, "daily_tribute");
            days = WorldDiplomacyEnvelopeJsonRules.ReadInteger(value, "duration_days");
        }
        if (value["daily_tribute"] == null || (intent != "accept_trade" && tribute == 0)) tribute = WorldDiplomacyEnvelopeJsonRules.ReadInteger(peace, "daily_tribute");
        if (value["duration_days"] == null || (intent != "accept_trade" && days == 0)) days = WorldDiplomacyEnvelopeJsonRules.ReadInteger(peace, "duration_days");
        return new WorldDiplomacyDialogueTerms {
            ReceivingKingdomId = WorldDiplomacyEnvelopeJsonRules.ReadString(value, "receiving_kingdom_id"),
            JoiningKingdomId = WorldDiplomacyEnvelopeJsonRules.ReadString(value, "joining_kingdom_id"),
            DailyTribute = tribute, DurationDays = days,
            CessionSettlementId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
                WorldDiplomacyEnvelopeJsonRules.ReadString(value, "cession_settlement_id"),
                WorldDiplomacyEnvelopeJsonRules.ReadString(peace, "cession_settlement_id")) };
    }

    public static bool TryResolveTradeAcceptanceTerms(WorldDiplomacyDocument source, string sourceActionId,
        WorldDiplomacyDialogueTerms acceptedTerms, out WorldDiplomacyDialogueTerms original, out string reason)
    {
        original = null;
        reason = "trade_acceptance_missing_source_offer";
        if (source == null) return false;
        var action = source.Actions?.Find(x => x != null && string.Equals(x.ActionId, sourceActionId, StringComparison.Ordinal));
        if (source.Actions?.Count > 0 && action == null) return false;
        original = action != null ? action.TreatyTerms : source.TreatyTerms;
        reason = "trade_acceptance_changes_terms";
        if (original?.DurationDays < 0 || original?.DailyTribute < 0
            || acceptedTerms?.DurationDays < 0 || acceptedTerms?.DailyTribute < 0) return false;
        var expected = (original ?? new WorldDiplomacyDialogueTerms()).ToTerms();
        if (acceptedTerms != null && !acceptedTerms.ToTerms().Equals(expected)) return false;
        reason = "";
        return true;
    }

	public static WorldDiplomacyPeaceTerms ClonePeaceTerms(WorldDiplomacyPeaceTerms source)
	{
		if (source == null) return null;
		return new WorldDiplomacyPeaceTerms
		{
			TributePayerKingdomId = source.TributePayerKingdomId ?? "",
			TributeReceiverKingdomId = source.TributeReceiverKingdomId ?? "",
			DailyTribute = source.DailyTribute,
			DurationDays = source.DurationDays,
			CessionFromKingdomId = source.CessionFromKingdomId ?? "",
			CessionToKingdomId = source.CessionToKingdomId ?? "",
			CessionSettlementId = source.CessionSettlementId ?? ""
		};
	}

	public static string FormatPeaceTermsForPrompt(WorldDiplomacyPeaceTerms terms, Func<string, string, string> localize = null)
	{
		if (terms == null) return WorldDiplomacyPromptContractRules.ContractText(localize, "PeaceTermsNone", "No additional tribute or cession terms");
		StringBuilder fact = new StringBuilder();
		if (terms.DailyTribute > 0)
		{
			fact.Append(WorldDiplomacyPromptContractRules.ContractText(localize, "PeaceTermsTribute", "Tribute=")).Append(terms.TributePayerKingdomId ?? "")
				.Append("→").Append(terms.TributeReceiverKingdomId ?? "")
				.Append(WorldDiplomacyPromptContractRules.ContractText(localize, "PeaceTermsDaily", ": daily ")).Append(terms.DailyTribute.ToString(CultureInfo.InvariantCulture))
				.Append(WorldDiplomacyPromptContractRules.ContractText(localize, "PeaceTermsDenarsFor", " denars, for ")).Append(Math.Max(0, terms.DurationDays).ToString(CultureInfo.InvariantCulture)).Append(WorldDiplomacyPromptContractRules.ContractText(localize, "PeaceTermsDays", " days"));
		}
		if (!string.IsNullOrWhiteSpace(terms.CessionSettlementId))
		{
			if (fact.Length > 0) fact.Append("；");
			fact.Append(WorldDiplomacyPromptContractRules.ContractText(localize, "PeaceTermsCession", "Cession=")).Append(terms.CessionFromKingdomId ?? "")
				.Append("→").Append(terms.CessionToKingdomId ?? "")
				.Append(":").Append(terms.CessionSettlementId);
		}
		return fact.Length == 0 ? WorldDiplomacyPromptContractRules.ContractText(localize, "PeaceTermsNone", "No additional tribute or cession terms") : fact.ToString();
	}
}
