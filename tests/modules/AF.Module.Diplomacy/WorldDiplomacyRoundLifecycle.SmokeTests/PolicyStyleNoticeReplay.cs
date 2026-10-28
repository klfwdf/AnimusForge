using AnimusForge;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;

internal static class PolicyStyleNoticeReplay
{
    private sealed class Sink : IWorldDiplomacyNotificationSink, IWorldDiplomacyPlayerLocationSink
    {
        public bool MapNotificationsEnabled { get; set; } = true;
        public bool Registered = true;
        public bool Throw;
        public int Registrations;
        public double CurrentHour { get; set; } = 300;
        public string PlayerKingdomId => "player";
        public string PlayerSettlementId { get; set; } = "";
        public int CurrentDay => 10;
        public readonly List<WorldDiplomacyNotice> Notices = new();
        public readonly List<string> Rumors = new();
        public readonly List<(string Id, double Hour)> Shown = new();
        public Action? BeforeNotice;
        public bool CanPublishMapNotification() => true;
        public bool EnsureMapNotificationRegistered() { Registrations++; return Registered; }
        public string KingdomName(string id) => "Name of " + id;
        public string FormatCampaignDate(int day) => "MAP_DATE";
        public void RecordShownTime(WorldDiplomacyDocument document, double hour) => Shown.Add((document.DocumentId, hour));
        public void ShowRumor(string text) => Rumors.Add(text);
        public void Log(string text) { }
        public void ShowNotice(WorldDiplomacyNotice notice)
        {
            BeforeNotice?.Invoke();
            if (Throw) throw new InvalidOperationException("display unavailable");
            Notices.Add(notice);
        }
    }

    private static WorldDiplomacyDocument Document(string id, string target = "foreign") => new()
    {
        DocumentId = id, AuthorKingdomId = "author", AuthorKingdomName = "Realm {TITLE}",
        TargetKingdomId = target, TargetKingdomName = target, AddressedKingdomIds = new() { target },
        Title = "Treaty {BODY}", Body = "PUBLIC_START\nSpecific condition: 100 gold; no access until paid. " + new string('x', 7000) + "\nPUBLIC_END",
        Day = 10, IsReadyForPublication = true, HasReachedPlayerCourt = true, ActualPlayerNoticeHour = -1
    };

