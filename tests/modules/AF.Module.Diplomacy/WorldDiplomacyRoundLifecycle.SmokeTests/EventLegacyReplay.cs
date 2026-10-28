using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class EventLegacyReplay
{
    private static object? Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args);
    private static (ConcurrentOralMigrationReplay.Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new ConcurrentOralMigrationReplay.Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; return (h, o);
    }
    private static WorldDiplomacyRound OldRound(string id) => new() { RoundId = id, InitiatorKingdomId = "a", State = "active",
        Participants = new() { new() { KingdomId = "a", State = "active" }, new() { KingdomId = "p", State = "active" } } };
    internal static void Run()
    {
        var (h, o) = Fixture();
        var unknown = OldRound("unknown");
        var stale = new WorldDiplomacyJob { JobId = "stale", Kind = "generate", RoundId = "unknown", AuthorKingdomId = "a", UserPrompt = "obsolete war order", PrivacyDraft = "private draft", IsRunning = true };
        var storage = new WorldDiplomacyStorage { ActiveRound = unknown, Jobs = new() { stale } };
        o.ReplaceStorage(storage);
        Test.True(storage.ActiveRound == null && storage.CompletedRounds.Single() == unknown && unknown.State == "legacy_preserved", "unverified old work releases active slot without inventing a political outcome");
        Test.True(storage.Jobs.Count == 0 && storage.LegacyPreservedJobs.Single().PrivacyDraft == "private draft" && h.Effects == 0, "old drafts remain evidence and never execute");
        var publicRound = OldRound("public"); publicRound.RootDocumentId = "published";
        var document = new WorldDiplomacyDocument { DocumentId = "published", RoundId = "public", AuthorKingdomId = "a", TargetKingdomId = "p",
            IsReadyForPublication = true, Body = "A previously published declaration", ChangedDiplomaticState = true, MechanicalResult = "prior actual action", Day = 5 };
        publicRound.PlayerNegotiationWaits.Add(new() { SourceDocumentId = "published", NotifiedHour = 2, RequiresAnswer = true });
        storage = new WorldDiplomacyStorage { ActiveRound = publicRound, Documents = new() { document }, Jobs = new() {
            new() { JobId = "old_analysis", RoundId = "public", Kind = "analyze", DocumentId = "published", UserPrompt = "obsolete execution" } } };
        o.ReplaceStorage(storage);
        Test.True(publicRound.State == "active" && publicRound.EventOpeningOrigin == "legacy_public_event", "real published event retains original identity");
        Test.True(storage.Jobs.Single().Kind == "player_lifecycle" && storage.LegacyPreservedJobs.Single().JobId == "old_analysis", "only lifecycle classification replaces obsolete action analysis");
        Test.True((bool)Invoke(o, "EventBoundaryComplete", publicRound, "started")! && storage.EventAnnouncements.Count == 0, "legacy public event never fabricates retroactive beginning or effects");
        Test.True(publicRound.PlayerNegotiationWaits.Single().NotifiedHour == -1 && document.ActualPlayerNoticeHour == -1, "legacy wait needs a fresh actual notice and cannot charge old silence");
        var job = storage.Jobs.Single();
        Invoke(o, "CompletePlayerLifecycle", job, JObject.Parse("{\"requires_response\":false,\"round_participation\":\"withdraw\",\"round_status\":\"resolved\"}"));
        Test.True(document.ChangedDiplomaticState && document.Body == "A previously published declaration" && h.Effects == 0 && h.TradeExecutions == 0, "classification preserves actual old result and prose without re-execution");
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        o.ReplaceStorage(saved);
        Test.True(saved.LegacyPreservedJobs.Count == 1 && saved.Jobs.Count == 0 && saved.ActiveRound.EventSystemVersion == 1, "migration is idempotent after save/load");
        var (ah, ao) = Fixture();
        ao.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=p;days=14",
            new DialogueInteractionOrigin("native", "accepted", "session", "Trade fourteen days", "Agreed"));
        var item = ao.CurrentStorage.DialogueArrangements.Single(); var round = ao.ResolveRound(item.RoundId); round.EventSystemVersion = 0;
        item.DocumentId = "stale_draft";
        ao.CurrentStorage.Documents.Add(new() { DocumentId = "stale_draft", RoundId = round.RoundId, AuthorKingdomId = "a", Body = "unverified different terms", EventExecutionPending = true });
        var acceptedStorage = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(ao.CurrentStorage))!;
        ao.ReplaceStorage(acceptedStorage); item = acceptedStorage.DialogueArrangements.Single();
        Test.True(item.Status == "accepted" && item.DocumentId == "" && item.Terms.DurationDays == 14 && acceptedStorage.LegacyPreservedJobs.Count == 1, "valid obligation keeps exact terms while retiring unverified old draft");
        Test.True(acceptedStorage.Documents.Single().Body == "unverified different terms" && !acceptedStorage.Documents.Single().EventExecutionPending, "quarantined old prose is retained but cannot be committed later");
        item.GenerationFailureAttempts = 3; Invoke(ao, "MaintainDialogueArrangements");
        Test.True(ah.TradeExecutions == 1 && ah.TradeDuration == 14 && item.Status == "executed", "recovered promise executes once from original accepted terms");
        Invoke(ao, "MaintainDialogueArrangements");
        Test.True(ah.TradeExecutions == 1, "legacy recovery does not duplicate obligation");
    }
}
