using AnimusForge;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;

internal static class PolicyStyleNoticeReplay
{
    private sealed class Sink : IWorldDiplomacyNotificationSink
    {
        public bool MapNotificationsEnabled { get; set; } = true;
        public bool Registered = true;
        public bool Throw;
        public int Registrations;
        public double CurrentHour { get; set; } = 300;
        public string PlayerKingdomId => "player";
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
            var related = Document("related", "player"); related.IsRead = true;
            var authored = Document("written", "foreign"); authored.IsPlayerAuthored = true;
            var notArrived = Document("unreceived"); notArrived.HasReachedPlayerCourt = false;
            var pending = Document("unpublished"); pending.IsReadyForPublication = false;
            var storage = new WorldDiplomacyStorage(); storage.Documents.AddRange(new[] { foreign, related, authored, notArrived, pending });
            var originalBody = foreign.Body;
            var owner = new WorldDiplomacyNotificationApplication(); var sink = new Sink { MapNotificationsEnabled = enabled };
            owner.Poll(storage, now, sink);
            Test.True(sink.Notices.Count == 2 && sink.Notices.All(n => n.DocumentId != "written"), "player-authored/unpublished/unreceived documents never announce");
            var text = sink.Notices.Single(n => n.DocumentId == "foreign");
            Test.True(!text.ShowOnMap && text.Description.EndsWith(originalBody), "foreign text notification contains full published body, including its tail");
            string expected = SmokeLocalizationEnvironment.EnglishUi
                ? "[Diplomatic Declaration] Realm {TITLE} issued \"Treaty {BODY}\": " + originalBody
                : "【外交宣言】Realm {TITLE}发布《Treaty {BODY}》：" + originalBody;
            Test.True(text.Description == expected, "real UI resource and literal author/title/body placeholders compose correctly");
            Test.True(!text.Description.Contains("MAP_DATE") && !text.Description.Contains("Click to") && !text.Description.Contains("点击查看全文"), "text is readable declaration, without map metadata or click instruction");
            var playerNotice = sink.Notices.Single(n => n.DocumentId == "related");
            Test.True(playerNotice.ShowOnMap == enabled, "player-related document takes exactly the enabled route, even when pre-read");
            Test.True(enabled ? !playerNotice.Description.Contains("PUBLIC_START") : playerNotice.Description.EndsWith(related.Body), "right widget keeps its existing compact description; disabled widget gets full text");
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
        var retryOwner = new WorldDiplomacyNotificationApplication(); var retrySink = new Sink { MapNotificationsEnabled = false, Throw = true };
        retryOwner.Poll(retryStorage, now, retrySink);
        Test.True(retry.ActualPlayerNoticeHour == -1 && !retry.FormalNoticeShown && retrySink.Shown.Count == 0, "failed text display never starts response clock");
        retrySink.Throw = false; retrySink.CurrentHour = 310;
        retryOwner.Poll(retryStorage, now.AddSeconds(1), retrySink);
        Test.True(retry.ActualPlayerNoticeHour == 310 && retrySink.Notices.Count == 1, "successful text retry starts at actual later display time");

        var registrationStorage = new WorldDiplomacyStorage(); var map = Document("map", "player"); var third = Document("third");
        registrationStorage.Documents.AddRange(new[] { map, third });
        var registrationOwner = new WorldDiplomacyNotificationApplication(); var registrationSink = new Sink { Registered = false };
        registrationOwner.Poll(registrationStorage, now, registrationSink);
        Test.True(!map.FormalNoticeShown && registrationSink.Notices.Single().DocumentId == "third", "missing right widget defers player notice without blocking third-party full text");
        registrationSink.Registered = true; registrationOwner.Poll(registrationStorage, now.AddSeconds(1), registrationSink);
        Test.True(registrationSink.Notices.Single(n => n.DocumentId == "map").ShowOnMap, "restored player widget publishes once to right, without a text duplicate");

        var queuedStorage = new WorldDiplomacyStorage(); var first = Document("first"); var reclassified = Document("reclassified");
        queuedStorage.Documents.AddRange(new[] { first, reclassified });
        var queuedSink = new Sink(); queuedSink.BeforeNotice = () => reclassified.IsPlayerAuthored = true;
        new WorldDiplomacyNotificationApplication().Poll(queuedStorage, now, queuedSink);
        Test.True(queuedSink.Notices.Count == 1 && !reclassified.FormalNoticeShown, "late player-authored reclassification cannot slip through a cached notification queue");
        Console.WriteLine("Policy-style notices: real production routing/localization and synthetic display sink; live game NOT_RUN.");
    }
}
