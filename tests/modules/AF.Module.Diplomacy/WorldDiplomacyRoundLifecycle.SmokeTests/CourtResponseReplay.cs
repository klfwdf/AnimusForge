using AnimusForge;
using Newtonsoft.Json;

internal static class CourtResponseReplay
{
    internal static void Run()
    {
        var (host, owner) = ConcurrentOralMigrationReplay.Fixture();
        host.PublishEnabled = true;
        var round = owner.EnsureActiveRound("p", "a", true);
        var observer = round.Participants.Single(x => x.KingdomId == "a");
        observer.State = "observer";
        var document = new WorldDiplomacyDocument { DocumentId = "player-source", RoundId = round.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "a", IsPlayerAuthored = true, IsReadyForPublication = true,
            AnalysisStatus = "success", RequiresResponse = true, Body = "Please join the public dispute.",
            ResponseRequestsClassified = true, ResponseRequests = new() { new() { KingdomId = "a", Kind = "invitation", Quote = "Please join the public dispute." } },
            AddressedKingdomIds = new() { "a" } };
        owner.AddDocument(document); round.RootDocumentId = document.DocumentId;
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(host.Publication(), owner, document);
        Test.True(owner.CurrentStorage.Jobs.Count == 0 && observer.State == "observer",
            "publication reconciliation cannot invent court knowledge or participation before arrival");
        WorldDiplomacyPropagationApplication.ReceiveCourt(owner.CurrentStorage, document, "a", host.CurrentDayValue, () => false, () => { });
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(host.Publication(), owner, document);
        Test.True(observer.State == "observer" && !owner.CurrentStorage.Jobs.Any(x => x.Kind == "generate")
            && owner.CurrentStorage.Jobs.Any(x => x.Kind == "player_decision" && x.ForcedIntent == "answer_invitation"),
            "actual compensation entry retains an observer and asks for informed voluntary participation");
        Test.True(round.PlayerResponses.Single().SourceDocumentId == document.DocumentId,
            "shared receiver preserves the exact pending source without a legacy response owner");
        int jobs = owner.CurrentStorage.Jobs.Count;
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(host.Publication(), owner, document);
        Test.True(owner.CurrentStorage.Jobs.Count == jobs && round.PlayerResponses.Count == 1,
            "repeated compensation coalesces one obligation and one decision");
        var copy = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(owner.CurrentStorage))!;
        var savedRound = copy.ActiveRound ?? copy.ConcurrentRounds.Single();
        Test.True(savedRound.Participants.Single(x => x.KingdomId == "a").State == "observer"
            && savedRound.PlayerResponses.Single().SourceDocumentId == document.DocumentId,
            "participation and source obligation survive JSON roundtrip together");
        var historical = new WorldDiplomacyDocument { DocumentId = "history", RoundId = "unknown-legacy", AuthorKingdomId = "a",
            TargetKingdomId = "p", IsReadyForPublication = true };
        owner.AddDocument(historical); owner.ProcessCourtArrival("p", historical);
        Test.True(historical.HasReachedPlayerCourt && owner.CurrentStorage.Jobs.Count == jobs,
            "historical player delivery remains readable without creating an AI response or event");
        var before = round.Participants.Count;
        owner.ProcessCourtArrival("b", historical);
        Test.True(round.Participants.Count == before && owner.CurrentStorage.Jobs.Count == jobs,
            "historical or unknown event cannot use a fallback to promote an unrelated country");
    }
}
