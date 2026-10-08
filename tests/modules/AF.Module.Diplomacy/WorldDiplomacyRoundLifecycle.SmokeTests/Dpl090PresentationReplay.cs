using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

internal static class Dpl090PresentationReplay
{
    private sealed class TimelineStateSource : IWorldDiplomacyTimelineStateSource
    {
        internal bool Available;
        internal WorldDiplomacyStorage State;
        public bool TryGetState(out WorldDiplomacyStorage storage)
        {
            storage = State;
            return Available;
        }
    }

    private static void Equal<T>(T expected, T actual, string message) =>
        Test.True(EqualityComparer<T>.Default.Equals(expected, actual), message + $": expected={expected}, actual={actual}");
    internal static void Run()
    {
        Queries();
        Notifications();
        Commands();
        Boundaries();
    }

    private static WorldDiplomacyDocument Document(string id, int day) => new()
    {
        DocumentId = id, Day = day, CreatedUtcTicks = day, AuthorKingdomId = "author",
        AuthorKingdomName = "作者国", AuthorRulerName = "统治者", Title = "宣言", Body = "正文",
        IsReadyForPublication = true, HasReachedPlayerCourt = true, RumorNotified = true, Actions = new()
    };

    private static void Queries()
    {
        var storage = new WorldDiplomacyStorage();
        var first = Document("first", 1);
        first.Actions.Add(new WorldDiplomacyDocumentAction { TargetKingdomId = "target", TargetKingdomName = "目标国" });
        first.MechanicalResult = "确认结果";
        var hidden = Document("hidden", 3);
        hidden.IsReadyForPublication = false;
        var second = Document("second", 2);
        second.HasReachedPlayerCourt = false;
        storage.Documents.AddRange(new[] { first, hidden, second });
        var timeline = WorldDiplomacyPresentationQueries.Timeline(storage, 2).Documents;
        Equal(1, timeline.Count, "timeline bounds before visibility filtering");
        Equal("second", timeline[0].DocumentId, "published timeline does not require court delivery");
        Equal(0, WorldDiplomacyPresentationQueries.Timeline(storage, 0).Documents.Count, "minimum one still excludes newest hidden document");
        var full = WorldDiplomacyPresentationQueries.Timeline(storage, 5000).Documents;
        Equal("first", full[1].DocumentId, "timeline descending ordering");
        Test.True(full[1].ImpactText.Contains("确认结果"), "impact projection includes confirmed results");
        first.Actions[0].TargetKingdomName = "later";
        first.Body = "changed";
        first.IsRead = true;
        Equal("正文", full[1].Body, "timeline body detached from canonical record");
        Equal("目标国", full[1].ActionTargets[0].CountryName, "nested timeline targets detached");
        Test.True(!full[1].IsRead, "timeline read marker is a snapshot");
        var detail = WorldDiplomacyPresentationQueries.Detail(second, 42, true, day => "日期" + day);
        Equal("作者国 · 统治者 · 日期2 · 外交公告", detail.Subtitle, "detail subtitle and localized type preserved");
        Equal(42L, detail.Generation, "detail carries lifecycle value only");
        Test.True(detail.CanReply && !second.IsRead, "query does not mark read");
        Test.True(WorldDiplomacyTimelineApplication.MarkRead(storage, second.DocumentId) && second.IsRead, "read command marks canonical record");
        Test.True(WorldDiplomacyTimelineApplication.MarkRead(storage, second.DocumentId), "read command is idempotent");
        Test.True(!WorldDiplomacyTimelineApplication.MarkRead(storage, "missing"), "missing read target fails");
        first.IsRead = false;
        var source = new TimelineStateSource { State = storage };
        Equal(WorldDiplomacyTimelineDocumentsStatus.Unavailable,
            WorldDiplomacyTimelineApplication.QueryDocuments(source, 2).Status,
            "module query reports missing campaign owner");
        Test.True(!WorldDiplomacyTimelineApplication.MarkRead(source, "first", out bool unavailable)
            && !unavailable && !first.IsRead, "missing owner cannot mark canonical record");
        source.Available = true;
        Equal("second", WorldDiplomacyTimelineApplication.QueryDocuments(source, 2).Documents[0].DocumentId,
            "real module query keeps take-before-visibility ordering");
        Test.True(!WorldDiplomacyTimelineApplication.MarkRead(source, "missing", out bool foundOwner)
            && foundOwner, "missing document differs from missing owner");
        Test.True(WorldDiplomacyTimelineApplication.MarkRead(source, "first", out bool appliedOwner)
            && appliedOwner && first.IsRead, "module command marks the canonical record");
        Test.True(WorldDiplomacyTimelineApplication.MarkRead(source, "first", out _),
            "module read command remains idempotent");

        storage.AnnualSummaries.Add(new WorldDiplomacyAnnualSummary { Year = 2, Summary = "年度", MajorEvents = new() { "事件" } });
        storage.CompressionSummaries.Add(new WorldDiplomacyCompressionSummary { BatchId = "batch", CreatedDay = 20, Summary = "压缩", ConfirmedResults = new() { "结果" } });
        var archive = WorldDiplomacyPresentationQueries.Archive(storage, day => "日期" + day, _ => null!, _ => null!);
        Equal(4, archive.Count, "archive includes published documents and both summary types");
        Equal("second", archive[0].EventId, "archive includes undelivered published declaration");
        Equal("diplomacy_summary:2", archive[2].EventId, "annual summary identity unchanged");
        Equal("第3年外交纪要", archive[2].TitleText, "annual title unchanged");
        Equal("diplomacy_summary:batch", archive[3].EventId, "compression identity unchanged");
        storage.AnnualSummaries[0].MajorEvents.Clear();
        Equal("事件", archive[2].ImpactText, "archive summary does not retain mutable event list");
        storage.Documents.Clear();
        for (int i = 0; i < 500; i++) storage.Documents.Add(Document(i.ToString(), i));
        Equal(420, WorldDiplomacyPresentationQueries.Timeline(storage, 900).Documents.Count, "timeline hard cap");
        Equal(242, WorldDiplomacyPresentationQueries.Archive(storage, _ => "日期", _ => null!, _ => null!).Count, "archive retains 240-document cap plus summaries");
        Test.True(WorldDiplomacyPresentationQueries.Standing(storage, "author").StartsWith("【国家威望与国际声誉】"), "encyclopedia heading preserved");
        Equal("", WorldDiplomacyPresentationQueries.Standing(storage, null!), "no faction yields empty encyclopedia block");
        storage.Documents = null!;
        var empty = WorldDiplomacyPresentationQueries.Timeline(storage, 40);
        Test.True(empty.IsAvailable && empty.Documents.Count == 0, "legacy missing document collection remains an available empty query");
    }

