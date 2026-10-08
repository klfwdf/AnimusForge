using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class PropagationLifecycleReplay
{
    internal static void Run()
    {
        var rumorStorage = new WorldDiplomacyStorage();
        rumorStorage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "late", Day = 5, CreatedUtcTicks = 2, IsReadyForPublication = true });
        rumorStorage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "early", Day = 4, CreatedUtcTicks = 3, IsReadyForPublication = true });
        rumorStorage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "tie", Day = 4, CreatedUtcTicks = 3, IsReadyForPublication = true });
        rumorStorage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "player", Day = 1, IsReadyForPublication = true, IsPlayerAuthored = true });
        rumorStorage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "draft", Day = 1 });
        var rumors = WorldDiplomacyPropagationApplication.SelectPendingRumors(rumorStorage, 2);
        Test.True(rumors.Select(x => x.DocumentId).SequenceEqual(new[] { "early", "tie" })
            && !rumors[0].RumorNotified,
            "rumor selection preserves chronological ties, batch limit, and per-display acknowledgement");
        WorldDiplomacyPropagationApplication.MarkRumorNotified(rumors[0]);
        Test.True(rumors[0].RumorNotified && !rumors[1].RumorNotified
            && WorldDiplomacyPropagationApplication.SelectPendingRumors(rumorStorage, 2)
                .Select(x => x.DocumentId).SequenceEqual(new[] { "tie", "late" }),
            "failed later display remains pending while acknowledged rumor is not selected again");

        var storage = new WorldDiplomacyStorage();
        var round = new WorldDiplomacyRound { RoundId = "r" };
        var document = new WorldDiplomacyDocument
        {
            DocumentId = "d", AuthorKingdomId = "author", TargetKingdomId = "target",
            IsResponse = true, SourceDocumentId = "source"
        };
        var trace = new List<string>();
        WorldDiplomacyPropagationApplication.BeginPublication(
            storage, document, "author",
            id => { trace.Add("resolve:" + id); return null; },
            () => { trace.Add("ensure"); return round; },
            () => { trace.Add("origin"); return "capital"; },
            () => { trace.Add("affiliation"); return true; },
            () => { trace.Add("player-kingdom"); return false; },
            () => 12, () => 4,
            _ => trace.Add("weekly"));
        Test.True(trace.SequenceEqual(new[]
            { "resolve:", "ensure", "origin", "affiliation", "player-kingdom", "weekly" }),
            "publication preserves round, origin, player receipt and weekly-material effect order");
        Test.True(document.RoundId == "r" && document.ExchangeId == "r"
                && document.OriginSettlementId == "capital" && document.PropagationStarted
                && document.IsReadyForPublication && document.HasReachedPlayerCourt,
            "publication binds the canonical round and origin before formal author knowledge");
        Test.True(round.RootDocumentId == "d" && round.LastActivityDay == 12
                && round.Participants.Count == 1 && round.Participants[0].SelectedForRelay
                && round.Participants[0].LastSpokeDay == 12
                && !round.Participants[0].MandatoryReplyPending
                && round.Participants[0].LastTriggeredDocumentId == "source",
            "publication preserves author relay participant and response-obligation settlement");
        Test.True(storage.SettlementKnowledge.Single().DocumentIds.Single() == "d"
                && storage.KingdomKnowledge.Single().DocumentIds.Single() == "d"
                && storage.NobleKnowledge.Single().DocumentIds.Single() == "d",
            "author court, kingdom and noble knowledge are written once to canonical storage");
        string saved = JsonConvert.SerializeObject(storage);
        Test.True(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(saved)) == saved,
            "publication knowledge survives a real JSON round trip");

        var external = new WorldDiplomacyDocument { DocumentId = "external", AnalysisStatus = "external_fact" };
        int ensured = 0;
        WorldDiplomacyPropagationApplication.BeginPublication(
            storage, external, "author", _ => null,
            () => { ensured++; return round; }, () => null,
            () => false, () => false, () => 13, () => 4, _ => { });
        Test.True(ensured == 0 && external.RoundId == "" && external.OriginSettlementId == ""
                && external.PropagationStarted && external.IsReadyForPublication,
            "external facts publish without manufacturing a new round");

        var ownRelay = new WorldDiplomacyDocument { DocumentId = "own", AuthorKingdomId = "player" };
        int courtEffects = 0;
        WorldDiplomacyPropagationApplication.ReceivePlayerRelay(
            "player", ownRelay, () => true, () => courtEffects++, () => 14, _ => { });
        Test.True(ownRelay.HasReachedPlayerCourt && courtEffects == 0,
            "relay to the author's own player court records the formal flag without a second court effect");
        var foreignRelay = new WorldDiplomacyDocument { DocumentId = "foreign", AuthorKingdomId = "npc" };
        WorldDiplomacyPropagationApplication.ReceivePlayerRelay(
            "player", foreignRelay, () => true,
            () => { courtEffects++; foreignRelay.HasReachedPlayerCourt = true; }, () => 14, _ => { });
        Test.True(foreignRelay.HasReachedPlayerCourt && courtEffects == 1,
            "foreign relay reaches the existing formal court effect exactly once");

        storage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "recover", IsReadyForPublication = true });
        storage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "read", IsReadyForPublication = true, FormalNoticeShown = true });
        storage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "player-owned", IsReadyForPublication = true, IsPlayerAuthored = true });
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "player", "recover", 2);
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "player", "read", 2);
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "player", "player-owned", 2);
        var recoveryLogs = new List<string>();
        WorldDiplomacyPropagationApplication.RecoverPlayerCourtReceipts(storage, "player", recoveryLogs.Add);
        WorldDiplomacyPropagationApplication.RecoverPlayerCourtReceipts(storage, "player", recoveryLogs.Add);
        Test.True(storage.Documents[0].HasReachedPlayerCourt && !storage.Documents[1].HasReachedPlayerCourt
                && !storage.Documents[2].HasReachedPlayerCourt && recoveryLogs.Count == 1,
            "old-save recovery repairs only missing formal receipts and is idempotent");

        var retryStorage = new WorldDiplomacyStorage();
        for (int day = 1; day <= 10; day++)
            retryStorage.Documents.Add(new WorldDiplomacyDocument
                { DocumentId = "retry-" + day, AuthorKingdomId = "author", Day = day, IsReadyForPublication = true });
        var retried = new List<string>();
        var failures = new List<string>();
        WorldDiplomacyPropagationApplication.RetryDeferred(
            retryStorage, _ => true,
            d => { retried.Add(d.DocumentId); if (d.DocumentId == "retry-3") throw new InvalidOperationException("transient"); },
            failures.Add);
        Test.True(retried.SequenceEqual(Enumerable.Range(1, 8).Select(x => "retry-" + x))
                && failures.Count == 1 && failures[0].Contains("retry-3", StringComparison.Ordinal),
            "daily deferred retry keeps the chronological eight-document cap and isolates one failed publication");

        var vanishedStorage = new WorldDiplomacyStorage();
        for (int i = 0; i < 8; i++) vanishedStorage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "gone-" + i, AuthorKingdomId = "gone", Day = i, IsReadyForPublication = true, Body = "retained archive" });
        var nextLive = new WorldDiplomacyDocument { DocumentId = "live", AuthorKingdomId = "live", Day = 9, IsReadyForPublication = true };
        vanishedStorage.Documents.Add(nextLive);
        int liveAttempts = 0;
        WorldDiplomacyPropagationApplication.RetryDeferred(vanishedStorage, id => id == "live", _ => liveAttempts++, _ => { });
        Test.True(liveAttempts == 0 && vanishedStorage.Documents.Take(8).All(x => x.PropagationCompleted && x.Body == "retained archive"),
            "vanished authors retire only the capped eight propagation tickets, retaining published archive");
        WorldDiplomacyPropagationApplication.RetryDeferred(vanishedStorage, id => id == "live", _ => liveAttempts++, _ => { });
        Test.True(liveAttempts == 1, "next daily batch reaches valid document behind vanished authors");
        WorldDiplomacyPropagationApplication.RetryDeferred(vanishedStorage, _ => throw new InvalidOperationException("resolver transient"), _ => liveAttempts++, _ => { });
        Test.True(!nextLive.PropagationCompleted && liveAttempts == 1, "resolver exception keeps live propagation retryable rather than claiming author disappeared");

        DirectoryInfo root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AnimusForge.csproj"))) root = root.Parent;
        Test.True(root != null, "repository located for propagation lifecycle boundary");
        string host = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        Test.True(host.Contains("WorldDiplomacyPublicationRoutingApplication.Start(", StringComparison.Ordinal)
                && !host.Contains("PublishImmediatePublicKnowledge(", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyPropagationApplication.ReceivePlayerRelay(", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyPropagationApplication.RecoverPlayerCourtReceipts(", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyPropagationApplication.RetryDeferred(", StringComparison.Ordinal),
            "active publication, relay, recovery and retry paths all enter the application owner");
    }
}
