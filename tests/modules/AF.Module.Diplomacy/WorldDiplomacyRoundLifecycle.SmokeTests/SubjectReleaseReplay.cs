using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Production orchestration/tag/analysis/execution; campaign permissions and effects are fakes.
internal static class SubjectReleaseReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host, IWorldDiplomacySubjectReleaseHost, IWorldDiplomacyDialogueHost
    {
        internal string Token = "first_incarnation";
        internal bool PlayerRuler = true;
        internal int Releases;
        internal bool ThrowMemory;
        public new MemoryCommitResult CommitFact(string ruler, string source, string fact, int day,
            string location, int hour = -1, string npcName = null, string gameDate = "")
        {
            if (ThrowMemory) throw new InvalidOperationException("memory_port_failure");
            return base.CommitFact(ruler, source, fact, day, location, hour, npcName, gameDate);
        }
        public override IWorldDiplomacyAnalysisPort AnalysisPort() => new AnalysisPort(this);
        public override IWorldDiplomacyPeaceAdmissionPort PeaceAdmission() => new PeaceAdmissionReplay.Port { War = false };
        public override bool HasIndependentAuthority(string id) => id != "vassal";
        public Dictionary<string, string> CapturePlayerSubjectReleaseTokens(string suzerain) =>
            PlayerRuler && suzerain == "p" && Token.Length > 0 ? new() { ["vassal"] = Token } : new();
        public string PlayerSubjectReleaseToken(string suzerain, string subject) =>
            PlayerRuler && suzerain == "p" && subject == "vassal" ? Token : "";
        public WorldDiplomacyImmediateActionReceipt ReleasePlayerSubject(string suzerain, string subject, string token)
        {
            if (PlayerSubjectReleaseToken(suzerain, subject) != token) return new(false, "stale");
            Releases++; Token = ""; return new(true, "已释放臣属国，恢复独立。");
        }
    }
    private readonly struct TagSource : IDiplomacyOralTagSource, IDiplomacySubjectReleaseTagSource
    {
        private readonly Host _host;
        private readonly string _channel;
        internal TagSource(Host host, string channel) { _host = host; _channel = channel; }
        public bool HasSpeaker => true;
        public bool IsAvailable => true;
        public string SpeakerHeroId => "ruler_vassal";
        public string ReleaseSubject(string payload) => _host.Owner.SubmitPlayerSubjectRelease("ruler_vassal", "vassal", payload,
            new DialogueInteractionOrigin(_channel, "interaction", "session", "我决定让你的国家独立。PRIVATE_PLAYER", "NPC_PRIVATE"));
        public string DeclareWar(string payload) => throw new Exception("unexpected war");
        public string MakePeace(string payload) => throw new Exception("unexpected peace");
        public string IndependentClanPeace(string payload) => throw new Exception("unexpected clan peace");
        public string FormAlliance(string payload) => throw new Exception("unexpected alliance");
        public string BreakAlliance(string payload) => throw new Exception("unexpected alliance break");
        public string MakeTrade(string payload) => throw new Exception("unexpected trade");
        public string CancelTrade(string payload) => throw new Exception("unexpected trade cancel");
        public void Log(string message) { }
    }
    private sealed class AnalysisPort : IWorldDiplomacyAnalysisPort
    {
        private readonly Host _host;
        internal AnalysisPort(Host host) => _host = host;
        public WorldDiplomacyStorage Storage => _host.Owner.CurrentStorage;
        public IWorldDiplomacyDocumentExecutionPort Execution => _host.DocumentExecution();
        public int MaxAutomaticReplyDepth => 3;
        public string KingdomName(string id) => id;
    }
    private static (Host, WorldDiplomacyOrchestration) Fixture()
    {
        var host = new Host { PublishEnabled = true };
        var owner = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState());
        host.Owner = owner; return (host, owner);
    }
    private static WorldDiplomacyDocument Declaration(WorldDiplomacyOrchestration owner)
    {
        var doc = owner.CreateDocument("p", null, "允许独立", "本国决定释放vassal国，解除其臣属条约。", "player", true, false, "");
        owner.AddDocument(doc);
        owner.PublishPlayerAuthoredDocumentImmediately(doc);
        return doc;
    }
    private const string ReleaseJson = "{\"status\":\"success\",\"intent\":\"release_subject\",\"commitment\":\"binding\",\"primary_target_kingdom_id\":\"vassal\",\"requires_response\":true}";
    private static void Analyze(Host host, WorldDiplomacyOrchestration owner, WorldDiplomacyDocument doc) =>
        WorldDiplomacyAnalysisApplication.Commit(new AnalysisPort(host), owner,
            new WorldDiplomacyJob { DocumentId = doc.DocumentId, Kind = "analyze" }, ReleaseJson);

    internal static void Run()
    {
        Test.True(WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent("release_subject")
            && WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent("release_subject")
            && WorldDiplomacyIntentVocabulary.IsImmediateIntent("release_subject")
            && WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("release_subject") == "binding"
            && !WorldDiplomacyIntentVocabulary.IsFormalTreatyIntent("release_subject"), "release is a unilateral action, not treaty creation");
        Test.True(DiplomacySubjectReleasePayload.TryParse("target=vassal;agreement=first_incarnation", out var target, out var token)
            && target == "vassal" && token == "first_incarnation", "release parser preserves target and incarnation");
        foreach (var payload in new[] { "", "target=vassal", "agreement=token", "target=vassal;target=other", "target=vassal;agreement=",
            "target=vassal;agreement=token;unknown=x", "target=vassal;unknown=token", "target=vassal;agreement=token=extra", "target=vassal;agreement=" + new string('x', 601) })
            Test.True(!DiplomacySubjectReleasePayload.TryParse(payload, out _, out _), "reject malformed release payload: " + payload);

        foreach (string channel in new[] { "native", "scene", "courier" })
        {
            var (host, owner) = Fixture();
            string context = owner.BuildPlayerSubjectReleaseContext("ruler_vassal", "vassal");
            Test.True(context.Contains("agreement=first_incarnation") && context.Contains("无须NPC同意"), "runtime context exposes exact release token: " + channel);
            string response = "收到。 [ACTION:DIPLOMACY:RELEASE_SUBJECT:target=vassal;agreement=first_incarnation]";
            DiplomacyOralTagApplication.Process(new TagSource(host, channel), ref response);
            Test.True(host.Releases == 1 && response.Contains("已释放") && !response.Contains("[ACTION:"), "shared dispatch executes and strips release tag: " + channel);
            var doc = owner.CurrentStorage.Documents.Single();
            Test.True(doc.IsPlayerAuthored && doc.ChangedDiplomaticState && doc.Intent == "release_subject"
                && doc.AnalysisStatus == "success" && !doc.RequiresResponse, "player-authored release receipt: " + channel);
            Test.True(!doc.Body.Contains("PRIVATE") && host.Facts.Count > 0 && doc.HistoryResultRecorded,
                "public result and personal fact preserve private conversation: " + channel);
            Test.True(owner.CurrentStorage.KingdomKnowledge.Any(x => x.KingdomId == "vassal" && x.DocumentIds.Contains(doc.DocumentId)),
                "counterparty court knows released status immediately: " + channel);
            string repeat = "[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=vassal;agreement=first_incarnation]";
            DiplomacyOralTagApplication.Process(new TagSource(host, channel), ref repeat);
            Test.True(host.Releases == 1 && owner.CurrentStorage.Documents.Count == 1 && repeat.Contains("未执行"), "repeat release has no effect or new public fact: " + channel);
            host.Token = "second_incarnation";
            string delayed = "[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=vassal;agreement=first_incarnation]";
            DiplomacyOralTagApplication.Process(new TagSource(host, channel), ref delayed);
            Test.True(host.Releases == 1 && host.Token == "second_incarnation", "old chat cannot release re-signed agreement: " + channel);
        }
        {
            var (host, owner) = Fixture();
            foreach (var input in new[] { ("ruler_b", "vassal", "target=vassal;agreement=first_incarnation"),
                ("ruler_vassal", "vassal", "target=b;agreement=first_incarnation"),
                ("ruler_vassal", "vassal", "target=vassal;agreement=wrong") })
                owner.SubmitPlayerSubjectRelease(input.Item1, input.Item2, input.Item3, new("native", playerText: "允许独立"));
            owner.SubmitPlayerSubjectRelease("ruler_vassal", "vassal", "target=vassal;agreement=first_incarnation", null);
            owner.SubmitPlayerSubjectRelease("ruler_vassal", "vassal", "target=vassal;agreement=first_incarnation", new("native"));
            host.PlayerRuler = false;
            Test.True(owner.BuildPlayerSubjectReleaseContext("ruler_vassal", "vassal") == "", "non-ruler gets no release context");
            owner.SubmitPlayerSubjectRelease("ruler_vassal", "vassal", "target=vassal;agreement=first_incarnation", new("native", playerText: "允许独立"));
            Test.True(host.Releases == 0 && owner.CurrentStorage.Documents.Count == 0, "non-ruler, wrong NPC/target/token and absent player source denied");
        }
        {
            var (host, owner) = Fixture(); var doc = Declaration(owner);
            Test.True(doc.SubjectReleaseTokens["vassal"] == host.Token, "declaration freezes treaty before async analysis");
            var roundtrip = JsonConvert.DeserializeObject<WorldDiplomacyDocument>(JsonConvert.SerializeObject(doc));
            Test.True(roundtrip.SubjectReleaseTokens["vassal"] == host.Token && roundtrip.AuthorRulerId == "ruler_p", "save JSON retains incarnation and author");
            Analyze(host, owner, doc);
            Test.True(host.Releases == 1 && doc.ChangedDiplomaticState && !doc.RequiresResponse
                && doc.HistoryResultRecorded && doc.IsReadyForPublication, "real analysis executes release of controlled vassal without consent");
            Analyze(host, owner, doc);
            Test.True(host.Releases == 1, "analysis replay does not execute released document twice");
        }
        foreach (string mutation in new[] { "new_agreement", "lost_ruler", "missing_snapshot", "npc_author", "foreign_author" })
        {
            var (host, owner) = Fixture(); var doc = Declaration(owner);
            if (mutation == "new_agreement") host.Token = "second_incarnation";
            if (mutation == "lost_ruler") host.PlayerRuler = false;
            if (mutation == "missing_snapshot") doc.SubjectReleaseTokens = null;
            if (mutation == "npc_author") { doc.IsPlayerAuthored = false; doc.AuthorKingdomId = "a"; }
            if (mutation == "foreign_author") doc.AuthorKingdomId = "b";
            Analyze(host, owner, doc);
            Test.True(host.Releases == 0 && !doc.ChangedDiplomaticState, "late or unauthorized declaration rejected: " + mutation);
        }
        {
            var (host, owner) = Fixture(); host.ThrowMemory = true;
            string response = "[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=vassal;agreement=first_incarnation]";
            DiplomacyOralTagApplication.Process(new TagSource(host, "scene"), ref response);
            Test.True(host.Releases == 1 && response.Contains("已释放") && owner.CurrentStorage.Documents.Single().HistoryResultRecorded,
                "personal memory failure cannot hide or replay a completed release");
        }
        {
            var (host, owner) = Fixture();
            Test.True(owner.CanReleasePlayerSubject("p", "vassal") && !owner.CanReleasePlayerSubject("a", "vassal")
                && !owner.CanReleasePlayerSubject("p", "b"), "release legal only in player suzerain direction");
            var doc = Declaration(owner); doc.TargetKingdomId = "vassal";
            WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(host.DocumentExecution(), owner, doc,
                "break_alliance", "binding", false, "neutral", 1);
            Test.True(host.Releases == 0 && !doc.ChangedDiplomaticState, "controlled-vassal authority exception does not enable ordinary alliance breaking");
        }
    }
}
