using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyEnvelopeJsonRules
{
    public static bool TryParseJsonObject(string raw, out JObject parsed)
    {
        parsed = null;
        string text = (raw ?? "").Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNewLine = text.IndexOf('\n');
            int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                text = text.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
            }
        }
        try
        {
            parsed = JObject.Parse(text);
            return true;
        }
        catch
        {
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                try
                {
                    parsed = JObject.Parse(text.Substring(start, end - start + 1));
                    return true;
                }
                catch
                {
                }
            }
            parsed = new JObject();
            return false;
        }
    }

    public static JObject ParseJsonObject(string raw)
    {
        return TryParseJsonObject(raw, out JObject parsed) ? parsed : new JObject();
    }

    public static string ReadString(JObject json, params string[] paths)
    {
        foreach (string path in paths ?? Array.Empty<string>())
        {
            try
            {
                string value = json?.SelectToken(path)?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch
            {
            }
        }
        return "";
    }

    public static List<string> ReadStringList(JObject json, params string[] paths)
    {
        foreach (string path in paths ?? Array.Empty<string>())
        {
            try
            {
                JToken token = json?.SelectToken(path);
                if (token is JArray array) return WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(array.Values<string>());
                string value = token?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value)) return WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(value.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries));
            }
            catch
            {
            }
        }
        return new List<string>();
    }

    public static List<string> ReadTokenStringList(JToken token)
    {
        return token is JArray array ? WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(array.Values<string>()) : new List<string>();
    }

    public static float ReadFloat(JObject json, string path)
    {
        try
        {
            return float.TryParse(json?.SelectToken(path)?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? Math.Max(0f, Math.Min(1f, value))
                : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    public static int ReadInteger(JObject json, string path)
    {
        try
        {
            return int.TryParse(json?.SelectToken(path)?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static bool TryReadInteger(JObject json, string path, out int value)
    {
        value = 0;
        try
        {
            JToken token = json?.SelectToken(path);
            return token != null
                && token.Type != JTokenType.Null
                && int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
        catch
        {
            return false;
        }
    }

    public static bool ReadBool(JObject json, string path)
    {
        try
        {
            string value = json?.SelectToken(path)?.ToString()?.Trim();
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsJsonStringArray(JToken token)
    {
        if (token is not JArray array) return false;
        foreach (JToken item in array)
        {
            if (item == null || item.Type != JTokenType.String) return false;
        }
        return true;
    }

    public static JObject BuildPeaceTermsJson(WorldDiplomacyPeaceTerms terms)
    {
        return new JObject
        {
            ["tribute_payer_kingdom_id"] = terms?.TributePayerKingdomId ?? "",
            ["tribute_receiver_kingdom_id"] = terms?.TributeReceiverKingdomId ?? "",
            ["daily_tribute"] = Math.Max(0, terms?.DailyTribute ?? 0),
            ["duration_days"] = Math.Max(0, terms?.DurationDays ?? 0),
            ["cession_from_kingdom_id"] = terms?.CessionFromKingdomId ?? "",
            ["cession_to_kingdom_id"] = terms?.CessionToKingdomId ?? "",
            ["cession_settlement_id"] = terms?.CessionSettlementId ?? ""
        };
    }

    public static void NormalizeGeneratedDiplomaticEnvelopeShape(WorldDiplomacyJob job, JObject json)
    {
        if (json == null) return;
        if (json["actions"] == null)
        {
            string legacyIntent = ReadString(json, "author_intent.intent", "intent", "author_intent");
            string legacyTargetId = ReadString(json, "primary_target_kingdom_id", "target_kingdom_id", "target");
            if (string.IsNullOrWhiteSpace(legacyTargetId)) legacyTargetId = job?.TargetKingdomId ?? "";
            JObject legacyAction = new JObject
            {
                ["target_kingdom_id"] = legacyTargetId,
                ["intent"] = legacyIntent,
                ["peace_terms"] = json["peace_terms"] is JObject legacyTerms
                    ? legacyTerms.DeepClone()
                    : BuildPeaceTermsJson(null)
            };
            string legacyOfferSource = ReadString(json, "responding_to_offer_document_id");
            string legacyThreatSource = ReadString(json, "responding_to_threat_document_id");
            if (!string.IsNullOrWhiteSpace(legacyOfferSource)) legacyAction["responding_to_offer_document_id"] = legacyOfferSource;
            if (!string.IsNullOrWhiteSpace(legacyThreatSource)) legacyAction["responding_to_threat_document_id"] = legacyThreatSource;
            json["actions"] = new JArray(legacyAction);
        }
        if (json["actions"] is JArray actions)
        {
            foreach (JObject action in actions.OfType<JObject>())
            {
                if (action["peace_terms"] is not JObject) action["peace_terms"] = BuildPeaceTermsJson(null);
            }
            MirrorFirstGeneratedActionEnvelope(json, actions);
            json["addressed_kingdom_ids"] = new JArray(WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(actions.OfType<JObject>()
                .Select(x => ReadString(x, "target_kingdom_id", "target"))));
        }
        if (json["mentioned_kingdom_ids"] is not JArray)
        {
            json["mentioned_kingdom_ids"] = new JArray();
        }

        if (json["round_plan"] is not JObject roundPlan)
        {
            roundPlan = new JObject();
            json["round_plan"] = roundPlan;
        }
        bool autonomousOpening = WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(job);
        if (roundPlan["selected_kingdom_ids"] is not JArray)
        {
            roundPlan["selected_kingdom_ids"] = autonomousOpening && json["actions"] is JArray generatedActions
                ? new JArray(WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(generatedActions.OfType<JObject>()
                    .Select(x => ReadString(x, "target_kingdom_id", "target"))))
                : new JArray();
        }
        if (roundPlan["topic"] == null || roundPlan["topic"].Type == JTokenType.Null)
        {
            roundPlan["topic"] = autonomousOpening ? WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(ReadString(json, "title"), "外交交涉") : "";
        }
        if (json["peace_terms"] is not JObject)
        {
            json["peace_terms"] = BuildPeaceTermsJson(null);
        }
        if (json["requires_response"] == null || json["requires_response"].Type == JTokenType.Null) json["requires_response"] = false;
        if (json["tone"] == null || json["tone"].Type == JTokenType.Null) json["tone"] = "neutral";
        if (json["confidence"] == null || json["confidence"].Type == JTokenType.Null) json["confidence"] = 0.5;
        if (json["round_participation"] == null || json["round_participation"].Type == JTokenType.Null) json["round_participation"] = "continue";
        if (json["round_status"] == null || json["round_status"].Type == JTokenType.Null) json["round_status"] = "continue";
        if (json["made_progress"] == null || json["made_progress"].Type == JTokenType.Null) json["made_progress"] = true;
    }

    public static void MirrorFirstGeneratedActionEnvelope(JObject json, JArray actions)
    {
        if (json == null || actions == null || actions.Count == 0 || actions[0] is not JObject first) return;
        string intent = ReadString(first, "intent", "author_intent.intent");
        string targetId = ReadString(first, "target_kingdom_id", "target");
        json["author_intent"] = new JObject
        {
            ["intent"] = intent,
            ["commitment"] = ReadString(first, "commitment")
        };
        json["primary_target_kingdom_id"] = targetId;
        json["peace_terms"] = first["peace_terms"] is JObject terms ? terms.DeepClone() : BuildPeaceTermsJson(null);
        json["responding_to_offer_document_id"] = ReadString(first, "responding_to_offer_document_id");
        json["responding_to_offer_action_id"] = ReadString(first, "responding_to_offer_action_id");
        json["responding_to_threat_document_id"] = ReadString(first, "responding_to_threat_document_id");
        json["responding_to_threat_action_id"] = ReadString(first, "responding_to_threat_action_id");
    }

    public static JObject BuildGeneratedSingleActionEnvelope(JObject source, JObject action)
    {
        JObject single = source == null ? new JObject() : (JObject)source.DeepClone();
        single.Remove("actions");
        string targetId = ReadString(action, "target_kingdom_id", "target");
        single["author_intent"] = new JObject
        {
            ["intent"] = ReadString(action, "intent", "author_intent.intent"),
            ["commitment"] = ReadString(action, "commitment")
        };
        single["primary_target_kingdom_id"] = targetId;
        single["negotiation_move"] = ReadString(action, "negotiation_move");
        single["addressed_kingdom_ids"] = string.IsNullOrWhiteSpace(targetId) ? new JArray() : new JArray(targetId);
        single["peace_terms"] = action?["peace_terms"] is JObject terms ? terms.DeepClone() : BuildPeaceTermsJson(null);
        single["responding_to_offer_document_id"] = ReadString(action, "responding_to_offer_document_id");
        single["responding_to_offer_action_id"] = ReadString(action, "responding_to_offer_action_id");
        single["responding_to_threat_document_id"] = ReadString(action, "responding_to_threat_document_id");
        single["responding_to_threat_action_id"] = ReadString(action, "responding_to_threat_action_id");
        return single;
    }

    public static void CopyDerivedGeneratedActionEnvelope(JObject single, JObject action)
    {
        if (single == null || action == null) return;
        action["intent"] = ReadString(single, "author_intent.intent", "intent");
        action["commitment"] = ReadString(single, "author_intent.commitment", "commitment");
        action["negotiation_move"] = ReadString(single, "negotiation_move");
        action["responding_to_offer_document_id"] = ReadString(single, "responding_to_offer_document_id");
        action["responding_to_offer_action_id"] = ReadString(single, "responding_to_offer_action_id");
        action["responding_to_threat_document_id"] = ReadString(single, "responding_to_threat_document_id");
        action["responding_to_threat_action_id"] = ReadString(single, "responding_to_threat_action_id");
        if (single["peace_terms"] is JObject terms) action["peace_terms"] = terms.DeepClone();
    }

    public static bool TryNormalizeInlineResponseBinding(JObject json, out string bindingKind)
    {
        bindingKind = "";
        if (json?["author_intent"] is not JObject authorIntent) return false;
        string rawCommitment = authorIntent["commitment"]?.ToString()?.Trim() ?? "";
        if (rawCommitment.Length == 0) return false;
        const string OfferMarker = ":offer=";
        const string ThreatMarker = ":threat=";
        int offerIndex = rawCommitment.IndexOf(OfferMarker, StringComparison.OrdinalIgnoreCase);
        int threatIndex = rawCommitment.IndexOf(ThreatMarker, StringComparison.OrdinalIgnoreCase);
        if ((offerIndex < 0) == (threatIndex < 0)) return false;
        bool isOffer = offerIndex >= 0;
        int markerIndex = isOffer ? offerIndex : threatIndex;
        string marker = isOffer ? OfferMarker : ThreatMarker;
        if (markerIndex <= 0) return false;
        string normalizedCommitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(rawCommitment.Substring(0, markerIndex));
        if (!WorldDiplomacyIntentVocabulary.IsSupportedCommitment(normalizedCommitment)) return false;
        string sourceDocumentId = rawCommitment.Substring(markerIndex + marker.Length).Trim();
        const string DocumentIdPrefix = "diplomacy_document:";
        if (!sourceDocumentId.StartsWith(DocumentIdPrefix, StringComparison.OrdinalIgnoreCase)
            || sourceDocumentId.Length <= DocumentIdPrefix.Length)
        {
            return false;
        }
        string fieldName = isOffer ? "responding_to_offer_document_id" : "responding_to_threat_document_id";
        string existingSource = ReadString(json, fieldName);
        if (!string.IsNullOrWhiteSpace(existingSource)
            && !string.Equals(existingSource, sourceDocumentId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        authorIntent["commitment"] = normalizedCommitment;
        json[fieldName] = sourceDocumentId;
        bindingKind = isOffer ? "offer" : "threat";
        return true;
    }

    public static int RemoveRedundantStatementActions(JObject json)
    {
        if (json?["actions"] is not JArray actions || actions.Count <= 1) return 0;
        bool hasSubstantiveAction = actions
            .OfType<JObject>()
            .Any(action => !string.Equals(
                WorldDiplomacyIntentVocabulary.NormalizeIntent(ReadString(action, "intent", "author_intent.intent")),
                "statement",
                StringComparison.OrdinalIgnoreCase));
        if (!hasSubstantiveAction) return 0;
        int removed = 0;
        for (int index = actions.Count - 1; index >= 0; index--)
        {
            if (actions[index] is not JObject action
                || !string.Equals(
                    WorldDiplomacyIntentVocabulary.NormalizeIntent(ReadString(action, "intent", "author_intent.intent")),
                    "statement",
                    StringComparison.OrdinalIgnoreCase)) continue;
            actions.RemoveAt(index);
            removed++;
        }
        return removed;
    }

    public static string GetGeneratedEnvelopeApplicationFailureReason(
        WorldDiplomacyDocument document, JObject json, string targetId, bool relayTurn,
        Func<string, WorldDiplomacyRound> resolveRound)
    {
        if (document == null || json == null || !(json["actions"] is JArray actions) || actions.Count == 0)
            return "diplomatic_actions_envelope_invalid";
        if (!(json["author_intent"] is JObject)) return "semantic_envelope_missing_author_intent";
        if (!IsJsonStringArray(json["addressed_kingdom_ids"])) return "semantic_envelope_invalid_addressed_kingdom_ids";
        if (!IsJsonStringArray(json["mentioned_kingdom_ids"])) return "semantic_envelope_invalid_mentioned_kingdom_ids";
        if (!(json["round_plan"] is JObject roundPlan) || !IsJsonStringArray(roundPlan["selected_kingdom_ids"])) return "semantic_envelope_invalid_round_plan";
        if (!(json["peace_terms"] is JObject)) return "semantic_envelope_missing_peace_terms";
        if (json["requires_response"] == null) return "semantic_envelope_missing_requires_response";
        if (json["tone"] == null) return "semantic_envelope_missing_tone";
        if (json["confidence"] == null) return "semantic_envelope_missing_confidence";
        if (json["primary_target_kingdom_id"] == null) return "semantic_envelope_missing_primary_target";
        string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(ReadString(json, "author_intent.intent", "intent"));
        string commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(ReadString(json, "author_intent.commitment", "commitment"));
        if (!WorldDiplomacyIntentVocabulary.IsSupportedCommitment(commitment)) return "unsupported_commitment";
        if (string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
            && !WorldDiplomacyIntentVocabulary.IsSupportedNegotiationMove(ReadString(json, "negotiation_move"))) return "statement_missing_negotiation_move";
        if (string.IsNullOrWhiteSpace(targetId) && !string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)) return "diplomatic_action_has_no_target";
        if (relayTurn && !string.IsNullOrWhiteSpace(targetId) && document.RoundId != null)
        {
            WorldDiplomacyRound round = resolveRound(document.RoundId);
            if (round != null && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, targetId)) return "kingdom_not_in_relay_route";
        }
        return "generated_semantic_envelope_incomplete";
    }
}
