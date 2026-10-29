using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class EventCommitmentReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        internal bool Enabled = true;
        public override bool WorldDiplomacyEnabled() => Enabled;
    }
    private static object? Invoke(WorldDiplomacyOrchestration owner, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, args);
    private static (Host host, WorldDiplomacyOrchestration owner) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        return (h, o);
    }
    private static DialogueInteractionOrigin Origin(string channel = "native") =>
        new(channel, "promise_" + channel, "session", "Trade for fourteen days.", "I agree to trade for fourteen days.");

    internal static void Run()
    {
        OralTreatyExecutionReplay.Run();
        foreach (string channel in new[] { "native", "scene", "courier" })
        {
            var (h, o) = Fixture();
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=p;days=14", Origin(channel), out bool registered);
            var item = o.CurrentStorage.DialogueArrangements.Single();
            var round = o.ResolveRound(item.RoundId);
            string oral = o.BuildOralArrangementContext("ruler_a", "a");
            Test.True(oral.Contains(DiplomacyPromptTexts.Get("CommittedDeclaration_AllianceTerms", "")) && oral.Contains(DiplomacyPromptTexts.Get("Oral_AllianceBeforeAssent", "")), "real oral consumer contains alliance scope before assent: " + channel);
            Test.True(registered && item.Status == "accepted" && round.BirthLane == "oral", "independent oral promise retains oral birth lane: " + channel);
            Test.True(o.CurrentStorage.Documents.Count == 0 && h.TradeExecutions == 0, "consent alone does not execute or publish: " + channel);
            foreach (string state in new[] { "cancelled", "deferred" })
                o.ControlOralDiplomaticCommitment("ruler_a", "a", "arrangement=" + item.ArrangementId + ";state=" + state + ";reason=changed_mind");
            o.CancelUnpublishedCommitmentWork(item.ArrangementId, item.Version, "changed_mind");
            Test.True(item.Status == "accepted" && o.CurrentStorage.Jobs.Count == 1, "conversation and direct cancellation cannot revoke commitment");
            o.CloseRound("player_negotiation_finished", round);
            Test.True(round.State == "active", "case cannot finish while an accepted promise remains unfulfilled");
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=p;days=21", Origin(channel), out bool revised);
            Test.True(!revised && o.CurrentStorage.DialogueArrangements.Count == 1 && item.Terms.DurationDays == 14, "conflicting new terms never replace promise");
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=CancelTrade;move=NewMatter;target=p", Origin(channel), out bool opposite);
            Test.True(!opposite && o.CurrentStorage.DialogueArrangements.Count == 1, "opposite action is rejected before a second promise");
            for (int failure = 1; failure <= 3; failure++)
            {
                var job = o.CurrentStorage.Jobs.Single();
                job.Kind = "privacy_audit";
                Invoke(o, "SuspendDiplomacyJob", job, "test_transport_failure");
                Test.True(item.Status == "deferred" && item.GenerationRetryAfterDay < int.MaxValue && o.CurrentStorage.Jobs.Count == 0,
                    "privacy failure preserves commitment with finite retry deadline");
                h.CurrentDayValue = item.GenerationRetryAfterDay;
                Invoke(o, "MaintainDialogueArrangements");
            }
            Test.True(o.ResolveDocument(item.DocumentId)?.IsReadyForPublication == true && h.TradeExecutions == 1,
                "three drafting failures publish exact accepted terms through real executor: " + item.Status + ":" + item.Reason);
            Test.True(h.TradeDuration == 14 && !o.ResolveDocument(item.DocumentId).Body.Contains("PRIVATE_MEMORY"), "fallback preserves clauses without private memory");
            Invoke(o, "MaintainDialogueArrangements");
            Test.True(h.TradeExecutions == 1 && o.CurrentStorage.Documents.Count == 1, "repeated maintenance cannot execute twice");
        }
        OriginalEvent();
        PauseAndResume();
    }

    private static void OriginalEvent()
    {
        var (h, o) = Fixture();
        var round = o.EnsureActiveRound("a", "b", false);
        var source = new WorldDiplomacyDocument { DocumentId = "known", RoundId = round.RoundId, AuthorKingdomId = "b", IsReadyForPublication = true };
        o.CurrentStorage.Documents.Add(source); round.RootDocumentId = source.DocumentId;
        string payload = "action=Trade;move=NewMatter;target=b;days=14;event=" + round.RoundId;
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", payload, Origin(), out bool unknown);
        Test.True(unknown && o.CurrentStorage.DialogueArrangements.Single().RoundId != round.RoundId,
            "unreviewed event hint cannot block a valid promise or attach it to an unknown case");
        string independent = o.CurrentStorage.DialogueArrangements.Single().RoundId;
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "a", source.DocumentId, 12);
        Invoke(o, "InvalidateDialogueIndex");
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", payload, Origin(), out bool known);
        Test.True(known && o.CurrentStorage.DialogueArrangements.Single().RoundId == independent
            && round.BirthLane == "normal" && o.CurrentStorage.ConcurrentRounds.Count == 1,
            "knowledge alone is not event relevance; repeated source cannot create a second case");
        var agenda = (WorldDiplomacyRound)Invoke(o, "CreateIndependentDialogueRound", "c", "p", "npc_player_agenda")!;
        Test.True(agenda.BirthLane == "player", "NPC proactive player event uses player lane, not oral lane");
    }

    private static void PauseAndResume()
    {
        var (h, o) = Fixture();
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=p;days=14", Origin());
        var item = o.CurrentStorage.DialogueArrangements.Single(); var round = o.ResolveRound(item.RoundId);
        round.PlayerNegotiationWaits.Add(new() { SourceDocumentId = "awaiting", RequiresAnswer = true, NotifiedHour = h.CurrentCampaignHours() - 24 });
        var originalJob = o.CurrentStorage.Jobs.Single(); string oldId = originalJob.JobId;
        h.Enabled = false; o.HandleDisabledState(); int epoch = round.OwnershipEpoch;
        o.HandleDisabledState();
        Test.True(round.State == "active" && round.OwnershipEpoch == epoch && item.Status == "accepted", "disable is idempotent and retains case and promise");
        Test.True(originalJob.JobId != oldId && o.CurrentStorage.EventsPausedAtHour == 288, "disable invalidates old transport identity and persists pause time");
        h.CurrentDayValue += 20;
        round.PlayerNegotiationWaits.Add(new() { SourceDocumentId = "noticed_while_disabled", RequiresAnswer = true, NotifiedHour = h.CurrentCampaignHours() - 3 });
        Invoke(o, "MaintainDialogueArrangements");
        Test.True(h.TradeExecutions == 0 && o.CurrentStorage.Documents.Count == 0, "disabled maintenance cannot publish or execute");
        Test.True(!o.AdmitCompletion(originalJob, new LlmJobResult { Success = true }), "disabled completion cannot advance case");
        string serialized = JsonConvert.SerializeObject(o.CurrentStorage);
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(serialized)!;
        Test.True(restored.EventsPausedAtHour == 288 && restored.ConcurrentRounds.Single().BirthLane == "oral", "pause and immutable birth lane survive save roundtrip");
        h.Enabled = true; Invoke(o, "ResumeDiplomacyEvents");
        var wait = round.PlayerNegotiationWaits.First();
        Test.True(h.CurrentCampaignHours() - wait.NotifiedHour == 24 && !DiplomacyEventRules.SilenceDue(wait, h.CurrentCampaignHours()), "disabled twenty days consume none of reply deadline");
        Test.True(round.PlayerNegotiationWaits.Last().NotifiedHour == h.CurrentCampaignHours(), "notice received during pause begins at resume, never in the future");
        int resumedEpoch = round.OwnershipEpoch; Invoke(o, "ResumeDiplomacyEvents");
        Test.True(round.OwnershipEpoch == resumedEpoch && o.CurrentStorage.EventsPausedAtHour == -1, "resume is idempotent");
    }
}
