using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    internal const string StructureObservationMarker = "AF_STRUCTURE_OBSERVATION_V1";
    private const int StructureObservationMaxChars = 3200;

    // Request/completion boundaries only. No persisted observation, additional
    // request, retrieval, scheduling decision or Tick work belongs to this lane.
    private bool CanObserveEventStructure(WorldDiplomacyJob job)
    {
        if (job == null || IsBoundMatterPlanning(job) || !IsEventPlanningJob(job)
            || !(job.EventSelectionStage == "compare" || job.Kind == "event_concept_audit")
            || !string.IsNullOrEmpty(job.EventTriggerMaterial) || !string.IsNullOrEmpty(job.DialogueArrangementId)) return false;
        var round = ResolveRound(job.RoundId);
        return IsLiveRound(round) && round.EventPhase == "forming"
            && !IsPlayerDeclarationOpening(round) && string.IsNullOrEmpty(round.RootDocumentId)
            && (round.EventSourceType == "npc_material" || string.IsNullOrEmpty(round.EventSourceType))
            && (round.EventOpeningOrigin == "npc_material" || string.IsNullOrEmpty(round.EventOpeningOrigin))
            && round.PlayerResponses.Count == 0;
    }

    private void AppendStructureObservation(WorldDiplomacyJob job, long businessInputTokens)
    {
        if (!CanObserveEventStructure(job)) return;
        try
        {
            string prompt = DiplomacyPromptTexts.Get("EventStructureObservation", "");
            int added = _host.EstimateTokens("\n" + prompt);
            int outputCap = Math.Min(job.MaxTokens > 0 ? job.MaxTokens : _host.AnalysisMaxTokens(), _host.AnalysisMaxTokens());
            // Never trim business evidence to make space for optional observation.
            // Output cap stays unchanged; complete and close business JSON first.
            bool enabled = prompt.Contains(StructureObservationMarker) && added <= 1800
                && businessInputTokens + added <= 28000 && outputCap >= 4096;
            ObservationLog("request job=" + job.JobId + " stage=" + job.EventSelectionStage
                + " enabled=" + enabled + " businessInputTokens=" + businessInputTokens
                + " addedInputTokens=" + (enabled ? added : 0) + " outputCharLimit=" + StructureObservationMaxChars
                + " outputTokenCap=" + outputCap);
            if (enabled) job.SystemPrompt += "\n" + prompt;
        }
        catch (Exception)
        {
            ObservationLog("request job=" + job.JobId + " status=unavailable");
        }
    }

    // The only recovered transport condition is truncation AFTER a complete,
    // strictly parsed business object (possibly during the optional delimiter).
    // That object still goes through every existing business validator.
    private bool ReadStructureObservation(WorldDiplomacyJob job, LlmJobResult result, out string business)
    {
        business = result.Content;
        if (!CanObserveEventStructure(job) || !(job.SystemPrompt ?? "").Contains(StructureObservationMarker)) return false;
        int split = (result.Content ?? "").IndexOf("\n" + StructureObservationMarker, StringComparison.Ordinal);
        if (split < 0)
        {
            if (result.IsOutputTruncated)
            {
                string text = (result.Content ?? "").TrimEnd();
                int line = text.LastIndexOf('\n');
                // A missing trailer or a partially emitted exact marker cannot
                // turn a complete business object into another model request.
                if (TryReadObservationBusiness(text, out string complete)
                    || line >= 0 && StructureObservationMarker.StartsWith(text.Substring(line + 1), StringComparison.Ordinal)
                        && TryReadObservationBusiness(text.Substring(0, line), out complete))
                {
                    business = complete;
                    ObservationLog("result job=" + job.JobId + " status=truncated_before_trailer completionTokens=" + result.CompletionTokens);
                    return true;
                }
            }
            ObservationLog("result job=" + job.JobId + " status=missing completionTokens=" + result.CompletionTokens);
            return false;
        }
        if (!TryReadObservationBusiness(result.Content.Substring(0, split), out string prefix)) return false;
        business = prefix;
        try
        {
            int start = split + 1 + StructureObservationMarker.Length;
            int length = result.Content.Length - start;
            string status = "unavailable";
            string payload = "";
            if (length > StructureObservationMaxChars + 2) status = "oversized";
            else if (result.IsOutputTruncated) status = "truncated";
            else
            {
                var observed = JObject.Parse(result.Content.Substring(start).Trim());
                if (observed["rows"] is JArray rows && rows.Count is > 0 and <= 6)
                {
                    var expected = job.Kind == "event_concept_audit" ? new HashSet<string> { "selected" }
                        : new HashSet<string>(JArray.Parse(job.EventCandidatePool).OfType<JObject>().Select(x => (string)x["id"]), StringComparer.Ordinal);
                    bool valid = true;
                    foreach (var token in rows)
                    {
                        if (token is not JObject row) { valid = false; break; }
                        foreach (string field in new[] { "id", "theme", "prior", "batch", "drift", "parties" })
                            if (row[field]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)row[field])
                                || ((string)row[field]).Length > 600) valid = false;
                        if (row["id"]?.Type != JTokenType.String || !expected.Remove((string)row["id"])) valid = false;
                    }
                    if (valid && expected.Count == 0) { status = "observed"; payload = observed.ToString(Formatting.None); }
                }
            }
            ObservationLog("result job=" + job.JobId + " event=" + job.RoundId + " stage=" + job.EventSelectionStage
                + " status=" + status + " diagnosticChars=" + length + " completionTokens=" + result.CompletionTokens
                + " diagnosticEstimatedTokens=" + (length <= StructureObservationMaxChars + 2
                    ? _host.EstimateTokens(result.Content.Substring(start)).ToString() : "unavailable")
                + (payload.Length == 0 ? "" : " data=" + payload));
        }
        catch (Exception) { ObservationLog("result job=" + job.JobId + " status=unavailable"); }
        return true;
    }

    private static bool TryReadObservationBusiness(string text, out string business)
    {
        business = text.Trim();
        // Strip only a complete conventional fence; never salvage a partial
        // object with the permissive general envelope parser.
        if (business.StartsWith("```", StringComparison.Ordinal) && business.EndsWith("```", StringComparison.Ordinal))
        {
            int line = business.IndexOf('\n');
            if (line >= 0 && line < business.Length - 3) business = business.Substring(line + 1, business.Length - line - 4).Trim();
        }
        try { JObject.Parse(business); return true; }
        catch (JsonException) { return false; }
    }

    private void ObservationLog(string message)
    {
        try { _host.Log("diplomacy.structure_observation " + message); }
        catch (Exception) { /* Optional diagnostic sink must never change job completion. */ }
    }
}
