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
		if (first == null || second == null) return first == null && second == null;
		return string.Equals(first.TributePayerKingdomId ?? "", second.TributePayerKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.TributeReceiverKingdomId ?? "", second.TributeReceiverKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& first.DailyTribute == second.DailyTribute
			&& first.DurationDays == second.DurationDays
			&& string.Equals(first.CessionFromKingdomId ?? "", second.CessionFromKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.CessionToKingdomId ?? "", second.CessionToKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.CessionSettlementId ?? "", second.CessionSettlementId ?? "", StringComparison.OrdinalIgnoreCase);
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
