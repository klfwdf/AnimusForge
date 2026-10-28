using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private static bool IsWorldBookWait(WorldDiplomacyJob job) => job.SuspendedReason == "event_required_world_book_missing"
        || job.SuspendedReason.StartsWith("event_world_book_wait:", StringComparison.Ordinal);

    private bool CheckPlayerWorldBookAvailability(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        if (!IsPlayerDeclarationOpening(round) || _host is not IDiplomacyWorldBookHost host) return true;
        var library = host.ReadWorldBookAvailability();
        if (library.state == "ready" && library.count > 0) return true;
        WaitForPlayerWorldBook(job, round);
        return false;
    }

    private void WaitForPlayerWorldBook(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        if (_host is not IDiplomacyWorldBookHost host)
        { SuspendDiplomacyJob(job, "event_required_world_book_missing"); return; }
        var library = host.ReadWorldBookAvailability();
        string state = library.state == "ready" ? "no_match" : library.state;
        string reason = "event_world_book_wait:" + state;
        job.WorldBookWaitRevision = library.revision ?? "";
        if (job.SuspendedReason != reason) HoldTechnicalRequest(job, reason);
        job.SuspendedReason = reason;
        job.IsRunning = false;
        job.RequestPreparationPending = false;
        job.SystemPrompt = ""; job.UserPrompt = ""; job.LlmMessages?.Clear();
        job.RetryAfterUtcTicks = 0;
        if (job.WorldBookWaitNotice == state) return;
        job.WorldBookWaitNotice = state;
        string message = state == "empty"
            ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldBookEmpty", "The world book is empty. Your declaration is preserved, but the event opening needs background sources. Import a world book; the event will retry automatically.")
            : state == "no_match"
                ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldBookNoMatch", "The world book is loaded, but no usable background source was found for this event. Your declaration is preserved. Update the relevant world-book content; the event will retry automatically.")
                : state == "load_failed"
                    ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldBookLoadFailed", "The world book could not be loaded. Your declaration is preserved and its event is waiting. Check the world-book data and import a valid copy to continue.")
                    : DiplomacyUiLocalization.Text("AF_DIP_UI_WorldBookUnavailable", "The world book is not ready. Your declaration is preserved; its event will retry when the background sources become available.");
        _host.Notify(message);
        _host.Log("diplomacy.world_book_wait event=" + round.RoundId + " state=" + state + " rules=" + library.count);
    }

    // Called only by the existing throttled held-job probe. The host reads O(1)
    // library metadata; a same-source wait never spends a model/retry budget.
    private bool ResumePlayerWorldBook(WorldDiplomacyJob job, WorldDiplomacyRound round)
    {
        if (_host is not IDiplomacyWorldBookHost host || !IsPlayerDeclarationOpening(round)) return false;
        var library = host.ReadWorldBookAvailability();
        if (library.state != "ready" || library.count <= 0)
        { WaitForPlayerWorldBook(job, round); return false; }
        bool legacy = job.SuspendedReason == "event_required_world_book_missing";
        if (!legacy && job.WorldBookWaitNotice == "no_match" && job.WorldBookWaitRevision == library.revision) return false;
        job.WorldBookWaitRevision = library.revision ?? "";
        job.EventMaterialSnapshot = ""; job.EventProbeSnapshot = ""; job.EventProbeVersion = "";
        job.AdmissionSupplementedCountries.Clear();
        if (WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.EventComparison) is JObject comparison)
        { comparison["supplemented"] = false; job.EventComparison = comparison.ToString(Formatting.None); }
        _completedPlanningCaptures.Remove(job); _planningCaptureInputs.Remove(job);
        job.MaterialRepairFeedback = ""; job.PrivacyDraft = ""; job.PrivacySourceMaterial = "";
        if (job.Kind == "event_concept_audit") job.Kind = "event_concept";
        if (job.Kind == "event_boundary_audit") job.Kind = "event_boundary";
        _host.Log("diplomacy.world_book_resumed event=" + round.RoundId + " rules=" + library.count);
        return true;
    }

    private static JArray SelectWorldBookEvidence(JArray sources) => new(sources.OfType<JObject>()
        .Where(x => (string)x["source_kind"] == "world_book_account" && !string.IsNullOrWhiteSpace((string)x["text"]))
        .Select(x => x.DeepClone()));

    private static bool HasWorldBookEvidence(JArray sources) => sources.OfType<JObject>()
        .Any(x => (string)x["source_kind"] == "world_book_account" && !string.IsNullOrWhiteSpace((string)x["text"]));

    private static JArray ReadEventLore(WorldDiplomacyRound round, string author = null)
    {
        if (string.IsNullOrEmpty(round?.EventLoreEvidence)) return new JArray();
        try
        {
            var sources = SelectWorldBookEvidence(JArray.Parse(round.EventLoreEvidence));
            return author == null ? sources : new JArray(sources.OfType<JObject>()
                .Where(x => (string)x["known_by"] == "public" || (string)x["known_by"] == author).Select(x => x.DeepClone()));
        }
        catch (JsonException) { return new JArray(); }
    }

    private string CompactPlanningWorld(string raw, int budget)
    {
        if (_host.EstimateTokens(raw) <= budget) return raw;
        JArray world;
        try { world = JArray.Parse(raw); }
        catch (JsonException) { return raw; }
        // Remove complete optional town records, never prose or geographic
        // qualifications. Realm identities, wars and relationships stay intact.
        var towns = world.OfType<JObject>().Where(x => x["towns"] is JArray)
            .SelectMany(x => ((JArray)x["towns"]).Select(t => new { owner = x, town = t,
                cost = _host.EstimateTokens(t.ToString(Formatting.None)) })).OrderByDescending(x => x.cost).ToList();
        int remaining = _host.EstimateTokens(raw);
        foreach (var entry in towns)
        {
            if (remaining <= budget) break;
            entry.town.Remove(); remaining -= entry.cost;
            entry.owner["omitted_town_records"] = ((int?)entry.owner["omitted_town_records"] ?? 0) + 1;
            entry.owner["town_sample_incomplete"] = true;
        }
        return world.ToString(Formatting.None);
    }

    // Reserve the full evidence and proposed audit result first. Geography is a
    // labelled sample; optional town records cannot crowd out exact obligations.
    // This only projects the outgoing request, never rewrites stored evidence.
    private void FitOptionalEventWorld(WorldDiplomacyJob job)
    {
        const int framingReserve = 512;
        int limit = WorldDiplomacyRequestHistoryApplication.DeclarationInputLimit;
        if (_host.EstimateTokens(job.SystemPrompt) + _host.EstimateTokens(job.UserPrompt) + framingReserve <= limit) return;
        var payload = JObject.Parse(job.UserPrompt);
        string world = (string)payload["world"];
        if (string.IsNullOrEmpty(world)) return;
        payload["world"] = "";
        int budget = Math.Max(0, limit - framingReserve - _host.EstimateTokens(job.SystemPrompt)
            - _host.EstimateTokens(payload.ToString(Formatting.None)));
        payload["world"] = CompactPlanningWorld(world, budget);
        job.UserPrompt = payload.ToString(Formatting.None);
    }

    private bool EnsureBoundaryWorldBook(WorldDiplomacyJob job, WorldDiplomacyRound round, List<string> participants)
    {
        if (!IsPlayerDeclarationOpening(round) || HasWorldBookEvidence(ReadEventLore(round))) return true;
        if (!CheckPlayerWorldBookAvailability(job, round)) return false;
        // Legacy events have no retained evidence. Recover it at request time;
        // do not rewrite their already published opening or execute its actions.
        string raw = CapturePlanningSources(job, participants.Take(6).ToList(), Array.Empty<string>());
        if (raw == null)
        { job.RequestPreparationPending = true; return false; }
        JArray captured;
        try { captured = JArray.Parse(raw); }
        catch (JsonException) { SuspendDiplomacyJob(job, "event_material_snapshot_invalid"); return false; }
        int budget = 8000;
        var selected = new JArray();
        foreach (var row in captured.OfType<JObject>().Where(x => (string)x["source_kind"] == "world_book_account")
            .OrderByDescending(x => (bool?)x["topic_query"] == true))
        {
            int cost = _host.EstimateTokens(row.ToString(Formatting.None));
            if (cost > budget) continue;
            selected.Add(row.DeepClone()); budget -= cost;
        }
        if (!HasWorldBookEvidence(selected))
        { WaitForPlayerWorldBook(job, round); return false; }
        round.EventLoreEvidence = selected.ToString(Formatting.None);
        return true;
    }

    // The published declaration owns the case. Planning is durable work on that
    // owner, not a new NPC event or a disposable ordinary opportunity.
    private bool IsPlayerDeclarationOpening(WorldDiplomacyRound round) => round != null
        && round.DialogueArrangementId == "player_manual_declaration"
        && ResolveDocument(round.RootDocumentId)?.IsPlayerAuthored == true;

    private bool EnsurePlayerOpeningPlan(WorldDiplomacyRound round)
    {
        if (EventHost == null || !IsPlayerDeclarationOpening(round) || !string.IsNullOrEmpty(round.EventPartyReview)) return true;
        // An accepted old boundary owns its frozen side effects. Migration must
        // not replace it with a newly planned payload after execution started.
        if (Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "started")
            || Storage.EventEffects.Any(x => x.EventId == round.RoundId)) return true;
        if (Storage.Jobs.Any(x => x.RoundId == round.RoundId && IsEventPlanningJob(x))) return false;
        var root = ResolveDocument(round.RootDocumentId);
        if (root.AnalysisStatus == "pending_analysis") return false;
        QueuePlayerReplyRoutingCheck(root);
        if (root.PlayerReplyRoutingPending) return false;
        foreach (var obsolete in Storage.Jobs.Where(x => x.RoundId == round.RoundId
            && x.Kind is "event_boundary" or "event_boundary_audit").ToList())
        {
            if (round.TechnicalHoldReason == obsolete.SuspendedReason
                || round.TechnicalRequestHoldReason == obsolete.SuspendedReason)
            {
                AccrueTechnicalPause(round);
                round.TechnicalHoldReason = ""; round.TechnicalRequestHoldReason = "";
                round.TechnicalHoldStartedHour = -1;
            }
            RemoveJob(obsolete.JobId);
        }
        var candidates = _host.AllKingdomIds().Where(x => _host.HasIndependentAuthority(x)
            && !_host.IsEliminatedParty(x)).Append(root.AuthorKingdomId).Distinct().ToList();
        round.EventPhase = "forming";
        round.EventOpeningOrigin = "player_declaration_v2";
        var targets = GetDocumentTargetIds(root).Concat(root.MentionedKingdomIds ?? new())
            .Concat(root.AddressedKingdomIds ?? new()).Where(candidates.Contains).Distinct().ToList();
        EnqueueJob(new WorldDiplomacyJob { JobId = _host.NewId("event_concept"), Kind = "event_concept",
            RoundId = round.RoundId, ExchangeId = round.RoundId, AuthorKingdomId = root.AuthorKingdomId,
            DocumentId = root.DocumentId, CandidateKingdomIds = candidates, EventSelectionStage = "player",
            PlayerLoreContractVersion = 1,
            EventInvestigationKingdoms = targets.Where(x => x != root.AuthorKingdomId).Take(5).ToList(),
            EventTriggerMaterial = JsonConvert.SerializeObject(new { root.DocumentId, root.AuthorKingdomId,
                root.Title, root.Body, root.Actions, root.AddressedKingdomIds, root.ResponseRequests }),
            EventSelectedCandidate = "{}", EventComparison = "{}", RequestLane = round.BirthLane,
            Priority = 110, CreatedDay = _host.CurrentDay(), MaxTokens = _host.AnalysisMaxTokens() });
        _host.Log("diplomacy.player_opening_planned event=" + round.RoundId + " document=" + root.DocumentId);
        return false;
    }

    private void AddPlayerOpeningSource(WorldDiplomacyRound round, JArray sources)
    {
        if (!IsPlayerDeclarationOpening(round)) return;
        var root = ResolveDocument(round.RootDocumentId);
        sources.Insert(0, new JObject { ["key"] = root.DocumentId, ["known_by"] = root.AuthorKingdomId,
            ["source_kind"] = "published_player_declaration", ["text"] = root.Body,
            ["hero_ids"] = new JArray(_host.PartyRulerId(root.AuthorKingdomId)), ["excerpt"] = false });
    }

    private void RegisterPlayerOpeningResponses(WorldDiplomacyRound round)
    {
        if (!IsPlayerDeclarationOpening(round) || string.IsNullOrEmpty(round.EventPartyReview)) return;
        var root = ResolveDocument(round.RootDocumentId);
        // These countries were admitted on evidence before the opening. This is
        // not automatic admission of every recipient of a broadcast.
        foreach (var party in round.Participants.Where(x => x.State == "active" && !_host.IsPlayerParty(x.KingdomId)))
        {
            if (round.PlayerResponses.Any(x => x.SourceDocumentId == root.DocumentId && x.KingdomId == party.KingdomId)) continue;
            round.PlayerResponses.Add(new WorldDiplomacyPlayerResponse { SourceDocumentId = root.DocumentId,
                OriginalRoundId = round.RoundId, KingdomId = party.KingdomId, CreatedDay = root.Day,
                RequestKind = "case_question", RequestQuote = root.Body });
        }
        _diplomacyWorkNeedsReconcile = true;
    }

    private void RecoverUnpublishedPlayerOpenings()
    {
        // Load boundary only. Preserve completed openings, declarations, actions,
        // receipts and existing planning jobs. Retire only the obsolete writer.
        foreach (var round in GetLiveRounds().Where(IsPlayerDeclarationOpening).ToList())
        {
            if (EventBoundaryComplete(round, "started")) { RegisterPlayerOpeningResponses(round); continue; }
            foreach (var job in Storage.Jobs.Where(x => x.RoundId == round.RoundId && IsEventPlanningJob(x)
                && x.PlayerLoreContractVersion < 1).ToList())
            {
                job.PlayerLoreContractVersion = 1;
                job.EventMaterialSnapshot = ""; job.PrivacyDraft = ""; job.Kind = "event_concept";
                job.LlmMessages?.Clear();
                if (job.SuspendedReason.StartsWith("player_opening_", StringComparison.Ordinal)
                    || job.SuspendedReason.StartsWith("input budget exceeded", StringComparison.Ordinal))
                    TryResumeTechnicalRequest(job, true);
            }
            if (!EnsurePlayerOpeningPlan(round)) continue;
            if (!Storage.Jobs.Any(x => x.RoundId == round.RoundId
                && (IsEventPlanningJob(x) || x.Kind is "event_boundary" or "event_boundary_audit")))
                EnsureEventBoundary(round, "started");
        }
    }
}
