using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Production writer/reviewer/publication/ending owners. Semantic decisions and
// game effects are controlled leaves; this is not provider or live acceptance.
internal static partial class MaterialCountryThirdReplay
{
    internal static void FinishStallBoundary(WorldDiplomacyOrchestration o, WorldDiplomacyRound r)
    {
        var job = o.CurrentStorage.Jobs.Single(x => x.RoundId == r.RoundId && x.ForcedIntent == "ended");
        o.PrepareSharedRequest(job);
        var draft = new JObject { ["title"] = "Conclusion of the correspondence", ["scene"] = "Negotiation",
            ["body"] = r.EventConvergence.ActualWar ? "The confirmed war was executed. No further consent is presumed."
                : "The correspondence has concluded without mutual agreement. Unpublished technical failures remain unresolved.",
            ["effects"] = new JArray(), ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray(),
            ["progress_evidence"] = new JArray(), ["narrative"] = new JArray() };
        Complete(o, job, draft);
        Test.True(job.Kind == "event_boundary_audit", "the real ending writer reaches independent review: " + job.SuspendedReason);
        o.PrepareSharedRequest(job); Complete(o, job, Audit(job, Array.Empty<string[]>()));
        Test.True(o.CurrentStorage.EventAnnouncements.Count(x => x.EventId == r.RoundId && x.Stage == "ended" && x.State == "public") == 1,
            "the real writer/auditor/publication path emits one ending announcement: " + job.SuspendedReason);
        Reload(o);
        Test.True(o.CurrentStorage.EventAnnouncements.Count(x => x.EventId == r.RoundId && x.Stage == "ended" && x.State == "public") == 1
            && !o.CurrentStorage.Jobs.Any(x => x.RoundId == r.RoundId && x.ForcedIntent == "ended"),
            "completed ending survives restart without another announcement job");
    }


}

