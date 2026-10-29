using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private static void InvalidateDraftRepair(WorldDiplomacyJob job)
    {
        job.PositionCorrectionRequired = false;
        job.DeclarationRepairField = "";
        job.DeclarationRepairAttempts = 0;
    }

    // Request boundaries only. Repair one failed contract against the retained
    // draft; no political state changes until independent review and publication.
    private bool BeginDraftRepair(WorldDiplomacyJob job, string field, string feedback)
    {
        job.DeclarationRepairField = field;
        job.PrivacyRepairFeedback = feedback ?? "";
        job.Kind = "privacy_repair";
        job.IsRunning = false;
        job.TransportRetryAttempts = 0;
        job.LlmMessages?.Clear();
        PrepareDraftRepair(job);
        return true;
    }

    private void PrepareDraftRepair(WorldDiplomacyJob job)
    {
        job.DeclarationRepairVersion = 1;
        job.SystemPrompt = PromptComposerTexts.ApplyOutputLanguage(DiplomacyPromptTexts.Get("DeclarationFieldRepair", ""));
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get(job.DeclarationRepairField == "position_correction"
            ? "SovereignPositionCorrection" : "ResponseRequestContract", "");
        job.UserPrompt = new JObject {
            ["required_field"] = job.DeclarationRepairField,
            ["original_draft"] = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft),
            ["repair_feedback"] = job.PrivacyRepairFeedback,
            ["response_evidence"] = BuildResponseEvidence(job),
            ["public_context"] = BuildPublicExecutionContext(job),
            ["isolated_context"] = job.PrivacyPublicContext
        }.ToString(Formatting.None);
        job.MaxTokens = _host.AnalysisMaxTokens();
        job.LlmMessages?.Clear();
    }

    private bool RetryDraftRepair(WorldDiplomacyJob job, string reason)
    {
        job.PrivacyRepairFeedback += "\n" + reason;
        if (++job.DeclarationRepairAttempts >= 3)
            SuspendDiplomacyJob(job, "declaration_field_repair_exhausted:" + job.DeclarationRepairField + ":" + reason);
        else { job.IsRunning = false; PrepareDraftRepair(job); }
        return true;
    }

    private bool RetryRejectedPositionCorrection(WorldDiplomacyJob job, string feedback)
    {
        job.PositionCorrectionRequired = true;
        job.Kind = "privacy_repair";
        job.DeclarationRepairField = "position_correction";
        job.PrivacyRepairFeedback = feedback;
        return RetryDraftRepair(job, "position_correction_rejected_by_independent_review");
    }

    private bool CompleteDraftRepair(WorldDiplomacyJob job, JObject response)
    {
        var draft = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft);
        if (draft["body"]?.Type != JTokenType.String || draft["title"]?.Type != JTokenType.String)
        { SuspendDiplomacyJob(job, "declaration_repair_original_missing"); return true; }
        string field = job.DeclarationRepairField;
        if (field is not "position_correction" and not "response_requests")
        { SuspendDiplomacyJob(job, "declaration_repair_field_invalid"); return true; }
        if (response.Properties().Any(x => x.Name != field) || response[field] == null)
            return RetryDraftRepair(job, "return_only_required_field:" + field + ";do_not_rewrite_prose_or_actions");
        draft[field] = response[field].DeepClone();
        if (field == "position_correction")
        {
            if (!TryReadSovereignPositionCorrection(job, draft, out var position, out string error) || position == null)
                return RetryDraftRepair(job, error + ";non_null_correction_required_for_current_sovereign");
        }
        else if (draft[field] is not JArray || !WorldDiplomacyAnalysisApplication.TryReadResponseRequests(draft,
            (string)draft["body"], _host.ResolveKingdomIdOrNull, out _))
            return RetryDraftRepair(job, "response_requests_require_valid_kingdom_kind_and_exact_original_body_quote");
        job.PrivacyDraft = draft.ToString(Formatting.None);
        job.TransportRetryAttempts = 0;
        job.PrivacyAuditRetries = 0;
        job.PrivacyRepairFeedback = "";
        PrepareDeclarationAudit(job);
        job.IsRunning = false;
        return true;
    }

    private bool NeedsSovereignOwnerReview(WorldDiplomacyJob job) => job.MaterialCountryContractVersion >= 1
        && !IsOralDeclarationJob(job) && !_host.IsPlayerParty(job.AuthorKingdomId)
        && ResolveRound(job.RoundId)?.Participants.Any(x => x.KingdomId == job.AuthorKingdomId
            && x.State == "active" && !string.IsNullOrWhiteSpace(x.Agenda)) == true;

    private JArray BuildPositionOwnerReview(JArray positions)
    {
        var required = new JArray();
        foreach (var position in (positions ?? new JArray()).OfType<JObject>())
        {
            string kingdom = position["kingdom"]?.Type == JTokenType.String ? (string)position["kingdom"] : "";
            if (string.IsNullOrEmpty(kingdom) || _host.IsPlayerParty(kingdom)) continue;
            required.Add(new JObject { ["kingdom"] = kingdom, ["ruler_hero_id"] = _host.PartyRulerId(kingdom),
                ["digest"] = MaterialHash(position.ToString(Formatting.None)), ["position"] = position.DeepClone() });
        }
        return required;
    }

    private bool ValidatePositionOwnerReview(JArray positions, JObject audit, out string reason, out bool rejected)
    {
        reason = "position_owner_review_incomplete"; rejected = false;
        var required = BuildPositionOwnerReview(positions);
        if (required.Count == 0) { reason = ""; return true; }
        if (audit["position_reviews"] is not JArray reviews || reviews.Count != required.Count) return false;
        foreach (var item in required.OfType<JObject>())
        {
            var matches = reviews.OfType<JObject>().Where(x => x["kingdom"]?.Type == JTokenType.String
                && (string)x["kingdom"] == (string)item["kingdom"]).ToList();
            if (matches.Count != 1) return false;
            var row = matches[0];
            if (row["ruler_hero_id"]?.Type != JTokenType.String || (string)row["ruler_hero_id"] != (string)item["ruler_hero_id"]
                || row["digest"]?.Type != JTokenType.String || (string)row["digest"] != (string)item["digest"]
                || row["consistent"]?.Type != JTokenType.Boolean || row["reason"]?.Type != JTokenType.String
                || string.IsNullOrWhiteSpace((string)row["reason"]) || ((string)row["reason"]).Length > 1200) return false;
            if (!(bool)row["consistent"])
            { rejected = true; reason = "position_owner_rejected:" + (string)item["kingdom"] + ":" + (string)row["reason"]; return false; }
        }
        reason = ""; return true;
    }

    private bool RetryConceptAudit(WorldDiplomacyJob job, string reason)
    {
        job.MaterialRepairFeedback = "event_concept_audit_invalid:" + reason + ";review_same_draft";
        if (++job.PrivacyAuditRetries >= 3) HoldTechnicalRequest(job, job.MaterialRepairFeedback);
        else { job.IsRunning = false; job.LlmMessages?.Clear(); }
        return true;
    }
}
