using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class DiplomacyBaselineReplay
{
    internal static void Run()
    {
        EventConvergenceReplay.RestoredBaselineMigration();
        MaterialCountryThirdReplay.RestoredSingleSourceAudit();
        Test.True(typeof(WorldDiplomacyOrchestration).GetMethod("AdvanceIssueEnding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) == null,
            "retired issue scheduler is removed, not disabled behind a flag");
        Test.True(typeof(WorldDiplomacyOrchestration).GetMethod("CanQueueCountrySpeech", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) == null,
            "retired per-country speech quota is removed");
        Test.True(DiplomacyPromptTexts.Get("IssueEndingContract", "removed") == "removed",
            "actual package no longer carries the retired issue contract");
        Console.WriteLine("Baseline restoration: old event convergence, saved-state import, protected duties and single-source audit PASS; provider/live NOT_RUN.");
    }
}

internal static partial class EventConvergenceReplay
{
    internal static void RestoredBaselineMigration()
    {
        foreach (string dutyKind in new[] { "ordinary", "player", "oral" })
        {
            bool duty = dutyKind != "ordinary";
            var (_, o, r) = Fixture();
            var root = Publish(o, r, "baseline-root", "a", "b", settle: false);
            r.RootDocumentId = root.DocumentId;
            var reply = Publish(o, r, "baseline-reply", "b", "a", root.DocumentId, settle: false);
            r.EventConvergence.IssueVersion = 1;
            r.EventConvergence.Issues.Add(new() { Id = "retired", Owner = "a", Respondent = "b", Status = "open", LastDocumentId = reply.DocumentId });
            r.EventConvergence.TechnicalUnresolvedSpeakers.Add("a");
            if (dutyKind == "player") r.PlayerResponses.Add(new() { KingdomId = "a", SourceDocumentId = reply.DocumentId, Status = "pending" });
            var job = new WorldDiplomacyJob { JobId = "baseline-job", Kind = "privacy_audit", RoundId = r.RoundId,
                AuthorKingdomId = "a", TargetKingdomId = "b", SourceDocumentId = reply.DocumentId, NegotiationIssueId = "retired",
                MaterialReactionId = "extra", DeclarationAnswerSnapshotVersion = 1, DeclarationAnswerSources = new() { "other" },
                PrivacyDraft = "retired draft", SuspendedReason = "declaration_audit_invalid:required_material_reaction_answer_missing:other" };
            o.CurrentStorage.Jobs.Add(job);
            if (dutyKind == "oral")
            {
                job.DialogueArrangementId = "preserved-oral";
                o.CurrentStorage.DialogueArrangements.Add(new() { ArrangementId = job.DialogueArrangementId, RoundId = r.RoundId,
                    ActorKingdomId = "a", TargetKingdomId = "b", SourceDocumentId = reply.DocumentId, Status = "accepted",
                    SourcePlayerText = "Original request", SourceNpcText = "Original promise", Terms = new() { DurationDays = 77 } });
            }
            r.TechnicalHoldReason = r.TechnicalRequestHoldReason = job.SuspendedReason;
            r.PlayerCourtWork.Add(new() { KingdomId = "b", SourceDocumentId = root.DocumentId, Status = "event_progress_owned" });
            var saved = JsonConvert.SerializeObject(o.CurrentStorage);
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(saved)!);
            r = o.ResolveRound(r.RoundId);
            Test.True(r.EventConvergence.IssueVersion == 0 && r.EventConvergence.Issues.Count == 0,
                "new-format live event is imported once into old convergence");
            Test.True(r.PlayerCourtWork.Any(x => x.Status == "pending"), "retired court-work ownership returns to original delivery owner");
            Test.True(o.ResolveDocument(root.DocumentId).Body == root.Body && o.ResolveDocument(reply.DocumentId).Body == reply.Body,
                "published original prose is untouched");
            var remaining = o.CurrentStorage.Jobs.FirstOrDefault(x => x.JobId == job.JobId);
            Test.True(duty ? remaining?.Kind == "generate" && remaining.PrivacyDraft == "" && remaining.DeclarationAnswerSources.Count == 0
                : remaining == null, "real duty survives; ordinary retired job is removed for original scheduling");
            if (dutyKind == "oral") Test.True(remaining!.DialogueArrangementId == "preserved-oral"
                && o.CurrentStorage.DialogueArrangements.Single().SourceNpcText == "Original promise"
                && o.CurrentStorage.DialogueArrangements.Single().Terms.DurationDays == 77,
                "oral duty keeps exact promise and terms while retiring uncommitted forced-merge prose");
            Test.True(r.TechnicalHoldReason == "", "ownerless retired audit wait is cleared");
            Call(o, "EnsureConvergence", r);
            Test.True(r.EventConvergence.CompletedRounds == 1, "old round counter reconstructs a published claim-answer pair");
            int revision = r.ConversationRevision;
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            r = o.ResolveRound(r.RoundId);
            Test.True(r.ConversationRevision == revision, "reload cannot repeat migration or replenish recovery budgets");
            string state = JsonConvert.SerializeObject(r.EventConvergence);
            Test.True(!state.Contains("IssueVersion") && !state.Contains("TechnicalUnresolvedSpeakers"),
                "retired state is absent from new saves and model convergence context");
        }
        {
            var (_, o, r) = Fixture();
            r.EventConvergence.IssueVersion = 1; r.EventConvergence.TechnicalFailure = "execution_unknown";
            var doc = Publish(o, r, "pending-execution", "a", "b", settle: false);
            doc.EventExecutionPending = true;
            o.CurrentStorage.Jobs.Add(new() { JobId = "protected-execution", RoundId = r.RoundId, DocumentId = doc.DocumentId, Kind = "generate" });
            r.TechnicalHoldReason = "execution_unknown";
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            r = o.ResolveRound(r.RoundId);
            Test.True(o.CurrentStorage.Jobs.Any(x => x.JobId == "protected-execution") && o.ResolveDocument(doc.DocumentId).EventExecutionPending
                && r.TechnicalHoldReason == "execution_unknown" && r.EventConvergence.TechnicalFailure == "execution_unknown",
                "migration preserves uncertain execution and its owner without rerolling effects");
        }
        {
            var (_, o, r) = Fixture();
            r.EventConvergence.IssueVersion = 1; r.EventConvergence.Version = 1;
            r.EventConvergence.Ending = true; r.EventConvergence.Result = "war_executed";
            r.EventPhase = "closing";
            o.CurrentStorage.EventAnnouncements.Add(new() { Id = "staged-ending", EventId = r.RoundId, Stage = "ended", State = "pending" });
            o.CurrentStorage.Jobs.Add(new() { JobId = "ending-owner", Kind = "event_boundary_audit", RoundId = r.RoundId, ForcedIntent = "ended" });
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            r = o.ResolveRound(r.RoundId);
            Test.True(r.EventPhase == "closing" && r.EventConvergence.Ending && r.EventConvergence.Result == "war_executed"
                && o.CurrentStorage.EventAnnouncements.Count(x => x.Stage == "ended") == 1
                && o.CurrentStorage.Jobs.Any(x => x.JobId == "ending-owner"),
                "staged ending retains its receipt and owner without creating a second conclusion");
        }
    }
}

