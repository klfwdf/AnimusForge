using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private static WorldDiplomacyDocument ContinuePlayerOpening(Host h, WorldDiplomacyOrchestration o,
        WorldDiplomacyRound round, WorldDiplomacyDocument previous)
    {
        int effects = h.Effects, writes = h.Writes;
        string root = round.RootDocumentId, opening = round.EventBeginningId;
        for (int turn = 2; turn <= 2; turn++)
        {
            // Deliver the NPC's prior publication before the human answers it.
            h.CurrentDayValue += 10; o.ProcessPropagationArrivals(); previous.IsRead = true;
            string body = "I reject your previous response. Explain your claim again, round " + turn + ".";
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o), body, o);
            var reply = o.CurrentStorage.Documents.Last(x => x.IsPlayerAuthored && x.Body == body);
            string provisionalId = reply.RoundId;
            var analysis = o.CurrentStorage.Jobs.Single(x => x.Kind == "analyze" && x.DocumentId == reply.DocumentId);
            o.CommitAnalysis(analysis, new JObject { ["status"] = "success", ["title_summary"] = "Player continuation " + turn,
                ["addressed_kingdom_ids"] = new JArray("a"), ["event_participation"] = "participate",
                ["response_requests"] = new JArray(new JObject { ["kingdom"] = "a", ["kind"] = "case_question", ["quote"] = body }),
                ["actions"] = new JArray(new JObject { ["intent"] = "condemn", ["target_kingdom_id"] = "a",
                    ["commitment"] = "non_binding", ["requires_response"] = true }) }.ToString());
            o.RemoveJob(analysis.JobId);
            Test.True(reply.PlayerReplyRoutingPending && !o.CurrentStorage.Jobs.Any(x => x.RoundId == provisionalId && x.Kind == "event_concept"),
                "actual analysis orders attribution before provisional opening creation: " + JsonConvert.SerializeObject(new {
                    reply.AnalysisStatus, reply.PlayerReplyRoutingPending, reply.RoundId, provisionalId,
                    jobs = o.CurrentStorage.Jobs.Select(x => new { x.Kind, x.RoundId, x.DocumentId }), round.State }));
            var route = o.CurrentStorage.Jobs.Single(x => x.Kind == "player_reply_routing" && x.DocumentId == reply.DocumentId);
            WorldDiplomacyJob obsolete = null;
            if (turn == 2)
            {
                // Reproduce an already queued old-version opening and audit.
                foreach (string kind in new[] { "event_concept", "event_concept_audit", "event_boundary_audit" })
                {
                    obsolete = new() { JobId = "legacy-" + kind, RoundId = provisionalId, ExchangeId = provisionalId,
                        Kind = kind, ForcedIntent = "started", DocumentId = reply.DocumentId,
                        EventSelectionStage = "player", EventMaterialSnapshot = "[]", PrivacyDraft = "old provisional draft" };
                    DiplomacyEventRules.Bind(o.ResolveRound(provisionalId), obsolete); o.CurrentStorage.Jobs.Add(obsolete);
                }
            }
            o.PrepareSharedRequest(route);
            Complete(o, route, new JObject { ["answered_npc_document_ids"] = new JArray(previous.DocumentId) });
            round = o.ResolveRound(round.RoundId); reply = o.ResolveDocument(reply.DocumentId);
            Test.True(reply.RoundId == round.RoundId && round.RootDocumentId == root && round.EventBeginningId == opening,
                "semantic continuation retains original root and public opening");
            Test.True(!o.CurrentStorage.Jobs.Any(x => x.JobId.StartsWith("legacy-"))
                && !o.CurrentStorage.Jobs.Any(x => x.RoundId == round.RoundId && x.Kind.StartsWith("event_concept")),
                "provisional planning and its audit are retired instead of rehomed");
            if (turn == 2)
            {
                // The actual incident persisted a migrated exhausted plan after the first reply.
                var broken = new WorldDiplomacyJob { JobId = "persisted-misroute", Kind = "event_concept", RoundId = round.RoundId,
                    ExchangeId = round.RoundId, DocumentId = reply.DocumentId, EventSelectionStage = "player", AuthorKingdomId = "p",
                    TechnicalRecoveryAttempts = 3, SuspendedReason = "player_opening_requires_grounded_npc_participant_and_actual_declaration_source" };
                DiplomacyEventRules.Bind(round, broken); o.CurrentStorage.Jobs.Add(broken);
                round.TechnicalHoldReason = round.TechnicalRequestHoldReason = broken.SuspendedReason;
                round.TechnicalHoldStartedHour = h.CurrentCampaignHours();
                var transferred = round.PlayerResponses.Single(x => x.SourceDocumentId == reply.DocumentId && x.KingdomId == "a");
                transferred.Status = "transferred";
                o.CurrentStorage.ConcurrentRounds.Add(new WorldDiplomacyRound { RoundId = "orphan-followup", State = "active",
                    EventSourceType = "player_followup", RootDocumentId = reply.DocumentId,
                    PlayerResponses = new() { new() { SourceDocumentId = reply.DocumentId, KingdomId = "a", Status = "pending" } } });
            }
            Reload(o); o.ReconcileActiveDiplomacyAfterLoad(); round = o.ResolveRound(round.RoundId); reply = o.ResolveDocument(reply.DocumentId);
            Test.True(!o.CurrentStorage.Jobs.Any(x => x.JobId == "persisted-misroute") && round.TechnicalHoldReason == "",
                "load removes the precise invalid opening and its hold");
            if (turn == 2) Test.True(o.ResolveRound("orphan-followup")?.State != "active"
                && round.PlayerResponses.Single(x => x.SourceDocumentId == reply.DocumentId && x.KingdomId == "a").Status == "pending",
                "load restores the transferred obligation and retires its unpublished duplicate shell");
            h.CurrentDayValue += 10; o.ProcessPropagationArrivals(); Call(o, "ReconcileEventQueue");
            var response = o.CurrentStorage.Jobs.SingleOrDefault(x => x.RoundId == round.RoundId && x.Kind == "generate" && x.AuthorKingdomId == "a");
            Test.True(response != null, "continued reply must schedule: " + JsonConvert.SerializeObject(new {
                turn, reply.EventExecutionPending, reply.AnalysisStatus, reply.IsReadyForPublication,
                round.PlayerResponses, round.PlayerCourtWork, jobs = o.CurrentStorage.Jobs.Select(x => new { x.Kind, x.RoundId, x.SuspendedReason }),
                arrivals = o.CurrentStorage.PropagationArrivals.Where(x => x.DocumentId == reply.DocumentId), round.TechnicalHoldReason }));
            o.PrepareSharedRequest(response);
            if (response.Kind == "privacy_material") Complete(o, response, new() { ["safe"] = true,
                ["public_context"] = "The player's new declaration reached this court; no agreement was made." });
            o.PrepareSharedRequest(response);
            CompleteWriter(o, response, new() { ["title"] = "NPC continuation " + turn,
                ["body"] = "We reject your renewed claim. Our independent authority remains our demand.",
                ["actions"] = new JArray(new JObject { ["intent"] = "statement", ["target_kingdom_id"] = "p", ["commitment"] = "non_binding" }),
                ["requires_response"] = false, ["round_participation"] = "continue", ["round_status"] = "continue",
                ["response_requests"] = new JArray(), ["answered_player_document_ids"] = new JArray(reply.DocumentId) });
            o.PrepareSharedRequest(response);
            Complete(o, response, SpanReview(Evidence(o, response), false, reply.DocumentId));
            previous = o.CurrentStorage.Documents.Single(x => x.Title == "NPC continuation " + turn && x.IsReadyForPublication);
            Test.True(round.PlayerResponses.Any(x => x.SourceDocumentId == reply.DocumentId && x.AnswerDocumentId == previous.DocumentId),
                "the second NPC reply discharges the exact continued declaration before ending");
            Reload(o); o.ReconcileActiveDiplomacyAfterLoad(); round = o.ResolveRound(round.RoundId); previous = o.ResolveDocument(previous.DocumentId);
            Test.True(round.RootDocumentId == root && round.EventBeginningId == opening && h.Writes == writes && h.Effects == effects,
                "repeated reload preserves the opening and never replays prior effects");
        }
        return previous;
    }

    private sealed class RealisticWorldHost : Host
    {
        public override string CaptureEventWorld(IReadOnlyCollection<string> ids) => new JArray(ids.Select(id => new JObject {
            ["kingdom_id"] = id, ["name"] = id, ["ruler_id"] = "ruler_" + id,
            ["towns"] = new JArray(Enumerable.Range(0, 8).Select(n => new JObject { ["id"] = id + n,
                ["geography_basis"] = "positions are proximity, not roads", ["nearby_foreign_forts"] = new string('界', 500) })) })).ToString(Formatting.None);
        public override int EstimateTokens(string text) => text.Length; // Conservative real-size stress, including complete messages.
    }

    internal static void LoreBudgetAndAudit()
    {
        var h = new RealisticWorldHost { PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0,3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o), "Our succession claim requires recognition.", o);
        var root = o.CurrentStorage.Documents.Single(); root.AnalysisStatus = "success"; root.PlayerAnalysisCommitted = true;
        Call(o, "RegisterPlayerResponseWork", root, true);
        var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept"); o.PrepareSharedRequest(job);
        var sent = JObject.Parse(job.UserPrompt);
        Test.True(((JArray)sent["sources"]!).Any(x => (string)x["source_kind"]! == "world_book_account"),
            "large campaign world cannot starve lore in the actual final request");
        Test.True(job.UserPrompt.Contains("town_sample_incomplete") && job.UserPrompt.Contains(root.Body),
            "bounded structured world explicitly reports omitted towns and keeps the complete declaration");
        Test.True(JArray.Parse(job.EventMaterialSnapshot).Any(x => (string)x["source_kind"]! == "world_book_account"),
            "final clipping retains the frozen lore used by the independent audit");
        CountryAdmissionReplay.FreezeFixture(o, job, new[] { "p", "a" }, prepareConcept: false);
        // The required-lore gate now follows selected-country supplementation.
        // Exercise a completed but empty dossier; the optimization suite also
        // covers real supplementation supplying the initially missing lore.
        job.EventComparison = "{\"supplemented\":true}";
        h.MaterialOverride = "[]"; job.EventMaterialSnapshot = ""; job.SuspendedReason = ""; o.PrepareSharedRequest(job);
        Test.True(job.SuspendedReason == "event_required_world_book_missing", "missing lore after supplementation blocks generation instead of silently falling back to world statistics");
    }
}