    private sealed class Sink : IWorldDiplomacyNotificationSink
    {
        public bool MapNotificationsEnabled { get; set; } = true;
        public bool Ready = true;
        public bool Registered = true;
        public bool ThrowNotice;
        public int Probes;
        public int Registrations;
        public readonly List<string> Notices = new();
        public readonly List<WorldDiplomacyNotice> RoutedNotices = new();
        public readonly List<string> Rumors = new();
        public readonly List<string> Logs = new();
        public Action? BeforeNotice;
        public bool CanPublishMapNotification() { Probes++; return Ready; }
        public bool EnsureMapNotificationRegistered() { Registrations++; return Registered; }
        public string KingdomName(string id) => id;
        public string FormatCampaignDate(int day) => day.ToString();
        public int CurrentDay => 10;
        public string PlayerKingdomId { get; set; } = "player";
        public void ShowRumor(string text) => Rumors.Add(text);
        public void ShowNotice(WorldDiplomacyNotice notice)
        {
            BeforeNotice?.Invoke();
            if (ThrowNotice) throw new InvalidOperationException("fixture");
            Notices.Add(notice.DocumentId);
            RoutedNotices.Add(notice);
        }
        public void Log(string text) => Logs.Add(text);
    }

    private static void Notifications()
    {
        var storage = new WorldDiplomacyStorage();
        for (int i = 5; i >= 1; i--) storage.Documents.Add(Document("doc" + i, i));
        var owner = new WorldDiplomacyNotificationApplication();
        var sink = new Sink();
        var now = new DateTime(2026, 1, 1);
        sink.BeforeNotice = () => Test.True(!storage.Documents.Single(d => d.DocumentId == "doc1").FormalNoticeShown, "first flag written after successful notice");
        // Check flag timing only on the first delivery.
        Action check = sink.BeforeNotice;
        sink.BeforeNotice = () => { check(); sink.BeforeNotice = null; };
        owner.Poll(storage, now, sink);
        Equal("doc1,doc2,doc3", string.Join(",", sink.Notices), "formal notices use chronological capped batch");
        Test.True(storage.Documents.Single(d => d.DocumentId == "doc1").FormalNoticeShown, "successful delivery writes formal flag");
        int viewBuilds = owner.RebuildCount;
        int probes = sink.Probes;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100000; i++) owner.Poll(storage, now, sink);
        Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before, "throttled notification ticks allocate zero");
        Equal(probes, sink.Probes, "throttled ticks do not probe game state");
        owner.Poll(storage, now.AddSeconds(1), sink);
        Equal(5, sink.Notices.Count, "next second drains remainder");
        Equal(viewBuilds, owner.RebuildCount, "bounded notification batches reuse the document view");
        owner.ResetView();
        owner.Poll(storage, now.AddSeconds(2), sink);
        Equal(5, sink.Notices.Count, "view replacement does not repeat persisted notices");
        owner.Reset();
        var failed = Document("failed", 6);
        storage.Documents.Add(failed);
        sink.ThrowNotice = true;
        owner.Poll(storage, now, sink);
        Test.True(!failed.FormalNoticeShown && !failed.IsNotified, "notice exception leaves persistent flags retryable");
        sink.ThrowNotice = false;
        owner.Poll(storage, now.AddSeconds(1), sink);
        Equal("failed", sink.Notices.Last(), "failure removes session reservation for retry");
        var suppressed = Document("suppressed", 7);
        suppressed.IsRead = true;
        storage.Documents.Add(suppressed);
        sink.MapNotificationsEnabled = false;
        owner.Poll(storage, now.AddSeconds(2), sink);
        Test.True(suppressed.FormalNoticeShown && suppressed.IsNotified, "disabled notifications consume reached published records including read ones");
        sink.MapNotificationsEnabled = true;
        owner.Poll(storage, now.AddSeconds(3), sink);
        Test.True(!sink.Notices.Contains("suppressed"), "reenabling does not replay suppressed notices");

        var deferred = Document("deferred", 8);
        deferred.AuthorKingdomId = "player";
        storage.Documents.Add(deferred);
        sink.Ready = false;
        owner.Poll(storage, now.AddSeconds(4), sink);
        Test.True(!deferred.FormalNoticeShown, "non-map state defers formal notice");
        sink.Ready = true; sink.Registered = false;
        owner.Poll(storage, now.AddSeconds(5), sink);
        Test.True(!deferred.FormalNoticeShown, "registration failure defers formal notice");
        sink.Registered = true;
        deferred.HasReachedPlayerCourt = false;
        owner.Poll(storage, now.AddSeconds(6), sink);
        Test.True(!deferred.FormalNoticeShown, "undelivered declaration not formally notified");
        deferred.RumorNotified = false;
        owner.Poll(storage, now.AddSeconds(7), sink);
        Test.True(deferred.RumorNotified && !deferred.FormalNoticeShown, "published rumor separate from court receipt");
        Equal(1, sink.Rumors.Count, "rumor displayed once");
        deferred.HasReachedPlayerCourt = true;
        owner.Poll(storage, now.AddSeconds(8), sink);
        Test.True(deferred.FormalNoticeShown, "later formal receipt still shows notice after rumor");

        var changingStorage = new WorldDiplomacyStorage();
        var changing = Document("changing", 1);
        changing.IsReadyForPublication = false;
        changingStorage.Documents.Add(changing);
        var changingOwner = new WorldDiplomacyNotificationApplication();
        var changingSink = new Sink { Ready = false };
        changingOwner.Poll(changingStorage, now, changingSink);
        int initialBuilds = changingOwner.RebuildCount;
        changing.IsReadyForPublication = true;
        changingOwner.Poll(changingStorage, now.AddSeconds(1), changingSink);
        Equal(initialBuilds + 1, changingOwner.RebuildCount, "publication mutation rebuilds notification view");
        changing.IsRead = true;
        changingSink.Ready = true;
        changingOwner.Poll(changingStorage, now.AddSeconds(2), changingSink);
        Test.True(!changing.FormalNoticeShown, "read mutation excludes formal notice before delivery");
        changingSink.MapNotificationsEnabled = false;
        changingOwner.Poll(changingStorage, now.AddSeconds(3), changingSink);
        Test.True(changing.FormalNoticeShown, "disabled map notices still consume read formal receipt");
        int settledBuilds = changingOwner.RebuildCount;
        for (int i = 4; i < 1004; i++) changingOwner.Poll(changingStorage, now.AddSeconds(i), changingSink);
        Equal(settledBuilds, changingOwner.RebuildCount, "idle one-second notification deadlines do not rescan documents");
        string saved = Newtonsoft.Json.JsonConvert.SerializeObject(changingStorage);
        Test.True(!saved.Contains("NotificationSelectionChanged", StringComparison.Ordinal)
            && (saved.Contains("\"formalNoticeShown\":true", StringComparison.Ordinal)
                || saved.Contains("\"FormalNoticeShown\":true", StringComparison.Ordinal)),
            "runtime notification invalidation does not change persisted document names");

        var routed = new WorldDiplomacyStorage();
        var domestic = Document("domestic", 1); domestic.AuthorKingdomId = "PLAYER";
        var foreign = Document("foreign", 2); foreign.TargetKingdomId = "player";
        var unrelated = Document("unrelated", 3); unrelated.TargetKingdomId = "other";
        unrelated.Body = "player PRIVATE_BODY_DO_NOT_LOG"; // Body mentions are not notification eligibility.
        var playerWritten = Document("self", 4); playerWritten.AuthorKingdomId = "player"; playerWritten.IsPlayerAuthored = true;
        var notArrived = Document("not-arrived", 5); notArrived.TargetKingdomId = "player"; notArrived.HasReachedPlayerCourt = false;
        routed.Documents.AddRange(new[] { domestic, foreign, unrelated, playerWritten, notArrived });
        var routeOwner = new WorldDiplomacyNotificationApplication();
        var routeSink = new Sink { Registered = false };
        routeOwner.Poll(routed, now, routeSink);
        Equal("unrelated", string.Join(",", routeSink.Notices), "unavailable player-realm widgets do not block unrelated foreign text");
        Test.True(!routeSink.RoutedNotices.Single().ShowOnMap && !domestic.FormalNoticeShown && !foreign.FormalNoticeShown,
            "author and explicit player target both remain retryable until the widget registers");
        Equal(1, routeSink.Registrations, "widget registration is probed once for both player-realm notices in a poll");
        Test.True(routeSink.Logs.Any(s => s.Contains("document=unrelated") && s.Contains("route=text")
            && s.Contains("author=author") && s.Contains("targets=other") && s.Contains("player=player")),
            "third-party text route records its author, targets and current player realm");
        Test.True(routeSink.Logs.All(s => !s.Contains("PRIVATE_BODY_DO_NOT_LOG")), "route diagnostics never log document body");
        int routeBuilds = routeOwner.RebuildCount;
        routeOwner.Poll(routed, now.AddSeconds(1), routeSink);
        Equal(routeBuilds, routeOwner.RebuildCount, "deferred widget retries do not rescan document storage");
        routeSink.Registered = true;
        routeOwner.Poll(routed, now.AddSeconds(2), routeSink);
        Test.True(domestic.FormalNoticeShown && foreign.FormalNoticeShown
            && routeSink.RoutedNotices.Where(n => n.DocumentId != "unrelated").All(n => n.ShowOnMap),
            "same-country author and foreign player target both restore right icons on registration recovery, ignoring ID case");
        Test.True(routeSink.Logs.Any(s => s.Contains("document=foreign") && s.Contains("route=map")
            && s.Contains("targets=player") && s.Contains("player=player")), "direct player map publication has a distinguishable route diagnostic");
        Test.True(!playerWritten.FormalNoticeShown && !notArrived.FormalNoticeShown,
            "existing self-authored exclusion and court-delivery gates remain");
        routeOwner.ResetView(); routeOwner.Poll(routed, now.AddSeconds(3), routeSink);
        Equal(3, routeSink.Notices.Count, "replacing view does not duplicate either notification route");

        var targetStorage = new WorldDiplomacyStorage();
        var actionTarget = Document("action-target", 1);
        actionTarget.TargetKingdomId = "other";
        actionTarget.Actions.Add(new WorldDiplomacyDocumentAction { TargetKingdomId = "other", ChangedDiplomaticState = true });
        actionTarget.Actions.Add(new WorldDiplomacyDocumentAction { TargetKingdomId = "PLAYER", ChangedDiplomaticState = false });
        var addressedTarget = Document("addressed-target", 2);
        addressedTarget.AddressedKingdomIds = new() { "other", "player" };
        var representedTarget = Document("represented-target", 3);
        representedTarget.TargetKingdomId = "representative";
        representedTarget.Actions.Add(new WorldDiplomacyDocumentAction { TargetKingdomId = "representative" });
        representedTarget.AddressedKingdomIds = new() { "player" };
        var explicitWithActions = Document("explicit-with-actions", 4);
        explicitWithActions.TargetKingdomId = "player";
        explicitWithActions.Actions.Add(new WorldDiplomacyDocumentAction { TargetKingdomId = "other" });
        var readTarget = Document("read-target", 5); readTarget.TargetKingdomId = "player"; readTarget.IsRead = true;
        targetStorage.Documents.AddRange(new[] { actionTarget, addressedTarget, representedTarget, explicitWithActions, readTarget });
        var targetOwner = new WorldDiplomacyNotificationApplication();
        var targetSink = new Sink();
        targetOwner.Poll(targetStorage, now, targetSink);
        Equal("action-target,addressed-target,represented-target", string.Join(",", targetSink.Notices),
            "foreign multi-action, explicit addressed list and mechanically represented addressee use capped right-icon publication");
        Test.True(targetSink.RoutedNotices.All(n => n.ShowOnMap), "non-changing action targets still concern the player realm");
        targetOwner.Poll(targetStorage, now.AddSeconds(1), targetSink);
        Test.True(explicitWithActions.FormalNoticeShown && targetSink.RoutedNotices.Last().ShowOnMap,
            "explicit document target remains eligible even when the action list has another target");
        Test.True(!readTarget.FormalNoticeShown && !targetSink.Notices.Contains("read-target"), "read player-target documents remain excluded");
        targetOwner.ResetView(); targetOwner.Poll(targetStorage, now.AddSeconds(2), targetSink);
        Equal(4, targetSink.Notices.Count, "all target routes persist once-only notification after view reset");

        var noRealmStorage = new WorldDiplomacyStorage();
        var noRealm = Document("no-realm", 1); noRealm.AuthorKingdomId = "";
        noRealmStorage.Documents.Add(noRealm);
        var noRealmSink = new Sink { PlayerKingdomId = "", Registered = false };
        new WorldDiplomacyNotificationApplication().Poll(noRealmStorage, now, noRealmSink);
        Test.True(noRealm.FormalNoticeShown && !noRealmSink.RoutedNotices.Single().ShowOnMap && noRealmSink.Registrations == 0,
            "empty player and author realms never count as domestic or require widget");
        var disabledStorage = new WorldDiplomacyStorage();
        var disabledOwn = Document("disabled-own", 1); disabledOwn.AuthorKingdomId = "player";
        var disabledForeign = Document("disabled-foreign", 2); disabledForeign.TargetKingdomId = "player";
        disabledStorage.Documents.AddRange(new[] { disabledOwn, disabledForeign });
        var disabledSink = new Sink { MapNotificationsEnabled = false };
        new WorldDiplomacyNotificationApplication().Poll(disabledStorage, now, disabledSink);
        Test.True(disabledOwn.FormalNoticeShown && disabledForeign.FormalNoticeShown && disabledSink.Notices.Count == 0 && disabledSink.Registrations == 0,
            "notification switch suppresses both routes without widget access or changing delivery");
    }

    private sealed class PlayerWorld : IWorldDiplomacyPlayerWorld
    {
        public WorldDiplomacyPlayerContext Player { get; set; } = new(42, "player", true, true, "宗主国");
        public WorldDiplomacyRound Round = new() { RoundId = "round", State = "active" };
        public WorldDiplomacyDocument? Source = new() { DocumentId = "source", RoundId = "round", AuthorKingdomId = "target", AutomaticReplyDepth = 2 };
        public bool TargetExists = true;
        public readonly List<WorldDiplomacyDocument> Added = new();
        public readonly List<string> Effects = new();
        public bool KingdomExists(string id) => TargetExists;
        public WorldDiplomacyDocument ResolveDocument(string id) => Source?.DocumentId == id ? Source : null!;
        public WorldDiplomacyRound ResolveRound(string id) => id == Round.RoundId ? Round : null!;
        public int CurrentDay() => 8;
    }

    private sealed class PlayerOrch : FakeOrchestration
    {
        private readonly PlayerWorld _world;
        internal PlayerOrch(PlayerWorld world) { _world = world; }
        public override WorldDiplomacyRound EnsureActiveRound(string initiatorId, string targetId, bool isPlayerInsertion)
        { Test.True(isPlayerInsertion, "player insertion preserved"); return _world.Round; }
        public override WorldDiplomacyDocument CreateDocument(string authorId, string targetId, string title, string body, string origin,
            bool isPlayerAuthored, bool isResponse, string exchangeId) => new()
            {
                DocumentId = "new" + _world.Added.Count, AuthorKingdomId = authorId, TargetKingdomId = targetId,
                Title = title, Body = body, Origin = origin, IsPlayerAuthored = isPlayerAuthored,
                IsResponse = isResponse, ExchangeId = exchangeId
            };
        public override void AddDocument(WorldDiplomacyDocument document) { _world.Added.Add(document); _world.Effects.Add("add"); }
        public override void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument document) { document.IsReadyForPublication = true; _world.Effects.Add("publish"); }
        public override void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority)
        { Equal(100, priority, "player analysis priority"); _world.Effects.Add("analysis"); }
    }

    private static void Commands()
    {
        var world = new PlayerWorld();
        var orch = new PlayerOrch(world);
        string Execute(string text, long generation = 42) => WorldDiplomacyPlayerApplication.Execute(world, new(text, generation), orch);
        Equal("", Execute("正文", 41), "old generation ignored");
        Equal("外交宣言正文不能为空。", Execute("  "), "blank declaration rejected before effects");
        world.Player = new(42, "player", false, true, "宗主国");
        Test.True(Execute("正文").Contains("不再是王国统治者"), "authority revalidated at submission");
        world.Player = new(42, "player", true, false, "宗主国");
        Test.True(Execute("正文").Contains("宗主国"), "dependent ruler denied with representative name");
        Equal(0, world.Added.Count, "rejected declaration performs no effects");
        world.Player = new(42, "player", true, true, "宗主国");
        Test.True(Execute("  正文  ").StartsWith("外交宣言已经公开发布"), "declaration accepted");
        Equal("add,publish,analysis", string.Join(",", world.Effects), "publication precedes analysis");
        Equal("正文", world.Added[0].Body, "body normalized");
        Equal(world.Added[0].DocumentId, world.Round.RootDocumentId, "root document set");
        Equal(8, world.Round.LastActivityDay, "player activity time preserved");
        world.Effects.Clear();
        var reply = new WorldDiplomacyPlayerDocumentCommand("回应", 42, "source", "round");
        Test.True(WorldDiplomacyPlayerApplication.Execute(world, reply, orch).StartsWith("外交回应已经公开发布"), "reply accepted");
        var response = world.Added.Last();
        Equal("source", response.SourceDocumentId, "reply bound to exact source");
        Equal("round", response.RoundId, "reply bound to original round");
        Equal(3, response.AutomaticReplyDepth, "reply depth preserved");
        Equal("target", response.TargetKingdomId, "reply target is original author");
        Equal("add,publish,analysis", string.Join(",", world.Effects), "reply publication precedes analysis");
        Test.True(!world.Round.Participants.Single(p => p.KingdomId == "player").MandatoryReplyPending, "player reply obligation cleared");
        int count = world.Added.Count;
        world.Source!.RoundId = "new-round";
        Equal("", WorldDiplomacyPlayerApplication.Execute(world, reply, orch), "rebound source cannot settle original round");
        world.Source = null;
        Equal("", WorldDiplomacyPlayerApplication.Execute(world, reply, orch), "removed source ignored");
        Equal(count, world.Added.Count, "stale reply produces no side effects");
    }

    private static void Boundaries()
    {
        string root = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(root, "AnimusForge.csproj"))) root = Directory.GetParent(root)!.FullName;
        string Read(string path) => File.ReadAllText(Path.Combine(root, path));
        string presentation = Read("src/modules/AF.Module.Diplomacy/Presentation/WorldDiplomacyPresentation.cs");
        Test.True(!presentation.Contains("_storage") && !presentation.Contains("WorldDiplomacyDocument document")
            && !presentation.Contains("WorldDiplomacyRound round") && !presentation.Contains(".IsRead ="), "UI only consumes read models and commands");
        Test.True(presentation.Contains("WorldDiplomacyPresentationHost.Submit(new WorldDiplomacyPlayerDocumentCommand"), "compose/reply route through typed command");
        Test.True(presentation.Contains("WorldDiplomacyPresentationHost.Archive()"), "archive consumes query projection");
        Test.True(Read("src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalBehavior.cs").Contains("DiplomacyPresentationBridge.OpenComposeFromTerminal()"), "terminal routes to presenter");
        Test.True(Read("src/AF.GameAdapter.Bannerlord/UI/Kingdom/EncyclopediaKingdomStabilityPatch.cs").Contains("DiplomacyPresentationBridge.Standing(kingdom?.StringId)"), "encyclopedia sends only stable id");
        foreach (string file in new[] { "WorldDiplomacyPresentationQueries", "WorldDiplomacyPlayerApplication", "WorldDiplomacyNotificationApplication" })
        {
            string source = Read("src/modules/AF.Module.Diplomacy/Application/" + file + ".cs");
            Test.True(!source.Contains("TaleWorlds") && !source.Contains("InformationManager") && !source.Contains("WorldDiplomacyBehavior"), file + " is game/UI independent");
        }
        foreach (Type type in new[] { typeof(WorldDiplomacyArchiveRecord), typeof(WorldDiplomacyDocumentDetail), typeof(WorldDiplomacyPlayerDocumentCommand), typeof(WorldDiplomacyPlayerContext), typeof(WorldDiplomacyNotice) })
            Test.True(type.GetProperties().All(p => !p.CanWrite), type.Name + " immutable contract");
    }
}