internal static partial class MaterialCountryThirdReplay
{
    internal static void RestoredSingleSourceAudit()
    {
        var (_, o, r, j) = EvidenceOpening();
        r.Participants.Add(new() { KingdomId = "c", State = "active", Agenda = "Separate claim", AgendaAddressTo = new() { "a" }, AgendaExpressionDocumentId = "separate-claim" });
        o.AddDocument(new() { DocumentId = "separate-claim", RoundId = r.RoundId, AuthorKingdomId = "c", TargetKingdomId = "a",
            IsReadyForPublication = true, Body = "An independent question", AnalysisStatus = "success" });
        Know(o, "a", "separate-claim");
        var evidence = SubmitEvidenceDraft(o, j, "We seek recognition of our independent position.", false);
        string prompt = j.SystemPrompt + j.UserPrompt;
        Test.True(!prompt.Contains("merged_answer_sources") && !prompt.Contains("country_speech_budget")
            && !prompt.Contains("required_material_reaction_sources"), "writer no longer receives forced merged-source requirements");
        Complete(o, j, SpanReview(evidence, true));
        Test.True(o.CurrentStorage.Documents.Any(x => x.AuthorKingdomId == "a" && x.IsReadyForPublication)
            && !j.AuditedAgendaAnswers.Contains("separate-claim"),
            "real writer/auditor publishes the current position without forcing an answer to another known country");
    }
}