    internal static void Run()
    {
        var now = new DateTime(2026, 1, 1);
        foreach (bool enabled in new[] { true, false })
        {
            var foreign = Document("foreign");
            foreign.IsRead = true;
            var related = Document("related", "player"); related.IsRead = true;
            var authored = Document("written", "foreign"); authored.IsPlayerAuthored = true;
            var notArrived = Document("unreceived"); notArrived.HasReachedPlayerCourt = false;
            var pending = Document("unpublished"); pending.IsReadyForPublication = false;
            foreign.RoundId = "event_1";
            var storage = new WorldDiplomacyStorage { ActiveRound = new() { RoundId = "event_1", RoundTopic = "The disputed crown", State = "active" } }; storage.Documents.AddRange(new[] { foreign, related, authored, notArrived, pending });
            var originalBody = foreign.Body;
            var owner = new WorldDiplomacyNotificationApplication(); var sink = new Sink { MapNotificationsEnabled = enabled };
            owner.Poll(storage, now, sink);
            if (!enabled)
            {
                Test.True(sink.Notices.Count == 0 && related.ActualPlayerNoticeHour < 0 && !foreign.FormalNoticeShown,
                    "disabled display preserves pending notices and never starts reply timers");
                sink.MapNotificationsEnabled = true;
                owner.Poll(storage, now.AddSeconds(1), sink);
            }
            Test.True(sink.Notices.Count == 2 && sink.Notices.All(n => n.DocumentId != "written"), "player-authored/unpublished/unreceived documents never announce");
            var text = sink.Notices.Single(n => n.DocumentId == "foreign");
            Test.True(!text.ShowOnMap && text.Description.EndsWith(originalBody), "foreign text notification contains full published body, including its tail");
            string expected = SmokeLocalizationEnvironment.EnglishUi
                ? "【Diplomatic Event: The disputed crown】" + originalBody
                : "【外交事件：The disputed crown】" + originalBody;
            Test.True(text.Description == expected, "real UI resource and literal author/title/body placeholders compose correctly");
            Test.True(!text.Description.Contains("MAP_DATE") && !text.Description.Contains("Click to") && !text.Description.Contains("点击查看全文"), "text is readable declaration, without map metadata or click instruction");
            var playerNotice = sink.Notices.Single(n => n.DocumentId == "related");
            Test.True(!playerNotice.ShowOnMap && sink.Registrations == 0, "all delivered declarations use bottom-left text, including player-related pre-read documents");
            Test.True(playerNotice.Description.EndsWith(related.Body), "player-related declaration also includes the complete published body");
            Test.True(related.ActualPlayerNoticeHour == 300 && related.FormalNoticeShown && sink.Shown.Count == 2, "notification timestamps recorded only after actual delivery in either route");
            Test.True(!authored.FormalNoticeShown && authored.ActualPlayerNoticeHour == -1 && !notArrived.FormalNoticeShown && !pending.FormalNoticeShown, "excluded publications are never marked shown or timed");
            Test.True(storage.Documents.Count == 5 && foreign.Body == originalBody && sink.Rumors.Count == 0, "public archive data and suppressed rumor-display behavior remain untouched");
            sink.MapNotificationsEnabled = !enabled;
            owner.ResetView(); owner.Poll(storage, now.AddSeconds(1), sink);
            Test.True(sink.Notices.Count == 2, "toggling route and rebuilding cannot duplicate a shown declaration");
            var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
            var afterLoad = new Sink(); new WorldDiplomacyNotificationApplication().Poll(restored, now, afterLoad);
            Test.True(afterLoad.Notices.Count == 0 && restored.Documents[0].Body == originalBody, "persisted shown flags prevent replay while full bulletin body survives reload");
        }

        var retryStorage = new WorldDiplomacyStorage(); var retry = Document("retry", "player"); retryStorage.Documents.Add(retry);
        var retryOwner = new WorldDiplomacyNotificationApplication(); var retrySink = new Sink { MapNotificationsEnabled = true, Throw = true };
        retryOwner.Poll(retryStorage, now, retrySink);
        Test.True(retry.ActualPlayerNoticeHour == -1 && !retry.FormalNoticeShown && retrySink.Shown.Count == 0, "failed text display never starts response clock");
        retrySink.Throw = false; retrySink.CurrentHour = 310;
        retryOwner.Poll(retryStorage, now.AddSeconds(1), retrySink);
        Test.True(retry.ActualPlayerNoticeHour == 310 && retrySink.Notices.Count == 1, "successful text retry starts at actual later display time");

        var registrationStorage = new WorldDiplomacyStorage(); var map = Document("map", "player"); var third = Document("third");
        registrationStorage.Documents.AddRange(new[] { map, third });
        var registrationOwner = new WorldDiplomacyNotificationApplication(); var registrationSink = new Sink { Registered = false };
        registrationOwner.Poll(registrationStorage, now, registrationSink);
        Test.True(map.FormalNoticeShown && registrationSink.Notices.Count == 2 && registrationSink.Registrations == 0, "right widget registration is irrelevant to delivery of both declarations");
        registrationSink.Registered = true; registrationOwner.Poll(registrationStorage, now.AddSeconds(1), registrationSink);
        Test.True(!registrationSink.Notices.Single(n => n.DocumentId == "map").ShowOnMap && registrationSink.Notices.Count == 2, "no duplicate notice after another poll");

        var civilianStorage = new WorldDiplomacyStorage(); var local = Document("civilian"); local.HasReachedPlayerCourt = false;
        civilianStorage.Documents.Add(local);
        var civilianOwner = new WorldDiplomacyNotificationApplication(); var civilianSink = new Sink { PlayerSettlementId = "town_a" };
        civilianOwner.Poll(civilianStorage, now, civilianSink);
        Test.True(civilianSink.Notices.Count == 0, "no civilian announcement before local propagation arrives");
        civilianStorage.SettlementKnowledge.Add(new() { SettlementId = "town_a", DocumentIds = new() { local.DocumentId } });
        civilianOwner.Poll(civilianStorage, now.AddSeconds(1), civilianSink);
        Test.True(civilianSink.Notices.Count == 1 && !local.HasReachedPlayerCourt, "local civilian delivery shows text without fabricating a court receipt");

        var queuedStorage = new WorldDiplomacyStorage(); var first = Document("first"); var reclassified = Document("reclassified");
        queuedStorage.Documents.AddRange(new[] { first, reclassified });
        var queuedSink = new Sink(); queuedSink.BeforeNotice = () => reclassified.IsPlayerAuthored = true;
        new WorldDiplomacyNotificationApplication().Poll(queuedStorage, now, queuedSink);
        Test.True(queuedSink.Notices.Count == 1 && !reclassified.FormalNoticeShown, "late player-authored reclassification cannot slip through a cached notification queue");
        Console.WriteLine("Policy-style notices: real production routing/localization and synthetic display sink; live game NOT_RUN.");
    }
}