internal static partial class LiveDiplomacyFixReplay
{
    internal static void StallSilenceClosure()
    {
        var recorded = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "diplomacy-stalls-20261027.json")))["silence"]!;
        foreach (bool conflict in new[] { false, true })
        foreach (bool legacy in new[] { false, true })
        {
            var (h, o, r) = Fixture("p");
            var source = Doc(o, r, (string)recorded["source_id"]!, "a", "p", "ultimatum");
            source.RequiresResponse = true; source.PlayerLifecycleClassified = true; Known(o, "p", source.DocumentId);
            o.ProcessDiplomaticThreatDocument(source, "a", "p");
            var threat = o.CurrentStorage.DiplomaticThreats.Single();
            var wait = new WorldDiplomacyPlayerWait { SourceDocumentId = source.DocumentId, NotifiedHour = h.CurrentCampaignHours() - 72 };
            r.PlayerNegotiationWaits.Add(wait);
            var closure = Doc(o, r, (string)recorded["closure_id"]!, "a", "p", conflict ? "declare_war" : "statement");
            closure.SilenceSourceDocumentId = source.DocumentId; closure.SilenceConsequence = conflict ? "conflict" : "ordinary";
            closure.Actions = new() { new() { Intent = closure.Intent, TargetKingdomId = "p", ChangedDiplomaticState = conflict } };
            if (legacy)
            { closure.SilenceConsequenceSettled = true; wait.ClosureDocumentId = closure.DocumentId; wait.RequiresAnswer = false; }
            else
            {
                var job = new WorldDiplomacyJob { RoundId = r.RoundId, DocumentId = closure.DocumentId, AuthorKingdomId = "a", TargetKingdomId = "p",
                    SilenceSourceDocumentId = source.DocumentId, SilenceConsequence = closure.SilenceConsequence };
                Call(o, "FinishSilenceConsequence", job, closure);
            }
            int writes = h.Writes, prestigeWrites = h.Standing.Writes;
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!); r = o.ResolveRound(r.RoundId);
            Call(o, "RecoverSettledSilence", r); Call(o, "RecoverSettledSilence", r);
            threat = o.CurrentStorage.DiplomaticThreats.Single();
            Test.True(threat.Status == (conflict ? "enforced" : "silence_concluded") && threat.TargetDecision == "pending"
                && threat.TargetDecisionDocumentId == "" && !threat.ReputationPenaltyApplied,
                "same-source silence closes a real threat without fabricating player consent/refusal or a prestige breach");
            Test.True(r.EventConvergence.ActualWar == conflict && h.Writes == writes && h.Standing.Writes == prestigeWrites,
                "reload repair reconciles silence from confirmed receipts without repeating game effects");
            var independent = new WorldDiplomacyThreat { ThreatId = "different-source", StageRoundId = r.RoundId,
                StageDocumentId = "another-source", IssuerKingdomId = "a", TargetKingdomId = "p" };
            o.CurrentStorage.DiplomaticThreats.Add(independent); Call(o, "RecoverSettledSilence", r);
            Test.True(independent.Status == "open", "same pair's independent source remains an actual obligation");
            o.CurrentStorage.DiplomaticThreats.Remove(independent);
            r.EventBeginningId = "boundary-started";
            o.CurrentStorage.EventAnnouncements.Add(new() { Id = r.EventBeginningId, EventId = r.RoundId, Stage = "started", State = "public" });
            Call(o, "AdvanceEventEnding", r);
            Test.True(r.EventPhase == "closing" && o.CurrentStorage.Jobs.Any(x => x.ForcedIntent == "ended"),
                "settled player silence reaches aggregate ending rather than queueing a nonexistent human decision");
            MaterialCountryThirdReplay.FinishStallBoundary(o, r);
        }
        foreach (string text in new[] { "北帝国宣战昭告", "Declaration of war", "We hereby declare war on you.", "现正式对西帝国宣战。" })
        {
            var (h, o, r) = Fixture("p"); var source = Doc(o, r, "request", "a", "p", "ultimatum");
            r.PlayerNegotiationWaits.Add(new() { SourceDocumentId = source.DocumentId, NotifiedHour = h.CurrentCampaignHours() - 72 });
            var job = new WorldDiplomacyJob { JobId = "false-war", RoundId = r.RoundId, Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "p",
                SilenceSourceDocumentId = source.DocumentId, SilenceConsequence = "ordinary", PrivacyApproved = true };
            o.EnqueueJob(job);
            var draft = new JObject { ["title"] = text.Contains("昭告") || text == "Declaration of war" ? text : "Closing correspondence",
                ["body"] = text, ["actions"] = new JArray() };
            object[] args = { job, draft.ToString() };
            Test.True(!(bool)Call(o, "BindSilenceConsequence", args) && job.PrivacyRewrites == 1 && o.CurrentStorage.Documents.Count == 1,
                "ordinary silence cannot publish an obvious false war even with an injected prior approval: " + text);
        }
        {
            var (h, o, r) = Fixture("p"); var source = Doc(o, r, "real-threat", "a", "p", "ultimatum");
            o.ProcessDiplomaticThreatDocument(source, "a", "p");
            var threat = o.CurrentStorage.DiplomaticThreats.Single();
            var refusal = Doc(o, r, "recorded-refusal", "p", "a", "statement");
            threat.TargetDecision = "noncomplied"; threat.TargetDecisionDocumentId = refusal.DocumentId;
            var closure = Doc(o, r, "ordinary-result", "a", "p", "statement");
            closure.SilenceSourceDocumentId = source.DocumentId; closure.SilenceConsequence = "ordinary"; closure.SilenceConsequenceSettled = true;
            r.PlayerNegotiationWaits.Add(new() { SourceDocumentId = source.DocumentId, ClosureDocumentId = closure.DocumentId, RequiresAnswer = false });
            Call(o, "RecoverSettledSilence", r);
            int prestige = o.CurrentStorage.NationalPrestigeByKingdom["a"], writes = h.Standing.Writes;
            Test.True(threat.Status == "breached" && threat.ReputationPenaltyApplied && threat.TargetDecisionDocumentId == refusal.DocumentId,
                "an existing actual refusal retains the original issuer prestige rule rather than being erased by silence repair");
            Call(o, "RecoverSettledSilence", r);
            Test.True(o.CurrentStorage.NationalPrestigeByKingdom["a"] == prestige && h.Standing.Writes == writes,
                "same-source prestige settlement is idempotent");
        }
        {
            var (_, o, r) = Fixture("p"); var source = Doc(o, r, "failed-war-source", "a", "p", "ultimatum");
            o.ProcessDiplomaticThreatDocument(source, "a", "p");
            var closure = Doc(o, r, "unconfirmed-war", "a", "p", "declare_war");
            closure.SilenceSourceDocumentId = source.DocumentId; closure.SilenceConsequence = "conflict"; closure.SilenceConsequenceSettled = true;
            closure.Actions = new() { new() { Intent = "declare_war", TargetKingdomId = "p", ChangedDiplomaticState = false } };
            r.PlayerNegotiationWaits.Add(new() { SourceDocumentId = source.DocumentId, ClosureDocumentId = closure.DocumentId, RequiresAnswer = false });
            Call(o, "RecoverSettledSilence", r);
            Test.True(o.CurrentStorage.DiplomaticThreats.Single().Status == "open" && !r.EventConvergence.ActualWar,
                "a saved settled flag without an actual war receipt cannot close the real obligation");
        }
        Console.WriteLine("Silence ending: exact source, actual war receipt, ordinary closure, reload/duplicate guards and CN/EN false-war rejection PASS; provider/game NOT_RUN.");
    }
}
