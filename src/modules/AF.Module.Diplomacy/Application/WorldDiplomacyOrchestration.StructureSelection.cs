using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // Ordinary, disposable NPC preparation only. The business verdict remains
    // separate from optional observation; no extra model call or Tick research.
    private bool CanSelectEventStructure(WorldDiplomacyJob job)
    {
        if (job == null || !IsEventPlanningJob(job) || IsBoundMatterPlanning(job)
            || !string.IsNullOrEmpty(job.EventTriggerMaterial) || !string.IsNullOrEmpty(job.DialogueArrangementId)) return false;
        var round = ResolveRound(job.RoundId);
        return CanContinueMaterialSearch(round) && string.IsNullOrEmpty(round.RootDocumentId)
            && (round.EventSourceType == "npc_material" || string.IsNullOrEmpty(round.EventSourceType))
            && (round.EventOpeningOrigin == "npc_material" || string.IsNullOrEmpty(round.EventOpeningOrigin))
            && round.PlayerResponses.Count == 0;
    }

    private static string ResearchHint(JObject result) => result?["research_hint"]?.Type == JTokenType.String
        && !string.IsNullOrWhiteSpace((string)result["research_hint"]) && ((string)result["research_hint"]).Length <= 600
            ? (string)result["research_hint"] : "";

    private void RecordMaterialSearchFailure(WorldDiplomacyJob job, WorldDiplomacyRound round, string reason, JObject result)
    {
        // Replace, never recursively nest evidence. Existing chunked round
        // persistence owns these values; no new save key or background write.
        round.EventSelectionEvidence = JsonConvert.SerializeObject(new {
            outcome = "no_qualified_material", reason, research_hint = ResearchHint(result),
            previous_theme = job.EventExplorationTheme, theme_pass = job.EventThemePass,
            stage = job.EventSelectionStage, search_pass = job.MaterialSearchPass,
            job.EventCandidatePool, job.EventComparison, job.EventSelectedCandidate,
            rejected_draft = job.PrivacyDraft, audit = result
        });
    }

    private bool RejectStoryCandidate(WorldDiplomacyJob job, string reason, JObject audit)
    {
        if (!CanSelectEventStructure(job)) return false;
        var round = ResolveRound(job.RoundId);
        _host.Log("diplomacy.structure_rejected job=" + job.JobId + " pass=" + job.MaterialSearchPass + " reason=" + reason);
        // Retire the selected seed before the common search reset can mark it
        // reusable. Qualified siblings retain their original comparison chance.
        foreach (var lead in Storage.MaterialLeads.Where(x => x.EventId == round.RoundId && x.Status == "selected_unpublished"))
            lead.Status = "not_viable";
        NoQualifiedMaterialReviewed(job, round, reason, audit);
        return true;
    }

    private string DirectedExplorationQuery(WorldDiplomacyJob job)
    {
        string theme = ThemeQuery(job);
        if (!CanSelectEventStructure(job) || job.MaterialSearchPass == 0) return theme;
        var evidence = MaterialSearchOutcome(ResolveRound(job.RoundId));
        // A gap belongs to the inspected direction. Do not drag it into the new
        // direction after the existing two-investigation allowance is exhausted.
        string hint = ResearchHint(evidence);
        if (hint.Length == 0 || (string)evidence["previous_theme"] != job.EventExplorationTheme) return theme;
        return theme + "\n" + DiplomacyPromptTexts.Get("EventStructureResearch", "") + "\n" + hint;
    }

    internal static bool IsThematicResearchTopic(string topic) => !string.IsNullOrWhiteSpace(topic)
        && EventThemeIds.Any(id => {
            string query = DiplomacyPromptTexts.Get("EventThemeQuery_" + id, "");
            return query.Length > 0 && (topic == query || topic.StartsWith(query + "\n", StringComparison.Ordinal));
        });

    private string ExhaustedExplorationTheme(WorldDiplomacyJob job)
    {
        if (job.MaterialSearchPass == 0 || string.IsNullOrEmpty(job.RoundId)) return "";
        var evidence = MaterialSearchOutcome(ResolveRound(job.RoundId));
        return (string)evidence?["outcome"] == "no_qualified_material"
            && evidence?["theme_pass"]?.Type == JTokenType.Integer && (int)evidence["theme_pass"] < job.EventThemePass
                ? (string)evidence["previous_theme"] ?? "" : "";
    }

    private bool ResumeRejectedStory(WorldDiplomacyJob job)
    {
        // Old held semantic verdicts consume the same persisted batch budget.
        // Unknown/technical errors are not reclassified or given new attempts.
        string reason = job.SuspendedReason ?? "";
        bool semantic = reason.StartsWith("event_concept_audit_failed:theme_motive_mismatch:", StringComparison.Ordinal)
            || reason.StartsWith("event_concept_audit_failed:story_structure_duplicate:", StringComparison.Ordinal)
            || reason == "event_concept_audit_failed:story_motive_replaced"
            || reason.StartsWith("event_concept_audit_failed:story_motive_replaced:", StringComparison.Ordinal)
            || reason == "event_story_already_used_without_new_motive";
        return semantic && RejectStoryCandidate(job, reason, null);
    }
}
