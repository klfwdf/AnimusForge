using AnimusForge;

internal static class EventMemoryReplay
{
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        var known = new HashSet<string>();
        var view = new WorldDiplomacyMemorySnapshot(storage, "outsider", known, "outsider_ruler");
        storage.EventAnnouncements.Add(new() { Id = "case:started", EventId = "case", Stage = "started", State = "public", Title = "The broken seal",
            Body = "An envoy alleges that forged evidence was presented at the banquet. 使节指控有人在宴会上伪造证据。", Day = 3,
            Narrative = new() { new() { Kind = "allegation", Source = "Envoy's public statement", Text = "Evidence was forged, according to the envoy.", Public = true },
                new() { Kind = "public_fact", Source = "private", Text = "PRIVATE_HIDDEN", Public = false } } });
        storage.EventAnnouncements.Add(new() { Id = "unrelated:ended", EventId = "unrelated", State = "public", Title = "Latest harvest", Body = "UNRELATED_LATEST wheat harvest", Day = 999 });
        var document = new WorldDiplomacyDocument { DocumentId = "reply", RoundId = "case", Title = "Court reply", Body = "ENVOY_REPLY: the envoy says we reject the allegation of forged evidence.", IsReadyForPublication = true };
        storage.Documents.Add(document);
        string Search(string text, int budget = 7000) => WorldDiplomacyMemoryApplication.Build(view, text, d => d.ToString(), budget);
        string found = Search("What happened to the envoy?");
        Test.True(found.Contains("The broken seal") && found.Contains("allegation"), "event body topics find public narrative without exact title");
        Test.True(!found.Contains("ENVOY_REPLY") && !found.Contains("PRIVATE_HIDDEN"), "worldwide opening does not reveal undelivered replies or hidden facts");
        Test.True(!found.Contains("UNRELATED_LATEST"), "no mandatory latest event filler");
        Test.True(Search("战马价格如何") == "", "unrelated input injects no diplomatic memory");
        Test.True(Search("使节怎么了").Contains("The broken seal"), "Chinese body keywords find event without title");
        known.Add("reply");
        Test.True(Search("envoy").Contains("ENVOY_REPLY"), "actual delivery changes visibility without duplicating history");
        storage.RoundSummaries.Add(new() { RoundId = "old", Facts = new() { new() { Kind = "allegation", Text = "envoy MIXED_PRIVATE_FACT", SourceDocumentIds = new() { "reply", "secret" } } } });
        Test.True(!Search("envoy").Contains("MIXED_PRIVATE_FACT"), "one known source cannot reveal a mixed-visibility summary fact");
        known.Add("secret"); Test.True(Search("envoy").Contains("MIXED_PRIVATE_FACT"), "fully known summary fact is retrievable");
        var round = new WorldDiplomacyRound { RoundId = "case", State = "active", PendingOffers = new() { new() {
            SourceDocumentId = "reply", SourceActionId = "", Status = "open", ProposerKingdomId = "a", TargetKingdomId = "outsider", Intent = "propose_trade" } } };
        storage.ActiveRound = round;
        document.TreatyTerms = new() { DurationDays = 17 };
        WorldDiplomacyMemoryApplication.Invalidate(storage);
        Test.True(Search("envoy").Contains("17") && Search("envoy").Contains("current_offer"), "selected event reads exact live proposal from authoritative owner");
        document.TreatyTerms.DurationDays = 21;
        Test.True(Search("envoy").Contains("21"), "current clauses are read live instead of copied into stale memory");
        storage.DialogueArrangements.Add(new() { ArrangementId = "private_promise", RoundId = "case", Status = "accepted", RulerId = "original_ruler", ActorKingdomId = "outsider" });
        Test.True(!Search("envoy").Contains("private_promise"), "new ruler does not inherit a predecessor's unpublished private promise");
        view = new WorldDiplomacyMemorySnapshot(storage, "outsider", known, "original_ruler");
        Test.True(Search("envoy").Contains("private_promise"), "original ruler retrieves their own binding private promise");
        Test.True(Search("envoy", 700).Length <= 700, "whole evidence rows respect bounded recall rather than loading all history");
        Test.True(Search("") == "", "empty query does not force latest events");

        storage.AidRequests.Add(new() { Id = "aid_known", RoundId = "closed_case", CallingKingdomId = "outsider", CalledKingdomId = "ally",
            EnemyKingdomId = "enemy", Status = "accepted", AcceptedRulerId = "ally_ruler", RequestDocumentId = "unreceived" });
        Test.True(Search("ally").Contains("aid_known"), "calling realm recalls still binding aid after event closes");
        storage.AidRequests[0].Status = "fulfilled";
        Test.True(Search("ally").Contains("fulfilled"), "aid truth comes from current owner, not stale search cache");
        view = new WorldDiplomacyMemorySnapshot(storage, "unrelated", new HashSet<string>(), "unrelated_ruler");
        Test.True(!Search("ally").Contains("aid_known"), "aid involvement is not worldwide disclosure");
        view = new WorldDiplomacyMemorySnapshot(storage, "ally", new HashSet<string>(), "ally_ruler");
        Test.True(Search("ally").Contains("aid_known"), "actual accepting ruler knows their own aid commitment");
        int embeds = 0; var embedded = new List<string>();
        float[] Embed(string value)
        {
            embeds++; embedded.Add(value);
            return value.Contains("envoy") || value == "diplomatic emissary" ? new float[] { 1, 0 } : new float[] { 0, 1 };
        }
        view = new WorldDiplomacyMemorySnapshot(storage, "unrelated", new HashSet<string>(), "unrelated_ruler");
        string semantic = WorldDiplomacyMemoryApplication.Build(view, "diplomatic emissary", d => d.ToString(), 7000, Embed);
        Test.True(semantic.Contains("The broken seal"), "semantic match recalls an event without lexical title or body overlap");
        Test.True(!semantic.Contains("ENVOY_REPLY") && !semantic.Contains("aid_known"), "semantic matching cannot bypass source visibility");
        Test.True(!embedded.Any(x => x.Contains("ENVOY_REPLY") || x.Contains("aid_known")), "hidden content is excluded before embedding");
        int initial = embeds;
        WorldDiplomacyMemoryApplication.Build(view, "diplomatic emissary", d => d.ToString(), 7000, Embed);
        Test.True(embeds == initial + 1, "unchanged source vectors reused; only query is embedded again");
        Test.True(WorldDiplomacyMemoryApplication.Build(view, "envoy", d => d.ToString(), 7000, _ => throw new Exception("offline")).Contains("The broken seal"),
            "local model failure preserves lexical recall");
        var big = new WorldDiplomacyStorage();
        for (int i = 0; i < 80; i++) big.EventAnnouncements.Add(new() { Id = "b" + i, State = "public", Body = "independent source " + i });
        embeds = 0;
        WorldDiplomacyMemoryApplication.Build(new(big, "", new()), "diplomatic emissary", d => d.ToString(), 7000, Embed);
        Test.True(embeds == 25, "semantic cache warmup has fixed per-request inference budget plus query");
    }
}
