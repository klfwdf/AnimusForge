using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // Request/completion only. Obvious affirmative announcements are rejected
    // independently of the semantic auditor; negations and conditional threats
    // still belong to the auditor rather than a broad keyword ban.
    private static readonly Lazy<Regex> SilenceWarAnnouncement = new(() => new Regex(
        DiplomacyCompatibilityTexts.Get("Event_SilenceWarAnnouncementPattern"),
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled));
    private static readonly Lazy<string[]> SilenceWarTitles = new(() =>
        DiplomacyCompatibilityTexts.Get("Event_SilenceWarTitles").Split('|'));
    private static readonly Lazy<string[]> SilenceTitleNegations = new(() =>
        DiplomacyCompatibilityTexts.Get("Event_SilenceTitleNegations").Split('|'));

    private bool ValidateSilenceProse(WorldDiplomacyJob job, JObject draft)
    {
        if (string.IsNullOrEmpty(job.SilenceSourceDocumentId) || job.SilenceConsequence != "ordinary") return true;
        string title = (string)draft?["title"] ?? "", body = (string)draft?["body"] ?? "";
        bool titleAnnouncement = SilenceWarTitles.Value.Any(title.Contains)
            || string.Equals(title.Trim(), "Declaration of war", StringComparison.OrdinalIgnoreCase);
        bool titleNegated = SilenceTitleNegations.Value.Any(title.Contains)
            || title.StartsWith("No ", StringComparison.OrdinalIgnoreCase) || title.StartsWith("Not ", StringComparison.OrdinalIgnoreCase);
        return !(titleAnnouncement && !titleNegated) && !SilenceWarAnnouncement.Value.IsMatch(body);
    }

    private string SilenceSourceContext(WorldDiplomacyJob job)
    {
        var source = ResolveDocument(job.SilenceSourceDocumentId);
        var round = ResolveRound(job.RoundId);
        return JsonConvert.SerializeObject(new { source_document_id = source?.DocumentId, source?.Title, source?.Body,
            source?.AuthorKingdomId, source?.TargetKingdomId, source?.Actions, event_topic = round?.RoundTopic,
            consequence = job.SilenceConsequence });
    }

    private string SilenceExecutionContext(WorldDiplomacyJob job) => DiplomacyPromptTexts.Get("PlayerSilenceClosure", "")
        + "\n" + SilenceSourceContext(job);

    private bool BindSilenceConsequence(WorldDiplomacyJob job, ref string raw)
    {
        if (string.IsNullOrEmpty(job?.SilenceSourceDocumentId)) return true;
        var source = ResolveDocument(job.SilenceSourceDocumentId);
        if (!IsCurrentSilenceJob(job) || source?.LegacyNoticeSuppressed == true
            || source?.AuthorKingdomId != job.AuthorKingdomId || CurrentPlayerAuthority() != job.TargetKingdomId
            || !GetDocumentTargetIds(source).Contains(job.TargetKingdomId))
        { SuspendDiplomacyJob(job, "silence_source_or_authority_changed"); return false; }
        if (job.SilenceConsequence is not ("conflict" or "ordinary")
            || !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(raw, out var json))
        { SuspendDiplomacyJob(job, "silence_classification_missing"); return false; }
        if (!ValidateSilenceProse(job, json))
        {
            job.PrivacyDraft = raw;
            RewriteDeclaration(job, "ordinary_silence_prose_announces_unexecuted_war", DiplomacyPromptTexts.Get("SilenceOrdinaryWarRepair", ""));
            return false;
        }
        string intent = job.SilenceConsequence == "conflict" ? "declare_war" : "statement";
        if (intent == "declare_war" && TryGetDiplomaticStateViolation(intent, job.AuthorKingdomId, job.TargetKingdomId, out var reason))
        { SuspendDiplomacyJob(job, "silence_war_not_executable:" + reason); return false; }
        if (intent == "statement" && (_host is not IDiplomacySilenceEffectHost || EventHost == null))
        { SuspendDiplomacyJob(job, "silence_effect_host_unavailable"); return false; }
        var beforeBinding = (JObject)json.DeepClone();
        // The model authors prose; the agreed consequence and target are program-owned.
        bool bound = json["actions"] is JArray rows && rows.Count == 1 && rows[0] is JObject row
            && (string)row["intent"] == intent && (string)row["target_kingdom_id"] == job.TargetKingdomId
            && (string)row["commitment"] == WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent)
            && row["requires_response"]?.Value<bool>() == false;
        if (!bound) json["actions"] = new JArray(new JObject { ["target_kingdom_id"] = job.TargetKingdomId,
            ["intent"] = intent, ["negotiation_move"] = "end_negotiation", ["commitment"] = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent), ["requires_response"] = false });
        json["target_kingdom_id"] = job.TargetKingdomId; json["intent"] = intent;
        json["commitment"] = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent);
        json["round_participation"] = "withdraw"; json["round_status"] = "continue"; json["requires_response"] = false;
        json["response_requests"] = new JArray();
        if (!JToken.DeepEquals(beforeBinding, json)) job.PrivacyApproved = false;
        raw = json.ToString(Formatting.None);
        return true;
    }

    private WorldDiplomacyJob SilenceWork(WorldDiplomacyDocument document)
    {
        if (!string.IsNullOrEmpty(document?.SilenceSourceDocumentId))
            return new WorldDiplomacyJob { RoundId = document.RoundId, DocumentId = document.DocumentId,
                AuthorKingdomId = document.AuthorKingdomId, TargetKingdomId = document.TargetKingdomId,
                SilenceSourceDocumentId = document.SilenceSourceDocumentId, SilenceConsequence = document.SilenceConsequence };
        return Storage.Jobs.FirstOrDefault(x => x.DocumentId == document?.DocumentId && !string.IsNullOrEmpty(x.SilenceSourceDocumentId));
    }

    private bool ValidateSilenceDocumentForExecution(WorldDiplomacyDocument document)
    {
        if (string.IsNullOrEmpty(document?.SilenceSourceDocumentId)) return true;
        if (document.IsReadyForPublication) return false;
        var job = SilenceWork(document);
        var source = ResolveDocument(job.SilenceSourceDocumentId);
        if (IsCurrentSilenceJob(job) && source?.LegacyNoticeSuppressed != true
            && source?.AuthorKingdomId == document.AuthorKingdomId
            && CurrentPlayerAuthority() == document.TargetKingdomId) return true;
        document.EventExecutionPending = false;
        SuppressInvalidDocumentBeforePropagation(document, "silence_source_or_authority_changed");
        return false;
    }

    internal bool FinalizeSilenceBeforePublication(WorldDiplomacyDocument document)
    {
        var job = SilenceWork(document);
        if (job == null) return true;
        FinishSilenceConsequence(job, document);
        var wait = ResolveRound(job.RoundId)?.PlayerNegotiationWaits.FirstOrDefault(x => x.SourceDocumentId == job.SilenceSourceDocumentId);
        if (wait?.ClosureDocumentId == document.DocumentId) return true;
        document.IsReadyForPublication = false;
        return false;
    }

    private void FinishSilenceConsequence(WorldDiplomacyJob job, WorldDiplomacyDocument document)
    {
        if (string.IsNullOrEmpty(job.SilenceSourceDocumentId) || document?.IsReadyForPublication != true) return;
        var wait = ResolveRound(job.RoundId)?.PlayerNegotiationWaits.FirstOrDefault(x => x.SourceDocumentId == job.SilenceSourceDocumentId);
        if (wait == null || !string.IsNullOrEmpty(wait.ClosureDocumentId)) return;
        if (job.SilenceConsequence == "conflict" && document.Actions?.Any(x => x.Intent == "declare_war"
            && x.TargetKingdomId == job.TargetKingdomId && x.ChangedDiplomaticState) != true)
        {
            // Never turn a failed game action into successful political closure.
            document.RoundParticipation = "continue"; wait.RequiresAnswer = true;
            var live = Storage.Jobs.FirstOrDefault(x => x.DocumentId == document.DocumentId);
            if (live != null) SuspendDiplomacyJob(live, "silence_war_execution_unconfirmed");
            var round = ResolveRound(document.RoundId);
            if (round != null) round.TechnicalHoldReason = "silence_war_execution_unconfirmed";
            return;
        }
        if (job.SilenceConsequence == "ordinary" && !ApplySilenceLoyaltyPenalty(job))
        {
            var round = ResolveRound(document.RoundId);
            if (round != null) round.TechnicalHoldReason = "silence_loyalty_effect_unconfirmed";
            _host.Log("silence loyalty effect unconfirmed source=" + job.SilenceSourceDocumentId);
            return;
        }
        document.SilenceConsequenceSettled = true;
        wait.ClosureDocumentId = document.DocumentId;
        wait.RequiresAnswer = false;
        ReconcileSettledSilence(ResolveRound(job.RoundId), document);
        _host.Log("player silence consequence settled source=" + wait.SourceDocumentId + " kind=" + job.SilenceConsequence
            + " document=" + document.DocumentId);
    }

    private void RecoverSettledSilence(WorldDiplomacyRound round)
    {
        // Visit this event's finite wait identities, never the document archive.
        foreach (var wait in round.PlayerNegotiationWaits.Where(x => !string.IsNullOrEmpty(x.ClosureDocumentId)))
            ReconcileSettledSilence(round, ResolveDocument(wait.ClosureDocumentId));
    }

    private void ReconcileSettledSilence(WorldDiplomacyRound round, WorldDiplomacyDocument document)
    {
        if (round == null || document?.IsReadyForPublication != true || !document.SilenceConsequenceSettled
            || document.RoundId != round.RoundId || string.IsNullOrEmpty(document.SilenceSourceDocumentId)) return;
        var wait = round.PlayerNegotiationWaits.FirstOrDefault(x => x.SourceDocumentId == document.SilenceSourceDocumentId);
        var source = ResolveDocument(document.SilenceSourceDocumentId);
        if (wait?.ClosureDocumentId != document.DocumentId || !string.IsNullOrEmpty(wait.AnswerDocumentId)
            || source?.IsReadyForPublication != true || source.RoundId != round.RoundId
            || source.AuthorKingdomId != document.AuthorKingdomId || !GetDocumentTargetIds(source).Contains(document.TargetKingdomId)
            || document.SilenceConsequence is not ("ordinary" or "conflict")) return;
        bool war = document.Actions?.Any(x => x.Intent == "declare_war" && x.TargetKingdomId == document.TargetKingdomId && x.ChangedDiplomaticState) == true;
        if (document.SilenceConsequence == "conflict" && !war) return;
        foreach (var threat in Storage.DiplomaticThreats.Where(x => x.StageRoundId == round.RoundId && x.Status == "open"
            && x.StageDocumentId == source.DocumentId && x.IssuerKingdomId == document.AuthorKingdomId && x.TargetKingdomId == document.TargetKingdomId))
        {
            // Preserve a real recorded refusal and its existing prestige rule.
            // Silence itself never supplies a target decision or compliance.
            if (threat.TargetDecision == "noncomplied" && !war)
            {
                if (_host is not IWorldDiplomacyThreatMechanicsHost) continue;
                WorldDiplomacyThreatSettlementApplication.ApplyDiplomaticThreatReputationPenalty(Storage, _host.ThreatSettlement(), this, threat, document);
                continue;
            }
            if (threat.TargetDecision != "pending") continue;
            threat.Status = war ? "enforced" : "silence_concluded";
            threat.ResolutionRoundId = round.RoundId; threat.ResolutionDocumentId = document.DocumentId;
            threat.ResolutionReason = war ? "silence_war_executed" : "silence_ordinary_concluded";
            threat.UpdatedDay = _host.CurrentDay(); threat.ObligationRoundId = ""; threat.ObligationClaimedDay = 0;
        }
        var state = round.EventConvergence;
        state.ActualWar |= war;
        // A confirmed silence receipt is also a terminal result for the restored
        // event-level owner. Do not ask the human to author a second final turn.
        if (war || string.IsNullOrEmpty(state.Result))
            state.Result = war ? "war_executed" : "silence_concluded";
    }

    private bool ApplySilenceLoyaltyPenalty(WorldDiplomacyJob job)
    {
        if (_host is not IDiplomacySilenceEffectHost towns || EventHost == null) return false;
        EnsureEventRuntimeIndex();
        int day = _host.CurrentDay();
        bool confirmed = true;
        foreach (string town in towns.KingdomTownIds(job.TargetKingdomId).Distinct(StringComparer.Ordinal))
        {
            string id = "silence_loyalty:" + job.TargetKingdomId + ":" + town;
            if (!_eventEffects.TryGetValue(id, out var effect))
            {
                effect = new DiplomacyEventEffectState { Id = id, EventId = job.RoundId, Stage = "silence",
                    Kind = "town_loyalty", KingdomId = job.TargetKingdomId, SettlementId = town, Delta = -0.5f,
                    StartedDay = day, ExpiresDay = day + 7, Reason = "ignored_diplomatic_correspondence" };
                _eventEffects[id] = effect; Storage.EventEffects.Add(effect); _activeEventEffects.Add(effect);
            }
            else
            {
                effect.ExpiresDay = Math.Max(effect.ExpiresDay, day + 7);
                if (effect.Status is "expired" or "unavailable" or "removed") effect.Status = "active";
                if (!_activeEventEffects.Contains(effect)) _activeEventEffects.Add(effect);
            }
            DiplomacyEventEffects.Pulse(effect, day, EventHost);
            confirmed &= effect.Status == "active" && string.IsNullOrEmpty(effect.Failure);
        }
        return confirmed;
    }
}
