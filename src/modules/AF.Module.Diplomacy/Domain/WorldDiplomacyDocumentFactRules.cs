using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain
{
    /// <summary>
    /// DPL-060AP: pure document-fact/query and vassalage-normalization rules
    /// extracted from the host. Operates on Refactor records only.
    /// </summary>
    public static class WorldDiplomacyDocumentFactRules
    {
    public static WorldDiplomacyDocumentAction ResolveSourceActionForTarget(
        WorldDiplomacyDocument source,
        string targetKingdomId)
    {
        if (source?.Actions == null || source.Actions.Count == 0 || string.IsNullOrWhiteSpace(targetKingdomId)) return null;
        return source.Actions.FirstOrDefault(x => x != null
            && string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildPeaceOfferTermsFact(
        WorldDiplomacyDocument source,
        string targetKingdomId)
    {
        if (source == null || string.IsNullOrWhiteSpace(targetKingdomId)) return "";
        if (source.Actions?.Count > 0)
        {
            WorldDiplomacyDocumentAction action = ResolveSourceActionForTarget(source, targetKingdomId);
            return action != null
                && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
                ? "action=" + (action.ActionId ?? "") + ":" + WorldDiplomacyOfferContractRules.FormatPeaceTermsForPrompt(action.PeaceTerms)
                : "";
        }
        if (string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(source.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
            && string.Equals(source.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            return WorldDiplomacyOfferContractRules.FormatPeaceTermsForPrompt(source.PeaceTerms);
        }
        return "";
    }

    public static string BuildSourceActionFactForTarget(
        WorldDiplomacyDocument source,
        string targetKingdomId)
    {
        if (source == null) return "";
        WorldDiplomacyDocumentAction action = ResolveSourceActionForTarget(source, targetKingdomId);
        if (action != null)
        {
            return "action=" + (action.ActionId ?? "")
                + "|对象国=" + (action.TargetKingdomId ?? "")
                + "|意图=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent);
        }
        if (source.Actions?.Count > 0
            || (!string.IsNullOrWhiteSpace(targetKingdomId)
                && !string.Equals(source.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase))) return "";
        return "对象国=" + (source.TargetKingdomId ?? "") + "|意图=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(source.Intent);
    }

    public static int DiplomacyDocumentQueryRelevance(WorldDiplomacyDocument document, string input)
    {
        if (document == null || string.IsNullOrWhiteSpace(input)) return 0;
        int score = 0;
        if (!string.IsNullOrWhiteSpace(document.Title) && input.IndexOf(document.Title, StringComparison.OrdinalIgnoreCase) >= 0) score += 100;
        if (!string.IsNullOrWhiteSpace(document.AuthorKingdomName) && input.IndexOf(document.AuthorKingdomName, StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
        if (!string.IsNullOrWhiteSpace(document.TargetKingdomName) && input.IndexOf(document.TargetKingdomName, StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
        if (input.IndexOf(WorldDiplomacyIntentVocabulary.IntentLabel(document.Intent), StringComparison.OrdinalIgnoreCase) >= 0) score += 20;
        return score;
    }

    public static bool IsBilateralBattleFact(WorldDiplomacyBattleFact fact, string firstKingdomId, string secondKingdomId)
    {
        if (fact == null)
        {
            return false;
        }
        bool firstAttacker = fact.AttackerKingdomIds?.Contains(firstKingdomId, StringComparer.OrdinalIgnoreCase) == true;
        bool firstDefender = fact.DefenderKingdomIds?.Contains(firstKingdomId, StringComparer.OrdinalIgnoreCase) == true;
        bool secondAttacker = fact.AttackerKingdomIds?.Contains(secondKingdomId, StringComparer.OrdinalIgnoreCase) == true;
        bool secondDefender = fact.DefenderKingdomIds?.Contains(secondKingdomId, StringComparer.OrdinalIgnoreCase) == true;
        return (firstAttacker && secondDefender) || (firstDefender && secondAttacker);
    }

    public static void MirrorPrimaryActionToDocument(
		WorldDiplomacyDocument document,
		WorldDiplomacyDocumentAction action)
	{
		if (document == null || action == null) return;
		document.TargetKingdomId = action.TargetKingdomId ?? "";
		document.TargetKingdomName = action.TargetKingdomName ?? "";
		document.Intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent);
		document.NegotiationMove = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(action.NegotiationMove);
		document.Commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(action.Commitment);
		document.HiddenIntent = document.Intent;
		document.HiddenCommitment = document.Commitment;
		document.PeaceTerms = action.PeaceTerms;
		document.RespondingToOfferDocumentId = action.RespondingToOfferDocumentId ?? "";
		document.TreatyTerms = action.TreatyTerms;
		document.RespondingToOfferActionId = action.RespondingToOfferActionId ?? "";
		document.RespondingToThreatDocumentId = action.RespondingToThreatDocumentId ?? "";
		document.RespondingToThreatActionId = action.RespondingToThreatActionId ?? "";
		document.SourceDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
			document.RespondingToOfferDocumentId,
			document.RespondingToThreatDocumentId,
			document.SourceDocumentId);
	}
public static string BuildMultiActionMechanicalResult(List<WorldDiplomacyDocumentAction> actions)
	{
		if (actions == null || actions.Count == 0) return "";
		return string.Join("；", actions
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.MechanicalResult))
			.Select(x => WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId) + "：" + x.MechanicalResult));
	}
public static bool DocumentContainsRequiredPeaceOfferResponse(
		WorldDiplomacyDocument document,
		WorldDiplomacyRoundOffer requiredOffer)
	{
		if (requiredOffer == null) return true;
		if (document?.Actions?.Count > 0)
		{
			return document.Actions.Any(x => x != null && WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
				x.TargetKingdomId,
				x.Intent,
				x.RespondingToOfferDocumentId,
				x.RespondingToOfferActionId,
				requiredOffer));
		}
		return document != null && WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
			document.TargetKingdomId,
			document.Intent,
			document.RespondingToOfferDocumentId,
			document.RespondingToOfferActionId,
			requiredOffer);
	}
public static bool PeaceTermsContainCession(WorldDiplomacyPeaceTerms terms)
	{
		return terms != null && (!string.IsNullOrWhiteSpace(terms.CessionFromKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionToKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionSettlementId));
	}
public static WorldDiplomacyDocument CloneDocument(WorldDiplomacyDocument document)
	{
		if (document == null)
		{
			return null;
		}
		return JsonConvert.DeserializeObject<WorldDiplomacyDocument>(JsonConvert.SerializeObject(document));
	}
public static Dictionary<string, WorldDiplomacyDocument> BuildDocumentIndex(
		IEnumerable<WorldDiplomacyDocument> documents)
	{
		Dictionary<string, WorldDiplomacyDocument> result = new Dictionary<string, WorldDiplomacyDocument>(StringComparer.OrdinalIgnoreCase);
		foreach (WorldDiplomacyDocument document in documents ?? Enumerable.Empty<WorldDiplomacyDocument>())
		{
			if (document == null || string.IsNullOrWhiteSpace(document.DocumentId)) continue;
			result[document.DocumentId.Trim()] = document;
		}
		return result;
	}
public static WorldDiplomacyDocumentAction ResolveDocumentAction(
		WorldDiplomacyDocument document,
		string actionId)
	{
		if (document?.Actions == null || document.Actions.Count == 0 || string.IsNullOrWhiteSpace(actionId)) return null;
		return document.Actions.FirstOrDefault(x => x != null
			&& string.Equals(x.ActionId, actionId, StringComparison.OrdinalIgnoreCase));
	}
public static WorldDiplomacyPeaceTerms ResolveOfferedPeaceTerms(
		WorldDiplomacyDocument source,
		string sourceActionId)
	{
		if (source == null) return null;
		if (source.Actions?.Count > 0)
		{
			return ResolveDocumentAction(source, sourceActionId)?.PeaceTerms;
		}
		return source.PeaceTerms;
	}
public static string FormatRoundFactForPrompt(WorldDiplomacyRoundFact fact)
	{
		if (fact == null || string.IsNullOrWhiteSpace(fact.Text)) return "";
		string text = fact.Text.Trim();
		if (text.StartsWith("[", StringComparison.Ordinal)) return text;
		return string.Equals(fact.Kind, "confirmed_result", StringComparison.OrdinalIgnoreCase)
			? "[游戏已执行] " + text
			: "[宣言记录，不代表执行] " + text;
	}
public static bool IsMajorDiplomaticDocument(WorldDiplomacyDocument document)
	{
		string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document?.Intent);
		return intent == "declare_war"
			|| intent == "accept_peace"
			|| intent == "propose_peace"
			|| intent == "accept_alliance"
			|| intent == "propose_alliance"
			|| intent == "break_alliance"
			|| intent == "accept_trade"
			|| intent == "propose_trade"
			|| intent == "cancel_trade"
			|| intent == "ultimatum"
			|| !string.IsNullOrWhiteSpace(document?.MechanicalResult);
	}

    public static bool GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(JArray actions,
        Func<string, WorldDiplomacyDocument> resolveDocument)
    {
        List<JObject> acceptances = actions?.OfType<JObject>()
            .Where(x => string.Equals(
                WorldDiplomacyIntentVocabulary.NormalizeIntent(WorldDiplomacyEnvelopeJsonRules.ReadString(x, "intent", "author_intent.intent")),
                "accept_peace",
                StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<JObject>();
        if (acceptances.Count <= 1) return false;
        foreach (JObject acceptance in acceptances)
        {
            WorldDiplomacyDocument source = resolveDocument(WorldDiplomacyEnvelopeJsonRules.ReadString(acceptance, "responding_to_offer_document_id"));
            WorldDiplomacyPeaceTerms terms = ResolveOfferedPeaceTerms(
                source,
                WorldDiplomacyEnvelopeJsonRules.ReadString(acceptance, "responding_to_offer_action_id"));
            if (source == null || PeaceTermsContainCession(terms)) return true;
        }
        return false;
    }

    public static bool DocumentHasUnsafeMultiplePeaceAcceptances(WorldDiplomacyDocument document,
        Func<string, WorldDiplomacyDocument> resolveDocument)
    {
        List<WorldDiplomacyDocumentAction> acceptances = document?.Actions?
            .Where(x => x != null && string.Equals(
                WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent),
                "accept_peace",
                StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<WorldDiplomacyDocumentAction>();
        if (acceptances.Count <= 1) return false;
        foreach (WorldDiplomacyDocumentAction acceptance in acceptances)
        {
            WorldDiplomacyDocument source = resolveDocument(acceptance.RespondingToOfferDocumentId);
            WorldDiplomacyPeaceTerms terms = ResolveOfferedPeaceTerms(source, acceptance.RespondingToOfferActionId);
            if (source == null || PeaceTermsContainCession(terms)) return true;
        }
        return false;
    }

    public static bool HasKingdomRespondedToDocument(IEnumerable<WorldDiplomacyDocument> documents,
        string kingdomId, string documentId)
    {
        return (documents ?? Enumerable.Empty<WorldDiplomacyDocument>()).Any(x => x != null
            && string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
            && WorldDiplomacyStructureRules.DocumentRespondsTo(x, documentId));
    }

    public static bool HasUndeliveredCourtArrivals(IEnumerable<WorldDiplomacyPropagationArrival> arrivals,
        string roundId)
    {
        return (arrivals ?? Enumerable.Empty<WorldDiplomacyPropagationArrival>()).Any(x => x != null
            && WorldDiplomacyStructureRules.IsCourtArrival(x)
            && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, roundId));
    }

    public static bool HasKingdomKnowledge(IEnumerable<WorldDiplomacyKingdomKnowledge> knowledge,
        string kingdomId, string documentId)
    {
        return (knowledge ?? Enumerable.Empty<WorldDiplomacyKingdomKnowledge>()).Any(x => x != null
            && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
            && (x.DocumentIds ?? new List<string>()).Contains(documentId, StringComparer.OrdinalIgnoreCase));
    }

    public static HashSet<string> GetKnownSettlementIdsForDocument(
        IEnumerable<WorldDiplomacySettlementKnowledge> knowledge, string documentId)
    {
        return new HashSet<string>((knowledge ?? Enumerable.Empty<WorldDiplomacySettlementKnowledge>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.SettlementId)
                && (x.DocumentIds ?? new List<string>()).Contains(documentId, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.SettlementId), StringComparer.OrdinalIgnoreCase);
    }

    public static HashSet<string> GetKnownKingdomIdsForDocument(
        IEnumerable<WorldDiplomacyKingdomKnowledge> knowledge, string documentId)
    {
        return new HashSet<string>((knowledge ?? Enumerable.Empty<WorldDiplomacyKingdomKnowledge>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId)
                && (x.DocumentIds ?? new List<string>()).Contains(documentId, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.KingdomId), StringComparer.OrdinalIgnoreCase);
    }

    public static string BuildFallbackRoundSummary(IEnumerable<WorldDiplomacyDocument> documents,
        List<string> ids, Func<int, string> formatDayFallback)
    {
        return string.Join("；", (documents ?? Enumerable.Empty<WorldDiplomacyDocument>())
            .Where(x => x != null && (ids ?? new List<string>()).Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase))
            .OrderBy(x => x.Day).Take(16)
            .Select(x => WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(x, formatDayFallback)));
    }

    public static string BuildFallbackRoundCompressionJson(IEnumerable<WorldDiplomacyDocument> documents,
        List<string> ids, Func<int, string> formatDayFallback)
    {
        return new JObject { ["summary"] = BuildFallbackRoundSummary(documents, ids, formatDayFallback), ["facts"] = new JArray() }.ToString(Formatting.None);
    }

    public static string BuildPeaceKingdomReferencePattern(string kingdomName, bool isAuthor, bool isTarget)
    {
        string name = Regex.Escape(kingdomName ?? "");
        if (isAuthor) return "(?:我国|本国|本王国|" + name + ")";
        if (isTarget) return "(?:贵国|" + name + ")";
        return "(?:" + name + ")";
    }
        public static bool ContainsDirectedPeaceTerm(string text,
            string fromName, bool fromIsAuthor, bool fromIsTarget,
            string toName, bool toIsAuthor, bool toIsTarget,
            string actionPattern)
    {
        if (string.IsNullOrWhiteSpace(fromName) || string.IsNullOrWhiteSpace(toName)) return false;
        string fromPattern = BuildPeaceKingdomReferencePattern(fromName, fromIsAuthor, fromIsTarget);
        string toPattern = BuildPeaceKingdomReferencePattern(toName, toIsAuthor, toIsTarget);
        string body = text ?? "";
        return Regex.IsMatch(body, fromPattern + @"[^。；\n]{0,40}(?:" + actionPattern + @")[^。；\n]{0,40}" + toPattern, RegexOptions.CultureInvariant)
            || Regex.IsMatch(body, fromPattern + @"[^。；\n]{0,24}(?:向|给|予)" + toPattern + @"[^。；\n]{0,24}(?:" + actionPattern + @")", RegexOptions.CultureInvariant);
    }

    public static void RecordDiplomaticStandingChange(WorldDiplomacyDocument document,
            string kind, string kingdomId, int before, int after, string reason,
            Func<string, string> kingdomName)
        {
            if (document == null || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(kingdomId)) return;
            document.DiplomaticStandingChanges ??= new List<WorldDiplomacyStandingChange>();
            string normalizedReason = WorldDiplomacyTextRules.Limit((reason ?? "").Trim(), 240);
            if (document.DiplomaticStandingChanges.Any(x => x != null
                && string.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Reason, normalizedReason, StringComparison.Ordinal))) return;
            document.DiplomaticStandingChanges.Add(new WorldDiplomacyStandingChange
            {
                Kind = kind,
                KingdomId = kingdomId,
                KingdomName = kingdomName(kingdomId),
                Before = before,
                After = after,
                Delta = after - before,
                Reason = normalizedReason
            });
        }
        public static bool IsDiplomaticRepresentativeForAddressedVassal(WorldDiplomacyDocument document,
                Func<string, bool> isAddressedKingdomRepresentedByReceiver)
        {
                if (document == null || isAddressedKingdomRepresentedByReceiver == null)
                {
                    return false;
                }
                IEnumerable<string> addressedIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((document.AddressedKingdomIds ?? new List<string>())
                    .Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId)
                        ? Enumerable.Empty<string>()
                        : new[] { document.TargetKingdomId }));
                foreach (string addressedId in addressedIds)
                {
                    if (isAddressedKingdomRepresentedByReceiver(addressedId))
                    {
                        return true;
                    }
                }
                return false;
        }

        internal const int MaxKnownDocumentsPerLocation = 64;

	public static void RecordSettlementKnowledge(List<WorldDiplomacySettlementKnowledge> settlementKnowledge,
		string settlementId, string documentId, int day)
	{
		if (settlementKnowledge == null || string.IsNullOrWhiteSpace(settlementId) || string.IsNullOrWhiteSpace(documentId)) return;
		WorldDiplomacySettlementKnowledge knowledge = settlementKnowledge.FirstOrDefault(x => x != null && string.Equals(x.SettlementId, settlementId, StringComparison.OrdinalIgnoreCase));
		if (knowledge == null)
		{
			knowledge = new WorldDiplomacySettlementKnowledge { SettlementId = settlementId };
			settlementKnowledge.Add(knowledge);
		}
		if (!knowledge.DocumentIds.Contains(documentId, StringComparer.OrdinalIgnoreCase)) knowledge.DocumentIds.Add(documentId);
		if (knowledge.DocumentIds.Count > MaxKnownDocumentsPerLocation) knowledge.DocumentIds.RemoveRange(0, knowledge.DocumentIds.Count - MaxKnownDocumentsPerLocation);
		knowledge.LastUpdatedDay = day;
	}
	public static bool RecordKingdomKnowledge(List<WorldDiplomacyKingdomKnowledge> kingdomKnowledge,
		string kingdomId, string documentId, int day)
	{
		if (kingdomKnowledge == null || string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(documentId)) return false;
		WorldDiplomacyKingdomKnowledge knowledge = kingdomKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (knowledge == null)
		{
			knowledge = new WorldDiplomacyKingdomKnowledge { KingdomId = kingdomId };
			kingdomKnowledge.Add(knowledge);
		}
		if (knowledge.DocumentIds.Contains(documentId, StringComparer.OrdinalIgnoreCase)) return false;
		knowledge.DocumentIds.Add(documentId);
		if (knowledge.DocumentIds.Count > MaxKnownDocumentsPerLocation * 2) knowledge.DocumentIds.RemoveRange(0, knowledge.DocumentIds.Count - MaxKnownDocumentsPerLocation * 2);
		knowledge.LastUpdatedDay = day;
		return true;
	}
	public static void RecordNobleKnowledge(List<WorldDiplomacyKingdomKnowledge> nobleKnowledge,
		string kingdomId, string documentId, int day)
	{
		if (nobleKnowledge == null || string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(documentId)) return;
		WorldDiplomacyKingdomKnowledge knowledge = nobleKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (knowledge == null)
		{
			knowledge = new WorldDiplomacyKingdomKnowledge { KingdomId = kingdomId };
			nobleKnowledge.Add(knowledge);
		}
		if (!knowledge.DocumentIds.Contains(documentId, StringComparer.OrdinalIgnoreCase)) knowledge.DocumentIds.Add(documentId);
		if (knowledge.DocumentIds.Count > MaxKnownDocumentsPerLocation * 2) knowledge.DocumentIds.RemoveRange(0, knowledge.DocumentIds.Count - MaxKnownDocumentsPerLocation * 2);
		knowledge.LastUpdatedDay = day;
	}
	public static string BuildRecentNativeSignalContext(IEnumerable<NativeDiplomacySignal> signals, string sourceId, string targetId)
	{
		return string.Join("\n", (signals ?? Enumerable.Empty<NativeDiplomacySignal>())
			.Where(x => x != null
				&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.Day)
			.Take(4)
			.Select(x => "- 第" + x.Day.ToString(CultureInfo.InvariantCulture) + "天：" + x.Reason));
	}
	public static string BuildRelevantCompletedRoundContext(IEnumerable<WorldDiplomacyRoundSummary> summaries,
		string activeRoundId, string sourceId, string targetId)
	{
		if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId)) return "";
		WorldDiplomacyRoundSummary summary = (summaries ?? Enumerable.Empty<WorldDiplomacyRoundSummary>())
			.Where(x => x != null && !x.IsTokenCompressed
				&& (x.KingdomIds ?? new List<string>()).Contains(sourceId, StringComparer.OrdinalIgnoreCase)
				&& (x.KingdomIds ?? new List<string>()).Contains(targetId, StringComparer.OrdinalIgnoreCase)
				&& !WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, activeRoundId))
			.OrderByDescending(x => x.CreatedDay)
			.FirstOrDefault();
		return summary == null ? "" : "- [已结束] " + WorldDiplomacyTextRules.Limit(summary.Summary, 700);
	}
	public static string BuildRelevantCompressedDiplomacyContext(IEnumerable<WorldDiplomacyRoundSummary> summaries,
		string sourceId, string targetId, int maxCount)
	{
		if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId) || maxCount <= 0) return "";
		return string.Join("\n", (summaries ?? Enumerable.Empty<WorldDiplomacyRoundSummary>())
			.Where(x => x != null && x.IsTokenCompressed
				&& (x.KingdomIds ?? new List<string>()).Contains(sourceId, StringComparer.OrdinalIgnoreCase)
				&& (x.KingdomIds ?? new List<string>()).Contains(targetId, StringComparer.OrdinalIgnoreCase))
			.OrderByDescending(x => x.CreatedDay).Take(maxCount)
			.Select(x => "- [已整理回合；宣言经过与游戏结果分列] " + WorldDiplomacyTextRules.Limit(x.Summary, 900)));
	}
	public static string BuildKnownRoundContext(IEnumerable<WorldDiplomacyKingdomKnowledge> kingdomKnowledge,
		IEnumerable<WorldDiplomacyDocument> documents,
		string kingdomId, string roundId, int maxCount, Func<int, string> formatCampaignDay)
	{
		if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(roundId)) return "";
		WorldDiplomacyKingdomKnowledge knowledge = (kingdomKnowledge ?? Enumerable.Empty<WorldDiplomacyKingdomKnowledge>()).FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		HashSet<string> known = new HashSet<string>(knowledge?.DocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyDocument> recentKnown = WorldDiplomacyRoundLifecycleRules.SelectNewestDocumentsChronologically(
			(documents ?? Enumerable.Empty<WorldDiplomacyDocument>()).Where(x => x != null && known.Contains(x.DocumentId ?? "")
				&& WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, roundId)),
			maxCount);
		return string.Join("\n", recentKnown
			.Select(x => "- " + WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(x, formatCampaignDay) + (string.IsNullOrWhiteSpace(x.Body) ? "" : "：" + WorldDiplomacyTextRules.Limit(x.Body, 420))));
	}

    }

}
